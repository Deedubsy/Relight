using System;
using UnityEngine;

namespace Relight.Presentation
{
    /// <summary>
    /// REL-67: the player's four volume levels and mute, as the router applies them when the project has no
    /// <see cref="UnityEngine.Audio.AudioMixer"/> (it has none today). Settings writes these from the stored
    /// preferences; <see cref="AudioCueRouter"/> multiplies each cue by <see cref="Gain"/>. They are the player's
    /// preferences, never the save's.
    ///
    /// When a mixer is added later, Settings drives the mixer's exposed parameters as well and the router routes
    /// to the mixer groups instead, so the two never apply the same level twice.
    /// </summary>
    public static class AudioLevels
    {
        public const string UiGroup = "ui";
        public const string WorldGroup = "world";
        public const string AlertsGroup = "alerts";

        /// <summary>0..1. The defaults match <c>Preferences</c>' own until Settings pushes the stored values.</summary>
        public static float Master { get; private set; } = 0.8f;
        public static float Ui { get; private set; } = 1f;
        public static float World { get; private set; } = 1f;
        public static float Alerts { get; private set; } = 1f;
        public static bool Mute { get; private set; }

        /// <summary>Set every level at once. Values are clamped to 0..1.</summary>
        public static void Set(float master, float ui, float world, float alerts, bool mute)
        {
            Master = Mathf.Clamp01(master);
            Ui = Mathf.Clamp01(ui);
            World = Mathf.Clamp01(world);
            Alerts = Mathf.Clamp01(alerts);
            Mute = mute;
        }

        /// <summary>The loudness of one group: master times the group's own level, or zero when muted.</summary>
        public static float Gain(string group)
        {
            if (Mute) return 0f;
            float level;
            if (string.Equals(group, UiGroup, StringComparison.Ordinal)) level = Ui;
            else if (string.Equals(group, AlertsGroup, StringComparison.Ordinal)) level = Alerts;
            else level = World;
            return Master * level;
        }
    }
}
