using System.Collections.Generic;
using Relight.Sim;
using Relight.World;
using UnityEngine;

namespace Relight.Presentation
{
    /// <summary>
    /// Batch 4, FRT-10 (REL-145): the Freight chapter's circles on the world (FREIGHT_STRONGHOLD_DESIGN §8). A
    /// search circle is a dim amber ring over the rounded ground a camp is somewhere inside; a hold ring is a bright
    /// ring at the camp's marker, with an arc that fills as the hold clock runs.
    ///
    /// It reads <see cref="FreightObjectives.Circles"/> and writes nothing (TA §2.5); which circles show, and their
    /// clocks, are the sim's. The rings are code-built <see cref="LineRenderer"/>s and sort ABOVE the darkness
    /// (<see cref="DrawOrder.FreightCircle"/>): the camps stand in the dark and the player has to find them there.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Relight/Freight Circles")]
    public sealed class FreightCirclePresenter : MonoBehaviour
    {
        const int Segments = 64;

        [Tooltip("The host whose simulation this draws. Found in the scene if left empty.")]
        [SerializeField] private SimHost host;

        [Tooltip("Depth; with the other world feedback, in front of the tilemap.")]
        [SerializeField] private float z = -1.5f;

        [SerializeField] private float searchWidth = 0.18f;
        [SerializeField] private float holdWidth = 0.14f;
        [SerializeField] private float fillWidth = 0.32f;
        [SerializeField] private Color searchColour = new Color(1f, 0.78f, 0.25f, 0.35f);
        [SerializeField] private Color holdColour = new Color(1f, 0.78f, 0.25f, 0.9f);
        [SerializeField] private Color fillColour = new Color(0.55f, 0.95f, 0.45f, 0.95f);

        private readonly List<FreightCircle> _circles = new List<FreightCircle>();
        private readonly List<LineRenderer> _rings = new List<LineRenderer>();
        private readonly List<LineRenderer> _fills = new List<LineRenderer>();
        private Material _material;

        /// <summary>The circles drawn on the last frame. For tests.</summary>
        public IReadOnlyList<FreightCircle> Drawn => _circles;

        private void Awake()
        {
            if (host == null) host = FindAnyObjectByType<SimHost>();
            _material = new Material(Shader.Find("Sprites/Default"));
        }

        private void LateUpdate()
        {
            _circles.Clear();
            var sim = host == null ? null : host.Simulation;
            if (sim != null) FreightObjectives.Circles(sim.Context, sim.State, _circles);

            var fills = 0;
            for (var i = 0; i < _circles.Count; i++)
            {
                var c = _circles[i];
                var ring = Slot(_rings, i, "Freight Circle", c.Hold ? DrawOrder.FreightHold : DrawOrder.FreightCircle);
                ring.loop = true;
                ring.widthMultiplier = c.Hold ? holdWidth : searchWidth;
                Arc(ring, c.Centre, c.Radius, 1.0, c.Hold ? holdColour : searchColour);
                if (c.Hold && c.Fraction > 0)
                {
                    var fill = Slot(_fills, fills++, "Freight Hold", DrawOrder.FreightHold + 1);
                    fill.loop = false;
                    fill.widthMultiplier = fillWidth;
                    Arc(fill, c.Centre, c.Radius, c.Fraction, fillColour);
                }
            }
            for (var i = _circles.Count; i < _rings.Count; i++) _rings[i].enabled = false;
            for (var i = fills; i < _fills.Count; i++) _fills[i].enabled = false;
        }

        /// <summary>A share of the circle, clockwise from north on screen.</summary>
        private void Arc(LineRenderer lr, Vec2 centre, double radius, double share, Color tint)
        {
            var n = Mathf.Max(2, Mathf.CeilToInt(Segments * (float)share) + (lr.loop ? 0 : 1));
            lr.positionCount = n;
            var steps = lr.loop ? n : n - 1;
            for (var k = 0; k < n; k++)
            {
                // Tile space has +Y south, so a growing angle from -90° runs clockwise on screen.
                var a = -Mathf.PI / 2f + 2f * Mathf.PI * (float)share * k / steps;
                var p = new Vec2(centre.X + radius * Mathf.Cos(a), centre.Y + radius * Mathf.Sin(a));
                lr.SetPosition(k, WorldSpace.World(p, z));
            }
            lr.startColor = tint;
            lr.endColor = tint;
            lr.enabled = true;
        }

        private LineRenderer Slot(List<LineRenderer> pool, int i, string label, int order)
        {
            while (pool.Count <= i)
            {
                var go = new GameObject(label);
                go.transform.SetParent(transform, false);
                var lr = go.AddComponent<LineRenderer>();
                lr.useWorldSpace = true;
                lr.material = _material;
                lr.numCapVertices = 2;
                lr.enabled = false;
                pool.Add(lr);
            }
            pool[i].sortingOrder = order;
            return pool[i];
        }

        private void OnDestroy()
        {
            if (_material != null) Destroy(_material);
        }
    }
}
