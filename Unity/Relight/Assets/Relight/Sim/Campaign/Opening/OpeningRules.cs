using System;
using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>
    /// C-09's pure rules, ported from reference openingEncounter.ts: which turret counts, which Assembler feeds it,
    /// whether a route actually reaches it, and the once-only "is this a fresh campaign" decision.
    ///
    /// Engine-independent and allocation-light: the walks reuse the caller's scratch lists and the conveyor routes
    /// come from <see cref="FlowRules.Destinations"/>, which is already cached on <see cref="SimState.Rev"/>.
    /// </summary>
    public static class OpeningRules
    {
        /// <summary>
        /// The same numbers <c>CatalogueData.Opening()</c> carries, so a <see cref="GameData"/> assembled without an
        /// opening row (a pre-C-09 fixture) still has tuning rather than a null — <see cref="GameData"/> assigns
        /// <c>Opening</c> with no fallback of its own, unlike <see cref="DefenceTuning.Fallback"/>. The catalogue row
        /// always wins when one is supplied. Editing GameData.cs to add the fallback there is outside C-09's
        /// ownership; the exact patch is in the wave-3 W-A report.
        /// </summary>
        public static readonly OpeningEncounterTuning Fallback = new OpeningEncounterTuning(
            25.0, 5, 300.0, 300.0, 660.0, 60.0, 45.0, 4, 3,
            "provisional", "packages/sim/src/openingEncounter.ts:27; packages/sim/src/openingEncounter.ts:55", true);

        public static OpeningEncounterTuning Tuning(GameData d) => d != null && d.Opening != null ? d.Opening : Fallback;

        /// <summary>Reference openingEncounter.ts:52 <c>RELAYS</c>: the only kinds a supply route may relay through.</summary>
        public static bool IsRelay(string kind) =>
            string.Equals(kind, "chest", StringComparison.Ordinal)
            || string.Equals(kind, "tramstop", StringComparison.Ordinal)
            || string.Equals(kind, "depot", StringComparison.Ordinal);

        /// <summary>Reference openingEncounter.ts:28 <c>COMPASS</c>, keyed by <see cref="DirectorRules.HeadingWord"/>'s letters.</summary>
        public static string Compass(string heading)
        {
            switch (heading)
            {
                case "N": return "north";
                case "NE": return "north-east";
                case "E": return "east";
                case "SE": return "south-east";
                case "S": return "south";
                case "SW": return "south-west";
                case "W": return "west";
                case "NW": return "north-west";
                default: return "outskirts";
            }
        }

        // ------------------------------------------------------------------ turrets

        /// <summary>Reference <c>liveTurrets</c>: every turret that still has hit points, loaded or not, powered or not.</summary>
        public static void LiveTurrets(SimContext ctx, SimState st, List<Machine> into)
        {
            into.Clear();
            for (var i = 0; i < st.Machines.Count; i++)
            {
                var m = st.Machines[i];
                if (!TurretHopper.IsTurret(ctx.Data, m)) continue;
                if (TurretQueries.Hp(ctx, st, m.Id) <= 0) continue;
                into.Add(m);
            }
        }

        /// <summary>
        /// Reference <c>homeTurrets</c>: the turrets that protect Home. The reference tests membership of the
        /// Founders Court block or the authored opening bounds; the port has neither (U-D-32 retired blocks), so a
        /// Home turret is one that stands nearer the Home core than to any authored camp — fully derived, no new
        /// constant, and the same answer for every turret the opening can be about. With no camps on the map
        /// (the synthetic test region) every live turret is a Home turret.
        /// </summary>
        public static void HomeTurrets(SimContext ctx, SimState st, List<Machine> into)
        {
            LiveTurrets(ctx, st, into);
            if (ctx.Sites == null || !st.Home.Placed) return;
            var core = HomeQueries.CoreRect(st);
            var cx = core.X + core.W / 2.0;
            var cy = core.Y + core.H / 2.0;
            for (var i = into.Count - 1; i >= 0; i--)
            {
                var m = into[i];
                var mx = m.X + m.Size / 2.0;
                var my = m.Y + m.Size / 2.0;
                var toCore = Dist(mx, my, cx, cy);
                var nearestCamp = double.PositiveInfinity;
                foreach (var s in ctx.Sites.OfKind(SiteKind.Camp))
                {
                    var c = s.Centre;
                    var d = Dist(mx, my, c.X, c.Y);
                    if (d < nearestCamp) nearestCamp = d;
                }
                if (toCore > nearestCamp) into.RemoveAt(i);
            }
        }

        /// <summary>
        /// Reference <c>readyOpeningTurret</c>: the first operational Home turret whose hopper is full. Hand loading
        /// and belts both qualify — <see cref="TurretQueries.Ready"/> is the per-turret half of the same test.
        /// </summary>
        public static Machine ReadyTurret(SimContext ctx, SimState st, List<Machine> scratch)
        {
            HomeTurrets(ctx, st, scratch);
            for (var i = 0; i < scratch.Count; i++)
                if (TurretQueries.Ready(ctx, st, scratch[i].Id)) return scratch[i];
            return null;
        }

        // ------------------------------------------------------------------ supply

        /// <summary>A processor whose current recipe makes the turret round (reference <c>magazineProducers</c>).</summary>
        public static bool IsBulletProducer(SimContext ctx, SimState st, Machine m)
        {
            if (m == null || !ProductionRules.IsProcessor(ctx.Data, m)) return false;
            var r = ProductionRules.RecipeOf(ctx.Data, st, m);
            return r != null && ProductionRules.OutputItem(r) == ItemId.Magazine;
        }

        /// <summary>
        /// Reference openingEncounter.ts:55 <c>supplyChainReaches</c>: true when items leaving <paramref name="source"/>
        /// can reach <paramref name="target"/> through belts, undergrounds, splitters and inserters, relaying through
        /// at most <paramref name="depth"/> stores. A belt that points elsewhere never qualifies.
        /// </summary>
        public static bool SupplyChainReaches(SimContext ctx, SimState st, Machine source, Machine target, int depth)
        {
            if (source == null || target == null) return false;
            var routes = FlowRules.Destinations(st);
            var seen = new HashSet<int>();
            return Visit(st, routes, source, target, source, depth, seen);
        }

        private static bool Visit(SimState st, List<ConveyorRoute> routes,
            Machine source, Machine target, Machine m, int left, HashSet<int> seen)
        {
            if (m.Id == target.Id) return true;
            if (left <= 0 || seen.Contains(m.Id) || (m.Id != source.Id && !IsRelay(m.Kind))) return false;
            seen.Add(m.Id);
            var next = new List<Machine>();
            for (var i = 0; i < st.Machines.Count; i++)
            {
                var b = st.Machines[i];
                if (FlowRules.IsInserter(b.Kind))
                {
                    var from = ProductionRules.InputTile(b);
                    var to = ProductionRules.OutputTile(b);
                    var pick = ProductionRules.MachineAt(st, from.x, from.y);
                    if (pick == null || pick.Id != m.Id) continue;
                    var drop = ProductionRules.MachineAt(st, to.x, to.y);
                    if (drop != null) next.Add(drop);
                }
                else if (FlowRules.IsConveyor(b.Kind))
                {
                    var behind = ProductionRules.MachineAt(st, b.X - Dirs.DX[(int)b.Dir], b.Y - Dirs.DY[(int)b.Dir]);
                    if (behind == null || behind.Id != m.Id) continue;
                    var route = FlowRules.RouteOf(routes, b.Id);
                    if (route != null) next.AddRange(route.Ends);
                }
            }
            for (var i = 0; i < next.Count; i++)
                if (Visit(st, routes, source, target, next[i], left - 1, seen)) return true;
            return false;
        }

        /// <summary>
        /// The finished output a processor is holding right now (reference <c>m.out</c>): the recipe's output item
        /// in its inventory. <c>Machine.Out</c> is the retired Phase B field — nothing in the port ever writes it
        /// (MachinePhase.TickProcessor adds output to <c>m.Inv</c>), which is why the integrated run of 2026-09-14
        /// never saw production: every "out" check here must read the inventory instead.
        /// </summary>
        public static int OutputBuffer(SimContext ctx, SimState st, Machine m)
        {
            if (m == null || !ProductionRules.IsProcessor(ctx.Data, m)) return 0;
            var r = ProductionRules.RecipeOf(ctx.Data, st, m);
            return r == null ? 0 : (int)Math.Floor(m.Inv[ProductionRules.OutputItem(r)]);
        }

        /// <summary>
        /// Did this machine finish a batch during the current tick? MachinePhase raises
        /// <see cref="MachineProducedEvent"/> before the opening phase runs, and <c>st.Events</c> is drained once per
        /// host frame (Simulation.DrainEvents), never between phases — so within a tick the event is always still
        /// there. This is the half of the reference's per-machine <c>observation.produced</c> that a belt or an
        /// inserter draining the buffer in the same tick would otherwise hide.
        /// </summary>
        public static bool ProducedThisTick(SimState st, Machine m)
        {
            if (m == null) return false;
            for (var i = st.Events.Count - 1; i >= 0; i--)
                if (st.Events[i] is MachineProducedEvent e && e.MachineId == m.Id && e.T >= st.T) return true;
            return false;
        }

        /// <summary>
        /// Reference openingEncounter.ts:74 <c>turretSupplier</c>: a PRODUCING bullet processor whose output route
        /// reaches this turret. "Producing" is the reference's <c>observation.produced.magazine &gt; 0 || out &gt; 0</c>;
        /// the port has no per-machine produced counter, so it is the live buffer (<see cref="OutputBuffer"/>), a
        /// batch finished this tick, OR the sticky <see cref="OpeningState.ProducedAt"/> (see that field for why).
        /// </summary>
        public static Machine TurretSupplier(SimContext ctx, SimState st, Machine turret)
        {
            if (turret == null) return null;
            var everProduced = st.Opening.ProducedAt >= 0;
            var depth = Tuning(ctx.Data).SupplyChainDepth;
            for (var i = 0; i < st.Machines.Count; i++)
            {
                var m = st.Machines[i];
                if (!IsBulletProducer(ctx, st, m)) continue;
                if (!everProduced && OutputBuffer(ctx, st, m) <= 0 && !ProducedThisTick(st, m)) continue;
                if (SupplyChainReaches(ctx, st, m, turret, depth)) return m;
            }
            return null;
        }

        // ------------------------------------------------------------------ stock

        /// <summary>
        /// What is waiting at Home, for the "(M available at Home)" half of a build step. The reference reads its
        /// abstract <c>flow.store</c>; the port retired that (U-D-32) and keeps real chests, so this is the stock of
        /// every supply chest on the map.
        /// </summary>
        public static int HomeStock(SimState st, ItemId item)
        {
            double n = 0;
            for (var i = 0; i < st.Machines.Count; i++)
            {
                var m = st.Machines[i];
                if (string.Equals(m.Kind, "chest", StringComparison.Ordinal)) n += m.Inv[item];
            }
            return (int)Math.Floor(n);
        }

        // ------------------------------------------------------------------ the once-only decision

        /// <summary>
        /// Reference openingEncounter.ts:43 <c>initOpeningEncounter</c>. A fresh campaign waits for its first full
        /// turret; a progressed save never receives a beginner attack (§7.8 — the ONE case that permanently skips).
        /// The reference's last clause, "no recovered or installed site", has no port equivalent yet (site
        /// progression is not in Phase C); every other clause is carried verbatim.
        /// </summary>
        public static void Init(SimContext ctx, SimState st)
        {
            var op = st.Opening;
            if (op.Status != OpeningStatus.None) return;
            var d = st.Director;
            var t = Tuning(ctx.Data);
            var scratch = new List<Machine>();
            LiveTurrets(ctx, st, scratch);
            var fresh = st.T <= 0
                || (d.History.Count == 0 && d.RaidsStarted == 0 && d.Major == null && d.Minor == null
                    && d.NextStart - st.T >= t.GuardS
                    && scratch.Count < t.TurretObjective);
            op.Status = fresh ? OpeningStatus.Pending : OpeningStatus.Skipped;
            op.Shots = 0;
            op.FedSeen = st.Stats.TurretFed;
        }

        // ------------------------------------------------------------------ U-Q-21's cap

        /// <summary>
        /// U-Q-21's second half: the player has set off for the first camp. Derived, with no new constant — the
        /// engineer is nearer an authored camp than the Home core. False on a map with no camps.
        /// </summary>
        public static bool LeftForFirstCamp(SimContext ctx, SimState st)
        {
            if (ctx.Sites == null || !st.Home.Placed) return false;
            var core = HomeQueries.CoreRect(st);
            var cx = core.X + core.W / 2.0;
            var cy = core.Y + core.H / 2.0;
            var e = st.Engineer.Pos;
            var toCore = Dist(e.X, e.Y, cx, cy);
            foreach (var s in ctx.Sites.OfKind(SiteKind.Camp))
            {
                var c = s.Centre;
                if (Dist(e.X, e.Y, c.X, c.Y) < toCore) return true;
            }
            return false;
        }

        // ------------------------------------------------------------------ the deferral estimate

        /// <summary>
        /// PORT ADDITION (U-D-26, provisional with U-P-04). The earliest second the clock can currently PROVE the
        /// area might be safe again, so the deferred card can say "about N s". Re-derived every tick while deferred,
        /// so it converges rather than inventing a promise. Never earlier than now.
        /// </summary>
        public static double SafeAt(SimContext ctx, SimState st)
        {
            var d = st.Director;
            var r = ctx.Data.Raids;
            var until = st.T;
            if (d.RecoveryUntil > until) until = d.RecoveryUntil;
            if (d.Major != null) until = Math.Max(until, d.Major.EndsAt + r.RecoveryS);
            // A minor wave carries no end time; the director's own grace is the spacing it uses between waves.
            if (d.Minor != null) until = Math.Max(until, st.T + r.GraceS);
            if (d.Reserved) until = Math.Max(until, st.T + r.WarningS);
            // GP-W4: an assault lasts as long as its wave plan, not RaidTuning.WindowS. A scheduled one is covered
            // by the d.Major branch above; this is the one that is due but not yet announced, so the length can
            // only come from the tuning the plan will be built from.
            if (d.NextStart - st.T < r.WarningS)
                until = Math.Max(until, d.NextStart + SiegePlan.Length(ctx, ctx.Data.Siege.MajorWaves) + r.RecoveryS);
            return until;
        }

        public static double Dist(double ax, double ay, double bx, double by)
        {
            var dx = ax - bx;
            var dy = ay - by;
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
