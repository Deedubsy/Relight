using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Relight.Editor;
using UnityEditor;
using UnityEngine;

namespace Relight.Authoring.Tests
{
    /// <summary>
    /// D-02b (EXP-01, REL-31). The real export passes <see cref="CityValidator"/>, and a city broken on purpose
    /// fails it with the id of the thing at fault in the message — one rule family at a time: roads and pavements,
    /// swept rail clearance, entrances, factory reservations, then the whole-city rules.
    ///
    /// The cities are the tracked exports under <c>Unity/Import</c>, parsed afresh for every test and changed in
    /// memory only. Nothing here imports: a failing <see cref="RegionImporter.Import"/> logs an error and opens a
    /// dialog, so the end-to-end tests use <see cref="RegionImporter.Check"/> on a copy in the project's Temp folder.
    /// </summary>
    public sealed class CityValidatorTests
    {
        private const string Workshop = "home-workshop";

        private sealed class Export
        {
            public string Folder, Json;
            public byte[] Kind, Solid;
            public int Width;
        }

        private static Export _full, _home;

        [OneTimeSetUp]
        public void ReadTheExports()
        {
            _full = Read("full");
            _home = Read("home");
        }

        private static Export Read(string region)
        {
            var folder = RegionImporter.ImportFolder(region);
            var json = Path.Combine(folder, "city.json");
            if (!File.Exists(json)) Assert.Ignore("No export at " + folder);
            var text = File.ReadAllText(json);
            var city = JsonUtility.FromJson<CityFile>(text);
            return new Export
            {
                Folder = folder,
                Json = text,
                Kind = File.ReadAllBytes(Path.Combine(folder, city.terrain.kind)),
                Solid = File.ReadAllBytes(Path.Combine(folder, city.terrain.solid)),
                Width = city.region.size.width,
            };
        }

        /// <summary>The city with one thing changed, validated. <paramref name="solid"/> is a copy the change may write to.</summary>
        private static CityReport Broken(Export from, Action<CityFile, byte[]> change)
        {
            var city = JsonUtility.FromJson<CityFile>(from.Json);
            var solid = (byte[])from.Solid.Clone();
            change(city, solid);
            return CityValidator.Validate(city, from.Kind, solid);
        }

        private static CityReport Broken(Action<CityFile, byte[]> change) => Broken(_full, change);

        private static JBuilding Building(CityFile c, string id) => c.buildings.First(b => b.id == id);

        /// <summary>Pick the whole building up, door and path and all, and put its rect's corner at x,y.</summary>
        private static void Move(JBuilding b, int x, int y)
        {
            int dx = x - b.rect.x, dy = y - b.rect.y;
            foreach (var r in new[] {b.rect, b.parcel, b.visual, b.doorRect}) { r.x += dx; r.y += dy; }
            b.door.x += dx;
            b.door.y += dy;
            foreach (var p in b.path.points) { p.x += dx; p.y += dy; }
        }

        private static JSample MidRail(CityFile c) => c.tram.baked[c.tram.baked.Length / 2];

        private static void Names(CityReport report, string id, string rule)
        {
            Assert.That(report.Ok, Is.False, "the broken city passed");
            Assert.That(report.Errors.Any(e => e.Contains(id) && e.Contains(rule)), Is.True,
                $"no error names '{id}' with '{rule}'. Errors:\n" + string.Join("\n", report.Errors.Take(12)));
        }

        // ================================================================= the real city

        [Test]
        public void TheRealCityPasses()
        {
            var report = Broken((c, s) => { });
            Assert.That(report.Errors, Is.Empty, string.Join("\n", report.Errors.Take(12)));
            Assert.That(report.WholeCity, Is.True);
            var recorded = JsonUtility.FromJson<CityFile>(_full.Json).validation.accessible;
            Assert.That(report.Accessible, Is.EqualTo(recorded), "the walkable flood disagrees with the exporter's count");
        }

        [Test]
        public void TheHomeCropPasses_WithoutTheWholeCityRules()
        {
            var report = Broken(_home, (c, s) => { });
            Assert.That(report.Errors, Is.Empty, string.Join("\n", report.Errors.Take(12)));
            Assert.That(report.WholeCity, Is.False);
            var recorded = JsonUtility.FromJson<CityFile>(_home.Json).validation.accessible;
            Assert.That(report.Accessible, Is.EqualTo(recorded));
        }

