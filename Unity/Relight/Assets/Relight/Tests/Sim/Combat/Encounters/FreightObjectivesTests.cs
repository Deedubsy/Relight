using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Relight.Sim.Tests.Campaign;
using Relight.Sim.UI;

namespace Relight.Sim.Tests.Combat
{
    /// <summary>
    /// Batch 4, FRT-10 (REL-145): the Freight chapter's nine objectives (design §5.9) and the HUD reads beside them.
    /// The accept line: each objective completes on its event.
    ///
    /// The map is <see cref="RaidFixture"/>'s open square with the chapter's places laid out far apart: the three key
    /// camps as bare markers, the warehouse floor and one door, and Riverside Works. The camps have no group sites,
    /// so nothing is ever born here; a camp is "found" by marking its record, which is all the objectives read (the
    /// finding itself is <see cref="EncounterPhaseTests"/>'). Every other step is driven by the real rule that fires
    /// its event: the hold clock, the door, the guardian's death, the lift, and the plant's two commands.
    /// </summary>
    public sealed class FreightObjectivesTests
    {
        const int PerSecond = 20;                            // RaidFixture.Dt is a twentieth of a second
        static readonly (int X, int Y)[] CampAt = { (31, 29), (29, 131), (131, 131) };
        const string Arena = "freight:arena";
        const int PlantX = 40, PlantY = 100;

        static SimContext Context()
        {
            var sites = new List<SiteRecord>
            {
                new SiteRecord("home", "Home Court", SiteKind.Core, RaidFixture.CoreX, RaidFixture.CoreY,
                    RaidFixture.CoreSize, RaidFixture.CoreSize, "", 0),
                new SiteRecord("freight:door:0", "Freight warehouse door 0", SiteKind.StrongholdDoor, 130, 36, 3, 1),
                new SiteRecord(Arena, "Freight warehouse floor", SiteKind.Arena, 125, 20, 20, 15, "", 60),
                new SiteRecord(FreightObjectives.Plant, "Riverside Works", SiteKind.Plant, PlantX, PlantY, 2, 2),
            };
            for (var k = 0; k < 3; k++)
                sites.Add(new SiteRecord(FreightObjectives.Camps[k], "camp " + k, SiteKind.Camp, CampAt[k].X, CampAt[k].Y,
                    1, 1, "", 20));
            return new SimContext(ReferenceData.Create(), RaidFixture.Map(), null, null, new WorldSites(sites));
        }

        /// <summary>A state with every opening row behind the player, so the objective is the chapter's.</summary>
        static SimState State(SimContext ctx)
        {
            var st = OpeningFixture.State(ctx);
            OpeningFixture.ToRifle(ctx, st);
            st.Weapons.Slot0 = st.Weapons.Owned[0].Id;
            OpeningFixture.ReadyTurret(ctx, st);
            RaidFixture.Turret(ctx, st, RaidFixture.CoreX - 6, RaidFixture.CoreY);
            RaidFixture.Turret(ctx, st, RaidFixture.CoreX, RaidFixture.CoreY + 6);
            st.Opening.Status = OpeningStatus.Repelled;
            st.Opening.EndedAt = -1;
            st.Opening.SuppliedAt = st.T;
            st.T += 1000;
            st.Engineer.Inv[ItemId.Magazine] += 8;
            OpeningFixture.Run(ctx, st, 1);
            st.Engineer.Pos = new Vec2(RaidFixture.CoreX + 4, RaidFixture.CoreY + 12);
            return st;
        }

        static void Found(SimState st, string id)
        {
            var rec = st.Encounters.Ensure(id);
            rec.Resolved = true;
            rec.ResolvedAt = st.T;
        }

        static void Seconds(SimContext ctx, SimState st, double s) =>
            RaidFixture.Run(ctx, st, (int)(s * PerSecond), new List<ITickPhase> { new EncounterPhase() });

        static Vec2 Marker(int k) => new Vec2(CampAt[k].X + .5, CampAt[k].Y + .5);

        static List<FreightCircle> Circles(SimContext ctx, SimState st)
        {
            var list = new List<FreightCircle>();
            FreightObjectives.Circles(ctx, st, list);
            return list;
        }

