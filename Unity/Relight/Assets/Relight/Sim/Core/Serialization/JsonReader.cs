using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Relight.Sim
{
    /// <summary>What a <see cref="JsonValue"/> holds.</summary>
    public enum JsonKind
    {
        Null,
        Bool,
        Number,
        String,
        Array,
        Object,
    }

    /// <summary>
    /// A tiny, allocation-simple JSON document (B-11). It exists because <c>Relight.Sim</c> has no engine and no
    /// serialisation package: <c>System.Text.Json</c> and Newtonsoft are not referenced by the assembly, and
    /// <c>JsonUtility</c> is an engine type that cannot represent what <see cref="SimState"/> needs
    /// (TECHNICAL_ARCHITECTURE.md §9.1). It is the exact inverse of <see cref="CanonicalJsonWriter"/>:
    /// everything that writer emits parses back here to the same value, bit for bit for doubles.
    ///
    /// It is a strict reader by design — no comments, no trailing commas, no NaN/Infinity literals, no duplicate
    /// keys, no trailing content — because the only files it reads are ones this build wrote, and anything else
    /// must be refused with a reason rather than half-understood (TECHNICAL_ARCHITECTURE.md §9.3: no migrations,
    /// no reference-save reader).
    /// </summary>
    public sealed class JsonValue
    {
        /// <summary>Nesting depth a document may reach before it is refused; a guard against hostile input.</summary>
        public const int MaxDepth = 64;

        private static readonly JsonValue NullValue = new JsonValue { Kind = JsonKind.Null };

        private List<string> _keys;
        private List<JsonValue> _values;
        private Dictionary<string, int> _index;   // lookup only; never iterated (TECHNICAL_ARCHITECTURE.md §10.5)

        public JsonKind Kind { get; private set; }
        public bool Bool { get; private set; }
        public double Number { get; private set; }
        public string Text { get; private set; }

        /// <summary>Array elements in order; null unless <see cref="Kind"/> is <see cref="JsonKind.Array"/>.</summary>
        public IReadOnlyList<JsonValue> Items => _values;

        /// <summary>Member count of an object or element count of an array; 0 otherwise.</summary>
        public int Count => _values == null ? 0 : _values.Count;

        public bool IsNull => Kind == JsonKind.Null;
        public bool IsObject => Kind == JsonKind.Object;
        public bool IsArray => Kind == JsonKind.Array;

        /// <summary>The member with this name, or null when the value is not an object or has no such member.</summary>
        public JsonValue Member(string name)
        {
            if (Kind != JsonKind.Object || _keys == null) return null;
            if (_index == null)
            {
                _index = new Dictionary<string, int>(_keys.Count, StringComparer.Ordinal);
                for (var i = 0; i < _keys.Count; i++) _index[_keys[i]] = i;
            }
            return _index.TryGetValue(name, out var at) ? _values[at] : null;
        }

        /// <summary>The member's text, or <paramref name="fallback"/> when it is absent or not a string.</summary>
        public string TextOf(string name, string fallback = null)
        {
            var m = Member(name);
            return m != null && m.Kind == JsonKind.String ? m.Text : fallback;
        }

        /// <summary>The member's number, or <paramref name="fallback"/> when it is absent or not a number.</summary>
        public double NumberOf(string name, double fallback = 0)
        {
            var m = Member(name);
            return m != null && m.Kind == JsonKind.Number ? m.Number : fallback;
        }

        /// <summary>The element at <paramref name="i"/>, or null when out of range.</summary>
        public JsonValue At(int i) => _values != null && i >= 0 && i < _values.Count ? _values[i] : null;

        /// <summary>
        /// Parses a whole document. Returns null and sets <paramref name="error"/> on any malformed input; the
        /// message names the character offset so a truncated file reports where it stopped.
        /// </summary>
        public static JsonValue Parse(string text, out string error)
        {
            if (text == null) { error = "no text"; return null; }
            var p = new Parser(text);
            var v = p.ParseDocument(out error);
            return error == null ? v : null;
        }

        // ---- construction ----

        private static JsonValue Scalar(JsonKind kind) => new JsonValue { Kind = kind };

        private sealed class Parser
        {
            private readonly string _s;
            private int _i;

            public Parser(string s) { _s = s; _i = 0; }

            public JsonValue ParseDocument(out string error)
            {
                error = null;
                SkipWhitespace();
                var v = ParseValue(0, ref error);
                if (error != null) return null;
                SkipWhitespace();
                if (_i != _s.Length) { error = Fail("unexpected text after the document"); return null; }
                return v;
            }

            private string Fail(string what) => what + " at character " + _i.ToString(CultureInfo.InvariantCulture);

            private void SkipWhitespace()
            {
                while (_i < _s.Length)
                {
                    var c = _s[_i];
                    if (c == ' ' || c == '\t' || c == '\n' || c == '\r') _i++;
                    else break;
                }
            }

            private JsonValue ParseValue(int depth, ref string error)
            {
                if (depth > MaxDepth) { error = Fail("too deeply nested"); return null; }
                if (_i >= _s.Length) { error = Fail("the document ends early"); return null; }
                var c = _s[_i];
                switch (c)
                {
                    case '{': return ParseObject(depth, ref error);
                    case '[': return ParseArray(depth, ref error);
                    case '"':
                        {
                            var s = ParseString(ref error);
                            if (error != null) return null;
                            var v = Scalar(JsonKind.String);
                            v.Text = s;
                            return v;
                        }
                    case 't':
                        if (Literal("true")) { var v = Scalar(JsonKind.Bool); v.Bool = true; return v; }
                        error = Fail("unrecognised value"); return null;
                    case 'f':
                        if (Literal("false")) { var v = Scalar(JsonKind.Bool); v.Bool = false; return v; }
                        error = Fail("unrecognised value"); return null;
                    case 'n':
                        if (Literal("null")) return NullValue;
                        error = Fail("unrecognised value"); return null;
                    default:
                        return ParseNumber(ref error);
                }
            }

            private bool Literal(string word)
            {
                if (_i + word.Length > _s.Length) return false;
                for (var k = 0; k < word.Length; k++) if (_s[_i + k] != word[k]) return false;
                _i += word.Length;
                return true;
            }

            private JsonValue ParseObject(int depth, ref string error)
            {
                _i++;                       // '{'
                var v = Scalar(JsonKind.Object);
                v._keys = new List<string>();
                v._values = new List<JsonValue>();
                SkipWhitespace();
                if (_i < _s.Length && _s[_i] == '}') { _i++; return v; }
                while (true)
                {
                    SkipWhitespace();
                    if (_i >= _s.Length || _s[_i] != '"') { error = Fail("expected a member name"); return null; }
                    var key = ParseString(ref error);
                    if (error != null) return null;
                    SkipWhitespace();
                    if (_i >= _s.Length || _s[_i] != ':') { error = Fail("expected ':'"); return null; }
                    _i++;
                    SkipWhitespace();
                    var member = ParseValue(depth + 1, ref error);
                    if (error != null) return null;
                    for (var k = 0; k < v._keys.Count; k++)
                        if (string.CompareOrdinal(v._keys[k], key) == 0) { error = Fail("duplicate member '" + key + "'"); return null; }
                    v._keys.Add(key);
                    v._values.Add(member);
                    SkipWhitespace();
                    if (_i >= _s.Length) { error = Fail("the object is not closed"); return null; }
                    if (_s[_i] == ',') { _i++; continue; }
                    if (_s[_i] == '}') { _i++; return v; }
                    error = Fail("expected ',' or '}'");
                    return null;
                }
            }

            private JsonValue ParseArray(int depth, ref string error)
            {
                _i++;                       // '['
                var v = Scalar(JsonKind.Array);
                v._values = new List<JsonValue>();
                SkipWhitespace();
                if (_i < _s.Length && _s[_i] == ']') { _i++; return v; }
                while (true)
                {
                    SkipWhitespace();
                    var element = ParseValue(depth + 1, ref error);
                    if (error != null) return null;
                    v._values.Add(element);
                    SkipWhitespace();
                    if (_i >= _s.Length) { error = Fail("the array is not closed"); return null; }
                    if (_s[_i] == ',') { _i++; continue; }
                    if (_s[_i] == ']') { _i++; return v; }
                    error = Fail("expected ',' or ']'");
                    return null;
                }
            }

            private string ParseString(ref string error)
            {
                _i++;                       // '"'
                var sb = new StringBuilder();
                while (true)
                {
                    if (_i >= _s.Length) { error = Fail("the string is not closed"); return null; }
                    var c = _s[_i++];
                    if (c == '"') return sb.ToString();
                    if (c < ' ') { error = Fail("a control character inside a string"); return null; }
                    if (c != '\\') { sb.Append(c); continue; }
                    if (_i >= _s.Length) { error = Fail("the string is not closed"); return null; }
                    var e = _s[_i++];
                    switch (e)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            {
                                if (_i + 4 > _s.Length) { error = Fail("a short \\u escape"); return null; }
                                var code = 0;
                                for (var k = 0; k < 4; k++)
                                {
                                    var h = _s[_i + k];
                                    int d;
                                    if (h >= '0' && h <= '9') d = h - '0';
                                    else if (h >= 'a' && h <= 'f') d = h - 'a' + 10;
                                    else if (h >= 'A' && h <= 'F') d = h - 'A' + 10;
                                    else { error = Fail("a bad \\u escape"); return null; }
                                    code = code * 16 + d;
                                }
                                _i += 4;
                                sb.Append((char)code);
                                break;
                            }
                        default:
                            error = Fail("an unknown escape");
                            return null;
                    }
                }
            }

            private JsonValue ParseNumber(ref string error)
            {
                var start = _i;
                if (_i < _s.Length && _s[_i] == '-') _i++;
                var digits = 0;
                while (_i < _s.Length && _s[_i] >= '0' && _s[_i] <= '9') { _i++; digits++; }
                if (digits == 0) { _i = start; error = Fail("expected a value"); return null; }
                if (_i < _s.Length && _s[_i] == '.')
                {
                    _i++;
                    var frac = 0;
                    while (_i < _s.Length && _s[_i] >= '0' && _s[_i] <= '9') { _i++; frac++; }
                    if (frac == 0) { error = Fail("a number with no digits after the point"); return null; }
                }
                if (_i < _s.Length && (_s[_i] == 'e' || _s[_i] == 'E'))
                {
                    _i++;
                    if (_i < _s.Length && (_s[_i] == '+' || _s[_i] == '-')) _i++;
                    var exp = 0;
                    while (_i < _s.Length && _s[_i] >= '0' && _s[_i] <= '9') { _i++; exp++; }
                    if (exp == 0) { error = Fail("a number with no digits in its exponent"); return null; }
                }
                var text = _s.Substring(start, _i - start);
                if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var n))
                {
                    error = Fail("a number this build cannot read ('" + text + "')");
                    return null;
                }
                var v = Scalar(JsonKind.Number);
                v.Number = n;
                return v;
            }
        }
    }
}
