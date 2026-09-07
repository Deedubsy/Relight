# P5-05 — factory harness and integration evidence

Date: 2026-09-07. Authorised by “Ok onto P5-05” (D-EX-26), under the reconciled [Phase 5 scope](PHASE_5_SCOPE_REPORT.md). Branch `codex/tram-expansion`, parent `184908158db1c328b2a01d617a551954a0b3c99e`, plus the uncommitted EX-07 and P5-01–04 work. The source/build manifest identifies the tested working tree; the parent commit alone does not.

## Implementation

The [blueprint reader and command driver](../packages/harness/src/blueprint.ts) validate versioned JSON before issuing commands: bounded integer coordinates, unique identifiers, kind-specific settings, rectangular occupancy and unambiguous paired underground endpoints. The [example factory line](../packages/harness/blueprints/factory-line.json) feeds an assembler from mixed storage and routes output through a belt, underground pair and priority splitter to storage. The four experiment roles are validated before construction. General blueprint construction remains a harness facility, not a player clipboard or unlock.

The engineer walks to a clear standing position, pays ordinary construction costs and applies recipes/settings through normal commands. If a later action fails, the error identifies its entity and the completed paid placements remain; there is no free rollback. Resource layouts use actual footprints and source queries. Finite rubble at a seeded source margin is hand-mined and surplus is stored at home, with no terrain deletion or inventory injection during the run.

[Campaign workloads](../packages/harness/src/campaignExperiments.ts) have their own [CLI](../packages/harness/src/factoryCli.ts), registry and `docs/experiments/campaign` outputs. Run:

```sh
npm run factory -- --seeds 3,4,5 --hours 5
```

Optional `--only E-chain,E-coal,E-tram,E-logistics`, `--blueprint PATH` and `--out PATH/campaign` select a workload, validated plan and isolated output directory. `--hours` is simulation time, greater than zero and at most five; shorter runs are diagnostics. Every result retains its declared initial state, final state, ordinary command log, checks, configuration profile, blueprint hash and per-source SHA-256 hashes. Campaign configuration provenance now includes the existing truck defaults; legacy hashes are unchanged.

## Scenario declarations

- **E-chain:** fresh ordinary campaign stock, active threats, no stock or position injection. The same paid line changes between Wire, Board, Frame and Shot. Board uses actual wire recovered from the preceding run. Each measurement follows ten seconds of warmup and covers sixty simulation seconds; production and output-storage arrivals are separate.
- **E-coal / E-tram / E-logistics:** explicitly assisted fixtures add 1,200 steel, 600 copper and 500 coal to home before opening the ledger; minor/major attacks are deferred and ruin guards removed. Later restoration, kit collection, construction, transfers and recovery use ordinary commands. Seeds are 3, 4 and 5, campaign `exploration-v2`, speed 1, current code defaults. This assistance is not campaign balance or survival evidence.
- **Five-hour soak:** after paid setup, regional steel/copper/coal output supplies the assembler through scripted hand trips. Magazines travel from the home stop to two receiving stops; coal travels back. Scripted collection/refuelling is explicit. The engineer fires delivered magazines toward an empty point through ordinary aim commands to sustain demand; ammunition expenditure remains in the ledger. This does not demonstrate automated defence or an unattended factory. Source and pocket buffers impose real backpressure.

The soak checks conservation after each service cycle, actual new production at every hourly checkpoint, save continuation against the live state, replay of every checkpoint interval and full replay from the declared initial state. Service walks can finish just beyond the requested endpoint; actual elapsed simulation seconds are reported. Setup time is excluded from the declared five hours and remains in the full replay.

## Measured results

| Experiment | Seed 3 | Seed 4 | Seed 5 |
|---|---|---|---|
| E-chain | 7/7 checks | 7/7 checks | 7/7 checks |
| E-coal | 5/5 checks | 5/5 checks | 5/5 checks |
| E-tram | 5/5 checks | 5/5 checks | 5/5 checks |
| E-logistics | 9/9 checks | 9/9 checks | 9/9 checks |

All twelve results are in [the campaign evidence directory](experiments/campaign); their checks include conservation, live-versus-reloaded continuation and full initial-state command replay.

