# Prototype build report — map view (§25 step 2)

Built 2026-09-02/03 against `RELIGHT-design.md` as patched in Phase 1 and Phase 5. Companion files: `PROTOTYPE_TEST_PLAN.md`, `DEFERRED.md`, `FRONT_FIX_REPORT.md` (section 6 has the Phase 5 runs).

## 1. What was built

### Layout and how to run it

| Path | What |
|---|---|
| `frontsim.py` | Python sim, still the reference model. One knob added (`gap_after`, default 8 min). |
| `export_fixtures.py` | Writes `packages/sim/fixtures/seed{3,4,5}.json` from the Python sim (map cells, both maps, four policies, 5 h). |
| `packages/sim` | TypeScript port of the sim. No engine, no DOM, no import from `packages/proto`. |
| `packages/proto` | Phaser 3.90 + Vite 6 map-view prototype, one screen. |
| `phase5.py`, `phase5b.py` | Phase 5 runners (E9–E13, E11b) → `phase5_results.json`, `phase5b_results.json`. |

```
npm install
npm test                      # sim regression fixtures (28 tests)
npm run dev                   # prototype at http://localhost:5173/?seed=3
npm run build                 # tsc --strict on both packages, vite build
```

URL parameters: `seed=N` (map), `economy=0` (rubble and costs off), `scatter=0` (open-ground map), `autoplay=compact|balanced|spike|cheapest` (a sim policy plays; for testing only), `player=name` (goes into the telemetry meta). Keys: space pauses, 1/2/3 set 1×/4×/16×. `window.__relight` exposes the session, `run(ticks)`, `summary()` and `exportJson()` for scripted checks.

### The sim (`packages/sim`, 1,220 lines)

- `prng.ts` — the Python sim's hash and half-to-even rounding, bit for bit.
- `map.ts` — districts, wells, the 9 % scattered inert map, facility placement, `generateMap(seed, cfg)`.
- `sim.ts` — state, `step`, `advance(state, realSeconds, commands)`, commands (`claim`, `ringOrder`, `addAssembler`, `setSpeed`), events (`claim`, `claim-rejected`, `held`, `bloom`, `fall`, `sub-off`, `sub-on`, `reorder`, `assembler`, `hour`), the ammo ring with hoppers and the unfed-substation rule, the economy tick (proto only, off in the regression).
- `bots.ts` — the four policies as command generators.
- `queries.ts` — `claimInfo` (the tooltip), `frontList` (edges with ring position, hopper level and pip), `hud`, `ammoStatus`, `shapeMetrics`, `facilityList`, `nearestHeld`.
- `types.ts` — state and config shapes; state is plain JSON and round-trips (tested).

Not ported, on purpose: the power model (Phase 1 decision), `--asm-track`, the experiment harness and its analytics, and the Python CLI output. The `validator` field (`none`, `no2x2`, `maxrun2`, `inert-dark`) is ported so E13's reading can be switched on.

### The prototype (`packages/proto`, 800 lines)

- `session.ts` — URL parsing, share link, the proto config, the frame loop (real seconds → `advance`, events → telemetry, per-sim-minute rows).
- `mapScene.ts` — the 24×22 grid at 24 px, block states, rot mottle by tier, well throb, awake-timer arc, contested flicker, interior border, unfed red square, hover outline, claim tooltip, pole lines, front bars and pips, bloom pulses (crawler-sized, violet second ring for shades, white diamond for hulks, wider and longer for wake blooms), fall pulses, facility silhouettes and labels.
- `panel.ts` — territory stats, ammo made against demanded, stock, blocks lost, empty hoppers, sim clock, speed buttons, rubble and production with a Build assembler button, the ammo ring order list with drag-and-drop reorder, nearest facilities, session summary, Export telemetry JSON.
- `telemetry.ts` — every claim (sim time, block, district, rot, F and interior before/after, retake, wake crawlers, player or bot), every rejected claim, every block lost, every reorder, every assembler, every speed change, a row per sim minute (held, front, interior, contested, lost, bounding box, aspect, perimeter, perimeter/area, production, demand, stock, empty hoppers, assemblers, copper, steel), first enclosure, hourly rows; `summarise` gives claims per hour, shape metrics, ammo spent, blocks lost, reorders.
- `main.ts` — boots Phaser, turns events into toasts, keyboard, dev hooks.

Each claim goes through the sim's `claimInfo` for the tooltip and the `claim` command for the click; the prototype holds no game state of its own beyond the telemetry log and the drawing caches.

### Verification done

