using Relight.Sim;
using Relight.World;
using UnityEngine;

namespace Relight.Presentation
{
    /// <summary>
    /// C-11. The darkness the player sees, drawn from the sim's own lit mask.
    ///
    /// Reference: <c>packages/sim/src/light.ts</c> owns the mask ("one texel per tile, multiplied over the world
    /// layer by the game; lit tiles are full colour, unlit ones darkened") and
    /// <c>packages/game/src/riverfrontLighting.ts</c> owns the presentation-only daylight smoothstep. Both halves are
    /// kept here in the same split: the shape of the light comes from <see cref="LightQueries.Mask(SimState)"/> and
    /// never from anything this class computes. The world is always dark now (U-D-58): <see cref="Daylight"/> is 0
    /// unless the admin "force daylight" override is on, and the strength on unlit ground is
    /// <see cref="Relight.Sim.UI.DarknessLook.Strength"/>, a picture-only rule held inside safe limits and moved by
    /// the player's brightness setting. WORLD_AND_ASSETS §2.8: the tint "must never feed gameplay" — nothing here is
    /// ever read back into the simulation.
    ///
    /// How it draws: one <see cref="SpriteRenderer"/> over the visible tile rectangle, with a
    /// <see cref="Texture2D"/> of <see cref="supersample"/> texels per tile, black (or the night tint) with a
    /// per-texel alpha. Bilinear filtering turns the per-tile mask into a soft edge, so there is no custom shader,
    /// no URP 2D light asset and no material to author — the same "presenters own no assets" idiom
    /// <see cref="TracerPresenter"/> uses. At a 30 x 20 tile view and supersample 2 the texture is about 2,500
    /// texels, so a per-frame refresh is cheap; even so the expensive half (the mask sampling) is cached and
    /// recomputed only when the lit picture, the daylight bucket or the visible rectangle actually changes
    /// (D-UI-11: "only the visible lighting surface recomposes as the pointer moves; static city lighting stays
    /// cached"). A district switching on is the one moment that rebuilds it every frame, and only for the second
    /// or two its sweep runs (REL-117, <see cref="Relight.Sim.UI.LightSweep"/>): the sim lights the district on one
    /// tick, and this draws that light arriving outward from the substation at the speed
    /// <see cref="CityPresenter"/> staggers the lamp heads, so ground and lamps come on together. Picture only —
    /// nothing read from this class, the hold included, ever reaches the simulation.
    ///
    /// The flashlight is not lighting: <see cref="MouseFlashlightPresenter"/> calls <see cref="SetBeam"/> and the
    /// beam is subtracted from the darkness here so there is still exactly one overlay on screen. It is
    /// visibility only — it changes no mask, no power and no sim query (D-UI-11).
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Relight/Lighting Presenter")]
    public sealed class LightingPresenter : MonoBehaviour
    {
        /// <summary>Tiles of margin drawn beyond the camera's rectangle, so a pan never shows an unpainted edge.</summary>
        private const int PadTiles = 3;

        [Tooltip("The host whose simulation this draws. Found in the scene if left empty.")]
        [SerializeField] private SimHost host;

        [Tooltip("Camera whose view is covered. Main camera if left empty.")]
        [SerializeField] private Camera worldCamera;

        [Tooltip("Texels per tile. 1 is the reference's own resolution; 2-3 soften the edge of a lamp's disc.")]
        [SerializeField, Range(1, 4)] private int supersample = 2;

        [Tooltip("How dark unlit ground is drawn, 0 clear to 1 opaque. U-D-58: a readable twilight, not black. " +
                 "DarknessLook holds it inside 0.35-0.70 and the player's brightness setting moves it.")]
        [SerializeField, Range(0f, 1f)] private float unlitDarkness = (float)Relight.Sim.UI.DarknessLook.DefaultUnlit;

        [Tooltip("How dark an unlit tile gets in full daylight. The reference darkens nothing by day.")]
        [SerializeField, Range(0f, 1f)] private float dayDarkness = 0f;

        [Tooltip("Colour of the darkness. Alpha is ignored; it comes from the mask.")]
        [SerializeField] private Color nightTint = new Color(0.03f, 0.04f, 0.10f, 1f);

        [Tooltip("Depth of the overlay. It must sit over the world and under the UI.")]
        [SerializeField] private float z = -6f;

