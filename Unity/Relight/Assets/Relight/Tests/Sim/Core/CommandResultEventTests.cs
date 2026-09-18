using System.Collections.Generic;
using NUnit.Framework;

namespace Relight.Sim.Tests.Core
{
    /// <summary>
    /// C-13. A command handed to <see cref="Simulation.Queue"/> — which is every command player input submits,
    /// through <c>SimHost.Submit</c> — has no caller left to return its <see cref="CommandResult"/> to, so what
    /// the sim decided used to be discarded. The flush now says it as a <see cref="CommandResultEvent"/>, in the
    /// sim's own words, and only when there are words: the held intents are submitted every frame and accept
    /// silently, so a blanket event would bury the refusals under thousands of empty ones.
    /// </summary>
    [TestFixture]
    public sealed class CommandResultEventTests
    {
        private static readonly List<SimEvent> Drained = new List<SimEvent>();

        private static List<CommandResultEvent> Results(Simulation sim)
        {
            Drained.Clear();
            sim.DrainEvents(Drained);
            var found = new List<CommandResultEvent>();
            for (var i = 0; i < Drained.Count; i++)
                if (Drained[i] is CommandResultEvent c) found.Add(c);
            return found;
        }

        /// <summary>A placement the engineer cannot reach: the far corner of the 32x32 flat map, spawn is (16,16).</summary>
        private static PlaceMachineCommand OutOfReach() => new PlaceMachineCommand("chest", 30, 30, Dir.N);

        [Test]
        public void ARefusedQueuedCommandSaysExactlyWhatApplyWouldHaveReturned()
        {
            var queued = Simulation.NewGame(CoreTest.Context(), 5);
            var applied = Simulation.NewGame(CoreTest.Context(), 5);

            // The same command down both paths: one queued and flushed by the tick, one applied synchronously.
            var direct = applied.Apply(OutOfReach());
            Assert.That(direct.Accepted, Is.False, "the fixture command is meant to be refused: " + direct.Problem);
            Assert.That(direct.Problem, Is.Not.Empty);

            queued.Queue(OutOfReach());
            queued.Tick();

            var results = Results(queued);
            Assert.That(results.Count, Is.EqualTo(1), "exactly one command result was announced");
            Assert.That(results[0].Command, Is.EqualTo("PlaceMachineCommand"), "the type name without its namespace");
            Assert.That(results[0].Accepted, Is.False);
            Assert.That(results[0].Text, Is.EqualTo(direct.Problem), "the sim's own wording, not a second vocabulary");
            Assert.That(results[0].T, Is.EqualTo(0).Within(1e-9), "stamped with the time the flush ran, before the tick");
        }

        [Test]
        public void AnAcceptedCommandWithNothingToSaySaysNothing()
        {
            var sim = Simulation.NewGame(CoreTest.Context(), 5);

            // Walk is submitted every frame the keys change and always accepts silently: one event per frame here
            // would flood the list the host drains and the HUD reads.
            sim.Queue(new WalkCommand(1, 0));
            sim.Queue(new SprintCommand(true));
            sim.Tick();

            Assert.That(Results(sim), Is.Empty);
        }

        [Test]
        public void RepeatedRefusalsAreAnnouncedOncePerSubmission()
        {
            var sim = Simulation.NewGame(CoreTest.Context(), 5);
            sim.Queue(OutOfReach());
            sim.Queue(OutOfReach());
            sim.Tick();

            var results = Results(sim);
            Assert.That(results.Count, Is.EqualTo(2), "the flush reports each queued command; the HUD does the keying");
        }

        /// <summary>
        /// Events are drained, never saved and never hashed (TECHNICAL_ARCHITECTURE.md §4.3), so announcing a
        /// refusal cannot move the state: two runs that refuse the same command stay bit-identical.
        /// </summary>
        [Test]
        public void AnnouncingARefusalDoesNotChangeTheState()
        {
            var a = Simulation.NewGame(CoreTest.Context(), 9);
            var b = Simulation.NewGame(CoreTest.Context(), 9);

            for (var i = 0; i < 20; i++)
            {
                a.Queue(OutOfReach());
                a.Tick();
                b.Tick();
                Drained.Clear();
                a.DrainEvents(Drained);
                b.DrainEvents(Drained);
            }

            Assert.That(a.Hash(), Is.EqualTo(b.Hash()), "a refused command changed the state hash");
        }
    }
}
