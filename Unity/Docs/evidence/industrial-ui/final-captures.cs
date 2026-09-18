var host=UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();var sim=host.Simulation;
var root=UnityEngine.Object.FindObjectsByType<UnityEngine.UIElements.UIDocument>().First(d=>d.visualTreeAsset!=null&&d.visualTreeAsset.name=="GameUI").rootVisualElement;
var inv=UnityEngine.Object.FindAnyObjectByType<Relight.UI.InventoryPanelController>();var shell=UnityEngine.Object.FindAnyObjectByType<Relight.UI.UiShell>();
var keyboard=UnityEngine.InputSystem.Keyboard.current;var mouse=UnityEngine.InputSystem.Mouse.current;
UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode=UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
UnityEngine.InputSystem.InputSystem.EnableDevice(keyboard);UnityEngine.InputSystem.InputSystem.EnableDevice(mouse);
string log="E:/Factorio2/Unity/Docs/evidence/industrial-ui/final-captures.txt";
void Note(string s){System.IO.File.AppendAllText(log,s+"\n");}
void Check(bool c,string text){if(!c)throw new System.Exception(text);Note("PASS "+text);}
UnityEngine.UIElements.VisualElement Q(string name)=>UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.VisualElement>(root,name);
System.Collections.IEnumerator Click(UnityEngine.UIElements.VisualElement b){Check(b!=null,"Control exists: "+b?.name);using(var e=UnityEngine.UIElements.NavigationSubmitEvent.GetPooled()){e.target=b;b.SendEvent(e);}yield return new UnityEngine.WaitForSecondsRealtime(.25f);}
System.Collections.IEnumerator Tap(UnityEngine.InputSystem.Key k){UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState(k));yield return null;yield return null;UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState());yield return null;yield return null;}

System.Collections.IEnumerator Run(){

shell.CloseActive();UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.WorldInput>().ClearHand();
var doc=UnityEngine.Object.FindObjectsByType<UnityEngine.UIElements.UIDocument>().First(d=>d.visualTreeAsset!=null&&d.visualTreeAsset.name=="GameUI");
var game=UnityEditor.EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView"));
void Size(int i){game.GetType().GetProperty("selectedSizeIndex",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic).SetValue(game,i);}
Size(3);doc.panelSettings.scale=1;
var read=Relight.Sim.SaveSerializer.Read(System.IO.File.ReadAllBytes("E:/Factorio2/Unity/Docs/evidence/ux-pass/final-prepared.json"),sim.Context);host.Attach(Relight.Sim.Simulation.Wrap(sim.Context,read.State));sim=host.Simulation;yield return new UnityEngine.WaitForSecondsRealtime(.3f);
yield return Click(Q("open-inventory"));((UnityEngine.UIElements.Foldout)Q("workshop-foldout")).value=true;yield return new UnityEngine.WaitForSecondsRealtime(.3f);
var scroll=(UnityEngine.UIElements.ScrollView)Q("backpack-body");Check(scroll.horizontalScrollerVisibility==UnityEngine.UIElements.ScrollerVisibility.Hidden,"Backpack is explicitly vertical-only");
var core=UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.VisualElement>(Q("card-core"),"card-output-icon");Check(core.resolvedStyle.backgroundImage.sprite.name=="core1","Final painted core icon rendered");
UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/industrial-ui/workshop-1080.png");yield return null;yield return null;
Size(9);doc.panelSettings.scale=1.25f;yield return new UnityEngine.WaitForSecondsRealtime(.4f);yield return Click(Q("jump-workshop"));
UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/industrial-ui/workshop-720-scale125.png");yield return null;
((UnityEngine.UIElements.ScrollView)Q("backpack-body")).ScrollTo(Q("card-rifle"));yield return new UnityEngine.WaitForSecondsRealtime(.25f);
var rifle=UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.Button>(Q("card-rifle"),"card-craft");Check(scroll.contentViewport.worldBound.Contains(rifle.worldBound.center),"Last recipe remains reachable by scrolling at 720p / 125 percent");
UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/industrial-ui/workshop-last-recipe-720-scale125.png");yield return null;
yield return Click(Q("jump-workshop"));shell.CloseActive();doc.panelSettings.scale=1;yield return new UnityEngine.WaitForSecondsRealtime(.3f);
yield return Tap(UnityEngine.InputSystem.Key.B);yield return Click(Q("card-excavator"));yield return new UnityEngine.WaitForSecondsRealtime(.3f);
UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/industrial-ui/placement-720.png");yield return null;
yield return Tap(UnityEngine.InputSystem.Key.Escape);Size(3);yield return new UnityEngine.WaitForSecondsRealtime(.3f);
var machine=sim.State.Machines.First(m=>m.Kind=="assembler");var v=UnityEngine.Camera.main.WorldToScreenPoint(new UnityEngine.Vector3(machine.X+.5f,-machine.Y-.5f,0));UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse,new UnityEngine.InputSystem.LowLevel.MouseState{position=new UnityEngine.Vector2(v.x,v.y)});yield return null;yield return null;yield return Tap(UnityEngine.InputSystem.Key.E);yield return new UnityEngine.WaitForSecondsRealtime(.3f);
Check(inv.Model.MachineId==machine.Id,"Machine inspection still opens the actual assembler");UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/industrial-ui/machine-1080.png");yield return null;
Note("FINAL CAPTURES DONE; scale 100 percent and 1080p restored");}
System.Collections.IEnumerator Guard(){var stack=new System.Collections.Generic.Stack<System.Collections.IEnumerator>();stack.Push(Run());while(stack.Count>0){var e=stack.Peek();bool next=false;try{next=e.MoveNext();}catch(System.Exception ex){Note("FAIL "+ex.Message);yield break;}if(!next){stack.Pop();continue;}if(e.Current is System.Collections.IEnumerator child){stack.Push(child);continue;}yield return e.Current;}}
host.StartCoroutine(Guard());return "Final weapon UI pass started";
