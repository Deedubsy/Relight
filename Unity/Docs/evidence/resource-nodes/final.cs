if(UnityEditor.EditorApplication.isPlaying||UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new System.Exception("Still in Play Mode");
var world=UnityEngine.Object.FindFirstObjectByType<Relight.World.SceneWorld>();
var sites=world.GetComponentsInChildren<Relight.World.SceneSite>().Where(s=>s.kind==Relight.Sim.SiteKind.Resource).ToArray();
var count=0;foreach(var site in sites){var nodes=site.GetComponentsInChildren<UnityEngine.SpriteRenderer>();if(nodes.Length!=site.size.x*site.size.y)throw new System.Exception("Missing sprites on "+site.id);count+=nodes.Length;}
if(UnityEngine.GameObject.Find("Temporary resource sprite QA")!=null)throw new System.Exception("Temporary gallery remains");
var scene=world.gameObject.scene;
return "Edit Mode; "+sites.Length+" deposits; "+count+" sprites; scene dirty="+scene.isDirty+"; preview error="+Relight.Editor.SceneWorldEditor.LastError;
