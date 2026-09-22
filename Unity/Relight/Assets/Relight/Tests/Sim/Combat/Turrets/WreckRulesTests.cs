using System.Collections.Generic;
using NUnit.Framework;
using Relight.Sim.Tests.Production;

namespace Relight.Sim.Tests.Combat
{
    /// <summary>
    /// REL-115 (E-24): what a raid may break, and what a broken machine does with what is in it. The owner's
    /// answers, U-D-69:
    ///
    /// * (f) "A destroyed chest or machine keeps its items in the wreck; repairing it brings them back."
    /// * (g) "A destroyed machine leaves a repairable wreck, as turrets already do."
    /// * (h) "Raiders aim for the core and the turrets, and break a machine only when it blocks their path."
    ///
    /// and U-D-68 (d), "All buildings the player places should have hit points", which now includes the belts and
    /// poles a body walks over.
    /// </summary>
    public sealed class WreckRulesTests
    {
        // ---------------------------------------------------------------- (f) the wreck keeps its items

        /// <summary>A fuelled circuit, a chest of steel, an arm out of it, a belt out of it and a chest beyond the arm.</summary>
        private static (SimContext Ctx, SimState St, Machine Src, Machine Arm, Machine Dst, Machine Belt) Yard(int steel)
        {
            var ctx = FlowFixture.Context();
            var st = FlowFixture.State(ctx);
            st.Engineer.Pos = new Vec2(16.5, 17.5);                     // in reach of both chests
            ProductionFixture.Grid(ctx, st, 20, 18, 18, 14, 50);
            var src = FlowFixture.Add(ctx, st, "chest", 14, 17);        // (14..15, 17..18)
            var arm = FlowFixture.Add(ctx, st, "inserter", 16, 17, Dir.E);
            var dst = FlowFixture.Add(ctx, st, "chest", 17, 17);        // (17..18, 17..18)
            var belt = FlowFixture.Add(ctx, st, "belt", 13, 17, Dir.W); // loads straight out of the chest behind it
            src.Inv.Add(ItemId.Steel, steel);
            FlowFixture.Seal(ctx, st);
            return (ctx, st, src, arm, dst, belt);
        }

        private static CommandResult Hand(SimContext ctx, SimState st, int id, ItemId item, int n, bool put)
        {
            Assert.That(new MachineTransferHandler().TryApply(ctx, st, new MachineTransferCommand(id, item, n, put), out var r),
                Is.True);
            return r;
        }

        /// <summary>
        /// The whole of (f) on one chest. Broken, it keeps every plate: the arm does not reach into it, the belt
        /// behind it loads nothing, and the hand is refused with the fix in the refusal. Repaired, the same plates
        /// flow again, and none was made or lost on the way.
        /// </summary>
        [Test]
        public void AWreckedChestKeepsItsItemsFromTheArmTheBeltAndTheHandUntilItIsRepaired()
        {
            var (ctx, st, src, arm, dst, belt) = Yard(10);
            var max = TurretRules.MaxHp(ctx.Data, src);
            Assert.That(max, Is.GreaterThan(0));
            Assert.That(TurretRules.Damage(ctx, st, src, max), Is.True);
            Assert.That(TurretRules.Wrecked(ctx.Data, st, src), Is.True);

            FlowFixture.Run(ctx, st, 200);                              // 10 s
            Assert.That(src.Inv[ItemId.Steel], Is.EqualTo(10), "every plate is still in the wreck");
            Assert.That(FlowQueries.Holding(st, arm.Id), Is.Null, "the arm did not reach into it");
            Assert.That(dst.Inv[ItemId.Steel], Is.Zero);
            Assert.That(FlowQueries.HeldCount(st), Is.Zero, "the belt behind it loaded nothing");

            var take = Hand(ctx, st, src.Id, ItemId.Steel, 1, false);
            Assert.That(take.Problem, Is.EqualTo(MachineInventory.WreckedText(ctx.Data, src)));
            Assert.That(take.Problem, Is.EqualTo("Repair the Supply chest to reach what is inside"));
            st.Engineer.Inv[ItemId.Copper] = 5;
            st.Ledger = Ledger.Open(st, ctx.Data);
            Assert.That(Hand(ctx, st, src.Id, ItemId.Copper, 1, true).Problem,
                Is.EqualTo(MachineInventory.WreckedText(ctx.Data, src)), "nor does anything go in");
            Assert.That(src.Inv[ItemId.Steel], Is.EqualTo(10));
            Assert.That(st.Engineer.Inv[ItemId.Steel], Is.Zero);

            TurretRules.TurretRepairHook(ctx, st, src.Id, max);
            Assert.That(TurretRules.Wrecked(ctx.Data, st, src), Is.False);
            FlowFixture.Run(ctx, st, 400);                              // 20 s
            Assert.That(dst.Inv[ItemId.Steel], Is.GreaterThan(0), "repaired, the arm takes from it again");
            Assert.That(FlowQueries.HeldCount(st), Is.GreaterThan(0), "and so does the belt");
            Assert.That(FlowFixture.TotalItems(st, ItemId.Steel), Is.EqualTo(10), "nothing made, nothing lost");
            Assert.That(FlowFixture.Off(ctx, st), Is.Empty);
        }

