namespace Relight.Sim
{
    /// <summary>
    /// The outcome of one write. Failures are data the UI shows, exactly as command refusals are — no exception
    /// escapes a store, and the game is never paused, blocked or stopped by a failed save
    /// (TECHNICAL_ARCHITECTURE.md §9.4.5).
    /// </summary>
    public sealed class SaveResult
    {
        public bool Ok { get; private set; }
        /// <summary>Why it failed; empty when <see cref="Ok"/>.</summary>
        public string Reason { get; private set; }
        /// <summary>The file written, or the one that was being written when it failed.</summary>
        public string Path { get; private set; }
        /// <summary>What to show in the HUD, if anything (§9.4.5's non-blocking notice).</summary>
        public string Notice { get; private set; }
        /// <summary>Bytes written on success.</summary>
        public int Bytes { get; private set; }

        public static SaveResult Wrote(string path, int bytes)
            => new SaveResult { Ok = true, Reason = "", Path = path, Bytes = bytes };

        public static SaveResult Failed(string path, string reason, string notice = null)
            => new SaveResult { Ok = false, Reason = reason ?? "the save could not be written", Path = path, Notice = notice };

        /// <summary>The same outcome with something to show the player (a success that had a caveat).</summary>
        public SaveResult WithNotice(string notice)
        {
            Notice = notice;
            return this;
        }

        public override string ToString() => Ok ? "wrote " + Path : "failed: " + Reason;
    }
}
