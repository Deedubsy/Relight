// REL-58 look: hold the staged alien at its dark spot, mark turret 2 hit now, and queue a capture for this frame.
var host = UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();
var ctx = host.Simulation.Context; var st = host.Simulation.State;
Relight.Sim.Machine gun = null; foreach (var m in st.Machines) if (m.Kind == "turret") gun = m;
if (st.Enemies.Actors.Count == 0) return "no enemy";
var e = st.Enemies.Actors[st.Enemies.Actors.Count - 1];
var at = new Relight.Sim.Vec2(60.9, 362.9);
e.Pos = at; e.Home = at; e.Aim = at; e.Hp = 1000;
st.Turrets.Of(gun.Id).HitAt = st.T;
var path = @"C:\Users\Admin\AppData\Local\Temp\claude-rel58\ui.png";
if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
UnityEngine.ScreenCapture.CaptureScreenshot(path);
return "held enemy " + e.Id + " blindNow=" + Relight.Sim.TurretQueries.Blind(ctx, st, gun.Id) + " T=" + st.T.ToString("0.00");
