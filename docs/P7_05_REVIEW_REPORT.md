# P7-05 — Discovery integration and engineering review

Date: 2026-09-08. Authority: adopted Q09 / D-EX-35 and owner request “Ok onto P7-05”. Branch `codex/tram-expansion`, base HEAD `17939145b5d40afe4dcfc1937a3ec6d34b763df6`. **P7-05 technical review is complete.** Gameplay source and rules are unchanged from P7-04. Phase 7 human approval and play observations remain outstanding. No commit, merge or push.

## Acceptance review

| Adopted requirement | Current evidence and conclusion |
|---|---|
| Stable discoveries and earned unlocks | Full regression checks cover local recruitment, once-only rewards, permanent plans, incapacitated/vehicle rejection, corrupted records and old electrical unlocks. The current metadata-9 model retains the independent workshop cache/guardian. |
| Reachable essential and optional rewards | New [64-seed sweep](evidence/p7-05-2026-09-08/sweep.log), seeds 1–64: all four shelters and the Turbine have paths, shelter footprints remain reserved, the hall/shelters do not overlap the surveyed tram routes, and fresh knowledge/save checks pass. This is the declared P7 sweep, not the later Phase 9 city-scale obligation. |
| Paid Concrete chain | All twelve combined runs mine ordinary home stone, feed a paid powered Mixer line, produce **56 concrete from 112 stone**, transport it, spend 40 on the Turbine and 16 on four Barricades. Full conservation and normal-start replay pass. |
| Useful Turbine and optional equipment | All runs pay for the commissioning Generator, restore the Turbine, build an Arc lamp, Big pole and Floodlight, and recruit Surveyors. The [service audit](evidence/p7-05-2026-09-08/services.log) confirms 12/600 kW Turbine output for the Arc load, lit radius-6 Arc/range-12 Floodlight, a Big pole in a 300-kW circuit, real district-type survey and earned recipe/journal state. |
| Connected power, disabling and recovery | Fresh full tests include the P7-03 physical wiring/coal-offset/isolation/switch checks and disabled-core suppression. Their extra finite wiring stock and suppressed raid clock remain labelled isolated fixtures. Ordinary acquisition is established separately by the twelve live runs. No new base is registered by a recruit or Turbine. |
| Barricade defence and repair | Combined runs buy/place four Barricades and retain one operational home through normal raids. Those particular Barricades receive **no damage**; this does not prove a better defensive layout. Fresh `concrete.test.ts` tests separately cover actual crawler breach, disabled passability, paid repair/save, construction history and packing conservation, using explicitly labelled combat stock/damage fixtures. |
| Alternate orders and live defence | Seeds **3/4/5 and holdouts 8/11/13**, each electrical-first and survey-first, complete with no changed raid schedule, debug stock, teleport or direct state edits. Two supplied turret pods are prepared first. The second normal raid is observed in every final segment; full-run ammunition use and core damage include earlier activity. |
| Existing station/radio/workshop/transport capabilities | New seed-3 supplied-network defence run passes **13/13** checks: ordinary mining/construction, station/radio network, earned three-base resupply, unattended remote minor defence, finite major assault, interrupted freight, exhausted outpost and paid recovery. This is a fresh current-code run, not a relabelled P6 result. Existing workshop/cache and truck regression coverage is retained and freshly executed in the full suite. |
| Old/new saves and replay | Actual retained saves from metadata **5, 7, 8 and 9** preserve machines, defence, cache and conservation, then round-trip and continue identically. The full suite also covers earlier-schema fixtures. Every combined run and the network workload verifies replay from the ordinary start; upgraded historical logs are not falsely advertised as fresh current-code logs. |
| Truthful information and usable controls | Read-only service audit plus fresh browser checks at 1366×900 and 900×900 exercise nine items, lock/provenance changes, local recruitment, known map, Turbine switches and Ctrl+S/O/full replay. No page error or horizontal overflow. Foreman/clipboard remain unavailable until Phase 8; hidden rewards and future attack targets stay excluded. |

## Combined workload observations

[Script](evidence/p7-05-2026-09-08/ordinary.ts), [explicit seed matrix](evidence/p7-05-2026-09-08/runs.sh), [twelve results](evidence/p7-05-2026-09-08/ordinary-final.log). Each result has a complete `seedN-forward.json` or `seedN-reverse.json` save/log in the same evidence directory. The preparation policy uses ordinary starting supplies plus mined home steel/stone, powered chest/inserter/belt production and carried deliveries. It does not use the player's unimplemented blueprint clipboard.

