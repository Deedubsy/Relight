using System;
using System.Collections.Generic;
using NUnit.Framework;
using Relight.Data;
using Relight.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Relight.Authoring.Tests
{
    /// <summary>
    /// REL-26 (ECO-02): the data check (Relight/Validate Game Data) fails when a recipe names a station no machine
    /// runs. 'bullet-batch-mk2' did: its "Assembler Mk2" station matched nothing, because the Mk2 carries the plain
    /// "Assembler" station and a 2x speed.
    /// </summary>
    public sealed class DataValidatorTests
    {
        const string RegistryPath = "Assets/Relight/Data/GameDataRegistry.asset";

        static GameDataRegistry Registry()
        {
            var registry = AssetDatabase.LoadAssetAtPath<GameDataRegistry>(RegistryPath);
            Assert.That(registry, Is.Not.Null, RegistryPath);
            return registry;
        }

        [Test] public void TheProjectsDataPassesTheCheck()
        {
            var problems = DataValidator.Check(Registry());
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        [Test] public void ARecipeOnAStationNoMachineRunsFailsTheCheck()
        {
            var real = Registry();
            RecipeDefinition shot = null;
            foreach (var r in real.Recipes) if (r != null && r.Key == "bullet-batch") shot = r;
            Assert.That(shot, Is.Not.Null, "bullet-batch");

            var orphan = Object.Instantiate(shot);
            var copy = ScriptableObject.CreateInstance<GameDataRegistry>();
            try
            {
                var so = new SerializedObject(orphan);
                so.FindProperty("key").stringValue = "rel26-orphan";
                so.FindProperty("station").stringValue = "Assembler Mk2";
                so.ApplyModifiedPropertiesWithoutUndo();

                var recipes = new List<RecipeDefinition>(real.Recipes) { orphan };
                copy.SetContents(new List<ItemDefinition>(real.Items).ToArray(),
                    new List<MachineDefinition>(real.Machines).ToArray(), recipes.ToArray(),
                    new List<WeaponDefinition>(real.Weapons).ToArray(), new List<EnemyDefinition>(real.Enemies).ToArray(),
                    new List<AmmoDefinition>(real.Ammunition).ToArray(), new List<TurretDefinition>(real.Turrets).ToArray(),
                    real.Power, real.Time, real.Raids, real.Opening, real.Stake, real.Defence, real.Engineer, real.World,
                    real.Siege);

                var problems = DataValidator.Check(copy);
                Assert.That(problems.Count, Is.EqualTo(1), string.Join("\n", problems));
                Assert.That(problems[0].IndexOf("'rel26-orphan'", StringComparison.Ordinal), Is.GreaterThanOrEqualTo(0), problems[0]);
                Assert.That(problems[0].IndexOf("'Assembler Mk2'", StringComparison.Ordinal), Is.GreaterThanOrEqualTo(0), problems[0]);
            }
            finally
            {
                Object.DestroyImmediate(orphan);
                Object.DestroyImmediate(copy);
            }
        }
    }
}
