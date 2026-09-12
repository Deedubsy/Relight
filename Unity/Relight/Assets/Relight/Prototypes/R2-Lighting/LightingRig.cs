using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Relight.Prototypes
{
    /// <summary>
    /// B-14 / R2 — the lighting risk prototype, and the code C-11 grows.
    ///
    /// The reference's worst recorded frames are a RENDERER problem, not a simulation one: 116.8 / 150.1 / 266.8 /
    /// 300.2 ms with changed light-mask repaints averaging ~24 ms (TECHNICAL_ARCHITECTURE.md §3.3, quoting
    /// docs/P5_05_INTEGRATION_REPORT.md:76). This rig reproduces that density in URP's 2D renderer so the port's
    /// lighting budget is decided on a measurement instead of an assumption.
    ///
    /// What it builds (authored by Relight/Prototypes/Build R2, re-runnable):
    ///   · one global Light2D at night intensity, and any other global light in the scene switched off,
    ///   · 200 point Light2Ds spread over the whole 864 x 576 map at the reference's lamp radii —
    ///     Lamp 4 t (LAMP_RADIUS), Arc lamp 6 t (ARC_LAMP_RADIUS), Floodlight 12 t (FLOODLIGHT_RANGE)
    ///     (CONTENT_CATALOGUE.md §140-142, §476-478),
    ///   · 20 of them flickering — a new intensity every frame, the worst case for a light that must redraw,
    ///   · 10 of them moving — tracer / enemy-torch motion,
    ///   · 50 building-sized ShadowCaster2Ds that can be switched on and off as a second measurement.
    ///
    /// Determinism note: this is presentation-side prototype code, not <c>Relight.Sim</c>, but the placement still
    /// uses a fixed integer hash rather than <c>Random</c> so two runs measure the same scene.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Relight/Prototypes/R2 Lighting Rig")]
    public sealed class LightingRig : MonoBehaviour
    {
        /// <summary>recipes.ts LAMP_RADIUS — 4 tiles.</summary>
        public const float LampRadius = 4f;

        /// <summary>flow.ts ARC_LAMP_RADIUS — 6 tiles.</summary>
        public const float ArcLampRadius = 6f;

        /// <summary>recipes.ts FLOODLIGHT_RANGE — 12 tiles.</summary>
        public const float FloodlightRange = 12f;

        [SerializeField] private int mapWidth = 864;
        [SerializeField] private int mapHeight = 576;

        [Tooltip("Total point lights scattered over the map (B-14 R2 spec: 200).")]
        [SerializeField, Min(0)] private int pointLights = 200;

        [Tooltip("How many of them change intensity every frame (B-14 R2 spec: 20).")]
        [SerializeField, Min(0)] private int flickering = 20;

        [Tooltip("How many of them move every frame (B-14 R2 spec: 10).")]
        [SerializeField, Min(0)] private int moving = 10;

        [Tooltip("Building-sized shadow casters (B-14 R2 spec: 50). Enabled by SetShadowCasters.")]
        [SerializeField, Min(0)] private int shadowCasters = 50;

        [Tooltip("Night level for the global light.")]
        [SerializeField, Range(0f, 1f)] private float nightIntensity = 0.12f;

        [SerializeField] private Light2D globalLight;
        [SerializeField] private Transform lightRoot;
        [SerializeField] private Transform casterRoot;

        private readonly List<Light2D> _flicker = new List<Light2D>();
        private readonly List<Light2D> _movers = new List<Light2D>();
        private readonly List<Vector3> _moverHome = new List<Vector3>();
        private readonly List<ShadowCaster2D> _casters = new List<ShadowCaster2D>();
        private float _clock;

        public int PointLightCount { get; private set; }
        public int FlickerCount => _flicker.Count;
        public int MoverCount => _movers.Count;
        public int ShadowCasterCount => _casters.Count;
        public bool ShadowsEnabled { get; private set; }

        private void Awake()
        {
            // Only one global light may be live, or the two add together and the night is not a night.
            foreach (var l in FindObjectsByType<Light2D>(FindObjectsSortMode.None))
                if (l != globalLight && l.lightType == Light2D.LightType.Global) l.enabled = false;
            if (globalLight != null) globalLight.intensity = nightIntensity;

            Collect();
            SetShadowCasters(false);
        }

        /// <summary>Re-read the authored children. Called on Awake and by the play-mode harness after a load.</summary>
        public void Collect()
        {
            _flicker.Clear();
            _movers.Clear();
            _moverHome.Clear();
            _casters.Clear();
            PointLightCount = 0;
            if (lightRoot != null)
            {
                for (var i = 0; i < lightRoot.childCount; i++)
                {
                    var l = lightRoot.GetChild(i).GetComponent<Light2D>();
                    if (l == null) continue;
                    PointLightCount++;
                    if (_flicker.Count < flickering) { _flicker.Add(l); continue; }
                    if (_movers.Count < moving) { _movers.Add(l); _moverHome.Add(l.transform.position); }
                }
            }
            if (casterRoot != null)
                for (var i = 0; i < casterRoot.childCount; i++)
                {
                    var c = casterRoot.GetChild(i).GetComponent<ShadowCaster2D>();
                    if (c != null) _casters.Add(c);
                }
        }

        /// <summary>The second R2 measurement: the same sweep with the 50 casters on.</summary>
        public void SetShadowCasters(bool on)
        {
            ShadowsEnabled = on;
            for (var i = 0; i < _casters.Count; i++) _casters[i].enabled = on;
            // A light only pays for shadows if it is asked to cast them.
            if (lightRoot == null) return;
            for (var i = 0; i < lightRoot.childCount; i++)
            {
                var l = lightRoot.GetChild(i).GetComponent<Light2D>();
                if (l != null) l.shadowsEnabled = on;
            }
        }

        private void Update()
        {
            _clock += Time.unscaledDeltaTime;
            // 20 lights change intensity every frame — the Phaser build's "changed light-mask repaint" case.
            for (var i = 0; i < _flicker.Count; i++)
            {
                var phase = _clock * (7f + i * 0.31f) + i;
                _flicker[i].intensity = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(phase));
            }
            // 10 lights move every frame — tracers and carried lights.
            for (var i = 0; i < _movers.Count; i++)
            {
                var home = _moverHome[i];
                var a = _clock * (0.7f + i * 0.11f) + i;
                _movers[i].transform.position = new Vector3(home.x + Mathf.Cos(a) * 12f, home.y + Mathf.Sin(a) * 12f, home.z);
            }
        }

        public void Configure(int width, int height, int lights, int flicker, int movers, int casters)
        {
            mapWidth = width;
            mapHeight = height;
            pointLights = lights;
            flickering = flicker;
            moving = movers;
            shadowCasters = casters;
        }

        public void SetRoots(Light2D global, Transform lights, Transform casters)
        {
            globalLight = global;
            lightRoot = lights;
            casterRoot = casters;
        }

        public int MapWidth => mapWidth;
        public int MapHeight => mapHeight;
        public int PlannedPointLights => pointLights;
        public int PlannedFlickering => flickering;
        public int PlannedMoving => moving;
        public int PlannedShadowCasters => shadowCasters;
        public float NightIntensity => nightIntensity;

        /// <summary>The radius of light <paramref name="i"/>: the reference's three lamp kinds in a 6 : 3 : 1 mix.</summary>
        public static float RadiusFor(int i)
        {
            var k = i % 10;
            if (k < 6) return LampRadius;
            return k < 9 ? ArcLampRadius : FloodlightRange;
        }

        /// <summary>Deterministic scatter: a fixed integer hash, so every run measures the identical arrangement.</summary>
        public static Vector2Int ScatterTile(int i, int width, int height)
        {
            unchecked
            {
                var h = (uint)(i * 2654435761u);
                h ^= h >> 15;
                h *= 2246822519u;
                h ^= h >> 13;
                var x = (int)(h % (uint)width);
                h ^= h >> 7;
                h *= 3266489917u;
                var y = (int)(h % (uint)height);
                return new Vector2Int(x, y);
            }
        }
    }
}
