using NUnit.Framework;
using Relight.Sim.Tests.Combat;
using Relight.Sim.Tests.Support;

namespace Relight.Sim.Tests.Campaign
{
    /// <summary>
    /// REL-9 (INT-05), "Drawing the screen can change what the sim decides". Since L-02 the lit mask decides turret
    /// targets, perception, hesitation and raid routes, and it used to be rebuilt by whoever asked for it first: a
    /// presenter drawing a frame between ticks rebuilt it after a command, so a rendered game's combat slot read the
    /// new light one tick before a headless run's did. The issue's outcome: "presentation reads the mask and never
    /// builds it; replay with and without a presenter hashes the same."
    ///
    /// The scenario is the brownout test's layout (<see cref="TurretDarkSightTests"/>), run through the real
    /// <see cref="Simulation"/> and its full tick composition. The Gun turret stands at 30,28 (centre 31,29) on its
    /// own fuelled generator. A Lamp at 30,40 hangs off poles at 32,40 and 38,40 and a generator at 40,40 that
    /// starts EMPTY; the nearest of those poles is 10 tiles from the turret's, so the two circuits never join. A
    /// camp alien stands 4 tiles north of the lamp, 7.52 from the turret's centre: past its dark sight (6), inside
    /// its full range (9), so the turret can take it only once the lamp is lit. The engineer stands at the lamp's
    /// generator, 10.8 tiles from the alien, outside its notice radius, and fuels it by hand with the same
    /// <see cref="MachineTransferCommand"/> the Backpack panel sends.
    /// </summary>
    public sealed class LightReplayTests
    {
        private const int Seed = 7;
        private const int FuelAt = 5;
        private const int Ticks = 80;

        private sealed class Game
        {
            public Simulation Sim;
            public Machine Turret;
            public Machine Generator;
            public Enemy Alien;
        }

        private static Game Build()
        {
            var ctx = RaidFixture.Context();
            var sim = Simulation.NewGame(ctx, Seed);
            var st = sim.State;
            var turret = RaidFixture.Turret(ctx, st, 30, 28);
            RaidFixture.Add(ctx, st, "lamp", 30, 40);
            RaidFixture.Add(ctx, st, "pole", 32, 40);
            RaidFixture.Add(ctx, st, "pole", 38, 40);
            var generator = RaidFixture.Add(ctx, st, "generator", 40, 40);
            var alien = RaidFixture.Guard(st, "biter", 30.5, 36.5);
            st.Engineer.Pos = new Vec2(generator.X + 0.5, generator.Y + 0.5);
            st.Engineer.Inv[ItemId.Coal] = 10;
            st.Stats.Made.Add(ItemId.Coal, 10);
            return new Game { Sim = sim, Turret = turret, Generator = generator, Alien = alien };
        }

        /// <summary>
        /// A presenter's frame, at its worst: before REL-9 <c>LightingPresenter</c> asked for the mask through the
        /// overload that rebuilt it, which is this <see cref="LightPhase.Ensure(SimContext,SimState)"/> call, then
        /// read it. Presentation no longer reaches Ensure at all; the call stays here so the test also proves that a
        /// rebuild between ticks, by anyone, cannot change what the next tick decides.
        /// </summary>
        private static void Draw(Simulation sim)
        {
            LightPhase.Ensure(sim.Context, sim.State);
            LightQueries.Mask(sim.State);
            LightQueries.MaskSize(sim.State);
            LightQueries.Builds(sim.State);
            LightQueries.LitAt(sim.State, 30, 36);
        }

        private static string Diff(Simulation a, Simulation b) =>
            SimTestUtil.FirstDifference(SimTestUtil.Canonical(a.State), SimTestUtil.Canonical(b.State));

