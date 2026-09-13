using NUnit.Framework;

namespace Relight.Sim.Tests
{
    /// <summary>Hand crafting (reference flow.ts tickHand/queueCraft/cancelCraft), with conservation across both endings.</summary>
    public sealed class HandCraftTests
    {
        [Test]
        public void WithoutADepotEveryCraftIsRefused()
        {
            var ctx = Fixture.Context();
            var st = Fixture.State(ctx);
            var r = Fixture.Apply(ctx, st, new HandCraftCommand(1));
            Assert.IsFalse(r.Accepted);
            Assert.AreEqual("Walk closer to Home workshop", r.Problem);
            Assert.AreEqual(0, st.Hand.Crafts);
            Fixture.Run(ctx, st, 25);
            Assert.AreEqual(0, st.Engineer.Inv[ItemId.Magazine]);
            Assert.AreEqual("", Fixture.Off(ctx, st));
        }

        [Test]
        public void ACompletedBatchMakesTenBulletsFromTwoSteelAndOneCopper()
        {
            var ctx = Fixture.Context();
            var st = Fixture.State(ctx);
            Fixture.Place(ctx, st, "depot", 10, 10);

            var q = Fixture.Apply(ctx, st, new HandCraftCommand(1));
            Assert.IsTrue(q.Accepted, q.Problem);
            Assert.IsFalse(HandCraft.HandLocked(st), "queued, not yet started");

            Fixture.Run(ctx, st, Fixture.Dt);
            Assert.IsTrue(HandCraft.HandLocked(st));
            Assert.AreEqual(HandCraft.LockText, "Handcrafting — Cancel to move.");
            Assert.AreEqual(18, st.Engineer.Inv[ItemId.Steel], "the batch's plates are reserved up front");
            Assert.AreEqual(4, st.Engineer.Inv[ItemId.Copper]);
            Assert.AreEqual("", Fixture.Off(ctx, st), "reserved plates are a consumed sink");

            Fixture.Run(ctx, st, 19);
            Assert.AreEqual(0, st.Engineer.Inv[ItemId.Magazine], "not before HandBulletSeconds");
            Fixture.Run(ctx, st, 1.5);

            Assert.AreEqual(10, st.Engineer.Inv[ItemId.Magazine]);
            Assert.AreEqual(1, st.Stats.HandCrafted);
            Assert.AreEqual(10, st.Stats.Made[ItemId.Magazine]);
            Assert.AreEqual(0, st.Hand.Crafts);
            Assert.IsFalse(HandCraft.HandLocked(st));
            Assert.AreEqual("", Fixture.Off(ctx, st));
        }

        [Test]
        public void CancellingMidBatchReturnsThePlatesAndBalances()
        {
            var ctx = Fixture.Context();
            var st = Fixture.State(ctx);
            Fixture.Place(ctx, st, "depot", 10, 10);
            Assert.IsTrue(Fixture.Apply(ctx, st, new HandCraftCommand(1)).Accepted);
            Fixture.Run(ctx, st, 5);
            Assert.IsTrue(st.Hand.Crafting);
            Assert.AreEqual(2, st.Stats.Consumed[ItemId.Steel]);

            var c = Fixture.Apply(ctx, st, new CancelCraftCommand());
            Assert.IsTrue(c.Accepted, c.Problem);
            Assert.AreEqual("Handcrafting cancelled. 2 Steel plates + 1 Copper returned.", c.Problem);
            Assert.AreEqual(20, st.Engineer.Inv[ItemId.Steel]);
            Assert.AreEqual(5, st.Engineer.Inv[ItemId.Copper]);
            Assert.AreEqual(0, st.Stats.Consumed[ItemId.Steel], "the refund un-counts the sink");
            Assert.AreEqual(0, st.Engineer.Inv[ItemId.Magazine]);
            Assert.AreEqual(0, st.Hand.RefundSteel, "it all fitted, so nothing is owed");
            Assert.AreEqual(0, st.Hand.RefundCopper);
            Assert.AreEqual("", Fixture.Off(ctx, st));

            var again = Fixture.Apply(ctx, st, new CancelCraftCommand());
            Assert.IsFalse(again.Accepted);
            Assert.AreEqual("Nothing is being crafted.", again.Problem);

            Fixture.Run(ctx, st, 25);
            Assert.AreEqual(0, st.Engineer.Inv[ItemId.Magazine], "a cancelled queue does not restart");
            Assert.AreEqual("", Fixture.Off(ctx, st));
        }

