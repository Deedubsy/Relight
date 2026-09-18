# Founders Court plan

Phase 3 Part A. Plan only: no scene, prefab, asset or code file was changed. Source: fresh `SceneWorld.Compile` dump of the open `World.unity` (`Unity/Relight/Temp/fc_dump.json`), planner `Unity/Relight/Temp/fc_plan.py`, drawing `Unity/Relight/Temp/fc_draw.py`. Tile coordinates: x east, y south, pivot north-west, Unity position = (x, −y). Rectangles are `(x,y) w×h` with inclusive tile ranges.

## 1. Inputs

Compiled values match heading 2 of `FOUNDERS-COURT-ASSESSMENT.md` (block, court road, circle, mouth, workshop); one roof-key difference is under heading 9.

| Input | Value |
|---|---|
| Block rectangle | (23,329) 99×99, x 23..121, y 329..427 |
| Court road | `Road 368`, polyline (72,432)→(72,374), half width 5, pavement 2 |
| Road band span | x 67..76 (carriageway x 69..75), from the mouth y 427 north to y 369; straight part y 379..427 |
| Circle | centre (72,374), radius 5.5; circle tiles = tile centres strictly inside the radius: rows 369..378, x 67..76, 88 tiles |
| Mouth | south block edge y 427, x 67..76 |
| Head | (72,374) |
| Workshop rectangle | `Home workshop [home-workshop]` (65,347) 10×14, x 65..74, y 347..360, roof rf-workshop-roof-1 |
| Workshop door tiles | (69,360), (70,360), (71,360) (south wall) |
| Player spawn | Unity (70.5, −360.5); tile (70,360) |
| Raid line | `Opening raid approach [opening:raid-line]` (69,432) 6×1, y 432, x 69..74 |
| Entry step cap | `EntryFarSteps = 76` at `Unity/Relight/Assets/Relight/Sim/Combat/Director/DirectorRules.cs:78` |
| Walk steps core → mouth (this compile, BFS from the ring around the Core) | 66 to the mouth row, 71 to the raid line |

Interior tiles reached before: 5901
(D42 on the current scene: mouth tiles x 67..76, y 427 marked solid, 4-connected flood from ring-road tile (72,435) over non-solid tiles, counted against the planned interior of 8089 tiles, x 26..118, y 332..424 minus road band and circle.) The Phase 2 closure sits inside the new fence line (fences at rows 333/423 and columns 27/120, buildings at x 31/113), so the strip between it and the planned fence line is open to the ring roads today; the tile in front of the workshop door (70,361) is reached.

## 2. Fence line and sidewalk

M1: block edges moved inward by D35 = 2: fence line x 25 and x 119, y 331 and y 425. Ring tiles in the road band (x 67..76, row 425) removed. 366 fence tiles. The two south tiles nearest the band, (66,425) and (77,425), touch the band's pavement edge tiles (67,425) and (76,425).

| Key | Side | Start | End | Rect | Tiles |
|---|---|---|---|---|---|
| `fc-fence-west-1` | west | (25,331) | (25,425) | (25,331) 1×95, x 25..25, y 331..425 | 95 |
| `fc-fence-east-1` | east | (119,331) | (119,425) | (119,331) 1×95, x 119..119, y 331..425 | 95 |
| `fc-fence-north-1` | north | (26,331) | (118,331) | (26,331) 93×1, x 26..118, y 331..331 | 93 |
| `fc-fence-south-1` | south | (26,425) | (66,425) | (26,425) 41×1, x 26..66, y 425..425 | 41 |
| `fc-fence-south-2` | south | (77,425) | (118,425) | (77,425) 42×1, x 77..118, y 425..425 | 42 |

M2 sidewalk: strip between the block edge and the fence line, 2 tiles wide, on all four sides, minus the road band. Object kind: `SceneArea` (paved square, `serviceArea` false) — no `ScenePath` kind has width 2.

