// REL-58 look: the booked major assault.
var host = UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();
var ctx = host.Simulation.Context; var st = host.Simulation.State; var m = st.Director.Major;
if (m == null) return "no major";
var w = ctx.Geometry.Width; var sb = new System.Text.StringBuilder();
sb.Append("id=" + m.Id + " total=" + m.Total + " remaining=" + m.Remaining + " startsAt=" + m.StartsAt.ToString("0") + " endsAt=" + m.EndsAt.ToString("0") + " waves=" + m.Wave + " origins: ");
foreach (var o in m.Origins) sb.Append((o % w) + "," + (o / w) + " ");
sb.Append("origin=" + (m.Origin % w) + "," + (m.Origin / w) + " T=" + st.T.ToString("0"));
return sb.ToString();
