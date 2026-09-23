using System.Collections.Generic;
using NUnit.Framework;
using Relight.Sim.Tests.Combat;
using Relight.Sim.UI;

namespace Relight.Sim.Tests.UI
{
    /// <summary>
    /// REL-134. The reading the health bar is drawn from — over the building in the world, and in the machine's
    /// inspect card.
    ///
    /// <para>The acceptance that matters most here is <i>"the bar tracks the same Hp/Max the text shows, so the two
    /// can never disagree"</i>. That is not something an assertion about the bar alone can establish, so the tests
    /// below put the reading and <see cref="HomeQueries.MachineRepairCard"/> — the query the card's own sentence is
    /// written from — side by side and require the numbers to be equal, in each of the states a building passes
    /// through. If either side is ever re-pointed at a different source, these fail.</para>
    ///
    /// <para>Damage and repair go through the REAL <see cref="TurretRules.Damage"/> and the real
    /// <see cref="RepairCommand"/>, never by writing <c>u.Damage</c> here, for the reason
    /// <c>BuildingConditionTests</c> gives: a test that sets the field itself still passes when the game stops
    /// setting it.</para>
    /// </summary>
    public sealed class BuildingHealthTests
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

        /// <summary>The engineer beside an undamaged turret, paid up and in reach of it.</summary>
        private static SimState BesideATurret(SimContext ctx, out Machine turret)
        {
            var st = RaidFixture.State(ctx);
            new HomeCoreInitializer().Init(ctx, st);
            turret = RaidFixture.Turret(ctx, st, Tx, Ty);
            st.Engineer.Pos = new Vec2(Tx - 0.5, Ty - 0.5);
            st.Engineer.Inv[ItemId.Steel] = 40;
            st.Engineer.Inv[ItemId.Copper] = 40;
            st.Ledger = Ledger.Open(st, ctx.Data);
            return st;
        }

        /// <summary>The whole acceptance, in one place: what the bar reads and what the sentence reads are equal.</summary>
        private static void AssertAgreesWithTheCard(SimContext ctx, SimState st, Machine m, string when)
        {
            var bar = BuildingCondition.Health(ctx.Data, st, m);
            var card = HomeQueries.MachineRepairCard(ctx, st, m.Id);
            Assert.That(bar.Hp, Is.EqualTo(card.Hp).Within(1e-9), "the bar and the card disagree on HP " + when);
            Assert.That(bar.Max, Is.EqualTo(card.Max).Within(1e-9), "the bar and the card disagree on max HP " + when);
        }

        // ---------------------------------------------------------------- when a bar is shown at all

        [Test]
        public void AnUndamagedBuildingWearsNoBar()
        {
            var ctx = Context();
            var st = BesideATurret(ctx, out var turret);

            var h = BuildingCondition.Health(ctx.Data, st, turret);
            Assert.That(h.Exists, Is.True, "a turret can be damaged, so it has a reading.");
            Assert.That(h.Damaged, Is.False, "a turret at full health must not sprout a bar.");
            Assert.That(h.Fraction, Is.EqualTo(1).Within(1e-9));
            AssertAgreesWithTheCard(ctx, st, turret, "at full health");
        }

        [Test]
        public void OnePointOfDamageIsEnoughToShowOne()
        {
            var ctx = Context();
            var st = BesideATurret(ctx, out var turret);

            Assert.That(TurretRules.Damage(ctx, st, turret, 1), Is.False, "one point must not take a turret down.");

            var h = BuildingCondition.Health(ctx.Data, st, turret);
            Assert.That(h.Damaged, Is.True, "the owner's rule is 'when a building has LOST health', not 'when it is low'.");
            Assert.That(h.Fraction, Is.LessThan(1));
            Assert.That(h.Hurt, Is.False, "one point off a hundred is not yet the hurt band.");
            AssertAgreesWithTheCard(ctx, st, turret, "after one point of damage");
        }

        [Test]
        public void RepairingBackToFullTakesTheBarAway()
        {
            var ctx = Context();
            var st = BesideATurret(ctx, out var turret);
            var max = TurretRules.MaxHp(ctx.Data, turret);
            // One instalment's worth of damage exactly, so a single repair brings it back to full.
            TurretRules.Damage(ctx, st, turret, ctx.Data.Defence.RepairHp);
            Assert.That(BuildingCondition.Health(ctx.Data, st, turret).Damaged, Is.True);

            Assert.That(Send(ctx, st, new RepairCommand(RepairKinds.Machine, turret.Id)).Problem, Is.Empty);
            Run(ctx, st, ctx.Data.Defence.RepairSeconds + 1);

            var h = BuildingCondition.Health(ctx.Data, st, turret);
            Assert.That(h.Hp, Is.EqualTo(max).Within(1e-9), "the repair did not finish.");
            Assert.That(h.Damaged, Is.False, "repaired back to full and the bar is still showing.");
            AssertAgreesWithTheCard(ctx, st, turret, "after the repair landed");
        }

