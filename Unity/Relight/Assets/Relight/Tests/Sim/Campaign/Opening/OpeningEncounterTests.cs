using System.Collections.Generic;
using NUnit.Framework;
using Relight.Sim.Tests.Combat;

namespace Relight.Sim.Tests.Campaign
{
    /// <summary>
    /// C-09's state machine: reference openingEncounter.ts <c>initOpeningEncounter</c> and campaignThreat.ts:204
    /// <c>tickOpeningEncounter</c>, with the two port rules the reference does not have — the U-D-26 deferral and
    /// its U-Q-21 cap.
    /// </summary>
    [TestFixture]
    public sealed class OpeningEncounterTests
    {
        // ------------------------------------------------------------------ init

        [Test]
        public void AFreshCampaignWaitsForTheFirstFullTurret()
        {
            var ctx = OpeningFixture.Context();
            var st = OpeningFixture.State(ctx);
            Assert.That(st.Opening.Status, Is.EqualTo(OpeningStatus.Pending));
        }

        [Test]
        public void AProgressedSaveWithThreeTurretsNeverReceivesTheEncounter()
        {
            var ctx = OpeningFixture.Context();
            var st = RaidFixture.State(ctx);
            HomeCore.Ensure(ctx, st);
            st.T = 900;                               // not t == 0, so the "fresh" rule has to look at the facts
            st.Director.NextStart = st.T + 5000;
            RaidFixture.Turret(ctx, st, RaidFixture.CoreX + 6, RaidFixture.CoreY);
            RaidFixture.Turret(ctx, st, RaidFixture.CoreX - 6, RaidFixture.CoreY);
            RaidFixture.Turret(ctx, st, RaidFixture.CoreX, RaidFixture.CoreY + 6);

            new OpeningInitializer().Init(ctx, st);
            Assert.That(st.Opening.Status, Is.EqualTo(OpeningStatus.Skipped));
        }

        [Test]
        public void AnUpgradedSaveIsDecidedOnItsFirstTick()
        {
            var ctx = OpeningFixture.Context();
            var st = RaidFixture.State(ctx);
            HomeCore.Ensure(ctx, st);
            st.T = 900;
            st.Director.NextStart = st.T + 5000;
            st.Director.RaidsStarted = 4;             // a save that has already fought raids
            Assert.That(st.Opening.Status, Is.EqualTo(OpeningStatus.None), "an upgraded save arrives undecided");

            OpeningFixture.Run(ctx, st, 1);
            Assert.That(st.Opening.Status, Is.EqualTo(OpeningStatus.Skipped));
        }

        // ------------------------------------------------------------------ pending -> scheduled -> active

        [Test]
        public void TheFirstReadyTurretSchedulesTheEncounterExactlyOnce()
        {
            var ctx = OpeningFixture.Context();
            var st = OpeningFixture.State(ctx);

            OpeningFixture.Seconds(ctx, st, 2);
            Assert.That(st.Opening.Status, Is.EqualTo(OpeningStatus.Pending), "an empty base announces nothing");

            var turret = OpeningFixture.ReadyTurret(ctx, st);
            OpeningFixture.Run(ctx, st, 1);

            Assert.That(st.Opening.Status, Is.EqualTo(OpeningStatus.Scheduled));
            Assert.That(st.Opening.TurretId, Is.EqualTo(turret.Id));
            Assert.That(st.Opening.StartsAt, Is.EqualTo(st.T + OpeningRules.Tuning(ctx.Data).WarningS).Within(0.2));
            Assert.That(st.Director.Reserved, Is.True, "nothing else may start while the encounter is announced");

            // A second ready turret changes nothing: the encounter is scheduled once (reference `op.id` set once).
            RaidFixture.Turret(ctx, st, RaidFixture.CoreX - 6, RaidFixture.CoreY);
            OpeningFixture.Seconds(ctx, st, 2);
            Assert.That(RaidFixture.Count<OpeningScheduledEvent>(st), Is.EqualTo(1));
            Assert.That(st.Opening.TurretId, Is.EqualTo(turret.Id));
        }

