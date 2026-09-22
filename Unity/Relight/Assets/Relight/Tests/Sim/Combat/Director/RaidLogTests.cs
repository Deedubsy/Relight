using System.Collections.Generic;
using NUnit.Framework;

namespace Relight.Sim.Tests.Combat
{
    /// <summary>
    /// REL-85 (CMB-09c, E-19): every raid leaves one log line, and each count on it is something the log saw
    /// happen. The fights are real — the combat phases run, a turret shoots, bodies die — and the log is fed each
    /// tick's events as the host feeds it each frame's.
    /// </summary>
    public sealed class RaidLogTests
    {
        private static (SimContext Ctx, SimState St, RaidLog Log) Bench()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            new HomeCoreInitializer().Init(ctx, st);
            st.Director.NextMinor = 1e9;                                // nothing scheduled: the raid is the test's
            st.Director.NextStart = 1e9;
            st.T = 100;
            return (ctx, st, new RaidLog());
        }

        /// <summary>Tick the combat phases, then hand the log the tick's events, as <c>SimHost</c> does per frame.</summary>
        private static List<SimEvent> Drive(SimContext ctx, SimState st, RaidLog log, double seconds)
        {
            var all = new List<SimEvent>();
            var ticks = (int)System.Math.Ceiling(seconds / RaidFixture.Dt);
            for (var i = 0; i < ticks; i++)
            {
                RaidFixture.Run(ctx, st, 1);
                log.Observe(ctx, st, st.Events);
                all.AddRange(st.Events);
                st.Events.Clear();
            }
            return all;
        }

        private static int Count<T>(List<SimEvent> events) where T : SimEvent
        {
            var n = 0;
            for (var i = 0; i < events.Count; i++) if (events[i] is T) n++;
            return n;
        }

        [Test]
        public void ASmallRaidATurretClearsLeavesOneLine_WithWhatTheLogSaw()
        {
            var (ctx, st, log) = Bench();
            var gun = RaidFixture.Turret(ctx, st, RaidFixture.CoreX + 12, RaidFixture.CoreY, rounds: 50);
            RaidFixture.Run(ctx, st, 1);                                // one tick of power, so the turret is ready
            st.Events.Clear();
            for (var i = 0; i < 3; i++) RaidFixture.Body(st, "skitter", gun.X + 3, gun.Y + i, EnemyLayer.Minor, 7);
            var started = st.Director.Minor.StartsAt;

            var events = Drive(ctx, st, log, 30);

            Assert.That(st.Director.Minor, Is.Null, "the turret cleared the raid within the window");
            Assert.That(Count<RaidEndedEvent>(events), Is.EqualTo(1), "one end event per raid");
            Assert.That(log.Lines.Count, Is.EqualTo(1), "one line per raid");
            Assert.That(log.Watching, Is.EqualTo(0));
            var line = log.Lines[0];
            Assert.That(line.RaidId, Is.EqualTo(7));
            Assert.That(line.Major, Is.False);
            Assert.That(line.Outcome, Is.EqualTo((int)RaidOutcome.Cleared));
            Assert.That(line.StartedAt, Is.EqualTo(started));
            Assert.That(line.EndedAt, Is.GreaterThan(started));
            Assert.That(line.Killed, Is.EqualTo(3), "the kills are the raid's own bodies");
            Assert.That(line.Spawned, Is.EqualTo(0), "placed by hand: the log saw no births");
            Assert.That(line.PeakAlive, Is.EqualTo(3));
            Assert.That(line.RoundsFired, Is.EqualTo(Count<TurretShotEvent>(events)), "every shot the turret fired");
            Assert.That(line.RoundsFired, Is.GreaterThan(0));
            Assert.That(line.TurretsDry, Is.EqualTo(0));
            Assert.That(line.CoreHpLost, Is.EqualTo(0));
            Assert.That(line.StructuresWrecked, Is.EqualTo(0));
            Assert.That(line.Shared, Is.False);
            Assert.That(line.Text, Does.StartWith("raid #7 small cleared · T " + started.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "→"));
            Assert.That(line.Text, Does.Contain(" s to clear · 0 spawned · 3 killed · peak 3 alive · "
                + line.RoundsFired + " rounds fired · 0 turrets dry · core lost 0 HP · 0 structures wrecked"));
        }

        [Test]
        public void ALargeRaidsLineCarriesItsSavedRecordsOutcome()
        {
            var (ctx, st, log) = Bench();
            var gun = RaidFixture.Turret(ctx, st, RaidFixture.CoreX + 12, RaidFixture.CoreY, rounds: 50);
            RaidFixture.Run(ctx, st, 1);
            st.Events.Clear();
            RaidFixture.Body(st, "skitter", gun.X + 3, gun.Y, EnemyLayer.Major, 9);
            var a = st.Director.Major;
            a.StartsAt = st.T;
            a.Remaining = 0;                                            // it owes the map nothing more: cleared when dead
            Drive(ctx, st, log, 1);
            Assert.That(log.Watching, Is.EqualTo(1), "a large raid is watched from its start time");

            var events = Drive(ctx, st, log, 30);

            Assert.That(st.Director.Major, Is.Null);
            var rec = st.Director.History[st.Director.History.Count - 1];
            Assert.That(rec.Id, Is.EqualTo(9));
            Assert.That(Count<RaidEndedEvent>(events), Is.EqualTo(1));
            Assert.That(log.Lines.Count, Is.EqualTo(1));
            var line = log.Lines[0];
            Assert.That(line.Major, Is.True);
            Assert.That(line.Outcome, Is.EqualTo(rec.Outcome), "the line says what the record says");
            Assert.That(line.Outcome, Is.EqualTo((int)RaidOutcome.Cleared));
            Assert.That(line.StartedAt, Is.EqualTo(rec.Started));
            Assert.That(line.EndedAt, Is.EqualTo(rec.Ended));
            Assert.That(line.Killed, Is.EqualTo(1));
            Assert.That(line.Text, Does.StartWith("raid #9 large cleared"));
        }

