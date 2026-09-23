using System.Collections.Generic;
using NUnit.Framework;
using Relight.Sim.UI;

namespace Relight.Sim.Tests
{
    /// <summary>
    /// REL-132, the gate half. The owner's note from their 2026-09-22 play: "With being able to build walls, we need
    /// the ability to build a gate so we can full wall off an area."
    ///
    /// The acceptance criteria these stand for, verbatim from the issue: "A player can enclose an area completely
    /// and still walk in and out"; "Raiders do not path through a standing gate; they attack it, and a destroyed
    /// gate becomes passable"; "Save round-trips a gate and its open/shut state correctly."
    ///
    /// <para>The ring tests build with <see cref="Placement.Add"/> rather than <see cref="Fixture.Place"/> because
    /// the real placement path checks the engineer's reach, and a yard the engineer can stand in the middle of is
    /// wider than their arms. <see cref="TheGateIsBuiltThroughTheOrdinaryPlacementPath"/> is the one that goes the
    /// whole way through <see cref="Placement.Place"/>, so the catalogue row is proved buildable for real.</para>
    /// </summary>
    public sealed class GateTests
    {
        private const string Gate = GateRules.Kind;

        private static SimContext Ctx() => Fixture.Context();
        private static SimState Fresh(SimContext ctx) => Fixture.State(ctx);

        /// <summary>Straight onto the map, past cost and reach — the ring fixtures' building block.</summary>
        private static Machine Add(SimContext ctx, SimState st, string kind, int x, int y) =>
            Placement.Add(ctx, st, kind, x, y, Dir.N);

        /// <summary>The engineer put somewhere definite, which is what <see cref="GateRules.Open"/> reads.</summary>
        private static void Stand(SimState st, double x, double y) => st.Engineer.Pos = new Vec2(x, y);

        private static void Wreck(SimContext ctx, SimState st, Machine m) =>
            TurretRules.Damage(ctx, st, m, TurretRules.MaxHp(ctx.Data, m));

        // ---- The catalogue row ----

        [Test]
        public void TheGateIsABuildableDefenceWithAPriceAndHitPoints()
        {
            var ctx = Ctx();
            Assert.That(ctx.Data.TryMachine(Gate, out var spec), Is.True, "the Gate is not in the catalogue");
            Assert.That(spec.Size, Is.EqualTo(1),
                "REL-127's building glow excludes anything under 2 tiles; a bigger gate would start glowing");
            Assert.That(spec.Hp, Is.EqualTo(100.0).Within(1e-9));
            Assert.That(ctx.Data.TryMachine("wall", out var wall), Is.True);
            Assert.That(spec.Hp, Is.LessThan(wall.Hp),
                "a door should be the weak point of a wall, not the strong one");
            Assert.That(spec.Cost, Is.Not.Null);
            Assert.That(spec.Cost.Count, Is.GreaterThan(0), "a gate nobody pays for is not a build");
            Assert.That(spec.Unlock, Is.Empty, "the hole in a wall is a problem the moment walls exist");
            Assert.That(BuildCatalogue.Category(Gate), Is.EqualTo(BuildCategory.Defence));
            Assert.That(BuildCatalogue.DescriptionFor(Gate), Is.Not.Empty,
                "it would sit in the build menu with no words");
        }

        [Test]
        public void TheGateIsBuiltThroughTheOrdinaryPlacementPath()
        {
            var ctx = Ctx();
            var st = Fresh(ctx);
            var g = Fixture.Place(ctx, st, Gate, (int)ctx.Geometry.Spawn.X + 1, (int)ctx.Geometry.Spawn.Y);
            Assert.That(GateRules.IsGate(g), Is.True);
            Assert.That(g.Size, Is.EqualTo(1));
            Assert.That(TurretRules.MaxHp(ctx.Data, g), Is.EqualTo(100.0).Within(1e-9));
            Assert.That(TurretRules.IsDefence(ctx.Data, g), Is.True,
                "with no integrity row a raid would treat it as ground it may never breach");
            Assert.That(Fixture.Off(ctx, st), Is.Empty, "placing a gate unbalanced the ledger");
        }

        [Test]
        public void TheGateDoesNotGlowLikeABuilding()
        {
            // REL-127 wrote this expectation down before the Gate existed: it is one tile, so it is excluded by the
            // same clause that excludes the Wall the owner excluded by name. This is that sentence held to.
            var ctx = Ctx();
            Assert.That(ctx.Data.TryMachine(Gate, out var spec), Is.True);
            Assert.That(BuildingGlow.Glows(spec), Is.False);
        }

