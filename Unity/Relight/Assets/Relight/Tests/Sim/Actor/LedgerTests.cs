using NUnit.Framework;

namespace Relight.Sim.Tests
{
    /// <summary>
    /// Conservation, ported from the structure of packages/sim/test/ledger.test.ts (the reference cases drive the
    /// whole city/hour sim, which Phase B has not ported; the sequences here are the Phase B equivalents that
    /// exercise the same ledger paths).
    /// </summary>
    public sealed class LedgerTests
    {
        [Test]
        public void AFreshStateBalancesAndOpensOnTheStartingPockets()
        {
            var ctx = Fixture.Context();
            var st = Fixture.State(ctx);
            var view = LedgerQueries.Conservation(ctx, st);
            Assert.IsTrue(view.Ok, Fixture.Off(ctx, st));
            Assert.AreEqual(0, view.OpenedAt);
            Assert.AreEqual(20, view.Opening[ItemId.Steel]);
            Assert.AreEqual(5, view.Opening[ItemId.Copper]);
            Assert.AreEqual(20, view.At(LedgerPlace.Pockets, ItemId.Steel));
        }

        [Test]
        public void TakeDropAndAChestTransferConserveEverything()
        {
            var ctx = Fixture.Context();
            var st = Fixture.State(ctx);
            var chest = Fixture.Place(ctx, st, "chest", 18, 16);

            var put = Fixture.Apply(ctx, st, new MachineTransferCommand(chest.Id, ItemId.Steel, 12, true));
            Assert.IsTrue(put.Accepted, put.Problem);
            Assert.AreEqual("12 Steel plates loaded", put.Problem);
            Assert.AreEqual(8, st.Engineer.Inv[ItemId.Steel]);
            Assert.AreEqual(12, chest.Inv[ItemId.Steel]);
            Assert.AreEqual("", Fixture.Off(ctx, st));

            var take = Fixture.Apply(ctx, st, new MachineTransferCommand(chest.Id, ItemId.Steel, 5, false));
            Assert.IsTrue(take.Accepted, take.Problem);
            Assert.AreEqual("5 Steel plates taken", take.Problem);
            Assert.AreEqual(13, st.Engineer.Inv[ItemId.Steel]);
            Assert.AreEqual(7, chest.Inv[ItemId.Steel]);

            var v = LedgerQueries.Conservation(ctx, st);
            Assert.IsTrue(v.Ok, Fixture.Off(ctx, st));
            Assert.AreEqual(13, v.At(LedgerPlace.Pockets, ItemId.Steel));
            Assert.AreEqual(7, v.At(LedgerPlace.Machines, ItemId.Steel));
            Assert.AreEqual(20, v.Held[ItemId.Steel], "nothing entered or left the world");
        }

        [Test]
        public void ARefusedTransferMovesNothingAndStillBalances()
        {
            var ctx = Fixture.Context();
            var st = Fixture.State(ctx);
            var chest = Fixture.Place(ctx, st, "chest", 18, 16);

            var none = Fixture.Apply(ctx, st, new MachineTransferCommand(chest.Id, ItemId.Coal, 4, true));
            Assert.IsFalse(none.Accepted);
            Assert.AreEqual("No Coal in Backpack", none.Problem);

            var empty = Fixture.Apply(ctx, st, new MachineTransferCommand(chest.Id, ItemId.Steel, 1, false));
            Assert.IsFalse(empty.Accepted);
            Assert.AreEqual("No whole Steel plates available", empty.Problem);

            st.Engineer.Pos = new Vec2(2, 2);
            var far = Fixture.Apply(ctx, st, new MachineTransferCommand(chest.Id, ItemId.Steel, 1, true));
            Assert.IsFalse(far.Accepted);
            Assert.AreEqual("Walk closer to the Supply chest", far.Problem);

            Assert.AreEqual(20, st.Engineer.Inv[ItemId.Steel]);
            Assert.IsTrue(chest.Inv.IsEmpty);
            Assert.AreEqual("", Fixture.Off(ctx, st));
        }

