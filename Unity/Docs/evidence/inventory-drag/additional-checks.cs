var host=UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();var sim=host.Simulation;var st=sim.State;
var inv=UnityEngine.Object.FindAnyObjectByType<Relight.UI.InventoryPanelController>();var shell=UnityEngine.Object.FindAnyObjectByType<Relight.UI.UiShell>();var root=inv.GetComponent<UnityEngine.UIElements.UIDocument>().rootVisualElement;
var log="E:/Factorio2/Unity/Docs/evidence/inventory-drag/additional.txt";System.IO.File.WriteAllText(log,"");
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
var gen=st.MachineById(inv.Model.MachineId);var n=st.Engineer.Inv[Relight.Sim.ItemId.Coal];
yield return Drag("pack-"+Pack("coal"),"store-0");
Q<UnityEngine.UIElements.ScrollView>("pack-scroll").ScrollTo(Q<UnityEngine.UIElements.Button>("pack-39"));yield return null;yield return null;
yield return Drag("store-0","pack-39");Check(inv.Model.Pack[39].Item=="coal"&&inv.Model.Pack[39].Count==n,"Real drop targets the final scrolled Backpack slot");
var button=Q<UnityEngine.UIElements.Button>("pack-39");using(var e=UnityEngine.UIElements.ClickEvent.GetPooled(new UnityEngine.Event{type=UnityEngine.EventType.MouseUp,button=0,modifiers=UnityEngine.EventModifiers.Shift,mousePosition=button.worldBound.center})){e.target=button;button.SendEvent(e);}yield return null;yield return null;
Check(gen.Inv[Relight.Sim.ItemId.Coal]==n&&inv.Model.Pack[39].Empty,"Shift-click transfers the selected stack into the machine");
Q<UnityEngine.UIElements.ScrollView>("pack-scroll").scrollOffset=UnityEngine.Vector2.zero;yield return null;yield return null;
yield return Drag("store-0","pack-0");
Relight.Sim.Machine Add(string kind){sim.Context.Data.TryMachine(kind,out var spec);var m=new Relight.Sim.Machine{Id=st.NextId++,Kind=kind,X=72,Y=360,Size=spec.Size};st.Machines.Add(m);st.Rev++;return m;}
var chest=Add("chest");inv.OpenStore(chest.Id);yield return null;yield return null;
Check(inv.Model.Store.Count>=10&&inv.Model.Store.All(c=>c.Empty),"Empty chest has a visible grid of open slots");
yield return Drag("pack-"+Pack("steel"),"store-4");Check(chest.Inv[Relight.Sim.ItemId.Steel]>0,"Dropping on an empty chest slot stores items");
var assembler=Add("assembler");inv.OpenStore(assembler.Id);yield return null;yield return null;
Check(inv.Model.Store.Any(c=>c.Role=="Input")&&inv.Model.Store.Any(c=>c.Role=="Output"),"Empty processor renders recipe input and output slots");
Check(inv.Model.Store.Where(c=>c.Role=="Output").All(c=>!c.CanLoad),"Processor outputs are take-only");
var recipe=Relight.Sim.ProductionRules.RecipeOf(sim.Context.Data,st,assembler);var ingredient=Relight.Sim.Items.Key(recipe.Inputs[0].Item);sim.Apply(new Relight.Sim.AdminCommand("grant",ingredient,10));inv.Paint(true);yield return null;
var input=Enumerable.Range(0,inv.Model.Store.Count).First(i=>inv.Model.Store[i].FilterItem==ingredient);yield return Drag("pack-"+Pack(ingredient),"store-"+input);
Check(assembler.Inv[recipe.Inputs[0].Item]>0,"Dragging a recipe ingredient fills its input buffer");
var output=Enumerable.Range(0,inv.Model.Store.Count).First(i=>inv.Model.Store[i].Role=="Output");
if(!inv.Model.Pack.Any(c=>c.Item==ingredient)){sim.Apply(new Relight.Sim.AdminCommand("grant",ingredient,3));inv.Paint(true);yield return null;}
yield return Drag("pack-"+Pack(ingredient),"store-"+output,false);
var game=UnityEditor.EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView"));var prop=game.GetType().GetProperty("selectedSizeIndex",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic);prop.SetValue(game,9);yield return new UnityEngine.WaitForSecondsRealtime(.3f);
UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/inventory-drag/processor-720.png");yield return null;yield return null;prop.SetValue(game,3);
yield return null;yield return null;
sim.Apply(new Relight.Sim.AdminCommand("grant","belt",2));inv.Paint(true);yield return null;
yield return Drag("pack-"+Pack("belt"),"hud-slot-9");Check(Relight.Sim.WeaponQueries.Bar(st)[9]=="belt","Packed structures drag onto the hotbar as shortcuts");
Check(Relight.Sim.Ledger.Conservation(st,sim.Context.Data).Problems.Count==0,"Additional storage and processor transfers conserve inventory");
System.IO.File.AppendAllText(log,"DONE\n");
}
host.StartCoroutine(Run());return "Additional inventory checks started";
