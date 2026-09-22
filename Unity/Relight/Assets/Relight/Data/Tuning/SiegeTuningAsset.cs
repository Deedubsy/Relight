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
    ///
    /// REL-84 added the last three groups — the failure path, where a wave enters, and the camp resident's patrol
    /// and memory. They were literals in DirectorRules, DirectorPhase, EnemyPhase and Enemies; every default here
    /// is the number those files carried, so an asset saved before REL-84 (which has none of these keys) keeps its
    /// field initialisers and behaves exactly as it did.
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

        [Header("The failure path (REL-84, E-18)")]
        [Tooltip("How close to the core's edge a raid body must be, in tiles, to block the core's repair (U-P-19).")]
        [SerializeField] private double coreThreatTiles = 12;
        [Tooltip("Seconds past a large raid's planned end after which it is called off whatever is still alive (U-P-20).")]
        [SerializeField] private double majorOverrunSeconds = 300;
        [Tooltip("Seconds after a large raid ends before any survivor still on the map is removed (U-P-21).")]
        [SerializeField] private double withdrawPurgeSeconds = 120;

        [Header("Where a wave enters (REL-84)")]
        [Tooltip("The nearest a wave may enter, in BFS steps from the core. Below this it is already inside.")]
        [SerializeField] private int entryNearSteps = 8;
        [Tooltip("The furthest a wave may enter. Past this the walk in is a wait, not a raid.")]
        [SerializeField] private int entryFarSteps = 76;
        [Tooltip("A wave never enters, or is born, this close to the engineer, in tiles.")]
        [SerializeField] private double safeFromEngineerTiles = 28;
        [Tooltip("The entry distance a single wave is scored against, in steps: 100 points per step away from it.")]
        [SerializeField] private int stagingIdealSteps = 24;
        [Tooltip("The same, for a major assault's sector approaches, which stand further out.")]
        [SerializeField] private int approachIdealSteps = 42;
        [Tooltip("Clearance a small raid's warning must keep from the next large raid's warning window, in seconds.")]
        [SerializeField] private double minorMajorGapSeconds = 120;

        [Header("Camp residents: patrol and memory (REL-84)")]
        [Tooltip("Past this distance from the engineer, in tiles, an idle camp resident is not ticked at all.")]
        [SerializeField] private double guardSleepTiles = 60;
        [Tooltip("The radius, in tiles, of a camp resident's patrol loop around its birthplace.")]
        [SerializeField] private double guardPatrolRadiusTiles = 2;
        [Tooltip("The share of its own speed a resident patrols at. It chases at the full speed.")]
        [SerializeField] private double guardPatrolSpeedMul = 0.35;
        [Tooltip("How long a body remembers where it last saw the engineer, in seconds.")]
        [SerializeField] private double memorySeconds = 6;

        [Header("Raid pacing and fair timing (REL-75, E-21)")]
        [Tooltip("The quiet spell after a large raid's recovery, in seconds: no warning and no small raid (U-P-17).")]
        [SerializeField] private double quietAfterMajorSeconds = 480;
        [Tooltip("Ordinary small raids that must arrive between one large raid and the next warning.")]
        [SerializeField] private int minorsPerCycle = 1;
        [Tooltip("How long after the quiet spell, in seconds, a large-raid warning waits for the cycle's small raid (U-P-22).")]
        [SerializeField] private double minorHoldCapSeconds = 600;
        [Tooltip("The longest, in seconds, a due small raid waits while the engineer is down or fighting a camp (U-P-23).")]
        [SerializeField] private double minorDelayCapSeconds = 180;
        [Tooltip("Past this straight-line distance from the raid's target, in tiles, the engineer is far (U-P-24).")]
        [SerializeField] private double farTargetTiles = 150;
        [Tooltip("Seconds added to a small raid's warning when the engineer is far from its target (U-P-24).")]
        [SerializeField] private double farWarningExtraSeconds = 30;
        [Tooltip("The share of the Home core's hit points the first small raid cannot take it below (U-P-25).")]
        [SerializeField] private double firstMinorCoreFloor = 0.5;

        public SiegeTuning ToRecord() => new SiegeTuning(
            minorWarningSeconds, minorWarningRangeSeconds, minorStageRetrySeconds, noticeHoldSeconds,
            majorWaves, majorWaveGapSeconds, majorWaveSpreadSeconds, majorWaveTailSeconds,
            majorWaveGrowth, majorFirstApproaches, majorApproachStep, majorSpitterWave,
            majorGrowthPerAssault, majorGrowthCap,
            majorBreakerAssault, majorBreakerWave, majorBreakerShare,
            guardPursuitTiles, guardReengageTiles,
            KindText, Source, Provisional,
            coreThreatTiles, majorOverrunSeconds, withdrawPurgeSeconds,
            entryNearSteps, entryFarSteps, safeFromEngineerTiles,
            stagingIdealSteps, approachIdealSteps, minorMajorGapSeconds,
            guardSleepTiles, guardPatrolRadiusTiles, guardPatrolSpeedMul,
            memorySeconds,
            quietAfterMajorSeconds, minorsPerCycle, minorHoldCapSeconds,
            minorDelayCapSeconds, farTargetTiles, farWarningExtraSeconds,
            firstMinorCoreFloor);

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
            coreThreatTiles = s.CoreThreatTiles;
            majorOverrunSeconds = s.MajorOverrunS;
            withdrawPurgeSeconds = s.WithdrawPurgeS;
            entryNearSteps = s.EntryNearSteps;
            entryFarSteps = s.EntryFarSteps;
            safeFromEngineerTiles = s.SafeFromEngineerTiles;
            stagingIdealSteps = s.StagingIdealSteps;
            approachIdealSteps = s.ApproachIdealSteps;
            minorMajorGapSeconds = s.MinorMajorGapS;
            guardSleepTiles = s.GuardSleepTiles;
            guardPatrolRadiusTiles = s.GuardPatrolRadiusTiles;
            guardPatrolSpeedMul = s.GuardPatrolSpeedMul;
            memorySeconds = s.MemoryS;
            quietAfterMajorSeconds = s.QuietAfterMajorS;
            minorsPerCycle = s.MinorsPerCycle;
            minorHoldCapSeconds = s.MinorHoldCapS;
            minorDelayCapSeconds = s.MinorDelayCapS;
            farTargetTiles = s.FarTargetTiles;
            farWarningExtraSeconds = s.FarWarningExtraS;
            firstMinorCoreFloor = s.FirstMinorCoreFloor;
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
            if (coreThreatTiles < 0) return "a ring round the core cannot have a negative radius";
            if (majorOverrunSeconds < 0) return "an assault cannot be called off before its planned end";
            if (withdrawPurgeSeconds < 0) return "survivors cannot be swept up before the raid they belong to ends";
            if (entryNearSteps < 0) return "a wave cannot enter fewer than no steps from the core";
            if (entryFarSteps <= entryNearSteps) return "the entry band has to have room between its near and far edges";
            if (safeFromEngineerTiles < 0) return "the ring kept clear of the engineer cannot be negative";
            if (stagingIdealSteps < entryNearSteps || stagingIdealSteps > entryFarSteps)
                return "the ideal entry distance has to be inside the entry band";
            if (approachIdealSteps < entryNearSteps || approachIdealSteps > entryFarSteps)
                return "the ideal approach distance has to be inside the entry band";
            if (minorMajorGapSeconds < 0) return "a small raid cannot be asked to keep a negative gap from a large one";
            if (guardSleepTiles <= 0) return "a resident that is never ticked cannot defend its camp";
            if (guardPatrolRadiusTiles < 0) return "a patrol cannot have a negative radius";
            if (guardPatrolSpeedMul < 0 || guardPatrolSpeedMul > 1)
                return "a patrol is a share of the body's own speed, never more than it";
            if (memorySeconds < 0) return "a body cannot remember the engineer for less than no time";
            if (quietAfterMajorSeconds < 0) return "a quiet spell cannot be shorter than none";
            if (minorsPerCycle < 0) return "a cycle cannot owe a negative number of small raids";
            if (minorHoldCapSeconds < 0) return "a warning cannot wait a negative time for a small raid";
            if (minorDelayCapSeconds < 0) return "a small raid cannot wait a negative time for the engineer";
            if (farTargetTiles <= 0) return "the engineer cannot be far from a target at no distance";
            if (farWarningExtraSeconds < 0) return "a far target cannot shorten the warning";
            if (firstMinorCoreFloor < 0 || firstMinorCoreFloor >= 1)
                return "the first small raid's floor is a share of the core's hit points, below all of them";
            return null;
        }
    }
}
