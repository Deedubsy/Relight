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

            for (var i = 0; i < 3; i++) acc.Intake(new EnemyKilledEvent(st.T, i, "drone", 0, 0, true, EnemyLayer.Minor, 1));
            var gun = RaidFixture.Turret(ctx, st, 20, 20);
            for (var i = 0; i < 12; i++) acc.Intake(new TurretShotEvent(st.T, gun.Id, 0, 0, 0, true));
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
            var wall = RaidFixture.Add(ctx, st, "wall", 30, 30);
            Arrive(st);
            acc.Refresh(ctx, st);

            dry.Rounds = 0;                                             // it fired its last round during the fight
            st.Turrets.Of(dry.Id).ShotT = st.T;
            acc.Intake(new EnemyKilledEvent(st.T, 1, "drone", 0, 0, true, EnemyLayer.Minor, 1));
            acc.Intake(new TurretBlindEvent(st.T, dry.Id, 0, 0));
            acc.Intake(new TurretBlindEvent(st.T, dry.Id, 0, 0));       // the same turret, hit twice
            acc.Intake(new PowerOutageEvent(st.T, dry.Id));
            acc.Intake(new StructureDamagedEvent(st.T, wall.Id, 40, 100));   // damaged, not wrecked
            acc.Intake(new StructureDamagedEvent(st.T, wall.Id, 0, 100));
            acc.Intake(new CoreDamagedEvent(st.T, HomeQueries.CoreHp(st) - 35, 35));
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
            acc.Intake(new CoreDamagedEvent(st.T, 0, HomeQueries.CoreHp(st)));
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

            acc.Intake(new EnemyKilledEvent(st.T, 1, "drone", 0, 0, false, EnemyLayer.Minor, 1));   // a stray kill before it lands
            st.T += 60.5;
            acc.Refresh(ctx, st);
            Assert.That(acc.Watching, Is.True);
            st.Director.History.Add(new RaidRecord { Id = 3, Outcome = (int)RaidOutcome.Cleared });   // as EndMajor leaves it
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
            acc.Intake(new EnemyKilledEvent(st.T, 1, "drone", 0, 0, true, EnemyLayer.Minor, 1));
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

        /// <summary>
        /// Tick the director (alone, unless told otherwise), then refresh the account and hand it that tick's
        /// events, in the order <c>HudViewModel</c> does it.
        /// </summary>
        private static void Drive(SimContext ctx, SimState st, RaidAccountSource acc, double seconds,
            System.Collections.Generic.List<ITickPhase> phases = null)
        {
            var ticks = (int)System.Math.Ceiling(seconds / RaidFixture.Dt);
            for (var i = 0; i < ticks; i++)
            {
                var seen = st.Events.Count;
                RaidFixture.Run(ctx, st, 1, phases ?? Director());
                acc.Refresh(ctx, st);
                for (var e = seen; e < st.Events.Count; e++) acc.Intake(st.Events[e]);
            }
        }

        /// <summary>A fresh bench at the director's first small-raid opportunity, with the raid announced.</summary>
        private static (SimContext Ctx, SimState St, MinorRaid Raid) Announced(SimContext on = null)
        {
            var ctx = on ?? RaidFixture.Context();
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

            for (var i = 0; i < 4; i++) acc.Intake(new EnemyKilledEvent(st.T, i, "drone", 0, 0, true, EnemyLayer.Minor, m.Id));
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

            acc.Intake(new EnemyKilledEvent(loaded.T, 1, "drone", 0, 0, true, EnemyLayer.Minor, 1));
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
        /// moving <c>NextStart</c>, once the small raid has outlasted the pacing hold (REL-75); everything after
        /// that is the director's own.
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
            acc.Intake(new EnemyKilledEvent(st.T, 1, "drone", 0, 0, true, EnemyLayer.Minor, 1));

            // REL-75 (U-P-22): a large-raid warning waits while a small raid is on, but only for MinorHoldCapS
            // after it arrived. This is the small raid that outlasts that wait.
            st.T = System.Math.Max(st.T, m.StartsAt + ctx.Data.Siege.MinorHoldCapS);
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

        // ------------------------------------------------------------------ INT-04b: this raid, this place
        //
        // A map with two districts. The Home core (76,76) stands in the first; the second is the far corner. The
        // raid is the director's own. The fight beside it is real too: a turret shoots a camp resident dead
        // (TurretPhase), a wall is wrecked through TurretRules.Damage, and a pole is pulled so PowerPhase reports
        // the cut. The only thing the two tests change is WHERE that fight happens.

        private const int HomeX = 60, HomeY = 60, FarX = 10, FarY = 10;

        private static SimContext TwoPlaces() =>
            new SimContext(ReferenceData.Create(), RaidFixture.Map(), null, null, new WorldSites(
                new System.Collections.Generic.List<SiteRecord>
                {
                    new SiteRecord("home", "Home Court", SiteKind.Core, RaidFixture.CoreX, RaidFixture.CoreY,
                        RaidFixture.CoreSize, RaidFixture.CoreSize, "", 0),
                    new SiteRecord("substation:0", "Home", SiteKind.Substation, 78, 70, 2, 2, "", 0),
                    new SiteRecord("substation:1", "Far Yard", SiteKind.Substation, 4, 4, 2, 2, "", 0),
                }));

        private static System.Collections.Generic.List<ITickPhase> Fight() =>
            new System.Collections.Generic.List<ITickPhase> { new PowerPhase(), new TurretPhase(), new DirectorPhase() };

        /// <summary>
        /// A live raid being tallied, and beside it (at x,y) one turret shooting one camp resident dead, one wall
        /// wrecked and one power cut. Returns with the raid ended and the account written.
        /// </summary>
        private static (SimState St, RaidAccountSource Acc, int Shots) ARaidWithAFightAt(int x, int y)
        {
            var ctx = TwoPlaces();
            Assert.That(Districts.Of(ctx).IndexAt(RaidFixture.CoreX + 4, RaidFixture.CoreY + 4), Is.EqualTo(0));
            Assert.That(Districts.Of(ctx).IndexAt(HomeX, HomeY), Is.EqualTo(0), "the near fight is in the core's district");
            Assert.That(Districts.Of(ctx).IndexAt(FarX, FarY), Is.EqualTo(1), "the far fight is not");

            var (_, st, m) = Announced(ctx);
            var gun = RaidFixture.Turret(ctx, st, x, y);
            var wall = RaidFixture.Add(ctx, st, "wall", x, y + 8);
            var acc = new RaidAccountSource();
            acc.Refresh(ctx, st);
            Drive(ctx, st, acc, m.StartsAt - st.T + 1, Fight());
            Assert.That(m.Spawned, Is.True);
            Assert.That(acc.Watching, Is.True);

            // A camp resident four tiles from the gun. It is not this raid's body, wherever it stands.
            RaidFixture.Body(st, "drone", x + 5.5, y + 0.5, EnemyLayer.Site, Filler);
            Drive(ctx, st, acc, 6, Fight());
            var shots = RaidFixture.Count<TurretShotEvent>(st);
            Assert.That(shots, Is.GreaterThan(0), "the turret really fired");
            Assert.That(RaidFixture.Count<EnemyKilledEvent>(st), Is.EqualTo(1), "and really killed the camp resident");

            var seen = st.Events.Count;
            TurretRules.Damage(ctx, st, wall, 1e9);
            st.Machines.RemoveAll(k => k.Kind == "pole");
            st.Rev++;
            for (var e = seen; e < st.Events.Count; e++) acc.Intake(st.Events[e]);
            Drive(ctx, st, acc, 1, Fight());
            Assert.That(RaidFixture.Count<PowerOutageEvent>(st), Is.GreaterThan(0), "the cut was really reported");
            Assert.That(TurretRules.Wrecked(ctx.Data, st, wall), Is.True, "the wall was really wrecked");

            // One of the raid's own bodies dies, far from everything. It is this raid's kill wherever it falls.
            var body = st.Enemies.Actors.Find(e => e.Group == m.Id);
            Assert.That(body, Is.Not.Null);
            seen = st.Events.Count;
            Enemies.Damage(ctx, st, body.Id, 1e9);
            for (var e = seen; e < st.Events.Count; e++) acc.Intake(st.Events[e]);

            st.Enemies.Actors.RemoveAll(e => e.Group == m.Id);
            Drive(ctx, st, acc, 2 * RaidFixture.Dt, Fight());
            Assert.That(st.Director.Minor, Is.Null, "the director ended the raid");
            Assert.That(acc.Serial, Is.EqualTo(1));
            return (st, acc, shots);
        }

        /// <summary>
        /// INT-04b's acceptance: a kill, a power cut and a wreck far from the target are all left out. So are the
        /// far turret's rounds. The raid's own dead body is the one thing counted.
        ///
        /// Save note: the raid id a kill is matched on is <see cref="Enemy.Group"/>, which every save already
        /// carries (the issue expected new saved state; none was needed), so a raid in flight at load counts its
        /// kills in full. It still gets no account, for INT-04a's reason: the session did not see it arrive.
        /// </summary>
        [Test]
        public void AFightFarFromTheRaidsTargetIsLeftOutOfItsAccount()
        {
            var (_, acc, _) = ARaidWithAFightAt(FarX, FarY);
            Assert.That(acc.Losses, Is.False);
            Assert.That(acc.Text, Is.EqualTo("Raid repelled · 1 alien killed · 0 rounds fired · nothing lost"));
        }

        /// <summary>The control: the same fight in the target's own district is counted, all but the camp kill.</summary>
        [Test]
        public void TheSameFightAtTheRaidsTargetIsCounted()
        {
            var (_, acc, shots) = ARaidWithAFightAt(HomeX, HomeY);
            var lines = acc.Text.Split('\n');
            Assert.That(lines[0], Is.EqualTo("Raid over · 1 alien killed · " + shots + " rounds fired"),
                "the camp resident is still not this raid's kill; the rounds spent on it here are");
            Assert.That(lines[1], Does.Contain("power failed"));
            Assert.That(lines[1], Does.Contain("1 structure wrecked"));
        }

        // ---------------------------------------------------------------- INT-04c: "repelled" only when true
        //
        // Director-driven, like the INT-04a tests. A large raid's outcome is read off its saved record, and the
        // account must agree with it. A small raid keeps no saved record, so its tests read the account's own.

        /// <summary>A bench whose large raid has been warned, committed and started under the account's eyes.</summary>
        private static (SimContext Ctx, SimState St, RaidAccountSource Acc, MajorRaid Raid) AnAssaultUnderWay()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            new HomeCoreInitializer().Init(ctx, st);
            st.Director.NextMinor = 1e9;                                // no small raid is due in these
            var acc = new RaidAccountSource();
            st.T = st.Director.NextStart - ctx.Data.Raids.WarningS;
            Drive(ctx, st, acc, RaidFixture.Dt);
            Assert.That(st.Director.Major, Is.Not.Null, "the fixture map must be able to stage an assault");
            st.T = st.Director.Major.StartsAt;
            Drive(ctx, st, acc, 2);
            var a = st.Director.Major;
            Assert.That(a.Committed, Is.True);
            Assert.That(acc.Watching, Is.True);
            return (ctx, st, acc, a);
        }

        /// <summary>Hit the core between ticks and hand the account the events, as the next frame would.</summary>
        private static void HitTheCore(SimState st, RaidAccountSource acc, double amount)
        {
            var seen = st.Events.Count;
            HomeCore.Damage(st, amount);
            for (var e = seen; e < st.Events.Count; e++) acc.Intake(st.Events[e]);
        }

        /// <summary>Every body of the assault is dead and it owes no more: the director's CLEARED ending.</summary>
        private static void KillThemAll(SimState st, MajorRaid a)
        {
            a.Cancelled += a.Remaining;
            a.Remaining = 0;
            st.Enemies.Actors.Clear();
        }

        private static RaidRecord RecordOf(SimState st, int id)
        {
            var h = st.Director.History;
            for (var i = h.Count - 1; i >= 0; i--) if (h[i].Id == id) return h[i];
            return null;
        }

        [Test]
        public void ACleanWinIsRecordedAsClearedAndReadsRepelled()
        {
            var (ctx, st, acc, a) = AnAssaultUnderWay();
            KillThemAll(st, a);
            Drive(ctx, st, acc, RaidFixture.Dt);

            Assert.That(RecordOf(st, a.Id).Outcome, Is.EqualTo((int)RaidOutcome.Cleared));
            Assert.That(acc.Outcome, Is.EqualTo((int)RaidOutcome.Cleared));
            Assert.That(acc.Losses, Is.False);
            Assert.That(acc.Text, Does.StartWith("Major assault repelled"));
            Assert.That(acc.Text, Does.EndWith("nothing lost"));
        }

        [Test]
        public void ALostRaidIsRecordedAsLostAndNeverReadsRepelled()
        {
            var (ctx, st, acc, a) = AnAssaultUnderWay();
            HitTheCore(st, acc, 1e9);
            Drive(ctx, st, acc, RaidFixture.Dt);

            Assert.That(st.Director.Major, Is.Null, "a raid that beat its target is over at once (E-18)");
            Assert.That(RecordOf(st, a.Id).Outcome, Is.EqualTo((int)RaidOutcome.Lost));
            Assert.That(acc.Outcome, Is.EqualTo((int)RaidOutcome.Lost));
            Assert.That(acc.Losses, Is.True);
            Assert.That(acc.Text, Does.StartWith("Major assault over"));
            Assert.That(acc.Text, Does.Not.Contain("repelled"));
            Assert.That(acc.Text, Does.EndWith("the core was disabled"));
        }

        [Test]
        public void ARaidThatBrokeOffIsRecordedAsThatAndCreditsNobody()
        {
            var (ctx, st, acc, a) = AnAssaultUnderWay();
            a.EndsAt = st.T - ctx.Data.Siege.MajorOverrunS - 1;          // long past its planned end, bodies alive
            Drive(ctx, st, acc, RaidFixture.Dt);

            Assert.That(st.Director.Major, Is.Null);
            Assert.That(RecordOf(st, a.Id).Outcome, Is.EqualTo((int)RaidOutcome.BrokeOff));
            Assert.That(acc.Outcome, Is.EqualTo((int)RaidOutcome.BrokeOff));
            Assert.That(acc.Text, Does.StartWith("Major assault broke off"));
            Assert.That(acc.Text, Does.Not.Contain("repelled"), "what a broken-off raid counts as is F1-30, still open");
        }

        [Test]
        public void ARepairInTheMiddleOfARaidDoesNotHideTheDamageBeforeIt()
        {
            var (ctx, st, acc, a) = AnAssaultUnderWay();
            var max = (double)ctx.Data.Defence.CoreHp;
            HitTheCore(st, acc, max * 0.4);
            st.Home.Hp = max;                                           // what HomeCorePhase does when a repair lands
            HitTheCore(st, acc, max * 0.25);
            KillThemAll(st, a);
            Drive(ctx, st, acc, RaidFixture.Dt);

            Assert.That(RecordOf(st, a.Id).Outcome, Is.EqualTo((int)RaidOutcome.Cleared));
            var lost = System.Math.Ceiling(max * 0.4 + max * 0.25).ToString("0", System.Globalization.CultureInfo.InvariantCulture);
            Assert.That(acc.Text, Does.EndWith("core lost " + lost + " HP"), "both hits, not only the first");
            Assert.That(acc.Text, Does.StartWith("Major assault over"), "cleared, and the core stands, but it was hurt");
        }

        [Test]
        public void ACoreAlreadyDownBeforeALargeRaidMeansNoRaid_NoRecordAndNoAccount()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            new HomeCoreInitializer().Init(ctx, st);
            st.Director.NextMinor = 1e9;
            var acc = new RaidAccountSource();
            st.T = st.Director.NextStart - ctx.Data.Raids.WarningS;
            Drive(ctx, st, acc, RaidFixture.Dt);
            var a = st.Director.Major;
            Assert.That(a, Is.Not.Null);

            HomeCore.Damage(st, 1e9);                                   // it falls during the warning
            st.T = a.StartsAt;
            Drive(ctx, st, acc, 2);

            Assert.That(st.Director.Major, Is.Null, "the director drops an assault that has no core to go for");
            Assert.That(RecordOf(st, a.Id), Is.Null);
            Assert.That(acc.Serial, Is.Zero);
        }

        [Test]
        public void ACoreAlreadyDownBeforeASmallRaidNeverReadsRepelledNothingLost()
        {
            var (ctx, st, m) = Announced();
            var acc = new RaidAccountSource();
            acc.Refresh(ctx, st);
            HomeCore.Damage(st, 1e9);                                   // down before a single raider is on the map
            st.T = m.StartsAt;
            Drive(ctx, st, acc, 1);
            Assert.That(m.Spawned, Is.True, "the announced raid still arrives");
            Assert.That(acc.Watching, Is.True);
            Assert.That(m.Retreat, Is.True, "and is called off at once: there is nothing to beat (E-18)");

            st.Enemies.Actors.Clear();                                  // they have walked off
            Drive(ctx, st, acc, RaidFixture.Dt);

            Assert.That(acc.Serial, Is.EqualTo(1));
            Assert.That(acc.Outcome, Is.EqualTo((int)RaidOutcome.Lost));
            Assert.That(acc.Losses, Is.True);
            Assert.That(acc.Text, Does.StartWith("Raid over"));
            Assert.That(acc.Text, Does.Not.Contain("repelled"));
            Assert.That(acc.Text, Does.Not.Contain("nothing lost"));
            Assert.That(acc.Text, Does.EndWith("the core was already down"));
        }

        [Test]
        public void ASmallRaidCalledOffWithTheCoreStandingBrokeOff()
        {
            var (ctx, st, m) = Announced();
            var acc = new RaidAccountSource();
            acc.Refresh(ctx, st);
            st.T = m.StartsAt;
            Drive(ctx, st, acc, 1);
            Assert.That(acc.Watching, Is.True);

            m.Retreat = true;                                           // as C-09's withdrawal leaves it
            m.Owed = 0;
            Drive(ctx, st, acc, RaidFixture.Dt);
            st.Enemies.Actors.Clear();
            Drive(ctx, st, acc, RaidFixture.Dt);

            Assert.That(acc.Outcome, Is.EqualTo((int)RaidOutcome.BrokeOff));
            Assert.That(acc.Text, Does.StartWith("Raid broke off"));
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

        /// <summary>
        /// GP-W6's "posted exactly once", kept through REL-74: the account is now shown as the report card instead of
        /// a notice row, so the check is that the card opens once per finished account, closes on its timer, does not
        /// reopen, and that no notice row repeats it.
        /// </summary>
        [Test]
        public void TheHudShowsEachAccountExactlyOnce()
        {
            var (ctx, st, _) = Bench();
            var vm = new HudViewModel();
            vm.Refresh(ctx, st, 0, false, false, force: true);

            Arrive(st);
            vm.Refresh(ctx, st, 1, false, false, force: true);
            vm.Intake(new SimEvent[] { new EnemyKilledEvent(st.T, 1, "drone", 0, 0, true, EnemyLayer.Minor, 1) }, 1);
            Assert.That(vm.ReportVisible, Is.False, "nothing is said while the fight is on");

            Leave(st);
            vm.Refresh(ctx, st, 2, false, false, force: true);
            Assert.That(vm.ReportVisible, Is.True);
            Assert.That(vm.ReportTitle, Is.EqualTo("Raid repelled"));
            Assert.That(vm.ReportLoss, Is.False);
            Assert.That(NoticeSaying(vm, vm.Account.Text), Is.False, "the card replaces the notice row; not both");

            vm.Refresh(ctx, st, 2 + RaidAccountSource.Seconds + 0.5, false, false, force: true);
            Assert.That(vm.ReportVisible, Is.False, "the card closes on its timer");
            for (var i = 30; i < 35; i++) vm.Refresh(ctx, st, i, false, false, force: true);
            Assert.That(vm.ReportVisible, Is.False, "shown when the raid ended, never again");
            Assert.That(vm.Account.Serial, Is.EqualTo(1));
        }

        private static bool NoticeSaying(HudViewModel vm, string text)
        {
            for (var i = 0; i < vm.Notices.Rows.Count; i++)
                if (vm.Notices.Rows[i].Text == text) return true;
            return false;
        }
    }
}
