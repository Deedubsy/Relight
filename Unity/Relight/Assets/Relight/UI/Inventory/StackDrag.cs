using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Relight.UI
{
    /// <summary>
    /// C-06. What the UI knew about a stack when the player picked it up (reference inventoryPanel.ts
    /// <c>StackDrag</c> / <c>snapshot()</c>). It is carried through the whole gesture and handed back to the sim
    /// so a stack that moved underneath the player is refused rather than silently re-targeted:
    /// <see cref="Layout"/> is the Backpack layout token and <see cref="Source"/> the total the panel saw.
    ///
    /// Pure data, no engine types, so the panel can hold one across a repaint without keeping an element alive.
    /// </summary>
    public sealed class StackSnapshot
    {
        /// <summary>True for a Backpack slot, false for a storage chunk (the reference's <c>side</c>).</summary>
        public bool Pack;

        /// <summary>Backpack: the real slot index. Storage: the chunk's position in the presented list.</summary>
        public int Index;

        /// <summary>The item key, e.g. <c>steel</c> or <c>rifle:3</c>.</summary>
        public string Item = "";

        public string DisplayName = "";

        /// <summary>How many this stack held when it was picked up.</summary>
        public double Count;

        public int StackSize = 1;

        public bool IsWeapon;

        /// <summary>The Backpack layout token (<c>PackLayout.Json</c>) at pick-up time.</summary>
        public string Layout = "";

        /// <summary>The total the SOURCE held at pick-up: carried count for a pack stack, pooled count for a store.</summary>
        public double Source;

        /// <summary>The store the panel was paired with at pick-up; a change refuses with "Store changed; …".</summary>
        public int Target = -1;

        /// <summary>
        /// GP-UX-2. True when the stack was picked up out of the Home workshop's OUTPUT TRAY.
        ///
        /// It needs its own flag rather than reusing <see cref="Pack"/>: the tray is neither the Backpack nor the
        /// paired store. It has no machine id, so a "store transfer" cannot address it, and the only command that
        /// moves anything out of it is <c>CollectWorkshopCommand</c>. Without this a tray stack would arrive at the
        /// drop handler looking exactly like a store chunk and be sent as a transfer against whatever machine
        /// happened to be open.
        /// </summary>
        public bool Tray;
    }

    /// <summary>
    /// C-06. The one live drag, and the hooks around it — the port of reference <c>uiDrag.ts</c>'s module state
    /// (<c>cancelCurrent</c>, <c>suppress</c>, <c>endHooks</c>). There is at most one drag in the whole UI, which
    /// is why this is static: the reference's guarantee that Escape cancels "the" drag, and that a panel which
    /// deferred its repaint refreshes once the gesture ends, both depend on there being exactly one.
    /// </summary>
    public static class UiDrag
    {
        /// <summary>The reference's click suppressor window (uiDrag.ts: <c>setTimeout(…, 250)</c>).</summary>
        public const double SuppressSeconds = 0.250;

        private static readonly List<Action> EndHooks = new List<Action>();
        private static Action _cancel;
        private static double _suppressUntil = double.NegativeInfinity;

        /// <summary>True while a drag has passed the threshold and is live (reference <c>draggingUi()</c>).</summary>
        public static bool Dragging => _cancel != null;

        /// <summary>
        /// True for the 250 ms after a drag ended. The reference swallows the synthetic click that a pointer-up
        /// produces at the end of a drag; UI Toolkit raises the same click, so slot handlers ask this first.
        /// </summary>
        public static bool ClickSuppressed => Time.unscaledTimeAsDouble < _suppressUntil;

        /// <summary>Panels that defer repainting during a drag refresh once it ends, dropped or cancelled.</summary>
        public static void OnDragEnd(Action fn)
        {
            if (fn != null && !EndHooks.Contains(fn)) EndHooks.Add(fn);
        }

        public static void RemoveDragEnd(Action fn)
        {
            if (fn != null) EndHooks.Remove(fn);
        }

        /// <summary>Reference <c>cancelUiDrag()</c>: true when there was one to cancel. This is Escape link 100.</summary>
        public static bool Cancel()
        {
            if (_cancel == null) return false;
            var run = _cancel;
            run();
            return true;
        }

        internal static void Begin(Action cancel) => _cancel = cancel;

        internal static void Ended(bool wasActive)
        {
            _cancel = null;
            if (!wasActive) return;
            _suppressUntil = Time.unscaledTimeAsDouble + SuppressSeconds;
            for (var i = 0; i < EndHooks.Count; i++) EndHooks[i]?.Invoke();
        }
    }

    /// <summary>
    /// C-06. Escape link at <see cref="EscapeOrder.CancelDrag"/> (100): cancelling a drag comes before everything
    /// else, exactly as the reference's ladder does (uiShell.ts:116).
    ///
    /// It is registered by <see cref="InventoryPanelController"/> and takes effect without editing
    /// <see cref="EscapeChain"/>: <see cref="EscapeChain.Register"/> inserts a handler AFTER every handler of equal
    /// order, so a link registered at order 100 runs immediately after the inert placeholder that sits there and
    /// returns false. (The placeholder is still removed — see the report — but the chain is correct either way.)
    /// </summary>
    public sealed class CancelDragLink : IEscapeHandler
    {
        public int Order => EscapeOrder.CancelDrag;
        public bool OnEscape() => UiDrag.Cancel();
    }

    /// <summary>
    /// GP-UX-9. Where the pointer is right now, and whether the split modifier is held right now.
    ///
    /// Both halves are read from the live pointer event rather than taken at pick-up, because both are decisions
    /// the player makes DURING the gesture: <see cref="Position"/> is what a drop has to be resolved against when
    /// <c>panel.Pick</c> lands between slots, and <see cref="Split"/> is a choice the ghost's preview line reports
    /// before the player commits to it.
    /// </summary>
    public readonly struct DragPoint
    {
        /// <summary>The pointer position in panel coordinates.</summary>
        public readonly Vector2 Position;

        /// <summary>True while Shift is held — the split gesture (§5.3).</summary>
        public readonly bool Split;

        public DragPoint(Vector2 position, bool split) { Position = position; Split = split; }
    }

    /// <summary>
    /// C-06. The port of reference <c>uiDrag.ts</c> <c>draggable()</c> as a UI Toolkit
    /// <see cref="PointerManipulator"/>, attached to every inventory slot, equipment slot and quickbar slot.
    ///
    /// The gesture is the reference's, step for step:
    /// <list type="number">
    /// <item>pointer down on button 0 asks <c>start</c> for a snapshot; no snapshot, no drag;</item>
    /// <item>nothing happens until the pointer has moved <see cref="ThresholdPx"/> (7 px, the reference's
    ///       <c>Math.hypot(...) &lt; 7</c>) — so a click is still a click;</item>
    /// <item>past the threshold a <c>.drag-preview</c> ghost appears, follows the pointer, and its label is
    ///       whatever <c>preview</c> says about the element under the pointer; the element under the pointer
    ///       gets <c>.drop-target</c> and loses it on the next move;</item>
    /// <item>pointer up inside the panel calls <c>drop</c> with the element under the pointer; outside, or on
    ///       cancel / lost capture / Escape, nothing is dropped;</item>
    /// <item>either way the ghost goes, the highlight goes, and the 250 ms click suppressor starts.</item>
    /// </list>
    ///
    /// Four Unity-specific notes:
    /// * the element under the pointer is found with <c>panel.Pick</c>, the port of <c>document.elementFromPoint</c>.
    ///   The ghost is <c>picking-mode="Ignore"</c> in the UXML so it can never be its own drop target;
    /// * <c>preview</c> and <c>drop</c> are handed a <see cref="DragPoint"/> as well as that element, because the
    ///   element alone is not enough to name a slot: <c>Pick</c> answers "what is exactly under this pixel", and a
    ///   wrapping slot grid inside a ScrollView has gutters, a background and a viewport that are not slots. The
    ///   controller resolves the picked element first and falls back to the nearest slot in the grid the pointer
    ///   is inside (GP-UX-9), so a release a few pixels off a slot still lands in the slot the player aimed at;
    /// * the ghost is moved with <see cref="IStyle.translate"/> and marked
    ///   <see cref="UsageHints.DynamicTransform"/> — the one place this file touches <c>style</c>, because a
    ///   position that changes every frame is not expressible as a class;
    /// * the browser's <c>blur</c> / <c>visibilitychange</c> cancels have no direct equivalent; the controller
    ///   calls <see cref="CancelIfDragging"/> from <c>OnApplicationFocus(false)</c> and <c>OnDisable</c>, and
    ///   <see cref="PointerCaptureOutEvent"/> covers the rest.
    /// </summary>
    public sealed class StackDragManipulator : PointerManipulator
    {
        /// <summary>Reference uiDrag.ts: the drag does not start until the pointer has moved 7 px.</summary>
        public const float ThresholdPx = 7f;

        private readonly Func<StackSnapshot> _start;
        private readonly Func<VisualElement, StackSnapshot, DragPoint, string> _preview;
        private readonly Action<VisualElement, StackSnapshot, DragPoint> _drop;
        private readonly Func<VisualElement> _ghost;

        private StackSnapshot _payload;
        private Vector2 _from;
        private int _pointer = -1;
        private bool _active;
        private VisualElement _highlight, _liveGhost;
        private Vector2 _lastPosition;

        public StackDragManipulator(Func<StackSnapshot> start,
            Func<VisualElement, StackSnapshot, DragPoint, string> preview,
            Action<VisualElement, StackSnapshot, DragPoint> drop,
            Func<VisualElement> ghost)
        {
            _start = start;
            _preview = preview;
            _drop = drop;
            _ghost = ghost;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            // TrickleDown matters. A slot is a Button, and Button's own Clickable also handles PointerDown/Up at
            // the target phase. Registering here in the CAPTURE phase means that when a real drag ends, this
            // manipulator can StopPropagation before the Clickable ever sees the pointer-up, so a finished drag
            // does not also fire the slot's click. (The 250 ms suppressor stays as well: it covers the paths that
            // do not go through this element at all, exactly as uiDrag.ts's own suppressor does.)
            target.RegisterCallback<PointerDownEvent>(OnDown, TrickleDown.TrickleDown);
            target.RegisterCallback<PointerMoveEvent>(OnMove, TrickleDown.TrickleDown);
            target.RegisterCallback<PointerUpEvent>(OnUp, TrickleDown.TrickleDown);
            target.RegisterCallback<PointerCancelEvent>(OnCancelEvent, TrickleDown.TrickleDown);
            target.RegisterCallback<PointerCaptureOutEvent>(OnCaptureOut);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(OnDown, TrickleDown.TrickleDown);
            target.UnregisterCallback<PointerMoveEvent>(OnMove, TrickleDown.TrickleDown);
            target.UnregisterCallback<PointerUpEvent>(OnUp, TrickleDown.TrickleDown);
            target.UnregisterCallback<PointerCancelEvent>(OnCancelEvent, TrickleDown.TrickleDown);
            target.UnregisterCallback<PointerCaptureOutEvent>(OnCaptureOut);
        }

        /// <summary>Cancel a live drag from outside: focus loss, panel close, Escape.</summary>
        public void CancelIfDragging()
        {
            if (_payload != null) Finish();
        }

        private void OnDown(PointerDownEvent e)
        {
            if (e.button != 0 || _payload != null) return;
            var payload = _start == null ? null : _start();
            if (payload == null) return;
            _payload = payload;
            _from = e.position;
            _pointer = e.pointerId;
            _active = false;
            target.CapturePointer(e.pointerId);
            UiDrag.Begin(Finish);
        }

        private void OnMove(PointerMoveEvent e)
        {
            if (_payload == null || e.pointerId != _pointer) return;
            var here = (Vector2)e.position;
            if (!_active && Vector2.Distance(here, _from) < ThresholdPx) return;

            var ghost = _ghost == null ? null : _ghost();
            if (!_active)
            {
                _active = true;
                if (ghost != null)
                {
                    _liveGhost=ghost;
                    ghost.RegisterCallback<GeometryChangedEvent>(OnGhostGeometry);
                    ghost.BringToFront();
                    ghost.style.display = DisplayStyle.Flex;
                    ItemIcons.Paint(ghost.Q<VisualElement>("drag-icon"),ghost.Q<Label>("drag-icon-code"),_payload.Item);
                    var stack = ghost.Q<Label>("drag-stack");
                    if (stack != null)
                        stack.text = _payload.Count > 0
                            ? Relight.Sim.PackLayout.Num(Math.Floor(_payload.Count)) + " " + _payload.DisplayName
                            : _payload.DisplayName;
                }
            }

            _lastPosition=here;
            ClearHighlight();
            var under = Pick(here);
            var label = _preview == null ? "" : _preview(under, _payload, new DragPoint(here, e.shiftKey));
            if (ghost != null)
            {
                var text = ghost.Q<Label>("drag-label");
                if (text != null) text.text = label ?? "";
                PositionGhost();
            }
            e.StopPropagation();
        }

        private void OnGhostGeometry(GeometryChangedEvent e) { if(_active)PositionGhost(); }

        /// <summary>The class that puts the ghost's card on the LEFT of the icon, near the right-hand edge.</summary>
        public const string FlipClass = "drag-flip";

        /// <summary>
        /// Put the ghost where the pointer is — and specifically, put <b>the icon</b> where the pointer is.
        ///
        /// The first port positioned the whole card at pointer+(16,16) and then clamped the card inside its
        /// parent. That reads fine in the middle of the screen and comes apart at the edges: the card is 300px
        /// wide, so anywhere within 300px of the right-hand edge the clamp held the card still while the pointer
        /// kept going, and the icon — the thing the player believes they are carrying — was left behind by up to a
        /// card's width. The storage column and the workshop tray are BOTH on that side of the drawer, so the
        /// gesture that detached was the ordinary one: dragging a stack out of storage into the Backpack.
        ///
        /// So the anchor is the icon, not the card. The icon's centre is pinned to the pointer in both axes, and
        /// when there is no room for the card to the right of it the card is drawn to the LEFT instead
        /// (<see cref="FlipClass"/>, a USS row-reverse) rather than sliding the icon away. Padding is read back
        /// from the resolved style so a restyle in UI Builder cannot put the icon a few pixels off the cursor.
        /// </summary>
        private void PositionGhost()
        {
            var ghost=_liveGhost;if(ghost?.parent==null)return;
            var icon=ghost.Q<VisualElement>("drag-icon");
            var place=Place(
                ghost.parent.WorldToLocal(_lastPosition),
                ghost.parent.contentRect.width,
                Size(ghost.resolvedStyle.width,300),
                Size(ghost.resolvedStyle.height,56),
                icon==null?42f:Size(icon.resolvedStyle.width,42),
                Size(ghost.resolvedStyle.paddingLeft,0),
                Size(ghost.resolvedStyle.paddingRight,0));
            ghost.EnableInClassList(FlipClass,place.Flip);
            ghost.style.translate=new StyleTranslate(new Translate(place.X,place.Y,0));
        }

        /// <summary>Where the ghost card goes, and which way round it faces. See <see cref="Place"/>.</summary>
        public readonly struct GhostPlace
        {
            public readonly float X, Y;
            public readonly bool Flip;
            public GhostPlace(float x,float y,bool flip){X=x;Y=y;Flip=flip;}

            /// <summary>Where the icon's centre ends up, given the card's own metrics. The point of the exercise.</summary>
            public float IconCentreX(float cardWidth,float iconWidth,float padLeft,float padRight)
                => Flip ? X+cardWidth-padRight-iconWidth*0.5f : X+padLeft+iconWidth*0.5f;

            public float IconCentreY(float cardHeight) => Y+cardHeight*0.5f;
        }

        /// <summary>
        /// The ghost's placement, as pure arithmetic so it can be asserted without a pointer device.
        ///
        /// <paramref name="pointer"/> is in the ghost parent's coordinates; the result is the card's top-left
        /// corner in the same space, chosen so that the ICON's centre lands exactly on the pointer. That is the
        /// whole rule, and everything else follows from it — <c>align-items: center</c> puts the icon on the card's
        /// vertical centre line, so Y is just the pointer less half the card.
        ///
        /// When the card would not fit to the right of the pointer the card is drawn to the LEFT of the icon
        /// instead (<see cref="GhostPlace.Flip"/> / <see cref="FlipClass"/>, a USS <c>row-reverse</c>). Flipping is
        /// what replaced clamping: a clamp keeps the CARD on screen by sliding it — and the icon with it — away
        /// from the cursor, which is the defect this fixes. Nothing here clamps, so within half a card of the top
        /// or bottom edge the card's far end may overhang; the icon, which is the thing the player is carrying,
        /// stays under the pointer, and the pointer is by definition on screen.
        /// </summary>
        public static GhostPlace Place(Vector2 pointer,float boundsWidth,float cardWidth,float cardHeight,
                                       float iconWidth,float padLeft,float padRight)
        {
            var flip=boundsWidth>0 && pointer.x+(cardWidth-padLeft-iconWidth*0.5f)>boundsWidth;
            var x=flip?pointer.x-cardWidth+padRight+iconWidth*0.5f:pointer.x-padLeft-iconWidth*0.5f;
            return new GhostPlace(x,pointer.y-cardHeight*0.5f,flip);
        }

        private static float Size(float resolved,float fallback)=>float.IsNaN(resolved)?fallback:resolved;

        private void OnUp(PointerUpEvent e)
        {
            if (_payload == null || e.pointerId != _pointer) return;
            var payload = _payload;
            var active = _active;
            var under = Pick(e.position);
            var at = new DragPoint(e.position, e.shiftKey);
            if (active)
            {
                // Capture phase: this stops the pointer-up reaching the Button's Clickable, so the drag that just
                // ended does not also count as a click on the slot it started from.
                e.StopPropagation();
                var inside=InsidePanel(e.position);
                Finish();
                if(inside)_drop?.Invoke(under,payload,at);
                return;
            }
            Finish();
        }

        private void OnCancelEvent(PointerCancelEvent e)
        {
            if (_payload != null && e.pointerId == _pointer) Finish();
        }

        private void OnCaptureOut(PointerCaptureOutEvent e)
        {
            // Lost capture is the reference's `lostpointercapture`: the gesture is over and nothing is dropped.
            if (_payload != null) Finish();
        }

        private VisualElement Pick(Vector2 panelPosition)
        {
            var panel = target?.panel;
            return panel?.Pick(panelPosition);
        }

        private bool InsidePanel(Vector2 p)
        {
            // Reference: a pointer-up outside the window drops nothing.
            var root = target?.panel?.visualTree;
            return root != null && root.worldBound.Contains(p);
        }

        private void ClearHighlight()
        {
            if (_highlight == null) return;
            _highlight.RemoveFromClassList(DropTargetClass);
            _highlight.RemoveFromClassList("drop-invalid");
            _highlight = null;
        }

        /// <summary>The reference's `.drop-target` class, added to whatever the pointer is over.</summary>
        public const string DropTargetClass = "drop-target";

        /// <summary>Highlight one element as the current drop target. Called by the panel's preview callback.</summary>
        public void Highlight(VisualElement e,bool invalid=false)
        {
            if (_highlight == e) return;
            ClearHighlight();
            if (e == null) return;
            _highlight = e;
            _highlight.AddToClassList(invalid?"drop-invalid":DropTargetClass);
        }

        private void Finish()
        {
            var wasActive = _active;
            _liveGhost?.UnregisterCallback<GeometryChangedEvent>(OnGhostGeometry);
            _liveGhost=null;
            _active = false;
            _payload = null;
            // Release only what this manipulator is responsible for. After a REAL drag the pointer-up was stopped
            // in the capture phase, so the Button's Clickable will never release it and this must; after a plain
            // click the Clickable still owns the release and taking it away here would swallow the click.
            if (wasActive && _pointer >= 0 && target != null && target.HasPointerCapture(_pointer))
                target.ReleasePointer(_pointer);
            _pointer = -1;
            ClearHighlight();
            var ghost = _ghost == null ? null : _ghost();
            if (ghost != null)
            {
                ghost.style.display = DisplayStyle.None;
                ghost.RemoveFromClassList(FlipClass);   // the next drag starts facing the usual way
            }
            UiDrag.Ended(wasActive);
        }
    }
}
