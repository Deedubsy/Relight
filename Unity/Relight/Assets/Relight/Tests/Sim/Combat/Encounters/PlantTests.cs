using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Relight.Sim.Tests.Persistence;

namespace Relight.Sim.Tests.Combat
{
    /// <summary>
    /// Batch 4, FRT-08 (REL-143): Riverside Works. The accept line: preparing is refused while any squatter lives;
    /// both costs come from carried stock only; commissioning puts 600 kW into the network; the save keeps the
    /// plant's stage.
    ///
    /// The map is <see cref="RaidFixture"/>'s open square with the real Riverside row standing on a hand-placed
    /// 1×1 site at (40, 130). Commands go through the whole composition.
    /// </summary>
    public sealed class PlantTests
    {
        const string Id = "plant:riverside";
        const string Squat = "plant:riverside:squat";
        const int Px = 40, Py = 130;

        static PlantDef Row => EncounterCatalogue.Plant(Id);

        static CommandResult Apply(SimContext ctx, SimState st, Command c) => CommandDispatcher.Apply(ctx, st, c);

        static SimContext Context() => new SimContext(ReferenceData.Create(), RaidFixture.Map(), null, null,
            new WorldSites(new List<SiteRecord>
            {
                new SiteRecord("home", "Home Court", SiteKind.Core, RaidFixture.CoreX, RaidFixture.CoreY,
                    RaidFixture.CoreSize, RaidFixture.CoreSize),
                new SiteRecord(Id, "Riverside Works", SiteKind.Plant, Px, Py, 1, 1),
            }));

        /// <summary>The engineer beside the plant, its squat born with one squatter per <paramref name="alive"/>.</summary>
        static SimState AtPlant(SimContext ctx, int alive = 0)
        {
            var st = RaidFixture.State(ctx);
            st.Engineer.Pos = new Vec2(Px + 2.5, Py + .5);
            var rec = st.Encounters.Ensure(Squat);
            rec.Resolved = true;
            rec.ResolvedAt = 0;
            for (var i = 0; i < alive; i++) RaidFixture.Guard(st, "crawler", Px - 3, Py + i).Site = Squat;
            return st;
        }

        /// <summary>Put items in the Backpack and book them as found, so the ledger balances.</summary>
        static void Give(SimState st, ItemId item, int n)
        {
            st.Engineer.Inv[item] = st.Engineer.Inv[item] + n;
            st.Stats.Found.Add(item, n);
        }

        static void Fund(SimState st, IReadOnlyList<ItemStack> cost)
        {
            for (var i = 0; i < cost.Count; i++) Give(st, cost[i].Item, cost[i].Count);
        }

        static string Off(SimContext ctx, SimState st) =>
            string.Join(" | ", Ledger.Conservation(st, ctx.Data).Problems);

        static SimState Prepared(SimContext ctx)
        {
            var st = AtPlant(ctx);
            Fund(st, Row.RepairCost);
            var r = Apply(ctx, st, new PreparePlantCommand());
            Assert.That(r.Accepted, Is.True, r.Problem);
            return st;
        }

        [Test]
        public void TheRiversideRowIsTheDesignsNumbers()
        {
            var p = Row;
            Assert.That(p, Is.Not.Null);
            Assert.That(p.Squat, Is.EqualTo(Squat));
            Assert.That(p.RepairCost.Select(s => s.ToString()), Is.EqualTo(new[] { "20xsteel", "10xconcrete" }));
            Assert.That(p.Commission.Select(s => s.ToString()), Is.EqualTo(new[] { "30xsteel", "15xcopper" }));
            Assert.That(p.Core, Is.EqualTo("freight"), "the Freight warehouse's core lights it");
            Assert.That(EncounterCatalogue.Find(p.Squat).Site, Is.EqualTo(Id));
            Assert.That(Plants.Kw(ReferenceData.Create()), Is.EqualTo(600));
        }

        [Test]
        public void PrepareIsRefusedWhileAnySquatterLives()
        {
            var ctx = Context();
            var st = AtPlant(ctx, alive: 2);
            Fund(st, Row.RepairCost);
            var guards = st.Enemies.Actors.Where(e => e.Site == Squat).ToList();

            var r = Apply(ctx, st, new PreparePlantCommand());
            Assert.That(r.Accepted, Is.False);
            Assert.That(r.Problem, Is.EqualTo(Plants.SquatText(Row)));
            guards[0].Hp = 0;
            Assert.That(Apply(ctx, st, new PreparePlantCommand()).Problem, Is.EqualTo(Plants.SquatText(Row)),
                "one squatter is still one too many");
            Assert.That(st.Engineer.Inv[ItemId.Steel], Is.EqualTo(20), "a refusal takes nothing");
            Assert.That(st.Encounters.Plant(Id), Is.Null);

            guards[1].Hp = 0;
            var ok = Apply(ctx, st, new PreparePlantCommand());
            Assert.That(ok.Accepted, Is.True, ok.Problem);
            Assert.That(ok.Problem, Is.EqualTo(Plants.ReadyText(Row)));
            Assert.That(st.Encounters.Plant(Id).Prepared, Is.True);
            Assert.That(RaidFixture.Count<PlantPreparedEvent>(st), Is.EqualTo(1));
            Assert.That(Apply(ctx, st, new PreparePlantCommand()).Problem, Is.EqualTo(Plants.PreparedText(Row)));
        }

        [Test]
        public void ASquatNobodyHasFoundYetIsNotCleared()
        {
            var ctx = Context();
            var st = RaidFixture.State(ctx);
            st.Engineer.Pos = new Vec2(Px + 2.5, Py + .5);
            Fund(st, Row.RepairCost);
            Assert.That(Apply(ctx, st, new PreparePlantCommand()).Problem, Is.EqualTo(Plants.SquatText(Row)),
                "no bodies yet is not the same as none left");
        }

        [Test]
        public void OutOfReachThereIsNoPlant()
        {
            var ctx = Context();
            var st = AtPlant(ctx);
            Fund(st, Row.RepairCost);
            st.Engineer.Pos = new Vec2(Px + ctx.Data.Engineer.ReachTiles + 3, Py + .5);
            Assert.That(Apply(ctx, st, new PreparePlantCommand()).Problem, Is.EqualTo(Plants.NoPlantText));
            Assert.That(Apply(ctx, st, new CommissionPlantCommand()).Problem, Is.EqualTo(Plants.NoPlantText));
        }

