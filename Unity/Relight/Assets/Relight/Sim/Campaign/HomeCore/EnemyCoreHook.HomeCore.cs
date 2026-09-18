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
        /// A destroyed core reports <paramref name="found"/> = false: it is no longer a thing to attack, and the
        /// declaring side then answers from <c>ctx.Sites.Core</c> so raids on an imported region still have a place
        /// to walk to.
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
    }
}
