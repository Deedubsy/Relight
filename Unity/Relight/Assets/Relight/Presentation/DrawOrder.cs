namespace Relight.Presentation
{
    /// <summary>
    /// Sorting orders around the darkness overlay (U-D-58, ALWAYS_DARK_SPEC.md §3). World views sort by Z at order
    /// 0 and sit UNDER the darkness. Feedback about the player's own actions, and anything that must always be
    /// readable, sits ABOVE it. City labels are at 600 (CityPresenter).
    /// </summary>
    public static class DrawOrder
    {
        public const int Darkness = 500;
        /// <summary>A streetlight's glow and lamp head: over the darkness, so a dead lamp can be found in the dark.</summary>
        public const int StreetLampGlow = 504;
        public const int StreetLamp = 505;
        public const int Cable = 510;
        public const int CablePulse = 511;
        public const int MachineCue = 512;
        /// <summary>Cargo dropped at a death (INT-01): over the darkness, so it can be found again where the engineer fell.</summary>
        public const int DroppedCargoRim = 513;
        public const int DroppedCargo = 514;
        public const int PlacementPreview = 520;
        public const int SilhouetteRim = 529;
        public const int Silhouette = 530;
        public const int AttackTell = 531;
        public const int Engineer = 540;
    }
}
