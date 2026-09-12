using System;
using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>
    /// The explicit public façade of the simulation and the only entry point the host, the UI and the tests use
    /// (TECHNICAL_ARCHITECTURE.md §2.2 / §4.2; MIGRATION_MAP.md S-03 corrects the reference's blanket
    /// <c>export *</c> surface — packages/sim/src/index.ts, 115 lines — to a deliberate one).
    ///
    /// It owns the tick driver (reference flow.ts <c>advanceFlow</c>, lines 1104–1120) and the transient tick
    /// accumulator, which is host state and deliberately not part of <see cref="SimState"/> (reference
    /// <c>st.acc</c> is one of the three <c>SAVE_TRANSIENT</c> fields).
    ///
    /// Player time is fixed 1× with pause (U-D-04): there is no speed field, no multiplier and no fast-forward.
    /// Pause is the host's business (B-04) — a paused host simply does not call <see cref="Advance"/>. A headless
    /// runner may call <see cref="Advance"/> or <see cref="Tick"/> as fast as it likes.
    /// </summary>
    public sealed class Simulation
    {
        public const int TicksPerSecond = 20;                        // reference flow.ts TILE_TPS
        public const double TickSeconds = 1.0 / TicksPerSecond;      // reference flow.ts TILE_DT

        /// <summary>
        /// Default clamp for one <see cref="Advance"/> call, in ticks: the reference's <c>maxTicks = 256</c> block
        /// ticks, i.e. <c>256 × TILE_TPS = 5120</c> tile ticks (TECHNICAL_ARCHITECTURE.md §10.1). The port has no
        /// block tick, so the cap is stated in the one unit that still exists.
        /// </summary>
        public const int MaxTicksPerCall = 256 * TicksPerSecond;

        public SimContext Context { get; }
        public SimState State { get; }

        /// <summary>Fractional tick accumulator, in ticks (reference <c>st.acc</c>, which is also in tile ticks).</summary>
        private double _acc;
        private readonly List<Command> _pending = new List<Command>();

        private Simulation(SimContext ctx, SimState state)
        {
            Context = ctx ?? throw new ArgumentNullException(nameof(ctx));
            State = state ?? throw new ArgumentNullException(nameof(state));
        }

        /// <summary>Accumulated fraction of a tick not yet run; 0..1 after a call. Presentation may use it to interpolate.</summary>
        public double Accumulator => _acc;

        /// <summary>Commands queued with <see cref="Queue"/> and not yet applied.</summary>
        public int PendingCount => _pending.Count;

        /// <summary>
        /// Drop the part-tick the accumulator is holding, so the next <see cref="Advance"/> starts from zero.
        /// The host calls this where the reference zeroes <c>st.acc</c> rather than letting a stale remainder
        /// survive a clock discontinuity: packages/game/src/session.ts:226 (<c>clockPolicy</c>, on a pause/unpause
        /// transition) and :258 (<c>frame</c>'s guard, when a background gap or a non-finite/negative/over-long
        /// real delta is discarded). No sim state changes — the accumulator is host-side, not saved (U-M-26).
        /// </summary>
        public void ResetAccumulator() => _acc = 0;

        /// <summary>
        /// A fresh campaign state: header fields first, then every registered initialiser in composition order
        /// (reference createCampaign / createEngineer / ensureFlow, split across the subsystems that own them).
        /// </summary>
        public static Simulation NewGame(SimContext ctx, int seed)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            var st = new SimState
            {
                Seed = seed,
                Rng = Prng.Seed(seed),
                T = 0,
                Tick = 0,
            };
            var inits = SimComposition.Initializers;
            for (var i = 0; i < inits.Count; i++) inits[i].Init(ctx, st);
            return new Simulation(ctx, st);
        }

        /// <summary>Wraps an already-built state (loading, B-11; scenario builders in the tests).</summary>
        public static Simulation Wrap(SimContext ctx, SimState state) => new Simulation(ctx, state);

        /// <summary>
        /// Apply one command now and return its result (reference session.ts <c>dispatch</c>, which flushes
        /// immediately and works while paused). A caller is by construction between ticks: nothing outside
        /// <see cref="Tick"/> runs inside one. Anything queued earlier is flushed first, in submission order.
        /// </summary>
        public CommandResult Apply(Command c)
        {
            FlushPending();
            return CommandDispatcher.Apply(Context, State, c);
        }

        /// <summary>
        /// Defer a command to just before the next tick (reference session.ts <c>queue</c> / flow.ts <c>f.pending</c>).
        /// </summary>
        public void Queue(Command c)
        {
            if (c != null) _pending.Add(c);
        }

        /// <summary>
        /// Run as many whole ticks as <paramref name="realSeconds"/> allows, applying queued commands first
        /// (reference <c>advanceFlow</c>). Returns the number of ticks run.
        ///
        /// <c>acc += realSeconds × 20; n = floor(acc + 1e-6)</c> — the epsilon is the reference's and is deliberate:
        /// "never lose a tick to rounding". Over the cap the accumulator is zeroed, exactly as the reference does,
        /// so a stall discards the backlog instead of fast-forwarding it.
        /// </summary>
        public int Advance(double realSeconds, int maxTicks = MaxTicksPerCall)
        {
            FlushPending();
            if (maxTicks < 0) maxTicks = 0;
            // The reference guards NaN/negative/long frame gaps in the host (session.ts:258); that guard is B-04's.
            // Here a non-finite or negative input is simply not allowed to poison the accumulator.
            if (double.IsNaN(realSeconds) || double.IsInfinity(realSeconds) || realSeconds < 0) realSeconds = 0;

            _acc += realSeconds * TicksPerSecond;
            var whole = Math.Floor(_acc + 1e-6);
            int n;
            if (whole > maxTicks) { n = maxTicks; _acc = 0; }
            else { n = whole > 0 ? (int)whole : 0; _acc -= n; }

            for (var k = 0; k < n; k++) TickInternal();
            return n;
        }

        /// <summary>One tick, for tests and headless runners. Does not touch the accumulator.</summary>
        public void Tick()
        {
            FlushPending();
            TickInternal();
        }

        private void TickInternal()
        {
            var phases = SimComposition.Phases;
            for (var i = 0; i < phases.Count; i++) phases[i].Tick(Context, State, TickSeconds);
            State.Tick++;
            State.T += TickSeconds;
        }

        private void FlushPending()
        {
            if (_pending.Count == 0) return;
            // Applied in submission order; a handler may queue more, which are taken on the next flush.
            var n = _pending.Count;
            for (var i = 0; i < n; i++) CommandDispatcher.Apply(Context, State, _pending[i]);
            _pending.RemoveRange(0, n);
        }

        /// <summary>
        /// Move this tick's events into <paramref name="into"/> and clear them (reference sim.ts <c>takeEvents</c>).
        /// Events are never saved and are never a persistence channel (TECHNICAL_ARCHITECTURE.md §4.3).
        /// </summary>
        public int DrainEvents(List<SimEvent> into)
        {
            var events = State.Events;
            var n = events.Count;
            if (into != null) for (var i = 0; i < n; i++) into.Add(events[i]);
            events.Clear();
            return n;
        }

        /// <summary>The Unity-canonical state hash (contracts H1/H2, TECHNICAL_ARCHITECTURE.md §10.4).</summary>
        public string Hash() => StateHash.Compute(State);
    }
}
