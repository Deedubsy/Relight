using System.Collections.Generic;
using System.IO;
using Relight.Data;
using Relight.Presentation;
using Relight.Sim;
using Relight.UI;
using Relight.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;
using Tile = UnityEngine.Tilemaps.Tile;

namespace Relight.Editor
{
    /// <summary>
    /// B-13. The EDITOR tool that authors the minimal visible scene. It writes assets and scene files; it is not a
    /// runtime constructor (TECHNICAL_ARCHITECTURE.md §7.1). Once it has run, the scene asset holds everything and a
    /// human opens, inspects, moves and edits it like any other scene; re-running it rebuilds the generated parts.
    ///
    /// Everything it generates is a labelled placeholder: flat colour squares for the engineer, the machine and the
    /// seven terrain classes. Phase C replaces the lot (C-01 real geometry, C-06 real art).
    ///
    /// Menus:
    ///   Relight/Setup/Build World Scene   — assets, prefabs, both scenes, all wiring (calls the painter).
    ///   Relight/Setup/Paint Synthetic Map — repaint the tilemap only, into the open or saved World scene.
    /// </summary>
    public static class SceneSetup
    {
        private const string Root = "Assets/Relight";
        private const string Placeholders = Root + "/Presentation/Placeholders";
        private const string TilesFolder = Root + "/World/Tiles";
        private const string PrefabFolder = Root + "/Prefabs";
        private const string MachinePrefabFolder = PrefabFolder + "/Machines";
        private const string UiFolder = Root + "/UI";
        private const string WorldScene = Root + "/Scenes/World.unity";
        private const string GameUiScene = Root + "/Scenes/GameUI.unity";
        private const string ControlsAsset = Root + "/Input/RelightControls.inputactions";
        private const string RegistryAsset = Root + "/Data/GameDataRegistry.asset";
        private const string StatusPanelUxml = UiFolder + "/Panels/StatusPanel.uxml";
        private const string ChestMachineAsset = Root + "/Data/Generated/Machines/Machine - Supply chest.asset";

        // Placeholder terrain colours, chosen to read at a glance, not to be art (reference worldScene.ts:310-345).
        private static readonly (TileClass k, string name, Color c)[] TileColours =
        {
            (TileClass.Street,  "street",  new Color(0.29f, 0.31f, 0.31f)),
            (TileClass.Ground,  "ground",  new Color(0.23f, 0.27f, 0.21f)),
            (TileClass.Rubble,  "rubble",  new Color(0.36f, 0.32f, 0.27f)),
            (TileClass.Inert,   "inert",   new Color(0.20f, 0.20f, 0.22f)),
            (TileClass.River,   "river",   new Color(0.16f, 0.30f, 0.40f)),
            (TileClass.Deposit, "deposit", new Color(0.42f, 0.34f, 0.18f)),
            (TileClass.Patch,   "patch",   new Color(0.26f, 0.40f, 0.26f)),
        };

        private static readonly Color SolidColour = new Color(0.10f, 0.12f, 0.13f);
        private static readonly Color EngineerColour = new Color(0.57f, 0.85f, 0.71f);   // --ui-working
        private static readonly Color MachineColour = new Color(0.89f, 0.73f, 0.44f);    // --ui-accent

        [MenuItem("Relight/Setup/Build World Scene")]
        public static void BuildWorldScene()
        {
            EnsureFolders();
            var palette = BuildTilePalette();
            var engineerSprite = Placeholder("engineer", EngineerColour, true);
            var machineSprite = Placeholder("machine", MachineColour, true);
            var engineerPrefab = BuildEngineerPrefab(engineerSprite);
            var chestPrefab = BuildMachinePrefab(machineSprite);
            var registry = BuildPrefabRegistry(chestPrefab);
            BuildWorld(palette, engineerPrefab, registry);
            BuildGameUi();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("SceneSetup: World.unity and GameUI.unity rebuilt.");
        }

