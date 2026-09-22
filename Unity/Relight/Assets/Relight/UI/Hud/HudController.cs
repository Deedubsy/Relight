using Relight.Presentation;
using Relight.Sim;
using Relight.Sim.UI;
using Relight.UI.Settings;
using UnityEngine;
using UnityEngine.UIElements;

namespace Relight.UI
{
    /// <summary>
    /// C-07. The thin half of the HUD (TECHNICAL_ARCHITECTURE.md §8.2): it finds named elements in
    /// <c>Hud.uxml</c>, copies strings out of <see cref="HudViewModel"/> into them and adds or removes USS
    /// classes. It creates no element, writes no inline style and holds no game fact of its own.
    ///
    /// <b>It is not a panel.</b> The root carries no <c>panel</c> class, so <see cref="UiShell.Rebuild"/> never
    /// hides it, and it registers nothing on <see cref="EscapeChain"/> — the wave-2 W-C note: every
    /// <c>EscapeOrder</c> slot is taken and the HUD is not one of them.
    ///
    /// Refresh is driven exactly as the status panel and the goal card are: on
    /// <see cref="SimHost.TickBoundary"/> and on the reference HUD's own 150 ms throttle (hud.ts:47), on
    /// <b>unscaled</b> time so a paused game still repaints and a toast still expires.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Relight/HUD Controller")]
    public sealed class HudController : MonoBehaviour
    {
        /// <summary>Set on an element instead of writing <c>style.display</c> (ui-uitk: classes only).</summary>
        public const string HiddenClass = "is-hidden";

        /// <summary>Set on the root while the player has asked for reduced motion (§11): the pip stops blinking.</summary>
        public const string ReduceMotionClass = "motion-reduce";

        /// <summary>The lit half of the threat pip's blink. Never applied under reduced motion.</summary>
        public const string PipOnClass = "pip-on";

        /// <summary>Seconds between pip states. §4: one slow pulse, not a strobe.</summary>
        public const double PipSeconds = 0.6;

        [Tooltip("The document holding Hud.uxml. Found on this object if left empty.")]
        [SerializeField] private UIDocument document;

        [Tooltip("The host read through selectors. Found in the scene if left empty.")]
        [SerializeField] private SimHost host;

        [Tooltip("Read only to know whether a drawer is open, which hides the mining readout. Optional.")]
        [SerializeField] private UiShell shell;

        [Tooltip("Fallback place name for the power strip when the region names no Home site.")]
        [SerializeField] private string placeName = "Home";

        private HudViewModel _model = new HudViewModel();
        private VisualElement _root, _threat, _mining, _handLock, _pip, _reloadMeter;
        private Label _clock, _light, _power, _core, _engineer, _weapon, _ammo, _backpack;
        private Label _threatText, _alert, _noticeMore, _miningTitle, _miningDetail, _handLockText;
        private Button _openInventory, _openBuild;
        private GameplayDock _dock;
        private ProgressBar _coreMeter, _engineerMeter, _reload, _miningMeter;
        private readonly Label[] _notices = new Label[HudNotices.MaxRows];
        private readonly Label[] _problems = new Label[HudViewModel.MaxProblems];
        private bool _pipOn;
        private double _pipAt;
        private string _place;
        private int _session=-1;

        /// <summary>The view-model, for tests and for anything that needs the same strings.</summary>
        public HudViewModel Model => _model;

        private void Awake()
        {
            if (document == null) document = GetComponent<UIDocument>();
            if (host == null) host = FindAnyObjectByType<SimHost>();
            if (shell == null) shell = FindAnyObjectByType<UiShell>();
        }

        private void OnEnable()
        {
            Bind();
            if (host != null) host.TickBoundary += OnTickBoundary;
        }

        private void OnDisable()
        {
            if (host != null) host.TickBoundary -= OnTickBoundary;
        }

        private void Start()
        {
            if (_root == null) Bind();   // see UiShell.Start: the document root may not exist yet in OnEnable
        }

