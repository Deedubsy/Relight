namespace Relight.Sim
{
    /// <summary>
    /// C-05's half of the C-08 seam declared in <c>Sim/Combat/Enemies/EnemyCoreHook.cs</c>: a raid body that reaches
    /// the Home core damages it through <see cref="HomeCore.Damage"/>, and the raid destination is the core this
    /// worker placed rather than the authored site alone.
    ///
    /// The reference calls <c>damageCore(st, core, amount)</c> straight from the threat layer (campaignDefence.ts);
    /// the partial method is the port's engine-free equivalent, and it is implemented exactly once — here.
    /// </summary>
    public static partial class EnemyCoreHook
    {
        static partial void DamageCoreImpl(SimContext ctx, SimState st, double amount) => HomeCore.Damage(st, amount);

        /// <summary>
        /// The live core footprint. <paramref name="size"/> is the declaring side's single dimension, so it takes the
        /// larger of the rect's two (the authored Home workshop is 10×14, the synthetic fall-back 3×3) — exactly what
        /// <c>EnemyCoreHook.Rect</c>'s own <c>ctx.Sites.Core</c> fall-back does.
        /// A destroyed core reports <paramref name="found"/> = false: it is no longer a thing to attack. Since E-18
        /// the declaring side does NOT then answer from <c>ctx.Sites.Core</c> either — <c>CoreDownImpl</c> below
        /// tells it the core fell, so the raid has no target and its bodies walk off.
        /// </summary>
        static partial void CoreRectImpl(SimContext ctx, SimState st, ref int x, ref int y, ref int size, ref bool found)
        {
            var h = st?.Home;
            if (h == null || !h.Placed || h.Hp <= 0) return;
            x = h.X;
            y = h.Y;
            size = h.W > h.H ? h.W : h.H;
            found = size > 0;
        }

        /// <summary>E-18: the core was placed and is at 0 hit points.</summary>
        static partial void CoreDownImpl(SimContext ctx, SimState st, ref bool down)
        {
            var h = st?.Home;
            down = h != null && h.Placed && h.Hp <= 0;
        }
    }
}
