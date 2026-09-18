using UnityEngine;

namespace Relight.Presentation
{
    /// <summary>Anything that can voice a cue. <see cref="AudioCueRouter"/> is the one in the scene.</summary>
    public interface IAudioCueSink
    {
        /// <summary>Voice <paramref name="key"/>, at a sim tile position when the cue has one.</summary>
        void Play(string key, Vector2? at);
    }

    /// <summary>
    /// C-11. The single audio hook (UI_AND_ONBOARDING §12, TASKS F-04). Everything in the game that wants a sound
    /// calls <see cref="Play(string,System.Nullable{Vector2})"/> with a key; whether that key maps to a clip is
    /// entirely <see cref="AudioCueRouter"/>'s business, and today it maps to nothing.
    ///
    /// The rules this obeys, from §12.1:
    /// <list type="number">
    /// <item>"A cue is a readback, never a mechanic." Nothing here returns a value a caller can branch on, so no cue
    /// can gate an action, and <c>Relight.Sim</c> does not reference this assembly at all.</item>
    /// <item><b>No clips.</b> "An unmapped key logs nothing and plays nothing" — an unknown key is silence, not a
    /// warning, because the whole key table is unmapped until D-11 supplies assets and a console full of warnings
    /// during the opening would be worse than the silence.</item>
    /// <item>"Sound follows the clock." The router drains a paused host's (empty) event list, so a paused game makes
    /// no sound and nothing queues up to replay on resume.</item>
    /// </list>
    ///
    /// <see cref="Requested"/> exists so the coordinator can confirm the wiring fires without a single audio file
    /// in the project.
    /// </summary>
    public static class AudioCue
    {
        /// <summary>The scene's router. Null means every cue is silence, which is the shipped state.</summary>
        public static IAudioCueSink Sink { get; set; }

        /// <summary>How many cues have been asked for since the game started. A wiring check, never gameplay.</summary>
        public static int Requested { get; private set; }

        /// <summary>The key of the last cue asked for. A wiring check.</summary>
        public static string Last { get; private set; } = "";

        /// <summary>Ask for a cue. Safe from any thread-free Unity context, safe with no router, safe with no clip.</summary>
        public static void Play(string key, Vector2? at = null)
        {
            if (string.IsNullOrEmpty(key)) return;
            Requested++;
            Last = key;
            var sink = Sink;
            if (sink == null) return;
            sink.Play(key, at);
        }

        /// <summary>Ask for a cue at a sim tile position.</summary>
        public static void Play(string key, Relight.Sim.Vec2 at) =>
            Play(key, new Vector2((float)at.X, (float)at.Y));

        /// <summary>Forget the counters. For tests and for a fresh session.</summary>
        public static void Reset() { Requested = 0; Last = ""; }

        /// <summary>
        /// The keys the UI raises itself (§12.7). The sim-event keys are in <see cref="AudioCueRouter"/>, because
        /// they are derived from events rather than typed by a caller. Both halves use the same "group.thing"
        /// shape so a mixer group can be chosen from the prefix alone.
        /// </summary>
        public static class Ui
        {
            public const string PanelOpen = "ui.panel.open";
            public const string PanelClose = "ui.panel.close";
            public const string Pause = "ui.pause";
            public const string Resume = "ui.resume";
            public const string Escape = "ui.escape";
            public const string Confirm = "ui.confirm";
            public const string Refused = "ui.refused";
            public const string DragPick = "ui.drag.pick";
            public const string DragDrop = "ui.drag.drop";
            public const string DragCancel = "ui.drag.cancel";
            public const string Transfer = "ui.transfer";
            public const string SlotSelect = "ui.slot";
            public const string ToastGood = "ui.toast.good";
            public const string ToastBad = "ui.toast.bad";
            public const string Autosave = "ui.autosave";
            /// <summary>§12.7: "the failure cue is the refusal cue, not an urgent alert".</summary>
            public const string AutosaveFailed = Refused;
        }
    }
}
