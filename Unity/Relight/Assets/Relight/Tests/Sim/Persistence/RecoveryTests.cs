using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Relight.Sim.Tests.Support;

namespace Relight.Sim.Tests.Persistence
{
    /// <summary>
    /// Recovery after the two failures a save system has to survive on a real machine: the newest copy of a file is
    /// gone or broken, and a write that succeeded only half way through its bookkeeping
    /// (TECHNICAL_ARCHITECTURE.md §9.4.4–§9.4.5).
    ///
    /// The tests here are the ones the round-trip and refusal suites cannot give, because both of those ask a single
    /// question once. What matters after a real crash is that the answer is still right the <b>second</b> time and
    /// after a restart — a recovery that works once and then loses the save is worse than no recovery, because the
    /// player was told it worked.
    /// </summary>
    public sealed class RecoveryTests
    {
        private static (MemoryFileSystem Fs, SaveStore Store, Simulation Sim) Setup(int ticks = 200)
        {
            var (fs, store) = PersistenceFixture.Store();
            var (sim, _) = Scenarios.ShortRun().Play(ticks);
            return (fs, store, sim);
        }

        /// <summary>Save twice, so the write protocol has made a <c>.bak</c>, and return the state in that backup.</summary>
        private static string SaveTwice(SaveStore store, Simulation sim, string name)
        {
            Assert.That(store.Save(name, sim.State, sim.Context.Data).Ok, Is.True);
            var inBackup = sim.Hash();
            SimTestUtil.Step(sim, 60);
            Assert.That(store.Save(name, sim.State, sim.Context.Data).Ok, Is.True, "the second save makes the .bak");
            return inBackup;
        }

        private static void Damage(MemoryFileSystem fs, string path) => fs.PutText(path, "{ this is not json");

        // ---------------------------------------------------------------- F3: the manual slots

        /// <summary>
        /// The defect that made recovery a one-shot: loading from the <c>.bak</c> left the primary missing, so the
        /// very next load said "there is no save there" and the slot had vanished from the list. A recovery must
        /// put the file back.
        /// </summary>
        [Test]
        public void RecoveringFromTheBackupWorksTwiceAndLeavesTheSlotWhereItWas()
        {
            var (fs, store, sim) = Setup();
            var inBackup = SaveTwice(store, sim, "slot");

            Damage(fs, store.PathOf("slot"));
            var first = store.Load("slot", sim.Context.Data);
            Assert.That(first.Ok, Is.True, "the previous copy must be offered: " + first.Reason);
            Assert.That(first.Recovered, Does.Contain("damaged"));
            Assert.That(StateHash.Compute(first.State), Is.EqualTo(inBackup));
            Assert.That(fs.Has(store.PathOf("slot")), Is.True, "the primary must be restored from the copy that worked");
            Assert.That(fs.Has(store.PathOf("slot") + AtomicWrite.CorruptSuffix), Is.True, "and the damage kept aside");

            var second = store.Load("slot", sim.Context.Data);
            Assert.That(second.Ok, Is.True, "the second load must still find the save: " + second.Reason);
            Assert.That(second.Recovered, Is.Null.Or.Empty, "and by then it is an ordinary load, not a recovery");
            Assert.That(StateHash.Compute(second.State), Is.EqualTo(inBackup));

            Assert.That(store.Exists("slot"), Is.True);
            var list = store.List();
            Assert.That(list.Count, Is.EqualTo(1), "the slot is still listed after the recovery");
            Assert.That(list[0].Ok, Is.True);
        }

        /// <summary>The same, across a restart: a new store over the same disk recovers exactly as the first did.</summary>
        [Test]
        public void RecoveryStillWorksAfterARestart()
        {
            var (fs, store, sim) = Setup();
            var inBackup = SaveTwice(store, sim, "slot");
            Damage(fs, store.PathOf("slot"));

            var restarted = new SaveStore(fs, PersistenceFixture.Root);
            var r = restarted.Load("slot", sim.Context.Data);
            Assert.That(r.Ok, Is.True, r.Reason);
            Assert.That(StateHash.Compute(r.State), Is.EqualTo(inBackup));

            var again = new SaveStore(fs, PersistenceFixture.Root);
            Assert.That(again.Exists("slot"), Is.True, "and the slot is still there for the session after that");
            var r2 = again.Load("slot", sim.Context.Data);
            Assert.That(r2.Ok, Is.True, r2.Reason);
            Assert.That(StateHash.Compute(r2.State), Is.EqualTo(inBackup));
        }

