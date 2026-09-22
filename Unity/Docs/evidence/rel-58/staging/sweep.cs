// REL-58 look: slow ONLY the scaled clock the lamp-head sweep animates on (the sim runs on unscaled time), then put a
// fuelled generator and a pole beside substation 0 so its district goes live on the next tick. Assistant session only
// (redirected saves); timescale.cs restores the clock. Placement follows the game's own geometry rule.
var host = UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();
var ctx = host.Simulation.Context; var st = host.Simulation.State;
UnityEngine.Time.timeScale = 0.05f;
Relight.Sim.Machine Put(string kind, int ox, int oy)
{
    ctx.Data.TryMachine(kind, out var spec);
    for (var r = 0; r <= 4; r++)
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
var g = Put("generator", 94, 355);
if (g == null) return "no generator spot";
g.Inv[Relight.Sim.ItemId.Coal] = Relight.Sim.MachineInventory.GeneratorFuelCap(ctx.Data);
var p = Put("pole", 97, 355);
return "gen " + g.Id + " @" + g.X + "," + g.Y + " pole " + (p == null ? "none" : p.Id + " @" + p.X + "," + p.Y) + " T=" + st.T.ToString("0.0") + " timeScale=" + UnityEngine.Time.timeScale;
