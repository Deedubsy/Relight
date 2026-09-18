using NUnit.Framework;

namespace Relight.Sim.Tests
{
    /// <summary>
    /// C-02's build economy: the pockets pay the price once, a refusal moves nothing, the ghost query answers the
    /// same question the command does, and a carried machine is spent instead of plates (reference flow.ts
    /// `buildAffordability` 1333-1340 and `place` 1367-1375).
    /// </summary>
    [TestFixture]
    public sealed class PlacementCostTests
    {
        [Test]
        public void PlacingAChestChargesItsPriceExactlyOnce()
        {
            var ctx = Fixture.Context();
            var st = Fixture.State(ctx);
            Assert.AreEqual(20, st.Engineer.Inv[ItemId.Steel], 1e-9, "the opening stake");

            var r = Fixture.Apply(ctx, st, new PlaceMachineCommand("chest", 18, 16));
            Assert.IsTrue(r.Accepted, r.Problem);
            Assert.AreEqual(15, st.Engineer.Inv[ItemId.Steel], 1e-9, "the chest's 5 plates left the Backpack");
            Assert.AreEqual(5, st.Stats.Placed[ItemId.Steel], 1e-9, "and are counted as a placed sink");
            Assert.AreEqual(1, st.Machines.Count);
            Assert.AreEqual("", Fixture.Off(ctx, st), "the ledger balances after a purchase");

            var again = Fixture.Apply(ctx, st, new PlaceMachineCommand("chest", 20, 16));
            Assert.IsTrue(again.Accepted, again.Problem);
            Assert.AreEqual(10, st.Engineer.Inv[ItemId.Steel], 1e-9);
            Assert.AreEqual(10, st.Stats.Placed[ItemId.Steel], 1e-9);
            Assert.AreEqual("", Fixture.Off(ctx, st));
        }

        [Test]
        public void AShortBackpackIsRefusedWithThePriceAndNothingMoves()
        {
            var ctx = Fixture.Context();
            var st = Fixture.State(ctx);
            st.Engineer.Inv[ItemId.Steel] = 4;

            var r = Fixture.Apply(ctx, st, new PlaceMachineCommand("chest", 18, 16));
            Assert.IsFalse(r.Accepted);
            Assert.AreEqual("not enough in the Backpack (5 Steel plates)", r.Problem);
            Assert.AreEqual(4, st.Engineer.Inv[ItemId.Steel], 1e-9, "no partial charge");
            Assert.AreEqual(0, st.Machines.Count);
            Assert.AreEqual(0, st.Stats.Placed[ItemId.Steel], 1e-9);

            var turret = Fixture.Apply(ctx, st, new PlaceMachineCommand("turret", 18, 16));
            Assert.IsFalse(turret.Accepted);
            Assert.AreEqual("not enough in the Backpack (15 Steel plates + 5 Copper plate)", turret.Problem,
                "the whole price is named, not just the missing part");
        }

        [Test]
        public void TheGhostQueryAnswersGeometryReachPriceAndTheStationaryLock()
        {
            var ctx = Fixture.Context();
            var st = Fixture.State(ctx);

            Assert.IsTrue(Placement.Validity(ctx, st, "chest", 18, 16, Dir.N).ok);

            var far = Placement.Validity(ctx, st, "chest", 30, 30, Dir.N);
            Assert.IsFalse(far.ok);
            Assert.AreEqual("Walk closer to place the Supply chest", far.reason);

            var off = Placement.Validity(ctx, st, "chest", -2, 16, Dir.N);
            Assert.IsFalse(off.ok, "off the map is refused before anything else");

            st.Engineer.Inv[ItemId.Steel] = 0;
            var poor = Placement.Validity(ctx, st, "chest", 18, 16, Dir.N);
            Assert.IsFalse(poor.ok);
            Assert.AreEqual("not enough in the Backpack (5 Steel plates)", poor.reason);

            st.Engineer.Inv[ItemId.Steel] = 20;
            st.Home.RepairKind = RepairKinds.Machine;
            st.Home.RepairId = 1;
            st.Home.RepairRemaining = 10;
            var locked = Placement.Validity(ctx, st, "chest", 18, 16, Dir.N);
            Assert.IsFalse(locked.ok);
            Assert.AreEqual(Home.LockText, locked.reason);
            Assert.IsTrue(Placement.Buildable(ctx, st, "chest", 18, 16, Dir.N).ok,
                "Buildable is the physical question only; the lock belongs to the command");
        }

        /// <summary>
        /// A repair refuses both build commands. U-D-44: a queued workshop batch does not — the engineer is free to
        /// keep building while the workshop works, which is the whole point of the change.
        /// </summary>
        [Test]
        public void ARepairRefusesPlacementAndRotationButAQueuedBatchDoesNot()
        {
            var ctx = Fixture.Context();
            var st = Fixture.State(ctx);
            Fixture.Place(ctx, st, "depot", 10, 10);
            var turret = Fixture.Place(ctx, st, "turret", 18, 16);
            var machines = st.Machines.Count;

            Assert.IsTrue(Fixture.Apply(ctx, st, new HandCraftCommand(1)).Accepted);
            var free = Fixture.Apply(ctx, st, new PlaceMachineCommand("chest", 20, 16));
            Assert.IsTrue(free.Accepted, "a queued batch does not stop a build: " + free.Problem);
            Assert.AreEqual(machines + 1, st.Machines.Count);
            Assert.AreEqual(1, HandCraft.QueuedBatches(st), "and the build did not disturb the queue");

            st.Home.RepairKind = RepairKinds.Machine;
            st.Home.RepairId = 1;
            st.Home.RepairRemaining = 10;
            machines = st.Machines.Count;

            var place = Fixture.Apply(ctx, st, new PlaceMachineCommand("chest", 22, 16));
            Assert.IsFalse(place.Accepted);
            Assert.AreEqual(Home.LockText, place.Problem);
            Assert.AreEqual(machines, st.Machines.Count);

            var rotate = Fixture.Apply(ctx, st, new RotatePlacementCommand(turret.Id));
            Assert.IsFalse(rotate.Accepted);
            Assert.AreEqual(Home.LockText, rotate.Problem);
            Assert.AreEqual(Dir.N, turret.Dir);
        }