        [Test]
        public void WalkingOutOfRangeCancelsTheRunningBatch()
        {
            var ctx = Fixture.Context();
            var st = Fixture.State(ctx);
            Fixture.Place(ctx, st, "depot", 10, 10);
            Assert.IsTrue(Fixture.Apply(ctx, st, new HandCraftCommand(2)).Accepted);
            Fixture.Run(ctx, st, 5);
            Assert.IsTrue(st.Hand.Crafting);

            st.Engineer.Pos = new Vec2(30, 30);
            Fixture.Run(ctx, st, Fixture.Dt);
            Assert.IsFalse(st.Hand.Crafting);
            Assert.AreEqual(0, st.Hand.Crafts);
            Assert.AreEqual(20, st.Engineer.Inv[ItemId.Steel]);
            Assert.AreEqual("", Fixture.Off(ctx, st));
        }

        [Test]
        public void MoreBatchesThanThePocketsCanPayForAreClamped()
        {
            var ctx = Fixture.Context();
            var st = Fixture.State(ctx);
            Fixture.Place(ctx, st, "depot", 10, 10);
            Assert.IsTrue(Fixture.Apply(ctx, st, new HandCraftCommand(99)).Accepted);
            Assert.AreEqual(5, st.Hand.Crafts, "min(floor(20/2), floor(5/1))");

            var full = Fixture.Apply(ctx, st, new HandCraftCommand(1));
            Assert.IsFalse(full.Accepted);
            Assert.AreEqual("Need 2 Steel plates + 1 Copper in Backpack per ammunition batch", full.Problem);
        }

        // ---- the pending refund (U-M-31 item 2, superseded 2026-09-12) -------------------------------------
        //
        // A cancel used to refund only what the Backpack could take and leave the rest counted as `consumed`,
        // so the overflow left the game while the message still claimed the whole batch came back. The plates are
        // now an explicit debt on HandState that the ledger counts as held and HandCraft.Tick pays back.

        /// <summary>
        /// Stages stock directly in the pockets. There is no Phase B source that can fill a Backpack to its last
        /// cell, so the tests put it there and re-open the ledger, which makes conservation a statement about the
        /// refund rather than about the staging (<see cref="Ledger.Open"/> is exactly what a new game does).
        /// </summary>
        private static void Reopen(SimContext ctx, SimState st) => st.Ledger = Ledger.Open(st, ctx.Data);

        /// <summary>
        /// A running batch whose plates cannot go back: the pockets held exactly one batch's worth, the batch took
        /// them (freeing their two cells), and filler took those cells while it ran.
        /// </summary>
        private static (SimContext ctx, SimState st) BatchWithNoRoomToRefund()
        {
            var ctx = Fixture.Context();
            var st = Fixture.State(ctx);
            var d = ctx.Data;
            var e = st.Engineer;
            Fixture.Place(ctx, st, "depot", 10, 10);

            var magStack = d.StackSize(ItemKey.Of(ItemId.Magazine));
            e.Inv[ItemId.Steel] = d.Engineer.HandBulletSteel;     // one cell, and no slack left in it afterwards
            e.Inv[ItemId.Copper] = d.Engineer.HandBulletCopper;   // one cell
            e.Inv[ItemId.Magazine] = (Pockets.Cap(d) - 2) * magStack;
            Reopen(ctx, st);
            Assert.AreEqual(Pockets.Cap(d), Pockets.Used(d, e), "every Backpack cell is in use");
            Assert.AreEqual("", Fixture.Off(ctx, st));

            Assert.IsTrue(Fixture.Apply(ctx, st, new HandCraftCommand(1)).Accepted);
            Fixture.Run(ctx, st, Fixture.Dt);
            Assert.IsTrue(st.Hand.Crafting);
            Assert.AreEqual(0, e.Inv[ItemId.Steel], "the batch emptied the plate cells");
            Assert.AreEqual(0, e.Inv[ItemId.Copper]);

            e.Inv[ItemId.Magazine] += 2 * magStack;   // the two freed cells are taken while the batch runs
            Reopen(ctx, st);
            Assert.AreEqual(Pockets.Cap(d), Pockets.Used(d, e));
            return (ctx, st);
        }