| Recipe | Code nominal/min | Output-arm ceiling/min | Completed/min, seeds 3 / 4 / 5 | Arrived/min, seeds 3 / 4 / 5 |
|---|---|---|---|---|
| wire | 120 | 60 | 60 / 60 / 60 | 60 / 60 / 60 |
| board | 15 | 60 | 14 / 14 / 14 | 13 / 13 / 13 |
| frame | 30 | 60 | 30 / 30 / 30 | 30 / 30 / 30 |
| shot | 10 | 60 | 4 / 4 / 4 | 4 / 4 / 4 |

This finite-stock run does not claim that every recipe reaches nominal capacity. The single output inserter limits Wire to 60/minute. Shot receives only seven steel after the earlier recipes, so its four/minute window includes input starvation; it is not the assembler's maximum Shot rate. Board's batch/input timing also affects arrivals. The regional-supply soak separately sustains approximately ten magazines/minute.

| Seed | Soak seconds after setup | Machines | Magazines made (total) | Freight items moved (total) | Largest unexplained item error |
|---|---|---|---|---|---|
| 3 | 18000.00 | 79 | 2998 | 5497.00 | 8.86757e-11 |
| 4 | 18000.00 | 109 | 2988 | 4742.00 | 5.77529e-11 |
| 5 | 18057.00 | 94 | 3007 | 5339.00 | 5.45697e-11 |

Each seed has five hourly checkpoints with new production, conserved items, matching interval replay and matching save continuation. Totals include setup activity; the reported duration starts after setup. Fractional freight counters reflect existing item accounting and are reported without rounding them into invented whole-item claims.

E-coal exhausts the finite home coal tile at whole-item mining granularity, then verifies that isolated regional extraction stops without generator fuel and restarts using produced coal. E-tram verifies destination demand, source reserves, copper return freight, retained in-flight cargo on a removed track segment and delivery after paid repair.

## Verification and rendering measurements

Final [232/232 tests](evidence/p5-05-2026-09-07/suite-complete.log), [all-package typecheck/build](evidence/p5-05-2026-09-07/typecheck-complete.log), [lint](evidence/p5-05-2026-09-07/lint-final.log), [unchanged legacy snapshot](evidence/p5-05-2026-09-07/snapshot.log), [both documentation profiles](evidence/p5-05-2026-09-07/docsync.log), [freshness](evidence/p5-05-2026-09-07/freshness.log) and [evidence/reference/archive checks](evidence/p5-05-2026-09-07/consistency.log) passed. The existing Vite bundle-size advisory remains; it is not a runtime FPS result.

The renderer adds read-only aggregate timing counters around actual light-mask checks and changed texture paints. [Chrome measurements](evidence/p5-05-2026-09-07/performance-result.json) use live fresh and five-hour assisted seed-3 saves, real resizes at 1366/900/1920 × 900 and M map/world return. Five-second RAF samples follow warmup. Changed-mask samples toggle the existing rendering-only hand-lamp preview every 400 ms; this is labelled diagnostic rendering, not a gameplay light unlock. No page errors or horizontal overflow were observed.

| Workload / width | Frame mean ms | Frame p95 ms | Light check mean ms | Changed paints | Paint mean ms |
|---|---|---|---|---|---|
| fresh / 1366 | 16.67 | 16.80 | 0.745 | 0 | 0.000 |
| fresh / 900 | 16.67 | 16.80 | 0.658 | 0 | 0.000 |
| fresh / 1920 | 16.67 | 16.80 | 0.629 | 0 | 0.000 |
| fresh-changed-light-mask / 1920 | 17.01 | 16.80 | 9.081 | 13 | 23.892 |
| five-hour-assisted / 1366 | 16.67 | 16.80 | 0.984 | 0 | 0.000 |
| five-hour-assisted / 900 | 16.67 | 16.80 | 1.041 | 0 | 0.000 |
| five-hour-assisted / 1920 | 16.67 | 16.80 | 1.014 | 0 | 0.000 |
| five-hour-assisted-changed-light-mask / 1920 | 17.13 | 16.80 | 9.554 | 13 | 23.731 |

