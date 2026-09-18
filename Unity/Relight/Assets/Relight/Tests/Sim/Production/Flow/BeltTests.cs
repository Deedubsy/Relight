using NUnit.Framework;

namespace Relight.Sim.Tests.Production
{
    /// <summary>
    /// B-10 belts and direct conveyor loading, against reference flow.ts <c>tickBelt</c>/<c>beltOrder</c> and
    /// directConveyor.ts <c>loadConveyor</c>. Tuning under test: BELT_SPACING .25, BELT_PER_S 7.5,
    /// FAST_BELT_PER_S 15, so a belt runs at 1.875 tiles/s and a fast belt at 3.75.
    /// </summary>
    public sealed class BeltTests
    {
        /// <summary>chest -> belts -> chest, all facing east along y = 6.</summary>
        private static (SimContext Ctx, SimState St, Machine Src, Machine Dst) Line(string kind, int belts, int steel)
        {
            var ctx = FlowFixture.Context();
            var st = FlowFixture.State(ctx);
            var src = FlowFixture.Add(ctx, st, "chest", 2, 6);          // 2x2 over (2..3, 6..7)
            for (var i = 0; i < belts; i++) FlowFixture.Add(ctx, st, kind, 4 + i, 6, Dir.E);
            var dst = FlowFixture.Add(ctx, st, "chest", 4 + belts, 6);
            if (steel > 0) src.Inv.Add(ItemId.Steel, steel);
            FlowFixture.Seal(ctx, st);
            return (ctx, st, src, dst);
        }

        [Test]
        public void ABeltCarriesAnItemFromOneChestToTheNextWithNoInserter()
        {
            var (ctx, st, src, dst) = Line("belt", 3, 1);

            // directConveyor.ts:52 loadConveyor — the first belt pulls straight out of the chest behind it.
            FlowFixture.Run(ctx, st, 1);
            Assert.That(src.Inv[ItemId.Steel], Is.Zero, "the chest behind the belt was drawn from directly");
            Assert.That(FlowQueries.HeldCount(st), Is.EqualTo(1), "and the item is on the belt, not lost");

            // 3 tiles at 1.875 tiles/s is 1.6 s; it cannot arrive before that and must arrive soon after.
            FlowFixture.Run(ctx, st, 30);
            Assert.That(dst.Inv[ItemId.Steel], Is.Zero, "1.55 s is not enough to cross three tiles");
            FlowFixture.Run(ctx, st, 6);
            Assert.That(dst.Inv[ItemId.Steel], Is.EqualTo(1), "three tiles at 1.875 tiles/s is 1.6 s");
            Assert.That(FlowQueries.HeldCount(st), Is.Zero, "nothing is still in transit");
            Assert.That(FlowFixture.Off(ctx, st), Is.Empty, "with the belts empty the ledger balances");
        }

        /// <summary>
        /// flow.ts:755 <c>beltRoom</c>: items sit one BELT_SPACING apart, so one dead-end tile holds exactly four
        /// items and no more. flow.ts:862's <c>np = 1 - BELT_SPACING / 2</c> is then floored by <c>Math.max(it.p, np)</c>
        /// (flow.ts:866), so the leader does not slide backwards: it simply freezes on the last step that would have
        /// carried it off the end, somewhere in [1 - adv, 1).
        /// </summary>
        [Test]
        public void ADeadEndBeltPacksExactlyFourItemsOneSpacingApart()
        {
            var ctx = FlowFixture.Context();
            var st = FlowFixture.State(ctx);
            var src = FlowFixture.Add(ctx, st, "chest", 2, 6);
            var belt = FlowFixture.Add(ctx, st, "belt", 4, 6, Dir.E);   // nothing in front of it
            src.Inv.Add(ItemId.Steel, 50);
            FlowFixture.Seal(ctx, st);

            FlowFixture.Run(ctx, st, 200);
            Assert.That(FlowQueries.Count(st, belt.Id), Is.EqualTo(4), "1 tile / 0.25 spacing = 4 slots");
            Assert.That(src.Inv[ItemId.Steel], Is.EqualTo(46), "the chest kept everything that did not fit");

            var items = new System.Collections.Generic.List<BeltItemView>();
            FlowQueries.ItemsOn(st, belt, items);
            var adv = 7.5 * 0.25 * (1.0 / 20);                      // BELT_PER_S x BELT_SPACING x TILE_DT
            Assert.That(items[3].P, Is.LessThan(1).And.GreaterThanOrEqualTo(1 - adv),
                "flow.ts:866 Math.max: the blocked leader freezes where it stands, it never slides back");
            var frozen = items[3].P;
            FlowFixture.Run(ctx, st, 20);
            FlowQueries.ItemsOn(st, belt, items);
            Assert.That(items[3].P, Is.EqualTo(frozen).Within(1e-12), "and it stays there");
            for (var i = 3; i > 0; i--)
                Assert.That(items[i].P - items[i - 1].P, Is.EqualTo(0.25).Within(1e-9), "one BELT_SPACING apart");
        }

