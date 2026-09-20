using System;

namespace Relight.Sim
{
    /// <summary>What a body has decided to do this tick (reference campaignThreat.ts <c>CampaignAction</c>).</summary>
    public enum EnemyAction { Attack, Breach, Pursue, Advance, Withdraw, Roam, Blocked }

    /// <summary>What the action is aimed at.</summary>
    public enum EnemyTargetKind { You, Structure, Core, Exit, Ruin }

    /// <summary>
    /// Bodies move and strike. The port of campaignThreat.ts <c>tickCampaignThreat</c>'s per-crawler loop plus
    /// gameplayCombat.ts <c>tickSpit</c> / <c>tickRaidAttack</c> / <c>tickCombatActor</c>, campaign-gameplay branch
    /// only. The reference's block economy, breaker/conductor roles, shades, reinforcements, encounter interrupts
    /// and the legacy dusk schedule are retired (CONTENT_CATALOGUE.md §17) and are not carried.
    ///
    /// DIFFERENCES, all deliberate and listed in the W-B report:
    /// 1. Pursuit uses the reference's greedy 8-connected <c>stepToward</c> (move.ts) rather than the budgeted A*
    ///    planner <c>cityStep</c> (cityNavigation.ts). The raid approach itself still walks the BFS field exactly as
    ///    the reference does, so long-range routing, walls and breaching are unaffected; only the last few tiles of
    ///    a chase differ. The planner needs per-body transient plans and a per-tick search budget and is deferred.
    /// 2. Danger seconds are charged by <see cref="SecondPhase"/> through <see cref="EnemyThreatLayer"/> instead of
    ///    the reference's <c>T.dangerS</c> accumulator, because the core already owns that counter.
    /// 3. The list is iterated by index over a snapshot count, and a body that dies or leaves is removed at once, so
    ///    a kill inside the loop cannot make another body skip its tick.
    /// </summary>
    public sealed class EnemyPhase : ITickPhase
    {
        public void Tick(SimContext ctx, SimState st, double dt)
        {
            // The ballistics seam is never saved: re-point it every tick so a load, a restart and a fresh game all
            // reach the same bodies (Wave 1 integration note).
            if (st.Enemies.Seam == null) st.Enemies.Seam = new EnemyTargets(st);
            st.Weapons.Targets = st.Enemies.Seam;

            if (st.Admin.FreezeEnemies)
            {
                foreach (var enemy in st.Enemies.Actors) enemy.Until += dt;
                return;
            }
            TickSpit(ctx, st, dt);

            var actors = st.Enemies.Actors;
            for (var i = 0; i < actors.Count; i++)
            {
                var e = actors[i];
                var before = actors.Count;
                Step(ctx, st, e, dt);
                // Step may have withdrawn (removed) this body; stay on the same index when the list shrank.
                if (actors.Count < before) i--;
            }
        }

