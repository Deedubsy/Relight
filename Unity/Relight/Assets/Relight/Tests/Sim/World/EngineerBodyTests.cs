using NUnit.Framework;

namespace Relight.Sim.Tests
{
    /// <summary>
    /// The engineer's body at 20 Hz (reference walk.ts:167 tickEngineerTiles, engineer.ts movement constants).
    /// Every figure comes from <c>ReferenceData.Create()</c>; nothing is hard-coded twice.
    /// </summary>
    public sealed class EngineerBodyTests
    {
        private const double Eps = 1e-9;

        [Test]
        public void TheEngineerStartsOnTheSpawnAtFullHealth()
        {
            var (ctx, st) = WorldTestSupport.New();
            Assert.AreEqual(SyntheticMap.Spawn.X, st.Engineer.Pos.X, Eps);
            Assert.AreEqual(SyntheticMap.Spawn.Y, st.Engineer.Pos.Y, Eps);
            Assert.AreEqual(ctx.Data.Engineer.MaxHp, st.Engineer.Hp, Eps);
            Assert.AreEqual(1.0, st.Engineer.Stamina, Eps);
            Assert.IsFalse(st.Engineer.IsDown);
            Assert.AreEqual(TileClass.Street, ctx.Geometry.TileAt(6, 6), "the spawn is on a street");
        }

        [Test]
        public void TheMovementPhaseAndHandlerAreRegistered()
        {
            var hasPhase = false;
            foreach (var p in SimComposition.Phases) if (p is EngineerMovementPhase) hasPhase = true;
            var hasInit = false;
            foreach (var i in SimComposition.Initializers) if (i is EngineerSpawnInitializer) hasInit = true;
            var hasHandler = false;
            foreach (var h in SimComposition.Handlers) if (h is MovementCommandHandler) hasHandler = true;
            Assert.IsTrue(hasPhase, "EngineerMovementPhase");
            Assert.IsTrue(hasInit, "EngineerSpawnInitializer");
            Assert.IsTrue(hasHandler, "MovementCommandHandler");
        }

        [Test]
        public void OneSecondOfHeldKeysWalksWalkTilesPerS()
        {
            var (ctx, st) = WorldTestSupport.New();
            WorldTestSupport.Place(st, 6.5, 6.5);
            WorldTestSupport.Send(ctx, st, new WalkCommand(1, 0));
            WorldTestSupport.Step(ctx, st, 20);
            Assert.AreEqual(6.5 + ctx.Data.Engineer.WalkTilesPerS, st.Engineer.Pos.X, Eps);
            Assert.AreEqual(6.5, st.Engineer.Pos.Y, Eps);
            Assert.AreEqual(1.0, st.Engineer.Walked, Eps);
            Assert.AreEqual(1.0, st.Engineer.Face.X, Eps);
        }

        [Test]
        public void DiagonalKeysStillWalkWalkTilesPerS()
        {
            var (ctx, st) = WorldTestSupport.New();
            WorldTestSupport.Place(st, 10.5, 44.5);
            WorldTestSupport.Send(ctx, st, new WalkCommand(1, -1));   // north-east: the map edge is south of here
            WorldTestSupport.Step(ctx, st, 20);
            var dx = st.Engineer.Pos.X - 10.5;
            var dy = st.Engineer.Pos.Y - 44.5;
            Assert.AreEqual(ctx.Data.Engineer.WalkTilesPerS, System.Math.Sqrt(dx * dx + dy * dy), 1e-9);
        }

