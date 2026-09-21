using NUnit.Framework;
using Relight.Sim.Tests.Combat;
using Relight.Sim.Tests.Production;
using Relight.Sim.UI;

namespace Relight.Sim.Tests.UI
{
    /// <summary>
    /// GP-W6, "fuel, power and failure readability": one fuel-time formula and one wording for it, a low-fuel
    /// warning that is posted once and teaches a reserve, generator delivery by inserter and by belt with the
    /// ledger balanced, a standing mark for a shortage, and problem rows that are ranked and grouped.
    /// </summary>
    public sealed class PowerReadabilityTests
    {
        // ---- the fuel estimate --------------------------------------------------------------------------------

        [Test]
        public void FuelTimeIsTheInverseOfTheBurn()
        {
            var ctx = ProductionFixture.Context();
            var mj = ctx.Data.Power.CoalMj;
            Assert.That(PowerQueries.FuelSecondsAt(ctx.Data, 50, 300), Is.EqualTo(50 * mj * 1000 / 300).Within(1e-9));
            Assert.That(PowerQueries.FuelSecondsAt(ctx.Data, 50, 0), Is.EqualTo(double.PositiveInfinity),
                "nothing drawing means the fuel is not running out");
            Assert.That(PowerQueries.FuelSecondsAt(ctx.Data, -3, 300), Is.Zero);
        }

        /// <summary>The estimate against the real burn: a Generator run flat out empties when the formula said it would.</summary>
        [Test]
        public void TheEstimateMatchesHowLongTheFuelActuallyLasts()
        {
            var ctx = ProductionFixture.Context();
            var st = ProductionFixture.State(ctx);
            var (_, gen) = ProductionFixture.Grid(ctx, st, 20, 18, 18, 14, 2);
            for (var i = 0; i < 3; i++) ProductionFixture.Add(ctx, st, "assembler", 12 + i * 3, 20);   // 300 kW of demand
            ProductionFixture.Run(ctx, st, 1);

            var estimate = PowerQueries.GeneratorFuelSeconds(ctx, st, gen.Id);
            Assert.That(estimate, Is.GreaterThan(0).And.LessThan(60));
            Assert.That(PowerQueries.Network(ctx, st).FuelSeconds, Is.EqualTo(estimate).Within(1e-6),
                "one generator: the strip's figure and the generator's own are the same number");

            var t0 = st.T;
            var ticks = 0;
            while (PowerGrid.FuelUnits(gen) > 0 && ticks++ < 4000) ProductionFixture.Run(ctx, st, 1);
            Assert.That(st.T - t0, Is.EqualTo(estimate).Within(1.0), "within a second over the whole burn");
        }

        [Test]
        public void FuelTimeIsWordedCoarselyBecauseItIsAnEstimate()
        {
            Assert.That(PowerQueries.FuelTimeText(4), Is.EqualTo("under 10 s"));
            Assert.That(PowerQueries.FuelTimeText(44), Is.EqualTo("about 40 s"));
            Assert.That(PowerQueries.FuelTimeText(240), Is.EqualTo("about 4 min"));
            Assert.That(PowerQueries.FuelTimeText(5000), Is.EqualTo("over an hour"));
            Assert.That(PowerQueries.FuelTimeText(double.NaN), Is.EqualTo("under 10 s"));
        }

        [Test]
        public void AGeneratorsOwnLineCallsItsFuelTimeAnEstimate()
        {
            var ctx = ProductionFixture.Context();
            var st = ProductionFixture.State(ctx);
            var (_, gen) = ProductionFixture.Grid(ctx, st, 20, 18, 18, 14, 20);
            ProductionFixture.Add(ctx, st, "assembler", 12, 20);
            ProductionFixture.Run(ctx, st, 1);

            var line = PowerQueries.Description(ctx, st, gen.Id);
            Assert.That(line, Does.Contain("(estimate)"));
            Assert.That(line, Does.Contain(PowerQueries.FuelTimeText(PowerQueries.GeneratorFuelSeconds(ctx, st, gen.Id))));
        }

        // ---- the low-fuel warning -----------------------------------------------------------------------------

        [Test]
        public void LowFuelIsPostedOnceTeachesAReserveAndClearsWhenRefuelled()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            HomeCore.Ensure(ctx, st);
            RaidFixture.Add(ctx, st, "pole", 20, 18);
            var gen = RaidFixture.Add(ctx, st, "generator", 18, 14);
            gen.Inv.Add(ItemId.Coal, 2);
            for (var i = 0; i < 3; i++) RaidFixture.Add(ctx, st, "assembler", 12 + i * 3, 20);
            ProductionFixture.Run(ctx, st, 1);

