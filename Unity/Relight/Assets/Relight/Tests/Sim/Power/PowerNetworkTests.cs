using NUnit.Framework;
using Relight.Sim.Tests.Production;

namespace Relight.Sim.Tests.Power
{
    /// <summary>
    /// B-09 (U-D-12). The rules pinned here are the reference's, cited per test: the GP-POWER-FIX link rule
    /// (campaignPower.ts:16-19), the supply/demand accumulation and throttle (:46-47, :57), the burn formula
    /// (flow.ts:1045-1057) and the one deliberate addition — an outage is reported only when supply that existed
    /// is lost, never on the first tick of a world.
    /// </summary>
    public sealed class PowerNetworkTests
    {
        // ---------------------------------------------------------------- the link rule

        /// <summary>
        /// campaignPower.ts:17 <c>within</c>: a reach node's centre must be within its reach of the other node's
        /// FOOTPRINT. A Pole at (10,10) has its centre at (10.5,10.5) and 8 tiles of reach, so a 2×2 Generator
        /// whose left edge is at x = 18 is 7.5 tiles away and joins; at x = 19 it is 8.5 tiles away and does not.
        /// The generator's own reach is 0, so nothing links back the other way.
        /// </summary>
        [TestCase(18, true)]
        [TestCase(19, false)]
        public void AGeneratorAtTheEdgeOfAPolesReachConnectsAndOneTileFurtherDoesNot(int genX, bool connected)
        {
            var ctx = ProductionFixture.Context();
            var st = ProductionFixture.State(ctx);
            var pole = ProductionFixture.Add(ctx, st, "pole", 10, 10);
            var gen = ProductionFixture.Add(ctx, st, "generator", genX, 10);
            gen.Inv.Add(ItemId.Coal, 10);
            var foundry = ProductionFixture.Add(ctx, st, "foundry", 10, 12);
            ProductionFixture.Seal(ctx, st);

            var grid = PowerGrid.Of(ctx, st);
            Assert.That(grid.Of(foundry.Id), Is.Not.Null, "the foundry is 1.5 tiles from the pole either way");
            Assert.That(grid.Of(gen.Id), Is.Not.Null, "a generator always joins the nearest node in reach of it");
            Assert.That(ReferenceEquals(grid.Of(gen.Id), grid.Of(pole.Id)), Is.EqualTo(connected),
                "the generator is on the pole's circuit only when the reach rule links them");
            Assert.That(PowerQueries.Throttle(ctx, st, foundry.Id) > 0, Is.EqualTo(connected),
                "a foundry is supplied only when a fuelled generator shares its circuit");
        }

        /// <summary>campaignPower.ts:45 <c>if(!c)continue</c>: nothing in reach means no circuit at all.</summary>
        [Test]
        public void AMachineWithNoPoleInReachJoinsNoCircuit()
        {
            var ctx = ProductionFixture.Context();
            var st = ProductionFixture.State(ctx);
            ProductionFixture.Grid(ctx, st, 10, 10, 12, 10, 10);
            var far = ProductionFixture.Add(ctx, st, "foundry", 40, 40);
            ProductionFixture.Seal(ctx, st);

            Assert.That(PowerQueries.Connected(ctx, st, far.Id), Is.False);
            Assert.That(PowerQueries.Throttle(ctx, st, far.Id), Is.Zero);
            Assert.That(PowerQueries.Supplied(ctx, st, far.Id), Is.False);
        }

        // ---------------------------------------------------------------- no outage at start

        /// <summary>
        /// U-D-12: the reference build showed "Power outage" on the first tick of a fresh world because the HUD
        /// derived it from a zero throttle. <see cref="PowerState.Supplied"/> starts empty, so a machine must have
        /// HAD supply to lose any: an unfuelled generator beside a consumer raises nothing at all.
        /// </summary>
        [Test]
        public void AnUnfuelledGeneratorAtTheStartRaisesNoOutage()
        {
            var ctx = ProductionFixture.Context();
            var st = ProductionFixture.State(ctx);
            var (_, gen) = ProductionFixture.Grid(ctx, st, 20, 18, 18, 16, 0);
            var foundry = ProductionFixture.Add(ctx, st, "foundry", 21, 17);
            ProductionFixture.Seal(ctx, st);

            ProductionFixture.Run(ctx, st, 20);
            Assert.That(ProductionFixture.Count<PowerOutageEvent>(st), Is.Zero, "nothing was ever supplied");
            Assert.That(ProductionFixture.Count<PowerRestoredEvent>(st), Is.Zero);
            Assert.That(st.Power.Supplied, Is.Empty);
            Assert.That(ProductionQueries.OperatingState(ctx, st, foundry.Id), Is.EqualTo(MachineOperatingState.Unpowered));
            Assert.That(ProductionQueries.OperatingState(ctx, st, gen.Id), Is.EqualTo(MachineOperatingState.OutOfFuel));

            // Fuel it by hand and the restoration is reported exactly once.
            st.Events.Clear();
            gen.Inv.Add(ItemId.Coal, 5);
            ProductionFixture.Seal(ctx, st);      // the coal was handed in, not conjured: re-open over the new world
            ProductionFixture.Run(ctx, st, 5);
            Assert.That(ProductionFixture.Count<PowerRestoredEvent>(st), Is.EqualTo(1));
            Assert.That(ProductionFixture.Count<PowerOutageEvent>(st), Is.Zero);
            Assert.That(st.Power.Supplied, Is.EqualTo(new[] { foundry.Id }));
            Assert.That(ProductionFixture.Off(ctx, st), Is.Empty);
        }

