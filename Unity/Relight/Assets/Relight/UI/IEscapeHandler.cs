namespace Relight.UI
{
    /// <summary>
    /// One link in the Escape chain (TECHNICAL_ARCHITECTURE.md §8.3). Registered with an explicit
    /// <see cref="Order"/> so the chain is data, not a hard-coded <c>if/else</c> ladder as in the reference
    /// (uiShell.ts:114-120) — the order there is the contract and is reproduced exactly by
    /// <see cref="EscapeOrder"/>.
    /// </summary>
    public interface IEscapeHandler
    {
        /// <summary>Lower runs first. Use the <see cref="EscapeOrder"/> constants.</summary>
        int Order { get; }

        /// <summary>Do the one thing this link does and return true, or return false to pass Escape on.</summary>
        bool OnEscape();
    }

    /// <summary>
    /// The literal Escape order of the reference (uiShell.ts:114-120): cancel drag, close slot menu, close nav menu,
    /// close modal child, unpause, close drawer, cancel world selection, pause. Values are spaced so a Phase C
    /// handler can slot between two of them without renumbering.
    /// </summary>
    public static class EscapeOrder
    {
        public const int CancelDrag = 100;            // uiShell.ts:116 cancelUiDrag()
        public const int CloseSlotMenu = 200;         // uiShell.ts:117 .slot-menu:not([hidden])
        public const int CloseNavMenu = 300;          // uiShell.ts:118 details[open]
        public const int CloseModalChild = 400;       // uiShell.ts:119 if (childId) closeChild()
        public const int Unpause = 500;               // uiShell.ts:119 else if (!modal.hidden) unpause()
        public const int CloseDrawer = 600;           // uiShell.ts:119 else if (active) closeDrawer()
        public const int CancelWorldSelection = 700;  // uiShell.ts:119 else if (!hooks.cancelSelection())
        public const int Pause = 800;                 // uiShell.ts:119 else pause()
    }
}