        [Test]
        public void RemovingAMachineHandsItBackAndRebuildingSpendsTheCarriedOne()
        {
            var ctx = Fixture.Context();
            var st = Fixture.State(ctx);
            // Placed through the command, not Fixture.Place: this test is about the price, and the shared helper
            // pays a machine's price in for its callers (see the report's patch request for InventoryFixture.cs).
            var placed = Fixture.Apply(ctx, st, new PlaceMachineCommand("chest", 18, 16));
            Assert.IsTrue(placed.Accepted, placed.Problem);
            var chest = st.Machines[st.Machines.Count - 1];
            Assert.AreEqual(15, st.Engineer.Inv[ItemId.Steel], 1e-9);

            var removed = Fixture.Apply(ctx, st, new RemoveMachineCommand(chest.Id));
            Assert.IsTrue(removed.Accepted, removed.Problem);
            Assert.AreEqual(0, st.Machines.Count);
            Assert.AreEqual(1, st.Engineer.Inv[new ItemKey("chest")], 1e-9, "the machine itself comes back as an item");
            Assert.AreEqual(15, st.Engineer.Inv[ItemId.Steel], 1e-9, "the plates are not refunded twice");

            var (ok, _, carried) = Placement.Affordability(ctx.Data, "chest", st.Engineer.Inv);
            Assert.IsTrue(ok);
            Assert.IsTrue(carried, "a carried machine pays for itself");

            var back = Fixture.Apply(ctx, st, new PlaceMachineCommand("chest", 19, 16));
            Assert.IsTrue(back.Accepted, back.Problem);
            Assert.AreEqual(0, st.Engineer.Inv[new ItemKey("chest")], 1e-9);
            Assert.AreEqual(15, st.Engineer.Inv[ItemId.Steel], 1e-9, "rebuilding a carried chest costs no plates");
            Assert.AreEqual(5, st.Stats.Placed[ItemId.Steel], 1e-9, "and is not counted a second time");
            Assert.AreEqual("", Fixture.Off(ctx, st));
        }

        [Test]
        public void RotationTurnsWhatCanTurnAndRefusesWhatCannot()
        {
            var ctx = Fixture.Context();
            var st = Fixture.State(ctx);
            st.Engineer.Inv[ItemId.Steel] = 40;   // enough for both; this test is about facing, not price
            var turret = Fixture.Place(ctx, st, "turret", 18, 16);
            var chest = Fixture.Place(ctx, st, "chest", 20, 16);

            var rev = st.Rev;
            var r = Fixture.Apply(ctx, st, new RotatePlacementCommand(turret.Id));
            Assert.IsTrue(r.Accepted, r.Problem);
            Assert.AreEqual(Dir.E, turret.Dir, "a quarter clockwise");
            Assert.Greater(st.Rev, rev, "the view is told to redraw");

            var no = Fixture.Apply(ctx, st, new RotatePlacementCommand(chest.Id));
            Assert.IsFalse(no.Accepted);
            Assert.AreEqual("this does not turn", no.Problem);
            Assert.IsFalse(Placement.Rotatable("chest"));

            var gone = Fixture.Apply(ctx, st, new RotatePlacementCommand(9999));
            Assert.IsFalse(gone.Accepted);
            Assert.AreEqual("nothing there", gone.Problem);

            st.Engineer.Pos = new Vec2(2, 2);
            var far = Fixture.Apply(ctx, st, new RotatePlacementCommand(turret.Id));
            Assert.IsFalse(far.Accepted);
            Assert.AreEqual("Walk closer to rotate", far.Problem);
        }

        [Test]
        public void ThePriceTextComesFromTheCatalogue()
        {
            var ctx = Fixture.Context();
            Assert.IsTrue(ctx.Data.TryMachine("turret", out var spec));
            Assert.AreEqual("15 Steel plates + 5 Copper plate", Placement.CostText(ctx.Data, spec.Cost));
            Assert.IsTrue(ctx.Data.TryMachine("depot", out var depot));
            Assert.AreEqual("nothing", Placement.CostText(ctx.Data, depot.Cost), "the Depot is free");

            var st = Fixture.State(ctx);
            st.Engineer.Inv[ItemId.Steel] = 0;
            st.Engineer.Inv[ItemId.Copper] = 0;
            var (ok, _, carried) = Placement.Affordability(ctx.Data, "depot", st.Engineer.Inv);
            Assert.IsTrue(ok, "an empty Backpack still affords the Depot");
            Assert.IsFalse(carried);
        }
    }
}