        // ---------------------------------------------------------------- out of fuel

        /// <summary>
        /// flow.ts:1045-1057. Three Foundries draw 240 kW, which one Generator carries in full, so it burns
        /// 240/(4 MJ) = 0.06 coal a second: its single coal lasts 4000/240 = 16.67 s = 334 ticks. The tick the coal
        /// runs out raises <see cref="GeneratorDryEvent"/>; the grid is rebuilt at the top of the NEXT tick, which
        /// is when the consumers lose supply — the reference's ordering, kept deliberately.
        /// </summary>
        [Test]
        public void AGeneratorThatBurnsItsLastCoalGoesOutOfFuelAndTheOutageFollows()
        {
            var ctx = ProductionFixture.Context();
            var st = ProductionFixture.State(ctx);
            var (_, gen) = ProductionFixture.Grid(ctx, st, 20, 18, 18, 14, 1);
            var a = ProductionFixture.Add(ctx, st, "foundry", 14, 17);
            var b = ProductionFixture.Add(ctx, st, "foundry", 18, 21);
            var c = ProductionFixture.Add(ctx, st, "foundry", 24, 17);
            ProductionFixture.Seal(ctx, st);

            var before = PowerQueries.Network(ctx, st);
            Assert.That(before.DemandKw, Is.EqualTo(240).Within(1e-9), "three Foundries at 80 kW, fed or not");
            Assert.That(before.SupplyKw, Is.EqualTo(300).Within(1e-9));
            Assert.That(before.LoadKw, Is.EqualTo(240).Within(1e-9));
            Assert.That(before.FuelSeconds, Is.EqualTo(4000.0 / 240).Within(1e-6));

            ProductionFixture.Run(ctx, st, 330);
            Assert.That(ProductionFixture.Count<GeneratorDryEvent>(st), Is.Zero, "16.67 s is 334 ticks, not 330");
            Assert.That(PowerQueries.Supplied(ctx, st, a.Id), Is.True);

            ProductionFixture.Run(ctx, st, 10);
            Assert.That(ProductionFixture.Count<GeneratorDryEvent>(st), Is.EqualTo(1));
            Assert.That(ProductionFixture.Count<PowerOutageEvent>(st), Is.EqualTo(3), "one per drawing machine");
            Assert.That(st.Stats.CoalBurned, Is.EqualTo(1));
            Assert.That(PowerGrid.FuelUnits(gen), Is.Zero);
            Assert.That(ProductionQueries.OperatingState(ctx, st, gen.Id), Is.EqualTo(MachineOperatingState.OutOfFuel));
            foreach (var m in new[] { a, b, c })
                Assert.That(ProductionQueries.OperatingState(ctx, st, m.Id), Is.EqualTo(MachineOperatingState.Unpowered));
            Assert.That(st.Power.Supplied, Is.Empty);
            Assert.That(ProductionFixture.Off(ctx, st), Is.Empty, "burned coal is a ledger sink");
        }

        // ---------------------------------------------------------------- throttle

