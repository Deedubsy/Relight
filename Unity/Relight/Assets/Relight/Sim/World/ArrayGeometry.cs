using System;

namespace Relight.Sim
{
    /// <summary>
    /// The injected world as flat arrays (U-M-14: geometry is data, never compiled into the sim). Row-major, Y-down,
    /// <c>t = y * Width + x</c>. Phase C's riverfront exporter produces the same two arrays from
    /// <c>city/riverfront.ts</c> (<c>cg.kind</c> → <see cref="TileClass"/>, the per-building wall/door loop →
    /// <see cref="ICityGeometry.Solid"/>); Phase B feeds it from <see cref="SyntheticMap"/>.
    ///
    /// Immutable once constructed: authored geometry never changes during a tick. Runtime change (digging rubble,
    /// placing machines) lives in <see cref="GroundState"/> and <see cref="SimState.Machines"/>, exactly as the
    /// reference keeps <c>G.base</c> authored and <c>G.dug</c>/<c>flow.machines</c> live.
    /// </summary>
    public sealed class ArrayGeometry : ICityGeometry
    {
        private readonly byte[] _kind;
        private readonly bool[] _solid;

        public int Width { get; }
        public int Height { get; }
        public Vec2 Spawn { get; }

        /// <param name="kind">Tile classes, length <c>width*height</c>, row-major Y-down. Not copied; do not mutate.</param>
        /// <param name="solid">Building collision mask, same layout. May be null for a map with no buildings.</param>
        public ArrayGeometry(int width, int height, byte[] kind, bool[] solid, Vec2 spawn)
        {
            if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (kind == null) throw new ArgumentNullException(nameof(kind));
            if (kind.Length != width * height) throw new ArgumentException("kind length must be width*height", nameof(kind));
            if (solid != null && solid.Length != width * height) throw new ArgumentException("solid length must be width*height", nameof(solid));
            Width = width;
            Height = height;
            _kind = kind;
            _solid = solid ?? new bool[width * height];
            Spawn = spawn;
        }

        /// <summary>Reference ground.ts:47 <c>inGround</c>.</summary>
        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

        public TileClass TileAt(int x, int y) =>
            InBounds(x, y) ? (TileClass)_kind[y * Width + x] : TileClass.Void;

        public bool Solid(int x, int y) => InBounds(x, y) && _solid[y * Width + x];

        /// <summary>
        /// Reference ground.ts:641 <c>citySight</c>: sample the segment four times per tile and fail on the first
        /// sample that is off-map or on a solid building tile.
        ///
        /// Phase C parts deliberately not implemented here, because the state they read does not exist yet:
        /// the freight-gate check (<c>st.campaign.progression.gameplay.strongholds.freight</c>) and the
        /// wall/barricade machine set keyed on <c>st.flow.rev</c> with its start/end-tile exemption. Both need
        /// <see cref="SimState"/>, which <see cref="ICityGeometry.Sight"/> does not receive; when Phase C adds
        /// machine <c>Hp</c> and campaign progression, sight over machines belongs in a state-aware wrapper.
        /// Until then a wall machine does not block sight (see the B-08 report's limitations).
        /// </summary>
        public bool Sight(double x0, double y0, double x1, double y1)
        {
            var n = Segments.SampleCount(x0, y0, x1, y1);
            for (var i = 1; i < n; i++)
            {
                var p = Segments.Sample(x0, y0, x1, y1, i, n);
                if (!InBounds(p.X, p.Y)) return false;
                if (_solid[p.Y * Width + p.X]) return false;
            }
            return true;
        }
    }
}
