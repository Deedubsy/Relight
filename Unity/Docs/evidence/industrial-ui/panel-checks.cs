var host=UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();var sim=host.Simulation;
var root=UnityEngine.Object.FindObjectsByType<UnityEngine.UIElements.UIDocument>().First(d=>d.visualTreeAsset!=null&&d.visualTreeAsset.name=="GameUI").rootVisualElement;
var inv=UnityEngine.Object.FindAnyObjectByType<Relight.UI.InventoryPanelController>();var shell=UnityEngine.Object.FindAnyObjectByType<Relight.UI.UiShell>();
var keyboard=UnityEngine.InputSystem.Keyboard.current;var mouse=UnityEngine.InputSystem.Mouse.current;
UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode=UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
UnityEngine.InputSystem.InputSystem.EnableDevice(keyboard);UnityEngine.InputSystem.InputSystem.EnableDevice(mouse);
string log="E:/Factorio2/Unity/Docs/evidence/industrial-ui/panel-checks.txt";System.IO.File.WriteAllText(log,"PREPARED LATER-STATE UI INSPECTION: +200 steel, +200 copper, +10 coal. Normal opening evidence is separate.\n");
shell.CloseActive();var loaded=Relight.Sim.SaveSerializer.Read(System.IO.File.ReadAllBytes("E:/Factorio2/Unity/Docs/evidence/ux-pass/final-prepared.json"),sim.Context);if(!loaded.Ok)throw new System.Exception(loaded.Reason);host.Attach(Relight.Sim.Simulation.Wrap(sim.Context,loaded.State));sim=host.Simulation;
void Note(string s){System.IO.File.AppendAllText(log,s+"\n");}
void Check(bool c,string text){if(!c)throw new System.Exception(text);Note("PASS "+text);}
UnityEngine.UIElements.VisualElement Q(string name)=>UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.VisualElement>(root,name);
System.Collections.IEnumerator Click(UnityEngine.UIElements.VisualElement b){
Check(b!=null,"Control exists: "+b?.name);var p=b.worldBound.center;
using(var e=UnityEngine.UIElements.PointerDownEvent.GetPooled(new UnityEngine.Event{type=UnityEngine.EventType.MouseDown,mousePosition=p,button=0})){e.target=b;b.SendEvent(e);}yield return null;
using(var e=UnityEngine.UIElements.PointerUpEvent.GetPooled(new UnityEngine.Event{type=UnityEngine.EventType.MouseUp,mousePosition=p,button=0})){e.target=b;b.SendEvent(e);}yield return new UnityEngine.WaitForSecondsRealtime(.2f);
}
System.Collections.IEnumerator Tap(UnityEngine.InputSystem.Key k){UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState(k));yield return null;yield return null;UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState());yield return null;yield return null;}
int Place(string kind){var p=sim.State.Engineer.Pos;for(int r=0;r<6;r++)for(int y=(int)p.Y-r;y<=(int)p.Y+r;y++)for(int x=(int)p.X-r;x<=(int)p.X+r;x++)if(Relight.Sim.Placement.Validity(sim.Context,sim.State,kind,x,y,Relight.Sim.Dir.N).ok){var ok=sim.Apply(new Relight.Sim.PlaceMachineCommand(kind,x,y,Relight.Sim.Dir.N));if(ok.Accepted)return Relight.Sim.ProductionRules.MachineAt(sim.State,x,y).Id;}throw new System.Exception("No placement for "+kind);}
System.Collections.IEnumerator Open(int id){shell.CloseActive();yield return null;var m=sim.State.MachineById(id);var v=UnityEngine.Camera.main.WorldToScreenPoint(new UnityEngine.Vector3(m.X+.5f,-m.Y-.5f,0));UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse,new UnityEngine.InputSystem.LowLevel.MouseState{position=new UnityEngine.Vector2(v.x,v.y)});yield return null;yield return null;yield return Tap(UnityEngine.InputSystem.Key.E);yield return new UnityEngine.WaitForSecondsRealtime(.2f);Check(inv.Model.MachineId==id,"E opens pointed machine "+m.Kind);}
System.Collections.IEnumerator Run(){
yield return new UnityEngine.WaitForSecondsRealtime(.3f);
var chest=sim.State.Machines.First(m=>m.Kind=="chest");yield return Open(chest.Id);
Check(Q("inventory-panel").ClassListContains("storage-open"),"Storage expands paired case");
var total=sim.State.Engineer.Inv[Relight.Sim.ItemId.Steel]+chest.Inv[Relight.Sim.ItemId.Steel];var before=chest.Inv[Relight.Sim.ItemId.Steel];
inv.Paint(true);int pack=inv.Model.Pack.First(c=>c.Item=="steel").Index;yield return Click(Q("pack-"+pack));((UnityEngine.UIElements.TextField)Q("quantity")).value="7";yield return Click(Q("transfer"));
Check(chest.Inv[Relight.Sim.ItemId.Steel]==before+7,"Load exactly seven steel through UI");
inv.Paint(true);int slot=inv.Model.Store.First(c=>c.Item=="steel").Index;yield return Click(Q("store-"+slot));((UnityEngine.UIElements.TextField)Q("quantity")).value="3";yield return Click(Q("transfer"));
Check(chest.Inv[Relight.Sim.ItemId.Steel]==before+4 && sim.State.Engineer.Inv[Relight.Sim.ItemId.Steel]+chest.Inv[Relight.Sim.ItemId.Steel]==total,"Take three back; stock conserved");
UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/industrial-ui/storage-1080.png");yield return null;
yield return Tap(UnityEngine.InputSystem.Key.Tab);yield return Tap(UnityEngine.InputSystem.Key.Tab);yield return new UnityEngine.WaitForSecondsRealtime(.2f);
Check(inv.Model.MachineId==-1,"Reopening Backpack clears storage ownership");
((UnityEngine.UIElements.Foldout)Q("workshop-foldout")).value=true;yield return new UnityEngine.WaitForSecondsRealtime(.3f);
UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/industrial-ui/workshop-1080.png");yield return null;
yield return Click(Q("jump-workshop"));
var craft=UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.Button>(Q("card-hand-bullets"),"card-craft");var cancel=UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.Button>(Q("card-hand-bullets"),"card-cancel");
var steel=sim.State.Engineer.Inv[Relight.Sim.ItemId.Steel];var copper=sim.State.Engineer.Inv[Relight.Sim.ItemId.Copper];yield return Click(craft);Check(sim.State.Hand.Crafting,"Recipe Craft still starts real handcraft");
yield return new UnityEngine.WaitForSecondsRealtime(.8f);Check(UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.VisualElement>(Q("card-hand-bullets"),"card-progress-fill").resolvedStyle.width>0,"Recipe progress fill is live");
UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/industrial-ui/crafting-1080.png");yield return null;
yield return Click(cancel);Check(!sim.State.Hand.Crafting && sim.State.Engineer.Inv[Relight.Sim.ItemId.Steel]==steel && sim.State.Engineer.Inv[Relight.Sim.ItemId.Copper]==copper,"Cancel returns the reserved ingredients");
yield return Click(Q("jump-workshop"));shell.CloseActive();yield return Tap(UnityEngine.InputSystem.Key.Digit9);yield return new UnityEngine.WaitForSecondsRealtime(.25f);
Check(Q("hud-slot-8").ClassListContains("selected"),"Equipped weapon shows selected dock rim");
var pos=sim.State.Engineer.Pos;yield return Tap(UnityEngine.InputSystem.Key.D);Check(sim.State.Engineer.Pos.X>pos.X,"Keyboard movement retained");
yield return Tap(UnityEngine.InputSystem.Key.B);Check(shell.Active=="build-panel","B opens Build");
UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/industrial-ui/build-1080.png");yield return null;
Note("PANEL CHECKS DONE");}
System.Collections.IEnumerator Guard(){var stack=new System.Collections.Generic.Stack<System.Collections.IEnumerator>();stack.Push(Run());while(stack.Count>0){var e=stack.Peek();bool next=false;try{next=e.MoveNext();}catch(System.Exception ex){Note("FAIL "+ex.Message);yield break;}if(!next){stack.Pop();continue;}if(e.Current is System.Collections.IEnumerator child){stack.Push(child);continue;}yield return e.Current;}}
host.StartCoroutine(Guard());return "Supporting UI pass started (prepared state)";
