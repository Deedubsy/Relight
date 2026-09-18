# Founders Court decisions

| ID | Decision | Value | In this pass |
|---|---|---|---|
| D1 | What blocks enemies | Houses and fences. Nothing else. Amended by D17. | Yes |
| D2 | Do fences block the player too | Yes. One rule for everyone. The player leaves the block through the mouth. | Yes |
| D3 | Perimeter rule | Every segment of the perimeter is covered by a house or a fence. The only gap is the mouth. | Yes |
| D4 | House spacing on the perimeter | Add houses until the perimeter is lined. About two thirds of neighbouring houses touch. About one third have a gap, and every gap is closed by a fence. | Yes |
| D5 | Interior houses | Only along the court road, doors facing the road, one driveway stub each. No house sits on the road. | Yes |
| D6 | Driveways to ring roads | Perimeter houses may keep outward driveways. Those driveways are outside the block and do not matter for defence. | Yes |
| D7 | The paved lot becomes a Works Yard | Rename it. Keep the paving. Nodes: iron = scrap pile, copper = cable spool next to the transformer, coal = fuel bunker. | Yes |
| D8 | Works Yard position | Inside the perimeter, between the head and the mouth, closer to the head. | Yes |
| D9 | Node spacing | Each node is at least 8 belt tiles from the workshop input. Nodes are at least 4 belt tiles apart. | Yes |
| D10 | Objects with no job | Tiled footpath, tall grey bar, triangle: the AI reports their GameObject names first. Default is remove unless the human says otherwise. Resolved by D27 and D28. | Yes |
| D11 | Court road width | Half the ring road width. Deferred by D23. | No |
| D12 | Enemy road preference | Grass costs 4 times road cost in enemy pathing. Applies map-wide. | No |
| D13 | Spawn points | Player spawn stays on the workbench. Enemy spawn stays at the mouth. Nothing else changes about waves. Amended by D19 and D25. | Yes |
| D14 | Works Yard as a map-wide site type | Every paved lot on the map becomes a Works Yard with nodes. | No |
| D15 | Project paths | Project root is `Unity/Relight`. Editor script at `Unity/Relight/Assets/Editor/FoundersCourtChecks.cs`. Docs stay in `Unity/Docs`. | Yes |
| D16 | What a fence is | A `SceneProp` of kind `fence` with `blocksMovement` 1. No prefab. The two existing fences at x 27 get `blocksMovement` 1 and stay. | Yes |
| D17 | D1 amended | Buildings, fence props, and the substation block. Trees and debris do not. | Yes |
| D18 | Baseline solids | Remove every baseline solid tile inside the block rectangle so the only solids come from objects in the scene. Verify with a compile before and after. | Yes |
| D19 | Raid line | Move the raid line to the mouth on the south ring road centre line, y 432, x 69 to 74. | Yes |
| D20 | Entry step cap | Measure the walk steps from the core to the mouth after the perimeter is closed. If the cap of 68 is less than that plus 10: raise the cap in `DirectorRules` to that plus 10 and report the old and new value. If not: leave it. | Yes |
| D21 | "House" in D3, D4, D5 | Any `SceneBuilding`. Garages count. | Yes |
| D22 | Maintenance garage | Move it to touch the Salvage garages so it becomes part of the workshop lot. | Yes |
| D23 | D11 court road width | Defer. Record as not done. Per-path road width becomes its own task later. | No |
| D24 | "Workshop input" in D9 | The workshop door tiles. Already satisfied. Verify and change nothing. | Yes |
| D25 | D13 player spawn | Stays where it is, on the workshop door tile. | Yes |
| D26 | Gap check method | Band projection per edge on the compiled `Solid` grid. Gap threshold 1 tile. | Yes |
| D27 | Debris | Keep `debris [court-brush]`. Clearing debris is taught. Its `blocksMovement` stays 0 under D17. Its baseline solid tiles are removed under D18. | Yes |
| D28 | Objects to remove | `Court garden walk`, `Drive 1`. Keep the street lights and the label. | Yes |
| D34 | Compound fence | One continuous fence around the block, inset from the block edge by the sidewalk width, open only across the road band at the mouth. Fence props with `blocksMovement` 1. | Yes |
| D35 | Sidewalk width | 2 tiles. A paved strip along the block edge outside the fence. | Yes |
| D36 | Lots | The interior is divided into lots. Along the straight part: rectangular lots of equal frontage on each side, from the mouth to the circle. Around the circle: the workshop lot centred on the road centre, and lots either side of it. | Yes |
| D37 | Lot frontage | 16 tiles along the road. Lot depth: from the road edge to the compound fence. | Yes |
| D38 | Side fences | A fence between every pair of neighbouring lots, from the compound fence to the front yard line. Front yards are open to the road. | Yes |
| D39 | Front setback | 4 tiles from the road edge to the house's front wall. House centred on the lot's frontage. Door on the road-facing wall. One stub from the door to the road. | Yes |
| D40 | Works Yard lots | Two adjacent lots in one corner, merged. Nodes and substation inside. No house. | Yes |
| D41 | Houses | One per lot, drawn from the existing non-enterable buildings, no two neighbours with the same roof key. The Foreman workshop keeps its lot. | Yes |
| D42 | Gap test | Temporarily mark the mouth solid, flood fill from a ring road tile, report every interior tile reached. Zero means closed. | Yes |
| D43 | Build as a generator | An editor command that takes a block rectangle and a road polyline, so it can be run on other blocks later. Founders Court is the first run. | Yes |
| D44 | Houses on the circle | Three lots touch the circle: the workshop lot north of the circle, one lot west of it, one lot east of it. Each house is axis aligned with its door on the wall facing the circle. No other lot touches the circle. | Yes |
| D45 | Foreman workshop door | South wall. Stub runs south from the door, then west to the road band. | Yes |
| D46 | Baseline decor | Every baseline decor entry inside the block rectangle is cleared. The generator clears baseline decor and baseline solids inside its block before it builds. | Yes |
| D47 | Fence art | Fence art uses no colour shared with path or sidewalk art. | No |
| D48 | Works Yard ownership | The yard is a separate fenced property, not a court lot. It shares a fence with the court on its west and south edges. | Yes |
| D49 | Broken section | One gap of 4 tiles in the yard's west fence, centred on the circle's row. The gap tiles hold a non-blocking broken fence prop. | Yes |
| D50 | Yard outer fence | Intact on the north and east. A closed gate object on the east fence, 4 tiles wide, solid, not openable yet. | Yes |
| D51 | Broken fence art | Until broken fence art exists, the gap uses the debris prop as a placeholder. Recorded as an art task with D47. | Yes |
| D52 | Closure test | Unchanged. The flood fill treats court plus yard as one enclosure. | Yes |
| D53 | Yard office | One building inside the yard, against the east fence beside the gate, door on the west wall facing into the yard. Copied from the smallest house in the block. Not enterable yet. Named "Works Yard office". | Yes |
| D54 | First attack origin | The introductory attack, and every raid after it, is born at or below the raid line on the south ring road, never inside the block. The director's distance field reaches 88 tiles beyond every edge of the core rect (`RaidField.Reach`, was a 70-tile box measured from the core's top-left tile, which stopped at row 417 and left the raid line at row 432 unreachable, so the first group was born at (62,347) beside the workshop). `Origin` skips tiles that are not entry tiles instead of falling back to the cheapest interior tile. Verify C20 checks the origin on every run; the `Relight/Gizmos/Raid Director` Scene gizmo shows it. Record: `FOUNDERS-COURT-SPAWN-FIX.md`. | Yes |
| D55 | Substation powers the court lights | Accepted design, not yet implemented. The authored `substation:0` site at (93,350) becomes a reach-8 power node; every authored streetlight is fed by its nearest substation site (the court's six map to `substation:0`) and bills 2 kW to that circuit instead of joining the nearest pole; opening objective 8 completes on the site's own circuit having supply and its text drops the core/"no power" claims. Two or three Light sites on the circle at (72,374) may be added with the implementation. The core keeps no power demand. Record: `FOUNDERS-COURT-SUBSTATION.md`. | Yes (design) |

D3, D4 and D26 are superseded by D34, D36, D38 and D42. D16 is amended by D34.
D39 is amended for the Foreman workshop by D45.
D34 is amended by D48: the fence tiles beside the yard belong to the yard fence.
D19 is completed by D54: the raid line at row 432 is now inside the director's field, and the first attack enters there.
