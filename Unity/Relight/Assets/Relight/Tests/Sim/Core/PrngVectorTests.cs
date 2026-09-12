using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using NUnit.Framework;

namespace Relight.Sim.Tests.Core
{
    /// <summary>
    /// The PRNG must be bit-exact with the reference (MIGRATION_MAP.md S-04; TECHNICAL_ARCHITECTURE.md §10.2/§10.6
    /// check 1). Every expected value in Fixtures/prng-vectors.txt was produced by running the reference
    /// packages/sim/src/prng.ts under node (scratchpad/B03/genvectors.mts) and printing it with JavaScript
    /// <c>String(x)</c>, which is the shortest form that round-trips — so <c>double.Parse</c> recovers the exact
    /// double and the comparison below is exact, not approximate. mulberry32's final division by 2^32 is exact in
    /// doubles, so there is no rounding for a tolerance to hide.
    /// </summary>
    public sealed class PrngVectorTests
    {
        private static Dictionary<string, List<string>> _sections;

        private static Dictionary<string, List<string>> Sections()
        {
            if (_sections != null) return _sections;
            var path = CoreFixtures.Path("prng-vectors.txt");
            var sections = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            List<string> current = null;
            foreach (var raw in File.ReadAllLines(path))
            {
                var line = raw.Trim();
                if (line.Length == 0) continue;
                if (line[0] == '#')
                {
                    // "# <name> <...> <count>". The whole header is the key, count included, because two sections
                    // differ only by their count ("next seed=3 10000" and "next seed=3 100"). A comment line with no
                    // trailing integer is file prose, not a section.
                    var header = line.Substring(1).Trim();
                    var parts = header.Split(' ');
                    if (parts.Length < 2 || !int.TryParse(parts[parts.Length - 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                    {
                        current = null;
                        continue;
                    }
                    current = new List<string>();
                    sections[header] = current;
                    continue;
                }
                current?.Add(line);
            }
            return _sections = sections;
        }

        private static List<string> Section(string name)
        {
            Assert.IsTrue(Sections().ContainsKey(name), $"fixture section '{name}' missing");
            return Sections()[name];
        }

        private static double D(string s) => double.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture);

        private static void AssertExact(double expected, double actual, string what)
        {
            if (expected.Equals(actual)) return;
            Assert.Fail($"{what}: expected {expected.ToString("R", CultureInfo.InvariantCulture)}, got {actual.ToString("R", CultureInfo.InvariantCulture)}");
        }

        [Test]
        public void SeedRngMatchesReference()
        {
            var seeds = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, -1 };
            var expected = Section("seedrng 12");
            Assert.AreEqual(seeds.Length, expected.Count);
            for (var i = 0; i < seeds.Length; i++)
                Assert.AreEqual(uint.Parse(expected[i], CultureInfo.InvariantCulture), Prng.Seed(seeds[i]), $"seedRng({seeds[i]})");
        }

        [Test]
        public void TenThousandValuesFromSeedThreeMatchReference()
        {
            var expected = Section("next seed=3 10000");
            Assert.AreEqual(10000, expected.Count, "the fixture must hold 10,000 values");
            var state = Prng.Seed(3);
            for (var i = 0; i < expected.Count; i++)
                AssertExact(D(expected[i]), Prng.Next(ref state), $"seed 3 value {i}");

            var after = Section("rngstate-after seed=3 1");
            Assert.AreEqual(1, after.Count);
            Assert.AreEqual(uint.Parse(after[0], CultureInfo.InvariantCulture), state, "rng word after 10,000 draws");
        }

        [Test]
        public void SeedsOneToTenMatchReference()
        {
            var compared = 0;
            for (var seed = 1; seed <= 10; seed++)
            {
                var expected = Section($"next seed={seed} 100");
                Assert.AreEqual(100, expected.Count, $"seed {seed} vector length");
                var state = Prng.Seed(seed);
                for (var i = 0; i < expected.Count; i++, compared++)
                    AssertExact(D(expected[i]), Prng.Next(ref state), $"seed {seed} value {i}");
            }
            Assert.AreEqual(1000, compared);
        }

        [Test]
        public void RngIntMatchesReference()
        {
            var expected = Section("rngint seed=7 n=13 100");
            var state = Prng.Seed(7);
            for (var i = 0; i < expected.Count; i++)
                Assert.AreEqual(int.Parse(expected[i], CultureInfo.InvariantCulture), Prng.Int(ref state, 13), $"rngInt value {i}");
        }

        [Test]
        public void RngUniformMatchesReference()
        {
            var expected = Section("rnguniform seed=11 lo=-3 hi=5 100");
            var state = Prng.Seed(11);
            for (var i = 0; i < expected.Count; i++)
                AssertExact(D(expected[i]), Prng.Uniform(ref state, -3, 5), $"rngUniform value {i}");
        }

        [Test]
        public void Hash01MatchesReferenceOverAGrid()
        {
            var expected = Section("hash01 grid seeds=1,3,9 ts=0,1,7,600 x=0..4 y=0..4 300");
            var k = 0;
            foreach (var s in new[] { 1, 3, 9 })
                foreach (var t in new[] { 0, 1, 7, 600 })
                    for (var x = 0; x < 5; x++)
                        for (var y = 0; y < 5; y++, k++)
                            AssertExact(D(expected[k]), Prng.Hash01(s, t, x, y), $"hash01({s},{t},{x},{y})");
            Assert.AreEqual(expected.Count, k);
        }

        [Test]
        public void PyRoundMatchesReference()
        {
            var expected = Section("pyround 168");
            Assert.AreEqual(0, expected.Count % 2, "the section holds input/output pairs");
            for (var i = 0; i < expected.Count; i += 2)
            {
                var input = D(expected[i]);
                AssertExact(D(expected[i + 1]), Prng.PyRound(input), $"pyRound({expected[i]})");
            }
        }

        [Test]
        public void RngStateIsTheOnlyPrngState()
        {
            // The same word produces the same stream wherever it is held: no hidden static state (§10.5).
            var a = Prng.Seed(42);
            var b = Prng.Seed(42);
            for (var i = 0; i < 50; i++) AssertExact(Prng.Next(ref a), Prng.Next(ref b), $"parallel draw {i}");
        }
    }
}
