using NUnit.Framework;

namespace Relight.Sim.Tests
{
    /// <summary>
    /// The Home workshop under U-D-44 (2026-09-15), which supersedes U-D-10: the workshop processes its queue while
    /// the player is anywhere else, at the same slow speeds, and its products are physical goods waiting in the
    /// workshop's own output tray until they are collected in reach.
    ///
    /// What these pin, in the order the brief asks for them: ingredients are RESERVED when queued so they cannot be
    /// spent twice; there is ONE queue that neither reopening a panel nor walking away duplicates; finished goods
    /// never appear remotely in the Backpack; a full tray PAUSES a batch without consuming its ingredients or making
    /// the same output twice; a cancel is LOSSLESS; and the queue, the reservation, the tray and the progress all
    /// survive a save and an old save's fold-in. Every ending is checked against <see cref="Ledger"/> conservation.
    ///
    /// Reference data: <c>hand-bullets</c> is 2 Steel + 1 Copper -> 10 Bullets in 12 s (OpeningBalance), and the
    /// opening Backpack is Steel 20 / Copper 5, so five batches is everything the stake can pay for.
    /// </summary>
    public sealed class HandCraftTests
    {
        private const string Bullets = "hand-bullets";

        /// <summary>The recipe's batch time, read from data rather than restated here.</summary>
        private static double Secs(SimContext ctx) => HandCraft.Recipe(ctx.Data, Bullets).Seconds;

        /// <summary>
        /// Re-opens the ledger over the current holdings, which is exactly what a new game does. The tests that
        /// stage stock directly (a full tray, a legacy debt) use it so conservation is a statement about the
        /// workshop rather than about the staging.
        /// </summary>
        private static void Reopen(SimContext ctx, SimState st) => st.Ledger = Ledger.Open(st, ctx.Data);

        private static double Tray(SimState st, ItemId id) => st.Hand.Output[id];

        private static (SimContext ctx, SimState st) AtHome()
        {
            var ctx = Fixture.Context();
            var st = Fixture.State(ctx);
            Fixture.Place(ctx, st, "depot", 10, 10);
            return (ctx, st);
        }

        // ---- queueing --------------------------------------------------------------------------------------

        [Test]
        public void WithoutADepotEveryCraftIsRefused()
        {
            var ctx = Fixture.Context();
            var st = Fixture.State(ctx);
            var r = Fixture.Apply(ctx, st, new HandCraftCommand(1));
            Assert.IsFalse(r.Accepted);
            Assert.AreEqual("Walk closer to Home workshop", r.Problem);
            Assert.AreEqual(0, HandCraft.QueuedBatches(st));
            Fixture.Run(ctx, st, 25);
            Assert.AreEqual(0, st.Engineer.Inv[ItemId.Magazine], "nothing ran, so nothing was made");
            Assert.AreEqual(0, Tray(st, ItemId.Magazine));
            Assert.AreEqual("", Fixture.Off(ctx, st));
        }

        [Test]
        public void MoreBatchesThanThePocketsCanPayForAreClamped()
        {
            var (ctx, st) = AtHome();
            Assert.IsTrue(Fixture.Apply(ctx, st, new HandCraftCommand(99)).Accepted);
            Assert.AreEqual(5, HandCraft.QueuedBatches(st), "min(floor(20/2), floor(5/1))");
            Assert.AreEqual(1, st.Hand.Jobs.Count, "a repeat order of one recipe merges rather than stacking up");
            Assert.AreEqual(10, st.Hand.Reserved[ItemId.Steel], "five batches' plates are held at the workshop");
            Assert.AreEqual(5, st.Hand.Reserved[ItemId.Copper]);
            Assert.AreEqual(10, st.Engineer.Inv[ItemId.Steel], "they left the Backpack, so they cannot be spent twice");
            Assert.AreEqual(0, st.Engineer.Inv[ItemId.Copper], "copper is what binds: every unit of it is held");
            Assert.AreEqual(0, st.Stats.Consumed[ItemId.Steel], "reserved is held, not consumed");

            var full = Fixture.Apply(ctx, st, new HandCraftCommand(1));
            Assert.IsFalse(full.Accepted);
            Assert.AreEqual("Need recipe ingredients in Backpack", full.Problem);
            Assert.AreEqual("", Fixture.Off(ctx, st));
        }

