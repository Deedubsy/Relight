using System.Collections.Generic;
using NUnit.Framework;

namespace Relight.Sim.Tests.Core
{
    /// <summary>
    /// The canonical writer and the Unity state hash (reference save.ts lines 58–75; TECHNICAL_ARCHITECTURE.md
    /// §10.3). The exact-text checks pin the format B-11 will read back: ordinal key order, nothing dropped,
    /// non-finite numbers as null, integral doubles without a decimal point, nulls round-tripping as null.
    /// </summary>
    public sealed class HashTests
    {
        private sealed class Nested : IVisitable
        {
            public int N;
            public void Visit(IStateVisitor v) => v.Field("n", ref N);
        }

        /// <summary>Fields are visited in a deliberately unsorted order; the writer must sort them.</summary>
        private sealed class Sample : IVisitable
        {
            public string Zebra = "z";
            public int Alpha = 1;
            public double Middle = 2.5;
            public Vec2 Point = new Vec2(1.5, -2);
            public string NullString;
            public Nested Child = new Nested { N = 7 };
            public Nested NullChild;
            public int[] Ints = { 3, 1, 2 };
            public List<Nested> Kids = new List<Nested> { new Nested { N = 1 }, new Nested { N = 2 } };
            public List<string> Words = new List<string> { "b", "a" };
            public bool Flag = true;
            public uint Word = 4294967295u;

            public void Visit(IStateVisitor v)
            {
                v.Field("zebra", ref Zebra);
                v.Field("alpha", ref Alpha);
                v.Field("middle", ref Middle);
                v.Field("point", ref Point);
                v.Field("nullString", ref NullString);
                v.Object("child", ref Child, () => new Nested());
                v.Object("nullChild", ref NullChild, () => new Nested());
                v.Field("ints", ref Ints);
                v.List("kids", Kids, () => new Nested());
                v.StringList("words", Words);
                v.Field("flag", ref Flag);
                v.Field("word", ref Word);
            }
        }

        private sealed class Numbers : IVisitable
        {
            public double A, B, C, D, E;
            public void Visit(IStateVisitor v)
            {
                v.Field("a", ref A); v.Field("b", ref B); v.Field("c", ref C); v.Field("d", ref D); v.Field("e", ref E);
            }
        }

        [Test]
        public void HandBuiltObjectGivesTheExactCanonicalText()
        {
            const string expected =
                "{\"alpha\":1," +
                "\"child\":{\"n\":7}," +
                "\"flag\":true," +
                "\"ints\":[3,1,2]," +
                "\"kids\":[{\"n\":1},{\"n\":2}]," +
                "\"middle\":2.5," +
                "\"nullChild\":null," +
                "\"nullString\":null," +
                "\"point\":[1.5,-2]," +
                "\"word\":4294967295," +
                "\"words\":[\"b\",\"a\"]," +
                "\"zebra\":\"z\"}";
            Assert.AreEqual(expected, CanonicalJsonWriter.Write(new Sample()));
        }

        [Test]
        public void KeysAreSortedOrdinallyNotByCulture()
        {
            // Ordinal puts every upper-case letter before every lower-case one; a culture-aware sort would not.
            var text = CanonicalJsonWriter.Write(new Sample());
            Assert.Less(text.IndexOf("\"alpha\""), text.IndexOf("\"zebra\""));
            Assert.Less(text.IndexOf("\"nullChild\""), text.IndexOf("\"nullString\""), "'C' < 'S' ordinally");
            Assert.Less(text.IndexOf("\"word\""), text.IndexOf("\"words\""), "a prefix sorts before its extension");
        }

        [Test]
        public void NumbersFollowTheDocumentedRules()
        {
            var n = new Numbers
            {
                A = 3,                       // integral -> no decimal point
                B = -0.0,                    // negative zero -> "0", as JavaScript String(-0) gives
                C = double.NaN,              // non-finite -> null
                D = double.PositiveInfinity, // non-finite -> null
                E = 0.1 + 0.2,               // must round-trip exactly
            };
            var text = CanonicalJsonWriter.Write(n);
            Assert.AreEqual("{\"a\":3,\"b\":0,\"c\":null,\"d\":null,\"e\":0.30000000000000004}", text);
        }

        [Test]
        public void EveryDoubleRoundTripsThroughTheCanonicalForm()
        {
            var state = Prng.Seed(5);
            for (var i = 0; i < 2000; i++)
            {
                var v = Prng.Uniform(ref state, -1e6, 1e6) * System.Math.Pow(10, Prng.Int(ref state, 20) - 10);
                var text = CanonicalJsonWriter.Num(v);
                Assert.IsTrue(double.TryParse(text, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var back), $"'{text}' does not parse");
                Assert.AreEqual(v, back, 0.0, $"'{text}' does not round-trip");
            }
        }

        [Test]
        public void StringsAreJsonEscaped()
        {
            Assert.AreEqual("\"a\\\"b\\\\c\\nd\\u0001e\"", CanonicalJsonWriter.QuoteString("a\"b\\c\nde"));
        }

        [Test]
        public void FnvMatchesTheReferenceAlgorithm()
        {
            // FNV-1a 32-bit over UTF-16 code units, 8 lowercase hex digits. The empty string is the offset basis.
            Assert.AreEqual("811c9dc5", StateHash.Of(""));
            Assert.AreEqual(8, StateHash.Of("{}").Length);
            Assert.AreNotEqual(StateHash.Of("{\"a\":1}"), StateHash.Of("{\"a\":2}"));
            foreach (var c in StateHash.Of("{\"a\":1}")) Assert.IsTrue(char.IsDigit(c) || (c >= 'a' && c <= 'f'), "lowercase hex only");
        }

        [Test]
        public void StateHashChangesWithSimTimeAndIsStableOtherwise()
        {
            var sim = Simulation.NewGame(CoreTest.Context(), 4);
            var before = sim.Hash();
            Assert.AreEqual(before, sim.Hash());

            sim.State.T += 0.05;
            var after = sim.Hash();
            Assert.AreNotEqual(before, after, "T is part of the hashed state");

            sim.State.T -= 0.05;
            Assert.AreEqual(before, sim.Hash(), "restoring the field restores the hash");
        }

        [Test]
        public void EventsAreNotHashed()
        {
            var sim = Simulation.NewGame(CoreTest.Context(), 4);
            var before = sim.Hash();
            sim.State.Events.Add(new EngineerUpEvent(sim.State.T));
            Assert.AreEqual(before, sim.Hash(), "events are transient and never visited (reference SAVE_TRANSIENT)");
            sim.DrainEvents(null);
        }

        [Test]
        public void TheAccumulatorIsNotPartOfTheState()
        {
            var sim = Simulation.NewGame(CoreTest.Context(), 4);
            sim.Advance(0.03);                    // 0.6 of a tick carried, no tick run
            Assert.AreEqual(0, sim.State.Tick);
            Assert.AreEqual(Simulation.NewGame(CoreTest.Context(), 4).Hash(), sim.Hash(),
                "the tick accumulator is host state, not sim state (reference SAVE_TRANSIENT 'acc')");
        }
    }
}
