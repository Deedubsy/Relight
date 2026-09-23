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

        /// <summary>
        /// REL-134. The fraction below which a building reads as HURT rather than whole, and the fraction below
        /// which it reads as CRITICAL. They live here, next to the reading itself, because the bar over the building
        /// and the bar in the inspect card must change colour at the same moment — two copies of 0.66 in two
        /// assemblies is exactly the kind of pair that drifts and is never noticed, because each one looks right on
        /// its own.
        ///
        /// <para>Both are the implementer's under U-D-28; the owner's note names no numbers. Recorded as U-P-35.</para>
        /// </summary>
        public const double HurtBelow = 0.66;
        public const double CriticalBelow = 0.30;

        /// <summary>
        /// REL-134. What a machine's hit points are, for drawing only.
        ///
        /// <para><b>Why <see cref="TurretRules"/> and not <see cref="HomeQueries.MachineRepairCard"/>.</b> The
        /// acceptance is that the bar and the text "can never disagree", and the text's own numbers come from
        /// <c>HomeCore.Price</c> → <c>HomeCore.MachineDefenceHp</c>, which IS
        /// <see cref="TurretRules.Hp"/> and <see cref="TurretRules.MaxHp"/> — the same two calls, one lookup nearer.
        /// Going through the card instead would price the repair, walk the engineer's reach and pockets and build a
        /// refusal string, every frame, for every damaged building on screen, to arrive at these same two doubles.
        /// <c>BuildingHealthTests</c> asserts the two agree rather than assuming it.</para>
        ///
        /// <para>A machine with no hit points at all — the Depot — reads <see cref="HealthReading.Exists"/> false
        /// and never grows a bar.</para>
        /// </summary>
        public static HealthReading Health(GameData d, SimState st, Machine m)
        {
            if (d == null || st == null || m == null) return default;
            var max = TurretRules.MaxHp(d, m);
            return max > 0 ? new HealthReading(TurretRules.Hp(d, st, m), max) : default;
        }

        /// <summary>
        /// REL-134. The same reading for the Home core, which is a rect on <see cref="HomeState"/> and not a
        /// <see cref="Machine"/> — the same pair <c>HomeCore.Price</c> returns for <see cref="RepairKinds.Core"/>,
        /// so the bar cannot disagree with the HUD's "Health 1400 / 2000" either.
        /// </summary>
        public static HealthReading CoreHealth(GameData d, SimState st)
        {
            var h = st?.Home;
            if (d == null || h == null || !h.Placed) return default;
            return new HealthReading(h.Hp, d.Defence.CoreHp);
        }
    }

    /// <summary>
    /// REL-134. One building's hit points and the three bands a picture reads them in. A <c>default</c> reading —
    /// no state, no machine, or a machine that cannot be damaged at all — has <see cref="Exists"/> false and is
    /// never damaged, so a caller that forgets to check draws nothing rather than drawing an empty bar.
    /// </summary>
    public readonly struct HealthReading
    {
        public readonly double Hp;
        public readonly double Max;

        public HealthReading(double hp, double max)
        {
            Hp = hp;
            Max = max;
        }

        /// <summary>This building can be damaged at all. False for the Depot, which has no integrity row.</summary>
        public bool Exists => Max > 0;

        /// <summary>
        /// Worth a bar. The owner's rule: <i>"When a building has lost health"</i> — so a base at full health
        /// sprouts nothing over every wall and pole, and one point of damage is enough to show one.
        /// </summary>
        public bool Damaged => Max > 0 && Hp < Max;

        /// <summary>Standing but doing nothing: <see cref="TurretRules.Wrecked"/>'s condition, without the lookup.</summary>
        public bool Wrecked => Max > 0 && Hp <= 0;

        /// <summary>0 to 1. Clamped at both ends, so a stale or over-healed figure cannot draw a bar off its track.</summary>
        public double Fraction => Max <= 0 ? 0 : Hp <= 0 ? 0 : Hp >= Max ? 1 : Hp / Max;

        /// <summary>Below <see cref="BuildingCondition.HurtBelow"/> and still standing.</summary>
        public bool Hurt => Max > 0 && Hp > 0 && Fraction < BuildingCondition.HurtBelow;

        /// <summary>Below <see cref="BuildingCondition.CriticalBelow"/>, or down. The reddest band.</summary>
        public bool Critical => Max > 0 && (Hp <= 0 || Fraction < BuildingCondition.CriticalBelow);
    }
}
