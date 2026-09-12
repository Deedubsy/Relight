using System;

namespace Relight.Sim
{
    /// <summary>
    /// Decides <i>when</i> to autosave and carries out TECHNICAL_ARCHITECTURE.md §9.4.5's failure policy. It owns no
    /// file handling — <see cref="AutosaveStore"/> does that — and it never throws: a save that cannot be written is
    /// a <see cref="SaveResult"/> and a line of HUD text, and the game keeps running (§9.4.5: "failures are data,
    /// never exceptions; a failed save never pauses, blocks or stops the game").
    ///
    /// The clock it runs on is <b>unpaused sim time</b> (§9.4.2), which the host supplies as ticks run
    /// × <see cref="Simulation.TickSeconds"/>. Wall-clock time would count menus, pauses and a backgrounded window
    /// as play and autosave over a good slot with a state the player never reached. The same quantity advances
    /// <see cref="SimState.PlaySeconds"/>, which is done here so there is exactly one place that says what "played"
    /// means.
    ///
    /// The failure policy, verbatim from §9.4.5 and each covered by a test:
    /// <list type="bullet">
    /// <item>a failed write leaves the previous autosave and its <c>.bak</c> untouched and loadable;</item>
    /// <item>a lock is transient: the next attempt uses a fresh temp name, and nothing ever spins or sleeps;</item>
    /// <item><b>three consecutive failures stop the ring</b> with one persistent notice, instead of retrying every
    ///       interval forever against a full or read-only disk;</item>
    /// <item>a stopped ring resumes when a manual save succeeds — the player's own evidence that the disk works.</item>
    /// </list>
    /// </summary>
    public sealed class AutosaveScheduler
    {
        /// <summary>§9.4.5: how many consecutive failures stop the ring.</summary>
        public const int FailuresBeforeStop = 3;

        private readonly SaveStore _store;
        private double _since;          // unpaused sim seconds since the last ring autosave
        private int _attempt;           // fresh-temp-name counter for a retry after a lock (§9.4.5)

        public AutosaveScheduler(SaveStore store, AutosaveSettings settings = null)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            Settings = (settings ?? AutosaveSettings.Default).Clamped();
        }

        /// <summary>The policy in force, always clamped to its documented ranges.</summary>
        public AutosaveSettings Settings { get; private set; }

        /// <summary>Consecutive failed autosaves; reset by any success and by a successful manual save.</summary>
        public int ConsecutiveFailures { get; private set; }

        /// <summary>True once the ring has given up (§9.4.5). Timed and event autosaves do nothing while it is set.</summary>
        public bool Stopped { get; private set; }

        /// <summary>The one persistent line the HUD shows while something is wrong; null when all is well.</summary>
        public string Notice { get; private set; }

        /// <summary>The most recent autosave attempt, or null if none has been made yet.</summary>
        public SaveResult Last { get; private set; }

        /// <summary>Unpaused sim seconds accumulated toward the next autosave.</summary>
        public double SecondsSinceSave => _since;

        /// <summary>Unpaused sim seconds until the next autosave, or -1 when autosaving is off or stopped.</summary>
        public double SecondsUntilSave
        {
            get
            {
                if (Stopped || !Settings.TimedEnabled) return -1;
                var left = Settings.IntervalSeconds - _since;
                return left < 0 ? 0 : left;
            }
        }

        /// <summary>Change the policy. The elapsed time is kept, so shortening the interval can save immediately.</summary>
        public void Apply(AutosaveSettings settings)
        {
            Settings = (settings ?? AutosaveSettings.Default).Clamped();
        }

        /// <summary>
        /// Advance the autosave clock by <paramref name="unpausedSimSeconds"/> and write a ring autosave if one is
        /// due. Also the single place <see cref="SimState.PlaySeconds"/> grows. Returns the save's result, or null
        /// when none was due — which is the usual answer, every frame.
        /// </summary>
        public SaveResult Advance(double unpausedSimSeconds, Simulation sim)
        {
            if (sim == null) return null;
            if (double.IsNaN(unpausedSimSeconds) || double.IsInfinity(unpausedSimSeconds) || unpausedSimSeconds <= 0)
                return null;

            PlayClock.Add(sim.State, unpausedSimSeconds);

            if (Stopped || !Settings.TimedEnabled) return null;
            _since += unpausedSimSeconds;
            if (_since < Settings.IntervalSeconds) return null;

            // One save per due point, never a burst: a long stall (a load, a breakpoint) must not fire three saves.
            _since = 0;
            return Write("the timed autosave", sim);
        }

