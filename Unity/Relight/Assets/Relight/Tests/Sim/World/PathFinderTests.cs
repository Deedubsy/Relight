using NUnit.Framework;

namespace Relight.Sim.Tests
{
    /// <summary>A* over the synthetic map (reference walk.ts:80 findPath).</summary>
    public sealed class PathFinderTests
    {
        [Test]
        public void ReturnsAnEmptyPathWhenAlreadyOnTheGoal()
        {
            var (ctx, st) = WorldTestSupport.New();
            var p = PathFinder.FindPath(ctx, st, 20, 44, 20, 44);
            Assert.IsNotNull(p);
            Assert.AreEqual(0, p.Count);
        }

        [Test]
        public void NorthToSouthGoesThroughTheOneRiverCrossing()
        {
            var (ctx, st) = WorldTestSupport.New();
            var p = PathFinder.FindPath(ctx, st, 6, 6, 6, 44);
            Assert.IsNotNull(p, "there is a way south");

            var crossed = false;
            for (var i = 0; i < p.Count; i++)
            {
                var t = p[i];
                Assert.AreNotEqual(TileClass.River, ctx.Geometry.TileAt(t.X, t.Y), $"stepped into the river at {t}");
                if (t.Y >= 24 && t.Y < 28)
                {
                    Assert.IsTrue(t.X >= 30 && t.X < 34, $"crossed the river outside the bridge at {t}");
                    crossed = true;
                }
            }
            Assert.IsTrue(crossed, "the path must use the crossing");
            Assert.AreEqual(new TilePoint(6, 44), p[p.Count - 1], "ends on the goal");
        }

        [Test]
        public void TheSamePathTwice()
        {
            var (ctx, st) = WorldTestSupport.New();
            var a = PathFinder.FindPath(ctx, st, 6, 6, 6, 44);
            var b = PathFinder.FindPath(ctx, st, 6, 6, 6, 44);
            Assert.IsNotNull(a);
            Assert.IsNotNull(b);
            Assert.AreEqual(a.Count, b.Count);
            for (var i = 0; i < a.Count; i++) Assert.AreEqual(a[i], b[i], $"step {i}");

            // And on a state built from scratch, so the result does not depend on the reused scratch arrays.
            var (ctx2, st2) = WorldTestSupport.New();
            var c = PathFinder.FindPath(ctx2, st2, 6, 6, 6, 44);
            Assert.AreEqual(a.Count, c.Count);
            for (var i = 0; i < a.Count; i++) Assert.AreEqual(a[i], c[i], $"step {i}");
        }

        [Test]
        public void NoPathIntoTheRiverOrIntoABuilding()
        {
            var (ctx, st) = WorldTestSupport.New();
            Assert.IsNull(PathFinder.FindPath(ctx, st, 6, 6, 10, 25), "goal in the river");
            Assert.IsNull(PathFinder.FindPath(ctx, st, 6, 6, 44, 10), "goal inside the building");
            Assert.IsNull(PathFinder.FindPath(ctx, st, 6, 6, -1, 6), "goal off the map");
        }

        [Test]
        public void PathsRouteAroundASolidBuilding()
        {
            var (ctx, st) = WorldTestSupport.New();
            var p = PathFinder.FindPath(ctx, st, 38, 11, 52, 11);
            Assert.IsNotNull(p);
            for (var i = 0; i < p.Count; i++)
            {
                var t = p[i];
                var inside = t.X >= 40 && t.X < 50 && t.Y >= 8 && t.Y < 16;
                Assert.IsFalse(inside, $"walked into the building at {t}");
            }
        }

        [Test]
        public void AWallOfMachinesCanCloseTheOnlyRoute()
        {
            var (ctx, st) = WorldTestSupport.New();
            Assert.IsNotNull(PathFinder.FindPath(ctx, st, 6, 6, 6, 44), "open before");
            for (var x = 30; x < 34; x++)
                for (var y = 24; y < 28; y++)
                    WorldTestSupport.AddMachine(st, "chest", x, y);
            Assert.IsNull(PathFinder.FindPath(ctx, st, 6, 6, 6, 44), "the bridge is blocked");
        }

        [Test]
        public void BeltsDoNotBlockAPath()
        {
            var (ctx, st) = WorldTestSupport.New();
            for (var x = 30; x < 34; x++)
                for (var y = 24; y < 28; y++)
                    WorldTestSupport.AddMachine(st, "belt", x, y);
            Assert.IsNotNull(PathFinder.FindPath(ctx, st, 6, 6, 6, 44), "the engineer walks over belts");
        }

        [Test]
        public void NoCornerCutting()
        {
            var (ctx, st) = WorldTestSupport.New();
            // A diagonal pinch: the only diagonal step between (20,44) and (21,45) has both its sides blocked.
            WorldTestSupport.AddMachine(st, "chest", 21, 44);
            WorldTestSupport.AddMachine(st, "chest", 20, 45);
            var p = PathFinder.FindPath(ctx, st, 20, 44, 21, 45);
            Assert.IsNotNull(p);
            Assert.IsTrue(p.Count > 1, "it must go round, not slip through the corner");
        }

        [Test]
        public void ClearanceNeedsRoomOnEveryStep()
        {
            var (ctx, st) = WorldTestSupport.New();
            // Clearance 1 needs a 3x3 clear box; the 4-wide bridge still allows it, a 1-wide gap would not.
            Assert.IsNotNull(PathFinder.FindPath(ctx, st, 6, 6, 6, 44, PathFinder.DefaultLimit, 1));
            // Right against the building wall there is no room for a clearance-1 body.
            Assert.IsNull(PathFinder.FindPath(ctx, st, 38, 11, 39, 11, PathFinder.DefaultLimit, 1));
        }

        [Test]
        public void TheExpansionLimitGivesUp()
        {
            var (ctx, st) = WorldTestSupport.New();
            Assert.IsNull(PathFinder.FindPath(ctx, st, 6, 6, 6, 44, 5), "five expansions cannot reach the far bank");
        }
    }
}
