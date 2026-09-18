var host=UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();var sim=host.Simulation;
var root=UnityEngine.Object.FindObjectsByType<UnityEngine.UIElements.UIDocument>().First(d=>d.visualTreeAsset!=null&&d.visualTreeAsset.name=="GameUI").rootVisualElement;
var inv=UnityEngine.Object.FindAnyObjectByType<Relight.UI.InventoryPanelController>();var shell=UnityEngine.Object.FindAnyObjectByType<Relight.UI.UiShell>();
var keyboard=UnityEngine.InputSystem.Keyboard.current;var mouse=UnityEngine.InputSystem.Mouse.current;
UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode=UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
UnityEngine.InputSystem.InputSystem.EnableDevice(keyboard);UnityEngine.InputSystem.InputSystem.EnableDevice(mouse);
var read=Relight.Sim.SaveSerializer.Read(System.IO.File.ReadAllBytes("E:/Factorio2/Unity/Docs/evidence/ux-pass/prepared-latest.json"),sim.Context);if(!read.Ok)throw new System.Exception(read.Reason);host.Attach(Relight.Sim.Simulation.Wrap(sim.Context,read.State));sim=host.Simulation;
string log="E:/Factorio2/Unity/Docs/evidence/ux-pass/final-input-log.txt";System.IO.File.WriteAllText(log,"Prepared later-state: pointer-event UI gestures and queued keyboard input.\n");
void Note(string s){System.IO.File.AppendAllText(log,s+"\n");}
void Check(bool c,string text){if(!c)throw new System.Exception(text);Note("PASS "+text);}
UnityEngine.UIElements.VisualElement Q(string name)=>UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.VisualElement>(root,name);
System.Collections.IEnumerator Click(UnityEngine.UIElements.VisualElement b){
Check(b!=null,"Control exists: "+b?.name);var p=b.worldBound.center;
using(var e=UnityEngine.UIElements.PointerDownEvent.GetPooled(new UnityEngine.Event{type=UnityEngine.EventType.MouseDown,mousePosition=p,button=0})){e.target=b;b.SendEvent(e);}yield return null;
using(var e=UnityEngine.UIElements.PointerUpEvent.GetPooled(new UnityEngine.Event{type=UnityEngine.EventType.MouseUp,mousePosition=p,button=0})){e.target=b;b.SendEvent(e);}yield return new UnityEngine.WaitForSecondsRealtime(.2f);
}
System.Collections.IEnumerator Tap(UnityEngine.InputSystem.Key k){UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState(k));yield return null;yield return null;UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState());yield return null;yield return null;}

System.Collections.IEnumerator Run(){
yield return Tap(UnityEngine.InputSystem.Key.Tab); yield return Click(Q("jump-workshop"));
yield return Click(UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.Button>(Q("card-rifle"),"card-craft"));
Check(sim.State.Weapons.Crafting,"Rifle recipe starts through its card");
float until=UnityEngine.Time.realtimeSinceStartup+10;while(sim.State.Weapons.Crafting && UnityEngine.Time.realtimeSinceStartup<until)yield return null;
Check(sim.State.Weapons.Owned.Count==1,"Crafted rifle exists as one owned instance");inv.Paint(true);
var cell=inv.Model.Pack.First(c=>c.IsWeapon);var weapon=cell.Item;
((UnityEngine.UIElements.ScrollView)Q("backpack-body")).ScrollTo(Q("pack-"+cell.Index));yield return new UnityEngine.WaitForSecondsRealtime(.3f);
var from=Q("pack-"+cell.Index);var to=Q("hud-slot-8");
using(var e=UnityEngine.UIElements.PointerDownEvent.GetPooled(new UnityEngine.Event{type=UnityEngine.EventType.MouseDown,mousePosition=from.worldBound.center,button=0})){e.target=from;from.SendEvent(e);}yield return null;
using(var e=UnityEngine.UIElements.PointerMoveEvent.GetPooled(new UnityEngine.Event{type=UnityEngine.EventType.MouseDrag,mousePosition=to.worldBound.center,button=0})){e.target=from;from.SendEvent(e);}yield return null;
Check(Relight.UI.UiDrag.Dragging,"Pointer drag carries inventory stack");
using(var e=UnityEngine.UIElements.PointerUpEvent.GetPooled(new UnityEngine.Event{type=UnityEngine.EventType.MouseUp,mousePosition=to.worldBound.center,button=0})){e.target=from;from.SendEvent(e);}yield return new UnityEngine.WaitForSecondsRealtime(.4f);
Check(sim.State.Weapons.Bar[8]==weapon,"Dragging owned rifle assigns persistent HUD slot 9");Check(sim.State.Weapons.Owned.Count==1,"Assigning shortcut preserves owned weapon identity");
yield return Tap(UnityEngine.InputSystem.Key.Tab);yield return Tap(UnityEngine.InputSystem.Key.Digit9);
Check(Relight.Sim.WeaponQueries.Equipped(sim.State,sim.Context.Data).Item==weapon,"9 selects and equips the same weapon");yield return Tap(UnityEngine.InputSystem.Key.R);
Check(Relight.Sim.WeaponQueries.Equipped(sim.State,sim.Context.Data).Reloading,"R starts reload while weapon selected");
UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/ux-pass/final-combat-1080.png");yield return new UnityEngine.WaitForSecondsRealtime(3f);
var equipped=Relight.Sim.WeaponQueries.Equipped(sim.State,sim.Context.Data);Check(equipped.Loaded==10 && equipped.Reserve==0,"Reload moves ten crafted bullets into the same rifle");
yield return Click(Q("action-inventory"));Check(shell.Active=="inventory-panel","HUD Backpack button opens drawer");Check(equipped.Loaded==Relight.Sim.WeaponQueries.Equipped(sim.State,sim.Context.Data).Loaded,"UI click does not fire rifle");
UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/ux-pass/final-inventory-1080.png");yield return null;
System.IO.File.WriteAllBytes("E:/Factorio2/Unity/Docs/evidence/ux-pass/final-prepared.json",Relight.Sim.SaveSerializer.Write(sim.State,sim.Context,null,out _));Note("FINAL INPUT DONE");
}
System.Collections.IEnumerator Guard(){var stack=new System.Collections.Generic.Stack<System.Collections.IEnumerator>();stack.Push(Run());while(stack.Count>0){var e=stack.Peek();bool next=false;try{next=e.MoveNext();}catch(System.Exception ex){Note("FAIL "+ex.Message);yield break;}if(!next){stack.Pop();continue;}if(e.Current is System.Collections.IEnumerator child){stack.Push(child);continue;}yield return e.Current;}}
host.StartCoroutine(Guard());return "Final weapon UI pass started";
