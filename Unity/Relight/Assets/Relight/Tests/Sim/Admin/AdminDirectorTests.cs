using System.Collections.Generic;
using NUnit.Framework;
using Relight.Sim.Tests.Combat;

namespace Relight.Sim.Tests.Admin
{
    /// <summary>
    /// E-19 part 1 (U-D-66 (8)): the Admin director page. A developer can start a REAL large raid on demand, skip
    /// the wait, pin the number the next raid is sized by, and read the director's state.
    ///
    /// The rule these pin: the tools move the director's CLOCK and nothing else. The raid is still booked, warned,
    /// committed and spawned wave by wave by <see cref="DirectorPhase"/>, so what is watched is what a player gets —
    /// and the rules the director tests for (a reserved approach, a fallen core, a raid already under way) still
    /// refuse, with the reason.
    /// </summary>
    public sealed class AdminDirectorTests
    {
        private SimContext ctx;
        private SimState st;

        [SetUp]
        public void Setup()
        {
            ctx = RaidFixture.Context();
            st = RaidFixture.State(ctx);
            new HomeCoreInitializer().Init(ctx, st);
            st.Director.NextMinor = 1e9;          // these are about the large raid
            RaidFixture.Run(ctx, st, 1);          // the director seeds its clock on the first tick
        }

        private CommandResult Run(AdminCommand c) { new AdminHandler().TryApply(ctx, st, c, out var r); return r; }

        private List<RaidNoticeEvent> Notices(RaidNoticeKind kind)
        {
            var list = new List<RaidNoticeEvent>();
            foreach (var e in st.Events) if (e is RaidNoticeEvent n && n.Kind == kind) list.Add(n);
            return list;
        }

        private void RunTo(double t) { while (st.T < t) RaidFixture.Run(ctx, st, 1); }

        // ---------------------------------------------------------------- start a large raid now

        [Test]
        public void StartNow_BooksARealLargeRaidWithinOneTick_WithTheNormalWarning_ThenItsWaves()
        {
            var d = st.Director;
            var r = ctx.Data.Raids;
            Assert.That(d.NextStart - st.T, Is.GreaterThan(r.WarningS * 2), "the ordinary first raid is a long way off");
            var t0 = st.T;

            var result = Run(new AdminCommand("major-now"));
            Assert.That(result.Accepted, Is.True, result.Problem);
            Assert.That(d.Major, Is.Null, "the command only moves the clock; the director books the raid itself");

            RaidFixture.Run(ctx, st, 1);
            Assert.That(d.Major, Is.Not.Null, "booked within one tick");
            Assert.That(d.Major.Committed, Is.False);
            Assert.That(d.Major.StartsAt, Is.EqualTo(t0 + r.WarningS).Within(1e-3), "the full, normal warning");
            Assert.That(d.Major.Total, Is.EqualTo(SiegePlan.TotalFor(ctx, 0)), "the ordinary first-raid plan");
            Assert.That(d.Major.Waves, Is.EqualTo(ctx.Data.Siege.MajorWaves));
            var said = Notices(RaidNoticeKind.Announced);
            Assert.That(said.Count, Is.EqualTo(1));
            Assert.That(said[0].Text, Does.StartWith("Major assault inbound from").And.Contain("in " + (int)r.WarningS + " s"));
            Assert.That(EnemyQueries.GroupAlive(st, d.Major.Id), Is.Zero, "nothing arrives during the warning");

            // …then the waves, on the plan's own seconds.
            var plan = d.Major;
            RunTo(plan.StartsAt + 2);
            Assert.That(plan.Committed, Is.True);
            Assert.That(EnemyQueries.GroupAlive(st, plan.Id), Is.GreaterThan(0), "the first wave is on the map");
            var first = d.MajorSpawned;
            Assert.That(first, Is.LessThanOrEqualTo(plan.WaveCount[0]), "and only the first wave");
            RunTo(plan.WaveStart[1] + ctx.Data.Siege.MajorWaveGapS / 2);
            Assert.That(d.MajorSpawned, Is.GreaterThan(plan.WaveCount[0]), "the second wave followed on its own second");

            // The clock carries on normally afterwards: the next raid is a full interval after this one began.
            Assert.That(d.NextStart, Is.GreaterThanOrEqualTo(plan.StartsAt + r.IntervalMinS));
        }

