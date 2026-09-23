using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Relight.Sim.Tests.Persistence;

namespace Relight.Sim.Tests.Combat
{
    /// <summary>
    /// Batch 4, FRT-06 (REL-141): the stronghold guardian. The accept line: the charge is a straight line and stops
    /// at a wall; phase 2 begins at 300 HP with the shorter windup and recovery; the alert wakes every body of the
    /// guardian's arena and nothing else; the Power core drops where it dies. The garrison it is born with is
    /// StrongholdTests' map.
    ///
    /// The map is <see cref="RaidFixture"/>'s open 160-tile square, with a wall where a test needs one. Only the
    /// enemy phase runs.
    /// </summary>
    public sealed class GuardianTests
    {
        const string Arena = "freight:arena";

        static List<ITickPhase> Phase() => new List<ITickPhase> { new EnemyPhase() };

        static void Seconds(SimContext ctx, SimState st, double s) => RaidFixture.Run(ctx, st, (int)(s * 20), Phase());

        static Enemy Guardian(SimState st, double x, double y)
        {
            var e = RaidFixture.Guard(st, GuardianRules.Kind, x, y, 600);
            e.Site = Arena;
            return e;
        }

        static void Charging(SimState st, Enemy e, double ax, double ay)
        {
            e.Aim = new Vec2(ax, ay);
            GuardianRules.BeginCharge(st, e);
        }

        [Test]
        public void TheChargeIsAStraightLineAndStopsAtAWall()
        {
            // A wall across the line at x 50–51. The engineer is far off the line and does not matter.
            var ctx = RaidFixture.Context(RaidFixture.Map(new[] { new TileRect(50, 40, 2, 20) }));
            var st = RaidFixture.State(ctx);
            st.Engineer.Pos = new Vec2(20, 120);
            var e = Guardian(st, 40.5, 50.5);
            Charging(st, e, 60.5, 50.5);
            var xs = new List<double>();
            for (var i = 0; i < 20 && e.Phase == EnemyPhaseKind.Charge; i++)
            {
                Seconds(ctx, st, 0.05);
                Assert.That(e.Pos.Y, Is.EqualTo(50.5).Within(1e-9), "it never leaves the line");
                xs.Add(e.Pos.X);
            }
            Assert.That(e.Phase, Is.EqualTo(EnemyPhaseKind.Recover), "the wall ended it");
            Assert.That(e.Pos.X, Is.LessThan(50 - GuardianRules.BodyRadius + 1e-9), "it stopped short of the wall");
            Assert.That(e.Pos.X, Is.GreaterThan(49), "and only just short");
            Assert.That(xs, Is.Ordered, "always forward");
            Assert.That(e.Until - st.T, Is.EqualTo(GuardianRules.RecoverSeconds).Within(0.06), "then 2 s standing still");
            var stood = e.Pos;
            Seconds(ctx, st, 1.9);
            Assert.That(e.Pos, Is.EqualTo(stood), "it does not move while it recovers");

            // No wall: 13 tiles a second for 0.9 s is 11.7 tiles, short of an aim 20 off.
            var ctx2 = RaidFixture.Context();
            var st2 = RaidFixture.State(ctx2);
            st2.Engineer.Pos = new Vec2(20, 120);
            var f = Guardian(st2, 20.5, 30.5);
            Charging(st2, f, 40.5, 30.5);
            Seconds(ctx2, st2, 1.2);
            Assert.That(f.Phase, Is.EqualTo(EnemyPhaseKind.Recover));
            Assert.That(f.Pos.X - 20.5, Is.EqualTo(GuardianRules.ChargeTilesPerS * GuardianRules.ChargeSeconds).Within(0.7));
            Assert.That(f.Pos.Y, Is.EqualTo(30.5).Within(1e-9));
        }

        [Test]
        public void TheChargeHitsTheEngineerInItsPathOnceAndMissesOneWhoSteppedAside()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            var hp = st.Engineer.Hp;
            st.Engineer.Pos = new Vec2(30.5, 30.5);
            var e = Guardian(st, 24.5, 30.5);
            Charging(st, e, 34.5, 30.5);
            Seconds(ctx, st, 1);
            Assert.That(st.Engineer.Hp, Is.EqualTo(hp - 25), "one hit of its 25");
            Assert.That(e.Phase, Is.EqualTo(EnemyPhaseKind.Recover), "the hit ends the charge");
            Assert.That(e.Pos.X, Is.LessThan(30.5), "it stopped at the engineer");

