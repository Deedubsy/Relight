// REL-58 look (GP-UX-7 c / GP-UX-8): stand the engineer beside the depot and call the method E calls
// (WorldInput.Interact), then report what opened and what is in hand and equipped.
var host = UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();
var sim = host.Simulation; var ctx = sim.Context; var st = sim.State;
Relight.Sim.Machine dep = null; foreach (var m in st.Machines) if (m.Kind == "depot") dep = m;
var placed = "";
for (var r = 1; r <= 4 && placed == ""; r++) for (var dy = -r; dy <= r && placed == ""; dy++) for (var dx = -r; dx <= r && placed == ""; dx++)
{
    var x = dep.X + dep.Size / 2.0 + dx; var y = dep.Y + dep.Size + .5 + dy;
    if (!Relight.Sim.Ground.CanStand(ctx, st, x, y, .3)) continue;
    st.Engineer.Pos = new Relight.Sim.Vec2(x, y);
    if (Relight.Sim.HandCraft.NearDepot(ctx, st)) placed = x + "," + y; 
}
var wi = UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.WorldInput>();
wi.GetType().GetMethod("Interact", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(wi, new object[] { sim });
var shell = UnityEngine.Object.FindAnyObjectByType<Relight.UI.UiShell>();
var active = shell.GetType().GetField("_active", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(shell);
return "depot " + dep.X + "," + dep.Y + " eng " + placed + " active=" + active + " tool=" + wi.Tool + " equipped=" + Relight.Sim.WeaponQueries.Equipped(st, ctx.Data).Item;
