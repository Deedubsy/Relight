using UnityEngine;
using Relight.Sim;

namespace Relight.Data
{
    /// <summary>CONTENT_CATALOGUE.md §13 (time), minus the excluded rows (§17.2).</summary>
    [CreateAssetMenu(menuName = "Relight/Tuning/Time", fileName = "Tuning - Time")]
    public sealed class TimeTuningAsset : DataDefinition
    {
        [Tooltip("Fixed simulation rate. The sim never reads Unity's frame time.")]
        [SerializeField] private int tileTps = 20;
        [SerializeField] private double tileDt = 0.05;
        [SerializeField] private double daySeconds = 1200;
        [SerializeField] private double daylightSeconds = 900;
        [SerializeField] private string openingId = "culdesac-v1";
        [SerializeField] private double campRepeatSeconds = 900;
        [SerializeField] private double decodeSeconds = 15;
        [SerializeField] private double openingMinorSlotFirstS = 900;
        [SerializeField] private double openingMinorSlotSecondS = 1080;

        public int TileTps => tileTps;
        public double TileDt => tileDt;

        public TimeTuning ToRecord() => new TimeTuning(tileTps, tileDt, daySeconds, daylightSeconds, openingId,
            campRepeatSeconds, decodeSeconds, openingMinorSlotFirstS, openingMinorSlotSecondS, KindText, Source, Provisional);

        public void Fill(TimeTuning r)
        {
            SetCommon("time", "Time", r.Kind, r.Source, r.Provisional);
            tileTps = r.TileTps; tileDt = r.TileDt; daySeconds = r.DaySeconds; daylightSeconds = r.DaylightSeconds;
            openingId = r.OpeningId; campRepeatSeconds = r.CampRepeatSeconds; decodeSeconds = r.DecodeSeconds;
            openingMinorSlotFirstS = r.OpeningMinorSlotFirstS; openingMinorSlotSecondS = r.OpeningMinorSlotSecondS;
        }

        public override string Problem()
        {
            var baseProblem = base.Problem();
            if (baseProblem != null) return baseProblem;
            if (tileTps <= 0) return "tick rate must be positive";
            if (System.Math.Abs(tileDt * tileTps - 1) > 1e-12) return "tick length and tick rate disagree";
            if (daylightSeconds > daySeconds) return "daylight is longer than the day";
            if (string.IsNullOrEmpty(openingId)) return "opening id is empty";
            return null;
        }
    }
}
