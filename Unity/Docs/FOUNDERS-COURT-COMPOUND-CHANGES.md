# Founders Court compound changes

Phase 3 Part B apply report. Plan: `Unity/Docs/FOUNDERS-COURT-PLAN.json` (regenerated 2026-09-18 after the go message: trees `neighbourhood:court-tree-b` and `court-east-cottage:garden-tree` now have fate delete; 75 delete / 20 keep; nothing else changed). Scene: `Unity/Relight/Assets/Relight/Scenes/World.unity`, edited only through the Unity CLI driving the open Editor, refreshed with `Relight/World/Refresh Editable World` and saved after the generator ran. Tile coordinates: x east, y south, pivot NW corner, rect = (x,y) w×h.

## 1. Decisions file

`Unity/Docs/FOUNDERS-COURT-DECISIONS.md`: D34–D44 appended verbatim to the decisions table, followed by the line "D3, D4 and D26 are superseded by D34, D36, D38 and D42. D16 is amended by D34."

## 2. Generator script

`Unity/Relight/Assets/Editor/FoundersCourtCompound.cs` — compiled clean (CLI `recompile` → `recompile_status` completed, compilationFailed false, no errors), twice: once as written, once after the C1 fix under heading 6.

Menu items:

- `Relight/Founders Court/Generate Compound` — reads the default plan, deletes every object whose name contains `[fc-`, deletes every plan fate `delete`, creates sidewalks (`SceneArea`), compound fence segments and side fences (`SceneProp` kind fence, blocksMovement 1), house copies (`SceneBuilding` built from the plan's copied fields) and stubs (`ScenePath` kind Path), moves the Foreman workshop and sets its door/roof from the plan, keeps the Home workshop in place, moves the Works Yard site and any node/substation whose plan rect differs, then runs Refresh Editable World and saves the open scene. Writes `Unity/Relight/Temp/fc_generate_log.txt`.
- `Relight/Founders Court/Verify` — compiles the Editable World and prints PASS/FAIL lines C1–C13, last line `Verify: N passed, M failed`. Writes `Unity/Relight/Temp/fc_verify_log.txt`.
- `Relight/Founders Court/Generate From File` — opens a file panel for a plan path, then runs the same generator on it.

The generator takes the block rectangle and road polyline from the plan (`block.rect`, `road.polyline`) and logs them (D43). It reads the plan with Newtonsoft.Json (`com.unity.nuget.newtonsoft-json`, already a resolved package dependency). Templates are not required to exist: copies are built from the plan's recorded fields (kind, size, variant, roof key, doors, enterable, campaign flag), so the command is rerunnable after the template buildings are gone.

## 3. Deleted objects

From the generator log. Earlier `fc-` objects removed first: 0 (first run). Plan fates: 75 deleted, 0 missing.

| # | Object | Key | Category | Was at |
|---|---|---|---|---|
| 1 | Salvage garages | home-garage] [home-garage | Buildings | (51,337) |
| 2 | court house | court-house] [court-house | Buildings | (49,382) |
| 3 | court garage | court-garage] [court-garage | Buildings | (87,335) |
| 4 | Court cottage | court-east-home] [court-east-home | Buildings | (87,410) |
| 5 | Maintenance garage | court-west-garage] [court-west-garage | Buildings | (49,344) |
| 6 | Court gardener’s home | court-northwest-home] [court-northwest-home | Buildings | (31,351) |
| 7 | Court brick home | court-west-home] [court-west-home | Buildings | (31,374) |
| 8 | Approach home | court-approach-home] [court-approach-home | Buildings | (49,404) |
| 9 | Court cottage | court-east-cottage] [court-east-cottage | Buildings | (85,382) |
| 10 | Garden Cottage | court-garden-home] [court-garden-home | Buildings | (31,393) |
| 11 | Court End home | court-southwest-home] [court-southwest-home | Buildings | (31,414) |
| 12 | North Court home | court-north-home] [court-north-home | Buildings | (31,333) |
| 13 | East Garden home | court-east-garden-home] [court-east-garden-home | Buildings | (101,414) |
| 14 | Perimeter home 1 | court-perimeter-1] [court-perimeter-1 | Buildings | (113,329) |
| 15 | Perimeter home 2 | court-perimeter-2] [court-perimeter-2 | Buildings | (113,339) |
| 16 | Perimeter home 3 | court-perimeter-3] [court-perimeter-3 | Buildings | (113,349) |
| 17 | Perimeter home 4 | court-perimeter-4] [court-perimeter-4 | Buildings | (113,359) |
| 18 | Perimeter home 5 | court-perimeter-5] [court-perimeter-5 | Buildings | (113,369) |
| 19 | Perimeter home 6 | court-perimeter-6] [court-perimeter-6 | Buildings | (110,385) |
| 20 | Perimeter home 7 | court-perimeter-7] [court-perimeter-7 | Buildings | (97,335) |
| 21 | Perimeter home 8 | court-perimeter-8] [court-perimeter-8 | Buildings | (31,360) |
| 22 | Perimeter home 9 | court-perimeter-9] [court-perimeter-9 | Buildings | (77,335) |
| 23 | Perimeter home 10 | court-perimeter-10] [court-perimeter-10 | Buildings | (102,404) |
| 24 | Perimeter home 11 | court-perimeter-11] [court-perimeter-11 | Buildings | (111,415) |
| 25 | Perimeter home 12 | court-perimeter-12] [court-perimeter-12 | Buildings | (77,415) |
| 26 | tree | court-east-cottage:garden-tree] [court-east-cottage:garden-tree | Props | (83,380) |
| 27 | tree | neighbourhood:court-tree-b] [neighbourhood:court-tree-b | Props | (44,410) |
| 28 | fence | neighbourhood:court-west-garden-edge] [neighbourhood:court-west-garden-edge | Props | (27,387) |
| 29 | fence | neighbourhood:court-west-garden-edge-2] [neighbourhood:court-west-garden-edge-2 | Props | (27,406) |
| 30 | Perimeter fence 1 | court-perimeter-fence-1] [court-perimeter-fence-1 | Props | (114,379) |
| 31 | Perimeter fence 2 | court-perimeter-fence-2] [court-perimeter-fence-2 | Props | (107,335) |
| 32 | Perimeter fence 3 | court-perimeter-fence-3] [court-perimeter-fence-3 | Props | (31,370) |
| 33 | Perimeter fence 4 | court-perimeter-fence-4] [court-perimeter-fence-4 | Props | (75,343) |
| 34 | Perimeter fence 5 | court-perimeter-fence-5] [court-perimeter-fence-5 | Props | (102,402) |
| 35 | Perimeter fence 6 | court-perimeter-fence-6] [court-perimeter-fence-6 | Props | (31,342) |
| 36 | Perimeter fence 7 | court-perimeter-fence-7] [court-perimeter-fence-7 | Props | (23,333) |
| 37 | Perimeter fence 8 | court-perimeter-fence-8] [court-perimeter-fence-8 | Props | (41,341) |
| 38 | Perimeter fence 9 | court-perimeter-fence-9] [court-perimeter-fence-9 | Props | (41,414) |
| 39 | Perimeter fence 10 | court-perimeter-fence-10] [court-perimeter-fence-10 | Props | (59,412) |
| 40 | Perimeter fence 11 | court-perimeter-fence-11] [court-perimeter-fence-11 | Props | (96,414) |
| 41 | Perimeter fence 12 | court-perimeter-fence-12] [court-perimeter-fence-12 | Props | (31,329) |
| 42 | Perimeter fence 13 | court-perimeter-fence-13] [court-perimeter-fence-13 | Props | (31,383) |
| 43 | Perimeter fence 14 | court-perimeter-fence-14] [court-perimeter-fence-14 | Props | (31,402) |
| 44 | Perimeter fence 15 | court-perimeter-fence-15] [court-perimeter-fence-15 | Props | (31,424) |
| 45 | Perimeter fence 16 | court-perimeter-fence-16] [court-perimeter-fence-16 | Props | (120,424) |
| 46 | Perimeter fence 17 | court-perimeter-fence-17] [court-perimeter-fence-17 | Props | (23,423) |
| 47 | Perimeter fence 18 | court-perimeter-fence-18] [court-perimeter-fence-18 | Props | (28,423) |
| 48 | Perimeter fence 19 | court-perimeter-fence-19] [court-perimeter-fence-19 | Props | (63,343) |
| 49 | Path 1 | — | Roads and paths | (70,361) |
| 50 | Path 2 | — | Roads and paths | (57,344) |
| 51 | Path 3 | — | Roads and paths | (103,402) |
| 52 | Path 4 | — | Roads and paths | (59,386) |
| 53 | Path 5 | — | Roads and paths | (92,344) |
| 54 | Path 26 | — | Roads and paths | (91,418) |
| 55 | Path 27 | — | Roads and paths | (52,351) |
| 56 | Path 39 | — | Roads and paths | (41,355) |
| 57 | Path 40 | — | Roads and paths | (41,378) |
| 58 | Path 41 | — | Roads and paths | (59,408) |
| 59 | Path 42 | — | Roads and paths | (84,385) |
| 60 | Path 61 | — | Roads and paths | (41,397) |
| 61 | Path 62 | — | Roads and paths | (36,424) |
| 62 | Path 63 | — | Roads and paths | (36,332) |
| 63 | Path 64 | — | Roads and paths | (106,424) |
| 64 | Path perimeter 1 | — | Roads and paths | (121,333) |
| 65 | Path perimeter 2 | — | Roads and paths | (121,343) |
| 66 | Path perimeter 3 | — | Roads and paths | (121,353) |
| 67 | Path perimeter 4 | — | Roads and paths | (121,363) |
| 68 | Path perimeter 5 | — | Roads and paths | (121,373) |
| 69 | Path perimeter 6 | — | Roads and paths | (119,389) |
| 70 | Path perimeter 7 | — | Roads and paths | (102,327) |
| 71 | Path perimeter 8 | — | Roads and paths | (21,364) |
| 72 | Path perimeter 9 | — | Roads and paths | (82,327) |
| 73 | Path perimeter 10 | — | Roads and paths | (111,408) |
| 74 | Path perimeter 11 | — | Roads and paths | (116,424) |
| 75 | Path perimeter 12 | — | Roads and paths | (82,424) |

## 4. Generated objects

29 objects, every name carrying an `fc-` bracket key.

| Object | Key | Type | Rect / points | Notes |
|---|---|---|---|---|
| Sidewalk west | `fc-sidewalk-west` | SceneArea (square) | (23,329) 2x99 | decor only |
| Sidewalk east | `fc-sidewalk-east` | SceneArea (square) | (120,329) 2x99 | decor only |
| Sidewalk north | `fc-sidewalk-north` | SceneArea (square) | (25,329) 95x2 | decor only |
| Sidewalk south | `fc-sidewalk-south-w` | SceneArea (square) | (25,426) 42x2 | decor only |
| Sidewalk south | `fc-sidewalk-south-e` | SceneArea (square) | (77,426) 43x2 | decor only |
| Compound fence west | `fc-fence-west-1` | SceneProp fence | (25,331) 1x95 | blocksMovement 1 |
| Compound fence east | `fc-fence-east-1` | SceneProp fence | (119,331) 1x95 | blocksMovement 1 |
| Compound fence north | `fc-fence-north-1` | SceneProp fence | (26,331) 93x1 | blocksMovement 1 |
| Compound fence south | `fc-fence-south-1` | SceneProp fence | (26,425) 41x1 | blocksMovement 1 |
| Compound fence south | `fc-fence-south-2` | SceneProp fence | (77,425) 42x1 | blocksMovement 1 |
| Side fence W1-W2 | `fc-sidefence-W1-W2` | SceneProp fence | (26,410) 37x1 | blocksMovement 1 |
| Side fence W2-W3 | `fc-sidefence-W2-W3` | SceneProp fence | (26,395) 37x1 | blocksMovement 1 |
| Side fence W3-CW | `fc-sidefence-W3-CW` | SceneProp fence | (26,379) 37x1 | blocksMovement 1 |
| Side fence E1-E2 | `fc-sidefence-E1-E2` | SceneProp fence | (81,410) 38x1 | blocksMovement 1 |
| Side fence E2-YARD | `fc-sidefence-E2-YARD` | SceneProp fence | (81,395) 38x1 | blocksMovement 1 |
| Side fence WS-CW | `fc-sidefence-WS-CW` | SceneProp fence | (63,332) 1x33 | blocksMovement 1 |
| Side fence WS-YARD | `fc-sidefence-WS-YARD` | SceneProp fence | (79,332) 1x33 | blocksMovement 1 |
| Salvage garages | `fc-house-W1` | SceneBuilding | (51,414) 12x7 | roof rf-garage-roof-2, door local (11,2) 1x3, copy of Salvage garages [home-garage] |
| Stub W1 | `fc-stub-W1` | ScenePath (Path) | (63,417)->(67,417) | door tile → first road tile |
| North Court home | `fc-house-W2` | SceneBuilding | (53,398) 10x9 | roof rf-house-E-roof-2, door local (9,3) 1x3, copy of North Court home [court-north-home] |
| Stub W2 | `fc-stub-W2` | ScenePath (Path) | (63,402)->(67,402) | door tile → first road tile |
| Court gardener’s home | `fc-house-W3` | SceneBuilding | (53,382) 10x9 | roof rf-house-E-roof-0, door local (9,3) 1x3, copy of Court gardener’s home [court-northwest-home] |
| Stub W3 | `fc-stub-W3` | ScenePath (Path) | (63,386)->(67,386) | door tile → first road tile |
| Court brick home | `fc-house-E1` | SceneBuilding | (81,413) 10x9 | roof rf-house-W-roof-1, door local (0,3) 1x3, copy of Court brick home [court-west-home] |
| Stub E1 | `fc-stub-E1` | ScenePath (Path) | (80,417)->(76,417) | door tile → first road tile |
| Stub E2 | `fc-stub-E2` | ScenePath (Path) | (80,402)->(76,402) | door tile → first road tile |
| Court End home | `fc-house-CW` | SceneBuilding | (53,369) 10x10 | roof rf-house-E-roof-1, door local (9,3) 1x3, copy of Court End home [court-southwest-home] |
| Stub CW | `fc-stub-CW` | ScenePath (Path) | (63,373)->(67,373) | door tile → first road tile |
| Stub WS | `fc-stub-WS` | ScenePath (Path) | (70,361)->(70,369) | door tile → first road tile |

## 5. Moved objects

| Object | Key | Old rect | New rect | Also changed |
|---|---|---|---|---|
| Foreman workshop | `foreman-shelter` | (98,393) 12×9 | (81,398) 12×9 | door local (4,8) 3×1 (south wall) → (0,3) 1×3 (west wall, facing the road); roof key unchanged rf-workshop-roof-0; its old stub Path 3 deleted, `fc-stub-E2` (80,402)→(76,402) created |
| Works Yard | `yard:0` | (83,350) 30×29 | (80,332) 39×63 | site only; paving follows the site rect |
| Works Yard Iron Scrap Pile | `opening-iron-v1` | (84,352) 3×4 | (84,352) 3×4 | not moved (plan rect equals old rect) |
| Works Yard Copper Cable Spool | `opening-copper-v1` | (96,352) 3×4 | (96,352) 3×4 | not moved |
| Works Yard Coal Fuel Bunker | `opening-coal-v1` | (96,369) 3×3 | (96,369) 3×3 | not moved |
| Substation 0 | `substation:0` | (93,350) 3×3 | (93,350) 3×3 | not moved |
| Home workshop | `home-workshop` | (65,347) 10×14 | (65,347) 10×14 | kept in place (D25); door (4,13) 3×1 and roof rf-workshop-roof-1 unchanged; old stub Path 1 deleted, `fc-stub-WS` (70,361)→(70,369) created |

## 6. Verify results

Final run (after the C1 fix below), full output:

```
PASS C1 compound closed: interior tiles reached with mouth solid = 0 (mouth x 67..76, y 425..427 blocked; start 71,435, start walkable True)
PASS C2 fence solid: 366 compound fence tiles, 0 not solid
PASS C3 no baseline solids: 1158 solid tiles in block, 0 outside any building/fence/substation
PASS C4 houses in lots: 7 houses; all inside their lot, no overlaps
PASS C5 setback 4: W1=4, W2=4, W3=4, E1=4, E2=4, CW=4, WS=8 (workshop exception, D25)
PASS C6 doors and stubs: 7 houses, each door on the front wall with one stub to the road
PASS C7 roof variety: 5 neighbour pairs, all roof keys differ
PASS C8 edge clear: 766 sidewalk/edge tiles, no building, prop or site on them
PASS C9 yard: yard (80,332) 39x63, 3 nodes, substation (93,350) 3x3; all inside, D9 gaps ok, substation touches copper
PASS C10 raid line: 1 raid line site(s): (69,432) 6x1; DirectorRules.RaidLineY = 432 (want y 432 x 69..74)
PASS C11 spawn: playerSpawn (70.5,360.5), compiled spawn tile (70,360), want (70.5,360.5)
PASS C12 plan match: 29 fc objects in scene, 29 planned, all rects match; kept houses at plan rects
PASS C13 walk steps: core -> mouth row 66 steps, + 10 = 76, cap EntryFarSteps = 76
Verify: 13 passed, 0 failed
```

First run: C1 failed (`interior tiles reached with mouth solid = 7857`), C2–C13 passed. Cause: the verifier blocked only the mouth's block-edge row (x 67..76, y 427). The sidewalk rows y 426..427 outside the fence are open and join the ring road, so the flood walked along the sidewalk to (67..76, 426) and through the fence-row gap at y 425. The scene was not touched; the generator's C1 was changed to block the road band from the fence row to the block edge (x 67..76, y 425..427), recompiled, and Verify rerun: 13 passed, 0 failed. One failure for C1, none for any other check.

## 7. Entry step cap

Cap unchanged at 76. C13: core → mouth row 66 steps, + 10 = 76 ≤ `EntryFarSteps` 76 (`Unity/Relight/Assets/Relight/Sim/Combat/Director/DirectorRules.cs:78`). `DirectorRules.cs` not edited.

## 8. Scene capture

`Unity/Docs/FOUNDERS-COURT-AFTER.png` — 2D Scene View, orthographic, pivot (72,−378), half-height 57 tiles, 1400×1400, taken after Refresh Editable World and save.

`Relight/World/Validate` does not exist (`Menu.GetEnabled` false; `ExecuteMenuItem` reports no such menu), so it was not run.

## 9. Temporary files

- `Unity/Relight/Assets/fc_after_capture.png` (scene-view capture, written by `capture_scene_view`) — copied to `Unity/Docs/FOUNDERS-COURT-AFTER.png`, then deleted with `AssetDatabase.DeleteAsset` (file and .meta gone).

Logs outside `Assets`, kept: `Unity/Relight/Temp/fc_generate_log.txt`, `Unity/Relight/Temp/fc_verify_log.txt` (git-ignored project Temp).

## 10. Gaps in this pass

Pre-apply commit: `3a707efa95cb440c75c40ee30dfe7ffebc1a2a63` ("Founders Court: before Phase 3 apply"). No further commit was made.

1. C1 mouth definition: the plan's mouth is the one-row band crossing at the block edge (y 427). Blocking only that row leaks via the open sidewalk, so C1 blocks x 67..76 for y 425..427 (fence row to block edge). Plan data unchanged.
2. Console: one error during Part B, produced by this pass's own probe: `ExecuteMenuItem failed because there is no menu named 'Relight/World/Validate'`. No errors from the generator, Verify, Refresh or save.
3. Foreman workshop door moved from the south wall to the west wall (plan doorsLocal for lot E2, D39/D44). No workshop roof art has a door-direction variant (only rf-workshop-roof-0/1 exist), so the roof key stays rf-workshop-roof-0 and the roof art still shows a south-facing door. No new art was added.
4. The plan keys `fc-house-E2` and `fc-house-WS` name the two kept buildings. Their GameObject names and ids stay `foreman-shelter` and `home-workshop` (campaign references), so they are not `fc-` objects; C12 checks their rects by name instead.
5. The sidewalks are `SceneArea` squares with areaName "Founders Court sidewalk"; the presenter uses that name for the paved GameObject only, and the capture shows no extra map label.
6. The capture still shows dark strips inside the lots (for example along y 346, y 366 and x 41 on the west side). They are baseline terrain decor, not scene objects; C3 confirms no baseline solids in the block. Not changed in this pass.
7. The generator's rerun path (delete every `fc-` object, then regenerate) is implemented but was not exercised on this scene after the first run; a rerun would log 75 `MISSING delete` lines for the objects already gone.
8. `Generate From File` prompts with an Editor file panel, so it is interactive; the CLI path is `Generate Compound` on the default plan.
9. `Unity/Docs/FOUNDERS-COURT-PLAN.md`, `.json` were regenerated for the tree change; `FOUNDERS-COURT-PLAN.png` was redrawn but is byte-identical (the drawing does not show trees).
