# Relight — programme state

Living file. One entry per phase, newest first; the current phase is the top one. Rules are in the programme constitution (the prompt that started Phase 0); the spec is `RELIGHT-design.md`; the sim is the judge. `ROADMAP.md` is the one-page summary of this file: the fourteen phases, which are done, and what each of the rest still owes.

**Current phase: 4, reopened by the rework (D5 the engineer on foot, D6 the street-first city; `REWORK_REPORT.md`, 2026-09-04). The lattice slice M1–M3 (`SLICE_REPORT.md`) is not scored at Gate B; the slice is being rebuilt from `docs/relight-prompt-B-vertical-slice.md`: D-R1/D-R2/D-R3 taken by recommendation on the human's `go` (2026-09-04), prompt B M1 "Ground and the engineer" built the same day. Next: on `go`, prompt B M2 "Flow on the faces".** D-P3-9/10/11 were made by their recommendations on that `Go` (sessions in parallel, C3/C4 as locked, C8 lock accepted). PR #1 (`phase-1` → `main`), PR #2 (`phase-2` → `phase-1`) and PR #3 (`phase-3` → `phase-2`) are open, unmerged; Phase 4 is PR #4 (`phase-4` → `phase-3`). Phase 1's human five-minute smoke test and the Gate A sessions are still the user's.

Gates passed: **Gate A, 2026-09-03, `verdict: go` by the owner on the bot calibration and the smoke test, with no tester sessions** (`TEST_RESULTS.md` §1). Every constant locked on it is tagged `[play: Gate A]`, a lock rather than a measurement; the sessions can still run (D-P3-9).

Housekeeping the constitution assumes and the repo does not have (a human decides how, not whether):

- ~~`/mnt/e/Factorio2` is not a git repository and has no CI.~~ Done in Phase 1: `github.com/Deedubsy/Relight` (private), branch `phase-1` → PR #1 to `main` (CI green), `.github/workflows/ci.yml` and `nightly.yml`; `main` protection is refused on a free-plan private repo (D-CI, human choice). **Decided (D-CI, 2026-09-03):** GitHub private repo, GitHub Actions, npm workspaces; push CI = lint + `tsc --strict` + fixtures + E1–E9 at three seeds; nightly = 10,000-seed and 25 h runs to `docs/experiments/`; Python sim stays as fixture exporter for one phase, then retired (**done in Phase 3**, D-P3-8). Linear for tasks. Phase 1's first task is to set this up.
- Reports live at the repo root, not in `docs/`. Left where they are (Phase 0 changes nothing else); Phase 1 may move them under `docs/` and leave root stubs.
- `packages/harness` and `packages/tools` exist since Phase 1. `packages/proto` (the Phase 2 map view) was renamed `packages/game` in Phase 4 M1 and holds both views; `apps/steam` does not exist yet (Phase 14).

## Prompt B — the slice rebuilt on the city (Phase 4 reopened, opened 2026-09-04)

**Status: M1 Ground and the engineer built 2026-09-04; M2–M6 not started.** Branch `phase-4`, PR #4. Report `SLICE_REPORT.md` (rewritten from the top; the lattice slice is its appendix). Decisions D-B1-1 … D-B1-5 taken by recommendation on the human's `go` for M1 (`DECISIONS.md`); D-P4-7/8/9 and D-B1-4/5 open.

### B.1 M1 Ground and the engineer — what changed

