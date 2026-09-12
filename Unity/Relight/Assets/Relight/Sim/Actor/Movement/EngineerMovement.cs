using System;
using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>
    /// The engineer's current walk-here route (reference walk.ts:146 <c>Plan</c>). The reference deliberately keeps
    /// plans OUTSIDE <c>SimState</c> in a <c>WeakMap</c> — "Paths live outside SimState (a snapshot restarts them)",
    /// walk.ts:8 — so this is a field of <see cref="Engineer"/> that <see cref="Engineer.Visit"/> does NOT visit:
    /// loading a save drops the route and the next tick re-plans, exactly as the reference behaves.
    /// </summary>
    public sealed class EngineerPlan
    {
        public int Goal;
        public List<TilePoint> Path;
        public int At;
        public int Rev;
    }

    public sealed partial class Engineer
    {
        /// <summary>Not saved; see <see cref="EngineerPlan"/>.</summary>
        public EngineerPlan Plan;

        /// <summary>
        /// Reference engineer.ts:215 <c>hurt</c>. Named <c>TakeDamage</c> because the contract already has a
        /// <see cref="Hurt"/> field (cumulative damage taken), so the reference's name is not available.
        /// Combat itself is Phase C; this exists so the damage path has one definition of going down.
        /// The truck-seat dismount in the reference is Phase C and is omitted.
        /// </summary>
        public void TakeDamage(SimContext ctx, SimState st, double hp)
        {
            if (IsDown || hp <= 0) return;
            Hp -= hp;
            Hurt += hp;
            LastHit = st.T;
            if (Hp > 0) return;
            Hp = 0;
            Down = st.T + ctx.Data.Engineer.RespawnS;
            Downs++;
            HasAim = false;
            Sprint = false;
            Dash = 0;
            HasTarget = false;
            Vel = Vec2.Zero;
            Plan = null;
            st.Events.Add(new EngineerDownEvent(st.T, Pos.X, Pos.Y));
        }
    }

    /// <summary>
    /// The engineer's body: one 1/20 s tile tick (reference walk.ts:167 <c>tickEngineerTiles</c> plus the movement,
    /// respawn, regen and cooldown parts of engineer.ts <c>tickEngineer</c>). Registered as a world phase.
    ///
    /// Ported: respawn, dodge-cooldown decay, facing from held keys, the dash, sprint and the stamina bar,
    /// walk-here path following with re-planning, WASD movement with axis-separated collision sliding, the
    /// <c>walked</c> accumulator, health regeneration, weapon-cooldown decay, and the hand-crafting lock
    /// (walk.ts:181-183, GP-PLAYTEST-FIX 2).
    ///
    /// NOT ported, and why: the truck seat and <c>driveTruck</c> and the passenger lock
    /// (both read Phase C state); the block walk (<c>e.dest</c>, <c>e.remaining</c>, <c>e.block</c>,
    /// <c>kitBlock</c>) and its fallback, retired with the block economy (CONTENT_CATALOGUE.md §17); the hourly
    /// <c>walkedHour</c> row (B-03 owns the hour sampling); and <c>fireRound</c> (Phase C combat) — the cooldown it
    /// sets is still decayed here so the field behaves.
    /// </summary>
    public sealed class EngineerMovementPhase : ITickPhase
    {
        /// <summary>Reference walk.ts:158 <c>nearestOpen(st, gx, gy, r = 4)</c>.</summary>
        private const int NearestOpenRadius = 4;

        public void Tick(SimContext ctx, SimState st, double dt)
        {
            var e = st.Engineer;
            var d = ctx.Data.Engineer;
            var tw = ctx.Geometry.Width;

            if (e.IsDown)
            {
                if (st.T >= e.Down)
                {
                    var s = ctx.Geometry.Spawn;
                    e.Pos = s;
                    e.Hp = d.MaxHp;
                    e.Down = -1;
                    e.LastHit = st.T;
                    e.Plan = null;
                    st.Events.Add(new EngineerUpEvent(st.T));
                }
                return;
            }

            // Reference walk.ts:181-183 (GP-PLAYTEST-FIX 2): a running hand-craft batch pins the engineer at the
            // workbench. The route, the walk-here target and any dash in flight are dropped, and nothing below
            // this point runs, so the body cannot be displaced until the batch finishes or is cancelled
            // (`CancelCraftCommand` is still accepted while locked — see MovementCommandHandler).
            if (HandCraft.HandLocked(st))
            {
                e.Plan = null;
                e.HasTarget = false;
                e.Dash = 0;
                // e.Vel is deliberately NOT cleared, unlike the reference. Here a WalkCommand carries the held-key
                // intent straight into Vel and the presentation only re-sends it when the held keys CHANGE
                // (worldScene.ts `sendWalk` is level-triggered and MovementCommands.cs case WalkCommand mirrors it),
                // so zeroing Vel would strand a key held across the cancel: the engineer would stand still until the
                // player let go and pressed again. Holding the intent and suppressing the displacement is what
                // returns control on the very next tick.
                //
                // The count-down timers keep running while locked: the reference returns before them, which freezes
                // the dodge cooldown for the whole 20 s batch. Only health regeneration is shared with the reference.
                if (e.DashCooldown > 0) e.DashCooldown = Math.Max(0, e.DashCooldown - dt);
                if (e.Cooldown > 0) e.Cooldown = Math.Max(0, e.Cooldown - dt);
                if (e.Hp < d.MaxHp && st.T - e.LastHit >= d.RegenDelayS)
                    e.Hp = Math.Min(d.MaxHp, e.Hp + d.RegenPerS * dt);
                return;
            }

            var v = d.WalkTilesPerS * dt;
            var moved = false;
            var rev = st.Rev;

            if (e.DashCooldown > 0) e.DashCooldown = Math.Max(0, e.DashCooldown - dt);

            // Held keys — not a walk-here — are what sprints and what sets the facing.
            var keys = !e.HasTarget && (e.Vel.X != 0 || e.Vel.Y != 0);
            if (keys)
            {
                var l = Math.Sqrt(e.Vel.X * e.Vel.X + e.Vel.Y * e.Vel.Y);
                e.Face = new Vec2(e.Vel.X / l, e.Vel.Y / l);
            }

            if (e.Dash > 0)
            {
                var s = Math.Min(dt, e.Dash);
                var step = d.DashTiles / d.DashSeconds * s;
                e.Dash -= s;
                e.IFrames += s;
                var nx = e.Pos.X + e.DashDir.X * step;
                var ny = e.Pos.Y + e.DashDir.Y * step;
                if (Ground.CanStand(ctx, st, nx, e.Pos.Y)) e.Pos.X = nx;
                if (Ground.CanStand(ctx, st, e.Pos.X, ny)) e.Pos.Y = ny;
                moved = true;
                v = 0;   // the dash is the whole of this tick's movement
            }
            else if (e.Sprint && keys)
            {
                // Sprint never takes the bar below one dodge; at the floor Shift just walks (release it to refill).
                if (e.Stamina > d.DashCost + 1e-9)
                {
                    e.Stamina = Math.Max(d.DashCost, e.Stamina - dt * d.SprintDrainPerS);
                    v *= d.SprintMul;
                }
            }
            else
            {
                e.Stamina = Math.Min(1, e.Stamina + dt * d.StaminaRegenPerS);
            }

            if (e.HasTarget)
            {
                var gx = (int)Math.Floor(e.Target.X);
                var gy = (int)Math.Floor(e.Target.Y);
                var hasNear = Ground.NearestOpen(ctx, st, gx, gy, NearestOpenRadius, out var near);
                var goal = hasNear ? near.Y * tw + near.X : -1;
                var plan = e.Plan;
                if (plan == null || plan.Goal != goal || plan.Rev != rev)
                {
                    var path = hasNear
                        ? PathFinder.FindPath(ctx, st, (int)Math.Floor(e.Pos.X), (int)Math.Floor(e.Pos.Y), near.X, near.Y)
                        : null;
                    if (path == null)
                    {
                        // No way there: the click is dropped (the reference's block-walk fallback is retired).
                        e.Plan = null;
                        plan = null;
                        e.HasTarget = false;
                        v = 0;
                    }
                    else
                    {
                        plan = new EngineerPlan { Goal = goal, Path = path, At = 0, Rev = rev };
                        e.Plan = plan;
                    }
                }
                if (plan != null && v > 0)
                {
                    while (v > 0 && plan.At < plan.Path.Count)
                    {
                        var t = plan.Path[plan.At];
                        if (!Ground.Passable(ctx, st, t.X, t.Y))
                        {
                            e.Plan = null;
                            e.HasTarget = false;
                            break;
                        }
                        var cx = t.X + 0.5;
                        var cy = t.Y + 0.5;
                        var dx = cx - e.Pos.X;
                        var dy = cy - e.Pos.Y;
                        var l = Math.Sqrt(dx * dx + dy * dy);
                        if (l <= v)
                        {
                            e.Pos = new Vec2(cx, cy);
                            v -= l;
                            plan.At++;
                        }
                        else
                        {
                            e.Pos = new Vec2(e.Pos.X + dx / l * v, e.Pos.Y + dy / l * v);
                            v = 0;
                        }
                        moved = true;
                    }
                    if (e.Plan == plan && plan.At >= plan.Path.Count)
                    {
                        e.Plan = null;
                        e.HasTarget = false;
                    }
                }
            }
            else if (e.Vel.X != 0 || e.Vel.Y != 0)
            {
                e.Plan = null;
                var l = Math.Sqrt(e.Vel.X * e.Vel.X + e.Vel.Y * e.Vel.Y);
                var nx = e.Pos.X + e.Vel.X / l * v;
                var ny = e.Pos.Y + e.Vel.Y / l * v;
                // Axis-separated: blocked on one axis, the engineer slides along the wall on the other.
                if (Ground.CanStand(ctx, st, nx, e.Pos.Y)) { e.Pos.X = nx; moved = true; }
                if (Ground.CanStand(ctx, st, e.Pos.X, ny)) { e.Pos.Y = ny; moved = true; }
            }

            if (moved) e.Walked += dt;

            if (e.Hp < d.MaxHp && st.T - e.LastHit >= d.RegenDelayS)
                e.Hp = Math.Min(d.MaxHp, e.Hp + d.RegenPerS * dt);

            if (e.Cooldown > 0) e.Cooldown = Math.Max(0, e.Cooldown - dt);
        }
    }

    /// <summary>
    /// Puts a fresh engineer on the map: standing at the geometry's spawn, at full health with a full stamina bar
    /// (reference engineer.ts <c>createEngineer</c> + walk.ts <c>workbenchTile</c>, which the port replaces with the
    /// injected <see cref="ICityGeometry.Spawn"/> — U-M-14, geometry is data).
    /// </summary>
    public sealed class EngineerSpawnInitializer : IStateInitializer
    {
        public void Init(SimContext ctx, SimState st)
        {
            var e = st.Engineer;
            e.Pos = ctx.Geometry.Spawn;
            e.Vel = Vec2.Zero;
            e.HasTarget = false;
            e.Face = new Vec2(1, 0);
            e.HasAim = false;
            e.Hp = ctx.Data.Engineer.MaxHp;
            e.Stamina = 1;
            e.Down = -1;
            e.Plan = null;
        }
    }
}
