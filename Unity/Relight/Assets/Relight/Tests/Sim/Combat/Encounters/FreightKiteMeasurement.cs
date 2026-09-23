using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Relight.Sim.Tests.Combat
{
    /// <summary>
    /// Batch 4, FRT-11 (REL-146): design §9 item 11, the kite measurement. A <b>measurement, not an assertion</b>:
    /// a scripted engineer shoots from 18 tiles and walks away from anything closer, and the run reports kills per
    /// minute and whether the occupation rule blocks the claim. Nothing here passes or fails on a number; the only
    /// asserts are that the script ran at all. It is <c>[Explicit]</c>, so no suite runs it; the printed report is
    /// kept in <c>Unity/Docs/evidence/freight-kite/</c>.
    ///
    /// The map is <see cref="RaidFixture"/>'s 160-tile open square with EncounterPhaseTests' West passage layout:
    /// the marker at (130, 130) and its four groups. The tick order is the game's own
    /// (<see cref="SimComposition.Phases"/>) without the director and the opening, so no raid or tutorial wave
    /// walks into the measurement, and there are no turrets. The engineer only walks; nobody sprints or dodges.
    /// </summary>
    public sealed class FreightKiteMeasurement
    {
        const string Camp = "freight:camp:1";
        const string Arena = "freight:arena";
        const int Mx = 130, My = 130;
        const double Keep = 18;                   // design §9: "shooting from 18 tiles"
        static double _keep = Keep;               // 0 for the stand-and-shoot contrast run
        const double Minutes = 10;                // the longest a run is allowed before it is called off
        static readonly (int X, int Y)[] Groups = { (118, 126), (132, 124), (118, 138), (138, 136) };

        static SimContext Context()
        {
            var sites = new List<SiteRecord>
            {
                new SiteRecord("home", "Home Court", SiteKind.Core, RaidFixture.CoreX, RaidFixture.CoreY,
                    RaidFixture.CoreSize, RaidFixture.CoreSize),
                new SiteRecord(Camp, "West passage camp", SiteKind.Camp, Mx, My, 1, 1, "", 20),
            };
            for (var i = 0; i < Groups.Length; i++)
                sites.Add(new SiteRecord(Camp + ":group:" + i, "group", SiteKind.Camp, Groups[i].X - 4, Groups[i].Y - 4, 9, 9));
            return new SimContext(ReferenceData.Create(), RaidFixture.Map(), null, null, new WorldSites(sites));
        }

        static SimState State(SimContext ctx, int seed, double x, double y)
        {
            var st = RaidFixture.State(ctx, seed);
            new WeaponInitializer().Init(ctx, st);
            WeaponRules.Create(st, "rifle");
            st.Weapons.Slot0 = st.Weapons.Owned[0].Id;
            st.Engineer.Inv[ItemId.Magazine] += 2000;
            if (st.Engineer.Hp <= 0) st.Engineer.Hp = ctx.Data.Engineer.MaxHp;
            st.Engineer.Pos = new Vec2(x, y);
            return st;
        }

        static List<ITickPhase> Phases() => SimComposition.Phases
            .Where(p => !(p is DirectorPhase) && !(p is OpeningPhase)).ToList();

        static CommandResult Apply(SimContext ctx, SimState st, Command c)
        {
            foreach (var h in SimComposition.Handlers)
                if (h.TryApply(ctx, st, c, out var r)) return r;
            return CommandResult.Refuse("no handler");
        }

        static double Dist(Vec2 a, Vec2 b) => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));

        sealed class Run
        {
            public string Name = "";
            public double Start;
            public double FoundAt = -1;
            public int Born;
            public readonly List<double> Kills = new List<double>();
            public double LastKillAt = -1;
            public double ClaimedAt = -1;
            public double HoldStartedAt = -1;
            public int HoldResets;
            public double DownAt = -1;
            public double MinHp = double.MaxValue;
            public double Damage;
            public long Fired;
            public double Retreating;
            public double Shooting;
            public double FarthestGuard;
            public int Charges;
            public double EndedAt;
            public string Ended = "";
        }

        /// <summary>
        /// The kiter, one tick: fire at the nearest living body of <paramref name="site"/> in the rifle's reach, walk
        /// directly away while it is nearer than 18, stand still at 18 or more, walk towards it when it is out of
        /// reach. With nobody left (or <paramref name="goHome"/> true) it walks to <paramref name="hold"/> and stands.
        /// </summary>
        static void Step(SimContext ctx, SimState st, Run run, string site, Vec2 hold, bool goHome, double reach)
        {
            var e = st.Engineer;
            var w = st.Weapons.ActiveWeapon();
            if (w != null && w.Loaded < 1 && w.Reload <= 0) Apply(ctx, st, new ReloadCommand());

            Enemy near = null;
            var nd = double.MaxValue;
            foreach (var b in st.Enemies.Actors)
            {
                if (b.Site != site || b.Hp <= 0) continue;
                var d = Dist(b.Pos, e.Pos);
                if (d < nd) { nd = d; near = b; }
            }

            if (near == null || goHome)
            {
                Apply(ctx, st, new HoldFireCommand());
                if (Dist(e.Pos, hold) > 0.4) Apply(ctx, st, new MoveCommand(hold.X, hold.Y));
                else Apply(ctx, st, new WalkCommand(0, 0));
                return;
            }

            if (nd <= reach) { Apply(ctx, st, new FireCommand(near.Pos.X, near.Pos.Y)); run.Shooting += RaidFixture.Dt; }
            else Apply(ctx, st, new HoldFireCommand());

            if (nd < _keep)
            {
                var dx = (e.Pos.X - near.Pos.X) / Math.Max(nd, 1e-6);
                var dy = (e.Pos.Y - near.Pos.Y) / Math.Max(nd, 1e-6);
                // Slide along the map's edge rather than into it.
                if ((e.Pos.X < 4 && dx < 0) || (e.Pos.X > 156 && dx > 0)) dx = 0;
                if ((e.Pos.Y < 4 && dy < 0) || (e.Pos.Y > 156 && dy > 0)) dy = 0;
                if (Math.Abs(dx) + Math.Abs(dy) < 1e-6) { dx = e.Pos.X < 80 ? 1 : -1; }
                Apply(ctx, st, new WalkCommand(dx, dy));
                run.Retreating += RaidFixture.Dt;
            }
            else if (nd > reach) Apply(ctx, st, new MoveCommand(near.Pos.X, near.Pos.Y));
            else Apply(ctx, st, new WalkCommand(0, 0));
        }

        static void Observe(SimState st, Run run, string site, Vec2 marker, ref int alive, HashSet<int> charging)
        {
            var now = st.Enemies.Actors.Where(b => b.Site == site && b.Hp > 0).ToList();
            if (now.Count > alive) { run.Born += now.Count - alive; if (run.FoundAt < 0) run.FoundAt = st.T - run.Start; }
            else if (now.Count < alive) for (var i = 0; i < alive - now.Count; i++) run.Kills.Add(st.T - run.Start);
            if (now.Count < alive && now.Count == 0) run.LastKillAt = st.T - run.Start;
            alive = now.Count;
            foreach (var b in now)
            {
                run.FarthestGuard = Math.Max(run.FarthestGuard, Dist(b.Pos, marker));
                if (b.Phase == EnemyPhaseKind.Charge) { if (charging.Add(b.Id)) run.Charges++; }
                else charging.Remove(b.Id);
            }
            run.MinHp = Math.Min(run.MinHp, st.Engineer.Hp);
        }

        static Run Kite(string name, int seed, double sx, double sy, double giveUpAfter = double.MaxValue)
        {
            var ctx = Context();
            var st = State(ctx, seed, sx, sy);
            var phases = Phases();
            var run = new Run { Name = name, Start = st.T };
            var marker = new Vec2(Mx + .5, My + .5);
            ctx.Data.TryWeapon(st.Weapons.ActiveWeapon().Kind, out var rifle);
            var alive = 0;
            var charging = new HashSet<int>();
            var hp = st.Engineer.Hp;
            var firedAt0 = st.Stats.EngineerFired;
            var ticks = (int)(Minutes * 60 / RaidFixture.Dt);
            var rec = (EncounterRecord)null;
            for (var i = 0; i < ticks; i++)
            {
                var t = st.T - run.Start;
                var goHome = run.FoundAt >= 0 && t - run.FoundAt >= giveUpAfter;
                Step(ctx, st, run, Camp, marker, goHome, rifle.MaxTiles);
                for (var p = 0; p < phases.Count; p++) phases[p].Tick(ctx, st, RaidFixture.Dt);
                st.Tick++;
                st.T += RaidFixture.Dt;
                if (st.Engineer.Hp < hp) run.Damage += hp - st.Engineer.Hp;
                hp = st.Engineer.Hp;
                Observe(st, run, Camp, marker, ref alive, charging);
                rec = st.Encounters.Find(Camp);
                if (rec != null && rec.OccupiedSince >= 0 && run.HoldStartedAt < 0) run.HoldStartedAt = rec.OccupiedSince - run.Start;
                if (rec != null && rec.OccupiedSince < 0 && run.HoldStartedAt >= 0 && !rec.Claimed) { run.HoldResets++; run.HoldStartedAt = -1; }
                if (st.Engineer.IsDown) { run.DownAt = st.T - run.Start; run.Ended = "the engineer went down"; break; }
                if (rec != null && rec.Claimed) { run.ClaimedAt = st.T - run.Start; run.Ended = "claimed"; break; }
            }
            if (run.Ended.Length == 0) run.Ended = "called off at " + Minutes + " min";
            run.EndedAt = st.T - run.Start;
            run.Fired = st.Stats.EngineerFired - firedAt0;
            return run;
        }

        static Run Guardian(int seed, double gap, bool kite = true)
        {
            _keep = kite ? Keep : 0;
            var ctx = RaidFixture.Context();
            var st = State(ctx, seed, Mx + .5, My + .5 - gap);
            var g = RaidFixture.Body(st, GuardianRules.Kind, Mx + .5, My + .5, EnemyLayer.Site, EncounterCatalogue.GroupBase + 99);
            g.Site = Arena;
            ctx.Data.TryEnemy(GuardianRules.Kind, out var def);
            g.Hp = def.Hp;
            var phases = Phases();
            var run = new Run { Name = "Guardian, alone on open ground, engineer starts " + F(gap) + " tiles off, "
                + (kite ? "kiting at 18" : "standing still and shooting (contrast)"), Start = st.T };
            var spot = new Vec2(Mx + .5, My + .5);
            ctx.Data.TryWeapon(st.Weapons.ActiveWeapon().Kind, out var rifle);
            var alive = 1;
            var charging = new HashSet<int>();
            var hp = st.Engineer.Hp;
            var firedAt0 = st.Stats.EngineerFired;
            run.FoundAt = 0;
            run.Born = 1;
            var ticks = (int)(Minutes * 60 / RaidFixture.Dt);
            for (var i = 0; i < ticks; i++)
            {
                Step(ctx, st, run, Arena, st.Engineer.Pos, false, rifle.MaxTiles);
                for (var p = 0; p < phases.Count; p++) phases[p].Tick(ctx, st, RaidFixture.Dt);
                st.Tick++;
                st.T += RaidFixture.Dt;
                if (st.Engineer.Hp < hp) run.Damage += hp - st.Engineer.Hp;
                hp = st.Engineer.Hp;
                Observe(st, run, Arena, spot, ref alive, charging);
                if (st.Engineer.IsDown) { run.DownAt = st.T - run.Start; run.Ended = "the engineer went down"; break; }
                if (alive == 0) { run.Ended = "the guardian fell"; break; }
            }
            if (run.Ended.Length == 0) run.Ended = "called off at " + Minutes + " min";
            run.EndedAt = st.T - run.Start;
            run.Fired = st.Stats.EngineerFired - firedAt0;
            _keep = Keep;
            return run;
        }

        static string F(double v) => v.ToString("0.#", CultureInfo.InvariantCulture);

        static string Report(Run r)
        {
            var sb = new StringBuilder();
            sb.AppendLine("## " + r.Name);
            sb.AppendLine("- ended: " + r.Ended + " at " + F(r.EndedAt) + " s");
            sb.AppendLine("- garrison born: " + r.Born + (r.FoundAt >= 0 ? " at " + F(r.FoundAt) + " s" : " (never found)"));
            var fightS = (r.LastKillAt >= 0 ? r.LastKillAt : r.EndedAt) - Math.Max(0, r.FoundAt);
            var kpm = fightS > 0 ? r.Kills.Count / (fightS / 60) : 0;
            sb.AppendLine("- kills: " + r.Kills.Count + " of " + r.Born + " in " + F(fightS) + " s of fighting = "
                + kpm.ToString("0.0", CultureInfo.InvariantCulture) + " kills per minute");
            if (r.Kills.Count > 0)
                sb.AppendLine("- kill times (s): " + string.Join(", ", r.Kills.Select(F)));
            sb.AppendLine("- rounds fired: " + r.Fired + (r.Kills.Count > 0 ? " (" + F((double)r.Fired / r.Kills.Count) + " per kill)" : ""));
            sb.AppendLine("- time shooting: " + F(r.Shooting) + " s; time walking away: " + F(r.Retreating) + " s");
            sb.AppendLine("- engineer damage taken: " + F(r.Damage) + "; lowest hit points: " + F(r.MinHp)
                + (r.DownAt >= 0 ? "; DOWN at " + F(r.DownAt) + " s" : "; never down"));
            sb.AppendLine("- farthest a living body got from its post: " + F(r.FarthestGuard) + " tiles");
            if (r.Charges > 0) sb.AppendLine("- guardian charges begun: " + r.Charges);
            if (r.Name.StartsWith("West", StringComparison.Ordinal))
                sb.AppendLine("- hold clock: " + (r.ClaimedAt >= 0 ? "claimed at " + F(r.ClaimedAt) + " s" : "never claimed")
                    + "; clock resets: " + r.HoldResets);
            return sb.ToString();
        }

        [Test, Explicit("FRT-11 measurement; run by hand, the report is evidence, not a result")]
        public void KiteMeasurement()
        {
            var ctx = Context();
            var d = ctx.Data;
            d.TryWeapon("rifle", out var rifle);
            d.TryEnemy("skitter", out var skitter);
            d.TryEnemy("spitter", out var spitter);
            d.TryEnemy(GuardianRules.Kind, out var guardian);
            var sb = new StringBuilder();
            sb.AppendLine("# FRT-11 kite measurement (design §9 item 11)");
            sb.AppendLine();
            sb.AppendLine("Inputs read from the game data: rifle effective " + F(rifle.EffectiveTiles) + " / max " + F(rifle.MaxTiles)
                + " tiles, " + F(rifle.Damage) + " damage, " + F(rifle.RatePerS) + " rounds/s, magazine " + rifle.Capacity
                + ", reload " + F(rifle.ReloadSeconds) + " s; engineer walk " + F(d.Engineer.WalkTilesPerS) + " tiles/s, "
                + F(d.Engineer.MaxHp) + " HP; skitter " + F(skitter.Hp) + " HP at " + F(skitter.SpeedTilesPerS)
                + " tiles/s; spitter " + F(spitter.Hp) + " HP at " + F(spitter.SpeedTilesPerS) + " tiles/s, range "
                + F(spitter.RangeTiles) + "; guardian " + F(guardian.Hp) + " HP at " + F(guardian.SpeedTilesPerS) + " tiles/s.");
            sb.AppendLine();

            // The seed does not reach the encounter (the garrison's places are dealt from the catalogue, not the
            // PRNG), so the three kite runs differ by the side the engineer walks in from instead.
            var runs = new List<Run>
            {
                Kite("West passage, walk in from the south, kite to the last kill", 7, Mx + .5, My + .5 - 45),
                Kite("West passage, walk in from the west, kite to the last kill", 7, Mx + .5 - 45, My + .5),
                Kite("West passage, walk in from the south-west, kite to the last kill", 7, Mx + .5 - 32, My + .5 - 32),
                Kite("West passage, kite 8 s then walk to the marker and stand", 7, Mx + .5, My + .5 - 45, 8),
                Kite("West passage, never shoot: walk to the marker and stand", 7, Mx + .5, My + .5 - 45, 0),
                Guardian(7, Keep),
                Guardian(7, Keep, false),
            };
            foreach (var r in runs) sb.AppendLine(Report(r));
            TestContext.Out.WriteLine(sb.ToString());
            Assert.That(runs.All(r => r.EndedAt > 0), Is.True, "every run ticked");
        }
    }
}
