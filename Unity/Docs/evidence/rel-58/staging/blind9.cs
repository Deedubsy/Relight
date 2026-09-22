// REL-58 look: a fresh site alien in the dark near turret 2, held still in its recover phase for 20 s of sim time,
// the turret re-marked hit and the once-only guide line re-armed. No freeze. Assistant Play session only.
var host = UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();
var ctx = host.Simulation.Context; var st = host.Simulation.State;
Relight.Sim.Machine gun = null; foreach (var m in st.Machines) if (m.Kind == "turret") gun = m;
gun.Rounds = 50;
string kind = null; foreach (var d in ctx.Data.Enemies) { kind = d.Key; break; }
ctx.Data.TryEnemy(kind, out var edef);
var at = new Relight.Sim.Vec2(60.9, 362.9);
var id = st.Enemies.Next++;
st.Enemies.Actors.Add(new Relight.Sim.Enemy { Id = id, Kind = kind, Hp = edef.Hp, Pos = at, Home = at, Aim = at, Origin = (int)at.Y * ctx.Geometry.Width + (int)at.X, Layer = Relight.Sim.EnemyLayer.Site, Waypoint = -1, Phase = Relight.Sim.EnemyPhaseKind.Recover, Until = st.T + 20 });
st.Events.Add(new Relight.Sim.EnemySpawnedEvent(st.T, id, kind, at.X, at.Y, 0));
st.Engineer.Pos = new Relight.Sim.Vec2(59.5, 356.5);
var u = st.Turrets.Of(gun.Id); u.Target = 0; u.Blind = false; u.HitAt = st.T + 0.5;
var hud = UnityEngine.Object.FindAnyObjectByType<Relight.UI.HudController>();
hud.Model.GetType().GetField("_taughtBlindTurret", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(hud.Model, false);
return "enemy " + id + " blindNow=" + Relight.Sim.TurretQueries.Blind(ctx, st, gun.Id) + " T=" + st.T.ToString("0.00");
