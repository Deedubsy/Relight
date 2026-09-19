namespace Relight.Sim.UI
{
    /// <summary>
    /// How dark unlit ground is DRAWN (U-D-58, ALWAYS_DARK_SPEC.md §3). Picture only: the lit mask, and therefore
    /// every rule, is untouched by any value here. Dark is a rule, not a black screen, so the strength is held
    /// inside limits whatever the scene or the player's setting asks for.
    /// </summary>
    public static class DarknessLook
    {
        /// <summary>Overlay alpha on unlit tiles. Provisional; the owner tunes it by eye.</summary>
        public const double DefaultUnlit = 0.55;
        public const double MinStrength = 0.35;
        public const double MaxStrength = 0.70;
        /// <summary>The settings slider's middle, which leaves the authored strength alone.</summary>
        public const double DefaultBrightness = 0.5;
        /// <summary>How far the slider can move the strength in total, end to end.</summary>
        public const double BrightnessSpan = 0.35;

        public static double Strength(double unlit, double brightness)
        {
            if (double.IsNaN(unlit)) unlit = DefaultUnlit;
            if (double.IsNaN(brightness)) brightness = DefaultBrightness;
            var b = brightness < 0 ? 0 : brightness > 1 ? 1 : brightness;
            var s = unlit + (DefaultBrightness - b) * BrightnessSpan;
            return s < MinStrength ? MinStrength : s > MaxStrength ? MaxStrength : s;
        }
    }
}