        private static void Step(SimContext ctx, SimState st, Enemy e, double dt)
        {
            var d = ctx.Data;
            var raids = d.Raids;
            var def = Enemies.Def(d, e);
            if (def == null) return;

            if (e.Layer == EnemyLayer.Site) { TickCombatActor(ctx, st, e, def, dt); return; }

            // GP-W5. This used to read 1.2 tiles, so a raider only ever "noticed" the engineer by walking into
            // him: he could stand three tiles away and shoot a wave in the back all day. The perception radius the
            // tables already carry is raids.NoticeTiles (8), widening to raids.ChaseEscapeTiles (20) once aware,
            // which is the same pair the camp residents use — the contrast that showed the raid branch was the
            // defective one. Sight is still required, so a wall still hides you.
            Enemies.ObservePlayer(ctx, st, e, raids.NoticeTiles, raids.ChaseEscapeTiles);

            var action = Decide(ctx, st, e, def, out var tx, out var ty, out var what, out var exit, out var structure);

            // A committed attack owns the body's tick: it never walks while winding up or recovering.
            if (!exit && (what != EnemyTargetKind.You
                || (ctx.Geometry.Sight(e.Pos.X, e.Pos.Y, st.Engineer.Pos.X, st.Engineer.Pos.Y)
                    && DirectorRules.Distance(e.Pos.X, e.Pos.Y, st.Engineer.Pos.X, st.Engineer.Pos.Y) <= raids.ChaseEscapeTiles)))
            {
                var contact = action == EnemyAction.Attack || action == EnemyAction.Breach;
                if (TickRaidAttack(ctx, st, e, def, dt, tx, ty, what, structure, contact)) return;
            }

            if (exit)
            {
                if (!e.Withdrawing) { e.Waypoint = -1; e.Withdrawing = true; }
                e.OnPlayer = false;
                var w = ctx.Geometry.Width;
                var fld = Enemies.Route(ctx, st, e, e.Origin % w, e.Origin / w, 0);
                if (WalkField(ctx, st, e, def, fld, dt, false)) Leave(st, e);   // a body going home does not dither
                return;
            }

            if (what == EnemyTargetKind.You)
            {
                if (action == EnemyAction.Attack) st.Engineer.TakeDamage(ctx, st, Enemies.ContactDps(d, def) * dt);
                else
                {
                    e.Waypoint = -1;
                    var gx = e.LastKnownUntil > st.T ? e.LastKnown.X : tx + .5;
                    var gy = e.LastKnownUntil > st.T ? e.LastKnown.Y : ty + .5;
                    // A CHASE, not the approach march: the body runs at its own configured speed, exactly as a camp
                    // resident does. At 2.0 t/s a raider could never catch an engineer who simply walked away, which
                    // is why raids were safe to ignore. A skitter at 5.4 is a shade slower than a walk (6.0) and
                    // much slower than a sprint, so disengaging is a choice the player makes rather than a freebie.
                    StepToward(ctx, st, e, gx, gy, def.SpeedTilesPerS * dt);
                }
                return;
            }

            e.OnPlayer = false;

            if (action == EnemyAction.Attack && what == EnemyTargetKind.Structure && structure != null)
            {
                TurretRules.Damage(ctx, st, structure, Enemies.StructureDps(d, def) * dt);
                return;
            }

            if (!DirectorRules.Target(ctx, st, out var bx, out var by, out var size)) { e.Stuck += dt; return; }
            var route = Enemies.Route(ctx, st, e, bx, by, size);
            if (WalkField(ctx, st, e, def, route, dt, true)) EnemyCoreHook.Damage(ctx, st, Enemies.StructureDps(d, def) * dt);
        }

        private static void Leave(SimState st, Enemy e)
        {
            var index = st.Enemies.IndexOf(e.Id);
            if (index >= 0) st.Enemies.Actors.RemoveAt(index);
        }

        // ------------------------------------------------------------------ decision

