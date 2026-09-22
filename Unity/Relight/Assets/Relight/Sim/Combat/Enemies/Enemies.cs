using System;

namespace Relight.Sim
{
    /// <summary>
    /// The living bodies: birth, damage, death and the two seams the rest of the game sees them through.
    /// Pure functions of (context, state); no static mutable field anywhere.
    /// </summary>
    public static class Enemies
    {
        // REL-84: how long a body remembers where it last saw the engineer (reference hostileAwareness.ts
        // HOSTILE_MEMORY_SECONDS) moved to SiegeTuning.MemoryS, unchanged at 6 s. Both sites that set it have a
        // SimContext, so it is read from ctx.Data.Siege rather than kept as a const here.

        public static EnemyDef Def(GameData d, Enemy e) => d.TryEnemy(e.Kind, out var def) ? def : null;

        /// <summary>
        /// The body the raid march pace is quoted for. <see cref="RaidTuning.SpeedTilesPerS"/> (2.0) is the speed a
        /// raid WALKS ITS APPROACH at — deliberately slower than the same body's 5.4 t/s chase, because a wave has
        /// to be readable from a distance and has to give the player time to act on the warning. It is one number
        /// for the whole roster, which is why every raider used to march identically; <see cref="MarchSpeed"/>
        /// keeps it as the pace of the SKITTER and scales every other type by its own configured speed.
        /// </summary>
        public const string MarchBaseline = "skitter";

        /// <summary>
        /// GP-W5: how fast <paramref name="def"/> walks its approach, in tiles per second.
        ///
        /// <c>raids.SpeedTilesPerS × def.SpeedTilesPerS / skitter.SpeedTilesPerS</c>. The skitter therefore marches
        /// at exactly the 2.0 it always did — no existing encounter changes pace — while the spitter comes in at
        /// 1.44 and the Breaker at 1.0, which is the "slow structure-breaker" its approved row (CONTENT_CATALOGUE.md
        /// §7.4) describes and the reason a player can choose to meet it away from the wall. Tuning stays in the
        /// exported <see cref="RaidTuning.SpeedTilesPerS"/>: changing it still moves the whole roster together.
        /// </summary>
        public static double MarchSpeed(GameData d, EnemyDef def)
        {
            var march = d.Raids != null ? d.Raids.SpeedTilesPerS : 2.0;
            if (def == null || def.SpeedTilesPerS <= 0) return march;
            if (!d.TryEnemy(MarchBaseline, out var baseline) || baseline.SpeedTilesPerS <= 0) return march;
            return march * def.SpeedTilesPerS / baseline.SpeedTilesPerS;
        }

        /// <summary>
        /// GP-W5: the ONE rule for how hard a bite lands on a STRUCTURE rather than on the engineer —
        /// <c>def.Damage × (raids.StructureDps / raids.ContactDps)</c>, a multiplier of 1.6 on the exported tables.
        ///
        /// Before this, every body of every type did a flat <see cref="RaidTuning.StructureDps"/> to a wall and a
        /// flat <see cref="RaidTuning.ContactDps"/> to the player, so the roster's own damage column did nothing at
        /// all outside the committed-attack path. The multiplier reproduces every documented number with no special
        /// case: the skitter's 5 becomes 8 per bite at its 1 s interval, which IS the exported 8 structure dps; the
        /// Breaker's 15 becomes 24, which is CONTENT_CATALOGUE.md §7.2's "three times structureDps" exactly and
        /// §7.4's approved 25 within rounding; the spitter's glob becomes 16. §7.4 binds the port to expressing the
        /// Breaker's heavier structure damage as the roster-wide multiplier and NOT as a second mechanic, so
        /// <see cref="RaidTuning.BreakerStructureMul"/> (3.0) is now a cross-check on those two rows rather than a
        /// rule of its own.
        /// </summary>
        public static double StructureDamage(GameData d, EnemyDef def)
        {
            if (def == null) return 0;
            return def.Damage * StructureMul(d);
        }

