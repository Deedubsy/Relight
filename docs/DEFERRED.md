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
