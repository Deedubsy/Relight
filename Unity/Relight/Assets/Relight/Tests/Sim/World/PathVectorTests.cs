using NUnit.Framework;

namespace Relight.Sim.Tests
{
    /// <summary>
    /// Vector check: the ported A* must return the SAME path, tile for tile, as the reference algorithm — not merely
    /// a path of the same length. The expected strings were produced by running the body of
    /// <c>packages/sim/src/walk.ts:80-142</c>, copied verbatim into a node script, over the same synthetic map
    /// (script kept in scratchpad/B08/refpath.js; nothing under packages/ was modified). All four cases matched
    /// character for character, which is what pins the neighbour order, the heap tie-breaks and the octile
    /// heuristic.
    /// </summary>
    public sealed class PathVectorTests
    {
        [Test]
        public void NorthToSouthAcrossTheRiver()
        {
            Assert.AreEqual(
                "7,7 8,8 9,9 10,10 11,11 12,12 13,13 14,14 15,15 16,16 17,17 18,18 19,19 20,20 21,21 22,22 23,22 24,22 25,22 26,22 27,22 28,22 29,22 30,23 30,24 30,25 30,26 30,27 30,28 29,29 28,30 27,31 26,32 25,33 24,34 23,35 22,36 21,37 20,38 19,39 18,40 17,41 16,42 15,42 14,43 13,44 12,44 11,44 10,44 9,44 8,44 7,44 6,44",
                Run(6, 6, 6, 44, 0));
        }

        [Test]
        public void RoundTheBuilding()
        {
            Assert.AreEqual(
                "39,10 39,9 39,8 39,7 40,7 41,7 42,7 43,7 44,7 45,7 46,7 47,7 48,7 49,7 50,7 50,8 50,9 51,10 52,11",
                Run(38, 11, 52, 11, 0));
        }

        [Test]
        public void NorthToSouthWithClearanceOne()
        {
            Assert.AreEqual(
                "7,7 8,8 9,9 10,10 11,11 12,12 13,13 14,14 15,15 16,16 17,17 18,18 19,19 20,20 21,21 22,21 23,21 24,21 25,21 26,21 27,21 28,21 29,21 30,21 31,22 31,23 31,24 31,25 31,26 31,27 31,28 31,29 30,30 29,31 28,32 27,33 26,34 25,35 24,36 23,37 22,38 21,39 20,40 19,41 18,42 17,42 16,42 15,42 14,42 13,42 12,42 11,42 10,43 9,43 8,43 7,43 6,44",
                Run(6, 6, 6, 44, 1));
        }

        [Test]
        public void AShortStraightRun()
        {
            Assert.AreEqual(
                "11,44 12,44 13,44 14,44 15,44 16,44 17,44",
                Run(10, 44, 17, 44, 0));
        }

        private static string Run(int sx, int sy, int gx, int gy, int clearance)
        {
            var (ctx, st) = WorldTestSupport.New();
            var p = PathFinder.FindPath(ctx, st, sx, sy, gx, gy, PathFinder.DefaultLimit, clearance);
            if (p == null) return "null";
            var sb = new System.Text.StringBuilder();
            for (var i = 0; i < p.Count; i++) sb.Append(i == 0 ? "" : " ").Append(p[i].X).Append(',').Append(p[i].Y);
            return sb.ToString();
        }
    }
}
