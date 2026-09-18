var host=UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();var sim=host.Simulation;
var inv=UnityEngine.Object.FindAnyObjectByType<Relight.UI.InventoryPanelController>();var shell=UnityEngine.Object.FindAnyObjectByType<Relight.UI.UiShell>();
var doc=inv.GetComponent<UnityEngine.UIElements.UIDocument>();var root=doc.rootVisualElement;
UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode=UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
UnityEngine.InputSystem.InputSystem.EnableDevice(UnityEngine.InputSystem.Mouse.current);
sim.Apply(new Relight.Sim.AdminCommand("grant","coal",20));sim.Context.Data.TryMachine("generator",out var spec);
var gen=new Relight.Sim.Machine{Id=sim.State.NextId++,Kind="generator",X=72,Y=360,Size=spec.Size};sim.State.Machines.Add(gen);sim.State.Rev++;
sim.State.Engineer.Pos=new Relight.Sim.Vec2(71,361);inv.OpenStore(gen.Id);shell.Open("inventory-panel");
var log="E:/Factorio2/Unity/Docs/evidence/inventory-drag/baseline.txt";System.IO.File.WriteAllText(log,"Generator storage cells: "+inv.Model.Store.Count+"\n");
UnityEngine.Application.LogCallback logError=(message,stack,type)=>{if(type==UnityEngine.LogType.Exception||type==UnityEngine.LogType.Error)System.IO.File.AppendAllText(log,message+"\n"+stack+"\n");};
UnityEngine.Application.logMessageReceived+=logError;
UnityEngine.Vector2 Screen(UnityEngine.Vector2 p)=>new UnityEngine.Vector2(p.x*UnityEngine.Screen.width/root.worldBound.width,UnityEngine.Screen.height-p.y*UnityEngine.Screen.height/root.worldBound.height);
UnityEngine.UIElements.Button dragSource=null;bool wasDown=false;
void Mouse(UnityEngine.Vector2 p,bool down){var kind=down&&!wasDown?UnityEngine.EventType.MouseDown:!down&&wasDown?UnityEngine.EventType.MouseUp:UnityEngine.EventType.MouseMove;var raw=new UnityEngine.Event{type=kind,mousePosition=p,button=0};
if(kind==UnityEngine.EventType.MouseDown){using(var e=UnityEngine.UIElements.PointerDownEvent.GetPooled(raw)){e.target=dragSource;dragSource.SendEvent(e);}}
else if(kind==UnityEngine.EventType.MouseUp){using(var e=UnityEngine.UIElements.PointerUpEvent.GetPooled(raw)){e.target=dragSource;dragSource.SendEvent(e);}}
else{using(var e=UnityEngine.UIElements.PointerMoveEvent.GetPooled(raw)){e.target=dragSource;dragSource.SendEvent(e);}}
wasDown=down;}
System.Collections.IEnumerator Run(){yield return new UnityEngine.WaitForSecondsRealtime(.3f);
dragSource=UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.Button>(root,"pack-0");var from=dragSource.worldBound.center;
var to=UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.Button>(root,"pack-5").worldBound.center;
Mouse(from,false);yield return new UnityEngine.WaitForSecondsRealtime(.1f);Mouse(from,true);yield return new UnityEngine.WaitForSecondsRealtime(.15f);Mouse(UnityEngine.Vector2.Lerp(from,to,.5f),true);yield return new UnityEngine.WaitForSecondsRealtime(.15f);Mouse(to,true);yield return new UnityEngine.WaitForSecondsRealtime(.15f);Mouse(to,false);yield return new UnityEngine.WaitForSecondsRealtime(.2f);
System.IO.File.AppendAllText(log,"Slot 5 after drag: "+inv.Model.Pack[5].Item+"\nNotice: "+inv.Notice+"\n");UnityEngine.Application.logMessageReceived-=logError;UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/inventory-drag/baseline.png");}
host.StartCoroutine(Run());return "Pointer-event reproduction started";
