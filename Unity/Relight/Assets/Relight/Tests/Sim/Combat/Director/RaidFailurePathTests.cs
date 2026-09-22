using System.Collections.Generic;
using NUnit.Framework;
using Relight.Sim.Tests.Persistence;

namespace Relight.Sim.Tests.Combat
{
    /// <summary>
    /// E-18 (U-D-64 d): the raid FAILURE path. A large raid that beats its target, or cannot finish, ends; its
    /// survivors walk off; the fallen core can be repaired; the next raid waits a full interval.
    ///
    /// Before E-18 a large raid had one ending — every body dead — and a fallen core was still a target, so a raid
    /// that WON stood on the wreck for ever, blocked the repair from anywhere on the map, and turned every later
    /// opportunity into "Previous assault cleanup is still unresolved." (ENM-01 to ENM-03).
    ///
    /// These run on the flat 160×160 fixture map. They pin the RULES; they are not the acceptance evidence, which
    /// has to come from the real city (<c>Tests/Editor/RealCityRaidTests.cs</c>).
    /// </summary>
    public sealed class RaidFailurePathTests
    {
        private static List<ITickPhase> Clock() => new List<ITickPhase> { new DirectorPhase() };

        private static SimState State(SimContext ctx, int seed = 7)
        {
            var st = RaidFixture.State(ctx, seed);
            new HomeCoreInitializer().Init(ctx, st);
            st.Director.NextMinor = 1e9;      // these tests are about the large raid; no small one is due
            return st;
        }

        /// <summary>Warn, commit and start a real large raid, and return it with its first bodies on the map.</summary>
        private static MajorRaid Assault(SimContext ctx, SimState st)
        {
            var d = st.Director;
            st.T = d.NextStart - ctx.Data.Raids.WarningS;
            RaidFixture.Run(ctx, st, 1, Clock());
            Assert.That(d.Major, Is.Not.Null, "the fixture map must be able to stage an assault");
            st.T = d.Major.StartsAt;
            RaidFixture.Run(ctx, st, 40, Clock());
            Assert.That(d.Major.Committed, Is.True);
            Assert.That(EnemyQueries.GroupAlive(st, d.Major.Id), Is.GreaterThan(0), "bodies are on the map");
            return d.Major;
        }

        private static int Unresolved(SimState st)
        {
            var n = 0;
            foreach (var e in st.Events)
                if (e is RaidNoticeEvent r && r.Text.Contains("Previous assault cleanup is still unresolved")) n++;
            return n;
        }

        // ---------------------------------------------------------------- the target

        [Test]
        public void AFallenCoreIsNotATarget_ButARegionWhoseCoreWasNeverPlacedStillFallsBackToTheSite()
        {
            var ctx = RaidFixture.Context();

            var unplaced = RaidFixture.State(ctx);
            Assert.That(EnemyCoreHook.Down(ctx, unplaced), Is.False);
            Assert.That(DirectorRules.Target(ctx, unplaced, out var x, out var y, out _), Is.True, "the authored site");
            Assert.That((x, y), Is.EqualTo((RaidFixture.CoreX, RaidFixture.CoreY)));

            var st = State(ctx);
            Assert.That(DirectorRules.Target(ctx, st, out _, out _, out _), Is.True);
            HomeCore.Damage(st, 1e9);
            Assert.That(EnemyCoreHook.Down(ctx, st), Is.True);
            Assert.That(DirectorRules.Target(ctx, st, out _, out _, out _), Is.False,
                "the authored site must not stand in for a core that has fallen");
        }

        // ---------------------------------------------------------------- the ending

        [Test]
        public void ALostAssaultEndsAtOnce_IsRecordedAsLost_AndTheNextOneWaitsAFullInterval()
        {
            var ctx = RaidFixture.Context();
            var st = State(ctx);
            var d = st.Director;
            var a = Assault(ctx, st);
            var owed = a.Remaining;
            Assert.That(owed, Is.GreaterThan(0), "most of the assault has not arrived yet");

            HomeCore.Damage(st, 1e9);
            RaidFixture.Run(ctx, st, 1, Clock());

            Assert.That(d.Major, Is.Null, "the assault is over although its bodies are alive");
            Assert.That(EnemyQueries.GroupAlive(st, a.Id), Is.GreaterThan(0));
            Assert.That(d.History, Has.Count.EqualTo(1));
            var rec = d.History[0];
            Assert.That(rec.Id, Is.EqualTo(a.Id));
            Assert.That(rec.Outcome, Is.EqualTo((int)RaidOutcome.Lost));
            Assert.That(a.Cancelled, Is.EqualTo(owed), "what it still owed is written off, not carried");
            Assert.That(d.NextStart, Is.GreaterThanOrEqualTo(st.T + ctx.Data.Raids.IntervalMinS - 1),
                "a full interval from the END, not from the start");
            Assert.That(d.RecoveryUntil, Is.GreaterThanOrEqualTo(st.T + ctx.Data.Raids.RecoveryS - 1));
            Assert.That(d.Notice, Does.Contain("withdrawing"));
        }

