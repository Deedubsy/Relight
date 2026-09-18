using System.Collections.Generic;
using NUnit.Framework;

namespace Relight.Sim.Tests
{
    /// <summary>
    /// A flat street map with a short authored rubble seam beside the spawn, because the shared
    /// <see cref="FlatGeometry"/> is street everywhere and <c>Tiles.CanHoldUnits</c> rejects street.
    /// Row y = 16 from x = 18 to x = 22 is rubble, (23,16) is a copper patch, and (30,30) is a far
    /// rubble tile used for the reach refusal.
    /// </summary>
    public sealed class MineGeometry : ICityGeometry
    {
        public int Width => 32;
        public int Height => 32;
        public bool Solid(int x, int y) => false;

        public TileClass TileAt(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height) return TileClass.Void;
            if (y == 16 && x >= 18 && x <= 22) return TileClass.Rubble;
            if (y == 16 && x == 23) return TileClass.Patch;
            if (x == 30 && y == 30) return TileClass.Rubble;
            return TileClass.Street;
        }

        public Vec2 Spawn => new Vec2(16, 16);
        public bool Sight(double x0, double y0, double x1, double y1) => true;
    }

    [TestFixture]
    public sealed class MiningTests
    {
        private static SimContext Context() => new SimContext(ReferenceData.Create(), new MineGeometry());

        private static SimState State(SimContext ctx)
        {
            var st = new SimState();
            st.Engineer.Pos = ctx.Geometry.Spawn;
            st.Engineer.Hp = ctx.Data.Engineer.MaxHp;
            new InventoryInitializer().Init(ctx, st);
            return st;
        }

        private static void Run(SimContext ctx, SimState st, int ticks)
        {
            var phase = new MiningPhase();
            for (var i = 0; i < ticks; i++)
            {
                phase.Tick(ctx, st, Fixture.Dt);
                st.Tick++;
                st.T += Fixture.Dt;
            }
        }

        private static CommandResult Apply(SimContext ctx, SimState st, Command c)
        {
            var handlers = new List<ICommandHandler> { new MiningHandler() };
            for (var i = 0; i < handlers.Count; i++)
                if (handlers[i].TryApply(ctx, st, c, out var r)) return r;
            return CommandResult.Refuse("no handler");
        }

        private static int Stops(SimState st, out string lastReason)
        {
            var n = 0;
            lastReason = "";
            for (var i = 0; i < st.Events.Count; i++)
                if (st.Events[i] is MiningStoppedEvent s) { n++; lastReason = s.Reason; }
            return n;
        }

        [Test]
        public void OneUnitArrivesEveryFortyTicksAtTheProvisionalRate()
        {
            var ctx = Context();
            var st = State(ctx);
            Assert.AreEqual(0.5, ctx.Data.Engineer.HandMinePerS, 1e-9, "U-P-01 hand mining rate");

            var before = st.Engineer.Inv[ItemId.Steel];
            Assert.IsTrue(Apply(ctx, st, new MineCommand(18, 16)).Accepted);

            Run(ctx, st, 39);
            Assert.AreEqual(before, st.Engineer.Inv[ItemId.Steel], 1e-9, "nothing before the whole unit");

            Run(ctx, st, 1);
            Assert.AreEqual(before + 1, st.Engineer.Inv[ItemId.Steel], 1e-9, "one item at 40 ticks");
            Assert.AreEqual(1, st.Stats.Mined, "Stats.Mined counts the unit");
            Assert.AreEqual(1, st.Stats.MinedOf[ItemId.Steel], 1e-9);

            Run(ctx, st, 40);
            Assert.AreEqual(before + 2, st.Engineer.Inv[ItemId.Steel], 1e-9, "a second item 40 ticks later");
            Assert.AreEqual("", Fixture.Off(ctx, st), "mined items are a ledger source");
        }

        [Test]
        public void TheTileClassDecidesTheItemAndProgressIsReadable()
        {
            var ctx = Context();
            var st = State(ctx);
            Assert.IsTrue(MiningQueries.Minable(ctx, st, 23, 16, out var item));
            Assert.AreEqual(ItemId.Copper, item, "a patch yields copper");
            Assert.IsFalse(MiningQueries.Minable(ctx, st, 16, 17, out _), "street is not minable");

            Assert.IsTrue(Apply(ctx, st, new MineCommand(23, 16)).Accepted);
            Run(ctx, st, 20);
            var p = MiningQueries.Progress(ctx, st);
            Assert.IsTrue(p.Active);
            Assert.AreEqual(23, p.X);
            Assert.AreEqual(16, p.Y);
            Assert.AreEqual(ItemId.Copper, p.Item);
            Assert.AreEqual(0.5, p.Fraction, 1e-9, "half way to the next item after 20 ticks");
            Assert.AreEqual(0.5, p.RatePerS, 1e-9);
            Assert.AreEqual(ctx.Data.World.RubbleUnitsPerTile, p.UnitsLeft, 1e-9, "untouched tile is full");
        }

        [Test]
        public void MiningOutOfReachIsRefusedAndNothingStarts()
        {
            var ctx = Context();
            var st = State(ctx);
            var r = Apply(ctx, st, new MineCommand(30, 30));
            Assert.IsFalse(r.Accepted);
            Assert.AreEqual(Mining.ReachText, r.Problem);
            Assert.IsFalse(st.Engineer.Mining);

            var empty = Apply(ctx, st, new MineCommand(16, 17));
            Assert.IsFalse(empty.Accepted);
            Assert.AreEqual(Mining.EmptyText, empty.Problem, "street holds no units");
        }

        [Test]
        public void WalkingAwayStopsTheHandsWithAReason()
        {
            var ctx = Context();
            var st = State(ctx);
            Assert.IsTrue(Apply(ctx, st, new MineCommand(18, 16)).Accepted);
            Run(ctx, st, 10);
            Assert.IsTrue(st.Engineer.Mining);

            st.Engineer.Pos = new Vec2(2, 2);
            Run(ctx, st, 1);
            Assert.IsFalse(st.Engineer.Mining, "out of reach stops the hands");
            Assert.AreEqual(1, Stops(st, out var reason));
            Assert.AreEqual(Mining.ReachText, reason);
            Assert.AreEqual(0, st.Stats.Mined, "no part-dug unit is credited");
        }

        /// <summary>
        /// The two things that take the hands away from a seam. Since U-D-44 a queued workshop batch is not one of
        /// them — the workshop processes it on its own — so that is pinned here too, in the same place the old rule
        /// used to live.
        /// </summary>
        [Test]
        public void ARepairAndBeingDownBothBlockTheHandsButAQueuedBatchDoesNot()
        {
            var ctx = Context();
            var st = State(ctx);

            st.Home.RepairKind = RepairKinds.Machine;
            st.Home.RepairId = 1;
            st.Home.RepairRemaining = 10;
            var r = Apply(ctx, st, new MineCommand(18, 16));
            Assert.IsFalse(r.Accepted);
            Assert.AreEqual(Home.LockText, r.Problem);

            st.Home.RepairKind = RepairKinds.None;
            Assert.IsTrue(Apply(ctx, st, new MineCommand(18, 16)).Accepted);
            st.Home.RepairKind = RepairKinds.Machine;
            Run(ctx, st, 1);
            Assert.IsFalse(st.Engineer.Mining, "a repair started mid-dig takes the hands back");
            Assert.AreEqual(1, Stops(st, out var reason));
            Assert.AreEqual(Home.LockText, reason);

            // U-D-44: the workshop's own queue never reaches into the dig.
            st.Home.RepairKind = RepairKinds.None;
            st.Hand.Jobs.Add(new WorkshopJob { Id = 1, RecipeKey = "hand-bullets", Batches = 3, Progress = 1 });
            st.Hand.Reserved[ItemId.Steel] = 6;
            st.Hand.Reserved[ItemId.Copper] = 3;
            Assert.IsTrue(Apply(ctx, st, new MineCommand(18, 16)).Accepted, "a queued batch does not take the hands");
            Run(ctx, st, 10);
            Assert.IsTrue(st.Engineer.Mining, "and it does not stop the dig half way either");
            st.Hand.Jobs.Clear();
            st.Hand.Reserved = new ItemBag();

            st.Engineer.Down = 0;
            Mining.Stop(st, "");
            var down = Apply(ctx, st, new MineCommand(18, 16));
            Assert.IsFalse(down.Accepted);
            Assert.AreEqual(Mining.DownText, down.Problem);
        }

        [Test]
        public void AFullBackpackRefusesTheDigAndStopsTheHands()
        {
            var ctx = Context();
            var st = State(ctx);
            var stack = ctx.Data.StackSize(ItemKey.Of(ItemId.Coal));
            st.Engineer.Inv[ItemId.Coal] = stack * Pockets.Cap(ctx.Data);

            var r = Apply(ctx, st, new MineCommand(18, 16));
            Assert.IsFalse(r.Accepted);
            Assert.AreEqual(Mining.FullText, r.Problem);

            st.Engineer.Inv[ItemId.Coal] = 0;
            Assert.IsTrue(Apply(ctx, st, new MineCommand(18, 16)).Accepted);
            st.Engineer.Inv[ItemId.Coal] = stack * Pockets.Cap(ctx.Data);
            Run(ctx, st, 40);
            Assert.IsFalse(st.Engineer.Mining);
            Assert.IsTrue(st.Engineer.MineFull, "the presenter can say why");
            Assert.AreEqual(1, Stops(st, out var reason));
            Assert.AreEqual(Mining.FullText, reason);
            Assert.AreEqual(0, st.Stats.Mined, "the tile keeps its unit when the Backpack will not take it");
        }

        [Test]
        public void ATileIsExhaustedAndThenYieldsNothing()
        {
            var ctx = Context();
            var st = State(ctx);
            var w = ctx.Geometry.Width;
            var tiles = w * ctx.Geometry.Height;
            st.Ground.SetDug(16 * w + 18, tiles, 2);

            Assert.IsTrue(Apply(ctx, st, new MineCommand(18, 16)).Accepted);
            Run(ctx, st, 80);
            Assert.AreEqual(2, st.Stats.Mined);
            Assert.AreEqual(0, Ground.UnitsAt(ctx, st, 18, 16), 1e-9, "the tile is cleared");
            Assert.AreEqual(TileClass.Ground, Ground.TileAt(ctx, st, 18, 16), "and reads back as plain ground");

            var cleared = 0;
            for (var i = 0; i < st.Events.Count; i++)
                if (st.Events[i] is MinedEvent m && m.Cleared) cleared++;
            Assert.AreEqual(1, cleared, "the last unit announces the clear");

            Run(ctx, st, 1);
            Assert.IsFalse(st.Engineer.Mining);
            Assert.AreEqual(1, Stops(st, out var reason));
            Assert.AreEqual(Mining.EmptyText, reason);

            var again = Apply(ctx, st, new MineCommand(18, 16));
            Assert.IsFalse(again.Accepted);
            Assert.AreEqual(Mining.EmptyText, again.Problem);
            Assert.AreEqual("", Fixture.Off(ctx, st));
        }

        [Test]
        public void RepeatingTheSameTileKeepsProgressAndANewTileRestartsIt()
        {
            var ctx = Context();
            var st = State(ctx);
            Assert.IsTrue(Apply(ctx, st, new MineCommand(18, 16)).Accepted);
            Run(ctx, st, 20);
            Assert.IsTrue(Apply(ctx, st, new MineCommand(18, 16)).Accepted, "the presenter re-sends while the button is held");
            Assert.AreEqual(0.5, st.Engineer.MineProg, 1e-9, "progress survives the repeat");

            Assert.IsTrue(Apply(ctx, st, new MineCommand(19, 16)).Accepted);
            Assert.AreEqual(0, st.Engineer.MineProg, 1e-9, "a new tile starts over");
            Assert.AreEqual(19, st.Engineer.MineX);

            Assert.IsTrue(Apply(ctx, st, new StopMiningCommand()).Accepted);
            Assert.IsFalse(st.Engineer.Mining);
            Run(ctx, st, 40);
            Assert.AreEqual(0, st.Stats.Mined, "stopped hands dig nothing");
        }
    }
}
