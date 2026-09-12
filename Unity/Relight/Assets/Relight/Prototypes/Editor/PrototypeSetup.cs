using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Relight.Sim;
using Relight.UI;
using Relight.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Relight.Prototypes.Editor
{
    /// <summary>
    /// B-14. The EDITOR tool that authors the three risk prototypes, following B-13's SceneSetup pattern: it writes
    /// assets and scene files, it is not a runtime constructor (TECHNICAL_ARCHITECTURE.md §7.1). Re-running a menu
    /// rebuilds that prototype's scene from scratch, so every measurement is taken over identical content.
    ///
    ///   Relight/Prototypes/Build R6 — 8 placeholder stack icons + R6-SlotGrid.unity (40 + 20 slots, one document).
    ///   Relight/Prototypes/Paint R7 — R7-Tilemap.unity, 864 × 576 = 497,664 tiles over 4 tilemap layers.
    ///   Relight/Prototypes/Build R2 — R2-Lighting.unity, 200 point lights + 50 shadow casters, no camera
    ///                                 (it is loaded ADDITIVELY over R7 so lighting is measured over the real map).
    ///
    /// Paint R7 also records what only the editor can see — the wall-clock paint time and the size of the saved
    /// scene file — into Unity/Docs/evidence/phase-b/b14-r7-paint.json.
    /// </summary>
    public static class PrototypeSetup
    {
        private const string Root = "Assets/Relight/Prototypes";
        private const string SceneFolder = Root + "/Scenes";
        private const string IconFolder = Root + "/R6-SlotGrid/Icons";
        private const string CasterFolder = Root + "/R2-Lighting/Placeholders";
        private const string R6Scene = SceneFolder + "/R6-SlotGrid.unity";
        private const string R7Scene = SceneFolder + "/R7-Tilemap.unity";
        private const string R2Scene = SceneFolder + "/R2-Lighting.unity";
        private const string SlotGridUxml = Root + "/R6-SlotGrid/SlotGrid.uxml";
        private const string PanelSettingsAsset = Root + "/R6-SlotGrid/PrototypePanelSettings.asset";
        private const string ThemeAsset = "Assets/Relight/UI/RelightRuntimeTheme.tss";
        private const string PaletteAsset = "Assets/Relight/World/Tiles/TilePalette.asset";

        /// <summary>riverfront-arc-v4-editor-ac16d9188c05 (WORLD_AND_ASSETS.md §2.3).</summary>
        public const int MapWidth = 864;
        public const int MapHeight = 576;

        private static readonly Color[] IconColours =
        {
            new Color(0.62f, 0.66f, 0.70f),  // steel
            new Color(0.78f, 0.47f, 0.29f),  // copper
            new Color(0.55f, 0.54f, 0.50f),  // stone
            new Color(0.20f, 0.20f, 0.22f),  // coal
            new Color(0.83f, 0.71f, 0.36f),  // magazine
            new Color(0.72f, 0.55f, 0.25f),  // wire
            new Color(0.45f, 0.55f, 0.62f),  // frame
            new Color(0.36f, 0.60f, 0.45f),  // board
        };

        [MenuItem("Relight/Prototypes/Build All")]
        public static void BuildAll()
        {
            BuildR6();
            PaintR7();
            BuildR2();
        }

        // ================================================================= R6

        [MenuItem("Relight/Prototypes/Build R6")]
        public static void BuildR6()
        {
            Directory.CreateDirectory(SceneFolder);
            Directory.CreateDirectory(IconFolder);
            AssetDatabase.Refresh();

            var icons = new Sprite[SlotGridPrototype.Items.Length];
            for (var i = 0; i < icons.Length; i++)
                icons[i] = PlaceholderSprite(IconFolder, SlotGridPrototype.Items[i], IconColours[i % IconColours.Length], 32);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var panelSettings = BuildPanelSettings();

            // A camera so a RenderTexture readback of the panel is possible headless (B-13: ScreenCapture is not).
            var cameraGo = new GameObject("Main Camera", typeof(Camera)) { tag = "MainCamera" };
            var cam = cameraGo.GetComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.082f, 0.114f, 0.118f, 1f);

            var ui = new GameObject("R6 SlotGrid");
            var document = ui.AddComponent<UIDocument>();
            document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(SlotGridUxml);
            // B-13 finding: UIDocument.panelSettings is a property whose side effects do not survive a batchmode
            // edit; the backing field must be written for the reference to reach the scene file.
            Set(document, ("m_PanelSettings", panelSettings));

            var escape = ui.AddComponent<EscapeChain>();
            var prototype = ui.AddComponent<SlotGridPrototype>();
            Set(prototype, ("document", document), ("escape", escape));
            SetValues(prototype, ("packSlots", 40), ("storeSlots", 20));
            SetArray(prototype, "icons", icons);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, R6Scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"B14 R6: {R6Scene} built — 40 + 20 slots, {icons.Length} placeholder icons.");
        }

        /// <summary>
        /// The prototype's own PanelSettings, so R6 never writes to the B-13 asset in Assets/Relight/UI.
        /// ConstantPixelSize / scale 1: a token's 17 px is 17 screen pixels (UI_AND_ONBOARDING.md).
        /// </summary>
        private static PanelSettings BuildPanelSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsAsset);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(settings, PanelSettingsAsset);
            }
            settings.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemeAsset);
            settings.scaleMode = PanelScaleMode.ConstantPixelSize;
            settings.scale = 1f;
            settings.sortingOrder = 0;
            EditorUtility.SetDirty(settings);
            return settings;
        }

        // ================================================================= R7

        [MenuItem("Relight/Prototypes/Paint R7")]
        public static void PaintR7()
        {
            Directory.CreateDirectory(SceneFolder);
            AssetDatabase.Refresh();
            var palette = AssetDatabase.LoadAssetAtPath<TilePalette>(PaletteAsset);
            if (palette == null)
            {
                Debug.LogError($"B14 R7: {PaletteAsset} is missing; run Relight/Setup/Build World Scene first.");
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraGo = new GameObject("Main Camera", typeof(Camera)) { tag = "MainCamera" };
            var cam = cameraGo.GetComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.082f, 0.114f, 0.118f, 1f);
            cam.transform.position = new Vector3(10f, -10f, -10f);
            var sweep = cameraGo.AddComponent<CameraSweep>();
            SetValues(sweep, ("mapWidth", MapWidth), ("mapHeight", MapHeight), ("seconds", 10f), ("autoRun", false));

            // One global light, the minimum for a lit 2D scene, so R7's baseline is an honestly rendered map and R2
            // measures the DIFFERENCE its 200 lights make rather than the difference between dark and lit.
            var globalGo = new GameObject("Global Light");
            var global = globalGo.AddComponent<Light2D>();
            global.lightType = Light2D.LightType.Global;
            global.intensity = 1f;

            var world = new GameObject("World");
            var gridGo = new GameObject("Grid", typeof(Grid));
            gridGo.transform.SetParent(world.transform, false);

            // WORLD_AND_ASSETS.md §7.1 names the sprite layers: Terrain, TerrainVariant, ResourcePatch, plus the
            // baked road surface. Four tilemaps — §2 does NOT name them, which the report records as a doc note.
            var terrain = EnsureTilemap(gridGo.transform, "Terrain", 0);
            var variant = EnsureTilemap(gridGo.transform, "TerrainVariant", 1);
            var resource = EnsureTilemap(gridGo.transform, "ResourcePatch", 2);
            var road = EnsureTilemap(gridGo.transform, "RoadSurface", 3);

            var terrainTiles = new TileBase[MapWidth * MapHeight];
            var variantTiles = new TileBase[MapWidth * MapHeight];
            var resourceTiles = new TileBase[MapWidth * MapHeight];
            var roadTiles = new TileBase[MapWidth * MapHeight];
            int variantCount = 0, resourceCount = 0, roadCount = 0;

            var variantTile = palette.For(TileClass.Ground);
            var roadTile = palette.For(TileClass.Street);

            var build = Stopwatch.StartNew();
            // The tilemap block is addressed in CELL space, which is the sim grid flipped once (WorldSpace.Cell);
            // row y of the sim is row (MapHeight - 1 - y) of the block, so the map is not painted upside down.
            for (var y = 0; y < MapHeight; y++)
            {
                var row = (MapHeight - 1 - y) * MapWidth;
                for (var x = 0; x < MapWidth; x++)
                {
                    var k = R7Map.ClassAt(x, y);
                    var i = row + x;
                    terrainTiles[i] = palette.For(k);
                    if (k == TileClass.Street) { roadTiles[i] = roadTile; roadCount++; }
                    else if (R7Map.HasVariant(x, y)) { variantTiles[i] = variantTile; variantCount++; }
                    if (k == TileClass.Deposit || k == TileClass.Patch) { resourceTiles[i] = palette.For(k); resourceCount++; }
                }
            }
            build.Stop();

            // SetTilesBlock — one call per layer. 497,664 individual SetTile calls is the thing NOT to do.
            var bounds = new BoundsInt(0, -MapHeight, 0, MapWidth, MapHeight, 1);
            var paint = Stopwatch.StartNew();
            terrain.SetTilesBlock(bounds, terrainTiles);
            var terrainMs = paint.Elapsed.TotalMilliseconds;
            variant.SetTilesBlock(bounds, variantTiles);
            resource.SetTilesBlock(bounds, resourceTiles);
            road.SetTilesBlock(bounds, roadTiles);
            paint.Stop();

            var save = Stopwatch.StartNew();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, R7Scene);
            save.Stop();

            var bytes = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), R7Scene)).Length;
            var values = new List<KeyValuePair<string, object>>
            {
                New("prototype", "R7 tilemap"),
                New("scene", R7Scene),
                New("map_width", MapWidth),
                New("map_height", MapHeight),
                New("tiles_total", MapWidth * MapHeight),
                New("tilemap_layers", 4),
                New("terrain_tiles", MapWidth * MapHeight),
                New("road_tiles", roadCount),
                New("variant_tiles", variantCount),
                New("resource_tiles", resourceCount),
                New("build_array_ms", EvidenceWriter.Round(build.Elapsed.TotalMilliseconds)),
                New("set_tiles_block_terrain_ms", EvidenceWriter.Round(terrainMs)),
                New("set_tiles_block_total_ms", EvidenceWriter.Round(paint.Elapsed.TotalMilliseconds)),
                New("save_scene_ms", EvidenceWriter.Round(save.Elapsed.TotalMilliseconds)),
                New("scene_bytes", bytes),
                New("scene_mib", EvidenceWriter.Round(bytes / 1048576.0)),
                New("serialisation", "ForceText (ProjectSettings m_SerializationMode: 2)"),
                New("unity", Application.unityVersion),
            };
            EvidenceWriter.Write("b14-r7-paint.json", values);
            Debug.Log($"B14 R7: painted {MapWidth * MapHeight} tiles over 4 layers in " +
                      $"{paint.Elapsed.TotalMilliseconds:F1} ms (array build {build.Elapsed.TotalMilliseconds:F1} ms); " +
                      $"scene {bytes} bytes ({bytes / 1048576.0:F2} MiB).");
        }

        private static Tilemap EnsureTilemap(Transform parent, string name, int order)
        {
            var go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
            go.transform.SetParent(parent, false);
            var renderer = go.GetComponent<TilemapRenderer>();
            renderer.sortingOrder = order;
            renderer.mode = TilemapRenderer.Mode.Chunk;
            return go.GetComponent<Tilemap>();
        }

        // ================================================================= R2

        [MenuItem("Relight/Prototypes/Build R2")]
        public static void BuildR2()
        {
            Directory.CreateDirectory(SceneFolder);
            Directory.CreateDirectory(CasterFolder);
            AssetDatabase.Refresh();
            var buildingSprite = PlaceholderSprite(CasterFolder, "building", new Color(0.18f, 0.19f, 0.21f), 32);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var rigGo = new GameObject("R2 Lighting");
            var rig = rigGo.AddComponent<LightingRig>();

            var globalGo = new GameObject("Global Light");
            globalGo.transform.SetParent(rigGo.transform, false);
            var global = globalGo.AddComponent<Light2D>();
            global.lightType = Light2D.LightType.Global;
            global.intensity = rig.NightIntensity;
            global.color = new Color(0.55f, 0.62f, 0.80f);

            var lightRoot = new GameObject("Lights");
            lightRoot.transform.SetParent(rigGo.transform, false);
            var casterRoot = new GameObject("Casters");
            casterRoot.transform.SetParent(rigGo.transform, false);

            var lights = rig.PlannedPointLights;
            for (var i = 0; i < lights; i++)
            {
                var radius = LightingRig.RadiusFor(i);
                var tile = LightingRig.ScatterTile(i, MapWidth, MapHeight);
                var go = new GameObject($"Light {i:D3} r{radius:F0}");
                go.transform.SetParent(lightRoot.transform, false);
                go.transform.position = WorldSpace.TileCentre(tile.x, tile.y);
                var l = go.AddComponent<Light2D>();
                l.lightType = Light2D.LightType.Point;
                l.color = new Color(1f, 0.91f, 0.76f);
                l.intensity = 1f;
                l.pointLightOuterRadius = radius;
                SetValues(l, ("m_PointLightInnerRadius", radius * 0.25f), ("m_FalloffIntensity", 0.5f));
                l.shadowsEnabled = false;   // LightingRig.SetShadowCasters turns these on for the second pass
            }

            var casters = rig.PlannedShadowCasters;
            for (var i = 0; i < casters; i++)
            {
                var tile = LightingRig.ScatterTile(i + 10007, MapWidth, MapHeight);
                var go = new GameObject($"Caster {i:D2}");
                go.transform.SetParent(casterRoot.transform, false);
                go.transform.position = WorldSpace.TileCentre(tile.x, tile.y);
                // A building-sized box: 6 × 4 tiles, the reference's ordinary downtown footprint.
                go.transform.localScale = new Vector3(6f, 4f, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = buildingSprite;
                sr.sortingOrder = 10;
                var caster = go.AddComponent<ShadowCaster2D>();
                ConfigureCaster(caster, 6f, 4f);
                caster.enabled = false;
            }

            Set(rig, ("globalLight", global), ("lightRoot", lightRoot.transform), ("casterRoot", casterRoot.transform));
            SetValues(rig, ("mapWidth", MapWidth), ("mapHeight", MapHeight));

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, R2Scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"B14 R2: {R2Scene} built — {lights} point lights, {casters} shadow casters, no camera " +
                      "(loaded additively over R7).");
        }

        /// <summary>
        /// Give the caster an explicit box shape instead of relying on ShadowCaster2D.Awake's renderer-bounds
        /// fallback, so the shadow geometry is the same whether or not the sprite happens to be loaded.
        /// m_ShapePath / m_ShapePathHash / m_ShadowCastingSource are serialised but not public.
        /// </summary>
        private static void ConfigureCaster(ShadowCaster2D caster, float width, float height)
        {
            var so = new SerializedObject(caster);
            var source = so.FindProperty("m_ShadowCastingSource");
            if (source != null) source.intValue = 1;            // ShadowCastingSources.ShapeEditor
            var path = so.FindProperty("m_ShapePath");
            if (path != null)
            {
                var hw = 0.5f;                                   // local space; the transform scales it to width × height
                var hh = 0.5f;
                var points = new[]
                {
                    new Vector3(-hw, -hh, 0f), new Vector3(hw, -hh, 0f),
                    new Vector3(hw, hh, 0f), new Vector3(-hw, hh, 0f),
                };
                path.arraySize = points.Length;
                for (var i = 0; i < points.Length; i++) path.GetArrayElementAtIndex(i).vector3Value = points[i];
            }
            var hash = so.FindProperty("m_ShapePathHash");
            if (hash != null) hash.intValue = Mathf.RoundToInt(width * 1000f) * 397 + Mathf.RoundToInt(height * 1000f);
            var casts = so.FindProperty("m_CastsShadows");
            if (casts != null) casts.boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(caster);
        }

        // ================================================================= helpers

        /// <summary>A flat colour PNG imported as a point-filtered 32-pixels-per-unit sprite (B-13 SceneSetup).</summary>
        private static Sprite PlaceholderSprite(string folder, string name, Color colour, int size)
        {
            var path = $"{folder}/{name}.png";
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            var edge = (Color32)(colour * 0.55f);
            var fill = (Color32)colour;
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
                pixels[y * size + x] = (x == 0 || y == 0 || x == size - 1 || y == size - 1) ? edge : fill;
            tex.SetPixels32(pixels);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = WorldSpace.PixelsPerTile;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            AssetDatabase.SetLabels(AssetDatabase.LoadAssetAtPath<Object>(path), new[] { "Relight", "Placeholder", "B14" });
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static KeyValuePair<string, object> New(string k, object v) => new KeyValuePair<string, object>(k, v);

        /// <summary>Assign serialised PRIVATE object references the way the Inspector would (B-13 SceneSetup.Set).</summary>
        private static void Set(Object target, params (string field, Object value)[] pairs)
        {
            if (target == null) return;
            var so = new SerializedObject(target);
            foreach (var (field, value) in pairs)
            {
                var p = so.FindProperty(field);
                if (p == null) { Debug.LogWarning($"PrototypeSetup: {target.GetType().Name} has no field '{field}'."); continue; }
                p.objectReferenceValue = value;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        /// <summary>The same, for serialised private value fields.</summary>
        private static void SetValues(Object target, params (string field, object value)[] pairs)
        {
            if (target == null) return;
            var so = new SerializedObject(target);
            foreach (var (field, value) in pairs)
            {
                var p = so.FindProperty(field);
                if (p == null) { Debug.LogWarning($"PrototypeSetup: {target.GetType().Name} has no field '{field}'."); continue; }
                switch (value)
                {
                    case int i: p.intValue = i; break;
                    case float f: p.floatValue = f; break;
                    case bool b: p.boolValue = b; break;
                    case string s: p.stringValue = s; break;
                    case Enum e: p.enumValueIndex = Convert.ToInt32(e); break;
                    default: Debug.LogWarning($"PrototypeSetup: unsupported value for '{field}'."); break;
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetArray(Object target, string field, Object[] values)
        {
            if (target == null) return;
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogWarning($"PrototypeSetup: {target.GetType().Name} has no field '{field}'."); return; }
            p.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }
    }

    /// <summary>
    /// The R7 prototype's terrain. NEW test content, not a port: the authored riverfront city is Phase C data that
    /// Phase B may not depend on (see SyntheticMap's note), so R7 needs its own map at the REAL scale — the only
    /// property being measured is 864 × 576 = 497,664 tiles over four layers, not what the tiles depict.
    ///
    /// Authored by fixed arithmetic, no RNG, so two runs paint byte-identical scenes.
    /// </summary>
    public static class R7Map
    {
        /// <summary>Road grid pitch and width, roughly the reference's downtown block size.</summary>
        public const int BlockX = 24;
        public const int BlockY = 18;
        public const int RoadWidth = 3;

        /// <summary>The river band and its one bridge column, so the map has a large non-street region too.</summary>
        public const int RiverTop = 286;
        public const int RiverHeight = 12;

        public static TileClass ClassAt(int x, int y)
        {
            var bridge = (x % (BlockX * 6)) < RoadWidth;
            if (y >= RiverTop && y < RiverTop + RiverHeight && !bridge) return TileClass.River;
            if (x % BlockX < RoadWidth || y % BlockY < RoadWidth) return TileClass.Street;
            var h = Hash(x, y) % 100u;
            if (h < 70u) return TileClass.Ground;
            if (h < 88u) return TileClass.Rubble;
            if (h < 94u) return TileClass.Inert;
            if (h < 98u) return TileClass.Deposit;
            return TileClass.Patch;
        }

        /// <summary>A quarter of the non-street tiles carry a second sprite — the TerrainVariant layer's load.</summary>
        public static bool HasVariant(int x, int y) => (Hash(x + 7919, y + 104729) & 3u) == 0u;

        private static uint Hash(int x, int y)
        {
            unchecked
            {
                var h = (uint)(x * 374761393 + y * 668265263);
                h = (h ^ (h >> 13)) * 1274126177u;
                return h ^ (h >> 16);
            }
        }
    }
}
