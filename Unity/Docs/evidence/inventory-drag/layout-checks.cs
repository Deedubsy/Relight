var host=UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();var sim=host.Simulation;var st=sim.State;
var inv=UnityEngine.Object.FindAnyObjectByType<Relight.UI.InventoryPanelController>();var shell=UnityEngine.Object.FindAnyObjectByType<Relight.UI.UiShell>();var root=inv.GetComponent<UnityEngine.UIElements.UIDocument>().rootVisualElement;
var log="E:/Factorio2/Unity/Docs/evidence/inventory-drag/layout.txt";System.IO.File.WriteAllText(log,"");
T Q<T>(string name)where T:UnityEngine.UIElements.VisualElement=>UnityEngine.UIElements.UQueryExtensions.Q<T>(root,name);
void Check(bool ok,string text){System.IO.File.AppendAllText(log,(ok?"PASS ":"FAIL ")+text+"\n");if(!ok)throw new System.Exception(text);}
int Pack(string item)=>Enumerable.Range(0,inv.Model.Pack.Count).First(i=>inv.Model.Pack[i].Item==item);
void Point(UnityEngine.UIElements.VisualElement source,UnityEngine.EventType type,UnityEngine.Vector2 p){var raw=new UnityEngine.Event{type=type,mousePosition=p,button=0};
if(type==UnityEngine.EventType.MouseDown){using(var e=UnityEngine.UIElements.PointerDownEvent.GetPooled(raw)){e.target=source;source.SendEvent(e);}}
else if(type==UnityEngine.EventType.MouseUp){using(var e=UnityEngine.UIElements.PointerUpEvent.GetPooled(raw)){e.target=source;source.SendEvent(e);}}
else{using(var e=UnityEngine.UIElements.PointerMoveEvent.GetPooled(raw)){e.target=source;source.SendEvent(e);}}}
System.Collections.IEnumerator Drag(string source,string target,bool valid=true,bool capture=false){
var from=Q<UnityEngine.UIElements.Button>(source);var to=Q<UnityEngine.UIElements.Button>(target);var a=from.worldBound.center;var b=to.worldBound.center;
Check(root.panel.Pick(a)!=null&&root.panel.Pick(b)!=null,"Both pointer endpoints have visible UI hit targets");
Point(from,UnityEngine.EventType.MouseDown,a);yield return null;Point(from,UnityEngine.EventType.MouseMove,UnityEngine.Vector2.Lerp(a,b,.5f));yield return null;Point(from,UnityEngine.EventType.MouseMove,b);yield return null;
var ghost=Q<UnityEngine.UIElements.VisualElement>("drag-preview");
Check(Relight.UI.UiDrag.Dragging&&ghost.resolvedStyle.display==UnityEngine.UIElements.DisplayStyle.Flex,"Pointer sweep starts drag and shows preview");
Check(root.worldBound.Contains(ghost.worldBound.min)&&root.worldBound.Contains(ghost.worldBound.max),"Drag preview stays inside the viewport");
Check(to.ClassListContains(valid?"drop-target":"drop-invalid"),valid?"Valid destination is highlighted":"Invalid destination is marked red");
if(capture){UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/inventory-drag/drag-preview-1080.png");yield return null;yield return null;}
Point(from,UnityEngine.EventType.MouseUp,b);yield return null;yield return null;Check(!Relight.UI.UiDrag.Dragging&&ghost.resolvedStyle.display==UnityEngine.UIElements.DisplayStyle.None,"Release finishes drag and hides preview");yield return new UnityEngine.WaitForSecondsRealtime(.27f);
}
System.Collections.IEnumerator Run(){
var game=UnityEditor.EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView"));var size=game.GetType().GetProperty("selectedSizeIndex",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic);size.SetValue(game,9);
var gen=st.MachineById(inv.Model.MachineId);sim.Context.Data.TryMachine("assembler",out var spec);var assembler=new Relight.Sim.Machine{Id=st.NextId++,Kind="assembler",X=72,Y=360,Size=spec.Size};st.Machines.Add(assembler);st.Rev++;
inv.OpenStore(assembler.Id);Q<UnityEngine.UIElements.ScrollView>("backpack-body").scrollOffset=UnityEngine.Vector2.zero;yield return new UnityEngine.WaitForSecondsRealtime(.3f);
var body=Q<UnityEngine.UIElements.ScrollView>("backpack-body");var slot=Q<UnityEngine.UIElements.Button>("store-0");
Check(body.contentViewport.worldBound.Contains(slot.worldBound.min)&&body.contentViewport.worldBound.Contains(slot.worldBound.max),"Processor's empty input slot is fully visible immediately at 720p");
var pack=Q<UnityEngine.UIElements.Button>("pack-0");Check(body.contentViewport.worldBound.Contains(pack.worldBound.min)&&body.contentViewport.worldBound.Contains(pack.worldBound.max),"Backpack's first slot is fully visible at 720p");
yield return Drag("pack-"+Pack("steel"),"store-0");Check(assembler.Inv[Relight.Sim.ItemId.Steel]>0,"Processor drop works with inventories-first layout at 720p");
UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/inventory-drag/processor-720.png");yield return null;yield return null;
body.ScrollTo(Q<UnityEngine.UIElements.Button>("apply-recipe"));yield return null;yield return null;Check(body.contentViewport.worldBound.Contains(Q<UnityEngine.UIElements.Button>("apply-recipe").worldBound.center),"Recipe controls remain reachable by scrolling");
body.scrollOffset=UnityEngine.Vector2.zero;inv.OpenStore(gen.Id);yield return null;yield return null;
yield return Drag("pack-"+Pack("coal"),"store-0");Check(gen.Inv[Relight.Sim.ItemId.Coal]>0,"Generator fuel drag works at 720p");
UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/inventory-drag/generator-720.png");yield return null;yield return null;size.SetValue(game,3);yield return null;yield return null;
host.Paused=true;yield return null;var session=UnityEngine.Object.FindAnyObjectByType<Relight.UI.FrontEnd.FrontEndBootstrap>();session.Dirty.Saved(host.TotalTicks,"test only");var ticks=host.TotalTicks;
inv.DragTo(Relight.UI.InventoryPanelController.SlotKind.Pack,5,inv.DragFrom(Relight.UI.InventoryPanelController.SlotKind.Store,0));
Check(host.TotalTicks==ticks&&session.Dirty.IsDirty(ticks),"A paused inventory transfer marks unsaved progress without a tick");host.Paused=false;
Check(Relight.Sim.Ledger.Conservation(st,sim.Context.Data).Problems.Count==0,"Final layout transfers conserve inventory");System.IO.File.AppendAllText(log,"DONE\n");
}
host.StartCoroutine(Run());return "Final 720p layout checks started";
