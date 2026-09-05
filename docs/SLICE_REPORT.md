# Relight — Phase 4 slice report (prompt B, §11 minutes 0–60)

Living document, rewritten from the top at each prompt B milestone (D5/D6 rework, 2026-09-04): the current milestones first, the §19 first-hour test at the top, Gate B before the appendix. The lattice slice's M1–M3 (2026-09-03) stay at the end as the record of what was built and measured before the rework; Gate B is not scored on them.

## §19 first-hour test (human play) — **0–10 scored at Gate B, the rest not recorded**

Scored by a human, from a played hour. Nothing here is filled in by the bot. Played by Daniel on 2026-09-05 (`docs/GATE_B.md`); the gate was closed as fully tested the same day, and the empty rows below are a **provenance note, not an outstanding debt** — no number is inferred into them.

| Window | New unlock | Current problem | Memorable moment |
|---|---|---|---|
| 0–10 min | **Movement.** *"Movement is good, it's kind of intuitive but need more development time."* | **Not knowing what to do next.** *"Knowing what is needed next. Maybe we need a small quest system?"* → **D-GB-2** | **None.** *"Nothing yet."* |
| 10–30 min | *not recorded — empty* | *not recorded — empty* | *not recorded — empty* |
| 30–60 min | *not recorded — empty* | *not recorded — empty* | *not recorded — empty* |

Did the burn-off make you say something? — **Not answered as asked.** What came back instead is a request to change what the lights *are*: light earned and walked out by the player rather than granted by a claim (**D-GB-1**, decided the same day as the hybrid, option (c)). Whether the current payoff lands is therefore still unestablished.

**Read against §19's own claim.** The doc says minutes 0–10 teach two rules — blocks bloom, and ammo is made from rubble. **Neither was named back**; movement was, which §19 does not count as a rule, and the absence of direction is something §19 does not predict. One session cannot rewrite the test, and §19 now carries that sentence against itself (`RELIGHT-design.md`, T9).

Gate B's two new rows (prompt B M6; the harness answers the second half of each from the played hour's export):

| Row | Answer |
|---|---|
| At what minute did the player first fire, and did it matter? (`npm run replay -- <export.json>` → `held anyway` / `saved it` / `fell anyway`) | **Gate B, 2026-09-05: no minute taken; it did not matter** — *"I was just shooting at things"*. No rescue, nothing saved, which agrees with the bots at both scales and is the play half of **D-R1**. Two asks came with it: more range or none (**D-GB-3**) and more enemies at the player (**D-GB-4**). The bot beside it, E-hour rifle on, 2026-09-04: The bot beside it, E-hour rifle on, 2026-09-04: first shot 2:48 / 2:44 / 3:05 on seeds 3 / 4 / 5, the first crawler's tick; **held anyway** on seeds 3 and 4, **fell anyway** on seed 5 (north fell at 58:27 with the rifle and without); the browser's hour on seed 3: 19 hand-fired fights, all held anyway |
| The walking row: minutes walked and chest trips in the hour, against §19's 15 % (the export's `summary.walkedPct`, `summary.chestTrips`) | **Gate B, 2026-09-05: unrecorded** — no telemetry panel was read, so no human share exists and this row stays the bot's. The bot beside it: The bot beside it: 2.8 / 3.1 / 2.8 min walked = 4.6 / 5.1 / 4.7 %, 11 / 10 / 9 chest trips (E-hour); the browser's hour 5.0 %, 18 trips |

## GAME-ASSUMPTION register (generated from the Assumed tables below, prompt B M6)

Every tag in the code, by the milestone that made it, with the phase or decision that resolves it. Generated from each milestone's Assumed table (the tag and the Where column are theirs; the resolution is the table's fourth column where it has one, else the milestone's Deferred / Decisions). The lattice slice's tags that survive the rework are in `REWORK_REPORT.md` §3.

