using System.Linq;
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
                Assert.That(places.Count, Is.EqualTo(EncounterCatalogue.Garrison(def)), def.Id);
            }
        }

        /// <summary>FRT-04 (U-P-39): the port's four small camps stand at least 25 tiles from every key camp, so
        /// clearing one never wakes the other.</summary>
        [Test]
        public void TheSmallCampsStandWellClearOfTheKeyCamps()
        {
            var ctx = RealCityFixture.Context(out _opening);
            var keys = EncounterCatalogue.All.Where(d => d.Key != null).Select(d => ctx.Sites.Find(d.Site)).ToList();
            var small = EncounterCatalogue.All.Where(d => d.Kind == EncounterKind.LootCamp).ToList();
            Assert.That(keys.Count, Is.EqualTo(3));
            Assert.That(small.Count, Is.EqualTo(4));
            foreach (var d in small)
            {
                var s = ctx.Sites.Find(d.Site);
                Assert.That(Ground.Walkable(ctx, s.X, s.Y), Is.True, d.Id + ": its marker is not on open ground");
                foreach (var k in keys)
                    Assert.That(DirectorRules.Distance(s.Centre.X, s.Centre.Y, k.Centre.X, k.Centre.Y),
                        Is.GreaterThanOrEqualTo(25), d.Id + " is too near " + k.Id);
            }
        }

        /// <summary>
        /// FRT-05 (REL-140): on the real city the two doors are the only way through the warehouse walls. With them
        /// shut, a body on the floor can reach nothing outside the ring by the rule enemy movement uses; with them
        /// open, it can.
        /// </summary>
        [Test]
        public void TheWarehouseDoorsAreTheOnlyWayIn()
        {
            var ctx = RealCityFixture.Context(out _opening);
            var st = Simulation.NewGame(ctx, 1).State;
            var floor = ctx.Sites.Find("freight:arena");
            Assert.That(floor, Is.Not.Null);
            Assert.That(ctx.Sites.OfKind(SiteKind.StrongholdDoor).Count(), Is.EqualTo(2));
            var guardian = ctx.Sites.Find("freight:arena:guardian");
            bool Out(int x, int y) => x < floor.X - 1 || x > floor.X + floor.W || y < floor.Y - 1 || y > floor.Y + floor.H;

            Assert.That(Flood(ctx, guardian.X, guardian.Y, (x, y) => DirectorRules.HostileOpen(ctx, st, x, y)).Any(t => Out(t.X, t.Y)),
                Is.False, "with the doors shut nothing on the floor can walk out");
            st.Encounters.Opened.Add("freight");
            st.Rev++;
            Assert.That(Flood(ctx, guardian.X, guardian.Y, (x, y) => DirectorRules.HostileOpen(ctx, st, x, y)).Any(t => Out(t.X, t.Y)),
                Is.True, "open, the doors lead out");
            Assert.That(Flood(ctx, guardian.X, guardian.Y, (x, y) => Ground.PassableForEngineer(ctx, st, x, y)).Any(t => Out(t.X, t.Y)),
                Is.True, "and the engineer can walk in");
        }

        /// <summary>Tiles reachable from (x, y) through <paramref name="open"/>, stopping 40 tiles out.</summary>
        static System.Collections.Generic.HashSet<(int X, int Y)> Flood(SimContext ctx, int x, int y, System.Func<int, int, bool> open)
        {
            var seen = new System.Collections.Generic.HashSet<(int X, int Y)> { (x, y) };
            var q = new System.Collections.Generic.Queue<(int X, int Y)>();
            q.Enqueue((x, y));
            while (q.Count > 0)
            {
                var c = q.Dequeue();
                for (var k = 0; k < 4; k++)
                {
                    var nx = c.X + Dirs.DX[k];
                    var ny = c.Y + Dirs.DY[k];
                    if (System.Math.Abs(nx - x) > 40 || System.Math.Abs(ny - y) > 40) continue;
                    if (!Ground.InBounds(ctx, nx, ny) || seen.Contains((nx, ny)) || !open(nx, ny)) continue;
                    seen.Add((nx, ny));
                    q.Enqueue((nx, ny));
                }
            }
            return seen;
        }
    }
}
