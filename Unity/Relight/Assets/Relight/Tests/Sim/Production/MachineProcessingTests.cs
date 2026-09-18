using NUnit.Framework;

namespace Relight.Sim.Tests.Production
{
    /// <summary>
    /// B-07. Every number here is the reference's: the assembler loop (flow.ts:961-978), its start conditions
    /// (:951-960), the output cap (:953 / ASM_OUTPUT_CAP), the excavator loop (:936-949) and <c>outputTile</c>
    /// (:601). Conservation is asserted in every test that moves an item, because the whole point of the side-table
    /// port is that nothing goes missing on the way.
    /// </summary>
    public sealed class MachineProcessingTests
    {
        /// <summary>A foundry on a fuelled circuit at full throttle, with ore in it.</summary>
        private static (SimContext Ctx, SimState St, Machine Foundry) Smelter(int ore)
        {
            var ctx = ProductionFixture.Context();
            var st = ProductionFixture.State(ctx);
            ProductionFixture.Grid(ctx, st, 20, 18, 18, 14, 50);
            var foundry = ProductionFixture.Add(ctx, st, "foundry", 14, 17, recipe: "steel-plates");
            if (ore > 0) foundry.Inv.Add(ItemId.IronOre, ore);
            ProductionFixture.Seal(ctx, st);
            return (ctx, st, foundry);
        }

        // ---------------------------------------------------------------- processors

        /// <summary>
        /// flow.ts:961-978. Steel plates take 3 s (60 ticks) and turn 2 Iron ore into 1 Steel; the craft that
        /// finishes restarts in the same tick with the remainder of the timer, so the second plate lands 60 ticks
        /// later and not a tick late.
        /// </summary>
        [Test]
        public void AFoundrySmeltsOnePlatePerRecipeTime()
        {
            var (ctx, st, foundry) = Smelter(20);
            Assert.That(ProductionQueries.Recipe(ctx, st, foundry.Id).Key, Is.EqualTo("steel-plates"), "the recipe the fixture chose");

            ProductionFixture.Run(ctx, st, 39);
            Assert.That(foundry.Inv[ItemId.Steel], Is.Zero, "2 s has not passed at 39 ticks");
            Assert.That(ProductionQueries.OperatingState(ctx, st, foundry.Id), Is.EqualTo(MachineOperatingState.Running));
            Assert.That(ProductionQueries.Status(ctx, st, foundry.Id).Progress, Is.EqualTo(39 / 40.0).Within(1e-9));

            ProductionFixture.Run(ctx, st, 1);
            Assert.That(foundry.Inv[ItemId.Steel], Is.EqualTo(1));
            Assert.That(foundry.Inv[ItemId.IronOre], Is.EqualTo(18), "two crafts have been paid for: the finished one and the next");
            Assert.That(st.Stats.Made[ItemId.Steel], Is.EqualTo(1));
            Assert.That(st.Stats.Consumed[ItemId.IronOre], Is.EqualTo(2));
            Assert.That(ProductionFixture.Count<MachineProducedEvent>(st), Is.EqualTo(1));

            ProductionFixture.Run(ctx, st, 40);
            Assert.That(foundry.Inv[ItemId.Steel], Is.EqualTo(2), "the restart kept the remainder, so the cadence holds");
            Assert.That(ProductionFixture.Off(ctx, st), Is.Empty);
        }

        /// <summary>
        /// flow.ts:953: a processor stops when its output buffer is full (ASM_OUTPUT_CAP = 5 batches, carried in
        /// the port as <c>MachineSpec.OutputBufferCap</c>). It has ore left over and simply waits.
        /// </summary>
        [Test]
        public void AFullOutputBufferStopsTheFoundryWithOreStillInIt()
        {
            var (ctx, st, foundry) = Smelter(20);

            ProductionFixture.Run(ctx, st, 400);
            Assert.That(foundry.Inv[ItemId.Steel], Is.EqualTo(5), "five batches is the cap");
            Assert.That(foundry.Inv[ItemId.IronOre], Is.EqualTo(15), "five crafts were paid for, no sixth was started");
            Assert.That(ProductionQueries.OperatingState(ctx, st, foundry.Id), Is.EqualTo(MachineOperatingState.OutputFull));
            Assert.That(ProductionQueries.Status(ctx, st, foundry.Id).Text, Is.EqualTo("output full"));
            Assert.That(ProductionFixture.Off(ctx, st), Is.Empty);

            // Take the plates away and it starts again on the next tick.
            foundry.Inv.Add(ItemId.Steel, -5);
            st.Stats.Consumed.Add(ItemId.Steel, 5);      // the test took them out of the world; the ledger must know
            ProductionFixture.Run(ctx, st, 1);
            Assert.That(ProductionQueries.OperatingState(ctx, st, foundry.Id), Is.EqualTo(MachineOperatingState.Running));
            Assert.That(ProductionFixture.Off(ctx, st), Is.Empty);
        }

