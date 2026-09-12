using NUnit.Framework;

namespace Relight.Sim.Tests
{
    /// <summary>Machine inventories, the drop target, placement validation and the read-only selectors.</summary>
    public sealed class TransferTests
    {
        [Test]
        public void ADropTargetPutsTheItemsInTheChosenSlot()
        {
            var ctx = Fixture.Context();
            var st = Fixture.State(ctx);
            var chest = Fixture.Place(ctx, st, "chest", 18, 16);
            Assert.IsTrue(Fixture.Apply(ctx, st, new MachineTransferCommand(chest.Id, ItemId.Steel, 20, true)).Accepted);

            var layout = InventoryQueries.Layout(ctx, st);
            var target = new TransferTargetSpec(7, layout);
            var take = Fixture.Apply(ctx, st, new MachineTransferCommand(chest.Id, ItemId.Steel, 6, false, target));
            Assert.IsTrue(take.Accepted, take.Problem);

            var slots = Pockets.Slots(ctx.Data, st.Engineer);
            Assert.IsNotNull(slots[7]);
            Assert.AreEqual("steel", slots[7].Item);
            Assert.AreEqual(6, slots[7].Count);
            Assert.AreEqual("", Fixture.Off(ctx, st));
        }

        [Test]
        public void AStaleDropTargetIsRefusedWithoutMovingAnything()
        {
            var ctx = Fixture.Context();
            var st = Fixture.State(ctx);
            var chest = Fixture.Place(ctx, st, "chest", 18, 16);
            Assert.IsTrue(Fixture.Apply(ctx, st, new MachineTransferCommand(chest.Id, ItemId.Steel, 20, true)).Accepted);

            var stale = Fixture.Apply(ctx, st, new MachineTransferCommand(
                chest.Id, ItemId.Steel, 6, false, new TransferTargetSpec(7, "[]")));
            Assert.IsFalse(stale.Accepted);
            Assert.AreEqual("Backpack changed; try the drop again.", stale.Problem);
            Assert.AreEqual(20, chest.Inv[ItemId.Steel]);
            Assert.AreEqual("", Fixture.Off(ctx, st));
        }

        [Test]
        public void AGeneratorTakesFuelOnlyUpToItsCap()
        {
            var ctx = Fixture.Context();
            var st = Fixture.State(ctx);
            var gen = Fixture.Place(ctx, st, "generator", 18, 16);
            st.Engineer.Inv[ItemId.Coal] = 80;
            st.Stats.MinedOf[ItemId.Coal] = 80;

            var fed = Fixture.Apply(ctx, st, new MachineTransferCommand(gen.Id, ItemId.Coal, 80, true));
            Assert.IsTrue(fed.Accepted, fed.Problem);
            Assert.AreEqual(MachineInventory.GeneratorFuelCap(ctx.Data), gen.Inv[ItemId.Coal]);
            Assert.AreEqual(30, st.Engineer.Inv[ItemId.Coal]);
            Assert.AreEqual(50, st.Stats.HandFedCoal);

            var over = Fixture.Apply(ctx, st, new MachineTransferCommand(gen.Id, ItemId.Coal, 10, true));
            Assert.IsFalse(over.Accepted);
            Assert.AreEqual("Cannot load Coal: wrong input or inventory full", over.Problem);

            var wrong = Fixture.Apply(ctx, st, new MachineTransferCommand(gen.Id, ItemId.Steel, 1, true));
            Assert.IsFalse(wrong.Accepted);
            Assert.AreEqual("Cannot load Steel plates: wrong input or inventory full", wrong.Problem);
            Assert.AreEqual("", Fixture.Off(ctx, st));
        }

        [Test]
        public void SelectorsReportSlotsCapacityAndMachineContents()
        {
            var ctx = Fixture.Context();
            var st = Fixture.State(ctx);
            var chest = Fixture.Place(ctx, st, "chest", 18, 16);
            Assert.IsTrue(Fixture.Apply(ctx, st, new MachineTransferCommand(chest.Id, ItemId.Steel, 12, true)).Accepted);

            var slots = InventoryQueries.Slots(ctx, st);
            Assert.AreEqual(40, slots.Count);
            Assert.AreEqual("copper", slots[0].Item);
            Assert.AreEqual(50, slots[0].StackSize);
            Assert.AreEqual("Steel plates", slots[1].DisplayName);

            var (used, cap) = InventoryQueries.Capacity(ctx, st);
            Assert.AreEqual(2, used);
            Assert.AreEqual(40, cap);

            var view = InventoryQueries.Machine(ctx, st, chest.Id);
            Assert.IsTrue(view.HasInventory);
            Assert.IsTrue(view.InReach);
            Assert.AreEqual("Supply chest", view.DisplayName);
            Assert.AreEqual(1, view.Items.Count);
            Assert.AreEqual(12, view.Items[0].Count);

            var preview = InventoryQueries.Preview(ctx, st, chest.Id, ItemId.Steel, 5, false);
            Assert.AreEqual(5, preview.Moved);
            Assert.AreEqual("", preview.Reason);
            Assert.AreEqual(12, chest.Inv[ItemId.Steel], "a preview never moves anything");

            Assert.IsNull(InventoryQueries.Machine(ctx, st, 9999));
        }

        [Test]
        public void PlacementValidatesTheKindTheMapAndTheOccupancy()
        {
            var ctx = Fixture.Context();
            var st = Fixture.State(ctx);
            Assert.AreEqual("no such machine",
                Fixture.Apply(ctx, st, new PlaceMachineCommand("teleporter", 18, 16)).Problem);
            Assert.AreEqual("outside the city",
                Fixture.Apply(ctx, st, new PlaceMachineCommand("chest", 31, 16)).Problem);
            Assert.AreEqual("Walk closer to place the Supply chest",
                Fixture.Apply(ctx, st, new PlaceMachineCommand("chest", 28, 28)).Problem);

            var chest = Fixture.Place(ctx, st, "chest", 18, 16);
            Assert.AreEqual("another machine is there",
                Fixture.Apply(ctx, st, new PlaceMachineCommand("chest", 19, 17)).Problem);
            Assert.AreEqual(1, st.Machines.Count);
            Assert.AreEqual(chest.Id, st.Machines[0].Id);

            var depot = Fixture.Place(ctx, st, "depot", 10, 10);
            Assert.AreEqual("the Depot stays", Fixture.Apply(ctx, st, new RemoveMachineCommand(depot.Id)).Problem);
            Assert.AreEqual("", Fixture.Off(ctx, st));
        }

        [Test]
        public void AFullBackpackRefusesTheWholePickUp()
        {
            var ctx = Fixture.Context();
            var st = Fixture.State(ctx);
            var chest = Fixture.Place(ctx, st, "chest", 18, 16);
            chest.Inv[ItemId.Stone] = 30;
            st.Stats.MinedOf[ItemId.Stone] = 30;
            Pockets.Take(ctx.Data, st.Engineer, ItemKey.Of(ItemId.Steel), 5000);
            st.Stats.MinedOf[ItemId.Steel] = st.Engineer.Inv[ItemId.Steel] - 20;

            var r = Fixture.Apply(ctx, st, new RemoveMachineCommand(chest.Id));
            Assert.IsFalse(r.Accepted);
            StringAssert.StartsWith("the Backpack is full", r.Problem);
            Assert.AreEqual(1, st.Machines.Count, "all of it or none");
            Assert.AreEqual(30, chest.Inv[ItemId.Stone]);
            Assert.AreEqual("", Fixture.Off(ctx, st));
        }
    }
}
