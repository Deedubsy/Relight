using System.Collections.Generic;
using NUnit.Framework;
using Relight.Sim.Tests.Combat;
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
        /// The control half matters as much. A plain pole is walk-through, so it is never in a raid's way and is
        /// never attacked. That is deliberate: a raid must never be able to stall itself chewing on the cheapest
        /// thing on the map, and a player must never lose a base because one 2-item pole was a chokepoint.
        /// GP-W5 met that by giving it no hit points; REL-115 (U-D-68 (d)) gives every building hit points, and
        /// U-D-69 (h) now meets it instead: a raider breaks a machine only when it blocks the path.
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

            Assert.That(TurretRules.MaxHp(ctx.Data, plain), Is.GreaterThan(0), "every building has hit points (REL-115)");
            Assert.That(TurretRules.BlocksRaiders(ctx.Data, plain), Is.False,
                "a walk-through pole is never in the way: a raid can never stall itself on one");
            Assert.That(TurretRules.BlocksRaiders(ctx.Data, relay), Is.True, "a big pole is solid, so it can be");
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
            // ---------------------------------------------------------------- authored substations (court D55)

        private const int SubX = 40, SubY = 40;
        private const string SubId = "substation:0";

        /// <summary>The 160×160 raid map with the Home core and one authored 3×3 substation lot at (40,40).</summary>
        private static SimContext SubstationContext() =>
            new SimContext(ReferenceData.Create(), RaidFixture.Map(), null, null, new WorldSites(new List<SiteRecord>
            {
                new SiteRecord("home", "Home Court", SiteKind.Core, RaidFixture.CoreX, RaidFixture.CoreY,
                    RaidFixture.CoreSize, RaidFixture.CoreSize, "", 0),
                new SiteRecord(SubId, "Substation 0", SiteKind.Substation, SubX, SubY, 3, 3),
            }));

        /// <summary>
        /// Court D55: the site is a reach-8 node. A Pole at (26,41) has its centre 13.5 tiles from the lot's west edge
        /// and its generator at (28,41) is 11.5 tiles from the lot's centre, so neither links; one more Pole at
        /// (33,43) is 6.5 tiles from the lot and 6.7 from the first Pole, which puts the site on the generator's
        /// circuit.
        /// </summary>
        [Test]
        public void APoleWithinReachOfASubstationSiteLinksItToTheCircuit()
        {
            var ctx = SubstationContext();
            var st = RaidFixture.State(ctx);
            RaidFixture.Power(ctx, st, 26, 41);        // pole (26,41), fuelled generator (28,41)

            Assert.That(PowerGrid.Of(ctx, st).OfSite(SubId), Is.Null, "out of reach: the site is on no circuit");

            var pole = RaidFixture.Add(ctx, st, "pole", 33, 43);
            var grid = PowerGrid.Of(ctx, st);
            var c = grid.OfSite(SubId);
            Assert.That(c, Is.Not.Null, "a pole within 8 tiles of the footprint links to the site");
            Assert.That(c, Is.SameAs(grid.Of(pole.Id)));
            Assert.That(c.Supply, Is.GreaterThan(0));
            Assert.That(c.Demand, Is.Zero, "the site itself neither supplies nor draws");
        }

        /// <summary>A lot no placed node reaches is on no circuit and adds no circuit to the HUD count.</summary>
        [Test]
        public void AnUnreachedSubstationSiteHasNoCircuit()
        {
            var ctx = SubstationContext();
            var st = RaidFixture.State(ctx);
            RaidFixture.Power(ctx, st, 100, 100);

            Assert.That(PowerGrid.Of(ctx, st).OfSite(SubId), Is.Null);
            Assert.That(PowerQueries.Network(ctx, st).Circuits, Is.EqualTo(1), "only the placed pole's circuit");
        }

        /// <summary>
        /// The one deliberate difference from the reference's substation: a site cables, but never owns a consuming
        /// machine. A Foundry beside an unconnected lot stays disconnected, and one with a Pole in reach is powered
        /// by that Pole even though the lot is nearer.
        /// </summary>
        [Test]
        public void ASubstationSiteNeverOwnsAConsumingMachine()
        {
            var ctx = SubstationContext();
            var st = RaidFixture.State(ctx);
            var beside = RaidFixture.Add(ctx, st, "foundry", SubX + 3, SubY);
            Assert.That(PowerQueries.Connected(ctx, st, beside.Id), Is.False, "the lot alone connects nothing");

            // A pole east of the Foundry (6.5 tiles from it, 9.5 from the lot, 10.5 from the lot's centre) with a
            // generator of its own.
            RaidFixture.Power(ctx, st, SubX + 12, SubY + 1);
            Assert.That(PowerGrid.Of(ctx, st).OfSite(SubId), Is.Null, "the lot is not linked to that pole");
            Assert.That(PowerQueries.Supplied(ctx, st, beside.Id), Is.True, "the pole in reach powers the Foundry");
        }

        /// <summary>
        /// Reference campaignPower.ts:28-34: a substation is a node like a Pole, so two pole networks that each reach
        /// the lot but not each other are one circuit through it.
        /// </summary>
        [Test]
        public void ASubstationSiteBridgesTwoPoleNetworks()
        {
            var ctx = SubstationContext();
            var st = RaidFixture.State(ctx);
            RaidFixture.Power(ctx, st, 34, 41);                         // west: pole 5.5 tiles from the lot, generator
            RaidFixture.Add(ctx, st, "pole", 50, 41);                   // east: pole 7.5 tiles from the lot
            var foundry = RaidFixture.Add(ctx, st, "foundry", 52, 41);

            Assert.That(PowerQueries.Supplied(ctx, st, foundry.Id), Is.True,
                "the east Foundry is supplied by the west generator through the substation");
            Assert.That(PowerQueries.Network(ctx, st).Circuits, Is.EqualTo(1));
        }
    }
}