        /// <summary>
        /// Reference campaignThreat.ts:404 <c>campaignCrawlerAction</c>, raid branch. Read-only: it decides, the
        /// caller acts, so the HUD can ask the same question without moving anything.
        /// </summary>
        private static EnemyAction Decide(SimContext ctx, SimState st, Enemy e, EnemyDef def,
            out int tx, out int ty, out EnemyTargetKind what, out bool exit, out Machine structure)
        {
            var w = ctx.Geometry.Width;
            var p = st.Engineer;
            structure = null;
            var known = e.LastKnownUntil > st.T;
            var visible = !p.IsDown && ctx.Geometry.Sight(e.Pos.X, e.Pos.Y, p.Pos.X, p.Pos.Y);
            var px = (int)Math.Floor(known ? e.LastKnown.X : p.Pos.X);
            var py = (int)Math.Floor(known ? e.LastKnown.Y : p.Pos.Y);

            // The body leaves when its wave is gone, when the director has called the retreat, or when there is
            // nothing left to attack (reference campaignThreat.ts: no target rect means the raid is over).
            bool retreat;
            if (e.Layer == EnemyLayer.Major) retreat = st.Director.Major == null || st.Director.Major.Retreat;
            else retreat = st.Director.Minor == null || st.Director.Minor.Retreat;
            if (!retreat && !DirectorRules.Target(ctx, st, out _, out _, out _)) retreat = true;
            exit = retreat;

            if (!retreat)
            {
                // GP-W5: the type's own reach, not a hard-coded 1.2 — the skitter bites at 1.3, the Breaker at 1.5
                // and the spitter opens up at 7.0, which is what makes its committed ranged attack reachable from
                // this branch at all. TickRaidAttack still owns the windup, the aim point and the projectile.
                var reach = def != null && def.RangeTiles > 0 ? def.RangeTiles : 1.2;
                if (!p.IsDown && p.Dash <= 0 && visible && DirectorRules.Distance(e.Pos.X, e.Pos.Y, p.Pos.X, p.Pos.Y) <= reach)
                { tx = px; ty = py; what = EnemyTargetKind.You; return EnemyAction.Attack; }
                if (known && !p.IsDown)
                { tx = px; ty = py; what = EnemyTargetKind.You; return EnemyAction.Pursue; }
                var near = NearbyStructure(ctx, st, e, def);
                if (near != null)
                { tx = near.X; ty = near.Y; what = EnemyTargetKind.Structure; structure = near; return EnemyAction.Attack; }
            }

            int gx, gy, size;
            if (retreat) { gx = e.Origin % w; gy = e.Origin / w; size = 0; }
            else DirectorRules.Target(ctx, st, out gx, out gy, out size);

            var fld = Enemies.Route(ctx, st, e, gx, gy, size);
            var here = fld.At((int)Math.Floor(e.Pos.X), (int)Math.Floor(e.Pos.Y));
            var reset = retreat && !e.Withdrawing;
            what = retreat ? EnemyTargetKind.Exit : EnemyTargetKind.Core;
            tx = gx; ty = gy;
            if (here == 0 && (reset || e.Waypoint < 0)) return retreat ? EnemyAction.Withdraw : EnemyAction.Attack;

            var next = FieldStep(ctx, st, e, fld, reset);
            if (next >= 0)
            {
                var m = st.Enemies.Index.At(ctx, st, next % w, next / w);
                if (m != null && TurretRules.IsDefence(ctx.Data, m) && TurretRules.Hp(ctx.Data, st, m) > 0)
                {
                    tx = m.X; ty = m.Y; what = EnemyTargetKind.Structure; structure = m;
                    return EnemyAction.Breach;
                }
            }
            if (next < 0 || !DirectorRules.HostileOpen(ctx, st, next % w, next / w)) return EnemyAction.Blocked;
            return retreat ? EnemyAction.Withdraw : EnemyAction.Advance;
        }

        /// <summary>
        /// A standing structure the body has walked right up to; it bites that before the core.
        ///
        /// GP-W5 SPLITS THIS BY ROLE, which is the brief's "avoid targeting every low-value belt unnecessarily".
        /// An ordinary raider still only stops for a WEAPON — the gun shooting at it — so it does not wander off to
        /// chew an inserter on its way to the core. A Breaker stops for any machine within reach, because breaking
        /// structures and the production connections behind them is its whole role (CONTENT_CATALOGUE.md §7.4);
        /// belts and poles are excluded everywhere by <see cref="CombatBalance"/>, which gives them no integrity at
        /// all, so even a Breaker cannot stall on a one-steel belt.
        /// </summary>
        private static Machine NearbyStructure(SimContext ctx, SimState st, Enemy e, EnemyDef def)
        {
            var anyStructure = Enemies.BreaksStructures(def);
            for (var i = 0; i < st.Machines.Count; i++)
            {
                var m = st.Machines[i];
                if (anyStructure ? !TurretRules.IsDefence(ctx.Data, m) : !TurretHopper.IsTurret(ctx.Data, m)) continue;
                if (TurretRules.Hp(ctx.Data, st, m) <= 0) continue;
                var cx = m.X + m.Size / 2.0;
                var cy = m.Y + m.Size / 2.0;
                if (DirectorRules.Distance(e.Pos.X, e.Pos.Y, cx, cy) >= 2) continue;
                if (!ctx.Geometry.Sight(e.Pos.X, e.Pos.Y, cx, cy)) continue;
                return m;
            }
            return null;
        }