        [Test]
        public void TheGroupArrivesAtStartsAt()
        {
            var ctx = OpeningFixture.Context();
            var st = OpeningFixture.State(ctx);
            OpeningFixture.ReadyTurret(ctx, st);
            OpeningFixture.Run(ctx, st, 1);
            var startsAt = st.Opening.StartsAt;

            OpeningFixture.Seconds(ctx, st, OpeningRules.Tuning(ctx.Data).WarningS - 1);
            Assert.That(st.Opening.Status, Is.EqualTo(OpeningStatus.Scheduled), "it waits out the whole warning");

            OpeningFixture.Seconds(ctx, st, 2);
            Assert.That(st.Opening.Status, Is.EqualTo(OpeningStatus.Active));
            Assert.That(st.T, Is.GreaterThanOrEqualTo(startsAt));
            Assert.That(st.Opening.Group, Is.GreaterThan(0));
            Assert.That(st.Opening.Count, Is.GreaterThan(0), "count becomes the number actually born");
            Assert.That(Director.GroupAlive(st, st.Opening.Group), Is.EqualTo(st.Opening.Count));
            Assert.That(RaidFixture.Count<OpeningStartedEvent>(st), Is.EqualTo(1));
        }

        // ------------------------------------------------------------------ active -> repelled / lost

        [Test]
        public void ALiveTurretAtTheEndMeansRepelled()
        {
            var ctx = OpeningFixture.Context();
            var st = OpeningFixture.State(ctx);
            OpeningFixture.ToActive(ctx, st);

            OpeningFixture.Kill(st, st.Opening.Group);
            OpeningFixture.Run(ctx, st, 1);

            Assert.That(st.Opening.Status, Is.EqualTo(OpeningStatus.Repelled));
            Assert.That(st.Opening.EndedAt, Is.GreaterThan(0));
            Assert.That(st.Director.Reserved, Is.False, "the hold on ordinary raids is released");
            Assert.That(st.Director.RecoveryUntil, Is.GreaterThan(st.T), "recovery is pushed by recoveryS");
            var ended = RaidFixture.Last<OpeningEndedEvent>(st);
            Assert.That(ended, Is.Not.Null);
            Assert.That(ended.Repelled, Is.True);
        }

        [Test]
        public void ADestroyedTurretAtTheEndMeansLost()
        {
            var ctx = OpeningFixture.Context();
            var st = OpeningFixture.State(ctx);
            var turret = OpeningFixture.ToActive(ctx, st);

            TurretRules.Damage(ctx, st, turret, 1e6);
            OpeningFixture.Kill(st, st.Opening.Group);
            OpeningFixture.Run(ctx, st, 1);

            Assert.That(st.Opening.Status, Is.EqualTo(OpeningStatus.Lost));
            Assert.That(RaidFixture.Last<OpeningEndedEvent>(st).Repelled, Is.False);
        }

        [Test]
        public void AGroupThatWillNotDieIsWithdrawnThenPurged()
        {
            var ctx = OpeningFixture.Context();
            var st = OpeningFixture.State(ctx);
            OpeningFixture.ToActive(ctx, st);
            var t = OpeningRules.Tuning(ctx.Data);
            var group = st.Opening.Group;

            st.T = st.Opening.StartsAt + t.MaxDurationS + 1;
            OpeningFixture.Run(ctx, st, 1);
            Assert.That(st.Director.Minor.Retreat, Is.True, "it is asked to withdraw at maxDuration");
            Assert.That(st.Opening.Status, Is.EqualTo(OpeningStatus.Active));

            st.T = st.Opening.StartsAt + 2 * t.MaxDurationS + 1;
            OpeningFixture.Run(ctx, st, 1);
            Assert.That(Director.GroupAlive(st, group), Is.EqualTo(0), "and removed at twice the duration");
            Assert.That(st.Opening.Status, Is.EqualTo(OpeningStatus.Repelled));
        }