        [Test]
        public void TheSurvivorsWalkOff_TheCoreCanThenBeRepaired_AndTheNextAssaultIsScheduledWithoutASkip()
        {
            var ctx = RaidFixture.Context();
            var st = State(ctx);
            var d = st.Director;
            var a = Assault(ctx, st);
            HomeCore.Damage(st, 1e9);

            // The whole combat stack now, so the bodies really walk. Nobody shoots them.
            st.Engineer.Pos = new Vec2(RaidFixture.CoreX + 4, RaidFixture.CoreY + 4);
            var limit = (int)((ctx.Data.Siege.WithdrawPurgeS + 5) / RaidFixture.Dt);
            var ticks = 0;
            while (EnemyQueries.GroupAlive(st, a.Id) > 0 && ticks++ < limit) RaidFixture.Run(ctx, st, 1);
            Assert.That(EnemyQueries.GroupAlive(st, a.Id), Is.EqualTo(0), "every survivor has left the map");
            Assert.That(d.Major, Is.Null);

            Assert.That(Home.AttackersNearby(ctx, st), Is.False);
            st.Engineer.Inv[ItemId.Steel] = 50;
            st.Engineer.Inv[ItemId.Copper] = 50;
            Assert.That(new HomeCoreHandler().TryApply(ctx, st, new RepairCommand(RepairKinds.Core, 0), out var r), Is.True);
            Assert.That(r.Problem, Is.Empty, "the fallen core can be repaired once the raid has ended");

            // Put the core back and let the clock reach the next assault.
            st.Home.Hp = ctx.Data.Defence.CoreHp;
            st.T = d.NextStart - ctx.Data.Raids.WarningS;
            RaidFixture.Run(ctx, st, 1, Clock());
            Assert.That(d.Major, Is.Not.Null, "the director schedules the next assault");
            Assert.That(d.Major.Id, Is.Not.EqualTo(a.Id));
            Assert.That(Unresolved(st), Is.EqualTo(0));
        }

        [Test]
        public void AnAssaultStillAliveLongAfterItsPlannedEndIsBrokenOff_ButAHandBuiltRaidWithNoPlanNeverIs()
        {
            var ctx = RaidFixture.Context();
            var st = State(ctx);
            var d = st.Director;
            var e = RaidFixture.Body(st, "skitter", 30.5, 30.5, EnemyLayer.Major, 5);
            e.Hp = 1e9;
            st.T = 5000;
            d.NextStart = st.T + 4000;    // nothing is due while the hand-built raid stands
            RaidFixture.Run(ctx, st, 20, Clock());
            Assert.That(d.Major, Is.Not.Null, "EndsAt 0 means no plan: nothing to overrun");

            d.Major.EndsAt = st.T - ctx.Data.Siege.MajorOverrunS + 1;
            RaidFixture.Run(ctx, st, 1, Clock());
            Assert.That(d.Major, Is.Not.Null, "not yet");

            st.T += 2;
            d.NextStart = st.T + 10;      // due almost at once: the ending must push it out
            RaidFixture.Run(ctx, st, 1, Clock());
            Assert.That(d.Major, Is.Null);
            Assert.That(d.History[d.History.Count - 1].Outcome, Is.EqualTo((int)RaidOutcome.BrokeOff));
            Assert.That(d.NextStart, Is.GreaterThanOrEqualTo(st.T + ctx.Data.Raids.IntervalMinS - 1));
            Assert.That(Unresolved(st), Is.EqualTo(0));
        }

        [Test]
        public void AClearedAssaultIsUnchanged()
        {
            var ctx = RaidFixture.Context();
            var st = State(ctx);
            var d = st.Director;
            RaidFixture.Body(st, "skitter", 30.5, 30.5, EnemyLayer.Major, 5);
            var next = d.NextStart;
            var serial = d.Serial;
            st.Enemies.Actors.Clear();
            RaidFixture.Run(ctx, st, 1, Clock());
            Assert.That(d.Major, Is.Null);
            Assert.That(d.History[0].Outcome, Is.EqualTo((int)RaidOutcome.Cleared));
            Assert.That(d.NextStart, Is.EqualTo(next), "a cleared assault does not move the schedule");
            Assert.That(d.Serial, Is.EqualTo(serial));
        }