        [Tooltip("Sorting order of the overlay sprite within its layer.")]
        [SerializeField] private int sortingOrder = DrawOrder.Darkness;

        /// <summary>PlayerPrefs key of the brightness setting. The settings screen writes it (Preferences.Brightness).</summary>
        public const string BrightnessKey = "relight.video.brightness";

        private float _brightness = (float)Relight.Sim.UI.DarknessLook.DefaultBrightness;

        /// <summary>The overlay strength on unlit tiles after limits and brightness. Read-only readback.</summary>
        public float UnlitStrength =>
            (float)Relight.Sim.UI.DarknessLook.Strength(unlitDarkness, _brightness);

        /// <summary>Live change from the settings screen. Picture only.</summary>
        public void SetBrightness(float value) => _brightness = Mathf.Clamp01(value);

        private SpriteRenderer _sr;
        private Texture2D _tex;
        private Sprite _sprite;
        private Color32[] _pixels;

        // The cached half: 1 where a tile is unlit, 0 where the sim says it is lit. Only the mask and the visible
        // rectangle feed it, so panning or a light switching on rebuilds it and a passing second does not.
        private float[] _shade;
        private int _tw, _th;                 // texture size in texels
        private int _rx, _ry, _rw, _rh;       // visible rectangle in tiles
        private int _builds = -1;             // LightQueries.Builds when _shade was filled
        private bool _dirty = true;

        // REL-117: the district switch-on sweep, picture only. The sim lights the whole district on one tick and
        // every rule reads that tick's mask; these hold the DRAWN light back so the ground arrives under the lamp
        // heads CityPresenter is staggering outward at the same speed. _wasLit is last build's mask (the sim's own
        // buffer is overwritten in place, so it has to be copied to be diffed) and _newLit marks the tiles this
        // sweep lit — ground that was already lit is never held, so nothing on screen flickers.
        private byte[] _wasLit, _newLit;
        private int _maskW, _maskH;
        private bool _sweeping;
        private float _sweepStart;
        private double _sweepX, _sweepY, _sweepSeconds;

        // REL-127: the small glow drawn around each placed building, picture only. The footprints inside the visible
        // rectangle are collected once and folded into _shade with the mask, because the set only moves when the
        // player builds or the view pans — not once a frame like the beam, which follows the pointer. _glowFold is
        // the same trick LightSources.Fold plays: a cheap value that changes exactly when the picture would.
        private readonly System.Collections.Generic.List<TileRect> _glows = new System.Collections.Generic.List<TileRect>();
        private int _glowFold;

        // The beam, in sim tile space, as the flashlight last set it.
        private bool _beam;
        private Vec2 _beamFrom;
        private double _beamDirX, _beamDirY, _beamRange, _beamCos, _beamGlow;

        // REL-131: the torch's own shadow, one byte a tile over the beam's bounding box, 1 where the beam gets
        // through. Rebuilt every frame, unlike _glows, because the beam follows the pointer and there is nothing
        // to cache; the cost is kept down at both ends instead — only the tiles the cone reaches are line-tested,
        // and a box holding no wall at all is answered by one scan with no line walk in it.
        private byte[] _beamClear;
        private int _beamBx, _beamBy, _beamBw, _beamBh;
        private bool _beamShadow;

        /// <summary>Texels written on the last frame. A profiling and test hook; not gameplay.</summary>
        public int Texels { get; private set; }

        /// <summary>
        /// Texels of the visible rectangle the switch-on sweep is still holding dark (REL-117). A test and
        /// profiling hook; not gameplay. 0 whenever no sweep is running.
        /// </summary>
        public int Held { get; private set; }

        /// <summary>True while a district's light is still arriving on screen. Picture only.</summary>
        public bool Sweeping => _sweeping;

        /// <summary>The daylight fraction last drawn, 0 dark to 1 full day. Read-only readback.</summary>
        public float Daylight { get; private set; } = 1f;

        /// <summary>True while the darkness overlay is being drawn at all (false under forced daylight).</summary>
        public bool Dark => _sr != null && _sr.enabled;

        /// <summary>
        /// How much a sim position is revealed by something this class draws rather than by the sim's mask, 0 to 1:
        /// the flashlight beam (D-UI-11), or a building's glow (REL-127). Picture only — no sim query moves either
        /// way. The glow is included because the alternative is worse than leaving it out: <c>EnemyPresenter</c>
        /// paints a flat silhouette over anything standing on ground the mask calls unlit, so a raider crossing the
        /// visibly lit strip beside the player's Foundry would be a black cut-out on bright ground.
        /// </summary>
        public float Reveal(Vec2 simPos) =>
            Mathf.Max(_beam ? Beam(simPos.X, simPos.Y) : 0f, Glow(simPos.X, simPos.Y));

