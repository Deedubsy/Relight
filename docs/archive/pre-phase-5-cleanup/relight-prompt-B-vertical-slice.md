# Relight — Prompt B: the vertical slice, rebuilt on the engineer and the street-first city

This is the prompt that reopens Phase 4. It replaces the constitution's Phase 4 section (M1–M6, Gate B) after the rework that applied D5 (the engineer on foot, with a weak weapon) and D6 (the city generated streets-first; blocks are the faces). Everything else in the constitution stands: doc follows sim, `GAME-ASSUMPTION` tags in code and in every report, humans decide constants, rules surface in-game, a report per milestone, `PROGRAMME_STATE.md` updated and `DEFERRED.md` re-read at every gate, §26 recounted, a bare `go` is a gate verdict.

**Read first, in this order:** `PROGRAMME_STATE.md` (the rework entry at the top), `REWORK_REPORT.md` (what D5/D6 changed, every assumption with its question, and the three decisions a human makes before this prompt runs), `DECISIONS.md` (D5, D6, and D-P4-4/5/7/8/9, which this slice settles at M6), `RELIGHT-design.md` §4, §5, §7, §11, §13, §14, §17, §19 as rewritten at the rework, and `SLICE_REPORT.md` for what M1–M3 built on the lattice.

**Starting point.** The block sim already runs on the graph (`packages/sim/src/city/`, `graph.ts`) with the engineer, the rifle, the truck and retaliation modelled at block level (`engineer.ts`, `bots.ts`); the map view is the polygon map (`packages/game/src/cityMapScene.ts`) with the side panel cut to held / front / interior, magazines made against demanded, stock and blocks lost, the rest behind the backquote key; the Phase 2 calibration and E1–E9 have been re-run on the city and E-rifle, E-walk and E-variance added. What is still lattice-bound is the tile layer (`tiles.ts`, `flow.ts`, `worldScene.ts`): 32×32 cells, 24×24 lots, a global Depot stock, a cursor with no body. Three stubs in the game mark the seam and M1 removes all three: the engineer walks only under a bot (`?walk=1&autoplay=…`), the tile flow layer is off on a city, and the world view is unavailable on a city (`?map=lattice` still has the old one).

**What the slice is for.** The first hour, played with a mouse and WASD as §11 writes it, by an engineer who walks, carries, places within reach, and can fire once when a belt is late; ending with lights coming on in a block that was dark. Gate B is not scored on the lattice slice; it is scored on this one.

## Milestones

Each milestone ends with its section in `SLICE_REPORT.md` (built / assumed with every `GAME-ASSUMPTION` / deferred / measured / where it disagrees with the doc, reported not resolved), a `PROGRAMME_STATE.md` update, a `DEFERRED.md` re-read, and the §26 recount. A milestone that finds a doc number wrong edits the doc with a changelog line and the run name.

### M1 — Ground and the engineer

