using System;
using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>
    /// Reusable A* working arrays (reference walk.ts:59 <c>Scratch</c>). Held by <see cref="GroundState"/>, never in a
    /// module-level cache, and never visited — the search leaves nothing behind that a save has to carry.
    /// </summary>
    public sealed class PathScratch
    {
        public readonly int Size;
        public readonly double[] G;
        public readonly int[] Parent;
        public readonly int[] Stamp;
        public readonly int[] Heap;
        public readonly double[] Hf;
        public int Gen;

        public PathScratch(int size)
        {
            Size = size;
            G = new double[size];
            Parent = new int[size];
            Stamp = new int[size];
            Heap = new int[size * 2];
            Hf = new double[size * 2];
        }
    }

    /// <summary>
    /// Shortest walk between two tiles, 8-connected with no corner cutting — a line-for-line port of
    /// <c>findPath</c> (packages/sim/src/walk.ts:80–142).
    ///
    /// Determinism depends on three things that are kept exactly as written there and must not be "tidied":
    /// the neighbour order <c>NX/NY</c> (N, E, S, W, then NE, SE, SW, NW), the binary heap's sift rules
    /// (<c>push</c> stops at the first parent with <c>hf &lt;= f</c>; <c>pop</c> prefers the LEFT child on a tie
    /// because the right child is only taken when <c>hf[c+1] &lt; hf[c]</c>), and the octile heuristic
    /// <c>max(dx,dy) + (sqrt2-1)*min(dx,dy)</c>. Same inputs therefore always give the same path, not merely a path
    /// of the same length.
    /// </summary>
    public static class PathFinder
    {
        /// <summary>Reference walk.ts:78 <c>limit</c> default: bounds the search for a stuck engineer.</summary>
        public const int DefaultLimit = 250000;

        private static readonly double Sqrt2 = Math.Sqrt(2);
        private static readonly int[] NX = { 0, 1, 0, -1, 1, 1, -1, -1 };
        private static readonly int[] NY = { -1, 0, 1, 0, -1, 1, 1, -1 };

        /// <summary>
        /// The tiles to step through, ending on the goal and excluding the start; an EMPTY list when the engineer is
        /// already on the goal tile; <c>null</c> when there is no route (reference returns <c>Int32Array</c> / null).
        /// </summary>
        /// <param name="clearance">Half-width of the box that must be open around every tile (reference: enemies use 1).</param>
        /// <param name="bias">Optional extra cost per tile (reference <c>cityNavigation</c> threat bias); null in the
        /// engineer's own path-finding, so the tick path allocates no delegate.</param>
        public static List<TilePoint> FindPath(SimContext ctx, SimState st, int sx, int sy, int gx, int gy,
            int limit = DefaultLimit, int clearance = 0, Func<int, int, double> bias = null)
        {
            var tw = ctx.Geometry.Width;
            var th = ctx.Geometry.Height;
            if (!Ground.InBounds(ctx, sx, sy) || !Ground.InBounds(ctx, gx, gy)) return null;

            var solid = st.Ground.SolidMap(ctx, st);

            if (!Open(ctx, solid, tw, th, gx, gy, clearance)) return null;

            var s = st.Ground.Scratch(tw * th);
            var gen = ++s.Gen;
            var start = sy * tw + sx;
            var goal = gy * tw + gx;
            if (start == goal) return new List<TilePoint>();

            var n = 0;
            s.Stamp[start] = gen;
            s.G[start] = 0;
            s.Parent[start] = -1;
            Push(s, ref n, start, Heuristic(sx, sy, gx, gy));

            var expanded = 0;
            while (n > 0)
            {
                var t = Pop(s, ref n);
                if (t == goal) return Reconstruct(s, start, t, tw);
                if (s.Stamp[t] == -gen) continue;   // closed
                s.Stamp[t] = -gen;
                if (++expanded > limit) return null;

                var x = t % tw;
                var y = (t - x) / tw;
                var gt = s.G[t];
                for (var k = 0; k < 8; k++)
                {
                    var nx = x + NX[k];
                    var ny = y + NY[k];
                    if (!Open(ctx, solid, tw, th, nx, ny, clearance)) continue;
                    // No corner cutting: a diagonal needs both of its orthogonal sides open.
                    if (k >= 4 && (!Open(ctx, solid, tw, th, x + NX[k], y, clearance)
                                || !Open(ctx, solid, tw, th, x, y + NY[k], clearance))) continue;
                    var nt = ny * tw + nx;
                    var extra = bias == null ? 0 : Math.Max(0, bias(nx, ny));
                    var ng = gt + (k >= 4 ? Sqrt2 : 1) + extra;
                    var seen = s.Stamp[nt] == gen || s.Stamp[nt] == -gen;
                    if (seen && s.G[nt] <= ng) continue;
                    if (s.Stamp[nt] == -gen) continue;
                    s.Stamp[nt] = gen;
                    s.G[nt] = ng;
                    s.Parent[nt] = t;
                    Push(s, ref n, nt, ng + Heuristic(nx, ny, gx, gy));
                }
            }
            return null;
        }

        /// <summary>Reference walk.ts:84 <c>tileOpen</c>, without the Phase C truck occupancy.</summary>
        private static bool TileOpen(SimContext ctx, byte[] solid, int tw, int th, int x, int y)
        {
            if (x < 0 || y < 0 || x >= tw || y >= th) return false;
            if (ctx.Geometry.TileAt(x, y) == TileClass.River) return false;
            if (ctx.Geometry.Solid(x, y)) return false;
            return solid[y * tw + x] == 0;
        }

        /// <summary>Reference walk.ts:85 <c>open</c>: the whole clearance box must be open.</summary>
        private static bool Open(SimContext ctx, byte[] solid, int tw, int th, int x, int y, int clearance)
        {
            for (var dy = -clearance; dy <= clearance; dy++)
                for (var dx = -clearance; dx <= clearance; dx++)
                    if (!TileOpen(ctx, solid, tw, th, x + dx, y + dy)) return false;
            return true;
        }

        /// <summary>Octile distance (reference walk.ts:90).</summary>
        private static double Heuristic(int x, int y, int gx, int gy)
        {
            var dx = Math.Abs(x - gx);
            var dy = Math.Abs(y - gy);
            return Math.Max(dx, dy) + (Sqrt2 - 1) * Math.Min(dx, dy);
        }

        private static void Push(PathScratch s, ref int n, int t, double f)
        {
            var i = n++;
            s.Heap[i] = t;
            s.Hf[i] = f;
            while (i > 0)
            {
                var p = (i - 1) >> 1;
                if (s.Hf[p] <= f) break;
                s.Heap[i] = s.Heap[p];
                s.Hf[i] = s.Hf[p];
                i = p;
            }
            s.Heap[i] = t;
            s.Hf[i] = f;
        }

        private static int Pop(PathScratch s, ref int n)
        {
            var top = s.Heap[0];
            n--;
            if (n > 0)
            {
                var t = s.Heap[n];
                var f = s.Hf[n];
                var i = 0;
                for (; ; )
                {
                    var c = 2 * i + 1;
                    if (c >= n) break;
                    if (c + 1 < n && s.Hf[c + 1] < s.Hf[c]) c++;
                    if (s.Hf[c] >= f) break;
                    s.Heap[i] = s.Heap[c];
                    s.Hf[i] = s.Hf[c];
                    i = c;
                }
                s.Heap[i] = t;
                s.Hf[i] = f;
            }
            return top;
        }

        private static List<TilePoint> Reconstruct(PathScratch s, int start, int goal, int tw)
        {
            var len = 0;
            for (var c = goal; c != start; c = s.Parent[c]) len++;
            var path = new List<TilePoint>(len);
            for (var i = 0; i < len; i++) path.Add(default);
            var w = len - 1;
            for (var c = goal; c != start; c = s.Parent[c])
            {
                var x = c % tw;
                path[w--] = new TilePoint(x, (c - x) / tw);
            }
            return path;
        }
    }
}
