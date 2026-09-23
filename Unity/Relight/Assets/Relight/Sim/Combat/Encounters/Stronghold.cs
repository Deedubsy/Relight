using System;

namespace Relight.Sim
{
    /// <summary>
    /// A stronghold's doors opened (§5.4). <c>Text</c> is the HUD line ("Freight warehouse doors open").
    /// </summary>
    public sealed record StrongholdOpenedEvent(double T, string Id, string Text) : SimEvent(T);

    /// <summary>
    /// Batch 4, FRT-05 (REL-140): the locked doors and the fight clock of a stronghold (FREIGHT_STRONGHOLD_DESIGN
    /// §4.3, §5.4; U-D-69 (e)). Its garrison is an ordinary encounter row (<c>freight:arena</c>) run by
    /// <see cref="EncounterPhase"/>, which calls <see cref="Tick"/> after its own rows.
    ///
    /// <para><b>The doors.</b> Each <see cref="StrongholdDef.Doors"/> site is solid to everyone, engineer and bodies
    /// alike, until the stronghold is open: <see cref="GroundState.SolidMap"/> marks its tiles
    /// <see cref="GroundState.Blocked"/> and <see cref="DirectorRules.HostileOpen"/> refuses them. The two inside
    /// squads therefore stay inside with no rule of their own, because nothing can walk them out. Opening bumps
    /// <see cref="SimState.Rev"/>, so every cache built on the walk grid is rebuilt.</para>
    ///
    /// <para><b>Opening is a walk-up, not a command</b> (U-P-40). The reference opens the Freight doors when the
    /// engineer holds every key and stands within 3 tiles of an entrance (firstRegion.ts <c>openFreight</c>); the
    /// port does the same each tick. Both doors open together and stay open.</para>
    /// </summary>
    public static class StrongholdRules
    {
        /// <summary>The stronghold is in the loaded region: its arena site is.</summary>
        public static bool Present(SimContext ctx, StrongholdDef def) => ctx.Sites.Find(def.Arena) != null;

        /// <summary>The pouch holds every key <paramref name="def"/> needs.</summary>
        public static bool HoldsKeys(SimState st, StrongholdDef def)
        {
            for (var i = 0; i < def.Keys.Count; i++) if (!st.Encounters.Holds(def.Keys[i])) return false;
            return true;
        }

        /// <summary>How many of <paramref name="def"/>'s keys the pouch holds.</summary>
        public static int KeysHeld(SimState st, StrongholdDef def)
        {
            var n = 0;
            for (var i = 0; i < def.Keys.Count; i++) if (st.Encounters.Holds(def.Keys[i])) n++;
            return n;
        }

        /// <summary>Tile (x, y) is a door of a stronghold that is still shut.</summary>
        public static bool DoorShut(SimContext ctx, SimState st, int x, int y)
        {
            var all = EncounterCatalogue.Strongholds;
            for (var i = 0; i < all.Count; i++)
            {
                var def = all[i];
                if (st.Encounters.IsOpen(def.Id)) continue;
                for (var d = 0; d < def.Doors.Count; d++)
                {
                    var door = ctx.Sites.Find(def.Doors[d]);
                    if (door != null && door.Contains(x, y)) return true;
                }
            }
            return false;
        }

        /// <summary>Marks every shut door tile in <paramref name="solid"/>, the walk grid <see cref="GroundState.SolidMap"/> builds.</summary>
        public static void MarkShutDoors(SimContext ctx, SimState st, byte[] solid, byte mark)
        {
            var w = ctx.Geometry.Width;
            var h = ctx.Geometry.Height;
            var all = EncounterCatalogue.Strongholds;
            for (var i = 0; i < all.Count; i++)
            {
                var def = all[i];
                if (st.Encounters.IsOpen(def.Id)) continue;
                for (var d = 0; d < def.Doors.Count; d++)
                {
                    var r = ctx.Sites.Find(def.Doors[d]);
                    if (r == null) continue;
                    for (var y = r.Y; y < r.Y + r.H; y++)
                        for (var x = r.X; x < r.X + r.W; x++)
                            if (x >= 0 && y >= 0 && x < w && y < h) solid[y * w + x] = mark;
                }
            }
        }

        /// <summary>The engineer, up, is within <see cref="EncounterCatalogue.DoorReachTiles"/> of one of the doors.</summary>
        public static bool AtADoor(SimContext ctx, SimState st, StrongholdDef def)
        {
            var p = st.Engineer;
            if (p.IsDown) return false;
            for (var d = 0; d < def.Doors.Count; d++)
            {
                var r = ctx.Sites.Find(def.Doors[d]);
                if (r == null) continue;
                if (Reach.DistToRect(p.Pos.X, p.Pos.Y, r.X, r.Y, r.W, r.H) <= EncounterCatalogue.DoorReachTiles) return true;
            }
            return false;
        }

        /// <summary>
        /// U-D-69 (e): the engineer is fighting this stronghold now — up, its garrison born and not all dead, and
        /// either within <see cref="EncounterCatalogue.FightNearTiles"/> of the arena floor or chased by one of
        /// its bodies.
        /// </summary>
        public static bool Engaged(SimContext ctx, SimState st, StrongholdDef def)
        {
            var p = st.Engineer;
            if (p.IsDown) return false;
            var rec = st.Encounters.Find(def.Arena);
            if (rec == null || !rec.Resolved) return false;
            var floor = ctx.Sites.Find(def.Arena);
            if (floor == null) return false;
            var near = Reach.DistToRect(p.Pos.X, p.Pos.Y, floor.X, floor.Y, floor.W, floor.H) <= EncounterCatalogue.FightNearTiles;
            var actors = st.Enemies.Actors;
            for (var i = 0; i < actors.Count; i++)
            {
                var e = actors[i];
                if (e.Hp <= 0 || string.CompareOrdinal(e.Site, def.Arena) != 0) continue;
                if (near || (e.OnPlayer && !e.Withdrawing)) return true;
            }
            return false;
        }

        /// <summary>U-D-69 (e): a stronghold fight is on, or ended less than <see cref="EncounterCatalogue.FightTailSeconds"/> ago.</summary>
        public static bool FightOn(SimState st) => st.T < st.Encounters.FightUntil;

        /// <summary>The doors and the fight clock, once a tick, after the encounter rows.</summary>
        public static void Tick(SimContext ctx, SimState st)
        {
            var all = EncounterCatalogue.Strongholds;
            for (var i = 0; i < all.Count; i++)
            {
                var def = all[i];
                if (!Present(ctx, def)) continue;
                if (!st.Encounters.IsOpen(def.Id) && HoldsKeys(st, def) && AtADoor(ctx, st, def))
                {
                    st.Encounters.Opened.Add(def.Id);
                    st.Rev++;
                    st.Events.Add(new StrongholdOpenedEvent(st.T, def.Id, def.Name + " doors open"));
                }
                if (Engaged(ctx, st, def))
                    st.Encounters.FightUntil = Math.Max(st.Encounters.FightUntil, st.T + EncounterCatalogue.FightTailSeconds);
            }
        }
    }

    public static partial class DirectorPacing
    {
        static partial void StrongholdFightImpl(SimContext ctx, SimState st, ref bool on) => on = StrongholdRules.FightOn(st);
    }
}
