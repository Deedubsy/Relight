using System;

namespace Relight.Sim
{
    /// <summary>What a light is (reference flow.ts <c>Light['kind']</c>): a disc, or the Floodlight's cone.</summary>
    public enum LightKind
    {
        /// <summary>An authored kerb streetlight; radius <see cref="LightRules.StreetLightRadiusTiles"/>.</summary>
        StreetLight = 0,
        /// <summary>A placed Lamp or Arc lamp; radius from <c>MachineSpec.LightRadiusTiles</c>.</summary>
        Lamp = 1,
        /// <summary>A placed Floodlight; a cone of <c>ConeRangeTiles</c> and <c>ConeHalfAngleRad</c>.</summary>
        Floodlight = 2,
    }

    /// <summary>
    /// One light source for this tick (reference flow.ts <c>Light</c>). A value: nothing here is saved, everything
    /// is derived from the machines and the authored sites by <see cref="LightSources"/>.
    /// <see cref="Tx"/>/<see cref="Ty"/> are tile coordinates exactly as the reference sets them — a streetlight and
    /// a Lamp sit on their tile index, a Floodlight on its footprint centre (flow.ts <c>blockLights</c>).
    /// </summary>
    public readonly struct Light
    {
        public readonly double Tx;
        public readonly double Ty;
        /// <summary>Radius in tiles (a Floodlight's cone range).</summary>
        public readonly double R;
        public readonly LightKind Kind;
        /// <summary>Facing, used by <see cref="LightKind.Floodlight"/> only.</summary>
        public readonly Dir Dir;
        /// <summary>Cone half-angle in radians, used by <see cref="LightKind.Floodlight"/> only.</summary>
        public readonly double HalfAngleRad;
        /// <summary>True when the source is actually burning (powered). Unlit sources are never stamped.</summary>
        public readonly bool Lit;
        /// <summary>The machine id for a Lamp/Floodlight, or 0 for an authored streetlight.</summary>
        public readonly int MachineId;

        public Light(double tx, double ty, double r, LightKind kind, bool lit,
            Dir dir = Dir.N, double halfAngleRad = 0, int machineId = 0)
        {
            Tx = tx; Ty = ty; R = r; Kind = kind; Lit = lit; Dir = dir; HalfAngleRad = halfAngleRad; MachineId = machineId;
        }
    }

    /// <summary>
    /// The light geometry, ported from reference flow.ts <c>lightCovers</c> and light.ts <c>stampLight</c>.
    /// <see cref="Covers"/> is pure: no state, no context, so the shade test and the light texture can never
    /// disagree (light.ts header). Since L-02 the mask also asks what stands between a light and a tile
    /// (<see cref="Stamp(byte[],int,int,in Light,SimContext,SimState)"/>); that is the port's rule, not the reference's.
    ///
    /// Two constants have no row in <see cref="GameData"/> yet and are held here with their reference source, exactly
    /// as the wave-3 W-B report records: the streetlight radius (reference constants.ts:58 <c>STREETLIGHT_RADIUS</c>)
    /// and the per-light draw (the imported region's <c>scalars.lightKw</c>, <c>Unity/Import/home/city.json</c>).
    /// The report carries the <c>PowerTuning</c> patch that moves them into the catalogue; when it lands, these two
    /// constants become the fallbacks that <see cref="StreetLights"/> uses when the record has no value.
    /// </summary>
    public static class LightRules
    {
        /// <summary>Reference flow.ts <c>lightCovers</c>: the same 1e-9 slack on both tests.</summary>
        public const double Epsilon = 1e-9;

        /// <summary>Reference constants.ts:58 <c>STREETLIGHT_RADIUS</c> — the kerb light reaches the street midline (D-B5-4).</summary>
        public const double StreetLightRadiusTiles = 7;

        /// <summary>Draw of one authored streetlight in kW (<c>Unity/Import/home/city.json</c> <c>scalars.lightKw</c> = 2).</summary>
        public const double StreetLightKw = 2;

        /// <summary>Reference flow.ts <c>lightCovers</c>: under a Floodlight the tiles within d² ≤ 2 are lit whatever the facing.</summary>
        public const double ConeOriginGlowD2 = 2;

