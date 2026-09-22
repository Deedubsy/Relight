// REL-58 look (GP-UX-9): drag the Steel stack (slot 3) and release it 1 px below slot 9, in the gutter where no
// slot button is — the drawer's own near-miss drop path (DragToPoint -> NearestSlot -> Drop).
var panel = UnityEngine.Object.FindAnyObjectByType<Relight.UI.InventoryPanelController>();
var payload = panel.DragFrom(Relight.UI.InventoryPanelController.SlotKind.Pack, 3);
if (payload == null) return "no payload";
panel.DragToPoint(new UnityEngine.Vector2(1673f, 236f), payload);
return "dropped " + payload.Item + " x" + payload.Count + " notice=[" + panel.Notice + "]";
