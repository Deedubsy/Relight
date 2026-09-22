// REL-58 look: a small unscripted raid (debug path, then marked not scripted) against full turrets, the engineer
// ~20 tiles east of the approach so the account is about the defence alone.
var host = UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();
var ctx = host.Simulation.Context; var st = host.Simulation.State;
if (st.Enemies.Actors.Count > 0 || st.Director.Minor != null) return "still enemies " + st.Enemies.Actors.Count;
st.Engineer.Pos = new Relight.Sim.Vec2(93.5, 366.5);
foreach (var m in st.Machines) if (m.Kind == "turret") m.Rounds = Relight.Sim.TurretHopper.Capacity(ctx.Data, m);
var allowed = st.Director.DebugAllowed; st.Director.DebugAllowed = true;
new Relight.Sim.DebugRaidHandler().TryApply(ctx, st, new Relight.Sim.DebugRaidCommand(3, true, 3), out var res);
st.Director.DebugAllowed = allowed;
if (st.Director.Minor != null) st.Director.Minor.Scripted = false;
return res.Problem + " T=" + st.T.ToString("0");
