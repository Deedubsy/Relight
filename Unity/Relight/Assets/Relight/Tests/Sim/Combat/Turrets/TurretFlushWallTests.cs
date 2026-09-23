using System.Collections.Generic;
using NUnit.Framework;

namespace Relight.Sim.Tests.Combat
{
    /// <summary>
    /// REL-132, the turret half. The owner's note from their 2026-09-22 play, verbatim: "Turrets should be able to
    /// shoot over a wall, only if up against the wall. Turrets placed away from wall can't shoot over it."
    ///
    /// <para><b>The bug was real.</b> <see cref="Sightline.Clear"/> exempted the tiles the two ENDS of the ray
    /// stand on, and its own comment claimed that covered "a turret placed hard against its wall". It did not: the
    /// exempt tile is the one the turret is standing ON, not the wall beside it, so a gun built flush against a
    /// perimeter was blind past its own parapet — you could wall your base and disarm it in the same action.</para>
    ///
    /// <para>Both halves of the note are tested against the SAME geometry, changed by one tile of wall thickness,
    /// because the two sentences are one rule and a fix that granted the first without keeping the second would be
    /// worse than the bug. And each half is checked three ways — the placement preview, the built turret's card and
    /// the round that does or does not leave the barrel — because GP-W3's whole point was that those three must
    /// never disagree, and REL-15's acceptance criterion says the card must keep telling the truth under the new
    /// rule.</para>
    /// </summary>
    public sealed class TurretFlushWallTests
    {
        private const int Tx = RaidFixture.CoreX, Ty = RaidFixture.CoreY;

        /// <summary>
        /// A wall ring <paramref name="thickness"/> tiles thick around the 2×2 hole a gun actually fits in. One
        /// tile thick puts the whole ring against the turret's footprint; two tiles puts a second course a tile
        /// back from it, which is the owner's "placed away from wall" said in wall rather than in turret.
        /// </summary>
        private static void Ring(SimContext ctx, SimState st, int thickness)
        {
            var lo = Tx - thickness;
            var hi = Tx + 1 + thickness;
            for (var y = Ty - thickness; y <= Ty + 1 + thickness; y++)
                for (var x = lo; x <= hi; x++)
                    if (x < Tx || x > Tx + 1 || y < Ty || y > Ty + 1)
                        RaidFixture.Add(ctx, st, "wall", x, y);
        }

        /// <summary>A loaded, powered gun in the hole, with its supply outside the ring.</summary>
        private static Machine Gun(SimContext ctx, SimState st, string kind = "turret")
        {
            var t = RaidFixture.Add(ctx, st, kind, Tx, Ty);
            t.Rounds = 50;
            RaidFixture.Power(ctx, st, Tx + 5, Ty + 5);
            return t;
        }

        // ---- "Turrets should be able to shoot over a wall, only if up against the wall" ----

        [Test]
        public void ATurretFlushAgainstItsWallSeesOverItAndFires()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            Ring(ctx, st, 1);

            var cover = TurretSight.Coverage(ctx, st, "turret", Tx, Ty);
            Assert.That(cover.OpenFraction, Is.GreaterThan(0.99),
                "every tile of a one-course ring touches the gun's footprint — it shoots over all of it");
            Assert.That(cover.Advice, Is.Empty, "and a gun that can see everywhere is not warned about anything");

            var t = Gun(ctx, st);
            Assert.That(TurretSight.BuiltAdvice(ctx, st, t), Is.Empty,
                "REL-15: the built turret's card must say what the cursor said");

            // Four tiles out. This ground is unlit and since U-D-59 a Gun turret reaches an alien on an unlit tile
            // only to its dark sight (6), so the body stands inside that — the test is about the wall.
            RaidFixture.Guard(st, "biter", Tx + 4, Ty + 0.5);
            RaidFixture.Run(ctx, st, 200);

            Assert.That(RaidFixture.Count<TurretShotEvent>(st), Is.GreaterThan(0),
                "the gun would not fire over the wall it is built against");
            Assert.That(t.Rounds, Is.LessThan(50));
        }

        // ---- "Turrets placed away from wall can't shoot over it" ----

        [Test]
        public void ATurretATileBackFromTheWallIsStillBlindAndHoldsItsFire()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            Ring(ctx, st, 2);

            var cover = TurretSight.Coverage(ctx, st, "turret", Tx, Ty);
            Assert.That(cover.OpenFraction, Is.LessThan(TurretSight.BlindFraction),
                "the second course is a tile back from the gun, and a tile back still blocks");
            Assert.That(cover.Advice, Does.StartWith("Blind position"));

            var t = Gun(ctx, st);
            Assert.That(TurretSight.BuiltAdvice(ctx, st, t), Is.EqualTo(cover.Advice),
                "REL-15: the card must keep telling the truth under the new rule, in the cursor's own words");
            Assert.That(ProductionQueries.Description(ctx, st, t.Id), Does.Contain("Blind position"),
                "and the player must be able to read it on the gun itself");