- **`packages/sim/src/ground.ts`** (new, run name `B-M1-ground`) — the tile layer as a pure derivation of `CityGeom`: rasterised faces as lots, 6–12-wide street ridges, the river, plazas and parks inert; 800×800 tiles in 625 chunks with `blockKey` / `chunkKey` for the renderer; rubble by district in three clusters, 250–350 tiles scaled by area; substations and streetlights placed by the face geometry (GA-B1-1/2). 42–117 ms cold a city, 0 ms cached. `tiles.ts` (the lattice layer) stays as the HQ layout's source and behind `?map=lattice`.
- **`packages/sim/src/walk.ts`** (new, run name `B-M1-walk`, six tests) — tile A* over `passable` (belts and poles walked through, GA-B1-3), `tickEngineerTiles` at 6 tiles/s with `engineer.block` kept in step so D5's kits, restock and rifle fire unchanged; `walkTo(block)` and `move(x, y)`.
- **`packages/sim/src/engineer.ts`, `flow.ts`** — pockets of 40 stacks (stack sizes GA-B1-4), the Depot as the 6×6 chest and workbench (`chestTake` / `chestPut` / `chestCount` / `nearDepot`), hand-mining to the pockets within reach 8, §11's start chest 200/100/50/20 in the game and the calibrated 80/40/0 in the harness (GA-B1-7); kits free from the chest, one a claim (GA-B1-5/9).
- **Two block-sim fixes found by the first city soak** (`SLICE_REPORT.md` Built): `flow.ts` `hookSyncEdges` no longer makes every HQ edge a turret edge — a city HQ segment the six start turrets do not cover keeps the ring-fed stand-in hopper (GA-B1-14; the HQ was lost at sim minute 20 with the line running); `sim.ts` `rebuildRing` starts the HQ's own edges kitted at t = 0 with `walk` on (GA-B1-15; a human with no bot fired nothing). Test added in `defence.test.ts`. The second fix moves the D6 regression fixtures `city4.json` / `city5.json` (seed 4: held 50 → 52, lost 2 → 0 over five hours; seed 5: walking minutes only) — a rules change under constitution rule 1, regenerated with `_exportCity.ts`; `city3.json`, the snapshot `5f3417b9` and the calibration (`calibration.md`, re-run) do not move; `E-walk`'s hour-3 walking minutes drop to 2.2 (check 0–10).
- **`packages/game`** — `worldScene.ts` chunked Blitter over the ground, the engineer with reach ring and path line, WASD and click-to-walk, camera following from minute 0, reach-refused tools with "walk closer"; `cityMapScene.ts` click-to-claim-and-walk, the engineer's block marked; `panel.ts` pockets on **I**, chest take/put; `main.ts` / `session.ts` the flow layer on for every session unless `?flow=0` (GA-B1-13); **M** toggles the views, **E** toggles nothing; `?walk=1` and the three stubs gone; `__relight` gains `walkTo(x, y)`, `engineer()`, `ground()`, `describe()`, `chest`, `togglePockets()`, `world.drawMs()`. `bots.ts` `walkingBot` unchanged at block level; the game drives the same `walkTo`.
- **Measured** — HQ → Tram depot → HQ on foot (20 s sim a leg, no stutter, no frame over 50 ms); the 1 h at 4× soak on seed 3 with the §11 line (`soak.cjs`, headless Chromium, swiftshader): 901.6 s real, sim 1:30:05, 39,378 frames, 43.7 fps, worst 48.3 ms, 0 frames over 50 ms, held 4 / lost 0 at the hour, one claim lost at 1:29:42 with the coal at 0. The DoD's 60 fps is not met on this host: 91.7 % of profiler samples are the software renderer, JS ≈ 1.6 ms a frame; the lattice M3's 60 fps flat (2026-09-03) is the baseline a GPU host has to match. Reported as measured, not waived.
- **Doc**: §14 passable belts and poles and §4 the key bindings, changelog lines `B-M1-walk`; the five disagreements in `SLICE_REPORT.md` (chest vs calibration start, passability, kits' price, substations by geometry, bindings).
- **Checks**: `npm test` 95/95, typecheck (incl. the game build), lint, `snapshot:check` (`5f3417b9`), `docsync:check`, `experiments` 12 / 0 failing — green.
- **Deferred** re-read (`DEFERRED.md` "Re-read at prompt B M1"): the lattice tile layer and `?map=lattice` → Phase 5 gate; sprite and tileset art → Phase 12; hand-crafting from the pockets → M2; kits by hand and the walk-back tedium → M6; the truck as a vehicle → Phase 5; start turrets on segments → M3 (D-P4-8); A* vs graph distance → Phase 11.

### B.2 Untagged recount at prompt B M1

33 → **33**. M1 added no `[sim]` tag to an untagged number: the ground and the walk are tagged where the rework already tagged them (`rework-graph`, `E-walk`); §14's passability line is new text, not one of the 33. §26 status unchanged: three systems, 5 of 10 gate criteria measurable. C1/C2 still read **MISSED** in `calibration.md` after D-R2's rewrite to kitted edges (first amber on a kitted edge 31 / 16 / 16 min on seeds 3/4/5, first enclosure 60 / 46 / 45 min): the first amber still lands at the claim minute although §25's D-R2 line has an edge kitted this second filled this second, so the cause is not the kit walk alone. Not chased in M1 and unchanged by its fixes; reported, not resolved — D-R2 is reopenable at Gate B and prompt B M3 (turrets on segments) is where the claim's first fill is decided.

## Rework — the engineer and the street-first city (D5, D6; applied 2026-09-03 → 2026-09-04)

**Status: the six steps of the rework brief are done; report `REWORK_REPORT.md`; slice prompt `docs/relight-prompt-B-vertical-slice.md`; three decisions (D-R1 rifle and shade, D-R2 kit walk and C1/C2, D-R3 HQ copper) taken by their recommendations on the human's `go` for prompt B M1 (2026-09-04; rows in `DECISIONS.md`), and a fourth taken by recommendation because it was holding the build red (D-R4, the generator's attempt budget).** Branch `phase-4`, on top of M3. Neither stop condition fired (§26 stays three; E-rifle's damage number is 10 HP a kill, 62 HP a rescue).

- **Sim** (`packages/sim`): the block sim runs on a street graph (`city/` generator with five presets, `graph.ts`; adjacency = shared segment ≥ 5 tiles, 3–7 neighbours, districts as hop bands, validator on the graph; slots and rubble pool by area); the engineer, kits, the truck, the rifle and retaliation at block level (`engineer.ts`, `bots.ts` walking bot). The lattice stays behind `--map lattice` with its fixtures under `fixtures/lattice/`; city fixtures `city3/4/5.json`; 88 tests. The block-only HQ start patch gained copper (3,840 at 32/min; the city HQ is civic) — GA-R15, D-R3.
- **Harness**: E1–E9 retagged on the River city; E-rifle, E-walk, E-variance added; calibration re-run with walking bots (`docs/experiments/calibration.md`) and without (`calibration-nowalk.md`) to separate the graph from the walk; lattice results archived under `docs/experiments/lattice/`. What moved: enclosure earlier (45–60 min), first amber/red at the claim minute (16–31 min) from the kit walk, spike's first red 0 → 66–69 min (no walk) / 16–31 (walk), cheapest's ammo ratio 0.96–2.07; C1/C2/C3/C7 missed with walking, C1/C2 met without. E-rifle: the bot never fires in steady play; the ring rescue falls to a shade either way. E-walk: under a minute walked in hour one, 2–4 min/h by hour three, truck at 91–146 min.
- **Game** (`packages/game`): polygon map view (`cityMapScene.ts`) is the default; side panel cut to held / front / interior, made vs demanded, stock, lost; the rest behind the backquote key; **M** toggles map ↔ world; `?map=lattice` keeps the old map. Three stubs until slice M1: walking only under a bot, the tile flow layer off on a city, the world view lattice-only (all three removed by prompt B M1, 2026-09-04).
- **Doc**: §3–§5, §7–§14, §17–§19, §22–§26 edited with changelog lines; §18's three examples rendered as `docs/section18-*.png` (`npm run section18`); tags `[sim: rework-graph]`, `[sim: E-rifle]`, `[sim: E-variance]`, `[sim: E-walk]`, `[play: Gate B]` in place. D5/D6 in `DECISIONS.md` before any other edit. **A second doc pass followed the `--big` run** and moved a whole class of numbers the first pass had carried over from the lattice: the shape ratios (§7, §9, §17, §19, §24, §25, §27 — spike 2.4× compact, quiet-block play 1.27×, the inert-wall claim rewritten as frontage rather than free walls), the unfed→fall delay and the fed ring (§5, §19, §24), the turtle's 963 magazines (§19), the wells (§5, 4–6 a map, first claimed at 7.6 h), §17's validator list as `validate()` actually checks it, the 25-hour costs and the Relight window (§6, §12, §16, §25 — the eight-assembler line never runs short on the city, so D4 keeps the design and loses its sim evidence, `DEFERRED.md` → Phase 10 E20), and §24's technical risk 4 (the generator's rejection rate and the raised attempt budget). Seven more changelog lines, each with its run name.
- **Checks**: `npm test` 88/88, typecheck (incl. the game build), lint, `snapshot` regenerated (config `5f3417b9`) and `snapshot:check`, `experiments --big`, `docsync:check` green. **The first `--big` run was red**: `E-variance` found one seed in 10,000 (3767) with no valid city after eight jitter streams ("no well site: west riverside"). The validator rejects 26 % of streams, so eight of them leave ~1 seed in 48,000 with no city and the check was flaky by construction. Fixed by D-R4, `CITY_ATTEMPTS` 8 → 16, which changes no seed that already validates (seed 3767 validates at attempt index 8; the snapshot fixture and every measured number are unmoved); `E-variance` now names the offending seeds in its detail line. The rerun is green: 12 experiments, 3,181 s, 0 failing checks (E-variance 4/4, `0 invalid of 10000`), every other measured number identical, and `docsync:check` green after it.
- **Deferred** re-read at the rework (`DEFERRED.md`): the tile layer's port to faces, the sprite, the reach ring, the pockets, the rifle button, the light texture at 800×800 → prompt B M1–M5; the truck's driving → Phase 5; the Arsenal upgrade → Phase 6.

## Phase 4 — vertical slice (in progress, opened 2026-09-03)

**Status: M1 Ground, M2 Flow and M3 Defence built; M4–M6 not started.** Branch `phase-4` on `phase-3`, PR #4. D-P4-1/2/3 made by recommendation on the `go` that opened M2, D-P4-4/5/6 by recommendation on the `Go` that opened M3; D-P4-7/8/9 open (`DECISIONS.md`). `packages/proto` → `packages/game` (`@relight/game`); the map view is untouched and its snapshot still verifies (`snapshot:check`, hash `ee23bb1c`). Canonical config unchanged (`01dc5d02`). Checks green locally (typecheck, `npm test` incl. six tile, ten flow and nine defence tests, lint, E1–E9 45/45, docsync, `snapshot:check`); PR #4's CI run is the proof. Report: `SLICE_REPORT.md` (per milestone; the §19 first-hour test sits at its top, empty until M6; Gate B is a human's `verdict: proceed` in it).

### 4.1 M1 Ground — what changed

- **`packages/sim/src/tiles.ts`** — the tile layer as a pure derivation of the block map: 32×32-tile cells, 24×24 lots, 4-tile margins (shared 8-wide streets), 768×768 city, row 23 river under a 4-tile embankment street; rubble 250–350 tiles per block typed by district (stone / copper / steel) in five density variants, laid in three clusters and denser on deeper blocks; outskirts carry a 160-tile iron or coal deposit patch on a quarter of blocks or nothing; standing rubble = tiles × pool ÷ pool max, dug thinnest-first; `cellKey` (state + rubble left) is what a renderer caches on; `describeTile` for tooltips. Six tests (`M1-tiles`): geometry, river and inert, counts and types, gradient, authority of block state, determinism and cost.
- **`packages/game/src/worldScene.ts`** — Phaser world view: a Blitter over a code-drawn 29-frame canvas tileset, per-cell cache keyed by `cellKey`, block-state overlay (Dark navy, Contested amber flicker, Held outline white interior / amber front), cell labels, HUD, drag / WASD pan, wheel zoom 0.5–3× about the pointer, tile tooltip, HQ slab. `view.ts` holds the mode and focus block; `main.ts` runs both scenes, steps the sim from the game's `STEP` event in either view, and **E** switches map ↔ world at the same block (map → world takes the hovered block; world → map takes the block under the camera centre and marks it for 2.5 s). `?view=world` opens in the world view. `window.__relight` gains `view`, `toggleView`, `world.{zoom,setZoom,centreOn,focus,drawn}`, `fps()`.
- **Bug found by the M1 soak, fixed:** `MapScene` took its session from Phaser's `init(data)`, which never runs for a scene added asleep (`?view=world`), so the first sim event (0:02:47 on seed 3) threw inside the game's `STEP` handler and killed the render loop. The session now comes in through the constructor, as the world scene's does; the soak was re-run after the fix.
- Doc: §4 world/map view lines edited to the built range and toggle (D-P4-1), §4/§14 geometry and rubble tagged `[sim: M1-tiles]`, §18 lot sketch re-routed to M3; two changelog lines.
- Constitution vs doc, reported not resolved: zoom 0.5–3× vs 1.0–0.2× (D-P4-1); §12 300 units per rubble tile vs the sim's 3,840 pool (D-P4-2); outskirts deposits by hash vs placed facilities (D-P4-3).

### 4.2 Untagged recount at M1

41 → **40**: §14.1 (street 8 wide, lot 24×24) carries `[sim: M1-tiles]`. §12.1's tile count (250–350) is now what the world draws, but its units per tile are not, so 12.1 stays untagged until Phase 5. 28 tile-scale + 12 design inputs.

| § | Phase 3 | Now untagged | Tagged in M1 |
|---|---|---|---|
| §5 | 12 | 12 | — |
| §7 | 3 | 3 | — |
| §12 | 7 | 7 | — (12.1 half: the tile count) |
| §13 | 16 | 16 | — (13.1 Excavator is M2) |
| §14 | 4 | 3 | 14.1 (`[sim: M1-tiles]`) |
| §15 | 1 | 1 | — |

### 4.3 §26 recount at M1

Three systems: the front, found tech, automated combat. M1 added a renderer and a derived data layer, no system, no mechanic the player holds in their head (the tiles are where the existing front rule is drawn). Complexity **5/10**, unchanged. §27 stays at 7.

### 4.4 M2 Flow — what changed (2026-09-03)

- **`packages/sim/src/flow.ts`** (run name `M2-rates`) — the tile flow layer: a fixed 20 ticks/s tile tick with the 1 s block tick derived from it (`advanceFlow` runs `step` every 20th tile tick; the harness never calls `ensureFlow`, so E1–E9 and the fixtures are byte-identical). Excavator 3×3 at 0.5/s onto the belt it faces; belts at 7.5/s (four items a tile, corners, side feeds, items visible); inserters at 1/s that pick what their target wants and wait holding it; the Mk1 Shot assembler (3 s, 2 steel + 1 Cu → 1 magazine, 20/min); the Depot as the global stock every placement draws from; hand-mining (a unit a second into the Depot) and hand-crafting (a magazine in 3 s from stock). Placement rules with costs and refunds; a machine on a block that stops being Held stands still. Ten tests.
- **`packages/sim/src/tiles.ts`** — the HQ lot: §11's steel, copper and coal patches as typed tiles carrying real units (the block sim's 7,680 steel over 25 tiles, 12 × 100 Cu, 700 coal over 9 tiles), the Depot footprint clear, the start lot cleared to 100 rubble tiles in its south strip. `sim.ts`: the flat HQ patch drain and the start lot's flat rubble yield are off when the flow layer exists.
- **`packages/game`** — world-view tools on keys (X/B/I/M/R/Q/C), ghost with the refusal reason, click or drag to place, right-click removes with a refund, hold-to-mine, machines drawn as flat shapes with their items; the panel's line section and hand-craft button; per-minute telemetry for the line (made, delivered, consumed, hand mined and crafted, machine counts, belt items); `?flow=0` for block-only sessions; `__relight.flow` and `world.key` hooks.
- Doc: §13 Excavator, Assembler (Shot rate), Belt and Inserter rows and §14's belt paragraph tagged `[sim: M2-rates]`; belt 8/16 → 7.5/15 (D-P4-6); §14 gains the hand-mining and hand-crafting sentence; two changelog lines.
- Constitution vs doc, reported not resolved: belt 7.5 vs 8 (D-P4-6); §11's 200/100/50 start against the calibrated 80/40/0 and unpriced machines (D-P4-4); the block-level assembler stand-in alongside the physical line (D-P4-5).

### 4.5 Untagged recount at M2

40 → **37**: §13.1 (Excavator 0.5/s), §13.2's rate (Shot 3 s, 20/min; its 100 kW waits for M3) and §13.10's belt and inserter rates carry `[sim: M2-rates]`. §13.10's splitter, underground and chest and §13.11 (Depot input 2×2: M2's Depot is the 6×6 facility, fed on any edge) stay untagged for Phase 5. §14.3 (hand-collecting one stack per 2 s) stays: M2 has hands but no chest. 25 tile-scale + 12 design inputs.

