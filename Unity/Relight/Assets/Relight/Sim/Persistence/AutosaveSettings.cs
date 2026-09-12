using System.Globalization;

namespace Relight.Sim
{
    /// <summary>
    /// The autosave policy and its defaults (U-D-35, TECHNICAL_ARCHITECTURE.md §9.4.2). The defaults are the
    /// recorded ones and are what a fresh install uses:
    /// <list type="bullet">
    /// <item><b>every 5 minutes of unpaused sim time</b> — not wall clock: time spent in a menu, paused, or with the
    ///       window in the background is not play, and autosaving after it would overwrite a good ring slot with a
    ///       state the player never reached;</item>
    /// <item><b>3 ring slots</b>, rotating oldest-first;</item>
    /// <item><b>autosave on events</b> on — the hook exists and is wired to nothing yet (§9.4.2);</item>
    /// <item><b>save on quit</b> on, to <c>auto-quit.json</c>, which is outside the ring and never rotated.</item>
    /// </list>
    /// Ranges: interval 1–30 minutes or off (<see cref="IntervalMinutes"/> &lt;= 0), slots 1–10.
    /// <see cref="Clamped"/> is applied by the scheduler, so a settings file edited by hand cannot produce a
    /// ring of 0 slots or an autosave every frame.
    /// </summary>
    public sealed class AutosaveSettings
    {
        public const double DefaultIntervalMinutes = 5;
        public const double MinIntervalMinutes = 1;
        public const double MaxIntervalMinutes = 30;
        public const int DefaultSlots = 3;
        public const int MinSlots = 1;
        public const int MaxSlots = 10;

        public AutosaveSettings(
            double intervalMinutes = DefaultIntervalMinutes,
            int slots = DefaultSlots,
            bool onEvents = true,
            bool saveOnQuit = true)
        {
            IntervalMinutes = intervalMinutes;
            Slots = slots;
            OnEvents = onEvents;
            SaveOnQuit = saveOnQuit;
        }

        /// <summary>Minutes of <b>unpaused sim time</b> between ring autosaves; zero or less means off.</summary>
        public double IntervalMinutes { get; }

        /// <summary>How many ring slots are kept. The oldest is the one overwritten.</summary>
        public int Slots { get; }

        /// <summary>Whether a subsystem may ask for an autosave at a notable moment (§9.4.2; nothing calls it yet).</summary>
        public bool OnEvents { get; }

        /// <summary>Whether quitting writes <c>auto-quit.json</c>.</summary>
        public bool SaveOnQuit { get; }

        /// <summary>The recorded defaults.</summary>
        public static AutosaveSettings Default { get; } = new AutosaveSettings();

        /// <summary>Autosaving is off when the interval is zero or less (a legitimate player choice, §9.4.2).</summary>
        public bool TimedEnabled => IntervalMinutes > 0;

        /// <summary>The interval in seconds of unpaused sim time, after clamping.</summary>
        public double IntervalSeconds => IntervalMinutes * 60.0;

        /// <summary>
        /// This settings object with every value inside its documented range. "Off" survives clamping — only a
        /// positive interval is pulled up to the 1-minute floor, so turning autosave off is never overridden.
        /// </summary>
        public AutosaveSettings Clamped()
        {
            var interval = IntervalMinutes;
            if (interval > 0)
            {
                if (interval < MinIntervalMinutes) interval = MinIntervalMinutes;
                else if (interval > MaxIntervalMinutes) interval = MaxIntervalMinutes;
            }
            else
            {
                interval = 0;
            }
            var slots = Slots < MinSlots ? MinSlots : (Slots > MaxSlots ? MaxSlots : Slots);
            if (interval == IntervalMinutes && slots == Slots) return this;
            return new AutosaveSettings(interval, slots, OnEvents, SaveOnQuit);
        }

        public AutosaveSettings WithInterval(double minutes) => new AutosaveSettings(minutes, Slots, OnEvents, SaveOnQuit);
        public AutosaveSettings WithSlots(int slots) => new AutosaveSettings(IntervalMinutes, slots, OnEvents, SaveOnQuit);
        public AutosaveSettings WithOnEvents(bool on) => new AutosaveSettings(IntervalMinutes, Slots, on, SaveOnQuit);
        public AutosaveSettings WithSaveOnQuit(bool on) => new AutosaveSettings(IntervalMinutes, Slots, OnEvents, on);

        public override string ToString()
            => (TimedEnabled ? IntervalMinutes.ToString("0.##", CultureInfo.InvariantCulture) + " min" : "off")
               + ", " + Slots.ToString(CultureInfo.InvariantCulture) + " slots"
               + (OnEvents ? ", on events" : "") + (SaveOnQuit ? ", on quit" : "");
    }
}
