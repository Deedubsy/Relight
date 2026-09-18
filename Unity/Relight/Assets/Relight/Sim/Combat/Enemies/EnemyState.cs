using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>What an enemy is doing right now (reference gameplayCombat.ts <c>CombatActor.phase</c>).</summary>
    public static class EnemyPhaseKind
    {
        public const int Idle = 0;
        public const int Windup = 1;
        public const int Charge = 2;
        public const int Recover = 3;
    }

    /// <summary>Which schedule an enemy belongs to (reference <c>c.campaign.layer</c>).</summary>
    public static class EnemyLayer
    {
        public const int Minor = 0;
        public const int Major = 1;
        /// <summary>A camp resident: it guards its ruin and never walks to a base core.</summary>
        public const int Site = 2;
    }

    /// <summary>
    /// One living enemy: reference threat.ts <c>Crawler</c> (id, position, hp, stuck, dir, campaign meta) fused with
    /// gameplayCombat.ts <c>CombatActor</c> (kind, phase, until, aim, hesitate, patrol, home). The reference keeps
    /// them as two objects because the crawler predates the gameplay roster; the port has only the gameplay roster
    /// (legacy enemies.ts Crawler/Shade/Hulk is explicitly not carried), so one record says it all.
    ///
    /// Every field is visited: a save taken mid-windup or mid-charge resumes in the same phase with the same aim
    /// point, which is the C-08 acceptance check.
    /// </summary>
    public sealed class Enemy : IVisitable
    {
        public int Id;
        /// <summary>The <see cref="EnemyDef"/> key: "skitter" or "spitter".</summary>
        public string Kind = "";
        /// <summary>Body centre in tiles.</summary>
        public Vec2 Pos;
        public double Hp;
        /// <summary>Unit direction of its last move, for the view's heading (reference <c>c.dir</c>).</summary>
        public Vec2 Dir;
        /// <summary><see cref="EnemyLayer"/>.</summary>
        public int Layer;
        /// <summary>The raid group it was born into (reference <c>c.campaign.group</c>).</summary>
        public int Group;
        /// <summary>The tile index it entered by and withdraws to (reference <c>c.campaign.origin</c>).</summary>
        public int Origin;
        /// <summary>Where it was born, the centre of its patrol (reference <c>a.home</c>).</summary>
        public Vec2 Home;
        /// <summary><see cref="EnemyPhaseKind"/>.</summary>
        public int Phase;
        /// <summary>Sim time the current phase ends (reference <c>a.until</c>).</summary>
        public double Until;
        /// <summary>The point the committed attack was aimed at (reference <c>a.aim</c>).</summary>
        public Vec2 Aim;
        /// <summary>Seconds spent hesitating at lit ground (reference <c>a.hesitate</c>).</summary>
        public double Hesitate;
        /// <summary>Patrol step counter (reference <c>a.patrol</c>).</summary>
        public int Patrol;
        /// <summary>The tile it is walking into, -1 when it has none (reference <c>c.campaign.waypoint</c>).</summary>
        public int Waypoint = -1;
        /// <summary>
        /// It has turned for home and is walking, not fighting. For a raider that is its entry point and it will
        /// leave the map (reference <c>meta.withdrawing</c>); for a camp resident (GP-W5) it is its own camp, after
        /// the pursuit leash broke it off. The two never overlap — a body is one layer or the other for life — and
        /// both mean the same thing to the renderer and to <see cref="EnemyQueries"/>: this one is not coming for
        /// you right now.
        /// </summary>
        public bool Withdrawing;
        /// <summary>Seconds without progress (reference <c>c.stuck</c>).</summary>
        public double Stuck;
        /// <summary>It is chasing the engineer (reference <c>c.onPlayer</c>).</summary>
        public bool OnPlayer;
        /// <summary>Last seen engineer position and how long it is believed (reference <c>c.lastKnown</c>).</summary>
        public Vec2 LastKnown;
        public double LastKnownUntil;

        public void Visit(IStateVisitor v)
        {
            v.Field("id", ref Id);
            v.Field("kind", ref Kind);
            v.Field("pos", ref Pos);
            v.Field("hp", ref Hp);
            v.Field("dir", ref Dir);
            v.Field("layer", ref Layer);
            v.Field("group", ref Group);
            v.Field("origin", ref Origin);
            v.Field("home", ref Home);
            v.Field("phase", ref Phase);
            v.Field("until", ref Until);
            v.Field("aim", ref Aim);
            v.Field("hesitate", ref Hesitate);
            v.Field("patrol", ref Patrol);
            v.Field("waypoint", ref Waypoint);
            v.Field("withdrawing", ref Withdrawing);
            v.Field("stuck", ref Stuck);
            v.Field("onPlayer", ref OnPlayer);
            v.Field("lastKnown", ref LastKnown);
            v.Field("lastKnownUntil", ref LastKnownUntil);
        }
    }

    /// <summary>
    /// A spitter's glob in flight (reference gameplayCombat.ts <c>SpitProjectile</c>).
    ///
    /// A PARALLEL LIST, not W-C's <see cref="WeaponState.Projectiles"/>: that list is the engineer's, every entry is
    /// keyed by a <see cref="WeaponDef"/> profile and <see cref="Ballistics.TickProjectiles"/> resolves it against
    /// <see cref="IProjectileTargets"/> — which is the enemies themselves. A spitter glob has no weapon profile, is
    /// swept against machines and the engineer instead, and dies on any impassable tile. Mixing the two would make
    /// enemy globs damage enemies. Recorded in the W-B report, as the brief asks.
    /// </summary>
    public sealed class EnemyProjectile : IVisitable
    {
        public Vec2 Pos;
        /// <summary>Tiles per second (reference <c>p.vx, p.vy</c>); its magnitude is <c>RaidTuning.ProjectileSpeedTilesPerS</c>.</summary>
        public Vec2 Vel;
        /// <summary>Seconds left (reference <c>p.life</c>).</summary>
        public double Life;
        /// <summary>The enemy that spat it (reference <c>p.source</c>).</summary>
        public int Source;

        public void Visit(IStateVisitor v)
        {
            v.Field("pos", ref Pos);
            v.Field("vel", ref Vel);
            v.Field("life", ref Life);
            v.Field("source", ref Source);
        }
    }

    /// <summary>
    /// C-08's living bodies, visited as <c>enemies</c>. A fresh instance means an empty map, which is what a new
    /// campaign and an upgraded Phase B save both start from.
    /// </summary>
    public sealed class EnemyState : IVisitable
    {
        /// <summary>Living enemies in birth order — a list, never a dictionary, so ties break the same way every run.</summary>
        public List<Enemy> Actors = new List<Enemy>();
        public List<EnemyProjectile> Projectiles = new List<EnemyProjectile>();
        /// <summary>The id the next body takes (reference <c>T.next</c>, from 1); it also drives the birth roster.</summary>
        public int Next = 1;

        /// <summary>
        /// The <see cref="IProjectileTargets"/> seam handed to <see cref="WeaponState.Targets"/>. NOT visited: it is
        /// a view over <see cref="Actors"/>, rebuilt by <see cref="EnemyPhase"/> and the initialiser, and a load
        /// leaves it null until the next tick sets it again.
        /// </summary>
        public IProjectileTargets Seam;

        /// <summary>
        /// Tile → machine lookup shared by the BFS field, the breach checks and the swept spit collisions. NOT
        /// visited: it is a cache keyed on <see cref="SimState.Rev"/> (see <see cref="MachineIndex"/>).
        /// </summary>
        public readonly MachineIndex Index = new MachineIndex();

        public Enemy Find(int id)
        {
            for (var i = 0; i < Actors.Count; i++) if (Actors[i].Id == id) return Actors[i];
            return null;
        }

        public int IndexOf(int id)
        {
            for (var i = 0; i < Actors.Count; i++) if (Actors[i].Id == id) return i;
            return -1;
        }

        public void Visit(IStateVisitor v)
        {
            v.List("actors", Actors, () => new Enemy());
            v.List("projectiles", Projectiles, () => new EnemyProjectile());
            v.Field("next", ref Next);
        }
    }

    public sealed partial class SimState
    {
        /// <summary>Living enemies and their projectiles (C-08), visited as <c>enemies</c>.</summary>
        public EnemyState Enemies = new EnemyState();

        partial void VisitEnemies(IStateVisitor v) => v.Object("enemies", ref Enemies, () => new EnemyState());
    }
}
