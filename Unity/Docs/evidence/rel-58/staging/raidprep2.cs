// REL-58 look, staging continued: a fuelled generator beside each of poles 14, 16 and 18, whose turrets had no power.
var host = UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();
var ctx = host.Simulation.Context; var st = host.Simulation.State;
var h = st.Home; var sb = new System.Text.StringBuilder();
bool Touches(int x, int y, int size) => x < h.X + h.W + 1 && x + size > h.X - 1 && y < h.Y + h.H + 1 && y + size > h.Y - 1;
Relight.Sim.Machine Put(string kind, int ox, int oy)
{
    ctx.Data.TryMachine(kind, out var spec);
    for (var r = 0; r <= 4; r++)
        for (var dy = -r; dy <= r; dy++)
            for (var dx = -r; dx <= r; dx++)
            {
                if (System.Math.Max(System.Math.Abs(dx), System.Math.Abs(dy)) != r) continue;
                if (Touches(ox + dx, oy + dy, spec.Size)) continue;
                if (Relight.Sim.Placement.GeometryProblem(ctx, st, kind, ox + dx, oy + dy, Relight.Sim.Dir.N) != "") continue;
                var m = new Relight.Sim.Machine { Id = st.NextId++, Kind = kind, X = ox + dx, Y = oy + dy, Dir = Relight.Sim.Dir.N, Size = spec.Size };
                st.Machines.Add(m); st.Rev++; return m;
            }
    return null;
}
foreach (var at in new[] { (74, 342), (79, 356), (74, 364) })
{
    var g = Put("generator", at.Item1, at.Item2);
    if (g != null) g.Inv[Relight.Sim.ItemId.Coal] = Relight.Sim.MachineInventory.GeneratorFuelCap(ctx.Data);
    sb.Append("G" + (g == null ? "none" : g.Id + "@" + g.X + "," + g.Y) + " ");
}
return sb.ToString();
