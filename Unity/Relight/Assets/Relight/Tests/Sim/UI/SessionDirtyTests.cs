using NUnit.Framework;
using Relight.Sim.UI;

namespace Relight.Sim.Tests
{
    /// <summary>
    /// C-10. The one fact every §2.6.4 confirmation rests on: has this session moved since it was last saved?
    /// It is measured in ticks, not wall clock, so time spent paused or reading a menu is not "progress".
    /// </summary>
    public sealed class SessionDirtyTests
    {
        [Test]
        public void APausedAdminEditIsDirtyUntilSavedOrReset()
        {
            var dirty=new SessionDirty();dirty.Saved(50,"2026-09-15");
            Assert.That(dirty.IsDirty(50),Is.False);
            dirty.MarkChanged();Assert.That(dirty.IsDirty(50),Is.True);
            dirty.Saved(50,"2026-09-15");Assert.That(dirty.IsDirty(50),Is.False);
            dirty.MarkChanged();dirty.Reset();Assert.That(dirty.IsDirty(0),Is.False);
        }

        [Test]
        public void AFreshSessionThatHasNotTickedIsNotDirty()
        {
            var d = new SessionDirty();

            Assert.IsFalse(d.EverSaved);
            Assert.IsFalse(d.IsDirty(0), "nothing has happened yet, so there is nothing to lose");
        }

        [Test]
        public void AFreshSessionIsDirtyAsSoonAsItTicks()
        {
            var d = new SessionDirty();

            Assert.IsTrue(d.IsDirty(1));
        }

        [Test]
        public void SavingClearsItUntilTheNextTick()
        {
            var d = new SessionDirty();
            d.Saved(500, "2026-09-13T10:00:00Z");

            Assert.IsTrue(d.EverSaved);
            Assert.IsFalse(d.IsDirty(500));
            Assert.IsTrue(d.IsDirty(501));
        }

        [Test]
        public void TheSavedTimeIsShownInTheSameWordsAsASaveRow()
        {
            var d = new SessionDirty();
            d.Saved(10, "2026-09-13T20:14:00Z");

            Assert.AreEqual(SaveRowFormatter.SavedAtText("2026-09-13T20:14:00Z"), d.SavedAtText);
        }

        [Test]
        public void ResetMakesItANewSessionAgain()
        {
            var d = new SessionDirty();
            d.Saved(500, "2026-09-13T10:00:00Z");
            d.Reset();

            Assert.IsFalse(d.EverSaved);
            Assert.AreEqual("", d.SavedAtText, "a session that was never saved has no last-save time to name");
            Assert.IsTrue(d.IsDirty(1));
        }

        [Test]
        public void PausingDoesNotMakeASavedSessionDirty()
        {
            var d = new SessionDirty();
            d.Saved(1000, "2026-09-13T10:00:00Z");

            // Ticks are the only input; a minute spent in the pause menu advances none of them.
            Assert.IsFalse(d.IsDirty(1000));
        }
    }
}
