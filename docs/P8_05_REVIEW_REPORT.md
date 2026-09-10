# P8-05 — construction integration and engineering review

2026-09-08 · `codex/tram-expansion` · parent HEAD `8e54ed602da159905b0428f316f6e75cb25ae351`. Authority: owner request “Go ahead with P8-05”, [scope acceptance](PHASE_8_SCOPE_REPORT.md) and D-EX-44. **Phase 8 implementation and engineering review are complete. EX-08D preparation is next.** No commit, merge or push. Human experience and phase verdicts remain outstanding.

## Acceptance review

| Requirement | Evidence and conclusion |
| --- | --- |
| Found clipboard, settings and paid construction | Fresh focused/full regressions cover Foreman gating, rectangular copy/rotation/mirroring, underground roles, splitter priorities, filters, recipes and freight; atomic price/reach/occupancy refusal and saved history. Six integrated runs walk to the actual recruit and copy their paid source layout. |
| Library, imports and independent ghosts | Every seeded run saves, exports/imports and transforms its real layout before queuing a second independent order. Focused checks cover malformed/oversized/locked imports, folder metadata, overlap, missing stock, partial builds, cancellation and stable IDs. Ghosts remain inert. |
| Useful powered production/defence | Seeds 3/4/5 and holdouts 8/11/13 each build two six-machine ammunition cells: input chest → powered inserter/assembler → output inserter → belt → turret. Each turret receives 20 rounds from actual manufacturing and belt delivery. The station circuit is powered with an ordinary coal-fed generator. This freshly exercises direct belt-to-turret delivery; it does not close the owner's unreproduced P6-AMMO report. |
| Physical source/truck integration | The ordinary restored second-area station grants the truck. A nearby real chest is stocked through local pocket transfers, then truck construction pays exactly 68 steel + 27 copper for the second cell and leaves engineer pockets unchanged during the job. The source is hand-stocked; this is not a claim that a tram automatically stocked it. Current transport regressions separately cover real freight, cargo and manual truck use. |
| Supply pattern and live threats | Seed 3 additionally pays for a two-chest/inserter/belt/two-wall cell (26 steel + 1 copper). It delivers three steel through its powered transport and retains two 120-HP walls. The truck works for seven sampled seconds alongside the naturally scheduled first home raid. Threat scheduling and rosters are unchanged. The walls receive no damage in this sample; this is construction/coexistence evidence, not improved defence. |
| Alternate orders and broader geometry | Odd seeds recruit Foreman before station restoration and build input-first; even seeds recruit Electricians, restore the station, recruit Foreman and reverse manual/copied construction order so turrets go down first. Both source/copy and executor work. The declared 16-seed sweep below covers rotated placement, current street routing and valid/blocked turns. |
| Interruption and capacity | Fresh truck/transport tests include paid route obstructions and removal, source depletion/refill/removal/reassignment, occupied or replaced destinations, full unrelated cargo, paused/cancelled orders, retry idempotence, manual boarding and resumed jobs. Routes remain swept and bounded; failures retain cargo and completed parts. |
| Conserved removal and undo | Fresh area/history checks exercise loaded belts/tunnels, loose turret rounds and buffer limits, fractional stock, busy machines, damaged defences, installed facilities, tram/track order, settings, stale selections and cumulative pocket capacity. Valid groups pack atomically; undo retains recovered contents in pockets and uses current reach/stock. |
| Save, migration and replay | Every integrated run has a saved live-job continuation, final continuation and complete opening-command replay. Actual older-checkpoint migration and malformed/current save tests run in the focused/full suites; no old records are relabelled as new complete logs. Browser Ctrl+S/O and replay also pass after control actions and live edits. |
| Truthful UI and controls | Corrected two findings: compact truck text now shows active automatic phase instead of Parked; order missing-stock text names pockets so it is not mistaken for truck-source stock. Browser controls at 1366/900 widths cover selection/cancel/apply, pause/resume/source/retry, native input focus, keyboard cancellation, save and replay. No page errors or overflow. |

## Paid workload observations

[Scenario and regression source](../packages/sim/test/constructionIntegration.test.ts), [final integration log](evidence/p8-05-2026-09-08/integration-final.log), results: [3](evidence/p8-05-2026-09-08/seed-3.json), [4](evidence/p8-05-2026-09-08/seed-4.json), [5](evidence/p8-05-2026-09-08/seed-5.json), [8](evidence/p8-05-2026-09-08/seed-8.json), [11](evidence/p8-05-2026-09-08/seed-11.json), [13](evidence/p8-05-2026-09-08/seed-13.json).

| Seed | Recruitment / build order | Automatic ammo build | Turret rounds from each cell | Commands | Final simulation time |
| --- | --- | ---: | --- | ---: | ---: |
| 3 | Foreman → station / input first | 10 s | 20 / 20 | 47 | 315 s |
| 4 | Electricians → station → Foreman / turret first | 10 s | 20 / 20 | 39 | 125 s |
| 5 | Foreman → station / input first | 11 s | 20 / 20 | 37 | 111 s |
| 8 | Electricians → station → Foreman / turret first | 10 s | 20 / 20 | 39 | 144 s |
| 11 | Foreman → station / input first | 12 s | 20 / 20 | 37 | 131 s |
| 13 | Foreman → station / input first | 10 s | 20 / 20 | 37 | 108 s |

