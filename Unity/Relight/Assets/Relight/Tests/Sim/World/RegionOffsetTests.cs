using System;
using System.IO;
using NUnit.Framework;

namespace Relight.Sim.Tests
{
    /// <summary>
    /// Correction pass C6, requirement 4. The Home crop and the whole city are the same authored places seen through
    /// two windows: the crop is 78x367 tiles taken out of the 864x576 city at (43, 91), and every exported
    /// coordinate is local to its own region. So for every authored thing the two exports name, the full-city
    /// number must be the crop number plus exactly (43, 91) — no rounding, no drift, no re-authoring. That relation
    /// is what lets <see cref="SaveRelocate"/> move a Phase C save onto the full map by adding a constant, and what
    /// lets the Home opening keep working unchanged on either region.
    ///
    /// <b>Why it is written this way.</b> <c>Relight.Sim</c> may not reference <c>UnityEngine</c>
    /// (TECHNICAL_ARCHITECTURE.md §3), so this test cannot load the generated <c>WorldGeometryAsset</c> /
    /// <c>HomeSitesAsset</c>. It works on the same numbers by two independent routes instead:
    /// <list type="bullet">
    /// <item>the constants below, copied from the two <c>export-report.json</c> runs, which always run; and</item>
    /// <item>the two exported <c>city.json</c> files read from disk when the checkout has them, parsed with the
    ///       sim's own <see cref="JsonValue"/> into a small stand-in for the imported region. That check confirms
    ///       the constants are still what the exporter writes rather than what someone once believed.</item>
    /// </list>
    /// </summary>
    public sealed class RegionOffsetTests
    {
        /// <summary>The crop's origin in whole-city tiles: the one offset between every pair of numbers here.</summary>
        private const int Dx = 43, Dy = 91;

        // ---- the two regions, as the exporter reports them -------------------------------------------------------

        private static Region HomeConstants() => new Region
        {
            Id = "home", Width = 78, Height = 367, OriginX = 43, OriginY = 91,
            SpawnX = 27, SpawnY = 269,
            CoreX = 22, CoreY = 256, CoreW = 10, CoreH = 14,
            CourtX = 29, CourtY = 283, CourtRadius = 5.5,
            HomeRaidY = 300,
            RaidLineX = 26, RaidLineW = 6,
            CampId = "freight:camp:1", CampX = 52, CampY = 195,
            SubstationX = 35, SubstationY = 256,
        };

        private static Region FullConstants() => new Region
        {
            Id = "full", Width = 864, Height = 576, OriginX = 0, OriginY = 0,
            SpawnX = 70, SpawnY = 360,
            CoreX = 65, CoreY = 347, CoreW = 10, CoreH = 14,
            CourtX = 72, CourtY = 374, CourtRadius = 5.5,
            HomeRaidY = 391,
            RaidLineX = 69, RaidLineW = 6,
            CampId = "freight:camp:1", CampX = 95, CampY = 286,
            SubstationX = 78, SubstationY = 347,
        };

        // ---- the relation ----------------------------------------------------------------------------------------

        [Test]
        public void TheFullCityIsTheHomeCropPlusItsOriginForEveryAuthoredPlace()
        {
            AssertOffset(HomeConstants(), FullConstants());
        }

        [Test]
        public void TheTwoExportedRegionsOnDiskStillHoldThatRelation()
        {
            var home = FromFile("home");
            var full = FromFile("full");
            if (home == null || full == null)
                Assert.Ignore("Unity/Import/{home,full}/city.json is not in this checkout (it is build output, "
                              + "written by `npm run export:unity -- --region home|full`); the constants form of this "
                              + "check ran instead.");

            AssertOffset(home, full);

            // And the constants above are still the numbers the exporter writes.
            AssertSame(HomeConstants(), home);
            AssertSame(FullConstants(), full);
        }

        [Test]
        public void TheCropLiesWhollyInsideTheCityWhichIsWhyNothingIsEverLostMovingOutwards()
        {
            var home = HomeConstants();
            var full = FullConstants();
            Assert.That(home.OriginX + home.Width, Is.LessThanOrEqualTo(full.Width));
            Assert.That(home.OriginY + home.Height, Is.LessThanOrEqualTo(full.Height));
            Assert.That(full.OriginX, Is.Zero, "the whole city is its own origin");
            Assert.That(full.OriginY, Is.Zero);
        }

