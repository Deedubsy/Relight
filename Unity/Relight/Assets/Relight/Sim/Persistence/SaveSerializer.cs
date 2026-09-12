using System;
using System.Globalization;
using System.Text;

namespace Relight.Sim
{
    /// <summary>
    /// Turns a <see cref="SimState"/> into the bytes of a schema-v1 save and back (B-11, TECHNICAL_ARCHITECTURE.md
    /// §9.1). It is the only place that knows the file shape; the stores below deal in bytes and paths.
    ///
    /// Writing is the canonical writer plus a header (<see cref="SaveSchema"/>). Reading is strict and answers with
    /// data, never an exception: a file that is not this build's save comes back as a refusal with a reason the UI
    /// can show, and the caller's existing state is untouched because nothing is mutated until a complete new
    /// <see cref="SimState"/> has been built and verified (TECHNICAL_ARCHITECTURE.md §9.4.5).
    ///
    /// The <c>hash</c> field makes the round trip self-checking: it is <see cref="StateHash"/> of the state as
    /// written, and a load re-hashes the state it just built and compares. A dropped field, a reader bug, a
    /// hand-edited file or a save from a build with extra fields all show up as a hash mismatch — which is contract
    /// H1 (§10.4) enforced on every real load, not only in the test.
    /// </summary>
    public static class SaveSerializer
    {
        /// <summary>UTF-8 without a byte-order mark, throwing on malformed bytes rather than substituting U+FFFD.</summary>
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);

        /// <summary>The bytes of a save of <paramref name="st"/>. <paramref name="savedAt"/> defaults to now (UTC).</summary>
        public static byte[] Write(SimState st, GameData data, string savedAt = null)
            => Utf8.GetBytes(WriteText(st, data, savedAt));

        /// <summary>
        /// The bytes of a save, with the header that describes them — what the stores put in the autosave index
        /// and the slot list, without re-parsing or re-hashing the file they just wrote.
        /// </summary>
        public static byte[] Write(SimState st, GameData data, string savedAt, out SaveHeader header)
            => Utf8.GetBytes(WriteText(st, data, savedAt, out header));

        /// <summary>The save document as text (the bytes are its UTF-8 encoding).</summary>
        public static string WriteText(SimState st, GameData data, string savedAt = null)
            => WriteText(st, data, savedAt, out _);

        /// <summary>The save document as text, with its header.</summary>
        public static string WriteText(SimState st, GameData data, string savedAt, out SaveHeader header)
        {
            if (st == null) throw new ArgumentNullException(nameof(st));
            var state = CanonicalJsonWriter.Write(st);
            var hash = StateHash.Of(state);
            var stamp = savedAt ?? NowIso();
            var dataVersion = GameDataHash.Compute(data);
            var profile = st.Ruleset ?? SimVersion.Ruleset;
            header = new SaveHeader
            {
                Version = SaveSchema.Version,
                Kind = SaveSchema.Kind,
                SavedAt = stamp,
                Seed = st.Seed,
                Tick = st.Tick,
                T = st.T,
                PlaySeconds = st.PlaySeconds,
                Profile = profile,
                DataVersion = dataVersion,
                Hash = hash,
            };

            // Header keys in ordinal order, as every canonical object is written.
            var sb = new StringBuilder(state.Length + 256);
            sb.Append('{');
            Member(sb, SaveSchema.FieldDataVersion, CanonicalJsonWriter.QuoteString(dataVersion), true);
            Member(sb, SaveSchema.FieldHash, CanonicalJsonWriter.QuoteString(hash), false);
            Member(sb, SaveSchema.FieldKind, CanonicalJsonWriter.QuoteString(SaveSchema.Kind), false);
            Member(sb, SaveSchema.FieldPlaySeconds, CanonicalJsonWriter.Num(st.PlaySeconds), false);
            Member(sb, SaveSchema.FieldProfile, CanonicalJsonWriter.QuoteString(profile), false);
            Member(sb, SaveSchema.FieldSavedAt, CanonicalJsonWriter.QuoteString(stamp), false);
            Member(sb, SaveSchema.FieldSeed, st.Seed.ToString(CultureInfo.InvariantCulture), false);
            Member(sb, SaveSchema.FieldState, state, false);
            Member(sb, SaveSchema.FieldT, CanonicalJsonWriter.Num(st.T), false);
            Member(sb, SaveSchema.FieldTick, st.Tick.ToString(CultureInfo.InvariantCulture), false);
            Member(sb, SaveSchema.FieldVersion, SaveSchema.Version.ToString(CultureInfo.InvariantCulture), false);
            sb.Append('}');
            return sb.ToString();
        }

