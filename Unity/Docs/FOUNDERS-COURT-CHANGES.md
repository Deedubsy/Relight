# Founders Court changes

## 1. Decisions file

- Decisions file: `Unity/Docs/FOUNDERS-COURT-DECISIONS.md` (first line `# Founders Court decisions`, then the confirmed table D1–D28 copied verbatim).
- Design doc link: not made. `Unity/Docs` holds two files whose names contain "design" (`GAME_DESIGN.md`, `FREIGHT_STRONGHOLD_DESIGN_2026-09-18.md`), so the single-file rule did not apply. Recorded under heading 14.

## 2. Gap check tool

- Script: `Unity/Relight/Assets/Editor/FoundersCourtChecks.cs`, menu item `Relight/Founders Court Gap Check`. Constants at the top of the script: block x 23..121, y 329..427; bands west x 23..41, north y 329..360, east x 97..121, south y 404..427; mouth x 67..76 on the south edge (assessment headings 2 and 5). It compiles the world root through `SceneWorld.Compile`, builds the coverage set from every `SceneBuilding` footprint and every `SceneProp` of kind `fence` with `blocksMovement` 1, and reports gaps by band projection per edge. The Editor recompiled it with no errors.
- The report below was produced before any scene change (run through `unity command eval` calling the same `Report()` the menu item logs). Note: the two fences at x 27 still had `blocksMovement` 0 at this point, so by D16/D26 they are not yet in the coverage set; that merges the assessment's west gaps around them.

Before:

```
GAP West 329 332 4
GAP West 342 350 9
GAP West 360 373 14
GAP West 383 392 10
GAP West 402 413 12
GAP West 424 427 4
GAP North 23 30 8
GAP North 41 50 10
GAP North 63 64 2
GAP North 75 86 12
GAP North 97 121 25
GAP East 329 392 64
GAP East 402 413 12
GAP East 424 427 4
GAP South 23 30 8
GAP South 41 48 8
GAP South 59 66 8
GAP South 77 86 10
GAP South 96 100 5
GAP South 111 121 11
Mouth excluded at south edge x 67..76, y 427
Total gaps: 20
Solid tiles in block: 1237
Baseline solids in block: 54
Baseline solid tiles:
29,372
30,372
29,373
30,373
83,380
84,380
83,381
84,381
111,383
112,383
113,383
111,384
112,384
113,384
27,387
27,388
44,388
45,388
27,389
44,389
45,389
27,390
27,391
27,392
27,393
27,394
27,395
27,396
27,397
27,398
27,399
27,400
27,406
27,407
27,408
27,409
27,410
44,410
45,410
27,411
44,411
45,411
27,412
27,413
27,414
27,415
27,416
27,417
27,418
27,419
27,420
27,421
27,422
27,423
```

## 3. Baseline solids

- Baseline asset: `Unity/Relight/Assets/Relight/World/Manual/SceneBaseGeometry.asset` (`WorldGeometryAsset`, 864 × 576). Storage: a flat `byte[]` `Solid` indexed by tile, `Index(x, y) = y * width + x`, so the "grid or array indexed by tile" path applied. Every entry inside the block rectangle (x 23..121, y 329..427) was set to 0 through `unity command eval` on the live asset, then `EditorUtility.SetDirty` and `AssetDatabase.SaveAssets`. `SceneWorld.Compile` was not changed.
- Baseline entries set inside the block: 54 before, 0 after.
- Gap check after the clear, "Baseline solids in block:" line: `Baseline solids in block: 0` (the "Baseline solid tiles:" list is empty).
- Solid tiles in block (compiled grid): old 1237, new 1183 (before the fences of heading 4 were made to block).
- Note: `git` reports this asset as untracked (`??`), which predates this pass; the file is saved on disk either way.

## 4. Fences

