using System;
using System.Collections.Generic;
using System.IO;
using Relight.Sim;
using Relight.World;
using UnityEditor;
using UnityEngine;

namespace Relight.Editor
{
    /// <summary>
    /// What one region import did. Returned by <see cref="RegionImporter.Import(string)"/> so a batch/eval caller can
    /// check the outcome without scraping the console. Never null, never throws.
    /// </summary>
    public sealed class ImportResult
    {
        public bool ok;
        public string regionId = "";
        public string message = "";
        public int width, height, originX, originY;
        public int buildings, props, land, accessible, tiles, sites;
        public string geometryPath = "", sitesPath = "";

        public override string ToString() => (ok ? "OK " : "FAILED ") + regionId + ": " + message;
    }

    /// <summary>
    /// D-02a / D-02b / C-01. Reads <c>Unity/Import/&lt;region&gt;/</c> (written by
    /// <c>npm run map:export-unity -- --region &lt;region&gt;</c>) and writes that region's generated assets.
    /// EDITOR ONLY: nothing here runs in a build, and the game reads only the assets, never the import folder.
    ///
    /// Regions are not special-cased. <c>home</c> is the 78x367 opening crop at (43,91); <c>full</c> is the whole
    /// 864x576 city at (0,0), so a full-map coordinate is a Home-crop coordinate plus (43,91) — the same relation the
    /// exporter guarantees (correction-pass contract C6). Importing one region never writes another's folder.
    ///
    /// Menus:
    ///   Relight/World/Import Home Region      — import 'home' (Generated/Home/HomeGeometry.asset, HomeSites.asset).
    ///   Relight/World/Re-import Home Region   — the same method (the name exists so "re-import" is discoverable).
    ///   Relight/World/Import Full City        — import 'full' (Generated/Full/FullGeometry.asset, FullSites.asset).
    /// and <see cref="Import(string)"/> is menu-free so `RegionImporter.Import("full")` works from an eval/batch call.
    ///
    /// Idempotence, by design rather than by luck:
    ///   * every list is written in the order it appears in <c>city.json</c>, which the exporter produces
    ///     deterministically (two exports are byte-identical);
    ///   * <see cref="AssetDatabase.CreateAsset"/> runs only when the asset does not exist — otherwise the existing
    ///     asset's fields are overwritten in place and marked dirty, so the GUID and every reference to it survive;
    ///   * nothing outside <c>Assets/Relight/World/Generated/&lt;Region&gt;/</c> is ever written (the empty manual
    ///     overrides asset is created once if missing and never rewritten). Manual corrections live in
    ///     <c>Assets/Relight/World/Manual/HomeSitesOverrides.asset</c> and win per site id at runtime
    ///     (<see cref="HomeSites.Resolve"/>).
    /// Therefore importing twice produces identical serialized YAML.
    ///
    /// The import is also a CHECK: the solid mask is re-derived here from the ported reference rule
    /// (ground.ts:628-630) and compared tile-by-tile with the exported <c>terrain.solid.bin</c>, and the region
    /// validation counts are re-derived from the arrays. Any mismatch aborts the import with the offending tile or
    /// count, so a drifting port cannot land silently.
    /// </summary>
    public static class RegionImporter
    {
        private const string GeneratedRoot = "Assets/Relight/World/Generated";
        private const string ManualFolder = "Assets/Relight/World/Manual";
        private const int SupportedSchema = 1;

        [MenuItem("Relight/World/Import Home Region", false, 100)]
        public static void ImportHomeRegion() => Import("home");

        [MenuItem("Relight/World/Re-import Home Region", false, 101)]
        public static void ReimportHomeRegion() => Import("home");

        [MenuItem("Relight/World/Import Full City", false, 102)]
        public static void ImportFullCity() => Import("full");

        /// <summary>The import folder for a region: <c>&lt;project&gt;/../Import/&lt;region&gt;</c>, i.e. <c>Unity/Import/home</c>.</summary>
        public static string ImportFolder(string regionId) =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Import", regionId));

        /// <summary>"home" -> "Home", "full" -> "Full": the Generated sub-folder and the asset name prefix.</summary>
        public static string AssetPrefix(string regionId)
        {
            if (string.IsNullOrEmpty(regionId)) return "Region";
            return char.ToUpperInvariant(regionId[0]) + regionId.Substring(1);
        }

