using NUnit.Framework;

namespace Relight.Sim.Tests.Production
{
    /// <summary>
    /// B-10 splitters and underground pairs, against reference routing.ts <c>tickRouting</c>,
    /// <c>splitterPorts</c>, <c>undergroundMate</c> and <c>routingAccepts</c>.
    /// </summary>
    public sealed class RoutingTests
    {
        /// <summary>chest -> belt -> splitter (facing east, so 1x2 over y 6..7) -> two belts -> two chests.</summary>
        private static (SimContext Ctx, SimState St, Machine Src, Machine Split, Machine A, Machine B) Fork(int steel)
        {
            var ctx = FlowFixture.Context();
            var st = FlowFixture.State(ctx);
            var src = FlowFixture.Add(ctx, st, "chest", 2, 6);          // (2..3, 6..7)
            FlowFixture.Add(ctx, st, "belt", 4, 6, Dir.E);              // into the splitter's left rear port
            var split = FlowFixture.Add(ctx, st, "splitter", 5, 6, Dir.E);
            FlowFixture.Add(ctx, st, "belt", 6, 6, Dir.E);
            FlowFixture.Add(ctx, st, "belt", 6, 7, Dir.E);
            var a = FlowFixture.Add(ctx, st, "chest", 7, 5);            // (7..8, 5..6)
            var b = FlowFixture.Add(ctx, st, "chest", 7, 7);            // (7..8, 7..8)
            if (steel > 0) src.Inv.Add(ItemId.Steel, steel);
            FlowFixture.Seal(ctx, st);
            return (ctx, st, src, split, a, b);
        }

        [Test]
        public void ASplitterFacingEastIsOneWideAndTwoTall()
        {
            var ctx = FlowFixture.Context();
            var st = FlowFixture.State(ctx);
            Assert.That(FlowFixture.Add(ctx, st, "splitter", 5, 6, Dir.E).Dimensions, Is.EqualTo((1, 2)));
            Assert.That(FlowFixture.Add(ctx, st, "splitter", 5, 9, Dir.N).Dimensions, Is.EqualTo((2, 1)));
        }

        /// <summary>routing.ts:69: a balanced splitter alternates, and every item comes out exactly once.</summary>
        [Test]
        public void ABalancedSplitterHalvesTheLineAndLosesNothing()
        {
            var (ctx, st, src, _, a, b) = Fork(40);

            for (var i = 0; i < 60; i++)
            {
                FlowFixture.Run(ctx, st, 10);
                Assert.That(FlowFixture.TotalItems(st, ItemId.Steel), Is.EqualTo(40), "count in == count out + in transit");
            }

            Assert.That(src.Inv[ItemId.Steel], Is.Zero);
            Assert.That(a.Inv[ItemId.Steel] + b.Inv[ItemId.Steel], Is.EqualTo(40));
            Assert.That(System.Math.Abs(a.Inv[ItemId.Steel] - b.Inv[ItemId.Steel]), Is.LessThanOrEqualTo(2), "balanced");
            Assert.That(FlowFixture.Off(ctx, st), Is.Empty);
        }

        /// <summary>
        /// routing.ts:71's <c>priority</c>: 1 prefers the left port, 2 the right. routing.ts:72's
        /// <c>for (const side of [first, 1 - side])</c> is the documented fall-back ("blocked priority falls back",
        /// routing.ts:120), so the far side still gets the occasional item when the preferred belt is momentarily
        /// full — a splitter runs at SPLITTER_PER_S 15 into a belt that only takes one item every three ticks.
        /// </summary>
        [Test]
        public void APrioritySplitterSendsEverythingOneWay()
        {
            foreach (var (priority, preferred) in new[] { (1, 0), (2, 1) })
            {
                var (ctx, st, _, split, a, b) = Fork(20);
                Assert.That(FlowBuild.SetPriority(ctx, st, split.Id, priority).ok, Is.True);
                Assert.That(FlowQueries.Priority(st, split.Id), Is.EqualTo(priority));

                FlowFixture.Run(ctx, st, 600);
                var chests = new[] { a.Inv[ItemId.Steel], b.Inv[ItemId.Steel] };
                Assert.That(chests[0] + chests[1], Is.EqualTo(20), "nothing lost or duplicated");
                Assert.That(chests[preferred], Is.GreaterThanOrEqualTo(19), "priority " + priority + " takes the near side");
                Assert.That(chests[1 - preferred], Is.LessThanOrEqualTo(1), "and only a blocked port falls back");
                Assert.That(FlowFixture.Off(ctx, st), Is.Empty);
            }
        }

        /// <summary>The same setting with the far port leading nowhere: the fall-back has nowhere to go, so it is absolute.</summary>
        [Test]
        public void APrioritySplitterWithNoFarPortSendsEverythingOneWay()
        {
            var ctx = FlowFixture.Context();
            var st = FlowFixture.State(ctx);
            var src = FlowFixture.Add(ctx, st, "chest", 2, 6);
            FlowFixture.Add(ctx, st, "belt", 4, 6, Dir.E);
            var split = FlowFixture.Add(ctx, st, "splitter", 5, 6, Dir.E);
            FlowFixture.Add(ctx, st, "belt", 6, 6, Dir.E);              // left port only; (6, 7) is bare ground
            var a = FlowFixture.Add(ctx, st, "chest", 7, 5);
            src.Inv.Add(ItemId.Steel, 20);
            FlowFixture.Seal(ctx, st);
            Assert.That(FlowBuild.SetPriority(ctx, st, split.Id, 1).ok, Is.True);

            FlowFixture.Run(ctx, st, 600);
            Assert.That(a.Inv[ItemId.Steel], Is.EqualTo(20), "left port only");
            Assert.That(FlowFixture.Off(ctx, st), Is.Empty);
        }

