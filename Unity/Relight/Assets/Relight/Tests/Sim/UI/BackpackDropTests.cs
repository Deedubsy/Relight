using NUnit.Framework;
using Relight.Sim.UI;

namespace Relight.Sim.Tests
{
    /// <summary>
    /// C-06. The transfer behaviours the Backpack panel is built on, asserted at the layer that decides them so
    /// the panel only has to send the command. UI_AND_ONBOARDING.md §5.3 (defect U-1: drops onto the rightmost
    /// slot and onto a slot only reachable by scrolling both used to fail) and §5.8 (conservation).
    /// </summary>
    public sealed class BackpackDropTests
    {
        [Test]
        public void EverySlotAcceptsADropIncludingTheLastOneAndOnesOnlyReachedByScrolling()
        {
            var ctx = Fixture.Context();
            var last = ctx.Data.Engineer.InvStacks - 1;
            Assert.GreaterOrEqual(last, 24, "the defect was reported against slots 24 and 39");

            foreach (var target in new[] { 5, 24, last })
            {
                var st = Fixture.State(ctx);
                var e = st.Engineer;
                Assert.IsTrue(Fixture.Apply(ctx, st, new InventorySortCommand()).Accepted);

                var slots = Pockets.Slots(ctx.Data, e);
                var from = -1;
                for (var i = 0; i < slots.Count; i++)
                    if (slots[i] != null && slots[i].Item == "steel") { from = i; break; }
                Assert.GreaterOrEqual(from, 0);

                var before = e.Inv[ItemId.Steel];
                var count = slots[from].Count;
                var move = Fixture.Apply(ctx, st,
                    new InventoryMoveCommand(from, target, "steel", count, Backpack.Layout(ctx.Data, e)));
                Assert.IsTrue(move.Accepted, "slot " + target + ": " + move.Problem);

                var after = Pockets.Slots(ctx.Data, e);
                Assert.AreEqual("steel", after[target].Item, "the stack landed in slot " + target);
                Assert.AreEqual(count, after[target].Count);
                Assert.AreEqual(before, e.Inv[ItemId.Steel], "a move never changes quantities");
                Assert.AreEqual("", Fixture.Off(ctx, st));
            }
        }

        [Test]
        public void RepeatedDropsOntoTheSameStackMergeUntilItIsFullAndNeverOverfillIt()
        {
            var ctx = Fixture.Context();
            var st = Fixture.State(ctx);
            var e = st.Engineer;
            var cap = ctx.Data.StackSize(ItemKey.Of(ItemId.Steel));
            var extra = cap * 3 - e.Inv[ItemId.Steel];
            e.Inv[ItemId.Steel] = cap * 3;
            st.Stats.Made.Add(ItemId.Steel, extra);   // the fixture's ledger has to know where the steel came from
            Assert.IsTrue(Fixture.Apply(ctx, st, new InventorySortCommand()).Accepted);

            // Break the three full stacks into a target holding one unit and three donors.
            var slots = Pockets.Slots(ctx.Data, e);
            var first = -1;
            for (var i = 0; i < slots.Count; i++)
                if (slots[i] != null && slots[i].Item == "steel") { first = i; break; }
            Assert.GreaterOrEqual(first, 0);

            var target = slots.Count - 1;
            Assert.IsTrue(Fixture.Apply(ctx, st,
                new InventorySplitCommand(first, target, "steel", cap, Backpack.Layout(ctx.Data, e), 1)).Accepted);

            var total = e.Inv[ItemId.Steel];
            for (var pass = 0; pass < 4; pass++)
            {
                slots = Pockets.Slots(ctx.Data, e);
                var donor = -1;
                for (var i = 0; i < slots.Count; i++)
                    if (i != target && slots[i] != null && slots[i].Item == "steel") { donor = i; break; }
                if (donor < 0) break;
                Fixture.Apply(ctx, st,
                    new InventoryMoveCommand(donor, target, "steel", slots[donor].Count, Backpack.Layout(ctx.Data, e)));

                slots = Pockets.Slots(ctx.Data, e);
                Assert.LessOrEqual(slots[target].Count, cap, "a merge never overfills a stack");
                Assert.AreEqual(total, e.Inv[ItemId.Steel], "a merge never loses or invents steel");
                Assert.AreEqual("", Fixture.Off(ctx, st));
            }

            slots = Pockets.Slots(ctx.Data, e);
            Assert.AreEqual(cap, slots[target].Count, "the target ends full");
        }

        [Test]
        public void CancellingACraftReturnsItsReservedIngredientsAndTheLedgerStillBalances()
        {
            var ctx = Fixture.Context();
            var st = Fixture.State(ctx);
            var e = st.Engineer;
            Fixture.Place(ctx, st, "depot", 10, 10);
            var steel = e.Inv[ItemId.Steel];
            var copper = e.Inv[ItemId.Copper];

            var start = Fixture.Apply(ctx, st, new HandCraftCommand(1));
            Assert.IsTrue(start.Accepted, start.Problem);
            Fixture.Run(ctx, st, 0.5);
            Assert.IsFalse(HandCraft.HandLocked(st), "U-D-44: a queued batch does not hold the engineer");

            var cancel = Fixture.Apply(ctx, st, new CancelCraftCommand());
            Assert.IsTrue(cancel.Accepted, cancel.Problem);
            StringAssert.StartsWith("Job cancelled.", cancel.Problem);
            Assert.AreEqual(steel, e.Inv[ItemId.Steel], "the reserved steel came back");
            Assert.AreEqual(copper, e.Inv[ItemId.Copper], "the reserved copper came back");
            Assert.AreEqual("", Fixture.Off(ctx, st));

            var again = Fixture.Apply(ctx, st, new CancelCraftCommand());
            Assert.AreEqual(steel, e.Inv[ItemId.Steel], "a second cancel returns nothing");
            Assert.AreEqual(copper, e.Inv[ItemId.Copper]);
            Assert.AreEqual("", Fixture.Off(ctx, st));
        }