        [Test]
        public void CancellingWithNoBackpackRoomHoldsThePlatesAsAPendingRefund()
        {
            var (ctx, st) = BatchWithNoRoomToRefund();

            var c = Fixture.Apply(ctx, st, new CancelCraftCommand());
            Assert.IsTrue(c.Accepted, c.Problem);
            Assert.AreEqual("Handcrafting cancelled. 2 Steel plates + 1 Copper waiting for Backpack space.", c.Problem,
                "the message says what actually happened, not a flat 'returned'");
            Assert.AreEqual(2, st.Hand.RefundSteel, "the plates are owed, not destroyed");
            Assert.AreEqual(1, st.Hand.RefundCopper);
            Assert.AreEqual(0, st.Engineer.Inv[ItemId.Steel], "and they are not in the pockets yet");
            Assert.AreEqual(0, st.Stats.Consumed[ItemId.Steel], "the WHOLE batch stops being a sink");
            Assert.AreEqual(0, st.Stats.Consumed[ItemId.Copper]);
            Assert.AreEqual("", Fixture.Off(ctx, st), "the ledger counts the debt as held in the pockets");
        }

        [Test]
        public void APendingRefundDrainsIntoThePocketsAsSoonAsThereIsRoom()
        {
            var (ctx, st) = BatchWithNoRoomToRefund();
            var d = ctx.Data;
            var e = st.Engineer;
            Assert.IsTrue(Fixture.Apply(ctx, st, new CancelCraftCommand()).Accepted);
            Assert.AreEqual(2, st.Hand.RefundSteel);

            Fixture.Run(ctx, st, 5);
            Assert.AreEqual(2, st.Hand.RefundSteel, "a full Backpack keeps owing; nothing is forced in");

            e.Inv[ItemId.Magazine] -= 2 * d.StackSize(ItemKey.Of(ItemId.Magazine));   // two cells freed
            Reopen(ctx, st);

            Fixture.Run(ctx, st, Fixture.Dt);
            Assert.AreEqual(2, e.Inv[ItemId.Steel], "one tick is enough");
            Assert.AreEqual(1, e.Inv[ItemId.Copper]);
            Assert.AreEqual(0, st.Hand.RefundSteel);
            Assert.AreEqual(0, st.Hand.RefundCopper);
            Assert.AreEqual("", Fixture.Off(ctx, st));
        }

        /// <summary>
        /// The refund is delivered through the player's own way of freeing space — moving the filler into chests
        /// with <see cref="MachineTransferCommand"/> — not only through the direct inventory edits the tests above
        /// use. A supply chest holds 200 items (U-M-31 (1)) and a magazine stack is 200 (U-D-08), so one stack fills
        /// one chest and each chest frees one Backpack cell: the first frees a cell and the steel comes back while
        /// the copper is still owed; the second frees another and the copper follows. The same sequence holds for
        /// the state after a save/load (loose-ends pass 2026-09-13, U-M-38).
        /// </summary>
        [Test]
        public void APendingRefundIsDeliveredAfterThePlayerMovesItemsIntoChests()
        {
            var (ctx, st) = BatchWithNoRoomToRefund();
            var d = ctx.Data;
            var magStack = d.StackSize(ItemKey.Of(ItemId.Magazine));
            var first = Fixture.Place(ctx, st, "chest", 18, 16);
            var second = Fixture.Place(ctx, st, "chest", 14, 16);
            Assert.IsTrue(Fixture.Apply(ctx, st, new CancelCraftCommand()).Accepted);
            Assert.AreEqual(2, st.Hand.RefundSteel);
            Fixture.Run(ctx, st, 1);
            Assert.AreEqual(2, st.Hand.RefundSteel, "nothing has been freed yet");
            Assert.AreEqual("", Fixture.Off(ctx, st));

            var loaded = SaveSerializer.ReadText(SaveSerializer.WriteText(st, d), d);
            Assert.IsTrue(loaded.Ok, loaded.Reason);
            Assert.AreEqual(2, loaded.State.Hand.RefundSteel);

            var pass = 0;
            foreach (var s in new[] { st, loaded.State })
            {
                pass++;
                var put = Fixture.Apply(ctx, s, new MachineTransferCommand(first.Id, ItemId.Magazine, magStack, true));
                Assert.IsTrue(put.Accepted, $"pass {pass}, first chest: {put.Problem}");
                Assert.AreEqual(2, s.Hand.RefundSteel, "the command itself frees the cell; delivery is the tick's job");
                Fixture.Run(ctx, s, Fixture.Dt);
                Assert.AreEqual(2, s.Engineer.Inv[ItemId.Steel], $"pass {pass}: the steel is back one tick after a cell appeared");
                Assert.AreEqual(0, s.Hand.RefundSteel);
                Assert.AreEqual(0, s.Engineer.Inv[ItemId.Copper], "one cell only: the copper is still waiting");
                Assert.AreEqual(1, s.Hand.RefundCopper);
                Assert.AreEqual("", Fixture.Off(ctx, s));

                put = Fixture.Apply(ctx, s, new MachineTransferCommand(second.Id, ItemId.Magazine, magStack, true));
                Assert.IsTrue(put.Accepted, $"pass {pass}, second chest: {put.Problem}");
                Fixture.Run(ctx, s, Fixture.Dt);
                Assert.AreEqual(1, s.Engineer.Inv[ItemId.Copper], $"pass {pass}: the copper follows the next freed cell");
                Assert.AreEqual(0, s.Hand.RefundCopper);
                Assert.AreEqual("", Fixture.Off(ctx, s), "nothing was lost or duplicated on the way");
            }
        }

