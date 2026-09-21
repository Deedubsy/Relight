using System;

namespace Relight.Sim
{
    /// <summary>
    /// GP-W4: the shape of a major assault.
    ///
    /// The old director emitted one flat roster — <see cref="RaidTuning.Total"/> identical bodies, evenly spaced
    /// across <see cref="RaidTuning.WindowS"/>, round-robining one approach per body. That is 1800 hp over 150 s,
    /// which is 12 hp/s, and spread over four rotating approaches only 3 hp/s per side: a single 10-dps gun turret
    /// on each side is three times the defence needed, so the encounter had no shape a player could read or
    /// prepare for. It also ignored the composition the exported tables already state
    /// (<see cref="RaidTuning.MajorSkitters"/>, <see cref="RaidTuning.MajorSpitters"/>) and always birthed through
    /// the legacy every-third-body rule.
    ///
    /// This class plans the assault instead, ONCE, at the moment it is scheduled, and the plan is saved. Spawning
    /// afterwards is a pure function of the saved plan plus two counters, so a save taken mid-assault resumes the
    /// same waves, from the same approaches, at the same absolute seconds — a wave can neither duplicate itself nor
    /// quietly vanish.
    ///
    /// Two things stay where they were. The HEAD COUNT and the skitter/spitter SPLIT come from the exported
    /// <see cref="RaidTuning"/>, which <c>exportCatalogue.ts</c> owns; only the shape — waves, spacing, how many
    /// approaches each wave uses — comes from this port's own <see cref="SiegeTuning"/>. And escalation follows the
    /// number of assaults the player has actually survived, never the machine count, so building more defence does
    /// not silently cancel the improvement (the brief's rule).
    /// </summary>
    public static class SiegePlan
    {
        /// <summary>Head count of a large raid booked after <paramref name="assaultsCompleted"/> finished ones.</summary>
        public static int TotalFor(SimContext ctx, int assaultsCompleted)
        {
            var s = ctx.Data.Siege;
            var growth = Math.Min(Math.Max(1.0, s.MajorGrowthCap),
                1.0 + Math.Max(0.0, s.MajorGrowthPerAssault) * Math.Max(0, assaultsCompleted));
            return Math.Max(Math.Max(1, s.MajorWaves), (int)Math.Round(ctx.Data.Raids.Total * growth));
        }

