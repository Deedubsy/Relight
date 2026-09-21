using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Relight.Sim.Tests.Combat;

namespace Relight.Sim.Tests.Campaign
{
    /// <summary>
    /// The objective chain (reference goal.ts:80 <c>campaignNext</c>): a priority list read from live state every
    /// tick, never a script. These checks pin the head of the list, the conditions that make a row skip itself, and
    /// the §7.0-b correction that no player-facing string ever says "magazine".
    /// </summary>
    [TestFixture]
    public sealed class OpeningObjectiveTests
    {
        [Test]
        public void AFreshWorldAsksForTheGeneratorFirst()
        {
            var ctx = OpeningFixture.Context();
            var st = OpeningFixture.State(ctx);

            var next = OpeningQueries.Objective(ctx, st);
            Assert.That(next.Title, Is.EqualTo("1 · Build a Generator"));
            Assert.That(next.Text, Is.EqualTo("Place 1 Generator in Founders Court"));
            Assert.That(next.HasLocation, Is.True, "the card can always point at Home");
            Assert.That(next.Materials, Is.Not.Empty, "and it lists what the Generator costs");
        }

        [Test]
        public void ADisabledCoreOutranksTheWholeChain()
        {
            var ctx = OpeningFixture.Context();
            var st = OpeningFixture.State(ctx);
            RaidFixture.Add(ctx, st, "generator", RaidFixture.CoreX + 10, RaidFixture.CoreY);

            HomeCore.Damage(st, 1e9);
            Assert.That(HomeQueries.CoreOperational(st), Is.False);

            var next = OpeningQueries.Objective(ctx, st);
            Assert.That(next.Id, Is.EqualTo("home-recovery"));
            Assert.That(next.Title, Is.EqualTo("Repair the Home core"));
            Assert.That(next.Text, Is.EqualTo("Press E at the Home core, then Recommission core in the workshop"));
        }

        [Test]
        public void ADownedEngineerOutranksEvenTheCore()
        {
            var ctx = OpeningFixture.Context();
            var st = OpeningFixture.State(ctx);
            HomeCore.Damage(st, 1e9);
            st.Engineer.Down = st.T + 5;

            Assert.That(OpeningQueries.Objective(ctx, st).Title, Is.EqualTo("Recover at Home"));
        }

        // ------------------------------------------------------------------ rows that skip themselves

        [Test]
        public void AnEquippedRifleSkipsTheEquipRow()
        {
            var ctx = OpeningFixture.Context();
            var st = OpeningFixture.State(ctx);
            OpeningFixture.ToRifle(ctx, st);

            var unequipped = OpeningQueries.Objective(ctx, st);
            Assert.That(unequipped.Id, Is.EqualTo("opening-equip"));
            Assert.That(unequipped.Title, Is.EqualTo("Equip your crafted Rifle"));

            st.Weapons.Slot0 = st.Weapons.Owned[0].Id;
            var equipped = OpeningQueries.Objective(ctx, st);
            Assert.That(equipped.Id, Is.Not.EqualTo("opening-equip"), "row 10 is gone on a save that already equips");
        }

        [Test]
        public void ARunningDeliverySkipsTheWholeResupplyBlock()
        {
            var ctx = OpeningFixture.Context();
            var st = OpeningFixture.State(ctx);
            OpeningFixture.ToRifle(ctx, st);
            st.Weapons.Slot0 = st.Weapons.Owned[0].Id;
            var turret = OpeningFixture.ReadyTurret(ctx, st);
            st.Opening.Status = OpeningStatus.Repelled;
            st.Opening.TurretId = turret.Id;
            st.Opening.EndedAt = -1;

            Assert.That(OpeningQueries.Objective(ctx, st).Id, Is.EqualTo("opening-ammo"),
                "with no delivery the chain asks for one");

            st.Opening.SuppliedAt = st.T;          // a delivery is on record
            st.T += 1000;                          // and the acknowledgement window is long past
            var next = OpeningQueries.Objective(ctx, st);
            Assert.That(next.Id, Is.Not.EqualTo("opening-ammo"));
            Assert.That(next.Title, Does.StartWith("Expand your defences"), "the chain moves on to §7.6");
        }

