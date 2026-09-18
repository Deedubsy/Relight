using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Relight.Presentation;
using Relight.Sim;
using Relight.World;

/// <summary>
/// Scene-view gizmo for the raid director (2026-09-18): where the first attack and later raids may enter, drawn from
/// the director's own rules so what the Scene view shows is what the sim will do.
///
/// Toggle it with Relight/Gizmos/Raid Director (Gizmos must be on in the Scene view). In Edit Mode it compiles the
/// scene the way a new game would and asks <see cref="DirectorRules"/> for the answer; recompute after moving
/// fences or the raid line with Relight/Gizmos/Raid Director: Recompute. In Play Mode it follows the live
/// <see cref="SimHost"/> state, refreshing twice a second, so a chosen approach can be watched as defences go up.
///
/// Drawn, in tile coordinates (x east, y south, Unity pos = (x, -y)):
///   green dashed box  the <see cref="RaidField"/> box, the only area where a tile has a distance to the core;
///   blue rectangle    the Home core rectangle the field is seeded from;
///   red line          the <see cref="SiteKind.RaidLine"/> row, entries must be at or below it;
///   tinted tiles      every legal entry tile: strong for tier 0 (the reference's 20–60 step window), lighter for
///                     tiers 1 and 2 (the map has nothing that good);
///   grey circle       the 28-tile ring around the engineer where no wave is born;
///   orange discs      the four sector approaches a major roster rotates through, each joined to its staging tile;
///   large disc        the single-wave origin: green when it is a legal entry tile, RED when the director has no
///                     legal tile at all (then the label says so, and nothing should be born).
/// </summary>
public static class RaidDirectorGizmo
{
    const string PrefKey = "Relight.RaidDirectorGizmo.Enabled";
    const string MenuToggle = "Relight/Gizmos/Raid Director";
    const string MenuRecompute = "Relight/Gizmos/Raid Director: Recompute";
    const float Z = -8.5f;
    const double LiveRefreshSeconds = 0.5;

    static bool Enabled
    {
        get => EditorPrefs.GetBool(PrefKey, false);
        set => EditorPrefs.SetBool(PrefKey, value);
    }

    /// <summary>One computed answer from the director, in tiles, plus the meshes that draw its entry tiles.</summary>
    sealed class Snapshot
    {
        public int W, H;
        public bool HasTarget;
        public int SeedX, SeedY, SeedSize;
        public RectInt Core;
        public int Line;
        public RectInt Box;
        public int Origin, OriginSteps;
        public bool OriginLegal;
        public string OriginHeading;
        public int[] Approaches = Array.Empty<int>();
        public int[] Staging = Array.Empty<int>();
        public Vector2 Engineer;
        public readonly Mesh[] Tier = new Mesh[3];
        public readonly int[] TierCount = new int[3];
        public bool Live;
        public long Tick;
        public double BuiltAt;
        public string Summary;
    }

    static Snapshot _snap;
    static string _error;

    // ------------------------------------------------------------------ menu

    [MenuItem(MenuToggle)]
    static void Toggle()
    {
        Enabled = !Enabled;
        if (Enabled) Recompute();
        SceneView.RepaintAll();
    }

    [MenuItem(MenuToggle, true)]
    static bool ToggleValidate()
    {
        Menu.SetChecked(MenuToggle, Enabled);
        return true;
    }

    /// <summary>Recomputes the snapshot from the live game in Play Mode, otherwise from a compile of the scene.</summary>
    [MenuItem(MenuRecompute)]
    public static void Recompute()
    {
        _error = null;
        try
        {
            if (Application.isPlaying)
            {
                var host = UnityEngine.Object.FindAnyObjectByType<SimHost>();
                if (host == null || host.Simulation == null) { _error = "Play Mode, but no SimHost with a running simulation"; _snap = null; }
                else _snap = Compute(host.Simulation.Context, host.Simulation.State, true);
            }
            else
            {
                var world = UnityEngine.Object.FindAnyObjectByType<SceneWorld>();
                if (world == null) { _error = "no SceneWorld in the open scene"; _snap = null; return; }
                WorldSites sites;
                var g = world.Compile(out sites);
                try
                {
                    var map = ImportedGeometry.Build(g);
                    var ctx = new SimContext(ReferenceData.Create(), map, threat: new EnemyThreatLayer(), sites: sites, mapId: g.MapId);
                    var sim = Simulation.NewGame(ctx, 1);
                    _snap = Compute(ctx, sim.State, false);
                }
                finally { UnityEngine.Object.DestroyImmediate(g); }
            }
        }
        catch (Exception e)
        {
            _error = e.GetType().Name + ": " + e.Message;
            _snap = null;
            Debug.LogException(e);
        }
        if (_snap != null) Debug.Log("Raid Director gizmo: " + _snap.Summary);
        SceneView.RepaintAll();
    }

