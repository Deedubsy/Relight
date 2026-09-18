var host=UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();var sim=host.Simulation;
var root=UnityEngine.Object.FindObjectsByType<UnityEngine.UIElements.UIDocument>().First(d=>d.visualTreeAsset!=null&&d.visualTreeAsset.name=="GameUI").rootVisualElement;
var inv=UnityEngine.Object.FindAnyObjectByType<Relight.UI.InventoryPanelController>();var shell=UnityEngine.Object.FindAnyObjectByType<Relight.UI.UiShell>();
var keyboard=UnityEngine.InputSystem.Keyboard.current;var mouse=UnityEngine.InputSystem.Mouse.current;
UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode=UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
UnityEngine.InputSystem.InputSystem.EnableDevice(keyboard);UnityEngine.InputSystem.InputSystem.EnableDevice(mouse);
string log="E:/Factorio2/Unity/Docs/evidence/ux-pass/final-input-log.txt";
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
var before=Relight.Sim.WeaponQueries.Equipped(sim.State,sim.Context.Data).Loaded;
yield return Click(Q("open-inventory"));Check(shell.Active=="inventory-panel","HUD Backpack button opens drawer");Check(before==Relight.Sim.WeaponQueries.Equipped(sim.State,sim.Context.Data).Loaded,"UI click does not fire rifle");
UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/ux-pass/final-inventory-1080.png");yield return null;
System.IO.File.WriteAllBytes("E:/Factorio2/Unity/Docs/evidence/ux-pass/final-prepared.json",Relight.Sim.SaveSerializer.Write(sim.State,sim.Context,null,out _));Note("FINAL INPUT DONE");}
System.Collections.IEnumerator Guard(){var stack=new System.Collections.Generic.Stack<System.Collections.IEnumerator>();stack.Push(Run());while(stack.Count>0){var e=stack.Peek();bool next=false;try{next=e.MoveNext();}catch(System.Exception ex){Note("FAIL "+ex.Message);yield break;}if(!next){stack.Pop();continue;}if(e.Current is System.Collections.IEnumerator child){stack.Push(child);continue;}yield return e.Current;}}
host.StartCoroutine(Guard());return "Final weapon UI pass started";
