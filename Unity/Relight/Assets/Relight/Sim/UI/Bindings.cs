using System;
using System.Collections.Generic;

namespace Relight.Sim.UI
{
    /// <summary>
    /// C-10. The keyboard binding table, ported verbatim from <c>packages/game/src/controls.ts</c>
    /// (<c>DEFAULT_BINDINGS</c>, <c>MODIFIED</c>, <c>FIXED</c>, <c>context</c>, <c>shortcut</c>).
    ///
    /// This lives in Relight.Sim (engine-free) so it is unit-testable: it holds no Unity type and knows nothing
    /// about the Input System. Key names are the reference's own vocabulary — a single character, an
    /// <c>Arrow*</c> name, <c>F1</c>-<c>F12</c>, <c>Shift</c> or <c>" "</c> for Space. The Unity settings screen
    /// translates between <c>UnityEngine.InputSystem.Key</c> and these spellings, so the validated, player-facing
    /// rules stay in one tested place (UI_AND_ONBOARDING.md §3.1-§3.2).
    ///
    /// Two reference actions are deliberately absent: <c>slower</c> and <c>faster</c>. Play is fixed at 1x with
    /// pause (U-D-04) and the port must not carry a control for a feature that does not exist. Both were already
    /// unbound and <c>FIXED</c> in the reference, so <see cref="BindingProblem.Problem"/> still answers for them
    /// with the same message an unknown action gets.
    /// </summary>
    public static class Bindings
    {
        /// <summary>Ctrl / Cmd shortcuts. They collide only with each other (controls.ts <c>context</c>).</summary>
        public const string ContextModified = "modified";

        /// <summary>Blueprint transforms. They collide with each other and with most world actions.</summary>
        public const string ContextBlueprint = "blueprint";

        /// <summary>Everything else.</summary>
        public const string ContextWorld = "world";

        /// <summary>
        /// The default table in the reference's declaration order. Order is load-bearing: the duplicate check
        /// reports the <b>first</b> colliding action, so a reordering would change a player-facing message.
        /// </summary>
        private static readonly KeyValuePair<string, string[]>[] Table =
        {
            Pair("copy", "c"), Pair("paste", "v"), Pair("mirrorX", "h"), Pair("mirrorY", "v"),
            Pair("north", "w", "ArrowUp"), Pair("west", "a", "ArrowLeft"),
            Pair("south", "s", "ArrowDown"), Pair("east", "d", "ArrowRight"),
            Pair("sprint", "Shift"), Pair("dodge", " "), Pair("pause", "p"), Pair("map", "m"),
            Pair("threat", "g"), Pair("engineer", "k"), Pair("pockets", "i", "Tab"), Pair("build", "b"),
            Pair("debug", "`"), Pair("projects", "F2"), Pair("cancel", "Escape"), Pair("pipette", "q"),
            Pair("rotate", "r"), Pair("recipe", "t"), Pair("interact", "e"), Pair("inspect", "f"),
            Pair("abort", "x"), Pair("save", "s"), Pair("load", "o"), Pair("undo", "z"), Pair("redo", "y"),
            Pair("belt", "1"), Pair("inserter", "2"), Pair("excavator", "3"), Pair("assembler", "4"),
            Pair("turret", "5"), Pair("lamp", "6"), Pair("pole", "7"), Pair("generator", "8"),
            Pair("rifle", "9"), Pair("floodlight", "0"), Pair("bigpole", "["), Pair("substation", "]"),
            Pair("chest", "c"), Pair("track", "l"), Pair("tramstop", "h"), Pair("tram", "v"),
            Pair("arclamp"), Pair("wall"), Pair("mixer"), Pair("barricade"),
            Pair("underground", "u"), Pair("splitter", "j"),
        };

        private static readonly Dictionary<string, string[]> Lookup = Build();

        /// <summary>Actions carrying Ctrl / Cmd (controls.ts <c>MODIFIED</c>).</summary>
        public static readonly string[] Modified = { "copy", "paste", "save", "load", "undo", "redo" };

