using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Relight.Sim.Tests.Support;

namespace Relight.Sim.Tests.Persistence
{
    /// <summary>
    /// The autosave ring: its defaults, its rotation, the guard that keeps it out of the manual slots, and the
    /// failure policy of TECHNICAL_ARCHITECTURE.md §9.4.5. Every test here is a sentence from §9.4.2/§9.4.5 turned
    /// into an assertion, because a save system's failure paths are the part nobody exercises by playing.
    /// </summary>
    public sealed class AutosaveTests
    {
        private static (MemoryFileSystem Fs, SaveStore Store, AutosaveScheduler Sched, Simulation Sim) Setup(
            int slots = 3, double intervalMinutes = AutosaveSettings.DefaultIntervalMinutes)
        {
            var (fs, store) = PersistenceFixture.Store();
            var sched = new AutosaveScheduler(store, new AutosaveSettings(intervalMinutes, slots));
            var (sim, _) = Scenarios.ShortRun().Play(100);
            return (fs, store, sched, sim);
        }

        /// <summary>The ring files that exist (<c>auto-1.json</c>…), excluding the index and the quit save.</summary>
        private static List<string> RingFiles(MemoryFileSystem fs, SaveStore store)
        {
            var names = fs.ListFiles(store.Autosaves.Directory);
            var ring = new List<string>();
            for (var i = 0; i < names.Count; i++)
            {
                var n = names[i];
                if (!n.StartsWith(AutosaveStore.SlotPrefix, System.StringComparison.Ordinal)) continue;
                if (!n.EndsWith(SaveSchema.Extension, System.StringComparison.Ordinal)) continue;
                if (n == AutosaveStore.IndexFile || n == AutosaveStore.QuitFile) continue;
                ring.Add(n);
            }
            return ring;
        }

        private static int TickIn(MemoryFileSystem fs, SaveStore store, string file)
        {
            var header = SaveSerializer.ReadHeader(fs.Bytes(Path.Combine(store.Autosaves.Directory, file)), out var problem);
            Assert.That(header, Is.Not.Null, file + ": " + problem);
            return header.Tick;
        }

        // ---------------------------------------------------------------- defaults

        /// <summary>The recorded defaults of U-D-35 / §9.4.2, and the ranges a hand-edited settings file is held to.</summary>
        [Test]
        public void TheRecordedDefaultsAreFiveMinutesAndThreeSlots()
        {
            var d = AutosaveSettings.Default;
            Assert.That(d.IntervalMinutes, Is.EqualTo(5));
            Assert.That(d.Slots, Is.EqualTo(3));
            Assert.That(d.OnEvents, Is.True);
            Assert.That(d.SaveOnQuit, Is.True);
            Assert.That(d.IntervalSeconds, Is.EqualTo(300));

            Assert.That(new AutosaveSettings(0.1, 99).Clamped().IntervalMinutes, Is.EqualTo(1), "the floor is one minute");
            Assert.That(new AutosaveSettings(99, 99).Clamped().IntervalMinutes, Is.EqualTo(30), "the ceiling is thirty");
            Assert.That(new AutosaveSettings(5, 99).Clamped().Slots, Is.EqualTo(10));
            Assert.That(new AutosaveSettings(5, 0).Clamped().Slots, Is.EqualTo(1));
            Assert.That(new AutosaveSettings(0, 3).Clamped().TimedEnabled, Is.False, "off stays off after clamping");
        }

        // ---------------------------------------------------------------- the ring

        /// <summary>
        /// The ring keeps N and drops the oldest: five autosaves into three slots leave three files, holding the
        /// three most recent states. Nothing accumulates, and nothing newer is thrown away in favour of something
        /// older.
        /// </summary>
        [Test]
        public void TheRingKeepsThreeSlotsAndOverwritesTheOldest()
        {
            var (fs, store, sched, sim) = Setup(slots: 3);
            var ticks = new List<int>();
            for (var i = 0; i < 5; i++)
            {
                SimTestUtil.Step(sim, 20);
                ticks.Add(sim.State.Tick);
                var r = sched.Trigger("test " + i, sim);
                Assert.That(r.Ok, Is.True, "autosave " + i + ": " + r.Reason);
            }

            var ring = RingFiles(fs, store);
            Assert.That(ring.Count, Is.EqualTo(3), "three slots, five saves: " + string.Join(", ", ring));

            var kept = new List<int>();
            for (var i = 0; i < ring.Count; i++) kept.Add(TickIn(fs, store, ring[i]));
            kept.Sort();
            Assert.That(kept, Is.EqualTo(new List<int> { ticks[2], ticks[3], ticks[4] }),
                "the three kept autosaves must be the three newest");
        }

