using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Relight.Presentation;
using Relight.Sim;
using Relight.World;

/// <summary>
/// Scene-view gizmo for the nine districts (2026-09-21): where each one ends, what it is called and how big it is,
/// drawn from the same <see cref="DistrictMap"/> the in-game debug overlay uses, so both show one answer.
///
/// Toggle it with Relight/Gizmos/Districts (Gizmos must be on in the Scene view). In Edit Mode it compiles the scene
/// the way a new game would; recompute after moving a substation with Relight/Gizmos/Districts: Recompute. In Play
/// Mode it reads the live <see cref="SimHost"/>.
///
/// Drawn, in tile coordinates (x east, y south, Unity pos = (x, -y)):
///   coloured wash     each district in its own colour;
///   white lines       the borders: a tile belongs to its nearest substation (ALWAYS_DARK_SPEC.md §6);
///   small squares     the substations;
///   label             name, walkable and street tiles, screens, and the time to walk across;
///   white box         one game screen, for scale, under each label;
///   pink diamonds     the roamer preview (Relight/Gizmos/Districts: Roamer Preview). A PREVIEW of a proposal, for
///                     judging density by eye. It is not a game rule and nothing is spawned.
/// </summary>
public static class DistrictGizmo
{
    const string PrefKey = "Relight.DistrictGizmo.Enabled";
    const string PrefPreview = "Relight.DistrictGizmo.Preview";
    const string MenuToggle = "Relight/Gizmos/Districts";
    const string MenuRecompute = "Relight/Gizmos/Districts: Recompute";
    const string MenuPreview = "Relight/Gizmos/Districts: Roamer Preview (cycle)";
    const float Z = -8.4f;
    const float TintAlpha = 0.16f;

    static bool Enabled
    {
        get => EditorPrefs.GetBool(PrefKey, false);
        set => EditorPrefs.SetBool(PrefKey, value);
    }

    static DistrictMap.Preview Preview
    {
        get => (DistrictMap.Preview)Mathf.Clamp(EditorPrefs.GetInt(PrefPreview, 0), 0, 3);
        set => EditorPrefs.SetInt(PrefPreview, (int)value);
    }

    static DistrictMap _map;
    static Mesh _thin, _thick, _dots;
    static Texture2D _tint;
    static DistrictMap.Preview _dotsFor;
    static bool _dotsFar;
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

    [MenuItem(MenuPreview)]
    static void CyclePreview()
    {
        Preview = (DistrictMap.Preview)(((int)Preview + 1) % 4);
        Debug.Log("District gizmo roamer preview: " + DistrictMap.PreviewName(Preview) + " (markers only)");
        SceneView.RepaintAll();
    }

    /// <summary>Recomputes from the live game in Play Mode, otherwise from a compile of the scene.</summary>
    [MenuItem(MenuRecompute)]
    public static void Recompute()
    {
        _error = null;
        Release();
        try
        {
            if (Application.isPlaying)
            {
                var host = UnityEngine.Object.FindAnyObjectByType<SimHost>();
                if (host == null || host.Simulation == null) { _error = "Play Mode, but no SimHost with a running simulation"; return; }
                _map = DistrictMap.Build(host.Simulation.Context);
            }
            else
            {
                var world = UnityEngine.Object.FindAnyObjectByType<SceneWorld>();
                if (world == null) { _error = "no SceneWorld in the open scene"; return; }
                WorldSites sites;
                var g = world.Compile(out sites);
                try
                {
                    var map = ImportedGeometry.Build(g);
                    _map = DistrictMap.Build(new SimContext(ReferenceData.Create(), map, sites: sites, mapId: g.MapId));
                }
                finally { UnityEngine.Object.DestroyImmediate(g); }
            }
            if (_map.Districts.Count == 0) { _error = "the scene has no substation sites, so no districts"; return; }
            _thin = _map.BorderMesh(0.4f, Z, true);
            _thick = _map.BorderMesh(1.8f, Z, true);
            _tint = _map.TintTexture(TintAlpha);
        }
        catch (Exception e)
        {
            _error = e.GetType().Name + ": " + e.Message;
            _map = null;
        }
        SceneView.RepaintAll();
    }

    static void Release()
    {
        foreach (var o in new UnityEngine.Object[] { _thin, _thick, _dots, _tint })
            if (o != null) UnityEngine.Object.DestroyImmediate(o);
        _thin = _thick = _dots = null;
        _tint = null;
        _map = null;
    }

