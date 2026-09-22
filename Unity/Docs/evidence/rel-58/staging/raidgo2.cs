// REL-58 look: a small raid staged through the debug raid path (3 bodies), then marked NOT scripted in the same step,
// before the HUD's next look, so the raid account treats it as an ordinary small raid (the debug path builds it as a
// scripted group, which the account skips by design, like the opening). Turrets refilled first. Assistant session only.
var host = UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();
var ctx = host.Simulation.Context; var st = host.Simulation.State;
var hud = UnityEngine.Object.FindAnyObjectByType<Relight.UI.HudController>();
var taught = hud.Model.GetType().GetField("_taughtHesitation", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
var before = taught.GetValue(hud.Model);
foreach (var m in st.Machines) if (m.Kind == "turret") m.Rounds = Relight.Sim.TurretHopper.Capacity(ctx.Data, m);
var allowed = st.Director.DebugAllowed; st.Director.DebugAllowed = true;
new Relight.Sim.DebugRaidHandler().TryApply(ctx, st, new Relight.Sim.DebugRaidCommand(3, false, 3), out var res);
st.Director.DebugAllowed = allowed;
if (st.Director.Minor != null) st.Director.Minor.Scripted = false;
return "taughtHesitationBefore=" + before + " result=" + res.Problem + " minor=" + (st.Director.Minor == null ? "none" : st.Director.Minor.Id + " scripted=" + st.Director.Minor.Scripted) + " T=" + st.T.ToString("0");