        [MenuItem("Relight/Setup/Paint Synthetic Map")]
        public static void PaintSyntheticMapMenu()
        {
            var palette = AssetDatabase.LoadAssetAtPath<TilePalette>(TilesFolder + "/TilePalette.asset");
            if (palette == null) { Debug.LogError("SceneSetup: no TilePalette; run Build World Scene first."); return; }
            var scene = EditorSceneManager.OpenScene(WorldScene, OpenSceneMode.Single);
            var generated = Find(scene.GetRootGameObjects(), "World", "Generated");
            if (generated == null) { Debug.LogError("SceneSetup: World/Generated not found."); return; }
            PaintSyntheticMap(generated.transform, palette);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, WorldScene);
            Debug.Log("SceneSetup: synthetic map repainted.");
        }

        // ----------------------------------------------------------------- assets

        private static void EnsureFolders()
        {
            foreach (var f in new[] { Placeholders, TilesFolder, PrefabFolder, MachinePrefabFolder,
                                      UiFolder + "/Panels", UiFolder + "/Styles", Root + "/Input" })
                Directory.CreateDirectory(f);
            AssetDatabase.Refresh();
        }

        /// <summary>A 1-tile flat colour PNG imported as a 32-pixels-per-unit point-filtered sprite.</summary>
        private static Sprite Placeholder(string name, Color colour, bool border)
        {
            var path = $"{Placeholders}/{name}.png";
            var size = WorldSpace.PixelsPerTile;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            var edge = (Color32)(colour * 0.55f);
            var fill = (Color32)colour;
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var onEdge = border && (x == 0 || y == 0 || x == size - 1 || y == size - 1);
                pixels[y * size + x] = onEdge ? edge : fill;
            }
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
            AssetDatabase.SetLabels(AssetDatabase.LoadAssetAtPath<Object>(path), new[] { "Relight", "Placeholder" });
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static TilePalette BuildTilePalette()
        {
            var tiles = new Dictionary<string, TileBase>();
            foreach (var (_, name, colour) in TileColours) tiles[name] = MakeTile(name, colour);
            tiles["solid"] = MakeTile("solid", SolidColour);

            var path = TilesFolder + "/TilePalette.asset";
            var palette = AssetDatabase.LoadAssetAtPath<TilePalette>(path);
            if (palette == null)
            {
                palette = ScriptableObject.CreateInstance<TilePalette>();
                AssetDatabase.CreateAsset(palette, path);
            }
            palette.SetTiles(tiles["street"], tiles["ground"], tiles["rubble"], tiles["inert"],
                tiles["river"], tiles["deposit"], tiles["patch"], tiles["solid"]);
            EditorUtility.SetDirty(palette);
            return palette;
        }

