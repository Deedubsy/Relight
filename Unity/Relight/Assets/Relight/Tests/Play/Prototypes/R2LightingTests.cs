using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Relight.Prototypes;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Relight.Tests.Play.Prototypes
{
    /// <summary>
    /// B-14 / R2 — "can URP's 2D lights carry the reference's night at city scale". Risk R2 in
    /// TECHNICAL_ARCHITECTURE.md: the reference's worst frames are a light-mask cost (~24 ms per changed repaint,
    /// worst frames 116.8 / 150.1 / 266.8 / 300.2 ms, §3.3), so the port needs a measured lighting budget.
    ///
    /// The rig is loaded ADDITIVELY over the R7 tilemap, so the lights are measured over the real 497,664-tile map
    /// rather than over an empty scene, and the three passes are directly comparable:
    ///   1. tilemap only (R7's own global light),
    ///   2. + 200 point lights, 20 flickering, 10 moving,
    ///   3. + 50 shadow casters, every light casting.
    /// </summary>
    public sealed class R2LightingTests
    {
        private const float SweepSeconds = 10f;    // the brief: the same sweep as R7

        [UnityTest, Timeout(300000)]
        public IEnumerator TheRigHasTheSpecifiedDensity()
        {
            yield return PrototypeFixture.Load(PrototypeFixture.R2);
            var rig = Object.FindAnyObjectByType<LightingRig>();
            Assert.NotNull(rig, "R2-Lighting.unity has no LightingRig; run Relight/Prototypes/Build R2.");
            rig.Collect();

            Assert.AreEqual(200, rig.PointLightCount, "point lights");
            Assert.AreEqual(50, rig.ShadowCasterCount, "shadow casters");
            Assert.AreEqual(20, rig.FlickerCount, "lights changing intensity every frame");
            Assert.AreEqual(10, rig.MoverCount, "lights moving every frame");

            // The reference's three lamp radii. CONTENT_CATALOGUE.md §140-142 / §476-478 give 4 / 6 / 12 tiles —
            // NOT the "~4/8/12" the brief quotes; the catalogue wins and the report records the difference.
            Assert.AreEqual(4f, LightingRig.LampRadius);
            Assert.AreEqual(6f, LightingRig.ArcLampRadius);
            Assert.AreEqual(12f, LightingRig.FloodlightRange);

            int lamp = 0, arc = 0, flood = 0;
            foreach (var l in Object.FindObjectsByType<Light2D>(FindObjectsSortMode.None))
            {
                if (l.lightType != Light2D.LightType.Point) continue;
                if (Mathf.Approximately(l.pointLightOuterRadius, LightingRig.LampRadius)) lamp++;
                else if (Mathf.Approximately(l.pointLightOuterRadius, LightingRig.ArcLampRadius)) arc++;
                else if (Mathf.Approximately(l.pointLightOuterRadius, LightingRig.FloodlightRange)) flood++;
            }
            Assert.AreEqual(200, lamp + arc + flood, "every point light carries one of the three catalogue radii");
            Assert.AreEqual(120, lamp);
            Assert.AreEqual(60, arc);
            Assert.AreEqual(20, flood);
            UnityEngine.Debug.Log($"B14 R2: {lamp} lamps (4 t), {arc} arc lamps (6 t), {flood} floodlights (12 t).");
        }

        [UnityTest, Timeout(900000)]
        public IEnumerator ThreePassesOverTheRealMapAreMeasured()
        {
            PrototypeFixture.RequireGraphics();
            PrototypeFixture.RequirePaintedMap();

            // Pass 1 — the tilemap on its own.
            yield return PrototypeFixture.Load(PrototypeFixture.R7);
            var sweep = Object.FindAnyObjectByType<CameraSweep>();
            Assert.NotNull(sweep, "the R7 scene supplies the camera; R2 deliberately has none.");
            sweep.Configure(R7TilemapTests.Width, R7TilemapTests.Height, SweepSeconds);
            var camera = sweep.GetComponent<Camera>();
            _probe = new RenderProbe();
            _probe.RenderMs(camera);
            var baselineProbe = new FrameSampler("tilemap-only-probe");
            var litProbe = new FrameSampler("200-lights-probe");
            var shadowedProbe = new FrameSampler("200-lights-50-casters-probe");
            var baseline = new FrameSampler("tilemap-only");
            yield return Sweep(sweep, baseline, baselineProbe);

            // Pass 2 — the same sweep with the rig loaded on top. LightingRig.Awake switches off R7's global light
            // and installs its own at night intensity, so the two lit passes are not double-lit.
            yield return PrototypeFixture.Load(PrototypeFixture.R2, LoadSceneMode.Additive);
            var rig = Object.FindAnyObjectByType<LightingRig>();
            Assert.NotNull(rig);
            rig.Collect();
            rig.SetShadowCasters(false);
            for (var i = 0; i < 30; i++) yield return null;
            var lit = new FrameSampler("200-lights");
            yield return Sweep(sweep, lit, litProbe);

            // Pass 3 — the same again with 50 casters and every light casting shadows.
            rig.SetShadowCasters(true);
            for (var i = 0; i < 30; i++) yield return null;
            var shadowed = new FrameSampler("200-lights-50-casters");
            yield return Sweep(sweep, shadowed, shadowedProbe);

            sweep.Place(0.5f);
            var png = _probe.Capture(camera, "b14-r2-night.png");

            var values = new List<KeyValuePair<string, object>>
            {
                new KeyValuePair<string, object>("prototype", "R2 lighting"),
                new KeyValuePair<string, object>("screenshot", System.IO.Path.GetFileName(png)),
                new KeyValuePair<string, object>("map_tiles", R7TilemapTests.Tiles),
                new KeyValuePair<string, object>("point_lights", rig.PointLightCount),
                new KeyValuePair<string, object>("flickering_lights", rig.FlickerCount),
                new KeyValuePair<string, object>("moving_lights", rig.MoverCount),
                new KeyValuePair<string, object>("shadow_casters", rig.ShadowCasterCount),
                new KeyValuePair<string, object>("lamp_radius_tiles", LightingRig.LampRadius),
                new KeyValuePair<string, object>("arc_lamp_radius_tiles", LightingRig.ArcLampRadius),
                new KeyValuePair<string, object>("floodlight_range_tiles", LightingRig.FloodlightRange),
                new KeyValuePair<string, object>("night_intensity", rig.NightIntensity),
                new KeyValuePair<string, object>("sweep_seconds", SweepSeconds),
                new KeyValuePair<string, object>("graphics_device", SystemInfo.graphicsDeviceType.ToString()),
                new KeyValuePair<string, object>("screen_width", Screen.width),
                new KeyValuePair<string, object>("screen_height", Screen.height),
                new KeyValuePair<string, object>("probe_width", _probe.Width),
                new KeyValuePair<string, object>("probe_height", _probe.Height),
                new KeyValuePair<string, object>("total_allocated_bytes", UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong()),
                new KeyValuePair<string, object>("total_reserved_bytes", UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong()),
                new KeyValuePair<string, object>("note_sweep", "unscaledDeltaTime in batchmode is uncapped and is not a frame budget"),
                new KeyValuePair<string, object>("note_probe", "camera.Render into a RenderTexture plus a 1-px ReadPixels GPU fence"),
                new KeyValuePair<string, object>("reference_worst_frames_ms", "116.8 / 150.1 / 266.8 / 300.2 (TA 3.3)"),
            };
            EvidenceWriter.AddFrames(values, "pass1_tilemap", baseline);
            EvidenceWriter.AddFrames(values, "pass2_lights", lit);
            EvidenceWriter.AddFrames(values, "pass3_shadows", shadowed);
            EvidenceWriter.AddFrames(values, "pass1_tilemap_probe", baselineProbe);
            EvidenceWriter.AddFrames(values, "pass2_lights_probe", litProbe);
            EvidenceWriter.AddFrames(values, "pass3_shadows_probe", shadowedProbe);
            values.Add(new KeyValuePair<string, object>("lights_cost_mean_ms", EvidenceWriter.Round(lit.Mean() - baseline.Mean())));
            values.Add(new KeyValuePair<string, object>("shadows_cost_mean_ms", EvidenceWriter.Round(shadowed.Mean() - lit.Mean())));
            values.Add(new KeyValuePair<string, object>("probe_lights_cost_mean_ms", EvidenceWriter.Round(litProbe.Mean() - baselineProbe.Mean())));
            values.Add(new KeyValuePair<string, object>("probe_shadows_cost_mean_ms", EvidenceWriter.Round(shadowedProbe.Mean() - litProbe.Mean())));
            EvidenceWriter.Write("b14-r2-frames.json", values);

            UnityEngine.Debug.Log("B14 R2 pass 1: " + baseline.Summary() + " | probe " + baselineProbe.Summary());
            UnityEngine.Debug.Log("B14 R2 pass 2: " + lit.Summary() + " | probe " + litProbe.Summary());
            UnityEngine.Debug.Log("B14 R2 pass 3: " + shadowed.Summary() + " | probe " + shadowedProbe.Summary());
            _probe.Dispose();
            _probe = null;
            Assert.Greater(baseline.Count, 10);
            Assert.Greater(lit.Count, 10);
            Assert.Greater(shadowed.Count, 10);
            Assert.Greater(shadowedProbe.Count, 10);
        }

        /// <summary>The render probe of the pass in flight; see <see cref="RenderProbe"/> for why deltaTime is not enough.</summary>
        private RenderProbe _probe;

        [TearDown]
        public void DisposeProbe()
        {
            if (_probe == null) return;
            _probe.Dispose();
            _probe = null;
        }

        private IEnumerator Sweep(CameraSweep sweep, FrameSampler into, FrameSampler probeInto)
        {
            for (var i = 0; i < 20; i++) yield return null;      // settle
            var camera = sweep.GetComponent<Camera>();
            sweep.Begin();
            while (sweep.Running)
            {
                yield return null;
                into.Add(Time.unscaledDeltaTime * 1000.0);
                if (_probe != null) probeInto.Add(_probe.RenderMs(camera));
            }
        }
    }
}
