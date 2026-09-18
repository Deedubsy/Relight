using System;

namespace Relight.Sim
{
    /// <summary>
    /// C-05's rules for the Home core: where it stands, what damages it and what a repair costs.
    /// Ported from reference campaignDefence.ts (<c>initDefence</c>'s <c>bases[0]</c>, <c>damageCore</c>,
    /// <c>coreDisabledAt</c>, <c>repairCheck</c>, <c>repairCost</c>, <c>manualRepairSeconds</c>).
    ///
    /// Differences from the reference, all forced by retired systems:
    /// * The reference keeps an array of <c>bases</c>, one per restored station; C-05's scope is the Home core alone,
    ///   so there is one record and no <c>block</c> key.
    /// * <c>damageCore</c> also turns the block circuit off (<c>st.blocks[...].subOn=false</c>) and writes a campaign
    ///   notice. The block/lattice economy is retired (U-D-32); the port raises <see cref="CoreDisabledEvent"/> and
    ///   <see cref="HomeQueries.CoreOperational"/> is what consumers read instead.
    /// * <c>manualRepairSeconds</c> multiplies by <c>DISCOVERY.repairMultiplier</c> once the discovery is recovered.
    ///   Discovery is not in Phase C; the multiplier is 1 and the branch is absent.
    /// </summary>
    public static partial class HomeCore
    {
        /// <summary>Reference <c>tickRepair</c>'s <c>r.remaining&gt;1e-8</c>.</summary>
        internal const double Eps = 1e-8;

        /// <summary>
        /// Puts the core down if it is not down yet, from <c>ctx.Sites.Core</c> (C-01's authored
        /// <c>home-workshop</c>, a region-local Y-down rect) or, on the synthetic map that has no sites, a 3x3
        /// centred on the engineer's spawn tile — which is where the authored core sits too (spawn (27,269) lies
        /// inside the workshop rect (22,256) 10x14). Never throws: a missing site is a fallback, not an error.
        /// Called by both <see cref="HomeCoreInitializer"/> (new game) and <see cref="HomeCorePhase"/> (a Phase B
        /// save upgraded to v3 arrives with <c>new HomeState()</c> and is placed on its first tick).
        /// </summary>
        public static void Ensure(SimContext ctx, SimState st)
        {
            var h = st.Home;
            if (h.Placed) return;
            var site = ctx.Sites?.Core;
            if (site != null)
            {
                h.X = site.X; h.Y = site.Y; h.W = site.W; h.H = site.H; h.Fallback = false;
            }
            else
            {
                var s = ctx.Geometry.Spawn;
                h.X = (int)Math.Floor(s.X) - 1; h.Y = (int)Math.Floor(s.Y) - 1; h.W = 3; h.H = 3;
                h.Fallback = true;
            }
            h.Hp = ctx.Data.Defence.CoreHp;
            h.CommissionedAt = st.T;
            h.DisabledAt = -1;
            h.Placed = true;
            st.Rev++;
        }

        /// <summary>
        /// Reference <c>damageCore</c>. The entry point Wave 2 W-B (enemies, C-08) calls; it needs no context, so an
        /// attack resolver can reach it from anywhere. A core already at 0 takes no further damage.
        /// </summary>
        public static void Damage(SimState st, double amount)
        {
            var h = st.Home;
            if (h == null || !h.Placed || h.Hp <= 0 || amount <= 0) return;
            h.Hp = Math.Max(0, h.Hp - amount);
            st.Events.Add(new CoreDamagedEvent(st.T, h.Hp));
            if (h.Hp > 0) return;
            h.DisabledAt = st.T;
            st.Rev++;   // reference bumps f.rev so presentation and routing see the disabled core
            st.Events.Add(new CoreDisabledEvent(st.T));
        }

        /// <summary>Reference <c>repairCost</c>: what a repair of this target would cost right now.</summary>
        public static RepairPrice Price(GameData d, SimState st, int kind, int id)
        {
            var t = d.Defence;
            if (kind == RepairKinds.Core)
            {
                var h = st.Home;
                var hp = h != null && h.Placed ? h.Hp : 0;
                var recommission = h != null && h.Placed && hp <= 0;
                return new RepairPrice(RepairKinds.Core, hp, t.CoreHp,
                    recommission ? t.CoreSteel : t.RepairSteel,
                    recommission ? t.CoreCopper : t.RepairCopper,
                    recommission ? t.CoreRepairSeconds : t.RepairSeconds, recommission);
            }
            double mhp = 0, mmax = 0;
            DefenceHp(d, st, id, ref mhp, ref mmax);
            return new RepairPrice(RepairKinds.Machine, mhp, mmax, t.RepairSteel, t.RepairCopper, t.RepairSeconds, false);
        }