        /// <summary>flow.ts:951 <c>asmCanStart</c>: no inputs, no craft — and the UI says why.</summary>
        [Test]
        public void AFoundryWithNoOreWaitsForMaterials()
        {
            var (ctx, st, foundry) = Smelter(0);

            ProductionFixture.Run(ctx, st, 100);
            Assert.That(foundry.Inv[ItemId.Steel], Is.Zero);
            Assert.That(ProductionQueries.OperatingState(ctx, st, foundry.Id), Is.EqualTo(MachineOperatingState.NoInput));
            Assert.That(ProductionQueries.Status(ctx, st, foundry.Id).Text, Is.EqualTo("waiting for materials"));
            Assert.That(ProductionFixture.Off(ctx, st), Is.Empty);
        }

        /// <summary>flow.ts:630 <c>powered</c>: a processor with no circuit does not advance at all.</summary>
        [Test]
        public void AFoundryWithNoPowerDoesNotSmelt()
        {
            var ctx = ProductionFixture.Context();
            var st = ProductionFixture.State(ctx);
            var foundry = ProductionFixture.Add(ctx, st, "foundry", 40, 40);
            foundry.Inv.Add(ItemId.IronOre, 20);
            ProductionFixture.Seal(ctx, st);

            ProductionFixture.Run(ctx, st, 200);
            Assert.That(foundry.Inv[ItemId.IronOre], Is.EqualTo(20), "nothing was even started");
            Assert.That(foundry.Inv[ItemId.Steel], Is.Zero);
            Assert.That(st.Production.Of(foundry.Id).Timer, Is.Zero);
            Assert.That(ProductionQueries.OperatingState(ctx, st, foundry.Id), Is.EqualTo(MachineOperatingState.Unpowered));
            Assert.That(ProductionQueries.Status(ctx, st, foundry.Id).Text, Is.EqualTo("no power"));
            Assert.That(ProductionFixture.Off(ctx, st), Is.Empty);
        }

        /// <summary>
        /// U-D-08 / flow.ts:972: one round is one bullet, so an Assembler's Bullet batch yields the recipe's full
        /// count of ten Magazines, and the ammunition output buffer (50 rounds) replaces ASM_OUTPUT_CAP for it.
        /// </summary>
        [Test]
        public void AnAssemblerYieldsTheWholeBatchOfRounds()
        {
            var ctx = ProductionFixture.Context();
            var st = ProductionFixture.State(ctx);
            ProductionFixture.Grid(ctx, st, 20, 18, 18, 14, 50);
            var asm = ProductionFixture.Add(ctx, st, "assembler", 14, 17, recipe: "bullet-batch");
            asm.Inv.Add(ItemId.Steel, 4);
            asm.Inv.Add(ItemId.Copper, 2);
            ProductionFixture.Seal(ctx, st);

            ProductionFixture.Run(ctx, st, 121);       // the Bullet batch takes 6 s = 120 ticks
            Assert.That(asm.Inv[ItemId.Magazine], Is.EqualTo(10));
            Assert.That(st.Stats.MagsMade, Is.EqualTo(10));
            Assert.That(ProductionFixture.Off(ctx, st), Is.Empty);
        }

        // ---------------------------------------------------------------- excavator

