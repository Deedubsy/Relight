using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using Relight.Prototypes;
using Relight.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Relight.Tests.Play.Prototypes
{
    /// <summary>
    /// B-14 / R6 — "can runtime UI Toolkit carry the inventory drag at all", measured rather than assumed.
    /// Risk R6 in TECHNICAL_ARCHITECTURE.md: UI Toolkit runtime drag-and-drop is not a built-in, so the reference's
    /// uiDrag.ts had to be ported by hand; these tests are the proof that the port behaves the same.
    /// </summary>
    public sealed class R6SlotGridTests
    {
        private SlotGridPrototype _panel;
        private SlotDragController _drag;

        private IEnumerator LoadPanel()
        {
            yield return PrototypeFixture.Load(PrototypeFixture.R6);
            _panel = Object.FindAnyObjectByType<SlotGridPrototype>();
            Assert.NotNull(_panel, "R6-SlotGrid.unity has no SlotGridPrototype; run Relight/Prototypes/Build R6.");
            _drag = _panel.Drag;
            Assert.NotNull(_drag, "the prototype built no drag controller (missing #pack-grid / #store-grid?).");
        }

        // ---------------------------------------------------------------- model rules (no panel needed)

        [Test]
        public void ModelMovesIntoEmptyAndSwapsWithOccupied()
        {
            var model = new SlotGridModel(40, 20);
            model.Set(new SlotAddress(SlotSide.Pack, 0), "steel", 12);
            model.Set(new SlotAddress(SlotSide.Store, 3), "coal", 5);

            Assert.IsTrue(model.MoveOrSwap(new SlotAddress(SlotSide.Pack, 0), new SlotAddress(SlotSide.Pack, 7)));
            Assert.AreEqual("steel", model.Get(new SlotAddress(SlotSide.Pack, 7)).Item);
            Assert.IsTrue(model.Get(new SlotAddress(SlotSide.Pack, 0)).Empty);

            Assert.IsTrue(model.MoveOrSwap(new SlotAddress(SlotSide.Pack, 7), new SlotAddress(SlotSide.Store, 3)));
            Assert.AreEqual("steel", model.Get(new SlotAddress(SlotSide.Store, 3)).Item);
            Assert.AreEqual("coal", model.Get(new SlotAddress(SlotSide.Pack, 7)).Item, "an occupied target swaps");

            Assert.IsFalse(model.MoveOrSwap(new SlotAddress(SlotSide.Pack, 1), new SlotAddress(SlotSide.Pack, 2)), "empty source");
            Assert.IsFalse(model.MoveOrSwap(new SlotAddress(SlotSide.Pack, 7), new SlotAddress(SlotSide.Pack, 7)), "self");
            Assert.IsFalse(model.MoveOrSwap(new SlotAddress(SlotSide.Pack, 7), new SlotAddress(SlotSide.Store, 99)), "out of range");
        }

        [Test]
        public void ThresholdAndSuppressionMatchTheReference()
        {
            // uiDrag.ts:12 Math.hypot(dx, dy) < 7 and uiDrag.ts:11 setTimeout(..., 250).
            Assert.AreEqual(7f, SlotDragController.ThresholdPixels);
            Assert.AreEqual(0.25, SlotDragController.ClickSuppressSeconds);
            Assert.AreEqual("drag-preview", SlotDragController.GhostClass);
            Assert.AreEqual("drop-target", SlotDragController.DropTargetClass);
            Assert.AreEqual(100, EscapeOrder.CancelDrag, "Escape cancels a drag before anything else (uiShell.ts:116).");
        }

        // ---------------------------------------------------------------- the document

        [UnityTest, Timeout(120000)]
        public IEnumerator OneDocumentCarriesFortyPlusTwentySlots()
        {
            yield return LoadPanel();
            Assert.AreEqual(40, _panel.PackElements.Count, "backpack slots");
            Assert.AreEqual(20, _panel.StoreElements.Count, "storage slots");
            Assert.AreEqual(40, _panel.Model.PackSlots);
            Assert.AreEqual(20, _panel.Model.StoreSlots);
            Assert.AreEqual(60, _panel.Root.Query<VisualElement>(className: "inventory-slot").ToList().Count,
                "both grids live in ONE UIDocument, which is the thing R6 had to prove.");
            Assert.AreEqual(60, _panel.Model.OccupiedCount(), "every slot is filled so any drop is a swap or a move");
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator LayoutResolvesAndSlotsArePickable()
        {
            yield return LoadPanel();
            PrototypeFixture.RequireLayout(_panel.Root);
            var first = _panel.PackElements[0];
            Assert.Greater(first.worldBound.width, 1f, "a slot has no width; the USS did not load.");
            var picked = _drag.SlotElementUnder(UiPointer.Centre(first));
            Assert.AreEqual(first, picked, "IPanel.Pick is the port of document.elementFromPoint (uiDrag.ts:12).");
        }

        // ---------------------------------------------------------------- the state machine

        [UnityTest, Timeout(120000)]
        public IEnumerator PressBelowSevenPixelsIsStillAClick()
        {
            yield return LoadPanel();
            PrototypeFixture.RequireLayout(_panel.Root);
            var source = _panel.PackElements[0];
            var p = UiPointer.Centre(source);

            UiPointer.Send(source, UiPointer.Down(p));
            Assert.IsTrue(_drag.Armed, "pointerdown captures the pointer");
            Assert.IsFalse(_drag.Dragging);

            UiPointer.Send(source, UiPointer.Move(p + new Vector2(6f, 0f)));
            Assert.IsFalse(_drag.Dragging, "6 px is below the 7 px threshold (uiDrag.ts:12)");
            Assert.AreEqual(0, _drag.Activations);

            UiPointer.Send(source, UiPointer.Move(p + new Vector2(9f, 0f)));
            Assert.IsTrue(_drag.Dragging, "9 px crosses the threshold");
            Assert.AreEqual(1, _drag.Activations);
            Assert.NotNull(_drag.Ghost, "the .drag-preview ghost exists while dragging");
            Assert.IsTrue(_panel.Root.ClassListContains("ui-dragging"));

            UiPointer.Send(source, UiPointer.Up(p + new Vector2(9f, 0f)));
            Assert.IsFalse(_drag.Dragging);
            Assert.IsNull(_drag.Ghost, "the ghost is removed on finish");
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator DragAcrossGridsMovesTheStack()
        {
            yield return LoadPanel();
            PrototypeFixture.RequireLayout(_panel.Root);
            var from = new SlotAddress(SlotSide.Pack, 0);
            var to = new SlotAddress(SlotSide.Store, 0);
            var before = _panel.Model.Get(from);
            var target = _panel.Model.Get(to);

            yield return Drag(_panel.PackElements[0], _panel.StoreElements[0]);

            Assert.AreEqual(1, _drag.Drops);
            Assert.AreEqual(1, _drag.Moves);
            Assert.AreEqual(before.Item, _panel.Model.Get(to).Item, "the dragged stack landed in the other grid");
            Assert.AreEqual(target.Item, _panel.Model.Get(from).Item, "an occupied target swaps (inventoryPanel.ts relocate)");
            Assert.IsTrue(_drag.LastPreviewLabel.Length > 0, "the ghost showed a policy label");
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator DropOnNothingChangesNothing()
        {
            yield return LoadPanel();
            PrototypeFixture.RequireLayout(_panel.Root);
            var from = new SlotAddress(SlotSide.Pack, 0);
            var before = _panel.Model.Get(from);
            var source = _panel.PackElements[0];
            var p = UiPointer.Centre(source);
            // The status label at the bottom of the panel is not a slot — the reference's "Release to cancel".
            var empty = new Vector2(_panel.Root.worldBound.xMax - 4f, _panel.Root.worldBound.yMax - 4f);

            UiPointer.Send(source, UiPointer.Down(p));
            UiPointer.Send(source, UiPointer.Move(p + new Vector2(20f, 0f)));
            UiPointer.Send(source, UiPointer.Move(empty));
            Assert.AreEqual("Release to cancel", _drag.LastPreviewLabel);
            UiPointer.Send(source, UiPointer.Up(empty));

            Assert.AreEqual(0, _drag.Moves, "nothing moved");
            Assert.AreEqual(before.Item, _panel.Model.Get(from).Item);
            yield return null;
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator EscapeCancelsTheDragBeforeAnythingElse()
        {
            yield return LoadPanel();
            PrototypeFixture.RequireLayout(_panel.Root);
            var escape = Object.FindAnyObjectByType<EscapeChain>();
            Assert.NotNull(escape, "the prototype registers its cancel-drag link on the B-13 EscapeChain.");
            var from = new SlotAddress(SlotSide.Pack, 0);
            var before = _panel.Model.Get(from);
            var source = _panel.PackElements[0];
            var p = UiPointer.Centre(source);

            UiPointer.Send(source, UiPointer.Down(p));
            UiPointer.Send(source, UiPointer.Move(p + new Vector2(30f, 0f)));
            Assert.IsTrue(_drag.Dragging);

            var handled = escape.Escape();
            Assert.NotNull(handled, "Escape found a handler");
            Assert.AreEqual(EscapeOrder.CancelDrag, handled.Order, "the FIRST link is cancel drag (uiShell.ts:116)");
            Assert.IsFalse(_drag.Dragging);
            Assert.AreEqual(1, _drag.Cancels);
            Assert.IsNull(_drag.Ghost);
            Assert.AreEqual(before.Item, _panel.Model.Get(from).Item, "a cancelled drag moves nothing");
            yield return null;
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator TheClickThatEndsADragIsSwallowed()
        {
            yield return LoadPanel();
            PrototypeFixture.RequireLayout(_panel.Root);
            yield return Drag(_panel.PackElements[0], _panel.PackElements[9]);

            using (var click = ClickEvent.GetPooled())
            {
                _panel.PackElements[9].SendEvent(click);
            }
            Assert.AreEqual(1, _drag.SuppressedClicks, "uiDrag.ts:11 swallows the click for 250 ms after a drag.");
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator FocusLossCancelsALiveDrag()
        {
            yield return LoadPanel();
            PrototypeFixture.RequireLayout(_panel.Root);
            var source = _panel.PackElements[1];
            var p = UiPointer.Centre(source);
            UiPointer.Send(source, UiPointer.Down(p));
            UiPointer.Send(source, UiPointer.Move(p + new Vector2(25f, 0f)));
            Assert.IsTrue(_drag.Dragging);

            _drag.OnApplicationFocusLost();             // uiDrag.ts:11 blur / visibilitychange
            Assert.IsFalse(_drag.Dragging);
            Assert.AreEqual(1, _drag.Cancels);
            yield return null;
        }

        // ---------------------------------------------------------------- the measurement

        [UnityTest, Timeout(300000)]
        public IEnumerator DragCostIsMeasuredAndRecorded()
        {
            yield return LoadPanel();
            PrototypeFixture.RequireLayout(_panel.Root);

            // 1. How long a full 60-slot rebuild takes — C-06's worst case is a whole-panel redraw per sim change.
            var rebuild = Stopwatch.StartNew();
            for (var i = 0; i < 20; i++) _panel.Refresh();
            rebuild.Stop();
            var rebuildMs = rebuild.Elapsed.TotalMilliseconds / 20.0;

            // 2. How long one pointer move costs — Pick + highlight + ghost placement, the per-frame drag work.
            var source = _panel.PackElements[0];
            var start = UiPointer.Centre(source);
            var end = UiPointer.Centre(_panel.StoreElements[19]);
            UiPointer.Send(source, UiPointer.Down(start));
            UiPointer.Send(source, UiPointer.Move(start + new Vector2(20f, 0f)));

            const int moves = 240;
            var move = Stopwatch.StartNew();
            for (var i = 0; i < moves; i++)
                UiPointer.Send(source, UiPointer.Move(Vector2.Lerp(start, end, (i + 1) / (float)moves)));
            move.Stop();
            var moveMs = move.Elapsed.TotalMilliseconds / moves;

            // 3. Frame cost with a drag live, so the figure includes UI Toolkit's own repaint.
            var sampler = new FrameSampler("r6-drag-frames");
            for (var i = 0; i < 200; i++)          // the brief's 200 drag frames
            {
                UiPointer.Send(source, UiPointer.Move(Vector2.Lerp(start, end, Mathf.PingPong(i * 0.05f, 1f))));
                yield return null;
                sampler.Add(Time.unscaledDeltaTime * 1000.0);
            }
            UiPointer.Send(source, UiPointer.Up(end));

            var values = new List<KeyValuePair<string, object>>
            {
                new KeyValuePair<string, object>("prototype", "R6 slot grid"),
                new KeyValuePair<string, object>("pack_slots", _panel.Model.PackSlots),
                new KeyValuePair<string, object>("store_slots", _panel.Model.StoreSlots),
                new KeyValuePair<string, object>("documents", 1),
                new KeyValuePair<string, object>("panel_rebuild_ms", EvidenceWriter.Round(rebuildMs)),
                new KeyValuePair<string, object>("pointer_move_ms", EvidenceWriter.Round(moveMs)),
                new KeyValuePair<string, object>("pointer_moves_sampled", moves),
                new KeyValuePair<string, object>("graphics_device", SystemInfo.graphicsDeviceType.ToString()),
                new KeyValuePair<string, object>("panel_width_px", EvidenceWriter.Round(_panel.Root.worldBound.width)),
                new KeyValuePair<string, object>("panel_height_px", EvidenceWriter.Round(_panel.Root.worldBound.height)),
            };
            EvidenceWriter.AddFrames(values, "drag", sampler);
            EvidenceWriter.Write("b14-r6-drag.json", values);
            UnityEngine.Debug.Log($"B14 R6: rebuild {rebuildMs:F3} ms/60 slots, pointer move {moveMs:F3} ms, {sampler.Summary()}");
            Assert.Greater(sampler.Count, 0);
        }

        // ---------------------------------------------------------------- helper

        private IEnumerator Drag(VisualElement from, VisualElement to)
        {
            var a = UiPointer.Centre(from);
            var b = UiPointer.Centre(to);
            UiPointer.Send(from, UiPointer.Down(a));
            UiPointer.Send(from, UiPointer.Move(a + new Vector2(12f, 0f)));
            for (var i = 1; i <= 8; i++) UiPointer.Send(from, UiPointer.Move(Vector2.Lerp(a, b, i / 8f)));
            UiPointer.Send(from, UiPointer.Up(b));
            yield return null;
        }
    }
}
