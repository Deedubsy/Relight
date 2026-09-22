using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Relight.Data;
using Relight.Sim;
using UnityEditor;
using UnityEngine;

namespace Relight.Authoring.Tests
{
    /// <summary>
    /// REL-55 (UI-05). The sim tests read the catalogue the offline build compiles (PlayerTextTests); these read
    /// what the game actually loads, the data assets, and the text written straight into UI code and UXML, which no
    /// sim test reaches. Two rules: no mojibake (a UTF-8 "·" read back as Latin-1 is "Â·"), and U-D-68 (c),
    /// "Ammunition is 'rounds' in all player text, never 'bullets'". The saved keys keep the old word and are
    /// allowed: they are identifiers, never shown.
    /// </summary>
    public sealed class PlayerTextSourceTests
    {
        const string RegistryPath = "Assets/Relight/Data/GameDataRegistry.asset";
        static readonly string[] Mojibake = { "\u00C2", "\u00C3", "\u00E2\u20AC", "\uFFFD" };
        static readonly HashSet<string> Keys = new HashSet<string> { "bullet", "bullet-batch", "hand-bullets" };
        static readonly string[] Folders = { "Sim", "UI", "Presentation", "Data" };

        static string Root => Path.Combine(Application.dataPath, "Relight");

        [Test] public void EveryDataPathTheGameLoadsNamesThingsInWords()
        {
            var registry = AssetDatabase.LoadAssetAtPath<GameDataRegistry>(RegistryPath);
            Assert.That(registry, Is.Not.Null);
            foreach (var d in new[] { registry.Build(), registry.BuildLegacy() })
            {
                Assert.That(d.Item(ItemId.Magazine).DisplayName, Is.EqualTo("Rounds"), "the Item asset, not only the catalogue");
                var rows = new List<(string Key, string Name)>();
                foreach (var r in d.Items) rows.Add((r.Key, r.DisplayName));
                foreach (var r in d.Machines) rows.Add((r.Key, r.DisplayName));
                foreach (var r in d.Recipes) rows.Add((r.Key, r.DisplayName));
                foreach (var r in d.Weapons) rows.Add((r.Key, r.DisplayName));
                foreach (var r in d.Enemies) rows.Add((r.Key, r.DisplayName));
                foreach (var r in d.Ammunition) rows.Add((r.Key, r.DisplayName));
                foreach (var r in d.Turrets) rows.Add((r.Key, r.DisplayName));
                foreach (var (key, name) in rows)
                {
                    Assert.That(name, Is.Not.Null.And.Not.Empty, key);
                    Assert.That(name, Is.Not.EqualTo(key), key + " shows its raw key");
                    Assert.That(Offence(name), Is.Null, key + ": \"" + name + "\"");
                }
            }
        }

        [Test] public void NoUiOrSimSourceCarriesMojibake()
        {
            var bad = new List<string>();
            foreach (var file in Files("*.cs", "*.uxml", "*.uss"))
            {
                var text = File.ReadAllText(file, Encoding.UTF8);
                foreach (var m in Mojibake)
                    if (text.IndexOf(m, StringComparison.Ordinal) >= 0) bad.Add(Rel(file));
            }
            Assert.That(bad, Is.Empty);
        }

        [Test] public void NoStringLiteralOrUxmlTextSaysBullets()
        {
            var bad = new List<string>();
            foreach (var file in Files("*.cs"))
            {
                var lines = File.ReadAllLines(file, Encoding.UTF8);
                for (var i = 0; i < lines.Length; i++)
                    foreach (var lit in Literals(lines[i]))
                        if (!Keys.Contains(lit) && lit.ToLowerInvariant().Contains("bullet"))
                            bad.Add(Rel(file) + ":" + (i + 1) + " \"" + lit + "\"");
            }
            var attr = new Regex("(?:text|tooltip|label)=\"([^\"]*)\"");
            foreach (var file in Files("*.uxml"))
            {
                var lines = File.ReadAllLines(file, Encoding.UTF8);
                for (var i = 0; i < lines.Length; i++)
                    foreach (Match m in attr.Matches(lines[i]))
                        if (m.Groups[1].Value.ToLowerInvariant().Contains("bullet"))
                            bad.Add(Rel(file) + ":" + (i + 1) + " \"" + m.Groups[1].Value + "\"");
            }
            Assert.That(bad, Is.Empty, "U-D-68 (c): rounds, never bullets");
        }

        static string Offence(string text)
        {
            foreach (var m in Mojibake) if (text.IndexOf(m, StringComparison.Ordinal) >= 0) return "mojibake";
            return text.ToLowerInvariant().Contains("bullet") ? "says bullets" : null;
        }

        /// <summary>
        /// The string literals on one line, stopping at a line comment. It is a line scanner, not a C# parser: a
        /// comment or an attribute is not text, and nothing in these folders spreads player text over a verbatim
        /// multi-line string.
        /// </summary>
        static IEnumerable<string> Literals(string line)
        {
            var t = line.TrimStart();
            if (t.StartsWith("//") || t.StartsWith("*") || t.StartsWith("/*") || t.StartsWith("[")) yield break;
            var sb = new StringBuilder();
            var inString = false;
            for (var i = 0; i < line.Length; i++)
            {
                var c = line[i];
                if (!inString)
                {
                    if (c == '/' && i + 1 < line.Length && line[i + 1] == '/') yield break;
                    if (c == '\'' && i + 1 < line.Length) { i += line[i + 1] == '\\' ? 3 : 2; continue; }
                    if (c == '"') { inString = true; sb.Clear(); }
                    continue;
                }
                if (c == '\\' && i + 1 < line.Length) { sb.Append(line[++i]); continue; }
                if (c == '"') { inString = false; yield return sb.ToString(); continue; }
                sb.Append(c);
            }
        }

        static IEnumerable<string> Files(params string[] patterns)
        {
            foreach (var folder in Folders)
            {
                var dir = Path.Combine(Root, folder);
                if (!Directory.Exists(dir)) continue;
                foreach (var pattern in patterns)
                    foreach (var file in Directory.GetFiles(dir, pattern, SearchOption.AllDirectories))
                        if (!file.Replace('\\', '/').Contains("/Generated/")) yield return file;
            }
        }

        static string Rel(string file) => file.Substring(Root.Length + 1).Replace('\\', '/');
    }
}
