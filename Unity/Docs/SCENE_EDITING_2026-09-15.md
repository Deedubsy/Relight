# Editable Unity world — 2026-09-15

The owner approved moving the map into Scene editing, with edits reflected in the existing tile-based simulation. Implemented in `Assets/Relight/Scenes/World.unity`. Original imported geometry, sites and Phaser sources are preserved.

## Start editing

Open **World.unity**, stay out of Play Mode, and use **Relight > World > Focus Home**. Expand **Editable World** in the Hierarchy. The scene contains 479 buildings, 370 props, 72 site/resource objects, road/path objects and paved areas. Normal Move, Duplicate, Delete, enable/disable and Undo operations apply. Save with Ctrl+S.

The display refreshes after edits, normally within about a second. Clicking a building, prop, road or paved area selects its persistent authoring object. Authored objects and edits are saved in the scene; generated artwork is transient and regenerated automatically. **Refresh Editable World** rebuilds manually if needed.

One Unity unit equals one tile. Use whole-number Position values and grid snapping. Keep Rotation at zero and Scale at one; resize through **Size** in the component. The object pivot is its north-west corner. Unity Y decreases southwards. Door rectangles and path points use local tile coordinates with Y increasing southwards.

## Buildings and interiors

Use **Relight > World > Add Enterable Building**, or duplicate a building. New buildings appear at the Scene view centre. Scene Building supplies Size, Enterable, Doors, Kind and visual variant. The initial new building is 8x8 with a two-tile south doorway. Its one-tile perimeter blocks movement; its interior and doorway are clear.

Use **Toggle Roofs in Scene** to furnish interiors. Runtime roofs still fade when the player enters. Enterable buildings now render actual wall strips and openings instead of an opaque wall rectangle covering the floor.

Use **Add Solid Prop**, then move it inside. **Blocks Movement** on Scene Prop controls whether its footprint is solid. Kind chooses an existing visual: `container`, `furniture`, `tree`, `rock`, `fence`, `gate`, `tank`, `statue`, `crane`, or `debris`. Size sets its footprint. Parent props under a building if they should move with it. Imported interior props were parented this way; Home's core and spawn follow Home.

Imported internal collision not represented by a complete prop is preserved as **Interior collision** children. These have an editable BoxCollider2D plus **Blocks Movement** component. Use that component for your own objects beneath Editable World without making a prefab. It currently supports axis-aligned boxes and conservatively covers touched cells. Use separate boxes for separate walls and leave doors clear. PolygonCollider2D and rotated collision shapes are not implemented. Selected objects show footprint gizmos.

Building and site IDs are stable gameplay identifiers. Duplicates receive new IDs automatically in the editor. Avoid changing IDs or deleting campaign buildings/sites unless intentionally changing the campaign; this pass does not redesign those objectives.

## Resources, roads and terrain

Move a resource under **Sites and resources** to move the actual mineable area. Size changes its extent; Item uses keys such as `ironore`, `copperore`, `coal`, `stone`, or `crude`; Amount is the deposit total. **Add Iron Deposit** creates a 3x3 deposit with 900 units. Invalid resource keys, blocked deposits and invalid object transforms produce validation errors.

Roads and paths have editable local Points; moving the object moves the whole polyline. Paved areas have Size. These are surface artwork: moving them does not move terrain classifications underneath. Paint terrain edits where terrain behaviour should change too. The retained tram route, river background and court shape remain baseline data rather than draggable route editors.

Open **Window > 2D > Tile Palette**, select **Relight Terrain**, and set Active Target to **Terrain Edits - paint here** under **Editable World > Terrain editing**. The seven tiles are Street, Ground, Rubble, Inert, River, Deposit and Patch (left to right). Prefer resource objects for ores rather than a generic Patch tile without a resource type. Use buildings, props or Blocks Movement for solid obstacles.

Terrain edits are a sparse overlay on the isolated **SceneBaseGeometry** asset. Painting changes the terrain used by the game; erasing an edit restores the original terrain at that cell. This avoids serializing approximately 500,000 baseline cells into the scene. World.unity is approximately 2.7 MB. Existing procedural map artwork remains unchanged.

## Running and saves

Press Play from World or choose **New Game** through the normal menu to use the authored layout. Compilation runs at session start. Runtime map changes are not written back to authoring; stop Play before permanent edits.

Authored saves include a hash of terrain, collision, spawn and sites in the map ID. Changing these rejects saves from the previous layout with a readable reason. Restore the old layout to use those saves, or choose New Game. Pure visual changes need not change the save identity.

Original pre-authoring saves remain supported: loading selects the original imported layout and its original opening resource/balance version. Authored saves select their matching layout. Foreign maps are refused before replacing the session. No existing player save files were modified during this task.

## Implementation and verification

Runtime authoring components are in `Assets/Relight/World/Authoring/`. Editor import, lifecycle, selection and menus are in `Assets/Relight/Editor/World/SceneWorldEditor.cs`. WorldBootstrap and AutosaveController select the matching runtime layout. CityPresenter renders wall rings; CitySprites releases resources during repeated editor rebuilds. The isolated baseline and native palette are under `Assets/Relight/World/Manual/`.

- [Baseline comparison](evidence/scene-authoring/baseline-comparison.txt): 479 buildings, 370 props, 72 sites; **zero terrain, resource-patch or collision-cell differences** from the approved ore-opening map.
- [Authoring tests](evidence/scene-authoring/authoring-tests.json): **9/9 passed**, covering walls/doors, movement/deletion, disabled objects, resources, box bounds, invalid layouts, terrain paint/erase and save identity.
- [Simulation regressions](evidence/scene-authoring/sim-tests.json): **596/596 passed**.
- [Live Play checks](evidence/scene-authoring/play.txt): **13 passed**, including ordinary WalkCommand movement through a new doorway, stopping at walls/props, original/authored save adoption and foreign-map refusal without replacing the session.
- [Save/reopen](evidence/scene-authoring/reopen.txt): layout identity preserved; display restored with a clean scene.
- [Move/Undo and cleanup](evidence/scene-authoring/finish.txt): preview follows prop edits; Undo restores position/identity; temporary objects removed; 479 buildings saved.
- [Scene view](evidence/scene-authoring/scene.png) and [temporary interior with roofs hidden](evidence/scene-authoring/interior.png) were visually inspected. The test building was removed afterwards.

Movement verification used the actual simulation in the running Editor, not native keyboard input. No standalone build, full campaign playthrough or human acceptance is claimed. C-ACC remains human.

Final scoped whitespace checks passed. Repository documentation sync/freshness scripts could not start because the existing checkout lacks `tsx`; no dependencies were installed.

[Final editor state](evidence/scene-authoring/final-state.txt): Edit Mode, World loaded, 479 buildings, scene clean, no preview error. [Preview lifecycle check](evidence/scene-authoring/lifecycle.txt) also verifies generated-resource stability, runtime terrain visibility and native palette metadata.
