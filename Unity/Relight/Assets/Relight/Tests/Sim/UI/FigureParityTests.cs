using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;
using Relight.Sim.Tests.Campaign;
using Relight.Sim.Tests.Combat;
using Relight.Sim.Tests.Production;
using Relight.Sim.UI;

namespace Relight.Sim.Tests.UI
{
    /// <summary>
    /// REL-10 (INT-06), "One source per figure is not true yet". Its outcome: "each of fuel time, slot size, turret
    /// rounds and core HP has one producer and a parity test". The producers:
    /// <list type="bullet">
    /// <item>fuel time: <see cref="PowerQueries.FuelTimeText"/> over <see cref="PowerQueries.FullSlotSeconds"/> or a
    /// circuit's own <see cref="PowerQueries.FuelSecondsAt"/>;</item>
    /// <item>slot size: <see cref="MachineInventory.GeneratorFuelCap"/>;</item>
    /// <item>turret rounds: <see cref="TurretHopper.RoundsText"/>;</item>
    /// <item>core HP: <see cref="HomeQueries.CoreHpText"/>.</item>
    /// </list>
    /// Each test puts one figure on every sim-side surface that shows it and asserts they all carry the producer's
    /// exact string. The UI-side readers (the world card, the Backpack panel's store, the power tooltip) pass these
    /// strings through unchanged.
    /// </summary>
    public sealed class FigureParityTests
    {
        private static string Num(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);

        // ---- fuel time and slot size ------------------------------------------------------------------------

        /// <summary>The opening's chain up to its "Fuel your Generator" row, as the objective sweep builds it.</summary>
        private static ObjectiveView FuelObjective(SimContext ctx)
        {
            var st = OpeningFixture.State(ctx);
            RaidFixture.Add(ctx, st, "generator", RaidFixture.CoreX + 10, RaidFixture.CoreY);
            RaidFixture.Add(ctx, st, "excavator", RaidFixture.CoreX + 14, RaidFixture.CoreY);
            RaidFixture.Add(ctx, st, "chest", RaidFixture.CoreX + 18, RaidFixture.CoreY);
            for (var x = RaidFixture.CoreX + 16; x < RaidFixture.CoreX + 18; x++)
                RaidFixture.Add(ctx, st, "belt", x, RaidFixture.CoreY, Dir.E);
            var v = OpeningQueries.Objective(ctx, st);
            Assert.That(v.Title, Is.EqualTo("Fuel your Generator"), "the fixture must reach the fuel row");
            return v;
        }

        /// <summary>
        /// The card's first finding: the objective said one Coal lasts "about 13 s" from its own arithmetic while
        /// every other surface worded times through <see cref="PowerQueries.FuelTimeText"/>. It now states the slot
        /// and a full slot's time, both from the producers the Generator's panel reads.
        /// </summary>
        [Test]
        public void TheFuelObjectiveStatesTheProducersSlotAndTime()
        {
            var ctx = OpeningFixture.Context();
            var d = ctx.Data;
            var v = FuelObjective(ctx);

            Assert.That(v.Detail, Does.Contain("The slot holds " + Num(MachineInventory.GeneratorFuelCap(d)) + ","));
            Assert.That(v.Detail, Does.Contain("a full slot runs it "
                + PowerQueries.FuelTimeText(PowerQueries.FullSlotSeconds(d)) + " at its full "
                + Num(PowerQueries.GeneratorKw(d)) + " kW (estimate)"));
            Assert.That(v.Detail, Does.Not.Contain("One Coal"), "no second, per-Coal figure beside the full slot's");
        }

        /// <summary>
        /// The Generator's tuning row carries its own copy of the fuel cap and the kW. The objective used to read
        /// those; with them changed and the machine spec left alone, it must still say what the Generator does.
        /// </summary>
        [Test]
        public void TheFuelObjectiveReadsTheMachineNotASecondTuningCopy()
        {
            var b = ReferenceData.Create();
            var data = new GameData(b.Items, b.Machines, b.Recipes, b.Engineer, b.World, b.Weapons, b.Enemies,
                b.Ammunition, b.Turrets, b.Power with { GeneratorFuelCap = 7, GeneratorKw = 999 }, b.Time, b.Raids,
                b.Opening, b.Stake, b.Defence, b.Siege);
            var ctx = new SimContext(data, RaidFixture.Map(), null, null, new WorldSites(new List<SiteRecord>
            {
                new SiteRecord("home", "Home Court", SiteKind.Core, RaidFixture.CoreX, RaidFixture.CoreY,
                    RaidFixture.CoreSize, RaidFixture.CoreSize, "", 0),
            }));
            Assert.That(MachineInventory.GeneratorFuelCap(data), Is.Not.EqualTo(7), "the spec must differ from the copy");
            Assert.That(PowerQueries.GeneratorKw(data), Is.Not.EqualTo(999));

            var v = FuelObjective(ctx);
            Assert.That(v.Detail, Does.Contain("The slot holds " + Num(MachineInventory.GeneratorFuelCap(data)) + ","));
            Assert.That(v.Detail, Does.Contain(Num(PowerQueries.GeneratorKw(data)) + " kW"));
            Assert.That(v.Detail, Does.Not.Contain("holds 7"));
            Assert.That(v.Detail, Does.Not.Contain("999"));
        }

