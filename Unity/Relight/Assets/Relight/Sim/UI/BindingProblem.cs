using System;
using System.Collections.Generic;

namespace Relight.Sim.UI
{
    /// <summary>
    /// C-10. <c>packages/game/src/controls.ts:bindingProblem</c>, ported message for message
    /// (UI_AND_ONBOARDING.md §3.2). Returns the empty string when the binding is allowed, otherwise the exact
    /// player-facing sentence the settings screen prints under the key-capture field.
    ///
    /// The collision rule is the reference's: three contexts (<c>modified</c>, <c>blueprint</c>, <c>world</c>), a
    /// clash only inside one context, and blueprint transforms additionally clashing with world actions except
    /// <c>chest</c>, <c>tramstop</c> and <c>tram</c> — the three the reference exempts because a blueprint
    /// transform and a build shortcut can never be live at the same moment.
    /// </summary>
    public static class BindingProblem
    {
        /// <summary>A <c>FIXED</c> action, or one this build does not have.</summary>
        public const string FixedText = "Escape and numbered quickbar slots stay fixed; edit slot contents in Build.";

        /// <summary>A key outside the accepted set.</summary>
        public const string KeyText = "Use one letter, arrow, F-key, Shift, Space or supported punctuation.";

        /// <summary>Escape and Tab belong to the interface.</summary>
        public const string ReservedText = "This key is reserved for interface navigation.";

        /// <summary>controls.ts: <c>`Already used by ${other} in this context.`</c></summary>
        public static string UsedText(string other) => "Already used by " + other + " in this context.";

        /// <summary>
        /// Empty when <paramref name="key"/> may be bound to <paramref name="action"/>, otherwise the reason.
        /// <paramref name="overrides"/> is the player's current override map (action name to key), so a collision
        /// is checked against what is really bound, not against the defaults.
        /// </summary>
        public static string Problem(string action, string key, IReadOnlyDictionary<string, string> overrides)
        {
            if (!Bindings.Known(action) || Bindings.IsFixed(action)) return FixedText;

            // Difference from the reference, deliberate: the reserved-key test runs BEFORE the character-set test.
            // In controls.ts it runs after, and "Escape"/"Tab" are multi-character strings that the character-set
            // regex has already rejected, so the reserved message is unreachable there. UI_AND_ONBOARDING.md §3.2
            // lists it as a player-facing message, so the port orders the two checks so it can actually be shown.
            // Every other input produces exactly the reference's answer.
            if (key == "Escape" || key == "Tab") return ReservedText;

            if (!Accepted(key)) return KeyText;

            var names = Bindings.Actions;
            var mine = Bindings.Context(action);
            for (var i = 0; i < names.Count; i++)
            {
                var other = names[i];
                if (string.Equals(other, action, StringComparison.Ordinal)) continue;
                if (!Overlaps(mine, action, Bindings.Context(other), other)) continue;
                var keys = Bindings.Keys(other, overrides);
                for (var k = 0; k < keys.Count; k++)
                    if (string.Equals(keys[k], key, StringComparison.OrdinalIgnoreCase))
                        return UsedText(other);
            }
            return "";
        }

        /// <summary>
        /// The filter that keeps a stored preferences file honest: every entry that passes is applied, every entry
        /// that does not is dropped in silence and the action keeps its default (controls.ts <c>parseBindings</c>,
        /// "Missing and corrupt preferences use safe defaults", uiPreferences.ts:14).
        /// </summary>
        public static Dictionary<string, string> Parse(IEnumerable<KeyValuePair<string, string>> raw)
        {
            var ok = new Dictionary<string, string>(StringComparer.Ordinal);
            if (raw == null) return ok;
            foreach (var entry in raw)
            {
                if (entry.Value == null) continue;
                if (Problem(entry.Key, entry.Value, ok).Length != 0) continue;
                ok[entry.Key] = entry.Value;
            }
            return ok;
        }

        /// <summary>
        /// controls.ts's accepted-key regex, spelled out: one ASCII letter, one of the four arrows, F1-F12,
        /// Shift, Space, or one of <c>- = [ ] ` ; , . /</c>. Digits are absent on purpose — the numbered
        /// quickbar slots are fixed.
        /// </summary>
        public static bool Accepted(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;
            if (key.Length == 1)
            {
                var c = key[0];
                if (c >= 'a' && c <= 'z') return true;
                if (c >= 'A' && c <= 'Z') return true;
                return c == ' ' || c == '-' || c == '=' || c == '[' || c == ']'
                       || c == '`' || c == ';' || c == ',' || c == '.' || c == '/';
            }
            if (key == "ArrowUp" || key == "ArrowDown" || key == "ArrowLeft" || key == "ArrowRight") return true;
            if (key == "Shift") return true;
            if (key.Length >= 2 && key[0] == 'F')
            {
                var n = key.Substring(1);
                if (n.Length == 1 && n[0] >= '1' && n[0] <= '9') return true;
                if (n.Length == 2 && n[0] == '1' && n[1] >= '0' && n[1] <= '2') return true;
            }
            return false;
        }

        private static bool Overlaps(string mine, string action, string theirs, string other)
        {
            if (string.Equals(mine, theirs, StringComparison.Ordinal)) return true;
            if (mine == Bindings.ContextBlueprint && theirs == Bindings.ContextWorld) return !Exempt(other);
            if (theirs == Bindings.ContextBlueprint && mine == Bindings.ContextWorld) return !Exempt(action);
            return false;
        }

        private static bool Exempt(string action)
        {
            var set = Bindings.BlueprintExempt;
            for (var i = 0; i < set.Length; i++)
                if (string.Equals(set[i], action, StringComparison.Ordinal)) return true;
            return false;
        }
    }
}
