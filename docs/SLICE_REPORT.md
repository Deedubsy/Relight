# Relight — Phase 4 slice report (§11, minutes 0–60)

Living document: one section per milestone, appended as each is built. Gate B is the last line of this file.

## §19 first-hour test (human play) — not yet scored

Scored by a human after M6, from a played hour. Nothing here is filled in by the bot.

| Window | New unlock | Current problem | Memorable moment |
|---|---|---|---|
| 0–10 min | — | — | — |
| 10–30 min | — | — | — |
| 30–60 min | — | — | — |

Did the burn-off make you say something? —

## M1 Ground — built 2026-09-03

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

## M2 Flow — built 2026-09-03

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

## M3 Defence — not built

## M4 Threat — not built

## M5 Light — not built

## M6 The hour — not built

## Gate B

verdict:
