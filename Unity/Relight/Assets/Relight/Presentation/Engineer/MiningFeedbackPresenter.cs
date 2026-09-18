using Relight.Sim;
using Relight.World;
using UnityEngine;

namespace Relight.Presentation
{
    /// <summary>
    /// C-02. Draws the hand-mining feedback the reference draws as a tinted tile plus a progress arc
    /// (worldScene.ts:1308 <c>miningFeedback</c>, :215 for the reason line): a ring over the tile under the hands
    /// that fills as the next whole item comes out.
    ///
    /// It reads <see cref="MiningQueries.Progress"/> and nothing else — no <c>SimState</c>, no writes (TA §2.5) —
    /// and it owns no assets. The ring is a <see cref="LineRenderer"/> built in code from a unit circle, so this
    /// component drops into the scene with no prefab and no material of its own; the coordinator may later assign
    /// a nicer material and sprite (see the report's Unity steps).
    ///
    /// There is no interpolation here on purpose. The mined tile is an integer tile and the fraction is a 20 Hz
    /// value; a ring that lerps its fill would say the engineer had mined more than the sim believes.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Relight/Mining Feedback")]
    public sealed class MiningFeedbackPresenter : MonoBehaviour
    {
        [Tooltip("The host whose simulation this draws. Found in the scene if left empty.")]
        [SerializeField] private SimHost host;

        [Tooltip("Segments in the full ring. The drawn arc uses the leading fraction of them.")]
        [SerializeField, Range(8, 128)] private int segments = 48;

        [Tooltip("Ring radius as a fraction of a tile.")]
        [SerializeField, Range(0.1f, 1f)] private float radius = 0.42f;

        [Tooltip("Ring thickness in world units.")]
        [SerializeField] private float width = 0.06f;

        [Tooltip("Depth; the ring draws over the tilemap and under the engineer.")]
        [SerializeField] private float z = -1.5f;

        [Tooltip("Normal progress colour.")]
        [SerializeField] private Color colour = new Color(1f, 0.85f, 0.3f, 0.9f);

        [Tooltip("Colour when the last stop was a full Backpack (reference 'Backpack full — make room').")]
        [SerializeField] private Color fullColour = new Color(1f, 0.35f, 0.3f, 0.9f);

        private LineRenderer _ring;

        /// <summary>The tile the ring is over, and how full it is. For tests.</summary>
        public MiningProgress Progress { get; private set; }

        private void Awake()
        {
            if (host == null) host = FindAnyObjectByType<SimHost>();
            _ring = gameObject.AddComponent<LineRenderer>();
            _ring.useWorldSpace = true;
            _ring.loop = false;
            _ring.widthMultiplier = width;
            _ring.numCapVertices = 2;
            _ring.textureMode = LineTextureMode.Stretch;
            _ring.material = new Material(Shader.Find("Sprites/Default"));
            _ring.enabled = false;
        }

        private void LateUpdate()
        {
            var sim = host == null ? null : host.Simulation;
            if (sim == null || _ring == null) { if (_ring != null) _ring.enabled = false; return; }

            var p = MiningQueries.Progress(sim.Context, sim.State);
            Progress = p;
            if (!p.Active) { _ring.enabled = false; return; }

            var centre = WorldSpace.TileCentre(p.X, p.Y);
            centre.z = z;
            // The arc is the leading `fraction` of the circle, at least one segment so a just-started dig shows.
            var n = Mathf.Max(2, Mathf.CeilToInt(segments * Mathf.Clamp01((float)p.Fraction)) + 1);
            _ring.enabled = true;
            _ring.positionCount = n;
            for (var i = 0; i < n; i++)
            {
                // Clockwise from the top, which is how the reference's arc reads.
                var a = Mathf.PI * 0.5f - (Mathf.PI * 2f * i / segments);
                _ring.SetPosition(i, centre + new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f));
            }
            var c = p.Full ? fullColour : colour;
            _ring.startColor = c;
            _ring.endColor = c;
        }
    }
}
