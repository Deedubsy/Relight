# Phase 5 — implementation readiness review

2026-09-07. EX-09B, authorised by “Ok continue” after the P5-05 handoff (D-EX-27). Reviewed `codex/tram-expansion`, parent `184908158db1c328b2a01d617a551954a0b3c99e`, plus the existing uncommitted work. [PROGRESS](PROGRESS.md) owns status.

## Assessment

**Ready to proceed to EX-08B preparation for an unscored diagnostic gameplay session, with known limitations.** The reconciled factory implementation and technical evidence meet the P5-01–05 acceptance in [the scope report](PHASE_5_SCOPE_REPORT.md). EX-09B and the retained RI-09 technical review are complete. The gameplay-test build still needs to be refreshed and checked in EX-08B before the human session.

This is an engineering readiness assessment, not owner approval of performance, balance, accessibility or Phase 5 completion. No human task is passed or waived. No gameplay, renderer, tuning, save, generated experiment, production build or earlier evidence was changed by this review.

## Acceptance review

| Increment / retained milestone | Evidence reviewed and conclusion |
|---|---|
| P5-01 / T13 — construction and controls | [Construction report](P5_01_CONSTRUCTION_REPORT.md): normal command dispatch, atomic paid drag paths, current-inventory/reach undo/redo, saved history, catalogue and shared bindings/strings. Final-tick paused replay is covered. Actual wide/narrow browser controls used ordinary home supplies. Technical acceptance met; provisional limits and D-SA-2 human sign-off retained. |
| P5-02 / T14 — routing and storage | [Routing report](P5_02_ROUTING_REPORT.md): paired undergrounds, rectangular splitters, priority/fallback, inserter filters, blocked-buffer conservation and saved settings. All four available recipes reach storage; mixed-belt and station-arrivals sources have focused coverage. Technical acceptance met. The single-assembler harness does not substitute for the human two-assembler belted-input exercise. |
| P5-03 / T15 — inspection, circuits and statistics | [Inspection report](P5_03_INSPECTION_REPORT.md): identity-based read-only inspection, local circuit numbers, ordinary settings commands, nominal versus measured production and bounded saved observations. Isolated/browned-out/empty/blocked cases and actual controls are covered. Technical acceptance met; fixture assistance, later graphs/art and human readability remain separate. |
| P5-04 / T16 — physical transport | [Transport report](P5_04_TRANSPORT_REPORT.md): truck at the restored second-area station under Q08, physical parking/boarding/exit/driving, separate conserved cargo and saved occupancy. Actual selected-stop routes reuse demand/reservation/return-freight rules. Paid expedition and browser save/replay evidence reviewed. Technical acceptance met; no autonomous Line truck or vehicle fuel system is implied. |
| P5-05 / T17 — integration | [Integration report](P5_05_INTEGRATION_REPORT.md): validated JSON blueprint, ordinary paid command construction, campaign E-chain/E-coal/E-tram, three five-hour E-logistics runs, periodic and full replay, save continuation and conservation. All twelve experiment records pass. Technical acceptance met under its explicit allowance for named performance gaps; the rendering concern below remains unresolved. |

Existing physical recipes, extraction, storage, accounting, local power, restoration, freight and city/Heart improvements are credited rather than rebuilt. Legacy tooling, fixtures, snapshots and historical acceptance records remain intact. The old T17 deletion list and territory/timing objectives are superseded by the approved profile separation and reconciled scope, not silently completed.

## Findings that carry forward

**Rendering:** P5-05's local headless Chrome samples show changed light-mask paints averaging about **24 ms**, with maximum frame intervals of **116.8 ms** in the fresh sample and **150.1 ms** in the assisted sample. Steady samples were near 60 RAF callbacks/second and resize/map-return checks passed without page errors or horizontal overflow. These results do not demonstrate hitch-free rendering or monitor presentation.

