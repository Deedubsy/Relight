using System.Collections.Generic;
using NUnit.Framework;
using Relight.Sim.Tests.Combat;
using Relight.Sim.Tests.Campaign;
using Relight.Sim.UI;

namespace Relight.Sim.Tests.UI
{
    /// <summary>
    /// INT-01 (REL-5): what the player is TOLD about the cargo a death drops. The pile and its collect command are
    /// pinned in <c>LedgerTests</c>; these hold the words around them — the standing HUD row that names the place,
    /// the "Recover at Home" row that mentions the pile, the pointer's view of a tile, and that all of it is read
    /// from saved state, so a loaded game still says where the cargo lies.
    /// </summary>
    public sealed class DroppedCargoNoticeTests
    {
        private static SimContext City()
        {
            var sites = new WorldSites(new List<SiteRecord>
            {
                new SiteRecord("home", "Home Court", SiteKind.Core, RaidFixture.CoreX, RaidFixture.CoreY, 8, 8, "", 0),
                new SiteRecord("substation:0", "Substation 0", SiteKind.Substation, 20, 20, 2, 2, "", 0),
                new SiteRecord("substation:1", "Substation 1", SiteKind.Substation, 130, 130, 2, 2, "", 0),
                new SiteRecord("label:0", "Founders Court", SiteKind.Label, 24, 24, 1, 1, "", 0),
                new SiteRecord("label:1", "Ironworks", SiteKind.Label, 134, 134, 1, 1, "", 0),
            });
            return new SimContext(ReferenceData.Create(), RaidFixture.Map(), null, null, sites);
        }

        private static void DieAt(SimContext ctx, SimState st, double x, double y, ItemId item, double count)
        {
            st.Engineer.Down = -1;
            st.Engineer.Hp = ctx.Data.Engineer.MaxHp;
            st.Engineer.Pos = new Vec2(x, y);
            st.Engineer.Inv[item] = count;
            st.Engineer.TakeDamage(ctx, st, ctx.Data.Engineer.MaxHp);
            Assert.That(st.Engineer.IsDown, Is.True);
        }

        private static HudNotice Row(HudViewModel vm)
        {
            for (var i = 0; i < vm.Notices.Rows.Count; i++)
                if (vm.Notices.Rows[i].Key == HudViewModel.DroppedCargoKey) return vm.Notices.Rows[i];
            return null;
        }

        [Test]
        public void TheRowNamesThePlace_IsPostedOnce_AndGoesWhenThePileIsEmptied()
        {
            var ctx = City();
            var st = RaidFixture.State(ctx);
            var vm = new HudViewModel();
            vm.Refresh(ctx, st, 0, false, false, force: true);
            Assert.That(Row(vm), Is.Null, "no pile, no row");

            DieAt(ctx, st, 130.5, 128.5, ItemId.Steel, 20);
            vm.Refresh(ctx, st, 1, false, false, force: true);
            var row = Row(vm);
            Assert.That(row, Is.Not.Null, "a death that drops cargo says so");
            Assert.That(row.Text, Is.EqualTo(HudViewModel.DroppedCargoText(1, "Ironworks")));
            Assert.That(row.Text, Does.Contain("Dropped cargo at Ironworks"));

            // U-D-55: a standing row is posted on a change, never per refresh. A repeat would raise the count.
            var repeats = row.Repeats;
            for (var i = 2; i < 12; i++) vm.Refresh(ctx, st, i, false, false, force: true);
            Assert.That(Row(vm).Repeats, Is.EqualTo(repeats), "re-posted on a refresh that changed nothing");

            // A second death elsewhere: several piles, the newest named.
            st.T += 30;
            DieAt(ctx, st, 22.5, 26.5, ItemId.Copper, 5);
            vm.Refresh(ctx, st, 20, false, false, force: true);
            Assert.That(st.Drops.Caches.Count, Is.EqualTo(2), "several deaths make several piles");
            Assert.That(Row(vm).Text, Is.EqualTo(HudViewModel.DroppedCargoText(2, "Founders Court")));

            // Collect both; the row goes with the last pile.
            st.Engineer.Down = -1;
            st.Engineer.Hp = ctx.Data.Engineer.MaxHp;
            Assert.That(DeathCache.Collect(ctx, st, 0, null, 0).Item1, Is.EqualTo(5));
            vm.Refresh(ctx, st, 21, false, false, force: true);
            Assert.That(Row(vm).Text, Is.EqualTo(HudViewModel.DroppedCargoText(1, "Ironworks")), "one pile left, named again");
            st.Engineer.Pos = new Vec2(130.5, 128.5);
            Assert.That(DeathCache.Collect(ctx, st, 0, null, 0).Item1, Is.EqualTo(20));
            vm.Refresh(ctx, st, 22, false, false, force: true);
            Assert.That(Row(vm), Is.Null, "an emptied pile takes its row with it");
        }

