using System;
using System.Collections.Generic;
using System.IO;

namespace Relight.Sim
{
    /// <summary>
    /// The real disk (<c>System.IO</c> only — no engine types, so it still lives in <c>Relight.Sim</c>).
    /// Implements the write protocol's primitives exactly as TECHNICAL_ARCHITECTURE.md §9.4.4 specifies them.
    ///
    /// Known limit, recorded rather than claimed away (§9.4.4 step 5): .NET exposes no portable directory fsync,
    /// so a crash in the window between the rename and the directory's own flush can lose the <i>rename</i>,
    /// leaving the previous good file in place. That is the safe direction of failure and is accepted.
    /// </summary>
    public sealed class SystemFileSystem : IFileSystem
    {
        /// <summary>Buffer used for the durable write; one write of a whole save, then the flush.</summary>
        private const int BufferBytes = 64 * 1024;

        public void CreateDirectory(string path)
        {
            if (!string.IsNullOrEmpty(path)) Directory.CreateDirectory(path);
        }

        public bool FileExists(string path) => File.Exists(path);

        public byte[] ReadAllBytes(string path) => File.ReadAllBytes(path);

        public void WriteDurable(string path, byte[] bytes)
        {
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, BufferBytes, FileOptions.None))
            {
                fs.Write(bytes, 0, bytes.Length);
                fs.Flush(true);     // §9.4.4 step 2 — to the device, not just to the OS cache.
            }
        }

        public void Replace(string source, string destination, string backup)
        {
            // File.Replace is the one call that swaps and keeps the superseded file (§9.4.4 step 3). On some
            // file systems (network shares, a few Linux mounts) it is not supported; fall back to an explicit
            // backup rename plus a move, which is the same sequence with a wider non-atomic window.
            try
            {
                File.Replace(source, destination, backup, true);
            }
            catch (PlatformNotSupportedException)
            {
                ReplaceByHand(source, destination, backup);
            }
            catch (IOException) when (!File.Exists(destination))
            {
                // The destination disappeared between the check and the call; a plain move is then correct.
                File.Move(source, destination);
            }
        }

        private static void ReplaceByHand(string source, string destination, string backup)
        {
            if (!string.IsNullOrEmpty(backup))
            {
                if (File.Exists(backup)) File.Delete(backup);
                File.Move(destination, backup);
            }
            else
            {
                File.Delete(destination);
            }
            File.Move(source, destination);
        }

        public void Move(string source, string destination) => File.Move(source, destination);

        public void Delete(string path)
        {
            if (File.Exists(path)) File.Delete(path);
        }

        public IReadOnlyList<string> ListFiles(string directory)
        {
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory)) return Array.Empty<string>();
            var full = Directory.GetFiles(directory);
            var names = new List<string>(full.Length);
            for (var i = 0; i < full.Length; i++) names.Add(Path.GetFileName(full[i]));
            names.Sort(StringComparer.Ordinal);
            return names;
        }
    }
}