| Key | Side | Rect |
|---|---|---|
| `fc-sidewalk-west` | west | (23,329) 2×99, x 23..24, y 329..427 |
| `fc-sidewalk-east` | east | (120,329) 2×99, x 120..121, y 329..427 |
| `fc-sidewalk-north` | north | (25,329) 95×2, x 25..119, y 329..330 |
| `fc-sidewalk-south-w` | south | (25,426) 42×2, x 25..66, y 426..427 |
| `fc-sidewalk-south-e` | south | (77,426) 43×2, x 77..119, y 426..427 |

## 3. Lots

M3 straight part: L = rows 379..424 = 46 (from the south fence line row 425 to the southernmost circle row 378). Lots per side = round(46/16) = 3; frontage = floor(46/3) = 15; remainder 1 to the northernmost lot. West lots run x 26..66 (fence line to road band edge), east lots x 77..118.

M4 circle band: interior y 332..378. Workshop lot WS centred on x 72, frontage 16 in x, from the north fence line to the circle's north edge. CW = remaining band west of WS, CE = east (both L-shaped, see heading 9). M5: YARD = CE merged with E3.

| Lot | Key | Rectangles | Fronts | Frontage span | Road edge | Front wall line | Note |
|---|---|---|---|---|---|---|---|
| W1 | `fc-lot-W1` | (26,410) 41×15, x 26..66, y 410..424 | band | y 410..424 (15) | x 67 | x 62 |  |
| W2 | `fc-lot-W2` | (26,395) 41×15, x 26..66, y 395..409 | band | y 395..409 (15) | x 67 | x 62 |  |
| W3 | `fc-lot-W3` | (26,379) 41×16, x 26..66, y 379..394 | band | y 379..394 (16) | x 67 | x 62 |  |
| E1 | `fc-lot-E1` | (77,410) 42×15, x 77..118, y 410..424 | band | y 410..424 (15) | x 76 | x 81 |  |
| E2 | `fc-lot-E2` | (77,395) 42×15, x 77..118, y 395..409 | band | y 395..409 (15) | x 76 | x 81 |  |
| E3 | `fc-lot-E3` | (77,379) 42×16, x 77..118, y 379..394 | band | y 379..394 (16) | x 76 | x 81 | merged into YARD |
| WS | `fc-lot-WS` | (64,332) 16×37, x 64..79, y 332..368 | circle | x 69..74 (6) | y 369 | y 364 |  |
| CW | `fc-lot-CW` | (26,332) 38×37, x 26..63, y 332..368; (26,369) 41×10, x 26..66, y 369..378 | circle | y 371..376 (6) | x 67 | x 62 |  |
| CE | `fc-lot-CE` | (80,332) 39×37, x 80..118, y 332..368; (77,369) 42×10, x 77..118, y 369..378 | circle | y 371..376 (6) | x 76 | x 81 | merged into YARD |
| YARD | `fc-lot-YARD` | (80,332) 39×37, x 80..118, y 332..368; (77,369) 42×10, x 77..118, y 369..378; (77,379) 42×16, x 77..118, y 379..394 | band+circle | y 379..394 (16) | x 76 | x 81 | = CE + E3, no house |

Coverage check: every interior tile (8089) lies in exactly one of W1, W2, W3, E1, E2, WS, CW, YARD: PASS.

## 4. Houses

M8 template order (assessment heading 2): `Salvage garages [home-garage]` (rf-garage-roof-2, 12×7), `Maintenance garage [court-west-garage]` (rf-garage-roof-2, 7×7), `court garage [court-garage]` (rf-garage-roof-2, 10×9), `North Court home [court-north-home]` (rf-house-N-roof-2, 10×9), `Court gardener’s home [court-northwest-home]` (rf-house-E-roof-0, 10×9), `Court brick home [court-west-home]` (rf-house-E-roof-1, 10×9), `Garden Cottage [court-garden-home]` (rf-house-E-roof-0, 10×9), `Court End home [court-southwest-home]` (rf-house-roof-1, 10×10), `court house [court-house]` (rf-house-E-roof-1, 10×9), `Approach home [court-approach-home]` (rf-house-E-roof-2, 10×9), `Court cottage [court-east-cottage]` (rf-house-W-roof-0, 9×7), `Court cottage [court-east-home]` (rf-house-roof-1, 9×8), `East Garden home [court-east-garden-home]` (rf-house-roof-0, 10×10).

