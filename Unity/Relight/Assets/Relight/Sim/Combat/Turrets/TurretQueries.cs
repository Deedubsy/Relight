using System;

namespace Relight.Sim
{
    /// <summary>Everything the turret presenter and the machine panel need about one turret, in one read.</summary>
    public readonly struct TurretView
    {
        /// <summary>Cannon heading in radians, y-down.</summary>
        public double Angle { get; }
        /// <summary>The enemy it is tracking, 0 for none.</summary>
        public int Target { get; }
        public double Rounds { get; }
        public double Capacity { get; }
        public double Hp { get; }
        public double MaxHp { get; }
        /// <summary>0 when it may fire now, rising to 1 just after a shot.</summary>
        public double CooldownFraction { get; }
        /// <summary>Where the last round was aimed.</summary>
        public Vec2 LastShot { get; }
        /// <summary>Sim time of the last round; the muzzle flash lasts <c>TurretDef.ShotFlashS</c> from it.</summary>
        public double LastShotT { get; }
        public bool Flashing { get; }
        public double MuzzleTiles { get; }
        public double TurnSpeedRadPerS { get; }
        public bool Exists { get; }

        public TurretView(double angle, int target, double rounds, double capacity, double hp, double maxHp,
            double cooldownFraction, Vec2 lastShot, double lastShotT, bool flashing, double muzzle, double turn, bool exists)
        {
            Angle = angle; Target = target; Rounds = rounds; Capacity = capacity; Hp = hp; MaxHp = maxHp;
            CooldownFraction = cooldownFraction; LastShot = lastShot; LastShotT = lastShotT; Flashing = flashing;
            MuzzleTiles = muzzle; TurnSpeedRadPerS = turn; Exists = exists;
        }
    }

    /// <summary>Read-only turret questions for presentation, the HUD and C-09's opening encounter.</summary>
    public static class TurretQueries
    {
        /// <summary>Hit points left; the full value for a structure that has never been hit.</summary>
        public static double Hp(SimContext ctx, SimState st, int id) =>
            TurretRules.Hp(ctx.Data, st, st.MachineById(id));

        /// <summary>Full hit points, 0 for a machine that has no HP row.</summary>
        public static double Max(SimContext ctx, SimState st, int id) =>
            TurretRules.MaxHp(ctx.Data, st.MachineById(id));

        /// <summary>A structure whose hit points have run out: it no longer fires and is waiting to be repaired.</summary>
        public static bool Disabled(SimContext ctx, SimState st, int id)
        {
            var m = st.MachineById(id);
            return m != null && TurretRules.IsDefence(ctx.Data, m) && TurretRules.Hp(ctx.Data, st, m) <= 0;
        }

        /// <summary>
        /// Reference openingEncounter.ts <c>readyOpeningTurret</c>'s per-turret half: running (powered, undamaged)
        /// AND its hopper full. C-09 picks the Home turret it wants from these.
        /// </summary>
        public static bool Ready(SimContext ctx, SimState st, int id)
        {
            var m = st.MachineById(id);
            if (m == null || !ctx.Data.TryTurret(m.Kind, out var def)) return false;
            if (TurretRules.Hp(ctx.Data, st, m) <= 0) return false;
            if (def.PowerKw > 0 && !PowerQueries.Supplied(ctx, st, id)) return false;
            var cap = TurretHopper.Capacity(ctx.Data, m);
            return cap > 0 && m.Rounds >= cap;
        }

        /// <summary>Reference <c>liveTurrets</c>: turrets that are powered and not disabled, loaded or not.</summary>
        public static int Live(SimContext ctx, SimState st)
        {
            var n = 0;
            for (var i = 0; i < st.Machines.Count; i++)
            {
                var m = st.Machines[i];
                if (!ctx.Data.TryTurret(m.Kind, out var def)) continue;
                if (TurretRules.Hp(ctx.Data, st, m) <= 0) continue;
                if (def.PowerKw > 0 && !PowerQueries.Supplied(ctx, st, m.Id)) continue;
                n++;
            }
            return n;
        }

        /// <summary>Reference <c>turretFull</c>: turrets whose hopper is full (C-09's three-turret objective).</summary>
        public static int Loaded(SimContext ctx, SimState st)
        {
            var n = 0;
            for (var i = 0; i < st.Machines.Count; i++)
                if (Ready(ctx, st, st.Machines[i].Id)) n++;
            return n;
        }

        /// <summary>Aim, target, hopper, hit points, cooldown and last shot — the presenter's single read.</summary>
        public static TurretView View(SimContext ctx, SimState st, int id)
        {
            var m = st.MachineById(id);
            if (m == null || !ctx.Data.TryTurret(m.Kind, out var def))
                return new TurretView(0, 0, 0, 0, 0, 0, 0, Vec2.Zero, 0, false, 0, 0, false);
            var u = st.Turrets.Find(id);
            var angle = u != null && u.Aimed ? u.Angle : TurretRules.InitialAngle(m);
            var cycle = def.RoundsPerS > 0 ? 1 / def.RoundsPerS : 0;
            var cool = u == null ? 0 : Math.Max(0, u.Cool);
            return new TurretView(
                angle, u?.Target ?? 0, m.Rounds, TurretHopper.Capacity(ctx.Data, m),
                TurretRules.Hp(ctx.Data, st, m), TurretRules.MaxHp(ctx.Data, m),
                cycle > 0 ? Math.Min(1, cool / cycle) : 0,
                u?.Shot ?? Vec2.Zero, u?.ShotT ?? 0,
                u != null && u.Flash > 0, def.MuzzleTiles, def.TurnSpeedRadPerS, true);
        }

        /// <summary>Every turret on the map, in placement order, for the presenter's index.</summary>
        public static System.Collections.Generic.List<int> All(SimContext ctx, SimState st)
        {
            var ids = new System.Collections.Generic.List<int>();
            for (var i = 0; i < st.Machines.Count; i++)
                if (ctx.Data.TryTurret(st.Machines[i].Kind, out _)) ids.Add(st.Machines[i].Id);
            return ids;
        }
    }
}
