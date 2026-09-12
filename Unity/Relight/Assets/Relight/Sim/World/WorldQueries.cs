using System;
using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>One tile as presentation sees it (reference ground.ts:504 <c>tileAt</c> plus the walk predicates).</summary>
    public sealed record TileView(int X, int Y, TileClass Class, bool Walkable, bool Passable, double Units);

    /// <summary>The engineer as presentation sees them. Tile space, Y-down; the view layer converts (U-M-14).</summary>
    public sealed record EngineerView(
        Vec2 Pos, Vec2 Face, double Hp, double MaxHp, double Stamina,
        bool Sprinting, bool Dashing, double DashCooldown, bool IsDown, double UpAt, double Walked);

    /// <summary>The route the engineer is walking, for drawing it (reference walk.ts:151 <c>currentPath</c>).</summary>
    public sealed record PathView(IReadOnlyList<TilePoint> Tiles, int At);

    /// <summary>
    /// Read-only selectors over the world. Presentation calls these; it never reads <see cref="SimState"/> directly
    /// (TECHNICAL_ARCHITECTURE.md §2.5). Every result is an immutable record, so a view cannot write state back.
    /// </summary>
    public static class WorldQueries
    {
        public static TileView Tile(SimContext ctx, SimState st, int x, int y) =>
            new TileView(
                x, y,
                Ground.TileAt(ctx, st, x, y),
                Ground.Walkable(ctx, x, y),
                Ground.Passable(ctx, st, x, y),
                Ground.UnitsAt(ctx, st, x, y));

        public static EngineerView Engineer(SimContext ctx, SimState st)
        {
            var e = st.Engineer;
            return new EngineerView(
                e.Pos, e.Face, e.Hp, ctx.Data.Engineer.MaxHp, e.Stamina,
                e.Sprint, e.Dash > 0, e.DashCooldown, e.IsDown, e.Down, e.Walked);
        }

        /// <summary>The current walk-here route, or null when the engineer is not following one.</summary>
        public static PathView Path(SimState st)
        {
            var p = st.Engineer.Plan;
            return p == null ? null : new PathView(p.Path, p.At);
        }

        /// <summary>Map size in tiles, for a view that needs to size a grid.</summary>
        public static (int width, int height) Size(SimContext ctx) => (ctx.Geometry.Width, ctx.Geometry.Height);
    }
}
