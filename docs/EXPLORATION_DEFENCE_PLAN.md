# Relight development plan — Version 2

**Current sequencing (D-EX-34, 2026-09-07):** the owner moves shared playtesting after P7. [Phase 7 scope review](PHASE_7_SCOPE_REPORT.md) is complete; catalogue Q09 is adopted under D-EX-35. P7-01–05 implement saved recruits, the paid Mixer/Barricade chain, Turbine generation and optional Arc lighting/district surveying, plus discovery and recipe information; EX-08C playtest preparation is next. P7 implementation and EX-08C preparation precede EX-08H/P6-H/P7-H and remaining T18 observations. EX-08B is retained historical preparation, not the post-P7 build. Earlier sequencing statements below describe prior checkpoints; human criteria remain outstanding.

Adopted direction and opening contracts: 2026-09-06, D-EX-01–11. The owner confirmed that **Phase 4 has just finished**. Phase 5 began with the EX-09A scope audit on 2026-09-07 (D-EX-21); D-EX-20 now brings factory implementation before the human representative-loop test, with scope reconciliation first; forward-built RI features do not mean later phases are complete.

This plan replaces the executable direction of [Version 1](REVISED_DEVELOPMENT_PLAN.md). [RELIGHT-design.md](RELIGHT-design.md) defines gameplay, [DECISIONS.md](DECISIONS.md) records authority and open choices, and [PROGRESS.md](PROGRESS.md) alone owns task order/status. This plan supplies task definitions, not a second tracker. The owner subsequently authorised baseline integration and the first tram increment (D-EX-12); D-EX-13 authorises campaign separation and the home opening, followed by D-EX-14 for the second area; subsequent coding follows that scope and PROGRESS.

## 1. Direction and implementation boundaries

Explore freely, return with useful discoveries, restore productive city sites, distribute goods through multiple tram stations, and prepare defended bases for intermittent assaults. Begin at a house in a cul-de-sac, open tram access at the second area, and make radio restoration a useful next opportunity.

Two days without major assaults initially allow exploration; a later defined milestone may reduce that to one. Minor raids and site creatures continue. Scheduled and major restoration assaults share one non-overlapping major-target schedule. Station factories and specialised resource regions keep early settlements relevant.

The owner approved the full recommended opening package through D-EX-11. Q01–06 are settled: the 20-minute initial cycle and shared schedule, home/station bases, radio rules, multi-stop reserved freight, persistent regional extraction and the repair/discovery slice are in the active GDD. Alternatives were not selected. Q07 remains open for the later campaign. Missing costs, rates, HP/radii and detailed tuning still require explicit specification; they do not reopen approved core choices.

Preserve physical inventories and deliveries, the pure simulation, replayability, clear state-driven feedback and human play evidence. Reuse proven components. No new survival-needs, happiness, dialogue, XP or loot-tier simulation is in scope. D-EX-12 authorises integration and tram implementation. It does not authorise automatic campaign save conversion, regeneration of historical benchmarks or evidence deletion.

## 2. Actual baseline and reusable work

Phase 4 is complete (owner confirmation). EX-01 compared the earlier RI checkout with newer main and reproduced the relevant Heart/project/city checks. D-EX-12 then authorised integration: newer main is merged with the adopted documentation preserved. See BASELINE_INTEGRATION_REPORT.md for the comparison and TRAM_INTEGRATION_REPORT.md for the resulting build and first freight increment.

Retain movement, inventories, machine recipes/production, belts, power, light queries, project records, commissioning and save/replay foundations where suitable. Reuse main's urban structure/landmark work and corrected Heart implementation when integration is authorised. Existing tram and enemy code are starting components, not proof that network transport or scheduled base attacks exist.

## 3. Canonical task definitions

