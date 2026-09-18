# Founders Court cleanup changes

## 1. Decisions file

`Unity/Docs/FOUNDERS-COURT-DECISIONS.md`. D45, D46 and D47 appended after D44 verbatim; the line "D39 is amended for the Foreman workshop by D45." added after the existing supersession line.

## 2. Baseline decor

Baseline asset: `Unity/Relight/Assets/Relight/World/Manual/SceneBaseGeometry.asset` (`WorldGeometryAsset`, 864 × 576, map `riverfront-arc-v4-editor-ac16d9188c05`). Counted and edited inside the Editor with `eval_file` (scripts `Unity/Relight/Temp/fc_inv.cs`, `fc_clear.cs`); the asset was never hand edited. Block rectangle (23,329) 99 × 99, x 23..121, y 329..427.

What the baseline actually contributes after `SceneWorld.Compile`: the `kind` and `solid` arrays, `roadNodes`, `tramSamples`, `court`/`courtRadius`/`riverY`. Compile replaces `props`, `roads`, `paths`, `drives`, `squares`, `serviceAreas` and `buildings` with the scene's own objects, so those baseline lists draw nothing in the current scene; they are cleared here because D46 says every entry and because the same lists seed a re-import.

| Field | What it draws | Total entries | Inside block (before) | Action |
|---|---|---|---|---|
| `kind` (byte per tile) | Tile class painted by `WorldPainter`: Street paving, Ground, Rubble, Inert, River, Deposit, Patch. The dark strips inside the lots were Street tiles rasterised from the old footpaths and the service drive. | 497,664 tiles | 9,801 tiles: Street 1,475, Ground 8,326, no Rubble/Inert/River/Deposit/Patch | 510 Street tiles set to Ground. 965 Street tiles kept as road-owned: the plan road band (67,369) 10 × 59, the circle (centre (72,374), r 5.5, tile-centre rule) and the four block-edge rows/columns x 23, x 121, y 329, y 427, which are the ring roads' pavement. |
| `variant` (byte per tile) | Nothing in Unity (no reader) | 497,664 | 9,801, all 0 | None |
| `patch` (byte per tile) | Ore patch type under Patch tiles | 497,664 | 9,801, all 0 | None |
| `solid` (byte per tile) | Starting collision grid for Compile | 1,412 solid tiles | 0 | None needed (confirmed 0) |
| `city.props` | Props (trees, fences, debris) | 370 | 7 overlapping | 7 removed: `court-brush`, `court-west-home:garden-tree`, `court-east-cottage:garden-tree`, `neighbourhood:court-tree-a`, `neighbourhood:court-tree-b`, `neighbourhood:court-west-garden-edge`, `neighbourhood:court-west-garden-edge-2`. The scene copies of the three D27/M10 keepers stay in the scene. |
| `city.roads` | Road pavement and carriageway polylines | 371 | 1 straddling: `(72,432)→(72,374)`, the court road | Left alone (rule 7: no road changes) |
| `city.paths` | Footpath polylines, 1.4 wide | 479 | 8 inside, 7 straddling | 8 removed; 7 split, keeping only the outside remnant: (106,428)→(106,429), (36,328)→(36,327), (36,428)→(36,429), (91,428)→(91,429), (72,328)→(72,327) twice, (122,402)→(123,402) |
| `city.drives` | Service drives, 4 wide with round caps | 4 | 1 straddling: `(126,382)→(117,382)→(117,344)` | Split, keeping (126,382)→(122,382) |
| `city.roadNodes` | Junction discs | 210 | 1: `72,374` (court head) | Left alone (road/node) |
| `city.tramSamples` | Tram boulevard centreline | 4,909 | 0 | None |
| `city.serviceAreas` | Service-area paving rectangles | 1 | 0 | None |
| `city.squares` | Paved squares | 113 | 1: `Court garden walk` (43,395) 4 × 25 | Removed (also D28) |
| `city.court`, `courtRadius`, `riverY`, `roadHalfWidth`, `roadPavement`, `tramLength`, `railHalfWidth` | Court bulb discs, river quad, widths | scalars | court (72,374) r 5.5 is inside | Left alone (road) |
| `buildings` | Building records (replaced by scene buildings on Compile) | 479 | 15 overlapping | Left alone: not decor, and lots are out of scope |