        // ------------------------------------------------------------------ committed attacks

        /// <summary>
        /// Reference gameplayCombat.ts <c>tickRaidAttack</c>: an attack commits to an aim point taken at the start
        /// of the windup, so stepping out of the marked line — or behind cover — still works. Returns true when the
        /// body's tick is spent.
        /// </summary>
        private static bool TickRaidAttack(SimContext ctx, SimState st, Enemy e, EnemyDef def, double dt,
            int tileX, int tileY, EnemyTargetKind what, Machine structure, bool contact)
        {
            double x, y;
            if (what == EnemyTargetKind.You) { x = st.Engineer.Pos.X; y = st.Engineer.Pos.Y; }
            else if (structure != null) { x = structure.X + structure.Size / 2.0; y = structure.Y + structure.Size / 2.0; }
            else if (DirectorRules.Target(ctx, st, out var bx, out var by, out var size)) { x = bx + size / 2.0; y = by + size / 2.0; }
            else { x = tileX + .5; y = tileY + .5; }

            var sight = ctx.Geometry.Sight(e.Pos.X, e.Pos.Y, x, y);

            if (e.Phase == EnemyPhaseKind.Recover)
            {
                if (st.T < e.Until) return true;
                e.Phase = EnemyPhaseKind.Idle;
            }
            if (e.Phase == EnemyPhaseKind.Windup)
            {
                if (st.T < e.Until) return true;
                if (def.Ranged) Spit(ctx, st, e, def);
                else if (contact && sight && DirectorRules.Distance(x, y, e.Aim.X, e.Aim.Y) < 1.5)
                    ApplyHit(ctx, st, what, structure, def.Damage);
                e.Phase = EnemyPhaseKind.Recover;
                e.Until = st.T + def.IntervalS - def.WindupS;
                return true;
            }
            if (sight && (contact || (def.Ranged && DirectorRules.Distance(x, y, e.Pos.X, e.Pos.Y) < def.RangeTiles)))
            {
                e.Phase = EnemyPhaseKind.Windup;
                e.Until = st.T + def.WindupS;
                e.Aim = new Vec2(x, y);
                return true;
            }
            return false;
        }

        private static void ApplyHit(SimContext ctx, SimState st, EnemyTargetKind what, Machine structure, double damage)
        {
            if (what == EnemyTargetKind.You) st.Engineer.TakeDamage(ctx, st, damage);
            else if (structure != null) TurretRules.Damage(ctx, st, structure, damage);
            else EnemyCoreHook.Damage(ctx, st, damage);
        }

        private static void Spit(SimContext ctx, SimState st, Enemy e, EnemyDef def)
        {
            var dx = e.Aim.X - e.Pos.X;
            var dy = e.Aim.Y - e.Pos.Y;
            var len = Math.Sqrt(dx * dx + dy * dy);
            if (len <= 0) len = 1;
            var speed = ctx.Data.Raids.ProjectileSpeedTilesPerS;
            st.Enemies.Projectiles.Add(new EnemyProjectile
            {
                Pos = e.Pos,
                Vel = new Vec2(dx / len * speed, dy / len * speed),
                Life = ctx.Data.Raids.ProjectileLifeS,
                Source = e.Id,
            });
            st.Events.Add(new EnemySpitEvent(st.T, e.Id, e.Pos.X, e.Pos.Y, e.Aim.X, e.Aim.Y));
        }

        // ------------------------------------------------------------------ projectiles

