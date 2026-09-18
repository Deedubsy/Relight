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
| E2 | `Foreman workshop [foreman-shelter]` | rf-workshop-roof-0 | 12×9 | rf-workshop-roof-0 | (81,398) 12×9, x 81..92, y 398..406 | (85,406), (86,406), (87,406) on the south wall | `fc-stub-E2` | (86,407) | (76,407), via (86,407): L stub south then west, 3 points | keep and move; D45 applied |
| CW | `Court End home [court-southwest-home]` | rf-house-roof-1 | 10×10 | rf-house-E-roof-1 | (53,369) 10×10, x 53..62, y 369..378 | (62,372), (62,373), (62,374) | `fc-stub-CW` | (63,373) | (67,373) | new copy |
| WS | `Home workshop [home-workshop]` | rf-workshop-roof-1 | 10×14 | rf-workshop-roof-1 | (65,347) 10×14, x 65..74, y 347..360 | (69,360), (70,360), (71,360) | `fc-stub-WS` | (70,361) | (70,369) | keep in place |

Stubs are straight `ScenePath` kind Path from the tile in front of the door to the first road band / circle tile. Exception under D45: `fc-stub-E2` is one 3-point polyline (86,407) → (86,407) → (76,407): south leg from the tile below the middle south-wall door tile to the row below the south wall (the same row 407, so that leg is zero length), then west to the road band edge x 76.

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
| tree [court-east-cottage:garden-tree] | court-east-cottage:garden-tree | Props (tree) | delete | go message answer 1 (under a side fence / the yard) |
| tree [neighbourhood:court-tree-a] | neighbourhood:court-tree-a | Props (tree) | keep in place | M10 trees stay |
| tree [neighbourhood:court-tree-b] | neighbourhood:court-tree-b | Props (tree) | delete | go message answer 1 (under a side fence / the yard) |
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

Delete count: 75
Keep count: 20 (18 keep in place, 2 keep and move)

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
## 11. Works Yard property

D48: the Works Yard is a separate fenced property, not a court lot. Its site rectangle is unchanged from heading 6; the YARD lot is removed from the lot table and the two side fences `fc-sidefence-WS-YARD` and `fc-sidefence-E2-YARD` are replaced by the yard's own west edge and south edge (heading 12). Road band edge for the verge: x 76 (lot E2 'Road edge', heading 3). Circle's first row: y 369.