        /// <summary>
        /// The event-autosave hook of §9.4.2 — "before a wave, after a milestone". It is exposed and honours
        /// <see cref="AutosaveSettings.OnEvents"/>, and <b>nothing calls it yet</b>: the moments that deserve one are
        /// a gameplay decision that belongs to the rows that own those events, not to the persistence foundation.
        /// Calling it resets the timed interval, so an event save and a timed save cannot land back to back.
        /// </summary>
        public SaveResult Trigger(string reason, Simulation sim)
        {
            if (sim == null) return null;
            if (Stopped || !Settings.OnEvents) return null;
            _since = 0;
            return Write(reason ?? "an event autosave", sim);
        }

        /// <summary>
        /// Write <c>auto-quit.json</c> on the way out (§9.4.2). It is outside the ring, so quitting never costs the
        /// player a ring slot, and it is written even when the ring has stopped — the last chance to keep the
        /// session is worth one more try.
        /// </summary>
        public SaveResult SaveOnQuit(Simulation sim)
        {
            if (sim == null || !Settings.SaveOnQuit) return null;
            var bytes = Serialize(sim, out var header, out var failure);
            if (bytes == null) { Last = failure; Notice = "the save on quit failed: " + failure.Reason; return failure; }
            var r = _store.Autosaves.WriteQuit(bytes, header, _attempt);
            Last = r;
            if (!r.Ok) Notice = "the save on quit failed: " + r.Reason;
            return r;
        }

        /// <summary>
        /// Tell the scheduler a manual save has just succeeded. That is proof the disk works, so a stopped ring
        /// resumes and the persistent notice clears (§9.4.5). The host calls this from its Save command.
        /// </summary>
        public void OnManualSaveSucceeded()
        {
            ConsecutiveFailures = 0;
            _attempt = 0;
            if (Stopped)
            {
                Stopped = false;
                Notice = null;
            }
            else if (Notice != null)
            {
                Notice = null;
            }
        }

        /// <summary>Forget the accumulated interval — used when a new game starts or a save is loaded.</summary>
        public void Reset()
        {
            _since = 0;
            _attempt = 0;
            ConsecutiveFailures = 0;
            Stopped = false;
            Notice = null;
            Last = null;
        }

        private SaveResult Write(string reason, Simulation sim)
        {
            var bytes = Serialize(sim, out var header, out var failure);
            if (bytes == null) { Fail(reason, failure); return failure; }

            var r = _store.Autosaves.WriteRing(bytes, Settings.Slots, header, _attempt);
            Last = r;
            if (r.Ok)
            {
                ConsecutiveFailures = 0;
                _attempt = 0;
                Notice = r.Notice;          // usually null; set when only the index update failed
                return r;
            }
            Fail(reason, r);
            return r;
        }

        private void Fail(string reason, SaveResult r)
        {
            Last = r;
            ConsecutiveFailures++;
            // A fresh temp name next time: §9.4.5 treats a locked temp as transient, and retrying the same name
            // against a file an antivirus or sync agent still holds would fail identically every interval.
            _attempt++;
            if (ConsecutiveFailures >= FailuresBeforeStop)
            {
                Stopped = true;
                Notice = "autosaving has stopped after " + FailuresBeforeStop + " failed attempts ("
                         + (r?.Reason ?? "unknown") + "). It will resume once a save you make by hand succeeds.";
            }
            else
            {
                Notice = "an autosave failed (" + (r?.Reason ?? "unknown") + "); the previous autosave is still there.";
            }
        }

        /// <summary>
        /// Serialise at the caller's moment — which the host guarantees is a tick boundary (§9.4.3), never mid-tick.
        /// A serialisation failure is turned into a <see cref="SaveResult"/> like any other, so a bug in a Visit
        /// implementation shows up as a refused autosave rather than an exception through the frame loop.
        /// </summary>
        private byte[] Serialize(Simulation sim, out SaveHeader header, out SaveResult failure)
        {
            header = null;
            failure = null;
            try
            {
                return SaveSerializer.Write(sim.State, sim.Context?.Data, SaveSerializer.NowIso(), out header);
            }
            catch (Exception e)
            {
                failure = SaveResult.Failed(null, AtomicWrite.Describe(e, "the autosave could not be prepared"));
                return null;
            }
        }
    }
}
