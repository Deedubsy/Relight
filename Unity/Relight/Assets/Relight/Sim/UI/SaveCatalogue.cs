using System.Collections.Generic;

namespace Relight.Sim.UI
{
    /// <summary>
    /// C-10. Turns what the store knows into what the Load screen shows: two groups, <b>Manual saves</b> then
    /// <b>Autosaves</b>, each newest first, never interleaved (UI_AND_ONBOARDING.md §2.7.1).
    ///
    /// It reads <see cref="SaveSlotInfo"/> and <see cref="AutosaveEntry"/> — both Relight.Sim types — so the whole
    /// Load screen's contents can be built and asserted without a file system, a store or Unity. Two details are
    /// carried straight from the store and not re-derived here:
    /// <list type="bullet">
    /// <item>a row's refusal is the store's own <c>Problem</c> text, shown verbatim; the map comparison is
    ///       <see cref="SaveSerializer.MapProblem"/>'s, reached through
    ///       <see cref="SaveRowFormatter.DisabledReason"/> — this class never compares map ids itself;</item>
    /// <item>an <see cref="AutosaveEntry"/> carries a tick, not a <c>t</c>, so its sim seconds come from
    ///       <see cref="SaveRow.FromTicks"/> (ticks × <see cref="Simulation.TickSeconds"/>).</item>
    /// </list>
    /// Nothing is ever dropped: a row that cannot be loaded is returned like any other, and the screen shows it
    /// disabled with its reason (§2.6.2, "never silently hidden and never deleted").
    /// </summary>
    public static class SaveCatalogue
    {
        /// <summary>The manual group, newest first.</summary>
        public static List<SaveRow> Manual(IReadOnlyList<SaveSlotInfo> slots)
        {
            var rows = new List<SaveRow>();
            if (slots == null) return rows;
            for (var i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot == null) continue;
                var row = new SaveRow
                {
                    Name = slot.Name ?? "",
                    Label = SaveRowFormatter.ManualLabel(slot.Name),
                    IsAutosave = false,
                    Problem = slot.Problem ?? "",
                    FromBackup = slot.FromBackup,
                };
                var h = slot.Header;
                if (h != null)
                {
                    row.SavedAt = h.SavedAt ?? "";
                    row.T = h.T;
                    row.PlaySeconds = h.PlaySeconds;
                    row.MapId = h.MapId ?? "";
                }
                rows.Add(row);
            }
            rows.Sort(Newest);
            return rows;
        }

        /// <summary>The autosave group, newest first. The newest is the one that carries the Latest tag.</summary>
        public static List<SaveRow> Autosaves(IReadOnlyList<AutosaveEntry> entries)
        {
            var rows = new List<SaveRow>();
            if (entries == null) return rows;
            for (var i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e == null) continue;
                rows.Add(new SaveRow
                {
                    Name = e.File ?? "",
                    Label = SaveRowFormatter.AutosaveLabel(e.File),
                    IsAutosave = true,
                    SavedAt = e.SavedAt ?? "",
                    T = SaveRow.FromTicks(e.Tick),
                    PlaySeconds = e.PlaySeconds,
                    MapId = e.MapId ?? "",
                    Problem = e.Problem ?? "",
                    FromBackup = e.FromBackup,
                    Seq = e.Seq,
                });
            }
            rows.Sort(Newest);
            return rows;
        }

        /// <summary>
        /// Both groups in one list, for <see cref="ContinuePicker"/>, which ranks manual and autosave rows
        /// together by <c>savedAt</c> (§2.6.3).
        /// </summary>
        public static List<SaveRow> All(IReadOnlyList<SaveSlotInfo> slots, IReadOnlyList<AutosaveEntry> entries)
        {
            var rows = Manual(slots);
            rows.AddRange(Autosaves(entries));
            return rows;
        }

        /// <summary>
        /// The row in <paramref name="rows"/> that carries the Latest tag: the newest autosave that a player can
        /// actually load. A broken newest file is not "the latest good save", so the tag steps past it, the same
        /// way Continue does.
        /// </summary>
        public static SaveRow LatestAutosave(IReadOnlyList<SaveRow> rows, string currentMapId)
        {
            if (rows == null) return null;
            SaveRow best = null;
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row == null || !row.IsAutosave) continue;
                if (!SaveRowFormatter.Loadable(row, currentMapId)) continue;
                if (best == null || Newest(row, best) < 0) best = row;
            }
            return best;
        }

        /// <summary>Newest first by <c>savedAt</c>, then by write order, then by label so the order is stable.</summary>
        private static int Newest(SaveRow a, SaveRow b)
        {
            var c = b.SavedAtUtc.CompareTo(a.SavedAtUtc);
            if (c != 0) return c;
            c = b.Seq.CompareTo(a.Seq);
            if (c != 0) return c;
            return string.CompareOrdinal(a.Label, b.Label);
        }
    }
}