- `fence [neighbourhood:court-west-garden-edge]` (27,387, 1 × 14): `blocksMovement` 0 → 1.
- `fence [neighbourhood:court-west-garden-edge-2]` (27,406, 1 × 18): `blocksMovement` 0 → 1.
- Set through `unity command eval` on the two `SceneProp` components, then `Relight/World/Refresh Editable World` and `save_scene`.
- Gap check after the change: both fences' tiles are set in the compiled `Solid` grid. Fence tiles solid: 32 of 32 for these two fences (the same compile shows 74 of 74 for every blocking fence prop under the world root, the other 42 lying outside the block). Solid tiles in block: 1215. Total gaps: 21 (the fences now split the west gaps at 383..386 and 402..405 and the south gaps at 23..26 and 28..30, matching the assessment list).

## 5. Raid line

- `Opening raid approach [opening:raid-line]` (SceneSite, kind RaidLine): position (69,391) → (69,432), size 6 × 1 (unchanged), i.e. tiles x 69..74 on row y 432, the south ring road centre line at the mouth. Set through `unity command eval`, then `Relight/World/Refresh Editable World` and `save_scene`.
- Compiled site record after the change: `opening:raid-line x=69 y=432 w=6 h=1`. It is the only RaidLine site the compile produces, and `DirectorRules.RaidLineY` takes the largest RaidLine Y (`DirectorRules.cs:64`).
- RaidLineY: 432

## 6. Works Yard

- `Factory yard 0 [yard:0]` renamed to `Works Yard [yard:0]` (GameObject name only; the `SceneSite.siteName` field still reads "Factory yard 0", kind Yard, paving kept).
- Yard rect check: position (83,350), size 30 × 29, tiles x 83..112, y 350..378. Inside the block (x 23..121, y 329..427): yes. Overlaps the court road band (x 67..76 from y 369 south, bulb around (72,374)): no. Centre (98, 364.5) is 27.8 tiles from the head (72,374) and 72.4 tiles from the mouth (72,432): closer to the head. All three hold, so the yard, the nodes and the substation were not offset.
- Node renames (GameObject names only, bracket keys kept): `Iron ore deposit [opening-iron-v1]` → `Works Yard Iron Scrap Pile [opening-iron-v1]`; `Copper ore deposit [opening-copper-v1]` → `Works Yard Copper Cable Spool [opening-copper-v1]`; `Coal deposit [opening-coal-v1]` → `Works Yard Coal Fuel Bunker [opening-coal-v1]`.
- Substation `Substation 0 [substation:0]` moved (78,347) → (93,350), size 3 × 3 (tiles x 93..95, y 350..352). Its east edge touches the copper node's west edge (shared edge tiles (95,352) and (96,352)); it lies inside the yard rect; it overlaps no node, building, road or path. Of the positions that satisfy those conditions it is the closest to the old position (15.3 tiles).
- Positions after the step: Works Yard (83,350) 30 × 29; iron node (84,352) 3 × 4; copper node (96,352) 3 × 4; coal node (96,369) 3 × 3; substation (93,350) 3 × 3.
- Node sprites and kinds unchanged (kinds Resource, items ironore 7680 / copperore 1200 / coal 700, sizes unchanged).

## 7. Node spacing

Axis gaps measured as the difference between nearest footprint edge tiles, the same method as assessment heading 4. Workshop door tiles: (69..71, 360).

- IRON (84..86, 352..355) → door tiles: [13 east, 5 south] = 13.9 tiles.
- COPPER (96..98, 352..355) → door tiles: [25 east, 5 south] = 25.5 tiles.
- COAL (96..98, 369..371) → door tiles: [25 east, 9 south] = 26.6 tiles.
- IRON – COPPER: [10, 0] = 10.0 tiles.
- IRON – COAL: [10, 14] = 17.2 tiles.
- COPPER – COAL: [0, 14] = 14.0 tiles.
- All nodes are ≥ 8 from the door tiles and all pairs are ≥ 4 apart. D9 satisfied, no change.

## 8. Interior buildings

Interior buildings (footprint touches no perimeter band): `court house [court-house]` (49,382, 10 × 9) and `Court cottage [court-east-cottage]` (85,382, 9 × 7). `Maintenance garage [court-west-garage]` touched the north band (y 358..364 reaches into y ≤ 360) but is moved anyway under D22.