        /// <summary>The newest autosave is the one a "continue" loads, and it is the state that was saved.</summary>
        [Test]
        public void LoadingTheNewestAutosaveGivesTheStateThatWasSaved()
        {
            var (_, store, sched, sim) = Setup(slots: 3);
            for (var i = 0; i < 4; i++)
            {
                SimTestUtil.Step(sim, 25);
                Assert.That(sched.Trigger("test", sim).Ok, Is.True);
            }
            var expected = sim.Hash();

            var loaded = store.Autosaves.LoadNewest(sim.Context.Data);
            Assert.That(loaded.Ok, Is.True, loaded.Reason);
            Assert.That(StateHash.Compute(loaded.State), Is.EqualTo(expected));
            Assert.That(loaded.State.Tick, Is.EqualTo(sim.State.Tick));
        }

        /// <summary>
        /// A lost or damaged index must not cost the player their newest autosave. Without the rebuild, slot 1 would
        /// look oldest and be the next thing overwritten — which is exactly the save they would want back.
        /// </summary>
        [Test]
        public void ALostIndexIsRebuiltFromTheFilesRatherThanRestartingAtSlotOne()
        {
            var (fs, store, sched, sim) = Setup(slots: 3);
            for (var i = 0; i < 3; i++)
            {
                SimTestUtil.Step(sim, 30);
                Assert.That(sched.Trigger("test", sim).Ok, Is.True);
            }
            var newest = sim.State.Tick;

            fs.Delete(Path.Combine(store.Autosaves.Directory, AutosaveStore.IndexFile));
            var rebuilt = new SaveStore(fs, PersistenceFixture.Root);

            var chosen = rebuilt.Autosaves.ChooseSlot(rebuilt.Autosaves.ReadIndex(), 3);
            Assert.That(TickIn(fs, rebuilt, chosen), Is.Not.EqualTo(newest), "the newest autosave is not the next victim");

            var loaded = rebuilt.Autosaves.LoadNewest(sim.Context.Data);
            Assert.That(loaded.Ok, Is.True, loaded.Reason);
            Assert.That(loaded.State.Tick, Is.EqualTo(newest), "and 'load the newest' still finds the newest");
        }

        // ---------------------------------------------------------------- the manual slots are out of reach

        /// <summary>
        /// The headline rule: an autosave never writes a manual slot. Ten autosaves run beside a manual save, and
        /// the manual file's bytes are identical afterwards — no rotation, no backup, no temp file next to it.
        /// </summary>
        [Test]
        public void AnAutosaveNeverWritesAManualSlot()
        {
            var (fs, store, sched, sim) = Setup(slots: 3);
            Assert.That(store.Save("my game", sim.State, sim.Context.Data).Ok, Is.True);
            var manual = store.PathOf("my game");
            var before = fs.Text(manual);

            for (var i = 0; i < 10; i++)
            {
                SimTestUtil.Step(sim, 10);
                Assert.That(sched.Trigger("test", sim).Ok, Is.True);
            }

            Assert.That(fs.Text(manual), Is.EqualTo(before), "the manual save's bytes must be untouched");
            var inProfile = fs.ListFiles(store.Directory);
            Assert.That(inProfile, Is.EqualTo(new[] { "slot-my game.json" }),
                "nothing but the manual save may appear in the profile folder: " + string.Join(", ", inProfile));
        }