        /// <summary>
        /// Reference gameplayCombat.ts <c>tickSpit</c>. Swept in 0.2-tile segments against the SAME solids and line
        /// of sight the bodies use — never endpoint-only collision — so a glob cannot pass through a wall corner.
        /// </summary>
        private static void TickSpit(SimContext ctx, SimState st, double dt)
        {
            var list = st.Enemies.Projectiles;
            if (list.Count == 0) return;
            var damage = ctx.Data.TryEnemy("spitter", out var spitter) ? spitter.Damage : 10;
            // GP-W5: a glob does the roster-wide structure multiple to a machine and its plain damage to a body,
            // the same split every bite uses — 16 to a wall, 10 to the engineer, from the one rule.
            var structureDamage = spitter != null ? Enemies.StructureDamage(ctx.Data, spitter) : damage;
            var hasCore = DirectorRules.Target(ctx, st, out var bx, out var by, out var size);

            for (var i = list.Count - 1; i >= 0; i--)
            {
                var p = list[i];
                var speed = Math.Sqrt(p.Vel.X * p.Vel.X + p.Vel.Y * p.Vel.Y);
                var n = Math.Max(1, (int)Math.Ceiling(speed * dt / Ballistics.Step));
                var dead = false;
                for (var k = 0; k < n && !dead; k++)
                {
                    var x = p.Pos.X + p.Vel.X * dt / n;
                    var y = p.Pos.Y + p.Vel.Y * dt / n;
                    var tx = (int)Math.Floor(x);
                    var ty = (int)Math.Floor(y);
                    if (!ctx.Geometry.Sight(p.Pos.X, p.Pos.Y, x, y) || !Ground.Passable(ctx, st, tx, ty))
                    {
                        var m = st.Enemies.Index.At(ctx, st, tx, ty);
                        if (m != null && TurretRules.IsDefence(ctx.Data, m)) TurretRules.Damage(ctx, st, m, structureDamage);
                        else if (hasCore && tx >= bx && ty >= by && tx < bx + size && ty < by + size) EnemyCoreHook.Damage(ctx, st, structureDamage);
                        dead = true;
                        break;
                    }
                    p.Pos = new Vec2(x, y);
                    if (hasCore && tx >= bx && ty >= by && tx < bx + size && ty < by + size)
                    {
                        EnemyCoreHook.Damage(ctx, st, structureDamage);
                        dead = true;
                        break;
                    }
                    if (!st.Engineer.IsDown && st.Engineer.Dash <= 0
                        && DirectorRules.Distance(st.Engineer.Pos.X, st.Engineer.Pos.Y, x, y) < .5)
                    {
                        st.Engineer.TakeDamage(ctx, st, damage);
                        dead = true;
                        break;
                    }
                }
                if (!dead)
                {
                    p.Life -= dt;
                    if (p.Life > 0) continue;
                }
                list.RemoveAt(i);
            }
        }

        // ------------------------------------------------------------------ field walking

        /// <summary>
        /// Reference campaignThreat.ts:388 <c>fieldStep</c>: the neighbour strictly closer to the target.
        /// L-02 (§5.2): "closer" is <see cref="RaidField.Cost"/>, which charges more for a lit tile, so the march
        /// bends round light where a darker way exists. Where nothing is lit it is the reference's step count.
        /// </summary>
        private static int FieldStep(SimContext ctx, SimState st, Enemy e, RaidField fld, bool ignoreWaypoint)
        {
            var w = ctx.Geometry.Width;
            var cx = (int)Math.Floor(e.Pos.X);
            var cy = (int)Math.Floor(e.Pos.Y);
            var next = ignoreWaypoint ? -1 : e.Waypoint;
            if (next >= 0) return next;
            var here = fld.Cost(cx, cy);
            var best = int.MaxValue;
            for (var k = 0; k < 4; k++)
            {
                var xx = cx + Dirs.DX[k];
                var yy = cy + Dirs.DY[k];
                if (!Ground.InBounds(ctx, xx, yy)) continue;
                var d = fld.Cost(xx, yy);
                if (d < 0) continue;
                if (here >= 0 && d >= here) continue;
                if (d >= best) continue;
                best = d;
                next = yy * w + xx;
            }
            return next;
        }

