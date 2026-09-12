using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Relight.Data;
using Relight.Sim;

namespace Relight.Editor
{
    /// <summary>
    /// Creates or updates the ScriptableObject content assets from the export-generated
    /// <see cref="CatalogueData"/> tables (TA §5.2, U-M-04). Idempotent: an asset that already carries a key is
    /// filled in place, so its GUID — and every prefab or scene reference to it — survives a regeneration.
    /// Assets whose key is no longer in the catalogue are deleted, which is how a retired row leaves the project.
    ///
    /// <b>Hand edits survive (U-M-04: "generated initially … then hand-editable").</b> After every write the
    /// generator stamps the asset with a fingerprint of the values it wrote. On the next run an asset whose current
    /// values no longer match its stamp has been edited in the Inspector: <see cref="Generate"/> keeps it exactly as
    /// it is and lists it in the console. Only <see cref="Regenerate"/> (the "discard hand edits" menu) overwrites
    /// such an asset with the reference values again. An asset with no stamp (one made before this rule) is
    /// treated as unedited and stamped on the first run.
    ///
    /// <b>Conflicts are reported, never resolved silently.</b> A kept hand-edited asset whose REFERENCE values have
    /// also moved on since the stamp (the export changed the same row a human changed in the Inspector) is a
    /// genuine two-sided conflict. The generator keeps the hand edit — it never overwrites — and logs a
    /// <c>CONFLICT</c> warning naming the asset, its stamp and both fingerprints, counts the conflicts in the
    /// summary and lists them in <see cref="LastConflicts"/>, so the human can re-apply the new reference values
    /// or discard the edit deliberately.
    ///
    /// <b>Two modes, never mixed.</b> Every run logs <c>B05-GENERATE: mode=keep</c> or <c>mode=reset</c> on its
    /// first line:
    /// <list type="bullet">
    /// <item>mode=keep — incremental. Menu Relight/Setup/Generate Data Assets, batch
    /// <c>-executeMethod Relight.Editor.DataAssetGenerator.Generate</c> (alias
    /// <c>.GenerateKeepingHandEdits</c>). Hand edits and hand-edited assets with unknown keys are kept.</item>
    /// <item>mode=reset — explicit discard. Menu Relight/Setup/Regenerate Data Assets (discard hand edits), batch
    /// <c>-executeMethod Relight.Editor.DataAssetGenerator.Regenerate</c> (alias
    /// <c>.RegenerateDiscardingHandEdits</c>). Every asset is overwritten with the reference values.</item>
    /// </list>
    /// Both finish with <c>B05-GENERATE: OK</c>; a failure logs <c>B05-GENERATE: FAILED</c> and exits 1 in batch mode.
    /// </summary>
    public static class DataAssetGenerator
    {
        public const string DataFolder = "Assets/Relight/Data";
        public const string GeneratedFolder = DataFolder + "/Generated";
        public const string RegistryPath = DataFolder + "/GameDataRegistry.asset";

        /// <summary>Incremental generation: fills what the generator owns and KEEPS hand edits (mode=keep).</summary>
        [MenuItem("Relight/Setup/Generate Data Assets")]
        public static void Generate() => Run(keepHandEdits: true);

        /// <summary>The explicit reset: overwrites every asset with the reference values (mode=reset).</summary>
        [MenuItem("Relight/Setup/Regenerate Data Assets (discard hand edits)")]
        public static void Regenerate() => Run(keepHandEdits: false);

        /// <summary>Spelt-out alias of <see cref="Generate"/> for batch command lines that want the mode in the name.</summary>
        public static void GenerateKeepingHandEdits() => Generate();

        /// <summary>Spelt-out alias of <see cref="Regenerate"/>; this one DISCARDS hand edits.</summary>
        public static void RegenerateDiscardingHandEdits() => Regenerate();

        /// <summary>The assets the last run left untouched because they carried hand edits, as asset paths.</summary>
        public static IReadOnlyList<string> LastKept => _kept;
        private static readonly List<string> _kept = new List<string>();

        /// <summary>
        /// The assets the last run found in conflict: hand-edited AND changed in the reference export since the
        /// stamp. They are a subset of <see cref="LastKept"/> — the hand edit was kept, not overwritten.
        /// </summary>
        public static IReadOnlyList<string> LastConflicts => _conflicts;
        private static readonly List<string> _conflicts = new List<string>();