All source-chest, truck-cargo, pocket, machine-buffer, production and paid-sink ledgers pass, with matching final replay hashes. Construction consumes real carried stock; mining supplies the shortfall. No stock, position, discovery, raid-clock or enemy injection is used in these integrated runs. The scripted site search uses full map knowledge and ordinary walking; these timings are not self-directed discovery or human task times. Factory input filling, generator fuelling and source transfers are hand-serviced. Two magazines per turret establish working connected output, not sustained nominal throughput.

The preparation probe requested 68 steel for the second cell but had only 62 after its first build/station/source costs; the final policy mines the remaining steel normally and reserves production inputs. Some seeds can load and build from the truck's existing service position and need no travel; live save checks cover loading/building there, while seeds with travel and the separate sweep exercise movement. These were scenario assumptions, not gameplay bugs. The [initial stock probe](evidence/p8-05-2026-09-08/integration-probe.log) is retained.

The raid case is deliberately limited. The station stays operational while home is unattended. During browser continuation, that undefended home reaches **0/300 HP**, the raid withdraws, and construction data/cargo remain coherent. The scene also shows a power shortfall (85% service during the live sample). No survival, indefinite automation, balanced power margin or human defence verdict is claimed. EX-08D should provide a representative defended start and preserve the observation, not silently treat this engineering fixture as the recommended play strategy.

## Placement, route and performance sweep

[Machine-readable sweep](evidence/p8-05-2026-09-08/route-sweep.json): seeds 1–16 prepared with ordinary commands. Each probes eight fixed cardinal/diagonal destinations at offsets 16/24, four rotated ammunition layouts at each destination, and an 11×11 lattice with four truck orientations and one quarter-turn per fitting pose. All queries preserve gameplay hashes. Every returned route is checked segment-by-segment with the same swept footprint rule used during movement.

The 128 route queries return **85 routes and 43 blocked destinations**. There are **3052 valid / 454 blocked turns** and **64/512 geometrically placeable rotated layouts**. Route-query times: median 6.63 ms, p95 76.99 ms, maximum 94.95 ms. No route teleports or query mutations were found.

These are explicit samples, not proof that arbitrary queued footprints are reachable or that every street can turn a truck. Unreachable layouts correctly wait; existing live-obstruction regressions cover dynamic congestion. Query timing follows prepared geometry and is local headless execution, not a cold-start budget or reference-machine certification.

[Browser results](evidence/p8-05-2026-09-08/browser-result.json), [runner](evidence/p8-05-2026-09-08/browser.cjs). Screenshots were visually inspected: [desktop](evidence/p8-05-2026-09-08/live-tools-1366.png), [compact](evidence/p8-05-2026-09-08/live-tools-900.png). The browser reuses the paid P8-03 control checkpoint, then loads this review's [paid/mined ammunition and supply checkpoint](evidence/p8-05-2026-09-08/ammo-ready.json). It uses ordinary walking, nearby chest UI retrieval of delivered steel, and paid lamp/pole helpers. Native text copy/Escape and canvas clipboard/removal cancellation leave gameplay hashes unchanged. Both current-session replays match after live edits; 14 existing threat bodies remain during the measured samples (including site guards/withdrawing raiders).

| Width | Edits attempted/succeeded | Mean frame interval | p95 | Maximum | Mean light paint |
| --- | --- | --- | --- | --- | --- |
| 1366×900 | 24/24 | 16.67 ms | 16.80 ms | 16.80 ms | 15.72 ms |
| 900×900 | 24/24 | 16.67 ms | 16.80 ms | 17.10 ms | 17.69 ms |

Each sample advances six simulation seconds and repaints lighting 12 times, with construction panels open. These small-scene measurements do not close EX-08C's 266.8-ms or P7-04's 300.2-ms stalls. The full regression process was also running on this host. Reference-laptop/GPU certification, D-SA-2/C3/D-B2-2 and the Vite large-chunk advisory remain open.

## Verification and handoff

Fresh **320/320 full tests passed** in 519.28 seconds; **76/76 focused tests** passed in 47.58 seconds, plus **7/7 final integration checks** in 33.39 seconds after explicitly reversing the even-seed build order. That final test refinement changes no gameplay source; the complete suite already covers the final two production label fixes. All-package typechecks/build, lint, the unchanged legacy three-hour snapshot, both-profile docsync and freshness pass.

[Full suite](evidence/p8-05-2026-09-08/full-test.log), [focused suite](evidence/p8-05-2026-09-08/focused.log), [typechecks/build](evidence/p8-05-2026-09-08/typecheck.log), [lint](evidence/p8-05-2026-09-08/lint.log), [legacy snapshot](evidence/p8-05-2026-09-08/snapshot.log), [docsync](evidence/p8-05-2026-09-08/docsync.log), [freshness](evidence/p8-05-2026-09-08/freshness.log), [source/build manifest](evidence/p8-05-2026-09-08/manifest.json), [integrity/reference results](evidence/p8-05-2026-09-08/verification.json). Prior P8/EX-08C/EX-08B/P7 evidence is preserved. Campaign constants/fingerprint are unchanged; current campaign experiments remain historical P8-03 measurements, not newly claimed P8-05 soaks.

**EX-08D is next:** freeze the resulting post-P8 build and useful starts, prepare a new guide and blank record, and retain the direct conveyor-to-turret retest. Ports 5177 and 5179 remain frozen older checkpoints; port 5178 is the mutable review build. No human task is passed or waived. P8-H, P7-H, P6-H, EX-08H and remaining T18 stay blocked on EX-08D, then require the owner's observations/verdict. Q07/wider progression remains separate.
