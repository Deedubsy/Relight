using System.Collections.Generic;
using Relight.Presentation;
using UnityEngine;
using UnityEngine.UIElements;

namespace Relight.UI
{
    /// <summary>
    /// B-13. One panel at a time, Escape closes it, and the world keeps running underneath — the three rules the
    /// reference's shell is built on (uiShell.ts:60-101 <c>open</c>/<c>closeDrawer</c>; "Panels stay live: enemies
    /// and machines keep moving", uiShell.ts:159). It is the port of the drawer, not of the nav bar, the settings
    /// modal or the drag layer; those are C-06, C-07 and C-10.
    ///
    /// Panels are VisualElements already present in the UXML document, found by name. Nothing is created from C#:
    /// that is what makes the layout editable in UI Builder (TA §8.1), and the acceptance criterion for this task.
    /// A panel is shown or hidden with the <c>hidden</c> USS state, so a UI Builder change to its position, size or
    /// children survives untouched.
    ///
    /// Pause is <see cref="SimHost.Paused"/> and nothing else (U-D-04: fixed 1× with pause, no speed control).
    /// Opening a panel does NOT pause: the reference's drawer leaves the world running and only the modal pauses.
    ///
    /// Correction pass (C-02): Tab is the Backpack (<c>inventory-panel</c>) and B is the build menu
    /// (<c>build-panel</c>) — both keys live on the always-live Global map, because the key that OPENS a drawer
    /// cannot sit on the map that drawer switches off. The shell also answers <see cref="PointerOverUi"/>, which
    /// <c>WorldInput</c> consults before it turns a click into a world command: while a drawer is open the World
    /// map is off and nothing can leak through, but the HUD and the build toolbar are visible with the world live,
    /// and a click on them used to place a machine underneath. The shell publishes the probe as
    /// <see cref="WorldInput.UiPointerProbe"/> rather than being called directly, because Relight.Presentation
    /// cannot reference Relight.UI (the dependency runs the other way).
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Relight/UI Shell")]
    public sealed class UiShell : MonoBehaviour
    {
        [Tooltip("The document whose root holds the panels. Found on this object if left empty.")]
        [SerializeField] private UIDocument document;

        [Tooltip("Moves input between the World and UI action maps as panels open and close.")]
        [SerializeField] private InputRouter router;

        [Tooltip("Owns Escape. This shell registers the 'close the open panel' and 'pause' links.")]
        [SerializeField] private EscapeChain escape;

        [Tooltip("Paused by the last link of the escape chain.")]
        [SerializeField] private SimHost host;

        [Tooltip("Panel opened when the scene loads; empty (the Phase C default) opens nothing, so world controls are live from the first frame.")]
        [SerializeField] private string openOnStart = "";

        [Tooltip("Panel the Tab key toggles. C-02: the Backpack.")]
        [SerializeField] private string tabPanel = "inventory-panel";

        [Tooltip("Panel the B key toggles. C-02: the build menu.")]
        [SerializeField] private string buildPanel = "build-panel";

        [Tooltip("GP-UX-1: a movement key closes the open drawer instead of being swallowed by it.")]
        [SerializeField] private bool movementClosesPanels = true;

        private readonly Dictionary<string, VisualElement> _panels = new Dictionary<string, VisualElement>();
        private readonly MovementKeys _movement = new MovementKeys();
        private string _active;

        /// <summary>True when the last close was caused by a movement key. For tests and for the notice line.</summary>
        public bool ClosedByMovement { get; private set; }

        /// <summary>The open panel's id, or null. Reference <c>shell.active()</c>.</summary>
        public string Active => _active;

        /// <summary>Panel ids found in the document, in document order.</summary>
        public IReadOnlyCollection<string> PanelIds => _panels.Keys;

        private void Awake()
        {
            if (document == null) document = GetComponent<UIDocument>();
            if (router == null) router = GetComponent<InputRouter>();
            if (escape == null) escape = GetComponent<EscapeChain>();
            if (host == null) host = FindAnyObjectByType<SimHost>();
        }