| Check | Result |
|---|---|
| `npm test` | 28/28: 24 fixture comparisons (seeds 3/4/5 × open/scattered × four policies, 5 h) plus four unit tests. Compared exactly: magazines, hourly held/front/interior, first-interior tick, blocks lost with the loss log, shells. |
| `tsc --strict` both packages, `vite build` | Clean. Bundle 1.5 MB (Phaser). |
| Bench | 18,000 ticks in 0.285 s (63k ticks/s; 103k on an idle machine). 16× needs 960 ticks/s. |
| Browser smoke test (headless Chromium, DevTools protocol, seed 3) | First frame correct (grid, river, scattered inert, wells, five facilities, start block with three green pips). Hover on a Dark block shows `Claim — rot 25 % · front +2 · closes 0` from `claimInfo`. Click claims, block goes contested then Held, copper and steel deducted, pole line drawn, telemetry row written. Drag-and-drop moved an edge to the top of the ring and the sim's ring followed. Pip click highlighted the edge and toasted its ring position and hopper level. Build assembler deducted cost and production went 20 → 40 mag/min. Speed keys logged. |
| 16× session, compact bot, seed 3, 2 h 47 min sim | No console errors (Phaser banner, Vite HMR and a favicon 404 only). Export is 297 kB and parses; summary: 17 claims (6.1/h), 18 Held, 11 front, 10 interior, 0 lost, first enclosure 1:40:36, 1,643 magazines spent. |
| Second person, same link | A fresh isolated browser context opened `?seed=3`: same static map hash, same 69 inert cells, same five facilities, no console errors. |
| 4× session, balanced bot, seed 3, two hours sim | 2:05 sim in 31.7 real minutes (4.0×), no console errors, export 281 kB and parses; balanced held 7, lost 6, 12 claims, 2235 mags, no enclosure. This run predates the calibration (CALIBRATION_REPORT.md): 8-minute cadence, 20 mag/min start assembler, magazines free. |

Not done: nobody has played it with a mouse; touch is not handled; only headless Chromium at 1200×900 was used; no Firefox or Safari.

## 2. Assumptions (every `PROTO-ASSUMPTION` in code or plan)

Each one is a gap in the doc; the question it stands in for is in italics. **Phase 2 (2026-09-03): every tag in code is now `GAME-ASSUMPTION` (constitution rule 6); the list, with four more, is `PHASE_2_REPORT.md` "Assumed".**

1. **Rubble yield** (`packages/sim/src/sim.ts`, economy tick; `types.ts`): one rubble per Held block per minute, flat, by district — civic gives stone, residential copper, industrial steel, outskirts nothing. *What does a block yield, at what rate, and does it deplete? (`FRONT_FIX_REPORT.md` open question 4/9.)*
2. ~~**Magazines from nothing**~~ — **removed by the calibration** (`CALIBRATION_REPORT.md`). Magazines consume the §12 recipe, 2 steel + 1 copper each (`sim.ts`, ring block); an assembler makes only what the stock pays for. Doc-derived, not an assumption; the number is kept so the references below stay valid.
3. **Stone has no sink** (same place). *Walls and foundations are world-view features (`DEFERRED.md`).*
4. **Claim cost** (`types.ts`): 10 wire = 5 copper, 5 frames = 10 steel. *Wire and frame recipes.*
5. **Assembler cost** (`types.ts`): 20 copper, 40 steel. *§13 machine costs.*
6. **Start stock and start patch** (`types.ts`): 40 copper, 80 steel, plus a start-block patch of 240 steel at 2/min, which is §19's "the start block's steel runs out in ~2 hours" read literally. *What the start block actually contains.*
7. **Proto config** (`packages/proto/src/session.ts`): production on, the doc's ring and unfed rule (N = 40), hopper 100, 300 start rounds, jitter 0.1, economy on, one assembler at t = 0 and no schedule; the player builds the rest. *The doc's assembler schedule is a sim stand-in for player behaviour; the prototype needs the player to do it.*
8. **Pip per edge** (`packages/sim/src/queries.ts`): the doc's pip rule is for a block's hoppers as a set; the prototype applies it to one edge's hopper. *Is the pip a per-block or per-edge signal?*
9. **"closes N"** (`queries.ts`): Held blocks that become Interior if this block were Held, the claimed block included. *§19 and §27 use the phrase; nothing defines it.*
10. **Claim telemetry "after" values** (`sim.ts`, claim): F and interior after are projections at claim time, not the values when the block turns Held. *The brief's telemetry list does not say which.*
11. **Shape metrics** (`queries.ts`): perimeter counts Held sides facing Dark, Contested or void; inert and the map edge are walls; area is Held blocks. *No shape metric exists in the doc.*
12. **Facility placement** (`packages/sim/src/map.ts`): Foundry at TARGET; Arsenal, Turbine hall, Refinery, Power station seeded into plausible distances and octants per §8/§17. *§17 gives ranges, not a placement rule.*
13. **Facility "reached"** (`sim.ts`, held event): when the facility's own block turns Held; the prototype toasts and nothing else. *What reaching a facility does is world view.*
14. **Distinct shapes** (`PROTOTYPE_TEST_PLAN.md`): perimeter/area differing by more than 0.15 or bounding-box aspect by more than 0.3. *No threshold exists anywhere; picked so that a 3×6 and a 4×5 territory count as the same shape.*

## 3. Regression results