        /// <summary>
        /// The sim types carry the offset too: <see cref="WorldSites"/> records which region it is and where that
        /// region sits, and <see cref="SaveRegion.Of"/> reads both back out for the save header.
        /// </summary>
        [Test]
        public void TheSimCarriesTheRegionAndItsOriginThroughToTheSaveHeader()
        {
            var home = HomeConstants();
            var core = new SiteRecord("home-workshop", "Home workshop", SiteKind.Core,
                home.CoreX, home.CoreY, home.CoreW, home.CoreH);
            var sites = new WorldSites(new[] { core }, home.Id, home.OriginX, home.OriginY);
            Assert.That(sites.RegionId, Is.EqualTo("home"));
            Assert.That(sites.OriginX, Is.EqualTo(Dx));
            Assert.That(sites.OriginY, Is.EqualTo(Dy));
            Assert.That(sites.Core, Is.SameAs(core), "the opening finds its core by kind, not by a literal");

            var geometry = RegionGeometry.FromArrays(4, 4, home.OriginX, home.OriginY,
                new byte[16], new byte[16], null, null, new Vec2(home.SpawnX + 0.5, home.SpawnY + 0.5));
            var ctx = new SimContext(ReferenceData.Create(), geometry, null, null, sites, "riverfront");
            var region = SaveRegion.Of(ctx);
            Assert.That(region.Id, Is.EqualTo("home"));
            Assert.That(region.OriginX, Is.EqualTo(Dx));
            Assert.That(region.OriginY, Is.EqualTo(Dy));
            Assert.That(region.Known, Is.True);
            Assert.That(region.HasSize, Is.True);

            // An empty WorldSites (the synthetic map) says nothing, so nothing is ever moved on it.
            Assert.That(SaveRegion.Of(new SimContext(ReferenceData.Create(), geometry)).Known, Is.False);
        }

        // ---- the assertions --------------------------------------------------------------------------------------

        private static void AssertOffset(Region home, Region full)
        {
            Assert.That(home.OriginX - full.OriginX, Is.EqualTo(Dx), "the crop's x origin");
            Assert.That(home.OriginY - full.OriginY, Is.EqualTo(Dy), "the crop's y origin");

            Assert.That(full.SpawnX, Is.EqualTo(home.SpawnX + Dx), "spawn x");
            Assert.That(full.SpawnY, Is.EqualTo(home.SpawnY + Dy), "spawn y");

            Assert.That(full.CoreX, Is.EqualTo(home.CoreX + Dx), "core rect x");
            Assert.That(full.CoreY, Is.EqualTo(home.CoreY + Dy), "core rect y");
            Assert.That(full.CoreW, Is.EqualTo(home.CoreW), "the core rect is the same size in both");
            Assert.That(full.CoreH, Is.EqualTo(home.CoreH));

            Assert.That(full.CourtX, Is.EqualTo(home.CourtX + Dx), "court x");
            Assert.That(full.CourtY, Is.EqualTo(home.CourtY + Dy), "court y");
            Assert.That(full.CourtRadius, Is.EqualTo(home.CourtRadius).Within(1e-9), "a radius is not a position");

            Assert.That(full.HomeRaidY, Is.EqualTo(home.HomeRaidY + Dy), "homeRaidY");
            Assert.That(full.RaidLineX, Is.EqualTo(home.RaidLineX + Dx), "the raid line's x");
            Assert.That(full.RaidLineW, Is.EqualTo(home.RaidLineW), "and it is the same width");

            Assert.That(full.CampId, Is.EqualTo(home.CampId), "the first camp is the same authored camp");
            Assert.That(full.CampX, Is.EqualTo(home.CampX + Dx), "first camp x");
            Assert.That(full.CampY, Is.EqualTo(home.CampY + Dy), "first camp y");

            Assert.That(full.SubstationX, Is.EqualTo(home.SubstationX + Dx), "the first substation's x");
            Assert.That(full.SubstationY, Is.EqualTo(home.SubstationY + Dy), "the first substation's y");
        }

        private static void AssertSame(Region expected, Region actual)
        {
            Assert.That(actual.Id, Is.EqualTo(expected.Id));
            Assert.That(actual.Width, Is.EqualTo(expected.Width), expected.Id + " width");
            Assert.That(actual.Height, Is.EqualTo(expected.Height), expected.Id + " height");
            Assert.That(actual.OriginX, Is.EqualTo(expected.OriginX), expected.Id + " origin x");
            Assert.That(actual.OriginY, Is.EqualTo(expected.OriginY), expected.Id + " origin y");
            Assert.That(actual.SpawnX, Is.EqualTo(expected.SpawnX), expected.Id + " spawn x");
            Assert.That(actual.SpawnY, Is.EqualTo(expected.SpawnY), expected.Id + " spawn y");
            Assert.That(actual.CoreX, Is.EqualTo(expected.CoreX), expected.Id + " core x");
            Assert.That(actual.CoreY, Is.EqualTo(expected.CoreY), expected.Id + " core y");
            Assert.That(actual.HomeRaidY, Is.EqualTo(expected.HomeRaidY), expected.Id + " homeRaidY");
            Assert.That(actual.CampX, Is.EqualTo(expected.CampX), expected.Id + " first camp x");
            Assert.That(actual.CampY, Is.EqualTo(expected.CampY), expected.Id + " first camp y");
        }