        [Test]
        public void BothCostsComeFromCarriedStockOnly()
        {
            var ctx = Context();
            var st = AtPlant(ctx);
            // Plenty at Home and in a chest beside the plant, one Steel short in the Backpack.
            st.Hand.Output.Items[ItemId.Steel] = 500;
            st.Hand.Output.Items[ItemId.Concrete] = 500;
            var chest = RaidFixture.Add(ctx, st, "chest", Px + 1, Py + 2);
            chest.Inv.Add(ItemId.Steel, 500);
            chest.Inv.Add(ItemId.Concrete, 500);
            chest.Inv.Add(ItemId.Copper, 500);
            st.Stats.Found.Add(ItemId.Steel, 1000);
            st.Stats.Found.Add(ItemId.Concrete, 1000);
            st.Stats.Found.Add(ItemId.Copper, 500);
            Give(st, ItemId.Steel, 19);
            Give(st, ItemId.Concrete, 10);

            var r = Apply(ctx, st, new PreparePlantCommand());
            Assert.That(r.Accepted, Is.False);
            Assert.That(r.Problem, Is.EqualTo("Preparing Riverside Works needs 20 steel plates and 10 concrete."));
            Assert.That(chest.Inv[ItemId.Steel], Is.EqualTo(500), "the chest is not the Backpack");
            Assert.That(st.Hand.Output.Items[ItemId.Steel], Is.EqualTo(500), "nor is Home");

            Give(st, ItemId.Steel, 1);
            Assert.That(Apply(ctx, st, new PreparePlantCommand()).Accepted, Is.True);
            Assert.That(st.Engineer.Inv[ItemId.Steel], Is.EqualTo(0));
            Assert.That(st.Engineer.Inv[ItemId.Concrete], Is.EqualTo(0));

            st.Engineer.Carrying = "freight";
            Give(st, ItemId.Steel, 30);
            Give(st, ItemId.Copper, 14);
            var c = Apply(ctx, st, new CommissionPlantCommand());
            Assert.That(c.Accepted, Is.False);
            Assert.That(c.Problem, Is.EqualTo("Commissioning Riverside Works needs 30 steel plates and 15 copper plate."));
            Assert.That(st.Engineer.Carrying, Is.EqualTo("freight"), "a refusal keeps the core in both hands");
            Assert.That(chest.Inv[ItemId.Copper], Is.EqualTo(500));

            Give(st, ItemId.Copper, 1);
            Assert.That(Apply(ctx, st, new CommissionPlantCommand()).Accepted, Is.True);
            Assert.That(st.Engineer.Inv[ItemId.Steel], Is.EqualTo(0));
            Assert.That(st.Engineer.Inv[ItemId.Copper], Is.EqualTo(0));
            Assert.That(st.Stats.Consumed[ItemId.Steel], Is.EqualTo(50));
            Assert.That(Off(ctx, st), Is.Empty, "every unit paid is explained");
        }

        [Test]
        public void CommissioningNeedsThePlantPreparedAndTheCoreInHand()
        {
            var ctx = Context();
            var st = AtPlant(ctx);
            Fund(st, Row.Commission);
            st.Engineer.Carrying = "freight";
            Assert.That(Apply(ctx, st, new CommissionPlantCommand()).Problem, Is.EqualTo(Plants.UnpreparedText(Row)));

            st = Prepared(ctx);
            Fund(st, Row.Commission);
            Assert.That(Apply(ctx, st, new CommissionPlantCommand()).Problem, Is.EqualTo(Plants.CoreText(Row)),
                "a core on the ground is not a core in the plant");

            st.Engineer.Carrying = "freight";
            st.T = 1234.5;
            var ok = Apply(ctx, st, new CommissionPlantCommand());
            Assert.That(ok.Accepted, Is.True, ok.Problem);
            var rec = st.Encounters.Plant(Id);
            Assert.That(st.Engineer.Carrying, Is.Empty, "the core is in the plant now");
            Assert.That(st.Encounters.Cores, Is.Empty, "and not on the ground either");
            Assert.That(rec.CommissionedAt, Is.EqualTo(1234.5));
            Assert.That(rec.Hp, Is.EqualTo(Plants.MaxHp(ctx.Data)).And.GreaterThan(0));
            Assert.That(st.Director.PlantRaid, Is.EqualTo(Id), "and the next major raid is owed to it (FRT-09)");
            Assert.That(st.Encounters.NewestPlant, Is.EqualTo(Id));
            var lit = RaidFixture.Last<PlantCommissionedEvent>(st);
            Assert.That(lit.Kw, Is.EqualTo(600));
            Assert.That(Plants.CardText(lit.Name, lit.Kw), Is.EqualTo("Riverside Works is lit · 600 kW · the north answers"));
            Assert.That(Apply(ctx, st, new CommissionPlantCommand()).Problem, Is.EqualTo(Plants.LitText(Row)));
        }

