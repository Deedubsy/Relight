# Relight — programme state

Living file. One entry per phase, newest first; the current phase is the top one. Rules are in the programme constitution (the prompt that started Phase 0); the spec is `RELIGHT-design.md`; the sim is the judge. `ROADMAP.md` is the one-page summary of this file: the fourteen phases, which are done, and what each of the rest still owes.

**Current phase: 4, reopened by the rework (D5 the engineer on foot, D6 the street-first city; `REWORK_REPORT.md`, 2026-09-04). The lattice slice M1–M3 (`SLICE_REPORT.md`) is not scored at Gate B; the slice is being rebuilt from `docs/relight-prompt-B-vertical-slice.md`: D-R1/D-R2/D-R3 taken by recommendation on the human's `go` (2026-09-04), prompt B M1 "Ground and the engineer" built the same day. prompt B M2 "Flow on the faces", M3 "Defence on the segment", the before-M4 gate (D-P4-7 settled, D-B3-4 built) and M4 "Threat, and the rifle" followed on 2026-09-04, M5 "Light" and M6 "The hour, §11 as rewritten" on 2026-09-04, **verified on the pass of 2026-09-04 (B.15: every check green after three test fixes and one sim fix in `replayVerdict`; `E-hour` played the hour six times — held 3 at 60 on every seed, north falls at 45–58 min, §11's minute-45 machines unaffordable, first red at ≈ 6 min on every seed, the walk-over 3–7 s; the browser's hour on §11's chest could not claim west or north)**. Prompt B's six milestones are built and verified. **The standards audit ran 2026-09-04 (`STANDARDS.md`, `STANDARDS_REPORT.md`, B.16): 46 human-check rows across the phases, five decisions D-SA-1–5 open, the standing DoD block below.** **The economy fix, the layout pass and E-rifle at tile scale built 2026-09-04 (B.17, `SLICE_REPORT.md` "Economy fix"): D-P4-4, D-P4-7 (b), D-P4-8, D-B1-1, D-B5-4 decided by the human by chat message and built (Mk1 6 s / 10 mag/min, Mk2 3 s the purchase; 40 chest coal; six full turrets + 20 magazines; streetlights radius 7); §11 rewritten to two claims in the hour with north at 60–75 — E-hour 10 / 10 on seeds 3 / 4 / 5 with no fall, no brownout, steel ≥ 30; north at 65 falls on coal (D-P4-10, recommended). Unverified: the cheap checks and the two re-runs only.** **T5–T8 were waived by Daniel on 2026-09-05** (`docs/GATE_B.md`): no walkthrough, no two-assembler line, no stranger, no played hour, nothing measured — a decision to proceed without play, not a pass. **T8 was then played in part later the same day and the waiver on it lifted** (B.29): one hands-on session, §19's minutes 0–10 only, verdict `proceed — with conditions`. T5, T6 and T7 stay waived, so D-P4-5/9, D-B1-1 and D-B6-1..3 still lose the evidence source ROADMAP §2 named for them. **The layout and readability pass (`PROGRESS.md` T3) was built 2026-09-05 out of order on the human's instruction (B.21, `LAYOUT_PASS_REPORT.md`): drawing only, ten of ROADMAP §2's eleven items, the lamp cone refused under D-B5-1, cheap checks green and no browser number; D-LP-1/2/3 recommended.** **The blockers task closed T1 on 2026-09-05 (B.23, `ECONOMY_FIX_REPORT.md` §9): D-P4-9, D-HOUR-2 and D-P4-10 decided by Daniel ("Do the blockers") and built — the second steel Excavator at 12, north's claim at 65 past the hour with `HOUR_END.held` 3, and §11's two carried turrets gone — and `npm run experiments` is 13 experiments, 0 failing checks (E-hour 10 / 10, E-rifle 12 / 12), so "make main green" is met and measured. `E-hour-north`: north claimed at 65:00 now stands to 75:00 on all three seeds. Half of D-P4-10 (a), "west's coal made real", could not be built — no coal rubble kind exists — and is asked as D-P4-12, which blocks nothing.** **T2 closed the same day (B.25): E-hour is a no-fall hour on seeds 3 / 4 / 5 with the rifle off and on — 10 / 10, no fall, steel never zero, 0 brownout seconds — and `E-hour-north` carries north to 75:00 on every seed.** **T4 closed the same day (B.27): at tile scale the rifle decides a rescue — 48 runs, 7 falls without it and 4 with, 3 saved and 3 delayed, every fall `unfed` and never a shade — and the tile steady table now scores §19's 10 % shooting and 5 % danger caps on §11's hour (0.50 / 0.69 / 0.06 % and 0.00 %), taking E-rifle to 14 / 14.** **Gate B was partly played on 2026-09-05 and its verdict is `proceed — with conditions` (B.29): STANDARDS row A.7 closed, four rows D-GB-1 … D-GB-4 opened, and D-GB-1 decided as the hybrid — the claim still burns off its block, and powering and lighting the next area becomes an expedition the engineer walks. Nothing was built by the gate.** Next: T9, now unblocked and re-scoped to fold the partly-played hour and D-GB-1 into code and doc. Nothing on the critical path is blocked. **Working mode from 2026-09-04 (user): a `go` is code changes and a light review only; the scripted checks, experiments, calibration and the soak form a separate verification pass the user schedules, and milestone numbers are marked *unverified* until it has run** (`CLAUDE.md`).** D-P3-9/10/11 were made by their recommendations on that `Go` (sessions in parallel, C3/C4 as locked, C8 lock accepted). PR #1 (`phase-1` → `main`), PR #2 (`phase-2` → `phase-1`) and PR #3 (`phase-3` → `phase-2`) are open, unmerged; Phase 4 is PR #4 (`phase-4` → `phase-3`). Phase 1's human five-minute smoke test and the Gate A sessions are still the user's.

Gates passed: **Gate A, 2026-09-03, `verdict: go` by the owner on the bot calibration and the smoke test, with no tester sessions** (`TEST_RESULTS.md` §1). Every constant locked on it is tagged `[play: Gate A]`, a lock rather than a measurement; the sessions can still run (D-P3-9).

Housekeeping the constitution assumes and the repo does not have (a human decides how, not whether):

- ~~`/mnt/e/Factorio2` is not a git repository and has no CI.~~ Done in Phase 1: `github.com/Deedubsy/Relight` (private), branch `phase-1` → PR #1 to `main` (CI green), `.github/workflows/ci.yml` and `nightly.yml`; `main` protection is refused on a free-plan private repo (D-CI, human choice). **Decided (D-CI, 2026-09-03):** GitHub private repo, GitHub Actions, npm workspaces; push CI = lint + `tsc --strict` + fixtures + E1–E9 at three seeds; nightly = 10,000-seed and 25 h runs to `docs/experiments/`; Python sim stays as fixture exporter for one phase, then retired (**done in Phase 3**, D-P3-8). Linear for tasks. Phase 1's first task is to set this up.
- Reports live at the repo root, not in `docs/`. Left where they are (Phase 0 changes nothing else); Phase 1 may move them under `docs/` and leave root stubs.
- `packages/harness` and `packages/tools` exist since Phase 1. `packages/proto` (the Phase 2 map view) was renamed `packages/game` in Phase 4 M1 and holds both views; `apps/steam` does not exist yet (Phase 14).

**Standing DoD rows (every phase from the layout pass on, added by the standards audit 2026-09-04; constitution rule 9):**

- **The `STANDARDS.md` rows for this phase are checked** — each is a one-minute check a human does at the DoD; the phase's rows are listed in `ROADMAP.md` §4 and in the constitution's DoD line. Build-with rows ship with the system they touch; add-later rows may slip a phase with a `DEFERRED.md` line, never silently.
- **No new gap was introduced without a row** — a thing the phase built that a reference game does differently gets a `STANDARDS.md` row (or a `## Considered` entry with its §22 / §23 reason) in the phase report.
- The phase report lists the rows checked, the rows that slipped, and updates `ROADMAP.md` §6's `standards` counter (closed / total).

## Prompt B — the slice rebuilt on the city (Phase 4 reopened, opened 2026-09-04)

