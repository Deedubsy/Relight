using NUnit.Framework;
using Relight.Sim.UI;

namespace Relight.Sim.Tests
{
    /// <summary>C-10. The five confirmations (§2.6.4), the save-failure modal and the Saving section (§2.7).</summary>
    public sealed class FrontEndTextTests
    {
        [Test]
        public void EveryDestructiveStepOffersASaveFirstAndACancelLast()
        {
            foreach (var c in new[]
            {
                FrontEndText.StartNewCity("20:14"),
                FrontEndText.LoadThisSave("20:14"),
                FrontEndText.QuitGame("20:14"),
            })
            {
                Assert.AreEqual(3, c.Buttons.Length);
                StringAssert.StartsWith("Save and ", c.Buttons[0]);
                StringAssert.Contains("without saving", c.Buttons[1]);
                Assert.AreEqual("Cancel", c.Buttons[2]);
            }
        }

        [Test]
        public void TheNewCityConfirmationSaysWhatIsDiscardedAndSinceWhen()
        {
            var c = FrontEndText.StartNewCity("11 Sep 2026, 20:14");
            Assert.AreEqual("Start a new city?", c.Title);
            Assert.AreEqual("This session has unsaved progress since 11 Sep 2026, 20:14. Starting a new city discards it.", c.Body);
        }

        [Test]
        public void ANeverSavedSessionIsNotDescribedAsHavingASaveTime()
        {
            var c = FrontEndText.StartNewCity("");
            Assert.AreEqual("This session has never been saved. Starting a new city discards it.", c.Body);
            StringAssert.DoesNotContain("since .", c.Body);
        }

        [Test]
        public void OverwriteAndDeleteBothNameTheSlotThePlaytimeAndTheTime()
        {
            var o = FrontEndText.Overwrite("Slot 1", "1:12:30 played", "11 Sep 2026, 20:14");
            Assert.AreEqual("Overwrite this save?", o.Title);
            Assert.AreEqual("Slot 1 (1:12:30 played, saved 11 Sep 2026, 20:14) will be replaced.", o.Body);
            CollectionAssert.AreEqual(new[] { "Overwrite", "Cancel" }, o.Buttons);

            var d = FrontEndText.Delete("Slot 1", "1:12:30 played", "11 Sep 2026, 20:14");
            Assert.AreEqual("Delete this save?", d.Title);
            Assert.AreEqual("Slot 1 · 1:12:30 played · saved 11 Sep 2026, 20:14. This cannot be undone.", d.Body);
            CollectionAssert.AreEqual(new[] { "Delete", "Cancel" }, d.Buttons);
        }

        [Test]
        public void AFailedManualSaveIsAModalThatEndsByPromisingThePreviousSaveIsIntact()
        {
            var c = FrontEndText.CouldNotSave("the folder could not be written to");
            Assert.AreEqual("Could not save", c.Title);
            Assert.AreEqual("The folder could not be written to. Your previous save is unchanged.", c.Body);
            CollectionAssert.AreEqual(new[] { "Retry", "Choose another slot", "Continue playing" }, c.Buttons);
        }

        [Test]
        public void AReasonThatAlreadyEndsInAFullStopDoesNotGetASecondOne()
        {
            StringAssert.DoesNotContain("..", FrontEndText.CouldNotSave("the disk is full.").Body);
        }

        [Test]
        public void TheAutosaveNoticeNamesThePlaytimeAndNoDay()
        {
            Assert.AreEqual("Autosaved · 1:12:30 played.", FrontEndText.Autosaved("1:12:30"));
        }

        [Test]
        public void TheCurrentBindingLineMarksCtrlCmdActions()
        {
            Assert.AreEqual("Current: w / ArrowUp", FrontEndText.CurrentBinding("north", Bindings.Keys("north", null)));
            Assert.AreEqual("Current: Ctrl / Cmd + c", FrontEndText.CurrentBinding("copy", Bindings.Keys("copy", null)));
            Assert.AreEqual("Current: Unassigned", FrontEndText.CurrentBinding("wall", Bindings.Keys("wall", null)));
            Assert.AreEqual("Current: Space", FrontEndText.CurrentBinding("dodge", Bindings.Keys("dodge", null)));
        }
    }
}