        /// <summary>Re-query the document. Public so a test (or a hot-reloaded UXML) can rebind.</summary>
        public void Bind()
        {
            var root = document == null ? null : document.rootVisualElement;
            if (root == null) return;
            _root = root.Q<VisualElement>("hud-root") ?? root;

            _clock = _root.Q<Label>("clock");
            _light = _root.Q<Label>("light-state");
            _power = _root.Q<Label>("power");
            _core = _root.Q<Label>("core");
            _coreMeter = _root.Q<ProgressBar>("core-meter");

            _threat = _root.Q<VisualElement>("threat");
            _threatText = _root.Q<Label>("threat-text");
            _pip = _root.Q<VisualElement>("threat-pip");

            _alert = _root.Q<Label>("alert");
            for (var i = 0; i < _notices.Length; i++) _notices[i] = _root.Q<Label>("notice-" + i);
            _noticeMore = _root.Q<Label>("notice-more");
            for (var i = 0; i < _problems.Length; i++) _problems[i] = _root.Q<Label>("problem-" + i);

            _mining = _root.Q<VisualElement>("mining");
            _miningTitle = _root.Q<Label>("mining-title");
            _miningMeter = _root.Q<ProgressBar>("mining-meter");
            _miningDetail = _root.Q<Label>("mining-detail");

            _engineer = _root.Q<Label>("engineer");
            _engineerMeter = _root.Q<ProgressBar>("engineer-meter");
            _weapon = _root.Q<Label>("weapon");
            _reload = _root.Q<ProgressBar>("reload-meter");
            _reloadMeter = _reload;
            _ammo = _root.Q<Label>("ammo");
            _backpack = _root.Q<Label>("backpack");

            _handLock = _root.Q<VisualElement>("hand-lock");
            _handLockText = _root.Q<Label>("hand-lock-text");

            WireActionBar();
            _dock = new GameplayDock(document.rootVisualElement, host, shell, GetComponent<InventoryPanelController>());
            WireTooltips();

            Paint(true);
        }

        /// <summary>
        /// Correction pass W-B R1. The owner could not reach the Backpack because nothing on screen named it, so
        /// the HUD now carries the two doors into the game and the HUD — which is not a panel — keeps them visible
        /// while a drawer is open.
        ///
        /// The labels are hard-coded rather than read from a binding display string: <c>BindingMap</c> (the only
        /// table that knows the Unity action a row drives) exposes <c>Entry.Label</c> ("Backpack"), the action id
        /// and the binding INDEX, but no key text, and <c>Bindings.Shortcut("pockets")</c> answers with the
        /// reference's browser key ("I"), not the Unity Tab binding. Reporting a key the game does not use would
        /// be worse than a constant. If a display-string API appears, this is the one place to change.
        /// </summary>
        private void WireActionBar()
        {
            var inventory = _root.Q<Button>("open-inventory");
            if (inventory != null && !ReferenceEquals(inventory, _openInventory))
            {
                _openInventory = inventory;
                inventory.clicked += () => shell?.ToggleBackpack();
                inventory.text = "Inventory [" + BindingMap.KeyTextFor("pockets", BindingMap.StoredOverrides()) + "]";
            }

            var build = _root.Q<Button>("open-build");
            if (build != null && !ReferenceEquals(build, _openBuild))
            {
                _openBuild = build;
                build.clicked += () => shell?.Toggle("build-panel");
                build.text = "Build [" + BindingMap.KeyTextFor("build", BindingMap.StoredOverrides()) + "]";
            }
        }

        /// <summary>
        /// Correction pass W-B R4 (contract C5). The three strip blocks explain themselves on hover: what the grid
        /// is doing, what the core's health means, and what the clock is counting. Every figure is read from the
        /// simulation at the moment the tooltip opens — the providers are closures, not captured strings.
        /// </summary>
        private void WireTooltips()
        {
            Tooltips.Attach(_root.Q<VisualElement>("clock-block"), ClockTooltip);
            Tooltips.Attach(_root.Q<VisualElement>("power-block"), PowerTooltip);
            Tooltips.Attach(_root.Q<VisualElement>("core-block"), CoreTooltip);
        }

        private TooltipContent ClockTooltip() => new TooltipContent
        {
            Title = _model.Clock,
            Body = "Time played. Below it, whether you are standing in light or in the dark. Lamps only help where there is power."
        };

