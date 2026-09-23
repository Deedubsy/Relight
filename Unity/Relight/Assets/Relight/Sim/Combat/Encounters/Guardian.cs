using System;

namespace Relight.Sim
{
    /// <summary>A stronghold's guardian fell to half its hit points and woke its whole garrison (§5.5).</summary>
    public sealed record GuardianEnragedEvent(double T, int EnemyId, string Stronghold, double X, double Y) : SimEvent(T);

    /// <summary>A stronghold's guardian died at (X, Y), and its Power core lies there now (§5.5).</summary>
    public sealed record GuardianKilledEvent(double T, int EnemyId, string Stronghold, double X, double Y) : SimEvent(T);

    /// <summary>
    /// A Power core lying on the ground (§4.3 <c>CoreDrop</c>, §5.6): where a guardian fell, and later wherever the
    /// engineer puts one down. The design calls it a <c>Core</c> site; the port's sites are the immutable map the
    /// region was built from, so a core the game moves is saved state instead.
    /// </summary>
    public sealed class LooseCore : IVisitable
    {
        /// <summary>The <see cref="StrongholdDef.Id"/> it came from.</summary>
        public string Stronghold = "";
        public Vec2 Pos;

        public void Visit(IStateVisitor v)
        {
            v.Field("stronghold", ref Stronghold);
            v.Field("pos", ref Pos);
        }
    }

    /// <summary>
    /// Batch 4, FRT-06 (REL-141): the stronghold guardian (FREIGHT_STRONGHOLD_DESIGN §4.3, §5.5; the reference's
    /// gameplayCombat.ts <c>GP_COMBAT.guardian</c> and its <c>charge</c> branch of <c>tickCombatActor</c>).
    ///
    /// <para>It is an ordinary camp resident of its arena with one extra move. When its windup ends it does not
    /// strike where it stands: it CHARGES in a straight line at the point it wound up against, at
    /// <see cref="ChargeTilesPerS"/> for up to <see cref="ChargeSeconds"/>. The charge stops at the first wall,
    /// machine or shut door in its way, at the first touch of the engineer (one hit of its damage), or at the aim
    /// point, and every stop is followed by <see cref="RecoverSeconds"/> standing still. That recovery is the
    /// window the player is meant to hit it in.</para>
    ///
    /// <para><b>Phase 2</b> (§5.5): the hit that takes it to <see cref="Phase2At"/> or below shortens the windup
    /// and the recovery, and wakes every living body of its arena, wherever it stands, with the engineer's
    /// position. It happens once. <b>Death</b> leaves the stronghold's Power core where it fell.</para>
    /// </summary>
    public static class GuardianRules
    {
        /// <summary>The roster key (<see cref="CombatBalance.Guardian"/>).</summary>
        public const string Kind = "guardian";
        /// <summary>The reference's charge speed, tiles per second.</summary>
        public const double ChargeTilesPerS = 13;
        /// <summary>The longest a charge lasts.</summary>
        public const double ChargeSeconds = 0.9;
        /// <summary>The stand-still after a charge, before phase 2.</summary>
        public const double RecoverSeconds = 2.0;
        /// <summary>§4.3 <c>Phase2At</c>: half its 600.</summary>
        public const double Phase2At = 300;
        /// <summary>§4.3 <c>Phase2Windup</c>.</summary>
        public const double Phase2WindupSeconds = 0.8;
        /// <summary>§4.3 <c>Phase2Recover</c>.</summary>
        public const double Phase2RecoverSeconds = 1.2;
        /// <summary>The reference's charge body radius: every step must stand clear of solids by this much.</summary>
        public const double BodyRadius = 0.45;
        /// <summary>The reference's reach of a charge: it hits an engineer this close to its centre.</summary>
        public const double HitTiles = 1;
        /// <summary>The reference steps a charge in pieces no longer than this, so it cannot jump a thin wall.</summary>
        const double StepTiles = 0.2;

        public static bool Is(Enemy e) => e != null && string.CompareOrdinal(e.Kind, Kind) == 0;

        /// <summary>It has reached phase 2: its hit points are what say so, so a save needs nothing extra for it.</summary>
        public static bool Enraged(Enemy e) => Is(e) && e.Hp <= Phase2At;

        /// <summary>How long <paramref name="e"/> winds up: the row's value, or the phase-2 value.</summary>
        public static double WindupSeconds(Enemy e, EnemyDef def) => Enraged(e) ? Phase2WindupSeconds : def.WindupS;

        /// <summary>How long the guardian stands still after a charge.</summary>
        public static double ChargeRecoverSeconds(Enemy e) => Enraged(e) ? Phase2RecoverSeconds : RecoverSeconds;

        /// <summary>The stronghold whose arena <paramref name="e"/> guards, or null.</summary>
        public static StrongholdDef StrongholdOf(Enemy e)
        {
            if (e == null || string.IsNullOrEmpty(e.Site)) return null;
            var all = EncounterCatalogue.Strongholds;
            for (var i = 0; i < all.Count; i++) if (string.CompareOrdinal(all[i].Arena, e.Site) == 0) return all[i];
            return null;
        }

