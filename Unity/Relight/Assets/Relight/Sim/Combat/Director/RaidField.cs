using System;
using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>
    /// A breadth-first distance field toward one rectangle, the port of campaignThreat.ts <c>field()</c>.
    ///
    /// DIFFERENCE FROM THE REFERENCE (memory, not behaviour): the reference allocates one <c>Int32Array</c> the size
    /// of the whole map per field and caches up to 32 of them. Since <c>open()</c> already rejects anything more
    /// than 70 tiles from the seed, the reachable set can never leave a 141×141 box, so the port stores the box
    /// only (79 KB instead of 2 MB on an 864×576 region) and translates on lookup. Every distance the reference
    /// would report is reported here, and everything outside the box is -1 in both.
    /// </summary>
    public sealed class RaidField
    {
        /// <summary>Box origin in tiles (already clipped to the map).</summary>
        public readonly int X0, Y0, BW, BH;
        private readonly int[] _dist;
        /// <summary>Global tile indices of the seed ring (distance 0), in scan order.</summary>
        public readonly int[] Targets;

        internal RaidField(int x0, int y0, int bw, int bh, int[] dist, int[] targets)
        {
            X0 = x0; Y0 = y0; BW = bw; BH = bh; _dist = dist; Targets = targets;
        }

        /// <summary>Steps to the seed rectangle from (x, y), or -1 when it cannot be reached inside the box.</summary>
        public int At(int x, int y)
        {
            var bx = x - X0;
            var by = y - Y0;
            if (bx < 0 || by < 0 || bx >= BW || by >= BH) return -1;
            return _dist[by * BW + bx];
        }

        /// <summary>The same, addressed by global tile index.</summary>
        public int AtTile(int tile, int worldWidth) => tile < 0 ? -1 : At(tile % worldWidth, tile / worldWidth);
    }

    /// <summary>
    /// The field cache. Keyed on <see cref="SimState.Rev"/> exactly as the reference keys its <c>WeakMap</c> on
    /// <c>flow.rev</c>, and cleared when it passes 32 entries — the reference's own bound, kept so a long game
    /// cannot accumulate fields. Held on <see cref="DirectorState"/> (never visited), not in a static field.
    /// </summary>
    public sealed class RaidFieldCache
    {
        private readonly struct Key : IEquatable<Key>
        {
            public readonly int X, Y, Size, Clearance;
            public readonly bool Breach;
            public Key(int x, int y, int size, bool breach, int clearance) { X = x; Y = y; Size = size; Breach = breach; Clearance = clearance; }
            public bool Equals(Key o) => X == o.X && Y == o.Y && Size == o.Size && Breach == o.Breach && Clearance == o.Clearance;
            public override bool Equals(object o) => o is Key k && Equals(k);
            public override int GetHashCode() => unchecked(((X * 397 ^ Y) * 397 ^ Size) * 397 ^ (Clearance * 2 + (Breach ? 1 : 0)));
        }

        private readonly Dictionary<Key, RaidField> _map = new Dictionary<Key, RaidField>();
        private int _rev = int.MinValue;

        /// <summary>Reference campaignThreat.ts:97 <c>field(st, x, y, size, breach, clearance)</c>.</summary>
        public RaidField Field(SimContext ctx, SimState st, int x, int y, int size, bool breach, int clearance = 0)
        {
            if (_rev != st.Rev) { _map.Clear(); _rev = st.Rev; }
            var key = new Key(x, y, size, breach, clearance);
            if (_map.TryGetValue(key, out var hit)) return hit;
            var built = Build(ctx, st, x, y, size, breach, clearance);
            if (_map.Count > 32) _map.Clear();
            _map[key] = built;
            return built;
        }

        private static RaidField Build(SimContext ctx, SimState st, int x, int y, int size, bool breach, int clearance)
        {
            const int Box = 70;
            var w = ctx.Geometry.Width;
            var h = ctx.Geometry.Height;
            var x0 = Math.Max(0, x - Box);
            var y0 = Math.Max(0, y - Box);
            var x1 = Math.Min(w - 1, x + Box);
            var y1 = Math.Min(h - 1, y + Box);
            var bw = Math.Max(0, x1 - x0 + 1);
            var bh = Math.Max(0, y1 - y0 + 1);
            var dist = new int[bw * bh];
            for (var i = 0; i < dist.Length; i++) dist[i] = -1;
            var queue = new int[bw * bh];       // box indices
            var targets = new List<int>();
            var tail = 0;

            bool Open(int xx, int yy)
            {
                if (!Ground.InBounds(ctx, xx, yy)) return false;
                if (Math.Abs(xx - x) > Box || Math.Abs(yy - y) > Box) return false;
                if (!Ground.Walkable(ctx, xx, yy)) return false;
                if (clearance > 0)
                {
                    for (var dy = -clearance; dy <= clearance; dy++)
                        for (var dx = -clearance; dx <= clearance; dx++)
                        {
                            if (DirectorRules.HostileOpen(ctx, st, xx + dx, yy + dy)) continue;
                            var n = st.Enemies.Index.At(ctx, st, xx + dx, yy + dy);
                            if (!breach || n == null || !TurretRules.IsDefence(ctx.Data, n) || !Ground.Walkable(ctx, xx + dx, yy + dy)) return false;
                        }
                }
                if (DirectorRules.HostileOpen(ctx, st, xx, yy)) return true;
                var m = st.Enemies.Index.At(ctx, st, xx, yy);
                return breach && m != null && TurretRules.IsDefence(ctx.Data, m);
            }

            for (var yy = y - 1 - clearance; yy <= y + size + clearance; yy++)
                for (var xx = x - 1 - clearance; xx <= x + size + clearance; xx++)
                {
                    if (size > 0 && xx >= x && xx < x + size && yy >= y && yy < y + size) continue;
                    if (size == 0 && (xx != x || yy != y)) continue;
                    if (!Open(xx, yy)) continue;
                    var b = (yy - y0) * bw + (xx - x0);
                    if (b < 0 || b >= dist.Length || dist[b] == 0) continue;
                    dist[b] = 0;
                    queue[tail++] = b;
                    targets.Add(yy * w + xx);
                }

            for (var head = 0; head < tail; head++)
            {
                var t = queue[head];
                var tx = t % bw + x0;
                var ty = t / bw + y0;
                for (var k = 0; k < 4; k++)
                {
                    var xx = tx + Dirs.DX[k];
                    var yy = ty + Dirs.DY[k];
                    var bx = xx - x0;
                    var by = yy - y0;
                    if (bx < 0 || by < 0 || bx >= bw || by >= bh) continue;
                    var q = by * bw + bx;
                    if (dist[q] >= 0 || !Open(xx, yy)) continue;
                    dist[q] = dist[t] + 1;
                    queue[tail++] = q;
                }
            }

            return new RaidField(x0, y0, bw, bh, dist, targets.ToArray());
        }
    }
}
