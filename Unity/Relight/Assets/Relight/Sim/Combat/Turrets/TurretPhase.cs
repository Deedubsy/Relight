using System;

namespace Relight.Sim
{
    /// <summary>
    /// One round left a turret's muzzle. <c>Angle</c> is the cannon heading it fired along and
    /// <c>TargetX/TargetY</c> the body it was aimed at, so the presenter can flash the muzzle and the tracer
    /// presenter can draw the line. The damage itself has already been applied.
    /// </summary>
    public sealed record TurretShotEvent(double T, int MachineId, double Angle, double TargetX, double TargetY, bool Hit) : SimEvent(T);

    /// <summary>
    /// L-02, ALWAYS_DARK_SPEC §5.7: a turret has started being damaged by something it cannot see into the dark
    /// (<see cref="TurretQueries.Blind"/>). Raised once per spell, on the change; the guide answers the first one.
    /// </summary>
    public sealed record TurretBlindEvent(double T, int MachineId, double X, double Y) : SimEvent(T);

    /// <summary>
    /// Turret tracking and firing, ported from the campaign branch of reference threat.ts <c>tickWeapons</c>
    /// (310-340) together with turretTracking.ts.
    ///
    /// The reference loop, kept line for line: the cooldown recovers at the circuit throttle so a brownout slows the
    /// gun (D-B3-4); a turret that is not running drops its target and resets its cooldown; an eligible target is
    /// retained rather than re-chosen; the muzzle stays where it fired while the flash is visible; the cannon must
    /// finish rotating before a bullet is spent; and the shot needs line of sight FROM THE MUZZLE, not from the
    /// centre.
    ///
    /// DELIBERATE DIFFERENCES.
    /// * The reference does <c>m.inv.rounds -= 1; m.out++</c>. Here only <c>Machine.Rounds</c> moves and
    ///   <c>Stats.Fired</c> is incremented: <see cref="Ledger"/> counts a turret's held magazines from
    ///   <c>Rounds</c> alone and ignores <c>Out</c> for turrets, and sinks <c>Stats.Fired</c>. Touching
    ///   <c>Out</c> as well would invent a magazine on every shot.
    /// * The reference's <c>litAt</c> shade rule is not ported: there are no shades in the C-08 roster
    ///   (gameplayCombat.ts skitter/spitter only).
    /// * TURRET SIGHT (U-D-59, ALWAYS_DARK_SPEC.md §5.7) is the port's own rule and has no reference counterpart:
    ///   a turret reaches a body standing on a LIT tile at its full range and one on an UNLIT tile only out to
    ///   <see cref="TurretDef.DarkSightTiles"/>. Both range tests below go through <see cref="TurretRules.Reach"/>,
    ///   so acquiring and keeping a target can never disagree. Lit is the sim's mask and nothing else: the
    ///   engineer's flashlight is a picture (D-UI-11) and changes none of this.
    /// * <c>opening.shots++</c> is C-09's counter; this phase raises <see cref="TurretShotEvent"/> and C-09 counts
    ///   the events, so the opening state machine stays in one file.
    ///
    /// TICK ORDER. The composition fixes Weapons → Turrets → Enemies → Director, so a turret sees the enemy
    /// positions from the END of the previous tick. The reference is the same shape: campaignThreat.ts's
    /// <c>tickCampaignThreat</c> moves the bodies and then calls <c>tickWeapons</c> at the end of the SAME tick, so
    /// there a turret sees this tick's positions. The difference is one tile tick (0.05 s) of lead at a walking
    /// speed of 2 tiles/s — 0.1 tiles — and it is stable rather than order-dependent. Recorded in the W-B report.
    /// </summary>
    public sealed class TurretPhase : ITickPhase
    {
        public void Tick(SimContext ctx, SimState st, double dt)
        {
            var d = ctx.Data;
            var turrets = st.Turrets;
            turrets.Prune(st);

            // Turret sight reads the lit mask, which LightPhase keeps current from the first tick on. A fixture or a
            // freshly loaded state that has never built one would read every tile as unlit, so build it once here.
            if (!st.Light.HasMask) LightPhase.Ensure(ctx, st);

            for (var i = 0; i < st.Machines.Count; i++)
            {
                var m = st.Machines[i];
                if (!d.TryTurret(m.Kind, out var def)) continue;

                var u = turrets.Of(m.Id);
                if (!u.Aimed) { u.Angle = TurretRules.InitialAngle(m); u.Aimed = true; }
                if (u.Flash > 0) u.Flash = Math.Max(0, u.Flash - dt);

                // The blind spell (§5.7). Only a turret hit in the last few seconds pays for the question.
                var blind = st.T - u.HitAt <= TurretQueries.BlindSeconds && TurretQueries.Blind(ctx, st, m.Id);
                if (blind && !u.Blind)
                    st.Events.Add(new TurretBlindEvent(st.T, m.Id, m.X + m.Size / 2.0, m.Y + m.Size / 2.0));
                u.Blind = blind;

                // A powered turret's cooldown recovers at its circuit's throttle (reference threat.ts:314).
                var throttle = def.PowerKw > 0 ? PowerQueries.Throttle(ctx, st, m.Id) : 1;
                u.Cool = Math.Max(-dt, u.Cool - dt * throttle);

                if (!Running(ctx, st, m, def)) { u.Target = 0; u.Cool = 0; continue; }

                var cx = m.X + m.Size / 2.0;
                var cy = m.Y + m.Size / 2.0;

                var target = Eligible(ctx, st, def, cx, cy, u.Target) ? u.Target : 0;
                if (target == 0) target = EnemyQueries.NearestInSight(ctx, st, cx, cy, def);
                if (target == 0) { u.Target = 0; u.Cool = 0; continue; }
                u.Target = target;

                // Keep the muzzle on its actual shot while the brief flash is visible (reference :327).
                if (u.Flash > 0) continue;

                if (!EnemyQueries.Position(st, target, out var at)) { u.Target = 0; u.Cool = 0; continue; }
                if (!TurretRules.Aim(u, m, def, at.X, at.Y, dt)) { u.Cool = Math.Max(0, u.Cool); continue; }

                var mx = cx + Math.Cos(u.Angle) * def.MuzzleTiles;
                var my = cy + Math.Sin(u.Angle) * def.MuzzleTiles;
                if (u.Cool > 1e-9 || m.Rounds < 1 || !Sightline.Clear(ctx, st, mx, my, at.X, at.Y)) continue;

                m.Rounds -= 1;                               // U-D-08: one item is one bullet
                st.Stats.Fired += 1;                         // the ledger's magazine sink
                u.Flash = def.ShotFlashS;
                u.Cool += 1 / Math.Max(1e-9, def.RoundsPerS);
                u.Shot = at;
                u.ShotT = st.T;

                Ballistics.NoteShot(st, mx, my, at.X, at.Y, true);   // the tracer W-C's TracerPresenter draws
                st.Events.Add(new TurretShotEvent(st.T, m.Id, u.Angle, at.X, at.Y, true));
                Enemies.Damage(ctx, st, target, def.DamagePerRound);
            }
        }

