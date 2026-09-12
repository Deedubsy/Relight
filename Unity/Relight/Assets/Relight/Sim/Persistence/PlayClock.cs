using System;

namespace Relight.Sim
{
    public sealed partial class SimState
    {
        /// <summary>
        /// Time actually played into this state, in seconds of unpaused simulation (B-11, U-D-41). It is save
        /// <b>header metadata</b>, not gameplay: no rule reads it, it is never <c>Visit</c>ed and so it is outside
        /// the state hash, and <see cref="SaveSerializer"/> carries it in and out of the file's header.
        ///
        /// It is new content this row originates. The reference has no playtime field at all — <c>makeSave</c>
        /// (packages/sim/src/save.ts:128-134) writes version/kind/savedAt/seed/tick/t/hash/params/state/log and
        /// nothing else, and the only real-time counter, <c>Session.realElapsed</c> (packages/game/src/session.ts:137,
        /// accumulated at :259), is never serialised. UI_AND_ONBOARDING.md §2.6.2 forbids labelling sim elapsed
        /// (<see cref="T"/>) as playtime, which is why this is a separate number: <see cref="T"/> is rewound by
        /// loading an earlier save, playtime is what the file records about the session that produced it.
        /// </summary>
        public double PlaySeconds;
    }

    /// <summary>
    /// Advances <see cref="SimState.PlaySeconds"/>. The host adds the unpaused sim time each frame — ticks run
    /// × <see cref="Simulation.TickSeconds"/> — which is the same quantity the autosave interval counts
    /// (TECHNICAL_ARCHITECTURE.md §9.4.2: "unpaused sim time, not wall clock"). A paused game adds nothing because
    /// it runs no ticks.
    /// </summary>
    public static class PlayClock
    {
        /// <summary>Play time represented by a number of ticks.</summary>
        public static double SecondsFor(int ticks) => ticks <= 0 ? 0 : ticks * Simulation.TickSeconds;

        /// <summary>Add unpaused seconds; a non-finite or negative amount is ignored.</summary>
        public static void Add(SimState st, double seconds)
        {
            if (st == null) return;
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds <= 0) return;
            st.PlaySeconds += seconds;
        }

        /// <summary>Add the play time of <paramref name="ticks"/> ticks.</summary>
        public static void AddTicks(SimState st, int ticks) => Add(st, SecondsFor(ticks));

        /// <summary>H:MM:SS for a slot row (reference queries.ts <c>clockOf</c>, lines 196-199).</summary>
        public static string Clock(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0) seconds = 0;
            var whole = (long)Math.Floor(seconds);
            var h = whole / 3600;
            var m = whole / 60 % 60;
            var s = whole % 60;
            return h.ToString(System.Globalization.CultureInfo.InvariantCulture)
                   + ":" + m.ToString("00", System.Globalization.CultureInfo.InvariantCulture)
                   + ":" + s.ToString("00", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
