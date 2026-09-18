using System;

namespace Relight.Sim
{
    /// <summary>
    /// What a minable tile yields. The reference reads this from authored world data — an HQ patch's type
    /// (<c>G.patch[t]</c>, flow.ts:697) or the district block's rubble item (<c>bg.rubble</c>, flow.ts:707) — but
    /// <see cref="ICityGeometry"/> (Phase B, B-08) only carries the tile CLASS, so there is nowhere to read it from.
    ///
    /// Rather than widen a shared interface another worker owns, the port asks the injected geometry for the item
    /// through this optional extra interface. A geometry that implements it decides; one that does not falls back to
    /// <see cref="Mining.FallbackItem"/>. W-A's exported riverfront geometry should implement it (see the W-C report).
    /// </summary>
    public interface IMinableGeometry
    {
        /// <summary>The item the authored tile at (x, y) yields, or false when it yields nothing.</summary>
        bool TryMinedItem(int x, int y, out ItemId item);
    }

    /// <summary>
    /// Hand mining, ported from reference flow.ts <c>rubbleAt</c> (683-712), <c>mineUnit</c> (718-736),
    /// <c>tickHand</c>'s mining half (1007-1023) and <c>setHandMine</c> (1460-1463).
    ///
    /// Retired with the block economy (CONTENT_CATALOGUE §17, U-D-32): block ownership and the HELD state, the
    /// per-block rubble pool that <c>mineUnit</c> drains alongside the tile, <c>st.patch.steel</c>, the campaign
    /// progression's extra resource rectangles, persistent sources (they need a powered Excavator) and the rail
    /// yard's coal counter. What is left is exactly the reference's tile arithmetic: one unit at a time, out of
    /// <see cref="GroundState"/>, into the pockets.
    /// </summary>
    public static class Mining
    {
        /// <summary>Reference flow.ts EPS in the <c>h.prog &gt;= 1 - EPS</c> test.</summary>
        public const double Eps = 1e-9;

        // ---- refusal texts, verbatim from reference game/src/worldScene.ts:215 -------------------------------
        public const string DownText = "Engineer down";
        public const string ReachText = "Move closer to mine";
        public const string EmptyText = "No rubble left here";
        public const string FullText = "Backpack full — make room";

        /// <summary>
        /// The item a tile yields when the geometry does not say (no <see cref="IMinableGeometry"/>).
        /// DELIBERATE DIFFERENCE, recorded in the W-C report: the reference's per-block authored item has no Phase B
        /// equivalent, so the tile class alone decides — rubble is scrap steel, a patch is copper, a deposit is coal.
        /// It is a pure function of the tile class, so it is deterministic and identical in every run.
        /// </summary>
        public static bool FallbackItem(TileClass k, out ItemId item)
        {
            switch (k)
            {
                case TileClass.Rubble: item = ItemId.Steel; return true;
                case TileClass.Patch: item = ItemId.Copper; return true;
                case TileClass.Deposit: item = ItemId.Coal; return true;
                default: item = ItemId.Steel; return false;
            }
        }

        /// <summary>
        /// Reference <c>rubbleAt</c>: what the tile holds for digging, after digging. False for ground, street,
        /// a cleared tile and anything off the map.
        /// </summary>
        public static bool TryTile(SimContext ctx, SimState st, int x, int y, out ItemId item, out double units)
        {
            item = ItemId.Steel;
            units = 0;
            if (!Ground.InBounds(ctx, x, y)) return false;
            var k = Ground.TileAt(ctx, st, x, y);      // a dug-out tile reads back as plain ground (ground.ts:508)
            if (!Tiles.CanHoldUnits(k)) return false;
            units = Ground.UnitsAt(ctx, st, x, y);
            if (units <= 0) return false;               // reference `if (units <= 0) return null`
            if (ctx.Geometry is IMinableGeometry mg && mg.TryMinedItem(x, y, out item)) return true;
            return FallbackItem(k, out item);
        }

        /// <summary>
        /// Why the hands cannot start on this tile, or "" when they can. The order is the reference HUD's
        /// (worldScene.ts:215): down, reach, nothing there, no room; the hand lock is the port's. Since U-D-44 that
        /// lock means a running core repair only — a queued workshop batch processes on its own and the engineer
        /// is free to keep mining while it does.
        /// </summary>
        public static string Problem(SimContext ctx, SimState st, int x, int y)
        {
            if (st.Engineer.IsDown) return DownText;
            if (HandCraft.HandLocked(st)) return HandCraft.LockTextFor(st);
            if (!Ground.InReach(ctx, st, x, y)) return ReachText;
            if (!TryTile(ctx, st, x, y, out var item, out _)) return EmptyText;
            if(item==ItemId.Crude)return "Extract crude oil with a Pumpjack";
            if(ctx.Geometry.Solid(x,y) || ProductionRules.MachineAt(st,x,y)!=null) return "Resource blocked by a structure";
            // Reference worldScene.ts:214 `room`: a slot that is empty, or a matching stack with slack.
            var trial = Pockets.Trial(st.Engineer);
            if (Pockets.Take(ctx.Data, trial, ItemKey.Of(item), 1) < 1) return FullText;
            return "";
        }

