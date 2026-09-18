Relight.Editor.SceneWorldEditor.RefreshNow();
var sv=UnityEditor.SceneView.lastActiveSceneView;sv.in2DMode=true;sv.LookAtDirect(new UnityEngine.Vector3(91,-361,0),UnityEngine.Quaternion.identity,14);sv.Repaint();
var nodes=UnityEngine.Object.FindFirstObjectByType<Relight.World.SceneWorld>().GetComponentsInChildren<UnityEngine.SpriteRenderer>().Where(r=>r.sprite!=null&&r.sprite.name.StartsWith("ironore_")).ToArray();
return "Iron sprites="+nodes.Length+" preview error="+Relight.Editor.SceneWorldEditor.LastError;

