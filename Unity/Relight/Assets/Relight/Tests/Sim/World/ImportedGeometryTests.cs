using NUnit.Framework;

namespace Relight.Sim.Tests
{
    /// <summary>
    /// C-01 / D-02a. The imported-region geometry, on hand-made arrays only — no Unity types, no import file.
    /// The point of these is that the sim side of the import is Y-DOWN and region-local: the flip to Unity cell
    /// space lives in <c>Relight.World.WorldSpace.Cell</c> alone (WORLD_AND_ASSETS.md §2.2).
    /// </summary>
    public sealed class ImportedGeometryTests
    {
        private const int W = 4;
        private const int H = 3;

        /// <summary>
        /// A 4×3 region, row-major Y-down:
        ///   row 0: street street street street
        ///   row 1: ground rubble  patch  river
        ///   row 2: ground ground  ground river
        /// Solid: the two middle tiles of row 1 (a "building"). Spawn: (0, 0).
        /// </summary>
        private static RegionGeometry Small()
        {
            var kind = new byte[]
            {
                (byte)TileClass.Street, (byte)TileClass.Street, (byte)TileClass.Street, (byte)TileClass.Street,
                (byte)TileClass.Ground, (byte)TileClass.Rubble, (byte)TileClass.Patch,  (byte)TileClass.River,
                (byte)TileClass.Ground, (byte)TileClass.Ground, (byte)TileClass.Ground, (byte)TileClass.River,
            };
            var solid = new byte[] { 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0 };
            var variant = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };
            var patch = new byte[] { 0, 0, 0, 0, 0, 0, (byte)PatchType.Copper, 0, 0, 0, 0, 0 };
            return RegionGeometry.FromArrays(W, H, 43, 91, kind, solid, variant, patch, new Vec2(0, 0));
        }

        [Test]
        public void TheRegionReportsItsBoundsAndSpawn()
        {
            var g = Small();
            Assert.AreEqual(W, g.Width);
            Assert.AreEqual(H, g.Height);
            Assert.AreEqual(0, g.Spawn.X, 1e-9);
            Assert.AreEqual(0, g.Spawn.Y, 1e-9);
            Assert.IsTrue(g.InBounds(3, 2));
            Assert.IsFalse(g.InBounds(4, 2), "x == Width is outside");
            Assert.IsFalse(g.InBounds(0, -1));
        }

        [Test]
        public void TileAtIsRowMajorAndYDown()
        {
            var g = Small();
            // Row 1 is the second row of the array, not the second from the bottom: nothing is flipped in the sim.
            Assert.AreEqual(TileClass.Street, g.TileAt(0, 0));
            Assert.AreEqual(TileClass.Rubble, g.TileAt(1, 1));
            Assert.AreEqual(TileClass.River, g.TileAt(3, 1));
            Assert.AreEqual(TileClass.Ground, g.TileAt(0, 2));
            Assert.AreEqual(TileClass.Void, g.TileAt(-1, 0), "off-map is Void");
            Assert.AreEqual(TileClass.Void, g.TileAt(0, H), "off-map is Void");
        }

        [Test]
        public void SolidIsASeparateMaskFromTheTerrainClass()
        {
            var g = Small();
            Assert.IsTrue(g.Solid(1, 1));
            Assert.IsTrue(g.Solid(2, 1));
            Assert.IsFalse(g.Solid(0, 1), "a ground tile beside the building is walkable");
            Assert.IsFalse(g.Solid(3, 1), "river is not solid; it is excluded by its tile class");
            Assert.IsFalse(g.Solid(9, 9), "off-map is not solid");
            Assert.AreEqual(TileClass.Patch, g.TileAt(2, 1), "the solid tile keeps its own terrain class");
        }

