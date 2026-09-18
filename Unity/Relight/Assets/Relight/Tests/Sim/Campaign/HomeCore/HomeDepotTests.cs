using NUnit.Framework;
using Relight.Sim.Tests.Combat;

namespace Relight.Sim.Tests.Campaign
{
    /// <summary>
    /// Phase C integrated run (2026-09-14): every craft on the imported region answered "Walk closer to Home
    /// workshop" because no Depot was ever placed. Reference flow.ts:400 puts one in the HQ lot at new-game time.
    /// </summary>
    [TestFixture]
    public sealed class HomeDepotTests
    {
        private static SimContext Context(bool withCore = true) => RaidFixture.Context(withCore: withCore);

        /// <summary>A new game with an authored core site has exactly one Depot, inside the core rect.</summary>
        [Test]
        public void NewGamePlacesOneDepotInsideTheAuthoredCore()
        {
            var ctx = Context();
            var st = Simulation.NewGame(ctx, 7).State;

            var depot = HomeCore.Depot(st);
            Assert.That(depot, Is.Not.Null, "the workbench stands in Home from the start");
            Assert.That(st.Machines.FindAll(m => m.Kind == HomeCore.DepotKind).Count, Is.EqualTo(1));
            var (hx, hy, hw, hh) = HomeQueries.CoreRect(st);
            Assert.That(depot.X, Is.GreaterThanOrEqualTo(hx));
            Assert.That(depot.Y, Is.GreaterThanOrEqualTo(hy));
            Assert.That(depot.X + depot.Size, Is.LessThanOrEqualTo(hx + hw));
            Assert.That(depot.Y + depot.Size, Is.LessThanOrEqualTo(hy + hh));
            Assert.That((depot.X, depot.Y), Is.EqualTo((RaidFixture.CoreX + 1, RaidFixture.CoreY + 1)), "centred in the 8x8 site");
        }

        /// <summary>The initialiser is idempotent: running it again on a state with a Depot adds nothing.</summary>
        [Test]
        public void EnsureDepotNeverAddsASecond()
        {
            var ctx = Context();
            var st = Simulation.NewGame(ctx, 7).State;
            var first = HomeCore.Depot(st);
            Assert.That(HomeCore.EnsureDepot(ctx, st), Is.SameAs(first));
            Assert.That(st.Machines.FindAll(m => m.Kind == HomeCore.DepotKind).Count, Is.EqualTo(1));
        }

        /// <summary>Standing at Home, the engineer can hand craft — with or without the Depot present (older saves).</summary>
        [Test]
        public void CraftingIsAllowedAtHomeWithAndWithoutTheDepot()
        {
            var ctx = Context();
            var st = Simulation.NewGame(ctx, 7).State;
            st.Engineer.Pos = new Vec2(RaidFixture.CoreX + 4, RaidFixture.CoreY + 4);
            Assert.That(HandCraft.NearDepot(ctx, st), Is.True, "next to the Depot");

            st.Machines.RemoveAll(m => m.Kind == HomeCore.DepotKind);
            st.Rev++;
            Assert.That(HandCraft.NearDepot(ctx, st), Is.True, "a pre-fix save still crafts in the authored workshop");

            st.Engineer.Pos = new Vec2(RaidFixture.CoreX + 40, RaidFixture.CoreY + 40);
            Assert.That(HandCraft.NearDepot(ctx, st), Is.False, "far from Home nothing is near");
        }

        /// <summary>The synthetic map has no core site: the fallback core gets no Depot and crafting still needs one placed.</summary>
        [Test]
        public void TheFallbackCoreGetsNoDepot()
        {
            var ctx = Context(withCore: false);
            var st = Simulation.NewGame(ctx, 7).State;
            Assert.That(HomeQueries.CoreIsFallback(st), Is.True);
            Assert.That(HomeCore.Depot(st), Is.Null);
            Assert.That(HandCraft.NearDepot(ctx, st), Is.False);
        }
    }
}
