using System;

namespace Relight.Sim
{
    /// <summary>The hand's queue at the workbench (reference flow.ts `FlowState.hand`, minus the retired mining fields).</summary>
    public sealed class HandState : IVisitable
    {
        /// <summary>Batches still queued, including the one in progress (reference `h.crafts`).</summary>
        public int Crafts;
        /// <summary>A batch has taken its ingredients and is running (reference `h.crafting`).</summary>
        public bool Crafting;
        /// <summary>Seconds into the running batch (reference `h.craftProg`).</summary>
        public double CraftProg;
        /// <summary>The finished batch is waiting because the Backpack is full (reference `h.full`).</summary>
        public bool Full;

        /// <summary>
        /// Steel a cancelled batch owes the engineer that the Backpack could not take yet.
        /// The reference pushes that overflow into the Depot's abstract stock (flow.ts:1477
        /// <c>st.stock[item] += n - back</c>), which the port retired with the block economy; U-M-31 item (2)
        /// let it stay counted as <c>consumed</c> instead, which quietly destroyed the plates. Superseded
        /// 2026-09-12: the debt is explicit and recoverable here, <see cref="Ledger.Held"/> counts it under
        /// <see cref="LedgerPlace.Pockets"/>, and <see cref="HandCraft.Tick"/> drains it into the pockets as
        /// space appears. Nothing is ever a hidden global inventory and nothing vanishes.
        /// </summary>
        public double RefundSteel;
        /// <summary>Copper a cancelled batch owes the engineer; see <see cref="RefundSteel"/>.</summary>
        public double RefundCopper;

        public void Visit(IStateVisitor v)
        {
            v.Field("crafts", ref Crafts);
            v.Field("crafting", ref Crafting);
            v.Field("craftProg", ref CraftProg);
            v.Field("full", ref Full);
            v.Field("refundSteel", ref RefundSteel);
            v.Field("refundCopper", ref RefundCopper);
        }
    }

    public sealed partial class SimState
    {
        /// <summary>The hand-craft queue (reference `f.hand`).</summary>
        public HandState Hand = new HandState();
    }

    /// <summary>Queue `Count` bullet batches at the workbench (reference flow.ts `queueCraft`, command `{type:'craft'}`).</summary>
    public sealed record HandCraftCommand(int Count = 1) : Command;

    /// <summary>Cancel the hand queue and return the running batch's ingredients (reference flow.ts `cancelCraft`).</summary>
    public sealed record CancelCraftCommand : Command;

    /// <summary>
    /// Hand crafting at the Depot workbench (reference flow.ts `tickHand` craft half, `handCraftCheck`,
    /// `queueCraft`, `cancelCraft`, `handLocked`, `HAND_LOCK_TEXT`).
    /// Hand MINING is not ported: it needs ground deposits, which arrive with Phase C.
    /// `ammoVersion` is always 1 in the port (U-D-08), so a batch is always HandBulletsPerCraft (10) bullets in
    /// HandBulletSeconds (20) seconds and the reference's ROUNDS_PER_MAG branches collapse away.
    /// </summary>
    public static class HandCraft
    {
        private const double Eps = 1e-9;

        /// <summary>Reference flow.ts:1467 HAND_LOCK_TEXT.</summary>
        public const string LockText = "Handcrafting — Cancel to move.";

        /// <summary>
        /// Reference `handLocked`: a running batch pins the engineer (the mover and the weapon read this).
        /// <see cref="EngineerMovementPhase"/> enforces it in the tick and <see cref="MovementCommandHandler"/>
        /// refuses the queued moves, exactly as reference walk.ts:181-183 does.
        /// </summary>
        public static bool HandLocked(SimState st) => st.Hand != null && st.Hand.Crafting && !st.Engineer.IsDown;