Result: inside the block the baseline now holds only Ground tiles plus the road-owned Street tiles, zero solids, no props, paths, drives, squares or service areas. Asset marked dirty and saved (`AssetDatabase.SaveAssets`), then `Relight/World/Refresh Editable World` and `save_scene`. Check capture: `Unity/Relight/Temp/fc_decor_check.png` (inspected: the dark strips along y 346, y 366, x 41, the east-lot fronts and the drive column are gone; the two kept trees, the debris, the band, the circle and the sidewalks are unchanged).

## 3. Generator changes

File: `Unity/Relight/Assets/Editor/FoundersCourtCompound.cs` (the only code file changed). Recompiled through the Editor twice; both compiles completed with no errors.

- **Method** `ClearBaseline(WorldGeometryAsset g, RectInt block, RectInt band, Vector2Int centre, float radius, bool dryRun)` (public static, before `Setback`). Inside the block it sets every tile whose class is not Ground/River to Ground, clears every `solid`, removes `props`/`squares`/`serviceAreas` overlapping the block, and cuts `paths`/`drives` so only their outside runs remain (a run shorter than two tiles is dropped). Road-owned tiles are skipped: the plan road band, the circle (tile centre strictly inside the radius) and the block's four edge rows/columns. `roads`, `roadNodes`, `tramSamples`, `court` and `riverY` are never touched. When it changed anything it calls `EditorUtility.SetDirty` and `AssetDatabase.SaveAssets`. It returns a text starting with `clear` (nothing to remove), `dirty` (dry run, something present) or `cleared and saved`.
- **Call site**: `Generate(string planPath)` calls it as step 0, after the Undo group is opened and before the fc- deletion, with `root.GetComponent<SceneWorld>().baseline`, `R(plan["block"]["rect"])`, `R(plan["road"]["bandRect"])`, `V(plan["road"]["circle"]["centre"])` and `plan["road"]["circle"]["radius"]`. Both menu items (`Generate Compound`, `Generate From File`) go through `Generate`, so both are covered. The log line is `Generate: baseline <report>`.
- **C14** after C13: `check("C14 baseline clear", report.StartsWith("clear"), report)` where `report` is `ClearBaseline(world.baseline, block, band, centre, radius, true)`.
- **Summary line**: `L("Verify: " + passed + " passed, " + failed + " failed of fourteen checks")`.
- **Stubs with any point count** (Step 4 item 7): stub creation now reads all `stub.points`; the ScenePath is placed at the first point with the remaining points relative to it. C12 compares the first and last planned point with the first and last scene point whatever the count.
- **C6** (Step 4 item 7): the stub test already used the first point (must be 4-adjacent to a door tile) and the last point (must lie in the band or the circle), so it accepts any point count; its pass text now says so. The door-wall test gained an optional per-house plan field `doorWall` (`"W"`, `"E"`, `"N"` or `"S"`, the wall itself); without it the wall is the one facing the road for the lot's side, as before. E2 has `doorWall: "S"` under D45. First Verify run after the change failed C6 because the new field was mapped through the lot-side table (fix 1 of 3 for C6, generator only); the mapping was corrected and the rerun passed.

## 4. Foreman workshop

Plan data (`Unity/Docs/FOUNDERS-COURT-PLAN.json`, house E2): `doorsLocal` `[[4,8,3,1]]`, `doorTiles` `[[85,406],[86,406],[87,406]]` (south wall row y 406), `doorWall` `"S"`, `roof` `rf-workshop-roof-0` (the plan heading 4 template roof; identical to the previous key, so no roof change), `stub` `fc-stub-E2` points `[[86,407],[86,407],[76,407]]`, `decision` `"D45 applied"`. `Unity/Docs/FOUNDERS-COURT-PLAN.md` heading 4 row E2 updated (door tiles, stub start/end/points, Fate "keep and move; D45 applied") and the stub sentence under the table gained the D45 exception. `Unity/Docs/FOUNDERS-COURT-PLAN.png` regenerated with `Unity/Relight/Temp/fc_draw.py` (drawer now draws stubs with any point count).

