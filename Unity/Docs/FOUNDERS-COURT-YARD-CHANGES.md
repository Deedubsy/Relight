# Founders Court yard changes

Phase 5, Part B: the Works Yard applied as its own property (D48–D53) through the generator, from the Part A plan data in `Unity/Docs/FOUNDERS-COURT-PLAN.json`. Run on 2026-09-18 against the open Editor (Unity 6000.6.0f1, project `Unity/Relight`, scene `Assets/Relight/Scenes/World.unity`). Nothing is committed.

An earlier "go apply" stopped at Step B1 because the Phase 4 changes were uncommitted; the human committed them as `9926fed3` and sent "go apply" again. This report is for the second run. At its Step B1 `git status --porcelain`, ignoring `Unity/Docs/` lines, printed nothing.

## 1. Decisions file

`Unity/Docs/FOUNDERS-COURT-DECISIONS.md`

- D48, D49, D50, D51, D52 and D53 appended verbatim as table rows after D47 (lines 47–52).
- The line "D34 is amended by D48: the fence tiles beside the yard belong to the yard fence." added after the existing amendment lines (line 56).
- Nothing else in the file changed.

## 2. Generator changes

File: `Unity/Relight/Assets/Editor/FoundersCourtCompound.cs` (the only code file touched). Compile confirmed through the CLI after each edit: `recompile` then `recompile_status` returned `status completed, failed false, errors [], compilationFailed false` three times (initial B2 edit, the duplicate-fence fix, the C19 fix).

Generate (`Relight/Founders Court/Generate Compound` and `From File`):

1. **Split fence parts.** No code change needed: step 4 already creates one fence object per `fenceLine.segments` entry, and the Part A plan holds the split east fence (`fc-fence-east-yard-1`, `fc-fence-east-yard-2`, `fc-fence-east-court`) and split north fence (`fc-fence-north-court`, `fc-fence-north-yard`) instead of `fc-fence-east-1` and `fc-fence-north-1`. Step 1 deletes every `[fc-` object first, so the two old whole fences go and the five parts are created.
2. **The two YARD side fences.** Step 1 deletes them as `[fc-` objects and the plan's `sideFences` no longer lists them, so they are not recreated. A new step 8 also looks for `fc-sidefence-WS-YARD` and `fc-sidefence-E2-YARD` by key and deletes any that remain, logging "not present" otherwise.
3. **Yard fence runs.** Step 8 reads `yard.yardFences`. Entries whose key is also a `fenceLine.segments` key (the north run and the two east-run parts) are logged as "compound fence segment, already generated" and skipped, so each shared tile has one fence object. The remaining entries (`fc-yardfence-west-1`, `fc-yardfence-west-2`, `fc-yardfence-south`) are created through the existing `Fence` helper: SceneProp kind `fence`, `blocksMovement` 1, `clearable` 0, named "Yard fence <side> [key]".
4. **Gap.** `yard.yardGap` becomes one SceneProp kind `debris`, `blocksMovement` 0, `clearable` 0, over exactly the four gap tiles, named "Works Yard broken fence [fc-yard-gap]" (D49, D51).
5. **Gate.** `yard.yardGate` becomes one SceneProp through `Fence`: kind `fence`, `blocksMovement` 1, over exactly the four gate tiles, named "Works Yard gate [fc-yard-gate]" (D50).
6. **Office.** `yard.office` becomes one SceneBuilding under "Buildings", named "Works Yard office [fc-yard-office]", `buildingName` "Works Yard office", kind and variant copied from the scene building whose id is `yard.office.sourceKey` (`fc-house-W1`, the Salvage garages copy; plan fields are the fallback if it is absent), size from the plan rect (12×7), `doors` from `doorsLocal` ((0,2) 1×3, west wall), `roofKey` from the plan (`rf-garage-roof-2`), `enterable` false, `campaignBuilding` false (D53).
7. **VERGE lot.** Lots have no scene object (Part A gap 7, accepted). Step 8 logs "lot VERGE [fc-lot-VERGE] (77,369) 2x26: plan data only, lots have no scene object" and creates nothing.
8. Step 8 runs after the site moves and before the existing MarkSceneDirty / log flush / `Relight/World/Refresh Editable World` / SaveOpenScenes sequence, so every Generate run still ends with Refresh and a save.

