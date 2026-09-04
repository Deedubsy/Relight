# Deferred from the map-view prototype

Everything here needs tiles, belts, machines on a lot, or lighting, and so is outside the map-view prototype by rule. One line each on why it was tempting while building.

Re-read at every phase gate (constitution rule 9): every item carries the phase that builds it, or is deleted with a reason. Phase 0 (2026-09-03) assigned the phases below; nothing was deleted.

- **World view (tiles inside a block).** → **Phase 4 M1–M4 (world view).** Tempting because the tooltip's "wake bloom ≈ 26 crawlers" has nothing to land on: the fight is a ring pulse on a 24 px square, so the player never sees why a big bloom is worse than a small one.
- **Belts and the ammo line as a physical thing.** → **Phase 4 M2 (belts, inserters, ammo line).** Tempting because the ring order list is the whole ammo model and reordering a list feels like admin, not building; the doc's "last edge starves first" is a belt fact the list only asserts.
- **Machines on a lot (assemblers, excavators, the Foundry's own machines).** → **Phase 4 M1 (excavator on a lot), M2 (assembler), Phase 5 (Foundry machines).** Tempting because "Build assembler" is a button that costs stock; there is no placement, so the tester cannot feel the space pressure §13 is about.
- **Light map and the lit/dark contrast.** → **Phase 4 M5 (light map).** Tempting because the map's Held amber against Dark navy is the only visual payoff, and §6's "the block lights up" is the design's core moment; flat colour cannot deliver it.
- **Power (Generators, substations as load, brownouts, the shed order).** → **Phase 1 (port the Python power model to `packages/sim`, experiment E4); Phase 4 M3 (substation as a placed thing).** Tempting because the sim already has a power model and the doc's hour one is dominated by it (§25 item 14); left out on the brief's Phase 1 decision.
- **Trams and the transport line.** → **Phase 7 (trams, E16).** Tempting because the facility toast ("Reached the Foundry") has no consequence; a transport link would give reaching a facility a reason.
- **Blueprints.** → **Phase 8 (blueprints and front kits, E17).** Tempting because compact play is repetitive by hour three and a "claim this shape" stamp would cut the clicking; it would also hide exactly the per-claim decision the prototype tests.
- **Line truck / Excavator as units.** → **Phase 8 (Line truck, E17). Excavator is a machine, not a unit: Phase 4 M1.** Tempting because the decorative pole line from the nearest Held block is drawn instantly; a truck that drives it would make claim time visible. The claim timer (20 s + 60 s × rot) already carries that.
- **Cannons and shells as a separate line.** → **Phase 6 (Arsenal, hulks, shells, E14).** Tempting because hulks are drawn (white diamond) but cost nothing: shells are counted in the summary and never made or spent. The Arsenal facility is a silhouette only.
- **The Relight.** → **Phase 10 (E20).** Tempting because the 25-hour state is where the front rule pays off (§16) and the prototype has no end condition; E11 in the Python sim covers the numbers.
- **Art beyond flat colour.** → **Phase 12 (art and audio).** Tempting because Dark blocks with rot mottle read as "noisy navy" rather than "rot"; the doc's mottled-by-density legend needs texture to be legible at 24 px.
- **Rubble sinks for stone.** → **Phase 5 (concrete recipe, walls and foundations in the world view).** Tempting because stone accumulates with no use, which makes civic blocks feel pointless; the doc gives stone to walls and foundations, both world-view features.

## Added at Phase 0

- ~~**`frontsim_legacy_backup.py`.**~~ **Deleted in Phase 1** (2026-09-03): the TS sim carries the power model and the suite is green.
- **Python reference sim (`frontsim.py`, `phase5*.py`, `export_fixtures.py`).** The doc's `[sim]` tags trace to it → **Phase 1: keep as the fixture source until the TS experiment suite reproduces every tagged number; then retire it and retag to the TS runs.**

## Re-read at the Phase 1 gate (2026-09-03)

Every item above keeps its phase; the power item is half done (TS power model and E4h exist; the substation as a placed thing stays Phase 4 M3). Added:

- **Python reference sim.** Fixtures pass and every doc tag now names a TS run → **retire at the Phase 2 gate**: freeze `packages/sim/fixtures/*.json`, delete `frontsim.py`, `phase5*.py`, `export_*.py`.
- **E10-bloom-cadence (§25 item 1, C8).** The only Python-only number left in the doc. → **Phase 2 Gate A** decides the rhythm by feel; if a number is wanted first, port E10 to the harness (a day).
- **Open-ground runs.** Every Phase 1 headline is on the scattered map; open ground appears only in E6-shape. The §19 turtle "same on open ground" is structural, not measured. → **nightly** if anyone wants the distribution; no phase needs it.
- **Edge hopper 100 vs turret hopper 50 (§12).** → **Phase 4 M3** (turret ring geometry) settles turrets per edge.
- **Relight bank as an object (C4, D4).** → **Phase 10 E20**, as before; E9-hold says the size matters more than the assembler count.
- **Map 24×24 vs 24×22.** → **decided 24×24 (D-P1-1)** and every experiment re-run on it. Left over: the **§18 drawings are still the 24×22 sketches** → **Phase 3** redraws them from the compact bot at the locked cadence (the constitution's Phase 3 bullet; this line said Phase 2 until the Phase 2 re-read); the sim's five hand-placed wells (the southern three moved with the HQ) → **Phase 9** generator.
- **Spike feed order (D-P1-2).** Spike's losses are the far end of the ring starving under creation-order feeding; a "feed the edge nearest the enemy first" rule is a belt/logistics question → **Phase 4** (belts and the ring), with E1-ring-spike as the before/after run.
- **Nightly distribution at 24×24.** The 20-seed smoke was on 24×22 and is dropped from the tree; the cron's first 1,000-seed run gives the 24×24 distribution → **nightly**, nobody waits on it.
- **Linear GitHub integration (D-CI).** → **Phase 2**, when the game tasks start moving.

## Re-read at the Phase 2 build (2026-09-03, before Gate A)

Every item above keeps its phase except the §18 redraw (corrected to Phase 3 in place). Nothing deleted. Changed or added:

- **Python reference sim.** "Retire at the Phase 2 gate" — the Phase 2 gate is Gate A, which has not run. → **retire in Phase 3's first commit once `TEST_RESULTS.md` says `go`**: freeze `packages/sim/fixtures/*.json`, delete `frontsim.py`, `phase5*.py`, `export_*.py`. Nothing in Phase 2 needed it.
- **E10-bloom-cadence (C8).** Unchanged → Gate A feel; `TEST_RESULTS.md` §8 now has a row for it.
- **Linear GitHub integration (D-CI).** Still a GitHub UI step a human performs; no commit can do it. → **human, any time**; not blocking.
- **Survivor unlocks.** §8 gives each group recipes on the toolbar; the proto only toasts "We're in." and lists them in the panel. → **Phase 4** (toolbar exists in world view). Tempting because the Electricians join at ~1:05 and nothing happens.
- **Optional survivor groups (Chemist, Lamplighters, Surveyors) and "dead-end pocket" placement.** The proto places the five named groups by Manhattan band and skips the optional ones. → **Phase 9** generator. Tempting because a pocket is a shape reason and the proto is about shape.
- **Skyline as a real skyline.** §8's "silhouettes up to 6 blocks away" is a visibility flag on a 24 px icon here. → **Phase 4 M5** (light map) or **Phase 12** (art). Tempting because a fresh start shows only the Arsenal (6 blocks off; the Foundry is 10), which reads as "one thing to head for" and gives the icon more weight than a 24 px sprite carries.
- **Ring feed order** (nearest-the-enemy first, D-P1-2). Scenario B's cascade runs down the ring in creation order; the drag list is the only counter. → **Phase 4** as before; Gate A's reorder telemetry is the before.
- **Steel income outside industrial blocks** (D-P2-3 option b). Only if the human calls the steel wall a bug. → **Phase 5 E11/E13** with the rubble model.

## Re-read at the Phase 3 gate (2026-09-03, after Gate A `go`)

Gate A was passed by the owner without tester sessions (`TEST_RESULTS.md` §1). Every item above keeps its phase unless listed here. Closed or changed:

- ~~**Python reference sim.**~~ **Deleted in Phase 3's first commit (D-P3-8):** `frontsim.py`, `phase5.py`, `phase5b.py`, `export_fixtures.py`, `export_power_fixtures.py`, the two result JSONs and `__pycache__`. `packages/sim/fixtures/*.json` are frozen at commit `52c4ca3` (`packages/sim/fixtures/README.md`); nothing regenerates them, and a fixture that stops passing is a rule change that needs a doc sentence and a changelog line.
- **E10-bloom-cadence (C8).** Gate A gave no feel verdict. → **locked at the doc value (D-P3-5)**; **Phase 4 playtests** are the first place it can move; ported to the harness only if the human asks for a number first (`PHASE_3_REPORT.md` decision 3, D-P3-11).
- ~~**§18 drawings are still the 24×22 sketches.**~~ **Closed in Phase 3:** the three map-view drawings are generated by `npm run docsync` from the compact bot on seed 3 at the locked cadence and CI-checked. The 10-minute world-view lot sketch stays hand-drawn at tile scale → **Phase 4 M1** redraws it from the world view. The five hand-placed wells → **Phase 9**, as before.
- **Steel patch size (C5 steel).** → **Phase 5** rubble model, as a named placeholder (proto 7,680 at 64/min, empties at 2:00); the §19 steel wall is intended (D-P2-3), so nothing moves it before then.
- **Relight bank as an object (C4, D4).** → **Phase 10 E20**, now with a size: ~20,000 magazines, two hours of eight-assembler production (D-P3-4). The 400-magazine line buffer is a chest (§13) and stays.
- **Slot count (C3).** One ammo line per interior block is what the sim and the slice encode (GAME-ASSUMPTION). → **Phase 4 M1** (excavator on a lot) measures the footprint a lot actually has; **Phase 5** sets the budget. `PHASE_3_REPORT.md` decision 2 (D-P3-10).
- **Gate A tester sessions.** Not run. → **human, any time**; recommended in parallel with **Phase 4 M1** (D-P3-9). If they contradict a `[play: Gate A]` lock, the lock moves and the tag becomes the session name.
- **Facility pull in the bots.** The compact bot scores a claim by front added and reaches the Foundry at hour 14 on seed 3; a player pulled by the skyline goes north at hour one (§11). The §18 drawings show the front rule alone. → **Phase 4 M1–M4** playtests show what a human does; a "toward the nearest visible facility" bot term is a harness question for **Phase 5** (E11–E13 need steel from the Foundry side). Tempting now because the strip-vs-blob difference is the §18 finding of this phase.
- **Steel income outside industrial blocks** (D-P2-3 option b). D-P2-3 made as intended → stays **Phase 5 E11/E13**, only if a Phase 4 tester calls the wall a bug.

## Re-read at Phase 4 M1 (2026-09-03, on the `Go` that opened Phase 4)

Phase 4 M1 built the ground only (tiles, rubble, river, camera, E toggle); the excavator and every machine moved to M2 by the constitution's milestone text, so items that said "M1 (excavator on a lot)" now say M2. Every item above keeps its phase unless listed here. Nothing deleted.

- **Machines on a lot / Excavator as a machine.** → **Phase 4 M2** (Excavator 3×3 at 0.5/s, belts, inserters, Depot, Mk1 Shot assembler, hand-mining). M1 has no machine; the HQ is a 6×6-tile slab placeholder at the lot centre.
- **Slot count (C3) / lot footprint.** → **Phase 4 M2** measures what fits on a 24×24 lot once machines land; **Phase 5** sets the budget (D-P3-10 made as locked).
- ~~**10-minute world-view lot sketch (§18).**~~ **Re-routed to Phase 4 M3**: the sketch draws turrets, belts and a ring, none of which M1 has; the §18 caption says so. The block-map drawings stay generated.
- **Gate A tester sessions.** D-P3-9 made: in parallel with Phase 4, the human's to run. Nothing in M1 reads a `[play: Gate A]` lock.
- **Fixed 20 ticks/s tile tick.** M1 steps the 1 s block sim by real delta × speed from the render loop, capped at 0.1 s a frame. → **Phase 4 M2** introduces the tile tick and derives the 1 s block tick from it, as the constitution's M2 line says.
- **Units per rubble tile (§12 300/tile vs the sim's 3,840 pool).** M1 draws the doc's tile count and lets the block's pool set how many stand. → **Phase 5** rubble model (E11/E13), D-P4-2.
- **Deposits as placed facilities.** M1's outskirts deposit patches are a hash placeholder. → **Phase 9** generator, D-P4-3.
- **Rot on tiles (mottle overlay) and the light map.** M1 shows block state as a flat lot overlay (navy / amber flicker / outline). → **Phase 4 M4** (rot at tile level) and **M5** (light map), as before.
- **Tileset art.** M1's tiles are flat-colour canvas frames drawn in code. → **Phase 12**, as before; the frame layout (street, ground, inert, river, 3 × 5 rubble, 2 × 5 deposit) is the art brief.
- **Camera in the map view.** The map view still has no pan or zoom (a 24 px grid fits the canvas). → **Phase 9** if the generator makes cities larger than 24×24; otherwise never.

## Re-read at Phase 4 M2 (2026-09-03, M2 Flow built)

M2 built the tile tick and the machines (`flow.ts`). Every item above keeps its phase unless listed here. Nothing deleted.

- ~~**Machines on a lot / Excavator as a machine.**~~ **Built in M2:** Excavator, belt, inserter, Mk1 Shot assembler, Depot, hands. The Foundry's own machines, Generators, Mixers, splitters, undergrounds and chests → **Phase 5** (nothing in minutes 0–60 needs them; a Generator is M3's power).
- ~~**Belts and the ammo line as a physical thing.**~~ **Built in M2** on the HQ lot. The belt to a turret hopper → **M3**. The ring feed order (D-P1-2) stays **Phase 4 M3/M6**: with physical belts the "nearest the enemy first" rule becomes a belt-layout fact the player makes.
- ~~**Fixed 20 ticks/s tile tick.**~~ **Built in M2:** `advanceFlow` steps 20 tile ticks a block tick; the harness stays block-only.
- **Slot count (C3) / lot footprint.** M2 measured: a §11 line (two 3×3 Excavators, one 3×3 assembler, 18 belts, three inserters) plus the 6×6 Depot and the three patches fits the north two-thirds of the 24×24 lot with room for a second assembler and its belts; the south strip keeps 100 rubble tiles. → **Phase 5** sets the budget from that (D-P3-10).
- **Units per rubble tile.** M2's patch tiles carry real units (steel ≈307 a tile, copper 100, coal ≈78), the ordinary rubble tiles still stand in for the block pool. → **Phase 5**, D-P4-2 as made.
- **Start stock and machine prices.** §13 prices nothing; M2's prices are a GAME-ASSUMPTION and the calibrated 80/40 start is one steel short of a §11 line. → **D-P4-4**, settled at **M6** from the played hour.
- **Block-level assembler stand-in on claimed blocks.** → **D-P4-5**, decided at **M6**.
- **Two-lane belts, fast belts, splitters, undergrounds, chests (§13/§14).** M2 belts are one lane at 7.5/s. → **Phase 5**, when the wire and frame recipes need a second input lane.
- **Power draw on machines (§13 kW).** Carried on each machine, drawn by nothing. → **M3** with the Generator and the substation.
- **Hand-collecting from a chest (§14.3).** No chest yet; hand-mining is a unit a second into the Depot. → **Phase 5** with chests.
- ~~**Survivor unlocks on the toolbar.**~~ **Built at prompt B M3** (`flow.ts` `lockReason` / `unlockedKinds`, keys 0 / [ / ]): the Electricians' three land when their block turns Held. The other groups' unlocks → **M6 / Phase 5** with their machines.

## Re-read at Phase 4 M3 (2026-09-03, M3 Defence built)

M3 built defence and power (`flow.ts`, `tiles.ts`). Every item above keeps its phase unless listed here. Nothing deleted.

- ~~**Substation as a placed thing.**~~ **Built in M3** as a pre-existing 3×3 (D-B1-4 places it nearest the lot's centroid since the pre-M2 items). ~~The craftable Substation and the outskirts' missing one~~ **built at prompt B M3** (`faceSub`; §7's outskirts have none at tile level, D-B3-2).
- ~~**Edge hopper 100 vs turret hopper 50.**~~ **Built in M3:** on the HQ an edge's rounds are the sum of its two 50-round turrets; every other Held block keeps the block sim's 100-round stand-in. → **D-P4-9**, decided at **M6** with D-P4-5.
- ~~**Belt to a turret hopper.**~~ **Built in M3:** inserter from a belt of magazines into the hopper. The Gunsmith's direct belt input → **Phase 5/6** (survivor unlocks).
- ~~**Generator / power draw on machines (§13 kW).**~~ **Built in M3:** every placed machine draws its rated kW, Generators supply by burning coal, one number with the §14 shed order through the tile machines.
- ~~**10-minute world-view lot sketch (§18).**~~ **Built in M3:** `renderLot` + docsync `section18lot`, CI-checked; it found that §11's own line browns out at minute 0 on one Generator (→ D-P4-7) and that the Generator-feed inserter must shed last (§14 edited, `M3-rates`).
- **Ring feed order (D-P1-2).** With physical belts it is a layout fact on the HQ; the block sim's drain among an edge's turrets is fullest-first (GA-M3-5). → **M6** with the played hour: closed if no tester asks which turret fires first.
- **Turret range 9 as geometry; enemies as things on tiles (5.17 hulk, 7.4 crawler, 7.5 shade stats).** M3 fires at the block sim's engagements; nothing walks. → **Phase 4 M4**.
- **Barricade 200 HP (13.8).** Nothing to stop until the hulk walks. → **Phase 4 M4** with the hulk, else **Phase 5**.
- **Light map (M5) and the shade rule at tile level.** M3 has the light model (`cellLights`, `litAt`, radius 4, streetlights three in eight broken); nothing draws darkness yet. → **Phase 4 M5**.
- ~~**Electricians' unlocks (Floodlight, Big pole, craftable Substation).**~~ **Built at prompt B M3** (run `B-M3-unlocks`).
- **Hour-one power (risk D7, "an early brownout sheds the HQ first").** At tile level with machines-first shedding the HQ substation is never shed in the §18 run; the assembler is, at minute 0, until the second Generator. → **D-P4-7**, settled at **M6** from the played hour together with D-P4-4 (the sim's 80/40 cannot buy the §11 line and that Generator).
- **Block fall illegible (risk D4).** M3 adds the red hopper pulse, the shed crosses, the dead-grid and brownout toasts. → **M4** (rot at tile level) and **M5** (light) as before, judged at Gate B.
- **Turrets on claimed blocks.** Physical on the HQ, block-level elsewhere — still so after prompt B M3 (the Electricians' block Held fires the block sim's way). → **D-P4-9** at **M6** with D-P4-5: a claimed block gets a world-view line and turrets, or a button and a hopper, one model.

## Re-read at the rework (2026-09-04, D5/D6 applied; the slice to be rebuilt from `docs/relight-prompt-B-vertical-slice.md`)

Every item above keeps its phase unless listed here. The lattice M1–M3 items that said "M4", "M5", "M6" now mean prompt B's M4–M6 on the city. Nothing deleted.

- **Tile layer on faces (`tiles.ts`, `flow.ts`, `worldScene.ts` are 32×32-lattice-bound).** → **Prompt B M1** ports rubble, lots, streets, the river and the substation to rasterised faces; M2/M3 re-place machines and turrets on them (a turret covers its nearest segment). Tempting to keep the lattice world under the city map, and the three game stubs do exactly that until M1.
- **The engineer as a sprite, reach ring, pockets panel, walk anywhere.** → **Prompt B M1**. The block-level engineer (`engineer.ts`) is the calibration's and stays.
- **Hand-mining into the pockets (M2 had it go to the Depot).** → **Prompt B M1/M2** with the chest.
- **The rifle as a held button, retaliation, HP bar, knockdown and respawn at tile level.** → **Prompt B M4**. The hand lamp (D-R1 option b) → **M5**, only if D-R1 takes it.
- **The truck driven (Tram depot) and self-driving (Line truck).** E-walk finds it at 91–146 min, outside the hour → **Phase 5** (driven), **Phase 8** (Line truck, as before). The block-level ×3 / 200 stacks model stays the calibration's.
- **The Arsenal rifle upgrade (1.4 s).** → **Phase 6** with the Arsenal.
- **Light texture at 800×800; burn-off sweeping along ridges.** → **Prompt B M5**.
- **Camera in the map view.** The polygon map fits 800 tiles in 648 px at 0.8 px a tile; faces of 20 tiles are 16 px. → **Prompt B M1** if the palette test (§24 risk 11) fails at Gate B; otherwise never.
- **Plazas' value (GA-R3).** A plaza is a free wall and nothing else. → **Phase 9** generator (a plaza as a facility site, a park as a well site) if a human wants them to matter; noted in `REWORK_REPORT.md` §8.
- **Preset-specific canvases and validators (GA-R1).** All five presets share 800×800 and one validator. → **Phase 9**.
- **Lattice map, fixtures and results.** Kept behind `--map lattice` / `?map=lattice` as the archived baseline for E-variance's lattice-vs-city rows. → **delete at the Phase 5 gate** if nothing reads them by then.
- **Gate B on the lattice slice.** Not scored (D5/D6 rule). → the rebuilt slice's Gate B, with the two new rows (first fire minute and whether it mattered; the walking row).
- **D-P4-4/5/7/8/9.** Still settled at **M6**, now prompt B's M6 on the city.
- **E9's bank evidence on the city.** On the lattice the same assemblers without a bank lost 21–30 blocks in the Relight window; on the city they lose none, so E9 no longer produces evidence for D4 and the check passes trivially. → **Phase 10 E20** needs a harder window (a longer hold, or the surge on a bigger front) before the bank's size can be argued from the sim — E9-hold, this run.


## Re-read at prompt B M1 (2026-09-04, Ground and the engineer built)

Every item above keeps its phase unless listed here. Nothing deleted.

- **Tile layer on faces.** Done in M1 (`ground.ts`: one derivation per state, the lattice wrapped into the same shape). The lattice itself still → **delete at the Phase 5 gate**.
- **The engineer as a sprite, reach ring, pockets panel, walk anywhere.** Done in M1 as a code-drawn disc, a ring, a DOM panel on I. The sprite, the ring's look and the "walk closer" cursor → **Phase 12** art pass.
- **Hand-mining into the pockets.** Done in M1. **Hand-crafting from the pockets** (§14) is not: the workbench still draws the chest and delivers to it, within reach of the workbench → **Prompt B M2**.
- **Kits by hand (D-B1-3).** → **Prompt B M6** tedium audit.
- **The truck driven (Tram depot).** Unchanged, **Phase 5**: on seed 3 the Tram depot is a 20 s walk from the HQ each way, so the hour does not need it.
- **Camera in the map view.** Unchanged.
- **The `?flow=0` free camera** (the old world view with no engineer, kept for bot comparisons against the block sim) → **delete with the lattice at the Phase 5 gate**.
- **Walk paths outside `SimState`.** A snapshot restarts a walk in progress (the path is re-planned from the saved destination, the WASD velocity is dropped) → **Phase 12** save/load, if a saved walk ever matters.
- **The panel's pockets rows as fixed "take n" buttons.** Take 50 (a stack) of rubble, 5 magazines, 1 kit; no drag, no split → **Phase 12** interface.
- **A block-level `walkTo` from the map on a city with the flow layer paths by A\* over the tiles, not by the graph's street distance** (the harness bot's walk). The two agree to within the path's diagonal saving; E-walk stays on the graph → **Phase 11** if the graph distance is ever wrong enough to matter.

## Re-read before prompt B M2 (2026-09-04, the four items after M1: D-B1-5, D-B1-4, C1/C2, reference machine)

Every item above keeps its phase unless listed here.

- ~~**Click-to-walk in the world view.**~~ **Deleted (D-B1-5, the human's decision):** the world view is direct control only (WASD, Shift sprint, Space dodge, the hand on left-click); the map view's walk-here is the only auto-walk. `walkTo` stays in `walk.ts` for the harness bots and the `__relight` dev hook, never bound to a click in the world.
- ~~**Start turrets on segments (D-P4-8, "prompt B M3").**~~ **Done before M2 by D-B1-4** (`flow.ts` `startTurrets`); prompt B M3 measured them on seed 3 (1 by reach / 2 / 2 / 2 over four segments) and put the hopper-empty pulse on the segment's pip. The player's turrets on claimed segments stay with D-P4-9.
- **The stamina bar, the dodge's look and the rifle's aim line** (D-B1-5) are code-drawn like the engineer's disc → **Phase 12** art pass, with the sprite.
- **The reference-machine measurement** is by hand once a milestone (`soak.cjs` over CDP to the Windows Chrome, `PROGRAMME_STATE.md` `reference_machine:`); the headless swiftshader soak stays the regression check → **not automated**; a CI GPU runner is a Phase 13 question if the DoD is ever gated in CI.
- **The block-only harness start ring** (`sim.ts` `syncEdges`: with no tile layer the HQ's edges at t = 0 are kitted and fed as an abstract ring, GA-B1-15) → **delete with the lattice at the Phase 5 gate**, when every state has tiles and `startTurrets` is the only source.

## Re-read at prompt B M2 (2026-09-04, Flow on the faces built)

Every item above keeps its phase unless listed here.

- **Machines in the Depot chest, and the truck bringing them** (§19): the chest holds rubble, magazines and kits; a machine is in the pockets or on the ground. → **Phase 5** with the truck; the chest's item list (`CHEST_ITEMS`) grows then.
- **A machine as a crafted item** (workbench recipe with a time, Factorio's way): M2 pays rubble at placement (GA-B2-1). → **Phase 5 recipes**, D-B2-1.
- **Ground items**: nothing in the slice drops an item on a tile — a full pocket refuses the pick-up, a full chest refuses the put. → **not in the slice**; revisit with the truck (a load dropped at the kerb).
- **The bots on the tile machines**: `bots.ts` still plays the block economy; the tile layer feeds the block sim the same numbers (M2 measured them equal on seed 3's HQ face). → **the Phase 5 gate** decides which sim rates the economy (D-P4-5, M6).
- **Slots from area vs footprint** (D-P3-10 measured, D-B2-2): `slotsOf` stays C3's cap for the slice; the tile layer does not enforce it. → **M6** when tile lines replace the block stand-in; if the human takes option (b), C3/C4 and the calibration move.
- **Belt side-loading and splitters**: a belt that meets another belt's side neither gives nor takes. → **Phase 5**.
- **The copper arm's 3:1 over-supply** of one assembler (one Excavator at 0.5/s against 0.333/s): the belt backs up and the drill stops, as a belt should; a smaller copper source or a second consumer is a **Phase 5** recipe question.

## Re-read at prompt B M3 (2026-09-04, Defence on the segment built)

Every item above keeps its phase unless listed here.

- **Turrets on claimed blocks and the outskirts' abstract substation** (D-P4-9, D-B3-2): the HQ fires through its turrets and an outskirts face has no substation on the tiles, while every other Held block — outskirts included — runs the block sim's hopper and substation. → **M6** with D-P4-5, one model for a claimed block.
- **Pole "supplies 7×7" / Big pole "supplies 3×3"** (§13): a substation powers its cell, so both only link and claim. → **M6 / Phase 5** with D-P4-9.
- **The Substation's recipe** (§13.14, frames + wire + boards): 50 steel + 25 Cu in rubble stands in (GA-B3-2, D-B3-1). → **Phase 5** recipes.
- **The other survivor groups' unlocks** (§8: Concrete crew, Foreman, Arsenal, Rail crew, …): the gate is one line a group in `SURVIVOR_UNLOCK_NAMES`; their machines do not exist. → **M6 / Phase 5** as each machine lands.
- **The Floodlight as light** (a cone of light on the ground rather than a filled sector in the world view). → **M5** with the light texture.
- **Hour-one power with the line on** (D-P4-7): on the city with power on, the §11 line plus the HQ's draw is 350 kW against one Generator and the Shot assembler is shed first, the HQ lost at 12:25. → stays with **D-P4-7** (open); the slice runs with power off until it settles.
- **The bot and the Electricians**: the compact bot never claims the Electricians' block (3 hops, civic on seeds 3 / 4 / 5) within the hour, so no harness run exercises the unlocks; the browser check and the tests do. → **M6** (the played hour) or a `river`-policy run that targets a survivor block, if Gate B wants the bot's number.

## Re-read before prompt B M4 (2026-09-04, D-P4-7 settled, D-B3-4 built)

Every item above keeps its phase unless listed here.

- **Hour-one power with the line on** (D-P4-7): closed. The M3 script was the pre-E4 hour; on the E4 schedule the tile-level hour holds on seeds 3 / 4 / 5 with 0 brownout seconds (`B-M4-gate`), and the slice now runs with power on (`session.ts` `power: true`). → **done**.
- **The Generator feed in the shed order** (D-P4-7's second half): moot — there is no shed order (D-B3-4). → **closed**.
- **Coal past the hour**: the HQ patch's ~700 coal is out of the Depot at 60:00 on every seed with four Generators burning (639 / 621 / 621 burned, 103–121 left in the Generators, about six minutes), so the E4 finding "west is wanted by minute 30" is now a tile-level one; the bot's hands cannot fetch coal from a claimed block. → **M6** with the played hour (the west claim's rubble into the Depot is Phase 5 logistics).
- **The first red pip before the line runs**: with the line at E4's minutes the HQ's start hoppers run dry at 5:34–6:21, before the assembler's first magazine at ~8:00; the bot's hands from the buffer carry the ring. §11's "run six magazines to its turrets" is that moment. → **M4** measures it with real crawlers; **M6** decides whether the start hoppers (C10 fill) or the buffer change.
- **A brownout at tile level in the block sim's window** (`E2`): the E2 6-hour rows are block-sim runs; the tile-level ring under a sustained brownout (the HQ's assemblers at 50 %) is measured only for the hour (`B-M4-gate`, no shortfall). → **M4** soak with power on.
- **Turrets and belts under a brownout**: never slowed (GA in `stepFlow`, §14). A rule, not a constant; revisit only if M4's playtest wants a brownout to bite the front. → **M6**.

## Re-read at prompt B M4 (2026-09-04, Threat and the rifle built)

Every item above keeps its phase unless listed here.

- **The first red pip before the line runs**: measured with real bodies — the HQ's corner sliver (two turrets by reach) meets 3–5 crawlers a bloom from 2:42–3:03 and runs dry at 5:34–6:21 on the E4 schedule; the bot's hands from the buffer carry it. The 6:10–6:11 pip with the rifle bot is its `restock` emptying the buffer, a harness artefact. → **M6** decides the start hoppers / the buffer with the played hour.
- **A brownout at tile level in the block sim's window**: the M4 soak ran the hour with power on and the tile threat, 0 brownout seconds (the E4 schedule never falls short). A tile-level shortfall soak is still unrun. → **M6**.
- **Shades in play**: no residential block reaches its threshold in the §11 hour on seeds 3 / 4 / 5; the rules are tested only. → **M6** (a longer or harder played hour).
- **Lights a crawler ate** (5–13 an hour) stay dark with no way to relight them. → **M5** repair.
- **The stand-in edge's bodies** (D-P4-9): a claimed block's unfed remainder walks the tiles while its fed count is the block sim's; the bot's first claim gets its own red pip at 15:29 on seeds 3 / 5 with 0 tile turrets. → **M6** with D-P4-5 / D-P4-9.
- **Rifle Mk2 and the truck**: the toolbar row is in; the upgrade (1.4 s a crawler) and the truck are outside the hour. → **M6 / Phase 5**.
- **Hulks and the barricade chain** (§7): no barricade in the slice. → **Phase 5**.
- **Crawlers and the player's own machines**: bodies walk around belts, inserters and assemblers and never harm them; §7 has no rule for it. → **Phase 5** with the barricade.
- **A player's walking share** (§19's 15 %): the bot's 2.6–4.1 % is a bot's. → **M6** playtest.

## Re-read at prompt B M5 (2026-09-04, Light built — unverified)

Every item above keeps its phase unless listed here.

- **Lights a crawler ate** stay dark with no way to relight them → **closed**: E on the light repairs it for 1 Cu (`repairLight`, GA-B4-7 retired, GA-B5-5).
- **The Floodlight's cone as light** (the M3/M4 texture deferral) → **closed**: the cone is stamped into the light map like every other light.
- **Copper for repairs in the hour**: a face's 3-in-8 broken streetlights leave gaps on the street until repaired or a Lamp fills them; whether the hour's copper stretches to it → **M6** playtest.
- **The unlit look** (§4 "desaturated"): a multiply cannot desaturate; the cool cast stands in → **Phase 12** art pass.
- **The hand lamp** (D-B5-1): none built; `?handlamp=1` previews a drawing-only disc → the human.
- **The sequence's pace against the burn-off** (the prompt's spread over 20 + 60·d s vs §6's three per second) → D-B5-2.
- **Light on the map view**: the map's squares do not read the mask → **M6** if the hour needs it.
- **Shades on lit tiles in play**, **the first pip**, **a tile-level brownout soak**, **the stand-in edge's bodies**, **a player's walking share** → **M6** as at M4.
- **The verification pass for M5** (checks, experiments, calibration, the soak with the light map, Playwright) → when the human schedules it; until then every M5 number is *unverified*.

## Re-read at prompt B M6 (2026-09-04, unverified)

Every item above keeps its phase unless listed here.

- **Copper for repairs in the hour** → **Gate B**: the bot never repairs; the played hour answers it.
- **Light on the map view** → **Gate B** says whether the hour needs it; not built at M6.
- **Shades on lit tiles in play**, **the first pip**, **a tile-level brownout soak**, **a player's walking share** → **E-hour** on the verification pass (the bot's numbers), then **Gate B** (the player's).
- **The stand-in edge's bodies** (a claimed block's edge with no turret) → **D-P4-9** after the played hour; the bot carries the HQ's idle turrets to north because a claimed block has none to carry (GA-B6-4).
- **The claimed lots' own machines** (the bot stands E4's stand-ins on the HQ lot) → **D-P4-5** after the played hour.
- **The bot's timeline against §11 and the calibration** → **the verification pass** (`E-hour`); the findings list is empty until then.
- **A scenario-B replay** (a snapshot session's command log) → when a scenario-B hour is played.
- **The frame time under the hour bot at 4×** → the verification pass's soak.
- **The verification pass for M5 and M6** (checks, experiments with `E-hour`, calibration, the soak, Playwright) → when the human schedules it; until then every M5 and M6 number is *unverified*.

