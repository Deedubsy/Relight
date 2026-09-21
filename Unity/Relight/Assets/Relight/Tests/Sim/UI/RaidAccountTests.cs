using NUnit.Framework;
using Relight.Sim.Tests.Combat;
using Relight.Sim.UI;

namespace Relight.Sim.Tests.UI
{
    /// <summary>
    /// GP-W6's post-attack account, held to the brief's own rule: it reports <b>only causes supported by actual
    /// events</b>. Each test drives <see cref="RaidAccountSource"/> with a raid record and the events a real fight
    /// raises, and checks that a clause is written when its event happened and is absent when it did not.
    /// </summary>
    public sealed class RaidAccountTests
    {
        private static (SimContext Ctx, SimState St, RaidAccountSource Acc) Bench()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            HomeCore.Ensure(ctx, st);
            st.T = 100;
            return (ctx, st, new RaidAccountSource());
        }

        private static void Arrive(SimState st, int id = 1, bool scripted = false) =>
            st.Director.Minor = new MinorRaid { Id = id, Spawned = true, Scripted = scripted, StartsAt = st.T };

        private static void Leave(SimState st) { st.Director.Minor = null; st.Director.Major = null; }

        [Test]
        public void ARaidThatCostNothingIsRepelledAndSaysSo()
        {
            var (ctx, st, acc) = Bench();
            Arrive(st);
            acc.Refresh(ctx, st);
            Assert.That(acc.Watching, Is.True);

            for (var i = 0; i < 3; i++) acc.Intake(new EnemyKilledEvent(st.T, i, "drone", 0, 0, true));
            st.Stats.Fired += 12;
            Leave(st);
            acc.Refresh(ctx, st);

            Assert.That(acc.Serial, Is.EqualTo(1));
            Assert.That(acc.Losses, Is.False);
            Assert.That(acc.Text, Is.EqualTo("Raid repelled · 3 aliens killed · 12 rounds fired · nothing lost"));
        }

        [Test]
        public void EveryLossClauseComesFromItsOwnEvent()
        {
            var (ctx, st, acc) = Bench();
            var dry = RaidFixture.Turret(ctx, st, 20, 20, rounds: 5);
            Arrive(st);
            acc.Refresh(ctx, st);

            dry.Rounds = 0;                                             // it fired its last round during the fight
            st.Turrets.Of(dry.Id).ShotT = st.T;
            acc.Intake(new EnemyKilledEvent(st.T, 1, "drone", 0, 0, true));
            acc.Intake(new TurretBlindEvent(st.T, dry.Id, 0, 0));
            acc.Intake(new TurretBlindEvent(st.T, dry.Id, 0, 0));       // the same turret, hit twice
            acc.Intake(new PowerOutageEvent(st.T, 4));
            acc.Intake(new StructureDamagedEvent(st.T, 9, 40, 100));    // damaged, not wrecked
            acc.Intake(new StructureDamagedEvent(st.T, 9, 0, 100));
            acc.Intake(new CoreDamagedEvent(st.T, HomeQueries.CoreHp(st) - 35));
            acc.Refresh(ctx, st);
            Leave(st);
            acc.Refresh(ctx, st);

            Assert.That(acc.Losses, Is.True);
            var lines = acc.Text.Split('\n');
            Assert.That(lines.Length, Is.EqualTo(2));
            Assert.That(lines[0], Is.EqualTo("Raid over · 1 alien killed · 0 rounds fired"),
                "with losses the headline claims nothing: it says over, not repelled");
            Assert.That(lines[1], Is.EqualTo(
                "1 turret ran dry · 1 turret hit from the dark · power failed · 1 structure wrecked · core lost 35 HP"));
        }

        [Test]
        public void ATurretThatWasAlreadyEmptyDidNotRunDryInThisFight()
        {
            var (ctx, st, acc) = Bench();
            var t = RaidFixture.Turret(ctx, st, 20, 20, rounds: 0);
            st.Turrets.Of(t.Id).ShotT = 1;                              // loaded and used once, long ago
            Arrive(st);
            acc.Refresh(ctx, st);
            Leave(st);
            acc.Refresh(ctx, st);

            Assert.That(acc.Text, Does.Not.Contain("ran dry"));
            Assert.That(acc.Losses, Is.False);
        }

