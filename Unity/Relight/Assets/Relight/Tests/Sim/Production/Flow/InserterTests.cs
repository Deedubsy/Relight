using NUnit.Framework;

namespace Relight.Sim.Tests.Production
{
    /// <summary>
    /// B-10 inserters and the power rule of row B-10 / U-D-12, against reference flow.ts <c>tickInserter</c>
    /// (INSERTER_PER_S 1, so INSERTER_SWING is 0.5 s out and 0.5 s back).
    /// </summary>
    public sealed class InserterTests
    {
        /// <summary>A fuelled circuit with chest -> inserter -> chest hanging off it.</summary>
        private static (SimContext Ctx, SimState St, Machine Src, Machine Ins, Machine Dst) Bench(int steel, int coal = 50)
        {
            var ctx = FlowFixture.Context();
            var st = FlowFixture.State(ctx);
            st.Engineer.Pos = new Vec2(16.5, 17.5);                     // in reach of the inserter, for SetFilter
            ProductionFixture.Grid(ctx, st, 20, 18, 18, 14, coal);
            var src = FlowFixture.Add(ctx, st, "chest", 14, 17);        // (14..15, 17..18)
            var ins = FlowFixture.Add(ctx, st, "inserter", 16, 17, Dir.E);
            var dst = FlowFixture.Add(ctx, st, "chest", 17, 17);        // (17..18, 17..18)
            if (steel > 0) src.Inv.Add(ItemId.Steel, steel);
            FlowFixture.Seal(ctx, st);
            return (ctx, st, src, ins, dst);
        }

        /// <summary>
        /// flow.ts:901: the arm picks up, swings for INSERTER_SWING, hands over, swings back, and picks up again in
        /// the same tick it lands — one item a second, INSERTER_PER_S.
        /// </summary>
        [Test]
        public void AnInserterMovesOneItemPerSecond()
        {
            var (ctx, st, src, ins, dst) = Bench(30);

            FlowFixture.Run(ctx, st, 1);
            Assert.That(FlowQueries.Holding(st, ins.Id), Is.EqualTo(ItemId.Steel), "it grabs on the first tick");
            Assert.That(src.Inv[ItemId.Steel], Is.EqualTo(29), "the item left the chest with the hand");
            Assert.That(dst.Inv[ItemId.Steel], Is.Zero, "and has not arrived yet");

            FlowFixture.Run(ctx, st, 9);
            Assert.That(dst.Inv[ItemId.Steel], Is.Zero, "0.5 s of swing has not elapsed at 0.45 s");
            Assert.That(FlowQueries.SwingFraction(ctx.Data, st, ins.Id), Is.GreaterThan(0).And.LessThan(1));

            FlowFixture.Run(ctx, st, 1);
            Assert.That(dst.Inv[ItemId.Steel], Is.EqualTo(1), "handed over after INSERTER_SWING");
            Assert.That(FlowQueries.Holding(st, ins.Id), Is.Null, "the hand is empty on the way back");

            FlowFixture.Run(ctx, st, 200);                              // 10 s
            Assert.That(dst.Inv[ItemId.Steel], Is.EqualTo(11).Within(1), "one item a second");
            Assert.That(FlowFixture.TotalItems(st, ItemId.Steel), Is.EqualTo(30), "nothing lost in the hand");
        }

        /// <summary>flow.ts:887 <c>inserterPickup</c>: a filter makes the arm take only the one item.</summary>
        [Test]
        public void AFilteredInserterTakesOnlyTheFilteredItem()
        {
            var (ctx, st, src, ins, dst) = Bench(0);
            src.Inv.Add(ItemId.Coal, 10);
            src.Inv.Add(ItemId.Steel, 10);
            Assert.That(FlowBuild.SetFilter(ctx, st, ins.Id, (int)ItemId.Coal).ok, Is.True);
            Assert.That(FlowQueries.Filter(st, ins.Id), Is.EqualTo(ItemId.Coal));

            FlowFixture.Run(ctx, st, 400);
            Assert.That(dst.Inv[ItemId.Coal], Is.EqualTo(10));
            Assert.That(dst.Inv[ItemId.Steel], Is.Zero, "the filter refused the steel");
            Assert.That(src.Inv[ItemId.Steel], Is.EqualTo(10));
        }

        /// <summary>
        /// An inserter on a dead circuit gets throttle 0. Reference flow.ts:1093 skips the whole machine loop body
        /// for anything <c>running(st, m)</c> refuses, and flow.ts:658 <c>running</c> refuses an unpowered machine —
        /// so the arm does not even grab. (Calling <c>tickInserter</c> with <c>dt = 0</c> would still pick up, because
        /// its phase-0 branch has no timer in it; the port gates on the throttle exactly as W-B's MachinePhase does.)
        /// </summary>
        [Test]
        public void AnUnpoweredInserterDoesNothing()
        {
            var (ctx, st, src, ins, dst) = Bench(10, coal: 0);
            FlowFixture.Run(ctx, st, 400);
            Assert.That(PowerQueries.Throttle(ctx, st, ins.Id), Is.Zero);
            Assert.That(dst.Inv[ItemId.Steel], Is.Zero);
            Assert.That(FlowQueries.Holding(st, ins.Id), Is.Null, "not even a grab");
            Assert.That(src.Inv[ItemId.Steel], Is.EqualTo(10));
        }

        /// <summary>
        /// Row B-10 / U-D-12: conveyors draw no power. The catalogue is what makes that true — belt, fast belt,
        /// underground and splitter all carry PowerKw 0 — so the check is on the grid's demand, not on a special case
        /// in the flow code.
        /// </summary>
        [Test]
        public void ConveyorsRegisterNoPowerDemandAndAnInserterRegistersTen()
        {
            var ctx = FlowFixture.Context();
            var st = FlowFixture.State(ctx);
            ProductionFixture.Grid(ctx, st, 20, 18, 18, 14, 50);
            FlowFixture.Add(ctx, st, "belt", 16, 17, Dir.E);
            FlowFixture.Add(ctx, st, "fastbelt", 17, 17, Dir.E);
            FlowFixture.Add(ctx, st, "underground", 18, 17, Dir.E);
            FlowFixture.Add(ctx, st, "splitter", 19, 17, Dir.E);
            Assert.That(PowerQueries.Network(ctx, st).DemandKw, Is.Zero, "U-D-12: conveyors draw nothing");

            FlowFixture.Add(ctx, st, "inserter", 16, 19, Dir.E);
            Assert.That(PowerQueries.Network(ctx, st).DemandKw, Is.EqualTo(10),
                "and an inserter draws whatever the catalogue says");
        }
    }
}
