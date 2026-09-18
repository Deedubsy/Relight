# Home opening repair plan

Proposed 2026-09-14 in response to the Unity/Phaser audit. Owner follow-up on 2026-09-14 accepted the correction-review repair order, required Tab for inventory, and authorised UI improvements. Implementation evidence for the current pass is in [UI_REPAIR_HANDOFF_2026-09-14.md](UI_REPAIR_HANDOFF_2026-09-14.md); the original planning notes below are historical where they say implementation is not authorised. Work status lives only in [TASKS.md](TASKS.md). Findings and measurements remain in the [audit](UNITY_PHASER_AUDIT_2026-09-14.md).

## Outcome

Deliver a Home-region build that looks recognisably like the Phaser reference and can be played from New Game through mining, construction, power, the introductory defence, automatic ammunition delivery and the three-turret objective, with save/continue throughout. The player can understand what is happening without developer assistance.

Use the existing Unity simulation, data and save architecture. Repair missing connections and actual defects. Restore the existing Phaser visual baseline first; this is the recommended visual direction, pending the owner's response. A new art direction and the owner's final art package can follow after this baseline works.

Full-city expansion, tram, truck, blueprints, later plants and the ending remain with their existing migration tasks. Opening visuals are part of this repair milestone, not an excuse to defer readability until the full city arrives. Minimal opening audio is a proposed advance of a bounded part of D-11; it does not complete D-11. Existing art provenance and release gates remain as recorded.

## Work packages

### C-R01 — Establish the repair baseline

- Record the current working tree and preserve the substantial uncommitted work before implementation. No reset, clean, merge or automatic commit. Store new evidence separately from old evidence.
- Capture matching Home exterior, interior, construction, Backpack, workshop and defence views from the frozen Phaser reference and current Unity build, with comparable framing, time of day and UI scale. Existing screenshots are historical evidence; label them accordingly.
- Build a small player-action checklist linking each claimed Phase C feature to its visible control, input handler, simulation command/query and feedback. Cover construction, inventory, production, combat, repair, settings and saves. Classify each as reachable, broken or intentionally deferred using code and runtime observations.
- Check availability of the local reference runner and Unity test/build tools. Resolve ordinary dependency setup during implementation; record genuine blockers. Do not restore old reference gameplay development.

**Exit:** a bounded baseline and a verified list of missing player routes, including any further gaps discovered before their UI is designed. This is not a broad rewrite or full reference-test port.

### C-R02 — Repair the UI shell and layout

**Main files:** `UI/GameUI.uxml`, shared USS, HUD, Inventory/Workshop, Guide, UiShell and InputRouter.

- Give template hosts a real viewport-sized layout; repair zero-height parents before tuning their children.
- Keep health, ammunition, current goal, warnings and opening instructions visible. Repair Backpack/workshop scrolling, item slots, recipe cards, headings and equipment-strip layout.
- Provide one visible navigation/action bar with Build, Backpack, Help/controls and Pause. Keep one drawer open at a time. The old diagnostic status panel leaves the ordinary player route.
- World-space input must not fire through UI controls. Opening a drawer keeps simulation running but routes input correctly; pause remains explicit. Camera position and framing do not depend on panel size.
- Handle compact layouts through scroll/collapse rules; do not hide essential controls or shrink text until it becomes unreadable.

**Exit:** full-frame captures and measured bounds at 1366×768, 1920×1080, 2560×1440 and 3440×1440, each at 100/125/150% UI scale. Required elements are inside the viewport, controls remain usable, and there is no unintended overlap. Check open drawers and pause as well as an empty HUD. A width-only assertion is insufficient.

### C-R03 — Connect building, the action bar and contextual interaction

**Main files:** WorldInput, input actions, binding definitions, UiShell, InventoryPanelController; add focused Build and interaction adapters where missing.

