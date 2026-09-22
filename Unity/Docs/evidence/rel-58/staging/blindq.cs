var host = UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();
var ctx = host.Simulation.Context; var st = host.Simulation.State;
var sb = new System.Text.StringBuilder("T=" + st.T.ToString("0.0") + " paused=" + host.Paused);
foreach (var m in st.Machines) if (m.Kind == "turret") { var u = st.Turrets.Find(m.Id); sb.Append(" | turret " + m.Id + " blind=" + Relight.Sim.TurretQueries.Blind(ctx, st, m.Id) + " uBlind=" + (u == null ? "?" : u.Blind.ToString()) + " target=" + (u == null ? 0 : u.Target) + " rounds=" + m.Rounds); }
sb.Append(" enemies=" + st.Enemies.Actors.Count);
return sb.ToString();
