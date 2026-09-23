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
    ///
    /// U-D-71 / REL-126 (2026-09-23): and it sets HOW FAR A PLACED LIGHT REACHES. The reference's radii are a
    /// daylit world's — the port is never daylit, so a quarter more reach is a port value, not a reference one,
    /// and this is the layer that already owns the difference.
    ///
    /// Rules: Unity/Docs/ALWAYS_DARK_SPEC.md.
    /// </summary>
    public static class DarkWorld
    {
        /// <summary>
        /// REL-126, the owner's "I think the lighting radius should be a little bigger": every placed light reaches
        /// a quarter further than the reference's row. **The quarter is the implementer's figure under U-D-28
        /// (U-P-29), not the owner's** — they asked for "a little", and this is what a little was read as.
        /// </summary>
        public const double LightRadiusScale = 1.25;

        /// <summary>Lamp: the reference's 4 tiles at <see cref="LightRadiusScale"/>.</summary>
        public const double LampRadiusTiles = 5;

        /// <summary>Arc lamp: the reference's 6 tiles at <see cref="LightRadiusScale"/>.</summary>
        public const double ArcLampRadiusTiles = 7.5;

        /// <summary>Floodlight cone: the reference's 12 tiles at <see cref="LightRadiusScale"/>. The half-angle is untouched.</summary>
        public const double FloodlightRangeTiles = 15;

        /// <summary>Authored kerb streetlight: the reference's 7 tiles at <see cref="LightRadiusScale"/>.</summary>
        public const double StreetLightRadiusTiles = 8.75;

        /// <summary>
        /// The reach this layer gives a light machine, or 0 for a kind it has no value for — which leaves that
        /// kind's own row exactly as it arrived, so a light added later is not silently rescaled.
        /// </summary>
        public static double LightReach(string machineKey)
        {
            switch (machineKey)
            {
                case "lamp": return LampRadiusTiles;
                case "arclamp": return ArcLampRadiusTiles;
                case "floodlight": return FloodlightRangeTiles;
                default: return 0;
            }
        }

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

            // REL-126. The reach is written as an absolute value rather than multiplied in place, so applying this
            // layer twice lands on the same numbers as applying it once; a multiplier would compound.
            var machines = d.Machines;
            if (machines != null)
            {
                List<MachineSpec> copy = null;
                for (var i = 0; i < machines.Count; i++)
                {
                    var m = machines[i];
                    if (m == null) continue;
                    var reach = LightReach(m.Key);
                    if (reach <= 0) continue;
                    // A Floodlight's reach is its cone range; a Lamp's is its disc. A row that carries neither is
                    // not a light whatever its key says, and is left alone.
                    if (m.ConeRangeTiles > 0)
                    {
                        if (m.ConeRangeTiles == reach) continue;
                        if (copy == null) copy = new List<MachineSpec>(machines);
                        copy[i] = m with { ConeRangeTiles = reach };
                    }
                    else if (m.LightRadiusTiles > 0)
                    {
                        if (m.LightRadiusTiles == reach) continue;
                        if (copy == null) copy = new List<MachineSpec>(machines);
                        copy[i] = m with { LightRadiusTiles = reach };
                    }
                }
                if (copy != null) { machines = copy; changed = true; }
            }

            // The same four numbers in the tuning record. They must agree with the machine rows above: the opening
            // advice quotes Power.LampRadiusTiles as what a Lamp does (OpeningQueries), and the authored kerb
            // streetlights have no machine row at all and read their radius from here (StreetLights.RadiusTiles).
            var power = d.Power;
            if (power != null && (power.LampRadiusTiles != LampRadiusTiles
                || power.ArcLampRadiusTiles != ArcLampRadiusTiles
                || power.FloodlightRangeTiles != FloodlightRangeTiles
                || power.StreetLightRadiusTiles != StreetLightRadiusTiles))
            {
                power = power with
                {
                    LampRadiusTiles = LampRadiusTiles,
                    ArcLampRadiusTiles = ArcLampRadiusTiles,
                    FloodlightRangeTiles = FloodlightRangeTiles,
                    StreetLightRadiusTiles = StreetLightRadiusTiles,
                };
                changed = true;
            }

            if (!changed) return d;
            return new GameData(d.Items, machines, d.Recipes, d.Engineer, d.World, d.Weapons, d.Enemies,
                d.Ammunition, turrets, power, time, d.Raids, d.Opening, d.Stake, d.Defence, d.Siege);
        }
    }
}
