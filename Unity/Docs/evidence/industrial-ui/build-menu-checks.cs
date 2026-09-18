var host=UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();var sim=host.Simulation;
var root=UnityEngine.Object.FindObjectsByType<UnityEngine.UIElements.UIDocument>().First(d=>d.visualTreeAsset!=null&&d.visualTreeAsset.name=="GameUI").rootVisualElement;
var inv=UnityEngine.Object.FindAnyObjectByType<Relight.UI.InventoryPanelController>();var shell=UnityEngine.Object.FindAnyObjectByType<Relight.UI.UiShell>();
var keyboard=UnityEngine.InputSystem.Keyboard.current;var mouse=UnityEngine.InputSystem.Mouse.current;
UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode=UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
UnityEngine.InputSystem.InputSystem.EnableDevice(keyboard);UnityEngine.InputSystem.InputSystem.EnableDevice(mouse);
string log="E:/Factorio2/Unity/Docs/evidence/industrial-ui/build-menu-checks.txt";
void Note(string s){System.IO.File.AppendAllText(log,s+"\n");}
void Check(bool c,string text){if(!c)throw new System.Exception(text);Note("PASS "+text);}
UnityEngine.UIElements.VisualElement Q(string name)=>UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.VisualElement>(root,name);
System.Collections.IEnumerator Click(UnityEngine.UIElements.VisualElement b){Check(b!=null,"Control exists: "+b?.name);using(var e=UnityEngine.UIElements.NavigationSubmitEvent.GetPooled()){e.target=b;b.SendEvent(e);}yield return new UnityEngine.WaitForSecondsRealtime(.25f);}
System.Collections.IEnumerator Tap(UnityEngine.InputSystem.Key k){UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState(k));yield return null;yield return null;UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState());yield return null;yield return null;}

System.Collections.IEnumerator Run(){
shell.CloseActive();var input=UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.WorldInput>();input.ClearHand();yield return new UnityEngine.WaitForSecondsRealtime(.2f);
host.Attach(Relight.Sim.Simulation.NewGame(sim.Context,1));sim=host.Simulation;yield return new UnityEngine.WaitForSecondsRealtime(.3f);
var start=sim.State.Engineer.Pos;
foreach(var key in new[]{UnityEngine.InputSystem.Key.S,UnityEngine.InputSystem.Key.A,UnityEngine.InputSystem.Key.W,UnityEngine.InputSystem.Key.D}){
UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState(key));yield return new UnityEngine.WaitForSecondsRealtime(.3f);UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState());yield return null;
if(sim.State.Engineer.Pos.X!=start.X || sim.State.Engineer.Pos.Y!=start.Y)break;
}
Check(sim.State.Engineer.Pos.X!=start.X || sim.State.Engineer.Pos.Y!=start.Y,"Held keyboard movement moves engineer on a clear direction");
var prepared=Relight.Sim.SaveSerializer.Read(System.IO.File.ReadAllBytes("E:/Factorio2/Unity/Docs/evidence/ux-pass/final-prepared.json"),sim.Context);host.Attach(Relight.Sim.Simulation.Wrap(sim.Context,prepared.State));sim=host.Simulation;yield return new UnityEngine.WaitForSecondsRealtime(.3f);
yield return Tap(UnityEngine.InputSystem.Key.B);yield return new UnityEngine.WaitForSecondsRealtime(.3f);Check(shell.Active=="build-panel","B opens Build");
var build=UnityEngine.Object.FindAnyObjectByType<Relight.UI.BuildPanelController>();var selected=build.Selected;var bar=Relight.Sim.WeaponQueries.Bar(sim.State).ToArray();var steel=sim.State.Engineer.Inv[Relight.Sim.ItemId.Steel];
yield return Click(Q("add-to-bar"));Check(build.Assigning,"Build enables dock assignment");yield return Click(Q("hud-slot-0"));Check(Relight.Sim.WeaponQueries.Bar(sim.State)[0]==selected && sim.State.Engineer.Inv[Relight.Sim.ItemId.Steel]==steel,"Assigning selected structure changes shortcut without spending stock");
UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/industrial-ui/build-720.png");yield return null;
yield return Click(Q("card-"+selected));Check(input.PlacementActive && string.IsNullOrEmpty(shell.Active),"Build card enters placement with drawer closed");
var mouse=UnityEngine.InputSystem.Mouse.current;UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse,new UnityEngine.InputSystem.LowLevel.MouseState{position=new UnityEngine.Vector2(UnityEngine.Screen.width*.48f,UnityEngine.Screen.height*.5f)});yield return new UnityEngine.WaitForSecondsRealtime(.3f);
Check(!Q("placement-toolbar").ClassListContains("is-hidden"),"Placement toolbar appears");UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/industrial-ui/placement-720.png");yield return null;
yield return Tap(UnityEngine.InputSystem.Key.Escape);Check(!input.PlacementActive,"Escape cancels placement");yield return Tap(UnityEngine.InputSystem.Key.Escape);Check(host.Paused,"Next Escape pauses");
var pause=UnityEngine.Object.FindObjectsByType<UnityEngine.UIElements.UIDocument>().First(d=>UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.VisualElement>(d.rootVisualElement,"pause-save")!=null).rootVisualElement;
UnityEngine.UIElements.VisualElement P(string name)=>UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.VisualElement>(pause,name);
yield return Click(P("pause-load"));UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/industrial-ui/load-720.png");yield return null;yield return Click(P("load-back"));
yield return Click(P("pause-resume"));Check(!host.Paused,"Resume returns to world");
Note("BUILD AND MENU CHECKS DONE");}
System.Collections.IEnumerator Guard(){var stack=new System.Collections.Generic.Stack<System.Collections.IEnumerator>();stack.Push(Run());while(stack.Count>0){var e=stack.Peek();bool next=false;try{next=e.MoveNext();}catch(System.Exception ex){Note("FAIL "+ex.Message);yield break;}if(!next){stack.Pop();continue;}if(e.Current is System.Collections.IEnumerator child){stack.Push(child);continue;}yield return e.Current;}}
host.StartCoroutine(Guard());return "Final weapon UI pass started";
