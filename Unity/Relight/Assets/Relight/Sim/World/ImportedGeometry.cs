using System;

namespace Relight.Sim
{
    /// <summary>
    /// C-01 / D-02a. An imported authored region as flat arrays — engine-free, so the importer's checks and the
    /// exporter's counts can be verified without Unity (U-M-14: geometry is data injected into the sim).
    ///
    /// This is <see cref="ArrayGeometry"/> plus the three things a region needs that a whole-map array does not:
    /// the region's origin in city tiles (so a site id exported region-locally can still be named in city
    /// coordinates), the renderer's per-tile <c>variant</c> and <c>patch</c> side-cars (reference ground.ts:527
    /// <c>paint</c> / ground.ts:561 <c>groundTiles</c>), and the two counts the exporter records so the importer can
    /// re-derive them and refuse a corrupted file (<c>land</c>, <c>accessible</c>).
    ///
    /// Coordinates are REGION-LOCAL and Y-DOWN throughout, exactly as in the sim (<c>t = y * Width + x</c>,
    /// WORLD_AND_ASSETS.md §2.1). The Y flip to Unity cell space happens only in <c>Relight.World.WorldSpace.Cell</c>
    /// (§2.2); nothing in this file flips anything.
    /// </summary>
    public sealed class RegionGeometry : ICityGeometry, IMinableGeometry
    {
        private readonly ArrayGeometry _core;
        private readonly byte[] _variant;
        private readonly byte[] _patch;

        /// <summary>The region's north-west corner in whole-city tiles (<c>city.json</c> <c>region.origin</c>).</summary>
        public int OriginX { get; }
        public int OriginY { get; }

        public int Width => _core.Width;
        public int Height => _core.Height;
        public Vec2 Spawn => _core.Spawn;

        private RegionGeometry(ArrayGeometry core, int originX, int originY, byte[] variant, byte[] patch)
        {
            _core = core;
            OriginX = originX;
            OriginY = originY;
            _variant = variant;
            _patch = patch;
        }

        /// <summary>
        /// Build a region from the exporter's arrays. <paramref name="kind"/> holds <see cref="TileClass"/> values,
        /// <paramref name="solid"/> the authored collision mask (reference ground.ts:628-630, 0 or 1),
        /// <paramref name="variant"/> and <paramref name="patch"/> the renderer side-cars (<paramref name="patch"/>
        /// holds <see cref="PatchType"/> values). <paramref name="variant"/> and <paramref name="patch"/> may be null
        /// (a map without them reports 0 / <see cref="PatchType.None"/>); the others may not.
        /// </summary>
        public static RegionGeometry FromArrays(int width, int height, int originX, int originY,
            byte[] kind, byte[] solid, byte[] variant, byte[] patch, Vec2 spawn)
        {
            if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (kind == null) throw new ArgumentNullException(nameof(kind));
            if (solid == null) throw new ArgumentNullException(nameof(solid));
            var n = width * height;
            if (kind.Length != n) throw new ArgumentException("kind length must be width*height", nameof(kind));
            if (solid.Length != n) throw new ArgumentException("solid length must be width*height", nameof(solid));
            if (variant != null && variant.Length != n) throw new ArgumentException("variant length must be width*height", nameof(variant));
            if (patch != null && patch.Length != n) throw new ArgumentException("patch length must be width*height", nameof(patch));

            var flags = new bool[n];
            for (var t = 0; t < n; t++) flags[t] = solid[t] != 0;
            var core = new ArrayGeometry(width, height, kind, flags, spawn);
            return new RegionGeometry(core, originX, originY, variant, patch);
        }

        public bool InBounds(int x, int y) => _core.InBounds(x, y);
        public TileClass TileAt(int x, int y) => _core.TileAt(x, y);
        public bool Solid(int x, int y) => _core.Solid(x, y);
        public bool Sight(double x0, double y0, double x1, double y1) => _core.Sight(x0, y0, x1, y1);

        /// <summary>The renderer's terrain variant for a tile (reference ground.ts:527 <c>paint</c>); 0 off-map.</summary>
        public byte VariantAt(int x, int y) =>
            _variant != null && InBounds(x, y) ? _variant[y * Width + x] : (byte)0;

        /// <summary>What a <see cref="TileClass.Patch"/> tile yields (reference tiles.ts:30 <c>P_*</c>).</summary>
        public PatchType PatchAt(int x, int y) =>
            _patch != null && InBounds(x, y) ? (PatchType)_patch[y * Width + x] : PatchType.None;

        /// <summary>
        /// <see cref="IMinableGeometry"/> (Wave 1 W-C, wired by the coordinator): an authored patch tile yields its
        /// patch item (reference flow.ts:697 <c>G.patch[t]</c>); any other tile answers false so
        /// <see cref="Mining.FallbackItem"/> decides from the tile class (rubble → steel, deposit → coal).
        /// </summary>
        public bool TryMinedItem(int x, int y, out ItemId item) => PatchTypes.TryItem(PatchAt(x, y), out item);

        /// <summary>Region-local tile → whole-city tile (the inverse of what the exporter subtracted).</summary>
        public void ToCity(int x, int y, out int cityX, out int cityY)
        {
            cityX = x + OriginX;
            cityY = y + OriginY;
        }

        /// <summary>Whole-city tile → region-local tile. Returns false when the tile is outside the region.</summary>
        public bool ToRegion(int cityX, int cityY, out int x, out int y)
        {
            x = cityX - OriginX;
            y = cityY - OriginY;
            return InBounds(x, y);
        }

        /// <summary>
        /// Tiles that are not river — the exporter's <c>validation.land</c>. Kept here so the two sides run the same
        /// definition and a mismatch means the file is wrong, not that the rule drifted.
        /// </summary>
        public int CountLand()
        {
            var n = 0;
            for (var y = 0; y < Height; y++)
            for (var x = 0; x < Width; x++)
                if (TileAt(x, y) != TileClass.River) n++;
            return n;
        }

        /// <summary>
        /// Tiles reachable from <see cref="Spawn"/> by 4-neighbour steps over tiles that are neither solid nor river —
        /// the exporter's <c>validation.accessible</c> (same algorithm, same neighbour order, so the counts match
        /// exactly). The spawn tile itself counts.
        /// </summary>
        public int CountAccessible()
        {
            var w = Width;
            var h = Height;
            var n = w * h;
            var start = (int)Math.Floor(Spawn.Y) * w + (int)Math.Floor(Spawn.X);
            if (start < 0 || start >= n) return 0;

            var seen = new bool[n];
            var queue = new int[n];
            var end = 0;
            var count = 0;
            queue[end++] = start;
            seen[start] = true;
            for (var at = 0; at < end; at++)
            {
                var t = queue[at];
                var x = t % w;
                var y = (t - x) / w;
                count++;
                Step(x - 1, y, w, h, seen, queue, ref end);
                Step(x + 1, y, w, h, seen, queue, ref end);
                Step(x, y - 1, w, h, seen, queue, ref end);
                Step(x, y + 1, w, h, seen, queue, ref end);
            }
            return count;
        }

        private void Step(int x, int y, int w, int h, bool[] seen, int[] queue, ref int end)
        {
            if (x < 0 || y < 0 || x >= w || y >= h) return;
            var t = y * w + x;
            if (seen[t] || Solid(x, y) || TileAt(x, y) == TileClass.River) return;
            seen[t] = true;
            queue[end++] = t;
        }
    }
}
