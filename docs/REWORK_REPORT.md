# Relight — Rework report: the engineer and the street-first city (D5, D6)

Written 2026-09-04 at the end of the rework brief's six steps. D5 and D6 were logged in `DECISIONS.md` before any other edit (the brief's first rule). Neither adds a system: §26 recounted below stays at three. Gate B was not scored on the lattice slice; the slice is rebuilt from `docs/relight-prompt-B-vertical-slice.md` after the three decisions at the end of this report. A fourth, D-R4, was taken during the run rather than left open, because a red check is a red build and it was the one thing standing between the rework and a green one. Neither stop condition fired: §26 did not reach four, and E-rifle found its damage number.

## 1. D5 and D6, applied

**D5 — the engineer on foot.** In the block sim (`packages/sim/src/engineer.ts`, `bots.ts`): a position on the graph, a reach (8 tiles), pockets (40 stacks), the Depot as a chest on the HQ block, a kit per claim (10 stacks: turrets, lamps, pole, belt stub, magazines) walked from the chest to the claimed block at 6 tiles/s along the street path, the truck found at the Tram depot (3–5 hops out; ×3 speed, 200 stacks), a rifle (1.5 rounds/s, three rounds a crawler ≈ 2 s; turret rules: shades untargetable off light, no range advantage), retaliation only (5 HP/s at arm's reach, 100 HP, 5 HP/s regen after 5 s, knocked down at zero, up at the HQ 10 s later with the pockets intact). The bots walk when `SimConfig.walk` is on; a block claimed from the map has unkitted edges (the pip shows `KIT_WAIT`) until the engineer arrives. In the game: **M** toggles map ↔ world (was E), the map view is the polygon map, the engineer is drawn on it. The tile-level sprite, the reach ring, the pockets panel and the rifle as a held button are the rebuilt slice's M1 and M4.

**D6 — streets first.** `packages/sim/src/city/` generates an 800×800-tile city per seed and preset: two curved arterials, a bending river, one diagonal avenue, a ring of secondary streets that branch and dead-end, plazas and parks as inert faces (8–10 %), widths 6–12; faces subdivided until they fit in 50×50, rasterised, 3–7 neighbours across shared street segments (a segment counts when the shared ridge is ≥ 5 tiles). The block sim runs on the graph (`graph.ts`: `nb`, `deg`, per-edge ring entries keyed by segment); districts are hop bands from the HQ; the validator is restated on the graph (HQ river-adjacent with 3–4 land neighbours, every face ≥ 2 land neighbours unless river-adjacent, outskirts reachable, no facility behind a well, wells ≥ 5 hops out); slots and the rubble pool scale with area. Five presets: River city (default), Ring road, One giant industrial district, No outskirts, Canals (`docs/seeds/<preset>-<seed>.png`). The lattice survives behind `--map lattice` / `?map=lattice` with its fixtures and results archived under `packages/sim/fixtures/lattice/` and `docs/experiments/lattice/`.

**The rework's own finding, fixed on the way (Step 5).** Every city calibration run lost the HQ at minute 27: the city's HQ face is civic (stone), while the lattice's start cell had been residential by accident of the zone rule (`x % 3`), so the lattice HQ yielded copper and the city HQ did not, and the 10-wire claim could not be paid from minute 15. The block-only economy now gives the HQ lot's own patch copper (3,840 at 32/min, the lattice's supply) beside its steel; the tile layer's HQ copper patch is 1,200 units. That gap is decision 3.

## 2. Doc sections changed (`RELIGHT-design.md`, every line in the changelog with a run name)

§3 (the engineer as the payoff picture), §4 (streets-first geometry; the world view as the primary screen, the map on M), §5 (every rule restated on the graph; `front +N` −7…+5; per-block variance row; slots per area; the measured rescue), §7 (retaliation, HP, respawn; rifle rate 1.5 rounds/s), §8/§9/§10 (reveal and enclosure across segments; the Depot row is the chest and workbench; the truck and the rifle upgrade), §11 (the three windows rewritten with the engineer; the E-walk sentence corrected to what the sim measures), §12 (the HQ lot's own patches), §13 (Depot chest, Workbench, Truck rows), §14 (chest and pockets; the truck's two lives; substations per face), §17 (the generator, districts as bands, the validator on the graph, presets), §18 (the three examples rendered as map-view images of the city at the locked cadence), §19 (hands test with walking 15 % and a shooting ceiling 10 %; tedium row for walking), §22 (player combat as a system, hunger, death penalties not simulated), §23 (rejected avatar reversed; lattice rejected), §24 (risks 10 and 11), §25 (items 2 and 3 closed; items 15–18 added), §26 (recounted). A second doc pass followed the `--big` run, because the city moved numbers the first pass had carried over from the lattice: §5 (unfed→fall about 5 min; wells 4–6 a map; the first well claimed at 7.6 h), §7, §9, §17, §19, §24, §25 and §27 (every shape ratio: spike 2.4× compact, quiet-block play 1.27×, the turtle 963 magazines, the fed ring on spike losing 19 blocks, parity at 5 h, and the inert-wall claim rewritten as frontage rather than free walls), §17's validator list restated as `validate()` actually checks it, §6/§12/§16/§25 (the 25-hour costs, late demand and the Relight window re-measured, which cost D4 its sim evidence), and §24's technical risk 4 (the generator's measured rejection rate and the raised attempt budget, D-R4). Seven more changelog lines, each with its run name. Tags delivered: `[sim: rework-graph]`, `[sim: E-rifle]`, `[sim: E-variance]`, `[sim: E-walk]`, `[play: Gate B]`.

## 3. Every `GAME-ASSUMPTION` the rework added, with its question

| # | Where | Assumption | The question for the human |
|---|---|---|---|
| GA-R1 | `city/generate.ts` | Canvas 800×800 tiles (~400 faces, the lattice's 552 cells minus plazas and the river); street widths 6–12 by street class. | Is 400 faces the right city for a 25-hour game, or should the canvas follow the preset? |
| GA-R2 | `city/generate.ts` | Subdivision stops when a face fits in 50×50; cuts land at 35–65 % of the long side; 20 % of secondary streets dead-end. | Do 20–50-tile faces read as blocks at map-view scale (§24 risk 11)? |
| GA-R3 | `city/generate.ts` | 8–10 % of faces are plazas and parks (inert), never stranding a neighbour below two land neighbours (one if river-adjacent). | Is a plaza worth anything to the player beyond a free wall (E6 on the city says inert-hugging is no cheaper than compact)? |
| GA-R4 | `city/generate.ts` | The HQ face must hold a 24-tile square (the lot the HQ layout was drawn on); a facility face 16. | Does the HQ layout survive on a face that is not a square (slice M2 measures)? |
| GA-R5 | `city/generate.ts`, `map.ts` | The Tram depot (the truck) is 3–5 hops out, not §8's 5–10, so the truck is an hour-2 to hour-5 find (E-walk: 91–146 min). | Is a truck found after the first hour the right pacing, or should the first hour end with it? |
| GA-R6 | `city/generate.ts` | Two faces are neighbours only across a ridge ≥ 5 tiles long. | Should a short touch (a corner across a wide street) wake the neighbour at all? |
| GA-R7 | `city/generate.ts` | Outskirts = every face within two hops of the boundary street; districts are hop bands from the HQ; the Power station sits at ≥ 60 % of the city's depth on the far side from the Foundry. | Are hop bands the districts we want, or should the arterials divide them? |
| GA-R8 | `city/generate.ts`, `city/spec.ts` | No well within `WELL_RANGE + 1` hops of the HQ; a hop is a block for well influence (1 − hops/4); "no facility behind a well" = not within one hop of a well and farther from the HQ than it; §17's "≥ 15 blocks" scaled to hops (a hop ≈ 1.35 lattice blocks). | Do the lattice's well distances mean the same thing on a graph with 3–7 neighbours? |
| GA-R9 | `sim.ts`, `types.ts` | Machine slots: one per ≈ 600 buildable tiles, at least one (§25 item 17); the rubble pool scales linearly with area, the lattice lot being the unit. | Does slot value make players chase big faces, and is that the read we want? |
| GA-R10 | `sim.ts` | On a city the start buffer fills every HQ hopper (a 4-neighbour HQ on 200 rounds has two full and two empty otherwise). | Should the HQ's start rounds scale with its neighbour count (D-P4-8)? |
| GA-R11 | `graph.ts` | An engineer walks block centre to block centre along the streets; the true street path is a slice M1 number. | Accept centre-to-centre as the calibration's walk until the tile path exists? |
| GA-R12 | `engineer.ts` | Reach 8, pockets 40 stacks, walk 6 tiles/s, truck ×3 and 200 stacks, HP 100, regen 5 HP/s after 5 s, respawn 10 s, rifle 1.5 rounds/s, retaliation 5 HP/s, kit 10 stacks. | §25 items 15, 16, 18 carry the questions; the calibration says the kit walk is what moved (§5 below). |
| GA-R13 | `engineer.ts` | Stack sizes: rubble 50, magazines 20, machines one each; kits are free to draw (the claim paid for them); a redirected walk restarts from its old destination's distance. | Is a free kit right, or should the kit be crafted from the chest's stock? |
| GA-R14 | `bots.ts` | The bot claims from the map the moment its clock says so and walks afterwards; with walk on, the Tram depot is claimed as soon as it is a candidate (the truck is worth the detour). | Does a player detour for the truck at hour two? |
| GA-R15 | `types.ts` | The HQ lot's start patch yields copper (3,840 at 32/min) as well as steel in the block-only economy; the tile layer's HQ copper patch is 1,200. | Decision 3. |
| GA-R16 | `run.ts` | The doc's headline numbers refer to the River city on seed 3 (E6 measures the other presets). | Is the River city the default players see first? |
| GA-R17 | `session.ts` | The engineer walks only when a bot plays (`?walk=1&autoplay=…`) until slice M1 gives the human a sprite. | None; removed by M1. |
| GA-R18 | `session.ts`, `main.ts` | The map is the street-first city unless `?map=lattice`; its tile flow layer is off and the world view unavailable on a city until M1 ports the tile layer. | None; removed by M1. |
| GA-R19 | `sim.ts` (E-rifle) | A block's belt "90 s away" is an edge whose hopper refills only after `cut`; the rescue scenario is the block whose Dark neighbours carry the most rot. | Is this the rescue a player meets, or is the late belt usually one edge (E-rifle says one edge holds either way)? |
| GA-R20 | `city/generate.ts` | The rejection-sampling budget is 16 jitter streams a seed (D-R4); a seed that exhausts all 16 is served its last invalid city rather than refused, with `valid: false` and the validator's reasons for the seed browser to show. | One seed in 2.3 billion has no valid city. Should that seed be refused outright — the browser says why and the player picks again — or is serving a slightly broken city the kinder failure? Nothing in the sim reads `valid` today. |

The 80-odd pre-rework tags in `flow.ts`, `tiles.ts`, `worldScene.ts` (`SLICE_REPORT.md` GA-M1…M3) stand until the slice rebuild replaces their geometry.

## 4. Experiment deltas, lattice → graph

E1–E9 were retagged to the city (seed 3, River city, the same CANON config plus the HQ copper). Every check that passed on the lattice passes on the city; the numbers moved as below. Lattice values from `docs/experiments/lattice/EXPERIMENTS.md`, city values from `docs/EXPERIMENTS.md` (the `--big` run of 2026-09-04).

| Experiment | Check | Lattice | City |
|---|---|---|---|
| E1 | §5 unfed→fall delay (substation N 40, starve) | 7.20 min | 5.03 min |
| E1 | fed ring on spike, blocks lost in 5 h | 10.3 [9–12] | 19.3 [4–33] |
| E3 | wake bloom ÷ steady bloom (residential) | 1.71× | 1.86× |
| E3 | civic steady blooms carrying a shade | 0.07 | 0.04 |
| E5 | hour-one load on one Shot assembler | 0.37 | 0.34 |
| E5 | four assemblers at 5 h | 0.60 | 0.58 |
| E5 | straight push against production at 5 h | 0.96 | 1.01 |
| E6/E7 | spike ÷ compact, §18 cadence | 2.60× | 2.41× |
| E6 | inert walls: inert plazas ÷ every plaza buildable | 0.48× (9 % scattered cells) | 0.97× (9 % plazas) |
| E6 | inert-hugging against compact, ammo | 26 % more | 99 % more |
| E7 | cheapest ÷ compact | 1.07× | 1.27× |
| E7 | turtle, magazines in 5 h | 878 [877–879] | 963 [756–1,131] |
| E8 | first well neutralised | 11.49 h | 7.60 h |
| E9 | Relight-window need, seeds 3/4/5 | 118.5 / 125.4 / 135.8 | 137.8 / 138.7 / 136.5 |
| E9 | D4: blocks lost in the window, bank vs none | 0/0/0 vs 21/26/30 | 0/0/0 vs 0/0/0 |

Unchanged: **E2** (all nineteen checks identical, no loss after the window), **E4** (start patch out at 29.32 min, coal at 609.21 s), E1's 200 starting rounds, E3's industrial shade rate 1.00 and outskirts 2.00 shells/min, E7's 0.99 interleaving.

**What moved, and why.** Four checks were retagged rather than re-measured, because the map they name stopped existing: the lattice's *scattered* map (9 % of cells inert at random) is now the city's plazas and parks (9 % of faces), and *open ground* is the same city with the plazas buildable. E6's three inert-wall checks and E8's well check carry the new wording.

- **A face has 4.91 neighbours where a lattice cell had 4**, so an unfed block is pressed from more sides: the unfed→fall delay drops to 5.03 min, the fed ring on spike loses 19 blocks instead of 10, and a Held block costs 1.22× the lattice's magazines (E-variance).
- **Inert walls stopped paying.** Scattered inert cells sat inside the field and shortened fronts everywhere (0.48×); plazas sit at street junctions and shorten few (0.97×). The doc's claim that inert cells are worth half the ammo bill was rewritten: the price of shape is frontage, not free walls (§7, §9, §17, §19, §24, §25, §27, this run).
- **Quiet-block play is no longer cheap anywhere.** It cost 0.57× compact on open ground and 1.07× on the scattered map; on the city it is 1.27×, and 1.40× with the plazas buildable. §24 risk 6 and §25 item 7 keep the risk but lose their evidence.
- **Hugging the inert boundary costs 99 % more ammo** than compact, against 26 % on the lattice, and holds 38 blocks [28–52] against 52. §25 item 2's check passes far more clearly than it did.
- **The graph settled a doc disagreement.** §15 puts the first well at hours 6–9; the lattice said 13–24 h and the doc carried the gap. The city says 7.60 h.
- **E9's D4 evidence is gone, not contradicted.** On the city nothing falls in the Relight window with or without the bank, so the check passes trivially and no longer supports D4's bank. Recorded in `DEFERRED.md` for E20 (Phase 10), which needs a harder window on the city.

**E-rifle** (`[sim: E-rifle]`). Steady play, compact, 5 h, three seeds: the rifle changes the ammo bill by 0.10 % (mean |diff|) and loses nothing; **the bot never fires** in five hours (no red engaged edge on the block it stands on), so the first-fire minute Gate B records is expected to be late or never. The rescue (the most threatened Held block, at 2 h and 4 h, edge scope and ring scope): every edge-scope rescue holds with or without the rifle; two of six ring-scope rescues fall at +1.5 and +1.7 min, both to a **shade at an unlit edge**, with or without the rifle, because the rifle follows the turret rules. **The damage number: 10 HP a kill** (5 HP/s while three rounds land at 1.5/s), **62 HP mean per rescue**, 90–145 HP on the four hard cases (HP min 10–30), no knockdown in twelve rescues; 100 HP buys ten kills.

**E-walk** (`[sim: E-walk]`). Compact on foot: 0.8–0.9 min walked in hour one, 1.2–3.9 in hour three, 1.8–2.1 in hour five; the truck found at 91 / 106 / 146 min; 39–100 % of hour-3 moves within reach or in the truck (the 20 % trigger in §19 is not tripped); walking vs the lattice's teleport at 5 h: held 52/52, 52/50, 52/52, lost 0/0, 0/2, 0/0, ammo −0.1 / −0.4 / +0.7 %, walks ≈ 100 tiles [26–438]. The bot walks between blocks, not across the lot: §11's "a fifth of the hour" was corrected to this, and the lot trips are the slice's to count.

**E-variance** (`[sim: E-variance]`).

| Measure (10,000 seeds through the generator, River city) | Value |
|---|---|
| Land neighbours per block, mean | 4.91 |
| Share of blocks with 3–7 neighbours | 95.40 % (1: 0.1 %, 2: 3.0 %, 8: 1.4 %, 9–10: 0.1 %) |
| Machine slots per block | 1: 83.4 %, 2: 15.3 %, 3: 1.3 % |
| Live blocks per city | 339 [305–372] |
| Inert share | 9.0 % [7.9–10.1] |
| Max hops from the HQ | 19 [16–23] |
| District shares (civic / residential / industrial / outskirts) | 10 / 17 / 33 / 40 % |
| Jitter attempts used | 0.35 [0–8] |
| Seeds with no valid city, budget 8 (as first shipped) | 1 in 10,000 (seed 3767) |
| Seeds with no valid city, budget 16 (D-R4, the run this table is from) | 0 in 10,000 |

| Sim, 1,000 seeds at 5 h | City (River) | Lattice |
|---|---|---|
| Magazines per Held block | 150.2 [94.1–292.8] | 123.5 [88.1–271.2] |
| Held | 52.0 [45–52] | 52.0 [50–52] |
| Blocks lost | 0.01 [0–7] | 0.00 [0–2] |
| spike ÷ compact | 2.20× [1.14–3.70] | 2.60× [1.15–3.64] |
| cheapest ÷ compact | 1.36× [0.76–2.33] | 1.09× [0.48–1.54] |

Three of the four checks passed on the first run: 3–7 neighbours is 95.4 % of blocks (D6's claim), spike still costs 2–3.5× compact on the graph, and a Held block costs 1.22× the lattice's ammo, inside the ±35 % the check allows. The seed distribution is what D6 promised: nearly every block has three to seven ways in, four in five have room for one machine, and no seed produced a city smaller than 305 live blocks or shallower than 16 hops.

**The fourth check was red, and that made the build red.** `D6: every seed validates within its attempt budget` failed at 1 invalid of 10,000, and the whole `--big` run exited 1 behind it. The seed is 3767 and the reason is `no well site: west riverside`: the well placer found nowhere on the west bank for the well the preset asks for, on all eight of the jitter streams the seed was given.

It is not a bad seed. Each attempt reseeds the whole generator, so the eight attempts a seed gets are eight independent draws against a validator that rejects 26 % of them. That rejection rate is measurable from the run itself: the mean seed spends 0.35 rejected attempts, which is the mean of a geometric draw at p = 0.26, and at least one seed in the run spent seven. Eight draws therefore leave 0.26⁸ ≈ one seed in 48,000 with no city at all — an expectation of 0.2 failures over 10,000 seeds, so the check had about a one-in-five chance of going red on any 10,000-seed run. It was flaky by construction, and this run drew the tail. Nobody had measured the rejection rate when the budget of 8 was written.

The fix is **D-R4: `CITY_ATTEMPTS` 8 → 16**, made by recommendation because a red check is a red build and it cannot wait for the gate. Seed 3767 validates at attempt index 8, the first stream the old budget did not reach; 16 streams put a seed with no city at one in 2.3 billion. Nothing else moves: `generateCity` stops at the first stream that validates, so every seed that already had a city keeps exactly the city it had. The proof is the snapshot: `b-compact-seed3.json` (compact, seed 3, three hours, config `5f3417b9`) still matches after the change, so every measured number in this report, in the doc and in the calibration stands. The two rejected options are in `DECISIONS.md`: keeping 8 and restating the check would leave a map the generator's own validator rejects reachable in play, and relaxing the well search would move a well on every seed and throw away the rework's evidence.

The experiment was changed too. `E-variance` now records `invalidSeeds` and names them in the check's detail line, so a future red run says which seed to look at instead of sending the next reader to scan ten thousand of them by hand, which is what this one cost.

The rerun at 16 is green: `0 invalid of 10000`, and every other number in the two tables above came back identical — 4.908 mean neighbours, 95.40 % with 3–7, 339 [305–372] live blocks, the same slot shares, the same 1,000-seed ammo figures. The one number that moved is the attempts row, whose maximum went from 7 to 8: that is seed 3767 taking the stream the old budget denied it.

## 5. Calibration deltas (compact, spike, cheapest; seeds 3, 4, 5; 3 h; targets C1–C7)

Three runs: the archived lattice (`docs/experiments/lattice/calibration.md`), the city with the bots teleporting (`calibration-nowalk.md`) to separate the graph from the walk, and the city with the bots walking (`calibration.md`, the new canon).

| Measure (compact unless said) | Lattice | City, no walk | City, walking |
|---|---|---|---|
| First enclosure (min) | 60 / 60 / 60 | 60 / 46 / 45 | 60 / 46 / 45 |
| First amber (min) | never ×3 | never / 178 / never | 31 / 16 / 16 |
| First red (min) | never ×3 | never / 180 / never | 31 / 16 / 16 |
| Held at 60 / 120 / 180 | 4 / 16 / 28 ×3 | 4/16/28, 4/16/26, 4/16/28 | 4/16/28, 4/16/25, 4/16/28 |
| Lost, stalls | 0, 0 ×3 | 0/0/0, 0/2/0 | 0/1/0, 0/2/0 |
| Magazines at 3 h | 2,306 / 2,214 / 2,442 | 2,441 / 3,626 / 2,037 | 2,407 / 3,480 / 1,951 |
| Spike: first red | 0 min ×3 | 66 / 69 / 66 | 31 / 16 / 16 |
| Spike: distinct blocks lost (falls) | 2 (24) ×3 | 3 / 3 / 2 (24) | 3 / 3 / 2 (24) |
| Cheapest: stalls, ammo ratio to compact | 0, 1.14 / 1.12 / 1.29 | 2/2/3, 1.44 / 0.96 / 2.05 | 1/2/2, 1.42 / 0.96 / 2.07 |
| Targets | C1–C7 met | C1, C2, C4–C6 met; C3 (seed 4 stalls), C7 missed | C4–C6 met; C1, C2, C3, C7 missed |

**What moved, and why.** The graph alone: enclosure comes earlier (a face with 3–7 neighbours closes a pocket in fewer claims), the spike bot's first red moves from minute 0 to 66–69 because a 4-neighbour HQ now starts with every hopper full (GA-R10), seed 4 stalls twice at 170 min on the wire recipe, and the cheapest bot's ammo ratio spreads to 0.96–2.05 because the cheapest edge on a graph can be a long ridge. **The walk alone:** first amber and first red move to 16–31 minutes on every bot. That is the claim kit walking from the chest: a block claimed at minute 15 has unkitted edges until the engineer arrives, so its pips read amber then red for the walk's length. Nothing falls to it (lost 0/1/0, and seed 4's one loss is the wire stall), and it is the walking cost the brief asked to be real; but it means C1 and C2 as written ("no red before 120 min") cannot hold with walking, which is decision 2. The 27-minute HQ loss that every city run showed before the copper fix was the district, not the walk (the no-walk run lost it too).

## 6. Step 5, the game

`packages/game/src/cityMapScene.ts`: the polygon map (canvas repaint every 150 ms, faces by state and rot tier, 1-px white rim on interior faces, streets and water, front ridges red or `KIT_WAIT` brown while unkitted, pips at segment midpoints, well rings, bloom arcs, facility icons, survivors, the engineer). The side panel is cut to held / front edges / interior, magazines made and demanded per minute, magazines in stock, blocks lost, empty hoppers, the clock; stock, line, ring order, skyline and summary sit behind the backquote key. **M** toggles map ↔ world. Headless check (seed 3, River city, compact bot walking, 3 h at speed): held 28, 20 front edges, 0 unkitted, 0 lost, engineer at block 360 after 302 tiles walked with the truck found, 60 fps, console clean; `scratchpad` screenshot matched the §18 25-hour image's palette. §18's three examples are `docs/section18-{10min,5h,25h}.png` from `npm run section18` (seed 3, the locked cadence, production off as E8's caption): 1 / 4 / 0 at 10 min, 52 / 27 / 38 at 5 h, 292 / 33 / 270 at 25 h, nothing lost, 100,103 magazines.

## 7. §26 recount

**Three systems, 5 / 10, unchanged.** (1) belts, inserters and machines; (2) the front rule; (3) found tech as a map. The engineer is presence (a position, a reach and a pocket, which a Factorio player already holds; the walk is a cost inside system 1 the way distance is). The rifle is a tool that fires the turrets' rounds at the turrets' rules and has none of its own. The street graph changes the geometry system 2 runs on, not the rule. The count reaches four if the rifle grows rules (aiming, ammo types, cover, shades) or the engineer grows needs; both sit in §22 as not simulated. Stop condition not met. Recounted again after D-R4: a bigger rejection-sampling budget is a generator setting, not a rule the player holds in their head, so the count is still three.

## 8. Three things a human decides before the slice is rebuilt, and one already taken

| ID | Decision | Options | Recommendation |
|---|---|---|---|
| D-R1 | **The rifle and the shade.** E-rifle: a whole belt lost for 90 s falls to a shade at an unlit edge, rifle or not; a single dry edge holds either way. | (a) keep D5 as written: the rifle is for crawlers, the lesson is "light the edge" (§5's text); (b) a hand lamp on the engineer (2-tile radius) so a shade at arm's reach is lit and targetable; (c) the rifle hits shades. | **(a)**, with (b) as a slice M5 experiment only if Gate B testers lose a block to a shade while standing on it. (c) makes the rifle the defence (§24 risk 10). |
| D-R2 | **The kit walk and C1/C2.** Walking moves first amber and first red to the claim minute (16–31 min) on every bot because the kit walks from the chest; C1 and C2 cannot hold as written. | (a) keep the walk: the pip ladder at the claim minute is the walking cost the brief asked for; rewrite C1/C2 to "no red on a kitted edge before 120 min"; (b) a claim ships its kit (the Line truck delivers, the engineer walks empty-handed); (c) the kit is pre-stocked on the claimed block's substation. | **(a)**; (b) is the truck's later life and hides the tedium row the truck exists to fix; (c) hides it entirely. |
| D-R3 | **The HQ's copper.** The city HQ is civic; without copper the HQ falls at minute 27. The block sim now gives the HQ lot 3,840 copper at 32/min (the lattice's accidental supply); the tile layer's patch is 1,200 (D1's ~700-unit coal patch is the same kind of thing). | (a) 3,840 in the block sim and the tile layer's 1,200 for the slice, reconciled at M6 with D-P4-4 from the played hour; (b) 1,200 in both, and the calibration's claim cadence slows once the patch is out; (c) the HQ face is residential (copper rubble), no patch. | **(a)**; the two numbers are what the two layers can measure today, and M6 is where the hour's real copper bill is counted. |
| D-R4 | **The generator's attempt budget** — already taken, because the alternative was shipping red. One seed in 10,000 (3767) had no valid city after eight jitter streams; the validator rejects 26 % of streams, so eight of them leave ~1 seed in 48,000 with no city. | (a) `CITY_ATTEMPTS` 8 → 16; (b) keep 8 and restate the check as "at most 1 in 10,000"; (c) relax the well-site search. | **(a), applied 2026-09-04.** It changes no seed that already validates, so every number here stands. (b) leaves a map the validator rejects reachable in play; (c) would move a well on every seed and throw the rework's evidence away. Reopen it at Gate B if 16 is the wrong shape of answer. |

Also open, not blocking: the plazas' value (GA-R3; E6 on the city says inert-hugging is no cheaper than compact, so a plaza is a wall the player did not pay for and nothing more) and whether the truck should be an hour-one find (GA-R5).

## 9. Checks

`npm test` 88 / 88; `npm run typecheck` (includes the game build) green; `npm run lint` green; `npm run snapshot` regenerated (`b-compact-seed3.json`, config `5f3417b9`, the copper change) and `snapshot:check` green; `npm run docsync:check` green; `npm run section18` and `npm run seeds` regenerate the images (seeds 3, 4 and 5, none of them touched by D-R4). The temporary `dbg.ts` was deleted.

`npm run experiments -- --big` was run twice. The first run was red on E-variance's attempt-budget check and is written up in §4; the second, after D-R4, is the one every number in this report is taken from: **12 experiments, 3,181 s, 0 failing checks** — E1 2/2, E2 19/19, E3 5/5, E4 4/4, E5 3/3, E6 3/3, E7 3/3, E8 3/3, E9 3/3, E-rifle 5/5, E-walk 3/3, E-variance 4/4. E-variance is 3,137 s of that: 2,367 s to generate 10,000 cities and 770 s to simulate 1,000 of them for five hours.
