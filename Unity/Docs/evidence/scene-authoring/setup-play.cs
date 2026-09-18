var w=UnityEngine.Object.FindFirstObjectByType<Relight.World.SceneWorld>();var g=w.Compile(out _);
var found=false;var bx=0;var by=0;
for(var y=375;y<410&&!found;y++)for(var x=84;x<116&&!found;x++){var clear=true;for(var yy=y;yy<y+10;yy++)for(var xx=x;xx<x+10;xx++)if(g.SolidAt(xx,yy)||g.TileAt(xx,yy)!=Relight.Sim.TileClass.Ground)clear=false;if(clear){bx=x;by=y;found=true;}}
UnityEngine.Object.DestroyImmediate(g);if(!found)throw new System.Exception("No clear test plot");
var go=new UnityEngine.GameObject("TEMP authoring verification building");go.transform.SetParent(w.transform);go.transform.position=new UnityEngine.Vector3(bx,-by,0);var b=go.AddComponent<Relight.World.SceneBuilding>();b.id="test-enterable";b.buildingName="Test building";b.size=new UnityEngine.Vector2Int(8,8);
var prop=new UnityEngine.GameObject("TEMP verification prop");prop.transform.SetParent(go.transform,false);prop.transform.localPosition=new UnityEngine.Vector3(2,-2,0);var p=prop.AddComponent<Relight.World.SceneProp>();p.id="test-prop";p.blocksMovement=true;p.size=new UnityEngine.Vector2Int(2,1);
Relight.Editor.SceneWorldEditor.RefreshNow();w.showRoofs=false;Relight.Editor.SceneWorldEditor.RefreshNow();UnityEditor.Selection.activeGameObject=go;var view=UnityEditor.SceneView.lastActiveSceneView;view.LookAtDirect(go.transform.position+new UnityEngine.Vector3(4,-4,0),UnityEngine.Quaternion.identity,10);
return "Temporary test building at "+bx+","+by+"; not saved.";