| § | M1 | Now untagged | Tagged in M2 |
|---|---|---|---|
| §5 | 12 | 12 | — |
| §7 | 3 | 3 | — |
| §12 | 7 | 7 | — (12.1's units per tile: the patches carry real units, ordinary rubble does not; D-P4-2) |
| §13 | 16 | 13 | 13.1, 13.2 (rate), 13.10 (belt and inserter rates) |
| §14 | 3 | 3 | — (14.3 needs a chest) |
| §15 | 1 | 1 | — |

### 4.6 §26 recount at M2

Three systems: the front, found tech, automated combat. M2 added the Factorio layer (belts, inserters, machines with footprints) that §26 already counts as "(1) belts/inserters/machines, which they already know", so no system is new; hands add no rule (a unit a second, a magazine in 3 s). Complexity **5/10**, unchanged. §27 stays at 7.

### 4.7 Milestone status at M2

- M1 Ground: **built** (4.1). M2 Flow: **built** (4.4). M3 Defence, M4 Threat, M5 Light, M6 The hour: not started.
- "Fixed 20 ticks/s at tile level, 1 s block ticks derived": `flow.ts` `advanceFlow`, flow test 7 (72,000 tile ticks and 3,600 block ticks for 1 h at 4×, identical across frame sizes).
- "A 1 h sim at 4× must not drop the render loop": `SLICE_REPORT.md` M1 and M2 measured (the M2 soak runs in the world view with a line working).
- "Map-view fixtures still pass; tile events drive the same block transitions": `snapshot:check` green (`ee23bb1c`), `npm test` 59/59; the block map stays the judge (flow test 9).
- "Measured rates equal doc rates": flow tests 2–6 and the browser line (`SLICE_REPORT.md` M2 measured).
- `DEFERRED.md` re-read at M2: every item has a phase; the machine, belt and tile-tick items are closed; prices, the second stand-in and two-lane belts added with phases.
- Decisions: D-P4-1/2/3 **made** by recommendation on the `go` that opened M2. Three for the human from M2: **open** — D-P4-4 start stock and machine prices, D-P4-5 the two assembler stand-ins, D-P4-6 belt 7.5 vs 8.

### 4.8 M3 Defence — what changed (2026-09-03)

- **`packages/sim/src/flow.ts`** (run name `M3-rates`) — defence and power in the flow layer: Gun turret 2×2 with a 50-round hopper fed by inserter, belt or hand, firing 5 rounds/s at the block sim's engagements on the street it faces; Generator 2×2, 300 kW on 4 MJ coal, 40 coal at the start, burning by load; Lamp 5 kW radius 4; pole reach 8 from a pole or a claimed substation, a connected run reaching a Dark substation raises the claim and a map claim strings its own poles. Power is the block sim's §14 model with its supply and demand from the tile layer through `TileHooks`, the shed order running through the tile machines (Shot assembler, other machines, coal Excavators and the Generator-feed inserter last) before the substations; a dead grid stops everything. Events `hopper-empty` (the tick the pip turns red), `gen-dry`, `brownout`, `shed`, `restore`. `renderLot` draws a cell one character a tile; `botHands` gives the autoplay bot §11's hand-feeding (a dev aid, GA-M3-21). Nine tests.
- **`packages/sim/src/tiles.ts`** — every lot's pre-existing 3×3 substation at a seeded street-side spot (the HQ's at lot (18,3)) and eight streetlights a side, three in eight broken; `cellLights` / `litAt` in flow.ts are the light model.
- **`packages/game`** — tools T/L/P/G, hand-feeding by click, turrets with flash, hopper bar and empty blink, Generators with chimney and coal, poles with wires, light discs, the substation slab, shed crosses; HUD power line; panel Power / Generators / Turret rounds / Lamps / Brownout rows; toasts for hopper-empty (with the side), gen-dry, brownout, shed, restore and pole claims; the map pip pulses red on `hopper-empty`; the session turns the §14 power model on with the flow layer (`supply 'generators'`, half draw, machines-first); per-minute telemetry for hoppers, belt ammo, lamps, poles, brownout seconds, Generators and coal, kW, shed machines, rounds fired.
- **`packages/tools/src/docsync.ts`** — generator `section18lot`: the §18 10-minute lot sketch from the world view (seed 3, the game's M3 config, §11's line placed by hand at the doc's stock), CI-checked; the hand sketch is gone.
- Doc: §13 Generator, Gun turret (rate and hopper), Lamp and Pole rows, §14's shed order and §5's fall paragraph tagged `[sim: M3-rates]`; §14 gains the Generator-feed inserter and the dead-grid sentence; §18 caption and sketch generated; three changelog lines.
- Constitution vs doc, reported not resolved: six start turrets vs §11's two (D-P4-8); §11's own opening line browns out at minute 0 on one 300 kW Generator at the half draw, and at the 80/40 start it cannot be bought (D-P4-7, with D-P4-4); physical turrets on the HQ only (D-P4-9).

### 4.9 Untagged recount at M3

37 → **33**: §13.3 (Generator 300 kW, 2×2), §13.5's rate and hopper (5 rounds/s, 50 rounds; range 9 is M4's geometry), §13.7's Lamp (5 kW, radius 4; the Floodlight is M6's unlock) and §13.9's pole (reach 8; the Big pole is M6's) carry `[sim: M3-rates]`. §5.8 (two turrets an edge, range 9) is placed but not measured until M4 brings enemies to tiles; §13.8 Barricade waits for the hulk (M4); §14.2 (the 220 kW line) is now what the tile machines draw but keeps its E2 tag. 21 tile-scale + 12 design inputs.

