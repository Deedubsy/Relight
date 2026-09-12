using NUnit.Framework;

namespace Relight.Sim.Tests
{
    /// <summary>
    /// Pocket/Backpack layout and the inventory commands, against reference engineer.ts
    /// (`pocketSlots`, `invStacks`, `take`, `inventoryCommand`).
    /// </summary>
    public sealed class PocketsTests
    {
        [Test]
        public void SlotsFillInOrdinalKeyOrderAndSplitFullStacks()
        {
            var ctx = Fixture.Context();
            var st = Fixture.State(ctx);
            var e = st.Engineer;
            e.Inv[ItemId.Steel] = 60;
            e.Inv[ItemId.Copper] = 5;
            e.Inv[ItemId.Coal] = 10;

            var slots = Pockets.Slots(ctx.Data, e);
            Assert.AreEqual(ctx.Data.Engineer.InvStacks, slots.Count, "the Backpack is always all its slots");
            Assert.AreEqual("coal", slots[0].Item);     // ordinal: coal < copper < steel
            Assert.AreEqual(10, slots[0].Count);
            Assert.AreEqual("copper", slots[1].Item);
            Assert.AreEqual("steel", slots[2].Item);
            Assert.AreEqual(50, slots[2].Count, "a full stack first");
            Assert.AreEqual("steel", slots[3].Item);
            Assert.AreEqual(10, slots[3].Count);
            Assert.IsNull(slots[4]);
            Assert.AreEqual(4, Pockets.Stacks(ctx.Data, e.Inv), "ceil(60/50) + 1 + 1");
        }

        [Test]
        public void TakeStopsAtCapacityAndNeverInventsQuantities()
        {
            var ctx = Fixture.Context();
            var st = Fixture.State(ctx);
            var e = st.Engineer;   // opens with 20 steel + 5 copper (U-D-18)
            var got = Pockets.Take(ctx.Data, e, ItemKey.Of(ItemId.Steel), 5000);
            Assert.AreEqual(1930, got, "38 free stacks of 50 plus the 30 slack in the open steel stack");
            Assert.AreEqual(1950, e.Inv[ItemId.Steel]);
            Assert.AreEqual(40, Pockets.Used(ctx.Data, e));
            Assert.AreEqual(0, Pockets.Take(ctx.Data, e, ItemKey.Of(ItemId.Steel), 1), "a full Backpack takes nothing");
        }

        [Test]
        public void SortThenSplitThenMergeKeepsQuantities()
        {
            var ctx = Fixture.Context();
            var st = Fixture.State(ctx);
            var e = st.Engineer;
            e.Inv[ItemId.Steel] = 20;

            Assert.IsTrue(Fixture.Apply(ctx, st, new InventorySortCommand()).Accepted);
            Assert.IsNotNull(e.Pack);

            var layout = Backpack.Layout(ctx.Data, e);
            var slots = Pockets.Slots(ctx.Data, e);
            var steelAt = slots[0].Item == "steel" ? 0 : 1;
            var split = Fixture.Apply(ctx, st, new InventorySplitCommand(steelAt, 9, "steel", 20, layout, 8));
            Assert.IsTrue(split.Accepted, split.Problem);
            Assert.AreEqual("Stack split.", split.Problem);
            slots = Pockets.Slots(ctx.Data, e);
            Assert.AreEqual(8, slots[9].Count);
            Assert.AreEqual(12, slots[steelAt].Count);
            Assert.AreEqual(20, e.Inv[ItemId.Steel], "a split never changes quantities");

            layout = Backpack.Layout(ctx.Data, e);
            var move = Fixture.Apply(ctx, st, new InventoryMoveCommand(9, steelAt, "steel", 8, layout));
            Assert.IsTrue(move.Accepted, move.Problem);
            Assert.AreEqual("Stack moved.", move.Problem);
            slots = Pockets.Slots(ctx.Data, e);
            Assert.AreEqual(20, slots[steelAt].Count);
            Assert.IsNull(slots[9]);
            Assert.AreEqual(20, e.Inv[ItemId.Steel]);
        }

        [Test]
        public void StaleLayoutAndBadSlotsAreRefusedWithTheReferenceMessages()
        {
            var ctx = Fixture.Context();
            var st = Fixture.State(ctx);
            var e = st.Engineer;
            var layout = Backpack.Layout(ctx.Data, e);

            var stale = Fixture.Apply(ctx, st, new InventoryMoveCommand(0, 1, "copper", 5, "[]"));
            Assert.IsFalse(stale.Accepted);
            Assert.AreEqual("Stacks changed. Select the stack again.", stale.Problem);

            var same = Fixture.Apply(ctx, st, new InventoryMoveCommand(0, 0, "copper", 5, layout));
            Assert.IsFalse(same.Accepted);
            Assert.AreEqual("Choose a different backpack slot.", same.Problem);

            var wrong = Fixture.Apply(ctx, st, new InventoryMoveCommand(0, 5, "steel", 999, layout));
            Assert.IsFalse(wrong.Accepted);
            Assert.AreEqual("Source stack changed.", wrong.Problem);

            var onto = Fixture.Apply(ctx, st, new InventorySplitCommand(0, 1, "copper", 5, layout, 2));
            Assert.IsFalse(onto.Accepted);
            Assert.AreEqual("Splitting needs an empty slot.", onto.Problem);

            var whole = Fixture.Apply(ctx, st, new InventorySplitCommand(0, 9, "copper", 5, layout, 5));
            Assert.IsFalse(whole.Accepted);
            Assert.AreEqual("Choose fewer than the stack contains.", whole.Problem);
        }
    }
}