        /// <summary>
        /// campaignPower.ts:57 <c>c.throttle = supply &gt; 0 ? min(1, supply/max(1,demand)) : 0</c>. Four Foundries
        /// want 320 kW from one 300 kW Generator, so every machine on the circuit runs at 300/320 = 0.9375 — the
        /// brownout shares the shortfall rather than cutting anyone off (U-D-12's BrownoutRule).
        /// </summary>
        [Test]
        public void ShortSupplyThrottlesEveryMachineOnTheCircuitEqually()
        {
            var ctx = ProductionFixture.Context();
            var st = ProductionFixture.State(ctx);
            ProductionFixture.Grid(ctx, st, 20, 18, 18, 14, 5);
            var a = ProductionFixture.Add(ctx, st, "foundry", 14, 17, recipe: "steel-plates");
            var b = ProductionFixture.Add(ctx, st, "foundry", 18, 21);
            var c = ProductionFixture.Add(ctx, st, "foundry", 24, 17);
            var d = ProductionFixture.Add(ctx, st, "foundry", 22, 21);
            ProductionFixture.Seal(ctx, st);

            var n = PowerQueries.Network(ctx, st);
            Assert.That(n.DemandKw, Is.EqualTo(320).Within(1e-9));
            Assert.That(n.SupplyKw, Is.EqualTo(300).Within(1e-9));
            Assert.That(n.LoadKw, Is.EqualTo(300).Within(1e-9), "load is min(supply, demand)");
            Assert.That(n.Circuits, Is.EqualTo(1));
            Assert.That(n.Generators, Is.EqualTo(1));
            foreach (var m in new[] { a, b, c, d })
                Assert.That(PowerQueries.Throttle(ctx, st, m.Id), Is.EqualTo(0.9375).Within(1e-12));

            // The machine clock runs at the throttle (flow.ts:1099 `dt * machineThrottle`): ten ticks of a
            // 3 s craft advance 10 × 0.05 × 0.9375 seconds, not 10 × 0.05.
            a.Inv.Add(ItemId.IronOre, 10);
            ProductionFixture.Seal(ctx, st);
            ProductionFixture.Run(ctx, st, 10);
            Assert.That(st.Production.Of(a.Id).Timer, Is.EqualTo(10 * ProductionFixture.Dt * 0.9375).Within(1e-9));
            Assert.That(ProductionQueries.OperatingState(ctx, st, a.Id), Is.EqualTo(MachineOperatingState.Throttled));
            Assert.That(ProductionQueries.Status(ctx, st, a.Id).Text, Is.EqualTo("running slowly — not enough power"));
        }

        /// <summary>campaignPower.ts:61 <c>machineThrottle</c>: a machine that draws nothing always runs at 1.</summary>
        [Test]
        public void AChestDrawsNothingAndIsNeverUnpowered()
        {
            var ctx = ProductionFixture.Context();
            var st = ProductionFixture.State(ctx);
            var chest = ProductionFixture.Add(ctx, st, "chest", 40, 40);
            ProductionFixture.Seal(ctx, st);

            Assert.That(PowerQueries.Throttle(ctx, st, chest.Id), Is.EqualTo(1));
            Assert.That(PowerQueries.Supplied(ctx, st, chest.Id), Is.True);
            Assert.That(ProductionQueries.OperatingState(ctx, st, chest.Id), Is.EqualTo(MachineOperatingState.Idle));
        }

        // ---------------------------------------------------------------- what C-04 asks

        /// <summary>
        /// The query C-04's turrets hold fire on: a turret cabled to a pole with no generator behind it is
        /// connected but not supplied. The firing rule itself is C-04's, not tested here.
        /// </summary>
        [Test]
        public void ATurretOnAPoleWithNoGeneratorIsConnectedButNotSupplied()
        {
            var ctx = ProductionFixture.Context();
            var st = ProductionFixture.State(ctx);
            ProductionFixture.Add(ctx, st, "pole", 20, 18);
            var turret = ProductionFixture.Add(ctx, st, "turret", 22, 18);
            ProductionFixture.Seal(ctx, st);

            Assert.That(PowerQueries.Connected(ctx, st, turret.Id), Is.True);
            Assert.That(PowerQueries.Supplied(ctx, st, turret.Id), Is.False);
            Assert.That(PowerQueries.Network(ctx, st).DemandKw, Is.EqualTo(20).Within(1e-9));

            var gen = ProductionFixture.Add(ctx, st, "generator", 18, 18);
            gen.Inv.Add(ItemId.Coal, 1);
            Assert.That(PowerQueries.Supplied(ctx, st, turret.Id), Is.True, "a fuelled generator on the same pole supplies it");
        }