        /// <summary>
        /// Point the visibility beam (D-UI-11). Presentation only: it subtracts darkness and touches nothing else.
        /// <paramref name="from"/> and <paramref name="dir"/> are sim tile space (Y-down);
        /// <paramref name="halfAngleRad"/> is half the cone's width, <paramref name="glowTiles"/> the small origin
        /// glow the spec asks for.
        /// </summary>
        public void SetBeam(Vec2 from, Vec2 dir, double rangeTiles, double halfAngleRad, double glowTiles)
        {
            var len = dir.Length;
            if (rangeTiles <= 0 || len < 1e-9) { _beam = false; return; }
            _beam = true;
            _beamFrom = from;
            _beamDirX = dir.X / len;
            _beamDirY = dir.Y / len;
            _beamRange = rangeTiles;
            _beamCos = System.Math.Cos(System.Math.Max(0, System.Math.Min(System.Math.PI, halfAngleRad)));
            _beamGlow = glowTiles;
        }

        /// <summary>Stop drawing the beam. Called when the pointer has no meaning (no session, no camera).</summary>
        public void ClearBeam() => _beam = false;

        private void Awake()
        {
            if (host == null) host = FindAnyObjectByType<SimHost>();
            if (worldCamera == null) worldCamera = Camera.main;

            var go = new GameObject("Darkness");
            go.transform.SetParent(transform, false);
            _sr = go.AddComponent<SpriteRenderer>();
            _sr.sortingOrder = sortingOrder;
            _sr.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
            _sr.enabled = false;

            _brightness = PlayerPrefs.GetFloat(BrightnessKey, (float)Relight.Sim.UI.DarknessLook.DefaultBrightness);
        }

        private void OnDisable()
        {
            if (_sr != null) _sr.enabled = false;
            Texels = 0;
            Held = 0;
            // A presenter that is not drawing reveals nothing: Reveal() reads _glows, so leaving the last frame's
            // buildings in it would keep lifting silhouettes off enemies after the overlay had gone.
            _glows.Clear();
            _glowFold = 0;
            // Same reason for the beam's shadow: a stale box would shadow a beam drawn from a new position.
            _beamShadow = false;
        }

        private void LateUpdate()
        {
            var sim = host == null ? null : host.Simulation;
            var cam = worldCamera != null ? worldCamera : Camera.main;
            if (sim == null || cam == null) { OnDisable(); return; }

            var ctx = sim.Context;
            var st = sim.State;

            // REL-131. Above every early return below, because Reveal() is read by the enemy presenter on frames
            // this one draws nothing at all — a stale box would shadow a beam that has since moved. It costs
            // nothing when the torch is off, which is the only case the returns below care about.
            BeamShadow(ctx, st);

            // The strength of the night. LightQueries.Daylight is the sim's clock; the smoothstep is the
            // renderer's (riverfrontLighting.ts), so dusk fades rather than steps.
            var day = (float)LightQueries.Daylight(ctx, st).Daylight;
            day = Mathf.Clamp01(day);
            Daylight = day * day * (3f - 2f * day);
            var darkness = Mathf.Lerp(UnlitStrength, dayDarkness, Daylight);

            // Nothing to draw in broad daylight: the reference darkens nothing by day, and skipping the upload is
            // the cheapest thing this class can do for the opening scene, which starts in daylight.
            if (darkness <= 0.001f && !_beam) { _sr.enabled = false; Texels = 0; Held = 0; return; }

            // The mask, read only (REL-9): the sim builds it at fixed points in the tick, and a frame that rebuilt it
            // between a command and the next tick handed the combat slot light a headless replay did not have. So a
            // light placed while paused shows on the next tick. LightQueries.Builds tells us whether it changed.
            var mask = LightQueries.Mask(st);
            var (mw, mh) = LightQueries.MaskSize(st);
            if (mask == null || mw <= 0 || mh <= 0) { _sr.enabled = false; Texels = 0; Held = 0; return; }

            if (!Rect(cam, mw, mh)) { _sr.enabled = false; Texels = 0; Held = 0; return; }
            Glows(ctx, st);
            var builds = LightQueries.Builds(st);
            if (builds != _builds) { _builds = builds; _dirty = true; Rebuilt(mask, mw, mh); }
            if (_sweeping) Advance();
            if (_dirty) Shade(mask, mw, mh);

            Paint(darkness);
            _sr.enabled = true;
        }