        public static string GeneratedFolder(string regionId) => GeneratedRoot + "/" + AssetPrefix(regionId);
        public static string GeometryAssetPath(string regionId) =>
            GeneratedFolder(regionId) + "/" + AssetPrefix(regionId) + "Geometry.asset";
        public static string SitesAssetPath(string regionId) =>
            GeneratedFolder(regionId) + "/" + AssetPrefix(regionId) + "Sites.asset";

        /// <summary>
        /// Import one region. Menu-free on purpose: the coordinator calls <c>RegionImporter.Import("full")</c> from an
        /// eval. Returns what happened; failures are also logged and (outside batch mode) shown in a dialog.
        /// </summary>
        public static ImportResult Import(string regionId)
        {
            var result = new ImportResult {regionId = regionId ?? ""};
            if (string.IsNullOrEmpty(regionId)) return Fail(result, "No region id given.");

            var folder = ImportFolder(regionId);
            if (!Directory.Exists(folder))
                return Fail(result, $"No import folder at\n{folder}\n\nRun this in the reference repo first:\n" +
                                    $"  npm run map:export-unity -- --region {regionId}");

            CityFile city;
            try
            {
                city = JsonUtility.FromJson<CityFile>(File.ReadAllText(Path.Combine(folder, "city.json")));
            }
            catch (Exception e)
            {
                return Fail(result, $"Could not read {Path.Combine(folder, "city.json")}:\n{e.Message}");
            }
            if (city == null || city.region == null || city.region.size == null || city.terrain == null)
                return Fail(result, $"{folder}/city.json is not a region export (missing region or terrain).");
            if (city.schema != SupportedSchema)
                return Fail(result, $"city.json schema {city.schema}; this importer reads schema {SupportedSchema}.");
            if (city.yAxis != "down")
                return Fail(result, $"city.json yAxis is '{city.yAxis}'; the sim is Y-down and the importer will not guess.");
            if (city.region.id != regionId)
                return Fail(result, $"city.json says region '{city.region.id}' but '{regionId}' was asked for; " +
                                    "importing it would write the wrong Generated folder.");

            var w = city.region.size.width;
            var h = city.region.size.height;
            var n = w * h;
            if (n <= 0) return Fail(result, $"city.json region size is {w}x{h}.");

            if (!TryReadSideCar(folder, city.terrain.kind, n, out var kind, result) ||
                !TryReadSideCar(folder, city.terrain.variant, n, out var variant, result) ||
                !TryReadSideCar(folder, city.terrain.patch, n, out var patch, result) ||
                !TryReadSideCar(folder, city.terrain.solid, n, out var solid, result))
                return result;

            if (!CheckSolidMask(city, w, h, solid, result)) return result;

            var origin = city.region.origin ?? new JPoint();
            var spawn = city.spawn ?? new JPoint();
            if (spawn.x < 0 || spawn.y < 0 || spawn.x >= w || spawn.y >= h)
                return Fail(result, $"city.json spawn ({spawn.x},{spawn.y}) is outside the {w}x{h} region.");

            // The reference engineer stands at a tile centre (createCampaign: 70.5, 360.5); the export carries the
            // tile and the runtime re-centres it in ImportedGeometry.Build, which reproduces it exactly.
            var geometry = RegionGeometry.FromArrays(w, h, origin.x, origin.y, kind, solid, variant, patch,
                new Vec2(spawn.x + 0.5, spawn.y + 0.5));

            if (!CheckCounts(city, geometry, n, result)) return result;

            var siteList = SiteRecords(city);
            WriteAssets(regionId, city, w, h, origin, spawn, kind, variant, patch, solid, siteList);

            result.ok = true;
            result.width = w; result.height = h; result.originX = origin.x; result.originY = origin.y;
            result.buildings = city.validation.buildings;
            result.props = city.validation.props;
            result.land = city.validation.land;
            result.accessible = city.validation.accessible;
            result.tiles = city.validation.tiles;
            result.sites = siteList.Count;
            result.geometryPath = GeometryAssetPath(regionId);
            result.sitesPath = SitesAssetPath(regionId);
            result.message =
                $"imported region '{city.region.id}' ({w}x{h} at {origin.x},{origin.y}) from {folder} — " +
                $"{city.validation.buildings} buildings, {city.validation.props} props, " +
                $"{city.validation.accessible}/{city.validation.land} accessible land tiles, {siteList.Count} sites, " +
                $"map {city.mapId}, source {Short(city.sourceSha256)} -> {result.geometryPath}";
            Debug.Log("Relight: " + result.message);
            return result;
        }

