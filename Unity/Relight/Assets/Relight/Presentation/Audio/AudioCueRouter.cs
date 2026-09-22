using System;
using System.Collections.Generic;
using Relight.Sim;
using Relight.World;
using UnityEngine;
using UnityEngine.Audio;

namespace Relight.Presentation
{
    /// <summary>
    /// C-11. Turns this frame's sim events into cue keys and voices them, and is the <see cref="IAudioCueSink"/>
    /// the UI's own keys reach too. The authored clip table ships empty (§12.9: sourcing, formats and licensing are
    /// WORLD_AND_ASSETS §9.2); since REL-67 (U-D-69 (d)) a key with no authored clip plays the rough sound
    /// <see cref="PlaceholderSounds"/> makes in code, so alerts can be heard and the sliders checked before D-11.
    /// An authored clip always wins, and <c>placeholders</c> off restores the old silence.
    ///
    /// Structure, per §12:
    /// <list type="bullet">
    /// <item>The key table below is the port of §12.2-12.8. Each row names the sim record it comes from, so a new
    /// event is a new row here and needs no other wiring.</item>
    /// <item>Mixer groups are exactly the four <c>SettingsController</c> drives — <c>MasterVol</c>, <c>UiVol</c>,
    /// <c>WorldVol</c>, <c>AlertsVol</c> (wave-2 W-C). This class chooses a <b>group</b> per cue from the key's
    /// prefix and never writes an exposed parameter: volume is the player's, through Settings. The project has no
    /// mixer yet, so a cue whose group is unassigned takes <see cref="AudioLevels.Gain"/> instead (REL-67).</item>
    /// <item>Voice limiting (§12.1 rule 4, "budgeted like alerts"): a key may sound at most once per
    /// <see cref="CoalesceSeconds"/>, which collapses a turret's stream of shots the way the alert strip collapses
    /// repeats. <see cref="AudioSource"/>s are pooled and capped.</item>
    /// <item>§12.1 rule 1: no cue is raised here that the sim has not already resolved, and nothing is written back.
    /// The class reads <see cref="SimHost.LastFrameEvents"/> and, for the approach cues of U-D-62
    /// (<see cref="ApproachCues"/>), the enemy list; it writes neither.</item>
    /// </list>
    ///
    /// <b>Deliberate omissions.</b> §12.4's per-machine running loops and §12.8's ambience beds are continuous, not
    /// event-driven, so they are not routed here; they need clips and a loop manager and belong with D-11. §12.5's
    /// UI and placement cues are raised by the UI with <see cref="AudioCue.Ui"/> keys, because they are input
    /// facts and never appear in <c>SimState.Events</c>. Both are recorded in the wave-3 W-B report.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Relight/Audio Cue Router")]
    public sealed class AudioCueRouter : MonoBehaviour, IAudioCueSink
    {
        /// <summary>The same key may sound at most this often (§12.1 rule 4).</summary>
        public const float CoalesceSeconds = 0.06f;

        /// <summary>One clip against one key. The table ships empty; adding a row is all an asset needs, and the row
        /// replaces that key's placeholder.</summary>
        [Serializable]
        public sealed class Cue
        {
            [Tooltip("The cue key, e.g. turret.shot. See AudioCueRouter's table and AudioCue.Ui.")]
            public string key = "";

            [Tooltip("Clips for this key. One is picked at random so a repeated cue does not machine-gun.")]
            public AudioClip[] clips = Array.Empty<AudioClip>();

            [Tooltip("Linear gain for this cue, before the mixer.")]
            [Range(0f, 1f)] public float volume = 1f;

            [Tooltip("Random pitch spread, +/- this fraction.")]
            [Range(0f, 0.5f)] public float pitchJitter = 0.05f;
        }

        [Tooltip("The host whose events are routed. Found in the scene if left empty.")]
        [SerializeField] private SimHost host;

        [Tooltip("Group for cues the player causes in the interface (ui.*). Exposed parameter UiVol.")]
        [SerializeField] private AudioMixerGroup uiGroup;

        [Tooltip("Group for cues that happen in the world (weapon, machine, enemy). Exposed parameter WorldVol.")]
        [SerializeField] private AudioMixerGroup worldGroup;

