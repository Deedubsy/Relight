using System;
using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>
    /// REL-55 (UI-05): the one place a machine or weapon kind becomes a name the player reads. Nine call sites used
    /// to fall back to the raw kind when the catalogue had no row ("tramstop", "rifle"), so a tram stop's hover or a
    /// weapon slot could show an identifier. A kind the catalogue carries reads as its display name; the three
    /// reference kinds the catalogue leaves out as not player-buildable (CONTENT_CATALOGUE.md §17.2) read as the
    /// reference's own labels (flow.ts <c>KIND_LABEL</c>); anything else reads as words, never as the key.
    /// </summary>
    public static class PlayerNames
    {
        /// <summary>Reference flow.ts <c>KIND_LABEL</c> for the kinds the Unity catalogue does not export.</summary>
        private static readonly Dictionary<string, string> Excluded = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "track", "Track" }, { "tramstop", "Tram stop" }, { "tram", "Tram" },
        };

        /// <summary>A machine kind's player-facing name ("Assembler Mk2", "Tram stop").</summary>
        public static string Machine(GameData d, string kind)
        {
            if (d != null && kind != null && d.TryMachine(kind, out var spec) && !string.IsNullOrEmpty(spec.DisplayName))
                return spec.DisplayName;
            return kind != null && Excluded.TryGetValue(kind, out var label) ? label : Words(kind);
        }

        /// <summary>A weapon kind's player-facing name; an instance key ("rifle:3") reads as its kind.</summary>
        public static string Weapon(GameData d, string kind)
        {
            var bare = Bare(kind);
            return d != null && d.TryWeapon(bare, out var profile) && !string.IsNullOrEmpty(profile.DisplayName)
                ? profile.DisplayName : Words(bare);
        }

        /// <summary>
        /// The last resort: a key as words ("hand-crank" → "Hand crank", "rifle:3" → "Rifle"), so an unknown kind
        /// still reads as a name. It is the reference itemName's fallback (capitalise the key) with the separators
        /// spelt out. Empty reads "Unknown".
        /// </summary>
        public static string Words(string key)
        {
            var bare = Bare(key).Replace('-', ' ').Replace('_', ' ').Trim();
            if (bare.Length == 0) return "Unknown";
            return char.ToUpperInvariant(bare[0]) + bare.Substring(1);
        }

        private static string Bare(string key)
        {
            if (string.IsNullOrEmpty(key)) return "";
            var colon = key.IndexOf(':');
            return colon > 0 ? key.Substring(0, colon) : key;
        }
    }
}