        [Test]
        public void TheEngineerStopsAtAWallAndSlidesAlongIt()
        {
            var (ctx, st) = WorldTestSupport.New();
            var r = ctx.Data.Engineer.BodyRadiusTiles;
            WorldTestSupport.Place(st, 38.5, 11.5);
            WorldTestSupport.Send(ctx, st, new WalkCommand(1, 0));
            WorldTestSupport.Step(ctx, st, 40);   // two seconds: far more than the 1.5 tiles to the wall
            Assert.Less(st.Engineer.Pos.X, 40 - r, "the body cannot overlap the building");
            Assert.Greater(st.Engineer.Pos.X, 39.0, "but it walks right up to it");
            Assert.AreEqual(11.5, st.Engineer.Pos.Y, Eps, "no drift on the free axis");

            // Now push into the wall diagonally: x is blocked, y keeps going (axis-separated sliding).
            var stuckX = st.Engineer.Pos.X;
            WorldTestSupport.Send(ctx, st, new WalkCommand(1, 1));
            WorldTestSupport.Step(ctx, st, 20);
            Assert.AreEqual(stuckX, st.Engineer.Pos.X, Eps, "still held by the wall");
            Assert.Greater(st.Engineer.Pos.Y, 11.5 + 3, "slid south along it");
        }

        [Test]
        public void SprintingIsFasterAndDrainsTheBar()
        {
            var (ctx, st) = WorldTestSupport.New();
            var d = ctx.Data.Engineer;
            WorldTestSupport.Place(st, 10.5, 44.5);
            WorldTestSupport.Send(ctx, st, new WalkCommand(1, 0));
            WorldTestSupport.Send(ctx, st, new SprintCommand(true));
            WorldTestSupport.Step(ctx, st, 20);
            Assert.AreEqual(10.5 + d.WalkTilesPerS * d.SprintMul, st.Engineer.Pos.X, 1e-9);
            Assert.AreEqual(1.0 - d.SprintDrainPerS, st.Engineer.Stamina, 1e-9);
        }

        [Test]
        public void TheBarRefillsWhenTheKeysAreReleased()
        {
            var (ctx, st) = WorldTestSupport.New();
            var d = ctx.Data.Engineer;
            WorldTestSupport.Place(st, 10.5, 44.5);
            st.Engineer.Stamina = 0.5;
            WorldTestSupport.Send(ctx, st, new WalkCommand(0, 0));
            WorldTestSupport.Send(ctx, st, new SprintCommand(false));
            WorldTestSupport.Step(ctx, st, 20);
            Assert.AreEqual(0.5 + d.StaminaRegenPerS, st.Engineer.Stamina, 1e-9);
        }

        [Test]
        public void SprintNeverTakesTheBarBelowOneDodge()
        {
            var (ctx, st) = WorldTestSupport.New();
            var d = ctx.Data.Engineer;
            WorldTestSupport.Place(st, 10.5, 44.5);
            st.Engineer.Stamina = d.DashCost + 0.01;
            WorldTestSupport.Send(ctx, st, new WalkCommand(1, 0));
            WorldTestSupport.Send(ctx, st, new SprintCommand(true));
            WorldTestSupport.Step(ctx, st, 40);
            Assert.AreEqual(d.DashCost, st.Engineer.Stamina, 1e-9);
        }

        [Test]
        public void ADodgeCoversDashTilesInDashSeconds()
        {
            var (ctx, st) = WorldTestSupport.New();
            var d = ctx.Data.Engineer;
            WorldTestSupport.Place(st, 10.5, 44.5);
            st.Engineer.Face = new Vec2(1, 0);
            Assert.IsTrue(WorldTestSupport.Send(ctx, st, new DodgeCommand()).Accepted);
            Assert.AreEqual(1.0 - d.DashCost, st.Engineer.Stamina, Eps);
            Assert.AreEqual(d.DashCooldownS, st.Engineer.DashCooldown, Eps);

            var ticks = (int)System.Math.Round(d.DashSeconds / WorldTestSupport.Dt);
            WorldTestSupport.Step(ctx, st, ticks);
            Assert.AreEqual(10.5 + d.DashTiles, st.Engineer.Pos.X, 1e-9);
            Assert.LessOrEqual(st.Engineer.Dash, 1e-12, "the dash has run out (to rounding)");
            Assert.AreEqual(d.DashSeconds, st.Engineer.IFrames, 1e-9, "i-frames run for the whole dash");
        }

