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

        /// <summary>
        /// The context every tick and command runs against. Replaced as a whole by <see cref="ReplaceData"/>
        /// (REL-84 tuning reload); never mutated in place.
        /// </summary>
        public SimContext Context { get; private set; }
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

        /// <summary>
        /// Wraps an already-built state (loading, B-11; scenario builders in the tests). The lit mask is never saved
        /// and a loaded state runs no initialisers, so it is built here, once, for every caller alike (REL-9): a
        /// loaded game is not dark until its first tick, and no loader outside the sim has to build it.
        ///
        /// REL-62 (ENM-07): the ballistics seam is rebuilt here for the same reason. It is never saved either, and
        /// <see cref="EnemyPhase"/> re-points it every tick — but the tick order is Weapons → Turrets → Enemies, so
        /// on the FIRST tick after a load the player's own shots looked out at a null target list and passed through
        /// every body on the map. One tick is 50 ms and it took a load to see it, which is why it survived: the
        /// enemy tests set the seam by hand and never met the seam they were hiding.
        ///
        /// REL-62 (PER-02): this is also the one place that knows a state was RESUMED rather than started, so it is
        /// where <see cref="SimState.Resumed"/> is raised. The flag is transient and no phase reads it; it is for the
        /// HUD rows that live in the session and have to re-derive themselves once after a load.
        /// </summary>
        public static Simulation Wrap(SimContext ctx, SimState state)
        {
            if (ctx != null && state != null)
            {
                LightPhase.Ensure(ctx, state);
                EnemyTargets.Point(state);
                state.Resumed = true;
            }
            return new Simulation(ctx, state);
        }

        /// <summary>
        /// REL-84 (CMB-09b, E-19): swap the balance data under a running game without a restart. The geometry,
        /// layers, sites and map id stay; only <see cref="SimContext.Data"/> changes, and it changes between ticks
        /// (a caller is by construction outside <see cref="Tick"/>). Every phase and handler reads the context it
        /// is handed on each call and caches nothing across ticks, so the next tick simply runs on the new numbers.
        ///
        /// Tuning values are data, not save state: nothing in <see cref="SimState"/> is touched, and a save written
        /// afterwards records the new data hash, so a load against the old data gives the existing mismatch
        /// warning only. A <see cref="DataReloadedEvent"/> tells the HUD and the log; like every event it is never
        /// saved. Returns false, and changes nothing, when the data is the same object already in use.
        /// </summary>
        public bool ReplaceData(GameData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (ReferenceEquals(data, Context.Data)) return false;
            var was = Context;
            Context = new SimContext(data, was.Geometry, was.Tiles, was.Threat, was.Sites, was.MapId);
            State.Events.Add(new DataReloadedEvent(State.T, GameDataHash.Compute(was.Data), GameDataHash.Compute(data)));
            return true;
        }

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
            for (var i = 0; i < n; i++)
            {
                var c = _pending[i];
                var r = CommandDispatcher.Apply(Context, State, c);
                // C-13: a queued command has no caller to return to, so anything it has to SAY becomes an event
                // (the reference toasts every dispatch result). Silent acceptance says nothing: the held intents
                // — walk, move, aim — are submitted every frame and would otherwise flood the list.
                if (r.Problem.Length > 0)
                    State.Events.Add(new CommandResultEvent(State.T, c.GetType().Name, r.Accepted, r.Problem));
            }
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