        /// <summary>
        /// Reference `nearDepot`. The reference measures the HQ lot rectangle; here it is "within reach of a placed
        /// machine of kind `depot`", because Phase B has no authored HQ lot in the sim (brief B-06 §4).
        /// With no depot placed at all, nothing is near one and every craft is refused.
        /// </summary>
        public static bool NearDepot(SimContext ctx, SimState st)
        {
            for (var i = 0; i < st.Machines.Count; i++)
            {
                var m = st.Machines[i];
                if (!string.Equals(m.Kind, "depot", StringComparison.Ordinal)) continue;
                if (Interaction.InReach(ctx, st, m)) return true;
            }
            return false;
        }

        /// <summary>Reference `handCraftCheck`: how many more batches the pockets can pay for, and why not.</summary>
        public static (int room, string reason) Check(SimContext ctx, SimState st)
        {
            var d = ctx.Data;
            if (!NearDepot(ctx, st)) return (0, "Walk closer to Home workshop");
            var e = st.Engineer;
            var steel = d.Engineer.HandBulletSteel;
            var copper = d.Engineer.HandBulletCopper;
            var afford = (int)Math.Min(Math.Floor(e.Inv[ItemId.Steel] / steel), Math.Floor(e.Inv[ItemId.Copper] / copper));
            var room = afford - (st.Hand.Crafts - (st.Hand.Crafting ? 1 : 0));
            var reason = room <= 0
                ? $"Need {steel} {d.Item(ItemId.Steel).DisplayName} + {copper} {d.Item(ItemId.Copper).DisplayName} in Backpack per ammunition batch"
                : "";
            return (room, reason);
        }

        /// <summary>Reference `queueCraft`: '' on success, else the refusal.</summary>
        public static string Queue(SimContext ctx, SimState st, int n)
        {
            if (n > 0)
            {
                var (room, reason) = Check(ctx, st);
                if (reason.Length > 0) return reason;
                n = Math.Min(n, room);
            }
            st.Hand.Crafts = Math.Max(0, st.Hand.Crafts + n);
            return "";
        }

        /// <summary>
        /// Reference `cancelCraft`. The running batch's reserved plates go back to the Backpack.
        /// Difference from the reference: what does not fit cannot go to the Depot stock (retired for Phase B),
        /// so it becomes an explicit debt on <see cref="HandState.RefundSteel"/> / <see cref="HandState.RefundCopper"/>
        /// instead — the FULL batch is un-counted from `consumed`, the pending refund is held by the ledger under
        /// <see cref="LedgerPlace.Pockets"/>, and <see cref="Drain"/> pays it back as Backpack space appears.
        /// The message says what actually came back and what is still waiting, never a flat "returned".
        /// </summary>
        public static (bool ok, string reason) Cancel(SimContext ctx, SimState st)
        {
            var h = st.Hand;
            if (!h.Crafting && h.Crafts <= 0) return (false, "Nothing is being crafted.");
            var d = ctx.Data;
            if (h.Crafting)
            {
                var steel = d.Engineer.HandBulletSteel;
                var copper = d.Engineer.HandBulletCopper;
                // The whole reservation stops being a sink the moment it is cancelled; where the plates end up
                // (pockets now, or the pending refund until there is room) is a question of place, not of amount.
                st.Stats.Consumed[ItemId.Steel] -= steel;
                st.Stats.Consumed[ItemId.Copper] -= copper;
                h.RefundSteel += steel;
                h.RefundCopper += copper;
            }
            h.Crafting = false;
            h.CraftProg = 0;
            h.Crafts = 0;
            h.Full = false;

            var owedSteel = h.RefundSteel;
            var owedCopper = h.RefundCopper;
            Drain(d, st);
            var backSteel = owedSteel - h.RefundSteel;
            var backCopper = owedCopper - h.RefundCopper;
            var steelName = d.Item(ItemId.Steel).DisplayName;
            var copperName = d.Item(ItemId.Copper).DisplayName;
            var refund = "";
            if (backSteel > 0 || backCopper > 0)
                refund += $" {backSteel} {steelName} + {backCopper} {copperName} returned.";
            if (h.RefundSteel > 0 || h.RefundCopper > 0)
                refund += $" {h.RefundSteel} {steelName} + {h.RefundCopper} {copperName} waiting for Backpack space.";
            return (true, $"Handcrafting cancelled.{refund}");
        }

