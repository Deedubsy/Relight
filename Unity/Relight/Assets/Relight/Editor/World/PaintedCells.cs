using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Relight.Editor
{
    /// <summary>
    /// Correction pass C6 / U-M-35. Empties every painted tilemap cell out of <c>World.unity</c> and saves it.
    ///
    /// The Phase C flow painted the imported region into the scene asset (<c>Relight/World/Paint Imported Region</c>,
    /// now deleted) and <c>Relight/Setup/Build World Scene</c> still paints the Phase B synthetic map the same way.
    /// Both bake tile cells into the scene file. At 864x576 that is half a million serialised cells in a file that is
    /// re-saved on every re-import, so the runtime <c>Relight.World.WorldPainter</c> took over the job: it fills the
    /// same two tilemaps from the geometry asset at session start, and the scene stores nothing.
    ///
    /// This menu is the one-off that gets an already-painted scene back to empty. Run it once after adding the
    /// <c>WorldPainter</c> component; after that nothing writes cells into the scene again.
    ///
    /// Nothing else in the scene is touched: only <see cref="Tilemap.ClearAllTiles"/> on the tilemaps it finds.
    /// </summary>
    public static class PaintedCells
    {
        private const string WorldScene = "Assets/Relight/Scenes/World.unity";

        [MenuItem("Relight/World/Clear painted cells", false, 120)]
        public static void ClearPaintedCellsMenu()
        {
            var report = Clear();
            Debug.Log("Relight: " + report);
            if (!Application.isBatchMode) EditorUtility.DisplayDialog("Relight: clear painted cells", report, "OK");
        }

        /// <summary>
        /// Open <c>World.unity</c>, empty every tilemap in it, save it, and describe what happened. Callable from an
        /// eval script (<c>Relight.Editor.PaintedCells.Clear()</c>) as well as from the menu.
        /// </summary>
        public static string Clear()
        {
            var scene = EditorSceneManager.OpenScene(WorldScene, OpenSceneMode.Single);
            if (!scene.IsValid()) return $"could not open {WorldScene}.";

            var maps = new List<Tilemap>();
            foreach (var root in scene.GetRootGameObjects())
                maps.AddRange(root.GetComponentsInChildren<Tilemap>(true));
            if (maps.Count == 0) return $"{WorldScene} has no Tilemap at all; nothing to clear.";

            var cleared = 0;
            var names = new List<string>();
            foreach (var map in maps)
            {
                var before = CountCells(map);
                if (before == 0) continue;
                Undo.RecordObject(map, "Clear painted cells");
                map.ClearAllTiles();
                map.CompressBounds();
                EditorUtility.SetDirty(map);
                cleared += before;
                names.Add($"{map.name} ({before})");
            }

            if (cleared == 0) return $"{WorldScene} already stores no painted cells ({maps.Count} tilemaps checked).";

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, WorldScene);
            return $"cleared {cleared} painted cells from {WorldScene}: {string.Join(", ", names)}. " +
                   "WorldPainter now fills these tilemaps at session start.";
        }

        /// <summary>How many cells the tilemap actually holds a tile in (its bounds may be larger than its content).</summary>
        private static int CountCells(Tilemap map)
        {
            var n = 0;
            var bounds = map.cellBounds;
            // GetTilesBlock allocates one array for the whole block; the scene's bounds are the painted region, so
            // this is a few hundred thousand references at worst and runs once, in the editor.
            if (bounds.size.x <= 0 || bounds.size.y <= 0 || bounds.size.z <= 0) return 0;
            var tiles = map.GetTilesBlock(bounds);
            foreach (var t in tiles) if (t != null) n++;
            return n;
        }
    }
}
