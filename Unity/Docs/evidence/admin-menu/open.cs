var host=UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();var shell=UnityEngine.Object.FindAnyObjectByType<Relight.UI.UiShell>();
var doc=UnityEngine.Object.FindObjectsByType<UnityEngine.UIElements.UIDocument>().First(d=>d.visualTreeAsset!=null&&d.visualTreeAsset.name=="GameUI");
var root=doc.rootVisualElement;
UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode=UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
var kb=UnityEngine.InputSystem.Keyboard.current;UnityEngine.InputSystem.InputSystem.EnableDevice(kb);
System.Collections.IEnumerator Run(){
UnityEngine.InputSystem.InputSystem.QueueStateEvent(kb,new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.F8));yield return null;yield return null;
UnityEngine.InputSystem.InputSystem.QueueStateEvent(kb,new UnityEngine.InputSystem.LowLevel.KeyboardState());yield return new UnityEngine.WaitForSecondsRealtime(.4f);
UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/admin-menu/supplies-1080.png");
System.IO.File.WriteAllText("E:/Factorio2/Unity/Docs/evidence/admin-menu/open.txt","Active: "+shell.Active+"\nPanel: "+UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.VisualElement>(root,"admin-panel").worldBound+"\n");
}
host.StartCoroutine(Run());return new {controller=doc.GetComponent<Relight.UI.AdminPanelController>()!=null,panels=shell.PanelIds};
