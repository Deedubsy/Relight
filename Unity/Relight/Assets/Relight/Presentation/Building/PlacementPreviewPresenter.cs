using System.Collections.Generic;
using Relight.Sim;
using Relight.World;
using UnityEngine;

namespace Relight.Presentation
{
    /// <summary>
    /// C-11. Everything the player must be able to see about a ghost before committing to it (UI_AND_ONBOARDING §6):
    /// whether it can go there, what it will be cabled to, which way items enter and leave it, and how far a turret
    /// would shoot.
    ///
    /// Every fact on screen is a sim query, never a rule re-derived here:
    /// <list type="bullet">
    /// <item>validity and its refusal text — <see cref="Placement.Validity"/> (W-C's);</item>
    /// <item>power cables — <see cref="PowerLinks.At"/>, which is <c>PowerGrid.NodesLinked</c> itself
    /// (campaignPower.ts:16), so the preview and the network can never disagree;</item>
    /// <item>turret reach — <see cref="TurretSight.Coverage"/>, which sweeps <see cref="Sightline.Clear"/>, the
    /// very call the gun makes at the trigger, so the shape drawn here is the shape the turret ends up with;</item>
    /// <item>footprint — <see cref="Footprints.Dimensions"/>, so the splitter's 2x1/1x2 flip is the sim's.</item>
    /// <item>what a light would light, and which ground inside a turret's ring is lit — <see cref="LightPreview"/>,
    /// which is the mask's own stamp and the mask itself (L-02, ALWAYS_DARK_SPEC §4 and §5.7).</item>
    /// </list>
    ///
    /// It owns no assets: every line is a <see cref="LineRenderer"/> built in code over a
    /// <c>Shader.Find("Sprites/Default")</c> material and pooled, the idiom <see cref="TracerPresenter"/> established.
    /// It is read-only — it never touches <c>SimState</c> and submits no command; the build tool (C-10) drives it
    /// through <see cref="Show"/> and <see cref="Hide"/>, which is the only API it has.
    ///
    /// A single chevron shows travel direction. Extractors also mark their direct output port.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Relight/Placement Preview Presenter")]
    public sealed class PlacementPreviewPresenter : MonoBehaviour
    {
        /// <summary>Segments in the turret range ring. Enough that a 12-tile circle reads as a circle.</summary>
        private const int RingSegments = 64;

        [Tooltip("The host whose simulation the ghost is tested against. Found in the scene if left empty.")]
        [SerializeField] private SimHost host;

        [Tooltip("Line thickness in world units.")]
        [SerializeField] private float width = 0.08f;

        [Tooltip("Depth; the preview draws over the world and under the shots.")]
        [SerializeField] private float z = -4f;

        [Tooltip("Footprint outline when the placement would succeed.")]
        [SerializeField] private Color okColour = new Color(0.45f, 1f, 0.55f, 0.95f);

        [Tooltip("Footprint outline when the sim would refuse the placement.")]
        [SerializeField] private Color badColour = new Color(1f, 0.35f, 0.3f, 0.95f);

        [Tooltip("Power link cables (UI_AND_ONBOARDING: purple).")]
        [SerializeField] private Color linkColour = new Color(0.72f, 0.45f, 1f, 0.85f);

        [Tooltip("Conveyor output side.")]
        [SerializeField] private Color outColour = new Color(1f, 0.76f, 0.3f, 0.95f);

        [Tooltip("Turret range ring: the nominal circle, drawn faint.")]
        [SerializeField] private Color rangeColour = new Color(1f, 0.55f, 0.45f, 0.28f);

        [Tooltip("The part of that range the turret can actually see and shoot into.")]
        [SerializeField] private Color coverColour = new Color(1f, 0.62f, 0.4f, 0.9f);

        [Tooltip("The same coverage shape when the position is blind enough to warn about.")]
        [SerializeField] private Color blindColour = new Color(1f, 0.35f, 0.3f, 0.95f);

        [Tooltip("L-02: the inner ring, how far the turret reaches an alien standing in the dark.")]
        [SerializeField] private Color darkSightColour = new Color(0.55f, 0.7f, 1f, 0.75f);

        [Tooltip("L-02: lit ground inside the outer ring, where the turret reaches its full range.")]
        [SerializeField] private Color litTintColour = new Color(1f, 0.86f, 0.45f, 0.22f);

        [Tooltip("L-02: the outline of the tiles a Lamp, Arc lamp or Floodlight ghost would light.")]
        [SerializeField] private Color lightCoverColour = new Color(1f, 0.9f, 0.55f, 0.9f);

