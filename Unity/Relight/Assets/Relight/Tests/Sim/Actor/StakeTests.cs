using NUnit.Framework;

namespace Relight.Sim.Tests
{
    /// <summary>
    /// The starting stake is data (CONTENT_CATALOGUE §15): what a new game finds in the engineer's pockets comes
    /// from <see cref="GameData.Stake"/>, so an edit to the `Tuning - Starting stake` asset changes the opening.
    /// These tests pin that, pin the ledger order (the opening stock is taken AFTER the pockets are filled, so
    /// conservation reads zero at tick 0 — U-D-05/U-D-18), and pin the refusal path: a stake that cannot be
    /// trusted is reported by <see cref="InventoryInitializer.StakeProblem"/> and the reference stake is used.
    /// </summary>
    public sealed class StakeTests
    {
        /// <summary>The exported catalogue with one table replaced — the fixture shape <c>ReferenceData.Create</c> uses.</summary>
        private static GameData DataWithStake(StartingStake stake) => new GameData(
            CatalogueData.Items(), CatalogueData.Machines(), CatalogueData.Recipes(),
            CatalogueData.Engineer(), CatalogueData.World(),
            CatalogueData.Weapons(), CatalogueData.Enemies(), CatalogueData.Ammunition(), CatalogueData.Turrets(),
            CatalogueData.Power(), CatalogueData.Time(), CatalogueData.Raids(), CatalogueData.Opening(), stake);

        private static SimContext ContextWithStake(StartingStake stake) =>
            new SimContext(DataWithStake(stake), new FlatGeometry());

        private static StartingStake Stake(params ItemStack[] pockets) =>
            new StartingStake(pockets, SimVersion.Ruleset, "approved", "test", false);

        [Test]
        public void TheCatalogueStakeIsWhatTheDefaultFixtureStartsWith()
        {
            var ctx = Fixture.Context();
            Assert.AreEqual("", InventoryInitializer.StakeProblem(ctx.Data));

            var st = Fixture.State(ctx);
            Assert.AreEqual(20, st.Engineer.Inv[ItemId.Steel]);
            Assert.AreEqual(5, st.Engineer.Inv[ItemId.Copper]);
            Assert.AreEqual("", Fixture.Off(ctx, st));
        }

        [Test]
        public void AnEditedStakeIsWhatTheEngineerStartsWith()
        {
            var ctx = ContextWithStake(Stake(
                new ItemStack(ItemId.Steel, 7), new ItemStack(ItemId.Copper, 3), new ItemStack(ItemId.Coal, 4)));

            var st = Fixture.State(ctx);
            Assert.AreEqual("", InventoryInitializer.StakeProblem(ctx.Data));
            Assert.AreEqual(7, st.Engineer.Inv[ItemId.Steel]);
            Assert.AreEqual(3, st.Engineer.Inv[ItemId.Copper]);
            Assert.AreEqual(4, st.Engineer.Inv[ItemId.Coal]);
        }

        [Test]
        public void TheLedgerOpensOnTheStakeSoConservationReadsZero()
        {
            var ctx = ContextWithStake(Stake(new ItemStack(ItemId.Steel, 7), new ItemStack(ItemId.Coal, 4)));
            var st = Fixture.State(ctx);

            var view = LedgerQueries.Conservation(ctx, st);
            Assert.IsTrue(view.Ok, Fixture.Off(ctx, st));
            Assert.AreEqual(0, view.OpenedAt, "the ledger opens at tick 0");
            Assert.AreEqual(7, view.Opening[ItemId.Steel], "the opening stock is the stake, not the old constant");
            Assert.AreEqual(4, view.Opening[ItemId.Coal]);
            Assert.AreEqual(0, view.Opening[ItemId.Copper], "nothing the stake does not name is in the opening stock");
            Assert.AreEqual("", Fixture.Off(ctx, st), "tick 0");

            Fixture.Run(ctx, st, 2.0);
            Assert.AreEqual("", Fixture.Off(ctx, st), "after 2 s of ticks");
        }

        [Test]
        public void ANewGameGoesThroughTheSameStake()
        {
            var ctx = ContextWithStake(Stake(new ItemStack(ItemId.Steel, 11), new ItemStack(ItemId.Copper, 2)));
            var sim = Simulation.NewGame(ctx, 7);

            Assert.AreEqual(11, sim.State.Engineer.Inv[ItemId.Steel]);
            Assert.AreEqual(2, sim.State.Engineer.Inv[ItemId.Copper]);
            Assert.AreEqual("", Fixture.Off(ctx, sim.State));

            for (var i = 0; i < 40; i++) sim.Tick();
            Assert.AreEqual("", Fixture.Off(ctx, sim.State), "after 40 ticks");
        }

        [Test]
        public void AStakeThatNamesAnItemTwiceIsOneStackOfTheSum()
        {
            var ctx = ContextWithStake(Stake(new ItemStack(ItemId.Steel, 4), new ItemStack(ItemId.Steel, 6)));
            var st = Fixture.State(ctx);
            Assert.AreEqual(10, st.Engineer.Inv[ItemId.Steel]);
            Assert.AreEqual("", Fixture.Off(ctx, st));
        }

        [Test]
        public void AStakeForAnotherRulesetIsRefusedAndReported()
        {
            var ctx = ContextWithStake(new StartingStake(
                new[] { new ItemStack(ItemId.Steel, 999) }, "legacy-v1", "approved", "test", false));

            var problem = InventoryInitializer.StakeProblem(ctx.Data);
            Assert.That(problem, Does.Contain("legacy-v1"), "the mismatch must say which ruleset it found");
            Assert.That(problem, Does.Contain(SimVersion.Ruleset), "and which one the sim runs");

            var st = Fixture.State(ctx);
            Assert.AreEqual(InventoryInitializer.StartSteel, st.Engineer.Inv[ItemId.Steel], "the foreign stake was used");
            Assert.AreEqual(InventoryInitializer.StartCopper, st.Engineer.Inv[ItemId.Copper]);
            Assert.AreEqual("", Fixture.Off(ctx, st), "the fallback still opens a balanced ledger");
        }

        [Test]
        public void AMissingOrEmptyStakeFallsBackToTheReferenceOpening()
        {
            var none = new SimContext(DataWithStake(null), new FlatGeometry());
            Assert.That(InventoryInitializer.StakeProblem(none.Data), Is.Not.Empty);
            var st = Fixture.State(none);
            Assert.AreEqual(InventoryInitializer.StartSteel, st.Engineer.Inv[ItemId.Steel]);
            Assert.AreEqual(InventoryInitializer.StartCopper, st.Engineer.Inv[ItemId.Copper]);
            Assert.AreEqual("", Fixture.Off(none, st));

            var empty = ContextWithStake(Stake());
            Assert.That(InventoryInitializer.StakeProblem(empty.Data), Is.Not.Empty);
            Assert.AreEqual(InventoryInitializer.StartSteel, Fixture.State(empty).Engineer.Inv[ItemId.Steel]);
        }

        [Test]
        public void ANonPositiveStackIsRefusedRatherThanSubtractedFromThePockets()
        {
            var ctx = ContextWithStake(Stake(new ItemStack(ItemId.Steel, 5), new ItemStack(ItemId.Copper, -2)));
            Assert.That(InventoryInitializer.StakeProblem(ctx.Data), Does.Contain("copper"));

            var st = Fixture.State(ctx);
            Assert.AreEqual(InventoryInitializer.StartSteel, st.Engineer.Inv[ItemId.Steel]);
            Assert.AreEqual(InventoryInitializer.StartCopper, st.Engineer.Inv[ItemId.Copper]);
        }
    }
}
