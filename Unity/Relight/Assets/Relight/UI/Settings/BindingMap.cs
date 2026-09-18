using System;
using System.Collections.Generic;
using Relight.Sim.UI;
using UnityEngine.InputSystem;

namespace Relight.UI.Settings
{
    /// <summary>
    /// C-10. Which of the reference's rebindable actions this build actually has, and where each one lives in
    /// <c>RelightControls.inputactions</c>.
    ///
    /// <c>Bindings</c> is the reference's whole table, ported verbatim because <c>BindingProblem</c>'s collision
    /// rule is defined against all of it: a key is refused because some <i>other</i> action already uses it, and
    /// that answer is only right if every action is present. But the settings screen must not <b>offer</b> a
    /// control for a feature that does not exist (UI_AND_ONBOARDING.md §3.8.3: "no control that pretends a feature
    /// exists"), so the dropdown is this list, not <c>Bindings.Rebindable</c>. The two are separate on purpose and
    /// the report records every reference action that is checked but not offered.
    ///
    /// Deliberately absent, each for a stated reason:
    /// <list type="bullet">
    /// <item>everything the port has not built yet — blueprints, the map, the tram;</item>
    /// <item><c>cancel</c> and the ten numbered quickbar slots, which <c>Bindings.Fixed</c> already refuses.</item>
    /// </list>
    ///
    /// The correction pass adds two rows the port now has: <c>build</c> (B, the build menu) and <c>rotate</c>
    /// (R). Both were already in the reference table and neither is <c>Bindings.Fixed</c>, so they were always
    /// legitimately rebindable — there was simply no Unity control behind them until C-02 added <c>ToggleBuild</c>
    /// and made R contextual. R is one Unity action (<c>Reload</c>) doing two jobs, rotate-the-ghost and reload,
    /// which <c>WorldInput</c> resolves; rebinding it moves both, which is why its label says both.
    ///
    /// <see cref="Controls"/> is a different thing from <see cref="Entries"/> and the two must not be confused.
    /// <c>Entries</c> is what the player may CHANGE; <c>Controls</c> is what the settings screen PRINTS, including
    /// the rows that are not rebindable at all (Move is a composite of eight bindings, the action bar is ten). It
    /// also carries THIS build's default for each row, which is not always the reference's: the reference opens
    /// the Backpack with I and offers Tab as the second key, and this build leads with Tab.
    /// </summary>
    public static class BindingMap
    {
        /// <summary>One row: a reference action, and the Unity binding it drives.</summary>
        public sealed class Entry
        {
            public Entry(string action, string map, string unityAction, int binding, string label)
            {
                Action = action;
                Map = map;
                UnityAction = unityAction;
                Binding = binding;
                Label = label;
            }

            /// <summary>The reference's action name — the key into <c>Bindings</c> and <c>BindingProblem</c>.</summary>
            public string Action { get; }

            public string Map { get; }
            public string UnityAction { get; }

            /// <summary>Index into that action's own bindings; a composite part has its own index.</summary>
            public int Binding { get; }

            /// <summary>What the dropdown shows. The reference's action names are not player-facing words.</summary>
            public string Label { get; }
        }

        /// <summary>
        /// In the order the dropdown lists them. The four movement rows address the WASD composite's parts
        /// (indices 1-4); the Arrows composite that follows is untouched, which is exactly
        /// <c>Bindings.Keys</c>'s rule — an override replaces the FIRST default and the rest are kept, so
        /// rebinding "north" keeps ArrowUp, as it does in the reference.
        /// </summary>
        public static readonly Entry[] Entries =
        {
            new Entry("north",    "World",  "Move",        1, "Walk north"),
            new Entry("south",    "World",  "Move",        2, "Walk south"),
            new Entry("west",     "World",  "Move",        3, "Walk west"),
            new Entry("east",     "World",  "Move",        4, "Walk east"),
            new Entry("sprint",   "World",  "Sprint",      0, "Sprint"),
            new Entry("dodge",    "World",  "Dodge",       0, "Dodge"),
            // U-D-56: E interacts and nothing else. The ACTION is still named "Equip" in the .inputactions asset
            // (renaming it would drop every saved rebinding); only the printed label had to stop promising equip.
            new Entry("interact", "World",  "Equip",       0, "Interact"),
            new Entry("pockets",  "Global", "TogglePanel", 0, "Backpack"),
            new Entry("build",    "Global", "ToggleBuild", 0, "Build menu"),
            new Entry("rotate",   "Global", "Reload",      0, "Rotate ghost / Reload"),
            new Entry("pause",    "Global", "Pause",       0, "Pause"),
        };

        /// <summary>One printed row of the controls list: what it is called and what it is bound to.</summary>
        public sealed class Row
        {
            public Row(string label, string action, string keys)
            {
                Label = label;
                Action = action;
                Keys = keys;
            }

            /// <summary>The player-facing name of the control.</summary>
            public string Label { get; }

            /// <summary>The reference action whose override renames this row, or "" when it cannot be rebound.</summary>
            public string Action { get; }

