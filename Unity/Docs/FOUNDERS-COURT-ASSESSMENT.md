# Founders Court assessment

Phase 1 of the Founders Court layout pass. Assessment only. No scene, prefab, asset or code file was changed. Read on 2026-09-18 against the open Unity Editor (6000.6.0f1, pid 11740, port 7800, project `E:\Factorio2\Unity\Relight`, scene `World.unity` active, not dirty, Play Mode stopped).

Coordinate convention used throughout: **tile coordinates**, x east, y south, one tile = one Unity unit. A GameObject's pivot is its north-west corner. Unity world position = (tile x, −tile y). All widths and heights are in tiles.

## 1. Scene and edit method

- **Scene:** `Unity/Relight/Assets/Relight/Scenes/World.unity`. It is the only `.unity` file in the repository whose text contains "Editable World" (one match). Note: the prompt names the Unity project folder as `Unity`; the actual Unity project root is `Unity/Relight` (the folder that holds `Assets/`, `ProjectSettings/`, `Packages/`). `Unity/Assets` exists but contains only an empty leftover folder. See heading 8.
- **World root:** GameObject `Editable World` (component `Relight.World.SceneWorld`, baseline asset `SceneBaseGeometry`). Its category children are `Buildings`, `Props`, `Sites and resources`, `Roads and paths`, `Paved areas` as the prompt defines.
- **How the scene is authored:** every object is an authoring component with a size in tiles and no renderer or collider of its own: `SceneBuilding` (buildings), `SceneProp` (fences, trees, debris), `SceneSite` (core, substation, resource nodes, yard, label, lights, raid line), `ScenePath` (roads, paths, drives as tile polylines), `SceneArea` (paved areas). `SceneWorld.Compile` (`Unity/Relight/Assets/Relight/World/Authoring/SceneWorld.cs`) turns these into the compiled world geometry, including the `Solid` grid the simulation walks against. The visible city is a transient generated display rebuilt by the menu `Relight/World/Refresh Editable World`; it is never saved.
- **Edit method (Phase 2):** the Unity CLI driving the open Editor, per the human's instruction. From WSL the binary is `/mnt/c/Users/Admin/AppData/Local/Unity/bin/unity.exe`; all calls use `--format json --no-banner`. Reads in this pass used `unity command run_script` (a static C# entry point compiled in memory that called `SceneWorld.Compile` live and dumped the block's `Solid` grid, the player spawn and the site records), `unity command eval`, `unity status`, and `unity command capture_scene_view`. Phase 2 edits would use `create_gameobject`, `rename_gameobject`, `delete_gameobject`, `set_transform`, `set_component_properties`, `menu --path "Relight/World/Refresh Editable World"` and `save_scene`, followed by the `Relight/World/Validate` menu if present. No `.unity`, `.prefab` or `.asset` YAML is to be hand-edited while the Editor is open.
- **Cross-check:** the scene file on disk was also parsed to build the inventory; every site, spawn and size it produced matched the live compile.

## 2. Block inventory

**Court road:** `Road 368` at `Editable World/Roads and paths/Road 368`, kind Road, polyline (72,432) → (72,374). No road under `Roads and paths` has "Founders Court" in its name, so the fallback rule applies: the enemy raid-gate row `Opening raid approach [opening:raid-line]` (tiles x 69..74, y 391) lies on this road's carriageway. The label site `Founders Court [label:0]` sits at (72,372), on the same road's head.

**Ring roads (four sides, two segments per side, all kind Road, centre lines):**

| Side | Segments | Centre line |
|---|---|---|
| North | `Road 124` (18,324)→(72,324); `Road 125` (72,324)→(126,324) | y = 324 |
| South | `Road 158` (18,432)→(72,432); `Road 159` (72,432)→(126,432) | y = 432 |
| West | `Road 200` (18,324)→(18,378); `Road 201` (18,378)→(18,432) | x = 18 |
| East | `Road 217` (126,324)→(126,378); `Road 218` (126,378)→(126,432) | x = 126 |

