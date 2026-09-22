using System;
using System.Collections.Generic;
using UnityEngine;

namespace Relight.Presentation
{
    /// <summary>
    /// REL-67, U-D-69 (d): the rough placeholder sounds, made in code. Every cue key the game raises gets a short
    /// beep, thud, chirp or siren built from a few sine, square, triangle, saw and filtered-noise voices, so alerts
    /// can be heard and the volume sliders can be checked before any real sound exists. Real clips replace these
    /// through <see cref="AudioCueRouter"/>'s authored table: a key with an authored clip never reaches this class.
    ///
    /// Provenance: made by the assistant in code, so there is no licence to record (U-D-69 (d)). D-09, F-03 and
    /// F-04 still own the real sound set.
    ///
    /// The samples are a pure function of the key: the same key always gives the same sound (the noise is seeded
    /// from the key), which is what lets a test check them without playing anything.
    /// </summary>
    public static class PlaceholderSounds
    {
        /// <summary>Samples per second of every placeholder. Low on purpose: these are beeps, not recordings.</summary>
        public const int SampleRate = 22050;

        /// <summary>The prefix of the per-kind approach cues (U-D-62). Unknown kinds get a pitch from their name.</summary>
        public const string ApproachPrefix = "enemy.approach.";

        private enum Wave { Sine, Square, Triangle, Saw, Noise }

        /// <summary>One voice: a wave swept from <c>F0</c> to <c>F1</c> (for noise, a low-pass cutoff), shaped by an envelope.</summary>
        private readonly struct Voice
        {
            public readonly Wave Wave;
            public readonly float F0, F1, At, Dur, Decay, Attack, Amp, VibHz, VibDepth;

            public Voice(Wave wave, float f0, float f1, float at, float dur, float decay, float attack, float amp,
                float vibHz, float vibDepth)
            {
                Wave = wave; F0 = f0; F1 = f1; At = at; Dur = dur; Decay = decay; Attack = attack; Amp = amp;
                VibHz = vibHz; VibDepth = vibDepth;
            }
        }

        private sealed class Recipe
        {
            public readonly float Gain;
            public readonly Voice[] Voices;
            public Recipe(float gain, Voice[] voices) { Gain = gain; Voices = voices; }
        }

        private static Voice V(Wave w, float f0, float f1, float at, float dur, float decay = 1f, float attack = 0.004f,
            float amp = 1f, float vibHz = 0f, float vibDepth = 0f) =>
            new Voice(w, f0, f1, at, dur, decay, attack, amp, vibHz, vibDepth);

        private static Recipe R(float gain, params Voice[] voices) => new Recipe(gain, voices);