The TypeScript sim reproduces the Python sim exactly on the 24 fixture runs (three seeds, both maps, four policies, five hours): total magazines, hourly held/front/interior rows, first-interior tick, blocks lost and their log, shells. Serialised state continues identically after a JSON round trip. The Python sim is unchanged apart from the `gap_after` knob; `frontsim_before_phase2.py` in the scratchpad was the pre-port copy used for the diff.

## 4. Deferred

`DEFERRED.md` lists twelve things that were tempting and are out by rule: world view, belts and the ammo line as a physical thing, machines on a lot, the light map, power, trams, blueprints, Line truck and Excavator as units, Cannons and shells as a line, the Relight itself, art beyond flat colour, stone sinks. The two that hurt most in the prototype: the light map (Held amber on Dark navy is the only visual payoff there is) and belts (reordering a list is the whole ammo model, and it feels like admin).

## 5. Phase 5 results

Full numbers in `FRONT_FIX_REPORT.md` section 6; doc tags applied where settled.

- **E9 claim cadence.** The §18 drawings imply a claim every ~5 minutes after hour one (8 minutes gives 34 and 184 blocks at 5 h and 25 h; 4 minutes gives 64 and 364). Drawings stay; the prototype's claims-per-hour telemetry decides.
- **E10 bloom cadence.** A 240 s timer halves fights per minute and cuts ammo per edge 30 %; a 20 % rot drop keeps the fight rate and cuts ammo 24 %. Feel question.
- **E11 Relight hold.** With wells ×3 alone (the wake-size bloom of every awake block is not in the sim), the ten minutes demand 1.6× the hour before, peak minutes 2.3×, and cost 18–29 blocks in twenty minutes at both 80 and 120 mag/min of production, against 2–4 without the surge. Compact play at the doc cadence is at parity by 25 h whatever the assembler count, so no run banked magazines and none survived the hold.
- **E12 spike on the scattered map.** Lost nothing in five hours on three seeds at 3.2× compact's ammo. A choice, not a trap; §25 item 13 closed.
- **E13 inert as Dark.** Compact and cheapest unchanged; the river-hugging policy pays 21 % more ammo and encloses 1.5–2.5 hours later. §5 stands.

## 6. What is missing or does not work as the design wants

1. **Ammo pressure arrives after the two-hour mark at bot cadence.** At 2 h 47 min the compact bot's front of 11 edges demanded 10.8 mag/min against one assembler's 20, the buffer sat full at 400 magazines, and no pip ever left green. E10's 1.37 mag per edge-minute puts one assembler's break-even at about 15 edges. A tester claiming every 4 minutes reaches roughly 19 blocks and 15 edges at 2 h; slower players never see a red pip. The test plan's reorder criterion (two of five testers reorder) is therefore at risk from the setup, not from the design. Options are a human call: start with less production, run the session longer, use a faster bloom timer, or open at a later state. Nothing was tuned here; the calibration that followed is in `CALIBRATION_REPORT.md`.
2. **The steel wall.** With the economy on, compact and cheapest bots stall at 29 Held with steel at zero once the start patch is gone (about 2 h); only industrial blocks make steel, so a fast tester hits this near the end of the session. The claim cost and yield are assumptions 1, 4 and 6. Run one tester or one control session with `economy=0`. Calibrated afterwards (`CALIBRATION_REPORT.md`): yield 32 per block-minute keeps every bot stall-free to 3 h; the wall moved, it did not go away.
3. **No persistence.** Reloading the page starts a new session and the telemetry is gone; export before closing. Phaser also pauses when the tab is hidden, so the sim clock stops while a tester is alt-tabbed.
4. **The tooltip's "wake bloom ≈ N crawlers" has nothing to land on.** The fight is a pulse on a square; bigger blooms are bigger pulses. Whether the front count reads as a threat at all is what the test is for, but this is the weakest link.
5. **Ring order is a list.** The doc's "last edge starves first" is asserted by the sim's hopper fill order and shown by the pips; the list itself carries no belt.
6. **Autoplay bots are the only play so far.** Everything above about pacing comes from bots at the doc cadence, not from a person.
7. **Shells are counted and never used.** Hulks draw a diamond and add to the shell count; no Cannon line, no cost.

## 7. Three decisions for a human before world view

1. **What a claim costs and what a block yields.** Assumptions 1, 4, 5 and 6 decide whether the session is a shape game or a steel-budget game after the first two hours. The prototype cannot answer this; the choice changes what the test measures.
2. **The claim cadence the game is drawn for.** §18 is drawn at ~5 minutes per claim; the sim and the ammo model were tuned at 8 (the sim now runs at 5, `CALIBRATION_REPORT.md` part 1). Together with the assembler count this fixes when the front rule starts to bite (item 6.1), which is the thing the prototype exists to test. Pick the drawing or the number, then set the test session's length and starting production to match.
3. **Whether the Relight hold is meant to be survived.** E11 says it is a wall at parity. Either banking magazines before the Relight is a designed mechanic (then the ammo line needs a visible stock the player can build, which is world view) or losing 20–30 blocks in the last ten minutes is the intended ending and §16 should say so.

§25 item 14 (power's hour-one role) is already marked in the doc as a decision before world view and is not repeated here.
