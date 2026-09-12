using NUnit.Framework;
using Relight.Sim.Tests.Support;

namespace Relight.Sim.Tests.Persistence
{
    /// <summary>
    /// What a save that is not this build's save does: it is <b>refused with a reason</b>, and the game that asked
    /// for it is untouched (TECHNICAL_ARCHITECTURE.md §9.4.5, §9.3, U-D-27). Nothing is migrated, nothing is
    /// guessed at, and no partially-loaded state is ever handed back — the reader builds a complete new
    /// <see cref="SimState"/> and verifies its hash before the caller sees anything.
    ///
    /// The two categories are kept apart deliberately:
    /// <list type="bullet">
    /// <item><b>foreign</b> — a file from the reference build, another game, or a future schema. It is refused and
    ///       <i>left exactly where it is</i>: the player pointed at the wrong file, nothing is wrong with it.</item>
    /// <item><b>damaged</b> — one of ours, broken. It is set aside as <c>.corrupt</c> and its <c>.bak</c> is
    ///       offered, because losing the file silently is the one outcome a save system must never produce.</item>
    /// </list>
    /// </summary>
    public sealed class SaveRefusalTests
    {
        private static (Simulation Sim, string Document) Played(int ticks = 300)
        {
            var (sim, _) = Scenarios.ShortRun().Play(ticks);
            return (sim, SaveSerializer.WriteText(sim.State, sim.Context.Data));
        }

        // ---------------------------------------------------------------- foreign files

        [Test]
        public void AReferenceBuildSaveIsRefusedAndSaysWhatItIs()
        {
            // The reference's envelope: packages/sim/src/save.ts:77-90 (kind 'relight-save', version 1-3).
            const string reference = "{\"kind\":\"relight-save\",\"version\":3,\"seed\":1,\"tick\":10,\"t\":0.5,\"state\":{}}";
            var r = SaveSerializer.ReadText(reference, ReferenceData.Create());
            Assert.That(r.Ok, Is.False);
            Assert.That(r.Reason, Does.Contain("original build"), "the refusal must say what the file actually is");
            Assert.That(r.Reason, Does.Contain("not converted"), "U-D-27: there is no converter, by decision");
            Assert.That(r.Damaged, Is.False, "a reference save is intact; it is simply not ours");
            Assert.That(r.State, Is.Null, "a refusal hands back nothing to load");
        }

        [Test]
        public void AFutureSchemaVersionIsRefusedByNumber()
        {
            var (sim, document) = Played();
            var future = SaveSchema.Version + 2;            // whatever this build reads, this is not it
            var bumped = PersistenceFixture.Retarget(document, "version", future.ToString());
            var r = SaveSerializer.ReadText(bumped, sim.Context.Data);
            Assert.That(r.Ok, Is.False);
            Assert.That(r.Reason, Does.Contain("unsupported save version " + future));
            Assert.That(r.Reason, Does.Contain("version " + SaveSchema.Version), "it says which version this build does read");
            Assert.That(r.Damaged, Is.False, "a newer save is not damage: there is nothing to recover");
        }

        [Test]
        public void AFileFromAnotherProgramIsRefused()
        {
            var r = SaveSerializer.ReadText("{\"kind\":\"some-other-game\",\"version\":1,\"state\":{}}", ReferenceData.Create());
            Assert.That(r.Ok, Is.False);
            Assert.That(r.Reason, Does.Contain("not a Relight save"));
        }

        [Test]
        public void AJsonDocumentThatIsNotASaveIsRefused()
        {
            foreach (var text in new[] { "[1,2,3]", "\"hello\"", "42", "null" })
            {
                var r = SaveSerializer.ReadText(text, ReferenceData.Create());
                Assert.That(r.Ok, Is.False, text + " must be refused");
                Assert.That(r.Reason, Is.Not.Empty);
            }
        }

        [Test]
        public void ASaveWithNoVersionIsRefused()
        {
            var r = SaveSerializer.ReadText("{\"kind\":\"relight-save-unity\",\"state\":{}}", ReferenceData.Create());
            Assert.That(r.Ok, Is.False);
            Assert.That(r.Reason, Does.Contain("which version"));
        }

        // ---------------------------------------------------------------- damaged files