        /// <summary>The timestamp format the header uses: ISO-8601, UTC, invariant.</summary>
        public static string NowIso() => DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ", CultureInfo.InvariantCulture);

        /// <summary>Reads a save. Never throws for bad input; see <see cref="LoadResult"/>.</summary>
        public static LoadResult Read(byte[] bytes, GameData data)
        {
            if (bytes == null || bytes.Length == 0) return LoadResult.Refuse("the file is empty", true);
            string text;
            try { text = Utf8.GetString(bytes); }
            catch (Exception) { return LoadResult.Refuse("the file is not a text save (its bytes are not UTF-8)", true); }
            // A byte-order mark is legal in a file someone re-saved from an editor; the parser does not want it.
            if (text.Length > 0 && text[0] == '\uFEFF') text = text.Substring(1);
            return ReadText(text, data);
        }

        /// <summary>Reads a save from its text. Never throws for bad input.</summary>
        public static LoadResult ReadText(string text, GameData data)
        {
            if (string.IsNullOrEmpty(text)) return LoadResult.Refuse("the file is empty", true);
            // A file re-saved by a text editor can arrive with a byte-order mark; the parser does not want one.
            if (text[0] == '\uFEFF') text = text.Substring(1);

            var root = JsonValue.Parse(text, out var jsonError);
            if (root == null) return LoadResult.Refuse("the file is damaged: " + jsonError, true);
            if (!root.IsObject) return LoadResult.Refuse("this is not a Relight save file");

            var kind = root.TextOf(SaveSchema.FieldKind);
            if (string.CompareOrdinal(kind, SaveSchema.Kind) != 0)
            {
                if (string.CompareOrdinal(kind, SaveSchema.ReferenceKind) == 0)
                    return LoadResult.Refuse("this is a save from the original build; it cannot be loaded here (saves are not converted)");
                return LoadResult.Refuse("this is not a Relight save file");
            }

            var versionMember = root.Member(SaveSchema.FieldVersion);
            if (versionMember == null || versionMember.Kind != JsonKind.Number)
                return LoadResult.Refuse("this save does not say which version it is");
            var version = (int)versionMember.Number;
            if (version != SaveSchema.Version)
                return LoadResult.Refuse("unsupported save version " + version.ToString(CultureInfo.InvariantCulture)
                                         + " (this build reads version " + SaveSchema.Version.ToString(CultureInfo.InvariantCulture) + ")");

            var stateJson = root.Member(SaveSchema.FieldState);
            if (stateJson == null || !stateJson.IsObject) return LoadResult.Refuse("this save has no state in it");

            var header = new SaveHeader
            {
                Version = version,
                Kind = kind,
                SavedAt = root.TextOf(SaveSchema.FieldSavedAt, ""),
                Seed = (int)root.NumberOf(SaveSchema.FieldSeed),
                Tick = (int)root.NumberOf(SaveSchema.FieldTick),
                T = root.NumberOf(SaveSchema.FieldT),
                PlaySeconds = root.NumberOf(SaveSchema.FieldPlaySeconds),
                Profile = root.TextOf(SaveSchema.FieldProfile, SimVersion.Ruleset),
                DataVersion = root.TextOf(SaveSchema.FieldDataVersion, ""),
                Hash = root.TextOf(SaveSchema.FieldHash, ""),
            };

            var state = new SimState();
            if (!JsonStateReader.Read(stateJson, state, out var problem))
                return LoadResult.Refuse("this save does not match this build's save format (" + problem + ")", true);

            if (string.IsNullOrEmpty(header.Hash)) return LoadResult.Refuse("this save has no integrity check in it", true);
            var actual = StateHash.Compute(state);
            if (string.CompareOrdinal(actual, header.Hash) != 0)
                return LoadResult.Refuse("this save is damaged (its contents do not match its checksum)", true);

            // Header metadata the state does not carry.
            state.PlaySeconds = header.PlaySeconds;

            // Balance data is a warning, never a refusal: a changed recipe is a difference, not damage.
            string warning = null;
            var current = GameDataHash.Compute(data);
            if (data != null && !string.IsNullOrEmpty(header.DataVersion) && string.CompareOrdinal(current, header.DataVersion) != 0)
                warning = "this save was made with different balance data (" + header.DataVersion + " vs " + current + ")";

            return LoadResult.Loaded(state, header, warning);
        }

