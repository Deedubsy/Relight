// REL-58 look: restage the blind turret cleanly. Refill turret 2, give the staged alien its catalogue HP back and hold it
// in the dark between dark sight and range, stand the Engineer near, re-arm the once-only guide line, mark the turret
// hit now. Assistant Play session only (redirected saves).
var host = UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();
var ctx = host.Simulation.Context; var st = host.Simulation.State;
Relight.Sim.Machine gun = null; foreach (var m in st.Machines) if (m.Kind == "turret") gun = m;
if (st.Enemies.Actors.Count == 0) return "no enemy";
var e = st.Enemies.Actors[st.Enemies.Actors.Count - 1];
ctx.Data.TryEnemy(e.Kind, out var edef);
var at = new Relight.Sim.Vec2(60.9, 362.9);
e.Pos = at; e.Home = at; e.Aim = at; e.Hp = edef.Hp;
gun.Rounds = 50;
st.Engineer.Pos = new Relight.Sim.Vec2(59.5, 356.5);
var u = st.Turrets.Of(gun.Id); u.HitAt = st.T; u.Target = 0; u.Blind = false;
var hud = UnityEngine.Object.FindAnyObjectByType<Relight.UI.HudController>();
var fld = hud.Model.GetType().GetField("_taughtBlindTurret", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
fld.SetValue(hud.Model, false);
return "enemy " + e.Id + " hp=" + e.Hp + " blindNow=" + Relight.Sim.TurretQueries.Blind(ctx, st, gun.Id) + " T=" + st.T.ToString("0.00") + " engHp=" + st.Engineer.Hp;
