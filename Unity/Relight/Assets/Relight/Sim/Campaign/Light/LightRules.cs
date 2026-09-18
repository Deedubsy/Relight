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
    /// Pure: no state, no context, so the shade test and the light texture can never disagree (light.ts header).
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
