using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Relight.Editor
{
    /// <summary>
    /// One-off editor bootstrap for B-02: creates the scene shells of
    /// TECHNICAL_ARCHITECTURE.md §7.1 and registers them in Build Settings.
    /// Editor-only; it is not a runtime constructor. Existing scenes are left untouched.
    /// Menu: Relight/Setup/Create Scene Shells, or
    /// -executeMethod Relight.Editor.ProjectBootstrap.CreateScenes from the command line.
    /// </summary>
    public static class ProjectBootstrap
    {
        private const string ScenesFolder = "Assets/Relight/Scenes";
        private const string Urp2DTemplate = "Assets/Settings/Scenes/URP2DSceneTemplate.unity";

        [MenuItem("Relight/Setup/Create Scene Shells")]
        public static void CreateScenes()
        {
            Directory.CreateDirectory(ScenesFolder);
            AssetDatabase.Refresh();

            var boot = EnsureScene("Boot", useTemplate: false, s =>
            {
                var go = new GameObject("Boot");
                go.AddComponent<Relight.Presentation.BootController>();
            });
            var menu = EnsureScene("MainMenu", useTemplate: true, s =>
            {
                new GameObject("MainMenuUI");
            });
            var world = EnsureScene("World", useTemplate: true, s =>
            {
                var root = new GameObject("World");
                new GameObject("Generated").transform.SetParent(root.transform);
                new GameObject("Manual").transform.SetParent(root.transform);
                new GameObject("Sim");
            });
            var ui = EnsureScene("GameUI", useTemplate: false, s =>
            {
                new GameObject("GameUI");
            });

            var scenes = new List<EditorBuildSettingsScene>();
            foreach (var path in new[] { boot, menu, world, ui })
            {
                if (!string.IsNullOrEmpty(path)) scenes.Add(new EditorBuildSettingsScene(path, true));
            }
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log($"ProjectBootstrap: build settings now list {scenes.Count} scenes.");
        }

        private static string EnsureScene(string name, bool useTemplate, System.Action<Scene> populate)
        {
            var path = $"{ScenesFolder}/{name}.unity";
            if (File.Exists(path))
            {
                Debug.Log($"ProjectBootstrap: {path} exists, left untouched.");
                return path;
            }

            Scene scene;
            if (useTemplate && File.Exists(Urp2DTemplate))
            {
                if (!AssetDatabase.CopyAsset(Urp2DTemplate, path))
                {
                    Debug.LogError($"ProjectBootstrap: could not copy {Urp2DTemplate} to {path}.");
                    return null;
                }
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            }
            else
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }

            populate(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, path))
            {
                Debug.LogError($"ProjectBootstrap: could not save {path}.");
                return null;
            }
            Debug.Log($"ProjectBootstrap: created {path}.");
            return path;
        }
    }
}
