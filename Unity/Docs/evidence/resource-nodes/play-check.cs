if(!UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Requires Play Mode");
var host=UnityEngine.Object.FindFirstObjectByType<Relight.Presentation.SimHost>();host.Paused=true;
var boot=UnityEngine.Object.FindFirstObjectByType<Relight.Presentation.WorldBootstrap>();
var sim=boot.StartSession(42);var st=sim.State;var ctx=sim.Context;
var city=UnityEngine.Object.FindFirstObjectByType<Relight.Presentation.CityPresenter>();city.RefreshResources(true);
var log=new System.Text.StringBuilder();void Check(bool ok,string note){log.AppendLine((ok?"PASS ":"FAIL ")+note);if(!ok)throw new System.Exception(log.ToString());}
Check(city.ResourceSpriteCount>0,"runtime node sprites="+city.ResourceSpriteCount);
var site=ctx.Sites.OfKind(Relight.Sim.SiteKind.Resource).First(s=>s.Item=="coal"&&s.Id.StartsWith("opening-"));
int x=site.X,y=site.Y;
var nodes=city.GetComponentsInChildren<UnityEngine.SpriteRenderer>();
var node=nodes.Last(r=>r.name=="Resource "+site.Id+" tile "+x+","+y);
var neighbor=nodes.Last(r=>r.name=="Resource "+site.Id+" tile "+(x+1)+","+y);var neighborSprite=neighbor.sprite;
Check(node.enabled&&node.sprite.name.Contains("_full_"),"untouched coal uses full sprite");
var initial=Relight.Sim.Ground.UnitsAt(ctx,st,x,y);st.Engineer.Pos=new Relight.Sim.Vec2(x-.5,y+.5);
Check(sim.Apply(new Relight.Sim.MineCommand(x,y)).Accepted,"real MineCommand accepted");
var phase=new Relight.Sim.MiningPhase();var dt=1.0/ctx.Data.Engineer.HandMinePerS;var count=0;
foreach(var threshold in new[]{.66,.33,0.0}){
 while(Relight.Sim.Ground.UnitsAt(ctx,st,x,y)>initial*threshold && count<1000){phase.Tick(ctx,st,dt);count++;}
 city.RefreshResources(true);
 var expected=threshold>.5?"_partial_":threshold>0?"_sparse_":null;
 Check(expected==null?!node.enabled:node.enabled&&node.sprite.name.Contains(expected),"actual mining state "+(expected??"cleared")+" after "+count+" units");
}
Check(neighbor.enabled&&neighbor.sprite==neighborSprite,"adjacent tile remains full");
Check(Relight.Sim.Ground.TileAt(ctx,st,x,y)==Relight.Sim.TileClass.Ground,"exhausted tile reads plain ground");
Check(Relight.Sim.Ledger.Conservation(st,ctx.Data).Ok,"mined inventory remains conserved");
var saved=Relight.Sim.SaveSerializer.Read(Relight.Sim.SaveSerializer.Write(st,ctx.Data),ctx);Check(saved.Ok,"in-memory save round-trip");
var saves=UnityEngine.Object.FindFirstObjectByType<Relight.Presentation.AutosaveController>();
saves.GetType().GetMethod("Adopt",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(saves,new object[]{saved});
city.RefreshResources(true);
node=city.GetComponentsInChildren<UnityEngine.SpriteRenderer>().Last(r=>r.name=="Resource "+site.Id+" tile "+x+","+y);
Check(!node.enabled,"loaded save retains visually cleared tile");
var rig=UnityEngine.Object.FindFirstObjectByType<Relight.Presentation.CameraRig>();if(rig!=null)rig.enabled=false;
var camera=UnityEngine.Camera.main;camera.transform.position=new UnityEngine.Vector3(90,-362,camera.transform.position.z);camera.orthographicSize=14;
System.IO.File.WriteAllText("E:/Factorio2/Unity/Docs/evidence/resource-nodes/play-checks.txt",log.ToString());
return log.ToString();

