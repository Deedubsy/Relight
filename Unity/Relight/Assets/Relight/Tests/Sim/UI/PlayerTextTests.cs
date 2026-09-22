using System;
using System.Collections.Generic;
using NUnit.Framework;
using Relight.Sim.Tests.Production;

namespace Relight.Sim.Tests.UI
{
    /// <summary>
    /// REL-55 (UI-05), "player text: mojibake, raw keys, two words for one thing". The issue's steps: "fix the four
    /// strings; a test that scans player text for non-ASCII mojibake and raw keys; pick one word". The word is
    /// U-D-68 (c): "Ammunition is 'rounds' in all player text, never 'bullets'." The opening chain's own sweep is
    /// <see cref="OpeningObjectiveTests"/>.
    /// </summary>
    public sealed class PlayerTextTests
    {
        /// <summary>What a UTF-8 "·" or "—" becomes when it is read back as Latin-1 or Windows-1252.</summary>
        private static readonly string[] Mojibake = { "\u00C2", "\u00C3", "\u00E2\u20AC", "\uFFFD" };

        private static void Clean(string text, string where)
        {
            Assert.That(text, Is.Not.Null.And.Not.Empty, where);
            // Ordinal: Unity's NUnit compares with the current culture, where U+FFFD is ignorable and "matches" anywhere.
            foreach (var bad in Mojibake)
                Assert.That(text.IndexOf(bad, StringComparison.Ordinal), Is.EqualTo(-1), where + ": mojibake in \"" + text + "\"");
            Assert.That(text.ToLowerInvariant(), Does.Not.Contain("bullet"), where + ": U-D-68 (c) says rounds — \"" + text + "\"");
        }

        // ---- the catalogue ------------------------------------------------------------------------------------

        [Test]
        public void EveryCatalogueNameIsWordsNotItsKey()
        {
            var d = ProductionFixture.Context().Data;
            var rows = new List<(string Key, string Name, string Table)>();
            foreach (var r in d.Items) rows.Add((r.Key, r.DisplayName, "item"));
            foreach (var r in d.Machines) rows.Add((r.Key, r.DisplayName, "machine"));
            foreach (var r in d.Recipes) rows.Add((r.Key, r.DisplayName, "recipe"));
            foreach (var r in d.Weapons) rows.Add((r.Key, r.DisplayName, "weapon"));
            foreach (var r in d.Enemies) rows.Add((r.Key, r.DisplayName, "enemy"));
            foreach (var r in d.Ammunition) rows.Add((r.Key, r.DisplayName, "ammunition"));
            foreach (var r in d.Turrets) rows.Add((r.Key, r.DisplayName, "turret"));
            Assert.That(rows.Count, Is.GreaterThan(50), "the whole catalogue, not an empty fixture");

            foreach (var (key, name, table) in rows)
            {
                Clean(name, table + " " + key);
                Assert.That(name, Is.Not.EqualTo(key), table + " " + key + " shows its raw key");
            }
        }

        [Test]
        public void TheAmmunitionItemIsCalledRounds()
        {
            var d = ProductionFixture.Context().Data;
            Assert.That(d.Item(ItemId.Magazine).DisplayName, Is.EqualTo("Rounds"));
            Assert.That(d.Item(ItemId.Magazine).Key, Is.EqualTo("magazine"), "the saved key does not move");
        }

        // ---- names for kinds the catalogue does not carry -----------------------------------------------------

        [Test]
        public void AKindTheCatalogueLeavesOutStillReadsAsAName()
        {
            var d = ProductionFixture.Context().Data;
            Assert.That(PlayerNames.Machine(d, "tramstop"), Is.EqualTo("Tram stop"), "flow.ts KIND_LABEL");
            Assert.That(PlayerNames.Machine(d, "track"), Is.EqualTo("Track"));
            Assert.That(PlayerNames.Machine(d, "tram"), Is.EqualTo("Tram"));
            Assert.That(PlayerNames.Machine(d, "no-such_kind"), Is.EqualTo("No such kind"), "words, never the key");
            Assert.That(PlayerNames.Machine(d, null), Is.EqualTo("Unknown"));
            Assert.That(d.TryMachine("assembler", out var asm), Is.True);
            Assert.That(PlayerNames.Machine(d, "assembler"), Is.EqualTo(asm.DisplayName));
        }

        [Test]
        public void AWeaponInstanceKeyReadsAsItsWeapon()
        {
            var d = ProductionFixture.Context().Data;
            Assert.That(d.TryWeapon("rifle", out var rifle), Is.True);
            Assert.That(PlayerNames.Weapon(d, "rifle:3"), Is.EqualTo(rifle.DisplayName),
                "a Backpack slot holding a weapon instance used to show \"rifle:3\"");
            Assert.That(PlayerNames.Weapon(d, "rifle"), Is.EqualTo(rifle.DisplayName));
            Assert.That(PlayerNames.Weapon(d, "no-such-gun:12"), Is.EqualTo("No such gun"), "an unknown weapon reads as words");
        }

        // ---- hover text ---------------------------------------------------------------------------------------

        /// <summary>The four strings the issue names: inserter, splitter, underground and conveyor hovers showed a
        /// mis-decoded middle dot (U+00C2 before the U+00B7).</summary>
        [Test]
        public void TheFlowHoversUseAPlainMiddleDot()
        {
            var ctx = ProductionFixture.Context();
            var st = ProductionFixture.State(ctx);
            var kinds = new[] { "inserter", "splitter", "underground", "belt", "fastbelt" };
            for (var i = 0; i < kinds.Length; i++)
            {
                var m = ProductionFixture.Add(ctx, st, kinds[i], 10 + i * 4, 10, Dir.E);
                ProductionFixture.Run(ctx, st, 1);
                var text = FlowQueries.Description(ctx, st, m);
                Clean(text, kinds[i]);
                Assert.That(text, Does.Contain(" · "), kinds[i] + " separates its parts with a middle dot");
                Clean(ProductionQueries.Description(ctx, st, m.Id), kinds[i] + " hover");
            }
        }

        /// <summary>
        /// The issue's last "Now" line: "A machine with no recipe reads 'idle', the same as one between crafts."
        /// U-D-53 ships the Assembler with no recipe, so this is the first thing a new one says.
        /// </summary>
        [Test]
        public void AnAssemblerWithNoRecipeSaysSoRatherThanIdle()
        {
            var ctx = ProductionFixture.Context();
            var st = ProductionFixture.State(ctx);
            ProductionFixture.Grid(ctx, st, 20, 18, 18, 14, 50);
            var asm = ProductionFixture.Add(ctx, st, "assembler", 12, 20);
            ProductionFixture.Run(ctx, st, 2);

            var status = ProductionQueries.Status(ctx, st, asm.Id);
            Assert.That(status.State, Is.EqualTo(MachineOperatingState.Idle), "the state itself does not change");
            Assert.That(status.Text, Is.EqualTo(ProductionQueries.NoRecipeText));
            Assert.That(ProductionQueries.Description(ctx, st, asm.Id), Does.StartWith("no recipe set"));

            ProductionFixture.Recipe(ctx, st, asm, "bullet-batch");
            ProductionFixture.Run(ctx, st, 2);
            Assert.That(ProductionQueries.Status(ctx, st, asm.Id).Text, Is.Not.EqualTo(ProductionQueries.NoRecipeText),
                "with a recipe it is waiting for materials, not missing a recipe");
        }
    }
}
