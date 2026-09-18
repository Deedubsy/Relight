using UnityEngine;
using Relight.Sim;

namespace Relight.Data
{
    /// <summary>
    /// GP-W3/GP-W4: the raid director's Unity-owned tuning — the values this port invents rather than exports.
    ///
    /// Everything the reference tables own (head count, the skitter/spitter split, speeds, damage, budgets) stays in
    /// <c>Tuning - Raids</c>, which <c>exportCatalogue.ts</c> regenerates. This asset holds only the shape the port
    /// decided: the minor raid's preparation warning, how long a notice stays news, and how a major assault is cut
    /// into waves. <see cref="SiegeTuning.Fallback"/> documents why each default is what it is.
    /// </summary>
    [CreateAssetMenu(menuName = "Relight/Tuning/Siege", fileName = "Tuning - Siege")]
    public sealed class SiegeTuningAsset : DataDefinition
    {
        [Header("Minor raid warning")]
        [Tooltip("Shortest preparation warning before a minor raid's bodies arrive.")]
        [SerializeField] private double minorWarningSeconds = 30;
        [Tooltip("Added to the warning by the deterministic choice, so it is not a metronome.")]
        [SerializeField] private double minorWarningRangeSeconds = 15;
        [Tooltip("How long a warned raid keeps looking for open ground before it is honestly dropped.")]
        [SerializeField] private double minorStageRetrySeconds = 30;
        [Tooltip("How long the director's last notice stays on the HUD after the thing it describes.")]
        [SerializeField] private double noticeHoldSeconds = 45;

        [Header("Major assault shape")]
        [Tooltip("How many waves the exported roster is cut into.")]
        [SerializeField] private int majorWaves = 4;
        [Tooltip("Seconds between the starts of consecutive waves. This is the knob that sets the assault's length.")]
        [SerializeField] private double majorWaveGapSeconds = 110;
        [Tooltip("Seconds a single wave takes to finish walking on.")]
        [SerializeField] private double majorWaveSpreadSeconds = 40;
        [Tooltip("Grace after the last body is due before the assault is declared over.")]
        [SerializeField] private double majorWaveTailSeconds = 30;
        [Tooltip("How much bigger each wave is than the first, per wave index. 0.5 weights four waves 1:1.5:2:2.5.")]
        [SerializeField] private double majorWaveGrowth = 0.5;

        [Header("Pressure on several positions")]
        [Tooltip("How many approaches the opening wave uses.")]
        [SerializeField] private int majorFirstApproaches = 1;
        [Tooltip("Approaches added per wave, capped by how many the map actually offers.")]
        [SerializeField] private int majorApproachStep = 1;
        [Tooltip("The first wave index carrying spitters. Earlier waves are skitters only.")]
        [SerializeField] private int majorSpitterWave = 1;

        [Header("Escalation between assaults")]
        [Tooltip("Head count added per completed assault, as a fraction of the exported roster.")]
        [SerializeField] private double majorGrowthPerAssault = 0.15;
        [Tooltip("The most the head count can grow to, as a multiple of the exported roster.")]
        [SerializeField] private double majorGrowthCap = 2.0;

        [Header("Breakers (GP-W5)")]
        [Tooltip("Completed major assaults before Breakers join one. 1 keeps them out of the first assault.")]
        [SerializeField] private int majorBreakerAssault = 1;
        [Tooltip("The first wave index carrying Breakers, once they appear at all.")]
        [SerializeField] private int majorBreakerWave = 2;
        [Tooltip("Share of the roster that arrives as Breakers. They are taken from the skitters, not added on top.")]
        [SerializeField] private double majorBreakerShare = 0.05;

        [Header("Camp residents (GP-W5)")]
        [Tooltip("How far from its camp a resident chases before it breaks off and walks back.")]
        [SerializeField] private double guardPursuitTiles = 36;
        [Tooltip("How far back inside that it must get before it will turn and chase again.")]
        [SerializeField] private double guardReengageTiles = 22;