        /// <summary>The excavator at (10,16) facing south, the chest on the tile it outputs to, a fuelled pole beside it.</summary>
        private static (SimContext Ctx, SimState St, Machine Dig, Machine Chest) Pit(bool withChest)
        {
            var ctx = ProductionFixture.Context();
            var st = ProductionFixture.State(ctx);
            var dig = ProductionFixture.Add(ctx, st, "excavator", 10, 16, Dir.S);
            Machine chest = null;
            if (withChest) chest = ProductionFixture.Add(ctx, st, "chest", 11, 19);
            ProductionFixture.Grid(ctx, st, 14, 16, 16, 16, 50);
            ProductionFixture.Seal(ctx, st);
            Assert.That(ProductionRules.OutputTile(dig), Is.EqualTo((11, 19)), "flow.ts:601 outputTile, facing south");
            return (ctx, st, dig, chest);
        }

        /// <summary>
        /// flow.ts:936-949. The Excavator's 0.5/s rate is a 2 s cycle (40 ticks); the first diggable tile in the
        /// ring around its footprint is the rubble at (10,15), which the synthetic map holds 300 units of.
        /// </summary>
        [Test]
        public void AnExcavatorDigsTheRubbleBesideItIntoTheChestItFaces()
        {
            var (ctx, st, dig, chest) = Pit(true);

            ProductionFixture.Run(ctx, st, 39);
            Assert.That(st.Stats.Mined, Is.Zero, "the first cycle is 2 s");

            ProductionFixture.Run(ctx, st, 6);
            Assert.That(chest.Inv[ItemId.Steel], Is.EqualTo(1), "scrap steel, the fallback item for rubble");
            Assert.That(st.Stats.Mined, Is.EqualTo(1));
            Assert.That(st.Stats.MinedOf[ItemId.Steel], Is.EqualTo(1));
            Assert.That(Ground.UnitsAt(ctx, st, 10, 15), Is.EqualTo(ctx.Data.World.RubbleUnitsPerTile - 1).Within(1e-9));
            Assert.That(ProductionQueries.OperatingState(ctx, st, dig.Id), Is.EqualTo(MachineOperatingState.Running));

            ProductionFixture.Run(ctx, st, 160);
            Assert.That(chest.Inv[ItemId.Steel], Is.EqualTo(5), "one unit every 40 ticks");
            Assert.That(ProductionFixture.Off(ctx, st), Is.Empty);
        }

        /// <summary>
        /// Blocked extraction retains exactly one visible output item in the ordinary inventory.
        /// The same inventory participates in saving, collection and conservation.
        /// </summary>
        [Test]
        public void AnExcavatorWithBlockedOutputBuffersOneVisibleConservedItem()
        {
            var (ctx, st, dig, _) = Pit(false);

            ProductionFixture.Run(ctx, st, 200);
            Assert.That(st.Stats.Mined, Is.EqualTo(1));
            Assert.That(dig.Inv[ItemId.Steel], Is.EqualTo(1));
            var contents = new ItemCounts(); MachineInventory.Contents(ctx.Data, dig, contents);
            Assert.That(contents[ItemId.Steel], Is.EqualTo(1), "buffer must be visible to hand transfer");
            Assert.That(Ground.UnitsAt(ctx, st, 10, 15), Is.EqualTo(ctx.Data.World.RubbleUnitsPerTile - 1).Within(1e-9));
            Assert.That(ProductionQueries.OperatingState(ctx, st, dig.Id), Is.EqualTo(MachineOperatingState.OutputFull));
            Assert.That(ProductionFixture.Off(ctx, st), Is.Empty);
        }

        /// <summary>flow.ts:943 <c>if(!r){m.timer=cycle;return;}</c>: nothing diggable in reach is a stall, not a crash.</summary>
        [Test]
        public void AnExcavatorOnBareGroundFindsNothingToDig()
        {
            var ctx = ProductionFixture.Context();
            var st = ProductionFixture.State(ctx);
            var dig = ProductionFixture.Add(ctx, st, "excavator", 34, 42, Dir.S);
            ProductionFixture.Add(ctx, st, "chest", 35, 45);
            ProductionFixture.Grid(ctx, st, 38, 42, 40, 42, 50);
            ProductionFixture.Seal(ctx, st);

            ProductionFixture.Run(ctx, st, 200);
            Assert.That(st.Stats.Mined, Is.Zero);
            Assert.That(ProductionQueries.OperatingState(ctx, st, dig.Id), Is.EqualTo(MachineOperatingState.NoInput));
            Assert.That(ProductionFixture.Off(ctx, st), Is.Empty);
        }