        private static void Run(bool keepHandEdits)
        {
            try
            {
                var log = new List<string>();
                _kept.Clear();
                _conflicts.Clear();
                _keepHandEdits = keepHandEdits;
                // First line of every run: which of the two modes this was, so a console or a batch log can never
                // be read as the wrong one.
                Debug.Log("B05-GENERATE: mode=" + (keepHandEdits ? "keep" : "reset") +
                          (keepHandEdits ? " (incremental; hand edits are kept)" : " (explicit reset; hand edits are discarded)"));
                EnsureFolder(GeneratedFolder);

                var items = Sync<ItemDefinition, ItemDef>("Items", "Item", CatalogueData.Items(),
                    r => r.Key, r => r.DisplayName, (a, r) => a.Fill(r), log);
                var machines = Sync<MachineDefinition, MachineSpec>("Machines", "Machine", CatalogueData.Machines(),
                    r => r.Key, r => r.DisplayName, (a, r) => a.Fill(r), log);
                var recipes = Sync<RecipeDefinition, Recipe>("Recipes", "Recipe", CatalogueData.Recipes(),
                    r => r.Key, r => r.DisplayName, (a, r) => a.Fill(r), log);
                var weapons = Sync<WeaponDefinition, WeaponDef>("Weapons", "Weapon", CatalogueData.Weapons(),
                    r => r.Key, r => r.DisplayName, (a, r) => a.Fill(r), log);
                var enemies = Sync<EnemyDefinition, EnemyDef>("Enemies", "Enemy", CatalogueData.Enemies(),
                    r => r.Key, r => r.DisplayName, (a, r) => a.Fill(r), log);
                var ammunition = Sync<AmmoDefinition, AmmoDef>("Ammunition", "Ammo", CatalogueData.Ammunition(),
                    r => r.Key, r => r.DisplayName, (a, r) => a.Fill(r), log);
                var turrets = Sync<TurretDefinition, TurretDef>("Turrets", "Turret", CatalogueData.Turrets(),
                    r => r.Key, r => r.DisplayName, (a, r) => a.Fill(r), log);

                EnsureFolder(GeneratedFolder + "/Tuning");
                var power = Single<PowerTuningAsset>("Tuning - Power", a => a.Fill(CatalogueData.Power()), log);
                var time = Single<TimeTuningAsset>("Tuning - Time", a => a.Fill(CatalogueData.Time()), log);
                var raids = Single<RaidDirectorTuningAsset>("Tuning - Raids", a => a.Fill(CatalogueData.Raids()), log);
                var opening = Single<OpeningEncounterTuningAsset>("Tuning - Opening", a => a.Fill(CatalogueData.Opening()), log);
                var stake = Single<StartingStakeAsset>("Tuning - Starting stake", a => a.Fill(CatalogueData.Stake()), log);
                var engineer = Single<EngineerTuningAsset>("Tuning - Engineer", a => a.Fill(CatalogueData.Engineer()), log);
                var world = Single<WorldTuningAsset>("Tuning - World", a => a.Fill(CatalogueData.World()), log);

                var registry = AssetDatabase.LoadAssetAtPath<GameDataRegistry>(RegistryPath);
                if (registry == null)
                {
                    registry = ScriptableObject.CreateInstance<GameDataRegistry>();
                    AssetDatabase.CreateAsset(registry, RegistryPath);
                    log.Add("created GameDataRegistry.asset");
                }
                registry.SetContents(items, machines, recipes, weapons, enemies, ammunition, turrets,
                    power, time, raids, opening, stake, engineer, world);
                EditorUtility.SetDirty(registry);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log("B05-GENERATE: " + string.Join("; ", log));
                if (_kept.Count > 0)
                    Debug.LogWarning($"B05-GENERATE: kept {_kept.Count} hand-edited asset(s) as they are (use Relight/Setup/Regenerate Data Assets to discard the edits): " + string.Join("; ", _kept));
                else
                    Debug.Log("B05-GENERATE: no hand-edited assets found" + (keepHandEdits ? "" : " (discard mode)"));
                if (_conflicts.Count > 0)
                    Debug.LogWarning($"B05-GENERATE: {_conflicts.Count} CONFLICT(s) — hand-edited AND changed in the reference export; the hand edit was kept in each case, so the new reference values are NOT in the project: " + string.Join("; ", _conflicts));
                Debug.Log($"B05-GENERATE: items={items.Length} machines={machines.Length} recipes={recipes.Length} " +
                          $"weapons={weapons.Length} enemies={enemies.Length} ammunition={ammunition.Length} " +
                          $"turrets={turrets.Length} tuning=7 mode={(keepHandEdits ? "keep" : "reset")} " +
                          $"kept={_kept.Count} conflicts={_conflicts.Count} registry={RegistryPath}");
                Debug.Log("B05-GENERATE: OK");
            }
            catch (Exception e)
            {
                Debug.LogError($"B05-GENERATE: FAILED {e}");
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        private static TAsset[] Sync<TAsset, TRecord>(string folderName, string prefix, TRecord[] records,
            Func<TRecord, string> key, Func<TRecord, string> display, Action<TAsset, TRecord> fill, List<string> log)
            where TAsset : DataDefinition
        {
            var folder = GeneratedFolder + "/" + folderName;
            EnsureFolder(folder);

            var existing = new Dictionary<string, TAsset>(StringComparer.Ordinal);
            var unkeyed = new List<TAsset>();
            foreach (var guid in AssetDatabase.FindAssets("t:" + typeof(TAsset).Name, new[] { folder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<TAsset>(path);
                if (asset == null) continue;
                if (string.IsNullOrEmpty(asset.Key) || existing.ContainsKey(asset.Key)) unkeyed.Add(asset);
                else existing[asset.Key] = asset;
            }

            var result = new TAsset[records.Length];
            var kept = new HashSet<string>(StringComparer.Ordinal);
            var created = 0;
            var updated = 0;
            var kept_edits = 0;
            var conflicts = 0;
            for (var i = 0; i < records.Length; i++)
            {
                var k = key(records[i]);
                var wanted = $"{prefix} - {Sanitise(display(records[i]))}";
                if (!existing.TryGetValue(k, out var asset))
                {
                    asset = ScriptableObject.CreateInstance<TAsset>();
                    fill(asset, records[i]);
                    Stamp(asset);
                    AssetDatabase.CreateAsset(asset, $"{folder}/{wanted}.asset");
                    created++;
                }
                else if (HandEdited(asset))
                {
                    var record = records[i];
                    _kept.Add(AssetDatabase.GetAssetPath(asset));
                    kept_edits++;
                    if (Conflicted(asset, a => fill(a, record))) conflicts++;
                }
                else
                {
                    fill(asset, records[i]);
                    Stamp(asset);
                    EditorUtility.SetDirty(asset);
                    var path = AssetDatabase.GetAssetPath(asset);
                    if (Path.GetFileNameWithoutExtension(path) != wanted)
                    {
                        // Renaming keeps the GUID, so scene and prefab references survive a display-name change.
                        var error = AssetDatabase.RenameAsset(path, wanted);
                        if (!string.IsNullOrEmpty(error)) Debug.LogWarning($"B05-GENERATE: rename of {path} failed: {error}");
                    }
                    updated++;
                }
                kept.Add(k);
                result[i] = asset;
            }

            var removed = 0;
            var orphans = 0;
            foreach (var pair in existing)
            {
                if (kept.Contains(pair.Key)) continue;
                // A key the catalogue no longer has is normally a retired row leaving the project. But a
                // HAND-EDITED asset in the same position may simply have had its key retyped in the Inspector, and
                // deleting it would throw away that work and break every prefab/scene reference to its GUID. Keep
                // it, report it, and let a human decide; mode=reset still clears it out (HandEdited is false there).
                if (Orphaned(pair.Value, $"its key '{pair.Key}' is not in the reference catalogue")) { orphans++; continue; }
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(pair.Value));
                removed++;
            }
            foreach (var asset in unkeyed)
            {
                // No key at all, or a key a second asset already claimed: the generator cannot match it to a row.
                if (Orphaned(asset, "it has no key, or its key duplicates another asset's")) { orphans++; continue; }
                var path = AssetDatabase.GetAssetPath(asset);
                Debug.LogWarning($"B05-GENERATE: removed unkeyed/duplicate-key asset {path} — it carries no generator " +
                                 "stamp differing from its values, so it is not recognisable as a hand edit.");
                AssetDatabase.DeleteAsset(path);
                removed++;
            }

            log.Add($"{folderName}: {created} created, {updated} updated, {kept_edits} kept (hand-edited), " +
                    $"{conflicts} conflicted, {orphans} kept (unmatched), {removed} removed");
            return result;
        }

        /// <summary>
        /// An asset the catalogue cannot match. Hand edits are never deleted: true means it was kept and reported
        /// (and recorded in <see cref="LastKept"/>), false means the caller may remove it.
        /// </summary>
        private static bool Orphaned(DataDefinition asset, string why)
        {
            if (!HandEdited(asset)) return false;
            var path = AssetDatabase.GetAssetPath(asset);
            _kept.Add(path);
            Debug.LogWarning($"B05-GENERATE: kept hand-edited asset {path} that the generator cannot match — {why}. " +
                             "It is NOT in the registry this run. Fix its key, or discard it with " +
                             "Relight/Setup/Regenerate Data Assets (discard hand edits).");
            return true;
        }

        /// <summary>
        /// Both sides moved: the asset was hand-edited (the caller has already established that) AND the reference
        /// record no longer fingerprints to the stamp the generator wrote, so the export changed the same row.
        /// The hand edit is kept — this generator never overwrites one — but the conflict is named in the console
        /// and counted, because the new reference values are silently absent from the project until a human acts.
        /// The probe is a throwaway instance of the same type, so its serialised shape matches the asset's and the
        /// two fingerprints are comparable (the stamp was produced the same way).
        /// </summary>
        private static bool Conflicted<TAsset>(TAsset asset, Action<TAsset> fill) where TAsset : DataDefinition
        {
            var stamp = asset.GeneratedFingerprint;
            string reference;
            var probe = ScriptableObject.CreateInstance<TAsset>();
            try
            {
                fill(probe);
                reference = Fingerprint(probe);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(probe);
            }
            if (string.Equals(reference, stamp, StringComparison.Ordinal)) return false;

            var path = AssetDatabase.GetAssetPath(asset);
            _conflicts.Add(path);
            Debug.LogWarning($"B05-GENERATE: CONFLICT {path} — hand-edited AND changed in the reference export. " +
                             $"stamp={stamp} asset={Fingerprint(asset)} reference={reference}. The hand edit is kept; " +
                             "the new reference values are not applied. Re-apply them by hand, or discard the edit " +
                             "with Relight/Setup/Regenerate Data Assets (discard hand edits).");
            return true;
        }

        private static TAsset Single<TAsset>(string assetName, Action<TAsset> fill, List<string> log)
            where TAsset : DataDefinition
        {
            var path = $"{GeneratedFolder}/Tuning/{assetName}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<TAsset>(path);
            var created = asset == null;
            if (!created && HandEdited(asset))
            {
                _kept.Add(path);
                var conflicted = Conflicted(asset, fill);
                log.Add($"{assetName}: kept (hand-edited{(conflicted ? ", CONFLICT" : "")})");
                return asset;
            }
            if (created) asset = ScriptableObject.CreateInstance<TAsset>();
            fill(asset);
            Stamp(asset);
            if (created) AssetDatabase.CreateAsset(asset, path);
            else EditorUtility.SetDirty(asset);
            log.Add($"{assetName}: {(created ? "created" : "updated")}");
            return asset;
        }

        private static bool _keepHandEdits = true;

        /// <summary>
        /// True when the asset's current values differ from the ones the generator last wrote, and the run keeps
        /// hand edits. An unstamped asset is never treated as hand-edited: there is nothing to compare against.
        /// </summary>
        private static bool HandEdited(DataDefinition asset)
        {
            if (!_keepHandEdits) return false;
            var stamp = asset.GeneratedFingerprint;
            if (string.IsNullOrEmpty(stamp)) return false;
            return !string.Equals(Fingerprint(asset), stamp, StringComparison.Ordinal);
        }

        private static void Stamp(DataDefinition asset) => asset.SetGeneratedFingerprint(Fingerprint(asset));

        /// <summary>
        /// A fingerprint of every serialised value except the asset's name, its class identifier and the stamp
        /// itself — the fields the generator's rename and the stamp can change without a human touching anything.
        /// </summary>
        public static string Fingerprint(DataDefinition asset)
        {
            var json = EditorJsonUtility.ToJson(asset);
            json = System.Text.RegularExpressions.Regex.Replace(json,
                "\"(m_Name|m_EditorClassIdentifier|generatedFingerprint)\":\"(?:[^\"\\\\]|\\\\.)*\",?", "");
            using (var sha = System.Security.Cryptography.SHA1.Create())
            {
                var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(json));
                var sb = new System.Text.StringBuilder(bytes.Length * 2);
                foreach (var b in bytes) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        private static void EnsureFolder(string folder)
        {
            if (string.IsNullOrEmpty(folder) || folder == "Assets" || AssetDatabase.IsValidFolder(folder)) return;
            var parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }

        /// <summary>Asset file names cannot contain path or reserved characters; display names otherwise pass through.</summary>
        private static string Sanitise(string name)
        {
            var chars = name.ToCharArray();
            for (var i = 0; i < chars.Length; i++)
            {
                var c = chars[i];
                if (c == '/' || c == '\\' || c == ':' || c == '*' || c == '?' || c == '"' || c == '<' || c == '>' || c == '|') chars[i] = '-';
            }
            return new string(chars).Trim();
        }
    }
}
