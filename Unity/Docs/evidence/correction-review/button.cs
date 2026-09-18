var doc=UnityEngine.Object.FindObjectsByType<UnityEngine.UIElements.UIDocument>(UnityEngine.FindObjectsSortMode.None).First(d=>d.visualTreeAsset!=null&&d.visualTreeAsset.name=="GameUI");
var button=UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.Button>(doc.rootVisualElement,"open-build");
using(var e=UnityEngine.UIElements.NavigationSubmitEvent.GetPooled()){e.target=button;button.SendEvent(e);}
UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/correction-review/build-ui.png");
return new {active=UnityEngine.Object.FindAnyObjectByType<Relight.UI.UiShell>().Active};