- `Maintenance garage [court-west-garage]`: (51,358) → (49,344), size 7 × 7 unchanged (tiles x 49..55, y 344..350). Its top row shares edge tiles (51..55, 344)/(51..55, 343) with `Salvage garages [home-garage]` (51..62, 337..343); it overlaps nothing, leaves the Salvage garages' door (56..58, 343) and stub `Path 2` clear, and is the closest such position to the old one (14.1 tiles). Stub `Path 27`: (54,365)→(72,365)→(72,371) changed to (52,351)→(52,365)→(72,365)→(72,371), starting under the garage door (51..53, 350) and ending at the court road band as before.
- `court house [court-house]`: the wall nearest the court road band is the east wall (9 tiles from x 67; the band starts at y 369 so the south wall is not nearer). Door moved from the south wall (4,8,3,1) to the east wall (9,3,1,3); roof key `rf-house-roof-1` → `rf-house-E-roof-1` (variant 1, already used in the scene). Stub `Path 4`: (54,391)→(69,391) changed to (59,386)→(69,386), from the new door (58, 385..387) to the court road band. No other stub touched the building.
- `Court cottage [court-east-cottage]`: door (0,2,1,3) on the west wall, the wall facing the court road; roof `rf-house-W-roof-0`; stub `Path 42` (84,385)→(75,385) already runs from the door to the band. No change.
- All interior doors face the court road.
- Set through `unity command eval`, then `Relight/World/Refresh Editable World` and `save_scene`.

## 9. Perimeter

Template building: `Court brick home [court-west-home]` (id `court-west-home`, kind house, 10×9, `enterable` false, variant 1, roof `rf-house-E-roof-1`, door local (9,3,1,3)). Template fence: `fence [neighbourhood:court-west-garden-edge]` (kind `fence`, `blocksMovement` 1). Template stub: `Path 40` (kind Path). All new objects were made by `Object.Instantiate` of the template under the same parent; the duplicated display children (the template's four sprite children, the stub's one) were destroyed on the copy before renaming, and `Relight/World/Refresh Editable World` regenerated display parts for the copies. No object under the world root carries a template-key name other than the template itself and its own `tree [court-west-home:garden-tree]`. No two objects share an `id` (checked across every `SceneBuilding` and `SceneProp` under the root).

Gaps were filled longest first from the heading 4 "After" list (21 gaps). Perimeter homes are non-enterable, variant 1, kind house; each has one door on the wall facing the ring road and a stub path from the door tile to that road. Fences are `blocksMovement` 1.

