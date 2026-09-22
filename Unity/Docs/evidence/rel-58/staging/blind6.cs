// REL-58 look: freeze the sim WITHOUT the pause menu (the private flag, not the Paused property that opens the menu),
// put a fresh alien in the dark between turret 2's dark sight and its range, mark the turret hit, and run one tick so
// the blind event is queued. The next frame drains it: badge and once-only guide line on screen together.
// Assistant Play session only (redirected saves). blind7.cs unfreezes.
var host = UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();
var ctx = host.Simulation.Context; var st = host.Simulation.State;
typeof(Relight.Presentation.SimHost).GetField("_paused", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(host, true);
Relight.Sim.Machine gun = null; foreach (var m in st.Machines) if (m.Kind == "turret") gun = m;
gun.Rounds = 50;
string kind = null; foreach (var d in ctx.Data.Enemies) { kind = d.Key; break; }
ctx.Data.TryEnemy(kind, out var edef);
var at = new Relight.Sim.Vec2(60.9, 362.9);
var id = st.Enemies.Next++;
st.Enemies.Actors.Add(new Relight.Sim.Enemy { Id = id, Kind = kind, Hp = edef.Hp, Pos = at, Home = at, Aim = at, Origin = (int)at.Y * ctx.Geometry.Width + (int)at.X, Layer = Relight.Sim.EnemyLayer.Site, Waypoint = -1 });
st.Events.Add(new Relight.Sim.EnemySpawnedEvent(st.T, id, kind, at.X, at.Y, 0));
st.Engineer.Pos = new Relight.Sim.Vec2(59.5, 356.5);
var u = st.Turrets.Of(gun.Id); u.Target = 0; u.Blind = false;
var hud = UnityEngine.Object.FindAnyObjectByType<Relight.UI.HudController>();
hud.Model.GetType().GetField("_taughtBlindTurret", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(hud.Model, false);
host.Simulation.Tick();
u.HitAt = st.T;
var n = 0; foreach (var ev in st.Events) if (ev is Relight.Sim.TurretBlindEvent) n++;
return "enemy " + id + " blind=" + Relight.Sim.TurretQueries.Blind(ctx, st, gun.Id) + " uBlind=" + u.Blind + " blindEvents=" + n + " T=" + st.T.ToString("0.00");
