using System.Collections.Generic;
using NUnit.Framework;
using Relight.Sim;

namespace Relight.Sim.Tests
{
    /// <summary>
    /// Checks the export-generated content tables (`Sim/Data/Generated/CatalogueData.g.cs`, produced by
    /// `Unity/Tools/export/exportCatalogue.ts`). These run without Unity: they touch only Relight.Sim.
    /// </summary>
    public sealed class CatalogueDataTests
    {
        private static readonly string[] Kinds = { "current", "approved", "provisional" };

        /// <summary>CONTENT_CATALOGUE §17.2 retires these; none may reappear in the tables.</summary>
        private static readonly string[] Retired = { "iron", "artifact1", "artifact2", "artifact3", "track", "tramstop", "tram" };

        [Test]
        public void ItemsCoverEveryIdInDeclarationOrder()
        {
            var items = CatalogueData.Items();
            Assert.That(items.Length, Is.EqualTo(Items.Count), "one definition per ItemId");
            Assert.That(items.Length, Is.EqualTo(20), "the retained roster is 20 items");
            for (var i = 0; i < items.Length; i++)
            {
                Assert.That((int)items[i].Id, Is.EqualTo(i), $"item {i} is out of ItemId order");
                Assert.That(items[i].Key, Is.EqualTo(Items.Key(items[i].Id)), "key must match the enum id");
                Assert.That(items[i].DisplayName, Is.Not.Empty);
                Assert.That(items[i].StackSize, Is.GreaterThan(0));
            }
        }

        [Test]
        public void StackSizesMatchTheReference()
        {
            var data = ReferenceData.Create();
            Assert.That(data.Item(ItemId.Steel).StackSize, Is.EqualTo(50));
            Assert.That(data.Item(ItemId.Magazine).StackSize, Is.EqualTo(200), "U-D-08: one item is one bullet");
            Assert.That(data.Item(ItemId.Shell).StackSize, Is.EqualTo(20));
            Assert.That(data.Item(ItemId.AlienArtifact).StackSize, Is.EqualTo(20));
            Assert.That(data.Item(ItemId.Core1).StackSize, Is.EqualTo(1));
            Assert.That(data.Item(ItemId.Overclock).StackSize, Is.EqualTo(1));
        }

        [Test]
        public void DisplayNamesComeFromTheReferenceNameTable()
        {
            var data = ReferenceData.Create();
            Assert.That(data.Item(ItemId.Steel).DisplayName, Is.EqualTo("Steel plates"));
            Assert.That(data.Item(ItemId.Magazine).DisplayName, Is.EqualTo("Bullets"));
            Assert.That(data.Item(ItemId.Crude).DisplayName, Is.EqualTo("Crude oil"));
            Assert.That(data.TryMachine("turret", out var turret), Is.True);
            Assert.That(turret.DisplayName, Is.EqualTo("Gun turret"));
        }

        [Test]
        public void NoRetiredIdIsPresent()
        {
            foreach (var item in CatalogueData.Items())
                Assert.That(Retired, Does.Not.Contain(item.Key), $"item '{item.Key}' is retired");
            foreach (var machine in CatalogueData.Machines())
                Assert.That(Retired, Does.Not.Contain(machine.Key), $"machine '{machine.Key}' is retired");
            Assert.That(CatalogueData.Stake().Ruleset, Is.Not.EqualTo("legacy-v1"), "legacy-v1 is retired");
        }

        [Test]
        public void KeysAreUniqueWithinEachFamily()
        {
            AssertUnique(Keys(CatalogueData.Items(), r => r.Key), "items");
            AssertUnique(Keys(CatalogueData.Machines(), r => r.Key), "machines");
            AssertUnique(Keys(CatalogueData.Recipes(), r => r.Key), "recipes");
            AssertUnique(Keys(CatalogueData.Weapons(), r => r.Key), "weapons");
            AssertUnique(Keys(CatalogueData.Enemies(), r => r.Key), "enemies");
            AssertUnique(Keys(CatalogueData.Ammunition(), r => r.Key), "ammunition");
            AssertUnique(Keys(CatalogueData.Turrets(), r => r.Key), "turrets");
        }

        [Test]
        public void EveryRecipeIsWellFormedAndItsItemsAreKnown()
        {
            var data = ReferenceData.Create();
            foreach (var recipe in data.Recipes)
            {
                Assert.That(recipe.Seconds, Is.GreaterThan(0), recipe.Key);
                Assert.That(recipe.Station, Is.Not.Empty, recipe.Key);
                Assert.That(recipe.Outputs.Count > 0 || recipe.OutputKey.Length > 0, Is.True,
                    $"recipe '{recipe.Key}' produces nothing");
                foreach (var stack in recipe.Inputs)
                {
                    Assert.That(stack.Count, Is.GreaterThan(0), recipe.Key);
                    Assert.That(data.Item(stack.Item), Is.Not.Null, $"'{recipe.Key}' uses an unknown item");
                }
                foreach (var stack in recipe.Outputs)
                {
                    Assert.That(stack.Count, Is.GreaterThan(0), recipe.Key);
                    Assert.That(data.Item(stack.Item), Is.Not.Null, $"'{recipe.Key}' makes an unknown item");
                }
            }
        }

