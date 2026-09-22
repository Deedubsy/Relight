using System;
using Relight.Presentation;
using Relight.Sim;
using Relight.Sim.UI;
using Relight.UI.Settings;
using UnityEngine;

namespace Relight.UI.FrontEnd
{
    /// <summary>
    /// C-10. The world scene's half of the front end: it takes delivery of what the title screen asked for,
    /// puts the player's saved preferences into effect, and keeps the one fact every §2.6.4 confirmation needs —
    /// whether this session holds progress that is not in a save.
    ///
    /// It exists because <c>Relight.Presentation</c> cannot reference <c>Relight.UI</c> (that would be circular),
    /// so the request the title screen made travels as data through <see cref="NewGameRequest"/> in
    /// <c>Relight.Sim</c> and is collected here, on the UI side, once the session exists.
    ///
    /// Put it on the same object as <see cref="SimHost"/>, after <see cref="WorldBootstrap"/> in the script
    /// execution order (or simply leave both on the same object — this reads the request in <c>Start</c>, which
    /// runs after every <c>Awake</c>).
    /// </summary>
    [AddComponentMenu("Relight/Front End/Front End Bootstrap")]
    public sealed class FrontEndBootstrap : MonoBehaviour
    {
        [Tooltip("The session this bootstrap adopts a save into. Found in the scene if left empty.")]
        [SerializeField] private SimHost host;

        [Tooltip("Saving. Its store is the one the title screen listed, and its scheduler takes the settings.")]
        [SerializeField] private AutosaveController autosave;

        /// <summary>
        /// Whether this session has advanced past its last save. Every §2.6.4 confirmation asks this, and the
        /// pause menu reads it rather than keeping a second copy.
        /// </summary>
        public SessionDirty Dirty { get; } = new SessionDirty();

        /// <summary>
        /// The manual slot name or autosave file this session was loaded from, or "" for a new city. §2.6.4:
        /// "Deleting is never offered for the save currently loaded in a running session."
        /// </summary>
        public string LoadedName { get; private set; } = "";

        /// <summary>Whether <see cref="LoadedName"/> names an autosave file rather than a manual slot.</summary>
        public bool LoadedIsAutosave { get; private set; }

        /// <summary>
        /// The one sentence the title screen could not show because the scene changed in the same frame — §2.6.3's
        /// "Continue fell back to {label}". Read once; it is not a persistent status.
        /// </summary>
        public string StartupNotice { get; private set; } = "";

        /// <summary>Take <see cref="StartupNotice"/> and clear it, so it is shown exactly once.</summary>
        public string TakeStartupNotice()
        {
            var notice = StartupNotice;
            StartupNotice = "";
            return notice;
        }

        private bool _subscribed;

        private void Awake()
        {
            if (host == null) host = FindAnyObjectByType<SimHost>();
            if (autosave == null) autosave = FindAnyObjectByType<AutosaveController>();
        }

        private void OnEnable()
        {
            if (host == null || _subscribed) return;
            host.SessionChanged += OnSessionChanged;
            _subscribed = true;
        }

        private void OnDisable()
        {
            if (host == null || !_subscribed) return;
            host.SessionChanged -= OnSessionChanged;
            _subscribed = false;
        }

        private void Start()
        {
            ApplyPreferences();
            Collect();
        }

        /// <summary>
        /// A new session is by definition unsaved, whatever the previous one had done. This runs for a New Game
        /// and for a load, and the load path immediately records the save it came from, below.
        /// </summary>
        private void OnSessionChanged(Simulation sim)
        {
            Dirty.Reset();
            LoadedName = "";
            LoadedIsAutosave = false;
        }

        /// <summary>
        /// The saved Autosave interval and count, put into effect for this session. Everything else the settings
        /// screen owns (scale, motion, audio, bindings) is applied by <see cref="SettingsController"/> itself when
        /// it wakes, because those are live preferences rather than session settings.
        /// </summary>
        private void ApplyPreferences()
        {
            var scheduler = autosave?.Scheduler;
            if (scheduler == null) return;
            autosave.ApplySettings(scheduler.Settings
                .WithInterval(Preferences.AutosaveMinutes)
                .WithSlots(Preferences.AutosavesKept));
        }

        /// <summary>
        /// Take the title screen's request. A New Game needs nothing done here — <see cref="WorldBootstrap"/> has
        /// already started the session with the requested seed — but a Load has to be adopted into the session
        /// that is now running, which is exactly what <see cref="AutosaveController"/> does.
        /// </summary>
        private void Collect()
        {
            StartupNotice = NewGameRequest.TakeNotice();
            if (!NewGameRequest.Take(out _, out var loadName, out var loadIsAutosave)) return;
            if (string.IsNullOrEmpty(loadName)) return;

            var result = loadIsAutosave ? autosave?.LoadAutosave(loadName) : autosave?.Load(loadName);
            if (result == null) { Say("saving is not available in this scene"); return; }
            if (!result.Ok) { Say(result.Reason); return; }

            // The store's own words about what it had to do to read the file are still the player's business
            // (§2.7.1), but they are no longer said here. REL-66: this chain showed at most ONE of the three
            // sentences, and only if and when the player next opened the pause menu — a startup notice is not a
            // notice about a load. The HUD says all of them, once, right after the load (HudViewModel.SayLoad).

            MarkLoaded(loadName, loadIsAutosave);
        }

        /// <summary>Record that this session is the one in <paramref name="name"/>, and is therefore saved.</summary>
        public void MarkLoaded(string name, bool isAutosave)
        {
            LoadedName = name ?? "";
            LoadedIsAutosave = isAutosave;
            Dirty.Saved(host == null ? 0 : host.TotalTicks, Iso());
        }

        /// <summary>Record a successful save of this session, manual or automatic.</summary>
        public void MarkSaved(string name, bool isAutosave)
        {
            if (!isAutosave && !string.IsNullOrEmpty(name)) { LoadedName = name; LoadedIsAutosave = false; }
            Dirty.Saved(host == null ? 0 : host.TotalTicks, Iso());
        }

        private static string Iso() => DateTime.UtcNow.ToString("o", System.Globalization.CultureInfo.InvariantCulture);

        private void Say(string line)
        {
            if (string.IsNullOrEmpty(line)) return;
            StartupNotice = string.IsNullOrEmpty(StartupNotice) ? line : StartupNotice + " " + line;
        }
    }
}
