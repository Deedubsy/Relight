using System.Collections.Generic;
using NUnit.Framework;
using Relight.Sim.Tests.Combat;

namespace Relight.Sim.Tests.Campaign
{
    /// <summary>
    /// L-02, ALWAYS_DARK_SPEC §5.4: the two moments that replace dawn. <see cref="DistrictLitEvent"/> fires once per
    /// connection; <see cref="EnteredLightEvent"/> fires when the engineer steps from unlit to lit ground, at most
    /// once in ten seconds. Neither replays on the first tick of a loaded game.
    /// </summary>
    public sealed class LightReliefTests
    {
        private static List<ITickPhase> Phases() =>
            new List<ITickPhase> { new PowerPhase(), new LightPhase() };

        /// <summary>A streetlight at 30,30 owned by a substation lot at 60,28 (the D55 fixture).</summary>
        private static SimContext District() =>
            new SimContext(ReferenceData.Create(), RaidFixture.Map(), null, null, new WorldSites(new List<SiteRecord>
            {
                new SiteRecord("home", "Home Court", SiteKind.Core, RaidFixture.CoreX, RaidFixture.CoreY,
                    RaidFixture.CoreSize, RaidFixture.CoreSize, "", 0),
                new SiteRecord(StreetLights.IdPrefix + "0", "Street light", SiteKind.Light, 30, 30, 1, 1),
                new SiteRecord(StreetLights.IdPrefix + "1", "Street light", SiteKind.Light, 44, 30, 1, 1),
                new SiteRecord("substation:0", "Foundry Row substation", SiteKind.Substation, 60, 28, 3, 3),
            }));

        /// <summary>A fuelled generator at 34,30 and poles east to the lot. Returns the last pole, the link to cut.</summary>
        private static Machine Connect(SimContext ctx, SimState st)
        {
            RaidFixture.Power(ctx, st, 32, 30);
            RaidFixture.Add(ctx, st, "pole", 40, 30);
            RaidFixture.Add(ctx, st, "pole", 48, 30);
            return RaidFixture.Add(ctx, st, "pole", 56, 30);
        }

        [Test]
        public void ConnectingASubstationRaisesDistrictLitOnce()
        {
            var ctx = District();
            var st = RaidFixture.State(ctx);
            RaidFixture.Run(ctx, st, 5, Phases());
            Assert.That(RaidFixture.Count<DistrictLitEvent>(st), Is.Zero);

            Connect(ctx, st);
            RaidFixture.Run(ctx, st, 200, Phases());

            Assert.That(LightQueries.LitAt(st, 30, 30), Is.True, "the fixture must light the district");
            Assert.That(RaidFixture.Count<DistrictLitEvent>(st), Is.EqualTo(1), "once per connection, not once per tick");
            var ev = RaidFixture.Last<DistrictLitEvent>(st);
            Assert.That(ev.SiteId, Is.EqualTo("substation:0"));
            Assert.That(ev.Name, Is.EqualTo("Foundry Row substation"));
            Assert.That(ev.Lights, Is.EqualTo(2));
        }

        [Test]
        public void TheSimLightsTheWholeDistrictOnTheTickTheEventFires()
        {
            // REL-117 draws the district's light arriving over a second or so. That is a picture, and this is the
            // rule it must not touch: on the very tick DistrictLitEvent fires, every tile of the district is
            // already lit in the sim's mask, so turret sight, alien hesitation and every other reader see the
            // whole district at once, exactly as they did before the sweep was drawn.
            var ctx = District();
            var st = RaidFixture.State(ctx);
            RaidFixture.Run(ctx, st, 5, Phases());
            Connect(ctx, st);

            var ticks = 0;
            while (RaidFixture.Count<DistrictLitEvent>(st) == 0 && ticks++ < 200) RaidFixture.Run(ctx, st, 1, Phases());
            Assert.That(RaidFixture.Count<DistrictLitEvent>(st), Is.EqualTo(1), "the fixture must light the district");

            Assert.That(LightQueries.LitAt(st, 30, 30), Is.True, "the near lamp, on this tick");
            Assert.That(LightQueries.LitAt(st, 44, 30), Is.True, "and the far one, on the same tick");
            var ev = RaidFixture.Last<DistrictLitEvent>(st);
            var far = System.Math.Sqrt((44.5 - ev.X) * (44.5 - ev.X) + (30.5 - ev.Y) * (30.5 - ev.Y));
            Assert.That(far, Is.GreaterThan(10), "the far lamp is far enough for a sweep to be visible at all");
        }

