using System;

namespace Relight.Sim
{
    /// <summary>
    /// Structure hit points and turret aiming, ported from reference campaignDefence.ts (<c>defenceMax</c>,
    /// <c>defenceHp</c>, <c>damageDefence</c>, the repair half of <c>tickRepair</c>) and turretTracking.ts
    /// (<c>angleDifference</c>, <c>aimTurret</c>).
    ///
    /// DIFFERENCE FROM THE REFERENCE. <c>DEFENCE.barricadeHp/wallHp/turretHp</c> are hard-coded constants there;
    /// here every value comes from <c>MachineSpec.Hp</c> (turret 100, cannon 140, wall 120, barricade 240 —
    /// the same numbers), so tuning stays in the catalogue (U-M-12).
    /// </summary>
    public static class TurretRules
    {
        /// <summary>Reference <c>defenceMax(m)</c>: a structure's full hit points, 0 for a machine that has none.</summary>
        public static double MaxHp(GameData d, Machine m) =>
            m != null && d.TryMachine(m.Kind, out var spec) ? spec.Hp : 0;

        /// <summary>
        /// A machine that can be damaged and repaired at all (reference <c>defenceMax(m) &gt; 0</c>).
        ///
        /// GP-W5 WIDENED WHAT THIS ANSWERS TRUE FOR, without touching the predicate. The reference gave hit points
        /// to walls, barricades, turrets and cannons only, so every other machine answered false — and because the
        /// raid rules ask this question to decide what may be breached, a line of ordinary supply chests was an
        /// invulnerable wall no wave could pass or break. <see cref="CombatBalance"/> now derives an integrity row
        /// for every BUILDABLE machine, so this reads "a machine that can be broken", which is what each caller
        /// already meant. The reference's own name is kept.
        /// </summary>
        public static bool IsDefence(GameData d, Machine m) => MaxHp(d, m) > 0;

        /// <summary>
        /// U-D-59, ALWAYS_DARK_SPEC.md §5.7. How far a turret reaches into UNLIT ground: its
        /// <see cref="TurretDef.DarkSightTiles"/>, never more than its range, and its whole range when the row
        /// carries no dark sight (0), which switches the rule off for that turret.
        /// </summary>
        public static double DarkSight(TurretDef def)
        {
            if (def == null) return 0;
            return def.DarkSightTiles > 0 ? Math.Min(def.DarkSightTiles, def.RangeTiles) : def.RangeTiles;
        }

        /// <summary>
        /// How far a turret reaches a body standing at (x, y). The tile the BODY stands on decides, not the tile
        /// the turret stands on: a turret in the dark shoots an alien under a lamp at full range, and a turret
        /// under a lamp is still short-sighted into the dark beyond it. Lit is <see cref="LightQueries.LitAt(SimState,int,int)"/>,
        /// the sim's mask; the flashlight never counts (D-UI-11).
        /// </summary>
        public static double Reach(SimState st, TurretDef def, double x, double y)
        {
            if (def == null) return 0;
            if (def.DarkSightTiles <= 0 || def.DarkSightTiles >= def.RangeTiles) return def.RangeTiles;
            return LightQueries.LitAt(st, (int)Math.Floor(x), (int)Math.Floor(y)) ? def.RangeTiles : def.DarkSightTiles;
        }

        /// <summary>
        /// GP-W5: the machine is a FORTIFICATION — a wall, a barricade, a gun turret or a cannon.
        ///
        /// This is the NARROW question <see cref="IsDefence"/> used to answer by accident, and the two callers that
        /// really meant it keep it: the director's staging, which wants an approach that does not open on top of the
        /// player's guns, and anything else asking "is this position defended" rather than "can this be broken".
        /// Widening <see cref="IsDefence"/> to every buildable machine would otherwise have made a supply chest
        /// count as a defended position and pushed every approach out to the worst tier.
        /// </summary>
        public static bool IsFortification(GameData d, Machine m) =>
            m != null && (Sightline.Opaque(m.Kind) || TurretHopper.IsTurret(d, m));