        [Test]
        public void StartNow_CutsARecoverySpellShort_BecauseThatIsTheClockToo()
        {
            var d = st.Director;
            d.RecoveryUntil = st.T + 10000;
            Assert.That(Run(new AdminCommand("major-now")).Accepted, Is.True);
            RaidFixture.Run(ctx, st, 1);
            Assert.That(d.Major, Is.Not.Null);
            Assert.That(Notices(RaidNoticeKind.Skipped), Is.Empty, "the director did not refuse the pulled-in raid");
            RunTo(d.Major.StartsAt + 1);
            Assert.That(d.Major, Is.Not.Null);
            Assert.That(d.Major.Committed, Is.True);
        }

        // ---------------------------------------------------------------- skip the wait

        [Test]
        public void SkipTheWait_SlidesAWarnedRaidToTenSecondsFromNow_KeepingItsPlan_AndSaysSoAgain()
        {
            var d = st.Director;
            Run(new AdminCommand("major-now"));
            RaidFixture.Run(ctx, st, 1);
            var a = d.Major;
            var gaps = new double[a.Waves];
            for (var i = 0; i < a.Waves; i++) gaps[i] = a.WaveStart[i] - a.StartsAt;
            var (id, total, length) = (a.Id, a.Total, a.EndsAt - a.StartsAt);
            var counts = (int[])a.WaveCount.Clone();

            var t0 = st.T;
            var result = Run(new AdminCommand("skip-clock"));
            Assert.That(result.Accepted, Is.True, result.Problem);
            Assert.That(d.Major, Is.SameAs(a));
            Assert.That((a.Id, a.Total), Is.EqualTo((id, total)));
            Assert.That(a.WaveCount, Is.EqualTo(counts));
            Assert.That(a.StartsAt, Is.EqualTo(t0 + DirectorRules.AdminSkipLeadS).Within(1e-9));
            Assert.That(a.EndsAt - a.StartsAt, Is.EqualTo(length).Within(1e-9));
            for (var i = 0; i < a.Waves; i++) Assert.That(a.WaveStart[i] - a.StartsAt, Is.EqualTo(gaps[i]).Within(1e-9));

            RaidFixture.Run(ctx, st, 1);
            var said = Notices(RaidNoticeKind.Announced);
            Assert.That(said.Count, Is.EqualTo(2), "announced again with the corrected time");
            Assert.That(said[1].Text, Does.Contain("in 10 s"));

            RunTo(a.StartsAt + 2);
            Assert.That(a.Committed, Is.True);
            Assert.That(EnemyQueries.GroupAlive(st, a.Id), Is.GreaterThan(0));
        }

        [Test]
        public void SkipTheWait_WithNothingWarned_BringsTheNextRaidInOnAShortWarning()
        {
            var d = st.Director;
            var t0 = st.T;
            Assert.That(Run(new AdminCommand("skip-clock")).Accepted, Is.True);
            RaidFixture.Run(ctx, st, 1);
            Assert.That(d.Major, Is.Not.Null);
            Assert.That(d.Major.StartsAt, Is.EqualTo(t0 + DirectorRules.AdminSkipLeadS).Within(1e-9));
            Assert.That(Notices(RaidNoticeKind.Announced).Count, Is.EqualTo(1), "still announced — a short warning, not none");
        }

        // ---------------------------------------------------------------- it is not a side door