Verify (`Relight/Founders Court/Verify`):

1. **C1 compound closed** now counts the enclosure: court interior plus the yard rect plus the verge rect (D52). A reached verge or yard tile is a failure. The detail line reports the reached count split into court, verge and yard.
2. **C12 plan match** adds the yard fence keys, the gap key, the gate key and the office key with their plan rects to the expected set, so the new objects are planned rather than "unplanned".
3. **C15 yard boundary**: every tile of `yard.westEdge`, `southEdge`, `northRun` and `eastRun` must be covered by exactly one object, and that object must be the gap prop (kind debris, non-blocking) on a gap tile, the gate prop (kind fence, blocking) on a gate tile, or a fence prop whose key is one of `yard.yardFences` (kind fence, blocking) elsewhere. Any other object on those tiles, a missing object, or a second object fails the check.
4. **C16 gap open**: BFS over tiles not solid in the compiled grid, 4-connected, from the circle centre tile (72,374), limited to the block plus 15 tiles; passes when at least one reached tile is inside the yard rect.
5. **C17 gap only**: the same BFS with the four gap tiles treated as solid; passes when no reached tile is inside the yard rect.
6. **C18 gate solid**: all four gate tiles solid in the compiled grid.
7. **C19 office**: the building with key `fc-yard-office` exists, its rect is inside the yard rect, every door tile lies on its west wall column, it does not overlap any plan node rect, the plan substation rect, any scene site that is a node or Substation, or the gate rect/gate object, and it is not enterable.
8. The last Verify line now reads "… of nineteen checks".

Two generator faults were found while running B3/B4 and fixed in the generator only (never in the scene by hand):

- First Generate run created the north run and the two east-run parts twice (once from `fenceLine.segments`, once from `yard.yardFences`), 39 fc objects instead of 36. Fix: the skip described in item 3 above. Generate was rerun; its step 1 removed the 39 objects including the duplicates.
- First Verify run failed C19 only, with "overlaps Works Yard [yard:0] (80,332) 39x63": the check compared the office against every non-Core site, and the office is inside the yard site by design. Fix: the overlap set is now nodes, the substation and the gate, as the check is specified. Generate and Verify were rerun (B3 then B4).

## 3. Generated and deleted objects

From the final Generate run's log (`Unity/Relight/Temp/fc_generate_log.txt`, ignored by git). Tile rects are `(x,y) w×h`, pivot NW corner.