        /// <summary>
        /// The visible tile rectangle, clamped to the map. Returns false when nothing is on screen. A change here
        /// is what forces a rebuild, so the rectangle is quantised to whole tiles and only grows when it must.
        /// </summary>
        private bool Rect(Camera cam, int mw, int mh)
        {
            var half = cam.orthographic
                ? cam.orthographicSize
                : Mathf.Abs(cam.transform.position.z) * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            var halfW = half * cam.aspect;
            var c = cam.transform.position;

            var min = WorldSpace.Tile(new Vector3(c.x - halfW, c.y + half, 0f));
            var max = WorldSpace.Tile(new Vector3(c.x + halfW, c.y - half, 0f));

            var x0 = Mathf.Clamp(min.X - PadTiles, 0, mw - 1);
            var y0 = Mathf.Clamp(min.Y - PadTiles, 0, mh - 1);
            var x1 = Mathf.Clamp(max.X + PadTiles, 0, mw - 1);
            var y1 = Mathf.Clamp(max.Y + PadTiles, 0, mh - 1);
            var w = x1 - x0 + 1;
            var h = y1 - y0 + 1;
            if (w <= 0 || h <= 0) return false;

            if (x0 != _rx || y0 != _ry || w != _rw || h != _rh)
            {
                _rx = x0; _ry = y0; _rw = w; _rh = h;
                _dirty = true;
                Resize(w * supersample, h * supersample);
            }
            return _tex != null;
        }

