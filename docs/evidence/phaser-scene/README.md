# EDITOR-02 — visual city scene and validated import

2026-09-10. Owner authorised creating a visual Phaser Editor scene and connecting its saved layout to the game. This is a bounded building-layout authoring pass, with the editing limits in [the user guide](../../PHASER_EDITOR.md).

## Delivered

- Native `RiverfrontCity.scene`, following the official starter's v5 scene JSON. 287 building Image objects plus one terrain/reference Image, 32 px snapping, distinct UUIDs and readable building labels. Generate Code is disabled because simulation data remains authoritative.
- Original SVG roofs, correctly scaled from their 320×256 native textures. The reference image is 3456×2304, scaled to 27648×18432, avoiding oversized GPU textures; it includes roads, paths, plot outlines, tram, sites, yards, resources and props.
- Separate scene manifest/source snapshot, stable-ID importer, source-drift protection, validation before atomic source replacement, and original-generator overwrite protection.
- Supported edits: building X/Y within existing plots and same-kind/facing roof variants. Seventeen campaign-bound buildings cannot move. Doors, collision footprints, visuals, entrance paths and owned props follow supported moves. Unknown/unsupported transforms and reference geometry edits fail with an explanation.
- Content-derived IDs isolate saves from changed layouts. Original v4 ID and exact source return on restoring the original scene. No runtime editor library or second gameplay scene.
- Windows/WSL commands and 500 ms polling on mounted Windows directories. The first browser render exposed missed filesystem notifications; restarting with polling resolved the new asset-pack load failure.

## Verification

- `focused-tests.txt`: three importer regression tests (no-op records, move/door/path/prop coupling, roof selection and rejection cases). `retained-tests.txt` records the retained Founders Court checks.
- `move-check.txt`: northwest Home house moved one tile east and changed roof variant; all city geometry/entrance checks pass, 287 buildings, 371 roads, 372448 accessible cells.
- `render.json`, `scene-home.png`, `scene-city.png`: actual Phaser loads and renders all 288 scene objects with both packs, correct reference dimensions, no missing textures or page errors. Images inspected. This exercises engine rendering and native object transforms; **it is not a native desktop editor UI test**.
- `roundtrip.json`, `applied-game.png`: ordinary CLI Apply rejects an invalid scene without changing source, applies the valid scene, and the actual game loads the changed map. House position 32,351, variant 1, door 41,354; old wall tile becomes passable and new wall tile becomes solid. State validation and save/load hash equality pass; original-layout save is rejected. Python generator correctly refuses to overwrite the applied layout. Finally, the ordinary importer restores the original map **byte-for-byte**. The user scene itself is not changed by the test.
- Typecheck/build, lint and docsync pass; Vite retains the existing large-bundle warning. Freshness retains the same 12 historical stale/unstamped findings. `final-check.txt` confirms unchanged v4 geometry after the final CRLF/LF-tolerant source-hash check. The in-game screenshot uses explicit engineer positioning only after state/save/collision assertions, to bring the tested house into view.
- Required broad `npm test` was run **before any temporary canonical map change**. It reached 221 reported tests: 148 pass and 73 fail, then was stopped after repeated existing failures/slow campaign work. `full-tests-partial.txt` preserves the output. Examples include pre-authored Foreman/survey/cul-de-sac fixtures, missing `hx` in discovery fixtures and legacy campaign expectations. The new importer was not used by those tests; the canonical runtime/map was unchanged throughout that run. **The full suite is not passing or complete.** This task does not repair those unrelated fixtures or close campaign/regression gates.

## Remaining limits

Native Phaser Editor Desktop was not available in the checked environment; opening/saving in its UI remains to be confirmed by the owner. Format/settings are checked against official example/model sources and engine rendering. The editor's background paths/props are a baseline reference, not a live regeneration while dragging. Broader terrain, road, tram/site, building-add/remove/resize or parcel authoring needs another importer extension. Human play, performance and release gates stay open.

Original Riverfront v4 source/layout was restored at the end. No commit, push or deployment.
