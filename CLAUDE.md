# Relight — coding-agent working rules

**Current CITY-F handoff (2026-09-10):** city-wide district density is `riverfront-arc-v4-editor-ac16d9188c05`, with 479 buildings, 77 rebuilt smaller blocks and ten retained civic blocks with new forecourts/details. The approved CITY-E starting neighbourhood, campaign buildings, factory yards, roads, tram and resources are preserved. Phaser has 959 objects (479 buildings/plots plus reference); 22 campaign/interior buildings remain fixed. [Current evidence](docs/evidence/city-density/README.md) and [editor workflow](docs/PHASER_EDITOR.md). Reload 5178 and choose New Game; live Editor Play is 5190. Existing artwork remains pending the owner's new package. Human/release gates remain open.

**Prior CITY-D handoff (2026-09-10):** CITY-D uses `riverfront-arc-v4`, 864 × 576 tiles, 287 buildings and 371 connected public-road edges. The full-city grid, four factory yards, service drives, fitted parcels and rounded four-stop tram are defined in `packages/sim/src/city/riverfront.ts`; `riverfrontRail.ts` supplies shared rail art/motion geometry. Run `validateRiverfront` when changing placements: roads/pavements, swept rail clearance, entrances and factory reservations are enforced. Home → Riverside → Ironworks → Civic, real inventories, finite power and 1×/pause remain authoritative. Earlier authored v1/v2/v3 saves are preserved but need their original builds; choose New Game for v4. Procedural saves remain isolated. Six additional downtown forms and existing roof/lighting rules are integrated. Enemy variety, bosses, new access locks and campaign-duration tuning remain deferred. [Current map conventions and evidence](docs/Implementation/RIVERFRONT_CITY.md) own the detailed layout; preview is mutable port 5178. Human/release gates remain open. Founders Court now has six homes, four-direction door/path support, an open concrete yard and readable opening daylight. Regenerate via `docs/evidence/city-c/authorFullCity.py`; its current runtime template drift guard must remain enforced.


**Prior correction handoff (2026-09-09):** EX-09C-01–09 and included RI-10/RI-12 mechanics are implemented. Read docs/Implementation/GAMEPLAY_CORRECTIONS.md for the ordinary empty opening, integrated evidence, initial defaults and remaining regression/freshness findings. Current preview is rebuilt mutable port 5178, bare exploration entry. Human observations and Q07/final campaign/performance gates remain open; PROGRESS owns the next work.

**Owner implementation direction (2026-09-09):** implement EX-09C-01–09 plus RI-10/RI-12 combat/encounters now. The active campaign uses defended factory/plant sites, never a territorial frontage/ring or adjacent-block-claim economy. Site lifecycle and explicit truck recovery replace the former EX-09C-04 territory scope. Normal new-game entry is to use exploration-v2; explicit legacy saves/mode remain isolated. Real inventory, connected finite power and 1×/pause remain authoritative. Q07 final requirements and human/release gates stay open, not prerequisites of this authorised correction set.

Current task pointer: PE-PLAYTEST-02 owner playtest corrections are complete; read docs/Implementation/PLAYER_PLAYTEST_FOLLOWUP_02.md and PROGRESS. Reload 5178 for owner retest. The earlier finding-6 exclusion and human/release gates remain unchanged.

Current direction: exploration, tram expansion and intermittent base defence. Phase 4 is complete. P5 implementation and technical review are complete, with successful factory, tram/truck and defence/recovery play reported by the owner. P6-00–04 and P7-01–05 are complete; EX-08C preserves the post-P7 build on port 5179. D-EX-39 now moves shared playtesting after Phase 8. The Phase 8 scope review is complete in docs/PHASE_8_SCOPE_REPORT.md; Q10 adopts the existing truck for queued blueprint construction from a chosen local supply chest. P8-01–04 provide the Foreman clipboard, saved library/ghost orders, physical truck construction delivery and conserved area removal/order controls. P8-05 construction integration and engineering review is complete. EX-08D post-P8 playtest preparation is complete on frozen port 5180; shared human play remains available. D-EX-46 now authorises independent Phase 9 city work; P9-01 validation and its evidence runner are complete (docs/P9_01_CITY_VALIDATION_REPORT.md). P9-02 fixes the seed-6/29 tram-survey failures for new games while preserving existing saves (docs/P9_02_SURVEY_RELIABILITY_REPORT.md); all 32 diagnostic seeds pass. P9-03 saved names, pins and wayfinding are complete (docs/P9_03_WAYFINDING_REPORT.md); P9-04 completed all 10,000 declared seeds, with 258 failures retained. P9-04R now repairs all 258 findings and passes the separate 10,000-seed comparison. P9-05 engineering/preparation is complete; RI-02B-UI-01–07 history is complete; UI-08 incorporates the owner’s updated interface direction and P9-H remains available. Human verdicts remain separate. P6-AMMO awaits the owner’s retest under D-EX-32; formal human verdicts and performance findings remain open. PROGRESS owns execution order. Do not resume the old RI task order automatically.

