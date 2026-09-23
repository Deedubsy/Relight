using System.Collections.Generic;
using NUnit.Framework;
using Relight.Sim.Tests.Combat;
using Relight.Sim.UI;

namespace Relight.Sim.Tests.UI
{
    /// <summary>
    /// REL-133. The two questions the repair mark asks of the simulation every frame.
    ///
    /// These run the REAL <see cref="RepairCommand"/> and the real <see cref="HomeCorePhase"/> rather than setting
    /// <c>st.Home</c>'s fields by hand, because the whole value of the rule is that it tracks the repair the player
    /// actually started: a test that wrote <c>RepairKind</c> itself would still pass if the command stopped setting
    /// it. Cancelling, finishing and going down all go through their own real paths for the same reason.
    /// </summary>
    public sealed class BuildingConditionTests
    {
        private const int Tx = 20, Ty = 20;
        private const double SpawnX = 72.5, SpawnY = 72.5;   // beside the fixture's core at 76,76 and in reach of it

        private static SimContext Context()
        {
            const int size = RaidFixture.Size;
            var kind = new byte[size * size];
            for (var i = 0; i < kind.Length; i++) kind[i] = (byte)TileClass.Ground;
            return RaidFixture.Context(new ArrayGeometry(size, size, kind, new bool[size * size], new Vec2(SpawnX, SpawnY)));
        }

        private static readonly List<ITickPhase> Phases = new List<ITickPhase> { new EngineerMovementPhase(), new HomeCorePhase() };

        private static void Run(SimContext ctx, SimState st, double seconds) =>
            RaidFixture.Run(ctx, st, (int)(seconds / RaidFixture.Dt) + 1, Phases);

        private static CommandResult Send(SimContext ctx, SimState st, Command c)
        {
            var handlers = new List<ICommandHandler> { new MovementCommandHandler(), new HomeCoreHandler() };
            for (var i = 0; i < handlers.Count; i++)
                if (handlers[i].TryApply(ctx, st, c, out var r)) return r;
            return CommandResult.Refuse("no handler");
        }

        /// <summary>The engineer beside a damaged turret, paid up, with nothing started yet.</summary>
        private static SimState BesideADamagedTurret(SimContext ctx, out Machine turret)
        {
            var st = RaidFixture.State(ctx);
            new HomeCoreInitializer().Init(ctx, st);
            turret = RaidFixture.Turret(ctx, st, Tx, Ty);
            st.Engineer.Pos = new Vec2(Tx - 0.5, Ty - 0.5);
            st.Engineer.Inv[ItemId.Steel] = 10;
            st.Engineer.Inv[ItemId.Copper] = 10;
            st.Ledger = Ledger.Open(st, ctx.Data);
            TurretRules.Damage(ctx, st, turret, 60);
            return st;
        }

        // ---------------------------------------------------------------- which building

        [Test]
        public void WithNothingBeingRepairedNoBuildingIsMarked()
        {
            var ctx = Context();
            var st = BesideADamagedTurret(ctx, out var turret);

            Assert.That(BuildingCondition.RepairingMachine(st, turret.Id), Is.False,
                "a damaged turret nobody has started on must not draw a repair.");
            Assert.That(BuildingCondition.RepairingCore(st), Is.False);
            Assert.That(BuildingCondition.RepairProgress(ctx.Data, st), Is.Zero);
        }

        [Test]
        public void StartingAMachineRepairMarksThatMachineAndOnlyThatMachine()
        {
            var ctx = Context();
            var st = BesideADamagedTurret(ctx, out var turret);
            var other = RaidFixture.Turret(ctx, st, Tx + 3, Ty);

            Assert.That(Send(ctx, st, new RepairCommand(RepairKinds.Machine, turret.Id)).Problem, Is.Empty);

            Assert.That(BuildingCondition.RepairingMachine(st, turret.Id), Is.True);
            Assert.That(BuildingCondition.RepairingMachine(st, other.Id), Is.False,
                "the turret next to it is not being repaired and must stay clean.");
            Assert.That(BuildingCondition.RepairingCore(st), Is.False,
                "a machine repair must not also mark the Home core.");
        }

