using System.Collections.Generic;
using NUnit.Framework;

namespace Relight.Sim.Tests.Combat
{
    /// <summary>
    /// L-02, ALWAYS_DARK_SPEC §5.7 (U-D-59): a turret reaches an alien on a lit tile at its full range and an
    /// alien on an unlit tile only to its dark sight — 9 and 6 for the Gun turret. The alien's tile decides, the
    /// sim's lit mask is the only light that counts, and a target that loses its light beyond dark sight is dropped.
    ///
    /// No <see cref="EnemyPhase"/> runs here, so a body stays exactly where the test put it and every distance
    /// below is the distance the rule sees. The turret stands at 40,40 (2 × 2), so its centre is 41,41.
    /// </summary>
    public sealed class TurretDarkSightTests
    {
        private static List<ITickPhase> Phases() =>
            new List<ITickPhase> { new PowerPhase(), new LightPhase(), new TurretPhase() };

        private static int Shots(SimState st) => RaidFixture.Count<TurretShotEvent>(st);

        /// <summary>A powered Lamp (radius 4) at <paramref name="x"/>,<paramref name="y"/> on its own generator.</summary>
        private static Machine Lamp(SimContext ctx, SimState st, int x, int y)
        {
            var lamp = RaidFixture.Add(ctx, st, "lamp", x, y);
            RaidFixture.Power(ctx, st, x + 2, y);
            return lamp;
        }

        [Test]
        public void TheCatalogueGivesTheGunTurretNineLitAndSixDark()
        {
            var ctx = RaidFixture.Context();
            Assert.That(ctx.Data.TryTurret("turret", out var def), Is.True);
            Assert.That(def.RangeTiles, Is.EqualTo(9).Within(1e-9));
            Assert.That(def.DarkSightTiles, Is.EqualTo(6).Within(1e-9), "U-P-11: just under the Spitter's 7");
            Assert.That(TurretRules.DarkSight(def), Is.EqualTo(6).Within(1e-9));
        }

        [Test]
        public void ALitAlienIsTakenNearFullRange()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            RaidFixture.Turret(ctx, st, 40, 40);
            Lamp(ctx, st, 49, 44);                                   // lights tile 49,41 (d² = 9)
            RaidFixture.Guard(st, "biter", 49.5, 41.5);              // 8.51 tiles from the centre

            RaidFixture.Run(ctx, st, 100, Phases());

            Assert.That(LightQueries.LitAt(st, 49, 41), Is.True, "the fixture must light the alien's tile");
            Assert.That(Shots(st), Is.GreaterThan(0), "an alien under a lamp is in reach out to 9");
        }

        [Test]
        public void AnUnlitAlienSevenTilesOutIsRefused()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            var t = RaidFixture.Turret(ctx, st, 40, 40);
            RaidFixture.Guard(st, "biter", 48.0, 41.0);              // exactly 7 out, inside the old range of 9

            RaidFixture.Run(ctx, st, 100, Phases());