            var vm = new HudViewModel();
            vm.Refresh(ctx, st, 0, false, false, force: true);
            Assert.That(vm.FuelLow, Is.True);
            Assert.That(vm.FuelText, Does.Contain("(estimate)"));
            var row = Row(vm, PowerAlertSource.LowFuelKey);
            Assert.That(row, Is.Not.Null, "the warning is on the HUD");
            Assert.That(row.Text, Is.EqualTo(PowerAlertSource.LowFuelLine(ctx.Data)));
            Assert.That(row.Text, Does.Contain("A full slot of"), "it teaches the reserve, not just the alarm");
            Assert.That(row.Text, Does.Contain(PowerQueries.FuelTimeText(PowerQueries.FullSlotSeconds(ctx.Data))));

            // Standing, not churning (U-D-55): refreshed again and again, it is still one row that never repeated.
            for (var i = 1; i <= 5; i++) vm.Refresh(ctx, st, i, false, false, force: true);
            Assert.That(Row(vm, PowerAlertSource.LowFuelKey).Repeats, Is.EqualTo(1));

            gen.Inv.Add(ItemId.Coal, 40);
            ProductionFixture.Run(ctx, st, 1);                          // the grid's fuel figure is the power tick's
            vm.Refresh(ctx, st, 7, false, false, force: true);
            Assert.That(vm.FuelLow, Is.False);
            Assert.That(Row(vm, PowerAlertSource.LowFuelKey), Is.Null, "refuelled: the row is gone");
        }

        [Test]
        public void AnIdleGeneratorIsNotLowOnFuel()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            HomeCore.Ensure(ctx, st);
            RaidFixture.Power(ctx, st, 20, 18, coal: 1);               // one coal and nothing drawing on it
            ProductionFixture.Run(ctx, st, 1);