        [Test]
        public void ThreeLoadedTurretsMoveTheChainOnToScouting()
        {
            var ctx = OpeningFixture.Context();
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
            OpeningFixture.Run(ctx, st, 1);

            Assert.That(OpeningQueries.Objective(ctx, st).Title, Is.EqualTo("Prepare to scout"));

            st.Engineer.Inv[ItemId.Magazine] += 8;
            Assert.That(OpeningQueries.Objective(ctx, st).Title, Is.EqualTo("Keep your workshop producing"),
                "the terminal row");
        }

        [Test]
        public void AnEmptyThirdTurretAsksToBeLoaded()
        {
            var ctx = OpeningFixture.Context();
            var st = OpeningFixture.State(ctx);
            OpeningFixture.ToRifle(ctx, st);
            st.Weapons.Slot0 = st.Weapons.Owned[0].Id;
            OpeningFixture.ReadyTurret(ctx, st);
            RaidFixture.Turret(ctx, st, RaidFixture.CoreX - 6, RaidFixture.CoreY);
            RaidFixture.Turret(ctx, st, RaidFixture.CoreX, RaidFixture.CoreY + 6, 0);
            st.Opening.Status = OpeningStatus.Repelled;
            st.Opening.EndedAt = -1;
            st.Opening.SuppliedAt = st.T;
            st.T += 1000;
            OpeningFixture.Run(ctx, st, 1);

            Assert.That(OpeningQueries.Objective(ctx, st).Title, Is.EqualTo("Load your new turret"));
        }

        [Test]
        public void ALostTurretAsksForARebuildNotAFirstBuild()
        {
            var ctx = OpeningFixture.Context();
            var st = OpeningFixture.State(ctx);
            OpeningFixture.ToRifle(ctx, st);
            st.Weapons.Slot0 = st.Weapons.Owned[0].Id;
            st.Opening.Status = OpeningStatus.Lost;
            st.Opening.EndedAt = -1;

            var next = OpeningQueries.Objective(ctx, st);
            Assert.That(next.Title, Is.EqualTo("Rebuild your turret"));
            Assert.That(next.Detail, Does.StartWith("The attack disabled your defence."));
        }

        [Test]
        public void ACancelledRifleCraftReArmsItsOwnRow()
        {
            var ctx = OpeningFixture.Context();
            var st = OpeningFixture.State(ctx);
            OpeningFixture.ToRifle(ctx, st);
            var owned = st.Weapons.Owned[0];
            st.Weapons.Slot0 = owned.Id;
            Assert.That(OpeningQueries.Objective(ctx, st).Id, Is.Not.EqualTo("opening-rifle"));

            st.Weapons.Owned.Clear();               // the craft was cancelled: nothing is owned
            st.Weapons.Slot0 = null;
            Assert.That(OpeningQueries.Objective(ctx, st).Title, Is.EqualTo("Prepare your expedition Rifle"));
        }

        /// <summary>
        /// L-02, ALWAYS_DARK_SPEC §5.6: after the substation and before the Rifle the opening asks for one Lamp, then
        /// for power to it, and says in so many words that the flashlight does not count.
        /// </summary>
        [Test]
        public void TheOpeningAsksForOneLitLampBeforeTheRifle()
        {
            var ctx = OpeningFixture.Context();
            var st = OpeningFixture.State(ctx);
            OpeningFixture.ToRifle(ctx, st);
            st.Weapons.Owned.Clear();
            Machine lamp = null;
            foreach (var m in st.Machines) if (m.Kind == "lamp") lamp = m;
            Assert.That(lamp, Is.Not.Null);
            st.Machines.Remove(lamp);
            st.Rev++;
            OpeningFixture.Run(ctx, st, 1);

            var ask = OpeningQueries.Objective(ctx, st);
            Assert.That(ask.Id, Is.EqualTo("opening-light"));
            Assert.That(ask.Text, Is.EqualTo("Place 1 Lamp within reach of a Pole"));
            Assert.That(ask.Detail, Does.Contain("Aliens avoid lit ground"));
            Assert.That(ask.Detail, Does.Contain("flashlight").And.Contain("does not count"));

            var far = RaidFixture.Add(ctx, st, "lamp", 20, 20);        // out of every pole's reach
            OpeningFixture.Run(ctx, st, 1);
            Assert.That(OpeningQueries.Objective(ctx, st).Text, Is.EqualTo("Connect your Lamp to power"));

            st.Machines.Remove(far);
            RaidFixture.Add(ctx, st, "lamp", lamp.X, lamp.Y);
            OpeningFixture.Run(ctx, st, 1);
            Assert.That(OpeningQueries.Objective(ctx, st).Id, Is.EqualTo("opening-rifle"));
        }

