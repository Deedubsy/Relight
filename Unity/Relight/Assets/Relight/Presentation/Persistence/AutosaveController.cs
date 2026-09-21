using System;
using System.Collections.Generic;
using Relight.Sim;
using UnityEngine;

namespace Relight.Presentation
{
    /// <summary>
    /// B-11's Unity side, and deliberately the thinnest part of it: it supplies the three things the engine-free
    /// save layer cannot get for itself — the writable folder (<see cref="SaveRoot.Path"/>), the
    /// real disk (<see cref="SystemFileSystem"/>), and the tick boundary to save on
    /// (<see cref="SimHost.TickBoundary"/>, TECHNICAL_ARCHITECTURE.md §9.4.3) — and then gets out of the way. Every
    /// decision, every path, every refusal and every recovery lives in <c>Relight.Sim</c>, where it is testable
    /// without an editor; this file has no logic worth a test of its own, which is the point.
    ///
    /// Three details matter here and nowhere else:
    /// <list type="bullet">
    /// <item>the autosave clock is fed <b>ticks × <see cref="Simulation.TickSeconds"/></b>, not
    ///       <see cref="Time.deltaTime"/> and not <see cref="SimHost.AcceptedRealSeconds"/> (which keeps accruing
    ///       while paused). Ticks run <i>is</i> unpaused sim time, which is what §9.4.2 specifies;</item>
    /// <item>the save is taken from <see cref="SimHost.TickBoundary"/>, which the host raises after the frame's
    ///       ticks and before the next frame's commands — the only moment the state is not mid-anything;</item>
    /// <item>the scheduler belongs to <b>one session</b>. Every <see cref="SimHost.SessionChanged"/> — a new game
    ///       from <c>WorldBootstrap</c>, a load adopted here, a detach — resets it (elapsed interval, failure
    ///       count, stopped ring, notice), and a swap that happened while this component was disabled is caught
    ///       by comparing <see cref="SimHost.Session"/> on enable and at every tick boundary. Before 2026-09-13
    ///       only this component's own <see cref="Load"/> reset it, so a new game started elsewhere inherited the
    ///       previous session's clock (an autosave seconds after starting) or its stopped ring (no autosave at
    ///       all until a manual save) — TASKS.md B-13 residue, closed by U-M-38.</item>
    /// </list>
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Relight/Autosave")]
    public sealed class AutosaveController : MonoBehaviour
    {
        [Tooltip("The host whose tick boundaries drive autosaving. Found on this GameObject if left empty.")]
        [SerializeField] private SimHost host;

        [Tooltip("Minutes of unpaused sim time between autosaves (1-30). 0 turns timed autosaving off.")]
        [SerializeField, Range(0f, 30f)] private float intervalMinutes = (float)AutosaveSettings.DefaultIntervalMinutes;

        [Tooltip("How many rotating autosave slots are kept (1-10). The oldest is the one overwritten.")]
        [SerializeField, Range(1, 10)] private int slots = AutosaveSettings.DefaultSlots;

        [Tooltip("Allow gameplay to ask for an autosave at a notable moment. Nothing calls it yet.")]
        [SerializeField] private bool autosaveOnEvents = true;

        [Tooltip("Write auto-quit.json when the game closes. It is outside the ring and costs no slot.")]
        [SerializeField] private bool saveOnQuit = true;

        private SaveStore _store;
        private AutosaveScheduler _scheduler;
        private bool _subscribed;
        private int _session = -1;      // the SimHost.Session the scheduler was last reset for

        /// <summary>Where saves live: <c>{SaveRoot.Path}/saves/{profile}/…</c> (§9.4.1).</summary>
        public SaveStore Store => _store;

        /// <summary>The ring's scheduler, for a settings screen to read and re-<see cref="AutosaveScheduler.Apply"/>.</summary>
        public AutosaveScheduler Scheduler => _scheduler;

        /// <summary>The one line the HUD shows while something is wrong with saving; null when all is well.</summary>
        public string Notice => _scheduler?.Notice;

