using System;

namespace Relight.Sim
{
    /// <summary>Whether it is day or night, and how far through (reference: the renderer's daylight curve).</summary>
    public readonly struct DaylightView
    {
        /// <summary>Sim seconds since the start of the current day, in [0, daySeconds).</summary>
        public readonly double TimeOfDay;
        /// <summary>The day number the clock shows, 1-based (the same day <c>StatusPanelViewModel.FormatClock</c> prints).</summary>
        public readonly int Day;
        /// <summary>True while the sun is up: <c>TimeOfDay &lt; daylightSeconds</c>.</summary>
        public readonly bool IsDay;
        /// <summary>0 fully dark, 1 full daylight. 1 through the day, falling to 0 across the night's first half and rising back over its second.</summary>
        public readonly double Daylight;

        public DaylightView(double timeOfDay, int day, bool isDay, double daylight)
        {
            TimeOfDay = timeOfDay; Day = day; IsDay = isDay; Daylight = daylight;
        }
    }

    /// <summary>
    /// The lit world as the rest of the game reads it (reference flow.ts <c>litAt</c> and light.ts <c>lightMask</c>).
    ///
    /// <see cref="LitAt(SimState,int,int)"/> is a pure function of the state on purpose: C-06's enemy hesitation rule
    /// runs in the Combat slot, where only <c>st</c> is convenient, and the coordinator's wave-2 binding note asks
    /// for exactly that signature. Before the first build there is no mask at all and every tile reads unlit, which
    /// is the safe default for a rule that makes enemies hesitate in light.
    ///
    /// REL-9 (INT-05): everything here READS. Only the sim builds the mask, at fixed points: a new game
    /// (<see cref="LightInitializer"/>), a state attached to a simulation (<see cref="Simulation.Wrap"/>, which is
    /// how every load arrives), the start of the Combat slot (<see cref="LightMaskPhase"/>) and the end of the tick
    /// (<see cref="LightPhase"/>). There used to be a <c>Mask(ctx, st)</c> and a <c>LitAt(ctx, st, x, y)</c> that
    /// rebuilt a stale mask "for presentation"; a frame drawn between a command and the next tick then gave the
    /// combat slot light a headless run did not have yet. They are gone, so presentation can only read.
    /// </summary>
    public static class LightQueries
    {
        /// <summary>Is this tile lit? Pure: reads the cached mask only, never rebuilds it. Unlit outside the map.</summary>
        public static bool LitAt(SimState st, int tx, int ty)
        {
            var s = st?.Light;
            if (s == null || !s.Built || s.Mask == null) return false;
            if (tx < 0 || ty < 0 || tx >= s.W || ty >= s.H) return false;
            return s.Mask[ty * s.W + tx] != 0;
        }

        /// <summary>
        /// The lit mask, one byte per tile at <c>ty * Width + tx</c>: 1 lit, 0 unlit. Null before the first build.
        /// The buffer belongs to the simulation and is overwritten in place — read it, never keep it.
        /// </summary>
        public static byte[] Mask(SimState st) => st?.Light != null && st.Light.Built ? st.Light.Mask : null;

        /// <summary>The mask's dimensions, or (0, 0) before the first build.</summary>
        public static (int Width, int Height) MaskSize(SimState st) =>
            st?.Light != null && st.Light.Built ? (st.Light.W, st.Light.H) : (0, 0);

        /// <summary>How many times the mask has been stamped since the state was created (a test and profiling hook).</summary>
        public static int Builds(SimState st) => st?.Light?.Builds ?? 0;

        /// <summary>Day/night now, from GameData.Time. The port's data has daylightSeconds 0 (U-D-58), so this is dark unless the admin override forces otherwise.</summary>
        public static DaylightView Daylight(SimContext ctx, SimState st)
        {
            var natural=Daylight(st?.T ?? 0, ctx?.Data?.Time?.DaySeconds ?? 0, ctx?.Data?.Time?.DaylightSeconds ?? 0);
            return st != null && st.Admin.Lighting >= 0
                ? new DaylightView(natural.TimeOfDay,natural.Day,st.Admin.Lighting==1,st.Admin.Lighting) : natural;
        }

        /// <summary>
        /// Day/night at an arbitrary time. Pure, so a test and the renderer's look-ahead can both call it.
        /// The day boundary is <c>t mod daySeconds</c>; the sun is up below <paramref name="daylightSeconds"/>, so
        /// t = 0 is day, t = daylightSeconds is the first dark instant and t = daySeconds is dawn of the next day.
        /// </summary>
        public static DaylightView Daylight(double t, double daySeconds, double daylightSeconds)
        {
            if (daySeconds <= 0) return new DaylightView(0, 1, true, 1);
            if (daylightSeconds < 0) daylightSeconds = 0;
            if (daylightSeconds > daySeconds) daylightSeconds = daySeconds;

            var day = (int)Math.Floor(t / daySeconds) + 1;
            var tod = t - (day - 1) * daySeconds;
            if (tod < 0) tod = 0;

            // U-D-58: no daylight seconds means no sun. Without this case the formula below would spread one long
            // dusk-and-dawn V across the whole day.
            if (daylightSeconds <= 0) return new DaylightView(tod, day, false, 0);

            var isDay = tod < daylightSeconds;

            double light;
            if (isDay) light = 1;
            else
            {
                var night = daySeconds - daylightSeconds;
                if (night <= 0) light = 1;
                else
                {
                    // Dusk over the night's first half, dawn over its second: 1 -> 0 -> 1.
                    var f = (tod - daylightSeconds) / night;        // 0 at dusk, 1 at dawn
                    light = Math.Abs(f - 0.5) * 2;
                }
            }
            return new DaylightView(tod, day, isDay, light);
        }

        /// <summary>Convenience: true when the sun is down.</summary>
        public static bool IsNight(SimContext ctx, SimState st) => !Daylight(ctx, st).IsDay;
    }
}