        /// <summary>
        /// campaignPower.ts:57 <c>grid.generation</c>: two generators on one circuit each carry half the load, so
        /// each burns at half the rate. With no turbine or plant in the port, the whole load is theirs.
        /// </summary>
        [Test]
        public void TwoGeneratorsShareTheLoadAndBurnAtHalfTheRate()
        {
            var ctx = ProductionFixture.Context();
            var st = ProductionFixture.State(ctx);
            ProductionFixture.Add(ctx, st, "pole", 20, 18);
            var g1 = ProductionFixture.Add(ctx, st, "generator", 18, 14);
            var g2 = ProductionFixture.Add(ctx, st, "generator", 22, 14);
            g1.Inv.Add(ItemId.Coal, 5);
            g2.Inv.Add(ItemId.Coal, 5);
            ProductionFixture.Add(ctx, st, "foundry", 14, 17);
            ProductionFixture.Add(ctx, st, "foundry", 24, 17);
            ProductionFixture.Seal(ctx, st);

            Assert.That(PowerQueries.GeneratorShareKw(ctx, st, g1.Id), Is.EqualTo(80).Within(1e-9));
            Assert.That(PowerQueries.GeneratorShareKw(ctx, st, g2.Id), Is.EqualTo(80).Within(1e-9));

            ProductionFixture.Run(ctx, st, 100);   // 5 s at 80 kW = 0.1 of a coal each
            Assert.That(st.Power.BurnOf(g1.Id).Timer, Is.EqualTo(0.1).Within(1e-9));
            Assert.That(st.Power.BurnOf(g2.Id).Timer, Is.EqualTo(0.1).Within(1e-9));
            Assert.That(st.Stats.CoalBurned, Is.Zero);
            Assert.That(ProductionFixture.Off(ctx, st), Is.Empty);
        }

        // ---------------------------------------------------------------- GP-W5: wrecks and the circuit

        /// <summary>
        /// GP-W5. The mechanism behind the Breaker's whole reason for existing — "pushes through light, pressure on
        /// production connections". A big pole is a BUILDING, so <see cref="CombatBalance.Integrity"/> gives it hit
        /// points, and flattening the one that carries a circuit out to the far yard cuts the machines behind it
        /// off: they read as no-power, not as still-running, and they come back when the pole is repaired.
        ///
        /// The control half matters as much. A plain pole is walk-through, so it has NO hit points at all and can
        /// never be attacked. That is deliberate: a raid must never be able to stall itself chewing on the cheapest
        /// thing on the map, and a player must never lose a base because one 2-item pole was a chokepoint.
        /// </summary>
        [Test]
        public void AWreckedBigPoleSeversTheCircuitBehindItWhileAPlainPoleCannotBeAttackedAtAll()
        {
            var ctx = ProductionFixture.Context();
            var st = ProductionFixture.State(ctx);
            var gen = ProductionFixture.Add(ctx, st, "generator", 10, 10);
            gen.Inv.Add(ItemId.Coal, 50);
            ProductionFixture.Add(ctx, st, "bigpole", 13, 10);          // the yard's own pole
            var relay = ProductionFixture.Add(ctx, st, "bigpole", 21, 10);   // the span out to the far yard
            var near = ProductionFixture.Add(ctx, st, "foundry", 13, 13);
            var far = ProductionFixture.Add(ctx, st, "foundry", 30, 10);
            var plain = ProductionFixture.Add(ctx, st, "pole", 40, 30);
            ProductionFixture.Seal(ctx, st);

            Assert.That(PowerQueries.Supplied(ctx, st, near.Id), Is.True);
            Assert.That(PowerQueries.Supplied(ctx, st, far.Id), Is.True, "the relay carries the circuit out to it");

            Assert.That(TurretRules.MaxHp(ctx.Data, plain), Is.Zero,
                "a walk-through pole has no hit points: a raid can never stall itself on one");
            var max = TurretRules.MaxHp(ctx.Data, relay);
            Assert.That(max, Is.GreaterThan(0), "a big pole is a building and can be broken");
            Assert.That(TurretRules.Damage(ctx, st, relay, max), Is.True);

            Assert.That(PowerQueries.Connected(ctx, st, far.Id), Is.False, "the span is gone");
            Assert.That(PowerQueries.Throttle(ctx, st, far.Id), Is.Zero);
            Assert.That(ProductionQueries.OperatingState(ctx, st, far.Id),
                Is.EqualTo(MachineOperatingState.Unpowered));
            Assert.That(PowerQueries.Supplied(ctx, st, near.Id), Is.True,
                "but only what was BEHIND the pole went dark");

            // Repairing it puts the far yard back on the circuit, which is what makes the wreck worth repairing.
            TurretRules.TurretRepairHook(ctx, st, relay.Id, max);
            Assert.That(TurretRules.Wrecked(ctx.Data, st, relay), Is.False);
            Assert.That(PowerQueries.Supplied(ctx, st, far.Id), Is.True);
        }
    }
}
