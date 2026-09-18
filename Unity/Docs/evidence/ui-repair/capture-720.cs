var shell=UnityEngine.Object.FindAnyObjectByType<Relight.UI.UiShell>();
var doc=shell.GetComponent<UnityEngine.UIElements.UIDocument>();
UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/ui-repair/inventory-720.png");
return new{width=UnityEngine.Screen.width,height=UnityEngine.Screen.height};
