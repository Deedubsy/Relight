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

        [Tooltip("Panel opened when the scene loads; empty opens nothing.")]
        [SerializeField] private string openOnStart = "status-panel";

        private readonly Dictionary<string, VisualElement> _panels = new Dictionary<string, VisualElement>();
        private string _active;

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
            if (escape != null && escape.Handlers.Count == 0)
                escape.InstallDefaults(CloseActive, TogglePause);
            if (!string.IsNullOrEmpty(openOnStart)) Open(openOnStart);
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
            if (router.TogglePanel != null && router.TogglePanel.WasPressedThisFrame() && _panels.Count > 0)
                Toggle(_active ?? openOnStart);
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
            if (_active != null && _panels.TryGetValue(_active, out var previous))
                previous.style.display = DisplayStyle.None;
            panel.style.display = DisplayStyle.Flex;
            _active = id;
            router?.SetUiActive(true);
            return true;
        }

        /// <summary>Reference <c>toggle</c>.</summary>
        public bool Toggle(string id) => _active == id ? !CloseActive() : Open(id);

        /// <summary>Close the open panel. Returns true if there was one (the escape chain's contract).</summary>
        public bool CloseActive()
        {
            if (_active == null) return false;
            if (_panels.TryGetValue(_active, out var panel)) panel.style.display = DisplayStyle.None;
            _active = null;
            router?.SetUiActive(false);
            return true;
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
