# REL-64 (PER-04) — real-city load reading (2026-09-22)

**What this is:** one measurement run on the real city, in the editor's Mono runtime. It is a reading, not a check and not a balance verdict. The owner's play is not part of it. Certification belongs to F-07 (REL-106).

**Who ran it:** the assistant, 2026-09-22, with `RealCityLoadMeasurement` (an `[Explicit]` EditMode test in `Unity/Relight/Assets/Relight/Tests/Editor/`). It never runs in an ordinary test run.

## Where and on what

- **Runtime:** Mono 6.13.0 · Unity 6000.6.0f1 · in the editor (EditMode) · editor code optimisation **Debug**.
- **Machine:** Intel i7-11700K @ 3.60 GHz · 16 logical processors · 64 GB RAM · Windows 11 (10.0.26200).
- **Build:** git `af3940b364` (main). The measurement file itself was uncommitted when run.
- **Map:** `riverfront-arc-v4-editor-ac16d9188c05` (864 × 576) · seed 1 · data `0be94136`.
- **Defence:** 8 gun turrets, 8 poles, 2 generators around the Home core; 19 machines on the map.

## Files

| File | What it is |
|---|---|
| `real-city-load-2026-09-22.txt` | The reading. Every case below, written as the run went. |
| `real-city-load-2026-09-22-first-run-own-exits.txt` | The first full run. Its 240-body case gave each body its own exit tile by mistake (see finding b). Kept because it is the first sight of that cost. |

## Results

Milliseconds of wall-clock time per sim tick. One tick is 50 ms of game time; a 60 fps frame is 16.7 ms.

| Case | Mean | Median | p95 | Max | Ticks > 16.7 ms | Costliest phase |
|---|---|---|---|---|---|---|
| No aliens | 0.38 | 0.26 | 0.47 | 53.53 | 1 of 600 | PowerPhase 0.22 |
| **B:** 65 roamer stand-ins mostly asleep + 3 full camps | 0.42 | 0.35 | 0.73 | 3.00 | 0 of 1200 | PowerPhase 0.14 |
| **A:** 240 awake, one shared exit (a director raid) | 3.88 | 2.35 | 8.04 | 47.42 | 4 of 1200 | EnemyPhase 3.47 (90%) |
| **A2:** 240 awake, 221 different exits (stress) | 165.72 | 5.43 | 11.70 | 4255.40 | 35 of 729 (stopped at the 120 s cap) | EnemyPhase 165.29 |
| 219 machines (200 unpowered, empty turrets added) | 25.96 | 25.54 | 31.67 | 42.97 | **400 of 400** | TurretPhase 24.39 (94%) |

- **Case A in two halves:** the Home core fell after tick 517 and the raid turned for home. Before: mean 6.51, p95 8.77. After: mean 1.89, p95 2.76.
- **Mask:** the real city's own lights rebuild in 0.97 ms mean (worst 1.54). 360 synthetic lights (300 discs r7, 60 cones r12) take 38.72 ms mean, **49.77 ms worst**. The offline .NET 8 figure for the same lights is 18.28 ms; its test bound is 50 ms.
- **Route, one cold field:** to the core 25.01 ms mean; breach 24.61; to an approach tile 22.43. Fields are cached until a placement or a mask rebuild.
- **HUD scans (every 150 ms in play):**
  - 19 machines, 146 aliens: `HudViewModel.Refresh` 0.29 ms, `DefenceAlertSource.Refresh` 0.06 ms.
  - 219 machines: `HudViewModel.Refresh` **44.99 ms** mean (worst 52.84); `DefenceAlertSource.Refresh` 11.38 ms.

## Findings

- **(a) A real raid of 240 is fine.** Mean 3.9 ms, p95 8 ms per tick, under one 60 fps frame.
- **(b) The route-field cache thrashes past 32 fields (latent).** With 221 different exits, each tick near the end cost about 3.9 s. The cache (`RaidFieldCache`, 32 entries, cleared whole when full) is rebuilt over and over. No director raid needs that many fields today: a raid shares its origin. Roamers (REL-42), raid types (REL-76) and REL-98 could. Filed as **REL-123**.
- **(c) Turrets cost about 0.12 ms each per tick, every tick.** 208 turrets put every tick over a 60 fps frame. The cost grows faster than the machine count. The per-turret power lookups are the suspect; this was not profiled. Filed as **REL-121**.
- **(d) The HUD problem scan is slow with many machines.** 45 ms every 150 ms at 219 machines, from `CollectProblems`. Filed as **REL-122**.
- **(e) The 360-light mask is close to its bound on Mono Debug.** Worst 49.77 ms against a 50 ms test bound set on .NET 8.

## Limits

- Editor Mono, Debug code optimisation. A player build (IL2CPP or Mono Release) was not measured.
- Roamers are stand-in residents: REL-42 is not built. Strongholds were left out.
- One machine, one run for each case.
- Case A2 and the 219-machine case are stress stagings, not play.
- The Home core was repaired between cases by the ordinary repair command, with the parts given.
