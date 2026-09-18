var host=UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();host.enabled=false;host.Paused=false;
var sim=host.Simulation;var st=sim.State;var ctx=sim.Context;
void Mine(int x,int y,Relight.Sim.ItemId item){st.Engineer.Pos=new Relight.Sim.Vec2(x-.5,y+.5);var r=sim.Apply(new Relight.Sim.MineCommand(x,y));if(!r.Accepted)throw new System.Exception(r.Problem);for(var i=0;i<200;i++)sim.Tick();sim.Apply(new Relight.Sim.StopMiningCommand());}
Mine(84,352,Relight.Sim.ItemId.IronOre);Mine(96,352,Relight.Sim.ItemId.CopperOre);st.Engineer.Pos=ctx.Geometry.Spawn;
var inv=UnityEngine.Object.FindAnyObjectByType<Relight.UI.InventoryPanelController>();var shell=UnityEngine.Object.FindAnyObjectByType<Relight.UI.UiShell>();var root=inv.GetComponent<UnityEngine.UIElements.UIDocument>().rootVisualElement;
shell.Open("inventory-panel");inv.GetType().GetMethod("SetWorkshopView",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(inv,new object[]{true});
var log="E:/Factorio2/Unity/Docs/evidence/ore-opening/workshop-ui.txt";System.IO.File.WriteAllText(log,"");
void Check(bool ok,string message){System.IO.File.AppendAllText(log,(ok?"PASS ":"FAIL ")+message+"\n");if(!ok)throw new System.Exception(message);}
UnityEngine.UIElements.VisualElement Q(UnityEngine.UIElements.VisualElement e,string name)=>UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.VisualElement>(e,name);
void Point(UnityEngine.UIElements.VisualElement el,bool down){var raw=new UnityEngine.Event{type=down?UnityEngine.EventType.MouseDown:UnityEngine.EventType.MouseUp,mousePosition=el.worldBound.center,button=0};if(down){using(var e=UnityEngine.UIElements.PointerDownEvent.GetPooled(raw)){e.target=el;el.SendEvent(e);}}else{using(var e=UnityEngine.UIElements.PointerUpEvent.GetPooled(raw)){e.target=el;el.SendEvent(e);}}}
System.Collections.IEnumerator Go(){
 yield return null;yield return null;
 var steel=Q(root,"card-hand-steel");var copper=Q(root,"card-hand-copper");Check(steel!=null&&copper!=null,"Both smelting cards exist in live Workshop");
 var button=Q(steel,"craft-five");Check(button!=null && button.enabledInHierarchy,"Steel batch button enabled with mined ore");
 var body=UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.ScrollView>(root,"backpack-body");body.ScrollTo(steel);yield return null;
 var session=UnityEngine.Object.FindAnyObjectByType<Relight.UI.FrontEnd.FrontEndBootstrap>();session.Dirty.Saved(host.TotalTicks,"UI verification only");
 Point(button,true);yield return null;Point(button,false);yield return null;
 Check(st.Hand.RecipeKey=="hand-steel" && st.Hand.Crafts==5,"Actual UI pointer click queues five Steel batches");
 Check(session.Dirty.IsDirty(host.TotalTicks),"Workshop action marks unsaved progress");
 for(var i=0;i<40;i++)sim.Tick();yield return new UnityEngine.WaitForSecondsRealtime(.3f);
 UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/ore-opening/workshop.png");yield return null;
 for(var i=0;i<360;i++)sim.Tick();yield return null;
 Check(st.Stats.Made[Relight.Sim.ItemId.Steel]==5 && st.Engineer.Inv[Relight.Sim.ItemId.IronOre]==0,"Five Steel plates produced from five Iron ore");
 Check(st.Engineer.Inv[Relight.Sim.ItemId.Magazine]==0,"Smelting did not accidentally produce bullets");
 body.ScrollTo(copper);yield return new UnityEngine.WaitForSecondsRealtime(.3f);var cu=Q(copper,"card-craft");Check(cu.enabledInHierarchy,"Copper button refreshed after Steel queue finished");Point(cu,true);yield return null;Point(cu,false);yield return null;
 Check(st.Hand.RecipeKey=="hand-copper" && st.Hand.Crafts==1,"Copper card selects Copper recipe");
 for(var i=0;i<80;i++)sim.Tick();
 Check(st.Stats.Made[Relight.Sim.ItemId.Copper]==1,"Copper plate reaches Backpack");
 Check(Relight.Sim.Ledger.Conservation(st,ctx.Data).Ok,"Live UI mining and smelting conserve items");
 System.IO.File.AppendAllText(log,"DONE. Synthetic UI Toolkit pointer events on live buttons; no native mouse acceptance claimed.\n");
}
host.StartCoroutine(Go());return "Workshop pointer checks running";
