# Relight — build the game from the design doc

Starting point: `RELIGHT-design.md` and whatever else is in the repo. Nothing is assumed built. This is a programme of fourteen phases with a gate between each; you will finish it over many sessions, each of which starts by reading `PROGRAMME_STATE.md` and the last phase report, and ends by writing the next. Treat this file as the constitution.

Read `RELIGHT-design.md` in full before anything else. Then read every `*_REPORT.md`, `TEST_RESULTS*.md` and `DEFERRED.md` that exists. Then inventory `packages/`.

## Constitution

1. **The doc is the spec, the sim is the judge, the doc follows the sim.** Every rule is a sentence in `RELIGHT-design.md`; every number that matters carries `[sim: run]` or `[play: session]`. Code and doc disagreeing means the code is wrong until a run says otherwise, and then the doc changes with a changelog line. The doc never drifts into "whatever the code does".
2. **Three systems: the front, found tech, automated combat.** Recount against §26 at every gate. A phase that needs a fourth system stops and reports. Complexity above 6 is a rework trigger.
3. **Headless first.** The whole game lives in `packages/sim`, pure TypeScript, no engine, no DOM, fixed tick, deterministic, JSON-serialisable state, `step(state, commands) → state'`. Every feature is testable from a node harness before it has a pixel. The experiment suite runs in CI; a red experiment is a red build.
4. **Production is territorial.** Machines occupy lots; only interior blocks (plus the HQ) host production; rubble is finite; magazines cost their recipe. These are §5 and §12 rules and they exist from the first prototype onward. A prototype in which an assembler is weightless or ore is infinite cannot test the front, because a number is always cheaper than a shape.
5. **Bots are the instrument.** Four claim policies (compact, spike, cheapest, balanced) that also build when demand exceeds 80 % of production and a slot is free. Calibration means stating the behaviour a session must have and finding the config that produces it with bots; it never means adjusting a number until it feels right.
6. **Report gaps, don't fill them.** Where the doc is silent, implement the simplest thing, tag it `GAME-ASSUMPTION` with the question it stands in for, list it in the phase report. Never a silent invention. Never "I made it more fun".
7. **Humans decide constants, direction and verdicts.** Design constants, art direction, difficulty philosophy, engine swaps, go/no-go at the two human gates. Write the question and the evidence into the report; do not pick. `DECISIONS.md` logs every one.
8. **Rules are watched, not read.** No tutorial screens. §11 is the tutorial; every rule surfaces as a toast, a tooltip, a pip, or a thing happening on screen. A rule that can't be taught that way is suspect.
9. **Every phase ends the same way.** Experiments green; a five-minute smoke test a human ran; `PHASE_N_REPORT.md` (built, assumed, deferred, measured, three decisions for a human); `PROGRAMME_STATE.md` updated; `DEFERRED.md` re-read and every item given a phase or deleted with a reason; §26 recounted; **the `STANDARDS.md` rows for this phase are checked (one minute each, by a human), and no new gap was introduced without a row** (added 2026-09-04 by the standards audit).
10. **Stop conditions are outcomes, not failures.** If the experiments say the front doesn't produce the layouts §9 promises, or a human gate returns `kill`, or the factory has no reason to exist without the threat, or complexity can't be held at 6 — write the finding and stop.

## Engine and layout

Phaser 3 + TypeScript + Vite for rendering; Electron or Tauri for Steam (decided in Phase 13 on measured memory and startup). If Phase 11's performance gate fails after the allocation work, the sim ports to C# and the renderer to Unity; rule 3 is what makes that a month rather than a rewrite.

```
packages/sim        the game, engine-free
packages/game       Phaser renderer, UI, input, audio
packages/harness    node runners: experiments, bots, calibration, fixture export
packages/tools      seed browser, map viewer, telemetry analyser, save inspector
apps/steam          shell, Steamworks bridge, installers
docs/               RELIGHT-design.md, reports, PROGRAMME_STATE.md, DECISIONS.md, DEFERRED.md
```

---

## Phase 0 — Inventory and doc lock (3 days)