        /// <summary>Hold camp k the way the sim claims it: 30 seconds on its marker with no guard near.</summary>
        static void Hold(SimContext ctx, SimState st, int k)
        {
            Found(st, FreightObjectives.Camps[k]);
            st.Engineer.Pos = Marker(k);
            Seconds(ctx, st, 31);
            Assert.That(st.Encounters.Holds(FreightObjectives.Camps[k]), Is.True);
        }

        static void AllKeys(SimContext ctx, SimState st)
        {
            for (var k = 0; k < 3; k++) Hold(ctx, st, k);
        }

        static Enemy Guardian(SimState st)
        {
            var g = RaidFixture.Body(st, GuardianRules.Kind, 134, 27, EnemyLayer.Site, 0);
            g.Site = Arena;
            return g;
        }

        [Test]
        public void AMapWithoutTheFreightPlacesHasNoChapter()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            Assert.That(FreightObjectives.Present(ctx), Is.False);
            Assert.That(FreightObjectives.Step(ctx, st), Is.Zero);
            Assert.That(FreightObjectives.Next(ctx, st).HasValue, Is.False);
            Assert.That(Circles(ctx, st), Is.Empty);
            Assert.That(FreightObjectives.KeysLine(ctx, st) + FreightObjectives.HoldLine(ctx, st)
                + FreightObjectives.PlantLine(ctx, st), Is.Empty);
        }

        [Test]
        public void TheChapterFollowsTheOpeningAndTheTerminalRowMovesAfterIt()
        {
            var ctx = Context();
            var st = State(ctx);

            var first = OpeningQueries.Objective(ctx, st);
            Assert.That(first.Id, Is.EqualTo("freight-1"), first.Title);
            Assert.That(first.Title, Is.EqualTo("Find where they're coming from"));

            var rec = new PlantRecord { Id = FreightObjectives.Plant, Prepared = true, CommissionedAt = st.T, Hp = 500 };
            st.Encounters.Plants.Add(rec);
            var last = OpeningQueries.Objective(ctx, st);
            Assert.That(last.Title, Is.EqualTo("Keep your workshop producing"));
            Assert.That(last.Text, Is.EqualTo(OpeningQueries.FreightDoneText));
            Assert.That(last.HasLocation, Is.False);
            Assert.That((last.Title + " " + last.Text + " " + last.Detail).ToLowerInvariant(),
                Does.Not.Contain("won").And.Not.Contain("victory").And.Not.Contain("complete"),
                "F-05: the port never marks the game complete");
        }

        [Test]
        public void NoSearchCircleShowsBeforeTheChapter()
        {
            var ctx = Context();
            var st = OpeningFixture.State(ctx);
            Assert.That(FreightObjectives.Step(ctx, st), Is.EqualTo(1), "the step reads 1 from the first tick");
            Assert.That(OpeningQueries.Objective(ctx, st).Id, Does.Not.StartWith("freight-"));
            Assert.That(Circles(ctx, st), Is.Empty, "the search circle waits for the chapter");

            Found(st, FreightObjectives.Camps[0]);
            var c = Circles(ctx, st);
            Assert.That(c.Count, Is.EqualTo(1), "a camp the player found on their own still shows its hold ring");
            Assert.That(c[0].Hold, Is.True);
        }

        [Test]
        public void FindingTheWestPassageCompletesTheFirstObjective()
        {
            var ctx = Context();
            var st = State(ctx);
            var o = FreightObjectives.Next(ctx, st).Value;
            Assert.That(o.Id, Is.EqualTo("freight-1"));
            Assert.That(o.Location, Is.EqualTo(new Vec2(40, 20)), "§5.1: the marker (31.5, 29.5) rounded to 20 tiles");
            var c = Circles(ctx, st);
            Assert.That(c.Count, Is.EqualTo(1), "only the first camp is searched for yet");
            Assert.That((c[0].Camp, c[0].Hold, c[0].Radius), Is.EqualTo((FreightObjectives.Camps[0], false, 20.0)));

            Found(st, FreightObjectives.Camps[0]);
            Assert.That(FreightObjectives.Step(ctx, st), Is.EqualTo(2));
            o = FreightObjectives.Next(ctx, st).Value;
            Assert.That(o.Id, Is.EqualTo("freight-2"));
            Assert.That(o.Location, Is.EqualTo(Marker(0)), "a found camp is marked precisely");
            c = Circles(ctx, st);
            Assert.That(c.Count, Is.EqualTo(1));
            Assert.That((c[0].Hold, c[0].Centre, c[0].Fraction), Is.EqualTo((true, Marker(0), 0.0)),
                "the search circle becomes the hold ring");
        }