| Task | Deliverable and boundary |
|---|---|
| EX-00 — documentation migration | Current GDD, decisions, constitution, phase criteria, tracker, handoff and agent entry points aligned; old records preserved; verification limitations reported. No game code. |
| EX-01 — baseline and contract preparation | Read-only comparison of checkout and fetched main; reusable/changed/unbuilt inventory; existing failures separated from revision effects; concrete recommended answers and tradeoffs for D-EX-Q01–06. Establish the proposed integration and save/profile plan without applying a merge or inventing approval. |
| EX-02 — opening contracts | Record the owner's approval of Q01–06 through D-EX-11. The approved recommended package and initial settings are incorporated in the GDD; unspecified tuning and Q07 remain separate. A decision record is not play evidence. |
| EX-03 — implementation and evidence profiles | Following the selected baseline and contracts, define versioned state, compatibility and legacy/new experiment scopes. Update docsync/freshness routing as necessary before changing profile constants; keep old benchmark expectations for their old rules. Verify no mixed-profile save and deterministic clock state; the actual assault schedule is EX-05. |
| EX-04 — cul-de-sac and useful second area | Reuse city geometry; create the one-approach home and small factory space, early tram-access event and reachable radio project. Real costs, valid placement, reachable resources, no circular transport unlock. Guidance names current state and opportunity; it does not force all roaming. |
| EX-05 — three threat layers and radio | Implement site encounters, shared small raids and the major-assault schedule per contract, with single major target, rest interval, restoration-event arbitration and warning rules. Opening wall/turret/damage support and personal combat must be real before defence acceptance. |
| EX-06 — station supply network and district economy | Multiple stops, selected cargo, onward reservations, return freight and local factories using real inventories. Verify two complementary resource/production sites, route interruption/recovery and continued utility of the older site. Include a workshop's approved practical service. Wire the later one-quiet-cycle milestone only to a third commissioned station with demonstrated automated resupply; EX-05's two-base slice retains two cycles. |
| EX-07 — exploration discovery slice | One optional useful discovery and one distinctive encounter integrated into normal roaming, using the approved catalogue. Persistent reward identity, no duplication, fair clues and optional detours. Essential progression remains reliably reachable. |
| EX-08 — representative loop gate | Automated invariants, then the human session defined in EXPLORATION_DEFENCE_PLAYTEST.md. Record observations and revisions; do not claim fun from bot success. Complete the reconciled Phase 5 factory and refresh the prepared build before human play (D-EX-20). Broader content expansion follows the assessment. |
| EX-09 — wider progression and remaining reconciliation | Credit the factory work brought forward as EX-09A/B. After the representative-loop assessment, reconcile and deliver remaining wider projects, useful facilities/encounters, regional economy and endgame contract D-EX-Q07. Record actual Phase 5 human exit evidence separately from implementation readiness. |
| EX-10 — full campaign, performance and release | Remaining Phases 10–14 obligations, seed fairness, save migration, accessibility, long-run play, production art/audio, performance and release preparation; re-estimate after the new loop. Existing phase gates are not silently waived. |

EX-06A is the independently reviewable freight subset of EX-06 brought forward under D-EX-12: multiple stops, reserved deliveries, return exports and save/UI support on the retained baseline. It does not complete EX-03 profiles, EX-04 opening, or EX-06 specialised factories/workshops. PROGRESS owns its status.

EX-04A is the home-only subset of EX-04 authorised by D-EX-13: house inventory, one physical entrance, reachable starter patches, factory space and ordinary paid placement, with save/replay and goal support. Its static boundary is provisional geometry; destructible/player-built defences require EX-05. The subsequent EX-04 station/radio increment completes those opening capabilities; EX-04A alone does not. The wider defence loop remains EX-05–08.

The completed [EX-09A audit](PHASE_5_SCOPE_REPORT.md) defines P5-01–05 and the rewritten T13–18/RI-09 acceptance; PROGRESS owns their status. [P5-01 evidence](P5_01_CONSTRUCTION_REPORT.md) records the implemented construction/controls increment; [P5-02 evidence](P5_02_ROUTING_REPORT.md) records routing primitives and current-recipe chains; [P5-03 evidence](P5_03_INSPECTION_REPORT.md) records inspection, local circuit feedback and saved statistics; [P5-04 evidence](P5_04_TRANSPORT_REPORT.md) records the physical truck, saved cargo and selected-stop routes. [P5-05 evidence](P5_05_INTEGRATION_REPORT.md) records the paid blueprint harness, current campaign experiments and five-hour logistics on three seeds, retaining a reference-machine performance gap. [EX-09B readiness evidence](PHASE_5_READINESS_REPORT.md) completes the implementation and retained RI-09 technical review, permitting EX-08B refreshed preparation with light-map repaint and reference-machine gaps retained. Human exit remains separate. Under D-EX-20, **EX-09A** first audits retained factory obligations and current implementations, rewrites acceptance, and maps them to executable increments. **EX-09B** assesses the reconciled P5-01–05 implementation and verification evidence before human play. **EX-08B** refreshes the frozen build, checks, guide and blank record afterward. EX-08A remains valid evidence of its earlier checkpoint. EX-08H is deferred, not passed or waived; human construction observations and the phase verdict follow implementation. Q07 blocks only dependent broader progression.

