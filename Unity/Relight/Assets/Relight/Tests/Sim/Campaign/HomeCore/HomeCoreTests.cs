using System.Collections.Generic;
using NUnit.Framework;

namespace Relight.Sim.Tests.Campaign
{
    /// <summary>
    /// C-05: the Home core's HP, its repair and its recommission, against reference campaignDefence.ts
    /// (<c>DEFENCE</c>, <c>damageCore</c>, <c>repairCheck</c>, <c>startRepair</c>, <c>tickRepair</c>).
    /// </summary>
    public sealed class HomeCoreTests
    {
        private const double Dt = 1.0 / 20;

        private static SimContext Context(WorldSites sites = null) =>
            new SimContext(ReferenceData.Create(), SyntheticMap.Create(), sites: sites);

        /// <summary>A state with the core placed and the engineer standing on it, holding the given stock.</summary>
        private static SimState State(SimContext ctx, int steel = 0, int copper = 0)
        {
            var st = new SimState();
            st.Engineer.Pos = ctx.Geometry.Spawn;
            st.Engineer.Hp = ctx.Data.Engineer.MaxHp;
            new HomeCoreInitializer().Init(ctx, st);
            if (steel > 0) st.Engineer.Inv[ItemId.Steel] = steel;
            if (copper > 0) st.Engineer.Inv[ItemId.Copper] = copper;
            st.Ledger = Ledger.Open(st, ctx.Data);
            return st;
        }

        private static void Run(SimContext ctx, SimState st, int ticks)
        {
            var phase = new HomeCorePhase();
            for (var i = 0; i < ticks; i++) { phase.Tick(ctx, st, Dt); st.Tick++; st.T += Dt; }
        }

        private static CommandResult Apply(SimContext ctx, SimState st, Command c)
        {
            Assert.That(new HomeCoreHandler().TryApply(ctx, st, c, out var r), Is.True, "the handler owns " + c);
            return r;
        }

        private static string Off(SimContext ctx, SimState st) =>
            string.Join(" | ", Ledger.Conservation(st, ctx.Data).Problems);

        // ---------------------------------------------------------------- placement

        /// <summary>
        /// The brief's fallback: with no <c>ctx.Sites.Core</c> (the synthetic map, and every pre-C-01 test) the core
        /// is a 3x3 at the engineer's spawn tile, and <see cref="HomeQueries.CoreIsFallback"/> says so.
        /// </summary>
        [Test]
        public void WithNoCoreSiteTheCoreIsAThreeByThreeAtTheSpawnTile()
        {
            var ctx = Context();
            var st = State(ctx);
            Assert.That(HomeQueries.CoreIsFallback(st), Is.True);
            Assert.That(HomeQueries.CoreRect(st), Is.EqualTo((5, 5, 3, 3)), "centred on spawn tile (6, 6)");
            Assert.That(HomeQueries.CoreHp(st), Is.EqualTo(300), "DEFENCE.coreHp");
            Assert.That(HomeQueries.CoreOperational(st), Is.True);
            Assert.That(HomeQueries.CoreDisabledAt(st), Is.EqualTo(-1), "it has never been knocked out");
        }

        /// <summary>The real region's core is the authored <c>home-workshop</c> rect, not the fallback.</summary>
        [Test]
        public void WithACoreSiteTheCoreIsTheAuthoredRect()
        {
            var ctx = Context(new WorldSites(new[]
            {
                new SiteRecord("home-workshop", "Home workshop", SiteKind.Core, 22, 256, 10, 14),
            }));
            var st = State(ctx);
            Assert.That(HomeQueries.CoreIsFallback(st), Is.False);
            Assert.That(HomeQueries.CoreRect(st), Is.EqualTo((22, 256, 10, 14)));
        }