        [Test]
        public void ADodgeIsRefusedUntilTheCooldownExpires()
        {
            var (ctx, st) = WorldTestSupport.New();
            var d = ctx.Data.Engineer;
            WorldTestSupport.Place(st, 10.5, 44.5);
            Assert.IsTrue(WorldTestSupport.Send(ctx, st, new DodgeCommand()).Accepted);
            WorldTestSupport.Step(ctx, st, 10);
            Assert.IsFalse(WorldTestSupport.Send(ctx, st, new DodgeCommand()).Accepted, "still cooling down");
            WorldTestSupport.Step(ctx, st, (int)System.Math.Round(d.DashCooldownS / WorldTestSupport.Dt));
            Assert.AreEqual(0, st.Engineer.DashCooldown, Eps);
            Assert.IsTrue(WorldTestSupport.Send(ctx, st, new DodgeCommand()).Accepted);
        }

        [Test]
        public void WalkHereFollowsAPathAndClearsTheTargetOnArrival()
        {
            var (ctx, st) = WorldTestSupport.New();
            WorldTestSupport.Place(st, 10.5, 44.5);
            WorldTestSupport.Send(ctx, st, new MoveCommand(16.5, 44.5));
            WorldTestSupport.Step(ctx, st, 1);
            Assert.IsNotNull(WorldQueries.Path(st), "a route was planned");
            WorldTestSupport.Step(ctx, st, 40);
            Assert.IsFalse(st.Engineer.HasTarget, "arrived");
            Assert.IsNull(WorldQueries.Path(st));
            Assert.AreEqual(16.5, st.Engineer.Pos.X, 1e-9);
            Assert.AreEqual(44.5, st.Engineer.Pos.Y, 1e-9);
        }

        [Test]
        public void AHeldKeyCancelsAWalkHere()
        {
            var (ctx, st) = WorldTestSupport.New();
            WorldTestSupport.Place(st, 10.5, 44.5);
            WorldTestSupport.Send(ctx, st, new MoveCommand(20.5, 44.5));
            WorldTestSupport.Step(ctx, st, 2);
            Assert.IsTrue(st.Engineer.HasTarget);
            WorldTestSupport.Send(ctx, st, new WalkCommand(0, 1));
            Assert.IsFalse(st.Engineer.HasTarget, "the key wins");
            WorldTestSupport.Step(ctx, st, 2);
            Assert.IsNull(WorldQueries.Path(st), "and the route is dropped");
        }

        [Test]
        public void AWalkHereIntoTheRiverIsDropped()
        {
            var (ctx, st) = WorldTestSupport.New();
            WorldTestSupport.Place(st, 10.5, 20.5);
            WorldTestSupport.Send(ctx, st, new MoveCommand(10.5, 25.5));
            WorldTestSupport.Step(ctx, st, 20);
            // nearestOpen snaps the goal to the bank, so the engineer walks to the water's edge and stops there.
            Assert.IsFalse(st.Engineer.HasTarget);
            Assert.Less(st.Engineer.Pos.Y, 24.0, "never entered the water");
        }

        [Test]
        public void NoHealthRegenerationBeforeTheDelay()
        {
            var (ctx, st) = WorldTestSupport.New();
            var d = ctx.Data.Engineer;
            st.Engineer.TakeDamage(ctx, st, 30);
            Assert.AreEqual(d.MaxHp - 30, st.Engineer.Hp, Eps);
            Assert.AreEqual(0, st.Engineer.LastHit, Eps);
            WorldTestSupport.Step(ctx, st, (int)System.Math.Round(d.RegenDelayS / WorldTestSupport.Dt) - 1);
            Assert.AreEqual(d.MaxHp - 30, st.Engineer.Hp, 1e-9, "nothing until the delay is up");
        }

