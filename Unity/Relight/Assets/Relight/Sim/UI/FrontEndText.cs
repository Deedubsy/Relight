using System;
using System.Globalization;

namespace Relight.Sim.UI
{
    /// <summary>
    /// C-10. The front end's and the settings screen's sentences. UI_AND_ONBOARDING.md §2.6-§2.7 and §3.8 fix
    /// almost all of them; the rest come from <c>packages/game/src/settingsPanel.ts</c>.
    /// </summary>
    public static class FrontEndText
    {
        // ---- Title screen (§2.6.1) -------------------------------------------------------------------------

        public const string Continue = "Continue";
        public const string NewGame = "New Game";
        public const string Load = "Load";
        public const string Settings = "Settings";
        public const string Quit = "Quit";

        /// <summary>§2.6.1 — the New Game screen's read-only line. There is one ruleset and no difficulty choice.</summary>
        public const string RulesetLine = "Standard ruleset · fixed 1× speed with pause";

        public const string Start = "Start";
        public const string Back = "Back";
        public const string SeedLabel = "Seed";

        /// <summary>§2.6.1 — the Load screen's two groups, in this order, newest first inside each.</summary>
        public const string ManualGroup = "Manual saves";
        public const string AutosaveGroup = "Autosaves";
        public const string NoSavesYet = "No saved games yet.";

        /// <summary>§2.6.5 — the in-game route back to the title.</summary>
        public const string SaveAndExit = "Save and exit to title";

        // ---- Pause menu (§2.6.5, §2.2) ---------------------------------------------------------------------

        /// <summary>The pause dialog's heading — UI_AND_ONBOARDING.md:90, `&lt;h1&gt;Paused&lt;/h1&gt;`.</summary>
        public const string Paused = "Paused";

        public const string Resume = "Resume";
        public const string Save = "Save";

        // ---- The five confirmations (§2.6.4) ---------------------------------------------------------------

        public static Confirmation StartNewCity(string lastSaveTime)
            => new Confirmation(
                "Start a new city?",
                string.IsNullOrEmpty(lastSaveTime)
                    ? "This session has never been saved. Starting a new city discards it."
                    : "This session has unsaved progress since " + lastSaveTime + ". Starting a new city discards it.",
                "Save and start", "Start without saving", "Cancel");

        public static Confirmation LoadThisSave(string lastSaveTime)
            => new Confirmation(
                "Load this save?",
                string.IsNullOrEmpty(lastSaveTime)
                    ? "Your current session has never been saved."
                    : "Your current session has unsaved progress since " + lastSaveTime + ".",
                "Save and load", "Load without saving", "Cancel");

        public static Confirmation Overwrite(string slotLabel, int day, string savedTime)
            => new Confirmation(
                "Overwrite this save?",
                slotLabel + " holds Day " + day.ToString(CultureInfo.InvariantCulture)
                    + ", saved " + savedTime + ". It will be replaced.",
                "Overwrite", "Cancel");

        public static Confirmation Delete(string slotLabel, int day, string savedTime)
            => new Confirmation(
                "Delete this save?",
                slotLabel + " · Day " + day.ToString(CultureInfo.InvariantCulture)
                    + " · saved " + savedTime + ". This cannot be undone.",
                "Delete", "Cancel");

        public static Confirmation QuitGame(string lastSaveTime)
            => new Confirmation(
                "Quit?",
                string.IsNullOrEmpty(lastSaveTime)
                    ? "This session has never been saved. Quitting discards it."
                    : "This session has unsaved progress since " + lastSaveTime + ". Quitting discards it.",
                "Save and quit", "Quit without saving", "Cancel");

        // ---- Save failure (§2.7.3) -------------------------------------------------------------------------

        /// <summary>
        /// §2.7.3 — a manual save that fails is a modal, not a notice, and it ends by saying the thing the player
        /// is actually afraid of: the previous save is still there.
        /// </summary>
        public static Confirmation CouldNotSave(string reason)
            => new Confirmation(
                "Could not save",
                Sentence(reason) + " Your previous save is unchanged.",
                "Retry", "Choose another slot", "Continue playing");

        /// <summary>§2.7.3 — an autosave failure is a transient notice and the rotation is not advanced.</summary>
        public const string AutosaveFailed = "Autosave failed. Your previous saves are unchanged.";

        /// <summary>§2.7.3 — after three consecutive failures, a persistent strip.</summary>
        public const string AutosaveStopped =
            "Autosaving is not working. Save manually to protect your progress.";

        /// <summary>§2.7.1 — the transient notice after a successful autosave, e.g. "Autosaved · Day 4, 1:12:30."</summary>
        public static string Autosaved(int day, string playtime)
            => "Autosaved · Day " + day.ToString(CultureInfo.InvariantCulture) + ", " + playtime + ".";

        // ---- Settings (§3.8, settingsPanel.ts) -------------------------------------------------------------

        public const string SectionInterface = "Interface";
        public const string SectionAudio = "Audio";
        public const string SectionSaving = "Saving";
        public const string SectionBindings = "Keyboard bindings";
        public const string SectionRestore = "Restore and recover";

        /// <summary>settingsPanel.ts — the read-only key-capture field's placeholder.</summary>
        public const string PressAKey = "Focus here and press a key";

