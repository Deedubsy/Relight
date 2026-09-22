using System.Collections.Generic;
using NUnit.Framework;
using Relight.Sim.Tests.Combat;

namespace Relight.Sim.Tests.Campaign
{
    /// <summary>
    /// L-02, ALWAYS_DARK_SPEC §4 and §5.7: the placement preview's two light questions are the mask's own rule, so
    /// what the ghost promises is what the placed light delivers, gaps included.
    /// </summary>
    public sealed class LightPreviewTests
    {
        private static List<ITickPhase> Phases() =>
            new List<ITickPhase> { new PowerPhase(), new LightPhase() };

        [Test]
        public void TheGhostOfALampPromisesExactlyWhatThePlacedLampLights()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            for (var y = 17; y <= 23; y++) RaidFixture.Add(ctx, st, "wall", 22, y);

            Assert.That(LightPreview.ForGhost(ctx, st, "lamp", 20, 20, Dir.N, out var ghost), Is.True);
            Assert.That(ghost.At(21, 20), Is.True);
            Assert.That(ghost.At(23, 20), Is.False, "the preview shows the gap behind the wall before the player pays");

            RaidFixture.Add(ctx, st, "lamp", 20, 20);
            RaidFixture.Add(ctx, st, "pole", 20, 22);
            RaidFixture.Add(ctx, st, "generator", 20, 24).Inv.Add(ItemId.Coal, 50);
            RaidFixture.Run(ctx, st, 1, Phases());

            for (var ty = 10; ty <= 30; ty++)
                for (var tx = 10; tx <= 30; tx++)
                    Assert.That(ghost.At(tx, ty), Is.EqualTo(LightQueries.LitAt(st, tx, ty)), "tile " + tx + "," + ty);
        }