        [Test]
        public void ASavePastTheRifleIsNeverSentBackForALamp()
        {
            var ctx = OpeningFixture.Context();
            var st = OpeningFixture.State(ctx);
            OpeningFixture.ToRifle(ctx, st);
            for (var i = st.Machines.Count - 1; i >= 0; i--) if (st.Machines[i].Kind == "lamp") st.Machines.RemoveAt(i);
            st.Rev++;
            OpeningFixture.Run(ctx, st, 1);

            Assert.That(OpeningQueries.Objective(ctx, st).Id, Is.EqualTo("opening-equip"));
        }

        // ------------------------------------------------------------------ the deferred card

        [Test]
        public void TheDeferredCardSaysWhenTheGroupIsComing()
        {
            var ctx = OpeningFixture.Context();
            var st = OpeningFixture.State(ctx);
            OpeningFixture.ToRifle(ctx, st);
            st.Weapons.Slot0 = st.Weapons.Owned[0].Id;
            OpeningFixture.FakeMajor(st);
            OpeningFixture.ReadyTurret(ctx, st);
            OpeningFixture.Run(ctx, st, 1);
            Assert.That(st.Opening.Status, Is.EqualTo(OpeningStatus.Deferred));

            var next = OpeningQueries.Objective(ctx, st);
            Assert.That(next.Id, Is.EqualTo("opening-attack"));
            Assert.That(next.Title, Is.EqualTo("Enemy group delayed"));
            Assert.That(next.Text, Does.StartWith("A raid is under way. The small enemy group will come once the area is safe (about "));
            Assert.That(next.Text, Does.EndWith(" s)."));
        }

        // ------------------------------------------------------------------ U-D-53: the rows that ask for a recipe

        /// <summary>
        /// U-D-53. The Foundry ships with no <c>DefaultRecipe</c>, so "Set up the Foundry" is a step the player can
        /// actually stand on. Before the change the machine arrived already smelting and the row completed itself
        /// the instant it was placed — the definition of a dead step.
        /// </summary>
        [Test]
        public void AFoundryWithNoRecipeAsksToBeSetUp()
        {
            var ctx = OpeningFixture.Context();
            var st = OpeningFixture.State(ctx);
            st.OpeningResourceVersion = 1;
            st.Stats.Made.Add(ItemId.Steel, 5);        // the hand-smelting row before this one is done
            OpeningFixture.ToRifle(ctx, st);

            Assert.That(OpeningQueries.Objective(ctx, st).Text,
                Is.EqualTo("Build a Foundry beside the Iron ore line"), "no Foundry yet");

            var foundry = RaidFixture.Add(ctx, st, "foundry", RaidFixture.CoreX + 10, RaidFixture.CoreY + 6);
            st.Rev++;
            Assert.That(ProductionRules.RecipeOf(ctx.Data, st, foundry), Is.Null, "the catalogue supplies none");

            var setUp = OpeningQueries.Objective(ctx, st);
            Assert.That(setUp.Id, Is.EqualTo("opening-smelt"));
            Assert.That(setUp.Title, Is.EqualTo("Set up the Foundry"));
            Assert.That(setUp.Text, Is.EqualTo("Select the Steel plates recipe"));

            st.Production.Of(foundry.Id).Recipe = "steel-plates";
            st.Rev++;
            Assert.That(OpeningQueries.Objective(ctx, st).Title, Is.EqualTo("Connect your plate production"),
                "choosing the recipe completes the step and the chain moves on");
        }

