using System.Collections.Generic;
using Relight.Sim;
using UnityEngine;

namespace Relight.Presentation
{
    /// <summary>
    /// REL-133. What a building looks like while it is being worked on, drawn on the building itself. Strokes only,
    /// built the same way <see cref="MachineActivityVisual"/> builds its badges: a pooled list of
    /// <see cref="LineRenderer"/>s parented to the thing they mark, in its local space, at
    /// <see cref="DrawOrder.MachineCue"/> — which is above <see cref="DrawOrder.Darkness"/>, so a repair is readable
    /// in the dark, which is when repairs happen.
    ///
    /// <para><b>Why this is not part of <see cref="MachineActivityVisual"/>.</b> That class draws what a machine is
    /// DOING, and <c>Supports</c> is its gate: a belt, an arm, a power source, a processor, a miner or a turret, plus
    /// anything wrecked. A Wall, a chest and a pole are outside it, and they are exactly the things this must draw
    /// on. Widening that gate would have given every damaged Wall the top-right operating badge as well, which says
    /// something about production that a wall has no part in.</para>
    ///
    /// <para><b>It does not know what it is drawing on.</b> It is given a footprint and a progress, so the Home core
    /// — which is a rect on <see cref="HomeState"/> and not a <see cref="Machine"/> at all — gets the same picture as
    /// a Gun turret from the same code.</para>
    ///
    /// <para><b>Where it sits, and why that matters.</b> Everything here is drawn INSIDE the footprint, centred. The
    /// space above the top edge is left deliberately clear: REL-134 puts a damaged building's health bar there, and
    /// a building being repaired is a damaged building, so the two are on screen together nearly always.</para>
    ///
    /// <para>The numbers are the assistant's under U-D-28 — the note asks only for "a little animation or something".
    /// Recorded as U-P-34.</para>
    /// </summary>
    internal sealed class BuildingConditionVisual
    {
        /// <summary>The worked-on colour: the same mint <see cref="MachineActivityVisual"/> uses for a machine running well.</summary>
        private static readonly Color Mint = new Color(.48f, .91f, .65f, 1);

        /// <summary>The arc itself, hotter than the mint so the point of work reads as the brightest thing on the building.</summary>
        private static readonly Color Arc = new Color(.88f, 1f, .92f, 1);

        /// <summary>Sparks, struck off the arc and cooling as they fall.</summary>
        private static readonly Color Spark = new Color(1f, .78f, .34f, 1);

        /// <summary>The unfilled part of the ring, dark enough to read the filled part against on any sprite.</summary>
        private static readonly Color Dark = new Color(.055f, .08f, .065f, 1);

        /// <summary>
        /// The ring's radius in tiles, clamped so that the mark means the same thing on a 1×1 Wall and on the 10×14
        /// Home core rather than growing with the building. The lower bound keeps it inside a one-tile footprint.
        /// </summary>
        private const float MinRadius = .24f;
        private const float MaxRadius = .75f;
        private const float RadiusOfShorterSide = .30f;

        /// <summary>Seconds one spark takes to fall and fade. Four are in the air at a time, evenly spread.</summary>
        private const float SparkLife = .55f;
        private const int Sparks = 4;

        private readonly Transform _parent;
        private readonly Material _material;
        private readonly List<LineRenderer> _strokes = new List<LineRenderer>(12);
        private int _used;
        private float _clock;

        /// <summary>True between the last <see cref="Draw"/> and the next <see cref="Hide"/>; read by the play tests.</summary>
        public bool Showing { get; private set; }

        public BuildingConditionVisual(Transform parent, Material material)
        {
            _parent = parent;
            _material = material;
        }

        /// <summary>Put every stroke away. Called the moment the repair stops, including when it is cancelled.</summary>
        public void Hide()
        {
            for (var i = 0; i < _strokes.Count; i++) _strokes[i].enabled = false;
            _used = 0;
            Showing = false;
        }

