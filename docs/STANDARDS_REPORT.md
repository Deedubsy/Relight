# STANDARDS_REPORT — the standards audit, run on 2026-09-04

Against `phase-4` at b666c68. Brief: `docs/relight-standards-audit.md`. Output: `STANDARDS.md` (the top-five lists, the matrix, the injection summary, `## Considered`), `ROADMAP.md` §4 rewritten from it, the DoD rows in `CONSTITUTION.md` and `PROGRAMME_STATE.md`, and D-SA-1–D-SA-5 in `DECISIONS.md`. Evidence per game: `docs/standards/`. Nothing was built and no mechanic was added.

Method: for each reference game the documentation was read first (Factorio Friday Facts and wiki; the shapez 2 wiki and 1.0 notes; Mindustry's source on GitHub — `Binding.java`, `HudFragment`, `Placement`, `BlockStatus`, `SettingsMenuDialog` and twenty more files — plus its Steam page; the Captain of Industry, FOUNDRY, ONI and Sandustry wikis, store pages and patch notes), then the reception (Steam reviews by theme, Steam discussions, Metacritic user reviews, VaporLens mining, the forums), then the same for the recent indies whose complaints define the dealbreakers (Techtonica, Astro Colony, Desynced, Factory Town, ShapeHero, DSP, Satisfactory). Relight's column was read from the code (`packages/game/src/worldScene.ts`, `main.ts`, `panel.ts`, `session.ts`; `packages/sim/src/flow.ts`, `engineer.ts`, `hour.ts`, `light.ts`, `threat.ts`) and the doc (§4, §13, §14, §19, §22, §23), not from the reports.

---

## 1. Roadmap corrections from Step 0 (before the audit's outputs)

`ROADMAP.md` §1 and §2 were checked claim by claim against `PROGRAMME_STATE.md`, `SLICE_REPORT.md`, `DECISIONS.md`, `DEFERRED.md` and `git log`; `PROGRAMME_STATE.md` wins where they disagreed. §4 was not touched in Step 0. The corrections, each with its evidence:

| # | Roadmap said | Evidence | Fixed to |
|---|---|---|---|
| 1 | Phase 4 state "M1–M4 built; M5, M6 owed" | `PROGRAMME_STATE.md` B.11, B.13, B.15; commits f24f383, b666c68 | M1–M6 built and verified on the pass of 2026-09-04; the one-paragraph summary, the §1 row and the §2 built list say so |
| 2 | M5 "Depot fill level on its sprite — decided 09-04" | no such decision in `DECISIONS.md` (D-B5-1..3 are the lamp, the pace, the repair cost), `SLICE_REPORT.md` M5 or `DEFERRED.md`; M5 did not build it | carried as an open item for the layout pass, "after a human confirms it", not as a decision |
| 3 | "E-rifle owed" as if nothing existed | `EXPERIMENTS.md`: the block-scale E-rifle is in CI (steady 5 h ± rifle 0.00 %; twelve of twelve rescues hold either way) | the block-scale run is real; the tile-scale one is owed, wanted before Gate B, not blocking it |
| 4 | M6 listed with three added checks as done | `PROGRAMME_STATE.md` B.13 / B.15: the coal-margin check, the arrival count at the first red pip, and the walking % from play were never run | listed as owed: the first two to the next verification pass, the third to Gate B |
| 5 | Absorb Gate B cites D-P4-8 for the lattice pip bands | D-P4-8 is the start-turret count, superseded by D-B1-4 on 2026-09-04 | the citation corrected; the bands carried without a decision id |
| 6 | "12 experiments" | `EXPERIMENTS.md` and B.15: 13 (E1–E9, E-rifle, E-walk, E-variance, E-hour) | 13 |
| 7 | §6 "every generated report carries its source commit and config hash; CI fails on a mismatch" | only the snapshot check gates; only config hashes are written; no report carries its commit | the counter reads *partial*; the full rule is a `DEFERRED.md` item owned by the next verification pass |
| 8 | §7's human queue stale on the same points | as above | the rows updated (M6's evidence in; Absorb Gate B's list corrected) |

Three `DEFERRED.md` items were added by Step 0 (generated-report freshness in full → next verification pass; M6's two unrun checks → next verification pass; E-rifle at tile scale → before Gate B if a go falls free, else Absorb Gate B). The constitution, which lived only in the prompt file, was copied to `docs/CONSTITUTION.md` so its DoD lines could carry rows.

---

## 2. Claim-by-claim verdict on the old `ROADMAP.md` §4

Each lesson and each provisional injection row is marked **confirmed / corrected / refuted** with its source. Rows whose phase or flag the evidence contradicted were moved (§3 below).

### 2.1 The lessons

| Claim | Verdict | Evidence |
|---|---|---|
| FFF-280: visual feedback makes actions feel real | **confirmed** | `docs/standards/factorio.md` — FFF-280 "Visual Feedback is the King"; the 2015 "no activity state" complaint it answered |
| FFF-191: the toolbar is shortcuts, not inventory | **confirmed** | FFF-191 "Quickbar as shortcuts": slots reference items; a filter slot puts a ghost in the cursor. Mindustry's category-plus-grid and Sandustry's 10×10 do the same |
| A GUI style guide with one window grammar | **corrected** | the guide is the community's (raiguard's), not Wube's, and says its rules are "merely suggestions"; the grammar (standard vs dialog windows, Back / Confirm corners) is real but not Wube's published rule. Kept as a Phase 12 reference, not a row |
| FFF-337: statistics as a first-class view | **confirmed** | FFF-337 production statistics redesign; Desynced's "crucial" stats review; Satisfactory's six-year request; FOUNDRY's "no production statistics" complaint. Row 3.1 |
| shapez 2 praise: in-place layout issues, open-end previews, tooltips, world labels, blueprints with folders / icons / bar, delete cleans up, unlock videos, skill academy | **confirmed** as praise, **corrected** as a baseline | each item is real (`shapez2.md`), but delete-cleanup and unlock videos are shapez 2 alone across the seven references — diverged, `## Considered` |
| shapez 2 criticism: a "belts full" stat nobody understood | **refuted** (not found) | no review, discussion or wiki page mentions such a stat; the belt-reader complaint found is that the *Belt Reader building* is unlocked late. The lesson (a rule that is not watched) stands on Mindustry's F6 and Captain of Industry's R / F instead |
| Indie complaint: no undo | **confirmed**, loud | DSP launch threads (65 and 92 replies), Captain of Industry's 110-vote request, FOUNDRY, Sandustry (a third-party mod exists). Row 1.10 |
| belts one segment at a time instead of dragged | **confirmed**, press day one | FOUNDRY "ABYSMAL" launch placement; Techtonica; every reference has drag. Row 1.3 |
| no throughput display or congestion overlay | **confirmed** (medium) | FOUNDRY, Sandustry; but the fix players ask for is machine status and stats (rows 2.6, 2.7, 3.1), not belt hover (shapez 2 alone) |
| accidental deletion with no full refund | **confirmed**, loud | DSP, Captain of Industry (8 threads), Satisfactory; Sandustry "no refund on deletion". Relight refunds in full already (1.15) |
| no way to drop what's in the cursor | **refuted** (not found) | no review or discussion in any reference; Relight has Q, right-click and the "in hand" toast (1.18). Dropped |
| QoL hidden in tooltips or key settings | **confirmed** | Captain of Industry's R / F "after 25 hours"; Mindustry's `[` `]` and F6; Sandustry's hotkey strip as the praised answer. Rows 4.3 (layout pass, Phase 12) |
| the mid-game slog | **confirmed**, the loudest theme | FOUNDRY, Desynced, Techtonica, Captain of Industry (35 mentions), Astro Colony, shapez 2. A design point; E17 measures it; dealbreaker 2 |

### 2.2 The provisional injection table

| Old row | Verdict | Where it went |
|---|---|---|
| Phase 5: belt lanes | **corrected** | Factorio alone has lanes (shapez 2 one lane on three layers; Mindustry, CoI, FOUNDRY, ONI, Sandustry none). `## Considered`; the §14-vs-`flow.ts` disagreement reported (§2.3) |
| Phase 5: drag-place with corner handling | **confirmed** | 7/7. Row 1.3, build-with |
| Phase 5: auto-underground | **corrected** | Factorio and Mindustry only. `## Considered` |
| Phase 5: undo / redo in the command layer | **confirmed** | converged by demand. Row 1.10, build-with |
| Phase 5: ghost placement | **corrected** (phase) | ghosts need a builder; §22 excludes robots; the Line truck (Phase 8) is the one thing with an order list. Row 1.9, Phase 8 |
| Phase 5: the shortcut toolbar | **confirmed** | 7/7. Row 1.16, build-with; §13's list will not fit the built `1–9 0 [ ]` row |
| Phase 5: pipette | **refuted** as a gap | built (Q). Row 1.8, none |
| Phase 5: copy / paste with a clipboard | **confirmed** | 5/7; dealbreaker 1. Row 1.11, Phase 5 build-with (the DoD's blueprint file) |
| Phase 5: delete cleans up orphans | **corrected** | shapez 2 alone. `## Considered` |
| Phase 5: full refund | **refuted** as a gap | built. Row 1.15 |
| Phase 5: Esc / Q drops the cursor | **refuted** as a gap | built. Row 1.18 |
| Phase 5: alt-mode data exposed by the sim | **confirmed** | 5/7. Row 2.9 (data Phase 5, art Phase 12) |
| Phase 5: item hover with throughput | **corrected** | rates belong on the machine panel (2.7, 5/7); belt hover is shapez 2 alone (2.8, `## Considered`) |
| Phase 5: splitter priority | **confirmed** | 5/6. Row 2.3 |
| Phase 5: splitter filter | **corrected** | 3/6. `## Considered` |
| Phase 5: inserter filters | **confirmed** as "a filtering primitive" | 6/6 applicable. Row 2.5; the "three inserters" disagreement reported |
| Phase 5: a string table | **confirmed** | every shipped reference ≥ 9 languages. Row 8.5 and D-SA-2 |
| Phase 5: hotbar designed for controller | **corrected** | a decision, not a row: D-SA-1 |
| Phase 6: turret and lamp range overlays while placing | **confirmed** | 3/3 defence games; the Floodlight cone already does it. Row B.5 |
| Phase 6: enemy hover with HP and target | **confirmed** | Row B.7 |
| Phase 6: alert events with jump-to | **confirmed**, plus a new row | Rows 3.3 and 3.4 (the off-screen arrow: Factorio, Mindustry, They Are Billions, Riftbreaker) |
| Phase 7: the survivor panel as the recipe browser | **confirmed** | 6/7. Row 3.5 |
| Phase 7: unlock notices with video | **corrected** | shapez 2 alone. `## Considered` |
| Phase 8: blueprints with folders and icons; area select; deconstruction planner; one-action retreat as a planner | **confirmed** | Rows 1.12, 1.14, 1.9, 1.7 |
| Phase 8: upgrade planner | **corrected** | 4/6 but Relight has no tiers to upgrade between. `## Considered` |
| Phase 9: pins and tags; charting | **confirmed** | Rows 7.3 / C.5, 7.2 (add-later) |
| Phase 9: seed browser in-game | **corrected** | Factorio alone. `## Considered`; the tools-package browser stays |
| Phase 12: graphs; alerts panel; hints that stop; settings; QoL teaching place | **confirmed** | Rows 3.1 / 3.2, 3.8, 4.2, 8.3 / 8.4, 4.3 |
| Phase 12: save slots, autosave | **confirmed**, interval **corrected** | ≤ 10 minutes, three slots, off the frame (8.1, 9.3); the constitution's "hourly" reported |
| Phase 12: cloud | **corrected** (phase) | Steamworks is Phase 13. Row 8.2 |
| Phase 12: controller, Steam Deck layout | **corrected** | D-SA-1 |
| Phase 12: screen-feedback toggle | **corrected** | D-SA-5 |
| Phase 13: achievements for measured things | **confirmed** | 6/7. Row 8.6 |
| Phase 13: Steam Deck verification pass | **corrected** | under D-SA-1 (a) it is a "Playable" pass with a shipped layout |
| Phase 13: store screenshots at full zoom-out and mid burn-off | **unsettled**, kept | no reference documents its screenshot rules; kept as Phase 13's store-page work, not a STANDARDS row |
| The five human decisions | **confirmed** | D-SA-1–D-SA-5 |

### 2.3 Where the build disagrees with the doc or the constitution (reported, not resolved)

1. **Belt lanes.** §14 line 296: "Standard two-lane belts, 7.5/s and 15/s". `packages/sim/src/flow.ts` line 70: "belts are one lane". The baseline is one lane (6/7). The human settles §14 when Phase 5 opens.
2. **Blueprints and copy-paste "from minute one".** §14 and §19 say so; the constitution puts blueprints in Phase 8; nothing is built. The audit injects copy / paste into Phase 5 (the DoD's own blueprint file) and the library into Phase 8; the doc's "minute one" is either edited to "Phase 5" or the constitution moves.
3. **Three inserters.** The constitution's Phase 5 says "three inserters"; §13 lists one Inserter row. Row 2.5 asks only for a filtering primitive; which inserter carries it is the human's.
4. **The shed order.** The constitution's Phase 5, M3 and E2 lines still say "shed order"; D-B3-4 (the human's decision, 2026-09-04) replaced it with proportional brownout, and the sim, tests and game follow D-B3-4. The constitution's sentences are stale.
5. **Rates behind a debug key.** §4 line 54: "the line's rates, the calibration counters sit behind a debug key". The baseline (4/7 plus the loudest indie requests) puts production statistics in the player's UI. Row 3.1 injects the counters into Phase 5 and the graphs into Phase 12; §4's sentence is below the baseline and is the human's to edit.
6. **Autosave hourly.** The constitution's Phase 12 line; the baseline is ≤ 10 minutes with slots (7/7). Row 8.1.
7. **The hand lamp.** D-B5-1 taken (a) none; four of five avatar references give an automatic personal light. Not softened and not reversed: Gate B row A.7 asks the human for one sentence after playing dark.

---

## 3. Rows that moved phase or flag, and why

| Row | From | To | Why |
|---|---|---|---|
| Belt lanes | Phase 5 build-with | `## Considered` | 1/7; and the doc-vs-build disagreement is the human's, not the audit's |
| Auto-underground | Phase 5 build-with | `## Considered` | 2/5 applicable; cheap once drag exists, decided at the drag build |
| Ghost placement | Phase 5 build-with | Phase 8 build-with (1.9) | needs a builder; §22 excludes robots; the Line truck's order list is the ghost layer |
| Pipette, full refund, Esc / Q | Phase 5 build-with | built | `worldScene.ts` Q pipette, right-click refund, Q / right-click clear |
| Delete cleans up orphans | Phase 5 build-with | `## Considered` | shapez 2 alone |
| Item hover with throughput | Phase 5 build-with | split: panel rates Phase 5 (2.7); belt hover `## Considered` (2.8) | 5/7 vs 1/7 |
| Splitter filter | Phase 5 build-with | `## Considered` | 3/6 |
| Controller hotbar | Phase 5 build-with | D-SA-1 | a decision; under (a) the hotbar's only constraint is "every action keyboard-reachable" |
| Unlock videos | Phase 7 build-with | `## Considered` | shapez 2 alone |
| Upgrade planner | Phase 8 build-with | `## Considered` | no tiers to upgrade between (Mk1 → Gunsmith is a recipe) |
| Seed browser in-game | Phase 9 build-with | `## Considered` | Factorio alone; the `packages/tools` browser is the DoD's |
| Cloud saves | Phase 12 add-later | Phase 13 add-later | Steamworks lands in Phase 13 |
| Controller, Deck layout, screen-feedback toggle | Phase 12 add-later | D-SA-1, D-SA-5 | decisions |
| Steam Deck verification | Phase 13 | Phase 13 under D-SA-1 | the pass's scope depends on the decision |
| Autosave interval | Phase 12 "hourly" (constitution) | Phase 12 "≤ 10 min, three slots, off the frame" | 7/7 |
| **New:** off-screen attack arrow | — | Phase 6 build-with (3.4) | 4/4 defence games; the roadmap's alert row had the jump but not the edge indicator |
| **New:** place names | — | Phase 9 build-with (C.1) | 5/6 city and island games; toasts say "Block (12,7)" today |
| **New:** key strip on the HUD; the kerb pip in the world view; the Depot from the viewport's edge | — | layout pass (4.3, B.3 / B.6, C.2) | the pass is already owed; these are its checks |
| **New:** the hand-lamp sentence | — | Gate B (A.7) | converged against a taken decision |
| **New:** machine status enum | — | Phase 5 build-with (2.6) | 5/7; the roadmap had the overlay but not the state the overlay draws |
| **New:** determinism check | — | Phase 11 DoD | D-SA-4's insurance |

Build-with vs add-later was flagged by one test: does retrofitting it touch the sim's command or data layer? Drag, undo, copy / paste, the hotbar's references, the binding and string tables, the status enum, the filter primitive, the alert events, place names and the blueprint format all do; graphs, hints, settings, autosave, decor, animation and the pockets grid read data the sim already has.

---

## 4. The five human decisions, and the cost of deciding late

| ID | Decision | Recommendation | Cost of deciding late |
|---|---|---|---|
| D-SA-1 | Controller and Steam Deck | (a) no native controller at launch; a shipped Steam Input layout and a "Playable" Deck pass in Phase 13; every action keyboard-reachable; not on the store page | Nothing until Phase 12. If native support is chosen after Phase 12's panels are built, every panel is rebuilt for focus navigation and the hotbar gets a radial — the roadmap's "constrains the Phase 5 hotbar" was wrong under (a): the sim never sees a device |
| D-SA-2 | Localisation | (a) yes: the string table in Phase 5 with the binding table; translation bought in Phase 13 for the languages the wishlist shows | A sweep of every literal in `packages/game` (~30 toasts, every panel, the key line, the ghost reasons) — about a week, once, and it lands in Phase 13's busiest weeks. Deciding (b) "table, English only" now costs the same as (a) and forecloses nothing |
| D-SA-3 | Mod support | (a) data-only: recipes, machine list, presets, strings as overridable files; Workshop for blueprints and seeds; no scripting | None if "no scripting" is decided now — Phase 5's generated `recipes.ts` and Phase 9's presets-as-parameters are the surface already. If scripting is wanted later, the command layer is the hook and Phase 11's determinism check must include it; and a scripted mod can add the fourth system the constitution forbids |
| D-SA-4 | Multiplayer | (a) no at launch; the sim stays deterministic, the command log stays the replay format, Phase 11's DoD gains the byte-for-byte replay check | Only if Phase 11's typed-array work breaks determinism unnoticed — the check is the insurance, and it costs one test |
| D-SA-5 | Screen-feedback intensity and its toggle | (a) low by default (a flash on the hit block, a particle on placement, a number on a hopper, no shake), one 0–4 slider | Decide before Phase 12's art brief is written, or the brief is written twice; the slider itself is one setting |

---

## 5. What the audit could not settle from evidence, and what would

1. **The search budget.** The seven research agents each had 200 web queries; every one ran out before its list was empty. Items marked "not found" (FOUNDRY's mirror key, Factorio hit feedback, the shapez 2 "belts full" stat, the cursor-drop complaint) are *not found within budget*, not confirmed absent. What would settle them: a second pass with the residual list, about 40 queries.
2. **Blocked hosts.** Reddit returned 403 to every fetch; Fandom wikis (402), miraheze (403), defkey, Rock Paper Shotgun, Eurogamer, IGN, Hooded Horse's site and ProtonDB refused. Reception was read from Steam, Metacritic, VaporLens, the official forums and GitHub instead. The Deck verdicts for FOUNDRY and Mindustry rest on Steam discussions and store badges, not ProtonDB.
3. **The Mindustry agent stalled** after ninety tool results; its file was compiled by hand from those results (twenty-plus source files read in full, the Steam and Metacritic pages, the Suggestions issues). Its controls, HUD, settings and threat rows are source-backed; its mod and localisation rows are store-backed; its "wave 500+ TPS" performance row is one review.
4. **Counts, not rates.** Review themes are counted (FOUNDRY blueprints 18, Captain of Industry tutorial 44, Techtonica performance 34) from VaporLens and Steam sorts; no reference exposes per-theme percentages of all reviews. The top-five lists rank by breadth across games, then by count. What would settle the ranking: SteamDB's review histogram per game around each launch, not fetched.
5. **The recent indie.** Sandustry was chosen on date (EA 2026-08-13) and review base (2,733); Dome Keeper, DINAO and Desynced were read for the threat and slog themes only. A different choice (ShapeHero, Astro Colony) would change the forgiven list's fourth item (pipette) — the only place a single game decided.
6. **Relight's column** was read from the code and the doc, not played. Two rows could be wrong in the player's favour: 1.2's ghost reasons and 4.5's costs are built but their legibility is the layout pass's and Gate B's to say. Nothing was scored "built" on a report alone.
7. **Three doc-vs-build disagreements** (§2.3 items 1–3) are not the audit's to settle and are left for the human at Phase 5's opening. The audit injected rows that hold under either resolution (a filtering primitive, not "filter inserter"; copy / paste, not "blueprints from minute one").
8. **The screenshot row** (old §4, Phase 13) had no reference evidence either way and stays as store-page work.
9. **What is not counted:** Factorio's Space Age (2024) and shapez 2's post-1.0 updates were read for controls and UI only; their content systems (planets, space platforms, wires) are `## Considered` fourth systems and were not scored.