Deleted (36 earlier `fc-` objects, the previous run's output, removed before regeneration):

```
deleted fc Salvage garages [fc-house-W1] [fc-house-W1] at (51,414)
deleted fc North Court home [fc-house-W2] [fc-house-W2] at (53,398)
deleted fc Court gardener’s home [fc-house-W3] [fc-house-W3] at (53,382)
deleted fc Court brick home [fc-house-E1] [fc-house-E1] at (81,413)
deleted fc Court End home [fc-house-CW] [fc-house-CW] at (53,369)
deleted fc Works Yard office [fc-yard-office] [fc-yard-office] at (106,353)
deleted fc Compound fence west [fc-fence-west-1] [fc-fence-west-1] at (25,331)
deleted fc Compound fence east [fc-fence-east-yard-1] [fc-fence-east-yard-1] at (119,331)
deleted fc Compound fence east [fc-fence-east-yard-2] [fc-fence-east-yard-2] at (119,365)
deleted fc Compound fence east [fc-fence-east-court] [fc-fence-east-court] at (119,395)
deleted fc Compound fence north [fc-fence-north-court] [fc-fence-north-court] at (26,331)
deleted fc Compound fence north [fc-fence-north-yard] [fc-fence-north-yard] at (80,331)
deleted fc Compound fence south [fc-fence-south-1] [fc-fence-south-1] at (26,425)
deleted fc Compound fence south [fc-fence-south-2] [fc-fence-south-2] at (77,425)
deleted fc Side fence W1-W2 [fc-sidefence-W1-W2] [fc-sidefence-W1-W2] at (26,410)
deleted fc Side fence W2-W3 [fc-sidefence-W2-W3] [fc-sidefence-W2-W3] at (26,395)
deleted fc Side fence W3-CW [fc-sidefence-W3-CW] [fc-sidefence-W3-CW] at (26,379)
deleted fc Side fence E1-E2 [fc-sidefence-E1-E2] [fc-sidefence-E1-E2] at (81,410)
deleted fc Side fence WS-CW [fc-sidefence-WS-CW] [fc-sidefence-WS-CW] at (63,332)
deleted fc Yard fence west edge [fc-yardfence-west-1] [fc-yardfence-west-1] at (79,332)
deleted fc Yard fence west edge [fc-yardfence-west-2] [fc-yardfence-west-2] at (79,376)
deleted fc Yard fence south edge [fc-yardfence-south] [fc-yardfence-south] at (79,395)
deleted fc Works Yard broken fence [fc-yard-gap] [fc-yard-gap] at (79,372)
deleted fc Works Yard gate [fc-yard-gate] [fc-yard-gate] at (119,361)
deleted fc Stub W1 [fc-stub-W1] [fc-stub-W1] at (63,417)
deleted fc Stub W2 [fc-stub-W2] [fc-stub-W2] at (63,402)
deleted fc Stub W3 [fc-stub-W3] [fc-stub-W3] at (63,386)
deleted fc Stub E1 [fc-stub-E1] [fc-stub-E1] at (80,417)
deleted fc Stub E2 [fc-stub-E2] [fc-stub-E2] at (86,407)
deleted fc Stub CW [fc-stub-CW] [fc-stub-CW] at (63,373)
deleted fc Stub WS [fc-stub-WS] [fc-stub-WS] at (70,361)
deleted fc Sidewalk west [fc-sidewalk-west] [fc-sidewalk-west] at (23,329)
deleted fc Sidewalk east [fc-sidewalk-east] [fc-sidewalk-east] at (120,329)
deleted fc Sidewalk north [fc-sidewalk-north] [fc-sidewalk-north] at (25,329)
deleted fc Sidewalk south [fc-sidewalk-south-w] [fc-sidewalk-south-w] at (25,426)
deleted fc Sidewalk south [fc-sidewalk-south-e] [fc-sidewalk-south-e] at (77,426)
```

Fate deletions (75 plan fates, all already absent since the Phase 4 baseline clear; nothing deleted):

```
MISSING delete Salvage garages [home-garage] [home-garage] (Buildings)
MISSING delete court house [court-house] [court-house] (Buildings)
MISSING delete court garage [court-garage] [court-garage] (Buildings)
MISSING delete Court cottage [court-east-home] [court-east-home] (Buildings)
MISSING delete Maintenance garage [court-west-garage] [court-west-garage] (Buildings)
MISSING delete Court gardener’s home [court-northwest-home] [court-northwest-home] (Buildings)
MISSING delete Court brick home [court-west-home] [court-west-home] (Buildings)
MISSING delete Approach home [court-approach-home] [court-approach-home] (Buildings)
MISSING delete Court cottage [court-east-cottage] [court-east-cottage] (Buildings)
MISSING delete Garden Cottage [court-garden-home] [court-garden-home] (Buildings)
MISSING delete Court End home [court-southwest-home] [court-southwest-home] (Buildings)
MISSING delete North Court home [court-north-home] [court-north-home] (Buildings)
MISSING delete East Garden home [court-east-garden-home] [court-east-garden-home] (Buildings)
MISSING delete Perimeter home 1 [court-perimeter-1] [court-perimeter-1] (Buildings)
MISSING delete Perimeter home 2 [court-perimeter-2] [court-perimeter-2] (Buildings)
MISSING delete Perimeter home 3 [court-perimeter-3] [court-perimeter-3] (Buildings)
MISSING delete Perimeter home 4 [court-perimeter-4] [court-perimeter-4] (Buildings)
MISSING delete Perimeter home 5 [court-perimeter-5] [court-perimeter-5] (Buildings)
MISSING delete Perimeter home 6 [court-perimeter-6] [court-perimeter-6] (Buildings)
MISSING delete Perimeter home 7 [court-perimeter-7] [court-perimeter-7] (Buildings)
MISSING delete Perimeter home 8 [court-perimeter-8] [court-perimeter-8] (Buildings)
MISSING delete Perimeter home 9 [court-perimeter-9] [court-perimeter-9] (Buildings)
MISSING delete Perimeter home 10 [court-perimeter-10] [court-perimeter-10] (Buildings)
MISSING delete Perimeter home 11 [court-perimeter-11] [court-perimeter-11] (Buildings)
MISSING delete Perimeter home 12 [court-perimeter-12] [court-perimeter-12] (Buildings)
MISSING delete tree [court-east-cottage:garden-tree] [court-east-cottage:garden-tree] (Props)
MISSING delete tree [neighbourhood:court-tree-b] [neighbourhood:court-tree-b] (Props)
MISSING delete fence [neighbourhood:court-west-garden-edge] [neighbourhood:court-west-garden-edge] (Props)
MISSING delete fence [neighbourhood:court-west-garden-edge-2] [neighbourhood:court-west-garden-edge-2] (Props)
MISSING delete Perimeter fence 1 [court-perimeter-fence-1] [court-perimeter-fence-1] (Props)
MISSING delete Perimeter fence 2 [court-perimeter-fence-2] [court-perimeter-fence-2] (Props)
MISSING delete Perimeter fence 3 [court-perimeter-fence-3] [court-perimeter-fence-3] (Props)
MISSING delete Perimeter fence 4 [court-perimeter-fence-4] [court-perimeter-fence-4] (Props)
MISSING delete Perimeter fence 5 [court-perimeter-fence-5] [court-perimeter-fence-5] (Props)
MISSING delete Perimeter fence 6 [court-perimeter-fence-6] [court-perimeter-fence-6] (Props)
MISSING delete Perimeter fence 7 [court-perimeter-fence-7] [court-perimeter-fence-7] (Props)
MISSING delete Perimeter fence 8 [court-perimeter-fence-8] [court-perimeter-fence-8] (Props)
MISSING delete Perimeter fence 9 [court-perimeter-fence-9] [court-perimeter-fence-9] (Props)
MISSING delete Perimeter fence 10 [court-perimeter-fence-10] [court-perimeter-fence-10] (Props)
MISSING delete Perimeter fence 11 [court-perimeter-fence-11] [court-perimeter-fence-11] (Props)
MISSING delete Perimeter fence 12 [court-perimeter-fence-12] [court-perimeter-fence-12] (Props)
MISSING delete Perimeter fence 13 [court-perimeter-fence-13] [court-perimeter-fence-13] (Props)
MISSING delete Perimeter fence 14 [court-perimeter-fence-14] [court-perimeter-fence-14] (Props)
MISSING delete Perimeter fence 15 [court-perimeter-fence-15] [court-perimeter-fence-15] (Props)
MISSING delete Perimeter fence 16 [court-perimeter-fence-16] [court-perimeter-fence-16] (Props)
MISSING delete Perimeter fence 17 [court-perimeter-fence-17] [court-perimeter-fence-17] (Props)
MISSING delete Perimeter fence 18 [court-perimeter-fence-18] [court-perimeter-fence-18] (Props)
MISSING delete Perimeter fence 19 [court-perimeter-fence-19] [court-perimeter-fence-19] (Props)
MISSING delete Path 1 [] (Roads and paths)
MISSING delete Path 2 [] (Roads and paths)
MISSING delete Path 3 [] (Roads and paths)
MISSING delete Path 4 [] (Roads and paths)
MISSING delete Path 5 [] (Roads and paths)
MISSING delete Path 26 [] (Roads and paths)
MISSING delete Path 27 [] (Roads and paths)
MISSING delete Path 39 [] (Roads and paths)
MISSING delete Path 40 [] (Roads and paths)
MISSING delete Path 41 [] (Roads and paths)
MISSING delete Path 42 [] (Roads and paths)
MISSING delete Path 61 [] (Roads and paths)
MISSING delete Path 62 [] (Roads and paths)
MISSING delete Path 63 [] (Roads and paths)
MISSING delete Path 64 [] (Roads and paths)
MISSING delete Path perimeter 1 [] (Roads and paths)
MISSING delete Path perimeter 2 [] (Roads and paths)
MISSING delete Path perimeter 3 [] (Roads and paths)
MISSING delete Path perimeter 4 [] (Roads and paths)
MISSING delete Path perimeter 5 [] (Roads and paths)
MISSING delete Path perimeter 6 [] (Roads and paths)
MISSING delete Path perimeter 7 [] (Roads and paths)
MISSING delete Path perimeter 8 [] (Roads and paths)
MISSING delete Path perimeter 9 [] (Roads and paths)
MISSING delete Path perimeter 10 [] (Roads and paths)
MISSING delete Path perimeter 11 [] (Roads and paths)
MISSING delete Path perimeter 12 [] (Roads and paths)
```

Generated (36 objects) and the remaining log lines:

```
generated SceneArea Sidewalk west [fc-sidewalk-west] [fc-sidewalk-west] (23,329) 2x99
generated SceneArea Sidewalk east [fc-sidewalk-east] [fc-sidewalk-east] (120,329) 2x99
generated SceneArea Sidewalk north [fc-sidewalk-north] [fc-sidewalk-north] (25,329) 95x2
generated SceneArea Sidewalk south [fc-sidewalk-south-w] [fc-sidewalk-south-w] (25,426) 42x2
generated SceneArea Sidewalk south [fc-sidewalk-south-e] [fc-sidewalk-south-e] (77,426) 43x2
generated SceneProp fence Compound fence west [fc-fence-west-1] [fc-fence-west-1] (25,331) 1x95 blocksMovement=1
generated SceneProp fence Compound fence east [fc-fence-east-yard-1] [fc-fence-east-yard-1] (119,331) 1x30 blocksMovement=1
generated SceneProp fence Compound fence east [fc-fence-east-yard-2] [fc-fence-east-yard-2] (119,365) 1x30 blocksMovement=1
generated SceneProp fence Compound fence east [fc-fence-east-court] [fc-fence-east-court] (119,395) 1x31 blocksMovement=1
generated SceneProp fence Compound fence north [fc-fence-north-court] [fc-fence-north-court] (26,331) 54x1 blocksMovement=1
generated SceneProp fence Compound fence north [fc-fence-north-yard] [fc-fence-north-yard] (80,331) 39x1 blocksMovement=1
generated SceneProp fence Compound fence south [fc-fence-south-1] [fc-fence-south-1] (26,425) 41x1 blocksMovement=1
generated SceneProp fence Compound fence south [fc-fence-south-2] [fc-fence-south-2] (77,425) 42x1 blocksMovement=1
generated SceneProp fence Side fence W1-W2 [fc-sidefence-W1-W2] [fc-sidefence-W1-W2] (26,410) 37x1 blocksMovement=1
generated SceneProp fence Side fence W2-W3 [fc-sidefence-W2-W3] [fc-sidefence-W2-W3] (26,395) 37x1 blocksMovement=1
generated SceneProp fence Side fence W3-CW [fc-sidefence-W3-CW] [fc-sidefence-W3-CW] (26,379) 37x1 blocksMovement=1
generated SceneProp fence Side fence E1-E2 [fc-sidefence-E1-E2] [fc-sidefence-E1-E2] (81,410) 38x1 blocksMovement=1
generated SceneProp fence Side fence WS-CW [fc-sidefence-WS-CW] [fc-sidefence-WS-CW] (63,332) 1x33 blocksMovement=1
generated SceneBuilding Salvage garages [fc-house-W1] [fc-house-W1] (51,414) 12x7 roof=rf-garage-roof-2 door=(11,2) 1x3 (copy of Salvage garages [home-garage])
generated ScenePath Stub W1 [fc-stub-W1] [fc-stub-W1] (63,417)->(67,417)
generated SceneBuilding North Court home [fc-house-W2] [fc-house-W2] (53,398) 10x9 roof=rf-house-E-roof-2 door=(9,3) 1x3 (copy of North Court home [court-north-home])
generated ScenePath Stub W2 [fc-stub-W2] [fc-stub-W2] (63,402)->(67,402)
generated SceneBuilding Court gardener’s home [fc-house-W3] [fc-house-W3] (53,382) 10x9 roof=rf-house-E-roof-0 door=(9,3) 1x3 (copy of Court gardener’s home [court-northwest-home])
generated ScenePath Stub W3 [fc-stub-W3] [fc-stub-W3] (63,386)->(67,386)
generated SceneBuilding Court brick home [fc-house-E1] [fc-house-E1] (81,413) 10x9 roof=rf-house-W-roof-1 door=(0,3) 1x3 (copy of Court brick home [court-west-home])
generated ScenePath Stub E1 [fc-stub-E1] [fc-stub-E1] (80,417)->(76,417)
generated ScenePath Stub E2 [fc-stub-E2] [fc-stub-E2] (86,407)->(86,407)->(76,407)
generated SceneBuilding Court End home [fc-house-CW] [fc-house-CW] (53,369) 10x10 roof=rf-house-E-roof-1 door=(9,3) 1x3 (copy of Court End home [court-southwest-home])
generated ScenePath Stub CW [fc-stub-CW] [fc-stub-CW] (63,373)->(67,373)
generated ScenePath Stub WS [fc-stub-WS] [fc-stub-WS] (70,361)->(70,369)
generated SceneProp fence Yard fence west edge [fc-yardfence-west-1] [fc-yardfence-west-1] (79,332) 1x40 blocksMovement=1
generated SceneProp fence Yard fence west edge [fc-yardfence-west-2] [fc-yardfence-west-2] (79,376) 1x19 blocksMovement=1
generated SceneProp fence Yard fence south edge [fc-yardfence-south] [fc-yardfence-south] (79,395) 40x1 blocksMovement=1
generated SceneProp debris Works Yard broken fence [fc-yard-gap] [fc-yard-gap] (79,372) 1x4 blocksMovement=0 (D51 placeholder)
generated SceneProp fence Works Yard gate [fc-yard-gate] [fc-yard-gate] (119,361) 1x4 blocksMovement=1
generated SceneBuilding Works Yard office [fc-yard-office] [fc-yard-office] (106,353) 12x7 roof=rf-garage-roof-2 door=(0,2) 1x3 (copy of Salvage garages [fc-house-W1], not enterable)
```

```
Generate: plan E:\Factorio2\Unity\Docs\FOUNDERS-COURT-PLAN.json
Generate: block (23,329) 99x99, road polyline (72,432) -> (72,374)
Generate: baseline clear, nothing to remove; block (23,329) 99x99: decor tiles 0, solids 0, props 0, paths 0, drives 0, squares 0, service areas 0 (roads, nodes, tram, court untouched)
Generate: removed 36 earlier fc- objects
Generate: deleted 0 objects, 75 missing
moved Foreman workshop [foreman-shelter] [foreman-shelter] old (81,398) 12x9 new (81,398) 12x9 doors old (4,8) 3x1 new (4,8) 3x1 roof old rf-workshop-roof-0 new rf-workshop-roof-0 (lot E2)
kept Home workshop [home-workshop] [home-workshop] old (65,347) 10x14 new (65,347) 10x14 doors old (4,13) 3x1 new (4,13) 3x1 roof old rf-workshop-roof-1 new rf-workshop-roof-1 (lot WS)
kept site Works Yard [yard:0] [yard:0] (80,332) 39x63
kept site Works Yard Iron Scrap Pile [opening-iron-v1] [opening-iron-v1] (84,352) 3x4
kept site Works Yard Copper Cable Spool [opening-copper-v1] [opening-copper-v1] (96,352) 3x4
kept site Works Yard Coal Fuel Bunker [opening-coal-v1] [opening-coal-v1] (96,369) 3x3
kept site Substation 0 [substation:0] [substation:0] (93,350) 3x3
side fence fc-sidefence-WS-YARD not present (replaced by the yard fence)
side fence fc-sidefence-E2-YARD not present (replaced by the yard fence)
yard fence north run [fc-fence-north-yard] (80,331) 39x1 is compound fence segment, already generated
yard fence east run [fc-fence-east-yard-1] (119,331) 1x30 is compound fence segment, already generated
yard fence east run [fc-fence-east-yard-2] (119,365) 1x30 is compound fence segment, already generated
lot VERGE [fc-lot-VERGE] (77,369) 2x26: plan data only, lots have no scene object
Generate: done
```

Net objects new to this pass: `fc-fence-east-yard-1`, `fc-fence-east-yard-2`, `fc-fence-east-court` replace `fc-fence-east-1`; `fc-fence-north-court`, `fc-fence-north-yard` replace `fc-fence-north-1`; `fc-yardfence-west-1` (79,332) 1×40, `fc-yardfence-west-2` (79,376) 1×19 and `fc-yardfence-south` (79,395) 40×1 replace `fc-sidefence-WS-YARD` and `fc-sidefence-E2-YARD`; `fc-yard-gap` (79,372) 1×4, `fc-yard-gate` (119,361) 1×4 and `fc-yard-office` (106,353) 12×7 are new. 36 fc objects in the scene after the run (29 before).

## 4. Verify

Final run, full output (`Unity/Relight/Temp/fc_verify_log.txt`):

```
PASS C1 compound closed: enclosure tiles reached with mouth solid = 0 (court 0, verge 0, yard 0; enclosure = interior (26,332) 93x93 + yard (80,332) 39x63 + verge (77,369) 2x26; mouth x 67..76, y 425..427 blocked; start 71,435, start walkable True)
PASS C2 fence solid: 362 compound fence tiles, 0 not solid
PASS C3 no baseline solids: 1270 solid tiles in block, 0 outside any building/fence/substation
PASS C4 houses in lots: 7 houses; all inside their lot, no overlaps
PASS C5 setback 4: W1=4, W2=4, W3=4, E1=4, E2=4, CW=4, WS=8 (workshop exception, D25)
PASS C6 doors and stubs: 7 houses, each door on its planned wall with one stub (first point at the door, last point on the road, any point count)
PASS C7 roof variety: 5 neighbour pairs, all roof keys differ
PASS C8 edge clear: 766 sidewalk/edge tiles, no building, prop or site on them
PASS C9 yard: yard (80,332) 39x63, 3 nodes, substation (93,350) 3x3; all inside, D9 gaps ok, substation touches copper
PASS C10 raid line: 1 raid line site(s): (69,432) 6x1; DirectorRules.RaidLineY = 432 (want y 432 x 69..74)
PASS C11 spawn: playerSpawn (70.5,360.5), compiled spawn tile (70,360), want (70.5,360.5)
PASS C12 plan match: 36 fc objects in scene, 36 planned, all rects match; kept houses at plan rects
PASS C13 walk steps: core -> mouth row 66 steps, + 10 = 76, cap EntryFarSteps = 76
PASS C14 baseline clear: clear, nothing to remove; block (23,329) 99x99: decor tiles 0, solids 0, props 0, paths 0, drives 0, squares 0, service areas 0 (roads, nodes, tram, court untouched)
PASS C15 yard boundary: 205 edge tiles: 197 fence, 4 gate, 4 gap, nothing else
PASS C16 gap open: flood from circle centre 72,374 (walkable True) reached 15208 tiles, 2364 in the yard rect (80,332) 39x63; first 80,332; gap tiles 79,372 open 79,373 open 79,374 open 79,375 open
PASS C17 gap only: flood from circle centre with gap tiles 79,372 79,373 79,374 79,375 solid reached 12840 tiles, 0 in the yard rect
PASS C18 gate solid: 4 of 4 gate tiles solid: 119,361 solid 119,362 solid 119,363 solid 119,364 solid
PASS C19 office: Works Yard office [fc-yard-office] (106,353) 12x7 inside yard rect (80,332) 39x63, door tiles 106,355 106,356 106,357 on west wall x=106, enterable=False, roof rf-garage-roof-2, no overlap with nodes, substation or gate
Verify: 19 passed, 0 failed of nineteen checks
```

History: the first Verify run of this pass (after the first Generate rerun) passed C1–C18 with identical detail lines and failed C19 once:

```
FAIL C19 office: Works Yard office [fc-yard-office] (106,353) 12x7 inside yard rect (80,332) 39x63, door tiles 106,355 106,356 106,357 on west wall x=106, enterable=False, roof rf-garage-roof-2; overlaps Works Yard [yard:0] (80,332) 39x63
Verify: 18 passed, 1 failed of nineteen checks
```

One failure on one check, fixed in the generator's check (heading 2); no check reached three failures.

## 5. Capture

`Unity/Docs/FOUNDERS-COURT-AFTER-3.png`, 1600×1600, Scene view in 2D looking at tile (72,378) with size 62, taken through `capture_scene_view` after the final Generate and Verify. It shows the court with its five side fences, the yard fenced on all four sides with the break in the west fence at rows 372–375, the office against the east fence with its door on the west wall, the nodes and the substation. The gate draws as fence (it is a fence-kind prop, D50); no gate art exists.

## 6. Temporary files

- `Assets/Temp/fc_after3.png` (capture target) deleted through `AssetDatabase.DeleteAsset`, then the empty `Assets/Temp` folder deleted the same way; `AssetDatabase.IsValidFolder("Assets/Temp")` returned false afterwards.
- `Unity/Relight/Temp/fc_generate_log.txt` and `fc_verify_log.txt` are the generator's own logs under the ignored `Temp/` folder, outside `Assets`; left in place as before.

## 7. Gaps in this pass

1. **Gap and gate objects are not clearable.** D49/D50/D51 do not say whether the placeholder debris or the gate can be cleared by the player; the generator sets `clearable` 0 on both so nothing removes them before the real art and gate behaviour exist. Not a decision; recorded for the human.
2. **The office copies kind and variant only.** D53 says "copied from the smallest house". The office takes the source's kind (`garage`) and variant (2) and the plan's size, roof and door; the source's other fields (`enterable`, `campaignBuilding`, name) are deliberately not copied because D53 sets them. The roof is `rf-garage-roof-2`, the same key as the source, because no west-facing garage roof exists (Part A gap 12).
3. **Console check window.** The `console` command returns the last 100 entries; the Generate and Verify logs alone are 185 lines, so the check covers only the tail of this pass. Those 100 entries hold no error, exception or assert, and every compile in this pass reported `compilationFailed false` through `recompile_status`.
4. **Two generator reruns.** Heading 2 records the duplicate yard fences and the C19 overlap set; both were generator faults fixed in the generator, and the final scene is the output of the third Generate run. The Verify output of the second and third runs is identical except C19.
5. **History.** The previous version of this report recorded the first "go apply" stopping at B1 on the uncommitted Phase 4 tree; that is resolved by commit `9926fed3` and the stop text is replaced by this run.

Working tree after this pass (`git status --porcelain`, not committed):

```
 M Unity/Docs/FOUNDERS-COURT-DECISIONS.md
 M Unity/Relight/Assets/Editor/FoundersCourtCompound.cs
 M Unity/Relight/Assets/Relight/Scenes/World.unity
?? Unity/Docs/FOUNDERS-COURT-AFTER-3.png
```
