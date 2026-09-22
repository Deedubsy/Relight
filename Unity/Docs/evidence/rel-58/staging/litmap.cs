// REL-58 look: which tiles are lit around the Home core (a coarse map), to pick a raid approach with a lit edge.
var host = UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();
var ctx = host.Simulation.Context; var st = host.Simulation.State;
var h = st.Home; var sb = new System.Text.StringBuilder();
sb.Append("home " + h.X + "," + h.Y + " " + h.W + "x" + h.H + " T=" + st.T.ToString("0") + "\n");
for (var y = h.Y - 20; y < h.Y + h.H + 20; y += 2) { for (var x = h.X - 30; x < h.X + h.W + 40; x += 2) sb.Append(x >= h.X && x < h.X + h.W && y >= h.Y && y < h.Y + h.H ? 'H' : Relight.Sim.LightQueries.LitAt(st, x, y) ? '#' : '.'); sb.Append('\n'); }
return sb.ToString();
