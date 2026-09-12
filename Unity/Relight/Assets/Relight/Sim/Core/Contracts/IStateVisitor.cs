using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>
    /// One walk over the whole simulation state, used for hashing (B-03), canonical JSON writing and reading (B-11).
    /// Every persistent sim type implements <see cref="IVisitable"/> and visits each of its fields exactly once,
    /// in a fixed declaration order, through these calls. A writing visitor records the value; a reading visitor
    /// replaces it. Transients (events, accumulators, caches) are never visited.
    /// Field names are the reference save keys where a reference field exists.
    /// </summary>
    public interface IStateVisitor
    {
        bool IsReading { get; }

        void Field(string name, ref bool v);
        void Field(string name, ref int v);
        void Field(string name, ref uint v);
        void Field(string name, ref double v);
        /// <summary>A string; null is allowed and round-trips as null.</summary>
        void Field(string name, ref string v);
        void Field(string name, ref int[] v);
        void Field(string name, ref double[] v);
        void Field(string name, ref byte[] v);
        void Field(string name, ref Vec2 v);

        /// <summary>A nested object; null round-trips as null. A reader creates a fresh instance with <paramref name="make"/>.</summary>
        void Object<T>(string name, ref T obj, System.Func<T> make) where T : class, IVisitable;

        /// <summary>A list of nested objects. A reader clears and refills the list.</summary>
        void List<T>(string name, List<T> list, System.Func<T> make) where T : class, IVisitable;

        /// <summary>A list of plain strings (ordinal, exact).</summary>
        void StringList(string name, List<string> list);
    }

    public interface IVisitable
    {
        void Visit(IStateVisitor v);
    }
}