        private readonly List<LineRenderer> _pool = new List<LineRenderer>();
        private readonly List<PowerLink> _links = new List<PowerLink>();
        private Material _material;

        private TurretCoverage _cover;
        private int _coverRev = int.MinValue, _coverX = int.MinValue, _coverY = int.MinValue;
        private string _coverKind = "";

        private readonly List<TileEdge> _lightEdges = new List<TileEdge>();
        private int _lightRev = int.MinValue, _lightX = int.MinValue, _lightY = int.MinValue;
        private string _lightKind = "";
        private Dir _lightDir = Dir.N;

        private readonly List<TileRun> _tintRuns = new List<TileRun>();
        private int _tintBuilds = int.MinValue, _tintX = int.MinValue, _tintY = int.MinValue;
        private string _tintKind = "";

        private bool _shown, _pair;
        private int _fromX, _fromY;
        private string _kind = "";
        private int _x, _y;
        private Dir _dir = Dir.N;

        /// <summary>True while a ghost is being previewed.</summary>
        public bool Visible => _shown;

        /// <summary>True when the sim would accept this placement. False while nothing is shown.</summary>
        public bool Ok { get; private set; }

        /// <summary>
        /// The sim's own refusal text for the ghost, "" when it would be accepted or when nothing is shown.
        /// The build tool shows this; it is <see cref="Placement.Validity"/>'s wording and never a second vocabulary.
        /// </summary>
        public string Problem { get; private set; } = "";

        /// <summary>
        /// A warning about the previewed turret's field of fire — GP-W3's "warn clearly about a blind position" —
        /// or "" when there is nothing worth saying, when the ghost is not a turret, or when nothing is shown.
        /// It is advice, never a refusal: a walled-in turret is a legal build and sometimes a deliberate one, so
        /// <see cref="Ok"/> is untouched by it.
        /// </summary>
        public string Advice { get; private set; } = "";

        /// <summary>Lines drawn on the last frame. For tests and profiling.</summary>
        public int Drawn { get; private set; }

        /// <summary>Preview a ghost of <paramref name="kind"/> with its north-west corner on tile (x, y).</summary>
        public void Show(string kind, int x, int y, Dir dir)
        {
            if (string.IsNullOrEmpty(kind)) { Hide(); return; }
            _pair = false;
            _shown = true;
            _kind = kind;
            _x = x;
            _y = y;
            _dir = dir;
        }

        public void ShowPair(int fromX, int fromY, int toX, int toY, Dir dir)
        {
            Show(FlowBuild.Kind, toX, toY, dir);
            _pair = true;
            _fromX = fromX;
            _fromY = fromY;
        }

        /// <summary>Stop previewing. Every line goes away on the next frame, and immediately if this is disabled.</summary>
        public void Hide()
        {
            _shown = false;
            _kind = "";
            Ok = false;
            Problem = "";
            Advice = "";
        }

        private void Awake()
        {
            if (host == null) host = FindAnyObjectByType<SimHost>();
            _material = new Material(Shader.Find("Sprites/Default"));
        }

        private void OnDisable()
        {
            for (var i = 0; i < _pool.Count; i++) if (_pool[i] != null) _pool[i].enabled = false;
            Drawn = 0;
        }

        /// <summary>
        /// A pooled line. <paramref name="thickness"/> is in world units and defaults to the inspector's width; a
        /// one-tile-thick line with square ends is how a run of tiles is tinted without owning a mesh or a sprite.
        /// </summary>
        private LineRenderer Line(int i, int points, Color c, float thickness = -1f)
        {
            while (_pool.Count <= i)
            {
                var go = new GameObject("Preview " + _pool.Count);
                go.transform.SetParent(transform, false);
                var created = go.AddComponent<LineRenderer>();
                created.useWorldSpace = true;
                created.sortingOrder = DrawOrder.PlacementPreview;
                created.widthMultiplier = width;
                created.numCapVertices = 1;
                created.textureMode = LineTextureMode.Stretch;
                created.material = _material;
                _pool.Add(created);
            }
            var lr = _pool[i];
            lr.enabled = true;
            lr.widthMultiplier = thickness > 0 ? thickness : width;
            lr.numCapVertices = thickness > 0 ? 0 : 1;      // a rounded cap would spill half a tile past the run
            lr.positionCount = points;
            lr.startColor = c;
            lr.endColor = c;
            return lr;
        }