- Add a data-driven Build picker with icon, name, footprint, cost, available carried stock, availability and refusal reason. Reuse the existing catalogue and placement queries; do not reimplement costs in the UI.
- Choosing a build entry puts the machine in hand without requiring a preassigned bar slot. Support assigning and swapping machine tools and weapons on the persistent action bar; choosing a tool does not create inventory.
- Connect B to Build and the Backpack control to the real Backpack. Recommended defaults: I for Backpack, with Tab as a compatible alias; retain valid saved rebindings and show the live bindings in hints. This default/alias mapping is a proposal to settle with the plan.
- E resolves the pointed/reachable Home or machine target and opens the correct workshop, storage or inspection view, with a clear out-of-reach reason. Keep equipment actions explicit and resolve E/weapon precedence consistently with the accepted reference behavior.
- Wire storage selection to `OpenStore`, clear stale pairing when a target disappears, and update the paired view after movement or load. Opening Home exposes its repair card and workshop.
- Preserve left-click use/place/fire, right-click cancel/pick-up, placement validity, rotation and refunds. Resolve R's rotate/reload behavior in one place. Audit existing duplicate action subscribers so one input cannot send conflicting actions.
- Connect the full Escape chain, including drag, panel, handcraft cancellation and held-input release. Verify the approved stationary-craft interruption/refund rules through the real command paths; document any rifle-craft discrepancy against U-D-11 rather than silently inheriting an old coordinator exception.

**Exit:** using visible buttons and real Input System actions, a new player can mine, choose and place the first machine, rotate/pick it up, open Home, open the Backpack and transfer fuel/materials. No direct `AssignBarCommand`, `shell.Open` or private handler invocation may stand in for the control being tested.

### C-R04 — Make the production chain operable and understandable

**Main files:** machine inspection/production UI, WorldInput construction modes, existing production/flow queries and command adapters.

- Show each machine's inventory, selected recipe, progress, input/output ports and authoritative operating reason: unpowered, out of fuel, no input, output full, throttled or disabled.
- Add recipe selection through `SetRecipeCommand`, with the correct station/availability restrictions and ingredient/output information.
- Provide the reference's needed belt placement/rotation behavior, underground entrance/exit pairing, inserter orientation and splitter configuration where the port already claims support. Confirm routing/configuration controls against the C-R01 checklist.
- Make fuel, power links, throughput and turret feeding visible. Distinguish carried inventory from nearby machine stock.
- Exercise repeated merges, partial transfers, split/move/sort, the rightmost/scrolled slots and full inventories without loss or duplication.

**Exit:** build and operate the required extraction/storage and ammunition-supply routes through ordinary controls. Deliberately disconnect power, remove fuel and fill output storage; the interface explains the stoppage and recovery. Produced ammunition physically reaches a turret and updates the tutorial.

### C-R05 — Restore Home's visual baseline

**Main files:** imported-world presenter/painter, prefab registry and machine views, asset import pipeline, item icons, engineer/enemy/lighting presentation, camera framing.

- Reuse existing riverfront SVG floor/roof assets through the recorded rasterise-at-export pipeline. Preserve building anchors, facing, variants, roof/interior behavior and collision independence.
- Draw the Home region's actual paths, roads, kerbs, court, resource patches, props and site labels from canonical imported data. Extend missing presentation metadata through the migration exporter/importer without hand-changing collision or the authored city. Preserve generated/manual boundaries and GUID references.
- Replace the universal chest fallback for opening machines with distinct silhouettes, footprints, facing and state cues. Retain any compatible reference art; where the Phaser renderer draws shapes procedurally, recreate the useful shapes rather than searching for a sprite that does not exist.
- Give the engineer, enemies and resources distinct readable forms. Populate item/build/recipe icons. Keep turrets' stationary bases, rotating barrels and muzzle-aligned tracers legible.
- Match the useful Home camera framing and readability before visual polish. Check daylight, night, flashlight, powered lamps and interior transitions against the reference and simulation lighting rules.
- Add validation that flags a player-buildable kind accidentally using the generic chest fallback or a required missing icon. Later unavailable content is not disguised as implemented content.

**Exit:** comparable exterior, interior, factory and defence captures look recognisably like the reference and communicate the same functional information. Ordinary machines are distinguishable without selecting them. Art changes do not change collision, reach or power connectivity. The owner's final art replacement remains separate.

### C-R06 — Close the tutorial and defence experience

