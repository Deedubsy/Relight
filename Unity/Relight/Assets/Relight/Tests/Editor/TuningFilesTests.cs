using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Relight.Data;
using Relight.Sim;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Relight.Authoring.Tests
{
    /// <summary>
    /// REL-73 (E-19): the development-build route to a tuning change (<see cref="TuningFiles"/>). A file written from
    /// an asset, edited and read back changes the loaded asset and the data record built from it; a file that does
    /// not parse, renames the asset's key or leaves it with a problem is refused and changes nothing.
    ///
    /// Every test works on a COPY of the real siege asset, in a registry made for the test, and in a folder under the
    /// system temp path: the project's assets and the player's own tuning folder are never touched.
    /// </summary>
    public sealed class TuningFilesTests
    {
        private string _folder;
        private readonly List<Object> _made = new List<Object>();

        [SetUp]
        public void Setup() => _folder = Path.Combine(Path.GetTempPath(), "relight-tuning-" + Guid.NewGuid().ToString("N"));

        [TearDown]
        public void Teardown()
        {
            foreach (var o in _made) if (o != null) Object.DestroyImmediate(o);
            _made.Clear();
            if (Directory.Exists(_folder)) Directory.Delete(_folder, true);
        }

        private static GameDataRegistry Real()
        {
            var r = AssetDatabase.LoadAssetAtPath<GameDataRegistry>(RealCityFixture.Registry);
            Assert.That(r, Is.Not.Null, RealCityFixture.Registry);
            return r;
        }

        private SiegeTuningAsset Copy(SiegeTuningAsset real)
        {
            var c = Object.Instantiate(real);
            c.name = real.name;
            _made.Add(c);
            return c;
        }

        /// <summary>The real registry's assets with <paramref name="siege"/> in the siege slot.</summary>
        private GameDataRegistry With(GameDataRegistry r, SiegeTuningAsset siege)
        {
            var g = ScriptableObject.CreateInstance<GameDataRegistry>();
            _made.Add(g);
            g.SetContents(new List<ItemDefinition>(r.Items).ToArray(), new List<MachineDefinition>(r.Machines).ToArray(),
                new List<RecipeDefinition>(r.Recipes).ToArray(), new List<WeaponDefinition>(r.Weapons).ToArray(),
                new List<EnemyDefinition>(r.Enemies).ToArray(), new List<AmmoDefinition>(r.Ammunition).ToArray(),
                new List<TurretDefinition>(r.Turrets).ToArray(), r.Power, r.Time, r.Raids, r.Opening, r.Stake, r.Defence,
                r.Engineer, r.World, siege);
            return g;
        }

        private string PathOf(DataDefinition d) => Path.Combine(_folder, TuningFiles.FileName(d));

        /// <summary>Sets one number in the file, the way a tester would in a text editor.</summary>
        private void Edit(DataDefinition d, string field, string value)
        {
            var path = PathOf(d);
            var text = File.ReadAllText(path);
            var edited = Regex.Replace(text, "(\"" + field + "\"\\s*:\\s*)[-0-9.eE+]+", "${1}" + value);
            Assert.That(edited, Is.Not.EqualTo(text), field + " is in the file");
            File.WriteAllText(path, edited);
        }

        [Test]
        public void AnEditedNumber_IsReadOntoTheLoadedAsset_AndReachesTheDataRecord()
        {
            var real = Real();
            var siege = Copy(real.Siege);
            var registry = With(real, siege);
            var before = registry.Build();
            var gap = before.Siege.MajorWaveGapS;
            Assert.That(gap, Is.Not.EqualTo(150), "the edit below is a change");

            Assert.That(TuningFiles.Write(new DataDefinition[] { siege }, _folder), Is.EqualTo(1));
            Edit(siege, "majorWaveGapSeconds", "150.0");
            var o = TuningFiles.Read(new DataDefinition[] { siege }, _folder);

            Assert.That(o.Files, Is.EqualTo(1));
            Assert.That(o.Refused, Is.Empty);
            Assert.That(o.Changed, Is.EqualTo(new[] { siege.name }));
            var after = registry.Build();
            Assert.That(after.Siege.MajorWaveGapS, Is.EqualTo(150));
            Assert.That(GameDataHash.Compute(after), Is.Not.EqualTo(GameDataHash.Compute(before)), "the data hash moves, as an Inspector edit moves it");
            Assert.That(real.Siege.ToRecord().MajorWaveGapS, Is.EqualTo(gap), "the project's own asset is untouched");
        }

        [Test]
        public void AFileThatMatchesTheAsset_ChangesNothing()
        {
            var siege = Copy(Real().Siege);
            TuningFiles.Write(new DataDefinition[] { siege }, _folder);
            var o = TuningFiles.Read(new DataDefinition[] { siege }, _folder);
            Assert.That(o.Files, Is.EqualTo(1));
            Assert.That(o.Changed, Is.Empty);
            Assert.That(o.Refused, Is.Empty);
        }

        [Test]
        public void WritingAgain_KeepsTheTestersEdits()
        {
            var siege = Copy(Real().Siege);
            TuningFiles.Write(new DataDefinition[] { siege }, _folder);
            Edit(siege, "majorWaveGapSeconds", "150.0");
            var edited = File.ReadAllText(PathOf(siege));

            Assert.That(TuningFiles.Write(new DataDefinition[] { siege }, _folder), Is.Zero, "a file already there is kept");
            Assert.That(File.ReadAllText(PathOf(siege)), Is.EqualTo(edited));
            Assert.That(TuningFiles.Write(new DataDefinition[] { siege }, _folder, overwrite: true), Is.EqualTo(1));
            Assert.That(File.ReadAllText(PathOf(siege)), Is.Not.EqualTo(edited), "only an explicit overwrite replaces it");
        }

        [Test]
        public void AFileThatDoesNotParse_IsRefused_AndTheAssetKeepsItsValues()
        {
            var siege = Copy(Real().Siege);
            var values = JsonUtility.ToJson(siege);
            Directory.CreateDirectory(_folder);
            File.WriteAllText(PathOf(siege), "{ \"majorWaveGapSeconds\": 150.0, ");
            var o = TuningFiles.Read(new DataDefinition[] { siege }, _folder);
            Assert.That(o.Refused.Count, Is.EqualTo(1), string.Join(" | ", o.Changed));
            Assert.That(o.Changed, Is.Empty);
            Assert.That(JsonUtility.ToJson(siege), Is.EqualTo(values));
        }

        [Test]
        public void AFileThatRenamesTheKey_OrEmptiesTheName_IsRefused_AndTheAssetKeepsItsValues()
        {
            var siege = Copy(Real().Siege);
            var values = JsonUtility.ToJson(siege);
            TuningFiles.Write(new DataDefinition[] { siege }, _folder);
            var original = File.ReadAllText(PathOf(siege));

            File.WriteAllText(PathOf(siege), Regex.Replace(original, "(\"key\"\\s*:\\s*)\"[^\"]*\"", "${1}\"something-else\""));
            var renamed = TuningFiles.Read(new DataDefinition[] { siege }, _folder);
            Assert.That(renamed.Refused.Count, Is.EqualTo(1));
            Assert.That(renamed.Refused[0], Does.Contain("key"));
            Assert.That(JsonUtility.ToJson(siege), Is.EqualTo(values));

            File.WriteAllText(PathOf(siege), Regex.Replace(original, "(\"displayName\"\\s*:\\s*)\"[^\"]*\"", "${1}\"\""));
            var emptied = TuningFiles.Read(new DataDefinition[] { siege }, _folder);
            Assert.That(emptied.Refused.Count, Is.EqualTo(1));
            Assert.That(emptied.Refused[0], Does.Contain("display name"));
            Assert.That(JsonUtility.ToJson(siege), Is.EqualTo(values));
        }

        [Test]
        public void NoFolder_ReadsNothing()
        {
            var o = TuningFiles.Read(new DataDefinition[] { Copy(Real().Siege) }, _folder);
            Assert.That(o.Files, Is.Zero);
            Assert.That(o.Changed, Is.Empty);
        }

        [Test]
        public void TheOfferedAssets_CoverTheThreatAssets_AndHoldNoReferencesToOtherAssets()
        {
            var real = Real();
            var offered = TuningFiles.Offered(real);
            Assert.That(offered, Does.Contain(real.Raids));
            Assert.That(offered, Does.Contain(real.Siege));
            Assert.That(offered, Does.Contain(real.Defence));
            Assert.That(offered, Does.Contain(real.Opening));
            foreach (var e in real.Enemies) Assert.That(offered, Does.Contain(e));
            foreach (var t in real.Turrets) Assert.That(offered, Does.Contain(t));
            var names = new HashSet<string>();
            foreach (var d in offered)
            {
                Assert.That(names.Add(TuningFiles.FileName(d)), Is.True, "one file per asset: " + d.name);
                // JsonUtility writes a reference to another asset as its instance id, which is not the same from one
                // run to the next; a file could not carry one.
                Assert.That(JsonUtility.ToJson(d), Does.Not.Contain("instanceID"), d.name);
            }
        }
    }
}
