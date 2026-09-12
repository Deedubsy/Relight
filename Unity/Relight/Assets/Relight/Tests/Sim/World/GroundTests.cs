using NUnit.Framework;

namespace Relight.Sim.Tests
{
    /// <summary>
    /// Walkability and reach on the synthetic map (reference ground.ts:440 walkable, walk.ts:50 passable,
    /// walk.ts:57 canStand, ground.ts:596 inReach; WORLD_AND_ASSETS.md §2.6).
    /// </summary>
    public sealed class GroundTests
    {
        [Test]
        public void StreetGroundRubbleDepositAndPatchAreWalkable()
        {
            var ctx = WorldTestSupport.Context();
            Assert.IsTrue(Ground.Walkable(ctx, 6, 6), "street");
            Assert.IsTrue(Ground.Walkable(ctx, 20, 44), "ground");
            Assert.IsTrue(Ground.Walkable(ctx, 12, 12), "rubble");
            Assert.IsTrue(Ground.Walkable(ctx, 46, 31), "deposit");
            Assert.IsTrue(Ground.Walkable(ctx, 23, 13), "patch");
        }

        [Test]
        public void RiverAndOffMapAreNotWalkable()
        {
            var ctx = WorldTestSupport.Context();
            Assert.AreEqual(TileClass.River, ctx.Geometry.TileAt(10, 25));
            Assert.IsFalse(Ground.Walkable(ctx, 10, 25));
            Assert.AreEqual(TileClass.Void, ctx.Geometry.TileAt(-1, 5));
            Assert.IsFalse(Ground.Walkable(ctx, -1, 5));
            Assert.IsFalse(Ground.Walkable(ctx, SyntheticMap.Width, 5));
        }

        [Test]
        public void TheRiverCrossingIsTheOneWayThrough()
        {
            var ctx = WorldTestSupport.Context();
            for (var y = 24; y < 28; y++)
                for (var x = 0; x < SyntheticMap.Width; x++)
                {
                    var open = x >= 30 && x < 34;
                    Assert.AreEqual(open, Ground.Walkable(ctx, x, y), $"({x},{y})");
                }
        }

        [Test]
        public void ASolidBuildingBlocksWalkingWithoutChangingTheTerrain()
        {
            var ctx = WorldTestSupport.Context();
            // WORLD_AND_ASSETS.md §2.6: building collision is a separate mask, not a terrain class.
            Assert.AreEqual(TileClass.Ground, ctx.Geometry.TileAt(44, 10));
            Assert.IsFalse(Ground.Walkable(ctx, 44, 10));
            Assert.IsTrue(Ground.Walkable(ctx, 39, 10), "just west of the wall");
        }

        [Test]
        public void InertIsWalkableBecauseTheReferenceOnlyExcludesRiver()
        {
            // ground.ts:440 `walkable` excludes T_RIVER and urban.solid — nothing else. T_INERT is ordinary footing
            // (D5: "any tile that is not water"). Recorded as an intentional divergence from the B-08 brief's
            // parenthetical, not a defect; see the B-08 report.
            var ctx = WorldTestSupport.Context();
            Assert.AreEqual(TileClass.Inert, ctx.Geometry.TileAt(14, 31));
            Assert.IsTrue(Ground.Walkable(ctx, 14, 31));
        }

        [Test]
        public void MachinesBlockExceptTheWalkThroughKinds()
        {
            var (ctx, st) = WorldTestSupport.New();
            WorldTestSupport.AddMachine(st, "chest", 20, 44);
            Assert.IsTrue(Ground.Walkable(ctx, 20, 44), "terrain is still ground");
            Assert.IsFalse(Ground.Passable(ctx, st, 20, 44), "a chest blocks");

            WorldTestSupport.AddMachine(st, "belt", 21, 44);
            WorldTestSupport.AddMachine(st, "pole", 22, 44);
            Assert.IsTrue(Ground.Passable(ctx, st, 21, 44), "belts are thin (walk.ts PASSABLE)");
            Assert.IsTrue(Ground.Passable(ctx, st, 22, 44), "poles are thin");
        }

