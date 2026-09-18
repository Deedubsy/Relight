using System.Collections.Generic;
using NUnit.Framework;
using Relight.Sim.Tests.Combat;

namespace Relight.Sim.Tests.Campaign
{
    /// <summary>
    /// Correction pass R5 — "objective clarity". The owner playtest found the goal card unusable because it said
    /// "Steel plates 0/30" and nothing else: no player could tell WHERE the steel was. These checks pin the new
    /// behaviour of <see cref="OpeningQueries.Source"/> and of the sentence it appends to the objective's
    /// "Why this next?" detail.
    ///
    /// Everything is derived from the loaded map. Nothing here grants a resource, and no check asserts a number
    /// that is not already in the catalogue or the fixture's geometry.
    /// </summary>
    [TestFixture]
    public sealed class ObjectiveSourceTests
    {
        /// <summary>A 160x160 plain-ground map with the named tiles overwritten; the spawn stays at the centre.</summary>
        private static ArrayGeometry MapWith(params (int X, int Y, TileClass Kind)[] tiles)
        {
            const int size = RaidFixture.Size;
            var kind = new byte[size * size];
            for (var i = 0; i < kind.Length; i++) kind[i] = (byte)TileClass.Ground;
            foreach (var t in tiles) kind[t.Y * size + t.X] = (byte)t.Kind;
            return new ArrayGeometry(size, size, kind, null, new Vec2(size / 2.0, size / 2.0));
        }

        private static SimContext Context(ArrayGeometry map, params SiteRecord[] extra)
        {
            var sites = new List<SiteRecord>
            {
                new SiteRecord("home", "Home Court", SiteKind.Core,
                    RaidFixture.CoreX, RaidFixture.CoreY, RaidFixture.CoreSize, RaidFixture.CoreSize, "", 0),
            };
            sites.AddRange(extra);
            return new SimContext(ReferenceData.Create(), map, null, null, new WorldSites(sites));
        }

        /// <summary>The first row of the chain that has materials: "1 · Build a Generator", 30 steel + 10 copper.</summary>
        private static ObjectiveView Generator(SimContext ctx, SimState st)
        {
            var next = OpeningQueries.Objective(ctx, st);
            Assert.That(next.Title, Is.EqualTo("1 · Build a Generator"), "the fixture must still be on row 1");
            Assert.That(next.Materials, Is.Not.Empty);
            return next;
        }

        // ------------------------------------------------------------------ an authored patch

        [Test]
        public void AShortRowNamesTheNearestAuthoredResourcePatch()
        {
            // 20 east and 20 north of the spawn (80,80): north-east, ~28 tiles away.
            var ctx = Context(MapWith((RaidFixture.CoreX + 20, RaidFixture.CoreY - 20, TileClass.Rubble)),
                new SiteRecord("resource:steel", "steel salvage", SiteKind.Resource,
                    RaidFixture.CoreX + 20, RaidFixture.CoreY - 20, 4, 4, "steel", 400));
            var st = OpeningFixture.State(ctx);

            var next = Generator(ctx, st);
            var steel = next.Materials[0];
            Assert.That(steel.Item, Is.EqualTo("steel"));
            Assert.That(steel.Available, Is.LessThan(steel.Required), "the fresh fixture carries no steel");

            Assert.That(steel.Source, Does.Contain("Steel plates on salvage rubble"),
                "the row says WHERE, and names the tile it is sending them to");
            Assert.That(steel.Source, Does.Contain("north-east"), "with a direction from the engineer");
            Assert.That(steel.Source, Does.Contain("tiles"), "and a distance");

            Assert.That(next.Detail, Does.Contain("Steel plates:"), "the detail names the material it is about");
            Assert.That(next.Detail, Does.Contain("hold the primary action on it"), "and the concrete step to take");
        }

        [Test]
        public void TheSourceQueryAgreesWithTheRow()
        {
            var ctx = Context(MapWith((RaidFixture.CoreX + 20, RaidFixture.CoreY - 20, TileClass.Rubble)),
                new SiteRecord("resource:steel", "steel salvage", SiteKind.Resource,
                    RaidFixture.CoreX + 20, RaidFixture.CoreY - 20, 4, 4, "steel", 400));
            var st = OpeningFixture.State(ctx);

            var src = OpeningQueries.Source(ctx, st, ItemId.Steel);
            Assert.That(src.Found, Is.True);
            Assert.That(src.Kind, Is.EqualTo("tile"));
            Assert.That(src.Compass, Is.EqualTo("north-east"));
            Assert.That(src.Tiles, Is.GreaterThan(0));
            Assert.That(Generator(ctx, st).Materials[0].Source, Is.EqualTo(src.Short));
        }

        // ------------------------------------------------------------------ the map itself

