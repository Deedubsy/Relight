---
verdict: pending          # go | rework | kill | pending — Prompt B refuses to start unless this is `go`
config_hash: 6e74fbfd     # economy-on build the testers played
control_hash: 91bad3aa    # economy=0 control
test_plan_version:        # commit or date of PROTOTYPE_TEST_PLAN.md used
seed: 3
scenario_a: fresh start, 2.5 h sim at 4×
scenario_b: mid-game snapshot (compact bot at 3:00), 1.5 h sim at 4×
testers: 5 + 1 control
dates:
observer:
---

# Map-view prototype — test results

Gate for `relight-prompt-B-vertical-slice.md`. Fill every section; where something wasn't measured, write `not measured` rather than leaving it blank. Telemetry exports go in `test_results/` named `<tester>_<scenario>.json` and are the source for every number below.

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

| Constant | Doc value | What the sessions suggest | Decision | Owner |
|---|---|---|---|---|
| Edges fed per assembler (§12 rate) | ~15 (Mk1 10, Mk2 20) | | | human |
| Claim cadence humans actually play at | 5 min (§18) | | | — |
| HQ coal patch | ~700 | | | human |
| Slot count per block | 1 (proto) | | | human |
| Buffer cap | 400 magazines | | | human |

## 9. Bugs, confusion, UI

| Sim time | Tester | What happened | Bug / confusion / feature request |
|---|---|---|---|
| | | | |

## 10. Decisions for Prompt B

Three things a human must decide before the slice encodes them, with the decision written here:

1.
2.
3.

## 11. Attachments

- `test_results/T1_A.json` …
- Observer notes: `test_results/notes.md`
- Screenshots of final territory per tester at 150 min: `test_results/shapes/`