        [Test]
        public void ARepairOfTheCoreMarksTheCoreAndNoMachine()
        {
            var ctx = Context();
            var st = BesideADamagedTurret(ctx, out var turret);
            st.Engineer.Pos = new Vec2(SpawnX, SpawnY);
            HomeCore.Damage(st, 40);

            Assert.That(Send(ctx, st, new RepairCommand(RepairKinds.Core, -1)).Problem, Is.Empty);

            Assert.That(BuildingCondition.RepairingCore(st), Is.True);
            Assert.That(BuildingCondition.RepairingMachine(st, turret.Id), Is.False);
            // -1 is the core's own RepairId. A machine can never hold it, but the rule is asked about ids that come
            // from a view, so it must not answer true for the sentinel.
            Assert.That(BuildingCondition.RepairingMachine(st, -1), Is.False,
                "the core's repair must not mark a machine whose id happens to be the core sentinel.");
        }

        // ---------------------------------------------------------------- when it stops

        [Test]
        public void CancellingTheRepairUnmarksTheBuildingAtOnce()
        {
            var ctx = Context();
            var st = BesideADamagedTurret(ctx, out var turret);
            Assert.That(Send(ctx, st, new RepairCommand(RepairKinds.Machine, turret.Id)).Problem, Is.Empty);
            Run(ctx, st, 0.5);
            Assert.That(BuildingCondition.RepairingMachine(st, turret.Id), Is.True, "it is running before the cancel");

            Assert.That(Send(ctx, st, new CancelRepairCommand()).Problem, Is.Empty);

            Assert.That(BuildingCondition.RepairingMachine(st, turret.Id), Is.False);
            Assert.That(BuildingCondition.RepairProgress(ctx.Data, st), Is.Zero);
        }

        [Test]
        public void FinishingTheRepairUnmarksTheBuilding()
        {
            var ctx = Context();
            var st = BesideADamagedTurret(ctx, out var turret);
            var before = TurretRules.Hp(ctx.Data, st, turret);
            Assert.That(Send(ctx, st, new RepairCommand(RepairKinds.Machine, turret.Id)).Problem, Is.Empty);

            Run(ctx, st, ctx.Data.Defence.RepairSeconds + 1);

            Assert.That(BuildingCondition.RepairingMachine(st, turret.Id), Is.False,
                "the repair landed, so the mark must go with it.");
            Assert.That(TurretRules.Hp(ctx.Data, st, turret), Is.GreaterThan(before),
                "and it really was a finished repair and not a cancelled one");
        }

        [Test]
        public void GoingDownUnmarksTheBuilding()
        {
            var ctx = Context();
            var st = BesideADamagedTurret(ctx, out var turret);
            Assert.That(Send(ctx, st, new RepairCommand(RepairKinds.Machine, turret.Id)).Problem, Is.Empty);

            st.Engineer.TakeDamage(ctx, st, 100000);

            Assert.That(st.Engineer.IsDown, Is.True);
            Assert.That(BuildingCondition.RepairingMachine(st, turret.Id), Is.False,
                "REL-16 ends the repair when the engineer goes down; the mark must not outlive it.");
        }

        // ---------------------------------------------------------------- how far through

        [Test]
        public void ProgressRunsFromZeroAtTheStartTowardsOneAsItLands()
        {
            var ctx = Context();
            var st = BesideADamagedTurret(ctx, out var turret);
            var total = ctx.Data.Defence.RepairSeconds;
            Assert.That(total, Is.GreaterThan(0), "the fixture's tuning must actually take time");

            Assert.That(Send(ctx, st, new RepairCommand(RepairKinds.Machine, turret.Id)).Problem, Is.Empty);
            Assert.That(BuildingCondition.RepairProgress(ctx.Data, st), Is.Zero.Within(1e-9),
                "nothing has been done the instant it starts.");

            Run(ctx, st, total / 2);
            var half = BuildingCondition.RepairProgress(ctx.Data, st);
            Assert.That(half, Is.EqualTo(0.5).Within(0.05), "half the seconds is half the ring");

            Run(ctx, st, total / 4);
            Assert.That(BuildingCondition.RepairProgress(ctx.Data, st), Is.GreaterThan(half),
                "it must only ever fill forwards.");
        }

