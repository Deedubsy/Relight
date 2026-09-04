---
verdict: go               # go | rework | kill | pending — Phase 3 does not start unless this is `go`
config_hash: ee23bb1c     # economy-on build (PROTO_CALIBRATED, 24×24, 200 start rounds)
control_hash: 825d2d09    # economy=0 control
test_plan_version: 52c4ca3 (PROTOTYPE_TEST_PLAN.md at the Phase 2 commit)
seed: 3
scenario_a: fresh start, ?seed=3, 2.5 h sim at 4× — not run by testers
scenario_b: ?state=b-compact-seed3 (compact bot at 3:00:00, packages/game/public/snapshots/b-compact-seed3.json), 1.5 h sim at 4× — not run by testers
testers: 0 (owner verdict on the bot calibration and the smoke test)
dates: 2026-09-03
observer: owner
---

# Map-view prototype — test results

Gate A of the programme (constitution Phase 2 → Phase 3). Every section is filled; where nothing was measured it says `not measured`. Telemetry exports would go in `test_results/` named `<tester>_<scenario>.json`; none exist.

## 1. Verdict

**`go`, written 2026-09-03 by the owner on the Phase 2 report, the bot calibration (`docs/experiments/calibration.md`, C3–C7 met on three seeds, C1/C2 missed only in the 30 s opening transient) and the headless smoke test, with no tester sessions.** The plan's §4 thresholds (three distinct shapes of five, an unprompted remark about shape, two reorders of five) were not measured: nobody played Scenario A or B. The verdict is therefore a decision to proceed on the bots' evidence, not a measurement that the front rule reads to humans; the constitution's rule 7 makes that the human's call and it was made with the single word "go" in reply to the Phase 2 report, which ended "Phase 3 begins only after a `go`". Every constant Phase 3 locks from this file is tagged `[play: Gate A]` in the design doc, and the changelog defines that tag as a lock made without sessions. The five sessions can still be run at any time; `PHASE_3_REPORT.md` decision 1 asks whether to run them in parallel with Phase 4 M1. If they contradict a lock, the lock moves and the tag changes to the session name.

## 2. Testers

| ID | Factorio hours (rough) | Mindustry / TAB / ONI played? | Scenario A | Scenario B | Control | Notes |
|---|---|---|---|---|---|---|
| T1–T5 | not measured | not measured | ☐ | ☐ | ☐ | no session run |
| C1 | not measured | not measured | ☐ | ☐ | ☐ | no session run |

Instructions given were the §5 one-sentence rules and nothing else: not applicable (no session).

## 3. Scenario A — fresh start (from telemetry)

Not measured. The bot pace for the same scenario is in `PROTOTYPE_TEST_PLAN.md` and `PHASE_2_REPORT.md` ("Scenario A at bot pace"): compact reaches the first enclosure at 1:00, 16 held at 2:00, 28 at 3:00, never sees amber after 0:30 and buys assemblers at 1:03 and 2:43 (seed 3).

Distinct shapes at 150 min: not measured. Nearest bot policy per tester: not measured.

## 4. Scenario B — mid-game snapshot (from telemetry)

Not measured. The two bot forward runs from the snapshot (`PHASE_2_REPORT.md`): the compact bot that keeps expanding sees steel 0 at ≈ 3:40, red pips at 3:50, first loss ≈ 3:55 and holds 2 blocks at 4:30; a snapshot nobody touches sees red from ≈ 4:15, first loss ≈ 4:20 and holds 22 with 6 lost at 4:30.

## 5. Criteria (from PROTOTYPE_TEST_PLAN.md)

| Criterion | Threshold | Result | Met |
|---|---|---|---|
| Distinct territory shapes at 2.5 h (Scenario A) | ≥ 3 of 5 | not measured (the six bots make three shapes on one seed: compact blob, spike line, cheapest/balanced sprawl — bots, not testers) | ☐ |
| Unprompted remark about shape | ≥ 1 tester | not measured | ☐ |
| Ring reordered (Scenario B) | ≥ 2 of 5 | not measured | ☐ |
| Read the greyed build button and changed behaviour | (observation) | not measured | ☐ |
| Control (economy=0) shape differed from economy-on shapes | (observation) | not measured (bot shapes differ: economy-off compact holds 52 at 5 h against 28 at 3 h economy-on) | ☐ |

