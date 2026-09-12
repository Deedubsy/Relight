using System;
using System.Collections.Generic;
using System.IO;

namespace Relight.Sim
{
    /// <summary>
    /// The manual save slots, and the door to the autosave ring (TECHNICAL_ARCHITECTURE.md §9.4.1):
    /// <code>
    /// {root}/saves/{profile}/slot-&lt;name&gt;.json     manual slots, written only by an explicit Save
    /// {root}/saves/{profile}/auto/...               the ring — a different directory and a different writer
    /// </code>
    /// <b>The rule that governs everything else: an autosave never writes a manual slot.</b> Two mechanical guards
    /// enforce it, both tested: the ring's writer is constructed with the <c>auto/</c> directory and refuses any
    /// path that resolves outside it (<see cref="AutosaveStore"/>), and a manual slot name is validated to reject
    /// the reserved <c>auto</c> prefix, so a player cannot type a name that collides with a ring file.
    ///
    /// <paramref name="profile"/> mirrors the reference's <c>profileSlot()</c> (packages/game/src/session.ts) and
    /// keeps profiles in separate directories. It is inert in the Unity build — there is only one ruleset
    /// (U-D-32) — but the separation is kept so the field means something if a second one ever exists.
    ///
    /// No method throws: every outcome is a <see cref="SaveResult"/> or a <see cref="LoadResult"/> (§9.4.5).
    /// </summary>
    public sealed class SaveStore
    {
        public const string SavesFolder = "saves";
        public const string AutoFolder = "auto";
        /// <summary>Manual files are <c>slot-&lt;name&gt;.json</c>; the prefix is what keeps them out of the ring's name space.</summary>
        public const string ManualPrefix = "slot-";
        /// <summary>The reserved prefix a manual name may not start with (§9.4.1 guard 2).</summary>
        public const string ReservedPrefix = "auto";
        public const int MaxNameLength = 40;

        private readonly IFileSystem _fs;
        private readonly Dictionary<string, int> _attempts = new Dictionary<string, int>(StringComparer.Ordinal);

        public SaveStore(IFileSystem fs, string rootDirectory, string profile = null)
        {
            _fs = fs ?? throw new ArgumentNullException(nameof(fs));
            Root = rootDirectory ?? throw new ArgumentNullException(nameof(rootDirectory));
            Profile = string.IsNullOrEmpty(profile) ? SimVersion.Ruleset : profile;
            Directory = Path.Combine(Path.Combine(Root, SavesFolder), Profile);
            Autosaves = new AutosaveStore(_fs, Path.Combine(Directory, AutoFolder));
        }

        /// <summary>Usually <c>Application.persistentDataPath</c>; injected so the sim stays engine-free.</summary>
        public string Root { get; }
        public string Profile { get; }
        /// <summary>The profile's directory: manual slots live here, the ring in <c>auto/</c> beneath it.</summary>
        public string Directory { get; }
        /// <summary>The rotating autosave ring. It physically cannot address a manual slot.</summary>
        public AutosaveStore Autosaves { get; }

        /// <summary>
        /// Why <paramref name="name"/> is not a usable manual slot name, or "" when it is. Allowed:
        /// 1–40 characters of <c>A–Z a–z 0–9 space _ -</c>, no leading or trailing space, and not starting with
        /// the reserved <c>auto</c> prefix in any casing.
        /// </summary>
        public static string SlotNameProblem(string name)
        {
            if (string.IsNullOrEmpty(name)) return "the save needs a name";
            if (name.Length > MaxNameLength) return "the name is longer than " + MaxNameLength + " characters";
            if (name[0] == ' ' || name[name.Length - 1] == ' ') return "the name cannot start or end with a space";
            for (var i = 0; i < name.Length; i++)
            {
                var c = name[i];
                var ok = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9')
                         || c == ' ' || c == '_' || c == '-';
                if (!ok) return "the name can use letters, digits, spaces, '-' and '_' only";
            }
            if (name.Length >= ReservedPrefix.Length
                && string.Compare(name, 0, ReservedPrefix, 0, ReservedPrefix.Length, StringComparison.OrdinalIgnoreCase) == 0)
                return "a save cannot be called '" + name + "': names starting with 'auto' belong to the autosaves";
            return "";
        }