        /// <summary>The structure multiplier itself, 1 when the tables cannot supply one.</summary>
        public static double StructureMul(GameData d)
        {
            var r = d.Raids;
            if (r == null || r.ContactDps <= 0 || r.StructureDps <= 0) return 1;
            return r.StructureDps / r.ContactDps;
        }

        /// <summary>
        /// The continuous form of <see cref="StructureDamage"/>, for the two places a body chews rather than bites:
        /// walking into a structure that stands on its next field tile, and standing on the core. Per-bite damage
        /// spread over the type's own attack interval, so the two paths agree — a skitter does its 8 dps either way.
        /// </summary>
        public static double StructureDps(GameData d, EnemyDef def)
        {
            if (def == null) return 0;
            var interval = def.IntervalS > 0 ? def.IntervalS : 1;
            return StructureDamage(d, def) / interval;
        }

        /// <summary>The same continuous form against the engineer: the type's damage over its own interval.</summary>
        public static double ContactDps(GameData d, EnemyDef def)
        {
            if (def == null) return d.Raids != null ? d.Raids.ContactDps : 0;
            var interval = def.IntervalS > 0 ? def.IntervalS : 1;
            return def.Damage / interval;
        }

        /// <summary>
        /// GP-W5: is this type the roster's structure-breaker (CONTENT_CATALOGUE.md §7.4)? Read from the approved
        /// role text rather than from a hard-coded key list.
        ///
        /// REL-115 took targeting away from it. GP-W5 let a Breaker bite any machine it walked past; U-D-69 (h)
        /// says every raider aims for the core and the turrets and breaks a machine only when it blocks the path,
        /// so no sim rule reads this now. It stays as the role's name, which the catalogue tests check.
        /// </summary>
        public static bool BreaksStructures(EnemyDef def) =>
            def != null && def.Role != null && def.Role.IndexOf("structure-breaker", StringComparison.Ordinal) >= 0;

        /// <summary>
        /// Apply damage. Returns true when this hit killed the body, which is then removed from the list at once
        /// (the reference splices it out of <c>T.crawlers</c> the same tick) and an <see cref="EnemyKilledEvent"/>
        /// is raised. Damaging an unknown id is a no-op, never an exception — a turret can fire at a body the
        /// engineer kills in the same tick.
        /// </summary>
        public static bool Damage(SimContext ctx, SimState st, int id, double amount, bool byTurret = true)
        {
            if (amount <= 0) return false;
            var index = st.Enemies.IndexOf(id);
            if (index < 0) return false;
            var e = st.Enemies.Actors[index];
            e.Hp -= amount;
            if (e.Hp > 0) return false;
            st.Enemies.Actors.RemoveAt(index);
            st.Events.Add(new EnemyKilledEvent(st.T, e.Id, e.Kind, e.Pos.X, e.Pos.Y, byTurret, e.Layer, e.Group));
            return true;
        }

        /// <summary>
        /// Reference hostileAwareness.ts <c>rememberShot</c>: being hit tells a body where the shot came FROM, once,
        /// and wakes the squadmates standing within the alert radius. It never leaks the shooter's later positions.
        /// </summary>
        public static void RememberShot(SimContext ctx, SimState st, int id, Vec2 origin)
        {
            var e = st.Enemies.Find(id);
            if (e == null) return;
            e.LastKnown = origin;
            e.LastKnownUntil = st.T + ctx.Data.Siege.MemoryS;
            e.OnPlayer = true;
            var radius = ctx.Data.Raids.AlertRadiusTiles;
            for (var i = 0; i < st.Enemies.Actors.Count; i++)
            {
                var o = st.Enemies.Actors[i];
                if (o == e || o.Group != e.Group || o.Layer != e.Layer) continue;
                if (DirectorRules.Distance(o.Pos.X, o.Pos.Y, e.Pos.X, e.Pos.Y) > radius) continue;
                o.LastKnown = origin;
                o.LastKnownUntil = e.LastKnownUntil;
                o.OnPlayer = true;
            }
        }

