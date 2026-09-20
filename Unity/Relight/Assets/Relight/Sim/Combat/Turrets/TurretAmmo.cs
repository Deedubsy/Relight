namespace Relight.Sim
{
    /// <summary>How much ammunition a turret holds, as the player needs to read it (U-D-61).</summary>
    public enum TurretAmmoState
    {
        /// <summary>A quarter of the hopper or more, or not a turret at all.</summary>
        Ok = 0,
        /// <summary>Under <see cref="TurretAmmo.LowFraction"/> of the hopper, but it can still fire.</summary>
        Low = 1,
        /// <summary>Not one round left.</summary>
        Dry = 2,
    }

    /// <summary>
    /// E-17 (U-D-61): the two ammunition states a turret shows in the world and the HUD reports when it matters.
    /// This replaces the C-04 choice that an empty but powered turret reads as "running" because the hopper gauge
    /// shows ammunition separately: that gauge is in the inventory panel, not in the world.
    ///
    /// Read-only and derived from saved state alone, so a loaded game answers the same as the one that was saved.
    /// </summary>
    public static class TurretAmmo
    {
        /// <summary>U-P-14: low is UNDER this share of the hopper. Provisional; the data row wins when it has one.</summary>
        public const double DefaultLowFraction = 0.25;

        public static double LowFraction(GameData d) =>
            d?.Defence != null && d.Defence.LowAmmoFraction > 0 ? d.Defence.LowAmmoFraction : DefaultLowFraction;

        /// <summary>
        /// Dry at 0 rounds; low under a quarter of the hopper (12 of the Gun turret's 50 is low, 13 is not, because
        /// a quarter is 12.5). The hopper is <see cref="TurretHopper.Capacity"/>, so an upgraded hopper moves the line.
        /// </summary>
        public static TurretAmmoState State(GameData d, Machine m)
        {
            if (d == null || m == null || !TurretHopper.IsTurret(d, m)) return TurretAmmoState.Ok;
            if (m.Rounds < 1) return TurretAmmoState.Dry;
            var cap = TurretHopper.Capacity(d, m);
            return cap > 0 && m.Rounds < cap * LowFraction(d) ? TurretAmmoState.Low : TurretAmmoState.Ok;
        }

        /// <summary>
        /// "Has this turret ever been loaded?" — FOUND, not added (the <see cref="UI.PowerAlertSource.EverHadPower"/>
        /// pattern): it holds a round now, or it has fired one (<see cref="TurretUnit.ShotT"/> is saved and is 0 until
        /// the first shot). A turret the player placed and has not fed yet is empty by plan, not running dry, and
        /// the HUD stays silent about it. One the player loaded and then emptied by hand without a shot reads as
        /// never loaded too, which is the player's own doing and needs no notice.
        /// </summary>
        public static bool EverLoaded(SimState st, Machine m)
        {
            if (st == null || m == null) return false;
            if (m.Rounds >= 1) return true;
            var u = st.Turrets.Find(m.Id);
            return u != null && u.ShotT > 0;
        }
    }
}
