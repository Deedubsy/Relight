// REL-58 look: overload substation 0's circuit (generator 5, 300 kW) with three Assembler Mk2s (150 kW each) placed
// by the game's geometry rule within pole 6's reach, so its streetlights run short: a brownout. Assistant session only.
var host = UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();
var ctx = host.Simulation.Context; var st = host.Simulation.State;
Relight.Sim.Machine Put(string kind, int ox, int oy)
{
    ctx.Data.TryMachine(kind, out var spec);
    for (var r = 0; r <= 3; r++)
        for (var dy = -r; dy <= r; dy++)
            for (var dx = -r; dx <= r; dx++)
            {
                if (System.Math.Max(System.Math.Abs(dx), System.Math.Abs(dy)) != r) continue;
                if (Relight.Sim.Placement.GeometryProblem(ctx, st, kind, ox + dx, oy + dy, Relight.Sim.Dir.N) != "") continue;
                var m = new Relight.Sim.Machine { Id = st.NextId++, Kind = kind, X = ox + dx, Y = oy + dy, Dir = Relight.Sim.Dir.N, Size = spec.Size };
                st.Machines.Add(m); st.Rev++; return m;
            }
    return null;
}
var sb = new System.Text.StringBuilder();
foreach (var at in new[] { (100, 353), (100, 357), (96, 358) }) { var m = Put("assembler2", at.Item1, at.Item2); sb.Append(m == null ? "none " : m.Id + "@" + m.X + "," + m.Y + " "); }
var grid = Relight.Sim.PowerGrid.Of(ctx, st);
Relight.Sim.SiteRecord sub = null; foreach (var s in Relight.Sim.PowerGrid.SubstationSites(ctx)) if (s.Id == "substation:0") sub = s;
var c = grid.OfSite(sub.Id);
return sb + "throttle=" + (c == null ? -1 : c.Throttle).ToString("0.00");
