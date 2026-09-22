// REL-58 look: find a legal turret spot near the Engineer where lit ground inside range is hidden behind a wall/building.
var host = UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();
var ctx = host.Simulation.Context; var st = host.Simulation.State;
ctx.Data.TryTurret("turret", out var t); ctx.Data.TryMachine("turret", out var spec);
var dark = Relight.Sim.TurretRules.DarkSight(t); var R = t.RangeTiles;
int ex = (int)st.Engineer.Pos.X, ey = (int)st.Engineer.Pos.Y;
var best = new System.Collections.Generic.List<string>();
int bestN = -1, bx = 0, by = 0, bestLit = 0;
for (var y = ey - 9; y <= ey + 9; y++) for (var x = ex - 16; x <= ex + 16; x++)
{
  var h = st.Home; if (x < h.X + h.W + 1 && x + spec.Size > h.X - 1 && y < h.Y + h.H + 1 && y + spec.Size > h.Y - 1) continue;
  if (Relight.Sim.Placement.GeometryProblem(ctx, st, "turret", x, y, Relight.Sim.Dir.N) != "") continue;
  var cx = x + spec.Size / 2.0; var cy = y + spec.Size / 2.0;
  var lit = Relight.Sim.LightPreview.LitWithin(st, cx, cy, R);
  int hidden = 0, all = 0;
  for (var j = 0; j < lit.H; j++) for (var i = 0; i < lit.W; i++)
  {
    if (!lit.At(lit.X0 + i, lit.Y0 + j)) continue;
    all++;
    var tx = lit.X0 + i + 0.5; var ty = lit.Y0 + j + 0.5;
    if (!Relight.Sim.Sightline.Clear(ctx, st, cx, cy, tx, ty)) hidden++;
  }
  if (hidden > bestN) { bestN = hidden; bx = x; by = y; bestLit = all; }
}
return "range=" + R + " dark=" + dark + " size=" + spec.Size + " best=" + bx + "," + by + " hiddenLit=" + bestN + " of lit=" + bestLit + " eng=" + ex + "," + ey;