        /// <summary>The most recent save attempt of any kind, for a "saved" indicator.</summary>
        public SaveResult LastSave { get; private set; }

        /// <summary>The most recent load attempt, for the Load screen's refusal message.</summary>
        public LoadResult LastLoad { get; private set; }

        private void Awake()
        {
            if (host == null) host = GetComponent<SimHost>();
            _store = new SaveStore(new SystemFileSystem(), SaveRoot.Path);
            _scheduler = new AutosaveScheduler(_store, Settings());
        }

        private AutosaveSettings Settings() =>
            new AutosaveSettings(intervalMinutes, slots, autosaveOnEvents, saveOnQuit);

        /// <summary>Re-read the inspector fields (or a settings screen's values) into the running scheduler.</summary>
        public void ApplySettings(AutosaveSettings settings = null) => _scheduler?.Apply(settings ?? Settings());

        private void OnEnable()
        {
            if (host == null || _subscribed) return;
            host.TickBoundary += OnTickBoundary;
            host.SessionChanged += OnSessionChanged;
            _subscribed = true;
            // A session that changed while this component was disabled is still a change.
            if (host.Session != _session) OnSessionChanged(host.Simulation);
        }

        private void OnDisable()
        {
            if (host == null || !_subscribed) return;
            host.TickBoundary -= OnTickBoundary;
            host.SessionChanged -= OnSessionChanged;
            _subscribed = false;
        }

        /// <summary>
        /// The scheduler starts again for the session that is live now: the elapsed interval, the failure count,
        /// a stopped ring and its notice all belonged to the previous one. Timed and event autosaves only ever
        /// serialise <see cref="SimHost.Simulation"/> at the moment they fire, so no pending work can save an old
        /// session; what this prevents is the old session's <i>clock</i> and <i>failure state</i> deciding when —
        /// or whether — the new one is saved.
        /// </summary>
        private void OnSessionChanged(Simulation sim)
        {
            _session = host.Session;
            _scheduler?.Reset();
            LastSave = null;
            LastLoad = null;
        }

        private void OnTickBoundary(int ticks)
        {
            if (host.Session != _session) OnSessionChanged(host.Simulation);
            if (ticks <= 0 || host.Simulation == null) return;
            // Ticks run is unpaused sim time by construction: a paused host runs none (§9.4.2).
            var result = _scheduler.Advance(PlayClock.SecondsFor(ticks), host.Simulation);
            if (result == null) return;
            LastSave = result;
            if (!result.Ok) Debug.LogWarning("Relight: autosave failed — " + result.Reason);
            else if (!string.IsNullOrEmpty(result.Notice)) Debug.LogWarning("Relight: " + result.Notice);
        }

        /// <summary>An event autosave (§9.4.2). Exposed for gameplay to call; nothing calls it yet.</summary>
        public SaveResult AutosaveNow(string reason)
        {
            if (host == null || host.Simulation == null) return null;
            var r = _scheduler.Trigger(reason, host.Simulation);
            if (r != null) LastSave = r;
            return r;
        }

        /// <summary>
        /// Save to a manual slot. A success also tells the scheduler the disk works, which is what resumes a ring
        /// that had stopped after three failures (§9.4.5).
        /// </summary>
        public SaveResult Save(string name)
        {
            if (host == null || host.Simulation == null) return SaveResult.Failed(null, "there is no game to save");
            var sim = host.Simulation;
            var r = _store.Save(name, sim.State, sim.Context);
            LastSave = r;
            if (r.Ok) _scheduler.OnManualSaveSucceeded();
            else Debug.LogWarning("Relight: save failed — " + r.Reason);
            return r;
        }

        /// <summary>Every manual slot, newest first, for the Load screen (C-10).</summary>
        public IReadOnlyList<SaveSlotInfo> List() => _store == null ? Array.Empty<SaveSlotInfo>() : _store.List();

        /// <summary>Load a manual slot into the running host. A refusal changes nothing (§9.4.5).</summary>
        public LoadResult Load(string name)
        {
            if (host == null || host.Simulation == null) return LoadResult.Refuse("there is no session to load into");
            return Adopt(_store.Load(name, LoadContext()));
        }

