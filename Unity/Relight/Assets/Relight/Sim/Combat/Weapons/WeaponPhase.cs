using System;

namespace Relight.Sim
{
    /// <summary>
    /// Hold the trigger towards (AimX, AimY) in tile coordinates (reference game/src/worldScene.ts:505, which calls
    /// <c>fireEquipment</c> + <c>firePlayerWeapon(st, ax, ay)</c> every frame while the button is down).
    /// Re-send it as the aim moves; the cadence is the sim's, not the sender's.
    /// </summary>
    public sealed record FireCommand(double AimX, double AimY) : Command;

    /// <summary>Let go of the trigger (reference: the pointer-up that clears worldScene's held flag).</summary>
    public sealed record HoldFireCommand : Command;

    /// <summary>Start a reload of the weapon in the hands (reference <c>{type:'reload'}</c>, the R key).</summary>
    public sealed record ReloadCommand : Command;

    /// <summary>One round left the muzzle. <c>Loaded</c> is what is left in the magazine after it.</summary>
    public sealed record WeaponFiredEvent(double T, string Item, string Kind, double Loaded) : SimEvent(T);

    /// <summary>A reload finished and moved <c>Rounds</c> bullets from the Backpack into the magazine.</summary>
    public sealed record ReloadedEvent(double T, string Item, double Rounds, double Loaded) : SimEvent(T);

    /// <summary>
    /// A bolt stopped: it reached its range, or it was stopped by geometry or a body (<c>Stopped</c> true).
    /// Part of the C-08 hand-off: the damage itself goes through <see cref="IProjectileTargets.Hit"/>; this event
    /// is only what the presentation needs in order to end a trail.
    /// </summary>
    public sealed record ProjectileExpiredEvent(double T, double X, double Y, string Kind, bool Stopped) : SimEvent(T);

    /// <summary>
    /// The player's weapon each tick, ported from reference equipment.ts <c>tickEquipment</c> (94-111) and
    /// <c>fireEquipment</c> (112-117), plus the player half of playerBallistics.ts <c>tickPlayerProjectiles</c>.
    /// Order inside the tick is the reference's: refund drain, remote-craft cancellation, cooldown decay, craft
    /// progress, reload completion, then firing, then bolts already in flight.
    ///
    /// Deliberate difference: the reference's <c>markShot(st)</c> (equipment.ts:116) tells the threat layer that
    /// this second was a shooting second; the threat layer is C-08's and <see cref="SecondPhase"/> already reads it
    /// through <c>ctx.Threat.Second</c>, so the port raises <see cref="WeaponFiredEvent"/> and leaves the marking to
    /// whoever owns the threat layer (see the W-C report's C-08 hand-off).
    /// </summary>
    public sealed class WeaponPhase : ITickPhase
    {
        private const double Eps = 1e-8;

