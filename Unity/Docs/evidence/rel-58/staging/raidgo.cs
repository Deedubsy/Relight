// REL-58 look: start one small raid (3 bodies, west approach) through the Admin panel's own raid command, with the
// engineer made invulnerable through the Admin panel's override so a downed engineer does not muddy the account, and
// the once-only hesitation guide line re-armed. Engineer placed south-east of the core so the camera frames it.
var host = UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();
var ctx = host.Simulation.Context; var st = host.Simulation.State;
var hud = UnityEngine.Object.FindAnyObjectByType<Relight.UI.HudController>();
hud.Model.GetType().GetField("_taughtHesitation", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(hud.Model, false);
var placed = "";
for (var r = 0; r <= 4 && placed == ""; r++) for (var dy = -r; dy <= r && placed == ""; dy++) for (var dx = -r; dx <= r && placed == ""; dx++)
{
    var x = 76.5 + dx; var y = 361.5 + dy;
    if (!Relight.Sim.Ground.CanStand(ctx, st, x, y, .3)) continue;
    st.Engineer.Pos = new Relight.Sim.Vec2(x, y); placed = x + "," + y;
}
host.Submit(new Relight.Sim.AdminCommand("invulnerable", Amount: 1));
host.Submit(new Relight.Sim.AdminCommand("raid", Amount: 3, Direction: 3));
return "engineer " + placed + " T=" + st.T.ToString("0");