        // ----------------------------------------------------------------- checks

        private static bool TryReadSideCar(string folder, string name, int n, out byte[] data, ImportResult result)
        {
            data = null;
            if (string.IsNullOrEmpty(name)) { Fail(result, "city.json terrain is missing a side-car file name."); return false; }
            var path = Path.Combine(folder, name);
            if (!File.Exists(path)) { Fail(result, $"Missing terrain side-car:\n{path}"); return false; }
            var bytes = File.ReadAllBytes(path);
            if (bytes.Length != n)
            {
                Fail(result, $"{name} has {bytes.Length} bytes; the region is {n} tiles. Re-export the region.");
                return false;
            }
            data = bytes;
            return true;
        }

        /// <summary>
        /// The reference collision rule, ported from <c>packages/sim/src/ground.ts:628-630</c> (<c>riverfrontGround</c>):
        ///   628  every building tile is solid unless the building is enterable, in which case only its wall ring is
        ///        solid and the doorway tiles of that ring are open;
        ///   629  every prop the campaign has not cleared or opened is solid (a fresh campaign has cleared none,
        ///        which is what the export captures — clearing a prop is a RUNTIME change, not part of the authored
        ///        mask);
        ///   630  every substation lot is 3x3 solid.
        /// Re-deriving it here and comparing against the exported mask is the whole point of exporting
        /// <c>terrain.solid.bin</c>: if this port ever drifts from the reference, the import fails instead of the
        /// game quietly disagreeing with the reference about where the walls are.
        /// </summary>
        private static bool CheckSolidMask(CityFile city, int w, int h, byte[] exported, ImportResult result)
        {
            var derived = new byte[w * h];

            foreach (var b in city.buildings ?? Array.Empty<JBuilding>())
            {
                if (b == null || b.rect == null) continue;
                for (var y = b.rect.y; y < b.rect.y + b.rect.h; y++)
                for (var x = b.rect.x; x < b.rect.x + b.rect.w; x++)
                {
                    if (x < 0 || y < 0 || x >= w || y >= h) continue;
                    var wall = x == b.rect.x || y == b.rect.y ||
                               x == b.rect.x + b.rect.w - 1 || y == b.rect.y + b.rect.h - 1;
                    var door = b.enterable && b.hasDoor && Inside(b.doorRect, x, y);
                    if (!door && b.enterable && b.doors != null)
                        foreach (var d in b.doors) if (Inside(d, x, y)) { door = true; break; }
                    derived[y * w + x] = (byte)(!b.enterable || (wall && !door) ? 1 : 0);
                }
            }

            foreach (var p in city.props ?? Array.Empty<JProp>())
            {
                if (p == null || p.rect == null) continue;
                for (var y = p.rect.y; y < p.rect.y + p.rect.h; y++)
                for (var x = p.rect.x; x < p.rect.x + p.rect.w; x++)
                    if (x >= 0 && y >= 0 && x < w && y < h) derived[y * w + x] = 1;
            }

            var substations = city.sites != null ? city.sites.substations : null;
            foreach (var s in substations ?? Array.Empty<JSubstation>())
            {
                if (s == null) continue;
                var size = s.size > 0 ? s.size : 3;
                for (var dy = 0; dy < size; dy++)
                for (var dx = 0; dx < size; dx++)
                {
                    var x = s.x + dx;
                    var y = s.y + dy;
                    if (x >= 0 && y >= 0 && x < w && y < h) derived[y * w + x] = 1;
                }
            }

            for (var t = 0; t < derived.Length; t++)
            {
                if (derived[t] == exported[t]) continue;
                Fail(result, $"The imported solid mask disagrees with the ported ground.ts:628-630 rule at region tile " +
                             $"({t % w},{t / w}): file says {exported[t]}, the rule says {derived[t]}. " +
                             "Either the exporter or this importer's port is wrong; do not import.");
                return false;
            }
            return true;
        }