**Task list: `docs/PROGRESS.md`.** **Status: M1 Ground and the engineer built 2026-09-04; the four pre-M2 items ("Four small things before Prompt B M2": D-B1-5 direct control, D-B1-4 placement by face geometry, C1/C2 birth artefact, reference machine) done 2026-09-04; M2 Flow on the faces built 2026-09-04; M3 Defence on the segment built 2026-09-04; before M4 (2026-09-04) D-P4-7 settled and D-B3-4 proportional brownout built, the M4 gate (the §11 hour on seeds 3 / 4 / 5 with power on, the E4 schedule, proportional brownout) holds; M4–M6 not started.** Branch `phase-4`, PR #4. Report `SLICE_REPORT.md` (rewritten from the top; the lattice slice is its appendix; the four items are its "Before M2" section). Decisions D-B1-1 … D-B1-3 taken by recommendation on the human's `go` for M1, **D-B1-4, D-B1-5, D-B3-1–D-B3-4 and D-P4-7 made by the human 2026-09-04** (`DECISIONS.md`); D-P4-8 superseded by D-B1-4; D-P4-9 open (M6). **Economy-fix task (Steps 1–5, 2026-09-04/05, `ECONOMY_FIX_REPORT.md`): nine rows decided by the human (D-P4-4, D-B1-1, D-P4-7, D-P4-8, D-B5-4, D-B1-4-rider, D-ENGINE-1, D-INSERTERS-1, D-HOUR-1), the eight disagreements fixed, `docsync:check` green; E-hour re-run on the decided minutes — No-fall hour: no (north at 40 falls on every seed, chest steel zero at 15:02); experiments red (E-hour 6 / 10, E-rifle 10 / 12), so `main` was not pushed — `phase-4` carries it, PR #5.** D-P4-10 and D-HOUR-2 recommended, the human's.

```yaml
reference_machine:            # the only host whose fps is reported against the 60 fps DoD (item 4, 2026-09-04)
  host: the owner's desktop, Windows 11 Home 10.0.26200; the repo and the toolchain run in WSL2 (Linux 6.18), the browser on Windows
  cpu: Intel Core i7-11700K, 8c/16t
  ram: 64 GB
  gpu: NVIDIA GeForce RTX 3070 (driver 32.0.15.9186); Intel UHD Graphics 750 present, unused
  display: 2560x1440 @ 59 Hz
  browser: Google Chrome (Windows), GPU-accelerated; the soak attaches over CDP (`soak.cjs` with `SOAK_CDP`)
  headless: Chromium + swiftshader in WSL2 is reported as "headless, for regressions only", never against the DoD
```

### B.1 M1 Ground and the engineer — what changed