| Tag | Milestone | Where | Resolved by |
|---|---|---|---|
| GA-B1-1 | Prompt B M1 Ground and the engineer | `ground.ts:31` | M5 Light |
| GA-B1-2 | Prompt B M1 Ground and the engineer | `ground.ts:189` | M5 Light |
| GA-B1-3 | Prompt B M1 Ground and the engineer | `walk.ts:16` | D-B1-2 (M6 tedium audit) |
| GA-B1-4 | Prompt B M1 Ground and the engineer | `engineer.ts:22` | Phase 5 (items as typed ore) |
| GA-B1-5 | Prompt B M1 Ground and the engineer | `engineer.ts:81` | M6 (D-B1-3) |
| GA-B1-6 | Prompt B M1 Ground and the engineer | `engineer.ts:99` | Phase 11 (A* vs graph distance) |
| GA-B1-7 | Prompt B M1 Ground and the engineer | `flow.ts:149` | D-B1-1 (M6, with D-P4-4) |
| GA-B1-8 | Prompt B M1 Ground and the engineer | `flow.ts:513` | M2 Flow |
| GA-B1-9 | Prompt B M1 Ground and the engineer | `panel.ts:80` | M6 (D-B1-3) |
| GA-B1-10 | Prompt B M1 Ground and the engineer | `worldScene.ts:48` | D-B1-5 (Phase 12) |
| GA-B1-11 | Prompt B M1 Ground and the engineer | `worldScene.ts:479` | Delete at the Phase 5 gate |
| GA-B1-12 | Prompt B M1 Ground and the engineer | `worldScene.ts:500` | Phase 12 art pass |
| GA-B1-13 | Prompt B M1 Ground and the engineer | `session.ts:105` | Delete at the Phase 5 gate |
| GA-B1-14 | Prompt B M1 Ground and the engineer | `flow.ts:899` | M3 Defence |
| GA-B1-15 | Prompt B M1 Ground and the engineer | `sim.ts:466` | delete with the lattice, Phase 5 gate |
| GA-B1-19 | Before M2 | `engineer.ts:22` | D-B1-5 (numbers the human's) |
| GA-B1-20 | Before M2 | `engineer.ts:25` | D-B1-5 |
| GA-B1-21 | Before M2 | `engineer.ts:29` | D-B1-5 |
| GA-B1-22 | Before M2 | `worldScene.ts:51` | M6 tedium audit |
| GA-B1-2 (moved) | Before M2 | `ground.ts:189` | D-B1-4 |
| GA-B1-14 (moved) | Before M2 | `flow.ts:162` | D-B1-4 |
| GA-B1-16 | Before M2 | `flow.ts:201`, `:263` | M3 |
| GA-B1-17 | Before M2 | `flow.ts:849`, `:882` | M3 |
| GA-B1-15 (moved) | Before M2 | `sim.ts:466` | delete at the Phase 5 gate |
| GA-B1-18 | Before M2 | `engineer.ts:94` | Gate B (D-R2) |
| GA-M2-6 (kept, reworded) | Prompt B M2 Flow on the faces | `flow.ts` `MACHINE_COST` | — |
| GA-B2-1 | Prompt B M2 Flow on the faces | `flow.ts` `canPlace` | Phase 5 (recipes) |
| GA-B2-2 | Prompt B M2 Flow on the faces | `flow.ts` `pickUpItems` | Phase 5 |
| GA-B2-3 | Prompt B M2 Flow on the faces | `flow.ts` `tickHand` | Phase 5 (hands) |
| GA-B3-1 | Prompt B M3 Defence on the segment | `flow.ts` `SURVIVOR_UNLOCKS`, `survivorJoined` | Phase 6 (survivors) |
| GA-B3-2 | Prompt B M3 Defence on the segment | `flow.ts` `MACHINE_COST` | Phase 5 (§13.14 recipe) |
| GA-B3-3 | Prompt B M3 Defence on the segment | `recipes.ts` `FLOODLIGHT_HALF_ANGLE`, `flow.ts` `litAt` | D-B3-1 (made) |
| GA-B3-4 | Prompt B M3 Defence on the segment | `flow.ts` `reachOf`, `poleGrid` | D-B3-3 (made) |
| GA-B3-5 | Prompt B M3 Defence on the segment | `flow.ts` `handFeed`, `depotFeed`, `botHands` | D-B6-1 |
| GA-B3-6 | Prompt B M3 Defence on the segment | `ground.ts` city loop | D-P4-9 (after the played hour) |
| GA-B3-7 | Prompt B M3 Defence on the segment | `sim.ts` held event | Phase 6 |
| GA-B3-8 | Prompt B M3 Defence on the segment | `worldScene.ts` `UNLOCK_KEYS` | D-B1-5 (keys) |
| GA-M3 (`session.ts:108`, edited) | Before M4 | game | — |
| GA-B4-1 | Prompt B M4 Threat, and the rifle | `threat.ts` header, `ROUND_DMG` / `CONTACT_R` / `DANGER_R` / `PATH_R` / `EAT_R` / `STUCK_S` | D-B4-2 (taken) |
| GA-B4-2 | Prompt B M4 Threat, and the rifle | `threat.ts` `spawnAt` | Phase 7 |
| GA-B4-3 | Prompt B M4 Threat, and the rifle | `threat.ts` `targetsFor` | D-P4-9 |
| GA-B4-4 | Prompt B M4 Threat, and the rifle | `sim.ts` engagement loop | D-P4-9 |
| GA-B4-5 | Prompt B M4 Threat, and the rifle | `threat.ts` `fieldFor` | verification pass (perf) |
| GA-B4-6 | Prompt B M4 Threat, and the rifle | `threat.ts` `arrive` | D-B4-3 (taken) |
| GA-B4-7 | Prompt B M4 Threat, and the rifle | `flow.ts` `placeMachine`, `blockLights` | closed by M5 repair |
| GA-B4-8 | Prompt B M4 Threat, and the rifle | `threat.ts` `stepCrawler` | Phase 7 |
| GA-B4-9 | Prompt B M4 Threat, and the rifle | `threat.ts` `stepCrawler` | D-B1-2 |
| GA-B4-10 | Prompt B M4 Threat, and the rifle | `flow.ts` `chestTrip` | Gate B walking row |
| GA-B4-11 | Prompt B M4 Threat, and the rifle | `panel.ts` | Phase 8 (Arsenal) |
| GA-B4-12 | Prompt B M4 Threat, and the rifle | `worldScene.ts` `reachable` | Gate B |
| GA-B4-13 | Prompt B M4 Threat, and the rifle | `main.ts` `bloom` toast | Phase 12 |
| GA-B5-1 | Prompt B M5 Light | `worldScene.ts` `UNLIT_RGB` | D-B5-3 (taken) / Phase 12 |
| GA-B5-2 | Prompt B M5 Light | `worldScene.ts` `LIGHT_REFRESH_MS` | verification pass (soak) |
| GA-B5-3 | Prompt B M5 Light | `flow.ts` `lightRanks` | D-B5-2 (taken) |
| GA-B5-4 | Prompt B M5 Light | `worldScene.ts` `SWEEP_R` | Phase 12 |
| GA-B5-5 | Prompt B M5 Light | `flow.ts` `REPAIR_COPPER` | D-B5-3 (taken) |
| GA-B5-6 | Prompt B M5 Light | `flow.ts` `blockLights`, `repairLight` | Phase 5 |
| GA-B5-7 | Prompt B M5 Light | `worldScene.ts` `HAND_LAMP_R` | D-B5-1 (taken) |
| GA-B6-1 | Prompt B M6 The hour | `hour.ts` `HOUR_*` | E-hour on the verification pass, then Gate B |
| GA-B6-2 | Prompt B M6 The hour | `hour.ts` `HOUR_CLAIM_AT`, `HOUR_GEN_AT` | D-B6-2 |
| GA-B6-3 | Prompt B M6 The hour | `hour.ts` `neighbourToward`, `claimStep` | D-P4-5 (after the played hour) |
| GA-B6-4 | Prompt B M6 The hour | `hour.ts` `idleTurret`, `carryTurrets` | D-P4-9 (after the played hour) |
| GA-B6-5 | Prompt B M6 The hour | `hour.ts` `replay` | E-hour determinism check |
| GA-B6-6 | Prompt B M6 The hour | `replay.ts` | a scenario-B hour |
| GA-B6-7 | Prompt B M6 The hour | `hour.ts` `westThenEast` | D-P4-8 (after the played hour) |
| GA-B6-8 | Prompt B M6 The hour | `hour.ts` `hourCommands` | E-hour |
| GA-M1-1 | Lattice M1 Ground | `tiles.ts` | Phase 5 (rubble as finite typed ore) |
| GA-M1-2 | Lattice M1 Ground | `tiles.ts` | Phase 9 sites (D-P4-3) |
| GA-M1-3 | Lattice M1 Ground | `tiles.ts` | Phase 12 art pass |
| GA-M1-4 | Lattice M1 Ground | `tiles.ts` | Phase 5 (E11/E13) |
| GA-M1-5 | Lattice M1 Ground | `tiles.ts` | M2 |
| GA-M1-6 | Lattice M1 Ground | `tiles.ts` (implied) | D-P4-2, Phase 5 |
| GA-M1-7 | Lattice M1 Ground | `worldScene.ts` | D-P4-1 |
| GA-M1-8 | Lattice M1 Ground | `worldScene.ts` | M4 / M5 |
| GA-M1-9 | Lattice M1 Ground | `worldScene.ts` | M2 |
| GA-M1-10 | Lattice M1 Ground | `worldScene.ts` | Phase 12 |
| GA-M1-11 | Lattice M1 Ground | `main.ts` (implied) | M2 |
| GA-M2-1 | Lattice M2 Flow | `flow.ts` | D-P4-6; Phase 5 if two lanes are wanted |
| GA-M2-2 | Lattice M2 Flow | `flow.ts` | Phase 12 if belt feel needs Factorio's compression rules |
| GA-M2-3 | Lattice M2 Flow | `flow.ts` | M3 (turret hoppers) |
| GA-M2-4 | Lattice M2 Flow | `flow.ts` | Phase 5 (recipes) |
| GA-M2-5 | Lattice M2 Flow | `flow.ts` | D-P4-4; Phase 5 (Depot rules, §14.3) |
| GA-M2-6 | Lattice M2 Flow | `flow.ts` | D-P4-4 |
| GA-M2-7 | Lattice M2 Flow | `flow.ts` | M3 |
| GA-M2-8 | Lattice M2 Flow | `flow.ts` | Phase 5 (walls, foundations) |
| GA-M2-9 | Lattice M2 Flow | `tiles.ts` | D-P4-2 / Phase 5 (units per tile) |
| GA-M2-10 | Lattice M2 Flow | `tiles.ts` | M3 (turret strip) / Phase 9 (generator) |
| GA-M2-11 | Lattice M2 Flow | `session.ts` | D-P4-5; M6 |
| GA-M2-12 | Lattice M2 Flow | `worldScene.ts` | Phase 12 |
| GA-M2-13 | Lattice M2 Flow | `worldScene.ts` | never (block-only mode) |
| GA-M3-1 | Lattice M3 Defence | `flow.ts` `ensureFlow` | D-P4-8 |
| GA-M3-2 | Lattice M3 Defence | `flow.ts` `ensureFlow` | D-P4-7; M6 |
| GA-M3-3 | Lattice M3 Defence | `flow.ts` `prefillTurrets` | never (C10 is a Gate A lock) |
| GA-M3-4 | Lattice M3 Defence | `flow.ts` `turretEdge`, `hookSyncEdges` | D-P4-9; M6 with D-P4-5 |
| GA-M3-5 | Lattice M3 Defence | `flow.ts` `hookDrainEdges` | M4 (enemies as things on tiles; range 9) |
| GA-M3-6 | Lattice M3 Defence | `flow.ts` `MACHINE_KW` | Phase 5 |
| GA-M3-7 | Lattice M3 Defence | `flow.ts` `GENERATOR_COAL_CAP`, `TURRET_FLASH_S` | Phase 12 |
| GA-M3-8 | Lattice M3 Defence | `flow.ts` `SHED_RANK`, `shedRank` | Phase 5 (recipe lines) |
| GA-M3-9 | Lattice M3 Defence | `flow.ts` `subPowered` | D-P4-7 |
| GA-M3-10 | Lattice M3 Defence | `flow.ts` `tickGenerator` | Phase 5 |
| GA-M3-11 | Lattice M3 Defence | `flow.ts` `placeable` | Phase 5 |
| GA-M3-12 | Lattice M3 Defence | `flow.ts` `poleGrid` | M6 (Electricians' Big pole) |
| GA-M3-13 | Lattice M3 Defence | `flow.ts` `layPoles` | M6 |
| GA-M3-14 | Lattice M3 Defence | `flow.ts` `handFeed` | Phase 5 (Depot rules) |
| GA-M3-15 | Lattice M3 Defence | `flow.ts` `MACHINE_COST` | D-P4-4 |
| GA-M3-16 | Lattice M3 Defence | `tiles.ts` `substationLot` | M6 (craftable Substation, outskirts) |
| GA-M3-17 | Lattice M3 Defence | `tiles.ts` `streetlights` | M5 (light map) |
| GA-M3-18 | Lattice M3 Defence | `session.ts` | D-P4-7 |
| GA-M3-19 | Lattice M3 Defence | `queries.ts` `pipOf` (M1, now read by turrets) | never (Gate A) |
| GA-M3-20 | Lattice M3 Defence | `worldScene.ts` | Phase 12 |
| GA-M3-21 | Lattice M3 Defence | `flow.ts` `botHands`, `session.ts` | M6 (the bot is a dev aid; Gate B is played by hand) |

## Prompt B M1 Ground and the engineer — built 2026-09-04

### Built

- **Tiles on faces** (`packages/sim/src/ground.ts`, run name `B-M1-ground`, tests in `tiles.test.ts`): the tile layer is a pure derivation of the city's `CityGeom`: a lot is a rasterised face (`owner`), streets are the 6–12-wide ridges between faces, the river is water, plazas and parks are inert faces. 800×800 tiles in 32×32 chunks (625 a city), a per-block key (`blockKey`: state, pool, dug count) and a per-chunk key (`chunkKey`) so the renderer redraws only what changed. Rubble typed by district in three clusters inside the polygon, 250–350 tiles scaled by area, densest on the deepest faces (hops from the HQ, `city/spec.ts`); outskirt deposits kept. Each lot has its pre-existing substation (3×3, a 1×1 stand-in on a face too thin for one — none on seeds 3/4/5) and streetlights on the street tiles that border it, one every ≥ 3 tiles, 3 in 8 broken. `walkable`, `blockOfTile`, `hqLot`, `blocksNear`, `inReach`, `describeGround` are the queries the game and the walk use. The lattice tile layer (`tiles.ts`) is still the source of the HQ lot layout (patches, Depot, turrets, Generator) and is reused for `?map=lattice`.
- **The engineer on the tiles** (`packages/sim/src/walk.ts`, run name `B-M1-walk`, six tests in `walk.test.ts`): a tile-level A* (`findPath`, 4-connected, limit 250k nodes) over `passable` (any walkable tile, dark blocks and rot included, never water; belts and poles are walked through, other machines never), `tickEngineerTiles` at 6 tiles/s with the block sim's `engineer.block` kept in step so the D5 block-level rules (kits on arrival, restock at the HQ, the rifle) fire unchanged. `walkTo(block)` from the map walks to the block's pole; `move(x, y)` from the world walks to a tile; a redirected walk restarts from where the engineer stands. The start is the HQ lot's workbench (`workbenchTile`).
- **Pockets and the chest** (`engineer.ts` stack sizes, `flow.ts` `chestTake` / `chestPut` / `chestCount` / `nearDepot`, `panel.ts`): 40 stacks (rubble 50 a stack, magazines 20, machines and kits one), the Depot is the 6×6 chest and workbench. Hand-mining within reach 8 puts units in the pockets (the lattice M2 sent them to the Depot); taking from and putting into the chest needs the engineer within reach of the Depot's edge, refused with a toast beyond it. The chest starts with §11's 200 steel, 100 copper, 50 stone and 20 magazines. Kits are taken from the chest by hand, one a claim; a claim made with none in the pockets toasts that its edges wait.
- **World view on foot** (`worldScene.ts`, `main.ts`, `cityMapScene.ts`, `view.ts`): a chunked Blitter over the ground (only chunks whose key changed are re-blitted), the engineer sprite with a reach ring (radius 8) and a walk-path line, WASD and click-to-walk, the camera following from minute 0 (free camera only under `?flow=0`), wheel zoom 0.5–3× about the engineer; a tool that needs reach (dig, place, remove, rotate, hand-feed, craft at the workbench) refuses out of reach with "walk closer". **M** toggles map ↔ world: the map opens with the engineer's block marked and a throbbing ring while a walk is under way; a click on a block claims it if it can be claimed and walks the engineer to its pole either way; back to the world lands on the engineer. **I** opens the pockets panel (take n / put all per chest item, disabled beyond reach). **E** does nothing. `?walk=1` is gone (walking is the only way to move); the three rework stubs (walk under a flag, the city stub in `toggleView`, the engineer dot only under `config.walk`) are gone.
- **The harness bot keeps walking** (`bots.ts` `walkingBot`) at block level; the game drives the same `walkTo`. `__relight` gains `walkTo(x, y)`, `engineer()`, `ground()`, `describe(tx, ty)`, `chest.{count, take, put}`, `togglePockets()`, `world.drawMs()`.
- **Two sim fixes found by the first city soak** (both in the block sim, both tagged, both surfaced by the test `defence.test.ts` "city HQ (prompt B M1)"): (1) `flow.ts` `hookSyncEdges` made every HQ edge a turret edge, as the lattice M3 could (its three streets each had a start pair); a city HQ has 4–7 segments and the six start turrets cover three of seed 3's, so the fourth had `turrets = 0`, a hopper the ring never filled and `empty` from tick one — the HQ was lost at sim minute 20 with the §11 line making magazines beside it. An edge with no physical turret now keeps the block-level stand-in hopper, ring-fed from the buffer. (2) `sim.ts` `rebuildRing` gave the HQ's own edges `kit = false` at t = 0 with `walk` on (firing budget 0) — the harness bot restocks and lays four kits on the HQ in its first second, which hid it; a human with no bot fired nothing until they took kits from the chest. The HQ's edges start kitted (the start turrets are the kit); every later edge still waits for a kit the engineer carries. Fix (2) moves the D6 regression fixtures `city4.json` and `city5.json` (a rules change under constitution rule 1, `_exportCity.ts` header): seed 4's compact-walk five hours go held 50 → 52, front 19 → 21, lost 2 → 0, retakes 2 → 0, total magazines 7,835.8 → 7,859.3, shells 1,690 → 1,490; seed 5 moves only its walking minutes (6.5 / 3.92 / 1.97 / 1.75 → 6.53 / 4.1 / 1.98 / 1.62 an hour); seed 3 and the snapshot `5f3417b9` do not move. `E-walk`'s hour-3 walking minutes drop (2.2 min mean, check 0–10), `E-variance` and the calibration (`calibration.md`, config `5eae8618`, re-run) are unchanged.
- **Checks:** `npm test` 95/95 (one added: `defence.test.ts` city HQ), typecheck (with the game build), lint, `snapshot:check` (config `5f3417b9`, unchanged: the block sim's numbers do not move), `docsync:check`, `experiments` — all green (see Measured).

### Assumed (every `GAME-ASSUMPTION` added or moved by M1; the rework's and the lattice slice's stay listed in `REWORK_REPORT.md` §3 and the appendix)

| # | Where | Assumption | Resolves at |
|---|---|---|---|
| GA-B1-1 | `ground.ts:31` | A face too thin for a 3×3 substation gets a 1×1 stand-in on its pole tile (none on seeds 3/4/5). The 3×3 stands on the buildable tile nearest the lot's centroid (D-B1-4, decided). | M5 Light |
| GA-B1-2 | `ground.ts:189` | A face's streetlights stand on each street segment's kerb, one every `FACE_LIGHT_STEP` = 4 tiles along the segment (D-B1-4: the count follows the segment's length; a 32-tile segment gets the lattice's 8), 3 in 8 broken as on the lattice. | M5 Light |
| GA-B1-3 | `walk.ts:16` | The engineer walks through belts and poles (thin), never through other machines or water. | D-B1-2 (M6 tedium audit) |
| GA-B1-4 | `engineer.ts:22` | Stack sizes: rubble and coal 50, magazines 20, machines and kits one each. | Phase 5 (items as typed ore) |
| GA-B1-5 | `engineer.ts:81` | Kits are free to draw from the chest; the claim paid for them (10 wire, 5 frames). | M6 (D-B1-3) |
| GA-B1-6 | `engineer.ts:99` | A block-level redirected walk restarts from its old destination's distance; on the tiles it restarts from where the engineer stands. | Phase 11 (A* vs graph distance) |
| GA-B1-7 | `flow.ts:149` | The game's chest starts with §11's 200 steel / 100 copper / 50 stone / 20 magazines; the harness keeps the calibrated 80/40/0 so `5f3417b9` stands. | D-B1-1 (M6, with D-P4-4) |
| GA-B1-8 | `flow.ts:513` | Hand-crafting still draws the chest and delivers to it; the scene asks for reach of the workbench. §14 has it from the pockets. | M2 Flow |
| GA-B1-9 | `panel.ts:80` | A human is not auto-restocked the way the bot is: kits are taken from the chest by hand, one a claim; a claim with none in the pockets toasts that its edges wait. | M6 (D-B1-3) |
| GA-B1-10 | `worldScene.ts:48` | Inserter on **N** and assembler on **F**; **I** is the pockets and **M** the map since D5. | D-B1-5 (Phase 12) |
| GA-B1-11 | `worldScene.ts:479` | Under `?flow=0` (no flow layer) the HQ is a 6×6 slab and the camera is free. | Delete at the Phase 5 gate |
| GA-B1-12 | `worldScene.ts:500` | A code-drawn disc with a reach ring stands in for the engineer sprite. | Phase 12 art pass |
| GA-B1-13 | `session.ts:105` | The flow layer (and so the tiles and the engineer) is on for every session unless `?flow=0`. | Delete at the Phase 5 gate |
| GA-B1-14 | `flow.ts:899` | An edge with physical turrets fires only through them; an edge with none keeps the block-level stand-in hopper, ring-fed. (The M1 clause "a city HQ's segment the six start turrets do not cover keeps the stand-in" is gone: under D-B1-4 every live HQ segment is covered.) | M3 Defence |
| GA-B1-15 | `sim.ts:466` | An edge covered by physical turrets is born kitted, whichever block, whenever born (D-B1-4). Only a block-only state (no tile layer: the harness's block sim) keeps §11's HQ ring as an abstract ring — its HQ edges at t = 0 born kitted; every later edge waits for a carried kit. | delete with the lattice, Phase 5 gate |

### Deferred

Re-read in `DEFERRED.md` ("Re-read at prompt B M1"): the lattice tile layer stays as the HQ layout's source and `?map=lattice` → delete at the Phase 5 gate; the engineer sprite and tileset art → Phase 12; hand-crafting from the pockets at the workbench → M2; kits by hand and the walk-back tedium → M6; the truck as a driveable vehicle → Phase 5 (the Tram depot is a 20 s walk on seed 3); the walk path outside `SimState` (derived each tick from the block sim's position) → Phase 12; fixed "take n" buttons → Phase 12; A* tile distance vs the block sim's graph distance for the D5 walk time → Phase 11.

### Measured

- **Ground derivation** (`ground(st)` per city, cold, tileset built once; cached re-derivation against unchanged keys 0 ms):

  | Seed | Blocks | Cold ms | Street tiles | Lot tiles | River | Rubble tiles | Substations (1×1) | Streetlights (broken) | Chunks |
  |---|---|---|---|---|---|---|---|---|---|
  | 3 | 382 | 68–115 | 238,690 | 310,782 | 90,528 | 91,255 | 347 (0) | 13,024 (4,778) | 625 |
  | 4 | 373 | 63–98 | 240,847 | 307,221 | 91,932 | 92,897 | 337 (0) | 12,864 (4,780) | 625 |
  | 5 | 349 | 42–117 | 229,796 | 299,372 | 110,832 | 88,868 | 315 (0) | 12,172 (4,683) | 625 |

  Cold ms is the range over three runs each on this host (WSL2, `/mnt/e`); the lattice's 576 cells took 51–66 ms. ≈ 90k rubble tiles a city against the lattice's ≈ 102k (fewer, larger faces; the count scales with area).
- **Walk check (headless Chromium, 1×, `walk.cjs`):** HQ → Tram depot (seed 3, 3 hops) 20 s sim walked in 24 s real, back 20 s, engineer at the depot's pole on arrival, no page errors, no frame over 50 ms on either leg.
- **1 h at 4× soak** (seed 3, `?view=world`, camera following the bot-driven engineer, the §11 line placed through the hooks at minute 0 so the HQ holds — without it the start 200 rounds run out at sim minute 16 as the lattice M3 record says — `soak.cjs`, 1280×800):

  | Real | Sim | Frames | fps | Worst frame | Frames > 50 ms | At sim 1 h | At sim 1:30 | Magazines made | Hand-fed | Brownout |
  |---|---|---|---|---|---|---|---|---|---|---|
  | 901.6 s | 1:30:05 | 39,378 | 43.7 mean, 35–57 by minute | 48.3 ms | 0 | held 4 (HQ + 3 claims), lost 0 | held 3, lost 1 at 1:29:42 | 572 (all delivered; capped by `bufferCap` 4000 from sim minute 36) | 839 | 265 s |

  The fixed build holds the hour (`soak900d.log`): the HQ's four segments are fed from tick one, the buffer is full from minute 36 and the bot claims three neighbours by minute 45, restocking kits at the HQ between claims. The one loss, at 1:29:42, comes after the DoD's hour with the chest's coal at 0 and both Generators dark (the brownout seconds are 5 at minute 36, 125 at minute 48, 265 at the end); the coal supply is prompt B M3's. The summary's first amber/red read 0:00:00: the stand-in segment's hopper is 0 until the first ring tick fills it (not chased, M3). `configHash` `b6060069` (the game's §11 chest, not the harness's).

  **What the soak found.** The first clean 900 s run (pre-fix build, `soak900c.log`) went 901.6 s real / sim 1:30:05, 30,393 frames, 33.7 fps, worst 65 ms, 65 frames over 50 ms in three bursts (real 180–220 s, 400–410 s, 780 s) with draw ≤ 3.7 ms and sim ≤ 0.09 ms a tick inside them — the renderer or the host, not M1's code — and lost the HQ between sim minutes 19 and 21 with the line making 305 magazines (first loss 0:17:04 was the claimed block). A node trace of the same setup showed the HQ's four segments as one with no turret, hopper 0, `empty` from tick one, and three with 3 / 2 / 1 turrets, the buffer always 0 and 170 magazines hand-fed: the two sim fixes above. After them the same trace holds the hour (held 4, lost 0, buffer 1,610 at 10 min → 3,993 at 30 min, 572 magazines made then capped by `bufferCap` 4000).

- **Frame-rate attribution** (`prof.cjs`, CDP profiler, 10 s at 4×): 91.7 % of samples in `(program)` — the headless swiftshader renderer — JS ≈ 1.6 ms a frame, draw EMA 0.1–0.27 ms, sim 0.14–0.23 ms a tick. Identical code ran at 27–57 fps across runs today; the map view and `?flow=0` (neither touched by M1) ran under 60 fps on the same host. The DoD's 60 fps is not met in this environment and the number is reported as measured; the M3 record's 60 fps flat (2026-09-03, same machine) is the baseline the next soak on a GPU host has to match. Nothing over 50 ms except the screenshot frame.

### Where M1 and the doc disagree (reported, not resolved)

- **§11's chest vs the calibration's start stock** (GA-B1-7): two starts. Decision D-B1-1.
- **§14 "walks through nothing built"** was not a doc rule: belts and poles are passable in the sim and §14 now says so (`B-M1-walk` changelog). Decision D-B1-2.
- **§14 kits from the pockets** are free of charge in the sim (the claim paid); the doc names wire and frames without a recipe. Decision D-B1-3.
- **§5/§14 substations and streetlights** are placed by the face geometry, not the doc's per-lot rule from the lattice (GA-B1-1/2). Decision D-B1-4.
- **§4's key bindings** (I inserter, M map on the lattice) moved. Decision D-B1-5.
- **§25's C1/C2 after D-R2** — this M1 line said they still read MISSED at the claim minute (16–31 min). That was read from a stale `calibration.md` written before the D-R2 ordering fix; M1's code already gave MET. Corrected in "Before M2" item 3 below with the clean numbers.

### Three decisions (rows in `DECISIONS.md`, D-B1-1 … D-B1-5; the three that matter)

1. **D-B1-1 — the chest's start stock.** Keep both starts (game §11's 200/100/50/20, harness 80/40/0) until D-P4-4 at M6 re-calibrates with the line in the loop. Recommended (a).
2. **D-B1-2 — passable belts and poles.** Keep; M6's tedium audit measures the walk-around cost if they were solid. Recommended (a).
3. **D-B1-3 — kits by hand.** Keep kits free and taken by hand one a claim; M6 counts the HQ walk-backs per hour. Recommended (a).

## Before M2 — the four small things (done 2026-09-04)

The human's "Four small things before Prompt B M2", in order, each with its check. No milestone: M1 stays built, M2 not started. Run names `B-M1-body` (item 1), `B-M1-start` (item 2), `B-M1-born` (item 3), `B-M1-ref` (item 4).

### 1. D-B1-5 — Direct control (decided by the human)

- **Built** (`engineer.ts` `SPRINT_MULT` / `SPRINT_S` / `STAMINA_REFILL_S` / `DODGE_*` / `RIFLE_RANGE` / `RIFLE_HIT_RADIUS`, `walk.ts`, `worldScene.ts`, `panel.ts`, `telemetry.ts`; `body.test.ts`, four tests): WASD moves; **Shift** sprints at 1.6× walk for ~4 s on a full bar, the bar refills in ~6 s, and sprint never drains it below one dodge; **Space** dodges 3 tiles in 0.25 s with i-frames against crawlers, a 1 s cooldown and a fixed quarter-bar cost; **left-click** does what the hand holds (place, dig, feed, or fire the rifle at the cursor: hitscan to 9 tiles, 1.5-tile hit radius, no auto-target); **E** interacts only; **Tab** / **I** the inventory; **B** the build menu and the hotbar 1–9; **R** rotates, **Q** pipettes, scroll zooms, **M** the map, **Esc** closes. Click-to-walk is gone from the world view; the map view's walk-here is the only auto-walk; `walkTo` stays for the bots and the `__relight` hook. Any WASD input cancels a walk-here; so does a dodge. Telemetry carries §19's guards: shooting ≤ 10 % and **time in danger ≤ 5 %** of session time, with the share of danger the rifle drew.
- **Doc**: §4 (the bindings, the stamina bar, no click-to-walk), §11 (the rifle as a hotbar item), §19 (the danger guard), §22, §23 — changelog line `B-M1-body`. `DECISIONS.md` D-B1-5 made by the human.
- **Check**: the human walkthrough (the list in the prompt) is the human's at the next play session; by proxy the four `body.test.ts` tests pass and the headless soak drives the same engineer for 33 sim minutes with no error. Not claimed: feel.

### 2. D-B1-4 — Placement by face geometry (decided by the human)

- **Built**: `ground.ts` `faceSubstation` — the buildable tile nearest the lot's centroid (a face too thin for 3×3 gets a 1×1, GA-B1-1); `faceLights` — one lamp every `FACE_LIGHT_STEP` = 4 tiles along each segment's kerb, count from length (GA-B1-2; replaces "8 per edge"); `segLength` — a segment's length as its ridge's extent along its axis (the ridge is two tiles wide, so `CitySeg.len` is ~2× the geometric length). `flow.ts` `startTurrets` — per live HQ segment `max(1, floor(segLength / TURRET_PER_TILES))` turrets at even spacing along the front ring, each facing the nearest ridge tile, on clear ground or a rubble pad; a segment no turret then reaches (a corner sliver whose two-tile front holds no 2×2) is served by the neighbouring segment's turrets that reach it (`edgeTurrets`, GA-B1-17). `sim.ts` `syncEdges` — an edge covered by turrets is born kitted, whichever block, whenever born; **the HQ special case M1 added (GA-B1-15's "HQ edges at t = 0") is removed** from every state with a tile layer. The lattice keeps its six fixed turrets (`?map=lattice`, Phase 5 gate).
- **Doc**: §5 step 1 and step 3, the §5 machine table's turret row, §13's Gun turret row and the "Pre-existing fixtures (D-B1-4)" paragraph — changelog line `B-M1-start`. `DECISIONS.md` D-B1-4 made by the human; D-P4-8 (six start turrets) superseded.
- **Check — every segment covered, seeds 3/4/5** (`defence.test.ts` "city HQ (D-B1-4)…", 45 tiles → 2 due means `floor(45/16)`):

  | Seed | Segment → neighbour | Length (tiles) | Due | Reach | Start hopper |
  |---|---|---|---|---|---|
  | 3 | → 332 (corner sliver) | 5.0 | 1 | 1 | 50 |
  | 3 | → 334 | 31.4 | 1 | 2 | 100 |
  | 3 | → 353 | 41.3 | 2 | 2 | 100 |
  | 3 | → 355 | 41.0 | 2 | 2 | 100 |
  | 4 | → 348 | 11.0 | 1 | 1 | 50 |
  | 4 | → 361 | 52.0 | 3 | 3 | 100 |
  | 4 | → 364 | 50.8 | 3 | 3 | 100 |
  | 5 | → 324 (corner sliver) | 7.4 | 1 | 1 | 33 |
  | 5 | → 325 | 37.0 | 2 | 3 | 100 |
  | 5 | → 348 | 33.3 | 2 | 2 | 100 |

  Turrets on the HQ: seed 3 six, seed 4 seven, seed 5 five (M1 had six fixed). "Reach" counts the turrets whose range covers the segment's ridge; a sliver's one turret is the neighbouring segment's. Seed 4's fourth segment faces an inert neighbour and gets none by design. The 33-round hopper on seed 5's sliver is the §11 start buffer running out after the first two segments' turrets took 100 each (per-edge prefill, GA-B1-16: `min(hopper, buffer)` a turret edge in ring order).
- **Check — the minute-20 soak, no HQ branch**: `grep` finds no `hq`/`startIdx` test in `hookSyncEdges`, `hookDrainEdges` or `hookCovered`; the only `startIdx` left in `syncEdges` is the block-only fallback (GA-B1-15). Headless seed 3, 4×, the §11 line (`soak20b.log`):

  | Real | Sim | Frames | fps (headless, regressions only) | Worst frame | > 50 ms | Held / lost at the end | Claims | Errors |
  |---|---|---|---|---|---|---|---|---|
  | 330.6 s | 33:03 | 17,055 | 51.6 | 51.7 ms | 1 | 3 / 0 | 2 | none |

  The HQ that fell at minute 20 in M1's first soak holds through minute 33 with the derived turrets, and the bot has claimed two neighbours by minute 30. A control run of the same build **without** the line lost the HQ between sim 12 and 18 min: with no magazines made, five turrets a block outlast the 200-round start by ~12 minutes — the §11 line is what holds the HQ, not the turret count, which is the doc's intent.

### 3. C1/C2 — the pip-birth artefact

- **What was found**: the M1 report's "first amber at the claim minute (31 / 16 / 16 min)" was read from a stale `calibration.md` committed before the D-R2 ordering fix; M1's HEAD code already gave C1/C2 MET. So the artefact in the *code* — a newly kitted edge visible to the pip before its hopper is filled — was already closed for the D-R2 case. What remained: an edge created by `syncEdges` (a claim) is on the ring for the rest of that tick with `hopper: 0` before the ring fill runs, and a bot's `kitBlock` set `kit = true` a tick before the fill.
- **Built by the rule**: `engineer.ts` `bornFed` — a kitted edge is born fed: its hopper takes `min(cap − hopper, buffer)` from the ring's buffer in the tick it is created or kitted (D5 kits on a `walk` state; the Gate A lattice fixtures keep their ring-order fill, which is why the 13 lattice fixtures do not move); `kitBlock` calls it the second it kits. `Edge.born` records the tick, and `calibrate.ts` never reads a pip on an edge's birth tick (both pip loops). GA-B1-18.
- **Numbers** (`npm run calibrate`, config `5eae8618`, three seeds; "stale" = the committed file the M1 report quoted, "clean" = this build; HEAD's own re-run matches "clean" on every target):

  | Target | Stale (committed before D-R2's fix) | Clean (this build) |
  |---|---|---|
  | C1 first enclosure before first amber on a kitted edge | MISSED — amber 31m / 16m / 16m | **MET** — enclosure 60m / 46m / 45m, amber never / 178m / never |
  | C2 no red on a kitted edge before 120 min | MISSED | **MET** — never / 178m / never |
  | i1 first amber on a kitted edge in 60–120 min (info) | — | MISSED: never / 178m / never (later than the window, not earlier) |

  The only row the born-fed rule moves against HEAD is seed 3's spike scenario: 2,316 → 2,291 magazines made, 579 → 573 delivered (the first fill is a tick earlier, so a few rounds land before the assembler's). C7 (stalls) and i2 are unchanged and still MISSED as before, outside this item.
- **Check**: first amber is strictly later than the claim minute on all three seeds (never / 178 min / never against claims from minute 1); D-R2 stays closed. `snapshot:check` still matches `5f3417b9`; `city{3,4,5}.json` unchanged.

### 4. Reference machine

- `PROGRAMME_STATE.md` gains `reference_machine:` (Windows 11 Home 10.0.26200, i7-11700K, 64 GB, RTX 3070 driver 32.0.15.9186, 2560×1440 @ 59 Hz, Chrome 153 GPU-accelerated over CDP; WSL2 runs the repo). `soak.cjs` takes `SOAK_CDP` to attach to it (the game served by `vite preview` on 4173 from WSL2, reached over mirrored networking). **Headless swiftshader fps is reported as "headless, for regressions only" from here on; only this host is measured against the 60 fps DoD.**
- **Measured once, the M1 soak on the reference machine** — seed 3, 4×, world view, the §11 line, camera following the engineer (`gpu900.log`; renderer `ANGLE (NVIDIA GeForce RTX 3070, Direct3D11)`, viewport 1584×905 @ 1×):

  | Host | Real | Sim | Frames | fps | Mean frame | Worst frame | > 50 ms | Held / lost at 1:30 | Claims | Magazines made | Errors |
  |---|---|---|---|---|---|---|---|---|---|---|---|
  | **Reference machine** (RTX 3070, Chrome 153, GPU) | 900.8 s | 1:30:03 | 53,921 | **59.9** | 16.7 ms | 26.7 ms | **0** | 3 / 1 | 3 | 561 | none |
  | Headless swiftshader, M1's run (regressions only) | 901.6 s | 1:30:05 | 39,378 | 43.7 | 22.9 ms | 48.3 ms | 0 | 4 / 0 | — | — | none |

  The reference machine holds the 60 fps DoD: every 10 s window read 59.9 fps at a 16.7 ms mean (the 59 Hz display's vsync), and the one slower frame (26.7 ms) is well under the 50 ms line. The game state is the same build as the headless minute-20 soak above, so the sim numbers are the sim's, not the renderer's: the bot claims three neighbours and loses one of them (not the HQ) between minute 33 and 1:30, and magazine production stops at 561 around sim minute 33 when the §11 line's Generator runs out of coal (`genCoal` 0, `brownout` count rising from minute 30) — D-P4-7 hour-one power, open, unchanged by these four items. The headless M1 row is kept for the regression comparison only.

### GAME-ASSUMPTIONs added by the four items

| Tag | Where | Assumption | Decided by |
|---|---|---|---|
| GA-B1-19 | `engineer.ts:22` | Sprint 1.6× walk, ~4 s a full bar, ~6 s to refill; sprint cannot drain the bar below one dodge. | D-B1-5 (numbers the human's) |
| GA-B1-20 | `engineer.ts:25` | Dodge 3 tiles in 0.25 s, i-frames against crawlers for its duration, 1 s cooldown, a quarter bar. | D-B1-5 |
| GA-B1-21 | `engineer.ts:29` | The rifle reaches the turret's 9 tiles (no range advantage) and hits within 1.5 tiles of the cursor; no auto-target. | D-B1-5 |
| GA-B1-22 | `worldScene.ts:51` | The hotbar order: 1 belt, 2 inserter, 3 Excavator, 4 Shot assembler, 5 turret, 6 lamp, 7 pole, 8 Generator, 9 the rifle; with the rifle in hand nothing is mined or placed until it is cleared. | M6 tedium audit |
| GA-B1-2 (moved) | `ground.ts:189` | Lamps every `FACE_LIGHT_STEP` = 4 tiles along the kerb. | D-B1-4 |
| GA-B1-14 (moved) | `flow.ts:162` | `TURRET_PER_TILES` = 16 of `segLength`, at least one a segment. | D-B1-4 |
| GA-B1-16 | `flow.ts:201`, `:263` | The HQ's turret pads were laid before the rubble (a pad is dug free at start); the start buffer prefills turret edges in ring order, `min(hopper, buffer)` each. | M3 |
| GA-B1-17 | `flow.ts:849`, `:882` | A turret serves one street, the nearest; a corner sliver is covered by the neighbouring segment's turrets that reach it (two streets). | M3 |
| GA-B1-15 (moved) | `sim.ts:466` | A block-only state keeps §11's HQ ring as an abstract ring kitted at t = 0. | delete at the Phase 5 gate |
| GA-B1-18 | `engineer.ts:94` | A kitted edge is born fed from the ring's buffer; telemetry never reads a birth-tick pip. | Gate B (D-R2) |

### Checks, fixtures, decisions

- `npm test` 99/99 (95 + `body.test.ts`'s four), `typecheck` (incl. the game build), `lint`, `snapshot:check` (`5f3417b9`; the snapshot regenerated twice for new fields only — the engineer's stamina/dodge/aim state and `Edge.born` — no number moved), `docsync:check`, `experiments` 12 / 105 s / 0 failing checks — green.
- The D6 regression fixtures `city{3,4,5}.json` do not move (`_exportCity.ts` re-run); the calibration moves only on the spike row above. No rules change beyond the two the human decided.
- Decisions: D-B1-4 and D-B1-5 made by the human; D-P4-8 superseded. Nothing new for the human from these four items. `DEFERRED.md` "Re-read before prompt B M2" (click-to-walk deleted; start turrets on segments done; the block-only start ring to delete at the Phase 5 gate). §26 recount unchanged at 33.

## Prompt B M2 Flow on the faces — built 2026-09-04

Run names `B-M2-pockets` (placement from the pockets, pick-up, hand-crafting) and `B-M2-rates` (the rates and the D-P3-10 footprint on seed 3's HQ face). The lattice M2 machines (`flow.ts`: Excavator, belts, inserters, Shot assembler, the 20 ticks/s tile tick and the derived 1 s block tick) do what they did; what changed is where a machine comes from and where it goes.

### Built

- **From the pockets, within reach** (`flow.ts` `canPlace` / `place`, `engineer.ts` `take` / `drop`; `worldScene.ts`, `panel.ts`): a machine is a pocket item, one stack each. Placing pays its rubble price from the pockets (`MACHINE_COST`, unchanged since the lattice: Excavator 10 steel, belt 1, inserter 1 + 1 Cu, assembler 40 + 20 Cu) or drops a carried machine for nothing; the Depot chest is never drawn on for a machine, so a line costs the walk to the chest first. `canPlace` returns `carried` and a reason the ghost shows ("not enough in the pockets (10 steel)"). The `place` command in the hand hook is reach-checked against the machine's footprint and does nothing beyond it; the world view's cursor says "Walk closer — the engineer reaches 8 tiles" and nothing moves, never a silent relocation. Direct `place()` calls keep reach the caller's (tests, `__relight.flow.place`).
- **Pick-up into the pockets** (`canPickUp` / `remove` / `pickUpItems`): right-click with an empty hand returns the machine and what it held — belt items, an inserter's hand, the assembler's inputs and output, a Generator's coal, a turret's rounds as whole magazines — all or nothing; a full pocket refuses with a toast ("No pick-up: the pockets are full (2 stacks to carry, 0 free)"). No rubble refund. §11's two turrets carried at minute 40 are four stacks (a turret and its magazines each).
- **Hand-crafting from the pockets into the pockets** (`queueCraft`, `tickHand`): 2 steel + 1 Cu a magazine in 3 s, at the workbench within reach of the Depot; the queue is capped at what the pockets can pay and refused with a reason ("walk closer to the workbench", "not enough in the pockets (2 steel + 1 Cu a magazine)"); the craft keeps its progress and pauses out of reach or with full pockets. The M1 stand-in (crafting drew the chest and delivered to it) is gone.
- **In the game**: the ghost reads "10 steel from the pockets" / "from the pockets (1 carried)" / the refusal; the pockets panel lists the machines carried; the build menu shows the count carried beside each price; the workbench button and E both queue from the pockets and toast a refusal; right-click's toast names the stacks and the pocket count. Dev hook `__relight.flow.canPickUp`.
- **Tests** (`flow.test.ts`, `defence.test.ts`, `walk.test.ts`): placement pays from the pockets and the Depot stock is untouched even at 10,000 steel; a carried machine is free to put down; pick-up stacks and contents; the full-pocket refusal; a turret with 50 rounds comes back as 1 turret + 5 magazines with 10 rounds to the line buffer; the hands test rewritten (refusal on empty pockets, the cap, the pause out of reach and the resume, the magazine in the pockets and the chest only down by what the pockets took).

### Assumed (every `GAME-ASSUMPTION` in M2 code)

| Tag | Where | Assumption |
|---|---|---|
| GA-M2-6 (kept, reworded) | `flow.ts` `MACHINE_COST` | machine prices in rubble (§13 gives none); pick-up returns the machine, never a refund |
| GA-B2-1 | `flow.ts` `canPlace` | paying rubble at placement stands in for crafting the machine — §13 prices none and §14 says "placed from their pockets"; Phase 5 recipes decide whether a machine is made at the workbench first (D-B2-1) |
| GA-B2-2 | `flow.ts` `pickUpItems` | a picked-up turret's rounds come back as whole magazines, the loose remainder (< 10) to the line buffer; a machine and each item it held are their own stacks (D-B2-3) |
| GA-B2-3 | `flow.ts` `tickHand` | the hand-craft pauses with its progress kept out of reach of the Depot or with full pockets; an unaffordable queue is dropped up front rather than left waiting |

The M1 tag "hand-crafting still draws the chest" is removed; the lattice M2 tags (GA-M2-1…5, the tick, the belt model, the inserter swing) stand. A machine stacks to 1 by `engineer.ts` `stackSize`'s default, untagged: §14's "into a stack" is the text.

### Deferred

- **Machines in the Depot chest and the truck bringing them** (§19): the chest holds rubble, magazines and kits only; a machine lives in the pockets or on the ground → Phase 5 with the truck.
- **Workbench recipes for machines** (a machine as an item crafted before it is placed) → Phase 5 recipes, D-B2-1.
- **Ground items** (a pick-up that spills, an inventory that overflows): nothing in the slice drops an item on a tile; the full pocket refuses instead → not in the slice.
- **The harness bots never use the tile machines**; the block sim stays the judge and the tile layer feeds it the same numbers (the C1/C2 calibration is unchanged, below) → the Phase 5 gate decides which sim rates the economy (D-P4-5, M6).
- **Belt-to-belt side-loading and splitters**: the lattice belt model (a straight run and corners) is what M2 has; a belt that meets another belt's side drops nothing and takes nothing → Phase 5.

### Measured

Seed 3's HQ face 356 (30×30 bbox, 780 tiles, `slotsOf` 1; the lot is not a rectangle: 73 tiles under the Depot, the substation, the Generator and the start turrets, 100 rubble, 46 patch). Script `m2measure.ts` (scratch), the tile tick at 20/s, the block tick at 1 s.

| What | Measured | Doc |
|---|---|---|
| chest → pockets | 200 steel + 100 Cu in 6 stacks; the Depot at 0 / 0 after | §11 |
| an Excavator ordered from 12 tiles away (command hook) | refused, nothing placed, 8 machines on the face before and after | §14 reach 8 |
| §11's line placed within reach from the pockets | 2 Excavators, 51 belts, 3 inserters, 1 assembler: 114 steel + 23 Cu paid from the pockets (86 / 77 left); the Depot untouched at 0 / 0 | §11's 200 steel covers it |
| belt run steel patch → assembler, 23 tiles (20 east, a corner, 3 south) + an inserter | first unit on the belt at 2.05 s (the Excavator's first 2 s), in the assembler at 14.30 s: 12.25 s in transit | 23 / 1.875 = 12.27 s, + 0.5 s inserter swing = 12.77 s |
| line rate, 10 min after a 2 min warm-up | 150 magazines made and 150 delivered (15.0 / min); steel mined 300 (0.500 / s) | 15 mag / min from one steel Excavator; 0.5 / s |
| copper in the same window | 0 mined: the copper belt backed up (98 items on belts at the end) and its Excavator stopped — one Cu Excavator (0.5 / s) over-supplies one assembler (0.333 / s) three to one | §11 has no copper rate to disagree with |
| saturated belt, 12 tiles with a corner into the Depot | 7.50 items / s over 60 s | 7.5 / s |
| pick-up of the assembler mid-craft | 1 stack more, pockets 5 / 40 (87 steel, 81 Cu, 1 assembler: its held 1 steel / 4 Cu came back too); put down again free, steel unchanged | §14 "into a stack" |
| hand-craft in the browser (preview build, seed 3) | with no copper refused with its reason; with 1 Cu queued and made in 3 s: 1 magazine in the pockets, steel 190 → 188 | §13 Workbench 3 s, §12 2 steel + 1 Cu |

Browser check (`m2game.cjs`, headless swiftshader against the preview build): the cursor places from the pockets within reach, refuses at 12+ tiles with the "Walk closer" toast and nothing placed, right-click picks the Excavator up into the pockets (5 / 40 stacks) and it goes down again free; 0 page errors. The reference-machine soak was not re-run for M2: the render loop is untouched and the M1 measurement stands (59.9 fps).

**For D-P3-10 — what fits on the HQ face and its four neighbours.** Free tiles are ground + rubble + deposit + patch not under a machine; "3×3 machines", "8×3 arms" (an Excavator, 3 belts, an inserter, 3 belts) and "11×7 §11 lines" (steel arm, assembler, copper arm, an inserter into the Depot) are greedy top-left packings of the free tiles, so a floor for what a player can lay out.

| seed | face | bbox | area (slots) | free tiles | 3×3 | 8×3 arms | 11×7 lines |
|---|---|---|---|---|---|---|---|
| 3 | HQ 356 | 30×30 | 780 (1) | 707 | 62 | 23 | 5 |
| 3 | 332 | 28×28 | 711 (1) | 702 | 71 | 24 | 6 |
| 3 | 334 | 23×30 | 640 (1) | 631 | 62 | 22 | 6 |
| 3 | 353 | 26×35 | 753 (1) | 744 | 73 | 26 | 6 |
| 3 | 355 | 43×36 | 1,116 (1) | 1,107 | 110 | 38 | 9 |
| 4 | HQ 360 | 31×46 | 1,197 (1) | 1,121 | 107 | 38 | 8 |
| 4 | 341 (Inert plaza) | 31×27 | 630 (1) | 0 | 0 | 0 | 0 |
| 4 | 348 | 20×27 | 497 (1) | 488 | 46 | 18 | 4 |
| 4 | 361 | 16×40 | 567 (1) | 558 | 53 | 19 | 4 |
| 4 | 364 | 24×38 | 711 (1) | 702 | 70 | 22 | 7 |
| 5 | HQ 346 | 37×29 | 891 (1) | 822 | 76 | 26 | 7 |
| 5 | 324 | 24×36 | 680 (1) | 671 | 65 | 24 | 6 |
| 5 | 325 | 35×27 | 754 (1) | 745 | 73 | 26 | 6 |
| 5 | 339 (Inert plaza) | 39×44 | 1,390 (2) | 0 | 0 | 0 | 0 |
| 5 | 348 | 22×21 | 391 (1) | 382 | 33 | 12 | 4 |

Every non-Inert face near the HQ gets one slot from area while its footprint holds 4–9 §11-scale lines and 33–110 3×3 machines; an Inert plaza gets 1–2 slots and holds nothing. The slot count from area does not match what a player can lay out in either direction: it is the economic cap C3 locked (one line per block), not a footprint limit.

Checks: `npm test` 99 / 99, `typecheck` (incl. the game build), `lint`, `snapshot:check` (`5f3417b9`, unchanged — the block sim is untouched), `docsync:check`, `experiments` 12 / 107 s / 0 failing checks, `calibrate` (C1 / C2 unchanged, MET). Fixtures `city{3,4,5}.json` unchanged: no rules change in the block sim.

### Where M2 and the doc disagree (reported, not resolved)

- **§14 slots vs footprint (D-P3-10)**: `slotsOf` = floor(area / 600) gives 1 on every HQ-neighbour face and 2 on an Inert plaza that holds nothing; the tile layer would let a player lay 4–9 lines. The tile layer does not enforce the slot and the block economy does not see the tiles, so the two do not collide in the slice — but they will when tile lines replace the stand-in (M6). Decision D-B2-2.
- **§13 / §14 machine prices**: the doc prices no machine and never says whether a machine is crafted first; M2 pays rubble at placement (GA-B2-1). Decision D-B2-1.
- **§14 "into a stack"**: the doc's one stack is the machine; its contents are extra stacks (a turret with its magazines is two), and a turret's loose rounds go to the line buffer rather than into the pockets. §11's "four stacks for two turrets" still holds. Decision D-B2-3.
- **§11 copper**: one copper Excavator over-supplies one Shot assembler three to one, so §11's line as drawn stalls its copper arm within the first minutes; not wrong, but the doc's picture of "an Excavator on each patch" is a steel-limited line with an idle copper drill. Reported; a smaller copper source is a Phase 5 recipe question.

### Decisions for the human (recommended in `DECISIONS.md` D-B2-1–D-B2-3)

- **D-B2-1 machine crafting**: rubble at placement as built (a) / a workbench recipe with a craft time (b) / machines stocked in the chest and brought by the truck (c). Recommend (a) for the slice and (b) with the Phase 5 recipes.
- **D-B2-2 slot count vs footprint**: keep `floor(area / 600)` as the economic cap (a) / derive slots from free footprint (~1 per 150 free tiles) (b) / drop slots when tile lines replace the stand-in at M6 (c). Recommend (a) now, (c) at M6.
- **D-B2-3 pick-up contents**: into the pockets as their own stacks, turret rounds as whole magazines and the remainder to the buffer, a full pocket refuses (a) / spill onto the ground (b) / lost (c). Recommend (a).

## Prompt B M3 Defence on the segment — built 2026-09-04

Run names `B-M3-segments` (an edge is a street segment: the HQ's start turrets by segment, the hopper-empty pip), `B-M3-unlocks` (the Electricians' Floodlight, Big pole and craftable Substation; the outskirts) and `B-M3-hands` (hand-feeding from the pockets). The lattice M3 machines (turret, Lamp, pole, Generator, the §14 shed order, `M3-rates`) do what they did; D-B1-4 had already put the pre-existing substation, the streetlights and the start turrets on the face by geometry. What M3 adds is the unlock gate, three machines, the outskirts rule and the pockets as the hand's source.

### Built

- **An edge is a street segment** (`flow.ts` `faceSegOf` / `turretEdge` / `edgeTurrets`, since D-B1-4; `cityMapScene.ts`): a turret serves the segment whose front ring holds most of its footprint, else the nearest ridge within 9; an edge's rounds are the sum of its turrets' hoppers (`hookSyncEdges`); a segment no turret serves is covered by the neighbouring turrets that reach it. The `hopper-empty` event carries the segment (block and neighbour) and the map view's pulse now lands on that segment's pip — the same event that turns the pip red — not on the block's centre.
- **The Electricians' unlocks** (`flow.ts` `SURVIVOR_UNLOCKS` / `survivorJoined` / `lockReason` / `unlockedKinds`; `map.ts` `SURVIVOR_UNLOCK_NAMES`; `sim.ts` held event `unlocks`; `worldScene.ts` `UNLOCK_KEYS`; `panel.ts`; `main.ts`): Floodlight, Big pole and Substation are locked kinds until the Electricians' block turns Held. `placeable` refuses a locked kind first ("the Electricians unlock it — hold their block"); the build menu shows the three rows disabled with the lock text, the keys 0 / [ / ] toast it; the held event names the group and its unlocks and the toast reads `Electricians: "We're in." — Floodlight, Big pole, Substation are on the build menu`. The unlocks stay after the block falls (GA-B3-1).
- **Floodlight** (2×2, 40 kW, `MACHINE_KW`; `blockLights` / `litAt`): a 12-tile cone 60° wide along its facing (GA-B3-3), R rotates it, lit while the face is powered, shed with the Lamps (rank 2); the ghost draws the cone and its arrow, the world view fills it.
- **Big pole** (2×2, `reachOf` 12): a post that stands in rubble, on the street and on Dark or Contested faces like a pole; it hangs from a substation 12 tiles out where a pole needs 8, claims across the street through `poleClaims` like a pole, and a wire spans the longer of its two ends' reaches (GA-B3-4), so a pole 11 tiles from a Big pole is on the grid.
- **Craftable Substation** (3×3, `faceSub` / `substationAt` / `isSubstationTile` / `poleGrid` / `layPoles`): goes on the lot of a Held face that has no substation, one a face ("the face has a substation" on the HQ), pays 50 steel + 25 Cu in rubble as a stand-in for §13's frames, wire and boards (GA-B3-2, D-B3-1); once placed it is the face's substation for every rule that reads one — the slab the world view draws, the pole anchor, the powered view — and a pick-up takes it back into the pockets.
- **The outskirts** (`ground.ts`): a face in district 3 gets no substation and no streetlights, §7's rule; the block sim's abstract substation still powers a Held one (GA-B3-6, D-B3-2 / D-P4-9's split), so the craftable Substation is what a player would place there.
- **Hand-feeding from the pockets** (`handFeed`, E on a turret or Generator): whole magazines from the pockets into a hopper, coal into a Generator, with the reasons "the hopper is full" / "no magazines in the pockets (take them from the Depot chest with I, or craft at the workbench with E)" / "the Generator is full" / "no coal in the pockets (…)"; the Depot chest is never drawn on. The harness bot, which has no pockets, keeps the Depot path (`depotFeed` / `botHands`, GA-B3-5).
- **Tests** (`defence.test.ts` 14, `tiles.test.ts`): the four M3 tests on the city — the unlock gate on seeds 3/4/5 (locked, the held event's `unlocks`, unlocked, kept after a fall, 12 kinds); the Floodlight's cone (ahead, 17° inside, 45° outside, beyond 12, behind, under the fixture, dark when shed, turned south by R) and its 40 kW on the face's demand; the Big pole on the grid where a pole at the same spot is not, and a pole hanging from it at 11; the craftable Substation on a dark outskirts face (none before, locked, placed, priced, the face's substation, a second refused, the HQ refuses, picked up into the pockets). `npm test` 103 / 103.

### Assumed (every `GAME-ASSUMPTION` in prompt B M3 code)

| Tag | Where | Assumption |
|---|---|---|
| GA-B3-1 | `flow.ts` `SURVIVOR_UNLOCKS`, `survivorJoined` | a group's unlocks land on the toolbar when its block turns Held and stay after the block falls (§11: the group has walked into the Depot) — D-B3-3 |
| GA-B3-2 | `flow.ts` `MACHINE_COST` | Substation 50 steel + 25 Cu in rubble stands in for §13's 20 frames + 20 wire + 10 boards (Phase 5 items); Floodlight 10 steel + 5 Cu and Big pole 4 steel + 4 Cu are priced like the rest — D-B3-1 |
| GA-B3-3 | `recipes.ts` `FLOODLIGHT_HALF_ANGLE`, `flow.ts` `litAt` | the Floodlight's cone is 60° wide (±30° about its facing; §13 gives only the 12-tile length); the tiles under the fixture count as lit |
| GA-B3-4 | `flow.ts` `reachOf`, `poleGrid` | a wire spans the longer of its two ends' reaches, so a Big pole reaches a pole 12 out and the pole reaches back |
| GA-B3-5 | `flow.ts` `handFeed`, `depotFeed`, `botHands` | hand-feeding is instant and draws the pockets only; the harness bot has no pockets and keeps feeding turrets and Generators from the Depot |
| GA-B3-6 | `ground.ts` city loop | an outskirts face (district 3) has no substation and no streetlights at tile level (§7) while the block sim's abstract substation still powers a Held one until D-P4-9 settles — D-B3-2 |
| GA-B3-7 | `sim.ts` held event | a survivor group joins ("we're in", §8) the second its block turns Held; the event carries the unlock names |
| GA-B3-8 | `worldScene.ts` `UNLOCK_KEYS` | the unlocks sit on 0, [ and ] (§4 gives the hotbar 1–9; - and = are the speed keys) |

The lattice M3 tags (GA-M3-1…16, the hopper drain, the shed order, the substation as a pre-existing 3×3) stand; GA-M3-16's "seeded street-side spot" is D-B1-4's centroid rule since the four pre-M2 items.

### Deferred

- **Turrets on claimed blocks** (D-P4-9): on the HQ an edge fires through its turrets; every other Held block keeps the block sim's 100-round hopper. The Electricians' block, once Held, fires the block sim's way → **M6** with D-P4-5.
- **"Supplies 7×7" / "supplies 3×3"** (§13 Pole / Big pole): a substation powers its whole cell, so a pole and a Big pole only link and claim → **M6 / Phase 5** with D-P4-9 (if a claimed block's machines ever need poling, the supply area is the rule that returns).
- **The Substation's recipe** (frames, wire, boards; §13.14) → **Phase 5**; the 50 + 25 stand-in is D-B3-1.
- **The other groups' unlocks** (Concrete crew, Foreman, Arsenal, …; §8): `SURVIVOR_UNLOCK_NAMES` names the Electricians only; the gate is the same one line a group → **M6 / Phase 5** with their machines.
- **The Floodlight's light texture** (a cone drawn as light rather than a filled sector) → **M5**.
- **The block sim's outskirts** (a Held outskirts block powered by an abstract substation the tiles do not show) → **M6** with D-P4-9; D-B3-2.
- **Hour-one power with the line on** (D-P4-7, below): the §11 line plus the HQ's start draw sheds the Shot assembler at once under one Generator → stays with **D-P4-7**, evidence added.

### Measured

Script `m3measure.ts` (scratch), the tile tick at 20/s, the block tick at 1 s; the compact walking rifle bot as the harness runs it; the §11 line laid as in the M2 measurement.

| What | Measured | Doc |
|---|---|---|
| seed 3 HQ start turrets by segment (`B-M3-segments`) | 6 turrets, 300 rounds; segments to 332 (5.0 tiles, a corner sliver): 0 of its own, 1 by reach, 50 rounds; 334 (31.4): 2, 100; 353 (41.3): 2, 100; 355 (41.0): 2, 100; no turret serves nothing | §13 one per 16 tiles, at least one a segment (D-B1-4) |
| hopper-empty → pip, no line and no hands | first hopper-empty 6:08 on the sliver; the HQ lost at 12:54 "unfed starved", 0 magazines made — the unfed consequence at tile level, as the lattice M3 soak found it | §5, §25 item 4 |
| the same with the §11 line and the bot's hands, seeds 3 / 4 / 5 | first hopper-empty 30:29 / 30:33 / 30:29, on the bot's first claim's segment (30.5 / 52.2 / 23.4 tiles), the pulse on that pip the same tick; the HQ holds the hour, 4 held, 0 lost, 845 / 783 / 810 magazines made, 132 / 108 / 68 hand-fed, buffer ≈ 3,990 | §4 the pip turns red on the transition |
| the Electricians' block, seeds 3 / 4 / 5 | 3 hops out, civic, on all three; the compact bot never claims it in the hour, so no unlock lands on the bot's toolbar — a player who claims it does (browser check) | §8 ≤ 3 blocks; §11 30–60 min |
| power on (generators, half draw) with the §11 line, seed 3 | demand 350 kW against 300 from the first tick: the Shot assembler shed 20 s in, 1 magazine made, the HQ lost at 12:25 | §11 "one Generator alone browns out at minute 8"; D-P4-7 |
| Floodlight vs Lamp on the HQ face (183 / 780 tiles lit at start) | Lamp +51 tiles, Floodlight (east) +82 tiles; face demand 200 → 240 kW of 300 | §13 r 4 / 12-tile cone, 5 / 40 kW |
| pole / Big pole, farthest centre-to-substation-edge distance still on the grid | 7.91 / 12.00 tiles | §13 reach 8 / 12 |
| outskirts faces, seeds 3 / 4 / 5 | 134 of 347 / 131 of 337 / 128 of 315 live faces have no substation and no streetlights; 213 / 206 / 187 pre-existing substations; 5,289 / 5,243 / 4,752 streetlights | §7 |
| craftable Substation on seed 3's nearest outskirts face (15 hops) | 3×3 placed for 50 steel + 25 Cu; `substationAt` → on, 100 kW; a second refused; picked up into the pockets | §13.14 (recipe Phase 5) |
| hand-feed (`B-M3-hands`) | 3 magazines in the pockets into an empty hopper → 30 / 50 rounds, pockets 0, the Depot at buffer 100 untouched; again → the "no magazines in the pockets" reason; 10 coal → the Generator 10, pockets 0 | §11 "run six magazines to its turrets" |

Browser check (`m3game.cjs`, headless swiftshader against the preview build, seed 3): the three build rows disabled with " · locked: the Electricians unlock it — hold their block"; key 0 toasts `Floodlight: the Electricians unlock it — hold their block` and the hand stays; the Electricians' block turned Held through the block sim (Contested, clock run out, one tick) enables the rows and key 0 puts the Floodlight in the hand; one placed within reach facing south; ] on the HQ lot refuses with `No substation here: the face has a substation`; with the turrets drained the map view toasts `Hopper EMPTY on block (421,634) facing (390,596) — its pip is red until it is fed` for each HQ segment on the next tick; 0 page errors.

Checks: `npm test` 103 / 103 (99 + the four M3 tests), `typecheck` (incl. the game build), `lint`, `snapshot:check` (`5f3417b9`, unchanged), `docsync:check`, `experiments` 12 / 113 s / 0 failing checks, `calibrate` (output identical to the M2 run: C1 / C2 MET). Fixtures `city{3,4,5}.json` regenerated and unchanged: the block sim's rules did not move (the outskirts' missing substation is tile-level only, GA-B3-6).

### Where prompt B M3 and the doc disagree (reported, not resolved)

- **The prompt's substation "at a street-side spot on the face's longest ridge"** vs D-B1-4's "the buildable 3×3 nearest the lot's centroid" (the human's decision before M2): D-B1-4 kept; the prompt predates it.
- **The prompt's "the pre-existing 3×3 substation per face"** vs §7's outskirts, which have none: §7 kept at tile level (134 / 131 / 128 faces on seeds 3 / 4 / 5), while the block sim still powers a Held outskirts block through its abstract substation (GA-B3-6). Decision D-B3-2.
- **§11's "the Electricians walk into the Depot: Floodlight, Big pole, Substation appear on the toolbar"** (30–60 min) vs built: the unlocks land when their block turns Held, whenever that is, and stay if it falls; §11 edited with the tag. Decision D-B3-3.
- **§13 Pole "supplies 7×7" / Big pole "supplies 3×3"**: a substation powers its cell, so both only link (the lattice M3 note); deferred with D-P4-9.
- **§13.14 the Substation's price**: frames, wire and boards are Phase 5 items; 50 steel + 25 Cu in rubble stands in. Decision D-B3-1.
- **§11 / §14 power at the start, on the city**: with power on, §11's line plus the HQ's 100 kW draw is 350 kW against one 300 kW Generator, and §14's "Shot assemblers first" sheds the ammo line 20 s after it is laid; with nothing refilling the hoppers the HQ falls at 12:25. §11 says one Generator browns out at minute 8 and D-P3-1 says hour one's lesson is ammo; at tile level with power on it is power, and the shed order's first victim is the one machine the hold depends on. Reported under D-P4-7 (open); the slice runs with power off until it settles.

### Decisions for the human (recommended in `DECISIONS.md` D-B3-1–D-B3-3)

- **D-B3-1 the Substation's price**: 50 steel + 25 Cu in rubble as built (a) / free for the Electricians, the group brings it (b) / wait for Phase 5's frames, wire and boards and leave the outskirts unholdable until then (c). Recommend (a).
- **D-B3-2 the outskirts at tile level**: no substation and no streetlights on the tiles while the block sim's abstract substation still powers a Held one, settled with D-P4-9 at M6 (a) / make the block sim require the craftable Substation for an outskirts hold now — a rules change that moves the fixtures (b) / give the outskirts a pre-existing substation too, against §7 (c). Recommend (a).
- **D-B3-3 when unlocks land and whether they stay**: on Held, kept after a fall, on keys 0 / [ / ] (a) / lost with the block until it is Held again (b) / on the group's walk into the Depot, §11's picture, which the sim has no event for (c). Recommend (a).


## Before M4 — D-P4-7 settled, D-B3-4 built (2026-09-04)

The human's decisions before M4: D-B3-1 (a) 50 steel + 25 Cu in rubble until Phase 5; D-B3-2 (a) the tile/block split through M5, settled with D-P4-9 at M6; D-B3-3 (a) as built; **D-P4-7 settled now, in two parts**, and the gate for M4 is the §11 hour on seeds 3 / 4 / 5 with power on. Run names `B-M4-gate` (scratch script `m4gate.ts`, the tile tick at 20/s, the block tick at 1 s, the compact bot's commands and hands, materials assumed on hand: 1,000 steel / 500 Cu in the pockets) and `E2-matrix` / `E2-sustained6h-h500-{50,75,90}pct` (the harness).

### Part 1 — the §11 script against E4

**What the schedule was.** The M3 measurement laid the whole §11 line at 0:00 on one 300 kW Generator and ran the hour on it: no second Generator, no coal Excavator, no stand-ins for E4's later draws. That is the pre-E4 hour (E4's post-D1 result is a second Generator at ~minute 6, a third at 15 and a fourth by 45), so the "350 vs 300 kW, the Shot assembler shed at 0:20, the HQ lost at 12:25" row was the stale script, not the doc's hour. **What it is now.** The line at E4's minutes: the three Excavators and their belts at 6:00 (steel and copper to the assembler, the coal Excavator on the HQ patch belted into the Depot) with the second Generator; the Shot assembler, three inserters and the ammo belt at 8:00; the third Generator at 15:00; E4's stand-in draws (a 60 kW Excavator at 16:34 for the east claim's copper, a 100 kW assembler at 17:34 for wire, a 60 kW Excavator at 26:34 for the west coal, an Excavator and an assembler at 45:00 for the fifth Excavator and third assembler); the fourth Generator at 45:00 (`E4-doc`, §11's schedule) or at 25:00 (`E4-check`, E4's "four by 25"). Every Generator after the first is hand-fed from the Depot's coal by the bot.

### Measured — the §11 hour, seeds 3 / 4 / 5, the M3 table's power row filled in

| Schedule | Power | HQ | Held / lost at 60:00 | Brownout s (worst throttle) | Peak demand / supply | Magazines made | First hopper-empty | Hand-fed mags | Coal at 60:00 (burned) |
|---|---|---|---|---|---|---|---|---|---|
| M3 (stale): 1 Generator, whole line at 0:00 | off | holds | 4 / 0 | — | — | 848 / 793 / 815 | 15:29 ×3 | 132 / 108 / 68 | 40 in the Generator + 702 in the Depot (0) |
| M3 (stale), the same line | **on** | **holds** | 4 / 0 | **3,600 (42 %)** — the whole hour, 410 → 710 kW on 300 | 710 / 300 kW | 500 / 512 / 512 | 15:29 ×3 | 407 / 383 / 343 | 46 + 427 (269) |
| E4-doc: Generators 0 / 6 / 15 / 45, machines at E4's minutes | off | holds | 4 / 0 | — | — | 806 / 801 / 806 | 6:18 / 5:34 / 6:21 | 255 / 231 / 195 | 166 + 576 (0) |
| **E4-doc, the same** | **on** | **holds** | 4 / 0 | **0 (100 %)** | 1,090 / 1,200 kW (600 from 6:00, 900 from 15:00, 1,200 from 45:00) | 806 / 801 / 806 | 6:18 / 5:34 / 6:21 | 831 / 807 / 771 | 103–121 in the Generators + **0** in the Depot (639 / 621 / 621) |
| **E4-check: Generators 0 / 6 / 15 / 25** | **on** | **holds** | 4 / 0 | **0 (100 %)** | 1,090 / 1,200 kW (1,200 from 25:00) | 806 / 801 / 806 | 6:18 / 5:34 / 6:21 | 831 / 805 / 769 | 103–119 + 0–2 (639 / 621 / 621) |

**The gate holds**: on seeds 3, 4 and 5 with power on, the E4 schedule in the script and proportional brownout in the sim, the §11 hour never browns out, the HQ holds, and the hour's output is the same with power on as off (806 / 801 / 806 magazines both ways). Three things the table also says:

- **The stale script holds too under D-B3-4.** One Generator against the whole line is a brownout for the whole hour at 42–73 %, and the line still makes 500 magazines and the HQ still holds; under the shed order the same run lost the HQ at 12:25. That is the difference between the two rules in one row.
- **Coal is the hour's edge.** With four Generators the HQ patch's ~700 coal is mined out at about 29 minutes (the Depot peaks at 452 at 30:00) and the Depot is bare at 60:00 on every seed, 103–121 coal in the Generators — about six minutes at 1,090 kW. E4's "west is wanted by minute 30" is now a tile-level fact; the bot has no way to bring the west's coal in (Phase 5 logistics), so a played hour past 60:00 goes dark unless the player does. Deferred to M6; reported to the human below.
- **The first red pip is at 5:34–6:21, before the line runs.** With the line at E4's minutes rather than at 0:00, the HQ's start hoppers (200 of 300 rounds, the C10 fill) run dry before the assembler's first magazine at ~8:00, and the bot's hands from the buffer carry the ring until then (771–831 magazines hand-fed over the hour against 132–255 with the line at 0:00). That is §11's "run six magazines to its turrets" and D-P3-1's "ammo, not power, is the hour-one lesson", on the tiles. M4 puts real crawlers behind that pip.

### Part 2 — D-B3-4 proportional brownout

**Built.** When demand exceeds supply every machine on the grid runs at supply ÷ demand (`PowerState.throttle`, `flow.power.throttle`; `stepFlow` ticks inserters, Excavators and assemblers with `dt × throttle`; the block sim's `productionMagPerMin` scales the same way). Nothing switches off, nothing has an order, no substation is ever stopped by power: `Block.shed`, `SimConfig.shed`, the shed stack, the shed / restore events, `Machine.shed`, `SHED_RANK` and the game's red cross are gone; a substation is binary and stops only under the unfed rule (`'unfed'`) or a shade. Events: `brownout` on entering a shortfall (once per 60 s of all-clear) and `power-ok` on leaving one that lasted ≥ 20 s. Stats: `brownoutS`, `throttleMin`, `firstBrownout`. In the game: the toast "Brownout: demand X kW over Y kW supply — every machine runs at Z % until a Generator is added or fed (§14). Nothing switches off.", "Power back: supply covers demand, every machine at full speed", the panel row "Brownout seconds · machines running at %", the world view's power line "BROWNOUT: every machine at N %", a machine's reason "… at N % (brownout)", the substation tooltip "off · no power (the block is unfed, or the grid is dead)". `firstHour` throttles the same way (its `hqExempt` option is gone with E4's `E4-hq-exempt` rows). The harness result carries `power.{brownoutS, throttleMin, firstBrownout}` and `hopperEmpty` (the pip ticks).

**Assumed (every `GAME-ASSUMPTION` added or changed):**

| Tag | Where | Assumption |
|---|---|---|
| GA in `stepFlow` (`flow.ts:692`) | sim | A Lamp or Floodlight cannot run slower: lit at any throttle above zero, dark only on a dead grid. Belts and turrets are never slowed (a brownout bites production, never the front). |
| GA-M3 (`session.ts:108`, edited) | game | The slice runs with power on (generators, half draw); a shortfall slows every machine to supply ÷ demand; nothing is shed. |
| `m4gate.ts` (scratch, not shipped) | measurement | Materials on hand for the whole hour (1,000 steel / 500 Cu in the pockets), since D-P4-4's 80 / 40 start cannot buy the line; the bot's hands feed Generators from the Depot's coal. |

**Tests.** `defence.test.ts`: the shed-order test replaced by "D-B3-4 proportional brownout" (a 300 kW grid at 410 kW demand → throttle 300 ÷ 410, the Excavator mines 60 × 0.5 × throttle ± 1 in a minute, one `brownout` event, the substation on, the Lamp lit, the reason "at 73 % (brownout)"; a 90 % shortfall → 30 ÷ demand and `throttleMin` recorded; the shortfall lifted and an assembler removed → throttle 1 and `power-ok`); the Generator test now asserts the substation is back the second the supply is; the Floodlight stays lit at throttle 0.2; the snapshot-shape test drops a pre-D-B3-4 `shed` on load. `power.test.ts`: parity with the frozen Python fixtures holds up to the minute of the Python run's first shed (≥ 10 min), then the first-brownout tick, the sample count and the D-B3-4 invariants (no unfed fall, no more lost than Python, nothing lost inside the window; full speed again by 5 h except under the §15 schedule, which stays short) — the fixtures' shed logs are history (`fixtures/README.md`). 103 / 103.

### Measured — E2 under proportional brownout (the harness, compact, D1 draw, production and the unfed rule on, shortfall from hour 4)

| Window | Headroom | Shortfall | Lost after start (per seed) | Brownout min | Worst throttle | First red pip (min after start) | First fall | Lead (fall − pip) | Held at end |
|---|---|---|---|---|---|---|---|---|---|
| 10 min | 500 kW | 5 / 15 / 25 / 50 % | 0 | 0 / 5 / 10 / 10 | 1.00 / 0.98 / 0.86 / 0.58 | never | never | — | — |
| 10 min | 1 MW | 25 / 50 % | 0 | 2 / 10 | 0.99 / 0.67 | never | never | — | — |
| to 5 h (60 min) | 500 kW | 5 / 15 / 25 / 50 % | 0 | 11 / 50 / 60 / 60 | 0.97 / 0.87 / 0.77 / 0.51 | never | never | — | — |
| to 5 h | 1 MW | 25 / 50 % | 0 | 40 / 60 | 0.89 / 0.59 | never | never | — | — |
| **6 h (10 h run)** | 500 kW | **50 %** | 60 / 34 / 58 | 360 | 0.46 | **66.9 / 215.6 / 78.0** | 71.5 / 223.1 / 81.0 | **4.6 / 7.5 / 2.9** | 52 / 78 / 54 |
| 6 h | 500 kW | 75 % | 96 / 66 / 74 | 360 | 0.28 | 17.5 / 34.5 / 21.9 | 22.2 / 37.7 / 24.8 | 4.7 / 3.2 / 2.9 | 16 / 46 / 38 |
| 6 h | 500 kW | 90 % | 97 / 96 / 96 | 360 | 0.11 | 11.6 / 14.5 / 15.6 | 14.5 / 16.2 / 17.6 | 2.9 / 1.7 / 2.1 | 15 / 16 / 16 |

**Does a sustained shortfall starve the ring slowly enough that the red pip is the warning?** Yes, with one qualification. Slowly: a 50 % shortfall from hour 4 costs nothing for the first 67–216 minutes (the Depot buffer, filled over four hours, carries the ring at half production), and a 10-minute or one-hour shortfall of any depth up to 50 % costs nothing at all — the 5 h runs of the old matrix never lose a block, which is why the sustained window had to grow to 6 h for the check to fire. Behind the pip: the red pip led the first fall on every losing seed in every run (checks `E2-sustained6h-h500-{50,75,90}pct` green). The qualification is the lead: 1.7–7.5 minutes, and shorter the deeper the shortfall, because a brownout drains the buffer first and the hopper last, so by the time a pip goes red the buffer is gone and the fortieth arrival is one hopper-drain away. The warning the player actually has is the HUD bar (brownout seconds and the percentage, on from the first second) plus the Depot's buffer count; the pip is the last call, not the first. Once the ring starts falling it keeps falling for as long as the shortfall does (52 / 78 / 54 of the blocks held at 10 h under 50 %, 15 / 16 / 16 under 90 %); nothing cascades — every loss is `unfed`, none is power.

### Doc (§ edited, changelog lines at the end of `RELIGHT-design.md`)

§5 (how a block falls: three ways, a brownout is not one; the cascade paragraph replaced by the slow-starvation measurement), §13 Floodlight (lit at any throttle), §14 Power (the rule, its legibility, what a shortfall costs, lights, belts and turrets, the dead grid), §19 (one HUD number, nothing stops), §23 (turrets-need-power: the dependency stays one step removed, the speed measured), §24 risks 7 and 9 (retired by D-B3-4, the remaining risk named), §25 item 14 and §11 0–10 / 10–30 min (the tile-level hour on the E4 schedule, `[sim: B-M4-gate]`), §26 (a power model with a proportional brownout). Three changelog lines. `docsync:check` green (no table number moved).

### Where this and the doc disagree (reported, not resolved)

- **§11's "one Generator alone browns out at minute 8"** is still the block model's ramp; at tile level the brownout is at 0:00 if the line is laid at once (the stale script) and never if the line follows E4's minutes. The sentence is kept with the tag; §11's minute 8 is not a tile-level number.
- **Coal past the hour** (§11 10–30 min: "coal does not run out in hour one, but 609 of the 740 coal in reach are burned by minute 60") — on the tiles 639 / 621 / 621 of 740 are burned and the Depot is bare at 60:00. The doc's number holds; what the doc does not say is that the tile layer has no way to bring west's coal in yet.
- **§5's brownout timing** (the old "grid demand > supply for 20 s") is gone; the 20 s survives only as the `power-ok` event's minimum shortfall. A rule, not a constant.

### Checks

`npm test` 103 / 103; `typecheck` (incl. the game build); `lint`; `snapshot:check` — the snapshot regenerated: config hash **`5f3417b9` → `68d07000`** (SimConfig lost `shed`; the state shape lost `Block.shed`, `PowerState.{overTimer, shedStack, lastShed, asmShed}`, `Stats.{shedEvents, shedLog}`, `Machine.shed`, and gained `PowerState.{throttle, short, shortAt, okAt}`, `flow.power.throttle`, `Stats.{brownoutS, throttleMin}`); `docsync:check`; `experiments` 12 / 107 s / 0 failing checks (E2 rewritten: 8 checks; E4's `E4-hq-exempt` rows dropped); `calibrate` — results identical to M3 (C1 / C2 MET), only the config hash in its header moved (`5eae8618` → `03328db7`). Fixtures `city{3,4,5}.json` regenerated and **unchanged** (power is off in them): the block rules the D6 regression set pins did not move. The frozen `lattice/power*.json` fixtures are pinned on their pre-first-shed prefix (above).

### Decisions for the human (`DECISIONS.md`)

- **D-B3-4** — made by the human, built as specified; the row records what the sim found (above) and the one GA the rule needed (lights, belts and turrets are never slowed).
- **D-P4-7** — made: settled before M4; the script realigned, the gate holds, the Generator-feed question moot.
- **New, not a decision row yet, for the human to place:** (1) *coal past the hour* — the HQ patch is out by minute 29 and the Depot bare at 60:00 with four Generators; §11 leans on west's rubble, which Phase 5 logistics bring — does M6's played hour end at 60:00, or does the slice need a coal stand-in (a bigger patch, or the bot's hands fetching from a claimed block) before then? (2) *the pip's lead* — under a sustained brownout the red pip leads the fall by 2–8 minutes because the buffer drains first; if that is too short, the candidates are a Depot-buffer warning on the HUD (a number the player already sees) or an amber pip at buffer-empty, not a change to the rule. (3) *the start hoppers* — with the line at E4's minutes the first red pip is at 5:34–6:21 on every seed, before the assembler's first magazine; M4's crawlers decide whether that is §11's lesson or a start-fill (C10) question for M6.

## Prompt B M4 Threat, and the rifle — built 2026-09-04

Run names `B-M4-threat` (crawlers as bodies on the tiles: the ridge, the chain, the turrets, retaliation, the rifle; scratch probes on seed 3) and `B-M4-hour` (the §11 hour on the E4 schedule with the tile threat on, seeds 3 / 4 / 5, the bot's rifle off and on). The block model's engagement arithmetic (`sim.ts`) is untouched on the lattice and on stand-in edges; on a city edge with physical turrets it now hands its arrivals to the tiles and takes the count back.

### Built

- **Rot as tile presence** (`worldScene.ts` overlay): a Dark lot's navy tint deepens with the block's `d` and carries hashed rot specks at zoom ≥ 0.6; nothing is simulated on the lot, it is the block map's word drawn.
- **Blooms at the ridge** (`threat.ts` `spawnAt`; `sim.ts` engagement loop): when the block sim opens an engagement on an edge the tiles know (the HQ's and any Held face's segments; `threatHooks.spawn`), its crawlers and shades are born on the segment ridge facing the held block, one tile a body chosen by a seeded hash along the ridge and the nearest passable tile from there (GA-B4-2). A stand-in edge (D-P4-9, a claimed block's abstract kit) keeps the block-level fed count and hands only its unfed remainder to the tiles as already-escaped bodies (GA-B4-4). No hulks.
- **The per-face flow field** (`fieldFor`, `stepCrawler`): a multi-source breadth-first field from the target class's footprints over the passable tiles of the two blocks the edge joins, 8-connected without corner cutting, cached per (face, from, class) and rebuilt at most once a block second (GA-B4-5); a crawler never leaves its two blocks. Target classes: the lit lamps and Floodlights (class 0, eaten from the next tile, `EAT_R` 1.6, the light stays dark until M5's repair, GA-B4-7), then the turret as a waypoint it does not harm (class 1, GA-B4-6), then the substation footprint — on an outskirts block the pole tile (class 2, GA-B4-3). Crawlers walk 3 tiles/s, shades 2 (`ENEMIES`); a body that cannot move for 30 s is gone (`STUCK_S`).
- **The 40-arrival rule at tile level** (`arrive`): a body that reaches class 2 counts one unshot arrival on the block (`b.unfed`, `stats.unfedTotal`), the block sim's threshold and 30 s substation-off for shades unchanged; the `arrival` event fires on the 1st and every 10th (a toast "n of 40").
- **Shades** only when a residential block's rot reaches its threshold (the block sim's `shades` count, as before); untargetable by turrets and the rifle off lit tiles (`litAt`), walk straight to the substation (GA-B4-1).
- **Turrets shoot bodies** (`tickTurrets`): each running turret takes the nearest body within `TURRET_RANGE` 9 out of its own hopper, `ROUND_DMG` 4 a round (12 HP ÷ 3 rounds a crawler, 40 ÷ 10 a shade, GA-B4-1), 0.6 s a crawler at the rate; the hopper-empty pip is the same event.
- **Retaliation only (D5)** (`turn`, `stepCrawler`): a crawler turns on the engineer only when the engineer's round hits it or the engineer stands in its path (`PATH_R` 0.9; a `retaliate` event with the cause, a toast); it then chases while the engineer is up and within its two blocks (GA-B4-8) and lands 5 HP/s within `CONTACT_R` 1.2 unless the dash's cover holds (GA-B4-9). Danger is any body within `DANGER_R` 3 (the M2 `dangerS` clock).
- **HP, regen, knockdown** (`engineer.ts` `hurt` / `tickEngineer`): 100 HP, 5 HP/s regen after 5 s out of contact, knockdown at zero (the `engineer-down` toast), up at the HQ workbench 10 s later with pockets intact (`engineer-up` toast: "no other penalty"); the HUD shows HP only below full; a body under the cursor is described (`describeCrawler`).
- **The rifle** (`walk.ts` `fireRound` → `threatHooks.fire`; `threat.ts` `fire`): a round along the aim line, `RIFLE_RANGE` 9, hits the first body within `RIFLE_HIT_RADIUS` 1.5 of the line; 1.5 rounds/s, 3 rounds a crawler, a shade off a lit tile cannot be hit. Fired with the left mouse button as D-B1-5 has it (the prompt's right-mouse wording is a disagreement, below). The harness bot's rifle (`botRifle`) shoots the nearest body of the edge it is engaging.
- **Hand-fired engagements** (`Fight`, `stats.fights`): every engagement the engineer fires into records the edge, the minute, rounds, kills and whether the edge held (resolved true when no body and no engagement remains a second after the last round, false when the block is lost or its substation stops).
- **The Arsenal's Rifle Mk2** (`map.ts` `SURVIVOR_UNLOCK_NAMES`, `panel.ts`): a toolbar entry only, "locked: the Arsenal unlock it — hold their block" until the Arsenal's block turns Held, then "the upgrade itself is outside the hour" (GA-B4-11). The truck stays outside the hour.
- **Telemetry** (`telemetry.ts`, `flow.ts` `stats`): minutes walked per sim hour (`walkedHour`), trips to the chest (`chestTrips`, the first transaction after leaving the Depot's reach, GA-B4-10), placements refused for reach (`reachRefused`, one a click, GA-B4-12), the minute of the first rifle shot, rounds fired by hand, kills by hand, HP lost, knockdowns, and every hand-fired engagement with its held flag; the minute record adds crawlers alive, arrivals, lamps eaten, rifle kills and HP lost.
- **In-game** (`main.ts`, `worldScene.ts`): toasts for `bloom` (only on or beside the engineer's block, GA-B4-13), `retaliate`, `lamp-eaten`, `arrival`, `engineer-up`; bodies drawn as dark discs with a birth ring, a red rim once turned, an HP bar once hurt, shades faint and only on lit tiles; the HUD line "crawlers N (M on you)".
- **Tests** (`threat.test.ts` 7, `body.test.ts` edited): the chain on seed 3's HQ (lamps out one by one, the turret waypoint, the substation, 40 arrivals, the fall); fed turrets at 3 rounds a crawler draining to the same pip; retaliation only (ignored beside the path, turned by a shot, turned by standing in the path, 5 HP/s, knockdown and the 10 s return with pockets intact, regen after 5 s); the rifle's rate, damage, range and the shade rule; hand fights resolving held / fell; the lattice untouched (no threat, the block arithmetic identical); an M3 snapshot loads without the M4 fields. The M1 rifle test aims at the nearest body instead of the block-level ridge — a test change, not a rules change.

### Assumed (every `GAME-ASSUMPTION` in prompt B M4 code)

| Tag | Where | Assumption |
|---|---|---|
| GA-B4-1 | `threat.ts` header, `ROUND_DMG` / `CONTACT_R` / `DANGER_R` / `PATH_R` / `EAT_R` / `STUCK_S` | a round does 4 HP (12 ÷ 3 a crawler, 40 ÷ 10 a shade); arm's reach is 1.2 tiles; danger is a body within 3; "stands in its path" is within 0.9 of the path; a lamp is eaten from 1.6; a body stuck 30 s is gone; shades walk straight to the substation — D-B4-2 |
| GA-B4-2 | `threat.ts` `spawnAt` | the birth tile is a seeded hash along the ridge, then the nearest passable ridge tile |
| GA-B4-3 | `threat.ts` `targetsFor` | an outskirts block has no substation, so class 2 is its pole tile |
| GA-B4-4 | `sim.ts` engagement loop | an edge with physical turrets hands all its arrivals to the tiles; a stand-in edge feeds at block level and hands only the unfed remainder over as escaped bodies |
| GA-B4-5 | `threat.ts` `fieldFor` | a flow field is rebuilt at most once a block second per (face, class), so a machine placed mid-second reroutes a moment late |
| GA-B4-6 | `threat.ts` `arrive` | crawlers do not harm a turret (§7 gives them no such attack); it is the chain's waypoint — D-B4-3 |
| GA-B4-7 | `flow.ts` `placeMachine`, `blockLights` | a light a crawler ate stays dark until M5 repairs it; a new machine on its tile is a fresh lamp |
| GA-B4-8 | `threat.ts` `stepCrawler` | a turned crawler keeps chasing while the engineer is up and within its two blocks; leaving them ends it |
| GA-B4-9 | `threat.ts` `stepCrawler` | the dash's cover (D5) holds at tile level too |
| GA-B4-10 | `flow.ts` `chestTrip` | a trip to the chest is the first transaction after being out of the Depot's reach |
| GA-B4-11 | `panel.ts` | the Arsenal's Rifle Mk2 is a toolbar entry only; the upgrade itself is outside the hour |
| GA-B4-12 | `worldScene.ts` `reachable` | placements refused for reach count one a click, not one a drag step |
| GA-B4-13 | `main.ts` `bloom` toast | only the blooms on or beside the engineer's block toast; the rest are the map view's pulses |

The M1–M3 and lattice tags stand. GA-M3's "the slice runs with power on" stands (`session.ts`).

### Deferred

- **Rifle Mk2 itself** (1.4 s a crawler, §8) and **the truck** → outside the hour, **M6 / Phase 5**; the toolbar entry is built.
- **Hulks** → **Phase 5** (§7 gives them the barricade chain; no barricade in the slice).
- **Shades inside the hour**: no residential block reached its threshold on seeds 3 / 4 / 5 in the §11 hour, so the shade rules are tested (`threat.test.ts`) but not measured in play → **M6** on the played hour.
- **Lights a crawler ate** stay dark until repair → **M5** (the light texture and repair).
- **The stand-in edge's bodies** (D-P4-9): a claimed block's unfed remainder walks the tiles, but its fed count is the block sim's → **M6** with D-P4-5 / D-P4-9.
- **Crawlers vs the player's own machines** (belts, inserters, the assembler): bodies walk around them and never harm them → **Phase 5** with §7's barricade.
- **A player's minutes walked** (§19's 15 %): the bot's 2.6–4.1 % is a bot's → **M6** playtest.
- **The first pip before the line** (the M3 re-read): measured below with real bodies; the start hoppers / buffer question → **M6**.

### Measured

Scratch scripts `m4measure.ts` (the E4-doc schedule, seeds 3 / 4 / 5, the compact bot with and without its rifle, events taken each block second) and `hopprobe.ts` / `chestprobe.ts`; the tile tick at 20/s.

| What | Measured | Doc |
|---|---|---|
| the chain, seed 3 HQ, one 10-crawler bloom on a dry edge (`B-M4-threat`) | born on the segment ridge; the pack eats all 14 lit lights of the face (first at 3 s, the last by 28 s), walks to the turret (unharmed), first substation arrival 29 s, the 40th arrival and the substation off at 68 s, the block lost at 158 s | §7 lamp → turret → substation, 40 arrivals |
| the same with fed turrets | 4 × 10 crawlers all killed, 100 rounds for 33 kills (3.0 a kill), first hopper-empty at 26 s, 0 arrivals, 0 lights eaten | §13 3 rounds a crawler, 0.6 s |
| shades, seed 3 | untargetable off lit tiles; first shade arrival at 49 s against a dry edge; on a lit tile a turret takes 10 rounds | §7, §13 |
| §11 hour, E4 schedule, power on, the bot's rifle off (`B-M4-hour`), seeds 3 / 4 / 5 | 467 / 370 / 241 crawlers born in 104 / 81 / 78 blooms (first 2:46 / 2:42 / 3:03); 461 / 370 / 241 turret kills; arrivals 6 / 0 / 0 (seed 3's first at 31:17); lights eaten 5 / 9 / 5 (first 3:05 / 5:39 / 9:28); peak 5 alive, 467 / 324 / 209 s with any alive; 0 shades; the HQ holds, 4 held, 0 lost, 0 brownout s, peak 1,090 of 1,200 kW; 806 / 770 / 784 magazines made; first hopper-empty 15:29 / 5:37 / 15:29 | §11, §7 |
| the same with the bot's rifle on | the same threat; seed 4: first shot 8:19, 3 rounds by hand, 1 kill, 1 turned, one hand fight `8:19 e3242 3 rounds / 1 kill, held`, 2 s shooting, 2 s danger; seeds 3 / 5 never shoot; 0 HP lost, 0 knockdowns on every seed; lights eaten 10 / 13 / 7; first hopper-empty 6:11 / 5:34 / 6:10 on an HQ segment with the buffer at 0 | §11 "run six magazines" |
| the engineer's hour (bot) | walked 2 min of 60 (3.5 / 2.6 / 4.1 %), 0 chest trips, 0 placements refused (the bot places from the Depot's reach) | §19 15 % walking |
| the first pip with the rifle bot | `restock` pulls the buffer's 10 start magazines into the bot's pockets (15 magazines) at 0:00, so the hands cannot feed the HQ's corner sliver and it runs dry at 6:10–6:11; with the rifle off the buffer's 10 keep it fed to 15:29 — a harness-bot artefact (the pre-M4 gate's 6:18 was this), not a rules change | §11 |
| a 1 h sim at 4× in the world view (`soak.cjs`, headless swiftshader, seed 3, autoplay compact) | 43,781 frames over 900 s real (sim 5,406 s: 1 h 30 min), mean 19.0–23.2 ms by minute, worst 33.3 ms, 0 frames over 50 ms, sim 0.03–0.82 ms a tick; the HQ held with §11's line and two Generators, 729 magazines made, 1,139 turret kills, 5 lamps eaten, 0 arrivals, no page errors. The first soak, before the speck fix and without the line, fell to 24 fps from sim minute 12 (circles a tile as the rot deepened) and lost the HQ at ~19:30 on dry hoppers | prompt B "must not drop the render loop" |

Browser check: the world view at seed 3 draws the bodies, the birth ring and the HP bar; the hover line names a crawler ("crawler, 8 of 12 HP, for the lamps, turned on you"); the HP line appears only below full; the "Rifle Mk2" row reads "locked: the Arsenal unlock it — hold their block". The map scene's hopper-empty pulse crashed a world-view session before the map scene's `create()` ran (`this.geom` unset, sim 6:11 on the first soak); guarded, the pulse sits on the block until the map is opened.

Checks: `npm test` 110 / 110 (103 + the seven M4 tests), `typecheck` (incl. the game build), `lint`, `snapshot:check` (`68d07000`, unchanged), `docsync:check`, `experiments` 12 / 108 s / 0 failing checks, `calibrate` output identical (C1 / C2 MET, C7 / i1 / i2 as before). Fixtures `city{3,4,5}.json` unchanged (the block sim's arithmetic is the same on the lattice and on the city's stand-in edges).

### Where prompt B M4 and the doc disagree (reported, not resolved)

- **The prompt's "fired with the right mouse button held"** vs D-B1-5's left-click (the human's decision at M1): D-B1-5 kept; the rifle fires on the left button while the cursor aims. Decision D-B4-1.
- **§7 "nearest lamp, then turret, then the substation"** vs built: the turret is a waypoint the crawlers pass unharmed (§7 gives a crawler no attack on a turret; a turret is a machine, and §7's damage is "lights go out"). Decision D-B4-3.
- **§19 "15 % is walking or driving"**: the harness bot walks 2.6–4.1 % of the §11 hour; the doc's number is a player's estimate and stays, tagged with the bot's.
- **§11 "run six magazines to its turrets"** against a first red pip at 5:34–6:21 with the line at E4's minutes: the pip is real at tile level with real bodies (the sliver's two turrets against 3–5 crawlers a bloom); the bot's hands carry it. Not resolved; M6.
- **§7's shades "once residential rot reaches its threshold"**: never inside the hour on seeds 3 / 4 / 5 (rot does not reach it before 60:00); tested, not played.
- **The prompt's "never more than two blocks"** holds by construction (the field is restricted to the two blocks); the doc's §17 "cached per block edge" is per (face, from, class) — the same thing at one more level of detail.

### Decisions for the human (recommended in `DECISIONS.md` D-B4-1–D-B4-3)

- **D-B4-1 the rifle's button**: keep D-B1-5's left click, the cursor aims (a) / the prompt's right button held, left free for placement (b) / left fires only with the rifle key held, R (c). Recommend (a).
- **D-B4-2 the threat constants** (`threat.ts` header): as built — 4 HP a round, 1.2 arm's reach, 3 danger, 0.9 path, 1.6 eat, 30 s stuck (a) / widen the path to 1.5 so a player standing near a chain is turned on more often, the D5 feel (b) / shorten stuck to 10 s (c). Recommend (a) until M6's playtest has a number.
- **D-B4-3 the turret in the chain**: a waypoint the crawlers pass unharmed, as built (a) / a turret takes contact damage (a new HP number for a machine) and the chain is lamp → turret → substation as §7 reads (b) / drop the turret from the chain, lamps then the substation (c). Recommend (a); §7 edited to say "then past the turret" only if (a) is chosen.


## Prompt B M5 Light — built 2026-09-04, verified 2026-09-04

Run name `B-M5-light` (the light map as data, the streetlight sequence, the burn-off, repair; `light.test.ts`). **Verified 2026-09-04** on the pass that also covered M6 (`PROGRAMME_STATE.md` B.15): every scripted check green after three test fixes, one 1-ulp snapshot regeneration and no sim rule moved; the Measured section is the run's numbers.

### Built

- **The light map as data** (`light.ts` `lightMask`, `stampLight`, `litCount`): one byte per tile, 1 where a lit light covers it under the same `lightCovers` rule `litAt` uses for the shade, so the picture and the threat never disagree. Streets are lit by their kerb streetlights (one every 4 tiles, radius 4: a powered face's street is lit end to end but for the 3-in-8 broken gaps); lots only within a Lamp's radius 4 or a Floodlight's 12-tile cone (M4's deferral: the cone is now light in the texture, not only the shade test).
- **The light texture** (`worldScene.ts` `refreshLight` / `paintLight`): a Phaser canvas texture the city's size, one texel per tile, scaled to the tile and multiplied over the ground and the machines at depth 1.5; lit texels white, unlit `[62, 66, 98]` (≈ 25 % with a cool cast, GA-B5-1); re-read from the sim eight times a second (GA-B5-2) and re-uploaded only when a texel changed.
- **Rot cannot exist on a lit tile** (`draw`): the Dark navy tint, the Contested amber and the rot specks are skipped on every lit tile of the mask.
- **The burn-off** (`sim.ts` `burnOffS` = 20 + 60·d, `claim`; `flow.ts` `contestProgress`): the claim's `contestUntil = t + burnOffS(d)` is the same number the block sim always used, now named and read back as a 0 → 1 progress. On a Contested lot the specks within `SWEEP_R` 16 · progress tiles of every lit lamp are gone (GA-B5-4), so the rot fades outward from the lamps and the far corners clear last; Held ends it.
- **The streetlight sequence** (`flow.ts` `LIGHT_SEQ_PER_S` 3, `lightRanks`, `blockLights`): on a claim the face's sound streetlights come on one every ⅓ s from the substation outward (§6's "three per second"; the order is straight-line distance, GA-B5-3); broken ones stay dark; the block sim's power gate still holds (no power, no light). A `Light` now carries `why` ('' / 'broken' / 'eaten').
- **Repair** (`flow.ts` `lightAt` / `canRepair` / `repairLight`; `worldScene.ts` `interact`): E on a broken (§13's 3-in-8) or eaten (M4) streetlight or Lamp within reach spends `REPAIR_COPPER` 1 Cu from the pockets (GA-B5-5); a §13-broken streetlight joins `flow.repaired` for good (GA-B5-6), an eaten light leaves `threat.broken`; `flow.repairs` counts both. A refusal is a toast with its reason ("No repair: no copper in the pockets (1 Cu)").
- **The engineer carries no light** (D-B5-1); `?handlamp=1` or `__relight.handLamp(true)` previews a 2-tile disc on the sprite in the light map only (GA-B5-7).
- **In-game** (`main.ts`, `worldScene.ts`): the `claim` toast says the lights come on now, 3 a second from the substation out, and in how many seconds the rot burns off; the `held` toast says the burn-off is done and the streets lit, the lot dark until Lamps; the `lamp-eaten` toast ends "E on the lamp repairs it (1 Cu)"; the hover line marks an unlit tile ("rot can sit here; a shade here cannot be hit") and names a light's state (lit · radius 4 / coming on (burn-off n %) / off · no power / off · its block is Dark / broken or put out by a crawler — E repairs it); the "Nothing here" toast lists the repair; streetlight posts draw a glow when lit and a cross (red broken, violet eaten) when not, the light itself being the texture.
- **Tests** (`light.test.ts` 4, unwritten-run): the HQ kerb mostly lit and its lot not, the pole of inaccessibility unlit, the mask agreeing with `litAt` on 400 lot tiles, Dark lots ≤ 25 % lit; a Lamp's disc at radius 4; the sequence at 3/s nearest the substation first, `contestUntil` = 20 + 60·d, progress mid-way, Held at its end; repair of a broken streetlight and an eaten Lamp, refused without copper and on a bare tile.

### Assumed (every `GAME-ASSUMPTION` in prompt B M5 code)

| Tag | Where | Assumption |
|---|---|---|
| GA-B5-1 | `worldScene.ts` `UNLIT_RGB` | the unlit texel is a multiply of ≈ 25 % with a cool cast; a multiply cannot desaturate (§4 says "desaturated and darkened"), so the cast stands in until Phase 12's art pass |
| GA-B5-2 | `worldScene.ts` `LIGHT_REFRESH_MS` | the light map is re-read eight times a second, so a light shows at most 125 ms after the sim lit it (the sequence is 3 a second) |
| GA-B5-3 | `flow.ts` `lightRanks` | the sequence's order is straight-line distance from the substation's centre (the pole on an outskirts block), not the walk along the kerb |
| GA-B5-4 | `worldScene.ts` `SWEEP_R` | the burn-off's sweep clears specks within 16 · progress tiles of every lit lamp; a drawing rule — the rot itself is still the block's `d` |
| GA-B5-5 | `flow.ts` `REPAIR_COPPER` | a repair is 1 Cu (wire) from the pockets; §13 prices no repair |
| GA-B5-6 | `flow.ts` `blockLights`, `repairLight` | a repaired §13-broken streetlight is sound for good; an eaten light can be eaten again |
| GA-B5-7 | `worldScene.ts` `HAND_LAMP_R` | the hand-lamp preview is drawing only: it lights no tile for the shade rule or the rot |

GA-B4-7 ("a light a crawler ate stays dark until M5 repairs it") is closed by the repair. The M1–M4 and lattice tags stand.

### Deferred

- **Copper for repairs in the hour**: a face's 3-in-8 broken streetlights leave unlit street tiles until repaired or a Lamp fills them; whether the hour's copper stretches to it is a played question → **M6**.
- **The unlit look** (§4's desaturation): a multiply cannot desaturate; a shader or a second layer → **Phase 12**.
- **The hand lamp** (D-B5-1) → the human; the preview is in, off by default.
- **The sequence's pace against the burn-off's length**: §6's three per second lights a 12-light face in 4 s; the prompt's "sweeps light along the ridges over 20 + 60·d s" would spread it over 38–80 s → D-B5-2.
- **Light on the map view**: the map's squares do not read the mask (Contested is the amber flicker there) → **M6** if the hour needs it.
- **Shades on lit tiles in play** → **M6** (no shade inside the hour on seeds 3 / 4 / 5 at M4).
- **A frame-time cost**: a city-sized texture upload on every change during a sequence (8 a second at most) → the verification pass's soak.

### Measured (verification pass, 2026-09-04)

| What | Measured | Doc |
|---|---|---|
| the scripted checks | `npm test` 121 / 121 (the four `light.test.ts` cases pass after three fixes to the tests, none to the sim: the kerb assertion's denominator, the sequence's slot count, a lamp placed on the Depot's tile); `typecheck` and `lint` clean; `snapshot:check` red by one value — `blocks[286].contestUntil` differs in its last digit because `burnOffS` associates `t + (20 + 60·d)` where the old code wrote `t + 20 + 60·d`; the summary and config hash `68d07000` are unchanged, the snapshot was regenerated, not a rules change; `docsync:check` match; `experiments` 13 / 137 s / 0 red; `calibrate` byte-identical; the D6 fixtures `city{3,4,5}.json` unchanged | — |
| the lit kerb of a powered face (`litCount`, seed 3 / 4 / 5 HQ) | the **kerb row** (the street tiles touching the lot): 72 / 112 = 64 %, 101 / 148 = 68 %, 61 / 121 = 50 %; the **half-street to the midline** (`G.near`, 6–7 tiles deep): 41 %, 34 %, 26 %; the lot 24 % (the streetlights' radius reaching in; no Lamp); the HQ's broken share 6 / 20, 10 / 25, 12 / 21 against §13's 3 in 8 | §4, §5 "streetlights come on" — **not "end to end"**, below |
| a claimed block's lights | the sequence runs over every light of the face including the broken ones (24 slots, 16 sound on seed 3's first claim), so the last sound light is on at N ÷ 3 s = 8 s, not sound ÷ 3 s; nearest the substation first | §6 three per second — the pace holds, the count is the face's, below |
| the burn-off | `contestUntil` = 20 + 60·d exactly (the test), the progress mid-way, Held at its end; in E-hour east (d ≈ 0.2) burned off in 30–35 s, west and north in 29–35 s | §5 step 4; §11 "40-second burn-off" edited, below |
| a repair | 1 Cu; a §13-broken streetlight lit again at once while its face is powered, an eaten Lamp likewise; refused without copper and on a bare tile (the test) | — |
| the frame time with the light map | the 900 s soak (M6's, `?autoplay=hour&rifle=1&view=world&seed=3`, 4×, headless swiftshader 1280×800): 36,947 frames over 902.9 s real (sim 1 h 30 min), mean 40.9 fps, per-minute 28–49.5 fps, worst 68.3 ms, **61 frames over 50 ms in five bursts** at real 140 / 240 / 710 / 740 / 860 s (the minutes between them 0 over 50 ms). M4's soak on the same rig: 43,781 frames, worst 33.3 ms, 0 over 50 ms. A regression flag, not a measurement (headless swiftshader is for regressions only): the bursts fall on the hour bot's long walks and claims, not on the light sequence, and the texture's 8 Hz re-read did not move the per-minute mean outside M4's band in the quiet minutes (20–24 ms) | prompt B "must not drop the render loop" — the reference-machine soak is the human's |
| the Playwright check | `?view=world&seed=3` under the soak: the multiply visible (lit kerbs pale, the lots and Dark blocks at the unlit level), lit streets and dark lots in the screenshot; the repair by E and `?handlamp=1` were not exercised by the script — they are the played hour's | — |

### Where prompt B M5 and the doc disagree (reported, not resolved)

- **The prompt's "burn-off sweeps light along the claimed face's ridges over 20 + 60·d s"** vs §6's "streetlights come on in sequence down the street at three per second": built to §6 (the lights are on in 4 s; the rot's specks sweep over the 20 + 60·d s). Decision D-B5-2.
- **§4 "desaturated and darkened to ~25 %"**: a multiply darkens and tints but cannot desaturate; the cool cast stands in (GA-B5-1). Decision D-B5-3 carries the unlit level.
- **The prompt's "streets and lamp radii lit, lots unlit"**: read as the kerb streetlights' radii covering the street; a face's broken 3-in-8 leaves gaps on the street until repaired. Not a disagreement, a reading — stated so the human can say otherwise.
- **§5 step 3 "the rot begins burning off from the lamps outward"**: built as drawing (the specks), the rot number itself is still the block's `d` falling by the block sim's rule; the doc's sentence is about the picture and stands.
- **Verified: "a powered face's street is lit end to end but for the broken gaps"** (this report's Built and the M5 test as first written) vs the measurement: the kerb row is 50–68 % lit and the half-street to the midline 26–41 %. Two causes, neither a bug: §13's broken share came out 30–57 % on the three HQs (3 in 8 = 37.5 % is the rule, the hash lands where it lands), and a radius-4 light on a kerb one every 4 tiles covers the kerb row but not a street 6–7 tiles deep. The picture is a lit kerb with a dark road, not a lit street. Reported, not resolved: the doc's §4 sentence is a picture, the human decides whether a street should read lit (a bigger radius, lights on both kerbs, or repairs as the opening's chore — copper for repairs is in `DEFERRED.md`). The test now asserts the kerb row over 50 % and the half-street over 25 %.
- **Verified: the sequence's count.** A face's broken lights keep their slot in the 3 a second sequence (`blockLights` ranks every light), so a 24-light face with 16 sound is fully lit at 8 s, not 5.3 s. §6 says nothing either way; the built rule is "three slots a second". Stated so the human can choose "three sound lights a second" instead.

### Decisions for the human (recommended in `DECISIONS.md` D-B5-1–D-B5-3)

- **D-B5-1 the hand lamp**: none — the engineer carries no light, the dark is the dark (a) / a 2-tile lamp on the sprite, drawing only (b) / a 2-tile lamp that counts as light for the shade rule and the rot (c). Recommend (a); the preview (b) is built behind `?handlamp=1`.
- **D-B5-2 the sequence's pace**: §6's three per second, the sweep of the specks carrying the 20 + 60·d s (a) / the prompt's spread — the face's lights come on evenly over the burn-off (b) / three per second for the lights, and the Lamps on the lot come on at the end (c). Recommend (a).
- **D-B5-3 the repair cost and the unlit level**: 1 Cu a repair and the unlit multiply at ≈ 25 % with a cool cast, as built (a) / 2 Cu and 20 % (darker, the dark reads harder) (b) / repairs free, 35 % (c). Recommend (a) until M6's played hour says the copper runs short.

## Prompt B M6 The hour — built 2026-09-04, verified 2026-09-04

Run name `B-M6-hour` (the §11 hour bot on the tile layer, the command-log replay, E-hour; `hour.test.ts`). **Verified 2026-09-04** (`PROGRAMME_STATE.md` B.15): the bot has now played the hour six times in the harness (seeds 3 / 4 / 5 × rifle off / on, `E-hour`, `docs/experiments/E-hour.json`) and once in the browser under the 900 s soak. One sim fix came out of the pass — `replayVerdict` compared a fight's own outcome with the block's end state in the replay and read "saved it" on a block that fell in both runs; it now compares the block's end state in both runs (`FightVerdict.fightHeld` keeps the fight's own outcome). The Measured section is the run's numbers; the findings are under "Where prompt B M6 and the doc disagree".

### Built

- **The hour bot** (`hour.ts` `createHourBot`, `hourCommands`, `hourSteps`): §11's minute list as a task queue on the tile layer — walk (a `move` to a stand tile beside the target, the path length and the seconds written down), do, wait-until (with a deadline; a timeout is a refusal in the log). 0:00 to the steel patch tile nearest the Depot, hand-mine 20 steel (`mineAt`), back to the workbench, 10 Cu from the chest, craft ten magazines (`craft`), 20 more from the chest, magazines walked to the turrets west-then-east and again after a chest trip (`feed`); 6:00 the chest trip for the coal Excavator and its belts into the Depot, Generator 2 fed, the steel and copper Excavators and their belts (M4's proven lot layout); 8:00 the Shot assembler, three inserters and the ammo belt; 15:00 Generator 3, then six kits and **claim east** (the HQ's Dark candidate neighbour most eastward, a map click), the walk over timed, standing on the lot until it is Held and every edge kitted, back to the chest; the E4 stand-ins on the HQ lot at east + burn-off + 60 / 120 s (D-P4-5); 25:00 west the same way; 40:00 north, then the **two idle HQ turrets** (an edge facing a Held or inert block, or no street) picked up (`pickUp`) and stood on north's front toward a Dark street within turret range, facing its ridge, and fed; 45:00 Generator 4, the fifth Excavator and third Assembler. From minute 10 a rounds run every five minutes: 20 magazines and 50 coal from the chest, every turret and Generator at or under half topped up. Every placement goes through `canPlace` first; what the sim refuses is a refusal with its reason, never forced. With `rifle=1` a reflex aims at the nearest crawler within 9 tiles when there are magazines in the pockets.
- **The log** (`HourLog`): §11's moments as marks (mine-done, craft-done, feed-done, generator-2/3/4, line-excavators, line-assembler, first-line-magazine, first-rounds-run, claim/arrive/held/kitted per direction, turrets-picked-up, turrets-carried, electricians, enclosure, first-crawler, first-turret-fire, first-shade, first-retaliation, first-shot, first-amber, first-red, first-brownout, hq-fell, fell-<dir>), the timed walks, the refusals, a running narrative.
- **The findings** (`hourReport`): every divergence from §11's prose and from the calibration timeline as one line — the 0–10 moments by 3 / 5 / 10 min, the bloom at about 3 min, the line by 8 / 10, Generator 2 at 6; east claimed in 10–30 and Held by 32, the walk over 20–60 s (§11: about 40), Generator 3 at 15, west by 30; north in 30–45, the enclosure (the white border) 35–60, the two turrets carried 38–55, the Electricians in the hour, the first shade 32–47, Generator 4 at 45, the end counts 8 turrets / 4 Generators / 5 Excavators / 3 Assemblers, no brownout; the rework's re-checks (claim walk-overs under a minute in all, walking vs §19's 15 %, the truck outside the hour); the calibration's first amber and red 16–31 min and held 4 at 60; the HQ or a claimed block falling; every refusal. Plus the row of numbers Gate B reads: seconds walked and the share of the hour, the claim walk-overs, chest trips, hand-fed, reach refusals, crawlers / shades / kills, brownout seconds, line magazines.
- **Three commands** (`types.ts`, `engineer.ts`, `flow.ts` hand hook): `feed` (E on a turret or Generator), `repair` (E on a light), `rotate` (R), each reach-checked, so the scene's last direct sim calls have a command form.
- **The command log** (`session.ts` `record`, `Session.log`): every command a session applies, with the tile tick it landed before — the queued ones as `frame` / `runTicks` apply them, the scene's and panel's direct calls (place, pick-up, rotate, craft, mine on / off, feed, repair, chest take / put) logged as the command they stand for after they run. The telemetry export carries it (`commands`) and, under the bot, the hour report (`hour`).
- **The replay** (`hour.ts` `replay`, `replayVerdict`; `session.ts` `replaySession`; `npm run replay -- <export.json> [--rifle-on]`; `__relight.replay()`): a fresh state built the session's way (seed, map, flow and power on), the log re-run one tick a step with the aim commands dropped, and Gate B's answer per hand-fired fight — `held anyway` (its block stood both times), `saved it` (stood only with the rifle), `fell anyway` — the run's verdict the worst of them, the HQ's fate when no shot was fired. `--rifle-on` keeps the aim: the replay must then match the played end state (the determinism check E-hour also runs).
- **`?autoplay=hour`** (+ `?rifle=1`): the session runs the bot in place of the policy bots (M3's `botHands` stays with those); `__relight.hour()` is the report, `__relight.commandLog()` the log.
- **E-hour** (`ehour.ts`): seeds 3 / 4 / 5, rifle off and on, one hour each on the river city with flow and power on; sections: the timeline of every mark in mm:ss, the end state and walking row, Gate B's rifle row (the rifle run replayed rifle-off), every finding; checks: walking ≤ 15 % (worst seed), claim walk-overs ≤ 60 s in all, the HQ stands, the logged hour replays to the same state.
- **Tests** (`hour.test.ts` 7, unwritten-run): minutes 0–6 (mined, crafted, the feed rounds walked, walks timed), minute 10 (three Excavators, two Assemblers, two Generators, every refusal reasoned), minute 45 (east / west / north claimed on the clock and in their directions, walked over and kitted, no step over 2 tiles a tick — no teleport), the report's never-lines, the three commands within reach only, the replay reproducing a logged run (states, machines, shots, hand-fed, the engineer's x) and the rifle-off replay's verdict shape, a placement and feed by hand.

### Assumed (every `GAME-ASSUMPTION` in prompt B M6 code)

| Tag | Where | Assumption |
|---|---|---|
| GA-B6-1 | `hour.ts` `HOUR_*` | the bot's numbers where §11 gives none: 20 steel hand-mined (ten magazines' worth), ten magazines crafted, six kits a claim, a rounds run every five minutes from minute 10 with 20 magazines and 50 coal, machines at or under half topped up |
| GA-B6-2 | `hour.ts` `HOUR_CLAIM_AT`, `HOUR_GEN_AT` | the claims on the calibration's clock (`FIRST_HOUR_CLAIMS` 15 / 25 / 40 min) and the Generators on E4-doc's (0 / 6 / 15 / 45); §11's prose gives windows, not minutes |
| GA-B6-3 | `hour.ts` `neighbourToward`, `claimStep` | "east" is the HQ's Dark candidate neighbour most eastward by lot centres (west, north likewise); the claimed blocks' machines are E4's stand-ins on the HQ lot (D-P4-5), not on the claimed lot |
| GA-B6-4 | `hour.ts` `idleTurret`, `carryTurrets` | §11's "two south-facing turrets on east and west" are the HQ's two idle turrets (an edge facing a Held or inert block, or no street) nearest the Depot; they go on north's front toward a Dark street within turret range |
| GA-B6-5 | `hour.ts` `replay` | the tile sim is frame-independent: a frame's commands all land before its first tick and nothing else touches the state between ticks, so a replay at one tick a step reproduces the played run; the scene's direct calls are logged as the commands they stand for, after they ran |
| GA-B6-6 | `replay.ts` | the replay rebuilds the state as `createSession` does (seed, map, scatter and economy from the export's URL; flow and power on); a snapshot session (scenario B) does not replay |
| GA-B6-7 | `hour.ts` `westThenEast` | the bot feeds every HQ turret in the order west, east, north, rest (six turrets under D-P4-8, not §11's two); "twice each" is two rounds with a chest trip between |
| GA-B6-8 | `hour.ts` `hourCommands` | a walk that ends short of reach (a dropped click, a knock-down) is tried once more, then refused; a wait past its deadline is refused and the step moves on |

The M1–M5 and lattice tags stand; the register below lists them all with what resolves each.

### Deferred

- **The bot's own hour**: every timeline number → the verification pass (`E-hour`); until then the findings list is empty because nothing has run, not because nothing diverges.
- **Copper for repairs in the hour** (M5's deferral): the bot never repairs; the played hour answers it → Gate B.
- **Light on the map view** → Gate B says whether the hour needs it.
- **The claimed lots' own machines**: the bot stands E4's stand-ins on the HQ lot (D-P4-5, GA-B6-3); a bot that walks the copper Excavator onto east's lot is the next bot → after D-P4-5.
- **The bot as a player stand-in for §19**: the bot is a dev aid following §11, not play; §19 is scored by the human → Gate B.
- **A snapshot session's replay** (scenario B) → when a scenario-B hour is played.
- **Frame-time under the hour bot at 4×** → the verification pass's soak (`?autoplay=hour` at `speed 4`).

### Measured (verification pass, 2026-09-04)

The scripted checks: `npm test` 121 / 121 (the seven `hour.test.ts` cases pass after one test fix — the minute-10 case expected two Assemblers and the bot builds one, the Depot being the block-level start Assembler, D-P4-5); `typecheck`, `lint`, `docsync:check` clean; `snapshot:check` (M5's 1 ulp, above); `experiments` 13 runs, 137 s, 0 red, `E-hour` 6 / 6 runs and 6 / 6 checks (the logged hour replays to the same Held set, machine count and shots on every seed; walking 5.21 % worst; claim walk-overs 17 s worst; the HQ stands 6 / 6); `calibrate` byte-identical.

**The timeline** (E-hour, rifle off; rifle on is the same clock to the second except the pips, which the rifle delays — seed 3 first amber 5:38 and red 6:15 rifle on against 3:14 / 6:09 off):

| Moment | §11 / calibration | The bot schedules | Measured (seed 3 / 4 / 5) |
|---|---|---|---|
| to the steel patch, 20 steel mined | 0–10 min ("a few frames' worth") | from 0:00, ≈ 20 s of mining plus the walk | mine-done **0:20** on all three |
| ten magazines crafted at the workbench | 0–10 | ≈ 30 s after the walk back | craft-done **0:50** on all three |
| the turrets fed twice, west and east | 0–10 | two rounds of the HQ's turrets with a chest trip between | feed-done 1:06 / 1:17 / 0:58 — and **the feeds moved nothing**: the six turrets start with full hoppers; the chest's 20 magazines were 10 (5 on seed 4) at the first take and 0 at the second (four refusals a run) |
| the first bloom, turrets fire | ≈ 3:00 (north) | the sim's | first crawler = first turret fire **2:48 / 2:44 / 3:05** |
| Generator 2, coal Excavator belted, steel and copper lines | 6:00 | 6:00 trip, then ≈ 50 belt placements from the pockets | Generator 2 6:03 / 6:02 / 6:03, **placed unfed** (the chest has 0 coal at 6:00, a refusal on every run); the line Excavators 6:13 / 6:13 / 6:14 |
| Shot line, ammo automated | by 10:00 (20 mag/min) | 8:00 | Assembler 8:01, first line magazine **8:06**, first rounds run 10:13 / 10:16 / 10:07 |
| Generator 3 | E4-doc 15:00 | 15:00 | 15:00 on all three |
| east claimed, the walk over | 10–30 min, ≈ 40 s | 15:00; the walk timed | claim 15:00, arrive 15:07 / 15:03 / 15:04 (**7 / 3 / 4 s**), Held and kitted 15:31 / 15:35 / 15:30 (burn-off 30–35 s) |
| west claimed | 10–30 | 25:00 | claim 25:00, Held 25:35 / 25:30 / 25:30 |
| north claimed, the HQ interior | 30–60, border ≈ 40:00 | 40:00 | claim 40:00, Held 40:29 / 40:34 / 40:30; kitted the same second on seeds 3 / 4, **never on seed 5** (the wait timed out after 208 s — one north edge never got its kit); **the HQ never turned interior** (enclosure never, 3 of its 4 street neighbours Held at best) |
| the two turrets carried | 30–60 | after north is Held | picked up 40:36 / 40:43 / 43:45, stood on north's front and fed 40:45 / 40:54 / 43:54 |
| the Electricians in the Depot | 30–60 | when their block turns Held | never (their block was not among the three claims) |
| first shade | 32–47 min | the sim's (none inside the hour at M4) | **42:22 / 40:33 / never** — inside the band on two seeds; 5 / 4 / 0 shades in the hour |
| Generator 4, fifth Excavator, third Assembler | E4-doc 45:00 | 45:00 | **never**: at 45:00 the chest has 0 steel and 27 Cu (80 steel + 30 Cu asked), both placements refused; the 17:34 stand-in Assembler for east was refused the same way (2 steel in the chest) — see D-P4-4 below |
| first amber / first red | calibration 16–31 min | — | amber **3:14 / 2:55 / 3:15**, red **6:09 / 5:33 / 6:14** — the M4 "first red at ≈ 6 min" finding, now on every seed |
| first brownout | §11 none | — | **8:01** on all three, 166 / 166 / 170 s in the hour: Generator 2 unfed from 6:03, the Shot line's draw from 8:01 until the 10:13 rounds run coals it |
| enclosure | calibration 45–60 min | — | never |
| held at 60 | calibration 4 | — | **3** on all three (HQ, east, west): **north fell** at 45:34 / 45:35 / 58:27, rifle off and on alike |
| the end state | §11: 8 turrets, 4 Generators, 5 Excavators, 3 Assemblers | — | turrets 6 / 7 / 5 (the two carried included; seed 5 lost one to the fall), Generators 3, Excavators **6** (three line + three stand-ins, one more than §11's 5), Assemblers 1; line magazines 786 / 793 / 793 |
| walked (§19: ≤ 15 %), chest trips | the rework's bot: 2.6–4.1 % (M4) | the lot's trips are real now | walked 2.8 / 3.1 / 2.8 min = **4.6 / 5.1 / 4.7 %**; chest trips 11 / 10 / 9; hand-fed 718 / 677 / 697 units (magazines and coal); reach refusals 0 |
| claim walk-overs (rework: < 60 s in all) | — | three walks of two street widths and a lot | **16 / 14 / 17 s for all three claims** — the candidate lot's kerb is across one shared street, not two widths and a lot |
| the threat | — | — | crawlers 541 / 429 / 340, turret kills 492 / 351 / 285, rifle kills (rifle on) 11 / 15 / 7 |
| first fire / did it matter | Gate B's row | rifle off: never; rifle on: the reflex's first shot, the replay's verdict | first shot **2:48 / 2:44 / 3:05** (the first crawler's tick), 76 / 79 / 45 rounds, 15 / 13 / 14 hand-fired fights; seeds 3 and 4 **held anyway** (every fight's block stood in both runs); seed 5 **fell anyway** (7 of its fights were on north, which fell at 58:27 rifle on and off) |

**The soak** (`?autoplay=hour&rifle=1&view=world&seed=3`, 4×, headless swiftshader, 902.9 s real, sim 1:30:11): the frame time is in M5's Measured (40.9 fps mean, 61 frames over 50 ms in five bursts, worst 68.3 ms). The browser's hour diverged from the harness's: **held 2 at the hour** (HQ and east), the west claim refused at 25:01 "stock 0 steel, 22 Cu" and north at 40:03 the same, the steel gone by 16:34 (a 10-steel take found 2) where the harness's chest lasted to 26:34; **567 s of brownout** (the coal ran out at 73:53 and the rounds runs found 0 coal after); 20 refusals; 951 units hand-fed, 18 chest trips, 271 s walked = 5.01 %; 112 rounds fired by hand, 19 hand-fired fights all held. Same bot, same seed, a different chest (D-B1-1: the game's §11 chest against the harness's calibrated one, config `d51dfee0` against the harness's) and a different steel curve — the divergence is a finding under D-P4-4, below. The M6 hooks answer: `__relight.hour()` (32 marks, the refusal list, the narrative), `commandLog()` (723 commands of 8 kinds: mineAt, chestTake, craft, move, feed, aim, place, claim), `replay({rifleOff:true})` (a verdict per fight and for the run) and `exportJson()` carries `commands` and `hour`. No page errors.

### Where prompt B M6 and the doc disagree (reported, not resolved)

- **§11 "You walk over (two street widths and a lot, about 40 s)"** — measured 3–7 s on every seed: the claim candidate is a street neighbour and its kerb is across the one shared street. **Edited** to the measurement `[sim: B-M6-hour]`; the "two street widths and a lot" was the lattice's geometry.
- **§11 "40-second burn-off"** for east — measured 30–35 s (20 + 60·d at east's d ≈ 0.2). **Edited** `[sim: B-M6-hour]`; the 20–80 s rule itself stands.
- **§11's machine list cannot be afforded by minute 45** on the block sim's start chest: the bot's chest was at 2 steel by 17:34 (east's stand-in Assembler refused) and 0 by 26:34, and Generator 4, the fifth Excavator and the third Assembler were refused at 45:00 (0 steel, 27 Cu in the chest, 80 + 30 asked). The line's three Excavators and their belts, three Generators, three claims' kits and three stand-ins spend it; the steel Excavator's take is belted into the chest but at one Excavator it does not cover a claim every ten minutes. So §11's end counts (4 Generators, 5 Excavators, 3 Assemblers, 8 turrets) are not reached: 3 / 6 / 1 / 5–7. And the browser, on §11's own 200-steel chest, ran out sooner still and could not claim west or north at all (the soak, above). **Reported, not resolved** — D-P4-4 / D-B1-1 with the evidence appended; the human decides the start stock and the prices from the played hour, as those rows say.
- **The calibration's "first amber / first red 16–31 min"** is contradicted on every seed (amber ≈ 3 min, red ≈ 6): the block-level hoppers of the calibration never emptied before the line came up; the tile-level HQ's six turrets fire from the first crawler at 2:48 and the chest's magazines are gone by 1:00. M4 saw it once (seed 3); it is now the rule. The bands are `docs/experiments/lattice/calibration.md`'s and stay as the lattice's record; no §11 number carries them. Reported.
- **"Held 4 at 60" and the enclosure 45–60** are not reached: north falls at 45–58 min on every seed (the hour's third claim is the one the rounds runs cannot keep fed once the chest's coal and steel are spent; seed 5's north never got its last kit), the HQ never turns interior, the Electricians' block is never claimed. Reported under D-P4-9 (a claimed block has no turrets of its own to hold north with) and D-P4-7 (the brownout).
- **§11 "one Generator alone browns out at minute 8"** — confirmed by accident: the brownout comes at 8:01 on every seed because Generator 2 is placed with no coal in the chest (the 40 start coal are in Generator 1's hopper, not the chest) and the Shot line's draw arrives at 8:01; it lasts until the first rounds run at 10:13. §11's sentence is about a player who never builds the second Generator; the bot's is about a player who builds it unfed. No edit; D-P4-7's recommendation carries it.
- **§11's first shade at 32–47 min** — 42:22 / 40:33 on seeds 3 / 4, never on seed 5 (0 shades in its hour). Inside the band on two of three; no tag on a band the third seed misses.
- **The chest's magazines**: "20 magazines in the chest" (§11) were 10 (seed 3, 5), 5 (seed 4) at the first take (the six start turrets' hoppers were filled from the chest, D-P4-8) and 0 at the second, on every run; and the turrets took none of them — their hoppers were full. §11's "walk the magazines to the turrets" in 0–10 min is a walk with nothing to carry. Reported under D-P4-8.
- **Six Excavators against §11's five**: the bot stands the claimed lots' Excavators on the HQ lot (D-P4-5, GA-B6-3) and the HQ line has three of its own (coal, steel, copper): 3 + 3. §11 counts the east lot's copper Excavator and the west lot's coal as its fourth and fifth. Settles with D-P4-5.
- What the code knew before the run and the run confirmed: the claims on the calibration's minutes rather than "10–30 / 30–60" (GA-B6-2 — the bot was never idle waiting for the clock; each claim's walk and kit took 30–35 s and the burn-off the rest); six start turrets fed, not two (D-P4-8, GA-B6-7).
- **The prompt's `docs/experiments/calibration.md`** is at `docs/experiments/lattice/calibration.md` since the rework's move; the bands quoted are the same (first amber / red 16–31, enclosure 45–60, held 4 / 16 / 25–28).
- **§11's "two south-facing turrets on east and west"**: the HQ has no turret on a claimed block to carry (D-P4-9 keeps claimed blocks on the block-level hopper), so the bot carries the HQ's own idle pair (GA-B6-4). The sentence stands until D-P4-9 is settled.
- **§19's walking share** is a player's estimate; the bot's number is beside it, not in its place (as at M4).

### Decisions for the human (recommended in `DECISIONS.md` D-B6-1–D-B6-3)

- **D-B6-1 the hands in the hour**: the bot (and the player) feed turrets and Generators from the pockets with real trips to the chest, M3's `botHands` Depot-feed kept for the policy bots only (a) / `botHands` for the hour bot too, so the hour measures placement and claims without the feed trips (b) / no bot hands anywhere, the line or nothing (c). Recommend (a): Gate B's walking row needs the trips.
- **D-B6-2 the claim minutes**: the calibration's 15 / 25 / 40 (a) / §11's prose read as 10 / 20 / 30 — the earliest of each window (b) / the bot claims when the line has 60 magazines banked, whatever the minute (c). Recommend (a) until E-hour's first run says the bot is idle waiting for the clock.
- **D-B6-3 the replay's ground**: the command log with the aim dropped, judged per hand-fired fight and the HQ (a) / judged on the HQ alone (b) / the fight's edge only, ignoring the block's later fate (c). Recommend (a).

## Economy fix, the layout pass and E-rifle at tile scale — built 2026-09-04 (E-hour and E-rifle re-run; cheap checks green; not the full verification pass)

Run names `B-M6-hour` (E-hour re-run: seeds 3 / 4 / 5 × rifle off / on, the D-P4-7 pair, and `E-hour-north` to 75:00), `B-M6-hour-north`, `B-M6-light` (the light tests at radius 7), `E-rifle-tile` (the steady hour and the rescue at tile scale, `docs/experiments/E-rifle.json`). ROADMAP §0 lines 1–4. The human's decisions came in the chat message of 2026-09-04 ("Gate B is not scored on this hour. M6 shows it loses on every seed and can't afford its own script. Fix, re-run, then I play. D-P4-4 / D-B1-1 (decided): Mk1 Shot assembler recipe 6 s = 10 mag/min … D-P4-7 … D-P4-8 (decided): six start turrets, hoppers full, plus 20 magazines in the chest … Streetlights (new row, D-B5-4): radius to the street midline (~7 tiles) … E-hour pass condition: §11's end state reached on seeds 3/4/5 with no block falling and steel never at zero — or §11 rewritten to what the economy affords (two claims in the hour, north at 60–75) with the same no-fall condition. Report which."); `DECISIONS.md` carries each row's `decided by / on / via`.

**The pass condition, reported:** §11 is **rewritten** to what the economy affords. On the Mk1 line the old script (three claims, north at 40) cannot hold north on any seed; the rewritten hour — east at 15, west at 25, Generator 4 at 45, the second copper Excavator at 46, the third Assembler at 50, north at 60–75 — reaches its end state (4 Generators, 7 Excavators, 3 Assemblers, 6 turrets, 3 Held) on seeds 3 / 4 / 5, rifle off and on, with **no block falling, no brownout and steel never below 30**. North claimed at 65:00 is Held in 30 s and falls at 72:45–74:38 on every seed because the Generators run dry at 67:51–68:01 (the HQ's coal patch is dug out at ~36:00 and west's coal is a stand-in that makes nothing): north is a coal problem, D-P4-10.

### Taken provisionally (rule 14)

- **GA-EF-1 — the second steel Excavator at 12:00, not the message's "~15:00".** The steel curve's minimum (30) is at 12:01 on every seed and a 15:00 placement crosses east's claim and kit. Reversing it moves no rule and no fixture; the row is D-P4-4's status.

### Built

- **Sim** — `recipes.ts`: Shot magazine Mk1 6 s / 10 mag/min (`SHOT`), a Mk2 row at 3 s / 20 mag/min (the purchase; unlock open, D-B1-1); `STREETLIGHT_RADIUS` 7 (the Lamp keeps 4). `flow.ts`: `START_TURRETS` 6 through D-B1-4's segment rule with every hopper at 50; `START_CHEST` 200 steel / 100 copper / 50 stone / **40 coal** / 20 magazines (`START_CHEST_COAL`, D-P4-7 (b)); the chest's coal counted in `flowSummary`. `hour.ts`: the second steel Excavator into the chest at 12:00 (`HOUR_STEEL2_AT`, `steelToChest`), the second copper Excavator at 46:00 (`HOUR_COPPER2_AT`), the third Assembler at 50:00 (`HOUR_ASM3_AT`), claims east 15 / west 25 / north 65 (`HOUR_CLAIM_AT`), `HOUR_END` 4 / 7 / 3 / 6 turrets / 3 Held, `coalPlan` 'wait' | 'chest' for D-P4-7's pair, the fall's reason captured (`fellWhy`), `putNear` spiral placement, `handsOff` + `rescueStance` for E-rifle's tile rescue. `light.ts` comment follows the radius.
- **Harness** — `ehour.ts`: the D-P4-7 pair (`E-hour-coal`), `E-hour-north` (claim at 65:00, the run carried to 75:00), the stock table with the steel curve's minimum, the timeline's first amber / red / hand-feed, `northAt65` in the data. `erifle.ts`: `E-rifle-tile-steady` (the hour bot rifle off / on) and `E-rifle-tile-rescue` (`tileRescue`: the most threatened Held block's edge or whole ring dry, the belt 90 s or 600 s away, the engineer at the segment's midpoint with 20 magazines; five tile checks).
- **Game (the layout pass, ROADMAP §0 line 1; STANDARDS 4.3, B.3, B.6, C.2)** — `main.ts`: `Scale.RESIZE`, the world view fills the window (`style.css`: `#map` flex, `#panel` 420 px, scrolling). `worldScene.ts`: the HUD top-left (view, block, zoom, tiles, pockets, in hand, power) and a **key strip** bottom-left (move, zoom range, `P` pause, `-` / `=` speed, the tool keys), both pinned under zoom; **kerb pips** (`drawKerbPips`: each segment's pip colour on its kerb tiles); the **Depot** with a 3-screen-px outline, a **fill bar** (line buffer / `bufferCap`, green → amber → red, blinking at 0) and the label `Depot · n / cap mag`; a **beacon** at the viewport's edge with the tile distance when the Depot is off-screen. `panel.ts`: the build menu says "Shot assembler Mk1, 6 s a magazine (10/min); the Mk2 (3 s) is the purchase", the turret's range and hopper, the Lamp's radius; the survivor list's first row from minute one is **"Blueprints and copy-paste · a survivor's gift (Phase 7) — not yet found"** (STANDARDS dealbreaker 1, Phase 7's row).
- **Tests** — `defence.test.ts` (six full turrets, 40 + 40 coal, 20 magazines, every pip green from the first tick; the city-HQ apportion), `flow.test.ts` (the line at 60 / `SHOT.seconds` = 10 / min; the craft in 6 s), `light.test.ts` (kerb > 0.5, half-street > 0.4, lot < 0.6, Dark ≤ 0.25 at radius 7), `hour.test.ts` (the first hand-feed ≥ 4 min; no north claim by 45).
- **Doc** — §11 rewritten in all three windows (0–10 around the ~6-minute red pip as the hand-feed beat; 10–30 east 15 / west 25; 30–60 the fourth Generator, copper 2, Assembler 3, north at 60–75 and the tile-scale rifle), §12's table (docsync, the Mk1 and Mk2 rows) and ammo chain, §13's Assembler row and fixtures paragraph (radius 7); six changelog lines.

### Assumed (every `GAME-ASSUMPTION` in the economy-fix code)

- **GA-EF-1** (`hour.ts`) — the second steel Excavator at 12:00 rather than ~15:00 (above; provisional).
- **GA-EF-2** (`worldScene.ts`) — the Depot's fill bar is the block sim's line buffer over `bufferCap` (not the chest's magazines), green above half, amber below it, red with a blinking outline at 0; the beacon is the Depot's glyph and tile distance at the viewport's edge; drawing only — §19's "visible six blocks out" is Phase 12 art.
- **GA-EF-3** (`panel.ts`) — the blueprints row is a fixed line of text, not a survivor the sim knows; Phase 7 replaces it.
- **GA-EF-4** (`erifle.ts`) — the dry edge is made by cutting the belt (`e.cut`) and emptying the hoppers for 90 s or 600 s; the block sim has no tile-level belt to cut, so the cut is the stand-in.
- Carried, still true: GA-B6-1 … GA-B6-7 (the hour bot's hands, claim clock, stand-ins, kits, replay), GA-B6-3 in particular — west's coal is a stand-in that makes nothing, which is why north is a coal problem.

### Deferred

- West's real coal (an Excavator and belt on west's rubble at the claim) → D-P4-10's decision, after Gate B.
- A second Shot line or the Mk2 purchase from the idle steel → D-P4-11, Gate B's tedium row.
- The `E-hour-north` run stops at 75:00; the human's north at 60–75 with real coal is unmeasured until D-P4-10 is built.

### Measured (E-hour and E-rifle re-run 2026-09-04; not the full verification pass — the pass re-runs these plus the soak)

**E-hour, seeds 3 / 4 / 5, rifle off and on (10 / 10 checks).**

| moment | seed 3 | seed 4 | seed 5 |
|---|---|---|---|
| first crawler / turret fire | 2:48 | 2:44 | 3:05 |
| first amber · first red | 3:14 · 6:09 | 2:55 · 5:33 | 3:18 · 6:18 |
| first hand-feed | 6:11 | 5:36 | 6:21 |
| Generator 2 | 6:01 | 6:02 | 6:01 |
| line Excavators · Assembler · first line magazine | 6:14 · 8:01 · 8:08 | 6:14 · 8:01 · 8:07 | 6:11 · 8:03 · 8:10 |
| second steel Excavator | 12:01 | 12:02 | 12:01 |
| east claimed · Held | 15:03 · 15:34 | 15:01–15:03 · 15:37–15:38 | 15:01 · 15:31 |
| west claimed · Held | 25:00 · 25:35 | 25:00 · 25:30 | 25:00 · 25:30 |
| Generator 4 · copper 2 | 45:00 · 46:01 | 45:00 · 46:03 | 45:00 · 46:02 |
| first shot (rifle on) | 14:02 | 5:37 | 6:26 |
| brownout · fall · steel zero | never | never | never |

| end state | seed 3 | seed 4 | seed 5 |
|---|---|---|---|
| Held / HQ / turrets / Generators / Excavators / Assemblers | 3 / yes / 6 / 4 / 7 / 3 | same | same |
| line magazines · hand-fed (off / on) | 519 · 864 / 861 | 519 · 822 / 819 | 519 · 792 / 788 |
| walked (min, %) · claim walk-overs (s) · chest trips | 2.2, 3.7 % · 11 · 9–11 | 2.2–2.3, 3.7–3.8 % · 8–9 · 13–14 | 1.3–1.4, 2.2–2.4 % · 13 · 3–4 |
| crawlers · turret kills · rifle kills (on) | 463 · 463 · 0 | 329 · 322–328 · 6 | 228 · 226–228 · 2 |
| steel min · copper min · coal min | 30 @ 12:01 · 17 · 0 | 30 @ 12:02 · 17 · 0 | 30 @ 12:01 · 17 · 0 |
| chest at 60:00 (St / Cu / coal / mag) | 1144 / 265 / 2 / 45 | 1144 / 264 / 2 / 127 | 1145 / 264 / 2 / 51 |
| Gate B rifle row (replay) | 7 fights, held anyway | 7 fights, held anyway | 3 fights, held anyway |

The steel curve: 200 → 42 at 10:00 → **30 at 12:01** → 118 at 15:00 → 1,144 at 60:00 (every seed within 1). The coal column is the Generators' clock: 40 → 402 at 30:00 (the patch) → 52 at 55:00 → 2 at 60:00.

**D-P4-7 (E-hour-coal, rifle off):** (a) coal Excavator first, chest coal 0 — Generator 2 at 7:32 / 7:24 / 7:26; (b) 40 coal in the chest — 6:01 / 6:02 / 6:01. Both: no brownout, coal min 0, 0 coal refusals. **(b) shipped.**

**E-hour-north (north claimed at 65:00, to 75:00, rifle off):**

| seed | Held north | turrets carried | Generators dry | brownout | north fell | why | chest at 75 (St / Cu / coal / mag) |
|---|---|---|---|---|---|---|---|
| 3 | 65:29 | 68:51 | 68:00 | 65:44, 557 s | 72:47 | shade | 1520 / 397 / 0 / 0 |
| 4 | 65:34 | 68:59 | 68:01 | 65:45, 556 s | 72:45 | shade | 1524 / 397 / 0 / 99 |
| 5 | 65:29 | 68:56 | 67:51 | 66:31, 510 s | 74:38 | unfed | 1524 / 397 / 0 / 20 |

**E-rifle at tile scale (12 / 12 checks with the lattice rows).** Steady: line magazines 519 / 519 on every seed (0.00 % diff), hand-fed 864 → 861, 822 → 819, 792 → 788, falls 0 / 0, rifle rounds 22 / 26 / 9, rifle kills 0 / 6 / 2, turret kills 463 / 322 / 226, first shot 14:02 / 5:37 / 6:26, HP lost 0, downs 0.

| belt | scope | without the rifle | with the rifle | kills | HP lost (min HP) |
|---|---|---|---|---|---|
| 90 s | edge, 6 runs | holds | holds | 0–8 | 0–40 (60) |
| 90 s | ring, 6 runs | holds | holds | 0–8 | 0–45 (55) |
| 600 s | edge, 6 runs | seed 5 at 20:00 falls at +7.2 min (unfed); 5 hold | **all hold** | 18–32 | 22–120 (34) |
| 600 s | ring, 6 runs | all fall at +4.3 … +8.9 min (unfed) | seed 4 holds both; seed 3 +5.7 / +9.0, seed 5 +4.4 / +7.7 (delayed 0.1 / 2.8 / 0.1 / 2.1 min) | 15–23 | 29–80 (55) |

Never knocked down; mean damage over the lattice rescues 62 HP.

**Light at radius 7 (`B-M6-light`, seeds 3 / 4 / 5):** kerb row 65–93 % lit (radius 4: 50–68 %), half-street 44–65 % (26–41 %), lot 44–50 %, a Dark lot ≤ 25 %.

**The layout pass:** the game builds (`npm run typecheck` includes it) and the snapshot is unchanged (config `68d07000`); no human has looked at it — its measure is the four one-minute STANDARDS checks (4.3, B.3, B.6, C.2) in the controls walkthrough (§0 line 5).

**The verification pass should measure:** E-hour and E-rifle as above on a clean run, the 900 s soak with the resized world view (the HUD and key strip pinned at every zoom, the Depot beacon at 0.5×, the fill bar), the full experiments list (EXPERIMENTS.md carries the tile sections), calibration unchanged.

### Where the economy fix and the doc disagree (reported, not resolved)

- **§11's 30–60 still lists the Electricians, the HQ's white border and the first shade at 32–47** — none of them happens in the two-claim hour (E-hour findings, every seed); the paragraph now says so beside them, the numbers stand as the lattice's.
- **§11 "walk over … 2–8 s"** — the six walks were 8 / 8 / 3 / 2 / 13 / 13 s; seed 5's 13 s is west (a longer street), the doc's line is east's; the harness finding now compares with 2–8 s.
- **§12 C9 / D-P3-7 "no Mk1 / Mk2 ladder"** — reversed by D-P4-4 / D-B1-1 (decided); the paragraph says so. Calibration untouched (the block-level `asmRate` is the Mk2's 20).
- **§13 "3 in 8 broken" vs §7 "broken 20 % of the time"** — pre-existing, untouched.
- **§5.8 "two turrets an edge"** — the HQ has six on three segments (D-B1-4); a claim still lays none (D-P4-9).

**Rebased onto the guardrails (Steps 4–6, `GUARDRAILS_REPORT.md`, 2026-09-04).** The fix now reads every tempo number from `packages/sim/src/constants.ts` (`SHOT_MAGAZINE` 6 s and `SHOT_MAGAZINE_MK2_SECONDS` 3, `ASSEMBLER_TIERS` Mk1 10 / Mk2 20 with `ASSEMBLER_MK1_MAG_PER_MIN` and `ASSEMBLER_MAG_PER_MIN` = the Mk2, the block sim's `asmRate`, so no fixture and no config hash moved; `START_TURRETS` 6, `STREETLIGHT_RADIUS` 7, `START_CHEST` with coal 40, `HOUR_CLAIM_MIN` 15 / 25 / 65, `HOUR_STEEL2_MIN` 12, `HOUR_COPPER2_MIN` 46, `HOUR_ASM3_MIN` 50, an eleven-line `HOUR_MINUTES`); `recipes.ts`, `flow.ts` and `hour.ts` only re-export. Of `GUARDRAILS_REPORT.md` §5's eight recorded disagreements, **items 1 (assembler rate) and 8 (§11 windows) are resolved by this fix** — the doc, the calibration's start "Mk1" and the tile recipe all say 10 mag/min for the Mk1 and 20 for the Mk2, and §11 names the claim minutes — and `docsync:check` now compares the Mk1 with `PROTO_CALIBRATED.startAsmRate` and the Mk2 with `asmRate`, and reads §11's "about minute 15 / 25" and "north at 60–75". **Items 2–7 remain and `docsync:check` exits 1 on them by design** (six lines: the calibration's 100-round edge hopper, the calibration's 80 / 40 / 0 chest, the calibration's and `FIRST_HOUR_DEFAULTS`' 200 / 40 kW draw, `STREETLIGHT_STEP` 3, `FIRST_HOUR_DEFAULTS.gens` 0 / 30); each is a human row, not a build fix, so under the constitution's "report, do not resolve" they are reported here, and CLAUDE.md rule 13's "if any is red, fix it before writing the report" is read as applying to checks the build can turn green without settling a constant. `firsthour.ts`'s north claim at 40 (`FIRST_HOUR_CLAIMS`) is a seventh line that only prints when it disagrees with `HOUR_CLAIM_MIN.north` (65); it does, so the check lists it with item 8's text — the calibration's E4 hour is the doc-literal one and is left as the human's. Stamps: `EXPERIMENTS.md` and `docs/experiments/*.json` were regenerated by the full run on the rebased tree; their `source_commit` is the parent (`9fa9739`, the tree at generation time is the fix's), the limit `freshness.ts` names.

### Decisions for the human (recommended in `DECISIONS.md`)

- **D-P4-10 coal after the patch, north's minute**: west's coal made real at the claim (a) / a 1,000+ HQ patch (b) / a delivering stand-in (c). Recommend (a).
- **D-P4-11 the idle steel**: a second Shot line at ~30:00 (a) / the Mk2 purchase priced from it (b) / leave it until the played hour says whether the hand-feed is the beat or tedium (c). Recommend (c), then (a).
- **D-P4-9 (open)**: north's two carried turrets did not hold it at 40 and will not at 65 without coal; a claim that lays its own turrets and its coal Excavator from stock is one row now (with D-P4-10).

### Rule 12 — the message's numbers that are in neither the doc nor a decided row (now recorded)

Mk1 6 s = 10 mag/min and Mk2 3 s (were C9's "no ladder"); the second steel Excavator at ~15:00 (placed 12:00, GA-EF-1); 40 coal in the chest (D-P4-7 (b)); six turrets with full hoppers plus 20 magazines (D-P4-8; §11 said two on the north edge); the streetlight radius ~7 (D-B5-4; the code had 4); two claims and north at 60–75 (the fallback pass condition, now §11). ROADMAP §0 line 3 said "HUMAN FIRST (write rows … in DECISIONS.md)": the rows came by chat message and are recorded with that message as `via`.

## M6 re-run after the economy fix — **passes**, measured 2026-09-05 (`docs/PROGRESS.md` T2; run name `B-M6-hour-3`)

**No-fall hour: yes.** On seeds 3, 4 and 5, rifle off and on — six runs — **no block falls, the chest's steel is never zero, and there is not one brownout second**. E-hour is **10 / 10**. Nothing was changed to make it pass here: the hour passes on the three rows the human decided on 2026-09-05 (D-HOUR-2, D-P4-10, D-P4-9 — `ECONOMY_FIX_REPORT.md` §9, the section "The blockers" above), and this task re-ran it and wrote the numbers down. Measured, not unverified: `npm run experiments` is 13 experiments, 231 s, **0 failing checks**, and E-hour alone was re-run on the committed tree to confirm it: **10 / 10 in 43.9 s, and the only byte that moved in `docs/experiments/E-hour.json` was the `source_commit` stamp** — every measured number is identical, which is the determinism the three replay checks assert, shown across two separate invocations.

Every number below is `[sim: B-M6-hour-3]` (`docs/experiments/E-hour.json`, seeds 3 / 4 / 5, rifle off and on; the rifle changes only the marks given in brackets). §11's existing `[sim: B-M6-hour]` tags were not renamed — the tag names the experiment, and these are its current numbers.

### The hour, seeds 3 / 4 / 5

| moment | seed 3 | seed 4 | seed 5 |
|---|---|---|---|
| mine done · craft done | 0:20 · 1:20 | 0:20 · 1:20 | 0:20 · 1:20 |
| first crawler (turrets fire) | 2:48 | 2:44 | 3:05 |
| first amber pip · first red pip · hand-feed | 3:14 · 6:09 · 6:11 | 2:55 · 5:33 · 5:36 | 3:18 · 6:18 · 6:21 |
| Generator 2 built · coal Excavator's line laid | 6:01 · 6:14 | 6:02 · 6:14 | 6:01 · 6:11 |
| Shot line (Assembler at 8) · first line magazine | 8:01 · 8:08 | 8:01 · 8:07 | 8:03 · 8:10 |
| the chest's start rounds run out | 10:02 | 10:03 (on: 10:04) | 10:04 |
| **second steel Excavator into the chest (D-HOUR-2: 12)** | **12:01** | **12:02** | **12:01** |
| Generator 3 (15) | 15:03 | 15:03 (on: 15:01) | 15:01 |
| east claimed · walked over · Held · kitted | 15:03 · 15:11 · 15:34 · 15:34 | 15:03 · 15:06 · 15:38 (on: 15:01 · 15:04 · 15:36) | 15:01 · 15:07 · 15:31 · 15:31 |
| west claimed · Held · kitted | 25:00 · 25:35 · 25:35 | 25:00 · 25:30 · 25:30 | 25:00 · 25:30 · 25:30 |
| Generator 4 (45) · copper Excavator 2 (46) | 45:00 · 46:02 | 45:01 (on: 45:00) · 46:03 | 45:00 · 46:01 |
| **north claimed (D-P4-10: 65)** | **never — past the hour** | **never** | **never** |
| **first brownout · seconds** | **never · 0** | **never · 0** | **never · 0** |
| **steel zero** | **never** | **never** | **never** |
| **any block fell** | **no** | **no** | **no** |
| first shade (§11 / E3: 32–47) | never | never | never |
| first shot (rifle on) | 14:02 | 5:37 | 6:22 |
| blocks Held at 60 (HQ, east, west) | 3 | 3 | 3 |
| time walked (off / on) | 3.7 % / 3.8 % | 3.8 % / 3.6 % | 2.2 % / 2.2 % |
| claim walk-overs (s) | 14 | 9 | 11 |
| chest trips · magazines hand-fed (off) | 9 · 858 | 14 · 821 | 4 · 792 |
| crawlers · killed by turrets · by the rifle (on) | 463 · 438 · 1 | 329 · 325 · 3 | 228 · 227 · 1 |
| reach refusals · claim refusals | 0 · 0 | 0 · 0 | 0 · 0 |
| Gate B replay verdict (fights) | held anyway (7) | held anyway (7) | held anyway (1) |

End state, all six runs: **3 Held with the HQ standing, 6 turrets, 4 Generators, 7 Excavators, 3 Assemblers, 519 line magazines.** §11's end state is a two-claim hour (`HOUR_END.held` 3, D-P4-10), and every run reaches it.

### The chest

**Steel** 200 → 200 (5:00) → 42 (10:00) → **the minimum, 30 at 12:01–12:02**, when the second steel Excavator is paid for → 118 (15:00) → 338 (25:00) → 778 (40:00) → 1,144 (55:00). The minute-15 cluster that emptied the chest in the 2026-09-04 attempt is gone: with the Excavator at 12 the chest is 76 steel ahead by 15:00 and Generator 3 and east's six kits are paid out of that.

**Copper** 100 → 27 from 20:00 (the minimum is **17**) → 134 at 50:00 → 264 at 55:00, the second copper Excavator at 46 turning it.

**Coal** 40 → 0 at 6:01–6:02 (Generator 2's take — the coal minimum, and the only zero in the hour) → 118 (10:00) → the HQ patch's peak 448–450 at 30:00 → 298 (40:00) → 98 (50:00) → 48 (55:00) → **out for good at 56:00, and 0 at the hour on every seed**. §11 said 2; the doc was corrected.

### After the hour — `E-hour-north` (measured, not scored)

The same hour carried to 75:00 with north claimed at `constants.HOUR`'s 65:00, rifle off:

| | seed 3 | seed 4 | seed 5 |
|---|---|---|---|
| north claimed · Held | 65:00 · 65:29 | 65:00 · 65:34 | 65:00 · 65:29 |
| north fell | **never** | **never** | **never** |
| falls · Held at 75:00 | 0 · **4** | 0 · **4** | 0 · **4** |
| Generators dry · first brownout · seconds | 67:59 · 65:44 · 557 | 68:01 · 65:45 · 556 | 67:51 · 66:31 · 510 |
| chest at 75:00 (St / Cu / coal / mag) | 1520 / 397 / 0 / 69 | 1524 / 397 / 0 / 220 | 1524 / 397 / 0 / 153 |
| verdict | stands | stands | stands |

In the 2026-09-04 attempt this variant fell at 72:47 / 72:45 / 74:38 on the three seeds. **D-P4-9 is what changed it**: with §11's two carried turrets removed, north's edges keep the block sim's ring-fed 100-round hopper instead of a hand-fed 50, and one engineer no longer has three physical turrets to keep alive across a 510–557 s brownout. North is still a coal problem — the Generators run dry at 67:51–68:01 and the chest's coal is 0 — which is what **D-P4-12** exists to fix.

### The two M6 checks

| check | seed 3 | seed 4 | seed 5 |
|---|---|---|---|
| (a) west's coal at a Generator | never (GA-B6-3: the stand-in makes nothing) | never | never |
| (a) chest coal out for good · Generators dry · margin | 56:00 · never in the hour · **−4.0 min** | 56:00 · never · **−4.0 min** | 56:00 · never · **−4.0 min** |
| (b) first red pip · crawlers spawned by then · arrivals at a Held edge by then | 6:09 · 60 · 0 | 5:33 · 44 · 0 | 6:18 · 44 · 0 |

(a) is two minutes better than the 2026-09-04 attempt's −6.0 (the coal Excavator's line is unchanged; the hour simply ends with more of the patch behind it), and it is still negative: the chest's coal runs out inside the hour and west's never comes. That is a Phase 5 number behind **D-P4-12**, not a constant to move. (b) is unchanged: by the first red pip 44–60 crawlers have spawned and **none** has reached a Held edge — the turrets kill them on the street, so the ~6-minute red is made of spawns, not arrivals, and the HQ's arrival count is 0 against the 40-arrival rule.

### E-hour's ten checks

All green: three per-seed replay-determinism checks (the logged hour replays to the same Held set, machine count and shots, aim kept), the chest's steel never zero (worst run 30), no block falls (0 across 6 runs), no brownout (worst 0 s), §11's end state on every run (4 Generators, 7 Excavators, 3 Assemblers, 3 Held — 6 / 6), §19's walking at most 15 % (worst 3.79 %), the claim walk-overs under a minute (worst 14.00 s), and the HQ standing at the hour (6 / 6).

### Where it still disagrees with the doc

**One finding, on all six runs, unchanged and already reported: the first shade never happens.** §11 and E3 put it at minute 32–47. It survived the economy fix, the decided minutes and now this hour, and it is the only line E-hour prints. In the two-claim hour the bot never lets a block sleep past d = 0.3 — the shade is a lesson hour one no longer teaches.

### The 2026-09-04 attempt, kept as the record (run name `B-M6-hour-2`)

**No-fall hour: no.** North, claimed at constants.HOUR's minute 40 (D-HOUR-1), falls on every seed inside the hour, and the chest's steel reaches zero at 15:02 on every seed. Nothing was changed to make it pass (the task's rule); the human chooses (ROADMAP §0 line 3, D-P4-10). Every number below is `[sim: B-M6-hour-2]` (`docs/experiments/E-hour.json`, stamp `ab8d80b` / `b95922d2`, seeds 3 / 4 / 5, rifle off and on; the rifle changes only the marks given in brackets). E-hour is red: 4 of its 10 checks fail (steel never zero, no fall, no brownout, end state).

| moment | seed 3 | seed 4 | seed 5 |
|---|---|---|---|
| mine done · craft done | 0:20 · 1:20 | 0:20 · 1:20 | 0:20 · 1:20 |
| first crawler (turrets fire) | 2:48 | 2:44 | 3:05 |
| first empty hopper (first red pip) · hand-feed | 6:09 · 6:11 | 5:33 · 5:36 | 6:18 · 6:21 |
| Generator 2 built, the chest's coal in it; coal Excavator's line laid | 6:01 · 6:14 | 6:02 · 6:14 | 6:01 · 6:11 |
| Shot line (Assembler at 8) · first line magazine | 8:01 · 8:08 | 8:01 · 8:07 | 8:03 · 8:10 |
| second steel Excavator (D-P4-7: 15) · Generator 3 | 15:00 · 15:03 | 15:03 (on: 15:02) · 15:05 (on: 15:03) | 15:01 · 15:02 |
| east claimed · Held | 15:03 · 15:34 | 15:05 · 15:40 (on: 15:03 · 15:38) | 15:02 · 15:32 |
| west claimed · Held | 25:00 · 25:35 | 25:00 · 25:30 | 25:00 · 25:30 |
| north claimed · Held (D-HOUR-1: 40) | 40:00 · 40:29 | 40:01 · 40:34 | 40:00 · 40:30 |
| the two turrets picked up · carried to north | 43:41 · 43:51 (on: 43:39 · 43:48) | 43:47 · 43:58 | 43:46 · 43:55 |
| first shade | 44:41 | 44:45 | never |
| Generator 4 (45) · copper Excavator 2 (46) | 45:00 · 46:01 | 45:00 · 46:01 | 45:00 · 46:02 |
| first brownout · seconds | 40:01 · 299 | 40:02 · 299 | 40:01 · 300 |
| **steel zero** | **15:02** | **15:04** (on: 15:03) | **15:02** |
| **north fell (why)** | **52:43 (shade)** | **48:54 (unfed)** (on: 49:05) | **49:55 (unfed)** (on: 50:02) |
| chest at the fall (St / Cu / coal / mag) | 962 / 205 / 2 / 19 (53:00) | 888 / 105 / 2 / 79 (49:00) | 918 / 134 / 2 / 36 (50:00) |
| time walked (off / on) | 5.0 % / 4.8 % | 5.0 % / 4.6 % | 3.9 % / 3.9 % |
| blocks Held at 60 (HQ, east, west) | 3 | 3 | 3 |
| lowest chest steel · minute | 0 · 15:02 | 0 · 15:04 | 0 · 15:02 |
| first shot (rifle on) · Gate B replay verdict | 14:02 · fell anyway (11 fights) | 5:37 · fell anyway (14 fights) | 6:26 · fell anyway (5 fights) |

The steel curve: 200 → 42 at 10:00 and 15:00 → **0 at 15:02** → 29 / 27 / 29 at 16:00 → 1,127–1,151 at 60:00. The three steps constants.HOUR puts at minute 15 (the second steel Excavator and its two belts, Generator 3, east's six kits) draw the chest's 42 steel together; with the Excavator at 12 (the withdrawn GA-EF-1) the minimum was 30 at 12:01. The coal column: 40 → 402 at 30:00 (the HQ patch) → 2 at 50:00 → out for good at 54:00 on every seed; the Generators run on their hoppers to the hour. North at 40 adds a fourth block's draw before Generator 4: the brownout runs 40:01 → 45:00 (299–300 s) on every seed, and north falls at 48:54–52:43 (unfed on seeds 4 / 5, a shade on seed 3) with 2 coal and 19–79 magazines in the chest.

**The two-claim variant (E-hour-north: east 15, west 25, north moved to 65:00, run to 75:00, rifle off).** Inside the 60 minutes no block falls and there is no brownout (the first is 66:16 / 66:20 / 67:06), but the chest's steel still reaches zero at 15:02 / 15:04 / 15:02 (the same minute-15 cluster), so **the two-claim variant is not a no-fall hour by the task's definition either** (no fall, yes; steel never zero, no). After the hour: north Held 65:29 / 65:34 / 65:29, the turrets carried 68:51 / 68:59 / 68:56, the Generators dry 68:02 / 68:06 / 67:57, brownout 525 / 521 / 475 s, north fell 72:47 (shade) / 72:45 (shade) / 74:38 (unfed); chest at 75: 1435–1439 / 397 / 0 / 0–100; 3 Held at 75. The design doc was not edited (the human chooses).

**The two M6 checks never run before (E-hour-m6checks, rifle off):**

| check | seed 3 | seed 4 | seed 5 |
|---|---|---|---|
| (a) west's coal at a Generator | never (GA-B6-3: the stand-in makes nothing) | never | never |
| (a) chest coal out for good · Generators dry · margin | 54:00 · not in the hour · **−6.0 min** | 54:00 · not in the hour · **−6.0 min** | 54:00 · not in the hour · **−6.0 min** |
| (b) first red pip · crawlers spawned by then · arrivals at a Held edge by then | 6:09 · 60 · 0 | 5:33 · 44 · 0 | 6:18 · 44 · 0 |

(a) The chest's coal runs out 6 minutes before the hour's end on every seed and west's coal never comes (through Gate B the west-coal stand-in makes nothing, GA-B6-3): the margin is negative, and it is a Phase 5 number (west's coal real) rather than a constant's. (b) By the first red pip the threat has spawned 44–60 crawlers and none has arrived at a Held edge (the turrets kill them on the street): the HQ's arrival count at ~6 minutes is 0, far below 40; the spawn count is the number the ~6-minute red is made of.

**Unchanged from the previous run (`B-M6-hour`):** the 0–10 script (mine 0:20, craft 1:20, first crawler 2:44–3:05, first red 5:33–6:18, Generator 2 at 6:01–6:02, first line magazine 8:07–8:10), east and west (15 / 25), Generator 4 at 45, walking under 5 %, the HQ standing at the hour on every seed, the logged hour replaying to the same state. **Changed by the decided minutes:** the second steel Excavator 12 → 15 (steel min 30 → 0) and north 65 → 40 (a fall inside the hour and 299–300 s of brownout).

## Layout and readability pass — built 2026-09-05 (unverified; `docs/PROGRESS.md` T3)

Full report: **`docs/LAYOUT_PASS_REPORT.md`**. This finishes the pass the economy-fix task started
on 2026-09-04 (the section above: `Scale.RESIZE` and the full-viewport world, the top-left HUD and
the bottom-left key strip, the HQ's kerb pips, the Depot's outline, fill bar and beacon). Drawing only — no file in `packages/sim` was opened,
the config hash is unmoved (`0176f61d`), and no measured number in this report can have changed.

Ten of `ROADMAP.md` §2's eleven readability items were built: the HUD as four corner overlays with
the toasts moved to bottom-centre, the map view as a centred full-screen overlay that refits on
resize, an opening zoom that puts the HQ lot at about a third of the viewport's height, the kerb
line where a lot meets a street, a soft light falloff (a render-side blur over the lit mask —
`lightPix`, `lightMask` and `litAt` are untouched), a common drop shadow and rim on every box
machine, the engineer's facing from `e.face` with a chevron, the shade as a diamond against the
crawler's disc, and the kerb pip on every Held block's front rather than the HQ's alone. The
full-viewport canvas and the clumped rubble were already built.

**The eleventh item was refused.** §2's line asks for the engineer to carry "a lamp cone";
**D-B5-1** (decided, Daniel, 2026-09-04) decided against a personal light. The facing was built,
the cone was not, and the ROADMAP line is reported as wrong rather than edited (constitution rule
12). D-B5-1 reopens only on STANDARDS row A.7 — the played-dark sentence — which the T5–T8 waiver
did not produce (`GATE_B.md`).

**Cheap checks green:** `npm test` 121/121, `typecheck`, `lint`, `docsync:check`, `snapshot:check`.
**Unverified:** the light-map blur costs 7–12 ms of arithmetic per repaint in Node at eight
repaints a second, and the map view rebuilds its raster on every resize step; both want the 900 s
soak on the reference machine before the pass's frame numbers mean anything. The pass's own success
condition — a stranger points to street, lot edge, lit area, rubble, Depot and engineer unaided —
is **untested**: T7 was waived and `docs/layout-pass/STRANGER_TEST.md` was never written.

**Three decisions:** D-LP-1 (the opening zoom clamps to 0.5× at 1080p and seed 4's 46-tile HQ lot
never reaches a third), D-LP-2 (the key strip's corner), D-LP-3 (the falloff strength — the one
that touches the identity).

## The blockers — built and **measured** 2026-09-05 (`docs/PROGRESS.md` T1 done)

Full report: **`docs/ECONOMY_FIX_REPORT.md` §9**. On the instruction "Do the blockers" the three
decision rows that walled T1's third clause were written as `decided` — Daniel, 2026-09-05, that
message, each taking its own recommendation — and then built. This section carries measured
numbers rather than the usual *unverified*, because T1's definition of done **is** "make main
green": the suite is the measurement (constitution rule 13), so `npm run experiments` was run.

### Built

- **D-HOUR-2 (a)** — `constants.HOUR`'s `steel-2` from minute **15 to 12**; §11's generated minute
  table follows by docsync; GA-EF-1's provisional 12:00 becomes a decided row. The decided 200
  start steel was the decided minute list's bill to minute 15 exactly, so no bot behaviour could
  have saved it.
- **D-P4-10 (a), the north-minute half** — `claim-north` from **40 to 65**, past the hour on
  purpose, and `HOUR_END` from 4 Held to **3**: hour one is two claims (east 15, west 25). The
  report expects north's claim, the HQ's white border and the Electricians only from `northAt`;
  `firsthour.ts` records that the `t < 3600` loop never adds north's substation. The 300 s
  brownout between two decided minutes is gone by construction.
- **D-P4-9** — §11's two carried turrets removed, with `carryTurrets`, `turretSpots`, `idleTurret`,
  `hqTurrets` and `HourBot.carried`; `flow.ts` and `firsthour.ts` lost their references and
  `hour.test.ts`'s minute-45 test was rewritten for two claims. Through Gate B a claimed block's
  edges keep the block sim's ring-fed hopper and nothing physical stands on them.
- **One defect from the light review**: `E-hour-stock`'s header was thirteen hardcoded five-minute
  labels against twelve columns of data, so every published value read one step late. It is now
  derived from the report's own sample times.

### Measured

**`npm run experiments`: 13 experiments, 231 s, 0 failing checks.** E-hour **10 / 10** (was 6 / 10),
E-rifle **12 / 12** (was 10 / 12 — the seed-5 knockdown in the 600 s edge rescue does not recur).
`snapshot:check` matches at config `0176f61d`; `npm test` 121 / 121; typecheck, lint and
`docsync:check` green.

| | before (economy-fix task Step 6) | now |
|---|---|---|
| chest steel minimum | 0 at 15:02–15:04, every seed | **30 at 12:01–12:02**, every seed |
| brownout | 262–315 s (exactly 300 s where nothing falls) | **0 s, 0 refusals**, six runs |
| falls | 3 runs of 6 lose north | **0 falls**, six runs |
| end state | 4 Held expected, not reached | **3 Held**, HQ held, 6 turrets, 4 Generators, 7 Excavators, 3 Assemblers, 519 magazines, six runs |
| walking / walk-overs | — | 2.2–3.8 % against §19's 15 %; 9–14 s against the 60 s rule |

**`E-hour-north` (measured, not scored) is the result worth naming.** Carried to 75:00 with north
claimed at `constants.HOUR`'s 65:00, north is Held at 65:29–65:34 and **never falls on any of the
three seeds — 0 falls, 4 Held at 75:00** — where the same variant fell at 72:45–74:38 on every seed
while §11 carried two turrets onto it. Removing them is what saved it: a physical turret on a claim
takes its edge off the ring feed (`hookSyncEdges`) and swaps a ring-fed 100-round hopper filled from
a buffer for a hand-fed 50, and one engineer cannot keep three of those fed. On the block-level
hopper north rides out the 510–557 s brownout that the hand-fed turrets did not. The Generators
still run dry at 67:51–68:01 and the chest at 75:00 reads 1520–1524 steel / 397 copper / **0 coal**.

### Where it disagrees with the doc

- **§11 said the chest's coal is 2 at minute 60.** It is **0**, spent for good at **56:00**, on
  every seed. Two sentences corrected.
- **§11 still described the two south-facing turrets picked up and carried, and "four stacks in
  your pockets".** Removed through Gate B under D-P4-9; the sentence now says what a claim's edges
  actually do.
- **§11 still put the second steel Excavator at minute 15 and north at "60–75 once west's coal is
  real".** Both are decided numbers now, and the prose says so.

### Deferred

- **Every front edge physical — a claim lays its segment's turrets from stock** (D-P4-9's other
  half) → after Gate B. The four functions were deleted, not hidden; `DEFERRED.md` names commit
  `6694b71` as the last one that has them. It cannot be built until the ring feed keeps feeding an
  edge that has a turret on it.
- **West's coal made real** (D-P4-10 (a)'s other half) → after Gate B and behind **D-P4-12**.
  `rubbleOf` has no coal kind at all — stone / copper / steel / null — and west is a rail yard, so
  a physical Excavator on its lot digs steel. The two constants it needs are the human's.

### Three decisions for the human

1. **D-P4-12** — the rail yard's coal (recommended: a fourth rubble kind, `coal`, at the HQ patch's
   ~700). Blocks nothing; what it buys back is the Generators running dry at 67:51–68:01.
2. **`HOUR_END.held` is 3**, so §11's enclosure — the HQ's white border when north holds — and the
   Electricians walking out of civic north are beats a 3,600 s run never reaches. Is that the shape
   of hour one, or a hole in it?
3. **The first shade still never happens** in the bot's hour, six runs, against §11 and E3's minute
   32–47. It is the only finding E-hour prints, and it has now survived every economy change.

## E-rifle at tile scale — the rescue run and the steady run, **measured** 2026-09-05 (`docs/PROGRESS.md` T4 done; run name `E-rifle-tile`)

**The rifle decides a rescue.** At block scale it never did: twelve of twelve rescues held either
way, and the item went to `DEFERRED.md` as "the block-scale E-rifle never sees the rifle decide a
rescue". At tile scale, over **24 scenarios run twice** (2 belts × 2 scopes × 3 seeds × 2 minutes,
rifle off and on — 48 runs), **seven blocks fall without the rifle and four with it**: the rifle
**saves three outright**, delays three of the four it cannot save, and never loses a block sooner
than the unarmed run does. Evidence: `docs/EXPERIMENTS.md` rows `E-rifle-tile-steady` and
`E-rifle-tile-rescue` (regenerated on the committed tree), and `docs/experiments/E-rifle.json`.

Measured, not *unverified*: T4's definition of done **is** the measurement (constitution rule 13),
so `npm run experiments` was run — **13 experiments, 256 s, 0 failing checks**, E-rifle **14 / 14**
in 96.6 s. The tile sections themselves were built on 2026-09-04 and the blockers task changed what
they measure, so this task re-ran them on a commit rather than a working tree and wrote the numbers
down.

### Built

One thing, and it came out of reading the run rather than out of the task line: **the tile steady
table now carries §19's two guards.** The tile path always accumulated them — `walk.ts`'s
`spendRound` calls `markShot`, and `advanceFlow` runs the block `step()` once every sim second
(`flow.ts`, `if (f.tick % TILE_TPS === 0)`), which is where `engineer.danger` is filled — but the
`E-rifle-tile-steady` table stopped at HP lost and downs, so the 10 % shooting and 5 % danger caps
were scored **only** on the 5 h compact bot. Two columns and two checks (`erifle.ts`), and E-rifle
goes **12 checks → 14**. No sim file was opened; the config hash is unmoved at `b95922d2`.

Everything else T4 reports is a measurement of code that already exists (`erifle.ts`'s `tileRescue`
and the two tile sections; `hour.ts`'s `handsOff` and `rescueStance`). The re-run is against commit
`2f1b016`, on top of the three rows decided on 2026-09-05 (D-P4-9, D-HOUR-2, D-P4-10).

### The steady hour — `E-rifle-tile-steady`

§11's hour, the hour bot, rifle off vs on. The reflex fires at the nearest crawler in range while
the bot walks its script.

| seed | line magazines off → on | diff | hand-fed off → on | falls off / on | rifle rounds | rifle kills | turret kills | first shot | HP lost | downs | shooting (§19: 10 %) | danger (§19: 5 %) |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 3 | 519 → 519 | 0.00 % | 858 → 856 | 0 / 0 | 18 | 1 | 438 | 14:02 | 0 | 0 | **0.50 %** | **0.00 %** |
| 4 | 519 → 519 | 0.00 % | 821 → 823 | 0 / 0 | 25 | 3 | 325 | 5:37 | 0 | 0 | **0.69 %** | **0.00 %** |
| 5 | 519 → 519 | 0.00 % | 792 → 792 | 0 / 0 | 2 | 1 | 227 | 6:22 | 0 | 0 | **0.06 %** | **0.00 %** |

The rifle is invisible in steady play at tile scale, which is what §24 risk 10 asks for: the line
makes the same 519 magazines with it and without, the hands carry within two trips of the same
number, five kills of 990 across the three seeds are the engineer's and the other 985 are the
turrets', and nobody takes a scratch. The bot's hour is 45 rounds fired on three seeds; the first
shot lands at 5:37–14:02, i.e. after the first bloom, never at the start.

**§19's two guards, now measured on the hour the player plays**: the engineer shoots for **18 s /
25 s / 2 s of 3,600 — 0.50 % / 0.69 % / 0.06 %** against the 10 % cap, and spends **0 seconds** with
a crawler on them — **0.00 %** against D-B1-5's 5 % cap. That is a fifteenth to a two-hundredth of
what §19 allows. The block-scale rows, on a 5 h compact bot, read 0.00 % and 0.07 %; the tile hour
is the busier of the two and still nowhere near the ceiling. **The caps are not what constrains
this design** — nothing in the hour pushes the engineer toward the shooting §19 was written to
limit, and the number that does bite is the rescue's HP, below.

### The rescue — `E-rifle-tile-rescue`

The most threatened Held block runs dry: `edge` = the turrets and hopper of the edge facing its
worst Dark neighbour at 0 rounds, `ring` = every edge of the block. The belt is **90 s** away (§11's
rescue) or **600 s** (it never comes inside the window). The bot drops its script and its hands and
stands at the dry edge's street midpoint; with the rifle it carries 20 magazines, without it stands
there empty-handed — **the body is in both arms, the magazines are the difference**. Then 10 minutes.

| belt | scope | scenarios | fall without the rifle | fall with it | verdict |
|---|---|---|---|---|---|
| 90 s | edge | 6 | 0 | 0 | a late belt is not the rifle's fight |
| 90 s | ring | 6 | 0 | 0 | nor is a late belt on every edge |
| 600 s | edge | 6 | **1** (seed 5, 20:00, +7.2 min, `unfed`) | 0 | **the rifle's fight, and it wins it** |
| 600 s | ring | 6 | **6** (+4.3 to +8.9 min, all `unfed`) | 4 | two saved (seed 4, both minutes); the other four fall **later**: +5.6 → +5.7, +6.3 → +9.0, +4.3 → +4.4, +5.6 → +7.7 |

- **Every fall in all 48 runs is `unfed`** — crawlers reaching the substation past dry turrets.
  Not one is a shade.
- **Cost**: 0–120 HP lost, HP min **34** (600 s edge, seed 3, 35:00), **0 knock-downs in 24 rifle
  runs**; the mean rescue costs **38.7 HP** against the block-scale rescue's 62.4.
- **Ammo is not the limit**: 924 rounds and 307 kills over the 24 rifle runs, the busiest single
  rescue **96 rounds** — under half of the 20 magazines (200 rounds) the engineer carries in. What
  runs out is coverage and HP, not Shot.
- **Scale of the fight**: 22–170 crawlers are born in a rescue window; the block that falls unfed
  without the rifle sees 107 of them, and the ring scenarios that beat the rifle see 97–170.

### The fourteen checks (E-rifle 14 / 14, all green)

The seven tile checks: the magazine bill moves < 5 % (0.00 %); nothing falls in the hour rifle off
or on (0 / 0 / 0); **shooting ≤ 10 % of the hour (max 0.69 %)**; **danger ≤ 5 % (max 0.00 %,
D-B1-5)**; one dry edge whose belt never comes — **1 / 1 single-edge falls saved**; a whole dry ring
is beyond one rifle at one edge — **2 / 6 ring falls saved, the rest delayed 0.1 / 2.8 / 0.1 /
2.1 min**; the engineer is never knocked down (HP lost 0–120, HP min 34). The seven block-scale
checks are unchanged: 0.00 % ammo diff, nothing lost in steady play, shooting 0.00 % and danger
0.07 % of an hour (§19, D-B1-5), 2 / 12 scenarios fall without the rifle, and the 62.36 HP damage
number.

### Determinism

Nothing the sim computes moved. Against the committed generated files, the whole of `docs/` after
the re-run is: **the `source_commit` stamp on fourteen files, one wall-clock figure (231 s → 256 s)
in the `EXPERIMENTS.md` header, and the two new §19 columns and two new checks in E-rifle.** Twelve
of the thirteen experiments produced JSON whose only changed line is the stamp; E-rifle's only
removals are the stamp and the reflowed table header. Two invocations on two commits, the same
numbers.

### Where it disagrees with the doc

1. **§5's rescue sentence is the block sim's, and the tile sim answers differently.** §5 says "when
   the block's whole belt is 90 s away it falls in about 1.5 min to a **shade** at an unlit edge, and
   the rifle cannot save it" **[sim: E-rifle]**. At tile scale a whole ring 90 s dry **never falls at
   all** (12 of 12 hold, both arms), and when a ring does fall — the belt 600 s away — the killer is
   **always crawlers, never a shade**. Both sentences are tagged to their own run, so neither is
   wrong as written; but §5's picture of what beats the rifle (an untargetable shade) and the tile
   layer's (more crawlers than one engineer at one edge can kill) are different pictures, and the
   tile one is the one the player will meet. §11's `[sim: E-rifle-tile]` sentence already reads the
   tile way and needed no edit.
2. **§19's two guards were being scored on the wrong bot — fixed in this task.** The 10 % shooting
   and 5 % danger caps were computed only by the 5 h compact bot in `E-rifle-steady`. The tile path
   was never uninstrumented — `markShot` fires from `walk.ts`'s `spendRound` and `advanceFlow` runs
   the block `step()` every sim second — the tile *report* simply did not carry the two shares. It
   does now (0.50 / 0.69 / 0.06 % shooting, 0.00 % danger), and the caps are checked where the rifle
   actually is. **No §19 number in the doc moves**: the tile hour is comfortably inside both.
3. **The generated header's date is UTC.** `EXPERIMENTS.md` says "Generated … on 2026-09-04" for a
   run made on the morning of 2026-09-05 AEST, because `cli.ts` stamps `toISOString()`. Cosmetic,
   but every generated file dated this way will read a day early for this machine's mornings.

### Deferred / re-read

`DEFERRED.md` has its "Re-read at E-rifle at tile scale" section: the 2026-09-04 item **"E-rifle at
tile scale — the rifle never decides a rescue"** is **deleted**, closed by this run, and the §19
reporting gap this task found is **deleted in the same breath** — it was built, not parked. Nothing
new is parked.

### Three decisions for the human

1. **Which scale scores §5's rescue sentence?** Recommendation: **the tile layer** — rewrite §5's
   "falls to a shade in ~1.5 min, the rifle cannot save it" as the tile finding (a late belt costs
   nothing; a belt that never comes costs the block unless the engineer stands on the dry edge; a
   whole dry ring is beyond one rifle) and keep the block-scale sentence as the lattice's record.
   No constant moves either way; it is which sim the design sentence quotes.
2. **Can D-R1 come off `provisional`?** The row — "the rifle and the shade" — was taken as (a) *keep
   D5, the rifle is for crawlers* on 2026-09-04, with (b) a hand lamp held back for a slice M5
   experiment "if Gate B testers lose a block to a shade while standing on it". Two runs now say the
   shade is not the fight: **no shade kills anything in the 48 tile rescues** (every fall is
   `unfed`) and **E-hour spawns zero shades in six hours**. Recommendation: **decide (a)** — with
   the caveat that both are bot runs, and Gate B, the play that (b) was waiting on, was waived.
3. **Does the 600 s belt stay?** It is not §11's rescue (§11's is 90 s) and it is the only scenario
   in which the rifle changes an outcome. Recommendation: **keep it, named as what it is** — the
   rifle's whole justification under §24 risk 10 rests on it, and at 90 s the experiment cannot tell
   an armed engineer from an unarmed one.

## Gate B — **proceed, with conditions** (2026-09-05, `docs/GATE_B.md`)

verdict: **proceed — with conditions.** Recorded for Daniel on 2026-09-05, and
**closed by him as fully tested the same day** — `PROGRESS.md` T8 is `done` and
nothing is owed back to the gate.

**Where the numbers come from.** Minutes 0–10 of §19's first-hour table carry the
player's own words; **10–30 and 30–60 were not recorded and stay empty**, and no
minute markers or telemetry were taken, so every shooting, danger and walking share
quoted in this report is still **the bot's**, tagged `[sim: E-hour]` /
`[sim: E-rifle-tile]` and never `[play: Gate B]`. The controls walkthrough, the
two-assembler line and the stranger test remain **waived, not passed**.

**What it closed.** **STANDARDS row A.7**, the hand-lamp sentence, played dark:
*"Yea the dark is pretty good so far."* The dark reads as **the claim's price, not
as a missing flashlight**, so **D-B5-1 does not reopen and now has play behind it**
— the first of `STANDARDS.md`'s 46 rows to close (`closed: 1 of 46`).

**What it found.** §19 expects minutes 0–10 to teach two rules (blocks bloom; ammo
is made from rubble). **Neither was named back.** What came back was *"movement is
good … but needs more development time"* and, as the hour's one problem, *"knowing
what to do next"*. The memorable moment §19 expects by minute 50 was not reached
(*"nothing yet"*). The rifle was fired without mattering — *"I was just shooting at
things"* — which is the play-side half of what **D-R1** has been waiting for and
agrees with `E-rifle` at both scales. **No shade was seen**, and none was recorded
either way, so §11's minute 32–47 is neither confirmed nor contradicted: `E-hour`'s
"never" stands as **the experiment's open finding**, carried into Phase 5, not as a
debt of this gate.

**What it opened — four rows, D-GB-1 to D-GB-4**, three of which asked for
something the doc does not contain (constitution rule 12: listed, not built).

**D-GB-1 is decided, as the hybrid (option c)** — Daniel, 2026-09-05. The claim
still burns off its block, so §5's rule, §11's hour and every measured number in
this report stand; **powering and lighting the next area becomes a physical
expedition the engineer walks**, and an area lighting up becomes a threat trigger
beside the wake bloom. It is Phase 5 / 6 work; the doc sentences it moves (§5, §10,
§11, §19) move on the milestone that builds it, with a changelog line.

**D-GB-2 (knowing what to do next, against constitution rule 8's "no tutorial
screens"), D-GB-3 (rifle range) and D-GB-4 (enemy density) are `open (gate
condition)`** — the gate passed *with* them, so they are resolved inside Phase 5.
D-GB-4 needs no permission: §19 caps player shooting at 10 % of an hour and the
measured hour is **0.50 / 0.69 / 0.06 %**, with danger **0.00 %** against a 5 %
cap, so density has twenty-fold headroom inside the design's own limits.

**The standing tension, named once.** D-GB-1, D-GB-3 and D-GB-4 all pull toward a
game in which the player fights, explores and lights the world personally. §19 caps
the player's shooting at a tenth of an hour on purpose, because the doc's game is
one in which turrets fight and the engineer builds the thing that fights. The
hybrid is the answer taken; if Phase 5's work finds it does not hold, this is the
sentence to come back to.

---

# Appendix — the lattice slice (superseded by the D5/D6 rework, 2026-09-04)

Not scored at Gate B. Kept as the record of what the lattice slice built and measured; its GAME-ASSUMPTIONs that survive the rework are still in the code and listed in `REWORK_REPORT.md` §3.

## Absorb Gate B — **doc only, no code**, 2026-09-05 (`docs/PROGRESS.md` T9 done; evidence `PROGRAMME_STATE.md` B.31)

The last Phase 4 task. It folds the played hour into `RELIGHT-design.md` and this report, records D-GB-1's hybrid as a decision the doc carries but has not built, attaches the gate's evidence to the rows that were waiting on it, and hands D-GB-2 / D-GB-3 / D-GB-4 to Phase 5 as the verdict's three conditions.

**Built.** Nothing in code, deliberately — see "where it disagrees" below. Seven edits to `RELIGHT-design.md`, and the played hour folded into this report's §19 table and its two Gate B rows:

1. **§4 — the pre-placed `[play: Gate B]` tag split.** The tag had been put on the body sentence before the gate was played. Only the movement half survives contact: *"movement is good, it's kind of intuitive but needs more development time"* keeps `[play: Gate B]`; sprint, dodge and "the game is completable without any of them" keep only `[sim: B-M1-body]` because they were never exercised. The rifle's own play line (*"I was just shooting at things"*) is named there as D-R1's play half.
2. **§4 — the range clause named as the disagreement.** "Out to the turret's 9 tiles (no range advantage)" is the one clause the played hour argued with; **D-GB-3** is named against it, and it stands as written until that row is decided.
3. **§19 — the 10 % shooting cap loses its `[play: Gate B]` tag.** The gate read no telemetry, so no player shooting share exists and the tag was never earned. The bot's **0.50 / 0.69 / 0.06 % shooting and 0.00 % danger [sim: E-rifle-tile]** are stated in its place, and **D-GB-4**'s ask is named against that headroom.
4. **§19 — the first-hour test carries the finding against itself.** Neither of minute 0–10's two rules was named back; movement was; the session's only problem was not knowing what to do next (**D-GB-2**, against constitution rule 8, so the rule-8-legal forms are listed first). The later windows are marked untested rather than contradicted.
5. **§10 — D-GB-1's hybrid, decided and not built.** The three loops stay exactly as written; the hybrid's content is recorded beneath them (the expedition the engineer walks; an area lighting up as a threat trigger beside the wake bloom) with the statement that §5, §11 and §18 do not move until the milestone that builds it.
6. **§19 — the walking share stops implying a human number.** "A player's share is M6's number" is corrected: M6 measured the bot (4.6–5.2 % at tile scale) and Gate B read no telemetry, so the row has no human number and the doc no longer says otherwise.
7. **§25 — open questions 19, 20 and 21**, the three gate conditions, so they live in the spec and not only in `DECISIONS.md`. The **Changelog preamble** now says what a `[play: Gate B]` tag means beside `[play: Gate A]`: a witnessed statement from one played hour, never a measured share.

**Assumed.** No new GAME-ASSUMPTION. T9 built nothing, so it assumed nothing about the sim; the register above is unchanged.

**Deferred.** D-GB-1's build (the expedition and the lighting-up trigger) to Phase 5 / 6, with the doc sentences it will move named in `DECISIONS.md` and §10. D-GB-2's answer, D-GB-3's range number and D-GB-4's density number to Phase 5 as the verdict's conditions. §19's unrecorded windows, the burn-off line, the first shade and the human walking share to the next played session — observations, not debts (`DEFERRED.md` re-read).

**Measured.** Nothing new. The only figures T9 writes into the doc — 0.50 / 0.69 / 0.06 % shooting and 0.00 % danger — arrive already tagged `[sim: E-rifle-tile]` from T4's measured run. The suite is unmoved at 13 experiments, 0 failing checks, and the config hash did not change because `packages/sim` was not opened. Cheap checks only for this task; nothing here waits on a verification pass.

**Where it disagrees with the doc — and with its own task line.** T9's task says "fold the played hour and D-GB-1's hybrid into **code** and doc". **No code was written, and that is the honest answer rather than a shortfall.** The gate produced no constant; D-GB-1's build is Phase 5 / 6 work by the terms of the decision itself; and D-GB-2, D-GB-3 and D-GB-4 are `open (gate condition)`, so writing a quest system, a rifle range or a density number now would be building from a prompt that carries a rule the design doc does not — constitution rule 12, the same rule the gate invoked when it refused to build them. The second disagreement is with the doc's own history: **two `[play: Gate B]` tags were placed before Gate B was played**, and one of them (§19's cap) was simply not earned. It has been removed rather than justified.

**Three decisions for the human.**
1. **D-R1 — take (a).** The rifle's play half is in (it decided nothing), and A.7's answer closes option (b), the hand lamp. (a) is the only live option and is already what the code does; deciding it is a signature.
2. **D-P4-5 — make every assembler physical.** The condition attached to that recommendation ("the block-level button stays through Gate B so the played hour is one model") is met. It is now unconditioned and blocks nothing.
3. **D-GB-2's first form.** The rule-8-legal answer — a next-objective line in the HUD driven by the sim's own state — is cheap and is what Phase 5 would build first. A quest system is a rule-8 change and stays yours to take explicitly.

## Lattice M1 Ground — built 2026-09-03

### Built

- **Tile layer** (`packages/sim/src/tiles.ts`, run name `M1-tiles`): a pure derivation of the block map. 32×32-tile cells, 24×24 lots inside 4-tile margins (so streets are 8 wide where two margins meet), the 768×768 city, row 23 river under a 4-tile embankment street. Rubble 250–350 tiles per block, typed by district (civic stone, residential copper, industrial steel), five density variants, three clusters per lot, denser on deeper blocks. Outskirts: no rubble; one cell in four carries a 160-tile iron or coal deposit. Standing rubble is `round(tiles · pool ÷ poolMax)` and digs out thinnest-first as the block's pool drains. `cellKey(state, rubbleLeft)` is the renderer's cache key; `describeTile` feeds the tooltip. Nothing in the tile layer writes to the block sim: the map view's block state is authoritative, as the constitution requires.
- **World view** (`packages/game/src/worldScene.ts`, `view.ts`, `main.ts`; the proto package renamed `packages/game`): Phaser Blitter over a code-drawn 29-frame tileset, per-cell cache on `cellKey`, block-state overlay on lots, cell labels, HUD pinned at screen scale, drag / WASD pan, wheel zoom 0.5–3× about the pointer, tile tooltip, HQ slab. **E** toggles map ↔ world at the same block (map → world takes the hovered block, world → map takes the block under the camera centre and marks it). `?view=world` opens in the world view. The sim is stepped from Phaser's `STEP` event in either view, so the map-view bot and fixtures run unchanged underneath.
- **Tests:** six in `packages/sim/test/tiles.test.ts` — geometry, river and inert, rubble counts and types, gradient, authority of block state, determinism and cost. `npm test`, `snapshot:check` (map-view fixtures), `docsync:check`, `experiments`, lint, typecheck, and the game build are green.

### Assumed (every `GAME-ASSUMPTION` in M1 code)

| # | Where | Assumption | Resolves at |
|---|---|---|---|
| GA-M1-1 | `tiles.ts` | Rubble count is uniform in 250–350 per cell by hash; district and depth change the density variant, not the count. | Phase 5 (rubble as finite typed ore) |
| GA-M1-2 | `tiles.ts` | Outskirt lots carry no rubble; one outskirt cell in four holds a 160-tile deposit, iron three times in five, coal otherwise; no oil field; deposits are not consumed (the block sim has no outskirt pool). | Phase 9 sites (D-P4-3) |
| GA-M1-3 | `tiles.ts` | Rubble sits in three gaussian clusters per lot (σ 6 tiles) so a lot reads as heaps with clear ground between, as in the §18 sketch. | Phase 12 art pass |
| GA-M1-4 | `tiles.ts` | Density variant = 1 + round(4 · (0.55 · rank in heap + 0.45 · depth)), depth being `dmax` over the district base from 0 at the start to 1 at +50 %. | Phase 5 (E11/E13) |
| GA-M1-5 | `tiles.ts` | Digging order is thinnest heap edge first, with standing rubble proportional to the block pool; M2's excavators replace it with their footprints. | M2 |
| GA-M1-6 | `tiles.ts` (implied) | Units per rubble tile are not modelled: a tile is 1/N of the block's 3,840-unit pool, not §12's 300 units. | D-P4-2, Phase 5 |
| GA-M1-7 | `worldScene.ts` | Zoom range 0.5–3× per the constitution; §4 edited to match. | D-P4-1 |
| GA-M1-8 | `worldScene.ts` | Flat block-state overlay on lots stands in for rot presence (M4) and the light texture (M5). | M4 / M5 |
| GA-M1-9 | `worldScene.ts` | HQ is a 6×6-tile slab at the start lot's centre until M2 places the Depot. | M2 |
| GA-M1-10 | `worldScene.ts` | Code-drawn flat-colour tileset frames stand in for the §4 tilesets; the frame table is the contract. | Phase 12 |
| GA-M1-11 | `main.ts` (implied) | No 20 ticks/s tile tick yet: M1 has nothing that moves at tile level, so the sim still steps in 1 s block ticks from the render loop. | M2 |

### Deferred

Excavators and footprints → M2; slot count as lot geometry → M2 / Phase 5; the §18 lot sketch from the world view → M3 (needs turrets and belts); rot as tile presence → M4; light texture → M5; tileset art → Phase 12; deposits as placed facilities → Phase 9. All in `DEFERRED.md` with a phase.

### Measured

- **Tile derivation cost:** `cityTiles` for all 576 cells, cold, 51–66 ms (tileset and layouts built once); re-deriving all 576 cells against the cache, 2 ms. Per city over seeds 3/4/5: ≈102–103k rubble tiles, 8–10k deposit tiles.
- **View checks (headless Chromium, canvas 648×648):** 529 tiles drawn at 1×, 1,849 at 0.5×, 81 at 3×; tooltips on rubble (type and density), ground, street, and the river; E round-trips at the same block by keyboard and API; console clean.
- **Render loop, 30 s samples (in-page RAF sampler; Phaser's delta is smoothed and is not the honest number):** 4× static and 16× roaming both 1,800 frames, mean 16.67 ms, worst 16.7 ms, none over 50 ms.
- **The first 1 h at 4× soak failed, and not on render cost.** At sim 0:02:47 the map scene threw `Cannot read properties of undefined (reading 'state')` from the game's `STEP` handler, Phaser's loop stopped, and the sampler degraded to 66 ms frames with a frozen clock. Cause: `MapScene` took its session from Phaser's `init(data)`, which never runs for a scene added asleep (`?view=world`); the first sim event handed to the sleeping scene dereferenced it. Fixed by passing the session through the constructor, as the world scene already did. This is the kind of thing the constitution's soak rule exists to catch.
- **1 h at 4× soak after the fix** (seed 3, `?view=world&autoplay=compact`, camera roaming to a random block every 5 s cycling 0.5/1/2/3/1.5×, bot claiming):
  | Measure | Value |
  |---|---|
  | Real time | 900 s |
  | Sim time reached | 1:00:07 |
  | Frames (RAF sampler) | 53,998 |
  | Mean frame | 16.67 ms (60 fps) |
  | Worst frame | 66.7 ms, one frame, at the moment a headless screenshot was captured mid-run |
  | Frames over 50 ms | 1 (that one) |
  | Frames over 33 ms | 2 |
  | Camera moves | 180 (random block every 5 s, zoom cycling 0.5/1/2/3/1.5×) |
  | Bot (compact) | 4 claims, 4 held, 0 lost, 0 interior at the hour |
  | Errors | none |

  Phaser's own smoothed delta over the same run: 53,997 frames, worst 21.7 ms, none over 50 ms. The render loop was not dropped: the one long frame is the screenshot tool's capture, and the sim clock ran the full hour at 4× in 900 s of wall time. **Constitution check met.**

### Where M1 and the doc disagree (reported, not resolved)

- Zoom: constitution 0.5–3×, §4 said 1.0–0.2×. Built 0.5–3×, §4 edited to it. **D-P4-1.**
- Rubble units: §12 says 300 units per tile; the sim's calibrated pool is 3,840 per block, which would be 11–15 units per tile. **D-P4-2.**
- Deposits: §7/§12 name placed mines and seams on the outskirts; M1 hashes a patch onto a quarter of outskirt cells. **D-P4-3.**

## Lattice M2 Flow — built 2026-09-03

### Built

- **Tile flow layer** (`packages/sim/src/flow.ts`, run name `M2-rates`): a fixed **20 ticks/s** tile tick under the 1 s block tick. `advanceFlow(state, realSeconds × speed)` steps whole tile ticks from an accumulator and calls the block `step` every 20th tick, so the map-view sim, bots and fixtures run unchanged underneath (`snapshot:check` still verifies `ee23bb1c`; the harness never calls `ensureFlow`, so E1–E9 are byte-identical). Machines: **Excavator** 3×3 (0.5 units/s off the patch or rubble tile it stands on, one tile at a time, onto the belt it faces), **belt** (7.5 items/s, one lane, four items a tile, items visible, corners and side feeds), **inserter** (1 item/s, half a second each way, picks only what its target could use, waits holding the item while the target is full), **Mk1 Shot assembler** 3×3 (3 s: 2 steel + 1 Cu → 1 magazine, four crafts of input and five magazines of output buffered), and the **Depot** (6×6 on the HQ lot; everything belted or inserted into it joins `stock` / `buffer`, the global stock hand placement draws from). **Hand-mining** (hold the hand tool on a rubble or patch tile: a unit a second into the Depot) and **hand-crafting** (C: a magazine in 3 s from stock) as §11's make-up. Placement rules (`canPlace`): Held cells only, belts and inserters on lot or street margin, excavators and assemblers on the lot, no rubble under anything but an Excavator, costs from stock with a full refund on removal. A machine whose block stops being Held stands still; the block map stays the judge.
- **HQ lot** (`tiles.ts`): the §11 patches as `T_PATCH` tiles (steel 5×5 carrying the block sim's 7,680, copper 4×3 at 100 a tile, coal 3×3 at 700 total) laid as the §18 sketch, the Depot footprint clear, the start lot cleared to 100 rubble tiles in its south strip so the line has ground. The block sim's flat HQ patch drain and the start lot's flat rubble yield are off once the flow layer exists: the lot's steel is dug by machines and hands.
- **World view** (`worldScene.ts`, `main.ts`, `panel.ts`): tools on keys (X Excavator, B belt, I inserter, M assembler, R rotate, Q hand, C craft), a green/red 3×3 or 1×1 ghost with the refusal reason in the HUD, click or drag to place, right-click to remove with a refund toast, hold-to-mine on rubble, machines drawn as flat shapes (belts with their items and corner geometry, inserter arm and held item, Excavator drill, assembler progress bar and buffers, the Depot slab), a machine line at the top of the tile tooltip. The panel shows the line: machine counts, items on belts, line mag/min, magazines made by the line and consumed, hand mined/crafted, the Depot's magazine buffer, coal, and a "Craft a magazine by hand" button. Production in the panel and telemetry is block production + line production. `?flow=0` turns the layer off (block-only sessions for bot comparison).
- **Telemetry:** per-minute `tileMagsMade`, `tileMagsDelivered`, `magsConsumed`, `handMined`, `handCrafted`, `tileMagPerMin`, machine counts, `beltItems`, `mined`, `coal`; the summary carries the flow summary.
- **Tests:** ten in `packages/sim/test/flow.test.ts` (`M2-rates`): the HQ lot and Depot; Excavator 0.5/s and the patch going with the tile; belt 7.5/s through a 14-tile run with a corner, spacing kept, no head-on feed; inserter 1/s and waiting on a full target; assembler 20/min into the Depot; a whole line (two Excavators → belt → assembler ← copper) at the doc rate; time (20 tile ticks a block tick, 72,000 tile ticks for 1 h at 4×, determinism across frame sizes, JSON round trip); placement rules, costs and refunds; the block map as judge; hands. `npm test` 59/59, `snapshot:check`, `docsync:check`, `experiments` 9/9, lint, typecheck and the game build are green.

### Assumed (every `GAME-ASSUMPTION` in M2 code)

| # | Where | Assumption | Resolves at |
|---|---|---|---|
| GA-M2-1 | `flow.ts` | Belts carry 7.5 items/s (the constitution's M2; §13/§14 said 8) as four items a tile at 1.875 tiles/s, one lane; a side feed joins at the tile's start like a corner. | D-P4-6; Phase 5 if two lanes are wanted |
| GA-M2-2 | `flow.ts` | An item joining a moving chain closes up to exactly one spacing behind the tail (rigid chain), at most one tick's travel ahead of where it was put; this is what makes a saturated belt carry 7.5/s at 20 ticks/s. | Phase 12 if belt feel needs Factorio's compression rules |
| GA-M2-3 | `flow.ts` | Inserter: half a second each way; it takes the front-most item its target could ever use and, if the target is full right now, swings over and waits holding it; it never picks an item the target has no use for. | M3 (turret hoppers) |
| GA-M2-4 | `flow.ts` | An assembler holds four crafts' worth of each input and five finished magazines, then stops. | Phase 5 (recipes) |
| GA-M2-5 | `flow.ts` | Hand-mining takes one unit a second straight into the Depot; hand-crafting a magazine takes the recipe's 3 s and its 2 steel + 1 Cu from stock. | D-P4-4; Phase 5 (Depot rules, §14.3) |
| GA-M2-6 | `flow.ts` | Machine costs in rubble (§13 gives none): assembler 40 steel + 20 Cu (the block-level price), Excavator 10 steel, belt 1 steel, inserter 1 steel + 1 Cu; removal refunds in full. | D-P4-4 |
| GA-M2-7 | `flow.ts` | Nothing draws power in M2; §13's kW ride on each machine for M3. | M3 |
| GA-M2-8 | `flow.ts` | A machine goes on any tile of a Held block's cell (belts and inserters on the lot or its street margin, excavators and assemblers on the lot), never on a tile another machine holds; only the Excavator may stand on rubble. | Phase 5 (walls, foundations) |
| GA-M2-9 | `tiles.ts` | The HQ lot's patches: 25 steel tiles carry the block sim's 7,680 (≈307 a tile, §12's ~300); 12 copper tiles carry 100 each (an hour of one Shot assembler); 9 coal tiles carry the 700 coal (D1). Laid as the §18 sketch. | D-P4-2 / Phase 5 (units per tile) |
| GA-M2-10 | `tiles.ts` | The start lot was cleared to make the HQ: it keeps 100 rubble tiles, all in lot rows 18–23, so the turret strip, patches, Depot and belts have ground; every other lot keeps §12's 250–350. | M3 (turret strip) / Phase 9 (generator) |
| GA-M2-11 | `session.ts` | The flow layer is on for every session unless `?flow=0`. Turning it on retires the HQ's Mk1 stand-in: hour one's magazines come from the line the player builds, or from hand-crafting. Block-level assemblers on claimed interior blocks stay as they were. | D-P4-5; M6 |
| GA-M2-12 | `worldScene.ts` | Flat item colours and flat code-drawn machine shapes stand in for sprites until the art pass; sizes and facings are the flow layer's. | Phase 12 |
| GA-M2-13 | `worldScene.ts` | Without the flow layer (`?flow=0`) the HQ is still a 6×6 slab at the lot centre. | never (block-only mode) |

GA-M1-5 (digging order), GA-M1-9 (HQ slab) and GA-M1-11 (no tile tick) are closed by M2.

### Deferred

Turret hoppers as inserter and belt targets → M3; power draw on machines → M3; splitters, undergrounds, chests, two-lane belts, fast belts → Phase 5 (nothing in minutes 0–60 needs them); the block-level assembler stand-in on interior blocks → M6 / D-P4-5; start stock and machine costs → D-P4-4; units per patch tile → Phase 5 (D-P4-2); the §18 lot sketch from the world view → M3, as before. All in `DEFERRED.md` with a phase.

### Measured

- **Rates at 20 ticks/s (flow tests, exact):** Excavator 0.5/s (300 units in 600 s, the patch down by 300, two patch tiles dug to ground); belt 7.5/s ± 0.05 through a 14-tile run with a corner, 0.25-tile spacing kept, a head-on belt does not feed; inserter 1.0/s ± 0.02, and it waits holding a magazine while the Depot buffer is full; assembler 100 magazines in 300 s (20.0/min) at exactly 2 steel + 1 Cu each, never touching the Depot stock; hand-mining 10 units in 10 s; two hand crafts in 6 s. The whole-line test (two Excavators on steel, one on copper, one assembler) delivers 397–400 magazines to the Depot in 20 min; left to run, it stalls only when the 4,000-round buffer (400 magazines) fills at minute 19, the §13 chest cap, which is correct behaviour.
- **Doc arithmetic holds in the world:** one Excavator on steel (30 units/min) feeds 15 magazines/min; §12's "20 mag/min = 1.3 Excavators on steel and 0.7 on copper" is what the line does (the browser line below ran at 20/min while its belt backlog lasted, then settled to one Excavator's 15/min, 336 magazines in the next 22 min, with the assembler waiting on steel a third of the time).
- **Time:** 1 h at 4× is 72,000 tile ticks and 3,600 block ticks whatever the frame size (the same tile ticks in other frame sizes give the same state, and a state survives a JSON round trip and continues identically).
- **Placement in the browser (headless Chromium, seed 3, `?view=world`):** a §11 line placed through the dev hooks: Excavator on the steel patch → 6 belts → inserter → assembler → inserter → Depot, and Excavator on the copper patch → 12 belts (one corner) → inserter → assembler. Cost 81 steel + 23 Cu against the calibrated start stock of 80 steel / 40 Cu: the last inserter waited on one steel, covered by three seconds of hand-mining (D-P4-4). Keys X/B/I/M/R/Q/C, left-click place, right-click remove (refund toast), machine tooltips and the ghost all exercised; console clean.
- **Render loop, 1 h at 4× in the world view with the line running** (seed 3, camera on the HQ lot at 1.25×, sampler on `requestAnimationFrame`):
  | Measure | Value |
  |---|---|
  | Real time | 942 s |
  | Sim time | 0:07:14 → 1:10:02 (3,768 s: exactly 4× real, the accumulator never fell behind) |
  | Frames (RAF sampler) | 56,514 |
  | Mean frame | 16.67 ms (60 fps), every 600-frame window at 16.7 |
  | Worst frame | 50.0 ms, one frame |
  | Frames over 50 ms | 0 |
  | Frames over 100 ms | 0 |
  | Line at the end | 2 Excavators, 18 belts, 3 inserters, 1 assembler, 72 items on belts |
  | Magazines made by the line | 535 in the soak (615 since placement), 610 delivered |
  | Consumed by the two HQ turrets | 200 (the block sim's ring, fed from the Depot buffer) |
  | Depot buffer at the end | 3,997 of 4,000 rounds: full, the line backed up as §13's chest cap says |
  | HQ steel patch | 7,680 → 6,409 (1,951 units mined, 4 stock) |
  | Errors | none |

  The render loop was not dropped and the sim clock ran the full hour at 4×. **Constitution check met.** Phaser's smoothed delta is not quoted; the sampler is the honest number, as in M1.

### Where M2 and the doc disagree (reported, not resolved)

- Belt rate: the constitution's M2 says 7.5/s, §13/§14 said 8/s (fast 16). Built 7.5; §13/§14 edited to 7.5 (fast 15) with a changelog line. **D-P4-6.**
- Start stock: §11 says 200 steel, 100 copper, 50 stone; the calibrated sim starts at 80 steel / 40 Cu / 0 stone (`PROTO_CALIBRATED`, Gate A). At M2's costs a §11 line is 81 steel + 23 Cu, so the player hand-mines for a second or belts the first Excavator's steel to the Depot before the last piece goes down. §13 prices no machine. **D-P4-4.**
- Two stand-ins for one thing: the flow layer makes hour one's magazines on the HQ lot, while claimed interior blocks still get the block-level "Build assembler" at 20/min with no footprint. **D-P4-5.**

## Lattice M3 Defence — built 2026-09-03

### Built

- **Defence and power in the flow layer** (`packages/sim/src/flow.ts`, run name `M3-rates`): **Gun turret** 2×2 with a 50-round hopper, fed by an inserter from a belt of magazines (ten rounds a magazine, the inserter waits holding one while the hopper is full), by hand from the Depot buffer (`handFeed`, instant), or by the C10 first fill; it fires at 5 rounds/s at the block sim's engagements on the street it faces (the nearest one), a removed turret hands its rounds back to the line buffer, and the ring's edge on the HQ is the sum of its turrets' hoppers. **Generator** 2×2, 300 kW on 4 MJ coal, 50-coal hopper, 40 coal at the start (the §11 opening), fed by inserter, belt or hand; it burns by load (0.075 coal/s at full load: 40 coal is 533 s), shared equally among burning Generators. **Power as one number**: the block sim's §14 model (`power: true, supply 'generators', draw 'half', shed 'machines-first'`) now takes its supply from the Generators through `TileHooks` (`supplyKw`, `demandKw`, `shedOne`, `restoreOne`, `setLoad`), demand is every placed machine's rated kW (Excavator 60, assembler 100, inserter 10, Lamp 5) plus the substations' 100/20, and the shed order runs through the tile machines in §14's order (Shot assembler, other machines, Lamps, Excavators, the coal Excavator and the Generator-feed inserter last), then the substations; a shed machine stands with a red cross and comes back last-shed-first when the supply allows. A grid with no Generator burning is dead: everything stops and a substation shed on it comes back 20 s after the supply returns. **Lamp** 1×1, 5 kW, radius 4, lit while its cell is powered. **Pole** 1×1, reach 8 from any pole or a claimed substation; a connected run that reaches a Dark neighbour's substation raises the claim through the block sim's `claim` command (the map still pays), and a map claim strings its own poles along the street to the new substation. **Substation** 3×3 on every lot (`tiles.ts`, pre-existing, the HQ's at lot (18,3)), powered while its block is Held or Contested, the grid alive and the block sim has not shed it; its footprint refuses machines. **Streetlights** eight a side every three tiles on the street margin, three in eight broken by hash, lit when the substation powers; `cellLights` and `litAt` are the light model M5 draws from. Events `hopper-empty` (same tick the edge's pip turns red), `gen-dry`, `brownout`, `shed`, `restore`. Block-only snapshots load with the M3 fields; the harness never calls `ensureFlow`, so E1–E9 are byte-identical and `snapshot:check` still gives `ee23bb1c`.
- **World view** (`worldScene.ts`, `main.ts`, `panel.ts`, `mapScene.ts`): tools T turret, L Lamp, P pole, G Generator (ghosts show a Lamp's 4-tile and a pole's 8-tile reach); left-click with the hand feeds a turret or Generator from the Depot; turrets drawn with a barrel along their facing, a half-second muzzle flash, a green/amber/red hopper bar and a blinking red outline when empty; Generators with a chimney glow, a square per five coal and a burn bar; poles with wires to what they hang from; Lamps and streetlights as discs of light, the broken ones crossed; the substation slab lime when on, red when off, navy when Dark; a shed machine crossed out. The HUD carries a power line (load / supply, demand, NO POWER or BROWNOUT with the shed count, Generators burning and coal, lamps lit, brownout seconds); the panel gains Power, Generators, Turret rounds, Lamps and poles, Brownout rows; toasts for a hopper going empty (with the side), a Generator running dry, a brownout, each shed and restore, and a pole claim. The map view's pip pulses red on `hopper-empty`.
- **Telemetry:** per-minute hopper rounds and capacity, ammo on the belts, lamps lit and built, poles connected, brownout seconds, Generators built and burning, their coal, supply / demand / load kW, machines shed, turret rounds fired, coal burned; the summary carries brownout seconds, rounds fired, coal burned and hand feeds.
- **§18 lot sketch from the world view:** `renderLot` (one character a tile, the whole 32×32 HQ cell) and the docsync generator `section18lot` replace the hand sketch: the game's M3 session config on seed 3, §11's first ten minutes placed by hand at the doc's stock, drawn at 10 minutes; CI-checked like the map drawings.
- **Tests:** nine in `packages/sim/test/defence.test.ts` (`M3-rates`): the HQ start and the C10 fill; feeding by inserter and hand; the engagement drain and the `hopper-empty` tick; the Generator's burn, running dry and the dead grid; the §14 shed order and restore; lamps and streetlights; poles and pole claims; the pre-M3 save upgrade and the substation footprint; the bot's hands. `npm test`, `snapshot:check`, `docsync:check`, `experiments` 9/9, lint, typecheck and the game build are green (below).

### Assumed (every `GAME-ASSUMPTION` in M3 code)

| # | Where | Assumption | Resolves at |
|---|---|---|---|
| GA-M3-1 | `flow.ts` `ensureFlow` | The HQ starts with two turrets on each of its three street sides (six): §5 prices two a side and the block sim's 100-round edge hopper is two turrets' worth; the north pair where the §18 sketch drew them (lot columns 3 and 16), each facing its street. §11 says "two Gun turrets on the north edge" and then hand-feeds the west and east ones. | D-P4-8 |
| GA-M3-2 | `flow.ts` `ensureFlow` | The start's 40 coal is in the Generator's hopper, not the Depot (§18's "coal by hand, 40 left"); the Generator stands at lot (19,8). | D-P4-7; M6 |
| GA-M3-3 | `flow.ts` `prefillTurrets` | The turrets start stocked the way the block sim's first ring fill stocks its edges (C10: 20 magazines round the ring in order, two edges full and the third empty); a block-only snapshot's HQ hoppers pour into its turrets the same way. | never (C10 is a Gate A lock) |
| GA-M3-4 | `flow.ts` `turretEdge`, `hookSyncEdges` | A turret covers one street, the nearest; deeper in the lot than its range it covers nothing. On the HQ an edge fires only through physical turrets; every other Held block keeps the block-level stand-in hopper. | D-P4-9; M6 with D-P4-5 |
| GA-M3-5 | `flow.ts` `hookDrainEdges` | An engagement drains an edge's turrets fullest-first at 5 rounds/s each; a rush past the rate goes unfed exactly as the block sim's did. | M4 (enemies as things on tiles; range 9) |
| GA-M3-6 | `flow.ts` `MACHINE_KW` | A placed machine draws its rated kW whether busy or idle (§13 has no idle draw); belts, turrets, poles and Generators draw nothing. | Phase 5 |
| GA-M3-7 | `flow.ts` `GENERATOR_COAL_CAP`, `TURRET_FLASH_S` | A Generator holds 50 coal; a muzzle flash lasts half a second. | Phase 12 |
| GA-M3-8 | `flow.ts` `SHED_RANK`, `shedRank` | "Excavators feeding Generators" = Excavators digging coal, and the inserter that puts coal into a Generator sheds with them (shedding it first starved the second Generator in the §18 drawing); Lamps shed as other machines. Ties shed the newest first. | Phase 5 (recipe lines) |
| GA-M3-9 | `flow.ts` `subPowered` | A grid with no Generator burning is dead, not browned out: everything on it stops at once. | D-P4-7 |
| GA-M3-10 | `flow.ts` `tickGenerator` | Burning Generators share the load equally; none burns while idle. | Phase 5 |
| GA-M3-11 | `flow.ts` `placeable` | Turrets and Lamps may stand on the street margin too (§14 keeps room for "a lamp line"); a pole goes on any street tile of the city and on any lot that is not Inert, Dark included, since a pole run is how a claim is strung; Generators stay on the lot. | Phase 5 |
| GA-M3-12 | `flow.ts` `poleGrid` | A pole hangs from any pole or claimed substation within reach 8 (centre to centre, or to the substation's nearest edge). The block map stays the judge of power: poles are how a claim is strung and shown; a claimed block keeps its power whether or not its poles still stand. | M6 (Electricians' Big pole) |
| GA-M3-13 | `flow.ts` `layPoles` | A map claim's 10 wire + 5 frames already paid for the poles it strings; a run that finds no room stops short and the map still powers the block. | M6 |
| GA-M3-14 | `flow.ts` `handFeed` | Hand-feeding is instant: §11's "hand-fed the west and east turrets twice each" is two clicks. | Phase 5 (Depot rules) |
| GA-M3-15 | `flow.ts` `MACHINE_COST` | Prices (§13 gives none): Gun turret 15 steel + 5 Cu, Lamp and pole 1 steel + 1 Cu, Generator 30 steel + 10 Cu. | D-P4-4 |
| GA-M3-16 | `tiles.ts` `substationLot` | Every lot has a pre-existing 3×3 substation one or two tiles in from a street side at a seeded spot (a street pole run reaches it at reach 8); the HQ's is fixed at lot (18,3). Its footprint never carries rubble. | M6 (craftable Substation, outskirts) |
| GA-M3-17 | `tiles.ts` `streetlights` | Eight streetlights a side every three tiles, each broken with probability 3/8 (a side averages §5's "3 Lamps to plug broken streetlights"); they draw nothing of their own and light when the substation powers. | M5 (light map) |
| GA-M3-18 | `session.ts` | With the flow layer the §14 power model is on: supply from the Generators, the D1 half draw (100/20 kW), machines-first shedding; one 300 kW Generator on 40 coal is the whole grid at the start. `?flow=0` changes nothing. | D-P4-7 |
| GA-M3-19 | `queries.ts` `pipOf` (M1, now read by turrets) | The pip rule is applied per edge to that edge's turrets as a set: green ≥ 50 % full, amber 10–50 %, red = at least one empty. | never (Gate A) |
| GA-M3-20 | `worldScene.ts` | Flat code-drawn shapes for turrets, Lamps, poles, Generators, streetlights, light discs and the substation slab until the art pass. | Phase 12 |
| GA-M3-21 | `flow.ts` `botHands`, `session.ts` | The autoplay bot has hands: each frame it hand-feeds every turret and Generator at or under half from the Depot, as §11's player does; it never builds a line, so a soak or E-run that wants the hour places one. Without hands the first M3 soak lost the HQ at minute 13. | M6 (the bot is a dev aid; Gate B is played by hand) |

GA-M2-3 (inserter targets), GA-M2-7 (no power draw) and GA-M2-10's "turret strip" are closed by M3.

### Deferred

Enemies as things on tiles, turret range 9 as geometry, the hulk and the Barricade → M4; the light map and the shade rule at tile level → M5; the Electricians' unlocks (Floodlight, Big pole, craftable Substation) and the outskirts' missing substation → M6; the Gunsmith's direct belt input to a hopper → Phase 5/6; the ring feed order as a belt-layout fact → M6 with the played hour; turrets on claimed blocks (physical or stand-in) → D-P4-9 / M6 with D-P4-5; hour-one power (one Generator, 40 coal, the minute-0 brownout of §11's own line) → D-P4-7; six start turrets vs §11's two → D-P4-8. All in `DEFERRED.md` with a phase.

### Measured

- **Rates at 20 ticks/s (defence tests, exact):** an inserter fills a turret from a belt of magazines to exactly 50 rounds and waits holding the next; hand-feeding moves 2 or 5 magazines at once and refuses a full hopper; a removed turret's 50 rounds return to the line buffer. The Generator supplies 300 kW, burns 0.075 coal/s at full load and runs the 40 start coal dry in 533 s (± 30), after which the grid reads 0 kW, the assembler says "no power" and the substation is shed; hand-feeding coal brings the supply back and the substation 20 s later. The shed order under a 90 % shortfall is assembler, inserter, Lamp, steel Excavator, Generator-feed inserter, coal Excavator, then the substation; with the shortfall lifted they return substation, coal Excavator, feed inserter, steel Excavator, Lamp, inserter, and the assembler only once an Excavator is removed. Streetlights: 32 a cell, three in eight broken over the seed, all lit while powered and none when the block goes; a Lamp lights a dark tile 5 wide. Poles: a run of reach-8 poles from the HQ substation reaches the north neighbour's substation and raises the claim, which the block map accepts; a claim made on the map strings its own poles.
- **The HQ at 0:00 (defence test 1 and the browser):** six turrets, 200 of 300 rounds (the C10 fill: north and west full, east empty, its pip red from the first tick), one Generator with 40 coal, power 100 / 300 kW, the substation on, 17 of 32 streetlights lit.
- **§18 at 10 minutes, from the world view** (`renderLot`, seed 3, the game's M3 config, §11's line placed by hand at the doc's 200 steel / 100 Cu): the line as §11 describes it draws 420 kW at 0:00 (the substation's 100 + three Excavators 180 + the assembler 100 + four inserters 40) against one 300 kW Generator, so the assembler and two inserters are shed at once and the grid reads a brownout for 120 s in all; the second Generator at minute 6 with the coal Excavator and its belt brings the supply to 600 kW, restores everything, and by 10:00 the line has made 17 magazines with 10 on the belt to the north-west turret, the hoppers hold 67 of 300 rounds after 193 fired, both Generators burn on 100 coal (48 burned), and 45 steel / 64 Cu remain. §11's "one Generator alone browns out at minute 8" is minute 0 at tile level with the whole line placed at once (D-P4-7). At the sim's 80 / 40 start (D-P4-4) the same line costs 156 steel + 36 Cu and cannot be placed.
- **Render loop, 1 h at 4× in the world view with the M3 HQ running** (seed 3, `?autoplay=compact` claiming as the bot does, the camera roaming the city and cycling 0.5–3× every 5 s, sampler on `requestAnimationFrame`):
  | Measure | Value |
  |---|---|
  | Real time | 900 s (899.8) |
  | Sim time reached | 1:00:00 from 0:00:33 (the line laid while paused) |
  | Frames (RAF sampler) | 53,969 |
  | Mean frame | 16.67 ms (60 fps) |
  | Worst frame | 33.4 ms |
  | Frames over 50 / 100 ms | 0 / 0 |
  | Camera moves | 180 (a random Held block every 5 s, zoom cycling 0.5/1/2/3/1.5×) |
  | Fixture | §18's line with its magazines into the Depot, both Generators and the coal feed at 0:00; stock set to 400 steel / 200 Cu for it (the sim's 80/40 cannot buy it, D-P4-4/7); the bot's hands feed the turrets (GA-M3-21) |
  | Magazines made / delivered to the Depot | 536 / 536 (20 a minute from minute 6; the Depot buffer 2,991 of 4,000 at the hour) |
  | Rounds fired by the six turrets | 1,077; hoppers never below 169 of 300, no red pip after minute 1; 110 magazines hand-fed by the bot |
  | Bot (compact) | 4 claims (3 Held, 1 Contested at the hour), 0 lost, 14 poles strung by the claims, all connected |
  | Power | 600 kW from two Generators from minute 6; 30 s brownout at the start (one Generator against 490 kW until the coal reached the second); from minute 30 the claims' substations push demand to 590–600 kW and the shed order runs through all nine line machines |
  | Coal | 500 burned; the Generators dry and the grid dead in the last minutes (below) |
  | Errors | none |

  The render loop was not dropped: no frame over 50 ms in the hour, the sim clock ran 3,600 s at 4× in 900 s of wall time, the camera moved 180 times. **Constitution check met.**

  **What the hour found.** A first soak with no line and no hands lost the HQ at minute 13: its 200 start rounds were fired and nothing refilled the turrets, since M3 makes the HQ's edges fire only through them (GA-M3-4) and the bot had no hands. That is the doc's unfed consequence at tile level, on time (§25 item 4), and it is why the bot got hands. With the line the HQ holds all hour, but the bot's third claim at minute 30 puts a fourth substation on two Generators (demand 590–600 against 600 kW) and the §14 shed order, running machines-first through the tile machines, reaches the Generator feed before any substation: the inserters that put coal into the Generators shed (rank 4, newest first), the Generators burn their 100 coal dry, and the grid dies before the hour is out. Shedding a 20 kW feed to close a 10 kW gap costs 600 kW. The rule as §14 writes it ("Excavators feeding Generators last") is self-defeating at tile level; **D-P4-7** now carries it.

### Where M3 and the doc disagree (reported, not resolved)

- **Turrets at the start:** §11 says two on the north edge; the sim starts six (two a side) because the ring has three edges and C10's ladder needs the third one empty. **D-P4-8.**
- **Hour-one power:** §11's opening line draws 420 kW against one 300 kW Generator from the first tick (the doc's "browns out at minute 8" came from the block model's ramp), and the second Generator §11 places at minute 6 is the fix; at the sim's 80 / 40 start the line plus that Generator (156 steel) is out of reach. **D-P4-7**, with D-P4-4.
- **Turrets everywhere or on the HQ:** the doc's ring is physical turrets on every front edge; M3 builds them on the HQ lot and keeps the block-level 100-round hopper elsewhere, as M2 kept the block-level assembler. **D-P4-9.**
- **The shed order's Generator feed:** §14 names "Excavators feeding Generators last"; the inserter on that line has to shed with them or the Generator starves (the §18 run found it). §14 edited with the tag; a rule, not a constant. The soak then showed that "last" is still too soon: once a brownout reaches the feed, the Generators dry and everything is lost; the feed should be exempt, like belts and turrets. **D-P4-7.**
