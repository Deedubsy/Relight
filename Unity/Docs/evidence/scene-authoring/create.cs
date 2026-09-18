if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Stop play before creating authoring objects.");
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByName("World");
if(!scene.isLoaded)scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Relight/Scenes/World.unity",UnityEditor.SceneManagement.OpenSceneMode.Additive);
UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
Relight.Editor.SceneWorldEditor.Create();
return "Created editable world. Preview error: "+Relight.Editor.SceneWorldEditor.LastError;