        /// <summary>routing.ts:35 <c>routingAccepts</c>: SPLITTER_CAPACITY is 8 items.</summary>
        [Test]
        public void ASplitterBuffersAtMostEightItems()
        {
            var ctx = FlowFixture.Context();
            var st = FlowFixture.State(ctx);
            var src = FlowFixture.Add(ctx, st, "chest", 2, 6);
            FlowFixture.Add(ctx, st, "belt", 4, 6, Dir.E);
            var split = FlowFixture.Add(ctx, st, "splitter", 5, 6, Dir.E);   // both front ports lead nowhere
            src.Inv.Add(ItemId.Steel, 50);
            FlowFixture.Seal(ctx, st);

            FlowFixture.Run(ctx, st, 400);
            Assert.That(FlowQueries.Count(st, split.Id), Is.EqualTo(8), "SPLITTER_CAPACITY");
        }

        /// <summary>chest -> belt -> underground entrance -(4 hidden tiles)- exit -> belt -> chest.</summary>
        private static (SimContext Ctx, SimState St, Machine Src, Machine In, Machine Out, Machine Dst) Tunnel(int steel)
        {
            var ctx = FlowFixture.Context();
            var st = FlowFixture.State(ctx);
            var src = FlowFixture.Add(ctx, st, "chest", 2, 6);
            FlowFixture.Add(ctx, st, "belt", 4, 6, Dir.E);
            var entrance = FlowFixture.Add(ctx, st, "underground", 5, 6, Dir.E);
            var exit = FlowFixture.Add(ctx, st, "underground", 9, 6, Dir.E);
            st.Flow.Of(exit.Id).Mode = 1;                               // routing.ts: the far endpoint is the 'output'
            FlowFixture.Add(ctx, st, "belt", 10, 6, Dir.E);
            var dst = FlowFixture.Add(ctx, st, "chest", 11, 6);
            if (steel > 0) src.Inv.Add(ItemId.Steel, steel);
            FlowFixture.Seal(ctx, st);
            return (ctx, st, src, entrance, exit, dst);
        }

        [Test]
        public void AnUndergroundPairPairsUpAndCarriesEveryItemAcrossTheGap()
        {
            var (ctx, st, src, entrance, exit, dst) = Tunnel(30);

            Assert.That(FlowQueries.IsUndergroundExit(st, exit), Is.True);
            Assert.That(FlowQueries.IsUndergroundExit(st, entrance), Is.False);
            Assert.That(FlowQueries.MateId(st, entrance), Is.EqualTo(exit.Id), "the entrance looks forward");
            Assert.That(FlowQueries.MateId(st, exit), Is.EqualTo(entrance.Id), "and the exit looks back");

            for (var i = 0; i < 80; i++)
            {
                FlowFixture.Run(ctx, st, 10);
                Assert.That(FlowFixture.TotalItems(st, ItemId.Steel), Is.EqualTo(30));
            }

            Assert.That(src.Inv[ItemId.Steel], Is.Zero);
            Assert.That(dst.Inv[ItemId.Steel], Is.EqualTo(30));
            Assert.That(FlowQueries.HeldCount(st), Is.Zero);
            Assert.That(FlowFixture.Off(ctx, st), Is.Empty);
        }

        /// <summary>routing.ts:23 <c>undergroundSpan</c>: at most four hidden tiles, and the exit must lie along the entrance.</summary>
        [Test]
        public void AnUndergroundSpanIsAtMostFourHiddenTiles()
        {
            Assert.That(FlowRules.UndergroundSpan(5, 6, 6, 6, Dir.E), Is.Empty, "adjacent");
            Assert.That(FlowRules.UndergroundSpan(5, 6, 10, 6, Dir.E), Is.Empty, "four hidden tiles");
            Assert.That(FlowRules.UndergroundSpan(5, 6, 11, 6, Dir.E), Is.Not.Empty, "five is too far");
            Assert.That(FlowRules.UndergroundSpan(5, 6, 8, 7, Dir.E), Is.Not.Empty, "off the axis");
            Assert.That(FlowRules.UndergroundSpan(5, 6, 3, 6, Dir.E), Is.Not.Empty, "behind the entrance");
        }

        /// <summary>
        /// flow.ts:810 <c>nextOf</c> through routing.ts:45 <c>routingEntry</c>: an underground entrance takes items
        /// only through the tile behind it, and an exit takes nothing at all from a belt.
        /// </summary>
        [Test]
        public void AnUndergroundExitRefusesItemsFromABeltPointingIntoIt()
        {
            var ctx = FlowFixture.Context();
            var st = FlowFixture.State(ctx);
            var src = FlowFixture.Add(ctx, st, "chest", 2, 6);
            var belt = FlowFixture.Add(ctx, st, "belt", 4, 6, Dir.E);
            var exit = FlowFixture.Add(ctx, st, "underground", 5, 6, Dir.E);
            st.Flow.Of(exit.Id).Mode = 1;
            src.Inv.Add(ItemId.Steel, 10);
            FlowFixture.Seal(ctx, st);

            Assert.That(FlowRules.NextOf(st, belt), Is.Null, "an exit is not an entry");
            FlowFixture.Run(ctx, st, 200);
            Assert.That(FlowQueries.Count(st, exit.Id), Is.Zero, "nothing got in the back of the exit");
            Assert.That(FlowQueries.Count(st, belt.Id), Is.EqualTo(4), "the belt simply backs up");
        }
    }
}
