# Calibration 2 — machine slots, finite rubble, and the D1–D4 doc edits

Brief: `CALIBRATION_REPORT.md` found that with any economy and a build rule no bot ever feels the front, because an assembler is weightless and rubble is infinite. This pass put two rules the doc already has into the prototype (§5: only interior blocks host machines; §12: rubble is finite), re-ran the seven targets plus two new ones, applied decisions D1–D4 to `RELIGHT-design.md`, re-ran E4 for D1, and rewrote the test plan's expected timeline. Same rules as calibration 1: proto-config numbers may move; district numbers, bloom rules, the unfed rule and hopper size did not. Fixtures stay green and identical (`export_fixtures.py` re-exported byte-for-byte; `npm test` 28 pass).

Config hashes: **`6e74fbfd`** (economy on, the tester build), **`91bad3aa`** (`economy=0` control). Old: `46619ab5` / `91cbab82`.

## 1. The two rules as built

**Machine slots** (`sim.ts`: `isInterior`, `freeSlot`, `addAssembler`; `Block.machine`). Each Held block has one machine slot. A front block's slot is taken by its defence ring, so only Interior blocks can host an assembler; the HQ is the one exception and starts with the Mk1 in its slot. Buying an assembler needs an empty interior slot; when none exists the build is rejected with the reason (`assembler-rejected`) and the panel's button is greyed with the reason and the coordinates of the next slot. A block that falls loses its machine (`machine-lost`, assembler count drops). A block that goes from Interior back to front keeps its machine and shows it at risk (red slot square, slow blink). The slot rule is enforced whether or not the economy is on: it is a doc rule, not an economy rule. Fixtures never call `addAssembler` (they use the scheduled assemblers), so they are untouched.

`PROTO-ASSUMPTION`s: one slot per block (§13 footprints would allow more; one is the strictest reading); the slot a purchase goes into is chosen automatically, nearest the start (the map view has no placement).

**Finite rubble** (`Block.pool`, `EconomyConfig.pool`). Each Held block has a rubble pool by district; the flat yield draws it down, at zero the block yields nothing and its rubble strip (top of the square) is gone. `PROTO-ASSUMPTION` for the size: 3,840 units for every district, which is 120 min × the calibrated yield of 32/min, so a block lasts about as long as §19's "start block's steel runs out in ~2 hours". Every run-dry is logged (`run-dry` event, `dryLog`, telemetry `dry[]`).

**Bots** (`bots.ts`). The build rule stays (demand > 80 % of production, affordable, once a minute) and now also needs a free interior slot. The compact bot alone has `claim-to-enclose`: when no slot is free and demand > 60 % of production it picks the candidate that most increases the interior count (ties: least frontage, then the old distance keys). Spike and cheapest got no such rule.

**UI.** Slot square bottom-right of each held block (outline = free interior slot, filled dark = machine, red = machine at risk; HQ exempt), rubble strip alpha = pool left, HUD lines for slots used / free, machines at risk and blocks dug out, tooltip on held blocks (slot state, rubble left), toasts for build, rejection, machine lost and run-dry. Telemetry per-minute rows carry `slotsUsed`, `slotsFree`, `atRisk`, `dry`; the summary carries `slotsUsed`, `slotsFree`, `machinesLost`, `ranDry`.

## 2. Targets (config `6e74fbfd`, economy on, bots build, seeds 3 / 4 / 5, 3 h)