        public const string ApplyBinding = "Apply binding";
        public const string BindingApplied = "Binding applied. Release held keys before returning to the city.";
        public const string RestoreDefaults = "Restore default bindings";
        public const string DefaultsRestored = "Default bindings restored.";
        public const string RestoreQuickbar = "Restore default quickbar";

        /// <summary>settingsPanel.ts, the paragraph under the bindings heading.</summary>
        public const string BindingsNote =
            "Ctrl / Cmd shortcuts and blueprint transforms keep their contexts. Escape, Tab navigation and numbered "
            + "quickbar slots remain fixed; assign slot contents in Build.";

        /// <summary>
        /// settingsPanel.ts says "Settings saved for this browser." / "Settings work for this session. Browser
        /// storage is unavailable; retry saving when storage is available." There is no browser here: preferences
        /// go to <c>PlayerPrefs</c>. The two states and the promise they make — the setting is live either way —
        /// are kept; only the word "browser" is replaced.
        /// </summary>
        public const string SettingsSaved = "Settings saved.";

        public const string SettingsSessionOnly =
            "Settings work for this session. They could not be written to disk; retry saving when that is fixed.";

        public const string RetrySaveSettings = "Retry saving settings";

        /// <summary>settingsPanel.ts: <c>`Current: ${…}`</c>, with the Ctrl / Cmd prefix for modified actions.</summary>
        public static string CurrentBinding(string action, System.Collections.Generic.IReadOnlyList<string> keys)
        {
            var prefix = Bindings.IsModified(action) ? "Ctrl / Cmd + " : "";
            if (keys == null || keys.Count == 0) return "Current: " + prefix + "Unassigned";
            var parts = new string[keys.Count];
            for (var i = 0; i < keys.Count; i++) parts[i] = keys[i] == " " ? "Space" : keys[i];
            return "Current: " + prefix + string.Join(" / ", parts);
        }

        // ---- Interface section (§3.8, settingsPanel.ts:12-17) ----------------------------------------------

        public const string InterfaceScale = "Interface scale";
        public const string InterfaceMotion = "Interface motion";

        /// <summary>settingsPanel.ts:12 — the three offered scales, as percentages.</summary>
        public static readonly int[] Scales = { 100, 125, 150 };

        /// <summary>settingsPanel.ts:13-14, verbatim and in order.</summary>
        public const string MotionSystem = "Follow system preference";
        public const string MotionReduce = "Reduce motion";
        public const string MotionFull = "Full interface motion";

        /// <summary>
        /// settingsPanel.ts:15-17. The reference's second sentence is "Settings are shared by this browser; game
        /// saves keep their own profile."; there is no browser here, so it names the machine instead. The promise
        /// — preferences are not part of a save — is the point and is unchanged (§3.3).
        /// </summary>
        public const string InterfaceNote =
            "UI scale changes text and controls independently of camera zoom. Settings are shared by this "
            + "computer; game saves keep their own profile.";

        /// <summary>"125 %" for a scale dropdown entry.</summary>
        public static string ScaleLabel(int percent)
            => percent.ToString(CultureInfo.InvariantCulture) + " %";

        // ---- Saving section (§2.7.2) -----------------------------------------------------------------------

        public const string AutosaveLabel = "Autosave";
        public const string AutosavesKeptLabel = "Autosaves kept";

        // ---- Restore and recover (§3.8, settingsPanel.ts:24-26) ---------------------------------------------

        public const string RestoreOpeningHint = "Restore opening hint for new cities";

        // ---- Load / Save screens (§2.6.1, §2.7.1) -----------------------------------------------------------

        /// <summary>The only two actions a Load row offers (§2.7.1).</summary>
        public const string LoadRowAction = "Load";
        public const string DeleteRowAction = "Delete";

        /// <summary>§2.6.4 — "Deleting is never offered for the save currently loaded in a running session."</summary>
        public const string DeleteLoadedBlocked = "This save is the one this session is running; it cannot be deleted.";

        /// <summary>The in-game Save screen's name field and its button.</summary>
        public const string SaveSlotNameLabel = "Save name";
        public const string SaveIntoSlot = "Save";
        public const string NewSaveRow = "New save…";

        /// <summary>Said after a manual save succeeds, so success is stated and not only implied.</summary>
        public static string SavedTo(string label) => "Saved to " + label + ".";

        // ---- Audio (§3.8.1, and the brief's bus names) ------------------------------------------------------

        public const string DisplayHeading = "Display";
        public const string Brightness = "Brightness in the dark";

        public const string MasterVolume = "Master volume";
        public const string MuteAll = "Mute all";
        public const string UiVolume = "Interface";
        public const string WorldVolume = "Effects";
        public const string AlertsVolume = "Alerts";

        /// <summary>A slider's value read out as text, so it is not signalled by position alone (§3.8.1).</summary>
        public static string Percent(float value01)
            => ((int)Math.Round(value01 * 100f)).ToString(CultureInfo.InvariantCulture) + " %";

        /// <summary>Ends <paramref name="reason"/> with a full stop without doubling one it already has.</summary>
        private static string Sentence(string reason)
        {
            if (string.IsNullOrEmpty(reason)) return "The save could not be written.";
            var text = reason.Trim();
            if (text.Length == 0) return "The save could not be written.";
            var last = text[text.Length - 1];
            if (last != '.' && last != '!' && last != '?') text += ".";
            return char.ToUpperInvariant(text[0]) + text.Substring(1);
        }
    }
}
