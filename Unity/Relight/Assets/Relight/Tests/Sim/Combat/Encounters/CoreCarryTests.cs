using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Relight.Sim.Tests.Persistence;

namespace Relight.Sim.Tests.Combat
{
    /// <summary>
    /// Batch 4, FRT-07 (REL-142): the two-handed carry. The accept line: sprint, dodge, fire and build are each
    /// refused with their reason while the core is carried; it can be put down and lifted again; death drops it at
    /// the body; the save keeps both a carried core and one on the ground.
    ///
    /// Commands go through the whole composition (<see cref="CommandDispatcher"/>), so a refusal proves the right
    /// handler said it. The map is <see cref="RaidFixture"/>'s open square.
    /// </summary>
    public sealed class CoreCarryTests
    {
        static CommandResult Apply(SimContext ctx, SimState st, Command c) => CommandDispatcher.Apply(ctx, st, c);

        /// <summary>A core on the ground at (x, y), and the engineer standing beside it.</summary>
        static SimState WithCore(SimContext ctx, double x, double y)
        {
            var st = RaidFixture.State(ctx);
            st.Encounters.Cores.Add(new LooseCore { Stronghold = "freight", Pos = new Vec2(x, y) });
            st.Engineer.Pos = new Vec2(x + 1.5, y);
            return st;
        }

        static SimState Carrying(SimContext ctx)
        {
            var st = WithCore(ctx, 40.5, 40.5);
            var r = Apply(ctx, st, new PickUpCoreCommand());
            Assert.That(r.Accepted, Is.True, r.Problem);
            return st;
        }

        [Test]
        public void LiftingTheCoreTakesItOffTheGroundAndIntoBothHands()
        {
            var ctx = RaidFixture.Context();
            var st = WithCore(ctx, 40.5, 40.5);
            st.Engineer.Sprint = true;
            st.Weapons.Firing = true;
            var r = Apply(ctx, st, new PickUpCoreCommand());
            Assert.That(r.Accepted, Is.True, r.Problem);
            Assert.That(st.Engineer.Carrying, Is.EqualTo("freight"));
            Assert.That(st.Encounters.Cores.Count, Is.EqualTo(0), "there is still exactly one core");
            Assert.That(st.Engineer.Sprint, Is.False);
            Assert.That(st.Weapons.Firing, Is.False, "the rifle stops the moment the hands are full");
            Assert.That(RaidFixture.Count<CoreLiftedEvent>(st), Is.EqualTo(1));

            var again = Apply(ctx, st, new PickUpCoreCommand());
            Assert.That(again.Accepted, Is.False);
            Assert.That(again.Problem, Is.EqualTo(CoreCarry.FullHandsText));
        }

        [Test]
        public void ACoreOutOfReachCannotBeLifted()
        {
            var ctx = RaidFixture.Context();
            var st = WithCore(ctx, 40.5, 40.5);
            st.Engineer.Pos = new Vec2(40.5 + ctx.Data.Engineer.ReachTiles + 2, 40.5);
            var r = Apply(ctx, st, new PickUpCoreCommand());
            Assert.That(r.Accepted, Is.False);
            Assert.That(r.Problem, Is.EqualTo(CoreCarry.NoCoreText));
            Assert.That(st.Encounters.Cores.Count, Is.EqualTo(1));
            Assert.That(st.Engineer.Carrying, Is.Empty);
        }

        [Test]
        public void SprintDodgeFireAndBuildAreEachRefusedWithTheirReason()
        {
            var ctx = RaidFixture.Context();
            var st = Carrying(ctx);
            var cases = new (Command c, string why)[]
            {
                (new SprintCommand(true), CoreCarry.SprintText),
                (new DodgeCommand(), CoreCarry.DodgeText),
                (new FireCommand(60, 40), CoreCarry.FireText),
                (new PlaceMachineCommand("stone-furnace", 45, 45), CoreCarry.BuildText),
                (new RotatePlacementCommand(1), CoreCarry.BuildText),
            };
            foreach (var (c, why) in cases)
            {
                var r = Apply(ctx, st, c);
                Assert.That(r.Accepted, Is.False, c.GetType().Name + " was allowed");
                Assert.That(r.Problem, Is.EqualTo(why), c.GetType().Name);
            }
            Assert.That(st.Engineer.Sprint, Is.False);
            Assert.That(st.Engineer.Dash, Is.EqualTo(0));
            Assert.That(st.Weapons.Firing, Is.False);
            Assert.That(CoreCarry.BuildRefusal(st), Is.EqualTo(CoreCarry.BuildText), "the build drawer is told the same");
            Assert.That(Apply(ctx, st, new SprintCommand(false)).Accepted, Is.True, "letting go of Shift is always fine");
            Assert.That(Apply(ctx, st, new HoldFireCommand()).Accepted, Is.True);
        }