Assignment order W1..W3, E1..E3, CW; E3 is YARD; E2 is the Foreman workshop's lot (its centre (98.0,402.5) is nearest the Foreman's current centre (104.0,397.5)). Skipped: `Maintenance garage`, `court garage` for W2 (same roof as W1), `Garden Cottage` for CW (same roof as W3). Roof keys of copies take the direction suffix of the road-facing wall (E for west lots, W for east lots), as the existing houses do; all resulting keys already exist in the scene.

| Lot | Source | Template roof | Size | Roof used | House rect | Door tiles | Stub key | Stub start | Stub end | Fate |
|---|---|---|---|---|---|---|---|---|---|---|
| W1 | `Salvage garages [home-garage]` | rf-garage-roof-2 | 12×7 | rf-garage-roof-2 | (51,414) 12×7, x 51..62, y 414..420 | (62,416), (62,417), (62,418) | `fc-stub-W1` | (63,417) | (67,417) | new copy |
| W2 | `North Court home [court-north-home]` | rf-house-N-roof-2 | 10×9 | rf-house-E-roof-2 | (53,398) 10×9, x 53..62, y 398..406 | (62,401), (62,402), (62,403) | `fc-stub-W2` | (63,402) | (67,402) | new copy |
| W3 | `Court gardener’s home [court-northwest-home]` | rf-house-E-roof-0 | 10×9 | rf-house-E-roof-0 | (53,382) 10×9, x 53..62, y 382..390 | (62,385), (62,386), (62,387) | `fc-stub-W3` | (63,386) | (67,386) | new copy |
| E1 | `Court brick home [court-west-home]` | rf-house-E-roof-1 | 10×9 | rf-house-W-roof-1 | (81,413) 10×9, x 81..90, y 413..421 | (81,416), (81,417), (81,418) | `fc-stub-E1` | (80,417) | (76,417) | new copy |
| E2 | `Foreman workshop [foreman-shelter]` | rf-workshop-roof-0 | 12×9 | rf-workshop-roof-0 | (81,398) 12×9, x 81..92, y 398..406 | (81,401), (81,402), (81,403) | `fc-stub-E2` | (80,402) | (76,402) | keep and move |
| CW | `Court End home [court-southwest-home]` | rf-house-roof-1 | 10×10 | rf-house-E-roof-1 | (53,369) 10×10, x 53..62, y 369..378 | (62,372), (62,373), (62,374) | `fc-stub-CW` | (63,373) | (67,373) | new copy |
| WS | `Home workshop [home-workshop]` | rf-workshop-roof-1 | 10×14 | rf-workshop-roof-1 | (65,347) 10×14, x 65..74, y 347..360 | (69,360), (70,360), (71,360) | `fc-stub-WS` | (70,361) | (70,369) | keep in place |

Stubs are straight `ScenePath` kind Path from the tile in front of the door to the first road band / circle tile.

| House | Inside lot, clear of fence line, road, sidewalk and other houses |
|---|---|
| `fc-house-W1` | OK |
| `fc-house-W2` | OK |
| `fc-house-W3` | OK |
| `fc-house-E1` | OK |
| `fc-house-E2` | OK |
| `fc-house-CW` | OK |
| `fc-house-WS` | OK |

| Neighbours (share a side fence) | Roof A | Roof B | Distinct |
|---|---|---|---|
| W1 – W2 | rf-garage-roof-2 | rf-house-E-roof-2 | OK |
| W2 – W3 | rf-house-E-roof-2 | rf-house-E-roof-0 | OK |
| W3 – CW | rf-house-E-roof-0 | rf-house-E-roof-1 | OK |
| E1 – E2 | rf-house-W-roof-1 | rf-workshop-roof-0 | OK |
| WS – CW | rf-workshop-roof-1 | rf-house-E-roof-1 | OK |

## 5. Side fences

M6: one 1-tile fence per pair of neighbouring lots along their shared edge, from the compound fence line to the front yard line. Fence tiles lie inside the lot whose frontage is nearer the mouth, never inside YARD.