- Reward/equipment setup finishes at **355–408 simulation seconds**, with **58–60 commands** by the end of each 651-second run. Scripted coordinates and full map knowledge make these engineering workloads, not unaided discovery timing.
- Home remains operational at **256.4–296 / 300 HP**. Turrets spend **51–69 rounds**. These are surviving prepared runs, not no-loss or indefinitely autonomous factories.
- A preliminary reward-first route with one late turret let the normal raids disable home. The revised route prepares two supplied pods before leaving. This is a policy finding; no enemies, costs, output or unlocks were retuned.
- Two preliminary scripts targeted a fixed gate stand obstructed by their own factory. Final placement uses the ordinary obstacle-aware approach helper and available home tiles. The final Barricades are not claimed to have blocked a raid. Logs retain both failures.
- The first matrix launcher dropped its forwarded seed/order arguments and repeated seed 3; that output is retained in `launcher-initial.log`/`ordinary.log` and excluded from the twelve-run evidence. The final saved launcher spells out each argument. The initial light audit used a single-tile coordinate for the 2×2 Floodlight; the corrected audit uses its defined half-tile centre. No gameplay change was needed.

The [network result](evidence/p7-05-2026-09-08/campaign/E-defence-turrets-seed3.json) and [run log](evidence/p7-05-2026-09-08/network-defence.log) retain all checks, state, command log and source/config provenance. It reaches the major-completed checkpoint at 3545 seconds and finishes recovery at 5633 seconds. Its explicit ammunition-exhaustion drill is not a claim that cutting one track instantly empties stocked turrets. It does not reach a second major assault.

## Verification and performance

Fresh **271/271 tests passed** in 538.5 seconds. All-package typechecks/production build, lint, unchanged legacy three-hour snapshot, both-profile docsync and freshness checks passed. Source/archive checks verify all **181 P7-04 source inputs** and the same seven production files, including its ordinary guide checkpoint. No production source change or tuning was necessary for this review.

[Browser record](evidence/p7-05-2026-09-08/browser.json) and [script](evidence/p7-05-2026-09-08/browser.cjs) repeat ordinary controls/save/replay at both widths. Screenshots were inspected. An additional [settled switch check](evidence/p7-05-2026-09-08/visual.log) waits for the journal update after each off/on command before capturing both widths; the earlier immediate screenshot can show the preceding panel refresh. Four five-second live samples keep the guide open while issuing paid lamp/pole edits: **80/80 edits succeed** and each sample advances five simulation seconds.

| Start / width | Mean frame interval | p95 | Maximum frame interval | Mean light paint |
|---|---:|---:|---:|---:|
| Fresh / 1366 | 16.67 ms | 16.8 ms | 16.9 ms | 12.06 ms |
| Powered / 1366 | 16.67 ms | 16.8 ms | 17.0 ms | 13.46 ms |
| Fresh / 900 | 16.67 ms | 16.8 ms | 16.9 ms | 12.81 ms |
| Powered / 900 | 17.07 ms | 16.8 ms | **133.5 ms** | 26.08 ms |

The compact powered sample also reaches **132.2 ms light paint**. Earlier P7-04's 300.2-ms frame interval and larger-workload stalls remain open: an unchanged-source rerun with a smaller observed maximum is not a performance fix. These are headless Chrome observations on this host, with other harness work running during part of the browser session, not reference-laptop/GPU certification. Vite's large-chunk advice remains.

Required records: [tests](evidence/p7-05-2026-09-08/tests.log), [build/typechecks](evidence/p7-05-2026-09-08/build.log), [lint](evidence/p7-05-2026-09-08/lint.log), [legacy snapshot](evidence/p7-05-2026-09-08/snapshot.log), [docsync](evidence/p7-05-2026-09-08/docsync-check.log), [freshness](evidence/p7-05-2026-09-08/freshness.log), [manifest](evidence/p7-05-2026-09-08/manifest.json) and [integrity/reference check](evidence/p7-05-2026-09-08/consistency.log). P7-04 and earlier reports/evidence, and the retained EX-08B/port-5177 checkpoint, are preserved. Historical five-hour factory experiments remain source-stamped historical measurements; they were not regenerated for this review. Freshness validates configuration/ancestry, not every source change.

## Handoff

**EX-08C is the next runnable task:** freeze the post-P7 shared build and useful starts, refresh the playtest guide and blank observation record, and carry the original direct-conveyor ammo retest forward. Do not use the retained port-5177 Phase 6 checkpoint as the new P7 build.

Phase 7 implementation and automated engineering review are complete. **P7-H, EX-08H, P6-H and remaining T18 observations stay blocked on EX-08C**, then require actual human play/verdict. Self-directed discovery, useful reward choices, restoration/recipe comprehension, preparation repetition, core damage and performance remain observations for that session. No human gate is passed or waived. Q07/wider progression and the Foreman/Phase 8 boundary remain unchanged.
