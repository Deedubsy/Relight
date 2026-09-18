using NUnit.Framework;
using Relight.Sim.UI;

namespace Relight.Sim.Tests
{
    /// <summary>
    /// C-10. What the title screen asks the world scene for. It is a request, not a setting: it is taken exactly
    /// once, and a scene that starts without one is an ordinary new city.
    /// </summary>
    public sealed class NewGameRequestTests
    {
        [SetUp]
        public void Clear() => NewGameRequest.Clear();

        [TearDown]
        public void Tidy() => NewGameRequest.Clear();

        [Test]
        public void NothingPendingMeansTakeRefuses()
        {
            Assert.IsFalse(NewGameRequest.Take(out _, out _, out _));
        }

        [Test]
        public void ANewCityCarriesItsSeedAndNoSave()
        {
            NewGameRequest.NewCity(17);

            Assert.IsTrue(NewGameRequest.Take(out var seed, out var name, out var autosave));
            Assert.AreEqual(17, seed);
            Assert.IsTrue(string.IsNullOrEmpty(name));
            Assert.IsFalse(autosave);
        }

        [Test]
        public void ARequestIsTakenOnlyOnce()
        {
            NewGameRequest.NewCity(4);

            Assert.IsTrue(NewGameRequest.Take(out _, out _, out _));
            Assert.IsFalse(NewGameRequest.Take(out _, out _, out _),
                "a second scene load must be an ordinary new game, not a repeat of the last request");
        }

        [Test]
        public void LoadingAnAutosaveSaysSo()
        {
            NewGameRequest.LoadSave("auto-2.json", true);

            Assert.IsTrue(NewGameRequest.Take(out _, out var name, out var autosave));
            Assert.AreEqual("auto-2.json", name);
            Assert.IsTrue(autosave, "an autosave is loaded through a different store than a manual slot");
        }

        [Test]
        public void TheNoticeIsShownOnceAndThenGone()
        {
            NewGameRequest.Notice = ContinuePicker.FellBackText("Autosave 2");

            Assert.AreEqual(ContinuePicker.FellBackText("Autosave 2"), NewGameRequest.TakeNotice());
            Assert.AreEqual("", NewGameRequest.TakeNotice());
        }

        [Test]
        public void ClearingDropsTheNoticeToo()
        {
            NewGameRequest.LoadSave("slot", false);
            NewGameRequest.Notice = "something";
            NewGameRequest.Clear();

            Assert.IsFalse(NewGameRequest.Take(out _, out _, out _));
            Assert.AreEqual("", NewGameRequest.TakeNotice());
        }

        [Test]
        public void TheDefaultSeedIsTheOneTheNewGameScreenOffers()
        {
            Assert.AreEqual(3, NewGameRequest.DefaultSeed);
        }
    }
}