        private void LateUpdate()
        {
            var sim = host == null ? null : host.Simulation;
            if (!_shown || sim == null) { OnDisable(); return; }

            var ctx = sim.Context;
            var st = sim.State;
            var d = ctx.Data;
            if (!d.TryMachine(_kind, out var spec)) { OnDisable(); return; }

            var (w, h) = Footprints.Dimensions(_kind, _dir, spec.Size);
            var (ok, reason) = Placement.Validity(ctx, st, _kind, _x, _y, _dir);
            if (_pair) (ok, reason) = FlowBuild.PairBuildable(ctx, st, _fromX, _fromY, _x, _y, _dir);
            Ok = ok;
            Problem = ok ? "" : reason;

            var n = 0;
            n = Outline(n, w, h, ok ? okColour : badColour);
            if (_pair)
            {
                var line = Line(n++, 2, ok ? okColour : badColour);
                line.SetPosition(0, P(_fromX + 0.5, _fromY + 0.5));
                line.SetPosition(1, P(_x + 0.5, _y + 0.5));
                var endX = _x; var endY = _y;
                _x = _fromX; _y = _fromY;
                n = Outline(n, w, h, ok ? okColour : badColour);
                _x = endX; _y = endY;
            }
            Advice = Cover(ctx, st).Advice;
            // A turret warning always outranks a belt one — they can never both apply to the same kind anyway.
            // For a dragged run the belt that needs a source is the one at the START of the drag, not the cursor.
            if (Advice.Length == 0)
                Advice = _pair
                    ? BeltSight.Advise(ctx, st, _kind, _fromX, _fromY, _dir)
                    : BeltSight.Advise(ctx, st, _kind, _x, _y, _dir);

            n = Links(n, ctx, st, spec.Size);
            n = Conveyor(n, w, h);
            n = Range(n, st, d, w, h);
            n = LightCover(n, ctx, st);

            for (var i = n; i < _pool.Count; i++) if (_pool[i] != null) _pool[i].enabled = false;
            Drawn = n;
        }

        private int Outline(int n, int w, int h, Color c)
        {
            var lr = Line(n++, 5, c);
            lr.SetPosition(0, P(_x, _y));
            lr.SetPosition(1, P(_x + w, _y));
            lr.SetPosition(2, P(_x + w, _y + h));
            lr.SetPosition(3, P(_x, _y + h));
            lr.SetPosition(4, P(_x, _y));
            return n;
        }

        private int Links(int n, SimContext ctx, SimState st, int size)
        {
            PowerLinks.At(ctx, st, _kind, _x, _y, _dir, size, _links);
            for (var i = 0; i < _links.Count; i++)
            {
                var l = _links[i];
                var lr = Line(n++, 2, linkColour);
                lr.SetPosition(0, P(l.FromX, l.FromY));
                lr.SetPosition(1, P(l.ToX, l.ToY));
            }
            return n;
        }

        private int Conveyor(int n, int w, int h)
        {
            if (!FlowRules.IsConveyor(_kind) && !FlowRules.IsInserter(_kind) && _kind != "excavator" && _kind != "pumpjack") return n;
            var cx = _x + w / 2.0;
            var cy = _y + h / 2.0;
            if (_kind == "excavator" || _kind == "pumpjack") { cx += Dirs.DX[(int)_dir] * (w / 2.0 - .38); cy += Dirs.DY[(int)_dir] * (h / 2.0 - .38); }
            n = Arrow(n, cx, cy, _dir, outColour);                       // OUT: along the facing
            // Only the direction of travel is shown.
            return n;
        }

        /// <summary>A chevron half a tile out from the centre, pointing the way items travel on that side.</summary>
        private int Arrow(int n, double cx, double cy, Dir dir, Color c)
        {
            double dx = Dirs.DX[(int)dir], dy = Dirs.DY[(int)dir];
            double px = -dy, py = dx;                                    // the perpendicular, Y-down
            var tipX = cx + dx * 0.62;
            var tipY = cy + dy * 0.62;
            var backX = tipX - dx * 0.32;
            var backY = tipY - dy * 0.32;
            var lr = Line(n++, 3, c);
            lr.SetPosition(0, P(backX + px * 0.26, backY + py * 0.26));
            lr.SetPosition(1, P(tipX, tipY));
            lr.SetPosition(2, P(backX - px * 0.26, backY - py * 0.26));
            return n;
        }

