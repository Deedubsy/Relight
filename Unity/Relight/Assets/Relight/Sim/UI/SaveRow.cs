using System;
using System.Collections.Generic;
using System.Globalization;

namespace Relight.Sim.UI
{
    /// <summary>
    /// C-10. One row of the Load screen, reduced to the facts UI_AND_ONBOARDING.md §2.6.2 says a row shows. The
    /// front end builds these from <see cref="SaveSlotInfo"/> and <see cref="AutosaveEntry"/> and hands them to
    /// <see cref="SaveRowFormatter"/>; keeping the shape plain is what lets the whole Load screen's text be tested
    /// without a store, a file system or Unity.
    /// </summary>
    public sealed class SaveRow
    {
        /// <summary>The name a load/delete asks the store for: the manual slot name, or the autosave file name.</summary>
        public string Name = "";

        /// <summary>What the player sees first: "Slot 1", "Autosave 3", "Autosave on quit".</summary>
        public string Label = "";

        public bool IsAutosave;

        /// <summary>ISO-8601 as written into the save (<c>savedAt</c>); "" when the header could not be read.</summary>
        public string SavedAt = "";

        /// <summary>Sim time in seconds. Autosave index entries carry ticks instead — see <see cref="FromTicks"/>.</summary>
        public double T;

        /// <summary>The B-11 wall-clock play accumulator. Zero means the save does not carry one.</summary>
        public double PlaySeconds;

        /// <summary>The map the save was made on; "" when the save does not say (older save, or synthetic map).</summary>
        public string MapId = "";

        /// <summary>Why the header could not be read, when it could not. The row is shown disabled with this text.</summary>
        public string Problem = "";

        /// <summary>The header came from the <c>.bak</c>; the slot still loads, slightly older.</summary>
        public bool FromBackup;

        /// <summary>Write order among autosaves, higher is newer. Only the newest carries the Latest tag.</summary>
        public long Seq;

        /// <summary>Sim seconds from a tick count, for autosave index entries, which carry no <c>t</c>.</summary>
        public static double FromTicks(int tick) => tick * Simulation.TickSeconds;

        /// <summary>
        /// The row's sort key: the ISO <c>savedAt</c>, which sorts correctly as text and is what §2.6.3 says
        /// Continue ranks by. Rows with no readable time sort last.
        /// </summary>
        public DateTime SavedAtUtc
        {
            get
            {
                DateTime parsed;
                if (!string.IsNullOrEmpty(SavedAt) && DateTime.TryParse(
                        SavedAt, CultureInfo.InvariantCulture,
                        DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out parsed))
                    return parsed;
                return DateTime.MinValue;
            }
        }
    }
}