        /// <summary>
        /// Pays the pending refund back into the pockets, as much of it as fits. Run at the top of every
        /// <see cref="Tick"/> and by <see cref="Cancel"/>, so freeing a Backpack slot recovers the plates within
        /// one tick. Until then the debt stays on <see cref="HandState"/> and the ledger counts it as held.
        /// </summary>
        private static void Drain(GameData d, SimState st)
        {
            var h = st.Hand;
            if (h.RefundSteel <= 0 && h.RefundCopper <= 0) return;
            var e = st.Engineer;
            if (h.RefundSteel > 0)
                h.RefundSteel = Math.Max(0, h.RefundSteel - Pockets.Take(d, e, ItemKey.Of(ItemId.Steel), h.RefundSteel));
            if (h.RefundCopper > 0)
                h.RefundCopper = Math.Max(0, h.RefundCopper - Pockets.Take(d, e, ItemKey.Of(ItemId.Copper), h.RefundCopper));
        }

        /// <summary>Reference `tickHand`, craft half only.</summary>
        public static void Tick(SimContext ctx, SimState st, double dt)
        {
            var d = ctx.Data;
            var h = st.Hand;
            var e = st.Engineer;
            Drain(d, st);   // a cancelled batch's plates come back the moment the Backpack has room for them
            var near = NearDepot(ctx, st);
            if (h.Crafting && (e.IsDown || !near)) Cancel(ctx, st);
            if (h.Crafts <= 0) return;
            if (!near) return;

            var steel = d.Engineer.HandBulletSteel;
            var copper = d.Engineer.HandBulletCopper;
            var batch = d.Engineer.HandBulletsPerCraft;
            var seconds = d.Engineer.HandBulletSeconds;

            if (!h.Crafting)
            {
                if (e.Inv[ItemId.Steel] >= steel && e.Inv[ItemId.Copper] >= copper)
                {
                    Pockets.Drop(d, e, ItemKey.Of(ItemId.Steel), steel);
                    Pockets.Drop(d, e, ItemKey.Of(ItemId.Copper), copper);
                    h.Crafting = true;
                    h.CraftProg = 0;
                    st.Stats.Consumed[ItemId.Steel] += steel;
                    st.Stats.Consumed[ItemId.Copper] += copper;
                }
                else { h.Crafts = 0; return; }
            }

            h.CraftProg += dt;
            if (h.CraftProg < seconds - Eps) return;

            var mag = ItemKey.Of(ItemId.Magazine);
            if (Pockets.Take(d, Pockets.Trial(e), mag, batch) == batch)
            {
                Pockets.Take(d, e, mag, batch);
                h.Crafting = false;
                h.Crafts--;
                h.CraftProg = 0;
                st.Stats.HandCrafted++;
                st.Stats.Made[ItemId.Magazine] += batch;
                st.Stats.MagsMade += batch;
            }
            else h.Full = true;
        }
    }

    public sealed class HandCraftPhase : ITickPhase
    {
        public void Tick(SimContext ctx, SimState st, double dt) => HandCraft.Tick(ctx, st, dt);
    }

    public sealed class HandCraftHandler : ICommandHandler
    {
        public bool TryApply(SimContext ctx, SimState st, Command c, out CommandResult result)
        {
            switch (c)
            {
                case HandCraftCommand q:
                {
                    var reason = HandCraft.Queue(ctx, st, q.Count);
                    result = reason.Length > 0 ? CommandResult.Refuse(reason) : CommandResult.Ok();
                    return true;
                }
                case CancelCraftCommand _:
                {
                    var (ok, reason) = HandCraft.Cancel(ctx, st);
                    result = ok ? CommandResult.Ok(reason) : CommandResult.Refuse(reason);
                    return true;
                }
                default:
                    result = default;
                    return false;
            }
        }
    }
}
