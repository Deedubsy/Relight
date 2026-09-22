using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Relight.Sim.Tests.Combat
{
    /// <summary>
    /// REL-84: the threat numbers the director and the guards used to carry as literals now live on
    /// <see cref="SiegeTuning"/>. The move was meant to change nothing, so this file pins both halves of that claim.
    ///
    /// <list type="bullet">
    /// <item>THE TABLE — every moved field still reads as the literal it replaced, both on
    ///       <see cref="SiegeTuning.Fallback"/> and through the data a real context hands the sim. If a default is
    ///       ever edited by accident, the table says which number moved and what it used to be.</item>
    /// <item>THE READS — two of them are followed through a real context, with the tuning changed and nothing else,
    ///       to show the sim genuinely asks the data rather than a constant that happens to agree with it:
    ///       <see cref="SiegeTuning.CoreThreatTiles"/> decides whether a body blocks the core repair, and
    ///       <see cref="SiegeTuning.MinorMajorGapS"/> decides whether a small raid may be announced in front of a
    ///       large one.</item>
    /// </list>
    ///
    /// These are refactor guards, not balance opinions. Any deliberate retune edits the expected column here and
    /// says so in DECISIONS; the behavioural pair keeps working whatever the numbers become, because it compares a
    /// context against a changed copy of itself.
    /// </summary>
    public sealed class SiegeTuningSweepTests
    {
        /// <summary>The same balance data with one edited siege row, and the same world around it.</summary>
        private static SimContext Retuned(SimContext plain, Func<SiegeTuning, SiegeTuning> change)
        {
            var d = plain.Data;
            var data = new GameData(d.Items, d.Machines, d.Recipes, d.Engineer, d.World, d.Weapons, d.Enemies,
                d.Ammunition, d.Turrets, d.Power, d.Time, d.Raids, d.Opening, d.Stake, d.Defence, change(d.Siege));
            return new SimContext(data, plain.Geometry, plain.Tiles, plain.Threat, plain.Sites, plain.MapId);
        }

        // ---------------------------------------------------------------- the table

        /// <summary>Field name, what it reads now, and the literal it was moved from.</summary>
        private static (string Name, double Now, double Was)[] Moved(SiegeTuning s) =>
            new (string, double, double)[]
            {
                // DirectorRules
                ("CoreThreatTiles",       s.CoreThreatTiles,       12),
                ("MajorOverrunS",         s.MajorOverrunS,         300),
                ("WithdrawPurgeS",        s.WithdrawPurgeS,        120),
                ("EntryNearSteps",        s.EntryNearSteps,        8),
                ("EntryFarSteps",         s.EntryFarSteps,         76),
                ("SafeFromEngineerTiles", s.SafeFromEngineerTiles, 28),
                ("StagingIdealSteps",     s.StagingIdealSteps,     24),
                ("ApproachIdealSteps",    s.ApproachIdealSteps,    42),
                // DirectorPhase
                ("MinorMajorGapS",        s.MinorMajorGapS,        120),
                // EnemyPhase and Enemies
                ("GuardSleepTiles",       s.GuardSleepTiles,       60),
                ("GuardPatrolRadiusTiles", s.GuardPatrolRadiusTiles, 2),
                ("GuardPatrolSpeedMul",   s.GuardPatrolSpeedMul,   0.35),
                ("MemoryS",               s.MemoryS,               6),
            };

        private static void Check(SiegeTuning s, string where)
        {
            foreach (var (name, now, was) in Moved(s))
                Assert.That(now, Is.EqualTo(was).Within(1e-9),
                    where + ": SiegeTuning." + name + " must still be the literal REL-84 moved onto it");
        }

        [Test]
        public void EveryNumberMovedOntoSiegeTuningStillCarriesTheLiteralItReplaced()
        {
            Check(SiegeTuning.Fallback, "the fallback row");
            Check(RaidFixture.Context().Data.Siege, "the data a context hands the sim");
        }

        [Test]
        public void TheMovedNumbersAreConsistentWithEachOther()
        {
            var s = SiegeTuning.Fallback;
            Assert.That(s.EntryFarSteps, Is.GreaterThan(s.EntryNearSteps), "the entry band has to have width");
            Assert.That(s.StagingIdealSteps, Is.InRange(s.EntryNearSteps, s.EntryFarSteps),
                "an origin aims at a distance inside the band it is chosen from");
            Assert.That(s.ApproachIdealSteps, Is.InRange(s.EntryNearSteps, s.EntryFarSteps),
                "an approach aims at a distance inside the band it is chosen from");
            Assert.That(RaidField.ReachOf(RaidFixture.Context()),
                Is.EqualTo(s.EntryFarSteps + RaidField.StagingMargin),
                "the search box is the far band plus the staging margin, and follows the tuning");
        }

        // ---------------------------------------------------------------- read one: the core repair ring

        [Test]
        public void WideningCoreThreatTilesMakesTheSameBodyBlockTheRepair()
        {
            var plain = RaidFixture.Context();
            var wide = Retuned(plain, s => s with { CoreThreatTiles = s.CoreThreatTiles * 2 });
            var st = RaidFixture.State(plain);
            new HomeCoreInitializer().Init(plain, st);

            // One body, parked between the two rings: outside 12 tiles, inside 24. Nothing about the world moves.
            var (cx, cy, cw, _) = HomeQueries.CoreRect(st);
            var gap = (plain.Data.Siege.CoreThreatTiles + wide.Data.Siege.CoreThreatTiles) / 2;
            RaidFixture.Body(st, "skitter", cx + cw + gap, cy + 0.5, EnemyLayer.Major, 5);

            Assert.That(Home.AttackersNearby(plain, st), Is.False, "outside the shipped ring");
            Assert.That(Home.AttackersNearby(wide, st), Is.True, "inside the widened one — the rule reads the data");
        }

        // ---------------------------------------------------------------- read two: the small-raid guard band

        /// <summary>
        /// Puts the clock exactly one second clear of the SHIPPED guard band and takes one director tick, so the
        /// only thing that can decide the answer is the band <paramref name="ctx"/> reads.
        /// </summary>
        private static MinorRaid AnnounceAttempt(SimContext ctx)
        {
            var phases = new List<ITickPhase> { new DirectorPhase() };
            var st = RaidFixture.State(ctx);
            new HomeCoreInitializer().Init(ctx, st);
            RaidFixture.Run(ctx, st, 1, phases);          // the first tick seeds the director

            var d = st.Director;
            var warn = ctx.Data.Siege.MinorWarningS + DirectorRules.RaidChoice(
                st.Seed, d.RaidsStarted + 1013, (int)ctx.Data.Siege.MinorWarningRangeS);
            d.NextMinor = st.T;                            // a small raid is due now
            d.NextStart = st.T + warn + ctx.Data.Raids.WarningS + SiegeTuning.Fallback.MinorMajorGapS + 1;
            RaidFixture.Run(ctx, st, 1, phases);
            return d.Minor;
        }

        [Test]
        public void WideningMinorMajorGapSMovesTheBandASmallRaidHasToClear()
        {
            var plain = RaidFixture.Context();
            var wide = Retuned(plain, s => s with { MinorMajorGapS = s.MinorMajorGapS * 5 });

            Assert.That(AnnounceAttempt(plain), Is.Not.Null,
                "one second clear of the shipped band, so the small raid is announced");
            Assert.That(AnnounceAttempt(wide), Is.Null,
                "the same clock is well inside a five-times wider band — the rule reads the data");
        }
    }
}
