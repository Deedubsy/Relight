---
verdict: pending          # go | rework | kill | pending — Phase 3 does not start unless this is `go`
config_hash: ee23bb1c     # economy-on build the testers played (PROTO_CALIBRATED, 24×24, 200 start rounds)
control_hash: 825d2d09    # economy=0 control
test_plan_version:        # commit or date of PROTOTYPE_TEST_PLAN.md used
seed: 3
scenario_a: fresh start, ?seed=3, 2.5 h sim at 4×
scenario_b: ?state=b-compact-seed3 (compact bot at 3:00:00, packages/proto/public/snapshots/b-compact-seed3.json), 1.5 h sim at 4×
testers: 5 + 1 control
dates:
observer:
---

# Map-view prototype — test results

Gate A of the programme (constitution Phase 2 → Phase 3). Fill every section; where something wasn't measured, write `not measured` rather than leaving it blank. Telemetry exports go in `test_results/` named `<tester>_<scenario>.json` and are the source for every number below; every export must carry `meta.configHash` `ee23bb1c` (control `825d2d09`) and the right `meta.scenario`.

## 1. Verdict

One paragraph. Which thresholds from §4 were met, which weren't, and why the verdict is what it is. If `rework`, name the single thing to change. If `kill`, name what the testers did instead of engaging with shape.

## 2. Testers

| ID | Factorio hours (rough) | Mindustry / TAB / ONI played? | Scenario A | Scenario B | Control | Notes |
|---|---|---|---|---|---|---|
| T1 | | | ☐ | ☐ | ☐ | |
| T2 | | | ☐ | ☐ | ☐ | |
| T3 | | | ☐ | ☐ | ☐ | |
| T4 | | | ☐ | ☐ | ☐ | |
| T5 | | | ☐ | ☐ | ☐ | |
| C1 | | | ☐ | ☐ | ☑ economy=0 | |

Instructions given were the §5 one-sentence rules and nothing else: ☐ yes ☐ no (if no, what else)

## 3. Scenario A — fresh start (from telemetry)

| Tester | Claims/h (h1 / h2 / h3) | Held @ 60 / 120 / 150 | First enclosure | Interior @ 150 | Bounding box @ 150 | Perimeter/area @ 150 | Assemblers bought (sim time) | Slots free @ 150 | Blocks lost | Reorders | First amber / red |
|---|---|---|---|---|---|---|---|---|---|---|---|
| T1 | | | | | | | | | | | |
| T2 | | | | | | | | | | | |
| T3 | | | | | | | | | | | |
| T4 | | | | | | | | | | | |
| T5 | | | | | | | | | | | |
| C1 | | | | | | | | | | | |

Distinct shapes at 150 min (perimeter/area differs > 0.15 or aspect > 0.3, per the plan): **N of 5** — list which pairs count as the same shape.

Nearest bot policy per tester (compact / spike / cheapest / balanced / none): T1 __ T2 __ T3 __ T4 __ T5 __ C1 __

## 4. Scenario B — mid-game snapshot (from telemetry)

| Tester | Held @ start / end | Front @ start / end | First amber / red | Blocks lost | Retakes | Reorders (and what moved to the back) | Assemblers bought | Claim-to-enclose visible? |
|---|---|---|---|---|---|---|---|---|
| T1 | | | | | | | | |
| T2 | | | | | | | | |
| T3 | | | | | | | | |
| T4 | | | | | | | | |
| T5 | | | | | | | | |

"Claim-to-enclose visible" = the tester made a claim whose tooltip showed `closes ≥ 1` while a pip was amber or red, without prompting.

## 5. Criteria (from PROTOTYPE_TEST_PLAN.md)

| Criterion | Threshold | Result | Met |
|---|---|---|---|
| Distinct territory shapes at 2.5 h (Scenario A) | ≥ 3 of 5 | | ☐ |
| Unprompted remark about shape | ≥ 1 tester | | ☐ |
| Ring reordered (Scenario B) | ≥ 2 of 5 | | ☐ |
| Read the greyed build button and changed behaviour | (observation) | | ☐ |
| Control (economy=0) shape differed from economy-on shapes | (observation) | | ☐ |

**Rework trigger:** shapes converge on one policy — which: ______
**Kill trigger:** nobody talks about shape and nobody reorders — ☐

## 6. What testers said (verbatim, unprompted, with sim time)

Shape:
-

Pips / ammo / the ring:
-

Slots / "no free interior slot":
-

Rubble running dry:
-

Skyline silhouettes / survivors ("We're in."):
-

The bloom pulses / whether the front read as a threat:
-

Boredom, waiting, "what do I do now":
-

## 7. Post-session answers (five open questions from the plan, per tester)

| Q | T1 | T2 | T3 | T4 | T5 |
|---|---|---|---|---|---|
| 1 | | | | | |
| 2 | | | | | |
| 3 | | | | | |
| 4 | | | | | |
| 5 | | | | | |

## 8. Design constants the test informs

| Constant | Doc value | What the bots say (PHASE_2_REPORT.md) | What the sessions suggest | Decision | Owner |
|---|---|---|---|---|---|
| Edges fed per assembler (C1, §12 rate) | ~15 (Mk1 10, Mk2 20) | one Mk1 feeds a 16-block blob to 2:30 without a pip; compact never ambers (D-P2-2) | | | human |
| Claim cadence humans actually play at (C2) | 5 min (§18) | bots 15 min in hour one, then 5 | | | — |
| Start ammo (C10) | 20 magazines | 200 rounds shows ▲/✕ for 30 s at 0:00; 300 does not (D-P2-1) | | | human |
| Slot count per block (C3) | 1 (proto) | enclosure at 1:00 is the only way to a second assembler | | | human |
| Buffer cap (C4) | 400 magazines | stock sits at the cap from 1:15 in A; drains in ~10 min in B | | | human |
| Bloom rhythm (C8) | `120/(0.5+d)`, 10 % drop | not varied in the proto (E10 is Python-only) | | | human |
| Survivors: "we're in" on Held (§8) | Held | Electricians ~1:05 at bot pace, Concrete crew 1:45–2:20 | | | human |

## 9. Bugs, confusion, UI

| Sim time | Tester | What happened | Bug / confusion / feature request |
|---|---|---|---|
| | | | |

## 10. Decisions for Phase 3

Three things a human must decide before Phase 3 locks constants and the slice encodes them, with the decision written here (the three open ones from `PHASE_2_REPORT.md` are D-P2-1 start rounds, D-P2-2 edges per assembler, D-P2-3 the steel wall):

1.
2.
3.

## 11. Attachments

- `test_results/T1_A.json` …
- Observer notes: `test_results/notes.md`
- Screenshots of final territory per tester at 2:30 (A) and 4:30 (B): `test_results/shapes/`
- Saved states from the **Save snapshot** button for any bug: `test_results/states/`