Host: 11th Gen Intel(R) Core(TM) i7-11700K @ 3.60GHz, 63.8 GiB, Chrome 153.0.8010.27, headless. WebGL renderer: `{'vendor': 'Google Inc. (NVIDIA)', 'renderer': 'ANGLE (NVIDIA, NVIDIA GeForce RTX 3070 (0x00002488) Direct3D11 vs_5_0 ps_5_0, D3D11)'}`.

**Measured rendering concern:** changed light-mask paints averaged about 24 ms, beyond a 16.7 ms frame budget. The changed-mask samples included maximum frame intervals of 116.8 ms (fresh) and 150.1 ms (assisted); the full counters also retain startup paint costs. Steady-state samples at approximately 60 RAF callbacks/second therefore do not establish hitch-free rendering. This concern carries into EX-09B.

**Remaining performance gap:** no owner-designated 2019 reference laptop or controlled visible GPU/display measurement was available. Headless RAF measures browser scheduling, not monitor presentation; local CPU/light timings do not pass the reference-machine 60 FPS gate or human readability. This gap is retained for EX-09B assessment and subsequent reference-machine validation. No legacy or Phase 11 scale target is claimed passed.

[check.py](evidence/p5-05-2026-09-07/check.py) verifies source/build and experiment hashes, all checks, references/statuses and frozen EX-08A archives. The report records P5-05 implementation and automated integration completion with the permitted named performance gap; human approval/play remain unobserved.


## Retained obligations and limits

| Obligation | Technical evidence / remaining boundary |
|---|---|
| T13 construction; 1.3/1.10/1.16/1.17/8.5 | [P5-01](P5_01_CONSTRUCTION_REPORT.md): commands, paid reach-safe paths, history, catalogue/bindings/strings. D-SA-2 human sign-off remains outstanding. |
| T14 routing; 1.4/2.3/2.5 | [P5-02](P5_02_ROUTING_REPORT.md) plus current E-chain: one lane, pairs, filters, priority/fallback, saved buffers and four available recipes. Later recipes/unlocks remain later. |
| T15 information; 2.6/2.7/2.9/3.1 | [P5-03](P5_03_INSPECTION_REPORT.md): physical circuits, contents, settings, nominal/measured rates and saved item counters. Final overlay art/graphs remain Phase 12. |
| T16 transport; 2.11/7.4/A.9 | [P5-04](P5_04_TRANSPORT_REPORT.md) plus E-tram/soak: physical truck/cargo, selected routes, demands/reserves, onward/return freight and repaired interruption. No signals or autonomous Line truck. |
| T17 integration | Validated paid blueprint, campaign experiments, five-hour workload, source/config provenance, save/replay/conservation and full code/profile checks. Legacy fixture/snapshot/tooling preservation replaces the obsolete deletion list. Local rendering measurements retain a reference-machine performance gap. |
| RI-09 / phase readiness | Evidence feeds EX-09B; that readiness review remains the next task. No RI-09 or Phase 5 completion is implied here. |
| T18 / EX-08H and human standards | Fresh EX-08B build, two-assembler construction exercise, representative-loop observation, D-SA-2/C3/D-B2-2 positions and owner phase verdict remain required. No human task passed or waived. Q07 remains open for dependent later progression. |

Initial diagnostic failures are retained in the evidence directory: an exact-equality coal assertion ignored whole-item mining granularity; the builder could obstruct its own approach; short-run thresholds incorrectly used long-run quantities; and seed 4 required actual rubble clearance plus the depot's ordinary transfer command. A subsequent long run filled its pockets with 1,200 mined stone; the timed setup now stores that real surplus at home. The new regression first used an incomplete setup that did not require mining, then was corrected to exercise the actual seed-4 logistics layout. The harness and measurement assertions were corrected without changing gameplay rates, terrain or conservation expectations. The earlier failed logs are not final acceptance evidence.

The 232-test full suite passed after the workload corrections. The final additional duplicate-track-under-tram validation guard then passed the four focused harness tests, including the complete seed-4 short logistics regression; all-package types/build and lint were rerun on that final source. Experiments were regenerated afterward to retain exact source provenance.

Port 5176 serves the current local production build. The earlier EX-08A source/build archives and port 5175 remain preserved; EX-08B must prepare the refreshed human-test build. No commit, merge or push was performed.