- Follow the goal chain from actual player actions and update guidance that currently advertises missing controls. Goals and shortage counts must remain truthful after repair, reload and save/load.
- Keep the warning direction, incoming enemies, turret aim, shots, hits, ammunition consumption, reload and damage readable in the repaired HUD/world.
- Reproduce the known workshop-wall turret-sight failure. First show valid deployment guidance and distinguish placement legality from effective line of sight; preserve the reference's collision/LOS rules. Only change combat rules if code comparison demonstrates a port defect or the owner chooses a design change.
- Verify death/downing, Home damage, repair/recommission, introductory loss/defer/skip behavior and recovery through accessible player controls. Check the nearest excursion promised by C-ACC: confirm it exists and has a reachable payoff; if an imported site is only metadata, expose that gap in TASKS before claiming this milestone complete.
- Proposal: bring forward essential opening sound cues and working volume controls using the existing AudioCueRouter. This is a limited D-11 subset; check actual clips and mixer routing. It must not become a new audio-content programme.
- Tune provisional values only after the ordinary starting economy can be played. Record a concrete problem, old/new values and the retest; do not grant resources or loosen rules to conceal a missing interaction.

**Exit:** the player can understand and survive the intended introductory defence, see automatic replenishment, reach the three-turret goal and recover from a mistake. Record any genuine design question separately from implementation defects.

### C-R07 — Verify the complete opening and hand off a player build

- Run focused checks after each preceding change. At integration, run the relevant existing EditMode/PlayMode regressions once on the final combined sources; investigate failures rather than changing tests to accept missing UI.
- Add a small set of real-route PlayMode checks using UI pointer events/Input System actions. Pure simulation fixtures remain valid for isolated rules, but are not evidence for player access.
- Run the primary acceptance journey at fixed 1× with the normal 20 Steel/5 Copper starting stake and no debug grants, teleports or direct gameplay-command injection. Pause is allowed. Save/continue after constructing the line and during the encounter; verify world views, UI selection, warnings and ammunition/production state recover without duplicate waves or phantom machines.
- Build a Windows development player from Boot, launch it, exercise New Game, the player controls, pause/settings and an actual quit/relaunch/Continue round trip. Protect existing manual saves and record the test slot used. Editor success alone is not player-build verification.
- Record full-frame screenshots, observed controls, the relevant test results and any remaining limitations. Update the Phase C handoff's launch instructions and scope claims from the evidence. C-ACC remains an owner verdict.

**Exit:** a runnable player build, evidence for the whole ordinary-control opening, and an accurate handoff. The owner can play the same route without needing the Inspector, a console or a developer explanation.

## Delivery order and review points

C-R01 → C-R02 → C-R03 → C-R04 → C-R06 → C-R07. C-R05 starts after the baseline/layout decisions and supplies readable machine visuals alongside C-R03/C-R04; it must be complete before the final experience check. This is a dependency order, not permission to start implementation during planning.

Three concrete review points:

1. **Usable opening screen:** readable HUD/Backpack, Build picker, contextual E and the first placed/fuelled machine. Include an early Home visual comparison so appearance does not drift unnoticed.
2. **Recognisable, functioning Home:** repaired world/machine visuals, readable production, a defended turret and real automated supply.
3. **Playable handoff:** ordinary-control opening verified, save/continue checked in a Windows player, owner C-ACC play route ready.

Do not estimate delivery time from current test counts. Reassess the remaining scope after C-R01 and the first usable-screen review, when the number of disconnected controls and reusable assets is known.

## Tracker and approval handling

The added C-R rows in TASKS are proposed repair work, all unstarted. Historical implementation entries remain as evidence of what was built; they are not evidence that the current opening is playable. The audit correction at the top of TASKS points at the missing integration. The existing C-ACC human gate covers the repaired opening; no duplicate acceptance verdict is created.

Outstanding plan preference: restore the Phaser visual baseline first (recommended) versus redesign now. This is not a blocker to defining the functional repairs. Implementation begins after the owner directs it; this plan does not grant itself approval.

## Planning verification

All plan links resolve, and C-R01–07 each occur once as tracker rows. No gameplay code, Unity scene or asset was changed during planning. `docsync:check` and `freshness:check` were attempted and both stopped before running because `tsx` is unavailable; logs are `evidence/phase-c/repair-plan-docsync.log` and `evidence/phase-c/repair-plan-freshness.log`.
