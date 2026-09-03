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
