using System.Collections.Generic;
using NUnit.Framework;
using Relight.Sim.Tests.Combat;
using Relight.Sim.UI;

namespace Relight.Sim.Tests.UI
{
    /// <summary>
    /// E-17 (U-D-61): a turret's two ammunition states and the ONE per-place row that reports dry turrets and
    /// wrecks. Low is under a quarter of the hopper; unpowered reads first; a low turret raises nothing until a
    /// raid is warned; two dry turrets at one place are one row; the row clears itself; nothing shows at game
    /// start; and the wreck count is the number of machines at 0 hit points.
    /// </summary>
    public sealed class DefenceAlertTests
    {
        private static readonly List<ITickPhase> PowerOnly = new List<ITickPhase> { new PowerPhase() };

        private static Machine Gun(SimContext ctx, SimState st, int x, int y, int rounds, bool fired = true)
        {
            var t = RaidFixture.Turret(ctx, st, x, y, rounds);
            if (fired) st.Turrets.Of(t.Id).ShotT = 1;            // the saved witness that it has been loaded and used
            return t;
        }

        private static void Settle(SimContext ctx, SimState st) => RaidFixture.Run(ctx, st, 2, PowerOnly);

        private static void Wreck(SimContext ctx, SimState st, Machine m) =>
            TurretRules.Damage(ctx, st, m, TurretRules.MaxHp(ctx.Data, m) + 1);

        private static void WarnARaid(SimState st) => st.Director.Minor = new MinorRaid { StartsAt = st.T + 30 };

        // ---- the two states ------------------------------------------------------------------------------------

        [Test]
        public void AGunTurretIsLowAtTwelveRoundsNotAtThirteenAndDryAtNone()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            var t = RaidFixture.Turret(ctx, st, 40, 40, 13);
            Assert.That(TurretHopper.Capacity(ctx.Data, t), Is.EqualTo(50));
            Assert.That(TurretAmmo.State(ctx.Data, t), Is.EqualTo(TurretAmmoState.Ok));
            t.Rounds = 12;
            Assert.That(TurretAmmo.State(ctx.Data, t), Is.EqualTo(TurretAmmoState.Low));
            t.Rounds = 1;
            Assert.That(TurretAmmo.State(ctx.Data, t), Is.EqualTo(TurretAmmoState.Low));
            t.Rounds = 0;
            Assert.That(TurretAmmo.State(ctx.Data, t), Is.EqualTo(TurretAmmoState.Dry));
        }

