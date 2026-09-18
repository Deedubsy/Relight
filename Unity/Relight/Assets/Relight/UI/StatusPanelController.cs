using Relight.Presentation;
using Relight.Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace Relight.UI
{
    /// <summary>
    /// B-13. The thin half of the UI (TECHNICAL_ARCHITECTURE.md §8.2). It does three things and no more: find named
    /// elements in the document, copy strings out of <see cref="StatusPanelViewModel"/> into them, and turn one
    /// button click into one sim <see cref="Command"/>.
    ///
    /// It never creates an element, never sets a size, colour, margin or position, and never reads a style. That is
    /// the whole point: <c>Assets/Relight/UI/Panels/StatusPanel.uxml</c> can be reordered, restyled or rebuilt in UI
    /// Builder and this file does not change. The only contract between them is the set of element NAMES below; the
    /// panel keeps working (minus that line) if one goes missing, so a layout experiment cannot throw.
    ///
    /// Refresh is driven twice, exactly as the reference is: on <see cref="SimHost.TickBoundary"/> (hud.ts is called
    /// from the session's frame callback) and on a 150 ms throttle so a paused or idle game still repaints when
    /// something outside the sim changes (hud.ts:47).
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Relight/Status Panel Controller")]
    public sealed class StatusPanelController : MonoBehaviour
    {
        [Tooltip("The document holding StatusPanel.uxml. Found on this object if left empty.")]
        [SerializeField] private UIDocument document;

        [Tooltip("The host read through selectors and written to through Submit. Found in the scene if left empty.")]
        [SerializeField] private SimHost host;

        private readonly StatusPanelViewModel _model = new StatusPanelViewModel();
        private Label _clock, _health, _position, _condition, _pockets, _machines, _ticks;
        private Button _sort;

        /// <summary>The view-model, for tests.</summary>
        public StatusPanelViewModel Model => _model;

        private void Awake()
        {
            if (document == null) document = GetComponent<UIDocument>();
            if (host == null) host = FindAnyObjectByType<SimHost>();
        }

        private void OnEnable()
        {
            Bind();
            if (host != null) host.TickBoundary += OnTickBoundary;
        }

        private void OnDisable()
        {
            if (host != null) host.TickBoundary -= OnTickBoundary;
            if (_sort != null) _sort.clicked -= SortPockets;
        }

        /// <summary>Re-query the document. Public so a test (or a hot-reloaded UXML) can rebind.</summary>
        public void Bind()
        {
            var doc = document == null ? null : document.rootVisualElement;
            if (doc == null) return;
            // Scope every query to the panel itself: GameUI.uxml puts the HUD and this panel in one document, and
            // the HUD's status strip also has a Label named "clock" (Hud.uxml:22). An unscoped root.Q found that one
            // first, so the panel's own heading stayed at its UXML placeholder (Phase C integrated run, 2026-09-14).
            var root = doc.Q("status-panel") ?? doc;
            _clock = root.Q<Label>("clock");
            _health = root.Q<Label>("health");
            _position = root.Q<Label>("position");
            _condition = root.Q<Label>("condition");
            _pockets = root.Q<Label>("pockets");
            _machines = root.Q<Label>("machines");
            _ticks = root.Q<Label>("ticks");

            if (_sort != null) _sort.clicked -= SortPockets;
            _sort = root.Q<Button>("sort-pockets");
            if (_sort != null) _sort.clicked += SortPockets;

            Paint(true);
        }

        private void Start()
        {
            if (_clock == null) Bind();   // see UiShell.Start: document root may not exist yet in OnEnable
        }

        private void OnTickBoundary(int ticks) => Paint(false);

        private void Update() => Paint(false);

        private void Paint(bool force)
        {
            if (!_model.Refresh(host, Time.unscaledTimeAsDouble, force)) return;
            Set(_clock, _model.Clock);
            Set(_health, _model.Health);
            Set(_position, _model.Position);
            Set(_condition, _model.Condition);
            Set(_pockets, _model.Pockets);
            Set(_machines, _model.Machines);
            Set(_ticks, _model.Ticks);
        }

        private static void Set(Label l, string text)
        {
            if (l != null) l.text = text;
        }

        /// <summary>
        /// The B-06 pocket sort: the proof that the UI can ACT, not only read — one click, one command, no state
        /// written here. It replaces the old "Walk east" proof button, which the correction pass removed along
        /// with click-to-move: the engineer is driven by WASD and by nothing else, so no controller anywhere may
        /// submit a <c>MoveCommand</c>.
        /// </summary>
        public void SortPockets()
        {
            if (host != null) host.Submit(new InventorySortCommand());
        }
    }
}
