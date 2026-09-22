using NUnit.Framework;
using Relight.Sim.UI;

namespace Relight.Sim.Tests
{
    /// <summary>
    /// C-06. The one sentence the Backpack/storage panel composes itself, UI_AND_ONBOARDING.md §5.4:
    /// <c>"N Item moved to X. M stay in Y (X full)."</c> The panel reports what the command actually moved.
    /// </summary>
    public sealed class TransferTextTests
    {
        [Test]
        public void AFullTransferSaysOnlyWhatMoved()
        {
            Assert.AreEqual("20 Steel plate moved to Home storage.",
                TransferText.Moved(20, 20, "Steel plate", "Backpack", "Home storage"));
        }

        [Test]
        public void APartialTransferNamesTheRemainderWhereItStayedAndWhy()
        {
            Assert.AreEqual("12 Steel plate moved to Backpack. 8 stay in Home storage (Backpack full).",
                TransferText.Moved(12, 20, "Steel plate", "Home storage", "Backpack"));
        }

        [Test]
        public void AMergeThatMovedNothingStillReportsTheWholeRemainder()
        {
            Assert.AreEqual("0 Rounds moved to Backpack. 30 stay in Turret (Backpack full).",
                TransferText.Moved(0, 30, "Rounds", "Turret", "Backpack"));
        }

        [Test]
        public void FractionalQuantitiesKeepTheSimsOwnNumberFormatting()
        {
            Assert.AreEqual("2.5 Ore moved to Backpack.", TransferText.Moved(2.5, 2.5, "Ore", "Excavator", "Backpack"));
        }

        [Test]
        public void TheDrawerHeadingNamesTheStoreOnlyWhenOneIsOpen()
        {
            Assert.AreEqual("Backpack", TransferText.DrawerHeading(""));
            Assert.AreEqual("Home storage & Backpack", TransferText.DrawerHeading("Home storage"));
        }

        [Test]
        public void TheEquipmentStripLabelsBothStates()
        {
            Assert.AreEqual("Slot 1: Empty", TransferText.EquipSlot(0, ""));
            Assert.AreEqual("Slot 2: Rifle · Unequip", TransferText.EquipSlot(1, "Rifle"));
        }

        [Test]
        public void TheStackDetailNamesWhichSideTheStackIsOn()
        {
            Assert.AreEqual("12 / 50 in this stack · Backpack", TransferText.StackDetail(12, 50, true));
            Assert.AreEqual("12 / 50 in this stack · Storage", TransferText.StackDetail(12, 50, false));
        }
    }
}