| Gap (edge, span, length) | Placed | Position, size | Door (local) | Roof | Stub |
| --- | --- | --- | --- | --- | --- |
| East 329..392 (64) | Perimeter home 1..5 `[court-perimeter-1..5]` | (113,329) (113,339) (113,349) (113,359) (113,369), each 8×10 | (7,3,1,3) east wall | rf-house-E-roof-1 | Path perimeter 1..5: (121,333)→(123,333), (121,343)→(123,343), (121,353)→(123,353), (121,363)→(123,363), (121,373)→(123,373) |
| East 329..392 (cont.) | Perimeter home 6 `[court-perimeter-6]` | (110,385) 9×10 | (8,3,1,3) east wall | rf-house-E-roof-1 | Path perimeter 6: (119,389)→(123,389) |
| East 329..392 (cont.) | Perimeter fence 1 `[court-perimeter-fence-1]` | (114,379) 1×6 | | | touches home 5 (y 378) and home 6 (y 385) |
| North 97..121 (25) | Perimeter home 7 `[court-perimeter-7]` | (97,335) 10×9 | (4,0,3,1) north wall | rf-house-N-roof-1 | Path perimeter 7: (102,327)→(102,334) |
| North 97..121 (cont.) | Perimeter fence 2 `[court-perimeter-fence-2]` | (107,335) 6×1 | | | touches home 7 (x 106) and home 1 (x 113) |
| West 360..373 (14) | Perimeter home 8 `[court-perimeter-8]` | (31,360) 9×10 | (0,3,1,3) west wall | rf-house-W-roof-1 | Path perimeter 8: (21,364)→(30,364) |
| West 360..373 (cont.) | Perimeter fence 3 `[court-perimeter-fence-3]` | (31,370) 1×4 | | | touches home 8 (y 369) and Court brick home (y 374) |
| North 75..86 (12) | Perimeter home 9 `[court-perimeter-9]` | (77,335) 10×9 | (4,0,3,1) north wall | rf-house-N-roof-1 | Path perimeter 9: (82,327)→(82,334) |
| North 75..86 (cont.) | Perimeter fence 4 `[court-perimeter-fence-4]` | (75,343) 2×1 | | | touches home 9 (x 77); the Home workshop end is not touched (see heading 14) |
| East 402..413 (12) | Perimeter home 10 `[court-perimeter-10]` | (102,404) 9×10 | (8,3,1,3) east wall | rf-house-E-roof-1 | Path perimeter 10: (111,408)→(123,408) |
| East 402..413 (cont.) | Perimeter fence 5 `[court-perimeter-fence-5]` | (102,402) 1×2 | | | touches Foreman workshop (y 401) and home 10 (y 404) |
| South 111..121 (11) | Perimeter home 11 `[court-perimeter-11]` | (111,415) 10×9 | (4,8,3,1) south wall | rf-house-roof-1 | Path perimeter 11: (116,424)→(116,429) |
| South 77..86 (10) | Perimeter home 12 `[court-perimeter-12]` | (77,415) 10×9 | (4,8,3,1) south wall | rf-house-roof-1 | Path perimeter 12: (82,424)→(82,429) |
| West 342..350 (9) | Perimeter fence 6 `[court-perimeter-fence-6]` | (31,342) 1×9 | | | North Court home (y 341) and Court gardener's home (y 351) |
| North 23..30 (8) | Perimeter fence 7 `[court-perimeter-fence-7]` | (23,333) 8×1 | | | North Court home (x 31); west end at the block edge, no building |
| North 41..48 (8) | Perimeter fence 8 `[court-perimeter-fence-8]` | (41,341) 8×1 | | | North Court home (x 40); Maintenance garage end not touched (heading 14) |
| South 41..48 (8) | Perimeter fence 9 `[court-perimeter-fence-9]` | (41,414) 8×1 | | | Court End home (x 40); Approach home end not touched (heading 14) |
| South 59..66 (8) | Perimeter fence 10 `[court-perimeter-fence-10]` | (59,412) 8×1 | | | Approach home (x 58); east end at the mouth |
| South 96..100 (5) | Perimeter fence 11 `[court-perimeter-fence-11]` | (96,414) 5×1 | | | Court cottage (x 95) and East Garden home (x 101) |
| West 329..332 (4) | Perimeter fence 12 `[court-perimeter-fence-12]` | (31,329) 1×4 | | | North Court home (y 333); north end at the block edge |
| West 383..386 (4) | Perimeter fence 13 `[court-perimeter-fence-13]` | (31,383) 1×4 | | | Court brick home (y 382); south end meets the x-27 fence span, not a building (heading 14) |
| West 402..405 (4) | Perimeter fence 14 `[court-perimeter-fence-14]` | (31,402) 1×4 | | | Garden Cottage (y 401); south end meets the x-27 fence span (heading 14) |
| West 424..426 (3) | Perimeter fence 15 `[court-perimeter-fence-15]` | (31,424) 1×3 | | | Court End home (y 423); south end at the block edge |
| East 424..426 (3) | Perimeter fence 16 `[court-perimeter-fence-16]` | (120,424) 1×3 | | | home 11 (x 120, y 423); south end at the block edge |
| South 23..26 (4) | Perimeter fence 17 `[court-perimeter-fence-17]` | (23,423) 4×1 | | | no building at either end (heading 14) |
| South 28..30 (3) | Perimeter fence 18 `[court-perimeter-fence-18]` | (28,423) 3×1 | | | Court End home (x 31); west end meets the x-27 fence |
| North 63..64 (2) | Perimeter fence 19 `[court-perimeter-fence-19]` | (63,343) 2×1 | | | Salvage garages (x 62); Home workshop end not touched (heading 14) |

