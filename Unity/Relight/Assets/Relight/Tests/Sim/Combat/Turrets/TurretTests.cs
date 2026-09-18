using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Relight.Sim.Tests.Combat
{
    /// <summary>
    /// C-04. Every rule pinned here is the reference's, cited per test: threat.ts:310-340 (the campaign turret
    /// loop), U-D-08 (one item is one bullet) and U-D-12 (an unsupplied turret holds fire).
    /// </summary>
    public sealed class TurretTests
    {
        private static readonly List<ITickPhase> Combat =
            new List<ITickPhase> { new PowerPhase(), new TurretPhase(), new EnemyPhase() };

        private static TurretDef Def(SimContext ctx)
        {
            Assert.That(ctx.Data.TryTurret("turret", out var def), Is.True);
            return def;
        }

        // ---------------------------------------------------------------- supply

        /// <summary>
        /// U-D-12: a turret draws 20 kW and holds fire without it. With no generator anywhere the turret tracks
        /// nothing and spends no ammunition, however close the body stands.
        /// </summary>
        [Test]
        public void AnUnpoweredTurretHoldsFire()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            var t = RaidFixture.Add(ctx, st, "turret", 40, 40);
            t.Rounds = 50;
            RaidFixture.Body(st, "skitter", 43.5, 40.5);

            RaidFixture.Run(ctx, st, 100, Combat);

            Assert.That(t.Rounds, Is.EqualTo(50), "no power, no shot");
            Assert.That(RaidFixture.Count<TurretShotEvent>(st), Is.Zero);
        }

        /// <summary>U-D-08 plus threat.ts:331 <c>m.rounds--</c>: every shot costs exactly one carried item.</summary>
        [Test]
        public void EachShotSpendsExactlyOneBullet()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            var t = RaidFixture.Turret(ctx, st, 40, 40);
            var e = RaidFixture.Body(st, "skitter", 43.5, 40.5);
            e.Hp = 1e9;                                   // it must not die and end the test early

            RaidFixture.Run(ctx, st, 200, Combat);

            var shots = RaidFixture.Count<TurretShotEvent>(st);
            Assert.That(shots, Is.GreaterThan(0), "a powered, loaded turret in range fires");
            Assert.That(50 - t.Rounds, Is.EqualTo(shots), "one item, one bullet");
            Assert.That(st.Stats.Fired, Is.EqualTo(shots), "every round is booked to the ledger's sink");
        }

        /// <summary>
        /// threat.ts:332 <c>cool += 1/def.roundsPerS</c>: the campaign turret fires at exactly its catalogue rate.
        /// Ten seconds at 1 round/s is ten shots, allowing one for where the first shot lands in the tick grid.
        /// </summary>
        [Test]
        public void ItFiresAtItsCatalogueRate()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            var t = RaidFixture.Turret(ctx, st, 40, 40);
            RaidFixture.Guard(st, "skitter", 43.5, 40.5);

            RaidFixture.Run(ctx, st, 200, Combat);       // 10 s
            var def = Def(ctx);
            var expected = (int)Math.Round(10 * def.RoundsPerS);
            Assert.That(RaidFixture.Count<TurretShotEvent>(st), Is.InRange(expected, expected + 1));
        }

        /// <summary>The hopper never holds more than the catalogue capacity (<c>TurretDef.Hopper</c> = 50).</summary>
        [Test]
        public void TheHopperStopsAtItsCapacity()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            var t = RaidFixture.Add(ctx, st, "turret", 40, 40);

            var capacity = TurretHopper.Capacity(ctx.Data, t);
            Assert.That(TurretHopper.Give(ctx, st, t.Id, ItemId.Magazine, capacity + 10), Is.EqualTo(capacity),
                "only what fits is taken");
            Assert.That(t.Rounds, Is.EqualTo(capacity));
            Assert.That(TurretHopper.Room(ctx, st, t.Id), Is.Zero);
            Assert.That(TurretHopper.Give(ctx, st, t.Id, ItemId.Magazine, 1), Is.Zero, TurretHopper.FullProblem);
            Assert.That(TurretHopper.Accepts(ctx, st, t.Id, ItemId.Coal), Is.False, "a turret takes only its ammunition");
        }

        // ---------------------------------------------------------------- aiming

        /// <summary>
        /// threat.ts:321 <c>aimTurret</c>: the mount turns by at most <c>turnSpeed*dt</c> and takes the shorter way
        /// round. A turret facing north with a body due west turns anticlockwise (−90°), never 270° the other way.
        /// </summary>
        [Test]
        public void TheMountTakesTheShortWayRoundAtItsTurnSpeed()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            var t = RaidFixture.Turret(ctx, st, 40, 40);
            RaidFixture.Body(st, "skitter", 36.5, 40.5);        // due west of the turret's centre
            var def = Def(ctx);

            RaidFixture.Run(ctx, st, 1, Combat);
            var u = st.Turrets.Find(t.Id);
            Assert.That(u, Is.Not.Null);
            var start = TurretRules.InitialAngle(t);
            var moved = TurretRules.AngleDifference(u.Angle, start);
            Assert.That(Math.Abs(moved), Is.LessThanOrEqualTo(def.TurnSpeedRadPerS * RaidFixture.Dt + 1e-9),
                "one tick of rotation is capped at the catalogue turn speed");
            Assert.That(moved, Is.LessThan(0), "west of a north-facing mount is a shorter turn anticlockwise");
        }

        // ---------------------------------------------------------------- damage and repair

        /// <summary>
        /// A defence at 0 hp is out of action until it is repaired: it stops firing, reports "disabled (0 hp)" and
        /// comes back the moment W-A's repair command hands hit points back through <c>TurretRepairHook</c>.
        /// </summary>
        [Test]
        public void AtZeroHitPointsItIsDisabledUntilRepaired()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            var t = RaidFixture.Turret(ctx, st, 40, 40);
            RaidFixture.Guard(st, "skitter", 43.5, 40.5);

            var max = TurretRules.MaxHp(ctx.Data, t);
            TurretRules.Damage(ctx, st, t, max);
            Assert.That(TurretQueries.Disabled(ctx, st, t.Id), Is.True);
            Assert.That(ProductionQueries.OperatingState(ctx, st, t.Id), Is.EqualTo(MachineOperatingState.Disabled));
            Assert.That(ProductionQueries.StateText(MachineOperatingState.Disabled), Is.EqualTo("disabled (0 hp)"));
            Assert.That(RaidFixture.Last<StructureDamagedEvent>(st), Is.Not.Null);

            var before = t.Rounds;
            RaidFixture.Run(ctx, st, 100, Combat);
            Assert.That(t.Rounds, Is.EqualTo(before), "a knocked-out turret fires nothing");

            TurretRules.TurretRepairHook(ctx, st, t.Id, max);
            Assert.That(TurretQueries.Disabled(ctx, st, t.Id), Is.False);
            Assert.That(TurretQueries.Hp(ctx, st, t.Id), Is.EqualTo(max));
            Assert.That(RaidFixture.Last<StructureRepairedEvent>(st), Is.Not.Null);

            RaidFixture.Run(ctx, st, 100, Combat);
            Assert.That(t.Rounds, Is.LessThan(before), "repaired, it fires again");
        }

        /// <summary>
        /// C-09's <c>readyOpeningTurret</c> gate: ready means supplied AND completely full, so the tutorial's
        /// "prepared turret" step cannot be satisfied by a half-loaded one.
        /// </summary>
        [Test]
        public void ReadyMeansSuppliedAndFull()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            var t = RaidFixture.Add(ctx, st, "turret", 40, 40);
            var capacity = TurretHopper.Capacity(ctx.Data, t);

            TurretHopper.Give(ctx, st, t.Id, ItemId.Magazine, capacity);
            RaidFixture.Run(ctx, st, 2, Combat);
            Assert.That(TurretQueries.Ready(ctx, st, t.Id), Is.False, "full, but nothing is powering it");

            RaidFixture.Power(ctx, st, 44, 44);
            RaidFixture.Run(ctx, st, 2, Combat);
            Assert.That(TurretQueries.Ready(ctx, st, t.Id), Is.True, "powered and full");
            Assert.That(TurretQueries.Loaded(ctx, st), Is.EqualTo(1));

            t.Rounds = capacity - 1;
            Assert.That(TurretQueries.Ready(ctx, st, t.Id), Is.False, "one short of full is not prepared");
        }
    }
}