**Block rectangle (inside the ring roads' inner edges, road half width 5):** min x 23, max x 121, min y 329, max y 427 (99 × 99 tiles).

**Mouth:** (72,432) on the south ring road centre line; the court road crosses the block's south edge at (72,427). Carriageway span at the mouth x 69..75, full paved band x 67..76.
**Head:** (72,374), the north end of `Road 368`, drawn as the court bulb (radius 5.5 around (72,374)).

**Objects inside or overlapping the block.** "Sprite" is the generated-display key (no authoring object carries a sprite or a collider; all are layer 0 Default, tag Untagged). Position is the NW corner tile.

| Name | Parent category | Pos x | Pos y | W | H | Sprite (generated key) | Colliders | Layer | Tag | Role |
|---|---|---|---|---|---|---|---|---|---|---|
| Home workshop [home-workshop] | Buildings | 65 | 347 | 10 | 14 | rf-workshop-roof-1 (enterable, campaign) | none | Default | Untagged | WORKSHOP |
| Home workshop [home-workshop] (child SceneSite, kind Core) | Buildings | 65 | 347 | 10 | 14 | none | none | Default | Untagged | WORKSHOP (core site) |
| Player Spawn (child of Home workshop) | Buildings | 70.5 | 360.5 | 1 | 1 | none | none | Default | Untagged | PLAYER SPAWN |
| Foreman workshop [foreman-shelter] | Buildings | 98 | 393 | 12 | 9 | rf-workshop-roof-0 (enterable, campaign) | none | Default | Untagged | none (campaign building) |
| Salvage garages [home-garage] | Buildings | 51 | 337 | 12 | 7 | rf-garage-roof-2 | none | Default | Untagged | none |
| Maintenance garage [court-west-garage] | Buildings | 51 | 358 | 7 | 7 | rf-garage-roof-2 | none | Default | Untagged | none |
| court garage [court-garage] | Buildings | 87 | 335 | 10 | 9 | rf-garage-roof-2 | none | Default | Untagged | none |
| North Court home [court-north-home] | Buildings | 31 | 333 | 10 | 9 | rf-house-N-roof-2 | none | Default | Untagged | none |
| Court gardener’s home [court-northwest-home] | Buildings | 31 | 351 | 10 | 9 | rf-house-E-roof-0 | none | Default | Untagged | none |
| Court brick home [court-west-home] | Buildings | 31 | 374 | 10 | 9 | rf-house-E-roof-1 | none | Default | Untagged | none |
| Garden Cottage [court-garden-home] | Buildings | 31 | 393 | 10 | 9 | rf-house-E-roof-0 | none | Default | Untagged | none |
| Court End home [court-southwest-home] | Buildings | 31 | 414 | 10 | 10 | rf-house-roof-1 | none | Default | Untagged | none |
| court house [court-house] | Buildings | 49 | 382 | 10 | 9 | rf-house-roof-1 | none | Default | Untagged | none |
| Approach home [court-approach-home] | Buildings | 49 | 404 | 10 | 9 | rf-house-E-roof-2 | none | Default | Untagged | none |
| Court cottage [court-east-cottage] | Buildings | 85 | 382 | 9 | 7 | rf-house-W-roof-0 | none | Default | Untagged | none |
| Court cottage [court-east-home] | Buildings | 87 | 410 | 9 | 8 | rf-house-roof-1 | none | Default | Untagged | none |
| East Garden home [court-east-garden-home] | Buildings | 101 | 414 | 10 | 10 | rf-house-roof-0 | none | Default | Untagged | none |
| Iron ore deposit [opening-iron-v1] | Sites and resources | 84 | 352 | 3 | 4 | site Resource, item ironore, amount 7680 | none | Default | Untagged | IRON |
| Copper ore deposit [opening-copper-v1] | Sites and resources | 96 | 352 | 3 | 4 | site Resource, item copperore, amount 1200 | none | Default | Untagged | COPPER |
| Coal deposit [opening-coal-v1] | Sites and resources | 96 | 369 | 3 | 3 | site Resource, item coal, amount 700 | none | Default | Untagged | COAL |
| Factory yard 0 [yard:0] | Sites and resources | 83 | 350 | 30 | 29 | site Yard (paving drawn from this rect) | none | Default | Untagged | PAVED LOT (see heading 8) |
| Substation 0 [substation:0] | Sites and resources | 78 | 347 | 3 | 3 | site Substation | none | Default | Untagged | TRANSFORMER (see heading 8) |
| Opening raid approach [opening:raid-line] | Sites and resources | 69 | 391 | 6 | 1 | site RaidLine (invisible gate row) | none | Default | Untagged | ENEMY SPAWN (gate row only, see heading 8) |
| Founders Court [label:0] | Sites and resources | 72 | 372 | 1 | 1 | site Label (text "Founders Court") | none | Default | Untagged | none |
| Street light [light:0] | Sites and resources | 64 | 363 | 1 | 1 | site Light | none | Default | Untagged | none |
| Street light [light:1] | Sites and resources | 78 | 363 | 1 | 1 | site Light | none | Default | Untagged | none |
| Street light [light:2] | Sites and resources | 89 | 360 | 1 | 1 | site Light | none | Default | Untagged | none |
| Street light [light:3] | Sites and resources | 108 | 360 | 1 | 1 | site Light | none | Default | Untagged | none |
| Street light [light:4] | Sites and resources | 89 | 375 | 1 | 1 | site Light | none | Default | Untagged | none |
| Street light [light:5] | Sites and resources | 108 | 375 | 1 | 1 | site Light | none | Default | Untagged | none |
| Court garden walk | Paved areas | 43 | 395 | 4 | 25 | area (tiled paving) | none | Default | Untagged | none |
| fence [neighbourhood:court-west-garden-edge] | Props | 27 | 387 | 1 | 14 | prop fence (CitySprites "Fence"), blocksMovement 0 | none | Default | Untagged | none |
| fence [neighbourhood:court-west-garden-edge-2] | Props | 27 | 406 | 1 | 18 | prop fence (CitySprites "Fence"), blocksMovement 0 | none | Default | Untagged | none |
| debris [court-brush] | Props | 111 | 383 | 3 | 2 | prop debris, blocksMovement 0, clearable 1 | none | Default | Untagged | none |
| tree [court-west-home:garden-tree] | Props | 29 | 372 | 2 | 2 | prop tree | none | Default | Untagged | none |
| tree [court-east-cottage:garden-tree] | Props | 83 | 380 | 2 | 2 | prop tree | none | Default | Untagged | none |
| tree [neighbourhood:court-tree-a] | Props | 44 | 388 | 2 | 2 | prop tree | none | Default | Untagged | none |
| tree [neighbourhood:court-tree-b] | Props | 44 | 410 | 2 | 2 | prop tree | none | Default | Untagged | none |
| Road 368 | Roads and paths | 72 | 432 | 10 wide | 58 long | road band, polyline (72,432)→(72,374) | none | Default | Untagged | court road |
| Drive 1 | Roads and paths | 126 | 382 | 4 wide | — | drive, polyline (126,382)→(117,382)→(117,344) | none | Default | Untagged | none |
| Path 1 | Roads and paths | 70 | 361 | 1.4 wide | — | path (70,361)→(72,361)→(72,371) | none | Default | Untagged | driveway stub, Home workshop |
| Path 2 | Roads and paths | 57 | 344 | 1.4 wide | — | path (57,344)→(72,344)→(72,327) | none | Default | Untagged | driveway stub, Salvage garages → north ring |
| Path 5 | Roads and paths | 92 | 344 | 1.4 wide | — | path (92,344)→(72,344)→(72,327) | none | Default | Untagged | driveway stub, court garage → north ring |
| Path 27 | Roads and paths | 54 | 365 | 1.4 wide | — | path (54,365)→(72,365)→(72,371) | none | Default | Untagged | driveway stub, Maintenance garage |
| Path 39 | Roads and paths | 41 | 355 | 1.4 wide | — | path (41,355)→(46,355)→(46,370)→(72,370)→(72,371) | none | Default | Untagged | driveway stub, Court gardener’s home |
| Path 40 | Roads and paths | 41 | 378 | 1.4 wide | — | path (41,378)→(69,378) | none | Default | Untagged | driveway stub, Court brick home |
| Path 4 | Roads and paths | 54 | 391 | 1.4 wide | — | path (54,391)→(69,391) | none | Default | Untagged | driveway stub, court house |
| Path 61 | Roads and paths | 41 | 397 | 1.4 wide | — | path (41,397)→(69,397) | none | Default | Untagged | driveway stub, Garden Cottage |
| Path 41 | Roads and paths | 59 | 408 | 1.4 wide | — | path (59,408)→(69,408) | none | Default | Untagged | driveway stub, Approach home |
| Path 42 | Roads and paths | 84 | 385 | 1.4 wide | — | path (84,385)→(75,385) | none | Default | Untagged | driveway stub, Court cottage [court-east-cottage] |
| Path 3 | Roads and paths | 103 | 402 | 1.4 wide | — | path (103,402)→(123,402) | none | Default | Untagged | driveway stub, Foreman workshop → east ring |
| Path 26 | Roads and paths | 91 | 418 | 1.4 wide | — | path (91,418)→(91,429) | none | Default | Untagged | driveway stub, Court cottage [court-east-home] → south ring |
| Path 63 | Roads and paths | 36 | 332 | 1.4 wide | — | path (36,332)→(36,327) | none | Default | Untagged | driveway stub, North Court home → north ring |
| Path 62 | Roads and paths | 36 | 424 | 1.4 wide | — | path (36,424)→(36,429) | none | Default | Untagged | driveway stub, Court End home → south ring |
| Path 64 | Roads and paths | 106 | 424 | 1.4 wide | — | path (106,424)→(106,429) | none | Default | Untagged | driveway stub, East Garden home → south ring |

Roles not assignable to any object: WORKBENCH (no object; see heading 8). ENEMY SPAWN is a gate row, not a point (see heading 8).

## 3. Enemy pathing and blocking property

- **Enemy prefab path:** none exists. Enemies have no prefab, no GameObject per body and no collider. They are drawn by `Unity/Relight/Assets/Relight/Presentation/Combat/EnemyPresenter.cs` as pooled sprite quads built in code (`bodyTiles` 0.7, scene value 0.7; the optional `body` sprite field is unassigned). Enemy definitions are data assets: `Unity/Relight/Assets/Relight/Data/Generated/Enemies/Enemy - Skitter.asset` (speed 5.4 tiles/s, range 1.3) and `Enemy - Spitter.asset`.
- **Movement code files:** `Unity/Relight/Assets/Relight/Sim/Combat/Enemies/EnemyPhase.cs` (per-tick enemy movement), `Unity/Relight/Assets/Relight/Sim/Combat/Director/RaidField.cs` (breadth-first distance field), `Unity/Relight/Assets/Relight/Sim/Combat/Director/DirectorRules.cs` (entry tile choice, passability for hostiles), `Unity/Relight/Assets/Relight/Sim/World/Ground.cs` (tile passability).
- **Pathing method: custom, tile-based. Not NavMesh, not A\*.** The raid approach descends a breadth-first distance field toward the Home core: `RaidField.cs:73–143` builds it (4-connected, `Open()` at lines 90–108 calls `Ground.Walkable` at line 94 and `DirectorRules.HostileOpen` at line 105); `EnemyPhase.cs:349` `FieldStep` picks the neighbouring tile with the lower distance and `EnemyPhase.cs:378` `WalkField` moves along it. Once a target is in reach, pursuit is a greedy 8-connected step: `EnemyPhase.cs:434` `StepToward` tests `Ground.Passable` on the next tile and never cuts a corner. The class remarks at the top of `EnemyPhase.cs` state that pursuit deliberately uses this greedy step rather than the A\* planner (the A\* `PathFinder` is used by the engineer only, `EngineerMovement.cs:171`). Enemy entry tiles are chosen at runtime by `DirectorRules.Origin` (`DirectorRules.cs:141`), 8–68 BFS steps from the core, at or south of the raid-line row (`RaidLineY`, `DirectorRules.cs:64`, = 391 here) and at least 28 tiles from the engineer.
- **A house blocks enemies because** `SceneWorld.Compile` writes its footprint into the compiled geometry's `Solid` byte grid (`Unity/Relight/Assets/Relight/World/Authoring/SceneWorld.cs:78`: every tile of a non-enterable `SceneBuilding`, or the one-tile wall ring minus the door rects of an enterable one), and `Ground.Walkable` (`Unity/Relight/Assets/Relight/Sim/World/Ground.cs:176–181`) returns false for any tile whose `Solid` entry is set. Both the field builder (`RaidField.cs:94`) and `HostileOpen` (`DirectorRules.cs:45`) call `Walkable`, and `StepToward` (`EnemyPhase.cs:434`) calls `Passable` (`Ground.cs:198`, = `Walkable` and not occupied by a machine), so no field distance and no step ever enters a house tile.
- **Blocking property:** the compiled `Solid` grid entry. There is no layer, tag, collider, NavMeshObstacle or tilemap collider involved. The same `Compile` also sets `Solid` for: `SceneProp` with `blocksMovement` true (`SceneWorld.cs:83`), `BlocksMovement` box components (`SceneWorld.cs:85`), `SceneSite` of kind Substation (`SceneWorld.cs:97`), and everything already solid in the baseline asset, because `Compile` starts from `Instantiate(baseline)` (`SceneWorld.cs:47`). The live compile shows the baseline contributes 54 solid tiles inside this block: the two fence props at x 27 (y 387..400 and 406..423), the four trees, and the debris at (111..113, 383..384). Those props therefore block today even though their `blocksMovement` flag is 0, and moving or deleting their GameObjects would not move or remove the blocking tiles. See heading 8.
- **Blocks player: yes.** The engineer's path planner and step both use `Ground.Passable` (`Unity/Relight/Assets/Relight/Sim/Actor/Movement/EngineerMovement.cs:192`), and standing uses `Ground.CanStand` (`Ground.cs:205–215`, four body corners at `BodyRadiusTiles`, reference 0.28). The player and enemies share one rule.

## 4. Measurements

- **Enemy width:** 0.7 tiles (`EnemyPresenter` `bodyTiles`, the only size an enemy has; no collider). Movement is decided per whole tile, so any one-tile opening passes an enemy. Gap threshold used in heading 5: 1 tile.
- **Belt tile:** 1 tile = 1 Unity unit (`Unity/Relight/Assets/Relight/Data/Generated/Machines/Machine - Belt.asset`, `size: 1`).
- **Ring road width:** all roads share one width from `SceneBaseGeometry.asset` (`roadHalfWidth` 5, `roadPavement` 2, read at `Unity/Relight/Assets/Relight/Presentation/City/CityPresenter.cs:250–251`): paved band 10 tiles, carriageway 6 tiles. North 10, East 10, South 10, West 10.
- **Court road width:** 10 tiles (same shared width; carriageway 6). D11 asks for 5. The renderer and the data have no per-road width, so a narrower court road needs a decision (heading 9).
- **Node → workshop input:** there is no object named as the workshop input; distances are measured to the workshop door tiles (69..71, 360) as the nearest edge, in tiles = belt tiles (axis gaps in brackets, Manhattan in parentheses).
  - IRON: 13.9 tiles [13 east, 5 south] (18)
  - COPPER: 25.5 tiles [25 east, 5 south] (30)
  - COAL: 26.6 tiles [25 east, 9 south] (34)
  - All three exceed the D9 minimum of 8.
- **Node pair distances (nearest edges):**
  - IRON – COPPER: 10.0 tiles [10 east, 0]
  - IRON – COAL: 17.2 tiles [10 east, 14 south]
  - COPPER – COAL: 14.0 tiles [0, 14 south]
  - All three exceed the D9 minimum of 4.
- **Player spawn:** tile (70.5, 360.5), the middle door tile of the Home workshop.
- **Block interior solid tiles today:** 1237 of 9801.

## 5. Perimeter gaps

Method: the compiled `Solid` grid from the live editor. The perimeter lines themselves (x 23, x 121, y 329, y 427) hold **zero** solid tiles: no house or fence stands on the perimeter, so taken literally every edge is one continuous gap. The useful measure is coverage by the outermost row of blockers, so each edge is projected through a band from the road's inner edge to the far side of that edge's outer house row (west x 23..41, north y 329..360, east x 97..121, south y 404..427). A position is covered when any solid tile lies in the band at that coordinate. Following D1, only houses and fences count; tiles that are solid only because of trees, debris or the substation are marked and not counted.

| Edge | Start | End | Length (tiles) | Note |
|---|---|---|---|---|
| West (x 23..41) | y 329 | y 332 | 4 | before North Court home |
| West | y 342 | y 350 | 9 | between North Court home and Court gardener’s home |
| West | y 360 | y 371 | 12 | between Court gardener’s home and Court brick home |
| West | y 383 | y 386 | 4 | between Court brick home and fence at x 27 |
| West | y 402 | y 405 | 4 | between the two fences at x 27 (fence solid comes from the baseline, heading 8) |
| West | y 424 | y 427 | 4 | after Court End home |
| East (x 97..121) | y 329 | y 392 | 64 | nothing but Drive 1 and the yard until Foreman workshop; debris at y 383..384 not counted |
| East | y 402 | y 413 | 12 | between Foreman workshop and East Garden home |
| East | y 424 | y 427 | 4 | after East Garden home |
| North (y 329..360) | x 23 | x 30 | 8 | before North Court home |
| North | x 41 | x 50 | 10 | between North Court home and Salvage garages |
| North | x 63 | x 64 | 2 | between Salvage garages and Home workshop |
| North | x 75 | x 86 | 12 | between Home workshop and court garage; substation at x 78..80 not counted |
| North | x 97 | x 121 | 25 | after court garage |
| South (y 404..427) | x 23 | x 26 | 4 | before fence at x 27 |
| South | x 28 | x 30 | 3 | between fence and Court End home |
| South | x 41 | x 48 | 8 | between Court End home and Approach home; tree at x 44..45 not counted |
| South | x 59 | x 66 | 8 | Approach home to the mouth |
| South | x 77 | x 86 | 10 | mouth to Court cottage [court-east-home] |
| South | x 96 | x 100 | 5 | between Court cottage [court-east-home] and East Garden home |
| South | x 111 | x 121 | 11 | after East Garden home |

Mouth excluded at (72,427): x 67..76 on the south edge.
Total gaps: 21

## 6. House and driveway problems

- No house overlaps the court road (band x 67..76 from y 369 south, and the bulb around (72,374)).
- No door marker: none. All 16 buildings carry a door rect, garages included.
- No driveway: none. Every building touches one path stub. All stubs are `ScenePath` kind Path (1.4 tiles wide footpaths), not kind Drive (4 wide); the only Drive in the block, `Drive 1`, touches no house.
- Driveway or door on the wrong side for a house along the court road (D5 wants doors facing the road):
  - `court house [court-house]` (49,382): door on the south wall, road is to the east; `Path 4` runs from the south door east to the road.
  - `Court cottage [court-east-home]` (87,410): door on the south wall, road is to the west; `Path 26` runs south to the ring road, not to the court road.
- Interior building not along the court road (D5): `Maintenance garage [court-west-garage]` (51,358, 7 × 7) stands 10 tiles west of the court road band with its stub `Path 27` reaching the road; it is neither a perimeter house nor on the court road.
- `Court cottage [court-east-cottage]` (85,382) and `Approach home [court-approach-home]` (49,404) face the court road correctly (west and east doors).

## 7. Objects with no job

Names first, matched to the scene capture taken through the editor:

- `Court garden walk` (Paved areas, 43,395, 4 × 25): the tiled footpath strip on the left, running south from the tree at (44,388) past Garden Cottage. It connects nothing.
- `Drive 1` (Roads and paths, kind Drive, (126,382)→(117,382)→(117,344)): the tall grey rectangle on the right, a 4-wide pavement band along x 117 from y 344 to 382 with a short leg to the east ring road. It reaches no building; it ends beside the yard's east edge.
- `debris [court-brush]` (Props, 111,383, 3 × 2): the small pale triangle just south-east of the paved lot. Its solid tiles come from the baseline, not from the prop.
- `fence [neighbourhood:court-west-garden-edge]` (Props, 27,387, 1 × 14) and `fence [neighbourhood:court-west-garden-edge-2]` (Props, 27,406, 1 × 18): the thin vertical lines on the left. **Possible fence sprite:** they are `SceneProp` kind `fence`, drawn by the generated display's fence art (`CitySprites.cs:84`, "posts and diagonal slats"). No fence prefab exists (`Unity/Relight/Assets/Relight/Prefabs` holds only `Engineer.prefab`, `Machines/`, `PrefabRegistry.asset`), but a fence prop kind with existing art does.
- `Street light [light:0]` … `Street light [light:5]` (Sites and resources, six one-tile Light sites at (64,363), (78,363), (89,360), (108,360), (89,375), (108,375)): decorative lights, four of them around the paved lot.
- `Founders Court [label:0]` (Sites and resources, 72,372): the name label at the head. Decorative.
- `Substation 0 [substation:0]` has the TRANSFORMER role and is kept out of this list; `Opening raid approach [opening:raid-line]` is the raid gate row and is kept out of this list (D13).

## 8. Gaps in this report

- The prompt's project folder `Unity` and docs path `Unity/Docs` do not match the Unity project: the project root is `Unity/Relight`, so the Phase 2 file `Unity/Assets/Editor/FoundersCourtChecks.cs` would have to live at `Unity/Relight/Assets/Editor/FoundersCourtChecks.cs` to compile; `Unity/Assets` holds only an empty folder.
- No WORKBENCH object exists in the scene; the player spawn is a child of the Home workshop at its middle door tile (70.5, 360.5). "Player spawn stays on the workbench" (D13) can only be read as "stays where it is".
- ENEMY SPAWN is not a scene point. `Opening raid approach [opening:raid-line]` is a gate row; the actual entry tile is chosen at runtime by `DirectorRules.Origin` from the distance field (8–68 steps from the core, y ≥ 391, ≥ 28 tiles from the engineer). Whether a wave enters exactly at the mouth cannot be verified from the scene.
- "Workshop input" is undefined; the D9 distances were measured to the workshop door tiles (69..71, 360).
- The PAVED LOT is `Factory yard 0 [yard:0]`, a `SceneSite` of kind Yard under `Sites and resources`, not a GameObject under `Paved areas`. Its paving is drawn from the yard rect. The nodes sit inside its rect.
- The TRANSFORMER is `Substation 0 [substation:0]`, a 3 × 3 `SceneSite` (kind Substation), not a building; it stands 3 tiles west of the yard's north-west corner and its 9 tiles are solid by `SceneWorld.Compile`, which conflicts with D1 as written.
- Court road width cannot be set per road: all Road polylines share `roadHalfWidth` from `SceneBaseGeometry.asset` (`CityPresenter.cs:250–251`). D11 needs either a code change (per-path width) or a kind change (Drive is 4 wide, Path 1.4 wide).
- No enemy prefab, collider or sprite bounds exist; enemy width is the presenter's `bodyTiles` value 0.7, and passability is decided per tile.
- 54 solid tiles inside the block come from the baseline asset (the two fence props at x 27, four trees, the debris), not from the props' `blocksMovement` flags, which are all 0. Moving, removing or reusing those GameObjects would leave their baseline solids in place; blocking would have to be checked in the compiled grid, not inferred from the GameObject list.
- The perimeter gap list uses a projection band per edge (stated in heading 5); no house or fence touches the perimeter lines themselves, so the literal per-line gap is the whole edge on all four sides.
- The heading 2 "sprite name" column holds the generated-display key (roof key, prop kind or site kind), because authoring objects carry no sprite renderers; the visible art is generated at refresh time.
- The "tall grey bar" and "triangle" identifications rest on one scene-view capture compared against object positions; no other visual reference was supplied.
- Process note: the scene-view capture was written by the CLI to `Assets/Temp/fc_view.png` inside the project (the CLI resolves save paths under `Assets`); the file and its folder were deleted through the editor's AssetDatabase in the same pass and `git status` shows no trace. The report is the only changed file. Helper scripts live in the untracked, ignored `Unity/Relight/Temp/` folder and in the session scratchpad.

## 9. Questions for the human

- Phase 2 paths: confirm that `Unity/Assets/Editor/FoundersCourtChecks.cs` and the Fence prefab should be created under `Unity/Relight/Assets/...` (the real project root).
- D11 court road width: the renderer has one shared road width. Should Phase 2 (a) add a per-path width to `ScenePath` and the renderer (code change), (b) change `Road 368` to kind Drive (4 wide, not 5), or (c) leave the width and record D11 as not done?
- D9 "workshop input": is the workshop door (tiles 69..71, 360) the intended reference, or a specific input tile?
- D4/D5 "house": do garages under `Buildings` count as houses (this decides whether `Maintenance garage [court-west-garage]` at (51,358) must be moved or removed under D5)?
- D1: the substation's 9 tiles and the baseline solids for trees and debris block enemies today. Should Phase 2 leave them (report only) or is a code or baseline change wanted?
- Which objects under heading 7 should be removed? List their names in the go message. If you list none, nothing is removed.
