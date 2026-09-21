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

        // ------------------------------------------------------------------ INT-04a, through the real director
        //
        // The tests above hand the source a raid record. These three do not: the DIRECTOR announces, stages, commits
        // and ends the raids, one tick at a time, and the source is refreshed after every tick as the HUD does it.

        private static System.Collections.Generic.List<ITickPhase> Director() =>
            new System.Collections.Generic.List<ITickPhase> { new DirectorPhase() };

        /// <summary>Tick the director alone, refreshing the account after every tick.</summary>
        private static void Drive(SimContext ctx, SimState st, RaidAccountSource acc, double seconds)
        {
            var ticks = (int)System.Math.Ceiling(seconds / RaidFixture.Dt);
            for (var i = 0; i < ticks; i++)
            {
                RaidFixture.Run(ctx, st, 1, Director());
                acc.Refresh(ctx, st);
            }
        }

        /// <summary>A fresh bench at the director's first small-raid opportunity, with the raid announced.</summary>
        private static (SimContext Ctx, SimState St, MinorRaid Raid) Announced()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            HomeCore.Ensure(ctx, st);
            st.T = st.Director.NextMinor;
            RaidFixture.Run(ctx, st, 1, Director());
            var m = st.Director.Minor;
            Assert.That(m, Is.Not.Null, "the director took the small-raid opportunity");
            Assert.That(m.Spawned, Is.False, "and it is a warning, with no bodies yet");
            return (ctx, st, m);
        }

        private const int Filler = 9000;

        /// <summary>
        /// INT-04a (1). The director holds a small raid back while the map is at its living-enemy budget and keeps
        /// trying for <c>MinorStageRetryS</c>, WITHOUT moving <c>StartsAt</c>. A raid that lands five seconds late
        /// used to read as "joined part-way" and got no account. This session watched it wait, so it gets one.
        /// </summary>
        [Test]
        public void ARaidTheDirectorStagesFiveSecondsLateStillGetsItsAccount()
        {
            var (ctx, st, m) = Announced();
            var acc = new RaidAccountSource();
            acc.Refresh(ctx, st);

            // Camp residents far from the core, up to the budget: the director can birth nothing.
            for (var i = 0; i < ctx.Data.Raids.LivingBudget; i++)
                RaidFixture.Body(st, "drone", 4 + i % 40, 4, EnemyLayer.Site, Filler);

            Drive(ctx, st, acc, m.StartsAt - st.T + 5);
            Assert.That(st.Director.Minor, Is.SameAs(m), "the director is still trying, inside its retry window");
            Assert.That(m.Spawned, Is.False, "held back for five seconds by the budget");
            Assert.That(acc.Watching, Is.False, "a raid with no bodies is not under way");

            st.Enemies.Actors.RemoveAll(e => e.Group == Filler);
            Drive(ctx, st, acc, 2 * RaidFixture.Dt);
            Assert.That(m.Spawned, Is.True, "room opened and the raid arrived");
            Assert.That(st.T - m.StartsAt, Is.GreaterThan(RaidAccountSource.ArrivalGraceS), "well past the old grace");
            Assert.That(acc.Watching, Is.True, "the late raid is tallied: this session saw it waiting");

            for (var i = 0; i < 4; i++) acc.Intake(new EnemyKilledEvent(st.T, i, "drone", 0, 0, true));
            st.Enemies.Actors.Clear();
            Drive(ctx, st, acc, 2 * RaidFixture.Dt);
            Assert.That(st.Director.Minor, Is.Null, "the director ended it");
            Assert.That(acc.Serial, Is.EqualTo(1));
            Assert.That(acc.Text, Does.StartWith("Raid repelled · 4 aliens killed"));
        }

        /// <summary>
        /// INT-04a (2). The same raid, saved ten seconds into the fight and loaded. The loaded session has a new
        /// HUD and so a new source, which never saw this raid wait or arrive: no account, as before.
        /// </summary>
        [Test]
        public void ASaveLoadedPartWayThroughARaidStillGetsNoAccount()
        {
            var (ctx, st, m) = Announced();
            var before = new RaidAccountSource();
            before.Refresh(ctx, st);
            Drive(ctx, st, before, m.StartsAt - st.T + 10);
            Assert.That(m.Spawned, Is.True);
            Assert.That(before.Watching, Is.True, "the session that saw it arrive is tallying it");

            var result = SaveSerializer.ReadText(SaveSerializer.WriteText(st, ctx.Data), ctx.Data);
            Assert.That(result.Ok, Is.True, result.Reason);
            var loaded = result.State;
            new EnemyInitializer().Init(ctx, loaded);

            var acc = new RaidAccountSource();                          // HudController makes a new model per session
            Drive(ctx, loaded, acc, 1);
            Assert.That(loaded.Director.Minor, Is.Not.Null, "the raid is still on in the loaded game");
            Assert.That(acc.Watching, Is.False, "met part-way: half a tally would be a false one");

            acc.Intake(new EnemyKilledEvent(loaded.T, 1, "drone", 0, 0, true));
            loaded.Enemies.Actors.Clear();
            Drive(ctx, loaded, acc, 2 * RaidFixture.Dt);
            Assert.That(loaded.Director.Minor, Is.Null);
            Assert.That(acc.Serial, Is.Zero);
            Assert.That(acc.Text, Is.Empty);
        }

        /// <summary>
        /// INT-04a (3). A large raid commits while a small one is on the ground. The director turns the small raid
        /// back itself (<c>DirectorPhase.Commit</c>), so the defence did not repel it: it closes with no account,
        /// and the tally that opens is the large raid's. The large raid's warning is cut to one second here by
        /// moving <c>NextStart</c>; everything after that is the director's own.
        /// </summary>
        [Test]
        public void ALargeRaidOverALiveSmallRaidLeavesTheSmallOneWithNoAccount()
        {
            var (ctx, st, m) = Announced();
            var acc = new RaidAccountSource();
            acc.Refresh(ctx, st);
            Drive(ctx, st, acc, m.StartsAt - st.T + 1);
            Assert.That(m.Spawned, Is.True);
            Assert.That(acc.Watching, Is.True, "the small raid is being tallied");
            acc.Intake(new EnemyKilledEvent(st.T, 1, "drone", 0, 0, true));

            st.Director.NextStart = st.T + 1;
            Drive(ctx, st, acc, 1 + 2 * RaidFixture.Dt);
            var major = st.Director.Major;
            Assert.That(major, Is.Not.Null);
            Assert.That(major.Committed, Is.True, "the large raid went ahead");
            Assert.That(st.Director.Minor, Is.SameAs(m), "the small raid's bodies are still on the map");
            Assert.That(m.Retreat, Is.True, "and the director, not the defence, turned it back");

            Assert.That(acc.Serial, Is.Zero, "no account for the replaced raid");
            Assert.That(acc.Text, Is.Empty);
            Assert.That(acc.Watching, Is.True, "the tally now open is the large raid's");

            // The small raid's last bodies leave while the large raid is still on. Still nothing is said for it.
            st.Enemies.Actors.RemoveAll(e => e.Group == m.Id);
            Drive(ctx, st, acc, 2 * RaidFixture.Dt);
            Assert.That(st.Director.Minor, Is.Null);
            Assert.That(acc.Serial, Is.Zero);
        }

        /// <summary>
        /// The other half of (3), on hand-built records because a real large raid runs for minutes: when the large
        /// raid ends FIRST, the replaced small raid is on its own again. It must not get a second tally.
        /// </summary>
        [Test]
        public void AReplacedSmallRaidIsNotTalliedAgainWhenTheLargeRaidEndsFirst()
        {
            var (ctx, st, acc) = Bench();
            Arrive(st);
            acc.Refresh(ctx, st);
            st.Director.Major = new MajorRaid { Id = 2, StartsAt = st.T, Committed = true, Remaining = 10 };
            acc.Refresh(ctx, st);
            Assert.That(acc.Serial, Is.Zero, "the small raid was dropped without a word");

            st.T += 60;
            st.Director.Major = null;                                   // the large raid ends; the small one lingers
            acc.Refresh(ctx, st);
            Assert.That(acc.Serial, Is.EqualTo(1), "the large raid is reported");
            Assert.That(acc.Text, Does.StartWith("Major assault"));
            Assert.That(acc.Watching, Is.False, "and the leftover small raid does not open a new tally");

            st.Director.Minor = null;
            acc.Refresh(ctx, st);
            Assert.That(acc.Serial, Is.EqualTo(1));
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
