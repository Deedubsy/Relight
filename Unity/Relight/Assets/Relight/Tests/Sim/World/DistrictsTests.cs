using System.Collections.Generic;
using NUnit.Framework;

namespace Relight.Sim.Tests
{
    /// <summary>
    /// INT-09a (REL-81): the one district query. The rule is pinned here on small maps; the nine districts of the
    /// real city are pinned in the Editor assembly (<c>Tests/Editor/RealCityDistrictTests.cs</c>), which can load it.
    /// </summary>
    public sealed class DistrictsTests
    {
        private static SiteRecord Sub(int i, string name, int x, int y) =>
            new SiteRecord("substation:" + i, name, SiteKind.Substation, x, y, 2, 2, "", 0);

        private static SiteRecord Label(int i, string name, int x, int y) =>
            new SiteRecord("label:" + i, name, SiteKind.Label, x, y, 1, 1, "", 0);

        private static SimContext On(params SiteRecord[] sites) =>
            new SimContext(ReferenceData.Create(), SyntheticMap.Create(), sites: new WorldSites(sites));

        [Test]
        public void AMapWithNoSubstationsHasNoDistricts_AndEveryQuestionSaysSo()
        {
            var ctx = WorldTestSupport.Context();
            var d = Districts.Of(ctx);
            Assert.That(d.Count, Is.EqualTo(0));
            Assert.That(d.IndexAt(5, 5), Is.EqualTo(-1));
            Assert.That(Districts.At(ctx, 5, 5), Is.Null);
            Assert.That(Districts.SubstationAt(ctx, 5, 5), Is.Null);
            Assert.That(Districts.NameAt(ctx, 5, 5), Is.Null);
            Assert.That(d.StreetTotal, Is.EqualTo(0));
            Assert.That(d.Owner, Has.All.EqualTo(Districts.None));
        }

        [Test]
        public void APositionBelongsToItsNearestSubstation_AndATieGoesToTheEarlierSite()
        {
            var ctx = On(Sub(0, "West", 10, 20), Sub(1, "East", 30, 20));   // centres 11,21 and 31,21
            var d = Districts.Of(ctx);
            Assert.That(d.Count, Is.EqualTo(2));
            Assert.That(d.At(12, 40).Index, Is.EqualTo(0));
            Assert.That(d.At(30, 2).Index, Is.EqualTo(1));
            Assert.That(d.At(21, 21).Index, Is.EqualTo(0), "equidistant: the earlier site in export order keeps it");
            Assert.That(d.At(21.001, 21).Index, Is.EqualTo(1));
            Assert.That(d.OfTile(20, 21).Index, Is.EqualTo(0), "a tile is measured from its centre, 20.5");
            Assert.That(d.OfTile(21, 21).Index, Is.EqualTo(1), "21.5 is nearer the east site");
            Assert.That(Districts.SubstationAt(ctx, 30, 2).Id, Is.EqualTo("substation:1"));
            Assert.That(Districts.Of(ctx), Is.SameAs(d), "worked out once for a context");
        }

        [Test]
        public void ADistrictIsCalledByTheLabelNearestItsSubstation_ThenTheSubstationsName_ThenItsId()
        {
            var labelled = On(Sub(0, "Substation 0", 10, 10), Sub(1, "Substation 1", 40, 40),
                Label(0, "Founders Court", 14, 14), Label(1, "", 41, 41), Label(2, "Ironworks", 44, 44));
            Assert.That(Districts.NameAt(labelled, 5, 5), Is.EqualTo("Founders Court"));
            Assert.That(Districts.NameAt(labelled, 50, 50), Is.EqualTo("Ironworks"), "a label with no name is not a name");
            var at = Districts.At(labelled, 50, 50).Label;
            Assert.That((at.X, at.Y), Is.EqualTo((44.5, 44.5)), "the stats sit on the label that named it");

            var bare = On(Sub(0, "Riverside", 10, 10), Sub(1, "", 40, 40));
            Assert.That(Districts.NameAt(bare, 5, 5), Is.EqualTo("Riverside"));
            Assert.That(Districts.NameAt(bare, 50, 50), Is.EqualTo("substation:1"));
        }

        [Test]
        public void EveryTileHasOneDistrict_AndTheCountsAddUpToTheMaps()
        {
            var ctx = On(Sub(0, "A", 8, 8), Sub(1, "B", 50, 12), Sub(2, "C", 30, 40));
            var d = Districts.Of(ctx);
            var g = ctx.Geometry;
            int walkable = 0, street = 0;
            for (var y = 0; y < g.Height; y++)
            for (var x = 0; x < g.Width; x++)
            {
                Assert.That(d.Owner[y * g.Width + x], Is.EqualTo((byte)d.OfTile(x, y).Index));
                if (!Ground.Walkable(ctx, x, y)) continue;
                walkable++;
                if (g.TileAt(x, y) == TileClass.Street) street++;
            }
            Assert.That(street, Is.GreaterThan(0), "the synthetic map has streets");

            int tiles = 0, walk = 0, streets = 0;
            foreach (var one in d.All)
            {
                tiles += one.Tiles;
                walk += one.Walkable;
                streets += one.Street;
                Assert.That(one.StreetTiles.Count, Is.EqualTo(one.Street));
                foreach (var t in one.StreetTiles) Assert.That(d.Owner[t], Is.EqualTo((byte)one.Index));
            }
            Assert.That(tiles, Is.EqualTo(g.Width * g.Height));
            Assert.That(walk, Is.EqualTo(walkable));
            Assert.That(streets, Is.EqualTo(street));
            Assert.That(d.StreetTotal, Is.EqualTo(street));
        }

        [Test]
        public void PowerTheOpeningAndTheHudAllAnswerFromTheOneQuery()
        {
            var ctx = On(Sub(0, "Substation 0", 10, 10), Sub(1, "Substation 1", 40, 40),
                Label(0, "Founders Court", 14, 14), Label(1, "Ironworks", 44, 44));
            var hud = new Relight.Sim.UI.DefenceAlertSource();
            var lights = new List<SiteRecord>();
            for (var y = 1; y < 48; y += 7)
            for (var x = 2; x < 60; x += 5)
                lights.Add(new SiteRecord("light:" + x + ":" + y, "", SiteKind.Light, x, y, 1, 1, "", 0));
            foreach (var light in lights)
            {
                var c = light.Centre;
                var district = Districts.At(ctx, c.X, c.Y);
                Assert.That(PowerGrid.SubstationOf(ctx, light), Is.SameAs(district.Substation));
                Assert.That(hud.PlaceAt(ctx, c.X, c.Y), Is.EqualTo(district.Name));
            }
        }
    }
}