        /// <summary>
        /// The mechanical half of that guarantee: the ring resolves names against its own directory and refuses
        /// anything that would leave it, so no caller — and no corrupt index — can aim a write at a manual slot.
        /// </summary>
        [Test]
        public void TheRingRefusesAnyPathOutsideItsOwnFolder()
        {
            var (_, store, _, _) = Setup();
            var ring = store.Autosaves;

            foreach (var name in new[]
                     {
                         "../slot-my game.json", "..", ".", "auto/../../slot-x.json",
                         "sub/auto-1.json", "auto-1/../../auto-1.json", "C:auto-1.json", "\\auto-1.json",
                     })
            {
                var path = ring.Resolve(name, out var problem);
                Assert.That(path, Is.Null, "'" + name + "' must be refused");
                Assert.That(problem, Is.Not.Empty);
            }

            Assert.That(ring.Resolve("slot-my game.json", out _), Is.Null, "a manual file name is not a ring name");
            Assert.That(ring.Resolve(AutosaveStore.SlotFile(1), out _), Is.Not.Null, "an ordinary ring name is fine");
            Assert.That(ring.Resolve(AutosaveStore.QuitFile, out _), Is.Not.Null);
        }

        /// <summary>An index is a file on disk, so it is untrusted input: an entry pointing elsewhere is dropped.</summary>
        [Test]
        public void AnIndexNamingAFileOutsideTheRingIsIgnored()
        {
            var (fs, store, sched, sim) = Setup(slots: 2);
            SimTestUtil.Step(sim, 20);
            Assert.That(sched.Trigger("test", sim).Ok, Is.True);

            fs.PutText(Path.Combine(store.Autosaves.Directory, AutosaveStore.IndexFile),
                "{\"entries\":[{\"file\":\"../slot-my game.json\",\"hash\":\"\",\"playSeconds\":0,\"savedAt\":\"2030-01-01T00:00:00.0000000Z\",\"seq\":99,\"tick\":0}],\"next\":100,\"version\":1}");

            var index = store.Autosaves.ReadIndex();
            Assert.That(index.Find("../slot-my game.json"), Is.Null, "the outside entry must not survive parsing");

            SimTestUtil.Step(sim, 20);
            var r = sched.Trigger("test", sim);
            Assert.That(r.Ok, Is.True, r.Reason);
            Assert.That(r.Path, Does.StartWith(store.Autosaves.Directory), "and the write still lands in the ring");
        }

        // ---------------------------------------------------------------- unpaused time

        /// <summary>
        /// §9.4.2: the interval is unpaused sim time, not wall clock. A paused game feeds no seconds, so no amount
        /// of sitting in a menu triggers an autosave — and the same seconds are what playtime counts.
        /// </summary>
        [Test]
        public void TheIntervalCountsOnlyUnpausedSimTime()
        {
            var (_, _, sched, sim) = Setup(slots: 3, intervalMinutes: 1);

            for (var i = 0; i < 1000; i++)
                Assert.That(sched.Advance(0, sim), Is.Null, "a paused frame buys nothing");
            Assert.That(sim.State.PlaySeconds, Is.EqualTo(0), "and counts as no playtime");

            Assert.That(sched.Advance(59.9, sim), Is.Null, "59.9 s of a 60 s interval is not yet due");
            Assert.That(sched.SecondsUntilSave, Is.EqualTo(0.1).Within(1e-9));

            var r = sched.Advance(0.2, sim);
            Assert.That(r, Is.Not.Null, "the interval must fire when it is reached");
            Assert.That(r.Ok, Is.True, r.Reason);
            Assert.That(sim.State.PlaySeconds, Is.EqualTo(60.1).Within(1e-9), "playtime is the same unpaused seconds");
            Assert.That(sched.SecondsUntilSave, Is.EqualTo(60).Within(1e-9), "and the clock starts again");
        }

        /// <summary>A long stall must not fire a burst of autosaves: one save per due point, whatever arrives.</summary>
        [Test]
        public void ALongStallProducesOneAutosaveNotABurst()
        {
            var (fs, store, sched, sim) = Setup(slots: 3, intervalMinutes: 1);
            var r = sched.Advance(600, sim);      // ten intervals in one go
            Assert.That(r.Ok, Is.True, r.Reason);
            Assert.That(RingFiles(fs, store).Count, Is.EqualTo(1));
        }

