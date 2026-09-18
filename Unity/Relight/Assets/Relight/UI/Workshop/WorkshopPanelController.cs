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
    /// C-06. The Home workshop section of the Backpack drawer (reference inventoryPanel.ts <c>coreCard</c> and
    /// <c>recipeCard</c>; UI_AND_ONBOARDING.md §5.1 item 1 and §6).
    ///
    /// Three rules, all of them load-bearing:
    /// <list type="bullet">
    /// <item><b>The core card is first.</b> At the opening the player's whole reason to be at the workshop is the
    ///       damaged core, so it is card one, every time, whatever the recipe list says (GP-HOME-REPAIR).</item>
    /// <item><b>Recipe cards come from the data.</b> One card per <see cref="GameData.Recipes"/> row whose
    ///       <c>Station</c> is <see cref="Station"/> — today "Hand bullet batch" and "Rifle". Adding a workshop
    ///       recipe to the catalogue adds a card with no code change.</item>
    /// <item><b>No equipment block.</b> U-D-11 / GP-PLAYTEST-FIX 3 removed it; the two equipped slots live in the
    ///       equipment strip under the Backpack grid (§5.6) and must not be duplicated here.</item>
    /// </list>
    ///
    /// The controller never mutates a count. Every button calls <see cref="Simulation.Apply"/> — not
    /// <see cref="SimHost.Submit"/> — because <c>Apply</c> is the port of the reference's <c>dispatch</c>: it
    /// flushes pending commands, runs the command even while paused, and RETURNS the
    /// <see cref="CommandResult"/> whose <c>Problem</c> this panel then shows verbatim. <c>Submit</c> only queues
    /// and returns void, so a refusal would be silent.
    ///
    /// A refused Craft is DISABLED with the reason on the card, never hidden — the same rule the Load screen
    /// follows for a mismatched save. Cancel is the one control that is hidden when there is nothing to cancel.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Relight/Workshop Panel Controller")]
    public sealed class WorkshopPanelController : MonoBehaviour
    {
        /// <summary>The catalogue station this section shows (<c>Recipe.Station</c>).</summary>
        public const string Station = "Home workshop";

        /// <summary>Catalogue keys the controller wires to a specific command pair.</summary>
        public const string HandBulletsKey = "hand-bullets";
        public const string RifleKey = "rifle";

        [Tooltip("The document holding InventoryPanel.uxml (which instances WorkshopPanel.uxml). Found on this object if left empty.")]
        [SerializeField] private UIDocument document;

        [Tooltip("The simulation this section reads and sends commands to. Found in the scene if left empty.")]
        [SerializeField] private SimHost host;

        [Tooltip("RecipeCard.uxml. One instance per card, core card first.")]
        [SerializeField] private VisualTreeAsset recipeCard;

        private VisualElement _root, _cards, _output;
        private Label _heading, _notice, _outputText, _outputHint;
        private Button _collect;
        private readonly List<Card> _built = new List<Card>();
        private string _notes = "";
        private double _noticeUntil;
        private int _noticeSession = -1;
        private double _lastRefresh = double.NegativeInfinity;
        private bool _wasShown;

        /// <summary>One built card and the elements inside it. Recipe is null for the core card.</summary>
        private sealed class Card
        {
            public Recipe Recipe;              // null => the Base core card
            public VisualElement Root, Chips, ProgressFill;
            public Label Title, Detail, Note;
            public Button Craft, Cancel;
            public Action OnCraft, OnCancel;
        }

        /// <summary>The last line the workshop said — the sim's words, or the panel's own. For tests.</summary>
        public string Notice => _notes;

        /// <summary>How many cards are built, core card included. For tests.</summary>
        public int CardCount => _built.Count;

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
        }

        private void Start()
        {
            if (_cards == null) Bind();   // UIDocument may build its root after this component enabled
        }

        /// <summary>
        /// The reference's <c>open(id)</c> calls the adapter's <c>enter()</c> before <c>sync()</c> (uiShell.ts:63-70),
        /// so a drawer paints the frame it opens. Here the shell only flips the panel's display, so the first
        /// frame the workshop is shown is painted unthrottled: the card the player opened the drawer for can
        /// never be a 150 ms-old frame (found by U-6 in Play Mode: a freshly damaged core still read "Intact").
        /// </summary>
        private void Update()
        {
            if (host != null && (_noticeSession != host.Session || Time.unscaledTimeAsDouble >= _noticeUntil))
            {
                _noticeSession = host.Session;
                _notes = "";
                if (_notice != null) _notice.text = "";
            }
            var shown = IsShown(_root);
            var force = shown && !_wasShown;
            _wasShown = shown;
            Paint(force);
        }

        /// <summary>Whether the element and every ancestor is displayed; the shell hides whole panels by display.</summary>
        private static bool IsShown(VisualElement e)
        {
            if (e == null || e.panel == null) return false;
            for (var v = e; v != null; v = v.parent)
                if (v.resolvedStyle.display == DisplayStyle.None) return false;
            return true;
        }

        private void OnTickBoundary(int ticks) => Paint(false);

        /// <summary>Re-query the document and rebuild the cards. Public so a hot-reloaded UXML can rebind.</summary>
        public void Bind()
        {
            var docRoot = document == null ? null : document.rootVisualElement;
            if (docRoot == null) return;
            _root = docRoot.Q<VisualElement>("workshop");
            if (_root == null) return;
            _heading = _root.Q<Label>("workshop-heading");
            _cards = _root.Q<VisualElement>("workshop-cards");
            _notice = _root.Q<Label>("workshop-notice");
            _output = _root.Q<VisualElement>("workshop-output");
            _outputText = _root.Q<Label>("workshop-output-text");
            _outputHint = _root.Q<Label>("workshop-output-hint");
            _collect = _root.Q<Button>("workshop-collect");
            if (_heading != null) _heading.text = WorkshopText.Heading;
            if (_collect != null)
            {
                // U-D-44: finished goods are a physical thing at the workshop. Collect is the only way they reach
                // the Backpack, and the sim refuses it out of reach — the button says so rather than going quiet.
                _collect.text = WorkshopText.Collect;
                _collect.clicked += () => Send(new CollectWorkshopCommand());
            }
            BuildCards();
            Paint(true);
        }

        // ---- building -------------------------------------------------------------------------------------

        private void BuildCards()
        {
            _built.Clear();
            if (_cards == null) return;
            _cards.Clear();
            if (recipeCard == null)
            {
                // No template assigned: say so on the panel rather than throwing, and leave the drawer usable.
                _notes = "Workshop cards are unavailable (RecipeCard.uxml is not assigned).";
                if (_notice != null) _notice.text = _notes;
                return;
            }

            // 1. The Base core, always first.
            var core = Instantiate("core");
            core.Root.AddToClassList("core-card");
            core.OnCraft = RepairCore;
            core.OnCancel = CancelRepair;
            Wire(core);
            _built.Add(core);

            // 2. One card per catalogue recipe made at this station, in catalogue order.
            var data = host == null || host.Simulation == null ? null : host.Simulation.Context.Data;
            if (data == null) return;
            var ordered = new List<Recipe>(data.Recipes);
            ordered.Sort((a,b)=>RecipeOrder(a.Key).CompareTo(RecipeOrder(b.Key)));
            for (var i = 0; i < ordered.Count; i++)
            {
                var r = ordered[i];
                if (!string.Equals(r.Station, Station, StringComparison.Ordinal)) continue;
                var card = Instantiate(r.Key);
                card.Recipe = r;
                if (string.Equals(r.Key, RifleKey, StringComparison.Ordinal))
                {
                    card.OnCraft = () => Send(new CraftRifleCommand());
                    card.OnCancel = () => Send(new CancelRifleCraftCommand());
                }
                else
                {
                    // Recipe keys select the actual simulation inputs, output and duration.
                    card.OnCraft = () => Send(new HandCraftCommand(1, r.Key));
                    // A card's Cancel drops that recipe's queued batches only; other recipes keep processing.
                    card.OnCancel = () => Send(new CancelCraftCommand(0, r.Key));
                }
                Wire(card);
                _built.Add(card);
            }
        }

        private static int RecipeOrder(string key) => key=="hand-steel"?0:key=="hand-copper"?1:key=="rifle"?2:3;

        private Card Instantiate(string key)
        {
            var instance = recipeCard.Instantiate();
            // Instantiate() wraps the template in a TemplateContainer; the container must not eat the layout.
            instance.AddToClassList("recipe-host");
            _cards.Add(instance);
            var root = instance.Q<VisualElement>("recipe-card") ?? instance;
            root.name = "card-" + key;
            return new Card
            {
                Root = root,
                Title = root.Q<Label>("card-title"),
                Detail = root.Q<Label>("card-detail"),
                Note = root.Q<Label>("card-note"),
                Chips = root.Q<VisualElement>("card-chips"),
                ProgressFill = root.Q<VisualElement>("card-progress-fill"),
                Craft = root.Q<Button>("card-craft"),
                Cancel = root.Q<Button>("card-cancel"),
            };
        }

        private void Wire(Card c)
        {
            var iconKey = c.Recipe == null ? "core1" : c.Recipe.Key == RifleKey ? "rifle" : c.Recipe.Outputs.Count > 0 ? Items.Key(c.Recipe.Outputs[0].Item) : "";
            ItemIcons.Paint(c.Root.Q("card-output-icon"), c.Root.Q<Label>("card-output-icon-code"), iconKey);
            var count = c.Root.Q<Label>("card-output-count");
            if(count != null) count.text = c.Recipe == null ? "Core" : c.Recipe.Outputs.Count > 0 ? PackLayout.Num(c.Recipe.Outputs[0].Count) : "1";
            if (c.Craft != null) { c.Craft.text = WorkshopText.Craft; c.Craft.clicked += () => c.OnCraft?.Invoke(); }
            if (c.Cancel != null) { c.Cancel.text = WorkshopText.Cancel; c.Cancel.clicked += () => c.OnCancel?.Invoke(); }

            if(c.Recipe!=null && c.Recipe.Outputs.Count>0)
            {
                var five=new Button(()=>Send(new HandCraftCommand(5,c.Recipe.Key))){name="craft-five",text="Queue 5 batches"};
                five.AddToClassList("ui-button");five.AddToClassList("card-button");
                c.Root.Q("card-buttons")?.Add(five);
            }

            // Contract C5: ONE tooltip per card, attached once here rather than per chip. Chips() rebuilds its
            // labels on every repaint, so attaching there would register a new provider every 150 ms; the card root
            // is built once and outlives them. The provider runs at hover time, so the ledger is read from the live
            // simulation and can never be stale.
            if (c.Root != null) Tooltips.Attach(c.Root, () => CardTooltip(c));
        }

        /// <summary>
        /// The recipe card's tooltip: what it makes, how long it takes, and one have/need row per ingredient with
        /// the shortfall flagged. Every number comes from <see cref="GameData"/> and the live engineer inventory —
        /// nothing here is written down twice.
        /// </summary>
        private TooltipContent CardTooltip(Card c)
        {
            var content = new TooltipContent
            {
                Title = c.Title == null ? "" : c.Title.text,
                Body = c.Detail == null ? "" : c.Detail.text,
                Footer = c.Note == null ? "" : c.Note.text,
            };

            var sim = host == null ? null : host.Simulation;
            if (sim == null) return content;
            var ctx = sim.Context;
            var st = sim.State;
            if (ctx == null || st == null) return content;

            var inputs = c.Recipe != null ? Inputs(c.Recipe) : CoreInputs(ctx, st);
            var rows = new List<(string label, string value, bool ok)>(inputs.Length);
            for (var i = 0; i < inputs.Length; i++)
            {
                var item = inputs[i].Item1;
                var need = inputs[i].Item2;
                var have = st.Engineer.Inv[item];
                rows.Add((ctx.Data.Item(item).DisplayName, WorkshopText.Chip(have, need), have >= need));
            }
            content.Rows = rows;
            return content;
        }

        /// <summary>The core card has no recipe: its bill is the repair cost the sim quotes right now.</summary>
        private static (ItemId, double)[] CoreInputs(SimContext ctx, SimState st)
        {
            var v = HomeQueries.RepairCard(st, ctx.Data);
            return new (ItemId, double)[] { (ItemId.Steel, v.Steel), (ItemId.Copper, v.Copper) };
        }

        // ---- commands -------------------------------------------------------------------------------------

        private void RepairCore()
        {
            // W-A's C-05 command. RepairKinds.Core takes no id, so -1 is the documented "no machine" value.
            Send(new RepairCommand(RepairKinds.Core, -1), WorkshopText.RepairStarted);
        }

        private void CancelRepair() => Send(new CancelRepairCommand());

        /// <summary>
        /// One button, one command, and the sim's own answer on the card. <paramref name="okText"/> is used only
        /// when the sim accepted without saying anything itself — the sim's note always wins.
        /// </summary>
        private void Send(Command c, string okText = "")
        {
            var sim = host == null ? null : host.Simulation;
            if (sim == null) return;
            var result = sim.Apply(c);
            if(result.Accepted) FindAnyObjectByType<FrontEnd.FrontEndBootstrap>()?.Dirty.MarkChanged();
            _notes = !string.IsNullOrEmpty(result.Problem) ? result.Problem
                   : (result.Accepted ? okText : "");
            _noticeUntil = Time.unscaledTimeAsDouble + 5;
            _noticeSession = host.Session;
            if (_notice != null) _notice.text = _notes;
            Paint(true);
        }

        // ---- painting -------------------------------------------------------------------------------------

        /// <summary>Repaint on the shared 150 ms throttle (hud.ts:47), or now when <paramref name="force"/>.</summary>
        public void Paint(bool force)
        {
            if (_cards == null || _built.Count == 0) return;
            var now = Time.unscaledTimeAsDouble;
            if (!force && now - _lastRefresh < InventoryViewModel.RefreshSeconds) return;
            _lastRefresh = now;

            var sim = host == null ? null : host.Simulation;
            if (sim == null) return;
            var ctx = sim.Context;
            var st = sim.State;

            for (var i = 0; i < _built.Count; i++)
            {
                var card = _built[i];
                if (card.Recipe == null) PaintCore(ctx, st, card);
                else if (string.Equals(card.Recipe.Key, RifleKey, StringComparison.Ordinal)) PaintRifle(ctx, st, card);
                else PaintHand(ctx, st, card);
            }

            PaintOutput(ctx, st);
        }

        /// <summary>
        /// The output tray row (U-D-44). The tray is the workshop's own storage: a finished batch lands here, not
        /// in the Backpack, and stays until the engineer walks over and collects it. When it fills, the running
        /// batch HOLDS at full progress — nothing is consumed and nothing is made twice — so the row says so in
        /// the danger colour and Collect is the way out.
        ///
        /// GP-UX-2 moved the CONTENTS out of this row and into a slot grid underneath, built by
        /// <see cref="Relight.UI.InventoryPanelController"/> from the same code as the Backpack. This controller
        /// still owns the row itself: how full the tray is, why Collect is greyed, and the danger colour when the
        /// tray has stalled a batch. Nothing writes the grid from here, and nothing writes this row from there.
        /// </summary>
        private void PaintOutput(SimContext ctx, SimState st)
        {
            if (_output == null) return;
            var h = st?.Hand;
            if (h == null) { Show(_output, false); return; }
            Show(_output, true);

            var used = HandCraft.TrayUsed(ctx.Data, h.Output);
            var inReach = HandCraft.NearDepot(ctx, st) && !st.Engineer.IsDown;
            var line = used == 0 ? WorkshopText.OutputEmpty : WorkshopText.OutputStacksLine(used, HandCraft.OutputStacks);
            // Disabled controls say why on the panel rather than going quiet — the same rule the cards follow.
            if (used > 0 && !inReach) line += " · " + WorkshopText.CollectAway;
            Set(_outputText, line);
            Set(_outputHint, used == 0 ? "" : WorkshopText.OutputGridHint);
            SetClass(_output, "tray-full", h.OutputFull);
            Enable(_collect, used > 0 && inReach);
        }

        private void PaintCore(SimContext ctx, SimState st, Card c)
        {
            var v = HomeQueries.RepairCard(st, ctx.Data);
            var repairHp = ctx.Data.Defence.RepairHp;

            Set(c.Title, WorkshopText.CoreCardTitle(v.Hp));
            SetClass(c.Root, "core-down", v.Hp <= 0);
            Set(c.Detail, WorkshopText.CoreHp(v.Hp, v.Max));

            Chips(c, new[]
            {
                (ItemId.Steel, (double)v.Steel),
                (ItemId.Copper, (double)v.Copper),
            }, st, ctx.Data);

            // The note, in priority order: what is running, then what is missing, then "Intact."
            var note = "";
            var danger = false;
            if (v.InProgress) note = WorkshopText.HandLockPrompt(Home.LockText);
            else if (v.Hp >= v.Max) note = WorkshopText.Intact;
            else if (!v.CanAfford) { note = WorkshopText.CoreShortfall(v.Steel, v.Copper); danger = true; }
            Set(c.Note, note);
            SetClass(c.Note, "shortfall", danger);
            SetClass(c.Note, "working", v.InProgress);

            Progress(c, v.InProgress && v.Seconds > 0 ? 1.0 - v.RemainingS / v.Seconds : 0);

            Set(c.Craft, WorkshopText.CoreButton(v.InProgress, v.RemainingS, v.Recommission, v.Seconds, repairHp));
            Enable(c.Craft, !v.InProgress && v.Hp < v.Max && v.CanAfford);
            Show(c.Cancel, v.InProgress);
        }

        private void PaintHand(SimContext ctx, SimState st, Card c)
        {
            var r = c.Recipe;
            var h = st.Hand;
            var outName = OutputName(ctx, r);
            var outCount = r.Outputs.Count > 0 ? r.Outputs[0].Count : 0;
            Set(c.Title, WorkshopText.RecipeTitle(outName, outCount));
            Set(c.Detail, WorkshopText.Duration(r.Seconds));
            Chips(c, Inputs(r), st, ctx.Data);

            var (room, reason) = HandCraft.Check(ctx, st, r.Key);
            // U-D-44: one persistent queue belongs to the workshop. `QueueOf` reports this recipe's share of it —
            // how many batches are waiting, whether one of them is the batch being processed now, and its progress.
            var (batches, running, progress) = HandCraft.QueueOf(st, r.Key);
            var note = "";
            var danger = false;
            var working = false;
            if (running && h.OutputFull) { note = WorkshopText.FinishedNoRoom; danger = true; }
            else if (running)
            {
                note = WorkshopText.Progress(r.Seconds, progress, batches) + " · " + WorkshopText.CancelRefunds;
                working = true;
            }
            else if (batches > 0) { note = WorkshopText.QueuedBatches(batches); working = true; }
            else if (reason.Length > 0) { note = reason; danger = true; }
            else note = r.Key.StartsWith("hand-") && r.Key!="hand-bullets"
                ? "No electricity needed. Automate this recipe in a Foundry."
                : WorkshopText.UsesCarried(r.Seconds);
            Set(c.Note, note);
            SetClass(c.Note, "shortfall", danger);
            SetClass(c.Note, "working", working);

            Progress(c, running && r.Seconds > 0 ? progress / r.Seconds : 0);

            Set(c.Craft, WorkshopText.Craft);
            Enable(c.Craft, room > 0);
            Show(c.Cancel, batches > 0);
            c.Root.Q<Button>("craft-five")?.SetEnabled(room>0);
        }

        private void PaintRifle(SimContext ctx, SimState st, Card c)
        {
            var r = c.Recipe;
            var w = st.Weapons;
            // The rifle's product is a weapon INSTANCE, so the catalogue row has no output stack: the card is
            // titled from the recipe's own display name and always makes one.
            Set(c.Title, WorkshopText.RecipeTitle(r.DisplayName, 1));
            Set(c.Detail, WorkshopText.Duration(r.Seconds));
            Chips(c, Inputs(r), st, ctx.Data);

            var crafting = w != null && w.Crafting;
            var reason = WeaponRules.CheckCraft(ctx, st);
            var note = "";
            var danger = false;
            // The rifle's product is a weapon instance, so it keeps its own single-item slot rather than joining the
            // batch queue — but under U-D-44 it too runs unattended and waits in the workshop's output tray.
            if (crafting)
            {
                note = WorkshopText.Progress(r.Seconds, w.CraftSeconds, 1);
                if (w.CraftSeconds >= r.Seconds - 1e-6 && st.Hand != null && st.Hand.Output.IsEmpty == false
                    && HandCraft.TrayFree(ctx.Data, st.Hand.Output) <= 0)
                { note = WorkshopText.FinishedNoRoom; danger = true; }
            }
            else if (reason.Length > 0) { note = reason; danger = true; }
            else note = WorkshopText.UsesCarried(r.Seconds);
            Set(c.Note, note);
            SetClass(c.Note, "shortfall", danger);
            SetClass(c.Note, "working", crafting);

            Progress(c, crafting && r.Seconds > 0 ? w.CraftSeconds / r.Seconds : 0);

            Set(c.Craft, WorkshopText.Craft);
            Enable(c.Craft, reason.Length == 0);
            Show(c.Cancel, crafting);
        }

        // ---- small helpers --------------------------------------------------------------------------------

        private static (ItemId, double)[] Inputs(Recipe r)
        {
            var a = new (ItemId, double)[r.Inputs.Count];
            for (var i = 0; i < r.Inputs.Count; i++) a[i] = (r.Inputs[i].Item, r.Inputs[i].Count);
            return a;
        }

        private static string OutputName(SimContext ctx, Recipe r)
            => r.Outputs.Count > 0 ? ctx.Data.Item(r.Outputs[0].Item).DisplayName : r.DisplayName;

        /// <summary>"2 /20" per ingredient, red when the Backpack is short. The names are in the card tooltip.</summary>
        private static void Chips(Card c, (ItemId item, double need)[] inputs, SimState st, GameData data)
        {
            if (c.Chips == null) return;
            // Keep ingredient elements stable while live counts repaint and the pointer crosses a card.
            if(c.Chips.childCount != inputs.Length) {
                c.Chips.Clear();
                for(int i=0;i<inputs.Length;i++) {
                    var chip=new VisualElement { pickingMode=PickingMode.Ignore };
                    chip.AddToClassList("ingredient");
                    chip.Add(ItemIcons.Make("ingredient-icon",out _));
                    var count=new Label { name="ingredient-count",pickingMode=PickingMode.Ignore };
                    count.AddToClassList("ingredient-count");chip.Add(count);c.Chips.Add(chip);
                }
            }
            for(int i=0;i<inputs.Length;i++) {
                var chip=c.Chips[i];var have=st.Engineer.Inv[inputs[i].item];var need=inputs[i].need;
                ItemIcons.Paint(chip.Q("ingredient-icon"),chip.Q<Label>("ingredient-icon-code"),Items.Key(inputs[i].item));
                chip.Q<Label>("ingredient-count").text=WorkshopText.Chip(have,need);
                chip.EnableInClassList("short",have<need);
            }
        }

        private static void Progress(Card c, double fraction)
        {
            if (c.ProgressFill == null) return;
            if (fraction < 0) fraction = 0;
            if (fraction > 1) fraction = 1;
            // The one style this controller sets: a bar's fill is a measured quantity, not a state a class can name.
            c.ProgressFill.style.width = Length.Percent((float)(fraction * 100.0));
        }

        private static void Set(Label l, string text) { if (l != null) l.text = text ?? ""; }
        private static void Set(Button b, string text) { if (b != null) b.text = text ?? ""; }
        private static void Enable(Button b, bool on) { if (b != null) b.SetEnabled(on); }

        private static void Show(VisualElement e, bool on)
        {
            if (e == null) return;
            if (on) e.RemoveFromClassList("hidden");
            else e.AddToClassList("hidden");
        }

        private static void SetClass(VisualElement e, string cls, bool on)
        {
            if (e == null) return;
            if (on) e.AddToClassList(cls);
            else e.RemoveFromClassList(cls);
        }
    }
}
