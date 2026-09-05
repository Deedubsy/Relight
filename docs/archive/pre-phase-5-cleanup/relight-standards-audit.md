# Standards audit: the modern 2D factory-game baseline

Relight's roadmap is organised by system. Modern factory games are judged on a second axis: the affordances players expect from Factorio and everything since. This audit builds that list, scores Relight against it, and injects each item into the roadmap phase where it is cheapest to build — many are cheap only if built with the system they touch, and expensive if bolted on in Phase 12.

Do not build anything in this task. Do not add mechanics. The output is `STANDARDS.md`, roadmap edits, and a report.

## Step 1 — The baseline, from evidence

Build the checklist from the games, not from memory. For each reference game, list what it does in each category below, from its documentation, its controls screen and its store page: Factorio (the baseline), Shapez 2, Mindustry, Captain of Industry, Foundry, Oxygen Not Included, and one recent indie 2D factory game released in the last 18 months (search for it; name it and why). Note where the genre has converged (every game does it) versus diverged (only Factorio does it).

Categories:

1. **Controls** — movement, placement (drag, line, rotate, mirror), pipette, ghost placement, undo/redo, copy/paste, blueprints, upgrade and deconstruction planners, area select, hotbar, quickbar paging, rebinding, controller, Steam Deck.
2. **Logistics affordances** — belt lanes, belt drag with auto-underground, splitter filters and priority, inserter filters and stack sizes, item hover showing throughput, "what's on this belt".
3. **Information** — production and consumption graphs, power graph, alerts with click-to-jump, recipe browser (what makes this / what uses this), machine tooltips with rates and bottleneck reason, alt-mode overlay (contents, recipes, filters, ranges), a minimap and a chart map with tags.
4. **Onboarding and help** — tutorial or scripted first hour, tips-and-tricks, in-game reference, first-time hints that stop repeating.
5. **Look and feel** — machine animation states, item movement on belts, particles and feedback for fire/damage/build/destroy, light and shadow, ambient sound, music behaviour, screen feedback (shake, flash) and its accessibility toggle.
6. **Environment** — terrain and biome variety, decor and props, landmarks, water and its rules, day/night (and whether it affects play), ambient life, weather.
7. **Map and world** — exploration and charting, fog, map tags and pins, long-distance travel, the "where am I" answer.
8. **Meta** — save slots, autosave, cloud saves, settings (graphics, audio, UI scale, colour-blind), localisation, achievements, mod support, multiplayer, replays or screenshots.
9. **Performance expectations** — late-game entity counts players consider normal, UPS/FPS separation, time-scaling.

## Step 2 — Score Relight

For every item: **built / planned (phase N) / not planned / never (with the doc section that excludes it).** "Never" is allowed only where the doc excludes it on purpose (§22, §23, the constitution) — e.g. circuit networks (fourth system), logistics bots (§14's Depot rule), day/night affecting play (§22). Anything not planned and not excluded is a gap.

For each gap, state: is it converged (every reference game has it) or diverged; what it costs; and **which phase it is cheapest in**, with the reason (e.g. "belt lanes: Phase 5 with belts, or never — lanes cannot be added to a single-lane belt sim later without rewriting item movement").

## Step 3 — Inject into the roadmap

Edit `PROGRAMME_STATE.md`'s roadmap (and the phase prompts where they exist):

* Every converged gap gets a row in its cheapest phase's DoD, phrased as a check a human can run in a minute ("drag a belt across a lot; it auto-undergrounds a machine in its way; Ctrl+Z removes it").
* Diverged gaps go to a `## Considered` section in `STANDARDS.md` with a one-line reason to skip or a phase, for a human to decide.
* Items that must be built with a system get flagged **build-with**; items that can be added later get flagged **add-later**. Phase 5 will collect most of the build-with rows (lanes, drag, undo in the command layer, ghosts, pipette, copy/paste, alt-mode data); Phase 12 the add-later ones.
* Add two rows to the constitution's per-phase DoD: "the STANDARDS.md rows for this phase are checked" and "no new gap was introduced without a row".

## Step 4 — The five that decide the game's category

Name the five gaps whose absence would make a Factorio player call Relight unfinished on day one, and the five whose absence they'd forgive. Justify from the reference games' reviews (search for common complaints in each game's early reception — "no undo", "no blueprints", "no lanes" are recurring). This list goes at the top of `STANDARDS.md`.

## Step 5 — Decisions for a human

List, with a recommendation and its cost:

* Controller and Steam Deck support (converged among recent games; touches Phase 12 and the hotbar design in Phase 5).
* Localisation (a string table from Phase 5 on is near-free; retrofitting is a month).
* Mod support (the data-driven `recipes.ts` makes recipe and machine mods plausible; the sim's purity makes it cheap; the question is whether the doc's "no fourth system" survives contact with modders).
* Multiplayer (the deterministic sim makes lockstep co-op feasible; recommend no at launch with the door left open, or say why not).
* Screen feedback intensity and its toggle (art-direction adjacent).

## Report

`STANDARDS.md` with the matrix, the top-five lists and the Considered section; the roadmap edits; `STANDARDS_REPORT.md` with the reference-game evidence, the count of gaps by category, which phases gained rows, and the five decisions. Do not soften a gap because it's late in the roadmap; the point of the audit is to find the ones that got late.
