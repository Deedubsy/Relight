namespace Relight.Sim
{
    /// <summary>
    /// The seam through which C-08 damages the Home core without owning it.
    ///
    /// The reference calls <c>damageCore(st, core, amount)</c> from campaignDefence.ts directly; in the port the
    /// core belongs to C-05/W-A (<c>Sim/Campaign/HomeCore/</c>), which is written concurrently with this file.
    /// Rather than a mutable static delegate — module-global mutable state, which the architecture forbids because
    /// it would make a run depend on composition order — the seam is a <c>static partial void</c>, exactly the
    /// idiom <see cref="SimComposition"/> uses for its sub-slots: unimplemented it compiles to nothing (enemies
    /// then simply do no core damage, which is the correct behaviour on a map with no core), and C-05 implements it
    /// exactly once in its own file.
    ///
    /// THE PATCH C-05/W-A OWES (verbatim, in a file under Sim/Campaign/HomeCore/ that it owns):
    /// <code>
    /// namespace Relight.Sim
    /// {
    ///     public static partial class EnemyCoreHook
    ///     {
    ///         static partial void DamageCoreImpl(SimContext ctx, SimState st, double amount) =>
    ///             HomeCore.Damage(st, amount);
    ///         static partial void CoreRectImpl(SimContext ctx, SimState st, ref int x, ref int y, ref int size, ref bool found)
    ///         {
    ///             // set x/y/size to the core footprint and found = true while the core still stands
    ///         }
    ///     }
    /// }
    /// </code>
    /// Until that lands, <see cref="Rect"/> falls back to <c>ctx.Sites.Core</c> (the authored Home workshop rect,
    /// U-D-19), so raids still have a real destination on an imported region.
    /// </summary>
    public static partial class EnemyCoreHook
    {
        static partial void DamageCoreImpl(SimContext ctx, SimState st, double amount);

        /// <summary>
        /// The core footprint, if the region has one. C-05 may override the authored rect (for instance to stop
        /// reporting it once the core is destroyed) by implementing <c>CoreRectImpl</c>.
        /// </summary>
        static partial void CoreRectImpl(SimContext ctx, SimState st, ref int x, ref int y, ref int size, ref bool found);

        /// <summary>
        /// E-18. True when the region HAS a core of its own and that core is at 0 hit points. C-05 answers; with no
        /// implementation it stays false, which is right for a map whose core was never placed.
        /// </summary>
        static partial void CoreDownImpl(SimContext ctx, SimState st, ref bool down);

        /// <summary>The placed core has fallen. A fallen core is not a raid target (E-18, U-D-64 d).</summary>
        public static bool Down(SimContext ctx, SimState st)
        {
            var down = false;
            CoreDownImpl(ctx, st, ref down);
            return down;
        }

        /// <summary>Apply <paramref name="amount"/> hit points of damage to the Home core; a no-op until C-05 lands.</summary>
        public static void Damage(SimContext ctx, SimState st, double amount)
        {
            if (amount <= 0) return;
            DamageCoreImpl(ctx, st, amount);
        }

        /// <summary>The raid destination: the core footprint in tiles. False when the region has no core (synthetic map).</summary>
        public static bool Rect(SimContext ctx, SimState st, out int x, out int y, out int size)
        {
            x = 0; y = 0; size = 0;
            var found = false;
            CoreRectImpl(ctx, st, ref x, ref y, ref size, ref found);
            if (found) return true;
            // E-18: the authored-site fall-back is for a region whose core was never placed. A core that was placed
            // and has fallen is not a target at all — answering with the site here is what kept a beaten raid
            // standing on the wreck for ever (ENM-02).
            if (Down(ctx, st)) return false;
            var site = ctx.Sites?.Core;
            if (site == null) return false;
            x = site.X; y = site.Y; size = site.W > site.H ? site.W : site.H;
            return size > 0;
        }
    }
}
