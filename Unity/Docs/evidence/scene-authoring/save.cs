var w=UnityEngine.Object.FindFirstObjectByType<Relight.World.SceneWorld>();
if(w==null)throw new System.Exception("No editable world");
var compiled=w.Compile(out _);UnityEngine.Object.DestroyImmediate(compiled);
Relight.Editor.SceneWorldEditor.ClearPreview();
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(w.gameObject.scene);
if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(w.gameObject.scene))throw new System.Exception("Scene save failed");
Relight.Editor.SceneWorldEditor.RefreshNow();Relight.Editor.SceneWorldEditor.FocusHome();
return "Saved "+w.gameObject.scene.path+" with "+w.GetComponentsInChildren<Relight.World.SceneBuilding>().Length+" buildings; preview="+Relight.Editor.SceneWorldEditor.LastError;
