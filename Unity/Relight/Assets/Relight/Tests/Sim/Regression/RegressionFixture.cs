using System.Collections.Generic;
using NUnit.Framework;

namespace Relight.Sim.Tests.Regression
{
    /// <summary>
    /// C-12's own fixture. It deliberately drives the WHOLE composition
    /// (<see cref="Simulation.NewGame"/> → <see cref="SimComposition.Phases"/> / <see cref="SimComposition.Handlers"/>)
    /// rather than a subsystem's phase list: every row in UI_AND_ONBOARDING.md §9 is a defect a PLAYER saw, so the
    /// regression check has to be a statement about the assembled game, not about one slot of it. A subsystem test
    /// that passes while the assembled game still shows the defect is exactly the failure mode these rows record.
    ///
    /// Nothing here mutates gameplay code and nothing here is used by another worker's tests.
    /// </summary>
    public static class RegressionFixture
    {
        public const double Dt = Simulation.TickSeconds;

        /// <summary>The ordinary campaign context: the synthetic map, the reference catalogue, no authored sites.</summary>
        public static SimContext Context() => new SimContext(ReferenceData.Create(), SyntheticMap.Create());

        /// <summary>A fresh campaign through every registered initialiser, exactly as the host starts one.</summary>
        public static Simulation NewGame(SimContext ctx = null, int seed = 7) =>
            Simulation.NewGame(ctx ?? Context(), seed);

        /// <summary>
        /// Steps <paramref name="ticks"/> ticks, returning EVERY event raised on the way. The host clears
        /// <c>st.Events</c> each frame, so a check that only inspects the list at the end would miss the very
        /// thing defect U-3 is about — a toast that appears once, early, and is gone by the time anyone looks.
        /// </summary>
        public static List<SimEvent> StepCollecting(Simulation sim, int ticks)
        {
            var all = new List<SimEvent>();
            for (var i = 0; i < ticks; i++)
            {
                sim.Tick();
                all.AddRange(sim.State.Events);
                sim.State.Events.Clear();
            }
            return all;
        }

        /// <summary>Steps without keeping the events (the host's own behaviour).</summary>
        public static void Step(Simulation sim, int ticks)
        {
            for (var i = 0; i < ticks; i++) { sim.Tick(); sim.State.Events.Clear(); }
        }

        /// <summary>
        /// Puts <paramref name="n"/> of an item in the pockets and books it as made, so the conservation ledger
        /// still balances. A test that stocked the world without this would report "unexplained" on every
        /// subsequent check and hide a real leak (Wave 2 integration note: <c>Ledger.Open</c> runs in the
        /// inventory initialiser, before anything a test adds).
        /// </summary>
        public static void Give(SimState st, ItemId item, double n)
        {
            st.Engineer.Inv[item] = st.Engineer.Inv[item] + n;
            st.Stats.Made.Add(item, n);
        }

        /// <summary>Every item out of ledger tolerance as one line each; "" when conservation holds.</summary>
        public static string Off(SimContext ctx, SimState st) =>
            string.Join(" | ", Ledger.Conservation(st, ctx.Data).Problems);

        /// <summary>Places a machine without charging for it, the way <see cref="Tests.Combat.RaidFixture"/> does.</summary>
        public static Machine Add(SimContext ctx, SimState st, string kind, int x, int y, Dir dir = Dir.N)
        {
            Assert.That(ctx.Data.TryMachine(kind, out var spec), Is.True, kind + " is not in the catalogue");
            var m = new Machine { Id = st.NextId++, Kind = kind, X = x, Y = y, Dir = dir, Size = spec.Size };
            st.Machines.Add(m);
            st.Rev++;
            return m;
        }

        /// <summary>Hands the engineer a weapon of <paramref name="kind"/> and returns its instance id.</summary>
        public static string GiveWeapon(SimContext ctx, SimState st, string kind = "rifle")
        {
            var id = WeaponRules.Create(st, kind);
            Pockets.Take(ctx.Data, st.Engineer, new ItemKey(id), 1);
            return id;
        }

        /// <summary>
        /// Asserts the command was accepted, quoting the refusal if it was not.
        /// <see cref="CommandResult.Problem"/> is NOT empty on success — it doubles as the confirmation note
        /// ("Stack moved.", "Reloading · 2 seconds…") — so acceptance is read from <c>Accepted</c> alone.
        /// </summary>
        public static CommandResult Accept(CommandResult r, string because = null)
        {
            Assert.That(r.Accepted, Is.True,
                (because == null ? "" : because + " — ") + "refused with \"" + r.Problem + "\"");
            return r;
        }

        /// <summary>Asserts the command was refused, with exactly <paramref name="text"/>.</summary>
        public static void Refused(CommandResult r, string text, string because = null)
        {
            Assert.That(r.Accepted, Is.False, because);
            Assert.That(r.Problem, Is.EqualTo(text), because);
        }

        /// <summary>The first event of <typeparamref name="T"/> in a collected list, or null.</summary>
        public static T First<T>(List<SimEvent> events) where T : class
        {
            for (var i = 0; i < events.Count; i++) if (events[i] is T t) return t;
            return null;
        }

        /// <summary>How many events of <typeparamref name="T"/> a collected list holds.</summary>
        public static int Count<T>(List<SimEvent> events) where T : class
        {
            var n = 0;
            for (var i = 0; i < events.Count; i++) if (events[i] is T) n++;
            return n;
        }
    }
}
