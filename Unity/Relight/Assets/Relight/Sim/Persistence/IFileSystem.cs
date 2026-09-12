using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>
    /// The one seam between the save stores and the disk (B-11). It exists for two reasons: <c>Relight.Sim</c> must
    /// stay engine-free and testable without touching a real disk, and the failure paths of
    /// TECHNICAL_ARCHITECTURE.md §9.4.5 (disk full, file locked, a write that dies half way) can only be tested if
    /// a fake can fail on demand.
    ///
    /// Every method may throw; the stores catch and turn the failure into a <see cref="SaveResult"/>, because
    /// "failures are data the UI shows" and no exception escapes to a crash dialog (§9.4.5).
    /// </summary>
    public interface IFileSystem
    {
        /// <summary>Create a directory and every missing parent. A directory that exists is not an error.</summary>
        void CreateDirectory(string path);

        bool FileExists(string path);

        byte[] ReadAllBytes(string path);

        /// <summary>
        /// Write the whole file and <b>flush it to the device</b> before returning (step 2 of §9.4.4:
        /// <c>FileStream.Flush(flushToDisk: true)</c>). Without the flush, a power cut can leave a renamed but
        /// empty file — the worst outcome, because it looks valid.
        /// </summary>
        void WriteDurable(string path, byte[] bytes);

        /// <summary>
        /// Atomically replace <paramref name="destination"/> with <paramref name="source"/>, keeping the superseded
        /// file as <paramref name="backup"/> (step 3 of §9.4.4: <c>File.Replace</c>). The destination must exist.
        /// </summary>
        void Replace(string source, string destination, string backup);

        /// <summary>Rename, for the case where the destination does not exist yet (<c>File.Move</c>).</summary>
        void Move(string source, string destination);

        /// <summary>Delete a file; a file that is not there is not an error.</summary>
        void Delete(string path);

        /// <summary>File names (not paths) directly in a directory, ordinal-sorted; empty when it does not exist.</summary>
        IReadOnlyList<string> ListFiles(string directory);
    }
}
