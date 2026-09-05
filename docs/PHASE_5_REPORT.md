# Phase 5 report — the factory, complete

**Status: opened 2026-09-05, nothing built.** This file is the phase's opening, written by `PROGRESS.md` T10. It reads Phase 5's line in `PHASES.md` against `RELIGHT-design.md` §12–§14 and the decided rows of `DECISIONS.md`, lists every number and rule the phase carries that neither of them does, says what each one conflicts with, and **stops there** — constitution rule 12. It also collects what the phase inherits (Gate B's three conditions, the deferred work that was parked "→ Phase 5", the fourteen STANDARDS rows), answers the one question `PROGRESS.md` addressed to this opening by name, and proposes the milestone order. The build starts when the human has resolved §2 and §4; `PROGRESS.md` T11 is that task and every Phase 5 build task is blocked on it.

Written on branch `phase-4` (PR #5) because it is doc-only and belongs with the phase it closes. The first Phase 5 build task opens `phase-5`, stacked on `phase-4` as PR #6.

---

## 1. What Phase 5 is

`PHASES.md`, verbatim:

> §12–§14 in full: resources (stone, steel, copper, coal, oil from the Refinery only), intermediates, every recipe in a generated `recipes.ts` that the doc's table is built from; the §13 machine list with footprints and nothing outside it; belts, undergrounds, splitters, one inserter (§13) — [open: D-INSERTERS-1], chests, the Depot (hand placement only, never a logistics network), trams (one per track, two stops, six inserters per stop, lines drawn on the map view); rubble as finite typed ore with visible depletion; power complete with proportional brownout (D-B3-4) and the dimming on the map view; slots as real lot geometry, with the doc stating whether an interior lot can hold more than one machine.
>
> Experiments: **E10 chain throughput** (every chain at its §12 rate), **E11 coal depletion** (when the Turbine hall is needed), **E12 tram sufficiency** (one tram carries the outskirts' steel at 25 h).
>
> **DoD:** the 5 h §18 territory is buildable by a bot from a blueprint file at doc rates; a human builds a two-assembler ammo line in the world view in under ten minutes unaided.

Six weeks in the constitution's estimate. It is the first phase since Phase 0 that adds no new system: §26 stays at three systems and complexity 5/10, because every machine it builds serves the front, the found tech or automated combat that already exist.

---

## 2. The rule-12 read

Constitution rule 12: *"If a task prompt contains a number, a rate, a size, a cost, or a rule that is not already in the design doc or in a decided row of `DECISIONS.md`, the assistant's first and only action is to write a list of every such number or rule, say where it conflicts, and stop."* Phase 5's line is a task prompt. This is that list.

### 2a. Already true, or already in the doc — nothing to resolve

| The phase says | Where the doc or the code already says it |
|---|---|
| "every recipe in a generated `recipes.ts` that the doc's table is built from" | **Already built.** `packages/sim/src/recipes.ts` holds all nine §12 recipes and `npm run docsync` generates §12's table from it; `docsync:check` is a cheap check. What is *not* built is production: `flow.ts` picks `Shot magazine` alone (`const SHOT = RECIPES.find(…)`), so the other eight are data, not something a machine makes. Phase 5's work is the eight, not the file. |
| "one inserter (§13)" | **Decided** — D-INSERTERS-1, Daniel, 2026-09-04: one, no tier ladder. Its `doc sentence` column said the sentence lands "§13 when Phase 5 opens", so this task wrote it (§13, with a changelog line). |
| "proportional brownout (D-B3-4)" | §14 states the rule outright and §5, §13, §23 and §24 risk 7 follow it. The row itself is still `recommended` (no name, no date), which is a provenance gap, not a rule-12 one — the doc carries the rule. |
| "the Depot (hand placement only, never a logistics network)" | §14 D5, exactly. |
| "chests" | §13: 400 items. `CHEST_ITEMS` grows in this phase (`DEFERRED.md`). |
| "rubble as finite typed ore" | §12: a rubble tile holds 300 units, a block holds 250–350 of them (75–105k); mined-out tiles become plain ground. |
| "one tram per track, two stops" | §14, exactly. |
| DoD "a human builds a two-assembler ammo line … under ten minutes unaided" | §19's hands test, and it is `PROGRESS.md` **T6 returning**: T6 was waived at Gate B, so this DoD is the first time it is actually run. A human task, listed as one. |

### 2b. Corrected in `PHASES.md` by this task — the doc plainly settles them

Precedent: the guardrails task of 2026-09-04 corrected three sentences in `PHASES.md` the same way (including Phase 5's own "three inserters" → "one inserter (§13)"), and recorded each in that file's footnote. These are corrections *to the doc's own words*, not decisions.

| The phase said | What §12–§14 say | Now reads |
|---|---|---|
| "six inserters per stop" | §13 Tram stop: "loads/unloads via **up to 6** inserters (one per tile of its long sides)"; §14 repeats "up to 6". A fixed six is a different rule and it fixes a stop's throughput at 6 items/s, which is exactly what **E12** is meant to measure. | "up to six inserters a stop (§13)" |
| "oil from the Refinery only" | §12: crude comes from the oil field via the **Pumpjack** (0.5/s, §13) as a Crude drum item; the **Refinery** turns crude into Fuel and Polymer. §13's Pumpjack unlock is "Refinery", so the phrase is true of the *unlock* and false of the *source*. | "crude only once the Refinery is found (§13 Pumpjack unlock), and the Refinery its only consumer (§12)" |

### 2c. Raised as rows and **not built** — the doc does not settle them

Six. Each has a new `DECISIONS.md` row with a recommendation and no decision (rule 7).

| Row | The phase carries | What it conflicts with | Recommendation (not a decision) |
|---|---|---|---|
| **D-P5-1** | "**the dimming on the map view**" as part of "power complete" | The doc has no dimming anywhere. §14 says the opposite in two places: *"Lights do not dim: a Lamp, Floodlight or streetlight is lit at any throttle above zero and dark only on a dead grid"*, and the brownout's whole legibility argument is *"every machine visibly slows, and the HUD shows one bar, demand over supply and the percentage everything runs at"*. §4's map-view vocabulary has ten entries and no throttle state; §24 risk 4's brownout icon sits on the substation and is about a **fall**, not a throttle. | The map view gets the *number*, not a dim: the HUD's one bar already carries `Power 2.1 / 3.0 MW`, and a block whose machines are throttled reads it on the machine panel (STANDARDS 2.6/2.7, this phase). If the human wants a map-view read, make it a discrete mark like the supply pip, not a brightness — brightness on that view already means rot density and Held/Interior state. |
| **D-P5-2** | "**the §13 machine list with footprints and nothing outside it**" | §13's list is the whole game's machines. Six of its rows unlock in later phases: **Cannon** (Arsenal — Phase 6's second line), **Barricade** (Concrete crew — Phase 6's hulks), **Mixer** (Concrete crew — found tech, Phase 7), **Pumpjack** (Refinery found — Phase 7), the six **facilities** (Phase 7), **Line truck garage** (Foreman — Phase 8). Building "nothing outside it" in Phase 5 either pulls three phases forward or means "no machine that is not in §13", which is a different sentence. The Concrete recipe is the sharpest case: §12 lists it as one of the eight intermediates Phase 5 owes, and its only machine is found tech. | Read it as the second sentence: Phase 5 builds every §13 row whose unlock is `start` or belongs to a Phase 5 system (belts, undergrounds, splitters, inserter, chests, Depot, poles, Track, Tram stop, Tram, Truck), and **no machine outside §13 is invented**; each remaining row ships with the phase that unlocks it. Concrete's recipe is generated in `recipes.ts` (it already is) and becomes makeable when the Mixer arrives with the Concrete crew. |
| **D-P5-3** | "§12–§14 **in full**" reaches §14's opening sentence: "**Standard two-lane belts**" | `flow.ts` says belts are one lane, and has since M2. `STANDARDS.md` row 2.1 records it as a doc-vs-build disagreement and routes it here in as many words: *"the human settles §14 vs `flow.ts` when Phase 5 opens"*. It is also the one row where the baseline **diverges** — Factorio alone of seven has two lanes. | One lane, and §14 changes with a changelog line. Two lanes exist to let one belt carry two inputs to one machine; §13's recipes take at most two inputs (Board: 3 wire + 1 steel) and the doc already solves that with a second belt and a second inserter, on 8-wide streets with room for two belts. Two lanes double the belt state, every splitter rule and every side-load rule for one recipe's convenience. But it is a doc sentence, so it is the human's. |
| **D-P5-4** | DoD: "buildable by a bot from **a blueprint file**", and STANDARDS **1.11** copy/paste is a Phase 5 build-with row | Three documents disagree. §14 and §19 say "**Blueprints and copy-paste from minute one**". `PHASES.md` puts blueprints in **Phase 8** ("blueprints with ghosted delivery") and Phase 7's dealbreaker-1 row makes them a **found unlock** ("the survivor who brings them", GA-EF-3 — the survivor panel already shows the "not yet found" row). `STANDARDS.md` 1.11 records the same and says "reported, not resolved". | Split the word. The **bot's blueprint file** is a harness input (a JSON the E10/E12 runner reads), needs no in-game item, and meets the DoD on its own; build it in Phase 5. The **in-game blueprint** stays a found unlock (Phase 7) built in Phase 8, and §14 / §19's "from minute one" is the sentence that changes, because the survivor-gate is the thing three later phases are built on. If instead "minute one" is the design, Phase 7's dealbreaker row and Phase 8's line both have to change. |
| **D-P5-5** | STANDARDS **2.5**, a Phase 5 build-with row: "a filtering primitive somewhere on the line" | With D-INSERTERS-1 decided at one inserter, the filter can only live on that inserter — but **§13 has no filter sentence at all**, and the row's own note ("which of the three inserters filters is the human's") was written when there were three. The check is one minute: "an item can be pulled off a mixed belt in a minute". | The filter goes on the one Inserter as a per-inserter setting with no cost and no new machine, so §13 gains a clause rather than a row. What it must not become is a second inserter tier, which D-INSERTERS-1 has already decided against. |
| **D-P5-6** | "trams (… **lines drawn on the map view**)" | §4's map-view table draws **Trams** as "moving rectangles on the street lines" and **Belts** as "thin lines with moving dashes"; there is no tram *line* — the route between a tram's two stops — in the vocabulary. STANDARDS 2.11 is what asks for it ("long-haul transport with lines drawn on the map", converged 6/7). | Draw the route as a thin line in the belt's own idiom, and add the row to §4's table with a changelog line when it is built. It is a view of a model the doc already has (one tram, two stops), so it invents nothing — but §4's table is called a "fixed vocabulary, learned in the first 30 minutes", so a new row in it is a doc change, not a rendering detail. |

### 2d. One finding the read turned up that is not Phase 5's own

**`DECISIONS.md` D-P4-12 says two constants are missing that the design doc in fact carries.** The row reads: *"Two constants are missing and neither is in the design doc or in a decided row: what a rail-yard lot's coal is, and how much of it there is."* §12's raws table answers both:

> | Coal | **rail-yard rubble (direct, ~30k per block)** · coal seam (outskirts) | 4 MJ each; Shell recipe |

So the doc already says a rail yard's rubble *is* coal, dug directly, at **~30k a block** — which is option (a) of D-P4-12 (a fourth rubble kind on rail-yard lots) with the doc's own quantity, and it is 43× the row's recommended ~700. The row's premise is corrected in place; the decision itself is untouched and still the human's, because taking §12's ~30k is a choice with a consequence (it makes west's claim an answer to coal for the rest of the game, not a top-up for hour two), and because §12's number has no `[sim: …]` tag behind it.

### The stop

**No Phase 5 code is written on this task and none should be written until §2c and §4 are resolved.** Every item in §2c would otherwise be built from a prompt rather than from a doc sentence or a decided row, which is the thing rule 12 exists to stop. This is the same reason T9 wrote no code, and it is the second time in two tasks that the programme's own paperwork, not the sim, is what the work is waiting on.

---

## 3. What Phase 5 inherits

### 3a. Gate B's three conditions (`docs/GATE_B.md`, verdict `proceed — with conditions`)

Gate B passed *with* these, so they are resolved inside Phase 5, not before it. All three are `open (gate condition)`; none is a run.

| | What the played hour asked for | What it needs before it can be built |
|---|---|---|
| **D-GB-2** | *"Knowing what to do next… Maybe we need a small quest system?"* | A human's word on form. Constitution rule 8 forbids tutorial screens, so the rule-8-legal answers go first: a next-objective line in the HUD carrying the sim's own truthful next constraint, the claim tooltip made louder, §11's beats as toasts. A quest system is a rule-8 change and needs the rule changed explicitly, with a changelog line. §25 open question 19. |
| **D-GB-3** | *"I'd want the range increased or have no range."* | A number. §4 fixes the rifle at the turret's 9 tiles with no advantage; the mechanism that protects is that standing on a segment must not beat building the thing that holds it. `E-rifle-tile-rescue` measures the consequence directly (today: 3 of 7 falls saved, mean 38.7 HP, busiest rescue 96 of 200 rounds). A tune, measured. §25 open question 20. |
| **D-GB-4** | *"More enemies when the base is being attacked. More roaming enemies as well…"* | A §7 density number, and no rule change: §19 caps player shooting at 10 % of an hour and danger at 5 %, and the measured hour is **0.50 / 0.69 / 0.06 %** and **0.00 %** `[sim: E-rifle-tile]` — twenty- to two-hundred-fold headroom inside the doc's own limits. §19's own line ("the turret rules are wrong, not the rifle") is what decides it if spending the headroom is not enough. §25 open question 21. |

### 3b. D-GB-1's hybrid — decided, not built

Decided at Gate B (Daniel, 2026-09-05, option (c)). The claim still burns off its block, so §5, §11, §18 and every measured number stand; what is added is that **powering and lighting the next area becomes a physical expedition the engineer walks** (poles and lamps carried to the next area's box) and **an area lighting up becomes a threat trigger** beside the wake bloom, sometimes silent. §10 records it as decided-and-not-built; `DECISIONS.md` and §10 both name the sentences it moves when it lands (§5's claim loop, §10's triggers, §11's hour, §19's asks). **Which phase builds it is the human's** — it is a loop change with a threat trigger in it, so Phase 6 is the natural home; putting it in Phase 5 makes this phase the one that changes the loop while it is also completing the factory. Decision 1 below.

### 3c. Deferred work parked "→ Phase 5", unblocked and ready

From `DEFERRED.md`'s Gate B and Absorb Gate B re-reads:

- **Every front edge physical** (D-P4-9's other half, D-B1-4's count, GA-B6-4, D-P4-4's steel cost). Restore `carryTurrets` / `turretSpots` / `idleTurret` / `hqTurrets` from commit `6694b71`; `hookSyncEdges` must first learn to keep feeding an edge that has a turret on it. **The first unblocked item on the list.**
- **West's coal made real** (D-P4-10 (a)'s other half) — behind **D-P4-12**, whose premise §2d corrects.
- **The second Shot line / the Mk2 purchase from the idle steel** (D-P4-11) — a human's row still.
- **The calibration's pip bands** (16–31 min, the lattice's, against ≈ 3 / ≈ 6 min at tile level) — recalibrate at tile level or retire them with the lattice appendix. The human's call, now due.
- **Machines, recipes and the chest**: a machine as a crafted item (D-B2-1), the Substation's real recipe (D-B3-1 — §13.14's 20 frames + 20 wire + 10 boards, which only this phase can pay), belt side-loading and splitters, the copper arm's 3:1 over-supply, hand-collecting from a chest, `CHEST_ITEMS`, the Foundry's own machines, stone's rubble sink (concrete), the other survivor groups' unlocks, pole/Big pole "supplies 7×7 / 3×3", the barricade, crawlers and the player's own machines, the Rifle Mk2, the truck driven.

### 3d. Observations the gate handed over — measurements a bot cannot take

Not debts and not blockers; each is closed by any later played session that reaches minute 45, with the telemetry panel open. **The first shade** (§11 and E3 say minute 32–47; six bot hours print "never"; 48 tile rescue runs kill nothing with a shade) — `E-hour`'s one open finding. **A player's walking share**, **the first pip**, **shades on lit tiles in play**, **a tile-level brownout soak**, **copper for repairs**, **the repair-by-E and `?handlamp=1` Playwright check**, **light on the map view**, **the burn-off question** (asked at the gate and displaced by D-GB-1 rather than answered), and **§19's unrecorded 10–30 and 30–60 windows**.

### 3e. Due at the Phase 5 **gate** (phase end, not now)

Delete the lattice and everything kept for it: `--map lattice` / `?map=lattice`, the lattice fixtures and results, the `?flow=0` free camera, and the block-only harness start ring (`sim.ts syncEdges`, GA-B1-15) — "when every state has tiles and `startTurrets` is the only source". Also at the gate: which sim rates the economy (D-P4-5, D-B2-2 option (c)).

---

## 4. The rows Phase 5 cannot start without

Beyond §2c's six. All are `provisional` or `recommended` — none is decided, and rule 7 keeps every one of them the human's.

| Row | What it blocks | Status |
|---|---|---|
| **C3 / D-B2-2** | "slots as real lot geometry" — the phase's last clause. `slotsOf` = floor(area / 600) gives one slot on every HQ-neighbour face while the free footprint holds 4–9 §11-scale lines. (a) keep the economic cap; (b) derive slots from free footprint; (c) drop slots when tile lines replace the block stand-in. §25 item 17 is the GAME-ASSUMPTION it would replace. | `provisional` / `recommended` |
| **D-P4-12** | West's coal, and every rail-yard lot after it. §2d corrects its premise: §12 already says coal, ~30k a block. | `recommended` |
| **D-P4-2** | Units per rubble tile — §12's 300 against the sim's 3,840-per-block pool and M1's ≈ 11–15 a tile. "Phase 5's rubble model (E11/E13) sets units per tile with the deposit sizes" is the row's own text. | `provisional` |
| **D-P4-3** | Deposits on the outskirts (iron ~2M, coal seam ~1.5M) — the placeholder is inert; E11 and E12 both need them real. | `provisional` |
| **C5** | The HQ steel patch size, "routed to Phase 5's rubble model as a named placeholder". | `recommended` |
| **D-B3-1** | The craftable Substation's price — §13.14's 20 frames + 20 wire + 10 boards against M3's 50 steel + 25 Cu stand-in. Phase 5 is the phase that can pay the real recipe. | `recommended` |
| **D-B2-1** | Is a machine a crafted item with a workbench time, or paid in rubble at placement (as M2 built it)? | `recommended` |
| **D-P4-5** | The two assembler stand-ins — its recommendation is now unconditioned ("make every assembler physical after Gate B"), and Phase 5 is where the block-level button goes. | `provisional` |
| **D-P4-11** | The second Shot line / the Mk2 purchase from the idle steel. | `recommended` |
| **D-SA-2** | Localisation: (a) a **string table in Phase 5**, translation bought in Phase 13. STANDARDS 8.5 is a Phase 5 row because of it. | `recommended` |

---

## 5. The STANDARDS rows for this phase

Fourteen build-with rows in `PHASES.md`, plus 2.1 which the audit routed to this opening (→ D-P5-3) and 2.2, which is already the doc's. Each is a **one-minute human check** at the phase gate (constitution rule 9); the build-with note is why it cannot be retrofitted.

| Row | What it asks | Where it lands |
|---|---|---|
| 1.3 | Drag / line placement of belts, corners handled, rotation following the drag | M2 — "the placement command takes a path, not a tile". 7/7 baseline; a day-one press complaint where it is missing. |
| 1.4 | Undergrounds show their span while placing | M3, with §14's span 4 |
| 1.10 | **Undo / redo** | M2 — the command layer: every placement and removal a reversible command. The record/replay log already exists (`hour.ts`, `session.ts`). Dealbreaker 3. |
| 1.11 | Copy / paste with a visible clipboard | M6, and it is **D-P5-4**'s question |
| 1.16 | Hotbar as references with room for §13's list | M2 — built today as 12 fixed keys; ~20 machines will not fit one row |
| 1.17 | Every key through one binding table | M2 (the table; the rebinding screen is Phase 12) |
| 8.5 | One string table | M2 (D-SA-2) |
| 2.3 | Splitter with output priority | M3 |
| 2.5 | A filtering primitive | M3, and it is **D-P5-5**'s question |
| 2.6 | Every machine states **why** it is not running (starved / blocked / no power / no ammo) | M4 — a status enum the sim owns |
| 2.7 | Machine panel shows rates | M4 |
| 2.9 | Alt-mode data per entity | M4 |
| 3.1 | Per-item production / consumption counters readable in game | M4 — the sim already counts; §4 currently puts "the line's rates" behind a debug key, which the audit calls below baseline |
| 2.11 / 7.4 / A.9 | Trams and the driven truck | M5 |
| 2.1 | Two-lane belts | **D-P5-3** — settled at this opening, by the human |

---

## 6. The question this opening owed: §11's enclosure and the Electricians

`PROGRESS.md` asked: *"§11's enclosure and the Electricians are now outside the hour. `HOUR_END.held` is 3, so the HQ's white border and the Electricians walking out of civic north are beats a 3,600 s run never reaches. Phase 5's opening should say whether that is the shape of hour one or a hole in it."*

**It is the shape, and §11 already says so.** The 30–60 paragraph reads: *"north claimed at minute 65 — past the hour on purpose, so hour one is two claims and ends with three blocks Held (D-P4-10, decided 2026-09-05)"*, and both beats are written behind it: *"**When it holds**, the HQ polygon gets its white border and the Electricians walk into the Depot."* Both are gated on north, north is at 65, and north is a **coal** problem, not an ammo one — the Generators run dry at 67:51–68:01. Nothing is missing from hour one; the beats belong to the claim that pays for them.

Two things follow, and neither is a hole:

1. **§11's heading now spans more than its window.** The paragraph is titled "30–60 min" and its last third describes minute 65–75. A reader scoring §19's first-hour test against it will look for a white border that the hour cannot produce — which is exactly what happened at Gate B, where the 30–60 window went unrecorded. Recommend §11 either splits the 65-minute beats under their own heading or the hour is re-scoped.
2. **The 75-minute variant is live and already measured.** `E-hour-north` runs north at 65:00: Held at 65:29–65:34 and **standing to 75:00 on every seed** `[sim: B-M6-hour-north]`. `DEFERRED.md` parked it exactly here: *"if the hour is ever re-scoped to 75 minutes, north's claim becomes a scored check and wants its own DoD line."* Re-scoping would make the enclosure, the Electricians and the burn-off payoff scored beats instead of promises — and it is the cheapest way to answer three of §3d's observations at once. **The human's call; decision 2 below.**

---

## 7. The pre-placed-tag finding, and the check this phase adds

`DEFERRED.md` parked this at T9 and addressed it to this opening: two `[play: Gate B]` tags were written into `RELIGHT-design.md` **before Gate B was played** (commits `27cc9b0`, `906044c`); one of them was never earned and has been removed. *"A tag naming a gate that has not happened cannot be checked by `docsync`, and nothing in the repo would have caught either of them."*

**The check Phase 5 adds** (M1, with the recipes work, because it is the same `docsync` pass): `docsync:check` learns one rule — **every `[play: <gate>]` tag in `RELIGHT-design.md` must name a gate whose evidence file exists and carries a verdict**. `docs/GATE_B.md` exists with `verdict:`; a tag naming a gate with no such file is a red build. It costs a dozen lines in `packages/tools/src/docsync.ts` and it is the only class of provenance error the repo currently cannot see — `[sim: …]` tags are checked against the run names the harness prints, and numbers are checked against `packages/sim`, but a play tag is checked by nobody. This is the one piece of Phase 5 work that has no rule-12 question in front of it, because it invents no game rule.

---

## 8. The milestone shape (proposed — not started)

Six milestones, in the order the dependencies force. Every one is blocked on `PROGRESS.md` T11. The order is proposed, not decided: if the human's answers to §2c change it, the rows in `PROGRESS.md` change with them.

| | Milestone | What it builds | Waits on |
|---|---|---|---|
| **M1** | **Recipes and rubble** | The eight non-Shot recipes made by real machines; rubble as finite typed ore with visible depletion at doc units; the outskirts deposits made real; the HQ patches sized. The `[play: <gate>]` check in `docsync` (§7). | D-P4-2, D-P4-3, D-P4-12, C5 |
| **M2** | **Placement, and the command layer** | Every Phase 5 §13 machine placeable with its footprint; **undo / redo** as the command layer (1.10); drag placement taking a path (1.3); the hotbar as references (1.16); one binding table (1.17); one string table (8.5). | D-P5-2, D-B2-1, D-SA-2 |
| **M3** | **Belts complete** | Undergrounds with the span shown (1.4), splitters with priority (2.3), the filter (2.5), side-loading, chests, the Depot's item list. | D-P5-3 (lanes), D-P5-5 (the filter) |
| **M4** | **Power and the panels** | Power complete on the tile layer; the machine status enum (2.6), rates (2.7), alt-mode (2.9), per-item counters (3.1); the brownout's map-view read. **D-GB-2's next-objective line lands here** — it is the same HUD work and the same "the sim's own truthful state" rule. | D-P5-1, D-GB-2's form |
| **M5** | **Trams and the truck** | Track, stops, trams, the driven truck (2.11 / 7.4 / A.9), the route on the map view. | D-P5-6 |
| **M6** | **Slots, the blueprint file, and the DoD** | Slots as real lot geometry; the bot's blueprint file; copy/paste (1.11); **E10, E11, E12**; then the two DoD runs. | C3 / D-B2-2, D-P5-4 |

**D-GB-3 (rifle range)** is a constant and an `E-rifle` re-run, not a milestone — it lands wherever the human writes the number, and the re-run is one command. **D-GB-4 (enemy density)** is a §7 number and belongs with Phase 6's threat work unless the human wants it sooner; §19's headroom means it changes no rule either way.

---

## 9. The experiments

**Renamed by D-P5-7 at T11, 2026-09-05**: `E-chain`, `E-coal`, `E-tram`, the tile-era convention. The name-collision note below is what the row was written from; `E10` stays the retired bloom-cadence run cited in §25 item 1, C8, D-P3-5 and D-P3-11.

| | What it measures | Note |
|---|---|---|
| **`E-chain`** chain throughput (was "E10", renamed by D-P5-7) | Every chain at its §12 rate: wire, frame, board, concrete, shell, fuel, polymer, and the Shot line already measured. The check is that a chain built at doc rates reaches the doc's number. | **Name collision.** §25 item 1, C8, D-P3-5 and D-P3-11 all cite `E10-bloom-cadence`, the retired Python bloom experiment. A `[sim: E10-…]` tag would be ambiguous in the one section that already carries one. Recommend the tile-era naming the harness has used since M6 — **`E-chain`, `E-coal`, `E-tram`** — and `PHASES.md` corrected to match. Row **D-P5-7**; nothing is tagged `E10` until it is settled. |
| **`E-coal`** coal depletion (was "E11") | When the Turbine hall is needed. §25 item 9 is the open question it closes: *"there is no rubble-depletion model, so 'coal runs out for the second player in three' (§15) is unsupported; it needs excavator throughput against per-block rubble stock."* | Needs D-P4-2 and D-P4-12 first — it is a run against numbers that do not exist yet. |
| **`E-tram`** tram sufficiency (was "E12") | One tram carries the outskirts' steel at 25 h. | The 25-hour run is the block sim's (E9-hold's shape); the tram is a tile object. Whether E12 runs at block or tile scale is a harness question for M6, not a doc one. |

---

## 10. Definition of done

> the 5 h §18 territory is buildable by a bot from a blueprint file at doc rates; a human builds a two-assembler ammo line in the world view in under ten minutes unaided.

Plus the standing phase-end requirements (constitution rule 9): the cheap checks green at every milestone, the **full verification pass** green at the phase end, a five-minute smoke test a human ran, this report completed (built / assumed / deferred / measured / three decisions), `PROGRAMME_STATE.md` updated, `DEFERRED.md` re-read with every item given a phase or deleted with a reason, §26 recounted, and **the fifteen STANDARDS rows of §5 checked, one minute each, by a human, with no new gap introduced without a row**.

What has to be true before the DoD can even be attempted: §18's 5 h territory is 52 blocks with 38 interior — the bot must place real machines on real lots, so **C3 / D-B2-2 gates the whole DoD**, and the blueprint file gates it twice (D-P5-4). The human half is `PROGRESS.md` T6 returning from its waiver.

---

## 11. Untagged recount and §26

**33 → 33.** This opening adds no number to `RELIGHT-design.md`. The one design-doc edit is §13's D-INSERTERS-1 sentence, which states a decided row's answer and carries no new constant. §26 unchanged: three systems (belts/inserters/machines, the front, found tech), complexity **5/10** — Phase 5 completes the first of the three and adds no fourth, which is the point of constitution rule 2's recount. D-GB-1's expedition is the next thing that raises it, on the milestone that builds it.

---

## 12. The decisions (T11, 2026-09-05) — the stop is lifted

Section 2c stopped the phase under constitution rule 12: seven rules in Phase 5's line were in neither the design doc nor a decided row, so they were listed and not built. **`PROGRESS.md` T11 closed all seven and seven more in one message** — the human's *"Go with your recommendation for all"* — and every row was taken on its own recommendation, with the rule-11 triple written into each: `decided | Daniel | 2026-09-05 | the chat message of 2026-09-05 ("Go with your recommendation for all"), taking this row's recommendation`.

**The stop is lifted. `DECISIONS.md` now holds no `open` row** — only D-GB-2, D-GB-3 and D-GB-4 remain `open (gate condition)`, which is exactly what Gate B's verdict made them, to be resolved inside this phase rather than before it.

**The seven rule-12 rows (§2c):**

| row | taken | the doc sentence that moved |
|---|---|---|
| D-P5-1 | (a) **no map-view dim** — the HUD bar (demand ÷ supply, the percentage) and the machine panel carry the read | none. §14's *"lights do not dim"* stands, and §4's map-view vocabulary stays at ten entries. Builds at M4 with STANDARDS 2.6 / 2.7 |
| D-P5-2 | (a) every §13 row whose unlock is `start` or belongs to a Phase 5 system; no machine outside §13; Cannon, Barricade, Mixer, Pumpjack, the six facilities and the Line truck garage ship with their own phases | `PHASES.md` Phase 5. §13's unlock column unchanged. Concrete stays generated recipe data until the Mixer arrives |
| D-P5-3 | (a) **one lane** | §14's opening sentence, rewritten: `flow.ts` was right since M2. STANDARDS 2.1 closes as a **deliberate** divergence from Factorio, the only reference of seven with two lanes |
| D-P5-4 | (a) the word splits — the bot's blueprint **file** is a harness JSON built in Phase 5; the in-game blueprint is Phase 7's found unlock and Phase 8's build | §14, §19's tedium audit **and §24 risk 1's mitigation**, which had rested on "blueprints from minute one" and now rests on the kit stamp. STANDARDS 1.11 and dealbreaker 1 close: the gate is visible from minute one, which is the condition the baseline forgives it under |
| D-P5-5 | (a) a **per-inserter filter setting**, one item kind, off by default | §13's Inserter row. No new machine, no tier ladder, D-INSERTERS-1 intact. STANDARDS 2.5 closes; builds at M3 |
| D-P5-6 | **(c)** the route appears only while a tram stop is selected | none — §4's fixed vocabulary is untouched, which is why (c) was preferred to (a). The sentence lands at M5 |
| D-P5-7 | (a) `E-chain`, `E-coal`, `E-tram` | `PHASES.md`'s experiment line. `E10` stays the retired bloom-cadence run in §25 item 1, C8, D-P3-5 and D-P3-11; nothing new is tagged `E10` |

**The five M1/M2 constants**, each on its own recommendation: **D-P4-2** (the stand-in through M2; M1's rubble model sets units per tile against §12's 300 a tile and 75–105k a block), **D-P4-3** (the 160-tile placeholder stays inert; Phase 9's validator places facilities), **D-P4-12** (a `coal` rubble kind on rail-yard / industrial lots at **~700 a lot**), **C5** (coal ~700, already in the doc body; steel stays the named placeholder M1 sizes), **D-B2-1** ((a) for the slice as built, and **(b) is what Phase 5 builds** — a workbench recipe with a craft time, priced by M1's recipe table).

**And two programme calls the same message settled**, both of them decisions 1 and 2 below:

- **D-GB-1-rider — Phase 6 builds D-GB-1's hybrid**, as its own milestone and its own doc-edit pass. Phase 5 stays the phase that finishes the factory; the loop change that moves §5, §10, §11, §18 and §19 does not get folded into a corner of M4. `PHASES.md` Phase 6 now says so in its first sentence.
- **D-HOUR-3 — hour one is re-scoped to 75 minutes.** North's claim at 65:00 becomes a scored check with its own DoD line, and the HQ's white border and the Electricians stop being beats a 3,600 s run can never reach. `E-hour-north` already holds to 75:00 on every seed **[sim: B-M6-hour-north]**. **It builds at M1**, because §11's minute table is generated by docsync from `constants.ts` `HOUR` — the re-scope is a code change first and a doc change by regeneration.

### 12a. The one place a decided row overrode the design doc

Every other row filled a blank. **D-P4-12 did not.** §12's raws table read *"Coal | rail-yard rubble (direct, ~30k per block)"* — untagged prose, no run behind it — and the row's recommendation was **~700 a lot**, 43× smaller, on the argument that west's claim replaces the HQ patch rather than ending coal for the game. §2's corrected premise put both numbers in front of the human before the row was signed, so the decision was informed and §12's line is now rewritten to ~700 with a changelog line. `PROGRESS.md` names it as the sentence to reopen if ~30k was the intent; **`E-coal` measures it at M1** — at ~700 the Turbine hall has a job, at ~30k it does not.

### 12b. What Claude decided, and what it did not

Three rows are named by Phase 5 milestones but were **not** in T11's list, so they were taken `provisional` under constitution rule 14 and are attributed to Claude Code, not to the human: **D-B2-2** (slots stay `floor(area / 600)` now, dropped at M6 when tile lines replace the block stand-in), **C3** (unchanged — one ammo line per interior block), **D-SA-2** (a string table in Phase 5, translation bought in Phase 13). T13 and T17 name them in their blocked-by lines so they surface at the milestone that needs them. **Nothing was built from any of the fourteen decided rows in this task**: T11 is doc-only, and T12 (M1) is the first build.

One task line was corrected while the block list was regenerated: **T15 no longer waits on D-GB-2's form.** Gate B passed *with* D-GB-2 as a condition to be resolved **inside** Phase 5, so M4 resolves it — the row's recommendation is the rule-8-legal forms first (a HUD next-objective line driven by the sim's own state), then re-ask. A quest system stays the human's to take, and takes constitution rule 8 with it.

---

## Three decisions for the human — all three answered 2026-09-05

**All three were answered by the human's message *"Go with your recommendation for all"* (see §12): decision 1 is D-GB-1-rider (Phase 6), decision 2 is D-HOUR-3 (75 minutes, at M1), decision 3 is the seven rows of §2c. They are kept below as written, so the recommendations that were taken can be read back.**


1. **Which phase builds D-GB-1's hybrid.** It is decided and not built, and it is the only inherited item that changes the loop rather than completing it. **Recommend Phase 6**, with the threat trigger it contains: Phase 5 is already six weeks of completing §12–§14, and putting a loop change inside it means the phase that finishes the factory is also the phase that moves §5, §11 and §18. If it goes in Phase 5, it wants its own milestone and its own doc-edit pass, not a corner of M4.
2. **Re-scope §11's hour to 75 minutes, or split the beats.** `E-hour-north` already shows north Held at 65:29–65:34 and standing to 75:00 on every seed. Re-scoping turns the enclosure, the Electricians and the burn-off payoff from promises into scored checks and answers three of §3d's observations in one session; leaving it at 60 means §11's "30–60 min" paragraph keeps describing minute 65 and the next played hour will miss the same beats Gate B missed. **Recommend re-scoping**, with north's claim getting its own DoD line as `DEFERRED.md` asks.
3. **The six rows of §2c, together, before M1 opens.** They are one sitting: the map view's brownout read, which §13 machines are Phase 5's, one belt lane or two, what "blueprints from minute one" means against Phase 7 and 8, where the filter lives, and the tram route on the map view. Five of the six change a sentence in `RELIGHT-design.md`; none of them is a run. **The belt-lane one is the load-bearing one** — it decides how much of M3 exists — and the blueprint one is the one that reaches furthest, because Phase 7's dealbreaker row and Phase 8's whole line are built on the answer.