        [Test]
        public void ARecommissionIsMeasuredAgainstTheRecommissionTimeNotTheRepairTime()
        {
            var ctx = Context();
            var st = BesideADamagedTurret(ctx, out _);
            st.Engineer.Pos = new Vec2(SpawnX, SpawnY);
            st.Engineer.Inv[ItemId.Steel] = 500;
            st.Engineer.Inv[ItemId.Copper] = 500;
            st.Ledger = Ledger.Open(st, ctx.Data);
            HomeCore.Damage(st, ctx.Data.Defence.CoreHp);
            Assert.That(st.Home.Hp, Is.Zero, "the core is down, so this is a recommission");

            Assert.That(Send(ctx, st, new RepairCommand(RepairKinds.Core, -1)).Problem, Is.Empty);
            Assert.That(st.Home.RepairRecommission, Is.True);

            // A recommission is the long job. Measured against the SHORT total the ring would be full long before
            // the work was, which is the one way a progress mark can lie to the player.
            Run(ctx, st, ctx.Data.Defence.RepairSeconds + 0.5);
            Assert.That(BuildingCondition.RepairProgress(ctx.Data, st), Is.LessThan(1).And.GreaterThan(0),
                "past the ordinary repair time a recommission is still partly done, not finished.");
        }

        [Test]
        public void AnOverlongRemainingTimeIsClampedRatherThanRunBackwards()
        {
            var ctx = Context();
            var st = BesideADamagedTurret(ctx, out var turret);
            Assert.That(Send(ctx, st, new RepairCommand(RepairKinds.Machine, turret.Id)).Problem, Is.Empty);

            // What an older save, or one whose tuning has since been shortened, hands the presenter.
            st.Home.RepairRemaining = ctx.Data.Defence.RepairSeconds * 3;
            Assert.That(BuildingCondition.RepairProgress(ctx.Data, st), Is.Zero,
                "a ring drawn from a negative fraction is a ring drawn the wrong way round.");

            st.Home.RepairRemaining = -5;
            Assert.That(BuildingCondition.RepairProgress(ctx.Data, st), Is.EqualTo(1));
        }

        // ---------------------------------------------------------------- it is a picture

        [Test]
        public void AskingChangesNothingInTheSimulation()
        {
            var ctx = Context();
            var st = BesideADamagedTurret(ctx, out var turret);
            Assert.That(Send(ctx, st, new RepairCommand(RepairKinds.Machine, turret.Id)).Problem, Is.Empty);
            Run(ctx, st, 0.5);

            var rev = st.Rev;
            var remaining = st.Home.RepairRemaining;
            var hp = TurretRules.Hp(ctx.Data, st, turret);
            var units = st.Turrets.Units.Count;
            var events = st.Events.Count;

            for (var i = 0; i < 200; i++)
            {
                BuildingCondition.RepairingMachine(st, turret.Id);
                BuildingCondition.RepairingCore(st);
                BuildingCondition.RepairProgress(ctx.Data, st);
            }

            Assert.That(st.Rev, Is.EqualTo(rev));
            Assert.That(st.Home.RepairRemaining, Is.EqualTo(remaining));
            Assert.That(TurretRules.Hp(ctx.Data, st, turret), Is.EqualTo(hp));
            Assert.That(st.Turrets.Units.Count, Is.EqualTo(units), "and it never creates a damage entry by asking");
            Assert.That(st.Events.Count, Is.EqualTo(events));
        }

        [Test]
        public void ItSurvivesAStateWithNoHomeAndANullData()
        {
            var ctx = Context();
            var st = RaidFixture.State(ctx);
            st.Home = null;

            Assert.That(BuildingCondition.RepairingMachine(st, 1), Is.False);
            Assert.That(BuildingCondition.RepairingCore(st), Is.False);
            Assert.That(BuildingCondition.RepairProgress(ctx.Data, st), Is.Zero);
            Assert.That(BuildingCondition.RepairProgress(null, null), Is.Zero);
        }
    }
}