        // ---- Passability: the engineer, and nobody else ----

        [Test]
        public void AStandingGateIsSolidToEveryoneAndOpenToTheEngineer()
        {
            var ctx = Ctx();
            var st = Fresh(ctx);
            var g = Add(ctx, st, Gate, 20, 20);

            Assert.That(Ground.Passable(ctx, st, 20, 20), Is.False, "the plain answer must stay 'blocked'");
            Assert.That(Ground.Occupied(ctx, st, 20, 20), Is.True,
                "a gate tile is occupied — nothing else may be built on it");
            Assert.That(Ground.PassableForEngineer(ctx, st, 20, 20), Is.True,
                "the engineer cannot get through their own gate");
            Assert.That(TurretRules.BlocksRaiders(ctx.Data, g), Is.True, "a raid must treat it as the wall it is");
            Assert.That(DirectorRules.HostileOpen(ctx, st, 20, 20), Is.False, "and so must the raid's own routing");
        }

        [Test]
        public void AWallIsShutToTheEngineerToo()
        {
            // The control. Without it, a bug that opened EVERY machine to the engineer would pass the test above.
            var ctx = Ctx();
            var st = Fresh(ctx);
            Add(ctx, st, "wall", 20, 20);
            Assert.That(Ground.PassableForEngineer(ctx, st, 20, 20), Is.False);
            Assert.That(Ground.Passable(ctx, st, 20, 20), Is.False);
        }

        [Test]
        public void AWreckedGateIsAHoleForEveryone()
        {
            var ctx = Ctx();
            var st = Fresh(ctx);
            var g = Add(ctx, st, Gate, 20, 20);
            Wreck(ctx, st, g);

            Assert.That(TurretRules.Wrecked(ctx.Data, st, g), Is.True, "the fixture did not actually wreck it");
            Assert.That(Ground.Passable(ctx, st, 20, 20), Is.True, "a destroyed gate must become passable");
            Assert.That(Ground.PassableForEngineer(ctx, st, 20, 20), Is.True);
            Assert.That(DirectorRules.HostileOpen(ctx, st, 20, 20), Is.True,
                "whatever broke it walks in through the hole");
            Assert.That(GateRules.Open(ctx, st, g), Is.False, "a wreck is a hole, not an open door");
        }

        // ---- The owner's sentence: enclose an area completely and still walk in and out ----

        private const int X0 = 18, Y0 = 18, X1 = 24, Y1 = 24, GateX = 21;

        /// <summary>A closed ring of wall, with one gate in the middle of its northern run when asked for.</summary>
        private static Machine Enclose(SimContext ctx, SimState st, bool withGate)
        {
            Machine gate = null;
            for (var x = X0; x <= X1; x++)
            {
                if (withGate && x == GateX) gate = Add(ctx, st, Gate, x, Y0);
                else Add(ctx, st, "wall", x, Y0);
                Add(ctx, st, "wall", x, Y1);
            }
            for (var y = Y0 + 1; y < Y1; y++)
            {
                Add(ctx, st, "wall", X0, y);
                Add(ctx, st, "wall", X1, y);
            }
            return gate;
        }

        private const int InsideX = 21, InsideY = 21, OutsideX = 21, OutsideY = 15;

        [Test]
        public void AnEnclosedYardCanStillBeWalkedIntoAndOutOf()
        {
            var ctx = Ctx();
            var st = Fresh(ctx);
            var gate = Enclose(ctx, st, withGate: true);
            Assert.That(gate, Is.Not.Null);

            var leaving = PathFinder.FindPath(ctx, st, InsideX, InsideY, OutsideX, OutsideY, throughGates: true);
            Assert.That(leaving, Is.Not.Null, "the engineer is sealed inside their own yard");
            Assert.That(Contains(leaving, gate.X, gate.Y), Is.True, "the way out did not go through the gate");

            var returning = PathFinder.FindPath(ctx, st, OutsideX, OutsideY, InsideX, InsideY, throughGates: true);
            Assert.That(returning, Is.Not.Null, "in AND out — the note asks for both");
            Assert.That(Contains(returning, gate.X, gate.Y), Is.True);
        }