        /// <summary>
        /// GP-W5: the machine has been damaged past its integrity and is a WRECK — it stands where it stood, it is
        /// still the player's, and <see cref="TurretRepairHook"/> can bring it back, but until then it does nothing
        /// and nothing walks around it. This is the single gate the phases read
        /// (<see cref="PowerQueries.Throttle"/> returns 0 for it, so miners, processors, inserters and turrets all
        /// stop without a wreck test of their own) and the single reason <see cref="GroundState.SolidMap"/> lets a
        /// body through, which is the reference's own <c>walk.ts:37</c> rule applied to the wider roster.
        /// </summary>
        public static bool Wrecked(GameData d, SimState st, Machine m) =>
            m != null && MaxHp(d, m) > 0 && Hp(d, st, m) <= 0;

        /// <summary>Reference <c>defenceHp(m)</c>: what is left. An undamaged structure has never had an entry made.</summary>
        public static double Hp(GameData d, SimState st, Machine m)
        {
            if (m == null) return 0;
            var max = MaxHp(d, m);
            if (max <= 0) return 0;
            var u = st.Turrets.Find(m.Id);
            return u == null ? max : Math.Max(0, max - u.Damage);
        }

        /// <summary>
        /// Reference <c>damageDefence</c>: take hit points off, clamp at zero and bump <c>rev</c> when the structure
        /// goes down, because a dead wall stops blocking and every cached path must be rebuilt.
        /// Returns true when this call is the one that disabled it.
        /// </summary>
        public static bool Damage(SimContext ctx, SimState st, Machine m, double amount)
        {
            if (m == null || amount <= 0) return false;
            var max = MaxHp(ctx.Data, m);
            if (max <= 0) return false;
            var u = st.Turrets.Of(m.Id);
            var was = Math.Max(0, max - u.Damage);
            if (was <= 0) return false;
            u.Damage = Math.Min(max, u.Damage + amount);
            u.HitAt = st.T;
            var now = Math.Max(0, max - u.Damage);
            st.Events.Add(new StructureDamagedEvent(st.T, m.Id, now, max));
            if (now > 0) return false;
            u.Target = 0;
            u.Cool = 0;
            st.Rev++;
            return true;
        }

        /// <summary>
        /// The repair hook W-A's <c>RepairCommand</c> calls (reference <c>tickRepair</c>'s
        /// <c>m.hp = Math.min(defenceMax(m), defenceHp(m) + DEFENCE.repairHp)</c>). Returns the hit points actually
        /// restored, so the caller can charge exactly what it healed and refuse when nothing is missing.
        /// </summary>
        public static double TurretRepairHook(SimContext ctx, SimState st, int id, double hp)
        {
            var m = st.MachineById(id);
            if (m == null || hp <= 0) return 0;
            var max = MaxHp(ctx.Data, m);
            if (max <= 0) return 0;
            var u = st.Turrets.Find(id);
            if (u == null || u.Damage <= 0) return 0;
            var healed = Math.Min(hp, u.Damage);
            var wasDown = u.Damage >= max;
            u.Damage -= healed;
            if (wasDown) st.Rev++;                 // it blocks and shoots again: rebuild the cached fields
            st.Events.Add(new StructureRepairedEvent(st.T, id, Math.Max(0, max - u.Damage), max));
            return healed;
        }

        /// <summary>Reference turretTracking.ts:11 <c>angleDifference</c>: the shortest signed turn from one heading to another.</summary>
        public static double AngleDifference(double to, double from) =>
            Math.Atan2(Math.Sin(to - from), Math.Cos(to - from));

        /// <summary>Reference <c>(m.dir - 1) * Math.PI / 2</c>: the heading a freshly placed turret starts at.</summary>
        public static double InitialAngle(Machine m) => ((int)m.Dir - 1) * Math.PI / 2;

        /// <summary>
        /// Reference <c>aimTurret</c>: rotate the cannon the short way towards a point, at most
        /// <c>TurnSpeedRadPerS · dt</c>, and report whether it is now on target. The base's placement direction
        /// never changes; only <see cref="TurretUnit.Angle"/> moves.
        /// </summary>
        public static bool Aim(TurretUnit u, Machine m, TurretDef def, double x, double y, double dt)
        {
            var desired = Math.Atan2(y - m.Y - m.Size / 2.0, x - m.X - m.Size / 2.0);
            var delta = AngleDifference(desired, u.Angle);
            var step = Math.Min(Math.Abs(delta), def.TurnSpeedRadPerS * dt);
            var next = u.Angle + Math.Sign(delta) * step;
            u.Angle = Math.Atan2(Math.Sin(next), Math.Cos(next));
            return Math.Abs(AngleDifference(desired, u.Angle)) < 1e-6;
        }
    }

