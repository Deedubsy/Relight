using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Relight.Data
{
    /// <summary>
    /// REL-73 (E-19): the development-build half of "a changed tuning value takes effect without a restart". In the
    /// editor an Inspector edit raises <see cref="DataDefinition.Edited"/> and the game reloads (REL-84). A build has
    /// no Inspector and never runs <c>OnValidate</c>, so a development build takes its changes from text files: the
    /// Admin panel writes each combat asset's values to a JSON file, the tester edits a number, and the panel reads
    /// the files back onto the loaded assets and rebuilds the data record, as an Inspector edit does.
    ///
    /// Only the loaded copy of an asset changes. A build cannot write its assets back and nothing here saves
    /// anything, so the files are the whole record of a change: a restart runs the shipped values until the files
    /// are read again. The files hold numbers only; the assets offered (<see cref="Offered"/>) have no references to
    /// other assets, which a JSON file could not carry from one run to the next.
    /// </summary>
    public static class TuningFiles
    {
        /// <summary>The folder under <c>Application.persistentDataPath</c>.</summary>
        public const string FolderName = "Tuning";

        public static string DefaultFolder => Path.Combine(Application.persistentDataPath, FolderName);

        /// <summary>The file for one asset: its asset name, as the Project window shows it.</summary>
        public static string FileName(DataDefinition d) => d.name + ".json";

        /// <summary>
        /// The assets a file may change: the nine tuning assets and the enemy, turret, ammunition and weapon
        /// definitions, which between them hold every threat number THREAT_TUNING.md lists as on an asset.
        /// </summary>
        public static List<DataDefinition> Offered(GameDataRegistry registry)
        {
            var list = new List<DataDefinition>();
            if (registry == null) return list;
            void Add(DataDefinition d) { if (d != null) list.Add(d); }
            Add(registry.Raids); Add(registry.Siege); Add(registry.Defence); Add(registry.Opening);
            Add(registry.Power); Add(registry.Time); Add(registry.Stake); Add(registry.Engineer); Add(registry.World);
            foreach (var d in registry.Enemies) Add(d);
            foreach (var d in registry.Turrets) Add(d);
            foreach (var d in registry.Ammunition) Add(d);
            foreach (var d in registry.Weapons) Add(d);
            return list;
        }

        /// <summary>
        /// Writes each asset's current values to its file. A file already there is kept unless
        /// <paramref name="overwrite"/>, so pressing the button twice never throws away a tester's edits. Returns the
        /// number of files written.
        /// </summary>
        public static int Write(IEnumerable<DataDefinition> assets, string folder, bool overwrite = false)
        {
            Directory.CreateDirectory(folder);
            var written = 0;
            foreach (var d in assets)
            {
                var path = Path.Combine(folder, FileName(d));
                if (!overwrite && File.Exists(path)) continue;
                File.WriteAllText(path, JsonUtility.ToJson(d, true));
                written++;
            }
            return written;
        }

        /// <summary>What <see cref="Read"/> did.</summary>
        public sealed class Outcome
        {
            /// <summary>Files found for an offered asset.</summary>
            public int Files;
            /// <summary>Assets whose values the files changed.</summary>
            public readonly List<string> Changed = new List<string>();
            /// <summary>Files refused, each with the reason; the asset keeps the values it had.</summary>
            public readonly List<string> Refused = new List<string>();
        }

        /// <summary>
        /// Reads each asset's file, when there is one, onto the loaded asset. A file that does not parse, that changes
        /// the asset's key, or that leaves the asset with a <see cref="DataDefinition.Problem"/> is refused and the
        /// asset keeps exactly the values it had. A file that leaves a field out leaves that field alone.
        /// </summary>
        public static Outcome Read(IEnumerable<DataDefinition> assets, string folder)
        {
            var o = new Outcome();
            if (!Directory.Exists(folder)) return o;
            foreach (var d in assets)
            {
                var path = Path.Combine(folder, FileName(d));
                if (!File.Exists(path)) continue;
                o.Files++;
                var before = JsonUtility.ToJson(d);
                var key = d.Key;
                string problem;
                try
                {
                    JsonUtility.FromJsonOverwrite(File.ReadAllText(path), d);
                    problem = d.Key != key ? "its key cannot change" : d.Problem();
                }
                catch (Exception e)
                {
                    problem = "it does not read as JSON (" + e.Message + ")";
                }
                if (problem != null)
                {
                    JsonUtility.FromJsonOverwrite(before, d);
                    o.Refused.Add(d.name + ": " + problem);
                    continue;
                }
                if (JsonUtility.ToJson(d) != before) o.Changed.Add(d.name);
            }
            return o;
        }
    }
}