        /// <summary>
        /// The other side of the arm: it does not lift a plate it could only hand to a wreck, so it never stands
        /// holding something it cannot put down, and the plate stays where it was.
        /// </summary>
        [Test]
        public void AnArmDoesNotFeedAWreckedChest()
        {
            var (ctx, st, src, arm, dst, _) = Yard(10);
            Assert.That(TurretRules.Damage(ctx, st, dst, TurretRules.MaxHp(ctx.Data, dst)), Is.True);

            FlowFixture.Run(ctx, st, 200);
            Assert.That(FlowQueries.Holding(st, arm.Id), Is.Null, "no plate lifted for a wreck");
            Assert.That(dst.Inv[ItemId.Steel], Is.Zero);
            Assert.That(src.Inv[ItemId.Steel] + FlowQueries.HeldCount(st), Is.EqualTo(10),
                "every plate is in the chest or on the working belt");
            Assert.That(FlowFixture.Off(ctx, st), Is.Empty);
        }

        /// <summary>
        /// A belt has hit points now (U-D-68 (d)), so it can be a wreck — only by something other than a raid
        /// walking over it, which never bites it (U-D-69 (h)). A wrecked belt holds what it carries and stops the
        /// line at the break; repaired, the line runs to the end with nothing lost.
        /// </summary>
        [Test]
        public void AWreckedBeltHoldsWhatItCarriesAndTheLineRunsAgainWhenItIsRepaired()
        {
            var ctx = FlowFixture.Context();
            var st = FlowFixture.State(ctx);
            var src = FlowFixture.Add(ctx, st, "chest", 2, 6);
            var belts = new List<Machine>();
            for (var i = 0; i < 4; i++) belts.Add(FlowFixture.Add(ctx, st, "belt", 4 + i, 6, Dir.E));
            var dst = FlowFixture.Add(ctx, st, "chest", 8, 6);
            src.Inv.Add(ItemId.Steel, 12);
            FlowFixture.Seal(ctx, st);

            var broken = belts[2];
            var max = TurretRules.MaxHp(ctx.Data, broken);
            Assert.That(max, Is.GreaterThan(0), "every building the player places has hit points");
            Assert.That(TurretRules.BlocksRaiders(ctx.Data, broken), Is.False, "but a raid walks over a belt");

            FlowFixture.Run(ctx, st, 30);
            Assert.That(TurretRules.Damage(ctx, st, broken, max), Is.True);
            var held = st.Flow.Find(broken.Id)?.Items.Count ?? 0;
            FlowFixture.Run(ctx, st, 200);
            var arrived = dst.Inv[ItemId.Steel];
            FlowFixture.Run(ctx, st, 200);
            Assert.That(st.Flow.Find(broken.Id)?.Items.Count ?? 0, Is.EqualTo(held), "the wreck keeps what it carries");
            Assert.That(dst.Inv[ItemId.Steel], Is.EqualTo(arrived), "nothing crosses the break");
            Assert.That(FlowFixture.TotalItems(st, ItemId.Steel), Is.EqualTo(12));

            TurretRules.TurretRepairHook(ctx, st, broken.Id, max);
            FlowFixture.Run(ctx, st, 600);
            Assert.That(dst.Inv[ItemId.Steel], Is.EqualTo(12), "repaired, every plate reaches the end");
            Assert.That(FlowFixture.Off(ctx, st), Is.Empty);
        }

        // ---------------------------------------------------------------- (h) break only what is in the way

        private static SimState RaidState(SimContext ctx)
        {
            var st = RaidFixture.State(ctx);
            new HomeCoreInitializer().Init(ctx, st);
            st.Director.NextMinor = 1e9;
            st.Director.NextStart = 1e9;
            st.Engineer.Pos = new Vec2(5.5, 5.5);                       // far away: the raid is about the core
            return st;
        }

