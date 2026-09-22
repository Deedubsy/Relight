var pv = UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.PlacementPreviewPresenter>();
var bf = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var t = pv.GetType();
var mouse = UnityEngine.InputSystem.InputSystem.GetDevice<UnityEngine.InputSystem.Mouse>();
return "visible=" + pv.Visible + " kind=" + t.GetField("_kind", bf).GetValue(pv) + " at " + t.GetField("_x", bf).GetValue(pv) + "," + t.GetField("_y", bf).GetValue(pv) + " ok=" + pv.Ok + " problem=" + pv.Problem + " advice=" + pv.Advice + " drawn=" + pv.Drawn + " mouse=" + mouse.position.ReadValue();
