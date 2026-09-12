using System;
using System.Collections.Generic;
using System.Text;

namespace Relight.Sim
{
    /// <summary>
    /// The reading half of the <see cref="IStateVisitor"/> contract (B-11): it walks a parsed
    /// <see cref="JsonValue"/> document and fills a visitable graph from it, so that
    /// <c>Read(Write(x))</c> is exactly <c>x</c> for everything the visitor covers.
    ///
    /// It mirrors <see cref="CanonicalJsonWriter"/> field for field: an object is a JSON object keyed by the visited
    /// names, a list is a JSON array, <see cref="Vec2"/> is <c>[x,y]</c>, a null object/list/string is <c>null</c>.
    /// Key order is irrelevant on the way in — the writer sorts, the reader looks up.
    ///
    /// Nothing is guessed. A member that is missing, null where an object is not allowed, or of the wrong kind is
    /// recorded in <see cref="Problems"/> with its full path and the field keeps its constructed default; the
    /// caller (<see cref="SaveSerializer"/>) refuses the file when any problem was recorded. A save from a build
    /// with different fields therefore fails loudly instead of loading a half-built state — there is no migration
    /// path (TECHNICAL_ARCHITECTURE.md §9.3, U-D-27).
    /// </summary>
    public sealed class JsonStateReader : IStateVisitor
    {
        /// <summary>Problems stop accumulating past this many; the first is the one reported.</summary>
        public const int MaxProblems = 16;

        private readonly List<JsonValue> _stack = new List<JsonValue>();
        private readonly List<string> _path = new List<string>();
        private readonly List<string> _problems = new List<string>();

        public bool IsReading => true;

        /// <summary>Every field that was missing or of the wrong shape, in visit order.</summary>
        public IReadOnlyList<string> Problems => _problems;
        public bool Ok => _problems.Count == 0;

        /// <summary>
        /// Fills <paramref name="target"/> from <paramref name="obj"/>. Returns true when nothing was missing or
        /// malformed; otherwise <paramref name="problem"/> names the first thing that was wrong.
        /// </summary>
        public static bool Read(JsonValue obj, IVisitable target, out string problem)
        {
            problem = null;
            if (target == null) { problem = "nothing to read into"; return false; }
            if (obj == null || obj.Kind != JsonKind.Object) { problem = "the saved state is not an object"; return false; }
            var r = new JsonStateReader();
            r._stack.Add(obj);
            target.Visit(r);
            r._stack.RemoveAt(r._stack.Count - 1);
            if (r.Ok) return true;
            problem = r._problems[0];
            return false;
        }

        private JsonValue Current => _stack[_stack.Count - 1];

        private void Problem(string name, string what)
        {
            if (_problems.Count >= MaxProblems) return;
            var sb = new StringBuilder();
            for (var i = 0; i < _path.Count; i++) { sb.Append(_path[i]); sb.Append('.'); }
            sb.Append(name);
            _problems.Add(what + " '" + sb + "'");
        }

        /// <summary>The member, or null with a recorded problem when it is absent.</summary>
        private JsonValue Take(string name)
        {
            var m = Current.Member(name);
            if (m == null) Problem(name, "missing field");
            return m;
        }

        private JsonValue TakeOfKind(string name, JsonKind kind)
        {
            var m = Take(name);
            if (m == null) return null;
            if (m.Kind != kind) { Problem(name, "wrong kind of value for field"); return null; }
            return m;
        }

        // ---- scalars ----

        public void Field(string name, ref bool v)
        {
            var m = TakeOfKind(name, JsonKind.Bool);
            if (m != null) v = m.Bool;
        }

        public void Field(string name, ref int v)
        {
            var m = TakeOfKind(name, JsonKind.Number);
            if (m != null) v = ToInt(m.Number);
        }

        public void Field(string name, ref uint v)
        {
            var m = TakeOfKind(name, JsonKind.Number);
            if (m == null) return;
            var n = m.Number;
            if (n < 0 || n > uint.MaxValue || n != Math.Floor(n)) { Problem(name, "not a whole 32-bit value for field"); return; }
            v = (uint)n;
        }

        public void Field(string name, ref double v)
        {
            var m = Take(name);
            if (m == null) return;
            // The writer emits a non-finite double as null (CanonicalJsonWriter's reference rule); read it back as NaN.
            if (m.Kind == JsonKind.Null) { v = double.NaN; return; }
            if (m.Kind != JsonKind.Number) { Problem(name, "wrong kind of value for field"); return; }
            v = m.Number;
        }

        public void Field(string name, ref string v)
        {
            var m = Take(name);
            if (m == null) return;
            if (m.Kind == JsonKind.Null) { v = null; return; }
            if (m.Kind != JsonKind.String) { Problem(name, "wrong kind of value for field"); return; }
            v = m.Text;
        }

        // ---- arrays ----

        public void Field(string name, ref int[] v)
        {
            var a = TakeArray(name, out var isNull);
            if (isNull) { v = null; return; }
            if (a == null) return;
            var outArray = new int[a.Count];
            for (var i = 0; i < a.Count; i++)
            {
                var e = a.At(i);
                if (e == null || e.Kind != JsonKind.Number) { Problem(name, "a non-number in array field"); return; }
                outArray[i] = ToInt(e.Number);
            }
            v = outArray;
        }