        /// <summary>Generation, demand and what is actually delivered — <see cref="PowerSummary"/>, unrounded by me.</summary>
        private TooltipContent PowerTooltip()
        {
            var n = _model.Power.Summary;
            var rows = new System.Collections.Generic.List<(string label, string value, bool ok)>
            {
                ("Generation", PackLayout.Num(n.SupplyKw) + " kW", n.SupplyKw > 0),
                ("Demand", PackLayout.Num(n.DemandKw) + " kW", n.DemandKw <= n.SupplyKw),
                ("Delivered", PackLayout.Num(n.LoadKw) + " kW", n.LoadKw >= n.DemandKw),
                ("Generators", n.Generators + " of " + n.RatedGenerators + " fuelled", n.Generators > 0),
                ("Circuits", n.Circuits.ToString(), n.Circuits > 0)
            };
            if (_model.FuelText.Length > 0) rows.Add(("Fuel left", _model.FuelText, !_model.FuelLow));   // REL-10

            return new TooltipContent
            {
                Title = "Power",
                Body = _model.PowerText,
                Rows = rows,
                Footer = _model.PowerShort ? "Demand is above generation, so every machine on the circuit runs slowly. Add a Generator or remove load." : ""
            };
        }

        /// <summary>The core's own HP, and the one thing in the game that takes it down.</summary>
        private TooltipContent CoreTooltip()
        {
            var sim = host == null ? null : host.Simulation;
            var rows = new System.Collections.Generic.List<(string label, string value, bool ok)>();
            if (sim != null)
            {
                var card = HomeQueries.RepairCard(sim.State, sim.Context.Data);
                rows.Add(("Health", PackLayout.Num(card.Hp) + " / " + PackLayout.Num(card.Max), card.Hp >= card.Max));
                rows.Add(("Repair price", card.Steel + " steel + " + card.Copper + " copper", card.CanAfford));
                rows.Add(("Repair time", PackLayout.Num(card.Seconds) + " s", true));
            }
            return new TooltipContent
            {
                Title = "Home core",
                Body = _model.Core.Length > 0 ? _model.Core
                     : "The core is what the raiders come for. Raiders that reach it damage it; at zero it stops.",
                Rows = rows,
                Footer = "Repair it at the Home workshop (E)."
            };
        }

        /// <summary>
        /// C-13. The ONLY place this frame's events are read: <see cref="SimHost.LastFrameEvents"/> is refilled by
        /// the host's own Update and is valid only until the next one, and <see cref="SimHost.TickBoundary"/> is
        /// raised immediately after it is drained — so the list is taken here and never cached. Intake runs before
        /// <see cref="Paint"/> so a refusal raised this frame is on screen this frame.
        /// </summary>
        private void OnTickBoundary(int ticks)
        {
            if (host != null) _model.Intake(host.LastFrameEvents, Time.unscaledTimeAsDouble);
            Paint(false);
        }

        private void Update() => Paint(false);

        // --------------------------------------------------------------------------------------------------

        private void Paint(bool force)
        {
            if (_root == null) return;
            if(host!=null && _session!=host.Session){_session=host.Session;_model=new HudViewModel();Tooltips.Controller?.HideNow();}
            var now = Time.unscaledTimeAsDouble;
            var sim = host == null ? null : host.Simulation;
            var ctx = sim == null ? null : sim.Context;
            var st = sim == null ? null : sim.State;

            var reduce = Preferences.Motion == Preferences.MotionReduce;
            Toggle(_root, ReduceMotionClass, reduce);

            _model.Power.PlaceName = Place(ctx);
            var paused = host != null && host.Paused;
            var menuOpen = shell != null && !string.IsNullOrEmpty(shell.Active);

            PulsePip(now, reduce);

            _dock?.Paint();
            if (!_model.Refresh(ctx, st, now, paused, menuOpen, force)) return;

            Set(_clock, _model.Clock);
            Set(_light, _model.LightText);
            Toggle(_light, "is-danger", _model.InDark);
            Show(_light, _model.LightText.Length > 0);
            // GP-W6: the text, the meter and the two colours all come from PowerAlertSource through the model.
            // This block used to build a second sentence of its own over the top of the model's.
            Set(_power, _model.PowerText);
            var powerMeter = _root.Q<ProgressBar>("power-meter");
            if (powerMeter != null) powerMeter.value = (float)_model.PowerFraction;
            var powerBlock = _root.Q("power-block");
            powerBlock?.EnableInClassList("power-off", _model.PowerOff);
            powerBlock?.EnableInClassList("power-short", _model.PowerShort);


            Set(_core, _model.Core);
            Toggle(_core, "is-danger", _model.CoreDisabled);
            Value(_coreMeter, _model.CoreFraction);
            Show(_core, _model.Core.Length > 0);
            Show(_coreMeter, _model.Core.Length > 0);

            Set(_threatText, _model.Threat);
            Toggle(_threatText, "is-danger", _model.ThreatUrgent);
            Show(_threat, _model.Threat.Length > 0);
            Show(_pip, _model.ThreatUrgent);

            Set(_alert, _model.Alert);
            Show(_alert, _model.Alert.Length > 0);
            PaintNotices();
            PaintProblems();

            Show(_mining, _model.MiningVisible);
            Set(_miningTitle, _model.MiningTitle);
            Value(_miningMeter, _model.MiningFraction);
            Set(_miningDetail, _model.MiningDetail);

            Set(_engineer, _model.Engineer);
            Value(_engineerMeter, _model.EngineerFraction);
            Set(_weapon, _model.Weapon);
            Value(_reload, _model.ReloadFraction);
            Show(_reloadMeter, _model.ReloadFraction > 0);
            Set(_ammo, _model.Ammo);
            Set(_backpack, _model.Backpack);

            Set(_handLockText, _model.HandLock);
            Show(_handLock, _model.HandLock.Length > 0);
        }