| Item | Rectangle | Tiles |
|---|---|---|
| Yard rectangle | (80,332) 39×63, x 80..118, y 332..394 | 2457 |
| Yard west edge (column one tile west of the yard rect) | (79,332) 1×63, x 79..79, y 332..394 | 63 |
| Yard south edge (row one tile south of the yard rect, from the west edge column to the yard's east column) | (79,395) 40×1, x 79..118, y 395..395 | 40 |
| Yard north run (compound fence north row above the yard rect's columns) | (80,331) 39×1, x 80..118, y 331..331 | 39 |
| Yard east run (compound fence east column beside the yard rect's rows) | (119,332) 1×63, x 119..119, y 332..394 | 63 |
| Verge (between the road band edge and the yard west edge, from the circle's first row to the yard's south row) | (77,369) 2×26, x 77..78, y 369..394 | 52 |

Lot VERGE: key `fc-lot-VERGE`, rectangle (77,369) 2×26, x 77..78, y 369..394, court ground; no house, no stub, no side fence (M5). Plan data: `lots` gains VERGE and loses YARD; `sideFences` loses `fc-sidefence-WS-YARD` and `fc-sidefence-E2-YARD`; `yard` gains `westEdge`, `southEdge`, `northRun`, `eastRun`, `verge`; the lots W1, W2, W3, E1, E2, WS, CW are unchanged.

Coverage check (Phase 3 rule, M5: every interior tile in exactly one lot or the yard rectangle, or a fence tile): 8089 interior tiles; 8063 in exactly one of W1, W2, W3, E1, E2, WS, CW, VERGE or the yard rectangle; 22 fence tiles in none (the yard west edge x 79, y 369..394 outside the gap); 0 tiles in two; 0 tiles in none and not fence. The 4 gap tiles (79,372), (79,373), (79,374), (79,375) are in no lot, not in the yard rectangle and not fence tiles (they hold the non-blocking debris placeholder): see heading 14. Result: PASS apart from the gap tiles.

## 12. Yard fence, gap and gate

M1: the yard west edge and south edge are new 1-thick fence runs; `fc-sidefence-WS-YARD` (79,332) 1×33 and `fc-sidefence-E2-YARD` (81,395) 38×1 are deleted and replaced by them. `fc-fence-north-1` and `fc-fence-east-1` are split at the yard's boundary. Keys per M6.

| Key | Run | Start | End | Rect | Tiles |
|---|---|---|---|---|---|
| `fc-fence-north-yard` | north run | (80,331) | (118,331) | (80,331) 39×1, x 80..118, y 331..331 | 39 |
| `fc-fence-east-yard-1` | east run — north of the gate; includes the compound NE corner tile (119,331) | (119,331) | (119,360) | (119,331) 1×30, x 119..119, y 331..360 | 30 |
| `fc-fence-east-yard-2` | east run — south of the gate | (119,365) | (119,394) | (119,365) 1×30, x 119..119, y 365..394 | 30 |
| `fc-yardfence-west-1` | west edge — north of the gap | (79,332) | (79,371) | (79,332) 1×40, x 79..79, y 332..371 | 40 |
| `fc-yardfence-west-2` | west edge — south of the gap | (79,376) | (79,394) | (79,376) 1×19, x 79..79, y 376..394 | 19 |
| `fc-yardfence-south` | south edge | (79,395) | (118,395) | (79,395) 40×1, x 79..118, y 395..395 | 40 |

Gap (D49, M2): yard west edge column x 79; circle row y 374 and the row above it y 373, plus one row above (y 372) and one below (y 375); centred on the boundary between rows 373 and 374. Four tiles: (79,372), (79,373), (79,374), (79,375) = rect (79,372) 1×4, x 79..79, y 372..375. No fence tile on them; one debris object (D51 placeholder, SceneProp kind `debris`, `blocksMovement` 0) covers exactly those four, name "Works Yard broken fence" (go message, 2026-09-18), key `fc-yard-gap`.

Gate (D50, M3): yard east run column x 119; yard rect rows 332..394 (63 rows), middle row 363; four tiles centred on it, rounded toward north: rows 361..364 (centre 362.5). Tiles: (119,361), (119,362), (119,363), (119,364) = rect (119,361) 1×4, x 119..119, y 361..364. No fence tile there; one fence object (SceneProp kind `fence`, `blocksMovement` 1) covering exactly those four, name "Works Yard gate", key `fc-yard-gate`.

Splits of the compound fence:

| Old key | Old rect | New key | Part | Rect |
|---|---|---|---|---|
| `fc-fence-east-1` | (119,331) 1×95, x 119..119, y 331..425 | `fc-fence-east-yard-1` | yard — north of the gate; includes the compound NE corner tile | (119,331) 1×30, x 119..119, y 331..360 |
| `fc-fence-east-1` | (119,331) 1×95, x 119..119, y 331..425 | `fc-fence-east-yard-2` | yard — south of the gate | (119,365) 1×30, x 119..119, y 365..394 |
| `fc-fence-east-1` | (119,331) 1×95, x 119..119, y 331..425 | `fc-fence-east-court` | court | (119,395) 1×31, x 119..119, y 395..425 |
| `fc-fence-north-1` | (26,331) 93×1, x 26..118, y 331..331 | `fc-fence-north-court` | court | (26,331) 54×1, x 26..79, y 331..331 |
| `fc-fence-north-1` | (26,331) 93×1, x 26..118, y 331..331 | `fc-fence-north-yard` | yard | (80,331) 39×1, x 80..118, y 331..331 |

Compound fence tile count: 366 before; 362 in the eight `fenceLine` segments after, plus 4 gate tiles = 366, the same tiles. Court fence = `fc-fence-west-1`, `fc-fence-north-court`, `fc-fence-east-court`, `fc-fence-south-1`, `fc-fence-south-2` and the five unchanged side fences; yard fence = the six runs above. Plan data: `yard.yardFences`, `yard.yardGap`, `yard.yardGate`; `fenceLine.segments` now holds the split parts.

## 13. Yard office

| Field | Value |
|---|---|
| Footprint areas (heading 4 houses) | W1 `fc-house-W1` 12×7 = 84, W2 `fc-house-W2` 10×9 = 90, W3 `fc-house-W3` 10×9 = 90, E1 `fc-house-E1` 10×9 = 90, E2 `fc-house-E2` 12×9 = 108, CW `fc-house-CW` 10×10 = 100, WS `fc-house-WS` 10×14 = 140 |
| Source house (smallest footprint, M4) | `fc-house-W1` (lot W1, copy of `Salvage garages [home-garage]`, kind garage, variant 2), 12×7 = 84 tiles |
| Office rectangle | (106,353) 12×7, x 106..117, y 353..359; east wall x 117, one tile west of the yard east run x 119 (tile x 118 between); south wall row y 359, two rows above the gate's top row y 361 (go message, 2026-09-18; supersedes the M4 centring on the gate rows, which gave (106,359)) |
| Door | west wall x 106, centred: local [0, 2, 1, 3], tiles (106,355), (106,356), (106,357) (the source's door is 1×3 on its east wall; same length, moved to the west wall) |
| Stub | none (M4) |
| Roof key | `rf-garage-roof-2` (source `rf-garage-roof-2`; west-door key `rf-garage-W-roof-2` does not exist in the scene, so unchanged) |
| Name / key | "Works Yard office" / `fc-yard-office`; not enterable (D53) |
| Moves south | 0 (M4 rule); then moved north 6 rows by the go message so the south wall is two rows above the gate |

| Check | Result |
|---|---|
| office inside yard rect | PASS |
| no overlap with Works Yard Iron Scrap Pile [opening-iron-v1] | PASS |
| no overlap with Works Yard Copper Cable Spool [opening-copper-v1] | PASS |
| no overlap with Works Yard Coal Fuel Bunker [opening-coal-v1] | PASS |
| no overlap with substation | PASS |
| no overlap with gate | PASS |
| ≥ 4 from Works Yard Iron Scrap Pile [opening-iron-v1] (axis gap 19, -3; largest 19) | PASS |
| ≥ 4 from Works Yard Copper Cable Spool [opening-copper-v1] (axis gap 7, -3; largest 7) | PASS |
| ≥ 4 from Works Yard Coal Fuel Bunker [opening-coal-v1] (axis gap 7, 9; largest 9) | PASS |

Plan data: `yard.office` with `rect`, `doorTiles`, `roofKey`, `sourceKey` (and `doorsLocal`, `doorWall`, `size`, `kind`, `variant`, `name`, `key`).

## 14. Gaps in the yard plan

1. **Coverage: the four gap tiles.** (79,372), (79,373), (79,374), (79,375) lie in no lot, outside the yard rectangle, and are not fence tiles (they hold the non-blocking debris placeholder, D49/D51). Every other interior tile passes the M5 rule. They are the broken section of the yard west edge; no lot was widened to take them (rule 9).
2. **East yard fence key.** M6 names one east part, `fc-fence-east-yard`, but the gate (M3) breaks the yard east run into two fence objects. Used `fc-fence-east-yard-1` (north of the gate) and `fc-fence-east-yard-2` (south of the gate), the same pattern M6 gives for the west edge. The name is not in M6.
3. **North-east corner tile (119,331).** By the definitions it is in neither the yard north run (x 80..118) nor the yard east run (y 332..394), so it is court fence; splitting `fc-fence-east-1` strictly would leave a one-tile court piece between the yard's north and east fences. It is placed in `fc-fence-east-yard-1`, which therefore starts at y 331. Change the plan data if it must be a court fence object.
4. **M4 "heading 9 of Part A".** Part A adds headings 11–14 only; no heading 9 is written in this pass. The office overlapped nothing, so it moved 0 rows; recorded here and as `yard.office.movesSouth`.
5. **Smallest house is a garage.** M4 says the smallest footprint area; that is `fc-house-W1` (12×7 = 84, kind garage, copy of `Salvage garages [home-garage]`). If "house" excludes garages, the smallest are W2, W3 and E1 at 10×9 = 90 (tie). W1 was used.
6. **Gap object name.** The gap had a key (`fc-yard-gap`) but no object name in the decisions. Resolved by the go message (2026-09-18): "Works Yard broken fence".
7. **VERGE as a scene object.** Lots are plan data only: the generator creates no `fc-lot-*` objects (Phase 3 C12 counts 29 objects without lots), so `fc-lot-VERGE` is a plan key unless the go message says a scene object is wanted.
8. **Old lot records.** E3 and CE stay in the plan data marked `merged_into: YARD` as history; YARD itself is removed. `yard.lot` and `yard.lotRects` (the YARD lot's rectangles) are removed and `yard.property` added. The generator's step 7 and C9 read `yard.siteName`, `rect`, `nodes` and `substation` only, which are unchanged.
9. **Fence tiles inside unchanged lots.** The yard west edge x 79, y 332..368 lies inside lot WS and the yard south edge y 395, x 79..118 lies inside lot E2 (both lots unchanged, rule 9), exactly where the two deleted side fences lay. The coverage rule accepts a fence tile inside one lot.
10. **Gate tiles and the compound fence count.** The four gate tiles are in `yard.yardGate`, not `fenceLine.segments`, so `fenceLine.tileCount` is 362 (was 366); the gate makes up the difference. Part B's C2 count line will change accordingly.
11. **D49 wording.** D49 says the gap is centred on the circle's row; M2 centres it on the boundary between the circle row (374) and the row above (373), giving rows 372..375. M2 is the stated method and was used.
12. **Read-only Editor query.** To apply M4's roof rule the open Editor was asked (eval) for the roof keys of every `SceneBuilding`; `rf-garage-W-roof-2` is not among them, so the office keeps `rf-garage-roof-2`. Nothing in the scene, assets or code was changed in Part A.
