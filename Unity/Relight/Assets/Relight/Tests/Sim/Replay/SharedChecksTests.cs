using System;
using System.Collections.Generic;
using NUnit.Framework;
using Relight.Sim.Tests.Support;

// Folder Tests/Sim/Replay (B-12's own folder); namespace ...Tests.Determinism, see ReplayConsistencyTests.cs.
namespace Relight.Sim.Tests.Determinism
{
    /// <summary>
    /// The cross-cutting Phase B checks B-12 hosts (TECHNICAL_ARCHITECTURE.md §10.6, "the one shared piece"), over
    /// the single short headless run in <see cref="Scenarios.ShortRun"/>:
    /// <list type="number">
    /// <item><b>PRNG vectors</b> — owned by B-03 and already in <c>Tests/Sim/Core/PrngVectorTests.cs</c>;</item>
    /// <item><b>Two-run equality</b> — owned by B-03 (<c>Tests/Sim/Core/TwoRunEqualityTests.cs</c>) and extended to a
    ///       command log by <see cref="ReplayConsistencyTests"/>, which is B-12's own deliverable;</item>
    /// <item><b>Save round trip</b> (contract H1) — owned by B-11; what exists before that lands is checked here, and
    ///       the file-level round trip is the <c>[Explicit]</c> stub at the end of this class;</item>
    /// <item><b>Conservation</b> (§2.7, U-D-05) — owned by B-06; checked here at the end of the shared run and at
    ///       intervals through it.</item>
    /// </list>
    /// Plus the sim-side half of the B-04 timing check: the edit-mode driver has no wall clock, so what is asserted
    /// here is that a stream of irregular real-second deltas buys exactly the ticks it pays for. The wall-clock half
    /// (a real 10 s giving ~200 ticks) is a play-mode test and stays in <c>Tests/Play/SimHostTests.cs</c>.
    /// </summary>
    public sealed class SharedChecksTests
    {
        private const int Until = Scenarios.ShortRunTicks;

        private static (SimContext Ctx, SimState St) RunShort(int untilTick = Until)
        {
            var s = Scenarios.ShortRun();
            var ctx = s.Context();
            var (st, _) = Replay.Run(ctx, s.Seed, s.Build().Log, untilTick);
            return (ctx, st);
        }

        // ---------------------------------------------------------------- check 4: conservation

        /// <summary>
        /// §10.6 check 4: <c>conservation(state).ok</c> at the end of the short run. Ported assertion: reference
        /// packages/sim/test/ledger.test.ts:91 (<c>assert.ok(c.ok &amp;&amp; ...)</c>) and its helper at ledger.test.ts:28,
        /// which calls an item unexplained at <c>Math.abs(v) &gt; 0.01</c> — the default of
        /// <c>conservation(st, tolerance = 0.01)</c> at ledger.ts:103. That tolerance, 0.01 of an item, is kept here.
        /// </summary>
        [Test]
        public void ConservationHoldsAtTheEndOfTheShortRun()
        {
            var (ctx, st) = RunShort();
            var c = Ledger.Conservation(st, ctx.Data);
            Assert.That(c.Ok, Is.True, "unexplained items after five sim-minutes: " + string.Join(" | ", c.Problems));
            Assert.That(c.Tolerance, Is.EqualTo(0.01).Within(1e-12), "the reference's tolerance");

            // The run must have moved items about, or "nothing unexplained" would be trivially true.
            Assert.That(c.Sources[ItemId.Magazine], Is.EqualTo(Scenarios.ExpectedBullets).Within(1e-9), "bullets entered the game");
            Assert.That(c.Sinks[ItemId.Steel], Is.EqualTo(10).Within(1e-9), "steel left it as a recipe input");
            Assert.That(c.At(LedgerPlace.Machines, ItemId.Magazine), Is.EqualTo(25).Within(1e-9), "bullets counted inside the chest");
            Assert.That(c.At(LedgerPlace.Pockets, ItemId.Magazine), Is.EqualTo(25).Within(1e-9), "and in the Backpack");
        }

