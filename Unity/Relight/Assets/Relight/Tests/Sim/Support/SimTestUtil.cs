using System;
using System.Collections.Generic;

namespace Relight.Sim.Tests.Support
{
    /// <summary>
    /// Small helpers the B-12 checks share and the other test folders may adopt. Nothing here knows about a
    /// particular subsystem; anything that does belongs in that subsystem's own fixture
    /// (<c>Tests/Sim/Core/CoreTestSupport.cs</c>, <c>Tests/Sim/Actor/InventoryFixture.cs</c>,
    /// <c>Tests/Sim/World/WorldTestSupport.cs</c>).
    /// </summary>
    public static class SimTestUtil
    {
        /// <summary>Step <paramref name="ticks"/> whole ticks through the driver, dropping events as a host would.</summary>
        public static Simulation Step(Simulation sim, int ticks)
        {
            for (var i = 0; i < ticks; i++)
            {
                sim.Tick();
                sim.State.Events.Clear();
            }
            return sim;
        }

        /// <summary>The canonical JSON the hash is taken over — the text to diff when two hashes disagree.</summary>
        public static string Canonical(SimState st) => CanonicalJsonWriter.Write(st);

        /// <summary>The conservation verdict, as the one-line-per-item text the ledger produces ("" when it balances).</summary>
        public static string ConservationProblems(SimContext ctx, SimState st) =>
            string.Join(" | ", Ledger.Conservation(st, ctx.Data).Problems);

        /// <summary>
        /// The first difference between two canonical states, as a short excerpt, so a failing hash comparison says
        /// <i>where</i> the two runs parted rather than only that they did.
        /// </summary>
        public static string FirstDifference(string a, string b)
        {
            if (string.Equals(a, b, StringComparison.Ordinal)) return "";
            var n = Math.Min(a.Length, b.Length);
            var i = 0;
            while (i < n && a[i] == b[i]) i++;
            var from = Math.Max(0, i - 60);
            return $"at offset {i}:\n  a: …{a.Substring(from, Math.Min(140, a.Length - from))}\n  b: …{b.Substring(from, Math.Min(140, b.Length - from))}";
        }

        /// <summary>
        /// Every field name the state visits, in visit order and fully qualified ("engineer/pos", "stats/made/n").
        /// Used to assert that the canonical text drops nothing: a field the writer forgot would be missing here too,
        /// which is the class of defect contract H1 exists to catch (TECHNICAL_ARCHITECTURE.md §10.4).
        /// </summary>
        public static List<string> FieldNames(IVisitable root)
        {
            var v = new NameRecordingVisitor();
            v.Enter(root);
            return v.Names;
        }

        /// <summary>Swaps the two commands at <paramref name="i"/> and <paramref name="j"/>, keeping the ticks where they are.</summary>
        public static List<LoggedCommand> SwapCommands(IReadOnlyList<LoggedCommand> log, int i, int j)
        {
            var copy = new List<LoggedCommand>(log.Count);
            for (var k = 0; k < log.Count; k++) copy.Add(log[k]);
            var a = copy[i];
            var b = copy[j];
            copy[i] = new LoggedCommand(a.Tick, b.Command);
            copy[j] = new LoggedCommand(b.Tick, a.Command);
            return copy;
        }

        /// <summary>A visitor that records field paths and touches nothing (writing side, like the canonical writer).</summary>
        private sealed class NameRecordingVisitor : IStateVisitor
        {
            public readonly List<string> Names = new List<string>();
            private readonly List<string> _path = new List<string>();

            public bool IsReading => false;

            public void Enter(IVisitable obj)
            {
                if (obj == null) return;
                obj.Visit(this);
            }

            private string Qualify(string name)
            {
                if (_path.Count == 0) return name;
                return string.Join("/", _path) + "/" + name;
            }

            private void Leaf(string name) => Names.Add(Qualify(name));

            public void Field(string name, ref bool v) => Leaf(name);
            public void Field(string name, ref int v) => Leaf(name);
            public void Field(string name, ref uint v) => Leaf(name);
            public void Field(string name, ref double v) => Leaf(name);
            public void Field(string name, ref string v) => Leaf(name);
            public void Field(string name, ref int[] v) => Leaf(name);
            public void Field(string name, ref double[] v) => Leaf(name);
            public void Field(string name, ref byte[] v) => Leaf(name);
            public void Field(string name, ref Vec2 v) => Leaf(name);

            public void Object<T>(string name, ref T obj, Func<T> make) where T : class, IVisitable
            {
                Leaf(name);
                if (obj == null) return;
                _path.Add(name);
                obj.Visit(this);
                _path.RemoveAt(_path.Count - 1);
            }

            public void List<T>(string name, List<T> list, Func<T> make) where T : class, IVisitable
            {
                Leaf(name);
                if (list == null) return;
                for (var i = 0; i < list.Count; i++)
                {
                    if (list[i] == null) continue;
                    _path.Add(name + "[" + i.ToString(System.Globalization.CultureInfo.InvariantCulture) + "]");
                    list[i].Visit(this);
                    _path.RemoveAt(_path.Count - 1);
                }
            }

            public void StringList(string name, List<string> list) => Leaf(name);
        }
    }
}
