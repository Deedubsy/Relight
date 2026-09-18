using System;
using System.Globalization;

namespace Relight.Sim.UI
{
    /// <summary>
    /// C-10. The Settings → Saving section, UI_AND_ONBOARDING.md §2.7.2 (U-D-35). Two dropdowns and the two
    /// sentences they can produce.
    ///
    /// <b>The offered defaults differ from <see cref="AutosaveSettings"/>'s.</b> The recorded engine defaults are
    /// 5 minutes and 3 ring slots; §2.7.2 says the player-facing defaults are <b>Every 10 minutes</b> and
    /// <b>5 autosaves kept</b>, and this brief repeats them. Both offered sets sit inside the engine's clamps
    /// (1-30 minutes or off, 1-10 slots), so nothing here can produce a settings object the scheduler rejects.
    /// The difference is deliberate and is recorded for the coordinator.
    /// </summary>
    public static class AutosaveChoices
    {
        /// <summary>Minutes, in the order the dropdown lists them. 0 is "Off" (§2.7.2).</summary>
        public static readonly double[] Intervals = { 0, 5, 10, 15, 30 };

        /// <summary>§2.7.2 — the player-facing default interval, not <see cref="AutosaveSettings.DefaultIntervalMinutes"/>.</summary>
        public const double DefaultIntervalMinutes = 10;

        /// <summary>§2.7.2 — how many autosaves are kept, in dropdown order.</summary>
        public static readonly int[] Kept = { 3, 5, 10 };

        /// <summary>§2.7.2 — the player-facing default count, not <see cref="AutosaveSettings.DefaultSlots"/>.</summary>
        public const int DefaultKept = 5;

        /// <summary>"Autosave off" / "Every 10 minutes" — what the dropdown shows.</summary>
        public static string IntervalLabel(double minutes)
            => minutes <= 0
                ? "Autosave off"
                : "Every " + PackLayout.Num(minutes) + (minutes == 1 ? " minute" : " minutes");

        /// <summary>"3 autosaves kept".</summary>
        public static string KeptLabel(int count)
            => count.ToString(CultureInfo.InvariantCulture) + (count == 1 ? " autosave kept" : " autosaves kept");

        /// <summary>
        /// §2.7.2 — turning autosaving off is allowed, and the screen states the consequence in plain words rather
        /// than a warning icon.
        /// </summary>
        public const string OffConsequence =
            "New games and long sessions will only be saved when you save them.";

        /// <summary>
        /// §2.7.2 — changing the interval restarts the timer from now and never triggers an immediate save.
        /// Said on the screen so the player is not left wondering whether one just happened.
        /// </summary>
        public const string IntervalRestarts =
            "Changing this restarts the autosave timer; it does not save now.";

        /// <summary>§2.7.2 — autosave timing counts play, not time spent paused or in a menu.</summary>
        public const string TimingNote = "Autosave timing counts active play, not time paused or in a menu.";

        /// <summary>
        /// §2.7.2 — lowering "Autosaves kept" deletes the oldest autosaves, so it asks first and <b>names how many
        /// will be removed</b>. Returns null when nothing would be deleted, so the caller applies the change
        /// straight away instead of raising an empty question.
        /// </summary>
        public static Confirmation LowerKept(int from, int to, int existing)
        {
            if (to >= from) return null;
            var removed = existing - to;
            if (removed <= 0) return null;
            var what = removed == 1 ? "the oldest autosave" : "the " + removed.ToString(CultureInfo.InvariantCulture) + " oldest autosaves";
            return new Confirmation(
                "Keep fewer autosaves?",
                "Keeping " + to.ToString(CultureInfo.InvariantCulture) + " instead of "
                    + from.ToString(CultureInfo.InvariantCulture) + " deletes " + what
                    + ". This cannot be undone.",
                "Delete and keep " + to.ToString(CultureInfo.InvariantCulture), "Cancel");
        }
    }
}