        /// <summary>Conservation is not just true at the end: it is true at every sample through the run.</summary>
        [Test]
        public void ConservationHoldsThroughoutTheShortRun()
        {
            var s = Scenarios.ShortRun();
            var ctx = s.Context();
            var (sim, log) = s.Build();
            var k = 0;
            for (var tick = 0; tick < Until; tick++)
            {
                while (k < log.Count && log[k].Tick <= sim.State.Tick) sim.Apply(log[k++].Command);
                sim.Tick();
                sim.State.Events.Clear();
                if (tick % 250 != 0) continue;
                var c = Ledger.Conservation(sim.State, ctx.Data);
                Assert.That(c.Ok, Is.True, $"tick {sim.State.Tick}: " + string.Join(" | ", c.Problems));
            }
        }

        // ---------------------------------------------------------------- check 3: save round trip (H1)

        /// <summary>
        /// What is checkable before B-11 lands: the canonical text is exactly what the hash is taken over, and it is
        /// stable — the same state written twice gives the same bytes, which is the property a save file depends on
        /// (TECHNICAL_ARCHITECTURE.md §10.3, <c>CanonicalJsonWriter</c>'s format header).
        /// </summary>
        [Test]
        public void TheHashIsTakenOverAStableCanonicalText()
        {
            var (_, st) = RunShort(1200);
            var first = CanonicalJsonWriter.Write(st);
            var second = CanonicalJsonWriter.Write(st);
            Assert.That(second, Is.EqualTo(first), "writing the same state twice must give the same text");
            Assert.That(StateHash.Compute(st), Is.EqualTo(StateHash.Of(first)), "the hash is FNV-1a of that text");
        }

        /// <summary>
        /// The class of defect contract H1 exists to catch is "a field the serialiser drops". Until B-11's reader
        /// exists, the writer side of it is checkable: every field the state visits must appear as a key in the
        /// canonical text. A field added to <see cref="SimState"/> and forgotten by the writer fails here.
        /// </summary>
        [Test]
        public void EveryVisitedFieldReachesTheCanonicalText()
        {
            var (_, st) = RunShort(600);
            var text = CanonicalJsonWriter.Write(st);
            var names = SimTestUtil.FieldNames(st);
            Assert.That(names.Count, Is.GreaterThan(40), "the state visits a substantial number of fields");
            var missing = new List<string>();
            for (var i = 0; i < names.Count; i++)
            {
                var leaf = names[i];
                var slash = leaf.LastIndexOf('/');
                if (slash >= 0) leaf = leaf.Substring(slash + 1);
                if (text.IndexOf("\"" + leaf + "\":", StringComparison.Ordinal) < 0) missing.Add(names[i]);
            }
            Assert.That(missing, Is.Empty, "fields visited but absent from the canonical text: " + string.Join(", ", missing));
        }

        /// <summary>
        /// Contract H1 proper — <c>UnityHash(Load(Save(state))) == UnityHash(state)</c> after this same short run.
        /// B-11 owns the save layer and is being written concurrently; this stub is the wiring point.
        ///
        /// The API this test assumes, so the coordinator can connect it in one edit:
        /// a reading <see cref="IStateVisitor"/> over the canonical JSON, reached through something of the shape
        /// <c>SaveFile.Write(SimState) → string</c> and <c>SaveFile.Read(string) → SimState</c> (or
        /// <c>SaveGame.Save/Load</c>), plus the file-level pair that writes and reads a path. The body then becomes:
        /// run <see cref="Scenarios.ShortRun"/>, save, load, and assert the two <see cref="StateHash"/> values are
        /// equal and that <see cref="Ledger.Conservation"/> still reports <c>ok</c> on the loaded state.
        /// </summary>
        [Test]
        public void SaveThenLoadGivesTheSameHash()
        {
            // Wired by the coordinator against B-11's SaveSerializer (Sim/Persistence) once both landed.
            var (ctx, st) = RunShort();
            var text = SaveSerializer.WriteText(st, ctx.Data, "2026-09-12T00:00:00.0000000Z");
            var load = SaveSerializer.ReadText(text, ctx.Data);
            Assert.That(load.Ok, Is.True, load.Reason);
            Assert.That(StateHash.Compute(load.State), Is.EqualTo(StateHash.Compute(st)));
            Assert.That(SimTestUtil.Canonical(load.State), Is.EqualTo(SimTestUtil.Canonical(st)));
            Assert.That(SimTestUtil.ConservationProblems(ctx, load.State), Is.Empty);
        }

