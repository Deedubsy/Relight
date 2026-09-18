using System;
using Relight.Sim.UI;
using UnityEngine;

namespace Relight.UI.Settings
{
    /// <summary>
    /// C-10. Player preferences: interface, audio, saving and key bindings.
    ///
    /// They live <b>outside the save file and outside the sim</b> (UI_AND_ONBOARDING.md §3.3, §2.7.2,
    /// RI-02B_UI_SPEC.md:68). The reference keeps them in <c>localStorage</c>; the port keeps them in
    /// <see cref="PlayerPrefs"/>, which is the same promise on a machine instead of in a browser: a save carries
    /// none of this, and a player who moves a save file moves no settings with it.
    ///
    /// Every read is total. A missing or nonsense value yields the documented default rather than an error
    /// (uiPreferences.ts:14, "Missing and corrupt preferences use safe defaults"), and every write is wrapped:
    /// when the store refuses, the setting still applies for this session and <see cref="Writable"/> goes false so
    /// the settings screen can say so in <see cref="FrontEndText.SettingsSessionOnly"/>'s words instead of
    /// pretending the change was kept.
    /// </summary>
    public static class Preferences
    {
        // One prefix, so a player clearing Relight's preferences can find them all.
        private const string Prefix = "relight.";

        public const string KeyScale = Prefix + "ui.scale";
        public const string KeyMotion = Prefix + "ui.motion";
        public const string KeyMaster = Prefix + "audio.master";
        public const string KeyUi = Prefix + "audio.ui";
        public const string KeyWorld = Prefix + "audio.world";
        public const string KeyAlerts = Prefix + "audio.alerts";
        public const string KeyMute = Prefix + "audio.mute";
        public const string KeyInterval = Prefix + "save.intervalMinutes";
        public const string KeyKept = Prefix + "save.kept";
        public const string KeyBindings = Prefix + "input.overrides";
        public const string KeyBindingKeys = Prefix + "input.keys";
        public const string KeyOpeningHint = Prefix + "hint.opening";

        // Defaults. The audio figures are §3.8.1's; the saving figures are §2.7.2's.
        public const int DefaultScale = 100;
        public const string MotionSystem = "system";
        public const string MotionReduce = "reduce";
        public const string MotionFull = "full";
        public const float DefaultMaster = 0.80f;
        public const float DefaultUi = 1.00f;
        public const float DefaultWorld = 1.00f;
        public const float DefaultAlerts = 1.00f;

        /// <summary>False once a write has failed; the settings screen then says the session-only sentence.</summary>
        public static bool Writable { get; private set; } = true;

        public static int Scale
        {
            get => Clamp(GetInt(KeyScale, DefaultScale), FrontEndText.Scales, DefaultScale);
            set => SetInt(KeyScale, Clamp(value, FrontEndText.Scales, DefaultScale));
        }

        public static string Motion
        {
            get
            {
                var v = GetString(KeyMotion, MotionSystem);
                return v == MotionReduce || v == MotionFull ? v : MotionSystem;
            }
            set => SetString(KeyMotion, value == MotionReduce || value == MotionFull ? value : MotionSystem);
        }

        public static float Master { get => GetVolume(KeyMaster, DefaultMaster); set => SetVolume(KeyMaster, value); }
        public static float UiVolume { get => GetVolume(KeyUi, DefaultUi); set => SetVolume(KeyUi, value); }
        public static float WorldVolume { get => GetVolume(KeyWorld, DefaultWorld); set => SetVolume(KeyWorld, value); }
        public static float AlertsVolume { get => GetVolume(KeyAlerts, DefaultAlerts); set => SetVolume(KeyAlerts, value); }

        /// <summary>Mute is a separate state, so unmuting restores the slider rather than guessing a value (§3.8.1).</summary>
        public static bool Mute { get => GetInt(KeyMute, 0) != 0; set => SetInt(KeyMute, value ? 1 : 0); }

        /// <summary>Minutes between autosaves; 0 is Off, which is a legitimate choice (§2.7.2).</summary>
        public static double AutosaveMinutes
        {
            get => Clamp(GetFloat(KeyInterval, (float)AutosaveChoices.DefaultIntervalMinutes),
                         AutosaveChoices.Intervals, AutosaveChoices.DefaultIntervalMinutes);
            set => SetFloat(KeyInterval,
                         (float)Clamp(value, AutosaveChoices.Intervals, AutosaveChoices.DefaultIntervalMinutes));
        }

