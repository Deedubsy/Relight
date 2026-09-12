# Repository guidance

**Unity migration (2026-09-11):** the project is paused as the runnable reference for a Unity port. Start with `Unity/README.md`; `Unity/Docs/TASKS.md` alone owns migration task status. The rules below still govern any edit to the reference project.

**Active implementation:** D-GP-START authorises GP-01–14. Follow the current block in `docs/EXPLORATION_DEFENCE_PLAN.md` and `docs/PROGRESS.md`. Earlier planning-only review blocks below are superseded for this scope; GP-15–25 remain held for checkpoint review.

**Gameplay planning handoff (2026-09-10):** Latest accepted intent and labelled proposals: [design](docs/Design/RELIGHT_PROGRESSION_AND_WEAPONS_DRAFT.md). Existing active [implementation plan §6](docs/EXPLORATION_DEFENCE_PLAN.md#6-gameplay-implementation-reconciliation-and-delivery-plan) reconciles code and phases; [PROGRESS](docs/PROGRESS.md) alone owns status. Await owner review and explicit implementation authorisation; GP-01 is first thereafter. New accepted Keys/Schematics/Artifacts, roster and global raid direction supersede conflicting older gameplay text; proposals remain open. Existing instructions and permission rules are preserved.

**Current CITY-F handoff (2026-09-10):** city-wide district density is `riverfront-arc-v4-editor-ac16d9188c05`, with 479 buildings, 77 rebuilt smaller blocks and ten retained civic blocks with new forecourts/details. The approved CITY-E starting neighbourhood, campaign buildings, factory yards, roads, tram and resources are preserved. Phaser has 959 objects (479 buildings/plots plus reference); 22 campaign/interior buildings remain fixed. [Current evidence](docs/evidence/city-density/README.md) and [editor workflow](docs/PHASER_EDITOR.md). Reload 5178 and choose New Game; live Editor Play is 5190. Existing artwork remains pending the owner's new package. Human/release gates remain open.

**Prior CITY-D handoff (2026-09-10):** CITY-D uses `riverfront-arc-v4`, 864 × 576 tiles, 287 buildings and 371 connected public-road edges. The full-city grid, four factory yards, service drives, fitted parcels and rounded four-stop tram are defined in `packages/sim/src/city/riverfront.ts`; `riverfrontRail.ts` supplies shared rail art/motion geometry. Run `validateRiverfront` when changing placements: roads/pavements, swept rail clearance, entrances and factory reservations are enforced. Home → Riverside → Ironworks → Civic, real inventories, finite power and 1×/pause remain authoritative. Earlier authored v1/v2/v3 saves are preserved but need their original builds; choose New Game for v4. Procedural saves remain isolated. Six additional downtown forms and existing roof/lighting rules are integrated. Enemy variety, bosses, new access locks and campaign-duration tuning remain deferred. [Current map conventions and evidence](docs/Implementation/RIVERFRONT_CITY.md) own the detailed layout; preview is mutable port 5178. Human/release gates remain open. Founders Court now has six homes, four-direction door/path support, an open concrete yard and readable opening daylight. Regenerate via `docs/evidence/city-c/authorFullCity.py`; its current runtime template drift guard must remain enforced.


**Owner implementation direction (2026-09-09):** implement EX-09C-01–09 plus RI-10/RI-12 combat/encounters now. The active campaign uses defended factory/plant sites, never a territorial frontage/ring or adjacent-block-claim economy. Site lifecycle and explicit truck recovery replace the former EX-09C-04 territory scope. Normal new-game entry is to use exploration-v2; explicit legacy saves/mode remain isolated. Real inventory, connected finite power and 1×/pause remain authoritative. Q07 final requirements and human/release gates stay open, not prerequisites of this authorised correction set.

Read CLAUDE.md for the current coding-agent workflow, then docs/PROGRAMME_STATE.md and docs/PROGRESS.md. Phase 4 is complete; the active design and successor plan are docs/RELIGHT-design.md and docs/EXPLORATION_DEFENCE_PLAN.md. docs/REVISED_DEVELOPMENT_PLAN.md is historical Version 1.

RI-02B — Intuitive UI and Visual Identity follows Phase 9 engineering/preparation (P9-05): read docs/RI-02B_UI_SPEC.md and D-UI-02. Reuse P9 names/pins/navigation; PROGRESS owns the next runnable task.

Current user instructions override older repository directions. docs/DECISIONS.md records approved rules and unresolved contracts; docs/CONSTITUTION.md defines evidence and authority. PROGRESS is the only task list. Legacy references describe earlier rules, not current acceptance.

Keep gameplay in packages/sim; rendering submits ordinary commands. Reuse existing code and fetched-main city/Heart improvements after baseline reconciliation. Do not merge branches or claim tests passed without verifying. Follow the documentation and code verification policy in CLAUDE.md.

<!-- CODEGRAPH_START -->
## CodeGraph

In repositories indexed by CodeGraph (a .codegraph/ directory exists at the repo root), reach for it BEFORE grep/find or reading files when you need to understand or locate code. Prefer codegraph_explore or codegraph_node MCP tools when available; the shell equivalents are codegraph explore and codegraph node. If there is no .codegraph/ directory, skip CodeGraph entirely; indexing is the user's decision.
<!-- CODEGRAPH_END -->
