# Phase 2 report — map-view prototype → Gate A

Date: 2026-09-03. Branch `phase-2` (stacked on `phase-1`, PR #1). Proto: `packages/proto` on the TypeScript sim, config hash **`ee23bb1c`** (economy on) / **`825d2d09`** (`economy=0` control); the canonical experiment config stays `01dc5d02`. Calibration: `npm run calibrate` → `docs/experiments/calibration.md`. Gate A protocol: `PROTOTYPE_TEST_PLAN.md`; results template: `TEST_RESULTS.md` (`verdict: pending`). **The gate is a human's**: the programme stops here until `TEST_RESULTS.md` says `verdict: go`.

## Where the proto and the doc disagreed

Listed before any edit. "Proto" is `packages/proto` as it stood at the end of Phase 1 (built before the programme, verified only against the sim).

| # | § | Doc says | Proto did | Done |
|---|---|---|---|---|
| 1 | §8 scouting | from any Held block you see facility silhouettes up to 6 blocks away; a block's contents (survivors, rubble, well) are revealed when any 4-neighbour is Held | drew every facility silhouette from t = 0, no survivors at all | proto follows the doc: `facilityList` carries `visible` (Manhattan ≤ 6 from any Held block — GAME-ASSUMPTION on the metric), survivors exist in the sim and show when a 4-neighbour is Held; at a fresh start only the **Arsenal** is on the skyline (6 blocks from the HQ); the Foundry is 10 off and appears as the blob grows |
| 2 | §8 survivors | five named groups with distance bands (Electricians ≤ 3, Concrete crew ≤ 4, Gunsmith 4–8, Rail crew 5–10, Foreman 6–10); "we're in" when the block becomes Held | none | placed by band with a seeded RNG (GAME-ASSUMPTION: the bands are read as Manhattan distance from the HQ, one group per block, never on a facility, well, inert or start block; the optional Chemist / Lamplighters / Surveyors are left to the Phase 9 generator); "we're in" toasts on Held as §8 says |
| 3 | §5 pips | green/amber/red per hopper level | colour only | glyph per state: ● green, ▲ amber, ✕ red blinking, in the map and in the ring list (constitution: shape-coded, not colour-only) |
| 4 | §11 start ammo (C10, D-P1-3) | 20 magazines is "enough, narrowly" | proto still ran on 300 rounds (`6e74fbfd`) | proto now takes the sim's 200; the HQ's third hopper shows ▲/✕ for up to 30 s at t = 0 (Measured) — decision D-P2-1 |
| 5 | constitution Phase 2 | `?state=` loads a snapshot; Scenario B is the compact bot at 3 h | no snapshot loading, no state export | `?state=<name|url>` loads `public/snapshots/<name>.json` (raw sim state or a telemetry export's `finalState`), opens paused; **Save snapshot** button; `npm run snapshot` regenerates `b-compact-seed3.json`, `snapshot:check` diffs it in CI |
| 6 | rule 6 | gaps are `GAME-ASSUMPTION` | 14 `PROTO-ASSUMPTION` tags in `packages/*` | all retagged (`grep -r PROTO-ASSUMPTION packages` is empty); listed under Assumed with their questions |

Confirmed unchanged: the claim tooltip `Claim — rot N % · front +N · closes N`, the pole line, bloom pulses sized by crawler count with shades and hulks marked, slots and rubble strip, HUD, speed keys, seed in the URL, telemetry schema (extended, not changed: `meta.scenario`, `meta.snapshot`, `meta.startT`, `held.survivor`).

## Built

- **Sim (`packages/sim`)** — `Survivor` on `MapSpec`/`SimState`, `placeSurvivors` in `map.ts`, `survivorAt` in the held event; queries `survivorList`, `FacilityView.visible`, `SKYLINE_RANGE = 6`. No rule that any experiment measures changed: `npm test` and E1–E9 are bit-identical, `docsync --check` clean, config hashes unchanged.
- **Proto (`packages/proto`)** — survivor markers (figure + letter badge + name), skyline-gated silhouettes, shape-coded pips on the map and in the drag list, a Scenario badge (`Scenario B · from 3:00:00`), Skyline and Survivors sections in the panel, `?state=` loading (paused on load with a toast), Save snapshot, session-relative summary (claims, losses, reorders, assemblers and hours count from the load point, so a B export is the tester's, not the bot's), a `__relight.stateJson()` dev hook.
- **Harness (`packages/harness`)** — `calibrate.ts` scores the constitution's C1–C7 (plus two informational rows from the earlier reports) over three seeds and writes JSON and a markdown table; `snapshot.ts` writes and checks the Scenario B snapshot. Root scripts `calibrate`, `snapshot`, `snapshot:check`.
- **CI** — two new steps: `snapshot:check` (gating) and `calibrate` (reported, uploaded as an artefact, not gating — the C1/C2 rows are a decision, see below).
- **Gate documents** — `PROTOTYPE_TEST_PLAN.md` rewritten for two scenarios with bot timelines for each; `TEST_RESULTS.md` template retargeted (hashes, snapshot name, survivor quotes, C10 and bloom rows in §8, Phase 3 decisions).

## Assumed (GAME-ASSUMPTION)

Rule 6: every gap the proto fills is tagged in code and listed here with the question it stands in for. The first fourteen are `PROTOTYPE_BUILD_REPORT.md` §2's `PROTO-ASSUMPTION`s, retagged; item 2 was closed by the calibration and is kept so the numbering holds.

1. **Rubble yield** (`sim.ts`, `types.ts`): one rubble per Held block per minute by district — civic stone, residential copper, industrial steel, outskirts nothing; 32/min into a finite 3,840 pool per block. *What does a block yield, at what rate, and does it deplete?* → Phase 5 E11.
2. ~~Magazines from nothing~~ — closed: magazines cost the §12 recipe.
3. **Stone has no sink.** *Walls and foundations are world view.* → Phase 5.
4. **Claim cost** 5 Cu + 10 steel (10 wire, 5 frames). *Wire and frame recipes.* → Phase 5 E13.
5. **Assembler cost** 20 Cu + 40 steel. *§13 machine costs.* → Phase 4 M2.
6. **Start stock and start patch** 40 Cu, 80 steel, steel patch 7,680 at 64/min (empties at 2:00, §19 read literally). *What the start block contains.* → C5, Phase 4.
7. **Proto config**: production on, unfed rule N = 40, hopper 100, **200** start rounds (was 300 — C10 made), jitter 0.1, economy on, one Mk1 assembler at t = 0 and no schedule; the player builds the rest. *The doc's assembler schedule stands in for a player.* → Gate A.
8. **Pip per edge**: the doc's pip rule is for a block's hoppers as a set; the proto shows one per edge. *Per-block or per-edge?* → Gate A, Phase 4 M3.
9. **"closes N"**: Held blocks that become Interior if this block were Held, itself included. *§19/§27 use the phrase, nothing defines it.* → Gate A.
10. **Claim telemetry "after" values** are projections at claim time. → telemetry schema, cross-cutting.
11. **Shape metrics**: perimeter counts Held sides facing Dark, Contested or void; inert and the map edge are walls; area is Held blocks. *No shape metric exists in the doc.* → Gate A.
12. **Facility placement**: Foundry at the target; Arsenal, Turbine hall, Refinery, Power station seeded into §8/§17 distances and octants. *§17 gives ranges, not a rule.* → Phase 9.
13. **Facility "reached"** = its block turns Held; a toast and nothing else. → Phase 4–6.
14. **Distinct shapes** thresholds (perimeter/area > 0.15, aspect > 0.3). *No threshold exists.* → Gate A.
15. **Survivor placement** (`map.ts` `placeSurvivors`): §8's bands read as Manhattan distance from the HQ, one group per block, seeded per map, avoiding facilities, wells, inert and the start block; the three optional groups are not placed. *Does the generator place survivors by Manhattan band, by walking distance, or by pocket (§8 "dead-end pockets")?* → Phase 9.
16. **Skyline range** (`queries.ts` `SKYLINE_RANGE`): §8's "6 blocks" is Manhattan from any Held block. *Manhattan, Chebyshev, or line of sight?* → Phase 4 M5 (light map) or Phase 9.
17. **"We're in" on Held, reveal on neighbour** follows §8 exactly; the proto does nothing with the unlock (no toolbar). *What a survivor group gives in map view before world view exists.* → Phase 4.
18. **Snapshot semantics** (`session.ts`): a loaded state opens paused at its own clock, keeps its seed, economy and scatter flags, drops its event log, and the telemetry counts from the load point. *Whether a saved game resumes paused is a UX choice, not a rule.* → Gate A observer notes.
19. **Edge hopper 100, wells claimable, assembler line 220 kW, map 24×24** — carried from Phase 1 unchanged.

## Deferred

`DEFERRED.md` re-read at this phase (section "Re-read at the Phase 2 build"). Moved or added: the **§18 redraw is Phase 3** (the constitution says so; DEFERRED.md said Phase 2 — corrected); **Python sim retirement** waits for `verdict: go` (the Phase 2 gate is Gate A, which has not run); **survivor unlocks** (toolbar recipes) → Phase 4; **optional survivor groups and pocket placement** → Phase 9; **E10-bloom-cadence** unchanged (Gate A feel, C8); **Linear integration** unchanged (a human's GitHub UI step). Nothing deleted.

## Measured

Every number is at `ee23bb1c` (24×24, 200 start rounds, PROTO_CALIBRATED, economy on, build on, react off), seeds 3/4/5, 3 h, bots at the E8 cadence (15 min in hour one, then 5). Full tables: `docs/experiments/calibration.md` (regenerated in CI). Seed-3 event traces from `packages/harness` runs.

### Calibration targets (constitution C1–C7)

| Target | Result | Detail |
|---|---|---|
| C1 compact: first enclosure before first amber | **MISSED — at t = 0 only** | enclosure 60 m on all seeds; first amber 0 m; **after minute 10: never** |
| C2 compact: no red before 120 min | **MISSED — at t = 0 only** | first red 0 m; after minute 10: never |
| C3 compact: 0 lost and no stall to 180 | MET | 0 / 0 on all seeds |
| C4 spike: red by 90 min | MET | 0 m (transient); 65 m after the transient |
| C5 spike: 1–4 distinct blocks lost by 180 | MET | 2 distinct (24 falls) on all seeds |
| C6 spike: never more than 2 assemblers before 120 | MET | max 1 |
| C7 cheapest: no stall; ammo per held block 0.7–1.3× compact | MET | 1.14 / 1.12 / 1.29 |
| i1 (info) compact first amber 60–120 min (old T1) | MISSED | never after the transient |
| i2 (info) held at 120 in 20–30 (old T7) | MISSED | 16 compact and cheapest, 4 spike — the bot cadence, not the economy |

**The C1/C2 miss is a 30-second transient, not the front.** 200 start rounds (20 magazines, C10) fill two of the HQ's three 100-round hoppers; the third is empty at t = 0 and the Mk1 assembler fills it over the first half-minute. Trace on seed 3: last non-green pip at 0:30, all green from 0:31, and nothing leaves green again in three hours. At 300 rounds no pip ever leaves green. Scored from minute 1 (or minute 10, the column `amber after 10 min`), C1 and C2 are met on every seed.

**Compact never sees amber after the transient.** Whatever the tester builds, one Mk1 (10 mag/min) feeds the compact blob's front through 2:30 (front 10–17 edges at 3 h, demand ≈ 24 mag/min against 50 of production once the bot has bought two more assemblers; even before the second assembler the 400-magazine stock covers the gap). The constitution names this: it is the **edges-per-assembler** constant (C1 in `DECISIONS.md`) showing itself. Recorded as the first open decision for the gate (D-P2-2), not tuned around.

### Levers, in the constitution's order

Each lever alone, everything else at base. "Same" means every target row identical to base.

| Lever | Value | What it did | Verdict |
|---|---|---|---|
| base | PROTO_CALIBRATED | C3–C7 met; C1/C2 the t = 0 transient; compact amber never | reference |
| 1a start production | Mk1 5 mag/min | compact **loses 2–3 blocks** on two seeds (C3 missed), C7 missed (1.30); enclosure moves to 45–46 m because the bot builds sooner; compact amber still only at t = 0 (the stock covers it) | worse |
| 1b start production | Mk1 20 mag/min | C7 missed (cheapest 1.34); spike loses 4 distinct blocks on two seeds; enclosure 60–76 m; compact amber never | worse |
| 2a start rounds | 300 | **all seven met**: no transient, compact amber never, spike red 66 m | meets the targets, **contradicts C10 (20 magazines, D-P1-3)** — a human's call, D-P2-1 |
| 2b start rounds | 100 | same as base (the transient is already at 0 m) | no change |
| 3 pool size | half (1,920/block) | cheapest **stalls 9–10 times** on seeds 3 and 5 (C7 missed); compact unchanged | worse |
| 4a yield | 16/min | cheapest stalls 13 times on seeds 3 and 5 (C7 missed) | worse |
| 4b yield | 24/min | same as base | no change |
| 5 costs | claim and assembler ×2 | cheapest stalls once on seed 5 (C7 missed) | worse |

Kept `PROTO_CALIBRATED` unchanged. The only lever that turns C1/C2 green is the one a human already decided the other way; the report carries it as a decision, not as a tuning.

### Scenario A at bot pace (seed 3; the test plan's table)

| Sim clock | compact | spike | cheapest |
|---|---|---|---|
| 0:00–0:31 | third HQ hopper ▲ then ✕, all ● from 0:31 | same | same |
| 1:00 | first enclosure (4 held) | never | 1:00 |
| 1:03 | second assembler | — | 1:03 |
| 1:05 | Electricians "We're in." | ▲/✕ 1:05, first loss 1:08, then every 5 min on the same 2 blocks | — |
| 1:15 | stock at the 400 cap | stock 0 | Electricians 1:15 |
| 2:00 | patch out, HQ block dry (run-dry 1:59:59), held 16 | held 4 | held 16; third assembler 1:47 |
| 2:15–2:20 | Arsenal reached 2:15:35, Concrete crew 2:20:29, second run-dry 2:15:28 | — | Concrete crew 1:45 |
| 2:30 | held 22, 0 lost, every pip ● | held 4, 18 falls | held 22, 0 lost |
| 3:00 | held 28, front 11, 4 dry, 2–3 assemblers | 24 falls on 2 blocks | held 28, front 13–24 |

### Scenario B from the snapshot (seed 3, 3:00:00 → 4:30:00)

Snapshot: 28 held / 11 front / 18 interior, 27 claims, 3 assemblers (50 mag/min) against demand ≈ 24, steel 1,799, copper 16,736, patch 0, 4 blocks dry, stock 400, every pip green. Two forward runs:

| | compact bot keeps going | nobody acts |
|---|---|---|
| steel reaches 0 | ≈ 3:40 | ≈ 3:50 |
| magazine stock reaches 0 | 3:50 | 4:20 |
| pips | ▲ ×3, ✕ ×3 at 3:50 | ✕ from ≈ 4:15 |
| first loss | ≈ 3:55 | ≈ 4:20 |
| 4:00 | 29 held / 6 lost | — |
| 4:10 · 4:20 | 18 / 17 · 11 / 24 (assemblers fall with their blocks) | — |
| 4:30 | **2 held / 33 lost** | 22 held / 6 lost |

B is the **steel wall** the build report named (item 6.2): only industrial blocks yield steel, magazines cost 2 steel each, and the compact position at 3 h holds too few live industrial blocks. It is the pressure scenario the constitution asked for — ring order, retreat and where to claim next all matter and none of them is prompted. Whether this is the intended pressure or a tuning bug is decision D-P2-3.

### Smoke test

Headless Chromium against the Vite dev server; results in "Smoke test" at the end of this report.

### Speed

`npm run calibrate` ≈ 30 s for nine bot-runs; `npm run snapshot:check` ≈ 6 s; full check set (typecheck, tests, lint, experiments, docsync, snapshot) ≈ 2 min locally.

## Definition of done

| DoD (constitution Phase 2) | Status |
|---|---|
| One Phaser screen with everything the bullet lists, incl. survivor markers, shape-coded pips, `?state=` | built (Where-disagreed table) |
| Economy as specified | unchanged from calibration 2, retagged |
| Telemetry as specified, JSON export, on-screen summary | built; summary is session-relative for B |
| Calibration by bots at the E8 cadence, three seeds, levers in order, each reported | done: C3–C7 met; C1/C2 a t = 0 transient; compact never ambers → D-P2-2 as the constitution instructs |
| Two scenarios | A `?seed=3`; B `?state=b-compact-seed3` (CI checks it is reproducible) |
| `PROTOTYPE_TEST_PLAN.md` with the listed contents | rewritten |
| `TEST_RESULTS.md` template with `verdict:` | `pending` |
| Experiments green in CI | green locally; the PR's CI run is the proof |
| §26 recount | three systems, complexity 5/10, nothing added (the survivors are §8 content, not a system) |
| **Gate A: a human writes `verdict: go`** | **pending — the programme waits here** |

## Three decisions (for the human, at the gate)

1. **D-P2-1 — Start rounds and the C10 transient.** 200 rounds (C10 = 20 magazines, D-P1-3) leave the HQ's third hopper empty for the first 30 s, so the first thing a tester sees is an amber then a red pip that fixes itself; 300 rounds (30 magazines) meet all seven calibration targets with nothing else changed. Options: (a) keep 20 magazines and score C1/C2 from minute 1 (the harness column `after 10 min`), accepting the opening flicker as the tutorial for what a pip is; (b) change C10 to 30 magazines (reverses a made decision; §11 and `01dc5d02` move with it); (c) keep 20 and pre-fill all three hoppers from it (a rule change: 200 rounds spread evenly is 67 each, all amber). **Recommendation: (a)** — the flicker shows a tester the pip ladder in the first half-minute at no cost, and the constitution's targets are about the front, not the opening.
2. **D-P2-2 — Edges per assembler (C1), the constitution's named first open decision.** Compact play never sees amber after the transient: one Mk1 feeds a 16-block blob's front to 2:30 and the 400-magazine stock hides any gap. So in Scenario A the second assembler is never *needed*, only affordable, and the front's price is shape and slots, not ammo. Options: (a) accept — A tests shape and enclose-to-build, B tests ammo pressure (that is what the constitution's two scenarios say), and Gate A decides C1 from what testers do in B; (b) make an assembler feed fewer edges (Mk1 at 5 mag/min lost blocks in the sweep; the doc's ~15 edges per 20 mag/min is the number to change); (c) cut the buffer cap (C4) so the pip ladder moves instead of the stock. **Recommendation: (a)**, with C1 locked in Phase 3 from the B telemetry (`summary.firstAmber`, reorders, assemblers built).
3. **D-P2-3 — The steel wall as Scenario B's pressure.** From the 3 h snapshot steel hits 0 at ≈ 3:40 whatever the tester does, and a tester who keeps expanding like the bot is down to 2 blocks by 4:30. Options: (a) intended: B is "where the front bites", the exit is to claim industrial blocks north or to reorder and retreat, and the doc says so in §19 (a `[play]` sentence after the gate); (b) a tuning bug: raise industrial yield or the pool so a 28-block position is self-sustaining and B's pressure comes from the front alone (the sweep says yield and pool cannot move without stalling cheapest, so this means a new lever — steel from residential or a second industrial band); (c) snapshot a different bot or hour so the wall falls outside the 90 minutes. **Recommendation: (a)** — nothing else in the proto makes the ring order or retreat matter, and the test plan's reorder threshold needs exactly this.

## Smoke test

Playwright Chromium 151 (headless, 1400×950) driven over the DevTools protocol against `vite --force` on 127.0.0.1:5173, the same build a tester gets. Three pages, every number read from `__relight.summary()` and cross-checked against the harness.

| Page | What was checked | Result |
|---|---|---|
| `?seed=3` (Scenario A, fresh) | config hash on screen, opening pips, Skyline, Survivors, Save snapshot, console | `ee23bb1c`; pips ● ● ▲ then ✕ on the third HQ edge for the first 30 s (the D-P2-1 transient), all ● by 0:31; Skyline lists the Arsenal (6 from the HQ) and nothing else; Survivors "none found yet"; Save snapshot present; console clean apart from Phaser's WebGL warnings and a favicon 404 |
| `?seed=3&autoplay=compact` run to 1:15:01 | the A timeline in the test plan | held 7 / front 8 / interior 2, 30 mag/min made vs 19.6 demanded, stock at the 400 cap, 0 lost, 2 assemblers, slots 2/0, first enclosure 1:00:29, Electricians "with us" (the "We're in." toast fired on the Held), Arsenal silhouette drawn beside the blob — matches the plan's 1:00 and 1:15 rows |
| `?state=b-compact-seed3` run idle to 4:30:19 | snapshot load, session-relative summary, the idle column of the B timeline | opened paused with the badge "Scenario B · from 3:00:00"; 28 held, survivors with-us/seen, summary claims 0 / claimsTotal 27; at 4:30: 22 held / 6 lost, steel 0, patch 0, stock 0, 10 empty hoppers, ✕ on every ring entry, 3 assemblers, slots 3/10 — the harness's idle run to the tick (firstAmber 4:11:37, firstRed 4:14:02, firstLoss 4:19:29) |

Two things learned:

- The docs said a fresh start has an empty skyline. It does not: the Arsenal at (8,20) is exactly 6 from the HQ. Test plan, this report and DEFERRED.md corrected.
- `__relight.run(ticks)` steps at the page's speed, so on a snapshot page (which opens at speed 0) it does nothing until the speed is set; the panel's text is throttled, so read `__relight.summary()` not the DOM. Both noted for whoever automates the next smoke test.

Screenshots of all three pages are in the session scratchpad only (not committed); the test plan tells testers to attach their own.
