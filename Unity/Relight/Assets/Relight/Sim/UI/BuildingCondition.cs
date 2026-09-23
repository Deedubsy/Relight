namespace Relight.Sim.UI
{
    /// <summary>
    /// REL-133. What a building's CONDITION looks like on the building itself. Picture only, like
    /// <see cref="BuildingGlow"/>, <see cref="DarknessLook"/> and <see cref="LightSweep"/>: nothing here is read back
    /// into the simulation, nothing here is saved, and no phase calls it.
    ///
    /// <para>The owner's note: <i>"When a building is being repaired, it needs a little animation or something to
    /// show it's being repaired apart from just text"</i>. Repair already exists in full — <see cref="HomeState"/>
    /// carries the one repair that may be running and <see cref="HomeCorePhase"/> counts it down — so the whole of
    /// this issue is drawing, and the only thing the picture needed was somewhere to ask its two questions without a
    /// presenter reaching into <c>st.Home</c>'s fields itself.</para>
    ///
    /// <para><b>Why the answers are here and not in <c>HomeQueries</c>.</b> <c>HomeQueries.MachineRepairCard</c>
    /// answers the same question and more, and it is the right thing for the workshop card: it prices the repair,
    /// counts the instalments to full and works out what the command would refuse with. A presenter running every
    /// frame wants none of that, and <c>RepairProblem</c> alone walks the engineer's reach and pockets. These two are
    /// field reads.</para>
    /// </summary>
    public static class BuildingCondition
    {
        /// <summary>
        /// True while <paramref name="id"/> is the machine the running repair is working on. False when no repair is
        /// running, when the repair is the core's, and when it is another machine's — a repair is one at a time
        /// (<c>RepairProblem</c> refuses a second with "already repairing"), so at most one building draws this.
        /// </summary>
        public static bool RepairingMachine(SimState st, int id)
        {
            var h = st?.Home;
            return h != null && h.RepairKind == RepairKinds.Machine && h.RepairId == id;
        }

        /// <summary>True while the running repair is the Home core's, whether a patch or a recommission.</summary>
        public static bool RepairingCore(SimState st)
        {
            var h = st?.Home;
            return h != null && h.Placed && h.RepairKind == RepairKinds.Core;
        }

        /// <summary>
        /// How far through the running repair is: 0 the moment it starts, approaching 1 as it finishes, and 0 when
        /// nothing is being repaired.
        ///
        /// <para>The total is read from the tuning rather than from <see cref="HomeCore.Price"/>, because that is
        /// where <see cref="HomeCommands"/> takes it from when it sets <c>RepairRemaining</c> — the same two rows,
        /// chosen by the same <c>RepairRecommission</c> flag. Asking <c>Price</c> instead would re-read the target's
        /// hit points every frame to answer a question that does not depend on them, and would disagree with the
        /// countdown the instant a repair healed the core past the recommission threshold.</para>
        ///
        /// <para>A save written by an older build mid-repair, or one whose tuning has since changed, can hold a
        /// remaining time longer than the present total; the result is clamped rather than run backwards.</para>
        /// </summary>
        public static double RepairProgress(GameData d, SimState st)
        {
            var h = st?.Home;
            if (d == null || h == null || h.RepairKind == RepairKinds.None) return 0;
            var total = h.RepairKind == RepairKinds.Core && h.RepairRecommission
                ? d.Defence.CoreRepairSeconds
                : d.Defence.RepairSeconds;
            if (!(total > 0)) return 0;
            var done = 1 - h.RepairRemaining / total;
            return done < 0 ? 0 : done > 1 ? 1 : done;
        }
    }
}