        [Test]
        public void NothingButTheEngineerFindsAWayThroughThatYard()
        {
            var ctx = Ctx();
            var st = Fresh(ctx);
            Enclose(ctx, st, withGate: true);
            Assert.That(PathFinder.FindPath(ctx, st, OutsideX, OutsideY, InsideX, InsideY), Is.Null,
                "the default route walked straight through a standing gate");
        }

        [Test]
        public void AYardWithNoGateIsSealedToTheEngineerAsWell()
        {
            // The control for the two above: the ring itself has to be genuinely closed, or they prove nothing.
            var ctx = Ctx();
            var st = Fresh(ctx);
            Enclose(ctx, st, withGate: false);
            Assert.That(PathFinder.FindPath(ctx, st, InsideX, InsideY, OutsideX, OutsideY, throughGates: true),
                Is.Null, "there is a hole in the wall ring, so the gate tests are measuring the hole");
        }

        [Test]
        public void ABrokenGateLetsTheYardBeWalkedByAnything()
        {
            var ctx = Ctx();
            var st = Fresh(ctx);
            var gate = Enclose(ctx, st, withGate: true);
            Wreck(ctx, st, gate);
            Assert.That(PathFinder.FindPath(ctx, st, OutsideX, OutsideY, InsideX, InsideY), Is.Not.Null,
                "a destroyed gate must be a way in for whatever destroyed it");
        }

        private static bool Contains(List<TilePoint> path, int x, int y)
        {
            for (var i = 0; i < path.Count; i++) if (path[i].X == x && path[i].Y == y) return true;
            return false;
        }

        // ---- Sight ----

        [Test]
        public void TheGateStopsASightlineExactlyAsTheWallEitherSideDoes()
        {
            var ctx = Ctx();
            var st = Fresh(ctx);
            Add(ctx, st, Gate, 20, 20);
            Assert.That(Sightline.Opaque(Gate), Is.True);
            // Well back from the gate at both ends, so neither the end-tile exemption nor REL-132's own flush
            // exemption reaches it.
            Assert.That(Sightline.Clear(ctx, st, 20.5, 15.5, 20.5, 25.5), Is.False,
                "a gate fitted into a wall would otherwise be a firing slit");
        }

        [Test]
        public void TheGateStaysOpaqueWhileItIsDrawnOpen()
        {
            // A deliberate limit, written down in GateRules: WallMask is cached on Rev, and the engineer walking
            // does not bump Rev, so opacity cannot follow the leaf. If that is ever revisited in play, this is the
            // test to change.
            var ctx = Ctx();
            var st = Fresh(ctx);
            var g = Add(ctx, st, Gate, 20, 20);
            Stand(st, 20.5, 21.5);
            Assert.That(GateRules.Open(ctx, st, g), Is.True, "the engineer is standing right on top of it");
            Assert.That(Sightline.Clear(ctx, st, 20.5, 15.5, 20.5, 25.5), Is.False);
        }

        // ---- What the leaf is drawn as ----

        [Test]
        public void TheGateIsDrawnOpenOnlyWithTheEngineerBesideIt()
        {
            var ctx = Ctx();
            var st = Fresh(ctx);
            var g = Add(ctx, st, Gate, 20, 20);

            Stand(st, 20.5, 20.5);
            Assert.That(GateRules.Open(ctx, st, g), Is.True, "standing in the doorway");

            Stand(st, 20.5, 22.9);
            Assert.That(GateRules.Open(ctx, st, g), Is.True, "within the open radius");

            Stand(st, 20.5, 24.5);
            Assert.That(GateRules.Open(ctx, st, g), Is.False, "it must shut behind them");
        }

        [Test]
        public void ADownedEngineerDoesNotHoldTheGateOpen()
        {
            var ctx = Ctx();
            var st = Fresh(ctx);
            var g = Add(ctx, st, Gate, 20, 20);
            Stand(st, 20.5, 20.5);
            // Down them the way the game does. IsDown reads Engineer.Down — the sim second they stand back up,
            // -1 while they are on their feet — and not Hp, so zeroing Hp by hand would leave them standing as
            // far as this rule is concerned.
            st.Engineer.TakeDamage(ctx, st, st.Engineer.Hp);
            Assert.That(st.Engineer.IsDown, Is.True, "the fixture did not actually put them down");
            Assert.That(GateRules.Open(ctx, st, g), Is.False);
        }