- **`packages/sim/src/ground.ts`** (new, run name `B-M1-ground`) — the tile layer as a pure derivation of `CityGeom`: rasterised faces as lots, 6–12-wide street ridges, the river, plazas and parks inert; 800×800 tiles in 625 chunks with `blockKey` / `chunkKey` for the renderer; rubble by district in three clusters, 250–350 tiles scaled by area; substations and streetlights placed by the face geometry (GA-B1-1/2). 42–117 ms cold a city, 0 ms cached. `tiles.ts` (the lattice layer) stays as the HQ layout's source and behind `?map=lattice`.
- **`packages/sim/src/walk.ts`** (new, run name `B-M1-walk`, six tests) — tile A* over `passable` (belts and poles walked through, GA-B1-3), `tickEngineerTiles` at 6 tiles/s with `engineer.block` kept in step so D5's kits, restock and rifle fire unchanged; `walkTo(block)` and `move(x, y)`.
- **`packages/sim/src/engineer.ts`, `flow.ts`** — pockets of 40 stacks (stack sizes GA-B1-4), the Depot as the 6×6 chest and workbench (`chestTake` / `chestPut` / `chestCount` / `nearDepot`), hand-mining to the pockets within reach 8, §11's start chest 200/100/50/20 in the game and the calibrated 80/40/0 in the harness (GA-B1-7); kits free from the chest, one a claim (GA-B1-5/9).
- **Two block-sim fixes found by the first city soak** (`SLICE_REPORT.md` Built): `flow.ts` `hookSyncEdges` no longer makes every HQ edge a turret edge — a city HQ segment the six start turrets do not cover keeps the ring-fed stand-in hopper (GA-B1-14; the HQ was lost at sim minute 20 with the line running); `sim.ts` `rebuildRing` starts the HQ's own edges kitted at t = 0 with `walk` on (GA-B1-15; a human with no bot fired nothing). Test added in `defence.test.ts`. The second fix moves the D6 regression fixtures `city4.json` / `city5.json` (seed 4: held 50 → 52, lost 2 → 0 over five hours; seed 5: walking minutes only) — a rules change under constitution rule 1, regenerated with `_exportCity.ts`; `city3.json`, the snapshot `5f3417b9` and the calibration (`calibration.md`, re-run) do not move; `E-walk`'s hour-3 walking minutes drop to 2.2 (check 0–10).
- **`packages/game`** — `worldScene.ts` chunked Blitter over the ground, the engineer with reach ring and path line, WASD and click-to-walk, camera following from minute 0, reach-refused tools with "walk closer"; `cityMapScene.ts` click-to-claim-and-walk, the engineer's block marked; `panel.ts` pockets on **I**, chest take/put; `main.ts` / `session.ts` the flow layer on for every session unless `?flow=0` (GA-B1-13); **M** toggles the views, **E** toggles nothing; `?walk=1` and the three stubs gone; `__relight` gains `walkTo(x, y)`, `engineer()`, `ground()`, `describe()`, `chest`, `togglePockets()`, `world.drawMs()`. `bots.ts` `walkingBot` unchanged at block level; the game drives the same `walkTo`.
- **Measured** — HQ → Tram depot → HQ on foot (20 s sim a leg, no stutter, no frame over 50 ms); the 1 h at 4× soak on seed 3 with the §11 line (`soak.cjs`, headless Chromium, swiftshader): 901.6 s real, sim 1:30:05, 39,378 frames, 43.7 fps, worst 48.3 ms, 0 frames over 50 ms, held 4 / lost 0 at the hour, one claim lost at 1:29:42 with the coal at 0. The DoD's 60 fps is not met on this host: 91.7 % of profiler samples are the software renderer, JS ≈ 1.6 ms a frame; the lattice M3's 60 fps flat (2026-09-03) is the baseline a GPU host has to match. Reported as measured, not waived.
- **Doc**: §14 passable belts and poles and §4 the key bindings, changelog lines `B-M1-walk`; the five disagreements in `SLICE_REPORT.md` (chest vs calibration start, passability, kits' price, substations by geometry, bindings).
- **Checks**: `npm test` 95/95, typecheck (incl. the game build), lint, `snapshot:check` (`5f3417b9`), `docsync:check`, `experiments` 12 / 0 failing — green.
- **Deferred** re-read (`DEFERRED.md` "Re-read at prompt B M1"): the lattice tile layer and `?map=lattice` → Phase 5 gate; sprite and tileset art → Phase 12; hand-crafting from the pockets → M2; kits by hand and the walk-back tedium → M6; the truck as a vehicle → Phase 5; start turrets on segments → M3 (D-P4-8; since decided by D-B1-4 before M2); A* vs graph distance → Phase 11.

### B.1a Before M2 — the four small things (2026-09-04, `SLICE_REPORT.md` "Before M2")

- **D-B1-5 direct control** (the human's decision, run `B-M1-body`): `engineer.ts` sprint/stamina/dodge/rifle constants (GA-B1-19/20/21), `walk.ts`, `worldScene.ts` hotbar 1–9 with the rifle at 9 (GA-B1-22), `panel.ts`, `telemetry.ts` shooting ≤ 10 % and danger ≤ 5 % guards; click-to-walk removed from the world view; `body.test.ts` four tests. Doc §4/§11/§19/§22/§23.
- **D-B1-4 placement by face geometry** (the human's decision, run `B-M1-start`): `ground.ts` substation nearest the centroid, lamps every 4 tiles along each kerb (`FACE_LIGHT_STEP`, GA-B1-2), `segLength`; `flow.ts` `startTurrets` one per 16 tiles of segment (`TURRET_PER_TILES`, GA-B1-14), at least one a segment, a corner sliver served by the turrets that reach it (`edgeTurrets`, GA-B1-17), pads and per-edge prefill (GA-B1-16); `sim.ts` a covered edge is born kitted, the M1 HQ-at-t=0 branch removed from every tiled state (GA-B1-15 is now the block-only fallback). Seeds 3/4/5: every live HQ segment covered (6 / 7 / 5 turrets); the minute-20 soak holds through sim minute 33 with the §11 line, no HQ branch. Doc §5/§13; D-P4-8 superseded.
- **C1/C2 birth artefact** (run `B-M1-born`): the M1 report quoted a stale `calibration.md`; HEAD already read MET. Built by the rule anyway: `engineer.ts` `bornFed` (a kitted edge is born fed, GA-B1-18), `Edge.born`, `calibrate.ts` never reads a birth-tick pip. Clean numbers: C1 MET (enclosure 60 / 46 / 45 min, first amber never / 178 / never), C2 MET; only seed 3's spike row moves (2,316 → 2,291 magazines). D-R2 stays closed.
- **Reference machine** (run `B-M1-ref`): the `reference_machine:` block above; `soak.cjs` attaches over CDP (`SOAK_CDP`) to the Windows Chrome; the M1 soak measured there once (900.8 s real / sim 1:30:03, 53,921 frames, **59.9 fps**, worst frame 26.7 ms, 0 over 50 ms — the 60 fps DoD holds; the headless M1 row was 43.7 fps / worst 48.3 ms). Headless swiftshader fps is "for regressions only" from here on.
- Checks: `npm test` 99/99 (95 + `body.test.ts`'s four), `typecheck` (incl. the game build), `lint`, `snapshot:check` (`5f3417b9`; the snapshot regenerated twice for new fields only — the engineer's stamina/dodge/aim state and `Edge.born` — no number moved), `docsync:check`, `experiments` 12 / 105 s / 0 failing checks — green. Fixtures `city{3,4,5}.json` unchanged; the snapshot `5f3417b9` unchanged.

### B.2 Untagged recount at prompt B M1

33 → **33**. M1 added no `[sim]` tag to an untagged number: the ground and the walk are tagged where the rework already tagged them (`rework-graph`, `E-walk`); §14's passability line is new text, not one of the 33. §26 status unchanged: three systems, 5 of 10 gate criteria measurable. C1/C2: the M1 text here said they still read **MISSED** at the claim minute; that was a stale `calibration.md` — the clean re-run (B.1a item 3) reads **MET** on all three seeds with first amber never / 178 min / never, and D-R2 stays closed. §26 recount after the four items: still **33**.

### B.3 M2 Flow on the faces (2026-09-04, `SLICE_REPORT.md` "Prompt B M2")

- **From the pockets, within reach** (run `B-M2-pockets`): `flow.ts` `canPlace` / `place` pay a machine's rubble price from the pockets or drop a carried one free (`carried`), never the Depot chest; the hand hook's `place` is reach-checked and refuses with the cursor ("Walk closer"), nothing moved; `canPickUp` / `remove` return a machine and what it held into the pockets, all or nothing, a full pocket refuses with a toast (GA-B2-1/2); `queueCraft` / `tickHand` hand-craft from the pockets into the pockets, paused out of reach or with full pockets (GA-B2-3). `worldScene.ts` ghost reasons and toasts, `panel.ts` machines carried and the build menu's carried count, `__relight.flow.canPickUp`. Tests in `flow.test.ts`, `defence.test.ts`, `walk.test.ts`. Doc §13, §14 tagged `B-M2-pockets`.
- **Rates on a face that is not a rectangle** (run `B-M2-rates`, seed 3's HQ face): the 23-tile steel run's first unit in transit 12.25 s (doc 12.27 s), 15.0 mag/min from one steel Excavator over 10 min (doc 15), 7.50 items/s on a saturated belt (doc 7.5), §11's line 114 steel + 23 Cu of the 200 steel; the copper arm over-supplies one assembler 3:1 and stalls (reported).
- **D-P3-10 measured**: every non-Inert HQ-neighbour face on seeds 3/4/5 gets 1 slot from area (391–1,197 tiles) and holds 4–9 §11-scale lines / 33–110 3×3 machines; an Inert plaza gets 1–2 slots and holds nothing. The slot is C3's economic cap, not a footprint limit — D-B2-2.
- Decisions for the human: D-B2-1 machine crafting, D-B2-2 slots vs footprint, D-B2-3 pick-up contents. Deferred: machines in the chest / the truck, workbench machine recipes, ground items, the bots on tile machines, side-loading → `DEFERRED.md` "Re-read at prompt B M2".
- Checks: `npm test` 99/99, `typecheck` (incl. the game build), `lint`, `snapshot:check` (`5f3417b9`), `docsync:check`, `experiments` 12 / 107 s / 0 failing, `calibrate` unchanged (C1/C2 MET) — green. Fixtures `city{3,4,5}.json` and the snapshot unchanged: the block sim is untouched. Browser check of the cursor path in the preview build, 0 errors; the reference-machine soak not re-run (render loop untouched, the M1 59.9 fps stands).

### B.4 Untagged recount at prompt B M2

33 → **33**. M2's `[sim: B-M2-pockets]` tags land on §13's Workbench row and §14's pocket paragraph, whose numbers were already tagged (3 s and 2 steel + 1 Cu under `M2-rates`; reach 8 and 40 stacks under GA §25 items 15/16); the Workbench's 2×2 is the M1 lot mark, untouched. No number moved from untagged to tagged. §26 status unchanged: three systems, 5 of 10 gate criteria measurable.

### B.5 M3 Defence on the segment (2026-09-04, `SLICE_REPORT.md` "Prompt B M3")

- **An edge is a street segment** (run `B-M3-segments`): a turret serves the segment its front ring or nearest ridge gives it (D-B1-4's `faceSegOf`), an edge's rounds are the sum of its turrets, and the map view's hopper-empty pulse lands on the segment's pip, the same event that turns it red. Seed 3's HQ: 6 start turrets serve its four segments 1 (by reach, the 5-tile sliver) / 2 / 2 / 2.
- **The Electricians' unlocks** (run `B-M3-unlocks`): Floodlight (2×2, 40 kW, a 12-tile cone 60° wide, GA-B3-3), Big pole (2×2, reach 12, GA-B3-4) and the craftable Substation (3×3, one a face, on a Held face that has none; 50 steel + 25 Cu stand in for §13.14, GA-B3-2) are locked kinds (`lockReason`) until the Electricians' block turns Held, then on the build menu (0, [, ]) and kept after a fall (GA-B3-1); the held event carries the names. The outskirts have no substation and no streetlights at tile level (§7, GA-B3-6) while the block sim still powers a Held one (D-P4-9's split).
- **Hands from the pockets** (run `B-M3-hands`): E on a turret or Generator feeds magazines or coal from the pockets with the M2 reasons; the Depot is never drawn on; the harness bot keeps the Depot path (GA-B3-5).
- **Measured**: with the §11 line and the bot's hands the HQ holds the hour on seeds 3 / 4 / 5 (845 / 783 / 810 magazines, first hopper-empty at 30:29 / 30:33 / 30:29 on the bot's first claim); with no line the HQ falls at 12:54 (the unfed consequence at tile level, as the lattice soak found); with power on the §11 line is 350 kW against one 300 kW Generator and §14's order sheds the Shot assembler first, the HQ lost at 12:25 (D-P4-7 evidence). Floodlight +82 tiles vs Lamp +51 on the HQ face; pole / Big pole on the grid to 7.91 / 12.00 tiles; the Electricians sit 3 hops out on all three seeds and the compact bot never claims them in the hour.
- **Doc**: §7, §8, §11, §13 tagged `[sim: B-M3-unlocks]`, §11 `[sim: B-M3-hands]`; three changelog lines. Decisions for the human: D-B3-1 the Substation's price, D-B3-2 the outskirts at tile level, D-B3-3 when unlocks land. Deferred: turrets on claimed blocks and the outskirts' abstract substation (D-P4-9, M6), the poles' supply areas, the Substation recipe and the other groups' unlocks (Phase 5), the Floodlight's light texture (M5) → `DEFERRED.md` "Re-read at prompt B M3".
- Checks: `npm test` 103/103, `typecheck` (incl. the game build), `lint`, `snapshot:check` (`5f3417b9`), `docsync:check`, `experiments` 12 / 113 s / 0 failing, `calibrate` identical — green. Fixtures `city{3,4,5}.json` regenerated, unchanged.

### B.6 Untagged recount at prompt B M3

33 → **33**. `[sim: B-M3-unlocks]` lands on §13.7's Floodlight half and §13.9's Big pole half, whose rows left the untagged set at the lattice M3 (13.7 / 13.9 under `M3-rates`), on §8's Electricians row and §7's outskirts paragraph (rules, not numbers); §13.14 (the Substation's recipe) stays untagged: the 50 + 25 is a stand-in, not the number. No number moved. §26 status unchanged: three systems, 5 of 10 gate criteria measurable; the one new rule a player holds is "a survivor's block Held is their machines on the menu", which §26 already counts under found tech.

### B.7 Before M4 — D-P4-7 settled, D-B3-4 built (2026-09-04, `SLICE_REPORT.md` "Before M4")

- **The human's decisions**: D-B3-1 (a), D-B3-2 (a), D-B3-3 (a); D-P4-7 settled now, not at M6; D-B3-4 proportional brownout given and built.
- **D-P4-7 part 1** (run `B-M4-gate`): the M3 script was the pre-E4 hour (one Generator, the whole line at 0:00). Realigned to E4 (Generators 0 / 6 / 15 / 45 and 0 / 6 / 15 / 25, the machines at E4's minutes, the coal Excavator on the HQ patch into the Depot) the tile-level §11 hour holds on seeds 3 / 4 / 5 with power on: 0 brownout seconds, peak 1,090 of 1,200 kW, 806 / 801 / 806 magazines (the same as with power off), the HQ patch's coal out of the Depot at 60:00 (639 / 621 / 621 burned). **The M4 gate holds.** The stale script under D-B3-4 also holds the hour, at 42–73 % with 500 magazines.
- **D-B3-4** (runs `M3-rates`, `E2-matrix`, `E2-sustained6h-h500-*`): every machine runs at supply ÷ demand while short; nothing is shed, no substation is stopped by power; lights lit at any throttle above zero; belts and turrets never slowed (GA in `stepFlow`). The shed order, `SimConfig.shed`, the shed events and the game's red cross are gone; toasts, panel row and world-view line carry the percentage. E2 rewritten: a 10-minute or one-hour shortfall of up to 50 % costs nothing; from hour 4 to hour 10 a 50 / 75 / 90 % shortfall starves the ring behind the red pip (first pip 67–216 / 18–35 / 12–16 min, the fall 1.7–7.5 min after it, the pip leading on every seed), never through power.
- **Doc**: §5, §11, §13, §14, §19, §23, §24 risks 7 and 9, §25 item 14, §26 edited; `[sim: B-M4-gate]`, `[sim: E2-sustained6h-h500-50pct/75pct/90pct]`; three changelog lines. `DEFERRED.md` "Re-read before prompt B M4" (D-P4-7 closed; coal past the hour, the first pip before the line, the tile-level brownout soak → M4 / M6).
- Checks: `npm test` 103/103, `typecheck`, `lint`, `snapshot:check` (regenerated, config hash `5f3417b9` → `68d07000`: SimConfig lost `shed`), `docsync:check`, `experiments` 12 / 107 s / 0 failing, `calibrate` identical (header hash `5eae8618` → `03328db7`) — green. Fixtures `city{3,4,5}.json` regenerated, unchanged; `lattice/power*.json` pinned on their pre-first-shed prefix.

### B.8 Untagged recount before prompt B M4

33 → **33**. `[sim: B-M4-gate]` lands on §11 (0–10 and 10–30 min), §24 risk 7 and §25 item 14, all already tagged by the E4h rows; `[sim: E2-sustained6h-*]` on §5, §14, §23 and §24 risk 9, replacing `E2-matrix`'s shed-order numbers with the starvation ones. §14's shed order was a rule, never a numbered row; its replacement is one too. No number moved, no row left or joined the untagged set. §26: three systems, 5 of 10 gate criteria measurable; the power rule a player holds is now "everything slows, one bar", which §26 counts under the front.

### B.9 M4 Threat, and the rifle (2026-09-04, `SLICE_REPORT.md` "Prompt B M4")

- **Built** (runs `B-M4-threat`, `B-M4-hour`): `threat.ts`, the tile threat layer installed through `engineer.ts` `threatHooks` — crawlers and shades born on the segment ridge when the block sim opens an engagement on an edge the tiles know, a per-face flow field (lit lamps → the turret as a waypoint → the substation, or the outskirts pole; two blocks, never more), the 40-arrival rule and the shades' 30 s at tile level, turrets shooting bodies out of their own hoppers at 4 HP a round, retaliation only (shot or stood in the path; 5 HP/s at 1.2 tiles, chase within the two blocks), 100 HP / regen 5 after 5 s / knockdown / up at the workbench 10 s later with the pockets, the rifle along the aim line (9 tiles, 3 rounds a crawler, shades only on lit tiles), hand-fired engagements with a held flag, the Arsenal's Rifle Mk2 as a locked toolbar row, the §19 telemetry (minutes walked per hour, chest trips, reach refusals, first shot, rounds and kills by hand, HP lost, knockdowns, fights). Rot drawn as tile presence on Dark lots. No hulks; the upgrade and the truck outside the hour.
- **Measured**: one 10-crawler bloom on a dry HQ edge eats the face's 14 lights by 28 s, reaches the substation at 29 s, 40 arrivals at 68 s, the fall at 158 s; fed turrets kill 40 of 40 at 3.0 rounds a kill. The §11 hour on the E4 schedule with power on: 467 / 370 / 241 bodies on seeds 3 / 4 / 5, all but 6 / 0 / 0 shot by turrets, 5–13 lights eaten, no shades, the HQ holds, 0 brownout s, 806 / 770 / 784 magazines; the bot walks 2.6–4.1 % of the hour and fires 0–3 rounds. The first red pip at 6:10–6:11 with the rifle bot is its `restock` emptying the buffer, a harness artefact; without it 15:29 / 5:37 / 15:29. Soak: 43,781 frames over 900 s real (1 h 30 min of sim at 4×), worst 33.3 ms, 0 over 50 ms, sim ≤ 0.82 ms a tick, the HQ held with the line; a first soak found the rot specks halving the frame rate (fixed: rects, capped) and the map scene's hopper-empty pulse crashing a world-view session (guarded).
- **Assumed**: GA-B4-1…13 (the constants, the birth tile, the outskirts pole, the stand-in hand-over, the field's one-second cache, the unharmed turret, eaten lights until M5, the chase's end, the dash's cover, chest trips, Rifle Mk2 as an entry, reach refusals a click, the bloom toast's radius).
- **Doc**: `[sim: B-M4-threat]` on §7 (targets, retaliation) and §17 (the flow field, two blocks); `[sim: B-M4-hour]` on §19 (walking); changelog lines. `DEFERRED.md` "Re-read at prompt B M4". Decisions D-B4-1 (the rifle's button: keep D-B1-5), D-B4-2 (the threat constants), D-B4-3 (the turret as a waypoint).
- Checks: `npm test` 110/110, `typecheck`, `lint`, `snapshot:check` (`68d07000`, unchanged), `docsync:check`, `experiments` 12 / 108 s / 0 failing checks, `calibrate` output identical (C1 / C2 MET, C7 / i1 / i2 as before) — green. Fixtures unchanged.

### B.10 Untagged recount at prompt B M4

33 → **33**. `[sim: B-M4-threat]` lands on §7's target and retaliation rows and §17's flow-field lines, all rules already tagged or never numbered rows; `[sim: B-M4-hour]` on §19's walking share, which stays a player's estimate with the bot's number beside it. The threat constants (4 HP a round, 1.2 / 3 / 0.9 / 1.6 tiles, 30 s) are new GAME-ASSUMPTIONs in code, not doc rows, and go to D-B4-2. No number moved, no row left or joined the untagged set. §26: the threat is the third system a player holds in the hour (the front, the power, the bodies); 5 of 10 gate criteria measurable.

### B.11 M5 Light (2026-09-04, `SLICE_REPORT.md` "Prompt B M5") — verified 2026-09-04 (B.15)

- **Built** (run `B-M5-light`): `light.ts`, the light map as data (`lightMask`: one byte a tile, lit where a lit light covers it under the shade rule's own `lightCovers`); the game multiplies it over the ground and the machines as a canvas texture (lit white, unlit ≈ 25 % with a cool cast), re-read eight times a second. Rot never sits on a lit tile. The burn-off is named (`burnOffS` = 20 + 60·d, the claim's number all along) and read back as `contestProgress`; a claimed face's sound streetlights come on at 3 a second from the substation out (`LIGHT_SEQ_PER_S`, `lightRanks`); the Contested lot's specks clear within 16 · progress tiles of every lit lamp. E on a broken or eaten light repairs it for 1 Cu from the pockets (`repairLight`; M4's deferral closed). The engineer carries no light; `?handlamp=1` previews a 2-tile disc. Toasts and the hover line carry the rules. Four tests in `light.test.ts`.
- **Verified** (B.15): the four light tests pass after three test fixes; the kerb row of a powered face is 50–68 % lit and the half-street 26–41 % — the report's "lit end to end" was wrong and is a reported disagreement; the sequence counts every slot (N ÷ 3 s); the snapshot moved by 1 ulp (`burnOffS`'s association) and was regenerated; the soak's frame time is in the report.
- **Assumed**: GA-B5-1…7 (the multiply's cool cast, the 8 Hz re-read, the sequence's straight-line order, the sweep radius 16, 1 Cu a repair, a repaired streetlight sound for good, the hand-lamp preview as drawing).
- **Doc**: `[sim: B-M5-light]` on §4 (the light map), §5 step 3 (the streetlights and the burn-off), §6 (three per second); three changelog lines. `DEFERRED.md` "Re-read at prompt B M5". D-B4-1/2/3 taken by recommendation (a) on this `go`; D-B5-1 (the hand lamp: none), D-B5-2 (the sequence's pace: §6's 3/s), D-B5-3 (1 Cu a repair, ≈ 25 % unlit) open.

### B.12 Untagged recount at prompt B M5

33 → **33**. `[sim: B-M5-light]` lands on §4's light map (a tech line, not a numbered row: "~25 %" is GA-B5-1's rendering, not a sim number), §5 step 3 and §6's "three per second" (`LIGHT_SEQ_PER_S`, a constant the code now carries — evidenced by the sequence test, not measured in play, so it stays out of the untagged count as a design input). 20 + 60·d was tagged at Phase 1. No number moved, no row left or joined the untagged set. §26: the light is the payoff the hour is built for; 5 of 10 gate criteria measurable, the light's own criterion (the lights coming on read as a reward) is a playtest's.

### B.13 M6 The hour, §11 as rewritten (2026-09-04, `SLICE_REPORT.md` "Prompt B M6") — verified 2026-09-04 (B.15)

- **Built** (run `B-M6-hour`): `hour.ts`, §11's minute list as a walking bot on the tile layer — the steel patch, the workbench, the magazines walked to the turrets, the line from the pockets in chest trips (M4's lot layout), east / west / north claimed on the calibration's clock with the kits walked over, the two idle HQ turrets carried to north's front, the rounds run every five minutes; every §11 moment a mark in a log, every refusal with its reason, and `hourReport` turning the log into findings against §11's prose and the calibration timeline (the rework's three re-checks included). Three commands (`feed`, `repair`, `rotate`) give the scene's last direct calls a command form; the session logs every command with its tile tick and the export carries it; `replay` re-runs a log rifle-off for Gate B's "did it matter?" row (`held anyway` / `saved it` / `fell anyway`; `npm run replay`, `__relight.replay()`). `E-hour` (seeds 3 / 4 / 5 × rifle off / on, the timeline, the end state, the rifle row, the findings, a determinism check). Seven tests in `hour.test.ts`. `?autoplay=hour&rifle=1`.
- **Verified** (B.15): `E-hour` 6 / 6 runs, 6 / 6 checks; the timeline and the findings are in the report; `replayVerdict` fixed on the pass (it read "saved it" on a block that fell in both runs).
- **Assumed**: GA-B6-1…8 (the bot's numbers, the calibration's claim minutes, "east" by lot centres and the stand-ins on the HQ lot, the idle-turret carry, the replay's frame-independence and rebuilt state, six turrets fed, one retry then a refusal).
- **Doc**: no tag placed at the build; the pass put `[sim: B-M6-hour]` on §11's walk-over (edited 40 s → 3–7 s) and east's burn-off (edited 40 s → 30–35 s), B.15. `SLICE_REPORT.md` rewritten from the top: the GAME-ASSUMPTION register and Gate B's two rows at the top. `DEFERRED.md` "Re-read at prompt B M6". D-B5-1/2/3 taken by recommendation (a) on this `go`; D-B6-1 (the hands from the pockets), D-B6-2 (the claim minutes), D-B6-3 (the replay's ground) open; D-P4-4/5/7/8/9 carry a recommendation each, settled by the human after the played hour.

### B.14 Untagged recount at prompt B M6

33 → **33**. M6 confirms no doc number at the build; the pass (B.15) tags two §11 numbers and moves none of the untagged set. §26: with the hour bot the slice's ten gate criteria are all measurable by E-hour but the three §19 asks (new unlock, current problem, memorable moment) and the burn-off's line, which are the human's; 5 of 10 measured on the pass.

### B.18 Untagged recount at the economy fix

33 → **33**. The fix tags §11's rewritten numbers (`B-M6-hour`, `B-M6-hour-north`, `E-rifle-tile`) and §13's streetlight radius (`B-M6-light`), none of which is in the untagged set; §12's Mk2 row and the radius are new numbers born tagged, so the set does not grow. §5.8 (two turrets an edge) is still placed, not measured. §26 status unchanged: three systems, complexity 5; the rifle grew a stance (`rescueStance`) for the harness, not a rule. 21 tile-scale + 12 design inputs.

### B.17 Economy fix, layout pass, E-rifle at tile scale (2026-09-04, `SLICE_REPORT.md` "Economy fix") — built, cheap checks and the two re-runs only

- **Decided by the human (chat message 2026-09-04, provenance in `DECISIONS.md`)**: D-P4-4 / D-B1-1 the Mk1 at 6 s = 10 mag/min and the Mk2 at 3 s (the purchase), the second steel Excavator from the surplus, the 200-steel chest for now; D-P4-7 (b) 40 coal in the chest; D-P4-8 six full turrets + 20 magazines with §11's 0–10 around the ~6-minute red pip; D-B5-1 no hand lamp for the gate; D-B5-2/3 and D-B6-1/2/3 (a); D-B5-4 streetlights radius 7. Taken provisionally: GA-EF-1 (the Excavator at 12:00 not ~15:00).
- **Built**: `recipes.ts` Mk1 / Mk2 rows (docsync), `STREETLIGHT_RADIUS` 7; `flow.ts` `START_CHEST_COAL` 40, six full turrets; `hour.ts` steel-to-chest at 12, copper-2 at 46, Assembler 3 at 50, claims 15 / 25 / 65, `HOUR_END`, `coalPlan`, `fellWhy`, `rescueStance`; `ehour.ts` E-hour-coal / E-hour-north / the stock table; `erifle.ts` the tile steady and rescue sections with five checks; the layout pass (`Scale.RESIZE`, HUD + key strip, kerb pips, the Depot's outline / fill bar / beacon, the panel's Mk1 line and the "not yet found" blueprints row); four tests and four test files moved to the new constants.
- **Measured** (E-hour re-run, `B-M6-hour`): 10 / 10 checks; §11's rewritten end state on 6 / 6 runs, no fall, no brownout, steel min 30 at 12:01, copper min 17, coal 2 at the hour; first red 5:33–6:18, first hand-feed 5:36–6:21, Generator 2 at 6:01 ((a) would be 7:24–7:32), the first line magazine 8:07–8:10, east Held 15:31–15:38, west 25:30–25:35; the line 519 magazines, the hands 788–864. North at 65 (`E-hour-north`): Held 65:29–65:34, Generators dry 67:51–68:01, fell 72:45–74:38. E-rifle tile: 12 / 12; a late belt holds, one dry edge saved by the rifle, a dry ring delayed or saved (seed 4), never knocked down. Light: kerb 65–93 %, half-street 44–65 %.
- **Doc**: §11 rewritten in all three windows, §12's table and ammo chain, §13's Assembler row and fixtures, seven changelog lines; `STANDARDS.md` rows 4.3 / B.3 / B.6 / C.2 built (unverified), 3.5 and dealbreaker 1 carry the placeholder row; `PHASES.md` Phase 7 DoD; `ROADMAP.md` §0 lines 1–4 ticked (line 2 on the full experiments run, 13 / 13 green, `EXPERIMENTS.md`).
- **Rebased onto the guardrails (Steps 4–6)**: every new number lives in `constants.ts` (`SHOT_MAGAZINE_MK2_SECONDS`, `ASSEMBLER_MK1_MAG_PER_MIN`, `START_TURRETS`, `STREETLIGHT_RADIUS`, `START_CHEST.coal`, `HOUR_STEEL2_MIN` / `HOUR_COPPER2_MIN` / `HOUR_ASM3_MIN`); `GUARDRAILS_REPORT.md` §5 items 1 and 8 resolved, 2–7 remained (`docsync:check` red by design, six lines) until the economy-fix task's nine decided rows settled them (B.19); the ten decided rows carry `decided by` / `on` / `via` (rule 11); the full experiments run re-stamped `EXPERIMENTS.md` and the JSONs (`freshness:check` green).
- **Left to the human**: ROADMAP §0 lines 5–7; D-P4-10 / D-P4-11 (recommended); the four one-minute STANDARDS checks of the layout pass.

### B.19 The economy-fix task (2026-09-04/05, `ECONOMY_FIX_REPORT.md`) — coded (unverified: E-hour and E-rifle red on the decided minutes)

- **Step 1 — rows**: nine decided rows written by the human's message "fix the eight constant disagreements" of 2026-09-04 (`decided by` Daniel, `on` 2026-09-04, `via` the message): D-P4-4, D-B1-1, D-P4-7, D-P4-8, D-B5-4, D-B1-4-rider, D-ENGINE-1, D-INSERTERS-1, D-HOUR-1 (the §11 minute list lives in `constants.ts` `HOUR`; the generated table is the rule).
- **Step 2 — the eight fixes**: every one in `packages/sim/src/constants.ts` or the row's named file; `docsync:check` green with zero disagreements; the D6 fixtures byte-identical (`ECONOMY_FIX_REPORT.md` §2).
- **Step 3 — the economy fix**: already re-created on top of `constants.ts` by the parallel session (6796d13); the stash was consumed there, so nothing was popped; §11 0–10 / 10–30 prose brought to the decided minutes (three changelog lines); `light.test.ts` thresholds set to the measured values.
- **Step 4 — E-hour** (`B-M6-hour-2`, `SLICE_REPORT.md` "M6 re-run after the economy fix"): **No-fall hour: no.** North claimed at 40 is Held at 40:29–40:34, the Generators brown out 40:01→45:00 and north falls at 48:54–52:43 on every seed; chest steel is zero at 15:02–15:04 on every seed. The two-claim variant (north at 65, run to 75:00) holds the hour with no fall and no brownout inside 60 min but steel still reaches zero at 15:02. The two M6 checks: west's coal never reaches a Generator (GA-B6-3); chest coal is out for good at 54:00 on every seed (margin −6.0 min); the first red pip at 5:33–6:18 with 44–60 crawlers spawned and 0 arrivals.
- **Step 5 — verification pass**: cheap checks green (121 / 121, typecheck, lint, docsync); `npm run experiments` exit 1 — E-hour 6 / 10, E-rifle 10 / 12 (north falls in the steady run rifle off and on; the 600 s edge rescue on seed 5 at 35:00 knocks the engineer down), the other eleven green; calibrate identical to the committed files except the stamp; `freshness:check` and `snapshot:check` green; the 900 s headless soak ran clean (0 errors, 20–26 fps swiftshader) but shows east's claim refused at 15:06 for lack of steel and never retried, so east is never Held in the browser hour (`ECONOMY_FIX_REPORT.md` §6, evidence for D-HOUR-2). `git push origin phase-4:main` not done: main would be red. **Blocked**: the parallel session's north 65 / steel2 12 (D-P4-10 recommended, GA-EF-1 provisional) had made the hour green; the decided rows (north 40, steel2 15) make it red — recorded in `ECONOMY_FIX_REPORT.md` §7; ROADMAP §0 lines 1, 2 and 4 un-ticked with the reason on each line.

### B.20 Untagged recount at the economy-fix task

33 → **33**. The task moved no number in the design doc except by a decided row: §11's minutes 6 / 8 / 15 / 25 / 40 / 45 are generated from `constants.HOUR` (D-HOUR-1) and the three edited 0–10 / 10–30 sentences carry their row ids and `[sim: B-M6-hour]`; the re-measured steel minimum points at `SLICE_REPORT.md` (`B-M6-hour-2`). Nothing in the untagged set was touched. §26 unchanged: three systems, complexity 6.


### B.21 Layout and readability pass (2026-09-05, `LAYOUT_PASS_REPORT.md`, `SLICE_REPORT.md` "Layout and readability pass") — built, cheap checks only (unverified)

`docs/PROGRESS.md` **T3**, run out of order on the human's instruction of 2026-09-05 (T1 and T2 stay blocked on D-P4-9, D-HOUR-2 and D-P4-10). **Drawing only: no file in `packages/sim` was opened, the config hash is unmoved (`0176f61d`), no measured number in the programme can have changed.**

- **Ten of `ROADMAP.md` §2's eleven readability items built**, finishing the pass B.17 started: the HUD as four corner overlays (top-left the key strip and the in-hand line, top-right territory, bottom-left pockets and power, bottom-right the clock and speed) with the toasts moved to bottom-centre of the **canvas**; the map view as a centred full-screen overlay that refits and re-rasters on resize; an opening zoom fitting the HQ lot to ≈ ⅓ of viewport height, re-fitted on resize until the wheel is touched; the kerb line where an owned lot tile meets a street tile; a soft light falloff; a common drop shadow and 2 screen-px rim on every box machine, with the turret's "covers a live edge" mark and the empty-blink drawn after it so both still win; the engineer's facing from `e.face` with a chevron; the shade as a diamond against the crawler's disc; the kerb pip on **every Held block's** front, not the HQ's alone. The full-viewport canvas and the clumped rubble were already built.
- **The eleventh item was refused, and rule 12 is why.** §2 asks for the engineer to carry "a lamp cone"; **D-B5-1** (decided, Daniel, 2026-09-04) decided against a personal light. The facing was built, the cone was not, and the ROADMAP line is reported as wrong rather than edited. D-B5-1 reopens only on STANDARDS row **A.7** — the played-dark sentence — which the T5–T8 waiver did not produce (`GATE_B.md`).
- **The falloff does not move the lit set.** The blur is a render pass over a copy: `lightPix` stays the sim's binary mask and `lightMask` / `litAt` still decide what is lit, so what a shade may stand on and what burns off are unchanged. Tagged as such at the drawing site.
- **Seven new `GAME-ASSUMPTION` tags**, all in `packages/game`, all drawing-only; plus three untagged lengths named in the report (`KEY_STRIP_FRAC`, `cityMapScene`'s `PAD`, the stylesheet's `--panel-w` / `--gutter`).
- **Measured, unverified.** Cheap checks green: `npm test` 121 / 121, typecheck (with the game build), lint, `docsync:check`, `snapshot:check` (`held 28 front 11 interior 18 lost 0 claims 27 assemblers 3 config 0176f61d`). Computed from the code on seeds 3 / 4 / 5: the light map is 800 × 800 = 640,000 texels and the blur costs **7–12 ms of arithmetic per repaint** in Node at eight repaints a second; `fitZoom()` **clamps to 0.5×** on a 1040 px canvas, where the HQ lot fills 46 % / 71 % / 45 % of the height instead of a third, and seed 4's 46-tile lot never reaches a third at any canvas size (§4's lot is 24 × 24; a D6 lot is 29–46). **No browser number:** the frame cost of the blur and of the map's resize re-raster are the pass's first two items for the next verification pass.
- **Three decisions**: **D-LP-1** the opening zoom (a third, clamped, best-effort — or a rule that can actually hold), **D-LP-2** the key strip's corner, **D-LP-3** the falloff strength (the one that touches the identity, and the only frame-cost risk).
- **The pass's own success condition is untested.** §2's check is "a stranger points to street, lot edge, lit area, rubble, Depot and engineer unaided"; T7 was waived and `docs/layout-pass/STRANGER_TEST.md` was never written. The four STANDARDS rows the pass exists to enable (**4.3**, **B.3**, **B.6**, **C.2**) are built-for but unchecked — they are human minutes inside the waived walkthrough — so `ROADMAP.md` §6 still reads **closed: 0 of 46** and this pass does not change it.

### B.22 Untagged recount at the layout and readability pass

33 → **33**. The pass built no system and moved no number in the design doc: every value it added is a render constant with a `GAME-ASSUMPTION` tag or a length in a stylesheet, none of them is one of the 33, and `packages/sim` was not opened. §26 unchanged: three systems (belts/inserters/machines, the front, found tech), complexity **5/10** as `ROADMAP.md` §6 and every recount from 0.7 to B.14 read it. **One inconsistency spotted and not fixed:** B.20 above records the same recount as "complexity 6". Nothing in the economy-fix task added a system, so 5 is the number the rest of the programme carries; the stray 6 is reported here rather than edited, because a complexity count is the human's (rule 7).

### B.23 The blockers task (2026-09-05, `ECONOMY_FIX_REPORT.md` §9, `PROGRESS.md` T1 done) — built and **measured**: the full experiment suite is green

`docs/PROGRESS.md` **T1** finished on the human's instruction "Do the blockers": the three decision rows that walled its third clause were written as `decided` (Daniel, 2026-09-05, that message, each taking its own recommendation) and then built. This is the one milestone since the working-mode change whose numbers are **not** marked unverified, because T1's own definition of done is "make main green" — the suite is the measurement (constitution rule 13), so it was run.

- **D-HOUR-2 (a) — the second steel Excavator at minute 12.** `constants.HOUR`'s `steel-2` 15 → 12; §11's generated minute table follows by docsync; GA-EF-1's provisional 12:00 is now a decided row. The decided 200 start steel was the decided minute list's bill to minute 15 **exactly** (200 − 110 − 48 − 12 − 30 = 0), which no bot behaviour could change; at 12 the curve's minimum is **30 at 12:01–12:02** on every seed.
- **D-P4-10 (a) — north's claim at minute 65, past the hour: half of the row.** `constants.HOUR`'s `claim-north` 40 → 65 and `HOUR_END.held` 4 → 3, so hour one is two claims (east 15, west 25) and ends with three blocks Held; `hour.ts` expects north's claim, the HQ's white border and the Electricians only from `northAt`, and `firsthour.ts` says the `t < 3600` loop never adds north's substation. The 300 s brownout between two decided minutes is gone by construction. **The other half — "west's coal made real" — could not be built**: `rubbleOf` has no coal kind at all (stone / copper / steel / null) and west is a rail yard, so a physical Excavator on its lot digs steel; the two constants it needs are the human's under rule 7. Deferred, and asked as **D-P4-12**.
- **D-P4-9 — the block-level hopper stays through Gate B, so §11's two carried turrets go.** `hour.ts` lost the carry step with `carryTurrets`, `turretSpots`, `idleTurret`, `hqTurrets` and `HourBot.carried`; `flow.ts` and `firsthour.ts` lost their references; `hour.test.ts`'s minute-45 test was rewritten for two claims. The row's other half — every front edge physical, a claim laying its segment's turrets from stock — is deferred to after Gate B, with commit `6694b71` named in `DEFERRED.md` as the last one carrying the four functions.
- **Measured, 2026-09-05: `npm run experiments` is 13 experiments, 231 s, 0 failing checks.** E-hour **10 / 10** (was 6 / 10 at B.20's task) and E-rifle **12 / 12** (was 10 / 12 — the seed-5 knockdown in the 600 s edge rescue does not recur, which retires that item from `DEFERRED.md`). `snapshot:check` matches at config `0176f61d`; `npm test` 121 / 121; typecheck, lint, `docsync:check` green. End state on all six runs: 3 Held, HQ held, 6 turrets, 4 Generators, 7 Excavators, 3 Assemblers, 519 line magazines, **0 brownout seconds, 0 refusals**, walking 2.2–3.8 % against §19's 15 %, claim walk-overs 9–14 s.
- **The result worth naming is `E-hour-north` (measured, not scored).** Carried to 75:00 with north claimed at 65:00, north is Held at 65:29–65:34 and **never falls on any of the three seeds — 0 falls, 4 Held at 75:00** — where the same variant fell at 72:45–74:38 on every seed while §11 carried two turrets onto it. Removing the turrets is what saved it: a physical turret on a claim takes its edge off the ring feed (`hookSyncEdges`) and swaps a ring-fed 100-round hopper filled from a buffer for a hand-fed 50, and one engineer cannot keep three of those fed. The Generators still run dry at **67:51–68:01** and the chest at 75:00 reads 1520–1524 steel / 397 copper / **0 coal** — which is what D-P4-12 exists to fix.
- **One defect found by the light review, not by a check.** `E-hour-stock`'s header was thirteen hardcoded five-minute labels against twelve columns of data (the chest is sampled at t = 0…3540), so every published value read one step late. The header is now derived from the report's own sample times, and the table's last column is 55:00.
- **Two doc numbers were wrong and are corrected.** §11 said the chest's coal is 2 at minute 60; it is **0**, spent for good at **56:00**, on every seed. §11 also still described the two carried turrets and the second steel Excavator at 15. Four sentences edited, four changelog lines added, each naming its row.
- **What is left for the human**: **D-P4-12** (the rail yard's coal, blocking nothing) and the fact that `HOUR_END.held` is 3 — §11's enclosure and the Electricians walking out of civic north are now beats a 3,600 s run never reaches, which Phase 5's opening should call the shape of the hour or a hole in it. E-hour's only remaining finding is unchanged and already reported: the first shade never happens in the bot's hour against §11's minute 32–47.

### B.24 Untagged recount at the blockers task

33 → **33**. The task moved three §11 numbers and every one of them came from a decided row, not from the build: the second steel Excavator's minute and north's minute are generated from `constants.HOUR` (D-HOUR-1's rule, D-HOUR-2 and D-P4-10's values) and the corrected coal figure carries `[sim: B-M6-hour]`, as the sentence around it already did. The carried turrets were a rule, never a numbered row, and their removal is D-P4-9's. Nothing entered or left the untagged set. §26 unchanged: three systems (belts/inserters/machines, the front, found tech), complexity **5/10** — B.22's note stands, and B.20's stray 6 is still reported rather than edited.

### B.25 The no-fall hour (2026-09-05, `SLICE_REPORT.md` "M6 re-run after the economy fix", `PROGRESS.md` T2 done) — **measured**

`docs/PROGRESS.md` **T2**, "re-run E-hour until no block falls on seeds 3, 4 and 5". **It does.** Run name `B-M6-hour-3`.

- **10 / 10 checks in 43.9 s on the committed tree.** Six runs (seeds 3 / 4 / 5, rifle off and on): **no block falls, the chest's steel is never zero — minimum 30 at 12:01–12:02 — and there are 0 brownout seconds and 0 refusals.** End state every run: 3 Held with the HQ standing, 6 turrets, 4 Generators, 7 Excavators, 3 Assemblers, 519 line magazines. Walking 2.2–3.8 % against §19's 15 %; claim walk-overs 9–14 s; every Gate B replay verdict "held anyway".
- **Nothing was changed to make it pass.** The hour passes on the three rows the human decided on 2026-09-05 (B.23). T2 re-ran it and wrote the numbers down; no constant, rule or bot behaviour moved in this task.
- **Determinism shown across two invocations**: the full suite and this run produced byte-identical `docs/experiments/E-hour.json` apart from the `source_commit` stamp, which is what E-hour's three replay checks assert per seed and what a Gate B replay verdict rests on.
- **`E-hour-north` (measured, not scored)**: north claimed at 65:00, Held at 65:29–65:34, **never falls on any seed — 4 Held at 75:00** — where the 2026-09-04 attempt lost it at 72:45–74:38. Generators dry 67:51–68:01, chest at 75:00 1520–1524 steel / 397 copper / **0 coal**.
- **The (a) margin improved and is still negative**: the chest's coal is out for good at 56:00 (was 54:00), **−4.0 min** against the hour's end on every seed, and west's never comes. That is **D-P4-12**, not a constant to move.
- **The one finding that survives everything**: the first shade never happens, six runs, against §11 and E3's minute 32–47. It has now outlived the economy fix, the decided minutes and the no-fall hour. Either §11's minute is wrong for a two-claim hour or the bot never lets a block sleep past d = 0.3 and a human would — and **a played hour is what tells them apart**, which the T5–T8 waiver removed.
- The 2026-09-04 attempt (`B-M6-hour-2`, north at 40, steel zero at 15:02) is kept beneath the new section as the record, not deleted.

### B.26 Untagged recount at the no-fall hour

33 → **33**. T2 measured and wrote; it built nothing and moved no number in the design doc. §11's sentences carry the tags B.23's task placed on them. §26 unchanged: three systems (belts/inserters/machines, the front, found tech), complexity **5/10**.

### B.27 E-rifle at tile scale (2026-09-05, `SLICE_REPORT.md` "E-rifle at tile scale", `PROGRESS.md` T4 done) — **measured**

`docs/PROGRESS.md` **T4**, "E-rifle at tile scale: the rescue run and the steady run". Run name `E-rifle-tile`; **13 experiments, 256 s, 0 failing checks**, E-rifle **14 / 14** in 96.6 s.

- **The rifle decides a rescue at tile scale, which at block scale it never did.** 24 scenarios (2 belts × 2 scopes × 3 seeds × 2 minutes) run rifle off and on — 48 runs: **7 blocks fall without it, 4 with it. Three saved outright, three of the remaining four delayed (0.1 / 2.8 / 2.1 min and one at +0.1), and no block is ever lost sooner with the rifle than without.** The `DEFERRED.md` item "the block-scale E-rifle never sees the rifle decide a rescue" is closed by this.
- **The belt's length is what decides.** At §11's **90 s** belt, 12 of 12 scenarios hold in both arms — a late belt is not the rifle's fight. At **600 s** (it never comes), one dry edge costs the block without the rifle and the rifle saves it 1 / 1; a whole dry ring beats one engineer at one edge, 2 / 6 saved.
- **Every fall in all 48 runs is `unfed` — not one shade**, where the block-scale ring falls were all shades. **E-hour spawns 0 shades in six hours.** The evidence is appended to **D-R1**, which stays `provisional`: it is the case for taking (a) off, and both runs are bots.
- **The cost of a rescue**: 0–120 HP lost, HP min **34**, **0 knock-downs in 24 rifle runs**, mean **38.7 HP** (block scale: 62.4). **Ammo is never the limit** — 924 rounds over 24 runs, the busiest single rescue 96, under half the 200 rounds carried. What runs out is coverage and HP.
- **Steady play is unchanged by the rifle**: 519 line magazines off and on on every seed (0.00 %), hands within two trips, 0 falls, 0 HP lost; 5 of 990 kills are the engineer's. First shot 5:37–14:02, after the first bloom.
- **Built, and it was not in the task line: the tile steady table now carries §19's two guards.** The tile path always accumulated them (`walk.ts`'s `spendRound` → `markShot`; `advanceFlow` runs the block `step()` every sim second) but the tile table stopped at HP lost, so the 10 % shooting and 5 % danger caps were scored only on the 5 h compact bot. Two columns and two checks: **shooting 0.50 / 0.69 / 0.06 %, danger 0.00 %** — a fifteenth to a two-hundredth of what §19 allows. E-rifle 12 checks → **14**. No sim file opened; config hash unmoved at `b95922d2`.
- **Determinism, a third time**: against the committed generated files the whole of `docs/` after the re-run is the `source_commit` stamp on fourteen files, one wall-clock figure (231 s → 256 s), and E-rifle's two new columns and checks. Twelve of thirteen JSONs changed one line each.
- **Where it disagrees**: §5's rescue sentence is the block sim's (a shade at an unlit edge, ~1.5 min, the rifle cannot save it) and the tile sim says crawlers, never a shade, and a 90 s ring that never falls. Both are tagged to their own run; which one §5 quotes is decision 1 of the T4 report. §11's `[sim: E-rifle-tile]` sentence already reads the tile way and needed no edit.

### B.28 Untagged recount at E-rifle at tile scale

33 → **33**. T4 measured and reported; the two columns it added are harness reporting, not a rule, and no sentence of `RELIGHT-design.md` moved. §26 unchanged: three systems (belts/inserters/machines, the front, found tech), complexity **5/10**.

### B.29 Gate B — played in part, verdict `proceed — with conditions` (2026-09-05, `docs/GATE_B.md`, `SLICE_REPORT.md` "## Gate B") — **human**

`docs/PROGRESS.md` **T8**, the gate `ROADMAP.md` names as the one Phase 5 does not open without. **It is not a scored hour.** Daniel played one hands-on session and answered the gate's four questions in his own words; the answers are quoted in `GATE_B.md` and the same verdict is in the slice report, which is the evidence file `ROADMAP.md` points at. Nothing was built by the gate and no constant moved.

- **What was played, and what was not.** §19's first-hour table has **minutes 0–10 filled and 10–30 and 30–60 empty**; no telemetry panel was read, so the hour's own numbers (shooting share, danger share, walked share) come from the bot, not the session. The three other human tasks — the controls walkthrough (T5), the two-assembler line (T6) and the stranger test (T7) — **stay waived**. The verdict is a decision to proceed on one partial session, recorded as such.
- **One STANDARDS row closed, the first of 46: A.7**, the hand-lamp sentence. Played dark: *"Yea the dark is pretty good so far."* The dark reads as the claim's price rather than as an inconvenience, so **D-B5-1 does not reopen and now stands play-supported**, and the standards audit's seventh doc disagreement (D-B5-1 against the personal-light norm) is settled the doc's way. `ROADMAP.md` §6 `closed: 0 of 46` → **`1 of 46`**.
- **§19's minutes 0–10 did not teach what the doc says they teach.** The doc claims the first ten minutes teach "blocks bloom" and "ammo is made from rubble"; neither came back. **Movement did** ("kind of intuitive but need more development time"), and the session's one problem was **not knowing what to do next**. That is a finding against §19's own claim, not against the build.
- **The rifle was fired without mattering** — *"I was just shooting at things"* — which is the play half of **D-R1** and agrees with B.27's bots: at §11's 90 s belt the rifle decides nothing. The session asked for **more range or no range** and **more enemies**, both at the player.
- **Four rows opened, D-GB-1 … D-GB-4**, three of which ask for something `RELIGHT-design.md` does not contain (constitution rule 12: listed and asked, **not built**). **D-GB-1 was decided the same day, option (c), the hybrid**: the claim still burns off its block, so §5, §11, §18 and every measured number in this file stand unchanged; **powering and lighting the next area becomes a physical expedition the engineer walks** (poles and lamps carried to the next area's box), and **an area lighting up becomes a threat trigger** beside the wake bloom, sometimes silent. That is Phase 5 / 6 work; the doc sentences it moves move on the milestone that builds it, with a changelog line. **D-GB-2 (knowing what to do next — and constitution rule 8 forbids tutorial screens, so the rule-8-legal forms are listed first), D-GB-3 (rifle range) and D-GB-4 (enemy density) are `open (gate condition)`** — carried into Phase 5 by the verdict rather than blocking it.
- **D-GB-4 needs no rule change.** §19 caps player shooting at 10 % of an hour and time in danger at 5 %; B.27 measured **0.50 / 0.69 / 0.06 %** and **0.00 %**. There is twenty-fold headroom inside the doc's own limits, so raising density is a tuning decision, not a doc fight.
- **What the gate did not answer**: the burn-off question was asked and displaced by the loop redesign rather than answered; **the first shade is still unreached** (the session stopped short of minute 30, and §11 / E3 put it at 32–47, where six bot hours produce none); and §19's 10–30 and 30–60 rows stay empty. One longer played session settles all three at once.

### B.30 Untagged recount at Gate B

33 → **33**. The gate measured nothing and moved no sentence of `RELIGHT-design.md`; D-GB-1's hybrid is decided but not built, and the sentences it will move (§5's claim loop, §10's threat triggers, §11's hour, §19's asks) move on the milestone that builds them. §26 unchanged: three systems (belts/inserters/machines, the front, found tech), complexity **5/10** — D-GB-1 raises it when it is built, not now.

### B.16 Standards audit (2026-09-04, `STANDARDS.md`, `STANDARDS_REPORT.md`)

- Run against b666c68 from `docs/relight-standards-audit.md`: Factorio, shapez 2, Mindustry, Captain of Industry, FOUNDRY, Oxygen Not Included and Sandustry (the recent indie) across nine categories plus the body, threat and the city; evidence per game in `docs/standards/`. Nothing built, no mechanic added.
- 52 matrix rows carry a gap, merged to 46 one-minute human-check rows: 28 build-with (layout pass 3, Gate B 1, Phase 5 14, Phase 6 4, Phase 7 1, Phase 8 4, Phase 9 1) and 18 add-later (Phase 9 2, Phase 10 1, Phase 12 12, Phase 13 3). 29 items under `## Considered`, 16 excluded on purpose. Written into `ROADMAP.md` §4 / §5, the constitution's DoD lines and the standing block above.
- Step 0 corrected `ROADMAP.md` against this file (M5 / M6 / the pass recorded; the Depot fill level has no decision on record; E-rifle owed at tile scale; M6's three unrun checks; D-P4-8 → D-B1-4; experiments 12 → 13; freshness counter partial). Three `DEFERRED.md` items added; the constitution copied to `docs/CONSTITUTION.md`.
- Seven doc / constitution disagreements reported, not resolved (`STANDARDS_REPORT.md` §2): §14 lanes vs `flow.ts`; §14 / §19 blueprints "minute one" vs Phase 8; "three inserters" vs §13; the shed order vs D-B3-4; §4's debug-key rates; autosave hourly; D-B5-1 vs the personal-light norm (Gate B row A.7).
- Decisions D-SA-1–D-SA-5 (controller / Deck, localisation, mods, multiplayer, screen feedback) in `DECISIONS.md` with recommendations and the cost of deciding late. §26 unchanged: three systems, complexity 5; untagged 33 → 33 (no number touched).

### B.15 Verification pass on M5 and M6 (2026-09-04)

- **Ran** (from the repo root, the `CLAUDE.md` list): `npm test` 121 / 121 after four test failures were read and fixed in the tests (hour minute-10 expected two Assemblers, the Depot is the start one; light's kerb denominator was the half-street; the sequence's slot count; a lamp on the Depot's tile); `typecheck`, `lint` clean; `snapshot:check` red by 1 ulp on one `contestUntil` (M5's `burnOffS` association), regenerated, config `68d07000` and the summary unchanged; `docsync:check` match; `experiments` 13 runs / 137 s / 0 red, `E-hour` 6 / 6; `calibrate` byte-identical; the D6 fixtures unchanged; the 900 s soak at `?autoplay=hour&rifle=1` (40.9 fps mean, 61 frames over 50 ms in five bursts on the bot's walks, worst 68.3 ms — a regression flag against M4's 0 over 50 ms, for the reference machine to confirm); the M6 hooks and the export checked under Playwright.
- **One sim fix**: `replayVerdict` (`hour.ts`) judged a fight by its own outcome in play against the block's end state in the replay and read "saved it" on seed 5's north, which fell in both runs; it now compares the block's end state in both runs. E-hour rerun: seed 5 "fell anyway".
- **Measured** (`SLICE_REPORT.md` M5 and M6 Measured): the kerb row 50–68 % lit, the half-street 26–41 %; the hour's marks on seeds 3 / 4 / 5 — the line up by 8:06, Generator 2 unfed and a 166–170 s brownout from 8:01, the three claims Held at 15:31 / 25:35 / 40:29 with 3–7 s walk-overs and 30–35 s burn-offs, first amber ≈ 3 min and red ≈ 6 on every seed, first shade 42:22 / 40:33 / never, north falls 45:34 / 45:35 / 58:27, held 3 at the hour, §11's minute-45 machines refused for steel, walked 4.6–5.1 %, first shot 2:44–3:05 and the verdict held anyway / held anyway / fell anyway. The browser's hour on §11's 200-steel chest could not claim west or north (0 steel at 25:01) and held 2.
- **Doc**: §11 edited twice with `[sim: B-M6-hour]` (the walk-over 3–7 s, east's burn-off 30–35 s), three changelog lines; the calibration bands, held 4 and the enclosure reported as contradicted, not edited (they are the lattice calibration's record). `DEFERRED.md` "Re-read at the verification pass". `DECISIONS.md`: evidence appended to D-P4-4, D-P4-7, D-P4-8, D-P4-9, D-B1-1, D-B6-2, D-B6-3; no decision taken.
- **Left to the human**: the reference-machine soak by hand; the played hour (Gate B) with §19's three asks and the burn-off line; the M5 repair-by-E and hand-lamp preview, which the script did not exercise.

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
- Constitution vs doc, reported not resolved: six start turrets vs §11's two (D-P4-8, superseded 2026-09-04 by D-B1-4: one per 16 tiles of segment); §11's own opening line browns out at minute 0 on one 300 kW Generator at the half draw, and at the 80/40 start it cannot be bought (D-P4-7, with D-P4-4); physical turrets on the HQ only (D-P4-9).

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
- Decisions: D-P4-4/5/6 **made** by recommendation on the `Go` that opened M3. Three for the human from M3: **open** — D-P4-7 hour-one power, D-P4-8 six start turrets (superseded by D-B1-4), D-P4-9 turrets on claimed blocks.

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