        // ================================================================= roads and pavements

        [Test]
        public void ABuildingOnARoad_IsNamed()
        {
            var report = Broken((c, s) =>
            {
                var road = c.roads.polylines[0].points[0];
                Move(Building(c, Workshop), road.x, road.y);
            });
            Names(report, Workshop, "road/pavement setback");
        }

        [Test]
        public void APropOnARoad_IsNamed()
        {
            string prop = null;
            var report = Broken((c, s) =>
            {
                var road = c.roads.polylines[0].points[0];
                prop = c.props[0].id;
                c.props[0].rect.x = road.x;
                c.props[0].rect.y = road.y;
            });
            Names(report, prop, "prop on public road/pavement");
        }

        [Test]
        public void AYardOnARoad_IsNamed()
        {
            var report = Broken((c, s) =>
            {
                var road = c.roads.polylines[0].points[0];
                c.sites.yards[0].x = road.x;
                c.sites.yards[0].y = road.y;
            });
            Names(report, "Yard 0", "overlaps public pavement");
        }

        // ================================================================= swept rail clearance

        [Test]
        public void ABuildingOnTheTramLine_IsNamed()
        {
            var report = Broken((c, s) =>
            {
                var rail = MidRail(c);
                Move(Building(c, Workshop), (int)rail.x, (int)rail.y);
            });
            Names(report, Workshop, "swept tram corridor");
        }

        [Test]
        public void APropOnTheTramLine_IsNamed()
        {
            string prop = null;
            var report = Broken((c, s) =>
            {
                var rail = MidRail(c);
                prop = c.props[0].id;
                c.props[0].rect.x = (int)rail.x;
                c.props[0].rect.y = (int)rail.y;
            });
            Names(report, prop, "prop on tram corridor");
        }

        [Test]
        public void AYardOnTheTramLine_IsNamed()
        {
            var report = Broken((c, s) =>
            {
                var rail = MidRail(c);
                c.sites.yards[0].x = (int)rail.x;
                c.sites.yards[0].y = (int)rail.y;
            });
            Names(report, "Yard 0", "overlaps swept rail at");
        }

        // ================================================================= entrances

        [Test]
        public void ADoorOnTheWrongWall_IsNamed()
        {
            var report = Broken((c, s) => Building(c, Workshop).facing = "N");
            Names(report, Workshop, "door on wrong wall");
        }

        [Test]
        public void APathThatDoesNotStartAtTheDoor_IsNamed()
        {
            var report = Broken((c, s) => Building(c, Workshop).path.points[0].x += 3);
            Names(report, Workshop, "entrance path faces wrong way");
        }

        [Test]
        public void APropOnAnEntrancePath_IsNamed_AndSoIsTheProp()
        {
            string prop = null;
            var report = Broken((c, s) =>
            {
                var end = Building(c, Workshop).path.points.Last();
                prop = c.props[0].id;
                c.props[0].rect = new JRect {x = end.x, y = end.y - 3, w = 1, h = 1};
            });
            Names(report, Workshop, "path width obstructed by " + prop);
        }

        [Test]
        public void ASolidTileOnAnEntrancePath_IsNamed()
        {
            var report = Broken((c, s) =>
            {
                var end = Building(c, Workshop).path.points.Last();
                s[(end.y - 3) * _full.Width + end.x] = 1;
            });
            Names(report, Workshop, "obstructed entrance path at");
        }

        [Test]
        public void ADoorNobodyCanWalkTo_IsNamed()
        {
            var report = Broken((c, s) =>
            {
                var first = Building(c, Workshop).path.points[0];
                for (var y = first.y - 1; y <= first.y + 1; y++)
                for (var x = first.x - 1; x <= first.x + 1; x++)
                    s[y * _full.Width + x] = 1;
            });
            Names(report, Workshop, "unreachable entrance approach");
        }