The owner subsequently confirmed successful current-build factory, transport and defence/recovery testing and requested the **Phase 6 review** (D-EX-28). [PHASE_6_SCOPE_REPORT.md](PHASE_6_SCOPE_REPORT.md) now maps the independent current-loop threat completion work to P6-01–04 and P6-H, crediting EX-05–07. This review takes priority over the earlier preparation sequence above; the shared EX-08B build/guide refresh follows the planned threat changes. Earlier owner observations remain valid, while unreported formal criteria remain explicit. Q07 and EX-08 still govern dependent wider content; no RI-10 candidate is automatically adopted. PROGRESS alone owns the new order.

[P6-01 reliability evidence](P6_01_RELIABILITY_REPORT.md) subsequently completes the first threat increment under D-EX-29: same-approach staging, saved deferral, physical withdrawal and validated lifecycle/upgrade behaviour. [P6-02 information evidence](P6_02_INFORMATION_REPORT.md) completes range previews, known-threat navigation and immediate-action feedback under D-EX-30. [P6-03 supplied-network evidence](P6_03_NETWORK_DEFENCE_REPORT.md) is complete; [P6-04 engineering readiness](P6_04_READINESS_REPORT.md) is complete; [EX-08B shared preparation](EX08B_PREPARATION_REPORT.md) is complete and EX-08H/P6-H observations are ready (ammo retesting remains open under D-EX-32); remaining phase criteria are unchanged.

## 4. Migration of existing work

Completed T/RI tasks keep their evidence and original completion meaning. Unfinished old tasks retain their EX-09 reconciliation dependency until EX-09A explicitly remaps the factory subset; later obligations remain with EX-09. They are not marked done or automatically waived. Before they are made runnable their scope and acceptance must be revised against the GDD. EX tasks may absorb their useful components; the tracker must state the mapping and avoid duplicate implementation.

- RI-07 and T12c's opening/enclosure work maps to EX-04 and EX-08; the fixed enclosure target does not survive by default.
- T12b's robustness methods map to EX-03/08; old scenarios remain legacy tests.
- RI-08 and human T19 map to EX-08; their old goals do not pass the new human gate.
- RI-09 and T13–18 map to EX-04–06 for slice dependencies, then EX-09A/B for remaining factory implementation. T18 remains a human exercise after implementation, with starting conditions/acceptance reconciled before play. Existing work is not repeated.
- RI-10's regular enemy work maps to EX-05/07 and later EX-09, with bloom-specific mechanics reconsidered.
- RI-11's project, tools and city work maps to EX-04/06/07/09. Reuse the newer city implementation.
- RI-12's later bosses are candidates for EX-09, after the first revised loop; they do not block early tram access.
- RI-13's campaign/release scope maps to EX-09/10.

The approximately 40% estimate is the owner's rough estimate, not a recalculated plan. Phase 4 completion is the confirmed milestone. Do not reuse old duration estimates as delivery promises for this revised scope.

## 5. Evidence and acceptance

Automated checks should demonstrate:

- A single major target, protected rest days, coherent restoration queues, no duplicate post-load events and bounded minor-raid frequency as settlements increase.
- Valid hostile approaches and site creature behaviour; radio information consistent with actual sim state and unlocks.
- Real opening costs and achievable unlocks, including tram access without requiring tram delivery first.
- Item conservation through station transfers; explicit onward supply; understandable interruption/recovery; deterministic discovery rewards.
- Compatible saves or an explicit refusal to load unsupported profiles, with old saves never silently acquiring mixed rules.

The human gate records early payoff, self-directed exploration, station understanding, time to return and prepare, meaningful personal defence, old-factory usefulness and recurring maintenance burden. Numerical thresholds must be recorded before a scored test. Missing measurements stay missing.

No old evidence is rewritten to describe new gameplay. Baseline checks failing before the task remain separate from regressions. Documentation-only work requires document consistency, evidence freshness and referenced-path checks; environment blockers are reported rather than concealed.

## Changelog

- 2026-09-06 — Created as the successor to Version 1 following the owner's documentation request and confirmation that Phase 4 is complete.

- 2026-09-06 — D-EX-11: adopted the six opening contracts and initial settings; technical baseline work remains outstanding.
