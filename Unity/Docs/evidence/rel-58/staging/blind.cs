// REL-58 look: stage a blind turret (powered, loaded, hit just now, an alien in the dark between its dark sight and
// its range). A staging script for an assistant Play session, never a player path.
var host = UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();
var ctx = host.Simulation.Context; var st = host.Simulation.State; var h = st.Home;
var placed = new System.Collections.Generic.List<string>();
System.Func<string, double, double, Relight.Sim.Machine> put = (kind, x, y) =>
{
    ctx.Data.TryMachine(kind, out var spec);
    var ox = (int)System.Math.Round(x - spec.Size / 2.0); var oy = (int)System.Math.Round(y - spec.Size / 2.0);
    for (var r = 0; r <= 6; r++)
        for (var dy = -r; dy <= r; dy++)
            for (var dx = -r; dx <= r; dx++)
            {
                int bx = ox + dx, by = oy + dy;
                if (bx < h.X + h.W + 1 && bx + spec.Size > h.X - 1 && by < h.Y + h.H + 1 && by + spec.Size > h.Y - 1) continue;
                if (Relight.Sim.Placement.GeometryProblem(ctx, st, kind, bx, by, Relight.Sim.Dir.N) != "") continue;
                var m = new Relight.Sim.Machine { Id = st.NextId++, Kind = kind, X = bx, Y = by, Dir = Relight.Sim.Dir.N, Size = spec.Size };
                st.Machines.Add(m); st.Rev++;
                placed.Add(kind + "@" + bx + "," + by);
                return m;
            }
    return null;
};
var ex = st.Engineer.Pos.X; var ey = st.Engineer.Pos.Y;
var gun = put("turret", ex - 4, ey);
var pole = put("pole", ex - 1, ey + 2);
var gen = put("generator", ex + 1, ey + 3);
if (gun == null || pole == null || gen == null) return "place failed " + string.Join(" ", placed);
gen.Inv[Relight.Sim.ItemId.Coal] = ctx.Data.Power.GeneratorFuelCap;
gun.Rounds = 20;
ctx.Data.TryTurret("turret", out var def);
var dark = Relight.Sim.TurretRules.DarkSight(def);
var cx = gun.X + gun.Size / 2.0; var cy = gun.Y + gun.Size / 2.0;
string kind = null; foreach (var e in ctx.Data.Enemies) { kind = e.Key; break; }
ctx.Data.TryEnemy(kind, out var edef);
Relight.Sim.Vec2 at = default; var found = false;
for (var k = 0; k < 64 && !found; k++)
{
    var a = k * System.Math.PI * 2 / 64; var r = (dark + def.RangeTiles) / 2.0;
    var px = cx + System.Math.Cos(a) * r; var py = cy + System.Math.Sin(a) * r;
    if (!Relight.Sim.Ground.CanStand(ctx, st, px, py, .3)) continue;
    if (Relight.Sim.LightQueries.LitAt(st, (int)System.Math.Floor(px), (int)System.Math.Floor(py))) continue;
    if (!Relight.Sim.Sightline.Clear(ctx, st, cx, cy, px, py)) continue;
    at = new Relight.Sim.Vec2(px, py); found = true;
}
if (!found) return "no dark spot; placed " + string.Join(" ", placed);
var id = st.Enemies.Next++;
var body = new Relight.Sim.Enemy { Id = id, Kind = kind, Hp = edef.Hp, Pos = at, Home = at, Aim = at, Origin = (int)at.Y * ctx.Geometry.Width + (int)at.X, Layer = Relight.Sim.EnemyLayer.Site, Waypoint = -1 };
st.Enemies.Actors.Add(body); st.Events.Add(new Relight.Sim.EnemySpawnedEvent(st.T, id, kind, at.X, at.Y, 0));
st.Turrets.Of(gun.Id).HitAt = st.T;
return "gun=" + gun.Id + " placed " + string.Join(" ", placed) + " enemy " + kind + " at " + at.X.ToString("0.0") + "," + at.Y.ToString("0.0") + " dark=" + dark + " range=" + def.RangeTiles + " supplied=" + Relight.Sim.PowerQueries.Supplied(ctx, st, gun.Id);