        /// <summary>
        /// The primary simply not there — a crash between the temp write and the replace, or a file a sync agent
        /// removed. The previous copy is a save the player made; it must be visible and loadable, not reported as
        /// "there is no save there".
        /// </summary>
        [Test]
        public void ASaveThatExistsOnlyAsABackupIsVisibleAndLoadable()
        {
            var (fs, store, sim) = Setup();
            var inBackup = SaveTwice(store, sim, "slot");
            fs.Delete(store.PathOf("slot"));

            Assert.That(store.Exists("slot"), Is.True, "a slot with only its previous copy still exists");
            var list = store.List();
            Assert.That(list.Count, Is.EqualTo(1), "and is listed rather than hidden");
            Assert.That(list[0].Name, Is.EqualTo("slot"));
            Assert.That(list[0].Ok, Is.True, "with the header of the copy that can actually be loaded");

            var r = store.Load("slot", sim.Context.Data);
            Assert.That(r.Ok, Is.True, r.Reason);
            Assert.That(r.Recovered, Is.Not.Null.And.Not.Empty, "the player is told an older copy was used");
            Assert.That(StateHash.Compute(r.State), Is.EqualTo(inBackup));
            Assert.That(fs.Has(store.PathOf("slot")), Is.True, "and the primary is put back");
        }

        /// <summary>
        /// Restoring the primary is a courtesy, never a risk: if that write fails the last good copy must still be
        /// the <c>.bak</c>, the load must still succeed, and the player must be told the file could not be rewritten.
        /// </summary>
        [Test]
        public void AFailedRestoreKeepsTheBackupAndStillLoads()
        {
            var (fs, store, sim) = Setup();
            var inBackup = SaveTwice(store, sim, "slot");
            var backup = store.PathOf("slot") + AtomicWrite.BackupSuffix;
            var backupBytes = fs.Text(backup);

            Damage(fs, store.PathOf("slot"));
            fs.FailWrites(9, "slot-slot");                 // every attempt to put the primary back fails
            var r = store.Load("slot", sim.Context.Data);

            Assert.That(r.Ok, Is.True, "a failed restore must not fail the load: " + r.Reason);
            Assert.That(StateHash.Compute(r.State), Is.EqualTo(inBackup));
            Assert.That(r.Recovered, Does.Contain("could not"), "and says what could not be done: " + r.Recovered);
            Assert.That(fs.Text(backup), Is.EqualTo(backupBytes), "the last good copy is untouched");
            Assert.That(fs.Has(store.PathOf("slot") + AtomicWrite.TempSuffix), Is.False, "and no temp is left behind");

            fs.ClearFaults();
            var again = store.Load("slot", sim.Context.Data);
            Assert.That(again.Ok, Is.True, "the save is still recoverable on the next try: " + again.Reason);
            Assert.That(StateHash.Compute(again.State), Is.EqualTo(inBackup));
        }

        /// <summary>The refusals that must survive the repair: nothing there at all, and damage with no way back.</summary>
        [Test]
        public void NoSaveAndNoUsableCopyAreStillRefused()
        {
            var (fs, store, sim) = Setup();

            var missing = store.Load("never saved", sim.Context.Data);
            Assert.That(missing.Ok, Is.False);
            Assert.That(missing.Reason, Does.Contain("no save there"));

            Assert.That(store.Save("lonely", sim.State, sim.Context.Data).Ok, Is.True);
            Damage(fs, store.PathOf("lonely"));
            var noBackup = store.Load("lonely", sim.Context.Data);
            Assert.That(noBackup.Ok, Is.False, "a damaged file with no previous copy cannot be recovered");
            Assert.That(fs.Has(store.PathOf("lonely") + AtomicWrite.CorruptSuffix), Is.True, "but it is kept for inspection");

            SaveTwice(store, sim, "both");
            Damage(fs, store.PathOf("both"));
            Damage(fs, store.PathOf("both") + AtomicWrite.BackupSuffix);
            var bothBroken = store.Load("both", sim.Context.Data);
            Assert.That(bothBroken.Ok, Is.False, "and neither can two damaged copies");
            Assert.That(bothBroken.Reason, Is.Not.Empty);
        }

