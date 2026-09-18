using System;
using System.IO;
using NUnit.Framework;

namespace Relight.Sim.Tests.Persistence
{
    /// <summary>
    /// Correction pass C6. The same authored city is now imported twice — as the Phase C Home crop (78x367 at
    /// (43, 91)) and whole (864x576 at (0, 0)) — and both carry the same <c>mapId</c>, so the C-10 map binding
    /// cannot tell a save from one apart from a save from the other. Schema 4 records the region in the header and
    /// <see cref="SaveRelocate"/> moves a save from one region onto the other, or refuses it.
    ///
    /// The first test is the real thing: the Phase C integrated-run save (schema 3, written by the shipped build on
    /// 2026-09-14 and kept as evidence) read onto a full-city map. The evidence file is READ-ONLY — it is copied
    /// into a throwaway temp folder and the copy is what the test opens, so a failing run can never write to it.
    /// </summary>
    public sealed class SaveRelocateTests
    {
        private const string At = "2026-09-14T12:00:00.0000000Z";
        private const string MapId = SaveUpgrade.RiverfrontMapId;

        /// <summary>The Phase C save, relative to the repository root. See <see cref="FindEvidence"/>.</summary>
        private const string EvidenceRelative =
            "Unity/Docs/evidence/phase-c/integrated-run/slot-phasec-int-end.json";

        // The whole city and the Home crop, as the exporter writes them (contract C6).
        private const int FullW = 864, FullH = 576;
        private const int CropW = 78, CropH = 367, CropX = 43, CropY = 91;

        private string _temp;