        [Test]
        public void AFloodlightGhostIsItsConeAndAChestIsNothing()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);

            Assert.That(LightPreview.ForGhost(ctx, st, "floodlight", 40, 40, Dir.E, out var cone), Is.True);
            Assert.That(cone.At(48, 40), Is.True, "along its facing");
            Assert.That(cone.At(32, 40), Is.False, "never behind it");
            Assert.That(LightPreview.ForGhost(ctx, st, "chest", 40, 40, Dir.N, out var none), Is.False);
            Assert.That(none.Count, Is.Zero);
        }

        [Test]
        public void AGhostAtTheMapEdgeIsClippedNotThrown()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            Assert.That(LightPreview.ForGhost(ctx, st, "lamp", 0, 0, Dir.N, out var lit), Is.True);
            Assert.That(lit.At(0, 0), Is.True);
            Assert.That(lit.At(-1, 0), Is.False);
        }

        [Test]
        public void TheTurretTintIsTheLitMaskInsideTheRing()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            Assert.That(LightPreview.LitWithin(st, 41, 41, 9).Count, Is.Zero, "no mask yet: nothing to tint");

            RaidFixture.Add(ctx, st, "lamp", 49, 44);
            RaidFixture.Power(ctx, st, 51, 44);
            RaidFixture.Run(ctx, st, 1, Phases());

            var tint = LightPreview.LitWithin(st, 41, 41, 9);
            Assert.That(tint.At(49, 41), Is.True, "lit and 8.51 out");
            Assert.That(LightQueries.LitAt(st, 52, 44), Is.True);
            Assert.That(tint.At(52, 44), Is.False, "lit, but outside the ring");
            Assert.That(tint.At(45, 41), Is.False, "inside the ring, but dark");
        }

        /// <summary>
        /// REL-116 (found by REL-58): the tint used to promise the lit street behind a warehouse, outside the very
        /// coverage outline drawn over it. A turret cannot shoot what it cannot see, so it must not be tinted.
        /// </summary>
        [Test]
        public void TheTintStopsAtTheWallEvenWhenTheGroundBehindItIsLit()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            for (var y = 38; y <= 44; y++) RaidFixture.Add(ctx, st, "wall", 45, y);

            RaidFixture.Add(ctx, st, "lamp", 48, 41);                    // behind the wall, from the turret's side
            RaidFixture.Power(ctx, st, 50, 41);
            RaidFixture.Add(ctx, st, "lamp", 43, 44);                    // in front of it, in plain sight
            RaidFixture.Power(ctx, st, 43, 46);
            RaidFixture.Run(ctx, st, 1, Phases());

            var cover = TurretSight.Coverage(ctx, st, "turret", 40, 40);
            Assert.That(cover.Known, Is.True);
            var within = LightPreview.LitWithin(st, cover.CentreX, cover.CentreY, cover.RangeTiles);
            var visible = LightPreview.LitVisible(ctx, st, in cover);

            Assert.That(LightQueries.LitAt(st, 47, 41), Is.True, "the ground behind the wall really is lit");
            Assert.That(within.At(47, 41), Is.True, "and it really is inside the range ring");
            Assert.That(visible.At(47, 41), Is.False, "but the turret cannot see it, so it is not tinted");

            Assert.That(LightQueries.LitAt(st, 43, 43), Is.True, "the near lamp really is lighting its own street");
            Assert.That(visible.At(43, 43), Is.True, "lit and in plain sight: still tinted");

            // Nothing is tinted that the gun could not fire at, and nothing is tinted that was not lit to start with.
            for (var ty = within.Y0; ty < within.Y0 + within.H; ty++)
                for (var tx = within.X0; tx < within.X0 + within.W; tx++)
                {
                    if (!visible.At(tx, ty)) continue;
                    Assert.That(within.At(tx, ty), Is.True, "tinted but not lit in range: " + tx + "," + ty);
                    Assert.That(Sightline.Clear(ctx, st, cover.CentreX, cover.CentreY, tx + 0.5, ty + 0.5), Is.True,
                        "tinted but out of sight: " + tx + "," + ty);
                }
            Assert.That(visible.Count, Is.LessThan(within.Count), "the wall costs the tint something");
        }

        /// <summary>REL-116 acceptance 2: with nothing in the way the tint is exactly what it always was.</summary>
        [Test]
        public void WithNothingInTheWayTheTintIsUnchanged()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            RaidFixture.Add(ctx, st, "lamp", 48, 41);
            RaidFixture.Power(ctx, st, 50, 41);
            RaidFixture.Add(ctx, st, "lamp", 43, 44);
            RaidFixture.Run(ctx, st, 1, Phases());

            var cover = TurretSight.Coverage(ctx, st, "turret", 40, 40);
            var within = LightPreview.LitWithin(st, cover.CentreX, cover.CentreY, cover.RangeTiles);
            var visible = LightPreview.LitVisible(ctx, st, in cover);

            Assert.That(within.Count, Is.GreaterThan(0), "there is something to tint");
            Assert.That(visible.Count, Is.EqualTo(within.Count));
            for (var ty = within.Y0; ty < within.Y0 + within.H; ty++)
                for (var tx = within.X0; tx < within.X0 + within.W; tx++)
                    Assert.That(visible.At(tx, ty), Is.EqualTo(within.At(tx, ty)), "tile " + tx + "," + ty);
        }

        [Test]
        public void RunsAndOutlineDescribeTheSameShape()
        {
            // ##.
            // ###     at 10,10
            var w = new TileWindow(10, 10, 3, 2, new byte[] { 1, 1, 0, 1, 1, 1 });
            var runs = new List<TileRun>();
            var edges = new List<TileEdge>();
            LightPreview.Runs(in w, runs);
            LightPreview.Outline(in w, edges);

            Assert.That(runs.Count, Is.EqualTo(2));
            Assert.That((runs[0].Y, runs[0].X0, runs[0].X1), Is.EqualTo((10, 10, 11)));
            Assert.That((runs[1].Y, runs[1].X0, runs[1].X1), Is.EqualTo((11, 10, 12)));

            // top 2, step 1 across + 1 down, right 1, bottom 3, left 2: six joined sides, perimeter 10.
            Assert.That(edges.Count, Is.EqualTo(6));
            var length = 0;
            foreach (var e in edges) length += System.Math.Abs(e.X1 - e.X0) + System.Math.Abs(e.Y1 - e.Y0);
            Assert.That(length, Is.EqualTo(10));
        }
    }
}
