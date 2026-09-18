using System;
using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>
    /// A pile of cargo on the ground where the engineer went down (GP-W5). It is a PLACE, not a timer: it never
    /// despawns and nothing ever culls it, because a backpack can hold an Alien Key, a schematic or an artifact
    /// and progression must not be destroyed by a death. The walk back is the cost; the loss is not.
    /// </summary>
    public sealed class DropCache : IVisitable
    {
        /// <summary>Stable serial, so a panel's Collect button cannot slip onto a different pile.</summary>
        public int Id;
        /// <summary>The tile the pile occupies. Whole tiles, so two deaths on one spot are one pile, not two.</summary>
        public int X;
        public int Y;
        /// <summary>Sim seconds of the most recent death that added to this pile — the HUD's "how long ago".</summary>
        public double T;
        /// <summary>What is in the pile. The same bag type the pockets use, so a stack is a stack either side.</summary>
        public ItemBag Items = new ItemBag();

        public void Visit(IStateVisitor v)
        {
            v.Field("id", ref Id);
            v.Field("x", ref X);
            v.Field("y", ref Y);
            v.Field("t", ref T);
            v.Object("items", ref Items, () => new ItemBag());
        }
    }

    /// <summary>Every pile on the map, in the order they were first made, and the next serial.</summary>
    public sealed class DropState : IVisitable
    {
        public List<DropCache> Caches = new List<DropCache>();
        /// <summary>The serial the next pile takes. From 1, so 0 can mean "no pile chosen".</summary>
        public int NextId = 1;

        public void Visit(IStateVisitor v)
        {
            v.List("caches", Caches, () => new DropCache());
            v.Field("nextId", ref NextId);
        }
    }

    public sealed partial class SimState
    {
        /// <summary>Cargo the engineer dropped on dying, waiting on the ground to be walked back to (GP-W5).</summary>
        public DropState Drops = new DropState();
    }

    /// <summary>The engineer's cargo hit the ground at a tile (drained by the HUD, never saved).</summary>
    public sealed record CargoDroppedEvent(double T, int Id, int X, int Y) : SimEvent(T);

    /// <summary>
    /// Collect from a dropped pile in reach. <c>Id</c> 0 means the nearest pile in reach; a null <c>Item</c>
    /// means everything the Backpack will take.
    /// </summary>
    public sealed record CollectCacheCommand(int Id = 0, string Item = null, double Count = 0) : Command;

    /// <summary>
    /// Dying costs the engineer the walk back, not the cargo (GP-W5). This is the whole of that rule.
    ///
    /// WHAT DROPS. The carried items, and only those. Weapons are retained through death: an unequipped weapon is
    /// represented in the bag as its own key (<c>rifle:3</c>) while the instance lives in
    /// <see cref="WeaponState.Owned"/>, so "it is not in the backpack" is not true of the representation and the
    /// rule has to be enforced here — <see cref="ItemKey.IsWeapon"/> keys are skipped and stay in the pockets.
    /// Losing the rifle you just built to a skitter you never saw is the punishment that makes people stop
    /// exploring, which is the opposite of what this pass is for.
    ///
    /// CONSERVATION. Every unit moves exactly once, both ways. The spill uses <see cref="Pockets.Drop"/> and adds
    /// precisely what came out; the collect uses <see cref="Pockets.Take"/> and removes precisely what went in, so
    /// a Backpack with room for half a pile leaves the other half on the ground rather than eating it. Nothing is
    /// a source and nothing is a sink: <see cref="Ledger.Held"/> counts a pile where it stands, with the machines
    /// (the workshop output tray's precedent, U-D-44) — the four <see cref="LedgerPlace"/> slots are unchanged.
    /// Dying twice in one place merges into one pile, so repeated deaths cannot double anything.
    /// </summary>
    public static class DeathCache
    {
        private const double Eps = 1e-9;

        /// <summary>The refusal when there is a pile but the engineer is not standing at it.</summary>
        public const string ReachText = "Walk closer to the dropped cargo.";

        /// <summary>
        /// Put what the engineer was carrying on the ground at the tile they fell on. Called from
        /// <see cref="Engineer.TakeDamage"/> at the moment they go down, and from nowhere else.
        /// Returns the pile, or null when there was nothing to drop.
        /// </summary>
        public static DropCache Spill(SimContext ctx, SimState st, Engineer e)
        {
            if (e == null || st == null) return null;
            var drops = st.Drops;
            if (drops == null) { drops = new DropState(); st.Drops = drops; }

            var tx = (int)Math.Floor(e.Pos.X);
            var ty = (int)Math.Floor(e.Pos.Y);
            var keys = new List<ItemKey>();
            e.Inv.Keys(keys);                       // snapshot: the bag is emptied inside the loop

            DropCache cache = null;
            for (var i = 0; i < keys.Count; i++)
            {
                var key = keys[i];
                if (key.IsWeapon) continue;         // retained through death; see the class remarks
                var n = e.Inv[key];
                if (n <= 0) continue;
                var got = Pockets.Drop(ctx.Data, e, key, n);
                if (got <= 0) continue;
                cache ??= At(drops, tx, ty);
                cache.Items[key] = cache.Items[key] + got;
            }
            if (cache == null) return null;         // carrying nothing but a rifle: no pile, no marker, no litter
            cache.T = st.T;
            st.Events.Add(new CargoDroppedEvent(st.T, cache.Id, cache.X, cache.Y));
            return cache;
        }

        /// <summary>The pile on this tile, made now if there is not one yet.</summary>
        private static DropCache At(DropState drops, int x, int y)
        {
            for (var i = 0; i < drops.Caches.Count; i++)
            {
                var c = drops.Caches[i];
                if (c.X == x && c.Y == y) return c;
            }
            var made = new DropCache { Id = drops.NextId++, X = x, Y = y };
            drops.Caches.Add(made);
            return made;
        }

        /// <summary>
        /// Move a pile's contents into the Backpack. Modelled on <see cref="HandCraft.Collect"/>: what the
        /// Backpack will not take stays exactly where it is, and the pile is removed only once it is empty.
        /// </summary>
        public static (double moved, string reason) Collect(SimContext ctx, SimState st, int id = 0, string item = null, double count = 0)
        {
            var e = st.Engineer;
            if (e.IsDown) return (0, "Engineer down");
            var drops = st.Drops;
            if (drops == null || drops.Caches.Count == 0) return (0, "There is no dropped cargo to collect.");

            var cache = id > 0 ? Find(st, id) : NearestInReach(ctx, st);
            if (cache == null)
                return (0, id > 0 ? "That cargo is already collected." : "No dropped cargo within reach.");
            if (!InReach(ctx, st, cache)) return (0, ReachText);
            if (cache.Items.IsEmpty) { drops.Caches.Remove(cache); return (0, "That pile is empty."); }

            var d = ctx.Data;
            var keys = new List<ItemKey>();
            cache.Items.Keys(keys);
            double moved = 0;
            for (var i = 0; i < keys.Count; i++)
            {
                var key = keys[i];
                if (item != null && !string.Equals(key.Key, item, StringComparison.Ordinal)) continue;
                var want = count > 0 ? Math.Min(count - moved, cache.Items[key]) : cache.Items[key];
                if (want <= 0) continue;
                var got = Pockets.Take(d, e, key, want);
                if (got <= 0) continue;
                cache.Items[key] = cache.Items[key] - got;
                moved += got;
                if (count > 0 && moved >= count - Eps) break;
            }
            if (moved <= 0) return (0, item != null ? "Backpack has no room for that." : "Backpack is full.");
            if (cache.Items.IsEmpty) drops.Caches.Remove(cache);
            return (moved, "");
        }

        // ---- views (presentation reads these; it never writes a pile) ---------------------------------------

        public static DropCache Find(SimState st, int id)
        {
            var drops = st?.Drops;
            if (drops == null || id <= 0) return null;
            for (var i = 0; i < drops.Caches.Count; i++) if (drops.Caches[i].Id == id) return drops.Caches[i];
            return null;
        }

        /// <summary>Reach of a pile is reach of its tile — the same rule as reaching a machine.</summary>
        public static bool InReach(SimContext ctx, SimState st, DropCache c) =>
            c != null && Interaction.InReach(ctx, st, c.X, c.Y, 1, 1);

        /// <summary>The nearest pile with something in it that the engineer could collect from where they stand.</summary>
        public static DropCache NearestInReach(SimContext ctx, SimState st)
        {
            var drops = st?.Drops;
            if (drops == null) return null;
            DropCache best = null;
            var bestD = double.MaxValue;
            var p = st.Engineer.Pos;
            for (var i = 0; i < drops.Caches.Count; i++)
            {
                var c = drops.Caches[i];
                if (c.Items.IsEmpty || !InReach(ctx, st, c)) continue;
                var dx = c.X + 0.5 - p.X;
                var dy = c.Y + 0.5 - p.Y;
                var dist = dx * dx + dy * dy;
                if (dist >= bestD) continue;
                bestD = dist;
                best = c;
            }
            return best;
        }
    }

    public sealed class DeathCacheHandler : ICommandHandler
    {
        public bool TryApply(SimContext ctx, SimState st, Command c, out CommandResult result)
        {
            if (c is CollectCacheCommand g)
            {
                var (moved, reason) = DeathCache.Collect(ctx, st, g.Id, g.Item, g.Count);
                result = moved > 0
                    ? CommandResult.Ok("Recovered " + PackLayout.Num(moved) + " from the dropped cargo.")
                    : CommandResult.Refuse(reason);
                return true;
            }
            result = default;
            return false;
        }
    }
}
