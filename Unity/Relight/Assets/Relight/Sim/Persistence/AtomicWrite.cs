using System;
using System.Globalization;

namespace Relight.Sim
{
    /// <summary>
    /// The write protocol of TECHNICAL_ARCHITECTURE.md §9.4.4, used by <b>every</b> save — manual, autosave,
    /// save-on-quit and the ring index alike:
    /// <list type="number">
    /// <item>write the bytes to <c>&lt;target&gt;.tmp</c> <b>in the same directory</b> (a cross-directory temp file
    ///       cannot be renamed atomically across volumes);</item>
    /// <item>flush to the device before closing (<see cref="IFileSystem.WriteDurable"/>);</item>
    /// <item>replace atomically, keeping the superseded file as <c>&lt;target&gt;.bak</c>, or plain-move when the
    ///       target does not exist yet;</item>
    /// <item>the caller updates any index only after this reports success.</item>
    /// </list>
    /// A failure at any step deletes the temp and leaves <c>&lt;target&gt;</c> and its <c>.bak</c> untouched, so a
    /// recoverable previous save always exists.
    /// </summary>
    public static class AtomicWrite
    {
        public const string TempSuffix = ".tmp";
        public const string BackupSuffix = ".bak";
        public const string CorruptSuffix = ".corrupt";

        /// <summary>
        /// Writes <paramref name="bytes"/> to <paramref name="target"/>. <paramref name="attempt"/> &gt; 0 uses a
        /// fresh temp name (<c>.tmp-1</c>, <c>.tmp-2</c>, …) so a temp left locked by an antivirus or cloud-sync
        /// agent does not block the next try — §9.4.5 treats a lock as transient and never spins or sleeps.
        /// </summary>
        public static SaveResult Write(IFileSystem fs, string directory, string target, byte[] bytes, int attempt = 0)
        {
            if (fs == null) return SaveResult.Failed(target, "no file system");
            if (bytes == null) return SaveResult.Failed(target, "nothing to write");

            try { fs.CreateDirectory(directory); }
            catch (Exception e) { return SaveResult.Failed(target, Describe(e, "the saves folder could not be created")); }

            var temp = target + TempSuffix;
            if (attempt > 0) temp += "-" + attempt.ToString(CultureInfo.InvariantCulture);

            try
            {
                fs.WriteDurable(temp, bytes);
            }
            catch (Exception e)
            {
                TryDelete(fs, temp);
                return SaveResult.Failed(target, Describe(e, "the save could not be written"));
            }

            try
            {
                if (fs.FileExists(target)) fs.Replace(temp, target, target + BackupSuffix);
                else fs.Move(temp, target);
            }
            catch (Exception e)
            {
                TryDelete(fs, temp);
                return SaveResult.Failed(target, Describe(e, "the save could not replace the previous file"));
            }

            return SaveResult.Wrote(target, bytes.Length);
        }

        /// <summary>Delete without ever throwing (cleanup paths must not fail a save that already failed).</summary>
        public static void TryDelete(IFileSystem fs, string path)
        {
            try { fs.Delete(path); }
            catch (Exception) { /* nothing to do: the temp is already unreadable or gone */ }
        }

        /// <summary>
        /// A file that failed to parse is never overwritten silently: it is renamed <c>&lt;file&gt;.corrupt</c> so
        /// it can be inspected (§9.4.5). Returns the new path, or null when the rename itself failed.
        /// </summary>
        public static string SetAside(IFileSystem fs, string path)
        {
            var corrupt = path + CorruptSuffix;
            try
            {
                TryDelete(fs, corrupt);
                fs.Move(path, corrupt);
                return corrupt;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>A short, player-readable reason from an I/O exception.</summary>
        public static string Describe(Exception e, string fallback)
        {
            if (e == null) return fallback;
            if (e is System.IO.IOException && IsDiskFull(e)) return fallback + ": the disk is full";
            if (e is UnauthorizedAccessException) return fallback + ": the file is in use or not writable";
            if (e is System.IO.DirectoryNotFoundException) return fallback + ": the folder is missing";
            var message = e.Message;
            return string.IsNullOrEmpty(message) ? fallback : fallback + ": " + message;
        }

        // ERROR_DISK_FULL (0x70) and ERROR_HANDLE_DISK_FULL (0x27) on Windows; ENOSPC (28) elsewhere.
        private static bool IsDiskFull(Exception e)
        {
            var code = e.HResult & 0xFFFF;
            return code == 0x70 || code == 0x27 || code == 28;
        }
    }
}
