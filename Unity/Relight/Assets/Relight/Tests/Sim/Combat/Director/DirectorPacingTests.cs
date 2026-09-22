using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Relight.Sim.Tests.Persistence;

namespace Relight.Sim.Tests.Combat
{
    /// <summary>
    /// REL-75 (E-21): raid pacing and fair timing (U-D-64 d, U-D-66 (5), U-D-68 b, U-D-69 e; U-P-17, U-P-22..25).
    ///
    /// <list type="bullet">
    /// <item>After a large raid's recovery comes a quiet spell with no warning and no small raid.</item>
    /// <item>Each cycle has one small raid, after the quiet spell; the next large-raid warning waits for it to end
    ///       and for the gap after it, then opens with its full length. The wait has caps, so a small raid that
    ///       cannot happen, or never ends, never stops the clock.</item>
    /// <item>A small raid waits while the engineer is down or fighting a camp, for a capped time; one on a far
    ///       target gets a longer warning; the campaign's first one cannot take the core below its floor.</item>
    /// </list>
    /// These run the director alone on the flat fixture map, so bodies stand where they are born and only the
    /// clock and the rules move. They pin the rules; they are not a playtest of the pacing.
    /// </summary>
    public sealed class DirectorPacingTests
    {
        private const double Dt = RaidFixture.Dt;

        private static List<ITickPhase> Clock() => new List<ITickPhase> { new DirectorPhase() };

        private static SimState Bench(SimContext ctx)
        {
            var st = RaidFixture.State(ctx);
            new HomeCoreInitializer().Init(ctx, st);
            return st;
        }

        /// <summary>Run the director until <paramref name="done"/> holds; false if <paramref name="seconds"/> ran out first.</summary>
        private static bool Until(SimContext ctx, SimState st, Func<bool> done, double seconds)
        {
            var end = st.T + seconds;
            while (!done())
            {
                if (st.T >= end) return false;
                RaidFixture.Run(ctx, st, 1, Clock());
            }
            return true;
        }

        /// <summary>A real large raid, warned, committed and started, then cleared by hand. Returns when it ended.</summary>
        private static double ClearedAssault(SimContext ctx, SimState st)
        {
            var d = st.Director;
            st.T = d.NextStart - ctx.Data.Raids.WarningS;
            RaidFixture.Run(ctx, st, 1, Clock());
            Assert.That(d.Major, Is.Not.Null, "the fixture map must be able to stage an assault");
            st.T = d.Major.StartsAt;
            RaidFixture.Run(ctx, st, 40, Clock());
            var a = d.Major;
            Assert.That(a.Committed, Is.True);
            a.Remaining = 0;
            st.Enemies.Actors.RemoveAll(e => e.Group == a.Id);
            RaidFixture.Run(ctx, st, 1, Clock());
            Assert.That(d.Major, Is.Null, "the assault is over");
            Assert.That(d.History[d.History.Count - 1].Outcome, Is.EqualTo((int)RaidOutcome.Cleared));
            return d.LastMajorEnd;
        }

        private static int Notices(SimState st, RaidNoticeKind kind, int from = 0)
        {
            var n = 0;
            for (var i = from; i < st.Events.Count; i++)
                if (st.Events[i] is RaidNoticeEvent r && r.Kind == kind) n++;
            return n;
        }

        // ---------------------------------------------------------------- the cycle (U-D-64 d)

        [Test]
        public void AfterTheRecoveryComesAQuietSpell_WithNoWarningAndNoSmallRaid()
        {
            var ctx = RaidFixture.Context();
            var st = Bench(ctx);
            var d = st.Director;
            var s = ctx.Data.Siege;
            var end = ClearedAssault(ctx, st);
            var seen = st.Events.Count;

            Assert.That(d.RecoveryUntil, Is.EqualTo(end + ctx.Data.Raids.RecoveryS).Within(1e-9));
            Assert.That(d.QuietUntil, Is.EqualTo(d.RecoveryUntil + s.QuietAfterMajorS).Within(1e-9));
            Assert.That(d.NextMinor, Is.GreaterThanOrEqualTo(d.QuietUntil), "the cycle's small raid is booked after it");

            Assert.That(Until(ctx, st, () => st.T > d.RecoveryUntil + 1, d.RecoveryUntil + 2 - st.T), Is.True);
            var v = DirectorQueries.Readout(ctx, st);
            Assert.That(v.Phase, Is.EqualTo("quiet"));
            Assert.That(v.PhaseSeconds, Is.EqualTo(d.QuietUntil - st.T).Within(1e-9));
            Assert.That(v.Pacing, Is.EqualTo("quiet spell after the last large raid"), "the Admin readout says why");
            Assert.That(DirectorQueries.Describe(v), Does.Contain("quiet spell"));

            Assert.That(Until(ctx, st, () => d.Major != null || d.Minor != null, d.QuietUntil - Dt - st.T), Is.False,
                "nothing is warned or sent through the recovery and the quiet spell");
            Assert.That(Notices(st, RaidNoticeKind.MinorRaid, seen) + Notices(st, RaidNoticeKind.Announced, seen), Is.Zero);
        }

