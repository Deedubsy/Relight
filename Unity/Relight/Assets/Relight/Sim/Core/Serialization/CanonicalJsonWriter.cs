using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Relight.Sim
{
    /// <summary>
    /// Writes the canonical JSON text of a visited object graph (reference save.ts <c>canonicalJson</c>, lines 58–63).
    /// It is the single source of both the Unity state hash (<see cref="StateHash"/>) and, from B-11, the save file.
    ///
    /// FORMAT (stable; anything that reads it back must follow the same rules):
    /// <list type="bullet">
    /// <item>An object is <c>{"key":value,...}</c> with its keys sorted <b>ordinally</b>, ties broken by visit order,
    ///       so two structurally equal states produce the same text whatever order they were built in.</item>
    /// <item>Nothing is dropped silently: every visited field is emitted. The reference dropped <c>undefined</c> and
    ///       functions, neither of which exists here. The three reference transients (<c>events</c>, <c>acc</c>,
    ///       <c>speed</c>) are excluded structurally — events are never visited, the accumulator lives on
    ///       <see cref="Simulation"/> and there is no speed (U-D-04).</item>
    /// <item>Numbers: non-finite → <c>null</c> (reference rule). An integral double within ±2^53 prints without a
    ///       decimal point, as JavaScript <c>String(x)</c> does. Any other double prints in the <b>shortest form that
    ///       round-trips</b>, found by trying G15, G16 then G17 and keeping the first that parses back bit-identical.
    ///       That criterion is JavaScript's, so the text usually matches, but byte-identity with the TypeScript hash
    ///       is explicitly not pursued (U-M-13); what is required is that the form is stable across runtimes and
    ///       lossless. An exponent is written with a lowercase <c>e</c>.</item>
    /// <item>Strings are JSON-escaped (<c>"</c>, <c>\</c>, and control characters below 0x20); a null string,
    ///       object, list or array is <c>null</c>.</item>
    /// <item><see cref="Vec2"/> is <c>[x,y]</c>; every array and list is a JSON array in visit order.</item>
    /// </list>
    /// </summary>
    public sealed class CanonicalJsonWriter : IStateVisitor
    {
        public bool IsReading => false;

        private sealed class Frame
        {
            public readonly List<string> Keys = new List<string>();
            public readonly List<string> Values = new List<string>();
        }

        private readonly List<Frame> _stack = new List<Frame>();

        /// <summary>The canonical JSON text of one visitable object graph.</summary>
        public static string Write(IVisitable root) => new CanonicalJsonWriter().WriteObject(root);

        private string WriteObject(IVisitable obj)
        {
            if (obj == null) return "null";
            _stack.Add(new Frame());
            obj.Visit(this);
            var frame = _stack[_stack.Count - 1];
            _stack.RemoveAt(_stack.Count - 1);
            return Render(frame);
        }

        private static string Render(Frame f)
        {
            var n = f.Keys.Count;
            var order = new int[n];
            for (var i = 0; i < n; i++) order[i] = i;
            var keys = f.Keys;
            // Ordinal by key, then by visit order, so the comparison is total and the sort's instability cannot show.
            Array.Sort(order, (a, b) =>
            {
                var c = string.CompareOrdinal(keys[a], keys[b]);
                return c != 0 ? c : a.CompareTo(b);
            });
            var sb = new StringBuilder();
            sb.Append('{');
            for (var i = 0; i < n; i++)
            {
                if (i > 0) sb.Append(',');
                var k = order[i];
                Quote(sb, f.Keys[k]);
                sb.Append(':');
                sb.Append(f.Values[k]);
            }
            sb.Append('}');
            return sb.ToString();
        }

        private void Add(string name, string json)
        {
            if (_stack.Count == 0) throw new InvalidOperationException("CanonicalJsonWriter: field written outside an object.");
            var f = _stack[_stack.Count - 1];
            f.Keys.Add(name);
            f.Values.Add(json);
        }

        // ---- IStateVisitor ----

        public void Field(string name, ref bool v) => Add(name, v ? "true" : "false");
        public void Field(string name, ref int v) => Add(name, v.ToString(CultureInfo.InvariantCulture));
        public void Field(string name, ref uint v) => Add(name, v.ToString(CultureInfo.InvariantCulture));
        public void Field(string name, ref double v) => Add(name, Num(v));
        public void Field(string name, ref string v) => Add(name, v == null ? "null" : QuoteString(v));

        public void Field(string name, ref int[] v)
        {
            if (v == null) { Add(name, "null"); return; }
            var sb = new StringBuilder("[");
            for (var i = 0; i < v.Length; i++) { if (i > 0) sb.Append(','); sb.Append(v[i].ToString(CultureInfo.InvariantCulture)); }
            Add(name, sb.Append(']').ToString());
        }

        public void Field(string name, ref double[] v)
        {
            if (v == null) { Add(name, "null"); return; }
            var sb = new StringBuilder("[");
            for (var i = 0; i < v.Length; i++) { if (i > 0) sb.Append(','); sb.Append(Num(v[i])); }
            Add(name, sb.Append(']').ToString());
        }

        public void Field(string name, ref byte[] v)
        {
            if (v == null) { Add(name, "null"); return; }
            var sb = new StringBuilder("[");
            for (var i = 0; i < v.Length; i++) { if (i > 0) sb.Append(','); sb.Append(v[i].ToString(CultureInfo.InvariantCulture)); }
            Add(name, sb.Append(']').ToString());
        }

        public void Field(string name, ref Vec2 v) => Add(name, "[" + Num(v.X) + "," + Num(v.Y) + "]");

        public void Object<T>(string name, ref T obj, Func<T> make) where T : class, IVisitable
            => Add(name, WriteObject(obj));

        public void List<T>(string name, List<T> list, Func<T> make) where T : class, IVisitable
        {
            if (list == null) { Add(name, "null"); return; }
            var sb = new StringBuilder("[");
            for (var i = 0; i < list.Count; i++) { if (i > 0) sb.Append(','); sb.Append(WriteObject(list[i])); }
            Add(name, sb.Append(']').ToString());
        }

        public void StringList(string name, List<string> list)
        {
            if (list == null) { Add(name, "null"); return; }
            var sb = new StringBuilder("[");
            for (var i = 0; i < list.Count; i++) { if (i > 0) sb.Append(','); sb.Append(list[i] == null ? "null" : QuoteString(list[i])); }
            Add(name, sb.Append(']').ToString());
        }

        // ---- formatting ----

        private const double MaxExactInteger = 9007199254740992.0;   // 2^53

        /// <summary>A double in the canonical form documented on this class.</summary>
        public static string Num(double v)
        {
            if (double.IsNaN(v) || double.IsInfinity(v)) return "null";
            if (v == Math.Floor(v) && Math.Abs(v) < MaxExactInteger)
                return ((long)v).ToString(CultureInfo.InvariantCulture);
            var s = v.ToString("G15", CultureInfo.InvariantCulture);
            if (!RoundTrips(s, v))
            {
                s = v.ToString("G16", CultureInfo.InvariantCulture);
                if (!RoundTrips(s, v)) s = v.ToString("G17", CultureInfo.InvariantCulture);
            }
            return s.IndexOf('E') >= 0 ? s.Replace("E", "e") : s;
        }

        private static bool RoundTrips(string s, double v)
            => double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var back) && back == v;

        /// <summary>A JSON string literal.</summary>
        public static string QuoteString(string s)
        {
            var sb = new StringBuilder();
            Quote(sb, s);
            return sb.ToString();
        }

        private static void Quote(StringBuilder sb, string s)
        {
            sb.Append('"');
            for (var i = 0; i < s.Length; i++)
            {
                var c = s[i];
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < ' ') sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }
    }
}
