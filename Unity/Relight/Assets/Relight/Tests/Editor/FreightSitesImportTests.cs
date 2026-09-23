using NUnit.Framework;
using Relight.Sim;
using Relight.World;
using Object = UnityEngine.Object;

namespace Relight.Authoring.Tests
{
    /// <summary>
    /// Batch 4, FRT-01 (REL-136; GP-W7's accept line): the Freight places the importer used to drop reach the real
    /// city with the reference's ids and tiles (city.json <c>sites.plants</c>, <c>sites.cores</c>,
    /// <c>combat.freightGates</c>, <c>combat.freightArena</c>). A missing site here means the generated asset was not
    /// re-imported after the importer changed.
    /// </summary>
    public sealed class FreightSitesImportTests
    {
        private WorldGeometryAsset _opening;

        [TearDown]
        public void Teardown()
        {
            if (_opening != null) Object.DestroyImmediate(_opening);
            _opening = null;
        }

        private static void At(WorldSites s, string id, SiteKind kind, int x, int y, int w = 1, int h = 1)
        {
            var site = s.Find(id);
            Assert.That(site, Is.Not.Null, id + " is missing from the real city");
            Assert.That(site.Kind, Is.EqualTo(kind), id);
            Assert.That((site.X, site.Y, site.W, site.H), Is.EqualTo((x, y, w, h)), id);
        }

        [Test]
        public void ThePlantsPowerCoresDoorsAndArenaArriveWithTheirReferenceIdsAndTiles()
        {
            var s = RealCityFixture.Context(out _opening).Sites;

            At(s, "plant:riverside", SiteKind.Plant, 287, 417);
            At(s, "plant:ironworks", SiteKind.Plant, 402, 78);
            At(s, "plant:civic", SiteKind.Plant, 707, 415);
            Assert.That(s.Find("plant:riverside").Name, Is.EqualTo("Riverside Works"));

            At(s, "core:freight", SiteKind.PowerCore, 63, 48);
            At(s, "core:quarry", SiteKind.PowerCore, 806, 49);
            At(s, "core:wharf", SiteKind.PowerCore, 826, 431);

            At(s, "freight:door:0", SiteKind.StrongholdDoor, 65, 61, 3, 1);
            At(s, "freight:door:1", SiteKind.StrongholdDoor, 56, 49, 1, 3);

            At(s, "freight:arena", SiteKind.Arena, 57, 40, 28, 21);
            Assert.That(s.Find("freight:arena").Amount, Is.EqualTo(60), "the reference garrison");
            At(s, "freight:arena:guardian", SiteKind.Arena, 77, 50);
            var groups = new[] { (45, 35), (45, 58), (98, 35), (98, 61), (62, 58), (79, 46) };
            for (var i = 0; i < groups.Length; i++)
                At(s, "freight:arena:group:" + i, SiteKind.Arena, groups[i].Item1, groups[i].Item2);
            Assert.That(s.Find("freight:arena:group:6"), Is.Null);
        }

        [Test]
        public void TheHomeCoreIsStillHomeAndThePowerCoresSitInsideTheirStronghold()
        {
            var s = RealCityFixture.Context(out _opening).Sites;
            Assert.That(s.Core, Is.Not.Null);
            Assert.That(s.Core.Kind, Is.EqualTo(SiteKind.Core));
            Assert.That(s.Core.Id, Is.Not.EqualTo("core:freight"));
            var arena = s.Find("freight:arena");
            var core = s.Find("core:freight");
            Assert.That(arena.Contains(core.X, core.Y), "the freight core lies on the warehouse floor");
            Assert.That(arena.Contains(77, 50), "the guardian starts on the warehouse floor");
        }

        [Test]
        public void TheDoorTilesAreWalkableGroundSoOnlyTheSimKeepsThemShut()
        {
            var ctx = RealCityFixture.Context(out _opening);
            foreach (var id in new[] { "freight:door:0", "freight:door:1" })
            {
                var d = ctx.Sites.Find(id);
                for (var y = d.Y; y < d.Y + d.H; y++)
                    for (var x = d.X; x < d.X + d.W; x++)
                        Assert.That(ctx.Geometry.Solid(x, y), Is.False, id + " tile " + x + "," + y);
            }
        }
    }
}