        /// <summary>
        /// The keyed inbox, three authored rows deep. A row that is not in <see cref="HudNotices.Rows"/> is
        /// hidden rather than destroyed, so nothing here creates or removes an element. Under the rows, the count of
        /// live notices that did not fit (REL-86).
        /// </summary>
        private void PaintNotices()
        {
            var rows = _model.Notices.Rows;
            for (var i = 0; i < _notices.Length; i++)
            {
                var l = _notices[i];
                if (l == null) continue;
                if (i >= rows.Count) { Show(l, false); continue; }
                var n = rows[i];
                l.text = n.Repeats > 1 ? n.Text + " × " + n.Repeats : n.Text;
                Toggle(l, "notice-warning", n.Kind == HudNoticeKind.Warning);
                Toggle(l, "notice-danger", n.Kind == HudNoticeKind.Danger);
                Show(l, true);
            }
            var more = _model.Notices.MoreText;
            Set(_noticeMore, more);
            Show(_noticeMore, more.Length > 0);
        }

        private void PaintProblems()
        {
            var rows = _model.Problems;
            for (var i = 0; i < _problems.Length; i++)
            {
                var l = _problems[i];
                if (l == null) continue;
                if (i >= rows.Count) { Show(l, false); continue; }
                l.text = rows[i].Text;
                Show(l, true);
            }
        }

        /// <summary>
        /// §11: under reduced motion the pip does not blink. The USS resting state is then the LIT one, so the
        /// warning is never quieter for a player who asked for less movement — it simply stops moving.
        /// </summary>
        private void PulsePip(double now, bool reduce)
        {
            if (_pip == null) return;
            if (reduce)
            {
                if (_pipOn) { _pip.RemoveFromClassList(PipOnClass); _pipOn = false; }
                return;
            }
            if (now - _pipAt < PipSeconds) return;
            _pipAt = now;
            _pipOn = !_pipOn;
            Toggle(_pip, PipOnClass, _pipOn);
        }

        /// <summary>
        /// The place name the power strip and the alert print. Derived from the region's own Home site, so a new
        /// region renames both without a code change; the inspector field is only the fallback.
        /// </summary>
        private string Place(SimContext ctx)
        {
            var name = ctx?.Sites?.Core?.Name;
            if (!string.IsNullOrEmpty(name)) _place = name;
            return string.IsNullOrEmpty(_place) ? placeName : _place;
        }

        private static void Set(Label l, string text)
        {
            if (l != null) l.text = text;
        }

        private static void Value(ProgressBar b, double v)
        {
            if (b != null) b.value = (float)(v < 0 ? 0 : v > 1 ? 1 : v);
        }

        private static void Show(VisualElement e, bool visible)
        {
            if (e == null) return;
            if (visible) e.RemoveFromClassList(HiddenClass);
            else e.AddToClassList(HiddenClass);
        }

        private static void Toggle(VisualElement e, string cls, bool on)
        {
            if (e == null) return;
            if (on) e.AddToClassList(cls);
            else e.RemoveFromClassList(cls);
        }
    }
}