        public void Tick(SimContext ctx, SimState st, double dt)
        {
            var d = ctx.Data;
            var e = st.Engineer;
            var q = st.Weapons;

            WeaponRules.Drain(d, st);   // a cancelled craft's plates return as Backpack space appears

            // U-D-44 supersedes equipment.ts:96 — the workshop keeps working while the engineer is elsewhere or
            // down, so walking away no longer cancels and refunds the rifle.

            for (var i = 0; i < q.Owned.Count; i++)
            {
                var o = q.Owned[i];
                o.Cooldown = Math.Max(0, o.Cooldown - dt);
            }

            // equipment.ts:98-104, changed by U-D-44: the craft runs at the workshop whether or not the engineer is
            // there, and the finished rifle waits in the workshop's output tray until it is collected in reach —
            // never delivered remotely into the Backpack.
            if (q.Crafting && d.TryRecipe(WeaponRules.RifleRecipe, out var recipe))
            {
                q.CraftSeconds = Math.Min(recipe.Seconds, q.CraftSeconds + dt);
                if (q.CraftSeconds >= recipe.Seconds - Eps)
                {
                    var kind = recipe.OutputKey.Length > 0 ? recipe.OutputKey : WeaponRules.RifleRecipe;
                    var id = kind + ":" + q.Next.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    if (HandCraft.TrayPut(d, st.Hand.Output, new ItemKey(id), 1) == 1)
                    {
                        WeaponRules.Create(st, kind);
                        // The reference records `stats.made[id] = 1` (equipment.ts:102). `Stats.Made` is an
                        // ItemCounts over the 20 sim items, and `Ledger.Held` likewise counts only those, so a
                        // weapon instance is neither held nor sourced and the ledger stays balanced without it.
                        q.Crafting = false;
                        q.CraftSeconds = 0;
                        st.Events.Add(new WeaponCraftedEvent(st.T, id, kind));
                    }
                }
            }

            var w = q.ActiveWeapon();

            // equipment.ts:106-109 — the reload runs down and then moves whole bullets out of the Backpack.
            if (w != null && w.Reload > 0 && !e.IsDown)
            {
                w.Reload = Math.Max(0, w.Reload - dt);
                if (w.Reload <= 0)
                {
                    w.Reload = 0;
                    var ammo = WeaponRules.Ammo(d);
                    var n = Math.Min(WeaponRules.Capacity(d, w) - w.Loaded, WeaponRules.Reserve(d, e));
                    if (ammo != null && n > 0)
                    {
                        Pockets.Drop(d, e, ItemKey.Of(ammo.Item), n / ammo.RoundsPerItem);
                        w.Loaded += n;
                        st.Events.Add(new ReloadedEvent(st.T, w.Id, n, w.Loaded));
                    }
                }
            }

            e.Cooldown = w?.Cooldown ?? 0;   // equipment.ts:110

            if (q.Firing && e.HasAim && !e.IsDown && !HandCraft.HandLocked(st)) TryFire(ctx, st, w);

            Ballistics.TickProjectiles(ctx, st, dt);
            Ballistics.ExpireShots(st);
        }

        /// <summary>Reference <c>fireEquipment</c> (113-116) followed by <c>firePlayerWeapon</c>.</summary>
        private static void TryFire(SimContext ctx, SimState st, WeaponInstance w)
        {
            if (w == null || w.Reload > 0 || w.Cooldown > Eps || w.Loaded < 1) return;
            if (!ctx.Data.TryWeapon(w.Kind, out var p) || p.RatePerS <= 0) return;
            var e = st.Engineer;
            w.Loaded -= 1;
            w.Cooldown = 1 / p.RatePerS;
            e.Cooldown = w.Cooldown;
            st.Stats.EngineerFired++;
            st.Events.Add(new WeaponFiredEvent(st.T, w.Id, w.Kind, w.Loaded));
            Ballistics.Fire(ctx, st, p, e.Aim.X, e.Aim.Y);
        }
    }

    public sealed class WeaponHandler : ICommandHandler
    {
        public bool TryApply(SimContext ctx, SimState st, Command c, out CommandResult result)
        {
            switch (c)
            {
                case FireCommand f:
                {
                    if (st.Engineer.IsDown) { result = CommandResult.Refuse(WeaponRules.OnFootText); return true; }
                    if (HandCraft.HandLocked(st)) { result = CommandResult.Refuse(HandCraft.LockTextFor(st)); return true; }
                    if (st.Weapons.ActiveWeapon() == null) { result = CommandResult.Refuse(WeaponRules.NoWeaponText); return true; }
                    st.Engineer.HasAim = true;
                    st.Engineer.Aim = new Vec2(f.AimX, f.AimY);
                    st.Weapons.Firing = true;
                    result = CommandResult.Ok();
                    return true;
                }
                case HoldFireCommand _:
                    st.Weapons.Firing = false;
                    result = CommandResult.Ok();
                    return true;
                case ReloadCommand _:
                {
                    var (ok, reason) = WeaponRules.Reload(ctx, st);
                    result = ok ? CommandResult.Ok(reason) : CommandResult.Refuse(reason);
                    return true;
                }
                default:
                    result = default;
                    return false;
            }
        }
    }
}