        /// <summary>BELT_PER_S 7.5: at a steady state the line delivers 7.5 items a second, and a fast belt 15.</summary>
        [Test]
        public void AFullLineDeliversAtTheCatalogueRate()
        {
            foreach (var (kind, perSecond) in new[] { ("belt", 7.5), ("fastbelt", 15.0) })
            {
                var (ctx, st, _, dst) = Line(kind, 3, 400);
                FlowFixture.Run(ctx, st, 80);                       // 4 s: the line is full
                var before = dst.Inv[ItemId.Steel];
                FlowFixture.Run(ctx, st, 40);                       // 2 s of steady state
                Assert.That(dst.Inv[ItemId.Steel] - before, Is.EqualTo(perSecond * 2).Within(1.0),
                    kind + " runs at " + perSecond + " items a second");
            }
        }

        /// <summary>
        /// flow.ts:1063 <c>stepFlow</c> steps belts in <c>beltOrder</c> — leaders first — so a long line moves one
        /// whole step a tick rather than one belt a tick. Nothing may be lost or duplicated on the way.
        /// </summary>
        [Test]
        public void ALongLineLosesNothingAndDuplicatesNothing()
        {
            var (ctx, st, src, dst) = Line("belt", 12, 60);
            var total = FlowFixture.TotalItems(st, ItemId.Steel);
            Assert.That(total, Is.EqualTo(60));

            for (var i = 0; i < 40; i++)
            {
                FlowFixture.Run(ctx, st, 5);
                Assert.That(FlowFixture.TotalItems(st, ItemId.Steel), Is.EqualTo(60), "every item is in exactly one place");
            }

            FlowFixture.Run(ctx, st, 400);
            Assert.That(src.Inv[ItemId.Steel], Is.Zero);
            Assert.That(dst.Inv[ItemId.Steel], Is.EqualTo(60));
            Assert.That(FlowQueries.HeldCount(st), Is.Zero);
            Assert.That(FlowFixture.Off(ctx, st), Is.Empty);
        }

        /// <summary>
        /// directConveyor.ts:23 <c>roomAtEnd</c>: the line reserves room for what is already travelling, so a full
        /// destination stops the loading rather than overfilling. The Supply chest caps at 200.
        /// </summary>
        [Test]
        public void AFullDestinationStopsTheLineInsteadOfOverfillingIt()
        {
            var (ctx, st, src, dst) = Line("belt", 3, 400);
            FlowFixture.Run(ctx, st, 2000);
            Assert.That(dst.Inv[ItemId.Steel], Is.EqualTo(200), "the Supply chest cap");
            Assert.That(FlowFixture.TotalItems(st, ItemId.Steel), Is.EqualTo(400), "and nothing went missing while it filled");
            Assert.That(src.Inv[ItemId.Steel] + FlowQueries.HeldCount(st), Is.EqualTo(200));
        }
    }
}