        /// <summary>
        /// A Phase B save upgraded to v3 arrives with <c>new HomeState()</c> — nothing placed. The phase must not
        /// throw and must place the core on its first tick (brief: "never throw at tick time on a missing site").
        /// </summary>
        [Test]
        public void ADefaultHomeStateIsPlacedOnTheFirstTickInsteadOfThrowing()
        {
            var ctx = Context();
            var st = new SimState { Engineer = { Pos = ctx.Geometry.Spawn } };
            Assert.That(st.Home.Placed, Is.False);
            Assert.That(HomeQueries.CoreOperational(st), Is.False, "nothing has happened yet");

            Assert.DoesNotThrow(() => Run(ctx, st, 1));
            Assert.That(st.Home.Placed, Is.True);
            Assert.That(HomeQueries.CoreHp(st), Is.EqualTo(300));
        }

        // ---------------------------------------------------------------- damage

        /// <summary>Reference <c>damageCore</c>: hp floors at 0, and 0 is disabled.</summary>
        [Test]
        public void DamageTakesTheCoreDownAndZeroDisablesIt()
        {
            var ctx = Context();
            var st = State(ctx);
            st.T = 12.5;

            HomeCore.Damage(st, 100);
            Assert.That(HomeQueries.CoreHp(st), Is.EqualTo(200));
            Assert.That(HomeQueries.CoreOperational(st), Is.True);
            Assert.That(Events<CoreDamagedEvent>(st), Is.EqualTo(1));
            Assert.That(Events<CoreDisabledEvent>(st), Is.Zero);

            HomeCore.Damage(st, 500);
            Assert.That(HomeQueries.CoreHp(st), Is.Zero, "hp floors at 0");
            Assert.That(HomeQueries.CoreOperational(st), Is.False, "a disabled core stops counting as commissioned");
            Assert.That(HomeQueries.CoreDisabledAt(st), Is.EqualTo(12.5));
            Assert.That(Events<CoreDisabledEvent>(st), Is.EqualTo(1));

            HomeCore.Damage(st, 50);
            Assert.That(Events<CoreDamagedEvent>(st), Is.EqualTo(2), "a core already at 0 takes no further damage");
        }

        // ---------------------------------------------------------------- repair

        /// <summary>
        /// Reference <c>startRepair</c> + <c>tickRepair</c>: 2 steel and 1 copper leave the pockets at the start,
        /// the work takes 4 s, and it restores DEFENCE.repairHp = 40.
        /// </summary>
        [Test]
        public void ARepairChargesTwoSteelAndOneCopperTicksFourSecondsAndRestoresFortyHp()
        {
            var ctx = Context();
            var st = State(ctx, steel: 10, copper: 10);
            HomeCore.Damage(st, 100);

            var card = HomeQueries.RepairCard(st, ctx.Data);
            Assert.That(card.Steel, Is.EqualTo(2));
            Assert.That(card.Copper, Is.EqualTo(1));
            Assert.That(card.Seconds, Is.EqualTo(4));
            Assert.That(card.Recommission, Is.False);
            Assert.That(card.CanAfford, Is.True);
            Assert.That(card.InProgress, Is.False);

            Assert.That(Apply(ctx, st, new RepairCommand(RepairKinds.Core, -1)).Accepted, Is.True);
            Assert.That(st.Engineer.Inv[ItemId.Steel], Is.EqualTo(8), "charged at the start (U-D-05)");
            Assert.That(st.Engineer.Inv[ItemId.Copper], Is.EqualTo(9));
            Assert.That(st.Stats.SpentSteel, Is.EqualTo(2), "and recorded as a ledger sink");
            Assert.That(Home.RepairLocked(st), Is.True, "a repair pins the engineer like a craft batch");

            Run(ctx, st, 79);
            Assert.That(HomeQueries.CoreHp(st), Is.EqualTo(200), "3.95 s is not 4 s");
            Assert.That(HomeQueries.RepairCard(st, ctx.Data).RemainingS, Is.EqualTo(0.05).Within(1e-9));

            Run(ctx, st, 1);
            Assert.That(HomeQueries.CoreHp(st), Is.EqualTo(240), "+ DEFENCE.repairHp");
            Assert.That(Home.RepairLocked(st), Is.False);
            Assert.That(Events<CoreRepairedEvent>(st), Is.EqualTo(1));
            Assert.That(Off(ctx, st), Is.Empty, "the spend is explained");
        }

