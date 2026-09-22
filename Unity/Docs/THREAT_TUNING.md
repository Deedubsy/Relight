# Threat tuning — every number in one place

**Owner of status:** `TASKS.md` (E-19). **Owner of the numbers:** the assets under
`Unity/Relight/Assets/Relight/Data/Generated/Tuning/`, read through `GameDataRegistry.Build()`.
**Owner of tuning authority:** U-D-28 (numbers may move without a decision; a new *behaviour* needs one).
Built by REL-84 (CMB-09b) on 2026-09-22. This page is a list, not a tracker: the values below are what the
assets carried on that date, and the asset wins whenever the two disagree.

## How a change takes effect

- **In the editor, during Play:** edit the asset in the Inspector. `DataDefinition.OnValidate` raises
  `DataDefinition.Edited`; `WorldBootstrap` marks a reload and, on the next frame, rebuilds the data record from the
  registry (the same build the session started with — opening balance for a versioned opening, legacy for an older
  save) and hands it to the running game with `Simulation.ReplaceData`. The sim swaps its `SimContext` between two
  ticks; every phase reads the context it is handed each tick and caches nothing, so the next tick runs on the new
  numbers. The Console logs `Relight: tuning reloaded at T=…: dataVersion A → B`; the HUD shows *Tuning reloaded*.
- **What a reload does NOT do:** touch save state. Tuning values are data, so the state hash is unmoved; a save
  written afterwards records the new `dataVersion`, and loading it against the old data gives the existing
  mismatch warning only (F1-31 stays open on whether that warning should ever block).
- **In a build:** nothing. `OnValidate` never runs in a build and a build cannot edit ScriptableObjects, so a
  development build only ever runs the values it shipped with. The issue asked for builds too; that half is not
  delivered and is recorded as such in `TASKS.md` E-19. `WorldBootstrap.ReloadData()` is public so an Admin control
  could call it, but there is nothing for it to re-read in a build.
- **Changing an asset moves the data hash** (`GameDataHash` covers every public property of every tuning record
  since REL-43). Adding a *field* to a record moves it once more, for every save; REL-84 did that.
- **Adding a field to an asset type** used to leave the on-disk asset without it: the generator saw the asset's
  fingerprint move and kept it as a "hand edit" (reported as a CONFLICT on every run). Since REL-84 the generator
  restamps an asset whose values equal the reference, so `Relight/Setup/Generate Data Assets` writes the new field.

## Measuring a change

