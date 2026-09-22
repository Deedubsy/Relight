using System;
using System.Collections.Generic;
using NUnit.Framework;
using Relight.Sim.Tests.Combat;
using Relight.Sim.UI;

namespace Relight.Sim.Tests.UI
{
    /// <summary>
    /// REL-74 (E-20): the raid arrow, the wave banners and the report card, held to the row's three checks: the arrow
    /// points at the announced target's raid from anywhere on the map; each wave is named once; the card never claims
    /// more than was seen. Also the two defects found while building it: the threat line printed the stale countdown
    /// through a fight, and a small raid walking away hid a large one that was coming.
    /// </summary>
    public sealed class RaidFeedbackTests
    {
        // A 1920 × 1080 panel with the HUD's side columns and strips taken off.
        private const double L = 340, T = 96, R = 1580, B = 910;

        private static (SimContext Ctx, SimState St) Bench(int seed = 3)
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx, seed);
            HomeCore.Ensure(ctx, st);
            return (ctx, st);
        }

        /// <summary>Runs the clock to the next assault's warning and returns it, with its real wave plan.</summary>
        private static MajorRaid Announce(SimContext ctx, SimState st)
        {
            st.T = st.Director.NextStart - ctx.Data.Raids.WarningS;
            RaidFixture.Run(ctx, st, 1, new List<ITickPhase> { new DirectorPhase() });
            Assert.That(st.Director.Major, Is.Not.Null, "an assault was announced");
            Assert.That(st.Director.Major.Waves, Is.GreaterThan(2), "the shipped plan has several waves");
            return st.Director.Major;
        }

        /// <summary>Puts the assault on the ground with <paramref name="waves"/> waves named.</summary>
        private static void Named(SimState st, MajorRaid a, int waves)
        {
            a.WaveAnnounced = waves;
            a.Wave = waves - 1;
            st.T = Math.Max(st.T, a.WaveStart[waves - 1]);
        }

        private static int Tile(SimContext ctx, int x, int y) => y * ctx.Geometry.Width + x;

        private static int East(SimContext ctx) => Tile(ctx, RaidFixture.CoreX + 60, RaidFixture.CoreY + 4);
        private static int South(SimContext ctx) => Tile(ctx, RaidFixture.CoreX + 4, RaidFixture.CoreY + 60);

        // ---- the arrow's geometry ---------------------------------------------------------------------------

        [Test]
        public void APointOnScreenGetsTheArrowOverItPointingDown()
        {
            var p = EdgeArrow.Place(800, 500, L, T, R, B);
            Assert.That(p.Valid && p.OnScreen, Is.True);
            Assert.That(p.X, Is.EqualTo(800));
            Assert.That(p.Y, Is.EqualTo(500 - EdgeArrow.StandOff));
            Assert.That(p.AngleDeg, Is.EqualTo(90));

            var nearTop = EdgeArrow.Place(800, T + 5, L, T, R, B);
            Assert.That(nearTop.Y, Is.EqualTo(T), "the arrow never leaves the box to stand over a point");
        }

        [TestCase(5000, 503, R, 503, 0)]
        [TestCase(-5000, 503, L, 503, 180)]
        [TestCase(960, -5000, 960, T, -90)]
        [TestCase(960, 5000, 960, B, 90)]
        public void APointOffScreenPinsTheArrowToTheNearSide(double px, double py, double x, double y, double angle)
        {
            var p = EdgeArrow.Place(px, py, L, T, R, B);
            Assert.That(p.Valid, Is.True);
            Assert.That(p.OnScreen, Is.False);
            Assert.That(p.X, Is.EqualTo(x).Within(1e-9));
            Assert.That(p.Y, Is.EqualTo(y).Within(1e-9));
            Assert.That(p.AngleDeg, Is.EqualTo(angle).Within(1e-9));
        }

        /// <summary>
        /// "From anywhere on the map": points all the way round the screen, near and very far, each put the arrow
        /// on the box's edge and aim it at the point.
        /// </summary>
        [Test]
        public void FromAnywhereTheArrowSitsOnTheEdgeAndAimsAtThePoint()
        {
            var cx = (L + R) / 2;
            var cy = (T + B) / 2;
            foreach (var reach in new[] { 1000.0, 1e5, 1e8 })
                for (var deg = 0; deg < 360; deg += 5)
                {
                    var r = deg * Math.PI / 180;
                    var px = cx + Math.Cos(r) * reach;
                    var py = cy + Math.Sin(r) * reach;
                    var p = EdgeArrow.Place(px, py, L, T, R, B);
                    Assert.That(p.Valid && !p.OnScreen, Is.True, deg + "°");
                    var onEdge = Math.Abs(p.X - L) < 1e-6 || Math.Abs(p.X - R) < 1e-6
                                 || Math.Abs(p.Y - T) < 1e-6 || Math.Abs(p.Y - B) < 1e-6;
                    Assert.That(onEdge, Is.True, deg + "° sits on the edge");
                    Assert.That(p.X >= L - 1e-6 && p.X <= R + 1e-6 && p.Y >= T - 1e-6 && p.Y <= B + 1e-6, Is.True);
                    var aim = Math.Atan2(py - p.Y, px - p.X) * 180 / Math.PI;
                    var off = Math.Abs(((aim - p.AngleDeg) % 360 + 540) % 360 - 180);
                    Assert.That(off, Is.LessThan(1e-6), deg + "° aims at the point");
                }
        }

        [Test]
        public void ThereIsNoArrowForAPointOrABoxThatCannotBePlaced()
        {
            Assert.That(EdgeArrow.Place(double.NaN, 5, L, T, R, B).Valid, Is.False);
            Assert.That(EdgeArrow.Place(5, double.PositiveInfinity, L, T, R, B).Valid, Is.False);
            Assert.That(EdgeArrow.Place(5, 5, 400, 100, 400, 800).Valid, Is.False, "a box with no width");
        }

        // ---- the arrow's target ---------------------------------------------------------------------------

        [Test]
        public void TheArrowPointsAtTheWarnedApproachThenAtTheFrontOfTheRaid()
        {
            var (ctx, st) = Bench();
            var a = Announce(ctx, st);
            var vm = new HudViewModel();
            vm.Refresh(ctx, st, 0, false, false, force: true);

            var w = ctx.Geometry.Width;
            Assert.That(vm.RaidPointerVisible, Is.True, "up from the warning");
            Assert.That(vm.RaidPointerX, Is.EqualTo(a.Origin % w + .5));
            Assert.That(vm.RaidPointerY, Is.EqualTo(a.Origin / w + .5));
            Assert.That(vm.RaidPointerLabel, Does.StartWith("Major assault · ").And.EndWith(" s"));

            Named(st, a, 1);
            RaidFixture.Body(st, "skitter", 120, 80, EnemyLayer.Major, a.Id);
            RaidFixture.Body(st, "skitter", 95, 80, EnemyLayer.Major, a.Id);     // nearer the core: the front
            var gone = RaidFixture.Body(st, "skitter", 88, 80, EnemyLayer.Major, a.Id);
            gone.Withdrawing = true;                                               // walking home does not count
            RaidFixture.Body(st, "skitter", 84, 80, EnemyLayer.Site, 99);          // a camp resident is not the raid
            vm.Refresh(ctx, st, 1, false, false, force: true);
            Assert.That(vm.RaidPointerVisible, Is.True);
            Assert.That(vm.RaidPointerX, Is.EqualTo(95));
            Assert.That(vm.RaidPointerY, Is.EqualTo(80));
            Assert.That(vm.RaidPointerLabel, Is.EqualTo("Wave 1 of " + a.Waves));

            a.Retreat = true;
            vm.Refresh(ctx, st, 2, false, false, force: true);
            Assert.That(vm.RaidPointerVisible, Is.False, "no arrow for a raid that is only walking away");
        }

        [Test]
        public void ASmallRaidWalkingAwayDoesNotHideALargeOneThatIsComing()
        {
            var (ctx, st) = Bench();
            var a = Announce(ctx, st);
            st.Director.Minor = new MinorRaid { Id = 999, Origin = South(ctx), Spawned = true, Retreat = true, StartsAt = st.T };

            var wv = DirectorQueries.Warning(ctx, st);
            Assert.That(wv.Kind, Is.EqualTo("warning"));
            Assert.That(wv.Label, Is.EqualTo("Major assault"));
            Assert.That(wv.RaidId, Is.EqualTo(a.Id));

            st.Director.Major = null;
            Assert.That(DirectorQueries.Warning(ctx, st).Kind, Is.EqualTo("withdrawal"), "alone, it is still reported");
        }

        // ---- the threat line ------------------------------------------------------------------------------

        [Test]
        public void TheThreatLineDescribesAFightFromStateNotFromTheCountdown()
        {
            var (ctx, st) = Bench();
            var a = Announce(ctx, st);
            Assert.That(st.Director.Notice, Does.Contain("inbound"), "the director's last words are the countdown");

            Named(st, a, 1);
            var line = HudViewModel.ThreatLine(ctx, st, out var urgent);
            Assert.That(urgent, Is.True);
            Assert.That(line, Is.EqualTo("Major assault · wave 1 of " + a.Waves + " · from the "
                                         + DirectorRules.HeadingsOf(ctx, st, SiegePlan.Sides(a, 0))));

            Named(st, a, 3);
            Assert.That(HudViewModel.ThreatLine(ctx, st, out _), Does.StartWith("Major assault · wave 3 of "));

            st.Director.Major = null;
            st.Director.Minor = new MinorRaid { Id = 50, Origin = East(ctx), Spawned = true, StartsAt = st.T };
            st.Director.Notice = "Raid inbound from E in 30 s.";
            line = HudViewModel.ThreatLine(ctx, st, out urgent);
            Assert.That(urgent, Is.True);
            Assert.That(line, Is.EqualTo("Raid attacking · from the E"));
        }

        [Test]
        public void AfterARaidTheLineDoesNotSayTheBaseIsUnderAttack()
        {
            var (ctx, st) = Bench();
            st.Director.Major = null;
            st.Director.Minor = null;
            st.Director.RecoveryUntil = st.T + 60;
            st.Director.Notice = "";
            Assert.That(DirectorQueries.Warning(ctx, st).Kind, Is.EqualTo("recovery"));
            Assert.That(HudViewModel.ThreatLine(ctx, st, out var urgent), Is.Empty, "the director's last word has expired");
            Assert.That(urgent, Is.False);

            st.Director.Notice = "Major assault repelled.";
            Assert.That(HudViewModel.ThreatLine(ctx, st, out urgent), Is.EqualTo("Major assault repelled."));
            Assert.That(urgent, Is.False);

            st.Director.Notice = "";
            st.Director.Minor = new MinorRaid { Id = 51, Origin = East(ctx), Spawned = true, Retreat = true, StartsAt = st.T };
            Assert.That(DirectorQueries.Warning(ctx, st).Kind, Is.EqualTo("withdrawal"));
            Assert.That(HudViewModel.ThreatLine(ctx, st, out urgent), Is.EqualTo("Raid withdrawing"));
            Assert.That(urgent, Is.False);
        }

        // ---- the banners ----------------------------------------------------------------------------------

        [Test]
        public void EachWaveIsNamedOnce()
        {
            var (ctx, st) = Bench();
            var a = Announce(ctx, st);
            var b = new RaidBannerSource();
            b.Refresh(ctx, st);
            Assert.That(b.Serial, Is.Zero, "a countdown is not a wave");

            Named(st, a, 1);
            b.Refresh(ctx, st);
            Assert.That(b.Serial, Is.EqualTo(1));
            Assert.That(b.Text, Is.EqualTo("Wave 1 of " + a.Waves + ", from the "
                                           + DirectorQueries.SideWords(ctx, st, SiegePlan.Sides(a, 0))));
            for (var i = 0; i < 5; i++) b.Refresh(ctx, st);
            Assert.That(b.Serial, Is.EqualTo(1), "named once");

            for (var w = 2; w <= a.Waves; w++)
            {
                Named(st, a, w);
                b.Refresh(ctx, st);
                b.Refresh(ctx, st);
                Assert.That(b.Serial, Is.EqualTo(w), "wave " + w);
                Assert.That(b.Text, Does.StartWith("Wave " + w + " of " + a.Waves + ", from the "));
            }
        }

        [Test]
        public void AGameLoadedMidAssaultDoesNotReplayTheWavesItMissed()
        {
            var (ctx, st) = Bench();
            var a = Announce(ctx, st);
            Named(st, a, 2);
            st.T = a.WaveStart[1] + 30;
            var b = new RaidBannerSource();                                 // a fresh session, as after a load
            b.Refresh(ctx, st);
            Assert.That(b.Serial, Is.Zero, "no catch-up burst");

            Named(st, a, 3);
            b.Refresh(ctx, st);
            Assert.That(b.Serial, Is.EqualTo(1));
            Assert.That(b.Text, Does.StartWith("Wave 3 of "));
        }

        [Test]
        public void ASmallRaidGetsOneBannerAndTheScriptedOpeningNone()
        {
            var (ctx, st) = Bench();
            var b = new RaidBannerSource();
            st.T = 200;
            var m = new MinorRaid { Id = 40, Origin = East(ctx), StartsAt = st.T + 30 };
            st.Director.Minor = m;
            b.Refresh(ctx, st);
            Assert.That(b.Serial, Is.Zero);

            st.T = m.StartsAt + 5;                                          // staged late, but seen waiting
            m.Spawned = true;
            b.Refresh(ctx, st);
            b.Refresh(ctx, st);
            Assert.That(b.Serial, Is.EqualTo(1));
            Assert.That(b.Text, Is.EqualTo("Raid, from the east"));

            st.Director.Minor = new MinorRaid { Id = 41, Origin = East(ctx), Spawned = true, Scripted = true, StartsAt = st.T };
            b.Refresh(ctx, st);
            Assert.That(b.Serial, Is.EqualTo(1), "the goal card owns the opening encounter");
        }

        [Test]
        public void AnAssaultTurningBackIsNotBannered()
        {
            var (ctx, st) = Bench();
            var a = Announce(ctx, st);
            var b = new RaidBannerSource();
            b.Refresh(ctx, st);
            Named(st, a, 1);
            b.Refresh(ctx, st);
            a.Retreat = true;
            Named(st, a, 2);
            b.Refresh(ctx, st);
            Assert.That(b.Serial, Is.EqualTo(1));
        }

        [Test]
        public void SidesAreSpelledOutFromTheTarget()
        {
            var (ctx, st) = Bench();
            Assert.That(DirectorQueries.SideWords(ctx, st, new[] { East(ctx) }), Is.EqualTo("east"));
            Assert.That(DirectorQueries.SideWords(ctx, st, new[] { East(ctx), South(ctx), East(ctx) }),
                Is.EqualTo("east and south"));
            Assert.That(DirectorRules.HeadingsOf(ctx, st, new[] { East(ctx), South(ctx) }), Is.EqualTo("E and S"),
                "the same sides the director's own notice names");
            Assert.That(DirectorQueries.SideWords(ctx, st, Array.Empty<int>()), Is.EqualTo(""));
        }

        // ---- the report card ------------------------------------------------------------------------------

        private static void Dry(SimState st, Machine t)
        {
            t.Rounds = 0;
            st.Turrets.Of(t.Id).ShotT = st.T;
        }

        private static string[] Labels(RaidAccountSource acc)
        {
            var l = new string[acc.CardRows.Count];
            for (var i = 0; i < l.Length; i++) l[i] = acc.CardRows[i].Label;
            return l;
        }

        private static string Value(RaidAccountSource acc, string label)
        {
            for (var i = 0; i < acc.CardRows.Count; i++)
                if (acc.CardRows[i].Label == label) return acc.CardRows[i].Value;
            return null;
        }

        [Test]
        public void TheCardNamesTheSideAndTheWaveATurretRanDryIn()
        {
            var (ctx, st) = Bench();
            var east = RaidFixture.Turret(ctx, st, 100, 79);
            var a = Announce(ctx, st);
            var acc = new RaidAccountSource();
            acc.Refresh(ctx, st);                                           // seen waiting
            Named(st, a, 1);
            acc.Refresh(ctx, st);
            Assert.That(acc.Watching, Is.True);

            Named(st, a, 3);
            Dry(st, east);
            acc.Refresh(ctx, st);
            st.Director.Major = null;
            st.T += 20;
            acc.Refresh(ctx, st);

            Assert.That(acc.Serial, Is.EqualTo(1));
            Assert.That(acc.CardTitle, Is.EqualTo("Major assault over"), "a loss is never called repelled");
            Assert.That(acc.CardLesson, Is.EqualTo("The east turret ran dry in wave 3"));
            Assert.That(Labels(acc), Is.EqualTo(new[]
                { "Aliens killed", "Rounds fired", "Core", "Turrets ran dry", "Waves", "Lasted" }));
            Assert.That(Value(acc, "Turrets ran dry"), Is.EqualTo("1"));
            Assert.That(Value(acc, "Core"), Is.EqualTo("no damage"));
            Assert.That(Value(acc, "Waves"), Is.EqualTo("3 of " + a.Waves), "the waves this session saw, not the plan");
        }

        [Test]
        public void SeveralSidesAndWavesAreSaidAsTheyWereSeen()
        {
            var (ctx, st) = Bench();
            var east1 = RaidFixture.Turret(ctx, st, 100, 79);
            var east2 = RaidFixture.Turret(ctx, st, 110, 82);
            var south = RaidFixture.Turret(ctx, st, 79, 100);
            var a = Announce(ctx, st);
            var acc = new RaidAccountSource();
            acc.Refresh(ctx, st);
            Named(st, a, 1);
            acc.Refresh(ctx, st);

            Named(st, a, 2);
            Dry(st, east1);
            Dry(st, east2);
            acc.Refresh(ctx, st);
            Named(st, a, 3);
            Dry(st, south);
            acc.Intake(new PowerOutageEvent(st.T, south.Id));               // worse things first: dry outranks power
            acc.Refresh(ctx, st);
            st.Director.Major = null;
            acc.Refresh(ctx, st);

            Assert.That(acc.CardLesson, Is.EqualTo("East and south turrets ran dry, the first in wave 2"));
            Assert.That(Value(acc, "Turrets ran dry"), Is.EqualTo("3"));
            Assert.That(Value(acc, "Power"), Is.EqualTo("failed"));
            Assert.That(acc.CardRows.Count, Is.LessThanOrEqualTo(RaidAccountSource.MaxCardRows),
                "Hud.uxml authors MaxCardRows rows");
        }

        [Test]
        public void TwoTurretsOnOneSideInOneWaveAreOneLine()
        {
            var (ctx, st) = Bench();
            var east1 = RaidFixture.Turret(ctx, st, 100, 79);
            var east2 = RaidFixture.Turret(ctx, st, 110, 82);
            var a = Announce(ctx, st);
            var acc = new RaidAccountSource();
            acc.Refresh(ctx, st);
            Named(st, a, 1);
            acc.Refresh(ctx, st);
            Named(st, a, 3);
            Dry(st, east1);
            Dry(st, east2);
            acc.Refresh(ctx, st);
            st.Director.Major = null;
            acc.Refresh(ctx, st);
            Assert.That(acc.CardLesson, Is.EqualTo("East turrets ran dry in wave 3"));
        }

        [Test]
        public void PowerIsTheLessonWhenNothingRanDry()
        {
            var (ctx, st) = Bench();
            var gun = RaidFixture.Turret(ctx, st, 100, 79);
            var a = Announce(ctx, st);
            var acc = new RaidAccountSource();
            acc.Refresh(ctx, st);
            Named(st, a, 1);
            acc.Refresh(ctx, st);
            Named(st, a, 2);
            acc.Intake(new PowerOutageEvent(st.T, gun.Id));
            st.Director.Major = null;
            acc.Refresh(ctx, st);
            Assert.That(acc.CardLesson, Is.EqualTo("Power failed in wave 2"));
        }

        [Test]
        public void ASmallRaidThatCostNothingHasAPlainCard()
        {
            var (ctx, st) = Bench();
            st.T = 100;
            var gun = RaidFixture.Turret(ctx, st, 100, 79);
            st.Director.Minor = new MinorRaid { Id = 1, Origin = East(ctx), Spawned = true, StartsAt = st.T };
            var acc = new RaidAccountSource();
            acc.Refresh(ctx, st);
            acc.Intake(new EnemyKilledEvent(st.T, 1, "drone", 0, 0, true, EnemyLayer.Minor, 1));
            for (var i = 0; i < 7; i++) acc.Intake(new TurretShotEvent(st.T, gun.Id, 0, 0, 0, true));
            st.T += 45;
            st.Director.Minor = null;
            acc.Refresh(ctx, st);

            Assert.That(acc.CardTitle, Is.EqualTo("Raid repelled"));
            Assert.That(acc.CardLesson, Is.EqualTo("Nothing lost"));
            Assert.That(Labels(acc), Is.EqualTo(new[] { "Aliens killed", "Rounds fired", "Core", "Lasted" }),
                "no loss row with a zero count, and no waves for a small raid");
            Assert.That(Value(acc, "Aliens killed"), Is.EqualTo("1"));
            Assert.That(Value(acc, "Rounds fired"), Is.EqualTo("7"));
            Assert.That(Value(acc, "Lasted"), Is.EqualTo("45 s"));
            foreach (var row in acc.CardRows) Assert.That(row.Bad, Is.False, row.Label);
        }

        [Test]
        public void TheCardsCoreRowSaysWhatHappenedToTheCore()
        {
            var (ctx, st) = Bench();
            st.T = 100;
            st.Director.Minor = new MinorRaid { Id = 1, Origin = East(ctx), Spawned = true, StartsAt = st.T };
            var acc = new RaidAccountSource();
            acc.Refresh(ctx, st);
            acc.Intake(new CoreDamagedEvent(st.T, HomeQueries.CoreHp(st) - 35, 35));
            st.Director.Minor = null;
            acc.Refresh(ctx, st);
            Assert.That(Value(acc, "Core"), Is.EqualTo("lost 35 HP"));
            Assert.That(acc.CardLesson, Is.EqualTo("They reached the core"));
            Assert.That(acc.CardTitle, Is.EqualTo("Raid over"));
        }

        [TestCase(0, "0 s")]
        [TestCase(45.4, "45 s")]
        [TestCase(130, "2 min 10 s")]
        [TestCase(180, "3 min")]
        public void DurationsAreWholeSecondsAndMinutes(double s, string text) =>
            Assert.That(RaidAccountSource.Duration(s), Is.EqualTo(text));

        // ---- the HUD's card and banner timing -------------------------------------------------------------

        [Test]
        public void TheCardClosesWhenTheNextRaidArrivesOrWhenDismissed()
        {
            var (ctx, st) = Bench();
            st.T = 100;
            var vm = new HudViewModel();
            st.Director.Minor = new MinorRaid { Id = 1, Origin = East(ctx), Spawned = true, StartsAt = st.T };
            vm.Refresh(ctx, st, 0, false, false, force: true);
            st.Director.Minor = null;
            vm.Refresh(ctx, st, 1, false, false, force: true);
            Assert.That(vm.ReportVisible, Is.True);

            st.T += 60;
            st.Director.Minor = new MinorRaid { Id = 2, Origin = East(ctx), Spawned = true, StartsAt = st.T };
            vm.Refresh(ctx, st, 2, false, false, force: true);
            Assert.That(vm.ReportVisible, Is.False, "the next raid on the ground puts the card away");

            st.Director.Minor = null;
            vm.Refresh(ctx, st, 3, false, false, force: true);
            Assert.That(vm.ReportVisible, Is.True, "that raid's own card");
            vm.DismissReport();
            Assert.That(vm.ReportVisible, Is.False);
            vm.Refresh(ctx, st, 4, false, false, force: true);
            Assert.That(vm.ReportVisible, Is.False, "a dismissed card stays closed");
        }

        [Test]
        public void TheCardStepsAsideWhileADrawerIsOpenAndItsTimeWaits()
        {
            var (ctx, st) = Bench();
            st.T = 100;
            var vm = new HudViewModel();
            st.Director.Minor = new MinorRaid { Id = 1, Origin = East(ctx), Spawned = true, StartsAt = st.T };
            vm.Refresh(ctx, st, 0, false, false, force: true);
            st.Director.Minor = null;
            vm.Refresh(ctx, st, 1, false, false, force: true);
            Assert.That(vm.ReportVisible, Is.True);

            var t = 1.0;
            for (var i = 0; i < 100; i++)                                   // 30 s with the Backpack open
            {
                t += 0.3;
                vm.Refresh(ctx, st, t, false, true);
                Assert.That(vm.ReportVisible, Is.False, "the card is not drawn under a drawer");
            }
            vm.Refresh(ctx, st, t + 0.3, false, false);
            Assert.That(vm.ReportVisible, Is.True, "the seconds a drawer was open did not count");
            vm.Refresh(ctx, st, t + RaidAccountSource.Seconds + 0.5, false, false);
            Assert.That(vm.ReportVisible, Is.False);
        }

        [Test]
        public void TheBannerShowsForItsTimeAndThenGoes()
        {
            var (ctx, st) = Bench();
            var a = Announce(ctx, st);
            var vm = new HudViewModel();
            vm.Refresh(ctx, st, 0, false, false, force: true);
            Assert.That(vm.BannerVisible, Is.False);
            Named(st, a, 1);
            vm.Refresh(ctx, st, 1, false, false, force: true);
            Assert.That(vm.BannerVisible, Is.True);
            Assert.That(vm.Banner, Does.StartWith("Wave 1 of "));
            vm.Refresh(ctx, st, 1 + RaidBannerSource.Seconds + 0.1, false, false, force: true);
            Assert.That(vm.BannerVisible, Is.False);
        }
    }
}
