using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Relight.Sim.Tests.Combat
{
    /// <summary>
    /// C-08 bodies. The rules pinned here are campaignThreat.ts <c>walkField</c>/<c>tickCampaignThreat</c> and
    /// gameplayCombat.ts <c>tickRaidAttack</c>/<c>tickSpit</c>, plus the birth roster parity.
    /// </summary>
    public sealed class EnemyTests
    {
        private static readonly List<ITickPhase> Combat =
            new List<ITickPhase> { new PowerPhase(), new TurretPhase(), new EnemyPhase() };

        /// <summary>
        /// campaignThreat.ts:434: a body whose next field tile holds a standing defence bites it for
        /// <c>structureDps * dt</c> instead of walking through. It arrives, it chews, the wall loses hit points.
        /// </summary>
        [Test]
        public void ASkitterReachesAWallAndChewsItAtTheStructureRate()
        {
            // A pocket of building tiles with one gap, and the gap plugged by the wall: the breach field has no way
            // round it, which is exactly the case the reference's breach routing exists for.
            var pocket = new List<TileRect>
            {
                new TileRect(56, 74, 5, 1), new TileRect(56, 79, 5, 1), new TileRect(56, 75, 1, 4),
                new TileRect(60, 75, 1, 1), new TileRect(60, 77, 1, 2),
            };
            var ctx = RaidFixture.Context(RaidFixture.Map(pocket));
            var st = RaidFixture.State(ctx);
            var wall = RaidFixture.Add(ctx, st, "wall", 60, 76);
            RaidFixture.Body(st, "skitter", 57.5, 76.5);          // three tiles west of the gap, core due east

            var max = TurretRules.MaxHp(ctx.Data, wall);
            Assert.That(max, Is.GreaterThan(0), "a wall is a defence with hit points");

            RaidFixture.Run(ctx, st, 200, Combat);                // 10 s: ~1.5 s to walk, the rest biting

            var hp = TurretRules.Hp(ctx.Data, st, wall);
            Assert.That(hp, Is.LessThan(max), "the wall is being attacked");
            var spent = max - hp;
            var dps = ctx.Data.Raids.StructureDps;
            Assert.That(spent, Is.LessThanOrEqualTo(10 * dps + 1e-6), "never faster than the catalogue rate");
            Assert.That(spent, Is.GreaterThan(4 * dps), "and it did not spend the ten seconds walking");
            Assert.That(RaidFixture.Last<StructureDamagedEvent>(st), Is.Not.Null);
        }

        /// <summary>
        /// gameplayCombat.ts <c>tickSpit</c>: a glob is swept against the same solids the bodies walk, so a wall of
        /// building tiles between the spitter and its aim point stops it — it never arrives by teleporting past a
        /// corner. The same shot with the wall removed does arrive.
        /// </summary>
        [Test]
        public void ASpitterGlobIsStoppedByASolidTile()
        {
            var blocked = Fired(withWall: true);
            var open = Fired(withWall: false);
            Assert.That(open, Is.GreaterThan(0), "with a clear line the glob crosses the gap");
            Assert.That(blocked, Is.Zero, "a solid building tile stops it");
        }

        /// <summary>How many globs were still alive after the flight time, with and without a wall in the way.</summary>
        private static int Fired(bool withWall)
        {
            var solids = withWall
                ? new List<TileRect> { new TileRect(64, 70, 1, 12) }
                : new List<TileRect>();
            var ctx = RaidFixture.Context(RaidFixture.Map(solids));
            var st = RaidFixture.State(ctx);
            var e = RaidFixture.Guard(st, "spitter", 60.5, 76.5);
            // Fire by hand from the same state the windup leaves behind, so the test is about flight, not aiming.
            e.Aim = new Vec2(70.5, 76.5);
            e.Phase = EnemyPhaseKind.Windup;
            e.Until = st.T;
            st.Engineer.Pos = new Vec2(5, 5);

            var phases = new List<ITickPhase> { new EnemyPhase() };
            var travelled = 0;
            for (var i = 0; i < 20; i++)                       // 1 s at 8 tiles/s: well past the wall
            {
                RaidFixture.Run(ctx, st, 1, phases);
                for (var p = 0; p < st.Enemies.Projectiles.Count; p++)
                    if (st.Enemies.Projectiles[p].Pos.X > 65) travelled++;
            }
            return travelled;
        }

        /// <summary>
        /// campaignThreat.ts:192: the roster reads its counter AFTER incrementing it, so ids 3, 6, 9 … are the
        /// heavier body. In the port's two-type roster that is the spitter, and a <c>basic</c> group never has one.
        /// </summary>
        [Test]
        public void EveryThirdBodyIsASpitterUnlessTheGroupIsBasic()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            var origin = 76 * RaidFixture.Size + 40;             // open ground well west of the core

            var kinds = new List<string>();
            for (var i = 0; i < 9; i++)
            {
                var id = DirectorRules.Birth(ctx, st, origin, EnemyLayer.Minor, 1, false);
                Assert.That(id, Is.Not.Zero, "the ground is open");
                kinds.Add(st.Enemies.Find(id).Kind);
            }
            Assert.That(kinds, Is.EqualTo(new List<string>
            {
                "skitter", "spitter", "skitter", "skitter", "spitter", "skitter", "skitter", "spitter", "skitter",
            }), "ids 2, 5 and 8 are the third, sixth and ninth draws of the counter");

            var st2 = RaidFixture.State(ctx);
            for (var i = 0; i < 9; i++)
            {
                var id = DirectorRules.Birth(ctx, st2, origin, EnemyLayer.Minor, 1, true);
                Assert.That(st2.Enemies.Find(id).Kind, Is.EqualTo("skitter"), "a basic group is skitters only");
            }
        }

        /// <summary>A body at 0 hp leaves the map and says so once, naming what killed it.</summary>
        [Test]
        public void ABodyDiesAtZeroHitPoints()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            var e = RaidFixture.Guard(st, "skitter", 40.5, 40.5, hp: 20);

            Assert.That(Enemies.Damage(ctx, st, e.Id, 5), Is.False, "still standing");
            Assert.That(RaidFixture.Count<EnemyKilledEvent>(st), Is.Zero);

            Assert.That(Enemies.Damage(ctx, st, e.Id, 15), Is.True, "that was the last of it");
            Assert.That(st.Enemies.Find(e.Id), Is.Null, "it is off the map");
            Assert.That(RaidFixture.Count<EnemyKilledEvent>(st), Is.EqualTo(1));
            Assert.That(RaidFixture.Last<EnemyKilledEvent>(st).ByTurret, Is.True);
            Assert.That(EnemyQueries.Count(st), Is.Zero);
        }

        /// <summary>
        /// A save taken in the middle of a windup resumes mid-windup: the phase, its deadline and the aim point the
        /// attack committed to all round-trip, so the body does not get a free second swing after a load.
        /// </summary>
        [Test]
        public void ASaveTakenMidWindupResumesMidWindup()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            st.T = 123.45;
            var e = RaidFixture.Guard(st, "spitter", 60.5, 76.5);
            e.Phase = EnemyPhaseKind.Windup;
            e.Until = st.T + 0.4;
            e.Aim = new Vec2(70.5, 76.25);
            e.Waypoint = 77 * RaidFixture.Size + 61;
            e.Withdrawing = true;
            e.Stuck = 1.5;
            st.Enemies.Projectiles.Add(new EnemyProjectile
            {
                Pos = new Vec2(62.5, 76.5), Vel = new Vec2(8, 0), Life = 1.25, Source = e.Id,
            });

            var doc = SaveSerializer.WriteText(st, ctx.Data);
            var result = SaveSerializer.ReadText(doc, ctx.Data);
            Assert.That(result.Ok, Is.True, result.Reason);
            var loaded = result.State;

            var back = loaded.Enemies.Find(e.Id);
            Assert.That(back, Is.Not.Null);
            Assert.That(back.Kind, Is.EqualTo("spitter"));
            Assert.That(back.Phase, Is.EqualTo(EnemyPhaseKind.Windup));
            Assert.That(back.Until, Is.EqualTo(e.Until).Within(1e-9));
            Assert.That(back.Aim.X, Is.EqualTo(70.5).Within(1e-9));
            Assert.That(back.Aim.Y, Is.EqualTo(76.25).Within(1e-9));
            Assert.That(back.Waypoint, Is.EqualTo(e.Waypoint));
            Assert.That(back.Withdrawing, Is.True);
            Assert.That(back.Stuck, Is.EqualTo(1.5).Within(1e-9));
            Assert.That(loaded.Enemies.Projectiles.Count, Is.EqualTo(1));
            Assert.That(loaded.Enemies.Projectiles[0].Life, Is.EqualTo(1.25).Within(1e-9));
            Assert.That(loaded.Enemies.Next, Is.EqualTo(st.Enemies.Next), "ids never restart after a load");

            // The seam is transient by design: a load leaves it null until the initialiser or the phase re-points it.
            Assert.That(loaded.Weapons.Targets, Is.Null);
            new EnemyInitializer().Init(ctx, loaded);
            Assert.That(loaded.Weapons.Targets, Is.Not.Null);
            Assert.That(loaded.Weapons.Targets.Count, Is.EqualTo(1));
        }

        /// <summary>
        /// REL-62 (ENM-07). A bolt already in the air when the player saved must hit the body on the FIRST tick after
        /// the load — and this test never points the seam by hand, which is the whole point of it. The seam is
        /// transient, a load runs no initialisers, and the weapon phase runs BEFORE the enemy phase that re-points
        /// it, so for one tick the player's own shots looked out at nothing and passed through every body on the map.
        /// </summary>
        [Test]
        public void ALoadedGameHitsBodiesOnItsFirstTick()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            var e = RaidFixture.Guard(st, "spitter", 84.5, 76.5, hp: 50);
            Assert.That(ctx.Data.TryWeapon("plasma", out var bolt), Is.True);
            st.Engineer.Pos = new Vec2(84.0, 76.5);
            Ballistics.Fire(ctx, st, bolt, 84.5, 76.5);      // half a tile short of the body: one tick's travel
            Assert.That(st.Weapons.Projectiles.Count, Is.EqualTo(1), "a bolt is in the air when the game is saved");

            var doc = SaveSerializer.WriteText(st, ctx.Data);
            var read = SaveSerializer.ReadText(doc, ctx.Data);
            Assert.That(read.Ok, Is.True, read.Reason);
            Assert.That(read.State.Weapons.Targets, Is.Null, "the seam is never saved");

            var loaded = Simulation.Wrap(ctx, read.State).State;     // exactly what loading a game does
            Assert.That(loaded.Weapons.Targets, Is.Not.Null, "and the load points it before any tick runs");

            // The real tick order: Weapons first, the phase that re-points the seam third.
            RaidFixture.Run(ctx, loaded, 1, new List<ITickPhase>
            {
                new WeaponPhase(), new TurretPhase(), new EnemyPhase(), new DirectorPhase(),
            });
            var body = loaded.Enemies.Find(e.Id);
            Assert.That(body, Is.Not.Null, "50 hit points, and the bolt does 25");
            Assert.That(body.Hp, Is.LessThan(50), "the bolt in the air hit it on the first tick after the load");
        }

        /// <summary>The player's rifle reaches bodies through the same seam the turret phase re-points every tick.</summary>
        [Test]
        public void ThePlayerProjectileSeamSeesTheBodies()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            var e = RaidFixture.Guard(st, "skitter", 40.5, 40.5, hp: 20);

            var targets = st.Weapons.Targets;
            Assert.That(targets, Is.Not.Null);
            Assert.That(targets.Count, Is.EqualTo(1));
            Assert.That(targets.At(0).X, Is.EqualTo(40.5).Within(1e-9));

            targets.Hit(ctx, st, 0, 20, new Vec2(30, 40));
            Assert.That(st.Enemies.Find(e.Id), Is.Null, "a rifle kills it like a turret does");
            Assert.That(RaidFixture.Last<EnemyKilledEvent>(st).ByTurret, Is.False, "but it is not booked as a turret kill");
        }

        // ------------------------------------------------------------------ GP-W5: perception and the leash

        /// <summary>
        /// GP-W5. A raider's eyes are the TABLES' — <c>raids.NoticeTiles</c> to notice, widening to
        /// <c>raids.ChaseEscapeTiles</c> once it is aware — and not the 1.2-tile contact radius the raid branch
        /// used to read, which meant a raider only ever "noticed" the engineer by walking into him and the player
        /// could shoot a wave in the back from three tiles away all day.
        ///
        /// Three cases are the whole of the rule: inside the radius with a clear line it notices; beyond it, it
        /// does not; and a building between the two hides the engineer at a distance it would otherwise see him
        /// at, so breaking line of sight is still the escape.
        /// </summary>
        [Test]
        public void ARaiderNoticesTheEngineerAtThePerceptionRadiusTheTablesCarry()
        {
            var raids = ReferenceData.Create().Raids;
            Assert.That(raids.NoticeTiles, Is.GreaterThan(4),
                "the tables carry a real perception radius, not a contact radius");

            Assert.That(Notices(raids.NoticeTiles - 2, wall: false), Is.True, "inside the radius, in the open");
            Assert.That(Notices(raids.NoticeTiles + 4, wall: false), Is.False, "beyond it, nothing is seen");
            Assert.That(Notices(raids.NoticeTiles - 2, wall: true), Is.False,
                "and a building on the line hides him at the very distance he was just seen at");
        }

        /// <summary>
        /// One tick of a single skitter <paramref name="gap"/> tiles west of the engineer: did it come to know
        /// where he is? Optionally with an authored building wall standing halfway between the two.
        /// </summary>
        private static bool Notices(double gap, bool wall)
        {
            const double ex = RaidFixture.CoreX + 0.5, ey = RaidFixture.CoreY + 0.5;
            var solids = wall
                ? new List<TileRect> { new TileRect((int)(ex - gap / 2), (int)ey - 4, 1, 9) }
                : null;
            var ctx = RaidFixture.Context(RaidFixture.Map(solids));
            var st = RaidFixture.State(ctx);
            st.Engineer.Pos = new Vec2(ex, ey);
            var e = RaidFixture.Body(st, "skitter", ex - gap, ey);

            RaidFixture.Run(ctx, st, 1, Combat);
            return e.LastKnownUntil > st.T;
        }

        /// <summary>
        /// GP-W5's readable leash, and the reason the old rule made retreating from a camp free.
        ///
        /// The camps used to break off on <c>Distance(lastKnown, home) &lt; 36</c> — a circle drawn around the CAMP,
        /// measured against where the ENGINEER was, re-evaluated every tick, and the failing branch wiped the
        /// body's memory. So the moment the player stepped over a line nothing on screen draws, every resident
        /// forgot him mid-stride. A resident now commits: it chases until IT is <c>GuardPursuitTiles</c> from its
        /// own camp, then breaks off and walks back — still remembering him, which is the point — and will not
        /// turn round again until it is back inside <c>GuardReengageTiles</c>, even though it can see him the
        /// whole way.
        /// </summary>
        [Test]
        public void ACampResidentChasesToItsLeashThenWalksHomeAndOnlyTurnsRoundNearTheCamp()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            var s = ctx.Data.Siege;
            var phases = new List<ITickPhase> { new EnemyPhase() };
            const double homeX = 30.5, homeY = 100.5;

            st.Engineer.Pos = new Vec2(homeX + 4, homeY);
            var g = RaidFixture.Guard(st, "skitter", homeX, homeY);
            RaidFixture.Run(ctx, st, 1, phases);
            Assert.That(g.OnPlayer, Is.True, "a resident notices an engineer four tiles away");
            Assert.That(g.Withdrawing, Is.False, "and sets off after him");

            // The player backs away at the resident's own pace, staying visible and inside the escape radius the
            // whole time. This is exactly the retreat the old rule cancelled for free.
            var brokeAt = -1.0;
            for (var i = 0; i < 20 * 120 && brokeAt < 0; i++)
            {
                st.Engineer.Pos = new Vec2(g.Pos.X + 10, homeY);
                var before = DirectorRules.Distance(g.Pos.X, g.Pos.Y, g.Home.X, g.Home.Y);
                RaidFixture.Run(ctx, st, 1, phases);
                if (g.Withdrawing) brokeAt = before;
                else Assert.That(before, Is.LessThanOrEqualTo(s.GuardPursuitTiles), "it did not break off early");
            }

            Assert.That(brokeAt, Is.GreaterThan(s.GuardPursuitTiles), "it chased its whole leash out");
            Assert.That(brokeAt, Is.LessThan(s.GuardPursuitTiles + 1),
                "and broke off on the tick the leash ran out, not somewhere vague afterwards");
            Assert.That(g.OnPlayer, Is.False, "walking home is not a chase, and the HUD says so");
            Assert.That(g.LastKnownUntil, Is.GreaterThan(st.T),
                "but it has NOT forgotten him — breaking off is a choice, not amnesia");

            // On the way back it can still see him the whole time and still will not turn round.
            var turnedAt = -1.0;
            for (var i = 0; i < 20 * 120 && turnedAt < 0; i++)
            {
                st.Engineer.Pos = new Vec2(g.Pos.X + 10, homeY);
                var before = DirectorRules.Distance(g.Pos.X, g.Pos.Y, g.Home.X, g.Home.Y);
                RaidFixture.Run(ctx, st, 1, phases);
                if (!g.Withdrawing) turnedAt = before;
                else Assert.That(before, Is.GreaterThan(s.GuardReengageTiles),
                    "still outside the re-engage ring, and still walking home");
            }

            Assert.That(turnedAt, Is.GreaterThan(0), "it did turn round eventually");
            Assert.That(turnedAt, Is.LessThanOrEqualTo(s.GuardReengageTiles),
                "and only once it was most of the way home");
            Assert.That(g.OnPlayer, Is.True, "and it is a chase again");
        }
    }
}
