if(!UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Not in Play Mode");
var host=UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();host.enabled=false;host.Paused=false;
var sim=host.Simulation;var log=new System.Text.StringBuilder();
void Check(bool ok,string text){log.AppendLine((ok?"PASS ":"FAIL ")+text);if(!ok){System.IO.File.WriteAllText("E:/Factorio2/Unity/Docs/evidence/scene-authoring/play.txt",log.ToString());throw new System.Exception(text);}}
var b=UnityEngine.Object.FindAnyObjectByType<Relight.World.SceneWorld>().GetComponentsInChildren<Relight.World.SceneBuilding>().First(x=>x.id=="test-enterable");var r=b.Record().rect;
Check(sim.Context.MapId.Contains("-scene-"),"New Game uses authored scene geometry");
Check(UnityEngine.GameObject.Find("Editable world display (generated)")==null,"editor preview is absent in Play Mode");
Check(sim.Context.Geometry.Solid(r.x,r.y),"new building wall blocks movement");
Check(!sim.Context.Geometry.Solid(r.x+3,r.y+7),"doorway remains open");
Check(sim.Context.Geometry.Solid(r.x+2,r.y+2),"new prop occupies its two intended cells");
void Walk(double x,double y,double dx,double dy,int ticks){sim.State.Engineer.Pos=new Relight.Sim.Vec2(x,y);sim.Apply(new Relight.Sim.WalkCommand(dx,dy));for(var i=0;i<ticks;i++)sim.Tick();sim.Apply(new Relight.Sim.WalkCommand(0,0));}
Walk(r.x+3.5,r.y+8.5,0,-1,20);Check(sim.State.Engineer.Pos.Y<r.y+7,"engineer walks through new doorway into interior");
Walk(r.x+1.5,r.y+1.5,0,-1,30);Check(sim.State.Engineer.Pos.Y>=r.y+1,"engineer stops at new north wall");
Walk(r.x+1.5,r.y+2.5,1,0,30);Check(sim.State.Engineer.Pos.X<r.x+2,"engineer stops at solid prop");
var ctx=sim.Context;var bytes=Relight.Sim.SaveSerializer.Write(sim.State,ctx,null,out _);var load=Relight.Sim.SaveSerializer.Read(bytes,ctx);Check(load.Ok,"authored map save round-trip: "+load.Reason);
var autosave=UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.AutosaveController>();var adopt=autosave.GetType().GetMethod("Adopt",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
var bootstrap=UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.WorldBootstrap>();var oldContext=bootstrap.ContextForLayout(0,false);var oldSim=Relight.Sim.Simulation.NewGame(oldContext,1);var oldBytes=Relight.Sim.SaveSerializer.Write(oldSim.State,oldContext,null,out _);var originalLoad=Relight.Sim.SaveSerializer.Read(oldBytes,oldContext);Check(originalLoad.Ok,"legacy save fixture valid");
adopt.Invoke(autosave,new object[]{originalLoad});Check(host.Simulation.Context.MapId==bootstrap.SourceGeometry.MapId&&host.Simulation.Context.Geometry.TileAt(59,352)==Relight.Sim.TileClass.Patch,"legacy adoption restores original map and deposits");
adopt.Invoke(autosave,new object[]{load});Check(host.Simulation.Context.MapId==ctx.MapId&&host.Simulation.Context.Geometry.Solid(r.x,r.y),"authored save adoption restores edited geometry");
var foreignBytes=Relight.Sim.SaveSerializer.Write(sim.State,ctx.Data,null,"different-map",Relight.Sim.SaveRegion.Of(ctx));var foreignLoad=Relight.Sim.SaveSerializer.Read(foreignBytes,ctx.Data);var session=host.Session;
var refused=(Relight.Sim.LoadResult)adopt.Invoke(autosave,new object[]{foreignLoad});Check(!refused.Ok&&host.Session==session,"foreign map refused without replacing active session");
log.AppendLine("DONE");System.IO.File.WriteAllText("E:/Factorio2/Unity/Docs/evidence/scene-authoring/play.txt",log.ToString());return log.ToString();
