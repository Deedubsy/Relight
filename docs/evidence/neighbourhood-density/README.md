# CITY-E — neighbourhood density and Phaser plot authoring

Implemented 2026-09-10 after the owner accepted the focused neighbourhood recommendation. This pass uses the existing artwork. Human visual acceptance and broader gameplay/release gates remain open.

## Layout

The current map is `riverfront-arc-v4-editor-f72ebc665048`: 301 buildings, up from 287. The eight generic houses in the two blocks east of Founders Court are replaced by four shops, four north-facing homes, two service garages and eight mews homes. Four additional homes fill gaps around the Court. Eighteen solid garden details and four paved strips define the service alley, shop frontage and garden walks.

| 54 × 54 tile block | Before | After | Building footprint coverage |
| --- | --- | --- | --- |
| Shops / Market Lane, x126–180 / y324–378 | 4 buildings | 10 buildings | 13.7% → 32.0% |
| Founders Mews, x126–180 / y378–432 | 4 buildings | 8 buildings | 13.7% → 26.3% |

These percentages include roads in the block denominator. Factory yards intentionally remain open. Every retained building record is unchanged; all roads, tram geometry, factory yards, service drives, resources, campaign sites, power positions and progression bindings compare exactly with the pre-pass source. The rest of the city is unchanged.

- [Scene neighbourhood overview](scene-neighbourhood.png): actual Phaser rendering of the saved scene images, with plot overlays hidden for readability.
- [Shops in game](shops-day.png), [mews in game](mews-day.png), [court](court-day.png), [yard](yard-day.png).
- [Court at night](court-night.png), [shops at night](shops-night.png): unpowered fresh game, debug time and flashlight disabled; these do not claim powered illumination.
- [Authored output and full geometry validation](authored.json): no errors, 371 road edges and 371,093 accessible tiles.

The bounded compiler is `packages/tools/src/authorNeighbourhood.ts`. It reads the archived pre-pass source and refuses to overwrite subsequent source/scene edits. Native editor saving normalizes the scene, so a later compiler rerun requires explicit reconciliation. Continue ordinary authoring through the editor instead. The original Python full-city generator's editor-layout drift guard remains active.

## Editor workflow

The native Phaser Editor MCP connection saved the updated scene: 603 objects, comprising one terrain reference, 301 building Images and 301 plot Rectangles. The saved scene passes the importer with `changed: false`. The editor initially held the old scene in memory; it was synchronized and duplicate objects were removed before saving and checking the final count/unique labels.

The importer now supports adding/removing background buildings and editing `PLOT_<image label>` rectangles. New images receive stable simulation IDs, solid collision, boarded doors and a checked pavement approach. Existing buildings keep their size/archetype; new images can choose whole-tile dimensions and registered roof textures. Twenty-two interior/campaign buildings remain fixed. Invalid plots/transforms, orphan plots, missing protected objects and blocked routes fail before applying. Roads, props and gameplay content remain authored references. See [editor instructions](../../PHASER_EDITOR.md).

The current scene, manifest and source snapshot are a coherent new baseline; pre-pass backups are `before.scene`, `before-manifest.json` and `before-riverfront.ts.txt`. The older Tiled export is unchanged and predates CITY-E.

## Verification

- [12 focused tests](focused-tests.txt) pass: retained geometry, local density, walking/truck access, save identity, original four-direction/court behavior, new buildings, moved plots and invalid imports.
- [Three retained city/factory/save checks](retained-city-tests.txt) also pass, including the isolated regression evidence writer. Total focused verification: 15 passing checks.
- [Native-saved scene validation](editor-check.txt) passes; [project typecheck/production build](typecheck.txt) and [lint](lint.txt) pass.
- [Browser review](visual-review.json): inspected real game day/night views, held-key wall collision, walking into/out of Rowan's retained interior, roof transitions and browser save/reload. No page errors. Positions/time are explicitly debug setup in an isolated browser context; walking and saving use normal controls.
- [Apply/game/save/restore round trip](roundtrip.json): a moved plot beyond the former margin and an added replacement home reach the live game with correct collision and save behavior. Invalid Home movement is rejected without a source write. The exact final neighbourhood source is restored afterward.
- [Scene rendering](render.json): all 302 image objects load in Phaser with no asset failures or page errors. This rendering screenshot is separate from native editor UI capture.
- [Broad suite](full-tests-summary.json) is **partial, not a full pass**: stopped after 148 passes / 73 failures. Every failure name is present in the prior unchanged-map partial run. No broader regression repair is claimed. The city regression writer now uses a temporary output directory so repeated tests cannot overwrite historical CITY-C evidence.
- [Documentation synchronization](docsync.txt) passes. [Freshness](freshness.txt) retains 12 historical stale/unstamped findings. Referenced documentation paths pass; historical campaign evidence was not regenerated.

Reload the rebuilt preview at `http://127.0.0.1:5178/?view=world` and choose **New Game**. Live editor Play remains port 5190. Previous map saves are preserved and require their matching build. Nothing was committed, pushed or published.