        /// <summary>Reference <c>tickRepair</c> line 144's <c>Math.min</c>: a repair never overshoots the maximum.</summary>
        [Test]
        public void ARepairIsCappedAtTheMaximum()
        {
            var ctx = Context();
            var st = State(ctx, steel: 10, copper: 10);
            HomeCore.Damage(st, 10);
            Assert.That(Apply(ctx, st, new RepairCommand(RepairKinds.Core, -1)).Accepted, Is.True);
            Run(ctx, st, 100);
            Assert.That(HomeQueries.CoreHp(st), Is.EqualTo(300), "290 + 40 clamps to coreHp");
        }

        /// <summary>Reference <c>repairCost</c>: a disabled core needs the recommission kit, 10 steel + 5 copper, 12 s.</summary>
        [Test]
        public void ADisabledCoreNeedsTheRecommissionKit()
        {
            var ctx = Context();
            var st = State(ctx, steel: 10, copper: 5);
            HomeCore.Damage(st, 300);

            var card = HomeQueries.RepairCard(st, ctx.Data);
            Assert.That(card.Recommission, Is.True);
            Assert.That(card.Steel, Is.EqualTo(10));
            Assert.That(card.Copper, Is.EqualTo(5));
            Assert.That(card.Seconds, Is.EqualTo(12));

            Assert.That(Apply(ctx, st, new RepairCommand(RepairKinds.Core, -1)).Accepted, Is.True);
            Assert.That(st.Engineer.Inv[ItemId.Steel], Is.Zero);
            Assert.That(st.Engineer.Inv[ItemId.Copper], Is.Zero);

            Run(ctx, st, 239);
            Assert.That(HomeQueries.CoreOperational(st), Is.False, "11.95 s is not 12 s");
            Run(ctx, st, 1);
            Assert.That(HomeQueries.CoreHp(st), Is.EqualTo(300), "a recommission restores the whole core");
            Assert.That(HomeQueries.CoreOperational(st), Is.True);
            Assert.That(HomeQueries.CoreDisabledAt(st), Is.EqualTo(-1), "and it counts as never knocked out again");
            Assert.That(Off(ctx, st), Is.Empty);
        }

        /// <summary>
        /// Reference <c>tickRepair</c> line 143: a patch repair whose core is knocked out mid-work is void, and the
        /// player is told a full recovery kit is required. The payment is not refunded — the reference drops it.
        /// </summary>
        [Test]
        public void ACoreKnockedOutDuringAPatchRepairVoidsTheWork()
        {
            var ctx = Context();
            var st = State(ctx, steel: 10, copper: 10);
            HomeCore.Damage(st, 100);
            Assert.That(Apply(ctx, st, new RepairCommand(RepairKinds.Core, -1)).Accepted, Is.True);

            Run(ctx, st, 40);
            HomeCore.Damage(st, 1000);
            Run(ctx, st, 40);

            Assert.That(HomeQueries.CoreHp(st), Is.Zero, "the patch did not bring it back");
            Assert.That(st.Home.RepairKind, Is.EqualTo(RepairKinds.None));
            Assert.That(Events<CoreRepairedEvent>(st), Is.Zero);
            Assert.That(Text<CoreRepairAbortedEvent>(st), Is.EqualTo("Core knocked out during repair; a full recovery kit is required."));
            Assert.That(Off(ctx, st), Is.Empty, "the spend is still explained");
        }

        /// <summary>Reference <c>tickRepair</c> line 138: paid progress pauses out of reach, with no extra charge on resuming.</summary>
        [Test]
        public void ARepairPausesOutOfReachAndResumesWithoutChargingAgain()
        {
            var ctx = Context();
            var st = State(ctx, steel: 10, copper: 10);
            HomeCore.Damage(st, 100);
            Assert.That(Apply(ctx, st, new RepairCommand(RepairKinds.Core, -1)).Accepted, Is.True);

            Run(ctx, st, 20);
            var left = st.Home.RepairRemaining;
            st.Engineer.Pos = new Vec2(60.5, 44.5);                 // well outside the 8-tile reach
            Run(ctx, st, 200);
            Assert.That(st.Home.RepairRemaining, Is.EqualTo(left), "the timer stopped");
            Assert.That(st.Engineer.Inv[ItemId.Steel], Is.EqualTo(8), "and nothing more was charged");

            st.Engineer.Pos = ctx.Geometry.Spawn;
            Run(ctx, st, 100);
            Assert.That(HomeQueries.CoreHp(st), Is.EqualTo(240), "it finished where it left off");
        }

