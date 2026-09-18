using System;
using System.Collections.Generic;
using System.Globalization;

namespace Relight.Sim.UI
{
    /// <summary>
    /// C-10. Turns a <see cref="SaveRow"/> into the exact line UI_AND_ONBOARDING.md §2.6.2 and §2.7.1 specify:
    ///
    /// <code>Slot 1 · Day 4 · 1:12:30 · saved 11 Sep 2026, 20:14</code>
    /// <code>Autosave 3 · Day 4 · 1:12:30 · saved 11 Sep 2026, 20:14</code>
    ///
    /// Rules carried over verbatim:
    /// <list type="bullet">
    /// <item>Day is <c>floor(t / daySeconds) + 1</c> with the catalogue's 1200 s day, the same arithmetic the HUD
    ///       clock uses (<c>StatusPanelViewModel.FormatClock</c>).</item>
    /// <item>Playtime comes from the save's <c>playSeconds</c>; a save without one shows its sim elapsed time and
    ///       says so — "1:12:30 sim time" — rather than quietly presenting a different measurement as the same one.</item>
    /// <item>The map id is shown only when it differs from the running game's.</item>
    /// <item>A row that cannot be loaded is <b>disabled with the reason</b>. It is never hidden and never deleted.</item>
    /// </list>
    /// The map mismatch text is not built here: <see cref="SaveSerializer.MapProblem"/> owns it, so the Load screen
    /// and the store refuse in the same words.
    /// </summary>
    public static class SaveRowFormatter
    {
        /// <summary>The tag on the newest autosave (§2.7.1).</summary>
        public const string LatestTag = "Latest";

        /// <summary>Shown when a row's header could not be read at all and the reader gave no reason.</summary>
        public const string UnreadableText = "this save could not be read";

        /// <summary>Said beside a row recovered from its copy, so "slightly older" is not a surprise.</summary>
        public const string FromBackupText = "recovered from the previous copy";

        /// <summary>§2.6.1 — Continue when there is nothing to continue.</summary>
        public const string NoSaveText = "No saved game yet";

        /// <summary>The day number a save is in (§2.6.2).</summary>
        public static int Day(double t, double daySeconds)
        {
            if (daySeconds <= 0) daySeconds = 1;
            if (t < 0) t = 0;
            return (int)Math.Floor(t / daySeconds) + 1;
        }

        /// <summary>"1:12:30", or "1:12:30 sim time" when the save carries no play accumulator.</summary>
        public static string Playtime(SaveRow row)
        {
            if (row == null) return "";
            if (row.PlaySeconds > 0) return PlayClock.Clock(row.PlaySeconds);
            return PlayClock.Clock(row.T) + " sim time";
        }

        /// <summary>
        /// The ISO <c>savedAt</c> in the player's own time zone, as "11 Sep 2026, 20:14" (§2.6.2). An unparsable
        /// or absent time yields "" and the caller drops the clause rather than printing a placeholder date.
        /// </summary>
        public static string SavedAtText(string iso)
        {
            DateTime parsed;
            if (string.IsNullOrEmpty(iso)) return "";
            if (!DateTime.TryParse(iso, CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out parsed))
                return "";
            return parsed.ToLocalTime().ToString("d MMM yyyy, HH:mm", CultureInfo.CurrentCulture);
        }

        /// <summary>The whole row line. <paramref name="latest"/> appends the newest-autosave tag.</summary>
        public static string Line(SaveRow row, double daySeconds, bool latest = false)
        {
            if (row == null) return "";
            var text = row.Label;
            if (row.Problem.Length != 0 || row.SavedAt.Length == 0)
            {
                // No readable header: there is no Day and no playtime to state. Say only what is true.
                if (latest) text += " · " + LatestTag;
                return text;
            }
            text += " · Day " + Day(row.T, daySeconds).ToString(CultureInfo.InvariantCulture);
            text += " · " + Playtime(row);
            var saved = SavedAtText(row.SavedAt);
            if (saved.Length != 0) text += " · saved " + saved;
            if (latest) text += " · " + LatestTag;
            return text;
        }

        /// <summary>
        /// Why this row cannot be loaded, or "" when it can. The order matters: an unreadable file is reported as
        /// unreadable even if its map also disagrees, because the reader never got far enough to know the map.
        /// </summary>
        public static string DisabledReason(SaveRow row, string currentMapId)
        {
            if (row == null) return UnreadableText;
            if (row.Problem.Length != 0) return row.Problem;
            if (row.SavedAt.Length == 0 && row.T <= 0 && row.PlaySeconds <= 0) return UnreadableText;
            return SaveSerializer.MapProblem(row.MapId, currentMapId) ?? "";
        }

        /// <summary>True when the Load screen may offer Load and Delete on this row.</summary>
        public static bool Loadable(SaveRow row, string currentMapId) => DisabledReason(row, currentMapId).Length == 0;

        /// <summary>
        /// The second line under a row: the map it was made on when that differs (§2.6.2 "shown only when it
        /// differs"), and the recovered-copy note. Empty when there is nothing extra to say.
        /// </summary>
        public static string Detail(SaveRow row, string currentMapId)
        {
            if (row == null) return "";
            var parts = new List<string>(2);
            if (row.MapId.Length != 0 && currentMapId != null && currentMapId.Length != 0
                && !string.Equals(row.MapId, currentMapId, StringComparison.Ordinal))
                parts.Add("map " + row.MapId);
            if (row.FromBackup) parts.Add(FromBackupText);
            return string.Join(" · ", parts);
        }

        /// <summary>"Slot 1" from the store's slot name; the name is the player's own and is shown as written.</summary>
        public static string ManualLabel(string slotName)
            => string.IsNullOrEmpty(slotName) ? "Save" : slotName;

        /// <summary>
        /// "Autosave 3" / "Autosave on quit" from a ring file name (<c>auto-3.json</c>, <c>auto-quit.json</c>),
        /// so the row names the slot the player will actually overwrite or delete.
        /// </summary>
        public static string AutosaveLabel(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return "Autosave";
            var name = fileName;
            if (name.EndsWith(SaveSchema.Extension, StringComparison.Ordinal))
                name = name.Substring(0, name.Length - SaveSchema.Extension.Length);
            if (name.StartsWith(AutosaveStore.SlotPrefix, StringComparison.Ordinal))
                name = name.Substring(AutosaveStore.SlotPrefix.Length);
            if (name == "quit") return "Autosave on quit";
            return name.Length == 0 ? "Autosave" : "Autosave " + name;
        }
    }
}