        private void OnEnable()
        {
            Rebuild();
            // The chain ignores a second install, so this is safe whatever enabled first (see EscapeChain).
            if (escape != null) escape.InstallDefaults(CloseActive, TogglePause);
            // Hand the world the "is this point on a widget?" test. Presentation cannot reference UI, so the
            // dependency is inverted through a delegate the shell owns for as long as it is enabled.
            WorldInput.UiPointerProbe = PointerOverUi;
            WorldInput.UiTextInputFocused = TextInputFocused;
            if (!string.IsNullOrEmpty(openOnStart)) Open(openOnStart);
        }

        private void OnDisable()
        {
            // Only clear the probe if it is still ours: a second shell (a scene swap mid-frame) must keep its own.
            if (WorldInput.UiTextInputFocused?.Target == this) WorldInput.UiTextInputFocused = null;
            var probe = WorldInput.UiPointerProbe;
            if (probe != null && ReferenceEquals(probe.Target, this)) WorldInput.UiPointerProbe = null;
        }

        private void Start()
        {
            // UIDocument builds its root in OnEnable; if component order put us first, pick the panels up here.
            if (_panels.Count == 0)
            {
                Rebuild();
                if (!string.IsNullOrEmpty(openOnStart)) Open(openOnStart);
            }
        }

        private void Update()
        {
            // Escape and Pause live on the Global map, which is enabled whether or not a panel is open.
            if (router == null) return;
            if (router.Cancel != null && router.Cancel.WasPressedThisFrame()) escape?.Escape();
            if (router.Pause != null && router.Pause.WasPressedThisFrame()) TogglePause();
            if (!TextInputFocused() && router.TogglePanel != null && router.TogglePanel.WasPressedThisFrame() && _panels.Count > 0)
                ToggleBackpack();
            if (!TextInputFocused() && router.ToggleBuild != null && router.ToggleBuild.WasPressedThisFrame() && _panels.Count > 0)
                Toggle(buildPanel);
            CloseOnMovement();
        }

        /// <summary>
        /// GP-UX-1. A drawer is a thing the engineer is standing at, not a mode they are stuck in: pressing a key
        /// they walk with closes it and hands control straight back to the world — the Home workshop being the
        /// case the owner hit, where the only way out used to be Escape or Tab.
        ///
        /// Three guards, and each one is a case where the same keystroke means something else:
        /// <list type="bullet">
        /// <item>a focused text field — the save name, the rename box, the drawer's quantity — where W is a
        ///       letter the player is typing, not a step north;</item>
        /// <item>paused, which is also how the pause menu and the settings screen are up: there is no walking to
        ///       return to, so nothing is gained by throwing the drawer away underneath them;</item>
        /// <item>a live drag, because the player already has a stack in hand and the gesture owns the pointer;
        ///       Escape (link 100) is what cancels that, and it must go on being what cancels it.</item>
        /// </list>
        /// The World map is re-enabled by <see cref="CloseActive"/> as usual. Nothing re-sends the keystroke and
        /// nothing needs to: <c>World/Move</c> is a Value action with <c>initialStateCheck</c>, so the map comes
        /// back reading the keys that are held right now and the engineer walks without a second press.
        /// </summary>
        private void CloseOnMovement()
        {
            ClosedByMovement = false;
            if (!movementClosesPanels || _active == null) return;
            if (host != null && host.Paused) return;
            if (UiDrag.Dragging) return;
            if (TextInputFocused()) return;
            if (!_movement.PressedThisFrame(router.Move)) return;
            ClosedByMovement = CloseActive();
        }

        /// <summary>Re-scan the document for panels. Called on enable and after a document swap.</summary>
        public void Rebuild()
        {
            _panels.Clear();
            _active = null;
            var root = document == null ? null : document.rootVisualElement;
            if (root == null) return;
            // Every element carrying the "panel" class is a panel, and its name is its id.
            root.Query<VisualElement>(className: "panel").ForEach(e =>
            {
                if (string.IsNullOrEmpty(e.name)) return;
                _panels[e.name] = e;
                e.style.display = DisplayStyle.None;
            });
        }

        /// <summary>Open one panel, closing whichever was open (reference uiShell.ts:60 <c>open</c>).</summary>
        public bool Open(string id)
        {
            if (id == null || !_panels.TryGetValue(id, out var panel)) return false;
            if (_active == id) return true;
            UiDrag.Cancel();
            if (_active != null && _panels.TryGetValue(_active, out var previous))
                previous.style.display = DisplayStyle.None;
            panel.style.display = DisplayStyle.Flex;
            _active = id;
            router?.SetUiActive(true);
            return true;
        }