        [Test]
        public void HoldingEachKeyCampCompletesItsObjective()
        {
            var ctx = Context();
            var st = State(ctx);
            Found(st, FreightObjectives.Camps[0]);
            st.Engineer.Pos = Marker(0);
            Seconds(ctx, st, 15);
            Assert.That(FreightObjectives.HoldLine(ctx, st), Does.StartWith("Holding ").And.EndWith("\u00A0/\u00A030\u00A0s"), "the count does not break across lines");
            Assert.That(FreightObjectives.Next(ctx, st).Value.Text, Does.StartWith("Holding: 1"));
            var ring = Circles(ctx, st).Single(r => r.Hold);
            Assert.That(ring.Fraction, Is.InRange(0.45, 0.55), "the ring fills with the clock");
            Assert.That(FreightObjectives.KeysLine(ctx, st), Is.EqualTo("Keys 0/3"));

            Seconds(ctx, st, 16);
            Assert.That(RaidFixture.Count<EncounterClaimedEvent>(st), Is.EqualTo(1));
            Assert.That(FreightObjectives.Step(ctx, st), Is.EqualTo(3), "key 1 held");
            Assert.That(FreightObjectives.Next(ctx, st).Value.Title, Does.StartWith("Push north"));
            Assert.That(FreightObjectives.KeysLine(ctx, st), Is.EqualTo("Keys 1/3"));
            Assert.That(FreightObjectives.HoldLine(ctx, st), Is.Empty, "a held camp has no clock");
            Assert.That(Circles(ctx, st).Select(r => r.Camp), Is.EqualTo(new[] { FreightObjectives.Camps[1] }),
                "the next camp's search circle, and no ring at the held one");

            Hold(ctx, st, 1);
            Assert.That(FreightObjectives.Step(ctx, st), Is.EqualTo(4), "key 2 held");
            Assert.That(FreightObjectives.Next(ctx, st).Value.Title, Does.StartWith("Northwood approach"));

            Hold(ctx, st, 2);
            Assert.That(FreightObjectives.Step(ctx, st), Is.EqualTo(5), "key 3 held");
            Assert.That(FreightObjectives.KeysLine(ctx, st), Is.EqualTo("Keys 3/3"));
            Assert.That(RaidFixture.Count<EncounterClaimedEvent>(st), Is.EqualTo(3));
        }

        [Test]
        public void AGuardAtTheCampSaysWhyTheClockIsNotRunning()
        {
            var ctx = Context();
            var st = State(ctx);
            Found(st, FreightObjectives.Camps[0]);
            var guard = RaidFixture.Body(st, "skitter", Marker(0).X + 10, Marker(0).Y, EnemyLayer.Site, 0);
            guard.Site = FreightObjectives.Camps[0];
            st.Engineer.Pos = Marker(0);
            Seconds(ctx, st, .1);
            Assert.That(FreightObjectives.HoldLine(ctx, st), Does.Contain("clear the guards"));
            Assert.That(FreightObjectives.Next(ctx, st).Value.Text, Does.StartWith("Clear the camp (1 left)"));
        }

        [Test]
        public void OpeningTheWarehouseCompletesTheFifthObjective()
        {
            var ctx = Context();
            var st = State(ctx);
            AllKeys(ctx, st);
            var o = FreightObjectives.Next(ctx, st).Value;
            Assert.That(o.Id, Is.EqualTo("freight-5"));
            Assert.That(o.Location, Is.EqualTo(new Vec2(131.5, 36.5)), "the door");

            st.Engineer.Pos = new Vec2(131.5, 38);
            StrongholdRules.Tick(ctx, st);
            Assert.That(RaidFixture.Count<StrongholdOpenedEvent>(st), Is.EqualTo(1));
            Assert.That(FreightObjectives.Step(ctx, st), Is.EqualTo(6));
            Assert.That(FreightObjectives.KeysLine(ctx, st), Is.Empty, "the keys have done their work");
        }