| # | Target | Result | Seed 3 | Seed 4 | Seed 5 |
|---|---|---|---|---|---|
| T1 | compact: first amber 60–120 min | **MISSED** | never | never | never |
| T2 | compact: no red before 120 min | MET | never | never | never |
| T3 | compact: 0 lost, 0 stalls to 180 | MET | 0 / 0 | 0 / 0 | 0 / 0 |
| T4 | spike: red by 90 min | MET | 66 m | 69 m | 66 m |
| T5 | spike: 1–4 lost by 180 | **MISSED** | 24 (2 distinct blocks) | 23 (3 distinct) | 24 (2 distinct) |
| T6 | cheapest: no stalls, mags/held 0.7–1.3× compact | **MISSED** | 0 stalls, 1.13× | 0 stalls, 1.44× | 0 stalls, 1.31× |
| T7 | all: held at 120 min in 20–30 | **MISSED** | 16 / 4 / 16 | 16 / 5 / 16 | 16 / 4 / 16 |
| T8 | compact: first enclosure before first amber, ≥ 2 of 3 seeds | MET | encl 60 m, amber never | 60 m, never | 60 m, never |
| T9 | spike: never more than 2 assemblers before 120 min | MET | max 1 | max 1 | max 1 |

Per-bot lines (seed 3 / 4 / 5):

| Bot | Assembler bought | Held 60/120/180 | Slots used/free 60 · 120 · 180 | Dry 60/120/180 | Steel at 180 | Peak demand (at) |
|---|---|---|---|---|---|---|
| compact | 63 m + 153 m / 63 m / 71 m + 172 m | 4/16/28 | 1/0 · 2/6 · 3/15 (s4 2/7 · 2/17; s5 2/4 · 3/12) | 0/1/4 | 1.7 k / 2.6 k / 1.6 k | 27.4 (176 m) / 23.1 (179 m) / 25.3 (172 m) |
| spike | never (no slot ever exists) | 4/4/4 (s4 4/5/5) | 1/0 · 1/0 · 1/0 | 0/1/4 | 6.4 k / 7.2 k / 6.4 k | 24.2 (71 m) / 22.5 (73 m) / 25.2 (71 m) |
| cheapest | 63 m + 117 m / 63 m + 122 m / 63 m + 104 m | 4/16/28 | 1/0 · 3/3 · 3/13 (s4 2/3 · 3/16; s5 3/2 · 3/12) | 0/1/4 | 0.7 k / 0.7 k / 0 (at 179 m, no stall) | 26.7 / 29.6 / 32.3 |

Controls on the same config:

| Run | compact first amber | compact red | compact lost | spike lost | cheapest lost |
|---|---|---|---|---|---|
| bots do not build (`--build 0`) | 119 / 162 / 119 m | 121 / 164 / 121 m | 8 / 3 / 12 | 24 / 23 / 24 | 16 / 16 / 17 |
| bots build at the pip (`--react 1`) | 119 / 162 / 119 m | never / never / 121 m | 0 / 0 / 0 | 24 / 23 / 24 | 0 / 0 / 0 |
| `economy=0` (`91bad3aa`) | 134 / 165 / 119 m | 134 / 166 / 121 m | 9 / 3 / 11 | 24 / 23 / 24 | 16 / 16 / 17 |

## 3. Every `PROTO-CALIBRATED` and `PROTO-ASSUMPTION` number, old → new

| Number | Old | New | Why |
|---|---|---|---|
| `eco.pool` (rubble units per Held block, civ / res / ind) | none (rubble infinite) | **3840 / 3840 / 3840** | new, `PROTO-ASSUMPTION`: 120 min × `yieldPerMin`; lever 2 swept 1920 and 960 (part 4) and left at 3840 |
| slots per block | none | **1** | doc rule (§5); count is a `PROTO-ASSUMPTION`, held at one by the brief |
| `BOT_ENCLOSE_DEMAND_FRAC` | none | **0.6** | brief: compact prefers `closes N` when no slot is free and demand > 60 % |
| `BOT_BUILD_DEMAND_FRAC` | 0.8 | 0.8 | unchanged; now also needs a free slot |
| `eco.yieldPerMin` | 32 | 32 | lever 3 swept 16 and 8, rejected (part 4) |
| `startRounds` | 300 | 300 | lever 4 swept 100 and 1000, rejected (part 4) |
| `startAsmRate` / `asmRate` | 10 / 20 | 10 / 20 | not levers this pass |
| `eco.startPatch` | 7680 at 64/min | unchanged | empties at 120 min on every run |
| `eco.claimCost`, `eco.assemblerCost` | 5 Cu + 10 st, 20 Cu + 40 st | unchanged | lever 5 not triggered: T3 held |
| `eco.startStock`, `eco.magazineCost`, `startAssemblers` | 40 Cu + 80 st; 2 st + 1 Cu; 1 | unchanged | — |