- **Tiles on faces.** Port the tile layer to the city: a lot is a rasterised face (`CityGeom.owner`), streets are the 6–12-wide ridges between faces, the river is water tiles, plazas and parks are inert faces. Rubble typed by district in three clusters inside the polygon, densest on the deepest faces (depth in hops, `city/spec.ts`), 250–350 tiles scaled by area so a 50-tile face carries more than a 20-tile one; outskirts faces carry deposits or nothing. The lot's buildable tiles are the face minus its substation; the machine-slot count (one per ≈ 600 buildable tiles, D6) is checked against what actually fits when M2 places machines. The six tile tests are rewritten on faces (geometry, river and inert, counts and types, gradient, authority of block state, determinism and cost); a full-city derivation stays under 100 ms cold.
- **The engineer.** A sprite on the tile grid, starting at the HQ lot's workbench. WASD and click-to-walk at 6 tiles/s along any walkable tile (streets, lots, dark blocks, rot; never water); the camera follows; wheel zoom 0.5–3× about the engineer. **M** toggles map ↔ world: world → map opens the map with the engineer's block marked; map → world returns the camera to the engineer, and a click on a block in the map view sets a walk target on its pole tile. The reach ring (8 tiles) is drawn faintly and brightens when the cursor is inside it; anything outside reach shows the "walk closer" cursor and a toast the first time. Walking anywhere is harmless (D5: rot and dark blocks do not hurt).
- **Pockets and the chest.** A personal inventory of 40 stacks (stack sizes as `engineer.ts`: rubble 50, magazines 20, machines one each) with a panel on the I key; the Depot is a 6×6 chest and workbench on the HQ lot. Hand-mining within reach puts units in the pockets (M2 had it go straight to the Depot; that ends here); transferring to and from the chest needs the engineer within reach of it. Claims are paid from the chest because they are made from the map (§14). Start stock in the chest as §11: 200 steel, 100 copper, 50 stone, 20 magazines.
- **The harness bot keeps walking.** `walkingBot` is the calibration's player and stays block-level; the game's `?walk=1` flag goes away because walking is now the only way to move. The `__relight` hooks gain `walkTo(x, y)` and `engineer()` so headless checks can drive the sprite.
- **DoD:** the M1 soak (1 h at 4×, camera following a bot-driven engineer, 60 fps, no frame over 50 ms except a screenshot); the engineer can walk from the HQ to the Tram depot and back without a load stutter; E toggles nothing any more (M does); the three stubs above are gone.

### M2 — Flow on irregular lots

