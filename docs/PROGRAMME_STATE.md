# Relight — programme state

The two-page handoff. **Replaced, never appended**: this file says where the programme is
now. The per-milestone history lives in the phase reports (`PHASE_0_REPORT.md` …
`PHASE_5_REPORT.md`, `SLICE_REPORT.md`, `GATE_B.md`) and in the pre-cleanup copy
`archive/pre-phase-5-cleanup/PROGRAMME_STATE-2026-09-05.md`. Rules: `CONSTITUTION.md`.
Tasks: `PROGRESS.md`. Decisions: `DECISIONS.md`.

## Now (2026-09-05, after the pre-Phase-5 cleanup, `PROGRESS.md` T11a)

- **Phase 5 — the factory, complete — is open and nothing of it is built.** Opened
  2026-09-05 by T10 (`PHASE_5_REPORT.md`); unblocked the same day by T11, when Daniel
  decided the fourteen rows the phase could not start without ("Go with your
  recommendation for all"); tidied by T11a (this cleanup, doc-only).
- **Next task: T12 — Phase 5 M1** (recipes, rubble, the rail-yard coal at ~700 a lot, the
  75-minute hour, the `[play: <gate>]` docsync check). **Ready**: every row in its
  `blocked by` is `decided`. Its acceptance criteria are in `PROGRESS.md`.
- Phases 0–4 are complete. **Gate A** (2026-09-03) was an owner-only `go` on the bot
  calibration and the smoke test. **Gate B** (2026-09-05, `GATE_B.md`) is `proceed — with
  conditions`: one hour played by the owner, only minutes 0–10 recorded, no telemetry
  read; the conditions are D-GB-2 (knowing what to do next), D-GB-3 (rifle range) and
  D-GB-4 (enemy density), resolved inside Phase 5 and **not blockers for T12**.
- Branch `phase-4`; PR #5 open, #1–#4 merged (checked read-only on 2026-09-05). The first
  Phase 5 build task opens `phase-5` on top of it. Nothing is pushed by a task unless the
  human asks.

## What is built, what is a stand-in

**Implemented and measured** (`docs/EXPERIMENTS.md`, GREEN at commit 2f1b016, config
b95922d2; `SLICE_REPORT.md`):

- The block sim: §5's front rule, wake bloom and burn-off, the 40-unshot-arrival fall,
  proportional brownout (D-B3-4), wells, the 25-hour compact / spike / quiet-block
  policies (E1–E9).
- The street-first procedural city (D6; fixtures `packages/sim/fixtures/city{3,4,5}.json`
  are the regression set) with substations and streetlights placed by face geometry
  (D-B1-4; 3 in 8 streetlights start broken).
- The tile layer with the engineer on foot (D5): WASD, sprint, dodge, reach, pockets,
  the rifle; hand-mining and the workbench; the physical Shot line on the HQ lot
  (Excavators, Mk1 / Mk2 Assembler, one-lane belts, one inserter, turrets with hoppers);
  Generators on coal; claims from the map paid from the Depot chest; the Electricians'
  unlocks (Floodlight, Big pole, craftable Substation); crawlers, shades, hulks.
- The hour-one bot (`E-hour`): no block lost on seeds 3 / 4 / 5 at 60 minutes, rifle off
  and on; `E-hour-north` holds north from 65:29–65:34 to 75:00 on every seed. `E-rifle` at
  tile scale: the rifle decides a rescue (48 runs) and the bot spends 0.50 / 0.69 / 0.06 %
  of the hour shooting, 0.00 % in danger.
- The game (`packages/game`): world view and map view, HUD corners, the telemetry panel,
  the 3 h snapshot (`snapshot:check`), §18 and seed images (`freshness:check`).

**Stand-ins and approved-not-built changes** (each labelled where it sits in the doc):

- The eight non-Shot recipes are data only (`recipes.ts`); no machine makes them — M1.
- Machines are paid in rubble at placement (D-B2-1 (a)); the workbench recipe with a craft
  time is M1 / M2.
- The block-level "Build assembler" stand-in still exists beside the physical line
  (D-P4-5, provisional); slots are `floor(area / 600)` (D-B2-2, provisional).
- Outskirts deposits are placeholders (D-P4-3); coal rubble on rail-yard lots at ~700 a
  lot (D-P4-12) is decided and not built.
- `constants.ts` `HOUR` ends at 60 minutes; the 75-minute hour (D-HOUR-3) is decided and
  not built, so §11's "30–60 min" prose describes minute 65 until M1 regenerates it.
- The craftable Substation costs a 50 steel + 25 Cu stand-in (D-B3-1); the map claim
  holds an outskirts block on the block sim's abstract power until the Substation is
  placed (GAME-ASSUMPTION, `ground.ts`; D-CU-3).
- Turrets are unharmed waypoints in the crawler chain (D-B4-3, provisional) while §22
  says they can be chewed; the barricade chain is Phase 6.
- The hour bot hand-feeds 790–860 magazines an hour against the line's 519 (B-M6-hour)
  while §19 says hand-feeding ends by minute 10 (D-P4-11, open; measured next by T12b).
- The hybrid expedition loop (D-GB-1) is decided for Phase 6 and not built; its
  definition is Phase 6's entry criterion (`PHASES.md`).
- Not built at all: trams, the truck, barricades, cannons, undergrounds, splitters,
  filters, chests as placeable objects, the string table, undo / redo.

## Evidence

| kind | where | state |
|---|---|---|
| simulation | `docs/EXPERIMENTS.md`, `docs/experiments/*.json`, `calibration.md` | 13 experiments GREEN at 2f1b016; every generated file fresh at HEAD (`freshness:check`, 2026-09-05) |
| snapshot | `packages/game/public/snapshots/b-compact-seed3.json` | matches (compact seed 3 at 3:00:00, config 0176f61d) |
| human approval | `TEST_RESULTS.md` (Gate A), `GATE_B.md` (Gate B) | both passed by the owner alone; no outside tester has played |
| human play | `GATE_B.md` §19 table | minutes 0–10 only; 10–30 and 30–60 unrecorded; walking, first shade, burn-off, first pip unrecorded |
| waivers | `PROGRESS.md` T5, T6, T7 | waived by Daniel 2026-09-05; T6 returns as T18, the walkthrough's four STANDARDS rows ride on T19 |

## Counters

- Untagged numbers in the design doc: **33** (21 tile-scale, 12 design inputs), unchanged
  since Phase 4 M3; the cleanup added none.
- §26: **three systems, complexity 5 / 10**. Stop condition never fired.
- `STANDARDS.md`: **closed 1 of 46** (A.7, the hand-lamp sentence, at Gate B). Phase 5's
  fifteen build-with rows are listed on `PHASES.md` Phase 5.
- Engineer guards (shooting ≤ 10 %, danger ≤ 5 %, walking): every number is the bot's; no
  player number exists yet.

## Open items

- **Questions for the human** (`DECISIONS.md` "Outstanding questions"): D-CU-1 production
  siting (recommended (a), as built — not a blocker); D-CU-2 the Chemist and Polymer
  (Phase 7, not a blocker); D-CU-3 the outskirts Substation's order of operations (Phase 6
  with the hybrid, not a blocker); D-GB-2's form (blocks T12a only); D-P4-11 the idle steel
  and the hand-feed (measured by T12b before it is asked again).
- **Provisional rows a Phase 5 task will embody** (sign or reverse, any time): D-SA-2 (the
  string table, T13); D-P4-5 and C3 / D-B2-2 (slots and the stand-in, T13 / T17); D-B4-3
  (turret in the chain); D-R1 (rifle vs shade). The full list is `DECISIONS.md`.
- **Recommended rows whose rule is built and in the doc but carry no name** (D-B1-4,
  D-B3-4, D-B3-1, D5, D6, D-B1-5, D-LP-1…3 among 31): provenance gaps, not open questions.
- **Unscheduled obligations**: `DEFERRED.md` (the reference-machine soak by hand, the
  stranger test, the calibration pip bands, the soak script's home).

## Standing rules for every phase

The `STANDARDS.md` rows for the phase are checked at its exit, one minute each by a
human; no new gap without a row. The phase report and the full verification are the exit
(constitution rule 9 and "Verification policy").
