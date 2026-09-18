using System;
using System.Collections.Generic;
using System.Globalization;
using Relight.Presentation;
using Relight.Sim;
using Relight.Sim.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Relight.UI
{
    /// <summary>
    /// Correction pass W-B R3 — the build menu (the port of <c>packages/game/src/buildPanel.ts</c>), and the HUD's
    /// placement toolbar that goes with it.
    ///
    /// The owner's playtest found no way to build anything: there was no menu, and the only route to a machine was
    /// a quickbar digit for a slot nobody had filled. This controller is that route. It is thin in the
    /// TECHNICAL_ARCHITECTURE.md §8.2 sense — it instantiates <c>BuildCard.uxml</c>, copies strings from
    /// <see cref="BuildCatalogue"/> into named elements and toggles USS classes. Every number and every sentence
    /// comes from the simulation's catalogue; nothing here is hard-coded content.
    ///
    /// <b>Clicking a card takes the machine in hand.</b> It calls <c>WorldInput.HoldTool(kind)</c> (contract C3)
    /// and then closes the drawer, because a ghost you cannot see is not a placement preview. The HUD's action bar
    /// stays on screen, so the menu is one click away again.
    ///
    /// <b>The placement toolbar lives in Hud.uxml</b>, not in this panel, for exactly that reason: it must be
    /// visible while the panel is closed and the ghost is in hand. This controller owns it anyway — it is the same
    /// subject — and shows it only while <c>WorldInput.PlacementActive</c>.
    ///
    /// <b>Nothing is hidden and nothing is disabled.</b> A card the Backpack cannot pay for is greyed and says
    /// what is missing, but it still takes the ghost in hand: walking to the rubble with the ghost up is how a
    /// player learns what a machine needs. The simulation refuses the placement itself, in its own words
    /// (<see cref="Placement"/>), and the toolbar shows that refusal live.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Relight/Build Panel Controller")]
    public sealed class BuildPanelController : MonoBehaviour
    {
        /// <summary>The element name UiShell opens and B toggles (contract C1/C2).</summary>
        public const string PanelId = "build-panel";

        /// <summary>Set instead of writing <c>style.display</c> (tokens.uss owns it).</summary>
        public const string HiddenClass = "is-hidden";

        /// <summary>How many quickbar destinations "Add to action bar" offers — the sim's own bar length.</summary>
        public const int BarSlots = WeaponRules.BarSlots;

        [Tooltip("The GameUI document: it holds both this panel and the HUD's placement toolbar. Found on this object if left empty.")]
        [SerializeField] private UIDocument document;

        [Tooltip("The simulation this menu prices against and sends quickbar commands to. Found in the scene if left empty.")]
        [SerializeField] private SimHost host;

        [Tooltip("Closed when a card is clicked, so the ghost is visible. Found in the scene if left empty.")]
        [SerializeField] private UiShell shell;

        [Tooltip("BuildCard.uxml. One instance per catalogue row.")]
        [SerializeField] private VisualTreeAsset buildCard;

        [Tooltip("Optional item art. Missing keys fall back to the generated placeholder square.")]
        [SerializeField] private ItemIconLibrary icons;

        private VisualElement _root, _grid, _detail, _detailIconRow, _destinations, _toolbar, _toolbarButtons;
        private VisualElement _detailIcon;
        private Label _detailIconCode;
        private Label _title, _detailName, _detailFacts, _detailPower, _detailCost, _detailStatus, _detailUnlock;
        private Label _hint, _notice, _placementLabel, _placementProblem;
        private Button _addToBar, _rotate, _cancel;
        private readonly Button[] _tabs = new Button[4];
        private readonly List<Card> _cards = new List<Card>();

        private WorldInput _input;
        private PlacementPreviewPresenter _preview;
        private BuildCategory _tab = BuildCategory.Production;
        private string _selected = "";
        private string _notes = "";
        private bool _destinationsOpen;
        private double _lastRefresh = double.NegativeInfinity;
        private bool _wasShown;
        private bool _subscribed;

        /// <summary>One built card and the elements inside it.</summary>
        private sealed class Card
        {
            public BuildCardView View;

            /// <summary>
            /// The TemplateContainer the card was instantiated into — <c>.build-host</c>, which since U-D-55
            /// carries the card's width, gutter and `min-height`. A tab switch has to hide THIS and not only the
            /// card inside it: an empty host still claims its 144px floor, so hiding the card alone would leave
            /// the open tab's grid pocked with blank squares, one for every card the other three tabs own.
            /// </summary>
            public VisualElement Host;

            public Button Root;
            public VisualElement Icon;
            public Label IconCode, Name, Cost, Status;
        }

        /// <summary>How many cards are built (all tabs). For tests.</summary>
        public int CardCount => _cards.Count;

        /// <summary>How many cards the open tab is showing. For tests.</summary>
        public int VisibleCardCount
        {
            get
            {
                var n = 0;
                for (var i = 0; i < _cards.Count; i++) if (_cards[i].View.Category == _tab) n++;
                return n;
            }
        }

        /// <summary>The tab currently open.</summary>
        public BuildCategory Tab => _tab;

        /// <summary>The kind whose detail pane is showing, or "".</summary>
        public string Selected => _selected;

        /// <summary>The last line the menu said — the sim's words, or the menu's own. For tests.</summary>
        public string Notice => _notes;

        private void Awake()
        {
            if (document == null) document = GetComponent<UIDocument>();
            if (host == null) host = FindAnyObjectByType<SimHost>();
            if (shell == null) shell = FindAnyObjectByType<UiShell>();
            // W-A's components. Both may legitimately be absent in a UI-only test scene, so every use is guarded.
            if (_input == null) _input = FindAnyObjectByType<WorldInput>();
            if (_preview == null) _preview = FindAnyObjectByType<PlacementPreviewPresenter>();
            if (icons != null) ItemIcons.Library = icons;
        }

        private void OnEnable()
        {
            Bind();
            if (host != null) host.TickBoundary += OnTickBoundary;
            Subscribe();
        }

        private void OnDisable()
        {
            if (host != null) host.TickBoundary -= OnTickBoundary;
            if (_subscribed && _input != null) _input.HandChanged -= OnHandChanged;
            _subscribed = false;
        }

        private void Start()
        {
            if (_root == null) Bind();   // the document root may not exist yet in OnEnable (see UiShell.Start)
            Subscribe();
        }

        private void Subscribe()
        {
            if (_subscribed) return;
            if (_input == null) _input = FindAnyObjectByType<WorldInput>();
            if (_input == null) return;
            _input.HandChanged += OnHandChanged;
            _subscribed = true;
            PaintToolbar();
        }

        private void OnTickBoundary(int ticks) => Paint(false);

        private void OnHandChanged() => PaintToolbar();

        /// <summary>
        /// The first frame the drawer is shown is painted unthrottled (the same rule the workshop follows), and the
        /// toolbar is painted every frame because it carries the live validity text the player is steering by.
        /// </summary>
        private void Update()
        {
            var shown = IsShown(_root);
            var force = shown && !_wasShown;
            _wasShown = shown;
            if (shown) Paint(force);
            PaintToolbar();
        }

        private static bool IsShown(VisualElement e)
        {
            if (e == null || e.panel == null) return false;
            for (var v = e; v != null; v = v.parent)
                if (v.resolvedStyle.display == DisplayStyle.None) return false;
            return true;
        }

        // ---- binding --------------------------------------------------------------------------------------

        /// <summary>Re-query the document and rebuild the cards. Public so a hot-reloaded UXML can rebind.</summary>
        public void Bind()
        {
            var docRoot = document == null ? null : document.rootVisualElement;
            if (docRoot == null) return;

            _root = docRoot.Q<VisualElement>(PanelId);
            if (_root != null)
            {
                _title = _root.Q<Label>("build-title");
                _grid = _root.Q<VisualElement>("build-grid");
                _detail = _root.Q<VisualElement>("build-detail");
                _detailName = _root.Q<Label>("detail-name");
                _detailIconRow = _root.Q<VisualElement>("detail-icon-row");
                _detailFacts = _root.Q<Label>("detail-facts");
                _detailPower = _root.Q<Label>("detail-power");
                _detailCost = _root.Q<Label>("detail-cost");
                _detailStatus = _root.Q<Label>("detail-status");
                _detailUnlock = _root.Q<Label>("detail-unlock");
                _addToBar = _root.Q<Button>("add-to-bar");
                _destinations = _root.Q<VisualElement>("bar-destinations");
                _hint = _root.Q<Label>("build-hint");
                _notice = _root.Q<Label>("build-notice");

                _tabs[(int)BuildCategory.Production] = _root.Q<Button>("tab-production");
                _tabs[(int)BuildCategory.Logistics] = _root.Q<Button>("tab-logistics");
                _tabs[(int)BuildCategory.Power] = _root.Q<Button>("tab-power");
                _tabs[(int)BuildCategory.Defence] = _root.Q<Button>("tab-defence");
                WireTabs();

                BuildDetailIcon();
                BuildDestinations();
                if (_addToBar != null) _addToBar.clicked += ToggleDestinations;

                BuildCards();
            }

            // The toolbar is HUD furniture, so it is looked up from the document root, not from the panel.
            _toolbar = docRoot.Q<VisualElement>("placement-toolbar");
            _placementLabel = docRoot.Q<Label>("placement-label");
            _placementProblem = docRoot.Q<Label>("placement-problem");
            _toolbarButtons = docRoot.Q<VisualElement>("placement-buttons");
            _rotate = docRoot.Q<Button>("placement-rotate");
            _cancel = docRoot.Q<Button>("placement-cancel");
            if (_rotate != null) _rotate.clicked += RotateGhost;
            if (_cancel != null) _cancel.clicked += ClearHand;

            Paint(true);
            PaintToolbar();
        }

        private void WireTabs()
        {
            for (var i = 0; i < _tabs.Length; i++)
            {
                var button = _tabs[i];
                if (button == null) continue;
                var category = (BuildCategory)i;
                button.text = BuildCatalogue.Name(category);
                button.clicked += () => ShowTab(category);
            }
        }

        private void BuildDetailIcon()
        {
            if (_detailIconRow == null) return;
            _detailIconRow.Clear();
            _detailIcon = ItemIcons.Make("detail-icon", out _detailIconCode);
            _detailIconRow.Add(_detailIcon);
        }

        /// <summary>
        /// Ten destinations, one per quickbar slot, labelled the way the bar itself is (1-9 then 0). They are built
        /// once; only their text and enabled state change.
        /// </summary>
        private void BuildDestinations()
        {
            if (_destinations == null) return;
            _destinations.Clear();
            for (var i = 0; i < BarSlots; i++)
            {
                var slot = i;
                var button = new Button { name = "bar-slot-" + i, text = SlotLabel(i) };
                button.AddToClassList("ui-button");
                button.AddToClassList("ui-button-secondary");
                button.AddToClassList("bar-slot");
                button.clicked += () => AssignToBar(slot);
                _destinations.Add(button);
            }
        }

        /// <summary>Slot 9 is the "0" key — the same mapping the quickbar and the input router use.</summary>
        private static string SlotLabel(int index) =>
            (index == BarSlots - 1 ? 0 : index + 1).ToString(CultureInfo.InvariantCulture);

        // ---- cards ----------------------------------------------------------------------------------------

        private void BuildCards()
        {
            _cards.Clear();
            if (_grid == null) return;
            _grid.Clear();

            if (buildCard == null)
            {
                Say("The build menu is unavailable (BuildCard.uxml is not assigned).");
                return;
            }

            var sim = host == null ? null : host.Simulation;
            if (sim == null) return;

            var views = BuildCatalogue.Cards(sim.Context, sim.State);
            for (var i = 0; i < views.Count; i++)
            {
                var view = views[i];
                var instance = buildCard.Instantiate();
                // Instantiate() wraps the template in a TemplateContainer; the container must not eat the layout.
                // U-D-55: the container IS the card's host — it carries the `flex-basis: 50%`, the `min-height`
                // floor that makes every card one height and the gutter, exactly as WorkshopPanelController does
                // for the recipe cards. Setting flexGrow inline here instead left each card its own content height.
                instance.AddToClassList("build-host");
                _grid.Add(instance);

                var root = instance.Q<Button>("build-card");
                if (root == null) continue;
                root.name = "card-" + view.Kind;

                var card = new Card
                {
                    View = view,
                    Host = instance,
                    Root = root,
                    Icon = root.Q<VisualElement>("card-icon"),
                    IconCode = root.Q<Label>("card-icon-code"),
                    Name = root.Q<Label>("card-name"),
                    Cost = root.Q<Label>("card-cost"),
                    Status = root.Q<Label>("card-status"),
                };
                root.clicked += () => Choose(card);
                Tooltips.Attach(root, () => CardTooltip(card));
                _cards.Add(card);
            }

            if (_cards.Count > 0 && _selected.Length == 0) Select(_cards[0].View.Kind);
            ShowTab(_tab);
        }

        /// <summary>A card click selects it, takes the machine in hand and closes the drawer.</summary>
        private void Choose(Card card)
        {
            Select(card.View.Kind);
            HoldTool(card.View.Kind);
        }

        private void Select(string kind)
        {
            _selected = kind ?? "";
            _destinationsOpen = false;
            Paint(true);
        }

        /// <summary>Contract C3: the hand is W-A's <see cref="WorldInput"/>, and it is the only route into placement.</summary>
        private void HoldTool(string kind)
        {
            if (_input == null) _input = FindAnyObjectByType<WorldInput>();
            if (_input == null)
            {
                Say("Placement is unavailable in this scene.");
                return;
            }
            _input.HoldTool(kind);
            Say("");
            if (shell != null && string.Equals(shell.Active, PanelId, StringComparison.Ordinal)) shell.CloseActive();
            PaintToolbar();
        }

        // ---- the action bar -------------------------------------------------------------------------------

        private void ToggleDestinations()
        {
            _destinationsOpen = !_destinationsOpen && _selected.Length > 0;
            Show(_destinations, _destinationsOpen);
        }

        /// <summary>
        /// One command, the sim's own answer. <see cref="AssignBarCommand"/> is the only way the quickbar is
        /// written anywhere in the game (OpeningDefectsTests depends on that), so the menu adds nothing of its own.
        /// </summary>
        public bool Assigning => _destinationsOpen && shell?.Active == "build-panel";
        public bool TryAssignHudSlot(int slot)
        {
            if(!Assigning)return false;
            AssignToBar(slot);return true;
        }
        private void AssignToBar(int slot)
        {
            var sim = host == null ? null : host.Simulation;
            if (sim == null || _selected.Length == 0) return;
            var result = sim.Apply(new AssignBarCommand(slot, _selected));
            if (!string.IsNullOrEmpty(result.Problem)) Say(result.Problem);
            else if (result.Accepted) Say(Name(_selected) + " is on action bar slot " + SlotLabel(slot) + ".");
            _destinationsOpen = false;
            Show(_destinations, false);
        }

        private string Name(string kind)
        {
            for (var i = 0; i < _cards.Count; i++)
                if (string.Equals(_cards[i].View.Kind, kind, StringComparison.Ordinal)) return _cards[i].View.DisplayName;
            return kind;
        }

        // ---- painting -------------------------------------------------------------------------------------

        private void ShowTab(BuildCategory category)
        {
            _tab = category;
            for (var i = 0; i < _tabs.Length; i++) SetClass(_tabs[i], "selected", i == (int)category);
            Paint(true);
        }

        /// <summary>Repaint on the shared 150 ms throttle, or now when <paramref name="force"/>.</summary>
        public void Paint(bool force)
        {
            if (_grid == null) return;
            var now = Time.unscaledTimeAsDouble;
            if (!force && now - _lastRefresh < InventoryViewModel.RefreshSeconds) return;
            _lastRefresh = now;

            var sim = host == null ? null : host.Simulation;
            if (sim == null) return;

            // Re-price every card: the Backpack changes while the menu is open (a machine is packed, a craft ends).
            var priced = BuildCatalogue.Cards(sim.Context, sim.State);
            for (var i = 0; i < _cards.Count; i++)
            {
                var card = _cards[i];
                for (var j = 0; j < priced.Count; j++)
                    if (string.Equals(priced[j].Kind, card.View.Kind, StringComparison.Ordinal)) { card.View = priced[j]; break; }
                PaintCard(card);
            }

            PaintDetail();
        }

        private void PaintCard(Card c)
        {
            var v = c.View;
            var onTab = v.Category == _tab;
            Show(c.Host, onTab);       // U-D-55: the host owns the space, so the host is what a tab switch hides
            Show(c.Root, onTab);
            ItemIcons.Paint(c.Icon, c.IconCode, v.Kind);
            Set(c.Name, v.DisplayName);
            Set(c.Cost, v.CostText);
            Set(c.Status, v.Availability);
            SetClass(c.Status, "ui-bad", !v.Affordable);
            SetClass(c.Root, "unavailable", !v.Affordable);
            SetClass(c.Root, "selected", string.Equals(v.Kind, _selected, StringComparison.Ordinal));
        }

        private void PaintDetail()
        {
            var view = Find(_selected);
            Show(_detail, view != null);
            if (view == null) return;

            ItemIcons.Paint(_detailIcon, _detailIconCode, view.Kind);
            Set(_detailName, view.DisplayName);
            Set(_detailFacts, view.Facts);
            Set(_detailPower, view.PowerText);
            Show(_detailPower, view.PowerText.Length > 0);
            Set(_detailCost, view.CostText);
            Set(_detailStatus, view.Availability);
            SetClass(_detailStatus, "ui-bad", !view.Affordable);
            Set(_detailUnlock, view.Unlock.Length > 0 ? "Catalogue note: " + view.Unlock : "");
            Show(_detailUnlock, view.Unlock.Length > 0);
            Show(_destinations, _destinationsOpen);
            if (_addToBar != null) _addToBar.text = _destinationsOpen ? "Choose a slot" : "Add to action bar";
            if (_notice != null) _notice.text = _notes;
        }

        private BuildCardView Find(string kind)
        {
            if (string.IsNullOrEmpty(kind)) return null;
            for (var i = 0; i < _cards.Count; i++)
                if (string.Equals(_cards[i].View.Kind, kind, StringComparison.Ordinal)) return _cards[i].View;
            return null;
        }

        private void Say(string text)
        {
            _notes = text ?? "";
            if (_notice != null) _notice.text = _notes;
        }

        // ---- the placement toolbar ------------------------------------------------------------------------

        /// <summary>
        /// Shown only while something is in hand. The label names what is held and which way it faces; the second
        /// line is <see cref="PlacementPreviewPresenter.Problem"/> — the simulation's own refusal, live under the
        /// cursor — so the player is told why a spot is red before they click it.
        /// </summary>
        private void PaintToolbar()
        {
            if (_toolbar == null) return;
            var active = _input != null && _input.PlacementActive;
            Show(_toolbar, active);
            if (!active) return;

            var kind = _input.Tool ?? "";
            Set(_placementLabel, "Holding: " + Name(kind) + " · facing " + Facing(_input.GhostDir)
                + (kind == FlowBuild.Kind ? (_input.ChoosingUndergroundExit ? " · Choose exit · Esc cancels" : " · Choose entrance, then exit") : ""));

            // A refusal outranks advice: there is no point discussing a field of fire the build cannot have.
            // Below it sits the turret placement advisory (GP-W3), amber rather than red because a walled-in
            // turret is a legal build — the player is being warned, not stopped.
            var problem = _preview == null ? "" : _preview.Problem ?? "";
            var advice = _preview == null ? "" : _preview.Advice ?? "";
            var line = problem.Length > 0 ? problem : advice;
            Set(_placementProblem, line);
            Show(_placementProblem, line.Length > 0);
            if (_placementProblem != null)
                _placementProblem.EnableInClassList("placement-advice", problem.Length == 0 && line.Length > 0);

            // A rifle does not turn, and neither do the kinds Placement.Rotatable excludes.
            if (_rotate != null) _rotate.SetEnabled(Placement.Rotatable(kind));
        }

        private static string Facing(Dir d)
        {
            switch (d)
            {
                case Dir.N: return "north";
                case Dir.E: return "east";
                case Dir.S: return "south";
                default: return "west";
            }
        }

        private void RotateGhost()
        {
            if (_input == null) _input = FindAnyObjectByType<WorldInput>();
            if (_input == null) return;
            _input.RotateGhost();
            PaintToolbar();
        }

        private void ClearHand()
        {
            if (_input == null) _input = FindAnyObjectByType<WorldInput>();
            if (_input == null) return;
            _input.ClearHand();
            Tooltips.HideNow();
            PaintToolbar();
        }

        // ---- tooltips (contract C5) -----------------------------------------------------------------------

        /// <summary>
        /// The card's tooltip: purpose and supported outputs, then cost and power. Nothing is written here
        /// that the catalogue did not say — the rows are <see cref="BuildCostRow"/>s straight from the price.
        /// </summary>
        private TooltipContent CardTooltip(Card card)
        {
            var v = card.View;
            var rows = new List<(string label, string value, bool ok)>();
            for (var i = 0; i < v.Cost.Count; i++)
            {
                var line = v.Cost[i];
                rows.Add((line.DisplayName,
                    line.Available.ToString(CultureInfo.InvariantCulture) + " / " + line.Required.ToString(CultureInfo.InvariantCulture),
                    line.Ok));
            }
            if (v.PowerText.Length > 0) rows.Add((v.PowerText, "", true));

            return new TooltipContent
            {
                Title = v.DisplayName,
                Body = v.Description + (v.OutputSummary.Length > 0 ? "\n\n" + v.OutputSummary : ""),
                Rows = rows,
                Footer = v.Availability
            };
        }

        // ---- small helpers --------------------------------------------------------------------------------

        private static void Set(Label l, string text) { if (l != null) l.text = text ?? ""; }

        private static void Show(VisualElement e, bool on)
        {
            if (e == null) return;
            if (on) e.RemoveFromClassList(HiddenClass);
            else e.AddToClassList(HiddenClass);
        }

        private static void SetClass(VisualElement e, string cls, bool on)
        {
            if (e == null) return;
            if (on) e.AddToClassList(cls);
            else e.RemoveFromClassList(cls);
        }
    }
}
