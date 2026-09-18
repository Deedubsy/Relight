using NUnit.Framework;
using Relight.Sim.Tests.Campaign;
using Relight.Sim.Tests.Combat;
using Relight.Sim.Tests.Production;

namespace Relight.Sim.Tests.Regression
{
    /// <summary>
    /// GP-PLAYTEST-1 — the defects a 30 sim-minute objective playthrough of a fresh save turned up, one test per
    /// fix. The run that produced them is recorded in Unity/Docs/evidence/first-thirty-minutes/; what matters here
    /// is that each row states the behaviour the player is owed, not the wording that happened to carry it.
    /// </summary>
    [TestFixture]
    public sealed class PlaytestFixTests
    {
        // ------------------------------------------------------------------ U-D-54: "repelled" consults the core

        /// <summary>
        /// The playthrough ended with "Attack repelled" printed over a Home core at 0/300 HP, because the end
        /// state asked only whether the turret was alive. The attackers had walked past an out-of-range turret,
        /// which never fired a shot, and flattened the core — the one outcome no player can read as a win.
        /// </summary>
        [Test]
        public void ALiveTurretOverADeadCoreIsNotARepulse()
        {
            var ctx = OpeningFixture.Context();
            var st = OpeningFixture.State(ctx);
            var turret = OpeningFixture.ToActive(ctx, st);

            HomeCore.Damage(st, HomeQueries.CoreHp(st));
            Assert.That(HomeQueries.CoreOperational(st), Is.False, "the core is down");
            Assert.That(TurretQueries.Hp(ctx, st, turret.Id), Is.GreaterThan(0), "and the turret is untouched");

            OpeningFixture.Kill(st, st.Opening.Group);
            OpeningFixture.Run(ctx, st, 1);

            Assert.That(st.Opening.Status, Is.EqualTo(OpeningStatus.Lost));
            Assert.That(RaidFixture.Last<OpeningEndedEvent>(st).Repelled, Is.False,
                "the event the HUD and the save both read agrees with the status");
        }

        /// <summary>The other half of U-D-54: both intact is still a repulse, so the ordinary win is untouched.</summary>
        [Test]
        public void ALiveTurretOverALiveCoreIsStillARepulse()
        {
            var ctx = OpeningFixture.Context();
            var st = OpeningFixture.State(ctx);
            OpeningFixture.ToActive(ctx, st);

            OpeningFixture.Kill(st, st.Opening.Group);
            OpeningFixture.Run(ctx, st, 1);

            Assert.That(st.Opening.Status, Is.EqualTo(OpeningStatus.Repelled));
            Assert.That(RaidFixture.Last<OpeningEndedEvent>(st).Repelled, Is.True);
        }

        // ------------------------------------------------------------------ the core repair names what it charges

        /// <summary>
        /// The repair CHARGES steel and copper, but the row carried the numbers in prose only, so the card showed
        /// no material chips and an empty Backpack got no route to the ore.
        /// </summary>
        [Test]
        public void TheCoreRepairRowCarriesTheMaterialsItCharges()
        {
            var ctx = OpeningFixture.Context();
            var st = OpeningFixture.State(ctx);
            HomeCore.Damage(st, HomeQueries.CoreHp(st));

            var row = OpeningQueries.Objective(ctx, st);
            Assert.That(row.Id, Is.EqualTo("home-recovery"));
            Assert.That(row.Materials, Is.Not.Empty, "the card shows what the repair costs");

            var d = ctx.Data;
            var steel = Find(row, "steel");
            var copper = Find(row, "copper");
            Assert.That(steel.Required, Is.EqualTo(d.Defence.CoreSteel), "exactly what HomeCore charges");
            Assert.That(copper.Required, Is.EqualTo(d.Defence.CoreCopper));
            Assert.That(steel.Source, Is.Not.Empty, "and an empty Backpack is told where to get it");
        }

        private static ObjectiveMaterial Find(ObjectiveView row, string key)
        {
            foreach (var m in row.Materials) if (m.Item == key) return m;
            Assert.Fail("the row carries no " + key + " row");
            return default;
        }

        // ------------------------------------------------------------------ the belt nobody can see is wrong

        /// <summary>
        /// A conveyor PULLS from the tile behind its arrow and <c>FlowRules.Accepts</c> never tests direction, so a
        /// belt laid alongside its intended source is accepted in silence and then carries nothing for the rest of
        /// the run. This is advice for the placement preview, never a refusal.
        /// </summary>
        [Test]
        public void ABeltBesideItsSourceRatherThanInFrontOfItIsWarnedAbout()
        {
            var ctx = ProductionFixture.Context();
            var st = ProductionFixture.State(ctx);
            var dig = ProductionFixture.Add(ctx, st, "excavator", 10, 10, Dir.S);
            var east = dig.X + dig.Size;

            var sideways = BeltSight.Advise(ctx, st, "belt", east, 10, Dir.N);
            Assert.That(sideways, Is.Not.Empty, "the excavator is beside this belt, not behind it");
            Assert.That(sideways, Does.Contain("behind"), "and the sentence names the rule that decides it");
            Assert.That(sideways, Does.Contain("R"), "and the key that fixes it");
        }

        [Test]
        public void ABeltPointingAwayFromItsSourceIsLeftAlone()
        {
            var ctx = ProductionFixture.Context();
            var st = ProductionFixture.State(ctx);
            var dig = ProductionFixture.Add(ctx, st, "excavator", 10, 10, Dir.S);
            var east = dig.X + dig.Size;

            Assert.That(BeltSight.Advise(ctx, st, "belt", east, 10, Dir.E), Is.Empty,
                "facing east, the tile behind is the excavator: this is the correct build");
        }

        [Test]
        public void ABeltDeliveringIntoAMachineIsNotAMisrotation()
        {
            var ctx = ProductionFixture.Context();
            var st = ProductionFixture.State(ctx);
            var dig = ProductionFixture.Add(ctx, st, "excavator", 10, 10, Dir.S);
            var east = dig.X + dig.Size;

            Assert.That(BeltSight.Advise(ctx, st, "belt", east, 10, Dir.W), Is.Empty,
                "pointing INTO the machine is a delivery, which is an ordinary thing to build");
        }

        [Test]
        public void ABeltFedByTheBeltUpstreamOfItIsLeftAlone()
        {
            var ctx = ProductionFixture.Context();
            var st = ProductionFixture.State(ctx);
            var dig = ProductionFixture.Add(ctx, st, "excavator", 10, 10, Dir.S);
            var east = dig.X + dig.Size;
            ProductionFixture.Add(ctx, st, "belt", east, 9, Dir.S);

            Assert.That(BeltSight.Advise(ctx, st, "belt", east, 10, Dir.S), Is.Empty,
                "a belt in the middle of a line is fed by the one behind it");
        }

        [Test]
        public void OpenGroundWithNothingAroundItSaysNothing()
        {
            var ctx = ProductionFixture.Context();
            var st = ProductionFixture.State(ctx);

            Assert.That(BeltSight.Advise(ctx, st, "belt", 40, 40, Dir.N), Is.Empty,
                "half a belt line out in the open is not a mistake yet");
            Assert.That(BeltSight.Advise(ctx, st, "excavator", 40, 40, Dir.N), Is.Empty,
                "and nothing but a conveyor is ever advised about");
        }
    }
}