Placement constraints checked in the Editor after the refresh: no new footprint overlaps any building, prop, site or path segment anywhere in the scene (0 overlaps); nothing is on the mouth (x 67..76), on the court-road band, on any ring-road band (x ≤22, x ≥121, y ≤328, y ≥427), in the Works Yard rect, or on any path.

Counts:

| | Count |
| --- | --- |
| Buildings added | 12 |
| Fences added | 19 |
| Stub paths added | 12 |
| Touching pairs | 12 |
| Fenced pairs | 6 |

Touching pairs (share ≥1 edge tile): homes 1–2, 2–3, 3–4, 4–5; home 6 – Foreman workshop; home 7 – court garage; home 9 – court garage; home 8 – Court gardener's home; home 10 – East Garden home; home 11 – East Garden home; home 12 – Court cottage [court-east-home]; Maintenance garage – Salvage garages. Fenced pairs (fence touching both): home 5 – home 6 (fence 1); home 1 – home 7 (fence 2); home 8 – Court brick home (fence 3); Foreman workshop – home 10 (fence 5); North Court home – Court gardener's home (fence 6); Court cottage – East Garden home (fence 11). Touching 12 ≥ 2 × fenced 6 = 12, so no fenced gap was replaced by a building.

The gap check was rerun after the refresh and save. The four remaining reported "gaps" are the single coordinates x 121 (north and south edges) and y 427 (west and east edges): these tiles lie inside the ring-road bands (east road tiles 121..130, south road tiles 427..436), item 6 forbids placing on a road band, and the Solid grid already reports them solid through the road; see heading 14.

After:

```
GAP West 427 427 1
GAP North 121 121 1
GAP East 427 427 1
GAP South 121 121 1
Mouth excluded at south edge x 67..76, y 427
Total gaps: 4
Solid tiles in block: 2338
Baseline solids in block: 0
Baseline solid tiles:
```

## 10. Removed objects

Removed before the perimeter placements (both are D28 removals; they were taken first because `Drive 1` (x 115..119, y 344..384, 4 wide) crossed the east-column home positions and `Court garden walk` (x 43..46, y 395..419) crossed the west fence positions, and item 6 forbids placing over a path):

- `Roads and paths/Drive 1`, position (126,382), points (0,0) (−9,0) (−9,−38). Removed with `Undo.DestroyObjectImmediate`.
- `Paved areas/Court garden walk`, position (43,395). Removed with `Undo.DestroyObjectImmediate`.

Per D28 the two lights and the label were kept. Both removals were re-confirmed absent after the final refresh and save.

## 11. Entry step cap

Walk steps to the mouth: **66**. Method: `SceneWorld.Compile` in the Editor, BFS as `RaidField.Build` does it, seeded from the walkable ring around the core rect (core site `home-workshop`, rect (65,347) 10×14; 52 seed tiles at distance 0), 4-connected, a tile is open when it is in bounds, not River and not Solid; the first mouth tile reached was (67,427).

Cap: `Unity/Relight/Assets/Relight/Sim/Combat/Director/DirectorRules.cs` line 78, `public const int EntryFarSteps = 68;`. Under D20 the cap must be at least steps + 10 = 76 and 68 is less, so the line was changed to `public const int EntryFarSteps = 76;`. Recompile completed with no compile errors (`recompile_status`: completed, `compilationFailed` false). No test references the constant or its old value.

`DirectorRules.Origin(SimContext, SimState)` needs a live `SimState` with a director; no Editor helper builds one, so it was not called. Origin not tested in editor (heading 14).

## 12. Spawn points

Player spawn: `Buildings/Home workshop [home-workshop]/Player Spawn` at (70.5, 360.5), unchanged (D25). No other object under the world root has "spawn" in its name.