        /// <summary>
        /// Reference <c>setHandMine</c>: point the hands at a tile (progress always restarts), or refuse.
        /// A repeat of the tile already being mined is accepted and does NOT restart progress, so the presentation
        /// may re-send the command every frame while the button is held (worldScene.ts:766).
        /// </summary>
        public static (bool ok, string reason) Start(SimContext ctx, SimState st, int x, int y)
        {
            var problem = Problem(ctx, st, x, y);
            if (problem.Length > 0) return (false, problem);
            var e = st.Engineer;
            if (e.Mining && e.MineX == x && e.MineY == y) return (true, "");
            e.Mining = true;
            e.MineX = x;
            e.MineY = y;
            e.MineProg = 0;
            e.MineFull = false;
            return (true, "");
        }

        /// <summary>Reference <c>setHandMine(st, null)</c>: hands off, progress lost.</summary>
        public static void Stop(SimState st, string reason)
        {
            var e = st.Engineer;
            if (!e.Mining) return;
            e.Mining = false;
            e.MineProg = 0;
            st.Events.Add(new MiningStoppedEvent(st.T, e.MineX, e.MineY, reason ?? ""));
        }

        /// <summary>
        /// Reference <c>mineUnit</c>, tile arithmetic only: one unit out of the tile, the emptied tile recorded as
        /// dug, the counters bumped. The caller has already put the item in the pockets (the reference's order at
        /// flow.ts:1019-1021 — a Backpack that will not take it leaves the tile untouched).
        /// </summary>
        public static void TakeUnit(SimContext ctx, SimState st, int x, int y, ItemId item, double units, int machineId = -1)
        {
            var w = ctx.Geometry.Width;
            var left = units - 1;
            st.Ground.SetDug(y * w + x, w * ctx.Geometry.Height, left <= Eps ? 0 : left);
            st.Stats.Mined++;
            st.Stats.MinedOf.Add(item, 1);
            st.Events.Add(new MinedEvent(st.T, x, y, item, left <= Eps, machineId));
        }
    }

    /// <summary>
    /// Reference flow.ts <c>tickHand</c>, mining half (1007-1023). Registered in the Weapon composition sub-slot —
    /// the only slot W-C owns — rather than the Inventory slot beside <see cref="HandCraftPhase"/>; it therefore
    /// runs after production instead of before it. Nothing in the tick reads mining state, so the order is not
    /// observable; see the W-C report's request to move it when the Inventory slot's owner is next in that file.
    /// </summary>
    public sealed class MiningPhase : ITickPhase
    {
        public void Tick(SimContext ctx, SimState st, double dt)
        {
            var e = st.Engineer;
            if (!e.Mining) return;
            // A downed engineer digs nothing, and a running core repair keeps both hands busy (U-D-44: a queued
            // workshop batch does not — it is the workshop working, not the engineer).
            if (e.IsDown) { Mining.Stop(st, Mining.DownText); return; }
            if (HandCraft.HandLocked(st)) { Mining.Stop(st, HandCraft.LockTextFor(st)); return; }
            if (!Ground.InReach(ctx, st, e.MineX, e.MineY)) { Mining.Stop(st, Mining.ReachText); return; }
            if (!Mining.TryTile(ctx, st, e.MineX, e.MineY, out var item, out var units))
            {
                Mining.Stop(st, Mining.EmptyText);
                return;
            }

            e.MineProg += dt * ctx.Data.Engineer.HandMinePerS;
            if (e.MineProg < 1 - Mining.Eps) return;

            // flow.ts:1019 — the pockets are paid first; a Backpack with no room stops the hands, tile untouched.
            if (Pockets.Take(ctx.Data, e, ItemKey.Of(item), 1) < 1)
            {
                e.MineFull = true;
                Mining.Stop(st, Mining.FullText);
                return;
            }
            e.MineProg -= 1;
            Mining.TakeUnit(ctx, st, e.MineX, e.MineY, item, units);
        }
    }

    /// <summary>What the mining feedback presenter draws (C-02 presentation seam).</summary>
    public readonly struct MiningProgress
    {
        public bool Active { get; }
        public int X { get; }
        public int Y { get; }
        /// <summary>0..1 towards the next whole item.</summary>
        public double Fraction { get; }
        public ItemId Item { get; }
        /// <summary>Units left in the tile, after the part already dug.</summary>
        public double UnitsLeft { get; }
        /// <summary>Items a second at the current rate (GameData.Engineer.HandMinePerS).</summary>
        public double RatePerS { get; }
        /// <summary>The last stop was a full Backpack.</summary>
        public bool Full { get; }

        public MiningProgress(bool active, int x, int y, double fraction, ItemId item, double unitsLeft, double rate, bool full)
        {
            Active = active; X = x; Y = y; Fraction = fraction; Item = item; UnitsLeft = unitsLeft; RatePerS = rate; Full = full;
        }
    }

    public static class MiningQueries
    {
        /// <summary>The tile under the hands and how far the next item has come (reference worldScene.ts:217).</summary>
        public static MiningProgress Progress(SimContext ctx, SimState st)
        {
            var e = st.Engineer;
            var rate = ctx.Data.Engineer.HandMinePerS;
            if (!e.Mining || !Mining.TryTile(ctx, st, e.MineX, e.MineY, out var item, out var units))
                return new MiningProgress(false, e.MineX, e.MineY, 0, ItemId.Steel, 0, rate, e.MineFull);
            var f = Math.Max(0, Math.Min(1, e.MineProg));
            return new MiningProgress(true, e.MineX, e.MineY, f, item, units, rate, e.MineFull);
        }

        /// <summary>What a tile offers the hands, for the hover cursor: false when it is not minable at all.</summary>
        public static bool Minable(SimContext ctx, SimState st, int x, int y, out ItemId item) =>
            Mining.TryTile(ctx, st, x, y, out item, out _);
    }
}
