using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using Relight.Sim.Tests.Persistence;
using Relight.Sim.Tests.Support;
using Relight.Sim.UI;

namespace Relight.Sim.Tests.UI
{
    /// <summary>
    /// REL-66 (PER-06): what the save store had to do to read the file is said to the PLAYER, not only to the log.
    ///
    /// A <see cref="LoadResult"/> that succeeded can still carry three sentences — the file was an older schema and
    /// was upgraded as it was read, it was made against different balance data, or the file asked for was unusable
    /// and its backup was loaded instead. Each one can change what the player does next, and before this they were
    /// written with <c>Debug.Log</c> and, at most, half-shown: <c>FrontEndBootstrap.Collect</c> put ONE of the three
    /// into the startup notice, which surfaces only when the player next opens the pause menu, and
    /// <c>PauseMenuController.DoLoad</c> wrote the recovery sentence into a panel it dismissed in the same call.
    ///
    /// Three of the four tests here drive a REAL save on a REAL disk, in a throwaway folder under the system temp
    /// path (the pattern <see cref="RealDiskRecoveryTests"/> established), so the fixtures are files this test makes
    /// and deletes rather than anything checked in — the accept criterion asks for fixtures outside
    /// <c>Unity/Docs/evidence/</c>. The fourth drives the view model directly, because "two sentences at once" and
    /// "a refusal says nothing" are statements about <see cref="HudViewModel.SayLoad"/> and not about any file.
    ///
    /// Not covered here, and deliberately: that the notice neither pauses the game nor moves the camera.
    /// <see cref="HudViewModel"/> holds no engine reference and can reach neither, so it is true by construction —
    /// but no test in this file proves it.
    /// </summary>
    public sealed class LoadNoticeTests
    {
        private string _root;

