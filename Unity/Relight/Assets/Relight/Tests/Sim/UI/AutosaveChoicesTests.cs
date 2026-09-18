using NUnit.Framework;
using Relight.Sim.UI;

namespace Relight.Sim.Tests
{
    /// <summary>C-10. Settings → Saving, UI_AND_ONBOARDING.md §2.7.2 (U-D-35).</summary>
    public sealed class AutosaveChoicesTests
    {
        [Test]
        public void TheOfferedValuesAreTheSpecsAndTheDefaultsAreTheSpecs()
        {
            CollectionAssert.AreEqual(new double[] { 0, 5, 10, 15, 30 }, AutosaveChoices.Intervals);
            CollectionAssert.AreEqual(new[] { 3, 5, 10 }, AutosaveChoices.Kept);
            Assert.AreEqual(10, AutosaveChoices.DefaultIntervalMinutes);
            Assert.AreEqual(5, AutosaveChoices.DefaultKept);
        }

        [Test]
        public void EveryOfferedValueSurvivesTheEnginesOwnClamping()
        {
            foreach (var minutes in AutosaveChoices.Intervals)
                foreach (var kept in AutosaveChoices.Kept)
                {
                    var clamped = new AutosaveSettings(minutes, kept).Clamped();
                    Assert.AreEqual(minutes, clamped.IntervalMinutes, "interval " + minutes + " was clamped");
                    Assert.AreEqual(kept, clamped.Slots, "slot count " + kept + " was clamped");
                }
        }

        [Test]
        public void OffIsAChoiceAndIsLabelledAsOne()
        {
            Assert.AreEqual("Autosave off", AutosaveChoices.IntervalLabel(0));
            Assert.AreEqual("Every 10 minutes", AutosaveChoices.IntervalLabel(10));
            Assert.IsFalse(new AutosaveSettings(0, 5).Clamped().TimedEnabled);
        }

        [Test]
        public void LoweringTheCountAsksFirstAndNamesHowManyWillBeRemoved()
        {
            var c = AutosaveChoices.LowerKept(10, 3, 10);
            Assert.IsNotNull(c);
            Assert.AreEqual("Keep fewer autosaves?", c.Title);
            StringAssert.Contains("deletes the 7 oldest autosaves", c.Body);
            StringAssert.Contains("This cannot be undone.", c.Body);
            Assert.AreEqual("Cancel", c.Buttons[c.Buttons.Length - 1]);
        }

        [Test]
        public void OneRemovalIsWordedInTheSingular()
        {
            StringAssert.Contains("deletes the oldest autosave.", AutosaveChoices.LowerKept(5, 3, 4).Body);
        }

        [Test]
        public void NothingIsAskedWhenNothingWouldBeDeleted()
        {
            Assert.IsNull(AutosaveChoices.LowerKept(3, 5, 3), "raising the count deletes nothing");
            Assert.IsNull(AutosaveChoices.LowerKept(10, 5, 4), "there are fewer autosaves than the new limit");
        }
    }
}
