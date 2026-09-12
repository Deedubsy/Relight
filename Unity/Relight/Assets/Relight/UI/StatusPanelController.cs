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

        [Tooltip("Sim tile the walk button sends the engineer to. Default: east along the street from the spawn crossroads.")]
        [SerializeField] private Vector2 walkTarget = new Vector2(20.5f, 6.5f);

        private readonly StatusPanelViewModel _model = new StatusPanelViewModel();
        private Label _clock, _health, _position, _condition, _pockets, _machines, _ticks;
        private Button _walk, _sort;

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
            if (_walk != null) _walk.clicked -= WalkToTarget;
            if (_sort != null) _sort.clicked -= SortPockets;
        }

        /// <summary>Re-query the document. Public so a test (or a hot-reloaded UXML) can rebind.</summary>
        public void Bind()
        {
            var root = document == null ? null : document.rootVisualElement;
            if (root == null) return;
            _clock = root.Q<Label>("clock");
            _health = root.Q<Label>("health");
            _position = root.Q<Label>("position");
            _condition = root.Q<Label>("condition");
            _pockets = root.Q<Label>("pockets");
            _machines = root.Q<Label>("machines");
            _ticks = root.Q<Label>("ticks");

            if (_walk != null) _walk.clicked -= WalkToTarget;
            if (_sort != null) _sort.clicked -= SortPockets;
            _walk = root.Q<Button>("walk-here");
            _sort = root.Q<Button>("sort-pockets");
            if (_walk != null) _walk.clicked += WalkToTarget;
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

        /// <summary>The proof that the UI can act, not only read: one click, one command, no state written here.</summary>
        public void WalkToTarget()
        {
            if (host != null) host.Submit(new MoveCommand(walkTarget.x, walkTarget.y));
        }

        /// <summary>The B-06 pocket sort, the other end of the same path.</summary>
        public void SortPockets()
        {
            if (host != null) host.Submit(new InventorySortCommand());
        }
    }
}
