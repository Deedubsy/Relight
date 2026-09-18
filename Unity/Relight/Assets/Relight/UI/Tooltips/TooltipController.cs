using UnityEngine;
using UnityEngine.UIElements;

namespace Relight.UI
{
    /// <summary>
    /// C5. The single tooltip of the GameUI document: one element, one delay, one position calculation.
    ///
    /// It lives on the same GameObject as the GameUI <see cref="UIDocument"/> and finds <c>#tooltip</c> in that
    /// document (authored last in <c>GameUI.uxml</c>, so it draws over every panel — USS has no z-index and the
    /// layer order is document order, UI_AND_ONBOARDING.md §4.1). The element and its layer are authored
    /// <c>picking-mode="Ignore"</c>: a tooltip that could be picked would take the pointer off the very element
    /// that is keeping it open, and then flicker.
    ///
    /// The delay is honoured with the element scheduler rather than a coroutine, because the scheduler is bound to
    /// the panel: if the document is torn down mid-wait, the callback never fires.
    ///
    /// Inline styles: <see cref="Place"/> writes <c>left</c>/<c>top</c> on the tooltip. That is the same exception
    /// TECHNICAL_ARCHITECTURE.md §8.2 already grants the drag ghost and the slot menu — a position that only exists
    /// at runtime cannot be a USS class.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Relight/Tooltip Controller")]
    public sealed class TooltipController : MonoBehaviour
    {
        /// <summary>Milliseconds the pointer must rest on a target before the tooltip appears (C5).</summary>
        public const long DelayMs = 300;

        /// <summary>Gap between the target and the tooltip, and the minimum margin from the window edge.</summary>
        public const float Gap = 8f;

        /// <summary>USS state, never an inline display write.</summary>
        public const string HiddenClass = "is-hidden";

        [Tooltip("The document holding GameUI.uxml. Found on this object if left empty.")]
        [SerializeField] private UIDocument document;

        private VisualElement _root, _layer, _tip, _rows;
        private Label _title, _body, _footer;
        private VisualElement _target;      // what the pointer is over (may still be inside the delay)
        private VisualElement _shownFor;    // what the visible tooltip belongs to, or null
        private IVisualElementScheduledItem _pending;

        /// <summary>True while a tooltip is on screen. The PlayMode tests read this.</summary>
        public bool Visible => _shownFor != null;

        /// <summary>The element the visible tooltip belongs to, or null.</summary>
        public VisualElement ShownFor => _shownFor;

        private void Awake()
        {
            if (document == null) document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            Tooltips.Controller = this;
            Bind();
        }

        private void OnDisable()
        {
            HideNow();
            if (ReferenceEquals(Tooltips.Controller, this)) Tooltips.Controller = null;
        }

        private void Start()
        {
            if (_tip == null) Bind();   // the document root may not exist yet in OnEnable (see UiShell.Start)
        }

        /// <summary>Re-query the document. Public so a test or a hot-reloaded UXML can rebind.</summary>
        public void Bind()
        {
            var root = document == null ? null : document.rootVisualElement;
            if (root == null) return;
            _root = root;
            _layer = root.Q<VisualElement>("tooltip-layer");
            _tip = root.Q<VisualElement>("tooltip");
            if (_tip == null) return;
            _title = _tip.Q<Label>("tooltip-title");
            _body = _tip.Q<Label>("tooltip-body");
            _rows = _tip.Q<VisualElement>("tooltip-rows");
            _footer = _tip.Q<Label>("tooltip-footer");

            _pending = _root.schedule.Execute(Reveal);
            _pending.Pause();

            // The tooltip's own size is only known after it has been laid out with this frame's text, so the
            // final placement happens when the geometry lands, not when the text is set.
            _tip.RegisterCallback<GeometryChangedEvent>(_ => Place());
            _tip.AddToClassList(HiddenClass);
        }

        /// <summary>The pointer entered <paramref name="target"/>: start (or restart) the delay.</summary>
        internal void Request(VisualElement target)
        {
            if (_tip == null || target == null) return;
            if (ReferenceEquals(target, _shownFor)) return;   // already showing this one
            _target = target;
            if (_shownFor != null) Hide();                    // a different target: the old tooltip goes at once
            _pending?.ExecuteLater(DelayMs);
        }

        /// <summary>The pointer left <paramref name="target"/> (or it was destroyed).</summary>
        internal void Cancel(VisualElement target)
        {
            if (target != null && !ReferenceEquals(target, _target) && !ReferenceEquals(target, _shownFor)) return;
            _pending?.Pause();
            _target = null;
            Hide();
        }

        /// <summary>Hide whatever is showing and forget the pending one.</summary>
        public void HideNow()
        {
            _pending?.Pause();
            _target = null;
            Hide();
        }