            /// <summary>What THIS build binds by default — the text shown when there is no stored override.</summary>
            public string Keys { get; }
        }

        /// <summary>
        /// The controls the settings screen prints, in the order it prints them. Ten rows, exactly the set the
        /// correction pass asked for; the two with no <see cref="Row.Action"/> are the ones a player cannot
        /// rebind here (Move is a pair of composites, the action bar is ten fixed digits per
        /// <c>Bindings.Fixed</c>).
        /// </summary>
        public static readonly Row[] Controls =
        {
            new Row("Backpack",              "pockets",  "Tab"),
            new Row("Build menu",            "build",    "B"),
            new Row("Rotate ghost / Reload", "rotate",   "R"),
            new Row("Cancel",                "",         "Esc"),
            new Row("Pause",                 "pause",    "P"),
            new Row("Move",                  "",         "WASD / arrows"),
            new Row("Sprint",                "sprint",   "Shift"),
            new Row("Dodge",                 "dodge",    "Space"),
            new Row("Equip",                 "interact", "E"),
            new Row("Action bar",            "",         "1-9, 0"),
        };

        /// <summary>
        /// What one row is bound to right now: the player's stored key if they rebound it, else this build's
        /// default. A stored map with no entry for a row therefore reads as the default rather than as blank,
        /// which is what lets a new action appear without touching anybody's saved bindings.
        /// </summary>
        public static string KeyText(Row row, IReadOnlyDictionary<string, string> overrides)
        {
            if (row == null) return "";
            if (string.IsNullOrEmpty(row.Action) || overrides == null) return row.Keys;
            if (!overrides.TryGetValue(row.Action, out var key) || string.IsNullOrEmpty(key)) return row.Keys;
            var shown = KeyNames.Display(key);
            return string.IsNullOrEmpty(shown) ? row.Keys : shown;
        }

        /// <summary>
        /// The stored rebindings as the settings screen reads them (<see cref="Preferences.BindingKeys"/>,
        /// "action=key;..."; "Space" spelt out), keeping only known actions and accepted keys. Shared by the
        /// settings screen and the HUD's <c>Inventory [Tab]</c> / <c>Build [B]</c> labels so both print the same key.
        /// </summary>
        public static Dictionary<string, string> StoredOverrides()
        {
            var map = new Dictionary<string, string>();
            var raw = Preferences.BindingKeys;
            if (string.IsNullOrEmpty(raw)) return map;
            foreach (var pair in raw.Split(';'))
            {
                if (pair.Length == 0) continue;
                var split = pair.IndexOf('=');
                if (split <= 0) continue;
                var action = pair.Substring(0, split);
                var key = pair.Substring(split + 1);
                if (key == "Space") key = " ";
                if (Bindings.Known(action) && BindingProblem.Accepted(key)) map[action] = key;
            }
            return map;
        }

        /// <summary>The key text for one control row by its action id ("pockets", "build"), default when unbound.</summary>
        public static string KeyTextFor(string action, IReadOnlyDictionary<string, string> overrides)
        {
            for (var i = 0; i < Controls.Length; i++)
                if (Controls[i].Action == action) return KeyText(Controls[i], overrides);
            return "";
        }

        /// <summary>The printed list, one "Label — Key" line per row, in <see cref="Controls"/> order.</summary>
        public static List<string> ControlLines(IReadOnlyDictionary<string, string> overrides)
        {
            var list = new List<string>(Controls.Length);
            for (var i = 0; i < Controls.Length; i++)
                list.Add(Controls[i].Label + " — " + KeyText(Controls[i], overrides));
            return list;
        }

        public static Entry Find(string action)
        {
            for (var i = 0; i < Entries.Length; i++)
                if (string.Equals(Entries[i].Action, action, StringComparison.Ordinal)) return Entries[i];
            return null;
        }

        /// <summary>The labels, in order, for the dropdown.</summary>
        public static List<string> Labels()
        {
            var list = new List<string>(Entries.Length);
            for (var i = 0; i < Entries.Length; i++) list.Add(Entries[i].Label);
            return list;
        }

        /// <summary>
        /// Apply one accepted key to the asset. Returns "" or the reason it could not be applied — a reason that
        /// is about Unity (a missing map or action), never about the binding rules, which
        /// <see cref="BindingProblem"/> has already answered.
        /// </summary>
        public static string Apply(InputActionAsset asset, Entry entry, string key)
        {
            if (asset == null || entry == null) return "the control asset is missing";
            var path = KeyNames.Path(key);
            if (path == null) return BindingProblem.KeyText;
            var map = asset.FindActionMap(entry.Map, false);
            var action = map?.FindAction(entry.UnityAction, false);
            if (action == null) return "this build has no '" + entry.UnityAction + "' control to rebind";
            if (entry.Binding < 0 || entry.Binding >= action.bindings.Count)
                return "this build's '" + entry.UnityAction + "' control has changed shape";
            action.ApplyBindingOverride(entry.Binding, path);
            return "";
        }
    }
}
