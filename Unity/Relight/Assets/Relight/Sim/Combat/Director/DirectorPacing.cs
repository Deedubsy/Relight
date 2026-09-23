using System;

namespace Relight.Sim
{
    /// <summary>
    /// REL-75 (E-21): the raid pacing and fair-timing rules the director asks before it speaks.
    ///
    /// <list type="bullet">
    /// <item><b>Quiet spell</b> (U-D-64 d, U-P-17). After a large raid's recovery the map gets
    ///       <see cref="SiegeTuning.QuietAfterMajorS"/> with no warning and no small raid.</item>
    /// <item><b>One small raid per cycle</b> (U-D-64 d). A cycle runs from one large raid's start to the next one's.
    ///       After the quiet spell the next large-raid warning waits for the cycle's small raid to arrive, end and
    ///       clear <see cref="SiegeTuning.MinorMajorGapS"/>; it stops waiting
    ///       <see cref="SiegeTuning.MinorHoldCapS"/> after the quiet spell, so a small raid that cannot happen never
    ///       stops the clock (U-P-22).</item>
    /// <item><b>Fair timing</b> (U-D-66 (5)). A small raid does not start while the engineer is down or fighting a
    ///       camp, for at most <see cref="SiegeTuning.MinorDelayCapS"/> (U-P-23); one whose target is far from the
    ///       engineer gets <see cref="SiegeTuning.FarWarningExtraS"/> more warning (U-P-24); the first small raid
    ///       cannot take the Home core below its floor, and breaks off there (U-D-68 b, U-P-25).</item>
    /// <item><b>Strongholds</b> (U-D-69 e). A stronghold fight holds back a large-raid warning with no other time
    ///       limit. The rule is built behind one question, <see cref="StrongholdFight"/>, which
    ///       <see cref="StrongholdRules"/> answers since FRT-05.</item>
    /// </list>
    /// A hold is silent: the warning simply has not opened yet, so the HUD shows nothing and the player is told
    /// nothing they would have to un-learn. The large raid's own warning is always the full one.
    /// </summary>
    public static partial class DirectorPacing
    {
        /// <summary>
        /// U-D-69 (e): is a stronghold fight on? Answered by the Freight work, which owns strongholds
        /// (<see cref="StrongholdRules.FightOn"/>, FRT-05). On a map with none it stays false.
        /// </summary>
        static partial void StrongholdFightImpl(SimContext ctx, SimState st, ref bool on);

        /// <summary>A stronghold fight is on (U-D-69 e).</summary>
        public static bool StrongholdFight(SimContext ctx, SimState st)
        {
            var on = false;
            StrongholdFightImpl(ctx, st, ref on);
            return on;
        }

        /// <summary>
        /// The engineer is fighting a camp: a camp resident is chasing them and has not broken off. This is the
        /// "inside a camp fight" of U-D-66 (5), read from what the residents are doing rather than from where the
        /// engineer stands, so walking past a sleeping camp is not a fight.
        /// </summary>
        public static bool CampFight(SimState st)
        {
            var list = st.Enemies.Actors;
            for (var i = 0; i < list.Count; i++)
            {
                var e = list[i];
                if (e.Layer == EnemyLayer.Site && e.OnPlayer && !e.Withdrawing && e.Hp > 0) return true;
            }
            return false;
        }

        /// <summary>A small raid starting now would be unfair: the engineer is down, or fighting a camp or a stronghold.</summary>
        public static bool Unfair(SimContext ctx, SimState st) =>
            st.Engineer.IsDown || CampFight(st) || StrongholdFight(ctx, st);

        /// <summary>A large raid has ended at least once, so the one-small-raid-per-cycle rule applies.</summary>
        public static bool InCycle(SimState st) => st.Director.LastMajorEnd >= 0;

        /// <summary>
        /// Why the next large-raid warning must wait, or "" when it may open. The answer is for the director and
        /// the Admin readout; the player is never shown it.
        /// </summary>
        public static string WarningHold(SimContext ctx, SimState st)
        {
            var d = st.Director;
            var siege = ctx.Data.Siege;
            if (StrongholdFight(ctx, st)) return "a stronghold fight is on";
            if (st.T < d.QuietUntil) return "quiet spell after the last large raid";
            if (d.Minor != null && st.T < d.Minor.StartsAt + siege.MinorHoldCapS) return "a small raid is on";
            if (d.LastMinorEnd >= 0 && st.T < d.LastMinorEnd + siege.MinorMajorGapS) return "gap after the small raid";
            if (InCycle(st) && d.CycleMinors < siege.MinorsPerCycle && st.T < d.QuietUntil + siege.MinorHoldCapS)
                return "waiting for this cycle's small raid";
            return "";
        }

        /// <summary>
        /// The engineer is further than <see cref="SiegeTuning.FarTargetTiles"/> in a straight line from the centre
        /// of the raid's target, so a small raid on it gets the longer warning.
        /// </summary>
        public static bool Far(SimContext ctx, SimState st)
        {
            if (!DirectorRules.Target(ctx, st, out var x, out var y, out var tw, out var th)) return false;
            var dx = st.Engineer.Pos.X - (x + tw / 2.0);
            var dy = st.Engineer.Pos.Y - (y + th / 2.0);
            var far = ctx.Data.Siege.FarTargetTiles;
            return dx * dx + dy * dy > far * far;
        }

        /// <summary>The first small raid's floor in hit points: the core's full hit points times the tuned share.</summary>
        public static double FirstFloor(SimContext ctx, SimState st) =>
            EnemyCoreHook.Hp(ctx, st, out _, out var max) ? max * ctx.Data.Siege.FirstMinorCoreFloor : 0;

        /// <summary>
        /// The hit points the core cannot go below right now, or 0. Only a small raid that carries a floor sets one,
        /// and only while no large raid is under way: a large raid's damage is never softened by the small raid
        /// that happens to share the map with it.
        /// </summary>
        public static double CoreFloorHp(SimState st)
        {
            var d = st.Director;
            var m = d.Minor;
            if (m == null || m.Floor <= 0) return 0;
            if (d.Major != null && d.Major.Committed) return 0;
            return m.Floor;
        }

        /// <summary>
        /// The core has reached the first small raid's floor (U-D-68 b): the raiders leave and the HUD says the core
        /// held. Called by <see cref="EnemyCoreHook.Damage"/>; a raid already leaving is not told twice.
        /// </summary>
        public static void CoreHeld(SimContext ctx, SimState st)
        {
            var m = st.Director.Minor;
            if (m == null || m.Retreat) return;
            m.Retreat = true;
            m.Owed = 0;
            var from = m.Heading.Length > 0 ? " from " + m.Heading : "";
            Director.Say(ctx, st, "The core held. The raid" + from + " is breaking off.");
            st.Events.Add(new RaidNoticeEvent(st.T, RaidNoticeKind.CoreHeld, st.Director.Notice, m.Id));
        }
    }
}
