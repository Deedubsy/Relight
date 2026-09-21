using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace Relight.Prototypes
{
    /// <summary>
    /// B-14. Writes one measurement file per prototype run, so every figure in the report has a file behind it that
    /// the run actually produced. Plain key/value JSON written by hand (no Newtonsoft, no System.Text.Json) with
    /// <see cref="CultureInfo.InvariantCulture"/> everywhere.
    ///
    /// The B-14 files recorded in <c>Unity/Docs/evidence/phase-b/</c> are frozen evidence. Since PER-01 a run writes
    /// to <see cref="Folder"/> under the project's <c>Temp/</c> instead, so an ordinary PlayMode run no longer
    /// rewrites them; recording new evidence is a deliberate copy by hand.
    /// </summary>
    public static class EvidenceWriter
    {
        /// <summary>Where a run writes: <c>{project}/Temp/prototype-evidence</c>, never the recorded evidence tree.</summary>
        public static string Folder =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/prototype-evidence")).Replace('\\', '/');

        public static string Write(string fileName, IEnumerable<KeyValuePair<string, object>> values)
        {
            Directory.CreateDirectory(Folder);
            var sb = new StringBuilder();
            sb.Append("{\n");
            var first = true;
            foreach (var kv in values)
            {
                if (!first) sb.Append(",\n");
                first = false;
                sb.Append("  \"").Append(Escape(kv.Key)).Append("\": ").Append(Format(kv.Value));
            }
            sb.Append("\n}\n");
            var path = Path.Combine(Folder, fileName).Replace('\\', '/');
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
            Debug.Log($"B14 evidence: wrote {path}");
            return path;
        }

        /// <summary>The three acceptance figures of a sampler, as evidence keys prefixed with <paramref name="prefix"/>.</summary>
        public static void AddFrames(List<KeyValuePair<string, object>> into, string prefix, FrameSampler s)
        {
            into.Add(new KeyValuePair<string, object>(prefix + "_frames", s.Count));
            into.Add(new KeyValuePair<string, object>(prefix + "_mean_ms", Round(s.Mean())));
            into.Add(new KeyValuePair<string, object>(prefix + "_p95_ms", Round(s.P95())));
            into.Add(new KeyValuePair<string, object>(prefix + "_max_ms", Round(s.Max())));
        }

        public static double Round(double v) => System.Math.Round(v, 3);

        private static string Format(object v)
        {
            switch (v)
            {
                case null: return "null";
                case bool b: return b ? "true" : "false";
                case int i: return i.ToString(CultureInfo.InvariantCulture);
                case long l: return l.ToString(CultureInfo.InvariantCulture);
                case float f: return f.ToString("R", CultureInfo.InvariantCulture);
                case double d: return d.ToString("R", CultureInfo.InvariantCulture);
                default: return "\"" + Escape(v.ToString()) + "\"";
            }
        }

        private static string Escape(string s)
        {
            var sb = new StringBuilder();
            foreach (var c in s)
            {
                if (c == '"' || c == '\\') sb.Append('\\').Append(c);
                else if (c == '\n') sb.Append("\\n");
                else if (c == '\r') sb.Append("\\r");
                else if (c == '\t') sb.Append("\\t");
                else sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
