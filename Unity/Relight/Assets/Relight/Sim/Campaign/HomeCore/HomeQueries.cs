using System;

namespace Relight.Sim
{
    /// <summary>
    /// Read-only views of the Home core for presentation and for the other subsystems that ask whether the core is
    /// still standing. Reference: campaignDefence.ts <c>repairCost</c>/<c>defenceDescription</c> and
    /// packages/game/src/inventoryPanel.ts <c>coreCard</c>. The strings the card shows ("Repair +40 HP · 4 s",
    /// "Recommission core · 12 s", "Repairing · N s left") are the UI's to format from these numbers (C-06).
    /// </summary>
    public static class HomeQueries
    {
        /// <summary>
        /// Is the core standing and commissioned? A core at 0 HP stops counting as commissioned for turrets and the
        /// opening encounter (reference: <c>damageCore</c> turns the block circuit off at hp 0).
        /// </summary>
        public static bool CoreOperational(SimState st) => st.Home != null && st.Home.Placed && st.Home.Hp > 0;

        /// <summary>True when the core is the synthetic 3x3 at the engineer's spawn because the region carried no core site.</summary>
        public static bool CoreIsFallback(SimState st) => st.Home != null && st.Home.Placed && st.Home.Fallback;

        public static double CoreHp(SimState st) => st.Home != null && st.Home.Placed ? st.Home.Hp : 0;

        /// <summary>Sim time the core was last knocked out, or -1 if never (reference <c>coreDisabledAt</c>).</summary>
        public static double CoreDisabledAt(SimState st) => st.Home?.DisabledAt ?? -1;

        /// <summary>The core's footprint rect in tiles; (0,0,0,0) before it is placed.</summary>
        public static (int X, int Y, int W, int H) CoreRect(SimState st)
        {
            var h = st.Home;
            return h != null && h.Placed ? (h.X, h.Y, h.W, h.H) : (0, 0, 0, 0);
        }

        /// <summary>Everything the workshop core card (C-06) needs, priced exactly once (reference <c>repairCost</c>).</summary>
        public static RepairCardView RepairCard(SimState st, GameData data)
        {
            var h = st.Home;
            var p = HomeCore.Price(data, st, RepairKinds.Core, -1);
            var e = st.Engineer;
            var canAfford = e.Inv[ItemId.Steel] >= p.Steel && e.Inv[ItemId.Copper] >= p.Copper;
            var inProgress = h != null && h.RepairKind == RepairKinds.Core;
            return new RepairCardView(RepairKinds.Core, p.Hp, p.Max, p.Steel, p.Copper, p.Seconds, p.Recommission,
                canAfford, inProgress, inProgress ? h.RepairRemaining : 0);
        }

        /// <summary>
        /// GP-W5. The same card as <see cref="RepairCard"/>, for a MACHINE instead of the core — the player-facing
        /// half of "a wreck is repairable".
        ///
        /// WHY THIS EXISTS. The sim has always been able to repair a machine: <c>RepairCommand(Machine, id)</c>
        /// prices it, charges it and heals it. Nothing ever SHOWED it. The core had a workshop card quoting its
        /// price; a knocked-out turret — and, since GP-W5 gave every buildable machine an integrity row, a
        /// knocked-out assembler, miner or conveyor — read "disabled (0 hp)" and stopped there, with no cost, no
        /// button and no statement that it could be brought back at all. A wreck that looks permanent is a wreck
        /// the player bulldozes and rebuilds from scratch, which is the expensive, wrong answer.
        ///
        /// WHERE THE REPAIR HAPPENS. At the machine, not at the Home workshop: <see cref="HomeCore.RepairProblem"/>
        /// requires reach of the machine's own footprint. The core is the exception, not the rule, and the HUD's
        /// remedy text was corrected to match.
        ///
        /// ONE PAYMENT RESTORES <c>Defence.RepairHp</c>, not the whole bar, so a wreck takes
        /// <see cref="MachineRepairView.RepairsToFull"/> of them. That number is on the card because it is the
        /// difference between "this is cheap" and "this is most of a rebuild", and the player should be able to
        /// decide which before paying the first instalment.
        /// </summary>
        public static MachineRepairView MachineRepairCard(SimContext ctx, SimState st, int id)
        {
            var d = ctx.Data;
            var h = st.Home;
            var p = HomeCore.Price(d, st, RepairKinds.Machine, id);
            var e = st.Engineer;
            var inProgress = h != null && h.RepairKind == RepairKinds.Machine && h.RepairId == id;
            var m = st.MachineById(id);
            var inReach = m != null && !e.IsDown && Interaction.InReach(ctx, st, m);
            var perRepair = d.Defence.RepairHp;
            var missing = Math.Max(0, p.Max - p.Hp);
            var toFull = perRepair > 0 ? (int)Math.Ceiling(missing / perRepair) : 0;
            return new MachineRepairView(id, p.Hp, p.Max, p.Steel, p.Copper, p.Seconds, perRepair, toFull,
                e.Inv[ItemId.Steel] >= p.Steel && e.Inv[ItemId.Copper] >= p.Copper, inReach, inProgress,
                inProgress ? h.RepairRemaining : 0,
                inProgress ? "" : HomeCore.RepairProblem(ctx, st, RepairKinds.Machine, id));
        }