        /// <summary>
        /// U-D-58, ALWAYS_DARK_SPEC.md §3: a powered light's reach follows the power it actually gets — full at
        /// full throttle, half at the edge of failure. Off is a separate fact (<see cref="Light.Lit"/>).
        /// </summary>
        public static double BrownoutScale(double throttle)
        {
            if (double.IsNaN(throttle)) return 0.5;
            var t = throttle < 0 ? 0 : throttle > 1 ? 1 : throttle;
            return 0.5 + 0.5 * t;
        }

        /// <summary>
        /// Does a light reach a tile? Reference flow.ts <c>lightCovers</c>, line for line: the radius test first, then
        /// the Floodlight's cone test outside the origin glow.
        /// </summary>
        public static bool Covers(in Light l, int tx, int ty)
        {
            var ex = tx - l.Tx;
            var ey = ty - l.Ty;
            var d2 = ex * ex + ey * ey;
            if (d2 > l.R * l.R + Epsilon) return false;
            if (l.Kind == LightKind.Floodlight && d2 > ConeOriginGlowD2)
            {
                var cos = (ex * Dirs.DX[(int)l.Dir] + ey * Dirs.DY[(int)l.Dir]) / Math.Sqrt(d2);
                if (Math.Acos(Math.Max(-1, Math.Min(1, cos))) > l.HalfAngleRad + Epsilon) return false;
            }
            return true;
        }

        /// <summary>
        /// Stamp one lit light into a <paramref name="tw"/> × <paramref name="th"/> byte-per-tile mask.
        /// Reference light.ts <c>stampLight</c>: the bounding box of the radius, each tile tested with
        /// <see cref="Covers"/>, so the cone never leaks outside the disc.
        /// </summary>
        public static void Stamp(byte[] mask, int tw, int th, in Light l)
        {
            if (mask == null || tw <= 0 || th <= 0) return;
            var x0 = Math.Max(0, (int)Math.Floor(l.Tx - l.R));
            var x1 = Math.Min(tw - 1, (int)Math.Ceiling(l.Tx + l.R));
            var y0 = Math.Max(0, (int)Math.Floor(l.Ty - l.R));
            var y1 = Math.Min(th - 1, (int)Math.Ceiling(l.Ty + l.R));
            for (var ty = y0; ty <= y1; ty++)
                for (var tx = x0; tx <= x1; tx++)
                    if (Covers(in l, tx, ty)) mask[ty * tw + tx] = 1;
        }

        /// <summary>
        /// L-02, ALWAYS_DARK_SPEC §5.3 — "what stops a bullet stops light". As <see cref="Stamp(byte[],int,int,in Light)"/>,
        /// but a tile is lit only when the line from the light to it is clear of what <see cref="Sightline"/> calls
        /// opaque: authored solid building tiles, and the player's own Walls and Barricades. No other machine blocks.
        ///
        /// The tiles at both ends are exempt, for the authored mask as well as for walls, so a lamp against a wall
        /// lights its own side, the wall's near face is lit and the ground behind it is not, and a building's facade
        /// catches the light while its far side stays dark.
        ///
        /// Most lights stand in the open, so the line tests are skipped when the bounding box holds no occluder:
        /// that keeps a whole-map rebuild close to the cost it had before blocking.
        /// </summary>
        public static void Stamp(byte[] mask, int tw, int th, in Light l, SimContext ctx, SimState st)
        {
            if (ctx == null || st == null) { Stamp(mask, tw, th, in l); return; }
            StampWindow(mask, 0, 0, tw, tw, th, in l, ctx, st);
        }