        [Test]
        public void ALoadedGameStillSaysWhereTheCargoLies()
        {
            var ctx = City();
            var st = RaidFixture.State(ctx);
            DieAt(ctx, st, 130.5, 128.5, ItemId.Steel, 20);

            var back = OpeningFixture.RoundTrip(ctx, st);
            Assert.That(DeathCache.Live(back, out var pile), Is.EqualTo(1), "the pile is saved");
            Assert.That((pile.X, pile.Y), Is.EqualTo((130, 128)));
            Assert.That(pile.Items[ItemKey.Of(ItemId.Steel)], Is.EqualTo(20));

            var vm = new HudViewModel();                 // a fresh HUD, as after a load: no event was ever seen
            vm.Refresh(ctx, back, 0, false, false, force: true);
            Assert.That(Row(vm), Is.Not.Null, "the row is read from state, not from the unsaved event");
            Assert.That(Row(vm).Text, Does.Contain("Ironworks"));
        }

        [Test]
        public void TheRecoveryRowMentionsThePile_OnlyWhenThereIsOne()
        {
            var ctx = OpeningFixture.Context();
            var st = OpeningFixture.State(ctx);
            st.Engineer.Down = st.T + 5;                 // down with nothing dropped
            var plain = OpeningQueries.Objective(ctx, st);
            Assert.That(plain.Title, Is.EqualTo("Recover at Home"));
            Assert.That(plain.Detail, Does.Not.Contain("Backpack"));

            DieAt(ctx, st, 40.5, 40.5, ItemId.Steel, 3);
            var told = OpeningQueries.Objective(ctx, st);
            Assert.That(told.Title, Is.EqualTo("Recover at Home"), "the title is pinned elsewhere and stays");
            Assert.That(told.Detail, Does.Contain("lying where you fell").And.Contain("press E"));
        }

        [Test]
        public void ThePointerSeesOnlyAPileThatHoldsSomething_AndReadsItsContents()
        {
            var ctx = City();
            var st = RaidFixture.State(ctx);
            Assert.That(DeathCache.OnTile(st, 130, 128), Is.Null);
            DieAt(ctx, st, 130.5, 128.5, ItemId.Steel, 20);

            var pile = DeathCache.OnTile(st, 130, 128);
            Assert.That(pile, Is.Not.Null);
            Assert.That(DeathCache.OnTile(st, 131, 128), Is.Null, "one tile, not a neighbourhood");
            Assert.That(DeathCache.Summary(ctx.Data, pile),
                Is.EqualTo(ctx.Data.Item(ItemId.Steel).DisplayName + " × 20"));

            pile.Items[ItemKey.Of(ItemId.Steel)] = 0;    // an emptied pile the sim has not yet removed
            Assert.That(DeathCache.OnTile(st, 130, 128), Is.Null);
            Assert.That(DeathCache.Live(st, out _), Is.EqualTo(0));
        }
    }
}