        /// <summary>
        /// Reference campaignThreat.ts:430 <c>walkField</c>: one tile at a time down the field, biting whatever
        /// defence stands in the way and giving ground to a body already standing on the next tile. Returns true
        /// when the body has arrived at the target rect.
        /// </summary>
        /// <param name="wary">True on the approach march: an uncommitted body pauses before it steps from unlit
        /// ground onto lit ground (ALWAYS_DARK_SPEC §5.1, <see cref="Enemies.HesitatesAtLight"/>).</param>
        private static bool WalkField(SimContext ctx, SimState st, Enemy e, EnemyDef def, RaidField fld, double dt, bool wary)
        {
            var w = ctx.Geometry.Width;
            var cx = (int)Math.Floor(e.Pos.X);
            var cy = (int)Math.Floor(e.Pos.Y);
            if (fld.At(cx, cy) == 0 && e.Waypoint < 0) return true;
            var next = FieldStep(ctx, st, e, fld, false);
            if (next < 0) { e.Stuck += dt; return false; }
            var x = next % w;
            var y = next / w;
            var m = st.Enemies.Index.At(ctx, st, x, y);
            if (m != null && TurretRules.IsDefence(ctx.Data, m) && TurretRules.Hp(ctx.Data, st, m) > 0)
            {
                TurretRules.Damage(ctx, st, m, Enemies.StructureDps(ctx.Data, def) * dt);
                e.Waypoint = -1;
                return false;
            }
            if (!DirectorRules.HostileOpen(ctx, st, x, y)) { e.Waypoint = -1; e.Stuck += dt; return false; }
            for (var i = 0; i < st.Enemies.Actors.Count; i++)
            {
                var o = st.Enemies.Actors[i];
                if (o == e) continue;
                if (DirectorRules.Distance(o.Pos.X, o.Pos.Y, x + .5, y + .5) >= .7) continue;
                e.Stuck += dt;
                e.Waypoint = -1;
                return false;
            }
            // The pause at the edge of the light comes after everything that can stop the step anyway, so a body
            // chewing through a wall or queueing behind another never spends its hesitation on a step it cannot take.
            if (wary && Enemies.HesitatesAtLight(ctx, st, e, x, y, dt)) { e.Waypoint = -1; return false; }
            e.Waypoint = next;
            // The APPROACH march, which is deliberately slower than a chase so a wave stays readable from a
            // distance: raids.SpeedTilesPerS scaled by this type's own speed (Enemies.MarchSpeed). A skitter still
            // marches at exactly the 2.0 it always did; a Breaker lumbers in at 1.0.
            MoveTo(e, x + .5, y + .5, Enemies.MarchSpeed(ctx.Data, def) * dt);
            e.Stuck = 0;
            if (DirectorRules.Distance(e.Pos.X, e.Pos.Y, x + .5, y + .5) < 1e-8) e.Waypoint = -1;
            return false;
        }

        /// <summary>Reference move.ts <c>moveTo</c>: step toward a point, arriving exactly when within a step.</summary>
        private static void MoveTo(Enemy e, double gx, double gy, double step)
        {
            var dx = gx - e.Pos.X;
            var dy = gy - e.Pos.Y;
            var len = Math.Sqrt(dx * dx + dy * dy);
            if (len > 1e-9) e.Dir = new Vec2(dx / len, dy / len);
            if (len <= step) e.Pos = new Vec2(gx, gy);
            else e.Pos = new Vec2(e.Pos.X + dx / len * step, e.Pos.Y + dy / len * step);
        }

        private static readonly int[] NX = { 0, 1, 0, -1, 1, 1, -1, -1 };
        private static readonly int[] NY = { -1, 0, 1, 0, -1, 1, 1, -1 };