## Read order

1. docs/PROGRAMME_STATE.md — checkout versus fetched-main state, evidence and next work.
2. docs/PROGRESS.md — the only task list and its dependencies.
3. docs/CONSTITUTION.md — scope, authority and verification.
4. Relevant D-EX decisions in docs/DECISIONS.md, including any open contract the task needs.
5. docs/RELIGHT_CONFIRMED_GAMEPLAY.md is the current gameplay authority. Read the relevant docs/RELIGHT-design.md implementation/legacy references and docs/EXPLORATION_DEFENCE_PLAN.md task context only subject to that authority.

README.md and AGENTS.md point to the same documents. REVISED_DEVELOPMENT_PLAN.md is the preserved Version 1 plan, not a second executable plan. The GDD's legacy reference is retained for existing tooling and code; its old prohibitions and benchmark timings do not govern new implementation. Archives/reports are evidence, not instructions to restore superseded behaviour.

## Work and authority

Follow the user's requested execution scope. Current user instructions take precedence. For a next-task request, choose the first runnable PROGRESS row, inspect its actual dependencies, implement only authorised scope, verify, update its evidence and handoff, and report. Ask only for genuinely missing core decisions; proceed with independent work. A documentation revision is not permission to implement every discussed example.

Do not infer a completed later phase from a completed RI feature. EX-01 reconciled the baseline, and authorised integration retained newer main's city/Heart work. EX-03 separates campaign/evidence profiles; preserve both the tested baseline and the adopted design. No merge, push or destructive evidence operation without user authorisation.

The sim alone mutates gameplay through commands. Bots use player inventories and reach. Renderer-only effects cannot imply mechanics the sim does not implement. Record code, automated validation, design approval and human play separately.

## Verification

Code: npm test, npm run typecheck, npm run lint, npm run docsync:check and focused checks required by the task. Phase gates require the constitution's broader policy. Docs: docsync:check, freshness:check and referenced-path/consistency checks. Report baseline failures and environment blockers, including unrun checks, without rewriting expectations to pass.

Current documentation compatibility: the GDD keeps original generated blocks and guarded legacy prose intact. EX-03 routes campaign constants to docs/CAMPAIGN_RULES.md and campaign evidence to explicit campaign subdirectories; docsync checks both profiles by default. Do not regenerate old evidence into the new game.

## UI implementation

Use docs/RI-02B_UI_SPEC.md, including D-UI-10, for current UI contracts; PROGRESS alone owns live status. Components: game/src/uiShell.ts, buildPanel.ts, inventoryPanel.ts, hud.ts, itemIcons.ts and uiDrag.ts; shared tokens are in style.css. Customisable shortcuts are browser preferences, separate from inventory. Item counts, build costs, placement and transfers use authoritative sim selectors and commands. Backpack/storage use slot grids; optional engineer.pack allocation and inventory commands live in sim/engineer.ts, and storage retains construction/flow validators. Never infer stock at Home is carried stock.

Player gameplay is fixed at normal 1× with pause through session.ts; speed buttons, bindings and interactive fast-forward hooks must not return. Headless deterministic runners retain fast execution. Resume/load clear old accumulation and normalize running speeds. uiShell owns input capture and panel replacement; Escape cancels drag first. Keep headers, slots, HUD and tooltips from overlapping. Menus overlay the world; opening or closing them must not shift the camera. Verify focused behaviors and open actual-game screenshots at ordinary and enlarged/small sizes. For this bounded redesign, the owner explicitly requests focused tests instead of long simulation campaigns.

## Handoff

PROGRESS owns task status and one log entry per meaningful status change. PROGRAMME_STATE states the actual branch/build and the next runnable task. DECISIONS holds new or superseded rules with accurate provenance. The final report names changes, actual checks and material limitations. Do not fabricate attribution or commit trailers; commit/push only within the user's authorised scope.
