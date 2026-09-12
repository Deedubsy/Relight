using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Relight.Sim.Tests.Persistence
{
    /// <summary>
    /// An <see cref="IFileSystem"/> in memory, with the faults the real one only produces on a bad day.
    /// TECHNICAL_ARCHITECTURE.md §9.4.5 specifies what must happen when a disk is full, a file is locked, or a write
    /// dies half way through — none of which can be provoked on a real disk inside a unit test. That is the reason
    /// the seam exists at all, so the fake is the part that makes the failure policy testable rather than asserted.
    ///
    /// It is deliberately unforgiving in the same places the real file system is: reading a file that is not there
    /// throws <see cref="FileNotFoundException"/>, writing into a directory that was never created throws
    /// <see cref="DirectoryNotFoundException"/>, <see cref="Move"/> refuses an existing destination, and
    /// <see cref="Replace"/> requires the destination to exist. A store that only works against a forgiving fake
    /// would not work against Windows.
    /// </summary>
    public sealed class MemoryFileSystem : IFileSystem
    {
        private readonly Dictionary<string, byte[]> _files = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        private readonly HashSet<string> _dirs = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>Every path passed to <see cref="WriteDurable"/>, in order — including temp files.</summary>
        public List<string> Writes { get; } = new List<string>();

        /// <summary>How many more writes should fail. Set it, and the next N matching writes throw.</summary>
        public int WriteFailuresLeft { get; set; }

        /// <summary>Which writes the fault applies to; null means all of them.</summary>
        public Func<string, bool> FailWriteWhen { get; set; }

        /// <summary>
        /// Whether a failing write leaves a partial file behind, as a real interrupted write does. On by default,
        /// because "the temp file is half written" is the case the protocol's temp-then-replace exists to survive.
        /// </summary>
        public bool PartialOnFailure { get; set; } = true;

        public string WriteFailureMessage { get; set; } = "there is not enough space on the disk";

        /// <summary>ERROR_DISK_FULL, so <see cref="AtomicWrite.Describe"/> recognises it as a full disk.</summary>
        public int WriteFailureHResult { get; set; } = unchecked((int)0x80070070);

        /// <summary>Fail the next <paramref name="count"/> writes whose path contains <paramref name="contains"/>.</summary>
        public void FailWrites(int count, string contains = null)
        {
            WriteFailuresLeft = count;
            FailWriteWhen = contains == null ? (Func<string, bool>)null : p => p.IndexOf(contains, StringComparison.Ordinal) >= 0;
        }

        /// <summary>A lock rather than a full disk: the same refusal a sync agent or antivirus produces.</summary>
        public void FailWritesAsLock(int count, string contains = null)
        {
            FailWrites(count, contains);
            WriteFailureMessage = "the process cannot access the file because it is being used by another process";
            WriteFailureHResult = unchecked((int)0x80070020);
        }

        public void ClearFaults()
        {
            WriteFailuresLeft = 0;
            FailWriteWhen = null;
        }

        // ---- inspection helpers for the tests ----------------------------------------------------------------

        public bool Has(string path) => _files.ContainsKey(Norm(path));

        public byte[] Bytes(string path) => _files.TryGetValue(Norm(path), out var b) ? b : null;

        public string Text(string path)
        {
            var b = Bytes(path);
            return b == null ? null : new UTF8Encoding(false, false).GetString(b);
        }

        public void PutText(string path, string text)
        {
            EnsureDir(Dir(Norm(path)));
            _files[Norm(path)] = new UTF8Encoding(false, true).GetBytes(text);
        }

        public void PutBytes(string path, byte[] bytes)
        {
            EnsureDir(Dir(Norm(path)));
            _files[Norm(path)] = bytes;
        }

        /// <summary>Every file that exists, ordinal-sorted, full paths — for "nothing else was touched" assertions.</summary>
        public List<string> AllPaths()
        {
            var all = new List<string>(_files.Keys);
            all.Sort(StringComparer.Ordinal);
            return all;
        }

        /// <summary>Count of files directly in a directory whose name starts with a prefix.</summary>
        public int CountIn(string directory, string prefix)
        {
            var names = ListFiles(directory);
            var n = 0;
            for (var i = 0; i < names.Count; i++)
                if (names[i].StartsWith(prefix, StringComparison.Ordinal)) n++;
            return n;
        }

        // ---- IFileSystem -------------------------------------------------------------------------------------

        public void CreateDirectory(string path) => EnsureDir(Norm(path));

        public bool FileExists(string path) => _files.ContainsKey(Norm(path));

        public byte[] ReadAllBytes(string path)
        {
            if (!_files.TryGetValue(Norm(path), out var bytes)) throw new FileNotFoundException("no such file", path);
            var copy = new byte[bytes.Length];
            Array.Copy(bytes, copy, bytes.Length);
            return copy;
        }

        public void WriteDurable(string path, byte[] bytes)
        {
            var key = Norm(path);
            var dir = Dir(key);
            if (!string.IsNullOrEmpty(dir) && !_dirs.Contains(dir)) throw new DirectoryNotFoundException("no such directory: " + dir);
            Writes.Add(key);

            if (WriteFailuresLeft > 0 && (FailWriteWhen == null || FailWriteWhen(key)))
            {
                WriteFailuresLeft--;
                if (PartialOnFailure && bytes.Length > 0)
                {
                    // A real interrupted write leaves what it managed to flush: half a file, which must never be
                    // mistaken for the save. It is a temp file, so the protocol deletes it.
                    var half = new byte[bytes.Length / 2];
                    Array.Copy(bytes, half, half.Length);
                    _files[key] = half;
                }
                throw new IOException(WriteFailureMessage, WriteFailureHResult);
            }

            var stored = new byte[bytes.Length];
            Array.Copy(bytes, stored, bytes.Length);
            _files[key] = stored;
        }

        public void Replace(string source, string destination, string backup)
        {
            var s = Norm(source);
            var d = Norm(destination);
            if (!_files.ContainsKey(s)) throw new FileNotFoundException("no such file", source);
            if (!_files.ContainsKey(d)) throw new FileNotFoundException("no such file", destination);
            if (!string.IsNullOrEmpty(backup)) _files[Norm(backup)] = _files[d];
            _files[d] = _files[s];
            _files.Remove(s);
        }

        public void Move(string source, string destination)
        {
            var s = Norm(source);
            var d = Norm(destination);
            if (!_files.ContainsKey(s)) throw new FileNotFoundException("no such file", source);
            if (_files.ContainsKey(d)) throw new IOException("the destination already exists: " + destination);
            var dir = Dir(d);
            if (!string.IsNullOrEmpty(dir) && !_dirs.Contains(dir)) throw new DirectoryNotFoundException("no such directory: " + dir);
            _files[d] = _files[s];
            _files.Remove(s);
        }

        public void Delete(string path) => _files.Remove(Norm(path));

        public IReadOnlyList<string> ListFiles(string directory)
        {
            var dir = Norm(directory);
            var names = new List<string>();
            foreach (var key in _files.Keys)
            {
                var parent = Dir(key);
                if (string.CompareOrdinal(parent, dir) != 0) continue;
                names.Add(Path.GetFileName(key));
            }
            names.Sort(StringComparer.Ordinal);
            return names;
        }

        private void EnsureDir(string dir)
        {
            if (string.IsNullOrEmpty(dir)) return;
            var parts = dir.Split('/');
            var built = dir.StartsWith("/", StringComparison.Ordinal) ? "" : null;
            for (var i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length == 0) continue;
                built = built == null ? parts[i] : built + "/" + parts[i];
                _dirs.Add(built);
            }
        }

        /// <summary>
        /// The parent directory in the fake's own forward-slash form. <c>Path.GetDirectoryName</c> returns
        /// backslashes on Windows (Unity's editor), which would never match the keys <see cref="EnsureDir"/> stores;
        /// the scratch runner on Linux hid this until the Unity EditMode run (coordinator fix, 2026-09-12).
        /// </summary>
        private static string Dir(string key) => Norm(Path.GetDirectoryName(key));

        private static string Norm(string path)
        {
            if (string.IsNullOrEmpty(path)) return "";
            var p = path.Replace('\\', '/');
            while (p.Length > 1 && p[p.Length - 1] == '/') p = p.Substring(0, p.Length - 1);
            return p;
        }
    }

    /// <summary>Shared setup for the persistence tests: a fake disk, a store and the short headless run.</summary>
    public static class PersistenceFixture
    {
        /// <summary>A root that exercises a real-looking absolute path without touching a real disk.</summary>
        public const string Root = "/memory/Relight";

        public static (MemoryFileSystem Fs, SaveStore Store) Store()
        {
            var fs = new MemoryFileSystem();
            return (fs, new SaveStore(fs, Root));
        }

        /// <summary>The canonical text of a state — what H1 compares when the hashes already agree.</summary>
        public static string Canonical(SimState st) => CanonicalJsonWriter.Write(st);

        /// <summary>
        /// Replace one <b>top-level</b> member's raw value in a save document, to forge a damaged or foreign file.
        /// It walks the object rather than searching the text, because several header names (<c>version</c>,
        /// <c>seed</c>, <c>tick</c>) also occur inside the state and a textual search would edit the wrong one —
        /// which is the difference between testing the version check and testing the checksum.
        /// </summary>
        public static string Retarget(string json, string key, string rawValue)
        {
            var (from, to) = Locate(json, 0, key);
            if (from < 0) throw new ArgumentException("no top-level member " + key);
            return json.Substring(0, from) + rawValue + json.Substring(to);
        }

        /// <summary>The same, one level in: edits a member of the saved <c>state</c>, which the checksum covers.</summary>
        public static string RetargetInState(string json, string key, string rawValue)
        {
            var (stateFrom, _) = Locate(json, 0, "state");
            if (stateFrom < 0) throw new ArgumentException("no state in the document");
            var (from, to) = Locate(json, stateFrom, key);
            if (from < 0) throw new ArgumentException("no state member " + key);
            return json.Substring(0, from) + rawValue + json.Substring(to);
        }

        /// <summary>Where a member's value starts and ends inside the object beginning at <paramref name="objectAt"/>.</summary>
        private static (int From, int To) Locate(string json, int objectAt, string key)
        {
            if (objectAt >= json.Length || json[objectAt] != '{') return (-1, -1);
            var i = objectAt + 1;
            while (i < json.Length && json[i] != '}')
            {
                if (json[i] != '"') return (-1, -1);           // canonical output has no whitespace and no odd keys
                var keyEnd = EndOfString(json, i);
                var name = json.Substring(i + 1, keyEnd - i - 1);
                i = keyEnd + 1;
                if (i >= json.Length || json[i] != ':') return (-1, -1);
                i++;
                var valueEnd = EndOfValue(json, i);
                if (string.CompareOrdinal(name, key) == 0) return (i, valueEnd);
                i = valueEnd;
                if (i < json.Length && json[i] == ',') i++;
            }
            return (-1, -1);
        }

        private static int EndOfString(string json, int quoteAt)
        {
            var i = quoteAt + 1;
            while (i < json.Length)
            {
                if (json[i] == '\\') { i += 2; continue; }
                if (json[i] == '"') return i;
                i++;
            }
            throw new ArgumentException("unterminated string in the document");
        }

        private static int EndOfValue(string json, int at)
        {
            var c = json[at];
            if (c == '"') return EndOfString(json, at) + 1;
            if (c == '{' || c == '[')
            {
                var depth = 0;
                var i = at;
                while (i < json.Length)
                {
                    var d = json[i];
                    if (d == '"') { i = EndOfString(json, i) + 1; continue; }
                    if (d == '{' || d == '[') depth++;
                    else if (d == '}' || d == ']')
                    {
                        depth--;
                        if (depth == 0) return i + 1;
                    }
                    i++;
                }
                throw new ArgumentException("unterminated object in the document");
            }
            var j = at;
            while (j < json.Length && json[j] != ',' && json[j] != '}' && json[j] != ']') j++;
            return j;
        }
    }
}