Raid line at (69..74, 432) (`opening:raid-line`, 6×1, compiled record confirmed in the final compile).

## 13. Temporary files

None. No temporary file was created under `Unity/Relight/Assets` in this pass; `Assets/Editor/FoundersCourtChecks.cs` is the check script the prompt asked for and stays. The placement script lived at `Unity/Relight/Temp/FcPerimeter.cs` (outside Assets, git-ignored `Temp/`).

`Relight/World/Validate` does not exist in this Editor (the `Relight/World` menu offers `Refresh Editable World`, `Focus Home` and the other items listed in the assessment; the only validate item is `Relight/Validate Game Data`), so it was not run (heading 14).

## 14. Gaps in this pass

- Design doc not linked: `Unity/Docs` contains two files whose names contain "design" (`GAME_DESIGN.md` and `FREIGHT_STRONGHOLD_DESIGN_2026-09-18.md`), not exactly one, so no link line was appended to either.
- Final gap check reports "Total gaps: 4", not 0: the coordinates x 121 (north and south edges) and y 427 (west and east edges) from heading 2's block rectangle sit inside the east and south ring-road bands (road tiles 121..130 and 427..436, terrain Kind Street), item 6 forbids placing on a road band, and the coverage set counts only buildings and fences. No further object can close them without covering a road tile.
- East-column homes are 8×10, not the template's 10×9: the Works Yard rect ends at x 112 and the east road band starts at x 121, leaving eight columns; Perimeter home 6 and 10 are 9×10 for the same reason on their spans.
- Perimeter fence 4 (75,343) touches only Perimeter home 9; the Home workshop's north wall is at y 347, four tiles south of the band gap, so no fence within the band can touch it without covering Path 2/Path 5.
- Perimeter fence 19 (63,343) touches only Salvage garages; same reason on the Home workshop side.
- Perimeter fence 8 (41,341) touches only North Court home: the next building along the north band is the Maintenance garage (x 49..55, y 344..350), and no straight fence inside x 41..48 can be edge-adjacent both to North Court home (y ≤341) and to the garage (y ≥344).
- Perimeter fence 9 (41,414) touches only Court End home: Approach home (x 49..58) ends at y 412 while Court End home (x ≤40) starts at y 414, so no straight fence inside x 41..48 can be edge-adjacent to both.
- Perimeter fence 17 (23,423) has no building at either end (block corner span); fence 18 (28,423) touches Court End home only.
- Perimeter fences 13 and 14 meet the existing x-27 fences (D16) rather than a second building.
- Fenced gaps too short for a building (all under 10 tiles wide or deep, or hemmed by paths) kept fences: West 342..350, North 23..30, North 41..48, South 41..48, South 59..66, South 96..100, West 329..332, West 383..386, West 402..405, West 424..426, East 424..426, South 23..26, South 28..30, North 63..64. Touching (12) already equals 2 × fenced (6), so no replacement was required.
- `Drive 1` and `Court garden walk` were removed before Step 2.9 rather than at Step 2.12 (reason under heading 10).
- `DirectorRules.Origin` not tested in editor (needs a live `SimState`).
- `Relight/World/Validate` menu does not exist; not run.
- Console errors present at the end of the pass (none from this pass's scripts): `d3d12: failed to wait for fence (258)` (2026-09-17, graphics driver); `Failed to handle /api/exec request: Main thread operation timed out after 5000ms` (2026-09-18 05:20 UTC, an earlier CLI call in this session that exceeded the Pipeline 5 s limit); three `SerializedObjectNotCreatableException: Object at index 0 is null` from `GameObjectInspector`/`TransformInspector`/`SpriteRendererEditor.OnEnable` (2026-09-18 07:12 UTC, the Inspector re-enabling on an object that had just been destroyed by `Undo.DestroyObjectImmediate`, a UI-only exception with no scene effect).
- `SceneBaseGeometry.asset` (the baseline) and the whole `Sim/Combat/Director/` folder are untracked in git, so the baseline edit and the cap change cannot be shown as a diff; they are recorded here instead.