        /// <summary>
        /// Reference <c>machineRunning(st, m)</c> for a turret: it must have power (U-D-12 — an unsupplied turret
        /// holds fire) and it must not be disabled at 0 hp. Ammunition is checked at the trigger, not here, so an
        /// empty turret still tracks — which is what makes "out of ammunition" readable in the HUD.
        /// </summary>
        private static bool Running(SimContext ctx, SimState st, Machine m, TurretDef def)
        {
            if (TurretRules.Hp(ctx.Data, st, m) <= 0) return false;
            return def.PowerKw <= 0 || PowerQueries.Supplied(ctx, st, m.Id);
        }

        /// <summary>
        /// Reference <c>eligible</c>: in range and in sight of the turret's centre. Sight is
        /// <see cref="Sightline"/>, not bare geometry, so a wall the player built blinds the turret exactly as it
        /// blinds the engineer's rifle and exactly as the placement preview promised (GP-W3). Range is
        /// <see cref="TurretRules.Reach"/>, so a target that steps off lit ground beyond dark sight is dropped.
        /// </summary>
        private static bool Eligible(SimContext ctx, SimState st, TurretDef def, double cx, double cy, int id)
        {
            if (id == 0 || !EnemyQueries.Position(st, id, out var p)) return false;
            var dx = p.X - cx;
            var dy = p.Y - cy;
            if (Math.Sqrt(dx * dx + dy * dy) > TurretRules.Reach(st, def, p.X, p.Y)) return false;
            return Sightline.Clear(ctx, st, cx, cy, p.X, p.Y);
        }
    }
}