        /// <summary>
        /// GP-W5: the refusal <see cref="RepairCommand"/> would give for this target right now, or "" when it would
        /// be accepted. This is the whole of <c>repairCheck</c>, lifted out of <see cref="HomeCoreHandler"/> so that
        /// the machine repair card (<see cref="HomeQueries.MachineRepairCard"/>) can grey its button with the sim's
        /// own reason rather than keeping a second copy of the rules that would quietly drift out of step. The tests
        /// are in the command's order, so the reason a card shows is the reason the command would give.
        /// </summary>
        public static string RepairProblem(SimContext ctx, SimState st, int kind, int id)
        {
            var h = st.Home;
            if (h == null) return "defence repair is unavailable";
            if (h.RepairKind != RepairKinds.None) return "already repairing";
            if (kind != RepairKinds.Core && kind != RepairKinds.Machine) return "nothing damaged here";

            var p = Price(ctx.Data, st, kind, id);
            double tx, ty, tw, th;
            if (kind == RepairKinds.Core)
            {
                if (!h.Placed) return "nothing damaged here";
                if (h.Hp >= p.Max) return "the core is already at full health";
                tx = h.X; ty = h.Y; tw = h.W; th = h.H;
            }
            else
            {
                var m = HomeCorePhase.MachineById(st, id);
                if (m == null || p.Max <= 0 || p.Hp >= p.Max) return "nothing damaged here";
                var (mw, mh) = m.Dimensions;
                tx = m.X; ty = m.Y; tw = mw; th = mh;
            }

            if (st.Engineer.IsDown || !Interaction.InReach(ctx, st, tx, ty, tw, th))
                return "walk closer to repair";
            if (kind == RepairKinds.Core && p.Recommission && Home.AttackersNearby(ctx, st))
                return "wait for the attackers to leave";

            var e = st.Engineer;
            if (e.Inv[ItemId.Steel] < p.Steel || e.Inv[ItemId.Copper] < p.Copper)
                return $"repairing needs {p.Steel} steel and {p.Copper} copper";
            return "";
        }

        /// <summary>Reference <c>coreAt</c>: is (x, y) a tile of the core's footprint?</summary>
        public static bool CoreAt(SimState st, int x, int y)
        {
            var h = st.Home;
            return h != null && h.Placed && x >= h.X && y >= h.Y && x < h.X + h.W && y < h.Y + h.H;
        }

        /// <summary>Reference <c>coreDisabledAt</c>, narrowed to the Home core.</summary>
        public static bool DisabledAt(SimState st, int x, int y) => CoreAt(st, x, y) && st.Home.Hp <= 0;

        /// <summary>
        /// Turret/wall HP lives in W-B's <c>TurretState</c> (C-04), which this folder must not touch. These two
        /// classic partial methods are the seam: with no implementation compiled in they leave <c>max</c> at 0, so
        /// <c>RepairCommand(machine, id)</c> refuses with "nothing damaged here" and only the core is repairable.
        /// W-B (or the coordinator) implements them in the turret folder — see the report.
        /// Reference equivalents: <c>defenceHp</c> / <c>defenceMax</c> and <c>tickRepair</c>'s machine branch.
        /// </summary>
        internal static void DefenceHp(GameData d, SimState st, int id, ref double hp, ref double max) => MachineDefenceHp(d, st, id, ref hp, ref max);
        internal static void HealDefence(SimContext ctx, SimState st, int id, double amount, ref bool applied) => MachineHealDefence(ctx, st, id, amount, ref applied);

        // Coordinator (2026-09-14): the seams carry the game data — a structure's max HP is a MachineSpec fact.
        static partial void MachineDefenceHp(GameData d, SimState st, int id, ref double hp, ref double max);
        static partial void MachineHealDefence(SimContext ctx, SimState st, int id, double amount, ref bool applied);
    }

    /// <summary>Reference <c>RepairCost</c>, minus <c>home</c> (there is only the Home core here).</summary>
    public readonly struct RepairPrice
    {
        public readonly int Kind;
        public readonly double Hp;
        public readonly double Max;
        public readonly int Steel;
        public readonly int Copper;
        public readonly double Seconds;
        public readonly bool Recommission;

        public RepairPrice(int kind, double hp, double max, int steel, int copper, double seconds, bool recommission)
        {
            Kind = kind; Hp = hp; Max = max; Steel = steel; Copper = copper; Seconds = seconds; Recommission = recommission;
        }
    }

    /// <summary>
    /// The small predicates the rest of the sim asks about the Home core. Kept apart from <see cref="HomeCore"/>
    /// so the hand-lock hook reads as one line at the call site.
    /// </summary>
    public static partial class Home
    {
        /// <summary>
        /// The one thing that pins the engineer at Home. U-D-44 retired the craft-batch lock this text once
        /// mirrored, so <c>HandCraft.LockTextFor</c> now simply returns this.
        /// </summary>
        public const string LockText = "Repairing — Cancel to move.";

        /// <summary>
        /// A repair in progress locks the hands. Since U-D-44 it is the ONLY thing that does:
        /// <c>HandCraft.HandLocked</c> delegates here, because a queued workshop batch is the workshop working,
        /// not the engineer, and the player is free to walk, mine, build and fight while it processes.
        /// </summary>
        public static bool RepairLocked(SimState st) => st.Home != null && st.Home.RepairKind != RepairKinds.None && !st.Engineer.IsDown;

        /// <summary>
        /// Reference <c>repairCheck</c>/<c>tickRepair</c> refuse and pause a disabled core's recommission while the
        /// block is under attack (<c>d.major?.block===core.block || d.minor?.block===core.block</c>). Enemies are
        /// W-B's C-08, which this folder must not reference, so this classic partial method is the seam: unimplemented
        /// it leaves <c>near</c> false and the rule is simply absent. C-09/W-B implements it — see the report.
        /// </summary>
        public static bool AttackersNearby(SimContext ctx, SimState st)
        {
            var near = false;
            AttackersNearbyHook(ctx, st, ref near);
            return near;
        }

        static partial void AttackersNearbyHook(SimContext ctx, SimState st, ref bool near);
    }
}