        /// <summary>
        /// Opening the panel again, or sending the same order twice, adds batches to the one queue the workshop
        /// owns — it never starts a second run of the same work in parallel (brief item 1).
        /// </summary>
        [Test]
        public void RepeatOrdersJoinTheOneQueueRatherThanStartingASecondRun()
        {
            var (ctx, st) = AtHome();
            Assert.IsTrue(Fixture.Apply(ctx, st, new HandCraftCommand(1)).Accepted);
            Assert.IsTrue(Fixture.Apply(ctx, st, new HandCraftCommand(1)).Accepted);
            Assert.AreEqual(1, st.Hand.Jobs.Count);
            Assert.AreEqual(2, HandCraft.QueuedBatches(st));

            Fixture.Run(ctx, st, Secs(ctx) * 2 + 1);
            Assert.AreEqual(2, st.Stats.HandCrafted, "two batches, made one after the other");
            Assert.AreEqual(20, Tray(st, ItemId.Magazine), "not four batches' worth");
            Assert.AreEqual("", Fixture.Off(ctx, st));
        }

        // ---- processing and the output tray ----------------------------------------------------------------

        [Test]
        public void ACompletedBatchWaitsInTheWorkshopTrayAndIsCollectedInReach()
        {
            var (ctx, st) = AtHome();
            var q = Fixture.Apply(ctx, st, new HandCraftCommand(1));
            Assert.IsTrue(q.Accepted, q.Problem);
            Assert.AreEqual("Queued at the Home workshop. It keeps working while you are away.", q.Problem);
            Assert.IsFalse(HandCraft.HandLocked(st), "U-D-44: a queued batch does not pin the engineer");

            Fixture.Run(ctx, st, Fixture.Dt);
            Assert.AreEqual(18, st.Engineer.Inv[ItemId.Steel], "the batch's plates are reserved up front");
            Assert.AreEqual(4, st.Engineer.Inv[ItemId.Copper]);
            Assert.AreEqual(0, st.Stats.Consumed[ItemId.Steel], "and are not a sink until the batch completes");
            Assert.AreEqual("", Fixture.Off(ctx, st));

            Fixture.Run(ctx, st, Secs(ctx) - 1);
            Assert.AreEqual(0, Tray(st, ItemId.Magazine), "not before the recipe's seconds");
            Fixture.Run(ctx, st, 1.5);

            Assert.AreEqual(10, Tray(st, ItemId.Magazine), "the goods are physically at the workshop");
            Assert.AreEqual(0, st.Engineer.Inv[ItemId.Magazine], "and never appear remotely in the Backpack");
            Assert.AreEqual(1, st.Stats.HandCrafted);
            Assert.AreEqual(10, st.Stats.Made[ItemId.Magazine]);
            Assert.AreEqual(2, st.Stats.Consumed[ItemId.Steel], "consumed exactly once, at completion");
            Assert.AreEqual(0, st.Hand.Reserved[ItemId.Steel]);
            Assert.AreEqual(0, HandCraft.QueuedBatches(st));
            Assert.IsFalse(HandCraft.HandLocked(st));
            Assert.AreEqual("", Fixture.Off(ctx, st));

            var told = st.Events.FindAll(e => e is WorkshopFinishedEvent);
            Assert.AreEqual(1, told.Count, "the HUD is told once, so it can say so while the panel is closed");
            Assert.AreEqual(0, ((WorkshopFinishedEvent)told[0]).Remaining);

            var got = Fixture.Apply(ctx, st, new CollectWorkshopCommand());
            Assert.IsTrue(got.Accepted, got.Problem);
            Assert.AreEqual(10, st.Engineer.Inv[ItemId.Magazine]);
            Assert.IsTrue(st.Hand.Output.IsEmpty);
            Assert.AreEqual("", Fixture.Off(ctx, st));
        }