    /// <summary>
    /// The ammunition hopper seam W-A's flow (inserters, belts) and W-B's own
    /// <see cref="MachineTransferCommand"/> feed a turret through.
    ///
    /// The rounds live on the pre-existing <c>Machine.Rounds</c> field, NOT in a duplicate side-table counter:
    /// it is already visited, already what <see cref="MachineInventory.Contents"/> and
    /// <see cref="MachineInventory.Accepts"/> read, and already what <see cref="Ledger"/> counts as held magazines.
    /// A second copy would double-count in the conservation ledger.
    ///
    /// NEITHER METHOD CREATES OR DESTROYS ITEMS. <see cref="Give"/> only moves rounds in; the caller must remove the
    /// same number from wherever they came from, exactly as <see cref="MachineInventory.Transfer"/> does.
    /// </summary>
    public static class TurretHopper
    {
        private const double Eps = 1e-9;

        /// <summary>Hopper capacity in rounds, from <c>TurretDef.Hopper</c> (turret 50, cannon 20).</summary>
        public static int Capacity(GameData d, Machine m) => m != null ? Capacity(d, m.Kind) : 0;

        /// <summary>The same capacity for a kind, before one is built ("A turret holds 50").</summary>
        public static int Capacity(GameData d, string kind) =>
            d.TryTurret(kind, out var def) && def.Hopper > 0 ? def.Hopper
            : d.TryMachine(kind, out var spec) && spec.AmmoCap > 0 ? spec.AmmoCap : 0;

        /// <summary>
        /// "9 / 50 rounds": a turret's load as every surface prints it (REL-10). The hover, the world card, the
        /// turret's panel and the opening objectives all show this string, so they can never disagree.
        /// </summary>
        public static string RoundsText(GameData d, Machine m) =>
            (m != null ? m.Rounds : 0) + " / " + Capacity(d, m) + " rounds";

        /// <summary>The round a kind eats: <c>TurretDef.Ammo</c> (turret bullets, cannon shells).</summary>
        public static ItemId Ammo(GameData d, Machine m) =>
            m != null && d.TryTurret(m.Kind, out var def) ? def.Ammo : ItemId.Magazine;

        /// <summary>True when this machine has a hopper at all.</summary>
        public static bool IsTurret(GameData d, Machine m) => m != null && d.TryTurret(m.Kind, out _);

        /// <summary>
        /// Would this turret take one more of <paramref name="item"/>? Same answer as
        /// <see cref="MachineInventory.Accepts(GameData, Machine, ItemId)"/> for the turret kind, exposed here so
        /// W-A's flow does not have to know where the hopper lives.
        /// </summary>
        public static bool Accepts(SimContext ctx, SimState st, int id, ItemId item)
        {
            var m = st.MachineById(id);
            if (!IsTurret(ctx.Data, m) || item != Ammo(ctx.Data, m)) return false;
            var cap = Capacity(ctx.Data, m);
            return cap > 0 && m.Rounds + 1 <= cap + Eps;
        }

        /// <summary>How many more rounds would fit right now.</summary>
        public static int Room(SimContext ctx, SimState st, int id)
        {
            var m = st.MachineById(id);
            if (!IsTurret(ctx.Data, m)) return 0;
            return Math.Max(0, Capacity(ctx.Data, m) - m.Rounds);
        }

        /// <summary>
        /// Put up to <paramref name="n"/> rounds in and return how many went. The caller keeps the remainder.
        /// Counts them in <c>Stats.TurretFed</c> (reference flow.ts <c>stats.turretFed</c>), which is a plain
        /// counter and not a ledger source or sink, so conservation is unaffected.
        /// </summary>
        public static int Give(SimContext ctx, SimState st, int id, ItemId item, int n)
        {
            if (n <= 0) return 0;
            var m = st.MachineById(id);
            if (!IsTurret(ctx.Data, m) || item != Ammo(ctx.Data, m)) return 0;
            var took = Math.Min(n, Room(ctx, st, id));
            if (took <= 0) return 0;
            m.Rounds += took;
            st.Stats.TurretFed += took;
            return took;
        }

        /// <summary>The refusal W-A and the machine panel show when it will not take another round.</summary>
        public const string FullProblem = "the turret hopper is full";
    }
}