## 4. Levers tried, in the brief's order, and what each did

Slot count stayed at one. Every lever below leaves T1 at "never" on all three seeds.

| Lever | Value | Effect |
|---|---|---|
| rubble pool | 1920 | nothing measured changes in 3 h (compact and cheapest run dry later than they need the block) |
| rubble pool | 960 | cheapest stalls 14 times on every seed and holds 8 at 120 min (T6, T7 worse); compact unchanged; T1 unchanged |
| yield | 16 | cheapest seed 5 stalls 13 times; T1 unchanged |
| yield | 8 | T3 fails (compact 12–21 stalls, first enclosure 110–130 min); T1 unchanged |
| start rounds | 100 | amber and red at minute 0 on every bot (100 rounds fill one hopper; the pip artefact from calibration 1); T8 fails |
| start rounds | 1000 | spike's red moves 66–69 → 69–72 min; nothing else |

Claim and assembler cost were not touched: T3 never failed after the pool and yield sweeps.

## 5. What could not be met, and why

**T1 (compact first amber 60–120 min) cannot be met with slots and finite rubble under the 80 % build rule.** The chain, seed 3 (`cal2/tl-compact3.txt`): demand is 8.4 mag/min at 1:00 against the Mk1's 10, so the buffer fills to 400 magazines by 1:20 on Mk1 surplus alone. Compact's fourth claim (1:00) is its first enclosure on every seed, which is the first interior slot; the bot has 2,690 steel and buys the assembler at 1:03 from start stock, and 30 mag/min exceeds compact's demand for the rest of the session (peak 27.4 at 2:56). The pip never leaves green. No allowed lever touches that: the pool only matters after 2:00, yield changes when the bot can afford the assembler (which is never the binding constraint), and start rounds only move the hour-zero pip. Without the build (`--build 0`) the front does bite: amber at 1:59 / 2:42 / 1:59, 3–12 blocks lost by 3:00; with a build at the pip (`--react 1`) nothing is lost. So the tester's experience depends entirely on whether they buy the second assembler before 2:00, and the slot rule makes that purchase wait for the first enclosure (1:00) rather than for money. The front's price now shows as **shape** (T8: enclose first, then build; T9 and spike's column: a push can never build), not as an amber pip.

**T5 (spike 1–4 lost)** fails for the reason calibration 1 named: the bot retakes the block it lost and loses it again to a shade 3.5 minutes later, every 5 minutes, so 23–24 losses fall on 2–3 distinct blocks. A human would not do this twenty times; the number to read is the distinct count, which is inside 1–4. Fixing it means a bot rule ("do not retake a block you lost to a shade"), which is a bot change, not a calibration lever, and was not made.

**T6 (cheapest mags/held 0.7–1.3× compact)** is 1.13 / 1.44 / 1.31: quiet-block play costs about what §7 says it costs on the scattered map (1.3×) and the band is too narrow by 0.1. **T7 (held 20–30 at 120 min)** is unmeetable by cadence: 3 claims in hour one plus one per 5 minutes after is 16 at 2:00 for every bot that never loses a block. Both stand as they were in calibration 1.

**What the tester test should run on.** The brief said that if T1/T4/T5 still cannot be met, the front's price needs a rule the map view can't represent and the human test should run on `economy=0` only. T4 is met and T5 fails only on the retake loop, so the half of that condition that holds is T1. Recommendation: run the five testers on `6e74fbfd` as the test plan says, because the slot rule gives the economy a shape reading (enclose to build) that `economy=0` also has (assemblers are free there but still need a slot), and keep the sixth `economy=0` control. What the test cannot see on either config is a compact player who watches the HUD feeling any pressure before 2:30; the harness says that player never will.