        private static TileBase MakeTile(string name, Color colour)
        {
            var sprite = PlaceholderTileSprite(name, colour);
            var path = $"{TilesFolder}/{name}.asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, path);
            }
            tile.sprite = sprite;
            tile.colliderType = Tile.ColliderType.None;
            EditorUtility.SetDirty(tile);
            return tile;
        }

        private static Sprite PlaceholderTileSprite(string name, Color colour)
        {
            var path = $"{TilesFolder}/{name}.png";
            var size = WorldSpace.PixelsPerTile;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            var fill = (Color32)colour;
            var grid = (Color32)(colour * 0.85f);
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
                pixels[y * size + x] = (x == 0 || y == 0) ? grid : fill;
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
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            AssetDatabase.SetLabels(AssetDatabase.LoadAssetAtPath<Object>(path), new[] { "Relight", "Placeholder" });
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static GameObject BuildEngineerPrefab(Sprite sprite)
        {
            var path = PrefabFolder + "/Engineer.prefab";
            var go = new GameObject("Engineer");
            var art = new GameObject("Sprite");
            art.transform.SetParent(go.transform, false);
            var sr = art.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 20;
            go.AddComponent<Relight.Presentation.EngineerView>();
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject BuildMachinePrefab(Sprite sprite)
        {
            var definition = AssetDatabase.LoadAssetAtPath<MachineDefinition>(ChestMachineAsset);
            if (definition == null) Debug.LogWarning($"SceneSetup: {ChestMachineAsset} not found; the prefab will have no data asset.");

            var path = MachinePrefabFolder + "/Chest.prefab";
            var go = new GameObject("Chest");
            var art = new GameObject("Sprite");
            art.transform.SetParent(go.transform, false);
            var sr = art.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 10;
            var view = go.AddComponent<MachineView>();
            view.SetDefinition(definition);
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static PrefabRegistry BuildPrefabRegistry(GameObject chest)
        {
            var path = PrefabFolder + "/PrefabRegistry.asset";
            var registry = AssetDatabase.LoadAssetAtPath<PrefabRegistry>(path);
            if (registry == null)
            {
                registry = ScriptableObject.CreateInstance<PrefabRegistry>();
                AssetDatabase.CreateAsset(registry, path);
            }
            var definition = AssetDatabase.LoadAssetAtPath<MachineDefinition>(ChestMachineAsset);
            registry.SetRows(new[] { new PrefabRegistry.Row { definition = definition, prefab = chest } }, chest);
            EditorUtility.SetDirty(registry);
            return registry;
        }

        /// <summary>
        /// The runtime panel settings. "Constant pixel size" so a token's 17px is 17 screen pixels; C-10's interface
        /// scale setting multiplies <c>scale</c> here, which is why nothing else may.
        /// </summary>
        private static PanelSettings BuildPanelSettings()
        {
            var themePath = UiFolder + "/RelightRuntimeTheme.tss";
            if (!File.Exists(themePath))
            {
                File.WriteAllText(themePath, "@import url(\"unity-theme://default\");\n");
                AssetDatabase.ImportAsset(themePath, ImportAssetOptions.ForceUpdate);
            }
            var theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(themePath);

            var path = UiFolder + "/PanelSettings.asset";
            var settings = AssetDatabase.LoadAssetAtPath<PanelSettings>(path);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(settings, path);
            }
            settings.themeStyleSheet = theme;
            settings.scaleMode = PanelScaleMode.ConstantPixelSize;
            settings.scale = 1f;
            settings.sortingOrder = 0;
            EditorUtility.SetDirty(settings);
            return settings;
        }

        // ----------------------------------------------------------------- scenes

        private static void BuildWorld(TilePalette palette, GameObject engineerPrefab, PrefabRegistry registry)
        {
            var scene = EditorSceneManager.OpenScene(WorldScene, OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects();

            var worldRoot = Find(roots, "World") ?? new GameObject("World");
            var generated = Find(roots, "World", "Generated");
            if (generated == null)
            {
                generated = new GameObject("Generated");
                generated.transform.SetParent(worldRoot.transform, false);
            }
            if (Find(roots, "World", "Manual") == null)
                new GameObject("Manual").transform.SetParent(worldRoot.transform, false);

            PaintSyntheticMap(generated.transform, palette);

            var sim = Find(roots, "Sim") ?? new GameObject("Sim");
            var host = Get<SimHost>(sim);
            var bootstrap = Get<WorldBootstrap>(sim);
            var autosave = Get<AutosaveController>(sim);
            var input = Get<WorldInput>(sim);
            var presenter = Get<MachinePresenter>(sim);

            var actors = Find(roots, "Actors") ?? new GameObject("Actors");
            var machines = Find(actors.transform, "Machines");
            if (machines == null)
            {
                machines = new GameObject("Machines");
                machines.transform.SetParent(actors.transform, false);
            }
            var engineer = Find(actors.transform, "Engineer");
            if (engineer == null)
            {
                engineer = (GameObject)PrefabUtility.InstantiatePrefab(engineerPrefab);
                engineer.name = "Engineer";
                engineer.transform.SetParent(actors.transform, false);
            }

            var camera = Find(roots, "Main Camera");
            if (camera == null)
            {
                camera = new GameObject("Main Camera", typeof(Camera));
                camera.tag = "MainCamera";
            }
            var rig = Get<CameraRig>(camera);
            var cam = camera.GetComponent<Camera>();
            cam.orthographic = true;
            cam.transform.position = new Vector3(6.5f, -6.5f, -10f);
            // Off-map space is the UI ground colour (--ui-bg, UI_AND_ONBOARDING.md 10), not the URP default blue.
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.082f, 0.114f, 0.118f, 1f);

            var controls = AssetDatabase.LoadAssetAtPath<Object>(ControlsAsset);
            var dataRegistry = AssetDatabase.LoadAssetAtPath<GameDataRegistry>(RegistryAsset);

            Set(bootstrap, ("host", host), ("registry", dataRegistry));
            Set(autosave, ("host", host));
            Set(input, ("host", host), ("actions", controls), ("worldCamera", cam));
            Set(presenter, ("host", host), ("prefabs", registry), ("container", machines.transform));
            Set(engineer.GetComponent<Relight.Presentation.EngineerView>(), ("host", host));
            Set(rig, ("host", host), ("target", engineer.transform));

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, WorldScene);
        }

        private static void BuildGameUi()
        {
            var scene = EditorSceneManager.OpenScene(GameUiScene, OpenSceneMode.Single);
            // Built after the scene is open: opening a scene in Single mode unloads assets nothing references yet,
            // which silently turns a PanelSettings loaded earlier into a destroyed (fake-null) reference.
            var panelSettings = BuildPanelSettings();
            var roots = scene.GetRootGameObjects();
            var ui = Find(roots, "GameUI") ?? new GameObject("GameUI");

            var document = Get<UIDocument>(ui);
            document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(StatusPanelUxml);
            // UIDocument.panelSettings is a property with attach/detach side effects that do not persist from a
            // batchmode edit; write the backing field so the reference is actually serialised into the scene.
            Set(document, ("m_PanelSettings", panelSettings));

            var router = Get<InputRouter>(ui);
            var escape = Get<EscapeChain>(ui);
            var shell = Get<UiShell>(ui);
            var status = Get<StatusPanelController>(ui);

            var controls = AssetDatabase.LoadAssetAtPath<Object>(ControlsAsset);
            Set(router, ("actions", controls));
            Set(shell, ("document", document), ("router", router), ("escape", escape));
            Set(status, ("document", document));

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, GameUiScene);
        }

        /// <summary>
        /// Paints <see cref="SyntheticMap"/> into a Grid + two Tilemaps under <paramref name="parent"/>: the terrain
        /// class of every tile, and the building-collision mask over it (WORLD_AND_ASSETS.md §2.6). The painted
        /// tiles live in the SCENE file, so the map is visible with the editor stopped.
        /// </summary>
        private static void PaintSyntheticMap(Transform parent, TilePalette palette)
        {
            var gridGo = Find(parent, "Grid");
            if (gridGo == null)
            {
                gridGo = new GameObject("Grid", typeof(Grid));
                gridGo.transform.SetParent(parent, false);
            }
            var terrain = EnsureTilemap(gridGo.transform, "Terrain", 0);
            var solid = EnsureTilemap(gridGo.transform, "Solid", 1);
            terrain.ClearAllTiles();
            solid.ClearAllTiles();

            var geometry = SyntheticMap.Create();
            var solidTile = palette.Solid;
            for (var y = 0; y < geometry.Height; y++)
            for (var x = 0; x < geometry.Width; x++)
            {
                var cell = WorldSpace.Cell(x, y);
                terrain.SetTile(cell, palette.For(geometry.TileAt(x, y)));
                if (geometry.Solid(x, y)) solid.SetTile(cell, solidTile);
            }
            terrain.CompressBounds();
            solid.CompressBounds();
        }

        private static Tilemap EnsureTilemap(Transform parent, string name, int order)
        {
            var go = Find(parent, name);
            if (go == null)
            {
                go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
                go.transform.SetParent(parent, false);
            }
            var renderer = go.GetComponent<TilemapRenderer>();
            renderer.sortingOrder = order;
            return go.GetComponent<Tilemap>();
        }

        // ----------------------------------------------------------------- helpers

        private static T Get<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }

        /// <summary>Assign serialised PRIVATE fields the way the Inspector would, so the scene file holds them.</summary>
        private static void Set(Object target, params (string field, Object value)[] pairs)
        {
            if (target == null) return;
            var so = new SerializedObject(target);
            foreach (var (field, value) in pairs)
            {
                var p = so.FindProperty(field);
                if (p == null) { Debug.LogWarning($"SceneSetup: {target.GetType().Name} has no field '{field}'."); continue; }
                p.objectReferenceValue = value;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static GameObject Find(GameObject[] roots, string name)
        {
            foreach (var r in roots) if (r.name == name) return r;
            return null;
        }

        private static GameObject Find(GameObject[] roots, string rootName, string childName)
        {
            var root = Find(roots, rootName);
            return root == null ? null : Find(root.transform, childName);
        }

        private static GameObject Find(Transform parent, string name)
        {
            for (var i = 0; i < parent.childCount; i++)
                if (parent.GetChild(i).name == name) return parent.GetChild(i).gameObject;
            return null;
        }
    }
}