        [Test]
        public void TheToolsRefuse_WhatTheDirectorWouldRefuse_AndChangeNothing()
        {
            var d = st.Director;

            // The opening holds the approach.
            Director.Reserve(st, "The opening is still running.");
            foreach (var action in new[] { "major-now", "skip-clock" })
            {
                var before = CanonicalJsonWriter.Write(st);
                var held = Run(new AdminCommand(action));
                Assert.That(held.Accepted, Is.False, action);
                Assert.That(held.Problem, Is.EqualTo("The opening is still running."));
                Assert.That(CanonicalJsonWriter.Write(st), Is.EqualTo(before), action);
            }
            Director.Release(st);

            // The core is down: there is nothing to assault (E-18).
            st.Home.Hp = 0;
            foreach (var action in new[] { "major-now", "skip-clock" })
            {
                var before = CanonicalJsonWriter.Write(st);
                var down = Run(new AdminCommand(action));
                Assert.That(down.Accepted, Is.False, action);
                Assert.That(down.Problem, Does.Contain("no standing core"));
                Assert.That(CanonicalJsonWriter.Write(st), Is.EqualTo(before), action);
            }
            st.Home.Hp = ctx.Data.Defence.CoreHp;

            // A raid is already warned, then under way.
            Assert.That(Run(new AdminCommand("major-now")).Accepted, Is.True);
            RaidFixture.Run(ctx, st, 1);
            Assert.That(Run(new AdminCommand("major-now")).Accepted, Is.False, "one large raid at a time");
            Run(new AdminCommand("skip-clock"));
            RunTo(d.Major.StartsAt + 1);
            Assert.That(d.Major.Committed, Is.True);
            foreach (var action in new[] { "major-now", "skip-clock" })
            {
                var before = CanonicalJsonWriter.Write(st);
                Assert.That(Run(new AdminCommand(action)).Accepted, Is.False, action);
                Assert.That(CanonicalJsonWriter.Write(st), Is.EqualTo(before), action);
            }
        }

        // ---------------------------------------------------------------- the raid number

        [Test]
        public void TheRaidNumber_SizesTheNextRaidBooked_IsNeverSaved_AndResetsWithTheOtherOverrides()
        {
            var d = st.Director;
            Assert.That(DirectorPhase.GrowthStep(st), Is.Zero);
            Assert.That(SiegePlan.TotalFor(ctx, 4), Is.GreaterThan(SiegePlan.TotalFor(ctx, 0)), "the data grows raids at all");

            Assert.That(Run(new AdminCommand("raid-number", Amount: 4)).Accepted, Is.True);
            Assert.That(DirectorPhase.GrowthStep(st), Is.EqualTo(4));
            Assert.That(DirectorPhase.Survived(st), Is.Zero, "the history is not forged");
            Assert.That(d.History, Is.Empty);

            var loaded = SaveSerializer.ReadText(SaveSerializer.WriteText(st, ctx.Data), ctx.Data);
            Assert.That(loaded.Ok, Is.True, loaded.Reason);
            Assert.That(loaded.State.Admin.RaidNumber, Is.EqualTo(-1), "an Admin override, reset on load like the rest");

            Run(new AdminCommand("major-now"));
            RaidFixture.Run(ctx, st, 1);
            Assert.That(d.Major.Total, Is.EqualTo(SiegePlan.TotalFor(ctx, 4)));

            Assert.That(Run(new AdminCommand("reset-overrides")).Accepted, Is.True);
            Assert.That(st.Admin.RaidNumber, Is.EqualTo(-1));
            Assert.That(d.Major.Total, Is.EqualTo(SiegePlan.TotalFor(ctx, 4)), "a raid already warned keeps its plan");
        }

        [TestCase(-2)] [TestCase(51)]
        public void AnInvalidRaidNumberIsRefused(int n)
        {
            Assert.That(Run(new AdminCommand("raid-number", Amount: n)).Accepted, Is.False);
            Assert.That(st.Admin.RaidNumber, Is.EqualTo(-1));
        }