        [SetUp]
        public void MakeFolder()
        {
            _root = Path.Combine(Path.GetTempPath(), "relight-loadnotice-" + Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void RemoveFolder()
        {
            try { if (Directory.Exists(_root)) Directory.Delete(_root, true); } catch { /* best effort */ }
        }

        private static HudNotice Row(HudViewModel vm, string key)
        {
            for (var i = 0; i < vm.Notices.Rows.Count; i++) if (vm.Notices.Rows[i].Key == key) return vm.Notices.Rows[i];
            return null;
        }

        private static int RowsWith(HudViewModel vm, string key)
        {
            var n = 0;
            for (var i = 0; i < vm.Notices.Rows.Count; i++) if (vm.Notices.Rows[i].Key == key) n++;
            return n;
        }

        /// <summary>The canonical state text of <paramref name="st"/> with the dotted paths in <paramref name="drop"/> gone.</summary>
        private static string Without(JsonValue v, string path, HashSet<string> drop)
        {
            if (!v.IsObject) return v.ToCanonicalJson();
            var parts = new List<string>();
            foreach (var key in v.Keys)
            {
                var child = path.Length == 0 ? key : path + "." + key;
                if (drop.Contains(child)) continue;
                parts.Add(CanonicalJsonWriter.QuoteString(key) + ":" + Without(v.Member(key), child, drop));
            }
            return "{" + string.Join(",", parts) + "}";
        }

        /// <summary>
        /// A whole save document written as an earlier build would have written it: the state without the members
        /// that version did not have, stamped with that version and checksummed over what it actually says. The same
        /// forging <see cref="Relight.Sim.Tests.Combat.DirectorSaveTests"/> does, done here against a file on disk.
        /// </summary>
        private static string OlderDocument(SimState st, GameData data, int version, params string[] drop)
        {
            var parsed = JsonValue.Parse(PersistenceFixture.Canonical(st), out var error);
            Assert.That(error, Is.Null, "the state this test forges an older copy of parses");
            var state = Without(parsed, "", new HashSet<string>(drop));
            var doc = SaveSerializer.WriteText(st, data);
            doc = PersistenceFixture.Retarget(doc, "state", state);
            doc = PersistenceFixture.Retarget(doc, "version",
                version.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return PersistenceFixture.Retarget(doc, "hash", CanonicalJsonWriter.QuoteString(StateHash.Of(state)));
        }

        /// <summary>
        /// A save written by an earlier build, loaded off a real disk, says so on the HUD — as Info, because being
        /// read through an upgrade is a fact about the file rather than a warning about damage. The text is the
        /// store's own words, unreworded: the save layer owns what is true about the file.
        /// </summary>
        [Test]
        public void ASaveFromAnEarlierBuildSaysSoOnTheHudAfterItLoads()
        {
            var fs = new SystemFileSystem();
            var store = new SaveStore(fs, _root);
            var (sim, _) = Scenarios.ShortRun().Play(100);
            var data = sim.Context.Data;
            const string slot = "older-build";

            // A version-11 file: everything the current build writes, minus the quiet spell v12 added.
            Assert.That(store.Save(slot, sim.State, data).Ok, Is.True, "the fixture is written through the store");
            File.WriteAllText(store.PathOf(slot),
                OlderDocument(sim.State, data, 11, "director.quietUntil"), new UTF8Encoding(false));

            var result = store.Load(slot, data);
            Assert.That(result.Ok, Is.True, "an older save still loads: " + result.Reason);
            Assert.That(result.Upgraded, Is.Not.Null.And.Not.Empty, "and the store says what it had to do");
            Assert.That(result.Upgraded, Does.Contain("save version 11"));

            var vm = new HudViewModel();
            vm.SayLoad(result, 1);
            vm.ReapNotices(1);

            var row = Row(vm, HudViewModel.LoadUpgradedKey);
            Assert.That(row, Is.Not.Null, "the upgrade is on screen, not only in the log");
            Assert.That(row.Text, Is.EqualTo(HudViewModel.LoadSentence(result.Upgraded)), "in the store's own words");
            Assert.That(row.Kind, Is.EqualTo(HudNoticeKind.Info), "an upgrade is information, not damage");
            Assert.That(RowsWith(vm, HudViewModel.LoadWarningKey), Is.Zero, "and nothing else is claimed");
            Assert.That(RowsWith(vm, HudViewModel.LoadRecoveredKey), Is.Zero);
        }

        /// <summary>
        /// A damaged file recovered from its backup says so, as a Warning: the player has lost whatever play
        /// happened between the backup and the file that would not read, and that is the one fact they need to
        /// decide whether to carry on from here.
        /// </summary>
        [Test]
        public void AGameLoadedFromItsBackupSaysSoOnTheHud()
        {
            var fs = new SystemFileSystem();
            var store = new SaveStore(fs, _root);
            var (sim, _) = Scenarios.ShortRun().Play(100);
            var data = sim.Context.Data;
            const string slot = "recovered";
            var path = store.PathOf(slot);

            // Two saves so the protocol keeps a .bak, then the primary is made unreadable on disk.
            Assert.That(store.Save(slot, sim.State, data).Ok, Is.True, "first save");
            Assert.That(store.Save(slot, sim.State, data).Ok, Is.True, "second save, which leaves a .bak");
            Assert.That(File.Exists(path + AtomicWrite.BackupSuffix), Is.True, "the fixture has a backup to recover from");
            File.WriteAllBytes(path, Encoding.UTF8.GetBytes("{\"version\":2,\"garbage\":tru"));

            var result = store.Load(slot, data);
            Assert.That(result.Ok, Is.True, "the backup loads: " + result.Reason);
            Assert.That(result.Recovered, Is.Not.Null.And.Not.Empty, "and the store says the file was recovered");

            var vm = new HudViewModel();
            vm.SayLoad(result, 1);
            vm.ReapNotices(1);

            var row = Row(vm, HudViewModel.LoadRecoveredKey);
            Assert.That(row, Is.Not.Null, "a recovery is on screen, not only in the log");
            Assert.That(row.Text, Is.EqualTo(HudViewModel.LoadSentence(result.Recovered)));
            Assert.That(row.Kind, Is.EqualTo(HudNoticeKind.Warning), "losing the newer file is worth a warning");
        }

        /// <summary>
        /// A save made against different balance data loads and says so. This is the sentence a player most needs
        /// on screen and was least likely to see: it is neither a refusal (which the Load screen shows) nor a
        /// recovery (which the pause menu at least tried to write), so before this it went only to the log.
        /// </summary>
        [Test]
        public void ASaveMadeWithDifferentBalanceDataSaysSoOnTheHud()
        {
            var fs = new SystemFileSystem();
            var store = new SaveStore(fs, _root);
            var (sim, _) = Scenarios.ShortRun().Play(100);
            var data = sim.Context.Data;
            const string slot = "other-balance";

            Assert.That(store.Save(slot, sim.State, data).Ok, Is.True, "the fixture is written through the store");
            var stamped = PersistenceFixture.Retarget(File.ReadAllText(store.PathOf(slot)), "dataVersion", "\"deadbeef\"");
            File.WriteAllText(store.PathOf(slot), stamped, new UTF8Encoding(false));

            var result = store.Load(slot, data);
            Assert.That(result.Ok, Is.True, "changed balance data is a difference, not damage: " + result.Reason);
            Assert.That(result.Warning, Does.Contain("different balance data"));

            var vm = new HudViewModel();
            vm.SayLoad(result, 1);
            vm.ReapNotices(1);

            var row = Row(vm, HudViewModel.LoadWarningKey);
            Assert.That(row, Is.Not.Null, "the difference is on screen, not only in the log");
            Assert.That(row.Text, Is.EqualTo(HudViewModel.LoadSentence(result.Warning)));
            Assert.That(row.Kind, Is.EqualTo(HudNoticeKind.Warning));
        }

        /// <summary>
        /// The three sentences are independent facts, so a load carrying two says BOTH — the <c>else if</c> chain
        /// this replaced would have shown one and silently dropped the other. Saying them is per load, not per
        /// refresh: the result instance is what marks them said, so the HUD can hand the same load over on every
        /// paint without the rows multiplying. And a refusal says nothing at all — it changed nothing, and the Load
        /// screen is already showing the reason on the screen the player is looking at.
        /// </summary>
        [Test]
        public void TwoSentencesAtOnceAreBothSaidOncePerLoadAndARefusalSaysNothing()
        {
            var vm = new HudViewModel();
            var both = LoadResult.Loaded(new SimState(), new SaveHeader(),
                "this save was made with different balance data (aaaa vs bbbb)",
                "this save was made by an earlier build (save version 11) and was read as version 12; the file itself is unchanged");

            vm.SayLoad(both, 1);
            vm.ReapNotices(1);
            Assert.That(RowsWith(vm, HudViewModel.LoadWarningKey), Is.EqualTo(1), "the balance-data sentence is said");
            Assert.That(RowsWith(vm, HudViewModel.LoadUpgradedKey), Is.EqualTo(1), "and so is the upgrade, not instead of it");

            // Every later paint hands over the same load. It is already said.
            for (var i = 0; i < 5; i++) { vm.SayLoad(both, 1 + i * 0.15); vm.ReapNotices(1 + i * 0.15); }
            Assert.That(RowsWith(vm, HudViewModel.LoadWarningKey), Is.EqualTo(1), "and neither row is posted twice");
            Assert.That(RowsWith(vm, HudViewModel.LoadUpgradedKey), Is.EqualTo(1));

            // Read, then gone: a load sentence is a guide line, so it is spent by being SEEN (REL-118), not by a clock.
            vm.ReapNotices(1 + HudViewModel.GuideSeconds + 1);
            Assert.That(RowsWith(vm, HudViewModel.LoadWarningKey), Is.Zero, "it goes once it has been read");
            Assert.That(RowsWith(vm, HudViewModel.LoadUpgradedKey), Is.Zero);

            var refused = LoadResult.Refuse("this save is damaged (its contents do not match its checksum)", true);
            vm.SayLoad(refused, 50);
            vm.ReapNotices(50);
            Assert.That(vm.Notices.Rows, Is.Empty, "a refusal is the Load screen's to show, and changed nothing here");
        }
    }
}
