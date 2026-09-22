// REL-58 look: the raid account's state, the live notice rows and the director's raids.
var host = UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();
var st = host.Simulation.State;
var hud = UnityEngine.Object.FindAnyObjectByType<Relight.UI.HudController>();
var a = hud.Model.Account; var sb = new System.Text.StringBuilder();
sb.Append("watching=" + a.Watching + " serial=" + a.Serial + " outcome=" + a.Outcome + " losses=" + a.Losses + " text=[" + a.Text + "] | rows: ");
foreach (var r in hud.Model.Notices.Rows) sb.Append("{" + r.Key + ": " + r.Text + "} ");
var d = st.Director;
sb.Append("| minor=" + (d.Minor == null ? "none" : d.Minor.Id + " spawned=" + d.Minor.Spawned + " retreat=" + d.Minor.Retreat + " scripted=" + d.Minor.Scripted));
sb.Append(" major=" + (d.Major == null ? "none" : d.Major.Id + " startsAt=" + d.Major.StartsAt.ToString("0")) + " T=" + st.T.ToString("0") + " recoveryUntil=" + d.RecoveryUntil.ToString("0"));
return sb.ToString();