        [Test]
        public void RemovingAMachineReturnsItsContentsAndBalances()
        {
            var ctx = Fixture.Context();
            var st = Fixture.State(ctx);
            var chest = Fixture.Place(ctx, st, "chest", 18, 16);
            Assert.IsTrue(Fixture.Apply(ctx, st, new MachineTransferCommand(chest.Id, ItemId.Steel, 20, true)).Accepted);
            Assert.AreEqual(0, st.Engineer.Inv[ItemId.Steel]);

            var gone = Fixture.Apply(ctx, st, new RemoveMachineCommand(chest.Id));
            Assert.IsTrue(gone.Accepted, gone.Problem);
            Assert.AreEqual(0, st.Machines.Count);
            Assert.AreEqual(20, st.Engineer.Inv[ItemId.Steel], "the contents came back");
            Assert.AreEqual(1, st.Engineer.Inv[new ItemKey("chest")], "and so did the machine itself");
            Assert.AreEqual("", Fixture.Off(ctx, st));
            Assert.IsFalse(Fixture.Apply(ctx, st, new RemoveMachineCommand(chest.Id)).Accepted, "already removed");
        }

        [Test]
        public void ATurretsRoundsSurviveFeedingAndRemoval()
        {
            var ctx = Fixture.Context();
            var st = Fixture.State(ctx);
            var turret = Fixture.Place(ctx, st, "turret", 22, 16);
            st.Engineer.Inv[ItemId.Magazine] = 30;
            st.Stats.Made[ItemId.Magazine] = 30;   // the only honest way to have them: they were made
            Assert.AreEqual("", Fixture.Off(ctx, st));

            var feed = Fixture.Apply(ctx, st, new MachineTransferCommand(turret.Id, ItemId.Magazine, 30, true));
            Assert.IsTrue(feed.Accepted, feed.Problem);
            Assert.AreEqual(50, TurretHopper.Capacity(ctx.Data, turret));   // the helper moved to W-B's kind-agnostic hopper
            Assert.AreEqual(30, turret.Rounds);
            Assert.AreEqual(0, st.Engineer.Inv[ItemId.Magazine]);
            Assert.AreEqual(30, st.Stats.HandFedMags);
            Assert.AreEqual("", Fixture.Off(ctx, st));

            Assert.IsTrue(Fixture.Apply(ctx, st, new RemoveMachineCommand(turret.Id)).Accepted);
            Assert.AreEqual(30, st.Engineer.Inv[ItemId.Magazine], "one round is one bullet (U-D-08): nothing is lost");
            Assert.AreEqual(0, st.Stats.RoundsLost);
            Assert.AreEqual("", Fixture.Off(ctx, st));
        }

        [Test]
        public void AnInjectedItemIsReportedAndAReopenedLedgerReadsZero()
        {
            var ctx = Fixture.Context();
            var st = Fixture.State(ctx);
            st.Engineer.Inv[ItemId.Coal] = 7;   // no source counted it

            var bad = LedgerQueries.Conservation(ctx, st);
            Assert.IsFalse(bad.Ok);
            Assert.AreEqual(1, bad.Problems.Count);
            Assert.AreEqual("coal: +7.00 unexplained (opening 0.0 + sources 0.0 = held 7.0 + sinks 0.0)", bad.Problems[0]);
            Assert.AreEqual(7, bad.Unexplained[ItemId.Coal]);

            st.Tick = 40;
            st.Ledger = Ledger.Open(st, ctx.Data);
            var reopened = LedgerQueries.Conservation(ctx, st);
            Assert.IsTrue(reopened.Ok, string.Join(" | ", reopened.Problems));
            Assert.AreEqual(40, reopened.OpenedAt);
            Assert.AreEqual(7, reopened.Opening[ItemId.Coal]);
        }

