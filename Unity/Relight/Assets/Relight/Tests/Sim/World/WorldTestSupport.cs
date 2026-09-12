using System.Collections.Generic;

namespace Relight.Sim.Tests
{
    /// <summary>
    /// A tiny tick driver for the world tests. It runs the real <see cref="SimComposition"/> (so registration is
    /// covered too) but does not depend on B-03's <c>Simulation</c> class: the loop here advances the clock the same
    /// way the driver does — phases first, then <c>Tick++</c> and <c>T += 1/20</c>.
    /// </summary>
    public static class WorldTestSupport
    {
        public const double Dt = 1.0 / 20.0;

        public static SimContext Context() => new SimContext(ReferenceData.Create(), SyntheticMap.Create());

        public static SimState NewState(SimContext ctx)
        {
            var st = new SimState();
            var inits = SimComposition.Initializers;
            for (var i = 0; i < inits.Count; i++) inits[i].Init(ctx, st);
            return st;
        }

        public static (SimContext ctx, SimState st) New()
        {
            var ctx = Context();
            return (ctx, NewState(ctx));
        }

        /// <summary>One 1/20 s tick of the whole composition.</summary>
        public static void Step(SimContext ctx, SimState st)
        {
            var phases = SimComposition.Phases;
            for (var i = 0; i < phases.Count; i++) phases[i].Tick(ctx, st, Dt);
            st.Tick++;
            st.T += Dt;
        }

        public static void Step(SimContext ctx, SimState st, int ticks)
        {
            for (var i = 0; i < ticks; i++) Step(ctx, st);
        }

        /// <summary>Sends a command through the registered handlers (no dependency on B-03's dispatcher).</summary>
        public static CommandResult Send(SimContext ctx, SimState st, Command c)
        {
            var handlers = SimComposition.Handlers;
            for (var i = 0; i < handlers.Count; i++)
                if (handlers[i].TryApply(ctx, st, c, out var r)) return r;
            return CommandResult.Refuse("no handler");
        }

        /// <summary>Puts the engineer somewhere specific without going through a command.</summary>
        public static void Place(SimState st, double x, double y)
        {
            st.Engineer.Pos = new Vec2(x, y);
            st.Engineer.Vel = Vec2.Zero;
            st.Engineer.HasTarget = false;
            st.Engineer.Plan = null;
        }

        /// <summary>Adds a machine and bumps the structural revision, as a placement command would.</summary>
        public static Machine AddMachine(SimState st, string kind, int x, int y, int size = 1)
        {
            var m = new Machine { Id = st.NextId++, Kind = kind, X = x, Y = y, Size = size };
            st.Machines.Add(m);
            st.Rev++;
            return m;
        }

        public static List<T> EventsOf<T>(SimState st) where T : SimEvent
        {
            var list = new List<T>();
            for (var i = 0; i < st.Events.Count; i++) if (st.Events[i] is T e) list.Add(e);
            return list;
        }
    }
}
