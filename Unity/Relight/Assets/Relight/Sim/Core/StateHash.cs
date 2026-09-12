using System.Globalization;

namespace Relight.Sim
{
    /// <summary>
    /// The Unity-canonical state hash (reference save.ts <c>fnv1a</c>/<c>stateHash</c>, lines 65–75): FNV-1a, 32-bit,
    /// over the UTF-16 code units of the canonical JSON, printed as 8 lowercase hex digits.
    ///
    /// The <b>technique</b> is ported, not the reference's hash <b>values</b>: byte-identity with the TypeScript hash
    /// is explicitly not pursued (U-M-13, TECHNICAL_ARCHITECTURE.md §10.3). What the hash is for is stated as
    /// contracts H1 (save round trip, B-11) and H2 (two runs of the same seed and command list agree) in §10.4.
    ///
    /// The reference's <c>SAVE_TRANSIENT</c> (<c>events</c>, <c>acc</c>, <c>speed</c>) needs no exclusion list here:
    /// events are never visited, the tick accumulator is host state on <see cref="Simulation"/>, and there is no
    /// speed field (U-D-04).
    /// </summary>
    public static class StateHash
    {
        public static string Compute(SimState st) => Of(CanonicalJsonWriter.Write(st));

        /// <summary>FNV-1a over a string's UTF-16 code units, 8 lowercase hex digits.</summary>
        public static string Of(string s)
        {
            unchecked
            {
                var h = 0x811c9dc5u;
                for (var i = 0; i < s.Length; i++) { h ^= s[i]; h *= 0x01000193u; }
                return h.ToString("x8", CultureInfo.InvariantCulture);
            }
        }
    }
}