        /// <summary>
        /// Plans <paramref name="a"/>'s waves. <paramref name="a"/> must already carry its id, its start second and
        /// the approaches <c>Stageable</c> validated; everything else is filled in here.
        /// </summary>
        /// <param name="assaultsCompleted">Major assaults already finished, which is what escalation follows.</param>
        public static void Build(SimContext ctx, SimState st, MajorRaid a, int assaultsCompleted)
        {
            var r = ctx.Data.Raids;
            var s = ctx.Data.Siege;
            var waves = Math.Max(1, s.MajorWaves);
            var sides = a.Origins == null || a.Origins.Length == 0 ? 1 : a.Origins.Length;

            var total = TotalFor(ctx, assaultsCompleted);

            var count = new int[waves];
            var spitters = new int[waves];
            var start = new double[waves];
            var approaches = new int[waves];
            var offset = new int[waves];

            // Wave sizes by CUMULATIVE rounding of the weights, never by rounding each wave on its own: the parts
            // then always sum to the whole, so the roster the player is told about is the roster that arrives.
            var weightSum = 0.0;
            for (var i = 0; i < waves; i++) weightSum += Weight(s, i);
            var running = 0.0;
            var placed = 0;
            for (var i = 0; i < waves; i++)
            {
                running += Weight(s, i);
                var cum = i == waves - 1 || weightSum <= 0
                    ? total
                    : (int)Math.Round(total * running / weightSum);
                if (cum < placed + 1) cum = placed + 1;                  // every wave brings somebody
                if (cum > total - (waves - 1 - i)) cum = total - (waves - 1 - i);   // and leaves somebody for the rest
                count[i] = cum - placed;
                placed = cum;
            }

            // Spitters: the exported share of the roster, held out of the opening waves and then distributed by the
            // same cumulative rounding, so the split across the whole assault still matches the tables.
            var share = r.MajorCount > 0 ? (double)r.MajorSpitters / r.MajorCount : 0.0;
            var want = (int)Math.Round(total * share);
            if (want < 0) want = 0;
            if (want > total) want = total;
            var first = Math.Max(0, Math.Min(waves - 1, s.MajorSpitterWave));
            var capacity = 0;
            for (var i = first; i < waves; i++) capacity += count[i];
            if (want > capacity) want = capacity;
            var runCount = 0.0;
            var given = 0;
            for (var i = first; i < waves && capacity > 0; i++)
            {
                runCount += count[i];
                var cum = i == waves - 1 ? want : (int)Math.Round(want * runCount / capacity);
                if (cum > want) cum = want;
                if (cum < given) cum = given;
                var give = cum - given;
                if (give > count[i]) give = count[i];
                spitters[i] = give;
                given += give;
            }

            // Breakers (GP-W5): the approved heavy that actually threatens structures and the connections between
            // them. They are NOT a second roster — they are taken OUT of the skitters, so the head count the player
            // was warned about is still the head count that arrives and only its weight changes. They join late on
            // purpose: not at all until MajorBreakerAssault assaults have been survived, and then only from wave
            // MajorBreakerWave, so the first siege stays the teaching encounter and the escalation is legible.
            var breakers = new int[waves];
            var wantB = assaultsCompleted >= Math.Max(0, s.MajorBreakerAssault)
                ? (int)Math.Round(total * Math.Max(0.0, s.MajorBreakerShare))
                : 0;
            if (wantB > 0)
            {
                var firstB = Math.Max(0, Math.Min(waves - 1, s.MajorBreakerWave));
                var capB = 0;
                for (var i = firstB; i < waves; i++) capB += Math.Max(0, count[i] - spitters[i]);
                if (wantB > capB) wantB = capB;
                var runB = 0.0;
                var givenB = 0;
                for (var i = firstB; i < waves && capB > 0; i++)
                {
                    runB += Math.Max(0, count[i] - spitters[i]);
                    var cum = i == waves - 1 ? wantB : (int)Math.Round(wantB * runB / capB);
                    if (cum > wantB) cum = wantB;
                    if (cum < givenB) cum = givenB;
                    var give = cum - givenB;
                    if (give > count[i] - spitters[i]) give = count[i] - spitters[i];
                    if (give < 0) give = 0;
                    breakers[i] = give;
                    givenB += give;
                }
            }

            for (var i = 0; i < waves; i++)
            {
                start[i] = a.StartsAt + i * Math.Max(0.0, s.MajorWaveGapS);
                var k = s.MajorFirstApproaches + i * Math.Max(0, s.MajorApproachStep);
                if (k < 1) k = 1;
                if (k > sides) k = sides;
                if (k > count[i]) k = count[i];                          // never more sides than bodies to fill them
                approaches[i] = k;
                // Which side leads varies per assault and per wave, deterministically: the player learns to read the
                // warning rather than to memorise a compass point.
                offset[i] = sides <= 1 ? 0 : DirectorRules.RaidChoice(st.Seed, a.Id * 8 + i, sides - 1);
            }

            a.WaveCount = count;
            a.WaveSpitters = spitters;
            a.WaveBreakers = breakers;
            a.WaveStart = start;
            a.WaveApproaches = approaches;
            a.WaveOffset = offset;
            a.Wave = 0;
            a.WaveSpawned = 0;
            a.WaveAnnounced = 0;
            a.Total = total;
            a.Remaining = total;
            a.NextSpawn = a.StartsAt;
            a.EndsAt = a.StartsAt + Length(ctx, waves);
            a.Origin = Origin(a, 0, 0);
        }

        /// <summary>Relative size of wave <paramref name="i"/>. 0.5 growth weights four waves 1 : 1.5 : 2 : 2.5.</summary>
        private static double Weight(SiegeTuning s, int i) => 1.0 + Math.Max(0.0, s.MajorWaveGrowth) * i;

        /// <summary>
        /// How long an assault of <paramref name="waves"/> waves runs, from its start to the second it is declared
        /// over. The exported <see cref="RaidTuning.WindowS"/> is NOT this any more: it described a single flat
        /// stream, and a multi-wave assault is as long as its waves plus the grace its last bodies need to walk in.
        /// </summary>
        public static double Length(SimContext ctx, int waves)
        {
            var s = ctx.Data.Siege;
            var n = Math.Max(1, waves);
            return (n - 1) * Math.Max(0.0, s.MajorWaveGapS)
                + Math.Max(0.0, s.MajorWaveSpreadS)
                + Math.Max(0.0, s.MajorWaveTailS);
        }

        /// <summary>The planned length of <paramref name="a"/>, falling back to the tuned wave count if unplanned.</summary>
        public static double Length(SimContext ctx, MajorRaid a) =>
            Length(ctx, a != null && a.Waves > 0 ? a.Waves : ctx.Data.Siege.MajorWaves);

        /// <summary>True when <paramref name="a"/> carries a usable plan (an upgraded save may not).</summary>
        public static bool Planned(MajorRaid a) =>
            a != null && a.Waves > 0
            && a.WaveSpitters != null && a.WaveSpitters.Length == a.Waves
            && a.WaveBreakers != null && a.WaveBreakers.Length == a.Waves
            && a.WaveStart != null && a.WaveStart.Length == a.Waves
            && a.WaveApproaches != null && a.WaveApproaches.Length == a.Waves
            && a.WaveOffset != null && a.WaveOffset.Length == a.Waves;

        /// <summary>How many approaches wave <paramref name="w"/> uses, clamped to the approaches it actually has.</summary>
        public static int Approaches(MajorRaid a, int w)
        {
            var sides = a.Origins == null || a.Origins.Length == 0 ? 1 : a.Origins.Length;
            if (a.WaveApproaches == null || w < 0 || w >= a.WaveApproaches.Length) return 1;
            var k = a.WaveApproaches[w];
            if (k < 1) k = 1;
            if (k > sides) k = sides;
            return k;
        }

