using System;
using System.Collections.Generic;
using System.Globalization;
using Relight.Presentation;
using Relight.Sim;
using Relight.Sim.UI;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Relight.UI.Settings
{
    /// <summary>
    /// C-10. The one Settings surface (UI_AND_ONBOARDING.md §3.8), driven by both the title screen and the pause
    /// menu. <see cref="Bind"/> is called with the instanced <c>settings-panel</c> root; everything else is a
    /// reaction to a control the player moved.
    ///
    /// Three rules run through all of it:
    /// <list type="bullet">
    /// <item>Every setting takes effect the moment it is changed, and persistence is a separate promise
    ///       (<see cref="Preferences.Status"/>): "Settings work for this session" is a true statement, not a
    ///       failure to apply.</item>
    /// <item>No control is offered for a feature this build does not have (§3.8.3). The bindings dropdown is
    ///       <see cref="BindingMap"/>, not the whole reference table, and Restore default quickbar is hidden
    ///       unless the host gives it something to do.</item>
    /// <item>Every refusal shown is a ported one — <see cref="BindingProblem"/>'s sentences, verbatim.</item>
    /// </list>
    /// </summary>
    [AddComponentMenu("Relight/Front End/Settings")]
    public sealed class SettingsController : MonoBehaviour
    {
        [Tooltip("The mixer carrying the exposed parameters MasterVol, UiVol, WorldVol and AlertsVol.")]
        [SerializeField] private AudioMixer mixer;

        [Tooltip("RelightControls. Rebinding writes overrides onto this asset and into PlayerPrefs.")]
        [SerializeField] private InputActionAsset actions;

        [Tooltip("The running session's autosave controller. Empty on the title screen, where there is none.")]
        [SerializeField] private AutosaveController autosave;

        [Tooltip("The PanelSettings whose scale the Interface scale dropdown drives.")]
        [SerializeField] private PanelSettings panel;

        /// <summary>The exposed AudioMixer parameter names, fixed by the brief.</summary>
        public const string MasterParam = "MasterVol";
        public const string UiParam = "UiVol";
        public const string WorldParam = "WorldVol";
        public const string AlertsParam = "AlertsVol";

        /// <summary>Silence, in dB. Below the mixer's -80 floor is the same as off.</summary>
        private const float MuteDb = -80f;

        /// <summary>
        /// How the host asks a question. The pause menu and the title screen each own a <see cref="ConfirmDialog"/>;
        /// this class owns none, so that one modal serves the whole screen. Null means "apply without asking",
        /// which only happens when a host forgot to wire it — never silently for a destructive change, because
        /// <see cref="Apply"/> refuses to lower the kept count in that case.
        /// </summary>
        public Action<Confirmation, Action<int>> Ask;

        /// <summary>
        /// What "Restore default quickbar" does. Only a running session can do it (it is ten
        /// <c>AssignBarCommand(i, "")</c> commands, and the UI never writes the bar itself), so the pause menu
        /// supplies it and the title screen does not. When it is null the button is hidden rather than shown
        /// dead: §3.8.3 forbids a control that pretends a feature exists.
        /// </summary>
        public Action RestoreQuickbar;

        /// <summary>Raised after the interface scale changes, so a host can re-lay anything that caches sizes.</summary>
        public Action<int> ScaleChanged;

        private VisualElement _root;
        private DropdownField _scale, _motion, _interval, _kept, _action;
        private Slider _master, _ui, _world, _alerts;
        private Label _masterValue, _uiValue, _worldValue, _alertsValue;
        private Slider _brightness;
        private Label _brightnessValue;
        private Toggle _mute;
        private Label _interfaceNote, _autosaveNote, _autosaveConsequence, _bindingsNote,
                      _bindingCurrent, _bindingProblem, _bindingGood, _status;
        private TextField _key;
        private Button _apply, _restoreBindings, _restoreHint, _restoreQuickbar, _retry;
        private VisualElement _controls;

        /// <summary>Reference-action to reference-key. The port's copy of settingsPanel.ts's override map.</summary>
        private readonly Dictionary<string, string> _overrides = new Dictionary<string, string>(StringComparer.Ordinal);

        private string _captured = "";
        private bool _quiet;        // true while code is setting a control, so the change handler does nothing

        private void Awake()
        {
            ReadOverrides();
            ApplyStoredToAsset();
            ApplyAudio();
            ApplyScale(Preferences.Scale);
        }

        // ---- binding the document -------------------------------------------------------------------------

        /// <summary>
        /// Attach to an instanced <c>settings-panel</c>. Safe to call again with a different instance: the old
        /// one is simply forgotten, because nothing here holds state that the controls do not.
        /// </summary>
        public void Bind(VisualElement root)
        {
            _root = root;
            if (_root == null) return;

            _scale = _root.Q<DropdownField>("scale-field");
            _motion = _root.Q<DropdownField>("motion-field");
            _interfaceNote = _root.Q<Label>("interface-note");

            _master = _root.Q<Slider>("master-slider");
            _masterValue = _root.Q<Label>("master-value");
            _mute = _root.Q<Toggle>("mute-toggle");
            _ui = _root.Q<Slider>("ui-slider");
            _uiValue = _root.Q<Label>("ui-value");
            _world = _root.Q<Slider>("world-slider");
            _worldValue = _root.Q<Label>("world-value");
            _alerts = _root.Q<Slider>("alerts-slider");
            _alertsValue = _root.Q<Label>("alerts-value");

            _brightness = _root.Q<Slider>("brightness-slider");
            _brightnessValue = _root.Q<Label>("brightness-value");

            _interval = _root.Q<DropdownField>("autosave-interval");
            _kept = _root.Q<DropdownField>("autosave-kept");
            _autosaveNote = _root.Q<Label>("autosave-note");
            _autosaveConsequence = _root.Q<Label>("autosave-consequence");

            _bindingsNote = _root.Q<Label>("bindings-note");
            _action = _root.Q<DropdownField>("binding-action");
            _bindingCurrent = _root.Q<Label>("binding-current");
            _key = _root.Q<TextField>("binding-key");
            _apply = _root.Q<Button>("binding-apply");
            _restoreBindings = _root.Q<Button>("binding-restore");
            _bindingProblem = _root.Q<Label>("binding-problem");
            _bindingGood = _root.Q<Label>("binding-good");
            _controls = _root.Q<VisualElement>("controls-list");

            _restoreHint = _root.Q<Button>("restore-hint");
            _restoreQuickbar = _root.Q<Button>("restore-quickbar");
            _retry = _root.Q<Button>("retry-settings");
            _status = _root.Q<Label>("settings-status");

            Label(_root, "scale-label", FrontEndText.InterfaceScale);
            Label(_root, "motion-label", FrontEndText.InterfaceMotion);
            Label(_root, "heading-display", FrontEndText.DisplayHeading);
            Label(_root, "brightness-label", FrontEndText.Brightness);
            Label(_root, "master-label", FrontEndText.MasterVolume);
            Label(_root, "mute-label", FrontEndText.MuteAll);
            Label(_root, "ui-label", FrontEndText.UiVolume);
            Label(_root, "world-label", FrontEndText.WorldVolume);
            Label(_root, "alerts-label", FrontEndText.AlertsVolume);
            Label(_root, "autosave-label", FrontEndText.AutosaveLabel);
            Label(_root, "kept-label", FrontEndText.AutosavesKeptLabel);
            Label(_root, "heading-interface", FrontEndText.SectionInterface);
            Label(_root, "heading-audio", FrontEndText.SectionAudio);
            if(mixer==null)
            {
                Label(_root,"heading-audio","Audio · unavailable in this build");
                foreach(var control in new VisualElement[]{_master,_ui,_world,_alerts,_mute}) control?.SetEnabled(false);
            }
            Label(_root, "heading-saving", FrontEndText.SectionSaving);
            Label(_root, "heading-bindings", FrontEndText.SectionBindings);
            Label(_root, "heading-restore", FrontEndText.SectionRestore);

            if (_interfaceNote != null) _interfaceNote.text = FrontEndText.InterfaceNote;
            if (_autosaveNote != null) _autosaveNote.text = AutosaveChoices.TimingNote;
            if (_bindingsNote != null) _bindingsNote.text = FrontEndText.BindingsNote;
            if (_apply != null) _apply.text = FrontEndText.ApplyBinding;
            if (_restoreBindings != null) _restoreBindings.text = FrontEndText.RestoreDefaults;
            if (_restoreHint != null) _restoreHint.text = FrontEndText.RestoreOpeningHint;
            if (_restoreQuickbar != null) _restoreQuickbar.text = FrontEndText.RestoreQuickbar;
            if (_retry != null) _retry.text = FrontEndText.RetrySaveSettings;
            if (_key != null)
            {
                _key.isReadOnly = true;                       // settingsPanel.ts: the field reports, it does not type
                _key.value = "";
                _key.textEdition.placeholder = FrontEndText.PressAKey;
            }

            FillChoices();
            ReadIntoControls();
            Wire();
            ShowControls();
            ShowStatus();
        }

        /// <summary>
        /// Print what every control is bound to (C-02 requirement 5). It is a READ-ONLY list beside the rebinding
        /// row, because three of the ten — Cancel, Move and the action bar — cannot be rebound at all and §3.8.3
        /// forbids offering a control that pretends otherwise; showing them is honest, offering them is not.
        ///
        /// The element is optional. <c>controls-list</c> may be a <see cref="Label"/> (one block of text) or any
        /// container (one Label per row), and if the UXML has neither this does nothing at all — a layout without
        /// it keeps working, which is the same contract every other element in this class has.
        /// </summary>
        private void ShowControls()
        {
            if (_controls == null) return;
            var lines = BindingMap.ControlLines(_overrides);
            if (_controls is Label block) { block.text = string.Join("\n", lines); return; }
            _controls.Clear();
            for (var i = 0; i < lines.Count; i++)
            {
                var row = new Label(lines[i]);
                row.AddToClassList("controls-row");
                _controls.Add(row);
            }
        }

        private static void Label(VisualElement root, string name, string text)
        {
            var label = root.Q<Label>(name);
            if (label != null) label.text = text;
        }

        private void FillChoices()
        {
            if (_scale != null)
            {
                var list = new List<string>(FrontEndText.Scales.Length);
                foreach (var s in FrontEndText.Scales) list.Add(FrontEndText.ScaleLabel(s));
                _scale.choices = list;
            }

            if (_motion != null)
                _motion.choices = new List<string>
                    { FrontEndText.MotionSystem, FrontEndText.MotionReduce, FrontEndText.MotionFull };

            if (_interval != null)
            {
                var list = new List<string>(AutosaveChoices.Intervals.Length);
                foreach (var m in AutosaveChoices.Intervals) list.Add(AutosaveChoices.IntervalLabel(m));
                _interval.choices = list;
            }

            if (_kept != null)
            {
                var list = new List<string>(AutosaveChoices.Kept.Length);
                foreach (var k in AutosaveChoices.Kept) list.Add(AutosaveChoices.KeptLabel(k));
                _kept.choices = list;
            }

            if (_action != null) _action.choices = BindingMap.Labels();
        }

        private void ReadIntoControls()
        {
            _quiet = true;
            try
            {
                if (_scale != null) _scale.index = IndexOf(FrontEndText.Scales, Preferences.Scale);
                if (_motion != null) _motion.index = MotionIndex(Preferences.Motion);

                SetSlider(_master, _masterValue, Preferences.Master);
                SetSlider(_ui, _uiValue, Preferences.UiVolume);
                SetSlider(_world, _worldValue, Preferences.WorldVolume);
                SetSlider(_alerts, _alertsValue, Preferences.AlertsVolume);
                SetSlider(_brightness, _brightnessValue, Preferences.Brightness);
                if (_mute != null) _mute.value = Preferences.Mute;

                if (_interval != null) _interval.index = IndexOf(AutosaveChoices.Intervals, Preferences.AutosaveMinutes);
                if (_kept != null) _kept.index = IndexOf(AutosaveChoices.Kept, Preferences.AutosavesKept);
                ShowConsequence(Preferences.AutosaveMinutes, false);

                if (_action != null && _action.index < 0 && BindingMap.Entries.Length > 0) _action.index = 0;
                ShowCurrentBinding();
            }
            finally { _quiet = false; }
        }

        private void Wire()
        {
            if (_scale != null) _scale.RegisterValueChangedCallback(_ => OnScale());
            if (_motion != null) _motion.RegisterValueChangedCallback(_ => OnMotion());

            if (_master != null) _master.RegisterValueChangedCallback(e => OnVolume(MasterParam, _masterValue, e.newValue));
            if (_ui != null) _ui.RegisterValueChangedCallback(e => OnVolume(UiParam, _uiValue, e.newValue));
            if (_world != null) _world.RegisterValueChangedCallback(e => OnVolume(WorldParam, _worldValue, e.newValue));
            if (_alerts != null) _alerts.RegisterValueChangedCallback(e => OnVolume(AlertsParam, _alertsValue, e.newValue));
            if (_mute != null) _mute.RegisterValueChangedCallback(e => OnMute(e.newValue));
            if (_brightness != null) _brightness.RegisterValueChangedCallback(e => OnBrightness(e.newValue));

            if (_interval != null) _interval.RegisterValueChangedCallback(_ => OnInterval());
            if (_kept != null) _kept.RegisterValueChangedCallback(_ => OnKept());

            if (_action != null) _action.RegisterValueChangedCallback(_ => { ClearMessages(); ShowCurrentBinding(); });
            if (_key != null) _key.RegisterCallback<KeyDownEvent>(OnCaptureKey, TrickleDown.TrickleDown);
            if (_apply != null) _apply.clicked += OnApplyBinding;
            if (_restoreBindings != null) _restoreBindings.clicked += OnRestoreBindings;

            if (_restoreHint != null) _restoreHint.clicked += OnRestoreHint;
            if (_retry != null) _retry.clicked += () => { Preferences.Retry(); ShowStatus(); };

            if (_restoreQuickbar != null)
            {
                // Hidden, not disabled: there is nothing here to restore outside a session (§3.8.3).
                _restoreQuickbar.EnableInClassList("hidden", RestoreQuickbar == null);
                _restoreQuickbar.clicked += () => { RestoreQuickbar?.Invoke(); Say(_bindingGood, ""); };
            }
        }

        // ---- interface ------------------------------------------------------------------------------------

        private void OnScale()
        {
            if (_quiet || _scale == null) return;
            var i = Mathf.Clamp(_scale.index, 0, FrontEndText.Scales.Length - 1);
            var percent = FrontEndText.Scales[i];
            Preferences.Scale = percent;
            ApplyScale(percent);
            Persist();
        }

        private void ApplyScale(int percent)
        {
            // PanelSettings.scale multiplies the whole UI. It is deliberately NOT the camera's zoom: §3.8 says
            // interface scale and camera zoom are independent, and InterfaceNote repeats that to the player.
            if (panel != null)
            {
                panel.scaleMode = PanelScaleMode.ConstantPixelSize;
                panel.scale = percent / 100f;
            }
            ScaleChanged?.Invoke(percent);
        }

        private void OnMotion()
        {
            if (_quiet || _motion == null) return;
            var stored = _motion.index == 1 ? Preferences.MotionReduce
                       : _motion.index == 2 ? Preferences.MotionFull
                       : Preferences.MotionSystem;
            Preferences.Motion = stored;
            ApplyMotion(stored);
            Persist();
        }

        /// <summary>
        /// Reduced motion is a class on the panel root, so USS decides what it means and no C# animates around
        /// it. "Follow system preference" is the third state and not a synonym for Full: Unity exposes no runtime
        /// reduced-motion query on the platforms this ships to, so the system state is read as "no opinion" and
        /// the interface keeps its ordinary, already-restrained transitions.
        /// </summary>
        private void ApplyMotion(string stored)
        {
            var top = _root?.panel?.visualTree;
            if (top == null) return;
            top.EnableInClassList("reduce-motion", stored == Preferences.MotionReduce);
            top.EnableInClassList("full-motion", stored == Preferences.MotionFull);
        }

        private static int MotionIndex(string stored)
            => stored == Preferences.MotionReduce ? 1 : stored == Preferences.MotionFull ? 2 : 0;

        // ---- audio ----------------------------------------------------------------------------------------

        private void SetSlider(Slider slider, Label value, float v)
        {
            if (slider != null) slider.value = v;
            if (value != null) value.text = FrontEndText.Percent(v);
        }

        private void OnVolume(string param, Label value, float v)
        {
            if (value != null) value.text = FrontEndText.Percent(v);
            if (_quiet) return;

            if (param == MasterParam) Preferences.Master = v;
            else if (param == UiParam) Preferences.UiVolume = v;
            else if (param == WorldParam) Preferences.WorldVolume = v;
            else if (param == AlertsParam) Preferences.AlertsVolume = v;

            ApplyAudio();
            Persist();
        }

        private void OnBrightness(float v)
        {
            if (_brightnessValue != null) _brightnessValue.text = FrontEndText.Percent(v);
            if (_quiet) return;
            Preferences.Brightness = v;
            // Live in a session; on the title screen there is no presenter and the saved value is read at Awake.
            var lighting = FindAnyObjectByType<Relight.Presentation.LightingPresenter>();
            if (lighting != null) lighting.SetBrightness(v);
            Preferences.Save();
            ShowStatus();
        }

        private void OnMute(bool muted)
        {
            if (_quiet) return;
            Preferences.Mute = muted;
            ApplyAudio();
            Persist();
        }

        /// <summary>
        /// Push every level to the mixer. Mute silences the master bus only, so unmuting restores the four
        /// levels the player set rather than a remembered snapshot that could drift out of step with them.
        /// </summary>
        private void ApplyAudio()
        {
            if (mixer == null) return;
            var master = Preferences.Mute ? 0f : Preferences.Master;
            mixer.SetFloat(MasterParam, Decibels(master));
            mixer.SetFloat(UiParam, Decibels(Preferences.UiVolume));
            mixer.SetFloat(WorldParam, Decibels(Preferences.WorldVolume));
            mixer.SetFloat(AlertsParam, Decibels(Preferences.AlertsVolume));
        }

        /// <summary>A 0-1 level as mixer decibels. Zero is the mixer's floor, not negative infinity.</summary>
        public static float Decibels(float level01)
            => level01 <= 0.0001f ? MuteDb : Mathf.Max(MuteDb, Mathf.Log10(level01) * 20f);

        // ---- saving ---------------------------------------------------------------------------------------

        private void OnInterval()
        {
            if (_quiet || _interval == null) return;
            var i = Mathf.Clamp(_interval.index, 0, AutosaveChoices.Intervals.Length - 1);
            var minutes = AutosaveChoices.Intervals[i];
            Preferences.AutosaveMinutes = minutes;
            PushAutosave();
            // §2.7.2: the timer restarts from now and nothing is saved this instant. Said, not left to be guessed.
            ShowConsequence(minutes, true);
            Persist();
        }

        private void ShowConsequence(double minutes, bool changed)
        {
            if (_autosaveConsequence == null) return;
            _autosaveConsequence.text = minutes <= 0 ? AutosaveChoices.OffConsequence
                                      : changed ? AutosaveChoices.IntervalRestarts
                                      : "";
        }

        private void OnKept()
        {
            if (_quiet || _kept == null) return;
            var i = Mathf.Clamp(_kept.index, 0, AutosaveChoices.Kept.Length - 1);
            var to = AutosaveChoices.Kept[i];
            var from = Preferences.AutosavesKept;
            if (to == from) return;

            var question = AutosaveChoices.LowerKept(from, to, ExistingAutosaves());
            if (question == null) { CommitKept(to); return; }

            if (Ask == null)
            {
                // Nothing can ask, so nothing may delete. The dropdown goes back to what it was and says why.
                RevertKept(from);
                Say(_bindingProblem, "");
                return;
            }

            Ask(question, choice =>
            {
                if (choice == 0) CommitKept(to);
                else RevertKept(from);
            });
        }

        private void CommitKept(int to)
        {
            Preferences.AutosavesKept = to;
            PushAutosave();
            Persist();
        }

        private void RevertKept(int from)
        {
            _quiet = true;
            try { if (_kept != null) _kept.index = IndexOf(AutosaveChoices.Kept, from); }
            finally { _quiet = false; }
        }

        /// <summary>How many autosaves the ring actually holds, so the question can name what would be lost.</summary>
        private int ExistingAutosaves()
        {
            var store = autosave?.Store;
            if (store == null) return 0;
            try { return store.Autosaves.ListRecoverable().Count; }
            catch { return 0; }
        }

        /// <summary>
        /// Hand both numbers to the running scheduler. <c>WithInterval</c>/<c>WithSlots</c> keep the two settings
        /// the screen does not offer (autosave-on-event and save-on-quit) exactly as the session had them, and
        /// <c>AutosaveSettings.Clamped()</c> inside the scheduler is the final authority on range.
        /// </summary>
        private void PushAutosave()
        {
            var scheduler = autosave?.Scheduler;
            if (scheduler == null) return;
            var next = scheduler.Settings
                .WithInterval(Preferences.AutosaveMinutes)
                .WithSlots(Preferences.AutosavesKept);
            autosave.ApplySettings(next);
        }

        // ---- keyboard bindings ----------------------------------------------------------------------------

        private BindingMap.Entry Current()
        {
            if (_action == null) return null;
            var i = _action.index;
            return i >= 0 && i < BindingMap.Entries.Length ? BindingMap.Entries[i] : null;
        }

        private void ShowCurrentBinding()
        {
            if (_bindingCurrent == null) return;
            var entry = Current();
            _bindingCurrent.text = entry == null ? "" : FrontEndText.CurrentBinding(entry.Action, Bindings.Keys(entry.Action, _overrides));
        }

        /// <summary>
        /// settingsPanel.ts reads the key from the field itself rather than letting the platform rebind. This does
        /// the same, on purpose and not for want of the Input System's <c>PerformInteractiveRebinding</c>: that
        /// helper writes the override the instant a key is pressed, which would apply a binding the ported rules
        /// are about to refuse. Capturing first and applying on <see cref="FrontEndText.ApplyBinding"/> keeps
        /// <see cref="BindingProblem"/> the only thing that decides.
        /// </summary>
        private void OnCaptureKey(KeyDownEvent e)
        {
            e.StopPropagation();
            var name = KeyNames.FromKey(e.keyCode, e.character);
            if (string.IsNullOrEmpty(name))
            {
                _captured = "";
                if (_key != null) _key.SetValueWithoutNotify("");
                Say(_bindingProblem, BindingProblem.KeyText);
                return;
            }
            _captured = name;
            if (_key != null) _key.SetValueWithoutNotify(name == " " ? "Space" : name);
            ClearMessages();
        }

        private void OnApplyBinding()
        {
            ClearMessages();
            var entry = Current();
            if (entry == null) return;

            if (string.IsNullOrEmpty(_captured)) { Say(_bindingProblem, BindingProblem.KeyText); return; }

            // The ported rule decides. Fixed keys, reserved keys, unusable keys and keys already used by another
            // action in the same context are all refused here, in the reference's own words.
            var problem = BindingProblem.Problem(entry.Action, _captured, _overrides);
            if (problem != null) { Say(_bindingProblem, problem); return; }

            var failure = BindingMap.Apply(actions, entry, _captured);
            if (!string.IsNullOrEmpty(failure)) { Say(_bindingProblem, failure); return; }

            _overrides[entry.Action] = _captured;
            StoreOverrides();
            ShowCurrentBinding();
            ShowControls();
            Say(_bindingGood, FrontEndText.BindingApplied);
            _captured = "";
            if (_key != null) _key.SetValueWithoutNotify("");
        }

        private void OnRestoreBindings()
        {
            ClearMessages();
            _overrides.Clear();
            actions?.RemoveAllBindingOverrides();
            StoreOverrides();
            ShowCurrentBinding();
            ShowControls();
            Say(_bindingGood, FrontEndText.DefaultsRestored);
        }

        // ---- restore and recover --------------------------------------------------------------------------

        private void OnRestoreHint()
        {
            Preferences.OpeningHint = true;
            // C-09 (wave-3 W-A patch d): the hint card keeps its own dismissed flag; clear it too so the next
            // new city shows the hint again, which is what the button promises.
            OpeningHintController.Restore();
            Persist();
        }

        // ---- persistence ----------------------------------------------------------------------------------

        /// <summary>
        /// Read both halves of the stored bindings. They are written together, so a mismatch means the store was
        /// edited or half-written; the reference map wins, because it is the one the rules are defined against,
        /// and the asset is rebuilt from it by <see cref="ApplyStoredToAsset"/>.
        /// </summary>
        private void ReadOverrides()
        {
            _overrides.Clear();
            foreach (var pair in BindingMap.StoredOverrides()) _overrides[pair.Key] = pair.Value;
        }

        private void ApplyStoredToAsset()
        {
            if (actions == null) return;
            actions.RemoveAllBindingOverrides();
            foreach (var pair in _overrides)
            {
                var entry = BindingMap.Find(pair.Key);
                if (entry != null) BindingMap.Apply(actions, entry, pair.Value);
            }
        }

        private void StoreOverrides()
        {
            var parts = new List<string>(_overrides.Count);
            foreach (var pair in _overrides)
                parts.Add(pair.Key + "=" + (pair.Value == " " ? "Space" : pair.Value));
            Preferences.BindingKeys = string.Join(";", parts);
            Preferences.BindingOverrides = actions == null ? "" : actions.SaveBindingOverridesAsJson();
            Persist();
        }

        private void Persist()
        {
            Preferences.Save();
            ShowStatus();
        }

        private void ShowStatus()
        {
            if (_status != null) _status.text = Preferences.Status;
            if (_retry != null) _retry.EnableInClassList("hidden", Preferences.Writable);
        }

        // ---- small helpers --------------------------------------------------------------------------------

        private void ClearMessages()
        {
            Say(_bindingProblem, "");
            Say(_bindingGood, "");
        }

        private static void Say(Label label, string text)
        {
            if (label != null) label.text = text ?? "";
        }

        private static int IndexOf(int[] values, int want)
        {
            for (var i = 0; i < values.Length; i++) if (values[i] == want) return i;
            return 0;
        }

        private static int IndexOf(double[] values, double want)
        {
            for (var i = 0; i < values.Length; i++) if (Math.Abs(values[i] - want) < 0.0001) return i;
            return 0;
        }
    }
}
