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
        /// The live core footprint, at its real width and height (the authored Home workshop is 10×14, the synthetic
        /// fall-back 3×3). REL-124: this used to report one side, the larger, so raids aimed at a 14×14 square that ran
        /// four tiles past the core's east wall and bit the core through whatever the player built there.
        /// A destroyed core reports <paramref name="found"/> = false: it is no longer a thing to attack. Since E-18
        /// the declaring side does NOT then answer from <c>ctx.Sites.Core</c> either — <c>CoreDownImpl</c> below
        /// tells it the core fell, so the raid has no target and its bodies walk off.
        /// </summary>
        static partial void CoreRectImpl(SimContext ctx, SimState st, ref int x, ref int y, ref int w, ref int h, ref bool found)
        {
            var home = st?.Home;
            if (home == null || !home.Placed || home.Hp <= 0) return;
            x = home.X;
            y = home.Y;
            w = home.W;
            h = home.H;
            found = w > 0 && h > 0;
        }

        /// <summary>REL-75: the placed core's hit points and its full hit points, for the first small raid's floor.</summary>
        static partial void CoreHpImpl(SimContext ctx, SimState st, ref double hp, ref double max, ref bool found)
        {
            var h = st?.Home;
            if (h == null || !h.Placed) return;
            hp = h.Hp;
            max = HomeQueries.CoreMaxHp(ctx?.Data);
            found = true;
        }

        /// <summary>E-18: the core was placed and is at 0 hit points.</summary>
        static partial void CoreDownImpl(SimContext ctx, SimState st, ref bool down)
        {
            var h = st?.Home;
            down = h != null && h.Placed && h.Hp <= 0;
        }
    }
}
