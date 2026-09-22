// REL-58 look: with the sim paused, put an alien in the dark between turret 2's dark sight and its range, mark the
// turret hit now, and run ONE tick so the turret phase raises its blind event. Assistant Play session only.
var host = UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();
var ctx = host.Simulation.Context; var st = host.Simulation.State;
if (!host.Paused) return "pause first";
Relight.Sim.Machine gun = null; foreach (var m in st.Machines) if (m.Kind == "turret") gun = m;
ctx.Data.TryTurret("turret", out var def);
var dark = Relight.Sim.TurretRules.DarkSight(def);
var cx = gun.X + gun.Size / 2.0; var cy = gun.Y + gun.Size / 2.0;
string kind = null; foreach (var e in ctx.Data.Enemies) { kind = e.Key; break; }
ctx.Data.TryEnemy(kind, out var edef);
Relight.Sim.Vec2 at = default; var found = false;
for (var k = 0; k < 64 && !found; k++)
{
    var a = k * System.Math.PI * 2 / 64; var r = def.RangeTiles - 0.6;
    var px = cx + System.Math.Cos(a) * r; var py = cy + System.Math.Sin(a) * r;
    if (!Relight.Sim.Ground.CanStand(ctx, st, px, py, .3)) continue;
    if (Relight.Sim.LightQueries.LitAt(st, (int)System.Math.Floor(px), (int)System.Math.Floor(py))) continue;
    if (!Relight.Sim.Sightline.Clear(ctx, st, cx, cy, px, py)) continue;
    at = new Relight.Sim.Vec2(px, py); found = true;
}
if (!found) return "no dark spot";
var id = st.Enemies.Next++;
var body = new Relight.Sim.Enemy { Id = id, Kind = kind, Hp = edef.Hp, Pos = at, Home = at, Aim = at, Origin = (int)at.Y * ctx.Geometry.Width + (int)at.X, Layer = Relight.Sim.EnemyLayer.Site, Waypoint = -1 };
st.Enemies.Actors.Add(body); st.Events.Add(new Relight.Sim.EnemySpawnedEvent(st.T, id, kind, at.X, at.Y, 0));
st.Turrets.Of(gun.Id).HitAt = st.T;
host.Simulation.Tick();
var u = st.Turrets.Find(gun.Id);
var blindEvents = 0; foreach (var ev in st.Events) if (ev is Relight.Sim.TurretBlindEvent) blindEvents++;
return "enemy " + kind + " at " + at.X.ToString("0.0") + "," + at.Y.ToString("0.0") + " turret " + gun.Id + " blind=" + Relight.Sim.TurretQueries.Blind(ctx, st, gun.Id) + " uBlind=" + u.Blind + " target=" + u.Target + " blindEventsQueued=" + blindEvents + " T=" + st.T.ToString("0.00");
