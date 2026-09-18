var doc=UnityEngine.Object.FindObjectsByType<UnityEngine.UIElements.UIDocument>(UnityEngine.FindObjectsSortMode.None).First(d=> d.visualTreeAsset != null && d.visualTreeAsset.name=="GameUI");
var names=new[]{"hud-host","hud-root","status-strip","engineer-block","backpack","inventory-panel-host","inventory-panel","opening-hint-host","opening-hint","goal-card-host","goal-card"};
var result=names.Select(n=> {var e=UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.VisualElement>(doc.rootVisualElement,n); return e==null ? new {name=n,rect=new float[0],display="missing",parent=""} : new {name=n,rect=new[]{e.worldBound.x,e.worldBound.y,e.worldBound.width,e.worldBound.height}, display=e.resolvedStyle.display.ToString(),parent=e.parent?.name};}).ToArray();
UnityEngine.Object.FindAnyObjectByType<Relight.UI.UiShell>().Open("inventory-panel");
UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/phase-c/audit-inventory-forced.png");
return new {screen=new[]{UnityEngine.Screen.width,UnityEngine.Screen.height},elements=result};
