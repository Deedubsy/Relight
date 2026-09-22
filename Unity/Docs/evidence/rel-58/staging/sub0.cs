// REL-58 look: substation 0's lamps, and stand the Engineer where the camera frames them. Assistant session only.
var host = UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();
var ctx = host.Simulation.Context; var st = host.Simulation.State;
Relight.Sim.SiteRecord sub = null; foreach (var s in Relight.Sim.PowerGrid.SubstationSites(ctx)) if (s.Id == "substation:0") sub = s;
var sb = new System.Text.StringBuilder("sub @" + sub.X + "," + sub.Y);
double sx = 0, sy = 0; var n = 0;
foreach (var l in ctx.Sites.OfKind(Relight.Sim.SiteKind.Light))
    if (ReferenceEquals(Relight.Sim.PowerGrid.SubstationOf(ctx, l), sub)) { sb.Append(" | lamp @" + l.X + "," + l.Y); sx += l.X; sy += l.Y; n++; }
sb.Append(" mean=" + (sx / n).ToString("0.0") + "," + (sy / n).ToString("0.0"));
return sb.ToString();
