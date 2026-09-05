# PROGRESS — the only task list

This file is the single source for what to do next. Nothing else holds the task order.
`PROGRAMME_STATE.md` says where the programme is; this file says what happens next.

Rules for this file:
1. Tasks run in the order listed. The first task whose status is `todo` or `in_progress`
   and whose `blocked by` is clear is the current task. A `blocked` task is reported and
   skipped until its blocker clears; it does not hold the tasks after it that do not
   depend on it.
2. Statuses are exactly `todo`, `in_progress`, `blocked`, `done`, `waived`. A task is `done`
   only when the file named in its `evidence` column exists and holds the result and its
   acceptance column is met. `waived` names the person and the date.
3. Only a human marks a `human` task `done` or `waived`. Claude marks a `claude` task done
   after writing its evidence.
4. `blocked by` names tasks and decision rows. A decision row blocks only the tasks that
   need it (constitution rule 7).
5. When a task changes status, add one line to the log at the bottom.
6. New tasks are inserted where they must run, with a new id; existing ids never change
   or move.

**Now:** Phase 5 open, nothing built. **T12 is the current task and is ready.** The
pre-Phase-5 cleanup (T11a) is done; the pre-cleanup version of this file, with its long
log, is `archive/pre-phase-5-cleanup/PROGRESS-2026-09-05.md`.