        [Test]
        public void ABuildingThatCannotBeDamagedNeverHasAReading()
        {
            var ctx = Context();
            var st = BesideATurret(ctx, out _);
            // The Depot is the one machine with no integrity row (REL-115 gave every BUILDABLE machine one), so it
            // is the case where "no reading" and "full health" must not be confused: both are undamaged, but only
            // one of them could ever grow a bar.
            var depot = RaidFixture.Add(ctx, st, "depot", Tx + 6, Ty);
            Assume.That(TurretRules.MaxHp(ctx.Data, depot), Is.Zero, "the Depot has gained hit points; pick another machine.");

            var h = BuildingCondition.Health(ctx.Data, st, depot);
            Assert.That(h.Exists, Is.False);
            Assert.That(h.Damaged, Is.False);
            Assert.That(h.Wrecked, Is.False, "a machine with no hit points is not a wreck, it is simply not a target.");
            Assert.That(h.Fraction, Is.Zero);
        }

        [Test]
        public void ThereIsNoReadingForANullMachineOrANullState()
        {
            var ctx = Context();
            var st = BesideATurret(ctx, out var turret);

            Assert.That(BuildingCondition.Health(ctx.Data, st, null).Exists, Is.False);
            Assert.That(BuildingCondition.Health(ctx.Data, null, turret).Exists, Is.False);
            Assert.That(BuildingCondition.Health(null, st, turret).Exists, Is.False);
            Assert.That(BuildingCondition.CoreHealth(ctx.Data, null).Exists, Is.False);
            Assert.That(BuildingCondition.CoreHealth(null, st).Exists, Is.False);
            Assert.That(default(HealthReading).Damaged, Is.False, "a default reading must never draw a bar.");
        }

        // ---------------------------------------------------------------- the bands

        /// <summary>Real damage, enough to leave the building sitting at <paramref name="fraction"/> of its maximum.</summary>
        private static HealthReading DamageTo(SimContext ctx, SimState st, Machine m, double fraction)
        {
            var max = TurretRules.MaxHp(ctx.Data, m);
            var wanted = TurretRules.Hp(ctx.Data, st, m) - max * fraction;
            if (wanted > 0) TurretRules.Damage(ctx, st, m, wanted);
            return BuildingCondition.Health(ctx.Data, st, m);
        }

        [Test]
        public void TheBandsChangeAtTheFractionsTheCardAndTheWorldBothRead()
        {
            var ctx = Context();
            var st = BesideATurret(ctx, out var turret);

            // Either side of each threshold, walked downwards by real damage. Written against the constants rather
            // than against 0.66 and 0.30, so a later re-tuning moves the test with the game instead of failing it.
            var h = DamageTo(ctx, st, turret, Midpoint(1, BuildingCondition.HurtBelow));
            Assert.That(h.Hurt, Is.False, "still above the hurt fraction.");
            Assert.That(h.Critical, Is.False);

            h = DamageTo(ctx, st, turret, Midpoint(BuildingCondition.HurtBelow, BuildingCondition.CriticalBelow));
            Assert.That(h.Hurt, Is.True);
            Assert.That(h.Critical, Is.False, "hurt is not yet critical, or the amber band would never be seen.");

            h = DamageTo(ctx, st, turret, BuildingCondition.CriticalBelow * 0.5);
            Assert.That(h.Critical, Is.True);
            Assert.That(h.Wrecked, Is.False, "critical must not be confused with down — one of them still works.");
            Assert.That(h.Fraction, Is.GreaterThan(0));
            AssertAgreesWithTheCard(ctx, st, turret, "in the critical band");
        }

        private static double Midpoint(double a, double b) => (a + b) * 0.5;