        /// <summary>
        /// The point of U-D-44: the player queues, leaves, and the workshop is still working when they come back.
        /// Walking out of reach neither cancels the batch nor delivers its product across the map.
        /// </summary>
        [Test]
        public void TheQueueKeepsProcessingWhileTheEngineerIsAwayAndCollectingNeedsReach()
        {
            var (ctx, st) = AtHome();
            Assert.IsTrue(Fixture.Apply(ctx, st, new HandCraftCommand(2)).Accepted);
            Fixture.Run(ctx, st, 5);

            st.Engineer.Pos = new Vec2(30, 30);
            Fixture.Run(ctx, st, Secs(ctx) * 2);
            Assert.AreEqual(2, st.Stats.HandCrafted, "both batches ran with nobody standing there");
            Assert.AreEqual(20, Tray(st, ItemId.Magazine));
            Assert.AreEqual(0, st.Engineer.Inv[ItemId.Magazine]);

            var away = Fixture.Apply(ctx, st, new CollectWorkshopCommand());
            Assert.IsFalse(away.Accepted, "the goods are a physical thing at the workshop");
            Assert.AreEqual("Walk closer to Home workshop", away.Problem);
            Assert.AreEqual(20, Tray(st, ItemId.Magazine));

            st.Engineer.Pos = ctx.Geometry.Spawn;
            var got = Fixture.Apply(ctx, st, new CollectWorkshopCommand());
            Assert.IsTrue(got.Accepted, got.Problem);
            Assert.AreEqual(20, st.Engineer.Inv[ItemId.Magazine]);
            Assert.AreEqual("", Fixture.Off(ctx, st));
        }

        /// <summary>
        /// A full tray HOLDS the finished batch: nothing is consumed, nothing is produced twice however long it
        /// waits, and collecting resumes it. This is the "pause safely when output storage is full" requirement.
        /// </summary>
        [Test]
        public void AFullTrayPausesTheBatchWithoutConsumingOrRepeatingIt()
        {
            var (ctx, st) = AtHome();
            var d = ctx.Data;
            var magStack = d.StackSize(ItemKey.Of(ItemId.Magazine));
            st.Hand.Output[ItemId.Magazine] = HandCraft.OutputStacks * magStack;   // staged: the tray is full
            Reopen(ctx, st);
            Assert.AreEqual(0, HandCraft.TrayFree(d, st.Hand.Output));

            Assert.IsTrue(Fixture.Apply(ctx, st, new HandCraftCommand(1)).Accepted);
            Fixture.Run(ctx, st, Secs(ctx) + 10);

            Assert.IsTrue(st.Hand.OutputFull, "the job holds and says why");
            Assert.AreEqual(1, HandCraft.QueuedBatches(st), "the batch is not lost");
            Assert.AreEqual(Secs(ctx), st.Hand.Jobs[0].Progress, 1e-6, "it waits at full progress");
            Assert.AreEqual(0, st.Stats.HandCrafted, "and nothing completed");
            Assert.AreEqual(2, st.Hand.Reserved[ItemId.Steel], "its ingredients are still held, not spent");
            Assert.AreEqual(0, st.Stats.Consumed[ItemId.Steel]);
            Assert.AreEqual(HandCraft.OutputStacks * magStack, Tray(st, ItemId.Magazine), "nothing was made twice");
            Assert.AreEqual("", Fixture.Off(ctx, st));

            Assert.IsTrue(Fixture.Apply(ctx, st, new CollectWorkshopCommand()).Accepted);
            Assert.IsFalse(st.Hand.OutputFull);
            Fixture.Run(ctx, st, Fixture.Dt);
            Assert.AreEqual(1, st.Stats.HandCrafted, "collecting resumes the held batch on the next tick");
            Assert.AreEqual(10, Tray(st, ItemId.Magazine));
            Assert.AreEqual(2, st.Stats.Consumed[ItemId.Steel], "consumed now, and only now");
            Assert.AreEqual("", Fixture.Off(ctx, st));
        }

