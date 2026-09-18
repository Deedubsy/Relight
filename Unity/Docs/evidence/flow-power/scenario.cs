var host=UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();
var sim=Relight.Sim.Simulation.NewGame(host.Simulation.Context,1);var st=sim.State;
Relight.Sim.Machine Add(string kind,int x,int y,Relight.Sim.Dir dir=Relight.Sim.Dir.N) {sim.Context.Data.TryMachine(kind,out var spec);var m=new Relight.Sim.Machine{Id=st.NextId++,Kind=kind,X=x,Y=y,Dir=dir,Size=spec.Size};st.Machines.Add(m);st.Rev++;return m;}
var dig=Add("excavator",60,355,Relight.Sim.Dir.S);
for(int y=358;y<=362;y++) Add("belt",61,y,Relight.Sim.Dir.S);
var dest=Add("chest",61,363);
var pole=Add("pole",64,359);var pole2=Add("pole",65,364);
var gen=Add("generator",66,362);gen.Inv.Add(Relight.Sim.ItemId.Coal,10);
var source=Add("chest",55,363);source.Inv.Add(Relight.Sim.ItemId.Copper,80);
for(int x=57;x<=60;x++)Add("belt",x,364,Relight.Sim.Dir.E);
st.Engineer.Pos=new Relight.Sim.Vec2(64,361);st.Engineer.Vel=new Relight.Sim.Vec2(0,0);st.Engineer.Inv[Relight.Sim.ItemId.Steel]=100;
st.Ledger=Relight.Sim.Ledger.Open(st,sim.Context.Data);
host.Attach(sim);host.Paused=false;
UnityEngine.Object.FindAnyObjectByType<Relight.UI.UiShell>().CloseActive();UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.WorldInput>().ClearHand();
UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode=UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
UnityEngine.InputSystem.InputSystem.EnableDevice(UnityEngine.InputSystem.Mouse.current);
var camera=UnityEngine.Camera.main;camera.orthographicSize=10;camera.transform.position=new UnityEngine.Vector3(63,-360,-10);
var screen=camera.WorldToScreenPoint(new UnityEngine.Vector3(61.5f,-356.5f,0));
UnityEngine.InputSystem.InputSystem.QueueStateEvent(UnityEngine.InputSystem.Mouse.current,new UnityEngine.InputSystem.LowLevel.MouseState{position=new UnityEngine.Vector2(screen.x,screen.y)});
System.IO.File.WriteAllText("E:/Factorio2/Unity/Docs/evidence/flow-power/runtime-ids.json",UnityEngine.JsonUtility.ToJson(new UnityEngine.Vector4(dig.Id,dest.Id,source.Id,pole.Id)));
return new{dig=dig.Id,dest=dest.Id,pole=pole.Id,source=source.Id};