        // ---------------------------------------------------------------- stragglers

        [Test]
        public void ASurvivorThatNeverLeavesIsRemovedAfterThePurgeTime_WithoutAKill()
        {
            var ctx = RaidFixture.Context();
            var st = State(ctx);
            var d = st.Director;
            var a = Assault(ctx, st);
            HomeCore.Damage(st, 1e9);
            RaidFixture.Run(ctx, st, 1, Clock());
            var alive = EnemyQueries.GroupAlive(st, a.Id);
            Assert.That(alive, Is.GreaterThan(0));

            // Only the director runs, so the bodies never move: they are stuck for the purpose of this test.
            RaidFixture.Run(ctx, st, (int)((ctx.Data.Siege.WithdrawPurgeS - 5) / RaidFixture.Dt), Clock());
            Assert.That(EnemyQueries.GroupAlive(st, a.Id), Is.EqualTo(alive), "not before the purge time");
            RaidFixture.Run(ctx, st, (int)(10 / RaidFixture.Dt), Clock());
            Assert.That(EnemyQueries.GroupAlive(st, a.Id), Is.EqualTo(0));
            Assert.That(RaidFixture.Count<EnemyKilledEvent>(st), Is.EqualTo(0), "removed, not killed");
        }

        [Test]
        public void ASurvivorOfAnEarlierAssaultKeepsLeaving_ItDoesNotJoinTheNextOne()
        {
            var ctx = RaidFixture.Context();
            var st = State(ctx);
            var old = RaidFixture.Body(st, "skitter", 30.5, 30.5, EnemyLayer.Major, 5);
            st.Director.Major = new MajorRaid { Id = 9, Origin = old.Origin, Committed = true };
            RaidFixture.Run(ctx, st, 1, new List<ITickPhase> { new EnemyPhase() });
            var still = st.Enemies.Actors.Find(e => e.Id == old.Id);
            Assert.That(still == null || still.Withdrawing, Is.True, "it left, or is on its way out");
        }

        // ---------------------------------------------------------------- the small raid

        [Test]
        public void ASmallRaidThatBeatsTheCoreIsCalledOffForGood_EvenIfTheCoreIsRepairedWhileItLeaves()
        {
            var ctx = RaidFixture.Context();
            var st = State(ctx);
            var e = RaidFixture.Body(st, "skitter", 30.5, 30.5);
            e.Hp = 1e9;
            HomeCore.Damage(st, 1e9);
            RaidFixture.Run(ctx, st, 1, Clock());
            Assert.That(st.Director.Minor, Is.Not.Null);
            Assert.That(st.Director.Minor.Retreat, Is.True);

            st.Home.Hp = ctx.Data.Defence.CoreHp;
            RaidFixture.Run(ctx, st, 5);
            Assert.That(st.Director.Minor == null || st.Director.Minor.Retreat, Is.True, "it does not turn round");
        }

        // ---------------------------------------------------------------- repair

        [Test]
        public void OnlyARaidBodyNearTheCoreBlocksItsRepair()
        {
            var ctx = RaidFixture.Context();
            var st = State(ctx);
            var (cx, cy, cw, _) = HomeQueries.CoreRect(st);
            var reach = ctx.Data.Siege.CoreThreatTiles;

            var e = RaidFixture.Body(st, "skitter", cx + cw + reach + 1.5, cy + 0.5, EnemyLayer.Major, 5);
            Assert.That(Home.AttackersNearby(ctx, st), Is.False, "just outside the ring");
            e.Pos = new Vec2(cx + cw + reach - 0.5, cy + 0.5);
            Assert.That(Home.AttackersNearby(ctx, st), Is.True, "just inside it");

            e.Layer = EnemyLayer.Site;
            Assert.That(Home.AttackersNearby(ctx, st), Is.False, "a site guard never blocked the repair");
        }

        // ---------------------------------------------------------------- growth (INTERIM — F1-30)

        [Test]
        public void OnlyAClearedAssaultCountsTowardsGrowth()
        {
            var ctx = RaidFixture.Context();
            var st = State(ctx);
            var h = st.Director.History;
            h.Add(new RaidRecord { Id = 1, Outcome = (int)RaidOutcome.Cleared });
            h.Add(new RaidRecord { Id = 2, Outcome = (int)RaidOutcome.Lost });
            h.Add(new RaidRecord { Id = 3, Outcome = (int)RaidOutcome.BrokeOff });
            Assert.That(DirectorPhase.Survived(st), Is.EqualTo(1));
        }