        [Test]
        public void HealthRegeneratesAtTheDataRateAndStopsFull()
        {
            var (ctx, st) = WorldTestSupport.New();
            var d = ctx.Data.Engineer;
            st.Engineer.TakeDamage(ctx, st, 30);
            st.Engineer.LastHit = -1000;   // the delay is already behind us
            WorldTestSupport.Step(ctx, st, 20);
            Assert.AreEqual(d.MaxHp - 30 + d.RegenPerS, st.Engineer.Hp, 1e-9);

            st.Engineer.Hp = d.MaxHp - 0.1;
            WorldTestSupport.Step(ctx, st, 20);
            Assert.AreEqual(d.MaxHp, st.Engineer.Hp, Eps, "never above full");
        }

        [Test]
        public void GoingDownAndStandingUpAgainAtTheSpawn()
        {
            var (ctx, st) = WorldTestSupport.New();
            var d = ctx.Data.Engineer;
            WorldTestSupport.Place(st, 20.5, 44.5);
            st.Engineer.TakeDamage(ctx, st, d.MaxHp);

            Assert.IsTrue(st.Engineer.IsDown);
            Assert.AreEqual(0, st.Engineer.Hp, Eps);
            Assert.AreEqual(1, st.Engineer.Downs);
            Assert.AreEqual(d.RespawnS, st.Engineer.Down, Eps);
            Assert.AreEqual(1, WorldTestSupport.EventsOf<EngineerDownEvent>(st).Count);

            WorldTestSupport.Step(ctx, st, (int)System.Math.Round((d.RespawnS - 0.5) / WorldTestSupport.Dt));
            Assert.IsTrue(st.Engineer.IsDown, "still down half a second early");
            Assert.AreEqual(20.5, st.Engineer.Pos.X, Eps, "and still lying where they fell");

            WorldTestSupport.Step(ctx, st, 20);
            Assert.IsFalse(st.Engineer.IsDown);
            Assert.AreEqual(d.MaxHp, st.Engineer.Hp, Eps);
            Assert.AreEqual(ctx.Geometry.Spawn.X, st.Engineer.Pos.X, Eps);
            Assert.AreEqual(ctx.Geometry.Spawn.Y, st.Engineer.Pos.Y, Eps);
            Assert.AreEqual(1, WorldTestSupport.EventsOf<EngineerUpEvent>(st).Count);
        }

        [Test]
        public void ADownEngineerIgnoresMovementCommands()
        {
            var (ctx, st) = WorldTestSupport.New();
            WorldTestSupport.Place(st, 20.5, 44.5);
            st.Engineer.TakeDamage(ctx, st, ctx.Data.Engineer.MaxHp);
            Assert.IsFalse(WorldTestSupport.Send(ctx, st, new DodgeCommand()).Accepted);
            WorldTestSupport.Send(ctx, st, new WalkCommand(1, 0));
            WorldTestSupport.Send(ctx, st, new MoveCommand(30.5, 44.5));
            WorldTestSupport.Step(ctx, st, 20);
            Assert.AreEqual(20.5, st.Engineer.Pos.X, Eps);
            Assert.IsFalse(st.Engineer.HasTarget);
        }

        [Test]
        public void TwoRunsOfTheSameInputsGiveTheSamePosition()
        {
            var a = Run();
            var b = Run();
            Assert.AreEqual(a.X, b.X, 0, "bit-identical");
            Assert.AreEqual(a.Y, b.Y, 0, "bit-identical");

            Vec2 Run()
            {
                var (ctx, st) = WorldTestSupport.New();
                WorldTestSupport.Send(ctx, st, new MoveCommand(6.5, 44.5));
                WorldTestSupport.Step(ctx, st, 200);
                WorldTestSupport.Send(ctx, st, new WalkCommand(1, 1));
                WorldTestSupport.Send(ctx, st, new SprintCommand(true));
                WorldTestSupport.Step(ctx, st, 60);
                WorldTestSupport.Send(ctx, st, new DodgeCommand());
                WorldTestSupport.Step(ctx, st, 20);
                return st.Engineer.Pos;
            }
        }
    }
}