        /// <summary>
        /// U-D-53's other half. The Assembler has no default either, so "Build 1 Assembler and set it to Bullets"
        /// is two steps: the build row has to stop asking once the machine exists, and the second half has to be an
        /// instruction the player can act on rather than a clause on a step they already finished.
        /// </summary>
        [Test]
        public void AnAssemblerWithNoRecipeAsksForTheBulletsRecipe()
        {
            var ctx = OpeningFixture.Context();
            var st = OpeningFixture.State(ctx);
            var turret = OpeningFixture.ReadyTurret(ctx, st);
            OpeningFixture.ToRifle(ctx, st);
            RaidFixture.Power(ctx, st, RaidFixture.CoreX + 10, RaidFixture.CoreY + 3);   // the second Generator row
            st.Weapons.Slot0 = st.Weapons.Owned[0].Id;
            st.Engineer.Inv[ItemId.Magazine] += 50;
            st.Opening.Status = OpeningStatus.Repelled;
            st.Opening.EndedAt = -1;
            st.Opening.TurretId = turret.Id;
            OpeningFixture.Run(ctx, st, 1);

            Assert.That(OpeningQueries.Objective(ctx, st).Text,
                Is.EqualTo("Build 1 Assembler and set it to Bullets"), "no Assembler yet");

            var asm = RaidFixture.Add(ctx, st, "assembler", turret.X - 4, turret.Y);
            OpeningFixture.Run(ctx, st, 1);

            var set = OpeningQueries.Objective(ctx, st);
            Assert.That(set.Id, Is.EqualTo("opening-ammo"));
            Assert.That(set.Title, Is.EqualTo("Set up the Assembler"), "the build half is done; the recipe half is not");
            Assert.That(set.Text, Is.EqualTo("Select the Bullets recipe"));
            Assert.That(set.HasLocation, Is.True, "and it points at the machine they built");

            st.Production.Of(asm.Id).Recipe = "bullet-batch";
            st.Rev++;
            Assert.That(OpeningQueries.Objective(ctx, st).Text, Is.Not.EqualTo("Select the Bullets recipe"),
                "choosing it completes the step");
        }

        // ------------------------------------------------------------------ §7.0-b: never "magazine" in player text

        [Test]
        public void NoPlayerFacingStringSaysMagazine()
        {
            var seen = new List<string>();
            foreach (var text in Sweep(seen)) { }
            Assert.That(seen, Is.Not.Empty);

            foreach (var s in seen)
                Assert.That(s.ToLowerInvariant(), Does.Not.Contain("magazine"),
                    "§7.0-b: player text always says Bullets — offending string: " + s);

            // And the fixed notices the phase raises, whatever a sweep happens to reach.
            foreach (var f in typeof(OpeningPhase).GetFields(BindingFlags.Public | BindingFlags.Static))
                if (f.FieldType == typeof(string))
                    Assert.That(((string)f.GetValue(null)).ToLowerInvariant(), Does.Not.Contain("magazine"), f.Name);
        }

        [Test]
        public void TheSweepReachesTheAmmunitionRowsThatUsedToSayMagazine()
        {
            var seen = new List<string>();
            foreach (var t in Sweep(seen)) { }
            Assert.That(seen.Exists(s => s.Contains("Bullets")), Is.True,
                "the Assembler recipe is named to the player as Bullets");
            Assert.That(seen.Exists(s => s.Contains("bullets")), Is.True);
            Assert.That(seen.Exists(s => s.Contains("1 item = 1 bullet")), Is.True,
                "U-D-08 is stated where loading happens");
        }

