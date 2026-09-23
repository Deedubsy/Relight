using System.Collections.Generic;
using NUnit.Framework;

namespace Relight.Sim.Tests.Combat
{
    /// <summary>
    /// REL-124 (CMB-15): a raid aims at its target's real footprint, not a square on its longer side.
    ///
    /// The Home workshop is 10 wide and 14 high. Before this fix the director reported a 14×14 square, which ran four
    /// tiles past the core's east wall: a raider standing next to the square bit the core through whatever the player
    /// had built there, and a glob that struck anything solid in the empty part of the square hurt the core too.
    /// Every other raid fixture uses a square core, which is why none of them could see it. This one uses the real
    /// shape, with the core's own tiles solid as they are on the imported city.
    /// </summary>
    public class RaidTargetRectTests
    {
        private const int X = RaidFixture.CoreX, Y = RaidFixture.CoreY, W = 10, H = 14;

        private static SimContext Context(params TileRect[] extra)
        {
            var solids = new List<TileRect> { new TileRect(X, Y, W, H) };
            solids.AddRange(extra);
            var sites = new WorldSites(new List<SiteRecord>
            {
                new SiteRecord("home", "Home Court", SiteKind.Core, X, Y, W, H, "", 0),
            });
            return new SimContext(ReferenceData.Create(), RaidFixture.Map(solids), null, null, sites);
        }

        private static SimState State(SimContext ctx)
        {
            var st = RaidFixture.State(ctx);
            st.Engineer.Pos = new Vec2(10.5, 10.5);
            st.Home.Placed = true;
            st.Home.X = X; st.Home.Y = Y; st.Home.W = W; st.Home.H = H;
            st.Home.Hp = ctx.Data.Defence.CoreHp;
            return st;
        }

        private static List<ITickPhase> Enemies() => new List<ITickPhase> { new EnemyPhase() };

        [Test]
        public void TheTargetIsTheCoresRealWidthAndHeight()
        {
            var ctx = Context();
            var st = State(ctx);
            Assert.That(DirectorRules.Target(ctx, st, out var x, out var y, out var w, out var h), Is.True);
            Assert.That((x, y, w, h), Is.EqualTo((X, Y, W, H)));

            // The authored site answers the same when the core was never placed.
            var unplaced = RaidFixture.State(ctx);
            Assert.That(DirectorRules.Target(ctx, unplaced, out x, out y, out w, out h), Is.True);
            Assert.That((x, y, w, h), Is.EqualTo((X, Y, W, H)));
        }

        [Test]
        public void TheRouteEndsAtTheRealWallsNotAtTheOldSquare()
        {
            var ctx = Context();
            var st = State(ctx);
            var fld = st.Director.Fields.Field(ctx, st, X, Y, W, H, false);
            Assert.That(fld.At(X + W, Y + 4), Is.Zero, "the tile touching the east wall is the end of the route");
            Assert.That(fld.At(X + H, Y + 4), Is.EqualTo(4), "the old square's edge is four steps short of the wall");
            Assert.That(fld.At(X - 1, Y + 4), Is.Zero, "west wall");
            Assert.That(fld.At(X + 4, Y + H), Is.Zero, "south wall");
        }

        [Test]
        public void ARaiderBitesTheCoreOnlyOnceItReachesTheRealWall()
        {
            var ctx = Context();
            var st = State(ctx);
            var full = st.Home.Hp;
            // Next to where the old 14×14 square ended: before REL-124 this body bit the core on its first tick.
            var e = RaidFixture.Body(st, "skitter", X + H + .5, Y + 4.5);

            double bitAt = double.NaN;
            for (var i = 0; i < 20 * 10 && double.IsNaN(bitAt); i++)
            {
                RaidFixture.Run(ctx, st, 1, Enemies());
                if (st.Home.Hp < full) bitAt = e.Pos.X;
            }
            Assert.That(bitAt, Is.Not.NaN, "the body reached the core and bit it");
            Assert.That(bitAt, Is.LessThan(X + W + 1.0), "it bit only from the tile touching the real east wall");
        }

        [Test]
        public void AGlobThatStrikesStoneOutsideTheCoreDoesNotHurtIt()
        {
            // A solid tile inside the old square but outside the real core: a player's building would stand here.
            var ctx = Context(new TileRect(X + W + 2, Y + 4, 1, 1));
            var st = State(ctx);
            var full = st.Home.Hp;
            st.Enemies.Projectiles.Add(new EnemyProjectile
            {
                Pos = new Vec2(X + H + 3.5, Y + 4.5), Vel = new Vec2(-8, 0), Life = 3, Source = -1,
            });
            RaidFixture.Run(ctx, st, 20 * 2, Enemies());
            Assert.That(st.Enemies.Projectiles.Count, Is.Zero, "the glob stopped on the stone");
            Assert.That(st.Home.Hp, Is.EqualTo(full), "outside the real footprint, so the core is untouched");
        }

        [Test]
        public void AGlobThatReachesTheRealWallHurtsTheCore()
        {
            var ctx = Context();
            var st = State(ctx);
            var full = st.Home.Hp;
            st.Enemies.Projectiles.Add(new EnemyProjectile
            {
                Pos = new Vec2(X + H + 3.5, Y + 4.5), Vel = new Vec2(-8, 0), Life = 3, Source = -1,
            });
            RaidFixture.Run(ctx, st, 20 * 2, Enemies());
            Assert.That(st.Home.Hp, Is.LessThan(full), "the glob struck the core's own east wall");
        }
    }
}
