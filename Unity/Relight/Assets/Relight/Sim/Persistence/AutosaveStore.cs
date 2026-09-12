using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Relight.Sim
{
    /// <summary>
    /// The rotating autosave ring (TECHNICAL_ARCHITECTURE.md §9.4.1–§9.4.2):
    /// <code>
    /// {profile}/auto/auto-1.json … auto-N.json   the ring, oldest slot overwritten next
    /// {profile}/auto/auto-quit.json              save-on-quit, outside the ring, never rotated
    /// {profile}/auto/auto-index.json             which slot is which, so "the oldest" survives a restart
    /// </code>
    /// <b>This class is the mechanical guarantee that an autosave never touches a manual slot.</b> It is constructed
    /// with the <c>auto/</c> directory and every path it writes goes through <see cref="Resolve"/>, which refuses a
    /// name containing a separator or <c>..</c> and then re-checks the resolved full path against the directory. A
    /// caller cannot hand it <c>../slot-my game.json</c>, and a corrupt index naming an outside file is ignored
    /// rather than obeyed.
    ///
    /// The index carries a monotonic <c>seq</c> per slot, so the oldest slot is a fact on disk rather than a guess
    /// from timestamps — and if the index is lost the ring is rebuilt from the slot headers instead of restarting
    /// at slot 1 and destroying the newest save.
    /// </summary>
    public sealed class AutosaveStore
    {
        public const string SlotPrefix = "auto-";
        public const string IndexFile = "auto-index.json";
        public const string QuitFile = "auto-quit.json";
        public const int IndexVersion = 1;

        private readonly IFileSystem _fs;
        private readonly string _full;      // the directory, resolved once, for the containment check

        public AutosaveStore(IFileSystem fs, string directory)
        {
            _fs = fs ?? throw new ArgumentNullException(nameof(fs));
            Directory = directory ?? throw new ArgumentNullException(nameof(directory));
            _full = NormalizeDirectory(directory);
        }

        /// <summary>The <c>auto/</c> directory. Nothing this class writes is outside it.</summary>
        public string Directory { get; }

        /// <summary>The file name of ring slot <paramref name="index"/> (1-based).</summary>
        public static string SlotFile(int index) => SlotPrefix + index.ToString(CultureInfo.InvariantCulture) + SaveSchema.Extension;

        /// <summary>
        /// The full path of a file in the ring directory, or null (with a reason) when the name would escape it.
        /// Every write and every read in this class goes through here — including names read back from the index,
        /// which is a file on disk and therefore untrusted input.
        /// </summary>
        public string Resolve(string fileName, out string problem)
        {
            problem = "";
            if (string.IsNullOrEmpty(fileName)) { problem = "an autosave needs a file name"; return null; }
            if (fileName.IndexOf('/') >= 0 || fileName.IndexOf('\\') >= 0 || fileName.IndexOf(':') >= 0)
            {
                problem = "an autosave name cannot contain a path";
                return null;
            }
            if (string.CompareOrdinal(fileName, ".") == 0 || string.CompareOrdinal(fileName, "..") == 0)
            {
                problem = "an autosave name cannot contain a path";
                return null;
            }
            if (!fileName.StartsWith(SlotPrefix, StringComparison.Ordinal))
            {
                problem = "an autosave file is called '" + SlotPrefix + "…'; '" + fileName + "' is not one";
                return null;
            }

            var combined = Path.Combine(Directory, fileName);
            string full;
            try { full = Path.GetFullPath(combined); }
            catch (Exception e) { problem = AtomicWrite.Describe(e, "that autosave path cannot be used"); return null; }

            if (!Contains(full))
            {
                problem = "that path is outside the autosave folder";
                return null;
            }
            return combined;
        }

        private bool Contains(string fullPath)
        {
            if (string.IsNullOrEmpty(_full)) return false;
            if (!fullPath.StartsWith(_full, StringComparison.Ordinal)) return false;
            var rest = fullPath.Substring(_full.Length);
            if (rest.Length == 0) return false;
            // Exactly one segment below the directory: a ring file, never a subdirectory of one.
            return rest.IndexOf(Path.DirectorySeparatorChar) < 0 && rest.IndexOf('/') < 0 && rest.IndexOf('\\') < 0;
        }

        private static string NormalizeDirectory(string directory)
        {
            string full;
            try { full = Path.GetFullPath(directory); }
            catch (Exception) { return null; }
            if (full.Length > 0 && (full[full.Length - 1] == Path.DirectorySeparatorChar || full[full.Length - 1] == '/' || full[full.Length - 1] == '\\'))
                return full;
            return full + Path.DirectorySeparatorChar;
        }

        // ---- writing -----------------------------------------------------------------------------------------

        /// <summary>
        /// Write one ring autosave, overwriting the oldest of <paramref name="slots"/>. Order matters and is
        /// §9.4.4's: the slot is written atomically first, and only a slot that landed updates the index — so a
        /// failure can lose the new autosave but can never make the index point at a file that is not there.
        /// </summary>
        public SaveResult WriteRing(byte[] bytes, int slots, SaveHeader header, int attempt = 0)
        {
            if (slots < 1) slots = 1;
            var index = ReadIndex();
            var target = ChooseSlot(index, slots);
            var path = Resolve(target, out var bad);
            if (path == null) return SaveResult.Failed(target, bad);

            var wrote = AtomicWrite.Write(_fs, Directory, path, bytes, attempt);
            if (!wrote.Ok) return wrote;

            index.Record(target, header);
            var indexResult = WriteIndex(index);
            if (!indexResult.Ok)
            {
                // The autosave itself is on disk and loadable; only the bookkeeping failed. Say so, do not fail.
                return SaveResult.Wrote(path, bytes.Length).WithNotice(
                    "the autosave was written but its index could not be updated (" + indexResult.Reason + ")");
            }
            return wrote;
        }

        /// <summary>Write the save-on-quit file. It is outside the ring and is simply replaced each time (§9.4.2).</summary>
        public SaveResult WriteQuit(byte[] bytes, SaveHeader header, int attempt = 0)
        {
            var path = Resolve(QuitFile, out var bad);
            if (path == null) return SaveResult.Failed(QuitFile, bad);
            var wrote = AtomicWrite.Write(_fs, Directory, path, bytes, attempt);
            if (!wrote.Ok) return wrote;
            var index = ReadIndex();
            index.Record(QuitFile, header);
            WriteIndex(index);   // best effort: the quit file is found by name even with no index at all
            return wrote;
        }

        /// <summary>
        /// The slot to overwrite, in the order that costs the player least:
        /// <list type="number">
        /// <item>a slot of 1..<paramref name="slots"/> that holds nothing at all — no file and no previous copy;</item>
        /// <item>a slot whose contents cannot be read and have no usable previous copy, because replacing rubbish
        ///       costs nothing while replacing a working save costs a save;</item>
        /// <item>otherwise the lowest sequence number, which after <see cref="ReadIndex"/>'s reconciliation is the
        ///       genuinely oldest save by the time written in the file itself, not by whatever the index claims.</item>
        /// </list>
        /// </summary>
        public string ChooseSlot(AutosaveIndex index, int slots)
        {
            for (var i = 1; i <= slots; i++)
            {
                var file = SlotFile(i);
                var path = Resolve(file, out _);
                if (path == null) continue;
                if (!_fs.FileExists(path) && !_fs.FileExists(path + AtomicWrite.BackupSuffix)) return file;
            }
            for (var i = 1; i <= slots; i++)
            {
                var file = SlotFile(i);
                var entry = index?.Find(file);
                if (entry != null && !string.IsNullOrEmpty(entry.Problem)) return file;
            }
            var oldest = SlotFile(1);
            var oldestSeq = long.MaxValue;
            for (var i = 1; i <= slots; i++)
            {
                var file = SlotFile(i);
                var seq = index?.SeqOf(file) ?? 0;
                if (seq < oldestSeq) { oldestSeq = seq; oldest = file; }
            }
            return oldest;
        }

        // ---- the index ---------------------------------------------------------------------------------------

        /// <summary>
        /// The ring as it actually is: the index on disk read for what it remembers, then <b>reconciled against the
        /// files themselves</b>. It never throws and never returns null \u2014 a lost index must not stop autosaving.
        ///
        /// An index that parses is still only a claim. <see cref="WriteRing"/> replaces a slot and then updates the
        /// index, so a crash or a full disk between those two steps leaves an index describing the <i>previous</i>
        /// contents of a slot that now holds the newest save. Trusting it then hides the newest autosave from
        /// "continue" and \u2014 because its recorded sequence number is the lowest \u2014 makes it the first slot the next
        /// rotation destroys. So the files win: every ring file's own header is read (they are small, there are at
        /// most eleven, and correctness beats a saved parse here), an entry that disagrees with its file is
        /// corrected from it, an entry whose file has gone is dropped, and a file the index has never heard of is
        /// added.
        ///
        /// Order is then a fact about the saves rather than about the bookkeeping: oldest first by <c>savedAt</c>,
        /// then <c>tick</c>, then \u2014 only as a tie-break between writes in the same instant \u2014 the sequence number the
        /// index remembered, then the file name. The sequence numbers are re-issued from that order, so they stay a
        /// dense rank that <see cref="ChooseSlot"/> and <see cref="ListRecoverable"/> can rely on. Nothing is
        /// written here: a read path must not need the disk to work.
        /// </summary>
        public AutosaveIndex ReadIndex() => Reconcile(ParseIndex());

        /// <summary>
        /// Reconstruct the ring's order from the files alone, ignoring any index. Used when the index is missing or
        /// damaged; without it a lost index would make slot 1 look oldest and the newest autosave would be the
        /// first thing overwritten.
        /// </summary>
        public AutosaveIndex RebuildIndex() => Reconcile(null);

        /// <summary>The index exactly as written, or null when there is none we can use.</summary>
        private AutosaveIndex ParseIndex()
        {
            var path = Resolve(IndexFile, out _);
            if (path == null || !_fs.FileExists(path)) return null;
            try
            {
                var text = new UTF8Encoding(false, false).GetString(_fs.ReadAllBytes(path));
                if (text.Length > 0 && text[0] == '\uFEFF') text = text.Substring(1);
                var root = JsonValue.Parse(text, out _);
                return AutosaveIndex.FromJson(root, this);
            }
            catch (Exception) { return null; }
        }

        /// <summary>
        /// The index that <paramref name="recorded"/> should have been, given the files on disk. See
        /// <see cref="ReadIndex"/> for why this is not optional.
        /// </summary>
        private AutosaveIndex Reconcile(AutosaveIndex recorded)
        {
            var files = _fs.ListFiles(Directory);
            var names = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < files.Count; i++)
            {
                var file = files[i];
                if (!file.StartsWith(SlotPrefix, StringComparison.Ordinal)) continue;
                // A slot that survives only as the copy the write protocol kept is still an autosave (\u00A79.4.4).
                if (file.EndsWith(AtomicWrite.BackupSuffix, StringComparison.Ordinal))
                    file = file.Substring(0, file.Length - AtomicWrite.BackupSuffix.Length);
                if (!file.EndsWith(SaveSchema.Extension, StringComparison.Ordinal)) continue;   // .tmp, .corrupt
                if (string.CompareOrdinal(file, IndexFile) == 0) continue;
                if (Resolve(file, out _) == null) continue;
                if (seen.Add(file)) names.Add(file);
            }

            var entries = new List<AutosaveEntry>();
            for (var i = 0; i < names.Count; i++)
            {
                var file = names[i];
                var path = Resolve(file, out _);
                var known = recorded?.Find(file);
                var header = SaveStore.TryReadHeaderOrBackup(_fs, path, out var fromBackup, out var problem);
                var entry = new AutosaveEntry
                {
                    File = file,
                    Seq = known?.Seq ?? 0,               // a tie-break only; re-issued below
                    FromBackup = fromBackup,
                };
                if (header != null)
                {
                    entry.SavedAt = header.SavedAt ?? "";
                    entry.Tick = header.Tick;
                    entry.PlaySeconds = header.PlaySeconds;
                    entry.Hash = header.Hash ?? "";
                }
                else
                {
                    // The file is there but says nothing about itself. What the index remembered is the only
                    // description left, so it is kept for ordering \u2014 and the problem is recorded so the recovery
                    // list still offers it (its .bak may load) and rotation can take it before a working save.
                    entry.SavedAt = known?.SavedAt ?? "";
                    entry.Tick = known?.Tick ?? 0;
                    entry.PlaySeconds = known?.PlaySeconds ?? 0;
                    entry.Hash = known?.Hash ?? "";
                    entry.Problem = string.IsNullOrEmpty(problem) ? "the autosave could not be read" : problem;
                }
                entries.Add(entry);
            }

            entries.Sort(CompareOldestFirst);
            var index = new AutosaveIndex();
            for (var i = 0; i < entries.Count; i++)
            {
                entries[i].Seq = i + 1;
                index.Add(entries[i]);
            }
            index.Next = entries.Count + 1;
            return index;
        }

        /// <summary>Oldest first: the saved time in the file, then its tick, then the remembered order, then name.</summary>
        private static int CompareOldestFirst(AutosaveEntry a, AutosaveEntry b)
        {
            var c = string.CompareOrdinal(a.SavedAt, b.SavedAt);            // ISO-8601 sorts as text
            if (c != 0) return c;
            c = a.Tick.CompareTo(b.Tick);
            if (c != 0) return c;
            c = a.Seq.CompareTo(b.Seq);
            return c != 0 ? c : string.CompareOrdinal(a.File, b.File);
        }

        /// <summary>Write the index with the same atomic protocol as a save (a torn index is as bad as a torn save).</summary>
        public SaveResult WriteIndex(AutosaveIndex index)
        {
            var path = Resolve(IndexFile, out var bad);
            if (path == null) return SaveResult.Failed(IndexFile, bad);
            byte[] bytes;
            try { bytes = new UTF8Encoding(false, true).GetBytes(index.ToJson()); }
            catch (Exception e) { return SaveResult.Failed(path, AtomicWrite.Describe(e, "the autosave index could not be prepared")); }
            return AtomicWrite.Write(_fs, Directory, path, bytes, 0);
        }

        // ---- reading -----------------------------------------------------------------------------------------

        /// <summary>
        /// Every autosave that exists, newest first — the ring and <c>auto-quit.json</c> alike, each described by
        /// the header in the file rather than by the index's memory of it, and each ordered by when it was actually
        /// saved (see <see cref="ReadIndex"/>). This is the order §9.4.5 recovers in: newest, then the next newest.
        /// </summary>
        public IReadOnlyList<AutosaveEntry> ListRecoverable()
        {
            var index = ReadIndex();
            var entries = index.Entries;
            var found = new List<AutosaveEntry>(entries.Count);
            for (var i = entries.Count - 1; i >= 0; i--)      // reconciled entries are oldest first
            {
                var e = entries[i];
                var path = Resolve(e.File, out _);
                if (path == null) continue;
                found.Add(new AutosaveEntry
                {
                    File = e.File,
                    Path = path,
                    Seq = e.Seq,
                    SavedAt = e.SavedAt,
                    Tick = e.Tick,
                    PlaySeconds = e.PlaySeconds,
                    Hash = e.Hash,
                    FromBackup = e.FromBackup,
                    Problem = e.Problem,
                });
            }
            return found;
        }

        /// <summary>
        /// Load the newest autosave that actually works. A damaged one is set aside as <c>.corrupt</c>, its
        /// <c>.bak</c> is tried, and failing that the next-newest slot is tried — §9.4.5's recovery order. The
        /// result says which file it came from and why, so the UI can tell the player.
        /// </summary>
        public LoadResult LoadNewest(GameData data)
        {
            var entries = ListRecoverable();
            if (entries.Count == 0) return LoadResult.Refuse("there are no autosaves yet");

            string firstReason = null;
            for (var i = 0; i < entries.Count; i++)
            {
                var r = SaveStore.LoadFile(_fs, entries[i].Path, data, true);
                if (r.Ok)
                {
                    if (i > 0 && string.IsNullOrEmpty(r.Recovered))
                        r.Recovered = "the newest autosave was unusable (" + firstReason + "); an older one was loaded instead";
                    return r;
                }
                if (firstReason == null) firstReason = r.Reason;
            }
            return LoadResult.Refuse("no autosave could be loaded (" + firstReason + ")");
        }

        /// <summary>Load one named ring file (<c>auto-2.json</c>, <c>auto-quit.json</c>), with the same recovery.</summary>
        public LoadResult Load(string fileName, GameData data)
        {
            var path = Resolve(fileName, out var bad);
            if (path == null) return LoadResult.Refuse(bad);
            return SaveStore.LoadFile(_fs, path, data, true);
        }
    }

    /// <summary>One autosave as the index and the recovery list see it.</summary>
    public sealed class AutosaveEntry
    {
        /// <summary>File name only, never a path — see <see cref="AutosaveStore.Resolve"/>.</summary>
        public string File { get; set; } = "";
        /// <summary>Filled in by <see cref="AutosaveStore.ListRecoverable"/>; not stored in the index.</summary>
        public string Path { get; set; }
        /// <summary>
        /// Write order, higher is newer — this is what "the oldest slot" means. It is re-issued as a dense rank
        /// every time the index is read, from the saved times in the files themselves, so a sequence number that
        /// the disk contradicts cannot survive to mis-order the ring (<see cref="AutosaveStore.ReadIndex"/>).
        /// </summary>
        public long Seq { get; set; }
        public string SavedAt { get; set; } = "";
        public int Tick { get; set; }
        public double PlaySeconds { get; set; }
        public string Hash { get; set; } = "";
        /// <summary>
        /// True when this entry describes the slot's <c>.bak</c> because the slot itself is missing or unreadable.
        /// The save still loads: <see cref="SaveStore.LoadFile"/> recovers it and puts the file back. Not stored.
        /// </summary>
        public bool FromBackup { get; set; }
        /// <summary>Why the slot's own header could not be read, when it could not. Not stored in the index.</summary>
        public string Problem { get; set; }
    }

    /// <summary>
    /// <c>auto-index.json</c>: which ring file was written when. Small, hand-written in the canonical style so it
    /// diffs cleanly and so no second JSON writer is needed.
    /// </summary>
    public sealed class AutosaveIndex
    {
        private readonly List<AutosaveEntry> _entries = new List<AutosaveEntry>();

        /// <summary>The sequence number the next write will take.</summary>
        public long Next { get; set; } = 1;

        public IReadOnlyList<AutosaveEntry> Entries => _entries;

        public void Add(AutosaveEntry entry)
        {
            if (entry != null) _entries.Add(entry);
        }

        public AutosaveEntry Find(string file)
        {
            for (var i = 0; i < _entries.Count; i++)
                if (string.CompareOrdinal(_entries[i].File, file) == 0) return _entries[i];
            return null;
        }

        public long SeqOf(string file) => Find(file)?.Seq ?? 0;

        /// <summary>Note that <paramref name="file"/> has just been written, giving it the next sequence number.</summary>
        public void Record(string file, SaveHeader header)
        {
            var entry = Find(file);
            if (entry == null)
            {
                entry = new AutosaveEntry { File = file };
                _entries.Add(entry);
            }
            entry.Seq = Next++;
            entry.SavedAt = header?.SavedAt ?? "";
            entry.Tick = header?.Tick ?? 0;
            entry.PlaySeconds = header?.PlaySeconds ?? 0;
            entry.Hash = header?.Hash ?? "";
        }

        public string ToJson()
        {
            var sb = new StringBuilder(128 + _entries.Count * 96);
            sb.Append("{\"entries\":[");
            for (var i = 0; i < _entries.Count; i++)
            {
                var e = _entries[i];
                if (i > 0) sb.Append(',');
                sb.Append("{\"file\":").Append(CanonicalJsonWriter.QuoteString(e.File));
                sb.Append(",\"hash\":").Append(CanonicalJsonWriter.QuoteString(e.Hash ?? ""));
                sb.Append(",\"playSeconds\":").Append(CanonicalJsonWriter.Num(e.PlaySeconds));
                sb.Append(",\"savedAt\":").Append(CanonicalJsonWriter.QuoteString(e.SavedAt ?? ""));
                sb.Append(",\"seq\":").Append(e.Seq.ToString(CultureInfo.InvariantCulture));
                sb.Append(",\"tick\":").Append(e.Tick.ToString(CultureInfo.InvariantCulture));
                sb.Append('}');
            }
            sb.Append("],\"next\":").Append(Next.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"version\":").Append(IndexVersionText);
            sb.Append('}');
            return sb.ToString();
        }

        private static string IndexVersionText => AutosaveStore.IndexVersion.ToString(CultureInfo.InvariantCulture);

        /// <summary>
        /// Parse an index, or null when it is not one we can use (the caller then rebuilds from the files). Entries
        /// naming anything outside the ring directory are dropped rather than trusted.
        /// </summary>
        public static AutosaveIndex FromJson(JsonValue root, AutosaveStore store)
        {
            if (root == null || !root.IsObject) return null;
            if ((int)root.NumberOf("version") != AutosaveStore.IndexVersion) return null;
            var list = root.Member("entries");
            if (list == null || !list.IsArray) return null;

            var index = new AutosaveIndex();
            var highest = 0L;
            for (var i = 0; i < list.Count; i++)
            {
                var item = list.At(i);
                if (item == null || !item.IsObject) continue;
                var file = item.TextOf("file", "");
                if (store != null && store.Resolve(file, out _) == null) continue;   // never trust a path from a file
                var entry = new AutosaveEntry
                {
                    File = file,
                    Seq = (long)item.NumberOf("seq"),
                    SavedAt = item.TextOf("savedAt", ""),
                    Tick = (int)item.NumberOf("tick"),
                    PlaySeconds = item.NumberOf("playSeconds"),
                    Hash = item.TextOf("hash", ""),
                };
                if (entry.Seq > highest) highest = entry.Seq;
                index.Add(entry);
            }
            var next = (long)root.NumberOf("next");
            index.Next = next > highest ? next : highest + 1;
            return index;
        }
    }
}