        [Test]
        public void ADisabledCoreIsNamedAsThat()
        {
            var (ctx, st, acc) = Bench();
            Arrive(st);
            acc.Refresh(ctx, st);
            acc.Intake(new CoreDamagedEvent(st.T, 0));
            acc.Intake(new CoreDisabledEvent(st.T));
            Leave(st);
            acc.Refresh(ctx, st);

            Assert.That(acc.Text, Does.EndWith("the core was disabled"));
            Assert.That(acc.Text, Does.Not.Contain("core lost"));
        }

        [Test]
        public void AMajorAssaultCountsFromItsStartNotFromItsWarning()
        {
            var (ctx, st, acc) = Bench();
            st.Director.Major = new MajorRaid { Id = 3, StartsAt = st.T + 60, Remaining = 10 };
            acc.Refresh(ctx, st);
            Assert.That(acc.Watching, Is.False, "a warned raid is a countdown, not an attack (GP-W3)");

            acc.Intake(new EnemyKilledEvent(st.T, 1, "drone", 0, 0, false));   // a stray kill before it lands
            st.T += 60.5;
            acc.Refresh(ctx, st);
            Assert.That(acc.Watching, Is.True);
            Leave(st);
            acc.Refresh(ctx, st);

            Assert.That(acc.Text, Does.StartWith("Major assault repelled · 0 aliens killed"),
                "the kill before it arrived is not this raid's");
        }

        [Test]
        public void ARaidJoinedPartWayGetsNoAccountAtAll()
        {
            var (ctx, st, acc) = Bench();
            Arrive(st);
            st.T += 45;                                                 // the save was loaded mid-fight
            acc.Refresh(ctx, st);
            Assert.That(acc.Watching, Is.False);
            acc.Intake(new EnemyKilledEvent(st.T, 1, "drone", 0, 0, true));
            Leave(st);
            acc.Refresh(ctx, st);

            Assert.That(acc.Serial, Is.Zero, "half a tally would be a false one");
            Assert.That(acc.Text, Is.Empty);
        }

        [Test]
        public void TheScriptedOpeningIsLeftToTheGoalCard()
        {
            var (ctx, st, acc) = Bench();
            Arrive(st, scripted: true);
            acc.Refresh(ctx, st);
            Assert.That(acc.Watching, Is.False);
            Leave(st);
            acc.Refresh(ctx, st);
            Assert.That(acc.Serial, Is.Zero);
        }

        [Test]
        public void TheHudPostsEachAccountExactlyOnce()
        {
            var (ctx, st, _) = Bench();
            var vm = new HudViewModel();
            vm.Refresh(ctx, st, 0, false, false, force: true);

            Arrive(st);
            vm.Refresh(ctx, st, 1, false, false, force: true);
            vm.Intake(new SimEvent[] { new EnemyKilledEvent(st.T, 1, "drone", 0, 0, true) }, 1);
            Assert.That(Row(vm), Is.Null, "nothing is said while the fight is on");

            Leave(st);
            vm.Refresh(ctx, st, 2, false, false, force: true);
            var row = Row(vm);
            Assert.That(row, Is.Not.Null);
            Assert.That(row.Text, Is.EqualTo(vm.Account.Text));
            Assert.That(row.Kind, Is.EqualTo(HudNoticeKind.Info));

            for (var i = 3; i < 8; i++) vm.Refresh(ctx, st, i, false, false, force: true);
            Assert.That(Row(vm).Repeats, Is.EqualTo(1), "posted when the raid ended, never again");
        }

        private static HudNotice Row(HudViewModel vm)
        {
            for (var i = 0; i < vm.Notices.Rows.Count; i++)
                if (vm.Notices.Rows[i].Key == RaidAccountSource.Key) return vm.Notices.Rows[i];
            return null;
        }
    }
}
