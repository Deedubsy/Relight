# Relight — Phase 4 slice report (prompt B, §11 minutes 0–60)

Living document, rewritten from the top at each prompt B milestone (D5/D6 rework, 2026-09-04): the current milestones first, the §19 first-hour test at the top, Gate B before the appendix. The lattice slice's M1–M3 (2026-09-03) stay at the end as the record of what was built and measured before the rework; Gate B is not scored on them.

## §19 first-hour test (human play) — not scored yet

Scored by a human after prompt B M6, from a played hour. Nothing here is filled in by the bot.

| Window | New unlock | Current problem | Memorable moment |
|---|---|---|---|
| 0–10 min | — | — | — |
| 10–30 min | — | — | — |
| 30–60 min | — | — | — |

Did the burn-off make you say something? —

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


## Prompt B M5 Light — built 2026-09-04 (unverified)

Run name `B-M5-light` (the light map as data, the streetlight sequence, the burn-off, repair; `light.test.ts`). **Unverified**: built under the working mode of 2026-09-04 — no scripted check, experiment, calibration or soak has run on it. The numbers below are what the code says, not what a run measured, until the verification pass.

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

### Measured — *unverified*

No run has measured M5. What the code says, to be confirmed by the verification pass:

| What | From the code | Doc |
|---|---|---|
| the lit kerb of a powered face | every 4th kerb tile carries a radius-4 light, so a face's street tiles are lit end to end but for the 3-in-8 broken gaps; the lot is lit only under Lamps and the Floodlight's cone | §4, §5 "streetlights come on" |
| a claimed block's lights | sound streetlights on at 3/s from the substation out; a 12-light face is fully lit after 4 s | §6 three per second |
| the burn-off | 20 + 60·d s: 38 s at d 0.3, 80 s at d 1.0; the specks clear within 16·p tiles of each lit lamp | §5 step 4 |
| a repair | 1 Cu; the light is lit again at once (a streetlight while its face is powered, a Lamp while its block is claimed and powered) | — |

The verification pass should run and record: `npm test` (the four `light.test.ts` cases), `typecheck` (the game build: the canvas texture, `MULTIPLY`), `lint`, `snapshot:check` (expected unchanged — `repaired` / `repairs` are optional and created on first repair; `burnOffS` is the number the claim already used), `docsync:check`, `experiments` and `calibrate` (expected identical for the same reason), then measure: the lit kerb and lot fractions per face on seeds 3 / 4 / 5 (`litCount`), the sequence and sweep timings at d 0.3 / 1.0, a shade on a lit tile taking rounds, the 900 s soak's frame time with the light map, and the Playwright check at `?view=world&seed=3` (the multiply visible, lit streets, dark lots, a repair by E, `?handlamp=1`).

### Where prompt B M5 and the doc disagree (reported, not resolved)

- **The prompt's "burn-off sweeps light along the claimed face's ridges over 20 + 60·d s"** vs §6's "streetlights come on in sequence down the street at three per second": built to §6 (the lights are on in 4 s; the rot's specks sweep over the 20 + 60·d s). Decision D-B5-2.
- **§4 "desaturated and darkened to ~25 %"**: a multiply darkens and tints but cannot desaturate; the cool cast stands in (GA-B5-1). Decision D-B5-3 carries the unlit level.
- **The prompt's "streets and lamp radii lit, lots unlit"**: read as the kerb streetlights' radii covering the street; a face's broken 3-in-8 leaves gaps on the street until repaired. Not a disagreement, a reading — stated so the human can say otherwise.
- **§5 step 3 "the rot begins burning off from the lamps outward"**: built as drawing (the specks), the rot number itself is still the block's `d` falling by the block sim's rule; the doc's sentence is about the picture and stands.

### Decisions for the human (recommended in `DECISIONS.md` D-B5-1–D-B5-3)

- **D-B5-1 the hand lamp**: none — the engineer carries no light, the dark is the dark (a) / a 2-tile lamp on the sprite, drawing only (b) / a 2-tile lamp that counts as light for the shade rule and the rot (c). Recommend (a); the preview (b) is built behind `?handlamp=1`.
- **D-B5-2 the sequence's pace**: §6's three per second, the sweep of the specks carrying the 20 + 60·d s (a) / the prompt's spread — the face's lights come on evenly over the burn-off (b) / three per second for the lights, and the Lamps on the lot come on at the end (c). Recommend (a).
- **D-B5-3 the repair cost and the unlit level**: 1 Cu a repair and the unlit multiply at ≈ 25 % with a cool cast, as built (a) / 2 Cu and 20 % (darker, the dark reads harder) (b) / repairs free, 35 % (c). Recommend (a) until M6's played hour says the copper runs short.

## Prompt B M6 The hour — not built

## Gate B

verdict:

---

# Appendix — the lattice slice (superseded by the D5/D6 rework, 2026-09-04)

Not scored at Gate B. Kept as the record of what the lattice slice built and measured; its GAME-ASSUMPTIONs that survive the rework are still in the code and listed in `REWORK_REPORT.md` §3.

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