        /// <summary>
        /// The table. Keys are the router's and <see cref="AudioCue.Ui"/>'s. A key with a kind on the end
        /// (<c>weapon.shot.rifle</c>, <c>enemy.death.skitter</c>) falls back to its family row when it has no row
        /// of its own, so a new kind is never silent.
        /// </summary>
        private static readonly Dictionary<string, Recipe> Table = new Dictionary<string, Recipe>(StringComparer.Ordinal)
        {
            // ---- interface (ui.*) ------------------------------------------------------------------------------
            ["ui.panel.open"] = R(0.35f, V(Wave.Sine, 700, 1000, 0, 0.06f, 2)),
            ["ui.panel.close"] = R(0.35f, V(Wave.Sine, 1000, 700, 0, 0.06f, 2)),
            ["ui.pause"] = R(0.4f, V(Wave.Sine, 660, 660, 0, 0.07f), V(Wave.Sine, 440, 440, 0.07f, 0.09f)),
            ["ui.resume"] = R(0.4f, V(Wave.Sine, 440, 440, 0, 0.07f), V(Wave.Sine, 660, 660, 0.07f, 0.09f)),
            ["ui.escape"] = R(0.35f, V(Wave.Sine, 500, 380, 0, 0.05f, 2)),
            ["ui.confirm"] = R(0.45f, V(Wave.Sine, 660, 660, 0, 0.06f), V(Wave.Sine, 990, 990, 0.06f, 0.1f, 2)),
            ["ui.refused"] = R(0.35f, V(Wave.Square, 160, 160, 0, 0.09f, 0.5f), V(Wave.Square, 120, 120, 0.1f, 0.12f, 0.8f)),
            ["ui.drag.pick"] = R(0.3f, V(Wave.Sine, 900, 1300, 0, 0.035f, 2)),
            ["ui.drag.drop"] = R(0.35f, V(Wave.Sine, 700, 450, 0, 0.05f, 2), V(Wave.Noise, 2000, 2000, 0, 0.03f, 3, amp: 0.3f)),
            ["ui.drag.cancel"] = R(0.3f, V(Wave.Sine, 500, 300, 0, 0.08f, 1.5f)),
            ["ui.transfer"] = R(0.3f, V(Wave.Sine, 1200, 1200, 0, 0.03f, 2), V(Wave.Sine, 1500, 1500, 0.035f, 0.04f, 2)),
            ["ui.slot"] = R(0.25f, V(Wave.Sine, 1100, 1100, 0, 0.03f, 2)),
            ["ui.toast.good"] = R(0.4f, V(Wave.Sine, 880, 880, 0, 0.08f), V(Wave.Sine, 1320, 1320, 0.08f, 0.14f, 2)),
            ["ui.toast.bad"] = R(0.4f, V(Wave.Triangle, 300, 300, 0, 0.1f), V(Wave.Triangle, 220, 220, 0.1f, 0.14f, 1.5f)),
            ["ui.autosave"] = R(0.3f, V(Wave.Sine, 520, 520, 0, 0.05f), V(Wave.Sine, 780, 780, 0.05f, 0.08f, 2)),

            // ---- weapons and impacts (world) --------------------------------------------------------------------
            ["weapon.shot"] = R(0.6f, V(Wave.Noise, 6000, 800, 0, 0.14f, 3), V(Wave.Sine, 140, 50, 0, 0.1f, 2, amp: 0.8f)),
            ["weapon.reload"] = R(0.4f,
                V(Wave.Noise, 5000, 5000, 0, 0.02f, 3), V(Wave.Square, 900, 900, 0, 0.015f, 3, amp: 0.4f),
                V(Wave.Noise, 4000, 4000, 0.12f, 0.025f, 3), V(Wave.Square, 700, 700, 0.12f, 0.02f, 3, amp: 0.4f)),
            ["turret.shot"] = R(0.4f, V(Wave.Noise, 5000, 1200, 0, 0.09f, 3, amp: 0.8f), V(Wave.Sine, 180, 70, 0, 0.06f, 2, amp: 0.5f)),
            ["impact.hit"] = R(0.4f, V(Wave.Sine, 160, 60, 0, 0.09f, 2), V(Wave.Noise, 1500, 1500, 0, 0.05f, 3, amp: 0.5f)),
            ["impact.miss"] = R(0.2f, V(Wave.Noise, 3000, 1500, 0, 0.05f, 3, amp: 0.5f)),

            // ---- enemies (world) --------------------------------------------------------------------------------
            ["enemy.spit"] = R(0.4f, V(Wave.Noise, 700, 1800, 0, 0.18f, 1, 0.01f, 1f, 30, 0.4f), V(Wave.Sine, 300, 500, 0, 0.12f, amp: 0.3f)),
            ["enemy.death"] = R(0.45f, V(Wave.Saw, 900, 200, 0, 0.3f, 1.5f, amp: 0.6f, vibHz: 18, vibDepth: 0.08f), V(Wave.Noise, 2500, 2500, 0, 0.2f, 2, amp: 0.3f)),
            ["enemy.death.skitter"] = R(0.4f, V(Wave.Saw, 1400, 300, 0, 0.22f, 1.5f, amp: 0.6f, vibHz: 24, vibDepth: 0.1f), V(Wave.Noise, 4000, 4000, 0, 0.12f, 2, amp: 0.3f)),
            ["enemy.death.spitter"] = R(0.45f, V(Wave.Saw, 600, 150, 0, 0.35f, 1.2f, amp: 0.5f, vibHz: 12, vibDepth: 0.12f), V(Wave.Noise, 1200, 600, 0, 0.3f, 1.5f, amp: 0.6f)),
            ["enemy.death.breaker"] = R(0.55f, V(Wave.Saw, 300, 60, 0, 0.45f, 1.2f, amp: 0.6f, vibHz: 9, vibDepth: 0.1f), V(Wave.Sine, 80, 35, 0, 0.4f, 1.5f, amp: 0.8f)),

            // U-D-62: each kind has its own approach sound, heard beyond sight and panned by direction.
            // Skitter: a fast dry chitter. Spitter: a low wet gurgle. Breaker: slow heavy footfalls.
            [ApproachPrefix + "skitter"] = R(0.45f,
                V(Wave.Noise, 7000, 7000, 0.00f, 0.02f, 3), V(Wave.Sine, 3200, 2600, 0.00f, 0.015f, 3, amp: 0.3f),
                V(Wave.Noise, 7000, 7000, 0.07f, 0.02f, 3), V(Wave.Sine, 3000, 2400, 0.07f, 0.015f, 3, amp: 0.3f),
                V(Wave.Noise, 7000, 7000, 0.14f, 0.02f, 3), V(Wave.Sine, 3300, 2700, 0.14f, 0.015f, 3, amp: 0.3f),
                V(Wave.Noise, 7000, 7000, 0.21f, 0.02f, 3), V(Wave.Sine, 3100, 2500, 0.21f, 0.015f, 3, amp: 0.3f),
                V(Wave.Noise, 7000, 7000, 0.28f, 0.02f, 3), V(Wave.Sine, 3200, 2600, 0.28f, 0.015f, 3, amp: 0.3f),
                V(Wave.Noise, 7000, 7000, 0.35f, 0.02f, 3), V(Wave.Sine, 2900, 2300, 0.35f, 0.015f, 3, amp: 0.3f)),
            [ApproachPrefix + "spitter"] = R(0.5f,
                V(Wave.Sine, 110, 150, 0, 0.6f, 0.5f, 0.08f, 1f, 7, 0.15f),
                V(Wave.Noise, 500, 900, 0.05f, 0.5f, 0.8f, 0.05f, 0.5f, 11, 0.5f)),
            [ApproachPrefix + "breaker"] = R(0.6f,
                V(Wave.Sine, 70, 45, 0.0f, 0.18f, 2), V(Wave.Noise, 400, 400, 0.0f, 0.1f, 3, amp: 0.6f),
                V(Wave.Sine, 70, 45, 0.4f, 0.18f, 2), V(Wave.Noise, 400, 400, 0.4f, 0.1f, 3, amp: 0.6f)),

            // ---- structures and machines (world) ----------------------------------------------------------------
            ["structure.hit"] = R(0.4f, V(Wave.Sine, 440, 440, 0, 0.18f, 3, amp: 0.6f), V(Wave.Sine, 623, 623, 0, 0.15f, 3, amp: 0.5f), V(Wave.Noise, 3000, 3000, 0, 0.03f, 3, amp: 0.4f)),
            ["structure.destroyed"] = R(0.6f, V(Wave.Noise, 3000, 300, 0, 0.7f, 2), V(Wave.Sine, 90, 35, 0, 0.5f, 2), V(Wave.Sine, 330, 330, 0.05f, 0.3f, 3, amp: 0.3f)),
            ["machine.stop"] = R(0.4f, V(Wave.Triangle, 220, 90, 0, 0.35f, 1)),
            ["machine.start"] = R(0.4f, V(Wave.Triangle, 90, 220, 0, 0.35f, 0.5f)),
            ["machine.produced"] = R(0.12f, V(Wave.Sine, 1600, 1600, 0, 0.025f, 3)),
            ["mine.tick"] = R(0.25f, V(Wave.Noise, 4000, 4000, 0, 0.03f, 3), V(Wave.Sine, 1800, 1200, 0, 0.03f, 3, amp: 0.4f)),
            ["mine.cleared"] = R(0.35f, V(Wave.Noise, 4000, 4000, 0, 0.03f, 3), V(Wave.Sine, 880, 880, 0.04f, 0.12f, 2, amp: 0.5f), V(Wave.Sine, 1320, 1320, 0.08f, 0.14f, 2, amp: 0.4f)),
            ["mine.stopped"] = R(0.3f, V(Wave.Sine, 300, 200, 0, 0.1f, 1.5f)),
            ["craft.done"] = R(0.4f, V(Wave.Sine, 660, 660, 0, 0.1f), V(Wave.Sine, 880, 880, 0.08f, 0.12f), V(Wave.Sine, 1320, 1320, 0.16f, 0.2f, 2)),
            ["core.repaired"] = R(0.45f, V(Wave.Sine, 440, 440, 0, 0.15f), V(Wave.Sine, 660, 660, 0.12f, 0.15f), V(Wave.Sine, 880, 880, 0.24f, 0.3f, 2)),

            // ---- light (world): the two relief moments of ALWAYS_DARK_SPEC §5.4 -------------------------------------
            ["light.district-on"] = R(0.45f, V(Wave.Sine, 220, 440, 0, 0.6f, 0.3f, 0.05f), V(Wave.Sine, 330, 660, 0.05f, 0.6f, 0.3f, 0.05f, 0.6f), V(Wave.Sine, 440, 880, 0.1f, 0.6f, 0.3f, 0.05f, 0.4f)),
            ["light.entered"] = R(0.3f, V(Wave.Sine, 330, 330, 0, 0.4f, 0.8f, 0.08f, 0.7f), V(Wave.Sine, 495, 495, 0.02f, 0.4f, 0.8f, 0.08f, 0.5f)),

            // ---- alerts (raid.*, core.*, engineer.*, power.*) ----------------------------------------------------
            ["raid.warning"] = R(0.55f,
                V(Wave.Square, 520, 880, 0.00f, 0.35f, 0.2f, 0.02f), V(Wave.Square, 880, 520, 0.35f, 0.35f, 0.2f, 0.02f),
                V(Wave.Square, 520, 880, 0.70f, 0.35f, 0.2f, 0.02f), V(Wave.Square, 880, 520, 1.05f, 0.35f, 0.6f, 0.02f)),
            ["raid.begins"] = R(0.55f,
                V(Wave.Square, 110, 110, 0.00f, 0.25f, 0.3f, 0.01f), V(Wave.Square, 165, 165, 0.00f, 0.25f, 0.3f, 0.01f, 0.5f),
                V(Wave.Square, 110, 110, 0.35f, 0.25f, 0.3f, 0.01f), V(Wave.Square, 165, 165, 0.35f, 0.25f, 0.3f, 0.01f, 0.5f),
                V(Wave.Square, 110, 110, 0.70f, 0.40f, 0.8f, 0.01f), V(Wave.Square, 165, 165, 0.70f, 0.40f, 0.8f, 0.01f, 0.5f)),
            ["core.disabled"] = R(0.55f, V(Wave.Square, 660, 330, 0, 0.4f, 0.3f), V(Wave.Square, 660, 330, 0.45f, 0.4f, 0.3f), V(Wave.Square, 330, 165, 0.9f, 0.6f, 1)),
            ["engineer.down"] = R(0.5f, V(Wave.Triangle, 440, 220, 0, 0.3f, 0.5f), V(Wave.Triangle, 330, 110, 0.3f, 0.5f, 1)),
            ["engineer.up"] = R(0.45f, V(Wave.Triangle, 220, 440, 0, 0.2f, 0.5f), V(Wave.Triangle, 330, 660, 0.2f, 0.25f, 1)),
            ["power.generator-dry"] = R(0.45f, V(Wave.Noise, 800, 800, 0, 0.06f, 2), V(Wave.Noise, 800, 800, 0.12f, 0.05f, 2), V(Wave.Noise, 800, 800, 0.22f, 0.04f, 2), V(Wave.Sine, 300, 150, 0.3f, 0.3f, 1)),
        };