Source inspection confirms that `refreshLight` skips unchanged masks, while `paintLight` creates image data, blurs and converts the full map, then refreshes the texture. The changed-mask experiment toggles the existing rendering-only hand-lamp preview; that preview is off in the ordinary start. Normal lighting changes can still invoke the same repaint path. The current evidence does not measure the frequency or practical impact of those stalls during a normal expedition. No optimization or new performance claim is made here.

This is a known issue for the diagnostic session, not a functional blocker to preparing it: steady rendering and actual factory/transport controls have passed, and no save, conservation or command failure remains in the final evidence. EX-08B must retain the warning and exercise normal light/power changes on the refreshed build, recording hardware, visible input impact and any lost interaction. If stalls prevent meaningful interaction, correct and reverify before using that build for human observations. Reference-laptop/GPU validation and broader scale acceptance remain outstanding; EX-10/Phase 11 performance obligations are not waived.

**Economy and assistance:** E-chain's finite ordinary stock supplies only seven steel to its final Shot segment, producing four magazines in the measured minute; this is input starvation, not maximum assembler capacity. The regional-supply soak sustains approximately ten magazines/minute, but adds declared opening stock, suppresses threats and uses scripted hand servicing plus a rifle ammunition sink. Its success proves the declared integration workload, not unattended automation, defence balance, progression pace or enjoyable play. EX-08B must use a fresh ordinary campaign start, not a soak save.

**Human acceptance:** EX-08H and T18 remain blocked on EX-08B. Retain the two-assembler exercise, actual magazines reaching a turret, elapsed time, prior experience, pauses and assistance. The existing protocol defaults to **unscored** unless a threshold is explicitly decided before play; the old ten-minute limit is not a new pass threshold. D-SA-2, C3, D-B2-2 and the owner's explicit Phase 5 verdict remain human decisions. The representative loop must actually observe exploration, restoration, freight, defence and recovery; no missing observation is inferred from bot results.

**Later scope:** faster belts, Assembler Mk2, later recipe/facility chains, the player blueprint clipboard, Foreman/Line truck automation, translations/rebinding UI and final presentation remain with their reconciled later work. Q07 stays open for dependent broader progression; it does not block refreshed factory test preparation. This review does not complete EX-09 or other later phases.

## Verification and handoff

The [baseline integrity check](evidence/phase5-readiness-2026-09-07/baseline-integrity.log) verified the P5-05 manifest before documentation changed. The [review manifest](evidence/phase5-readiness-2026-09-07/review-manifest.json) protects 352 source/build/evidence and session-template files. The review verifies all 166 current source/test/build inputs, six production files, twelve experiment artifacts and the earlier P5 verification artifacts against their recorded hashes; both EX-08A archives remain unchanged.

The retained required code results are P5-05's 232/232 full tests, four final-source harness regressions after the last validation-only change, all-package typecheck/build, lint and unchanged legacy snapshot. Those tests, builds, browser checks and five-hour simulations were **not rerun** for this documentation-only assessment. Their exact artifacts and source identities were checked instead. Earlier P5 browser runs retain their original build identity; EX-08B owns refreshed functional checks on the consolidated build.

Fresh [documentation-profile checks](evidence/phase5-readiness-2026-09-07/docsync.log), [evidence freshness](evidence/phase5-readiness-2026-09-07/freshness.log), [reference/status/protected-file checks](evidence/phase5-readiness-2026-09-07/consistency.log) and [diff whitespace validation](evidence/phase5-readiness-2026-09-07/diff-check.log) passed. [check.py](evidence/phase5-readiness-2026-09-07/check.py) checks the preserved evidence and current completion boundaries without rewriting historical manifests.

EX-08B is next: freeze the consolidated source/build and ordinary paused seed-3 start, rerun relevant controls/mechanics checks, carry the rendering finding into visible-machine preparation, and refresh the guide and blank human record. Preserve the EX-08A archive and port 5175; port 5176 remains the current local preview until refreshed preparation identifies its new build. The old session guide/record remain unchanged in this review. Changes remain uncommitted; no merge or push was performed.
