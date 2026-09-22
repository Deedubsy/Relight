var host = UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();
var ctx = host.Simulation.Context; var st = host.Simulation.State; var d = st.Director;
var w = Relight.Sim.DirectorQueries.Warning(ctx, st);
var line = Relight.Sim.UI.HudViewModel.ThreatLine(ctx, st, out var urgent);
return "T=" + st.T.ToString("0.0") + " minor=" + (d.Minor != null) + " major=" + (d.Major != null) + " recoveryUntil=" + d.RecoveryUntil.ToString("0.0") + " nextStart=" + d.NextStart.ToString("0.0") + " notice='" + d.Notice + "' warn.kind=" + w.Kind + " secs=" + w.SecondsLeft.ToString("0.0") + " line='" + line + "' urgent=" + urgent + " opening=" + st.Opening.Status + " coreOp=" + Relight.Sim.HomeQueries.CoreOperational(st);