Generate Compound ran once after the plan change (log `Unity/Relight/Temp/fc_generate_log.txt`): baseline already clear; 29 earlier fc- objects removed and regenerated; the 75 "delete" fates report MISSING because Phase 3 already deleted them; Foreman workshop `[foreman-shelter]` kept at (81,398) 12×9, doors old (0,3) 1×3 → new (4,8) 3×1, roof rf-workshop-roof-0 → rf-workshop-roof-0; `Stub E2 [fc-stub-E2]` generated as (86,407)→(86,407)→(76,407); Refresh Editable World and scene save ran inside the generator.

Scene confirmation (Editor eval): Foreman rect (81,398) 12×9, roof `rf-workshop-roof-0`, one door (4,8) 3×1 = tiles x 85..87 on row 406, which is the south wall row; `Stub E2 [fc-stub-E2]` kind Path, three points (86,407)(86,407)(76,407), the last on the band edge x 76; scene not dirty after the save.

## 5. Verify

Second run, after the C6 mapping fix:

```
PASS C1 compound closed: interior tiles reached with mouth solid = 0 (mouth x 67..76, y 425..427 blocked; start 71,435, start walkable True)
PASS C2 fence solid: 366 compound fence tiles, 0 not solid
PASS C3 no baseline solids: 1158 solid tiles in block, 0 outside any building/fence/substation
PASS C4 houses in lots: 7 houses; all inside their lot, no overlaps
PASS C5 setback 4: W1=4, W2=4, W3=4, E1=4, E2=4, CW=4, WS=8 (workshop exception, D25)
PASS C6 doors and stubs: 7 houses, each door on its planned wall with one stub (first point at the door, last point on the road, any point count)
PASS C7 roof variety: 5 neighbour pairs, all roof keys differ
PASS C8 edge clear: 766 sidewalk/edge tiles, no building, prop or site on them
PASS C9 yard: yard (80,332) 39x63, 3 nodes, substation (93,350) 3x3; all inside, D9 gaps ok, substation touches copper
PASS C10 raid line: 1 raid line site(s): (69,432) 6x1; DirectorRules.RaidLineY = 432 (want y 432 x 69..74)
PASS C11 spawn: playerSpawn (70.5,360.5), compiled spawn tile (70,360), want (70.5,360.5)
PASS C12 plan match: 29 fc objects in scene, 29 planned, all rects match; kept houses at plan rects
PASS C13 walk steps: core -> mouth row 66 steps, + 10 = 76, cap EntryFarSteps = 76
PASS C14 baseline clear: clear, nothing to remove; block (23,329) 99x99: decor tiles 0, solids 0, props 0, paths 0, drives 0, squares 0, service areas 0 (roads, nodes, tram, court untouched)
Verify: 14 passed, 0 failed of fourteen checks
```

## 6. Capture

`Unity/Docs/FOUNDERS-COURT-AFTER-2.png` (Scene view, 2D, centre (72,378), size 62, 1600 × 1600; captured after Generate and Verify).

## 7. Temporary files

Deleted through `AssetDatabase.DeleteAsset`: `Assets/Temp/fc_decor_check.png`, `Assets/Temp/fc_after2.png` (both `capture_scene_view` outputs, copied out first) and the then-empty `Assets/Temp` folder that the capture command had created. `Unity/Relight/Temp/fc_decor_check.png` deleted. Left in the ignored `Unity/Relight/Temp/`: `fc_inv.cs`, `fc_clear.cs`, `fc_clear_dry.cs`, `fc_clear_live.cs`, `fc_inventory.txt`, `fc_clear_report.txt`, `fc_generate_log.txt`, `fc_verify_log.txt`, `fc_draw.py`, `fc_plan.py`.

