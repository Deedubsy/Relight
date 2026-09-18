using System;
using System.Collections.Generic;
using Relight.Presentation;
using Relight.Sim;
using Relight.Sim.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Relight.UI
{
    /// <summary>
    /// C-06. The Backpack / storage drawer (reference packages/game/src/inventoryPanel.ts
    /// <c>createInventoryPanel</c>; UI_AND_ONBOARDING.md §5).
    ///
    /// <b>The one rule that governs this whole file: the UI never mutates a count.</b> Every change — a drag, a
    /// shift-click, Split, Move, Sort, Equip, Reload — becomes exactly one <see cref="Command"/>, and whatever the
    /// simulation answers is shown word for word on the notice line. There is no place in this file where a number
    /// on screen is written from anything but a query.
    ///
    /// Commands go through <see cref="Simulation.Apply"/>, never <see cref="SimHost.Submit"/>: <c>Apply</c> is the
    /// port of the reference's <c>dispatch</c> — it flushes what is pending, runs even while paused, and RETURNS
    /// the <see cref="CommandResult"/>. <c>Submit</c> returns void, so a refusal would vanish.
    ///
    /// The asymmetry of §5.2 is respected throughout: the Backpack is forty real slots and all forty are rendered,
    /// empty ones included, so every one of them can be dropped on (defect U-1). Storage is a pooled quantity that
    /// is only PRESENTED in stack-sized chunks, so a "full" chunk is never a refusal — a drop onto any compatible
    /// chunk merges into the pool, and a partial transfer reports what actually moved
    /// (<see cref="TransferText.Moved"/>), never what was asked for.
    ///
    /// Layout is authored in InventoryPanel.uxml. This file looks elements up by name, creates the slot buttons
    /// (a forty-slot grid is data, not layout — the reference generates them too) with USS classes only, and
    /// writes no style property anywhere except the drag ghost's position and the menu's offset.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Relight/Inventory Panel Controller")]
    public sealed class InventoryPanelController : MonoBehaviour
    {
        /// <summary>Action-bar slots, §5.5: ten, stable, keys 1..0.</summary>
        public const int QuickSlots = 10;

        [Tooltip("The document holding InventoryPanel.uxml. Found on this object if left empty.")]
        [SerializeField] private UIDocument document;

        [Tooltip("The simulation this drawer reads and sends commands to. Found in the scene if left empty.")]
        [SerializeField] private SimHost host;

        [Tooltip("Owns Escape. The drag-cancel (100) and slot-menu (200) links are registered here.")]
        [SerializeField] private EscapeChain escape;

        [Tooltip("Moves input between the World and UI maps; read for the Reload and Slot actions.")]
        [SerializeField] private InputRouter router;

        // ---- element handles ------------------------------------------------------------------------------
        private VisualElement _root, _packGrid, _storeGrid, _quickbar, _pair, _storeSide, _storeInventory, _ghost, _detail;
        private Label _drawerHeading, _packHeading, _packCapacity, _storeHeading, _storeCapacity;
        private Label _detailTitle, _detailDescription, _notice, _hint, _equipmentLabel;
        private TextField _quantity;
        private VisualElement _machineControls, _machineRepair;
        private Label _machineRepairText;
        private Button _machineRepairButton;
        private Label _machineStatus, _recipeDescription;
        private DropdownField _recipePicker, _filterPicker, _priorityPicker;
        private Button _applyRecipe, _closeDrawer;
        private ProgressBar _machineProgress;
        private readonly List<Recipe> _recipes = new List<Recipe>();
        private int _configuredMachine = -2;
        private readonly List<string> _recipeKeys = new List<string>();
        private Button _transfer, _split, _move, _sort, _packSort, _reload, _swap;
        private readonly Button[] _equipment = new Button[2];

        private readonly InventoryViewModel _model = new InventoryViewModel();
        private readonly List<Button> _packButtons = new List<Button>(40);
        private readonly List<Button> _storeButtons = new List<Button>(32);
        private readonly List<Button> _trayButtons = new List<Button>(HandCraft.OutputStacks);
        private readonly List<Button> _quickButtons = new List<Button>(QuickSlots);
        private readonly Dictionary<VisualElement, SlotRef> _refs = new Dictionary<VisualElement, SlotRef>();

        private SlotMenu _menu;
        private CancelDragLink _dragLink;
        private SlotRef _selected;
        private bool _moveMode;
        private string _notes = "";
        private bool _dirty;
        private int _inventorySession = -1;

        /// <summary>
        /// Whether the Home workshop is what the player opened. U-D-55: Tab and "E at the depot" BOTH pair with
        /// machine -1, so the id cannot tell them apart — the caller says which it was, and only the interaction
        /// says true. Before this flag the workshop appeared under every Tab, which is not what -1 means.
        /// </summary>
        private bool _workshop;

        /// <summary>
        /// Which grid a slot belongs to. Equipment and Quick are drop targets, never sources of a count.
        ///
        /// Public since the correction pass so the PlayMode drag tests can name a slot: synthesising a real
        /// pointer drag through the panel is not something this machine can verify, so the tests drive the drag's
        /// two ends (<see cref="DragFrom"/>, <see cref="DragTo"/>) instead. The manipulator calls exactly the same
        /// two methods, so a test that passes here is testing the transfer the drag performs.
        /// </summary>
        public enum SlotKind { Pack, Store, Equipment, Quick, Tray }

        private sealed class SlotRef
        {
            public SlotKind Kind;
            public int Index;
            public Button Button;
        }

        /// <summary>The view-model, for tests.</summary>
        public InventoryViewModel Model => _model;

        /// <summary>The last thing the drawer said. The sim's words when the sim spoke. For tests.</summary>
        public string Notice => _notes;

        /// <summary>True while "Move to slot" is waiting for the destination slot to be clicked.</summary>
        public bool MoveMode => _moveMode;

        /// <summary>
        /// Test seam: the payload a drag started on this slot would carry — exactly what the manipulator's
        /// <c>start</c> callback returns. Null when the slot holds nothing that can be dragged.
        /// </summary>
        public StackSnapshot DragFrom(SlotKind kind, int index) => Snapshot(kind, index);

        /// <summary>
        /// Test seam: release <paramref name="payload"/> over a slot — the manipulator's <c>drop</c> callback with
        /// the target already resolved. The transfer, the refusal and the notice are the live ones.
        /// </summary>
        public void DragTo(SlotKind kind, int index, StackSnapshot payload)
            => Drop(new SlotRef { Kind = kind, Index = index }, payload);

        /// <summary>
        /// Test seam: release <paramref name="payload"/> over a slot with Shift held — the split gesture
        /// (GP-UX-9), with the destination already resolved.
        /// </summary>
        public void DragTo(SlotKind kind, int index, StackSnapshot payload, bool split)
            => Drop(new SlotRef { Kind = kind, Index = index }, payload, new DragPoint(Vector2.zero, split));

        /// <summary>
        /// Test seam: release <paramref name="payload"/> at a raw panel POSITION, with nothing picked under it —
        /// the near-miss path (GP-UX-9), so a release in the gutter between two slots is resolved exactly as the
        /// live gesture resolves it.
        /// </summary>
        public void DragToPoint(Vector2 position, StackSnapshot payload, bool split = false)
            => Drop(NearestSlot(position), payload, new DragPoint(position, split));

        /// <summary>
        /// Pair the drawer with a machine's inventory, or -1 for the Backpack alone (§5.1). This is what the world
        /// interaction calls when the engineer opens Home storage; the panel itself never decides what is open.
        /// "Alone" now means alone: the Home workshop needs <see cref="OpenWorkshop"/> (U-D-55).
        /// </summary>
        public void OpenStore(int machineId) => OpenStore(machineId, false);

        /// <summary>
        /// Open the Home workshop in the producer column — what interacting with the depot does, and what the
        /// drawer's own "Home workshop" button does. U-D-55: the workshop is a PLACE the engineer stands at, so
        /// only an interaction with it may put it on screen; Tab is the Backpack and nothing else.
        /// </summary>
        public void OpenWorkshop() => OpenStore(-1, true);

        /// <summary>
        /// The pairing above, told whether the Home workshop is what was opened. See <see cref="_workshop"/> for
        /// why the machine id cannot carry that by itself.
        /// </summary>
        public void OpenStore(int machineId, bool workshop)
        {
            _workshop = workshop;
            UiDrag.Cancel();
            _inventorySession = host == null ? -1 : host.Session;
            _model.Pair(machineId);
            _configuredMachine = -2;
            // U-D-51: the producer now OWNS the right column, so a newly opened one starts at the top of it.
            // U-D-52: its slots are FIXED at that top and cannot scroll away, so what needs resetting is
            // whichever scroller the producer brought below them — a machine’s controls, the workshop’s cards.
            var machineScroll = _root?.Q<ScrollView>("machine-scroll");
            if (machineScroll != null) machineScroll.scrollOffset = Vector2.zero;
            var cardScroll = _root?.Q<ScrollView>("workshop-cards");
            if (cardScroll != null) cardScroll.scrollOffset = Vector2.zero;
            _selected = null;
            _moveMode = false;
            Say("");
            Paint(true);
        }

        private void OpenFromWorld(int machineId)
        {
            // WorldInput sends -1 only from the depot branch of Interact (it checks HandCraft.NearDepot first),
            // so a -1 arriving HERE really is the engineer standing at the Home workshop. U-D-55.
            OpenStore(machineId, machineId < 0);
            FindAnyObjectByType<UiShell>()?.Open("inventory-panel");
        }

        private void WorldNotice(string message)
        {
            Say(message);
            FindAnyObjectByType<HudController>()?.Model.Notices.Post("interaction", message, HudNoticeKind.Info, Time.unscaledTimeAsDouble);
        }

        // ---- lifecycle ------------------------------------------------------------------------------------

        private void Awake()
        {
            if (document == null) document = GetComponent<UIDocument>();
            if (host == null) host = FindAnyObjectByType<SimHost>();
            if (escape == null) escape = FindAnyObjectByType<EscapeChain>();
            if (router == null) router = FindAnyObjectByType<InputRouter>();
        }

        private void OnEnable()
        {
            Bind();
            WorldInput.OpenMachine = OpenFromWorld;
            WorldInput.InteractionNotice = WorldNotice;
            if (host != null) host.TickBoundary += OnTickBoundary;
            // A panel that deferred its repaint during a drag catches up the moment the gesture ends
            // (reference uiDrag.ts onUiDragEnd).
            UiDrag.OnDragEnd(OnDragEnded);
        }

        private void OnDisable()
        {
            if (WorldInput.OpenMachine?.Target == this) WorldInput.OpenMachine = null;
            if (WorldInput.InteractionNotice?.Target == this) WorldInput.InteractionNotice = null;
            if (host != null) host.TickBoundary -= OnTickBoundary;
            UiDrag.RemoveDragEnd(OnDragEnded);
            UiDrag.Cancel();                     // a drag must never outlive the panel it started in
            _menu?.Close();
            if (escape != null)
            {
                escape.Unregister(_dragLink);
                escape.Unregister(_menu);
            }
        }

        private void OnApplicationFocus(bool focus)
        {
            // The reference cancels on window blur and on visibilitychange; losing focus is the same event here.
            if (!focus) UiDrag.Cancel();
        }

        private void Start()
        {
            if (_packGrid == null) Bind();      // UIDocument may build its root after this component enabled
        }

        private void OnTickBoundary(int ticks) => _dirty = true;

        private void Update()
        {
            Paint(false);
        }

        private void OnDragEnded()
        {
            _dirty = true;
            Paint(true);
        }

        // ---- binding --------------------------------------------------------------------------------------

        /// <summary>Re-query the document and rebuild the grids. Public so a hot-reloaded UXML can rebind.</summary>
        public void Bind()
        {
            var docRoot = document == null ? null : document.rootVisualElement;
            if (docRoot == null) return;
            _root = docRoot.Q<VisualElement>("inventory-panel");
            if (_root == null) return;

            _drawerHeading = _root.Q<Label>("drawer-heading");
            _pair = _root.Q<VisualElement>("inventory-pair");
            _storeSide = _root.Q<VisualElement>("side-store");
            // U-D-51. The right column holds a machine's own container OR the Home workshop's, never both empty:
            // this is the machine half, hidden for the workshop and for a machine that has no storage at all.
            _storeInventory = _root.Q<VisualElement>("store-inventory");
            _packGrid = _root.Q<VisualElement>("pack-grid");
            _storeGrid = _root.Q<VisualElement>("store-grid");
            // GP-UX-2. The Home workshop section is instanced INSIDE this drawer (InventoryPanel.uxml line 24), so
            // its output tray is reachable from here and is built by the same slot code as the Backpack. Null on a
            // document that has no workshop section, and every use below tolerates that.
            _trayGrid = _root.Q<VisualElement>("workshop-output-grid");
            _quickbar = _root.Q<VisualElement>("quickbar");
            _packHeading = _root.Q<Label>("pack-heading");
            _packCapacity = _root.Q<Label>("pack-capacity");
            _storeHeading = _root.Q<Label>("store-heading");
            _storeCapacity = _root.Q<Label>("store-capacity");
            _detail = _root.Q<VisualElement>("item-detail");
            _detailTitle = _root.Q<Label>("detail-title");
            _detailDescription = _root.Q<Label>("detail-description");
            _notice = _root.Q<Label>("notice");
            _hint = _root.Q<Label>("hint");
            BindMachineControls();
            _quantity = _root.Q<TextField>("quantity");
            _ghost = docRoot.Q<VisualElement>("drag-preview");
            // The full-screen template host retains the inventory stylesheet but does not clip to the drawer.
            if(_ghost!=null && _ghost.parent==_root)_root.parent.Add(_ghost);
            _equipmentLabel = _root.Q<Label>("equipment-label");
            _equipment[0] = _root.Q<Button>("equipment-slot-0");
            _equipment[1] = _root.Q<Button>("equipment-slot-1");
            _reload = _root.Q<Button>("reload");
            _swap = _root.Q<Button>("swap");
            _transfer = _root.Q<Button>("transfer");
            _split = _root.Q<Button>("split");
            _move = _root.Q<Button>("move");
            _sort = _root.Q<Button>("sort");
            // GP-UX-9: the same command, on the Backpack column's own header, where a player looks for it.
            _packSort = _root.Q<Button>("pack-sort");

            if (_hint != null) _hint.text = TransferText.Hint;
            if (_packHeading != null) _packHeading.text = "Backpack";
            if (_detailDescription != null) _detailDescription.text = TransferText.CarriedNote;

            WireDetailButtons();
            WireStrip();
            BuildQuickbar();
            // U-D-51: one button, one job — put the Home workshop back in the right column. It is shown only
            // while a machine has taken that column, because otherwise the workshop is already on screen.
            var workshopButton=_root.Q<Button>("jump-workshop");
            if(workshopButton!=null) workshopButton.clicked+=()=>{ Tooltips.HideNow(); OpenWorkshop(); };

            _menu = new SlotMenu(_root.Q<VisualElement>("slot-menu"),
                MenuTransfer, MenuSplit, MenuMove, MenuSort);
            _dragLink = new CancelDragLink();
            if (escape != null)
            {
                // No edit to EscapeChain is needed: Register inserts AFTER every handler of equal order, so these
                // live links land immediately behind the inert placeholders that hold slots 100 and 200, and the
                // reference's ladder (uiShell.ts:114-120) is reproduced exactly.
                escape.Register(_dragLink);
                escape.Register(_menu);
            }

            Paint(true);
        }

        // U-D-51 retired the full-screen `workshop-view`: the workshop is a producer like any other and lives in
        // the drawer's right column, so there is no second mode to switch into and nothing to switch back from.

        private void WireDetailButtons()
        {
            if (_transfer != null) _transfer.clicked += TransferSelected;
            if (_split != null) { _split.text = TransferText.SplitStack; _split.clicked += SplitSelected; }
            if (_move != null) { _move.text = TransferText.MoveToSlot; _move.clicked += BeginMove; }
            if (_sort != null) { _sort.text = TransferText.Sort; _sort.clicked += () => Send(new InventorySortCommand()); }
            // GP-UX-9. The footer copy is behind a "Select a stack" detail block and reads as one of that block's
            // per-stack actions; sorting is not one. The header copy needs no selection and is never disabled.
            if (_packSort != null) { _packSort.text = TransferText.Sort; _packSort.clicked += () => Send(new InventorySortCommand()); }
        }

        private void WireStrip()
        {
            for (var i = 0; i < _equipment.Length; i++)
            {
                var slot = i;
                var b = _equipment[i];
                if (b == null) continue;
                Register(b, SlotKind.Equipment, slot);
                Tooltips.Attach(b, () => EquipmentTooltip(slot));
                b.RegisterCallback<ClickEvent>(e =>
                {
                    if (UiDrag.ClickSuppressed) return;
                    // A filled equipment slot unequips; an empty one says what it wants instead of doing nothing.
                    if (!string.IsNullOrEmpty(_model.Equipped[slot])) Send(new UnequipCommand(slot));
                    else Say(TransferText.EquipDropHint);
                });
                AttachDrag(b, SlotKind.Equipment, slot);
            }
            if (_reload != null) _reload.clicked += () => Send(new ReloadCommand());
            if (_swap != null) _swap.clicked += () => Send(new SwapWeaponCommand());
        }

        private void BuildQuickbar()
        {
            _quickButtons.Clear();
            if (_quickbar == null) return;
            _quickbar.Clear();
            for (var i = 0; i < QuickSlots; i++)
            {
                var slot = i;
                var b = new Button { name = "quick-" + i };
                b.AddToClassList("quick-slot");
                // The key that selects it: 1..9 then 0 (§5.5).
                b.text = (i == 9 ? 0 : i + 1).ToString();
                b.RegisterCallback<ClickEvent>(e =>
                {
                    if (UiDrag.ClickSuppressed) return;
                    Select(slot);
                });
                Register(b, SlotKind.Quick, slot);
                Tooltips.Attach(b, () => QuickTooltip(slot));
                AttachDrag(b, SlotKind.Quick, slot);
                _quickbar.Add(b);
                _quickButtons.Add(b);
            }
        }

        // ---- grids ----------------------------------------------------------------------------------------

        /// <summary>Make <paramref name="grid"/> hold exactly <paramref name="want"/> slot buttons, reusing what is there.</summary>
        private void Fit(VisualElement grid, List<Button> buttons, int want, SlotKind kind)
        {
            if (grid == null) return;
            while (buttons.Count > want)
            {
                var last = buttons[buttons.Count - 1];
                _refs.Remove(last);
                grid.Remove(last);
                buttons.RemoveAt(buttons.Count - 1);
            }
            while (buttons.Count < want)
            {
                var index = buttons.Count;
                var b = new Button { name = Prefix(kind) + index };
                b.AddToClassList("inventory-slot");
                // Correction pass W-B R2: the owner could not tell one slot from another. Every slot now leads
                // with an icon (ItemIcons: real art when the library has it, a coloured square with a two-letter
                // code until then), then the name, then the count.
                var icon = ItemIcons.Make("slot-icon", out var iconCode);
                icon.AddToClassList("slot-icon");
                var stack = new Label { name = "slot-item", pickingMode = PickingMode.Ignore };
                stack.AddToClassList("slot-item");
                var count = new Label { name = "stack-count", pickingMode = PickingMode.Ignore };
                count.AddToClassList("stack-count");
                b.Add(icon);
                b.Add(stack);
                b.Add(count);
                var role=new Label{name="slot-role",pickingMode=PickingMode.Ignore};role.AddToClassList("slot-role");b.Add(role);
                b.Add(new VisualElement { name="selection-rim", pickingMode=PickingMode.Ignore });
                Tooltips.Attach(b, () => SlotTooltip(kind, index));
                b.RegisterCallback<ClickEvent>(e => OnSlotClick(kind, index, e.shiftKey));
                b.RegisterCallback<PointerDownEvent>(e =>
                {
                    if (e.button != 1) return;    // right button opens the slot menu (reference contextmenu)
                    e.StopPropagation();
                    OpenMenu(kind, index);
                });
                Register(b, kind, index);
                AttachDrag(b, kind, index);
                grid.Add(b);
                buttons.Add(b);
            }
        }

        private static string Prefix(SlotKind kind) =>
            kind == SlotKind.Pack ? "pack-" : kind == SlotKind.Tray ? "tray-" : "store-";

        public void RegisterHudSlot(VisualElement element, int index) => Register(element, SlotKind.Quick, index);

        private void Register(VisualElement e, SlotKind kind, int index)
        {
            if (e == null) return;
            _refs[e] = new SlotRef { Kind = kind, Index = index, Button = e as Button };
        }

        // ---- painting -------------------------------------------------------------------------------------

        /// <summary>Repaint from the view-model. Deferred while a drag is live, exactly as the reference defers it.</summary>
        public void Paint(bool force)
        {
            if (_packGrid == null) return;
            if (host != null && _inventorySession != host.Session)
            {
                _inventorySession = host.Session;
                _model.Pair(-1);
                _configuredMachine = -2;
                _selected = null;
                _moveMode = false;
                _menu?.Close();
                UiDrag.Cancel();
            }
            // A repaint in the middle of a drag would rebuild the element the pointer is over. The reference has
            // the same rule; the deferred repaint runs from the onUiDragEnd hook.
            if (UiDrag.Dragging && !force) return;

            var now = Time.unscaledTimeAsDouble;
            if (!_model.Refresh(host, now, force || _dirty)) return;
            _dirty = false;

            var pack = _model.Pack;
            Fit(_packGrid, _packButtons, pack.Count, SlotKind.Pack);
            for (var i = 0; i < _packButtons.Count; i++) PaintCell(_packButtons[i], pack[i], SlotKind.Pack);

            var store = _model.Store;
            Fit(_storeGrid, _storeButtons, store.Count, SlotKind.Store);
            for (var i = 0; i < _storeButtons.Count; i++) PaintCell(_storeButtons[i], store[i], SlotKind.Store);

            // GP-UX-2. The workshop's output tray, painted whether or not the section is expanded: it is twenty
            // slots, and building them on expansion would mean the first frame of an opened foldout was empty.
            var tray = _model.Tray;
            Fit(_trayGrid, _trayButtons, tray.Count, SlotKind.Tray);
            for (var i = 0; i < _trayButtons.Count; i++) PaintCell(_trayButtons[i], tray[i], SlotKind.Tray);

            if (_drawerHeading != null) _drawerHeading.text = _model.Heading;
            // U-D-51: the left column always sits beside a right one now, so it always wears its own name.
            Show(_packHeading, true);
            if (_packCapacity != null) _packCapacity.text = _model.Capacity;
            if (_storeHeading != null)
                _storeHeading.text = string.IsNullOrEmpty(_model.StoreName) ? "Storage" : _model.StoreName;
            if (_storeCapacity != null) _storeCapacity.text = _model.StoreCapacity;
            var storeHint=_root.Q<Label>("store-hint");if(storeHint!=null)storeHint.text=_model.StoreOutOfReach?"Out of reach — walk closer to transfer items.":_model.StoreHint;
            // A store that is open but out of reach stays on screen and says so; it does not disappear.
            if (_storeInventory != null) Show(_storeInventory, _model.MachineId >= 0);

            PaintMachineControls();
            // U-D-51. The right column IS the producer: a machine's slots and controls, or the Home workshop's
            // tray and cards. It closes only when a paired machine has neither — a belt, say — and the drawer
            // narrows back to the Backpack alone.
            var machineOpen = Showing(_storeInventory);
            var workshopOpen = Showing(_root.Q<VisualElement>("workshop-host"));
            var columnOpen = machineOpen || workshopOpen || Showing(_machineControls);
            if (_storeSide != null) Show(_storeSide, columnOpen);
            // U-D-52 retired the `store-machine` class with the share-of-the-column cap it drove. The slots are
            // fixed at their own height at the top of the column and everything below them divides what is left,
            // so there is no longer a case where one half has to be rationed against the other.
            Show(_root.Q<Button>("jump-workshop"), _model.MachineId >= 0 && !workshopOpen);
            _root.EnableInClassList("storage-open", columnOpen);
            PaintStrip();
            PaintDetail();
            if (_notice != null) _notice.text = _notes;
        }

        private VisualElement _trayGrid;

        private VisualElement _machineBoundRoot;
        private void BindMachineControls()
        {
            if (_machineBoundRoot == _root) return;
            _machineBoundRoot = _root;
            _machineControls = _root.Q<VisualElement>("machine-controls");
            _machineStatus = _root.Q<Label>("machine-status");
            _machineProgress = _root.Q<ProgressBar>("machine-progress");
            _machineRepair = _root.Q<VisualElement>("machine-repair");
            _machineRepairText = _root.Q<Label>("machine-repair-text");
            _machineRepairButton = _root.Q<Button>("machine-repair-button");
            // GP-W5. One button, two commands: it starts the repair of the machine on screen, and while that
            // repair is running it is the way out of the hand-lock — the same pair the core's card offers.
            if (_machineRepairButton != null) _machineRepairButton.clicked += () =>
            {
                var s = host?.Simulation?.State;
                var h = s?.Home;
                if (h != null && h.RepairKind == RepairKinds.Machine && h.RepairId == _model.MachineId)
                    Send(new CancelRepairCommand());
                else Send(new RepairCommand(RepairKinds.Machine, _model.MachineId));
            };
            _recipePicker = _root.Q<DropdownField>("machine-recipe");
            _recipeDescription = _root.Q<Label>("recipe-description");
            _applyRecipe = _root.Q<Button>("apply-recipe");
            _filterPicker = _root.Q<DropdownField>("machine-filter");
            _priorityPicker = _root.Q<DropdownField>("machine-priority");
            _closeDrawer = _root.Q<Button>("close-inventory");
            if (_closeDrawer != null) _closeDrawer.clicked += () => FindAnyObjectByType<UiShell>()?.CloseActive();
            if (_applyRecipe != null) _applyRecipe.clicked += () =>
            {
                var index = _recipePicker?.index ?? -1;
                if (index >= 0 && index < _recipeKeys.Count) Send(new SetRecipeCommand(_model.MachineId, _recipeKeys[index]));
            };
            _recipePicker?.RegisterValueChangedCallback(_ => PaintRecipeDescription());
            _filterPicker?.RegisterValueChangedCallback(_ => Send(new SetInserterFilterCommand(_model.MachineId, _filterPicker.index - 1)));
            _priorityPicker?.RegisterValueChangedCallback(_ => Send(new SetSplitterPriorityCommand(_model.MachineId, _priorityPicker.index)));
        }

        private void PaintMachineControls()
        {
            var sim = host == null ? null : host.Simulation;
            var m = sim?.State.MachineById(_model.MachineId);
            Show(_machineControls, m != null);
            // U-D-52: the controls sit in their own scroller now, because they are the part of a machine’s
            // column that may run past the bottom; an empty scroller would still draw a gutter, so it goes
            // with them.
            Show(_root.Q<ScrollView>("machine-scroll"), m != null);
            // U-D-51: the section itself shows or hides now — the foldout that used to wrap it is gone.
            var workshop = _root.Q<VisualElement>("workshop-host");
            // U-D-55. Was `m == null || m.Kind == "depot"`, which showed the workshop under every Tab, because
            // Tab pairs with no machine. It is shown when the player OPENED it — by interacting with the depot,
            // or by the drawer's own button — and when the paired machine is the depot, which is the same thing
            // reached from its own tile.
            Show(workshop, _workshop || (m != null && m.Kind == "depot"));
            if (m == null) return;
            var ctx = sim.Context;
            var status = ProductionQueries.Status(ctx, sim.State, m.Id);
            var reachable = Interaction.InReach(ctx, sim.State, m);
            if (_machineStatus != null) _machineStatus.text = ProductionQueries.Description(ctx, sim.State, m.Id) + (reachable ? "" : "\nOut of reach — walk closer to transfer or configure.");
            if (_machineProgress != null) { _machineProgress.value = (float)(status.Progress * 100); _machineProgress.title = status.Progress > 0 ? "Processing" : status.Text; }
            PaintRepairRow(ctx, sim.State, m);
            if (_storeInventory != null) Show(_storeInventory, ctx.Data.TryMachine(m.Kind, out var spec) && spec.HasInventory);
            if (_configuredMachine != m.Id)
            {
                _configuredMachine = m.Id;
                ProductionRules.RecipesFor(ctx.Data, m, _recipes);
                _recipeKeys.Clear();
                var labels = new List<string>();
                foreach (var recipe in _recipes) { _recipeKeys.Add(recipe.Key); labels.Add(recipe.DisplayName); }
                if (_recipePicker != null)
                {
                    _recipePicker.choices = labels;
                    var chosen = _recipeKeys.IndexOf(status.Recipe);
                    _recipePicker.SetValueWithoutNotify(chosen >= 0 ? labels[chosen] : labels.Count > 0 ? labels[0] : "");
                }
                if (_filterPicker != null)
                {
                    var items = new List<string> { "Any item" };
                    for (int i = 0; i < Items.Count; i++) items.Add(ctx.Data.Item((ItemId)i).DisplayName);
                    _filterPicker.choices = items;
                    var filter = sim.State.Flow.Find(m.Id)?.Filter ?? -1;
                    _filterPicker.SetValueWithoutNotify(items[System.Math.Clamp(filter + 1, 0, items.Count - 1)]);
                }
                if (_priorityPicker != null)
                {
                    _priorityPicker.choices = new List<string> { "Balanced", "Left first", "Right first" };
                    var priority = sim.State.Flow.Find(m.Id)?.Priority ?? 0;
                    _priorityPicker.SetValueWithoutNotify(_priorityPicker.choices[System.Math.Clamp(priority, 0, 2)]);
                }
                PaintRecipeDescription();
            }
            Show(_recipePicker, _recipes.Count > 0);
            Show(_applyRecipe, _recipes.Count > 0);
            Show(_recipeDescription, _recipes.Count > 0);
            Show(_filterPicker, FlowRules.IsInserter(m.Kind));
            Show(_priorityPicker, FlowRules.IsSplitter(m.Kind));
            _recipePicker?.SetEnabled(reachable);
            _applyRecipe?.SetEnabled(reachable);
            _filterPicker?.SetEnabled(reachable);
            _priorityPicker?.SetEnabled(reachable);
        }

        /// <summary>
        /// GP-W5's repair row. Everything on it is the sim's own answer
        /// (<see cref="HomeQueries.MachineRepairCard"/>): the wording, the price, how many instalments a full
        /// repair takes and — when the button is greyed — the refusal <c>RepairCommand</c> would actually give.
        /// The row is absent for a machine with nothing to repair, so an intact assembler looks exactly as it did.
        /// </summary>
        private void PaintRepairRow(SimContext ctx, SimState st, Machine m)
        {
            if (_machineRepair == null) return;
            var v = HomeQueries.MachineRepairCard(ctx, st, m.Id);
            var show = v.Damaged || v.InProgress;
            Show(_machineRepair, show);
            if (!show) return;

            SetClass(_machineRepair, "is-wrecked", v.Wrecked && !v.InProgress);
            if (_machineRepairText != null)
            {
                var line = HomeQueries.MachineRepairLine(ctx, st, m.Id);
                _machineRepairText.text = v.InProgress ? line + " · " + Home.LockText : line;
            }
            if (_machineRepairButton == null) return;
            _machineRepairButton.text = v.InProgress
                ? "Cancel repair"
                : "Repair +" + PackLayout.Num(v.RepairHp) + " HP · " + PackLayout.Num(v.Seconds) + " s";
            // A greyed button says why on the panel rather than going quiet — the workshop card's rule.
            _machineRepairButton.SetEnabled(v.InProgress || v.Problem.Length == 0);
            _machineRepairButton.tooltip = v.InProgress ? "" : v.Problem;
        }

        private void PaintRecipeDescription()
        {
            var i = _recipePicker?.index ?? -1;
            if (_recipeDescription == null || i < 0 || i >= _recipes.Count || host?.Simulation == null) return;
            var recipe = _recipes[i];
            var d = host.Simulation.Context.Data;
            var inputs = new List<string>();
            foreach (var item in recipe.Inputs) inputs.Add(item.Count + " " + d.Item(item.Item).DisplayName);
            var outputs = new List<string>();
            foreach (var item in recipe.Outputs) outputs.Add(item.Count + " " + d.Item(item.Item).DisplayName);
            _recipeDescription.text = string.Join(" + ", inputs) + " → " + string.Join(" + ", outputs) + " · " + recipe.Seconds + " s";
        }

        /// <summary>
        /// One slot, in whichever grid it belongs to. <paramref name="kind"/> replaced a <c>bool pack</c> when the
        /// workshop tray became a third grid (GP-UX-2): with two grids "not pack" meant "store", and the selection
        /// rim keyed off that — so tray slot 3 would have lit up whenever store slot 3 was selected.
        /// </summary>
        private void PaintCell(Button b, InventoryViewModel.Cell cell, SlotKind kind)
        {
            if (b == null) return;
            var pack = kind == SlotKind.Pack;
            var item = b.Q<Label>("slot-item");
            var count = b.Q<Label>("stack-count");
            ItemIcons.Paint(b.Q<VisualElement>("slot-icon"), b.Q<Label>("slot-icon-code"), cell.Empty ? cell.FilterItem ?? "" : cell.Item);
            SetClass(b,"reserved-empty",!pack&&cell.Empty&&!string.IsNullOrEmpty(cell.FilterItem));
            SetClass(b,"output-slot",!pack&&!cell.CanLoad);
            var role=b.Q<Label>("slot-role");if(role!=null)role.text=pack?"":cell.Role;
            if (item != null) item.text = cell.Empty ? "" : cell.DisplayName;
            if (count != null) count.text = !pack&&cell.Limit>0?$"{cell.Count:0.#}/{cell.Limit:0}":cell.Empty?"":PackLayout.Num(Math.Floor(cell.Count));
            // The C5 tooltip replaces Unity's built-in one: same information, the project's own type and colours,
            // and it can carry have/need rows. Leaving both on would show two tooltips for one slot.
            b.tooltip = "";
            SetClass(b, "empty", cell.Empty);
            SetClass(b, "selected", _selected != null && _selected.Index == cell.Index && _selected.Kind == kind);
        }

        private void PaintStrip()
        {
            for (var i = 0; i < _equipment.Length; i++)
            {
                var b = _equipment[i];
                if (b == null) continue;
                if(b.Q("equipment-icon")==null) {
                    b.text="";
                    var icon=ItemIcons.Make("equipment-icon",out _);icon.AddToClassList("equipment-icon");b.Add(icon);
                    var caption=new Label {name="equipment-caption",pickingMode=PickingMode.Ignore};caption.AddToClassList("equipment-caption");b.Add(caption);
                    b.Add(new VisualElement {name="selection-rim",pickingMode=PickingMode.Ignore});
                }
                ItemIcons.Paint(b.Q("equipment-icon"),b.Q<Label>("equipment-icon-code"),_model.Equipped[i] ?? "");
                b.Q<Label>("equipment-caption").text=(i+1)+" · "+(string.IsNullOrEmpty(_model.EquippedNames[i]) ? "Empty" : _model.EquippedNames[i]);
                SetClass(b, "active", _model.ActiveSlot == i);
                b.tooltip = "";   // C5 tooltip instead (see EquipmentTooltip)
            }
            if (_equipmentLabel != null)
            {
                _equipmentLabel.text = _model.HasWeapon
                    ? PackLayout.Num(_model.Loaded) + " / " + PackLayout.Num(_model.MagazineCapacity)
                      + " · " + PackLayout.Num(_model.Reserve) + " spare"
                    : "No weapon equipped";
            }
            // Reload and Swap are never hidden: they are enabled or they refuse in the sim's own words.
            if (_reload != null) _reload.SetEnabled(_model.HasWeapon && !_model.Reloading);
            if (_swap != null) _swap.SetEnabled(true);

            for (var i = 0; i < _quickButtons.Count; i++)
            {
                var bar = _model.Bar;
                var key = i < bar.Count ? bar[i] : "";
                var b = _quickButtons[i];
                b.tooltip = "";   // C5 tooltip instead (see QuickTooltip)
                b.userData = key; // the assigned tool's key, readable by tests and a debug panel (U-2)
                SetClass(b, "empty", string.IsNullOrEmpty(key));
                SetClass(b, "active", _model.ActiveSlot == i);
            }
        }

        private void PaintDetail()
        {
            if (_detail == null) return;
            var cell = Cell(_selected);
            if (cell == null || cell.Empty)
            {
                if (_detailTitle != null) _detailTitle.text = TransferText.SelectStack;
                if (_detailDescription != null) _detailDescription.text = TransferText.CarriedNote;
                if (_transfer != null) { _transfer.text = TransferText.TakeIntoBackpack; _transfer.SetEnabled(false); }
                if (_split != null) _split.SetEnabled(false);
                if (_move != null) _move.SetEnabled(false);
                return;
            }

            var pack = _selected.Kind == SlotKind.Pack;
            if (_detailTitle != null) _detailTitle.text = cell.DisplayName;
            if (_detailDescription != null)
                _detailDescription.text = TransferText.StackDetail(cell.Count, cell.StackSize, pack);
            if (_transfer != null)
            {
                _transfer.text = pack
                    ? TransferText.LoadInto(string.IsNullOrEmpty(_model.StoreName) ? "Home storage" : _model.StoreName)
                    : TransferText.TakeIntoBackpack;
                _transfer.SetEnabled(_model.MachineId >= 0);
            }
            // Split and Move are Backpack-only: storage is a pool, and a pool has no slot to split into (§5.2).
            if (_split != null) _split.SetEnabled(pack);
            if (_move != null) _move.SetEnabled(pack);
        }

        // ---- selection and the slot menu -------------------------------------------------------------------

        private InventoryViewModel.Cell Cell(SlotRef r)
        {
            if (r == null) return null;
            if (r.Kind == SlotKind.Pack) return r.Index < _model.Pack.Count ? _model.Pack[r.Index] : null;
            if (r.Kind == SlotKind.Store) return r.Index < _model.Store.Count ? _model.Store[r.Index] : null;
            if (r.Kind == SlotKind.Tray) return _model.TrayAt(r.Index);
            return null;
        }

        private void OnSlotClick(SlotKind kind, int index, bool shift)
        {
            if (UiDrag.ClickSuppressed) return;
            _menu?.Close();

            if (_moveMode && kind == SlotKind.Pack)
            {
                MoveTo(index);
                return;
            }

            // GP-UX-2. A tray slot has exactly one thing it can do, so clicking it does that thing: take that
            // stack. Selecting it would be an empty gesture — Transfer, Split and Move all act against the paired
            // store, and the tray belongs to no machine — and an empty gesture is what the Collect-only tray
            // already was. The sim's refusal (out of reach, no room) is shown verbatim, as everywhere else.
            if (kind == SlotKind.Tray)
            {
                var slot = _model.TrayAt(index);
                if (slot == null || slot.Empty) { Say(""); return; }
                if (!_model.TrayInReach) { Say(WorkshopText.CollectAway); return; }
                Send(new CollectWorkshopCommand(slot.Item, Math.Floor(slot.Count)));
                return;
            }

            var wasSelected = _selected;
            _selected = new SlotRef { Kind = kind, Index = index };
            if (shift)
            {
                // Shift-click is the whole-stack transfer, the reference's one-gesture "send it across".
                TransferSelected();
                return;
            }
            // Clicking the selected slot again clears the selection, so the detail block can be dismissed.
            if (wasSelected != null && wasSelected.Kind == kind && wasSelected.Index == index) _selected = null;
            Say("");
            Paint(true);
        }

        private void OpenMenu(SlotKind kind, int index)
        {
            // The tray has no menu: every entry on it (transfer, split, move to slot) addresses a Backpack slot or
            // the paired store, and the tray is neither.
            if (kind == SlotKind.Tray) { _menu?.Close(); return; }
            _selected = new SlotRef { Kind = kind, Index = index };
            var cell = Cell(_selected);
            if (cell == null || cell.Empty) { _menu?.Close(); return; }
            var pack = kind == SlotKind.Pack;
            var label = pack
                ? TransferText.LoadInto(string.IsNullOrEmpty(_model.StoreName) ? "Home storage" : _model.StoreName)
                : TransferText.TakeIntoBackpack;
            var button = pack ? _packButtons[index] : _storeButtons[index];
            _menu?.Show(button, label, pack);
            Paint(true);
        }

        private void MenuTransfer() => TransferSelected();
        private void MenuSplit() => SplitSelected();
        private void MenuMove() => BeginMove();
        private void MenuSort() => Send(new InventorySortCommand());

        private void Select(int quickSlot)
        {
            // §5.5: an action-bar key selects that slot's equipment. The sim owns what is on the bar; the panel
            // only asks for it, and the sim's refusal (an empty slot, a weapon no longer carried) is shown as-is.
            var bar = _model.Bar;
            var key = quickSlot < bar.Count ? bar[quickSlot] : "";
            if (string.IsNullOrEmpty(key)) { Say(TransferText.ChooseDestination); return; }
            var input = FindAnyObjectByType<WorldInput>();
            if (input != null) input.SelectBarSlot(quickSlot);
        }

        private static int SlotIndexOf(UnityEngine.InputSystem.InputAction slot)
        {
            // The Slot action carries ten bindings (1..9, 0). The control that fired names its own key, and the
            // binding order in RelightControls.inputactions is the slot order, so its index IS the slot.
            var control = slot.activeControl;
            if (control == null) return -1;
            for (var i = 0; i < slot.bindings.Count; i++)
            {
                var path = slot.bindings[i].effectivePath;
                if (string.IsNullOrEmpty(path)) continue;
                if (path.EndsWith("/" + control.name, StringComparison.Ordinal)) return i;
            }
            return -1;
        }

        // ---- the four actions -----------------------------------------------------------------------------

        private void TransferSelected()
        {
            var cell = Cell(_selected);
            if (cell == null || cell.Empty) { Say(TransferText.SelectStack); return; }
            if (_model.MachineId < 0) { Say(TransferText.StoreRemoved); return; }
            var pack = _selected.Kind == SlotKind.Pack;
            var n = Quantity(cell);
            if (n <= 0) return;
            // A null target is "the sim decides where it lands" (Pockets.Target returns everything for a null
            // target). Passing an empty TransferTargetSpec instead would look the same and would be refused,
            // because an explicit target with no layout is a STALE target by definition.
            SendTransfer(cell, pack, n, null);
        }

        private void SplitSelected()
        {
            var cell = Cell(_selected);
            if (cell == null || cell.Empty) { Say(TransferText.SelectStack); return; }
            if (_selected.Kind != SlotKind.Pack) { Say(TransferText.SplitNeedsEmpty); return; }
            var empty = FirstEmptyPackSlot();
            if (empty < 0) { Say(TransferText.SplitNeedsEmpty); return; }
            var n = Quantity(cell, half: true);
            if (n <= 0) return;
            Send(new InventorySplitCommand(_selected.Index, empty, cell.Item, cell.Count, _model.Layout, n));
        }

        private void BeginMove()
        {
            var cell = Cell(_selected);
            if (cell == null || cell.Empty) { Say(TransferText.SelectStack); return; }
            if (_selected.Kind != SlotKind.Pack) { Say(TransferText.ChooseDestination); return; }
            _moveMode = true;
            Say(TransferText.ChooseDestination);
        }

        private void MoveTo(int to)
        {
            _moveMode = false;
            var cell = Cell(_selected);
            if (cell == null || cell.Empty) { Say(TransferText.SourceChanged); return; }
            Send(new InventoryMoveCommand(_selected.Index, to, cell.Item, cell.Count, _model.Layout));
        }

        private int FirstEmptyPackSlot()
        {
            var pack = _model.Pack;
            for (var i = 0; i < pack.Count; i++) if (pack[i].Empty) return i;
            return -1;
        }

        /// <summary>
        /// The quantity field, parsed. An unreadable or non-positive value is the reference's
        /// "Choose a whole positive quantity." and nothing is sent — which is why this is a TextField and not an
        /// IntegerField: an IntegerField would silently clamp and there would be nothing to refuse.
        /// An empty field means the whole stack (or half of it, for Split).
        /// </summary>
        private int Quantity(InventoryViewModel.Cell cell, bool half = false)
        {
            var whole = (int)Math.Floor(cell.Count);
            var fallback = half ? Math.Max(1, whole / 2) : whole;
            var text = _quantity == null ? "" : (_quantity.value ?? "").Trim();
            if (text.Length == 0) return fallback;
            if (!int.TryParse(text, System.Globalization.NumberStyles.Integer,
                    System.Globalization.CultureInfo.InvariantCulture, out var n) || n <= 0)
            {
                Say(TransferText.WholeQuantity);
                return 0;
            }
            return Math.Min(n, whole);
        }

        /// <summary>
        /// GP-UX-9. How many a Shift-drag splits off: the Quantity field when it holds a whole positive number,
        /// otherwise half the stack — which is the sim's own default for a negative <c>N</c>.
        ///
        /// Deliberately NOT <see cref="Quantity"/>: that one writes to the notice line when the field is
        /// unreadable, and this is asked on every pointer move to caption the ghost. A preview must not talk.
        /// A zero answer is an unreadable field, and the drop says so in the shared words.
        /// </summary>
        private int SplitAmount(StackSnapshot payload)
        {
            var whole = (int)Math.Floor(payload.Count);
            var text = _quantity == null ? "" : (_quantity.value ?? "").Trim();
            if (text.Length == 0) return Math.Max(1, whole / 2);
            return int.TryParse(text, System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out var n) && n > 0
                ? Math.Min(n, whole)
                : 0;
        }

        // ---- drag and drop --------------------------------------------------------------------------------

        private void AttachDrag(VisualElement e, SlotKind kind, int index)
        {
            if (e == null) return;
            StackDragManipulator m = null;
            m = new StackDragManipulator(
                start: () => Snapshot(kind, index),
                preview: (under, payload, at) =>
                {
                    var target = Resolve(under) ?? NearestSlot(at.Position);
                    var refusal=DropRefusal(target,payload,at);
                    m.Highlight(target == null ? null : target.Button,refusal.Length>0);
                    return refusal.Length>0?refusal:PreviewText(target, payload, at);
                },
                drop: (under, payload, at) => Drop(Resolve(under) ?? NearestSlot(at.Position), payload, at),
                ghost: () => _ghost);
            e.AddManipulator(m);
        }

        private StackSnapshot Snapshot(SlotKind kind, int index)
        {
            if (kind == SlotKind.Equipment)
            {
                var id = _model.Equipped[index];
                if (string.IsNullOrEmpty(id)) return null;
                return new StackSnapshot
                {
                    Pack = true, Index = -1 - index,      // negative index encodes "from equipment slot n"
                    Item = id, DisplayName = _model.EquippedNames[index], Count = 1, StackSize = 1,
                    IsWeapon = true, Layout = _model.Layout, Source = 1, Target = _model.MachineId,
                };
            }
            if (kind == SlotKind.Quick) return null;      // the bar is a reference to a weapon, not a stack

            var cell = Cell(new SlotRef { Kind = kind, Index = index });
            if (cell == null || cell.Empty) return null;
            var pack = kind == SlotKind.Pack;
            var tray = kind == SlotKind.Tray;
            return new StackSnapshot
            {
                Pack = pack,
                Tray = tray,
                Index = index,
                Item = cell.Item,
                DisplayName = cell.DisplayName,
                Count = cell.Count,
                StackSize = cell.StackSize,
                IsWeapon = cell.IsWeapon,
                Layout = _model.Layout,
                Source = tray ? cell.Count : SourceTotal(cell, pack),
                Target = _model.MachineId,
            };
        }

        /// <summary>What the SOURCE held when the stack was picked up: the carried total, or the pooled total.</summary>
        private double SourceTotal(InventoryViewModel.Cell cell, bool pack)
        {
            var sim = host == null ? null : host.Simulation;
            if (sim == null) return cell.Count;
            var st = sim.State;
            if (!new ItemKey(cell.Item).IsItem(out var id)) return cell.Count;
            if (pack) return st.Engineer.Inv[id];
            var total = 0.0;
            var store = _model.Store;
            for (var i = 0; i < store.Count; i++)
                if (string.Equals(store[i].Item, cell.Item, StringComparison.Ordinal)) total += store[i].Count;
            return total;
        }

        private SlotRef Resolve(VisualElement under)
        {
            var e = under;
            while (e != null)
            {
                if (_refs.TryGetValue(e, out var r)) return r;
                e = e.parent;
            }
            return null;
        }

        /// <summary>
        /// GP-UX-9. The slot nearest <paramref name="p"/> inside whichever slot grid the pointer is in.
        ///
        /// <see cref="Resolve"/> answers the question <c>panel.Pick</c> asks — "what is exactly under this pixel" —
        /// and a wrapping grid inside a ScrollView is not a solid surface. Between two slots, on the grid's own
        /// background, on the scroll viewport and on the scroller there is nothing registered, so a release a few
        /// pixels off a slot resolved to NO target: the stack did not move and the notice said nothing. A Backpack
        /// the player has never arranged is displayed auto-compacted (<c>Engineer.Pack</c> is null until an
        /// inventory command writes it), so every stack sits at the front and "nothing moved" is indistinguishable
        /// from "it sorted my stack back into the first free slot". That is the defect the owner reported.
        ///
        /// The grid is the bound, not a pixel tolerance: a release anywhere inside the Backpack grid names a
        /// Backpack slot, a release inside storage names a storage chunk, and neither can ever snap into the
        /// other. Slots scrolled out of the viewport are not candidates, so the answer is always a slot the
        /// player can see.
        /// </summary>
        private SlotRef NearestSlot(Vector2 p)
            => Nearest(_packGrid, _packButtons, p)
               ?? Nearest(_storeGrid, _storeButtons, p)
               ?? Nearest(_trayGrid, _trayButtons, p);

        private SlotRef Nearest(VisualElement grid, List<Button> buttons, Vector2 p)
        {
            if (grid == null || buttons.Count == 0) return null;
            var clip = Clip(grid);
            if (clip.width <= 0 || clip.height <= 0 || !clip.Contains(p)) return null;
            Button best = null;
            var bestSq = float.MaxValue;
            for (var i = 0; i < buttons.Count; i++)
            {
                var r = buttons[i].worldBound;
                if (r.width <= 0 || r.height <= 0 || !r.Overlaps(clip)) continue;
                if (r.Contains(p)) { best = buttons[i]; break; }
                var d = (r.center - p).sqrMagnitude;
                if (d >= bestSq) continue;
                bestSq = d;
                best = buttons[i];
            }
            return best != null && _refs.TryGetValue(best, out var slot) ? slot : null;
        }

        /// <summary>What of <paramref name="grid"/> is actually on screen: its own box, clipped by what scrolls it.</summary>
        private static Rect Clip(VisualElement grid)
        {
            var r = grid.worldBound;
            var scroll = grid.GetFirstAncestorOfType<ScrollView>();
            var view = scroll?.contentViewport;
            if (view == null) return r;
            var v = view.worldBound;
            var x = Mathf.Max(r.xMin, v.xMin);
            var y = Mathf.Max(r.yMin, v.yMin);
            return new Rect(x, y, Mathf.Min(r.xMax, v.xMax) - x, Mathf.Min(r.yMax, v.yMax) - y);
        }

        /// <summary>
        /// GP-UX-9. Why this release cannot do what the player is asking for — the ordinary rules, plus the split
        /// rules when Shift is held.
        /// </summary>
        private string DropRefusal(SlotRef target, StackSnapshot payload, DragPoint at)
        {
            var refusal = DropRefusal(target, payload);
            if (refusal.Length > 0 || !at.Split) return refusal;
            return SplitRefusal(target, payload);
        }

        /// <summary>
        /// GP-UX-9. Why a Shift-drag cannot split HERE. The rules are the sim's own
        /// (<c>InventoryCommandHandler.Apply</c>: a carried source, an empty Backpack destination, fewer than the
        /// stack holds), asked before the release so the ghost states them while the player can still change
        /// their mind, instead of the notice having to explain a refusal afterwards.
        /// </summary>
        private string SplitRefusal(SlotRef target, StackSnapshot payload)
        {
            if (payload.Tray || !payload.Pack || payload.Index < 0) return "Split a carried stack";
            if (payload.IsWeapon) return "A weapon is one item; there is nothing to split";
            if (target.Kind != SlotKind.Pack) return "Split into an empty Backpack slot";
            var cell = Cell(target);
            if (cell != null && !cell.Empty) return TransferText.SplitNeedsEmptySlot;
            if (payload.Count < 2) return TransferText.SplitTooFew;
            return "";
        }

        private string DropRefusal(SlotRef target,StackSnapshot payload)
        {
            if(target==null)return "Release to cancel";
            // GP-UX-2. The tray takes nothing: no command loads it, so a slot that accepted a drop would be
            // promising something the sim cannot do. It gives up a stack only into the Backpack, and only in reach.
            if(target.Kind==SlotKind.Tray)return "Output only — take items into Backpack";
            if(payload.Tray)
            {
                if(target.Kind!=SlotKind.Pack)return TransferText.TakeIntoBackpack;
                if(!_model.TrayInReach)return WorkshopText.CollectAway;
                return "";
            }
            if(target.Kind==SlotKind.Store)
            {
                if(!payload.Pack)return "Choose a Backpack slot";
                if(payload.IsWeapon)return TransferText.WeaponsNotStored;
                var cell=Cell(target);
                if(cell!=null&&!cell.CanLoad)return cell.Role=="Full"?"Storage is full":"Output only — take items into Backpack";
                if(cell!=null&&!string.IsNullOrEmpty(cell.FilterItem)&&cell.FilterItem!=payload.Item)return "Requires "+cell.DisplayName;
                if(!new ItemKey(payload.Item).IsItem(out var item))return "Packed structures stay in Backpack";
                if(host?.Simulation!=null)
                {var sim=host.Simulation;var p=InventoryQueries.Preview(sim.Context,sim.State,_model.MachineId,item,(int)Math.Floor(payload.Count),true);if(p.Moved<=0)return p.Reason;}
            }
            if(target.Kind==SlotKind.Equipment&&!payload.IsWeapon)return TransferText.WeaponTarget;
            if(target.Kind==SlotKind.Quick&&!Shortcut(payload))return "Use a weapon or packed structure for a shortcut";
            return "";
        }

        private bool Shortcut(StackSnapshot payload) => payload.IsWeapon || (host?.Simulation!=null && host.Simulation.Context.Data.TryMachine(payload.Item,out _));

        /// <summary>
        /// GP-UX-9. The ghost's second line when Shift is held: the split, named with its amount and its
        /// destination, so the gesture announces itself rather than having to be discovered.
        /// </summary>
        private string PreviewText(SlotRef target, StackSnapshot payload, DragPoint at)
        {
            if (!at.Split || target == null) return PreviewText(target, payload);
            return TransferText.DragSplit(SplitAmount(payload), target.Index);
        }

        /// <summary>The ghost's second line: what releasing here would do (reference uiDrag.ts preview).</summary>
        private string PreviewText(SlotRef target, StackSnapshot payload)
        {
            if (target == null) return TransferText.DragCancel;
            if (payload.Index < 0) return target.Kind == SlotKind.Pack ? TransferText.DragUnequip : TransferText.DragCancel;
            if (payload.Tray) return target.Kind == SlotKind.Pack ? TransferText.TakeIntoBackpack : TransferText.DragCancel;
            switch (target.Kind)
            {
                case SlotKind.Equipment:
                    return payload.IsWeapon ? TransferText.DragEquip : TransferText.WeaponTarget;
                case SlotKind.Quick:
                    return Shortcut(payload) ? "Assign shortcut" : "Use a weapon or packed structure";
                case SlotKind.Tray:
                    return "Output only — take items into Backpack";
                case SlotKind.Store:
                    return payload.IsWeapon ? TransferText.WeaponsNotStored
                         : TransferText.LoadInto(string.IsNullOrEmpty(_model.StoreName) ? "Home storage" : _model.StoreName);
                default:
                    return payload.Pack ? TransferText.DragMove : TransferText.TakeIntoBackpack;
            }
        }

        private void Drop(SlotRef target, StackSnapshot payload) => Drop(target, payload, default);

        private void Drop(SlotRef target, StackSnapshot payload, DragPoint at)
        {
            if (payload == null) return;
            // GP-UX-9. A null target now means the release really was away from every slot grid — a near miss
            // inside one resolves to the nearest slot (NearestSlot). Saying nothing was the other half of the
            // reported defect: the stack stayed where it was and the drawer gave no reason at all.
            if (target == null) { Say(TransferText.DropOffSlot); return; }

            // The store the drag started against must still be the store on screen (§5.4).
            if (payload.Target != _model.MachineId) { Say(TransferText.StoreChanged); return; }
            var refusal=DropRefusal(target,payload,at);if(refusal.Length>0){Say(refusal);return;}

            // GP-UX-9. Shift-drag splits, and it splits into the slot under the pointer — which is the whole point
            // of doing it as a gesture. The Split BUTTON has no destination to read, so it keeps using the first
            // empty slot (SplitSelected); this is the one that lets the player choose. The rules have already been
            // checked by SplitRefusal above, so what is left is the amount and the command.
            if (at.Split)
            {
                var n = SplitAmount(payload);
                if (n <= 0) { Say(TransferText.WholeQuantity); return; }
                Send(new InventorySplitCommand(payload.Index, target.Index, payload.Item, payload.Count,
                    payload.Layout, n));
                return;
            }

            // GP-UX-2. Out of the workshop tray. The refusal above has already established that the target is a
            // Backpack slot and that the engineer is at the workshop; what is left is to ask for THIS stack rather
            // than the lot, which is the one thing the old Collect button could not express. The sim chooses where
            // it lands in the Backpack — a tray chunk is a slice of a pooled count, not a slot, so there is no
            // source slot to pair with a destination one.
            if (payload.Tray)
            {
                Send(new CollectWorkshopCommand(payload.Item, Math.Floor(payload.Count)));
                return;
            }

            // From an equipment slot: the only thing that can happen is going back into the Backpack.
            if (payload.Index < 0)
            {
                var slot = -1 - payload.Index;
                if (target.Kind == SlotKind.Pack) Send(new UnequipCommand(slot));
                else Say(TransferText.WeaponTarget);
                return;
            }

            switch (target.Kind)
            {
                case SlotKind.Equipment:
                    if (!payload.IsWeapon) { Say(TransferText.WeaponTarget); return; }
                    Send(new EquipCommand(payload.Item, target.Index));
                    return;

                case SlotKind.Quick:
                    if (!Shortcut(payload)) { Say("Use a weapon or packed structure for a shortcut"); return; }
                    Send(new AssignBarCommand(target.Index, payload.Item));
                    return;

                case SlotKind.Store:
                    if (payload.IsWeapon) { Say(TransferText.WeaponsNotStored); return; }
                    if (!payload.Pack) { Say(""); return; }      // store → store is not a move
                    if (_model.MachineId < 0) { Say(TransferText.StoreRemoved); return; }
                    SendTransfer(CellOf(payload), true, (int)Math.Floor(payload.Count),
                        new TransferTargetSpec(Item: TargetItem(target)), payload);
                    return;

                default:
                    if (payload.Pack)
                    {
                        if (target.Index == payload.Index) return;
                        Send(new InventoryMoveCommand(payload.Index, target.Index, payload.Item, payload.Count,
                            payload.Layout));
                    }
                    else
                    {
                        if (_model.MachineId < 0) { Say(TransferText.StoreRemoved); return; }
                        SendTransfer(CellOf(payload), false, (int)Math.Floor(payload.Count),
                            new TransferTargetSpec(target.Index, payload.Layout, payload.Item), payload);
                    }
                    return;
            }
        }

        /// <summary>The item key of the chunk being dropped on, so a store drop merges rather than displacing.</summary>
        private string TargetItem(SlotRef target)
        {
            var cell = Cell(target);
            return cell == null ? null : cell.FilterItem ?? (cell.Empty ? null : cell.Item);
        }

        private static InventoryViewModel.Cell CellOf(StackSnapshot p) => new InventoryViewModel.Cell
        {
            Index = p.Index, Item = p.Item, DisplayName = p.DisplayName,
            Count = p.Count, StackSize = p.StackSize, IsWeapon = p.IsWeapon,
        };

        // ---- sending --------------------------------------------------------------------------------------

        /// <summary>
        /// One transfer, with the snapshot the panel had. <paramref name="put"/> loads the store; otherwise the
        /// store is emptied into the Backpack. The reply is the sim's, and a PARTIAL move is reported as what
        /// actually moved plus where the rest stayed (§5.4 <see cref="TransferText.Moved"/>).
        /// </summary>
        private void SendTransfer(InventoryViewModel.Cell cell, bool put, int n, TransferTargetSpec target,
            StackSnapshot payload = null)
        {
            if (cell == null || cell.Empty) { Say(TransferText.SelectStack); return; }
            var key = new ItemKey(cell.Item);
            if (key.IsWeapon) { Say(TransferText.WeaponsNotStored); return; }
            if (!key.IsItem(out var id)) { Say(TransferText.WeaponsNotStored); return; }
            if (n <= 0) { Say(TransferText.WholeQuantity); return; }

            var expected = payload != null
                ? new TransferExpectation(payload.Source, payload.Layout, put ? payload.Index : -1)
                : new TransferExpectation(SourceTotal(cell, put), _model.Layout, put ? cell.Index : -1);

            var sim = host == null ? null : host.Simulation;
            if (sim == null) return;

            var storeName = string.IsNullOrEmpty(_model.StoreName) ? "Home storage" : _model.StoreName;
            var before = put ? sim.State.Engineer.Inv[id] : 0;
            var result = sim.Apply(new MachineTransferCommand(_model.MachineId, id, n, put, target, expected));
            if (!result.Accepted) { Say(result.Problem); return; }

            FindAnyObjectByType<FrontEnd.FrontEndBootstrap>()?.Dirty.MarkChanged();
            // How much actually moved is the sim's answer, not the panel's arithmetic: read it back.
            var after = put ? sim.State.Engineer.Inv[id] : 0;
            var moved = put ? before - after : Moved(result.Problem, n);
            Say(TransferText.Moved(moved, n, cell.DisplayName,
                put ? "the Backpack" : storeName, put ? storeName : "the Backpack"));
            _dirty = true;
            Paint(true);
        }

        /// <summary>The leading number of the sim's own "N Steel plates taken" note; falls back to what was asked.</summary>
        private static double Moved(string note, int asked)
        {
            if (string.IsNullOrEmpty(note)) return asked;
            var i = 0;
            while (i < note.Length && (char.IsDigit(note[i]) || note[i] == '.')) i++;
            return i > 0 && double.TryParse(note.Substring(0, i),
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : asked;
        }

        /// <summary>One command, and the sim's own words on the notice line. Never a reworded version of them.</summary>
        private void Send(Command c)
        {
            var sim = host == null ? null : host.Simulation;
            if (sim == null) return;
            var result = sim.Apply(c);
            if(result.Accepted)FindAnyObjectByType<FrontEnd.FrontEndBootstrap>()?.Dirty.MarkChanged();
            Say(result.Problem);
            _dirty = true;
            Paint(true);
        }

        private void Say(string text)
        {
            _notes = text ?? "";
            if (_notice != null) _notice.text = _notes;
        }

        // ---- tooltips (correction pass W-B R4, contract C5) ------------------------------------------------

        /// <summary>
        /// What one Backpack or storage slot is holding. Every number is the view-model's, which is the
        /// simulation's: the tooltip states stock, it never computes it.
        /// </summary>
        private TooltipContent SlotTooltip(SlotKind kind, int index)
        {
            var list = kind == SlotKind.Pack ? _model.Pack : kind == SlotKind.Tray ? _model.Tray : _model.Store;
            if (index < 0 || index >= list.Count) return null;
            var cell = list[index];
            var hint = kind == SlotKind.Tray ? _model.TrayHint : _model.StoreHint;
            if (cell.Empty)
                return new TooltipContent
                {
                    Title = kind == SlotKind.Pack ? "Empty slot" : cell.DisplayName,
                    Body = kind == SlotKind.Pack ? TransferText.CarriedNote : cell.Role+" · "+hint
                };

            if (kind == SlotKind.Tray)
                return new TooltipContent
                {
                    Title = cell.DisplayName,
                    Body = TransferText.StackDetail(cell.Count, cell.StackSize, false),
                    Rows = new List<(string label, string value, bool ok)>
                    {
                        ("Waiting", PackLayout.Num(Math.Floor(cell.Count)), true),
                        ("Tray", _model.TrayCapacity, !_model.TrayFull),
                    },
                    Footer = _model.TrayInReach ? TransferText.TakeIntoBackpack : WorkshopText.CollectAway
                };

            var pack = kind == SlotKind.Pack;
            var rows = new List<(string label, string value, bool ok)>
            {
                (pack ? "Carried" : "Stored", PackLayout.Num(Math.Floor(cell.Count)), true)
            };
            if (cell.StackSize > 1) rows.Add(("Stack size", PackLayout.Num(cell.StackSize), true));

            return new TooltipContent
            {
                Title = cell.DisplayName,
                Body = TransferText.StackDetail(cell.Count, cell.StackSize, pack),
                Rows = rows,
                Footer = pack ? TransferText.MoveToSlot + " · drag to move" : TransferText.TakeIntoBackpack
            };
        }

        /// <summary>
        /// The equipped weapon's real profile: damage, rate, magazine and the ammunition item it feeds on, all
        /// read out of <see cref="GameData"/> and the weapon state. Nothing here is a constant in this file.
        /// </summary>
        private TooltipContent EquipmentTooltip(int slot)
        {
            var label = TransferText.EquipSlot(slot, _model.EquippedNames[slot]);
            var key = slot >= 0 && slot < _model.Equipped.Length ? _model.Equipped[slot] : null;
            if (string.IsNullOrEmpty(key))
                return new TooltipContent { Title = label, Body = TransferText.EquipDropHint };

            var sim = host == null ? null : host.Simulation;
            var data = sim == null ? null : sim.Context.Data;
            var rows = new List<(string label, string value, bool ok)>();

            // A weapon in a pocket is keyed "rifle:3" — the profile is the part before the colon.
            var kind = key;
            var colon = kind.IndexOf(':');
            if (colon > 0) kind = kind.Substring(0, colon);

            var body = "";
            if (data != null && data.TryWeapon(kind, out var profile))
            {
                body = profile.DisplayName;
                rows.Add(("Damage", PackLayout.Num(profile.Damage), true));
                if (profile.RatePerS > 0) rows.Add(("Rate", PackLayout.Num(profile.RatePerS) + " /s", true));
                if (profile.Capacity > 0) rows.Add(("Magazine", PackLayout.Num(profile.Capacity), true));
                if (profile.ReloadSeconds > 0) rows.Add(("Reload", PackLayout.Num(profile.ReloadSeconds) + " s", true));

                var ammo = WeaponRules.Ammo(data);
                if (ammo != null) rows.Add(("Ammunition", data.Item(ammo.Item).DisplayName, true));
            }

            if (_model.ActiveSlot == slot && _model.HasWeapon)
            {
                rows.Add(("Loaded", PackLayout.Num(_model.Loaded) + " / " + PackLayout.Num(_model.MagazineCapacity),
                    _model.Loaded > 0));
                rows.Add(("Spare rounds", PackLayout.Num(_model.Reserve), _model.Reserve > 0));
            }

            return new TooltipContent
            {
                Title = string.IsNullOrEmpty(_model.EquippedNames[slot]) ? label : _model.EquippedNames[slot],
                Body = body,
                Rows = rows,
                Footer = _model.ActiveSlot == slot ? "In hand" : "Carried on the strip"
            };
        }

        /// <summary>What the action-bar key selects, named the way the catalogue names it.</summary>
        private TooltipContent QuickTooltip(int slot)
        {
            var bar = _model.Bar;
            var key = slot >= 0 && slot < bar.Count ? bar[slot] : "";
            var stroke = "Key " + (slot == QuickSlots - 1 ? 0 : slot + 1);
            if (string.IsNullOrEmpty(key))
                return new TooltipContent { Title = stroke, Body = "Empty. Drag a tool here, or use Build ▸ Add to action bar." };

            var sim = host == null ? null : host.Simulation;
            var data = sim == null ? null : sim.Context.Data;
            var name = key;
            if (data != null)
            {
                if (data.TryMachine(key, out var spec)) name = spec.DisplayName;
                else if (new ItemKey(key).IsItem(out var id)) name = data.Item(id).DisplayName;
                else
                {
                    var kind = key;
                    var colon = kind.IndexOf(':');
                    if (colon > 0) kind = kind.Substring(0, colon);
                    if (data.TryWeapon(kind, out var weapon)) name = weapon.DisplayName;
                }
            }
            return new TooltipContent { Title = name, Body = stroke + " selects this." };
        }

        // ---- tiny element helpers -------------------------------------------------------------------------

        private static void Show(VisualElement e, bool on)
        {
            if (e == null) return;
            if (on) e.RemoveFromClassList("hidden");
            else e.AddToClassList("hidden");
        }

        /// <summary>The inverse of <see cref="Show"/>: is this element currently not hidden?</summary>
        private static bool Showing(VisualElement e) => e != null && !e.ClassListContains("hidden");

        private static void SetClass(VisualElement e, string cls, bool on)
        {
            if (e == null) return;
            if (on) e.AddToClassList(cls);
            else e.RemoveFromClassList(cls);
        }
    }
}
