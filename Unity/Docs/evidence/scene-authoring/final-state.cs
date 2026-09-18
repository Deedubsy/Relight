var w=UnityEngine.Object.FindFirstObjectByType<Relight.World.SceneWorld>();
var text="play="+UnityEditor.EditorApplication.isPlaying+", changing="+UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode+", world="+(w!=null)+", preview="+Relight.Editor.SceneWorldEditor.LastError;
for(var i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++){var scene=UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);text+="; "+scene.name+" loaded="+scene.isLoaded+" dirty="+scene.isDirty;}
if(w!=null){text+="; buildings="+w.GetComponentsInChildren<Relight.World.SceneBuilding>().Length;UnityEditor.Selection.activeGameObject=w.gameObject;Relight.Editor.SceneWorldEditor.FocusHome();}
System.IO.File.WriteAllText("E:/Factorio2/Unity/Docs/evidence/scene-authoring/final-state.txt",text);return text;
