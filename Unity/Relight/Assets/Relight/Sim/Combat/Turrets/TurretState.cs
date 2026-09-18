using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>
    /// One defence machine's combat state, keyed by machine id (reference turretTracking.ts <c>TurretTracking</c>
    /// on <c>m.turret</c>, plus <c>m.cool</c>, <c>m.timer</c> and campaignDefence.ts's <c>m.hp</c>).
    ///
    /// SCHEMA. The reference hangs all of this on the machine itself; save schema v3 forbids new members inside
    /// <c>Machine</c> (SaveUpgrade fills missing members from a fresh state and never recurses into arrays), so it
    /// is a side table keyed by id, exactly like <see cref="MachineWork"/>.
    ///
    /// A fresh entry means "nothing has happened yet", which is why the stored field is <see cref="Damage"/> (hit
    /// points LOST) rather than the reference's <c>hp</c>: zero then reads as an undamaged structure, and a Phase B
    /// save upgraded to v3 comes back with every wall and turret intact. <see cref="Aimed"/> plays the same role for
    /// the cannon angle, whose reference default <c>(dir-1)·π/2</c> cannot be a C# field initialiser because it
    /// depends on the machine.
    ///
    /// The table covers EVERY structure with a <c>MachineSpec.Hp</c> row — turret, cannon, wall, barricade — because
    /// PowerNetwork.cs:66 records "<c>Machine.Hp</c> is C-04's" and enemies must be able to breach a wall.
    /// </summary>
    public sealed class TurretUnit : IVisitable
    {
        public int Id;
        /// <summary>Hit points lost (reference <c>defenceMax(m) - defenceHp(m)</c>); 0 is undamaged.</summary>
        public double Damage;
        /// <summary>Cannon heading in radians, y-down (reference <c>m.turret.angle</c>).</summary>
        public double Angle;
        /// <summary><see cref="Angle"/> has been seeded from the placement direction; false on a fresh entry.</summary>
        public bool Aimed;
        /// <summary>The enemy id it is tracking, 0 for none (reference <c>m.turret.target</c>).</summary>
        public int Target;
        /// <summary>Seconds until it may fire again (reference <c>m.cool</c>); may go to -dt, as the reference's does.</summary>
        public double Cool;
        /// <summary>Muzzle-flash seconds left (reference <c>m.timer</c> set to TURRET_SHOT_FLASH).</summary>
        public double Flash;
        /// <summary>Where the last round was aimed and when (reference <c>m.turret.shot</c>), for the presenter.</summary>
        public Vec2 Shot;
        public double ShotT;

        public void Visit(IStateVisitor v)
        {
            v.Field("id", ref Id);
            v.Field("damage", ref Damage);
            v.Field("angle", ref Angle);
            v.Field("aimed", ref Aimed);
            v.Field("target", ref Target);
            v.Field("cool", ref Cool);
            v.Field("flash", ref Flash);
            v.Field("shot", ref Shot);
            v.Field("shotT", ref ShotT);
        }
    }

    /// <summary>
    /// C-04's saved state, visited as <c>turrets</c>. A fresh instance means no structure has been damaged and no
    /// turret has aimed or fired — what a new campaign and an upgraded Phase B save both start from.
    /// </summary>
    public sealed class TurretState : IVisitable
    {
        /// <summary>Entries in machine-id order, so the canonical text is stable.</summary>
        public readonly List<TurretUnit> Units = new List<TurretUnit>();

        public void Visit(IStateVisitor v) => v.List("units", Units, () => new TurretUnit());

        /// <summary>The entry for a machine, created on first use (kept sorted by id).</summary>
        public TurretUnit Of(int id)
        {
            var lo = 0;
            var hi = Units.Count - 1;
            while (lo <= hi)
            {
                var mid = (lo + hi) / 2;
                if (Units[mid].Id == id) return Units[mid];
                if (Units[mid].Id < id) lo = mid + 1; else hi = mid - 1;
            }
            var e = new TurretUnit { Id = id };
            Units.Insert(lo, e);
            return e;
        }

        /// <summary>The entry for a machine if one exists, without creating it (queries must not mutate).</summary>
        public TurretUnit Find(int id)
        {
            var lo = 0;
            var hi = Units.Count - 1;
            while (lo <= hi)
            {
                var mid = (lo + hi) / 2;
                if (Units[mid].Id == id) return Units[mid];
                if (Units[mid].Id < id) lo = mid + 1; else hi = mid - 1;
            }
            return null;
        }

        /// <summary>Drops entries whose machine has been removed.</summary>
        public void Prune(SimState st)
        {
            for (var i = Units.Count - 1; i >= 0; i--)
                if (st.MachineById(Units[i].Id) == null) Units.RemoveAt(i);
        }
    }

    public sealed partial class SimState
    {
        /// <summary>Turret aim, cooldown and structure damage (C-04), visited as <c>turrets</c>.</summary>
        public TurretState Turrets = new TurretState();

        partial void VisitTurrets(IStateVisitor v) => v.Object("turrets", ref Turrets, () => new TurretState());
    }
}
