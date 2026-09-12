using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using Relight.Data;
using Relight.Sim;

namespace Relight.Editor
{
    /// <summary>
    /// Registry-wide content checks (TA §5.4): no duplicate keys, every recipe and cost item known, machines
    /// carry a size and a power figure, every asset carries a Kind and a Source, no retired id, no "Unresolved"
    /// marker, and the registry converts to a <see cref="GameData"/> without throwing.
    /// Menu: Relight/Validate Game Data, or -executeMethod Relight.Editor.DataValidator.Validate.
    /// In batchmode a failure exits with code 1.
    /// </summary>
    public static class DataValidator
    {
        /// <summary>Ids and keys CONTENT_CATALOGUE §17.2 retires; none may appear in an asset.</summary>
        private static readonly string[] RetiredKeys =
        {
            "iron", "artifact1", "artifact2", "artifact3", "track", "tramstop", "tram", "legacy-v1",
        };

        [MenuItem("Relight/Validate Game Data")]
        public static void Validate()
        {
            var problems = new List<string>();
            var report = new StringBuilder();
            var registry = AssetDatabase.LoadAssetAtPath<GameDataRegistry>(DataAssetGenerator.RegistryPath);
            if (registry == null)
            {
                Finish(new List<string> { $"no registry at {DataAssetGenerator.RegistryPath}" }, report);
                return;
            }

            var keys = new Dictionary<string, string>(StringComparer.Ordinal);
            var assetCount = 0;
            foreach (var def in registry.All())
            {
                if (def == null)
                {
                    problems.Add("the registry holds a missing (null) asset reference");
                    continue;
                }
                assetCount++;
                var family = def.GetType().Name;
                var id = $"{family} '{def.name}'";

                var problem = def.Problem();
                if (problem != null) problems.Add($"{id}: {problem}");
                if (string.IsNullOrEmpty(def.Source)) problems.Add($"{id}: carries no Source");

                var scoped = family + "/" + def.Key;
                if (keys.ContainsKey(scoped)) problems.Add($"{id}: duplicate key '{def.Key}' in {family}");
                else keys[scoped] = id;

                foreach (var retired in RetiredKeys)
                    if (def.Key == retired) problems.Add($"{id}: uses the retired id '{retired}' (CONTENT_CATALOGUE §17.2)");
            }

            // Every item the sim knows must have exactly one definition, and every recipe/cost item must be one of them.
            var known = new HashSet<ItemId>();
            foreach (var item in registry.Items) if (item != null) known.Add(item.Id);
            if (known.Count != Items.Count)
                problems.Add($"the item table has {known.Count} distinct ids but the sim declares {Items.Count}");

            foreach (var recipe in registry.Recipes)
            {
                if (recipe == null) continue;
                foreach (var a in recipe.Inputs)
                    if (!known.Contains(a.item)) problems.Add($"Recipe '{recipe.Key}': input '{Items.Key(a.item)}' has no item definition");
                foreach (var a in recipe.Outputs)
                    if (!known.Contains(a.item)) problems.Add($"Recipe '{recipe.Key}': output '{Items.Key(a.item)}' has no item definition");
            }
            foreach (var ammo in registry.Ammunition)
                if (ammo != null && !known.Contains(ammo.Item))
                    problems.Add($"Ammo '{ammo.Key}': item '{Items.Key(ammo.Item)}' has no item definition");
            foreach (var turret in registry.Turrets)
                if (turret != null && !known.Contains(turret.Ammo))
                    problems.Add($"Turret '{turret.Key}': ammunition '{Items.Key(turret.Ammo)}' has no item definition");
            foreach (var machine in registry.Machines)
            {
                if (machine == null) continue;
                if (machine.Size <= 0) problems.Add($"Machine '{machine.Key}': no footprint size");
                if (machine.PowerKw == 0 && machine.HasInventory && machine.Key != "chest" && machine.Key != "cannon")
                    problems.Add($"Machine '{machine.Key}': an active machine with no power figure");
            }

            GameData data = null;
            try
            {
                data = registry.Build();
            }
            catch (Exception e)
            {
                problems.Add("the registry does not convert to GameData: " + e.Message);
            }

            report.AppendLine($"assets checked: {assetCount}");
            if (data != null)
            {
                report.AppendLine($"items {data.Items.Count}, machines {data.Machines.Count}, recipes {data.Recipes.Count}, " +
                                  $"weapons {data.Weapons.Count}, enemies {data.Enemies.Count}, ammunition {data.Ammunition.Count}, " +
                                  $"turrets {data.Turrets.Count}");
                var provisional = 0;
                foreach (var def in registry.All()) if (def != null && def.Provisional) provisional++;
                report.AppendLine($"provisional assets: {provisional}");
            }
            Finish(problems, report);
        }

        private static void Finish(List<string> problems, StringBuilder report)
        {
            if (problems.Count == 0)
            {
                Debug.Log($"B05-VALIDATE: OK\n{report}");
                return;
            }
            var text = new StringBuilder();
            text.AppendLine($"B05-VALIDATE: FAILED with {problems.Count} problem(s)");
            foreach (var p in problems) text.AppendLine("  - " + p);
            text.Append(report);
            Debug.LogError(text.ToString());
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }
    }
}
