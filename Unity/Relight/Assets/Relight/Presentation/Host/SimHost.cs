using System;
using System.Collections.Generic;
using System.Diagnostics;
using Relight.Sim;
using UnityEngine;

namespace Relight.Presentation
{
    /// <summary>
    /// B-04. The single owner of the simulation clock (TECHNICAL_ARCHITECTURE.md §4.2). Nothing else ticks the sim.
    /// Fixed 20 Hz step through <see cref="Simulation.Advance"/> (reference flow.ts advanceFlow: floor(acc + 1e-6),
    /// maxTicks clamp), the frame-gap guard of session.ts:258 (a NaN, negative or &gt; 0.25 s frame is discarded and the
    /// accumulator zeroed rather than fast-forwarded), and <see cref="Paused"/> as the ONLY clock control.
    /// There is no speed API: player time is 1× with pause (CLAUDE.md § UI implementation, U-D-04), and
    /// <c>Time.timeScale</c> is never touched — pause is a bool here, so UI animation and the camera keep running.
    /// Presentation interpolates with <see cref="Alpha"/>; the sim never sees a fractional tick.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Relight/Sim Host")]
    public sealed class SimHost : MonoBehaviour
    {
        /// <summary>Frame gaps above this are background stalls and are discarded (session.ts:258).</summary>
        public const float MaxFrameSeconds = 0.25f;

        [Tooltip("Log the measured per-tick CPU cost (ms) every N seconds; 0 disables. This is the tick CPU cost, " +
                 "reported separately from the fixed 50 ms tick interval (TECHNICAL_ARCHITECTURE.md §3.5, §10.1).")]
        [SerializeField, Min(0f)] private float tickCostLogIntervalSeconds = 0f;

        private readonly List<SimEvent> _frameEvents = new List<SimEvent>(64);
        private readonly Stopwatch _tickWatch = new Stopwatch();
        private bool _paused;
        private double _costAccumMs;
        private long _costTicks;
        private double _costMaxMs;
        private float _nextCostLog;

        /// <summary>The running simulation, or null before <see cref="StartNewGame"/> / <see cref="Attach"/>.</summary>
        public Simulation Simulation { get; private set; }

        /// <summary>
        /// The only clock control. Changing it zeroes the accumulator exactly as the reference clockPolicy does
        /// (session.ts:226): unpausing never releases a backlog of ticks.
        /// </summary>
        public bool Paused
        {
            get => _paused;
            set
            {
                if (_paused == value) return;
                _paused = value;
                Simulation?.ResetAccumulator();
            }
        }

        /// <summary>0..1 fraction of the next tick already elapsed — the presentation interpolation factor.</summary>
        public float Alpha => Simulation == null ? 0f : (float)Simulation.Accumulator;

        /// <summary>Events drained after this frame's ticks; valid until the next Update.</summary>
        public IReadOnlyList<SimEvent> LastFrameEvents => _frameEvents;

        /// <summary>Ticks run in the most recent Update.</summary>
        public int LastFrameTicks { get; private set; }

        /// <summary>Total ticks run by this host since it was (re)started.</summary>
        public long TotalTicks { get; private set; }

        /// <summary>Wall-clock seconds accepted by the gap guard since (re)start — the denominator of the timing check.</summary>
        public double AcceptedRealSeconds { get; private set; }

        /// <summary>Average CPU cost of one tick in ms since the last log/reset (tick cost, not tick interval).</summary>
        public double AverageTickCostMs => _costTicks == 0 ? 0 : _costAccumMs / _costTicks;

        /// <summary>Worst single-frame per-tick cost in ms since the last log/reset.</summary>
        public double MaxTickCostMs => _costMaxMs;

        /// <summary>
        /// Raised once per Update after the frame's ticks have run and before the next frame's commands are applied —
        /// the tick-boundary capture point autosave uses (TECHNICAL_ARCHITECTURE.md §9.4.3). Argument: ticks run.
        /// </summary>
        public event Action<int> TickBoundary;