| Key | Between | In lot | Start | End | Rect | Extent |
|---|---|---|---|---|---|---|
| `fc-sidefence-W1-W2` | W1 – W2 | W1 | (26,410) | (62,410) | (26,410) 37×1, x 26..62, y 410..410 | x 26..62 (fence line → front yard line x 62) |
| `fc-sidefence-W2-W3` | W2 – W3 | W2 | (26,395) | (62,395) | (26,395) 37×1, x 26..62, y 395..395 | x 26..62 (fence line → front yard line x 62) |
| `fc-sidefence-W3-CW` | W3 – CW | W3 | (26,379) | (62,379) | (26,379) 37×1, x 26..62, y 379..379 | x 26..62 (fence line → front yard line x 62) |
| `fc-sidefence-E1-E2` | E1 – E2 | E1 | (81,410) | (118,410) | (81,410) 38×1, x 81..118, y 410..410 | x 81..118 (front yard line x 81 → fence line) |
| `fc-sidefence-E2-YARD` | E2 – YARD | E2 | (81,395) | (118,395) | (81,395) 38×1, x 81..118, y 395..395 | x 81..118 (front yard line x 81 → fence line) |
| `fc-sidefence-WS-CW` | WS – CW | CW | (63,332) | (63,364) | (63,332) 1×33, x 63..63, y 332..364 | y 332..364 (fence line → front yard line y 364 of WS) |
| `fc-sidefence-WS-YARD` | WS – YARD | WS | (79,332) | (79,364) | (79,332) 1×33, x 79..79, y 332..364 | y 332..364 (fence line → front yard line y 364 of WS) |

Overlap check (house, stub, road band, circle, compound fence): PASS.

## 6. Works Yard

M11: `Works Yard [yard:0]` moves from (83,350) 30×29, x 83..112, y 350..378 to the YARD lot rectangle (80,332) 39×63, x 80..118, y 332..394 (largest rectangle inside the L-shaped YARD, see heading 9). Nodes and substation already meet D9, the 2-tile edge margin and the copper contact, so they stay.

| Object | Old rect | New rect | Fate |
|---|---|---|---|
| `Works Yard [yard:0]` | (83,350) 30×29, x 83..112, y 350..378 | (80,332) 39×63, x 80..118, y 332..394 | keep and move |
| `Works Yard Iron Scrap Pile [opening-iron-v1]` | (84,352) 3×4, x 84..86, y 352..355 | (84,352) 3×4, x 84..86, y 352..355 | keep in place |
| `Works Yard Copper Cable Spool [opening-copper-v1]` | (96,352) 3×4, x 96..98, y 352..355 | (96,352) 3×4, x 96..98, y 352..355 | keep in place |
| `Works Yard Coal Fuel Bunker [opening-coal-v1]` | (96,369) 3×3, x 96..98, y 369..371 | (96,369) 3×3, x 96..98, y 369..371 | keep in place |
| `Substation 0 [substation:0]` | (93,350) 3×3, x 93..95, y 350..352 | (93,350) 3×3, x 93..95, y 350..352 | keep in place |

| Node | Axis gap to workshop door tiles (x, y) | Largest axis gap | ≥ 8 (D9) | Margin to yard rect edge | ≥ 2 |
|---|---|---|---|---|---|
| `Works Yard Iron Scrap Pile [opening-iron-v1]` | 12, 4 | 12 | yes | 4 | yes |
| `Works Yard Copper Cable Spool [opening-copper-v1]` | 24, 4 | 24 | yes | 16 | yes |
| `Works Yard Coal Fuel Bunker [opening-coal-v1]` | 24, 8 | 24 | yes | 16 | yes |

| Pair | Axis gap (x, y) | Largest | ≥ 4 (D9) |
|---|---|---|---|
| `Works Yard Iron Scrap Pile [opening-iron-v1]` – `Works Yard Copper Cable Spool [opening-copper-v1]` | 9, 0 | 9 | yes |
| `Works Yard Iron Scrap Pile [opening-iron-v1]` – `Works Yard Coal Fuel Bunker [opening-coal-v1]` | 9, 13 | 13 | yes |
| `Works Yard Copper Cable Spool [opening-copper-v1]` – `Works Yard Coal Fuel Bunker [opening-coal-v1]` | 0, 13 | 13 | yes |