        [Test]
        public void ATruncatedFileIsRefusedWithThePositionOfTheDamage()
        {
            var (sim, document) = Played();
            var r = SaveSerializer.ReadText(document.Substring(0, document.Length / 2), sim.Context.Data);
            Assert.That(r.Ok, Is.False);
            Assert.That(r.Reason, Does.Contain("damaged"));
            Assert.That(r.Damaged, Is.True, "a half file is one of ours, broken");
        }

        [Test]
        public void AnEmptyFileIsRefused()
        {
            var r = SaveSerializer.Read(new byte[0], ReferenceData.Create());
            Assert.That(r.Ok, Is.False);
            Assert.That(r.Reason, Does.Contain("empty"));
        }

        [Test]
        public void BytesThatAreNotTextAreRefused()
        {
            var r = SaveSerializer.Read(new byte[] { 0xFF, 0xFE, 0x00, 0x01, 0x80 }, ReferenceData.Create());
            Assert.That(r.Ok, Is.False);
            Assert.That(r.Reason, Does.Contain("UTF-8"));
        }

        /// <summary>
        /// The integrity check doing its job: one edited number inside the state, and the recomputed hash no longer
        /// matches the header's. This is the mechanism that makes H1 self-enforcing on every real load rather than
        /// only in the round-trip test.
        /// </summary>
        [Test]
        public void AnEditedStateIsCaughtByTheChecksum()
        {
            var (sim, document) = Played();
            var tampered = PersistenceFixture.RetargetInState(document, "seed", "999999");
            Assert.That(tampered, Is.Not.EqualTo(document));
            var r = SaveSerializer.ReadText(tampered, sim.Context.Data);
            Assert.That(r.Ok, Is.False);
            Assert.That(r.Reason, Does.Contain("do not match its checksum"));
            Assert.That(r.Damaged, Is.True);
        }

        [Test]
        public void ASaveWithNoChecksumIsRefused()
        {
            var (sim, document) = Played();
            var stripped = PersistenceFixture.Retarget(document, "hash", "\"\"");
            var r = SaveSerializer.ReadText(stripped, sim.Context.Data);
            Assert.That(r.Ok, Is.False);
            Assert.That(r.Reason, Does.Contain("integrity check"));
        }

        // ---------------------------------------------------------------- differences that are not refusals

        /// <summary>
        /// Balance data that has moved on is a <b>warning</b>, not a refusal: a changed recipe cost makes an old save
        /// different, not damaged, and refusing it would throw away a player's game over a tuning edit. The save
        /// loads, and the difference is something the UI can say out loud.
        /// </summary>
        [Test]
        public void DifferentBalanceDataIsAWarningNotARefusal()
        {
            var (sim, document) = Played();
            var other = PersistenceFixture.Retarget(document, "dataVersion", "\"deadbeef\"");
            var r = SaveSerializer.ReadText(other, sim.Context.Data);
            Assert.That(r.Ok, Is.True, "an old balance version still loads: " + r.Reason);
            Assert.That(r.Warning, Does.Contain("different balance data"));
            Assert.That(r.Warning, Does.Contain("deadbeef"));
            Assert.That(StateHash.Compute(r.State), Is.EqualTo(sim.Hash()), "and it is still the same state");
        }

        [Test]
        public void ALeadingByteOrderMarkIsTolerated()
        {
            var (sim, document) = Played();
            var r = SaveSerializer.ReadText("\uFEFF" + document, sim.Context.Data);
            Assert.That(r.Ok, Is.True, "a file re-saved by an editor still loads: " + r.Reason);
            Assert.That(StateHash.Compute(r.State), Is.EqualTo(sim.Hash()));
        }

        // ---------------------------------------------------------------- the running game is untouched

