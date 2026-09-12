using NUnit.Framework;
using Relight.Sim.Tests.Support;

namespace Relight.Sim.Tests.Persistence
{
    /// <summary>
    /// Contract H1 (TECHNICAL_ARCHITECTURE.md §10.4, required of B-11):
    /// <c>UnityHash(Load(Save(state))) == UnityHash(state)</c> after a five-minute headless run.
    ///
    /// The run is <see cref="Scenarios.ShortRun"/> — the same 6,000 ticks of move, craft and transfer the other
    /// Phase B checks use (§10.6: "3–4 reuse the same short run"), so a round-trip loss and a replay divergence are
    /// statements about the same five minutes of play rather than about two unrelated fixtures.
    ///
    /// Two assertions, not one. The hash is the contract, but a hash is 32 bits: the canonical text is compared as
    /// well, so a failure says <i>which field</i> came back wrong instead of only that something did. And because a
    /// state that merely looks right can still be wrong in a way that only shows up when it runs, both copies are
    /// then stepped on and compared again — the defect class this catches (a lazily rebuilt cache restored into the
    /// wrong shape) is invisible to any single-frame comparison.
    /// </summary>
    public sealed class SaveRoundTripTests
    {
        /// <summary>Further ticks run on both copies after the load, to catch a difference the text does not show.</summary>
        private const int ContinueTicks = 200;

        private static (Simulation Sim, string Hash, string Text) PlayShortRun()
        {
            var (sim, _) = Scenarios.ShortRun().Play(Scenarios.ShortRunTicks);
            return (sim, sim.Hash(), PersistenceFixture.Canonical(sim.State));
        }

        [Test]
        public void H1_SaveThenLoadGivesTheSameHashAndTheSameCanonicalText()
        {
            var (sim, hash, text) = PlayShortRun();
            Assert.That(sim.State.Tick, Is.EqualTo(Scenarios.ShortRunTicks), "the fixture ran the full five minutes");

            var document = SaveSerializer.WriteText(sim.State, sim.Context.Data);
            var loaded = SaveSerializer.ReadText(document, sim.Context.Data);

            Assert.That(loaded.Ok, Is.True, "the save this build just wrote must load: " + loaded.Reason);
            Assert.That(loaded.Warning, Is.Null, "the same balance data cannot be a difference");

            var afterText = PersistenceFixture.Canonical(loaded.State);
            Assert.That(afterText, Is.EqualTo(text),
                "the loaded state is not the saved state; " + SimTestUtil.FirstDifference(text, afterText));
            Assert.That(StateHash.Compute(loaded.State), Is.EqualTo(hash), "H1: the hash must survive the round trip");
        }

        /// <summary>
        /// H1 through the real store, which is what a player actually does: bytes to a file, a fresh store object,
        /// and the file read back. It proves the envelope, the UTF-8 encoding and the file layout carry the state as
        /// faithfully as the in-memory text does.
        /// </summary>
        [Test]
        public void H1_SaveToAFileThenLoadItBackGivesTheSameHash()
        {
            var (sim, hash, text) = PlayShortRun();
            var (fs, store) = PersistenceFixture.Store();

            var wrote = store.Save("five minutes", sim.State, sim.Context.Data);
            Assert.That(wrote.Ok, Is.True, "the save must be written: " + wrote.Reason);
            Assert.That(fs.Has(wrote.Path), Is.True, "the file is where the store says it is");

            var reopened = new SaveStore(fs, PersistenceFixture.Root);
            var loaded = reopened.Load("five minutes", sim.Context.Data);

            Assert.That(loaded.Ok, Is.True, "the file must load: " + loaded.Reason);
            Assert.That(StateHash.Compute(loaded.State), Is.EqualTo(hash), "H1 through a file");
            Assert.That(PersistenceFixture.Canonical(loaded.State), Is.EqualTo(text));
        }

        /// <summary>
        /// The state must be <i>runnable</i>, not merely equal. Both copies run on for 200 ticks and must still
        /// agree: a cache restored into a subtly wrong shape (the ground's lazy indices, a machine's inventory
        /// backing array) matches on the frame it is loaded and diverges on the next one.
        /// </summary>
        [Test]
        public void H1_ALoadedStateKeepsRunningIdenticallyToTheOneItCameFrom()
        {
            var (sim, _, _) = PlayShortRun();
            var loaded = SaveSerializer.ReadText(SaveSerializer.WriteText(sim.State, sim.Context.Data), sim.Context.Data);
            Assert.That(loaded.Ok, Is.True, loaded.Reason);

            var resumed = Simulation.Wrap(sim.Context, loaded.State);
            SimTestUtil.Step(sim, ContinueTicks);
            SimTestUtil.Step(resumed, ContinueTicks);

            var a = PersistenceFixture.Canonical(sim.State);
            var b = PersistenceFixture.Canonical(resumed.State);
            Assert.That(b, Is.EqualTo(a),
                ContinueTicks + " ticks after the load the two runs differ; " + SimTestUtil.FirstDifference(a, b));
            Assert.That(resumed.Hash(), Is.EqualTo(sim.Hash()));
        }