        /// <summary>
        /// The issue's first step: "same commands, one run calling LightQueries.Mask between ticks, compare hashes".
        /// Compared after every tick, so the first tick the two runs disagree on is the one reported.
        /// </summary>
        [Test]
        public void ReadingTheLightBetweenTicksChangesNothingTheSimDecides()
        {
            var headless = Build();
            var drawn = Build();
            Assert.That(drawn.Sim.Hash(), Is.EqualTo(headless.Sim.Hash()), "the two games start identical");

            for (var tick = 0; tick < Ticks; tick++)
            {
                if (tick == FuelAt)
                {
                    var fuel = new MachineTransferCommand(headless.Generator.Id, ItemId.Coal, 10, true);
                    var a = headless.Sim.Apply(fuel);
                    var b = drawn.Sim.Apply(fuel);
                    Assert.That(a.Accepted, Is.True, a.Problem);
                    Assert.That(b.Accepted, Is.True, b.Problem);
                }
                Draw(drawn.Sim);
                headless.Sim.Tick();
                drawn.Sim.Tick();
                Assert.That(drawn.Sim.Hash(), Is.EqualTo(headless.Sim.Hash()),
                    "tick " + tick + ": a presenter reading the light changed the game\n" + Diff(headless.Sim, drawn.Sim));
            }

            // The scenario must actually hinge on the light, or equal hashes prove nothing.
            var st = headless.Sim.State;
            Assert.That(LightQueries.LitAt(st, 30, 36), Is.True, "the fuelled lamp lights the alien's tile");
            Assert.That(RaidFixture.Count<TurretShotEvent>(st), Is.GreaterThan(0), "the turret took the lit alien");
            Assert.That(headless.Turret.Rounds, Is.LessThan(50));
        }

        /// <summary>
        /// The same thing from the other side: before the fuel, the lamp is dark and the alien stands past the
        /// turret's dark sight, so the turret holds its fire however often the light is read.
        /// </summary>
        [Test]
        public void BeforeTheFuelTheTurretCannotSeeTheAlien()
        {
            var g = Build();
            for (var tick = 0; tick < FuelAt; tick++) { Draw(g.Sim); g.Sim.Tick(); }
            Assert.That(LightQueries.LitAt(g.Sim.State, 30, 36), Is.False);
            Assert.That(RaidFixture.Count<TurretShotEvent>(g.Sim.State), Is.Zero);
            Assert.That(g.Turret.Rounds, Is.EqualTo(50));
        }

        /// <summary>
        /// The issue's third step: "a save → load → hash test with a lit target at the edge of reach". The mask is
        /// never saved (<see cref="LightState.Visit"/>). A game loaded from disk must reach the same state as the
        /// game that was saved, tick for tick, with the turret still holding the lit alien at 8.51 tiles; a load
        /// that came back dark would drop it for a tick.
        /// </summary>
        [Test]
        public void ASavedGameWithALitTargetAtTheEdgeOfReachLoadsAndRunsTheSame()
        {
            var ctx = RaidFixture.Context();
            var sim = Simulation.NewGame(ctx, Seed);
            var st = sim.State;
            var turret = RaidFixture.Turret(ctx, st, 40, 40);                     // centre 41,41; range 9, dark 6
            RaidFixture.Add(ctx, st, "lamp", 49, 44);                            // lights tile 49,41
            RaidFixture.Power(ctx, st, 51, 44);
            var alien = RaidFixture.Guard(st, "biter", 49.5, 41.5);              // 8.51 out: lit, so in reach
            st.Engineer.Pos = new Vec2(10.5, 10.5);                              // 50 tiles off: outside its notice radius
            SimTestUtil.Step(sim, 40);
            Assert.That(st.Turrets.Of(turret.Id).Target, Is.EqualTo(alien.Id), "the fixture must have the turret on the lit alien");

            var text = SaveSerializer.WriteText(st, ctx.Data, "2026-09-22T00:00:00Z");
            var load = SaveSerializer.ReadText(text, ctx.Data);
            Assert.That(load.Ok, Is.True, load.Reason);
            var loaded = Simulation.Wrap(ctx, load.State);
            Assert.That(loaded.Hash(), Is.EqualTo(sim.Hash()), "load(save(state)) hashes the same");
            Assert.That(LightQueries.LitAt(loaded.State, 49, 41), Is.True, "a loaded game is not dark until its first tick");

            for (var tick = 0; tick < 40; tick++)
            {
                sim.Tick();
                loaded.Tick();
                Assert.That(loaded.Hash(), Is.EqualTo(sim.Hash()), "tick " + tick + " after the load\n" + Diff(sim, loaded));
            }
            Assert.That(loaded.State.Turrets.Of(turret.Id).Target, Is.EqualTo(alien.Id), "the loaded turret kept the target");
        }
    }
}
