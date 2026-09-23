using NUnit.Framework;
using Relight.Sim;
using Relight.World;
using Object = UnityEngine.Object;

namespace Relight.Authoring.Tests
{
    /// <summary>
    /// Batch 4, FRT-02 (REL-137): every encounter row stands on a site the real city has, and each garrison fits
    /// whole on the real streets round its groups. The phase's rules are proven on the synthetic map
    /// (EncounterPhaseTests); this is only that the real places give them room.
    /// </summary>
    public sealed class FreightEncounterSitesTests
    {
        private WorldGeometryAsset _opening;

        [TearDown]
        public void Teardown()
        {
            if (_opening != null) Object.DestroyImmediate(_opening);
            _opening = null;
        }

        [Test]
        public void EveryEncounterStandsOnARealSiteAndItsGarrisonFitsWhole()
        {
            var ctx = RealCityFixture.Context(out _opening);
            var sim = Simulation.NewGame(ctx, 1);
            var st = sim.State;
            st.Engineer.Pos = new Vec2(-1000, -1000);
            st.Enemies.Actors.Clear();
            foreach (var def in EncounterCatalogue.All)
            {
                var site = ctx.Sites.Find(def.Site);
                Assert.That(site, Is.Not.Null, def.Id + ": its site " + def.Site + " is not in the real city");
                Assert.That(EncounterPhase.TryPlace(ctx, st, def, site, out var places), Is.True,
                    def.Id + ": the garrison does not fit round its groups");
                Assert.That(places.Count, Is.EqualTo(def.Bodies), def.Id);
            }
        }
    }
}
