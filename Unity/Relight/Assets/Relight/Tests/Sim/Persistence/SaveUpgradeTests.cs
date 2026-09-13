using System.IO;
using NUnit.Framework;
using Relight.Sim.Tests.Support;

namespace Relight.Sim.Tests.Persistence
{
    /// <summary>
    /// A version-1 Unity save — what the port wrote until the 2026-09-12 repair pass bumped the schema — still
    /// loads (U-M-38, 2026-09-13). The one difference between the versions is <c>state.hand.refundSteel</c> /
    /// <c>refundCopper</c>, so <see cref="SaveUpgrade"/> defaults them to zero on the parsed document and the
    /// file is never rewritten. These tests pin the four things that make that safe: the v1 checksum is checked
    /// as written, the upgraded state hashes exactly as a v2 state with no refund, the file on disk is untouched
    /// by the load (and kept as the <c>.bak</c> by the next save), and anything that is not honestly version 1 is
    /// refused rather than guessed at.
    ///
    /// No real version-1 file exists on the machine these run on, so the fixture is forged from a version-2
    /// document by the inverse of the schema change — the two members removed, both version numbers set to 1
    /// and the checksum recomputed over the v1 state text — independently of the emitter under test.
    /// </summary>
    public sealed class SaveUpgradeTests
    {
        /// <summary>The two members version 2 added, in the canonical (ordinal) order they are written in.</summary>
        private const string RefundMembers = ",\"refundCopper\":0,\"refundSteel\":0";

        private static int Count(string text, string part)
        {
            var n = 0;
            for (var i = text.IndexOf(part, System.StringComparison.Ordinal); i >= 0; i = text.IndexOf(part, i + 1, System.StringComparison.Ordinal)) n++;
            return n;
        }

        /// <summary>A played state with no refund owed, its v2 document, and the v1 document the old build would have written for it.</summary>
        private static (Simulation Sim, string V2, string V1) Forge(int ticks = 300)
        {
            var (sim, _) = Scenarios.ShortRun().Play(ticks);
            Assert.That(sim.State.Hand.RefundSteel, Is.Zero, "the fixture must owe nothing: a v1 file cannot");
            Assert.That(sim.State.Hand.RefundCopper, Is.Zero);

            var v2 = SaveSerializer.WriteText(sim.State, sim.Context.Data);
            var state2 = PersistenceFixture.Canonical(sim.State);
            Assert.That(Count(v2, RefundMembers), Is.EqualTo(1), "exactly one hand object in the document");
            Assert.That(Count(state2, "\"version\":"), Is.EqualTo(1), "the state's own version is the only one inside it");

            var state1 = state2.Replace(RefundMembers, "").Replace("\"version\":2", "\"version\":1");
            var v1 = v2.Replace(RefundMembers, "");
            v1 = PersistenceFixture.RetargetInState(v1, "version", "1");
            v1 = PersistenceFixture.Retarget(v1, "version", "1");
            v1 = PersistenceFixture.Retarget(v1, "hash", CanonicalJsonWriter.QuoteString(StateHash.Of(state1)));
            Assert.That(v1, Is.Not.EqualTo(v2));
            return (sim, v2, v1);
        }

        // ---------------------------------------------------------------- the emitter the upgrade relies on

        /// <summary>
        /// <see cref="JsonValue.ToCanonicalJson"/> must reproduce <see cref="CanonicalJsonWriter"/> byte for byte,
        /// or a v1 checksum could never be verified: the played state (positions, playtime and every other double)
        /// and the whole document round-trip through parse → emit unchanged, and a hand-written document comes
        /// out in canonical order.
        /// </summary>
        [Test]
        public void TheCanonicalEmitterReproducesTheWriterExactly()
        {
            var (sim, v2, _) = Forge();
            var state = PersistenceFixture.Canonical(sim.State);
            Assert.That(JsonValue.Parse(state, out var e1).ToCanonicalJson(), Is.EqualTo(state), e1);
            Assert.That(JsonValue.Parse(v2, out var e2).ToCanonicalJson(), Is.EqualTo(v2), e2);
            Assert.That(StateHash.Of(JsonValue.Parse(v2, out _).Member("state").ToCanonicalJson()),
                Is.EqualTo(sim.Hash()), "the emitted state hashes as the writer's does");

            var loose = JsonValue.Parse("{ \"b\" : 1.5 , \"a\" : [ 2 , true , null , \"x\\\"y\\u0001\" ], \"c\": {} }", out var e3);
            Assert.That(loose, Is.Not.Null, e3);
            Assert.That(loose.ToCanonicalJson(), Is.EqualTo("{\"a\":[2,true,null,\"x\\\"y\\u0001\"],\"b\":1.5,\"c\":{}}"));
        }

        // ---------------------------------------------------------------- a v1 save loads

