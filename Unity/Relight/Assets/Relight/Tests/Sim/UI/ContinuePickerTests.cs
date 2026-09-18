using System.Collections.Generic;
using NUnit.Framework;
using Relight.Sim.UI;

namespace Relight.Sim.Tests
{
    /// <summary>C-10. What Continue picks, UI_AND_ONBOARDING.md §2.6.3.</summary>
    public sealed class ContinuePickerTests
    {
        private const string Map = "riverfront-arc-v4";

        private static SaveRow Row(string label, string savedAt, string map = Map, string problem = "")
            => new SaveRow { Name = label, Label = label, SavedAt = savedAt, T = 1200, PlaySeconds = 60, MapId = map, Problem = problem ?? "" };

        [Test]
        public void ContinueTakesTheMostRecentlyWrittenSaveWhateverKindItIs()
        {
            var rows = new List<SaveRow>
            {
                Row("Slot 1", "2026-09-11T18:00:00Z"),
                Row("Autosave 2", "2026-09-11T19:30:00Z"),
                Row("Slot 2", "2026-09-10T08:00:00Z"),
            };
            var choice = ContinuePicker.Pick(rows, Map);
            Assert.AreEqual("Autosave 2", choice.Row.Label);
            Assert.AreEqual("", choice.Notice, "nothing was skipped, so nothing is said");
            Assert.IsTrue(choice.Enabled);
        }

        [Test]
        public void WhenTheNewestWillNotOpenContinueFallsToTheNextAndSaysSo()
        {
            var rows = new List<SaveRow>
            {
                Row("Autosave 2", "2026-09-11T19:30:00Z", problem: "this save is damaged (its contents do not match its checksum)"),
                Row("Slot 1", "2026-09-11T18:00:00Z"),
            };
            var choice = ContinuePicker.Pick(rows, Map);
            Assert.AreEqual("Slot 1", choice.Row.Label);
            Assert.AreEqual("Your most recent save could not be opened; continuing from Slot 1.", choice.Notice);
        }

        [Test]
        public void ASaveFromAnotherMapIsStepppedPastLikeAnyOtherUnopenableOne()
        {
            var rows = new List<SaveRow>
            {
                Row("Slot 9", "2026-09-11T19:30:00Z", map: "other-map"),
                Row("Slot 1", "2026-09-11T18:00:00Z"),
            };
            Assert.AreEqual("Slot 1", ContinuePicker.Pick(rows, Map).Row.Label);
        }

        [Test]
        public void WithNoSaveAtAllContinueIsDisabledWithTheReasonAndIsNotHidden()
        {
            var choice = ContinuePicker.Pick(new List<SaveRow>(), Map);
            Assert.IsNull(choice.Row);
            Assert.IsFalse(choice.Enabled);
            Assert.AreEqual("No saved game yet", choice.DisabledReason);
        }

        [Test]
        public void WithNoValidSaveContinueIsDisabledWithTheNewestFailuresReason()
        {
            var rows = new List<SaveRow>
            {
                Row("Slot 1", "2026-09-11T18:00:00Z", problem: "this is not a Relight save file"),
                Row("Slot 2", "2026-09-11T19:00:00Z", problem: "the file is empty"),
            };
            var choice = ContinuePicker.Pick(rows, Map);
            Assert.IsFalse(choice.Enabled);
            Assert.AreEqual("the file is empty", choice.DisabledReason, "the newest failure is the one explained");
        }

        [Test]
        public void ARetryStepsPastASaveThisSessionHasAlreadyFailedToLoad()
        {
            var rows = new List<SaveRow>
            {
                Row("Slot 2", "2026-09-11T19:00:00Z"),
                Row("Slot 1", "2026-09-11T18:00:00Z"),
            };
            var choice = ContinuePicker.Pick(rows, Map, new List<string> { "Slot 2" });
            Assert.AreEqual("Slot 1", choice.Row.Label);
            StringAssert.StartsWith("Your most recent save could not be opened", choice.Notice);
        }
    }
}
