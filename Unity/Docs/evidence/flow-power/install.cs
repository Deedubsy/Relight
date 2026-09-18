if(UnityEditor.EditorApplication.isPlaying) throw new System.Exception("Must install scene components in edit mode");
var presenter=UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.MachinePresenter>();
if(presenter.GetComponent<Relight.Presentation.PowerConnectionPresenter>()==null) UnityEditor.Undo.AddComponent<Relight.Presentation.PowerConnectionPresenter>(presenter.gameObject);
var library=UnityEngine.Resources.Load<Relight.UI.ItemIconLibrary>("RelightItemIcons");
var registry=UnityEditor.AssetDatabase.LoadAssetAtPath<Relight.Data.GameDataRegistry>(UnityEditor.AssetDatabase.GUIDToAssetPath(UnityEditor.AssetDatabase.FindAssets("t:GameDataRegistry")[0]));
var assigned=new System.Collections.Generic.List<string>();
foreach(var item in registry.Items) {
if(!library.TryGet(item.Key,out var sprite)) throw new System.Exception("No icon for "+item.Key);
var so=new UnityEditor.SerializedObject(item);so.FindProperty("icon").objectReferenceValue=sprite;so.ApplyModifiedProperties();UnityEditor.EditorUtility.SetDirty(item);assigned.Add(item.Key);
}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(presenter.gameObject.scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(presenter.gameObject.scene);
UnityEditor.AssetDatabase.SaveAssets();
return new {cables=presenter.GetComponent<Relight.Presentation.PowerConnectionPresenter>()!=null,icons=assigned};
