// REL-58 look: hold the staged site alien (id 8) still in its recover phase for 20 s of sim time, re-mark turret 2 hit,
// re-arm the once-only guide line and unfreeze the sim. The turret goes blind on the next tick. Assistant session only.
var host = UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();
var ctx = host.Simulation.Context; var st = host.Simulation.State;
var e = st.Enemies.Find(8); if (e == null) return "no alien 8";
e.Pos = new Relight.Sim.Vec2(60.9, 362.9); e.Phase = Relight.Sim.EnemyPhaseKind.Recover; e.Until = st.T + 20;
Relight.Sim.Machine gun = null; foreach (var m in st.Machines) if (m.Kind == "turret") gun = m;
var u = st.Turrets.Of(gun.Id); u.Target = 0; u.Blind = false; u.HitAt = st.T + 0.5;
var hud = UnityEngine.Object.FindAnyObjectByType<Relight.UI.HudController>();
hud.Model.GetType().GetField("_taughtBlindTurret", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(hud.Model, false);
typeof(Relight.Presentation.SimHost).GetField("_paused", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(host, false);
return "blindNow=" + Relight.Sim.TurretQueries.Blind(ctx, st, gun.Id) + " T=" + st.T.ToString("0.00");