        // ------------------------------------------------------------------ shots

        [Test]
        public void ShotsAreCountedFromTheTurretsOwnEvents()
        {
            var ctx = OpeningFixture.Context();
            var st = OpeningFixture.State(ctx);
            var turret = OpeningFixture.ToActive(ctx, st);

            st.Events.Add(new TurretShotEvent(st.T, turret.Id, 0, 1, 1, true));
            st.Events.Add(new TurretShotEvent(st.T, turret.Id, 0, 1, 1, false));
            st.Events.Add(new TurretShotEvent(st.T, turret.Id + 999, 0, 1, 1, true));   // another turret's round
            var before = st.Opening.Shots;
            OpeningFixture.Run(ctx, st, 1);

            Assert.That(st.Opening.Shots - before, Is.EqualTo(2));
        }

        // ------------------------------------------------------------------ U-D-26 deferral

        [Test]
        public void ALiveRaidDefersTheEncounterAndItResumesExactlyOnce()
        {
            var ctx = OpeningFixture.Context();
            var st = OpeningFixture.State(ctx);
            var nextStart = st.Director.NextStart;

            OpeningFixture.FakeMajor(st);
            OpeningFixture.ReadyTurret(ctx, st);
            OpeningFixture.Run(ctx, st, 1);

            Assert.That(st.Opening.Status, Is.EqualTo(OpeningStatus.Deferred), "a live raid defers, never cancels");
            Assert.That(st.Opening.DeferCount, Is.EqualTo(1));
            Assert.That(st.Opening.DeferredUntil, Is.GreaterThan(st.T), "the card can say when it will come");
            Assert.That(RaidFixture.Count<OpeningDeferredEvent>(st), Is.EqualTo(1));

            // Ordinary raids keep their own cadence meanwhile (U-D-38: no accumulated debt).
            OpeningFixture.Seconds(ctx, st, 30);
            Assert.That(st.Director.NextStart, Is.EqualTo(nextStart).Within(1e-9));
            Assert.That(st.Opening.Status, Is.EqualTo(OpeningStatus.Deferred));
            Assert.That(st.Opening.DeferCount, Is.EqualTo(1), "one deferral, however long it lasts");

            OpeningFixture.ClearMajor(st);
            OpeningFixture.Seconds(ctx, st, 1);

            Assert.That(st.Opening.Status, Is.EqualTo(OpeningStatus.Scheduled));
            Assert.That(RaidFixture.Count<OpeningScheduledEvent>(st), Is.EqualTo(1));
            Assert.That(RaidFixture.Count<OpeningDeferredEvent>(st), Is.EqualTo(1), "and it is not deferred again");
        }

        [Test]
        public void TheDeferredEncounterStillRunsAfterTheRaidIsOver()
        {
            var ctx = OpeningFixture.Context();
            var st = OpeningFixture.State(ctx);
            OpeningFixture.FakeMajor(st);
            OpeningFixture.ReadyTurret(ctx, st);
            OpeningFixture.Run(ctx, st, 1);
            OpeningFixture.ClearMajor(st);

            OpeningFixture.Seconds(ctx, st, OpeningRules.Tuning(ctx.Data).WarningS + 2);
            Assert.That(st.Opening.Status, Is.EqualTo(OpeningStatus.Active));
            OpeningFixture.Kill(st, st.Opening.Group);
            OpeningFixture.Run(ctx, st, 1);
            Assert.That(st.Opening.Status, Is.EqualTo(OpeningStatus.Repelled));
        }

        // ------------------------------------------------------------------ U-Q-21 cap