        // ---------------------------------------------------------------- saves

        [Test]
        public void TheOutcomeIsSaved_AndAVersionNineFileReadsEveryEarlierAssaultAsCleared()
        {
            var ctx = RaidFixture.Context();
            var st = State(ctx);
            st.Director.History.Add(new RaidRecord { Id = 4, Started = 10, Ended = 90, Spawned = 12, Outcome = (int)RaidOutcome.Lost });
            st.Director.History.Add(new RaidRecord { Id = 6, Started = 900, Ended = 990, Spawned = 20, Outcome = (int)RaidOutcome.BrokeOff });

            var load = SaveSerializer.ReadText(SaveSerializer.WriteText(st, ctx.Data), ctx.Data);
            Assert.That(load.Ok, Is.True, load.Reason);
            Assert.That(load.State.Director.History[0].Outcome, Is.EqualTo((int)RaidOutcome.Lost));
            Assert.That(load.State.Director.History[1].Outcome, Is.EqualTo((int)RaidOutcome.BrokeOff));

            // Forge the version-9 shape: the same records with no "outcome" member at all.
            var state = PersistenceFixture.Canonical(st);
            var state9 = System.Text.RegularExpressions.Regex.Replace(state, "\"outcome\":[0-9]+,|,\"outcome\":[0-9]+", "");
            Assert.That(state9, Does.Not.Contain("\"outcome\""));
            state9 = PersistenceFixture.Retarget(state9, "version", "9");
            var doc = SaveSerializer.WriteText(st, ctx.Data);
            doc = PersistenceFixture.Retarget(doc, "state", state9);
            doc = PersistenceFixture.Retarget(doc, "version", "9");
            doc = PersistenceFixture.Retarget(doc, "hash", CanonicalJsonWriter.QuoteString(StateHash.Of(state9)));

            var old = SaveSerializer.ReadText(doc, ctx.Data);
            Assert.That(old.Ok, Is.True, old.Reason);
            Assert.That(old.Header.Version, Is.EqualTo(9));
            Assert.That(old.Upgraded, Does.Contain("version 9"));
            Assert.That(old.Upgraded, Does.Contain("recorded as cleared"));
            Assert.That(old.State.Director.History, Has.Count.EqualTo(2));
            Assert.That(old.State.Director.History[0].Outcome, Is.EqualTo((int)RaidOutcome.Cleared));
            Assert.That(old.State.Director.History[1].Ended, Is.EqualTo(990));
        }

        /// <summary>
        /// INT-04c removed the record's dead <c>defeated</c> flag (save v11). A version-10 file still carries the
        /// member on every finished assault. It must load, with each record's outcome exactly as the file has it:
        /// the reader re-hashes what it built, so a member merely ignored would read as a damaged file.
        /// </summary>
        [Test]
        public void AVersionTenFileThatStillCarriesTheOldDefeatedFlagLoads_WithItsOutcomesUntouched()
        {
            var ctx = RaidFixture.Context();
            var st = State(ctx);
            st.Director.History.Add(new RaidRecord { Id = 4, Started = 10, Ended = 90, Spawned = 12, Outcome = (int)RaidOutcome.Lost });

            var state = PersistenceFixture.Canonical(st);
            Assert.That(state, Does.Not.Contain("\"defeated\""), "this build no longer writes it");
            var older = state.Replace("\"ended\":90", "\"defeated\":true,\"ended\":90");
            Assert.That(older, Does.Contain("\"defeated\":true"));
            older = PersistenceFixture.Retarget(older, "version", "10");
            var doc = SaveSerializer.WriteText(st, ctx.Data);
            doc = PersistenceFixture.Retarget(doc, "state", older);
            doc = PersistenceFixture.Retarget(doc, "version", "10");
            doc = PersistenceFixture.Retarget(doc, "hash", CanonicalJsonWriter.QuoteString(StateHash.Of(older)));

            var load = SaveSerializer.ReadText(doc, ctx.Data);
            Assert.That(load.Ok, Is.True, load.Reason);
            Assert.That(load.Header.Version, Is.EqualTo(10));
            Assert.That(load.Upgraded, Does.Contain("version 10"));
            Assert.That(load.State.Director.History, Has.Count.EqualTo(1));
            Assert.That(load.State.Director.History[0].Outcome, Is.EqualTo((int)RaidOutcome.Lost));
            Assert.That(load.State.Director.History[0].Ended, Is.EqualTo(90));
        }
    }
}
