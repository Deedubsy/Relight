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
    /// Version 3 (2026-09-14, Phase C Wave 0) adds the Phase C subsystem objects (<c>power</c>, <c>production</c>,
    /// <c>flow</c>, <c>weapons</c>, <c>turrets</c>, <c>enemies</c>, <c>director</c>, <c>home</c>, <c>opening</c>,
    /// <c>light</c>, and the engineer's equipment members). A version-2 file has none of them; the honest default is
    /// a fresh state's value for each — nothing built, owned or scheduled — because the build that wrote the file had
    /// none of those systems. <see cref="SaveUpgrade"/> fills exactly the members that are missing, from a fresh
    /// <see cref="SimState"/>, and never a member the file already carries.
    ///
    /// Version 4 (2026-09-14, correction pass C6) adds <c>region</c>: which imported region of the city the save was
    /// made on, and where that region sits in it. <see cref="SaveRegion"/> says why <c>mapId</c> alone stopped being
    /// enough once the same city could be imported as the Home crop or whole. Nothing in the state changes, so the
    /// step is a header one: a version-3 file made on the riverfront city is stamped as the Home crop it can only
    /// have been (<see cref="SaveUpgrade"/>), and one from the synthetic map is left saying nothing. A save whose
    /// region is not the running one is moved by <see cref="SaveRelocate"/> on the way in, or refused with a reason.
    ///
    /// Version 7 (2026-09-15, GP-W3) adds the raid director's warning state: a minor raid now exists as a WARNING
    /// before it exists as a wave (<c>director.minor.startsAt</c> / <c>owed</c> / <c>spawned</c> / <c>heading</c>)
    /// and every notice carries the second it stops being news (<c>director.noticeUntil</c>). The step is a generic
    /// fill: a version-6 save's live wave arrives with <c>spawned</c> false, which would make the director treat a
    /// wave already on the map as a warning it still owes bodies for, so the step repairs that case explicitly
    /// (<see cref="SaveUpgrade"/>).
    ///
    /// Version 8 (2026-09-15, GP-W4) adds the major assault's WAVE PLAN. An assault is no longer a flat roster
    /// trickling out of a rotating approach: its whole shape — how many waves, how big each is, how many spitters
    /// it carries, the second it opens, how many approaches it comes from and which one it starts counting at — is
    /// decided once, when the assault is scheduled, and saved (<c>director.major.wave</c>, <c>waveSpawned</c>,
    /// <c>waveAnnounced</c>, <c>waveCount</c>, <c>waveSpitters</c>, <c>waveStart</c>, <c>waveApproaches</c>,
    /// <c>waveOffset</c>). Spawning is then a pure function of the plan and two counters, so a save taken mid-siege
    /// resumes the same waves on the same seconds from the same sides. A version-7 assault has no plan and a fresh
    /// state has no <c>major</c> object to default one from, so the step stamps the members explicitly, as the one
    /// wave the file was already sending (<see cref="SaveUpgrade"/>).
    ///
    /// Version 9 (2026-09-15, GP-W5) adds <c>director.major.waveBreakers</c>: how many of each wave's bodies are
    /// Breakers, taken out of its skitters and leading it. It is one more per-wave member of the same saved plan,
    /// and it needs its own step for the same reason version 8 did — a fresh director has <c>major</c> as JSON
    /// null, so <see cref="SaveUpgrade.FillMissing"/> has no template object to default it from. A version-8
    /// assault carried no Breakers, so the honest stamp is a zero for every wave the file already planned.
    /// The same version adds <c>drops</c>: the piles of cargo the engineer dropped where they died. A version-8
    /// save has none — the rule did not exist — so the generic fill's empty <c>drops</c> is the truth about it,
    /// and no stamping is needed: a fresh state carries a real <c>drops</c> object to copy the default from.
    ///
    /// Version 10 (2026-09-22, E-18) adds <c>outcome</c> to each record of <c>director.history</c>: how a finished
    /// assault ended — cleared, lost, or broken off (<see cref="RaidOutcome"/>). Before E-18 an assault had exactly
    /// one ending, so there was nothing to record. A fresh director's history is empty, so the generic fill has no
    /// template record and the step stamps 0 (cleared) on every record the file already holds — which is the truth
    /// about a build that only ever recorded an assault once its last body was gone.
    ///
    /// Version 11 (INT-04c) takes the dead <c>defeated</c> flag off each history record.
    ///
    /// Version 12 (2026-09-22, REL-75, E-21) adds the raid pacing members: <c>director.quietUntil</c>,
    /// <c>cycleMinors</c>, <c>lastMinorEnd</c> and <c>minorDelayedSince</c>, and <c>director.minor.floor</c>. The
    /// director members come from the generic fill with values that hold nothing back; a small raid already in
    /// the file is stamped with no floor (<see cref="SaveUpgrade"/>).
    ///
    /// Version 13 (2026-09-23, REL-137, batch 4) adds <c>encounters</c> (which camps have been found, cleared and
    /// claimed) and <c>site</c> on each enemy (the camp it guards). The fill supplies the first, empty; every body
    /// already in the file is stamped with no camp (<see cref="SaveUpgrade"/>).
    ///
    /// Version 14 (2026-09-23, REL-138, batch 4) adds <c>encounters.pouch</c> (the stronghold keys held),
    /// <c>stats.found</c> (what came out of camp crates, a ledger source) and <c>site</c> on each machine (the camp
    /// a cache crate belongs to). The fill supplies the first two, empty; every machine already in the file is
    /// stamped as the player's own (<see cref="SaveUpgrade"/>).
    ///
    /// Version 15 (2026-09-23, REL-140, batch 4) adds <c>encounters.opened</c> (the strongholds whose doors are
    /// open) and <c>encounters.fightUntil</c> (U-D-69 (e)'s stronghold fight clock). The fill supplies both: no door
    /// open and no fight fought.
    ///
    /// Version 16 (2026-09-23, REL-141, batch 4) adds <c>encounters.felled</c> (the strongholds whose guardian has
    /// died) and <c>encounters.cores</c> (the Power cores lying on the ground). The fill supplies both, empty.
    ///
    /// Version 17 (2026-09-23, REL-142, batch 4) adds <c>engineer.carrying</c>, the stronghold whose Power core the
    /// engineer holds in both hands, empty when none. The fill supplies it empty.
    ///
    /// Version 18 (2026-09-23, REL-143, batch 4) adds <c>encounters.plants</c>, each prepared plant's stage
    /// (prepared, commissionedAt, hp), and <c>encounters.newestPlant</c>, the plant lit most recently. The fill
    /// supplies no plant touched and none lit.
    ///
    /// The document is one JSON object:
    /// <code>
    /// {
    ///   "dataVersion": "1f2e3d4c",     // GameDataHash of the balance tables the save was made with (warning only)
    ///   "hash":        "a1b2c3d4",     // StateHash of "state" — the integrity check and contract H1's anchor
    ///   "kind":        "relight-save-unity",
    ///   "mapId":       "riverfront-arc-v4-editor-ac16d9188c05",   // the city (C-10 map binding); "" when unknown
    ///   "playSeconds": 617,            // unpaused sim seconds played into this state (U-D-41; new, B-11)
    ///   "profile":     "exploration-v2",   // inert slot field, kept from profileSlot() (U-D-32)
    ///   "region":      { "height": 367, "id": "home", "originX": 43, "originY": 91, "width": 78 },   // v4
    ///   "savedAt":     "2026-09-12T10:11:12.0000000Z",   // metadata, excluded from the hash
    ///   "seed":        3,
    ///   "state":       { ... },        // the whole SimState through the IStateVisitor contract
    ///   "t":           617.0,          // sim seconds  (duplicated in the header so a slot list needs no full parse)
    ///   "tick":        12345,          // tile ticks   (ditto)
    ///   "version":     4
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
        public const int Version = 18;

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
        /// <summary>The imported region the game was played on (C-10 map binding); absent or "" on older saves and the synthetic map.</summary>
        public const string FieldMapId = "mapId";
        /// <summary>The region record added by schema 4 (<see cref="SaveRegion"/>); absent on v1–v3.</summary>
        public const string FieldRegion = "region";

        public const string RegionId = "id";
        public const string RegionOriginX = "originX";
        public const string RegionOriginY = "originY";
        public const string RegionWidth = "width";
        public const string RegionHeight = "height";

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
        /// <summary>The map the save was made on; "" when unknown (older save) or synthetic. A Load screen greys out a row whose map is not the loaded one.</summary>
        public string MapId = "";
        /// <summary>
        /// The imported region the save was made on (schema 4). Never null: a file that does not say — or says
        /// nothing this build can place — reads as <see cref="SaveRegion.Unknown"/>, which loads unmoved.
        /// </summary>
        public SaveRegion Region = SaveRegion.Unknown;

        /// <summary>Sim elapsed as H:MM:SS — labelled <i>sim time</i>, never playtime (UI_AND_ONBOARDING.md §2.6.2).</summary>
        public string SimClock => PlayClock.Clock(T);
        /// <summary>Playtime as H:MM:SS.</summary>
        public string PlayClockText => PlayClock.Clock(PlaySeconds);
    }
}