        [Test]
        public void ThreeLoadedTurretsDropTheDeferredEncounterWithoutAReward()
        {
            var ctx = OpeningFixture.Context();
            var st = OpeningFixture.State(ctx);
            OpeningFixture.FakeMajor(st);
            OpeningFixture.ReadyTurret(ctx, st);
            OpeningFixture.Run(ctx, st, 1);
            Assert.That(st.Opening.Status, Is.EqualTo(OpeningStatus.Deferred));

            RaidFixture.Turret(ctx, st, RaidFixture.CoreX - 6, RaidFixture.CoreY);
            RaidFixture.Turret(ctx, st, RaidFixture.CoreX, RaidFixture.CoreY + 6);
            OpeningFixture.Seconds(ctx, st, 1);

            Assert.That(st.Opening.Status, Is.EqualTo(OpeningStatus.Skipped), "dropped at the U-Q-21 cap");
            Assert.That(st.Opening.Shots, Is.EqualTo(0), "and it pays nothing: there was no encounter");
            Assert.That(RaidFixture.Count<OpeningStartedEvent>(st), Is.EqualTo(0));

            // It never comes back once dropped, even after the raid ends.
            OpeningFixture.ClearMajor(st);
            OpeningFixture.Seconds(ctx, st, 5);
            Assert.That(st.Opening.Status, Is.EqualTo(OpeningStatus.Skipped));
        }

        [Test]
        public void LeavingForTheFirstCampAlsoDropsIt()
        {
            var ctx = new SimContext(ReferenceData.Create(), RaidFixture.Map(), null, null,
                new WorldSites(new List<SiteRecord>
                {
                    new SiteRecord("home", "Home Court", SiteKind.Core, RaidFixture.CoreX, RaidFixture.CoreY, 8, 8),
                    new SiteRecord("camp", "Freight camp", SiteKind.Camp, 140, 140, 4, 4, null, 3),
                }));
            var st = OpeningFixture.State(ctx);
            OpeningFixture.FakeMajor(st);
            OpeningFixture.ReadyTurret(ctx, st);
            OpeningFixture.Run(ctx, st, 1);
            Assert.That(st.Opening.Status, Is.EqualTo(OpeningStatus.Deferred));

            st.Engineer.Pos = new Vec2(138, 138);     // closer to the camp than to Home
            OpeningFixture.Run(ctx, st, 1);
            Assert.That(st.Opening.Status, Is.EqualTo(OpeningStatus.Skipped));
        }

        // ------------------------------------------------------------------ automated resupply

        [Test]
        public void OnlyTheAutomatedFeedPathRecordsTheSupplyChain()
        {
            var ctx = OpeningFixture.Context();
            var st = OpeningFixture.State(ctx);
            var turret = OpeningFixture.ReadyTurret(ctx, st);
            turret.Rounds = 10;

            // Hand loading moves rounds but counts HandFedMags, never TurretFed — so nothing is recorded.
            st.Stats.HandFedMags += 10;
            turret.Rounds += 10;
            OpeningFixture.Run(ctx, st, 1);
            Assert.That(st.Opening.SuppliedAt, Is.LessThan(0), "hand loading never records a supply chain");

            // An assembler that has produced, with a belt into the turret, and the automated counter moving.
            var asm = RaidFixture.Add(ctx, st, "assembler", turret.X - 3, turret.Y);
            st.Production.Of(asm.Id).Recipe = "bullet-batch";
            asm.Inv.Add(ItemId.Magazine, 1);   // finished output waits in the inventory; Machine.Out is retired
            RaidFixture.Add(ctx, st, "belt", turret.X - 1, turret.Y, Dir.E);
            st.Rev++;
            st.Stats.TurretFed += 1;
            OpeningFixture.Run(ctx, st, 1);

            Assert.That(st.Opening.SuppliedAt, Is.GreaterThanOrEqualTo(0));
            Assert.That(RaidFixture.Count<ResupplyWorkingEvent>(st), Is.EqualTo(1));
        }
    }
}