        [Test]
        public void TheWarningWaitsForTheCyclesSmallRaidAndTheGapAfterIt_ThenOpensInFull()
        {
            var ctx = RaidFixture.Context();
            var st = Bench(ctx);
            var d = st.Director;
            var s = ctx.Data.Siege;
            var r = ctx.Data.Raids;
            ClearedAssault(ctx, st);
            var seen = st.Events.Count;

            Assert.That(Until(ctx, st, () => d.Minor != null, d.QuietUntil + r.MinorRangeS + 2 - st.T), Is.True,
                "the cycle's small raid comes after the quiet spell");
            Assert.That(st.T, Is.GreaterThanOrEqualTo(d.QuietUntil));
            var m = d.Minor;
            Assert.That(d.Major, Is.Null);
            Assert.That(DirectorPacing.WarningHold(ctx, st), Is.EqualTo("a small raid is on"));

            Assert.That(Until(ctx, st, () => m.Spawned, m.StartsAt + 2 - st.T), Is.True);
            Assert.That(d.CycleMinors, Is.EqualTo(1), "counted when it arrives");

            st.Enemies.Actors.RemoveAll(e => e.Group == m.Id);
            RaidFixture.Run(ctx, st, 1, Clock());
            Assert.That(d.Minor, Is.Null);
            Assert.That(d.LastMinorEnd, Is.EqualTo(st.T - Dt).Within(1e-9));
            Assert.That(DirectorPacing.WarningHold(ctx, st), Is.EqualTo("gap after the small raid"));

            Assert.That(Until(ctx, st, () => d.Major != null, s.MinorMajorGapS + r.IntervalRangeS + r.WarningS), Is.True);
            Assert.That(st.T, Is.GreaterThanOrEqualTo(d.LastMinorEnd + s.MinorMajorGapS), "never inside the gap");
            // Booked on the tick before this one; the warning is the whole WarningS to the tick.
            Assert.That(d.Major.StartsAt - (st.T - Dt), Is.EqualTo(r.WarningS).Within(Dt + 1e-6), "the full warning");
            Assert.That(Notices(st, RaidNoticeKind.Skipped, seen), Is.Zero, "a held warning is not a missed raid");
            Assert.That(Notices(st, RaidNoticeKind.MinorRaid, seen), Is.EqualTo(1), "one small raid in the cycle");
        }

        [Test]
        public void ASmallRaidThatCannotHappenHoldsTheWarningOnlyUntilItsCap()
        {
            var ctx = RaidFixture.Context();
            var st = Bench(ctx);
            var d = st.Director;
            var s = ctx.Data.Siege;
            var r = ctx.Data.Raids;
            ClearedAssault(ctx, st);
            var seen = st.Events.Count;

            // Camp residents far from the core, up to the living budget: the director can send nothing.
            for (var i = 0; i < r.LivingBudget; i++) RaidFixture.Guard(st, "drone", 4 + i % 40, 4);

            Assert.That(Until(ctx, st, () => d.Major != null, d.QuietUntil + s.MinorHoldCapS + r.IntervalRangeS - st.T), Is.True);
            Assert.That(Notices(st, RaidNoticeKind.MinorRaid, seen), Is.Zero);
            Assert.That(d.CycleMinors, Is.Zero);
            Assert.That(st.T, Is.EqualTo(d.QuietUntil + s.MinorHoldCapS).Within(2 * Dt), "opened at the cap (U-P-22)");
            Assert.That(d.Major.StartsAt - (st.T - Dt), Is.EqualTo(r.WarningS).Within(Dt + 1e-6), "the full warning");
            Assert.That(Notices(st, RaidNoticeKind.Skipped, seen), Is.Zero);
        }

