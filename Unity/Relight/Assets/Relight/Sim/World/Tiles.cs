using System;

namespace Relight.Sim
{
    /// <summary>
    /// Structural tile helpers (reference <c>packages/sim/src/tiles.ts</c>).
    ///
    /// What is ported here is only what the authored-city path still uses: the flat row-major index convention
    /// (<c>t = y * width + x</c>, WORLD_AND_ASSETS.md §2.1), the tile-class display names (tiles.ts:25) and the
    /// patch typing that <see cref="TileClass.Patch"/> tiles carry (tiles.ts:30 <c>P_*</c>).
    ///
    /// Deliberately NOT ported (lattice-era generators retired with the block economy, CONTENT_CATALOGUE.md §17):
    /// <c>CELL_TILES/LOT_TILES/MARGIN_TILES/STREET_TILES</c> cell arithmetic and <c>isMargin</c>/<c>tileCell</c>,
    /// <c>lotLayout</c>/<c>cellTiles</c>/<c>cityTiles</c> (seeded rubble blobs: CLUSTERS, SIGMA, RUBBLE_TILES_MIN/MAX,
    /// RUBBLE_VARIANTS, DEPOSIT_* fractions), <c>substationLot</c>/<c>streetlights</c>/<c>STREETLIGHT_*</c>,
    /// <c>rubbleOf</c>/<c>depthFrac</c>/<c>rubbleLeft</c>/<c>goneCount</c>, <c>RAIL_YARD_*</c>, and the HQ lot
    /// rectangles <c>HQ_PATCHES</c>/<c>HQ_SUBSTATION</c>/<c>HQ_RUBBLE_TILES</c>/<c>HQ_CLEAR_ROWS</c> with
    /// <c>hqPatchAt</c>/<c>hqReserved</c>. The HQ rectangles are still read on the authored path
    /// (ground.ts:400 <c>markPatches</c>, flow.ts:701, goal.ts:182, hour.ts:168, inspection.ts:79) but they are
    /// authored *content* — where a resource patch sits and how much it holds — so they belong to the geometry the
    /// Phase C importer injects through <see cref="ICityGeometry"/>, not to a compiled-in constant (U-M-14).
    /// The numeric data that is tuning (<c>TILE_PX</c>, lot sizes, rubble units per tile) lives in
    /// <see cref="WorldTuning"/>; nothing here duplicates it.
    /// </summary>
    public static class Tiles
    {
        /// <summary>Renderer chunk side in tiles (reference ground.ts:29 <c>CHUNK</c>). Presentation-only.</summary>
        public const int Chunk = 32;

        /// <summary>Flat row-major index of a tile (reference: <c>t = ty * tw + tx</c> everywhere).</summary>
        public static int Index(int x, int y, int width) => y * width + x;

        public static int XOf(int index, int width) => index % width;
        public static int YOf(int index, int width) => (index - index % width) / width;

        /// <summary>Reference tiles.ts:25 <c>TILE_NAMES</c>; <see cref="TileClass.Void"/> is the port's off-map marker.</summary>
        public static string Name(TileClass k)
        {
            switch (k)
            {
                case TileClass.Street: return "street";
                case TileClass.Ground: return "ground";
                case TileClass.Rubble: return "rubble";
                case TileClass.Inert: return "inert";
                case TileClass.River: return "river";
                case TileClass.Deposit: return "deposit";
                case TileClass.Patch: return "patch";
                default: return "void";
            }
        }

        /// <summary>True for a tile class that can hold minable units (reference flow.ts <c>rubbleAt</c> inputs).</summary>
        public static bool CanHoldUnits(TileClass k) =>
            k == TileClass.Rubble || k == TileClass.Deposit || k == TileClass.Patch;
    }

    /// <summary>What a <see cref="TileClass.Patch"/> tile yields (reference tiles.ts:30 <c>P_NONE/P_STEEL/P_COPPER/P_COAL</c>).</summary>
    public enum PatchType : byte
    {
        None = 0,
        Steel = 1,
        Copper = 2,
        Coal = 3,
    }

    public static class PatchTypes
    {
        /// <summary>Reference tiles.ts:31 <c>PATCH_NAMES</c>.</summary>
        public static string Name(PatchType p)
        {
            switch (p)
            {
                case PatchType.Steel: return "steel";
                case PatchType.Copper: return "copper";
                case PatchType.Coal: return "coal";
                default: return "";
            }
        }

        /// <summary>The item a patch tile yields (reference flow.ts:704).</summary>
        public static bool TryItem(PatchType p, out ItemId id)
        {
            switch (p)
            {
                case PatchType.Steel: id = ItemId.Steel; return true;
                case PatchType.Copper: id = ItemId.Copper; return true;
                case PatchType.Coal: id = ItemId.Coal; return true;
                default: id = default; return false;
            }
        }
    }
}