        /// <summary>
        /// The windup is over: start the charge at the aim point it wound up against. The aim is not updated, so an
        /// engineer who steps out of the line during the windup is not hit.
        /// </summary>
        public static void BeginCharge(SimState st, Enemy e)
        {
            e.Phase = EnemyPhaseKind.Charge;
            e.Until = st.T + ChargeSeconds;
        }

        /// <summary>One tick of a charge (reference <c>tickCombatActor</c>, <c>phase === 'charge'</c>).</summary>
        public static void Charge(SimContext ctx, SimState st, Enemy e, EnemyDef def, double dt)
        {
            var p = st.Engineer;
            var dx = e.Aim.X - e.Pos.X;
            var dy = e.Aim.Y - e.Pos.Y;
            var len = Math.Sqrt(dx * dx + dy * dy);
            var travel = Math.Min(len, ChargeTilesPerS * dt);
            var n = Math.Max(1, (int)Math.Ceiling(travel / StepTiles));
            for (var i = 0; i < n && len > 0; i++)
            {
                var x = e.Pos.X + dx / len * travel / n;
                var y = e.Pos.Y + dy / len * travel / n;
                if (!CanStand(ctx, st, x, y) || !ctx.Geometry.Sight(e.Pos.X, e.Pos.Y, x, y)) { Recover(st, e); return; }
                e.Pos = new Vec2(x, y);
                e.Dir = new Vec2(dx / len, dy / len);
                if (!p.IsDown && p.Dash <= 0 && DirectorRules.Distance(p.Pos.X, p.Pos.Y, x, y) < HitTiles)
                {
                    p.TakeDamage(ctx, st, def.Damage);
                    Recover(st, e);
                    return;
                }
            }
            if (st.T >= e.Until || DirectorRules.Distance(e.Pos.X, e.Pos.Y, e.Aim.X, e.Aim.Y) < StepTiles) Recover(st, e);
        }

        static void Recover(SimState st, Enemy e)
        {
            e.Phase = EnemyPhaseKind.Recover;
            e.Until = st.T + ChargeRecoverSeconds(e);
        }

        /// <summary>
        /// Reference walk.ts <c>canStand</c> for a body, not the engineer: every corner of the body square on
        /// <see cref="Ground.Passable"/> ground, so a shut door and the player's Gate both stop it.
        /// </summary>
        public static bool CanStand(SimContext ctx, SimState st, double x, double y)
        {
            const double r = BodyRadius;
            return Ground.Passable(ctx, st, (int)Math.Floor(x - r), (int)Math.Floor(y - r))
                && Ground.Passable(ctx, st, (int)Math.Floor(x + r), (int)Math.Floor(y - r))
                && Ground.Passable(ctx, st, (int)Math.Floor(x - r), (int)Math.Floor(y + r))
                && Ground.Passable(ctx, st, (int)Math.Floor(x + r), (int)Math.Floor(y + r));
        }

        /// <summary>
        /// <see cref="Enemies.Damage"/> calls this for every hit on a living body, after the hit points fall and
        /// before a dead body leaves the list. The hit that carries a guardian across <see cref="Phase2At"/> and
        /// leaves it alive wakes its arena.
        /// </summary>
        public static void Hurt(SimContext ctx, SimState st, Enemy e, double before)
        {
            if (!Is(e) || before <= Phase2At || e.Hp <= 0 || e.Hp > Phase2At) return;
            var def = StrongholdOf(e);
            var p = st.Engineer;
            var until = st.T + ctx.Data.Siege.MemoryS;
            var actors = st.Enemies.Actors;
            for (var i = 0; i < actors.Count; i++)
            {
                var o = actors[i];
                if (o.Hp <= 0 || string.CompareOrdinal(o.Site, e.Site) != 0) continue;
                o.LastKnown = p.Pos;
                o.LastKnownUntil = until;
                o.OnPlayer = true;
            }
            st.Events.Add(new GuardianEnragedEvent(st.T, e.Id, def?.Id ?? "", e.Pos.X, e.Pos.Y));
        }

        /// <summary>
        /// <see cref="Enemies.Damage"/> calls this once a dead body has left the list. A guardian leaves its
        /// stronghold's Power core where it fell (§4.3 <c>CoreDrop</c>).
        /// </summary>
        public static void Died(SimState st, Enemy e)
        {
            if (!Is(e)) return;
            var def = StrongholdOf(e);
            if (def == null) return;                          // an Admin spawn guards nothing and holds no core
            var enc = st.Encounters;
            if (!enc.Felled.Contains(def.Id)) enc.Felled.Add(def.Id);
            enc.Cores.Add(new LooseCore { Stronghold = def.Id, Pos = e.Pos });
            st.Events.Add(new GuardianKilledEvent(st.T, e.Id, def.Id, e.Pos.X, e.Pos.Y));
        }
    }
}