* List what exists: sim (Python, TS, neither), prototype, reports, test results. For each, whether it matches the doc as of today. Do not rebuild anything that exists and passes; do not trust anything that exists and was never verified.
* Read §25's open questions and §24's risks. For each, note which phase below answers it. Anything with no phase gets one or is deleted from §25 with a reason.
* Find every number in §5, §7, §12, §13, §14 and §15 without a `[sim]` or `[play]` tag. List them in `PROGRAMME_STATE.md` as the untagged set; the goal of Phases 1–4 is to empty it.
* Name the design constants that set tempo and are not yet evidenced. At minimum: **edges fed per assembler**, **claim cadence the game is drawn for**, **slots per block**, **magazine buffer cap**, **HQ start patch sizes**, **fall time**, **substation draw**. Each gets a `DECISIONS.md` line as "open" with its doc value.
* Create `PROGRAMME_STATE.md`, `DECISIONS.md`, and `DEFERRED.md` if absent.

**DoD:** the inventory, the untagged set, and the open-constants list are in `PROGRAMME_STATE.md`; nothing else has changed.

## Phase 1 — Headless front sim (1 week)

The block-level game with no tiles: 24×24 blocks, river and scattered inert, districts with rot caps and growth, depth multiplier, wells, wake and steady blooms with the 40 cap, claim → contested → burn-off → held, frontage and interior, the ammo ring with per-edge hoppers, the unfed-substation rule, block fall and retake, power draw with brownout shedding, machine slots on interior blocks, finite rubble pools, the magazine recipe, the four bots with the build rule.

* If a Python sim exists and is trusted, port it and prove the port with fixtures (maps and totals exported per seed; magazines and first-interior exact, RNG-dependent counts within 10 %). If nothing exists, build in TS directly and write the fixture export for the future.
* Experiments, each a named run with three seeds, results in `EXPERIMENTS.md`, every doc number they touch retagged:
  * **E1 unfed consequence** — starving an edge costs its block in a bounded time (target 5–15 min residential).
  * **E2 brownout** — a 5 % / 15 % / 25 % ten-minute shortfall; losses stop at restoration; the shed order that makes "nothing cascades" true, or the doc sentence changes.
  * **E3 enemy thresholds** — per-district shade and hulk fractions at steady state vs on wake blooms; the §7 cost table from the sim.
  * **E4 first hour** — §11 as written under the power model; the Generator count and coal that reach minute 60 without brownout.
  * **E5 early bite** — demand ÷ production at 1 h, 3 h, 5 h per policy.
  * **E6 inert walls** — river-hugging and scatter vs compact; the validator rule if needed; the map (open or scattered) the doc's headline ratios refer to.
  * **E7 shape ratios** — spike/compact and cheapest/compact on the canonical map; §7 and §9 retagged.
  * **E8 cadence** — the claim gap that produces §18's drawings.
  * **E9 Relight hold** — wells ×3 plus the wake-size bloom on every awake block, with and without a banked stock.
* Report every place the sim and the doc disagreed before any doc edit.

**DoD:** experiments green in CI; the untagged set is at most a third of its Phase 0 size; `PHASE_1_REPORT.md` names the doc contradictions found and the runs that settled them.

## Phase 2 — Map-view prototype (2 weeks) → Human gate A

One screen, Phaser, the block grid only. The purpose is to test whether choosing blocks on a map with a front count is interesting on its own; if it isn't, nothing after it matters.

* Grid at 24 px, states (dark with rot mottle, contested, held, interior, inert), wells, facility silhouettes with names, survivor markers. Claim tool with tooltip `Claim — rot N % · front +N · closes N`. Pole line on claim. Front pips per edge (green/amber/red from hopper level; shape-coded, not colour-only). Ammo ring order as a drag list. Bloom pulses sized by crawler count, shades and hulks marked, wake blooms bigger. Slots drawn on held blocks; rubble strip fading. HUD: held/front/interior, ammo made vs demanded, stock, blocks lost, slots, clock, speed 1×/4×/16×, pause. Seed in the URL. `?state=` loads a snapshot.
* Economy: rubble yields per district into a pool; claims and assemblers cost stock; magazines cost the recipe; assemblers need a free interior slot. No power in the prototype — poles are the claim gesture.
* Telemetry: every claim (time, block, district, rot, F and interior before/after), every loss, reorder, build, rejected build, run-dry, per-sim-minute shape metrics (bounding box, perimeter/area), first enclosure; JSON export; session summary on screen.
* **Calibration by bots** (rule 5). Targets for the session the testers will play, at the E8 cadence, three seeds:
  * compact: first enclosure before first amber; no red before 120 min; 0 lost and no stall to 180.
  * spike: red by 90 min; 1–4 distinct blocks lost by 180; never more than 2 assemblers before 120.
  * cheapest: no stall; ammo per held block within 0.7–1.3× compact.
  * Levers, in order: start production rate, start rounds, pool size, yield, then costs. Report what each did. If compact never sees amber, that is the **edges-per-assembler** constant showing itself — record it as the first open decision for the human gate rather than tuning around it.
