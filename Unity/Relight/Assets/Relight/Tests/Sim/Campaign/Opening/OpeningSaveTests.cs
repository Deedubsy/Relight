using NUnit.Framework;
using Relight.Sim.Tests.Combat;

namespace Relight.Sim.Tests.Campaign
{
    /// <summary>
    /// The opening is saved facts only (reference openingEncounter.ts:15 <c>OpeningEncounter</c>), so a save taken at
    /// any point of the fifteen minutes must come back reading exactly the same and carry on from there.
    /// </summary>
    [TestFixture]
    public sealed class OpeningSaveTests
    {
        [Test]
        public void ASaveInEveryStateComesBackUnchanged()
        {
            foreach (OpeningStatus status in System.Enum.GetValues(typeof(OpeningStatus)))
            {
                var ctx = OpeningFixture.Context();
                var st = OpeningFixture.State(ctx);
                var op = st.Opening;
                op.Status = status;
                op.Shots = 13;
                op.Group = 4;
                op.TurretId = 9;
                op.Origin = 77 * RaidFixture.Size + 40;
                op.ScheduledAt = 11.5;
                op.StartsAt = 36.5;
                op.EndedAt = 120.25;
                op.Count = 5;
                op.SuppliedAt = 200.5;
                op.ProducedAt = 180.25;
                op.DeferredUntil = 400.75;
                op.DeferCount = 2;
                op.DeferNotice = OpeningPhase.DeferReason;
                op.FedSeen = 7;

                var back = OpeningFixture.RoundTrip(ctx, st).Opening;
                Assert.That(back.Status, Is.EqualTo(status), status.ToString());
                Assert.That(back.Shots, Is.EqualTo(13));
                Assert.That(back.Group, Is.EqualTo(4));
                Assert.That(back.TurretId, Is.EqualTo(9));
                Assert.That(back.Origin, Is.EqualTo(op.Origin));
                Assert.That(back.ScheduledAt, Is.EqualTo(11.5));
                Assert.That(back.StartsAt, Is.EqualTo(36.5));
                Assert.That(back.EndedAt, Is.EqualTo(120.25));
                Assert.That(back.Count, Is.EqualTo(5));
                Assert.That(back.SuppliedAt, Is.EqualTo(200.5));
                Assert.That(back.ProducedAt, Is.EqualTo(180.25));
                Assert.That(back.DeferredUntil, Is.EqualTo(400.75));
                Assert.That(back.DeferCount, Is.EqualTo(2));
                Assert.That(back.DeferNotice, Is.EqualTo(OpeningPhase.DeferReason));
                Assert.That(back.FedSeen, Is.EqualTo(7));
            }
        }

        [Test]
        public void AnAnnouncedEncounterResumesAndStillArrives()
        {
            var ctx = OpeningFixture.Context();
            var st = OpeningFixture.State(ctx);
            OpeningFixture.ReadyTurret(ctx, st);
            OpeningFixture.Run(ctx, st, 1);
            Assert.That(st.Opening.Status, Is.EqualTo(OpeningStatus.Scheduled));

            var back = OpeningFixture.RoundTrip(ctx, st);
            Assert.That(back.Opening.StartsAt, Is.EqualTo(st.Opening.StartsAt).Within(1e-9));
            Assert.That(back.Director.Reserved, Is.True, "the hold on ordinary raids survives the load");

            OpeningFixture.Seconds(ctx, back, OpeningRules.Tuning(ctx.Data).WarningS + 1);
            Assert.That(back.Opening.Status, Is.EqualTo(OpeningStatus.Active), "and it is not re-announced, it starts");
            Assert.That(RaidFixture.Count<OpeningScheduledEvent>(back), Is.EqualTo(0),
                "the loaded state announces nothing a second time");
        }

        [Test]
        public void AnActiveEncounterResumesWithItsGroupAndItsShotCount()
        {
            var ctx = OpeningFixture.Context();
            var st = OpeningFixture.State(ctx);
            var turret = OpeningFixture.ToActive(ctx, st);
            st.Events.Add(new TurretShotEvent(st.T, turret.Id, 0, 1, 1, true));
            OpeningFixture.Run(ctx, st, 1);
            var shots = st.Opening.Shots;
            Assert.That(shots, Is.GreaterThan(0));

            var back = OpeningFixture.RoundTrip(ctx, st);
            Assert.That(back.Opening.Shots, Is.EqualTo(shots));
            Assert.That(Director.GroupAlive(back, back.Opening.Group), Is.EqualTo(st.Opening.Count),
                "the bodies come back with it");

            OpeningFixture.Kill(back, back.Opening.Group);
            OpeningFixture.Run(ctx, back, 1);
            Assert.That(back.Opening.Status, Is.EqualTo(OpeningStatus.Repelled));
            Assert.That(RaidFixture.Last<OpeningEndedEvent>(back).Shots, Is.EqualTo(shots));
        }

        [Test]
        public void ADeferredEncounterResumesDeferredAndIsNotCountedTwice()
        {
            var ctx = OpeningFixture.Context();
            var st = OpeningFixture.State(ctx);
            OpeningFixture.FakeMajor(st);
            OpeningFixture.ReadyTurret(ctx, st);
            OpeningFixture.Run(ctx, st, 1);
            Assert.That(st.Opening.Status, Is.EqualTo(OpeningStatus.Deferred));

            var back = OpeningFixture.RoundTrip(ctx, st);
            Assert.That(back.Opening.Status, Is.EqualTo(OpeningStatus.Deferred));
            Assert.That(back.Opening.DeferCount, Is.EqualTo(1));

            OpeningFixture.Seconds(ctx, back, 5);
            Assert.That(back.Opening.DeferCount, Is.EqualTo(1), "a load is not a second deferral");
            Assert.That(RaidFixture.Count<OpeningDeferredEvent>(back), Is.EqualTo(0));

            OpeningFixture.ClearMajor(back);
            OpeningFixture.Seconds(ctx, back, 1);
            Assert.That(back.Opening.Status, Is.EqualTo(OpeningStatus.Scheduled));
        }

        [Test]
        public void AnUpgradedSaveCarriesTheUndecidedOpeningAndIsDecidedOnce()
        {
            // This is the shape a Phase B save takes on its way in: SaveUpgrade fills the missing "opening" member
            // from a fresh state, so the loaded campaign arrives undecided and the phase applies the fresh rule.
            var ctx = OpeningFixture.Context();
            var st = OpeningFixture.State(ctx);
            st.Opening = new OpeningState();
            st.T = 600;
            st.Director.NextStart = st.T + 5000;

            var back = OpeningFixture.RoundTrip(ctx, st);
            Assert.That(back.Opening, Is.Not.Null);
            Assert.That(back.Opening.Status, Is.EqualTo(OpeningStatus.None), "nothing has been decided yet");

            OpeningFixture.Run(ctx, back, 1);
            Assert.That(back.Opening.Status, Is.EqualTo(OpeningStatus.Pending),
                "a save with nothing built is still at the start of the opening");

            // And a second load of the decided save changes nothing.
            var again = OpeningFixture.RoundTrip(ctx, back);
            OpeningFixture.Run(ctx, again, 1);
            Assert.That(again.Opening.Status, Is.EqualTo(OpeningStatus.Pending));
        }
    }
}