        /// <summary>
        /// Reference move.ts <c>stepToward</c>: a greedy 8-connected step that never cuts a corner, used for the
        /// short chase and the site patrol. The reference's optional soft cost (the light avoidance a patrolling
        /// body applies to lit ground) is not carried; see the class remarks. Its pause is: with
        /// <paramref name="wary"/> set, the body waits <c>LightHesitateS</c> before a step from unlit ground onto
        /// lit ground (ALWAYS_DARK_SPEC §5.1). A chase never passes it — an engaged body does not hesitate.
        /// </summary>
        private static bool StepToward(SimContext ctx, SimState st, Enemy e, double gx, double gy, double step,
            bool wary = false, double dt = 0)
        {
            var cx = (int)Math.Floor(e.Pos.X);
            var cy = (int)Math.Floor(e.Pos.Y);
            var initial = DirectorRules.Distance(gx, gy, e.Pos.X, e.Pos.Y);
            if ((int)Math.Floor(gx) == cx && (int)Math.Floor(gy) == cy) { MoveTo(e, gx, gy, step); return true; }
            var best = -1;
            var bd = initial;
            for (var k = 0; k < 8; k++)
            {
                var xx = cx + NX[k];
                var yy = cy + NY[k];
                if (!Ground.Passable(ctx, st, xx, yy)) continue;
                if (k >= 4 && !(Ground.Passable(ctx, st, cx + NX[k], cy) && Ground.Passable(ctx, st, cx, cy + NY[k]))) continue;
                var d = DirectorRules.Distance(gx, gy, xx + 0.5, yy + 0.5);
                if (d < bd) { bd = d; best = k; }
            }
            if (best < 0) return false;
            if (wary && Enemies.HesitatesAtLight(ctx, st, e, cx + NX[best], cy + NY[best], dt)) return true;
            MoveTo(e, cx + NX[best] + 0.5, cy + NY[best] + 0.5, step);
            return true;
        }

        // ------------------------------------------------------------------ ruin guardians

