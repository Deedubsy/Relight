// REL-58 look: the engineer stood beside the core, so the raiders chased him (a chase never hesitates). Move him
// ~20 tiles east of the southern approach, re-arm the guide line, empty turrets, start an unscripted small raid.
var host = UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();
var ctx = host.Simulation.Context; var st = host.Simulation.State;
var hud = UnityEngine.Object.FindAnyObjectByType<Relight.UI.HudController>();
if (st.Enemies.Actors.Count > 0 || st.Director.Minor != null) return "still enemies " + st.Enemies.Actors.Count;
var placed = "";
for (var r = 0; r <= 6 && placed == ""; r++) for (var dy = -r; dy <= r && placed == ""; dy++) for (var dx = -r; dx <= r && placed == ""; dx++)
{
    var x = 93.5 + dx; var y = 366.5 + dy;
    if (!Relight.Sim.Ground.CanStand(ctx, st, x, y, .3)) continue;
    st.Engineer.Pos = new Relight.Sim.Vec2(x, y); placed = x + "," + y;
}
hud.Model.GetType().GetField("_taughtHesitation", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(hud.Model, false);
foreach (var m in st.Machines) if (m.Kind == "turret") m.Rounds = 0;
var allowed = st.Director.DebugAllowed; st.Director.DebugAllowed = true;
new Relight.Sim.DebugRaidHandler().TryApply(ctx, st, new Relight.Sim.DebugRaidCommand(3, true, 3), out var res);
st.Director.DebugAllowed = allowed;
if (st.Director.Minor != null) st.Director.Minor.Scripted = false;
return "eng " + placed + " " + res.Problem + " T=" + st.T.ToString("0");