        // ---------------------------------------------------------------- timing

        /// <summary>
        /// The sim-side timing check: a stream of irregular real-second deltas buys exactly the ticks it pays for,
        /// and sim time is the tick count (reference flow.ts <c>advanceFlow</c>, lines 1109–1112, including the 1e-6
        /// "never lose a tick to rounding" epsilon at flow.ts:1110). Tolerance: ±1 tick, that epsilon's whole effect.
        /// </summary>
        [Test]
        public void IrregularRealSecondsBuyExactlyTheTicksTheyPayFor()
        {
            var deltas = new[] { 1.0 / 60, 1.0 / 30, 0.25, 0.008, 0.1, 1.0 / 144 };
            var sim = Simulation.NewGame(ScenarioBuilder.New().Context(), 3);
            double total = 0;
            for (var i = 0; i < 6000; i++)
            {
                var dt = deltas[i % deltas.Length];
                total += dt;
                sim.Advance(dt);
                sim.State.Events.Clear();
            }
            var expected = Math.Floor(total * Simulation.TicksPerSecond);
            Assert.That(sim.State.Tick, Is.EqualTo((int)expected).Within(1),
                $"{total:F3} real seconds should buy {expected} ticks, not {sim.State.Tick}");
            Assert.That(sim.State.T, Is.EqualTo(sim.State.Tick * Simulation.TickSeconds).Within(1e-9),
                "sim seconds are the tick count, not the real-time total");
            Assert.That(sim.Accumulator, Is.LessThan(1.0), "at most a part tick is ever held over");
        }

        /// <summary>
        /// B-ACC's shape, in edit mode: five sim-minutes of move, craft and transfer repeated twice give equal hashes.
        /// The long form (20 sim-minutes) runs only with RELIGHT_LONG_TIMING=1, following Tests/Play/SimHostTests.cs.
        /// </summary>
        [Test]
        public void FiveSimMinutesRepeatedTwiceGiveEqualHashes()
        {
            AssertTwoRunsAgree(Until);
        }

        [Test, Explicit("long form")]
        public void TwentySimMinutesRepeatedTwiceGiveEqualHashes_Long()
        {
            if (Environment.GetEnvironmentVariable("RELIGHT_LONG_TIMING") != "1")
            {
                Assert.Ignore("Set RELIGHT_LONG_TIMING=1 to run the 20 sim-minute form; the 5-minute form runs by default.");
                return;
            }
            AssertTwoRunsAgree(4 * Until);
        }

        private static void AssertTwoRunsAgree(int untilTick)
        {
            var s = Scenarios.ShortRun();
            var log = s.Build().Log;
            var a = Replay.Run(s.Context(), s.Seed, log, untilTick);
            var b = Replay.Run(s.Context(), s.Seed, log, untilTick);
            Assert.That(a.State.Tick, Is.EqualTo(untilTick));
            Assert.That(b.Hash, Is.EqualTo(a.Hash),
                SimTestUtil.FirstDifference(SimTestUtil.Canonical(a.State), SimTestUtil.Canonical(b.State)));
            var ctx = s.Context();
            Assert.That(Ledger.Conservation(a.State, ctx.Data).Ok, Is.True,
                SimTestUtil.ConservationProblems(ctx, a.State));
        }
    }
}