        // ---------------------------------------------------------------- refusals and cancel

        [Test]
        public void TheRefusalsAreTheBriefsExactTexts()
        {
            var ctx = Context();
            var st = State(ctx, steel: 1, copper: 0);

            Assert.That(Apply(ctx, st, new RepairCommand(RepairKinds.Core, -1)).Problem,
                Is.EqualTo("the core is already at full health"));

            HomeCore.Damage(st, 100);
            Assert.That(Apply(ctx, st, new RepairCommand(RepairKinds.Core, -1)).Problem,
                Is.EqualTo("repairing needs 2 steel and 1 copper"), "priced from the tuning, so a recommission reads 10/5");

            st.Engineer.Inv[ItemId.Steel] = 20;
            st.Engineer.Inv[ItemId.Copper] = 20;
            var far = new Vec2(60.5, 44.5);
            var home = st.Engineer.Pos;
            st.Engineer.Pos = far;
            Assert.That(Apply(ctx, st, new RepairCommand(RepairKinds.Core, -1)).Problem, Is.EqualTo("walk closer to repair"));

            st.Engineer.Pos = home;
            Assert.That(Apply(ctx, st, new RepairCommand(RepairKinds.Core, -1)).Accepted, Is.True);
            Assert.That(Apply(ctx, st, new RepairCommand(RepairKinds.Core, -1)).Problem, Is.EqualTo("already repairing"));

            HomeCore.Damage(st, 1000);
            var st2 = State(ctx, steel: 20, copper: 20);
            HomeCore.Damage(st2, 1000);
            Assert.That(HomeQueries.RepairCard(st2, ctx.Data).Steel, Is.EqualTo(10), "a recommission is priced once, here");
        }

        /// <summary>A repair aimed at a machine that does not exist is refused honestly (the turret hook itself is covered by TurretRepairTests).</summary>
        [Test]
        public void AMachineRepairOnAMissingMachineIsRefused()
        {
            var ctx = Context();
            var st = State(ctx, steel: 20, copper: 20);
            Assert.That(Apply(ctx, st, new RepairCommand(RepairKinds.Machine, 7)).Problem, Is.EqualTo("nothing damaged here"));
        }

        /// <summary>The port's cancel (the reference has none): the charge comes back and the sink is unwound.</summary>
        [Test]
        public void CancellingARepairRefundsWhatWasCharged()
        {
            var ctx = Context();
            var st = State(ctx, steel: 10, copper: 10);
            HomeCore.Damage(st, 100);
            Assert.That(Apply(ctx, st, new RepairCommand(RepairKinds.Core, -1)).Accepted, Is.True);
            Run(ctx, st, 20);

            Assert.That(Apply(ctx, st, new CancelRepairCommand()).Accepted, Is.True);
            Assert.That(st.Engineer.Inv[ItemId.Steel], Is.EqualTo(10), "the steel came back");
            Assert.That(st.Engineer.Inv[ItemId.Copper], Is.EqualTo(10));
            Assert.That(st.Stats.SpentSteel, Is.Zero, "and the ledger sink was unwound with it");
            Assert.That(st.Stats.SpentCopper, Is.Zero);
            Assert.That(Home.RepairLocked(st), Is.False);
            Assert.That(HomeQueries.CoreHp(st), Is.EqualTo(200), "no work was banked");
            Assert.That(Off(ctx, st), Is.Empty);

            Assert.That(Apply(ctx, st, new CancelRepairCommand()).Problem, Is.EqualTo("no repair is in progress"));
        }

        // ---------------------------------------------------------------- persistence

