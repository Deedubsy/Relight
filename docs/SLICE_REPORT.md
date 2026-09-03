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

## M3 Defence — built 2026-09-03

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

## M4 Threat — not built

## M5 Light — not built

## M6 The hour — not built

## Gate B

verdict:
