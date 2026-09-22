// REL-58 look: the substation lots nearest the Home core, with the lamps each owns and whether it is live.
var host = UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();
var ctx = host.Simulation.Context; var st = host.Simulation.State;
var h = st.Home; var cx = h.X + h.W / 2.0; var cy = h.Y + h.H / 2.0;
var subs = Relight.Sim.PowerGrid.SubstationSites(ctx);
var grid = Relight.Sim.PowerGrid.Of(ctx, st);
var list = new System.Collections.Generic.List<Relight.Sim.SiteRecord>(subs);
list.Sort((a, b) => ((a.Centre.X - cx) * (a.Centre.X - cx) + (a.Centre.Y - cy) * (a.Centre.Y - cy)).CompareTo((b.Centre.X - cx) * (b.Centre.X - cx) + (b.Centre.Y - cy) * (b.Centre.Y - cy)));
var sb = new System.Text.StringBuilder("subs=" + subs.Count);
for (var i = 0; i < System.Math.Min(4, list.Count); i++)
{
    var s = list[i]; var c = grid.OfSite(s.Id);
    sb.Append(" | " + s.Id + " '" + s.Name + "' @" + s.X + "," + s.Y + " " + s.W + "x" + s.H + " lamps=" + Relight.Sim.StreetLights.OwnedBy(ctx, s) + " live=" + (c != null && c.Throttle > 0) + " dist=" + System.Math.Sqrt((s.Centre.X - cx) * (s.Centre.X - cx) + (s.Centre.Y - cy) * (s.Centre.Y - cy)).ToString("0"));
}
return sb.ToString();
