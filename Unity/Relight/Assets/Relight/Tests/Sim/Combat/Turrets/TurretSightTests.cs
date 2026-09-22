using NUnit.Framework;

namespace Relight.Sim.Tests.Combat
{
    /// <summary>
    /// GP-W3: "show turret range and obstructed sight during placement; warn clearly about a blind position; keep
    /// wall, projectile and sight behaviour consistent."
    ///
    /// The two halves are tested together on purpose. A coverage preview that disagreed with the gun would be
    /// worse than no preview at all, so every check here asserts the shape the player is shown AND the shot the
    /// turret then takes.
    /// </summary>
    public sealed class TurretSightTests
    {
        private const int Tx = RaidFixture.CoreX, Ty = RaidFixture.CoreY;

        [Test]
        public void ATurretInTheOpenReadsAsFullyCoveredAndIsGivenNoAdvice()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);

            var cover = TurretSight.Coverage(ctx, st, "turret", Tx, Ty);

            Assert.That(cover.Known, Is.True);
            Assert.That(ctx.Data.TryTurret("turret", out var def), Is.True);
            Assert.That(cover.RangeTiles, Is.EqualTo(def.RangeTiles).Within(1e-9));
            Assert.That(cover.Reach.Length, Is.EqualTo(TurretSight.Rays));
            Assert.That(cover.OpenFraction, Is.GreaterThan(0.99), "flat ground blocks nothing");
            Assert.That(cover.Advice, Is.Empty, "a clear position is not worth a sentence");
        }

        [Test]
        public void ARingOfWallsReadsAsBlindAndTheTurretInsideItCannotShootOut()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);

            // A closed 5x5 box of wall machines with a 1x1 hole in the middle for the turret.
            for (var y = Ty - 2; y <= Ty + 2; y++)
                for (var x = Tx - 2; x <= Tx + 2; x++)
                    if (x != Tx || y != Ty) RaidFixture.Add(ctx, st, "wall", x, y);

            var cover = TurretSight.Coverage(ctx, st, "turret", Tx, Ty);
            Assert.That(cover.Known, Is.True);
            Assert.That(cover.OpenFraction, Is.LessThan(TurretSight.BlindFraction), "boxed in on every bearing");
            Assert.That(cover.Advice, Does.StartWith("Blind position"));

            // And the gun agrees: a body four tiles outside the box is never engaged.
            var t = RaidFixture.Turret(ctx, st, Tx, Ty);
            RaidFixture.Guard(st, "biter", Tx + 6, Ty);
            RaidFixture.Run(ctx, st, 200);

            Assert.That(RaidFixture.Count<TurretShotEvent>(st), Is.Zero, "a wall the preview drew must stop the shot");
            Assert.That(t.Rounds, Is.EqualTo(50), "and no ammunition may be spent on a shot that cannot land");
        }

        [Test]
        public void TheSameTurretWithTheWallRemovedCoversItsRangeAgainAndFires()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            for (var y = Ty - 2; y <= Ty + 2; y++)
                for (var x = Tx - 2; x <= Tx + 2; x++)
                    if (x != Tx || y != Ty) RaidFixture.Add(ctx, st, "wall", x, y);

            var boxed = TurretSight.Coverage(ctx, st, "turret", Tx, Ty).OpenFraction;

            st.Machines.Clear();
            st.Rev++;                                   // the mask is keyed on Rev; a removal must invalidate it
            var open = TurretSight.Coverage(ctx, st, "turret", Tx, Ty);

            Assert.That(boxed, Is.LessThan(0.2));
            Assert.That(open.OpenFraction, Is.GreaterThan(0.99), "the cached wall mask must not outlive the walls");
            Assert.That(open.Advice, Is.Empty);

            var t = RaidFixture.Turret(ctx, st, Tx, Ty);
            RaidFixture.Guard(st, "biter", Tx + 6, Ty);
            RaidFixture.Run(ctx, st, 200);
            Assert.That(RaidFixture.Count<TurretShotEvent>(st), Is.GreaterThan(0));
            Assert.That(t.Rounds, Is.LessThan(50));
        }

        [Test]
        public void AWallBesideATurretDoesNotBlindItAlongTheOtherBearings()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);

            // One wall segment hard against the turret's east face — the tucked-in-behind-cover case.
            for (var y = Ty - 3; y <= Ty + 3; y++) RaidFixture.Add(ctx, st, "wall", Tx + 1, y);

            var cover = TurretSight.Coverage(ctx, st, "turret", Tx, Ty);

            Assert.That(cover.OpenFraction, Is.GreaterThan(TurretSight.BlindFraction),
                "one wall is cover, not blindness");
            Assert.That(cover.Advice, Does.StartWith("Restricted position"));
            Assert.That(cover.Advice, Does.Contain("W"), "and it must say which way the gun still sees");

            // West is open, east is not, and the gun behaves the same way.
            var t = RaidFixture.Turret(ctx, st, Tx, Ty);
            // Four tiles out, not six: this ground is unlit, and since U-D-59 a Gun turret reaches an alien on an
            // unlit tile only to its dark sight (6). The test is about the wall, so the body stands inside that.
            RaidFixture.Guard(st, "biter", Tx - 4, Ty);
            RaidFixture.Run(ctx, st, 100);
            Assert.That(RaidFixture.Count<TurretShotEvent>(st), Is.GreaterThan(0), "the open bearing still fires");
            Assert.That(t.Rounds, Is.LessThan(50));
        }

        /// <summary>
        /// GP-W5. A wreck stops being a barrier, and it stops being a barrier EVERYWHERE at once — the turret's
        /// coverage preview, the turret's actual line of fire, the shared walk grid and the raid's own "is this
        /// tile open to me" all read the same ruin the same way.
        ///
        /// WHY THIS IS ONE TEST AND NOT FOUR. Before GP-W5 only <see cref="DirectorRules.HostileOpen"/> knew about
        /// wrecks, so a flattened wall was something raiders walked over and the engineer walked into, while the
        /// turret behind it still believed it was blind. Those four answers have to move together or the player is
        /// being lied to by whichever one they happen to look at.
        ///
        /// The wreck STAYS on the map: <c>st.Machines.Count</c> is unchanged, which is what makes it repairable
        /// (GP-W5's other half) rather than something the player has to notice was silently deleted.
        /// </summary>
        [Test]
        public void AWreckedWallStopsBlockingSightMovementAndRoutingAllAtOnce()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            for (var y = Ty - 2; y <= Ty + 2; y++)
                for (var x = Tx - 2; x <= Tx + 2; x++)
                    if (x != Tx || y != Ty) RaidFixture.Add(ctx, st, "wall", x, y);
            var walls = st.Machines.Count;

            Assert.That(TurretSight.Coverage(ctx, st, "turret", Tx, Ty).OpenFraction, Is.LessThan(0.2),
                "standing walls are still walls");
            Assert.That(Ground.Passable(ctx, st, Tx + 1, Ty), Is.False, "and the engineer cannot walk through one");
            Assert.That(DirectorRules.HostileOpen(ctx, st, Tx + 1, Ty), Is.False, "nor can a raider");

            foreach (var m in st.Machines)
                if (m.Kind == "wall") TurretRules.Damage(ctx, st, m, TurretRules.MaxHp(ctx.Data, m));

            foreach (var m in st.Machines)
                if (m.Kind == "wall") Assert.That(TurretRules.Wrecked(ctx.Data, st, m), Is.True);
            Assert.That(st.Machines.Count, Is.EqualTo(walls),
                "a wreck still stands — it is rubble the player can repair, not a deletion");

            var open = TurretSight.Coverage(ctx, st, "turret", Tx, Ty);
            Assert.That(open.OpenFraction, Is.GreaterThan(0.99), "the preview must not draw a wall that fell down");
            Assert.That(open.Advice, Is.Empty);
            Assert.That(Ground.Passable(ctx, st, Tx + 1, Ty), Is.True, "the engineer walks over the rubble");
            Assert.That(DirectorRules.HostileOpen(ctx, st, Tx + 1, Ty), Is.True, "and so does the raid");

            // And the gun agrees with all three: the shot the ring of walls refused now lands.
            var t = RaidFixture.Add(ctx, st, "turret", Tx, Ty);
            t.Rounds = 50;
            RaidFixture.Power(ctx, st, Tx + 4, Ty + 4);
            RaidFixture.Guard(st, "biter", Tx + 6, Ty);
            RaidFixture.Run(ctx, st, 200);

            Assert.That(RaidFixture.Count<TurretShotEvent>(st), Is.GreaterThan(0));
            Assert.That(t.Rounds, Is.LessThan(50));
        }

        /// <summary>
        /// A courtyard the gun actually FITS in: a ring of wall around a 2×2 hole, because a Gun turret's footprint
        /// is 2×2 and <see cref="Placement"/> would refuse the 1×1 hole the older tests bypass with a direct Add.
        /// </summary>
        private static void Courtyard(SimContext ctx, SimState st)
        {
            for (var y = Ty - 2; y <= Ty + 3; y++)
                for (var x = Tx - 2; x <= Tx + 3; x++)
                    if (x < Tx || x > Tx + 1 || y < Ty || y > Ty + 1)
                        RaidFixture.Add(ctx, st, "wall", x, y);
        }

        /// <summary>
        /// REL-15 (INT-11): "a turret inside the workshop walls never fires", and the audit called it legal, silent
        /// and fatal. Legal it stays — a gun covering one doorway is a real choice, and GP-W3 already decided to
        /// warn rather than refuse. What it must not be is silent AFTER the build: the sentence the build cursor
        /// showed has to still be there on the turret's own card, in the same words.
        /// </summary>
        [Test]
        public void ATurretBoxedInSaysSoOnItsOwnCardAndIsStillAllowedToStandThere()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            Courtyard(ctx, st);

            Assert.That(Placement.GeometryProblem(ctx, st, "turret", Tx, Ty, Dir.N), Is.Empty,
                "the position is not refused — GP-W3 warns about a blind spot, it does not forbid one");

            var advice = TurretSight.Coverage(ctx, st, "turret", Tx, Ty).Advice;
            Assert.That(advice, Does.StartWith("Blind position"), "the build cursor's own sentence");

            var t = RaidFixture.Add(ctx, st, "turret", Tx, Ty);
            var card = ProductionQueries.Description(ctx, st, t.Id);

            Assert.That(card, Does.Contain(advice),
                "the built turret says exactly what the cursor said — one state, one vocabulary");
        }

        /// <summary>REL-15: no news is still good news. An ordinary turret's card gains nothing.</summary>
        [Test]
        public void ATurretWithAClearFieldOfFireAddsNothingToItsCard()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            var t = RaidFixture.Add(ctx, st, "turret", Tx, Ty);

            var card = ProductionQueries.Description(ctx, st, t.Id);

            Assert.That(card, Does.Not.Contain("Blind position"));
            Assert.That(card, Does.Not.Contain("Restricted position"));
        }

        /// <summary>
        /// REL-15: the card follows the world. The coverage sweep is far too heavy for a hover that asks every
        /// frame, so it is cached on <see cref="SimState.Rev"/> — and this is the test that the cache cannot go
        /// stale, because knocking the walls down is exactly the moment a player would look at the card again.
        /// </summary>
        [Test]
        public void KnockingTheWallsDownTakesTheWarningOffTheCardAgain()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            Courtyard(ctx, st);
            var t = RaidFixture.Add(ctx, st, "turret", Tx, Ty);

            Assert.That(ProductionQueries.Description(ctx, st, t.Id), Does.Contain("Blind position"));

            foreach (var m in st.Machines)
                if (m.Kind == "wall") TurretRules.Damage(ctx, st, m, TurretRules.MaxHp(ctx.Data, m));

            Assert.That(ProductionQueries.Description(ctx, st, t.Id), Does.Not.Contain("Blind position"),
                "a wrecked wall stops nothing, and the card must not still be reading the old sweep");
        }
    }
}