        public static int AutosavesKept
        {
            get => Clamp(GetInt(KeyKept, AutosaveChoices.DefaultKept), AutosaveChoices.Kept, AutosaveChoices.DefaultKept);
            set => SetInt(KeyKept, Clamp(value, AutosaveChoices.Kept, AutosaveChoices.DefaultKept));
        }

        /// <summary>The Input System's own override document (<c>SaveBindingOverridesAsJson</c>); "" when none.</summary>
        public static string BindingOverrides
        {
            get => GetString(KeyBindings, "");
            set => SetString(KeyBindings, value ?? "");
        }

        /// <summary>
        /// The same overrides in the REFERENCE's vocabulary — "north=w;sprint=Shift" — because
        /// <c>Bindings.Keys</c> and <c>BindingProblem.Problem</c> are defined against reference key names, not
        /// Input System control paths. The Input System document above is what the game plays with; this is what
        /// the settings screen reasons with, and the two are written together and never separately.
        /// </summary>
        public static string BindingKeys
        {
            get => GetString(KeyBindingKeys, "");
            set => SetString(KeyBindingKeys, value ?? "");
        }

        /// <summary>False once the player has dismissed the opening hint; Restore turns it back on (§3.8).</summary>
        public static bool OpeningHint { get => GetInt(KeyOpeningHint, 1) != 0; set => SetInt(KeyOpeningHint, value ? 1 : 0); }

        /// <summary>Push everything to disk. Returns false when the store refused, and remembers that it did.</summary>
        public static bool Save()
        {
            try
            {
                PlayerPrefs.Save();
                Writable = true;
                return true;
            }
            catch (Exception)
            {
                Writable = false;
                return false;
            }
        }

        /// <summary>Try the write again after a failure — <see cref="FrontEndText.RetrySaveSettings"/>.</summary>
        public static bool Retry() => Save();

        /// <summary>The settings screen's status line: saved, or working for this session only.</summary>
        public static string Status => Writable ? FrontEndText.SettingsSaved : FrontEndText.SettingsSessionOnly;

        private static float GetVolume(string key, float fallback)
        {
            var v = GetFloat(key, fallback);
            if (float.IsNaN(v) || float.IsInfinity(v)) return fallback;
            return v < 0f ? 0f : v > 1f ? 1f : v;
        }

        private static void SetVolume(string key, float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) return;
            SetFloat(key, value < 0f ? 0f : value > 1f ? 1f : value);
        }

        /// <summary>A value that is not one of the offered choices is not "close to" one: it is the default.</summary>
        private static int Clamp(int value, int[] allowed, int fallback)
        {
            for (var i = 0; i < allowed.Length; i++)
                if (allowed[i] == value) return value;
            return fallback;
        }

        private static double Clamp(double value, double[] allowed, double fallback)
        {
            for (var i = 0; i < allowed.Length; i++)
                if (Math.Abs(allowed[i] - value) < 0.0001) return allowed[i];
            return fallback;
        }

        private static int GetInt(string key, int fallback)
        {
            try { return PlayerPrefs.GetInt(key, fallback); }
            catch (Exception) { return fallback; }
        }

        private static void SetInt(string key, int value)
        {
            try { PlayerPrefs.SetInt(key, value); }
            catch (Exception) { Writable = false; }
        }

        private static float GetFloat(string key, float fallback)
        {
            try { return PlayerPrefs.GetFloat(key, fallback); }
            catch (Exception) { return fallback; }
        }

        private static void SetFloat(string key, float value)
        {
            try { PlayerPrefs.SetFloat(key, value); }
            catch (Exception) { Writable = false; }
        }

        private static string GetString(string key, string fallback)
        {
            try { return PlayerPrefs.GetString(key, fallback); }
            catch (Exception) { return fallback; }
        }

        private static void SetString(string key, string value)
        {
            try { PlayerPrefs.SetString(key, value); }
            catch (Exception) { Writable = false; }
        }
    }
}
