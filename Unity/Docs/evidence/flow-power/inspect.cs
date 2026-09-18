return new {
playing=UnityEditor.EditorApplication.isPlaying,
scenes=Enumerable.Range(0,UnityEngine.SceneManagement.SceneManager.sceneCount).Select(i=>new {path=UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).path,dirty=UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty}).ToArray(),
belts=UnityEngine.Object.FindObjectsByType<Relight.Presentation.BeltItemPresenter>().Select(x=>new {name=x.name,registry=new UnityEditor.SerializedObject(x).FindProperty("registry").objectReferenceValue?.name}).ToArray(),
machines=UnityEngine.Object.FindObjectsByType<Relight.Presentation.MachinePresenter>().Select(x=>x.name).ToArray(),
icons=UnityEditor.AssetDatabase.FindAssets("t:GameDataRegistry").Select(g=>UnityEditor.AssetDatabase.LoadAssetAtPath<Relight.Data.GameDataRegistry>(UnityEditor.AssetDatabase.GUIDToAssetPath(g))).Select(r=>new {name=r.name,items=r.Items.Select(i=>new {i.Key,icon=i.Icon==null?null:i.Icon.name}).ToArray()}).ToArray()
};