| id | task | owner | status | blocked by | acceptance | evidence | done on |
|---|---|---|---|---|---|---|---|
| T1 | Resolve the eight constant disagreements, rebuild the economy fix, make main green | claude | done | — | `npm run experiments` 13 / 13 green | `docs/ECONOMY_FIX_REPORT.md` §9 | 2026-09-05 |
| T2 | Re-run E-hour until no block falls on seeds 3, 4 and 5 | claude | done | — | no fall, rifle off and on | `docs/SLICE_REPORT.md` "M6 re-run after the economy fix" | 2026-09-05 |
| T3 | Layout and readability pass | claude | done | — | ten of eleven items built; the eleventh refused under D-B5-1 | `docs/LAYOUT_PASS_REPORT.md` | 2026-09-05 |
| T4 | E-rifle at tile scale: the rescue run and the steady run | claude | done | — | both runs on a commit | `docs/EXPERIMENTS.md` row `E-rifle-tile` | 2026-09-05 |
| T5 | Controls walkthrough on the reference machine | human | waived (Daniel, 2026-09-05) | — | its four STANDARDS rows ride on T19 | `docs/GATE_B.md` "Controls walkthrough" | — |
| T6 | Build a two-assembler ammo line unaided in ten minutes | human | waived (Daniel, 2026-09-05) | — | returns as T18 | `docs/GATE_B.md` "Two-assembler line" | — |
| T7 | Stranger test: eight questions, a person who has not seen the game | human | waived (Daniel, 2026-09-05) | — | unscheduled — `DEFERRED.md` | `docs/GATE_B.md` "Stranger test" | — |
| T8 | Gate B: play the §11 hour on seed 3 and fill the §19 table | human | done | — | verdict recorded; 0–10 row only | `docs/GATE_B.md` "Gate B" | 2026-09-05 |
| T9 | Absorb Gate B: fold the played hour and D-GB-1's hybrid into the doc; carry D-GB-2/3/4 into Phase 5 as conditions | claude | done | — | doc only, no code | `docs/SLICE_REPORT.md` "Absorb Gate B"; archive `PROGRAMME_STATE-2026-09-05.md` B.31 | 2026-09-05 |
| T10 | Open Phase 5 (the factory, complete) | claude | done | — | the rule-12 read written, nothing built | `docs/PHASE_5_REPORT.md` | 2026-09-05 |
| T11 | Decide the rows Phase 5 cannot start without: D-P5-1 … D-P5-7, D-P4-2, D-P4-3, D-P4-12, C5, D-B2-1 (plus D-GB-1-rider, D-HOUR-3) | human | done | T10 | all fourteen `decided`, Daniel, via "Go with your recommendation for all" | `docs/DECISIONS.md` those rows | 2026-09-05 |
| T11a | Pre-Phase-5 cleanup: one owner per kind of information, the archive, the constitution's authority and verification rules, `CLAUDE.md`, stale text reconciled, Phase 5 tasks with acceptance criteria | claude | done | T11 | doc-only; no gameplay constant moved; cheap checks, docsync, freshness, snapshot green; archive mapping written | `docs/archive/pre-phase-5-cleanup/README.md`; the T11a commit | 2026-09-05 |
| T12 | **Phase 5 M1 — recipes, rubble and the 75-minute hour**: the eight non-Shot recipes made by real machines and priced (D-B2-1 (b)); rubble as finite typed ore with visible depletion at D-P4-2's units, rubble sinks for stone; rail-yard coal rubble at ~700 a lot (D-P4-12); outskirts deposits kept as the placeholder unless a run needs them, with a tripwire that reports which half was built (D-P4-3); the HQ patches sized (C5); `constants.ts` `HOUR` re-scoped to 75 minutes and §11 regenerated, north a scored check (D-HOUR-3); the `[play: <gate>]` check in docsync | claude | todo | — | cheap checks green; `docsync:check` green after regeneration; `E-hour` green on seeds 3 / 4 / 5 at 75 minutes with north Held and two new rows — rail-yard coal reaching the Generators with ≥ 10 min margin before the Depot's coal is gone, and the arrival count at the first red pip; every new constant tagged or rowed; fixtures unchanged or the change reported as a rules change | `docs/PHASE_5_REPORT.md` section "M1" | |
| T12a | **Next-objective line** (D-GB-2, recommendation (a)): one HUD line driven by the sim's own state naming the truthful next constraint (an emptying hopper, coal minutes left, the claim's price, the front count), no screens, no list | claude | blocked | T12; D-GB-2 (the human confirms (a) or chooses (b)) | the line is never wrong against the sim's state in an `E-hour` replay on seeds 3 / 4 / 5; it changes at every §11 beat; rule 8 kept; T19 records whether a person followed it | `docs/PHASE_5_REPORT.md` section "Next-objective line" | |
| T12b | **E-hour robustness variants and a policy comparison** on the 75-minute hour (scheduled, not run): ammo automation delayed to minute 15 and 20; west claimed first instead of east; 100 steel spent on nothing at minute 5; the first supply warning ignored for three minutes; a belt tile misplaced at minute 8 and repaired at minute 9; plus a policy table (compact / spike / quiet-block) on ammo, blocks lost, walking minutes, time to first interior and coal margin | claude | blocked | T12 | each variant reported with hypothesis, seed, duration, config hash and commit; seeds 3 / 4 / 5 and 6 / 7 / 8 so no variant is tuned to the fixture seeds; a variant that loses a block names the mechanism, and no constant is changed to make it pass; D-P4-11's hand-feed share reported per variant | `docs/EXPERIMENTS.md` rows `E-hour-robust-*`, `E-policy` | |
| T12c | **E-enclosure-short (hypothesis)**: can the bot reach the first enclosure in 15–25 minutes on a small opening (the HQ and its two river neighbours), and at what ammo, coal and walking cost — an exploration, not a benchmark; the 75-minute hour stays the benchmark | claude | blocked | T12 | reported as balance exploration with its hypothesis stated first; no decided constant moved; the result is a recommendation row, not a doc edit | `docs/EXPERIMENTS.md` row `E-enclosure-short` | |
| T19 | **Short unaided opening playtest**: seed 3, minutes 0–20, one person, the observer silent; record §19's 0–10 and 10–20 rows, whether the next-objective line was truthful and followed, the first pip's minute, the walking share from the telemetry panel, and STANDARDS 4.3 / B.3 / B.6 / C.2 (one minute each) | human | blocked | T12a | the record says what was and was not observed; nothing unobserved is scored | `docs/GATE_B.md` section "Short opening playtest (T19)" | |
| T13 | **Phase 5 M2 — placement and the command layer**: every Phase 5 §13 machine with its footprint; undo / redo (1.10); drag placement taking a path (1.3); the hotbar as references (1.16); one binding table (1.17); one string table (8.5); every front edge physical (restore from commit 6694b71); the workbench recipe (D-B2-1 (b)) | claude | blocked | T12 | placement only through `Command`s from real inventories at reach; expansion validated by real placements, deliveries and inventories, and D-P4-5's block-level stand-in removed or labelled; each hotbar entry carries one line on the decision the machine opens; D-SA-2 signed or reversed by the human before the string table is called done; D-CU-1 recommended (a) as built — if the human picks (b) this task changes | `docs/PHASE_5_REPORT.md` section "M2" | |
| T14 | **Phase 5 M3 — belts complete**: undergrounds with the span shown (1.4), splitters with priority (2.3), the filter (2.5), side-loading, chests, the Depot's item list | claude | blocked | T13 | every chain of §12 buildable end to end with belts alone; one lane (D-P5-3) | `docs/PHASE_5_REPORT.md` section "M3" | |
| T15 | **Phase 5 M4 — power and the panels**: power complete on the tile layer; the machine status enum (2.6), rates (2.7), alt-mode (2.9), per-item counters (3.1); the brownout's HUD read, no map-view dim (D-P5-1) | claude | blocked | T14 | a brownout is read on the HUD bar and the machine panel in a replay; the status enum names why every stopped machine stopped | `docs/PHASE_5_REPORT.md` section "M4" | |
| T16 | **Phase 5 M5 — trams and the driven truck**: track, stops, trams, the truck driven (2.11 / 7.4 / A.9), the route on the map view while a stop is selected (D-P5-6) | claude | blocked | T15 | one tram on one track between two stops moves outskirts steel in a replay | `docs/PHASE_5_REPORT.md` section "M5" | |
| T17 | **Phase 5 M6 — slots, the blueprint file and the DoD**: slots as real lot geometry (C3 / D-B2-2 signed or reversed); the bot's blueprint file (D-P5-4, harness JSON); copy / paste (1.11); `E-chain`, `E-coal`, `E-tram`; the bot's 5 h §18 territory; the Phase 5 gate deletions (`--map lattice`, `?map=lattice`, the lattice fixtures and results, `?flow=0`, `sim.ts` `syncEdges`); the phase's full verification | claude | blocked | T16 | the 5 h territory is built from real placements and deliveries at doc rates; the three experiments reported with provenance; full verification green or every failure named, including the light-map blur's browser frame cost and the map view's resize raster; the fifteen STANDARDS rows checked | `docs/PHASE_5_REPORT.md` section "M6" | |
| T18 | **The DoD, played**: build a two-assembler ammo line in the world view, unaided, in under ten minutes (T6 returning) — the definition is on `PHASES.md` Phase 5 | human | blocked | T17 | pass / fail by the definition; the prompts needed and the minutes recorded | `docs/PHASE_5_REPORT.md` section "The DoD, played" | |

## Log

- 2026-09-05 — T1, T2, T3, T4 done; T5, T6, T7 waived and T8 done by Daniel; T9, T10 done;
  T11 done by Daniel. The long entries are in
  `archive/pre-phase-5-cleanup/PROGRESS-2026-09-05.md`.
- 2026-09-05 — T11a done (the pre-Phase-5 cleanup): evidence
  `archive/pre-phase-5-cleanup/README.md` and the commit. T12a, T12b, T12c and T19 added
  with acceptance criteria; T12–T18 given acceptance columns; statuses renamed to the
  constitution's vocabulary; T15's next-objective line moved to T12a.
