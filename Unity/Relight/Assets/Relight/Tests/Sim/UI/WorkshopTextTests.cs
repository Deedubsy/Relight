using NUnit.Framework;
using Relight.Sim.UI;

namespace Relight.Sim.Tests
{
    /// <summary>C-06. The Home workshop cards, UI_AND_ONBOARDING.md §6 / <c>inventoryPanel.ts</c>.</summary>
    public sealed class WorkshopTextTests
    {
        [Test]
        public void ADeadCoreSaysSoInItsTitle()
        {
            Assert.AreEqual("Base core · DISABLED", WorkshopText.CoreCardTitle(0));
            Assert.AreEqual("Base core", WorkshopText.CoreCardTitle(120));
        }

        [Test]
        public void TheCoreButtonHasExactlyThreeStates()
        {
            Assert.AreEqual("Repairing · 8 s left", WorkshopText.CoreButton(true, 7.2, false, 30, 40));
            Assert.AreEqual("Recommission core · 30 s", WorkshopText.CoreButton(false, 0, true, 30, 40));
            Assert.AreEqual("Repair +40 HP · 30 s", WorkshopText.CoreButton(false, 0, false, 30, 40));
        }

        [Test]
        public void CoreHpRoundsUpSoAThinSliverNeverReadsAsZero()
        {
            Assert.AreEqual("1 / 200 HP", WorkshopText.CoreHp(0.2, 200));
        }

        [Test]
        public void TheShortfallNoteNamesBothMaterialsAndWhereToGetThem()
        {
            Assert.AreEqual("Move 20 Steel plates and 5 Copper into the Backpack (drag from Home storage) to repair.",
                WorkshopText.CoreShortfall(20, 5));
        }

        [Test]
        public void ARecipeCardNamesItsOutputAndBatchSize()
        {
            Assert.AreEqual("Bullets ×10", WorkshopText.RecipeTitle("Bullets", 10));
            Assert.AreEqual("Rifle ×1", WorkshopText.RecipeTitle("Rifle", 1));
        }

        [Test]
        public void ProgressCountsDownAndOnlyMentionsAQueueWhenThereIsOne()
        {
            Assert.AreEqual("5 s left", WorkshopText.Progress(12, 7.5, 1));
            Assert.AreEqual("5 s left · 2 more queued", WorkshopText.Progress(12, 7.5, 3));
            Assert.AreEqual("0 s left", WorkshopText.Progress(12, 20, 1), "a finished batch never counts below zero");
        }

        [Test]
        public void TheMissingNoteSaysHowMuchMoreIsNeededNotHowMuchIsRequired()
        {
            Assert.AreEqual("Need 8 more Copper", WorkshopText.NeedMore(20, 12, "Copper"));
            Assert.AreEqual("Need 0 more Copper", WorkshopText.NeedMore(20, 25, "Copper"));
        }

        [Test]
        public void TheLockPromptShowsTheSimsOwnSentenceUnchanged()
        {
            Assert.AreEqual("Repairing — Cancel to move. · Escape cancels",
                WorkshopText.HandLockPrompt(Home.LockText));
        }
    }
}
