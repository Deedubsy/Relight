namespace Relight.Sim
{
    /// <summary>
    /// The schema upgrades this build performs when it reads an older Unity save — exactly one so far, and
    /// deliberately not a framework: a table of "version N → N+1" steps would be the migration machinery
    /// TECHNICAL_ARCHITECTURE.md §9.3 declines. What is here is the smallest honest reading of a file this same
    /// port wrote a day earlier (U-M-38, 2026-09-13).
    ///
    /// <b>v1 → v2.</b> Version 2 added <c>state.hand.refundSteel</c> / <c>refundCopper</c> (the pending hand-craft
    /// refund, U-M-37). A version-1 file has no such debt to record — the build that wrote it discarded unreturnable
    /// plates as <c>consumed</c> (U-M-31 (2)) — so the honest default is zero, and nothing else in the state
    /// changed between the two versions. The plates that build destroyed are not resurrected: a v1 save loads as
    /// the game it was.
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

            // fromVersion == 1 is the only case left.
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
            note = "this save was made by an earlier build (save version 1) and was read as version "
                   + SaveSchema.Version.ToString(System.Globalization.CultureInfo.InvariantCulture)
                   + " with no pending hand-craft refund; the file itself is unchanged";
            return "";
        }
    }
}
