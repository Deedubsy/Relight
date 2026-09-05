# Relight — the programme map

Living file. It answers three questions on one page: **where the programme is, what phase that is, and what is left.** Everything here is a summary of something else and cites it: the phases and their DoDs are the programme constitution's; the detail per phase is `PROGRAMME_STATE.md` (one entry per phase, newest first); the human decisions are `DECISIONS.md`; the parked work is `DEFERRED.md`; the spec the whole thing serves is `RELIGHT-design.md`.

Written 2026-09-04, at the end of the D5/D6 rework. Update it at every phase gate, alongside `PROGRAMME_STATE.md`.

## Where we are, in one paragraph

**Phase 4 of 14 — the vertical slice — in progress, and reopened.** Phases 0–3 are complete and Gate A is passed. Phase 4 built three of its six milestones (M1 Ground, M2 Flow, M3 Defence) on the 24×24 lattice; the D5/D6 rework then replaced the player (a cursor with a global stock → an engineer on foot with pockets, a reach and a rifle) and the map (a lattice of squares → a street-first city of irregular blocks), which leaves those milestones' sim and rendering code good and their *slice* no longer the thing Gate B scores. The rework is finished, green and reported (`REWORK_REPORT.md`); the slice is being rebuilt from `docs/relight-prompt-B-vertical-slice.md` — six milestones again: **M1 Ground and the engineer is built (2026-09-04)**, M2–M6 are not. **Ten and a half phases and two gates remain.** No phase after 4 has been opened, with one exception: the rework pulled most of Phase 9's city generator forward by four phases.

## 1. The fourteen phases

| # | Phase | Nominal | State | Evidence |
|---|---|---|---|---|
| 0 | Inventory and doc lock | 3 days | ✅ **done** 2026-09-03 | `PHASE_0_REPORT.md`, `PROGRAMME_STATE.md` §0 |
| 1 | Headless front sim | 1 wk | ✅ **done** 2026-09-03 | `PHASE_1_REPORT.md`; `packages/sim`, E1–E9 |
| 2 | Map-view prototype → Gate A | 2 wk | ✅ **done** 2026-09-03 | `PHASE_2_REPORT.md`; `packages/game`, calibration |
| — | **Gate A** (human) | — | ✅ **passed** 2026-09-03 | `TEST_RESULTS.md` `verdict: go`, owner, no tester sessions |
| 3 | Absorb Gate A | 3 days | ✅ **done** 2026-09-03 | `PHASE_3_REPORT.md`; 19 `[play: Gate A]` tags |
| 4 | **Vertical slice: §11, minutes 0–60** | 6 wk | ◐ **in progress** — lattice M1–M3 built, superseded by the rework; prompt B M1 built, M2–M6 not started | `SLICE_REPORT.md`, `REWORK_REPORT.md`, `docs/relight-prompt-B-vertical-slice.md` |
| — | **Gate B** (human plays the hour) | — | ⬜ not reached | scored in `SLICE_REPORT.md`, `verdict: proceed` |
| 5 | The factory, complete | 6 wk | ⬜ not started | E10 chain throughput, E11 coal, E12 tram |
| 6 | Threat, complete | 4 wk | ⬜ not started | E13 enemy purpose, E9-full, E14 hulk niche |
| 7 | Found tech | 3 wk | ⬜ not started | E15 reachability, E16 survivor pull |
| 8 | Territory tools | 3 wk | ⬜ not started | E17 actions per block |
| 9 | The city | 4 wk | ◐ **mostly pre-built by the rework** — generator, presets, validator and seed browser exist; E18/E19 owed | `packages/sim/src/city/`, `npm run seeds`, E-variance |
| 10 | Progression and endgame | 4 wk | ⬜ not started | E20 full run, E21 quiet city |
| 11 | Performance and scale (‖ 12) | 4 wk | ⬜ not started | E22 megabase; **engine gate** |
| 12 | Interface, onboarding, art, sound (‖ 11) | 8 wk | ⬜ not started | E23 cold player |
| 13 | Balance and Steam | 6 wk + | ⬜ not started | E20 × 1,000 seeds a change; three human rounds |
| 14 | Launch | ongoing | ⬜ not started | — |

The nominal column is the constitution's own calendar and nothing has been measured against it: Phases 0–3 are budgeted at about four weeks and took two days. Remaining nominal work is ~44 weeks with 11 and 12 in parallel. Treat both numbers as shape, not schedule.

