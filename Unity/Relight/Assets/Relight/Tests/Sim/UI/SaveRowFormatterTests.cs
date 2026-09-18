using NUnit.Framework;
using Relight.Sim.UI;

namespace Relight.Sim.Tests
{
    /// <summary>
    /// C-10. The Load screen's rows, UI_AND_ONBOARDING.md §2.6.2 and §2.7.1. The example row in the spec is
    /// <c>Slot 1 · Day 4 · 1:12:30 · saved 11 Sep 2026, 20:14</c>, and that is what these assert.
    /// </summary>
    public sealed class SaveRowFormatterTests
    {
        private const double Day = 1200.0;

        private static SaveRow Row(string label = "Slot 1", double t = 3 * Day + 10, double play = 4350,
                                   string map = "riverfront-arc-v4", string savedAt = "2026-09-11T18:14:00Z")
            => new SaveRow { Name = label, Label = label, T = t, PlaySeconds = play, MapId = map, SavedAt = savedAt };

        [Test]
        public void DayCountsFromOne()
        {
            Assert.AreEqual(1, SaveRowFormatter.Day(0, Day));
            Assert.AreEqual(1, SaveRowFormatter.Day(Day - 0.001, Day));
            Assert.AreEqual(2, SaveRowFormatter.Day(Day, Day));
            Assert.AreEqual(4, SaveRowFormatter.Day(3 * Day + 10, Day));
        }

        [Test]
        public void ARowNamesTheSlotTheDayThePlaytimeAndTheSaveTime()
        {
            var line = SaveRowFormatter.Line(Row(), Day);
            StringAssert.StartsWith("Slot 1 · Day 4 · 1:12:30 · saved ", line);
        }

        [Test]
        public void ASaveWithoutAPlayAccumulatorSaysItIsSimTime()
        {
            var row = Row(play: 0, t: 4350);
            Assert.AreEqual("1:12:30 sim time", SaveRowFormatter.Playtime(row));
            Assert.AreEqual("1:12:30", SaveRowFormatter.Playtime(Row(play: 4350)));
        }

        [Test]
        public void TheNewestAutosaveCarriesTheLatestTag()
        {
            var row = Row("Autosave 3");
            StringAssert.EndsWith(" · Latest", SaveRowFormatter.Line(row, Day, latest: true));
            Assert.IsFalse(SaveRowFormatter.Line(row, Day).EndsWith(" · Latest"));
        }

        [Test]
        public void AutosaveLabelsComeFromTheRingFileName()
        {
            Assert.AreEqual("Autosave 3", SaveRowFormatter.AutosaveLabel("auto-3.json"));
            Assert.AreEqual("Autosave on quit", SaveRowFormatter.AutosaveLabel("auto-quit.json"));
        }

        [Test]
        public void AMismatchedMapDisablesTheRowWithTheStoresOwnReason()
        {
            var row = Row(map: "other-map");
            var reason = SaveRowFormatter.DisabledReason(row, "riverfront-arc-v4");
            Assert.AreEqual(SaveSerializer.MapProblem("other-map", "riverfront-arc-v4"), reason);
            Assert.IsFalse(SaveRowFormatter.Loadable(row, "riverfront-arc-v4"));
            StringAssert.Contains("map other-map", SaveRowFormatter.Detail(row, "riverfront-arc-v4"));
        }

        [Test]
        public void AMatchingOrUnknownMapLeavesTheRowLoadableAndSaysNothingExtra()
        {
            Assert.IsTrue(SaveRowFormatter.Loadable(Row(), "riverfront-arc-v4"));
            Assert.AreEqual("", SaveRowFormatter.Detail(Row(), "riverfront-arc-v4"));
            Assert.IsTrue(SaveRowFormatter.Loadable(Row(map: ""), "riverfront-arc-v4"), "a save that does not say is not refused");
        }

        [Test]
        public void AnUnreadableRowIsShownDisabledWithTheReadersReasonAndKeepsItsLabel()
        {
            var row = new SaveRow { Name = "slot-2", Label = "Slot 2", Problem = "this save is damaged (its contents do not match its checksum)" };
            Assert.AreEqual("this save is damaged (its contents do not match its checksum)",
                SaveRowFormatter.DisabledReason(row, "riverfront-arc-v4"));
            Assert.AreEqual("Slot 2", SaveRowFormatter.Line(row, Day), "no Day and no playtime are invented for a header that was never read");
        }

        [Test]
        public void ARecoveredRowSaysTheCopyIsTheOlderOne()
        {
            var row = Row();
            row.FromBackup = true;
            Assert.AreEqual(SaveRowFormatter.FromBackupText, SaveRowFormatter.Detail(row, "riverfront-arc-v4"));
            Assert.IsTrue(SaveRowFormatter.Loadable(row, "riverfront-arc-v4"), "the slot still loads");
        }
    }
}
