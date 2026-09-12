using Relight.Sim;
using UnityEngine;

namespace Relight.World
{
    /// <summary>
    /// B-13. The ONE place sim tile space and Unity world space meet (U-M-14: the sim is Y-down with the origin at
    /// the top-left tile; Unity is Y-up). Every view, the camera rig, the tilemap painter and the pointer mapping go
    /// through this class, so the flip exists exactly once and a mistake shows up everywhere at the same time
    /// instead of in one component.
    ///
    /// Scale: one tile is one Unity world unit. The reference draws a tile as <c>TILE_PX = 32</c> pixels
    /// (packages/sim/src/tiles.ts:17) and multiplies every tile coordinate by it (worldScene.ts passim); in Unity the
    /// same factor lives in the sprites' pixels-per-unit (32) instead of in the coordinates, so tile arithmetic here
    /// stays in whole tiles and nothing has to remember to multiply.
    /// </summary>
    public static class WorldSpace
    {
        /// <summary>Reference tiles.ts:17 TILE_PX — the pixels-per-unit every placeholder sprite is imported at.</summary>
        public const int PixelsPerTile = 32;

        /// <summary>Unity world units one sim tile spans.</summary>
        public const float UnitsPerTile = 1f;

        /// <summary>A continuous sim position (tile units, Y-down) as a Unity world point.</summary>
        public static Vector3 World(Vec2 p) => new Vector3((float)p.X * UnitsPerTile, -(float)p.Y * UnitsPerTile, 0f);

        /// <summary>A continuous sim position as a Unity world point at an explicit depth.</summary>
        public static Vector3 World(Vec2 p, float z) => new Vector3((float)p.X * UnitsPerTile, -(float)p.Y * UnitsPerTile, z);

        /// <summary>The centre of sim tile (x, y) in Unity world space.</summary>
        public static Vector3 TileCentre(int x, int y) =>
            new Vector3((x + 0.5f) * UnitsPerTile, -(y + 0.5f) * UnitsPerTile, 0f);

        /// <summary>The centre of a footprint whose north-west corner is sim tile (x, y) and which is w × h tiles.</summary>
        public static Vector3 RectCentre(int x, int y, int w, int h) =>
            new Vector3((x + w * 0.5f) * UnitsPerTile, -(y + h * 0.5f) * UnitsPerTile, 0f);

        /// <summary>The tilemap cell a sim tile paints into: the cell's own origin is its lower-left corner, so Y flips by one.</summary>
        public static Vector3Int Cell(int x, int y) => new Vector3Int(x, -y - 1, 0);

        /// <summary>
        /// The sim tile under a Unity world point (reference worldScene.ts:719 <c>Math.floor(w.x / TILE_PX)</c>).
        /// Out-of-range coordinates are returned as they are; callers ask the geometry whether the tile exists.
        /// </summary>
        public static TilePoint Tile(Vector3 world) => new TilePoint(
            Mathf.FloorToInt(world.x / UnitsPerTile),
            Mathf.FloorToInt(-world.y / UnitsPerTile));

        /// <summary>The continuous sim position of a Unity world point (reference worldScene.ts:713 <c>aimAt</c>).</summary>
        public static Vec2 Position(Vector3 world) => new Vec2(world.x / UnitsPerTile, -world.y / UnitsPerTile);

        /// <summary>Linear interpolation between two sim positions, in sim space, before the flip (presentation only).</summary>
        public static Vec2 Lerp(Vec2 a, Vec2 b, double t) =>
            new Vec2(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);
    }
}
