using NUnit.Framework;
using Relight.Sim.Tests.Support;

// Folder Tests/Sim/Replay; the namespace is ...Tests.Determinism because a namespace named `Replay` would hide
// the `Relight.Sim.Replay` class these tests exercise.
namespace Relight.Sim.Tests.Determinism
{
    /// <summary>
    /// B-12's own deliverable: the replay-consistency check (TECHNICAL_ARCHITECTURE.md §10.6 check 2, contract H2 in
    /// §10.4, decision U-M-13). Same seed and same command log, run twice in one process, reach the same tick and the
    /// same Unity-canonical hash. It is a self-consistency claim about this build on this platform — no TypeScript
    /// hash is reproduced or compared (H4 is explicitly not attempted), and nothing here is replay *infrastructure*
    /// (logged streams from the reference, verdicts, soaks stay deferred as TASKS.md DEF-01).
    ///
    /// Ported check: reference packages/sim/test/hour.test.ts:97–113 ("a logged hour-bot run replays to the same
    /// state"), reduced to its assertion — a logged command stream replayed from a fresh state agrees with the run it
    /// was logged from. The reference compares selected fields because its hour bot is stochastic in the player's
    /// hands; here the whole canonical state is compared, which is strictly stronger. The bot itself (hour.ts, 1,548
    /// lines) is not ported: the scenario in <see cref="Scenarios.ShortRun"/> plays its part.
    /// </summary>
    public sealed class ReplayConsistencyTests
    {
        private const int Until = Scenarios.ShortRunTicks;

        /// <summary>H2: same seed, same log, twice in one process.</summary>
        [Test]
        public void SameSeedAndCommandLogReplayTwiceToTheSameHash()
        {
            var s = Scenarios.ShortRun();
            var (_, log) = s.Build();

            var a = Replay.Run(s.Context(), s.Seed, log, Until);
            var b = Replay.Run(s.Context(), s.Seed, log, Until);

            Assert.That(a.State.Tick, Is.EqualTo(Until), "the replay must land exactly on the target tick");
            Assert.That(b.State.Tick, Is.EqualTo(a.State.Tick), "tick");
            Assert.That(b.Hash, Is.EqualTo(a.Hash),
                "two replays of the same seed and log must agree\n" +
                SimTestUtil.FirstDifference(SimTestUtil.Canonical(a.State), SimTestUtil.Canonical(b.State)));
            Assert.That(a.State.T, Is.EqualTo(Until * Simulation.TickSeconds).Within(1e-9), "sim seconds");
        }

        /// <summary>The scenario must actually do the work it claims, or the hash comparison proves nothing.</summary>
        [Test]
        public void TheShortRunActuallyMovesCraftsAndTransfers()
        {
            var s = Scenarios.ShortRun();
            var (_, log) = s.Build();
            var ctx = s.Context();
            var (st, _) = Replay.Run(ctx, s.Seed, log, Until);

            Assert.That(st.Machines.Count, Is.EqualTo(2), "the Depot and the supply chest were placed");
            Assert.That(st.Stats.MagsMade, Is.EqualTo(Scenarios.ExpectedBullets), "five hand batches finished");
            Assert.That(st.Stats.HandCrafted, Is.EqualTo(5), "batches counted");
            Assert.That(st.Engineer.Walked, Is.GreaterThan(1.0), "the engineer moved");

            var chest = st.MachineById(Scenarios.ChestId);
            Assert.That(chest, Is.Not.Null);
            Assert.That(chest.Inv[ItemId.Steel], Is.EqualTo(6).Within(1e-9), "10 steel in, 4 back out");
            Assert.That(chest.Inv[ItemId.Magazine], Is.EqualTo(25).Within(1e-9), "25 bullets loaded");
            Assert.That(st.Engineer.Inv[ItemId.Magazine], Is.EqualTo(25).Within(1e-9), "the rest stayed in the Backpack");
        }

        /// <summary>A different seed is a different run: the check would be vacuous if every run hashed alike.</summary>
        [Test]
        public void ADifferentSeedGivesADifferentHash()
        {
            var a = Scenarios.ShortRun(7);
            var b = Scenarios.ShortRun(8);
            var ra = Replay.Run(a.Context(), a.Seed, a.Build().Log, Until);
            var rb = Replay.Run(b.Context(), b.Seed, b.Build().Log, Until);
            Assert.That(rb.Hash, Is.Not.EqualTo(ra.Hash), "the seed and its rng word are part of the hashed state");
        }

