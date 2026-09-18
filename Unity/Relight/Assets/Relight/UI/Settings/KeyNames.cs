using System;
using System.Collections.Generic;
using UnityEngine;

namespace Relight.UI.Settings
{
    /// <summary>
    /// C-10. The translation layer between the reference's key vocabulary — a single character, an <c>Arrow*</c>
    /// name, <c>F1</c>-<c>F12</c>, <c>Shift</c>, <c>" "</c> for Space — and the Input System's control paths
    /// (<c>&lt;Keyboard&gt;/w</c>).
    ///
    /// It is here, in Relight.UI, and not beside <c>Bindings</c> in Relight.Sim, on purpose: the validated,
    /// player-facing rules are engine-free and tested (<c>Bindings</c>, <c>BindingProblem</c>); only this
    /// spelling table needs Unity, and it holds no rule of its own.
    /// </summary>
    public static class KeyNames
    {
        private static readonly Dictionary<string, string> Paths = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "ArrowUp", "upArrow" }, { "ArrowDown", "downArrow" },
            { "ArrowLeft", "leftArrow" }, { "ArrowRight", "rightArrow" },
            // The reference has one "Shift"; a keyboard has two. Left is the one its own default (sprint) means.
            { "Shift", "leftShift" },
            { " ", "space" },
            { "-", "minus" }, { "=", "equals" }, { "[", "leftBracket" }, { "]", "rightBracket" },
            { "`", "backquote" }, { ";", "semicolon" }, { ",", "comma" }, { ".", "period" }, { "/", "slash" },
        };

        private static readonly Dictionary<KeyCode, string> FromCode = new Dictionary<KeyCode, string>
        {
            { KeyCode.UpArrow, "ArrowUp" }, { KeyCode.DownArrow, "ArrowDown" },
            { KeyCode.LeftArrow, "ArrowLeft" }, { KeyCode.RightArrow, "ArrowRight" },
            { KeyCode.LeftShift, "Shift" }, { KeyCode.RightShift, "Shift" },
            { KeyCode.Space, " " },
            { KeyCode.Minus, "-" }, { KeyCode.Equals, "=" },
            { KeyCode.LeftBracket, "[" }, { KeyCode.RightBracket, "]" },
            { KeyCode.BackQuote, "`" }, { KeyCode.Semicolon, ";" },
            { KeyCode.Comma, "," }, { KeyCode.Period, "." }, { KeyCode.Slash, "/" },
            { KeyCode.Escape, "Escape" }, { KeyCode.Tab, "Tab" },
        };

        /// <summary>
        /// The Input System path for a reference key name, or null when there is none. Note that Escape and Tab
        /// resolve to a path perfectly well; it is <c>BindingProblem</c> that refuses them, so the refusal is one
        /// tested sentence rather than a silent gap in this table.
        /// </summary>
        public static string Path(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (Paths.TryGetValue(key, out var named)) return "<Keyboard>/" + named;
            if (key.Length == 1)
            {
                var c = char.ToLowerInvariant(key[0]);
                if (c >= 'a' && c <= 'z') return "<Keyboard>/" + c;
                if (c >= '0' && c <= '9') return "<Keyboard>/" + c;
                return null;
            }
            if (key == "Escape") return "<Keyboard>/escape";
            if (key == "Tab") return "<Keyboard>/tab";
            if (key.Length >= 2 && key[0] == 'F')
            {
                var n = key.Substring(1);
                if (int.TryParse(n, out var number) && number >= 1 && number <= 12)
                    return "<Keyboard>/f" + number.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
            return null;
        }

        /// <summary>
        /// How a reference key name is PRINTED to the player. The vocabulary is a storage format, not English:
        /// <c>" "</c> is Space, <c>Escape</c> is the key everyone calls Esc, and a letter is shown capitalised
        /// even though it is stored lower case. Returns "" for a key with no name at all, so a caller can fall
        /// back to its own default rather than print a blank.
        /// </summary>
        public static string Display(string key)
        {
            if (string.IsNullOrEmpty(key)) return "";
            if (key == " ") return "Space";
            if (key == "Escape") return "Esc";
            if (Printed.TryGetValue(key, out var pretty)) return pretty;
            return key.Length == 1 ? key.ToUpperInvariant() : key;
        }

        private static readonly Dictionary<string, string> Printed = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "ArrowUp", "Up arrow" }, { "ArrowDown", "Down arrow" },
            { "ArrowLeft", "Left arrow" }, { "ArrowRight", "Right arrow" },
        };

        /// <summary>
        /// The reference key name for a captured key press, or "" when the key has no name in that vocabulary.
        /// <paramref name="character"/> is the event's typed character, which is what identifies a letter or a
        /// punctuation mark reliably across layouts; the key code answers for the named keys.
        /// </summary>
        public static string FromKey(KeyCode code, char character)
        {
            if (FromCode.TryGetValue(code, out var named)) return named;
            if (code >= KeyCode.F1 && code <= KeyCode.F12)
                return "F" + ((int)code - (int)KeyCode.F1 + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (code >= KeyCode.A && code <= KeyCode.Z)
                return ((char)('a' + (code - KeyCode.A))).ToString();
            if (character != '\0')
            {
                var c = char.ToLowerInvariant(character);
                if (c >= 'a' && c <= 'z') return c.ToString();
                if (c == ' ' || c == '-' || c == '=' || c == '[' || c == ']'
                    || c == '`' || c == ';' || c == ',' || c == '.' || c == '/')
                    return c.ToString();
            }
            return "";
        }
    }
}