        [Test]
        public void CancellingAgainWhileARefundIsStillOutstandingBalances()
        {
            var (ctx, st) = BatchWithNoRoomToRefund();
            var d = ctx.Data;
            var e = st.Engineer;
            Assert.IsTrue(Fixture.Apply(ctx, st, new CancelCraftCommand()).Accepted);

            // Whole stacks of plates: enough to pay for a batch, but not one cell or one unit of slack to pay the
            // outstanding debt with, so it is still owed when the second cancel arrives.
            var steelStack = d.StackSize(ItemKey.Of(ItemId.Steel));
            var copperStack = d.StackSize(ItemKey.Of(ItemId.Copper));
            e.Inv[ItemId.Magazine] -= 2 * d.StackSize(ItemKey.Of(ItemId.Magazine));
            e.Inv[ItemId.Steel] = steelStack;
            e.Inv[ItemId.Copper] = copperStack;
            Reopen(ctx, st);
            Assert.AreEqual(Pockets.Cap(d), Pockets.Used(d, e));

            Fixture.Run(ctx, st, 1);
            Assert.AreEqual(2, st.Hand.RefundSteel, "full stacks leave no slack: still owed");

            Assert.IsTrue(Fixture.Apply(ctx, st, new HandCraftCommand(1)).Accepted);
            Fixture.Run(ctx, st, Fixture.Dt);
            Assert.IsTrue(st.Hand.Crafting, "a second batch runs while the first cancel is still owed");
            Assert.AreEqual(2, st.Stats.Consumed[ItemId.Steel]);

            var c = Fixture.Apply(ctx, st, new CancelCraftCommand());
            Assert.IsTrue(c.Accepted, c.Problem);
            Assert.AreEqual("Handcrafting cancelled. 2 Steel plates + 1 Copper returned."
                            + " 2 Steel plates + 1 Copper waiting for Backpack space.", c.Problem,
                "the batch's own plates fit into the slack it made; the older debt still does not");
            Assert.AreEqual(steelStack, e.Inv[ItemId.Steel]);
            Assert.AreEqual(copperStack, e.Inv[ItemId.Copper]);
            Assert.AreEqual(2, st.Hand.RefundSteel);
            Assert.AreEqual(1, st.Hand.RefundCopper);
            Assert.AreEqual(0, st.Stats.Consumed[ItemId.Steel]);
            Assert.AreEqual(0, st.Stats.Consumed[ItemId.Copper]);
            Assert.AreEqual("", Fixture.Off(ctx, st));
        }

        [Test]
        public void AnOutstandingRefundSurvivesASaveAndLoad()
        {
            var (ctx, st) = BatchWithNoRoomToRefund();
            Assert.IsTrue(Fixture.Apply(ctx, st, new CancelCraftCommand()).Accepted);
            Assert.AreEqual(2, st.Hand.RefundSteel);

            var text = SaveSerializer.WriteText(st, ctx.Data);
            var loaded = SaveSerializer.ReadText(text, ctx.Data);
            Assert.IsTrue(loaded.Ok, loaded.Reason);
            Assert.AreEqual(2, loaded.State.Hand.RefundSteel, "the debt is saved state, not a transient");
            Assert.AreEqual(1, loaded.State.Hand.RefundCopper);
            Assert.AreEqual(StateHash.Compute(st), StateHash.Compute(loaded.State), "H1 across the pending refund");
            Assert.AreEqual("", Fixture.Off(ctx, loaded.State));

            // And it still drains after the load, which a field that only round-tripped as a number would not do.
            var e = loaded.State.Engineer;
            e.Inv[ItemId.Magazine] -= 2 * ctx.Data.StackSize(ItemKey.Of(ItemId.Magazine));
            Reopen(ctx, loaded.State);
            Fixture.Run(ctx, loaded.State, Fixture.Dt);
            Assert.AreEqual(2, e.Inv[ItemId.Steel]);
            Assert.AreEqual(0, loaded.State.Hand.RefundSteel);
            Assert.AreEqual("", Fixture.Off(ctx, loaded.State));
        }
    }
}