        private void Resize(int tw, int th)
        {
            if (_tex != null && _tw == tw && _th == th) return;
            _tw = tw; _th = th;
            if (_tex != null) Destroy(_tex);
            if (_sprite != null) Destroy(_sprite);
            _tex = new Texture2D(tw, th, TextureFormat.RGBA32, false)
            {
                name = "Darkness",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            _pixels = new Color32[tw * th];
            _shade = new float[tw * th];
            // One texel is 1 / supersample of a tile, and one tile is one world unit, so pixels-per-unit is the
            // supersample factor and the sprite lands exactly on the tile rectangle.
            _sprite = Sprite.Create(_tex, new UnityEngine.Rect(0, 0, tw, th), new Vector2(0.5f, 0.5f), supersample,
                0, SpriteMeshType.FullRect);
            _sr.sprite = _sprite;
        }

        /// <summary>
        /// REL-117. The mask has just been stamped again: work out what this build newly lit and, if a district
        /// switched on this frame, start the sweep that reveals it. Everything else — a lamp placed, a brownout
        /// shrinking a disc — lights at once, exactly as before.
        /// </summary>
        private void Rebuilt(byte[] mask, int mw, int mh)
        {
            if (_wasLit == null || _maskW != mw || _maskH != mh)
            {
                // A new game or a load: take what is already lit as the baseline, so nothing replays on arrival.
                _maskW = mw; _maskH = mh;
                _wasLit = new byte[mw * mh];
                _newLit = new byte[mw * mh];
                _sweeping = false;
                System.Array.Copy(mask, _wasLit, _wasLit.Length);
                return;
            }

            var lit = Switched();
            if (lit != null) Begin(lit, mask);
            System.Array.Copy(mask, _wasLit, _wasLit.Length);
        }

        /// <summary>The district that switched on in this frame's events, or null. The same event the lamps read.</summary>
        private DistrictLitEvent Switched()
        {
            var events = host == null ? null : host.LastFrameEvents;
            if (events == null) return null;
            for (var i = events.Count - 1; i >= 0; i--)
                if (events[i] is DistrictLitEvent e) return e;
            return null;
        }

        /// <summary>Mark every tile this build lit that was dark before, and time the front from the substation.</summary>
        private void Begin(DistrictLitEvent e, byte[] mask)
        {
            var max = 0.0;
            var count = 0;
            for (var ty = 0; ty < _maskH; ty++)
                for (var tx = 0; tx < _maskW; tx++)
                {
                    var i = ty * _maskW + tx;
                    if (mask[i] == 0 || _wasLit[i] != 0) { _newLit[i] = 0; continue; }
                    _newLit[i] = 1;
                    count++;
                    var dx = tx + 0.5 - e.X;
                    var dy = ty + 0.5 - e.Y;
                    var d = System.Math.Sqrt(dx * dx + dy * dy);
                    if (d > max) max = d;
                }

            // REL-11's replayed event lights nothing new, so it sweeps nothing here either.
            _sweepSeconds = count == 0 ? 0 : Relight.Sim.UI.LightSweep.Seconds(max);
            _sweeping = _sweepSeconds > 0;
            _sweepX = e.X; _sweepY = e.Y; _sweepStart = Time.time;
        }

        /// <summary>A sweep is running, so the drawn mask moves every frame until the front is past the district.</summary>
        private void Advance()
        {
            if (Time.time - _sweepStart >= _sweepSeconds)
            {
                _sweeping = false;
                System.Array.Clear(_newLit, 0, _newLit.Length);
            }
            _dirty = true;   // one more pass either way, so the last held tiles are drawn at full strength
        }

        /// <summary>The cached half: sample the sim's mask once per texel. Rebuilt on a mask or rectangle change.</summary>
        private void Shade(byte[] mask, int mw, int mh)
        {
            var sweeping = _sweeping;
            var elapsed = sweeping ? Time.time - _sweepStart : 0f;
            Held = 0;

            var inv = 1f / supersample;
            for (var j = 0; j < _th; j++)
            {
                // The texture's V axis runs up; sim tiles run down, so row j of the texture is the bottom of the
                // rectangle. This is the one place the flip appears outside WorldSpace, because a Texture2D has no
                // sim position of its own.
                var ty = _ry + _rh - 1 - Mathf.FloorToInt((j + 0.5f) * inv);
                var row = j * _tw;
                var lit = ty >= 0 && ty < mh;
                // The texel's centre in continuous tile space, for the building glow: the mask is per tile, but the
                // glow is a distance and would step in whole tiles if it were sampled at tile indices.
                var wy = _ry + _rh - (j + 0.5f) * inv;
                for (var i = 0; i < _tw; i++)
                {
                    var tx = _rx + Mathf.FloorToInt((i + 0.5f) * inv);
                    var on = lit && tx >= 0 && tx < mw && mask[ty * mw + tx] != 0;
                    var a = on ? 0f : 1f;
                    // REL-117: a tile this sweep lit stays dark until the front reaches it. Picture only — the
                    // sim's mask, and so turret sight and every other rule, changed on the tick as it always did.
                    if (on && sweeping && _newLit != null && _newLit[ty * mw + tx] != 0)
                    {
                        var dx = tx + 0.5 - _sweepX;
                        var dy = ty + 0.5 - _sweepY;
                        var hold = (float)Relight.Sim.UI.LightSweep.Hold(
                            System.Math.Sqrt(dx * dx + dy * dy), elapsed);
                        if (hold > 0f) { a = hold; Held++; }
                    }
                    // REL-127: the building glow lifts the darkness here, in the cached half, because the buildings
                    // stand still. The beam is subtracted later, in Paint, because the pointer does not.
                    if (a > 0f && _glows.Count > 0) a *= 1f - Glow(_rx + (i + 0.5f) * inv, wy);
                    _shade[row + i] = a;
                }
            }
            _dirty = false;
        }

        /// <summary>
        /// REL-127. Collect the footprints of the buildings whose glow could reach the visible rectangle, and mark
        /// the shade cache dirty only when that set has actually moved — so a pan or a placement rebuilds it and a
        /// passing second does not. Read-only over <c>st.Machines</c>, exactly as the mask read above is: this class
        /// writes nothing to the simulation, and <see cref="Relight.Sim.UI.BuildingGlow"/> is never asked by it.
        /// </summary>
        private void Glows(SimContext ctx, SimState st)
        {
            _glows.Clear();
            var d = ctx?.Data;
            if (d != null && st != null)
            {
                var reach = (float)Relight.Sim.UI.BuildingGlow.ReachTiles;
                float x0 = _rx - reach, y0 = _ry - reach, x1 = _rx + _rw + reach, y1 = _ry + _rh + reach;
                for (var i = 0; i < st.Machines.Count; i++)
                {
                    var m = st.Machines[i];
                    if (!Relight.Sim.UI.BuildingGlow.Glows(d, m)) continue;
                    var r = m.Rect;
                    if (r.X + r.W < x0 || r.X > x1 || r.Y + r.H < y0 || r.Y > y1) continue;
                    _glows.Add(r);
                }
            }

            var fold = 17;
            unchecked
            {
                fold = fold * 31 + _glows.Count;
                for (var i = 0; i < _glows.Count; i++)
                {
                    var r = _glows[i];
                    fold = fold * 31 + r.X;
                    fold = fold * 31 + r.Y;
                    fold = fold * 31 + r.W;
                    fold = fold * 31 + r.H;
                }
            }
            if (fold == _glowFold) return;
            _glowFold = fold;
            _dirty = true;
        }

        /// <summary>
        /// How much a building's glow lifts the darkness at a sim position, 0 to 1 (REL-127). The strongest glow
        /// reaching the point wins rather than the sum of them, so a yard of machines has one even spill instead of a
        /// bright seam wherever two of them overlap.
        /// </summary>
        private float Glow(double wx, double wy)
        {
            var best = 0f;
            for (var i = 0; i < _glows.Count; i++)
            {
                var r = _glows[i];
                var d = Relight.Sim.UI.BuildingGlow.Distance(r.X, r.Y, r.W, r.H, wx, wy);
                if (d >= Relight.Sim.UI.BuildingGlow.ReachTiles) continue;
                var s = (float)Relight.Sim.UI.BuildingGlow.Strength(d);
                if (s > best) best = s;
            }
            return best;
        }

        /// <summary>
        /// REL-131. Work out what the torch cannot see past, for this frame's beam. The owner's note:
        /// <i>"Existing and placed walls should block light from the player torch, but not light from placed
        /// objects"</i>; asked which half to build, they chose <b>only stop the torch</b>, so placed lights keep
        /// the behaviour REL-116 gave them and this is the torch half alone.
        ///
        /// Still picture only. The shadow is drawn and nothing else: <c>LightQueries.LitAt</c> has never known the
        /// torch exists and does not learn it here, so no raider hesitates and no turret sees further because of
        /// where the player is pointing.
        ///
        /// Two costs are deliberately avoided. Tiles whose centre and four corners all fall outside the cone are
        /// never line-tested — the corners as well as the centre, because the overlay is supersampled and a texel
        /// near a tile's edge can be inside the cone when the tile's own centre is not, and a tile left untested
        /// would read as shadow and cut a bite out of the cone's rim. And a bounding box with no wall in it is
        /// answered by <see cref="LightRules.Shadow"/> in one scan, which is the open ground the player crosses
        /// for most of a run.
        /// </summary>
        private void BeamShadow(SimContext ctx, SimState st)
        {
            _beamShadow = false;
            if (!_beam || ctx == null || ctx.Geometry == null || st == null) return;

            var r = _beamRange;
            var x0 = Mathf.FloorToInt((float)(_beamFrom.X - r));
            var y0 = Mathf.FloorToInt((float)(_beamFrom.Y - r));
            var bw = Mathf.CeilToInt((float)(_beamFrom.X + r)) - x0 + 1;
            var bh = Mathf.CeilToInt((float)(_beamFrom.Y + r)) - y0 + 1;
            if (bw <= 0 || bh <= 0) return;

            var n = bw * bh;
            if (_beamClear == null || _beamClear.Length < n) _beamClear = new byte[n];
            System.Array.Clear(_beamClear, 0, n);
            _beamBx = x0;
            _beamBy = y0;
            _beamBw = bw;
            _beamBh = bh;

            // Mark the tiles the cone can touch. The feet glow is deliberately not marked here: it is never
            // occluded (standing against a wall must not put the player in the dark), and Beam() returns it
            // before it ever consults the shadow.
            var r2 = r * r;
            for (var by = 0; by < bh; by++)
                for (var bx = 0; bx < bw; bx++)
                    if (ConeTouches(x0 + bx, y0 + by, r2)) _beamClear[by * bw + bx] = 1;

            _beamShadow = LightRules.Shadow(_beamClear, x0, y0, bw, bh, ctx, st, _beamFrom.X, _beamFrom.Y);
        }

        /// <summary>Does the cone reach any part of this tile? Centre and four corners, which is enough at one tile.</summary>
        private bool ConeTouches(int tx, int ty, double r2)
        {
            for (var k = 0; k < 5; k++)
            {
                var px = tx + (k == 0 ? 0.5 : k == 1 || k == 3 ? 0.0 : 1.0);
                var py = ty + (k == 0 ? 0.5 : k <= 2 ? 0.0 : 1.0);
                var ex = px - _beamFrom.X;
                var ey = py - _beamFrom.Y;
                var d2 = ex * ex + ey * ey;
                if (d2 > r2) continue;
                if (d2 < 1e-12) return true;
                if ((ex * _beamDirX + ey * _beamDirY) / System.Math.Sqrt(d2) >= _beamCos) return true;
            }
            return false;
        }

        /// <summary>
        /// REL-131. Does the beam reach the tile this position stands on? True whenever no shadow was built — no
        /// wall inside the beam's reach, no simulation to ask — so the open-ground case costs one bool test.
        /// </summary>
        private bool BeamVisible(double wx, double wy)
        {
            if (!_beamShadow) return true;
            var bx = Mathf.FloorToInt((float)wx) - _beamBx;
            var by = Mathf.FloorToInt((float)wy) - _beamBy;
            if (bx < 0 || by < 0 || bx >= _beamBw || by >= _beamBh) return true;
            return _beamClear[by * _beamBw + bx] != 0;
        }

        /// <summary>The per-frame half: darkness strength, minus the beam, into the texture.</summary>
        private void Paint(float darkness)
        {
            byte r = (byte)Mathf.RoundToInt(Mathf.Clamp01(nightTint.r) * 255f);
            byte g = (byte)Mathf.RoundToInt(Mathf.Clamp01(nightTint.g) * 255f);
            byte b = (byte)Mathf.RoundToInt(Mathf.Clamp01(nightTint.b) * 255f);

            var inv = 1f / supersample;
            for (var j = 0; j < _th; j++)
            {
                var row = j * _tw;
                var wy = _ry + _rh - (j + 0.5f) * inv;    // sim tile Y of this texel's centre
                for (var i = 0; i < _tw; i++)
                {
                    var a = _shade[row + i];
                    if (a > 0f && _beam)
                    {
                        var wx = _rx + (i + 0.5f) * inv;
                        a *= 1f - Beam(wx, wy);
                    }
                    _pixels[row + i] = new Color32(r, g, b, (byte)Mathf.RoundToInt(Mathf.Clamp01(a * darkness) * 255f));
                }
            }
            _tex.SetPixels32(_pixels);
            _tex.Apply(false, false);
            Texels = _pixels.Length;

            var centre = WorldSpace.RectCentre(_rx, _ry, _rw, _rh);
            _sr.transform.position = new Vector3(centre.x, centre.y, z);
        }

        /// <summary>
        /// How much the beam reveals at a sim position, 0 to 1. The same shape as the sim's own cone rule
        /// (<see cref="LightRules.Covers"/>): a radius, a half-angle and a small origin glow — but with a soft
        /// falloff, because this one is a picture and not a threshold.
        /// </summary>
        private float Beam(double wx, double wy)
        {
            var ex = wx - _beamFrom.X;
            var ey = wy - _beamFrom.Y;
            var d2 = ex * ex + ey * ey;
            if (d2 > _beamRange * _beamRange) return 0f;
            var d = System.Math.Sqrt(d2);

            var glow = _beamGlow > 0 && d < _beamGlow ? (float)(1.0 - d / _beamGlow) : 0f;
            if (d < 1e-6) return Mathf.Clamp01(glow);

            var cos = (ex * _beamDirX + ey * _beamDirY) / d;
            if (cos < _beamCos) return Mathf.Clamp01(glow);

            // REL-131: a wall stops the cone here. After the glow, never before it — the small light at one's own
            // feet is not occluded, so standing with your back to a wall does not put you in the dark.
            if (!BeamVisible(wx, wy)) return Mathf.Clamp01(glow);

            // Radial falloff over the last third of the reach, angular falloff over the outer quarter of the cone,
            // so the beam has no hard rim anywhere.
            var radial = (float)(1.0 - d / _beamRange);
            radial = Mathf.Clamp01(radial / 0.34f);
            var edge = (float)((cos - _beamCos) / System.Math.Max(1e-6, (1.0 - _beamCos) * 0.25));
            return Mathf.Clamp01(Mathf.Max(glow, radial * Mathf.Clamp01(edge)));
        }
    }
}