        /// <summary>The file a manual slot name maps to (no validation; use <see cref="SlotNameProblem"/> first).</summary>
        public string PathOf(string name) => Path.Combine(Directory, ManualPrefix + name + SaveSchema.Extension);

        /// <summary>Serialise and save to a manual slot.</summary>
        public SaveResult Save(string name, SimState st, GameData data, string savedAt = null)
        {
            var bad = SlotNameProblem(name);
            if (bad.Length > 0) return SaveResult.Failed(null, bad);
            byte[] bytes;
            try { bytes = SaveSerializer.Write(st, data, savedAt); }
            catch (Exception e) { return SaveResult.Failed(PathOf(name), AtomicWrite.Describe(e, "the save could not be prepared")); }
            return SaveBytes(name, bytes);
        }

        /// <summary>Save already-serialised bytes to a manual slot (the host serialises at a tick boundary, §9.4.3).</summary>
        public SaveResult SaveBytes(string name, byte[] bytes)
        {
            var bad = SlotNameProblem(name);
            if (bad.Length > 0) return SaveResult.Failed(null, bad);
            var target = PathOf(name);
            var attempt = _attempts.TryGetValue(target, out var a) ? a : 0;
            var r = AtomicWrite.Write(_fs, Directory, target, bytes, attempt);
            _attempts[target] = r.Ok ? 0 : attempt + 1;
            return r;
        }

        /// <summary>
        /// Load a manual slot. A file that is damaged is set aside as <c>.corrupt</c> and its <c>.bak</c> is offered
        /// (§9.4.5); a file that is simply not one of our saves is refused and left alone.
        /// </summary>
        public LoadResult Load(string name, GameData data)
        {
            var bad = SlotNameProblem(name);
            if (bad.Length > 0) return LoadResult.Refuse(bad);
            return LoadFile(_fs, PathOf(name), data, true);
        }

        /// <summary>
        /// Whether a manual slot with this name exists. A slot that is present only as the <c>.bak</c> the write
        /// protocol keeps counts: it is a save the player made, <see cref="Load"/> can read it, and hiding it would
        /// mean a crash between the temp write and the replace silently deleted their game (§9.4.4, §9.4.5).
        /// The check is existence, exactly as it is for the primary — validity is settled by the load that follows.
        /// </summary>
        public bool Exists(string name)
        {
            if (SlotNameProblem(name).Length > 0) return false;
            var path = PathOf(name);
            return _fs.FileExists(path) || _fs.FileExists(path + AtomicWrite.BackupSuffix);
        }

        /// <summary>Delete a manual slot and its backup. Returns "" or the reason it could not be deleted.</summary>
        public string Delete(string name)
        {
            var bad = SlotNameProblem(name);
            if (bad.Length > 0) return bad;
            try
            {
                _fs.Delete(PathOf(name));
                _fs.Delete(PathOf(name) + AtomicWrite.BackupSuffix);
                return "";
            }
            catch (Exception e) { return AtomicWrite.Describe(e, "the save could not be deleted"); }
        }

