using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Relight.Sim;
using Relight.World;

/// <summary>
/// Founders Court compound generator and verifier (FOUNDERS-COURT-DECISIONS D34–D44, D43 generator).
/// Reads a plan file (Unity/Docs/FOUNDERS-COURT-PLAN.json by default): block rectangle, road polyline, fence line,
/// sidewalk strips, lots, houses, side fences, Works Yard and the fate of every existing object in the block.
/// Every generated object carries a bracket key beginning "fc-" and the generator deletes every "fc-" object before
/// creating, so it can be run again. Tile coordinates: x east, y south, pivot NW corner, Unity pos = (x, -y).
/// </summary>
public static class FoundersCourtCompound
{
    const string WorldRootName = "Editable World";
    const string RefreshMenu = "Relight/World/Refresh Editable World";
    const string FcMarker = "[fc-";

    static string DefaultPlanPath => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Docs/FOUNDERS-COURT-PLAN.json"));
    static string TempDir => Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp"));

    // ------------------------------------------------------------------ menu items

    [MenuItem("Relight/Founders Court/Generate Compound")]
    public static void GenerateCompound() => Generate(DefaultPlanPath);

    [MenuItem("Relight/Founders Court/Verify")]
    public static void VerifyMenu() => Verify(DefaultPlanPath);

    [MenuItem("Relight/Founders Court/Generate From File")]
    public static void GenerateFromFile()
    {
        var path = EditorUtility.OpenFilePanel("Founders Court plan", Path.GetDirectoryName(DefaultPlanPath), "json");
        if (string.IsNullOrEmpty(path)) { Debug.Log("Generate From File: cancelled"); return; }
        Generate(path);
    }

    // ------------------------------------------------------------------ shared helpers

    static StringBuilder log;
    static void L(string s) { log.Append(s).Append('\n'); }

    static void Flush(string file)
    {
        Directory.CreateDirectory(TempDir);
        File.WriteAllText(Path.Combine(TempDir, file), log.ToString());
        foreach (var line in log.ToString().Split('\n')) if (line.Length > 0) Debug.Log(line);
    }

    static RectInt R(JToken t) => new RectInt((int)t[0], (int)t[1], (int)t[2], (int)t[3]);
    static Vector2Int V(JToken t) => new Vector2Int((int)t[0], (int)t[1]);
    static string RS(RectInt r) => "(" + r.x + "," + r.y + ") " + r.width + "x" + r.height;
    static bool Contains(RectInt r, int x, int y) => x >= r.xMin && x < r.xMax && y >= r.yMin && y < r.yMax;
    static bool Overlaps(RectInt a, RectInt b) => a.xMin < b.xMax && b.xMin < a.xMax && a.yMin < b.yMax && b.yMin < a.yMax;
    static bool Inside(RectInt inner, RectInt outer) => inner.xMin >= outer.xMin && inner.xMax <= outer.xMax && inner.yMin >= outer.yMin && inner.yMax <= outer.yMax;
    static IEnumerable<Vector2Int> Tiles(RectInt r)
    {
        for (var y = r.yMin; y < r.yMax; y++) for (var x = r.xMin; x < r.xMax; x++) yield return new Vector2Int(x, y);
    }
    static string Key(string name)
    {
        var i = name.LastIndexOf('['); var j = name.LastIndexOf(']');
        return i >= 0 && j > i ? name.Substring(i + 1, j - i - 1) : "";
    }
    static string BaseName(string name)
    {
        var i = name.LastIndexOf('[');
        return (i > 0 ? name.Substring(0, i) : name).Trim();
    }

    static Transform Root()
    {
        var go = GameObject.Find(WorldRootName);
        if (go == null) throw new Exception("World root '" + WorldRootName + "' not found");
        return go.transform;
    }

    static JObject LoadPlan(string path)
    {
        if (!File.Exists(path)) throw new Exception("Plan file not found: " + path);
        return JObject.Parse(File.ReadAllText(path));
    }

    // ------------------------------------------------------------------ generate

