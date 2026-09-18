if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Install in edit mode");
var doc=UnityEngine.Object.FindObjectsByType<UnityEngine.UIElements.UIDocument>().First(d=>d.visualTreeAsset!=null&&d.visualTreeAsset.name=="GameUI");
if(doc.GetComponent<Relight.UI.AdminPanelController>()==null)UnityEditor.Undo.AddComponent<Relight.UI.AdminPanelController>(doc.gameObject);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(doc.gameObject.scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(doc.gameObject.scene);
UnityEditor.AssetDatabase.SaveAssets();
return new {controller=doc.GetComponent<Relight.UI.AdminPanelController>()!=null,scene=doc.gameObject.scene.path};
