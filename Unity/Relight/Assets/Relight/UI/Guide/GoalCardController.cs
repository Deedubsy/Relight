using Relight.Presentation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Relight.UI
{
    /// <summary>
    /// C-09. The thin half of the goal card (TECHNICAL_ARCHITECTURE.md §8.2). It finds named elements in the
    /// document, copies strings out of <see cref="GoalCardViewModel"/> into them, and turns the two camera buttons
    /// into camera moves. It never creates an element, never sets a size, colour, margin or position, and never
    /// writes an inline style: visibility is the <c>is-hidden</c> USS class, nothing else.
    ///
    /// Refresh is driven exactly as the status panel is: on <see cref="SimHost.TickBoundary"/> and on the
    /// reference HUD's own 150 ms throttle (hud.ts:47), so a paused game still repaints.
    ///
    /// <b>Camera.</b> The reference's two buttons are <c>world.viewLocation(x,y)</c> — "centre on this tile and
    /// HOLD there" (worldScene.ts:460) — and <c>world.returnToEngineer()</c> — "stop holding and snap back"
    /// (worldScene.ts:461); moving cancels the hold (worldScene.ts:923). The port expresses the same three rules
    /// through <see cref="CameraRig"/> without touching it: holding is <c>rig.enabled = false</c> plus one
    /// transform write, releasing is <see cref="CameraRig.Unsnap"/> plus <c>rig.enabled = true</c>. There is no
    /// walk command here and no map click that walks (UI_AND_ONBOARDING.md §7.8).
    ///
    /// <b>The camera still never reads a UI inset</b> (D-UI-10): this moves the camera only when the player asks
    /// it to, and nothing about the card's size or position enters the calculation.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Relight/Goal Card Controller")]
    public sealed class GoalCardController : MonoBehaviour
    {
        /// <summary>Set on an element instead of writing <c>style.display</c> (ui-uitk: classes only).</summary>
        public const string HiddenClass = "is-hidden";

        /// <summary>Set on a material row the Backpack already satisfies (reference hud.ts:54 class "ready").</summary>
        public const string ReadyClass = "goal-material-ready";

        [Tooltip("The document holding GoalCard.uxml. Found on this object if left empty.")]
        [SerializeField] private UIDocument document;

        [Tooltip("The host read through selectors. Found in the scene if left empty.")]
        [SerializeField] private SimHost host;

        [Tooltip("The world camera the two buttons move. Found in the scene if left empty.")]
        [SerializeField] private CameraRig rig;

        [Tooltip("Read only to cancel a held view when the player moves. Found in the scene if left empty.")]
        [SerializeField] private InputRouter router;

        private readonly GoalCardViewModel _model = new GoalCardViewModel();
        private readonly Label[] _materialRows = new Label[GoalCardViewModel.MaxMaterials];
        private Label _place, _title, _text, _detail;
        private VisualElement _materials;
        private Foldout _why;
        private Button _locate, _back;
        private InputAction _move;
        private bool _holding;
        private int _session=-1;

        /// <summary>The view-model, for tests.</summary>
        public GoalCardViewModel Model => _model;

        /// <summary>True while the camera is parked on an objective instead of following the engineer.</summary>
        public bool Holding => _holding;

        private void Awake()
        {
            if (document == null) document = GetComponent<UIDocument>();
            if (host == null) host = FindAnyObjectByType<SimHost>();
            if (rig == null) rig = FindAnyObjectByType<CameraRig>();
            if (router == null) router = FindAnyObjectByType<InputRouter>();
            // The World map's Move action, used only to cancel a held view (reference worldScene.ts:923).
            var actions = router == null ? null : router.Actions;
            if (actions != null) _move = actions.FindAction("World/Move", false);
        }

        private void OnEnable()
        {
            Bind();
            if (host != null) host.TickBoundary += OnTickBoundary;
        }

        private void OnDisable()
        {
            if (host != null) host.TickBoundary -= OnTickBoundary;
            if (_locate != null) _locate.clicked -= ShowLocation;
            if (_back != null) _back.clicked -= ReturnToEngineer;
            ReturnToEngineer();
        }

        /// <summary>Re-query the document. Public so a test (or a hot-reloaded UXML) can rebind.</summary>
        public void Bind()
        {
            var root = document == null ? null : document.rootVisualElement;
            if (root == null) return;
            _place = root.Q<Label>("goal-place");
            _title = root.Q<Label>("goal-title");
            _text = root.Q<Label>("goal-text");
            _detail = root.Q<Label>("goal-detail");
            _materials = root.Q<VisualElement>("goal-materials");
            _why = root.Q<Foldout>("goal-why");
            for (var i = 0; i < _materialRows.Length; i++)
                _materialRows[i] = root.Q<Label>("material-" + i.ToString());

            if (_locate != null) _locate.clicked -= ShowLocation;
            if (_back != null) _back.clicked -= ReturnToEngineer;
            _locate = root.Q<Button>("show-location");
            _back = root.Q<Button>("return-to-engineer");
            if (_locate != null) _locate.clicked += ShowLocation;
            if (_back != null) _back.clicked += ReturnToEngineer;

            Paint(true);
        }

        private void Start()
        {
            if (_title == null) Bind();   // see UiShell.Start: the document root may not exist yet in OnEnable
        }

        private void OnTickBoundary(int ticks) => Paint(false);

        private void Update()
        {
            if(host!=null && _session!=host.Session){_session=host.Session;ReturnToEngineer();if(_why!=null)_why.value=false;}
            // Any movement releases a held view, the way the reference's move keys do (worldScene.ts:923).
            if (_holding && _move != null && _move.ReadValue<Vector2>().sqrMagnitude > 0.01f) ReturnToEngineer();
            Paint(false);
        }

        private void Paint(bool force)
        {
            if (!_model.Refresh(host, Time.unscaledTimeAsDouble, force)) return;

            Set(_place, _model.Place);
            Show(_place, !string.IsNullOrEmpty(_model.Place));
            Set(_title, _model.Title);
            Set(_text, _model.Text);
            Set(_detail, _model.Detail);
            Show(_why, !string.IsNullOrEmpty(_model.Detail));

            var rows = _model.Materials;
            for (var i = 0; i < _materialRows.Length; i++)
            {
                var row = _materialRows[i];
                if (row == null) continue;
                var used = i < rows.Count;
                Show(row.parent, used);
                var meter=row.parent.Q<ProgressBar>("material-progress-"+i);
                if(used && meter!=null)meter.value=_model.MaterialFractions[i];
                if (!used) continue;
                row.text = rows[i];
                row.EnableInClassList(ReadyClass, _model.MaterialReady[i]);
            }
            Show(_materials, rows.Count > 0);

            // Reference hud.ts:52 — the locate button is hidden when the step has no place to show, and the
            // return button appears only while the camera is actually parked ("back.hidden = !viewingThreat").
            Show(_locate, _model.HasLocation);
            Show(_back, _holding);
        }

        /// <summary>
        /// Reference worldScene.ts:460 <c>viewLocation</c>: centre the camera on the objective's tile and hold it
        /// there. Nothing is commanded and nobody walks.
        /// </summary>
        public void ShowLocation()
        {
            if (!_model.HasLocation || rig == null) return;
            rig.enabled = false;
            var p = rig.transform.position;
            rig.transform.position = new Vector3(Horizontal(_model.LocationX), Vertical(_model.LocationY), p.z);
            _holding = true;
            Show(_back, true);
        }

        /// <summary>Reference worldScene.ts:461 <c>returnToEngineer</c>: stop holding and snap back to the engineer.</summary>
        public void ReturnToEngineer()
        {
            if (!_holding) return;
            _holding = false;
            if (rig != null)
            {
                rig.Unsnap();
                rig.enabled = true;
            }
            Show(_back, false);
        }

        // Relight.World.WorldSpace is the authority for this mapping (UnitsPerTile = 1, Y-down tiles to Y-up
        // world). Relight.UI does not reference Relight.World, so the two lines are repeated here rather than a
        // new assembly reference added to an asmdef this task does not own; the wave-3 W-A report carries the
        // one-line patch that would let this call WorldSpace.World instead.
        private static float Horizontal(double tileX) => (float)tileX;
        private static float Vertical(double tileY) => -(float)tileY;

        private static void Set(Label l, string text)
        {
            if (l != null) l.text = text;
        }

        private static void Show(VisualElement e, bool visible)
        {
            if (e != null) e.EnableInClassList(HiddenClass, !visible);
        }
    }
}