        [Test]
        public void ASmallRaidThatNeverEndsHoldsTheWarningOnlyUntilItsCap()
        {
            var ctx = RaidFixture.Context();
            var st = Bench(ctx);
            var d = st.Director;
            var s = ctx.Data.Siege;
            ClearedAssault(ctx, st);

            Assert.That(Until(ctx, st, () => d.Minor != null && d.Minor.Spawned, d.QuietUntil + 300 - st.T), Is.True);
            var m = d.Minor;
            Assert.That(Until(ctx, st, () => d.Major != null, s.MinorHoldCapS + 5), Is.True);
            Assert.That(st.T, Is.EqualTo(m.StartsAt + s.MinorHoldCapS).Within(2 * Dt));
            Assert.That(d.Minor, Is.SameAs(m), "its bodies are still on the map");
        }

        [Test]
        public void BeforeTheFirstLargeRaidThereIsNoCycle_AndThePacingHoldsNothing()
        {
            var ctx = RaidFixture.Context();
            var st = Bench(ctx);
            Assert.That(DirectorPacing.InCycle(st), Is.False);
            Assert.That(DirectorPacing.WarningHold(ctx, st), Is.Empty);
            Assert.That(DirectorPacing.StrongholdFight(ctx, st), Is.False, "no strongholds exist yet (U-D-69 e)");
        }

        // ---------------------------------------------------------------- fair timing (U-D-66 (5))

        [Test]
        public void ASmallRaidWaitsWhileTheEngineerIsDown_ButNoLongerThanItsCap()
        {
            var ctx = RaidFixture.Context();
            var st = Bench(ctx);
            var d = st.Director;
            var s = ctx.Data.Siege;
            st.T = d.NextMinor;
            var t0 = st.T;
            st.Engineer.Down = st.T;

            RaidFixture.Run(ctx, st, 1, Clock());
            Assert.That(d.Minor, Is.Null);
            Assert.That(d.MinorDelayedSince, Is.EqualTo(t0).Within(1e-9));
            Assert.That(Until(ctx, st, () => d.Minor != null, s.MinorDelayCapS + 2), Is.True);
            Assert.That(st.T - t0, Is.EqualTo(s.MinorDelayCapS + 0.5).Within(0.5 + 2 * Dt), "it starts at the cap (U-P-23)");
            Assert.That(d.MinorDelayedSince, Is.EqualTo(-1));
        }

        [Test]
        public void ASmallRaidHeldForADownedEngineerStartsOnceTheyAreUp()
        {
            var ctx = RaidFixture.Context();
            var st = Bench(ctx);
            var d = st.Director;
            st.T = d.NextMinor;
            st.Engineer.Down = st.T;

            Assert.That(Until(ctx, st, () => d.Minor != null, 30), Is.False);
            st.Engineer.Down = -1;
            Assert.That(Until(ctx, st, () => d.Minor != null, 1 + 2 * Dt), Is.True);
            Assert.That(d.MinorDelayedSince, Is.EqualTo(-1));
        }

        [Test]
        public void ACampFightHoldsASmallRaid_AndACampThatIsNotChasingDoesNot()
        {
            var ctx = RaidFixture.Context();
            var st = Bench(ctx);
            var d = st.Director;
            var e = RaidFixture.Guard(st, "drone", 10, 10);
            Assert.That(DirectorPacing.CampFight(st), Is.False, "a sleeping camp is not a fight");
            e.OnPlayer = true;
            Assert.That(DirectorPacing.CampFight(st), Is.True);
            e.Withdrawing = true;
            Assert.That(DirectorPacing.CampFight(st), Is.False, "a resident breaking off is not a fight");
            e.Withdrawing = false;

            st.T = d.NextMinor;
            Assert.That(Until(ctx, st, () => d.Minor != null, 20), Is.False);
            e.OnPlayer = false;
            Assert.That(Until(ctx, st, () => d.Minor != null, 1 + 2 * Dt), Is.True);
        }

        [Test]
        public void ASmallRaidOnAFarTargetGetsTheLongerWarning()
        {
            // A map wide enough for the engineer to stand further than FarTargetTiles from the core.
            var ctx = RaidFixture.Context(RaidFixture.Map(size: 400));
            var s = ctx.Data.Siege;

            double Warning(double x, double y, bool far)
            {
                var st = Bench(ctx);
                st.Engineer.Pos = new Vec2(x, y);
                Assert.That(DirectorPacing.Far(ctx, st), Is.EqualTo(far));
                st.T = st.Director.NextMinor;
                var t0 = st.T;
                RaidFixture.Run(ctx, st, 1, Clock());
                Assert.That(st.Director.Minor, Is.Not.Null);
                return st.Director.Minor.StartsAt - t0;
            }

            var near = Warning(RaidFixture.CoreX + 4, RaidFixture.CoreY + 4, false);
            var far = Warning(300, 300, true);
            Assert.That(near, Is.InRange(s.MinorWarningS, s.MinorWarningS + s.MinorWarningRangeS));
            Assert.That(far - near, Is.EqualTo(s.FarWarningExtraS).Within(1e-9), "U-P-24");
        }

