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

## Prompt B M2 Flow on the faces — not built

## Prompt B M3 Defence — not built

## Prompt B M4 Threat — not built

## Prompt B M5 Light — not built

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
