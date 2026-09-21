using NUnit.Framework;
using Relight.Sim;
using Relight.World;
using Object = UnityEngine.Object;

namespace Relight.Authoring.Tests
{
    /// <summary>
    /// INT-09a (REL-81): the nine districts of the REAL CITY, from the sim's one query
    /// (<see cref="Districts"/>). The rule itself is pinned offline in <c>Tests/Sim/World/DistrictsTests.cs</c>;
    /// this pins what it answers on the map the game is played on.
    /// </summary>
    public sealed class RealCityDistrictTests
    {
        private static readonly string[] Names =
        {
            "Founders Court", "Riverside Works", "Ironworks", "Civic Utility", "Westridge Homes",
            "Old Town", "Northwood Freight", "Ravenholm Quarry", "East Wharf",
        };

        private WorldGeometryAsset _opening;

        [TearDown]
        public void Teardown()
        {
            if (_opening != null) Object.DestroyImmediate(_opening);
            _opening = null;
        }

        [Test]
        public void TheRealCityHasItsNineNamedDistricts_EachOnItsOwnSubstation()
        {
            var ctx = RealCityFixture.Context(out _opening);
            var d = Districts.Of(ctx);
            Assert.That(d.Count, Is.EqualTo(9));
            for (var i = 0; i < 9; i++)
            {
                var one = d.All[i];
                Assert.That(one.Index, Is.EqualTo(i));
                Assert.That(one.Substation.Id, Is.EqualTo("substation:" + i), "export order");
                Assert.That(one.Name, Is.EqualTo(Names[i]));
                var c = one.Substation.Centre;
                Assert.That(d.At(c.X, c.Y), Is.SameAs(one), "a substation stands in its own district");
                Assert.That(d.At(one.Label.X, one.Label.Y), Is.SameAs(one),
                    one.Name + ": the label that names a district lies inside it, so the map and the HUD agree");
                Assert.That(one.Street, Is.GreaterThan(0), one.Name + " has streets");
            }

            // Home is in Founders Court, which is what the opening's substation goal has always assumed.
            var home = ctx.Sites.Core.Centre;
            Assert.That(Districts.NameAt(ctx, home.X, home.Y), Is.EqualTo("Founders Court"));
        }

        [Test]
        public void TheDistrictsStreetTilesAddUpToTheCitysStreetTotal_AndEveryTileIsOwned()
        {
            var ctx = RealCityFixture.Context(out _opening);
            var d = Districts.Of(ctx);
            var g = ctx.Geometry;
            int street = 0, walkable = 0;
            var owner = d.Owner;
            Assert.That(owner.Length, Is.EqualTo(g.Width * g.Height));
            for (var y = 0; y < g.Height; y++)
            for (var x = 0; x < g.Width; x++)
            {
                if (owner[y * g.Width + x] >= 9) Assert.Fail("tile " + x + "," + y + " is in no district");
                if (!Ground.Walkable(ctx, x, y)) continue;
                walkable++;
                if (g.TileAt(x, y) == TileClass.Street) street++;
            }

            int tiles = 0, walk = 0, streets = 0;
            var report = "";
            foreach (var one in d.All)
            {
                tiles += one.Tiles;
                walk += one.Walkable;
                streets += one.Street;
                Assert.That(one.StreetTiles.Count, Is.EqualTo(one.Street));
                report += "\n  " + one.Index + " " + one.Name + ": " + one.Tiles + " tiles, " + one.Walkable + " walkable, " + one.Street + " street";
            }
            Assert.That(street, Is.GreaterThan(0));
            Assert.That(tiles, Is.EqualTo(g.Width * g.Height), report);
            Assert.That(walk, Is.EqualTo(walkable), report);
            Assert.That(streets, Is.EqualTo(street), report);
            Assert.That(d.StreetTotal, Is.EqualTo(street));
            TestContext.Out.WriteLine("street total " + street + report);
        }
    }
}
