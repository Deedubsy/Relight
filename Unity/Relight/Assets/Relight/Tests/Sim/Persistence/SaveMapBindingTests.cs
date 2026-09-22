using NUnit.Framework;
using Relight.Sim.Tests.Support;

namespace Relight.Sim.Tests.Persistence
{
    /// <summary>
    /// A save is bound to the map it was played on (C-10, U-D-41): the header names the imported region, a save
    /// from another map is refused with both names and left where it is, and a save that does not say (an older
    /// file, or the synthetic map) still loads — there is nothing for it to disagree with.
    /// </summary>
    public sealed class SaveMapBindingTests
    {
        private const string At = "2026-09-14T00:00:00.0000000Z";

        [Test]
        public void TheHeaderCarriesTheMapAndItRoundTrips()
        {
            var (sim, _) = Scenarios.ShortRun().Play(60);
            var text = SaveSerializer.WriteText(sim.State, sim.Context.Data, At, "home-v4", out var written);
            Assert.That(written.MapId, Is.EqualTo("home-v4"));
            StringAssert.Contains("\"mapId\":\"home-v4\"", text);

            var load = SaveSerializer.ReadText(text, sim.Context.Data, "home-v4");
            Assert.That(load.Ok, Is.True, load.Reason);
            Assert.That(load.Header.MapId, Is.EqualTo("home-v4"));
            Assert.That(SaveSerializer.ReadHeader(SaveSerializer.Write(sim.State, sim.Context.Data, At, "home-v4"), out _).MapId,
                Is.EqualTo("home-v4"));
        }

        [Test]
        public void ASaveFromAnotherMapIsRefusedWithBothNamesAndIsNotDamaged()
        {
            var (sim, _) = Scenarios.ShortRun().Play(60);
            var text = SaveSerializer.WriteText(sim.State, sim.Context.Data, At, "home-v4");
            var load = SaveSerializer.ReadText(text, sim.Context.Data, "riverfront-v9");
            Assert.That(load.Ok, Is.False);
            Assert.That(load.Damaged, Is.False, "the file is fine; the player pointed at another map's save");
            StringAssert.Contains("home-v4", load.Reason);
            StringAssert.Contains("riverfront-v9", load.Reason);
        }

        [Test]
        public void ASaveWithoutAMapLoadsAnywhereAndNoExpectationChecksNothing()
        {
            var (sim, _) = Scenarios.ShortRun().Play(60);
            var unbound = SaveSerializer.WriteText(sim.State, sim.Context.Data, At);
            Assert.That(SaveSerializer.ReadText(unbound, sim.Context.Data, "home-v4").Ok, Is.True, "an unbound save loads on any map");
            Assert.That(SaveSerializer.ReadText(unbound, sim.Context.Data).Header.MapId, Is.EqualTo(""));

            var bound = SaveSerializer.WriteText(sim.State, sim.Context.Data, At, "home-v4");
            Assert.That(SaveSerializer.ReadText(bound, sim.Context.Data).Ok, Is.True, "no expectation: nothing is refused");
            Assert.That(SaveSerializer.ReadText(bound, sim.Context.Data, "").Ok, Is.True);
        }

        [Test]
        public void MapProblemIsSharedWithTheLoadScreen()
        {
            Assert.That(SaveSerializer.MapProblem("a", "a"), Is.Null);
            Assert.That(SaveSerializer.MapProblem("", "a"), Is.Null);
            Assert.That(SaveSerializer.MapProblem("a", null), Is.Null);
            Assert.That(SaveSerializer.MapProblem("a", "b"), Is.Not.Null);
        }

        /// <summary>
        /// REL-63, U-D-69 (b): while the game is unreleased a save is bound to the exact map it was made on. The real
        /// city's id is the imported baseline plus <c>-scene-</c> and a fingerprint of the scene (<c>SceneWorld</c>),
        /// so any scene edit gives a new id, and two ids that share the baseline are still different maps. The old
        /// save is refused as another map's — not damaged, left where it is — and the player starts a new game.
        /// Loosening this (a hand-bumped map version, or relocating on load) is a new owner decision, not a fix to
        /// this test; TECHNICAL_ARCHITECTURE.md §9.1 says so too.
        /// </summary>
        [Test]
        public void DuringDevelopmentASceneEditOrphansEverySave()
        {
            const string before = "riverfront-arc-v4-editor-ac16d9188c05-scene-0123456789abcdef";
            const string after = "riverfront-arc-v4-editor-ac16d9188c05-scene-fedcba9876543210";
            Assert.That(SaveSerializer.MapProblem(before, after), Is.Not.Null, "one baseline, an edited scene: another map");
            Assert.That(SaveSerializer.MapProblem(before, before), Is.Null);

            var (fs, store) = PersistenceFixture.Store();
            var (sim, _) = Scenarios.ShortRun().Play(60);
            Assert.That(store.Save("long-run", sim.State, sim.Context.Data, At, before).Ok, Is.True);
            var stored = (byte[])fs.ReadAllBytes(store.PathOf("long-run")).Clone();

            var load = store.Load("long-run", sim.Context.Data, after);
            Assert.That(load.Ok, Is.False, "no save is carried across a scene edit");
            Assert.That(load.Damaged, Is.False, "the file is fine; it belongs to the map before the edit");
            StringAssert.Contains("different map", load.Reason);
            StringAssert.Contains(before, load.Reason);
            StringAssert.Contains(after, load.Reason);
            Assert.That(fs.FileExists(store.PathOf("long-run")), Is.True, "the old save is kept");
            CollectionAssert.AreEqual(stored, fs.ReadAllBytes(store.PathOf("long-run")), "and not rewritten");
            Assert.That(store.Load("long-run", sim.Context.Data, before).Ok, Is.True, "on its own map it still loads");
        }

        [Test]
        public void TheStoresPassTheExpectationThroughAndTheIndexRemembersTheMap()
        {
            var (fs, store) = PersistenceFixture.Store();
            var (sim, _) = Scenarios.ShortRun().Play(60);
            Assert.That(store.Save("bound", sim.State, sim.Context.Data, At, "home-v4").Ok, Is.True);

            Assert.That(store.Load("bound", sim.Context.Data, "home-v4").Ok, Is.True);
            Assert.That(store.Load("bound", sim.Context.Data).Ok, Is.True);
            var other = store.Load("bound", sim.Context.Data, "riverfront-v9");
            Assert.That(other.Ok, Is.False);
            Assert.That(other.Damaged, Is.False);
            Assert.That(fs.FileExists(store.PathOf("bound")), Is.True, "a refused save is left where it is");
            Assert.That(store.List()[0].Header.MapId, Is.EqualTo("home-v4"));
        }
    }
}
