using System;
using System.IO;
using System.Text;
using NUnit.Framework;
using Relight.Sim.Tests.Support;

namespace Relight.Sim.Tests.Persistence
{
    /// <summary>
    /// The one persistence check that touches a REAL disk (Phase B repair pass, 2026-09-12, audit finding F3).
    /// Every other persistence test runs against <see cref="MemoryFileSystem"/>; this one drives
    /// <see cref="SystemFileSystem"/> through a save → replace → corrupt → recover cycle in a throwaway folder under
    /// the system temp path, so the temp-then-replace protocol, <c>File.Replace</c>'s backup behaviour and the
    /// backup-only recovery are proven on the platform the test runs on (NTFS in the Unity editor, ext4 in the
    /// scratch runner). It is a real filesystem operation, not a simulated failure.
    /// </summary>
    public sealed class RealDiskRecoveryTests
    {
        private string _root;

        [SetUp]
        public void MakeFolder()
        {
            _root = Path.Combine(Path.GetTempPath(), "relight-recovery-" + Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void RemoveFolder()
        {
            try { if (Directory.Exists(_root)) Directory.Delete(_root, true); } catch { /* best effort */ }
        }

        [Test]
        public void ADamagedPrimaryOnARealDiskRecoversFromItsBackupRepeatedly()
        {
            var fs = new SystemFileSystem();
            var store = new SaveStore(fs, _root);
            var (sim, _) = Scenarios.ShortRun().Play(100);
            var data = sim.Context.Data;
            const string slot = "disk-recovery";
            var path = store.PathOf(slot);
            var backup = path + AtomicWrite.BackupSuffix;

            // 1. Save twice, so the second write replaces the first and the protocol keeps a .bak.
            Assert.That(store.Save(slot, sim.State, data).Ok, Is.True, "first real save");
            SimTestUtil.Step(sim, 40);
            var hash = sim.Hash();
            Assert.That(store.Save(slot, sim.State, data).Ok, Is.True, "second real save (replace)");
            Assert.That(File.Exists(path), Is.True, "the primary exists after the replace");
            Assert.That(File.Exists(backup), Is.True, "File.Replace kept the superseded file as .bak");
            Assert.That(File.Exists(path + AtomicWrite.TempSuffix), Is.False, "no temp file is left behind");

            // Make the backup the same generation as the primary (save a third time), then corrupt the primary.
            Assert.That(store.Save(slot, sim.State, data).Ok, Is.True, "third real save");
            var backupBytes = File.ReadAllBytes(backup);      // the copy the loader will fall back to
            File.WriteAllBytes(path, Encoding.UTF8.GetBytes("{\"version\":2,\"garbage\":tru"));

            // 2. First load after the damage: recovered from the .bak and the primary written back.
            var first = store.Load(slot, data);
            Assert.That(first.Ok, Is.True, "first load after corruption: " + first.Reason);
            Assert.That(first.Recovered, Is.Not.Null, "the recovery is reported, not hidden");
            Assert.That(StateHash.Compute(first.State), Is.EqualTo(hash), "the recovered state is the saved one");
            Assert.That(File.Exists(path), Is.True, "the primary was restored on disk");
            Assert.That(File.ReadAllBytes(path), Is.EqualTo(backupBytes), "restored byte-for-byte from the copy");
            Assert.That(File.Exists(path + AtomicWrite.CorruptSuffix), Is.True, "the damaged file was set aside");

            // 3. Second load through a NEW store over the same folder (a restart): an ordinary load.
            var restarted = new SaveStore(new SystemFileSystem(), _root);
            var second = restarted.Load(slot, data);
            Assert.That(second.Ok, Is.True, "second load: " + second.Reason);
            Assert.That(second.Recovered, Is.Null, "nothing to recover the second time");
            Assert.That(StateHash.Compute(second.State), Is.EqualTo(hash));

            // 4. Primary missing entirely, backup present: still discoverable and recoverable, again repeatedly.
            File.Delete(path);
            Assert.That(restarted.Exists(slot), Is.True, "a backup-only slot is discoverable");
            var listed = restarted.List();
            var found = false;
            for (var i = 0; i < listed.Count; i++) if (listed[i].Name == slot && listed[i].Ok) found = true;
            Assert.That(found, Is.True, "a backup-only slot is listed with a readable header");
            var third = restarted.Load(slot, data);
            Assert.That(third.Ok, Is.True, "load with the primary missing: " + third.Reason);
            Assert.That(third.Recovered, Is.Not.Null);
            Assert.That(File.Exists(path), Is.True, "the primary was restored again");
            var fourth = new SaveStore(new SystemFileSystem(), _root).Load(slot, data);
            Assert.That(fourth.Ok, Is.True, fourth.Reason);
            Assert.That(fourth.Recovered, Is.Null, "and the slot is ordinary again after the restart");
            Assert.That(StateHash.Compute(fourth.State), Is.EqualTo(hash));
        }
    }
}