        /// <summary>
        /// Reference gameplayCombat.ts <c>tickCombatActor</c>: a camp resident. It patrols its birthplace, wakes its
        /// own squad when it sees the engineer, commits an attack from an aim point, and goes back to patrol when
        /// the memory runs out. The guardian charge phase belongs to the freight guardian, which is not in the port's
        /// roster, so the charge branch is omitted.
        ///
        /// GP-W5 replaced the hard 36-tile boundary this used to break off at with a leash it can be watched on:
        /// see the comment at the pursuit check below for why the old rule made retreating from a camp free.
        /// </summary>
        private static void TickCombatActor(SimContext ctx, SimState st, Enemy e, EnemyDef def, double dt)
        {
            var raids = ctx.Data.Raids;
            var p = st.Engineer;
            var distance = DirectorRules.Distance(p.Pos.X, p.Pos.Y, e.Pos.X, e.Pos.Y);
            // Distant sleeping residents do not consume path searches (the reference's own 60-tile cut-off). A body
            // that is walking home is not sleeping: it has to be ticked wherever the engineer has got to, or it
            // would stand where it broke off for ever.
            if (distance > 60 && e.Phase == EnemyPhaseKind.Idle && !e.OnPlayer && !e.Withdrawing) return;
            var sight = !p.IsDown && ctx.Geometry.Sight(e.Pos.X, e.Pos.Y, p.Pos.X, p.Pos.Y);
            Enemies.ObservePlayer(ctx, st, e, raids.NoticeTiles, raids.EscapeTiles);

            // GP-W5: a LEASH the player can watch, not an invisible boundary.
            //
            // This used to be `engaged = known && Distance(lastKnown, home) < 36`, re-evaluated every tick, and the
            // failing branch WIPED the body's memory. So the moment the engineer stepped over a circle drawn around
            // the camp — a circle nothing on screen draws — every resident forgot him mid-stride and went back to
            // patrol. Retreating from a camp was free, and a committed attack could be cancelled by a coordinate.
            //
            // A resident now commits. It chases until it is genuinely far from its own camp
            // (<see cref="SiegeTuning.GuardPursuitTiles"/>), then BREAKS OFF visibly and walks back at its own
            // speed, and it will not turn and chase again until it is most of the way home
            // (<see cref="SiegeTuning.GuardReengageTiles"/>) and can actually see the engineer again. Nothing here
            // edits what the body knows: memory is cleared only on arrival, or by expiring on its own after
            // <see cref="Enemies.MemorySeconds"/>, which is what makes breaking line of sight the real escape.
            var siege = ctx.Data.Siege;
            var known = e.LastKnownUntil > st.T;
            var fromHome = DirectorRules.Distance(e.Pos.X, e.Pos.Y, e.Home.X, e.Home.Y);
            if (e.Withdrawing)
            {
                if (fromHome <= siege.GuardReengageTiles && known && sight) e.Withdrawing = false;
            }
            else if (fromHome > siege.GuardPursuitTiles)
            {
                e.Withdrawing = true;
            }
            var engaged = known && !e.Withdrawing;
            if (e.Withdrawing) e.OnPlayer = false;          // walking home is not a chase, and the HUD says so

            if (engaged && sight && distance <= raids.EscapeTiles)
            {
                for (var i = 0; i < st.Enemies.Actors.Count; i++)
                {
                    var o = st.Enemies.Actors[i];
                    if (o == e || o.Layer != EnemyLayer.Site || o.Group != e.Group) continue;
                    if (DirectorRules.Distance(o.Pos.X, o.Pos.Y, e.Pos.X, e.Pos.Y) > raids.AlertRadiusTiles) continue;
                    o.LastKnown = e.LastKnown;
                    o.LastKnownUntil = e.LastKnownUntil;
                    o.OnPlayer = true;
                }
            }

            if (e.Phase == EnemyPhaseKind.Recover)
            {
                if (st.T < e.Until) return;
                e.Phase = EnemyPhaseKind.Idle;
            }
            if (e.Phase == EnemyPhaseKind.Windup)
            {
                if (st.T < e.Until) return;
                if (def.Ranged) Spit(ctx, st, e, def);
                else if (sight && distance <= def.RangeTiles && p.Dash <= 0) st.Engineer.TakeDamage(ctx, st, def.Damage);
                e.Phase = EnemyPhaseKind.Recover;
                e.Until = st.T + def.IntervalS - def.WindupS;
                return;
            }

            // Breaking off. Placed AFTER the windup and recover blocks on purpose: a strike already committed to
            // still lands, because the leash decides what the body does next, never what it is in the middle of.
            if (e.Withdrawing)
            {
                e.Hesitate = 0;
                if (fromHome > .6)
                {
                    StepToward(ctx, st, e, e.Home.X, e.Home.Y, def.SpeedTilesPerS * dt);
                    return;
                }
                e.Withdrawing = false;                       // home; only now does it stop believing it knows where you are
                e.LastKnownUntil = 0;
                e.OnPlayer = false;
            }

            if (engaged)
            {
                e.Hesitate = 0;
                if (sight && distance <= raids.EscapeTiles && distance <= def.RangeTiles)
                {
                    e.Phase = EnemyPhaseKind.Windup;
                    e.Until = st.T + def.WindupS;
                    e.Aim = p.Pos;
                    return;
                }
                StepToward(ctx, st, e, e.LastKnown.X, e.LastKnown.Y, def.SpeedTilesPerS * dt);
                return;
            }

            // A short visible pause at illuminated ground (the reference's rule, gameplayCombat.ts:96; L-02,
            // ALWAYS_DARK_SPEC §5.1). The reference then takes a bounded soft-cost route round the light; the port's
            // patrol is a two-tile loop round its birthplace and does not, which is a recorded difference.
            var angle = (e.Id * 7 + e.Patrol) * Math.PI / 2;
            var gx = e.Home.X + Math.Cos(angle) * 2;
            var gy = e.Home.Y + Math.Sin(angle) * 2;
            if (DirectorRules.Distance(e.Pos.X, e.Pos.Y, gx, gy) < .6 || e.Stuck > 2) { e.Patrol++; e.Stuck = 0; }
            var bx = e.Pos.X;
            var by = e.Pos.Y;
            StepToward(ctx, st, e, gx, gy, def.SpeedTilesPerS * .35 * dt, true, dt);
            e.Stuck = DirectorRules.Distance(e.Pos.X, e.Pos.Y, bx, by) < 1e-8 ? e.Stuck + dt : 0;
        }
    }
}
