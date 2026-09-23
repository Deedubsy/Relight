using System.Collections.Generic;
using UnityEngine;

namespace Relight.Presentation
{
    /// <summary>
    /// REL-132. The Gate's leaf: one bar across the doorway that parts as the engineer arrives and closes behind
    /// them. Strokes only, pooled and parented exactly as <see cref="BuildingHealthVisual"/> pools its bar, at
    /// <see cref="DrawOrder.MachineCue"/> so it reads over the darkness the port is always in.
    ///
    /// <para><b>This is the whole of "open".</b> The simulation's gate never changes: it is solid, it is opaque, it
    /// has hit points, and the engineer walks it whether or not this drawing has caught up
    /// (<c>GateRules</c> says why). So nothing here is allowed to be asked a question by the sim, and nothing is —
    /// the class has no reference to it at all. What it gets is a bool and a delta time.</para>
    ///
    /// <para><b>Why it is animated rather than snapped.</b> A bar that vanished the instant the engineer came into
    /// range would read as the gate breaking, not opening. <see cref="OpenPerSecond"/> is set so the leaf takes
    /// about a third of a second end to end, which at walking pace means it is moving while the engineer covers
    /// the last tile — the thing that makes it look like the gate reacted to them.</para>
    ///
    /// <para>The numbers are the assistant's under U-D-28; the owner asked only for a gate that opens
    /// automatically and shuts behind. Recorded as U-P-36.</para>
    /// </summary>
    internal sealed class GateVisual
    {
        /// <summary>The closed leaf: the copper of the build icon, darkened to sit against a night street.</summary>
        private static readonly Color Leaf = new Color(.64f, .58f, .49f, 1);

        /// <summary>The frame either side, which does not move. Darker, so the leaf is the part that reads.</summary>
        private static readonly Color Frame = new Color(.33f, .31f, .28f, 1);

        /// <summary>Half the doorway, in tiles: a little inside the tile so the frame posts are visible at the ends.</summary>
        private const float HalfSpan = .42f;

        /// <summary>How far each half of the leaf retracts when fully open, in tiles.</summary>
        private const float Travel = .34f;

        /// <summary>The frame post at each end of the doorway, in tiles.</summary>
        private const float PostLength = .12f;

        /// <summary>Leaf thickness as a fraction of the camera's half-height, clamped — see BuildingHealthVisual.</summary>
        private const float ThicknessOfHalfView = .022f;
        private const float MinThickness = .12f;
        private const float MaxThickness = .40f;

        /// <summary>Openness travelled per second. 3 is roughly a third of a second from shut to wide.</summary>
        public const float OpenPerSecond = 3f;

        private readonly Transform _parent;
        private readonly Material _material;
        private readonly List<LineRenderer> _strokes = new List<LineRenderer>(4);
        private int _used;

        /// <summary>0 shut, 1 fully open. Eased toward the target every <see cref="Draw"/>. Read by the play tests.</summary>
        public float Openness { get; private set; }

        /// <summary>True between the last <see cref="Draw"/> and the next <see cref="Hide"/>; read by the play tests.</summary>
        public bool Showing { get; private set; }

        public GateVisual(Transform parent, Material material)
        {
            _parent = parent;
            _material = material;
        }

        /// <summary>Put the leaf away — the gate is a wreck, or this view is no longer a gate.</summary>
        public void Hide()
        {
            for (var i = 0; i < _strokes.Count; i++) _strokes[i].enabled = false;
            _used = 0;
            Showing = false;
        }

        /// <summary>
        /// One frame of the gate. <paramref name="horizontal"/> lays the leaf across the X axis,
        /// <paramref name="open"/> is where it is being driven to, and <paramref name="halfView"/> is the camera's
        /// orthographic size. The parent transform already stands on the gate's centre.
        /// </summary>
        public void Draw(bool horizontal, bool open, float dt, float halfView)
        {
            _used = 0;
            Showing = true;
            Openness = Mathf.Clamp01(Openness + (open ? 1 : -1) * OpenPerSecond * Mathf.Max(0, dt));
            var t = Mathf.Clamp(halfView * ThicknessOfHalfView, MinThickness, MaxThickness);
            // Smoothstep, so the leaf eases out of the shut position and into the open one rather than sliding at
            // one speed and stopping dead.
            var e = Openness * Openness * (3f - 2f * Openness);
            var inner = HalfSpan - Travel * e;

            // The frame first, so the leaf draws over it where they meet.
            Post(horizontal, -HalfSpan, -HalfSpan + PostLength, t);
            Post(horizontal, HalfSpan - PostLength, HalfSpan, t);

            // Two halves meeting in the middle when shut. Drawn as two strokes at every openness rather than one
            // stroke that splits, so there is no frame where the leaf pops from one shape to two.
            Along(horizontal, -HalfSpan, -inner, Leaf, t);
            Along(horizontal, inner, HalfSpan, Leaf, t);

            for (var i = _used; i < _strokes.Count; i++) _strokes[i].enabled = false;
        }

        private void Post(bool horizontal, float a, float b, float t) =>
            Across(horizontal, (a + b) * .5f, t * 1.6f, Frame, t);

        /// <summary>A stroke running ALONG the doorway from <paramref name="a"/> to <paramref name="b"/>.</summary>
        private void Along(bool horizontal, float a, float b, Color c, float width)
        {
            if (b - a <= 1e-4f) return;
            if (horizontal) Segment(new Vector2(a, 0), new Vector2(b, 0), c, width);
            else Segment(new Vector2(0, a), new Vector2(0, b), c, width);
        }

        /// <summary>A short stroke ACROSS the doorway at <paramref name="at"/> — the frame posts.</summary>
        private void Across(bool horizontal, float at, float half, Color c, float width)
        {
            if (horizontal) Segment(new Vector2(at, -half), new Vector2(at, half), c, width);
            else Segment(new Vector2(-half, at), new Vector2(half, at), c, width);
        }

        private void Segment(Vector2 a, Vector2 b, Color c, float width)
        {
            if (_used == _strokes.Count)
            {
                var go = new GameObject("Gate leaf");
                go.transform.SetParent(_parent, false);
                var created = go.AddComponent<LineRenderer>();
                created.sharedMaterial = _material;
                created.useWorldSpace = false;
                created.numCapVertices = 0;
                created.positionCount = 2;
                _strokes.Add(created);
            }
            var line = _strokes[_used++];
            line.enabled = true;
            line.positionCount = 2;
            line.widthMultiplier = width;
            line.startColor = line.endColor = c;
            line.sortingOrder = DrawOrder.MachineCue;
            line.SetPosition(0, new Vector3(a.x, a.y, -.8f));
            line.SetPosition(1, new Vector3(b.x, b.y, -.8f));
        }
    }
}
