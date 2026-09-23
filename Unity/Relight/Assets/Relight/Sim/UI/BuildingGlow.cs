namespace Relight.Sim.UI
{
    /// <summary>
    /// REL-127. The small light a placed building is DRAWN giving off. Picture only, like <see cref="DarknessLook"/>
    /// and <see cref="LightSweep"/>: nothing here is read back into the simulation.
    ///
    /// The owner's note: "Buildings should have their own light, not large, but big enough to illuminate a small area
    /// around them, excludes walls and power poles." Asked whether that light should be real or only seen, they chose
    /// **seen only** (U-D-71 b). That answer is the whole reason this class exists instead of a
    /// <c>MachineSpec.LightRadiusTiles</c> row: that field is read by <see cref="LightSources.Collect"/>, so a value
    /// there would stamp the sim's own mask — and raiders hesitate on lit ground
    /// (<c>RaidDirectorTuning.LightHesitateS</c>), so a glow on every building would quietly turn a base into one
    /// large hesitation field and make raids easier. A glow held here can never do that: no caller in
    /// <c>Relight.Sim</c> outside this file uses it, and <see cref="LightQueries.Mask"/> never sees it.
    ///
    /// <para><b>Which kinds glow.</b> The rule is read from the catalogue rather than written as a list of keys, so a
    /// kind added later is classified by what it is. A kind glows when it has a footprint of 2 tiles or more, carries
    /// no power reach, and is not already a light:</para>
    /// <list type="bullet">
    /// <item><b>2 tiles or more</b> is what separates a building from the one-tile parts laid down in lines — belts,
    ///       fast belts, undergrounds, splitters, inserters — and from the Wall and the Barricade, which the owner
    ///       excluded by name. A line of forty glowing belt tiles would be the "large" light the note rules out.
    ///       REL-132's Gate is one tile, so it is excluded by the same clause the Wall is.</item>
    /// <item><b>No power reach</b> (<c>MachineSpec.ReachTiles</c>) excludes the Pole, the Big pole and the
    ///       Substation — the owner's "power poles". The Substation is the largest of them, and the ground around one
    ///       is district-lit already.</item>
    /// <item><b>Not a light</b> excludes the Lamp, Arc lamp and Floodlight, whose light is real, comes from the mask,
    ///       and goes out when their power does. A fake glow under a dead lamp would be a lie about the grid.</item>
    /// </list>
    /// <para>What is left is what the note calls a building: Excavator, Pumpjack, Foundry, Refinery, Assembler,
    /// Assembler Mk2, Mixer, Alien workbench, Supply chest, Generator, Gun turret, Cannon and Depot.</para>
    ///
    /// <para><b>The two numbers are the assistant's</b> under U-D-28; the note says only "not large". Recorded as
    /// U-P-32.</para>
    /// </summary>
    public static class BuildingGlow
    {
        /// <summary>
        /// How far the glow reaches BEYOND the building's footprint, in tiles. Measured from the edge and not the
        /// centre, so a 6×6 Depot and a 2×2 chest both spill the same distance onto the ground around them.
        /// </summary>
        public const double ReachTiles = 2.5;

        /// <summary>
        /// How much darkness the glow removes where it is strongest, right against the wall: 0 nothing, 1 as bright
        /// as true lamplight. Held below 1 on purpose — a building that lit its surroundings as well as a Lamp does
        /// would make the Lamp pointless, and the point of the note is to see the ground by a building, not to
        /// replace the lighting the player builds.
        /// </summary>
        public const double Peak = 0.75;

        /// <summary>True for a kind that draws a glow. See the class remarks for why the rule reads the catalogue.</summary>
        public static bool Glows(MachineSpec spec) =>
            spec != null && spec.Size >= 2 && spec.ReachTiles <= 0 &&
            spec.LightRadiusTiles <= 0 && spec.ConeRangeTiles <= 0;

        /// <summary>True for a placed machine that draws a glow. Unconditional: a building glows whether or not its circuit is delivering.</summary>
        public static bool Glows(GameData d, Machine m) =>
            m != null && d != null && d.TryMachine(m.Kind, out var spec) && Glows(spec);

        /// <summary>
        /// Distance in tiles from a point in tile space to the nearest edge of a footprint, 0 anywhere inside it.
        /// The footprint is the half-open tile rect <c>[x, x + w) × [y, y + h)</c> the machine occupies.
        /// </summary>
        public static double Distance(int x, int y, int w, int h, double px, double py)
        {
            var dx = px < x ? x - px : px > x + w ? px - (x + w) : 0;
            var dy = py < y ? y - py : py > y + h ? py - (y + h) : 0;
            if (dx <= 0) return dy;
            if (dy <= 0) return dx;
            return System.Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// How much darkness the glow removes <paramref name="distance"/> tiles out from the footprint: <see cref="Peak"/>
        /// against the wall, falling linearly to 0 at <see cref="ReachTiles"/> and never negative.
        /// </summary>
        public static double Strength(double distance)
        {
            if (!(distance > 0)) return Peak;
            if (distance >= ReachTiles) return 0;
            return Peak * (1 - distance / ReachTiles);
        }
    }
}