        private static readonly Dictionary<string, AudioClip> Clips = new Dictionary<string, AudioClip>(StringComparer.Ordinal);

        /// <summary>Every key with a row of its own, for tests and for a listening pass.</summary>
        public static IEnumerable<string> Keys => Table.Keys;

        /// <summary>Whether <paramref name="key"/> has a placeholder, its own or its family's.</summary>
        public static bool Has(string key) => Find(key) != null;

        /// <summary>
        /// The placeholder's samples, mono at <see cref="SampleRate"/>, peak at the row's gain; null when the key has
        /// none. A fresh array each call.
        /// </summary>
        public static float[] Samples(string key)
        {
            var recipe = Find(key);
            return recipe == null ? null : Render(recipe, Seed(key));
        }

        /// <summary>The placeholder as a clip, built on first use and kept; null when the key has none.</summary>
        public static AudioClip Clip(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (Clips.TryGetValue(key, out var cached) && cached != null) return cached;
            var samples = Samples(key);
            if (samples == null) return null;
            var clip = AudioClip.Create("placeholder " + key, samples.Length, 1, SampleRate, false);
            clip.SetData(samples, 0);
            clip.hideFlags = HideFlags.DontSave;
            Clips[key] = clip;
            return clip;
        }

        private static Recipe Find(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (Table.TryGetValue(key, out var exact)) return exact;
            // A kind with no approach row of its own still gets a sound of its own: a hum pitched from its name.
            if (key.StartsWith(ApproachPrefix, StringComparison.Ordinal) && key.Length > ApproachPrefix.Length)
                return Hum(key.Substring(ApproachPrefix.Length));
            for (var cut = key.LastIndexOf('.'); cut > 0; cut = key.LastIndexOf('.', cut - 1))
                if (Table.TryGetValue(key.Substring(0, cut), out var family)) return family;
            return null;
        }