        /// <summary>
        /// Walks the chain through every reachable row, collecting each title, text, detail and material name. Each
        /// step satisfies the row it was given, so the walk stops only at the terminal row.
        /// </summary>
        private static IEnumerable<string> Sweep(List<string> seen)
        {
            var ctx = OpeningFixture.Context();
            var st = OpeningFixture.State(ctx);
            var statuses = new[]
            {
                OpeningStatus.Pending, OpeningStatus.Deferred, OpeningStatus.Scheduled, OpeningStatus.Active,
                OpeningStatus.Repelled, OpeningStatus.Lost, OpeningStatus.Skipped,
            };

            void Take()
            {
                var v = OpeningQueries.Objective(ctx, st);
                seen.Add(v.Title); seen.Add(v.Text); seen.Add(v.Detail);
                for (var i = 0; i < v.Materials.Count; i++) seen.Add(v.Materials[i].DisplayName);
                seen.Add(OpeningQueries.Encounter(ctx, st).DeferNotice);
            }

            Take();                                                                 // 1 generator
            var gen = RaidFixture.Add(ctx, st, "generator", RaidFixture.CoreX + 10, RaidFixture.CoreY);
            Take();                                                                 // 2 excavator
            var dig = RaidFixture.Add(ctx, st, "excavator", RaidFixture.CoreX + 14, RaidFixture.CoreY);
            Take();                                                                 // 3 storage
            RaidFixture.Add(ctx, st, "chest", RaidFixture.CoreX + 18, RaidFixture.CoreY);
            Take();                                                                 // 4 belts
            for (var x = RaidFixture.CoreX + 16; x < RaidFixture.CoreX + 18; x++)
                RaidFixture.Add(ctx, st, "belt", x, RaidFixture.CoreY, Dir.E);
            Take();                                                                 // fuel
            gen.Inv.Add(ItemId.Coal, 50);
            Take();                                                                 // power
            RaidFixture.Add(ctx, st, "pole", RaidFixture.CoreX + 12, RaidFixture.CoreY);
            RaidFixture.Run(ctx, st, 1, OpeningFixture.Quiet());
            Take();                                                                 // start extraction
            OpeningFixture.Dig(ctx, st, dig);
            Take();                                                                 // 9 the Rifle
            OpeningFixture.ToRifle(ctx, st);
            Take();                                                                 // 10 equip
            st.Weapons.Slot0 = st.Weapons.Owned[0].Id;
            Take();                                                                 // 11 make ammunition
            st.Engineer.Inv[ItemId.Magazine] += 50;
            Take();                                                                 // 12 build the turret
            var turret = RaidFixture.Add(ctx, st, "turret", RaidFixture.CoreX + 6, RaidFixture.CoreY);
            RaidFixture.Run(ctx, st, 1, OpeningFixture.Quiet());
            Take();                                                                 // 13 power it / 14 fill it
            RaidFixture.Power(ctx, st, RaidFixture.CoreX + 4, RaidFixture.CoreY + 2);
            RaidFixture.Run(ctx, st, 1, OpeningFixture.Quiet());
            Take();

            // Every encounter state in turn, on a prepared turret, so the §7.4 texts are reached.
            st.Opening.TurretId = turret.Id;
            st.Opening.Origin = (RaidFixture.CoreY - 20) * RaidFixture.Size + RaidFixture.CoreX;
            st.Opening.StartsAt = st.T + 10;
            st.Opening.Count = 5;
            st.Opening.Shots = 7;
            st.Opening.DeferredUntil = st.T + 30;
            st.Opening.DeferNotice = OpeningPhase.DeferReason;
            foreach (var s in statuses)
            {
                st.Opening.Status = s;
                st.Opening.EndedAt = st.T;
                turret.Rounds = 50;
                Take();
                turret.Rounds = 0;                                                  // the "runs dry" variants
                Take();
                turret.Rounds = 50;
            }

            // The resupply block, one text at a time (§7.5).
            st.Opening.Status = OpeningStatus.Repelled;
            st.Opening.EndedAt = -1;
            st.Opening.SuppliedAt = -1;
            Take();                                                                 // build an Assembler
            var asm = RaidFixture.Add(ctx, st, "assembler", turret.X - 4, turret.Y);
            RaidFixture.Run(ctx, st, 1, OpeningFixture.Quiet());
            Take();                                                                 // U-D-53: set it to Bullets
            st.Production.Of(asm.Id).Recipe = "bullet-batch";
            st.Rev++;
            Take();                                                                 // it is not producing
            asm.Out = 1;
            st.Rev++;
            Take();                                                                 // connect the route
            RaidFixture.Add(ctx, st, "chest", turret.X - 2, turret.Y);
            st.Rev++;
            Take();                                                                 // extend the route
            RaidFixture.Add(ctx, st, "belt", turret.X - 3, turret.Y, Dir.E);
            RaidFixture.Add(ctx, st, "belt", turret.X - 1, turret.Y, Dir.E);
            st.Rev++;
            Take();                                                                 // waiting for the first batch
            st.Opening.ProducedAt = st.T;
            Take();
            st.Opening.SuppliedAt = st.T;
            Take();                                                                 // automatic resupply working

            // §7.6 and §7.7.
            st.T += 1000;
            Take();
            RaidFixture.Turret(ctx, st, RaidFixture.CoreX - 6, RaidFixture.CoreY);
            Take();
            RaidFixture.Turret(ctx, st, RaidFixture.CoreX, RaidFixture.CoreY + 6, 0);
            RaidFixture.Run(ctx, st, 1, OpeningFixture.Quiet());
            Take();                                                                 // load your new turret
            foreach (var m in st.Machines) if (m.Kind == "turret") m.Rounds = 50;
            st.Rev++;
            Take();                                                                 // prepare to scout
            st.Engineer.Inv[ItemId.Magazine] += 8;
            Take();                                                                 // terminal

            // The two rows that outrank everything.
            HomeCore.Damage(st, 1e9);
            Take();
            st.Engineer.Down = st.T + 5;
            Take();

            return seen;
        }
            // ------------------------------------------------------------------ row 8: the block substation (court D55)