- **The raid log** (REL-85, CMB-09c): every raid that reaches the ground leaves one line — id, size, outcome, start
  and end, length, bodies spawned, killed, peak alive, rounds fired, turrets that ran dry, core HP lost, structures
  wrecked. In Play each line goes to the Console as `Relight raid log: …`. Rounds, dry turrets and wrecks are counted
  on the whole map (not the raid's district), so two raids on the ground at once share them and both lines say so.
  The log is not save state; a new session starts a new one.
- **The real-city defence run**: the EditMode test
  `RealCityRaidTests.ADefendedHomeFacesThreeLargeRaids_AndEveryRaidLeavesOneLogLine` defends the Home on the real
  city (eight gun turrets on a pole ring, two generators, restored between raids) and runs three large-raid cycles
  with every phase of the sim, in seconds of real time. It writes its log to
  `Unity/Relight/Logs/REL-85-real-city-defence-run.txt` (ignored by git, and outside `Unity/Docs/evidence/` until the
  owner approves adding it). Change a number, run the test, compare the lines.

## Raid director — `Tuning - Raids.asset` → `RaidTuning`

Source of the row: `packages/sim/src/campaignThreat.ts` (reference export, `Kind = current`). Reader column:
which sim file reads it; **(unread)** means no sim code reads it today, so editing it changes nothing.

| Field | Value | Read by | U-P |
|---|---|---|---|
| FirstMinS / FirstRangeS | 1500 / 300 s | DirectorPhase | — |
| IntervalMinS / IntervalRangeS | 1080 / 240 s | DirectorPhase | — |
| WarningS | 300 s | DirectorPhase, OpeningPhase, OpeningRules, Threat, AdminCommands | — |
| GraceS | 780 s | DirectorPhase, OpeningRules | — |
| WindowS | 150 s | OpeningRules, SiegePlan | — |
| RecoveryS | 300 s | DirectorPhase, OpeningPhase, OpeningRules, AdminCommands | — |
| Total | 60 | SiegePlan, MachineInventory | — |
| ActiveRaidBudget | 48 | DirectorPhase, DirectorQueries | — |
| LivingBudget | 240 | DirectorPhase, DirectorQueries, DebugRaidCommand | — |
| MinorMinS / MinorRangeS | 300 / 120 s | DirectorPhase | — |
| MinorCountBase / MinorCountRange | 6 / 4 | DirectorPhase | — |
| MinorAfterMajorS | 300 s | **(unread)** | — |
| MajorCount / MajorSkitters / MajorSpitters | 60 / 40 / 20 | SiegePlan | — |
| MajorSectors | 4 | **(unread)** — `SiegeTuning.MajorFirstApproaches` does this job | — |
| SpawnEveryS | 4 s | **(unread)** — `SiegeTuning.MajorWaveSpreadS` does this job | — |
| ContactDps / StructureDps | 5 / 8 | Enemies, EnemyPhase | — |
| BreakerStructureMul | 3 | Enemies | — |
| SpeedTilesPerS | 2 | Enemies, EnemyPhase (per-kind speed is on the enemy definition) | — |
| GuardLeashTiles / GuardNoticeTiles | 12 / 8 | **(unread)** — `SiegeTuning.GuardPursuitTiles/GuardReengageTiles` (36/22) do this job | — |
| ChaseEscapeTiles | 20 | EnemyPhase | — |
| PatrolRadiusTiles | 6 | **(unread)** — the live patrol radius is `SiegeTuning.GuardPatrolRadiusTiles` = 2; wiring this field would change behaviour 2 → 6, so REL-84 did not | — |
| RadioUpgradeSteel / RadioUpgradeCopper | 15 / 10 | **(unread)** — the radio upgrade is not built | — |
| ProjectileSpeedTilesPerS / ProjectileLifeS | 8 / 2 | EnemyPhase, EnemyState | — |
| NoticeTiles / EscapeTiles | 8 / 18 | EnemyPhase | — |
| AlertRadiusTiles | 6 | Enemies, EnemyPhase | — |
| LightHesitateS | 0.65 s | Enemies | — |
| AssaultHistory | 32 | DirectorPhase (REL-84: the record cap used to be a literal 32) | — |
| LitNoticeMul / LitStepCost | 0.6 / 4 | Enemies; DirectorRules, RaidField (L-02, provisional; on the asset since the REL-84 generator fix wrote the fields to disk) | — |

## Siege — `Tuning - Siege.asset` → `SiegeTuning`

Owned by the port (`Kind = provisional`), created by REL-43; the asset holds `SiegeTuning.Fallback`.

| Field | Value | Read by | U-P |
|---|---|---|---|
| MinorWarningS / MinorWarningRangeS | 30 / 15 s | DirectorPhase | — |
| MinorStageRetryS | 30 s | DirectorPhase, RaidAccountSource | — |
| NoticeHoldS | 45 s | DirectorPhase, Threat | — |
| MajorWaves | 4 | SiegePlan, OpeningRules | — |
| MajorWaveGapS / MajorWaveSpreadS / MajorWaveTailS | 110 / 40 / 30 s | SiegePlan | — |
| MajorWaveGrowth | 0.5 | SiegePlan | — |
| MajorFirstApproaches / MajorApproachStep | 1 / 1 | SiegePlan | — |
| MajorSpitterWave | 1 | SiegePlan | — |
| MajorGrowthPerAssault / MajorGrowthCap | 0.15 / 2 | SiegePlan | — |
| MajorBreakerAssault / MajorBreakerWave / MajorBreakerShare | 1 / 2 / 0.05 | SiegePlan | — |
| GuardPursuitTiles / GuardReengageTiles | 36 / 22 | EnemyPhase | — |

### Moved out of code by REL-84 (each default IS the literal it replaced; no behaviour changed)

| Field | Value | Was | Read by | U-P |
|---|---|---|---|---|
| CoreThreatTiles | 12 | `DirectorRules.CoreThreatTiles` | DirectorRules, HomeCore.Turrets | U-P-19 |
| MajorOverrunS | 300 s | `DirectorRules.MajorOverrunS` | DirectorPhase | U-P-20 |
| WithdrawPurgeS | 120 s | `DirectorRules.WithdrawPurgeS` | DirectorPhase | U-P-21 |
| EntryNearSteps | 8 | `DirectorRules.EntryNearSteps` | DirectorRules | — |
| EntryFarSteps | 76 | `DirectorRules.EntryFarSteps` | DirectorRules, RaidField (`ReachOf` = this + `StagingMargin` 12; the reach is part of the field cache key so a reload never serves a stale box) | — |
| SafeFromEngineerTiles | 28 | `DirectorRules.SafeFromEngineerTiles`, and a second literal 28 in `DirectorRules.Place` (a body never appears on top of the engineer) that is the same reference rule (`campaignThreat.ts:132`); both now read this one field | DirectorRules | — |
| StagingIdealSteps | 24 | literal in `DirectorRules` staging score | DirectorRules | — |
| ApproachIdealSteps | 42 | literal in `DirectorRules` approach-origin score | DirectorRules | — |
| MinorMajorGapS | 120 s | literal in `DirectorPhase` minor-near-major guard | DirectorPhase | — |
| GuardSleepTiles | 60 | literal in `EnemyPhase` camp-resident cut-off | EnemyPhase | — |
| GuardPatrolRadiusTiles | 2 | literal in `EnemyPhase` patrol loop | EnemyPhase | — |
| GuardPatrolSpeedMul | 0.35 | literal in `EnemyPhase` patrol speed | EnemyPhase | — |
| MemoryS | 6 s | `Enemies.MemorySeconds` | Enemies | — |

## Still literals, on purpose

The accept line says no threat number may remain a literal in director, roamer or garrison code. These stay,
and here is why each is not a tuning number:

| Where | Literal | Why it stays |
|---|---|---|
| `DirectorRules.AdminSkipLeadS` | 10 s | The Admin panel's "warn now" lead. A developer tool, not gameplay. |
| `DirectorRules` tier bands (12 / 32 / 20 / 8 steps), `× 100` score weights, `Crowded(…, 1)` | — | The shape of the entry-scoring algorithm: bands and weights that only mean something relative to each other. Retuning them is redesigning the scorer, which is a behaviour change under U-D-28, not a number. |
| `DirectorRules` / `DirectorPhase` RNG salts (1013 and the others) | — | Determinism seeds. Changing one changes every replay; they are not balance. |
| `RaidField.StagingMargin` (12 steps past `EntryFarSteps`) and `_map` cache cap (32) | — | Buffer sizes. The margin keeps the BFS box wide enough for the far entries; the cap bounds memory. |
| `EnemyPhase` step thresholds (`> .6`, `< .5`, `>= 2`, `< 1.5`, `>= .7`, `1.2`, `: 10`) | — | Tile-crossing and stuck-detection epsilons of the movement code; they belong to the grid, not to the enemies. |
| `st.Enemies.Next % 3 == 0` spitter pick, `def.Hp : 20` | — | The 1-in-3 spitter interleave of a small raid and a fallback hp for a body with no definition. The first is a composition rule (U-D-28 behaviour), the second a guard that should never be hit. |

## Threat numbers that live elsewhere (pointers only)

- **Enemy bodies** (hp, speed, reach, damage per kind): the enemy definition assets `Data/Generated/Enemies/*` →
  `EnemyDef`; `CombatBalance.Apply` overrides the Breaker row (220 hp, 2.7, 15, 2, 1, 1.5) and machine integrity
  (20 / 2 / 120) in code — a candidate for the same treatment, outside REL-84's scope.
- **Defence** (core hp, repair costs and times, low-ammo fraction): `Tuning - Defence.asset` → `DefenceTuning`.
- **Turrets** (`TurretSight` 48 / 0.5 / 0.40 / 0.70, `TurretQueries.BlindSeconds` 3, `TurretAmmo.DefaultLowFraction`
  0.25, `Ballistics` 0.2 / 0.5 / 24): turret code; light-related ones are owned by `ALWAYS_DARK_SPEC.md`.
- **Light and dark** (`LightRules` 7 / 2 / 2, `DarkWorld` 6 / 8, `LightPhase.EnteredLightEveryS` 10):
  `ALWAYS_DARK_SPEC.md`.
- **Opening** (`OpeningPhase.StageRetryS` 60, `OpeningQueries.ScoutBullets` 8, `SourceScanTiles` 64,
  `OpeningRules.Fallback`): `Tuning - Opening.asset` and the opening code.
- **HUD** (`RaidAccountSource` 20 / 2): presentation timing, not threat.
