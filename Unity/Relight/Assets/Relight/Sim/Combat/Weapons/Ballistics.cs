using System;

namespace Relight.Sim
{
    /// <summary>
    /// The C-08 hand-off. Reference playerBallistics.ts casts against <c>[...T.crawlers, ...stalkersOf(st)]</c>
    /// (line 17); enemies are the next wave's work, so the port asks this seam instead of naming a body type.
    /// C-08 implements it over its own enemy list and assigns it to <see cref="WeaponState.Targets"/>; until then
    /// <see cref="WeaponState.Targets"/> is null and every ray stops at geometry.
    ///
    /// Contract for the implementer:
    /// * <see cref="Count"/> and <see cref="At"/> must enumerate bodies in a FIXED order (a list, never a
    ///   dictionary) — the reference picks the nearest body along the ray and ties must break the same way in
    ///   every run.
    /// * <see cref="Visible"/> is the reference's <c>citySight(st, x, y, c.x, c.y)</c> plus its per-kind rule
    ///   (a shade is only hittable on a lit tile, playerBallistics.ts:27).
    /// * <see cref="Hit"/> is the reference's <c>impact(target, damage, origin)</c>: apply the damage, raise your
    ///   own events. It is called at most once per ray and once per bolt.
    /// </summary>
    public interface IProjectileTargets
    {
        int Count { get; }
        /// <summary>The body's centre in tiles.</summary>
        Vec2 At(int index);
        bool Visible(SimContext ctx, SimState st, int index, double fromX, double fromY);
        void Hit(SimContext ctx, SimState st, int index, double damage, Vec2 origin);
    }

    /// <summary>The end of one cast: the body it found (-1 for none) and how far the ray reached.</summary>
    public readonly struct CastResult
    {
        public int Target { get; }
        public double Distance { get; }
        public CastResult(int target, double distance) { Target = target; Distance = distance; }
    }

    /// <summary>
    /// Ray casting and bolt sweeping, ported from reference playerBallistics.ts <c>cast</c> (19-31),
    /// <c>firePlayerWeapon</c> (32-44), <c>noteShot</c> (13-15) and <c>tickPlayerProjectiles</c> (45-57).
    ///
    /// Retired from the reference cast: the campaign region's closed freight gate (C-09's), and
    /// <c>litAt</c>/shade selection, which is the target layer's business and lives behind
    /// <see cref="IProjectileTargets.Visible"/>. Kept: the 0.2-tile step, the wall/barricade stop, line of sight,
    /// nearest-body-along-the-ray selection and the perpendicular-distance hit radius.
    /// </summary>
    public static class Ballistics
    {
        /// <summary>Reference playerBallistics.ts:21 — rays and bolt sweeps advance 0.2 tiles at a time.</summary>
        public const double Step = 0.2;
        /// <summary>Reference playerBallistics.ts:12 SHOT_TRACE_S — how long a tracer is drawn.</summary>
        public const double ShotTraceSeconds = 0.5;
        /// <summary>Reference playerBallistics.ts:14 — at most 24 tracers are kept.</summary>
        public const int MaxShots = 24;

        /// <summary>Reference weaponProfiles.ts:13 <c>weaponDamage</c>: full inside the effective range, falling linearly to zero at max.</summary>
        public static double Damage(WeaponDef p, double distance)
        {
            if (distance > p.MaxTiles) return 0;
            if (distance <= p.EffectiveTiles) return p.Damage;
            var span = p.MaxTiles - p.EffectiveTiles;
            if (span <= 0) return 0;
            return p.Damage * Math.Max(0, (p.MaxTiles - distance) / span);
        }

        /// <summary>
        /// Reference playerBallistics.ts:22 — a wall or barricade machine stops a ray; nothing else does. The rule
        /// itself now lives in <see cref="Sightline"/>, which every gun shares (GP-W3); this asks the same cached
        /// mask instead of re-scanning every machine at every 0.2-tile step.
        /// </summary>
        private static bool Blocked(SimContext ctx, SimState st, int tx, int ty) => st.Walls.At(ctx, st, tx, ty);

        /// <summary>
        /// Reference <c>cast</c>: walk the ray to the first stop (off map, authored building, wall/barricade, or a
        /// broken line of sight) and then choose the nearest body whose perpendicular distance is inside
        /// <paramref name="radius"/>.
        /// </summary>
        public static CastResult Cast(SimContext ctx, SimState st, double x, double y, double ux, double uy,
                                      double length, double radius)
        {
            var limit = length;
            for (var d = Math.Min(Step, length); d <= length + 1e-8; d = Math.Min(length, d + Step))
            {
                var nx = x + ux * d;
                var ny = y + uy * d;
                var tx = (int)Math.Floor(nx);
                var ty = (int)Math.Floor(ny);
                if (!Ground.InBounds(ctx, tx, ty) || ctx.Geometry.Solid(tx, ty) || Blocked(ctx, st, tx, ty)
                    || !ctx.Geometry.Sight(x, y, nx, ny))
                {
                    limit = Math.Max(0, d - Step);
                    break;
                }
                if (d >= length) break;
            }

            var targets = st.Weapons.Targets;
            var target = -1;
            var best = limit;
            if (targets != null)
            {
                for (var i = 0; i < targets.Count; i++)
                {
                    var c = targets.At(i);
                    var dx = c.X - x;
                    var dy = c.Y - y;
                    var along = dx * ux + dy * uy;
                    if (along < 0 || along > best) continue;
                    if (Math.Abs(dx * uy - dy * ux) > radius) continue;
                    if (!targets.Visible(ctx, st, i, x, y)) continue;
                    target = i;
                    best = along;
                }
            }
            return new CastResult(target, target >= 0 ? best : limit);
        }

