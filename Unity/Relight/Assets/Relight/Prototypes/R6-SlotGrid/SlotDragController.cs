using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Relight.Prototypes
{
    /// <summary>
    /// B-14 / R6. The reference's slot drag state machine (packages/game/src/uiDrag.ts, whole file) ported to UI
    /// Toolkit runtime pointer events. The RULES are ported; none of the DOM code is:
    ///
    ///   uiDrag.ts:11  pointerdown, left button only                 -> PointerDownEvent, evt.button != 0 returns
    ///   uiDrag.ts:11  start() returns null                          -> Begin() callback returns an empty stack
    ///   uiDrag.ts:11  node.setPointerCapture(e.pointerId)           -> VisualElement.CapturePointer(evt.pointerId)
    ///   uiDrag.ts:12  Math.hypot(dx,dy) &lt; 7 -> not a drag yet    -> ThresholdPixels, identical value and test
    ///   uiDrag.ts:12  ghost = div.drag-preview following the pointer-> a VisualElement with the same class names
    ///   uiDrag.ts:12  document.elementFromPoint(x, y)               -> IPanel.Pick(position)
    ///   uiDrag.ts:12  .drop-target added to the hovered element     -> the same USS class, added and cleared the same way
    ///   uiDrag.ts:13  drop only while the pointer is inside the page-> the panel's own worldBound
    ///   uiDrag.ts:11  pointercancel / lostpointercapture / blur     -> PointerCancelEvent / PointerCaptureOutEvent / focus loss
    ///   uiDrag.ts:11  suppress the click that follows a drag, 250 ms-> ClickSuppressSeconds, a TrickleDown ClickEvent guard
    ///   uiDrag.ts:6   cancelUiDrag() : boolean                      -> <see cref="Cancel"/>, false when nothing is dragging
    ///   uiDrag.ts:5   onUiDragEnd hooks (panels defer redraw)       -> <see cref="DragEnded"/>
    ///
    /// The one deliberate simplification is the drop POLICY: this prototype only ever moves or swaps stacks inside
    /// its own in-memory model (<see cref="SlotGridModel"/>). The reference's policies — transfer across sides, equip
    /// on a weapon slot, quickbar assignment, "Release to cancel" — are sim commands and belong to C-06, which grows
    /// this class by replacing the three callbacks, not by editing the state machine.
    /// </summary>
    public sealed class SlotDragController
    {
        /// <summary>uiDrag.ts:12 — a pointer must travel 7 px before a press becomes a drag.</summary>
        public const float ThresholdPixels = 7f;

        /// <summary>uiDrag.ts:11 — the click that ends a drag is swallowed for 250 ms.</summary>
        public const double ClickSuppressSeconds = 0.25;

        /// <summary>USS class of the element that follows the pointer (style.css .drag-preview).</summary>
        public const string GhostClass = "drag-preview";

        /// <summary>USS class of the stack icon inside the ghost (style.css .drag-stack).</summary>
        public const string GhostStackClass = "drag-stack";

        /// <summary>USS class put on whatever the pointer is over (style.css .drop-target).</summary>
        public const string DropTargetClass = "drop-target";

        private readonly VisualElement _root;
        private readonly Func<SlotAddress, SlotStack> _begin;
        private readonly Func<VisualElement, SlotAddress?> _addressOf;
        private readonly Func<SlotAddress, SlotAddress?, string> _preview;
        private readonly Action<SlotAddress, SlotAddress?> _drop;

        private VisualElement _source;
        private VisualElement _ghost;
        private Label _ghostCount;
        private Label _ghostLabel;
        private VisualElement _highlighted;
        private SlotAddress _from;
        private SlotStack _payload;
        private Vector2 _start;
        private int _pointerId = -1;
        private bool _armed;
        private bool _active;
        private double _suppressClickUntil;

        // Counters the play-mode tests assert on, so "it worked" is a number and not a screenshot.
        public int Activations { get; private set; }
        public int Drops { get; private set; }
        public int Cancels { get; private set; }
        public int SuppressedClicks { get; private set; }
        public int Moves { get; private set; }
        public string LastPreviewLabel { get; private set; } = string.Empty;

        /// <summary>True between the threshold being crossed and the drag finishing (uiDrag.ts draggingUi()).</summary>
        public bool Dragging => _active;

        /// <summary>True once a press has been captured, whether or not it has become a drag yet.</summary>
        public bool Armed => _armed;

        /// <summary>The ghost element, for tests and for a debug overlay. Null while nothing is dragging.</summary>
        public VisualElement Ghost => _ghost;

        /// <summary>uiDrag.ts:5 onUiDragEnd — panels that defer their rebuild refresh once, drop or cancel.</summary>
        public event Action DragEnded;

        public SlotDragController(
            VisualElement root,
            Func<SlotAddress, SlotStack> begin,
            Func<VisualElement, SlotAddress?> addressOf,
            Func<SlotAddress, SlotAddress?, string> preview,
            Action<SlotAddress, SlotAddress?> drop)
        {
            _root = root;
            _begin = begin;
            _addressOf = addressOf;
            _preview = preview;
            _drop = drop;
            // uiDrag.ts:8 — the capture-phase click swallower, so the click that ends a drag never reaches a button.
            _root.RegisterCallback<ClickEvent>(OnClick, TrickleDown.TrickleDown);
        }

        /// <summary>Make one slot element a drag source. Mirrors draggable(node, ...) in uiDrag.ts:10.</summary>
        public void Attach(VisualElement slot, SlotAddress address)
        {
            slot.userData = address;
            slot.RegisterCallback<PointerDownEvent>(e => OnDown(e, slot, address));
            slot.RegisterCallback<PointerMoveEvent>(OnMove);
            slot.RegisterCallback<PointerUpEvent>(OnUp);
            slot.RegisterCallback<PointerCancelEvent>(_ => Cancel());
            slot.RegisterCallback<PointerCaptureOutEvent>(_ => { if (_armed) Cancel(); });
        }

        // ------------------------------------------------------------------ the state machine

        private void OnDown(PointerDownEvent evt, VisualElement slot, SlotAddress address)
        {
            if (evt.button != 0 || _armed) return;              // uiDrag.ts:11 if (e.button !== 0) return
            var stack = _begin(address);
            if (stack.Empty) return;                            // uiDrag.ts:11 if (!payload) return
            _source = slot;
            _from = address;
            _payload = stack;
            _start = new Vector2(evt.position.x, evt.position.y);
            _pointerId = evt.pointerId;
            _armed = true;
            _active = false;
            slot.CapturePointer(evt.pointerId);                 // uiDrag.ts:11 node.setPointerCapture
            evt.StopPropagation();
        }

        private void OnMove(PointerMoveEvent evt)
        {
            if (!_armed || evt.pointerId != _pointerId) return;
            var p = new Vector2(evt.position.x, evt.position.y);
            // uiDrag.ts:12 — below the threshold the press is still a click, not a drag.
            if (!_active && Vector2.Distance(p, _start) < ThresholdPixels) return;
            if (!_active) Activate();
            evt.PreventDefault();
            Track(p);
            evt.StopPropagation();
        }

        private void OnUp(PointerUpEvent evt)
        {
            if (!_armed || evt.pointerId != _pointerId) return;
            var p = new Vector2(evt.position.x, evt.position.y);
            if (_active)
            {
                evt.PreventDefault();
                evt.StopPropagation();
                // uiDrag.ts:13 — a release outside the page drops nothing.
                if (InsidePanel(p))
                {
                    var target = AddressUnder(p);
                    _drop(_from, target);
                    Drops++;
                    if (target.HasValue) Moves++;
                }
            }
            Finish();
        }

        private void OnClick(ClickEvent evt)
        {
            if (Time.realtimeSinceStartupAsDouble >= _suppressClickUntil) return;
            // uiDrag.ts:8 — preventDefault + stopImmediatePropagation in the capture phase.
            evt.PreventDefault();
            evt.StopImmediatePropagation();
            SuppressedClicks++;
        }

        /// <summary>uiDrag.ts:6 cancelUiDrag(). The first link of the Escape chain (uiShell.ts:116) calls this.</summary>
        public bool Cancel()
        {
            if (!_armed) return false;
            var wasDrag = _active;
            Finish();
            if (wasDrag) Cancels++;
            return wasDrag;
        }

        /// <summary>The window losing focus cancels a live drag (uiDrag.ts:11 blur / visibilitychange).</summary>
        public void OnApplicationFocusLost() { if (_active) Cancel(); }

        private void Activate()
        {
            _active = true;
            Activations++;
            _root.AddToClassList("ui-dragging");                 // uiDrag.ts:12 document.body.classList.add
            _ghost = new VisualElement { name = "drag-ghost", pickingMode = PickingMode.Ignore };
            _ghost.AddToClassList(GhostClass);
            var icon = new VisualElement { name = "drag-icon", pickingMode = PickingMode.Ignore };
            icon.AddToClassList(GhostStackClass);
            icon.AddToClassList("slot-icon");
            icon.AddToClassList("item-" + _payload.Item);
            _ghostCount = new Label(Mathf.FloorToInt(_payload.Count).ToString()) { pickingMode = PickingMode.Ignore };
            _ghostCount.AddToClassList("stack-count");
            icon.Add(_ghostCount);
            _ghostLabel = new Label(string.Empty) { pickingMode = PickingMode.Ignore };
            _ghostLabel.AddToClassList("drag-label");
            _ghost.Add(icon);
            _ghost.Add(_ghostLabel);
            // The accessible name the reference sets on the ghost (uiDrag.ts:12 aria-label).
            _ghost.tooltip = "Dragging " + _payload.Item;
            _root.Add(_ghost);
        }

        private void Track(Vector2 p)
        {
            ClearHighlight();
            var picked = _root.panel?.Pick(p);
            var target = Address(picked);
            _highlighted = SlotElement(picked);
            _highlighted?.AddToClassList(DropTargetClass);
            LastPreviewLabel = _preview(_from, target);
            if (_ghostLabel != null) _ghostLabel.text = LastPreviewLabel;
            if (_ghost != null)
            {
                _ghost.style.left = p.x;
                _ghost.style.top = p.y;
            }
        }

        private void Finish()
        {
            if (_source != null && _pointerId >= 0 && _source.HasPointerCapture(_pointerId))
                _source.ReleasePointer(_pointerId);
            ClearHighlight();
            if (_ghost != null) { _ghost.RemoveFromHierarchy(); _ghost = null; }
            _ghostCount = null;
            _ghostLabel = null;
            _root.RemoveFromClassList("ui-dragging");
            var wasActive = _active;
            _armed = false;
            _active = false;
            _source = null;
            _pointerId = -1;
            _payload = SlotStack.None;
            if (!wasActive) return;
            _suppressClickUntil = Time.realtimeSinceStartupAsDouble + ClickSuppressSeconds;
            DragEnded?.Invoke();
        }

        private void ClearHighlight()
        {
            if (_highlighted == null) return;
            _highlighted.RemoveFromClassList(DropTargetClass);
            _highlighted = null;
        }

        // ------------------------------------------------------------------ hit testing

        private bool InsidePanel(Vector2 p)
        {
            var tree = _root.panel?.visualTree;
            return tree != null && tree.worldBound.Contains(p);
        }

        /// <summary>The slot element under a panel point, or null when the pointer is over anything else.</summary>
        public VisualElement SlotElementUnder(Vector2 p) => SlotElement(_root.panel?.Pick(p));

        /// <summary>The slot address under a panel point, or null (the reference's "Release to cancel").</summary>
        public SlotAddress? AddressUnder(Vector2 p) => Address(_root.panel?.Pick(p));

        private VisualElement SlotElement(VisualElement picked)
        {
            var e = picked;
            while (e != null)
            {
                if (_addressOf(e).HasValue) return e;
                e = e.parent;
            }
            return null;
        }

        private SlotAddress? Address(VisualElement picked)
        {
            var e = SlotElement(picked);
            return e == null ? (SlotAddress?)null : _addressOf(e);
        }
    }
}