        private static Recipe Hum(string kind)
        {
            var f = 180f + (Seed(kind) % 600u);
            return R(0.45f, V(Wave.Triangle, f, f * 0.85f, 0, 0.45f, 0.6f, 0.05f, 1f, 5, 0.1f));
        }

        /// <summary>FNV-1a of the key: the noise seed and the unknown-kind pitch. Stable across runs and machines.</summary>
        private static uint Seed(string s)
        {
            var h = 2166136261u;
            for (var i = 0; i < s.Length; i++) { h ^= s[i]; h *= 16777619u; }
            return h == 0 ? 1u : h;
        }

        private static float[] Render(Recipe recipe, uint seed)
        {
            var end = 0f;
            foreach (var v in recipe.Voices) end = Mathf.Max(end, v.At + v.Dur);
            var n = Mathf.Max(1, Mathf.CeilToInt(end * SampleRate));
            var buffer = new float[n];
            var rng = seed;

            foreach (var v in recipe.Voices)
            {
                var start = Mathf.FloorToInt(v.At * SampleRate);
                var count = Mathf.Min(n - start, Mathf.CeilToInt(v.Dur * SampleRate));
                var phase = 0.0;
                var low = 0f;
                for (var i = 0; i < count; i++)
                {
                    var t = i / (float)SampleRate;
                    var u = v.Dur <= 0f ? 1f : Mathf.Clamp01(t / v.Dur);
                    var f = v.F0 * Mathf.Pow(v.F1 / v.F0, u);                              // exponential sweep
                    if (v.VibHz > 0f) f *= 1f + v.VibDepth * Mathf.Sin(2f * Mathf.PI * v.VibHz * t);

                    float s;
                    if (v.Wave == Wave.Noise)
                    {
                        rng ^= rng << 13; rng ^= rng >> 17; rng ^= rng << 5;              // xorshift32
                        var white = (rng / (float)uint.MaxValue) * 2f - 1f;
                        var k = 1f - Mathf.Exp(-2f * Mathf.PI * f / SampleRate);          // one-pole low-pass at f
                        low += k * (white - low);
                        s = low;
                    }
                    else
                    {
                        phase += f / SampleRate;
                        var p = (float)(phase - Math.Floor(phase));
                        switch (v.Wave)
                        {
                            case Wave.Square: s = p < 0.5f ? 0.6f : -0.6f; break;          // softened
                            case Wave.Triangle: s = 1f - 4f * Mathf.Abs(p - 0.5f); break;
                            case Wave.Saw: s = (2f * p - 1f) * 0.7f; break;
                            default: s = Mathf.Sin(2f * Mathf.PI * p); break;
                        }
                    }

                    var attack = v.Attack <= 0f ? 1f : Mathf.Clamp01(t / v.Attack);
                    var decay = v.Decay <= 0f ? 1f : Mathf.Pow(1f - u, v.Decay);
                    var tail = Mathf.Clamp01((v.Dur - t) / 0.004f);                       // no click at the end
                    buffer[start + i] += s * v.Amp * attack * decay * tail;
                }
            }

            var peak = 0f;
            for (var i = 0; i < n; i++) peak = Mathf.Max(peak, Mathf.Abs(buffer[i]));
            if (peak > 0f)
            {
                var scale = recipe.Gain / peak;
                for (var i = 0; i < n; i++) buffer[i] *= scale;
            }
            return buffer;
        }
    }
}
