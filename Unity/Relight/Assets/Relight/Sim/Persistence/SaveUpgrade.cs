namespace Relight.Sim
{
    /// <summary>
    /// The schema upgrades this build performs when it reads an older Unity save — two steps, chained, and
    /// deliberately not a framework: a table of "version N → N+1" steps would be the migration machinery
    /// TECHNICAL_ARCHITECTURE.md §9.3 declines. What is here is the smallest honest reading of a file this same
    /// port wrote a day or two earlier (U-M-38, 2026-09-13; Phase C Wave 0, 2026-09-14).
    ///
    /// <b>v1 → v2.</b> Version 2 added <c>state.hand.refundSteel</c> / <c>refundCopper</c> (the pending hand-craft
    /// refund, U-M-37). A version-1 file has no such debt to record — the build that wrote it discarded unreturnable
    /// plates as <c>consumed</c> (U-M-31 (2)) — so the honest default is zero, and nothing else in the state
    /// changed between the two versions. The plates that build destroyed are not resurrected: a v1 save loads as
    /// the game it was.
    ///
    /// <b>v2 → v3.</b> Version 3 added the Phase C subsystem objects (power, production, flow, weapons, turrets,
    /// enemies, director, home, opening, light, the engineer's equipment). A version-2 file was written by a build
    /// with none of those systems, so the honest default for each is what a brand-new game has: the value the same
    /// member holds in a fresh <see cref="SimState"/> document. The step fills only members the file lacks,
    /// recursing into objects both documents carry; it never touches a member the file has, and it never reaches
    /// into arrays (a list element the old build wrote is complete as written — which is why Phase C keeps its
    /// per-machine state in side tables keyed by machine id rather than as new <see cref="Machine"/> fields).
    ///
    /// <b>v3 → v4.</b> Version 4 added the <c>region</c> header record (<see cref="SaveRegion"/>). Nothing in the
    /// state changed, so the state step is empty and the work is a header one: <see cref="RegionOfOlder"/> says what
    /// region a file that predates the record must have been made on. There was exactly one imported region before
    /// this pass — the Home crop, 78x367 at (43, 91) — and only one map id to go with it, so a v1–v3 file whose
    /// <c>mapId</c> is the riverfront city's is stamped as that crop, and one that says nothing (the synthetic map,
    /// or a Phase B file) is left saying nothing and loads exactly where it is. That is a fact about the only build
    /// that could have written the file, not a guess: no build before this pass could import the city whole.
    ///
    /// Rules every step here keeps:
    /// <list type="bullet">
    /// <item>the file's own checksum is verified <i>before</i> anything is defaulted, over the document exactly as
    ///       it was written (<see cref="JsonValue.ToCanonicalJson"/>), so an upgrade can never launder damage;</item>
    /// <item>the upgrade happens on the parsed document only; the caller never writes it back. The original file
    ///       stays byte-for-byte as it was until the player saves again, and then the ordinary write protocol keeps
    ///       it as the <c>.bak</c>;</item>
    /// <item>a value the file already carries is never overwritten (<see cref="JsonValue.TryAddMember"/> refuses),
    ///       and a file whose contents contradict its version number is refused rather than guessed at;</item>
    /// <item>the result is data: "" on success, otherwise the reason, with <paramref name="damaged"/> saying whether
    ///       it is damage (quarantine and recover) or simply not a file this build can read (leave it alone).</item>
    /// </list>
    /// </summary>
    public static class SaveUpgrade
    {
        public const string Hand = "hand";
        public const string RefundSteel = "refundSteel";
        public const string RefundCopper = "refundCopper";
        /// <summary>v6 → v7 (GP-W3): the director members the warning state lives in.</summary>
        public const string Director = "director";
        public const string Minor = "minor";
        public const string Spawned = "spawned";
        public const string Owed = "owed";
        public const string StartsAt = "startsAt";
        public const string Heading = "heading";
        public const string T = "t";
        /// <summary>v7 → v8 (GP-W4): the major assault's saved wave plan.</summary>
        public const string Major = "major";
        public const string Origins = "origins";
        public const string Remaining = "remaining";
        public const string Wave = "wave";
        public const string WaveSpawned = "waveSpawned";
        public const string WaveAnnounced = "waveAnnounced";
        public const string WaveCount = "waveCount";
        public const string WaveSpitters = "waveSpitters";
        public const string WaveBreakers = "waveBreakers";
        public const string WaveStart = "waveStart";
        public const string WaveApproaches = "waveApproaches";
        public const string WaveOffset = "waveOffset";
        public const string History = "history";
        public const string Outcome = "outcome";
        public const string Defeated = "defeated";
        /// <summary>v11 → v12 (REL-75): a small raid's core floor.</summary>
        public const string Floor = "floor";

        /// <summary>
        /// The one city every pre-C6 imported save was made on (WORLD_AND_ASSETS.md §2.3). A save that names this
        /// map and no region can only have been made on the Home crop, because that is the only region that existed.
        /// </summary>
        public const string RiverfrontMapId = "riverfront-arc-v4-editor-ac16d9188c05";

        /// <summary>The Home crop as Phase C exported it: 78x367 tiles taken from the city at (43, 91).</summary>
        public static readonly SaveRegion HomeCrop = new SaveRegion("home", 43, 91, 78, 367);

        /// <summary>
        /// The region a save written before schema 4 was made on: the Home crop when it names the riverfront city,
        /// and <see cref="SaveRegion.Unknown"/> otherwise (the synthetic map, or a map this build cannot place).
        /// A file that already carries a region is never passed here — what it says about itself is the answer.
        /// </summary>
        public static SaveRegion RegionOfOlder(int version, string mapId)
        {
            if (version >= 4) return SaveRegion.Unknown;
            return string.CompareOrdinal(mapId ?? "", RiverfrontMapId) == 0 ? HomeCrop : SaveRegion.Unknown;
        }

        /// <summary>
        /// Bring the parsed <c>state</c> of a version-<paramref name="fromVersion"/> save up to
        /// <see cref="SaveSchema.Version"/>, in place. <paramref name="hash"/> is the file's header checksum.
        /// Returns "" and a one-line <paramref name="note"/> for the player on success; otherwise the refusal.
        /// A file already at the current version is left as it is with no note.
        /// </summary>
        public static string ToCurrent(JsonValue state, int fromVersion, string hash, out bool damaged, out string note)
        {
            damaged = false;
            note = null;
            if (state == null || !state.IsObject) { damaged = true; return "this save has no state in it"; }
            if (fromVersion == SaveSchema.Version) return "";
            if (fromVersion < SaveSchema.OldestReadable || fromVersion > SaveSchema.Version)
                return "unsupported save version " + fromVersion.ToString(System.Globalization.CultureInfo.InvariantCulture);

            // Integrity first, over the document as written. Only then is anything defaulted.
            if (string.IsNullOrEmpty(hash)) { damaged = true; return "this save has no integrity check in it"; }
            var written = StateHash.Of(state.ToCanonicalJson());
            if (string.CompareOrdinal(written, hash) != 0)
            {
                damaged = true;
                return "this save is damaged (its contents do not match its checksum)";
            }

            var v = fromVersion;
            var what = "";
            if (v == 1)
            {
                var problem = OneToTwo(state, out damaged);
                if (problem.Length != 0) return problem;
                what = " with no pending hand-craft refund";
                v = 2;
            }
            if (v == 2)
            {
                var problem = TwoToThree(state, out damaged);
                if (problem.Length != 0) return problem;
                what += (what.Length == 0 ? " with" : " and") + " nothing yet built, owned or scheduled by the systems added since";
                v = 3;
            }
            if (v == 3)
            {
                // v3 → v4 is a header step only (see RegionOfOlder): the state document is unchanged, so there is
                // nothing to fill here. The clause is still worth saying, because the reader may be about to move
                // the save onto a different crop of the same city and the player should know why.
                what += (what.Length == 0 ? " with" : " and") + " the region it was made on read from its map";
                v = 4;
            }
            if(v==4)
            {
                FillMissing(state,FreshDocument());
                what += " with its original resource layout and saved ammunition queue retained";
                v=5;
            }
            if (v == 5)
            {
                // v5 -> v6 adds the Home workshop's job queue, ingredient reservation and output tray (U-D-44).
                // The generic fill gives the save the new, empty members; the old `hand.crafts`/`crafting`/
                // `craftProg` fields survive untouched and HandCraft.Normalise folds them into one job on the
                // first tick, so a batch that was running is neither duplicated nor lost.
                FillMissing(state, FreshDocument());
                what += (what.Length == 0 ? " with" : " and") + " its Home workshop queue carried into the new output tray";
                v = 6;
            }
            if (v == 6)
            {
                var problem = SixToSeven(state, out damaged);
                if (problem.Length != 0) return problem;
                what += (what.Length == 0 ? " with" : " and") + " any raid that was already on the map kept as a wave rather than re-announced";
                v = 7;
            }
            if (v == 7)
            {
                var problem = SevenToEight(state, out damaged);
                if (problem.Length != 0) return problem;
                what += (what.Length == 0 ? " with" : " and") + " any major assault in progress kept as the single wave it was already sending";
                v = 8;
            }
            if (v == 8)
            {
                var problem = EightToNine(state, out damaged);
                if (problem.Length != 0) return problem;
                what += (what.Length == 0 ? " with" : " and")
                        + " no Breakers added to an assault already under way and no cargo left on the ground";
                v = 9;
            }
            if (v == 9)
            {
                var problem = NineToTen(state, out damaged);
                if (problem.Length != 0) return problem;
                what += (what.Length == 0 ? " with" : " and") + " every earlier assault recorded as cleared";
                v = 10;
            }
            if (v == 10)
            {
                var problem = TenToEleven(state, out damaged);
                if (problem.Length != 0) return problem;
                what += (what.Length == 0 ? " with" : " and") + " each earlier assault kept with the one account of how it ended";
                v = 11;
            }
            if (v == 11)
            {
                var problem = ElevenToTwelve(state, out damaged);
                if (problem.Length != 0) return problem;
                what += (what.Length == 0 ? " with" : " and") + " no quiet spell or small-raid hold owed from before the pacing rules";
                v = 12;
            }
            // Every version between OldestReadable and Version has a step above; a gap here is a programming error.
            if (v != SaveSchema.Version)
                return "unsupported save version " + fromVersion.ToString(System.Globalization.CultureInfo.InvariantCulture);

            note = "this save was made by an earlier build (save version "
                   + fromVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)
                   + ") and was read as version "
                   + SaveSchema.Version.ToString(System.Globalization.CultureInfo.InvariantCulture)
                   + what + "; the file itself is unchanged";
            return "";
        }

        /// <summary>
        /// v7 → v8: the major assault's wave plan (GP-W4).
        ///
        /// A version-7 assault was a flat roster — a head count, a start, an end and a rotation over its approaches
        /// — and the new spawner reads a saved PLAN instead. <see cref="FillMissing"/> cannot supply one: a fresh
        /// director has <c>major</c> as JSON null, so there is no template object to copy defaults from, and every
        /// new member of a live assault has to be stamped here or the load is refused for a missing field. That is
        /// exactly the trap the version-6 minor raid fell into.
        ///
        /// The honest reading of a version-7 assault is ONE wave holding exactly what the file says it still owes,
        /// opening at its own start (or at once, if it is already in flight). The file's <c>remaining</c>,
        /// <c>total</c>, <c>startsAt</c>, <c>endsAt</c> and <c>nextSpawn</c> are left untouched, so the save
        /// resumes the assault it was in rather than being handed a fresh four-wave encounter it was never warned
        /// about, and nothing it already sent is sent again. Its spitters are counted as the version-7 build would
        /// have birthed them — every third body — so the composition the player is already fighting does not
        /// change under them mid-assault. <c>waveAnnounced</c> is 1 because that build had already announced the
        /// assault; the loaded director must not announce it a second time.
        /// </summary>
        static string SevenToEight(JsonValue state, out bool damaged)
        {
            damaged = false;
            // Stamped BEFORE the generic fill, and with TryAddMember, for the same reason as SixToSeven: an object
            // under `director.major` in a version-7 file is a real assault, and nothing here may overwrite a member
            // a later file already carries.
            var major = state.Member(Director)?.Member(Major);
            if (major != null && major.IsObject)
            {
                var owed = (int)System.Math.Max(0, major.NumberOf(Remaining, 0));
                var origins = major.Member(Origins);
                var sides = origins != null && origins.IsArray && origins.Count > 0 ? origins.Count : 1;
                var opens = System.Math.Max(state.NumberOf(T, 0), major.NumberOf(StartsAt, 0));

                major.TryAddMember(Wave, JsonValue.NumberValue(0));
                major.TryAddMember(WaveSpawned, JsonValue.NumberValue(0));
                major.TryAddMember(WaveAnnounced, JsonValue.NumberValue(1));
                major.TryAddMember(WaveCount, Numbers(Whole(owed)));
                major.TryAddMember(WaveSpitters, Numbers(Whole(owed / 3)));
                major.TryAddMember(WaveStart, Numbers(CanonicalJsonWriter.Num(opens)));
                major.TryAddMember(WaveApproaches, Numbers(Whole(sides)));
                major.TryAddMember(WaveOffset, Numbers("0"));
            }
            FillMissing(state, FreshDocument());
            return "";
        }

        /// <summary>
        /// v8 → v9: Breakers in the wave plan (GP-W5).
        ///
        /// <see cref="MajorRaid.WaveBreakers"/> is one more per-wave member of the plan version 8 introduced, and
        /// it falls into the same trap: a fresh director has <c>major</c> as JSON null, so
        /// <see cref="FillMissing"/> has no template object to default it from and a live assault would be refused
        /// for a missing field. The honest stamp is a zero per planned wave — the build that wrote the file had no
        /// Breakers in it at all, and an assault the player is already fighting must not have heavier bodies
        /// appear in it halfway through because the game was saved and reloaded. The length has to match
        /// <c>waveCount</c> or <see cref="SiegePlan.Planned"/> would reject the whole plan and replace it with a
        /// residual single wave, which would lose the rest of the encounter.
        ///
        /// The same version adds <c>drops</c> (cargo dropped where the engineer died). That one needs no stamp:
        /// a fresh state carries a real, empty <c>drops</c> object, so the generic fill supplies it — and empty is
        /// the truth about a save made by a build that never dropped anything.
        /// </summary>
        static string EightToNine(JsonValue state, out bool damaged)
        {
            damaged = false;
            var major = state.Member(Director)?.Member(Major);
            if (major != null && major.IsObject)
            {
                var counts = major.Member(WaveCount);
                major.TryAddMember(WaveBreakers, Zeros(counts != null && counts.IsArray ? counts.Count : 0));
            }
            FillMissing(state, FreshDocument());
            return "";
        }

        /// <summary>
        /// v9 → v10: how a finished assault ended (E-18).
        ///
        /// <see cref="RaidRecord.Outcome"/> is a member of the ITEMS of <c>director.history</c>, and a fresh
        /// director's history is an empty list — so <see cref="FillMissing"/> has no template record to default it
        /// from, the same trap versions 7, 8 and 9 each fell into, and every record the file already holds has to
        /// be stamped here. The honest stamp is 0, <see cref="RaidOutcome.Cleared"/>: the build that wrote the file
        /// only ever recorded an assault once its last body was gone, which is exactly what Cleared means. An
        /// assault still under way needs nothing; the new ending rules simply apply to it from the next tick.
        /// </summary>
        static string NineToTen(JsonValue state, out bool damaged)
        {
            damaged = false;
            var history = state.Member(Director)?.Member(History);
            if (history != null && history.IsArray)
                for (var i = 0; i < history.Count; i++)
                {
                    var record = history.At(i);
                    if (record != null && record.IsObject) record.TryAddMember(Outcome, JsonValue.NumberValue(0));
                }
            FillMissing(state, FreshDocument());
            return "";
        }

        /// <summary>
        /// v10 → v11: the assault record loses its dead <c>defeated</c> flag (INT-04c, ENM-05).
        ///
        /// The flag copied the raid's retreat flag, nothing read it, and its name said the opposite of what it
        /// held; <see cref="RaidRecord.Outcome"/> is the one account of how an assault ended. Nothing is decided
        /// here: the outcome each record already carries is kept exactly as the file has it. The member has to be
        /// taken off the parsed document rather than merely ignored, because the reader re-hashes the state it
        /// built and a member it never visited would make every file with a finished assault read as damaged.
        /// </summary>
        static string TenToEleven(JsonValue state, out bool damaged)
        {
            damaged = false;
            var history = state.Member(Director)?.Member(History);
            if (history != null && history.IsArray)
                for (var i = 0; i < history.Count; i++)
                {
                    var record = history.At(i);
                    if (record != null && record.IsObject) record.TryRemoveMember(Defeated);
                }
            FillMissing(state, FreshDocument());
            return "";
        }

        /// <summary>
        /// v11 → v12: raid pacing and fair timing (REL-75, E-21). The director gains the quiet spell's end, the
        /// cycle's small-raid count, the last small raid's end and a due small raid's delay, and a small raid gains
        /// its core floor. The director members are plain numbers a fresh director carries, so the generic fill
        /// supplies them, and the fresh values hold nothing back: no quiet spell is owed, and the first warning the
        /// loaded game gives is where the file already had it. <c>director.minor</c> is JSON null in a fresh state,
        /// so a raid the file already has gets its <c>floor</c> stamped here as 0: the build that wrote it had no
        /// floor, and a raid under way must not gain one because the game was saved and reloaded.
        /// </summary>
        static string ElevenToTwelve(JsonValue state, out bool damaged)
        {
            damaged = false;
            var minor = state.Member(Director)?.Member(Minor);
            if (minor != null && minor.IsObject) minor.TryAddMember(Floor, JsonValue.NumberValue(0));
            FillMissing(state, FreshDocument());
            return "";
        }

        static string Whole(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);

        /// <summary>
        /// A JSON array of <paramref name="n"/> zeroes, for a new per-wave member of a plan the file already has.
        /// Built by parsing, like <see cref="Numbers"/>, because <see cref="JsonValue"/> has no array factory.
        /// </summary>
        static JsonValue Zeros(int n)
        {
            var sb = new System.Text.StringBuilder("[");
            for (var i = 0; i < n; i++) sb.Append(i > 0 ? ",0" : "0");
            sb.Append(']');
            var v = JsonValue.Parse(sb.ToString(), out var error);
            if (v == null || !v.IsArray)
                throw new System.InvalidOperationException("could not build an upgrade array: " + error);
            return v;
        }

        /// <summary>
        /// A one-element JSON array. <see cref="JsonValue"/> has no array factory, so the only way to build one is
        /// to parse it; the element text therefore comes from <see cref="CanonicalJsonWriter.Num"/> or from an
        /// invariant integer, so what is stamped reads back exactly as the writer would have written it.
        /// </summary>
        static JsonValue Numbers(string element)
        {
            var v = JsonValue.Parse("[" + element + "]", out var error);
            if (v == null || !v.IsArray)
                throw new System.InvalidOperationException("could not build an upgrade array: " + error);
            return v;
        }

        /// <summary>
        /// v6 → v7: the director's warning state (GP-W3). The generic fill supplies the new members, but their
        /// defaults describe a raid that has not arrived yet, and a version-6 save may well have been taken with a
        /// minor raid's bodies already walking. Leaving <c>spawned</c> false would make the loaded director wait
        /// for a wave that is already there, then birth a second one and never clean either up — the duplicate the
        /// brief forbids. So an existing minor is stamped as the wave it is: spawned, owing nothing, arriving at
        /// the save's own <c>t</c>, with the heading recomputed by the first tick's queries rather than guessed.
        /// </summary>
        static string SixToSeven(JsonValue state, out bool damaged)
        {
            damaged = false;
            // Stamp the live wave BEFORE the generic fill, because FillMissing adds defaults and TryAddMember
            // refuses to overwrite — which is the property that makes this safe to run on a save that already has
            // the fields. An object under `minor` in a version-6 file can only be bodies already on the map.
            var minor = state.Member(Director)?.Member(Minor);
            if (minor != null && minor.IsObject)
            {
                minor.TryAddMember(Spawned, JsonValue.BoolValue(true));
                minor.TryAddMember(Owed, JsonValue.NumberValue(0));
                minor.TryAddMember(StartsAt, JsonValue.NumberValue(state.NumberOf(T, 0)));
                // FillMissing cannot supply these: a FRESH director has no minor at all, so there is no template
                // object under `director.minor` to copy a default from. Every new member of a live wave has to be
                // stamped here, or the load is refused for a missing field. The heading is left empty rather than
                // guessed — nothing an arrived wave does reads it, and the HUD names its quarter from the origin.
                minor.TryAddMember(Heading, JsonValue.TextValue(""));
            }
            FillMissing(state, FreshDocument());
            return "";
        }

        /// <summary>v1 → v2: the pending hand-craft refund (U-M-37), defaulted to zero.</summary>
        static string OneToTwo(JsonValue state, out bool damaged)
        {
            damaged = false;
            var hand = state.Member(Hand);
            if (hand == null || !hand.IsObject)
            {
                damaged = true;
                return "this save does not match this build's save format (missing field '" + Hand + "')";
            }
            if (hand.Member(RefundSteel) != null || hand.Member(RefundCopper) != null)
                return "this save says it is version 1 but already carries version-2 fields; it was not upgraded";

            hand.TryAddMember(RefundCopper, JsonValue.NumberValue(0));
            hand.TryAddMember(RefundSteel, JsonValue.NumberValue(0));
            return "";
        }

        /// <summary>
        /// v2 → v3: every member a fresh state document has and the file lacks is added with the fresh value,
        /// recursively through objects, never into arrays, never over an existing member. A version-2 file that
        /// already has every member is simply read as it is: the shape grows as Phase C lands, and a file written
        /// before any of it has nothing to be filled yet. Cannot fail on a well-formed object, so it never reports
        /// damage; the checksum check before it is what catches damage.
        /// </summary>
        static string TwoToThree(JsonValue state, out bool damaged)
        {
            damaged = false;
            FillMissing(state, FreshDocument());
            return "";
        }

        /// <summary>The canonical document of a brand-new, un-initialised state: the source of every default.</summary>
        static JsonValue FreshDocument()
        {
            var text = CanonicalJsonWriter.Write(new SimState());
            var doc = JsonValue.Parse(text, out var error);
            if (doc == null || !doc.IsObject)
                throw new System.InvalidOperationException("fresh state did not serialise to a JSON object: " + error);
            return doc;
        }

        /// <summary>
        /// Add to <paramref name="target"/> every member <paramref name="fresh"/> has and it lacks, with the fresh
        /// value; recurse where both carry an object; never descend into arrays; never overwrite. Returns how many
        /// members were added. Public so the rule can be pinned by a test independently of the current shape.
        /// </summary>
        public static int FillMissing(JsonValue target, JsonValue fresh)
        {
            if (target == null || fresh == null || !target.IsObject || !fresh.IsObject) return 0;
            var added = 0;
            foreach (var key in fresh.Keys)
            {
                var want = fresh.Member(key);
                var have = target.Member(key);
                if (have == null)
                {
                    // Re-parse so the added node is independent of the fresh document.
                    var copy = JsonValue.Parse(want.ToCanonicalJson(), out _);
                    if (copy != null && target.TryAddMember(key, copy)) added++;
                    continue;
                }
                if (have.IsObject && want.IsObject) added += FillMissing(have, want);
                // Arrays and scalars the file carries are complete as written.
            }
            return added;
        }
    }
}
