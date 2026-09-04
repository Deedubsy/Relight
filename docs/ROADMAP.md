# Relight — the programme map

Living file. It answers three questions on one page: **where the programme is, what phase that is, and what is left.** Everything here summarises something else and cites it: the phases and their DoDs are the constitution's; per-phase detail is `PROGRAMME_STATE.md` (newest first); human decisions are `DECISIONS.md`; parked work is `DEFERRED.md`; the spec is `RELIGHT-design.md`; the expectation baseline is `STANDARDS.md` (run 2026-09-04; its report is `STANDARDS_REPORT.md`).

Written 2026-09-04 at the end of the D5/D6 rework; **rewritten 2026-09-04 (evening) after prompt B M4; state corrected 2026-09-04 (Step 0 of the standards audit) through the verification pass, b666c68; §4 rewritten from `STANDARDS.md` the same day.** Update at every milestone and gate, alongside `PROGRAMME_STATE.md`. Where this file and `PROGRAMME_STATE.md` disagree, `PROGRAMME_STATE.md` is right and this file is stale.

---

## 0. Next actions

The task list has moved to `docs/PROGRESS.md`. That file is the only place
task order lives. This file explains the programme; PROGRESS.md says what
happens next.

## Risks

- The engine gate in Phase 11 (a port to C#/Godot if the TypeScript performance targets fail).
- Garbage-collection discipline in TypeScript: any per-tick allocation in the sim will show up in Phase 11.
- The untagged-number counter has stayed at 33 through seven milestones (the economy fix: 33 → 33; §11's new numbers were born tagged); it is meant to go down.
- Gate A was passed by the owner alone; no outside tester has played the map view.
- Step 5 of the guardrails landed (`constants.ts` is the one source and `docsync:check` reports every disagreement), but six recorded disagreements between the calibration config / `firsthour.ts` and the tile code stand until a human settles each (`GUARDRAILS_REPORT.md` §5 items 2–7); `docsync:check` was red by design until then; the economy-fix task (2026-09-04, nine decided rows, `ECONOMY_FIX_REPORT.md`) settled all of them and `docsync:check` is green with zero disagreements.

---

## Where we are, in one paragraph

**Phase 4 of 14 — the vertical slice — all six milestones built and verified on the street-first city with the engineer; Gate B is next.** Phases 0–3 are complete and Gate A is passed (owner-only; tester sessions still owed). The D5/D6 rework replaced the cursor-and-global-stock player with an engineer on foot (pockets, reach, rifle, sprint, dodge) and the 24×24 lattice with a street-first city of irregular blocks; the slice was restarted on that base. Since then: **M1 Ground and the engineer, the pre-M2 controls pass (WASD, E interacts, click by hand, Tab/I, B, sprint bar, dodge), M2 Flow on the faces, M3 Defence on the segment, D-B3-4 proportional brownout, M4 Threat and the rifle, M5 Light and M6 The hour are built, and the verification pass of 2026-09-04 (b666c68, `PROGRAMME_STATE.md` B.15) ran every check green.** The bot's §11 hour on the tile layer holds three blocks at minute 60 on seeds 3/4/5; the north claim falls at 45–58 min on every seed and §11's minute-45 machines are refused for steel — Gate B's findings are already on the table (`SLICE_REPORT.md` M6, `E-hour`). **What remains in Phase 4 is human: the controls walkthrough, the two-assembler line, Gate B, then Absorb Gate B.** Two things were added to the programme today that no phase previously owned: a **standards audit** against the modern 2D factory-game baseline, and a **layout-and-readability pass** so the game test happens on a full-viewport world that reads as a place. **The standards audit ran on 2026-09-04 (`STANDARDS.md`, `STANDARDS_REPORT.md`): 52 gaps became 46 one-minute human-check rows in the phases where they are cheapest, 14 of them build-with rows in Phase 5, five decisions went to `DECISIONS.md` as D-SA-1–D-SA-5, and §4 is now the audit's summary.** Ten phases and two gates remain.

---

## 1. The fourteen phases

| # | Phase | Nominal | State | Evidence |
|---|---|---|---|---|
| 0 | Inventory and doc lock | 3 d | ✅ done 09-03 | `PHASE_0_REPORT.md` |
| 1 | Headless front sim | 1 wk | ✅ done 09-03 | `PHASE_1_REPORT.md`; `packages/sim`, E1–E9 |
| 2 | Map-view prototype → Gate A | 2 wk | ✅ done 09-03 | `PHASE_2_REPORT.md`; calibration |
| — | **Gate A** (human) | — | ✅ passed 09-03, owner only | `TEST_RESULTS.md` `verdict: go`; tester sessions owed (D-P3-9) |
| 3 | Absorb Gate A | 3 d | ✅ done 09-03 | `PHASE_3_REPORT.md`; 19 `[play: Gate A]` tags |
| — | **D5/D6 rework** (engineer, street-first city) | — | ✅ done 09-04 | `REWORK_REPORT.md`; commit 27cc9b0, PR #4 |
| 4 | **Vertical slice: §11, minutes 0–60** | 6 wk | ◐ **M1–M6 built and verified** (pass of 09-04); pre-M2 controls built; the human items owed (walkthrough, two-assembler line, Gate B) | `SLICE_REPORT.md` M1–M6 sections, `PROGRAMME_STATE.md` B.1–B.15; commits 042cfe0, 906044c, 7564125, d332464, f7ee1df, 7ba2b9b, f24f383, b666c68 |
| — | **Standards audit** (cross-cutting) | 2–3 d | ✅ run 09-04 | `STANDARDS.md` (matrix, `## Considered`), `STANDARDS_REPORT.md`, D-SA-1–D-SA-5 |
| — | **Layout and readability pass** (new, pre-test) | 2–3 d | ⬜ not started — before the game test | full-viewport world, HUD overlays, readability rules; STANDARDS rows 4.3, B.3, B.6, C.2 |
| — | **Gate B** (human plays the hour) | — | ⬜ not reached | scored in `SLICE_REPORT.md`, `verdict: proceed`; STANDARDS row A.7 (the hand lamp, played dark) |
| 5 | The factory, complete | 6 wk | ⬜ not started | E10 chain, E11 coal, E12 tram; 14 STANDARDS build-with rows (§4) |
| 6 | Threat, complete | 4 wk | ⬜ not started | E13, E9-full, E14; 4 STANDARDS build-with rows |
| 7 | Found tech | 3 wk | ⬜ not started | E15, E16; 1 STANDARDS build-with row |
| 8 | Territory tools | 3 wk | ⬜ not started | E17; 4 STANDARDS build-with rows |
| 9 | The city | 4 wk | ◐ mostly pre-built by the rework | E18, E19 owed; human seed test owed; 1 build-with + 2 add-later STANDARDS rows |
| 10 | Progression and endgame | 4 wk | ⬜ not started | E20, E21; 1 STANDARDS add-later row |
| 11 | Performance and scale (‖ 12) | 4 wk | ⬜ not started | E22; **engine gate** |
| 12 | Interface, onboarding, art, sound (‖ 11) | 8 wk | ⬜ not started | E23; 12 STANDARDS add-later rows (§4) |
| 13 | Balance and Steam | 6 wk + | ⬜ not started | E20 × 1,000/change; three human rounds; 3 STANDARDS add-later rows; D-SA-1 Deck pass |
| 14 | Launch | ongoing | ⬜ not started | — |

The nominal column is the constitution's calendar; Phases 0–3 were budgeted at four weeks and took two days, the rework, M1–M6 and the verification pass took two more. Remaining nominal work is ~44 weeks with 11 and 12 in parallel. Treat both numbers as shape, not schedule.

---

## 2. Phase 4 exactly

### Built and kept (as of b666c68)

- **Block sim on the street graph** (`packages/sim/src/city/`, `graph.ts`): streets first, blocks are faces, adjacency is a shared segment ≥ 5 tiles, districts are hop bands, slots and rubble scale with area, five presets, validator at a measured 26 % reject rate. 88+ tests.
- **The engineer** (`engineer.ts`, `walk.ts`, `body.test.ts`): WASD 8-way with collision, Shift sprint on a stamina bar that never drains below one dodge, Space dodge with i-frames and a 1 s cooldown, reach 8, 40-stack pockets, the Depot as a chest, workbench crafting from the pockets, 100 HP, retaliation only, 10 s return to the workbench with pockets intact. A* remains for bots and the map view's walk-here.
- **Hands and keys** (D-B1-5): left-click does what the hand holds (empty → mine/pick up; building → place; rifle → fire at cursor); right-click removes with full refund; **E interacts only**; Tab/I inventory; B build menu; R rotate; Q pipette; M map; Esc closes; backtick debug panel. Rifle is hotbar slot 9, hitscan to 9 tiles, 1.5-tile hit radius, three rounds a crawler, cannot hit a shade off lit tiles. **Owed: the human walkthrough.**
- **Ground** (M1, `ground.ts`): tile layer from rasterised faces; 32×32 cells; streets 6–12 wide; rubble typed by district with the density gradient; the river as inert tiles.
- **Flow** (M2, `flow.ts`): machines placed from the pockets within reach, carried placement free, pick-up returns contents (a turret's rounds as whole magazines, remainder to the line buffer), hand-crafting at the workbench. Measured on seed 3: 23-tile steel run 12.25 s vs doc 12.27; one Excavator 15.0 mag/min vs 15; belt 7.50/s vs 7.5. D-P3-10 measured: faces hold 4–9 §11-scale lines against a one-slot cap — **the cap is the rule, not a stand-in** (D-B2-2 decision).
- **Defence** (M3): Electricians' unlocks (Floodlight 2×2 60° cone, Big pole reach 12, craftable Substation 3×3), substation at the tile nearest the centroid, lamps every 4 kerb tiles, turret kits per segment length (one per 16 tiles, min one), outskirts without substations per §7, hand-feeding from the pockets on E, edge = street segment with the map pip driven by the same hopper-empty event. HQ holds the hour with the §11 line; without it, HQ lost at 12:54.
- **Power** (D-B3-4): **proportional brownout** — every machine runs at supply ÷ demand, nothing switches off, no shed order, no substation stopped by power; lights lit at any throttle > 0; belts and turrets never slowed. E2 rewritten with a 6-hour sustained window: the red pip led every fall on every losing seed; nothing cascades; every loss is unfed. The §11 hour on the E4 schedule (Generators at 0/6/15/45) holds with power on: 0 brownout seconds, peak 1,090/1,200 kW, 806/801/806 magazines.
- **Threat** (M4, `threat.ts`): crawlers and shades as bodies on the tiles, born on the segment ridge, per-face flow field lamp → substation (turrets are an unharmed waypoint, D-B4-3: **turrets have no HP and are lost only with their block**), 4 HP a round, retaliation 5 HP/s at arm's reach, knockdown and respawn. A dry HQ edge: lights eaten by 28 s, substation reached 29 s, 40 arrivals 68 s, fall at 158 s. Fed turrets 40 of 40 at 3.0 rounds a kill. Hour holds on three seeds; no shades inside the hour; bot walks 2.6–4.1 % and fires 0–3 rounds.
- **Light** (M5, `light.ts`, `B-M5-light`): the single-channel light map multiplied over the world layer; streets and lamp radii lit, lots unlit; rot cannot exist on a lit tile; the streetlight sequence when a substation powers (`LIGHT_SEQ_PER_S`); the burn-off sweeps light along the claimed face over `20 + 60·d` s exactly (E-hour: 29–35 s on the §11 claims); chewed lamps repaired from the pockets; `litCount` on the kerb.
- **The hour** (M6, `hour.ts`, `B-M6-hour`): §11's minute list as a task queue on the tile layer with telemetry on; a command log and a rifle-off replay (`replayVerdict`, fixed on the pass to compare block end states); `hourReport` lists every divergence from §11's prose and from the calibration timeline. §11 edited with `[sim: B-M6-hour]` and three changelog lines.
- **Verification pass** (B.15, b666c68): tests 121/121 after three test fixes and one sim fix; snapshot regenerated one ulp (`burnOffS`), config hash 68d07000; 13 experiments green in 137 s; E-hour 6/6; calibration byte-identical; the 900 s headless soak at 40.9 fps with 61 frames over 50 ms in five bursts (a finding, not a failure; the reference-machine soak is by hand). E-hour's timeline: line up 8:06, a 166–170 s brownout from 8:01, claims Held 15:31 / 25:35 / 40:29, walk-overs 3–7 s, first amber ≈ 3 min and red ≈ 6 min on every seed, north falls 45:34 / 45:35 / 58:27, three blocks held at 60, minute-45 machines refused for steel, walked 4.6–5.1 %, first shot 2:44–3:05, the rifle-off replay's verdict "held anyway / held anyway / fell anyway". A browser hour on §11's 200-steel chest held two (the west and north claims refused for 0 steel).
- **Experiments**: 13 green in CI (E1–E9 on the city, E-rifle at block scale — steady 5 h and the rescue at 2 h and 4 h, seven checks — E-walk, E-variance, E-hour), calibration identical with C1/C2 met (C1 met by absence of amber on two seeds — see §3), fixtures pinned, docsync, snapshot hash 68d07000.
- **Reference machine** in `PROGRAMME_STATE.md`: Win11, i7-11700K, RTX 3070, 2560×1440. M1 soak there: 59.9 fps, worst 26.7 ms. Headless swiftshader is "for regressions only".

### Owed in Phase 4

| Item | The one-line job | Owner |
|---|---|---|
| **Depot fill level on its sprite** | The earlier roadmap listed this inside M5 as "decided 09-04"; no such decision exists in `DECISIONS.md`, `SLICE_REPORT.md` or `DEFERRED.md`, and M5 did not build it. Carried here as an open item for the layout pass, not as a decision. | Claude Code, after a human confirms it |
| **E-rifle at tile scale** | The block-scale E-rifle is in CI (`EXPERIMENTS.md`): steady 5 h ± rifle changes the ammo bill by 0.00 % and loses nothing; in all twelve rescues the block holds with the rifle and without it, so the rifle has never decided a rescue. Owed: the same two questions with M4's bodies on the tiles (an edge red, the belt 90 s away — does the rifle change the outcome, and at what damage-per-round). Not built in M5 or M6. Wanted before Gate B; not blocking it. | Claude Code |
| **Layout and readability pass** | Full-viewport canvas; HUD as overlays (territory top-right, power and pockets bottom-left, speed and clock bottom-right, toasts bottom-centre); map view as a full-screen overlay; default zoom with the HQ lot ≈ one third of screen height; street surface and kerb line; rubble as clumped rectangles thinning with density; soft light falloff; distinct machine silhouettes with outlines; engineer as a two-tone sprite with facing and a lamp cone; front-segment pip state on the kerb in the world view; distinct crawler/shade shapes. Flat colour only, no art direction. Check: a stranger points to street, lot edge, lit area, rubble, Depot and engineer unaided. **STANDARDS rows (one minute each):** 4.3 the key strip is on the HUD, not in a tooltip; B.3 / B.6 the kerb pip in the world view shows the segment's state and an empty turret; C.2 the Depot reads from the far edge of the viewport. | Claude Code |
| **M6's added checks** | M6 is built and verified, but the three checks the earlier roadmap added to it were not run: rail-yard coal reaching the Generators before the Depot's is gone with ≥ 10 min margin (else the HQ patch grows by the gap); the arrival count at the ~6-minute first red pip on seeds 3/4/5 (E-hour records the pip at ≈ 6 min on every seed, not the count); walking % retagged from play (Gate B's row). The first two go to the next verification pass; the third to Gate B. | Claude Code (harness), human (walking) |
| **Controls walkthrough** | The pre-M2 check list on the reference machine: WASD, sprint bar, E on the Depot, hold-to-mine, B → belt → place, rifle at a crawler, dodge, Q, one dodge left after a full sprint. | **Human** |
| **Two-assembler line unaided** | M2's second DoD line, ten minutes, no instructions. | **Human** |
| **Gate B** | Play seed 3; fill the §19 first-hour table (new unlock / current problem / memorable moment per window; the burn-off line; the two rework rows — minute of first rifle shot and whether it mattered, minutes walked); write `verdict: proceed`. **STANDARDS row A.7:** play the hour without the hand lamp and write one sentence — does the dark read as the claim's price or as a missing flashlight? D-B5-1 reopens only on that sentence. | **Human** |
| **Absorb Gate B** | Settle with evidence: the steel budget (D-P4-4, D-B1-1), turrets on claimed blocks (D-P4-9), the lattice pip bands (the earlier roadmap cited D-P4-8, which is the start-turret count, superseded 09-04 by D-B1-4), the assembler stand-ins (D-P4-5), the hands / claim minutes / replay ground (D-B6-1 to 3), the half-lit street; fold into code and doc; tag `[play: Gate B]`; recalibrate the tile-level hour if the lattice bands retire; refresh this file. | Claude Code |

---

## 3. What was learned since the last version, that the roadmap now carries

- **Production must be territorial or the front has no price** — slots on interior blocks, finite rubble, magazines cost their recipe. Constitution rule 4; enforced from Phase 2 onward; confirmed at M2 (faces hold 4–9 lines, cap stays at one).
- **Proportional brownout beats a shed order.** Nothing switches off, nothing cascades, the pip is the last call. The first warning is in-world (Depot fill, belt thinning), not a HUD number.
- **Turrets are invulnerable; lamps and the block are the cost.** Removes a repair loop the brief excludes.
- **The engineer's body is the only skill layer** — rifle, sprint, dodge — and the game is completable without any of it. Guards: §19's 10 % shooting-time ceiling and the 5 % time-in-danger telemetry.
- **Edges fed per assembler is the tempo constant** and it is still unevidenced: one Mk1 covers a compact player for three hours, so C1 passes by absence. Gate B and the first tester rounds measure it; §8 of `TEST_RESULTS.md` holds the decision.
- **Stale reports mislead** — the C1/C2 "MISSED" that wasn't. `calibration.md` now carries the commit and config hash it was generated from and CI fails on a mismatch. Same for every generated report.
- **The roadmap was organised by system and missed the expectation baseline.** The standards audit (next section) ran on 2026-09-04 and found that the roadmap's own guesses were right on the loud complaints (undo, belt drag, blueprints, hidden QoL, the mid-game slog) and wrong on the quiet ones: belt lanes, auto-underground and the cursor-drop complaint were not in the evidence, and pipette, full refund and Q-to-clear were already built.

---

## 4. The standards audit — run on 2026-09-04 — and what it feeds

**Status: run on 2026-09-04** against b666c68 (`STANDARDS.md` is the matrix and `## Considered`; `STANDARDS_REPORT.md` is the claim-by-claim verdict, the moved rows, the five decisions with the cost of deciding late, and what the evidence could not settle). References: Factorio as the baseline, shapez 2, Mindustry, Captain of Industry, FOUNDRY, Oxygen Not Included and **Sandustry** as the recent indie (EA 2026-08-13, 97 % of 2,733 reviews; the newest 2D factory game with a review base large enough to read complaints from). Evidence per game is in `docs/standards/`. Three categories the brief did not name were added: the body (A), threat (B) and the city (C).

### The five that decide the category, and the five they'd forgive

**Decide** (day-one dealbreakers from the reviews): 1 blueprints and copy-paste, or a visible plan for gating them; 2 a goal after the first problems are solved (the mid-game slog — a design point E17 measures); 3 deletion that is safe (confirm or undo, and a full refund); 4 performance at launch; 5 onboarding that teaches the core loop and the QoL keys (hidden QoL is the same failure). **Forgive** at launch: multiplayer; mod support; controller and Steam Deck (unless the store page over-promises); pipette; localisation quality.

### The rows, as decided

Each row is a one-minute human check at the phase's DoD, written into `CONSTITUTION.md`'s DoDs and `PROGRAMME_STATE.md`'s standing block. Row numbers are `STANDARDS.md`'s.

| Phase | Build-with rows (ship with the system they touch) | Add-later rows |
|---|---|---|
| 4 layout pass | key strip on the HUD (4.3); the kerb pip in the world view for segment state and empty turrets (B.3, B.6); the Depot reads from the viewport's far edge (C.2) | — |
| 4 Gate B | the hand-lamp sentence (A.7): played dark, does the dark read as the claim's price or a missing flashlight | — |
| 5 | belt drag with corners and the underground span shown (1.3, 1.4); undo/redo in the command layer (1.10); copy/paste with a clipboard, the blueprint file the DoD already needs (1.11); the hotbar as references to the recipe table with room for §13 (1.16); one binding table (1.17) and one string table (8.5); splitter priority on its panel (2.3); a filtering primitive (2.5); the machine status enum — running / starved / blocked / no power / no ammo — on the panel (2.6); rates on the machine panel (2.7); alt-mode data per entity (2.9); per-item production and consumption counters readable in game (3.1); trams and the driven truck (2.11, 7.4, A.9) | — |
| 6 | alerts that jump the camera (3.3); an off-screen arrow to the block under attack (3.4); turret range on placement and select (B.5); enemy hover with HP and target (B.7) | — |
| 7 | the survivor panel as the recipe browser: what makes this, what uses this, who unlocked it (3.5); the "Blueprints and copy-paste · not yet found" row placed 2026-09-04 becomes the survivor (dealbreaker 1) | — |
| 8 | blueprint library with folders, icons, strings (1.12); mirror / flip (1.7); area select for removal with the retreat and the ring editor (1.14); the Line truck's order list as the ghost layer (1.9) | — |
| 9 | generated, overwritable names for blocks and districts, used by the toasts (C.1) | charting (7.2); pins with an off-screen arrow (7.3) |
| 10 | — | the difficulty readout (B.10) |
| 12 | — | production and power graphs (3.1, 3.2); alerts panel with per-type mute (3.8); hints that stop repeating, with a toggle (4.2); the place that teaches the QoL (4.3); idle/working animation from the status enum (5.1); feedback on every action (5.3, A.6); reactive music and SFX (5.5); decor and district silhouettes (6.2, C.7); autosave ≤ 10 min in three rotating versioned slots, off the frame (8.1, 9.3); settings with UI scale, rebinding, colour-blind shape codes (8.3, 8.4); alt-mode art (2.9); pockets as a grid with the Shift/Ctrl grammar (A.1) |
| 13 | — | cloud saves (8.2); achievements for measured things (8.6); E22's numbers as the store page's minimum spec (8.10); the Steam Deck pass under D-SA-1 |

What moved from the earlier guess (`STANDARDS_REPORT.md` §3): belt lanes, auto-underground, delete-cleanup, splitter filters, belt-hover throughput, upgrade planner, unlock videos and the seed browser went to `## Considered` (diverged — no reference majority); ghost placement moved from Phase 5 to Phase 8 with the Line truck (§22 has no builders); cloud saves moved from Phase 12 to Phase 13 (Steamworks); pipette, full refund and Esc/Q were already built; the autosave interval is ≤ 10 minutes, not hourly. Two rows were new: the off-screen attack indicator (Phase 6) and place names (Phase 9). One row lands on a taken decision: the automatic personal light (four of five avatar games) against D-B5-1 (a) — it is Gate B's sentence, not a reversal.

Seven places where the doc or the constitution disagrees with the build or the baseline, reported and not resolved (`STANDARDS_REPORT.md` §2): §14 two-lane belts vs `flow.ts` one lane; §14/§19 "blueprints and copy-paste from minute one" vs the constitution's Phase 8; the constitution's "three inserters" vs §13's one; the constitution's "shed order" (Phase 5, M3, E2) vs D-B3-4's brownout; §4's "rates behind a debug key" vs the statistics baseline; the constitution's "autosave hourly" vs ≤ 10 minutes; D-B5-1's no hand lamp vs the avatar-game norm.

### The five human decisions (`DECISIONS.md` D-SA-1–D-SA-5)

| ID | Decision | Recommendation | Cost of deciding late |
|---|---|---|---|
| D-SA-1 | Controller / Steam Deck | No native controller at launch; a shipped Steam Input layout and a "Playable" Deck pass in Phase 13; every action keyboard-reachable; not on the store page | if decided after Phase 12, its panels are rebuilt for focus navigation |
| D-SA-2 | Localisation | Yes: the string table in Phase 5 (free), translation bought in Phase 13 | a sweep of every literal (~30 toasts, every panel, the key line) — about a week, once |
| D-SA-3 | Mod support | Data-only surface (recipes, presets, strings; Workshop for blueprints and seeds); no scripting | none if "no scripting" is decided now; a scripting surface later is a fourth system by proxy |
| D-SA-4 | Multiplayer | No at launch; `step(state, commands)` stays deterministic, the command log the replay format; a determinism check in Phase 11's DoD | only if Phase 11's allocation work breaks determinism unnoticed |
| D-SA-5 | Screen-feedback intensity and toggle | Low by default (a flash on the hit block, no shake); one 0–4 slider | decide before Phase 12's art brief, or the brief is written twice |

Full matrix with the per-game evidence, the Relight column, the gap type and the flag: `STANDARDS.md`.

---

## 5. What each remaining phase owes

- **Phase 5 — the factory.** §12–§14 in full: every resource, intermediate and recipe generated into `recipes.ts` (and from it, the doc's tables); the §13 machine list and nothing outside it; undergrounds, splitters, three inserters, chests, trams (one per track, two stops, six inserters a stop, lines drawn on the map); rubble as finite typed ore with visible depletion; power complete under proportional brownout; slots as substation connections shown on the sprite (D-B2-2's watchable form). **Plus the 14 STANDARDS build-with rows (§4): belt drag with corners and undergrounds' span; undo/redo; copy/paste; the hotbar as references; one binding table and one string table; splitter priority; a filtering primitive; the machine status enum; panel rates; alt-mode data; production counters; trams and the truck.** E10 chain throughput, E11 coal depletion, E12 tram sufficiency. **DoD:** a bot builds the 5 h §18 territory from a blueprint at doc rates; a human builds a two-assembler ammo line unaided in ten minutes; each STANDARDS row for this phase checked in a minute; no new gap introduced without a row.
- **Phase 6 — threat.** Three enemies exactly; every §5 rot rule at tile level; the fall, retake and machine loss; Cannons and shells with the interior-siting rule; wells with the all-neighbours kill and the Relight surge. E13 enemy purpose, E9-full (the hold with the wake-size bloom on every awake block and a banked stock), E14 hulk niche (if under 5 % of edges outside wells, the Cannon line goes to a human). **STANDARDS build-with rows: alerts that jump the camera; an off-screen arrow to the block under attack; turret range on placement and select; enemy hover with HP and target.**
- **Phase 7 — found tech.** Facilities restored by claim plus a belt delivery; survivors on Held; the HQ survivor panel as the entire tech screen and the recipe browser; no research menu. **STANDARDS build-with row: the panel answers what makes this, what uses this, who unlocked it; the panel's "not yet found" blueprints row (placed 2026-09-04, dealbreaker 1) becomes the survivor who brings them.** E15 reachability over 10,000 seeds, E16 survivor pull.
- **Phase 8 — territory tools.** The §19 tedium audit's removal schedule on time: Foreman kits, the Line truck (driven first, automated later — the walking row's removal tool), blueprints, one-action retreat, the ring editor at tile level. E17 actions per block before and after each tool — and the "solved by hour three" complaint measured as the gap between tools. **STANDARDS build-with rows: the blueprint library with folders, icons and strings; mirror/flip; area select for removal; the Line truck's order list as the ghost layer.**
- **Phase 9 — the city.** Mostly built. Owed: E18 seed fairness (10,000 seeds through compact *and* spike; first-enclosure and Turbine-hall spread stated in the doc as run variance), E19 preset identity, the human test of naming what differs across ten seeds, map pins and charting. **STANDARDS rows: generated, overwritable names for blocks and districts used by the toasts (build-with E19); charting and pins with an off-screen arrow (add-later).**
- **Phase 10 — progression and endgame.** The §15 arc; the Relight with banking as intended play (D-P3-4, the ~20,000-magazine bank); no end screen that stops play; the difficulty slider and the quiet city as generator parameters and nothing else. E20 full run over 100 seeds, E21 quiet city (if no factory gets built without the threat, that is a stop-condition finding). **STANDARDS add-later row: the difficulty readout.**
- **Phase 11 — performance and scale.** 2019-laptop targets: 313 held blocks, 60 live edges, 4,000 belt segments, 200 machines, 60 fps world view, 16× map view, a 25 h save under 20 MB loading in 3 s. Typed arrays, struct-of-arrays, pooled bodies, no per-tick allocation. E22 megabase. **Engine gate:** if the targets fail after the allocation work, the numbers go to the human who decides on the C#/Godot port. **D-SA-4's check: a replay of a 5 h command log reproduces the state byte for byte after the allocation work.**
- **Phase 12 — interface, onboarding, art, sound.** §11 as the entire onboarding until E23 cold player passes 4 of 5; **the 12 add-later STANDARDS rows (§4): graphs, the alerts panel, hints that stop, the QoL teaching place, animation from the status enum, feedback on every action, reactive audio, decor and silhouettes, autosave ≤ 10 min in three slots, settings, alt-mode art, the pockets grid;** accessibility (shape-coded states, rebinding, UI scale, no reflex-required input outside the engineer's body); save/load with versioning; **art direction (a human decision)** under §4's legibility rule, with the light map as the finished visual and feedback density as the brief — every action a sound, a particle, a number that moves; audio with the turret line as a rhythm you learn to hear stop. No mechanic arrives through art or UI.
- **Phase 13 — balance and Steam.** E20 on 1,000 seeds per change with a sensitivity row per constant and a bot per §24 degenerate strategy; three human rounds in the `TEST_RESULTS` template; the shell chosen on Phase 11's numbers; Steamworks, achievements, store page, a demo of §11 through the first enclosure; Steam Deck verification under D-SA-1. **STANDARDS add-later rows: cloud saves; achievements for measured things; E22's numbers as the minimum spec.**
- **Phase 14 — launch.** Next Fest, telemetry against E20's distributions, release. One rule after: **no fourth system.**

Experiment numbering: the constitution's E10–E23 and the doc's own run tags collide; `PROGRAMME_STATE.md` §0.8 carries the map and `EXPERIMENTS.md` carries both names per row.

---

## 6. The counters the programme keeps

- **The untagged set** — 44 (Phase 0) → 41 (Phase 3) → 40 (M1) → 37 (M2) → 33 (M3) → 33 (prompt B M1–M6 and the verification pass; every recount B.1–B.15 reads 33 → 33) → 33 (the economy fix, B.18). 21 tile-scale, 12 design inputs. Gate B's `[play]` tags are the next expected drop.
- **§26 systems** — three (belts/inserters/machines, the front, found tech as a map), complexity **5/10**, unchanged at every recount including the rework, D5, D-B3-4 and M4. The engineer's body (rifle, sprint, dodge) is presence, not a system, by the constitution's stated exception; it reaches four if the rifle grows rules of its own or the engineer grows needs. Stop condition; never fired.
- **Gates** — Gate A ✅ (owner-only); **Gate B** ahead at the end of Phase 4; the **engine gate** in Phase 11.
- **Generated-report freshness** — built (guardrails Step 6, 2026-09-04). Every generated file carries `source_commit` and `config_hash` (markdown first line, JSON top-level keys, a `.json` sidecar per PNG); `npm run freshness:check` recomputes the hash from the checked-out code and fails CI if a stamp is missing, the commit is not an ancestor of HEAD, or the hash differs. Not covered: `docs/experiments/lattice/` (the archive, ancestry only), the D6 fixtures, a dirty tree at generation, and a rule change that leaves the config unchanged (`snapshot:check` and the suite catch those). See `GUARDRAILS_REPORT.md` §6.
- **Standards** — 52 matrix rows carried a gap on 2026-09-04 (by category: controls 10, logistics 6, information 6, onboarding 2, look 3, environment 1, map 3, meta 6, performance 1, body 4, threat 6, city 4), merging to **46 check rows** where one check closes rows in two categories; build-with rows per phase: layout pass 3, Gate B 1, Phase 5 14, Phase 6 4, Phase 7 1, Phase 8 4, Phase 9 1 (28); add-later rows: Phase 9 2, Phase 10 1, Phase 12 12, Phase 13 3 (18); **closed: 0 of 46** (the first closes are the layout pass's three); 29 items under `## Considered`, 16 of them excluded on purpose by §22 / §23 / the constitution; five decisions open (D-SA-1–5).
- **Guards on the engineer** — shooting time ≤ 10 % of the hour, time in danger ≤ 5 %, walking retagged from play (bot: 2.6–4.1 % in hour one, which is correct for an hour spent on the HQ lot).

---

## 7. What is waiting on a human right now

| What | Where | Blocking? |
|---|---|---|
| **Controls walkthrough** on the reference machine | pre-M2 check list | Yes for Gate B; first, because Gate B's hour is played with these controls |
| **Two-assembler line unaided**, ten minutes | M2 DoD | Yes for Gate B |
| **Gate B** — play the hour, fill the §19 table, `verdict: proceed` | `SLICE_REPORT.md` | Yes — Phase 5 does not open without it |
| **Art direction lean** — grim-and-quiet (deep blues, warm lamp light) or more saturated | first Phase 12 decision, informs the readability pass palette | No, but the readability pass will ask |
| **D-SA-1 to D-SA-5** — controller / Deck, localisation, mods, multiplayer, screen feedback | `DECISIONS.md`, `STANDARDS_REPORT.md` §4 | No for Gate B; D-SA-2's string table is a Phase 5 row either way, D-SA-1 constrains Phase 12's panels |
| Gate A tester sessions (D-P3-9) | `TEST_RESULTS.md`, `DEFERRED.md` | No — recommended in parallel; a session that contradicts a `[play: Gate A]` lock moves the lock |
| D-P4-4/5/9, D-B1-1, D-B6-1 to 3, the lattice pip bands, the half-lit street | `DECISIONS.md` | No — M6's bot evidence is in (`SLICE_REPORT.md` M6, `E-hour`); settled at Absorb Gate B from the played hour |
| PRs #1–#4 open and unmerged; branch protection refused on the free plan | GitHub `Deedubsy/Relight` | No — CI green on each; merge to keep the record honest |

---

## Changelog

- 2026-09-04 (guardrails, Step 6) — the record is on `main`: PR #1 merged as b181067, #2 as 7278dd9, #3 as 055d5cb, and #4 (this branch, through this commit) as the merge commit that follows it on `main` (its hash is in `GUARDRAILS_REPORT.md` §7). The "PRs #1–#4 are stacked and unmerged" line left Risks (five lines remain); the §6 freshness counter reads built.
- 2026-09-04 (guardrails, Step 3) — §0 "Next actions" (eight owned lines, ticked only by evidence) and "Risks" (six lines) inserted above the summary paragraph.
- 2026-09-04 (audit, Steps 1–3) — §4 rewritten wholesale as the summary of `STANDARDS.md` (run on 2026-09-04): the top-five lists, the injection table as decided, the moved rows, the seven reported disagreements, D-SA-1–D-SA-5 with cost of deciding late; every "hypothesis / unconfirmed / pending / not yet run" marker removed (line 3, the summary paragraph, §1's audit and Phase 5/12 rows, §3's last bullet, §5's Phase 5 line, §7's three rows); the STANDARDS rows written into §1's evidence column, §2's layout-pass and Gate B items and §5's phase lines; §6 gains the `standards` counter.
- 2026-09-04 (audit, Step 0) — state corrected through b666c68 against `PROGRAMME_STATE.md`, `SLICE_REPORT.md`, `DECISIONS.md`, `DEFERRED.md` and the git log: M5, M6 and the verification pass recorded as built and verified (one-paragraph summary, §1 Phase 4 row, §2 built list); the owed table loses M5 and M6 and gains the Depot fill level (no decision on record), the tile-scale E-rifle (the block-scale one is real and in CI) and M6's three unrun checks; the D-P4-8 citation fixed (superseded by D-B1-4); experiments 12 → 13; the untagged counter carried through B.15; the "generated-report freshness" counter corrected from claimed to partial; §7 rows updated. §4 untouched.
- 2026-09-04 (evening, rev 2) — §4 marked explicitly as pre-audit hypothesis; Phase 5's DoD marked not final until the audit runs.
- 2026-09-04 (evening) — rewritten after prompt B M4: state updated through M4 and D-B3-4; the pre-M2 controls pass, proportional brownout, invulnerable turrets and the engineer's body recorded; the standards audit and the layout/readability pass added as owned work; §4 (audit and reference lessons) and §3 (what was learned) added; provisional STANDARDS rows injected per phase; counters updated (freshness check, engineer guards); the human queue updated.
- 2026-09-04 — created at the end of the D5/D6 rework, from `PROGRAMME_STATE.md`, the constitution, `DECISIONS.md` and `DEFERRED.md`.
- 2026-09-04 (economy fix) — §0 lines 1, 3 and 4 ticked (evidence: `SLICE_REPORT.md` "Economy fix"; the five decisions came by chat message, recorded with provenance in `DECISIONS.md`); line 2 ticked on the full experiments run (`EXPERIMENTS.md` carries the tile sections); §4 / §5 Phase 7 carry the blueprints placeholder row.