        [Test]
        public void OccupancyFollowsTheStructuralRevision()
        {
            var (ctx, st) = WorldTestSupport.New();
            Assert.IsTrue(Ground.Passable(ctx, st, 20, 44));
            var m = WorldTestSupport.AddMachine(st, "assembler", 20, 44, 3);
            Assert.IsFalse(Ground.Passable(ctx, st, 22, 46), "a 3x3 footprint covers its whole rect");
            st.Machines.Remove(m);
            st.Rev++;
            Assert.IsTrue(Ground.Passable(ctx, st, 22, 46), "the cache rebuilds when Rev changes");
        }

        [Test]
        public void CanStandRefusesTheCornerOfABuilding()
        {
            var (ctx, st) = WorldTestSupport.New();
            // The building's north-west corner tile is (40, 8); a body centred exactly on that corner overlaps it.
            Assert.IsFalse(Ground.CanStand(ctx, st, 40.0, 8.0));
            Assert.IsTrue(Ground.CanStand(ctx, st, 39.0, 7.0), "clear of it by a whole tile");
        }

        [Test]
        public void CanStandRefusesADiagonalGapBetweenTwoMachines()
        {
            var (ctx, st) = WorldTestSupport.New();
            WorldTestSupport.AddMachine(st, "chest", 20, 44);
            WorldTestSupport.AddMachine(st, "chest", 21, 45);
            // The shared corner of the two chests: two of the four body corners are inside them.
            Assert.IsFalse(Ground.CanStand(ctx, st, 21.0, 45.0));
            Assert.IsTrue(Ground.CanStand(ctx, st, 21.5, 44.5), "the open tile beside them is fine");
        }

        [Test]
        public void DiggingAMinableTileReadsBackAsGround()
        {
            var (ctx, st) = WorldTestSupport.New();
            var t = 12 * SyntheticMap.Width + 12;
            Assert.AreEqual(TileClass.Rubble, Ground.TileAt(ctx, st, 12, 12));
            Assert.AreEqual(ctx.Data.World.RubbleUnitsPerTile, Ground.UnitsAt(ctx, st, 12, 12), 1e-9);
            st.Ground.SetDug(t, SyntheticMap.Width * SyntheticMap.Height, 10);
            Assert.AreEqual(TileClass.Rubble, Ground.TileAt(ctx, st, 12, 12), "still rubble while units remain");
            st.Ground.SetDug(t, SyntheticMap.Width * SyntheticMap.Height, 0);
            Assert.AreEqual(TileClass.Ground, Ground.TileAt(ctx, st, 12, 12));
        }

        [Test]
        public void ReachIsEightTilesAndNeedsLineOfSight()
        {
            var (ctx, st) = WorldTestSupport.New();
            Assert.AreEqual(8, ctx.Data.Engineer.ReachTiles, 1e-9);

            WorldTestSupport.Place(st, 6.5, 6.5);
            Assert.IsTrue(Ground.InReach(ctx, st, 12, 6), "5.5 tiles away, clear street");
            Assert.IsFalse(Ground.InReach(ctx, st, 16, 6), "9.5 tiles away");

            WorldTestSupport.Place(st, 10.5, 44.5);
            Assert.IsTrue(Ground.InReach(ctx, st, 17, 44), "6.5 tiles, nothing in the way");

            WorldTestSupport.Place(st, 39.0, 11.5);
            Assert.IsFalse(Ground.InReach(ctx, st, 46, 11), "7 tiles, but the building is between them");
        }

        [Test]
        public void NearestOpenSnapsAGoalOutOfTheRiver()
        {
            var (ctx, st) = WorldTestSupport.New();
            Assert.IsTrue(Ground.NearestOpen(ctx, st, 20, 44, 4, out var same));
            Assert.AreEqual(new TilePoint(20, 44), same);

            Assert.IsTrue(Ground.NearestOpen(ctx, st, 10, 25, 4, out var off));
            Assert.IsFalse(ctx.Geometry.TileAt(off.X, off.Y) == TileClass.River);
            Assert.AreEqual(10, off.X, "the scan is symmetric in x, so the first best is straight up");
            Assert.AreEqual(23, off.Y);
        }
    }
}