        [Tooltip("Group for warnings and alerts (raid, core, engineer). Exposed parameter AlertsVol.")]
        [SerializeField] private AudioMixerGroup alertsGroup;

        [Tooltip("Fallback group. Exposed parameter MasterVol.")]
        [SerializeField] private AudioMixerGroup masterGroup;

        [Tooltip("Authored cue table. Empty today: a key with no row plays its placeholder, or nothing (REL-67).")]
        [SerializeField] private List<Cue> cues = new List<Cue>();

        [Tooltip("Most sources that may sound at once. Beyond this the quietest-ranked cue is simply dropped.")]
        [SerializeField, Min(1)] private int voices = 16;

        [Tooltip("Tiles beyond which a positional cue is inaudible.")]
        [SerializeField, Min(1f)] private float hearingTiles = 40f;

        [Tooltip("Play the rough code-made sound for a key with no authored clip (REL-67, U-D-69 d). Off = silence.")]
        [SerializeField] private bool placeholders = true;

        [Tooltip("Sound each alien kind's approach cue (U-D-62, REL-67).")]
        [SerializeField] private bool approachCues = true;

        private readonly Dictionary<string, Cue> _byKey = new Dictionary<string, Cue>(StringComparer.Ordinal);
        private readonly Dictionary<string, float> _lastAt = new Dictionary<string, float>(StringComparer.Ordinal);
        private readonly List<AudioSource> _pool = new List<AudioSource>();
        private readonly ApproachCues _approach = new ApproachCues();
        private readonly List<ApproachCues.Cue> _heard = new List<ApproachCues.Cue>();
        private AudioListener _listener;
        private SimHost _subscribed;

        /// <summary>Cues asked for since enable. A wiring check the coordinator can read with no clips present.</summary>
        public int Routed { get; private set; }

        /// <summary>Cues that actually reached an <see cref="AudioSource"/>, placeholders included.</summary>
        public int Played { get; private set; }

        private void Awake()
        {
            if (host == null) host = FindAnyObjectByType<SimHost>();
            for (var i = 0; i < cues.Count; i++)
            {
                var c = cues[i];
                if (c != null && !string.IsNullOrEmpty(c.key)) _byKey[c.key] = c;
            }
        }

        private void OnEnable()
        {
            AudioCue.Sink = this;
            if (host != null)
            {
                host.SessionChanged += OnSession;
                _subscribed = host;
            }
        }

        private void OnDisable()
        {
            if (ReferenceEquals(AudioCue.Sink, this)) AudioCue.Sink = null;
            if (_subscribed != null) _subscribed.SessionChanged -= OnSession;
            _subscribed = null;
        }

        private void OnSession(Simulation _) => _approach.Reset();

        private void LateUpdate()
        {
            // A paused host produced no ticks and therefore no events, so §12.1 rule 3 ("at speed 0 ... one-shots
            // do not queue up to replay on resume") holds without a special case.
            var events = host == null ? null : host.LastFrameEvents;
            if (events == null) return;
            for (var i = 0; i < events.Count; i++) Route(events[i]);
            Approach();
        }

        /// <summary>
        /// U-D-62: one cue per coming alien kind, at the nearest body, so the stereo pan says where it is. Skipped
        /// while paused, which also keeps the sim clock (and so every cadence) still.
        /// </summary>
        private void Approach()
        {
            if (!approachCues || host.Paused) return;
            var st = host.Simulation?.State;
            if (st == null) return;
            if (_listener == null) _listener = FindAnyObjectByType<AudioListener>();
            var ear = _listener != null ? _listener.transform : (Camera.main != null ? Camera.main.transform : null);
            if (ear == null) return;

            _heard.Clear();
            _approach.Collect(EnemyQueries.All(st), WorldSpace.Position(ear.position), st.T, _heard);
            for (var i = 0; i < _heard.Count; i++)
                Cast(_heard[i].Key, new Vector2((float)_heard[i].At.X, (float)_heard[i].At.Y));
        }

