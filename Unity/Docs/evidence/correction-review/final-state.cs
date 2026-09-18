var scenes = new System.Collections.Generic.List<object>();
for (int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++) { var s=UnityEngine.SceneManagement.SceneManager.GetSceneAt(i); scenes.Add(new {s.path,s.isDirty}); }
return new {playing=UnityEditor.EditorApplication.isPlaying,scenes};