        [Test]
        public void AutosavingCanBeTurnedOff()
        {
            var (fs, store, sched, sim) = Setup(slots: 3, intervalMinutes: 0);
            Assert.That(sched.Settings.TimedEnabled, Is.False);
            Assert.That(sched.Advance(10000, sim), Is.Null, "nothing is written when it is off");
            Assert.That(RingFiles(fs, store), Is.Empty);
            Assert.That(sim.State.PlaySeconds, Is.EqualTo(10000).Within(1e-9), "but playtime still counts");
        }

        // ---------------------------------------------------------------- save on quit

        /// <summary>Save-on-quit is outside the ring, so quitting never costs a ring slot (§9.4.2).</summary>
        [Test]
        public void SaveOnQuitWritesItsOwnFileAndNotARingSlot()
        {
            var (fs, store, sched, sim) = Setup(slots: 3);
            SimTestUtil.Step(sim, 40);
            Assert.That(sched.Trigger("test", sim).Ok, Is.True);
            var ringBefore = RingFiles(fs, store);

            SimTestUtil.Step(sim, 40);
            var quit = sched.SaveOnQuit(sim);
            Assert.That(quit.Ok, Is.True, quit.Reason);
            Assert.That(Path.GetFileName(quit.Path), Is.EqualTo(AutosaveStore.QuitFile));
            Assert.That(RingFiles(fs, store), Is.EqualTo(ringBefore), "the ring is untouched by quitting");

            var loaded = store.Autosaves.Load(AutosaveStore.QuitFile, sim.Context.Data);
            Assert.That(loaded.Ok, Is.True, loaded.Reason);
            Assert.That(StateHash.Compute(loaded.State), Is.EqualTo(sim.Hash()));
        }

        // ---------------------------------------------------------------- failure policy (§9.4.5)

        /// <summary>
        /// The acceptance case: a write that fails part way through leaves the previous autosave exactly as it was,
        /// and loadable. The half-written temp file is deleted rather than left to be mistaken for a save.
        /// </summary>
        [Test]
        public void AnInjectedWriteFailureLeavesThePreviousAutosaveLoadable()
        {
            var (fs, store, sched, sim) = Setup(slots: 1);
            SimTestUtil.Step(sim, 30);
            Assert.That(sched.Trigger("first", sim).Ok, Is.True);
            var good = sim.Hash();
            var slot = Path.Combine(store.Autosaves.Directory, AutosaveStore.SlotFile(1));
            var goodBytes = fs.Text(slot);

            SimTestUtil.Step(sim, 30);
            fs.FailWrites(1, AutosaveStore.SlotPrefix);
            var failed = sched.Trigger("second", sim);

            Assert.That(failed.Ok, Is.False, "the injected failure must be reported, not swallowed");
            Assert.That(failed.Reason, Does.Contain("disk is full"), "and described in words: " + failed.Reason);
            Assert.That(sched.Notice, Does.Contain("the previous autosave is still there"));
            Assert.That(fs.Text(slot), Is.EqualTo(goodBytes), "the previous autosave is byte-for-byte untouched");
            Assert.That(fs.Has(slot + AtomicWrite.TempSuffix), Is.False, "the half-written temp file is cleaned up");

            fs.ClearFaults();
            var loaded = store.Autosaves.LoadNewest(sim.Context.Data);
            Assert.That(loaded.Ok, Is.True, "and it still loads: " + loaded.Reason);
            Assert.That(StateHash.Compute(loaded.State), Is.EqualTo(good));
        }

        /// <summary>The superseded copy the protocol keeps survives a failure too, so there are two ways back.</summary>
        [Test]
        public void AFailedWriteLeavesTheBackupCopyIntact()
        {
            var (fs, store, sched, sim) = Setup(slots: 1);
            SimTestUtil.Step(sim, 20);
            Assert.That(sched.Trigger("first", sim).Ok, Is.True);
            SimTestUtil.Step(sim, 20);
            Assert.That(sched.Trigger("second", sim).Ok, Is.True);

            var slot = Path.Combine(store.Autosaves.Directory, AutosaveStore.SlotFile(1));
            var backup = slot + AtomicWrite.BackupSuffix;
            Assert.That(fs.Has(backup), Is.True, "replacing a slot keeps the superseded file");
            var backupBytes = fs.Text(backup);
            var slotBytes = fs.Text(slot);

            SimTestUtil.Step(sim, 20);
            fs.FailWrites(1, AutosaveStore.SlotPrefix);
            Assert.That(sched.Trigger("third", sim).Ok, Is.False);

            Assert.That(fs.Text(slot), Is.EqualTo(slotBytes));
            Assert.That(fs.Text(backup), Is.EqualTo(backupBytes));
        }

