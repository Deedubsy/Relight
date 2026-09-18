var host=UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();var sim=host.Simulation;
var root=UnityEngine.Object.FindObjectsByType<UnityEngine.UIElements.UIDocument>().First(d=>d.visualTreeAsset!=null&&d.visualTreeAsset.name=="GameUI").rootVisualElement;
var inv=UnityEngine.Object.FindAnyObjectByType<Relight.UI.InventoryPanelController>();var shell=UnityEngine.Object.FindAnyObjectByType<Relight.UI.UiShell>();
var keyboard=UnityEngine.InputSystem.Keyboard.current;var mouse=UnityEngine.InputSystem.Mouse.current;
UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode=UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
UnityEngine.InputSystem.InputSystem.EnableDevice(keyboard);UnityEngine.InputSystem.InputSystem.EnableDevice(mouse);
string log="E:/Factorio2/Unity/Docs/evidence/ux-pass/menu-log.txt";
void Note(string s){System.IO.File.AppendAllText(log,s+"\n");}
void Check(bool c,string text){if(!c)throw new System.Exception(text);Note("PASS "+text);}
UnityEngine.UIElements.VisualElement Q(string name)=>UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.VisualElement>(root,name);
System.Collections.IEnumerator Click(UnityEngine.UIElements.VisualElement b){Check(b!=null,"Control exists: "+b?.name);using(var e=UnityEngine.UIElements.NavigationSubmitEvent.GetPooled()){e.target=b;b.SendEvent(e);}yield return new UnityEngine.WaitForSecondsRealtime(.25f);}
System.Collections.IEnumerator Tap(UnityEngine.InputSystem.Key k){UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState(k));yield return null;yield return null;UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState());yield return null;yield return null;}

System.Collections.IEnumerator Run(){
Relight.UI.Tooltips.HideNow();
var barBefore=sim.State.Weapons.Bar.ToArray();var steel=sim.State.Engineer.Inv[Relight.Sim.ItemId.Steel];
var build=UnityEngine.Object.FindAnyObjectByType<Relight.UI.BuildPanelController>();Check(build.Assigning,"Build starts HUD assignment mode");yield return Click(Q("hud-slot-0"));
Check(sim.State.Weapons.Bar[0]=="excavator" && sim.State.Weapons.Bar[1]==barBefore[0],"HUD assignment swaps existing shortcut without duplicates");Check(steel==sim.State.Engineer.Inv[Relight.Sim.ItemId.Steel],"Shortcut assignment consumes no materials");
UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/ux-pass/final-build-720-scale125.png");yield return null;
shell.CloseActive();for(int i=0;i<3 && !host.Paused;i++)yield return Tap(UnityEngine.InputSystem.Key.Escape);Check(host.Paused,"Escape reaches Pause after clearing the tool");
var pause=UnityEngine.Object.FindObjectsByType<UnityEngine.UIElements.UIDocument>().First(d=>UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.VisualElement>(d.rootVisualElement,"pause-save")!=null).rootVisualElement;
UnityEngine.UIElements.VisualElement P(string name)=>UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.VisualElement>(pause,name);
yield return Click(P("pause-save"));((UnityEngine.UIElements.TextField)P("save-name")).Focus();((UnityEngine.UIElements.TextField)P("save-name")).value="UI inspection 123";
var hand=UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.WorldInput>().SelectedBarSlot;
yield return Tap(UnityEngine.InputSystem.Key.Digit1);yield return Tap(UnityEngine.InputSystem.Key.R);Check(UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.WorldInput>().SelectedBarSlot==hand,"Typing digits or R in paused Save does not select tools");
UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/ux-pass/final-save-720-scale125.png");yield return null;yield return Click(P("save-back"));yield return Click(P("pause-load"));
UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/ux-pass/final-load-720-scale125.png");yield return null;yield return Click(P("load-back"));yield return Click(P("pause-settings"));
Check(!P("master-slider").enabledInHierarchy,"Unavailable audio controls are visibly disabled");UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/ux-pass/final-settings-720-scale125.png");yield return null;yield return Click(P("settings-back"));yield return Click(P("pause-resume"));Check(!host.Paused,"Resume returns to gameplay");
var n=new Relight.Sim.UI.HudNotices();n.Post("danger","Danger",Relight.Sim.UI.HudNoticeKind.Danger,0);for(int i=0;i<6;i++)n.Post("info"+i,"Routine",Relight.Sim.UI.HudNoticeKind.Info,0);Check(n.Rows.Count==3 && n.Rows[0].Key=="danger","Danger remains visible during routine notification burst");Note("MENU CHECKS DONE");}
System.Collections.IEnumerator Guard(){var stack=new System.Collections.Generic.Stack<System.Collections.IEnumerator>();stack.Push(Run());while(stack.Count>0){var e=stack.Peek();bool next=false;try{next=e.MoveNext();}catch(System.Exception ex){Note("FAIL "+ex.Message);yield break;}if(!next){stack.Pop();continue;}if(e.Current is System.Collections.IEnumerator child){stack.Push(child);continue;}yield return e.Current;}}
host.StartCoroutine(Guard());return "Final weapon UI pass started";