    /// <summary>The figures as text, for the console and for checks.</summary>
    public static string Summary()
    {
        if (_map == null) return _error ?? "not computed";
        var sb = new System.Text.StringBuilder();
        foreach (var d in _map.Districts)
            sb.AppendLine($"{d.Index} {d.Name}: substation ({d.Substation.X},{d.Substation.Y}) · {d.Tiles:N0} tiles · " +
                          $"{d.Walkable:N0} walkable · {d.Street:N0} street · {d.Screens:0} screens · {d.WalkSeconds:0} s across");
        sb.Append($"{_map.Edges.Count:N0} border edges");
        return sb.ToString();
    }

    // ------------------------------------------------------------------ drawing

    [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected)]
    static void Draw(SceneWorld world, GizmoType type)
    {
        if (!Enabled) return;
        if (_map == null && _error == null) Recompute();
        var style = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = Color.white }, richText = true };
        if (_map == null)
        {
            Handles.Label(new Vector3(0, 4, Z), "District gizmo: " + _error, style);
            return;
        }

        // The wash. DrawGUITexture puts the texture's top row at the rect's yMin, so a negative height hangs it
        // down from the map's north edge with north at the top.
        if (_tint != null) Gizmos.DrawGUITexture(new Rect(0f, 0f, _map.W, -_map.H), _tint);

        // Thick lines when the whole map is on screen, thin when close enough to see tiles.
        var cam = SceneView.currentDrawingSceneView != null ? SceneView.currentDrawingSceneView.camera : null;
        var far = cam != null && cam.orthographic && cam.orthographicSize > 90f;
        Gizmos.color = Color.white;
        Gizmos.DrawMesh(far ? _thick : _thin);

        var preview = Preview;
        if (preview != DistrictMap.Preview.Off)
        {
            if (_dots == null || _dotsFor != preview || _dotsFar != far) { BuildDots(preview, far ? 9f : 1.6f); _dotsFar = far; }
            Gizmos.color = new Color(1f, 0.25f, 0.85f, 1f);
            Gizmos.DrawMesh(_dots);
        }

        var total = 0;
        foreach (var d in _map.Districts)
        {
            Gizmos.color = d.Colour;
            var s = d.Substation;
            Gizmos.DrawCube(new Vector3(s.X + s.W * 0.5f, -(s.Y + s.H * 0.5f), Z), new Vector3(far ? 8f : s.W, far ? 8f : s.H, 0.01f));

            var at = new Vector3(d.LabelTile.x, -d.LabelTile.y, Z);
            Gizmos.color = new Color(1f, 1f, 1f, 0.9f);
            // The screen box sits above the label and to its left, so the text never covers it.
            Gizmos.DrawWireCube(at + new Vector3(-DistrictMap.ScreenTilesWide * 0.5f - 2f, DistrictMap.ScreenTilesHigh * 0.5f + 2f, 0f),
                new Vector3(DistrictMap.ScreenTilesWide, DistrictMap.ScreenTilesHigh, 0f));

            // Short lines: two labels are only 100 tiles apart, which is little room with the whole map on screen.
            var text = $"<b>{d.Name}</b>\n{d.Walkable:N0} walkable\n{d.Street:N0} street\n{d.Screens:0} screens\n{d.WalkSeconds:0} s across";
            if (preview != DistrictMap.Preview.Off)
            {
                var n = DistrictMap.PreviewCount(d, preview);
                total += n;
                text += $"\n<color=#ff66dd>{n} roamers\n1 per {d.Screens / n:0} screens</color>";
            }
            Handles.Label(at, text, style);
        }

        var head = "Districts: a tile belongs to its nearest substation. White box = one game screen.";
        if (preview != DistrictMap.Preview.Off)
            head += $"\nRoamer PREVIEW ({DistrictMap.PreviewName(preview)}): {total} on the map. Markers only, not a game rule.";
        Handles.Label(new Vector3(0, 26, Z), head, style);
    }

    static void BuildDots(DistrictMap.Preview preview, float size)
    {
        if (_dots != null) UnityEngine.Object.DestroyImmediate(_dots);
        var quads = new List<Vector3>();
        var h = size * 0.5f;
        foreach (var d in _map.Districts)
        foreach (var p in _map.PreviewTiles(d, preview))
        {
            quads.Add(new Vector3(p.x, -(p.y - h), Z));
            quads.Add(new Vector3(p.x + h, -p.y, Z));
            quads.Add(new Vector3(p.x, -(p.y + h), Z));
            quads.Add(new Vector3(p.x - h, -p.y, Z));
        }
        _dots = DistrictMap.QuadMesh(quads, true);
        _dotsFor = preview;
    }
}
