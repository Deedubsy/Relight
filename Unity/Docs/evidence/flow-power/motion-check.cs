var host=UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();var sim=host.Simulation;
var presenter=UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.BeltItemPresenter>();
var sprites=(System.Collections.Generic.List<UnityEngine.SpriteRenderer>)presenter.GetType().GetField("_live",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(presenter);
var doc=UnityEngine.Object.FindObjectsByType<UnityEngine.UIElements.UIDocument>().First(d=>d.visualTreeAsset!=null&&d.visualTreeAsset.name=="GameUI");
var root=doc.rootVisualElement;var log="E:/Factorio2/Unity/Docs/evidence/flow-power/motion.txt";
System.Collections.IEnumerator Run(){
host.Paused=false;
// Keep a chest-fed conveyor moving for frame sampling, using the real flow phase at normal speed.
var source=sim.State.MachineById(12);source.Inv[Relight.Sim.ItemId.Copper]=50;
var last=sim.State.MachineById(16);last.Dir=Relight.Sim.Dir.S;
sim.State.Machines.Add(new Relight.Sim.Machine{Id=sim.State.NextId++,Kind="chest",X=60,Y=368,Size=2});sim.State.Rev++;
var oldTick=sim.State.Tick;var positions=new System.Collections.Generic.Dictionary<UnityEngine.SpriteRenderer,UnityEngine.Vector3>();var observed=false;
for(var frame=0;frame<180;frame++){
yield return new UnityEngine.WaitForEndOfFrame();
foreach(var sprite in sprites){var id=sprite;if(positions.TryGetValue(id,out var p)&&oldTick==sim.State.Tick&&UnityEngine.Vector3.Distance(p,sprite.transform.position)>.00001f)observed=true;positions[id]=sprite.transform.position;}
oldTick=sim.State.Tick;if(observed)break;
}
System.IO.File.WriteAllText(log,(observed?"PASS":"FAIL")+" belt sprites move between fixed simulation ticks\n");
if(!observed)throw new System.Exception("Did not observe inter-tick sprite movement");
// Hover the powered extractor at the smaller game size; no synthetic text.
var game=UnityEditor.EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView"));
game.GetType().GetProperty("selectedSizeIndex",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic).SetValue(game,9);
yield return new UnityEngine.WaitForSecondsRealtime(.3f);
var camera=UnityEngine.Camera.main;var point=camera.WorldToScreenPoint(new UnityEngine.Vector3(61.5f,-356.5f,0));
UnityEngine.InputSystem.InputSystem.QueueStateEvent(UnityEngine.InputSystem.Mouse.current,new UnityEngine.InputSystem.LowLevel.MouseState{position=new UnityEngine.Vector2(point.x,point.y)});
yield return new UnityEngine.WaitForSecondsRealtime(.3f);
var hover=UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.Label>(root,"target-detail");
System.IO.File.AppendAllText(log,"Hover: "+hover.text+"\n");
UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/flow-power/hover-720.png");yield return null;yield return null;
game.GetType().GetProperty("selectedSizeIndex",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic).SetValue(game,3);
host.Paused=true;System.IO.File.AppendAllText(log,"DONE\n");
}
host.StartCoroutine(Run());return "Motion and 720p checks started";