Substation (93,350) 3×3, x 93..95, y 350..352: inside yard rect True, east edge touches the copper node's west edge (shared tile row 352): True.

D8: yard centre (99.5,363.5) is 29.4 tiles from the head and 73.8 from the mouth centre (72,432): PASS. D9: PASS.

## 7. Fate of existing objects

Every object whose rectangle or path touches the block rectangle, from the fresh compile, plus the Player Spawn transform.

| Name | Bracket key | Category | Fate | Reason |
|---|---|---|---|---|
| Home workshop [home-workshop] | home-workshop | Buildings (workshop) | keep in place | D25 workshop stays |
| Salvage garages [home-garage] | home-garage | Buildings (garage) | delete | M9: template only, copied |
| Foreman workshop [foreman-shelter] | foreman-shelter | Buildings (workshop) | keep and move | M9: to lot E2 |
| court house [court-house] | court-house | Buildings (house) | delete | M9: template only, copied |
| court garage [court-garage] | court-garage | Buildings (garage) | delete | M9: template only, copied |
| Court cottage [court-east-home] | court-east-home | Buildings (house) | delete | M9: template only, copied |
| Maintenance garage [court-west-garage] | court-west-garage | Buildings (garage) | delete | M9: template only, copied |
| Court gardener’s home [court-northwest-home] | court-northwest-home | Buildings (house) | delete | M9: template only, copied |
| Court brick home [court-west-home] | court-west-home | Buildings (house) | delete | M9: template only, copied |
| Approach home [court-approach-home] | court-approach-home | Buildings (house) | delete | M9: template only, copied |
| Court cottage [court-east-cottage] | court-east-cottage | Buildings (house) | delete | M9: template only, copied |
| Garden Cottage [court-garden-home] | court-garden-home | Buildings (house) | delete | M9: template only, copied |
| Court End home [court-southwest-home] | court-southwest-home | Buildings (house) | delete | M9: template only, copied |
| North Court home [court-north-home] | court-north-home | Buildings (house) | delete | M9: template only, copied |
| East Garden home [court-east-garden-home] | court-east-garden-home | Buildings (house) | delete | M9: template only, copied |
| Perimeter home 1 [court-perimeter-1] | court-perimeter-1 | Buildings (house) | delete | M10 perimeter home |
| Perimeter home 2 [court-perimeter-2] | court-perimeter-2 | Buildings (house) | delete | M10 perimeter home |
| Perimeter home 3 [court-perimeter-3] | court-perimeter-3 | Buildings (house) | delete | M10 perimeter home |
| Perimeter home 4 [court-perimeter-4] | court-perimeter-4 | Buildings (house) | delete | M10 perimeter home |
| Perimeter home 5 [court-perimeter-5] | court-perimeter-5 | Buildings (house) | delete | M10 perimeter home |
| Perimeter home 6 [court-perimeter-6] | court-perimeter-6 | Buildings (house) | delete | M10 perimeter home |
| Perimeter home 7 [court-perimeter-7] | court-perimeter-7 | Buildings (house) | delete | M10 perimeter home |
| Perimeter home 8 [court-perimeter-8] | court-perimeter-8 | Buildings (house) | delete | M10 perimeter home |
| Perimeter home 9 [court-perimeter-9] | court-perimeter-9 | Buildings (house) | delete | M10 perimeter home |
| Perimeter home 10 [court-perimeter-10] | court-perimeter-10 | Buildings (house) | delete | M10 perimeter home |
| Perimeter home 11 [court-perimeter-11] | court-perimeter-11 | Buildings (house) | delete | M10 perimeter home |
| Perimeter home 12 [court-perimeter-12] | court-perimeter-12 | Buildings (house) | delete | M10 perimeter home |
| debris [court-brush] | court-brush | Props (debris) | keep in place | D27 debris stays |
| tree [court-west-home:garden-tree] | court-west-home:garden-tree | Props (tree) | keep in place | M10 trees stay |
| tree [court-east-cottage:garden-tree] | court-east-cottage:garden-tree | Props (tree) | keep in place | M10 trees stay |
| tree [neighbourhood:court-tree-a] | neighbourhood:court-tree-a | Props (tree) | keep in place | M10 trees stay |
| tree [neighbourhood:court-tree-b] | neighbourhood:court-tree-b | Props (tree) | keep in place | M10 trees stay |
| fence [neighbourhood:court-west-garden-edge] | neighbourhood:court-west-garden-edge | Props (fence) | delete | M10 fence removed |
| fence [neighbourhood:court-west-garden-edge-2] | neighbourhood:court-west-garden-edge-2 | Props (fence) | delete | M10 fence removed |
| Perimeter fence 1 [court-perimeter-fence-1] | court-perimeter-fence-1 | Props (fence) | delete | M10 fence removed |
| Perimeter fence 2 [court-perimeter-fence-2] | court-perimeter-fence-2 | Props (fence) | delete | M10 fence removed |
| Perimeter fence 3 [court-perimeter-fence-3] | court-perimeter-fence-3 | Props (fence) | delete | M10 fence removed |
| Perimeter fence 4 [court-perimeter-fence-4] | court-perimeter-fence-4 | Props (fence) | delete | M10 fence removed |
| Perimeter fence 5 [court-perimeter-fence-5] | court-perimeter-fence-5 | Props (fence) | delete | M10 fence removed |
| Perimeter fence 6 [court-perimeter-fence-6] | court-perimeter-fence-6 | Props (fence) | delete | M10 fence removed |
| Perimeter fence 7 [court-perimeter-fence-7] | court-perimeter-fence-7 | Props (fence) | delete | M10 fence removed |
| Perimeter fence 8 [court-perimeter-fence-8] | court-perimeter-fence-8 | Props (fence) | delete | M10 fence removed |
| Perimeter fence 9 [court-perimeter-fence-9] | court-perimeter-fence-9 | Props (fence) | delete | M10 fence removed |
| Perimeter fence 10 [court-perimeter-fence-10] | court-perimeter-fence-10 | Props (fence) | delete | M10 fence removed |
| Perimeter fence 11 [court-perimeter-fence-11] | court-perimeter-fence-11 | Props (fence) | delete | M10 fence removed |
| Perimeter fence 12 [court-perimeter-fence-12] | court-perimeter-fence-12 | Props (fence) | delete | M10 fence removed |
| Perimeter fence 13 [court-perimeter-fence-13] | court-perimeter-fence-13 | Props (fence) | delete | M10 fence removed |
| Perimeter fence 14 [court-perimeter-fence-14] | court-perimeter-fence-14 | Props (fence) | delete | M10 fence removed |
| Perimeter fence 15 [court-perimeter-fence-15] | court-perimeter-fence-15 | Props (fence) | delete | M10 fence removed |
| Perimeter fence 16 [court-perimeter-fence-16] | court-perimeter-fence-16 | Props (fence) | delete | M10 fence removed |
| Perimeter fence 17 [court-perimeter-fence-17] | court-perimeter-fence-17 | Props (fence) | delete | M10 fence removed |
| Perimeter fence 18 [court-perimeter-fence-18] | court-perimeter-fence-18 | Props (fence) | delete | M10 fence removed |
| Perimeter fence 19 [court-perimeter-fence-19] | court-perimeter-fence-19 | Props (fence) | delete | M10 fence removed |
| Home workshop [home-workshop] | home-workshop | Sites and resources (Core) | keep in place | Core stays with workshop |
| Substation 0 [substation:0] | substation:0 | Sites and resources (Substation) | keep in place | M11 touches copper |
| Works Yard [yard:0] | yard:0 | Sites and resources (Yard) | keep and move | M11 yard to YARD |
| Founders Court [label:0] | label:0 | Sites and resources (Label) | keep in place | M10 light/label stays |
| Street light [light:0] | light:0 | Sites and resources (Light) | keep in place | M10 light/label stays |
| Street light [light:1] | light:1 | Sites and resources (Light) | keep in place | M10 light/label stays |
| Street light [light:2] | light:2 | Sites and resources (Light) | keep in place | M10 light/label stays |
| Street light [light:3] | light:3 | Sites and resources (Light) | keep in place | M10 light/label stays |
| Street light [light:4] | light:4 | Sites and resources (Light) | keep in place | M10 light/label stays |
| Street light [light:5] | light:5 | Sites and resources (Light) | keep in place | M10 light/label stays |
| Works Yard Iron Scrap Pile [opening-iron-v1] | opening-iron-v1 | Sites and resources (Resource) | keep in place | M11 already satisfied |
| Works Yard Copper Cable Spool [opening-copper-v1] | opening-copper-v1 | Sites and resources (Resource) | keep in place | M11 already satisfied |
| Works Yard Coal Fuel Bunker [opening-coal-v1] | opening-coal-v1 | Sites and resources (Resource) | keep in place | M11 already satisfied |
| Road 368 | — | Roads and paths (Road) | keep in place | Court road, rule 9 |
| Path 1 | — | Roads and paths (Path) | delete | replaced by fc-stub-WS |
| Path 2 | — | Roads and paths (Path) | delete | M10 touches deleted building |
| Path 3 | — | Roads and paths (Path) | delete | replaced by fc-stub-E2 |
| Path 4 | — | Roads and paths (Path) | delete | M10 touches deleted building |
| Path 5 | — | Roads and paths (Path) | delete | M10 touches deleted building |
| Path 26 | — | Roads and paths (Path) | delete | M10 touches deleted building |
| Path 27 | — | Roads and paths (Path) | delete | M10 touches deleted building |
| Path 39 | — | Roads and paths (Path) | delete | M10 touches deleted building |
| Path 40 | — | Roads and paths (Path) | delete | M10 touches deleted building |
| Path 41 | — | Roads and paths (Path) | delete | M10 touches deleted building |
| Path 42 | — | Roads and paths (Path) | delete | M10 touches deleted building |
| Path 61 | — | Roads and paths (Path) | delete | M10 touches deleted building |
| Path 62 | — | Roads and paths (Path) | delete | M10 touches deleted building |
| Path 63 | — | Roads and paths (Path) | delete | M10 touches deleted building |
| Path 64 | — | Roads and paths (Path) | delete | M10 touches deleted building |
| Path perimeter 1 | — | Roads and paths (Path) | delete | M10 perimeter stub |
| Path perimeter 2 | — | Roads and paths (Path) | delete | M10 perimeter stub |
| Path perimeter 3 | — | Roads and paths (Path) | delete | M10 perimeter stub |
| Path perimeter 4 | — | Roads and paths (Path) | delete | M10 perimeter stub |
| Path perimeter 5 | — | Roads and paths (Path) | delete | M10 perimeter stub |
| Path perimeter 6 | — | Roads and paths (Path) | delete | M10 perimeter stub |
| Path perimeter 7 | — | Roads and paths (Path) | delete | M10 perimeter stub |
| Path perimeter 8 | — | Roads and paths (Path) | delete | M10 perimeter stub |
| Path perimeter 9 | — | Roads and paths (Path) | delete | M10 perimeter stub |
| Path perimeter 10 | — | Roads and paths (Path) | delete | M10 perimeter stub |
| Path perimeter 11 | — | Roads and paths (Path) | delete | M10 perimeter stub |
| Path perimeter 12 | — | Roads and paths (Path) | delete | M10 perimeter stub |
| Player Spawn | — | Transform (PLAYER SPAWN) | keep in place | D25 spawn stays |

