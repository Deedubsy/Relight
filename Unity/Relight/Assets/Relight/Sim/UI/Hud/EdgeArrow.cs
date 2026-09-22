using System;

namespace Relight.Sim.UI
{
    /// <summary>Where the raid arrow sits and which way it points, in panel pixels (y grows downwards).</summary>
    public readonly struct EdgeArrowPlacement
    {
        /// <summary>The arrow head's centre.</summary>
        public readonly double X, Y;
        /// <summary>The way the head points: 0 is right, 90 is down, as a UI rotation reads it.</summary>
        public readonly double AngleDeg;
        /// <summary>True when the point itself is inside the box, so the arrow stands over it and points down.</summary>
        public readonly bool OnScreen;
        /// <summary>False for a point or box that cannot be placed (not a number, or a box with no room).</summary>
        public readonly bool Valid;

        public EdgeArrowPlacement(double x, double y, double angleDeg, bool onScreen, bool valid)
        {
            X = x; Y = y; AngleDeg = angleDeg; OnScreen = onScreen; Valid = valid;
        }
    }

    /// <summary>
    /// REL-74 (E-20): the raid arrow's geometry, engine-free so <c>Tests/Sim/UI/RaidFeedbackTests.cs</c> can hold it
    /// to "points at the raid from anywhere on the map".
    ///
    /// The box is the part of the screen the world shows through: the HUD's columns and strips are outside it, so the
    /// arrow never sits on a readout. A point inside the box gets the arrow over it, pointing down at it. A point
    /// outside is pinned to the box's edge on the line from the box's centre to the point, pointing along that line,
    /// so wherever the player stands the arrow turns towards the raid.
    /// </summary>
    public static class EdgeArrow
    {
        /// <summary>Pixels the head stands above a point it can see, so it does not cover what it points at.</summary>
        public const double StandOff = 30;

        public static EdgeArrowPlacement Place(double px, double py, double left, double top, double right, double bottom)
        {
            if (double.IsNaN(px) || double.IsNaN(py) || double.IsInfinity(px) || double.IsInfinity(py)
                || !(right > left) || !(bottom > top))
                return new EdgeArrowPlacement(0, 0, 0, false, false);

            if (px >= left && px <= right && py >= top && py <= bottom)
                return new EdgeArrowPlacement(px, Math.Max(top, py - StandOff), 90, true, true);

            var cx = (left + right) / 2;
            var cy = (top + bottom) / 2;
            var dx = px - cx;
            var dy = py - cy;
            // The first box side the line from the centre crosses. The point is outside, so at least one of the two
            // is finite and the smaller lies in (0, 1).
            var tx = dx > 0 ? (right - cx) / dx : dx < 0 ? (left - cx) / dx : double.PositiveInfinity;
            var ty = dy > 0 ? (bottom - cy) / dy : dy < 0 ? (top - cy) / dy : double.PositiveInfinity;
            var t = Math.Min(tx, ty);
            var angle = Math.Atan2(dy, dx) * 180 / Math.PI;
            return new EdgeArrowPlacement(cx + dx * t, cy + dy * t, angle, false, true);
        }
    }
}