        [Test]
        public void CarryingIsWalkingSpeedEvenWithSprintAlreadyHeld()
        {
            var ctx = RaidFixture.Context();
            var phases = new List<ITickPhase> { new EngineerMovementPhase() };
            double Speed(SimState s)
            {
                s.Engineer.Sprint = true;                 // held before the lift, or restored from an odd save
                Assert.That(Apply(ctx, s, new WalkCommand(1, 0)).Accepted, Is.True);
                var x = s.Engineer.Pos.X;
                RaidFixture.Run(ctx, s, 20, phases);
                return s.Engineer.Pos.X - x;
            }
            var free = RaidFixture.State(ctx);
            free.Engineer.Pos = new Vec2(40.5, 40.5);
            var loaded = Carrying(ctx);
            loaded.Engineer.Pos = new Vec2(40.5, 60.5);
            var sprinting = Speed(free);
            var walking = Speed(loaded);
            Assert.That(walking, Is.EqualTo(ctx.Data.Engineer.WalkTilesPerS).Within(0.3), "a second of walking");
            Assert.That(sprinting, Is.GreaterThan(walking * 1.2), "and the same keys without the core sprint");
        }

        [Test]
        public void TheCoreCanBePutDownAndLiftedAgain()
        {
            var ctx = RaidFixture.Context();
            var st = Carrying(ctx);
            st.Engineer.Pos = new Vec2(70.25, 55.75);
            var down = Apply(ctx, st, new DropCoreCommand());
            Assert.That(down.Accepted, Is.True, down.Problem);
            Assert.That(st.Engineer.Carrying, Is.Empty);
            var core = st.Encounters.Cores.Single();
            Assert.That(core.Stronghold, Is.EqualTo("freight"));
            Assert.That(core.Pos, Is.EqualTo(new Vec2(70.25, 55.75)), "at the engineer's feet");
            Assert.That(RaidFixture.Last<CoreSetDownEvent>(st).Fell, Is.False);

            var empty = Apply(ctx, st, new DropCoreCommand());
            Assert.That(empty.Accepted, Is.False);
            Assert.That(empty.Problem, Is.EqualTo(CoreCarry.EmptyHandsText));
            Assert.That(Apply(ctx, st, new SprintCommand(true)).Accepted, Is.True, "free hands sprint again");
            Assert.That(Apply(ctx, st, new SprintCommand(false)).Accepted, Is.True);

            Assert.That(Apply(ctx, st, new PickUpCoreCommand()).Accepted, Is.True);
            Assert.That(st.Engineer.Carrying, Is.EqualTo("freight"));
            Assert.That(st.Encounters.Cores.Count, Is.EqualTo(0));
        }

        [Test]
        public void DeathDropsTheCoreWhereTheBodyFell()
        {
            var ctx = RaidFixture.Context();
            var st = Carrying(ctx);
            st.Engineer.Pos = new Vec2(52.5, 47.25);
            st.Engineer.TakeDamage(ctx, st, st.Engineer.Hp + 10);
            Assert.That(st.Engineer.IsDown, Is.True);
            Assert.That(st.Engineer.Carrying, Is.Empty);
            Assert.That(st.Encounters.Cores.Single().Pos, Is.EqualTo(new Vec2(52.5, 47.25)));
            Assert.That(RaidFixture.Last<CoreSetDownEvent>(st).Fell, Is.True);

            var r = Apply(ctx, st, new PickUpCoreCommand());
            Assert.That(r.Accepted, Is.False, "a body on the ground lifts nothing");
            Assert.That(r.Problem, Is.EqualTo(CoreCarry.DownText));
        }

        [Test]
        public void TheSaveKeepsACarriedCoreAndOneOnTheGround()
        {
            var ctx = RaidFixture.Context();
            var st = Carrying(ctx);
            st.Encounters.Cores.Add(new LooseCore { Stronghold = "other", Pos = new Vec2(12.5, 13.5) });

            var load = SaveSerializer.ReadText(SaveSerializer.WriteText(st, ctx.Data), ctx.Data);
            Assert.That(load.Ok, Is.True, load.Reason);
            Assert.That(load.Header.Version, Is.EqualTo(SaveSchema.Version));
            var b = load.State;
            Assert.That(b.Engineer.Carrying, Is.EqualTo("freight"));
            Assert.That(b.Encounters.Cores.Single().Pos, Is.EqualTo(new Vec2(12.5, 13.5)));
            Assert.That(StateHash.Compute(b), Is.EqualTo(StateHash.Compute(st)));
            Assert.That(Apply(ctx, b, new DodgeCommand()).Problem, Is.EqualTo(CoreCarry.DodgeText), "and still carries it");
        }

        [Test]
        public void AVersionSixteenSaveUpgradesWithEmptyHands()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            var state = PersistenceFixture.Canonical(st);
            Assert.That(Regex.Matches(state, "\"carrying\"").Count, Is.EqualTo(1));
            var state16 = Regex.Replace(state, "\"carrying\":\"[^\"]*\",?", "").Replace(",}", "}");
            Assert.That(state16, Does.Not.Contain("\"carrying\""));

            var file = SaveSerializer.WriteText(st, ctx.Data);
            file = PersistenceFixture.Retarget(file, "state", state16);
            file = PersistenceFixture.Retarget(file, "version", "16");
            file = PersistenceFixture.Retarget(file, "hash", CanonicalJsonWriter.QuoteString(StateHash.Of(state16)));

            var old = SaveSerializer.ReadText(file, ctx.Data);
            Assert.That(old.Ok, Is.True, old.Reason);
            Assert.That(old.Header.Version, Is.EqualTo(16));
            Assert.That(old.Upgraded, Does.Contain("nothing in the engineer's arms"));
            Assert.That(old.State.Engineer.Carrying, Is.Empty);
        }
    }
}