        // ---------------------------------------------------------------- SetRecipeCommand

        [Test]
        public void ARecipeCannotBeChangedFromOutsideInteractionRange()
        {
            var (ctx, st, foundry) = Smelter(20);
            st.Engineer.Pos = new Vec2(0, 0);
            var before = CanonicalJsonWriter.Write(st);
            var result = Send(ctx, st, new SetRecipeCommand(foundry.Id, "refined-copper"));
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Problem, Does.Contain("closer"));
            Assert.That(CanonicalJsonWriter.Write(st), Is.EqualTo(before));
        }

        private static CommandResult Send(SimContext ctx, SimState st, Command c)
        {
            var ok = new SetRecipeHandler().TryApply(ctx, st, c, out var r);
            Assert.That(ok, Is.True, "the handler must claim its own command");
            return r;
        }

        /// <summary>flow.ts:987-990: the refusals, in the reference's order.</summary>
        [Test]
        public void SettingARecipeRefusesAMachineThatRunsNoneAndARecipeItCannotRun()
        {
            var (ctx, st, foundry) = Smelter(0);
            st.Engineer.Pos = new Vec2(foundry.X - 1, foundry.Y);
            var chest = ProductionFixture.Add(ctx, st, "chest", 22, 21);

            var onChest = Send(ctx, st, new SetRecipeCommand(chest.Id, "steel-plates"));
            Assert.That(onChest.Accepted, Is.False);
            Assert.That(onChest.Problem, Is.EqualTo(SetRecipeHandler.NoMachineText));

            var wrongStation = Send(ctx, st, new SetRecipeCommand(foundry.Id, "concrete"));
            Assert.That(wrongStation.Accepted, Is.False);
            Assert.That(wrongStation.Problem, Is.EqualTo(SetRecipeHandler.UnsupportedText));

            var missing = Send(ctx, st, new SetRecipeCommand(9999, "steel-plates"));
            Assert.That(missing.Accepted, Is.False);
            Assert.That(missing.Problem, Is.EqualTo(SetRecipeHandler.NoMachineText));

            var same = Send(ctx, st, new SetRecipeCommand(foundry.Id, "steel-plates"));
            Assert.That(same.Accepted, Is.True, "the reference treats the current recipe as a no-op, not a refusal");
            Assert.That(same.Problem, Is.Empty);
        }

        /// <summary>
        /// flow.ts:995-998: switching a busy machine hands the ingredients of the abandoned craft back and
        /// un-counts them, so no ore is destroyed by changing your mind.
        /// </summary>
        [Test]
        public void SwitchingRecipesGivesTheAbandonedCraftsIngredientsBack()
        {
            var (ctx, st, foundry) = Smelter(20);
            st.Engineer.Pos = new Vec2(foundry.X - 1, foundry.Y);
            ProductionFixture.Run(ctx, st, 10);
            Assert.That(st.Production.Of(foundry.Id).Busy, Is.True);
            Assert.That(foundry.Inv[ItemId.IronOre], Is.EqualTo(19));

            var r = Send(ctx, st, new SetRecipeCommand(foundry.Id, "refined-copper"));
            Assert.That(r.Accepted, Is.True);
            Assert.That(foundry.Inv[ItemId.IronOre], Is.EqualTo(20), "the reserved ore came back");
            Assert.That(st.Stats.Consumed[ItemId.IronOre], Is.Zero);
            var work = st.Production.Of(foundry.Id);
            Assert.That(work.Busy, Is.False);
            Assert.That(work.Timer, Is.Zero);
            Assert.That(work.Recipe, Is.EqualTo("refined-copper"));
            Assert.That(ProductionFixture.Off(ctx, st), Is.Empty);

            // It now smelts copper and leaves the iron alone.
            foundry.Inv.Add(ItemId.CopperOre, 4);
            ProductionFixture.Seal(ctx, st);
            ProductionFixture.Run(ctx, st, 61);
            Assert.That(foundry.Inv[ItemId.Copper], Is.EqualTo(1));
            Assert.That(foundry.Inv[ItemId.IronOre], Is.EqualTo(20));
            Assert.That(ProductionFixture.Off(ctx, st), Is.Empty);
        }