**Rework trigger:** not evaluated. **Kill trigger:** not evaluated. The owner passed the gate without the criteria; see §1.

## 6. What testers said (verbatim, unprompted, with sim time)

Not measured (no session). The owner's only words at the gate: "go".

## 7. Post-session answers (five open questions from the plan, per tester)

Not measured.

## 8. Design constants the test informs

| Constant | Doc value | What the bots say (PHASE_2_REPORT.md) | What the sessions suggest | Decision | Owner |
|---|---|---|---|---|---|
| Edges fed per assembler (C1, §12 rate) | ~15 (Mk1 10, Mk2 20) | one Mk1 feeds a 16-block blob to 2:30 without a pip; compact never ambers (D-P2-2); E9-hourly 52–67 mag/min over 20–21 edges at 5–12 h | not measured | **locked (D-P2-2 by recommendation, D-P3 §12 sentence): one 20 mag/min assembler ≈ 7 edges of the mixed mid-game front, 15 civic / 11 residential at steady state; the early front cheaper. No Mk1/Mk2 ladder in the game (C9)** | human |
| Claim cadence humans actually play at (C2) | 5 min (§18) | bots 15 min in hour one, then 5 | not measured | **locked at 15 min in hour one, then 5 (D-P3-6); §18 generated at it** | human (was —) |
| Start ammo (C10) | 20 magazines | 200 rounds shows ▲/✕ for 30 s at 0:00; 300 does not (D-P2-1) | not measured | **kept at 20; C1/C2 scored from minute 1 (D-P2-1 by recommendation)** | human |
| Slot count per block (C3) | 1 (proto) | enclosure at 1:00 is the only way to a second assembler | not measured | **one ammo line per interior block for the sim and the slice (GAME-ASSUMPTION); the lot footprint budget is Phase 4 M1 / Phase 5 (PHASE_3_REPORT.md decision 2)** | human |
| Buffer cap (C4) | 400 magazines | stock sits at the cap from 1:15 in A; drains in ~10 min in B | not measured | **line buffer 400 magazines, a chest (§13); the Relight bank is a separate ~20,000-magazine object, Phase 10 (D-P3-4)** | human |
| Bloom rhythm (C8) | `120/(0.5+d)`, 10 % drop | not varied in the proto (E10 is Python-only) | not measured | **locked at the doc value with no feel evidence (D-P3-5); PHASE_3_REPORT.md decision 3 asks whether to port E10 first** | human |
| Survivors: "we're in" on Held (§8) | Held | Electricians ~1:05 at bot pace, Concrete crew 1:45–2:20 | not measured | **kept: on Held** | human |

## 9. Bugs, confusion, UI

| Sim time | Tester | What happened | Bug / confusion / feature request |
|---|---|---|---|
| — | — | none reported (no session) | — |

## 10. Decisions for Phase 3

The three open ones from `PHASE_2_REPORT.md`, each made by its recommendation on the "go" (the human may override any of them in `DECISIONS.md`):

1. **D-P2-1 start rounds:** keep 20 magazines; score C1/C2 from minute 1; the opening 30 s flicker is the pip ladder's lesson (§11).
2. **D-P2-2 edges per assembler:** accept; C1 locked in Phase 3 from the bot evidence (E9-hourly, E3-block, calibration) since there is no Scenario B telemetry (§12).
3. **D-P2-3 steel wall:** intended pressure; §19 says so with a `[play: Gate A]` tag.

## 11. Attachments

- `test_results/`: none.
- Bot evidence the verdict rests on: `docs/experiments/calibration.md`, `docs/experiments/calibration.json`, `PHASE_2_REPORT.md` (Measured, Smoke test).