        [Test]
        public void AWreckReadsEmptyAndStillHasABarToRead()
        {
            var ctx = Context();
            var st = BesideATurret(ctx, out var turret);

            Assert.That(TurretRules.Damage(ctx, st, turret, TurretRules.MaxHp(ctx.Data, turret) + 5), Is.True,
                "that should have taken the turret down.");

            var h = BuildingCondition.Health(ctx.Data, st, turret);
            Assert.That(h.Wrecked, Is.True);
            Assert.That(h.Critical, Is.True, "a wreck is drawn in the reddest band.");
            Assert.That(h.Hurt, Is.False, "a wreck is past hurt, and the amber band must not claim it.");
            Assert.That(h.Fraction, Is.Zero, "a wreck's bar is empty, not negative.");
            Assert.That(h.Damaged, Is.True, "a wreck is still worth a bar — it is what the player repairs first.");
            AssertAgreesWithTheCard(ctx, st, turret, "once it is a wreck");
        }

        [Test]
        public void AnOverHealedOrStaleReadingIsClampedRatherThanDrawnOffItsTrack()
        {
            // Not reachable through the commands, but a save written by an older build, or one whose catalogue has
            // since been re-tuned downwards, hands the presenter exactly this. A fraction above 1 would draw a fill
            // longer than its own track, and a negative one would draw it backwards.
            Assert.That(new HealthReading(500, 100).Fraction, Is.EqualTo(1));
            Assert.That(new HealthReading(500, 100).Damaged, Is.False);
            Assert.That(new HealthReading(-40, 100).Fraction, Is.Zero);
            Assert.That(new HealthReading(-40, 100).Wrecked, Is.True);
        }

        // ---------------------------------------------------------------- the Home core

        [Test]
        public void TheCoreReadsWhatTheHudReadsAndOnlyWearsABarWhenItHasLostHealth()
        {
            var ctx = Context();
            var st = BesideATurret(ctx, out _);

            var whole = BuildingCondition.CoreHealth(ctx.Data, st);
            Assert.That(whole.Exists, Is.True, "the fixture's core is placed.");
            Assert.That(whole.Damaged, Is.False, "an intact core must not wear a bar either.");

            HomeCore.Damage(st, st.Home.Hp * 0.5);

            var hurt = BuildingCondition.CoreHealth(ctx.Data, st);
            var card = HomeQueries.RepairCard(st, ctx.Data);
            Assert.That(hurt.Damaged, Is.True);
            Assert.That(hurt.Hp, Is.EqualTo(card.Hp).Within(1e-9), "the core's bar and the HUD's figure disagree.");
            Assert.That(hurt.Max, Is.EqualTo(card.Max).Within(1e-9));
            Assert.That(hurt.Fraction, Is.EqualTo(0.5).Within(1e-6));
        }

        [Test]
        public void AKnockedOutCoreReadsEmptyRatherThanAbsent()
        {
            var ctx = Context();
            var st = BesideATurret(ctx, out _);
            HomeCore.Damage(st, st.Home.Hp + 10);

            var h = BuildingCondition.CoreHealth(ctx.Data, st);
            Assert.That(st.Home.Placed, Is.True, "a knocked-out core still stands; it is disabled, not gone.");
            Assert.That(h.Exists, Is.True, "so it still has a reading, and still shows an empty bar.");
            Assert.That(h.Wrecked, Is.True);
            Assert.That(h.Fraction, Is.Zero);
        }

        // ---------------------------------------------------------------- it is a picture, and nothing else

        [Test]
        public void AskingChangesNothingInTheSimulation()
        {
            var ctx = Context();
            var st = BesideATurret(ctx, out var turret);
            TurretRules.Damage(ctx, st, turret, 30);

            // A chest that has never been hit, asked about alongside the damaged turret: TurretState.Find must not
            // become Of and start making a damage entry for a building merely because a bar asked about it.
            var untouched = RaidFixture.Add(ctx, st, "chest", Tx + 6, Ty + 6);

            var rev = st.Rev;
            var units = st.Turrets.Units.Count;
            var events = st.Events.Count;
            var hp = TurretRules.Hp(ctx.Data, st, turret);
            var coreHp = st.Home.Hp;

            for (var i = 0; i < 200; i++)
            {
                var _ = BuildingCondition.Health(ctx.Data, st, turret);
                var __ = BuildingCondition.CoreHealth(ctx.Data, st);
                var ___ = BuildingCondition.Health(ctx.Data, st, untouched);
            }

            Assert.That(st.Turrets.Units.Count, Is.EqualTo(units), "asking about a building made a damage entry for it.");
            Assert.That(st.Events.Count, Is.EqualTo(events), "asking raised an event.");
            Assert.That(TurretRules.Hp(ctx.Data, st, turret), Is.EqualTo(hp).Within(1e-9));
            Assert.That(st.Home.Hp, Is.EqualTo(coreHp).Within(1e-9));
            Assert.That(st.Rev, Is.EqualTo(rev), "asking bumped the structural revision.");
        }
    }
}