        // ---------------------------------------------------------------- the first raid's floor (U-D-68 b)

        [Test]
        public void TheFirstSmallRaidCannotTakeTheCoreBelowItsFloor_AndBreaksOffThere()
        {
            var ctx = RaidFixture.Context();
            var st = Bench(ctx);
            var d = st.Director;
            var s = ctx.Data.Siege;
            Assert.That(EnemyCoreHook.Hp(ctx, st, out var hp, out var max), Is.True);
            Assert.That(hp, Is.EqualTo(max));

            st.T = d.NextMinor;
            RaidFixture.Run(ctx, st, 1, Clock());
            var m = d.Minor;
            Assert.That(m, Is.Not.Null);
            Assert.That(m.Floor, Is.EqualTo(max * s.FirstMinorCoreFloor).Within(1e-9), "U-P-25");
            Assert.That(Until(ctx, st, () => m.Spawned, m.StartsAt + 2 - st.T), Is.True);
            var seen = st.Events.Count;

            EnemyCoreHook.Damage(ctx, st, max * 0.25);
            Assert.That(m.Retreat, Is.False, "above the floor nothing changes");
            EnemyCoreHook.Damage(ctx, st, 1e9);
            EnemyCoreHook.Hp(ctx, st, out hp, out _);
            Assert.That(hp, Is.EqualTo(m.Floor).Within(1e-9), "the core held");
            Assert.That(m.Retreat, Is.True, "and the raid breaks off");
            Assert.That(d.Notice, Does.StartWith("The core held."));
            Assert.That(Notices(st, RaidNoticeKind.CoreHeld, seen), Is.EqualTo(1));

            EnemyCoreHook.Damage(ctx, st, 50);
            EnemyCoreHook.Hp(ctx, st, out hp, out _);
            Assert.That(hp, Is.EqualTo(m.Floor).Within(1e-9), "the floor holds while its bodies walk off");
            Assert.That(Notices(st, RaidNoticeKind.CoreHeld, seen), Is.EqualTo(1), "and it is said once");

            st.Enemies.Actors.RemoveAll(e => e.Group == m.Id);
            RaidFixture.Run(ctx, st, 1, Clock());
            Assert.That(d.Minor, Is.Null);
            var ended = RaidFixture.Last<RaidEndedEvent>(st);
            Assert.That(ended.RaidId, Is.EqualTo(m.Id));
            Assert.That(ended.Outcome, Is.EqualTo((int)RaidOutcome.BrokeOff), "broke off, not repelled and not lost");
            Assert.That(EnemyCoreHook.Down(ctx, st), Is.False);
        }

        [Test]
        public void ALaterSmallRaidHasNoFloor()
        {
            var ctx = RaidFixture.Context();
            var st = Bench(ctx);
            st.Director.RaidsStarted = 1;
            st.T = st.Director.NextMinor;
            RaidFixture.Run(ctx, st, 1, Clock());
            Assert.That(st.Director.Minor, Is.Not.Null);
            Assert.That(st.Director.Minor.Floor, Is.Zero);
        }

        [Test]
        public void TheFloorNeverSoftensALargeRaid()
        {
            var ctx = RaidFixture.Context();
            var st = Bench(ctx);
            var d = st.Director;
            d.Minor = new MinorRaid { Id = 5, Spawned = true, StartsAt = st.T, Floor = 150 };
            Assert.That(DirectorPacing.CoreFloorHp(st), Is.EqualTo(150));
            d.Major = new MajorRaid { Id = 6, Committed = true };
            Assert.That(DirectorPacing.CoreFloorHp(st), Is.Zero);
            EnemyCoreHook.Damage(ctx, st, 1e9);
            Assert.That(EnemyCoreHook.Down(ctx, st), Is.True);
            Assert.That(d.Minor.Retreat, Is.False, "the core did not hold: nothing breaks off on its account");
        }

        // ---------------------------------------------------------------- the Admin pull-in (E-19)

        [Test]
        public void TheAdminPullInWaivesThePacingHolds_ButNotALiveSmallRaid()
        {
            var ctx = RaidFixture.Context();
            var st = Bench(ctx);
            var d = st.Director;
            var r = ctx.Data.Raids;
            ClearedAssault(ctx, st);
            Assert.That(DirectorPacing.WarningHold(ctx, st), Is.Not.Empty, "the quiet spell is on");

            d.Minor = new MinorRaid { Id = 99, Spawned = true, StartsAt = st.T };
            Assert.That(Director.PullIn(ctx, st, st.T + r.WarningS), Is.EqualTo("A small raid is still on the map. Clear enemies first."));
            d.Minor = null;

            Assert.That(Director.PullIn(ctx, st, st.T + r.WarningS - 1e-6), Is.Empty);
            Assert.That(DirectorPacing.WarningHold(ctx, st), Is.Empty);
            RaidFixture.Run(ctx, st, 1, Clock());
            Assert.That(d.Major, Is.Not.Null, "warned on the next tick");
        }