        /// <summary>
        /// Row 8 completes on the substation site's own circuit having supply (goal.ts:113), not on a pole merely
        /// being nearby, and its text no longer claims a core draw or a base without power.
        /// </summary>
        [Test]
        public void TheSubstationRowCompletesOnlyWhenTheSitesOwnCircuitHasSupply()
        {
            var sites = new WorldSites(new List<SiteRecord>
            {
                new SiteRecord("home", "Home Court", SiteKind.Core, RaidFixture.CoreX, RaidFixture.CoreY,
                    RaidFixture.CoreSize, RaidFixture.CoreSize, "", 0),
                new SiteRecord("substation:0", "Substation 0", SiteKind.Substation, 109, 79, 3, 3),
                new SiteRecord(StreetLights.IdPrefix + "0", "Street light", SiteKind.Light, 112, 84, 1, 1),
                new SiteRecord(StreetLights.IdPrefix + "1", "Street light", SiteKind.Light, 104, 84, 1, 1),
            });
            var ctx = new SimContext(ReferenceData.Create(), RaidFixture.Map(), null, null, sites);
            var st = OpeningFixture.State(ctx);
            OpeningFixture.ToRifle(ctx, st);               // generator and pole at (86..88, 76), all rows 1-7 done

            var open = OpeningQueries.Objective(ctx, st);
            Assert.That(open.Title, Is.EqualTo("Connect Founders Court’s substation"));
            Assert.That(open.Detail, Does.Contain("2 streetlights"));
            Assert.That(open.Detail, Does.Not.Contain("draws"), "the core has no power demand in the port");
            Assert.That(open.Detail, Does.Not.Contain("no power"));
            Assert.That(open.Detail, Does.Not.Contain("at night"), "U-D-58: the world is always dark");
            Assert.That(open.Detail, Does.Contain("and the court lights up."),
                "the sentence ends at 'lights up.'; Explain may append a what-to-get-next sentence after it");
            Assert.That(open.Location.X, Is.EqualTo(110.5).Within(1e-9));

            // One Pole short: (95,80) links to the fixture pole but is 14 tiles from the lot.
            RaidFixture.Add(ctx, st, "pole", 95, 80);
            OpeningFixture.Run(ctx, st, 1);
            Assert.That(OpeningQueries.Objective(ctx, st).Title, Is.EqualTo("Connect Founders Court’s substation"));

            // The second Pole (102,80) is 6.5 tiles from the lot: the site joins the supplied circuit.
            RaidFixture.Add(ctx, st, "pole", 102, 80);
            OpeningFixture.Run(ctx, st, 1);
            Assert.That(OpeningQueries.Objective(ctx, st).Id, Is.EqualTo("opening-equip"), "row 8 is satisfied");
            Assert.That(PowerQueries.Network(ctx, st).DemandKw, Is.GreaterThanOrEqualTo(2 * LightRules.StreetLightKw),
                "and its two streetlights now draw from it");
        }
    }
}