        [Test]
        public void KillingTheGuardianCompletesTheSixthObjective()
        {
            var ctx = Context();
            var st = State(ctx);
            AllKeys(ctx, st);
            st.Encounters.Opened.Add(FreightObjectives.Stronghold);
            var g = Guardian(st);
            var o = FreightObjectives.Next(ctx, st).Value;
            Assert.That(o.Id, Is.EqualTo("freight-6"));
            Assert.That(o.Location, Is.EqualTo(g.Pos), "the guardian itself while it lives");

            st.Enemies.Actors.Remove(g);
            GuardianRules.Died(st, g);
            Assert.That(RaidFixture.Count<GuardianKilledEvent>(st), Is.EqualTo(1));
            Assert.That(FreightObjectives.Step(ctx, st), Is.EqualTo(7));
            o = FreightObjectives.Next(ctx, st).Value;
            Assert.That(o.Location, Is.EqualTo(g.Pos), "the core lies where it fell");
            Assert.That(o.Text, Does.StartWith("Pick up the Power core"));
        }

        [Test]
        public void CarryingTheCoreToThePlantCompletesTheSeventhObjective()
        {
            var ctx = Context();
            var st = State(ctx);
            AllKeys(ctx, st);
            st.Encounters.Opened.Add(FreightObjectives.Stronghold);
            var g = Guardian(st);
            st.Enemies.Actors.Remove(g);
            GuardianRules.Died(st, g);

            st.Engineer.Pos = g.Pos;
            Assert.That(CoreCarry.PickUp(ctx, st).ok, Is.True);
            Assert.That(FreightObjectives.CarryingLine(st), Is.EqualTo(FreightObjectives.CarryLine));
            var o = FreightObjectives.Next(ctx, st).Value;
            Assert.That(o.Text, Does.StartWith("Walk the Power core to Riverside Works"));
            Assert.That(o.Location, Is.EqualTo(new Vec2(PlantX + 1, PlantY + 1)), "the plant, while it is carried");
            Assert.That(FreightObjectives.PlantLine(ctx, st), Does.StartWith("Riverside Works · squatters inside"));

            st.Engineer.Pos = new Vec2(PlantX + 1, PlantY + 9.5);
            Assert.That(FreightObjectives.Step(ctx, st), Is.EqualTo(7), "8.5 tiles out is not there yet");
            st.Engineer.Pos = new Vec2(PlantX + 1, PlantY + 8.5);
            Assert.That(FreightObjectives.Step(ctx, st), Is.EqualTo(8), "7.5 tiles out has arrived");

            Assert.That(CoreCarry.Drop(st).ok, Is.True);
            Assert.That(FreightObjectives.CarryingLine(st), Is.Empty);
            Assert.That(FreightObjectives.Step(ctx, st), Is.EqualTo(8), "a core set down beside the plant still counts");
        }