        // ---- cancellation ----------------------------------------------------------------------------------

        [Test]
        public void CancellingMidBatchReturnsEveryUnprocessedIngredientAndBalances()
        {
            var (ctx, st) = AtHome();
            Assert.IsTrue(Fixture.Apply(ctx, st, new HandCraftCommand(3)).Accepted);
            Fixture.Run(ctx, st, 5);
            Assert.AreEqual(6, st.Hand.Reserved[ItemId.Steel]);
            Assert.AreEqual(0, st.Stats.Consumed[ItemId.Steel]);

            // What a recipe card's Cancel sends: every job making this recipe.
            var c = Fixture.Apply(ctx, st, new CancelCraftCommand(0, Bullets));
            Assert.IsTrue(c.Accepted, c.Problem);
            Assert.AreEqual("Job cancelled. Ingredients returned.", c.Problem);
            Assert.AreEqual(20, st.Engineer.Inv[ItemId.Steel], "all three batches' plates came back");
            Assert.AreEqual(5, st.Engineer.Inv[ItemId.Copper]);
            Assert.IsTrue(st.Hand.Reserved.IsEmpty);
            Assert.IsTrue(st.Hand.Output.IsEmpty, "a cancelled batch produced nothing");
            Assert.AreEqual(0, st.Stats.Consumed[ItemId.Steel]);
            Assert.AreEqual("", Fixture.Off(ctx, st));

            var again = Fixture.Apply(ctx, st, new CancelCraftCommand());
            Assert.IsFalse(again.Accepted);
            Assert.AreEqual("Nothing is queued at the workshop.", again.Problem);

            Fixture.Run(ctx, st, 25);
            Assert.AreEqual(0, Tray(st, ItemId.Magazine), "a cancelled queue does not restart");
            Assert.AreEqual("", Fixture.Off(ctx, st));
        }

        /// <summary>
        /// Cancelling one job leaves the rest of the queue alone and returns only that job's share — the other
        /// job's ingredients stay reserved for it.
        /// </summary>
        [Test]
        public void CancellingOneJobLeavesTheRestOfTheQueueFunded()
        {
            var (ctx, st) = AtHome();
            Assert.IsTrue(Fixture.Apply(ctx, st, new HandCraftCommand(2)).Accepted);
            var first = st.Hand.Jobs[0].Id;
            st.Engineer.Inv[ItemId.IronOre] = 4;
            st.Stats.Made.Add(ItemId.IronOre, 4);
            Assert.IsTrue(Fixture.Apply(ctx, st, new HandCraftCommand(4, "hand-steel")).Accepted);
            Assert.AreEqual(2, st.Hand.Jobs.Count);
            Assert.AreEqual(4, st.Hand.Reserved[ItemId.IronOre]);

            var c = Fixture.Apply(ctx, st, new CancelCraftCommand(first));
            Assert.IsTrue(c.Accepted, c.Problem);
            Assert.AreEqual(1, st.Hand.Jobs.Count);
            Assert.AreEqual(20, st.Engineer.Inv[ItemId.Steel], "the bullets job's plates came back");
            Assert.AreEqual(0, st.Hand.Reserved[ItemId.Steel]);
            Assert.AreEqual(4, st.Hand.Reserved[ItemId.IronOre], "the smelting job keeps its ore");
            Assert.AreEqual("", Fixture.Off(ctx, st));

            Fixture.Run(ctx, st, 4 * 4 + 1);
            Assert.AreEqual(4, Tray(st, ItemId.Steel), "and it ran to the end");
            Assert.AreEqual("", Fixture.Off(ctx, st));
        }

        // ---- persistence -----------------------------------------------------------------------------------