        /// <summary>
        /// flow.ts:151 <c>recipesFor</c>: the choices a machine offers are its station's recipes. The input-free
        /// <c>alien-decode</c> is a campaign action rather than a settable recipe and is deliberately excluded.
        /// </summary>
        [Test]
        public void TheChoicesAMachineOffersAreItsStationsRecipes()
        {
            var ctx = ProductionFixture.Context();
            var st = ProductionFixture.State(ctx);
            var foundry = ProductionFixture.Add(ctx, st, "foundry", 14, 17);
            var bench = ProductionFixture.Add(ctx, st, "alienworkbench", 20, 17);
            var chest = ProductionFixture.Add(ctx, st, "chest", 26, 17);

            var foundryKeys = new System.Collections.Generic.List<string>();
            foreach (var r in ProductionQueries.Choices(ctx, st, foundry.Id)) foundryKeys.Add(r.Key);
            Assert.That(foundryKeys, Does.Contain("steel-plates"));
            Assert.That(foundryKeys, Does.Contain("refined-copper"));
            Assert.That(foundryKeys, Does.Not.Contain("concrete"));

            foreach (var r in ProductionQueries.Choices(ctx, st, bench.Id))
                Assert.That(r.Key, Is.Not.EqualTo("alien-decode"), "an input-free campaign action is not a recipe choice");

            Assert.That(ProductionQueries.Choices(ctx, st, chest.Id), Is.Empty);
            Assert.That(ProductionQueries.Recipe(ctx, st, chest.Id), Is.Null);
        }

        /// <summary>
        /// GP-W5. Every buildable machine now carries an integrity row (<see cref="CombatBalance.Integrity"/>), so
        /// "the raid got into the smelting yard" has to mean something on the production side as well as the combat
        /// side. A wrecked foundry draws no power, runs no craft and consumes no ore, and it says so in the one word
        /// the machine panel reads — it does not quietly keep smelting behind a red icon.
        ///
        /// The ore and the plates it had made are still in it. A wreck is a stopped machine, never a hole items
        /// fall through: <see cref="ProductionFixture.Off"/> is asserted after the damage exactly as it is before.
        /// </summary>
        [Test]
        public void AWreckedFoundryStopsSmeltingAndReadsAsDisabledWithoutLosingWhatWasInIt()
        {
            var (ctx, st, foundry) = Smelter(20);
            ProductionFixture.Run(ctx, st, 40);
            var steel = foundry.Inv[ItemId.Steel];
            var ore = foundry.Inv[ItemId.IronOre];
            Assert.That(steel, Is.EqualTo(1), "it was working before the raid arrived");

            var max = TurretRules.MaxHp(ctx.Data, foundry);
            Assert.That(max, Is.GreaterThan(0), "GP-W5 gives a built machine hit points, so a raid can flatten it");
            Assert.That(TurretRules.Damage(ctx, st, foundry, max), Is.True, "and that is the call that disables it");

            Assert.That(TurretRules.Wrecked(ctx.Data, st, foundry), Is.True);
            Assert.That(PowerQueries.Throttle(ctx, st, foundry.Id), Is.Zero, "a wreck neither supplies nor draws");
            Assert.That(ProductionQueries.OperatingState(ctx, st, foundry.Id),
                Is.EqualTo(MachineOperatingState.Disabled));

            ProductionFixture.Run(ctx, st, 400);
            Assert.That(foundry.Inv[ItemId.Steel], Is.EqualTo(steel), "ten recipe times later, not one more plate");
            Assert.That(foundry.Inv[ItemId.IronOre], Is.EqualTo(ore), "and not one more ore eaten");
            Assert.That(ProductionFixture.Off(ctx, st), Is.Empty, "a wreck is a stopped machine, not a leak");
        }
    }
}
