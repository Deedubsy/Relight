namespace Relight.Sim
{
    /// <summary>
    /// Bit-exact port of the reference PRNG (packages/sim/src/prng.ts, 49 lines; algorithm reproduced in
    /// TECHNICAL_ARCHITECTURE.md §10.2). mulberry32: one 32-bit word of state, held in <see cref="SimState.Rng"/>
    /// so it serialises as a plain number.
    ///
    /// Every step is 32-bit and wrapping. JavaScript's <c>Math.imul(a, b)</c> is a 32-bit multiply with wraparound,
    /// which is a plain <c>uint</c> multiply inside <c>unchecked</c>; <c>x &gt;&gt;&gt; k</c> is a <c>uint</c> shift;
    /// <c>x | 0</c> and <c>^</c> are ToInt32, i.e. mod 2^32 on the same bits. Holding the state as <c>uint</c> is
    /// therefore identical to the reference, which stores it alternately as a signed and an unsigned 32-bit value.
    ///
    /// Vectors generated from the reference live in Tests/Sim/Core/Fixtures/prng-vectors.txt.
    /// </summary>
    public static class Prng
    {
        /// <summary>Reference <c>seedRng</c>: the initial mulberry32 word for a game seed.</summary>
        public static uint Seed(int seed)
        {
            unchecked { return ((uint)seed * 0x9E3779B1u) ^ 0x2545F491u; }
        }

        /// <summary>Reference <c>rngNext</c>: advance the word and return a uniform double in [0, 1).</summary>
        public static double Next(ref uint state)
        {
            unchecked
            {
                var a = state + 0x6D2B79F5u;                       // (holder.rng + 0x6D2B79F5) | 0
                state = a;
                var t = (a ^ (a >> 15)) * (1u | a);                // Math.imul(a ^ (a >>> 15), 1 | a)
                t = ((t + ((t ^ (t >> 7)) * (61u | t))) ^ t);      // t + Math.imul(t ^ (t >>> 7), 61 | t) ^ t
                return (t ^ (t >> 14)) / 4294967296.0;
            }
        }

        /// <summary>Reference <c>rngInt</c>: <c>floor(next * n)</c>.</summary>
        public static int Int(ref uint state, int n) => (int)System.Math.Floor(Next(ref state) * n);

        /// <summary>Reference <c>rngUniform</c>: uniform in [lo, hi).</summary>
        public static double Uniform(ref uint state, double lo, double hi) => lo + (hi - lo) * Next(ref state);

        /// <summary>
        /// Reference <c>rngPick</c> has no counterpart in prng.ts (the reference picks with <c>rngInt</c> at the call
        /// site); this is that idiom, kept here so callers do not re-derive it. Refuses an empty range with -1.
        /// </summary>
        public static int PickIndex(ref uint state, int count) => count <= 0 ? -1 : Int(ref state, count);

        /// <summary>
        /// Reference <c>hash01</c>: a deterministic uniform in [0,1) from integers, documented as bit-identical to
        /// <c>hash01</c> in the original frontsim.py.
        /// </summary>
        public static double Hash01(int seed, int t, int x, int y)
        {
            unchecked
            {
                var h = (uint)seed ^ 0x9E3779B9u;
                h = (h ^ (uint)t) * 0x9E3779B1u; h ^= h >> 15;
                h = (h ^ (uint)x) * 0x9E3779B1u; h ^= h >> 15;
                h = (h ^ (uint)y) * 0x9E3779B1u; h ^= h >> 15;
                h ^= h >> 16;
                h *= 0x85EBCA6Bu; h ^= h >> 13;
                h *= 0xC2B2AE35u; h ^= h >> 16;
                return h / 4294967296.0;
            }
        }

        /// <summary>Reference <c>pyRound</c>: Python's <c>round()</c>, half to even.</summary>
        public static double PyRound(double v)
        {
            var f = System.Math.Floor(v);
            var r = v - f;
            if (r > 0.5) return f + 1;
            if (r < 0.5) return f;
            return f % 2 == 0 ? f : f + 1;
        }

        // ---- convenience overloads on the state's own word (the reference's RngHolder) ----

        public static double Next(SimState st) => Next(ref st.Rng);
        public static int Int(SimState st, int n) => Int(ref st.Rng, n);
        public static double Uniform(SimState st, double lo, double hi) => Uniform(ref st.Rng, lo, hi);
        public static int PickIndex(SimState st, int count) => PickIndex(ref st.Rng, count);
    }
}
