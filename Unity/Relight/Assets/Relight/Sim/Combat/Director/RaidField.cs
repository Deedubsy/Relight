using System;
using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>
    /// A breadth-first distance field toward one rectangle, the port of campaignThreat.ts <c>field()</c>.
    ///
    /// DIFFERENCE FROM THE REFERENCE (memory): the reference allocates one <c>Int32Array</c> the size of the whole
    /// map per field and caches up to 32 of them. Its <c>open()</c> rejects anything more than 70 tiles from the
    /// seed's top-left tile, so the port stores only the box the field can occupy and translates on lookup.
    ///
    /// DIFFERENCE FROM THE REFERENCE (behaviour, 2026-09-18): the box is now <see cref="Reach"/> tiles beyond every
    /// EDGE of the seed rectangle, not 70 tiles from its top-left corner. The reference's 70 was never derived from
    /// anything; it happened to cover the reference city's Home raid line (row 391, 44 rows below the core) and
    /// nothing more. On the authored Founders Court the raid line sits 85 rows below the core's top row and 72
    /// below its last row, so a 70-tile box measured from the top-left corner ended at row 417, every tile at or
    /// below the raid line read -1, no legal entry tile existed, and the introductory attack was born INSIDE the
    /// court (see DirectorRules.Origin). The box is now derived from the one rule that actually bounds where a wave
    /// may enter, <see cref="DirectorRules.EntryFarSteps"/>, plus the 12 steps <see cref="DirectorRules.Staging"/>
    /// may search outward from an entry tile. Every distance the reference reported is still reported; distances
    /// in the extra ring are new, and everything outside the box is -1 in both.
    ///
    /// L-02, ALWAYS_DARK_SPEC §5.2 (approach preference): the field now carries a second number per tile,
    /// <see cref="Cost"/>, over exactly the same open tiles. Stepping onto an unlit tile costs 1 and onto a lit
    /// tile <c>RaidTuning.LitStepCost</c> (4), so a body that walks down <see cref="Cost"/> takes the dark way
    /// round when there is one. The extra cost is finite and the open set is unchanged, so <see cref="Cost"/> is
    /// reachable wherever <see cref="At"/> is: a fully lit base is slower to approach, never unreachable.
    /// <see cref="At"/> still counts plain steps, because the entry bands, staging and the opening origin are all
    /// written in steps. With nothing lit inside the box the two are the same array.
    /// </summary>
    public sealed class RaidField
    {
        /// <summary>
        /// How far beyond the seed rectangle's edges the field is computed, in tiles: the furthest a wave may enter
        /// (<see cref="DirectorRules.EntryFarSteps"/> BFS steps, which is at most that many tiles) plus the 12-step
        /// outward search <see cref="DirectorRules.Staging"/> makes from an entry tile. A raid line further than
        /// this from the core can never be reached and the director will say so (-1) rather than guess.
        /// </summary>
        public const int Reach = DirectorRules.EntryFarSteps + 12;

        /// <summary>Box origin in tiles (already clipped to the map).</summary>
        public readonly int X0, Y0, BW, BH;
        private readonly int[] _dist;
        private readonly int[] _cost;
        /// <summary>Global tile indices of the seed ring (distance 0), in scan order.</summary>
        public readonly int[] Targets;

        internal RaidField(int x0, int y0, int bw, int bh, int[] dist, int[] cost, int[] targets)
        {
            X0 = x0; Y0 = y0; BW = bw; BH = bh; _dist = dist; _cost = cost ?? dist; Targets = targets;
        }

        /// <summary>Steps to the seed rectangle from (x, y), or -1 when it cannot be reached inside the box.</summary>
        public int At(int x, int y)
        {
            var bx = x - X0;
            var by = y - Y0;
            if (bx < 0 || by < 0 || bx >= BW || by >= BH) return -1;
            return _dist[by * BW + bx];
        }

        /// <summary>
        /// The light-weighted cost to the seed rectangle from (x, y): 1 per unlit tile entered, LitStepCost per lit
        /// one. -1 exactly where <see cref="At"/> is -1. This is what a marching body descends (§5.2).
        /// </summary>
        public int Cost(int x, int y)
        {
            var bx = x - X0;
            var by = y - Y0;
            if (bx < 0 || by < 0 || bx >= BW || by >= BH) return -1;
            return _cost[by * BW + bx];
        }

        /// <summary>How much of the cheapest route's cost is light: 0 on a route that never touches a lit tile.</summary>
        public int LightPenalty(int x, int y)
        {
            var c = Cost(x, y);
            return c < 0 ? 0 : c - At(x, y);
        }

        /// <summary>True when light shaped this field at all (a test and profiling hook).</summary>
        public bool Weighted => !ReferenceEquals(_cost, _dist);

        /// <summary>The same, addressed by global tile index.</summary>
        public int AtTile(int tile, int worldWidth) => tile < 0 ? -1 : At(tile % worldWidth, tile / worldWidth);
    }

    /// <summary>
    /// The field cache. Keyed on <see cref="SimState.Rev"/> exactly as the reference keys its <c>WeakMap</c> on
    /// <c>flow.rev</c>, and cleared when it passes 32 entries — the reference's own bound, kept so a long game
    /// cannot accumulate fields. Held on <see cref="DirectorState"/> (never visited), not in a static field.
    ///
    /// L-02 (§5.2): it is also keyed on <see cref="LightState.Builds"/>, the count of real mask rebuilds. A lamp
    /// switching on, a brownout shrinking a radius and a generator running dry all change the lit picture without
    /// moving Rev, and a route field that still believed in the old light would walk a wave down a lane the player
    /// has just lit.
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
        private int _light = int.MinValue;

        /// <summary>Reference campaignThreat.ts:97 <c>field(st, x, y, size, breach, clearance)</c>.</summary>
        public RaidField Field(SimContext ctx, SimState st, int x, int y, int size, bool breach, int clearance = 0)
        {
            var light = st.Light.Builds;
            if (_rev != st.Rev || _light != light) { _map.Clear(); _rev = st.Rev; _light = light; }
            var key = new Key(x, y, size, breach, clearance);
            if (_map.TryGetValue(key, out var hit)) return hit;
            var built = Build(ctx, st, x, y, size, breach, clearance);
            if (_map.Count > 32) _map.Clear();
            _map[key] = built;
            return built;
        }

        private static RaidField Build(SimContext ctx, SimState st, int x, int y, int size, bool breach, int clearance)
        {
            var w = ctx.Geometry.Width;
            var h = ctx.Geometry.Height;
            // The seed rectangle's last tile on each axis; a point seed (size 0) is its own last tile.
            var xLast = x + Math.Max(size, 1) - 1;
            var yLast = y + Math.Max(size, 1) - 1;
            var x0 = Math.Max(0, x - RaidField.Reach);
            var y0 = Math.Max(0, y - RaidField.Reach);
            var x1 = Math.Min(w - 1, xLast + RaidField.Reach);
            var y1 = Math.Min(h - 1, yLast + RaidField.Reach);
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
                if (xx < x - RaidField.Reach || xx > xLast + RaidField.Reach || yy < y - RaidField.Reach || yy > yLast + RaidField.Reach) return false;
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

            return new RaidField(x0, y0, bw, bh, dist, Weigh(ctx, st, x0, y0, bw, bh, dist, targets, w), targets.ToArray());
        }

        /// <summary>
        /// The light-weighted cost over the tiles the BFS already proved open (dist ≥ 0), by Dijkstra from the same
        /// seed ring. Returns null — "use the step field" — when no open tile in the box is lit, which is every
        /// field in a test that has no light and most fields far from the player's base.
        /// </summary>
        private static int[] Weigh(SimContext ctx, SimState st, int x0, int y0, int bw, int bh, int[] dist, List<int> targets, int worldWidth)
        {
            var lit = Math.Max(1, ctx.Data.Raids.LitStepCost);
            if (lit <= 1 || !st.Light.HasMask) return null;
            var weight = new byte[dist.Length];
            var any = false;
            for (var by = 0; by < bh; by++)
                for (var bx = 0; bx < bw; bx++)
                {
                    var b = by * bw + bx;
                    if (dist[b] < 0) continue;
                    if (LightQueries.LitAt(st, bx + x0, by + y0)) { weight[b] = 1; any = true; }
                }
            if (!any) return null;

            var cost = new int[dist.Length];
            for (var i = 0; i < cost.Length; i++) cost[i] = -1;
            // A binary heap of (cost << 32 | box index). Stale entries are skipped when popped.
            var heap = new List<long>(targets.Count * 4 + 16);
            void Push(int c, int b)
            {
                heap.Add(((long)c << 32) | (uint)b);
                var i = heap.Count - 1;
                while (i > 0)
                {
                    var up = (i - 1) >> 1;
                    if (heap[up] <= heap[i]) break;
                    var t = heap[up]; heap[up] = heap[i]; heap[i] = t;
                    i = up;
                }
            }
            long Pop()
            {
                var top = heap[0];
                var last = heap.Count - 1;
                heap[0] = heap[last];
                heap.RemoveAt(last);
                var i = 0;
                while (true)
                {
                    var l = i * 2 + 1;
                    if (l >= heap.Count) break;
                    var r = l + 1;
                    var m = r < heap.Count && heap[r] < heap[l] ? r : l;
                    if (heap[i] <= heap[m]) break;
                    var t = heap[m]; heap[m] = heap[i]; heap[i] = t;
                    i = m;
                }
                return top;
            }

            for (var i = 0; i < targets.Count; i++)
            {
                var tx = targets[i] % worldWidth - x0;
                var ty = targets[i] / worldWidth - y0;
                var b = ty * bw + tx;
                if (b < 0 || b >= cost.Length || cost[b] == 0) continue;
                cost[b] = 0;
                Push(0, b);
            }
            var done = new bool[dist.Length];
            while (heap.Count > 0)
            {
                var top = Pop();
                var t = (int)(top & 0xffffffff);
                if (done[t]) continue;
                done[t] = true;
                var c = (int)(top >> 32);
                var tx = t % bw;
                var ty = t / bw;
                // The field points TOWARD the seed, so a body standing on q pays for entering t, the tile it steps
                // onto: cost[q] = cost[t] + weight of t.
                var step = c + (weight[t] != 0 ? lit : 1);
                for (var k = 0; k < 4; k++)
                {
                    var bx = tx + Dirs.DX[k];
                    var by = ty + Dirs.DY[k];
                    if (bx < 0 || by < 0 || bx >= bw || by >= bh) continue;
                    var q = by * bw + bx;
                    if (dist[q] < 0 || done[q]) continue;
                    if (cost[q] >= 0 && cost[q] <= step) continue;
                    cost[q] = step;
                    Push(step, q);
                }
            }
            return cost;
        }
    }
}
