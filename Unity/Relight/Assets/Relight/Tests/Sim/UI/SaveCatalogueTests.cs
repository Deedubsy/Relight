using System.Collections.Generic;
using NUnit.Framework;
using Relight.Sim.UI;

namespace Relight.Sim.Tests
{
    /// <summary>
    /// C-10. What the Load screen holds, UI_AND_ONBOARDING.md §2.7.1: two groups, newest first, never
    /// interleaved, nothing hidden — and the Latest tag on the newest autosave a player can actually load.
    /// </summary>
    public sealed class SaveCatalogueTests
    {
        private const string Map = "riverfront-arc-v4";

        private static SaveSlotInfo Slot(string name, string savedAt, string map = Map, string problem = "")
            => new SaveSlotInfo(
                name, "/saves/slot-" + name + ".json",
                problem.Length > 0 ? null : new SaveHeader { SavedAt = savedAt, T = 2400, PlaySeconds = 600, MapId = map },
                problem);

        private static AutosaveEntry Auto(string file, string savedAt, long seq, string map = Map, string problem = "")
            => new AutosaveEntry
            {
                File = file, SavedAt = savedAt, Seq = seq, Tick = 48000, PlaySeconds = 900,
                MapId = map, Problem = problem,
            };

        [Test]
        public void ManualRowsAreNewestFirst()
        {
            var rows = SaveCatalogue.Manual(new List<SaveSlotInfo>
            {
                Slot("older", "2026-09-11T10:00:00Z"),
                Slot("newest", "2026-09-13T10:00:00Z"),
                Slot("middle", "2026-09-12T10:00:00Z"),
            });

            Assert.AreEqual(new[] { "newest", "middle", "older" }, new[] { rows[0].Name, rows[1].Name, rows[2].Name });
            Assert.IsFalse(rows[0].IsAutosave);
        }

        [Test]
        public void AnUnreadableSlotIsKeptAndCarriesTheStoresOwnWords()
        {
            var rows = SaveCatalogue.Manual(new List<SaveSlotInfo> { Slot("broken", "", Map, "the file is not readable") });

            Assert.AreEqual(1, rows.Count, "a save that cannot be read is never dropped from the list");
            Assert.AreEqual("the file is not readable", rows[0].Problem);
            Assert.AreEqual("", rows[0].MapId, "no header means no map id, and no invented one");
        }

        [Test]
        public void AutosaveSecondsComeFromItsTick()
        {
            var rows = SaveCatalogue.Autosaves(new List<AutosaveEntry> { Auto("auto-1.json", "2026-09-13T10:00:00Z", 4) });

            Assert.AreEqual(1, rows.Count);
            Assert.IsTrue(rows[0].IsAutosave);
            Assert.AreEqual(SaveRow.FromTicks(48000), rows[0].T, 1e-9,
                "an AutosaveEntry has no t; its sim seconds are ticks x TickSeconds");
            Assert.AreEqual(4, rows[0].Seq);
        }

        [Test]
        public void AllKeepsManualRowsBeforeAutosaveRows()
        {
            var all = SaveCatalogue.All(
                new List<SaveSlotInfo> { Slot("mine", "2026-09-10T10:00:00Z") },
                new List<AutosaveEntry> { Auto("auto-1.json", "2026-09-13T10:00:00Z", 1) });

            Assert.AreEqual(2, all.Count);
            Assert.IsFalse(all[0].IsAutosave, "the groups are concatenated, not interleaved");
            Assert.IsTrue(all[1].IsAutosave);
        }

        [Test]
        public void LatestTagIsTheNewestAutosaveThatCanBeLoaded()
        {
            var rows = SaveCatalogue.Autosaves(new List<AutosaveEntry>
            {
                Auto("auto-3.json", "2026-09-13T10:00:00Z", 3, Map, "the file is not readable"),
                Auto("auto-2.json", "2026-09-12T10:00:00Z", 2),
                Auto("auto-1.json", "2026-09-11T10:00:00Z", 1),
            });

            var latest = SaveCatalogue.LatestAutosave(rows, Map);

            Assert.IsNotNull(latest);
            Assert.AreEqual("auto-2.json", latest.Name, "the tag steps past a newest file that cannot be loaded");
        }

        [Test]
        public void ASaveFromAnotherMapNeverCarriesTheLatestTag()
        {
            var rows = SaveCatalogue.Autosaves(new List<AutosaveEntry>
            {
                Auto("auto-2.json", "2026-09-13T10:00:00Z", 2, "some-other-map"),
                Auto("auto-1.json", "2026-09-12T10:00:00Z", 1),
            });

            var latest = SaveCatalogue.LatestAutosave(rows, Map);

            Assert.AreEqual("auto-1.json", latest.Name);
            Assert.IsNotNull(SaveCatalogue.LatestAutosave(rows, ""),
                "with no current map nothing is refused, so the newest is simply the newest");
        }

        [Test]
        public void NullsAreAnEmptyList()
        {
            Assert.AreEqual(0, SaveCatalogue.Manual(null).Count);
            Assert.AreEqual(0, SaveCatalogue.Autosaves(null).Count);
            Assert.IsNull(SaveCatalogue.LatestAutosave(null, Map));
        }
    }
}
