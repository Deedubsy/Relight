using System.Collections.Generic;
using Relight.Sim;

namespace Relight.Presentation
{
    /// <summary>One placed machine as the world view needs it: identity and footprint, no sim references (TA §4.5).</summary>
    public readonly struct MachinePlacement
    {
        public readonly int Id;
        public readonly string Kind;
        public readonly int X;
        public readonly int Y;
        public readonly Dir Dir;
        public readonly int W;
        public readonly int H;

        public MachinePlacement(int id, string kind, int x, int y, Dir dir, int w, int h)
        {
            Id = id; Kind = kind; X = x; Y = y; Dir = dir; W = w; H = h;
        }
    }

    /// <summary>
    /// INTERIM (B-13). <c>Relight.Sim/Queries</c> has no "every placed machine" selector yet — B-06 ported the
    /// inventory selectors, which address a machine by id, and nothing needs a list until something draws them.
    /// <see cref="MachinePresenter"/> does, so this is the one place in the presentation that reads
    /// <c>SimState.Machines</c>, kept out of any MonoBehaviour and out of every view, and kept to the read pattern
    /// TA §4.5 allows for exactly this case ("expose IReadOnlyList over the sim's own collections plus a change
    /// counter", <see cref="SimState.Rev"/>).
    ///
    /// The report proposes <c>WorldQueries.Machines(ctx, st, List&lt;MachinePlacement&gt;)</c> in
    /// <c>Relight.Sim/Queries</c>; when the coordinator adds it, this class becomes a one-line forward and then goes.
    /// </summary>
    public static class MachineSnapshot
    {
        /// <summary>The structural revision, bumped on every placement/removal (reference flow.ts <c>f.rev</c>).</summary>
        public static int Revision(SimState st) => st.Rev;

        /// <summary>How many machines are placed, for a status readout.</summary>
        public static int Count(SimState st) => st.Machines.Count;

        /// <summary>Fill <paramref name="into"/> with every placed machine, in placement order. Allocates nothing per call.</summary>
        public static void All(SimState st, List<MachinePlacement> into)
        {
            into.Clear();
            var list = st.Machines;
            for (var i = 0; i < list.Count; i++)
            {
                var m = list[i];
                var (w, h) = m.Dimensions;
                into.Add(new MachinePlacement(m.Id, m.Kind, m.X, m.Y, m.Dir, w, h));
            }
        }
    }
}