        /// <summary>
        /// The log is honoured, not merely counted: swapping the "walk west" and "stop" commands — same two ticks,
        /// same two commands, opposite order — changes where the engineer ends up and therefore the hash.
        /// </summary>
        [Test]
        public void SwappingTwoLoggedCommandsChangesTheOutcome()
        {
            var s = Scenarios.ShortRun();
            var (_, log) = s.Build();
            var swapped = SimTestUtil.SwapCommands(log, Scenarios.WalkStartIndex, Scenarios.WalkStopIndex);

            var straight = Replay.Run(s.Context(), s.Seed, log, Until);
            var reordered = Replay.Run(s.Context(), s.Seed, swapped, Until);

            Assert.That(reordered.Hash, Is.Not.EqualTo(straight.Hash), "command order must matter");
            // Both runs are told to walk back to the spawn at tick 120, so they finish standing in the same place;
            // what differs is the route, because the swap pushes the westward walk into the sprint window at tick 20.
            Assert.That(reordered.State.Engineer.Walked, Is.Not.EqualTo(straight.State.Engineer.Walked).Within(1e-9),
                "the same two commands in the other order send the engineer a different distance");
        }

        /// <summary>
        /// H3-shaped, inside one language: the two drivers agree. The live run goes through the host's path —
        /// <c>Queue</c> then <c>Advance</c>, so the tick accumulator and the pending queue decide when commands land —
        /// and the replay goes through <c>Apply</c> + <c>Tick</c>. Equal hashes say the replay reproduces play rather
        /// than a second copy of itself.
        /// </summary>
        [Test]
        public void ReplayingTheLogEqualsPlayingItLive()
        {
            var s = Scenarios.ShortRun();
            var (live, log) = s.Play(Until);
            var replayed = Replay.Run(s.Context(), s.Seed, log, Until);

            Assert.That(live.State.Tick, Is.EqualTo(Until), "the live run lands on the target tick");
            Assert.That(replayed.Hash, Is.EqualTo(live.Hash()),
                "a replay must reproduce the played run\n" +
                SimTestUtil.FirstDifference(SimTestUtil.Canonical(live.State), SimTestUtil.Canonical(replayed.State)));
        }

        /// <summary>
        /// The reference's <c>dropAim</c> option (hour.ts:1483), which it uses to replay a run "without the rifle".
        /// Phase B has no weapon, so what it can assert is the filter itself: the aim never reaches the state, and
        /// everything else about the run is unchanged.
        /// </summary>
        [Test]
        public void DropAimRemovesTheAimAndNothingElse()
        {
            var s = Scenarios.ShortRun();
            var (_, log) = s.Build();
            // Stop before the scenario clears the aim again, so there is an aim to drop.
            const int untilAimed = 2000;
            var kept = Replay.Run(s.Context(), s.Seed, log, untilAimed);
            var dropped = Replay.Run(s.Context(), s.Seed, log, untilAimed, dropAim: true);

            Assert.That(kept.State.Engineer.HasAim, Is.True, "the scenario aims before tick 2000");
            Assert.That(dropped.State.Engineer.HasAim, Is.False, "dropAim must keep the aim out of the state");
            Assert.That(dropped.State.Engineer.Pos.Equals(kept.State.Engineer.Pos), Is.True, "aiming does not move the engineer");
            Assert.That(dropped.State.Stats.MagsMade, Is.EqualTo(kept.State.Stats.MagsMade), "nor does it change crafting");
        }

        /// <summary>
        /// The ordering rule itself (reference hour.ts:1481–1488 with flow.ts:1107): a command logged at tick N is
        /// applied while the state is still at tick N, before tick N is stepped; and an entry at the final tick is
        /// applied without stepping past it (hour.ts:1489–1497).
        /// </summary>
        [Test]
        public void ACommandLandsBeforeTheTickItIsLoggedOn()
        {
            var log = new System.Collections.Generic.List<LoggedCommand>
            {
                new LoggedCommand(10, new WalkCommand(1, 0)),
            };
            var ctx = ScenarioBuilder.New().Context();

            var before = Replay.Run(ctx, 7, log, 10);
            Assert.That(before.State.Tick, Is.EqualTo(10));
            Assert.That(before.State.Engineer.Vel.X, Is.EqualTo(1).Within(1e-12),
                "the boundary command is applied at the target tick without stepping again");

            // Ten ticks of walking east must have moved the engineer by the time tick 20 is reached.
            var after = Replay.Run(ctx, 7, log, 20);
            Assert.That(after.State.Engineer.Pos.X, Is.GreaterThan(before.State.Engineer.Pos.X + 0.5),
                "the command took effect from tick 10 onwards, not from tick 11");
        }

        /// <summary>An empty log replays to the same state as plain stepping: the helper adds nothing of its own.</summary>
        [Test]
        public void AnEmptyLogIsJustTicking()
        {
            var ctx = ScenarioBuilder.New().Context();
            var replayed = Replay.Run(ctx, 7, new System.Collections.Generic.List<LoggedCommand>(), 200);
            var stepped = SimTestUtil.Step(Simulation.NewGame(ScenarioBuilder.New().Context(), 7), 200);
            Assert.That(replayed.Hash, Is.EqualTo(stepped.Hash()));
        }
    }
}
