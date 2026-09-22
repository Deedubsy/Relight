// REL-58 look: which drawers are open.
var s = "";
foreach (var doc in UnityEngine.Object.FindObjectsByType<UnityEngine.UIElements.UIDocument>(UnityEngine.FindObjectsSortMode.None))
{
    var root = doc.rootVisualElement; if (root == null) continue;
    root.Query<UnityEngine.UIElements.VisualElement>().ForEach(v => { if (v.name != null && (v.name.EndsWith("-panel") || v.name.EndsWith("-drawer") || v.name == "workshop" || v.name.Contains("pack")) && v.resolvedStyle.display == UnityEngine.UIElements.DisplayStyle.Flex && v.visible && v.worldBound.width > 0) s += v.name + " "; });
}
return s;
