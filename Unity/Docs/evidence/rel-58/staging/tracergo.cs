// REL-58 look (GP-UX-8): the engineer a few tiles from turret 17 (south of the core), turrets full, a small
// unscripted raid from the south, so turret shots land near the engineer.
var host = UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();
var ctx = host.Simulation.Context; var st = host.Simulation.State;
if (st.Enemies.Actors.Count > 0 || st.Director.Minor != null) return "still enemies " + st.Enemies.Actors.Count;
var placed = "";
for (var r = 0; r <= 4 && placed == ""; r++) for (var dy = -r; dy <= r && placed == ""; dy++) for (var dx = -r; dx <= r && placed == ""; dx++)
{
    var x = 75.5 + dx; var y = 366.5 + dy;
    if (!Relight.Sim.Ground.CanStand(ctx, st, x, y, .3)) continue;
    st.Engineer.Pos = new Relight.Sim.Vec2(x, y); placed = x + "," + y;
}
foreach (var m in st.Machines) if (m.Kind == "turret") m.Rounds = Relight.Sim.TurretHopper.Capacity(ctx.Data, m);
var allowed = st.Director.DebugAllowed; st.Director.DebugAllowed = true;
new Relight.Sim.DebugRaidHandler().TryApply(ctx, st, new Relight.Sim.DebugRaidCommand(3, true, 3), out var res);
st.Director.DebugAllowed = allowed;
if (st.Director.Minor != null) st.Director.Minor.Scripted = false;
return "eng " + placed + " " + res.Problem + " T=" + st.T.ToString("0");