        /// <summary>
        /// Reference hostileAwareness.ts <c>observePlayer</c>: refresh only from actual sight within the perception
        /// range (a wider one once already aware), otherwise investigate the remembered point until it is reached
        /// or the memory expires.
        ///
        /// L-02, ALWAYS_DARK_SPEC §5.5: an uncommitted body standing on a lit tile notices the engineer from a
        /// shorter distance (<c>RaidTuning.LitNoticeMul</c>, 0.6). Only the first notice shrinks: a body that is
        /// already aware keeps the full escape range, so stepping into light never makes a chase let go.
        /// </summary>
        public static void ObservePlayer(SimContext ctx, SimState st, Enemy e, double notice, double escape)
        {
            var p = st.Engineer;
            if (p.IsDown) { e.LastKnownUntil = 0; e.OnPlayer = false; return; }
            var distance = DirectorRules.Distance(p.Pos.X, p.Pos.Y, e.Pos.X, e.Pos.Y);
            var aware = e.LastKnownUntil > st.T;
            if (!aware && !Committed(st, e) && LightQueries.LitAt(st, (int)Math.Floor(e.Pos.X), (int)Math.Floor(e.Pos.Y)))
                notice *= ctx.Data.Raids.LitNoticeMul;
            if (distance <= (aware ? escape : notice) && ctx.Geometry.Sight(e.Pos.X, e.Pos.Y, p.Pos.X, p.Pos.Y))
            {
                e.LastKnown = p.Pos;
                e.LastKnownUntil = st.T + ctx.Data.Siege.MemoryS;
            }
            else if (aware && DirectorRules.Distance(e.Pos.X, e.Pos.Y, e.LastKnown.X, e.LastKnown.Y) < .5)
            {
                e.LastKnownUntil = 0;
            }
            e.OnPlayer = e.LastKnownUntil > st.T;
        }

        /// <summary>
        /// ALWAYS_DARK_SPEC §5.1/§5.5: a body is committed when it belongs to a committed raid group — a major
        /// assault that has started (<see cref="MajorRaid.Committed"/>). A minor wave on the march and a camp
        /// resident on patrol are not; being engaged with the engineer is a separate fact the callers know.
        /// </summary>
        public static bool Committed(SimState st, Enemy e) =>
            e != null && e.Layer == EnemyLayer.Major && st.Director.Major != null && st.Director.Major.Committed;

        /// <summary>
        /// ALWAYS_DARK_SPEC §5.1 — the pause at the edge of the light. Called by a body about to step from the tile
        /// it stands on to (<paramref name="nx"/>,<paramref name="ny"/>). Returns true while it must wait: its own
        /// tile is unlit, the next is lit, and it has not yet stood there for <c>RaidTuning.LightHesitateS</c>.
        /// The timer clears on any other step, so every new lit edge costs the pause again. Raises
        /// <see cref="LightHesitationEvent"/> as the pause begins, which is what the guide line listens for (§5.6).
        /// </summary>
        public static bool HesitatesAtLight(SimContext ctx, SimState st, Enemy e, int nx, int ny, double dt)
        {
            var need = ctx.Data.Raids.LightHesitateS;
            var cx = (int)Math.Floor(e.Pos.X);
            var cy = (int)Math.Floor(e.Pos.Y);
            if (need <= 0 || Committed(st, e) || (nx == cx && ny == cy)
                || LightQueries.LitAt(st, cx, cy) || !LightQueries.LitAt(st, nx, ny))
            {
                e.Hesitate = 0;
                return false;
            }
            if (e.Hesitate >= need) return false;
            if (e.Hesitate <= 0) st.Events.Add(new LightHesitationEvent(st.T, e.Id, e.Layer, e.Group, nx, ny));
            e.Hesitate += dt;
            return true;
        }

        /// <summary>
        /// Reference campaignThreat.ts:120 <c>route</c>: the ordinary field when the body can already reach the
        /// target through open ground, otherwise the breach field, which treats a defence as a door it can chew
        /// through. That is what makes walls matter without ever trapping a wave.
        /// </summary>
        public static RaidField Route(SimContext ctx, SimState st, Enemy e, int x, int y, int size)
        {
            var normal = st.Director.Fields.Field(ctx, st, x, y, size, false);
            if (normal.At((int)Math.Floor(e.Pos.X), (int)Math.Floor(e.Pos.Y)) >= 0) return normal;
            return st.Director.Fields.Field(ctx, st, x, y, size, true);
        }
    }