        /// <summary>
        /// One frame of "being repaired" on a <paramref name="w"/> × <paramref name="h"/> footprint whose centre the
        /// parent transform already stands on. <paramref name="progress"/> is 0 at the start of the instalment and
        /// approaches 1 as it lands.
        /// </summary>
        public void Draw(int w, int h, double progress, float dt)
        {
            _used = 0;
            _clock += dt;
            Showing = true;
            var r = Mathf.Clamp(Mathf.Min(w, h) * RadiusOfShorterSide, MinRadius, MaxRadius);
            var done = Mathf.Clamp01((float)progress);

            // The ring: the whole circle in dark, then the part that is done in mint, from the top clockwise. The
            // backing is drawn first and slightly wider, so the filled part reads on a pale sprite as well as a dark
            // one without needing to know what it is standing on.
            Ring(Vector2.zero, r, 0, 1, Dark, .085f);
            if (done > 0) Ring(Vector2.zero, r, 0, done, Mint, .055f);

            // The arc, at the leading end of the filled part: that is where a welder would be, and it means the mark
            // also says which way round the ring is filling on a building that is only briefly on screen.
            var a = Mathf.PI * .5f - done * Mathf.PI * 2;
            var at = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
            // Arc-welding flicker: two sines at rates that do not divide each other, so it never settles into a
            // visible beat. Cheap, deterministic, and it allocates nothing.
            var flicker = .45f + .55f * Mathf.Abs(Mathf.Sin(_clock * 31.7f) * Mathf.Sin(_clock * 11.3f));
            var hot = Arc;
            hot.a = flicker;
            Segment(at + new Vector2(-.09f, 0), at + new Vector2(.09f, 0), hot, .05f);
            Segment(at + new Vector2(0, -.09f), at + new Vector2(0, .09f), hot, .05f);
            Circle(at, .045f, hot, .07f);

            // Sparks struck off the arc: each on its own phase of one shared life, thrown outward and falling, and
            // fading out as it goes. Four strokes, no particle system and no allocation.
            for (var i = 0; i < Sparks; i++)
            {
                var t = Mathf.Repeat(_clock / SparkLife + i / (float)Sparks, 1);
                // A different launch angle per spark, tied to which spark it is rather than to time, so the spray
                // stays a spray instead of every spark following the one before it down the same line.
                var spread = (i - (Sparks - 1) * .5f) * .55f;
                var dir = new Vector2(Mathf.Cos(a + spread), Mathf.Sin(a + spread));
                var from = at + dir * (t * .42f) + new Vector2(0, -t * t * .55f);
                var c = Spark;
                c.a = (1 - t) * .85f;
                Segment(from, from + dir * .075f + new Vector2(0, -.03f), c, .035f);
            }

            for (var i = _used; i < _strokes.Count; i++) _strokes[i].enabled = false;
        }

        /// <summary>
        /// An arc of a circle from <paramref name="t0"/> to <paramref name="t1"/> of a full turn, measured from the
        /// top and going clockwise, which is the direction a progress ring is read in.
        /// </summary>
        private void Ring(Vector2 at, float r, float t0, float t1, Color c, float width)
        {
            // Points follow the arc's LENGTH, not its angle, so the big ring is round and the small dot at the arc's
            // centre costs five points instead of forty for a shape a twentieth the size.
            var points = Mathf.Clamp(Mathf.CeilToInt((t1 - t0) * 2 * Mathf.PI * r * 12) + 1, 2, 41);
            var line = Stroke(points, width, c);
            for (var i = 0; i < points; i++)
            {
                var t = t0 + (t1 - t0) * (points == 1 ? 0 : i / (float)(points - 1));
                var ang = Mathf.PI * .5f - t * Mathf.PI * 2;
                Point(line, i, at + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r);
            }
        }

        private LineRenderer Stroke(int points, float width, Color colour)
        {
            if (_used == _strokes.Count)
            {
                var go = new GameObject("Repair detail");
                go.transform.SetParent(_parent, false);
                var created = go.AddComponent<LineRenderer>();
                created.sharedMaterial = _material;
                created.useWorldSpace = false;
                created.numCapVertices = 2;
                _strokes.Add(created);
            }
            var stroke = _strokes[_used++];
            stroke.enabled = true;
            stroke.positionCount = points;
            stroke.widthMultiplier = width;
            stroke.startColor = stroke.endColor = colour;
            stroke.sortingOrder = DrawOrder.MachineCue;
            return stroke;
        }

        private static void Point(LineRenderer line, int i, Vector2 p) => line.SetPosition(i, new Vector3(p.x, p.y, -.8f));

        private void Segment(Vector2 a, Vector2 b, Color c, float width)
        {
            var line = Stroke(2, width, c);
            Point(line, 0, a);
            Point(line, 1, b);
        }

        private void Circle(Vector2 at, float radius, Color c, float width) => Ring(at, radius, 0, 1, c, width);
    }
}
