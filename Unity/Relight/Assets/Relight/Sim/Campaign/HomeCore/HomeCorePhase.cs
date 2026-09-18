using System;

namespace Relight.Sim
{
    /// <summary>
    /// Reference campaignDefence.ts <c>tickRepair</c> (line 134). Runs in the Campaign slot, before C-09's opening
    /// phase, so the encounter sees this tick's core HP.
    ///
    /// Ported line for line:
    /// * no repair → nothing to do (<c>if(!d||!r)return</c>);
    /// * the target vanished → the repair is dropped, no refund (<c>if(!target){d.repair=null;return;}</c>). The
    ///   payment stays counted in <c>Stats.SpentSteel/SpentCopper</c>, so the ledger still balances (U-D-05);
    /// * out of reach or down → paid progress pauses with no extra charge on resuming (line 138);
    /// * a disabled core with attackers present → paused (line 139), through <see cref="Home.AttackersNearby"/>;
    /// * <c>remaining</c> counts down by <c>dt</c> and only finishes below 1e-8 (line 140);
    /// * a non-recommission repair whose core was knocked out mid-work aborts with the reference's exact text
    ///   (line 143), raised as <see cref="CoreRepairAbortedEvent"/> because the port has no campaign notice string;
    /// * success: <c>hp = wasDisabled ? coreHp : min(coreHp, hp + repairHp)</c> (line 144).
    ///   The reference then re-energises the block (<c>subOn</c>/<c>HELD</c>) and re-nominates the base; the block
    ///   economy and the base roster are retired (U-D-32, single-core scope), so those two lines have no port.
    ///
    /// It also calls <see cref="HomeCore.Ensure"/> first, so a Phase B save upgraded to v3 — which arrives with a
    /// default <see cref="HomeState"/> — places its core on the first tick instead of throwing.
    /// </summary>
    public sealed class HomeCorePhase : ITickPhase
    {
        public void Tick(SimContext ctx, SimState st, double dt)
        {
            HomeCore.Ensure(ctx, st);

            var h = st.Home;
            if (h.RepairKind == RepairKinds.None) return;

            double tx, ty, tw, th;
            if (h.RepairKind == RepairKinds.Core)
            {
                if (!h.Placed) { Clear(h); return; }
                tx = h.X; ty = h.Y; tw = h.W; th = h.H;
            }
            else
            {
                var m = MachineById(st, h.RepairId);
                if (m == null) { Clear(h); return; }
                var (mw, mh) = m.Dimensions;
                tx = m.X; ty = m.Y; tw = mw; th = mh;
            }

            if (st.Engineer.IsDown || !Interaction.InReach(ctx, st, tx, ty, tw, th)) return;
            if (h.RepairKind == RepairKinds.Core && h.Hp <= 0 && Home.AttackersNearby(ctx, st)) return;

            h.RepairRemaining = Math.Max(0, h.RepairRemaining - dt);
            if (h.RepairRemaining > HomeCore.Eps) return;

            if (h.RepairKind == RepairKinds.Core)
            {
                var wasDisabled = h.Hp <= 0;
                if (wasDisabled && !h.RepairRecommission)
                {
                    Clear(h);
                    st.Events.Add(new CoreRepairAbortedEvent(st.T, "Core knocked out during repair; a full recovery kit is required."));
                    return;
                }
                var max = ctx.Data.Defence.CoreHp;
                h.Hp = wasDisabled ? max : Math.Min(max, h.Hp + ctx.Data.Defence.RepairHp);
                if (wasDisabled) h.DisabledAt = -1;
            }
            else
            {
                var applied = false;
                HomeCore.HealDefence(ctx, st, h.RepairId, ctx.Data.Defence.RepairHp, ref applied);
            }

            Clear(h);
            st.Rev++;
            st.Events.Add(new CoreRepairedEvent(st.T));
        }

        private static void Clear(HomeState h)
        {
            h.RepairKind = RepairKinds.None;
            h.RepairId = -1;
            h.RepairRemaining = 0;
            h.RepairRecommission = false;
            h.PaidSteel = 0;
            h.PaidCopper = 0;
        }

        internal static Machine MachineById(SimState st, int id)
        {
            for (var i = 0; i < st.Machines.Count; i++) if (st.Machines[i].Id == id) return st.Machines[i];
            return null;
        }
    }

    /// <summary>
    /// Reference <c>tickRepair</c> line 143's notice: the core fell to 0 while a patch repair was running, so the
    /// work is void and a full recovery kit is needed. The port has no campaign notice string, so the text travels
    /// as an event the HUD can show.
    /// </summary>
    public sealed record CoreRepairAbortedEvent(double T, string Text) : SimEvent(T);

    /// <summary>
    /// Places the core at new-game time (reference <c>initDefence</c>'s first <c>bases</c> entry). Idempotent, and
    /// <see cref="HomeCorePhase"/> repeats it defensively for upgraded saves.
    /// </summary>
    public sealed class HomeCoreInitializer : IStateInitializer
    {
        public void Init(SimContext ctx, SimState st)
        {
            HomeCore.Ensure(ctx, st);
            HomeCore.EnsureDepot(ctx, st);   // reference flow.ts:400: the Depot stands in the HQ lot from the start
        }
    }
}