## 6. D1–D4 edits and the E4 re-run

`frontsim.py` gained `first_hour(sub_kw, interior_kw, patch, patch_exc_at)` and `--draw=half` in `run()` (100/20 kW, Contested 100); defaults are unchanged, so every old tag reproduces and the fixtures are byte-identical. New tags `E4h-*`, `E2-demand-half`, `E2-demand-half-25h` run under `--experiments`.

**D1, power.** §5 cost table: substation draw 100 kW for a Contested or Held block with a dark neighbour, 20 kW interior (was 200/40); the cascade's knock-on draw is 80 kW. E4 re-run (300 kW Generators, 40 coal in hand, a 3,000-unit coal patch on the HQ lot mined by a 60 kW Excavator from minute 6, §11's machine list):

| Run | Result |
|---|---|
| one Generator | brownout 8.0 min (was 6.0 at 200 kW); HQ shed 8.3 min |
| Generators at 0, 6 | brownout 17.6 min |
| Generators at 0, 6, 15 | brownout **45.0 min**, when the fifth Excavator and third Assembler land |
| Generators at 0, 6, 15, 25 (or 0, 6, 15, 45) | no brownout to 60 min; peak 980 kW, 660 kW of it machines |
| Generators needed per 10-min slot | 2, 3, 3, 3, 4, 4 |

**Three Generators do not hold to minute 60; four do.** §11 now says three carry the front to minute 45 and the fourth comes with the minute-45 machines (was five by minute 40). The count is set by machines, not substations, which is D1's intent: the substations draw 320 kW of the 980 kW peak.

**The 3,000-unit patch does not run dry around minute 30.** At §13's Excavator rate (0.5/s) it mines out at about minute 106; hour one burns 609 coal in total and 207 by minute 30. A ~700-unit patch mines out at minute 30 (450 at 21 min, 1,000 at 39 min). The doc says ~3,000 and states the measured mine-out time; §25 item 14 is closed on the draw and records the patch size as not settled, so the choice is yours: keep 3,000 and accept that west's coal is wanted in hour two, or cut it to ~700 so west is wanted by minute 30.

§12 power row from `E2-demand-half` (compact, seed 3): early 1.0–3.1 MW (was 1.2–4.5), mid 3.1–5.5 (was 4.5–8), late 5.5–8.6 (was 10–15), Relight 40 MW unchanged. At the old draw the same run reads 1.5–5.6 / 5.6–10 / 10–16.4 MW, so the doc's old row was already below its own model after the calibration-1 cadence change. §23 risk 7 updated to the same numbers.

**D2, fall time.** §5: a block is Dark 90 s after its substation stops, wherever the substation sits; the 48 s figure is deleted; the 30 s rescue window against the 60 s refeed reset is stated as a play we want. §25 item 12 closed. No sim change (the sim already used 90 s).

**D3, unfed rule.** §5: one sentence added, the 40-arrival counter is per block across all its edges, so corner blocks fail faster. No sim change (both sims already count per block).

**D4, Relight.** §16 step 4: survivable with preparation; banking magazines over the last hour is the intended play; ~2× an hour's production banked should lose fewer than 5 blocks; the E11 figures are labelled the unprepared floor. §25 item 5 reframed to confirming that number with the wake-size bloom on every awake block before the world view builds the hold. E11 was not re-run in this pass (the brief scheduled it before the world view, not here).

Every edit has a changelog line in `RELIGHT-design.md` (ten lines, D1–D4 and their tags).

## 7. What a tester sees, minute 0–150 (compact, seed 3, config `6e74fbfd`)