        /// <summary>
        /// The objective's promise, checked against the game: a Generator with a full slot drawing its full output
        /// reports the same time on its own line and in the HUD's power tooltip row.
        /// </summary>
        [Test]
        public void AFullSlotAtFullLoadReadsTheSameTimeEverywhere()
        {
            var ctx = ProductionFixture.Context();
            var st = ProductionFixture.State(ctx);
            var d = ctx.Data;
            var cap = (int)MachineInventory.GeneratorFuelCap(d);
            var (_, gen) = ProductionFixture.Grid(ctx, st, 20, 18, 18, 14, cap);
            for (var i = 0; i < 3; i++) ProductionFixture.Add(ctx, st, "assembler", 12 + i * 3, 20);
            ProductionFixture.Run(ctx, st, 1);
            Assert.That(PowerQueries.Network(ctx, st).LoadKw, Is.EqualTo(PowerQueries.GeneratorKw(d)).Within(1e-6),
                "the Generator must be drawing its full output, or the objective's figure does not apply");

            var time = PowerQueries.FuelTimeText(PowerQueries.FullSlotSeconds(d));
            Assert.That(PowerQueries.FuelTimeText(PowerQueries.GeneratorFuelSeconds(ctx, st, gen.Id)), Is.EqualTo(time),
                "one tick of burn does not move a coarse estimate");
            Assert.That(PowerQueries.Description(ctx, st, gen.Id), Does.Contain("fuel for " + time + " at this load (estimate)"));

            var vm = new HudViewModel();
            vm.Refresh(ctx, st, 0, paused: true, menuOpen: false);
            Assert.That(vm.FuelText, Is.EqualTo(time + " at this load (estimate)"), "the power tooltip's Fuel left row");
        }

        // ---- turret rounds ---------------------------------------------------------------------------------

        [Test]
        public void ATurretsLoadIsOneString()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            var t = RaidFixture.Turret(ctx, st, 40, 40, 50);
            var d = ctx.Data;
            Assert.That(TurretHopper.RoundsText(d, t), Is.EqualTo(t.Rounds + " / " + TurretHopper.Capacity(d, t) + " rounds"));
            Assert.That(TurretHopper.Capacity(d, t), Is.EqualTo(TurretHopper.Capacity(d, "turret")),
                "the capacity of a built turret and of the kind before one is built");

            // A full turret's hover used to say nothing, and the world card added " · 50 rounds" of its own.
            Assert.That(ProductionQueries.Description(ctx, st, t.Id), Does.Contain("Loaded " + TurretHopper.RoundsText(d, t)));
            t.Rounds = 9;
            Assert.That(ProductionQueries.Description(ctx, st, t.Id),
                Does.Contain("Low on ammunition · " + TurretHopper.RoundsText(d, t)));
            Assert.That(ProductionQueries.Description(ctx, st, t.Id), Does.Not.Contain("Loaded "),
                "one load line, not two");
            t.Rounds = 0;
            Assert.That(ProductionQueries.Description(ctx, st, t.Id), Does.Contain("Out of ammunition"));
        }

        /// <summary>The opening's "Load your new turret" row prints the hopper through the same producer.</summary>
        [Test]
        public void TheOpeningQuotesTheTurretsLoadFromTheSameProducer()
        {
            var ctx = OpeningFixture.Context();
            var st = OpeningFixture.State(ctx);
            OpeningFixture.ToRifle(ctx, st);
            st.Weapons.Slot0 = st.Weapons.Owned[0].Id;
            OpeningFixture.ReadyTurret(ctx, st);
            RaidFixture.Turret(ctx, st, RaidFixture.CoreX - 6, RaidFixture.CoreY);
            var empty = RaidFixture.Turret(ctx, st, RaidFixture.CoreX, RaidFixture.CoreY + 6, 0);
            st.Opening.Status = OpeningStatus.Repelled;
            st.Opening.EndedAt = -1;
            st.Opening.SuppliedAt = st.T;
            st.T += 1000;
            OpeningFixture.Run(ctx, st, 1);

            var v = OpeningQueries.Objective(ctx, st);
            Assert.That(v.Title, Is.EqualTo("Load your new turret"));
            Assert.That(v.Detail, Does.StartWith("Loaded " + TurretHopper.RoundsText(ctx.Data, empty) + "."));
        }

        // ---- core HP ---------------------------------------------------------------------------------------

        /// <summary>
        /// A core with a fraction of a point left: every surface rounds it up the same way. The Admin readout used to
        /// round to nearest and print "hp", and the workshop card spaced its slash.
        /// </summary>
        [Test]
        public void TheCoresHealthIsOneString()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            HomeCore.Ensure(ctx, st);
            var max = HomeQueries.CoreMaxHp(ctx.Data);
            Assert.That(max, Is.EqualTo((double)ctx.Data.Defence.CoreHp));
            st.Home.Hp = max - 87.6;

            var text = HomeQueries.CoreHpText(st.Home.Hp, max);
            Assert.That(text, Is.EqualTo(Num(System.Math.Ceiling(max - 87.6)) + "/" + Num(max) + " HP"), "rounded up");

            var vm = new HudViewModel();
            vm.Refresh(ctx, st, 0, paused: true, menuOpen: false);
            Assert.That(vm.Core, Is.EqualTo("Home core · " + text), "HUD");
            Assert.That(HomeQueries.Description(st, ctx.Data), Does.StartWith("Base core · " + text), "hover");
            var card = HomeQueries.RepairCard(st, ctx.Data);
            Assert.That(WorkshopText.CoreHp(card.Hp, card.Max), Is.EqualTo(text), "workshop card");
            var readout = DirectorQueries.Readout(ctx, st);
            Assert.That(readout.HasTarget, Is.True, "the Admin readout names the core only while it is the target");
            Assert.That(DirectorQueries.Describe(readout), Does.Contain(" · " + text), "Admin readout");
        }
    }
}
