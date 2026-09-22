namespace Relight.Sim.UI
{
    /// <summary>
    /// How a district's light is DRAWN arriving (ALWAYS_DARK_SPEC.md §5.4: "a visible sweep of light across the
    /// district"). Picture only, like <see cref="DarknessLook"/>: the sim lights every tile of the district on one
    /// tick, and every rule — turret sight, alien hesitation, the lamp queries — reads that tick's mask. Nothing
    /// here is ever read back into the simulation.
    ///
    /// REL-117: the lamp heads already staggered outward from the substation at <see cref="TilesPerSecond"/>
    /// (<c>CityPresenter.SweepDistricts</c>) while the ground under them went bright in a single frame, so the two
    /// halves of the same moment disagreed. Both now measure the front with this one rule, so the ground arrives
    /// under each head as it comes on.
    ///
    /// The front is a circle growing from the substation. A tile the sweep has not reached yet is drawn as it was —
    /// dark — and <see cref="EdgeTiles"/> of soft edge keeps the front from reading as a hard disc.
    /// </summary>
    public static class LightSweep
    {
        /// <summary>How fast the front travels, in tiles of sim map per second of wall clock.</summary>
        public const double TilesPerSecond = 30.0;

        /// <summary>Width of the soft leading edge, in tiles. A tile inside it is drawn part-lit. Always above 0.</summary>
        public const double EdgeTiles = 2.0;

        /// <summary>Where the front has reached after <paramref name="elapsed"/> seconds. Never negative.</summary>
        public static double Radius(double elapsed) => elapsed <= 0 ? 0 : elapsed * TilesPerSecond;

        /// <summary>
        /// How much a newly lit tile is still held back, 1 fully dark to 0 fully lit, for a tile
        /// <paramref name="distance"/> tiles from the substation after <paramref name="elapsed"/> seconds.
        /// Only tiles this sweep lit are ever asked: ground that was already lit is never held.
        /// </summary>
        public static double Hold(double distance, double elapsed)
        {
            var d = distance - Radius(elapsed);
            if (d <= 0) return 0;
            var h = d / EdgeTiles;
            return h >= 1 ? 1 : h;
        }

        /// <summary>
        /// How long a sweep runs whose furthest newly lit tile is <paramref name="maxDistance"/> away — the point
        /// at which every tile of the district is drawn at its full strength and the hold can be dropped.
        /// </summary>
        public static double Seconds(double maxDistance)
        {
            if (!(maxDistance > 0)) return 0;
            return maxDistance / TilesPerSecond;
        }
    }
}