- **0:00–1:00.** One held block, then two, three, four (claims at 0:15, 0:30, 0:45, 1:00). Every pip green. The HUD's demand climbs 2.9 → 8.4 mag/min against 10; the buffer climbs 71 → 250 magazines. Slots read 1 used / 0 free and the build button is greyed: "no free interior slot". The start block's rubble strip shrinks (92 % → 69 %). Steel 520 → 2,690, so the money is there and the slot is not.
- **1:00.** The fourth claim closes the first block: interior 1, the slot square appears as an outline. A HUD-watcher buys the assembler at 1:03 (toast: assembler 2 at (13,20)); production 30. Buffer full (400) by 1:20.
- **1:00–2:00.** One claim per 5 minutes; interior 1 → 8; slots 2 used / 6 free by 2:00. Demand 8 → 15 against 30. Nothing turns. Rubble strips on the four start-row blocks fade together.
- **1:59:59.** The start patch empties and the HQ block is dug out in the same second (toast, strip gone). Steel is 4,500 and climbing from the other held blocks; nothing is felt.
- **2:00–2:30.** Held 16 → 22, interior 14, demand 22. Blocks (13,20) and (11,20) dig out at 2:15 and 2:30. Buffer 397 at 2:30, the first time it is below 400. Session ends at 2:30 with 0 lost and no pip ever off green.
- **If the tester never builds:** amber at 1:59, red at 2:01, first loss 2:12; a build then still holds. **Spike (seed 3):** amber and red at 1:06 with the buffer at 0, first loss 1:08 to a shade, four blocks held and no slot ever, so the build button is greyed for the whole session with the reason; the HQ digs out at 2:00 and the four-block column is 0 % rubble by 2:50.

## 8. Browser verification

Vite dev server restarted with `--force`, headless Chrome, seed 3, config `6e74fbfd` in every export. Console filtered to error / warn / assert: empty on all four runs.

| Run | Speed | Held | Lost | First amber / red | First enclosure | Assemblers (sim clock) | Slots used / free | Dug out | Export |
|---|---|---|---|---|---|---|---|---|---|
| compact, 3 h | 16× | 28 | 0 | never / never | 1:00:29 | 1:03, 2:33 | 3 / 15 | 4 | parses, 352 kB |
| spike, 3 h | 16× | 4 | 24 | 1:05:52 / 1:05:52 | never | none (no slot; button greyed with the reason) | 1 / 0 | 4 | parses, 353 kB |
| cheapest, 3 h | 16× | 28 | 0 | never / never | 1:00:29 | 1:03, 1:57 | 3 / 13 | 4 | parses, 354 kB |
| compact, 2.5 h (the tester protocol) | 4× | 22 | 0 | never / never | 1:00:29 | 1:03 | 2 / 12 | 2 (2:00, 2:15) | parses, 334 kB |

The 2.5 h run at 4× ends with 22 held, 14 interior, front 10, buffer 397 magazines, demand 21.6 against 30, steel 3.3 k: the same numbers as the harness's seed-3 timeline at 2:30. Every browser run reproduces the harness minute for minute (assembler times, run-dry times, losses), so the proto and the harness are running the same sim on the same config. `npm run build` is clean (Vite's chunk-size note only); `npm test` 28 pass, 0 fail.

## 9. Still structural

- **A HUD-watcher on compact never feels the front in 2.5 h.** T1 is out of reach with the levers allowed: the second assembler is gated by the first enclosure at 1:00, not by money, and 30 mag/min covers compact to 3 h. The price of shape is visible (a push cannot build; a blob must close a block before it can), the price of the front as a pip is not. If the test needs compact players to meet an amber pip, that needs a rule this pass was not allowed to touch (a smaller buffer than 400 magazines, a slower Mk1, or an assembler cost that scales with count).
- **The spike bot's retake loop** (T5) is a bot artefact; distinct blocks lost are 2–3.
- **The HQ coal patch size** (D1) is not settled: 3,000 units mines out at ~106 min, ~700 at 30 min; the doc carries 3,000 and says so.
- **E11 with the wake-size bloom and a banked stock** (D4) has not been run; §16's "fewer than 5 blocks" is a design statement awaiting that run.
- **§12's power row** now rests on one seed (compact, seed 3) at the halved draw; E2's cascade counts (3 / 9 blocks) were measured at 200/40 kW and are marked so in §5.
