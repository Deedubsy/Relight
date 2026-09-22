// REL-58 look (GP-UX-9): the Backpack as the drawer shows it, slot by slot, with each laid-out button's box.
var panel = UnityEngine.Object.FindAnyObjectByType<Relight.UI.InventoryPanelController>();
var doc = panel.GetComponent<UnityEngine.UIElements.UIDocument>();
var cells = panel.Model.Pack; var s = "slots=" + cells.Count + " notice=[" + panel.Notice + "]";
for (var i = 0; i < cells.Count; i++)
{
    var b = UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.Button>(doc.rootVisualElement, "pack-" + i);
    var wb = b == null ? default : b.worldBound;
    if (!cells[i].Empty || i < 12) s += "\n" + i + ": " + (cells[i].Empty ? "-" : cells[i].Item + " x" + cells[i].Count) + " box=" + wb.x.ToString("0") + "," + wb.y.ToString("0") + " " + wb.width.ToString("0") + "x" + wb.height.ToString("0");
}
return s;
