using System;

namespace Relight.Sim
{
    /// <summary>Facing on the tile grid (reference flow.ts `Dir`): 0 N, 1 E, 2 S, 3 W. Sim tile space is Y-down (U-M-14).</summary>
    public enum Dir { N = 0, E = 1, S = 2, W = 3 }

    public static class Dirs
    {
        public static readonly int[] DX = { 0, 1, 0, -1 };
        public static readonly int[] DY = { -1, 0, 1, 0 };
        public static Dir Rotate(Dir d, int quarterTurns) => (Dir)((((int)d + quarterTurns) % 4 + 4) % 4);
        public static Dir Opposite(Dir d) => Rotate(d, 2);
    }

    /// <summary>An integer tile coordinate, Y-down.</summary>
    public readonly struct TilePoint : IEquatable<TilePoint>
    {
        public int X { get; }
        public int Y { get; }
        public TilePoint(int x, int y) { X = x; Y = y; }
        public bool Equals(TilePoint o) => X == o.X && Y == o.Y;
        public override bool Equals(object obj) => obj is TilePoint p && Equals(p);
        public override int GetHashCode() => unchecked(X * 73856093 ^ Y * 19349663);
        public override string ToString() => $"({X},{Y})";
        public static bool operator ==(TilePoint a, TilePoint b) => a.Equals(b);
        public static bool operator !=(TilePoint a, TilePoint b) => !a.Equals(b);
    }

    /// <summary>A continuous tile-space position or vector, Y-down, in tile units. Doubles only inside the sim.</summary>
    public struct Vec2 : IEquatable<Vec2>
    {
        public double X;
        public double Y;
        public Vec2(double x, double y) { X = x; Y = y; }
        public static readonly Vec2 Zero = new Vec2(0, 0);
        public double Length => Math.Sqrt(X * X + Y * Y);
        public bool IsZero => X == 0 && Y == 0;
        public static Vec2 operator +(Vec2 a, Vec2 b) => new Vec2(a.X + b.X, a.Y + b.Y);
        public static Vec2 operator -(Vec2 a, Vec2 b) => new Vec2(a.X - b.X, a.Y - b.Y);
        public static Vec2 operator *(Vec2 a, double s) => new Vec2(a.X * s, a.Y * s);
        public bool Equals(Vec2 o) => X == o.X && Y == o.Y;
        public override bool Equals(object obj) => obj is Vec2 v && Equals(v);
        public override int GetHashCode() => X.GetHashCode() * 31 + Y.GetHashCode();
        public override string ToString() => $"({X},{Y})";
    }

    /// <summary>An axis-aligned tile rectangle (footprint), Y-down, inclusive origin, exclusive far edge.</summary>
    public readonly struct TileRect
    {
        public int X { get; }
        public int Y { get; }
        public int W { get; }
        public int H { get; }
        public TileRect(int x, int y, int w, int h) { X = x; Y = y; W = w; H = h; }
        public bool Contains(int tx, int ty) => tx >= X && ty >= Y && tx < X + W && ty < Y + H;
        public override string ToString() => $"[{X},{Y} {W}x{H}]";
    }
}

namespace Relight.Sim
{
    /// <summary>Reference footprint.ts: splitters are 2×1 facing N/S and 1×2 facing E/W; everything else is square.</summary>
    public static class Footprints
    {
        public static (int w, int h) Dimensions(string kind, Dir dir, int size) =>
            kind == "splitter" ? (((int)dir % 2 == 0) ? (2, 1) : (1, 2)) : (size, size);
    }

    public static class Reach
    {
        /// <summary>Reference ground.ts distToRect: Euclidean distance from a point to an axis-aligned tile rect (0 inside).</summary>
        public static double DistToRect(double px, double py, double x, double y, double w, double h)
        {
            var dx = System.Math.Max(System.Math.Max(x - px, 0), px - (x + w));
            var dy = System.Math.Max(System.Math.Max(y - py, 0), py - (y + h));
            return System.Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