        [Test]
        public void MachinesCarrySizeAndInventoryContract()
        {
            var data = ReferenceData.Create();
            foreach (var machine in data.Machines)
            {
                Assert.That(machine.Size, Is.GreaterThan(0), machine.Key);
                Assert.That(machine.DisplayName, Is.Not.Empty, machine.Key);
                if (machine.HasInventory) Assert.That(machine.InventorySlots, Is.GreaterThan(0), machine.Key);
                else Assert.That(machine.InventorySlots, Is.Zero, machine.Key);
                foreach (var stack in machine.Cost) Assert.That(stack.Count, Is.GreaterThan(0), machine.Key);
            }

            Assert.That(data.TryMachine("generator", out var generator), Is.True);
            Assert.That(generator.FuelCap, Is.EqualTo(50));
            Assert.That(generator.PowerKw, Is.EqualTo(-300), "the generator is a supply");
            Assert.That(data.TryMachine("turret", out var turret), Is.True);
            Assert.That(turret.AmmoCap, Is.EqualTo(50));
            Assert.That(data.TryMachine("chest", out var chest), Is.True);
            Assert.That(chest.HasInventory, Is.True);
            Assert.That(chest.InventorySlots, Is.EqualTo(200));
        }

        [Test]
        public void EveryRowCarriesKindAndSourceAndAgreesWithItsProvisionalFlag()
        {
            var data = ReferenceData.Create();
            foreach (var item in data.Items) Provenance(item.Key, item.Kind, item.Source, item.Provisional);
            foreach (var machine in data.Machines) Provenance(machine.Key, machine.Kind, machine.Source, machine.Provisional);
            foreach (var recipe in data.Recipes) Provenance(recipe.Key, recipe.Kind, recipe.Source, recipe.Provisional);
            foreach (var weapon in data.Weapons) Provenance(weapon.Key, weapon.Kind, weapon.Source, weapon.Provisional);
            foreach (var enemy in data.Enemies) Provenance(enemy.Key, enemy.Kind, enemy.Source, enemy.Provisional);
            foreach (var ammo in data.Ammunition) Provenance(ammo.Key, ammo.Kind, ammo.Source, ammo.Provisional);
            foreach (var t in data.Turrets) Provenance(t.Key, t.Kind, t.Source, t.Provisional);
            Provenance("power", data.Power.Kind, data.Power.Source, data.Power.Provisional);
            Provenance("time", data.Time.Kind, data.Time.Source, data.Time.Provisional);
            Provenance("raids", data.Raids.Kind, data.Raids.Source, data.Raids.Provisional);
            Provenance("opening", data.Opening.Kind, data.Opening.Source, data.Opening.Provisional);
            Provenance("stake", data.Stake.Kind, data.Stake.Source, data.Stake.Provisional);
            Provenance("engineer", data.Engineer.Kind, data.Engineer.Source, data.Engineer.Provisional);
            Provenance("world", data.World.Kind, data.World.Source, data.World.Provisional);
        }

        [Test]
        public void CombatTablesAreLoadedAndLookupWorks()
        {
            var data = ReferenceData.Create();
            Assert.That(data.Weapons.Count, Is.EqualTo(4));
            Assert.That(data.Enemies.Count, Is.EqualTo(2));
            Assert.That(data.Ammunition.Count, Is.EqualTo(2));
            Assert.That(data.Turrets.Count, Is.EqualTo(2));

            Assert.That(data.TryWeapon("rifle", out var rifle), Is.True);
            Assert.That(rifle.Damage, Is.EqualTo(10));
            Assert.That(rifle.Capacity, Is.EqualTo(10));
            Assert.That(data.TryEnemy("skitter", out var skitter), Is.True);
            Assert.That(skitter.Hp, Is.EqualTo(20));
            Assert.That(data.TryEnemy("spitter", out var spitter), Is.True);
            Assert.That(spitter.Ranged, Is.True);
            Assert.That(data.TryTurret("turret", out var turret), Is.True);
            Assert.That(turret.RangeTiles, Is.EqualTo(9));
            Assert.That(turret.Ammo, Is.EqualTo(ItemId.Magazine));
        }

        [Test]
        public void TuningTablesCarryTheReferenceNumbers()
        {
            var data = ReferenceData.Create();
            Assert.That(data.Time.TileTps, Is.EqualTo(20));
            Assert.That(data.Time.TileDt * data.Time.TileTps, Is.EqualTo(1).Within(1e-12));
            Assert.That(data.Time.DaySeconds, Is.EqualTo(1200));
            Assert.That(data.Power.GeneratorKw, Is.EqualTo(300));
            Assert.That(data.Power.CoalMj, Is.EqualTo(4));
            Assert.That(data.Raids.Total, Is.EqualTo(60));
            Assert.That(data.Raids.MajorSkitters + data.Raids.MajorSpitters, Is.EqualTo(data.Raids.MajorCount));
            Assert.That(data.Opening.Count, Is.EqualTo(5));
            Assert.That(data.Stake.Ruleset, Is.EqualTo("exploration-v2"));
            Assert.That(data.Stake.Pockets.Count, Is.EqualTo(2));
            Assert.That(data.Engineer.ReachTiles, Is.EqualTo(8));
            Assert.That(data.Engineer.DashCost, Is.EqualTo(0.25));
            Assert.That(data.World.TilePx, Is.EqualTo(32));
        }

        private static void Provenance(string key, string kind, string source, bool provisional)
        {
            Assert.That(Kinds, Does.Contain(kind), $"'{key}' has an unknown kind '{kind}'");
            Assert.That(source, Is.Not.Empty, $"'{key}' carries no source");
            Assert.That(source, Does.Not.Contain("Unresolved"), $"'{key}' carries an Unresolved marker");
            Assert.That(provisional, Is.EqualTo(kind == "provisional"), $"'{key}': the provisional flag disagrees with its kind");
        }

        private static List<string> Keys<T>(T[] rows, System.Func<T, string> key)
        {
            var list = new List<string>(rows.Length);
            foreach (var r in rows) list.Add(key(r));
            return list;
        }

        private static void AssertUnique(List<string> keys, string family)
        {
            var seen = new HashSet<string>();
            foreach (var k in keys) Assert.That(seen.Add(k), Is.True, $"duplicate {family} key '{k}'");
        }
    }
}