        [Test]
        public void AVersionOneSaveLoadsAsTheSameStateWithNoRefundOwed()
        {
            var (sim, _, v1) = Forge();
            var r = SaveSerializer.ReadText(v1, sim.Context.Data);

            Assert.That(r.Ok, Is.True, "a v1 file this port wrote must load: " + r.Reason);
            Assert.That(r.Header.Version, Is.EqualTo(1), "the header reports the file as it is");
            Assert.That(r.Upgraded, Does.Contain("version 1"), "the player is told the file was older");
            Assert.That(r.Upgraded, Does.Contain("unchanged"), "and that the file was not touched");
            Assert.That(r.Warning, Is.Null, "an upgrade is information, not a warning");
            Assert.That(r.State.Hand.RefundSteel, Is.Zero);
            Assert.That(r.State.Hand.RefundCopper, Is.Zero);
            Assert.That(r.State.Version, Is.EqualTo(SaveSchema.Version), "in memory it is a current state");
            Assert.That(StateHash.Compute(r.State), Is.EqualTo(sim.Hash()), "H1 across the upgrade");
            Assert.That(PersistenceFixture.Canonical(r.State), Is.EqualTo(PersistenceFixture.Canonical(sim.State)));
            Assert.That(SimTestUtil.ConservationProblems(sim.Context, r.State), Is.Empty);
        }

        [Test]
        public void AnUpgradedStateKeepsRunningLikeTheOriginal()
        {
            var (sim, _, v1) = Forge();
            var r = SaveSerializer.ReadText(v1, sim.Context.Data);
            Assert.That(r.Ok, Is.True, r.Reason);

            var copy = Simulation.Wrap(sim.Context, r.State);
            SimTestUtil.Step(sim, 200);
            SimTestUtil.Step(copy, 200);
            Assert.That(copy.Hash(), Is.EqualTo(sim.Hash()), "the two copies diverged after the load");
        }

        [Test]
        public void ASaveMadeAfterAnUpgradeIsAnOrdinaryCurrentFile()
        {
            var (sim, v2, v1) = Forge();
            var r = SaveSerializer.ReadText(v1, sim.Context.Data);
            Assert.That(r.Ok, Is.True, r.Reason);

            var again = SaveSerializer.WriteText(r.State, sim.Context.Data, "2026-09-13T00:00:00.0000000Z");
            var header = SaveSerializer.ReadHeader(System.Text.Encoding.UTF8.GetBytes(again), out var problem);
            Assert.That(header, Is.Not.Null, problem);
            Assert.That(header.Version, Is.EqualTo(SaveSchema.Version));
            Assert.That(again, Does.Contain(RefundMembers));
            Assert.That(again, Does.Contain("\"version\":" + SaveSchema.Version + "}"), "the state's own version is current too");
            var back = SaveSerializer.ReadText(again, sim.Context.Data);
            Assert.That(back.Ok, Is.True, back.Reason);
            Assert.That(back.Upgraded, Is.Null, "the second load needs no upgrade");
            Assert.That(StateHash.Compute(back.State), Is.EqualTo(StateHash.Of(JsonValue.Parse(v2, out _).Member("state").ToCanonicalJson())));
        }

        [Test]
        public void AVersionOneHeaderIsListable()
        {
            var (_, _, v1) = Forge();
            var header = SaveSerializer.ReadHeader(System.Text.Encoding.UTF8.GetBytes(v1), out var problem);
            Assert.That(header, Is.Not.Null, problem);
            Assert.That(header.Version, Is.EqualTo(1));
        }

        // ---------------------------------------------------------------- the file is preserved

        /// <summary>
        /// Loading through the store must leave the v1 file byte for byte as it was: no rewrite, no <c>.corrupt</c>,
        /// no <c>.bak</c>. The slot still lists, and the next save keeps the original as the <c>.bak</c>.
        /// </summary>
        [Test]
        public void LoadingAVersionOneSlotLeavesTheFileUntouchedAndTheNextSaveKeepsItAsTheBackup()
        {
            var (sim, _, v1) = Forge();
            var (fs, store) = PersistenceFixture.Store();
            var path = store.PathOf("old");
            fs.PutText(path, v1);

            var r = store.Load("old", sim.Context.Data);
            Assert.That(r.Ok, Is.True, r.Reason);
            Assert.That(r.Upgraded, Is.Not.Null);
            Assert.That(fs.Text(path), Is.EqualTo(v1), "the load rewrote the file");
            Assert.That(fs.AllPaths().Count, Is.EqualTo(1), "the load created or renamed something: " + string.Join(", ", fs.AllPaths()));
            Assert.That(fs.Has(path), Is.True);

            var list = store.List();
            Assert.That(list.Count, Is.EqualTo(1));
            Assert.That(list[0].Ok, Is.True, list[0].Problem);
            Assert.That(list[0].Header.Version, Is.EqualTo(1));

            Assert.That(store.Save("old", r.State, sim.Context.Data).Ok, Is.True);
            Assert.That(fs.Text(path + AtomicWrite.BackupSuffix), Is.EqualTo(v1), "the original survives as the .bak");
            var header = SaveSerializer.ReadHeader(fs.Bytes(path), out var problem);
            Assert.That(header, Is.Not.Null, problem);
            Assert.That(header.Version, Is.EqualTo(SaveSchema.Version));
        }