        [Test]
        public void VariantAndPatchSideCarsAreReadAtTheSameIndex()
        {
            var g = Small();
            Assert.AreEqual(6, g.VariantAt(2, 1));
            Assert.AreEqual(10, g.VariantAt(2, 2));
            Assert.AreEqual(PatchType.Copper, g.PatchAt(2, 1));
            Assert.AreEqual(PatchType.None, g.PatchAt(0, 0));
            Assert.AreEqual(PatchType.None, g.PatchAt(-1, -1), "off-map has no patch");
            Assert.AreEqual(0, g.VariantAt(-1, -1));
        }

        [Test]
        public void AnAuthoredPatchDecidesTheMinedItemAndOtherTilesDefer()
        {
            IMinableGeometry g = Small();
            Assert.IsTrue(g.TryMinedItem(2, 1, out var copper), "the copper patch answers");
            Assert.AreEqual(ItemId.Copper, copper);
            Assert.IsFalse(g.TryMinedItem(1, 1, out _), "rubble with no patch defers to Mining.FallbackItem");
            Assert.IsFalse(g.TryMinedItem(-1, -1, out _), "off-map yields nothing");
        }

        [Test]
        public void RegionLocalAndCityCoordinatesConvertBothWays()
        {
            var g = Small();
            Assert.AreEqual(43, g.OriginX);
            Assert.AreEqual(91, g.OriginY);
            g.ToCity(2, 1, out var cx, out var cy);
            Assert.AreEqual(45, cx);
            Assert.AreEqual(92, cy);
            Assert.IsTrue(g.ToRegion(45, 92, out var x, out var y));
            Assert.AreEqual(2, x);
            Assert.AreEqual(1, y);
            Assert.IsFalse(g.ToRegion(0, 0, out _, out _), "a city tile outside the region is rejected");
        }

        [Test]
        public void LandAndAccessibleCountsMatchTheExportersDefinitions()
        {
            var g = Small();
            Assert.AreEqual(10, g.CountLand(), "12 tiles less the two river tiles");
            // From (0,0): the whole street row, the left column, the bottom row up to x=2. The two solid tiles and
            // the two river tiles are excluded, and nothing reaches past them.
            Assert.AreEqual(8, g.CountAccessible());
        }

        [Test]
        public void SightIsBlockedByASolidTileAndNotByTerrain()
        {
            var g = Small();
            Assert.IsTrue(g.Sight(0.5, 2.5, 2.5, 2.5), "along the open bottom row");
            Assert.IsFalse(g.Sight(0.5, 1.5, 3.5, 1.5), "through the building");
            Assert.IsTrue(g.Sight(0.5, 1.5, 0.5, 2.5), "river does not block sight, only movement");
        }

        [Test]
        public void ArraysOfTheWrongLengthAreRefused()
        {
            var kind = new byte[W * H];
            var solid = new byte[W * H];
            Assert.Throws<System.ArgumentException>(() =>
                RegionGeometry.FromArrays(W, H, 0, 0, new byte[3], solid, null, null, Vec2.Zero));
            Assert.Throws<System.ArgumentException>(() =>
                RegionGeometry.FromArrays(W, H, 0, 0, kind, new byte[3], null, null, Vec2.Zero));
            Assert.Throws<System.ArgumentException>(() =>
                RegionGeometry.FromArrays(W, H, 0, 0, kind, solid, new byte[3], null, Vec2.Zero));
            Assert.Throws<System.ArgumentNullException>(() =>
                RegionGeometry.FromArrays(W, H, 0, 0, null, solid, null, null, Vec2.Zero));
            Assert.DoesNotThrow(() => RegionGeometry.FromArrays(W, H, 0, 0, kind, solid, null, null, Vec2.Zero));
        }

        [Test]
        public void TheGeometryIsUsableAsICityGeometry()
        {
            ICityGeometry g = Small();
            Assert.AreEqual(W, g.Width);
            Assert.AreEqual(H, g.Height);
            Assert.AreEqual(TileClass.Rubble, g.TileAt(1, 1));
            Assert.IsTrue(g.Solid(1, 1));
        }
    }
}