    public static void Generate(string planPath)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogError("Generate: not in Play Mode"); return; }
        log = new StringBuilder();
        try
        {
            var plan = LoadPlan(planPath);
            var root = Root();
            var buildings = root.Find("Buildings"); var props = root.Find("Props"); var sitesT = root.Find("Sites and resources");
            var paths = root.Find("Roads and paths"); var areas = root.Find("Paved areas");
            var block = R(plan["block"]["rect"]);
            var poly = plan["road"]["polyline"].Select(V).ToList();
            L("Generate: plan " + planPath);
            L("Generate: block " + RS(block) + ", road polyline " + string.Join(" -> ", poly.Select(p => "(" + p.x + "," + p.y + ")")));
            Undo.IncrementCurrentGroup(); Undo.SetCurrentGroupName("Founders Court compound");

            // 0. D46: clear baseline decor and baseline solids inside the block before anything is built.
            L("Generate: baseline " + ClearBaseline(root.GetComponent<SceneWorld>().baseline, block, R(plan["road"]["bandRect"]),
                V(plan["road"]["circle"]["centre"]), (float)plan["road"]["circle"]["radius"], false));

            // 1. Delete every existing fc- object (rerunnable).
            var fcOld = root.GetComponentsInChildren<Transform>(true).Where(t => t != root && t.name.Contains(FcMarker)).ToList();
            var removedFc = 0;
            foreach (var t in fcOld) if (t != null) { L("deleted fc " + t.name + " [" + Key(t.name) + "] " + Pos(t)); Undo.DestroyObjectImmediate(t.gameObject); removedFc++; }
            L("Generate: removed " + removedFc + " earlier fc- objects");

            // 2. Fates: delete. Located by category group + name, then by exact name anywhere under the root.
            var deleted = 0; var missing = 0;
            foreach (var f in plan["fates"])
            {
                if ((string)f["fate"] != "delete") continue;
                var name = (string)f["name"]; var cat = (string)f["category"]; var key = (string)f["key"];
                var t = FindObject(root, cat, name);
                if (t == null) { L("MISSING delete " + name + " [" + key + "] (" + cat + ")"); missing++; continue; }
                L("deleted " + name + " [" + key + "] " + Pos(t) + " (" + cat + ")");
                Undo.DestroyObjectImmediate(t.gameObject); deleted++;
            }
            L("Generate: deleted " + deleted + " objects, " + missing + " missing");

            // 3. Sidewalks (SceneArea strips, decor only).
            foreach (var s in plan["sidewalk"]["strips"])
            {
                var key = (string)s["key"]; var r = R(s["rect"]);
                var go = New("Sidewalk " + (string)s["side"] + " [" + key + "]", areas, r.x, r.y);
                var a = go.AddComponent<SceneArea>(); a.areaName = "Founders Court sidewalk"; a.serviceArea = false; a.size = new Vector2Int(r.width, r.height);
                L("generated SceneArea " + go.name + " [" + key + "] " + RS(r));
            }

            // 4. Compound fence segments.
            foreach (var s in plan["fenceLine"]["segments"])
            {
                var key = (string)s["key"]; var r = R(s["rect"]);
                Fence("Compound fence " + (string)s["side"] + " [" + key + "]", props, key, r);
            }

            // 5. Side fences.
            foreach (var s in plan["sideFences"])
            {
                var key = (string)s["key"]; var r = R(s["rect"]);
                Fence("Side fence " + string.Join("-", s["lots"].Select(x => (string)x)) + " [" + key + "]", props, key, r);
            }

            // 6. Houses and stubs.
            foreach (var h in plan["houses"])
            {
                var fate = (string)h["fate"]; var lot = (string)h["lot"]; var key = (string)h["key"]; var r = R(h["rect"]);
                var doors = h["doorsLocal"].Select(R).ToList();
                if (fate == "new copy")
                {
                    var src = (string)h["source"];
                    var go = New(BaseName(src) + " [" + key + "]", buildings, r.x, r.y);
                    var b = go.AddComponent<SceneBuilding>();
                    b.id = key; b.buildingName = BaseName(src); b.kind = (string)h["kind"]; b.size = new Vector2Int(r.width, r.height);
                    b.enterable = (bool)h["enterable"]; b.doors = doors; b.variant = (int)h["variant"]; b.roofKey = (string)h["roof"];
                    b.campaignBuilding = (bool)h["campaign"];
                    L("generated SceneBuilding " + go.name + " [" + key + "] " + RS(r) + " roof=" + b.roofKey + " door=" + string.Join(";", doors.Select(RS)) + " (copy of " + src + ")");
                }
                else if (fate == "keep and move" || fate == "keep in place")
                {
                    var src = (string)h["source"];
                    var t = FindObject(root, "Buildings", src);
                    if (t == null) { L("MISSING house " + src + " for lot " + lot); continue; }
                    var b = t.GetComponent<SceneBuilding>();
                    var old = SceneWorld.Rect(t, b.size); var oldDoors = string.Join(";", b.doors.Select(RS)); var oldRoof = b.roofKey;
                    Undo.RecordObject(t, "Move house"); Undo.RecordObject(b, "Move house");
                    if (fate == "keep and move") t.position = new Vector3(r.x, -r.y, t.position.z);
                    b.size = new Vector2Int(r.width, r.height); b.doors = doors; b.roofKey = (string)h["roof"];
                    EditorUtility.SetDirty(b);
                    L((fate == "keep and move" ? "moved " : "kept ") + t.name + " [" + b.id + "] old " + RS(old) + " new " + RS(SceneWorld.Rect(t, b.size)) +
                      " doors old " + oldDoors + " new " + string.Join(";", doors.Select(RS)) + " roof old " + oldRoof + " new " + b.roofKey + " (lot " + lot + ")");
                }
                var stub = h["stub"]; var sk = (string)stub["key"]; var spts = stub["points"].Select(V).ToList(); var p0 = spts[0];
                var pg = New("Stub " + lot + " [" + sk + "]", paths, p0.x, p0.y);
                var sp = pg.AddComponent<ScenePath>(); sp.kind = ScenePathKind.Path; sp.points = spts.Select(q => q - p0).ToList();
                L("generated ScenePath " + pg.name + " [" + sk + "] " + string.Join("->", spts.Select(q => "(" + q.x + "," + q.y + ")")));
            }

            // 7. Works Yard, nodes and substation.
            var yard = plan["yard"];
            MoveSite(root, (string)yard["siteName"], R(yard["rect"]));
            foreach (var n in yard["nodes"]) MoveSite(root, (string)n["name"], R(n["rect"]));
            MoveSite(root, (string)yard["substation"]["name"], R(yard["substation"]["rect"]));

            // 8. D48–D53: the yard as its own property. The two former YARD side fences are replaced by the yard's own
            //    west edge and south edge; the compound north/east fences arrive already split through plan fenceLine.
            foreach (var oldKey in new[] { "fc-sidefence-WS-YARD", "fc-sidefence-E2-YARD" })
            {
                var t = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x != root && Key(x.name) == oldKey);
                if (t == null) { L("side fence " + oldKey + " not present (replaced by the yard fence)"); continue; }
                L("deleted side fence " + t.name + " [" + oldKey + "] " + Pos(t) + " (replaced by the yard fence)"); Undo.DestroyObjectImmediate(t.gameObject);
            }
            if (yard["yardFences"] != null)
            {
                var compoundKeys = new HashSet<string>(plan["fenceLine"]["segments"].Select(x => (string)x["key"]));
                foreach (var s in yard["yardFences"])
                {
                    var key = (string)s["key"]; var r = R(s["rect"]);
                    // The north run and the east run are compound fence segments already generated in step 4 (shared tiles, one object each).
                    if (compoundKeys.Contains(key)) { L("yard fence " + (string)s["side"] + " [" + key + "] " + RS(r) + " is compound fence segment, already generated"); continue; }
                    Fence("Yard fence " + (string)s["side"] + " [" + key + "]", props, key, r);
                }
                // D49/D51: the broken section, a non-blocking debris placeholder over exactly the four gap tiles.
                var gap = yard["yardGap"]; var gr = R(gap["rect"]); var gk = (string)gap["key"];
                var gg = New((string)gap["name"] + " [" + gk + "]", props, gr.x, gr.y);
                var gp = gg.AddComponent<SceneProp>(); gp.id = gk; gp.kind = (string)gap["propKind"]; gp.size = new Vector2Int(gr.width, gr.height); gp.blocksMovement = false; gp.clearable = false;
                L("generated SceneProp " + gp.kind + " " + gg.name + " [" + gk + "] " + RS(gr) + " blocksMovement=0 (D51 placeholder)");
                // D50: the closed gate, a solid fence object over exactly the four gate tiles.
                var gate = yard["yardGate"]; var tr = R(gate["rect"]); var tk = (string)gate["key"];
                Fence((string)gate["name"] + " [" + tk + "]", props, tk, tr);
                // D53: the office, copied from the smallest house in the block (plan yard.office.sourceKey), door on the west wall.
                var of = yard["office"]; var orc = R(of["rect"]); var ok = (string)of["key"]; var srcKey = (string)of["sourceKey"];
                var srcB = root.GetComponentsInChildren<SceneBuilding>().FirstOrDefault(b => b.id == srcKey);
                var og = New((string)of["name"] + " [" + ok + "]", buildings, orc.x, orc.y);
                var ob = og.AddComponent<SceneBuilding>();
                ob.id = ok; ob.buildingName = (string)of["name"]; ob.kind = srcB != null ? srcB.kind : (string)of["kind"]; ob.variant = srcB != null ? srcB.variant : (int)of["variant"];
                ob.size = new Vector2Int(orc.width, orc.height); ob.enterable = false; ob.campaignBuilding = false; ob.roofKey = (string)of["roofKey"];
                ob.doors = of["doorsLocal"].Select(R).ToList();
                L("generated SceneBuilding " + og.name + " [" + ok + "] " + RS(orc) + " roof=" + ob.roofKey + " door=" + string.Join(";", ob.doors.Select(RS)) + " (copy of " + (srcB != null ? srcB.name : srcKey + " (not in scene; plan fields)") + ", not enterable)");
                var vergeLot = plan["lots"].FirstOrDefault(l => (string)l["name"] == "VERGE");
                if (vergeLot != null) L("lot VERGE [" + (string)vergeLot["key"] + "] " + string.Join(" ", vergeLot["rects"].Select(x => RS(R(x)))) + ": plan data only, lots have no scene object");
            }
            else L("Generate: plan has no yard.yardFences; yard property objects not generated");

            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            L("Generate: done");
        }
        catch (Exception e) { L("Generate: FAILED " + e.Message + "\n" + e.StackTrace); }
        Flush("fc_generate_log.txt");
        if (!log.ToString().Contains("Generate: FAILED"))
        {
            EditorApplication.ExecuteMenuItem(RefreshMenu);
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("Generate: refreshed and saved");
        }
    }

    static string Pos(Transform t) => "at (" + Mathf.RoundToInt(t.position.x) + "," + Mathf.RoundToInt(-t.position.y) + ")";

    static Transform FindObject(Transform root, string category, string name)
    {
        var group = root.Find(category);
        if (group != null) { var t = group.Find(name); if (t != null) return t; }
        foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name && t != root) return t;
        return null;
    }

    static GameObject New(string name, Transform parent, int x, int y)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, -y, 0);
        Undo.RegisterCreatedObjectUndo(go, "Founders Court compound");
        return go;
    }

    static void Fence(string name, Transform props, string key, RectInt r)
    {
        var go = New(name, props, r.x, r.y);
        var f = go.AddComponent<SceneProp>();
        f.id = key; f.kind = "fence"; f.size = new Vector2Int(r.width, r.height); f.blocksMovement = true; f.clearable = false;
        L("generated SceneProp fence " + go.name + " [" + key + "] " + RS(r) + " blocksMovement=1");
    }

    static void MoveSite(Transform root, string name, RectInt r)
    {
        var t = FindObject(root, "Sites and resources", name);
        if (t == null) { L("MISSING site " + name); return; }
        var s = t.GetComponent<SceneSite>();
        var old = SceneWorld.Rect(t, s.size);
        if (old.Equals(r)) { L("kept site " + name + " [" + s.id + "] " + RS(old)); return; }
        Undo.RecordObject(t, "Move site"); Undo.RecordObject(s, "Move site");
        t.position = new Vector3(r.x, -r.y, t.position.z); s.size = new Vector2Int(r.width, r.height);
        EditorUtility.SetDirty(s);
        L("moved site " + name + " [" + s.id + "] old " + RS(old) + " new " + RS(r));
    }

    // ------------------------------------------------------------------ verify

    sealed class Item { public string name, key; public RectInt rect; public string kind; public Component c; }

    public static void Verify(string planPath)
    {
        log = new StringBuilder();
        var passed = 0; var failed = 0;
        Action<string, bool, string> check = (id, ok, detail) => { if (ok) passed++; else failed++; L((ok ? "PASS " : "FAIL ") + id + ": " + detail); };
        WorldGeometryAsset g = null;
        try
        {
            var plan = LoadPlan(planPath);
            var root = Root(); var world = root.GetComponent<SceneWorld>();
            WorldSites sites; g = world.Compile(out sites);
            var block = R(plan["block"]["rect"]);
            var interiorX = plan["interior"]["x"]; var interiorY = plan["interior"]["y"];
            var interior = new RectInt((int)interiorX[0], (int)interiorY[0], (int)interiorX[1] - (int)interiorX[0] + 1, (int)interiorY[1] - (int)interiorY[0] + 1);
            var band = R(plan["road"]["bandRect"]);
            var mouthY = (int)plan["road"]["mouth"]["y"]; var mouthX0 = (int)plan["road"]["mouth"]["x"][0]; var mouthX1 = (int)plan["road"]["mouth"]["x"][1];
            var circle = new HashSet<Vector2Int>(plan["road"]["circle"]["tiles"].Select(V));
            var setback = (int)plan["constants"]["setback"];

            var bld = root.GetComponentsInChildren<SceneBuilding>().Select(b => new Item { name = b.name, key = b.id, rect = SceneWorld.Rect(b.transform, b.size), kind = b.kind, c = b }).ToList();
            var prp = root.GetComponentsInChildren<SceneProp>().Select(p => new Item { name = p.name, key = p.id, rect = SceneWorld.Rect(p.transform, p.size), kind = p.kind, c = p }).ToList();
            var sts = root.GetComponentsInChildren<SceneSite>().Select(s => new Item { name = s.name, key = s.id, rect = SceneWorld.Rect(s.transform, s.size), kind = s.kind.ToString(), c = s }).ToList();
            var pth = root.GetComponentsInChildren<ScenePath>().ToList();
            var ars = root.GetComponentsInChildren<SceneArea>().Select(a => new Item { name = a.name, key = Key(a.name), rect = SceneWorld.Rect(a.transform, a.size), kind = a.serviceArea ? "service" : "square", c = a }).ToList();
            var fences = prp.Where(p => p.kind == "fence" && ((SceneProp)p.c).blocksMovement).ToList();
            var blocking = prp.Where(p => ((SceneProp)p.c).blocksMovement).ToList();
            var substations = sts.Where(s => s.kind == "Substation").ToList();
            Func<int, int, bool> walk = (x, y) => x >= 0 && y >= 0 && x < g.Width && y < g.Height && !g.SolidAt(x, y);

            // C1 compound closed (D42): mouth temporarily solid, flood from a ring road tile.
            {
                // The mouth is the road band's crossing of the fence line and the sidewalk outside it: blocking the block-edge
                // row alone leaks, because the sidewalk rows between the fence and the edge are open and join the ring road.
                var fenceY1 = (int)plan["fenceLine"]["y"][1];
                var blocked = new HashSet<Vector2Int>();
                for (var y = Mathf.Min(fenceY1, mouthY); y <= Mathf.Max(fenceY1, mouthY); y++) for (var x = mouthX0; x <= mouthX1; x++) blocked.Add(new Vector2Int(x, y));
                var start = new Vector2Int((mouthX0 + mouthX1) / 2, mouthY + 8);
                var lim = new RectInt(block.xMin - 15, block.yMin - 15, block.width + 30, block.height + 30);
                var seen = new HashSet<Vector2Int> { start }; var q = new Queue<Vector2Int>(); q.Enqueue(start);
                while (q.Count > 0)
                {
                    var t = q.Dequeue();
                    foreach (var d in new[] { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down })
                    {
                        var n = t + d;
                        if (seen.Contains(n) || blocked.Contains(n) || !Contains(lim, n.x, n.y) || !walk(n.x, n.y)) continue;
                        seen.Add(n); q.Enqueue(n);
                    }
                }
                // D52: the enclosure is the court interior plus the yard rect plus the verge; a reached verge or yard tile is a failure too.
                var yardRectC1 = R(plan["yard"]["rect"]); var vergeRectsC1 = plan["yard"]["verge"] == null ? new List<RectInt>() : new List<RectInt> { R(plan["yard"]["verge"]) };
                Func<Vector2Int, bool> inEnclosure = t => Contains(interior, t.x, t.y) || Contains(yardRectC1, t.x, t.y) || vergeRectsC1.Any(v => Contains(v, t.x, t.y));
                var reached = seen.Where(inEnclosure).OrderBy(t => t.y).ThenBy(t => t.x).ToList();
                var reachedYard = reached.Count(t => Contains(yardRectC1, t.x, t.y)); var reachedVerge = reached.Count(t => vergeRectsC1.Any(v => Contains(v, t.x, t.y)));
                var sample = string.Join(" ", reached.Take(20).Select(t => t.x + "," + t.y));
                check("C1 compound closed", reached.Count == 0, "enclosure tiles reached with mouth solid = " + reached.Count + " (court " + (reached.Count - reachedYard - reachedVerge) + ", verge " + reachedVerge + ", yard " + reachedYard + "; enclosure = interior " + RS(interior) + " + yard " + RS(yardRectC1) + " + verge " + string.Join(" ", vergeRectsC1.Select(RS)) + "; mouth x " + mouthX0 + ".." + mouthX1 + ", y " + Mathf.Min(fenceY1, mouthY) + ".." + Mathf.Max(fenceY1, mouthY) + " blocked; start " + start.x + "," + start.y + ", start walkable " + walk(start.x, start.y) + ")" + (reached.Count > 0 ? "; first: " + sample : ""));
            }

            // C2 fence solid.
            {
                var bad = new List<string>(); var total = 0;
                foreach (var s in plan["fenceLine"]["segments"])
                    foreach (var t in Tiles(R(s["rect"]))) { total++; if (!g.SolidAt(t.x, t.y)) bad.Add(t.x + "," + t.y); }
                check("C2 fence solid", bad.Count == 0, total + " compound fence tiles, " + bad.Count + " not solid" + (bad.Count > 0 ? ": " + string.Join(" ", bad.Take(20)) : ""));
            }

            // C3 no baseline solids.
            {
                var cover = new HashSet<Vector2Int>();
                foreach (var b in bld) foreach (var t in Tiles(b.rect)) cover.Add(t);
                foreach (var f in fences) foreach (var t in Tiles(f.rect)) cover.Add(t);
                foreach (var s in substations) foreach (var t in Tiles(s.rect)) cover.Add(t);
                var bad = new List<string>(); var solid = 0;
                foreach (var t in Tiles(block)) if (g.SolidAt(t.x, t.y)) { solid++; if (!cover.Contains(t)) bad.Add(t.x + "," + t.y); }
                check("C3 no baseline solids", bad.Count == 0, solid + " solid tiles in block, " + bad.Count + " outside any building/fence/substation" + (bad.Count > 0 ? ": " + string.Join(" ", bad.Take(20)) : ""));
            }

            // House lookup: plan house -> scene building.
            var lots = plan["lots"].ToDictionary(l => (string)l["name"], l => l);
            var houses = new List<(JToken plan, Item item)>();
            foreach (var h in plan["houses"])
            {
                var key = (string)h["key"]; var src = (string)h["source"]; var fate = (string)h["fate"];
                var item = fate == "new copy" ? bld.FirstOrDefault(b => b.key == key) : bld.FirstOrDefault(b => b.name == src);
                houses.Add((h, item));
            }

            // C4 houses inside lots, overlapping nothing.
            {
                var bad = new List<string>();
                foreach (var (h, it) in houses)
                {
                    var lot = (string)h["lot"];
                    if (it == null) { bad.Add(lot + ": house missing"); continue; }
                    var lotRects = lots[lot]["rects"].Select(R).ToList();
                    var outside = Tiles(it.rect).Count(t => !lotRects.Any(lr => Contains(lr, t.x, t.y)));
                    if (outside > 0) bad.Add(lot + ": " + outside + " tiles outside lot");
                    foreach (var o in bld.Where(o => o != it && Overlaps(o.rect, it.rect))) bad.Add(lot + ": overlaps building " + o.name);
                    foreach (var o in prp.Where(o => Overlaps(o.rect, it.rect))) bad.Add(lot + ": overlaps prop " + o.name);
                    foreach (var o in sts.Where(o => o.kind != "Core" && Overlaps(o.rect, it.rect))) bad.Add(lot + ": overlaps site " + o.name);
                    if (Overlaps(band, it.rect)) bad.Add(lot + ": overlaps road band");
                }
                check("C4 houses in lots", bad.Count == 0, houses.Count + " houses; " + (bad.Count == 0 ? "all inside their lot, no overlaps" : string.Join("; ", bad)));
            }

            // C5 setback (workshop reported as an exception, go message answer 2).
            {
                var bad = new List<string>(); var report = new List<string>();
                foreach (var (h, it) in houses)
                {
                    if (it == null) continue;
                    var lot = (string)h["lot"]; var side = (string)lots[lot]["side"]; var edge = (int)lots[lot]["road_edge"];
                    var sb = Setback(side, edge, it.rect);
                    var exception = (string)h["sourceKey"] == "home-workshop";
                    report.Add(lot + "=" + sb + (exception ? " (workshop exception, D25)" : ""));
                    if (!exception && sb != setback) bad.Add(lot + " setback " + sb);
                }
                check("C5 setback " + setback, bad.Count == 0, string.Join(", ", report) + (bad.Count > 0 ? "; wrong: " + string.Join(", ", bad) : ""));
            }

            // C6 door on front wall, exactly one stub from the door to the band/circle.
            {
                var bad = new List<string>();
                var fcStubs = pth.Where(p => p.name.Contains(FcMarker) && p.kind == ScenePathKind.Path).Select(p => new { p, pts = p.Record().points }).ToList();
                foreach (var (h, it) in houses)
                {
                    if (it == null) continue;
                    var lot = (string)h["lot"]; var side = (string)lots[lot]["side"]; var b = (SceneBuilding)it.c;
                    // D45: the plan may name the door wall itself (Foreman workshop: "S"); otherwise the door sits on the wall facing the road for the lot's side.
                    var wall = h["doorWall"] != null ? (string)h["doorWall"] : side == "W" ? "E" : side == "E" ? "W" : side == "N" ? "S" : "N";
                    var doorTiles = new List<Vector2Int>();
                    foreach (var d in b.doors)
                    {
                        var w = new RectInt(it.rect.x + d.x, it.rect.y + d.y, d.width, d.height);
                        foreach (var t in Tiles(w))
                        {
                            doorTiles.Add(t);
                            var onFront = wall == "W" ? t.x == it.rect.xMin : wall == "E" ? t.x == it.rect.xMax - 1 : wall == "N" ? t.y == it.rect.yMin : t.y == it.rect.yMax - 1;
                            if (!onFront) bad.Add(lot + ": door tile " + t.x + "," + t.y + " not on the " + wall + " wall");
                        }
                    }
                    if (doorTiles.Count == 0) bad.Add(lot + ": no door");
                    var touching = 0;
                    foreach (var s in fcStubs)
                    {
                        var first = s.pts.First(); var last = s.pts.Last();
                        var adj = doorTiles.Any(t => Mathf.Abs(t.x - first.x) + Mathf.Abs(t.y - first.y) == 1);
                        if (!adj) continue;
                        touching++;
                        if (!(Contains(band, last.x, last.y) || circle.Contains(last))) bad.Add(lot + ": stub " + s.p.name + " ends off the road at " + last.x + "," + last.y);
                    }
                    if (touching != 1) bad.Add(lot + ": " + touching + " stubs touch the door");
                }
                check("C6 doors and stubs", bad.Count == 0, bad.Count == 0 ? houses.Count + " houses, each door on its planned wall with one stub (first point at the door, last point on the road, any point count)" : string.Join("; ", bad));
            }

            // C7 roof variety between neighbours (pairs from the plan's side fences).
            {
                var bad = new List<string>(); var pairs = 0;
                var byLot = houses.Where(x => x.item != null).ToDictionary(x => (string)x.plan["lot"], x => (SceneBuilding)x.item.c);
                foreach (var s in plan["sideFences"])
                {
                    var a = (string)s["lots"][0]; var bl = (string)s["lots"][1];
                    if (!byLot.ContainsKey(a) || !byLot.ContainsKey(bl)) continue;
                    pairs++;
                    if (byLot[a].roofKey == byLot[bl].roofKey) bad.Add(a + "/" + bl + " both " + byLot[a].roofKey);
                }
                check("C7 roof variety", bad.Count == 0, pairs + " neighbour pairs" + (bad.Count == 0 ? ", all roof keys differ" : "; same: " + string.Join(", ", bad)));
            }

            // C8 edge clear: nothing on the sidewalks or the block edge.
            {
                var zone = new HashSet<Vector2Int>();
                foreach (var s in plan["sidewalk"]["strips"]) foreach (var t in Tiles(R(s["rect"]))) zone.Add(t);
                for (var x = block.xMin; x < block.xMax; x++) { zone.Add(new Vector2Int(x, block.yMin)); zone.Add(new Vector2Int(x, block.yMax - 1)); }
                for (var y = block.yMin; y < block.yMax; y++) { zone.Add(new Vector2Int(block.xMin, y)); zone.Add(new Vector2Int(block.xMax - 1, y)); }
                var bad = new List<string>();
                foreach (var o in bld.Concat(prp).Concat(sts)) if (Tiles(o.rect).Any(zone.Contains)) bad.Add(o.name);
                check("C8 edge clear", bad.Count == 0, zone.Count + " sidewalk/edge tiles" + (bad.Count == 0 ? ", no building, prop or site on them" : "; on them: " + string.Join(", ", bad)));
            }

            // C9 yard.
            {
                var yard = plan["yard"]; var want = R(yard["rect"]); var bad = new List<string>();
                var ys = sts.FirstOrDefault(s => s.name == (string)yard["siteName"]);
                if (ys == null) bad.Add("yard site missing"); else if (!ys.rect.Equals(want)) bad.Add("yard rect " + RS(ys.rect) + " != " + RS(want));
                var nodes = new List<Item>();
                foreach (var n in yard["nodes"])
                {
                    var it = sts.FirstOrDefault(s => s.name == (string)n["name"]);
                    if (it == null) { bad.Add("node missing " + (string)n["name"]); continue; }
                    nodes.Add(it);
                    if (!Inside(it.rect, want)) bad.Add(it.name + " outside yard");
                }
                var sub = sts.FirstOrDefault(s => s.name == (string)yard["substation"]["name"]);
                if (sub == null) bad.Add("substation missing");
                else
                {
                    if (!Inside(sub.rect, want)) bad.Add("substation outside yard");
                    var copper = nodes.FirstOrDefault(n => n.key.Contains("copper"));
                    if (copper == null || Gap(sub.rect, copper.rect) != 0) bad.Add("substation does not touch copper");
                }
                var core = sts.FirstOrDefault(s => s.kind == "Core");
                var doorTiles = new List<RectInt>();
                if (core != null)
                {
                    var wb = bld.FirstOrDefault(b => b.rect.Equals(core.rect));
                    if (wb != null) foreach (var d in ((SceneBuilding)wb.c).doors) doorTiles.Add(new RectInt(wb.rect.x + d.x, wb.rect.y + d.y, d.width, d.height));
                }
                foreach (var n in nodes)
                {
                    var gd = doorTiles.Count == 0 ? -1 : doorTiles.Min(d => Gap(n.rect, d));
                    if (gd < 8) bad.Add(n.name + " gap to workshop door " + gd + " < 8");
                }
                for (var i = 0; i < nodes.Count; i++) for (var j = i + 1; j < nodes.Count; j++)
                    if (Gap(nodes[i].rect, nodes[j].rect) < 4) bad.Add(nodes[i].name + "/" + nodes[j].name + " gap " + Gap(nodes[i].rect, nodes[j].rect) + " < 4");
                check("C9 yard", bad.Count == 0, "yard " + (ys == null ? "missing" : RS(ys.rect)) + ", " + nodes.Count + " nodes, substation " + (sub == null ? "missing" : RS(sub.rect)) + (bad.Count == 0 ? "; all inside, D9 gaps ok, substation touches copper" : "; " + string.Join("; ", bad)));
            }

            // C10 raid line.
            {
                var want = R(plan["fixed"]["raidLine"]["rect"]);
                var lines = sites.OfKind(SiteKind.RaidLine).ToList();
                var ry = lines.Count == 0 ? int.MinValue : lines.Max(s => s.Y);
                var ok = lines.Count == 1 && lines[0].X == want.x && lines[0].Y == want.y && lines[0].W == want.width && lines[0].H == want.height && ry == want.y;
                var ctx = new SimContext(CatalogueData.Build(), new ArrayGeometry(1, 1, new byte[1], new bool[1], new Vec2(0, 0)), null, null, sites);
                var rulesY = DirectorRules.RaidLineY(ctx);
                ok &= rulesY == want.y;
                check("C10 raid line", ok, lines.Count + " raid line site(s): " + string.Join(" ", lines.Select(s => "(" + s.X + "," + s.Y + ") " + s.W + "x" + s.H)) + "; DirectorRules.RaidLineY = " + rulesY + " (want y " + want.y + " x " + want.xMin + ".." + (want.xMax - 1) + ")");
            }

            // C11 spawn.
            {
                var want = plan["fixed"]["playerSpawn"]; var wx = (float)want[0]; var wy = (float)want[1];
                var p = world.playerSpawn == null ? Vector3.zero : world.playerSpawn.position;
                var ok = world.playerSpawn != null && Mathf.Approximately(p.x, wx) && Mathf.Approximately(-p.y, wy) && g.Spawn == V(plan["fixed"]["spawnTile"]);
                check("C11 spawn", ok, "playerSpawn " + (world.playerSpawn == null ? "null" : "(" + p.x + "," + (-p.y) + ")") + ", compiled spawn tile (" + g.Spawn.x + "," + g.Spawn.y + "), want (" + wx + "," + wy + ")");
            }

            // C12 plan match.
            {
                var expected = new Dictionary<string, RectInt>();
                foreach (var s in plan["fenceLine"]["segments"]) expected[(string)s["key"]] = R(s["rect"]);
                foreach (var s in plan["sideFences"]) expected[(string)s["key"]] = R(s["rect"]);
                foreach (var s in plan["sidewalk"]["strips"]) expected[(string)s["key"]] = R(s["rect"]);
                if (plan["yard"]["yardFences"] != null)
                {
                    foreach (var s in plan["yard"]["yardFences"]) expected[(string)s["key"]] = R(s["rect"]);
                    expected[(string)plan["yard"]["yardGap"]["key"]] = R(plan["yard"]["yardGap"]["rect"]);
                    expected[(string)plan["yard"]["yardGate"]["key"]] = R(plan["yard"]["yardGate"]["rect"]);
                    expected[(string)plan["yard"]["office"]["key"]] = R(plan["yard"]["office"]["rect"]);
                }
                var stubRects = new Dictionary<string, (Vector2Int a, Vector2Int b)>();
                foreach (var h in plan["houses"])
                {
                    if ((string)h["fate"] == "new copy") expected[(string)h["key"]] = R(h["rect"]);
                    stubRects[(string)h["stub"]["key"]] = (V(h["stub"]["points"].First()), V(h["stub"]["points"].Last()));
                }
                var actual = new Dictionary<string, RectInt>();
                foreach (var o in bld.Concat(prp).Concat(ars)) if (o.name.Contains(FcMarker)) actual[Key(o.name)] = o.rect;
                var actualStubs = new Dictionary<string, (Vector2Int a, Vector2Int b)>();
                foreach (var p in pth) if (p.name.Contains(FcMarker)) { var pts = p.Record().points; actualStubs[Key(p.name)] = (pts.First(), pts.Last()); }
                var bad = new List<string>();
                foreach (var kv in expected) { RectInt r; if (!actual.TryGetValue(kv.Key, out r)) bad.Add("missing " + kv.Key); else if (!r.Equals(kv.Value)) bad.Add(kv.Key + " " + RS(r) + " != " + RS(kv.Value)); }
                foreach (var k in actual.Keys) if (!expected.ContainsKey(k)) bad.Add("unplanned " + k);
                foreach (var kv in stubRects) { (Vector2Int a, Vector2Int b) s; if (!actualStubs.TryGetValue(kv.Key, out s)) bad.Add("missing " + kv.Key); else if (s.a != kv.Value.a || s.b != kv.Value.b) bad.Add(kv.Key + " points differ"); }
                foreach (var k in actualStubs.Keys) if (!stubRects.ContainsKey(k)) bad.Add("unplanned " + k);
                foreach (var (h, it) in houses)
                    if ((string)h["fate"] != "new copy") { if (it == null) bad.Add("missing kept house " + (string)h["source"]); else if (!it.rect.Equals(R(h["rect"]))) bad.Add((string)h["lot"] + " kept house rect " + RS(it.rect) + " != " + RS(R(h["rect"]))); }
                var fcTotal = actual.Count + actualStubs.Count;
                check("C12 plan match", bad.Count == 0, fcTotal + " fc objects in scene, " + (expected.Count + stubRects.Count) + " planned" + (bad.Count == 0 ? ", all rects match; kept houses at plan rects" : "; " + string.Join("; ", bad)));
            }

            // C13 walk steps core -> mouth, + 10 <= cap.
            {
                var core = sites.OfKind(SiteKind.Core).FirstOrDefault();
                var steps = -1;
                if (core != null)
                {
                    var cr = new RectInt(core.X, core.Y, core.W, core.H);
                    var dist = new Dictionary<Vector2Int, int>(); var q = new Queue<Vector2Int>();
                    for (var y = cr.yMin - 1; y <= cr.yMax; y++) for (var x = cr.xMin - 1; x <= cr.xMax; x++)
                        if (!Contains(cr, x, y) && walk(x, y)) { var t = new Vector2Int(x, y); dist[t] = 0; q.Enqueue(t); }
                    var lim = new RectInt(block.xMin - 15, block.yMin - 15, block.width + 30, block.height + 30);
                    while (q.Count > 0)
                    {
                        var t = q.Dequeue();
                        foreach (var d in new[] { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down })
                        {
                            var n = t + d;
                            if (dist.ContainsKey(n) || !Contains(lim, n.x, n.y) || !walk(n.x, n.y)) continue;
                            dist[n] = dist[t] + 1; q.Enqueue(n);
                        }
                    }
                    var best = int.MaxValue;
                    for (var x = mouthX0; x <= mouthX1; x++) { int v; if (dist.TryGetValue(new Vector2Int(x, mouthY), out v) && v < best) best = v; }
                    steps = best == int.MaxValue ? -1 : best;
                }
                var cap = SiegeTuning.Fallback.EntryFarSteps;   // REL-84: the far band is tuning; the check reads the shipped default
                check("C13 walk steps", steps >= 0 && steps + 10 <= cap, "core -> mouth row " + steps + " steps, + 10 = " + (steps + 10) + ", cap EntryFarSteps = " + cap);
            }

            // C14 baseline clear (D46): nothing left to clear in the baseline asset inside the block.
            {
                var report = ClearBaseline(world.baseline, block, band, V(plan["road"]["circle"]["centre"]), (float)plan["road"]["circle"]["radius"], true);
                check("C14 baseline clear", report.StartsWith("clear"), report);
            }

            // C15–C19: the Works Yard as its own property (D48–D53).
            {
                var yard = plan["yard"];
                var yardRect = R(yard["rect"]);
                var edges = new (string name, RectInt r)[] { ("west edge", R(yard["westEdge"])), ("south edge", R(yard["southEdge"])), ("north run", R(yard["northRun"])), ("east run", R(yard["eastRun"])) };
                var gapTiles = new HashSet<Vector2Int>(yard["yardGap"]["tiles"].Select(V)); var gapKey = (string)yard["yardGap"]["key"];
                var gateTiles = new HashSet<Vector2Int>(yard["yardGate"]["tiles"].Select(V)); var gateKey = (string)yard["yardGate"]["key"];
                var yardFenceKeys = new HashSet<string>(yard["yardFences"].Select(f => (string)f["key"]));
                var all = bld.Concat(prp).Concat(sts).Concat(ars).ToList();
                Func<Vector2Int, List<Item>> at = t => all.Where(o => Contains(o.rect, t.x, t.y)).ToList();

                // C15 yard boundary: every tile of the west edge, south edge, north run and east run is a fence, gate or gap tile and nothing else.
                {
                    var bad = new List<string>(); var total = 0; var nFence = 0; var nGate = 0; var nGap = 0;
                    foreach (var e in edges) foreach (var t in Tiles(e.r))
                    {
                        total++;
                        var here = at(t);
                        var want = gapTiles.Contains(t) ? "gap" : gateTiles.Contains(t) ? "gate" : "fence";
                        var okTile = false;
                        if (here.Count == 1)
                        {
                            var o = here[0]; var sp = o.c as SceneProp;
                            if (want == "gap") okTile = o.key == gapKey && sp != null && sp.kind == "debris" && !sp.blocksMovement;
                            else if (want == "gate") okTile = o.key == gateKey && sp != null && sp.kind == "fence" && sp.blocksMovement;
                            else okTile = yardFenceKeys.Contains(o.key) && sp != null && sp.kind == "fence" && sp.blocksMovement;
                        }
                        if (okTile) { if (want == "gap") nGap++; else if (want == "gate") nGate++; else nFence++; }
                        else if (bad.Count < 12) bad.Add(e.name + " " + t.x + "," + t.y + " want " + want + " got [" + string.Join(", ", here.Select(o => o.name)) + "]");
                    }
                    check("C15 yard boundary", bad.Count == 0, total + " edge tiles: " + nFence + " fence, " + nGate + " gate, " + nGap + " gap" + (bad.Count == 0 ? ", nothing else" : "; " + string.Join("; ", bad)));
                }

                // Flood fill from the circle centre tile over non-solid tiles, 4-connected, optional extra solid tiles.
                var centre = V(plan["road"]["circle"]["centre"]);
                var limY = new RectInt(block.xMin - 15, block.yMin - 15, block.width + 30, block.height + 30);
                Func<HashSet<Vector2Int>, HashSet<Vector2Int>> flood = extraSolid =>
                {
                    var seen = new HashSet<Vector2Int>(); var q = new Queue<Vector2Int>();
                    if (walk(centre.x, centre.y) && !extraSolid.Contains(centre)) { seen.Add(centre); q.Enqueue(centre); }
                    while (q.Count > 0)
                    {
                        var t = q.Dequeue();
                        foreach (var d in new[] { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down })
                        {
                            var n = t + d;
                            if (seen.Contains(n) || extraSolid.Contains(n) || !Contains(limY, n.x, n.y) || !walk(n.x, n.y)) continue;
                            seen.Add(n); q.Enqueue(n);
                        }
                    }
                    return seen;
                };

                // C16 gap open: the flood from the circle centre reaches a yard tile.
                {
                    var seen = flood(new HashSet<Vector2Int>());
                    var yardReached = seen.Where(t => Contains(yardRect, t.x, t.y)).OrderBy(t => t.y).ThenBy(t => t.x).ToList();
                    var gapWalk = string.Join(" ", gapTiles.OrderBy(t => t.y).Select(t => t.x + "," + t.y + (walk(t.x, t.y) ? " open" : " solid")));
                    check("C16 gap open", yardReached.Count > 0, "flood from circle centre " + centre.x + "," + centre.y + " (walkable " + walk(centre.x, centre.y) + ") reached " + seen.Count + " tiles, " + yardReached.Count + " in the yard rect " + RS(yardRect) + (yardReached.Count > 0 ? "; first " + yardReached[0].x + "," + yardReached[0].y : "") + "; gap tiles " + gapWalk);
                }

                // C17 gap only: with the four gap tiles temporarily solid the flood reaches no yard tile.
                {
                    var seen = flood(new HashSet<Vector2Int>(gapTiles));
                    var yardReached = seen.Where(t => Contains(yardRect, t.x, t.y)).OrderBy(t => t.y).ThenBy(t => t.x).ToList();
                    check("C17 gap only", yardReached.Count == 0, "flood from circle centre with gap tiles " + string.Join(" ", gapTiles.OrderBy(t => t.y).Select(t => t.x + "," + t.y)) + " solid reached " + seen.Count + " tiles, " + yardReached.Count + " in the yard rect" + (yardReached.Count > 0 ? "; first: " + string.Join(" ", yardReached.Take(20).Select(t => t.x + "," + t.y)) : ""));
                }

                // C18 gate solid: the four gate tiles are solid in the compiled grid.
                {
                    var states = gateTiles.OrderBy(t => t.y).Select(t => t.x + "," + t.y + (g.SolidAt(t.x, t.y) ? " solid" : " OPEN")).ToList();
                    var solid = gateTiles.Count(t => g.SolidAt(t.x, t.y));
                    check("C18 gate solid", gateTiles.Count == 4 && solid == gateTiles.Count, solid + " of " + gateTiles.Count + " gate tiles solid: " + string.Join(" ", states));
                }

                // C19 office: inside the yard rect, door tiles on its west wall, no overlap with node, substation or gate.
                {
                    var of = yard["office"]; var ok = (string)of["key"];
                    var it = bld.FirstOrDefault(b => b.key == ok);
                    if (it == null) check("C19 office", false, "no building with key " + ok + " in the scene");
                    else
                    {
                        var sb = (SceneBuilding)it.c; var bad = new List<string>();
                        if (!Inside(it.rect, yardRect)) bad.Add("rect " + RS(it.rect) + " not inside yard rect " + RS(yardRect));
                        var doorTiles = new List<Vector2Int>();
                        foreach (var d in sb.doors) foreach (var t in Tiles(new RectInt(it.rect.x + d.x, it.rect.y + d.y, d.width, d.height))) doorTiles.Add(t);
                        if (doorTiles.Count == 0) bad.Add("no door");
                        foreach (var t in doorTiles) if (t.x != it.rect.xMin) bad.Add("door tile " + t.x + "," + t.y + " not on the west wall x=" + it.rect.xMin);
                        var gateRect = R(yard["yardGate"]["rect"]);
                        // Nodes and the substation: the plan's yard.nodes keys/rects and yard.substation rect, plus any scene Substation site (the yard site itself contains the office by D53).
                        var nodeKeys = new HashSet<string>(yard["nodes"].Select(n => (string)n["key"]));
                        foreach (var n in yard["nodes"]) if (Overlaps(it.rect, R(n["rect"]))) bad.Add("overlaps node " + (string)n["name"] + " " + RS(R(n["rect"])));
                        if (Overlaps(it.rect, R(yard["substation"]["rect"]))) bad.Add("overlaps substation " + RS(R(yard["substation"]["rect"])));
                        foreach (var s in sts) if ((nodeKeys.Contains(s.key) || s.kind == "Substation") && Overlaps(it.rect, s.rect)) bad.Add("overlaps " + s.name + " " + RS(s.rect));
                        if (Overlaps(it.rect, gateRect)) bad.Add("overlaps gate " + RS(gateRect));
                        foreach (var p in prp) if (p.key == gateKey && Overlaps(it.rect, p.rect)) bad.Add("overlaps gate object " + p.name);
                        if (sb.enterable) bad.Add("enterable");
                        check("C19 office", bad.Count == 0, it.name + " " + RS(it.rect) + " inside yard rect " + RS(yardRect) + ", door tiles " + string.Join(" ", doorTiles.Select(t => t.x + "," + t.y)) + " on west wall x=" + it.rect.xMin + ", enterable=" + sb.enterable + ", roof " + sb.roofKey + (bad.Count == 0 ? ", no overlap with nodes, substation or gate" : "; " + string.Join("; ", bad)));
                    }
                }
            }

            // C20 first attack origin (2026-09-18): the director, run on this very geometry, must choose an entry tile at or
            // below the raid line, inside its distance band, with a staging tile, and never a tile inside the block.
            {
                var raid = sites.OfKind(SiteKind.RaidLine).FirstOrDefault();
                var core = sites.OfKind(SiteKind.Core).FirstOrDefault();
                if (raid == null || core == null) check("C20 first attack origin", false, (raid == null ? "no RaidLine site" : "") + (core == null ? " no Core site" : ""));
                else
                {
                    var map = ImportedGeometry.Build(g);
                    var ctx = new SimContext(ReferenceData.Create(), map, threat: new EnemyThreatLayer(), sites: sites, mapId: g.MapId);
                    var reach = RaidField.ReachOf(ctx);
                    var reachable = raid.Y - (core.Y + core.H - 1) <= reach;
                    var sim = Simulation.NewGame(ctx, 1);
                    var st = sim.State;
                    var w = ctx.Geometry.Width;
                    int bx, by, size;
                    var has = DirectorRules.Target(ctx, st, out bx, out by, out size);
                    var line = DirectorRules.RaidLineY(ctx);
                    var origin = has ? DirectorRules.Origin(ctx, st) : -1;
                    var staged = DirectorRules.Staging(ctx, st, origin);
                    var detail = "raid line row " + raid.Y + " is " + (raid.Y - (core.Y + core.H - 1)) + " rows below the core's last row (field reach " + reach + ")";
                    if (origin < 0) check("C20 first attack origin", false, detail + "; the director found no entry tile (origin -1)");
                    else
                    {
                        var ox = origin % w; var oy = origin / w;
                        var fld = st.Director.Fields.Field(ctx, st, bx, by, size, true);
                        var steps = fld.At(ox, oy);
                        var entry = DirectorRules.EntryTile(ctx, st, fld, line, ox, oy, ctx.Data.Siege.EntryNearSteps, ctx.Data.Siege.EntryFarSteps);
                        var heading = DirectorRules.HeadingWord(DirectorRules.Heading(ox + .5 - (bx + size / 2.0), oy + .5 - (by + size / 2.0)));
                        var bad = new List<string>();
                        if (!reachable) bad.Add("raid line beyond the field's reach");
                        if (!entry) bad.Add("origin is not an entry tile");
                        if (oy < raid.Y) bad.Add("origin above the raid line");
                        if (Contains(block, ox, oy)) bad.Add("origin inside the block");
                        if (staged < 0) bad.Add("no staging tile");
                        check("C20 first attack origin", bad.Count == 0, detail + "; origin (" + ox + "," + oy + ") " + steps + " steps, heading " + heading + ", staging " + (staged < 0 ? "none" : "(" + staged % w + "," + staged / w + ")") + (bad.Count == 0 ? "" : "; " + string.Join("; ", bad)));
                    }
                }
            }

            // C21 substation lights (D55, 2026-09-19): every streetlight inside the block is owned by substation:0 under the
            // sim's nearest-substation rule, and a pole beside the core's east edge plus two more Poles on non-solid tiles
            // put the site on that pole's circuit in the real power grid.
            {
                var sub = sites.Find("substation:0");
                var core = sites.OfKind(SiteKind.Core).FirstOrDefault();
                if (sub == null || core == null) check("C21 substation lights", false, (sub == null ? "no substation:0 site" : "") + (core == null ? " no Core site" : ""));
                else
                {
                    var ctx = new SimContext(ReferenceData.Create(), ImportedGeometry.Build(g), sites: sites, mapId: g.MapId);
                    var court = StreetLights.Sites(ctx).Where(l => Contains(block, l.X, l.Y)).ToList();
                    var strays = court.Where(l => PowerGrid.SubstationOf(ctx, l) != sub).Select(l => l.Id + " -> " + (PowerGrid.SubstationOf(ctx, l)?.Id ?? "none")).ToList();
                    var poleReach = ctx.Data.TryMachine("pole", out var poleSpec) ? poleSpec.ReachTiles : 8;
                    var siteReach = PowerGrid.SiteReach(ctx.Data);
                    Func<int, int, int, int, bool> link = (ax, ay, bx2, by2) => PowerGrid.NodesLinked(ax, ay, 1, 1, poleReach, bx2, by2, 1, 1, poleReach);
                    Vector2Int? p0 = null, p1 = null, p2 = null;
                    var ex = core.X + core.W;
                    for (var y0 = core.Y; y0 < core.Y + core.H && p2 == null; y0++)
                    {
                        if (!walk(ex, y0)) continue;
                        for (var y1 = y0 - 8; y1 <= y0 + 8 && p2 == null; y1++)
                            for (var x1 = ex + 1; x1 <= ex + 8 && p2 == null; x1++)
                            {
                                if (!walk(x1, y1) || !link(ex, y0, x1, y1)) continue;
                                for (var y2 = y1 - 8; y2 <= y1 + 8 && p2 == null; y2++)
                                    for (var x2 = x1 + 1; x2 <= x1 + 8 && p2 == null; x2++)
                                        if (walk(x2, y2) && link(x1, y1, x2, y2) &&
                                            PowerGrid.NodesLinked(x2, y2, 1, 1, poleReach, sub.X, sub.Y, sub.W, sub.H, siteReach))
                                        { p0 = new Vector2Int(ex, y0); p1 = new Vector2Int(x1, y1); p2 = new Vector2Int(x2, y2); }
                            }
                    }
                    var chain = "no two-Pole chain from x=" + ex + " found";
                    var onCircuit = false;
                    if (p2 != null)
                    {
                        var st = Simulation.NewGame(ctx, 1).State;
                        var ids = new List<int>();
                        foreach (var p in new[] { p0.Value, p1.Value, p2.Value })
                        {
                            var m = new Machine { Id = st.NextId++, Kind = "pole", X = p.x, Y = p.y, Dir = Dir.N, Size = 1 };
                            st.Machines.Add(m); st.Rev++; ids.Add(m.Id);
                        }
                        var grid = PowerGrid.Of(ctx, st);
                        onCircuit = grid.OfSite(sub.Id) != null && ReferenceEquals(grid.OfSite(sub.Id), grid.Of(ids[0]));
                        chain = "network pole (" + p0.Value.x + "," + p0.Value.y + ") + Poles (" + p1.Value.x + "," + p1.Value.y + ") (" + p2.Value.x + "," + p2.Value.y + ") " + (onCircuit ? "put the site on its circuit" : "did NOT put the site on its circuit in the grid");
                    }
                    check("C21 substation lights", court.Count > 0 && strays.Count == 0 && onCircuit,
                        court.Count + " streetlights in the block (" + string.Join(" ", court.Select(l => l.Id + "@" + l.X + "," + l.Y)) + ") " + (strays.Count == 0 ? "all owned by " + sub.Id : "not owned by " + sub.Id + ": " + string.Join("; ", strays)) + "; " + chain);
                }
            }
        }
        catch (Exception e) { failed++; L("FAIL Verify: exception " + e.Message + "\n" + e.StackTrace); }
        finally { if (g != null) UnityEngine.Object.DestroyImmediate(g); }
        L("Verify: " + passed + " passed, " + failed + " failed of twenty-one checks");
        Flush("fc_verify_log.txt");
    }

    /// <summary>
    /// D46: clears baseline decor and baseline solids inside the block. Tile classes other than Ground/River become
    /// Ground, solids are cleared, props/squares/service areas overlapping the block are removed and paths/drives
    /// are cut so only their parts outside the block remain. Road-owned tiles are never touched: the plan road band,
    /// the circle (tile centre strictly inside the radius) and the block's four edge rows/columns (ring-road
    /// pavement). Roads, road nodes, tram samples, court and river are left alone. With <paramref name="dryRun"/>
    /// nothing is changed; the returned text starts with "clear" when there is nothing to clear, else "dirty".
    /// </summary>
    public static string ClearBaseline(WorldGeometryAsset g, RectInt block, RectInt band, Vector2Int centre, float radius, bool dryRun)
    {
        if (g == null) return "dirty: Editable World has no baseline asset";
        Func<int, int, bool> inBlock = (x, y) => Contains(block, x, y);
        Func<Vector2Int, bool> inV = v => inBlock(v.x, v.y);
        Func<RectInt, bool> ov = r => Overlaps(r, block);
        Func<int, int, bool> roadOwned = (x, y) => x == block.xMin || x == block.xMax - 1 || y == block.yMin || y == block.yMax - 1
            || Contains(band, x, y) || (new Vector2(x + .5f, y + .5f) - new Vector2(centre.x, centre.y)).sqrMagnitude < radius * radius;
        var tiles = 0; var solids = 0;
        for (var y = block.yMin; y < block.yMax; y++) for (var x = block.xMin; x < block.xMax; x++)
        {
            var i = g.Index(x, y);
            if (g.Solid[i] != 0) { solids++; if (!dryRun) g.Solid[i] = 0; }
            if (roadOwned(x, y)) continue;
            var k = g.Kind[i];
            if (k == (byte)TileClass.Ground || k == (byte)TileClass.River) continue;
            tiles++; if (!dryRun) g.Kind[i] = (byte)TileClass.Ground;
        }
        var c = g.City;
        var props = 0; for (var i = c.props.Count - 1; i >= 0; i--) if (ov(c.props[i].rect)) { props++; if (!dryRun) c.props.RemoveAt(i); }
        var squares = 0; for (var i = c.squares.Count - 1; i >= 0; i--) if (ov(c.squares[i].rect)) { squares++; if (!dryRun) c.squares.RemoveAt(i); }
        var areas = 0; for (var i = c.serviceAreas.Count - 1; i >= 0; i--) if (ov(c.serviceAreas[i])) { areas++; if (!dryRun) c.serviceAreas.RemoveAt(i); }
        Func<List<WorldGeometryAsset.Poly>, int> clip = list =>
        {
            var touched = 0; var add = new List<WorldGeometryAsset.Poly>();
            for (var i = list.Count - 1; i >= 0; i--)
            {
                var pts = list[i].points; if (pts == null || pts.Count == 0) continue;
                var walkTiles = new List<Vector2Int> { pts[0] };
                for (var j = 1; j < pts.Count; j++)
                {
                    var a = pts[j - 1]; var b = pts[j]; var n = Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
                    for (var st = 1; st <= n; st++) walkTiles.Add(new Vector2Int(a.x + Math.Sign(b.x - a.x) * Mathf.Min(st, Mathf.Abs(b.x - a.x)), a.y + Math.Sign(b.y - a.y) * Mathf.Min(st, Mathf.Abs(b.y - a.y))));
                }
                if (!walkTiles.Any(inV)) continue;
                touched++;
                if (dryRun) continue;
                var runs = new List<List<Vector2Int>>(); List<Vector2Int> run = null;
                foreach (var t in walkTiles) { if (inV(t)) { run = null; continue; } if (run == null) { run = new List<Vector2Int>(); runs.Add(run); } run.Add(t); }
                list.RemoveAt(i);
                foreach (var r in runs)
                {
                    if (r.Count < 2) continue;
                    var poly = new WorldGeometryAsset.Poly(); poly.points.Add(r[0]);
                    for (var j = 1; j < r.Count - 1; j++) if (r[j] - r[j - 1] != r[j + 1] - r[j]) poly.points.Add(r[j]);
                    poly.points.Add(r[r.Count - 1]); add.Add(poly);
                }
            }
            list.AddRange(add);
            return touched;
        };
        var paths = clip(c.paths); var drives = clip(c.drives);
        var total = tiles + solids + props + squares + areas + paths + drives;
        var detail = "block " + RS(block) + ": decor tiles " + tiles + ", solids " + solids + ", props " + props + ", paths " + paths + ", drives " + drives + ", squares " + squares + ", service areas " + areas + " (roads, nodes, tram, court untouched)";
        if (!dryRun && total > 0) { EditorUtility.SetDirty(g); AssetDatabase.SaveAssets(); }
        return (total == 0 ? "clear, nothing to remove; " : dryRun ? "dirty, still present; " : "cleared and saved; ") + detail;
    }

    /// <summary>Tiles between the house's front wall and the road edge (D39).</summary>
    static int Setback(string side, int roadEdge, RectInt r)
    {
        switch (side)
        {
            case "W": return roadEdge - r.xMax;
            case "E": return r.xMin - roadEdge - 1;
            case "N": return roadEdge - r.yMax;
            default: return r.yMin - roadEdge - 1;
        }
    }

    /// <summary>Belt-tile gap between two rects: the larger of the x and y gaps, 0 when they touch or overlap on both axes.</summary>
    static int Gap(RectInt a, RectInt b)
    {
        var gx = Mathf.Max(0, Mathf.Max(a.xMin - b.xMax, b.xMin - a.xMax));
        var gy = Mathf.Max(0, Mathf.Max(a.yMin - b.yMax, b.yMin - a.yMax));
        return Mathf.Max(gx, gy);
    }
}
