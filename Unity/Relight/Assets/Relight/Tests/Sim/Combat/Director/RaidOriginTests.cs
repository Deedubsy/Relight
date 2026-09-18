using System.Collections.Generic;
using NUnit.Framework;

namespace Relight.Sim.Tests.Combat
{
    /// <summary>
    /// Where a wave is born when the region's raid line sits far south of the core, the Founders Court case
    /// (2026-09-18): the core's top row is 347, its bottom edge 360, the raid line 432. Two faults compounded there.
    /// The <see cref="RaidField"/> box stopped 70 tiles from the core's top-left tile, so nothing at or below the
    /// raid line had a distance; and <see cref="DirectorRules.Origin"/> did not reject a tile with no entry tier
    /// when nothing better existed, so it handed back a tile inside the fence. Both are pinned here on a flat map
    /// with the court's own offsets.
    /// </summary>
    public sealed class RaidOriginTests
    {
        const int Size = 220;
        const int CoreX = 100, CoreY = 40, CoreSize = 8;

        /// <summary>A flat map whose Home core is near the top and whose raid line is <paramref name="rowsBelowBottomEdge"/> rows below the core's last row.</summary>
        static SimContext Context(int rowsBelowBottomEdge)
        {
            // The engineer spawns just south of the core, as on Founders Court (spawn (70,360) against a core ending on
            // row 360), not in the middle of the map: a spawn near the raid line would itself forbid every entry tile
            // through the 28-tile safety ring, which is a different rule from the ones under test.
            var kind = new byte[Size * Size];
            for (var i = 0; i < kind.Length; i++) kind[i] = (byte)TileClass.Ground;
            var g = new ArrayGeometry(Size, Size, kind, new bool[Size * Size], new Vec2(CoreX + 4.5, CoreY + CoreSize + 1.5));
            var sites = new WorldSites(new List<SiteRecord>
            {
                new SiteRecord("home", "Home Court", SiteKind.Core, CoreX, CoreY, CoreSize, CoreSize),
                new SiteRecord("home-raid-line", "Raid approach", SiteKind.RaidLine, CoreX - 10, CoreY + CoreSize - 1 + rowsBelowBottomEdge, 30, 1),
            });
            return new SimContext(ReferenceData.Create(), g, null, null, sites);
        }

        /// <summary>
        /// Founders Court's offset: the raid line 72 rows below the core's last row (row 432 against a core ending on
        /// row 360). The field must reach it, and the origin must be an entry tile on the far side of it — the old
        /// 70-tile box measured from the core's top-left tile ended short of it.
        /// </summary>
        [Test]
        public void TheOriginIsBeyondARaidLineSeventyTwoRowsBelowTheCore()
        {
            var ctx = Context(72);
            var st = RaidFixture.State(ctx);
            var line = DirectorRules.RaidLineY(ctx);
            Assert.That(line, Is.EqualTo(CoreY + CoreSize - 1 + 72));
            Assert.That(line - CoreY, Is.GreaterThan(70), "the old box, 70 tiles from the core's top-left tile, could not reach this row");

            Assert.That(DirectorRules.Target(ctx, st, out var bx, out var by, out var size), Is.True);
            var fld = st.Director.Fields.Field(ctx, st, bx, by, size, true);
            Assert.That(fld.At(CoreX + 4, line), Is.GreaterThanOrEqualTo(0), "the field reaches the raid line row");
            Assert.That(fld.At(CoreX + 4, line), Is.LessThanOrEqualTo(DirectorRules.EntryFarSteps), "and it is inside the entry band there");

            var origin = DirectorRules.Origin(ctx, st);
            Assert.That(origin, Is.GreaterThanOrEqualTo(0), "the director finds an entry tile");
            var ox = origin % Size;
            var oy = origin / Size;
            Assert.That(oy, Is.GreaterThanOrEqualTo(line), "the origin is at or below the raid line, not inside the court");
            Assert.That(DirectorRules.EntryTile(ctx, st, fld, line, ox, oy, DirectorRules.EntryNearSteps, DirectorRules.EntryFarSteps), Is.True, "and it is a legal entry tile");
            Assert.That(DirectorRules.Staging(ctx, st, origin), Is.EqualTo(origin), "staging accepts the origin unchanged");

            var approaches = DirectorRules.Approaches(ctx, st);
            Assert.That(approaches, Is.Not.Empty);
            foreach (var a in approaches)
                Assert.That(a / Size, Is.GreaterThanOrEqualTo(line), "every advertised approach is beyond the raid line");
        }

        /// <summary>
        /// A raid line further than the entry band can reach is an authored fault, and the honest answer is -1 —
        /// never a tile beside the workshop. Before the guard, this returned the first cheapest tile in the scan.
        /// </summary>
        [Test]
        public void AnUnreachableRaidLineYieldsNoOriginRatherThanAnInteriorTile()
        {
            var ctx = Context(DirectorRules.EntryFarSteps + 20);
            var st = RaidFixture.State(ctx);
            Assert.That(DirectorRules.Origin(ctx, st), Is.EqualTo(-1), "no entry tile exists, so there is no origin");
            Assert.That(DirectorRules.Approaches(ctx, st), Is.Empty, "and nothing is advertised");
        }

        /// <summary>The box is measured from the seed rectangle's edges, so a wide core does not lose reach on its far side.</summary>
        [Test]
        public void TheFieldBoxExtendsReachTilesBeyondEveryEdgeOfTheSeed()
        {
            var ctx = Context(72);
            var st = RaidFixture.State(ctx);
            var fld = st.Director.Fields.Field(ctx, st, CoreX, CoreY, CoreSize, true);
            Assert.That(fld.X0, Is.EqualTo(CoreX - RaidField.Reach));
            Assert.That(fld.Y0, Is.EqualTo(0), "clipped to the map's top edge");
            Assert.That(fld.X0 + fld.BW - 1, Is.EqualTo(CoreX + CoreSize - 1 + RaidField.Reach));
            Assert.That(fld.Y0 + fld.BH - 1, Is.EqualTo(CoreY + CoreSize - 1 + RaidField.Reach));
            Assert.That(fld.At(CoreX + CoreSize - 1 + RaidField.Reach + 1, CoreY), Is.EqualTo(-1), "one tile past the reach is outside");
        }
    }
}
