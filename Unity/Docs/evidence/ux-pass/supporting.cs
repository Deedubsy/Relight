var host=UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();var sim=host.Simulation;
var root=UnityEngine.Object.FindObjectsByType<UnityEngine.UIElements.UIDocument>().First(d=>d.visualTreeAsset!=null&&d.visualTreeAsset.name=="GameUI").rootVisualElement;
var inv=UnityEngine.Object.FindAnyObjectByType<Relight.UI.InventoryPanelController>();var shell=UnityEngine.Object.FindAnyObjectByType<Relight.UI.UiShell>();
var keyboard=UnityEngine.InputSystem.Keyboard.current;var mouse=UnityEngine.InputSystem.Mouse.current;
UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode=UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
UnityEngine.InputSystem.InputSystem.EnableDevice(keyboard);UnityEngine.InputSystem.InputSystem.EnableDevice(mouse);
string log="E:/Factorio2/Unity/Docs/evidence/ux-pass/supporting-log.txt";System.IO.File.WriteAllText(log,"PREPARED LATER-STATE UI INSPECTION: +200 steel, +200 copper, +10 coal. Normal opening evidence is separate.\n");
sim.State.Engineer.Inv[Relight.Sim.ItemId.Steel]+=200;sim.State.Engineer.Inv[Relight.Sim.ItemId.Copper]+=200;sim.State.Engineer.Inv[Relight.Sim.ItemId.Coal]+=10;
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
int chestId=Place("chest");var chest=sim.State.MachineById(chestId);yield return Open(chestId);
double total=sim.State.Engineer.Inv[Relight.Sim.ItemId.Steel]+chest.Inv[Relight.Sim.ItemId.Steel];
for(int i=0;i<3;i++){
inv.Paint(true);int pack=inv.Model.Pack.First(c=>c.Item=="steel").Index;yield return Click(Q("pack-"+pack));((UnityEngine.UIElements.TextField)Q("quantity")).value="7";yield return Click(Q("transfer"));
Check(chest.Inv[Relight.Sim.ItemId.Steel]>=7,"Load chosen quantity into storage");
inv.Paint(true);int slot=inv.Model.Store.First(c=>c.Item=="steel").Index;yield return Click(Q("store-"+slot));((UnityEngine.UIElements.TextField)Q("quantity")).value="3";yield return Click(Q("transfer"));
Check(sim.State.Engineer.Inv[Relight.Sim.ItemId.Steel]+chest.Inv[Relight.Sim.ItemId.Steel]==total,"Transfer cycle "+i+" conserves stock");
yield return Tap(UnityEngine.InputSystem.Key.Tab);yield return Open(chestId);
}
UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/ux-pass/storage-1080.png");
for(int i=0;i<3;i++){
inv.Paint(true);var source=inv.Model.Pack.First(c=>c.Item=="steel" && c.Count>10);yield return Click(Q("pack-"+source.Index));((UnityEngine.UIElements.TextField)Q("quantity")).value="5";yield return Click(Q("split"));inv.Paint(true);
var donor=inv.Model.Pack.First(c=>c.Item=="steel" && c.Index!=source.Index);
var payload=inv.DragFrom(Relight.UI.InventoryPanelController.SlotKind.Pack,donor.Index);inv.DragTo(Relight.UI.InventoryPanelController.SlotKind.Pack,source.Index,payload);yield return null;inv.Paint(true);
Check(sim.State.Engineer.Inv[Relight.Sim.ItemId.Steel]+chest.Inv[Relight.Sim.ItemId.Steel]==total,"Split/merge cycle "+i+" conserves stock");Check(inv.Model.Pack.All(c=>c.Count<=c.StackSize),"Merged slots respect stack limits");}
yield return Tap(UnityEngine.InputSystem.Key.Tab);yield return Tap(UnityEngine.InputSystem.Key.Tab);
Check(inv.Model.MachineId==-1,"Backpack resets storage pairing");yield return Click(Q("jump-workshop"));yield return new UnityEngine.WaitForSecondsRealtime(.3f);
var craft=UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.Button>(Q("card-hand-bullets"),"card-craft");var cancel=UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.Button>(Q("card-hand-bullets"),"card-cancel");
double beforeSteel=sim.State.Engineer.Inv[Relight.Sim.ItemId.Steel],beforeCopper=sim.State.Engineer.Inv[Relight.Sim.ItemId.Copper];yield return Click(craft);Check(sim.State.Hand.Crafting,"Workshop Craft starts production");yield return Click(cancel);Check(!sim.State.Hand.Crafting,"Workshop Cancel stops production");Check(sim.State.Engineer.Inv[Relight.Sim.ItemId.Steel]==beforeSteel && sim.State.Engineer.Inv[Relight.Sim.ItemId.Copper]==beforeCopper,"Cancellation returns reserved ingredients");
yield return Click(craft);UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/ux-pass/workshop-progress-1080.png");
float until=UnityEngine.Time.realtimeSinceStartup+30;while(sim.State.Hand.Crafting && UnityEngine.Time.realtimeSinceStartup<until)yield return null;Check(sim.State.Engineer.Inv[Relight.Sim.ItemId.Magazine]>0,"Finished crafting adds individual bullets to Backpack");
shell.CloseActive();int assembler=Place("assembler");yield return Open(assembler);var picker=(UnityEngine.UIElements.DropdownField)Q("machine-recipe");picker.index=1;yield return Click(Q("apply-recipe"));var recipe=Relight.Sim.ProductionQueries.Recipe(sim.Context,sim.State,assembler).Key;Check(recipe.Length>0,"Recipe picker configures real assembler");UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/ux-pass/machine-1080.png");
var saved=Relight.Sim.SaveSerializer.Write(sim.State,sim.Context,null,out _);var read=Relight.Sim.SaveSerializer.Read(saved,sim.Context);Check(read.Ok,"Prepared state save roundtrip");host.Attach(Relight.Sim.Simulation.Wrap(sim.Context,read.State));sim=host.Simulation;yield return null;yield return null;inv.Paint(true);Check(inv.Model.MachineId==-1,"Load clears stale machine pairing");Check(Relight.Sim.ProductionQueries.Recipe(sim.Context,sim.State,assembler).Key==recipe,"Load preserves configured recipe");
System.IO.File.WriteAllBytes("E:/Factorio2/Unity/Docs/evidence/ux-pass/prepared-inspection.json",saved);Note("SUPPORTING DONE");
}
System.Collections.IEnumerator Guard(){var stack=new System.Collections.Generic.Stack<System.Collections.IEnumerator>();stack.Push(Run());while(stack.Count>0){var e=stack.Peek();bool next=false;try{next=e.MoveNext();}catch(System.Exception ex){Note("FAIL "+ex.Message);yield break;}if(!next){stack.Pop();continue;}if(e.Current is System.Collections.IEnumerator child){stack.Push(child);continue;}yield return e.Current;}}
host.StartCoroutine(Guard());return "Supporting UI pass started (prepared state)";