            var st2 = RaidFixture.State(ctx);
            var hp2 = st2.Engineer.Hp;
            st2.Engineer.Pos = new Vec2(30.5, 33.5);    // three tiles off the line
            var f = Guardian(st2, 24.5, 30.5);
            Charging(st2, f, 34.5, 30.5);
            Seconds(ctx, st2, 1);
            Assert.That(st2.Engineer.Hp, Is.EqualTo(hp2), "the aim is where it wound up against, not where the engineer went");
        }

        [Test]
        public void PhaseTwoBeginsAtThreeHundredWithTheShorterWindupAndRecovery()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            st.Engineer.Pos = new Vec2(30.5, 30.5);
            var e = Guardian(st, 24.5, 30.5);
            var def = ctx.Data.Enemies.Single(d => d.Key == GuardianRules.Kind);
            Assert.That((def.Hp, def.SpeedTilesPerS, def.Damage, def.WindupS), Is.EqualTo((600.0, 2.7, 25.0, 1.2)));

            e.LastKnown = st.Engineer.Pos;
            e.LastKnownUntil = st.T + 100;
            RaidFixture.Run(ctx, st, 1, Phase());
            Assert.That(e.Phase, Is.EqualTo(EnemyPhaseKind.Windup), "six tiles off, in its 9-tile reach, it winds up");
            Assert.That(e.Until - st.T, Is.EqualTo(1.2).Within(0.06));
            Assert.That(GuardianRules.ChargeRecoverSeconds(e), Is.EqualTo(2.0));
            Seconds(ctx, st, 1.25);
            Assert.That(e.Phase, Is.EqualTo(EnemyPhaseKind.Charge), "the windup ends in a charge, not a strike");

            Assert.That(Enemies.Damage(ctx, st, e.Id, 299), Is.False);
            Assert.That(GuardianRules.Enraged(e), Is.False, "301 is still phase 1");
            Assert.That(GuardianRules.WindupSeconds(e, def), Is.EqualTo(1.2));
            Enemies.Damage(ctx, st, e.Id, 1);
            Assert.That(GuardianRules.Enraged(e), Is.True, "300 is phase 2");
            Assert.That(GuardianRules.WindupSeconds(e, def), Is.EqualTo(0.8));
            Assert.That(GuardianRules.ChargeRecoverSeconds(e), Is.EqualTo(1.2));

