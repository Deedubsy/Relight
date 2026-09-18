var host=UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();var sim=host.Simulation;var st=sim.State;
var inv=UnityEngine.Object.FindAnyObjectByType<Relight.UI.InventoryPanelController>();var shell=UnityEngine.Object.FindAnyObjectByType<Relight.UI.UiShell>();var root=inv.GetComponent<UnityEngine.UIElements.UIDocument>().rootVisualElement;
var log="E:/Factorio2/Unity/Docs/evidence/inventory-drag/gestures.txt";System.IO.File.WriteAllText(log,"");
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
System.Collections.IEnumerator Run(){yield return null;yield return null;
Check(inv.Model.Store.Count==2&&inv.Model.Store.All(c=>c.Empty),"Empty generator renders both fuel slots without invented stock");
Check(inv.Model.StoreCapacity.Contains("50")&&inv.Model.StoreCapacity.Contains("shared"),"Generator displays shared fuel capacity");
var gen=st.MachineById(inv.Model.MachineId);var coal=st.Engineer.Inv[Relight.Sim.ItemId.Coal];
yield return Drag("pack-"+Pack("coal"),"store-0",true,true);
Check(gen.Inv[Relight.Sim.ItemId.Coal]==coal&&st.Engineer.Inv[Relight.Sim.ItemId.Coal]==0,"Dragging to empty coal slot transfers the whole stack");
var steel=st.Engineer.Inv[Relight.Sim.ItemId.Steel];yield return Drag("pack-"+Pack("steel"),"store-0",false);
Check(st.Engineer.Inv[Relight.Sim.ItemId.Steel]==steel&&gen.Inv[Relight.Sim.ItemId.Steel]==0,"Wrong generator input is refused without losing items");
yield return Drag("store-0","pack-5");Check(inv.Model.Pack[5].Item=="coal"&&inv.Model.Pack[5].Count==coal,"Stored fuel lands in the exact empty Backpack slot");
yield return Drag("pack-5","pack-6");Check(inv.Model.Pack[5].Empty&&inv.Model.Pack[6].Item=="coal","Backpack drag moves to the requested slot");
var copper=Pack("copper");yield return Drag("pack-6","pack-"+copper);Check(inv.Model.Pack[copper].Item=="coal"&&inv.Model.Pack[6].Item=="copper","Different Backpack stacks swap");
var item=inv.Model.Pack[copper];var split=sim.Apply(new Relight.Sim.InventorySplitCommand(copper,7,item.Item,item.Count,inv.Model.Layout,5));Check(split.Accepted,"Split setup accepted");inv.Paint(true);yield return null;
yield return Drag("pack-7","pack-"+copper);Check(inv.Model.Pack[7].Empty&&inv.Model.Pack[copper].Count==coal,"Matching Backpack stacks merge");
var from=Q<UnityEngine.UIElements.Button>("pack-"+copper);var original=inv.Model.Layout;Point(from,UnityEngine.EventType.MouseDown,from.worldBound.center);yield return null;Point(from,UnityEngine.EventType.MouseMove,from.worldBound.center+new UnityEngine.Vector2(35,20));yield return null;
Check(Relight.UI.UiDrag.Cancel(),"Escape drag-cancel hook handles the active gesture");yield return null;Check(inv.Model.Layout==original&&!Relight.UI.UiDrag.Dragging,"Cancelled drag leaves the layout unchanged");
Point(from,UnityEngine.EventType.MouseDown,from.worldBound.center);yield return null;Point(from,UnityEngine.EventType.MouseMove,from.worldBound.center+new UnityEngine.Vector2(35,20));yield return null;shell.CloseActive();yield return null;Check(!Relight.UI.UiDrag.Dragging&&Q<UnityEngine.UIElements.VisualElement>("drag-preview").resolvedStyle.display==UnityEngine.UIElements.DisplayStyle.None,"Closing the drawer cancels and hides a drag");
shell.Open("inventory-panel");inv.OpenStore(gen.Id);yield return null;
var scroll=Q<UnityEngine.UIElements.ScrollView>("pack-scroll");scroll.ScrollTo(Q<UnityEngine.UIElements.Button>("pack-39"));yield return null;yield return null;
Check(scroll.contentViewport.worldBound.Contains(Q<UnityEngine.UIElements.Button>("pack-39").worldBound.center),"Last Backpack slot is reachable by scrolling");
scroll.scrollOffset=UnityEngine.Vector2.zero;
Check(Relight.Sim.Ledger.Conservation(st,sim.Context.Data).Problems.Count==0,"All drag moves conserve inventory");
UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/inventory-drag/generator-1080.png");System.IO.File.AppendAllText(log,"DONE\n");
}
host.StartCoroutine(Run());return "Full pointer gesture checks started";
