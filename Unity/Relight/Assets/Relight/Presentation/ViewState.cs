namespace Relight.Presentation
{
    /// <summary>Renderer-only view state (never in SimState): which view is up and the block both views agree on.</summary>
    public enum ViewMode { Map, World }

    /// <summary>
    /// A literal port of <c>packages/game/src/view.ts</c> (29 lines), comments included. Every field here is
    /// presentation state: it is never read by a tick phase or a command handler, never visited by an
    /// <c>IStateVisitor</c>, and therefore never saved (TECHNICAL_ARCHITECTURE.md §4.5, §9.1).
    ///
    /// Two deliberate differences from the reference, both forced by the host language rather than by design:
    /// <list type="bullet">
    /// <item>the reference's module-level singletons (<c>debugView</c>, <c>hudInset</c>, …) are fields of one
    ///       <see cref="ViewState"/> instance owned by the scene, with a static <see cref="Current"/> for the
    ///       handful of call sites that cannot be handed it. A C# static that survives a domain reload would carry a
    ///       previous play session's view state into the next one, so <see cref="Reset"/> is called when a session
    ///       starts;</item>
    /// <item>the reference's <c>objectiveView.targetId: string|null|undefined</c> three-state is spelled as
    ///       <see cref="ObjectiveView.Automatic"/> plus <see cref="ObjectiveView.TargetId"/>, because C# has no
    ///       null/undefined distinction. The three states are the same three.</item>
    /// </list>
    /// Phase B uses only <see cref="Mode"/>, <see cref="Debug"/> and <see cref="HudInset"/>; the rest are ported now
    /// so the Phase C panels that own them (C-02 inspection, C-07 HUD, C-10 navigation) find them already correct.
    /// </summary>
    public sealed class ViewState
    {
        /// <summary>The view state of the running session. Assigned by the scene; never serialised.</summary>
        public static ViewState Current { get; private set; } = new ViewState();

        /// <summary>Install a fresh view state for a new session (see the class comment on domain reload).</summary>
        public static ViewState Reset() => Current = new ViewState();

        public ViewMode Mode = ViewMode.World;

        /// <summary>The block the views hand each other on E: the block under the map cursor, or under the world camera's centre.</summary>
        public int FocusX;
        public int FocusY;

        /// <summary>Sim tick of the last switch, for the map view's returning marker.</summary>
        public int SwitchedAt;

        /// <summary>
        /// RI-02 (§11.2 "debug coordinates behind a toggle"): the HUD, tooltips and toasts name blocks and streets
        /// (names.ts); block and tile coordinates appear only while this is on — the ` key toggles it with the debug
        /// panel. Renderer-only state, shared by the panel and both scenes.
        /// </summary>
        public readonly DebugView Debug = new DebugView();

        /// <summary>
        /// RI-02: the height the goal overlay (#goal) takes at the top of the canvas, so the world view's top HUD
        /// corners sit under it instead of behind it. main.ts measures it once a panel update; 0 while the overlay is
        /// hidden. UI layout only — the camera never reads these (TECHNICAL_ARCHITECTURE.md §8.3).
        /// </summary>
        public readonly HudInset HudInset = new HudInset();

        /// <summary>Selected stop is presentation only: selecting a route never dispatches a movement command.</summary>
        public readonly TransportView Transport = new TransportView();

        /// <summary>Inspected machine identity is presentation only.</summary>
        public readonly InspectionView Inspection = new InspectionView();

        /// <summary>P9-03 selected navigation identity is presentation-only.</summary>
        public readonly NavigationView Navigation = new NavigationView();

        /// <summary>One known objective, presentation only; Automatic follows automatic guidance, a null id untracks.</summary>
        public readonly ObjectiveView Objective = new ObjectiveView();
    }

    public sealed class DebugView
    {
        public bool Coords;
    }

    public sealed class HudInset
    {
        public float Top;
        public float Right;
        public float Bottom;
    }

    public sealed class TransportView
    {
        /// <summary>Stop id, or -1 for none (the reference's null).</summary>
        public int StopId = -1;
    }

    public sealed class InspectionView
    {
        /// <summary>Machine id, or -1 for none (the reference's null). An id, never a machine (TA §4.5).</summary>
        public int MachineId = -1;
        public bool Pinned;
    }

    public sealed class NavigationView
    {
        public string TargetId;
    }

    public sealed class ObjectiveView
    {
        /// <summary>The reference's <c>undefined</c>: follow automatic guidance.</summary>
        public bool Automatic = true;

        /// <summary>The tracked objective; null with <see cref="Automatic"/> false is the reference's explicit untrack.</summary>
        public string TargetId;
    }
}
