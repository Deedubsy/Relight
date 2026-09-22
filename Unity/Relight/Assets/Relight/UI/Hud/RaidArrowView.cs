using Relight.Sim.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Relight.UI
{
    /// <summary>
    /// REL-74 (E-20, U-D-66 (7)): the screen-edge arrow to the raid. <see cref="HudViewModel"/> says whether there
    /// is one and where it points in the world; <see cref="EdgeArrow"/> says where on screen it sits. This class only
    /// projects the world point through the camera and moves the element, so it runs every frame, not on the HUD's
    /// 150 ms throttle: the camera moves every frame and a throttled arrow would lag behind it.
    ///
    /// The box the arrow keeps inside is the world between the HUD's side columns and its top and bottom strips, so
    /// the arrow never sits on a readout. The head's shape is painted here in the head's own USS colour; the arrow's
    /// place is the one inline style the HUD writes, as the world-target outline's is (<see cref="GameplayDock"/>).
    /// </summary>
    public sealed class RaidArrowView
    {
        /// <summary>Put on the arrow when its label reads above the head (<c>Hud.uss</c>).</summary>
        public const string LabelAboveClass = "label-above";

        // Half of #raid-arrow's 36 px, so the element's centre is the placement's point.
        private const float Half = 18f;

        // The HUD ground (--ui-bg), round the head so it reads over a lit street as well as a dark one.
        private static readonly Color Outline = new Color(0.082f, 0.114f, 0.118f, 0.9f);

        private readonly VisualElement _root, _arrow, _head;
        private readonly Label _label;
        private double _angle = double.NaN;

        /// <param name="root">The HUD document's root, which carries <c>compact-ui</c>.</param>
        public RaidArrowView(VisualElement root)
        {
            _root = root;
            _arrow = root?.Q<VisualElement>("raid-arrow");
            _head = root?.Q<VisualElement>("raid-arrow-head");
            _label = root?.Q<Label>("raid-arrow-label");
            if (_head != null) _head.generateVisualContent += Draw;
        }

        /// <summary>Stop painting the head, before a rebind makes a new view over the same elements.</summary>
        public void Detach()
        {
            if (_head != null) _head.generateVisualContent -= Draw;
        }

        public void Paint(HudViewModel model)
        {
            if (_arrow == null) return;
            var camera = Camera.main;
            var panel = _root.panel;
            if (model == null || !model.RaidPointerVisible || camera == null || panel == null)
            {
                Show(false);
                return;
            }

            var s = camera.WorldToScreenPoint(new Vector3((float)model.RaidPointerX, -(float)model.RaidPointerY, 0));
            var p = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(s.x, Screen.height - s.y));
            Box(out var left, out var top, out var right, out var bottom);
            var at = EdgeArrow.Place(p.x, p.y, left, top, right, bottom);
            if (!at.Valid)
            {
                Show(false);
                return;
            }

            _arrow.style.left = (float)at.X - Half;
            _arrow.style.top = (float)at.Y - Half;
            if (at.AngleDeg != _angle)
            {
                _angle = at.AngleDeg;
                _head.style.rotate = new StyleRotate(new Rotate(new Angle((float)_angle, AngleUnit.Degree)));
            }
            if (_label != null && _label.text != model.RaidPointerLabel) _label.text = model.RaidPointerLabel;
            _arrow.EnableInClassList(LabelAboveClass, at.OnScreen || at.Y > (top + bottom) / 2);
            Show(true);
        }

        /// <summary>
        /// The world the HUD leaves visible: inside the goal card and the right-hand column (their widths in
        /// <c>industrial.uss</c>, plus a margin), below the status strip and above the engineer block and the
        /// action bar. A window too small for that falls back to a plain margin round the edge.
        /// </summary>
        private void Box(out double left, out double top, out double right, out double bottom)
        {
            var w = _root.resolvedStyle.width;
            var h = _root.resolvedStyle.height;
            if (float.IsNaN(w) || w < 1) w = Screen.width;
            if (float.IsNaN(h) || h < 1) h = Screen.height;
            var side = _root.ClassListContains("compact-ui") ? 300 : 340;
            left = side;
            right = w - side;
            top = 96;
            bottom = h - 170;
            if (right - left < 160) { left = 24; right = w - 24; }
            if (bottom - top < 120) { top = 70; bottom = h - 24; }
        }

        /// <summary>A chevron pointing right; the element's rotation turns it.</summary>
        private void Draw(MeshGenerationContext mgc)
        {
            var r = _head.contentRect;
            var w = r.width;
            var h = r.height;
            if (w < 1 || h < 1) return;
            var p = mgc.painter2D;
            p.fillColor = _head.resolvedStyle.color;
            p.strokeColor = Outline;
            p.lineWidth = 2f;
            p.lineJoin = LineJoin.Round;
            p.BeginPath();
            p.MoveTo(new Vector2(w * 0.10f, h * 0.12f));
            p.LineTo(new Vector2(w * 0.95f, h * 0.50f));
            p.LineTo(new Vector2(w * 0.10f, h * 0.88f));
            p.LineTo(new Vector2(w * 0.34f, h * 0.50f));
            p.ClosePath();
            p.Fill();
            p.Stroke();
        }

        private void Show(bool on) => _arrow.EnableInClassList(HudController.HiddenClass, !on);
    }
}
