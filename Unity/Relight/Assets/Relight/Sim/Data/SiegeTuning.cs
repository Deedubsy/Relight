namespace Relight.Sim
{
    /// <summary>
    /// The raid director's Unity-owned tuning (GP-W3/GP-W4, owner brief 2026-09-15).
    ///
    /// <see cref="RaidTuning"/> is EXPORTED from the reference tables by
    /// <c>Unity/Relight/Tools/export/exportCatalogue.ts</c> and must keep matching them, so values this port
    /// invents — a minor raid's preparation warning, how long a notice stays news, the shape of a major siege —
    /// cannot live there without the next export silently dropping them. They live here instead, beside
    /// <see cref="DefenceTuning.Fallback"/>'s precedent: a plain record with a stated default, surfaced through
    /// <c>GameData.Siege</c>, editable as <c>Data/Generated/Tuning/Tuning - Siege.asset</c> through the ordinary
    /// B-05 pipeline, and falling back to <see cref="Fallback"/> when a fixture builds a <see cref="GameData"/>
    /// without one.
    ///
    /// The major-siege members describe a SHAPE, not a second roster: the head count and its skitter/spitter split
    /// still come from the exported <see cref="RaidTuning.MajorCount"/>, <see cref="RaidTuning.MajorSkitters"/> and
    /// <see cref="RaidTuning.MajorSpitters"/>, and this record only decides how that roster is cut into waves, how
    /// many approaches each wave uses and how far apart the waves land.
    /// </summary>
    /// <param name="MinorWarningS">Shortest preparation warning before a minor raid's bodies arrive.</param>
    /// <param name="MinorWarningRangeS">Added to it by the deterministic choice, so the warning is not a metronome.</param>
    /// <param name="MinorStageRetryS">How long a warned minor raid keeps trying to find open ground before it is dropped.</param>
    /// <param name="NoticeHoldS">How long the director's last notice stays on the HUD after the thing it describes.</param>
    /// <param name="MajorWaves">How many waves a major assault is cut into.</param>
    /// <param name="MajorWaveGapS">Seconds between the starts of consecutive waves.</param>
    /// <param name="MajorWaveSpreadS">Seconds a single wave takes to finish walking on.</param>
    /// <param name="MajorWaveTailS">Grace after the last body is due before the assault is declared over.</param>
    /// <param name="MajorWaveGrowth">How much bigger each wave is than the first, as a fraction per wave index.</param>
    /// <param name="MajorFirstApproaches">How many approaches the opening wave uses.</param>
    /// <param name="MajorApproachStep">Approaches added per wave, capped by how many the map actually offers.</param>
    /// <param name="MajorSpitterWave">The first wave index that carries spitters; earlier waves are skitters only.</param>
    /// <param name="MajorGrowthPerAssault">Head count added per completed assault, as a fraction of the roster.</param>
    /// <param name="MajorGrowthCap">The most the head count can grow to, as a multiple of the roster.</param>
    /// <param name="MajorBreakerAssault">Completed major assaults before Breakers join one; 0 would put them in the first.</param>
    /// <param name="MajorBreakerWave">The first wave index that carries Breakers, once they appear at all.</param>
    /// <param name="MajorBreakerShare">Share of the roster that arrives as Breakers, taken from the skitters.</param>
    /// <param name="GuardPursuitTiles">How far from its camp a resident will chase before it breaks off and walks back.</param>
    /// <param name="GuardReengageTiles">How far back inside that it must get before it will turn and chase again.</param>
    /// <param name="CoreThreatTiles">How close to the Home core a raid body must be to block the core's repair, in tiles from the core's edge (U-P-19).</param>
    /// <param name="MajorOverrunS">Seconds past a large raid's planned end after which it is called off whatever is still alive (U-P-20).</param>
    /// <param name="WithdrawPurgeS">Seconds after a large raid ends before any survivor still on the map is removed (U-P-21).</param>
    /// <param name="EntryNearSteps">The nearest a wave may enter, in BFS steps from the core. Below this it is already inside.</param>
    /// <param name="EntryFarSteps">The furthest a wave may enter. Past this the walk in is a wait, not a raid (fair travel, GP-W3).</param>
    /// <param name="SafeFromEngineerTiles">A wave never enters, or is born, this close to the engineer (reference campaignThreat.ts:132's 28 tiles).</param>
    /// <param name="StagingIdealSteps">The entry distance a single wave is scored against: 100 points per step away from it.</param>
    /// <param name="ApproachIdealSteps">The same, for a sector approach of a major assault, which stands further out.</param>
    /// <param name="MinorMajorGapS">Clearance a small raid's own warning must keep from the next large raid's warning window.</param>
    /// <param name="GuardSleepTiles">Past this distance from the engineer an idle camp resident is not ticked at all (reference cut-off).</param>
    /// <param name="GuardPatrolRadiusTiles">The radius of a camp resident's patrol loop around its birthplace.</param>
    /// <param name="GuardPatrolSpeedMul">The share of its own speed a camp resident patrols at; it chases at the full speed.</param>
    /// <param name="MemoryS">How long a body remembers where it last saw the engineer (reference hostileAwareness.ts HOSTILE_MEMORY_SECONDS).</param>
    public sealed record SiegeTuning(
        double MinorWarningS, double MinorWarningRangeS, double MinorStageRetryS, double NoticeHoldS,
        int MajorWaves, double MajorWaveGapS, double MajorWaveSpreadS, double MajorWaveTailS,
        double MajorWaveGrowth, int MajorFirstApproaches, int MajorApproachStep, int MajorSpitterWave,
        double MajorGrowthPerAssault, double MajorGrowthCap,
        int MajorBreakerAssault, int MajorBreakerWave, double MajorBreakerShare,
        double GuardPursuitTiles, double GuardReengageTiles,
        string Kind = "provisional", string Source = "", bool Provisional = true,
        // REL-84: the threat numbers that used to be literals in DirectorRules, DirectorPhase, EnemyPhase and
        // Enemies. Every default below IS the literal those files carried, so moving them changed nothing. They
        // trail the provenance columns — the same placement DefenceTuning.LowAmmoFraction uses, and for the same
        // reason: every existing construction of this record keeps compiling and takes the defaults.
        double CoreThreatTiles = 12, double MajorOverrunS = 300, double WithdrawPurgeS = 120,
        int EntryNearSteps = 8, int EntryFarSteps = 76, double SafeFromEngineerTiles = 28,
        int StagingIdealSteps = 24, int ApproachIdealSteps = 42, double MinorMajorGapS = 120,
        double GuardSleepTiles = 60, double GuardPatrolRadiusTiles = 2, double GuardPatrolSpeedMul = 0.35,
        double MemoryS = 6)
    {
        /// <summary>
        /// The values this build ships, and the ones a <see cref="GameData"/> built without a siege row uses.
        ///
        /// 30 + 0..15 s of warning is the brief's "initially around 30–45 seconds": long enough to run to a turret,
        /// drop a magazine in and take position at a walk speed of 6 tiles/s, short enough that a raid still
        /// interrupts what the player was doing. 30 s of retry is two full approach re-stagings; past that the raid
        /// is honestly dropped rather than left holding the slot. A notice outlives its encounter by 45 s so the
        /// player can read it after the shooting stops, and is then cleared instead of going stale.
        ///
        /// The siege shape is derived from what the exported tables actually cost the player (GP-W4; the arithmetic
        /// is recorded in DECISIONS.md U-D-47):
        ///
        /// * The exported roster is 40 skitters at 20 hp and 20 spitters at 50 hp = 1800 hp. A gun turret is 10 dps
        ///   (1 round/s × 10 damage) and a magazine is one round, so clearing a whole assault costs 180 rounds with
        ///   no overkill waste — a skitter is exactly two rounds and a spitter exactly five. At 10 rounds per
        ///   <c>bullet-batch</c> (2 steel + 1 copper, 6 s) that is 18 batches: 36 steel, 18 copper, 108 s of one
        ///   assembler. Three fully loaded turrets hold 150 rounds, so the assault deliberately outlasts loaded
        ///   hoppers and a mid-siege delivery is a real intervention rather than a formality.
        /// * Four waves, not the old single flat stream. Spread over a 110 s gap with a 40 s walk-on and a 30 s
        ///   tail, the assault runs 3×110 + 40 + 30 = 400 s of scheduling plus 10–30 s of walk-in from a 20–60 step
        ///   entry at 2 tiles/s: about 6.5–7 minutes, inside the brief's 6–8 minute proposal. It is a starting
        ///   point, not a mandate — the gap is the knob that moves it.
        /// * Growth 0.5 weights the waves 1 : 1.5 : 2 : 2.5, which cuts the 60-body roster into 9, 12, 18 and 21.
        ///   The opening wave is survivable by one prepared position; the last is not.
        /// * Approaches 1 → 2 → 3 → 4 (first 1, step 1) is the multi-position pressure the brief asks for, and it
        ///   escalates inside a single encounter rather than being announced as a four-sided assault the player
        ///   cannot read. Each row of a wave births one body per approach at the same second, so the defence is
        ///   split rather than queued.
        /// * Spitters from wave 1 onward. A spitter's 7-tile reach is INSIDE a turret's 9, so it never outranges the
        ///   defence; the pressure is concentration — four spitters on one turret is 20 dps against 100 hp and kills
        ///   it in 5 s while the turret kills one of them. Holding them out of the opening wave keeps the teaching
        ///   encounter honest and makes the later waves read as an escalation.
        /// * Later assaults grow by 15% of the roster each, capped at double. Escalation follows the assault the
        ///   player has actually survived, not the machine count, so building more does not silently cancel every
        ///   improvement the player just made.
        ///
        /// GP-W5 adds the Breaker's composition, which CONTENT_CATALOGUE.md §7.4 explicitly leaves to the
        /// implementer (Q06 — "per-raid composition shares and first-appearance point undecided"):
        ///
        /// * NOT in the first major assault. The first is the encounter the player has been taught to prepare for
        ///   with turrets and a rifle; a 220 hp body that eats 22 rounds before it reaches the wall belongs after
        ///   the player has survived one and had a full cycle to build ammunition. So Breakers join from the SECOND
        ///   assault onward (MajorBreakerAssault 1), and from its third wave (MajorBreakerWave 2), which is the same
        ///   "escalates inside the encounter" rule the approaches follow.
        /// * 5% of the roster, taken from the skitters, and placed at the FRONT of each wave it appears in. At the
        ///   second assault's 69 bodies that is 3 Breakers: 3 × (220 − 20) = +600 hp on top of the 2070 the growth
        ///   already gives, so the encounter costs 267 rounds rather than 207 — about a third more than the first
        ///   assault, which is a step the player can see coming and prepare for rather than a wall. They lead each
        ///   wave because they march at 1.0 tiles/s against a skitter's 2.0, so setting off first is what puts them
        ///   at the defence WITH their escort instead of alone behind it.
        ///
        /// The guard leash (36 tiles out, 22 back in) keeps the distance the camps already used and turns it into
        /// readable behaviour rather than an invisible switch: see <see cref="EnemyPhase"/>'s TickCombatActor.
        ///
        /// REL-84 added the trailing threat block, which this constructor does not pass: each one defaults to the
        /// literal its old site carried (U-P-19/20/21 and the reference's own entry, patrol and memory numbers), so
        /// <see cref="Fallback"/> still describes exactly the build that shipped.
        /// </summary>
        public static readonly SiegeTuning Fallback = new SiegeTuning(
            30, 15, 30, 45,
            4, 110, 40, 30,
            0.5, 1, 1, 1,
            0.15, 2.0,
            1, 2, 0.05,
            36, 22,
            "provisional", "Unity/Docs/TASKS.md GP-W3/GP-W4/GP-W5; owner brief 2026-09-15", true);
    }
}