        /// <summary>
        /// The key table: §12.2-12.6, one arm per sim record. Anything not named here is silent on purpose —
        /// §12.1 rule 5 makes the names provisional, but the coverage is the owner's decision.
        /// </summary>
        private void Route(SimEvent e)
        {
            switch (e)
            {
                // 12.2 Player and turret weapons.
                case WeaponFiredEvent w:
                    Cast("weapon.shot." + w.Kind, null);
                    break;
                case ReloadedEvent _:
                    Cast("weapon.reload", null);
                    break;
                case TurretShotEvent t:
                    Cast("turret.shot", new Vector2((float)t.TargetX, (float)t.TargetY));
                    // 12.3: the hit/miss distinction is the shot's own `Hit`, exactly as the tracer colour is.
                    Cast(t.Hit ? "impact.hit" : "impact.miss", new Vector2((float)t.TargetX, (float)t.TargetY));
                    break;

                // 12.3 Impacts and enemy cues.
                case ProjectileExpiredEvent p:
                    Cast(p.Stopped ? "impact.hit" : "impact.miss", new Vector2((float)p.X, (float)p.Y));
                    break;
                case EnemySpitEvent s:
                    Cast("enemy.spit", new Vector2((float)s.X, (float)s.Y));
                    break;
                case EnemyKilledEvent k:
                    Cast("enemy.death." + k.Kind, new Vector2((float)k.X, (float)k.Y));
                    break;
                case StructureDamagedEvent _:
                    Cast("structure.hit", null);
                    break;
                case StructureDestroyedEvent sd:
                    Cast("structure.destroyed." + sd.Kind, null);
                    break;
                case EngineerDownEvent ed:
                    Cast("engineer.down", new Vector2((float)ed.X, (float)ed.Y));
                    break;
                case EngineerUpEvent _:
                    Cast("engineer.up", null);
                    break;

                // 12.4 Machine operation. The running loops are continuous and are not routed here (see the class
                // remarks); what an event can say is the moment a machine stopped or started.
                case PowerOutageEvent _:
                    Cast("machine.stop", null);
                    break;
                case PowerRestoredEvent _:
                    Cast("machine.start", null);
                    break;
                case GeneratorDryEvent _:
                    Cast("power.generator-dry", null);
                    break;
                case MachineProducedEvent _:
                    Cast("machine.produced", null);
                    break;

                // 12.5 Crafting and construction feedback.
                case MinedEvent m:
                    Cast(m.Cleared ? "mine.cleared" : "mine.tick", new Vector2(m.X, m.Y));
                    break;
                case MiningStoppedEvent _:
                    Cast("mine.stopped", null);
                    break;
                case WeaponCraftedEvent _:
                    Cast("craft.done", null);
                    break;
                case EquipmentChangedEvent _:
                    Cast(AudioCue.Ui.SlotSelect, null);
                    break;
                case CoreRepairedEvent _:
                    Cast("core.repaired", null);
                    break;
                case CoreRepairAbortedEvent _:
                    Cast(AudioCue.Ui.Refused, null);
                    break;

                // 12.6 Raid warnings. One warning per announced attack, played once, never one per sector: only
                // the two announcing kinds map, exactly as the coordinator's wave-2 W-B note requires.
                case RaidNoticeEvent r:
                    if (r.Kind == RaidNoticeKind.Announced || r.Kind == RaidNoticeKind.MinorRaid) Cast("raid.warning", null);
                    break;
                case OpeningScheduledEvent _:
                    Cast("raid.warning", null);
                    break;
                case OpeningStartedEvent _:
                    Cast("raid.begins", null);
                    break;
                case CoreDamagedEvent _:
                    Cast("structure.hit", null);
                    break;
                case CoreDisabledEvent _:
                    Cast("core.disabled", null);
                    break;

                // L-02, ALWAYS_DARK_SPEC §5.4: the two relief moments that replace dawn. The sim already limits
                // them (once per connection; at most once in ten seconds), so neither needs a budget here.
                // REL-11: the cue belongs to a district CONNECTING. A district coming back after its supply failed
                // lights its lamps again — the sweep still draws — but the announcement is not made twice, so a
                // generator running dry and being refuelled never replays the fanfare.
                case DistrictLitEvent lit:
                    if (!lit.Returning) Cast("light.district-on", new Vector2((float)lit.X, (float)lit.Y));
                    break;
                case EnteredLightEvent _:
                    Cast("light.entered", null);
                    break;
            }
        }

        private void Cast(string key, Vector2? at)
        {
            Routed++;
            AudioCue.Play(key, at);
        }