        [Test]
        public void TheQueueReservationTrayAndProgressSurviveASaveAndLoad()
        {
            var (ctx, st) = AtHome();
            var d = ctx.Data;
            Assert.IsTrue(Fixture.Apply(ctx, st, new HandCraftCommand(3)).Accepted);
            Fixture.Run(ctx, st, Secs(ctx) + 1);
            Assert.AreEqual(10, Tray(st, ItemId.Magazine), "one batch finished before the save");
            Assert.AreEqual(2, HandCraft.QueuedBatches(st));

            var loaded = SaveSerializer.ReadText(SaveSerializer.WriteText(st, d), d);
            Assert.IsTrue(loaded.Ok, loaded.Reason);
            var h = loaded.State.Hand;
            Assert.AreEqual(1, h.Jobs.Count);
            Assert.AreEqual(st.Hand.Jobs[0].Id, h.Jobs[0].Id, "job ids are stable, so a Cancel button cannot slip");
            Assert.AreEqual(2, h.Jobs[0].Batches);
            Assert.AreEqual(st.Hand.Jobs[0].Progress, h.Jobs[0].Progress, 1e-9);
            Assert.AreEqual(4, h.Reserved[ItemId.Steel]);
            Assert.AreEqual(2, h.Reserved[ItemId.Copper]);
            Assert.AreEqual(10, h.Output[ItemId.Magazine]);
            Assert.AreEqual(StateHash.Compute(st), StateHash.Compute(loaded.State), "H1 across the workshop's state");
            Assert.AreEqual("", Fixture.Off(ctx, loaded.State));

            // And it goes on working after the load, which a field that only round-tripped as a number would not.
            Fixture.Run(ctx, loaded.State, Secs(ctx) * 2);
            Assert.AreEqual(3, loaded.State.Stats.HandCrafted);
            Assert.AreEqual(30, Tray(loaded.State, ItemId.Magazine));
            Assert.AreEqual(0, HandCraft.QueuedBatches(loaded.State));
            Assert.AreEqual("", Fixture.Off(ctx, loaded.State));
        }

        // ---- old saves -------------------------------------------------------------------------------------

        /// <summary>
        /// A pre-schema-6 save carried one recipe, a batch count and the running batch's progress, and the running
        /// batch's ingredients had already been counted as consumed. <see cref="HandCraft.Normalise"/> folds that
        /// into exactly one job, un-counts the sink and holds those units instead — no duplication, no loss — and
        /// is a no-op every tick afterwards.
        /// </summary>
        [Test]
        public void ALegacySingleBatchQueueFoldsIntoOneJobExactlyOnce()
        {
            var (ctx, st) = AtHome();
            var e = st.Engineer;

            // Exactly what the old model left behind: two batches queued, the first running and already paid for.
            e.Inv[ItemId.Steel] = e.Inv[ItemId.Steel] - 2;
            e.Inv[ItemId.Copper] = e.Inv[ItemId.Copper] - 1;
            st.Stats.Consumed.Add(ItemId.Steel, 2);
            st.Stats.Consumed.Add(ItemId.Copper, 1);
            st.Hand.Crafts = 2;
            st.Hand.Crafting = true;
            st.Hand.CraftProg = 7;
            st.Hand.RecipeKey = Bullets;
            Assert.AreEqual("", Fixture.Off(ctx, st), "the staged old state is itself balanced");

            Fixture.Run(ctx, st, Fixture.Dt);
            Assert.AreEqual(1, st.Hand.Jobs.Count);
            Assert.AreEqual(Bullets, st.Hand.Jobs[0].RecipeKey);
            Assert.AreEqual(2, st.Hand.Jobs[0].Batches, "one running plus one queued, not three and not one");
            Assert.AreEqual(7 + Fixture.Dt, st.Hand.Jobs[0].Progress, 1e-9, "the running batch keeps its progress");
            Assert.AreEqual(4, st.Hand.Reserved[ItemId.Steel], "both batches are funded and held");
            Assert.AreEqual(0, st.Stats.Consumed[ItemId.Steel], "the old sink is un-counted, not double counted");
            Assert.AreEqual(16, e.Inv[ItemId.Steel], "the second batch was paid for out of the Backpack");
            Assert.AreEqual(0, st.Hand.Crafts, "and the legacy fields are spent");
            Assert.IsFalse(st.Hand.Crafting);
            Assert.AreEqual(0, st.Hand.CraftProg);
            Assert.AreEqual("", Fixture.Off(ctx, st));

            Fixture.Run(ctx, st, 1);
            Assert.AreEqual(1, st.Hand.Jobs.Count, "the fold does not run again");
            Assert.AreEqual(2, HandCraft.QueuedBatches(st));

            Fixture.Run(ctx, st, Secs(ctx) * 2);
            Assert.AreEqual(2, st.Stats.HandCrafted);
            Assert.AreEqual(20, Tray(st, ItemId.Magazine), "both batches, once each");
            Assert.AreEqual("", Fixture.Off(ctx, st));
        }