        /// <summary>
        /// The sweep, recomputed only when the ghost or the world moved. <see cref="SimState.Rev"/> changes on every
        /// placement and removal, which is exactly when a wall could have appeared in the turret's way; a cursor
        /// resting on one tile costs nothing after the first frame.
        /// </summary>
        private TurretCoverage Cover(SimContext ctx, SimState st)
        {
            if (_coverRev == st.Rev && _coverX == _x && _coverY == _y && _coverKind == _kind) return _cover;
            _coverRev = st.Rev;
            _coverX = _x;
            _coverY = _y;
            _coverKind = _kind;
            _cover = TurretSight.Coverage(ctx, st, _kind, _x, _y);
            return _cover;
        }

        /// <summary>
        /// The nominal range as a faint circle and, inside it, the ground the turret can actually reach. The two
        /// together are the point: the gap between them is what the player is being shown.
        /// </summary>
        private int Range(int n, SimState st, GameData d, int w, int h)
        {
            if (!d.TryTurret(_kind, out var t) || t.RangeTiles <= 0) return n;
            var cx = _x + w / 2.0;
            var cy = _y + h / 2.0;

            // L-02 (§5.7): lit ground inside the outer ring first, so the rings and the coverage draw over it.
            var dark = TurretRules.DarkSight(t);
            if (dark < t.RangeTiles)
            {
                var builds = LightQueries.Builds(st);
                if (_tintBuilds != builds || _tintX != _x || _tintY != _y || _tintKind != _kind)
                {
                    _tintBuilds = builds; _tintX = _x; _tintY = _y; _tintKind = _kind;
                    var lit = LightPreview.LitWithin(st, cx, cy, t.RangeTiles);
                    LightPreview.Runs(in lit, _tintRuns);
                }
                for (var i = 0; i < _tintRuns.Count; i++)
                {
                    var run = _tintRuns[i];
                    var fill = Line(n++, 2, litTintColour, WorldSpace.UnitsPerTile);
                    fill.SetPosition(0, P(run.X0, run.Y + 0.5));
                    fill.SetPosition(1, P(run.X1 + 1, run.Y + 0.5));
                }
            }

            n = Ring(n, cx, cy, t.RangeTiles, rangeColour);
            if (dark < t.RangeTiles) n = Ring(n, cx, cy, dark, darkSightColour);

            var cover = _cover;
            if (!cover.Known || cover.Reach == null || cover.Reach.Length == 0) return n;
            var rays = cover.Reach.Length;
            var poly = Line(n++, rays + 1, cover.OpenFraction < TurretSight.BlindFraction ? blindColour : coverColour);
            for (var i = 0; i <= rays; i++)
            {
                var k = i == rays ? 0 : i;
                var a = k * (2.0 * System.Math.PI / rays);
                var r = cover.Reach[k];
                poly.SetPosition(i, P(cx + System.Math.Cos(a) * r, cy + System.Math.Sin(a) * r));
            }
            return n;
        }

        private int Ring(int n, double cx, double cy, double r, Color c)
        {
            var lr = Line(n++, RingSegments + 1, c);
            for (var i = 0; i <= RingSegments; i++)
            {
                var a = i * (2.0 * System.Math.PI / RingSegments);
                lr.SetPosition(i, P(cx + System.Math.Cos(a) * r, cy + System.Math.Sin(a) * r));
            }
            return n;
        }

        /// <summary>
        /// L-02 (§4): the outline of the tiles a light ghost would light, blocking applied, so the player sees the
        /// coverage and the gaps before paying. Recomputed only when the ghost or the world moved.
        /// </summary>
        private int LightCover(int n, SimContext ctx, SimState st)
        {
            if (_lightRev != st.Rev || _lightX != _x || _lightY != _y || _lightKind != _kind || _lightDir != _dir)
            {
                _lightRev = st.Rev; _lightX = _x; _lightY = _y; _lightKind = _kind; _lightDir = _dir;
                if (LightPreview.ForGhost(ctx, st, _kind, _x, _y, _dir, out var lit)) LightPreview.Outline(in lit, _lightEdges);
                else _lightEdges.Clear();
            }
            for (var i = 0; i < _lightEdges.Count; i++)
            {
                var e = _lightEdges[i];
                var lr = Line(n++, 2, lightCoverColour);
                lr.SetPosition(0, P(e.X0, e.Y0));
                lr.SetPosition(1, P(e.X1, e.Y1));
            }
            return n;
        }

        /// <summary>A sim tile-space point at the preview's depth. The Y flip lives in WorldSpace, not here.</summary>
        private Vector3 P(double tx, double ty) => WorldSpace.World(new Vec2(tx, ty), z);
    }
}
