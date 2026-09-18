using NUnit.Framework;
using Relight.UI;
using UnityEngine;

namespace Relight.Tests.Play.Ui
{
    /// <summary>
    /// GP-UX-3. "When dragging from inventory to inventory, have the icon drag with the mouse pointer."
    ///
    /// The ghost always followed the pointer in the middle of the screen; what it did not do was follow it at the
    /// EDGES. The first port placed the whole 300 px card at pointer+(16,16) and then clamped the card inside its
    /// parent, so within a card's width of the right-hand edge the clamp pinned the card and the icon drifted away
    /// from the cursor — and the storage column and the workshop tray are both on that side of the drawer, so the
    /// gesture that came apart was the ordinary one.
    ///
    /// <see cref="StackDragManipulator.Place"/> is the arithmetic on its own, which is why it can be asserted here
    /// without a pointer device, a panel or a scene. Every case below states the same property — the ICON's centre
    /// is on the pointer — and the interesting ones are where the old code could not hold it.
    ///
    /// Written WITHOUT Unity on this machine: never run here. It does compile: the whole UI assembly was built
    /// offline against the editor's managed DLLs.
    /// </summary>
    public sealed class DragGhostPlacementTests
    {
        // The shipped ghost: .drag-preview is 300 wide with --ui-space-2 side padding, .drag-item-icon is 42 square.
        private const float Bounds = 1920f;
        private const float Card = 300f;
        private const float Height = 56f;
        private const float Icon = 42f;
        private const float Pad = 8f;
        private const float Tol = 0.001f;

        private static StackDragManipulator.GhostPlace At(float x, float y) =>
            StackDragManipulator.Place(new Vector2(x, y), Bounds, Card, Height, Icon, Pad, Pad);

        private static void AssertOnPointer(StackDragManipulator.GhostPlace p, Vector2 pointer, string where)
        {
            Assert.That(p.IconCentreX(Card, Icon, Pad, Pad), Is.EqualTo(pointer.x).Within(Tol),
                "the icon is not under the pointer " + where + ".");
            Assert.That(p.IconCentreY(Height), Is.EqualTo(pointer.y).Within(Tol),
                "the icon is off the pointer vertically " + where + ".");
        }

        /// <summary>
        /// The whole contract, swept across the screen: wherever the pointer is, the icon is on it. The sweep runs
        /// past the flip point deliberately — a property that holds on both sides of a branch but not across it is
        /// exactly the defect being fixed, and only a sweep catches that.
        /// </summary>
        [Test]
        public void TheIconStaysUnderThePointerAllTheWayAcrossTheScreen()
        {
            for (var x = 0f; x <= Bounds; x += 37f)
            {
                var pointer = new Vector2(x, 400f);
                AssertOnPointer(At(x, 400f), pointer, "at x=" + x);
            }
            AssertOnPointer(At(Bounds, 400f), new Vector2(Bounds, 400f), "at the right-hand edge itself");
        }

        /// <summary>
        /// The specific regression: a pointer one card-width from the right edge. Under the old clamp the card
        /// stopped here and the icon trailed it; now the card turns around and the icon does not move.
        /// </summary>
        [Test]
        public void NearTheRightHandEdgeTheCardFlipsInsteadOfDraggingTheIconAwayFromTheCursor()
        {
            var middle = At(600f, 300f);
            Assert.That(middle.Flip, Is.False, "there is ample room at x=600; nothing should flip.");
            Assert.That(middle.X, Is.LessThan(600f), "unflipped, the card starts to the left of the icon's centre.");

            var edge = At(Bounds - 10f, 300f);
            Assert.That(edge.Flip, Is.True, "10 px from the edge a 300 px card cannot open to the right.");
            AssertOnPointer(edge, new Vector2(Bounds - 10f, 300f), "10 px from the right-hand edge");
            // Flipped, the only thing past the pointer is the icon's own half and the card's padding — 29 px,
            // against the 250 px of card the unflipped placement would have had to push off the screen.
            Assert.That(edge.X + Card - (Bounds - 10f), Is.EqualTo(Pad + Icon * 0.5f).Within(Tol),
                "a flipped card should reach barely past the pointer, not open to the right.");
        }

        /// <summary>
        /// The flip happens once, at the point where the card stops fitting, and not a pixel before: an early flip
        /// would have the card jumping sides in the middle of the screen for no reason the player can see.
        /// </summary>
        [Test]
        public void TheFlipHappensExactlyWhereTheCardStopsFitting()
        {
            // Unflipped, the card's right edge is at pointer.x + (Card - Pad - Icon/2).
            var overhang = Card - Pad - Icon * 0.5f;
            var last = Bounds - overhang;
            Assert.That(At(last, 300f).Flip, Is.False, "the card still fits exactly at this pointer position.");
            Assert.That(At(last + 1f, 300f).Flip, Is.True, "one pixel further and it does not fit.");
        }

        /// <summary>
        /// Vertically there is no flip and no clamp, by choice: the icon is centred on the pointer everywhere, and
        /// near the top or bottom edge the card's far end is allowed to overhang instead. Clamping Y would be the
        /// same defect turned ninety degrees.
        /// </summary>
        [Test]
        public void TheIconIsCentredOnThePointerVerticallyEvenAtTheTopOfTheScreen()
        {
            Assert.That(At(500f, 0f).Y, Is.EqualTo(-Height * 0.5f).Within(Tol));
            AssertOnPointer(At(500f, 0f), new Vector2(500f, 0f), "at the very top of the screen");
            AssertOnPointer(At(500f, 1080f), new Vector2(500f, 1080f), "at the very bottom of the screen");
        }

        /// <summary>
        /// A ghost whose size has not resolved yet (the first frame of a drag) must still be placed, not thrown
        /// away: bounds of zero means "no parent to fit inside", and the answer is the unflipped placement.
        /// </summary>
        [Test]
        public void AnUnresolvedParentPlacesTheCardWithoutFlipping()
        {
            var p = StackDragManipulator.Place(new Vector2(900f, 200f), 0f, Card, Height, Icon, Pad, Pad);
            Assert.That(p.Flip, Is.False);
            AssertOnPointer(p, new Vector2(900f, 200f), "with no resolved bounds");
        }
    }
}