        // ---------------------------------------------------------------- F4: the autosave index

        private static AutosaveScheduler Ring(SaveStore store, int slots)
            => new AutosaveScheduler(store, new AutosaveSettings(AutosaveSettings.DefaultIntervalMinutes, slots));

        private static string SlotPath(SaveStore store, int i)
            => Path.Combine(store.Autosaves.Directory, AutosaveStore.SlotFile(i));

        private static int TickIn(MemoryFileSystem fs, string path)
        {
            var header = SaveSerializer.ReadHeader(fs.Bytes(path), out var problem);
            Assert.That(header, Is.Not.Null, path + ": " + problem);
            return header.Tick;
        }

        /// <summary>
        /// The specific crash F4 describes: the ring slot was replaced and then the index write failed, so the index
        /// on disk still describes the <i>old</i> contents of that slot. An index is only evidence about the files;
        /// when it disagrees with them the files win — otherwise the newest autosave is hidden from "continue" and
        /// is the first slot the next rotation destroys.
        /// </summary>
        [Test]
        public void AFailedIndexWriteDoesNotHideOrDestroyTheNewestAutosave()
        {
            var (fs, store, sim) = Setup(100);
            var sched = Ring(store, 3);
            for (var i = 0; i < 3; i++)
            {
                SimTestUtil.Step(sim, 30);
                Assert.That(sched.Trigger("fill " + i, sim).Ok, Is.True);
            }

            // The fourth autosave lands in slot 1 (the oldest) and then the index write fails.
            SimTestUtil.Step(sim, 30);
            fs.FailWrites(1, AutosaveStore.IndexFile);
            var newest = sched.Trigger("index write fails", sim);
            Assert.That(newest.Ok, Is.True, "the slot itself was written: " + newest.Reason);
            Assert.That(newest.Notice, Does.Contain("index"), "and the failed bookkeeping is reported: " + newest.Notice);
            fs.ClearFaults();
            var newestTick = sim.State.Tick;
            var newestFile = Path.GetFileName(newest.Path);
            var newestHash = sim.Hash();

            // 1. Continue loads it.
            var loaded = store.Autosaves.LoadNewest(sim.Context.Data);
            Assert.That(loaded.Ok, Is.True, loaded.Reason);
            Assert.That(StateHash.Compute(loaded.State), Is.EqualTo(newestHash), "'continue' must get the newest autosave");

            // 2. The recovery list puts it first.
            var list = store.Autosaves.ListRecoverable();
            Assert.That(list.Count, Is.GreaterThanOrEqualTo(3));
            Assert.That(list[0].File, Is.EqualTo(newestFile), "the newest autosave heads the list");
            Assert.That(list[0].Tick, Is.EqualTo(newestTick), "with the header the file actually has");

            // 3. The next rotation does not choose it — in this session or the one after a restart.
            var sameSession = store.Autosaves.ChooseSlot(store.Autosaves.ReadIndex(), 3);
            Assert.That(sameSession, Is.Not.EqualTo(newestFile), "the newest slot is not the next victim");

            var restarted = new SaveStore(fs, PersistenceFixture.Root);
            Assert.That(restarted.Autosaves.ChooseSlot(restarted.Autosaves.ReadIndex(), 3), Is.Not.EqualTo(newestFile),
                "nor after a restart");
            SimTestUtil.Step(sim, 30);
            var after = Ring(restarted, 3).Trigger("after the restart", sim);
            Assert.That(after.Ok, Is.True, after.Reason);
            Assert.That(Path.GetFileName(after.Path), Is.Not.EqualTo(newestFile));
            Assert.That(TickIn(fs, Path.Combine(store.Autosaves.Directory, newestFile)), Is.EqualTo(newestTick),
                "and the file still holds the state it held");
        }

