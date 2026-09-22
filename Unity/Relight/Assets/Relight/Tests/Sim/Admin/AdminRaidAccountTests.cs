using System.Collections.Generic;
using NUnit.Framework;
using Relight.Sim.Tests.Combat;
using Relight.Sim.UI;

namespace Relight.Sim.Tests.Admin
{
    /// <summary>
    /// REL-119 (UI-08d): the Admin panel's raid is an ordinary small raid to the raid account and the raid log, and
    /// a raid the panel's clear-enemies removes is never called "repelled". The opening's scripted group keeps its
    /// old treatment: the goal card reports it, the account does not.
    ///
    /// The account and the log are fed the way the game feeds them: after every tick the account is refreshed and
    /// handed the tick's events, then the log observes them. The Admin commands are applied between ticks, straight
    /// to the sim, as <c>AdminPanelController</c> applies them, so the account can look before the end event is
    /// drained.
    /// </summary>
    public sealed class AdminRaidAccountTests
    {
        private SimContext ctx;
        private SimState st;
        private RaidAccountSource acc;
        private RaidLog log;

        [SetUp]
        public void Setup()
        {
            ctx = RaidFixture.Context();
            st = RaidFixture.State(ctx);
            new HomeCoreInitializer().Init(ctx, st);
            st.Director.NextMinor = 1e9;             // no small raid of the director's own
            RaidFixture.Run(ctx, st, 1);             // the director seeds its clock on the first tick
            st.Events.Clear();
            acc = new RaidAccountSource();
            log = new RaidLog();
        }

        private CommandResult Admin(string action, int amount = 1)
        {
            new AdminHandler().TryApply(ctx, st, new AdminCommand(action, Amount: amount), out var r);
            return r;
        }

        /// <summary>One tick, then the account and the log, in the order the HUD and <c>SimHost</c> use.</summary>
        private void Step(int ticks = 1)
        {
            for (var i = 0; i < ticks; i++)
            {
                RaidFixture.Run(ctx, st, 1);
                acc.Refresh(ctx, st);
                for (var e = 0; e < st.Events.Count; e++) acc.Intake(st.Events[e]);
                log.Observe(ctx, st, st.Events);
                st.Events.Clear();
            }
        }

        private bool StepUntil(double seconds, System.Func<bool> done)
        {
            var until = st.T + seconds;
            while (st.T < until)
            {
                Step();
                if (done()) return true;
            }
            return done();
        }

        private void RemoveGroup(int id)
        {
            var list = st.Enemies.Actors;
            for (var i = list.Count - 1; i >= 0; i--) if (list[i].Group == id) list.RemoveAt(i);
        }

        [Test]
        public void AnAdminRaidRunToItsEnd_GetsOneAccount_AndAnOrdinaryLogLine()
        {
            st.Director.NextStart = 1e9;             // and no large raid
            var gun = RaidFixture.Turret(ctx, st, RaidFixture.CoreX + RaidFixture.CoreSize, RaidFixture.CoreY, rounds: 200);
            Step();

            var r = Admin("raid", 3);
            Assert.That(r.Accepted, Is.True, r.Problem);
            var raid = st.Director.Minor;
            Assert.That(raid, Is.Not.Null);
            Assert.That(raid.Scripted, Is.False, "the Admin raid is an ordinary small raid");
            var id = raid.Id;

            Step();
            Assert.That(acc.Watching, Is.True, "the account opened on it");

            Assert.That(StepUntil(300, () => st.Director.Minor == null), Is.True, "the raid ended");
            Assert.That(acc.Serial, Is.EqualTo(1), "one account");
            Assert.That(acc.Text, Does.StartWith("Raid "));

            Step(40);
            Assert.That(acc.Serial, Is.EqualTo(1), "and only one");
            Assert.That(log.Lines.Count, Is.EqualTo(1), "one log line");
            Assert.That(log.Lines[0].RaidId, Is.EqualTo(id));
            Assert.That(log.Lines[0].Scripted, Is.False);
            Assert.That(log.Lines[0].Text, Does.StartWith("raid #" + id + " small "));
            Assert.That(st.Director.RaidsStarted, Is.Zero, "a staged raid still never advances the raid counter");
            Assert.That(gun, Is.Not.Null);
        }