        /// <summary>
        /// A locked temp file is transient: the next attempt uses a fresh temp name rather than the one something
        /// else is holding, and nothing spins or sleeps waiting for it (§9.4.5).
        /// </summary>
        [Test]
        public void ARetryAfterALockUsesAFreshTempName()
        {
            var (fs, store, sched, sim) = Setup(slots: 2);
            SimTestUtil.Step(sim, 20);
            fs.FailWritesAsLock(1, AutosaveStore.SlotPrefix);
            Assert.That(sched.Trigger("locked", sim).Ok, Is.False);

            fs.Writes.Clear();
            SimTestUtil.Step(sim, 20);
            var r = sched.Trigger("retry", sim);
            Assert.That(r.Ok, Is.True, r.Reason);

            var usedFreshTemp = false;
            for (var i = 0; i < fs.Writes.Count; i++)
                if (fs.Writes[i].Contains(AtomicWrite.TempSuffix + "-1")) usedFreshTemp = true;
            Assert.That(usedFreshTemp, Is.True, "the retry must not reuse the temp name that was locked");
        }

        /// <summary>
        /// Three consecutive failures stop the ring with one persistent notice instead of failing every interval
        /// forever, and a manual save that succeeds — the player's own proof that the disk works — resumes it.
        /// </summary>
        [Test]
        public void ThreeConsecutiveFailuresStopTheRingAndAManualSaveResumesIt()
        {
            var (fs, store, sched, sim) = Setup(slots: 3);
            fs.FailWrites(3, AutosaveStore.SlotPrefix);

            for (var i = 0; i < 3; i++)
            {
                SimTestUtil.Step(sim, 10);
                var r = sched.Trigger("attempt " + i, sim);
                Assert.That(r.Ok, Is.False, "attempt " + i + " was meant to fail");
            }

            Assert.That(sched.ConsecutiveFailures, Is.EqualTo(AutosaveScheduler.FailuresBeforeStop));
            Assert.That(sched.Stopped, Is.True, "the ring gives up after three failures");
            Assert.That(sched.Notice, Does.Contain("autosaving has stopped"));
            Assert.That(sched.Notice, Does.Contain("by hand"), "the notice says how to get it back");

            fs.ClearFaults();
            SimTestUtil.Step(sim, 10);
            Assert.That(sched.Trigger("while stopped", sim), Is.Null, "a stopped ring does not keep trying");
            Assert.That(RingFiles(fs, store), Is.Empty);

            var manual = store.Save("by hand", sim.State, sim.Context.Data);
            Assert.That(manual.Ok, Is.True, manual.Reason);
            sched.OnManualSaveSucceeded();

            Assert.That(sched.Stopped, Is.False);
            Assert.That(sched.Notice, Is.Null, "the persistent notice clears");
            SimTestUtil.Step(sim, 10);
            var resumed = sched.Trigger("after the manual save", sim);
            Assert.That(resumed, Is.Not.Null);
            Assert.That(resumed.Ok, Is.True, resumed.Reason);
            Assert.That(RingFiles(fs, store).Count, Is.EqualTo(1));
        }

        /// <summary>A failure that is not the third does not stop anything: the next interval simply tries again.</summary>
        [Test]
        public void TwoFailuresDoNotStopTheRing()
        {
            var (fs, store, sched, sim) = Setup(slots: 3);
            fs.FailWrites(2, AutosaveStore.SlotPrefix);
            for (var i = 0; i < 2; i++)
            {
                SimTestUtil.Step(sim, 10);
                Assert.That(sched.Trigger("fail " + i, sim).Ok, Is.False);
            }
            Assert.That(sched.Stopped, Is.False);

            SimTestUtil.Step(sim, 10);
            var r = sched.Trigger("recovered", sim);
            Assert.That(r.Ok, Is.True, r.Reason);
            Assert.That(sched.ConsecutiveFailures, Is.EqualTo(0), "a success clears the count");
            Assert.That(sched.Notice, Is.Null);
        }