        /// <summary>
        /// The blocked stamp, written into a window of the map rather than the whole of it: tile (tx, ty) lands at
        /// <c>mask[(ty - oy) * stride + (tx - ox)]</c>. The mask rebuild passes the whole map (origin 0, 0);
        /// <see cref="LightPreview"/> passes a box around one ghost, so a placement preview is THIS rule and cannot
        /// promise light the mask would not deliver. The caller guarantees the light's bounding box, clipped to the
        /// <paramref name="tw"/> × <paramref name="th"/> map, fits inside the window.
        /// </summary>
        internal static void StampWindow(byte[] mask, int ox, int oy, int stride, int tw, int th, in Light l,
            SimContext ctx, SimState st)
        {
            if (mask == null || tw <= 0 || th <= 0) return;
            var x0 = Math.Max(0, (int)Math.Floor(l.Tx - l.R));
            var x1 = Math.Min(tw - 1, (int)Math.Ceiling(l.Tx + l.R));
            var y0 = Math.Max(0, (int)Math.Floor(l.Ty - l.R));
            var y1 = Math.Min(th - 1, (int)Math.Ceiling(l.Ty + l.R));

            // The occluders inside the box, read once into a local grid: the line tests below then cost an array
            // read per sample instead of an interface call and a wall-mask lookup.
            var bw = x1 - x0 + 1;
            var bh = y1 - y0 + 1;
            if (bw <= 0 || bh <= 0) return;
            Span<bool> occ = bw * bh <= 1024 ? stackalloc bool[bw * bh] : new bool[bw * bh];
            var any = false;
            for (var ty = y0; ty <= y1; ty++)
                for (var tx = x0; tx <= x1; tx++)
                {
                    var o = Opaque(ctx, st, tx, ty);
                    occ[(ty - y0) * bw + (tx - x0)] = o;
                    any |= o;
                }

            // A Lamp and a streetlight sit on a tile index, so the light leaves that tile's centre; a Floodlight's
            // Tx/Ty is already its footprint centre (flow.ts blockLights).
            var sx = l.Kind == LightKind.Floodlight ? l.Tx : l.Tx + 0.5;
            var sy = l.Kind == LightKind.Floodlight ? l.Ty : l.Ty + 0.5;
            var fx = (int)Math.Floor(sx);
            var fy = (int)Math.Floor(sy);

            for (var ty = y0; ty <= y1; ty++)
                for (var tx = x0; tx <= x1; tx++)
                {
                    if (!Covers(in l, tx, ty)) continue;
                    if (any && !LineClear(occ, x0, y0, bw, bh, sx, sy, fx, fy, tx, ty)) continue;
                    mask[(ty - oy) * stride + (tx - ox)] = 1;
                }
        }

        /// <summary>Does this tile stop light? The same set <see cref="Sightline.Clear"/> stops a shot with.</summary>
        public static bool Opaque(SimContext ctx, SimState st, int tx, int ty) =>
            ctx.Geometry.Solid(tx, ty) || st.Walls.At(ctx, st, tx, ty);

        /// <summary>
        /// The reference's segment walk (four samples per tile, <see cref="Segments"/>) from the light to the centre
        /// of the tile, skipping both end tiles. A sample outside the box is off the map's edge and blocks nothing.
        /// </summary>
        private static bool LineClear(Span<bool> occ, int x0, int y0, int bw, int bh,
            double sx, double sy, int fx, int fy, int tx, int ty)
        {
            var ex = tx + 0.5;
            var ey = ty + 0.5;
            var n = Segments.SampleCount(sx, sy, ex, ey);
            for (var i = 1; i < n; i++)
            {
                var p = Segments.Sample(sx, sy, ex, ey, i, n);
                if ((p.X == fx && p.Y == fy) || (p.X == tx && p.Y == ty)) continue;
                var bx = p.X - x0;
                var by = p.Y - y0;
                if (bx < 0 || by < 0 || bx >= bw || by >= bh) continue;
                if (occ[by * bw + bx]) return false;
            }
            return true;
        }

        /// <summary>Fill a rect of the mask (the Home lot's built-in area lighting, light.ts <c>lightMask</c>'s last line).</summary>
        public static void StampRect(byte[] mask, int tw, int th, int x, int y, int w, int h)
        {
            if (mask == null || tw <= 0 || th <= 0 || w <= 0 || h <= 0) return;
            var x0 = Math.Max(0, x);
            var y0 = Math.Max(0, y);
            var x1 = Math.Min(tw - 1, x + w - 1);
            var y1 = Math.Min(th - 1, y + h - 1);
            for (var ty = y0; ty <= y1; ty++)
                for (var tx = x0; tx <= x1; tx++) mask[ty * tw + tx] = 1;
        }
    }
}
