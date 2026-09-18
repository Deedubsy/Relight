# UI and interaction repair — 2026-09-14

Implemented by Codex in the existing Unity project after the owner accepted the correction-review direction, explicitly required Tab for inventory, and authorised UI improvements. Existing uncommitted work was preserved. No commits or merges were made.

## Player-visible changes

- **Tab** opens/closes Backpack; opening it afresh clears an old machine pairing. The HUD button follows the same route. A visible Close · Tab button is available in the drawer.
- **E** opens a nearby machine under the pointer. At Home it opens the workshop; a held weapon keeps its equip fallback. Out-of-reach and missing-target explanations appear in the HUD.
- A machine's drawer shows its real inventory, operating reason and processing progress. Processors offer supported recipes with ingredient/output details; inserters offer item filters and splitters offer output priorities. Transfers continue through the existing inventory commands and their reach/capacity checks.
- **R and number keys have one global dispatcher.** Inventory no longer issues a second reload/equip request. Typing a quantity suppresses these gameplay shortcuts and B; Tab remains available to close the drawer.
- **Underground belts:** click an entrance, then an exit. The preview outlines both endpoints and their connection, with validity from the pair-placement rules. Neither endpoint is built or charged until the pair is accepted. Escape/right click cancels the held tool.
- Backpack gives carried items priority, uses larger slots with independently positioned stack counts, truncates long labels instead of wrapping over counts, and keeps quantity/transfer actions in a fixed footer. Home workshop is a collapsible section, expanded when opened through Home interaction.
- Sixty UI sprites were rasterized from the original local vector illustrations in `packages/game/src/itemIcons.ts`. A Resources-backed icon library supplies these to inventory, Build and workshop UI. This adds no external art dependency and does not change the world's machine prefabs.

## Save correctness

WorldBootstrap now carries the imported geometry's region ID and origin through SiteBridge, including regions with no site records. New saves record their coordinate system. Region-aware saves cannot load against a running world with missing metadata, and an imported save with unknown region metadata is refused without rewriting its source file.

The old integrated evidence save is not a valid successful-load fixture: after the +43/+91 relocation becomes active, its turret falls inside a building at (74,357). The reader now reports that geometry conflict. A separate valid Home-region fixture verifies (30.5,264.5) → (73.5,355.5), then a full-map save/reload without a second shift. The old evidence file remains byte-identical.

## Verification

- Unity compilation passed.
- EditMode simulation suite: **564 passed, 0 failed**. The new reach test confirms that distant recipe changes are refused without mutation. Existing production fixtures now place the engineer beside the machine when configuring it; their original production/conservation assertions remain.
- Final PlayMode suite: **60 passed, 0 failed, 5 skipped** (65 total). [Runner result](evidence/ui-repair/play-final.json). Covers actual Tab/E/R input, authored panels without stubs, valid Home relocation and full-map round trip, explicit ambiguous/geometry-conflict refusal, recipe UI dispatch and persistence, two-click underground endpoints, tooltip lifecycle and existing regressions. One intermediate run was aborted by Unity; the final restarted run completed.
- Live 1920×1080 and 1280×720 Backpack captures were opened and inspected. The resized Game view was allowed to finish layout before capture. Stack counts and footer controls are visible; lower sections are scrollable at 720p.
- [1080p Backpack](evidence/ui-repair/inventory-1080.png), [720p Backpack](evidence/ui-repair/inventory-720.png).
- The tooltip test initially had a transient failure at its fixed 0.4-second wait. Its reveal check now waits up to two real seconds for the UI scheduler; the immediate-hidden, correct-anchor, nonblank-content and hide-on-leave assertions remain.

Tests with extra supplies are explicitly fixture tests of UI/command integration. They do not establish the ordinary starting economy or complete the no-debug opening acceptance journey.

## Remaining scope

`TASKS.md` remains the status authority. This pass does not complete C-R01–07 or C-ACC. Still to verify or finish: the ordinary opening through defence and automatic ammunition supply, craft interruption/recovery, complete machine port/throughput feedback, distinct world machine/actor art and lighting, further display/UI-scale checks, and a standalone Windows quit/relaunch/Continue playtest. Ambiguous existing version-4 saves require case-specific recovery rather than guessed coordinates.

`npm run docsync:check` and `npm run freshness:check` were attempted; both remain blocked because the repository has no available `tsx` command. Report links and UTF-8 encoding were checked separately. The original 1080p Game view was restored. Final capture sessions disabled runtime autosave/save-on-quit and did not rewrite reference evidence or manual saves.

Final Editor state: edit mode, original World and GameUI scenes open and clean.