        // ---- the stand-in for an imported region -----------------------------------------------------------------

        /// <summary>
        /// The handful of numbers this test is about, however they were obtained. It stands in for the pair of
        /// generated assets (<c>WorldGeometryAsset</c> / <c>HomeSitesAsset</c>) that this assembly may not load.
        /// </summary>
        private sealed class Region
        {
            public string Id;
            public int Width, Height, OriginX, OriginY;
            public int SpawnX, SpawnY;
            public int CoreX, CoreY, CoreW, CoreH;
            public int CourtX, CourtY;
            public double CourtRadius;
            public int HomeRaidY;
            public int RaidLineX, RaidLineW;
            public string CampId;
            public int CampX, CampY;
            public int SubstationX, SubstationY;
        }

        /// <summary>The region an exported <c>city.json</c> describes, or null when the checkout has no export.</summary>
        private static Region FromFile(string regionId)
        {
            var path = FindExport(regionId);
            if (path == null) return null;
            var doc = JsonValue.Parse(File.ReadAllText(path), out var error);
            Assert.That(doc, Is.Not.Null, path + ": " + error);

            var region = Need(doc, "region");
            var origin = Need(region, "origin");
            var size = Need(region, "size");
            var spawn = Need(doc, "spawn");
            var scalars = Need(doc, "scalars");
            var court = Need(scalars, "court");
            var core = Need(Need(doc, "sites"), "core");
            var rect = Need(core, "rect");
            var combat = Need(doc, "combat");
            var raidLine = Need(combat, "raidLine");
            var camp = Need(combat, "firstCamps").At(0);
            Assert.That(camp, Is.Not.Null, path + ": no first camp");
            var substation = Need(Need(doc, "sites"), "substations").At(0);
            Assert.That(substation, Is.Not.Null, path + ": no substation");

            return new Region
            {
                Id = region.TextOf("id", ""),
                Width = (int)size.NumberOf("width"),
                Height = (int)size.NumberOf("height"),
                OriginX = (int)origin.NumberOf("x"),
                OriginY = (int)origin.NumberOf("y"),
                SpawnX = (int)spawn.NumberOf("x"),
                SpawnY = (int)spawn.NumberOf("y"),
                CoreX = (int)rect.NumberOf("x"),
                CoreY = (int)rect.NumberOf("y"),
                CoreW = (int)rect.NumberOf("w"),
                CoreH = (int)rect.NumberOf("h"),
                CourtX = (int)court.NumberOf("x"),
                CourtY = (int)court.NumberOf("y"),
                CourtRadius = court.NumberOf("radius"),
                HomeRaidY = (int)scalars.NumberOf("homeRaidY"),
                RaidLineX = (int)raidLine.NumberOf("x"),
                RaidLineW = (int)raidLine.NumberOf("w"),
                CampId = camp.TextOf("id", ""),
                CampX = (int)camp.NumberOf("x"),
                CampY = (int)camp.NumberOf("y"),
                SubstationX = (int)substation.NumberOf("x"),
                SubstationY = (int)substation.NumberOf("y"),
            };
        }

        private static JsonValue Need(JsonValue owner, string name)
        {
            var member = owner?.Member(name);
            Assert.That(member, Is.Not.Null, "the export has no '" + name + "'");
            return member;
        }

        /// <summary>
        /// <c>Unity/Import/&lt;id&gt;/city.json</c>, found by walking up from the working directory (the Unity
        /// editor's is the Unity project), from this assembly, from <c>RELIGHT_REPO</c>, and finally from the path
        /// the project documents. Null when the checkout has no export: it is build output, not source.
        /// </summary>
        private static string FindExport(string regionId)
        {
            var relative = Path.Combine("Unity", "Import", regionId, "city.json");
            var starts = new string[4];
            starts[0] = Directory.GetCurrentDirectory();
            try { starts[1] = Path.GetDirectoryName(typeof(RegionOffsetTests).Assembly.Location); }
            catch (Exception) { starts[1] = null; }
            try { starts[2] = Environment.GetEnvironmentVariable("RELIGHT_REPO"); }
            catch (Exception) { starts[2] = null; }
            starts[3] = "E:/Factorio2";

            for (var s = 0; s < starts.Length; s++)
            {
                var dir = starts[s];
                for (var up = 0; up < 12 && !string.IsNullOrEmpty(dir); up++)
                {
                    string candidate;
                    try { candidate = Path.Combine(dir, relative); }
                    catch (Exception) { break; }
                    if (File.Exists(candidate)) return candidate;
                    var parent = Path.GetDirectoryName(dir);
                    if (string.CompareOrdinal(parent, dir) == 0) break;
                    dir = parent;
                }
            }
            return null;
        }
    }
}