* **Two scenarios**: A, fresh start, 2.5 h sim at 4×; B, a snapshot of the compact bot at 3 h, 1.5 h at 4×, where the front bites. Scenario A tests shape and enclose-to-build; B tests pressure, ring order and retreat.
* `PROTOTYPE_TEST_PLAN.md`: five testers plus one `economy=0` control, same seed, §5's one-sentence rules and nothing else, observer notes verbatim with sim time, five open post-questions, the expected-timeline table from the bots, the config hash. Thresholds: ≥ 3 distinct shapes at 2.5 h (perimeter/area differs > 0.15 or aspect > 0.3), ≥ 1 unprompted remark about shape, ≥ 2 testers reorder the ring in B. Rework if shapes converge on one policy; kill if nobody talks about shape and nobody reorders.
* `TEST_RESULTS.md` template with a frontmatter `verdict:` field, tester table, per-scenario telemetry tables, criteria, verbatim quotes, the constants the sessions inform, decisions for the next phase.

**Gate A:** a human runs the sessions and writes `TEST_RESULTS.md` with `verdict: go`. The programme does not continue on `pending`.

## Phase 3 — Absorb gate A (3 days)

* Lock the constants `TEST_RESULTS.md` §8 informs — edges per assembler above all — as `[play]` sentences and `DECISIONS.md` lines. Re-run E1–E9 at the locked values; retag.
* Make the four pre-slice decisions if still open, from evidence: substation draw and the hour-one lesson (ammo or power); fall time (whether a rescue window exists against the refeed reset); the unfed rule as it ships; whether the Relight is survivable with banking or is the intended ending. Each is a `DECISIONS.md` line and a §5/§11/§16 sentence.
* Redraw §18's three example maps from the compact bot at the locked cadence.

**DoD:** no open constant remains that the slice would otherwise encode by accident.

## Phase 4 — Vertical slice: §11, minutes 0–60 (6 weeks) → Human gate B

The sim grows to tile level; the map view becomes the game's map view; the first hour is playable with a mouse as the doc writes it, ending with lights coming on in a block that was dark.

* **M1 Ground** — 32×32-tile blocks, 8-wide streets, lots, rubble tiles typed by district with the density gradient, the river as inert tiles; camera pan and zoom 0.5–3×; E toggles map ↔ world at the same block; the map view's block state is authoritative, the world view draws tiles.
* **M2 Flow** — Excavator (3×3, 0.5/s onto an adjacent belt), belts (7.5/s, items visible, corners), inserters (1/s), Depot as the global stock hand placement draws from, Mk1 Shot assembler (3 s, 2 steel + 1 Cu → 1 magazine), hand-mining and hand-crafting as §11 states. Measured rates equal doc rates.
* **M3 Defence** — turret (2×2, 50-round hopper, range 9, 5 rounds/s, fed by inserter), lamps and light radii, substation, poles, Generator on coal from the start patch, power as one number with the shed order, streetlights on when the substation powers. An empty hopper turns the map view's pip red from the same event.
* **M4 Threat** — rot as tile presence in dark blocks, blooms at the street edge facing the player, crawlers on a per-block flow field (lamp → turret → substation), the 40-arrival rule at tile level, never more than two blocks of travel. Shades only if residential rot reaches its threshold inside the hour; no hulks.
* **M5 Light** — a single-channel 768×768 light texture multiplied over the world layer; streets and lamp radii lit, lots unlit; rot cannot exist on a lit tile; burn-off sweeps light in over `20 + 60·d` seconds. Built last so the payoff lands on a working block.
* **M6 The hour** — a bot follows §11's minute list with telemetry on; every divergence from §11 and from the calibration timeline is a finding. Then a human plays.
* Fixed 20 ticks/s at tile level, 1 s block ticks derived; a 1 h sim at 4× must not drop the render loop. Map-view fixtures still pass; tile events drive the same block transitions.
* Telemetry adds per-minute magazines made and consumed, hopper levels, belt occupancy on the ammo loop, lamps lost, brownout seconds, coal, and the timestamp of the first completed burn-off.
* `SLICE_REPORT.md`: per-milestone, every `GAME-ASSUMPTION`, the M6 bot timeline against §11, performance, and — at the top — the §19 first-hour test scored from human play.