        /// <summary>
        /// An index that parses but whose order contradicts the files: the sequence numbers say slot 1 is newest,
        /// the saved times on disk say it is oldest. The files are the fact.
        /// </summary>
        [Test]
        public void AnIndexWhoseOrderContradictsTheFilesIsCorrectedFromThem()
        {
            var (fs, store, sim) = Setup(100);
            var sched = Ring(store, 3);
            var ticks = new List<int>();
            for (var i = 0; i < 3; i++)
            {
                SimTestUtil.Step(sim, 30);
                ticks.Add(sim.State.Tick);
                Assert.That(sched.Trigger("fill " + i, sim).Ok, Is.True);
            }

            // Hand-written index: the right files, plausible numbers, exactly the wrong order and stale headers.
            fs.PutText(Path.Combine(store.Autosaves.Directory, AutosaveStore.IndexFile),
                "{\"entries\":["
                + "{\"file\":\"auto-1.json\",\"hash\":\"stale\",\"playSeconds\":0,\"savedAt\":\"2030-01-01T00:00:00.0000000Z\",\"seq\":30,\"tick\":9999},"
                + "{\"file\":\"auto-2.json\",\"hash\":\"stale\",\"playSeconds\":0,\"savedAt\":\"2029-01-01T00:00:00.0000000Z\",\"seq\":20,\"tick\":8888},"
                + "{\"file\":\"auto-3.json\",\"hash\":\"stale\",\"playSeconds\":0,\"savedAt\":\"2028-01-01T00:00:00.0000000Z\",\"seq\":10,\"tick\":7777}"
                + "],\"next\":31,\"version\":1}");

            var list = store.Autosaves.ListRecoverable();
            Assert.That(list.Count, Is.EqualTo(3));
            Assert.That(list[0].File, Is.EqualTo(AutosaveStore.SlotFile(3)), "the newest file is newest, whatever the index says");
            Assert.That(list[0].Tick, Is.EqualTo(ticks[2]), "and is described by its own header, not the stale entry");
            Assert.That(list[2].File, Is.EqualTo(AutosaveStore.SlotFile(1)));

            var loaded = store.Autosaves.LoadNewest(sim.Context.Data);
            Assert.That(loaded.Ok, Is.True, loaded.Reason);
            Assert.That(loaded.State.Tick, Is.EqualTo(ticks[2]));

            Assert.That(store.Autosaves.ChooseSlot(store.Autosaves.ReadIndex(), 3), Is.EqualTo(AutosaveStore.SlotFile(1)),
                "and the genuinely oldest slot is the next victim");
        }

        /// <summary>An index entry for a file that is no longer there is dropped rather than offered to the player.</summary>
        [Test]
        public void AnIndexEntryForAMissingFileIsDropped()
        {
            var (fs, store, sim) = Setup(100);
            var sched = Ring(store, 3);
            for (var i = 0; i < 3; i++)
            {
                SimTestUtil.Step(sim, 30);
                Assert.That(sched.Trigger("fill " + i, sim).Ok, Is.True);
            }

            fs.Delete(SlotPath(store, 2));
            var index = store.Autosaves.ReadIndex();
            Assert.That(index.Find(AutosaveStore.SlotFile(2)), Is.Null, "an entry with no file is not a save");

            var list = store.Autosaves.ListRecoverable();
            Assert.That(list.Count, Is.EqualTo(2));
            Assert.That(store.Autosaves.ChooseSlot(index, 3), Is.EqualTo(AutosaveStore.SlotFile(2)), "the hole is filled first");
        }

        /// <summary>A ring slot that exists only as a <c>.bak</c> is still an autosave, and still loadable.</summary>
        [Test]
        public void ARingSlotThatExistsOnlyAsABackupIsStillRecoverable()
        {
            var (fs, store, sim) = Setup(100);
            var sched = Ring(store, 1);
            SimTestUtil.Step(sim, 30);
            Assert.That(sched.Trigger("first", sim).Ok, Is.True);
            var inBackup = sim.Hash();
            SimTestUtil.Step(sim, 30);
            Assert.That(sched.Trigger("second", sim).Ok, Is.True, "the second write makes a .bak of slot 1");

            var slot = SlotPath(store, 1);
            Assert.That(fs.Has(slot + AtomicWrite.BackupSuffix), Is.True);
            fs.Delete(slot);

            var list = store.Autosaves.ListRecoverable();
            Assert.That(list.Count, Is.EqualTo(1), "the slot is still an autosave");
            var loaded = store.Autosaves.LoadNewest(sim.Context.Data);
            Assert.That(loaded.Ok, Is.True, loaded.Reason);
            Assert.That(StateHash.Compute(loaded.State), Is.EqualTo(inBackup));
        }
    }
}