        [Test]
        public void SavingOrHashingTheStateDoesNotForgetWhatWasLit()
        {
            // A save and a state hash both walk LightState.Visit. Only a LOAD may reset the relief memory: if a
            // write did, the tick after every autosave would be a silent baseline and swallow a connection.
            var ctx = District();
            var st = RaidFixture.State(ctx);
            RaidFixture.Run(ctx, st, 5, Phases());
            Connect(ctx, st);
            StateHash.Compute(st);
            RaidFixture.Run(ctx, st, 5, Phases());
            Assert.That(RaidFixture.Count<DistrictLitEvent>(st), Is.EqualTo(1));

            for (var i = 0; i < 20; i++) { StateHash.Compute(st); RaidFixture.Run(ctx, st, 1, Phases()); }
            Assert.That(RaidFixture.Count<DistrictLitEvent>(st), Is.EqualTo(1), "and a hash every tick replays nothing");
        }

        [Test]
        public void LosingTheLineAndRestoringItIsASecondConnection()
        {
            var ctx = District();
            var st = RaidFixture.State(ctx);
            RaidFixture.Run(ctx, st, 1, Phases());
            var last = Connect(ctx, st);
            RaidFixture.Run(ctx, st, 5, Phases());
            Assert.That(RaidFixture.Count<DistrictLitEvent>(st), Is.EqualTo(1));

            st.Machines.Remove(last);
            st.Rev++;
            RaidFixture.Run(ctx, st, 5, Phases());
            Assert.That(LightQueries.LitAt(st, 30, 30), Is.False);
            Assert.That(RaidFixture.Count<DistrictLitEvent>(st), Is.EqualTo(1), "going dark is not a relief event");

            RaidFixture.Add(ctx, st, "pole", 56, 30);
            RaidFixture.Run(ctx, st, 5, Phases());
            Assert.That(RaidFixture.Count<DistrictLitEvent>(st), Is.EqualTo(2));
        }

        [Test]
        public void ADistrictThatIsAlreadyLitOnTheFirstTickDoesNotReplayItsSweep()
        {
            var ctx = District();
            var st = RaidFixture.State(ctx);
            Connect(ctx, st);                                  // what a loaded save looks like: lit before any tick

            RaidFixture.Run(ctx, st, 20, Phases());

            Assert.That(LightQueries.LitAt(st, 30, 30), Is.True);
            Assert.That(RaidFixture.Count<DistrictLitEvent>(st), Is.Zero);
        }

        [Test]
        public void SteppingIntoLightRaisesEnteredLightAtMostOnceInTenSeconds()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            RaidFixture.Add(ctx, st, "lamp", 20, 20);
            RaidFixture.Power(ctx, st, 20, 23);
            var dark = new Vec2(10.5, 20.5);
            var lit = new Vec2(20.5, 21.5);

            st.Engineer.Pos = dark;
            RaidFixture.Run(ctx, st, 2, Phases());
            Assert.That(RaidFixture.Count<EnteredLightEvent>(st), Is.Zero);

            st.Engineer.Pos = lit;
            RaidFixture.Run(ctx, st, 2, Phases());
            Assert.That(RaidFixture.Count<EnteredLightEvent>(st), Is.EqualTo(1));
            RaidFixture.Run(ctx, st, 20, Phases());
            Assert.That(RaidFixture.Count<EnteredLightEvent>(st), Is.EqualTo(1), "standing in light is not entering it");

            st.Engineer.Pos = dark;
            RaidFixture.Run(ctx, st, 2, Phases());
            st.Engineer.Pos = lit;
            RaidFixture.Run(ctx, st, 2, Phases());
            Assert.That(RaidFixture.Count<EnteredLightEvent>(st), Is.EqualTo(1), "a second step inside ten seconds is quiet");

            st.Engineer.Pos = dark;
            RaidFixture.Run(ctx, st, (int)(LightPhase.EnteredLightEveryS / RaidFixture.Dt), Phases());
            st.Engineer.Pos = lit;
            RaidFixture.Run(ctx, st, 2, Phases());
            Assert.That(RaidFixture.Count<EnteredLightEvent>(st), Is.EqualTo(2));
        }

        [Test]
        public void AGameThatStartsWithTheEngineerInLightRaisesNothing()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            RaidFixture.Add(ctx, st, "lamp", 20, 20);
            RaidFixture.Power(ctx, st, 20, 23);
            st.Engineer.Pos = new Vec2(20.5, 21.5);

            RaidFixture.Run(ctx, st, 40, Phases());

            Assert.That(RaidFixture.Count<EnteredLightEvent>(st), Is.Zero);
        }
    }
}