        /// <summary>
        /// The acceptance wording: a corrupt file and a foreign file are refused <i>and leave the previous state
        /// untouched</i>. Both senses are checked — the simulation that is running keeps its exact hash and keeps
        /// running, and the good save already on disk is still there and still loads.
        /// </summary>
        [Test]
        public void ARefusedLoadLeavesTheRunningGameAndTheGoodSaveUntouched()
        {
            var (sim, _) = Scenarios.ShortRun().Play(500);
            var (fs, store) = PersistenceFixture.Store();
            Assert.That(store.Save("good", sim.State, sim.Context.Data).Ok, Is.True);
            var before = sim.Hash();

            // A file from somewhere else, dropped into the saves folder under a plausible name.
            fs.PutText(store.PathOf("holiday"), "{\"kind\":\"some-other-game\",\"version\":1,\"state\":{}}");
            var foreign = store.Load("holiday", sim.Context.Data);
            Assert.That(foreign.Ok, Is.False);
            Assert.That(fs.Has(store.PathOf("holiday")), Is.True, "a foreign file is left exactly where it is");
            Assert.That(fs.Has(store.PathOf("holiday") + AtomicWrite.CorruptSuffix), Is.False,
                "and is not renamed: nothing is wrong with it");

            // And a broken one of ours.
            fs.PutText(store.PathOf("broken"), "{\"kind\":\"relight-save-unity\",\"version\":1,\"state\":{\"tick\"");
            var damaged = store.Load("broken", sim.Context.Data);
            Assert.That(damaged.Ok, Is.False);
            Assert.That(fs.Has(store.PathOf("broken") + AtomicWrite.CorruptSuffix), Is.True,
                "a damaged file is set aside rather than lost");

            Assert.That(sim.Hash(), Is.EqualTo(before), "the running game did not change");
            SimTestUtil.Step(sim, 20);
            Assert.That(sim.State.Tick, Is.EqualTo(520), "and it is still running");

            var good = store.Load("good", sim.Context.Data);
            Assert.That(good.Ok, Is.True, "the good save is still loadable: " + good.Reason);
            Assert.That(StateHash.Compute(good.State), Is.EqualTo(before));
        }

        /// <summary>
        /// A damaged file with a previous copy beside it: the <c>.bak</c> the write protocol keeps is offered, and
        /// the result says so rather than pretending the newest file loaded (§9.4.5).
        /// </summary>
        [Test]
        public void ADamagedSaveFallsBackToItsPreviousCopy()
        {
            var (sim, _) = Scenarios.ShortRun().Play(300);
            var (fs, store) = PersistenceFixture.Store();

            Assert.That(store.Save("slot", sim.State, sim.Context.Data).Ok, Is.True);
            var first = sim.Hash();
            SimTestUtil.Step(sim, 100);
            Assert.That(store.Save("slot", sim.State, sim.Context.Data).Ok, Is.True, "the second save makes a .bak");
            Assert.That(fs.Has(store.PathOf("slot") + AtomicWrite.BackupSuffix), Is.True);

            fs.PutText(store.PathOf("slot"), "{ this is not json");
            var r = store.Load("slot", sim.Context.Data);

            Assert.That(r.Ok, Is.True, "the previous copy must be offered: " + r.Reason);
            Assert.That(r.Recovered, Does.Contain("damaged"));
            Assert.That(StateHash.Compute(r.State), Is.EqualTo(first), "and it is the state from before the last save");
            Assert.That(fs.Has(store.PathOf("slot") + AtomicWrite.CorruptSuffix), Is.True);
        }

        // ---------------------------------------------------------------- slot names

        /// <summary>
        /// A player cannot name a manual save into the autosaves' name space — the second of the two guards that
        /// make "an autosave never writes a manual slot" true in both directions.
        /// </summary>
        [Test]
        public void AManualSlotCannotBeNamedIntoTheAutosaveNameSpace()
        {
            foreach (var name in new[] { "auto", "auto-1", "AUTO", "Autosave", "auto-quit" })
                Assert.That(SaveStore.SlotNameProblem(name), Does.Contain("autosaves"), name + " must be refused");
        }

        [Test]
        public void ASlotNameCannotEscapeTheSavesFolder()
        {
            foreach (var name in new[] { "../evil", "a/b", "a\\b", "c:evil", "", null })
                Assert.That(SaveStore.SlotNameProblem(name), Is.Not.Empty, "'" + name + "' must be refused");
            Assert.That(SaveStore.SlotNameProblem("My game 2"), Is.Empty, "an ordinary name is fine");
            Assert.That(SaveStore.SlotNameProblem(new string('x', 41)), Does.Contain("longer"));
            Assert.That(SaveStore.SlotNameProblem(" leading"), Does.Contain("space"));
        }

        [Test]
        public void SavingToARefusedNameWritesNothing()
        {
            var (sim, _) = Scenarios.ShortRun().Play(100);
            var (fs, store) = PersistenceFixture.Store();
            var r = store.Save("auto-1", sim.State, sim.Context.Data);
            Assert.That(r.Ok, Is.False);
            Assert.That(fs.AllPaths(), Is.Empty, "a refused name must not create a file anywhere");
        }
    }
}
