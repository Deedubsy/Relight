using System;

namespace Relight.Sim
{
    /// <summary>
    /// Pure tile-space segment helpers shared by line-of-sight and reach (reference ground.ts:641 <c>citySight</c>).
    /// No state, no allocation; sampling is the reference's exact scheme so results match tile for tile.
    /// </summary>
    public static class Segments
    {
        /// <summary>
        /// Number of samples the reference takes along a segment: <c>Math.ceil(Math.hypot(bx-ax, by-ay) * 4)</c>
        /// (ground.ts:643). Four samples per tile of length.
        /// </summary>
        public static int SampleCount(double ax, double ay, double bx, double by)
        {
            var dx = bx - ax;
            var dy = by - ay;
            return (int)Math.Ceiling(Math.Sqrt(dx * dx + dy * dy) * 4);
        }

        /// <summary>
        /// The i-th sampled tile along the segment, <c>floor(a + (b-a) * i / n)</c> (ground.ts:645).
        /// The reference walks <c>i = 1 .. n-1</c>, so both endpoints' own tiles are skipped by the caller's loop
        /// bounds, not by a special case.
        /// </summary>
        public static TilePoint Sample(double ax, double ay, double bx, double by, int i, int n)
        {
            var x = (int)Math.Floor(ax + (bx - ax) * i / n);
            var y = (int)Math.Floor(ay + (by - ay) * i / n);
            return new TilePoint(x, y);
        }

        /// <summary>
        /// Walks the interior samples of a segment and returns false as soon as <paramref name="blocked"/> says a
        /// sampled tile blocks. This is the shape of <c>citySight</c> minus the Phase C parts (wall/barricade
        /// machines and the freight gate) — see <see cref="ArrayGeometry.Sight"/>.
        /// </summary>
        public static bool Clear(double ax, double ay, double bx, double by, Func<int, int, bool> blocked)
        {
            var n = SampleCount(ax, ay, bx, by);
            for (var i = 1; i < n; i++)
            {
                var p = Sample(ax, ay, bx, by, i, n);
                if (blocked(p.X, p.Y)) return false;
            }
            return true;
        }
    }
}