        [Test]
        public void ALostLargeRaidSaysLost_AndCountsTheCoreHitsThatLostIt()
        {
            var (ctx, st, log) = Bench();
            RaidFixture.Body(st, "skitter", RaidFixture.CoreX - 20, RaidFixture.CoreY, EnemyLayer.Major, 4);
            var a = st.Director.Major;
            a.StartsAt = st.T;
            Drive(ctx, st, log, 1);

            HomeCore.Damage(st, 1e9);                                   // the raid beats its target
            Drive(ctx, st, log, 1);

            Assert.That(st.Director.Major, Is.Null, "a raid that has won is over at once (E-18)");
            Assert.That(log.Lines.Count, Is.EqualTo(1));
            var line = log.Lines[0];
            Assert.That(line.Outcome, Is.EqualTo((int)RaidOutcome.Lost));
            Assert.That(line.CoreHpLost, Is.GreaterThan(0), "the hit is on the line");
            Assert.That(line.Killed, Is.EqualTo(0));
            Assert.That(line.Text, Does.StartWith("raid #4 large lost"));
            Assert.That(line.Text, Does.Contain(" s until it ended · "));
        }

        [Test]
        public void ATurretThatRunsDryInTheRaidIsCounted_OneAlreadyEmptyIsNot()
        {
            var (ctx, st, log) = Bench();
            var empty = RaidFixture.Turret(ctx, st, RaidFixture.CoreX - 12, RaidFixture.CoreY, rounds: 0);
            st.Turrets.Of(empty.Id).ShotT = 1;                          // fired long ago and never refilled
            var gun = RaidFixture.Turret(ctx, st, RaidFixture.CoreX + 12, RaidFixture.CoreY, rounds: 2);
            RaidFixture.Run(ctx, st, 1);
            st.Events.Clear();
            Assert.That(TurretAmmo.State(ctx.Data, empty), Is.EqualTo(TurretAmmoState.Dry));

            var body = RaidFixture.Body(st, "skitter", gun.X + 3, gun.Y, EnemyLayer.Minor, 5);
            body.Hp = 1e9;                                              // outlives two rounds
            var events = Drive(ctx, st, log, 20);
            Assert.That(Count<TurretShotEvent>(events), Is.EqualTo(2), "it fired what it had");
            Assert.That(TurretAmmo.State(ctx.Data, gun), Is.EqualTo(TurretAmmoState.Dry));
            Assert.That(log.Watching, Is.EqualTo(1));

            st.Enemies.Actors.Remove(body);                             // the raid ends without a kill
            Drive(ctx, st, log, 1);

            Assert.That(log.Lines.Count, Is.EqualTo(1));
            var line = log.Lines[0];
            Assert.That(line.TurretsDry, Is.EqualTo(1), "the one that ran dry in this fight, not the one that was already empty");
            Assert.That(line.RoundsFired, Is.EqualTo(2));
            Assert.That(line.Killed, Is.EqualTo(0));
            Assert.That(line.Text, Does.Contain("2 rounds fired · 1 turret dry · "));
        }

        [Test]
        public void ARaidTheSessionNeverSawOnTheGroundStillGetsItsLine()
        {
            var (ctx, st, log) = Bench();
            log.Observe(ctx, st, new List<SimEvent> { new RaidEndedEvent(st.T, 3, true, false, (int)RaidOutcome.BrokeOff, st.T - 50) });

            Assert.That(log.Lines.Count, Is.EqualTo(1));
            var line = log.Lines[0];
            Assert.That(line.RaidId, Is.EqualTo(3));
            Assert.That(line.Outcome, Is.EqualTo((int)RaidOutcome.BrokeOff));
            Assert.That(line.Seconds, Is.EqualTo(50));
            Assert.That(line.Spawned, Is.EqualTo(0));
            Assert.That(line.Text, Does.StartWith("raid #3 large broke off · T 50.0→100.0 · 50.0 s until it ended · 0 spawned"));
        }

        [Test]
        public void EachLineIsRaisedOnceAsItIsWritten()
        {
            var (ctx, st, log) = Bench();
            var raised = new List<RaidLogLine>();
            log.Written += raised.Add;
            log.Observe(ctx, st, new List<SimEvent> { new RaidEndedEvent(st.T, 1, false, true, (int)RaidOutcome.Cleared, st.T - 5) });
            log.Observe(ctx, st, new List<SimEvent> { new RaidEndedEvent(st.T, 2, false, false, (int)RaidOutcome.Cleared, st.T - 5) });

            Assert.That(raised.Count, Is.EqualTo(2));
            Assert.That(raised[0].Text, Does.StartWith("raid #1 scripted cleared"));
            Assert.That(raised[1].Text, Does.StartWith("raid #2 small cleared"));
            Assert.That(log.Lines, Is.EqualTo(raised));
        }
    }
}