        /// <summary>
        /// Load one named autosave file ("auto-3.json", "auto-quit.json") into the running host — the Load
        /// screen's row action (C-10, §2.7.1). Added beside <see cref="LoadNewestAutosave"/> rather than changing
        /// it: the ring's own recovery still uses the newest, and this is only the player naming a different one.
        /// </summary>
        public LoadResult LoadAutosave(string fileName)
        {
            if (host == null || host.Simulation == null) return LoadResult.Refuse("there is no session to load into");
            return Adopt(_store.Autosaves.Load(fileName, LoadContext()));
        }

        /// <summary>Load the newest autosave — "continue" (§9.4.5's recovery order applies).</summary>
        public LoadResult LoadNewestAutosave()
        {
            if (host == null || host.Simulation == null) return LoadResult.Refuse("there is no session to load into");
            return Adopt(_store.Autosaves.LoadNewest(LoadContext()));
        }

        /// <summary>
        /// Swap a loaded state into the host. The scene is never reloaded — a load replaces the state behind the
        /// same <see cref="SimHost"/> — and the attach raises <see cref="SimHost.SessionChanged"/>, which starts
        /// the autosave clock again so the first autosave after a load is a full interval away rather than
        /// immediate. <see cref="LastLoad"/> is recorded after the attach, so the reset does not clear it.
        /// </summary>
        private SimContext LoadContext()
        {
            var ctx=host.Simulation.Context;
            // The header is checked against both supported layouts in Adopt, before changing the running world.
            return new SimContext(ctx.Data,ctx.Geometry,threat:new EnemyThreatLayer(),sites:ctx.Sites,mapId:null);
        }

        private LoadResult Adopt(LoadResult r)
        {
            if (!r.Ok)
            {
                LastLoad = r;
                Debug.LogWarning("Relight: load refused — " + r.Reason);
                return r;
            }
            var bootstrap=FindAnyObjectByType<WorldBootstrap>();
            var useScene=false;
            if(bootstrap!=null && !string.IsNullOrEmpty(r.Header?.MapId) && r.Header.MapId!=bootstrap.SourceGeometry?.MapId)
            {
                var authored=FindFirstObjectByType<Relight.World.SceneWorld>();
                string sceneId=null;
                if(authored!=null && authored.useForNewGames)
                {
                    var candidate=authored.Compile(out _);sceneId=candidate.MapId;Destroy(candidate);
                }
                if(sceneId!=r.Header.MapId)
                {
                    LastLoad=LoadResult.Refuse("this save belongs to a different scene layout; restore that layout or start a New Game");
                    return LastLoad;
                }
                useScene=true;
            }
            var ctx=bootstrap!=null ? bootstrap.ContextForLayout(r.State.OpeningResourceVersion,useScene) : host.Simulation.Context;
            // The lit mask is never saved, and a loaded state runs no initialisers. Turrets and aliens read the mask
            // before the light phase rebuilds it, so without this the first tick after a load would be unlit (L-02).
            LightPhase.Ensure(ctx, r.State);
            host.Attach(Simulation.Wrap(ctx, r.State));
            if (host.Session != _session) OnSessionChanged(host.Simulation);   // in case this component is disabled
            LastLoad = r;
            if (!string.IsNullOrEmpty(r.Upgraded)) Debug.Log("Relight: " + r.Upgraded);
            if (!string.IsNullOrEmpty(r.Warning)) Debug.LogWarning("Relight: " + r.Warning);
            if (!string.IsNullOrEmpty(r.Recovered)) Debug.LogWarning("Relight: " + r.Recovered);
            return r;
        }

        private void OnApplicationQuit()
        {
            if (host == null || host.Simulation == null || _scheduler == null) return;
            var r = _scheduler.SaveOnQuit(host.Simulation);
            if (r != null && !r.Ok) Debug.LogWarning("Relight: the save on quit failed — " + r.Reason);
        }
    }
}