        // ---------------------------------------------------------------- the page

        [Test]
        public void TheReadoutMatchesTheSim_ThroughWaitingWarnedAssaultAndRecovery()
        {
            var d = st.Director;
            var r = ctx.Data.Raids;

            var v = DirectorQueries.Readout(ctx, st);
            Assert.That(v.Phase, Is.EqualTo("waiting"));
            Assert.That(v.NextStartIn, Is.EqualTo(d.NextStart - st.T).Within(1e-9));
            Assert.That(v.HasTarget, Is.True);
            Assert.That((v.TargetX, v.TargetY), Is.EqualTo((st.Home.X, st.Home.Y)));
            Assert.That(v.CoreHp, Is.EqualTo((double)st.Home.Hp));
            Assert.That(v.CoreMaxHp, Is.EqualTo((double)ctx.Data.Defence.CoreHp));
            Assert.That((v.ActiveBudget, v.LivingBudget), Is.EqualTo((r.ActiveRaidBudget, r.LivingBudget)));
            Assert.That((v.Survived, v.Pinned, v.NextBodies), Is.EqualTo((0, false, SiegePlan.TotalFor(ctx, 0))));
            Assert.That(v.LastOutcome, Is.EqualTo(-1));
            Assert.That(DirectorQueries.Describe(v), Does.Contain("Phase: waiting").And.Contain("Last large raid: none yet"));

            Run(new AdminCommand("raid-number", Amount: 2));
            v = DirectorQueries.Readout(ctx, st);
            Assert.That((v.Survived, v.Pinned, v.NextBodies), Is.EqualTo((2, true, SiegePlan.TotalFor(ctx, 2))));
            Assert.That(DirectorQueries.Describe(v), Does.Contain("Raids survived: 2 (pinned by Admin)"));
            Run(new AdminCommand("raid-number", Amount: -1));

            Run(new AdminCommand("major-now"));
            RaidFixture.Run(ctx, st, 1);
            v = DirectorQueries.Readout(ctx, st);
            Assert.That(v.Phase, Is.EqualTo("warned"));
            Assert.That(v.PhaseSeconds, Is.EqualTo(d.Major.StartsAt - st.T).Within(1e-9));
            Assert.That((v.Waves, v.Remaining), Is.EqualTo((d.Major.Waves, d.Major.Remaining)));

            Run(new AdminCommand("skip-clock"));
            RunTo(d.Major.StartsAt + 2);
            v = DirectorQueries.Readout(ctx, st);
            Assert.That(v.Phase, Is.EqualTo("assault"));
            Assert.That(v.MajorAlive, Is.EqualTo(EnemyQueries.GroupAlive(st, d.Major.Id)).And.GreaterThan(0));
            Assert.That(v.Living, Is.EqualTo(st.Enemies.Actors.Count));
            Assert.That(v.Remaining, Is.EqualTo(d.Major.Remaining));
            Assert.That(DirectorQueries.Describe(v), Does.Contain("assault · wave 1 of " + d.Major.Waves)
                .And.Contain("Large-raid bodies alive: " + v.MajorAlive + " of " + r.ActiveRaidBudget)
                .And.Contain("of " + r.LivingBudget));

            // The core falls: withdrawing for a tick's worth of state, then the raid is over and recovery runs (E-18).
            var id = d.Major.Id;
            st.Home.Hp = 0;
            RaidFixture.Run(ctx, st, 1);
            v = DirectorQueries.Readout(ctx, st);
            Assert.That(v.Phase, Is.EqualTo("recovery"));
            Assert.That(v.HasTarget, Is.False);
            Assert.That((v.LastId, v.LastOutcome), Is.EqualTo((id, (int)RaidOutcome.Lost)));
            Assert.That(DirectorQueries.Describe(v), Does.Contain("Target: none · the core is down")
                .And.Contain("raid " + id + " · lost (the core fell)"));
        }
    }
}