        [Test]
        public void AVersionOneAutosaveIsReadInTheRingAndNotTreatedAsRubbish()
        {
            var (sim, _, v1) = Forge();
            var (fs, store) = PersistenceFixture.Store();
            var slot1 = Path.Combine(store.Autosaves.Directory, AutosaveStore.SlotFile(1));
            fs.PutText(slot1, v1);

            var list = store.Autosaves.ListRecoverable();
            Assert.That(list.Count, Is.EqualTo(1));
            Assert.That(string.IsNullOrEmpty(list[0].Problem), Is.True, "a v1 autosave has a readable header: " + list[0].Problem);
            Assert.That(store.Autosaves.ChooseSlot(store.Autosaves.ReadIndex(), 3), Is.EqualTo(AutosaveStore.SlotFile(2)),
                "an empty slot is taken before a v1 autosave, which is a working save");

            var r = store.Autosaves.LoadNewest(sim.Context.Data);
            Assert.That(r.Ok, Is.True, r.Reason);
            Assert.That(r.Upgraded, Is.Not.Null);
            Assert.That(fs.Text(slot1), Is.EqualTo(v1));
        }

        // ---------------------------------------------------------------- what is refused

        /// <summary>The v1 checksum is verified over the document as written, before anything is defaulted.</summary>
        [Test]
        public void ATamperedVersionOneSaveIsRefusedAsDamagedNotUpgraded()
        {
            var (sim, _, v1) = Forge();
            var tampered = PersistenceFixture.RetargetInState(v1, "seed", "999999");
            var r = SaveSerializer.ReadText(tampered, sim.Context.Data);
            Assert.That(r.Ok, Is.False);
            Assert.That(r.Reason, Does.Contain("do not match its checksum"));
            Assert.That(r.Damaged, Is.True);
        }

        [Test]
        public void AVersionOneSaveWithNoChecksumIsRefused()
        {
            var (sim, _, v1) = Forge();
            var r = SaveSerializer.ReadText(PersistenceFixture.Retarget(v1, "hash", "\"\""), sim.Context.Data);
            Assert.That(r.Ok, Is.False);
            Assert.That(r.Reason, Does.Contain("integrity check"));
        }

        /// <summary>A file that calls itself version 1 but carries version-2 members is not honest; it is left alone.</summary>
        [Test]
        public void AFileThatClaimsVersionOneButCarriesTheNewFieldsIsRefusedAndLeftAlone()
        {
            var (sim, v2, _) = Forge();
            var mislabelled = PersistenceFixture.Retarget(v2, "version", "1");
            var r = SaveSerializer.ReadText(mislabelled, sim.Context.Data);
            Assert.That(r.Ok, Is.False);
            Assert.That(r.Reason, Does.Contain("version 1"));
            Assert.That(r.Reason, Does.Contain("not upgraded"));
            Assert.That(r.Damaged, Is.False, "nothing is recovered from a file that is simply not what it says");

            var (fs, store) = PersistenceFixture.Store();
            fs.PutText(store.PathOf("odd"), mislabelled);
            Assert.That(store.Load("odd", sim.Context.Data).Ok, Is.False);
            Assert.That(fs.AllPaths().Count, Is.EqualTo(1), "left exactly where it was: " + string.Join(", ", fs.AllPaths()));
            Assert.That(fs.Has(store.PathOf("odd")), Is.True);
        }

        [Test]
        public void VersionZeroAndTheFutureAreStillRefusedByNumber()
        {
            var (sim, v2, _) = Forge();
            var zero = SaveSerializer.ReadText(PersistenceFixture.Retarget(v2, "version", "0"), sim.Context.Data);
            Assert.That(zero.Ok, Is.False);
            Assert.That(zero.Reason, Does.Contain("unsupported save version 0"));
            Assert.That(zero.Reason, Does.Contain("upgrades version 1"), "the message says what it can read");
            var future = SaveSerializer.ReadText(PersistenceFixture.Retarget(v2, "version", "3"), sim.Context.Data);
            Assert.That(future.Ok, Is.False);
            Assert.That(future.Reason, Does.Contain("unsupported save version 3"));
            Assert.That(future.Damaged, Is.False);
        }
    }
}
