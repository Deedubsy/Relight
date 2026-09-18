var root=UnityEngine.Object.FindObjectsByType<UnityEngine.UIElements.UIDocument>().First(d=>d.visualTreeAsset!=null && d.visualTreeAsset.name=="GameUI").rootVisualElement;
UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/industrial-ui/first-hud.png");
var names=new[]{"action-bar","goal-card","status-content","world-target","hud-slot-0","inventory-panel"};
return names.Select(n=>{var e=UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.VisualElement>(root,n);return new{name=n,bounds=e.worldBound.ToString(),background=e.resolvedStyle.backgroundImage.sprite?.name,display=e.resolvedStyle.display.ToString()};}).ToArray();