    // ------------------------------------------------------------------ compute

    static Snapshot Compute(SimContext ctx, SimState st, bool live)
    {
        var s = new Snapshot { W = ctx.Geometry.Width, H = ctx.Geometry.Height, Live = live, Tick = st.Tick, BuiltAt = EditorApplication.timeSinceStartup };
        s.Engineer = new Vector2((float)st.Engineer.Pos.X, (float)st.Engineer.Pos.Y);
        s.Line = DirectorRules.RaidLineY(ctx);
        var cr = HomeQueries.CoreRect(st);
        s.Core = new RectInt(cr.X, cr.Y, cr.W, cr.H);
        s.HasTarget = DirectorRules.Target(ctx, st, out s.SeedX, out s.SeedY, out s.SeedSize);
        if (!s.HasTarget)
        {
            s.Origin = DirectorRules.Origin(ctx, st);
            s.Summary = "no raid target (Home core not placed); fallback origin " + Tile(s, s.Origin);
            return s;
        }
        var fld = st.Director.Fields.Field(ctx, st, s.SeedX, s.SeedY, s.SeedSize, true);
        s.Box = new RectInt(fld.X0, fld.Y0, fld.BW, fld.BH);

        // Entry tiles by tier, as the director's OriginTier ranks them (20–60 / 12–far / near–far).
        var quads = new List<Vector3>[3];
        for (var i = 0; i < 3; i++) quads[i] = new List<Vector3>();
        for (var y = fld.Y0; y < fld.Y0 + fld.BH; y++)
            for (var x = fld.X0; x < fld.X0 + fld.BW; x++)
            {
                int tier;
                if (DirectorRules.EntryTile(ctx, st, fld, s.Line, x, y, 20, 60)) tier = 0;
                else if (DirectorRules.EntryTile(ctx, st, fld, s.Line, x, y, 12, DirectorRules.EntryFarSteps)) tier = 1;
                else if (DirectorRules.EntryTile(ctx, st, fld, s.Line, x, y, DirectorRules.EntryNearSteps, DirectorRules.EntryFarSteps)) tier = 2;
                else continue;
                s.TierCount[tier]++;
                var q = quads[tier];
                q.Add(new Vector3(x, -y, Z)); q.Add(new Vector3(x + 1, -y, Z)); q.Add(new Vector3(x + 1, -y - 1, Z)); q.Add(new Vector3(x, -y - 1, Z));
            }
        for (var i = 0; i < 3; i++) s.Tier[i] = QuadMesh(quads[i]);

        s.Origin = DirectorRules.Origin(ctx, st);
        if (s.Origin >= 0)
        {
            var ox = s.Origin % s.W; var oy = s.Origin / s.W;
            s.OriginSteps = fld.At(ox, oy);
            s.OriginLegal = DirectorRules.EntryTile(ctx, st, fld, s.Line, ox, oy, DirectorRules.EntryNearSteps, DirectorRules.EntryFarSteps);
            s.OriginHeading = DirectorRules.HeadingWord(DirectorRules.Heading(ox + .5 - (s.SeedX + s.SeedSize / 2.0), oy + .5 - (s.SeedY + s.SeedSize / 2.0)));
        }
        s.Approaches = DirectorRules.Approaches(ctx, st);
        s.Staging = new int[s.Approaches.Length];
        for (var i = 0; i < s.Approaches.Length; i++) s.Staging[i] = DirectorRules.Staging(ctx, st, s.Approaches[i]);

        var sb = new StringBuilder();
        sb.Append(live ? "LIVE tick " + st.Tick : "Edit Mode, new game");
        sb.Append(" | core ").Append(s.Core.x).Append(',').Append(s.Core.y).Append(' ').Append(s.Core.width).Append('x').Append(s.Core.height);
        sb.Append(" | raid line ").Append(s.Line == int.MinValue ? "none" : "row " + s.Line);
        sb.Append(" | field box rows ").Append(s.Box.yMin).Append("..").Append(s.Box.yMax - 1).Append(" (reach ").Append(RaidField.Reach).Append(')');
        sb.Append(" | entry tiles ").Append(s.TierCount[0]).Append('/').Append(s.TierCount[1]).Append('/').Append(s.TierCount[2]).Append(" by tier");
        sb.Append(" | origin ").Append(s.Origin < 0 ? "NONE" : Tile(s, s.Origin) + " " + s.OriginSteps + " steps " + s.OriginHeading + (s.OriginLegal ? "" : " NOT LEGAL"));
        sb.Append(" | approaches ").Append(s.Approaches.Length);
        for (var i = 0; i < s.Approaches.Length; i++) sb.Append(' ').Append(Tile(s, s.Approaches[i])).Append("->").Append(s.Staging[i] < 0 ? "none" : Tile(s, s.Staging[i]));
        s.Summary = sb.ToString();
        return s;
    }