## 2. Phase 4 exactly

### Built and kept

- **The block sim on a street graph** (`packages/sim/src/city/`, `graph.ts`): the generator lays streets first, blocks are the irregular faces, adjacency is a shared segment ≥ 5 tiles, districts are hop bands, slots and rubble scale with area. Five presets. 88 tests.
- **The engineer at block level** (`engineer.ts`, `bots.ts`): reach 8, 40-stack pockets, the Depot chest, the truck at the Tram depot, the rifle, retaliation only, 100 HP, respawn at the HQ.
- **The map view** (`packages/game/src/cityMapScene.ts`) as the polygon city, side panel cut to held / front / interior, **M** toggles map ↔ world.
- **The tile, flow and defence layers from the lattice milestones** (`tiles.ts`, `flow.ts`, `worldScene.ts`): 32×32 cells, belts at 7.5/s, inserters, the Mk1 Shot assembler, turrets with 50-round hoppers, Generators, lamps, poles, the §14 shed order. All of it works and none of it has been ported to faces yet.
- **12 experiments green** (E1–E9 retagged on the city, plus E-rifle, E-walk, E-variance), the calibration re-run with and without walking, `docs/section18-*.png` rendered from the sim.

### Owed — the six milestones of prompt B

| M | Milestone | The one-line job |
|---|---|---|
| M1 | Ground and the engineer | **Built 2026-09-04.** The tile layer on rasterised faces (`ground.ts`); the engineer on the tiles (`walk.ts`: WASD, click-to-walk, the map's click, reach 8, pockets and the Depot chest on I); the three stubs gone; the 1 h at 4× soak held the hour with no frame over 50 ms at 44 fps on the headless host — the DoD's 60 fps is not met there, reported as measured (`SLICE_REPORT.md`); two block-sim fixes from the soak moved the seed 4/5 regression fixtures (a rules change) |
| M2 | Flow on irregular lots | Place every machine from the pockets, within reach, on a face that is not a rectangle; measure what actually fits against the area-derived slot count |
| M3 | Defence on the segment | Turrets, lamps, substations, poles and power on street segments; an edge is a segment, and its empty hopper reddens the same pip |
| M4 | Threat, and the rifle | Rot and blooms at tile level, crawlers on a per-face flow field, retaliation only, the rifle as the minute the belt is late |
| M5 | Light | The 800×800 light texture, burn-off sweeping along the claimed face's ridges — built last so the payoff lands on a working block |
| M6 | The hour | A bot walks §11's minute list with telemetry on; every divergence is a finding; then a human plays |

Then **Gate B**: a human scores the §19 first-hour test and writes `verdict: proceed` in `SLICE_REPORT.md`. Gate B gains two rows the rework added — the minute of the first rifle shot and whether it mattered (replayed with the rifle off), and minutes walked against §19's 15 % budget.

## 3. What each remaining phase still owes

- **Phase 5 — the factory.** §12–§14 in full: every resource, intermediate and recipe generated into `recipes.ts`; the §13 machine list and nothing outside it; undergrounds, splitters, three inserters, chests, trams; rubble as finite typed ore with visible depletion; power complete. E10 chain throughput, E11 coal depletion, E12 tram sufficiency. **DoD:** a bot builds the 5 h §18 territory from a blueprint at doc rates, and a human builds a two-assembler ammo line unaided in ten minutes.
- **Phase 6 — threat.** Three enemies exactly and no fourth; rot at tile level with every §5 rule; the fall, retake and machine loss; Cannons and shells; wells with the four-neighbour kill and the Relight surge. E13, E9-full, E14.
- **Phase 7 — found tech.** Facilities restored by claim plus a belt delivery; survivors joining on Held; the HQ survivor panel as the entire tech screen; no research menu. E15 reachability over 10,000 seeds, E16 survivor pull.
- **Phase 8 — territory tools.** The §19 tedium audit's removal schedule on time: Foreman kits, the Line truck, blueprints, one-action retreat, the ring editor at tile level. E17 actions per block, before and after each tool.
- **Phase 9 — the city.** *Mostly built early.* The generator, the five presets as parameter sets, the validator with its measured 26 % reject rate, facilities and survivors placed by band, and the seed browser (`npm run seeds` → `docs/seeds/`) all exist and are what the rework ran on. Owed: **E18 seed fairness** (10,000 seeds through compact *and* spike, the spread of first-enclosure and Turbine-hall times stated in the doc as run variance — E-variance measured generation and 1,000 five-hour runs, not this), **E19 preset identity**, and the human DoD of looking at ten seeds and saying what is different about each.
- **Phase 10 — progression and endgame.** The §15 arc; the Relight as decided at Gate A's absorption (banking is intended play, the bank a ~20,000-magazine object, D-P3-4); the difficulty slider and the quiet city as generator parameters and no other difficulty system. E20 full run over 100 seeds, E21 quiet city.
- **Phase 11 — performance and scale.** The 2019-laptop targets: 313 held blocks, 60 live edges, 4,000 belt segments, 200 machines, 60 fps world view, 16× map view, a 25 h save under 20 MB loading in 3 s. E22 megabase. **The engine gate:** if the targets cannot be met, the numbers go to a human who decides on the C#/Godot port.
- **Phase 12 — interface, onboarding, art, sound.** §11 as the entire onboarding until E23 cold player passes 4 of 5; accessibility; save/load with versioning; art direction (a human decision) under §4's legibility constraint; the light map as the finished visual; audio. No mechanic arrives through art or UI.
- **Phase 13 — balance and Steam.** E20 on 1,000 seeds per change, a sensitivity row per constant, a bot per §24 degenerate strategy; three human rounds; the shell chosen on Phase 11's numbers; Steamworks, the store page, a demo of §11 through the first enclosure.
- **Phase 14 — launch.** Next Fest, telemetry against E20's distributions, release. One rule after: **no fourth system.**

Experiment numbering: the constitution's E10–E23 and the doc's own run tags collide (the retired Python `E10-bloom-cadence` is not Phase 5's `E10 chain throughput`). `PROGRAMME_STATE.md` §0.8 carries the map, and `EXPERIMENTS.md` carries both names per row.

## 4. The three counters the programme keeps

- **The untagged set** — doc numbers in §5, §7, §12, §13, §14 and §15 with no `[sim]` or `[play]` tag. **44 at Phase 0 → 41 (Phase 3) → 40 (M1) → 37 (M2) → 33 (M3) → 33 (prompt B M1).** 21 tile-scale, 12 design inputs. The rework added `rework-graph`, `E-rifle`, `E-walk` and `E-variance` tags but did not recount; the next recount is due at prompt B M1. Emptying it is the goal of Phases 1–4.
- **§26 systems** — three (belts/inserters/machines, the front rule, found tech as a map), complexity **5/10**, unchanged at every recount including the rework's. It reaches four if the rifle grows rules of its own or the engineer grows needs; both sit in §22 as not simulated. This is a stop condition and it has never fired.
- **Gates** — the only places the programme waits. Gate A ✅ passed; **Gate B** ahead at the end of Phase 4; the **engine gate** ahead in Phase 11.

## 5. What is waiting on a human right now

| What | Where | Blocking? |
|---|---|---|
| **D-R1** the rifle and the shade, **D-R2** the kit walk and C1/C2, **D-R3** the HQ's copper | `DECISIONS.md` | No — taken 2026-09-04 by recommendation on the human's `go` for prompt B M1; reopenable at Gate B |
| **D-P4-7** hour-one power, **D-P4-8** six start turrets or two, **D-P4-9** turrets on claimed blocks | `DECISIONS.md` | No — settled at prompt B M6 from the played hour |
| **D-R4** the generator's attempt budget | `DECISIONS.md` | No — taken 2026-09-04 by recommendation because a red check is a red build; reopenable at Gate B |
| Gate A tester sessions (**D-P3-9**) | `TEST_RESULTS.md`, `DEFERRED.md` | No — recommended in parallel with the slice; a session that contradicts a `[play: Gate A]` lock moves the lock |
| Phase 1's five-minute smoke test | `PHASE_1_REPORT.md` | No |
| PRs #1–#4 open and unmerged; `main` branch protection refused on a free-plan private repo | GitHub `Deedubsy/Relight`, D-CI | No — CI is green on each |
| The rework itself is uncommitted on `phase-4` (91 paths, on top of `6ce1786`) | working tree | Yes for the record; nothing depends on it to run |

## Changelog

- 2026-09-04 — created at the end of the D5/D6 rework, from `PROGRAMME_STATE.md`, the constitution, `DECISIONS.md` and `DEFERRED.md`. No number here is new; where this file and `PROGRAMME_STATE.md` disagree, `PROGRAMME_STATE.md` is right and this file is stale.