        [Test]
        public void PreparingAndCommissioningCompleteTheLastTwoObjectives()
        {
            var ctx = Context();
            var st = State(ctx);
            AllKeys(ctx, st);
            st.Encounters.Opened.Add(FreightObjectives.Stronghold);
            var g = Guardian(st);
            st.Enemies.Actors.Remove(g);
            GuardianRules.Died(st, g);
            st.Engineer.Pos = g.Pos;
            CoreCarry.PickUp(ctx, st);
            st.Engineer.Pos = new Vec2(PlantX + 1, PlantY + 3);

            var o = FreightObjectives.Next(ctx, st).Value;
            Assert.That(o.Id, Is.EqualTo("freight-8"));
            Assert.That(o.Text, Does.StartWith("Clear the squatters"));
            Assert.That(o.Materials.Select(m => (m.Item, m.Required)),
                Is.EqualTo(new[] { ("steel", 20), ("concrete", 10) }), "the repair cost");

            Found(st, "plant:riverside:squat");
            st.Engineer.Inv[ItemId.Steel] += 50;
            st.Engineer.Inv[ItemId.Concrete] += 10;
            st.Engineer.Inv[ItemId.Copper] += 15;
            Assert.That(FreightObjectives.PlantLine(ctx, st), Is.EqualTo("Riverside Works · clear · needs its repair"));
            Assert.That(FreightObjectives.Next(ctx, st).Value.Text, Does.StartWith("Press E at Riverside Works"));

            var (ok, reason) = Plants.Prepare(ctx, st);
            Assert.That(ok, Is.True, reason);
            Assert.That(RaidFixture.Count<PlantPreparedEvent>(st), Is.EqualTo(1));
            Assert.That(FreightObjectives.Step(ctx, st), Is.EqualTo(9));
            o = FreightObjectives.Next(ctx, st).Value;
            Assert.That(o.Title, Is.EqualTo("Commission the plant"));
            Assert.That(o.Materials.Select(m => (m.Item, m.Required)),
                Is.EqualTo(new[] { ("steel", 30), ("copper", 15) }), "the commission cost");
            Assert.That(FreightObjectives.PlantLine(ctx, st), Does.Contain("prepared"));

            (ok, reason) = Plants.Commission(ctx, st);
            Assert.That(ok, Is.True, reason);
            Assert.That(RaidFixture.Count<PlantCommissionedEvent>(st), Is.EqualTo(1));
            Assert.That(FreightObjectives.Step(ctx, st), Is.EqualTo(FreightObjectives.Done));
            Assert.That(FreightObjectives.Next(ctx, st).HasValue, Is.False);
            Assert.That(Circles(ctx, st), Is.Empty);
            Assert.That(FreightObjectives.PlantLine(ctx, st), Is.EqualTo(
                "Riverside Works · lit · 600 kW · " + Plants.MaxHp(ctx.Data) + "/" + Plants.MaxHp(ctx.Data) + " hp"));
        }

        [Test]
        public void ASaveLoadsOntoTheSameObjective()
        {
            var ctx = Context();
            var st = State(ctx);
            Hold(ctx, st, 0);
            Found(st, FreightObjectives.Camps[1]);
            st.Engineer.Pos = Marker(1);
            Seconds(ctx, st, 10);
            var before = FreightObjectives.Next(ctx, st).Value;

            var load = SaveSerializer.ReadText(SaveSerializer.WriteText(st, ctx.Data), ctx.Data);
            Assert.That(load.Ok, Is.True, load.Reason);
            var after = FreightObjectives.Next(ctx, load.State).Value;
            Assert.That((after.Id, after.Text), Is.EqualTo((before.Id, before.Text)));
            Assert.That(StateHash.Compute(load.State), Is.EqualTo(StateHash.Compute(st)), "the reads write nothing");
        }

        [Test]
        public void TheHudCarriesTheChapterLinesAndItsMoments()
        {
            var ctx = Context();
            var st = State(ctx);
            Found(st, FreightObjectives.Camps[0]);
            st.Engineer.Pos = Marker(0);
            Seconds(ctx, st, 5);

            var vm = new HudViewModel();
            vm.Refresh(ctx, st, 0, false, false, true);
            Assert.That(vm.FreightKeys, Is.EqualTo("Keys 0/3"));
            Assert.That(vm.FreightHold, Does.StartWith("Holding "));
            Assert.That(vm.FreightCarry, Is.Empty);

            vm.Intake(new SimEvent[]
            {
                new EncounterClaimedEvent(1, FreightObjectives.Camps[0], FreightObjectives.Camps[0], 1, 3,
                    "West passage secured — key 1 of 3"),
                new StrongholdOpenedEvent(2, FreightObjectives.Stronghold, "Freight warehouse doors open"),
                new GuardianKilledEvent(3, 9, FreightObjectives.Stronghold, 1, 1),
            }, 0);
            vm.Notices.Reap(0);
            var rows = vm.Notices.Rows.Select(r => r.Text).ToList();
            Assert.That(rows, Does.Contain("West passage secured — key 1 of 3"));
            Assert.That(rows, Does.Contain("Freight warehouse doors open"));
            Assert.That(rows, Does.Contain(HudViewModel.GuardianKilledText));
        }
    }
}