        /// <summary>
        /// GP-W5. The one sentence a damaged or destroyed machine adds to its own description, so the world hover
        /// and the machine panel say the same thing without either of them inventing a second vocabulary. The state
        /// word stays <see cref="ProductionQueries.StateText"/>'s; this is only the repair half.
        /// Empty for a machine that is whole, or one that cannot be damaged at all.
        /// </summary>
        public static string MachineRepairLine(SimContext ctx, SimState st, int id)
        {
            var v = MachineRepairCard(ctx, st, id);
            // PackLayout.Num throughout: these numbers are shown to the player and must read the same on every
            // machine's locale, exactly as the Backpack's counts do.
            if (v.InProgress)
                return "Repairing — " + PackLayout.Num(Math.Ceiling(v.RemainingS)) + "s left (stay in reach)";
            if (v.Max <= 0 || v.Hp >= v.Max) return "";

            var text = (v.Wrecked
                    ? "DESTROYED — repairable: "
                    : "Damaged " + PackLayout.Num(Math.Ceiling(v.Hp)) + "/" + PackLayout.Num(Math.Ceiling(v.Max))
                      + " HP — repairable: ")
                + PackLayout.Num(v.Steel) + " steel + " + PackLayout.Num(v.Copper) + " copper restores "
                + PackLayout.Num(v.RepairHp) + " HP (" + PackLayout.Num(v.Seconds) + "s)";
            if (v.RepairsToFull > 1) text += " · " + PackLayout.Num(v.RepairsToFull) + " repairs to full";
            if (v.Problem.Length != 0) text += " · " + v.Problem;
            return text;
        }

        /// <summary>Reference <c>defenceDescription</c>, narrowed to the core: what the hover text says.</summary>
        public static string Description(SimState st, GameData data)
        {
            var h = st.Home;
            if (h == null || !h.Placed) return "";
            var p = HomeCore.Price(data, st, RepairKinds.Core, -1);
            var text = $"Base core · {Math.Ceiling(h.Hp)}/{p.Max} HP";
            if (h.Hp <= 0) text += " · DISABLED";
            if (h.RepairKind == RepairKinds.Core) return text + $" · repair {Math.Ceiling(h.RepairRemaining)}s (stay in reach)";
            if (h.Hp < p.Max) text += $" · E opens the Home workshop — repair it there ({p.Steel} steel + {p.Copper} copper, {p.Seconds}s{(p.Recommission ? "" : $" / {data.Defence.RepairHp} HP")})";
            return text;
        }
    }

    /// <summary>The workshop core card's data (brief C-05: kind, hp, max, steel, copper, seconds, recommission, canAfford, inProgress, remainingS).</summary>
    public readonly struct RepairCardView
    {
        public readonly int Kind;
        public readonly double Hp;
        public readonly double Max;
        public readonly int Steel;
        public readonly int Copper;
        public readonly double Seconds;
        public readonly bool Recommission;
        public readonly bool CanAfford;
        public readonly bool InProgress;
        public readonly double RemainingS;

        public RepairCardView(int kind, double hp, double max, int steel, int copper, double seconds,
            bool recommission, bool canAfford, bool inProgress, double remainingS)
        {
            Kind = kind; Hp = hp; Max = max; Steel = steel; Copper = copper; Seconds = seconds;
            Recommission = recommission; CanAfford = canAfford; InProgress = inProgress; RemainingS = remainingS;
        }
    }

    /// <summary>
    /// GP-W5's machine repair card (<see cref="HomeQueries.MachineRepairCard"/>). The same shape as
    /// <see cref="RepairCardView"/> plus the three things a machine needs and the core does not: whether it is in
    /// reach (the core's card is only ever open at the core), how many instalments a full repair takes, and the
    /// sim's own refusal for the button that is greyed.
    /// </summary>
    public readonly struct MachineRepairView
    {
        /// <summary>The machine this card is about.</summary>
        public readonly int Id;
        public readonly double Hp;
        /// <summary>Full hit points; 0 for a machine that cannot be damaged at all, which has no card.</summary>
        public readonly double Max;
        public readonly int Steel;
        public readonly int Copper;
        /// <summary>Seconds one instalment takes.</summary>
        public readonly double Seconds;
        /// <summary>Hit points one instalment restores (<c>Defence.RepairHp</c>).</summary>
        public readonly double RepairHp;
        /// <summary>Instalments from here to full, rounded up. 0 when nothing is missing.</summary>
        public readonly int RepairsToFull;
        public readonly bool CanAfford;
        /// <summary>The engineer is standing at it and is not down — repairs happen at the machine.</summary>
        public readonly bool InReach;
        /// <summary>A repair of THIS machine is running now.</summary>
        public readonly bool InProgress;
        public readonly double RemainingS;
        /// <summary>What <c>RepairCommand(Machine, Id)</c> would refuse with, or "" when it would be accepted.</summary>
        public readonly string Problem;

        /// <summary>It is a wreck: it has hit points, and none left. The gate the phases read is the same one.</summary>
        public bool Wrecked => Max > 0 && Hp <= 0;
        /// <summary>There is something to repair.</summary>
        public bool Damaged => Max > 0 && Hp < Max;

        public MachineRepairView(int id, double hp, double max, int steel, int copper, double seconds,
            double repairHp, int repairsToFull, bool canAfford, bool inReach, bool inProgress, double remainingS,
            string problem)
        {
            Id = id; Hp = hp; Max = max; Steel = steel; Copper = copper; Seconds = seconds;
            RepairHp = repairHp; RepairsToFull = repairsToFull; CanAfford = canAfford; InReach = inReach;
            InProgress = inProgress; RemainingS = remainingS; Problem = problem ?? "";
        }
    }
}
