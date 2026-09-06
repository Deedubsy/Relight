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

**Now (2026-09-07):** Phase 4 complete (owner, D-EX-10). Phase 5 has not started (owner correction, D-EX-15); EX-03–08 are intervening design-revision work. EX-01/02 and freight EX-06A are complete. D-EX-13 authorises campaign separation and the home opening. EX-03, EX-04A and EX-04 are complete. D-EX-14 authorised the station/radio increment; EX-05 is complete for the two-base slice; EX-06 is next for specialised supply, workshop service and the later resupply milestone. The new home preview is explicitly selected and keeps legacy saves and benchmarks separate.

The Version 2 plan is EXPLORATION_DEFENCE_PLAN.md. The EX rows below are the current execution order. The earlier rows remain historical/reuse records in their original order. The historical T10 "Open Phase 5" row records an earlier administrative read, not a current Phase 5 start; the owner explicitly confirmed Phase 5 unstarted in D-EX-15. Every unfinished legacy row now depends on EX-09, where its acceptance must be rewritten, absorbed with explicit mapping, or referred to the owner; this is not permission to execute its obsolete text unchanged once EX-09 finishes. Completed and waived rows retain their evidence.

| id | task | owner | status | blocked by | acceptance | evidence | done on |
|---|---|---|---|---|---|---|---|
| EX-00 | Documentation migration to Version 2 | agent | done | D-EX-09 | Current rules, plan, tracker and entry points agree; archived originals; checks and limitations recorded; no code/evidence mutation | docs/EXPLORATION_DEFENCE_MIGRATION_REPORT.md | 2026-09-06; editorial complete, automated validation partial |
| EX-01 | Baseline comparison and contract preparation | agent | done | — | Compared ancestry, retained/changed/unbuilt systems, failures, saves and runtime; concrete integration plan and adopted recommendations recorded | docs/BASELINE_INTEGRATION_REPORT.md; docs/EXPLORATION_DEFENCE_RECOMMENDATIONS.md | 2026-09-06 |
| EX-02 | Adopt opening contracts D-EX-Q01–06 | human | done | D-EX-11 (owner approval; technical EX-01 audit remains separate) | Recommended contracts and initial settings adopted; alternatives and unspecified tuning remain separate | docs/DECISIONS.md D-EX-11 and Q01–06; docs/EXPLORATION_DEFENCE_RECOMMENDATIONS.md | 2026-09-06, owner via "Yes, I like all of those recommendations. Let's go with those" |
| EX-06A | Integrate newer baseline and first selective station freight increment | agent | done | EX-01 done; EX-02 done; D-EX-12 | Preserve adopted docs and main fixes; three-stop delivery, export reserves, return freight, conservation, interruption/save recovery and visible command controls; actual checks recorded | docs/TRAM_INTEGRATION_REPORT.md | 2026-09-06 |
| EX-03 | Version 2 implementation/evidence profiles and save compatibility | agent | done | EX-01 done; EX-02 done; D-EX-13 | Integrated baseline; legacy/new campaign rules and tooling separated before new constants; save/replay compatibility checked | docs/CAMPAIGN_OPENING_REPORT.md | 2026-09-06 |
| EX-04A | First home opening within EX-04 | agent | done | EX-03 done; D-EX-13 | House supplies, one physical entrance, reachable starter patches, real paid machine placement, state-driven goal, deterministic geometry on load | docs/CAMPAIGN_OPENING_REPORT.md | 2026-09-06 |
| EX-04 | Cul-de-sac, factory start, second-area tram access and radio restoration opportunity | agent | done | EX-03 done; D-EX-Q01–06 decided; D-EX-14 | One physical home approach; reachable resources; hand-supplied station reward; real costs and placements; no tram unlock circularity; radio restoration available | docs/STATION_RESTORATION_REPORT.md; docs/CAMPAIGN_OPENING_REPORT.md | 2026-09-07 |
| EX-05 | Site creatures, shared minor raids, major schedule and radio warning | agent | done | EX-04 done; D-EX-Q01/02/03/06; D-EX-16 | One major target; rest days survive restorations/load; bounded raids; valid paths; actual defence/repair support and useful truthful warnings | docs/CAMPAIGN_DEFENCE_REPORT.md; later third-station milestone maps to EX-06 | 2026-09-07 |
| EX-06 | Multi-stop freight, specialised factories and useful workshop | agent | todo | EX-04; D-EX-Q01/04/05/06 | Selective freight and onward supply; return goods; conserved items; interruption recovery; old factory contributes later; third-station actual resupply enables one quiet cycle | pending — EX-06 evidence | |
| EX-07 | Optional discovery and distinct exploration encounter | agent | blocked | EX-05; D-EX-Q05/06 | Persistent non-duplicating reward, readable encounter, optional detour, essential progression reachable | pending — EX-07 evidence | |
| EX-08A | Prepare representative-loop automated evidence and human session | agent | blocked | EX-05; EX-06; EX-07 | Relevant invariants recorded; build/seed/settings and open limitations recorded before observation | pending — EX-08 preparation | |
| EX-08H | Play the representative loop | human | blocked | EX-08A | EXPLORATION_DEFENCE_PLAYTEST.md observed record, interventions and explicit gaps; human verdict | pending — human play record | |
| EX-08 | Revise and assess representative loop | agent | blocked | EX-08H | Revisions trace to play evidence; no unobserved success claimed; owner gate recorded before content expansion | pending — EX-08 report | |
| EX-09 | Reconcile remaining RI/T obligations, complete factory and broader progression | agent | blocked | EX-08; D-EX-Q07 | Every retained old obligation mapped and acceptance rewritten before execution; Phase 5 exit checked; wider projects and economy validated | pending — EX-09 evidence | |
| EX-10 | Full campaign, scale, accessibility, save migration and release obligations | agent | blocked | EX-09 | Current Phases 10–14 criteria and human gates, with actual evidence and remaining failures explicit | pending — EX-10 evidence | |
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
| T12 | **Phase 5 M1 — recipes, rubble and the 75-minute hour**: the eight non-Shot recipes made by real machines and priced (D-B2-1 (b)); rubble as finite typed ore with visible depletion at D-P4-2's units, rubble sinks for stone; rail-yard coal rubble at ~700 a lot (D-P4-12); outskirts deposits kept as the placeholder unless a run needs them, with a tripwire that reports which half was built (D-P4-3); the HQ patches sized (C5); `constants.ts` `HOUR` re-scoped to 75 minutes and §11 regenerated, north a scored check (D-HOUR-3); the `[play: <gate>]` check in docsync | claude | waived (Daniel, 2026-09-05, "Whole plan" — absorbed into RI-01 (economy, recipes, rubble, coal, the 75-minute hour, the docsync check) and RI-03 (every front edge physical)) | — | cheap checks green; `docsync:check` green after regeneration; `E-hour` green on seeds 3 / 4 / 5 at 75 minutes with north Held and two new rows — rail-yard coal reaching the Generators with ≥ 10 min margin before the Depot's coal is gone, and the arrival count at the first red pip; every new constant tagged or rowed; fixtures unchanged or the change reported as a rules change | `docs/PHASE_5_REPORT.md` section "M1" | |
| T12a | **Next-objective line** (D-GB-2, recommendation (a)): one HUD line driven by the sim's own state naming the truthful next constraint (an emptying hopper, coal minutes left, the claim's price, the front count), no screens, no list | claude | waived (Daniel, 2026-09-05, "Whole plan" — absorbed into RI-02 (the goal line is RI-02's, not a second objective system)) | — | the line is never wrong against the sim's state in an `E-hour` replay on seeds 3 / 4 / 5; it changes at every §11 beat; rule 8 kept; T19 records whether a person followed it | `docs/PHASE_5_REPORT.md` section "Next-objective line" | |
| RI-00 | **Adopt the revised development plan and reconcile the repo** (`REVISED_DEVELOPMENT_PLAN.md` §13, §15): one live task order; the GDD, `PHASES.md`, `PROGRAMME_STATE.md` and `CLAUDE.md` updated; the old constraints and the map claim superseded in `DECISIONS.md`; current build and limits verified; duplicate work avoided | claude | done | T11a | doc-only; the T → RI mapping recorded once (this table); docsync, freshness and snapshot checks green; no gameplay constant moved; the authorising message quoted, not invented | `docs/RI_PASS_1_REPORT.md` "RI-00"; `DECISIONS.md` D-RI-1 … D-RI-6 | 2026-09-05 |
| RI-01 | **Establish the real opening economy and resource accounting** (plan RI-01; T12's scope): the non-Shot recipes whose machines are start-unlocked made by real machines and priced (D-B2-1 (b)), later recipes stay data; rubble as finite typed ore with visible depletion (D-P4-2), rubble sinks for stone; rail-yard coal at ~700 a lot (D-P4-12); outskirts deposits placeholder with the D-P4-3 tripwire; HQ patches sized (C5); `constants.ts` `HOUR` re-scoped to 75 minutes, §11 regenerated, north a scored check (D-HOUR-3); the `[play: <gate>]` docsync check; every assembler physical and the block-level stand-in removed (D-P4-5); a resource-conservation check across production, transfer, delivery commitment and consumption; the hand-fed magazine total explained by the accounting or removed (D-P4-11) | claude | done | RI-00 | cheap checks green; `docsync:check` green after regeneration; `E-hour` green on seeds 3 / 4 / 5 at 75 minutes with north Held, rail-yard coal reaching the Generators ≥ 10 min before the Depot's coal is gone, and the arrival count at the first red pip; the conservation check passes on `E-hour`; no unexplained manual-feed total; every new constant tagged or rowed; fixtures unchanged or the change reported as a rules change | `docs/RI_PASS_1_REPORT.md` "RI-01" | |
| RI-02 | **Opening guidance and essential presentation** (plan RI-02, §11.2; T12a's line): the current-goal HUD line driven by the sim's state with the reason it matters (D-GB-2 (a); rule 8 kept; no list, no screens); engineer, selected tool, machine purpose and running / starved / blocked state readable; place / remove / invalid-action feedback; stable names for destinations, debug coordinates behind a toggle; a save / load baseline (verify existing support first); the desktop UI usable at different viewport sizes | claude | done | RI-01 | the goal line is never wrong against the sim's state in an `E-hour` replay on seeds 3 / 4 / 5 and changes at every §11 beat; a save at minute N reloads to the same state hash and replays identically; two viewport sizes checked; rule 8 kept | `docs/RI_PASS_1_REPORT.md` "RI-02" | 2026-09-05 |
| RI-03 | **Physical commissioning and field deployment** (plan §4; T13's placement piece; D-CU-1, D-CU-3, D-P4-9's other half): one claim path — map selection previews and charges nothing; power connection, materials delivered to the installation and an explicit Activate within reach; paid once; the field kit (poles, unlocked lights, turrets, belts, inserters, chests, the outskirts Substation) placeable on Dark / Contested blocks adjacent to Held, drawing real power from the connected grid; one commissioning event per activation (no double bloom); the outskirts Substation before activation; replay and save consistency; the hour bot on the physical path | claude | done | RI-01 | a transition test covers double activation, invalid prerequisites, missing power and charging once; `E-hour` green on seeds 3 / 4 / 5 with every claim made by the physical path, reported beside the legacy run; no infinite-power flag; a field device never marks a block Held; `snapshot:check` unchanged or the change reported as a rules change | `docs/RI_PASS_1_REPORT.md` "RI-03" | 2026-09-05 |
| RI-04 | **Enemy origins, Crawler / Shade clarity and the Stalker prototype** (plan §6, §7; D-GB-4 owned here): emergence points with stable ids and valid placement; no spawn inside a secured interior; a Crawler's target and direction inspectable, a Shade's trace; the Stalker (`guard → investigate → pursue → attack → return`) tied to an occupied site, its §7.1 candidates in one data configuration | claude | done | RI-02, RI-03 | spawn-validity and safe-interior tests; Stalker leash, wind-up and no-attack-during-dodge tests; `E-rifle` rerun and reported; candidates in a candidate configuration, not the benchmark | `docs/RI_PASS_1_REPORT.md` "RI-04" | 2026-09-05 |
| RI-05 | **Neighbourhood project framework and the rail-yard reward** (plan §5; the minimal part of T16): the project record and stages; deliveries by hand and by belt; the rail-yard restoration as the first project, deliverable before trams; one minimal transport route (one track, two stops, one tram) that makes a real follow-on delivery; a local supply depot as a named chest | claude | done | RI-03 | stages and deliveries survive save / load and replay; ordinary items only; the tram moves one delivery in a replay; no circular unlock | `docs/RI_PASS_1_REPORT.md` "RI-05" | 2026-09-05 |
| RI-06 | **The Junction Heart** (plan §9): two feeder cabinets prepared in either order; Start commissioning; 90 s of productive commissioning paused on power loss (candidate); packets at 25 / 50 / 75 % keyed to attempt and threshold; interrupted after 60 s without progress (candidate) or by abort; retry without a second charge; completion destroys the Heart and grants the unlock once | claude | done | RI-04, RI-05 | §9.4 met by a bot with a prepared factory and no rifle use; interruption / retry / save / load tested; no repeated packet; deterministic attempts | `docs/RI_PASS_1_REPORT.md` "RI-06" | implementation complete 2026-09-05; automated validation **not_run** (code only, per the user's instruction — `E-heart` and `heart.test.ts` are written and unrun; `tsc` and eslint green) |
| RI-07 | **Integrate the opening candidate** (plan §11.1; T12c feeds it): one opening profile with real resources and commands in a candidate configuration separate from the 75-minute benchmark; automation → route choice → small enclosure → rail project → reward as one sequence; T12b's relevant variants rerun on the physical path | claude | blocked | EX-09 (Version 2 scope/acceptance reconciliation); prior: RI-02, RI-06 | a bot plays the sequence end to end on the candidate profile; its pacing measured and reported; the legacy `E-hour` benchmark unchanged; T12b's variants rerun | `docs/RI_PASS_1_REPORT.md` "RI-07" | |
| T12b | **E-hour robustness variants and a policy comparison** on the 75-minute hour (scheduled, not run): ammo automation delayed to minute 15 and 20; west claimed first instead of east; 100 steel spent on nothing at minute 5; the first supply warning ignored for three minutes; a belt tile misplaced at minute 8 and repaired at minute 9; plus a policy table (compact / spike / quiet-block) on ammo, blocks lost, walking minutes, time to first interior and coal margin | claude | blocked | EX-09 (Version 2 scope/acceptance reconciliation); prior: RI-01 (the 75-minute hour); rerun after RI-03 and RI-07 (plan §13) | each variant reported with hypothesis, seed, duration, config hash and commit; seeds 3 / 4 / 5 and 6 / 7 / 8 so no variant is tuned to the fixture seeds; a variant that loses a block names the mechanism, and no constant is changed to make it pass; D-P4-11's hand-feed share reported per variant | `docs/EXPERIMENTS.md` rows `E-hour-robust-*`, `E-policy` | |
| T12c | **E-enclosure-short (hypothesis)**: can the bot reach the first enclosure in 15–25 minutes on a small opening (the HQ and its two river neighbours), and at what ammo, coal and walking cost — an exploration, not a benchmark; the 75-minute hour stays the benchmark | claude | blocked | EX-09 (Version 2 scope/acceptance reconciliation); prior: RI-01 | reported as balance exploration with its hypothesis stated first; no decided constant moved; the result is a recommendation row, not a doc edit; its result feeds RI-07's opening candidate and RI-08's comprehension observations | `docs/EXPERIMENTS.md` row `E-enclosure-short` | |
| T19 | **Human session of the representative loop** (RI-08's human work; revised 2026-09-05 by RI-00 — the original text follows): seed 3 or RI-07's opening profile, one person, the observer silent, recording `REVISED_DEVELOPMENT_PLAN.md` §14.2's list (goal known, preparation and route chosen and why, automation freeing the player, an encounter decision, failure and recovery, restoration and enclosure noticed, the reward used, what was repetitive), time by activity, and observer interventions. Original T19 text: short unaided opening playtest: seed 3, minutes 0–20, one person, the observer silent; record §19's 0–10 and 10–20 rows, whether the next-objective line was truthful and followed, the first pip's minute, the walking share from the telemetry panel, and STANDARDS 4.3 / B.3 / B.6 / C.2 (one minute each) | human | blocked | EX-09 (Version 2 scope/acceptance reconciliation); prior: RI-07 | the record says what was and was not observed; nothing unobserved is scored; superseded criteria (the T12a next-objective line is now RI-02's goal line) are revised in the record, not faked | `docs/GATE_B.md` section "Representative loop session (T19)" | |
| RI-08 | **Playtest and revise the representative loop** (plan §14.2; the human session is T19; D-GB-3's rifle-range candidate tested here): the automated robustness evidence; the session prepared (build, seed, what to record); after T19's record, the comprehension / repetition / recovery problems it names fixed; adopted pacing changes recorded as decisions | claude | blocked | EX-09 (Version 2 scope/acceptance reconciliation); prior: RI-07, T19 | the automated evidence written before the session; every revision traceable to T19's record; no candidate promoted to the benchmark without a decided row; no fun claim from bot telemetry | `docs/RI_PASS_1_REPORT.md` "RI-08" | |
| RI-09 | **Complete the remaining Phase 5 factory features** = T13 → T14 → T15 → T16 (remaining transport) → T17, then T18 (plan RI-09) | claude | blocked | EX-09 (Version 2 scope/acceptance reconciliation); prior: RI-08 (experience-dependent decisions) | the five T rows done; nothing RI-01 … RI-07 built is rebuilt; the five-hour logistics validated | `docs/RI_PASS_1_REPORT.md` "RI-09" (the T rows keep their own evidence) | |
| T13 | **Phase 5 M2 — placement and the command layer**: every Phase 5 §13 machine with its footprint; undo / redo (1.10); drag placement taking a path (1.3); the hotbar as references (1.16); one binding table (1.17); one string table (8.5); every front edge physical (restore from commit 6694b71) — moved to RI-03; the workbench recipe (D-B2-1 (b)) | claude | blocked | EX-09 (Version 2 scope/acceptance reconciliation); prior: RI-08 (runs inside RI-09); its field-deployment piece is RI-03's | placement only through `Command`s from real inventories at reach; expansion validated by real placements, deliveries and inventories, and D-P4-5's block-level stand-in removed or labelled; each hotbar entry carries one line on the decision the machine opens; D-SA-2 signed or reversed by the human before the string table is called done; D-CU-1 recommended (a) as built — if the human picks (b) this task changes | `docs/PHASE_5_REPORT.md` section "M2" | |
| T14 | **Phase 5 M3 — belts complete**: undergrounds with the span shown (1.4), splitters with priority (2.3), the filter (2.5), side-loading, chests, the Depot's item list | claude | blocked | EX-09 (Version 2 scope/acceptance reconciliation); prior: T13 | every chain of §12 whose machines exist by this task buildable end to end with belts alone; later-phase recipes stay data (plan §13 scope correction); one lane (D-P5-3) | `docs/PHASE_5_REPORT.md` section "M3" | |
| T15 | **Phase 5 M4 — power and the panels**: power complete on the tile layer; the machine status enum (2.6), rates (2.7), alt-mode (2.9), per-item counters (3.1); the brownout's HUD read, no map-view dim (D-P5-1) | claude | blocked | EX-09 (Version 2 scope/acceptance reconciliation); prior: T14 | a brownout is read on the HUD bar and the machine panel in a replay; the status enum names why every stopped machine stopped | `docs/PHASE_5_REPORT.md` section "M4" | |
| T16 | **Phase 5 M5 — trams and the driven truck**: track, stops, trams, the truck driven (2.11 / 7.4 / A.9), the route on the map view while a stop is selected (D-P5-6) — the minimal route (one track, two stops, one tram) is built earlier by RI-05; this row completes the remaining transport cases | claude | blocked | EX-09 (Version 2 scope/acceptance reconciliation); prior: T15 | one tram on one track between two stops moves outskirts steel in a replay | `docs/PHASE_5_REPORT.md` section "M5" | |
| T17 | **Phase 5 M6 — slots, the blueprint file and the DoD**: slots as real lot geometry (C3 / D-B2-2 signed or reversed); the bot's blueprint file (D-P5-4, harness JSON); copy / paste (1.11); `E-chain`, `E-coal`, `E-tram`; the bot's 5 h §18 territory; the Phase 5 gate deletions (`--map lattice`, `?map=lattice`, the lattice fixtures and results, `?flow=0`, `sim.ts` `syncEdges`); the phase's full verification | claude | blocked | EX-09 (Version 2 scope/acceptance reconciliation); prior: T16 | the 5 h territory is built from real placements and deliveries at doc rates; the three experiments reported with provenance; full verification green or every failure named, including the light-map blur's browser frame cost and the map view's resize raster; the fifteen STANDARDS rows checked | `docs/PHASE_5_REPORT.md` section "M6" | |
| T18 | **The DoD, played**: build a two-assembler ammo line in the world view, unaided, in under ten minutes (T6 returning) — the definition is on `PHASES.md` Phase 5 | human | blocked | EX-09 (Version 2 scope/acceptance reconciliation); prior: T17 | pass / fail by the definition; the prompts needed and the minutes recorded | `docs/PHASE_5_REPORT.md` section "The DoD, played" | |
| RI-10 | **Complete the regular enemy ecosystem** (plan §7, §7.2): the Breaker (the Hulk's display name) and the Conductor (`idle → signal → recover`, 10× Crawler HP, +20 % linked blooms, suppressed on Held, destroyed for good — candidates); the roster introduced one archetype at a time and combinations tested; turret-damage semantics reconciled (D-B4-3); the minimum real Cannon / Shell chain and Arsenal access for Breaker counterplay | claude | blocked | EX-09 (Version 2 scope/acceptance reconciliation); prior: RI-08; RI-09 for the heavy-supply systems | each archetype's counterplay tested alone before combination; no single-solution encounter; D-B4-3 signed or reversed; the population budget kept | `docs/RI_PASS_1_REPORT.md` "RI-10" | |
| RI-11 | **Expand project progression, tools, city identity and mystery** (plan §5.2, §11.3, §12): the remaining reward catalogue on existing facilities and survivors; learned repetition automated (Phase 8's tools); generated neighbourhoods valid; the reveal data per major project after the internal Rot explanation is written | claude | blocked | EX-09 (Version 2 scope/acceptance reconciliation); prior: RI-09; RI-10 for threat-dependent sites | multiple worthwhile project orders shown by bot; generated neighbourhoods validated; reveals consistent with one written origin | `docs/RI_PASS_1_REPORT.md` "RI-11" | |
| RI-12 | **Later boss encounters** (plan §10): the Furnace Walker and the Blackout Crown, one at a time, each with its state / transition specification in the GDD first and a materially different decision from the Junction Heart | claude | blocked | EX-09 (Version 2 scope/acceptance reconciliation); prior: RI-10, RI-11 | distinct decisions; readable local mechanics; recovery and optional personal combat retained; costs justified by play evidence | `docs/RI_PASS_1_REPORT.md` "RI-12" | |
| RI-13 | **Full progression and the release roadmap** (plan RI-13): the complete Relight progression, seed fairness, scale, saves, accessibility, final presentation, long-run playtests and the platform / release obligations of Phases 10–14 | claude | blocked | EX-09 (Version 2 scope/acceptance reconciliation); prior: RI-09, RI-10, RI-11, RI-12 | `PHASES.md` Phases 10–14 exit criteria met or every failure named | `docs/RI_PASS_1_REPORT.md` "RI-13" | |

## Log

- 2026-09-05 — T1, T2, T3, T4 done; T5, T6, T7 waived and T8 done by Daniel; T9, T10 done;
  T11 done by Daniel. The long entries are in
  `archive/pre-phase-5-cleanup/PROGRESS-2026-09-05.md`.
- 2026-09-05 — T11a done (the pre-Phase-5 cleanup): evidence
  `archive/pre-phase-5-cleanup/README.md` and the commit. T12a, T12b, T12c and T19 added
  with acceptance criteria; T12–T18 given acceptance columns; statuses renamed to the
  constitution's vocabulary; T15's next-objective line moved to T12a.
- 2026-09-05 — RI-00 done (the plan adopted on "Whole plan"): RI-00 … RI-13 inserted; T12 and
  T12a waived into RI-01 / RI-03 and RI-02 by the plan's mapping; T12b, T12c re-blocked on
  RI-01; T19 revised into RI-08's human session and re-blocked on RI-07; T13 re-blocked on
  RI-08 (it runs inside RI-09) and its field-deployment piece moved to RI-03; T14's "every
  chain" corrected to the chains whose machines exist; T16's minimal route moved to RI-05.
  **RI-01 is next.**
- 2026-09-05 — RI-01 done (the real opening economy and resource accounting): the placed
  Assembler on four recipes (D-B2-1 (b)); rubble 300 a tile with the pool following the
  tiles (D-P4-2); the rail yard's 3×3 heap of 702 coal belted into the Depot (D-P4-12, C5);
  `HOUR_MINUTES` 75 with north scored (D-HOUR-3); every Assembler physical and the claims'
  lines real (D-P4-5); `ledger.ts` balancing every item on every `E-hour` run; the
  hand-feed count split into magazines and coal (D-P4-11); the `[play: <gate>]` docsync
  check. `E-hour` 13/13 at 75:00 on seeds 3 / 4 / 5, 13 experiments 0 failing, 124 tests;
  the snapshot regenerated for five new counters (state identical). T12b and T12c are
  runnable behind RI-02. **RI-02 is next.**
- 2026-09-05 — RI-02 done (opening guidance and essential presentation): the current-goal
  line read off the state with its reason and an amber support line (D-GB-2 (a), rule 8
  kept; `goal.ts`); machine running / starved / blocked / idle / off as a shape, a word and
  a reason; stable block names with debug coordinates behind the toggle (`names.ts`); the
  save / load baseline — a state hash, a validated load, Ctrl+S / Ctrl+O to a local slot
  and a download carrying the command log (`save.ts`; the "no save path exists" sentences
  in `PROGRAMME_STATE.md` and §28.10 were wrong and are corrected); 1200 / 900 px
  breakpoints. `E-hour` 18/18: 0 of 13,503 goal samples wrong, 42/42 beats change the line,
  a save at 30:00 reloads and replays to the unbroken hash on seeds 3 / 4 / 5; 13
  experiments 0 failing; 128 tests; two viewports checked in the browser. **RI-03 is next.**
- 2026-09-05 — RI-03 done (physical commissioning and field deployment): one claim path —
  the map previews and charges nothing; `deliver` / `activate` with one commissioning id
  an attempt, paid once at the substation (`commission.test.ts`, 5 tests); the field kit
  on Dark / Contested blocks next to Held on real pole power, never marking a block Held;
  the outskirts Substation before activation (D-CU-3 (b)); the hour bot on the physical
  path with the map claim beside it (`E-hour-legacy`: the claims 8–18 s later, 4 Held, no
  fall). `E-hour` 19/19; 13 experiments 0 failing; 133 tests; snapshot unchanged. The bot
  laying a claim's turrets (D-P4-9's other half) is not built — a Later candidate for the
  human. **RI-04 is next.**
- 2026-09-05 — RI-04 done (enemy origins, Crawler / Shade clarity and the Stalker
  prototype): emergence points, one per block and shared street on the Dark block's
  frontage kerb, ids from the geometry alone, births along the kerb by M4's seeded hash
  and never on a Held tile (`emergence.test.ts`, 4 tests); a crawler's heading and target
  tile and a shade's trace readable in the sim and the world view; the Stalker candidate
  (`candidates.ts`, `stalker.ts`, 8 tests) behind `enableStalkers` / `?stalker=1`, outside
  `SimConfig` (hash b95922d2 unchanged) — `E-rifle-cand-stalker` beside the benchmark
  rows: 5 Stalkers fielded a seed, 0 contacts (the hour bot never nears a well block).
  `E-rifle` 16/16 (red once at 14/16 on a single-tile mouth; the kerb spread is the fix);
  `E-hour` 19/19; 13 experiments 0 failing; 145 tests; snapshot unchanged; no browser
  check. D-GB-4 stays open — no density number chosen. **RI-05 is next.**
- 2026-09-05 — RI-05 done (`docs/RI_PASS_1_REPORT.md` "RI-05"): the project record
  (`packages/sim/src/project.ts`: `projectId, siteId, neighbourhoodBlockIds, requirements,
  deliveredItems, stage, activationAttemptId, rewardId` plus `restoredAt / rewardAt /
  installId`; the stage derived from the site's real state after every block tick and hand
  command, the once-only facts written once; the block record alone owns territory); the
  rail yard as the first project — its claim's materials by hand or off a belt into its
  substation, the Activate its commissioning, restored when Held with the attempt id, the
  reward the transport kit (Track, Tram stop, Tram) unlocked at the restoration's second and
  never before; the local supply depot on the restored yard as a named Supply chest (20 coal
  + 10 magazines, `commission` within reach, the record alone changes, its reward hands out
  kits); the kit as built (a 2×2 Supply chest of 200; Track on streets, walkable; a 2×2 Tram
  stop with two 200-item pools at 20 kW; a one-tile Tram of 200 at 8 t/s, one line, two
  stops, 4 s dwell); belts ending in a chest or a stop, inserters taking from them; the hour
  bot's tram route beside the benchmark (`createHourBot(…, 'tram')`, `tramPlan` read off
  the state) and its `within` re-walk (the sim drops an out-of-reach hand command without a
  word — `DEFERRED.md`); the game's four kinds drawn, keys C / L / H / V, E on a chest or a
  stop, the Projects list, project toasts. `E-project` 10/10 on seeds 3 / 4 / 5: restored
  25:40–25:46 with the kit unlocked the same second, the route laid 26:42–26:47, the tram's
  first delivery 26:56–27:00, the yard's coal at the Depot 27:05–27:15, the depot restored
  32:15–32:18, 732 items by tram, the replay and a 40:00 save to the same hash, every item
  conserved, 4 Held, no fall, 0 refusals. Finding, not tuned: the two stops' 40 kW brown the
  hour out from 26:43 until Generator 4 at 45:00 (1130–1688 s) where the belt route never
  does, and the Tram's 5 Cu leaves copper at 1 on seed 3. `E-hour` 19/19 unchanged bar the
  state hash (the state carries the records); 14 experiments 0 failing (369 s); 148 tests;
  snapshot unchanged; no browser check. **RI-06 is next.**
- 2026-09-05 — **RI-06 done (implementation complete; automated validation not_run).** The
  Junction Heart as the candidate layer (`heart.ts`, `CANDIDATES.heart`, `enableHeart`,
  game `?heart=1` — D-RI-5, beside the benchmark, config hash unchanged): two feeder
  cabinets on the yard's lot toward its two Dark approaches, supplied by `deliver` with
  `cabinet` and powered by a pole run; the Activate as the Start (both cabinets required;
  no bloom, no burn-off, `contestProgress` = the productive fraction); 90 s productive /
  60 s stall; Crawler packets 2 / 3 / 4 at 25 / 50 / 75 % keyed `attempt:threshold`,
  born on the approach's emergence point 5 s after the request, capped at 8; a body at a
  cabinet knocks it out, `repairCabinet` restores it; interruption (stall, X abort, the
  block leaving Contested) keeps the deliveries and the charge and drops the pending
  packets; completion spends the cabinets' materials, destroys the Heart and the yard
  turns Held on its normal path (the kit once). The hour bot's Heart step and patrol
  (`createHourBot(…, heart = true)`), `E-heart` (timeline, end, cabinets, replay with the
  events counted, a 30:00 save, the ledger, the benchmark beside), `heart.test.ts` (six
  tests), the game's cabinets / Heart ring / packet approach, E on a cabinet, X, toasts,
  `describeHeart` on the goal line and the project line. **Per the user's instruction
  ("don't worry about running tests or simulations, just write the code") no test,
  experiment, snapshot or browser check ran**: `npx tsc --noEmit` on sim / harness / game
  and eslint were the only checks (green). Verification is a separate pass. **RI-07 is
  next.**

- 2026-09-06 — EX-00: D-EX-09 authorised the documentation migration; D-EX-10 confirms Phase 4 complete. Added EX execution rows. All formerly todo/in_progress/blocked RI/T rows now blocked on EX-09 reconciliation; prior dependencies preserved. Completed/waived rows unchanged. EX-01 next and ready. Editorial work complete; automated validation partial, report names environment blockers.

- 2026-09-06 — EX-01 in_progress: owner requested recommendation options. Drafted Q01–06 options with proposed defaults/tradeoffs; core decisions remain open. Inspected relevant fetched-main and tram source differences read-only. Full baseline/compatibility audit and integration remain outstanding; no gameplay code or branch changes.

- 2026-09-06 — EX-02 done by the owner's explicit acceptance of all recommendations (D-EX-11); Q01–06 decided. EX-01 remains in_progress for its independent technical audit. No repeated design approval is needed; EX-03 retains baseline/integration dependencies.

- 2026-09-06 — EX-01 done: baseline comparison and targeted evidence recorded. Owner authorised newer-baseline integration and tram work (D-EX-12). EX-06A inserted before EX-03 as the independent transport increment; EX-03 in_progress for the remaining campaign/evidence profile work.

- 2026-09-06 — EX-06A done: tested main merged, station freight/UI/save increment implemented; 161 simulation tests and required code checks passed; browser controls checked. Campaign profiles and the wider redesigned loop remain unfinished. See TRAM_INTEGRATION_REPORT.md.

- 2026-09-06 — EX-03 done: explicit campaign identity, schema 3, separate save slots, profile-aware evidence/docs and legacy pressure isolation verified. EX-04 unblocked and in_progress; EX-04A inserted and done for the independently verified house/court/factory opening. Station restoration and radio remain pending; no human play gate completed. See CAMPAIGN_OPENING_REPORT.md.

- 2026-09-07 — EX-04 done under D-EX-14: paid second-area station restoration, once-only route-sized tram kit, radio restoration, remote construction/local power and UI/save/replay checks. EX-05 is todo; raids, assaults, core damage/repair and warnings remain unbuilt. See STATION_RESTORATION_REPORT.md.

- 2026-09-07 — D-EX-15/16: owner confirms Phase 5 unstarted and continues the intervening revision. EX-05 in_progress; defence, schedule, recovery and radio checks underway.

- 2026-09-07 — EX-05 done for the two-base defence slice: 180/180 full tests, build, browser defeat/paid recovery, docs/profile and legacy snapshot checks; report records provisional tuning and no human play verdict. EX-06 todo, including the third-station/resupply progression dependency. Phase 5 remains unstarted.