    static string Tile(Snapshot s, int t) => t < 0 ? "none" : "(" + t % s.W + "," + t / s.W + ")";

    static Mesh QuadMesh(List<Vector3> verts)
    {
        if (verts.Count == 0) return null;
        var m = new Mesh { hideFlags = HideFlags.HideAndDontSave, indexFormat = IndexFormat.UInt32 };
        // Both windings, so the fill shows whichever way the Scene camera faces the plane,
        // and explicit normals toward the 2D camera so the gizmo-lit shader does not shade it to black.
        var tris = new int[verts.Count / 4 * 12];
        for (int q = 0, i = 0; q < verts.Count; q += 4, i += 12)
        {
            tris[i] = q; tris[i + 1] = q + 1; tris[i + 2] = q + 2;
            tris[i + 3] = q; tris[i + 4] = q + 2; tris[i + 5] = q + 3;
            tris[i + 6] = q; tris[i + 7] = q + 2; tris[i + 8] = q + 1;
            tris[i + 9] = q; tris[i + 10] = q + 3; tris[i + 11] = q + 2;
        }
        var normals = new Vector3[verts.Count];
        for (var n = 0; n < normals.Length; n++) normals[n] = Vector3.back;
        m.SetVertices(verts);
        m.SetNormals(normals);
        m.SetTriangles(tris, 0);
        m.RecalculateBounds();
        return m;
    }

    // ------------------------------------------------------------------ draw