        /// <inheritdoc />
        public void Play(string key, Vector2? at)
        {
            if (string.IsNullOrEmpty(key)) return;

            // An authored clip wins; otherwise the code-made placeholder (REL-67); a key with neither is silence,
            // with no log (brief C-11).
            AudioClip clip = null;
            var volume = 1f;
            var jitter = PlaceholderJitter;
            if (_byKey.TryGetValue(key, out var cue) && cue.clips != null && cue.clips.Length > 0)
            {
                clip = cue.clips[cue.clips.Length == 1 ? 0 : UnityEngine.Random.Range(0, cue.clips.Length)];
                volume = cue.volume;
                jitter = cue.pitchJitter;
            }
            else if (placeholders)
            {
                clip = PlaceholderSounds.Clip(key);
            }
            if (clip == null) return;

            var now = Time.unscaledTime;
            if (_lastAt.TryGetValue(key, out var last) && now - last < CoalesceSeconds) return;
            _lastAt[key] = now;

            var src = Free();
            if (src == null) return;

            var group = Group(key);
            src.clip = clip;
            src.outputAudioMixerGroup = group;
            // No mixer in the project yet: the player's levels are applied here instead (REL-67). With a mixer the
            // group carries them, and multiplying again would apply them twice.
            src.volume = group == null ? volume * AudioLevels.Gain(GroupOf(key)) : volume;
            src.pitch = jitter <= 0f ? 1f : 1f + UnityEngine.Random.Range(-jitter, jitter);
            if (at.HasValue)
            {
                src.spatialBlend = 1f;
                // An approach is meant to be heard from beyond sight, so it carries past its own selection reach
                // (the camera sits 10 units off the plane, which the 3D distance includes).
                var reach = key.StartsWith(PlaceholderSounds.ApproachPrefix, StringComparison.Ordinal)
                    ? (float)ApproachCues.HearingTiles * 1.25f
                    : hearingTiles;
                src.maxDistance = reach * WorldSpace.UnitsPerTile;
                src.transform.position = WorldSpace.World(new Vec2(at.Value.x, at.Value.y));
            }
            else
            {
                src.spatialBlend = 0f;
                src.transform.localPosition = Vector3.zero;
            }
            src.Play();
            Played++;
        }

        /// <summary>The pitch spread of a placeholder, so a repeated beep does not sound identical.</summary>
        private const float PlaceholderJitter = 0.04f;

        /// <summary>
        /// The volume group a key belongs to (<see cref="AudioLevels"/>' names), chosen from its prefix so a new key
        /// needs no routing edit: <c>ui.*</c> is the interface; raid, core, engineer and power are alerts; the rest
        /// is the world.
        /// </summary>
        public static string GroupOf(string key)
        {
            if (string.IsNullOrEmpty(key)) return AudioLevels.WorldGroup;
            if (key.StartsWith("ui.", StringComparison.Ordinal)) return AudioLevels.UiGroup;
            if (key.StartsWith("raid.", StringComparison.Ordinal)
                || key.StartsWith("core.", StringComparison.Ordinal)
                || key.StartsWith("engineer.", StringComparison.Ordinal)
                || key.StartsWith("power.", StringComparison.Ordinal))
                return AudioLevels.AlertsGroup;
            return AudioLevels.WorldGroup;
        }

        /// <summary>The mixer group a key belongs to; null when neither its group nor the master is assigned.</summary>
        private AudioMixerGroup Group(string key)
        {
            switch (GroupOf(key))
            {
                case AudioLevels.UiGroup: return uiGroup != null ? uiGroup : masterGroup;
                case AudioLevels.AlertsGroup: return alertsGroup != null ? alertsGroup : masterGroup;
                default: return worldGroup != null ? worldGroup : masterGroup;
            }
        }

        private AudioSource Free()
        {
            for (var i = 0; i < _pool.Count; i++) if (!_pool[i].isPlaying) return _pool[i];
            if (_pool.Count >= voices) return null;
            var go = new GameObject("Cue " + _pool.Count);
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.dopplerLevel = 0f;   // the camera glides; a cue must not bend in pitch as it does
            src.minDistance = 4f * WorldSpace.UnitsPerTile;
            _pool.Add(src);
            return src;
        }
    }
}