        /// <summary>
        /// Raised whenever WHICH simulation is live changes — a new game, a loaded save adopted behind the same
        /// host, or <see cref="Detach"/> (argument: the new simulation, or null). Views must rebuild here: a
        /// loaded state can carry the same structural revision (<c>f.rev</c>) as the one it replaces, so a
        /// revision comparison alone would leave the previous session's views on screen. Raised after
        /// <see cref="Simulation"/> and <see cref="Session"/> have been updated.
        /// </summary>
        public event Action<Simulation> SessionChanged;

        /// <summary>
        /// Counts session changes: 0 before the first attach, then +1 on every attach/new game/detach. A view that
        /// cannot subscribe (it was disabled at the moment of the swap) can compare this instead.
        /// </summary>
        public int Session { get; private set; }

        /// <summary>Start a fresh campaign on this host.</summary>
        public Simulation StartNewGame(SimContext ctx, int seed) => Attach(Simulation.NewGame(ctx, seed));

        /// <summary>Attach an existing simulation (a loaded save wraps its state with Simulation.Wrap).</summary>
        public Simulation Attach(Simulation sim)
        {
            Simulation = sim ?? throw new ArgumentNullException(nameof(sim));
            _frameEvents.Clear();
            LastFrameTicks = 0;
            TotalTicks = 0;
            AcceptedRealSeconds = 0;
            ResetCostStats();
            Session++;
            SessionChanged?.Invoke(sim);
            return sim;
        }

        /// <summary>Detach the running simulation (the World scene is never reloaded; a load replaces the state).</summary>
        public void Detach()
        {
            var had = Simulation != null;
            Simulation = null;
            _frameEvents.Clear();
            if (!had) return;
            Session++;
            SessionChanged?.Invoke(null);
        }

        /// <summary>UI and input submit player actions here; they apply before the next frame's ticks, in order.</summary>
        public void Submit(Command c)
        {
            if (Simulation == null || c == null) return;
            Simulation.Queue(c);
        }

        private void Update()
        {
            if (Simulation == null) return;
            var dt = Time.unscaledDeltaTime;
            // session.ts:258 — discard background gaps rather than fast-forwarding through them.
            if (float.IsNaN(dt) || float.IsInfinity(dt) || dt < 0f || dt > MaxFrameSeconds)
            {
                dt = 0f;
                Simulation.ResetAccumulator();
            }
            AcceptedRealSeconds += dt;

            _tickWatch.Restart();
            // Paused: commands still apply (Advance(0) flushes the queue) but no tick runs, as the reference's speed 0.
            var n = Simulation.Advance(_paused ? 0.0 : dt);
            _tickWatch.Stop();

            LastFrameTicks = n;
            TotalTicks += n;
            _frameEvents.Clear();
            Simulation.DrainEvents(_frameEvents);

            if (n > 0)
            {
                var perTick = _tickWatch.Elapsed.TotalMilliseconds / n;
                _costAccumMs += perTick * n;
                _costTicks += n;
                if (perTick > _costMaxMs) _costMaxMs = perTick;
            }
            if (tickCostLogIntervalSeconds > 0f && Time.unscaledTime >= _nextCostLog)
            {
                _nextCostLog = Time.unscaledTime + tickCostLogIntervalSeconds;
                if (_costTicks > 0)
                    UnityEngine.Debug.Log($"SimHost tick cost: avg {AverageTickCostMs:F3} ms, max {MaxTickCostMs:F3} ms over {_costTicks} ticks (interval fixed at {Simulation.TickSeconds * 1000:F0} ms)");
                ResetCostStats();
            }

            TickBoundary?.Invoke(n);
        }

        /// <summary>Clear the tick-cost statistics window.</summary>
        public void ResetCostStats()
        {
            _costAccumMs = 0;
            _costTicks = 0;
            _costMaxMs = 0;
        }
    }
}