| § | M2 | Now untagged | Tagged in M3 |
|---|---|---|---|
| §5 | 12 | 12 | — (5.8's pair is placed; its range is M4) |
| §7 | 3 | 3 | — |
| §12 | 7 | 7 | — |
| §13 | 13 | 9 | 13.3, 13.5 (rate, hopper), 13.7 (Lamp), 13.9 (pole) |
| §14 | 3 | 3 | — (14.2 already carries E2-demand) |
| §15 | 1 | 1 | — |

### 4.10 §26 recount at M3

Three systems: the front, found tech, automated combat. M3 makes automated combat and the §14 power rule physical (turret hoppers, Generators, poles, the shed order); §26 already counts both, and the one new rule a player holds is "a Generator burns by load and the feed line sheds last", a Factorio rule they know. Complexity **5/10**, unchanged. §27 stays at 7.

### 4.11 Milestone status at M3

- M1 Ground: **built** (4.1). M2 Flow: **built** (4.4). M3 Defence: **built** (4.8). M4 Threat, M5 Light, M6 The hour: not started.
- "Turret (2×2, 50-round hopper, range 9, 5 rounds/s, fed by inserter), lamps and light radii, substation, poles, Generator on coal from the start patch, power as one number with the shed order, streetlights on when the substation powers": `flow.ts`, `tiles.ts`, defence tests 1–7; range 9 is geometry M4 needs enemies for.
- "An empty hopper turns the map view's pip red from the same event": defence test 3 (`hopper-empty` on the tick the pip turns red), `mapScene.ts` pulse.
- Telemetry (hopper levels, belt occupancy on the ammo loop, lamps lost, brownout seconds, coal): `telemetry.ts` per-minute record; lamps lost = built − lit.
- "Fixed 20 ticks/s tile tick; a 1 h sim at 4× must not drop the render loop": `SLICE_REPORT.md` M3 measured (roaming camera, the bot claiming).
- "Map-view fixtures still pass": `snapshot:check` green (`ee23bb1c`), `npm test` green.
- `DEFERRED.md` re-read at M3: every item has a phase; the substation, hopper, belt-to-hopper, Generator, power-draw and §18 sketch items are closed.
- Decisions: D-P4-4/5/6 **made** by recommendation on the `Go` that opened M3. Three for the human from M3: **open** — D-P4-7 hour-one power, D-P4-8 six start turrets, D-P4-9 turrets on claimed blocks.

---

## Phase 3 — absorb Gate A (2026-09-03)

**Status: built; Gate A passed on the owner's `go` without sessions; every constant the slice would otherwise encode by accident is locked and tagged `[play: Gate A]`; three decisions open for the human.** Canonical config unchanged (hash `01dc5d02`); proto hashes unchanged (`ee23bb1c` / `825d2d09`). Checks green locally (typecheck, `npm test`, lint, E1–E9, docsync, `snapshot:check`, calibration C1–C7 all met); PR #3's CI run is the proof. Report: `PHASE_3_REPORT.md`.

### 3.1 What changed

- **Gate A** — `TEST_RESULTS.md` `verdict: go` (2026-09-03, owner; §8 Decision column filled; §10 = D-P2-1/2/3 by recommendation). `DECISIONS.md` Gate A row made; C1, C2, C4, C8, C9 made, C3 and C5-steel routed; new Phase 3 table D-P3-1…D-P3-11.
- **Locks in the doc** (`[play: Gate A]`, 18 tags): C1 edges per assembler (§12: ~7 of the mid-game front, 15 civic / 11 residential), C2 cadence 15 min then 5 (§18, §25 item 10), C8 bloom timer and drop (§5, §25 item 1), C9 no Mk1/Mk2 ladder (§12), C10 20 magazines and the opening flicker (§11), the steel wall as intended (§19), survivors on Held (unchanged).
- **The four pre-slice decisions** — D-P3-1 draw 100/20 kW, hour-one lesson is ammo (§11, §25 item 14); D-P3-2 fall 90 s kept, the rescue is the minutes of red pip and the 30 s is the second chance (§5, §25 item 12); D-P3-3 the unfed rule as shipped, per block, 40 arrivals, 60 s all-fed clears and restarts (§5); D-P3-4 the Relight survivable by banking, the bank a ~20,000-magazine object (§16, §25 item 5).
- **§18 redrawn from the sim** — `renderMap` in `packages/sim/src/queries.ts`; `docsync.ts` generator `section18` (compact bot, seed 3, gap 300 s, production off, 0:10 / 5 h / 25 h) between `<!-- docsync:section18 -->` markers, CI-checked. Legend rewritten (facilities `F A U R P`, survivors `E N G K M`, upper/lower case by Held). Finding: **5 h is a river strip 14 × 4, not a blob**; the compact bot has no facility pull and reaches the Foundry at hour 14 on seed 3; at 25 h a fat blob with the whole west and far north never held (the §19 ignore test).
- **E8 retargeted** to the locked cadence's three-seed means (52/19/36 at 5 h, 292/43/253 at 25 h; new check that gap 5 is the best match); E1–E9 re-run, 45/45 checks, no canonical number moved.
- **Calibration scores C1/C2 from minute 1** (D-P2-1): columns `amber after 1 min` / `red after 1 min`; all seven targets met on three seeds.
- **Python retired** (D-P3-8): seven files and `__pycache__` deleted; `packages/sim/fixtures/README.md` freezes the fixtures at `52c4ca3`.
- Doc changelog: 11 lines, every one with a run name or `[play: Gate A]`.

### 3.2 Untagged recount

44 → **41** (three design inputs now carry `[play: Gate A]`: 5.2 bloom timer, 5.3 bloom drop, 12.6 assembler rate and edges). 29 tile-scale (Phases 4–9, unchanged) + 12 design inputs. Of the 12, C1/C2/C8/C9/C10 are locked in prose sentences whose table rows keep the old count discipline; the rest (claim cost, burn-off, district dmax/g, recipes, hopper, wells per map, Relight 40 MW, endgame hours) wait on a run that varies them or Phase 4's human hour.

| § | Phase 1 | Now untagged | Tagged in Phase 3 |
|---|---|---|---|
| §5 | 14 | 12 | 5.2, 5.3 (`[play: Gate A]`, lock) |
| §7 | 3 | 3 | — |
| §12 | 8 | 7 | 12.6 (`[play: Gate A]`, lock) |
| §13 | 16 | 16 | — |
| §14 | 4 | 4 | — |
| §15 | 1 | 1 | — |

### 3.3 Open constants after Phase 3

None the slice would encode by accident (the DoD). C1, C2, C4, C8, C9, C10 made; C3 routed (one line per block for the slice; footprint budget Phase 4 M1 / Phase 5); C5 steel routed to Phase 5; C6, C7 made in Phase 1. What remains open is the three human decisions: whether Gate A's sessions run in parallel with Phase 4 M1 (D-P3-9), C3/C4 for the slice (D-P3-10), and whether to port E10 before the slice encodes C8 (D-P3-11).

### 3.4 §26 recount at Phase 3

Three systems: the front, found tech, automated combat. Phase 3 added no system, no content, no mechanic; it locked numbers and generated drawings. Complexity **5/10**, unchanged. §27 stays at 7 with a note that the shape evidence Gate A was to supply is still outstanding.

### 3.5 Gate status

- Experiments green in CI: green locally (45/45); PR #3's run is the proof.
- No open constant the slice would encode by accident: **met** (3.3).
- Four pre-slice decisions as `DECISIONS.md` lines and §5/§11/§16 sentences: **done** (D-P3-1…D-P3-4).
- §18 redrawn from the compact bot at the locked cadence: **done**, generated and CI-checked.
- E1–E9 re-run at the locked values, retagged: **done**; nothing in the canonical config moved, so no `[sim]` tag changed its number.
- `DEFERRED.md` re-read: every item has a phase; Python bullet and §18 bullet closed; one item added (facility pull in the bots).
- Three decisions for the human: **open** — D-P3-9 sessions, D-P3-10 C3/C4, D-P3-11 E10 port.
- **Phase 4 begins only after a `go` on `PHASE_3_REPORT.md`.**

---

## Phase 2 — map-view prototype → Gate A (2026-09-03)

**Status: built; calibrated; Gate A passed 2026-09-03 on the owner's `go` without sessions (Phase 3); D-P2-1/2/3 made by recommendation.** The proto runs on the TS sim at config hash `ee23bb1c` (`PROTO_CALIBRATED` over the canonical `01dc5d02`: economy on, scattered map; `825d2d09` with `economy=0`). Checks green locally (typecheck, `npm test`, lint, E1–E9, docsync, `snapshot:check`); the PR's CI run is the proof. Report: `PHASE_2_REPORT.md`.

### 2.1 What exists

- `packages/proto` — one Phaser screen: 24 px grid with states, wells, skyline-gated facility silhouettes (§8: within 6 blocks of a Held block), **survivor markers** (revealed when a 4-neighbour is Held, "We're in." on Held), claim tool with `Claim — rot N % · front +N · closes N`, pole line, **shape-coded pips** (● ▲ ✕), ring drag list, bloom pulses, slots, rubble strip, HUD with clock/speed/pause, seed in the URL, **`?state=` snapshot loading** (opens paused), Save snapshot, telemetry export with session-relative summary and `meta.scenario`.
- `packages/sim` — survivors placed by §8 band (`placeSurvivors`), `survivorList`, `FacilityView.visible`, `SKYLINE_RANGE`. No measured rule changed; experiments and fixtures bit-identical.
- `packages/harness` — `calibrate.ts` (C1–C7, `--out`, `--md`), `snapshot.ts` (`--check`). Root scripts `calibrate`, `snapshot`, `snapshot:check`. CI runs both (calibration reported, not gated).
- `packages/proto/public/snapshots/b-compact-seed3.json` — Scenario B: compact bot, seed 3, 3:00:00 (28 held / 11 front / 18 interior / 3 assemblers / 0 lost).
- `docs/PROTOTYPE_TEST_PLAN.md` (two scenarios, bot timelines for each, hashes), `docs/TEST_RESULTS.md` (`verdict: pending`), `docs/experiments/calibration.{json,md}`.

### 2.2 Calibration (rule 5)

C3–C7 met on all seeds. C1 and C2 are missed only at t = 0: 200 start rounds (C10) leave the HQ's third hopper empty for 30 s. After the transient compact **never** sees amber in three hours — the edges-per-assembler constant showing itself, recorded as D-P2-2 as the constitution instructs. Lever sweep (start production, start rounds, pool, yield, costs, each alone): only 300 start rounds turns C1/C2 green, and that reverses C10 (D-P2-1); every other lever either does nothing or breaks C3/C7. `PROTO_CALIBRATED` unchanged.

### 2.3 Untagged recount

No doc number changed in Phase 2 (the proto follows the doc; no changelog line). Count stays at **44** (29 tile-scale, 15 design inputs) as D-P1-3 carries it; Gate A's `[play]` tags are what reduce it next.

### 2.4 Open constants after Phase 2

C1 edges per assembler: **the gate's first decision (D-P2-2)** — one Mk1 feeds a 16-block blob to 2:30 with no pip; B's telemetry decides. C2 cadence: Gate A `summary.claimsPerHour`. C3 slots: enclosure at 1:00 is the only route to a second assembler; observer notes. C4 buffer cap: stock sits at 400 in A and drains in ten minutes in B. C8 bloom rhythm: Gate A feel. C10: made at 20, with a 30 s opening flicker (D-P2-1). New: **the steel wall** (D-P2-3) — steel 0 at ≈ 3:40 from the snapshot whatever the tester does.

### 2.5 §26 recount at Phase 2

Three systems: the front, found tech, automated combat. Survivors and the skyline are §8 content of "found tech", not a system. Complexity **5/10**, unchanged.

### 2.6 Gate status

- Experiments green in CI: green locally; PR #2's run is the proof.
- Calibration reported with each lever: yes (`PHASE_2_REPORT.md` Measured).
- Test plan, results template, snapshot, config hash: yes.
- Three decisions for the human: **open** — D-P2-1 start rounds, D-P2-2 edges per assembler, D-P2-3 the steel wall (recommendations in the report).
- **Gate A: pending.** Five testers + control, seed 3, A then B; a human writes `verdict: go`. The programme does not continue on `pending`.

---

## Phase 1 — headless front sim (2026-09-03)

**Status: built; decisions made; gate pending the smoke test.** The canonical map is 24×24 with 200 start rounds (D-P1-1, C10; config hash `01dc5d02`). Experiments green locally (`npm run experiments`: 9 experiments, 3 seeds, 0 failing checks, ~50 s) and wired into CI (`.github/workflows/ci.yml`: lint, `tsc --strict`, fixtures, E1–E9, `docsync --check`). The human five-minute smoke test has not been run. Report: `PHASE_1_REPORT.md`.

### 1.1 What exists

- `packages/sim` — pure TypeScript port of `frontsim.py` with the power model (`firsthour.ts`), districts/enemies/recipes as data (`districts.ts`, `enemies.ts`, `recipes.ts`), six bot policies (`bots.ts`: compact, spike, balanced, cheapest, river, turtle), JSON state, fixed tick, deterministic (`prng.ts`). Fixtures from the Python sim (`fixtures/*.json`, incl. `power3–5.json`) pass under `npm test`.
- `packages/harness` — `cli.ts` (`--seeds`, `--hours`, `--out`, `--nightly --seeds-n`), `run.ts` (`runSim` → `RunSummary`), `experiments/e1–e9.ts`, `report.ts` → `docs/EXPERIMENTS.md` + `docs/experiments/E<n>.json`, `nightly.ts`.
- `packages/tools/src/docsync.ts` — regenerates the §7 district and enemy tables and the §12 recipe table between `<!-- docsync:… -->` markers from `packages/sim`; `--check` is a CI step.
- `docs/EXPERIMENTS.md` — every run named; config hash `01dc5d02` in the header (24×24, 200 start rounds; the 24×22 / 300-round pass was `7637b6e3`). Run names are now the section and row names of that file.

### 1.2 Doc pass

29 disagreements listed in `PHASE_1_REPORT.md` before editing; 27 edited into the doc with changelog lines (§4, §5, §7, §9, §11, §12, §14, §15, §16, §17, §18, §19, §23, §24, §25, §27), two left as flagged assumptions (edge hopper 100 vs turret hopper 50; assembler line 220 kW). Every `[sim: …]` tag outside the changelog now names a section or row of `docs/EXPERIMENTS.md`, except `E10-bloom-cadence` (Python only, marked as such).

### 1.3 Untagged recount

Phase 0 counted 70. After the Phase 1 pass: **44** (DoD asked for ≤ 23 — missed). D-P1-3 passes the gate on the 15 a block sim can reach and carries the 29 tile-scale numbers as Phase 0 routed them; later DoDs count only what that phase's instrument can reach.

| § | Phase 0 | Now untagged | Tagged in Phase 1 | Still untagged |
|---|---|---|---|---|
| §5 | 24 | 14 | 5.6, 5.10, 5.12, 5.13, 5.14, 5.15, 5.19, 5.20, 5.22, 5.23 | 5.1, 5.2, 5.3, 5.4, 5.5, 5.7, 5.8, 5.9, 5.11, 5.16, 5.17, 5.18, 5.21, 5.24 |
| §7 | 7 | 3 | 7.3, 7.5, 7.6, 7.7 | 7.1, 7.2, 7.4 (design inputs) |
| §12 | 9 | 8 | 12.8 | 12.1, 12.2, 12.3, 12.4 (generated from code, no run varies them), 12.5, 12.6, 12.7, 12.9 |
| §13 | 16 | 16 | — | all: tile-scale machine rows, Phase 4–9 |
| §14 | 5 | 4 | 14.2 | 14.1, 14.3, 14.4, 14.5 |
| §15 | 9 | 1 | 15.1–15.8 | 15.9 |

Of the 44, **29 are tile- or world-view numbers** (all of §13, §14.1/3/4/5, §5.1/5/8/9/18, §12.1/2/3/7) that no block sim can evidence — they belong to Phases 4–9 as Phase 0 already routed them. The remaining **15** are design inputs the sim takes as given (bloom timer and drop, claim cost, burn-off, district dmax/g, recipes, hopper, assembler rate, wells per map, Relight 40 MW, endgame hours) — evidenced only by `[play]` at Gate A or by a run that varies them.

### 1.4 Open constants after Phase 1

C1 edges per assembler: E9-hourly gives 67 mag/min over 20 front edges at 5 h and 52 over 21 an hour later (≈ 2.5–3.4 mag/edge-min in play, wake tails included) → one 20 mag/min assembler feeds ~6–8 edges, not ~15; open. C2 cadence: §18 cadence encoded (15 min in hour one, then 5); Gate A telemetry decides. C4 buffer cap: E9-hold shows a 4,000-round cap loses the hold and a 20,000-magazine bank wins it; a game object is needed (Phase 10). C5: made (D1; doc says ~700). C8: E10 not re-run (Python only). C10 start ammo: **made** — 20 magazines, sim default 200 rounds; holds under the 40-arrival rule with 32 accrued, falls at minute 8 under a 10- or 20-arrival rule (§11 states it).

### 1.5 §26 recount at Phase 1

Three systems: the front, found tech, automated combat. Phase 1 added tooling and data, no system. Complexity **5/10**, unchanged.

### 1.6 Gate status

- Experiments green in CI: green locally at 24×24; the PR run is the CI proof.
- Untagged ≤ 23: **missed** (44; 29 unreachable by a block sim) — **passed by D-P1-3** on the 15 reachable.
- Three decisions: **made** (D-P1-1 24×24, D-P1-2 priced choice, D-P1-3 pass with riders C10 = 20 magazines, hopper 100, `main` protection still needs Pro or public).
- `PHASE_1_REPORT.md` names the contradictions and the runs: yes.
- Human five-minute smoke test: **pending** (`npm run experiments`, read `docs/EXPERIMENTS.md`, then `npm run dev` for the proto).

---

## Phase 0 — inventory, untagged set, open constants

### 0.1 Inventory: what exists, and whether it matches the doc today

"Verified today" means run in this session (2026-09-03). "Matches the doc" is against `RELIGHT-design.md` as it reads today, changelog included.

| Artefact | What it is | Verified today | Matches the doc today | Programme phase it belongs to |
|---|---|---|---|---|
| `RELIGHT-design.md` (608 lines, §1–§27 + appendix + changelog) | The spec | read in full | is the doc; internal contradictions listed in 0.2 | all |
| ~~`frontsim.py`~~ **retired Phase 3** (Python, ~1,000 lines) | Reference block sim: front rules, ammo ring, **power model with shedding**, five claim policies, `--experiments` (E1–E8, E4h, E2-demand-half), `first_hour()` | runs: one sim-hour, seed 3, compact, in 0.23 s; `--experiments` suite **not** re-run | yes for §5/§7 rules it models (see gaps below); every `[sim: E*]` tag in the doc traces to a run of this file or `phase5*.py` | Phase 1 reference; the TS sim is the judge from Phase 1 on |
| ~~`frontsim_legacy_backup.py`~~ deleted Phase 1 | Pre-`FRONT_FIX_REPORT` copy | no | no (pre-fix rules) | delete or archive in Phase 1 (`DEFERRED.md` D-13) |
| ~~`phase5.py`, `phase5_results.json`, `phase5b.py`, `phase5b_results.json`~~ **retired Phase 3** | Doc runs E9–E13 (claim cadence, bloom cadence, Relight hold, spike-scattered, inert-as-solid) and calibration-2 reruns | no | tags in §16, §18, §25 trace to these | Phase 1 (E8, E9 of the programme reproduce E9/E11) |
| ~~`export_fixtures.py`~~ **retired Phase 3** → `packages/sim/fixtures/seed{3,4,5}.json` (frozen at `52c4ca3`) | Python → TS parity fixtures (mags, hourly rows, first interior, losses, shells) | via `npm test` | n/a | Phase 1 |
| `packages/sim` (TS, 1,423 lines: `types.ts`, `sim.ts`, `bots.ts`, `map.ts`, `queries.ts`, `index.ts`) | Pure `step(state, commands) → state'`, 1 s tick, JSON state `version: 1`, `configHash()`; six bots (compact, spike, balanced, cheapest, river, turtle); economy (slots 1/block, finite rubble 3,840/block, claim 5 Cu + 10 steel, assembler 20 Cu + 40 steel, magazine 2 steel + 1 Cu) | **`npm test` 28/28 green** (24 fixture comparisons + 4 unit tests); `tsc --strict` clean | partial — see "TS sim gaps" | Phase 1 (the headless sim) — exists, needs the gaps closed |
| `packages/sim/test/calibrate.ts`, `bench.ts`, `regression.test.ts` | Calibration harness CLI (`--hours --seeds --bots --economy --build --react --out` + JSON overrides), benchmark, fixture regression | regression yes; calibrate/bench not run | n/a | Phase 1 → becomes `packages/harness` |
| `packages/proto` (Phaser 3.90 + Vite 6.4, 7 files) | Map-view prototype: 24 px block map, claim tooltip `front +N · closes N`, ring order list, pips, build button with reason, slots, rubble strip, telemetry export with config hash, seed and `economy=0` in URL, speed keys, facility silhouettes, `autoplay` bots | **`npm run build` clean**, 1.53 MB bundle (chunk-size warning only); not played | Phase 2 spec: has everything except `?state=` snapshot loading (Scenario B), survivor markers and a state export button; verified against the sim only, never against a human | Phase 2 — built, **not gated** |
| `FRONT_FIX_REPORT.md` | 13 sim/doc disagreements → E1–E8; doc numbers changed; 8 unsettled; 10 rejected systems; E9–E13 | read | is the provenance of most `[sim]` tags | Phase 1 evidence |
| `CALIBRATION_REPORT.md` | Cadence 8 → 5 min; targets T1–T7; `PROTO_CALIBRATED`; flags §25 item 13 as no longer true | read | §7/§17/§24/§25 still carry the 8-min-cadence numbers it superseded (0.2) | Phase 1 evidence |
| `CALIBRATION_REPORT_2.md` | Slots + finite rubble; T1–T9 (T1, T5, T6, T7 missed, T1 structural); D1–D4 applied to the doc; coal patch size unsettled | read | D1–D4 are in the doc without a human's name on them (`DECISIONS.md`) | Phase 1/2 evidence |
| `PROTOTYPE_BUILD_REPORT.md` | What the proto is, 14 `PROTO-ASSUMPTION`s, three human decisions | read | assumptions not yet tagged `GAME-ASSUMPTION` (rule 6; retag in Phase 2's report) | Phase 2 |
| `PROTOTYPE_TEST_PLAN.md` | 5 testers + control, seed 3, 2.5 h at 4×, criteria, rework/kill triggers | read | is the Gate A protocol | Phase 2/3 |
| `TEST_RESULTS.md` | Template, `verdict: pending`, hashes `6e74fbfd` / `91bad3aa` | read | empty | Gate A |
| `DEFERRED.md` | 12 items | read; every item now has a phase | — | all |
| Not present | `docs/`, `DECISIONS.md` (created now), `EXPERIMENTS.md`, `PHASE_*_REPORT.md`, `test_results/`, `.git`, CI, `packages/harness|tools|game`, `apps/` | — | — | Phase 1 onward |

**Not rebuilt (exists and passes):** the TS sim and its fixture regression; the proto build; the calibration harness. **Not trusted (exists, never verified):** the Python `--experiments` suite as a whole (last run by a prior session, results only in the reports); the proto's feel (no human has played it); every `PROTO-ASSUMPTION`; D1–D4.

**TS sim gaps against §5/§7 (rules the doc states that `packages/sim` does not carry):**

- Power: no substation draw, Generators, brownout or shed order (Python only, `--power`). Programme Phase 1 E4 needs it in TS.
- Wells: `+0.3 dmax`, `×4 g`, 3-block influence are in; "a well dies after five minutes with no Dark neighbour" is in neither sim.
- Blooms: "adjacent blooms interleave 10 s apart" is in neither sim.
- Grid is **24×22** (`map.ts` `W = 24, H = 22`, row 21 = river) with five hard-coded wells and hard-coded district bands; §4 says 24×24 cells and §18's 25 h drawing says "full 24×22 city". No generator, no validator (Phase 9).
- Hopper is 100 rounds per edge (two turrets × 50) — matches §7's "100-round hopper" and §13's 50-round turret if read per edge; §12's "turret hopper (50 rounds = 5 magazines)" is per turret. Wording, not a rule gap.

### 0.2 Contradictions inside the doc (found while reading; none fixed in Phase 0, all for Phase 1's doc pass)

1. **§7 "What threat scaling feels like" and §17 "Strategy space"** still say 3.2× (14,152 vs 4,414 magazines) and 1.6×/1.31× with tag `[sim: E6-scatter, E6-noscatter]`; §9 item 1, §19 and §27 say 2.2× and 0.97× with the same tag "at the 5-minute cadence". The changelog entry that updated §9 is labelled "§7". One tag, two numbers.
2. **§24 risk 6 and §25 item 7** carry the 8-minute-cadence figures (56 % / 131 % / 1.3× / 0.56×); `CALIBRATION_REPORT.md` measured 0.97× / 0.57× at 5 min.
3. **§25 item 13 "Closed"** says spike on the scattered map "loses nothing"; `CALIBRATION_REPORT.md` measured 4–11 blocks lost at the 5-minute cadence and said "needs a human, not a retag". Reopened as D-25-13 in `DECISIONS.md`.
4. **§15 early phase** says 4.5 MW at 3 h `[sim: E2-demand]` (measured at the old 200/40 kW draw); §12's table says 1.0–3.1 MW `[sim: E2-demand-half]` at the D1 draw. §15 mid ("5.5 at 4 h, 7.6 at 10 h") is likewise pre-D1.
5. **Map size**: §4 24×24 cells; §18 and both sims 24×22.
6. **Big pole** "reach 12, supplies 3×3" is smaller than the pole's 7×7; almost certainly a typo for a larger area.
7. **Constitution vs doc** (the constitution is not the spec; the doc wins, but the constitution's Phase 4 text should be corrected before Phase 4): belts "7.5/s" (doc 8/s and 16/s); "Mk1 3 s recipe" (doc: Shot 3 s — agrees); "24×24" (doc agrees; sims 24×22).

### 0.3 The untagged set

Every number in §5, §7, §12, §13, §14, §15 that carries neither `[sim: run]` nor `[play: session]`. Grouped by the experiment or phase that can evidence it. Phases 1–4 aim to empty this list; a number leaves it only by gaining a tag or by being deleted from the doc with a changelog line. Numbers already tagged are not listed. "Derived" means arithmetic on other numbers and needs no run of its own once its inputs are tagged; it is listed so the count is honest.

**§5 The front**

| # | Number | Where | Evidence route |
|---|---|---|---|
| 5.1 | Lit tile burns rot at 0.05/s | Rot | Phase 4 M4 (tile rot); until then the block sim's burn-off stands in |
| 5.2 | Bloom timer `T = 120/(0.5+d)` (240 s at d=0, 96 s at 0.75) | Bloom | E10 varied it (§25 item 1); the *value* is a feel decision — Gate A, `[play]` |
| 5.3 | Bloom drop to `max(0.05, 0.9·d)` | Bloom | same as 5.2 |
| 5.4 | Claim cost 10 wire + 5 frames | Claiming | Phase 5 E13 (does the claim cost bite?); proto uses 5 Cu + 10 steel |
| 5.5 | Pole reach 8 | Claiming | Phase 4 (world view: does reach 8 cross a street margin of 8?) |
| 5.6 | Wake bloom "twice normal size" | Waking | the cap is tagged; the 2× is not — Phase 1 E3 (per-district wake cost) |
| 5.7 | Burn-off `20 + 60·d` s | Held | Phase 1 E8 (claim cadence: the burn-off sets the floor) |
| 5.8 | 2 turrets per edge, range 9, "one per 16 tiles" | Cost table | Phase 4 M3 (turret ring geometry) |
| 5.9 | 3 lamps × 5 kW per edge | Cost table | Phase 4 M5 (light map) |
| 5.10 | Outskirts ammo 4.3 mag/min + 2 shells/min | Cost table | Phase 1 E3 (10 h compact run reaching the outskirts) — residential/industrial rows are tagged, this one is not |
| 5.11 | Per-tile ammo 0.04 / 0.06 / 0.13 mag/min | Cost table | derived from tagged per-edge rows ÷ tiles |
| 5.12 | Substation draw 100 kW front / 20 kW interior | Cost table | D1 (`CALIBRATION_REPORT_2.md`, E4h-*) — evidence exists, **tag missing**, human not on record |
| 5.13 | Bloom size `4 + 36·d` crawlers over 15 s | Bloom | Phase 1 E3; Phase 2 feel |
| 5.14 | Adjacent blooms interleave 10 s | Bloom | not in either sim (0.1) — Phase 1 adds it or deletes it |
| 5.15 | Brownout: 20 s unpowered → turrets stop | Falls | Phase 1 E4 (TS power model) |
| 5.16 | Shade disables a turret 30 s, stacking | Falls / §7 | Phase 1 E3 |
| 5.17 | Hulk 600 HP | Falls / §7 | Phase 1 E3; Phase 4 M3 |
| 5.18 | Creep 1 tile / 3 s during a fall | Falls | Phase 4 M4 (tile rot) |
| 5.19 | Refeed within 60 s resets the arrival counter (D3) | Falls | Phase 1 E1 (starve-and-refeed) — decided, tag missing |
| 5.20 | Knock-on 80 kW per fallen front block (2 × 40) | Falls | derived from 5.12 |
| 5.21 | Wells: 3–6 per map | Wells | Phase 9 generator |
| 5.22 | Well `dmax + 0.3`, growth ×4, within 3 blocks | Wells / §7 | Phase 1 E3 (well row of the district table) |
| 5.23 | Well dies after 5 min with no Dark neighbour | Wells | not in either sim — Phase 1 adds it or Phase 6 |
| 5.24 | Outskirts cap 1.0 | Wells | with 7.1 |

**§7 Threat**

| # | Number | Where | Evidence route |
|---|---|---|---|
| 7.1 | District `dmax` 0.30 / 0.45 / 0.45 / 0.60 / 1.00 | table | design inputs; steady-state outputs are tagged E3-block; the inputs stay untagged until Gate A says the spread reads — `[play]` |
| 7.2 | District `g` 0.0004 / 0.0005 / 0.0005 / 0.0006 / 0.0008 | table | as 7.1 |
| 7.3 | Depth multiplier `1 + 0.3·distance/20`, capped 1.0 | Depth | Phase 1 E3 (E7 tags the *result* 0.24–0.34 only) |
| 7.4 | Crawler: 1 tile, 3 t/s, 12 HP (3 rounds) | Enemies | Phase 4 M3/M4 |
| 7.5 | Shade: 1 tile, 2 t/s, 40 HP (10 rounds); 1 per 8 crawlers at d ≥ 0.3; 30 s disable | Enemies | threshold tested in E3 (0.25 vs 0.3) and kept — tag missing; rate and stats Phase 4 M3 |
| 7.6 | Hulk: 3×3, 0.8 t/s, 600 HP, 60 dmg/shell → 10 shells; 40 % of blooms at d ≥ 0.5; barricade 1 per 4 s | Enemies | as 7.5 (0.45 vs 0.5 tested) |
| 7.7 | "3–4× a residential edge" (outskirts) | table text | with 5.10 |

**§12 Resources**

| # | Number | Where | Evidence route |
|---|---|---|---|
| 12.1 | Rubble 300 units/tile; 250–350 tiles/block (75–105 k) | Rubble | Phase 5 E11/E13 (finite rubble in world view); proto 3,840/block at 32/min |
| 12.2 | Coal ~30 k per block, 4 MJ each | Coal | Phase 5 E11 (coal depletion) |
| 12.3 | Iron deposit ~2 M, coal ~1.5 M | Deposits | Phase 5 E11 |
| 12.4 | All eight recipes (wire 1 Cu → 2 / 1 s; frame 2 steel / 2 s; concrete 2 stone / 2 s; board 3 wire + 1 steel / 4 s; shot 2 steel + 1 Cu → 10 rounds / 3 s; shell 2 steel + 1 coal / 3 s; fuel 1 crude → 4; polymer 2 crude → 1) | Intermediates | Phase 5 E10 (chain throughput) |
| 12.5 | Turret hopper 50 rounds = 5 magazines | Ammo chain | wording (5.8) |
| 12.6 | One assembler = 20 mag/min = 40 steel + 20 Cu/min = 1.3 / 0.7 excavators | Ammo chain | 20/min follows from 12.4's 3 s; the "edges per assembler" it implies is open constant C1 |
| 12.7 | Cannon hopper 20 shells | Ammo chain | Phase 6 (Arsenal) |
| 12.8 | Mid-game 60–150 mag/min + 10–20 shells; late 150–250 + 30 | Counts table | Phase 1 E5 extended past 5 h; Phase 10 E20 |
| 12.9 | 40 MW at the Relight | Counts | Phase 10 E20 (D4 banked-stock hold) |

**§13 Machines** — every row is untagged; they are design inputs, evidenced by the milestone that places them.

| # | Number | Evidence route |
|---|---|---|
| 13.1 | Excavator 3×3, 60 kW, 5×5 area, 0.5/s | Phase 4 M1 |
| 13.2 | Assembler 3×3, 100 kW | Phase 4 M2 |
| 13.3 | Generator 2×2, 300 kW, coal-fired | Phase 4 M3 |
| 13.4 | Mixer 2×2, 150 kW | Phase 5 |
| 13.5 | Gun turret 2×2, range 9, 5 rounds/s, 50-round hopper | Phase 4 M3 |
| 13.6 | Cannon 3×3, range 12, 1 shell / 2 s, 20-shell hopper | Phase 6 |
| 13.7 | Lamp 1×1, 5 kW, radius 4; Floodlight 2×2, 40 kW, 12-tile cone | Phase 4 M5 |
| 13.8 | Barricade 200 HP | Phase 4 M3 |
| 13.9 | Pole reach 8, supplies 7×7; Big pole reach 12, supplies 3×3 (typo, 0.2 #6) | Phase 4 M1 |
| 13.10 | Belt 8/s and 16/s; Inserter 10 kW, 1/s; Splitter 1×2; Underground span 4; Chest 400 | Phase 4 M2, Phase 5 |
| 13.11 | Depot input 2×2 | Phase 5 |
| 13.12 | Tram stop 2×3, 20 kW, 6 inserters; Tram 1×3 / 1×9, 200 / 600 items, 8 t/s | Phase 7 |
| 13.13 | Line truck garage 4×4, 50 kW, holds 4 kits | Phase 8 |
| 13.14 | Substation recipe 20 frames + 20 wire + 10 boards | Phase 5 |
| 13.15 | Pumpjack 3×3, 200 kW, 0.5/s | Phase 6 |
| 13.16 | Facility footprints (HQ 8×8, Foundry 10×10, etc.) and "twenty-six placeable things" | Phase 9 (generator places them) |

**§14 Logistics**

| # | Number | Evidence route |
|---|---|---|
| 14.1 | Street 8 tiles wide (4 + 4 margins); lot 24×24 | Phase 4 M1 |
| 14.2 | Assembler line 220 kW (100 + 2 × 60) | derived from §13 |
| 14.3 | Hand-collecting one stack per 2 s | Phase 5 (Depot) |
| 14.4 | Tram 200 items at 8 t/s; freight 600; 100 s load (6 inserters), 34 s | Phase 7 E16 |
| 14.5 | Front kit 32×4 strip, 2 turrets; truck carries 4 kits | Phase 8 E17 |

**§15 Progression**

| # | Number | Evidence route |
|---|---|---|
| 15.1 | Early 0–3 h, 12–25 blocks | Phase 1 E3/E8 (bot territory at 3 h), then `[play]` Gate A |
| 15.2 | First interior ~40 min (sim says ~47 [E8]) | reconcile in Phase 1 doc pass |
| 15.3 | 15–20 edges by hour 3 | Phase 1 E3 |
| 15.4 | 4.5 MW at 3 h (stale, 0.2 #4) | replace with E2-demand-half |
| 15.5 | Mid 3–12 h, 25–100 blocks; F passes 30 | Phase 1 E3 at 10 h; Phase 6 |
| 15.6 | Turbine 5 MW; "5.5 at 4 h, 7.6 at 10 h" pre-D1 | replace with E2-demand-half-25h |
| 15.7 | Wells killed hours 6–9 | Phase 6 |
| 15.8 | Late 12–25 h, 100–200 blocks, ≥ 15 blocks between wells, 50–70 edges | Phase 1 E3 at 25 h (Python E2-demand-25h holds 184 at 25 h, untagged here); Phase 10 |
| 15.9 | Endgame 20–40 h; first Relight ~25 h | Phase 10 E20; `[play]` Phase 13 |

Count: **§5 24 · §7 7 · §12 9 · §13 16 rows · §14 5 · §15 9 = 70 entries** (a §13 row bundles a machine's numbers). Of these, four have evidence in a report but no tag (5.12, 5.19, 7.5/7.6 thresholds) and two are stale tagged numbers (15.4, 15.6) that need replacing rather than evidencing.

### 0.4 Open constants (tempo-setting, not yet evidenced)

Each has an "open" line in `DECISIONS.md` with its doc value. The seven the constitution names first, then three more the reading turned up.

| ID | Constant | Doc value | Proto / sim value | Where evidence would come from |
|---|---|---|---|---|
| C1 | Edges fed per assembler | ~15 (20 mag/min ÷ 1.37 mag/edge-min in play; §12) | Mk1 10 mag/min, Mk2 20 (proto `PROTO_CALIBRATED`) | Phase 1 E5, Gate A `[play]` |
| C2 | Claim cadence the game is drawn for | §18 drawn at ~5 min after hour one `[sim: E9-claim-cadence]` (a run tag on a drawing, not a decision) | bots: 15 min hour one, then 5 min (was 8) | Gate A telemetry `summary.claimsPerHour` |
| C3 | Machine slots per block | "machine slots on interior blocks" (§5), lot 24×24 tiles (§4) | 1 (PROTO-ASSUMPTION) | Phase 4 M1 (what fits on a lot); Gate A |
| C4 | Magazine buffer cap | none stated | 400 magazines (4,000 rounds) | Phase 1 E1/E5; Phase 4 M2 (a chest is 400) |
| C5 | HQ start patch sizes | §11: "a small steel patch"; coal **~700 (D1 ratified 2026-09-03; doc still says ~3,000 until the Phase 1 doc pass)** | proto steel 7,680 at 64/min (empties at 2:00); coal not in proto | Phase 1 E4 (hour-one power), Phase 5 E11 |
| C6 | Fall time after the substation stops | 90 s (D2, was 5 min) `[sim: E8-fall16]` | 90 s | D2 ratified 2026-09-03; Phase 3 checks a human can act in the window `[play]` |
| C7 | Substation draw | 100 kW front / 20 kW interior (D1, was 200/40) | Python only | D1 ratified 2026-09-03; Phase 1 E4 encodes it in TS |
| C8 | Bloom timer base and drop | `120/(0.5+d)`, 10 % (§5) | same | Gate A `[play]` (E10 measured the alternatives) |
| C9 | Assembler rate | 20 mag/min (§12) | proto Mk1 10 / Mk2 20 | with C1 |
| C10 | Start ammo | 20 magazines (§11; sim `startRounds` 300 = 30 magazines) | 300 rounds | Phase 1 doc pass (reconcile), Phase 3 |

### 0.5 §25 open questions → phase

| §25 | Question | Phase that answers it |
|---|---|---|
| 1 | Bloom cadence (feel) | Gate A (Phase 2 → 3), `[play]`; C8 |
| 2 | Diagonal leaks (closed, E6/E13) | Phase 9 — generator validator asserts no diagonal-only Dark pocket |
| 3 | Global stock too easy | Phase 5 (Depot radius rule is the test) and Phase 13 human rounds |
| 4 | Well visibility | Phase 2 shows wells; decision at Gate A |
| 5 | Endgame hold | Phase 1 E9 (banked), Phase 10 E20 (full) |
| 6 | Density farming | Phase 6 (enemy purpose) and Phase 13 (degenerate-strategy bots) |
| 7 | Quiet-block problem | Gate A (cheapest-matched testers), Phase 13 |
| 8 | Outskirts / well edge cost | Phase 1 E3 (10 h compact run) — also 5.10 |
| 9 | Coal depletion | Phase 5 E11 |
| 10 | §18 drawings vs cadence | Phase 1 E8 + Gate A telemetry; §18 redrawn in Phase 3 |
| 11 | Late-game demand | Phase 1 E5 extended to 25 h in the TS sim; Phase 10 E20 |
| 12 | Fall distance (closed, D2) | Phase 3 confirms the rescue window `[play]`; C6 |
| 13 | Spike on the scattered map (marked closed; evidence says otherwise) | **reopened by the human 2026-09-03** — Phase 1 E7 reruns at the 5-min cadence; D-25-13 |
| 14 | Power in hour one (closed on draw; patch open) | Phase 1 E4, Phase 3; C5, C7 |

No §25 item lacks a phase; none deleted.

### 0.6 §24 risks → phase

| §24 | Risk | Phase |
|---|---|---|
| D1 | Re-fronting tedium | Phase 8 E17; Phase 13 |
| D2 | Ammo tuning window | Phase 1 E5; Gate A; Phase 13 |
| D3 | Rot unreadable | Phase 2 (mottle), Phase 12 |
| D4 | Block fall illegible | Phase 4 M3/M4, Phase 12 |
| D5 | Endgame haul | Phase 10 |
| D6 | Quiet-block greed | Gate A, Phase 13 |
| D7 | Early brownout sheds HQ | Phase 1 E4, Phase 4 M3 |
| D8 | Straight push outruns ammo | Phase 1 E5/E7, Gate A |
| D9 | Cascade | Phase 1 E2 |
| T1 | Per-tile rot cost | Phase 4 M4, Phase 11 |
| T2 | Pathfinding | Phase 4 M4 (flow fields), Phase 6 |
| T3 | Belt sim cost | Phase 5, Phase 11 |
| T4 | Procedural validity | Phase 9 |
| T5 | Light map | Phase 4 M5 |

### 0.7 §26 recount at Phase 0

Three systems: the front, found tech, automated combat. Nothing was added. Complexity **5/10** as the doc says; the count is unchanged because Phase 0 built nothing.

### 0.8 Experiment naming

The doc's run tags (E1-starve … E13-inert-solid, E4h-*, E2-demand-half) and the constitution's experiment numbers (E1–E9 in Phase 1, E10–E20 later) **collide**. Phase 1's `EXPERIMENTS.md` must carry both names per row (constitution number, doc tag) so a `[sim: …]` tag stays traceable. Proposed map for Phase 1: E1 ↔ E1-starve · E2 ↔ E2-h500 · E3 ↔ E3/E3-block/E7 · E4 ↔ E4h-* · E5 ↔ E5 · E6 ↔ E6-scatter/E6-noscatter · E7 ↔ E12-spike-scattered · E8 ↔ E9-claim-cadence · E9 ↔ E11-relight-hold.
