using System;

namespace Relight.Sim
{
    /// <summary>
    /// NEW Unity test content — this is not a port. The reference's 24×24 block lattice (<c>map.ts generateMap</c>)
    /// is retired with the block economy (CONTENT_CATALOGUE.md §17) and the authored riverfront city
    /// (<c>city/riverfront.ts</c>, 864×576) is Phase C data that no Phase B code may depend on. So Phase B needs a
    /// small deterministic map of its own; B-12/B-13 use this one too.
    ///
    /// 64×48 tiles, Y-down, origin top-left (U-M-14). Everything is authored by fixed rectangles — no RNG, no seed,
    /// no iteration order that could vary — so <see cref="Create"/> returns byte-identical geometry every call.
    ///
    /// Sketch, one character per 2×2 tiles (so column c is x = 2c, row r is y = 2r):
    ///
    /// ..==...........==...........==..
    /// ..==...........==...........==..
    /// ================================
    /// ===@============================
    /// ..==...........==...#####...==..
    /// ..==.rrrrr.....==...#####...==..
    /// ..==.rrrrr.pp..==...#####...==..
    /// ..==.rrrrr.pp..==...#####...==..
    /// ..==...........==...........==..
    /// ================================
    /// ================================
    /// ..==...........==...........==..
    /// ~~~~~~~~~~~~~~~==~~~~~~~~~~~~~~~
    /// ~~~~~~~~~~~~~~~==~~~~~~~~~~~~~~~
    /// ..==...........==...........==..
    /// ..==..iii......==.....dddd..==..
    /// ..==..iii......==.....dddd..==..
    /// ..==...........==...........==..
    /// ================================
    /// ================================
    /// ..==...........==...........==..
    /// ..==...........==...........==..
    /// ..==...........==...........==..
    /// ..==...........==...........==..
    ///
    /// <c>.</c> ground · <c>=</c> street · <c>~</c> river (impassable) · <c>r</c> rubble · <c>p</c> patch ·
    /// <c>d</c> deposit · <c>i</c> inert · <c>#</c> solid building (terrain underneath stays ground; the block is in
    /// the collision mask, matching WORLD_AND_ASSETS.md §2.6) · <c>@</c> spawn.
    ///
    /// Properties the Phase B tests rely on:
    /// * The river spans the full width at y = 24..27 with exactly ONE crossing, the street at x = 30..33, so any
    ///   north↔south path must route through it.
    /// * The building at x = 40..49, y = 8..15 is solid but stands on ordinary ground tiles, so it separates
    ///   "terrain says river" from "a building blocks me".
    /// * Spawn (6.5, 6.5) is the centre of a street tile at a crossroads, with open street in all four directions.
    /// </summary>
    public static class SyntheticMap
    {
        public const int Width = 64;
        public const int Height = 48;

        /// <summary>Street band thickness and positions (authored, not the retired <c>STREET_TILES</c> lattice maths).</summary>
        public const int StreetBand = 4;

        /// <summary>The one gap in the river; tests assert a path crosses here.</summary>
        public static readonly TileRect RiverCrossing = new TileRect(30, 24, 4, 4);
        /// <summary>The river strip, full width.</summary>
        public static readonly TileRect River = new TileRect(0, 24, Width, 4);
        /// <summary>The non-enterable building: solid mask only, terrain underneath is ground.</summary>
        public static readonly TileRect Building = new TileRect(40, 8, 10, 8);
        public static readonly TileRect Rubble = new TileRect(10, 10, 10, 6);
        public static readonly TileRect Patch = new TileRect(22, 12, 3, 3);
        public static readonly TileRect Deposit = new TileRect(44, 30, 8, 4);
        public static readonly TileRect Inert = new TileRect(12, 30, 6, 4);

        /// <summary>Centre of the street tile at (6, 6).</summary>
        public static readonly Vec2 Spawn = new Vec2(6.5, 6.5);

        private static readonly int[] StreetRows = { 4, 18, 36 };
        private static readonly int[] StreetCols = { 4, 30, 56 };

        public static ArrayGeometry Create()
        {
            var kind = new byte[Width * Height];
            var solid = new bool[Width * Height];

            Fill(kind, 0, 0, Width, Height, TileClass.Ground);

            for (var i = 0; i < StreetRows.Length; i++)
                Fill(kind, 0, StreetRows[i], Width, StreetBand, TileClass.Street);
            for (var i = 0; i < StreetCols.Length; i++)
                Fill(kind, StreetCols[i], 0, StreetBand, Height, TileClass.Street);

            Fill(kind, River, TileClass.River);
            Fill(kind, RiverCrossing, TileClass.Street);

            Fill(kind, Rubble, TileClass.Rubble);
            Fill(kind, Patch, TileClass.Patch);
            Fill(kind, Deposit, TileClass.Deposit);
            Fill(kind, Inert, TileClass.Inert);

            // The building leaves the terrain alone and only sets collision (WORLD_AND_ASSETS.md §2.6; the reference
            // does the same in riverfrontGround, ground.ts:628 — solid[t] = 1 over unchanged G.base tiles).
            for (var y = Building.Y; y < Building.Y + Building.H; y++)
                for (var x = Building.X; x < Building.X + Building.W; x++)
                    solid[y * Width + x] = true;

            return new ArrayGeometry(Width, Height, kind, solid, Spawn);
        }

        private static void Fill(byte[] kind, TileRect r, TileClass k) => Fill(kind, r.X, r.Y, r.W, r.H, k);

        private static void Fill(byte[] kind, int x0, int y0, int w, int h, TileClass k)
        {
            var v = (byte)k;
            for (var y = y0; y < y0 + h; y++)
                for (var x = x0; x < x0 + w; x++)
                    kind[y * Width + x] = v;
        }
    }
}