        private static bool Inside(JRect r, int x, int y) =>
            r != null && x >= r.x && x < r.x + r.w && y >= r.y && y < r.y + r.h;

        /// <summary>Re-derive every count the exporter recorded, from the arrays alone.</summary>
        private static bool CheckCounts(CityFile city, RegionGeometry geometry, int n, ImportResult result)
        {
            var v = city.validation ?? new JValidation();
            city.validation = v;   // so the asset write below never dereferences a missing member
            var buildings = city.buildings != null ? city.buildings.Length : 0;
            var props = city.props != null ? city.props.Length : 0;
            var land = geometry.CountLand();
            var accessible = geometry.CountAccessible();

            if (!Same("tiles", v.tiles, n, result)) return false;
            if (!Same("buildings", v.buildings, buildings, result)) return false;
            if (!Same("props", v.props, props, result)) return false;
            if (!Same("land", v.land, land, result)) return false;
            if (!Same("accessible", v.accessible, accessible, result)) return false;
            return true;
        }

        private static bool Same(string what, int recorded, int derived, ImportResult result)
        {
            if (recorded == derived) return true;
            Fail(result, $"validation.{what} is {recorded} in city.json but {derived} when re-derived from the exported " +
                         "arrays. The export is inconsistent; re-export the region.");
            return false;
        }

        // ----------------------------------------------------------------- assets

