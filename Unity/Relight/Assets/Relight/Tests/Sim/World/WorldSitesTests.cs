using System;
using System.Linq;
using NUnit.Framework;
using Relight.Sim;

namespace Relight.Sim.Tests
{
    /// <summary>C-01: the engine-free site list handed to the sim through SimContext.Sites.</summary>
    public sealed class WorldSitesTests
    {
        private static WorldSites Sample() => new WorldSites(new[]
        {
            new SiteRecord("home-workshop", "Home workshop", SiteKind.Core, 22, 256, 10, 14),
            new SiteRecord("steel", "Steel salvage", SiteKind.Resource, 16, 261, 5, 5, "steel", 40),
            new SiteRecord("freight:camp:1", "Camp 1", SiteKind.Camp, 29, 175, 9, 9, null, 3),
            new SiteRecord("home-raid-line", "Raid approach", SiteKind.RaidLine, 26, 300, 6, 1),
        });

        [Test]
        public void EmptyHasNoCoreAndNoSites()
        {
            Assert.AreEqual(0, WorldSites.Empty.Count);
            Assert.IsNull(WorldSites.Empty.Core);
            Assert.IsNull(WorldSites.Empty.Find("home-workshop"));
            Assert.IsFalse(WorldSites.Empty.OfKind(SiteKind.Camp).Any());
        }

        [Test]
        public void SimContextDefaultsToEmptySites()
        {
            var ctx = new SimContext(ReferenceData.Create(), SyntheticMap.Create());
            Assert.AreSame(WorldSites.Empty, ctx.Sites);
        }

        [Test]
        public void CoreFindAndKindLookupsUseExportOrder()
        {
            var s = Sample();
            Assert.AreEqual("home-workshop", s.Core.Id);
            Assert.AreEqual(SiteKind.Resource, s.Find("steel").Kind);
            Assert.AreEqual("steel", s.Find("steel").Item);
            Assert.AreEqual(40, s.Find("steel").Amount);
            Assert.AreEqual(new[] { "freight:camp:1" }, s.OfKind(SiteKind.Camp).Select(x => x.Id).ToArray());
            Assert.IsNull(s.Find("missing"));
        }

        [Test]
        public void RectGeometryIsRegionLocalYDown()
        {
            var core = Sample().Core;
            Assert.AreEqual(27.0, core.Centre.X, 1e-9);
            Assert.AreEqual(263.0, core.Centre.Y, 1e-9);
            Assert.IsTrue(core.Contains(22, 256));
            Assert.IsTrue(core.Contains(31, 269));
            Assert.IsFalse(core.Contains(32, 269));
            Assert.IsFalse(core.Contains(22, 255));
        }

        [Test]
        public void APowerCoreIsNeverTakenForTheHomeCore()
        {
            // Batch 4 (REL-136): a stronghold's power core has its own kind, so the Home core lookup (the FIRST Core)
            // cannot land on it even when the power core comes first in the list.
            var s = new WorldSites(new[]
            {
                new SiteRecord("core:freight", "Occupied freight depot", SiteKind.PowerCore, 63, 48, 1, 1),
                new SiteRecord("home-workshop", "Home workshop", SiteKind.Core, 22, 256, 10, 14),
                new SiteRecord("plant:riverside", "Riverside Works", SiteKind.Plant, 287, 417, 1, 1),
            });
            Assert.AreEqual("home-workshop", s.Core.Id);
            Assert.AreEqual(new[] { "core:freight" }, s.OfKind(SiteKind.PowerCore).Select(x => x.Id).ToArray());
            Assert.AreEqual(new[] { "plant:riverside" }, s.OfKind(SiteKind.Plant).Select(x => x.Id).ToArray());
            Assert.IsNull(new WorldSites(new[] { s.Find("core:freight") }).Core, "a map with only a power core has no Home core");
        }

        [Test]
        public void RefusesDuplicateIdsAndEmptyRects()
        {
            Assert.Throws<ArgumentException>(() => new WorldSites(new[]
            {
                new SiteRecord("a", "a", SiteKind.Label, 0, 0, 1, 1),
                new SiteRecord("a", "b", SiteKind.Label, 1, 1, 1, 1),
            }));
            Assert.Throws<ArgumentException>(() => new SiteRecord("z", "z", SiteKind.Label, 0, 0, 0, 1));
            Assert.Throws<ArgumentException>(() => new SiteRecord("", "z", SiteKind.Label, 0, 0, 1, 1));
        }
    }
}
