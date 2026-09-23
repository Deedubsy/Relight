using System.Collections.Generic;
using Relight.Sim.UI;
using UnityEngine;

namespace Relight.Presentation
{
    /// <summary>
    /// REL-134. A small health bar over a damaged building. Strokes only, pooled and parented the same way
    /// <see cref="BuildingConditionVisual"/> and <see cref="MachineActivityVisual"/> pool theirs, at
    /// <see cref="DrawOrder.MachineCue"/> — above <see cref="DrawOrder.Darkness"/>, because a raid is the only
    /// reason a building is damaged and a raid happens in the dark.
    ///
    /// <para><b>Where it sits.</b> Above the top edge of the footprint, in the gap REL-133 deliberately left clear
    /// (<c>BuildingConditionVisual</c> draws everything INSIDE the footprint). A building being repaired is always a
    /// damaged building, so the repair ring and this bar are on screen together nearly every time; they cannot
    /// collide because one is inside the rectangle and the other is outside it, not because two offsets were tuned
    /// until they happened to miss.</para>
    ///
    /// <para><b>Why the thickness follows the camera.</b> REL-130 opened the view by half again, so the same world
    /// measurement is two thirds as many pixels at the far notch as at the near one. The bar's THICKNESS is what
    /// decides whether it reads, so it is written as a fraction of what the camera is showing and comes out the
    /// same size on screen at either end of the wheel's range. Its WIDTH stays in world units and tracks the
    /// footprint, because that is what keeps the bar visibly attached to the building it belongs to rather than
    /// floating at a fixed size over everything.</para>
    ///
    /// <para><b>It does not know what it is drawing on</b>, exactly like <see cref="BuildingConditionVisual"/>: it is
    /// given a footprint and a fraction, so the Home core gets the same bar as a Wall from the same code.</para>
    ///
    /// <para>The numbers are the assistant's under U-D-28; the owner's note asks only for "a small health bar".
    /// Recorded as U-P-35.</para>
    /// </summary>
    internal sealed class BuildingHealthVisual
    {
        /// <summary>Above <see cref="BuildingCondition.HurtBelow"/>: the mint the interface already calls "healthy".</summary>
        private static readonly Color Whole = new Color(.57f, .85f, .71f, 1);

        /// <summary>Hurt but well clear of down: the interface's accent amber.</summary>
        private static readonly Color Hurt = new Color(.89f, .73f, .44f, 1);

        /// <summary>Below <see cref="BuildingCondition.CriticalBelow"/>, or down: the interface's danger red.</summary>
        private static readonly Color Critical = new Color(1f, .54f, .50f, 1);

        /// <summary>The track. Nearly opaque, so the fill reads against a pale sprite as well as a dark one.</summary>
        private static readonly Color Track = new Color(.05f, .07f, .06f, .92f);

        /// <summary>
        /// The bar's width in tiles: the footprint's, clamped. The floor makes a 1×1 Wall's bar a little wider than
        /// the wall, which is what makes it findable in a line of them; the ceiling stops the 10×14 Home core
        /// wearing a bar ten tiles across.
        /// </summary>
        private const float MinWidth = .9f;
        private const float MaxWidth = 2.4f;

        /// <summary>
        /// The bar's thickness as a fraction of the camera's half-height. At the opening framing (20 tiles high,
        /// half-height 10) that is 0.16 tiles; at REL-130's far notch (30 tiles) it is 0.24 tiles — the same number
        /// of pixels either way. The clamps cover a camera that is not the game's.
        /// </summary>
        private const float ThicknessOfHalfView = .016f;
        private const float MinThickness = .10f;
        private const float MaxThickness = .34f;

        /// <summary>Clear air between the top edge of the footprint and the underside of the track.</summary>
        private const float Gap = .14f;

        /// <summary>How much of the track's thickness is border rather than fill, per side.</summary>
        private const float InsetOfThickness = .22f;

        private readonly Transform _parent;
        private readonly Material _material;
        private readonly List<LineRenderer> _strokes = new List<LineRenderer>(2);
        private int _used;

        /// <summary>True between the last <see cref="Draw"/> and the next <see cref="Hide"/>; read by the play tests.</summary>
        public bool Showing { get; private set; }

        /// <summary>The thickness the last <see cref="Draw"/> used, in world units. For the zoom test.</summary>
        public float Thickness { get; private set; }

        public BuildingHealthVisual(Transform parent, Material material)
        {
            _parent = parent;
            _material = material;
        }

        /// <summary>Put the bar away. Called the moment the building is whole again, and when it is destroyed.</summary>
        public void Hide()
        {
            for (var i = 0; i < _strokes.Count; i++) _strokes[i].enabled = false;
            _used = 0;
            Showing = false;
        }

        /// <summary>
        /// One frame of the bar over a <paramref name="w"/> × <paramref name="h"/> footprint whose centre the parent
        /// transform already stands on. <paramref name="fraction"/> is 0 to 1, and <paramref name="halfView"/> is the
        /// camera's orthographic size — half the height of what the player can see, in world units.
        /// </summary>
        public void Draw(int w, int h, double fraction, float halfView)
        {
            _used = 0;
            Showing = true;
            var t = Thickness = Mathf.Clamp(halfView * ThicknessOfHalfView, MinThickness, MaxThickness);
            var width = Mathf.Clamp(w, MinWidth, MaxWidth);
            var y = h * .5f + Gap + t * .5f;
            var left = -width * .5f;
            var right = width * .5f;
            var f = Mathf.Clamp01((float)fraction);

            Segment(new Vector2(left, y), new Vector2(right, y), Track, t);

            var inset = t * InsetOfThickness;
            var innerLeft = left + inset;
            var innerRight = right - inset;
            var fill = f <= 0 ? Critical : f < BuildingCondition.CriticalBelow ? Critical
                     : f < BuildingCondition.HurtBelow ? Hurt : Whole;
            // A wreck still shows its empty track — that IS the reading, and it is the thing the player is looking
            // for when deciding what to repair first. Anything still standing keeps at least a stub of fill, so a
            // building on its last point of health is never mistaken for one that is already down.
            if (f > 0)
            {
                var end = Mathf.Max(innerLeft + (innerRight - innerLeft) * f, innerLeft + (t - inset * 2) * .5f);
                Segment(new Vector2(innerLeft, y), new Vector2(Mathf.Min(end, innerRight), y), fill, t - inset * 2);
            }

            for (var i = _used; i < _strokes.Count; i++) _strokes[i].enabled = false;
        }

        private void Segment(Vector2 a, Vector2 b, Color c, float width)
        {
            if (_used == _strokes.Count)
            {
                var go = new GameObject("Health bar detail");
                go.transform.SetParent(_parent, false);
                var created = go.AddComponent<LineRenderer>();
                created.sharedMaterial = _material;
                created.useWorldSpace = false;
                // Square ends, unlike the repair ring's rounded ones: a rounded cap would make the fill overhang
                // its own track at full health, and a bar that is longer than its track reads as a bug.
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
