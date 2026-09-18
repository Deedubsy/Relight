using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>One body as presentation and the HUD see it. A plain value: no reference to the state is retained.</summary>
    public readonly struct EnemyView
    {
        public readonly bool Exists;
        public readonly int Id;
        public readonly string Kind;
        public readonly Vec2 Pos;
        public readonly Vec2 Heading;
        public readonly double Hp;
        public readonly double MaxHp;
        /// <summary><see cref="EnemyPhaseKind"/>; a presenter shows a tell during <c>Windup</c>.</summary>
        public readonly int Phase;
        /// <summary>The point a committed attack is aimed at (meaningful during <c>Windup</c> and <c>Charge</c>).</summary>
        public readonly Vec2 Aim;
        public readonly bool OnPlayer;
        public readonly bool Withdrawing;

        public EnemyView(bool exists, int id, string kind, Vec2 pos, Vec2 heading, double hp, double maxHp,
            int phase, Vec2 aim, bool onPlayer, bool withdrawing)
        {
            Exists = exists; Id = id; Kind = kind; Pos = pos; Heading = heading; Hp = hp; MaxHp = maxHp;
            Phase = phase; Aim = aim; OnPlayer = onPlayer; Withdrawing = withdrawing;
        }
    }

    /// <summary>Read-only selectors over <see cref="EnemyState"/>. Presentation never touches the list directly.</summary>
    public static class EnemyQueries
    {
        public static int Count(SimState st) => st.Enemies.Actors.Count;

        /// <summary>Living bodies in birth order, for a presenter that pools sprites by id.</summary>
        public static IReadOnlyList<Enemy> All(SimState st) => st.Enemies.Actors;

        /// <summary>Spitter globs in flight, for the presenter; empty most of the time.</summary>
        public static IReadOnlyList<EnemyProjectile> Projectiles(SimState st) => st.Enemies.Projectiles;

        /// <summary>Body centre of an enemy. False when the id is unknown (it died this tick or earlier).</summary>
        public static bool Position(SimState st, int id, out Vec2 pos)
        {
            var e = st.Enemies.Find(id);
            if (e == null) { pos = Vec2.Zero; return false; }
            pos = e.Pos;
            return true;
        }

        /// <summary>
        /// Reference threat.ts:366 <c>nearest</c>: the closest body within <paramref name="range"/> tiles of
        /// (x, y) that the point can actually see, by squared distance, scanning in birth order so ties break the
        /// same way every run. Returns its id, or 0 when nothing qualifies.
        ///
        /// The reference's extra "a shade is untargetable off lit ground" clause is not carried: shades are part of
        /// the retired legacy roster (CONTENT_CATALOGUE.md §17), so every body here is targetable.
        /// </summary>
        public static int Nearest(SimContext ctx, SimState st, double x, double y, double range)
        {
            var best = 0;
            var bd = range * range;
            for (var i = 0; i < st.Enemies.Actors.Count; i++)
            {
                var e = st.Enemies.Actors[i];
                var dx = e.Pos.X - x;
                var dy = e.Pos.Y - y;
                var d2 = dx * dx + dy * dy;
                if (d2 > bd) continue;
                if (!Sightline.Clear(ctx, st, x, y, e.Pos.X, e.Pos.Y)) continue;
                bd = d2;
                best = e.Id;
            }
            return best;
        }

        public static EnemyView View(SimContext ctx, SimState st, int id)
        {
            var e = st.Enemies.Find(id);
            if (e == null) return default;
            var max = ctx.Data.TryEnemy(e.Kind, out var def) ? def.Hp : e.Hp;
            return new EnemyView(true, e.Id, e.Kind, e.Pos, e.Dir, e.Hp, max, e.Phase, e.Aim, e.OnPlayer, e.Withdrawing);
        }

        /// <summary>How many bodies of one raid group are still alive (C-09 asks this to know when its wave is over).</summary>
        public static int GroupAlive(SimState st, int group)
        {
            var n = 0;
            for (var i = 0; i < st.Enemies.Actors.Count; i++) if (st.Enemies.Actors[i].Group == group) n++;
            return n;
        }

        /// <summary>The nearest living body to the engineer, or 0 — what the HUD threat marker points at.</summary>
        public static int NearestToEngineer(SimContext ctx, SimState st, double range) =>
            Nearest(ctx, st, st.Engineer.Pos.X, st.Engineer.Pos.Y, range);
    }
}
