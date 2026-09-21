using System.Collections.Generic;
using NUnit.Framework;
using Relight.Sim.Tests.Combat;
using Relight.Sim.UI;

namespace Relight.Sim.Tests.Campaign
{
    /// <summary>
    /// OPN-07 (REL-72): the core-down goal card says only what the sim does.
    ///
    /// The old card was titled "Restore Home power", told the player to "hold E" and promised that the repair
    /// "restarts every machine that was running". None of that exists in Unity: the core powers nothing, nothing
    /// stops when it falls, and the repair is a button in the Home workshop. What a fallen core really does is
    /// E-18's failure path (REL-44), and that is what the card now describes.
    ///
    /// The second test does not set the core to 0 by hand. It lets a real large raid take it, and then holds each
    /// sentence on the card against the state the raid left behind.
    /// </summary>
    public sealed class CoreDownCardTests
    {
        private static ObjectiveView Card(SimContext ctx, SimState st)
        {
            var card = OpeningQueries.Objective(ctx, st);
            Assert.That(card.Id, Is.EqualTo("home-recovery"), "the core-down card is showing");
            return card;
        }

        /// <summary>The whole combat stack, with the phase that runs a repair.</summary>
        private static List<ITickPhase> Phases()
        {
            var list = RaidFixture.Phases();
            list.Add(new HomeCorePhase());
            return list;
        }

        [Test]
        public void TheCardMakesNoneOfTheOldFalseClaims_AndNamesTheRealButton()
        {
            var ctx = OpeningFixture.Context();
            var st = OpeningFixture.State(ctx);
            HomeCore.Damage(st, 1e9);

            var card = Card(ctx, st);
            var all = card.Title + " " + card.Text + " " + card.Detail;
            Assert.That(card.Title, Does.Not.Contain("power"), "the core powers nothing, so nothing lost power");
            Assert.That(all, Does.Not.Contain("hold E"), "the repair is a button in the workshop, never a hold");
            Assert.That(all, Does.Not.Contain("restart"), "nothing stopped, so nothing restarts");
            Assert.That(all, Does.Not.Contain("Home is disabled"), "only the core is down; Home carries on");

            // The card quotes the workshop's own button, so the two cannot drift apart.
            var button = WorkshopText.CoreButton(false, 0, true, ctx.Data.Defence.CoreRepairSeconds, 0);
            Assert.That(button, Does.StartWith(OpeningQueries.RecommissionButton));
            Assert.That(card.Text, Does.Contain(OpeningQueries.RecommissionButton));
            Assert.That(card.Detail, Does.Contain(OpeningQueries.RecommissionButton));
            Assert.That(card.Detail, Does.Contain("Hand mining"), "the player is told what they can still do");
        }