        private static void WriteAssets(string regionId, CityFile city, int w, int h, JPoint origin, JPoint spawn,
            byte[] kind, byte[] variant, byte[] patch, byte[] solid, List<WorldSite> siteList)
        {
            EnsureFolder(GeneratedRoot);
            EnsureFolder(GeneratedFolder(regionId));
            EnsureFolder(ManualFolder);

            var geometryAsset = LoadOrCreate<WorldGeometryAsset>(GeometryAssetPath(regionId));
            geometryAsset.Fill(
                city.mapId, city.region.id, city.region.name, city.sourceSha256,
                city.manifest != null ? city.manifest.cityId : "",
                city.manifest != null ? city.manifest.sourceHash : "",
                city.manifest != null ? city.manifest.version : 0,
                city.schema, w, h, origin.x, origin.y, new Vector2Int(spawn.x, spawn.y),
                kind, variant, patch, solid,
                BuildingRecords(city),
                new WorldGeometryAsset.RegionValidation
                {
                    buildings = city.validation.buildings,
                    props = city.validation.props,
                    land = city.validation.land,
                    accessible = city.validation.accessible,
                    tiles = city.validation.tiles,
                });
            geometryAsset.FillCity(Decor(city));
            EditorUtility.SetDirty(geometryAsset);

            // The overrides asset lives OUTSIDE Generated/ and is never written again once it exists: creating the
            // empty one here only makes the override point discoverable in the Project window. Its own region id and
            // origin are stamped once, so crop-local overrides keep resolving after the game moves to the full map.
            var overridesPath = ManualFolder + "/HomeSitesOverrides.asset";
            if (AssetDatabase.LoadAssetAtPath<HomeSitesOverrides>(overridesPath) == null)
            {
                var created = ScriptableObject.CreateInstance<HomeSitesOverrides>();
                created.StampRegion(city.region.id, origin.x, origin.y);
                AssetDatabase.CreateAsset(created, overridesPath);
            }

            var sitesAsset = LoadOrCreate<HomeSitesAsset>(SitesAssetPath(regionId));
            sitesAsset.Fill(city.region.id, origin.x, origin.y, siteList);
            EditorUtility.SetDirty(sitesAsset);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static List<WorldGeometryAsset.BuildingRecord> BuildingRecords(CityFile city)
        {
            var list = new List<WorldGeometryAsset.BuildingRecord>();
            foreach (var b in city.buildings ?? Array.Empty<JBuilding>())
            {
                if (b == null) continue;
                var doors = new List<RectInt>();
                foreach (var d in b.doors ?? Array.Empty<JRect>()) doors.Add(Rect(d));
                list.Add(new WorldGeometryAsset.BuildingRecord
                {
                    id = b.id, name = b.name, kind = b.kind, facing = b.facing,
                    rect = Rect(b.rect), parcel = Rect(b.parcel), visual = Rect(b.visual),
                    doorRect = Rect(b.doorRect),
                    enterable = b.enterable, hasDoor = b.hasDoor,
                    variant = b.variant, roofKey = b.roofKey, fixedBuilding = b.isFixed,
                    doors = doors,
                });
            }
            return list;
        }

        /// <summary>
        /// The presentation-only furniture: props, the road graph and its polylines, paths, drives, the baked tram
        /// centreline and the paved areas. None of it is collision (that is <c>solid</c> alone); it exists so
        /// <c>Presentation/City</c> can redraw the reference's riverfrontDraw.ts pass from the asset.
        /// </summary>
        private static WorldGeometryAsset.CityDecor Decor(CityFile city)
        {
            var d = new WorldGeometryAsset.CityDecor();

            foreach (var p in city.props ?? Array.Empty<JProp>())
            {
                if (p == null) continue;
                d.props.Add(new WorldGeometryAsset.PropRecord
                {
                    id = p.id, kind = p.kind, rect = Rect(p.rect), clearable = p.clearable,
                });
            }

            if (city.roads != null)
            {
                foreach (var nd in city.roads.nodes ?? Array.Empty<JRoadNode>())
                    if (nd != null)
                        d.roadNodes.Add(new WorldGeometryAsset.RoadNode {id = nd.id, tile = new Vector2Int(nd.x, nd.y)});
                foreach (var line in city.roads.polylines ?? Array.Empty<JPolyline>()) d.roads.Add(Poly(line));
                if (city.roads.halfWidth > 0) d.roadHalfWidth = city.roads.halfWidth;
                if (city.roads.pavement > 0) d.roadPavement = city.roads.pavement;
            }
            foreach (var line in city.paths ?? Array.Empty<JPolyline>()) d.paths.Add(Poly(line));
            foreach (var line in city.drives ?? Array.Empty<JPolyline>()) d.drives.Add(Poly(line));

            if (city.tram != null)
            {
                foreach (var s in city.tram.baked ?? Array.Empty<JSample>())
                    if (s != null) d.tramSamples.Add(new Vector2(s.x, s.y));
                d.tramLength = city.tram.length;
            }

            if (city.sites != null)
            {
                foreach (var a in city.sites.serviceAreas ?? Array.Empty<JServiceArea>())
                    if (a != null) d.serviceAreas.Add(new RectInt(a.x, a.y, a.w, a.h));
                foreach (var s in city.sites.squares ?? Array.Empty<JSquare>())
                    if (s != null)
                        d.squares.Add(new WorldGeometryAsset.NamedRect
                        {
                            name = s.name ?? "", rect = new RectInt(s.x, s.y, s.w, s.h),
                        });
            }

            if (city.scalars != null)
            {
                if (city.scalars.railHalf > 0) d.railHalfWidth = city.scalars.railHalf;
                d.riverY = city.scalars.riverY;
                if (city.scalars.court != null)
                {
                    d.court = new Vector2Int(city.scalars.court.x, city.scalars.court.y);
                    d.courtRadius = city.scalars.court.radius;
                }
            }
            return d;
        }

        private static WorldGeometryAsset.Poly Poly(JPolyline line)
        {
            var poly = new WorldGeometryAsset.Poly();
            foreach (var p in (line != null ? line.points : null) ?? Array.Empty<JPoint>())
                if (p != null) poly.points.Add(new Vector2Int(p.x, p.y));
            return poly;
        }

        /// <summary>
        /// Sites, in a fixed order: the core, substations, resources, camps (each camp then its spawn groups),
        /// the raid line, tram stops, yards, labels, lights. Ids are the reference ids; a derived id (a camp's group,
        /// a yard, a label) is the reference id or name with an index, so overrides have a stable key.
        ///
        /// The order and the ids are IDENTICAL for every region: what changes between the Home crop and the full city
        /// is the rect, never the key, so a <see cref="HomeSitesOverrides"/> entry authored against the crop still
        /// matches after the game moves to the full map.
        /// </summary>
        private static List<WorldSite> SiteRecords(CityFile city)
        {
            var list = new List<WorldSite>();
            var s = city.sites;
            if (s != null && s.hasCore && s.core != null)
                list.Add(new WorldSite
                {
                    id = s.core.id, name = s.core.name, kind = WorldSiteKind.Core, rect = Rect(s.core.rect),
                    item = "", amount = 0,
                });

            if (s != null)
            {
                var i = 0;
                foreach (var sub in s.substations ?? Array.Empty<JSubstation>())
                {
                    var size = sub.size > 0 ? sub.size : 3;
                    list.Add(new WorldSite
                    {
                        id = "substation:" + i, name = "Substation " + i, kind = WorldSiteKind.Substation,
                        rect = new RectInt(sub.x, sub.y, size, size), item = "", amount = 0,
                    });
                    i++;
                }
                foreach (var r in s.resources ?? Array.Empty<JResource>())
                    list.Add(new WorldSite
                    {
                        id = "resource:" + r.item + ":" + r.x + ":" + r.y, name = r.item + " salvage",
                        kind = WorldSiteKind.Resource, rect = new RectInt(r.x, r.y, r.w, r.h),
                        item = r.item, amount = r.units,
                    });
            }

            if (city.combat != null)
            {
                foreach (var c in city.combat.firstCamps ?? Array.Empty<JCamp>())
                {
                    if (c == null) continue;
                    list.Add(new WorldSite
                    {
                        id = c.id, name = c.name, kind = WorldSiteKind.Camp, rect = new RectInt(c.x, c.y, 1, 1),
                        item = "", amount = c.count,
                    });
                    var g = 0;
                    foreach (var p in c.groups ?? Array.Empty<JPoint>())
                    {
                        var r = c.spawnRadius;
                        list.Add(new WorldSite
                        {
                            id = c.id + ":group:" + g, name = c.name + " group " + g, kind = WorldSiteKind.Camp,
                            // The camp's defenders appear up to spawnRadius tiles away (firstRegion.ts:31), so the
                            // site rect is the area they can occupy, not just the marker.
                            rect = new RectInt(p.x - r, p.y - r, r * 2 + 1, r * 2 + 1), item = "", amount = 0,
                        });
                        g++;
                    }
                }
                if (city.combat.raidLine != null)
                    list.Add(new WorldSite
                    {
                        id = "opening:raid-line", name = "Opening raid approach", kind = WorldSiteKind.RaidLine,
                        rect = new RectInt(city.combat.raidLine.x, city.combat.raidLine.y, city.combat.raidLine.w, 1),
                        item = "", amount = 0,
                    });
            }

            if (s != null)
            {
                foreach (var stop in s.stops ?? Array.Empty<JSite>())
                    list.Add(new WorldSite
                    {
                        id = stop.id, name = stop.name, kind = WorldSiteKind.TramStop,
                        rect = new RectInt(stop.x, stop.y, 2, 2), item = "", amount = 0,
                    });
                var y = 0;
                foreach (var yard in s.yards ?? Array.Empty<JRect>())
                {
                    list.Add(new WorldSite
                    {
                        id = "yard:" + y, name = "Factory yard " + y, kind = WorldSiteKind.Yard,
                        rect = Rect(yard), item = "", amount = 0,
                    });
                    y++;
                }
                var l = 0;
                foreach (var label in s.labels ?? Array.Empty<JLabel>())
                {
                    list.Add(new WorldSite
                    {
                        id = "label:" + l, name = label.name, kind = WorldSiteKind.Label,
                        rect = new RectInt(label.x, label.y, 1, 1), item = "", amount = 0,
                    });
                    l++;
                }
                var li = 0;
                foreach (var light in s.lights ?? Array.Empty<JPoint>())
                {
                    list.Add(new WorldSite
                    {
                        id = "light:" + li, name = "Street light", kind = WorldSiteKind.Light,
                        rect = new RectInt(light.x, light.y, 1, 1), item = "", amount = 0,
                    });
                    li++;
                }
            }
            return list;
        }

        private static RectInt Rect(JRect r) =>
            r == null ? new RectInt(0, 0, 0, 0) : new RectInt(r.x, r.y, r.w, r.h);

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            var leaf = Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        private static string Short(string sha) =>
            string.IsNullOrEmpty(sha) ? "(none)" : sha.Substring(0, Math.Min(12, sha.Length));

        private static ImportResult Fail(ImportResult result, string message)
        {
            result.ok = false;
            result.message = message;
            Debug.LogError("Relight import: " + message);
            if (!Application.isBatchMode) EditorUtility.DisplayDialog("Relight: region import failed", message, "OK");
            return result;
        }
    }
}