    /// <summary>
    /// The enemies seen as targets by W-C's ballistics (<see cref="IProjectileTargets"/>): the engineer's hitscan
    /// rays and bolts resolve against this. Rebuilt and assigned to <see cref="WeaponState.Targets"/> at the start
    /// of every <see cref="EnemyPhase"/> tick and by <see cref="EnemyInitializer"/>, because the field is never
    /// saved and a load therefore leaves it null.
    /// </summary>
    public sealed class EnemyTargets : IProjectileTargets
    {
        private readonly SimState _st;
        public EnemyTargets(SimState st) { _st = st; }

        /// <summary>
        /// REL-62 (ENM-07): point a state's weapon seam at its own bodies, building it if it has none. The ONE place
        /// that knows how, so the tick, the initialiser and <see cref="Simulation.Wrap"/> cannot drift apart — and so
        /// no test has to reach in and set the seam itself to make shooting work.
        /// </summary>
        public static void Point(SimState st)
        {
            if (st == null) return;
            if (st.Enemies.Seam == null) st.Enemies.Seam = new EnemyTargets(st);
            st.Weapons.Targets = st.Enemies.Seam;
        }

        public int Count => _st.Enemies.Actors.Count;

        public Vec2 At(int index) => _st.Enemies.Actors[index].Pos;

        public bool Visible(SimContext ctx, SimState st, int index, double fromX, double fromY)
        {
            if (index < 0 || index >= st.Enemies.Actors.Count) return false;
            var p = st.Enemies.Actors[index].Pos;
            return ctx.Geometry.Sight(fromX, fromY, p.X, p.Y);
        }

        public void Hit(SimContext ctx, SimState st, int index, double damage, Vec2 origin)
        {
            if (index < 0 || index >= st.Enemies.Actors.Count) return;
            var id = st.Enemies.Actors[index].Id;
            Enemies.RememberShot(ctx, st, id, origin);
            Enemies.Damage(ctx, st, id, damage, byTurret: false);
        }
    }

    /// <summary>
    /// C-08's implementation of the core's <see cref="IThreatLayer"/> seam — the replacement for the reference's
    /// module-global <c>threatHooks.current</c>. The host passes one to <see cref="SimContext"/>;
    /// <see cref="SecondPhase"/> then charges the engineer's danger seconds from it.
    ///
    /// NAME NOTE for the coordinator: the brief calls C-08's director surface "IThreatLayer" too, but that name was
    /// already taken by this core interface (Sim/Core/Contracts/Layers.cs), which has a different shape
    /// (<c>Tick</c>/<c>Second</c>) and existing implementors. C-09's surface is therefore
    /// <see cref="IRaidDirector"/>/<see cref="Director"/>, and this class fills the core's seam.
    ///
    /// <see cref="Tick"/> is deliberately empty: the enemies are stepped by <see cref="EnemyPhase"/>, which is in
    /// the fixed composition order, so ticking them again here would move every body twice.
    /// </summary>
    public sealed class EnemyThreatLayer : IThreatLayer
    {
        public void Tick(SimContext ctx, SimState st, double dt) { }

        public (bool danger, bool shot) Second(SimState st)
        {
            var danger = false;
            for (var i = 0; i < st.Enemies.Actors.Count && !danger; i++)
            {
                var e = st.Enemies.Actors[i];
                if (e.OnPlayer) danger = true;
                else if (DirectorRules.Distance(e.Pos.X, e.Pos.Y, st.Engineer.Pos.X, st.Engineer.Pos.Y) <= 1.5) danger = true;
            }
            // The reference counts a danger second as "shot" when the weapon fired inside it; the port reads the
            // weapon's own cooldown rather than keeping a second counter, which would not survive a load.
            return (danger, danger && st.Engineer.Cooldown > 0);
        }
    }
}
