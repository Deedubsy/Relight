// REL-58 look (GP-UX-9), any UI scale: move slot FROM to a release 1 px below slot TO's bottom edge (in the gutter,
// no slot button under it), through the drawer's own near-miss path. SPLIT = Shift held.
const int FROM = 7, TO = 10; const bool SPLIT = true;
var panel = UnityEngine.Object.FindAnyObjectByType<Relight.UI.InventoryPanelController>();
var root = panel.GetComponent<UnityEngine.UIElements.UIDocument>().rootVisualElement;
var box = UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.Button>(root, "pack-" + TO).worldBound;
var release = new UnityEngine.Vector2(box.center.x, box.yMax + 1f);
var inside = "";
for (var i = 0; i < 40; i++) { var b = UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.Button>(root, "pack-" + i); if (b != null && b.worldBound.Contains(release)) inside += " " + i; }
var payload = panel.DragFrom(Relight.UI.InventoryPanelController.SlotKind.Pack, FROM);
if (payload == null) return "no payload";
panel.DragToPoint(release, payload, SPLIT);
var cells = panel.Model.Pack;
return "release=" + release + " insideSlots=[" + inside + "] " + payload.Item + " x" + payload.Count + " -> slot " + TO + " now " + (cells[TO].Empty ? "-" : cells[TO].Item + " x" + cells[TO].Count) + ", slot " + FROM + " now " + (cells[FROM].Empty ? "-" : cells[FROM].Item + " x" + cells[FROM].Count) + " notice=[" + panel.Notice + "]";