        [Test]
        public void ARealRaidTakesTheCore_AndEverySentenceOnTheCardHolds()
        {
            var ctx = OpeningFixture.Context();
            var st = OpeningFixture.State(ctx);
            var d = st.Director;
            d.NextMinor = 1e9;                 // one raid at a time: this is about the large one
            st.Admin.Invulnerable = true;      // the engineer is not under test; a downed engineer outranks this card

            // A powered turret with an empty hopper, well away from the core: it draws no fire and fires none, and
            // it is here only so "your power, machines and turrets keep running" has something to be true about.
            var turret = RaidFixture.Turret(ctx, st, 12, 12, rounds: 0);
            foreach (var m in st.Machines) if (m.Kind == "generator") m.Inv.Add(ItemId.Coal, 2000);
            RaidFixture.Run(ctx, st, 2, Phases());
            Assert.That(PowerQueries.Supplied(ctx, st, turret.Id), Is.True, "the fixture turret starts powered");

            // Warn, commit, and let the raid do the work. Nothing defends the core.
            st.T = d.NextStart - ctx.Data.Raids.WarningS;
            RaidFixture.Run(ctx, st, 1, Phases());
            Assert.That(d.Major, Is.Not.Null, "the fixture map must be able to stage an assault");
            var raid = d.Major;
            st.T = raid.StartsAt;
            var limit = (int)(900 / RaidFixture.Dt);
            var ticks = 0;
            while (HomeQueries.CoreOperational(st) && ticks++ < limit) RaidFixture.Run(ctx, st, 1, Phases());
            Assert.That(HomeQueries.CoreOperational(st), Is.False, "an undefended core falls to a large raid");
            RaidFixture.Run(ctx, st, 1, Phases());
            var fellAt = st.T;

            var card = Card(ctx, st);
            Assert.That(card.Title, Is.EqualTo("Repair the Home core"));

            // "The raid that beat it has been called off"
            Assert.That(d.Major, Is.Null, "the assault ended when the core fell");
            Assert.That(d.History[d.History.Count - 1].Id, Is.EqualTo(raid.Id));
            Assert.That(d.History[d.History.Count - 1].Outcome, Is.EqualTo((int)RaidOutcome.Lost));
            Assert.That(card.Detail, Does.Contain("has been called off"));

            // "no raid attacks a core that is down"
            Assert.That(DirectorRules.Target(ctx, st, out _, out _, out _), Is.False, "a downed core is not a target");
            Assert.That(card.Detail, Does.Contain("no raid attacks a core that is down"));

            // "After a large assault takes the core, the next one waits a full interval."
            Assert.That(d.NextStart, Is.GreaterThanOrEqualTo(fellAt + ctx.Data.Raids.IntervalMinS - 1));
            Assert.That(card.Detail, Does.Contain("the next one waits a full interval"));

            // "The repair waits while raiders are close to the core": the card's short line says so while they are
            // there, and the sim refuses the repair for the same reason.
            st.Engineer.Pos = new Vec2(RaidFixture.CoreX + 4, RaidFixture.CoreY + 4);
            st.Engineer.Inv[ItemId.Steel] = ctx.Data.Defence.CoreSteel;
            st.Engineer.Inv[ItemId.Copper] = ctx.Data.Defence.CoreCopper;
            Assert.That(Home.AttackersNearby(ctx, st), Is.True, "the raiders that took the core are still on it");
            Assert.That(card.Text, Does.Contain("the repair waits until they leave"));
            Assert.That(HomeCore.RepairProblem(ctx, st, RepairKinds.Core, -1), Is.EqualTo("wait for the attackers to leave"));

            // The survivors leave by themselves; nobody shoots them.
            limit = (int)((DirectorRules.WithdrawPurgeS + 5) / RaidFixture.Dt);
            ticks = 0;
            while (EnemyQueries.GroupAlive(st, raid.Id) > 0 && ticks++ < limit) RaidFixture.Run(ctx, st, 1, Phases());
            Assert.That(EnemyQueries.GroupAlive(st, raid.Id), Is.EqualTo(0), "every survivor has left the map");

            // "Your power, machines and turrets keep running"
            Assert.That(HomeQueries.CoreOperational(st), Is.False, "the core has been down all this while");
            Assert.That(PowerQueries.Supplied(ctx, st, turret.Id), Is.True, "and the turret never lost power");

            // "press E to open the Home workshop and choose Recommission core"
            card = Card(ctx, st);
            Assert.That(card.Text, Is.EqualTo("Press E at the Home core, then Recommission core in the workshop"));
            Assert.That(HomeQueries.RepairCard(st, ctx.Data).Recommission, Is.True, "the workshop offers the recommission");
            Assert.That(new HomeCoreHandler().TryApply(ctx, st, new RepairCommand(RepairKinds.Core, -1), out var r), Is.True);
            Assert.That(r.Problem, Is.Empty, "one command, no hold: the repair starts");
            Assert.That(Card(ctx, st).Text, Does.StartWith("Repairing the Home core"));

            // "it restores the core to full"
            limit = (int)(120 / RaidFixture.Dt);
            ticks = 0;
            while (!HomeQueries.CoreOperational(st) && ticks++ < limit) RaidFixture.Run(ctx, st, 1, Phases());
            Assert.That(st.Home.Hp, Is.EqualTo((double)ctx.Data.Defence.CoreHp),
                "a recommission brings the core back at full health");
            Assert.That(OpeningQueries.Objective(ctx, st).Id, Is.Not.EqualTo("home-recovery"), "and the card is gone");
        }
    }
}
