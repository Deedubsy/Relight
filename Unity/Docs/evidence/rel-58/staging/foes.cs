var host = UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();
var st = host.Simulation.State;
var sb = new System.Text.StringBuilder("T=" + st.T.ToString("0.0") + " paused=" + host.Paused + " coreHp=" + st.Home.Hp + " engHp=" + st.Engineer.Hp);
foreach (var e in st.Enemies.Actors) sb.Append(" | " + e.Id + " " + e.Kind + " L" + e.Layer + " g" + e.Group + " hp" + e.Hp.ToString("0") + " @" + e.Pos.X.ToString("0.0") + "," + e.Pos.Y.ToString("0.0") + " ph" + e.Phase);
foreach (var m in st.Machines) sb.Append(" || " + m.Id + " " + m.Kind + " r" + m.Rounds);
return sb.ToString();
