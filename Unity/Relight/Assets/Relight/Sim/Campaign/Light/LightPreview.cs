using System;
using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>A box of tiles cut out of the map: tile (tx, ty) is set when <see cref="At"/> says so.</summary>
    public readonly struct TileWindow
    {
        public readonly int X0;
        public readonly int Y0;
        public readonly int W;
        public readonly int H;
        private readonly byte[] _tiles;

        public TileWindow(int x0, int y0, int w, int h, byte[] tiles)
        {
            X0 = x0; Y0 = y0; W = w; H = h; _tiles = tiles;
        }

        public bool Empty => _tiles == null || W <= 0 || H <= 0;

        /// <summary>True for a set tile; false for an unset one and for anything outside the box.</summary>
        public bool At(int tx, int ty)
        {
            var x = tx - X0;
            var y = ty - Y0;
            return _tiles != null && x >= 0 && y >= 0 && x < W && y < H && _tiles[y * W + x] != 0;
        }

        public int Count
        {
            get
            {
                if (_tiles == null) return 0;
                var n = 0;
                for (var i = 0; i < W * H; i++) if (_tiles[i] != 0) n++;
                return n;
            }
        }
    }

    /// <summary>One side of a tile, or several in a straight line, in tile-corner coordinates.</summary>
    public readonly struct TileEdge
    {
        public readonly int X0, Y0, X1, Y1;
        public TileEdge(int x0, int y0, int x1, int y1) { X0 = x0; Y0 = y0; X1 = x1; Y1 = y1; }
    }

    /// <summary>A horizontal run of set tiles on row <see cref="Y"/>, from <see cref="X0"/> to <see cref="X1"/> inclusive.</summary>
    public readonly struct TileRun
    {
        public readonly int Y, X0, X1;
        public TileRun(int y, int x0, int x1) { Y = y; X0 = x0; X1 = x1; }
    }

    /// <summary>
    /// L-02, ALWAYS_DARK_SPEC §4 "placing a light shows what it will light" and §5.7 "making it readable": the two
    /// questions the placement preview asks about light, answered by the rules that stamp the mask so the picture
    /// can never promise what the sim would not deliver.
    ///
    /// Both are read-only and neither touches <see cref="LightState"/>.
    /// </summary>
    public static class LightPreview
    {
        /// <summary>
        /// The tiles a ghost of <paramref name="kind"/> would light from (x, y), with blocking applied (§5.3) and at
        /// its full radius — what the player gets for a fully supplied light. False for a kind that emits nothing.
        /// The source is built exactly as <see cref="LightSources.Collect"/> builds a placed one.
        /// </summary>
        public static bool ForGhost(SimContext ctx, SimState st, string kind, int x, int y, Dir dir, out TileWindow lit)
        {
            lit = default;
            if (ctx == null || st == null || string.IsNullOrEmpty(kind)) return false;
            if (!ctx.Data.TryMachine(kind, out var spec)) return false;

            Light l;
            if (spec.LightRadiusTiles > 0)
                l = new Light(x, y, spec.LightRadiusTiles, LightKind.Lamp, true, dir);
            else if (spec.ConeRangeTiles > 0)
            {
                var (w, h) = Footprints.Dimensions(kind, dir, spec.Size);
                l = new Light(x + w / 2.0, y + h / 2.0, spec.ConeRangeTiles, LightKind.Floodlight, true, dir,
                    spec.ConeHalfAngleRad);
            }
            else return false;

            var g = ctx.Geometry;
            var tw = g.Width;
            var th = g.Height;
            if (tw <= 0 || th <= 0) return false;
            var x0 = Math.Max(0, (int)Math.Floor(l.Tx - l.R));
            var x1 = Math.Min(tw - 1, (int)Math.Ceiling(l.Tx + l.R));
            var y0 = Math.Max(0, (int)Math.Floor(l.Ty - l.R));
            var y1 = Math.Min(th - 1, (int)Math.Ceiling(l.Ty + l.R));
            var bw = x1 - x0 + 1;
            var bh = y1 - y0 + 1;
            if (bw <= 0 || bh <= 0) return false;

            var tiles = new byte[bw * bh];
            LightRules.StampWindow(tiles, x0, y0, bw, tw, th, in l, ctx, st);
            lit = new TileWindow(x0, y0, bw, bh, tiles);
            return true;
        }

        /// <summary>
        /// The tiles of the CURRENT lit mask whose centre lies within <paramref name="r"/> tiles of (cx, cy): the
        /// ground on which a turret standing there reaches its full range (§5.7). Empty before the first mask.
        /// </summary>
        public static TileWindow LitWithin(SimState st, double cx, double cy, double r)
        {
            var mask = LightQueries.Mask(st);
            var (tw, th) = LightQueries.MaskSize(st);
            if (mask == null || tw <= 0 || th <= 0 || r <= 0) return default;
            var x0 = Math.Max(0, (int)Math.Floor(cx - r));
            var x1 = Math.Min(tw - 1, (int)Math.Ceiling(cx + r));
            var y0 = Math.Max(0, (int)Math.Floor(cy - r));
            var y1 = Math.Min(th - 1, (int)Math.Ceiling(cy + r));
            var bw = x1 - x0 + 1;
            var bh = y1 - y0 + 1;
            if (bw <= 0 || bh <= 0) return default;

            var tiles = new byte[bw * bh];
            for (var ty = y0; ty <= y1; ty++)
                for (var tx = x0; tx <= x1; tx++)
                {
                    if (mask[ty * tw + tx] == 0) continue;
                    var dx = tx + 0.5 - cx;
                    var dy = ty + 0.5 - cy;
                    if (dx * dx + dy * dy <= r * r) tiles[(ty - y0) * bw + (tx - x0)] = 1;
                }
            return new TileWindow(x0, y0, bw, bh, tiles);
        }

        /// <summary>The set tiles as one run per unbroken stretch of each row — what a fill is drawn from.</summary>
        public static void Runs(in TileWindow w, List<TileRun> into)
        {
            into.Clear();
            if (w.Empty) return;
            for (var ty = w.Y0; ty < w.Y0 + w.H; ty++)
            {
                var start = int.MinValue;
                for (var tx = w.X0; tx <= w.X0 + w.W; tx++)
                {
                    var set = w.At(tx, ty);
                    if (set && start == int.MinValue) start = tx;
                    if (!set && start != int.MinValue) { into.Add(new TileRun(ty, start, tx - 1)); start = int.MinValue; }
                }
            }
        }

        /// <summary>
        /// The boundary between set and unset tiles, with the sides that lie in one straight line joined — what an
        /// outline is drawn from. A gap behind a wall shows as its own edge, which is the point of the preview.
        /// </summary>
        public static void Outline(in TileWindow w, List<TileEdge> into)
        {
            into.Clear();
            if (w.Empty) return;

            // Horizontal sides: the line y = ty separates row ty - 1 from row ty.
            for (var ty = w.Y0; ty <= w.Y0 + w.H; ty++)
            {
                var start = int.MinValue;
                var side = false;
                for (var tx = w.X0; tx <= w.X0 + w.W; tx++)
                {
                    var above = w.At(tx, ty - 1);
                    var below = w.At(tx, ty);
                    var edge = above != below;
                    if (start != int.MinValue && (!edge || above != side))
                    {
                        into.Add(new TileEdge(start, ty, tx, ty));
                        start = int.MinValue;
                    }
                    if (edge && start == int.MinValue) { start = tx; side = above; }
                }
            }

            // Vertical sides: the line x = tx separates column tx - 1 from column tx.
            for (var tx = w.X0; tx <= w.X0 + w.W; tx++)
            {
                var start = int.MinValue;
                var side = false;
                for (var ty = w.Y0; ty <= w.Y0 + w.H; ty++)
                {
                    var left = w.At(tx - 1, ty);
                    var right = w.At(tx, ty);
                    var edge = left != right;
                    if (start != int.MinValue && (!edge || left != side))
                    {
                        into.Add(new TileEdge(tx, start, tx, ty));
                        start = int.MinValue;
                    }
                    if (edge && start == int.MinValue) { start = ty; side = left; }
                }
            }
        }
    }
}