**Gate B:** a human scores the first-hour test (new unlock, current problem, memorable moment in each of 0–10, 10–30, 30–60; and whether the burn-off made them say something) and writes `verdict: proceed` in `SLICE_REPORT.md`. **STANDARDS rows:** A.7 — the hour played without the hand lamp, one sentence on whether the dark reads as the claim's price or as a missing flashlight (D-B5-1 reopens only on that sentence). The layout pass before it checks 4.3 (a key strip on the HUD), B.3 / B.6 (the kerb pip in the world view: segment state and an empty turret) and C.2 (the Depot reads from the viewport's far edge). **STANDARDS:** the `STANDARDS.md` rows for this phase are checked; no new gap was introduced without a row.

## Phase 5 — The factory, complete (6 weeks)

§12–§14 in full: resources (stone, steel, copper, coal, oil from the Refinery only), intermediates, every recipe in a generated `recipes.ts` that the doc's table is built from; the §13 machine list with footprints and nothing outside it; belts, undergrounds, splitters, three inserters, chests, the Depot (hand placement only, never a logistics network), trams (one per track, two stops, six inserters per stop, lines drawn on the map view); rubble as finite typed ore with visible depletion; power complete with the shed order and the dimming on the map view; slots as real lot geometry, with the doc stating whether an interior lot can hold more than one machine.