        [SetUp]
        public void MakeTemp()
        {
            _temp = Path.Combine(Path.GetTempPath(), "relight-relocate-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_temp);
        }

        [TearDown]
        public void RemoveTemp()
        {
            try { if (Directory.Exists(_temp)) Directory.Delete(_temp, true); } catch { /* best effort */ }
        }

        // ---- 1. the real Phase C save, moved onto the whole city ------------------------------------------------

        [Test]
        public void TheEvidencePhaseCSaveLoadsOnTheFullCityWithEverythingShifted()
        {
            var source = FindEvidence();
            if (source == null)
                Assert.Ignore("could not find " + EvidenceRelative + " from " + Directory.GetCurrentDirectory()
                              + "; this check needs the repository checkout.");

            // The evidence is read-only: work on a copy in the test's own temp folder.
            var copy = Path.Combine(_temp, Path.GetFileName(source));
            File.Copy(source, copy);
            var text = File.ReadAllText(copy);

            var ctx = FullCity();
            var load = SaveSerializer.ReadText(text, ctx.Data, ctx.MapId, ctx);
            Assert.That(load.Ok, Is.True, load.Reason);
            Assert.That(load.Damaged, Is.False);

            // The header says version 3 and no region; the upgrade stamps the only region that existed then.
            Assert.That(load.Header.Version, Is.EqualTo(3));
            Assert.That(load.Header.Region.Id, Is.EqualTo("home"));
            Assert.That(load.Header.Region.OriginX, Is.EqualTo(CropX));
            Assert.That(load.Header.Region.OriginY, Is.EqualTo(CropY));
            Assert.That(load.Header.Region.Width, Is.EqualTo(CropW));
            StringAssert.Contains("moved onto", load.Upgraded ?? "");
            StringAssert.Contains("the file itself is unchanged", load.Upgraded ?? "");

            var st = load.State;
            // The engineer stood at the workshop door, (27.5, 269.5) in crop coordinates.
            Assert.That(st.Engineer.Pos.X, Is.EqualTo(27.5 + CropX).Within(1e-9));
            Assert.That(st.Engineer.Pos.Y, Is.EqualTo(269.5 + CropY).Within(1e-9));
            Assert.That(st.Engineer.Pos.X, Is.EqualTo(70.5).Within(1e-9), "the brief's number, spelled out");
            Assert.That(st.Engineer.Pos.Y, Is.EqualTo(360.5).Within(1e-9));

            // Every machine moved with it: the generator was placed at (33, 261) in the crop.
            Assert.That(st.Machines.Count, Is.EqualTo(15));
            var generator = st.Machines[0];
            Assert.That(generator.Kind, Is.EqualTo("generator"));
            Assert.That(generator.X, Is.EqualTo(33 + CropX));
            Assert.That(generator.Y, Is.EqualTo(261 + CropY));
            for (var i = 0; i < st.Machines.Count; i++)
            {
                var m = st.Machines[i];
                Assert.That(m.X, Is.GreaterThanOrEqualTo(CropX), "machine " + m.Id + " (" + m.Kind + ") moved east");
                Assert.That(m.Y, Is.GreaterThanOrEqualTo(CropY), "machine " + m.Id + " (" + m.Kind + ") moved south");
            }

            // The authored Home core rect, (22, 256) in the crop.
            Assert.That(st.Home.Placed, Is.True);
            Assert.That(st.Home.X, Is.EqualTo(22 + CropX));
            Assert.That(st.Home.Y, Is.EqualTo(256 + CropY));

            // Tile INDICES are re-formed against the new width, not merely offset: dug tile 21015 is (33, 269) in a
            // 78-wide region, and (76, 360) in an 864-wide one.
            Assert.That(st.Ground.DugTiles.Length, Is.EqualTo(3));
            Assert.That(st.Ground.DugTiles[0], Is.EqualTo(360 * FullW + 76));
            Assert.That(st.Ground.DugUnits[0], Is.EqualTo(192).Within(1e-9), "the units moved with their tile");
            // The opening encounter's approach, index 20982 = (0, 269) in the crop = (43, 360) on the city.
            Assert.That(st.Opening.Origin, Is.EqualTo(360 * FullW + 43));

            // A turret that has never fired keeps its "no shot" marker rather than gaining one at the crop origin.
            for (var i = 0; i < st.Turrets.Units.Count; i++)
            {
                var u = st.Turrets.Units[i];
                if (u.ShotT > 0) continue;
                Assert.That(u.Shot.X, Is.EqualTo(0).Within(1e-9));
                Assert.That(u.Shot.Y, Is.EqualTo(0).Within(1e-9));
            }
        }

        // ---- 2. a save already on this region is not touched ----------------------------------------------------

        [Test]
        public void AVersion4SaveWithTheSameOriginLoadsExactlyWhereItWasSaved()
        {
            var ctx = FullCity();
            var st = Placed(400.5, 300.5, 402, 301);
            var text = SaveSerializer.WriteText(st, ctx.Data, At, ctx.MapId, SaveRegion.Of(ctx), out var written);

            Assert.That(written.Version, Is.EqualTo(SaveSchema.Version));
            Assert.That(written.Region.Id, Is.EqualTo("full"));
            StringAssert.Contains("\"region\":{\"height\":576,\"id\":\"full\",\"originX\":0,\"originY\":0,\"width\":864}", text);

            var load = SaveSerializer.ReadText(text, ctx.Data, ctx.MapId, ctx);
            Assert.That(load.Ok, Is.True, load.Reason);
            Assert.That(load.Upgraded, Is.Null, "nothing was upgraded and nothing was moved");
            Assert.That(load.State.Engineer.Pos.X, Is.EqualTo(400.5).Within(1e-9));
            Assert.That(load.State.Engineer.Pos.Y, Is.EqualTo(300.5).Within(1e-9));
            Assert.That(load.State.Machines[0].X, Is.EqualTo(402));
            Assert.That(load.State.Machines[0].Y, Is.EqualTo(301));
        }

        // ---- 3. a move that would lose a machine is refused, and changes nothing --------------------------------

        [Test]
        public void AFullCitySaveIsRefusedOnTheOldCropWhenAMachineWouldFallOffTheMap()
        {
            var full = FullCity();
            // The engineer is inside the crop once shifted (50.5, 100.5) - (43, 91) = (7.5, 9.5); the chest is not:
            // (10, 10) - (43, 91) = (-33, -81).
            var st = Placed(50.5, 100.5, 10, 10);
            var text = SaveSerializer.WriteText(st, full.Data, At, full.MapId, SaveRegion.Of(full), out _);

            var crop = HomeCrop();
            var load = SaveSerializer.ReadText(text, crop.Data, crop.MapId, crop);
            Assert.That(load.Ok, Is.False);
            Assert.That(load.Damaged, Is.False, "the file is fine; this game is somewhere else");
            StringAssert.Contains("chest", load.Reason);
            StringAssert.Contains("outside the map", load.Reason);
            StringAssert.Contains("full (0,0)", load.Reason);
            StringAssert.Contains("home (43,91)", load.Reason);
            Assert.That(load.State, Is.Null, "a refused load hands back nothing to adopt");

            // The same save on the region it was made on is fine, which is what makes the refusal actionable.
            Assert.That(SaveSerializer.ReadText(text, full.Data, full.MapId, full).Ok, Is.True);
        }

        [Test]
        public void ASaveIsAlsoRefusedWhenTheEngineerWouldLandInsideABuilding()
        {
            var full = FullCity();
            var st = Placed(50.5, 100.5, 50, 100);
            var text = SaveSerializer.WriteText(st, full.Data, At, full.MapId, SaveRegion.Of(full), out _);

            // A crop whose tile (7, 9) — where the engineer lands — is solid.
            var crop = HomeCrop(solidX: 7, solidY: 9);
            var load = SaveSerializer.ReadText(text, crop.Data, crop.MapId, crop);
            Assert.That(load.Ok, Is.False);
            Assert.That(load.Damaged, Is.False);
            StringAssert.Contains("engineer inside a building", load.Reason);
        }

        // ---- 4. round trip ---------------------------------------------------------------------------------------

        [Test]
        public void AVersion4SaveRoundTripsThroughBytesWithItsRegion()
        {
            var ctx = FullCity();
            var st = Placed(120.5, 240.5, 118, 239);
            st.Tick = 4242;
            st.T = 212.1;
            var before = StateHash.Compute(st);

            var bytes = SaveSerializer.Write(st, ctx, At, out var written);
            var header = SaveSerializer.ReadHeader(bytes, out var problem);
            Assert.That(problem, Is.Empty);
            Assert.That(header.Version, Is.EqualTo(SaveSchema.Version));
            Assert.That(header.MapId, Is.EqualTo(MapId));
            Assert.That(header.Region.Id, Is.EqualTo("full"));
            Assert.That(header.Region.Width, Is.EqualTo(FullW));
            Assert.That(header.Region.Height, Is.EqualTo(FullH));
            Assert.That(header.Hash, Is.EqualTo(written.Hash));

            var load = SaveSerializer.Read(bytes, ctx);
            Assert.That(load.Ok, Is.True, load.Reason);
            Assert.That(StateHash.Compute(load.State), Is.EqualTo(before), "the state survived the round trip");
            Assert.That(load.State.Tick, Is.EqualTo(4242));
            Assert.That(load.Header.Region.OriginX, Is.EqualTo(0));
            Assert.That(load.Header.Region.OriginY, Is.EqualTo(0));
        }

        [Test]
        public void ASaveWithNoRegionIsLeftWhereItIsAndOneWithoutASizeIsRefusedRatherThanGuessedAt()
        {
            var ctx = FullCity();
            var st = Placed(400.5, 300.5, 402, 301);

            // No region at all (the synthetic map, or a v1–v3 file on another map): nothing to disagree with.
            var unbound = SaveSerializer.WriteText(st, ctx.Data, At, "", SaveRegion.Unknown, out _);
            var loose = SaveSerializer.ReadText(unbound, ctx.Data, null, ctx);
            Assert.That(loose.Ok, Is.True, loose.Reason);
            Assert.That(loose.State.Engineer.Pos.X, Is.EqualTo(400.5).Within(1e-9), "an unplaced save is not moved");

            // A region with an origin but no size: the tile INDICES cannot be re-formed, so it is refused.
            var sizeless = new SaveRegion("home", CropX, CropY);
            var text = SaveSerializer.WriteText(st, ctx.Data, At, ctx.MapId, sizeless, out _);
            var load = SaveSerializer.ReadText(text, ctx.Data, ctx.MapId, ctx);
            Assert.That(load.Ok, Is.False);
            Assert.That(load.Damaged, Is.False);
            StringAssert.Contains("how wide its region was", load.Reason);
        }

        [Test]
        public void TheRegionOfAnOlderFileIsTheOnlyRegionThatExistedWhenItWasWritten()
        {
            Assert.That(SaveUpgrade.RegionOfOlder(3, MapId).Id, Is.EqualTo("home"));
            Assert.That(SaveUpgrade.RegionOfOlder(3, MapId).OriginX, Is.EqualTo(CropX));
            Assert.That(SaveUpgrade.RegionOfOlder(3, MapId).OriginY, Is.EqualTo(CropY));
            Assert.That(SaveUpgrade.RegionOfOlder(1, MapId).Known, Is.True, "v1 and v2 were on the same crop");
            Assert.That(SaveUpgrade.RegionOfOlder(3, "").Known, Is.False, "the synthetic map says nothing");
            Assert.That(SaveUpgrade.RegionOfOlder(3, "some-other-city").Known, Is.False);
            Assert.That(SaveUpgrade.RegionOfOlder(4, MapId).Known, Is.False, "a v4 file speaks for itself");
        }

        // ---- fixtures -------------------------------------------------------------------------------------------

        /// <summary>A flat, walkable region of the given size and origin — geometry enough to place and bound-check.</summary>
        private static RegionGeometry Flat(int w, int h, int originX, int originY, int solidX = -1, int solidY = -1)
        {
            var kind = new byte[w * h];
            var solid = new byte[w * h];
            for (var i = 0; i < kind.Length; i++) kind[i] = (byte)TileClass.Ground;
            if (solidX >= 0 && solidY >= 0) solid[solidY * w + solidX] = 1;
            return RegionGeometry.FromArrays(w, h, originX, originY, kind, solid, null, null, new Vec2(0.5, 0.5));
        }

        private static SimContext Context(string regionId, int w, int h, int originX, int originY,
            int solidX = -1, int solidY = -1)
            => new SimContext(ReferenceData.Create(), Flat(w, h, originX, originY, solidX, solidY), null, null,
                new WorldSites(Array.Empty<SiteRecord>(), regionId, originX, originY), MapId);

        private static SimContext FullCity() => Context("full", FullW, FullH, 0, 0);

        private static SimContext HomeCrop(int solidX = -1, int solidY = -1)
            => Context("home", CropW, CropH, CropX, CropY, solidX, solidY);

        /// <summary>A minimal state with somewhere to stand and one 2x2 chest to move (or fail to move).</summary>
        private static SimState Placed(double ex, double ey, int mx, int my)
        {
            var st = new SimState();
            st.Engineer.Pos = new Vec2(ex, ey);
            st.Machines.Add(new Machine { Id = 1, Kind = "chest", X = mx, Y = my, Size = 2 });
            st.NextId = 2;
            st.Rev++;
            return st;
        }

        /// <summary>
        /// The evidence file, found by walking up from the working directory and from this assembly. The Unity
        /// editor runs with the Unity project as the working directory, so walking up from there reaches the
        /// checkout; a runner started somewhere else can be pointed at the checkout with <c>RELIGHT_REPO</c>, and
        /// the last resort is the path the project documents (<c>E:/Factorio2</c>). Null when it cannot be found,
        /// which the test reports as ignored rather than failed — an absent checkout is not a defect in the code.
        /// </summary>
        private static string FindEvidence()
        {
            var starts = new string[4];
            starts[0] = Directory.GetCurrentDirectory();
            try { starts[1] = Path.GetDirectoryName(typeof(SaveRelocateTests).Assembly.Location); }
            catch (Exception) { starts[1] = null; }
            try { starts[2] = Environment.GetEnvironmentVariable("RELIGHT_REPO"); }
            catch (Exception) { starts[2] = null; }
            starts[3] = "E:/Factorio2";

            var relative = EvidenceRelative.Replace('/', Path.DirectorySeparatorChar);
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
