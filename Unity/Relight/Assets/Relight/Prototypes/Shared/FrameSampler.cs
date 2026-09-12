using System.Collections.Generic;

namespace Relight.Prototypes
{
    /// <summary>
    /// B-14. Collects per-frame wall-clock samples and reports mean / 95th percentile / max, the three figures every
    /// B-14 acceptance line asks for. Deliberately dumb: it stores the samples so a test can also dump them, and the
    /// percentile is the plain nearest-rank one (sort, take ceil(0.95 n)) so the number is reproducible by hand.
    ///
    /// This is presentation-side measurement only; nothing here runs in <c>Relight.Sim</c> and it may use
    /// <see cref="double"/>/<see cref="float"/> freely.
    /// </summary>
    public sealed class FrameSampler
    {
        private readonly List<double> _ms = new List<double>();

        public string Name { get; }

        public FrameSampler(string name) { Name = name; }

        public int Count => _ms.Count;

        public IReadOnlyList<double> Samples => _ms;

        public void Clear() => _ms.Clear();

        /// <summary>Add one frame, in milliseconds (callers pass <c>Time.unscaledDeltaTime * 1000</c>).</summary>
        public void Add(double milliseconds) => _ms.Add(milliseconds);

        public double Mean()
        {
            if (_ms.Count == 0) return 0.0;
            var total = 0.0;
            for (var i = 0; i < _ms.Count; i++) total += _ms[i];
            return total / _ms.Count;
        }

        public double Max()
        {
            var max = 0.0;
            for (var i = 0; i < _ms.Count; i++) if (_ms[i] > max) max = _ms[i];
            return max;
        }

        /// <summary>Nearest-rank percentile: sort ascending, take element ceil(p * n) - 1.</summary>
        public double Percentile(double p)
        {
            if (_ms.Count == 0) return 0.0;
            var sorted = new List<double>(_ms);
            sorted.Sort();
            var rank = (int)System.Math.Ceiling(p * sorted.Count);
            if (rank < 1) rank = 1;
            if (rank > sorted.Count) rank = sorted.Count;
            return sorted[rank - 1];
        }

        public double P95() => Percentile(0.95);

        /// <summary>One line for a log and for the evidence file.</summary>
        public string Summary() =>
            string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "{0}: frames={1} mean={2:F3} ms p95={3:F3} ms max={4:F3} ms",
                Name, Count, Mean(), P95(), Max());
    }
}