        [Test]
        public void ASiteNobodyCanWalkTo_IsNamed()
        {
            string plant = null;
            var report = Broken((c, s) =>
            {
                var p = c.sites.plants[0];
                plant = p.id;
                for (var y = p.y - 2; y < p.y + 5; y++)
                for (var x = p.x - 2; x < p.x + 5; x++)
                    s[y * _full.Width + x] = 1;
            });
            Names(report, plant, "inaccessible binding");
        }

        // ================================================================= factory reservations

        [Test]
        public void ABuildingInAFactoryYard_IsNamed()
        {
            var report = Broken((c, s) =>
            {
                var yard = c.sites.yards[0];
                Move(Building(c, Workshop), yard.x + 1, yard.y + 1);
            });
            Names(report, Workshop, "factory yard");
        }

        [Test]
        public void ABuildingOnAServiceDrive_IsNamed()
        {
            var report = Broken((c, s) =>
            {
                var drive = c.drives[0].points[1];
                Move(Building(c, Workshop), drive.x, drive.y);
            });
            Names(report, Workshop, "service drive");
        }

        [Test]
        public void AMissingYard_Fails()
        {
            var report = Broken((c, s) => c.sites.yards = c.sites.yards.Skip(1).ToArray());
            Names(report, "Four factory yards", "required");
        }

        [Test]
        public void ADriveThatLeavesNoRoad_IsNamed()
        {
            var report = Broken((c, s) =>
            {
                c.drives[0].points[0].x += 9;
                c.drives[0].points[0].y += 9;
            });
            Names(report, "service drive 0", "Disconnected");
        }

        // ================================================================= the whole city

        [Test]
        public void AMissingRequiredParcel_IsNamed()
        {
            var report = Broken((c, s) => c.buildings = c.buildings.Where(b => b.id != Workshop).ToArray());
            Names(report, Workshop, "Missing required parcel");
        }

        [Test]
        public void TwoBuildingsWithOneId_AreNamed()
        {
            string id = null;
            var report = Broken((c, s) =>
            {
                id = c.buildings[4].id;
                c.buildings[5].id = id;
            });
            Names(report, id, "duplicate");
        }

        [Test]
        public void AMissingTramStop_IsNamed()
        {
            string stop = null;
            var report = Broken((c, s) =>
            {
                stop = c.sites.stops[0].id;
                c.sites.stops = c.sites.stops.Skip(1).ToArray();
            });
            Names(report, stop, "Missing/duplicate required sites");
        }

        [Test]
        public void ARoadToNowhere_IsNamed()
        {
            var report = Broken((c, s) => c.roads.edges[0].to = "nowhere");
            Names(report, "nowhere", "Missing road node");
        }

        [Test]
        public void ARoadNodeCutOff_IsNamed()
        {
            string node = null;
            var report = Broken((c, s) =>
            {
                node = c.roads.nodes[7].id;
                c.roads.edges = c.roads.edges.Where(e => e.from != node && e.to != node).ToArray();
            });
            Names(report, node, "Disconnected public road graph");
        }

        [Test]
        public void AnUndeclaredRoadEnd_IsNamed()
        {
            string node = null;
            var report = Broken((c, s) =>
            {
                var end = c.roads.nodes.First(n => !string.IsNullOrEmpty(n.end));
                node = end.id;
                end.end = "";
            });
            Names(report, node, "Unintended road end");
        }

        [Test]
        public void AGapInTheRail_IsNamedByItsSample()
        {
            var report = Broken((c, s) =>
            {
                var rail = c.tram.baked.ToList();
                rail.RemoveRange(2000, 40);
                c.tram.baked = rail.ToArray();
            });
            Names(report, "2000", "Rail tangent/position discontinuity");
        }

        [Test]
        public void AKinkInTheRail_IsNamedByItsSample()
        {
            var report = Broken((c, s) => c.tram.baked[2000].y += 0.2f);
            Names(report, "2001", "Rail tangent/position discontinuity");
        }

        [Test]
        public void ATramStopOnACurve_IsNamed()
        {
            string stop = null;
            var report = Broken((c, s) =>
            {
                stop = c.sites.stops[0].id;
                for (var i = 1; i < c.tram.baked.Length; i++)
                {
                    JSample p = c.tram.baked[i], q = c.tram.baked[i - 1];
                    if (Math.Abs(p.x - q.x) <= .05 || Math.Abs(p.y - q.y) <= .05) continue;
                    c.sites.stops[0].x = (int)p.x - 1;
                    c.sites.stops[0].y = (int)p.y - 1;
                    return;
                }
                Assert.Fail("the baked line has no curve");
            });
            Names(report, stop, "not on a straight");
        }

