// REL-58 look (GP-UX-9): Shift-drag the Coal stack (slot 0) and release it in the gutter just right of slot 7:
// the split must land in slot 7, not the first empty slot (3, now that Steel has moved).
var panel = UnityEngine.Object.FindAnyObjectByType<Relight.UI.InventoryPanelController>();
var payload = panel.DragFrom(Relight.UI.InventoryPanelController.SlotKind.Pack, 0);
if (payload == null) return "no payload";
panel.DragToPoint(new UnityEngine.Vector2(1569f, 202f), payload, true);
return "split " + payload.Item + " x" + payload.Count + " notice=[" + panel.Notice + "]";
