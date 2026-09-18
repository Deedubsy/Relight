using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Relight.Presentation;
using Relight.Sim;
using Relight.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Relight.Tests.Play.Ui
{
    /// <summary>
    /// R2's five drag facts, in the real scene, against the real simulation: repeated merges, a partial transfer
    /// by split, a drop on the last (only-by-scrolling) slot, a drag cancelled with Escape, and conservation over
    /// twenty random transfers.
    ///
    /// The transfers go through <see cref="InventoryPanelController.DragFrom"/> and
    /// <see cref="InventoryPanelController.DragTo"/> — the exact pair of callbacks
    /// <see cref="StackDragManipulator"/> invokes at the two ends of a gesture, with the drop target already
    /// resolved. Synthesising a full pointer sweep across a scrolled grid is not something this machine can
    /// verify; the Escape test does synthesise real pointer events, because the thing under test there IS the
    /// gesture rather than the transfer.
    ///
    /// Conservation is asserted over the WHOLE bag, key by key, not over the one item being moved: the defect
    /// class this guards against (a move that also credits or destroys) shows up in a neighbour as often as in the
    /// stack that was dragged.
    ///
    /// Written without the editor on this machine. It now COMPILES offline against the editor's managed
    /// assemblies, but Play Mode is unavailable here, so none of it has been RUN on this machine.
    /// </summary>
    public sealed class InventoryDragPlayTests
    {
        private const string PanelId = "inventory-panel";
        private const double Tolerance = 1e-6;

        private SimHost _host;
        private UiShell _shell;
        private InventoryPanelController _panel;
        private UIDocument _document;

        /// <summary>Load, pause, open the Backpack and paint it. Every test starts here.</summary>
        private IEnumerator Open()
        {
            yield return SceneFixture.LoadWorld();
            _host = Object.FindAnyObjectByType<SimHost>();
            Assert.That(_host, Is.Not.Null, "World.unity has no SimHost.");
            _host.Paused = true;                       // nothing else may move a count while the test moves one
            _shell = Object.FindAnyObjectByType<UiShell>();
            _panel = Object.FindAnyObjectByType<InventoryPanelController>();
            Assert.That(_shell, Is.Not.Null, "GameUI.unity has no UiShell.");
            Assert.That(_panel, Is.Not.Null, "GameUI.unity has no InventoryPanelController.");

            _document = UiFixture.DocumentWith(PanelId, out var element);
            Assert.That(element, Is.Not.Null, "no element named \"" + PanelId + "\".");

            _shell.Open(PanelId);
            yield return null;
            _panel.Paint(true);
            yield return null;
        }

        private SimState State => _host.Simulation.State;

        /// <summary>Every key the engineer carries, and how much. The conservation baseline.</summary>
        private Dictionary<string, double> Carried()
        {
            var keys = new List<ItemKey>();
            State.Engineer.Inv.Keys(keys);
            var bag = new Dictionary<string, double>(keys.Count);
            for (var i = 0; i < keys.Count; i++) bag[keys[i].Key] = State.Engineer.Inv[keys[i]];
            return bag;
        }

        private void AssertConserved(Dictionary<string, double> before, string what)
        {
            var after = Carried();
            foreach (var pair in before)
            {
                after.TryGetValue(pair.Key, out var now);
                Assert.That(now, Is.EqualTo(pair.Value).Within(Tolerance),
                    what + ": " + pair.Key + " went from " + pair.Value + " to " + now + ".");
            }
            foreach (var pair in after)
                Assert.That(before.ContainsKey(pair.Key), Is.True,
                    what + ": " + pair.Key + " appeared out of nothing (" + pair.Value + ").");
        }

        /// <summary>Repaint and hand back the fresh slot list.</summary>
        private IReadOnlyList<InventoryViewModel.Cell> Slots()
        {
            _panel.Paint(true);
            return _panel.Model.Pack;
        }

        private static int FirstFilled(IReadOnlyList<InventoryViewModel.Cell> cells)
        {
            for (var i = 0; i < cells.Count; i++) if (!cells[i].Empty && !cells[i].IsWeapon) return i;
            return -1;
        }

        private static int FirstEmpty(IReadOnlyList<InventoryViewModel.Cell> cells)
        {
            for (var i = 0; i < cells.Count; i++) if (cells[i].Empty) return i;
            return -1;
        }

        // ----------------------------------------------------------------------------------------------------

        /// <summary>
        /// §5.8: dropping stack after stack onto one slot merges them and never invents or loses a unit — the
        /// overfill defect the reference's <c>mergeInto</c> guarded against.
        /// </summary>
        [UnityTest, Timeout(60000)]
        public IEnumerator RepeatedMergesOntoOneSlotConserveTheCount()
        {
            yield return Open();

            // Sort first so identical items sit in adjacent slots and there is certain to be something to merge.
            _host.Simulation.Apply(new InventorySortCommand());
            var cells = Slots();

            var target = -1;
            var donors = new List<int>();
            for (var i = 0; i < cells.Count && donors.Count < 3; i++)
            {
                if (cells[i].Empty || cells[i].IsWeapon) continue;
                if (target < 0) { target = i; continue; }
                if (string.Equals(cells[i].Item, cells[target].Item, System.StringComparison.Ordinal)) donors.Add(i);
            }
            if (target < 0 || donors.Count == 0)
                Assert.Ignore("this save carries no item split across two Backpack slots, so there is nothing to merge.");

            var item = cells[target].Item;
            var stackSize = cells[target].StackSize;
            var before = Carried();

            foreach (var donor in donors)
            {
                var payload = _panel.DragFrom(InventoryPanelController.SlotKind.Pack, donor);
                if (payload == null) continue;
                _panel.DragTo(InventoryPanelController.SlotKind.Pack, target, payload);
                yield return null;
                var after = Slots();
                AssertConserved(before, "merging slot " + donor + " into " + target);
                for (var i = 0; i < after.Count; i++)
                    Assert.That(after[i].Count, Is.LessThanOrEqualTo(after[i].StackSize + Tolerance),
                        "slot " + i + " holds " + after[i].Count + " of a stack that caps at " + after[i].StackSize + ".");
            }

            var end = Slots();
            Assert.That(end[target].Item, Is.EqualTo(item), "the merge target changed item.");
            Assert.That(end[target].Count, Is.LessThanOrEqualTo(stackSize + Tolerance), "the target overfilled.");
        }

        /// <summary>
        /// A partial transfer: the split the drawer's "Split" button sends. Exactly half moves, the rest stays,
        /// and the engineer carries neither more nor less than before.
        /// </summary>
        [UnityTest, Timeout(60000)]
        public IEnumerator ASplitMovesExactlyTheRequestedHalfAndConservesTheTotal()
        {
            yield return Open();
            _host.Simulation.Apply(new InventorySortCommand());
            var cells = Slots();

            var from = -1;
            for (var i = 0; i < cells.Count; i++)
                if (!cells[i].Empty && !cells[i].IsWeapon && cells[i].Count >= 2) { from = i; break; }
            var empty = FirstEmpty(cells);
            if (from < 0 || empty < 0)
                Assert.Ignore("this save has no stack of two or more, or no free slot, so a split cannot be made.");

            var item = cells[from].Item;
            var count = cells[from].Count;
            var half = (int)System.Math.Floor(count / 2.0);
            var before = Carried();

            var result = _host.Simulation.Apply(
                new InventorySplitCommand(from, empty, item, count, _panel.Model.Layout, half));
            Assert.That(result.Accepted, Is.True, "the sim refused the split: " + result.Problem);
            yield return null;

            var after = Slots();
            Assert.That(after[empty].Item, Is.EqualTo(item), "the split half did not land in slot " + empty + ".");
            Assert.That(after[empty].Count, Is.EqualTo((double)half).Within(Tolerance), "the wrong amount moved.");
            Assert.That(after[from].Count, Is.EqualTo(count - half).Within(Tolerance), "the remainder is wrong.");
            AssertConserved(before, "a split");
        }

        /// <summary>
        /// §5.3 defect U-1: the last slot, the one only reachable by scrolling the grid, used to refuse drops.
        /// </summary>
        [UnityTest, Timeout(60000)]
        public IEnumerator ADropOnTheLastSlotLandsThere()
        {
            yield return Open();
            _host.Simulation.Apply(new InventorySortCommand());
            var cells = Slots();
            Assert.That(cells.Count, Is.GreaterThan(1), "the Backpack has fewer than two slots.");

            var last = cells.Count - 1;
            var from = FirstFilled(cells);
            if (from < 0) Assert.Ignore("the Backpack is empty, so there is nothing to drop.");
            if (from == last) Assert.Ignore("the only stack already sits in the last slot.");

            var item = cells[from].Item;
            var count = cells[from].Count;
            var before = Carried();

            var payload = _panel.DragFrom(InventoryPanelController.SlotKind.Pack, from);
            Assert.That(payload, Is.Not.Null, "slot " + from + " holds something but produced no drag payload.");
            _panel.DragTo(InventoryPanelController.SlotKind.Pack, last, payload);
            yield return null;

            var after = Slots();
            Assert.That(after[last].Item, Is.EqualTo(item),
                "the drop on the last slot did not land (" + _panel.Notice + ").");
            Assert.That(after[last].Count, Is.EqualTo(count).Within(Tolerance), "the amount changed on the way.");
            AssertConserved(before, "a drop on the last slot");
        }

        /// <summary>
        /// C4 order 100: Escape in the middle of a gesture cancels the drag and nothing else. This one drives real
        /// pointer events, because the manipulator's own state machine is what is being tested.
        /// </summary>
        [UnityTest, Timeout(60000)]
        public IEnumerator EscapeDuringADragLeavesEveryCountUnchanged()
        {
            yield return Open();
            _host.Simulation.Apply(new InventorySortCommand());
            var cells = Slots();
            var from = FirstFilled(cells);
            if (from < 0) Assert.Ignore("the Backpack is empty, so no drag can start.");

            var slot = _document.rootVisualElement.Q<Button>("pack-" + from);
            Assert.That(slot, Is.Not.Null, "no #pack-" + from + " button in the drawer.");
            Assert.That(UiFixture.Shown(slot), Is.True, "slot " + from + " is not on screen.");

            var before = Carried();
            var startCount = cells[from].Count;
            var startItem = cells[from].Item;

            var at = UiFixture.Centre(slot);
            UiFixture.Down(slot, at);
            // Well past StackDragManipulator.ThresholdPx, so the gesture is a drag and not a click.
            UiFixture.Move(slot, at + new Vector2(StackDragManipulator.ThresholdPx * 4f, 0f));
            yield return null;
            Assert.That(UiDrag.Dragging, Is.True,
                "pressing and moving " + (StackDragManipulator.ThresholdPx * 4f) + " px on slot " + from
                + " did not start a drag.");

            Assert.That(new CancelDragLink().OnEscape(), Is.True, "Escape did not claim the live drag.");
            yield return null;

            Assert.That(UiDrag.Dragging, Is.False, "the drag survived Escape.");
            var after = Slots();
            Assert.That(after[from].Item, Is.EqualTo(startItem), "the cancelled drag moved the stack.");
            Assert.That(after[from].Count, Is.EqualTo(startCount).Within(Tolerance), "the cancelled drag changed a count.");
            AssertConserved(before, "a cancelled drag");
        }

        /// <summary>
        /// §5.8 conservation: twenty transfers chosen by a fixed seed, and the bag at the end is the bag at the
        /// start. Fixed seed so a failure is reproducible rather than folklore.
        /// </summary>
        [UnityTest, Timeout(120000)]
        public IEnumerator TwentyRandomTransfersConserveEveryItem()
        {
            yield return Open();
            _host.Simulation.Apply(new InventorySortCommand());
            var cells = Slots();
            if (FirstFilled(cells) < 0) Assert.Ignore("the Backpack is empty, so there is nothing to shuffle.");

            var before = Carried();
            var rng = new System.Random(20260914);
            var moved = 0;

            var filled = new List<int>();
            for (var attempt = 0; attempt < 200 && moved < 20; attempt++)
            {
                cells = _panel.Model.Pack;
                // Pick the source among the slots that hold something: a fresh Backpack is mostly empty, and a
                // uniformly random source made twenty transfers a matter of luck (12 of 20 on the Phase C save).
                filled.Clear();
                for (var i = 0; i < cells.Count; i++) if (!cells[i].Empty && !cells[i].IsWeapon) filled.Add(i);
                if (filled.Count == 0) break;
                var from = filled[rng.Next(filled.Count)];
                var to = rng.Next(cells.Count);
                if (from == to) continue;
                var payload = _panel.DragFrom(InventoryPanelController.SlotKind.Pack, from);
                if (payload == null) continue;              // an empty slot carries nothing: not a transfer
                _panel.DragTo(InventoryPanelController.SlotKind.Pack, to, payload);
                moved++;
                _panel.Paint(true);
                if ((moved & 3) == 0) yield return null;     // let a frame pass every few moves
                AssertConserved(before, "transfer " + moved + " (" + from + " -> " + to + ")");
            }

            Assert.That(moved, Is.EqualTo(20), "only " + moved + " transfers could be made in 200 attempts.");
            AssertConserved(before, "twenty random transfers");
        }

        // ---- GP-UX-2: the Home workshop's output tray is an inventory -------------------------------------------

        /// <summary>Put the engineer where the workshop can be reached, and two items in its output tray.</summary>
        private void StageTray(double steel, double copper)
        {
            var st = State;
            var (x, y, w, h) = HomeQueries.CoreRect(st);
            if (w > 0) st.Engineer.Pos = new Vec2(x + w / 2.0, y + h / 2.0);
            st.Hand.Output[ItemId.Steel] = st.Hand.Output[ItemId.Steel] + steel;
            st.Hand.Output[ItemId.Copper] = st.Hand.Output[ItemId.Copper] + copper;
        }

        /// <summary>Everything waiting in the tray, key by key — the baseline for "the rest was left alone".</summary>
        private Dictionary<string, double> InTray()
        {
            var keys = new List<ItemKey>();
            State.Hand.Output.Keys(keys);
            var bag = new Dictionary<string, double>(keys.Count);
            for (var i = 0; i < keys.Count; i++) bag[keys[i].Key] = State.Hand.Output[keys[i]];
            return bag;
        }

        private static int TraySlotOf(IReadOnlyList<InventoryViewModel.Cell> tray, string item)
        {
            for (var i = 0; i < tray.Count; i++)
                if (!tray[i].Empty && string.Equals(tray[i].Item, item, System.StringComparison.Ordinal)) return i;
            return -1;
        }

        /// <summary>
        /// The owner's report: "everything that can create an item needs to have an inventory — eg the Home
        /// workshop just has a button to collect created items instead of an inventory." Processors and miners
        /// already opened as stores; the hand-craft tray was the one producer with nothing but a Collect button, so
        /// the only thing a player could do with a finished batch was take the WHOLE tray or nothing.
        ///
        /// What is asserted here is the thing the grid is for: <b>one stack</b> leaves, and everything else in the
        /// tray stays exactly where it was. The tray has no machine id — it is the engineer's own hand-craft
        /// state — so the drop is served by <c>CollectWorkshopCommand(item, count)</c> rather than by the
        /// store transfer, and this drives the same <c>DragFrom</c>/<c>DragTo</c> pair the manipulator calls.
        ///
        /// Compiled offline against Unity's managed assemblies; Play Mode is not available on this machine, so it
        /// has not been RUN here.
        /// </summary>
        [UnityTest, Timeout(60000)]
        public IEnumerator OneStackDraggedOutOfTheWorkshopTrayLeavesTheRestOfTheTrayAlone()
        {
            yield return Open();
            StageTray(200, 40);
            _panel.Paint(true);
            yield return null;

            if (!_panel.Model.TrayInReach)
                Assert.Ignore("this save has no Home workshop within reach; the tray cannot be emptied from here.");
            var tray = _panel.Model.Tray;
            Assert.That(tray.Count, Is.EqualTo(HandCraft.OutputStacks),
                "the tray should present a fixed grid of " + HandCraft.OutputStacks + " slots.");

            var steel = Items.Key(ItemId.Steel);
            var slot = TraySlotOf(tray, steel);
            Assert.That(slot, Is.GreaterThanOrEqualTo(0), "the staged steel is not in the tray grid.");

            var empty = FirstEmpty(_panel.Model.Pack);
            if (empty < 0) Assert.Ignore("the Backpack is full; there is nowhere for the stack to land.");

            var carriedBefore = Carried();
            var trayBefore = InTray();
            var payload = _panel.DragFrom(InventoryPanelController.SlotKind.Tray, slot);
            Assert.That(payload, Is.Not.Null, "a filled tray slot handed back no stack.");
            Assert.That(payload.Tray, Is.True, "the stack was picked up from the tray but is not marked as tray stock.");
            Assert.That(payload.Item, Is.EqualTo(steel));
            var taken = System.Math.Floor(payload.Count);
            Assert.That(taken, Is.GreaterThan(0));

            _panel.DragTo(InventoryPanelController.SlotKind.Pack, empty, payload);
            yield return null;
            _panel.Paint(true);

            carriedBefore.TryGetValue(steel, out var carried);
            Assert.That(State.Engineer.Inv[ItemId.Steel], Is.EqualTo(carried + taken).Within(Tolerance),
                "the Backpack did not gain exactly the one stack that was dragged out.");

            var trayAfter = InTray();
            foreach (var pair in trayBefore)
            {
                trayAfter.TryGetValue(pair.Key, out var now);
                var expected = string.Equals(pair.Key, steel, System.StringComparison.Ordinal)
                    ? pair.Value - taken : pair.Value;
                Assert.That(now, Is.EqualTo(expected).Within(Tolerance),
                    "taking one stack changed " + pair.Key + " in the tray: " + pair.Value + " -> " + now + ".");
            }
            foreach (var pair in trayAfter)
                Assert.That(trayBefore.ContainsKey(pair.Key), Is.True,
                    pair.Key + " appeared in the tray out of nothing (" + pair.Value + ").");
        }

        /// <summary>
        /// The other half of making the tray a grid: it is an OUTPUT, so nothing may be put back into it. A grid
        /// that accepted drops would be a second, invisible storage the sim has no command to empty — the
        /// stack would have to go somewhere, and the only thing <c>HandState.Output</c> means is "finished work
        /// waiting to be picked up". The refusal is asserted at the ends of the real gesture, and the check is
        /// that nothing moved in EITHER direction.
        /// </summary>
        [UnityTest, Timeout(60000)]
        public IEnumerator AStackDroppedOntoTheWorkshopTrayIsRefusedAndNothingMoves()
        {
            yield return Open();
            StageTray(200, 40);
            _host.Simulation.Apply(new InventorySortCommand());
            _panel.Paint(true);
            yield return null;

            var from = FirstFilled(_panel.Model.Pack);
            if (from < 0) Assert.Ignore("the Backpack carries nothing that could be dropped onto the tray.");

            var carriedBefore = Carried();
            var trayBefore = InTray();
            var payload = _panel.DragFrom(InventoryPanelController.SlotKind.Pack, from);
            Assert.That(payload, Is.Not.Null);
            Assert.That(payload.Tray, Is.False, "a Backpack stack must not claim to be tray stock.");

            _panel.DragTo(InventoryPanelController.SlotKind.Tray, 0, payload);
            yield return null;
            _panel.Paint(true);

            AssertConserved(carriedBefore, "dropping a Backpack stack onto the output tray");
            var trayAfter = InTray();
            foreach (var pair in trayBefore)
            {
                trayAfter.TryGetValue(pair.Key, out var now);
                Assert.That(now, Is.EqualTo(pair.Value).Within(Tolerance),
                    "the refused drop still changed " + pair.Key + " in the tray.");
            }
            Assert.That(trayAfter.Count, Is.EqualTo(trayBefore.Count), "the refused drop added a key to the tray.");
        }

        // ---- GP-UX-9: the release point decides the slot ---------------------------------------------------------

        /// <summary>Every Backpack slot button that is currently laid out, by slot index.</summary>
        private List<Button> PackButtons()
        {
            var list = new List<Button>();
            var count = _panel.Model.Pack.Count;
            for (var i = 0; i < count; i++)
            {
                var b = _document.rootVisualElement.Q<Button>("pack-" + i);
                list.Add(b != null && b.worldBound.width > 0 && b.worldBound.height > 0 ? b : null);
            }
            return list;
        }

        /// <summary>The slot whose centre is closest to <paramref name="p"/> — what the drop should snap to.</summary>
        private static int NearestIndex(List<Button> buttons, Vector2 p)
        {
            var best = -1;
            var bestSq = float.MaxValue;
            for (var i = 0; i < buttons.Count; i++)
            {
                if (buttons[i] == null) continue;
                var d = (buttons[i].worldBound.center - p).sqrMagnitude;
                if (d >= bestSq) continue;
                bestSq = d;
                best = i;
            }
            return best;
        }

        /// <summary>
        /// The owner's report: "when you drag an item in your inventory it automatically sorts it into the first
        /// available slot instead of the slot hovered over".
        ///
        /// The sim was never the problem — <c>BackpackDropTests</c> now covers the hovered slot even in a
        /// never-arranged Backpack. What went wrong was upstream of it: the drop target came from
        /// <c>panel.Pick</c> alone, and <c>#pack-grid</c> is a WRAPPING grid inside a ScrollView, so a release in
        /// the 4 px gutter between two slots picked the grid, not a slot, and the controller moved nothing and
        /// said nothing. Against a Backpack the player has never arranged — displayed auto-compacted, every stack
        /// at the front — "nothing moved" is indistinguishable from "it sorted my stack to the front".
        ///
        /// So: release one pixel below a slot's bottom edge, in the gutter, where no slot button is. It has to
        /// land in that slot.
        ///
        /// Compiled offline; Play Mode is not available on this machine, so it has not been RUN here.
        /// </summary>
        [UnityTest, Timeout(60000)]
        public IEnumerator AReleaseInTheGutterBesideASlotLandsInThatSlot()
        {
            yield return Open();
            _host.Simulation.Apply(new InventorySortCommand());
            var cells = Slots();

            var from = FirstFilled(cells);
            if (from < 0) Assert.Ignore("the Backpack is empty, so there is nothing to drag.");

            var buttons = PackButtons();
            // A visible empty slot to aim at, other than the one being dragged.
            var target = -1;
            for (var i = 0; i < cells.Count; i++)
            {
                if (i == from || !cells[i].Empty || buttons[i] == null) continue;
                if (!UiFixture.Shown(buttons[i])) continue;
                target = i;
                break;
            }
            if (target < 0) Assert.Ignore("no empty Backpack slot is on screen to aim at.");

            // One pixel outside the slot, in the row gutter (--ui-space-1 is 4 px), so no slot button contains it.
            var box = buttons[target].worldBound;
            var release = new Vector2(box.center.x, box.yMax + 1f);
            for (var i = 0; i < buttons.Count; i++)
                if (buttons[i] != null)
                    Assert.That(buttons[i].worldBound.Contains(release), Is.False,
                        "the release point is inside slot " + i + ", so it is not the near miss this tests.");

            var nearest = NearestIndex(buttons, release);
            if (nearest != target)
                Assert.Ignore("on this layout the gutter below slot " + target + " is nearest to slot " + nearest + ".");

            var item = cells[from].Item;
            var count = cells[from].Count;
            var before = Carried();

            var payload = _panel.DragFrom(InventoryPanelController.SlotKind.Pack, from);
            Assert.That(payload, Is.Not.Null, "slot " + from + " holds something but produced no drag payload.");
            _panel.DragToPoint(release, payload);
            yield return null;

            var after = Slots();
            Assert.That(after[target].Item, Is.EqualTo(item),
                "a release in the gutter beside slot " + target + " did not land there (" + _panel.Notice + ").");
            Assert.That(after[target].Count, Is.EqualTo(count).Within(Tolerance), "the amount changed on the way.");
            Assert.That(after[from].Empty, Is.True, "the stack is still in the slot it was dragged out of.");
            AssertConserved(before, "a release in the gutter");
        }

        /// <summary>
        /// GP-UX-9, the other half of the report: "can you also add ... stack splitting functionality". The Split
        /// button had to use the first empty slot, because a button has no destination to read. Shift while
        /// dragging does have one, and this asserts it is used: the half lands in the slot that was hovered, which
        /// is deliberately NOT the first empty slot.
        ///
        /// Compiled offline; Play Mode is not available on this machine, so it has not been RUN here.
        /// </summary>
        [UnityTest, Timeout(60000)]
        public IEnumerator AShiftDragSplitsIntoTheHoveredSlotAndNotTheFirstEmptyOne()
        {
            yield return Open();
            _host.Simulation.Apply(new InventorySortCommand());
            var cells = Slots();

            var from = -1;
            for (var i = 0; i < cells.Count; i++)
                if (!cells[i].Empty && !cells[i].IsWeapon && cells[i].Count >= 2) { from = i; break; }
            if (from < 0) Assert.Ignore("the Backpack carries no stack of two or more, so nothing can be split.");

            var firstEmpty = FirstEmpty(cells);
            var target = -1;
            for (var i = cells.Count - 1; i >= 0; i--) if (cells[i].Empty) { target = i; break; }
            if (target < 0 || target == firstEmpty)
                Assert.Ignore("the Backpack has fewer than two free slots, so the chosen slot cannot differ from the first.");

            var item = cells[from].Item;
            var count = cells[from].Count;
            var half = (int)System.Math.Floor(count / 2.0);
            var before = Carried();

            var payload = _panel.DragFrom(InventoryPanelController.SlotKind.Pack, from);
            Assert.That(payload, Is.Not.Null);
            _panel.DragTo(InventoryPanelController.SlotKind.Pack, target, payload, true);
            yield return null;

            var after = Slots();
            Assert.That(after[target].Item, Is.EqualTo(item),
                "the split half did not land in the hovered slot " + target + " (" + _panel.Notice + ").");
            Assert.That(after[target].Count, Is.EqualTo((double)half).Within(Tolerance), "the wrong amount was split off.");
            Assert.That(after[from].Count, Is.EqualTo(count - half).Within(Tolerance), "the source kept the wrong remainder.");
            Assert.That(after[firstEmpty].Empty, Is.True,
                "slot " + firstEmpty + " — the first empty slot, what the Split BUTTON uses — was written instead.");
            AssertConserved(before, "a Shift-drag split");
        }

        /// <summary>
        /// The near-miss snap has a boundary: a release that is not in any slot grid at all still moves nothing.
        /// The difference from before GP-UX-9 is that the drawer now SAYS so, which is what made the original
        /// defect unreadable — a lost drop and a silent panel look exactly like an unwanted auto-sort.
        ///
        /// Compiled offline; Play Mode is not available on this machine, so it has not been RUN here.
        /// </summary>
        [UnityTest, Timeout(60000)]
        public IEnumerator AReleaseAwayFromEveryGridMovesNothingAndSaysSo()
        {
            yield return Open();
            _host.Simulation.Apply(new InventorySortCommand());
            var cells = Slots();
            var from = FirstFilled(cells);
            if (from < 0) Assert.Ignore("the Backpack is empty, so there is nothing to drag.");

            var item = cells[from].Item;
            var count = cells[from].Count;
            var before = Carried();

            var payload = _panel.DragFrom(InventoryPanelController.SlotKind.Pack, from);
            Assert.That(payload, Is.Not.Null);
            _panel.DragToPoint(new Vector2(-4000f, -4000f), payload);
            yield return null;

            var after = Slots();
            Assert.That(after[from].Item, Is.EqualTo(item), "a release off the grids moved the stack.");
            Assert.That(after[from].Count, Is.EqualTo(count).Within(Tolerance), "a release off the grids changed a count.");
            Assert.That(_panel.Notice, Is.EqualTo(Relight.Sim.UI.TransferText.DropOffSlot),
                "the drawer said nothing about a drop that went nowhere.");
            AssertConserved(before, "a release away from every grid");
        }
    }
}
