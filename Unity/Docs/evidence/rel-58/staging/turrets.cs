// REL-58 look: every turret's rounds, power throttle and blind state; the core; the director's small raid.
var host = UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();
var ctx = host.Simulation.Context; var st = host.Simulation.State;
var sb = new System.Text.StringBuilder();
foreach (var m in st.Machines) if (m.Kind == "turret") sb.Append(m.Id + "@" + m.X + "," + m.Y + " r=" + m.Rounds.ToString("0") + " thr=" + Relight.Sim.PowerQueries.Throttle(ctx, st, m.Id).ToString("0.00") + " | ");
sb.Append("core hp=" + st.Home.Hp.ToString("0") + " op=" + Relight.Sim.HomeQueries.CoreOperational(st) + " enemies=" + st.Enemies.Actors.Count + " minor=" + (st.Director.Minor == null ? "none" : st.Director.Minor.Id + (st.Director.Minor.Spawned ? " spawned" : "") + (st.Director.Minor.Retreat ? " retreat" : "")) + " T=" + st.T.ToString("0"));
return sb.ToString();