        /// <summary>
        /// A damaged newest autosave falls back to the next-newest slot rather than refusing to continue — §9.4.5's
        /// recovery order, and the reason the ring has more than one slot in the first place.
        /// </summary>
        [Test]
        public void ADamagedNewestAutosaveFallsBackToTheNextOldest()
        {
            var (fs, store, sched, sim) = Setup(slots: 3);
            SimTestUtil.Step(sim, 40);
            Assert.That(sched.Trigger("older", sim).Ok, Is.True);
            var older = sim.Hash();
            SimTestUtil.Step(sim, 40);
            Assert.That(sched.Trigger("newer", sim).Ok, Is.True);

            var newest = Path.Combine(store.Autosaves.Directory, AutosaveStore.SlotFile(2));
            fs.PutText(newest, "{\"kind\":\"relight-save-unity\",\"version\":1,\"state\":{\"tick\":");

            var loaded = store.Autosaves.LoadNewest(sim.Context.Data);
            Assert.That(loaded.Ok, Is.True, "an older autosave must still be offered: " + loaded.Reason);
            Assert.That(StateHash.Compute(loaded.State), Is.EqualTo(older));
            Assert.That(loaded.Recovered, Is.Not.Null.And.Not.Empty, "and the player is told what happened");
            Assert.That(fs.Has(newest + AtomicWrite.CorruptSuffix), Is.True, "the damaged file is kept for inspection");
        }

        /// <summary>Listing what can be recovered, newest first — what a "load an autosave" screen shows.</summary>
        [Test]
        public void RecoverableAutosavesAreListedNewestFirst()
        {
            var (_, store, sched, sim) = Setup(slots: 3);
            var ticks = new List<int>();
            for (var i = 0; i < 3; i++)
            {
                SimTestUtil.Step(sim, 15);
                ticks.Add(sim.State.Tick);
                Assert.That(sched.Trigger("test", sim).Ok, Is.True);
            }
            Assert.That(sched.SaveOnQuit(sim).Ok, Is.True);

            var list = store.Autosaves.ListRecoverable();
            Assert.That(list.Count, Is.EqualTo(4), "three ring slots and the quit save");
            Assert.That(list[0].File, Is.EqualTo(AutosaveStore.QuitFile), "the quit save is the most recent write");
            Assert.That(list[1].Tick, Is.EqualTo(ticks[2]));
            Assert.That(list[3].Tick, Is.EqualTo(ticks[0]));
        }

        // ---------------------------------------------------------------- the manual slot list

        [Test]
        public void ManualSavesAreListedNewestFirstWithTheirHeaders()
        {
            var (_, store, _, sim) = Setup();
            Assert.That(store.Save("first", sim.State, sim.Context.Data, "2026-01-01T00:00:00.0000000Z").Ok, Is.True);
            SimTestUtil.Step(sim, 40);
            Assert.That(store.Save("second", sim.State, sim.Context.Data, "2026-06-01T00:00:00.0000000Z").Ok, Is.True);

            var list = store.List();
            Assert.That(list.Count, Is.EqualTo(2));
            Assert.That(list[0].Name, Is.EqualTo("second"), "newest first");
            Assert.That(list[0].Header.Tick, Is.EqualTo(sim.State.Tick));
            Assert.That(list[0].Header.SimClock, Is.Not.Empty);
            Assert.That(list[1].Name, Is.EqualTo("first"));
        }

        [Test]
        public void AnUnreadableSlotIsListedWithItsProblemRatherThanHidden()
        {
            var (fs, store, _, sim) = Setup();
            Assert.That(store.Save("good", sim.State, sim.Context.Data).Ok, Is.True);
            fs.PutText(store.PathOf("bad"), "not json at all");

            var list = store.List();
            Assert.That(list.Count, Is.EqualTo(2));
            var bad = list[0].Name == "bad" ? list[0] : list[1];
            Assert.That(bad.Ok, Is.False);
            Assert.That(bad.Problem, Is.Not.Empty, "a slot the player can see must say what is wrong with it");
        }
    }
}