        /// <summary>
        /// The absolute second body <paramref name="index"/> of wave <paramref name="w"/> is due.
        ///
        /// A wave arrives in ROWS: one body per approach at the same second, then the next row a fraction of the
        /// spread later. That is what puts pressure on several positions at once rather than queueing the whole
        /// wave down one lane — the defence has to be in more than one place, which is the brief's requirement.
        /// </summary>
        public static double Due(SimContext ctx, MajorRaid a, int w, int index)
        {
            if (a.WaveStart == null || w < 0 || w >= a.WaveStart.Length) return a.StartsAt;
            var k = Approaches(a, w);
            var n = a.WaveCount != null && w < a.WaveCount.Length ? a.WaveCount[w] : 0;
            var rows = Math.Max(1, (n + k - 1) / k);
            var gap = Math.Max(0.0, ctx.Data.Siege.MajorWaveSpreadS) / rows;
            return a.WaveStart[w] + index / k * gap;
        }

        /// <summary>
        /// What body <paramref name="index"/> of wave <paramref name="w"/> is. Breakers lead, then skitters, then
        /// spitters, so the heavy ranged echelon is the back of each wave and the player sees the escalation
        /// arrive.
        ///
        /// A Breaker leads because it is SLOW — 1.0 tiles/s against a skitter's 2.0 (<see cref="Enemies.MarchSpeed"/>)
        /// — so putting it at the front of the wave is what makes it reach the defence with its escort rather than
        /// arriving alone, after the fight it was meant to be part of is already over.
        /// </summary>
        public static string Kind(MajorRaid a, int w, int index)
        {
            if (a.WaveCount == null || w < 0 || w >= a.WaveCount.Length) return "skitter";
            var n = a.WaveCount[w];
            var sp = a.WaveSpitters != null && w < a.WaveSpitters.Length ? a.WaveSpitters[w] : 0;
            var br = a.WaveBreakers != null && w < a.WaveBreakers.Length ? a.WaveBreakers[w] : 0;
            if (br > n - sp) br = Math.Max(0, n - sp);      // a plan is built so this cannot bite; a hand-made one may
            if (index < br) return "breaker";
            return index >= n - sp ? "spitter" : "skitter";
        }

        /// <summary>The approach tile body <paramref name="index"/> of wave <paramref name="w"/> walks in on.</summary>
        public static int Origin(MajorRaid a, int w, int index)
        {
            if (a.Origins == null || a.Origins.Length == 0) return a.Origin;
            var sides = a.Origins.Length;
            var k = Approaches(a, w);
            var off = a.WaveOffset != null && w < a.WaveOffset.Length ? a.WaveOffset[w] : 0;
            var slot = ((off + index % k) % sides + sides) % sides;
            return a.Origins[slot];
        }

        /// <summary>The approaches wave <paramref name="w"/> uses, in the order its first row fills them.</summary>
        public static int[] Sides(MajorRaid a, int w)
        {
            var k = Approaches(a, w);
            var list = new int[k];
            for (var i = 0; i < k; i++) list[i] = Origin(a, w, i);
            return list;
        }

        /// <summary>
        /// Give an assault that has no plan the smallest honest one: a single wave holding whatever it still owes,
        /// opening now, on every approach it was announced from. An upgraded save's in-flight assault is stamped
        /// this way by the schema-8 step, and this is the same shape for a <see cref="MajorRaid"/> built by hand.
        /// It is a floor, not a fallback the scheduler is allowed to reach: a scheduled assault is always planned.
        /// </summary>
        public static void Residual(MajorRaid a, double now)
        {
            if (a == null) return;
            var owed = Math.Max(0, a.Remaining);
            var sides = a.Origins == null || a.Origins.Length == 0 ? 1 : a.Origins.Length;
            a.WaveCount = new[] { owed };
            a.WaveSpitters = new[] { 0 };
            a.WaveBreakers = new[] { 0 };
            a.WaveStart = new[] { now };
            a.WaveApproaches = new[] { Math.Max(1, Math.Min(sides, Math.Max(1, owed))) };
            a.WaveOffset = new[] { 0 };
            a.Wave = 0;
            a.WaveSpawned = 0;
            a.WaveAnnounced = 1;
        }

        /// <summary>
        /// Move a planned but uncommitted assault to a new start second (<see cref="Director.Defer"/>). The plan is
        /// kept — the same waves, the same composition, the same approaches — and only slid along the clock, so a
        /// deferral does not quietly reroll the encounter the player was warned about.
        /// </summary>
        public static void Shift(SimContext ctx, MajorRaid a, double startsAt)
        {
            if (a == null) return;
            var delta = startsAt - a.StartsAt;
            a.StartsAt = startsAt;
            a.NextSpawn += delta;
            if (a.WaveStart != null)
                for (var i = 0; i < a.WaveStart.Length; i++) a.WaveStart[i] += delta;
            a.EndsAt = startsAt + Length(ctx, a);
        }
    }
}