        [Test]
        public void ACityWithNoMapId_Fails()
        {
            var report = Broken((c, s) => c.mapId = "");
            Names(report, "city.json", "no map ID");
        }

        // ================================================================= the first region

        [Test]
        public void ACampKeyInsideAWall_IsNamed()
        {
            string camp = null;
            var report = Broken((c, s) =>
            {
                var marker = c.combat.firstCamps[0];
                camp = marker.id;
                s[marker.y * _full.Width + marker.x] = 1;
            });
            Names(report, camp, "key marker solid");
        }

        [Test]
        public void AFreightGateThatIsAWall_IsNamedByItsTile()
        {
            var at = "";
            var report = Broken((c, s) =>
            {
                var gate = c.combat.freightGates[0];
                at = gate.x + "," + gate.y;
                s[gate.y * _full.Width + gate.x] = 1;
            });
            Names(report, "Gate " + at, "not an existing door");
        }

        // ================================================================= the importer fails, end to end

        /// <summary>
        /// A copy of the Home export with one thing changed, run through every check the importer makes. Each change
        /// here leaves the solid mask and the recorded counts alone, so the importer's own file checks pass and it is
        /// the validator that stops the import.
        /// </summary>
        private static ImportResult CheckACopy(Action<CityFile> change)
        {
            var temp = FileUtil.GetUniqueTempPathInProject();
            Directory.CreateDirectory(temp);
            try
            {
                foreach (var file in Directory.GetFiles(_home.Folder))
                    File.Copy(file, Path.Combine(temp, Path.GetFileName(file)));
                var city = JsonUtility.FromJson<CityFile>(_home.Json);
                change(city);
                File.WriteAllText(Path.Combine(temp, "city.json"), JsonUtility.ToJson(city));
                return RegionImporter.Check("home", temp);
            }
            finally
            {
                Directory.Delete(temp, true);
            }
        }

        private static void Stops(ImportResult result, string id, string rule)
        {
            Assert.That(result.ok, Is.False, "the broken export would have imported: " + result.message);
            Assert.That(result.message, Does.Contain("nothing was imported"));
            Assert.That(result.problems.Any(e => e.Contains(id) && e.Contains(rule)), Is.True,
                $"no problem names '{id}' with '{rule}'. Problems:\n" + string.Join("\n", result.problems.Take(12)));
        }

        [Test]
        public void TheImporter_TakesAnUntouchedCopy()
        {
            var result = CheckACopy(c => { });
            Assert.That(result.ok, Is.True, result.message);
            Assert.That(result.problems, Is.Empty);
        }

        [Test]
        public void TheImporter_StopsAtAYardOnARoad()
        {
            var result = CheckACopy(c =>
            {
                var road = c.roads.polylines[0].points[0];
                c.sites.yards[0].x = road.x;
                c.sites.yards[0].y = road.y;
            });
            Stops(result, "Yard 0", "overlaps public pavement");
        }

        [Test]
        public void TheImporter_StopsAtAYardOnTheTramLine()
        {
            var result = CheckACopy(c =>
            {
                var rail = MidRail(c);
                c.sites.yards[0].x = (int)rail.x;
                c.sites.yards[0].y = (int)rail.y;
            });
            Stops(result, "Yard 0", "overlaps swept rail at");
        }

        [Test]
        public void TheImporter_StopsAtADoorOnTheWrongWall()
        {
            var result = CheckACopy(c => Building(c, Workshop).facing = "N");
            Stops(result, Workshop, "door on wrong wall");
        }

        [Test]
        public void TheImporter_StopsAtAYardOverABuilding()
        {
            var result = CheckACopy(c =>
            {
                var rect = Building(c, Workshop).rect;
                c.sites.yards[0].x = rect.x;
                c.sites.yards[0].y = rect.y;
            });
            Stops(result, Workshop, "factory yard");
        }
    }
}