        /// <summary>
        /// Actions a player may not rebind (controls.ts <c>FIXED</c>), minus <c>slower</c>/<c>faster</c>, which the
        /// port does not have at all. Escape and the ten numbered quickbar slots are the whole of it.
        /// </summary>
        public static readonly string[] Fixed =
        {
            "cancel", "belt", "inserter", "excavator", "assembler", "turret",
            "lamp", "pole", "generator", "rifle", "floodlight",
        };

        /// <summary>
        /// The three actions a blueprint transform is allowed to share a key with, because they can never be
        /// pressed in the same context (controls.ts: <c>['chest','tramstop','tram']</c>).
        /// </summary>
        public static readonly string[] BlueprintExempt = { "chest", "tramstop", "tram" };

        /// <summary>Every action name, in declaration order.</summary>
        public static IReadOnlyList<string> Actions
        {
            get
            {
                var names = new string[Table.Length];
                for (var i = 0; i < Table.Length; i++) names[i] = Table[i].Key;
                return names;
            }
        }

        /// <summary>Every action a player may rebind, in declaration order (the settings dropdown's contents).</summary>
        public static IReadOnlyList<string> Rebindable
        {
            get
            {
                var names = new List<string>(Table.Length);
                for (var i = 0; i < Table.Length; i++)
                    if (!IsFixed(Table[i].Key)) names.Add(Table[i].Key);
                return names;
            }
        }

        public static bool Known(string action) => action != null && Lookup.ContainsKey(action);

        public static bool IsFixed(string action) => Contains(Fixed, action);

        public static bool IsModified(string action) => Contains(Modified, action);

        /// <summary>controls.ts <c>context</c>.</summary>
        public static string Context(string action)
        {
            if (IsModified(action)) return ContextModified;
            if (action == "mirrorX" || action == "mirrorY") return ContextBlueprint;
            return ContextWorld;
        }

        /// <summary>The default keys of one action; empty when it has none.</summary>
        public static IReadOnlyList<string> Defaults(string action)
            => action != null && Lookup.TryGetValue(action, out var keys) ? keys : Array.Empty<string>();

        /// <summary>
        /// The live keys of one action: the override replaces the <b>first</b> default and the rest are kept
        /// (controls.ts <c>keys</c> inside <c>bindingProblem</c>, and <c>applyBindings</c>). So overriding
        /// <c>north</c> keeps ArrowUp, exactly as the reference does.
        /// </summary>
        public static IReadOnlyList<string> Keys(string action, IReadOnlyDictionary<string, string> overrides)
        {
            var defaults = Defaults(action);
            var live = new List<string>(defaults.Count + 1);
            string first = null;
            if (overrides != null && action != null && overrides.TryGetValue(action, out var over)) first = over;
            if (string.IsNullOrEmpty(first) && defaults.Count > 0) first = defaults[0];
            if (!string.IsNullOrEmpty(first)) live.Add(first);
            for (var i = 1; i < defaults.Count; i++)
                if (!string.IsNullOrEmpty(defaults[i])) live.Add(defaults[i]);
            return live;
        }

        /// <summary>controls.ts <c>shortcut</c> — what a button label prints, e.g. "R", "Space", "".</summary>
        public static string Shortcut(string action, IReadOnlyDictionary<string, string> overrides = null)
        {
            var keys = Keys(action, overrides);
            if (keys.Count == 0) return "";
            return keys[0] == " " ? "Space" : keys[0].ToUpperInvariant();
        }

        private static KeyValuePair<string, string[]> Pair(string action, params string[] keys)
            => new KeyValuePair<string, string[]>(action, keys ?? Array.Empty<string>());

        private static Dictionary<string, string[]> Build()
        {
            var map = new Dictionary<string, string[]>(Table.Length, StringComparer.Ordinal);
            for (var i = 0; i < Table.Length; i++) map[Table[i].Key] = Table[i].Value;
            return map;
        }

        private static bool Contains(string[] set, string value)
        {
            for (var i = 0; i < set.Length; i++)
                if (string.Equals(set[i], value, StringComparison.Ordinal)) return true;
            return false;
        }
    }
}
