namespace Relight.Sim
{
    /// <summary>
    /// The authored world seen by the sim: tile classes, blocked cells, deposits and the anchors of authored sites
    /// (U-M-14: geometry is data, injected into the sim, not compiled into it). Implemented by the world subsystem
    /// (B-08) on top of the synthetic map in Phase B and by the exported riverfront data in Phase C.
    /// Presentation never calls this directly; it reads the world through selectors.
    /// </summary>
    public interface ICityGeometry
    {
        /// <summary>Map size in tiles.</summary>
        int Width { get; }
        int Height { get; }
        /// <summary>The authored tile class at (x, y) (reference tiles.ts T_*); out-of-range is <see cref="TileClass.Void"/>.</summary>
        TileClass TileAt(int x, int y);
        /// <summary>
        /// Authored building collision at (x, y) (reference ground.ts <c>urban.solid[t]</c>: walls solid, doorways of
        /// enterable buildings open). A second mask, separate from the terrain class per WORLD_AND_ASSETS.md §2.6;
        /// out-of-range is false.
        /// </summary>
        bool Solid(int x, int y);
        /// <summary>Where the engineer stands up at start and after a down (reference: HQ / Home depot lot centre).</summary>
        Vec2 Spawn { get; }
        /// <summary>
        /// Line of sight through authored buildings between two tile-space points (reference ground.ts citySight, used by
        /// inReach). The synthetic Phase B map returns true unless a non-enterable building tile lies on the segment.
        /// </summary>
        bool Sight(double x0, double y0, double x1, double y1);
    }

    /// <summary>Authored tile classes, values identical to the reference tiles.ts T_* constants.</summary>
    public enum TileClass : byte
    {
        Street = 0,
        Ground = 1,
        Rubble = 2,
        Inert = 3,
        River = 4,
        Deposit = 5,
        Patch = 6,
        /// <summary>Outside the map (not a reference value; TileAt returns it for out-of-range coordinates).</summary>
        Void = 255,
    }
}
