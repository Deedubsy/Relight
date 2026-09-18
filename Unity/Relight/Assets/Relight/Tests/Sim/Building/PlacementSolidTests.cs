using NUnit.Framework;

namespace Relight.Sim.Tests
{
    /// <summary>
    /// Reference flow.ts:1293: the authored building mask refuses a footprint the way river and inert tiles do.
    /// Found in the Phase C integrated run, where machines could be placed through Home's walls.
    /// </summary>
    [TestFixture]
    public sealed class PlacementSolidTests
    {
        private static SimContext Context() => new SimContext(ReferenceData.Create(), SyntheticMap.Create());

        [Test]
        public void ASolidBuildingTileRefusesPlacement()
        {
            var ctx = Context();
            var st = Fixture.State(ctx);
            // SyntheticMap: the building at x = 40..49, y = 8..15 is solid over ordinary ground tiles.
            Assert.That(ctx.Geometry.Solid(42, 10), Is.True, "the fixture building is in the mask");
            Assert.That(ctx.Geometry.TileAt(42, 10), Is.EqualTo(TileClass.Ground), "and the terrain under it is ground");

            Assert.That(Placement.GeometryProblem(ctx, st, "chest", 42, 10, Dir.N), Is.EqualTo("a city structure is there"));
            Assert.That(Placement.GeometryProblem(ctx, st, "turret", 39, 10, Dir.N), Is.EqualTo("a city structure is there"), "a 2x2 footprint touching the wall");
            Assert.That(Placement.GeometryProblem(ctx, st, "chest", 38, 10, Dir.N), Is.EqualTo(""), "the tile beside the wall is fine");
        }
    }
}