        /// <summary>
        /// GP-W5. Dying costs the engineer the WALK BACK, not the cargo. Conservation is the whole of the rule:
        /// every unit that leaves the pockets is on the ground, counted where it stands, and every unit collected
        /// comes back — nothing is a source and nothing is a sink at either end.
        ///
        /// Two things are asserted here that the sim cannot express any other way. An unequipped weapon is held in
        /// the bag as its own key (<c>rifle:3</c>), so "it is not in the backpack" is not true of the
        /// representation and the retain-through-death rule has to be enforced in <see cref="DeathCache.Spill"/>;
        /// and two deaths on one tile merge into one pile, so repeated deaths cannot double anything.
        /// </summary>
        [Test]
        public void DyingDropsTheCargoWhereTheBodyFellAndCollectingItBringsEveryUnitBack()
        {
            var ctx = Fixture.Context();
            var st = Fixture.State(ctx);
            var rifle = new ItemKey("rifle:3");
            st.Engineer.Inv[rifle] = 1;                     // an unequipped weapon: never dropped, never ledger-counted

            var fell = new Vec2(10.5, 12.5);
            st.Engineer.Pos = fell;
            st.Engineer.TakeDamage(ctx, st, ctx.Data.Engineer.MaxHp);

            Assert.IsTrue(st.Engineer.IsDown);
            Assert.AreEqual(0, st.Engineer.Inv[ItemId.Steel], "the cargo went to the ground");
            Assert.AreEqual(0, st.Engineer.Inv[ItemId.Copper]);
            Assert.AreEqual(1, st.Engineer.Inv[rifle], "the rifle did not: losing it is what stops people exploring");
            Assert.AreEqual(1, st.Drops.Caches.Count);

            var pile = st.Drops.Caches[0];
            Assert.AreEqual(10, pile.X, "on the tile the body fell on");
            Assert.AreEqual(12, pile.Y);
            Assert.AreEqual(20, pile.Items[ItemKey.Of(ItemId.Steel)]);
            Assert.AreEqual(5, pile.Items[ItemKey.Of(ItemId.Copper)]);
            Assert.AreEqual("", Fixture.Off(ctx, st), "a pile is held where it stands, not lost");

            var v = LedgerQueries.Conservation(ctx, st);
            Assert.AreEqual(0, v.At(LedgerPlace.Pockets, ItemId.Steel));
            Assert.AreEqual(20, v.At(LedgerPlace.Machines, ItemId.Steel), "counted on the workshop tray's precedent");
            Assert.AreEqual(20, v.Held[ItemId.Steel], "nothing entered or left the world");

            // Die again on the same tile: one pile, not two, and still nothing invented.
            st.Engineer.Down = -1;
            st.Engineer.Hp = ctx.Data.Engineer.MaxHp;
            st.Engineer.Inv[ItemId.Coal] = 3;
            st.Stats.Made[ItemId.Coal] = 3;                 // the only honest way to have them: they were made
            st.Engineer.TakeDamage(ctx, st, ctx.Data.Engineer.MaxHp);

            Assert.AreEqual(1, st.Drops.Caches.Count, "two deaths on one tile are one pile, not a second marker");
            Assert.AreEqual(3, pile.Items[ItemKey.Of(ItemId.Coal)]);
            Assert.AreEqual("", Fixture.Off(ctx, st));

            // Who may collect it, and from where.
            Assert.AreEqual("Engineer down", Fixture.Apply(ctx, st, new CollectCacheCommand()).Problem);
            st.Engineer.Down = -1;
            st.Engineer.Hp = ctx.Data.Engineer.MaxHp;
            st.Engineer.Pos = new Vec2(2, 2);
            Assert.AreEqual("No dropped cargo within reach.", Fixture.Apply(ctx, st, new CollectCacheCommand()).Problem);
            Assert.AreEqual(DeathCache.ReachText, Fixture.Apply(ctx, st, new CollectCacheCommand(pile.Id)).Problem);
            Assert.AreEqual("", Fixture.Off(ctx, st), "a refused collect moves nothing");

            st.Engineer.Pos = fell;
            var got = Fixture.Apply(ctx, st, new CollectCacheCommand(pile.Id));
            Assert.IsTrue(got.Accepted, got.Problem);
            Assert.AreEqual("Recovered 28 from the dropped cargo.", got.Problem);
            Assert.AreEqual(20, st.Engineer.Inv[ItemId.Steel], "every unit came back");
            Assert.AreEqual(5, st.Engineer.Inv[ItemId.Copper]);
            Assert.AreEqual(3, st.Engineer.Inv[ItemId.Coal]);
            Assert.AreEqual(1, st.Engineer.Inv[rifle]);
            Assert.AreEqual(0, st.Drops.Caches.Count, "and an emptied pile is not left lying on the map");
            Assert.AreEqual("", Fixture.Off(ctx, st));
            Assert.AreEqual("There is no dropped cargo to collect.",
                Fixture.Apply(ctx, st, new CollectCacheCommand()).Problem);
        }
    }
}