        // ---------------------------------------------------------------- saves and data

        [Test]
        public void ThePacingStateIsSaved()
        {
            var ctx = RaidFixture.Context();
            var st = Bench(ctx);
            var d = st.Director;
            d.QuietUntil = 2400.5;
            d.CycleMinors = 1;
            d.LastMinorEnd = 1990.25;
            d.MinorDelayedSince = 2001;
            d.Minor = new MinorRaid { Id = 8, Spawned = false, StartsAt = 2050, Owed = 4, Heading = "north", Floor = 150 };

            var load = SaveSerializer.ReadText(SaveSerializer.WriteText(st, ctx.Data), ctx.Data);
            Assert.That(load.Ok, Is.True, load.Reason);
            var e = load.State.Director;
            Assert.That(e.QuietUntil, Is.EqualTo(2400.5));
            Assert.That(e.CycleMinors, Is.EqualTo(1));
            Assert.That(e.LastMinorEnd, Is.EqualTo(1990.25));
            Assert.That(e.MinorDelayedSince, Is.EqualTo(2001));
            Assert.That(e.Minor.Floor, Is.EqualTo(150));
        }

        /// <summary>
        /// A version-11 file has none of the pacing members. It loads with values that hold nothing back, and a
        /// small raid already in it is given no floor: the rule did not exist when it was announced.
        /// </summary>
        [Test]
        public void AVersionElevenFileLoads_OwingNoQuietSpellAndNoFloor()
        {
            var ctx = RaidFixture.Context();
            var st = Bench(ctx);
            st.Director.Minor = new MinorRaid { Id = 8, Spawned = true, StartsAt = 900, Heading = "north" };

            var state = PersistenceFixture.Canonical(st);
            var old = state;
            foreach (var name in new[] { "quietUntil", "cycleMinors", "lastMinorEnd", "minorDelayedSince", "floor" })
            {
                old = Regex.Replace(old, "\"" + name + "\":[^,}]+,|,\"" + name + "\":[^,}]+", "");
                Assert.That(old, Does.Not.Contain("\"" + name + "\""), name);
            }
            var doc = SaveSerializer.WriteText(st, ctx.Data);
            doc = PersistenceFixture.Retarget(doc, "state", old);
            doc = PersistenceFixture.Retarget(doc, "version", "11");
            doc = PersistenceFixture.Retarget(doc, "hash", CanonicalJsonWriter.QuoteString(StateHash.Of(old)));

            var load = SaveSerializer.ReadText(doc, ctx.Data);
            Assert.That(load.Ok, Is.True, load.Reason);
            Assert.That(load.Header.Version, Is.EqualTo(11));
            Assert.That(load.Upgraded, Does.Contain("version 11"));
            var d = load.State.Director;
            Assert.That(d.QuietUntil, Is.Zero);
            Assert.That(d.CycleMinors, Is.Zero);
            Assert.That(d.LastMinorEnd, Is.EqualTo(-1));
            Assert.That(d.MinorDelayedSince, Is.EqualTo(-1));
            Assert.That(d.Minor, Is.Not.Null);
            Assert.That(d.Minor.Floor, Is.Zero);
            Assert.That(d.Minor.Heading, Is.EqualTo("north"));
        }

        [Test]
        public void ThePacingNumbersAreTheRecordedOnes()
        {
            foreach (var s in new[] { SiegeTuning.Fallback, ReferenceData.Create().Siege })
            {
                Assert.That(s.QuietAfterMajorS, Is.EqualTo(480), "U-P-17");
                Assert.That(s.MinorsPerCycle, Is.EqualTo(1), "U-D-64 d");
                Assert.That(s.MinorHoldCapS, Is.EqualTo(600), "U-P-22");
                Assert.That(s.MinorDelayCapS, Is.EqualTo(180), "U-P-23");
                Assert.That(s.FarTargetTiles, Is.EqualTo(150), "U-P-24");
                Assert.That(s.FarWarningExtraS, Is.EqualTo(30), "U-P-24");
                Assert.That(s.FirstMinorCoreFloor, Is.EqualTo(0.5), "U-P-25");
            }
        }
    }
}