Console: 0 errors, 0 warnings in the entries the Editor returns (last 100, all Log). Both recompiles reported `compilationFailed: false` with no errors.

## 8. Gaps in this pass

1. **Step 1 tree state.** The pass was ordered to create this report before checking the tree, so `git status --porcelain` at Step 1 showed exactly one line, ` M Unity/Docs/FOUNDERS-COURT-CLEANUP-CHANGES.md` (this file, recreated as the empty skeleton). Nothing else was modified; the pass continued.
2. **Foreman door tiles.** The workshop is 12 wide, so "three centre tiles" of its south wall is ambiguous (local x 4..6 or 5..7). Local x 4..6 (tiles 85..87) was used because it matches the workshop's original pre-Phase-3 door (local (4,8) 3×1). Change `doorsLocal`/`doorTiles`/stub start in the plan if 5..7 was meant.
3. **Zero-length first leg.** The tile directly south of the middle door tile is (86,407), and the tile one row below the south wall (row 406) is also on row 407, so the first leg has no length. The stub was written as the three points the step asked for, (86,407)→(86,407)→(76,407); on the ground it is a straight run along row 407 from below the door to the band edge.
4. **Roof key.** The "roof key before Phase 3" (plan heading 4 Template roof) is `rf-workshop-roof-0`, the same as the current key, so the roof did not change.
5. **Baseline column x 77.** The baseline had Street tiles at x 77, y 370..425 (the old rasteriser's inclusive road half-width). The plan defines the road band as x 67..76, so column 77 lies in the E lots' front strips and was cleared to Ground with the other decor tiles. If column 77 is to be read as road, widen `road.bandRect` in the plan and rerun Generate; nothing else needs changing.
6. **Block-edge rows and columns.** The four edge lines x 23, x 121, y 329, y 427 are entirely Street (ring-road pavement, per plan heading 9 for the east and south edges) and were kept as road-owned. They sit under the sidewalk SceneAreas.
7. **Kept scene props.** Three scene props remain inside the block by decision: `debris [court-brush]` (D27), `tree [court-west-home:garden-tree]` and `tree [neighbourhood:court-tree-a]` (M10). Their baseline `city.props` entries were removed under D46, so a future re-import from the baseline would not recreate them; the scene copies are unaffected.
8. **Baseline `buildings` records.** 15 baseline building records overlap the block. They are not decor and `SceneWorld.Compile` replaces them with the scene's buildings, so they were left alone.
9. **Generate MISSING lines.** Rerunning Generate lists 75 "MISSING delete" fates; these objects were deleted in Phase 3, so the lines are expected on every rerun.
10. **Scene diff size.** `World.unity` shows about 2,900 changed lines because the generator deletes and recreates the 29 fc objects with new fileIDs. A document-level comparison against HEAD found 92 changed YAML documents: the 29 fc objects (three documents each), the Foreman workshop's SceneBuilding component (door) and the parent transforms' child lists. Every other document is identical to HEAD; the four non-fc names that appear in the line diff are alignment artefacts.

Working tree after this pass (`git status --porcelain`, not committed):

```
 M Unity/Docs/FOUNDERS-COURT-CLEANUP-CHANGES.md
 M Unity/Docs/FOUNDERS-COURT-DECISIONS.md
 M Unity/Docs/FOUNDERS-COURT-PLAN.json
 M Unity/Docs/FOUNDERS-COURT-PLAN.md
 M Unity/Docs/FOUNDERS-COURT-PLAN.png
 M Unity/Relight/Assets/Editor/FoundersCourtCompound.cs
 M Unity/Relight/Assets/Relight/Scenes/World.unity
 M Unity/Relight/Assets/Relight/World/Manual/SceneBaseGeometry.asset
?? Unity/Docs/FOUNDERS-COURT-AFTER-2.png
```

