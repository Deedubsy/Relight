using System;
using System.Collections.Generic;

namespace Relight.Sim.UI
{
    /// <summary>
    /// C-10. What Continue picks, UI_AND_ONBOARDING.md §2.6.3: the most recently written <b>valid</b> save of the
    /// current profile, ranked by <c>savedAt</c>, manual and autosave alike. When the newest cannot be opened the
    /// next newest is used and the player is told, in those words. When none can be opened Continue is shown
    /// <b>disabled with the reason</b> — §2.6.1: never hidden.
    /// </summary>
    public static class ContinuePicker
    {
        /// <summary>§2.6.3, said when Continue silently steps past a save that will not open.</summary>
        public static string FellBackText(string label)
            => "Your most recent save could not be opened; continuing from " + label + ".";

        /// <summary>The answer: which row, what to say, and why Continue is disabled when it is.</summary>
        public sealed class Choice
        {
            /// <summary>The row Continue will load, or null when there is nothing to continue.</summary>
            public SaveRow Row;

            /// <summary>The fall-back sentence, or "" when the newest save was the one taken.</summary>
            public string Notice = "";

            /// <summary>Why Continue is disabled, or "" when it is enabled.</summary>
            public string DisabledReason = "";

            public bool Enabled => Row != null;
        }

        /// <summary>
        /// <paramref name="rows"/> is every manual and autosave row, in any order.
        /// <paramref name="failed"/> names rows a load has already been tried on and refused this session, so a
        /// retry steps further down the list instead of offering the same broken file again.
        /// </summary>
        public static Choice Pick(IReadOnlyList<SaveRow> rows, string currentMapId,
                                 IReadOnlyCollection<string> failed = null)
        {
            var choice = new Choice();
            if (rows == null || rows.Count == 0)
            {
                choice.DisabledReason = SaveRowFormatter.NoSaveText;
                return choice;
            }

            var ranked = new List<SaveRow>(rows);
            ranked.Sort(Newest);

            var skipped = false;
            string firstReason = null;
            for (var i = 0; i < ranked.Count; i++)
            {
                var row = ranked[i];
                var reason = SaveRowFormatter.DisabledReason(row, currentMapId);
                if (reason.Length == 0 && failed != null && Contains(failed, row.Name))
                    reason = SaveRowFormatter.UnreadableText;
                if (reason.Length != 0)
                {
                    if (firstReason == null) firstReason = reason;
                    skipped = true;
                    continue;
                }
                choice.Row = row;
                if (skipped) choice.Notice = FellBackText(row.Label);
                return choice;
            }

            choice.DisabledReason = firstReason ?? SaveRowFormatter.NoSaveText;
            return choice;
        }

        /// <summary>Newest first by <c>savedAt</c>; ties break on the label so the order is stable.</summary>
        private static int Newest(SaveRow a, SaveRow b)
        {
            var c = b.SavedAtUtc.CompareTo(a.SavedAtUtc);
            if (c != 0) return c;
            c = b.Seq.CompareTo(a.Seq);
            if (c != 0) return c;
            return string.Compare(a.Label, b.Label, StringComparison.Ordinal);
        }

        private static bool Contains(IReadOnlyCollection<string> set, string name)
        {
            foreach (var s in set)
                if (string.Equals(s, name, StringComparison.Ordinal)) return true;
            return false;
        }
    }
}
