var host = UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();
if (host == null || host.Simulation == null) return "not ready";
var st = host.Simulation.State; var ctx = host.Simulation.Context; var h = st.Home;
var shell = UnityEngine.Object.FindAnyObjectByType<Relight.UI.UiShell>();
var cam = UnityEngine.Camera.main;
return "redirected=" + Relight.Presentation.SaveRoot.IsRedirected + " T=" + st.T.ToString("0.0") + " paused=" + host.Paused + " eng=" + st.Engineer.Pos.X.ToString("0.0") + "," + st.Engineer.Pos.Y.ToString("0.0")
  + " home=" + h.X + "," + h.Y + " " + h.W + "x" + h.H + " opening=" + st.Opening.Status + " machines=" + st.Machines.Count
  + " active=" + (shell == null ? "?" : shell.Active) + " cam=" + (cam == null ? "none" : cam.transform.position.ToString() + " ortho " + cam.orthographicSize)
  + " district=" + Relight.Sim.Districts.NameAt(ctx, st.Engineer.Pos.X, st.Engineer.Pos.Y) + " scale=" + Relight.UI.Settings.Preferences.Scale;