- Excavator (3×3, 0.5/s onto an adjacent belt), belts (7.5/s, items visible, corners), inserters (1/s), Mk1 Shot assembler (3 s, 2 steel + 1 Cu → 1 magazine), hand-crafting at the workbench within reach, as built in the lattice M2, now placed **from the pockets, within reach**, on any buildable tile of the face. Placement outside reach is refused with the cursor, never silently moved.
- Picking up a machine returns it to the pockets (four stacks for §11's two turrets carried at minute 40); a full pocket refuses the pick-up with a toast.
- The 20 ticks/s tile tick and the derived 1 s block tick as before; the block sim's economy stays the judge and the tile layer feeds it the same numbers. Measured rates equal doc rates on a face that is not a rectangle: the belt run from the steel patch to the assembler on seed 3's HQ face, timed.
- **Measured for D-P3-10:** what fits on the HQ face and on the three neighbouring faces; whether the slot count from area matches what a player can lay out.

### M3 — Defence on the segment

- Turret (2×2, 50-round hopper, range 9, 5 rounds/s, fed by inserter, belt or hand from the pockets), Lamp (5 kW, radius 4), the pre-existing 3×3 substation per face (GA-M3-16) at a street-side spot on the face's longest ridge, poles (reach 8), Generator on coal from the start patch, power as one number with the §14 shed order, streetlights along every ridge the substation powers.
- An **edge is a street segment** (`segs[].ridge`): a turret covers the segment its nearest ridge tile lies on; an edge's rounds are the sum of the turrets on it; the empty-hopper event turns that segment's pip red in the map view from the same event. The HQ's start turrets follow the doc's text once D-P4-8 is settled (the sim starts two a segment; §11 says two on the north edge).
- The Electricians' unlocks (Floodlight, Big pole, craftable Substation) land on the toolbar when their block turns Held (deferred from the lattice M3).

### M4 — Threat, and the rifle

- Rot as tile presence in dark faces; blooms at the segment ridge facing the player; crawlers on a per-face flow field (lamp → turret → substation), the 40-arrival rule at tile level, never more than two blocks of travel along the graph. Shades only if residential rot reaches its threshold inside the hour; no hulks.
- **Retaliation only (D5).** A crawler keeps its chain and turns on the engineer only when shot by them or when the engineer stands in its path; 5 HP/s at arm's reach; the engineer has 100 HP, regains 5 HP/s after 5 s out of contact, is knocked down at zero and stands up at the HQ workbench 10 s later with the pockets intact; no other penalty. The HP bar appears only when below full.
- **The rifle.** One weapon, fed by magazines in the pockets, 1.5 rounds a second, three rounds a crawler (≈ 2 s; the turret beside you does it in 0.6), no range advantage over a turret (9), shades untargetable off lit tiles, fired with the right mouse button held on a target. It is a tool for the minute the belt is late, not a defence: the harness's E-rifle found the compact bot never fires in five hours of steady play, and that a whole belt lost for 90 s falls to a shade the rifle cannot hit while a single dry edge holds either way. The Arsenal upgrade (1.4 s a crawler) is outside the hour and is a toolbar entry only.
- **The truck is outside the hour.** E-walk finds it at 91–146 minutes on the compact bot, so this slice does not build driving; the Tram depot's truck is a Phase 5 item (`DEFERRED.md`) and the block-level model (×3 speed, 200 stacks) stays the calibration's.
- **Telemetry** adds: minutes walked per hour, trips to the chest, placements refused for reach, the minute of the first rifle shot, rounds fired by hand, kills by hand, HP lost, knockdowns, and for every hand-fired engagement whether the edge held.

### M5 — Light

- A single-channel light texture at the city's size (800×800) multiplied over the world layer; streets and lamp radii lit, lots unlit; rot cannot exist on a lit tile; burn-off sweeps light along the claimed face's ridges over `20 + 60·d` seconds. The engineer carries no light (the hand lamp is one of the report's three decisions; if the human takes it, it is a 2-tile radius on the sprite and nothing else changes). Built last so the payoff lands on a working block.

### M6 — The hour, §11 as rewritten

- A bot follows §11's minute list on the tile layer with telemetry on: the walk to the steel patch, the trips to the chest, the workbench, the two hand-fed turrets, the claim of east and the 40-second walk over, west, north at the cap, the two turrets carried, the Electricians in the Depot. Every divergence from §11 and from the city calibration timeline (`docs/experiments/calibration.md`: first amber 16–31 min, first red 16–31 min, enclosure 45–60 min, held 4 / 16 / 25–28 at 60 / 120 / 180) is a finding. The rework's own findings to re-check at tile level: hour-one claim walking under a minute, the truck outside the hour, and whether the lot's trips make walking the 15 % of input §19 budgets or more.
- D-P4-4 (start stock and machine prices), D-P4-5 (the block-level assembler stand-in), D-P4-7 (hour-one power, the second Generator), D-P4-8 (the HQ's start turrets) and D-P4-9 (turrets on claimed blocks) are settled here from the played hour, each a `DECISIONS.md` row.
- Then a human plays.

**Throughout:** fixed 20 ticks/s at tile level, 1 s block ticks derived; a 1 h sim at 4× must not drop the render loop; the map-view fixtures and the block sim's 88 tests keep passing; tile events drive the same block transitions; `npm run snapshot:check`, `docsync:check` and the experiments stay green on every commit.

## SLICE_REPORT.md

Rewritten from the top for this slice: per milestone, every `GAME-ASSUMPTION` with the phase or decision that resolves it, the M6 bot timeline against §11 and against the city calibration, performance, and, at the top, the §19 first-hour test scored from human play with the two rows Gate B gains.

## Gate B

A human plays the hour and scores the §19 first-hour test: new unlock, current problem and memorable moment in each of 0–10, 10–30 and 30–60 minutes, and whether the burn-off made them say something. Gate B gains two rows:

- **At what minute did the player first fire, and did it matter?** The minute from telemetry, or "never" (a legitimate answer: the game is completable without firing, and the compact bot never fires). "Did it matter" is answered by the harness: the played hour's command log is replayed with the rifle off, and the row says whether the edge the player fired at fell in the replay. Answers are `held anyway`, `saved it`, or `fell anyway`. If the answer is `saved it` in more than one session, or the rifle was fired before minute 30, the rifle is becoming the defence (§24 risk 10) and the human reopens D5's time-to-kill.
- **The walking row.** Minutes walked and trips to the chest in the hour, against §19's 15 % budget. Over the budget reopens reach (§25 item 15) or brings the truck into the hour; under 5 % and walking is not a cost, which reopens the inventory cap (item 16).

The human writes `verdict: proceed` in `SLICE_REPORT.md`. **[play: Gate B]** tags in the doc are locked on this verdict, the way `[play: Gate A]` locked on Gate A's.
