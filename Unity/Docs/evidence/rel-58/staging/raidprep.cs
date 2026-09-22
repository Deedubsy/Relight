// REL-58 look, staging for a clean raid account: remove the staged alien 9 and the six staged Assembler Mk2s (the
// brownout staging), repair the core through the Admin panel's own command, and ring the core with four gun turrets
// (full hoppers) on poles, fed by one more fuelled generator. Placed free by the game's geometry rule, a tile clear
// of the core. Assistant Play session only (redirected saves).
var host = UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();
var ctx = host.Simulation.Context; var st = host.Simulation.State;
var sb = new System.Text.StringBuilder();
if (st.Enemies.Find(9) != null) sb.Append("killed9=" + Relight.Sim.Enemies.Damage(ctx, st, 9, 1e9, false) + " ");
var gone = st.Machines.RemoveAll(m => m.Kind == "assembler2"); st.Rev++; sb.Append("removedMk2=" + gone + " ");
host.Submit(new Relight.Sim.AdminCommand("repair"));
var h = st.Home;
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
double cx = h.X + h.W / 2.0, cy = h.Y + h.H / 2.0;
var spots = new[] { (cx - 1, h.Y - 4.0), (h.X + h.W + 3.0, cy - 1), (cx - 1, h.Y + h.H + 3.0), (h.X - 5.0, cy - 1) };
var poleSpots = new[] { (cx + 2, h.Y - 3.0), (h.X + h.W + 2.0, cy + 2), (cx + 2, h.Y + h.H + 2.0), (h.X - 3.0, cy + 2) };
for (var i = 0; i < 4; i++)
{
    var t = Put("turret", (int)spots[i].Item1, (int)spots[i].Item2);
    var p = Put("pole", (int)poleSpots[i].Item1, (int)poleSpots[i].Item2);
    if (t != null) t.Rounds = Relight.Sim.TurretHopper.Capacity(ctx.Data, t);
    sb.Append("T" + (t == null ? "none" : t.Id + "@" + t.X + "," + t.Y) + " P" + (p == null ? "none" : p.Id + "@" + p.X + "," + p.Y) + " ");
}
var g = Put("generator", h.X - 4, h.Y + h.H - 1);
if (g != null) g.Inv[Relight.Sim.ItemId.Coal] = Relight.Sim.MachineInventory.GeneratorFuelCap(ctx.Data);
sb.Append("G" + (g == null ? "none" : g.Id + "@" + g.X + "," + g.Y) + " ");
foreach (var m in st.Machines) if (m.Kind == "generator") m.Inv[Relight.Sim.ItemId.Coal] = Relight.Sim.MachineInventory.GeneratorFuelCap(ctx.Data);
sb.Append("home " + h.X + "," + h.Y + " " + h.W + "x" + h.H + " hp=" + h.Hp);
return sb.ToString();
