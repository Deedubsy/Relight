using System;

namespace Relight.Sim
{
    /// <summary>
    /// Which tiles a player-built wall or barricade occupies, rebuilt only when the world's structure changes.
    /// Keyed on <see cref="SimState.Rev"/>, the same way <see cref="RaidFieldCache"/> is: derived, never visited,
    /// so a save carries nothing about it and a load rebuilds it on the first question anyone asks.
    /// </summary>
    public sealed class WallMask
    {
        private int _rev = int.MinValue;
        private int _w, _h;
        private bool[] _bits;
        private bool _any;

        /// <summary>True when the world currently has no wall or barricade at all — the common case, checked first.</summary>
        public bool Empty(SimContext ctx, SimState st) { Ensure(ctx, st); return !_any; }

        public bool At(SimContext ctx, SimState st, int x, int y)
        {
            Ensure(ctx, st);
            if (!_any || x < 0 || y < 0 || x >= _w || y >= _h) return false;
            return _bits[y * _w + x];
        }

        private void Ensure(SimContext ctx, SimState st)
        {
            var w = ctx.Geometry.Width;
            var h = ctx.Geometry.Height;
            if (_rev == st.Rev && _w == w && _h == h && _bits != null) return;
            if (_bits == null || _w != w || _h != h) { _bits = new bool[w * h]; _w = w; _h = h; }
            else Array.Clear(_bits, 0, _bits.Length);
            _rev = st.Rev;
            _any = false;
            for (var i = 0; i < st.Machines.Count; i++)
            {
                var m = st.Machines[i];
                if (!Sightline.Opaque(m.Kind)) continue;
                // GP-W5: a wrecked wall stops nothing. GroundState.SolidMap lets a body walk over it, so a shot
                // must pass over it too — sight and movement have to give the same answer or a turret refuses to
                // fire at a raider standing in a gap it can see with its own eyes.
                if (TurretRules.Wrecked(ctx.Data, st, m)) continue;
                var r = m.Rect;
                for (var y = r.Y; y < r.Y + r.H; y++)
                    for (var x = r.X; x < r.X + r.W; x++)
                        if (x >= 0 && y >= 0 && x < w && y < h) { _bits[y * w + x] = true; _any = true; }
            }
        }
    }

    /// <summary>
    /// The ONE line-of-sight rule for anything that shoots (GP-W3, "keep wall, projectile and sight behaviour
    /// consistent").
    ///
    /// <see cref="ICityGeometry.Sight"/> knows only the authored building mask, because it is given no
    /// <see cref="SimState"/> and so cannot see a wall the player built this minute. The player's own bullets did
    /// not have that problem — <c>Ballistics.Cast</c> carried its own wall test (reference playerBallistics.ts:22)
    /// — so a wall stopped the engineer's rifle while turrets, and the placement preview's range ring, shot
    /// straight through it. That is the inconsistency the brief names: one rule now, used by the turret that
    /// fires, by the bullet that flies, and by the preview that promises coverage before either exists.
    /// </summary>
    public static class Sightline
    {
        /// <summary>Reference playerBallistics.ts:22 — a wall or barricade stops a shot; no other machine does.</summary>
        public static bool Opaque(string kind) =>
            string.Equals(kind, "wall", StringComparison.Ordinal) ||
            string.Equals(kind, "barricade", StringComparison.Ordinal);

        /// <summary>
        /// Can a shot from (<paramref name="x0"/>,<paramref name="y0"/>) reach
        /// (<paramref name="x1"/>,<paramref name="y1"/>)? Authored geometry first, then the player's own walls.
        ///
        /// The tiles the two ENDS stand on are exempt, which is the reference's own rule and not a convenience: a
        /// turret placed hard against its wall must still be able to fire over it, and a body standing on a
        /// barricade tile must still be hittable. Only what is BETWEEN them blocks.
        /// </summary>
        public static bool Clear(SimContext ctx, SimState st, double x0, double y0, double x1, double y1)
        {
            if (!ctx.Geometry.Sight(x0, y0, x1, y1)) return false;
            if (st == null || st.Walls.Empty(ctx, st)) return true;
            var sx = (int)Math.Floor(x0); var sy = (int)Math.Floor(y0);
            var ex = (int)Math.Floor(x1); var ey = (int)Math.Floor(y1);
            var n = Segments.SampleCount(x0, y0, x1, y1);
            for (var i = 1; i < n; i++)
            {
                var p = Segments.Sample(x0, y0, x1, y1, i, n);
                if ((p.X == sx && p.Y == sy) || (p.X == ex && p.Y == ey)) continue;
                if (st.Walls.At(ctx, st, p.X, p.Y)) return false;
            }
            return true;
        }
    }

    public sealed partial class SimState
    {
        /// <summary>Wall and barricade tiles, keyed on <see cref="Rev"/> (see <see cref="WallMask"/>). Never visited.</summary>
        public readonly WallMask Walls = new WallMask();
    }
}