        public SiegeTuning ToRecord() => new SiegeTuning(
            minorWarningSeconds, minorWarningRangeSeconds, minorStageRetrySeconds, noticeHoldSeconds,
            majorWaves, majorWaveGapSeconds, majorWaveSpreadSeconds, majorWaveTailSeconds,
            majorWaveGrowth, majorFirstApproaches, majorApproachStep, majorSpitterWave,
            majorGrowthPerAssault, majorGrowthCap,
            majorBreakerAssault, majorBreakerWave, majorBreakerShare,
            guardPursuitTiles, guardReengageTiles,
            KindText, Source, Provisional);

        public void Fill(SiegeTuning s)
        {
            SetCommon("siege", "Siege", s.Kind, s.Source, s.Provisional);
            minorWarningSeconds = s.MinorWarningS;
            minorWarningRangeSeconds = s.MinorWarningRangeS;
            minorStageRetrySeconds = s.MinorStageRetryS;
            noticeHoldSeconds = s.NoticeHoldS;
            majorWaves = s.MajorWaves;
            majorWaveGapSeconds = s.MajorWaveGapS;
            majorWaveSpreadSeconds = s.MajorWaveSpreadS;
            majorWaveTailSeconds = s.MajorWaveTailS;
            majorWaveGrowth = s.MajorWaveGrowth;
            majorFirstApproaches = s.MajorFirstApproaches;
            majorApproachStep = s.MajorApproachStep;
            majorSpitterWave = s.MajorSpitterWave;
            majorGrowthPerAssault = s.MajorGrowthPerAssault;
            majorGrowthCap = s.MajorGrowthCap;
            majorBreakerAssault = s.MajorBreakerAssault;
            majorBreakerWave = s.MajorBreakerWave;
            majorBreakerShare = s.MajorBreakerShare;
            guardPursuitTiles = s.GuardPursuitTiles;
            guardReengageTiles = s.GuardReengageTiles;
        }

        public override string Problem()
        {
            var baseProblem = base.Problem();
            if (baseProblem != null) return baseProblem;
            if (minorWarningSeconds <= 0) return "a raid the player is not warned about is not a warning";
            if (minorWarningRangeSeconds < 0) return "the warning's spread cannot be negative";
            if (minorStageRetrySeconds <= 0) return "a warned raid needs time to find open ground";
            if (noticeHoldSeconds < 0) return "a notice cannot be held for less than no time";
            if (majorWaves < 1) return "an assault needs at least one wave";
            if (majorWaveGapSeconds <= 0) return "waves that start together are one wave";
            if (majorWaveSpreadSeconds < 0 || majorWaveTailSeconds < 0) return "a wave cannot take negative time";
            if (majorWaveGrowth < 0) return "a later wave cannot be smaller than the first by growing";
            if (majorFirstApproaches < 1) return "the opening wave has to come from somewhere";
            if (majorApproachStep < 0) return "an assault cannot narrow as it escalates";
            if (majorSpitterWave < 0) return "the spitter wave index cannot be negative";
            if (majorGrowthPerAssault < 0) return "assaults cannot shrink as the player survives them";
            if (majorGrowthCap < 1) return "the growth cap cannot be under the roster it caps";
            if (majorBreakerAssault < 0) return "the Breaker's first assault cannot be negative";
            if (majorBreakerWave < 0) return "the Breaker's first wave index cannot be negative";
            if (majorBreakerShare < 0 || majorBreakerShare > 1) return "the Breaker share is a fraction of the roster";
            if (guardPursuitTiles <= 0) return "a camp resident that will not leave its tile cannot defend the camp";
            if (guardReengageTiles < 0 || guardReengageTiles > guardPursuitTiles)
                return "a resident must come back inside its leash before it turns and chases again";
            return null;
        }
    }
}
