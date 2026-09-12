using NUnit.Framework;

namespace Relight.Sim.Tests.Core
{
    /// <summary>
    /// The tick driver's accumulator (reference flow.ts <c>advanceFlow</c>, lines 1104–1120;
    /// TECHNICAL_ARCHITECTURE.md §10.1). The accumulator counts <b>ticks</b>, as the reference's <c>st.acc</c> does,
    /// and the <c>+1e-6</c> epsilon exists so a frame that is a hair short of a tick still runs it.
    /// </summary>
    public sealed class AccumulatorTests
    {
        private static Simulation Sim() => Simulation.NewGame(CoreTest.Context(), 1);

        [Test]
        public void TwentyHertzConstants()
        {
            Assert.AreEqual(20, Simulation.TicksPerSecond);
            Assert.AreEqual(0.05, Simulation.TickSeconds, 0.0);
            Assert.AreEqual(5120, Simulation.MaxTicksPerCall, "256 block ticks' worth of tile ticks");
        }

        [Test]
        public void JustShortOfATickRunsNothingAndTheRemainderCarries()
        {
            var sim = Sim();
            Assert.AreEqual(0, sim.Advance(0.0499), "0.0499 s is 0.998 ticks");
            Assert.AreEqual(0, sim.State.Tick);
            // 0.0001 s is 0.002 ticks; 0.998 + 0.002 reaches 1.0 and the epsilon guarantees it is not lost.
            Assert.AreEqual(1, sim.Advance(0.0001));
            Assert.AreEqual(1, sim.State.Tick);
            Assert.AreEqual(0.0, sim.Accumulator, 1e-9);
        }

        [Test]
        public void ASecondIsTwentyTicksAndAdvancesSimTime()
        {
            var sim = Sim();
            Assert.AreEqual(20, sim.Advance(1.0));
            Assert.AreEqual(20, sim.State.Tick);
            Assert.AreEqual(1.0, sim.State.T, 1e-9);
        }

        [Test]
        public void RemainderCarriesAcrossCalls()
        {
            var sim = Sim();
            // 0.07 s = 1.4 ticks -> 1 tick, 0.4 carried; +0.07 -> 1.8 -> 1 tick, 0.8 carried; +0.07 -> 1.8 ...
            Assert.AreEqual(1, sim.Advance(0.07));
            Assert.AreEqual(0.4, sim.Accumulator, 1e-9);
            Assert.AreEqual(1, sim.Advance(0.07));
            Assert.AreEqual(0.8, sim.Accumulator, 1e-9);
            Assert.AreEqual(2, sim.Advance(0.07));
            Assert.AreEqual(0.2, sim.Accumulator, 1e-9);
            Assert.AreEqual(4, sim.State.Tick);
        }

        [Test]
        public void OverTheCapIsClampedAndTheBacklogIsDiscarded()
        {
            var sim = Sim();
            // 100 s would be 2,000 ticks; an explicit cap of 32 runs 32 and drops the rest, exactly as the
            // reference does (`if (n > cap) { n = cap; st.acc = 0; }`) rather than fast-forwarding later.
            Assert.AreEqual(32, sim.Advance(100, 32));
            Assert.AreEqual(32, sim.State.Tick);
            Assert.AreEqual(0.0, sim.Accumulator, 1e-12, "the backlog is discarded, not carried");
            Assert.AreEqual(0, sim.Advance(0.0, 32), "nothing is owed from the discarded backlog");
        }

        [Test]
        public void DefaultCapIsTheReferenceCap()
        {
            var sim = Sim();
            Assert.AreEqual(Simulation.MaxTicksPerCall, sim.Advance(1000), "1,000 s is 20,000 ticks, over the cap");
            Assert.AreEqual(0.0, sim.Accumulator, 1e-12);
        }

        [Test]
        public void NonFiniteOrNegativeInputRunsNothingAndLeavesTheAccumulatorIntact()
        {
            var sim = Sim();
            sim.Advance(0.03);                      // 0.6 ticks carried
            var acc = sim.Accumulator;
            Assert.AreEqual(0, sim.Advance(double.NaN));
            Assert.AreEqual(0, sim.Advance(double.PositiveInfinity));
            Assert.AreEqual(0, sim.Advance(-5));
            Assert.AreEqual(acc, sim.Accumulator, 1e-12);
            Assert.AreEqual(0, sim.State.Tick);
        }

        [Test]
        public void ResetAccumulatorDropsThePartTickAndNothingElse()
        {
            // The host calls this on a pause/unpause transition and on a discarded frame gap, where the reference
            // zeroes st.acc (session.ts:226 and :258).
            var sim = Sim();
            sim.Advance(0.04);                      // 0.8 ticks carried, no tick run
            Assert.AreEqual(0.8, sim.Accumulator, 1e-9);
            var hash = sim.Hash();
            sim.ResetAccumulator();
            Assert.AreEqual(0.0, sim.Accumulator, 0.0);
            Assert.AreEqual(hash, sim.Hash(), "resetting the accumulator changes no sim state");
            Assert.AreEqual(0, sim.Advance(0.04), "the dropped remainder is not owed back");
        }

        [Test]
        public void SingleTickHelperDoesNotTouchTheAccumulator()
        {
            var sim = Sim();
            sim.Advance(0.03);
            var acc = sim.Accumulator;
            sim.Tick();
            Assert.AreEqual(1, sim.State.Tick);
            Assert.AreEqual(acc, sim.Accumulator, 1e-12);
        }
    }
}
