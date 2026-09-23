using System;

namespace Relight.Sim
{
    /// <summary>Lift the nearest Power core in reach off the ground (§5.6 <c>PickUpCoreCommand</c>).</summary>
    public sealed record PickUpCoreCommand : Command;

    /// <summary>Put the carried Power core down where the engineer stands (§5.6 <c>DropCoreCommand</c>).</summary>
    public sealed record DropCoreCommand : Command;

    /// <summary>The engineer lifted a stronghold's Power core at (X, Y).</summary>
    public sealed record CoreLiftedEvent(double T, string Stronghold, double X, double Y) : SimEvent(T);

    /// <summary>The Power core is on the ground at (X, Y): put down, or dropped when the engineer went down.</summary>
    public sealed record CoreSetDownEvent(double T, string Stronghold, double X, double Y, bool Fell) : SimEvent(T);

    /// <summary>
    /// Batch 4, FRT-07 (REL-142): the two-handed carry (FREIGHT_STRONGHOLD_DESIGN §5.6).
    ///
    /// <para>A Power core never enters the inventory. Lifting one moves it from
    /// <see cref="EncounterState.Cores"/> into <see cref="Engineer.Carrying"/>; putting it down moves it back, at
    /// the engineer's feet. Either way there is exactly one of it, so it cannot be doubled or lost.</para>
    ///
    /// <para>While it is carried the engineer can only walk. Sprint, the dodge, firing and building are each refused
    /// with a reason, which the HUD shows as it shows every refused command. Going down drops the core where the body
    /// fell, before the respawn, so the walk back to it is the cost of dying, as it is for cargo.</para>
    /// </summary>
    public static class CoreCarry
    {
        public const string SprintText = "You can't sprint while carrying the Power core.";
        public const string DodgeText = "You can't dodge while carrying the Power core.";
        public const string FireText = "Put the Power core down to use your weapon.";
        public const string BuildText = "Put the Power core down to build.";
        public const string NoCoreText = "No Power core in reach.";
        public const string FullHandsText = "You are already carrying a Power core.";
        public const string EmptyHandsText = "You are not carrying anything.";
        public const string DownText = "The engineer is down.";

        public static bool Holding(Engineer e) => e != null && !string.IsNullOrEmpty(e.Carrying);

        /// <summary>Why building is refused right now, or null when it is not. The build drawer asks this too.</summary>
        public static string BuildRefusal(SimState st) => Holding(st.Engineer) ? BuildText : null;

        /// <summary>Reach of a core is reach of its tile, the same rule as a dropped cargo pile.</summary>
        public static bool InReach(SimContext ctx, SimState st, LooseCore c) =>
            c != null && Interaction.InReach(ctx, st, (int)Math.Floor(c.Pos.X), (int)Math.Floor(c.Pos.Y), 1, 1);

        /// <summary>The nearest core on the ground in reach, or null.</summary>
        public static LooseCore NearestInReach(SimContext ctx, SimState st)
        {
            var p = st.Engineer.Pos;
            LooseCore best = null;
            var bestD = double.MaxValue;
            var cores = st.Encounters.Cores;
            for (var i = 0; i < cores.Count; i++)
            {
                var c = cores[i];
                if (!InReach(ctx, st, c)) continue;
                var d = DirectorRules.Distance(p.X, p.Y, c.Pos.X, c.Pos.Y);
                if (d < bestD) { bestD = d; best = c; }
            }
            return best;
        }

        public static (bool ok, string reason) PickUp(SimContext ctx, SimState st)
        {
            var e = st.Engineer;
            if (e.IsDown) return (false, DownText);
            if (HandCraft.HandLocked(st)) return (false, HandCraft.LockTextFor(st));
            if (Holding(e)) return (false, FullHandsText);
            var c = NearestInReach(ctx, st);
            if (c == null) return (false, NoCoreText);
            st.Encounters.Cores.Remove(c);
            e.Carrying = c.Stronghold;
            // Both hands are on the core from this tick: the rifle stops and Shift stops counting.
            e.Sprint = false;
            st.Weapons.Firing = false;
            st.Events.Add(new CoreLiftedEvent(st.T, c.Stronghold, c.Pos.X, c.Pos.Y));
            return (true, "");
        }

        public static (bool ok, string reason) Drop(SimState st)
        {
            var e = st.Engineer;
            if (e.IsDown) return (false, DownText);
            if (!Holding(e)) return (false, EmptyHandsText);
            SetDown(st, e, false);
            return (true, "");
        }

        /// <summary>
        /// <see cref="Engineer.TakeDamage"/> calls this as the engineer goes down, before the cargo spills: the core
        /// lands where the body fell.
        /// </summary>
        public static void Fell(SimState st, Engineer e)
        {
            if (Holding(e)) SetDown(st, e, true);
        }

        static void SetDown(SimState st, Engineer e, bool fell)
        {
            var id = e.Carrying;
            e.Carrying = "";
            st.Encounters.Cores.Add(new LooseCore { Stronghold = id, Pos = e.Pos });
            st.Events.Add(new CoreSetDownEvent(st.T, id, e.Pos.X, e.Pos.Y, fell));
        }
    }

    public sealed class CoreCarryHandler : ICommandHandler
    {
        public bool TryApply(SimContext ctx, SimState st, Command c, out CommandResult result)
        {
            switch (c)
            {
                case PickUpCoreCommand _:
                {
                    var (ok, reason) = CoreCarry.PickUp(ctx, st);
                    result = ok ? CommandResult.Ok() : CommandResult.Refuse(reason);
                    return true;
                }
                case DropCoreCommand _:
                {
                    var (ok, reason) = CoreCarry.Drop(st);
                    result = ok ? CommandResult.Ok() : CommandResult.Refuse(reason);
                    return true;
                }
                default:
                    result = default;
                    return false;
            }
        }
    }
}
