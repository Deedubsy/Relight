var host=UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();var sim=host.Simulation;
var root=UnityEngine.Object.FindObjectsByType<UnityEngine.UIElements.UIDocument>().First(d=>d.visualTreeAsset!=null&&d.visualTreeAsset.name=="GameUI").rootVisualElement;
var inv=UnityEngine.Object.FindAnyObjectByType<Relight.UI.InventoryPanelController>();var shell=UnityEngine.Object.FindAnyObjectByType<Relight.UI.UiShell>();
var keyboard=UnityEngine.InputSystem.Keyboard.current;var mouse=UnityEngine.InputSystem.Mouse.current;
UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode=UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
UnityEngine.InputSystem.InputSystem.EnableDevice(keyboard);UnityEngine.InputSystem.InputSystem.EnableDevice(mouse);
string log="E:/Factorio2/Unity/Docs/evidence/industrial-ui/layout-checks.txt";
void Note(string s){System.IO.File.AppendAllText(log,s+"\n");}
void Check(bool c,string text){if(!c)throw new System.Exception(text);Note("PASS "+text);}
UnityEngine.UIElements.VisualElement Q(string name)=>UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.VisualElement>(root,name);
System.Collections.IEnumerator Click(UnityEngine.UIElements.VisualElement b){Check(b!=null,"Control exists: "+b?.name);using(var e=UnityEngine.UIElements.NavigationSubmitEvent.GetPooled()){e.target=b;b.SendEvent(e);}yield return new UnityEngine.WaitForSecondsRealtime(.25f);}
System.Collections.IEnumerator Tap(UnityEngine.InputSystem.Key k){UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState(k));yield return null;yield return null;UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState());yield return null;yield return null;}

System.Collections.IEnumerator Run(){
shell.CloseActive();UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.WorldInput>().ClearHand();
var window=UnityEditor.EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView"));window.GetType().GetProperty("selectedSizeIndex",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic).SetValue(window,9);
yield return new UnityEngine.WaitForSecondsRealtime(.4f);
UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/industrial-ui/hud-720.png");yield return null;
yield return Tap(UnityEngine.InputSystem.Key.Tab);yield return new UnityEngine.WaitForSecondsRealtime(.3f);
UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/industrial-ui/backpack-720.png");yield return null;
Check(Q("inventory-panel").worldBound.yMax < Q("action-bar").worldBound.yMin,"720p drawer stays above interactive dock");
shell.CloseActive();for(int i=0;i<3 && !host.Paused;i++)yield return Tap(UnityEngine.InputSystem.Key.Escape);
var game=UnityEditor.EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView"));game.GetType().GetProperty("selectedSizeIndex",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic).SetValue(game,9);
var pause=UnityEngine.Object.FindObjectsByType<UnityEngine.UIElements.UIDocument>().First(d=>UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.VisualElement>(d.rootVisualElement,"pause-save")!=null).rootVisualElement;
UnityEngine.UIElements.VisualElement P(string name)=>UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.VisualElement>(pause,name);
yield return Click(P("pause-settings"));var scale=(UnityEngine.UIElements.DropdownField)P("scale-field");Note("Scale choices "+string.Join(",",scale.choices));scale.index=1;yield return new UnityEngine.WaitForSecondsRealtime(.4f);
Check(UnityEngine.Object.FindObjectsByType<UnityEngine.UIElements.UIDocument>().First(d=>d.visualTreeAsset!=null&&d.visualTreeAsset.name=="GameUI").panelSettings.scale==1.25f,"Settings applies actual 125 percent panel scale");UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/industrial-ui/settings-720-scale125.png");yield return null;yield return Click(P("settings-back"));yield return Click(P("pause-save"));((UnityEngine.UIElements.TextField)P("save-name")).value="UI inspection 123";
UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/industrial-ui/save-720-scale125.png");yield return null;yield return Click(P("save-back"));yield return Click(P("pause-resume"));
var read=Relight.Sim.SaveSerializer.Read(System.IO.File.ReadAllBytes("E:/Factorio2/Unity/Docs/evidence/ux-pass/final-prepared.json"),sim.Context);host.Attach(Relight.Sim.Simulation.Wrap(sim.Context,read.State));sim=host.Simulation;
yield return null;yield return Tap(UnityEngine.InputSystem.Key.Tab);yield return Click(Q("jump-workshop"));yield return new UnityEngine.WaitForSecondsRealtime(.3f);
UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/industrial-ui/workshop-720-scale125.png");yield return null;
var body=(UnityEngine.UIElements.ScrollView)Q("backpack-body");var craft=UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.Button>(Q("card-hand-bullets"),"card-craft");Check(body.contentViewport.worldBound.Contains(craft.worldBound.center),"Craft button visible without scrolling at 720p / 125 percent");
yield return Click(Q("jump-workshop"));UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/industrial-ui/inventory-720-scale125.png");yield return null;
Check(Q("inventory-panel").worldBound.xMin > Q("goal-card").worldBound.xMax,"125 percent Backpack does not cover objective");
Check(Q("inventory-panel").worldBound.yMax < Q("action-bar").worldBound.yMin,"125 percent Backpack does not cover dock");
Check(!Q("inventory-panel").ClassListContains("workshop-view"),"Backpack button returns from focused Workshop");shell.CloseActive();yield return null;
var target=Q("hud-slot-9");using(var e=UnityEngine.UIElements.PointerEnterEvent.GetPooled()){e.target=target;target.SendEvent(e);}yield return new UnityEngine.WaitForSecondsRealtime(.4f);UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/industrial-ui/tooltip-720-scale125.png");yield return null;
Relight.UI.Tooltips.HideNow();yield return Tap(UnityEngine.InputSystem.Key.Escape);yield return Click(P("pause-settings"));scale=(UnityEngine.UIElements.DropdownField)P("scale-field");scale.index=0;yield return Click(P("settings-back"));yield return Click(P("pause-resume"));
Note("FINAL LAYOUT DONE; interface scale restored to 100 percent");}
System.Collections.IEnumerator Guard(){var stack=new System.Collections.Generic.Stack<System.Collections.IEnumerator>();stack.Push(Run());while(stack.Count>0){var e=stack.Peek();bool next=false;try{next=e.MoveNext();}catch(System.Exception ex){Note("FAIL "+ex.Message);yield break;}if(!next){stack.Pop();continue;}if(e.Current is System.Collections.IEnumerator child){stack.Push(child);continue;}yield return e.Current;}}
host.StartCoroutine(Guard());return "Final weapon UI pass started";