        private float _refreshAt;
        private static bool OnScreen(VisualElement e)
        {
            if (e == null || e.panel == null) return false;
            for(var p=e;p!=null;p=p.parent)
                if(p.resolvedStyle.display==DisplayStyle.None || p.resolvedStyle.visibility==Visibility.Hidden) return false;
            return e.worldBound.width>0 && e.worldBound.height>0;
        }
        private void Update()
        {
            if(UiDrag.Dragging || (_target!=null && !OnScreen(_target))) { HideNow(); return; }
            if(_shownFor==null || Time.unscaledTime<_refreshAt)return;
            _refreshAt=Time.unscaledTime+.15f;
            Reveal();
        }

        private void Reveal()
        {
            var target = _target;
            if (_tip == null || !OnScreen(target) || UiDrag.Dragging) return;
            var content = Tooltips.ProviderFor(target)?.Invoke();
            if (content == null) return;

            SetLine(_title, content.Title);
            SetLine(_body, content.Body);
            SetLine(_footer, content.Footer);
            FillRows(content);

            _shownFor = target;
            _tip.RemoveFromClassList(HiddenClass);
            Place();
        }

        private void Hide()
        {
            if (_tip == null) return;
            _shownFor = null;
            _tip.AddToClassList(HiddenClass);
        }

        private static void SetLine(Label l, string text)
        {
            if (l == null) return;
            var has = !string.IsNullOrEmpty(text);
            l.text = has ? text : "";
            if (has) l.RemoveFromClassList(HiddenClass);
            else l.AddToClassList(HiddenClass);
        }

        /// <summary>
        /// The ledger. Rows are the one thing here that is created from C#: their number is the recipe's, not the
        /// author's, so they cannot be authored in UXML. Existing rows are reused and the surplus hidden, so a
        /// pointer sweeping a grid does not allocate on every slot.
        /// </summary>
        private void FillRows(TooltipContent content)
        {
            if (_rows == null) return;
            var rows = content.Rows;
            var want = rows == null ? 0 : rows.Count;

            while (_rows.childCount < want)
            {
                var row = new VisualElement { name = "tooltip-row-" + _rows.childCount, pickingMode = PickingMode.Ignore };
                row.AddToClassList("tooltip-row");
                var label = new Label { name = "row-label", pickingMode = PickingMode.Ignore };
                label.AddToClassList("tooltip-row-label");
                var value = new Label { name = "row-value", pickingMode = PickingMode.Ignore };
                value.AddToClassList("tooltip-row-value");
                row.Add(label);
                row.Add(value);
                _rows.Add(row);
            }

            for (var i = 0; i < _rows.childCount; i++)
            {
                var row = _rows[i];
                if (i >= want) { row.AddToClassList(HiddenClass); continue; }
                row.RemoveFromClassList(HiddenClass);
                var (label, value, ok) = rows[i];
                if (row.childCount >= 2)
                {
                    if (row[0] is Label l) l.text = label ?? "";
                    if (row[1] is Label v)
                    {
                        v.text = value ?? "";
                        if (ok) v.RemoveFromClassList("ui-bad");
                        else v.AddToClassList("ui-bad");
                    }
                }
            }

            if (want == 0) _rows.AddToClassList(HiddenClass);
            else _rows.RemoveFromClassList(HiddenClass);
        }

        /// <summary>
        /// Below the target by preference, above it when there is no room, and always inside the window with a
        /// <see cref="Gap"/> margin — C5's "clamped inside the panel bounds".
        /// </summary>
        private void Place()
        {
            if (_tip == null || _shownFor == null) return;
            var origin = _layer != null ? _layer.worldBound : (_root != null ? _root.worldBound : new Rect());
            if (origin.width <= 0f || origin.height <= 0f) return;

            var anchor = _shownFor.worldBound;
            var w = _tip.resolvedStyle.width;
            var h = _tip.resolvedStyle.height;
            if (w <= 0f || h <= 0f) return;

            var x = anchor.xMin;
            var y = anchor.yMax + Gap;

            if (y + h > origin.yMax - Gap) y = anchor.yMin - h - Gap;
            if (x + w > origin.xMax - Gap) x = origin.xMax - Gap - w;
            if (x < origin.xMin + Gap) x = origin.xMin + Gap;
            if (y < origin.yMin + Gap) y = origin.yMin + Gap;

            // Writing left/top changes the layout, which raises GeometryChangedEvent, which calls this again:
            // stop when the answer has stopped moving, or the two would ping-pong forever.
            var left = x - origin.xMin;
            var top = y - origin.yMin;
            if (Mathf.Abs(_tip.resolvedStyle.left - left) < 0.5f && Mathf.Abs(_tip.resolvedStyle.top - top) < 0.5f) return;
            _tip.style.left = left;
            _tip.style.top = top;
        }
    }
}
