// REL-58 look: empty every turret (recorded so they can be refilled), re-arm the hesitation guide line and start a
// small unscripted raid (debug path, then marked not scripted) so raiders reach the lit ring round the core and pause.
var host = UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();
var ctx = host.Simulation.Context; var st = host.Simulation.State;
var hud = UnityEngine.Object.FindAnyObjectByType<Relight.UI.HudController>();
hud.Model.GetType().GetField("_taughtHesitation", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(hud.Model, false);
foreach (var m in st.Machines) if (m.Kind == "turret") m.Rounds = 0;
var allowed = st.Director.DebugAllowed; st.Director.DebugAllowed = true;
new Relight.Sim.DebugRaidHandler().TryApply(ctx, st, new Relight.Sim.DebugRaidCommand(3, true, 3), out var res);
st.Director.DebugAllowed = allowed;
if (st.Director.Minor != null) st.Director.Minor.Scripted = false;
return res.Problem + " T=" + st.T.ToString("0");
