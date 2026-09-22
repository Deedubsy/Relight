var host = UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();
var st = host.Simulation.State; var s = "paused=" + host.Paused + " T=" + st.T.ToString("0.00") + " shots=" + st.Weapons.Shots.Count;
foreach (var sh in st.Weapons.Shots) s += " [" + sh.From.X.ToString("0.0") + "," + sh.From.Y.ToString("0.0") + "->" + sh.To.X.ToString("0.0") + "," + sh.To.Y.ToString("0.0") + " hit=" + sh.Hit + "]";
foreach (var m in st.Machines) if (m.Kind == "turret") s += " t" + m.Id + "@" + m.X + "," + m.Y;
s += " eng@" + st.Engineer.Pos.X.ToString("0.0") + "," + st.Engineer.Pos.Y.ToString("0.0");
return s;
