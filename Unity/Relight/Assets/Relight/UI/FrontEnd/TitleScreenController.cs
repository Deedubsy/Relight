using System.Collections.Generic;
using System.Globalization;
using Relight.Sim;
using Relight.Sim.UI;
using Relight.UI.Settings;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Relight.UI.FrontEnd
{
    /// <summary>
    /// C-10. The title screen (UI_AND_ONBOARDING.md §2.6). It drives <c>TitleScreen.uxml</c> and nothing else:
    /// every name it looks up is authored there, it creates only the things that are data (one row per save, one
    /// button per confirmation choice), and it never writes a style property.
    ///
    /// The rules it exists to keep, each of which is a line in §2.6 rather than a preference:
    /// <list type="bullet">
    /// <item><b>Continue is never hidden.</b> With no valid save it is shown disabled and the reason is printed
    ///       beside it — "No saved game yet" (§2.6.1, and RI-02B_UI_SPEC.md:38-43's disabled-control rule).</item>
    /// <item><b>Continue takes the newest valid save</b>, manual or autosave, and says so when it has to step
    ///       past one that will not open (§2.6.3). <see cref="ContinuePicker"/> decides; this only obeys.</item>
    /// <item><b>A save that cannot be loaded is shown, disabled, with the reason</b> — never hidden, never
    ///       deleted by the game (§2.6.2). The reason is the store's own text; the map comparison is
    ///       <see cref="SaveSerializer.MapProblem"/>'s and is not repeated here.</item>
    /// <item><b>Every destructive step is confirmed and the confirmation says what will be lost</b> (§2.6.4).
    ///       The wording is <see cref="FrontEndText"/>'s, which is engine-free and tested.</item>
    /// </list>
    ///
    /// There is no <c>SimHost</c> in <c>MainMenu</c>, so there is nothing to overwrite here: the New Game and Load
    /// confirmations of §2.6.4 belong to the <i>in-game</i> pause menu, where a session really is running, and
    /// this screen starts what the player asked for directly. The one destructive act available from here is
    /// Delete, which is confirmed.
    ///
    /// Escape is read from <see cref="Keyboard"/> rather than from <c>InputRouter</c>: the front end is a separate
    /// scene with no action asset, no <c>UiShell</c> and no <c>EscapeChain</c>, and giving it one would mean a
    /// second set of panels for a shell that is not there. In the world, Escape is the chain's, exactly as before.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Relight/Front End/Title Screen")]
    public sealed class TitleScreenController : MonoBehaviour
    {
        [Tooltip("The document holding TitleScreen.uxml. Found on this object if left empty.")]
        [SerializeField] private UIDocument document;

        [Tooltip("SaveRowItem.uxml — one instance per save row.")]
        [SerializeField] private VisualTreeAsset saveRow;

        [Tooltip("The settings surface (§3.8). The same component is bound by the pause menu in the world scene.")]
        [SerializeField] private SettingsController settings;

        [Tooltip("Scene loaded when a game starts. It is WorldBootstrap that starts the session there.")]
        [SerializeField] private string worldSceneName = "World";

        [Tooltip("Build string under the game name. Empty uses Application.version.")]
        [SerializeField] private string buildString = "";

        [Tooltip("The map this build runs on, as WorldBootstrap reports it (WorldGeometryAsset.MapId); empty for " +
                 "the synthetic map. Empty means no row is refused for its map here — the load itself still " +
                 "refuses, in the same words.")]
        [SerializeField] private string currentMapId = "";

        private VisualElement _root;
        private VisualElement _screenMenu, _screenNew, _screenLoad, _screenSettings, _settingsHost;
        private Button _continue, _new, _load, _settingsButton, _quit;
        private Label _continueReason, _newNotice, _loadNotice, _noSaves, _rulesetLine, _buildLabel;
        private Label _manualHeading, _autosaveHeading;
        private VisualElement _manualList, _autosaveList;
        private TextField _seedField;
        private ConfirmDialog _modal;

        private SaveGateway _saves;
        private readonly HashSet<string> _failed = new HashSet<string>();
        private bool _bound;
        private bool _settingsBound;

        /// <summary>The screen on show, for a test or a debug readout.</summary>
        public string Screen { get; private set; } = "menu";

        private void Awake()
        {
            if (document == null) document = GetComponent<UIDocument>();
            _saves = new SaveGateway(new SystemFileSystem(), Application.persistentDataPath);
        }

        private void OnEnable()
        {
            Bind();
            Refresh();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) Escape();
        }

        /// <summary>Escape: dismiss the question, else go back to the menu, else nothing (there is no world).</summary>
        public bool Escape()
        {
            if (_modal != null && _modal.IsOpen) { _modal.Cancel(); return true; }
            if (Screen == "menu") return false;
            Show("menu");
            return true;
        }

        /// <summary>Find every authored name once and wire the buttons.</summary>
        public void Bind()
        {
            if (_bound) return;
            _root = document == null ? null : document.rootVisualElement;
            if (_root == null) return;

            _buildLabel = _root.Q<Label>("title-build");
            if (_buildLabel != null)
                _buildLabel.text = string.IsNullOrEmpty(buildString) ? Application.version : buildString;

            _screenMenu = _root.Q<VisualElement>("screen-menu");
            _screenNew = _root.Q<VisualElement>("screen-new");
            _screenLoad = _root.Q<VisualElement>("screen-load");
            _screenSettings = _root.Q<VisualElement>("screen-settings");
            _settingsHost = _root.Q<VisualElement>("settings-host");

            _continue = _root.Q<Button>("menu-continue");
            _new = _root.Q<Button>("menu-new");
            _load = _root.Q<Button>("menu-load");
            _settingsButton = _root.Q<Button>("menu-settings");
            _quit = _root.Q<Button>("menu-quit");
            _continueReason = _root.Q<Label>("continue-reason");

            // The five entries are §2.6.1's, in its order and in its words.
            if (_continue != null) { _continue.text = FrontEndText.Continue; _continue.clicked += OnContinue; }
            if (_new != null) { _new.text = FrontEndText.NewGame; _new.clicked += () => Show("new"); }
            if (_load != null) { _load.text = FrontEndText.Load; _load.clicked += () => { Show("load"); PaintSaves(); }; }
            if (_settingsButton != null) { _settingsButton.text = FrontEndText.Settings; _settingsButton.clicked += OnSettings; }
            if (_quit != null) { _quit.text = FrontEndText.Quit; _quit.clicked += OnQuit; }

            _seedField = _root.Q<TextField>("seed-field");
            if (_seedField != null) _seedField.value = NewGameRequest.DefaultSeed.ToString(CultureInfo.InvariantCulture);
            var seedLabel = _root.Q<Label>("seed-label");
            if (seedLabel != null) seedLabel.text = FrontEndText.SeedLabel;
            _rulesetLine = _root.Q<Label>("ruleset-line");
            if (_rulesetLine != null) _rulesetLine.text = FrontEndText.RulesetLine;
            _newNotice = _root.Q<Label>("new-notice");
            var start = _root.Q<Button>("new-start");
            if (start != null) { start.text = FrontEndText.Start; start.clicked += OnStart; }
            var newBack = _root.Q<Button>("new-back");
            if (newBack != null) { newBack.text = FrontEndText.Back; newBack.clicked += () => Show("menu"); }

            _manualHeading = _root.Q<Label>("manual-heading");
            if (_manualHeading != null) _manualHeading.text = FrontEndText.ManualGroup;
            _autosaveHeading = _root.Q<Label>("autosave-heading");
            if (_autosaveHeading != null) _autosaveHeading.text = FrontEndText.AutosaveGroup;
            _manualList = _root.Q<VisualElement>("manual-list");
            _autosaveList = _root.Q<VisualElement>("autosave-list");
            _noSaves = _root.Q<Label>("no-saves");
            if (_noSaves != null) _noSaves.text = FrontEndText.NoSavesYet;
            _loadNotice = _root.Q<Label>("load-notice");
            var loadBack = _root.Q<Button>("load-back");
            if (loadBack != null) { loadBack.text = FrontEndText.Back; loadBack.clicked += () => Show("menu"); }

            var settingsBack = _root.Q<Button>("settings-back");
            if (settingsBack != null) { settingsBack.text = FrontEndText.Back; settingsBack.clicked += () => Show("menu"); }

            _modal = new ConfirmDialog(_root.Q<VisualElement>("modal"));
            _bound = true;
            Show("menu");
        }

        /// <summary>Re-read the store and set Continue's state. Cheap: it reads headers, not states.</summary>
        public void Refresh()
        {
            if (!_bound) return;
            var choice = ContinuePicker.Pick(_saves.All(), currentMapId, _failed);
            if (_continue != null)
            {
                _continue.SetEnabled(choice.Enabled);
                _continue.tooltip = choice.Enabled ? "" : choice.DisabledReason;
            }
            if (_continueReason != null)
            {
                // Never hidden: an enabled Continue simply has nothing to explain.
                _continueReason.text = choice.Enabled ? choice.Notice : choice.DisabledReason;
                _continueReason.EnableInClassList("hidden", _continueReason.text.Length == 0);
            }
        }

        private void Show(string screen)
        {
            Screen = screen;
            _screenMenu?.EnableInClassList("hidden", screen != "menu");
            _screenNew?.EnableInClassList("hidden", screen != "new");
            _screenLoad?.EnableInClassList("hidden", screen != "load");
            _screenSettings?.EnableInClassList("hidden", screen != "settings");
            if (screen == "menu") Refresh();
        }

        private void OnSettings()
        {
            if (settings != null && !_settingsBound && _settingsHost != null)
            {
                // The one modal on this screen answers the Settings screen's questions too, so lowering
                // "Autosaves kept" can ask before anything is deleted.
                settings.Ask = (question, chosen) => _modal?.Show(question, chosen);
                settings.Bind(_settingsHost);
                _settingsBound = true;
            }
            Show("settings");
        }

        // ---- Continue / New / Load ---------------------------------------------------------------------------

        private void OnContinue()
        {
            var choice = ContinuePicker.Pick(_saves.All(), currentMapId, _failed);
            if (!choice.Enabled)
            {
                Refresh();
                return;
            }
            // §2.6.3: when Continue steps past a save that will not open it says so. The sentence is shown here and
            // carried into the session, because the scene swap takes this screen away within a frame or two.
            if (choice.Notice.Length != 0)
            {
                if (_continueReason != null)
                {
                    _continueReason.text = choice.Notice;
                    _continueReason.RemoveFromClassList("hidden");
                }
                NewGameRequest.Notice = choice.Notice;
            }
            StartLoad(choice.Row);
        }

        private void OnStart()
        {
            var text = _seedField == null ? "" : _seedField.value;
            if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seed))
            {
                Say(_newNotice, "Enter a whole number for the seed.", true);
                return;
            }
            NewGameRequest.NewCity(seed);
            Enter();
        }

        private void StartLoad(SaveRow row)
        {
            if (row == null) return;
            NewGameRequest.LoadSave(row.Name, row.IsAutosave);
            Enter();
        }

        private void Enter()
        {
            if (string.IsNullOrEmpty(worldSceneName)) return;
            if (!Application.CanStreamedLevelBeLoaded(worldSceneName))
            {
                Debug.LogError("Relight: world scene '" + worldSceneName + "' is not in Build Settings.");
                Say(_newNotice, "The game could not be started; the world scene is missing from this build.", true);
                return;
            }
            SceneManager.LoadScene(worldSceneName, LoadSceneMode.Single);
        }

        // ---- The Load list ------------------------------------------------------------------------------------

        private void PaintSaves()
        {
            if (!_bound) return;
            var manual = _saves.Manual();
            var autos = _saves.Autosaves();
            var latest = SaveCatalogue.LatestAutosave(autos, currentMapId);

            SaveListView.Paint(saveRow, _manualList, manual, currentMapId, null,
                FrontEndText.LoadRowAction, StartLoad, Delete);
            SaveListView.Paint(saveRow, _autosaveList, autos, currentMapId, latest,
                FrontEndText.LoadRowAction, StartLoad, Delete);

            var empty = manual.Count == 0 && autos.Count == 0;
            _manualHeading?.EnableInClassList("hidden", manual.Count == 0);
            _autosaveHeading?.EnableInClassList("hidden", autos.Count == 0);
            _noSaves?.EnableInClassList("hidden", !empty);
            Say(_loadNotice, "", false);
        }

        private void Delete(SaveRow row)
        {
            if (row == null || _modal == null) return;
            var when = SaveRowFormatter.SavedAtText(row.SavedAt);
            _modal.Show(FrontEndText.Delete(row.Label, SaveRowFormatter.Played(row), when), index =>
            {
                if (index != 0) return;                       // 0 = Delete, 1 = Cancel
                var problem = _saves.Delete(row);
                Say(_loadNotice, problem, problem.Length != 0);
                _failed.Remove(row.Name);
                PaintSaves();
                Refresh();
            });
        }

        private void OnQuit()
        {
            // No session runs behind the title screen, so there is no unsaved progress to warn about: §2.6.4's
            // Quit confirmation belongs to the pause menu, where there is.
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        private static void Say(Label label, string text, bool bad)
        {
            if (label == null) return;
            label.text = text ?? "";
            label.EnableInClassList("bad", bad && label.text.Length != 0);
        }
    }
}