        /// <summary>A loaded state must still balance: loading cannot invent or lose items (B-12's ledger, on the file).</summary>
        [Test]
        public void ALoadedStateStillBalances()
        {
            var (sim, _, _) = PlayShortRun();
            var loaded = SaveSerializer.ReadText(SaveSerializer.WriteText(sim.State, sim.Context.Data), sim.Context.Data);
            Assert.That(loaded.Ok, Is.True, loaded.Reason);
            Assert.That(SimTestUtil.ConservationProblems(sim.Context, loaded.State), Is.Empty);
        }

        /// <summary>
        /// Transients are excluded structurally: <c>events</c> is never visited, so it is not in the file, is not in
        /// the hash, and comes back empty. The reference excluded three fields by name
        /// (<c>SAVE_TRANSIENT = ['events','acc','speed']</c>, packages/sim/src/save.ts:48); here the accumulator is
        /// host state on <see cref="Simulation"/> and there is no speed at all (U-D-04), so only the first remains
        /// and it is excluded by not being visited rather than by a list a new field can be forgotten from.
        /// </summary>
        [Test]
        public void EventsAreNotSavedAndDoNotChangeTheHash()
        {
            var (sim, _) = Scenarios.ShortRun().Play(400);
            var quiet = sim.Hash();

            sim.State.Events.Add(new EngineerDownEvent(sim.State.T, 9999.5, -8888.5));
            Assert.That(sim.Hash(), Is.EqualTo(quiet), "an event does not change the state hash");

            var document = SaveSerializer.WriteText(sim.State, sim.Context.Data);
            Assert.That(document.Contains("9999.5"), Is.False, "the pending event list must not be in the file");
            Assert.That(document.Contains("\"events\":"), Is.False, "there is no events member at all");

            var loaded = SaveSerializer.ReadText(document, sim.Context.Data);
            Assert.That(loaded.Ok, Is.True, loaded.Reason);
            Assert.That(loaded.State.Events, Is.Empty, "a loaded game starts with no pending events");
            Assert.That(StateHash.Compute(loaded.State), Is.EqualTo(quiet));
        }

        /// <summary>
        /// <c>playSeconds</c> is header metadata (U-D-41): it survives the round trip, and because it is not visited
        /// it is outside the hash — so how playtime is measured can change later without invalidating H2's replay
        /// hashes or any recorded state hash.
        /// </summary>
        [Test]
        public void PlaySecondsSurvivesTheRoundTripAndIsOutsideTheHash()
        {
            var (sim, _) = Scenarios.ShortRun().Play(600);
            var before = sim.Hash();
            PlayClock.AddTicks(sim.State, 600);
            Assert.That(sim.State.PlaySeconds, Is.EqualTo(30).Within(1e-9), "600 ticks at 20 Hz is 30 seconds");
            Assert.That(sim.Hash(), Is.EqualTo(before), "playtime is metadata, not state");

            var loaded = SaveSerializer.ReadText(SaveSerializer.WriteText(sim.State, sim.Context.Data), sim.Context.Data);
            Assert.That(loaded.Ok, Is.True, loaded.Reason);
            Assert.That(loaded.State.PlaySeconds, Is.EqualTo(30).Within(1e-9));
            Assert.That(loaded.Header.PlayClockText, Is.EqualTo("0:00:30"));
            Assert.That(loaded.Header.Tick, Is.EqualTo(600), "the header repeats the tick so a slot list needs no full parse");
            Assert.That(loaded.Header.Profile, Is.EqualTo(SimVersion.Ruleset), "the profile field is inert but present (U-D-32)");
        }

        /// <summary>Writing the same state twice gives byte-identical files, apart from the timestamp we control.</summary>
        [Test]
        public void WritingTheSameStateTwiceGivesTheSameDocument()
        {
            var (sim, _) = Scenarios.ShortRun().Play(300);
            var at = "2026-09-12T00:00:00.0000000Z";
            var first = SaveSerializer.WriteText(sim.State, sim.Context.Data, at);
            var second = SaveSerializer.WriteText(sim.State, sim.Context.Data, at);
            Assert.That(second, Is.EqualTo(first));
        }
    }
}
