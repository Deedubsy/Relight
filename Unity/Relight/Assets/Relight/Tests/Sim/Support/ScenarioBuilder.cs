using System;
using System.Collections.Generic;

namespace Relight.Sim.Tests.Support
{
    /// <summary>
    /// The B-12 scenario builder: a short, fluent description of "this seed, this map, these commands at these
    /// ticks", which any edit-mode test can then run two ways — live through the host driver
    /// (<see cref="Play"/>, i.e. <c>Queue</c> + <c>Advance</c>, the path <c>SimHost</c> uses) or through
    /// <see cref="Replay.Run"/>. The idea (a fixture builder rather than a hand-written setup in every test) is the
    /// reference's <c>packages/sim/test/_city.ts</c> / <c>transportFixture.ts</c>; none of their content is ported,
    /// because the block lattice those fixtures build is retired (CONTENT_CATALOGUE.md §17).
    ///
    /// Defaults: seed 7, <see cref="SyntheticMap"/> geometry, <see cref="ReferenceData"/> data — the same three every
    /// other Phase B test folder already uses, so a scenario is comparable with the checks in Core/, Actor/ and World/.
    /// </summary>
    public sealed class ScenarioBuilder
    {
        private int _seed = 7;
        private ICityGeometry _geometry;
        private GameData _data;
        private ITileLayer _tiles;
        private IThreatLayer _threat;
        private readonly List<LoggedCommand> _log = new List<LoggedCommand>();
        private int _at;

        public static ScenarioBuilder New() => new ScenarioBuilder();

        public ScenarioBuilder WithSeed(int seed) { _seed = seed; return this; }
        public ScenarioBuilder WithGeometry(ICityGeometry geometry) { _geometry = geometry; return this; }
        public ScenarioBuilder WithData(GameData data) { _data = data; return this; }
        public ScenarioBuilder WithLayers(ITileLayer tiles = null, IThreatLayer threat = null)
        {
            _tiles = tiles;
            _threat = threat;
            return this;
        }

        /// <summary>Move the cursor to <paramref name="tick"/>. Ticks must not go backwards: <see cref="Replay"/> reads the log in order.</summary>
        public ScenarioBuilder At(int tick)
        {
            if (tick < _at) throw new ArgumentOutOfRangeException(nameof(tick), $"the log must be non-decreasing; {tick} follows {_at}");
            _at = tick;
            return this;
        }

        /// <summary>Log a command at the current cursor tick.</summary>
        public ScenarioBuilder Do(Command command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            _log.Add(new LoggedCommand(_at, command));
            return this;
        }

        /// <summary>Log a command at <paramref name="tick"/> (shorthand for <c>.At(tick).Do(c)</c>).</summary>
        public ScenarioBuilder Do(int tick, Command command) => At(tick).Do(command);

        public int Seed => _seed;

        public SimContext Context() =>
            new SimContext(_data ?? ReferenceData.Create(), _geometry ?? SyntheticMap.Create(), _tiles, _threat);

        /// <summary>A fresh simulation at tick 0 and the log, unapplied.</summary>
        public (Simulation Sim, List<LoggedCommand> Log) Build()
        {
            var sim = Simulation.NewGame(Context(), _seed);
            return (sim, new List<LoggedCommand>(_log));
        }

        /// <summary>
        /// Run the scenario "live" — the host's path: each due command is <see cref="Simulation.Queue"/>d and the
        /// clock is advanced one tick's worth of real time, so the accumulator and the pending queue are exercised.
        /// Deliberately <b>not</b> the same code as <see cref="Replay.Run"/> (which applies immediately and calls
        /// <see cref="Simulation.Tick"/>): a test that compares the two is comparing two drivers, not a function
        /// with itself. The command-versus-tick ordering is identical — a flush happens before the ticks of an
        /// <c>Advance</c> — which is exactly what the comparison asserts.
        /// </summary>
        public (Simulation Sim, List<LoggedCommand> Log) Play(int untilTick)
        {
            var (sim, log) = Build();
            var st = sim.State;
            var k = 0;
            while (st.Tick < untilTick)
            {
                while (k < log.Count && log[k].Tick <= st.Tick) sim.Queue(log[k++].Command);
                var ran = sim.Advance(Simulation.TickSeconds);
                if (ran != 1)
                    throw new InvalidOperationException(
                        $"one tick's worth of real time ran {ran} ticks at tick {st.Tick}: the accumulator drifted, " +
                        "and the live run can no longer be compared tick for tick with a replay.");
                st.Events.Clear();
            }
            while (k < log.Count && log[k].Tick <= st.Tick) sim.Apply(log[k++].Command);
            st.Events.Clear();
            return (sim, log);
        }
    }
}