            var vm = new HudViewModel();
            vm.Refresh(ctx, st, 0, false, false, force: true);
            Assert.That(vm.FuelLow, Is.False);
            Assert.That(vm.FuelText, Is.Empty, "no load, no estimate: a time would be a guess");
        }

        // ---- generator delivery, with the ledger -------------------------------------------------------------

        [Test]
        public void AnInserterFeedsAGeneratorFromAChestAndEveryCoalIsAccountedFor()
        {
            var ctx = FlowFixture.Context();
            var st = FlowFixture.State(ctx);
            FlowFixture.Add(ctx, st, "pole", 20, 20);
            var chest = FlowFixture.Add(ctx, st, "chest", 14, 17);
            FlowFixture.Add(ctx, st, "inserter", 16, 17, Dir.E);
            var gen = FlowFixture.Add(ctx, st, "generator", 17, 17);
            gen.Inv.Add(ItemId.Coal, 1);                                // enough to swing the arm the first time
            chest.Inv.Add(ItemId.Coal, 10);
            FlowFixture.Seal(ctx, st);

            FlowFixture.Run(ctx, st, 20 * 8);

            Assert.That(chest.Inv[ItemId.Coal], Is.LessThan(10), "the arm took coal out of the chest");
            Assert.That(st.Stats.GenFed, Is.GreaterThan(0), "and the generator counted it in");
            Assert.That(FlowFixture.Off(ctx, st), Is.Empty, "chest + hand + slot + burned explains every unit");
        }

        [Test]
        public void TheGeneratorsSlotIsFiniteSoAnInserterStopsWhenItIsFull()
        {
            var ctx = FlowFixture.Context();
            var st = FlowFixture.State(ctx);
            FlowFixture.Add(ctx, st, "pole", 20, 20);
            var chest = FlowFixture.Add(ctx, st, "chest", 14, 17);
            FlowFixture.Add(ctx, st, "inserter", 16, 17, Dir.E);
            var gen = FlowFixture.Add(ctx, st, "generator", 17, 17);
            var cap = (int)MachineInventory.GeneratorFuelCap(ctx.Data);
            gen.Inv.Add(ItemId.Coal, cap);
            chest.Inv.Add(ItemId.Coal, 10);
            FlowFixture.Seal(ctx, st);

            FlowFixture.Run(ctx, st, 20 * 5);

            Assert.That(PowerGrid.FuelUnits(gen), Is.LessThanOrEqualTo(cap), "never above the slot");
            Assert.That(FlowFixture.Off(ctx, st), Is.Empty);
        }

        [Test]
        public void ABeltFeedsAGeneratorWithNoInserterAndEveryCoalIsAccountedFor()
        {
            var ctx = FlowFixture.Context();
            var st = FlowFixture.State(ctx);
            var chest = FlowFixture.Add(ctx, st, "chest", 2, 6);
            for (var i = 0; i < 3; i++) FlowFixture.Add(ctx, st, "belt", 4 + i, 6, Dir.E);
            var gen = FlowFixture.Add(ctx, st, "generator", 7, 6);
            chest.Inv.Add(ItemId.Coal, 5);
            FlowFixture.Seal(ctx, st);

            FlowFixture.Run(ctx, st, 20 * 10);

            Assert.That(st.Stats.GenFed, Is.EqualTo(5), "all five arrived");
            Assert.That(PowerGrid.FuelUnits(gen), Is.EqualTo(5).Within(1e-6), "nothing draws, so nothing burned");
            Assert.That(FlowFixture.Off(ctx, st), Is.Empty);
        }

        // ---- the shortage -------------------------------------------------------------------------------------

        [Test]
        public void AShortageStandsOnTheStripAndGetsOneRowNamingHowManyMachinesItSlows()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            HomeCore.Ensure(ctx, st);
            RaidFixture.Add(ctx, st, "pole", 20, 18);
            RaidFixture.Add(ctx, st, "generator", 18, 14).Inv.Add(ItemId.Coal, 50);
            for (var i = 0; i < 4; i++) RaidFixture.Add(ctx, st, "assembler", 12 + i * 3, 20);   // 400 kW against 300
            RaidFixture.Add(ctx, st, "turret", 16, 15).Rounds = 20;
            RaidFixture.Add(ctx, st, "turret", 22, 15).Rounds = 20;
            ProductionFixture.Run(ctx, st, 1);

            var vm = new HudViewModel();
            vm.Refresh(ctx, st, 0, false, false, force: true);

            Assert.That(vm.PowerShort, Is.True);
            Assert.That(vm.PowerOff, Is.False);
            Assert.That(vm.PowerText, Does.EndWith(PowerAlertSource.ShortSuffix));
            Assert.That(vm.PowerFraction, Is.GreaterThan(0).And.LessThan(1), "the meter shows what is delivered, not 100%");

            HudProblem? low = null;
            for (var i = 0; i < vm.Problems.Count; i++)
                if (vm.Problems[i].State == MachineOperatingState.Throttled) low = vm.Problems[i];
            Assert.That(low.HasValue, Is.True, "the shortage has a persistent row");
            Assert.That(low.Value.Count, Is.EqualTo(2), "both slowed turrets, on one row");
            Assert.That(low.Value.Text, Is.EqualTo("Low power: 2 machines "
                + ProductionQueries.StateText(MachineOperatingState.Throttled) + " — " + HudViewModel.ThrottledRemedy));
        }

        // ---- the problem rows ---------------------------------------------------------------------------------

        [Test]
        public void MachinesOfOneKindInOneStateShareARow()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            HomeCore.Ensure(ctx, st);
            for (var i = 0; i < 8; i++) RaidFixture.Add(ctx, st, "assembler", 20 + i * 4, 20);

            var vm = new HudViewModel();
            vm.Refresh(ctx, st, 0, false, false, force: true);

            Assert.That(vm.Problems.Count, Is.EqualTo(1), "eight identical rows used to fill the list three times over");
            Assert.That(vm.Problems[0].Count, Is.EqualTo(8));
            Assert.That(vm.Problems[0].Text, Does.StartWith("8 × Assembler: "));
        }

        [Test]
        public void TheWorstProblemIsFirstHoweverLateItWasBuilt()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            HomeCore.Ensure(ctx, st);
            RaidFixture.Add(ctx, st, "assembler", 20, 20);
            RaidFixture.Add(ctx, st, "foundry", 24, 20);
            RaidFixture.Add(ctx, st, "refinery", 28, 20);
            RaidFixture.Add(ctx, st, "generator", 40, 40);              // empty, and placed last

            var vm = new HudViewModel();
            vm.Refresh(ctx, st, 0, false, false, force: true);

            Assert.That(vm.Problems[0].State, Is.EqualTo(MachineOperatingState.OutOfFuel),
                "placement order used to push this off the end of the list");
            Assert.That(HudViewModel.Rank(MachineOperatingState.Disabled),
                Is.LessThan(HudViewModel.Rank(MachineOperatingState.OutOfFuel)));
            Assert.That(HudViewModel.Rank(MachineOperatingState.Throttled),
                Is.LessThan(HudViewModel.Rank(MachineOperatingState.NoInput)));
        }

        /// <summary>"Connect it to a powered pole" is the wrong advice for a machine that IS connected.</summary>
        [Test]
        public void ConnectedMachinesOnADeadCircuitAreToldToFuelAGeneratorNotToFindAPole()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            HomeCore.Ensure(ctx, st);
            RaidFixture.Add(ctx, st, "pole", 20, 18);
            RaidFixture.Add(ctx, st, "generator", 18, 14);              // linked, and empty
            RaidFixture.Add(ctx, st, "assembler", 12, 20);
            RaidFixture.Add(ctx, st, "foundry", 16, 20);

            var vm = new HudViewModel();
            vm.Refresh(ctx, st, 0, false, false, force: true);

            var found = false;
            for (var i = 0; i < vm.Problems.Count; i++)
            {
                var p = vm.Problems[i];
                if (p.State != MachineOperatingState.Unpowered) continue;
                found = true;
                Assert.That(p.Count, Is.EqualTo(2), "both kinds, one row: it is the circuit's problem");
                Assert.That(p.Text, Does.Contain(HudViewModel.UnsuppliedRemedy));
                Assert.That(p.Text, Does.Not.Contain(HudViewModel.Remedy(MachineOperatingState.Unpowered)));
            }
            Assert.That(found, Is.True);
        }

        // ---- no identifier reaches the player ------------------------------------------------------------------

        [Test]
        public void EveryShippedMachineAndItemHasANameSoNoKeyIsEverTheFallback()
        {
            var d = DarkWorld.Apply(CombatBalance.Apply(OpeningBalance.Apply(ReferenceData.Create())));
            foreach (var m in d.Machines)
                Assert.That(m.DisplayName, Is.Not.Null.And.Not.Empty.And.Not.EqualTo(m.Key), m.Key);
            foreach (var it in d.Items)
            {
                Assert.That(it.DisplayName, Is.Not.Null.And.Not.Empty, it.Key);
                Assert.That(it.DisplayName, Does.Not.Contain("-").And.Not.Contain("_"), it.Key + " reads as a key");
            }
            foreach (var w in d.Weapons) Assert.That(w.DisplayName, Is.Not.Null.And.Not.Empty, w.Key);
            foreach (var e in d.Enemies) Assert.That(e.DisplayName, Is.Not.Null.And.Not.Empty, e.Key);
        }

        // ---- one source per figure ------------------------------------------------------------------------------

        /// <summary>
        /// The objective card's "have" and the Backpack's count are the same carried stock, and what is at Home is
        /// never counted as carried. Pinned so neither surface can grow a count of its own.
        /// </summary>
        [Test]
        public void TheObjectiveCountsWhatTheBackpackCounts()
        {
            var ctx = RaidFixture.Context();
            var st = Relight.Sim.Tests.Campaign.OpeningFixture.State(ctx);
            st.Engineer.Inv[ItemId.Steel] = 7;

            var next = OpeningQueries.Objective(ctx, st);
            Assert.That(next.Materials, Is.Not.Empty, "the fixture must start on a row that needs materials");
            var carried = InventoryQueries.Carried(ctx, st);
            for (var i = 0; i < next.Materials.Count; i++)
            {
                var row = next.Materials[i];
                double pack = 0;
                for (var c = 0; c < carried.Count; c++) if (carried[c].Key == row.Item) pack = carried[c].Count;
                Assert.That(row.Available, Is.EqualTo((int)System.Math.Floor(pack)), row.Item);
            }
        }

        [Test]
        public void TheLastResortNameIsStillWordsNotAKey()
        {
            Assert.That(HudViewModel.Title("iron-ore"), Is.EqualTo("Iron ore"));
            Assert.That(HudViewModel.Title("power_core"), Is.EqualTo("Power core"));
            Assert.That(HudViewModel.Title(""), Is.Empty);
        }

        private static HudNotice Row(HudViewModel vm, string key)
        {
            for (var i = 0; i < vm.Notices.Rows.Count; i++)
                if (vm.Notices.Rows[i].Key == key) return vm.Notices.Rows[i];
            return null;
        }
    }
}