        private bool TextInputFocused()
        {
            if (_active == null) return false;
            var focused = document?.rootVisualElement?.focusController?.focusedElement as VisualElement;
            return focused is TextField || focused?.GetFirstAncestorOfType<TextField>() != null
                || focused is IntegerField || focused?.GetFirstAncestorOfType<IntegerField>() != null;
        }

        public bool ToggleBackpack()
        {
            if (_active == "inventory-panel") return CloseActive();
            GetComponent<InventoryPanelController>()?.OpenStore(-1);
            return Open("inventory-panel");
        }

        /// <summary>Reference <c>toggle</c>.</summary>
        public bool Toggle(string id) => _active == id ? !CloseActive() : Open(id);

        /// <summary>Close the open panel. Returns true if there was one (the escape chain's contract).</summary>
        public bool CloseActive()
        {
            if (_active == null) return false;
            UiDrag.Cancel();
            if (_panels.TryGetValue(_active, out var panel)) panel.style.display = DisplayStyle.None;
            _active = null;
            router?.SetUiActive(false);
            return true;
        }

        /// <summary>
        /// C-02. True when <paramref name="screenPosition"/> (mouse/touch screen coordinates, bottom-left origin,
        /// as the Input System reports them) lands on a piece of interface. <c>WorldInput</c> asks this before it
        /// turns a press into a world command, so a click on the HUD or on the build toolbar no longer places a
        /// machine or fires the rifle in the world behind it.
        ///
        /// Two tests, in order:
        /// <list type="number">
        /// <item><see cref="IPanel.Pick"/> at the converted point. This is the exact answer for anything that takes
        ///       pointer events — buttons, slots, the drawer body — and it respects transforms and clipping.</item>
        /// <item>A bounds test over the visible <c>.hud-block</c> and <c>.panel</c> elements. Needed because the
        ///       whole HUD is authored <c>picking-mode="Ignore"</c> (Hud.uxml) so it never steals the pointer from
        ///       the world; Pick therefore walks straight past it and returns the document root. Ignoring the
        ///       pointer is right for hover, but a CLICK on the readouts must still not reach the world.</item>
        /// </list>
        /// Pick returning the document root (or a bare template host) counts as "not over UI": the root is a
        /// full-screen pickable element, so treating it as a hit would swallow every click in the game.
        /// </summary>
        public bool PointerOverUi(Vector2 screenPosition)
        {
            var root = document == null ? null : document.rootVisualElement;
            if (root == null) return false;
            var panel = root.panel;
            if (panel == null) return false;

            var point = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(screenPosition.x, Screen.height - screenPosition.y));
            if (float.IsNaN(point.x) || float.IsNaN(point.y)) return false;

            if (PickedWidget(panel, root, point)) return true;
            return OverVisible(root, point, "panel") || OverVisible(root, point, "hud-block");
        }

        // Pick, minus the two "hits" that are really misses: the document root itself and the TemplateContainer
        // hosts GameUI.uxml uses to place each authored document.
        private static bool PickedWidget(IPanel panel, VisualElement root, Vector2 point)
        {
            var picked = panel.Pick(point);
            if (picked == null || picked == root) return false;
            var inside = false;
            for (var e = picked; e != null; e = e.parent)
            {
                if (e != root) continue;
                inside = true;
                break;
            }
            if (!inside) return false;
            return !(picked is TemplateContainer);
        }

        // worldBound is already in panel coordinates, the same space ScreenToPanel produced.
        private static bool OverVisible(VisualElement root, Vector2 point, string className)
        {
            var hit = false;
            root.Query<VisualElement>(className: className).ForEach(e =>
            {
                if (hit) return;
                if (e.resolvedStyle.display == DisplayStyle.None) return;
                if (e.resolvedStyle.visibility == Visibility.Hidden) return;
                var bounds = e.worldBound;
                if (bounds.width <= 0f || bounds.height <= 0f) return;
                if (bounds.Contains(point)) hit = true;
            });
            return hit;
        }

        /// <summary>The last link of the escape chain: pause, or unpause. Always consumes Escape.</summary>
        public bool TogglePause()
        {
            if (host == null) return false;
            host.Paused = !host.Paused;
            return true;
        }
    }
}