        [Test]
        public void TheNetworkGainsSixHundredKilowatts()
        {
            var ctx = Context();
            var st = Prepared(ctx);
            var pole = RaidFixture.Add(ctx, st, "pole", Px + 4, Py);
            var before = PowerGrid.Of(ctx, st);
            var supply = before.Supply;
            Assert.That(before.OfSite(Id), Is.Null, "an unlit plant is no node at all");
            Assert.That(before.Of(pole.Id).Supply, Is.EqualTo(0));

            Fund(st, Row.Commission);
            st.Engineer.Carrying = "freight";
            Assert.That(Apply(ctx, st, new CommissionPlantCommand()).Accepted, Is.True);
            var after = PowerGrid.Of(ctx, st);
            Assert.That(after.Supply - supply, Is.EqualTo(600));
            Assert.That(after.OfSite(Id), Is.SameAs(after.Of(pole.Id)), "the pole beside it cables to it");
            Assert.That(after.Of(pole.Id).Supply, Is.EqualTo(600));
            Assert.That(after.Of(pole.Id).PlantKw, Is.EqualTo(600));

            st.Encounters.Plant(Id).Hp = 0;
            st.Rev++;
            Assert.That(PowerGrid.Of(ctx, st).Supply, Is.EqualTo(supply), "a wrecked plant supplies nothing");
        }

        [Test]
        public void TheSaveKeepsThePlantsStage()
        {
            var ctx = Context();
            var st = Prepared(ctx);
            var a = SaveSerializer.ReadText(SaveSerializer.WriteText(st, ctx.Data), ctx.Data);
            Assert.That(a.Ok, Is.True, a.Reason);
            Assert.That(a.Header.Version, Is.EqualTo(SaveSchema.Version));
            Assert.That(a.State.Encounters.Plant(Id).Prepared, Is.True);
            Assert.That(a.State.Encounters.Plant(Id).Commissioned, Is.False);
            Assert.That(StateHash.Compute(a.State), Is.EqualTo(StateHash.Compute(st)));

            Fund(st, Row.Commission);
            st.Engineer.Carrying = "freight";
            st.T = 77;
            Assert.That(Apply(ctx, st, new CommissionPlantCommand()).Accepted, Is.True);
            var b = SaveSerializer.ReadText(SaveSerializer.WriteText(st, ctx.Data), ctx.Data);
            Assert.That(b.Ok, Is.True, b.Reason);
            var rec = b.State.Encounters.Plant(Id);
            Assert.That(rec.CommissionedAt, Is.EqualTo(77));
            Assert.That(rec.Hp, Is.EqualTo(Plants.MaxHp(ctx.Data)));
            Assert.That(b.State.Encounters.NewestPlant, Is.EqualTo(Id));
            Assert.That(StateHash.Compute(b.State), Is.EqualTo(StateHash.Compute(st)));
            Assert.That(PowerGrid.Of(ctx, b.State).OfSite(Id)?.PlantKw, Is.EqualTo(600), "and it is still lit");
        }

        [Test]
        public void AVersionSeventeenSaveUpgradesWithNoPlantTouched()
        {
            var ctx = Context();
            var st = RaidFixture.State(ctx);
            var state = PersistenceFixture.Canonical(st);
            Assert.That(state, Does.Contain("\"plants\":[]"));
            Assert.That(state, Does.Contain("\"newestPlant\":\"\""));
            var state17 = Regex.Replace(state, "\"plants\":\\[\\],?", "");
            state17 = Regex.Replace(state17, "\"newestPlant\":\"\",?", "").Replace(",}", "}");
            Assert.That(state17, Does.Not.Contain("\"plants\"").And.Not.Contain("\"newestPlant\""));

            var file = SaveSerializer.WriteText(st, ctx.Data);
            file = PersistenceFixture.Retarget(file, "state", state17);
            file = PersistenceFixture.Retarget(file, "version", "17");
            file = PersistenceFixture.Retarget(file, "hash", CanonicalJsonWriter.QuoteString(StateHash.Of(state17)));

            var old = SaveSerializer.ReadText(file, ctx.Data);
            Assert.That(old.Ok, Is.True, old.Reason);
            Assert.That(old.Header.Version, Is.EqualTo(17));
            Assert.That(old.Upgraded, Does.Contain("no power plant touched"));
            Assert.That(old.State.Encounters.Plants, Is.Empty);
            Assert.That(old.State.Encounters.NewestPlant, Is.Empty);
        }
    }
}
