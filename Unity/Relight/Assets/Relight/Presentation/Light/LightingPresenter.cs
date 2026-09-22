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
    /// cached").
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

        // The beam, in sim tile space, as the flashlight last set it.
        private bool _beam;
        private Vec2 _beamFrom;
        private double _beamDirX, _beamDirY, _beamRange, _beamCos, _beamGlow;

        /// <summary>Texels written on the last frame. A profiling and test hook; not gameplay.</summary>
        public int Texels { get; private set; }

        /// <summary>The daylight fraction last drawn, 0 dark to 1 full day. Read-only readback.</summary>
        public float Daylight { get; private set; } = 1f;

        /// <summary>True while the darkness overlay is being drawn at all (false under forced daylight).</summary>
        public bool Dark => _sr != null && _sr.enabled;

        /// <summary>How much the flashlight reveals a sim position, 0 to 1. Picture only (D-UI-11).</summary>
        public float Reveal(Vec2 simPos) => _beam ? Beam(simPos.X, simPos.Y) : 0f;

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
        }

        private void LateUpdate()
        {
            var sim = host == null ? null : host.Simulation;
            var cam = worldCamera != null ? worldCamera : Camera.main;
            if (sim == null || cam == null) { OnDisable(); return; }

            var ctx = sim.Context;
            var st = sim.State;

            // The strength of the night. LightQueries.Daylight is the sim's clock; the smoothstep is the
            // renderer's (riverfrontLighting.ts), so dusk fades rather than steps.
            var day = (float)LightQueries.Daylight(ctx, st).Daylight;
            day = Mathf.Clamp01(day);
            Daylight = day * day * (3f - 2f * day);
            var darkness = Mathf.Lerp(UnlitStrength, dayDarkness, Daylight);

            // Nothing to draw in broad daylight: the reference darkens nothing by day, and skipping the upload is
            // the cheapest thing this class can do for the opening scene, which starts in daylight.
            if (darkness <= 0.001f && !_beam) { _sr.enabled = false; Texels = 0; return; }

            // The mask, read only (REL-9): the sim builds it at fixed points in the tick, and a frame that rebuilt it
            // between a command and the next tick handed the combat slot light a headless replay did not have. So a
            // light placed while paused shows on the next tick. LightQueries.Builds tells us whether it changed.
            var mask = LightQueries.Mask(st);
            var (mw, mh) = LightQueries.MaskSize(st);
            if (mask == null || mw <= 0 || mh <= 0) { _sr.enabled = false; Texels = 0; return; }

            if (!Rect(cam, mw, mh)) { _sr.enabled = false; Texels = 0; return; }
            var builds = LightQueries.Builds(st);
            if (builds != _builds) { _builds = builds; _dirty = true; }
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

        /// <summary>The cached half: sample the sim's mask once per texel. Rebuilt on a mask or rectangle change.</summary>
        private void Shade(byte[] mask, int mw, int mh)
        {
            var inv = 1f / supersample;
            for (var j = 0; j < _th; j++)
            {
                // The texture's V axis runs up; sim tiles run down, so row j of the texture is the bottom of the
                // rectangle. This is the one place the flip appears outside WorldSpace, because a Texture2D has no
                // sim position of its own.
                var ty = _ry + _rh - 1 - Mathf.FloorToInt((j + 0.5f) * inv);
                var row = j * _tw;
                var lit = ty >= 0 && ty < mh;
                for (var i = 0; i < _tw; i++)
                {
                    var tx = _rx + Mathf.FloorToInt((i + 0.5f) * inv);
                    _shade[row + i] = lit && tx >= 0 && tx < mw && mask[ty * mw + tx] != 0 ? 0f : 1f;
                }
            }
            _dirty = false;
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

            // Radial falloff over the last third of the reach, angular falloff over the outer quarter of the cone,
            // so the beam has no hard rim anywhere.
            var radial = (float)(1.0 - d / _beamRange);
            radial = Mathf.Clamp01(radial / 0.34f);
            var edge = (float)((cos - _beamCos) / System.Math.Max(1e-6, (1.0 - _beamCos) * 0.25));
            return Mathf.Clamp01(Mathf.Max(glow, radial * Mathf.Clamp01(edge)));
        }
    }
}
