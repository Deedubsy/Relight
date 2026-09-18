using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using Relight.Sim;
using Relight.World;

/// <summary>
/// Founders Court gap check (FOUNDERS-COURT-DECISIONS D26). Compiles the "Editable World" root and reports, per block
/// edge, the perimeter coordinates not covered by any building or fence footprint inside that edge's band, plus the
/// solid-tile counts inside the block. Tile coordinates: x east, y south, pivot NW corner, Unity pos = (x, -y).
/// </summary>
public static class FoundersCourtChecks
{
    // Block rectangle, inclusive tiles (FOUNDERS-COURT-ASSESSMENT heading 2).
    const int BlockMinX = 23, BlockMaxX = 121, BlockMinY = 329, BlockMaxY = 427;
    // Perimeter bands, inclusive tiles (FOUNDERS-COURT-ASSESSMENT heading 5).
    const int WestBandMinX = 23, WestBandMaxX = 41;
    const int NorthBandMinY = 329, NorthBandMaxY = 360;
    const int EastBandMinX = 97, EastBandMaxX = 121;
    const int SouthBandMinY = 404, SouthBandMaxY = 427;
    // Mouth span on the south edge, inclusive x (FOUNDERS-COURT-ASSESSMENT heading 2).
    const int MouthMinX = 67, MouthMaxX = 76;
    const string WorldRootName = "Editable World";

    [MenuItem("Relight/Founders Court Gap Check")]
    public static void Run()
    {
        foreach (var line in Report().Split('\n')) Debug.Log(line);
    }

    /// <summary>The whole report as text, one item per line (same lines the menu item logs).</summary>
    public static string Report()
    {
        var root = GameObject.Find(WorldRootName);
        if (root == null) return "World root '" + WorldRootName + "' not found";
        var world = root.GetComponent<SceneWorld>();
        if (world == null) return "World root has no SceneWorld";
        WorldSites sites;
        var g = world.Compile(out sites);

        var buildings = new HashSet<Vector2Int>();
        foreach (var b in world.GetComponentsInChildren<SceneBuilding>())
            Add(buildings, SceneWorld.Rect(b.transform, b.size));
        var fences = new HashSet<Vector2Int>();
        foreach (var p in world.GetComponentsInChildren<SceneProp>())
            if (p.kind == "fence" && p.blocksMovement) Add(fences, SceneWorld.Rect(p.transform, p.size));
        var substations = new HashSet<Vector2Int>();
        foreach (var s in sites.OfKind(SiteKind.Substation))
            Add(substations, new RectInt(s.X, s.Y, s.W, s.H));
        var coverage = new HashSet<Vector2Int>(buildings);
        coverage.UnionWith(fences);

        var sb = new StringBuilder();
        var gaps = 0;
        gaps += Edge(sb, "West", BlockMinY, BlockMaxY, c => Covered(coverage, WestBandMinX, WestBandMaxX, c, c, false), null);
        gaps += Edge(sb, "North", BlockMinX, BlockMaxX, c => Covered(coverage, c, c, NorthBandMinY, NorthBandMaxY, false), null);
        gaps += Edge(sb, "East", BlockMinY, BlockMaxY, c => Covered(coverage, EastBandMinX, EastBandMaxX, c, c, false), null);
        gaps += Edge(sb, "South", BlockMinX, BlockMaxX, c => Covered(coverage, c, c, SouthBandMinY, SouthBandMaxY, false), c => c >= MouthMinX && c <= MouthMaxX);
        sb.Append("Mouth excluded at south edge x ").Append(MouthMinX).Append("..").Append(MouthMaxX).Append(", y ").Append(BlockMaxY).Append('\n');
        sb.Append("Total gaps: ").Append(gaps).Append('\n');

        var solid = 0;
        var baseline = new List<Vector2Int>();
        for (var y = BlockMinY; y <= BlockMaxY; y++)
            for (var x = BlockMinX; x <= BlockMaxX; x++)
            {
                if (!g.SolidAt(x, y)) continue;
                solid++;
                var t = new Vector2Int(x, y);
                if (!buildings.Contains(t) && !fences.Contains(t) && !substations.Contains(t)) baseline.Add(t);
            }
        sb.Append("Solid tiles in block: ").Append(solid).Append('\n');
        sb.Append("Baseline solids in block: ").Append(baseline.Count).Append('\n');
        sb.Append("Baseline solid tiles:");
        foreach (var t in baseline) sb.Append('\n').Append(t.x).Append(',').Append(t.y);
        Object.DestroyImmediate(g);
        return sb.ToString();
    }

    static void Add(HashSet<Vector2Int> set, RectInt r)
    {
        for (var y = r.yMin; y < r.yMax; y++)
            for (var x = r.xMin; x < r.xMax; x++) set.Add(new Vector2Int(x, y));
    }

    static bool Covered(HashSet<Vector2Int> set, int x0, int x1, int y0, int y1, bool _)
    {
        for (var y = y0; y <= y1; y++)
            for (var x = x0; x <= x1; x++) if (set.Contains(new Vector2Int(x, y))) return true;
        return false;
    }

    /// <summary>Walks one edge, logs "GAP edge start end length" per run of uncovered coordinates, returns the gap count.</summary>
    static int Edge(StringBuilder sb, string edge, int from, int to, System.Func<int, bool> covered, System.Func<int, bool> excluded)
    {
        var gaps = 0; var start = -1;
        for (var c = from; c <= to + 1; c++)
        {
            var open = c <= to && !covered(c) && (excluded == null || !excluded(c));
            if (open && start < 0) start = c;
            if (!open && start >= 0)
            {
                sb.Append("GAP ").Append(edge).Append(' ').Append(start).Append(' ').Append(c - 1).Append(' ').Append(c - start).Append('\n');
                gaps++; start = -1;
            }
        }
        return gaps;
    }
}
