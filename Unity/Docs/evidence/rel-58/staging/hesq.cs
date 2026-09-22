var hud = UnityEngine.Object.FindAnyObjectByType<Relight.UI.HudController>();
var host = UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>(); var st = host.Simulation.State;
var t = hud.Model.GetType().GetField("_taughtHesitation", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(hud.Model);
var sb = new System.Text.StringBuilder("taught=" + t + " T=" + st.T.ToString("0") + " | ");
foreach (var e in st.Enemies.Actors) sb.Append(e.Id + "@" + e.Pos.X.ToString("0.0") + "," + e.Pos.Y.ToString("0.0") + " hes=" + e.Hesitate.ToString("0.0") + " ");
return sb.ToString();