        /// <summary>
        /// Every manual slot with its header, newest first by saved time then by name (C-10's Load screen, U-D-41).
        /// A slot whose header cannot be read is listed with its problem instead of being hidden, and a slot that
        /// survives only as its <c>.bak</c> is listed with that copy's header — it is what a load would actually
        /// give the player, so the list must not pretend the slot is gone.
        /// </summary>
        public IReadOnlyList<SaveSlotInfo> List()
        {
            var names = _fs.ListFiles(Directory);
            var files = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < names.Count; i++)
            {
                var file = names[i];
                if (!file.StartsWith(ManualPrefix, StringComparison.Ordinal)) continue;
                // One slot, two possible files: the save and the copy the write protocol kept beside it.
                if (file.EndsWith(AtomicWrite.BackupSuffix, StringComparison.Ordinal))
                    file = file.Substring(0, file.Length - AtomicWrite.BackupSuffix.Length);
                if (!file.EndsWith(SaveSchema.Extension, StringComparison.Ordinal)) continue;   // .tmp, .corrupt
                if (seen.Add(file)) files.Add(file);
            }

            var found = new List<SaveSlotInfo>();
            for (var i = 0; i < files.Count; i++)
            {
                var file = files[i];
                var name = file.Substring(ManualPrefix.Length, file.Length - ManualPrefix.Length - SaveSchema.Extension.Length);
                var path = Path.Combine(Directory, file);
                var header = TryReadHeaderOrBackup(_fs, path, out var fromBackup, out var problem);
                found.Add(new SaveSlotInfo(name, path, header, problem, fromBackup));
            }
            found.Sort(CompareSlots);
            return found;
        }

        private static int CompareSlots(SaveSlotInfo a, SaveSlotInfo b)
        {
            var sa = a.Header?.SavedAt ?? "";
            var sb = b.Header?.SavedAt ?? "";
            var c = string.CompareOrdinal(sb, sa);                     // ISO-8601 sorts as text; newest first
            return c != 0 ? c : string.CompareOrdinal(a.Name, b.Name);
        }

        /// <summary>The header of a save without building its state, or null with the reason.</summary>
        internal static SaveHeader TryReadHeader(IFileSystem fs, string path, out string problem)
        {
            problem = "";
            byte[] bytes;
            try { bytes = fs.ReadAllBytes(path); }
            catch (Exception e) { problem = AtomicWrite.Describe(e, "the file could not be read"); return null; }
            var header = SaveSerializer.ReadHeader(bytes, out var reason);
            if (header == null) problem = reason;
            return header;
        }

        /// <summary>
        /// The header of a save, falling back to the <c>.bak</c> when the file itself is missing or unreadable —
        /// what a load of that path would actually produce. <paramref name="problem"/> describes the primary and
        /// is cleared when the previous copy answered instead.
        /// </summary>
        internal static SaveHeader TryReadHeaderOrBackup(IFileSystem fs, string path, out bool fromBackup, out string problem)
        {
            fromBackup = false;
            var header = TryReadHeader(fs, path, out problem);
            if (header != null) return header;
            var backup = path + AtomicWrite.BackupSuffix;
            if (!fs.FileExists(backup)) return null;
            var previous = TryReadHeader(fs, backup, out _);
            if (previous == null) return null;              // the primary's problem is the one worth showing
            fromBackup = true;
            problem = "";
            return previous;
        }

        /// <summary>
        /// Read one file into a state, with §9.4.5's recovery: on damage the file is renamed <c>.corrupt</c> and
        /// its <c>.bak</c> is tried; when the file is not there at all the <c>.bak</c> is still tried, because a
        /// crash between the temp write and the replace leaves exactly that. Shared by the manual slots and the ring.
        ///
        /// A recovery that works once and then loses the save is worse than none, so a copy that loaded is written
        /// back over the missing primary before returning (below): the next load is then an ordinary one, the slot
        /// stays in <see cref="Exists"/> and <see cref="List"/>, and a restart changes nothing.
        /// </summary>
        internal static LoadResult LoadFile(IFileSystem fs, string path, GameData data, bool recover)
        {
            byte[] bytes;
            try { bytes = fs.ReadAllBytes(path); }
            catch (FileNotFoundException) { return Missing(fs, path, data, recover); }
            catch (DirectoryNotFoundException) { return Missing(fs, path, data, recover); }
            catch (Exception e) { return LoadResult.Refuse(AtomicWrite.Describe(e, "the save could not be read")); }

            var r = SaveSerializer.Read(bytes, data);
            if (r.Ok) { r.Path = path; return r; }
            if (!recover || !r.Damaged) { r.Path = path; return r; }

            var firstReason = r.Reason;
            var backup = path + AtomicWrite.BackupSuffix;
            AtomicWrite.SetAside(fs, path);                 // never overwritten silently; kept for inspection
            var recovered = LoadBackup(fs, path, data, "that save was damaged (" + firstReason + ")");
            if (recovered != null) return recovered;
            if (fs.FileExists(backup))
                return LoadResult.Refuse(firstReason + "; the previous copy is unusable too", true);
            r.Path = path;
            return r;
        }