        /// <summary>
        /// GP-UX-9. The owner reported that a dragged stack "automatically sorts it into the first available slot
        /// instead of the slot hovered over". Both tests above pass an <see cref="InventorySortCommand"/> first,
        /// so neither of them ever reached the state a new game is actually in: <c>Engineer.Pack</c> is null until
        /// an inventory command writes one, and a null pack is DISPLAYED auto-compacted by <c>Pockets.Slots</c>.
        /// That is the state the report was made in,
        /// and it is the state this asserts: the chosen slot is honoured, from the first move of a fresh game.
        /// </summary>
        [Test]
        public void AMoveInANeverArrangedBackpackStillLandsInTheChosenSlot()
        {
            var ctx = Fixture.Context();
            var st = Fixture.State(ctx);
            var e = st.Engineer;
            Assert.IsNull(e.Pack, "a new game has no recorded layout — this is the reported state");

            var slots = Pockets.Slots(ctx.Data, e);
            var from = -1;
            for (var i = 0; i < slots.Count; i++)
                if (slots[i] != null) { from = i; break; }
            Assert.GreaterOrEqual(from, 0, "the opening balance carries something");

            const int target = 20;
            Assert.IsNull(slots[target], "slot " + target + " starts empty");
            var item = slots[from].Item;
            var count = slots[from].Count;
            var held = e.Inv[new ItemKey(item)];

            var move = Fixture.Apply(ctx, st,
                new InventoryMoveCommand(from, target, item, count, Backpack.Layout(ctx.Data, e)));
            Assert.IsTrue(move.Accepted, move.Problem);

            var after = Pockets.Slots(ctx.Data, e);
            Assert.IsNotNull(after[target], "the stack went to the slot that was chosen, not to the front");
            Assert.AreEqual(item, after[target].Item);
            Assert.AreEqual(count, after[target].Count);
            Assert.IsNull(after[from], "and it left the slot it came from");
            Assert.AreEqual(held, e.Inv[new ItemKey(item)], "a move never changes quantities");
            Assert.AreEqual("", Fixture.Off(ctx, st));
        }

        /// <summary>
        /// GP-UX-9. The Split button has no destination to read, so it splits into the first empty slot. The
        /// Shift-drag gesture does have one, and this is the command it sends: the half lands in the slot the
        /// pointer was over and the first empty slot is untouched.
        /// </summary>
        [Test]
        public void ASplitHonoursAChosenSlotRatherThanTheFirstEmptyOne()
        {
            var ctx = Fixture.Context();
            var st = Fixture.State(ctx);
            var e = st.Engineer;

            var slots = Pockets.Slots(ctx.Data, e);
            var from = -1;
            for (var i = 0; i < slots.Count; i++)
                if (slots[i] != null && slots[i].Count >= 2) { from = i; break; }
            Assert.GreaterOrEqual(from, 0, "the opening balance carries a stack of at least two");

            var firstEmpty = -1;
            for (var i = 0; i < slots.Count; i++)
                if (slots[i] == null) { firstEmpty = i; break; }
            Assert.GreaterOrEqual(firstEmpty, 0);

            const int target = 33;
            Assert.AreNotEqual(firstEmpty, target, "the chosen slot has to be a slot the button would not pick");
            Assert.IsNull(slots[target], "slot " + target + " starts empty");

            var item = slots[from].Item;
            var whole = slots[from].Count;
            var half = (int)(whole / 2);
            Assert.GreaterOrEqual(half, 1);
            var held = e.Inv[new ItemKey(item)];

            var split = Fixture.Apply(ctx, st,
                new InventorySplitCommand(from, target, item, whole, Backpack.Layout(ctx.Data, e), half));
            Assert.IsTrue(split.Accepted, split.Problem);

            var after = Pockets.Slots(ctx.Data, e);
            Assert.IsNotNull(after[target], "the half went to slot " + target);
            Assert.AreEqual(item, after[target].Item);
            Assert.AreEqual(half, after[target].Count);
            Assert.AreEqual(whole - half, after[from].Count, "and the source kept the rest");
            Assert.IsNull(after[firstEmpty], "the first empty slot — what the Split BUTTON would have used — is untouched");
            Assert.AreEqual(held, e.Inv[new ItemKey(item)], "a split never changes quantities");
            Assert.AreEqual("", Fixture.Off(ctx, st));
        }

        /// <summary>
        /// U-D-44: the prompt the HUD shows belongs to the repair, not to the workshop — a queued batch no longer
        /// pins anyone — and it is still the sim's own sentence, taken unchanged.
        /// </summary>
        [Test]
        public void TheLockTextTheHudShowsIsTheSimsOwn()
        {
            Assert.AreEqual("Repairing — Cancel to move.", Home.LockText);
            Assert.AreEqual("Repairing — Cancel to move. · Escape cancels", WorkshopText.HandLockPrompt(Home.LockText));
        }
    }
}