        [Test]
        public void TheHoverSaysWhatTheBadgeSays()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            var t = RaidFixture.Turret(ctx, st, 40, 40, 50);
            Settle(ctx, st);
            Assert.That(ProductionQueries.Description(ctx, st, t.Id), Does.Not.Contain("ammunition"));
            t.Rounds = 9;
            Assert.That(ProductionQueries.Description(ctx, st, t.Id), Does.Contain("Low on ammunition · 9 / 50 rounds"));
            t.Rounds = 0;
            Assert.That(ProductionQueries.Description(ctx, st, t.Id), Does.Contain("Out of ammunition"));
        }

        [Test]
        public void AMachineThatIsNotATurretHasNoAmmunitionState()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            var wall = RaidFixture.Add(ctx, st, "wall", 40, 40);
            Assert.That(TurretAmmo.State(ctx.Data, wall), Is.EqualTo(TurretAmmoState.Ok));
        }

        [Test]
        public void ATurretCountsAsLoadedOnceItHoldsARoundOrHasFiredOne()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            var t = RaidFixture.Turret(ctx, st, 40, 40, 0);
            Assert.That(TurretAmmo.EverLoaded(st, t), Is.False, "just placed");
            t.Rounds = 5;
            Assert.That(TurretAmmo.EverLoaded(st, t), Is.True, "holds a round");
            t.Rounds = 0;
            st.Turrets.Of(t.Id).ShotT = 12.5;
            Assert.That(TurretAmmo.EverLoaded(st, t), Is.True, "has fired");
        }

        // ---- the row -------------------------------------------------------------------------------------------

        [Test]
        public void NothingIsRaisedAtGameStartOrForATurretThatWasNeverLoaded()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            var source = new DefenceAlertSource();
            Assert.That(source.Refresh(ctx, st), Is.False, "an empty map");

            Gun(ctx, st, 40, 40, 0, fired: false);
            Settle(ctx, st);
            WarnARaid(st);
            Assert.That(source.Refresh(ctx, st), Is.False, "placed and not fed yet");
        }

        [Test]
        public void AnUnpoweredDryTurretIsThePowerAlertsBusiness()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            var t = RaidFixture.Add(ctx, st, "turret", 40, 40);
            st.Turrets.Of(t.Id).ShotT = 1;
            Settle(ctx, st);
            Assert.That(ProductionQueries.OperatingState(ctx, st, t.Id), Is.EqualTo(MachineOperatingState.Unpowered));
            Assert.That(new DefenceAlertSource().Refresh(ctx, st), Is.False);
        }

        [Test]
        public void ALowTurretRaisesNothingUntilARaidIsWarned()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            Gun(ctx, st, 40, 40, 12);
            Settle(ctx, st);
            var source = new DefenceAlertSource();
            Assert.That(source.Refresh(ctx, st), Is.False, "peacetime");

            WarnARaid(st);
            Assert.That(source.Refresh(ctx, st), Is.True, "warned");
            Assert.That(source.Rows.Count, Is.EqualTo(1));
            Assert.That(source.Rows[0].Low, Is.EqualTo(1));
            Assert.That(source.Rows[0].Urgent, Is.True);
            Assert.That(source.Rows[0].Text, Is.EqualTo("Home: 1 turret low on ammunition"));

            st.Director.Minor = null;
            Assert.That(source.Refresh(ctx, st), Is.False, "the raid is over and nothing is dry");
        }

        [Test]
        public void TwoDryTurretsAtOnePlaceAreOneRow()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            Gun(ctx, st, 40, 40, 0);
            Gun(ctx, st, 50, 40, 0);
            Settle(ctx, st);
            var source = new DefenceAlertSource();
            Assert.That(source.Refresh(ctx, st), Is.True);
            Assert.That(source.Rows.Count, Is.EqualTo(1));
            Assert.That(source.Rows[0].Key, Is.EqualTo("defence:Home"));
            Assert.That(source.Rows[0].Text, Is.EqualTo("Home: 2 turrets dry"));
            Assert.That(source.Rows[0].Urgent, Is.False);
        }

        [Test]
        public void EachPlaceIsNamedForItsNearestSubstation()
        {
            var sites = new WorldSites(new List<SiteRecord>
            {
                new SiteRecord("home", "Home Court", SiteKind.Core, RaidFixture.CoreX, RaidFixture.CoreY, 8, 8, "", 0),
                new SiteRecord("sub-a", "Ironworks", SiteKind.Substation, 20, 20, 2, 2, "", 0),
                new SiteRecord("sub-b", "Riverside", SiteKind.Substation, 130, 130, 2, 2, "", 0),
            });
            var ctx = new SimContext(ReferenceData.Create(), RaidFixture.Map(), null, null, sites);
            var st = RaidFixture.State(ctx);
            Gun(ctx, st, 30, 30, 0);
            Gun(ctx, st, 120, 120, 0);
            Settle(ctx, st);
            var source = new DefenceAlertSource();
            Assert.That(source.Refresh(ctx, st), Is.True);
            Assert.That(source.Rows.Count, Is.EqualTo(2));
            Assert.That(source.Rows[0].Text, Is.EqualTo("Ironworks: 1 turret dry"));
            Assert.That(source.Rows[1].Text, Is.EqualTo("Riverside: 1 turret dry"));
        }

        [Test]
        public void APlaceTakesTheNameOfTheLabelNearestItsSubstation()
        {
            // The real city's substation sites are called "Substation 0" to "Substation 8", and each has one named
            // label beside it. The player is told the label. The district is still the nearest substation's: the
            // second turret stands closer to the first label than to its own, and keeps its own district's name.
            var sites = new WorldSites(new List<SiteRecord>
            {
                new SiteRecord("home", "Home Court", SiteKind.Core, RaidFixture.CoreX, RaidFixture.CoreY, 8, 8, "", 0),
                new SiteRecord("substation:0", "Substation 0", SiteKind.Substation, 20, 20, 2, 2, "", 0),
                new SiteRecord("substation:1", "Substation 1", SiteKind.Substation, 130, 130, 2, 2, "", 0),
                new SiteRecord("label:0", "Founders Court", SiteKind.Label, 60, 60, 1, 1, "", 0),
                new SiteRecord("label:1", "Ironworks", SiteKind.Label, 150, 150, 1, 1, "", 0),
            });
            var ctx = new SimContext(ReferenceData.Create(), RaidFixture.Map(), null, null, sites);
            var st = RaidFixture.State(ctx);
            Gun(ctx, st, 30, 30, 0);
            Gun(ctx, st, 84, 84, 0);
            Settle(ctx, st);
            var source = new DefenceAlertSource();
            Assert.That(source.Refresh(ctx, st), Is.True);
            Assert.That(source.Rows.Count, Is.EqualTo(2));
            Assert.That(source.Rows[0].Text, Is.EqualTo("Founders Court: 1 turret dry"));
            Assert.That(source.Rows[1].Text, Is.EqualTo("Ironworks: 1 turret dry"));
        }

        [Test]
        public void TheWreckCountIsTheNumberOfMachinesAtNoHitPoints()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            Gun(ctx, st, 40, 40, 0);
            var dead = Gun(ctx, st, 50, 40, 0);
            var wall = RaidFixture.Add(ctx, st, "wall", 60, 40);
            var scuffed = RaidFixture.Add(ctx, st, "wall", 61, 40);
            Settle(ctx, st);
            Wreck(ctx, st, dead);
            Wreck(ctx, st, wall);
            TurretRules.Damage(ctx, st, scuffed, 1);

            var expected = 0;
            for (var i = 0; i < st.Machines.Count; i++)
                if (TurretRules.Wrecked(ctx.Data, st, st.Machines[i])) expected++;
            Assert.That(expected, Is.EqualTo(2));

            var source = new DefenceAlertSource();
            Assert.That(source.Refresh(ctx, st), Is.True);
            Assert.That(source.Rows[0].Wrecks, Is.EqualTo(expected));
            Assert.That(source.Rows[0].Dry, Is.EqualTo(1), "a wrecked turret is a wreck, not a dry turret");
            Assert.That(source.Rows[0].Text, Is.EqualTo("Home: 1 turret dry, 2 wrecks"));
        }

        [Test]
        public void WrecksAloneNeverRaiseARow()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            var wall = RaidFixture.Add(ctx, st, "wall", 60, 40);
            Wreck(ctx, st, wall);
            Assert.That(new DefenceAlertSource().Refresh(ctx, st), Is.False);
        }

        [Test]
        public void TheRowClearsItselfOnceTheTurretsAreReloadedAndTheWrecksRepaired()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            var t = Gun(ctx, st, 40, 40, 0);
            var wall = RaidFixture.Add(ctx, st, "wall", 60, 40);
            Settle(ctx, st);
            Wreck(ctx, st, wall);
            var source = new DefenceAlertSource();
            Assert.That(source.Refresh(ctx, st), Is.True);

            t.Rounds = 50;
            Assert.That(source.Refresh(ctx, st), Is.True, "reloaded, but the wreck still stands");
            Assert.That(source.Rows[0].Text, Is.EqualTo("Home: 1 wreck"));

            TurretRules.TurretRepairHook(ctx, st, wall.Id, TurretRules.MaxHp(ctx.Data, wall));
            Assert.That(source.Refresh(ctx, st), Is.False, "reloaded and repaired");

            Wreck(ctx, st, wall);
            Assert.That(source.Refresh(ctx, st), Is.False, "a cleared row is not brought back by a wreck alone");
        }
    }
}
