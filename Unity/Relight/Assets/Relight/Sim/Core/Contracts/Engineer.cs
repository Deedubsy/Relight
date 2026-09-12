namespace Relight.Sim
{
    /// <summary>
    /// The engineer (reference types.ts `Engineer`, engineer.ts). Partial: the body and movement fields live here
    /// and are ticked by the movement subsystem (B-08, Actor/); the pockets and Backpack allocation are added by the
    /// inventory subsystem (B-06) in its own partial file and visited through <see cref="VisitInventory"/>.
    /// Retired legacy fields (block walking `dest/remaining/block`, kits, the abstract rifle) are not ported.
    /// </summary>
    public sealed partial class Engineer : IVisitable
    {
        /// <summary>Position in tile units, Y-down (reference `e.x`, `e.y`).</summary>
        public Vec2 Pos;
        /// <summary>Commanded velocity direction (reference `e.vel`); the mover scales it by speed.</summary>
        public Vec2 Vel;
        /// <summary>Click-to-move target or null (reference `e.target`).</summary>
        public bool HasTarget;
        public Vec2 Target;
        public Vec2 Face = new Vec2(1, 0);
        public bool HasAim;
        public Vec2 Aim;

        public double Hp;
        public double LastHit = -999;
        /// <summary>Sim second the engineer stands back up, or -1 when up (reference `e.down`).</summary>
        public double Down = -1;
        public int Downs;
        public double Hurt;

        public double Stamina = 1;
        public bool Sprint;
        public double Dash;
        public Vec2 DashDir = new Vec2(1, 0);
        public double DashCooldown;
        public double IFrames;
        public double Cooldown;
        public double Walked;

        /// <summary>
        /// Seconds in which an enemy was on the engineer (reference sim.ts:941 <c>player.danger++</c>, recorded by
        /// <see cref="SecondPhase"/>). The reference's hour-bucketed companion array is retired telemetry.
        /// </summary>
        public int DangerSeconds;
        /// <summary>Of those seconds, the ones in which the weapon fired (reference <c>player.dangerShot</c>).</summary>
        public int DangerShotSeconds;

        public bool IsDown => Down >= 0;

        public void Visit(IStateVisitor v)
        {
            v.Field("pos", ref Pos);
            v.Field("vel", ref Vel);
            v.Field("hasTarget", ref HasTarget);
            v.Field("target", ref Target);
            v.Field("face", ref Face);
            v.Field("hasAim", ref HasAim);
            v.Field("aim", ref Aim);
            v.Field("hp", ref Hp);
            v.Field("lastHit", ref LastHit);
            v.Field("down", ref Down);
            v.Field("downs", ref Downs);
            v.Field("hurt", ref Hurt);
            v.Field("stamina", ref Stamina);
            v.Field("sprint", ref Sprint);
            v.Field("dash", ref Dash);
            v.Field("dashDir", ref DashDir);
            v.Field("dashCooldown", ref DashCooldown);
            v.Field("iframes", ref IFrames);
            v.Field("cooldown", ref Cooldown);
            v.Field("walked", ref Walked);
            v.Field("dangerSeconds", ref DangerSeconds);
            v.Field("dangerShotSeconds", ref DangerShotSeconds);
            VisitInventory(v);   // B-06
            VisitEquipment(v);   // Phase C
        }

        partial void VisitInventory(IStateVisitor v);
        partial void VisitEquipment(IStateVisitor v);
    }
}
