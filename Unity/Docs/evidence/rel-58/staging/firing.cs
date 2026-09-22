var host = UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();
var st = host.Simulation.State; var s = "shots=" + st.Weapons.Shots.Count + " |";
foreach (var m in st.Machines) if (m.Kind == "turret" && st.Turrets.Of(m.Id).Target != 0) s += " t" + m.Id + "->" + st.Turrets.Of(m.Id).Target;
foreach (var e in st.Enemies.Actors) s += " e" + e.Id + "@" + e.Pos.X.ToString("0.0") + "," + e.Pos.Y.ToString("0.0");
return s;