            Assert.That(LightQueries.LitAt(st, 48, 41), Is.False);
            Assert.That(Shots(st), Is.Zero, "beyond dark sight, on unlit ground: not a target");
            Assert.That(st.Turrets.Of(t.Id).Target, Is.Zero);
            Assert.That(t.Rounds, Is.EqualTo(50));
        }

        [Test]
        public void AnUnlitAlienSixTilesOutIsTaken()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            RaidFixture.Turret(ctx, st, 40, 40);
            RaidFixture.Guard(st, "biter", 47.0, 41.0);              // exactly 6 out: the edge is inside

            RaidFixture.Run(ctx, st, 100, Phases());

            Assert.That(LightQueries.LitAt(st, 47, 41), Is.False);
            Assert.That(Shots(st), Is.GreaterThan(0), "dark sight reaches 6");
        }

        [Test]
        public void ATargetThatStepsOffLitGroundBeyondDarkSightIsDropped()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            var t = RaidFixture.Turret(ctx, st, 40, 40);
            Lamp(ctx, st, 49, 44);
            var e = RaidFixture.Guard(st, "biter", 49.5, 41.5);

            RaidFixture.Run(ctx, st, 100, Phases());
            var before = Shots(st);
            Assert.That(before, Is.GreaterThan(0));
            Assert.That(st.Turrets.Of(t.Id).Target, Is.EqualTo(e.Id));

            e.Pos = new Vec2(41.5, 49.5);                            // still 8.51 out, but on unlit ground
            RaidFixture.Run(ctx, st, 100, Phases());

            Assert.That(LightQueries.LitAt(st, 41, 49), Is.False);
            Assert.That(Shots(st), Is.EqualTo(before), "not one more round after the light was lost");
            Assert.That(st.Turrets.Of(t.Id).Target, Is.Zero, "the target is dropped, not merely held without firing");
        }

        /// <summary>
        /// A save and a state hash walk <see cref="LightState.Visit"/> on the LIVE state. In the game's own tick
        /// order the turrets run before the light phase, so a walk that threw the mask away would leave every
        /// tile unlit for the tick after each autosave and a turret would drop a target it can plainly see.
        /// </summary>
        [Test]
        public void SavingOrHashingTheStateDoesNotPutTheLightsOutForATick()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            var t = RaidFixture.Turret(ctx, st, 40, 40);
            Lamp(ctx, st, 49, 44);
            var e = RaidFixture.Guard(st, "biter", 49.5, 41.5);      // 8.51 out: in reach only while it is lit
            var gameOrder = new List<ITickPhase> { new PowerPhase(), new TurretPhase(), new LightPhase() };

            RaidFixture.Run(ctx, st, 100, gameOrder);
            Assert.That(st.Turrets.Of(t.Id).Target, Is.EqualTo(e.Id));

            StateHash.Compute(st);
            Assert.That(LightQueries.LitAt(st, 49, 41), Is.True, "a hash reads the state; it does not change it");

            RaidFixture.Run(ctx, st, 1, gameOrder);
            Assert.That(st.Turrets.Of(t.Id).Target, Is.EqualTo(e.Id), "the target is still held on the next tick");
        }

        /// <summary>
        /// §5.7 "a brownout now costs twice": the lamp's circuit is overloaded, its radius shrinks (U-D-58) and the
        /// alien standing at the old edge of the light is no longer a target. The turret is on its own circuit, so
        /// nothing but the light changed.
        /// </summary>
        [Test]
        public void ABrownoutThatShrinksTheLampLosesTheTargetAtTheOldEdge()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            var t = RaidFixture.Turret(ctx, st, 30, 28);             // centre 31,29; pole 32,30; generator 34,30
            var lamp = Lamp(ctx, st, 30, 40);                        // pole 32,40 is 10 from the turret's: no link
            var e = RaidFixture.Guard(st, "biter", 30.5, 36.5);      // 4 tiles north of the lamp, 7.52 from the turret

            RaidFixture.Run(ctx, st, 100, Phases());
            var before = Shots(st);
            Assert.That(LightQueries.LitAt(st, 30, 36), Is.True);
            Assert.That(before, Is.GreaterThan(0));

            // Six 100 kW assemblers on the lamp's pole: 605 kW asked of 300 kW, radius 4 shrinks to about 3.
            foreach (var (x, y) in new[] { (28, 43), (32, 43), (36, 43), (28, 47), (32, 47), (36, 47) })
                RaidFixture.Add(ctx, st, "assembler", x, y);
            RaidFixture.Run(ctx, st, 100, Phases());

            Assert.That(PowerQueries.Throttle(ctx, st, lamp.Id), Is.GreaterThan(0).And.LessThan(0.6), "the lamp must brown out");
            Assert.That(PowerQueries.Throttle(ctx, st, t.Id), Is.EqualTo(1).Within(1e-9), "the turret must not");
            Assert.That(LightQueries.LitAt(st, 30, 36), Is.False, "the old edge of the light went dark");
            var settled = Shots(st);
            RaidFixture.Run(ctx, st, 100, Phases());
            Assert.That(Shots(st), Is.EqualTo(settled), "and the turret lost the target standing on it");
            Assert.That(st.Turrets.Of(t.Id).Target, Is.Zero);
            Assert.That(e.Hp, Is.GreaterThan(0));
        }

        /// <summary>
        /// D-UI-11 stands: the flashlight is a picture. The sim has no flashlight source at all, so the engineer
        /// standing beside an unlit alien lights nothing and the turret still cannot see it.
        /// </summary>
        [Test]
        public void TheEngineerStandingBesideAnUnlitAlienChangesNothing()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            RaidFixture.Turret(ctx, st, 40, 40);
            RaidFixture.Guard(st, "biter", 48.0, 41.0);
            st.Engineer.Pos = new Vec2(48.5, 42.5);

            RaidFixture.Run(ctx, st, 100, Phases());

            Assert.That(LightQueries.LitAt(st, 48, 42), Is.False, "the engineer carries no light the sim knows about");
            Assert.That(LightQueries.LitAt(st, 48, 41), Is.False);
            Assert.That(Shots(st), Is.Zero);
        }

        /// <summary>
        /// §5.7 "making it readable": a turret hit while an alien it cannot see stands in the dark inside its full
        /// range is blind, says so once, and stops saying so when the ground is lit or the hits stop.
        /// </summary>
        [Test]
        public void ATurretHitFromTheDarkIsBlindOncePerSpell()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            var t = RaidFixture.Turret(ctx, st, 40, 40);
            RaidFixture.Guard(st, "spitter", 48.0, 41.0);            // 7 out, unlit: the Spitter's range, not the turret's
            RaidFixture.Run(ctx, st, 5, Phases());
            Assert.That(TurretQueries.Blind(ctx, st, t.Id), Is.False, "nothing has hit it yet");

            for (var i = 0; i < 40; i++)
            {
                TurretRules.Damage(ctx, st, t, 1);
                RaidFixture.Run(ctx, st, 1, Phases());
            }
            Assert.That(TurretQueries.Blind(ctx, st, t.Id), Is.True);
            Assert.That(RaidFixture.Count<TurretBlindEvent>(st), Is.EqualTo(1), "once per spell, not once per hit");
            Assert.That(RaidFixture.Last<TurretBlindEvent>(st).MachineId, Is.EqualTo(t.Id));

            RaidFixture.Run(ctx, st, (int)(TurretQueries.BlindSeconds / RaidFixture.Dt) + 2, Phases());
            Assert.That(TurretQueries.Blind(ctx, st, t.Id), Is.False, "the hits stopped");

            Lamp(ctx, st, 48, 43);                                   // light the alien's ground: the turret can answer
            TurretRules.Damage(ctx, st, t, 1);
            RaidFixture.Run(ctx, st, 20, Phases());
            Assert.That(LightQueries.LitAt(st, 48, 41), Is.True);
            Assert.That(TurretQueries.Blind(ctx, st, t.Id), Is.False);
            Assert.That(Shots(st), Is.GreaterThan(0));
            Assert.That(RaidFixture.Count<TurretBlindEvent>(st), Is.EqualTo(1));
        }

        /// <summary>
        /// REL-14 (INT-10). The gap the audit named: a turret firing at the thing in front of it while something it
        /// cannot see shells it from the dark. The biter at 4 tiles is inside dark sight, so the gun holds it as a
        /// target and keeps shooting; the Spitter at 7 is unlit and beyond dark sight, so a lamp on ITS ground is
        /// still the answer, and the badge and the guide line must both say so. It used to be silent here.
        /// </summary>
        [Test]
        public void ATurretFiringAtOneAlienIsStillBlindToTheOneShellingItFromTheDark()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            var t = RaidFixture.Turret(ctx, st, 40, 40);
            RaidFixture.Guard(st, "biter", 45.0, 41.0);              // 4 out, unlit: inside dark sight, so a real target
            RaidFixture.Guard(st, "spitter", 48.0, 41.0);            // 7 out, unlit: beyond dark sight, unanswerable

            for (var i = 0; i < 40; i++)
            {
                TurretRules.Damage(ctx, st, t, 1);
                RaidFixture.Run(ctx, st, 1, Phases());
            }

            Assert.That(st.Turrets.Of(t.Id).Target, Is.Not.Zero, "the near biter is in reach even unlit");
            Assert.That(Shots(st), Is.GreaterThan(0), "and the gun is firing at it");
            Assert.That(TurretQueries.Blind(ctx, st, t.Id), Is.True, "REL-14: having a target is no reason to stay quiet");
            Assert.That(RaidFixture.Count<TurretBlindEvent>(st), Is.EqualTo(1));
            Assert.That(TurretQueries.BlindNow(st, t.Id), Is.True, "the tick latched the same answer the badge reads");
        }

        /// <summary>
        /// REL-14: the badge reads <see cref="TurretQueries.BlindNow"/>, the latch the tick wrote, so it can never
        /// disagree with the guide line the same tick's event raised.
        /// </summary>
        [Test]
        public void TheLatchedAnswerFollowsTheQueryTickByTick()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            var t = RaidFixture.Turret(ctx, st, 40, 40);
            RaidFixture.Guard(st, "spitter", 48.0, 41.0);
            RaidFixture.Run(ctx, st, 5, Phases());
            Assert.That(TurretQueries.BlindNow(st, t.Id), Is.False, "nothing has hit it yet");

            for (var i = 0; i < 40; i++)
            {
                TurretRules.Damage(ctx, st, t, 1);
                RaidFixture.Run(ctx, st, 1, Phases());
            }
            Assert.That(TurretQueries.BlindNow(st, t.Id), Is.True);

            RaidFixture.Run(ctx, st, (int)(TurretQueries.BlindSeconds / RaidFixture.Dt) + 2, Phases());
            Assert.That(TurretQueries.BlindNow(st, t.Id), Is.False, "the hits stopped, and the latch let go");
        }

        [Test]
        public void ATurretHitWithNoAlienInTheDarkIsNotCalledBlind()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            var t = RaidFixture.Turret(ctx, st, 40, 40);
            RaidFixture.Guard(st, "spitter", 60.0, 41.0);            // far outside its full range: a lamp would not help
            TurretRules.Damage(ctx, st, t, 1);
            RaidFixture.Run(ctx, st, 5, Phases());

            Assert.That(TurretQueries.Blind(ctx, st, t.Id), Is.False);
            Assert.That(RaidFixture.Count<TurretBlindEvent>(st), Is.Zero);
        }

        /// <summary>An empty turret hit from the dark is out of ammunition, not blind: a lamp would not make it fire.</summary>
        [Test]
        public void AnEmptyTurretHitFromTheDarkIsNotCalledBlind()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            var t = RaidFixture.Turret(ctx, st, 40, 40);
            t.Rounds = 0;
            RaidFixture.Guard(st, "spitter", 48.0, 41.0);
            for (var i = 0; i < 10; i++)
            {
                TurretRules.Damage(ctx, st, t, 1);
                RaidFixture.Run(ctx, st, 1, Phases());
            }

            Assert.That(TurretQueries.Blind(ctx, st, t.Id), Is.False);
            Assert.That(RaidFixture.Count<TurretBlindEvent>(st), Is.Zero);
        }

        /// <summary>Dark sight equal to the range switches the rule off for that turret (§5.7).</summary>
        [Test]
        public void DarkSightEqualToRangeSwitchesTheRuleOff()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            Assert.That(ctx.Data.TryTurret("turret", out var def), Is.True);
            var open = def with { DarkSightTiles = def.RangeTiles };

            Assert.That(TurretRules.Reach(st, open, 48.0, 41.0), Is.EqualTo(def.RangeTiles).Within(1e-9));
            Assert.That(TurretRules.Reach(st, def, 48.0, 41.0), Is.EqualTo(6).Within(1e-9));
        }
    }
}