            e.Phase = EnemyPhaseKind.Idle;
            e.LastKnown = st.Engineer.Pos;
            e.LastKnownUntil = st.T + 100;
            e.Pos = new Vec2(24.5, 30.5);
            RaidFixture.Run(ctx, st, 1, Phase());
            Assert.That(e.Phase, Is.EqualTo(EnemyPhaseKind.Windup));
            Assert.That(e.Until - st.T, Is.EqualTo(0.8).Within(0.06), "phase 2 winds up faster");
        }

        [Test]
        public void TheAlertWakesTheWholeArenaAndNothingElseOnce()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            st.Engineer.Pos = new Vec2(30.5, 30.5);
            var g = Guardian(st, 24.5, 30.5);
            var farInArena = RaidFixture.Guard(st, "skitter", 60.5, 120.5, 20);
            farInArena.Site = Arena;
            farInArena.Group = 7;                         // another squad, far out of any alert radius
            var campBody = RaidFixture.Guard(st, "skitter", 26.5, 30.5, 20);
            campBody.Site = "freight:camp:1";
            var raider = RaidFixture.Body(st, "skitter", 27.5, 30.5);

            Enemies.Damage(ctx, st, g.Id, 299);
            Assert.That(farInArena.OnPlayer, Is.False, "a hit above 300 wakes nobody else");
            Assert.That(RaidFixture.Count<GuardianEnragedEvent>(st), Is.EqualTo(0));

            Enemies.Damage(ctx, st, g.Id, 1);
            Assert.That(farInArena.OnPlayer, Is.True, "the far side of the arena wakes");
            Assert.That(farInArena.LastKnown, Is.EqualTo(st.Engineer.Pos), "and knows where the engineer is");
            Assert.That(farInArena.LastKnownUntil, Is.EqualTo(st.T + ctx.Data.Siege.MemoryS));
            Assert.That(g.OnPlayer, Is.True, "the guardian too");
            Assert.That(campBody.OnPlayer, Is.False, "a body of another camp does not, though it stands beside it");
            Assert.That(raider.OnPlayer, Is.False, "nor a raider");
            var ev = RaidFixture.Last<GuardianEnragedEvent>(st);
            Assert.That((ev.EnemyId, ev.Stronghold), Is.EqualTo((g.Id, "freight")));

            farInArena.OnPlayer = false;
            Enemies.Damage(ctx, st, g.Id, 100);
            Assert.That(RaidFixture.Count<GuardianEnragedEvent>(st), Is.EqualTo(1), "phase 2 begins once");
            Assert.That(farInArena.OnPlayer, Is.False);
        }

        [Test]
        public void ThePowerCoreDropsWhereTheGuardianDies()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            st.Engineer.Pos = new Vec2(10.5, 10.5);
            var g = Guardian(st, 44.25, 51.75);
            Assert.That(Enemies.Damage(ctx, st, g.Id, 600), Is.True);
            Assert.That(st.Enemies.Find(g.Id), Is.Null);
            Assert.That(RaidFixture.Count<GuardianEnragedEvent>(st), Is.EqualTo(0), "a single killing blow skips phase 2");
            var ev = RaidFixture.Last<GuardianKilledEvent>(st);
            Assert.That((ev.Stronghold, ev.X, ev.Y), Is.EqualTo(("freight", 44.25, 51.75)));
            Assert.That(st.Encounters.Felled, Is.EqualTo(new[] { "freight" }));
            Assert.That(st.Encounters.HasFallen("freight"), Is.True);
            Assert.That(st.Encounters.Cores.Count, Is.EqualTo(1));
            Assert.That(st.Encounters.Cores[0].Stronghold, Is.EqualTo("freight"));
            Assert.That(st.Encounters.Cores[0].Pos, Is.EqualTo(new Vec2(44.25, 51.75)));

            // A guardian that guards no arena (an Admin spawn) holds no core.
            var loose = RaidFixture.Guard(st, GuardianRules.Kind, 20.5, 20.5, 600);
            Enemies.Damage(ctx, st, loose.Id, 600);
            Assert.That(st.Encounters.Cores.Count, Is.EqualTo(1));
        }

        [Test]
        public void TheSaveRoundTripsTheFallenGuardianAndItsCore()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            var g = Guardian(st, 44.25, 51.75);
            Enemies.Damage(ctx, st, g.Id, 600);
            var charging = Guardian(st, 30.5, 30.5);
            Charging(st, charging, 40.5, 30.5);

            var load = SaveSerializer.ReadText(SaveSerializer.WriteText(st, ctx.Data), ctx.Data);
            Assert.That(load.Ok, Is.True, load.Reason);
            Assert.That(load.Header.Version, Is.EqualTo(SaveSchema.Version));
            var b = load.State;
            Assert.That(b.Encounters.Felled, Is.EqualTo(new[] { "freight" }));
            Assert.That(b.Encounters.Cores.Single().Pos, Is.EqualTo(new Vec2(44.25, 51.75)));
            Assert.That(b.Enemies.Find(charging.Id).Phase, Is.EqualTo(EnemyPhaseKind.Charge), "a charge resumes mid-charge");
            Assert.That(StateHash.Compute(b), Is.EqualTo(StateHash.Compute(st)));
        }

        [Test]
        public void AVersionFifteenSaveUpgradesWithNoGuardianFelled()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            var state = PersistenceFixture.Canonical(st);
            Assert.That(Regex.Matches(state, "\"felled\"").Count, Is.EqualTo(1));
            Assert.That(Regex.Matches(state, "\"cores\"").Count, Is.EqualTo(1));
            var state15 = Regex.Replace(state, "\"felled\":\\[[^\\]]*\\],?", "");
            state15 = Regex.Replace(state15, "\"cores\":\\[[^\\]]*\\],?", "");
            state15 = state15.Replace(",}", "}");
            Assert.That(state15, Does.Not.Contain("\"felled\""));
            Assert.That(state15, Does.Not.Contain("\"cores\""));

            var file = SaveSerializer.WriteText(st, ctx.Data);
            file = PersistenceFixture.Retarget(file, "state", state15);
            file = PersistenceFixture.Retarget(file, "version", "15");
            file = PersistenceFixture.Retarget(file, "hash", CanonicalJsonWriter.QuoteString(StateHash.Of(state15)));

            var old = SaveSerializer.ReadText(file, ctx.Data);
            Assert.That(old.Ok, Is.True, old.Reason);
            Assert.That(old.Header.Version, Is.EqualTo(15));
            Assert.That(old.Upgraded, Does.Contain("no guardian felled yet"));
            Assert.That(old.State.Encounters.Felled, Is.Empty);
            Assert.That(old.State.Encounters.Cores, Is.Empty);
        }
    }
}
