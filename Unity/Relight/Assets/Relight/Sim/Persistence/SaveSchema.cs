namespace Relight.Sim
{
    /// <summary>
    /// The on-disk save schema (TECHNICAL_ARCHITECTURE.md §9.1). Version 1 was the first Unity schema and there is
    /// no version 0, no compatibility branch and no reader for the reference's <c>SaveFile</c> v1–3
    /// (U-D-27, U-M-06, §9.3): a file this build does not recognise is refused with a reason, never migrated.
    ///
    /// Version 2 (2026-09-12) adds the hand's pending-refund fields (<c>state.hand.refundSteel</c> /
    /// <c>refundCopper</c>, HandCraft.cs). Because <see cref="JsonStateReader"/> guesses nothing, a schema-1 file
    /// would otherwise fail as a <i>missing field</i> — which reads as damage and gets a good file quarantined as
    /// <c>.corrupt</c>. Bumping the number keeps such a file undamaged, and since 2026-09-13 a version-1 file is
    /// <b>read through <see cref="SaveUpgrade"/></b>: its checksum is verified as written, the two refund members
    /// are defaulted to zero on the parsed document, and the file on disk is never touched (U-M-38). Anything
    /// older than <see cref="OldestReadable"/> or newer than <see cref="Version"/> is refused by number, as before.
    /// This is a default for a field a Unity save did not yet have, not the reference-save converter U-D-27 rules out.
    ///
    /// The document is one JSON object:
    /// <code>
    /// {
    ///   "dataVersion": "1f2e3d4c",     // GameDataHash of the balance tables the save was made with (warning only)
    ///   "hash":        "a1b2c3d4",     // StateHash of "state" — the integrity check and contract H1's anchor
    ///   "kind":        "relight-save-unity",
    ///   "playSeconds": 617,            // unpaused sim seconds played into this state (U-D-41; new, B-11)
    ///   "profile":     "exploration-v2",   // inert slot field, kept from profileSlot() (U-D-32)
    ///   "savedAt":     "2026-09-12T10:11:12.0000000Z",   // metadata, excluded from the hash
    ///   "seed":        3,
    ///   "state":       { ... },        // the whole SimState through the IStateVisitor contract
    ///   "t":           617.0,          // sim seconds  (duplicated in the header so a slot list needs no full parse)
    ///   "tick":        12345,          // tile ticks   (ditto)
    ///   "version":     2
    /// }
    /// </code>
    /// Keys are written in ordinal order, as <see cref="CanonicalJsonWriter"/> writes every object; a reader looks
    /// members up by name, so order never matters on the way in.
    ///
    /// Transients are excluded structurally, not by a list: <c>events</c> is never visited, the tick accumulator
    /// is host state on <see cref="Simulation"/>, and there is no speed (U-D-04) — the reference's three
    /// <c>SAVE_TRANSIENT</c> fields (packages/sim/src/save.ts:48).
    /// </summary>
    public static class SaveSchema
    {
        /// <summary>Schema version this build writes. Files from <see cref="OldestReadable"/> up to it are read.</summary>
        public const int Version = 2;

        /// <summary>
        /// The oldest schema version this build still reads (through <see cref="SaveUpgrade"/>). Never 0: there
        /// was no version 0, and the reference build's <c>relight-save</c> files are a different kind entirely.
        /// </summary>
        public const int OldestReadable = 1;

        /// <summary>File marker. The reference's marker is <c>relight-save</c>; a different string by design.</summary>
        public const string Kind = "relight-save-unity";

        /// <summary>The reference marker, recognised only so its refusal can say what the file actually is.</summary>
        public const string ReferenceKind = "relight-save";

        public const string FieldVersion = "version";
        public const string FieldKind = "kind";
        public const string FieldSavedAt = "savedAt";
        public const string FieldSeed = "seed";
        public const string FieldTick = "tick";
        public const string FieldT = "t";
        public const string FieldPlaySeconds = "playSeconds";
        public const string FieldProfile = "profile";
        public const string FieldDataVersion = "dataVersion";
        public const string FieldHash = "hash";
        public const string FieldState = "state";

        /// <summary>The file extension every save and the ring index use.</summary>
        public const string Extension = ".json";
    }

    /// <summary>
    /// Everything a slot list needs without reading the state (C-10's Load screen, U-D-41): the header of one save.
    /// </summary>
    public sealed class SaveHeader
    {
        public int Version;
        public string Kind;
        public string SavedAt;
        public int Seed;
        public int Tick;
        public double T;
        public double PlaySeconds;
        public string Profile;
        public string DataVersion;
        public string Hash;

        /// <summary>Sim elapsed as H:MM:SS — labelled <i>sim time</i>, never playtime (UI_AND_ONBOARDING.md §2.6.2).</summary>
        public string SimClock => PlayClock.Clock(T);
        /// <summary>Playtime as H:MM:SS.</summary>
        public string PlayClockText => PlayClock.Clock(PlaySeconds);
    }
}
