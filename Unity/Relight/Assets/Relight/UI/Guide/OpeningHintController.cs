using Relight.Presentation;
using Relight.Sim;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Relight.UI
{
    /// <summary>
    /// C-09. The only hint overlay in the game (UI_AND_ONBOARDING.md §7.2; reference hud.ts:32 and :69). One line
    /// of control help built from the LIVE bindings, and a "Dismiss hint" button whose answer is remembered.
    ///
    /// The reference stores the dismissal as <c>dismissedHints: ['opening']</c> inside its <c>relight.ui.v1</c>
    /// preferences blob; a Unity build has no localStorage, so the same fact is one
    /// <see cref="PlayerPrefs"/> key, <see cref="DismissedKey"/>. Settings' "Restore opening hint for new cities"
    /// is <see cref="Restore"/>.
    ///
    /// Which of the two §7.2 strings is shown follows the reference's own condition: the explicit one while
    /// movement or mining is still undemonstrated, the short one once both have been (engineer walk seconds and
    /// <c>Stats.Mined</c>, both of which survive a save, so a progressed save never gets the beginner line again).
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Relight/Opening Hint Controller")]
    public sealed class OpeningHintController : MonoBehaviour
    {
        /// <summary>Reference <c>dismissedHints: ['opening']</c> in <c>relight.ui.v1</c>.</summary>
        public const string DismissedKey = "relight.ui.v1.dismissedHints.opening";

        /// <summary>Set on an element instead of writing <c>style.display</c> (ui-uitk: classes only).</summary>
        public const string HiddenClass = "is-hidden";

        /// <summary>Reference hud.ts:66 — two seconds of walking is "the player has moved".</summary>
        public const double WalkedSeconds = 2.0;

        [Tooltip("The document holding OpeningHint.uxml. Found on this object if left empty.")]
        [SerializeField] private UIDocument document;

        [Tooltip("Read for the walk and mining counters. Found in the scene if left empty.")]
        [SerializeField] private SimHost host;

        [Tooltip("Supplies the live key bindings the hint names. Found in the scene if left empty.")]
        [SerializeField] private InputRouter router;

        [Tooltip("Read for the open drawer: the bar hides while a panel is open. Found in the scene if left empty.")]
        [SerializeField] private UiShell shell;

        private VisualElement _hint;
        private Label _text;
        private Button _dismiss;
        private InputActionAsset _actions;
        private string _painted = "";
        private bool _dismissed;

        /// <summary>True once the player has dismissed the hint (or a previous session did).</summary>
        public bool Dismissed => _dismissed;

        /// <summary>The line currently on screen, for tests.</summary>
        public string Text => _painted;

        /// <summary>Settings: "Restore opening hint for new cities" (UI_AND_ONBOARDING.md §7.2).</summary>
        public static void Restore()
        {
            PlayerPrefs.DeleteKey(DismissedKey);
            PlayerPrefs.Save();
        }

        private void Awake()
        {
            if (document == null) document = GetComponent<UIDocument>();
            if (host == null) host = FindAnyObjectByType<SimHost>();
            if (router == null) router = FindAnyObjectByType<InputRouter>();
            if (shell == null) shell = FindAnyObjectByType<UiShell>();
            _actions = router == null ? null : router.Actions;
            _dismissed = PlayerPrefs.GetInt(DismissedKey, 0) != 0;
        }

        private void OnEnable() => Bind();

        private void OnDisable()
        {
            if (_dismiss != null) _dismiss.clicked -= Dismiss;
        }

        /// <summary>Re-query the document. Public so a test (or a hot-reloaded UXML) can rebind.</summary>
        public void Bind()
        {
            var root = document == null ? null : document.rootVisualElement;
            if (root == null) return;
            _hint = root.Q<VisualElement>("opening-hint");
            _text = root.Q<Label>("hint-text");
            if (_dismiss != null) _dismiss.clicked -= Dismiss;
            _dismiss = root.Q<Button>("dismiss-hint");
            if (_dismiss != null) _dismiss.clicked += Dismiss;
            Paint();
        }

        private void Start()
        {
            if (_hint == null) Bind();   // see UiShell.Start: the document root may not exist yet in OnEnable
        }

        private void Update() => Paint();

        /// <summary>Reference hud.ts:32 — hide the hint for good and remember it.</summary>
        public void Dismiss()
        {
            _dismissed = true;
            PlayerPrefs.SetInt(DismissedKey, 1);
            PlayerPrefs.Save();
            Paint();
        }

        private void Paint()
        {
            if (_hint == null) return;
            // Correction pass: the bar is bottom-centre and the drawers are bottom-right, 720-820px wide, so at
            // 1280x720 the bar sat across the Backpack's lower-left corner. While a drawer is open the bar steps
            // aside (each drawer carries its own hint line); it returns, not dismissed, when the drawer closes.
            var covered = shell != null && shell.Active != null;
            _hint.EnableInClassList(HiddenClass, _dismissed || covered || Demonstrated());
            if (_dismissed || covered || Demonstrated()) return;
            var line = Line();
            if (line == _painted) return;
            _painted = line;
            if (_text != null) _text.text = line;
        }

        /// <summary>The two §7.2 strings, with every key taken from the live binding where one exists.</summary>
        private string Line()
        {
            var interact = Key("World/Equip", "E");
            var build = Key("Global/ToggleBuild", "B");
            if (Demonstrated())
                return interact + " interacts · " + Key("World/Move", "WASD") +
                       " moves · Build opens with " + build + ". Find all controls in Help.";
            return interact + " interacts · " + Key("World/Move", "W/A/S/D") +
                   " moves · " + build + " opens Build. Hold left-click to mine. " + DarkLine;
        }

        /// <summary>
        /// U-D-58: the port is always dark, and nothing else in the opening says so. One sentence on the first
        /// card names the flashlight and the HUD cue under the clock (ALWAYS_DARK_SPEC.md §3).
        /// </summary>
        private const string DarkLine =
            "It is always dark: your flashlight points at the mouse, and the clock shows whether you stand in light.";

        /// <summary>Both movement and mining have been shown to work (PLAYER_EXPERIENCE_CORRECTIONS scope F).</summary>
        private bool Demonstrated()
        {
            var sim = host == null ? null : host.Simulation;
            if (sim == null) return false;
            var st = sim.State;
            return WorldQueries.Engineer(sim.Context, st).Walked >= WalkedSeconds && st.Stats.Mined > 0;
        }

        /// <summary>
        /// The display string of a bound action, or the documented default when the action is not in
        /// <c>RelightControls</c> yet (Interact and Build arrive with the C-06/C-07 input work; the hint must not
        /// name a key that does not exist, and it must not go blank either).
        /// </summary>
        private string Key(string path, string fallback)
        {
            var action = _actions == null ? null : _actions.FindAction(path, false);
            if (action == null) return fallback;
            var shown = action.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontUseShortDisplayNames);
            return string.IsNullOrEmpty(shown) ? fallback : shown;
        }
    }
}