        [Test]
        public void WithNoAuthoredPatchTheScanFindsMinableGround()
        {
            // No Resource site at all: the fallback is the geometry, read through the real mining rules.
            var ctx = Context(MapWith((RaidFixture.CoreX + 14, RaidFixture.CoreY, TileClass.Rubble)));
            var st = OpeningFixture.State(ctx);

            var src = OpeningQueries.Source(ctx, st, ItemId.Steel);
            Assert.That(src.Found, Is.True);
            Assert.That(src.Kind, Is.EqualTo("tile"), "found by the ring scan, not by a site");
            Assert.That(src.What, Is.EqualTo("Steel plates on salvage rubble"), "TileClass.Rubble, named as such");
            Assert.That(src.Compass, Is.EqualTo("east"));
            Assert.That(Generator(ctx, st).Materials[0].Source, Does.Contain("Steel plates on salvage rubble"));
        }

        [Test]
        public void ADugOutTileIsNoLongerASource()
        {
            var ctx = Context(MapWith((RaidFixture.CoreX + 14, RaidFixture.CoreY, TileClass.Rubble)));
            var st = OpeningFixture.State(ctx);
            Assert.That(OpeningQueries.Source(ctx, st, ItemId.Steel).Kind, Is.EqualTo("tile"));

            // Empty the one rubble tile the way mining does, through the ground state.
            var w = ctx.Geometry.Width;
            var tile = RaidFixture.CoreY * w + (RaidFixture.CoreX + 14);
            st.Ground.SetDug(tile, w * ctx.Geometry.Height, 0);

            Assert.That(OpeningQueries.Source(ctx, st, ItemId.Steel).Kind, Is.Not.EqualTo("tile"),
                "a tile the engineer has already emptied is not somewhere to send them");
        }

        // ------------------------------------------------------------------ stock and craft

        [Test]
        public void StockInAHomeChestIsOfferedWhenNothingCanBeMined()
        {
            var ctx = Context(MapWith());
            var st = OpeningFixture.State(ctx);
            var chest = RaidFixture.Add(ctx, st, "chest", RaidFixture.CoreX, RaidFixture.CoreY + 12);
            chest.Inv.Add(ItemId.Steel, 40);

            var src = OpeningQueries.Source(ctx, st, ItemId.Steel);
            Assert.That(src.Kind, Is.EqualTo("home"));
            Assert.That(src.Text, Does.Contain("Home supply chest"));
            Assert.That(src.Text, Does.Contain("open it and take what you need"));
        }

        [Test]
        public void AMadeItemSendsThePlayerToTheStationThatMakesIt()
        {
            var ctx = Context(MapWith());
            var st = OpeningFixture.State(ctx);

            // Nothing on a plain-ground map yields wire, but the catalogue says an Assembler makes it.
            var src = OpeningQueries.Source(ctx, st, ItemId.Wire);
            Assert.That(src.Found, Is.True);
            Assert.That(src.Kind, Is.EqualTo("craft"));
            Assert.That(src.Text, Does.Contain("Assembler"));
        }

        [Test]
        public void AnItemThisMapCannotYieldSaysSoPlainly()
        {
            var ctx = Context(MapWith());
            var st = OpeningFixture.State(ctx);

            // Coal is mined, never crafted; a map with no deposit and no stock simply has none.
            var src = OpeningQueries.Source(ctx, st, ItemId.Coal);
            Assert.That(src.Found, Is.False);
            Assert.That(src.Text, Does.StartWith(OpeningQueries.NoSourceText));
            Assert.That(src.Text, Does.Contain(ctx.Data.Item(ItemId.Coal).DisplayName));
        }

        // ------------------------------------------------------------------ the satisfied case

        [Test]
        public void ASatisfiedRowCarriesNoSourceAndLeavesTheDetailAlone()
        {
            var ctx = Context(MapWith((RaidFixture.CoreX + 20, RaidFixture.CoreY - 20, TileClass.Rubble)),
                new SiteRecord("resource:steel", "steel salvage", SiteKind.Resource,
                    RaidFixture.CoreX + 20, RaidFixture.CoreY - 20, 4, 4, "steel", 400));
            var st = OpeningFixture.State(ctx);
            st.Engineer.Inv[ItemId.Steel] = 30;
            st.Engineer.Inv[ItemId.Copper] = 10;

            var next = Generator(ctx, st);
            for (var i = 0; i < next.Materials.Count; i++)
            {
                Assert.That(next.Materials[i].Available, Is.GreaterThanOrEqualTo(next.Materials[i].Required));
                Assert.That(next.Materials[i].Source, Is.EqualTo(""), "a row that is done says nothing about sources");
            }
            Assert.That(next.Detail, Does.Contain("Materials ready in Backpack"));
            Assert.That(next.Detail, Does.Not.Contain("steel salvage"));
        }

        [Test]
        public void TheChainKeepsItsIdsAndTitles()
        {
            // OpeningDefectsTests depends on these; R5 must only ever APPEND to the detail.
            var ctx = Context(MapWith());
            var st = OpeningFixture.State(ctx);
            var next = OpeningQueries.Objective(ctx, st);
            Assert.That(next.Id, Is.EqualTo("opening-workshop"));
            Assert.That(next.Title, Is.EqualTo("1 · Build a Generator"));
            Assert.That(next.Text, Is.EqualTo("Place 1 Generator in Founders Court"));
        }
    }
}