        /// <summary>
        /// A pre-schema-6 save can also carry a cancellation debt the Backpack had no room for. Nothing creates one
        /// any more (a cancel is lossless now), but an existing one is still owed and still paid back as space
        /// appears — an old save must not lose those plates.
        /// </summary>
        [Test]
        public void ALegacyCancellationDebtIsStillPaidBackIntoThePockets()
        {
            var (ctx, st) = AtHome();
            st.Hand.RefundSteel = 2;
            st.Hand.RefundCopper = 1;
            Reopen(ctx, st);

            Fixture.Run(ctx, st, Fixture.Dt);
            Assert.AreEqual(22, st.Engineer.Inv[ItemId.Steel]);
            Assert.AreEqual(6, st.Engineer.Inv[ItemId.Copper]);
            Assert.AreEqual(0, st.Hand.RefundSteel);
            Assert.AreEqual(0, st.Hand.RefundCopper);
            Assert.AreEqual("", Fixture.Off(ctx, st));
        }

        // ---- the tray as an inventory (GP-UX-2) --------------------------------------------------------------

        /// <summary>
        /// GP-UX-2. The owner's complaint was that the workshop "just has a button to collect created items instead
        /// of an inventory". The sim could always hand back one item at a time — <see cref="CollectWorkshopCommand"/>
        /// takes an item and a count — but nothing ever asked it for one, because nothing ever drew the tray as
        /// slots. <see cref="HandCraft.TrayStacks"/> is that drawing, and this pins the two properties the panel
        /// leans on.
        ///
        /// First, a chunk is a PRESENTATION of a pooled count, not a place: the tray is an <see cref="ItemBag"/>, so
        /// 25 bullets at a 20-stack is one full chunk and one of five, in canonical key order, and the grid is
        /// always <see cref="HandCraft.OutputStacks"/> long so it does not reflow as goods arrive.
        ///
        /// Second, and the reason the split has to be this rule and not a prettier one: the occupied chunk count
        /// must equal <see cref="HandCraft.TrayUsed"/> exactly. If it did not, the player could be looking at empty
        /// slots while the workshop paused a batch for a full tray — the grid would be calling the workshop a liar.
        /// </summary>
        [Test]
        public void TheOutputTrayReadsAsAGridOfStacksThatAgreesWithWhatTheWorkshopCallsFull()
        {
            var (ctx, st) = AtHome();
            var d = ctx.Data;
            var stack = (double)d.StackSize(ItemKey.Of(ItemId.Magazine));
            Assert.That(stack, Is.GreaterThan(1), "bullets stack, or there is nothing to chunk");
            var rows = new System.Collections.Generic.List<HandCraft.TrayStack>();

            // Empty: a full grid of empty slots, not an empty list. The player sees the tray's SIZE before using it.
            HandCraft.TrayStacks(d, st.Hand.Output, rows);
            Assert.AreEqual(HandCraft.OutputStacks, rows.Count);
            Assert.IsTrue(rows.TrueForAll(r => r.Empty));
            Assert.AreEqual(0, HandCraft.TrayUsed(d, st.Hand.Output));

            // One full stack and a remainder, in that order, and the remainder carries its own stack size so the
            // slot can read "5/20" without asking the catalogue a second time.
            st.Hand.Output[ItemId.Magazine] = stack + 5;
            Reopen(ctx, st);
            HandCraft.TrayStacks(d, st.Hand.Output, rows);
            Assert.AreEqual(HandCraft.OutputStacks, rows.Count);
            Assert.AreEqual(Items.Key(ItemId.Magazine), rows[0].Item);
            Assert.AreEqual(stack, rows[0].Count);
            Assert.AreEqual(stack, rows[0].StackSize);
            Assert.AreEqual(d.Item(ItemId.Magazine).DisplayName, rows[0].DisplayName);
            Assert.AreEqual(5, rows[1].Count);
            Assert.AreEqual(stack, rows[1].StackSize, "a part stack is still measured against a whole one");
            Assert.IsTrue(rows[2].Empty);
            Assert.AreEqual(2, rows.FindAll(r => !r.Empty).Count);
            Assert.AreEqual(2, HandCraft.TrayUsed(d, st.Hand.Output), "the grid and the workshop count alike");

            // A second key joins in canonical order and does not disturb the first.
            st.Hand.Output[ItemId.Steel] = 1;
            Reopen(ctx, st);
            HandCraft.TrayStacks(d, st.Hand.Output, rows);
            var filled = rows.FindAll(r => !r.Empty);
            Assert.AreEqual(3, filled.Count);
            Assert.AreEqual(HandCraft.TrayUsed(d, st.Hand.Output), filled.Count);
            Assert.AreEqual(1, filled.FindAll(r => r.Item == Items.Key(ItemId.Steel)).Count);

            // Full, which is the case the grid exists to make legible: every slot occupied and none left over.
            st.Hand.Output[ItemId.Steel] = 0;
            st.Hand.Output[ItemId.Magazine] = HandCraft.OutputStacks * stack;
            Reopen(ctx, st);
            HandCraft.TrayStacks(d, st.Hand.Output, rows);
            Assert.AreEqual(HandCraft.OutputStacks, rows.Count);
            Assert.IsTrue(rows.TrueForAll(r => !r.Empty && r.Count == stack));
            Assert.AreEqual(HandCraft.OutputStacks, HandCraft.TrayUsed(d, st.Hand.Output));
            Assert.AreEqual(0, HandCraft.TrayFree(d, st.Hand.Output));
        }

