using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>
    /// U-D-58: the port is always dark. <c>Sim/Data/Generated/CatalogueData.g.cs</c> is generated from the
    /// TypeScript reference, which has a sun (<c>daylightSeconds</c> 900), so the port's own value lives in this
    /// layer, applied after <see cref="CombatBalance"/> in both data paths (<see cref="ReferenceData.Create"/> and
    /// the Unity <c>GameDataRegistry.Build</c>). Zero daylight seconds means there is no sun:
    /// <see cref="LightQueries.Daylight(double,double,double)"/> answers dark at every time.
    ///
    /// U-D-59 (2026-09-20): the same layer gives each turret its DARK SIGHT, how far it reaches an alien standing
    /// on an unlit tile (ALWAYS_DARK_SPEC.md §5.7). The reference has no such value, so a turret row that arrives
    /// without one takes it from <see cref="DarkSight"/>; a row that already carries one is left alone.
    /// Rules: Unity/Docs/ALWAYS_DARK_SPEC.md.
    /// </summary>
    public static class DarkWorld
    {
        /// <summary>
        /// Gun turret: 6 of its 9 tiles, deliberately just under the Spitter's 7 so an unlit turret can be shelled
        /// by a Spitter it cannot answer (the owner: "Yes, slightly"). Provisional, U-P-11.
        /// </summary>
        public const double GunTurretDarkSightTiles = 6;

        /// <summary>
        /// Cannon: 8 of its 12 tiles, the Gun turret's two-thirds carried over. Provisional, U-P-11; the owner has
        /// only named the Gun turret's value, so this one is the implementer's under U-D-28.
        /// </summary>
        public const double CannonDarkSightTiles = 8;

        /// <summary>The dark sight this layer gives a turret kind, or 0 for a kind it has no value for.</summary>
        public static double DarkSight(string turretKey)
        {
            switch (turretKey)
            {
                case "turret": return GunTurretDarkSightTiles;
                case "cannon": return CannonDarkSightTiles;
                default: return 0;
            }
        }

        public static GameData Apply(GameData d)
        {
            if (d == null) return d;

            var time = d.Time;
            var changed = false;
            if (time != null && time.DaylightSeconds != 0) { time = time with { DaylightSeconds = 0 }; changed = true; }

            var turrets = d.Turrets;
            if (turrets != null)
            {
                List<TurretDef> copy = null;
                for (var i = 0; i < turrets.Count; i++)
                {
                    var t = turrets[i];
                    if (t == null || t.DarkSightTiles > 0) continue;
                    var sight = DarkSight(t.Key);
                    if (sight <= 0) continue;
                    if (copy == null) copy = new List<TurretDef>(turrets);
                    copy[i] = t with { DarkSightTiles = System.Math.Min(sight, t.RangeTiles) };
                }
                if (copy != null) { turrets = copy; changed = true; }
            }

            if (!changed) return d;
            return new GameData(d.Items, d.Machines, d.Recipes, d.Engineer, d.World, d.Weapons, d.Enemies,
                d.Ammunition, turrets, d.Power, time, d.Raids, d.Opening, d.Stake, d.Defence, d.Siege);
        }
    }
}
