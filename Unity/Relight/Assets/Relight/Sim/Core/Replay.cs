using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>
    /// One command with the tick it was issued on (reference hour.ts:1469
    /// <c>export interface LoggedCommand { tick: number; c: Command }</c>, the same shape the reference save carries
    /// in <c>SaveFile.log</c>, save.ts:85–88).
    /// </summary>
    public sealed record LoggedCommand(int Tick, Command Command);

    /// <summary>
    /// The determinism check helper (MIGRATION_MAP.md S-36/S-60: <b>keep <c>replay</c></b>, retire the hour bot).
    /// Ported from reference hour.ts <c>replay</c>, lines 1476–1499 — that function and nothing else from the file.
    ///
    /// <b>Ordering rule</b> (reference hour.ts:1481–1488 with flow.ts:1104–1120): while the state's tick is below
    /// <paramref name="untilTick"/>, every log entry whose tick is <c>&lt;=</c> the state's current tick is applied
    /// <b>first</b> — <c>advanceFlow</c> runs <c>applyCommands</c> before it steps — and only then is one tick run.
    /// So a command logged at tick N is applied while <c>State.Tick == N</c>, immediately <i>before</i> tick N is
    /// stepped, and an entry whose tick has already passed still applies at the next opportunity rather than being
    /// dropped. When the target tick is reached, the entries at that boundary are applied without stepping again
    /// (hour.ts:1489–1497: "UI commands can be committed while paused, including at the final tick"). The log must be
    /// in non-decreasing tick order, which is how it is recorded (hour.ts:1542).
    ///
    /// This is contract H2 (TECHNICAL_ARCHITECTURE.md §10.4) and nothing more: a *self*-consistency claim about the
    /// C# simulation. No reference hash is reproduced and none is compared (U-M-13). General replay infrastructure —
    /// logged streams from the reference, <c>replayVerdict</c>, long soaks — stays deferred (TASKS.md DEF-01).
    /// </summary>
    public static class Replay
    {
        /// <summary>
        /// Replay <paramref name="log"/> into a fresh game on <paramref name="seed"/> and report the state it
        /// reached with its Unity-canonical hash.
        /// </summary>
        /// <param name="dropAim">
        /// Reference <c>opts.dropAim</c> (hour.ts:1483): drop every <see cref="AimCommand"/> from the stream, which
        /// is how the reference replays a run "without the rifle". The reference also drops <c>setSpeed</c>; there is
        /// no speed command in the port (U-D-04), so that filter has nothing to match and is not ported.
        /// </param>
        public static (SimState State, string Hash) Run(SimContext ctx, int seed, IReadOnlyList<LoggedCommand> log,
            int untilTick, bool dropAim = false)
        {
            var sim = Simulation.NewGame(ctx, seed);
            Run(sim, log, untilTick, dropAim);
            return (sim.State, sim.Hash());
        }

        /// <summary>The same replay against a simulation the caller already built (loading, or a scenario fixture).</summary>
        public static Simulation Run(Simulation sim, IReadOnlyList<LoggedCommand> log, int untilTick, bool dropAim = false)
        {
            var st = sim.State;
            var k = 0;
            var n = log?.Count ?? 0;
            while (st.Tick < untilTick)
            {
                k = ApplyDue(sim, log, k, n, st.Tick, dropAim);
                sim.Tick();
                // The reference empties the event queue every iteration (hour.ts:1487); events are never hashed and
                // never saved, so this only stops the list growing for the length of the replay.
                st.Events.Clear();
            }
            ApplyDue(sim, log, k, n, st.Tick, dropAim);
            st.Events.Clear();
            return sim;
        }

        /// <summary>Applies every entry whose tick has arrived and returns the new read cursor.</summary>
        private static int ApplyDue(Simulation sim, IReadOnlyList<LoggedCommand> log, int k, int n, int tick, bool dropAim)
        {
            while (k < n)
            {
                var entry = log[k];
                if (entry.Tick > tick) break;
                k++;
                var c = entry.Command;
                if (c == null) continue;
                if (dropAim && c is AimCommand) continue;
                sim.Apply(c);
            }
            return k;
        }
    }
}