        [Test]
        public void TheCoreAndARepairInProgressRoundTripThroughASave()
        {
            var ctx = Context();
            var st = State(ctx, steel: 10, copper: 10);
            HomeCore.Damage(st, 100);
            Assert.That(Apply(ctx, st, new RepairCommand(RepairKinds.Core, -1)).Accepted, Is.True);
            Run(ctx, st, 30);

            const string at = "2026-09-14T00:00:00.0000000Z";     // the header stamps the wall clock otherwise
            var text = SaveSerializer.WriteText(st, ctx.Data, at);
            var loaded = SaveSerializer.ReadText(text, ctx.Data);
            Assert.That(loaded.Ok, Is.True, loaded.Reason);
            Assert.That(loaded.State.Home.Hp, Is.EqualTo(st.Home.Hp));
            Assert.That(loaded.State.Home.RepairRemaining, Is.EqualTo(st.Home.RepairRemaining));
            Assert.That(loaded.State.Home.RepairKind, Is.EqualTo(RepairKinds.Core));
            Assert.That(SaveSerializer.WriteText(loaded.State, ctx.Data, at), Is.EqualTo(text), "byte for byte");

            Run(ctx, st, 60);
            Run(ctx, loaded.State, 60);
            Assert.That(loaded.State.Home.Hp, Is.EqualTo(st.Home.Hp), "and both copies finish the repair the same way");
        }

        // ---------------------------------------------------------------- the C-08 seam

        /// <summary>
        /// A raid body walking onto the core damages it through <c>EnemyCoreHook</c>: reference campaignDefence.ts
        /// calls <c>damageCore</c> directly, and the port routes C-08's four call sites
        /// (EnemyPhase.cs:104 the body standing on the footprint, :238 <c>ApplyHit</c> with no structure,
        /// :287/:294 the spit landing) through the partial method this folder implements.
        /// </summary>
        [Test]
        public void ARaidBodysContactDamageReachesTheCoreThroughTheSeam()
        {
            var ctx = Context();
            var st = State(ctx);
            var max = ctx.Data.Defence.CoreHp;
            Assert.That(st.Home.Hp, Is.EqualTo(max));

            // Exactly EnemyPhase.cs:104's call: one tile tick of a body standing on the core.
            var bite = ctx.Data.Raids.StructureDps * Dt;
            EnemyCoreHook.Damage(ctx, st, bite);
            Assert.That(st.Home.Hp, Is.EqualTo(max - bite).Within(1e-9), "the seam reaches HomeCore.Damage");
            Assert.That(Events<CoreDamagedEvent>(st), Is.EqualTo(1), "and raises C-05's event, not the enemy's");

            EnemyCoreHook.Damage(ctx, st, 0);
            Assert.That(st.Home.Hp, Is.EqualTo(max - bite).Within(1e-9), "a zero-damage tick changes nothing");
        }

        /// <summary>
        /// The other half of the seam: the raid destination is the core this worker placed, and once the core is
        /// down it stops being a thing to walk at (the declaring side then falls back to <c>ctx.Sites.Core</c>).
        /// </summary>
        [Test]
        public void TheRaidDestinationIsTheLiveCoreFootprint()
        {
            var ctx = Context();
            var st = State(ctx);
            var (cx, cy, cw, ch) = HomeQueries.CoreRect(st);

            Assert.That(EnemyCoreHook.Rect(ctx, st, out var x, out var y, out var w, out var h), Is.True);
            Assert.That((x, y, w, h), Is.EqualTo((cx, cy, cw, ch)), "the fall-back 3x3 at the spawn tile");

            HomeCore.Damage(st, ctx.Data.Defence.CoreHp);
            Assert.That(HomeQueries.CoreOperational(st), Is.False);
            // No authored site on the synthetic map, so the declaring side's ctx.Sites.Core fall-back finds nothing.
            Assert.That(EnemyCoreHook.Rect(ctx, st, out _, out _, out _, out _), Is.False, "a destroyed core is not a target");
        }

        // ---------------------------------------------------------------- helpers

        private static int Events<T>(SimState st) where T : SimEvent
        {
            var n = 0;
            for (var i = 0; i < st.Events.Count; i++) if (st.Events[i] is T) n++;
            return n;
        }

        private static string Text<T>(SimState st) where T : SimEvent
        {
            for (var i = 0; i < st.Events.Count; i++) if (st.Events[i] is CoreRepairAbortedEvent e) return e.Text;
            return "";
        }
    }
}
