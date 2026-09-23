using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Relight.Sim.Tests.Combat
{
    /// <summary>
    /// L-02, ALWAYS_DARK_SPEC §5.1, §5.2 and §5.5: aliens pause at the edge of the light, prefer a dark way in,
    /// notice the engineer later while they stand in light — and light never removes the last route.
    /// </summary>
    public sealed class LightAvoidanceTests
    {
        private static List<ITickPhase> Phases() =>
            new List<ITickPhase> { new PowerPhase(), new LightPhase(), new EnemyPhase() };

        /// <summary>
        /// A map that is solid except for one three-wide street (y 78–80) running from x = 40 to the core's west
        /// face, so a body born at its far end has exactly one way in. Light cannot be routed around here.
        /// </summary>
        private static SimContext Street() =>
            RaidFixture.Context(RaidFixture.Map(new[]
            {
                new TileRect(0, 0, RaidFixture.Size, 78),
                new TileRect(0, 81, RaidFixture.Size, RaidFixture.Size - 81),
                new TileRect(0, 78, 40, 3),
                new TileRect(RaidFixture.CoreX + RaidFixture.CoreSize, 78, RaidFixture.Size, 3),
            }));

        /// <summary>A powered Lamp in the street. Its pole and generator stand inside the building to the south.</summary>
        private static void StreetLamp(SimContext ctx, SimState st, int x)
        {
            RaidFixture.Add(ctx, st, "lamp", x, 79);
            RaidFixture.Power(ctx, st, x, 83);
        }

        private static bool OnLit(SimState st, Enemy e) =>
            LightQueries.LitAt(st, (int)Math.Floor(e.Pos.X), (int)Math.Floor(e.Pos.Y));

        /// <summary>Ticks until the body first stands on lit ground, or -1.</summary>
        private static int TicksToLight(SimContext ctx, SimState st, Enemy e, int limit = 1200)
        {
            for (var i = 0; i < limit; i++)
            {
                RaidFixture.Run(ctx, st, 1, Phases());
                if (OnLit(st, e)) return i;
            }
            return -1;
        }

        [Test]
        public void AMinorRaiderPausesAtTheEdgeOfTheLightAndACommittedAssaultDoesNot()
        {
            var ctx = Street();

            var a = RaidFixture.State(ctx);
            a.Engineer.Pos = new Vec2(150, 150);
            StreetLamp(ctx, a, 58);
            var minor = RaidFixture.Body(a, "skitter", 42.5, 79.5);
            minor.Hp = 1e9;
            var slow = TicksToLight(ctx, a, minor);

            var b = RaidFixture.State(ctx);
            b.Engineer.Pos = new Vec2(150, 150);
            StreetLamp(ctx, b, 58);
            var major = RaidFixture.Body(b, "skitter", 42.5, 79.5, EnemyLayer.Major);
            major.Hp = 1e9;
            var fast = TicksToLight(ctx, b, major);

            Assert.That(fast, Is.GreaterThan(0), "the committed body must reach the light");
            Assert.That(slow, Is.GreaterThan(0), "the pause ends: the minor raider goes through too");
            Assert.That(RaidFixture.Count<LightHesitationEvent>(b), Is.Zero, "a committed assault does not hesitate");
            Assert.That(RaidFixture.Count<LightHesitationEvent>(a), Is.EqualTo(1), "one pause at one edge");
            var need = ctx.Data.Raids.LightHesitateS / RaidFixture.Dt;                       // 0.65 s = 13 ticks
            Assert.That(slow - fast, Is.InRange(need - 1, need + 2), "the pause is LightHesitateS long");

            var ev = RaidFixture.Last<LightHesitationEvent>(a);
            Assert.That(ev.EnemyId, Is.EqualTo(minor.Id));
            Assert.That(ev.Layer, Is.EqualTo(EnemyLayer.Minor));
            Assert.That(LightQueries.LitAt(a, ev.X, ev.Y), Is.True, "the event names the lit tile it baulked at");
        }

        [Test]
        public void AnApproachThatIsLitFromEndToEndIsStillARoute()
        {
            var ctx = Street();
            var st = RaidFixture.State(ctx);
            st.Engineer.Pos = new Vec2(150, 150);
            foreach (var x in new[] { 43, 50, 57, 64, 71 }) StreetLamp(ctx, st, x);
            RaidFixture.Run(ctx, st, 1, Phases());
            for (var x = 40; x < RaidFixture.CoreX; x++)
                Assert.That(LightQueries.LitAt(st, x, 79), Is.True, $"the fixture must light the whole street ({x})");

            Assert.That(DirectorRules.Target(ctx, st, out var bx, out var by, out var tw, out var th), Is.True);
            var fld = st.Director.Fields.Field(ctx, st, bx, by, tw, th, true);
            Assert.That(fld.Weighted, Is.True);
            Assert.That(fld.At(41, 79), Is.GreaterThanOrEqualTo(0), "reachable in plain steps");
            Assert.That(fld.Cost(41, 79), Is.GreaterThan(fld.At(41, 79)), "and dearer, but never closed, by light");
            Assert.That(DirectorRules.Origin(ctx, st), Is.GreaterThanOrEqualTo(0), "the director still finds an entry");

            var e = RaidFixture.Body(st, "skitter", 41.5, 79.5);
            e.Hp = 1e9;
            var start = e.Pos.X;
            RaidFixture.Run(ctx, st, 600, Phases());
            Assert.That(e.Pos.X, Is.GreaterThan(start + 20), "and the raid walks it");
        }

        [Test]
        public void TheDirectorPrefersAnEntryThatIsNotLit()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            RaidFixture.Run(ctx, st, 1, Phases());
            var first = DirectorRules.Origin(ctx, st);
            Assert.That(first, Is.GreaterThanOrEqualTo(0));
            var w = ctx.Geometry.Width;
            int fx = first % w, fy = first / w;

            RaidFixture.Add(ctx, st, "lamp", fx, fy);
            RaidFixture.Power(ctx, st, fx + 1, fy + 2);
            RaidFixture.Run(ctx, st, 1, Phases());
            Assert.That(LightQueries.LitAt(st, fx, fy), Is.True);

            var second = DirectorRules.Origin(ctx, st);
            Assert.That(second, Is.GreaterThanOrEqualTo(0));
            Assert.That(second, Is.Not.EqualTo(first));
            Assert.That(LightQueries.LitAt(st, second % w, second / w), Is.False, "the new entry is in the dark");
        }

        /// <summary>
        /// §5.2: the route field is refreshed when the mask is rebuilt. Fuelling a generator changes what is lit
        /// without touching <see cref="SimState.Rev"/>, which is all the field cache used to watch.
        /// </summary>
        [Test]
        public void TheRouteFieldIsRebuiltWhenTheLightChangesAndNothingElseDoes()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            RaidFixture.Add(ctx, st, "lamp", 60, 60);
            RaidFixture.Add(ctx, st, "pole", 60, 62);
            var gen = RaidFixture.Add(ctx, st, "generator", 62, 62);
            RaidFixture.Run(ctx, st, 2, Phases());
            Assert.That(LightQueries.LitAt(st, 58, 58), Is.False, "no fuel, no light");
            Assert.That(DirectorRules.Target(ctx, st, out var bx, out var by, out var tw, out var th), Is.True);
            var before = st.Director.Fields.Field(ctx, st, bx, by, tw, th, true);
            Assert.That(before.LightPenalty(58, 58), Is.Zero);

            var rev = st.Rev;
            gen.Inv.Add(ItemId.Coal, 50);
            RaidFixture.Run(ctx, st, 3, Phases());
            Assert.That(st.Rev, Is.EqualTo(rev), "the fixture must not move Rev");
            Assert.That(LightQueries.LitAt(st, 58, 58), Is.True);

            var after = st.Director.Fields.Field(ctx, st, bx, by, tw, th, true);
            Assert.That(after, Is.Not.SameAs(before));
            Assert.That(after.Weighted, Is.True);
            Assert.That(after.LightPenalty(58, 58), Is.GreaterThan(0), "the way out of the lamp's disc now costs more");
            Assert.That(after.At(58, 58), Is.EqualTo(before.At(58, 58)), "plain steps are untouched");
        }

        [Test]
        public void ABodyStandingInLightNoticesTheEngineerLater()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            RaidFixture.Add(ctx, st, "lamp", 30, 30);
            RaidFixture.Power(ctx, st, 30, 33);
            RaidFixture.Run(ctx, st, 1, new List<ITickPhase> { new PowerPhase(), new LightPhase() });
            Assert.That(LightQueries.LitAt(st, 30, 28), Is.True);
            Assert.That(LightQueries.LitAt(st, 30, 20), Is.False);
            const double notice = 8, escape = 12;                         // lit: 8 × 0.6 = 4.8

            var lit = RaidFixture.Guard(st, "skitter", 30.5, 28.5);
            st.Engineer.Pos = new Vec2(30.5, 22.5);                       // 6 away, in the dark to the north
            Enemies.ObservePlayer(ctx, st, lit, notice, escape);
            Assert.That(lit.OnPlayer, Is.False, "6 tiles is inside 8 but outside 4.8");

            st.Engineer.Pos = new Vec2(30.5, 24.5);                       // 4 away
            Enemies.ObservePlayer(ctx, st, lit, notice, escape);
            Assert.That(lit.OnPlayer, Is.True);

            st.Engineer.Pos = new Vec2(30.5, 18.5);                       // 10 away: aware, so the full escape range holds
            Enemies.ObservePlayer(ctx, st, lit, notice, escape);
            Assert.That(lit.OnPlayer, Is.True, "stepping into light never makes a chase let go");

            var dark = RaidFixture.Guard(st, "skitter", 30.5, 20.5);
            st.Engineer.Pos = new Vec2(30.5, 14.5);                       // 6 from the unlit body
            Enemies.ObservePlayer(ctx, st, dark, notice, escape);
            Assert.That(dark.OnPlayer, Is.True, "an unlit body keeps the full notice range");

            var assault = RaidFixture.Body(st, "skitter", 30.5, 28.5, EnemyLayer.Major, 5);
            st.Engineer.Pos = new Vec2(30.5, 22.5);
            Enemies.ObservePlayer(ctx, st, assault, notice, escape);
            Assert.That(assault.OnPlayer, Is.True, "a committed assault is not dazzled");
        }

        [Test]
        public void ACampPatrolBaulksAtALampBesideItsBeat()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            RaidFixture.Add(ctx, st, "lamp", 35, 30);                    // lights x 31..39 on the guard's row
            RaidFixture.Power(ctx, st, 36, 33);
            var g = RaidFixture.Guard(st, "skitter", 30.5, 30.5);
            st.Engineer.Pos = new Vec2(30.5, 60.5);                      // near enough that the camp is awake, too far to be seen

            RaidFixture.Run(ctx, st, 2400, Phases());

            Assert.That(RaidFixture.Count<LightHesitationEvent>(st), Is.GreaterThan(0));
            Assert.That(RaidFixture.Last<LightHesitationEvent>(st).Layer, Is.EqualTo(EnemyLayer.Site));
            Assert.That(RaidFixture.Last<LightHesitationEvent>(st).EnemyId, Is.EqualTo(g.Id));
        }
    }
}
