using System;
namespace Relight.Sim
{
    /// <summary>Shared visible tile resolution for hover and held mining. Placed footprints win over resources.</summary>
    public static class WorldTargetQueries
    {
        public static bool Visible(SimContext ctx, SimState st, int x, int y, int w = 1, int h = 1)
        {
            if (!Ground.InBounds(ctx, x, y)) return false;
            var p = st.Engineer.Pos;
            var tx = Math.Max(x - .01, Math.Min(x + w + .01, p.X));
            var ty = Math.Max(y - .01, Math.Min(y + h + .01, p.Y));
            return ctx.Geometry.Sight(p.X, p.Y, tx, ty);
        }
        public static bool Resource(SimContext ctx, SimState st, int x, int y, out ItemId item, out double units)
        {
            item = default; units = 0;
            return Ground.InBounds(ctx, x, y) && !ctx.Geometry.Solid(x, y)
                && ProductionRules.MachineAt(st, x, y) == null && Visible(ctx, st, x, y)
                && Mining.TryTile(ctx, st, x, y, out item, out units);
        }
    }
}