        /// <summary>The file is not there: the previous copy may still be, and it is a save the player made.</summary>
        private static LoadResult Missing(IFileSystem fs, string path, GameData data, bool recover)
        {
            if (recover)
            {
                var recovered = LoadBackup(fs, path, data, "the newest copy of that save was missing");
                if (recovered != null) return recovered;
            }
            return LoadResult.Refuse("there is no save there");
        }

        /// <summary>
        /// Load <c>&lt;path&gt;.bak</c>, or null when there is no usable previous copy. On success the primary is
        /// rewritten from the same bytes so the recovery is not a one-shot; that restore is a courtesy and never a
        /// risk — it is skipped while the damaged file is still in place (replacing it would push the damage into
        /// the <c>.bak</c> and destroy the last copy that works), and a failed restore leaves the <c>.bak</c>
        /// untouched and is reported in <see cref="LoadResult.Recovered"/> rather than failing the load.
        /// </summary>
        private static LoadResult LoadBackup(IFileSystem fs, string path, GameData data, string why)
        {
            var backup = path + AtomicWrite.BackupSuffix;
            byte[] bytes;
            try { bytes = fs.ReadAllBytes(backup); }
            catch (Exception) { return null; }

            var r = SaveSerializer.Read(bytes, data);
            if (!r.Ok) return null;

            var restored = RestorePrimary(fs, path, bytes);
            r.Path = restored.Ok ? path : backup;
            r.Recovered = why + "; the previous copy was loaded instead"
                          + (restored.Ok ? "" : " (it could not be written back over the missing one: " + restored.Reason + ")");
            return r;
        }

        /// <summary>Put a recovered copy back where the save belongs, using the ordinary write protocol (§9.4.4).</summary>
        private static SaveResult RestorePrimary(IFileSystem fs, string path, byte[] bytes)
        {
            if (fs.FileExists(path)) return SaveResult.Failed(path, "the damaged file could not be set aside");
            string directory;
            try { directory = Path.GetDirectoryName(path); }
            catch (Exception e) { return SaveResult.Failed(path, AtomicWrite.Describe(e, "the saves folder could not be found")); }
            return AtomicWrite.Write(fs, directory, path, bytes);
        }
    }

    /// <summary>One manual slot as the Load screen sees it.</summary>
    public sealed class SaveSlotInfo
    {
        public SaveSlotInfo(string name, string path, SaveHeader header, string problem, bool fromBackup = false)
        {
            Name = name;
            Path = path;
            Header = header;
            Problem = problem ?? "";
            FromBackup = fromBackup;
        }

        public string Name { get; }
        /// <summary>The path a load asks for — the slot itself, even when only its previous copy can be read.</summary>
        public string Path { get; }
        /// <summary>Null when the file could not be read; <see cref="Problem"/> then says why.</summary>
        public SaveHeader Header { get; }
        public string Problem { get; }
        /// <summary>
        /// True when <see cref="Header"/> came from the <c>.bak</c> because the slot itself is missing or damaged:
        /// the slot still loads (§9.4.5 recovers it), and the UI can say the copy is a slightly older one.
        /// </summary>
        public bool FromBackup { get; }
        public bool Ok => Header != null;
    }
}