        public void Field(string name, ref double[] v)
        {
            var a = TakeArray(name, out var isNull);
            if (isNull) { v = null; return; }
            if (a == null) return;
            var outArray = new double[a.Count];
            for (var i = 0; i < a.Count; i++)
            {
                var e = a.At(i);
                if (e == null) { Problem(name, "a missing element in array field"); return; }
                // The writer emits a non-finite double as null (CanonicalJsonWriter); read it back as NaN.
                if (e.Kind == JsonKind.Null) { outArray[i] = double.NaN; continue; }
                if (e.Kind != JsonKind.Number) { Problem(name, "a non-number in array field"); return; }
                outArray[i] = e.Number;
            }
            v = outArray;
        }

        public void Field(string name, ref byte[] v)
        {
            var a = TakeArray(name, out var isNull);
            if (isNull) { v = null; return; }
            if (a == null) return;
            var outArray = new byte[a.Count];
            for (var i = 0; i < a.Count; i++)
            {
                var e = a.At(i);
                if (e == null || e.Kind != JsonKind.Number || e.Number < 0 || e.Number > 255 || e.Number != Math.Floor(e.Number))
                { Problem(name, "a non-byte in array field"); return; }
                outArray[i] = (byte)e.Number;
            }
            v = outArray;
        }

        public void Field(string name, ref Vec2 v)
        {
            var a = TakeArray(name, out var isNull);
            if (isNull) { Problem(name, "null where a point was expected in field"); return; }
            if (a == null) return;
            if (a.Count != 2) { Problem(name, "a point that is not two numbers in field"); return; }
            var x = a.At(0);
            var y = a.At(1);
            if (x == null || y == null || x.Kind != JsonKind.Number || y.Kind != JsonKind.Number)
            { Problem(name, "a point that is not two numbers in field"); return; }
            v = new Vec2(x.Number, y.Number);
        }

        private JsonValue TakeArray(string name, out bool isNull)
        {
            isNull = false;
            var m = Take(name);
            if (m == null) return null;
            if (m.Kind == JsonKind.Null) { isNull = true; return null; }
            if (m.Kind != JsonKind.Array) { Problem(name, "wrong kind of value for field"); return null; }
            return m;
        }

        // ---- composites ----

        public void Object<T>(string name, ref T obj, Func<T> make) where T : class, IVisitable
        {
            var m = Take(name);
            if (m == null) return;
            if (m.Kind == JsonKind.Null) { obj = null; return; }
            if (m.Kind != JsonKind.Object) { Problem(name, "wrong kind of value for field"); return; }
            if (obj == null)
            {
                if (make == null) { Problem(name, "no way to build field"); return; }
                obj = make();
                if (obj == null) { Problem(name, "no way to build field"); return; }
            }
            Descend(name, m, obj);
        }

        public void List<T>(string name, List<T> list, Func<T> make) where T : class, IVisitable
        {
            var m = Take(name);
            if (m == null) return;
            if (list == null) return;                       // a null target list cannot be filled; the writer wrote null too
            if (m.Kind == JsonKind.Null) { list.Clear(); return; }
            if (m.Kind != JsonKind.Array) { Problem(name, "wrong kind of value for field"); return; }
            list.Clear();
            for (var i = 0; i < m.Count; i++)
            {
                var e = m.At(i);
                if (e == null || e.Kind != JsonKind.Object) { Problem(name, "a non-object element in list field"); return; }
                if (make == null) { Problem(name, "no way to build elements of field"); return; }
                var item = make();
                if (item == null) { Problem(name, "no way to build elements of field"); return; }
                Descend(name + "[" + i.ToString(System.Globalization.CultureInfo.InvariantCulture) + "]", e, item);
                list.Add(item);
            }
        }

        public void StringList(string name, List<string> list)
        {
            var m = Take(name);
            if (m == null) return;
            if (list == null) return;
            if (m.Kind == JsonKind.Null) { list.Clear(); return; }
            if (m.Kind != JsonKind.Array) { Problem(name, "wrong kind of value for field"); return; }
            list.Clear();
            for (var i = 0; i < m.Count; i++)
            {
                var e = m.At(i);
                if (e == null) { Problem(name, "a missing element in list field"); return; }
                if (e.Kind == JsonKind.Null) { list.Add(null); continue; }
                if (e.Kind != JsonKind.String) { Problem(name, "a non-string element in list field"); return; }
                list.Add(e.Text);
            }
        }

        private void Descend(string name, JsonValue obj, IVisitable target)
        {
            _path.Add(name);
            _stack.Add(obj);
            target.Visit(this);
            _stack.RemoveAt(_stack.Count - 1);
            _path.RemoveAt(_path.Count - 1);
        }

        private static int ToInt(double n)
        {
            if (double.IsNaN(n)) return 0;
            if (n >= int.MaxValue) return int.MaxValue;
            if (n <= int.MinValue) return int.MinValue;
            return (int)n;
        }
    }
}