            RaidFixture.Guard(st, "biter", Tx + 5, Ty + 0.5);
            RaidFixture.Run(ctx, st, 200);

            Assert.That(RaidFixture.Count<TurretShotEvent>(st), Is.Zero,
                "the exemption reached a wall the turret is not standing against");
            Assert.That(t.Rounds, Is.EqualTo(50), "and no ammunition may be spent on a shot that cannot land");
        }

        [Test]
        public void ACannonFlushAgainstItsWallShootsOverItToo()
        {
            // The Cannon is the other 2×2 gun and it fires the same Sightline rule. Worth its own test because
            // FlushTiles was chosen FROM the 2×2 centre offset, so a mistake there would show up here first.
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            Ring(ctx, st, 1);

            var cover = TurretSight.Coverage(ctx, st, "cannon", Tx, Ty);
            Assert.That(cover.OpenFraction, Is.GreaterThan(0.99));

            var c = Gun(ctx, st, "cannon");
            RaidFixture.Guard(st, "biter", Tx + 4, Ty + 0.5);
            RaidFixture.Run(ctx, st, 200);

            Assert.That(RaidFixture.Count<TurretShotEvent>(st), Is.GreaterThan(0));
            Assert.That(c.Rounds, Is.LessThan(50));
        }

        // ---- The rule itself, at the tile that decides it ----

        [Test]
        public void TheExemptionReachesTheTouchingRingAndStopsThere()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            // Two parallel walls east of a 2×2 gun's centre: the one it touches, and the one a tile further out.
            for (var y = Ty - 3; y <= Ty + 4; y++)
            {
                RaidFixture.Add(ctx, st, "wall", Tx + 2, y);
                RaidFixture.Add(ctx, st, "wall", Tx + 3, y);
            }

            var cx = Tx + 1.0;                                // a 2×2 gun's centre, as TurretSight computes it
            var cy = Ty + 1.0;
            Assert.That(Sightline.Flush(cx, cy, Tx + 2, Ty), Is.True, "the touching course");
            Assert.That(Sightline.Flush(cx, cy, Tx + 3, Ty), Is.False, "one course back");
            Assert.That(Sightline.Clear(ctx, st, cx, cy, Tx + 6, cy), Is.False,
                "the second course must still stop the shot");

            // And with only the touching course there, the same shot gets out.
            var st2 = RaidFixture.State(ctx);
            for (var y = Ty - 3; y <= Ty + 4; y++) RaidFixture.Add(ctx, st2, "wall", Tx + 2, y);
            Assert.That(Sightline.Clear(ctx, st2, cx, cy, Tx + 6, cy), Is.True);
        }

        [Test]
        public void TheExemptionIsForTheSHOOTERSWallAndNotForEveryWallNearTheShot()
        {
            // Measured from the ray's START, so a wall hugging the TARGET is not excused. Without this the rule
            // would read "walls near either end do not count", which is a different and much worse rule.
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            for (var y = Ty - 3; y <= Ty + 4; y++) RaidFixture.Add(ctx, st, "wall", Tx + 6, y);

            Assert.That(Sightline.Clear(ctx, st, Tx + 1.0, Ty + 1.0, Tx + 8.5, Ty + 1.0), Is.False,
                "a wall six tiles out is nobody's parapet");
        }

        [Test]
        public void AnAuthoredCityBuildingStopsTheShotEvenAtPointBlank()
        {
            // The flush exemption is only ever consulted for PLAYER walls: ctx.Geometry.Sight is tested first and
            // is never exempted, so building a turret against a city block does not turn the block into a parapet.
            var solids = new List<TileRect> { new TileRect(Tx + 2, Ty - 3, 2, 8) };
            var ctx = RaidFixture.Context(RaidFixture.Map(solids));
            var st = RaidFixture.State(ctx);

            Assert.That(Sightline.Clear(ctx, st, Tx + 1.0, Ty + 1.0, Tx + 6, Ty + 1.0), Is.False,
                "authored geometry is not the player's wall and never becomes flush");
            Assert.That(TurretSight.Coverage(ctx, st, "turret", Tx, Ty).OpenFraction, Is.LessThan(0.99),
                "and the preview must draw the block it cannot see past");
        }

        [Test]
        public void AWreckedFlushWallChangesNothingBecauseItStoppedBlockingAnyway()
        {
            // The two exemptions must not fight: a wreck is already transparent, and the flush rule must not be
            // the thing that decides it. Both rings shoot over a wrecked second course.
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            Ring(ctx, st, 2);
            Assert.That(TurretSight.Coverage(ctx, st, "turret", Tx, Ty).OpenFraction,
                Is.LessThan(TurretSight.BlindFraction));

            foreach (var m in st.Machines)
                if (m.Kind == "wall") TurretRules.Damage(ctx, st, m, TurretRules.MaxHp(ctx.Data, m));

            Assert.That(TurretSight.Coverage(ctx, st, "turret", Tx, Ty).OpenFraction, Is.GreaterThan(0.99),
                "a fallen wall stops nothing, flush or not");
        }
    }
}
