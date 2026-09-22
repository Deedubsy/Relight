// REL-58 look: hold a kind and put the pointer on a tile. Assistant Play session only.
string kind = "turret"; double tx = 61.5, ty = 351.5;
var input = UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.WorldInput>();
if (kind != "-") input.HoldTool(kind);
var cam = UnityEngine.Camera.main;
var w = Relight.World.WorldSpace.World(new Relight.Sim.Vec2(tx, ty), 0f);
var sp = cam.WorldToScreenPoint(w);
var mouse = UnityEngine.InputSystem.InputSystem.GetDevice<UnityEngine.InputSystem.Mouse>();
UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse, new UnityEngine.InputSystem.LowLevel.MouseState { position = new UnityEngine.Vector2(sp.x, sp.y) });
var pv = UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.PlacementPreviewPresenter>();
return "tool=" + input.Tool + " screen=" + sp.x.ToString("0") + "," + sp.y.ToString("0") + " previewVisible=" + pv.Visible + " ok=" + pv.Ok + " problem=" + pv.Problem + " drawn=" + pv.Drawn;