        /// <summary>
        /// The header of a save without building its state — what the Load screen lists (C-10). It deliberately
        /// does <b>not</b> verify the hash: reading a directory of saves must not cost a full parse-and-hash of
        /// every one, and a listed entry is verified for real when it is actually loaded.
        /// </summary>
        public static SaveHeader ReadHeader(byte[] bytes, out string problem)
        {
            problem = "";
            if (bytes == null || bytes.Length == 0) { problem = "the file is empty"; return null; }
            string text;
            try { text = Utf8.GetString(bytes); }
            catch (Exception) { problem = "the file is not a text save (its bytes are not UTF-8)"; return null; }
            if (text.Length > 0 && text[0] == '\uFEFF') text = text.Substring(1);

            var root = JsonValue.Parse(text, out var jsonError);
            if (root == null) { problem = "the file is damaged: " + jsonError; return null; }
            if (!root.IsObject) { problem = "this is not a Relight save file"; return null; }

            var kind = root.TextOf(SaveSchema.FieldKind);
            if (string.CompareOrdinal(kind, SaveSchema.Kind) != 0)
            {
                problem = string.CompareOrdinal(kind, SaveSchema.ReferenceKind) == 0
                    ? "this is a save from the original build; it cannot be loaded here (saves are not converted)"
                    : "this is not a Relight save file";
                return null;
            }

            var versionMember = root.Member(SaveSchema.FieldVersion);
            if (versionMember == null || versionMember.Kind != JsonKind.Number)
            {
                problem = "this save does not say which version it is";
                return null;
            }
            var version = (int)versionMember.Number;
            if (version != SaveSchema.Version)
            {
                problem = "unsupported save version " + version.ToString(CultureInfo.InvariantCulture)
                          + " (this build reads version " + SaveSchema.Version.ToString(CultureInfo.InvariantCulture) + ")";
                return null;
            }

            return new SaveHeader
            {
                Version = version,
                Kind = kind,
                SavedAt = root.TextOf(SaveSchema.FieldSavedAt, ""),
                Seed = (int)root.NumberOf(SaveSchema.FieldSeed),
                Tick = (int)root.NumberOf(SaveSchema.FieldTick),
                T = root.NumberOf(SaveSchema.FieldT),
                PlaySeconds = root.NumberOf(SaveSchema.FieldPlaySeconds),
                Profile = root.TextOf(SaveSchema.FieldProfile, SimVersion.Ruleset),
                DataVersion = root.TextOf(SaveSchema.FieldDataVersion, ""),
                Hash = root.TextOf(SaveSchema.FieldHash, ""),
            };
        }

        private static void Member(StringBuilder sb, string key, string value, bool first)
        {
            if (!first) sb.Append(',');
            sb.Append(CanonicalJsonWriter.QuoteString(key));
            sb.Append(':');
            sb.Append(value);
        }
    }

    /// <summary>
    /// The outcome of reading a save: a state, or a refusal with the reason to show. A refusal changes nothing —
    /// the caller's existing simulation is still running and still loadable (TECHNICAL_ARCHITECTURE.md §9.4.5).
    /// </summary>
    public sealed class LoadResult
    {
        public bool Ok { get; private set; }
        /// <summary>Why the file was refused; empty when <see cref="Ok"/>.</summary>
        public string Reason { get; private set; }
        /// <summary>A difference worth telling the player about on a file that did load (balance data).</summary>
        public string Warning { get; private set; }
        public SimState State { get; private set; }
        public SaveHeader Header { get; private set; }
        /// <summary>The file this state actually came from, when a store had to fall back (a .bak or an older ring slot).</summary>
        public string Path { get; internal set; }
        /// <summary>Set when the file asked for was unusable and this one was used instead.</summary>
        public string Recovered { get; internal set; }

        /// <summary>
        /// True when the file <i>is</i> one of ours but is broken (truncated, edited, checksum mismatch), false when
        /// it is simply not this build's save (another game's file, a reference-build save, a future version).
        /// Only a damaged file is set aside as <c>.corrupt</c> and recovered from its <c>.bak</c> (§9.4.5) — a
        /// foreign file the player pointed at by mistake is left exactly where it is.
        /// </summary>
        public bool Damaged { get; private set; }

        public static LoadResult Refuse(string reason, bool damaged = false)
            => new LoadResult { Ok = false, Reason = reason ?? "refused", Damaged = damaged };

        public static LoadResult Loaded(SimState state, SaveHeader header, string warning = null)
            => new LoadResult { Ok = true, Reason = "", State = state, Header = header, Warning = warning };

        public override string ToString() => Ok ? "loaded" : "refused: " + Reason;
    }
}
