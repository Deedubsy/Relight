using System;
using System.Collections.Generic;
using Relight.Presentation;
using Relight.Sim;
using Relight.Sim.UI;
using Relight.UI.FrontEnd;
using Relight.UI.Settings;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Relight.UI.PauseMenu
{
    /// <summary>
    /// C-10. The in-game pause menu (UI_AND_ONBOARDING.md §2.6.4, §2.6.5, §2.7.1, §2.7.3): Resume · Save · Load ·
    /// Settings · Save and exit to title, plus the Save and Load screens that open from it, the save-failure
    /// modal, and the transient autosave notice that belongs to play rather than to pausing.
    ///
    /// What it is NOT allowed to do, and does not:
    /// <list type="bullet">
    /// <item>It never writes a count, a slot or a file itself. Saving is <see cref="AutosaveController"/>;
    ///       deleting is <see cref="SaveGateway"/>; the words on every failure are the store's own.</item>
    /// <item>It never decides whether a save is loadable. <c>SaveRowFormatter</c> and
    ///       <c>SaveSerializer.MapProblem</c> decide, and a refused row is shown disabled with that reason
    ///       rather than hidden (§2.6.1's rule, applied to the in-game list as well).</item>
    /// <item>It owns no pause state of its own: <see cref="SimHost.Paused"/> is the truth and this surface
    ///       follows it, so pausing from any other route shows the same menu.</item>
    /// </list>
    ///
    /// Escape: three links are registered into the existing chain, at the orders the reference's ladder already
    /// reserved (uiShell.ts:114-120). <b>EscapeChain.cs itself is not edited</b> — <c>Register</c> inserts after
    /// equal-order handlers, so each live link runs immediately after the inert placeholder at its rung.
    /// </summary>
    [AddComponentMenu("Relight/Front End/Pause Menu")]
    public sealed class PauseMenuController : MonoBehaviour
    {
        [Tooltip("The document holding PauseMenu.uxml. Give its UIDocument a sortingOrder above the shell's.")]
        [SerializeField] private UIDocument document;

        [Tooltip("The session. Paused is read every frame; this surface follows it and never sets it alone.")]
        [SerializeField] private SimHost host;

        [Tooltip("The Escape chain this menu adds its three links to.")]
        [SerializeField] private EscapeChain escape;

        [Tooltip("Saving and loading. Also the source of the autosave notices.")]
        [SerializeField] private AutosaveController autosave;

        [Tooltip("The one settings surface, shared with the title screen.")]
        [SerializeField] private SettingsController settings;

        [Tooltip("Which save this session came from, and whether it has progress that is not in one.")]
        [SerializeField] private FrontEndBootstrap session;

        [Tooltip("SaveRowItem.uxml — one row of the Save and Load lists.")]
        [SerializeField] private VisualTreeAsset saveRow;

        [Tooltip("The scene the front end lives in.")]
        [SerializeField] private string titleSceneName = "MainMenu";

        [Tooltip("Seconds of sim time in one day, for the Day numbers on save rows. Must match the sim's.")]
        [SerializeField] private double daySeconds = 1200;

        [Tooltip("This build's map id, so a save from another map is shown disabled with the store's reason.")]
        [SerializeField] private string currentMapId = "";

        /// <summary>How long a transient notice stays up (§8's alert budget: short, and never urgent).</summary>
        private const double NoticeSeconds = 4;

        private VisualElement _root, _overlay, _screenPause, _screenSave, _screenLoad, _screenSettings, _settingsHost;
        private Label _noticeStrip, _pauseNotice, _saveNotice, _loadNotice, _saveEmpty, _noSaves,
                      _manualHeading, _autosaveHeading, _saveHeading;
        private VisualElement _saveList, _manualList, _autosaveList;
        private TextField _saveName;
        private Button _resume, _saveButton, _loadButton, _settingsButton, _exit, _saveConfirm, _saveBack, _loadBack, _settingsBack;

        private ConfirmDialog _modal;
        private SaveGateway _saves;
        private string _screen = "pause";
        private bool _settingsBound;
        private bool _wasPaused;
        private UiShell _adminShell;
        public bool AdminAvailable => _modal?.IsOpen != true && (!IsOpen || _screen == "pause");
        private double _noticeUntil;
        private SaveResult _lastSeenAutosave;
        private bool _sawStopped;

        private void Awake()
        {
            if (document == null) document = GetComponent<UIDocument>();
            if (host == null) host = FindAnyObjectByType<SimHost>();
            if (escape == null) escape = FindAnyObjectByType<EscapeChain>();
            if (autosave == null) autosave = FindAnyObjectByType<AutosaveController>();
            if (session == null) session = FindAnyObjectByType<FrontEndBootstrap>();
            if (settings == null) settings = FindAnyObjectByType<SettingsController>();
        }

        private void OnEnable()
        {
            Bind();
            Register();
        }

        private void OnDisable()
        {
            if (escape == null) return;
            for (var i = 0; i < _links.Count; i++) escape.Unregister(_links[i]);
            _links.Clear();
            if (_modal != null) escape.Unregister(_modal);
        }

        private readonly List<IEscapeHandler> _links = new List<IEscapeHandler>();

        /// <summary>
        /// Fill the three rungs the reference's ladder reserved. The chain is not edited: registering at an
        /// existing order puts the live link immediately after the placeholder that holds the rung.
        /// </summary>
        private void Register()
        {
            if (escape == null) return;
            if (_modal != null) escape.Register(_modal);                                   // 300, the modal first
            Add(new EscapeChain.Link(EscapeOrder.CloseModalChild, CloseChildScreen));      // 400
            Add(new EscapeChain.Link(EscapeOrder.Unpause, Unpause));                       // 500
        }

        private void Add(IEscapeHandler link)
        {
            escape.Register(link);
            _links.Add(link);
        }

        // ---- document -------------------------------------------------------------------------------------

        private void Bind()
        {
            _root = document == null ? null : document.rootVisualElement;
            if (_root == null) return;

            _overlay = _root.Q<VisualElement>("pause-menu");
            _noticeStrip = _root.Q<Label>("autosave-notice");

            _screenPause = _root.Q<VisualElement>("screen-pause");
            _screenSave = _root.Q<VisualElement>("screen-save");
            _screenLoad = _root.Q<VisualElement>("screen-load");
            _screenSettings = _root.Q<VisualElement>("screen-settings");
            _settingsHost = _root.Q<VisualElement>("settings-host");

            _resume = _root.Q<Button>("pause-resume");
            _saveButton = _root.Q<Button>("pause-save");
            _loadButton = _root.Q<Button>("pause-load");
            _settingsButton = _root.Q<Button>("pause-settings");
            _exit = _root.Q<Button>("pause-exit");
            _pauseNotice = _root.Q<Label>("pause-notice");

            _saveHeading = _root.Q<Label>("save-heading");
            _saveList = _root.Q<VisualElement>("save-list");
            _saveEmpty = _root.Q<Label>("save-empty");
            _saveName = _root.Q<TextField>("save-name");
            _saveConfirm = _root.Q<Button>("save-confirm");
            _saveBack = _root.Q<Button>("save-back");
            _saveNotice = _root.Q<Label>("save-notice");

            _manualHeading = _root.Q<Label>("manual-heading");
            _manualList = _root.Q<VisualElement>("manual-list");
            _autosaveHeading = _root.Q<Label>("autosave-heading");
            _autosaveList = _root.Q<VisualElement>("autosave-list");
            _noSaves = _root.Q<Label>("no-saves");
            _loadBack = _root.Q<Button>("load-back");
            _loadNotice = _root.Q<Label>("load-notice");
            _settingsBack = _root.Q<Button>("settings-back");

            var title = _root.Q<Label>("pause-title");
            if (title != null) title.text = FrontEndText.Paused;
            if (_resume != null) { _resume.text = FrontEndText.Resume; _resume.clicked += Resume; }
            if (_saveButton != null) { _saveButton.text = FrontEndText.Save; _saveButton.clicked += OpenSave; }
            if (_loadButton != null) { _loadButton.text = FrontEndText.Load; _loadButton.clicked += OpenLoad; }
            if (_settingsButton != null) { _settingsButton.text = FrontEndText.Settings; _settingsButton.clicked += OpenSettings; }
            if (_exit != null) { _exit.text = FrontEndText.SaveAndExit; _exit.clicked += OnExit; }

            if (_saveHeading != null) _saveHeading.text = FrontEndText.ManualGroup;
            if (_manualHeading != null) _manualHeading.text = FrontEndText.ManualGroup;
            if (_autosaveHeading != null) _autosaveHeading.text = FrontEndText.AutosaveGroup;
            var nameLabel = _root.Q<Label>("save-name-label");
            if (nameLabel != null) nameLabel.text = FrontEndText.SaveSlotNameLabel;
            if (_saveConfirm != null) { _saveConfirm.text = FrontEndText.SaveIntoSlot; _saveConfirm.clicked += OnSaveNew; }
            if (_saveBack != null) { _saveBack.text = FrontEndText.Back; _saveBack.clicked += () => Show("pause"); }
            if (_loadBack != null) { _loadBack.text = FrontEndText.Back; _loadBack.clicked += () => Show("pause"); }
            if (_settingsBack != null) { _settingsBack.text = FrontEndText.Back; _settingsBack.clicked += () => Show("pause"); }

            var modalRoot = _root.Q<VisualElement>("modal");
            if (modalRoot != null) _modal = new ConfirmDialog(modalRoot);

            _saves = autosave?.Store != null ? new SaveGateway(autosave.Store) : null;

            Show("pause");
            ShowOverlay(host != null && host.Paused);
        }

        // ---- the frame ------------------------------------------------------------------------------------

        private void Update()
        {
            if (host == null) return;

            if (_adminShell == null) _adminShell = FindAnyObjectByType<UiShell>();
            var showPause = host.Paused && _adminShell?.Active != "admin-panel";
            if (showPause != _wasPaused)
            {
                _wasPaused = showPause;
                ShowOverlay(_wasPaused);
                if (_wasPaused) { Show("pause"); ShowStartupNotice(); }
            }

            PollAutosave();
            if (_noticeUntil > 0 && Time.unscaledTimeAsDouble >= _noticeUntil) HideNotice();
        }

        private void ShowOverlay(bool open)
        {
            _overlay?.EnableInClassList("hidden", !open);
            if (open) Refresh();
        }

        private void Refresh()
        {
            if (_pauseNotice != null && _pauseNotice.text.Length == 0 && session != null && session.Dirty.EverSaved)
                _pauseNotice.text = "";
            if (_saveButton != null) _saveButton.SetEnabled(autosave != null);
            if (_loadButton != null) _loadButton.SetEnabled(autosave != null);
        }

        private void ShowStartupNotice()
        {
            var line = session?.TakeStartupNotice();
            if (string.IsNullOrEmpty(line) || _pauseNotice == null) return;
            _pauseNotice.text = line;
        }

        // ---- escape links ---------------------------------------------------------------------------------

        /// <summary>Rung 400. A child screen opened from Pause closes back to Pause, and Escape stops there.</summary>
        private bool CloseChildScreen()
        {
            if (!IsOpen || _screen == "pause") return false;
            Show("pause");
            return true;
        }

        /// <summary>Rung 500. With the menu open and nothing inside it, Escape resumes.</summary>
        private bool Unpause()
        {
            if (!IsOpen) return false;
            Resume();
            return true;
        }

        private bool IsOpen => host != null && host.Paused && _adminShell?.Active != "admin-panel";

        private void Resume()
        {
            if (host != null) host.Paused = false;
        }

        // ---- screens --------------------------------------------------------------------------------------

        private void Show(string screen)
        {
            _screen = screen;
            _screenPause?.EnableInClassList("hidden", screen != "pause");
            _screenSave?.EnableInClassList("hidden", screen != "save");
            _screenLoad?.EnableInClassList("hidden", screen != "load");
            _screenSettings?.EnableInClassList("hidden", screen != "settings");
        }

        private void OpenSettings()
        {
            if (settings != null && !_settingsBound && _settingsHost != null)
            {
                settings.Ask = (question, chosen) => _modal?.Show(question, chosen);
                // Only a running session can put the action bar back to its default, so only here is the control
                // offered at all (§3.8.3).
                settings.RestoreQuickbar = RestoreQuickbar;
                settings.Bind(_settingsHost);
                _settingsBound = true;
            }
            Show("settings");
        }

        /// <summary>
        /// Ten <c>AssignBarCommand(i, "")</c>, which is what "default" means: an empty bar. The UI does not write
        /// the bar — every slot is cleared by a command the sim accepts or refuses, as everywhere else.
        /// </summary>
        private void RestoreQuickbar()
        {
            var sim = host?.Simulation;
            if (sim == null) return;
            for (var i = 0; i < WeaponRules.BarSlots; i++) sim.Apply(new AssignBarCommand(i, ""));
            for (var i = 0; i < WeaponRules.DefaultBar.Length; i++) sim.Apply(new AssignBarCommand(i, WeaponRules.DefaultBar[i]));
        }

        // ---- Save -----------------------------------------------------------------------------------------

        private void OpenSave()
        {
            Show("save");
            Say(_saveNotice, "");
            PaintSaveList();
        }

        private void PaintSaveList()
        {
            if (_saves == null) return;
            var rows = _saves.Manual();
            var painted = SaveListView.Paint(
                saveRow, _saveList, rows, currentMapId, daySeconds, null,
                FrontEndText.SaveIntoSlot, OnOverwrite, null,
                // A save made on another map can be overwritten; only loading it is refused. The default rule
                // would disable it here, so the Save screen supplies its own: nothing blocks an overwrite.
                primaryBlocked: _ => "");
            if (_saveEmpty != null)
            {
                _saveEmpty.text = painted == 0 ? FrontEndText.NoSavesYet : "";
                _saveEmpty.EnableInClassList("hidden", painted != 0);
            }
        }

        private void OnOverwrite(SaveRow row)
        {
            if (row == null || _modal == null) return;
            var day = SaveRowFormatter.Day(row.T, daySeconds);
            var when = SaveRowFormatter.SavedAtText(row.SavedAt);
            _modal.Show(FrontEndText.Overwrite(row.Label, day, when), index =>
            {
                if (index == 0) DoSave(row.Name);
            });
        }

        private void OnSaveNew()
        {
            var name = _saveName == null ? "" : (_saveName.value ?? "").Trim();
            // The store's own name rule, in the store's own words. The UI invents no second rule.
            var problem = SaveStore.SlotNameProblem(name);
            if (problem.Length > 0) { Say(_saveNotice, problem, true); return; }

            if (_saves != null && _saves.Store.Exists(name))
            {
                var existing = Find(_saves.Manual(), name);
                if (existing != null && _modal != null)
                {
                    var day = SaveRowFormatter.Day(existing.T, daySeconds);
                    var when = SaveRowFormatter.SavedAtText(existing.SavedAt);
                    _modal.Show(FrontEndText.Overwrite(existing.Label, day, when), index =>
                    {
                        if (index == 0) DoSave(name);
                    });
                    return;
                }
            }
            DoSave(name);
        }

        private static SaveRow Find(IReadOnlyList<SaveRow> rows, string name)
        {
            for (var i = 0; i < rows.Count; i++)
                if (string.Equals(rows[i].Name, name, StringComparison.Ordinal)) return rows[i];
            return null;
        }

        /// <summary>
        /// Save, then say so. §2.7.3: a failed manual save is a modal with three ways out, and it ends by saying
        /// the previous save is unchanged — which is true, because the write protocol keeps it.
        /// </summary>
        private void DoSave(string name, Action then = null)
        {
            if (autosave == null) { Say(_saveNotice, "saving is not available in this scene", true); return; }
            var result = autosave.Save(name);
            if (result != null && result.Ok)
            {
                session?.MarkSaved(name, false);
                Say(_saveNotice, FrontEndText.SavedTo(SaveRowFormatter.ManualLabel(name)));
                PaintSaveList();
                then?.Invoke();
                return;
            }

            var reason = result == null ? "" : result.Reason;
            if (_modal == null) { Say(_saveNotice, reason, true); return; }
            _modal.Show(FrontEndText.CouldNotSave(reason), index =>
            {
                if (index == 0) DoSave(name, then);           // Retry
                else if (index == 1) OpenSave();              // Choose another slot
                // index 2 — Continue playing — changes nothing, which is the rule for the last button.
            });
        }

        // ---- Load -----------------------------------------------------------------------------------------

        private void OpenLoad()
        {
            Show("load");
            Say(_loadNotice, "");
            PaintLoadLists();
        }

        private void PaintLoadLists()
        {
            if (_saves == null) return;
            var manual = _saves.Manual();
            var autos = _saves.Autosaves();
            var all = new List<SaveRow>(manual.Count + autos.Count);
            all.AddRange(manual);
            all.AddRange(autos);
            var latest = SaveCatalogue.LatestAutosave(all, currentMapId);

            var m = SaveListView.Paint(saveRow, _manualList, manual, currentMapId, daySeconds, latest,
                FrontEndText.LoadRowAction, AskLoad, AskDelete, DeleteBlocked);
            var a = SaveListView.Paint(saveRow, _autosaveList, autos, currentMapId, daySeconds, latest,
                FrontEndText.LoadRowAction, AskLoad, AskDelete, DeleteBlocked);

            _manualHeading?.EnableInClassList("hidden", m == 0);
            _autosaveHeading?.EnableInClassList("hidden", a == 0);
            if (_noSaves != null)
            {
                _noSaves.text = m + a == 0 ? FrontEndText.NoSavesYet : "";
                _noSaves.EnableInClassList("hidden", m + a != 0);
            }
        }

        /// <summary>§2.6.4: the save this session is running is never offered for deletion.</summary>
        private string DeleteBlocked(SaveRow row)
        {
            if (session == null || row == null) return "";
            if (string.IsNullOrEmpty(session.LoadedName)) return "";
            return row.IsAutosave == session.LoadedIsAutosave
                && string.Equals(row.Name, session.LoadedName, StringComparison.Ordinal)
                ? FrontEndText.DeleteLoadedBlocked : "";
        }

        private void AskLoad(SaveRow row)
        {
            if (row == null) return;
            var dirty = session != null && host != null && session.Dirty.IsDirty(host.TotalTicks);
            if (!dirty || _modal == null) { DoLoad(row); return; }

            _modal.Show(FrontEndText.LoadThisSave(session.Dirty.SavedAtText), index =>
            {
                if (index == 0) SaveThen(() => DoLoad(row));      // Save and load
                else if (index == 1) DoLoad(row);                 // Load without saving
            });
        }

        private void DoLoad(SaveRow row)
        {
            if (autosave == null) { Say(_loadNotice, "saving is not available in this scene", true); return; }
            var result = row.IsAutosave ? autosave.LoadAutosave(row.Name) : autosave.Load(row.Name);
            if (result == null || !result.Ok)
            {
                // The store's refusal, verbatim. This is the same sentence the disabled row already carried.
                Say(_loadNotice, result == null ? "" : result.Reason, true);
                return;
            }
            session?.MarkLoaded(row.Name, row.IsAutosave);
            Say(_loadNotice, string.IsNullOrEmpty(result.Recovered) ? "" : result.Recovered);
            Show("pause");
            Resume();
        }

        private void AskDelete(SaveRow row)
        {
            if (row == null || _modal == null) return;
            var blocked = DeleteBlocked(row);
            if (blocked.Length > 0) { Say(_loadNotice, blocked, true); return; }

            var day = SaveRowFormatter.Day(row.T, daySeconds);
            var when = SaveRowFormatter.SavedAtText(row.SavedAt);
            _modal.Show(FrontEndText.Delete(row.Label, day, when), index =>
            {
                if (index != 0) return;
                var problem = _saves?.Delete(row) ?? "";
                Say(_loadNotice, problem, problem.Length > 0);
                PaintLoadLists();
            });
        }

        // ---- Save and exit to title -----------------------------------------------------------------------

        /// <summary>
        /// §2.6.5: this is the old "Start a new city" position, restated. §2.6.4 gives the shape of the question —
        /// the same one Quit asks, because leaving the session is what is at stake either way.
        /// </summary>
        private void OnExit()
        {
            var dirty = session != null && host != null && session.Dirty.IsDirty(host.TotalTicks);
            if (!dirty || _modal == null) { Leave(); return; }

            _modal.Show(FrontEndText.QuitGame(session.Dirty.SavedAtText), index =>
            {
                if (index == 0) SaveThen(Leave);      // Save and quit
                else if (index == 1) Leave();         // Quit without saving
            });
        }

        /// <summary>
        /// Save into the slot this session came from, or send the player to the Save screen to choose one.
        /// "Save and …" must never silently invent a slot name.
        /// </summary>
        private void SaveThen(Action then)
        {
            if (session != null && !session.LoadedIsAutosave && !string.IsNullOrEmpty(session.LoadedName))
            {
                DoSave(session.LoadedName, then);
                return;
            }
            OpenSave();
            Say(_saveNotice, FrontEndText.SaveSlotNameLabel);
        }

        private void Leave()
        {
            NewGameRequest.Clear();
            if (host != null) host.Paused = false;
            if (!string.IsNullOrEmpty(titleSceneName) && Application.CanStreamedLevelBeLoaded(titleSceneName))
                SceneManager.LoadScene(titleSceneName, LoadSceneMode.Single);
        }

        // ---- autosave notices (§2.7.1, §2.7.3) ------------------------------------------------------------

        private void PollAutosave()
        {
            var scheduler = autosave?.Scheduler;
            if (scheduler == null) return;

            // Three consecutive failures is a persistent strip, not a transient notice.
            if (scheduler.Stopped)
            {
                if (!_sawStopped) { _sawStopped = true; Notice(FrontEndText.AutosaveStopped, true, false); }
                return;
            }
            _sawStopped = false;

            var last = scheduler.Last;
            if (ReferenceEquals(last, _lastSeenAutosave) || last == null) return;
            _lastSeenAutosave = last;

            if (!last.Ok) { Notice(FrontEndText.AutosaveFailed, true, true); return; }

            var state = host?.Simulation?.State;
            if (state == null) return;
            var day = SaveRowFormatter.Day(state.T, daySeconds);
            var playtime = PlayClock.Clock(PlayClock.SecondsFor(state.Tick));
            Notice(FrontEndText.Autosaved(day, playtime), false, true);
        }

        private void Notice(string text, bool bad, bool transient)
        {
            if (_noticeStrip == null) return;
            _noticeStrip.text = text;
            _noticeStrip.EnableInClassList("bad", bad);
            _noticeStrip.EnableInClassList("hidden", false);
            _noticeUntil = transient ? Time.unscaledTimeAsDouble + NoticeSeconds : 0;
        }

        private void HideNotice()
        {
            _noticeUntil = 0;
            _noticeStrip?.EnableInClassList("hidden", true);
        }

        private static void Say(Label label, string text, bool bad = false)
        {
            if (label == null) return;
            label.text = text ?? "";
            label.EnableInClassList("bad", bad && label.text.Length > 0);
        }
    }
}
