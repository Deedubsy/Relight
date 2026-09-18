var host=UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();
UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode=UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
var keyboard=UnityEngine.InputSystem.InputSystem.GetDevice<UnityEngine.InputSystem.Keyboard>() ?? UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();
UnityEngine.InputSystem.InputSystem.EnableDevice(keyboard);
System.Collections.IEnumerator Run(){
UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.Tab));
yield return null;yield return null;yield return null;
UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState());
yield return null;yield return null;
System.IO.File.WriteAllText("E:/Factorio2/Unity/Docs/evidence/ux-pass/input-check.txt",UnityEngine.Object.FindAnyObjectByType<Relight.UI.UiShell>().Active ?? "closed");
UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/ux-pass/inventory-first.png");
}
host.StartCoroutine(Run());return "Tab queued through the Input System";
