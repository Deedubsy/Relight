// REL-58 look: every row in the HUD notice inbox, shown or hidden, with its kind and seconds left.
var hud = UnityEngine.Object.FindAnyObjectByType<Relight.UI.HudController>();
var n = hud.Model.Notices;
var rows = (System.Collections.Generic.List<Relight.Sim.UI.HudNotice>)n.GetType().GetField("_rows", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(n);
var now = UnityEngine.Time.unscaledTimeAsDouble;
var s = "hidden=" + n.Hidden + " more=[" + n.MoreText + "] |";
foreach (var r in rows) { var shown = false; foreach (var l in n.Rows) if (l == r) shown = true; s += " {" + r.Key + " [" + r.Text + "] " + r.Kind + " rep=" + r.Repeats + " shown=" + shown + " left=" + (r.Seconds - (now - r.At)).ToString("0.0") + "}"; }
return s;