    [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected)]
    static void Draw(SceneWorld world, GizmoType type)
    {
        if (!Enabled) return;
        if (Application.isPlaying)
        {
            var host = UnityEngine.Object.FindAnyObjectByType<SimHost>();
            var sim = host == null ? null : host.Simulation;
            if (sim != null && (_snap == null || !_snap.Live || (_snap.Tick != sim.State.Tick && EditorApplication.timeSinceStartup - _snap.BuiltAt > LiveRefreshSeconds)))
            {
                try { _snap = Compute(sim.Context, sim.State, true); _error = null; }
                catch (Exception e) { _error = e.GetType().Name + ": " + e.Message; }
            }
        }
        else if (_snap != null && _snap.Live) _snap = null;   // a Play Mode answer says nothing about the edited scene

        var style = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = Color.white }, richText = true };
        if (_snap == null)
        {
            var at = world.transform.position;
            Handles.Label(new Vector3(at.x, at.y, Z), "Raid Director gizmo: " + (_error ?? "not computed — Relight/Gizmos/Raid Director: Recompute"), style);
            return;
        }
        var s = _snap;

        // Entry tiles.
        var tierColour = new[] { new Color(0.15f, 0.75f, 0.35f, 0.55f), new Color(0.55f, 0.80f, 0.25f, 0.35f), new Color(0.85f, 0.75f, 0.20f, 0.30f) };
        for (var i = 0; i < 3; i++)
            if (s.Tier[i] != null) { Gizmos.color = tierColour[i]; Gizmos.DrawMesh(s.Tier[i], Vector3.zero, Quaternion.identity, Vector3.one); }

        // Field box, core, raid line.
        if (s.HasTarget)
        {
            Gizmos.color = new Color(0.2f, 0.9f, 0.5f, 0.9f);
            Gizmos.DrawWireCube(Centre(s.Box), new Vector3(s.Box.width, s.Box.height, 0));
            Handles.Label(new Vector3(s.Box.xMin + 0.5f, -s.Box.yMin + 0.2f, Z), "raid field box  rows " + s.Box.yMin + ".." + (s.Box.yMax - 1) + "  reach " + RaidField.Reach, style);
            Handles.Label(new Vector3(s.Box.xMin + 0.5f, -(s.Box.yMax) - 0.4f, Z), "field ends: row " + (s.Box.yMax - 1) + " — below this every tile reads -1", style);

            Gizmos.color = new Color(0.35f, 0.65f, 1f, 1f);
            Gizmos.DrawWireCube(Centre(s.Core), new Vector3(s.Core.width, s.Core.height, 0));
            Handles.Label(new Vector3(s.Core.xMax + 0.5f, -s.Core.yMin, Z), "core " + s.Core.x + "," + s.Core.y + " " + s.Core.width + "x" + s.Core.height, style);

            if (s.Line != int.MinValue)
            {
                Gizmos.color = new Color(1f, 0.3f, 0.25f, 1f);
                var x0 = Mathf.Min(s.Box.xMin, s.Core.xMin) - 4;
                var x1 = Mathf.Max(s.Box.xMax, s.Core.xMax) + 4;
                var y = -s.Line;   // top edge of the raid line row: tiles ON this row are allowed
                Gizmos.DrawLine(new Vector3(x0, y, Z), new Vector3(x1, y, Z));
                Gizmos.DrawLine(new Vector3(x0, y - 0.15f, Z), new Vector3(x1, y - 0.15f, Z));
                Handles.Label(new Vector3(x0 + 0.5f, y - 0.3f, Z), "raid line row " + s.Line + " — entries at or below", style);
            }
        }

        // The engineer's safe ring.
        Handles.color = new Color(0.8f, 0.8f, 0.8f, 0.7f);
        Handles.DrawWireDisc(new Vector3(s.Engineer.x, -s.Engineer.y, Z), Vector3.forward, (float)DirectorRules.SafeFromEngineerTiles);

        // Approaches and their staging tiles.
        for (var i = 0; i < s.Approaches.Length; i++)
        {
            var a = TileCentre(s, s.Approaches[i]);
            Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.95f);
            Gizmos.DrawSphere(a, 1.2f);
            if (s.Staging[i] >= 0)
            {
                var g = TileCentre(s, s.Staging[i]);
                Gizmos.DrawLine(a, g);
                Gizmos.DrawWireSphere(g, 0.9f);
            }
            Handles.Label(a + new Vector3(1.5f, 0.6f, 0), "approach " + Tile(s, s.Approaches[i]) + (s.Staging[i] < 0 ? "  NO STAGING" : s.Staging[i] == s.Approaches[i] ? "" : "  stages " + Tile(s, s.Staging[i])), style);
        }

        // The single-wave origin.
        if (s.Origin >= 0)
        {
            var o = TileCentre(s, s.Origin);
            Gizmos.color = s.OriginLegal ? new Color(0.1f, 1f, 0.3f, 1f) : new Color(1f, 0.1f, 0.1f, 1f);
            Gizmos.DrawSphere(o, 1.8f);
            Gizmos.DrawWireSphere(o, 3f);
            Handles.Label(o + new Vector3(3.5f, 1.2f, 0), "ORIGIN " + Tile(s, s.Origin) + "  " + s.OriginSteps + " steps  " + s.OriginHeading + (s.OriginLegal ? "" : "  <color=#ff4040>NOT A LEGAL ENTRY TILE</color>"), style);
        }
        else if (s.HasTarget)
        {
            var at = Centre(s.Core) + new Vector3(0, -s.Core.height, 0);
            Handles.Label(at, "<color=#ff4040>ORIGIN: NONE — no legal entry tile; the director will not spawn</color>", style);
        }

        // Summary.
        var head = s.HasTarget ? new Vector3(s.Box.xMin + 0.5f, -s.Box.yMin + 1.6f, Z) : new Vector3(s.Engineer.x, -s.Engineer.y + 4, Z);
        Handles.Label(head, s.Summary, style);
    }

    static Vector3 Centre(RectInt r) => new Vector3(r.x + r.width * .5f, -r.y - r.height * .5f, Z);
    static Vector3 TileCentre(Snapshot s, int t) => new Vector3(t % s.W + .5f, -(t / s.W) - .5f, Z);
}
