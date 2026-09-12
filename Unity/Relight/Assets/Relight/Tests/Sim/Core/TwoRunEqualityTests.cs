using NUnit.Framework;

namespace Relight.Sim.Tests.Core
{
    /// <summary>
    /// Contract H2 (TECHNICAL_ARCHITECTURE.md §10.4, §10.6 check 2): two runs of the same seed, stepped by the same
    /// real-second sequence in one process, reach the same tick and the same Unity-canonical hash. This is a
    /// self-consistency claim about the C# sim; it says nothing about the TypeScript reference's hash (U-M-13).
    /// </summary>
    public sealed class TwoRunEqualityTests
    {
        private static readonly double[] Steps = { 0.05, 0.13, 1.0, 0.007 };

        private static Simulation Run(int seed, int ticks)
        {
            var sim = Simulation.NewGame(CoreTest.Context(), seed);
            var i = 0;
            while (sim.State.Tick < ticks)
            {
                sim.Advance(Steps[i % Steps.Length]);
                i++;
            }
            return sim;
        }

        [Test]
        public void SameSeedAndStepSequenceGivesTheSameTickAndHash()
        {
            var a = Run(7, 400);
            var b = Run(7, 400);
            Assert.AreEqual(a.State.Tick, b.State.Tick, "tick");
            Assert.GreaterOrEqual(a.State.Tick, 400);
            Assert.AreEqual(a.Hash(), b.Hash(), "state hash after the same run");
            Assert.AreEqual(a.State.T, b.State.T, 0.0, "sim seconds");
            Assert.AreEqual(a.State.Rng, b.State.Rng, "rng word");
        }

        [Test]
        public void HashIsStableWhenNothingHappens()
        {
            var sim = Run(7, 100);
            var before = sim.Hash();
            Assert.AreEqual(before, sim.Hash(), "hashing twice must not change anything");
            sim.Advance(0.0);
            Assert.AreEqual(before, sim.Hash(), "an advance that runs no tick changes no state");
        }

        [Test]
        public void HashMovesWhenTheStateMoves()
        {
            var sim = Run(7, 100);
            var before = sim.Hash();
            sim.Tick();
            Assert.AreNotEqual(before, sim.Hash(), "one more tick must change the hash");
        }

        [Test]
        public void DifferentSeedsGiveDifferentRngWordsAndHashes()
        {
            var a = Simulation.NewGame(CoreTest.Context(), 1);
            var b = Simulation.NewGame(CoreTest.Context(), 2);
            Assert.AreNotEqual(a.State.Rng, b.State.Rng, "seeded rng word");
            Assert.AreEqual(Prng.Seed(1), a.State.Rng);
            Assert.AreEqual(Prng.Seed(2), b.State.Rng);
            Assert.AreNotEqual(a.Hash(), b.Hash(), "the seed is part of the hashed state");
        }

        [Test]
        public void NewGameSetsTheHeaderFields()
        {
            var sim = Simulation.NewGame(CoreTest.Context(), 11);
            Assert.AreEqual(11, sim.State.Seed);
            Assert.AreEqual(0, sim.State.Tick);
            Assert.AreEqual(0.0, sim.State.T, 0.0);
            Assert.AreEqual(SimVersion.Ruleset, sim.State.Ruleset);
            Assert.AreEqual(SimVersion.SchemaVersion, sim.State.Version);
        }

        [Test]
        public void TheSecondPhaseFiresOncePerTwentyTicksAndNeverBeforeTheFirst()
        {
            var threat = new ScriptedThreatLayer();
            var sim = Simulation.NewGame(CoreTest.Context(threat), 3);
            for (var k = 0; k < 19; k++) sim.Tick();
            Assert.AreEqual(0, threat.SecondCalls, "the reference runs its per-second step after the 20th tick, not before the 1st");
            sim.Tick();                                   // 20th tick; the phase fires at the top of the 21st
            Assert.AreEqual(0, threat.SecondCalls);
            sim.Tick();
            Assert.AreEqual(1, threat.SecondCalls);
            for (var k = 0; k < 20; k++) sim.Tick();
            Assert.AreEqual(2, threat.SecondCalls);
        }

        [Test]
        public void DangerTelemetryCountsSecondsNotTicks()
        {
            var threat = new ScriptedThreatLayer { Danger = true, Shot = false };
            var sim = Simulation.NewGame(CoreTest.Context(threat), 3);
            sim.Advance(5.0);                             // 100 ticks -> 4 completed second boundaries observed so far
            Assert.AreEqual(threat.SecondCalls, sim.State.Engineer.DangerSeconds);
            Assert.AreEqual(0, sim.State.Engineer.DangerShotSeconds);

            threat.Shot = true;
            var before = sim.State.Engineer.DangerShotSeconds;
            sim.Advance(2.0);
            Assert.Greater(sim.State.Engineer.DangerShotSeconds, before);
            Assert.AreEqual(sim.State.Engineer.DangerSeconds, threat.SecondCalls);
        }

        [Test]
        public void UnknownCommandsAreRefusedNotIgnored()
        {
            var sim = Simulation.NewGame(CoreTest.Context(), 3);
            var r = sim.Apply(new CoreUnownedCommand());
            Assert.IsFalse(r.Accepted);
            Assert.AreEqual(CommandDispatcher.UnknownCommand, r.Problem);
            Assert.IsFalse(sim.Apply(null).Accepted);
        }

        [Test]
        public void QueuedCommandsAreFlushedBeforeTheFirstTickOfTheNextAdvance()
        {
            var sim = Simulation.NewGame(CoreTest.Context(), 3);
            sim.Queue(new CoreUnownedCommand());
            sim.Queue(new CoreUnownedCommand());
            Assert.AreEqual(2, sim.PendingCount);
            sim.Advance(0.001);                           // runs no tick, still flushes
            Assert.AreEqual(0, sim.PendingCount);
        }

        [Test]
        public void EventsDrainAndClear()
        {
            var sim = Simulation.NewGame(CoreTest.Context(), 3);
            sim.State.Events.Add(new EngineerUpEvent(sim.State.T));
            var into = new System.Collections.Generic.List<SimEvent>();
            Assert.AreEqual(1, sim.DrainEvents(into));
            Assert.AreEqual(1, into.Count);
            Assert.AreEqual(0, sim.State.Events.Count);
            Assert.AreEqual(0, sim.DrainEvents(into));
        }
    }

    /// <summary>A command no handler owns, used to check the dispatcher's refusal path.</summary>
    internal sealed record CoreUnownedCommand : Command;
}