        /// <summary>
        /// The grid's whole point: dragging ONE slot out takes that stack and leaves the rest. Collect already
        /// supported it; this is the first caller to ask, so it is the first test to check that the rest stays.
        /// </summary>
        [Test]
        public void CollectingOneStackLeavesTheRestOfTheTrayAlone()
        {
            var (ctx, st) = AtHome();
            var d = ctx.Data;
            var stack = (double)d.StackSize(ItemKey.Of(ItemId.Magazine));
            st.Hand.Output[ItemId.Magazine] = stack + 5;
            st.Hand.Output[ItemId.Steel] = 3;
            Reopen(ctx, st);
            var carried = st.Engineer.Inv[ItemId.Steel];

            var r = Fixture.Apply(ctx, st, new CollectWorkshopCommand(Items.Key(ItemId.Magazine), stack));
            Assert.IsTrue(r.Accepted, r.Problem);
            Assert.AreEqual(5, Tray(st, ItemId.Magazine), "one stack left, not the lot");
            Assert.AreEqual(3, Tray(st, ItemId.Steel), "a different key is untouched");
            Assert.AreEqual(stack, st.Engineer.Inv[ItemId.Magazine]);
            Assert.AreEqual(carried, st.Engineer.Inv[ItemId.Steel]);
            Assert.AreEqual("", Fixture.Off(ctx, st));
        }
    }
}
