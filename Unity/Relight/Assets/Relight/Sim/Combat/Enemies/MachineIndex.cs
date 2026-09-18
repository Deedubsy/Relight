using System;

namespace Relight.Sim
{
    /// <summary>
    /// Tile → machine lookup, the port of the reference's <c>machineAt(st, x, y)</c> (flow.ts), which is a linear
    /// scan there and is called once per tile of every BFS, every swept projectile step and every breach check.
    ///
    /// DERIVED, NEVER SAVED, keyed on <see cref="SimState.Rev"/> exactly as <c>GroundState.SolidMap</c> is: the
    /// revision is bumped by every placement, removal and rotation, so a stale index cannot survive a change and a
    /// loaded save rebuilds it on first use. Held on <see cref="EnemyState"/> rather than in a static field so the
    /// sim keeps no module-global mutable state (TECHNICAL_ARCHITECTURE.md §10.5).
    /// </summary>
    public sealed class MachineIndex
    {
        private int[] _at;          // tile -> machine index into st.Machines, +1 (0 = empty)
        private int _rev = int.MinValue;
        private int _w, _h;

        private void Rebuild(SimContext ctx, SimState st)
        {
            var w = ctx.Geometry.Width;
            var h = ctx.Geometry.Height;
            if (_at == null || _w != w || _h != h) { _at = new int[w * h]; _w = w; _h = h; }
            else Array.Clear(_at, 0, _at.Length);
            for (var i = 0; i < st.Machines.Count; i++)
            {
                var r = st.Machines[i].Rect;
                for (var y = r.Y; y < r.Y + r.H; y++)
                {
                    if (y < 0 || y >= h) continue;
                    for (var x = r.X; x < r.X + r.W; x++)
                    {
                        if (x < 0 || x >= w) continue;
                        _at[y * w + x] = i + 1;
                    }
                }
            }
            _rev = st.Rev;
        }

        /// <summary>The machine covering the tile, or null. O(1) after the first call of a revision.</summary>
        public Machine At(SimContext ctx, SimState st, int x, int y)
        {
            if (_at == null || _rev != st.Rev || _w != ctx.Geometry.Width || _h != ctx.Geometry.Height) Rebuild(ctx, st);
            if (x < 0 || y < 0 || x >= _w || y >= _h) return null;
            var slot = _at[y * _w + x];
            return slot == 0 ? null : st.Machines[slot - 1];
        }
    }
}