        [Test]
        public void ClearingALiveSmallRaid_SaysNothingRepelled_AndClosesItsLogLineAsCancelled()
        {
            st.Director.NextStart = 1e9;
            Assert.That(Admin("raid", 4).Accepted, Is.True);
            var id = st.Director.Minor.Id;
            Step(2);
            Assert.That(acc.Watching, Is.True);

            var cleared = Admin("clear-enemies");
            Assert.That(cleared.Accepted, Is.True, cleared.Problem);
            acc.Refresh(ctx, st);                    // the HUD can look before the end event is drained
            Assert.That(acc.Watching, Is.False);
            Assert.That(acc.Serial, Is.Zero, "no account for a raid a tool removed");

            Step(40);
            Assert.That(acc.Serial, Is.Zero);
            Assert.That(acc.Text, Does.Not.Contain("repelled"));
            Assert.That(log.Watching, Is.Zero, "the log closed the raid's line");
            Assert.That(log.Lines.Count, Is.EqualTo(1));
            Assert.That(log.Lines[0].RaidId, Is.EqualTo(id));
            Assert.That(log.Lines[0].Outcome, Is.EqualTo((int)RaidOutcome.Cancelled));
            Assert.That(log.Lines[0].Text, Does.StartWith("raid #" + id + " small cancelled by Admin"));
        }

        [Test]
        public void ClearingALargeRaidUnderWay_IsSilentToo_AndLeavesNoRecord()
        {
            Assert.That(Admin("major-now").Accepted, Is.True);
            Step();
            var plan = st.Director.Major;
            Assert.That(plan, Is.Not.Null);
            Assert.That(StepUntil(plan.StartsAt + 3 - st.T, () => EnemyQueries.GroupAlive(st, plan.Id) > 0), Is.True,
                "the first wave arrived");
            Assert.That(acc.Watching, Is.True);

            Assert.That(Admin("clear-enemies").Accepted, Is.True);
            acc.Refresh(ctx, st);
            Assert.That(acc.Serial, Is.Zero);

            Step(40);
            Assert.That(acc.Serial, Is.Zero, "no \"Major assault repelled\" and no \"over\"");
            Assert.That(log.Lines.Count, Is.EqualTo(1));
            Assert.That(log.Lines[0].Major, Is.True);
            Assert.That(log.Lines[0].Text, Does.StartWith("raid #" + plan.Id + " large cancelled by Admin"));
            foreach (var h in st.Director.History)
                Assert.That(h.Id, Is.Not.EqualTo(plan.Id), "a cancelled raid is never in the saved history");
        }

        [Test]
        public void TheOpeningsScriptedGroup_StillGetsNoAccount()
        {
            st.Director.NextStart = 1e9;
            var origin = DirectorRules.Origin(ctx, st);
            Assert.That(origin, Is.GreaterThanOrEqualTo(0));
            // The call the opening makes (OpeningPhase), with the default left alone.
            var id = Director.ScheduleGroup(ctx, st, origin, 3, true, 0);
            Assert.That(id, Is.Not.Zero);
            Assert.That(st.Director.Minor.Scripted, Is.True);

            Step();
            Assert.That(acc.Watching, Is.False);
            RemoveGroup(id);
            Step(5);
            Assert.That(st.Director.Minor, Is.Null, "the group is over");
            Assert.That(acc.Serial, Is.Zero, "the goal card owns the opening's fight");
            Assert.That(log.Lines.Count, Is.EqualTo(1), "the log still measures it");
            Assert.That(log.Lines[0].Text, Does.StartWith("raid #" + id + " scripted "));
        }
    }
}