        /// <summary>The fixture map with a map id, so <see cref="DirectorRules.HostileOpen"/> takes the city branch.</summary>
        private static SimContext CityContext() =>
            new SimContext(ReferenceData.Create(), RaidFixture.Map(), null, null,
                new WorldSites(new List<SiteRecord>
                {
                    new SiteRecord("home", "Home Court", SiteKind.Core, RaidFixture.CoreX, RaidFixture.CoreY,
                        RaidFixture.CoreSize, RaidFixture.CoreSize, "", 0),
                }), "rel115-test");

        private static Enemy Breaker(SimContext ctx, SimState st, double x, double y)
        {
            var e = RaidFixture.Body(st, "breaker", x, y);
            Assert.That(ctx.Data.TryEnemy("breaker", out var def), Is.True);
            e.Hp = def.Hp;
            return e;
        }

        /// <summary>
        /// GP-W5 let a Breaker stop for any machine within reach. U-D-69 (h) ends that: it walks past a chest beside
        /// its path and over the belts across it, and reaches the core with all three whole.
        /// </summary>
        [Test]
        public void ABreakerWalksPastAChestBesideItsPathAndOverABeltToReachTheCore()
        {
            var ctx = RaidFixture.Context();
            var st = RaidState(ctx);
            var y = RaidFixture.CoreY + 4;                              // the row that runs straight into the core
            var chest = RaidFixture.Add(ctx, st, "chest", 64, y + 1);    // centre 1.5 tiles off the row
            var belt = RaidFixture.Add(ctx, st, "belt", 68, y, Dir.N);   // across the row
            var pole = RaidFixture.Add(ctx, st, "pole", 70, y);
            Breaker(ctx, st, 56.5, y + .5);
            var core = st.Home.Hp;

            for (var i = 0; i < 1200 && st.Home.Hp >= core; i++) RaidFixture.Run(ctx, st, 1);
            Assert.That(st.Home.Hp, Is.LessThan(core), "it reached the core");
            Assert.That(TurretRules.Hp(ctx.Data, st, chest), Is.EqualTo(TurretRules.MaxHp(ctx.Data, chest)),
                "the chest beside the path was left alone");
            Assert.That(TurretRules.Hp(ctx.Data, st, belt), Is.EqualTo(TurretRules.MaxHp(ctx.Data, belt)),
                "the belt it walked over was not bitten");
            Assert.That(TurretRules.Hp(ctx.Data, st, pole), Is.EqualTo(TurretRules.MaxHp(ctx.Data, pole)));
        }

        /// <summary>A closed ring of 2×2 chests round the core, two tiles out.</summary>
        private static List<Machine> ChestRing(SimContext ctx, SimState st)
        {
            var ring = new List<Machine>();
            int lo = RaidFixture.CoreX - 4, hi = RaidFixture.CoreX + RaidFixture.CoreSize + 2;   // 72 and 86
            for (var x = lo; x <= hi; x += 2)
            {
                ring.Add(RaidFixture.Add(ctx, st, "chest", x, lo));
                ring.Add(RaidFixture.Add(ctx, st, "chest", x, hi));
            }
            for (var y = lo + 2; y < hi; y += 2)
            {
                ring.Add(RaidFixture.Add(ctx, st, "chest", lo, y));
                ring.Add(RaidFixture.Add(ctx, st, "chest", hi, y));
            }
            return ring;
        }

        /// <summary>
        /// The hole REL-115 was opened for, on both kinds of map: a ring of supply chests is not an invulnerable
        /// wall. The raid breaks the chest in its way — a chest BLOCKS its path, which is exactly when (h) lets it
        /// break a machine — and reaches the core. The chest it broke still holds its plates.
        /// </summary>
        [TestCase(false)]
        [TestCase(true)]
        public void ARaidBreaksThroughARingOfChestsAndTheBrokenChestKeepsItsPlates(bool city)
        {
            var ctx = city ? CityContext() : RaidFixture.Context();
            var st = RaidState(ctx);
            var ring = ChestRing(ctx, st);
            foreach (var c in ring) c.Inv.Add(ItemId.Steel, 5);
            var body = RaidFixture.Body(st, "skitter", 50.5, RaidFixture.CoreY + 4.5);
            body.Hp = 1e9;
            var core = st.Home.Hp;

            for (var i = 0; i < 2400 && st.Home.Hp >= core; i++) RaidFixture.Run(ctx, st, 1);
            Assert.That(st.Home.Hp, Is.LessThan(core), "the ring did not keep the raid off the core");
            var broken = ring.FindAll(c => TurretRules.Wrecked(ctx.Data, st, c));
            Assert.That(broken, Is.Not.Empty, "it got there by breaking a chest");
            foreach (var c in broken) Assert.That(c.Inv[ItemId.Steel], Is.EqualTo(5), "and the wreck kept its plates");
        }
    }
}