        [Test]
        public void TheLeafLiesAlongTheRunOfWallItIsFittedInto()
        {
            var ctx = Ctx();

            var across = Fresh(ctx);
            var eastWest = Add(ctx, across, Gate, 20, 20);
            Add(ctx, across, "wall", 19, 20);
            Add(ctx, across, "wall", 21, 20);
            Assert.That(GateRules.Horizontal(ctx, across, eastWest), Is.True,
                "wall to east and west — the leaf lies east to west");

            var along = Fresh(ctx);
            var northSouth = Add(ctx, along, Gate, 20, 20);
            Add(ctx, along, "wall", 20, 19);
            Add(ctx, along, "wall", 20, 21);
            Assert.That(GateRules.Horizontal(ctx, along, northSouth), Is.False, "wall to north and south");

            var bare = Fresh(ctx);
            var alone = Add(ctx, bare, Gate, 20, 20);
            Assert.That(GateRules.Horizontal(ctx, bare, alone), Is.True,
                "a gate standing on its own needs one definite default, not a flicker between two");
        }

        // ---- Save ----

        [Test]
        public void SaveRoundTripsAGateAndTheStateItIsDrawnIn()
        {
            var ctx = Ctx();
            var st = Fresh(ctx);
            var g = Add(ctx, st, Gate, 20, 20);
            Add(ctx, st, "wall", 19, 20);
            Add(ctx, st, "wall", 21, 20);
            Stand(st, 20.5, 21.2);
            Assert.That(GateRules.Open(ctx, st, g), Is.True, "the precondition: it is open before the save");

            var load = SaveSerializer.ReadText(SaveSerializer.WriteText(st, ctx.Data), ctx.Data);
            Assert.That(load.Ok, Is.True, load.Reason);
            var back = load.State;

            Machine gate = null;
            for (var i = 0; i < back.Machines.Count; i++)
                if (GateRules.IsGate(back.Machines[i])) gate = back.Machines[i];
            Assert.That(gate, Is.Not.Null, "the gate did not survive the save");
            Assert.That(gate.X, Is.EqualTo(20));
            Assert.That(gate.Y, Is.EqualTo(20));
            Assert.That(TurretRules.Hp(ctx.Data, back, gate), Is.EqualTo(100.0).Within(1e-9),
                "its hit points did not survive");

            // Open/shut is DERIVED, so what has to round-trip is what it is derived FROM. It does.
            Assert.That(GateRules.Open(ctx, back, gate), Is.True,
                "it loaded shut with the engineer standing in the doorway");
            Assert.That(GateRules.Horizontal(ctx, back, gate), Is.True, "the wall either side did not survive");
            Assert.That(Ground.PassableForEngineer(ctx, back, 20, 20), Is.True, "the loaded gate will not open");
            Assert.That(Ground.Passable(ctx, back, 20, 20), Is.False,
                "the loaded gate stopped being a wall to everything else");
        }

        [Test]
        public void ADamagedGateRoundTripsAsDamagedAndAWreckedOneAsAHole()
        {
            var ctx = Ctx();
            var st = Fresh(ctx);
            var g = Add(ctx, st, Gate, 20, 20);
            TurretRules.Damage(ctx, st, g, 40);

            var load = SaveSerializer.ReadText(SaveSerializer.WriteText(st, ctx.Data), ctx.Data);
            Assert.That(load.Ok, Is.True, load.Reason);
            var hurt = load.State.Machines[0];
            Assert.That(TurretRules.Hp(ctx.Data, load.State, hurt), Is.EqualTo(60.0).Within(1e-9));
            Assert.That(Ground.Passable(ctx, load.State, 20, 20), Is.False, "a damaged gate is still a gate");

            Wreck(ctx, load.State, hurt);
            var again = SaveSerializer.ReadText(SaveSerializer.WriteText(load.State, ctx.Data), ctx.Data);
            Assert.That(again.Ok, Is.True, again.Reason);
            Assert.That(TurretRules.Wrecked(ctx.Data, again.State, again.State.Machines[0]), Is.True);
            Assert.That(Ground.Passable(ctx, again.State, 20, 20), Is.True, "the wreck loaded as a wall again");
        }

        [Test]
        public void TheGateAddsNothingToTheSaveFormat()
        {
            // 13 since REL-137 (the encounters), 14 since REL-138 (keys and crates), 15 since REL-140 (the
            // stronghold doors and fight clock), 16 since REL-141 (the guardian and its core) and 17 since REL-142 (the
            // carried core), none of them the gate's doing.
            Assert.That(SaveSchema.Version, Is.EqualTo(17),
                "a gate is an ordinary machine; if the version moved, something here started saving state of its own");
        }
    }
}
