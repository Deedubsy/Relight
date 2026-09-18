var w=UnityEngine.Object.FindFirstObjectByType<Relight.World.SceneWorld>();var g=w.Compile(out _);var id=g.MapId;UnityEngine.Object.DestroyImmediate(g);
var scene=w.gameObject.scene;var path=scene.path;
Relight.Editor.SceneWorldEditor.ClearPreview();UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);
scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path,UnityEditor.SceneManagement.OpenSceneMode.Additive);UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
w=UnityEngine.Object.FindFirstObjectByType<Relight.World.SceneWorld>();g=w.Compile(out _);if(g.MapId!=id)throw new System.Exception("Layout changed after reopen");UnityEngine.Object.DestroyImmediate(g);
Relight.Editor.SceneWorldEditor.RefreshNow();Relight.Editor.SceneWorldEditor.FocusHome();
var msg="PASS scene save/reopen preserves layout identity, "+w.GetComponentsInChildren<Relight.World.SceneBuilding>().Length+" buildings; preview error="+Relight.Editor.SceneWorldEditor.LastError+"; dirty="+scene.isDirty;
System.IO.File.WriteAllText("E:/Factorio2/Unity/Docs/evidence/scene-authoring/reopen.txt",msg);return msg;