Delete count: 73
Keep count: 22 (20 keep in place, 2 keep and move)

## 8. Picture

`Unity/Docs/FOUNDERS-COURT-PLAN.png` — drawn by `Unity/Relight/Temp/fc_draw.py` from `FOUNDERS-COURT-PLAN.json`, 1 px per tile scaled ×8, legend at the bottom.

## 9. Gaps in this plan

- M12 gives one sidewalk key per side (`fc-sidewalk-<side>`), but the south strip is split by the road band, so it is two objects: `fc-sidewalk-south-w` and `fc-sidewalk-south-e`.
- No `ScenePath` kind is 2 tiles wide (Road 10, Drive 4, Path 1.4), so the sidewalk is five `SceneArea` strips (decor squares, no gameplay effect), as M2 allows.
- The block rectangle's east column x 121 and south row y 427 are the ring roads' outer pavement tiles (terrain Street). The east and south sidewalk strips include them, so 2 of each strip's 2 tiles overlap ring-road pavement there. Kept as written because heading 2 fixes the block rectangle.
- M4 gives the workshop lot frontage D37 = 16 in x (x 64..79), which spans the whole circle (x 67..76). Read literally, CW and CE could then never touch the circle, contradicting D44. Resolution used: the workshop lot ends at the circle's north edge (y 368) across its full width, and the tiles beside the circle (x 64..66 and x 77..79, y 369..378) belong to CW and CE. CW and CE are therefore L-shaped (two rectangles each) and touch the circle along x 66|67 and x 76|77, rows 371..376.
- Circle tiles are defined as tiles whose centre lies strictly inside radius 5.5 of (72,374): rows 369..378, x 67..76, 88 tiles (row 369 is x 69..74). The road band (court road plus its drawn 5-tile north extension) is the rectangle x 67..76, y 369..427, which contains every circle tile; the straight part is y 379..427.
- YARD (CE merged with E3) is L-shaped: x 80..118 for y 332..368 plus x 77..118 for y 369..394. A `SceneSite` rectangle cannot equal it, so the Works Yard site rectangle is the largest rectangle inside YARD, (80,332) 39×63; the 3×26 strip x 77..79, y 369..394 stays YARD lot but is not paved. C9's 'yard rectangle equals YARD' will be checked against this rectangle.
- Template roof keys come from the fresh compile. `court house [court-house]` is rf-house-E-roof-1 in the scene (heading 2 of the assessment listed rf-house-roof-1; Phase 2 turned its door east).
- Lot CW: house `Court End home [court-southwest-home]` is 10 tiles along a frontage of 6; M7 asks for a narrower template when wider than frontage−2 = 4, and no template in the list is that narrow. Placed centred anyway.
- Home workshop: D25 keeps it in place, so its front wall (y 360) is 8 tiles from the circle edge (y 369), not D39 = 4. C5 will report the workshop as an exception, not a failure, unless the human says otherwise.
- M6 does not say which of the two lots' tile rows the 1-tile side fence occupies. Used: the fence lies inside the lot whose frontage is nearer the mouth (the same lot whose front yard line ends it), and never inside YARD (M6) — so `fc-sidefence-WS-YARD` lies in WS at x 79.
- `fc-sidefence-WS-CW` and `fc-sidefence-WS-YARD` run north–south, so they can never meet CW's or YARD's front yard line (x 62 / x 81). All three lots front the circle, so M6's 'whichever is reached first' clause was applied: they stop at the workshop lot's front yard line, y 364. The short east–west part of each shared edge (row 368|369, x 64..66 and x 77..79) lies south of that line, in the open front yards, and gets no fence.
- The three nodes and the substation already satisfy M11 inside the new yard rectangle (D9 gaps and ≥2 from every edge), so only the Works Yard site moves; nodes and substation keep their rectangles.

## 10. Questions for the human

1. Approve the fate table under heading 7 as written, or list changes in the go message.
2. The Home workshop's front wall is 8 tiles from the circle edge (D25 keeps it in place, D39 says 4). Should Verify C5 treat the workshop as an exception (planned), or should the workshop lot's front yard line be redefined?
3. CW's frontage on the circle is 6 tiles (rows 371..376), so every template is taller than frontage−2 (M7). The plan places `Court End home` (10×10) centred on it anyway. Accept, or name a different rule for circle lots?
4. YARD is L-shaped; the yard site rectangle is (80,332) 39×63, leaving the 3×26 strip x 77..79, y 369..394 unpaved. Accept?
5. The W1–W2 side fence (row 410) passes over the tree `tree [neighbourhood:court-tree-b]` at (44,410) 2×2, and the yard rectangle covers `tree [court-east-cottage:garden-tree]` at (83,380). Trees keep in place per M10. Accept, or delete those two trees?
