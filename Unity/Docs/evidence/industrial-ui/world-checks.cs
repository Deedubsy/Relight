var host=UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();var sim=host.Simulation;
var root=UnityEngine.Object.FindObjectsByType<UnityEngine.UIElements.UIDocument>().First(d=>d.visualTreeAsset!=null&&d.visualTreeAsset.name=="GameUI").rootVisualElement;
var inv=UnityEngine.Object.FindAnyObjectByType<Relight.UI.InventoryPanelController>();var shell=UnityEngine.Object.FindAnyObjectByType<Relight.UI.UiShell>();
var keyboard=UnityEngine.InputSystem.Keyboard.current;var mouse=UnityEngine.InputSystem.Mouse.current;
UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode=UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
UnityEngine.InputSystem.InputSystem.EnableDevice(keyboard);UnityEngine.InputSystem.InputSystem.EnableDevice(mouse);
string log="E:/Factorio2/Unity/Docs/evidence/industrial-ui/world-checks.txt";
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
var doc=UnityEngine.Object.FindObjectsByType<UnityEngine.UIElements.UIDocument>().First(d=>d.visualTreeAsset!=null&&d.visualTreeAsset.name=="GameUI");doc.panelSettings.scale=1;
var game=UnityEditor.EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView"));game.GetType().GetProperty("selectedSizeIndex",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic).SetValue(game,3);
shell.CloseActive();var read=Relight.Sim.SaveSerializer.Read(System.IO.File.ReadAllBytes("E:/Factorio2/Unity/Docs/evidence/ux-pass/normal-opening.json"),sim.Context);host.Attach(Relight.Sim.Simulation.Wrap(sim.Context,read.State));sim=host.Simulation;
yield return new UnityEngine.WaitForSecondsRealtime(.4f);UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/industrial-ui/hud-1080.png");yield return null;yield return null;
int px=(int)sim.State.Engineer.Pos.X,py=(int)sim.State.Engineer.Pos.Y,xx=-1,yy=-1;Relight.Sim.ItemId item=default;
for(int radius=0;radius<7 && xx<0;radius++)for(int y=py-radius;y<=py+radius && xx<0;y++)for(int x=px-radius;x<=px+radius && xx<0;x++)if(Relight.Sim.WorldTargetQueries.Resource(sim.Context,sim.State,x,y,out item,out _)){xx=x;yy=y;}
Check(xx>=0,"Normal opening save has nearby visible hand-minable resource");
UnityEngine.Vector2 Point(){var v=UnityEngine.Camera.main.WorldToScreenPoint(new UnityEngine.Vector3(xx+.5f,-yy-.5f,0));return new UnityEngine.Vector2(v.x,v.y);}
UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse,new UnityEngine.InputSystem.LowLevel.MouseState{position=Point()});yield return null;yield return null;yield return null;
Check(((UnityEngine.UIElements.Label)Q("target-title")).text.Contains(sim.Context.Data.Item(item).DisplayName),"Final hover labels the actual visible resource");
var before=sim.State.Engineer.Inv[item];UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse,new UnityEngine.InputSystem.LowLevel.MouseState{position=Point()}.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left));yield return new UnityEngine.WaitForSecondsRealtime(.7f);
Check(Q("world-target").worldBound.xMin >= 0 && Q("world-target").worldBound.xMax <= root.worldBound.width && Q("world-target").worldBound.yMax < Q("action-bar").worldBound.yMin,"Target label is clamped clear of dock");
Check(!Q("world-target").worldBound.Overlaps(Q("target-outline").worldBound),"Target label does not obscure its resource outline");
Check(sim.State.Engineer.Mining && sim.State.Engineer.MineProg>0,"Final mining card uses running simulation progress");UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/industrial-ui/mining-1080.png");yield return new UnityEngine.WaitForSecondsRealtime(2f);
UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse,new UnityEngine.InputSystem.LowLevel.MouseState{position=Point()});yield return null;yield return null;Check(!sim.State.Engineer.Mining && sim.State.Engineer.Inv[item]>before,"Release ends mining after real inventory gain");
UnityEngine.Camera.main.orthographicSize=14;yield return null;yield return null;UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse,new UnityEngine.InputSystem.LowLevel.MouseState{position=Point()});yield return null;yield return null;
Check(((UnityEngine.UIElements.Label)Q("target-title")).text.Contains(sim.Context.Data.Item(item).DisplayName),"Hover survives changed camera framing");UnityEngine.Camera.main.orthographicSize=10;
yield return Tap(UnityEngine.InputSystem.Key.Tab);yield return new UnityEngine.WaitForSecondsRealtime(.3f);UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/industrial-ui/backpack-1080.png");yield return null;
var scroll=(UnityEngine.UIElements.ScrollView)Q("backpack-body");Note("Scroller classes: "+string.Join(",",scroll.verticalScroller.GetClasses()));Note("Low button classes: "+string.Join(",",scroll.verticalScroller.lowButton.GetClasses()));
var goal=UnityEngine.Object.FindAnyObjectByType<Relight.UI.GoalCardController>().Model;
var objective=Relight.Sim.OpeningQueries.Objective(sim.Context,sim.State);
for(int i=0;i<objective.Materials.Count;i++){var m=objective.Materials[i];Check(goal.MaterialReady[i]==(m.Available>=m.Required),"Material tick "+i+" matches the sim");}
Check(Q("goal-card").resolvedStyle.display==UnityEngine.UIElements.DisplayStyle.Flex,"Objective remains visible beside Backpack");
Note("FINAL WORLD DONE");}
System.Collections.IEnumerator Guard(){var stack=new System.Collections.Generic.Stack<System.Collections.IEnumerator>();stack.Push(Run());while(stack.Count>0){var e=stack.Peek();bool next=false;try{next=e.MoveNext();}catch(System.Exception ex){Note("FAIL "+ex.Message);yield break;}if(!next){stack.Pop();continue;}if(e.Current is System.Collections.IEnumerator child){stack.Push(child);continue;}yield return e.Current;}}
host.StartCoroutine(Guard());return "Final weapon UI pass started";