Experiments: **E10 chain throughput** (every chain at its §12 rate), **E11 coal depletion** (when the Turbine hall is needed), **E12 tram sufficiency** (one tram carries the outskirts' steel at 25 h).

**DoD:** the 5 h §18 territory is buildable by a bot from a blueprint file at doc rates; a human builds a two-assembler ammo line in the world view in under ten minutes unaided. **STANDARDS rows:** 1.3 / 1.4 belt drag with corners and the underground span shown; 1.10 undo / redo in the command layer; 1.11 copy / paste with a clipboard; 1.16 the hotbar as references to the recipe table with room for §13; 1.17 one binding table; 8.5 one string table; 2.3 splitter priority on its panel; 2.5 a filtering primitive; 2.6 the machine status enum (running / starved / blocked / no power / no ammo) on the panel; 2.7 rates on the machine panel; 2.9 alt-mode data per entity; 3.1 per-item production and consumption counters readable in game; 2.11 / 7.4 / A.9 trams and the driven truck. **STANDARDS:** the `STANDARDS.md` rows for this phase are checked; no new gap was introduced without a row.

## Phase 6 — Threat, complete (4 weeks)

Three enemies exactly as §7 and no fourth: crawlers (ammo throughput), shades (lamp coverage, untargetable off-light, substation disable, stacking), hulks (frontage, through the ring, the Cannon line). Rot at tile level with every §5 rule. The fall at the decided time with retake and machine loss. Cannons and shells as the second line with the interior-siting rule. Wells with the four-neighbour kill and the Relight surge flag. Cached flow fields.

Experiments: **E13 enemy purpose** (each loses to exactly the logistics fix the doc names and nothing else), **E9-full** (the Relight hold at tile level; the banked-stock claim confirmed or the doc changes), **E14 hulk niche** (if hulks touch under 5 % of edges outside wells, §24's "cut the Cannon line" goes to a human).

**DoD:** a 5 h compact run at tile level loses the blocks the block sim predicts within ±1; a human watches one bloom and says what it wanted. **STANDARDS rows:** 3.3 alerts that jump the camera; 3.4 an off-screen arrow to the block under attack; B.5 turret range on placement and select; B.7 enemy hover with HP and target. **STANDARDS:** the `STANDARDS.md` rows for this phase are checked; no new gap was introduced without a row.

## Phase 7 — Found tech (3 weeks)

Facilities restored by claim plus a delivery on a belt, unlocking their §8 list; some never craftable. Survivors joining on Held, specialists unlocking by toast, the HQ survivor panel as the entire tech screen. §17 placement bands and the validator rules (nothing behind a well, the Foundry at §11's range). No research menu — any unlock without a building or survivor is a stop-and-report.

Experiments: **E15 reachability** (10,000 seeds; every facility reachable in its §15 window; failure rate), **E16 survivor pull** (why a player claims toward a survivor with no resource there; if a bot needs a rule, the doc explains the player's reason).

**DoD:** three seeds reach the Turbine hall in window by bot; the survivor panel reads as a tech tree with faces to someone who hasn't seen the doc. **STANDARDS rows:** 3.5 the survivor panel answers what makes this, what uses this, who unlocked it. **STANDARDS:** the `STANDARDS.md` rows for this phase are checked; no new gap was introduced without a row.

## Phase 8 — Territory tools (3 weeks)

The §19 tedium audit's removal schedule, on time: Foreman kits on claim, the Line truck re-fronting from Depot stock, blueprints with ghosted delivery, one-action retreat of an interior block's ring to the new front, the ammo ring editor at tile level (the belt is the ring; the map view's list is a view of it, never a second model). Every repeated action in the audit has its tool by phase end or the audit line changes.

Experiment: **E17 actions per block** before and after each tool, from a bot that places like a human; the doc's hands-test paragraph retagged.

**DoD:** hour 3–5 played with and without tools by a human; the observer's placement counts match the doc or the doc changes. **STANDARDS rows:** 1.12 the blueprint library with folders, icons and strings; 1.7 mirror / flip; 1.14 area select for removal with the retreat and the ring editor; 1.9 the Line truck's order list as the ghost layer. **STANDARDS:** the `STANDARDS.md` rows for this phase are checked; no new gap was introduced without a row.

## Phase 9 — The city (4 weeks)

The §17 generator: districts by band, river, wells, scattered inert at the locked density, rubble typing and gradient, outskirts deposits, depth multiplier, facilities and survivors by the Phase 7 rules. The validator with every rule and its reject rate. Five presets as parameter sets, not code. A seed browser in `packages/tools`.

Experiments: **E18 seed fairness** (10,000 seeds through compact and spike; the spread of first-enclosure and Turbine-hall times stated in the doc as run variance), **E19 preset identity** (each preset's bot line differs from default by the thing the preset is for).

**DoD:** a human looks at ten seeds in the browser and says what's different about each before playing. **STANDARDS rows:** C.1 generated, overwritable names for blocks and districts, used by the toasts (build-with E19); 7.2 charting; 7.3 pins with an off-screen arrow (add-later). **STANDARDS:** the `STANDARDS.md` rows for this phase are checked; no new gap was introduced without a row.

## Phase 10 — Progression and endgame (4 weeks)

The §15 arc with everything present. The Relight as decided at gate A's absorption: transformer yards on interior blocks, the ten-minute hold, banking as intended play, the last district lit, and no end screen that stops play. Post-Relight: the §17 optimisation targets in the HUD, nothing more. Difficulty: one slider on rot caps and a "quiet city" that removes blooms; both are generator parameters and there is no other difficulty system.

Experiments: **E20 full run** (100 seeds to the Relight; §15's hour ranges retagged), **E21 quiet city** (does a factory get built without the threat? If not, the front was the only reason to build, and that is a finding for a human).

**DoD:** a bot wins on 90 % of validated seeds in window; a human has seen the last district light and said something. **STANDARDS rows:** B.10 the difficulty readout (add-later). **STANDARDS:** the `STANDARDS.md` rows for this phase are checked; no new gap was introduced without a row.

## Phase 11 — Performance and scale (4 weeks, parallel with 12)

Targets on a 2019 laptop at 1080p: 313 held blocks, 60 live edges, 4,000 belt segments with items, 200 machines, 40-crawler blooms on 10 edges, 60 fps world view, 16× map view without dropping the render loop, 25 h save under 20 MB loading under 3 s. Typed arrays for belts and items, struct-of-arrays entities, pooled crawlers, no per-tick allocation. A 40-hour bot save as the benchmark.

Experiment: **E22 megabase** (a bot builds to 400 blocks; where frame time, tick time and save size break).

**Engine gate:** if the targets cannot be met after the allocation work, report the numbers; a human decides on the C#/Godot port. **STANDARDS rows:** 9.1 is the phase; D-SA-4's check — a replay of a 5 h command log reproduces the state byte for byte after the allocation work. **STANDARDS:** the `STANDARDS.md` rows for this phase are checked; no new gap was introduced without a row.

## Phase 12 — Interface, onboarding, art, sound (8 weeks, parallel with 11)

* The map view finished at final scale; the world-view HUD; §11 as the entire onboarding, toasts tuned until **E23 cold player** passes (4 of 5 people who haven't seen the doc reach the first enclosure without asking a question).
* Accessibility: colour-blind palettes with shape-coded states, remappable keys, UI scale, and no input anywhere that needs reflexes.
* Save/load from the sim's JSON with versioning, autosave hourly, saves inspectable in tools.
* Art direction is a human decision; implement it under §4's constraint (legible at full zoom-out). Districts, machines with idle/working, belts with items, the ring, the four sprites (three enemies and rot), trams, landmark facilities. The light map as the finished visual: streets, lamp radii, burn-off, disable flicker, interior glow — this is where the game's identity lives.
* Audio: ambient by district, bloom cues by enemy, the turret line as a rhythm the player learns to hear stop, burn-off, lights on. No music that competes with the turret rhythm.
* No mechanic arrives through art or UI; a sprite implying a rule the sim lacks is a bug.

**DoD:** the first-hour test passes 4/5 cold; a full zoom-out screenshot passes §4 with five people; the burn-off makes one of them say something. **STANDARDS rows:** (add-later) 3.1 / 3.2 production and power graphs; 3.8 the alerts panel with per-type mute; 4.2 hints that stop repeating, with a toggle; 4.3 the place that teaches the QoL; 5.1 idle / working animation from the status enum; 5.3 / A.6 feedback on every action; 5.5 reactive music and SFX; 6.2 / C.7 decor and district silhouettes; 8.1 / 9.3 autosave ≤ 10 min, three rotating versioned slots, off the frame; 8.3 / 8.4 settings with UI scale, rebinding and colour-blind shape codes; 2.9 alt-mode art; A.1 the pockets as a grid with the Shift / Ctrl grammar. **STANDARDS:** the `STANDARDS.md` rows for this phase are checked; no new gap was introduced without a row.

## Phase 13 — Balance and Steam (6 weeks, then continuous)

* **E20 on 1,000 seeds per change**; a sensitivity row per constant in §12–§15; every balance change is a changelog line with a run. Bots for every §24 degenerate strategy (turtle, turret-spam, never-enclose, quiet-block greed, river-hug), each with a doc sentence on why it loses or why it's allowed.
* Three human rounds on full builds, `TEST_RESULTS_N.md` each, the §19 tests scored per round, the pressure test measured as minutes building vs waiting vs fighting (waiting never over a minute). §27's fun-confidence score rewritten from evidence.
* Shell chosen on Phase 11's memory and startup numbers; Steamworks bridge; Windows first, Linux via Proton, macOS if clean. Achievements only for things the doc already measures. A demo of §11 through the first enclosure, seed locked, as the shape test with strangers. Store page from §1–§3, §20, §21, hook first, prior art named honestly. Opt-in telemetry on the prototype's schema so bot distributions can be checked against real play.

**DoD:** three rounds passed; a stranger installs from a key and reaches the first enclosure; the demo's cold-player score matches the internal one. **STANDARDS rows:** (add-later) 8.2 cloud saves; 8.6 achievements for measured things; 8.10 E22's numbers as the store page's minimum spec; the Steam Deck pass under D-SA-1. **STANDARDS:** the `STANDARDS.md` rows for this phase are checked; no new gap was introduced without a row.

## Phase 14 — Launch (ongoing)

Next Fest with the demo; telemetry read against E20's distributions; a balance pass where players diverge from bots. Release with the slider, the presets, the in-game seed browser, the post-Relight hooks, and the doc in the repo with its changelog. After launch, one rule: **no fourth system**. Content is seeds, presets and facilities; a proposed mechanic goes through §23's rejected-alternatives test in public. **STANDARDS:** a post-launch request is scored against `STANDARDS.md` before it is built; a converged gap gets a row, a diverged one goes under `## Considered`.

---

## Cross-cutting

* **Telemetry** keeps one schema from the prototype to launch; the analyser reads all of it.
* **Docs sync**: CI fails if `recipes.ts`, `districts.ts` or `enemies.ts` differ from the doc's tables, which are generated from them.
* **DECISIONS.md**: one line per human decision — phase, evidence file, the doc sentence it produced.
* **The two human gates (A and B) and the engine gate are the only places the programme waits.** Everything else is a report and a next phase.

## First session

Read the constitution. Create `PROGRAMME_STATE.md` if it doesn't exist. Do Phase 0. Write `PHASE_0_REPORT.md`. Stop.

---

*Standards audit, 2026-09-04 (`STANDARDS_REPORT.md`): the DoD rows marked **STANDARDS rows** and the two standing rows in rule 9 were added by the audit. Three sentences in this file disagree with decisions or the doc and are reported, not edited: Phase 5's, M3's and E2's "shed order" (superseded by D-B3-4's proportional brownout); Phase 5's "three inserters" (§13 lists one); Phase 12's "autosave hourly" (the baseline is ≤ 10 minutes, row 8.1). The human settles each when the phase opens.*
