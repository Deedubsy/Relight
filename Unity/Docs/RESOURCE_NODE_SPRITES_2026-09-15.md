# Mining node sprites — 2026-09-15

Implemented the owner-approved flat, near-overhead painted resource style in Unity.

## Delivered

- Six transparent PNG sheets: iron ore, copper ore, coal, stone, crude oil and rubble.
- Three natural silhouettes per type, each with full, partly mined and nearly exhausted artwork: 54 imported sprites.
- Assets: `Assets/Relight/Art/ResourceNodes/`; catalogue: `Assets/Relight/Resources/ResourceNodes.asset`.
- The current authored map draws 105 resource tiles across its 11 deposits. Rubble artwork is available for mineable rubble and legacy direct-metal deposits.
- Deterministic tile variation. Above two-thirds remaining uses full art; above one-third uses partial art; the remaining positive amount uses sparse art; zero hides the sprite.
- Both manual mining and machine extraction use the existing remaining-resource state. No recipe, resource quantity, collision or save-schema changes.
- The ground beneath illustrated resources is ordinary terrain; the old coloured resource rectangle is replaced. Resource sprites sit above paving and below machines.
- Scene view shows the full deposit. Sprites belong to their editable SceneSite, so selecting, moving, resizing and changing the resource uses the existing authored-world workflow. Play Mode shows depletion from the current session or loaded save.

## Import and maintenance

Generated with the built-in image_gen tool. Original generated files were preserved and copied into the project; no command-line image generation fallback or programmatic image retouching was used. Exact prompts are in [production-prompts.json](art/mining-nodes/production-prompts.json). The previously approved concept is in [the art folder](art/mining-nodes/README.md).

Each source is 1254 × 1254 RGBA. Unity Sprite Editor data providers sliced three 418-pixel columns and three rows separated at transparent gutters. Row boundaries vary by sheet to avoid cutting artwork. Sprites use centred pivots, Full Rect meshes, 418 PPU, bilinear filtering, no mipmaps, no compression and no NPOT rescaling. Uniform world scaling preserves proportions across rows.

Reimport scripts are in [evidence/resource-nodes](evidence/resource-nodes/). They check sprite-edit capabilities, retain existing sprite IDs by name and update name/file-ID mappings. Run imports before connect.cs when rebuilding the catalogue. Do not modify metadata or scene YAML manually.

## Verification

- Unity 6000.6.0f1 compiled without errors.
- 18/18 Edit Mode tests passed: nine catalogue/depletion cases and nine existing scene-authoring cases. [Results](evidence/resource-nodes/editor-tests.json).
- Live Scene view: resource sprite parenting, movement and Undo passed; original placement restored and scene saved.
- 11 live Play Mode assertions passed. A real MineCommand and MiningPhase extracted 78 coal units, switching at 27 and 53 units removed, then hiding the exhausted tile. Adjacent tile stayed full, inventory conservation passed, and actual in-memory save adoption retained the cleared sprite. [Results](evidence/resource-nodes/play-checks.txt).
- Inspected [Home in Scene view](evidence/resource-nodes/scene-home.png), [Home after mining in Play Mode](evidence/resource-nodes/game-home.png), and [all 54 imported sprites rendered in Unity](evidence/resource-nodes/imported-gallery.png).
- The first live test accidentally selected a renderer awaiting Unity's end-of-frame destruction after StartSession. The corrected helper targets the newly built renderer; no production workaround was required.
- Inspection used a temporary paused session, set the engineer in mining reach, and advanced the mining phase directly. Autosaves were disabled; no player save files were read or written. Temporary gallery objects disappeared on leaving Play Mode.
- Standalone build and manual player acceptance were not run. Full simulation suite was not repeated because gameplay rules were unchanged. C-ACC remains human.

Unity is left in Edit Mode with the existing editable World scene and the Home resource area visible.