        /// <summary>Reference <c>noteShot</c>: keep the newest 24 tracers.</summary>
        public static void NoteShot(SimState st, double x0, double y0, double x1, double y1, bool hit)
        {
            var shots = st.Weapons.Shots;
            shots.Add(new ShotTrace { From = new Vec2(x0, y0), To = new Vec2(x1, y1), T = st.T, Hit = hit });
            if (shots.Count > MaxShots) shots.RemoveRange(0, shots.Count - MaxShots);
        }

        /// <summary>
        /// Reference <c>firePlayerWeapon</c>: a projectile weapon pushes one bolt, everything else casts
        /// <c>pellets</c> rays across <c>spread</c> and applies falloff damage at the body's true distance.
        /// The muzzle is the engineer's exact centre, as in the reference (only turrets carry a muzzle offset).
        /// </summary>
        public static void Fire(SimContext ctx, SimState st, WeaponDef p, double ax, double ay)
        {
            var e = st.Engineer;
            var dx = ax - e.Pos.X;
            var dy = ay - e.Pos.Y;
            if (Math.Sqrt(dx * dx + dy * dy) < 1e-6) return;   // reference line 35
            var angle = Math.Atan2(dy, dx);
            var origin = e.Pos;

            if (p.ProjectileSpeed > 0)
            {
                st.Weapons.Projectiles.Add(new PlayerProjectile
                {
                    Kind = p.Key,
                    Pos = e.Pos,
                    Dir = new Vec2(Math.Cos(angle), Math.Sin(angle)),
                    Distance = 0,
                    Origin = origin,
                    Owner = 0,
                });
                return;
            }

            var pellets = Math.Max(1, p.Pellets);
            for (var i = 0; i < pellets; i++)
            {
                var a = angle + (pellets == 1 ? 0 : (i / (double)(pellets - 1) - 0.5) * p.SpreadRad);
                var ux = Math.Cos(a);
                var uy = Math.Sin(a);
                var ray = Cast(ctx, st, e.Pos.X, e.Pos.Y, ux, uy, p.MaxTiles, p.HitRadiusTiles);
                var targets = st.Weapons.Targets;
                var hit = ray.Target >= 0 && targets != null;
                var end = hit ? targets.At(ray.Target) : new Vec2(e.Pos.X + ux * ray.Distance, e.Pos.Y + uy * ray.Distance);
                NoteShot(st, e.Pos.X, e.Pos.Y, end.X, end.Y, hit);
                if (!hit) continue;
                var d = Math.Sqrt((end.X - e.Pos.X) * (end.X - e.Pos.X) + (end.Y - e.Pos.Y) * (end.Y - e.Pos.Y));
                var damage = Damage(p, d);
                if (damage > 0) targets.Hit(ctx, st, ray.Target, damage, origin);
            }
        }

        /// <summary>
        /// Reference <c>tickPlayerProjectiles</c>: sweep each bolt in 0.2-tile sub-steps so nothing tunnels,
        /// stop it on a body or on geometry, and expire it at the profile's max range.
        /// </summary>
        public static void TickProjectiles(SimContext ctx, SimState st, double dt)
        {
            var list = st.Weapons.Projectiles;
            for (var i = list.Count - 1; i >= 0; i--)
            {
                var p = list[i];
                if (!ctx.Data.TryWeapon(p.Kind, out var def)) { list.RemoveAt(i); continue; }
                var travel = Math.Min(def.MaxTiles - p.Distance, def.ProjectileSpeed * dt);
                var steps = Math.Max(1, (int)Math.Ceiling(travel / Step));
                var dead = false;
                for (var s = 0; s < steps; s++)
                {
                    var length = travel / steps;
                    var ray = Cast(ctx, st, p.Pos.X, p.Pos.Y, p.Dir.X, p.Dir.Y, length, def.HitRadiusTiles);
                    var targets = st.Weapons.Targets;
                    if (ray.Target >= 0 && targets != null)
                    {
                        var at = targets.At(ray.Target);
                        NoteShot(st, p.Pos.X, p.Pos.Y, at.X, at.Y, true);
                        targets.Hit(ctx, st, ray.Target, def.Damage, p.Origin);
                        dead = true;
                        break;
                    }
                    if (ray.Distance < length - 1e-8) { dead = true; break; }
                    p.Pos = new Vec2(p.Pos.X + p.Dir.X * length, p.Pos.Y + p.Dir.Y * length);
                    p.Distance += length;
                }
                if (!dead && p.Distance < def.MaxTiles - 1e-8) continue;
                st.Events.Add(new ProjectileExpiredEvent(st.T, p.Pos.X, p.Pos.Y, p.Kind, dead));
                list.RemoveAt(i);
            }
        }

        /// <summary>Drop tracers the renderer has finished with (reference: the renderer fades them out by age).</summary>
        public static void ExpireShots(SimState st)
        {
            var shots = st.Weapons.Shots;
            var keep = 0;
            for (var i = 0; i < shots.Count; i++)
                if (st.T - shots[i].T < ShotTraceSeconds) shots[keep++] = shots[i];
            if (keep < shots.Count) shots.RemoveRange(keep, shots.Count - keep);
        }
    }
}
