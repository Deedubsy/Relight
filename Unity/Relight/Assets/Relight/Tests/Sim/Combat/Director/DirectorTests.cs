using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Relight.Sim.Tests.Combat
{
    /// <summary>
    /// C-08 raid director. The clock, the announcement and the deterministic choice function are pinned against
    /// campaignThreat.ts; the deferral is the port's own (U-D-26), so it is pinned against the ruling instead.
    /// </summary>
    public sealed class DirectorTests
    {
        /// <summary>
        /// campaignThreat.ts:29 <c>raidChoice</c>. These seven values were computed by hand from the reference
        /// expression, so a drift in the wrapping multiply or in the unsigned reinterpretation is caught here and
        /// not three thousand simulated seconds later.
        /// </summary>
        [Test]
        public void TheChoiceFunctionMatchesTheReferenceArithmetic()
        {
            Assert.That(DirectorRules.RaidChoice(7, 0, 300), Is.EqualTo(212));
            Assert.That(DirectorRules.RaidChoice(7, 1, 240), Is.EqualTo(201));
            Assert.That(DirectorRules.RaidChoice(0, 0, 300), Is.EqualTo(262));
            Assert.That(DirectorRules.RaidChoice(12345, 5, 240), Is.EqualTo(121));
            Assert.That(DirectorRules.RaidChoice(7, 3, 4), Is.EqualTo(3));
            Assert.That(DirectorRules.RaidChoice(7, 0, 120), Is.EqualTo(63));
            Assert.That(DirectorRules.RaidChoice(99, 7, 4), Is.EqualTo(0));
            Assert.That(DirectorRules.RaidChoice(7, 0, 0), Is.Zero, "a zero range is a fixed answer, not a divide by zero");
        }

        /// <summary>
        /// The clock is seeded the first time the phase runs, from the state's own time, with the first-raid window.
        /// </summary>
        [Test]
        public void TheClockIsSeededOnceFromTheReferenceWindow()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            var r = ctx.Data.Raids;
            var expected = r.FirstMinS + DirectorRules.RaidChoice(st.Seed, 0, (int)r.FirstRangeS);
            Assert.That(st.Director.NextStart, Is.EqualTo(expected).Within(1e-9));
            Assert.That(st.Director.NextMinor, Is.EqualTo(r.GraceS).Within(1e-9));

            var before = st.Director.NextStart;
            RaidFixture.Run(ctx, st, 1, Clock());
            Assert.That(st.Director.NextStart, Is.EqualTo(before).Within(1e-9), "seeded once, not every tick");
        }

        /// <summary>
        /// The assault is announced exactly one warning window before it starts, and it starts at the time the
        /// clock had already rolled — announcing never moves the start.
        /// </summary>
        [Test]
        public void AMajorIsAnnouncedOneWarningWindowBeforeItStarts()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            var d = st.Director;
            var start = d.NextStart;

            st.T = start - ctx.Data.Raids.WarningS - 1;
            RaidFixture.Run(ctx, st, 1, Clock());
            Assert.That(d.Major, Is.Null, "one second too early");

            st.T = start - ctx.Data.Raids.WarningS;
            RaidFixture.Run(ctx, st, 1, Clock());
            Assert.That(d.Major, Is.Not.Null, "announced at nextStart - warningS");
            Assert.That(d.Major.StartsAt, Is.EqualTo(start).Within(1e-9));
            // GP-W4: the assault's length is its wave plan's, not RaidTuning.WindowS — the window no longer
            // describes anything the siege does.
            Assert.That(d.Major.EndsAt,
                Is.EqualTo(start + SiegePlan.Length(ctx, d.Major)).Within(1e-9));
            Assert.That(d.Major.Committed, Is.False, "announced is not started");
            Assert.That(d.Major.Total, Is.EqualTo((int)ctx.Data.Raids.Total));

            var notice = RaidFixture.Last<RaidNoticeEvent>(st);
            Assert.That(notice, Is.Not.Null);
            Assert.That(notice.Kind, Is.EqualTo(RaidNoticeKind.Announced));
            Assert.That(DirectorQueries.SecondsToMajor(st), Is.EqualTo(start - st.T).Within(1e-9),
                "the countdown the HUD reads is the warning window less the tick just run");
        }

        /// <summary>
        /// GP-W3. A minor raid is announced BEFORE it arrives, and what it says is what turns up: the brief's
        /// 30–45 s of preparation, a compass approach, and then six to ten bodies (<c>minorCountBase</c> plus a
        /// choice in <c>minorCountRange</c>) walking in on that same approach. The warning used to be a lie — the
        /// notice and the bodies landed in the same tick.
        /// </summary>
        [Test]
        public void AMinorRaidIsAnnouncedFirstAndArrivesWhereAndWhenItSaid()
        {
            var siege = SiegeTuning.Fallback;
            for (var seed = 1; seed <= 8; seed++)
            {
                var ctx = RaidFixture.Context();
                var st = RaidFixture.State(ctx, seed);
                st.T = st.Director.NextMinor;                 // the first opportunity, nothing else in the way
                RaidFixture.Run(ctx, st, 1, Clock());

                var m = st.Director.Minor;
                Assert.That(m, Is.Not.Null, "seed " + seed + ": the minor opportunity was taken");
                Assert.That(m.Spawned, Is.False, "seed " + seed + ": it is a warning, not yet a wave");
                Assert.That(EnemyQueries.GroupAlive(st, m.Id), Is.EqualTo(0), "seed " + seed + ": no bodies during the warning");
                var warn = m.StartsAt - st.T;
                Assert.That(warn, Is.InRange(siege.MinorWarningS - 1, siege.MinorWarningS + siege.MinorWarningRangeS),
                    "seed " + seed + ": the brief's 30-45 s of preparation");
                Assert.That(m.Heading, Is.Not.Empty.And.Not.EqualTo("\u00b7"), "seed " + seed + ": the approach is named");

                var said = RaidFixture.Last<RaidNoticeEvent>(st);
                Assert.That(said.Kind, Is.EqualTo(RaidNoticeKind.MinorRaid));
                Assert.That(said.Text, Does.Contain(m.Heading), "seed " + seed + ": the notice names the approach: " + said.Text);

                var warning = DirectorQueries.Warning(ctx, st);
                Assert.That(warning.SecondsLeft, Is.EqualTo(warn).Within(1e-9), "seed " + seed + ": the HUD shows the real countdown");
                Assert.That(warning.Label, Is.EqualTo("Raid"));

                // Run out the warning. The bodies arrive, and they arrive from the announced quarter.
                RaidFixture.Run(ctx, st, (int)System.Math.Ceiling(warn / RaidFixture.Dt) + 2, Clock());
                Assert.That(st.Director.Minor, Is.Not.Null, "seed " + seed + ": the raid survived its own warning");
                Assert.That(st.Director.Minor.Id, Is.EqualTo(m.Id), "seed " + seed + ": and it is the same raid, not a second one");
                Assert.That(st.Director.Minor.Spawned, Is.True, "seed " + seed + ": it arrived");
                var n = EnemyQueries.GroupAlive(st, m.Id);
                Assert.That(n, Is.InRange(6, 10), "seed " + seed + ": minorCountBase 6 + a 0..4 choice");
                Assert.That(st.Director.Minor.Scripted, Is.False, "a scheduled raid is not a scripted one");
                Assert.That(DirectorRules.HeadingsOf(ctx, st, new[] { st.Director.Minor.Origin }), Is.EqualTo(m.Heading),
                    "seed " + seed + ": the bodies came from the quarter the warning named");
            }
        }

        /// <summary>
        /// U-D-26: a deferral moves the wave, it never drops it. The clock goes out by exactly the amount asked
        /// for, an announced-but-uncommitted assault travels with it, and the notice says so.
        /// </summary>
        [Test]
        public void ADeferralMovesTheWaveAndNeverSkipsIt()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            var d = st.Director;
            st.T = d.NextStart - ctx.Data.Raids.WarningS;
            RaidFixture.Run(ctx, st, 1, Clock());
            Assert.That(d.Major, Is.Not.Null);
            var id = d.Major.Id;
            var until = d.NextStart + 600;

            Director.Defer(ctx, st, until, "Hold the assault: the opening encounter is still running.");

            Assert.That(d.NextStart, Is.EqualTo(until).Within(1e-9));
            Assert.That(d.Major, Is.Not.Null, "the wave is still there — deferred, not skipped");
            Assert.That(d.Major.Id, Is.EqualTo(id), "and it is the same wave");
            Assert.That(d.Major.StartsAt, Is.EqualTo(until).Within(1e-9), "it travels with the clock");
            Assert.That(d.Major.EndsAt,
                Is.EqualTo(until + SiegePlan.Length(ctx, d.Major)).Within(1e-9),
                "GP-W4: a deferral slides the whole plan, so its length is unchanged");
            var notice = RaidFixture.Last<RaidNoticeEvent>(st);
            Assert.That(notice.Kind, Is.EqualTo(RaidNoticeKind.Deferred));
            Assert.That(notice.Text, Does.Contain("opening encounter"));
            Assert.That(RaidFixture.Count<RaidNoticeEvent>(st), Is.EqualTo(2), "announced, then deferred — nothing skipped");

            // Never backwards: an earlier deadline is ignored, so two callers cannot pull the assault forward.
            Director.Defer(ctx, st, until - 300, "");
            Assert.That(d.NextStart, Is.EqualTo(until).Within(1e-9));
            Assert.That(RaidFixture.Count<RaidNoticeEvent>(st), Is.EqualTo(2), "and it stays quiet about it");
        }

        /// <summary>Two runs from the same seed produce the same world, tick for tick, body for body.</summary>
        [Test]
        public void TwoRunsFromTheSameSeedAreIdentical()
        {
            Assert.That(Digest(7), Is.EqualTo(Digest(7)));
            Assert.That(Digest(7), Is.Not.EqualTo(Digest(8)), "a different seed is a different campaign");
        }

        /// <summary>A whole minor raid, run to a fixed tick count, written out as its save text.</summary>
        private static string Digest(int seed)
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx, seed);
            st.T = st.Director.NextMinor;
            RaidFixture.Run(ctx, st, 600, RaidFixture.Phases());   // 30 s of walking, spitting and scheduling
            // The header carries a wall-clock stamp; the state is what has to be identical.
            var text = SaveSerializer.WriteText(st, ctx.Data);
            var at = text.IndexOf("\"state\"", StringComparison.Ordinal);
            return at < 0 ? text : text.Substring(at);
        }

        /// <summary>
        /// The bodies already sent stay sent: <c>majorSpawned</c> and the wave's remaining count are saved, so a
        /// load in the middle of an assault does not replay the wave from the top (the no-duplicate-waves rule).
        /// </summary>
        [Test]
        public void AWaveAlreadySpawnedDoesNotSpawnAgainAfterALoad()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            var d = st.Director;
            st.T = d.NextStart - ctx.Data.Raids.WarningS;
            RaidFixture.Run(ctx, st, 1, Clock());               // announce
            st.T = d.Major.StartsAt;
            RaidFixture.Run(ctx, st, 200, Clock());             // commit and send the opening bodies

            Assert.That(d.Major, Is.Not.Null);
            Assert.That(d.Major.Committed, Is.True);
            var sent = d.MajorSpawned;
            var alive = st.Enemies.Actors.Count;
            Assert.That(sent, Is.GreaterThan(1), "the wave is under way");
            Assert.That(d.Major.Remaining, Is.EqualTo(d.Major.Total - sent));

            var result = SaveSerializer.ReadText(SaveSerializer.WriteText(st, ctx.Data), ctx.Data);
            Assert.That(result.Ok, Is.True, result.Reason);
            var loaded = result.State;

            Assert.That(loaded.Director.MajorSpawned, Is.EqualTo(sent), "the count of bodies already sent survives");
            Assert.That(loaded.Director.Major, Is.Not.Null);
            Assert.That(loaded.Director.Major.Id, Is.EqualTo(d.Major.Id));
            Assert.That(loaded.Director.Major.Committed, Is.True, "a load does not re-announce an assault under way");
            Assert.That(loaded.Director.Major.Remaining, Is.EqualTo(d.Major.Remaining));
            Assert.That(loaded.Director.Major.NextSpawn, Is.EqualTo(d.Major.NextSpawn).Within(1e-9));
            Assert.That(loaded.Director.NextStart, Is.EqualTo(d.NextStart).Within(1e-9));
            Assert.That(loaded.Enemies.Actors.Count, Is.EqualTo(alive), "and no second copy of the wave arrives");

            new EnemyInitializer().Init(ctx, loaded);
            RaidFixture.Run(ctx, loaded, 1, Clock());
            Assert.That(loaded.Enemies.Actors.Count, Is.LessThanOrEqualTo(alive + 1), "the pacing resumes, it does not restart");
            Assert.That(loaded.Director.MajorSpawned, Is.LessThanOrEqualTo(sent + 1));
        }

        /// <summary>
        /// The editor trigger is refused outright unless the state says debug raids are allowed, and every answer it
        /// gives carries the label that keeps it out of player-facing text.
        /// </summary>
        [Test]
        public void TheDebugTriggerIsRefusedUnlessDebugIsAllowed()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            var handler = new DebugRaidHandler();

            Assert.That(st.Director.DebugAllowed, Is.False, "off by default in a normal game");
            Assert.That(handler.TryApply(ctx, st, new DebugRaidCommand(4, true), out var refused), Is.True,
                "the handler owns the command whatever it decides");
            Assert.That(refused.Accepted, Is.False);
            Assert.That(refused.Problem, Does.Contain(DebugRaidHandler.Label));
            Assert.That(st.Enemies.Actors.Count, Is.Zero, "and it staged nothing");
            Assert.That(st.Director.Minor, Is.Null);

            st.Director.DebugAllowed = true;
            Assert.That(handler.TryApply(ctx, st, new DebugRaidCommand(4, true), out var ok), Is.True);
            Assert.That(ok.Accepted, Is.True, ok.Problem);
            Assert.That(ok.Problem, Does.Contain(DebugRaidHandler.Label));
            Assert.That(st.Enemies.Actors.Count, Is.EqualTo(4));
            Assert.That(st.Director.Minor, Is.Not.Null);
            Assert.That(st.Director.Minor.Scripted, Is.True, "a staged group is never mistaken for a scheduled raid");
            Assert.That(st.Director.RaidsStarted, Is.Zero, "and it never advances the campaign's raid counter");

            // A second trigger while the first group is still on the map is refused, not stacked.
            Assert.That(handler.TryApply(ctx, st, new DebugRaidCommand(4, true), out var again), Is.True);
            Assert.That(again.Accepted, Is.False);
            Assert.That(st.Enemies.Actors.Count, Is.EqualTo(4));
        }

        private static List<ITickPhase> Clock() => new List<ITickPhase> { new DirectorPhase() };
    }
}
