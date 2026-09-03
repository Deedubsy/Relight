# Calibration report — map-view prototype

Brief: make the prototype able to test what it exists to test. As built (`PROTOTYPE_BUILD_REPORT.md` 6.1, 6.2) a tester spent two hours choosing shapes with no consequence and then hit a steel wall unrelated to shape. This report records what the bots measured, which proto-config numbers moved, and which targets could not be met with the levers allowed. No mechanic was added; no doc rule changed. Measuring instrument: `packages/sim/test/calibrate.ts` (the TypeScript sim, scattered map, seeds 3/4/5, economy on, 3 h), not the Python sim.

Calibrated config hash: **`46619ab5`** (economy on); the `economy=0` control is `91cbab82`. The proto's HUD export carries it as `meta.configHash`.

## 1. Cadence change and new reference totals

`gap_after` in `frontsim.py` and the default in `bots.ts` went from 8 minutes to **5 minutes** after hour one (the cadence §18 is drawn at, E9). Fixtures re-exported (`export_fixtures.py` → `packages/sim/fixtures/seed{3,4,5}.json`); `npm test` 28/28 pass. The economy is off in the fixtures and stays off.

Five-hour magazine totals, old (8 min) → new (5 min), seeds 3 / 4 / 5:

| Map | Policy | Old | New | Blocks lost, new (old) |
|---|---|---|---|---|
| scattered | compact | 5307 / 4168 / 3768 | 9689 / 6727 / 5700 | 0 (0) |
| scattered | spike | 13632 / 14298 / 14443 | 15779 / 16542 / 16184 | 4 / 9 / 11 (0) |
| scattered | cheapest | 5740 / 6026 / 5551 | 7135 / 7417 / 7004 | 0 (0) |
| scattered | turtle | 878 / 879 / 879 | 878 / 879 / 879 | 0 |
| open | compact | 10647 / 10647 / 10614 | 13557 / 13560 / 13508 | 2 |
| open | spike | 16506 / 16483 / 16484 | 17442 / 17463 / 17378 | 23 / 21 / 18 (4) |
| open | cheapest | 6180 / 5959 / 5935 | 7668 / 7799 / 7760 | 0 |

Ratios at equal territory (sum over seeds): scattered spike / compact **2.2×** (was 3.2×), cheapest / compact **0.97×** (was 1.31×); open 1.3× (was 1.6×) and 0.57× (was 0.56×). Doc tags updated with one changelog line each: §7 item 1, §19 (constraint-or-friction paragraph and the ignore test's spike sentence), and §27's evidence line, which quoted the same 3.2× / 1.31× and is beyond the §7/§19 brief; flagged here. Not edited: §25 item 13, which was closed on E12's "spike lost none on the scattered map"; at the 5-minute cadence spike loses 4–11 there. That needs a human, not a retag.

Why the spike ratio fell: at 5 minutes per claim compact's front is bigger sooner, so its own ammo bill rises faster than spike's; the corridor is still the expensive shape, by less.

## 2. Targets

Bots as the brief defines them (Step 4: they buy an assembler when demand passes 80 % of production and they can afford it). Config `46619ab5`, seeds 3 / 4 / 5.

| # | Bot | Target | Result | Value per seed | Lever |
|---|---|---|---|---|---|
| T1 | compact | first amber 60–90 min | **missed** | never / never / never | none produced it (part 5) |
| T2 | compact | no red before 120 min | met | never / never / never | — (trivially, see T1) |
| T3 | compact | 0 lost, no steel or copper stall, to 180 min | **met** | lost 0, stalls 0, ×3; steel at 180: 2178 / 2525 / 1528 | lever 3 (yield 32) |
| T4 | spike | red pip by 90 min | **missed** | never / never / never | none (part 5) |
| T5 | spike | 1–4 blocks lost by 180 min | **missed** | 0 / 0 / 0 | none (part 5) |
| T6 | cheapest | reaches 180 without stalling; mags per held block 0.7–1.3× compact's | half | stalls 0 ×3; ratio 1.25 / 1.43 / 1.30 | stalls: lever 3; the ratio moved with no lever |
| T7 | all | Held at 120 min in 20–30 | **missed** | 16 for every bot and seed | structural (part 5) |

The same config with the bot's build rule off, and with a harness-only stand-in for a tester who builds when a pip first leaves green (`--react 1`; not in the proto, measurement only):

| Bot | Never builds: first amber / red / loss, lost by 180 | Builds at the first pip: amber / red / loss, lost by 180 |
|---|---|---|
| compact | 119 / 121 / 132 m, 8 · 162 / 164 / 167 m, 3 · 119 / 121 / 125 m, 12 | 119 / never / never, 0 · 162 / never / never, 0 · 119 / 121 / never, 0 |
| spike | 66 / 66 / 68 m, 24 · 67 / 69 / 70 m, 23 · 66 / 66 / 68 m, 24 (held 4–5 at 180) | 66 / 66 / 74 m, 2 · 67 / 69 / never, 0 · 66 / 66 / 68 m, 2 |
| cheapest | 104 / 104 / 109 m, 16 · 102 / 103 / 107 m, 16 · 101 / 101 / 103 m, 17 | 104 / 104 / never, 0 · 102 / never / never, 0 · 101 / 101 / never, 0 |

Against the targets, that tester meets T2, T3 and T4, and T5 on two seeds of three (0 lost on seed 4). T1 stays missed at 119–162 min. The `economy=0` control gives the "never builds" column exactly, because with the economy off the bots do not build and the recipe never binds: compact's first pip is at 119 / 162 / 119 min, spike's at 66–67, cheapest's at 101–104.

## 3. Every `PROTO-CALIBRATED` number, old → new

All in `packages/sim/src/types.ts`, proto section (`PROTO_CALIBRATED`), applied by `protoCalibrated()` in the proto's session and in the harness.

| Number | Old | New | Target served | How chosen |
|---|---|---|---|---|
| `startAsmRate` (the start assembler's rate) | 20 | **10** | T2 | lever 1; at 20 the HUD-watching compact bot buys its second assembler at 86 / 168 / 94 min, at 10 at 54 / 54 / 66; nothing else measured changed |
| `startRounds` | 300 | 300 | — | lever 2 tried at 100 and rejected (part 4) |
| `eco.yieldPerMin` (rubble per Held block-minute, civic : residential : industrial 1 : 1 : 1) | 1 | **32** | T3 | lever 3; lowest level in the sweep 4 / 8 / 12 / 16 / 24 / 26 / 28 / 30 / 32 at which compact and cheapest run 180 min with 0 stalls and 0 lost; 30 leaves one stall at 175 min (cheapest, seed 5) |
| `eco.startPatch` | 240 steel at 2/min | **7680 steel at 64/min** | T3 | perMin = 2 × yield (the patch scales with the residential rate); total = 120 min × perMin so it still empties at ~2 h (§19) |
| `eco.claimCost` | 5 Cu + 10 steel | unchanged | — | lever 4 not triggered: T3 held after lever 3 |
| `eco.assemblerCost` | 20 Cu + 40 steel | unchanged | — | lever 5 not triggered: compact affords its second assembler at 54–66 min, before any pressure |
| `eco.startStock` | 40 Cu, 80 steel | unchanged | — | not a lever |
| `eco.magazineCost` | none (assumption 2) | **2 steel + 1 Cu per magazine** | — | §12 recipe, doc-derived; assumption 2 removed from the build report |
| `startAssemblers` | 1 | 1 | — | unchanged |

Bot rule (Step 4, `bots.ts`, `PROTO-ASSUMPTION`): a bot adds an assembler when the 10-minute demand exceeds 80 % of production and the stock affords it, checked once a minute; the build is logged in telemetry with `source: 'bot'` like a player build. Proto bots build; fixture bots do not (the economy is off there).

## 4. Levers tried and rejected, and what each did

- **Baseline with the recipe on** (20 mag/min start assembler, 300 rounds, yield 1, patch 2/min): the assembler filling the 400-magazine buffer costs 40 steel a minute against 2 a minute of income. Steel is 0 by minute 2–4, the first claim at 15 min is rejected, the hoppers dry out and the start block falls at 30–42 min. Every bot identical; Held 0 at 60 min. The steel wall of item 6.2 moved from hour 2 to minute 3.
- **Lever 1 alone** (Mk1 at 10 mag/min): same collapse, 20 steel a minute against 2. Kept anyway; its measured effect at a viable yield is only when the HUD-watcher's purchase happens (above).
- **Lever 2** (start rounds 100): 100 rounds fill one of the start block's hoppers and leave the others empty, so the tester's first sight is two red pips for about two minutes; at every yield the rest of the run is unchanged to the minute. Rejected; 300 kept.
- **Lever 3 sweep** (yield Y, patch 2Y/min for 120 min), compact unless said: Y 4 — Held 1 for three hours, 27 stalls. Y 8 — Held 4, 24 stalls. Y 12 — second assembler at ~54 min, stalls from 60 min, losses late; spike bankrupts itself buying at 38 min (22–23 stalls, Held 3–6, lost 0–2). Y 16 — compact Held 4 / 16 / 20–22, stalls 6–9 from 135–150 min once the patch is gone, first amber 160–176 min, seed 5 loses 6; spike reaches the industrial band, buys 5–6 assemblers, never sees a pip; cheapest loses 14–16. Y 24 — compact stalls 0 / 0 / 1, cheapest stalls 3–6 from 150 min, seed 5 loses 4. Y 26 — cheapest stalls 2 / 2 / 4. Y 28 — 0 / 1 / 3. Y 30 — 0 / 0 / 1. **Y 32 — 0 stalls, 0 lost, all bots.** At no yield did any building bot see a pip that was not steel starvation.
- **Lever 4, lever 5**: not reached under their conditions (part 3).
- **Two diagnostics outside the levers**, run to see what the tempting knob would do, not applied: buffer cap 600 rounds instead of 4000 at yield 32 — spike amber at 97 min on two seeds, nothing lost, compact still never (its bot buys at 54 min); Mk1 at 5 mag/min — the bots buy at 16–18 min, no pip ever.

## 5. Targets that could not be met, and what mechanic was tempting

**T1 (compact amber at 60–90 min).** Not reachable with levers 1–5 under the brief's own bot. Two facts combine. A pip leaves green only when the shared buffer is empty, since hoppers are topped up every tick while any buffer remains; compact's demand at 60–90 min is 8–12 mag/min against 10 mag/min of production, and the buffer holds 400 magazines, so it cannot empty before ~120 min (the never-builds measurement: 119 / 162 / 119). And the Step-4 bot buys at 80 % of production, which compact's demand crosses at 54–66 min, before any pip; every yield that satisfies T3 also makes the 40-steel purchase affordable then, so the pip is pre-empted. Levers that were tempting and are not levers: the buffer cap (`bufferCap` 4000 rounds; 600 gives spike a pip at 97 min and compact still none), a pip rule that reads the buffer instead of the hopper (doc rule), or a bot that waits for the pip (which is what the `--react` measurement shows: compact's first amber then sits at 119–162 min, with red two minutes behind it).

**T4, T5 (spike red by 90 min, loses 1–4 blocks).** Not reachable by price. On this map the industrial band (every block north of row 11, and rows 11–16 in columns 9–15) lies on the straight push's path; the spike bot holds industrial blocks from its fourth claim and each yields 32 steel a minute, so it ends with 25–37 k steel and buys 5–6 assemblers as demand rises (peak 81–91 mag/min). Compact's blob sits in the civic and residential rows and reaches no industrial block before ~150 min. Raising yield makes spike richer faster; lowering it starves compact first. The one lever that would bite, an assembler price in the thousands of steel, is lever 5 in the direction the brief forbids. Tempting mechanics: one fungible rubble so that 1 : 1 : 1 means what it says; a map whose steel is not on the corridor; a per-assembler price that scales. All out of scope. Without any of them, the spike's price is *time*: its first pip is at 66 min against compact's 119, and a tester who does not react by 68–70 min loses the block at the tip.

**T6 (cheapest's ammo per held block within 0.7–1.3× compact's).** 1.25 / 1.43 / 1.30 at every yield from 24 up; the levers move steel, not blooms, so nothing moved it. Cheapest reaches 180 min without stalling, which is the half that guards the session.

**T7 (Held at 120 min in 20–30).** 16 for every bot at every config: four claims in hour one, eleven in hour two, plus the start block (the twelfth lands at exactly 2:00:00). Reaching 20 needs a claim every ~4 minutes, which is the cadence question §18 and item 7.2 leave to a human, and cadence is not a lever.

**What the calibrated build can test.** Shape sets the minute at which one assembler stops being enough: 66 min for a push, 101–104 for quiet blocks, 119–162 for a blob. A tester who builds when the HUD's demand nears production sees nothing and loses nothing on any shape; a tester who waits for the pip has about two minutes; a tester who never builds loses the push at 68–70 min and the blob at 125–167. The 2.5-hour session in `PROTOTYPE_TEST_PLAN.md` is set so that all three moments fall inside it.

## 6. The session from the tester's seat

Minute 0: one assembler, three green pips on the start block, 40 copper and 80 steel and a steel patch under the block. The first claim is affordable at once; claims every fifteen minutes in the first hour are the pace the bots keep, and each one adds 1–3 green pips to the list on the right. Nothing turns. The HUD's demand number climbs from 3 to about 9 by the hour, and a tester who reads it against the assembler's 10 sees the second assembler coming; steel is never the reason not to buy it (stock is above 1 000 by minute 30 with the patch running). At minute 66, if the territory is a corridor pointing north, the pip at the tip goes amber and then red inside the same minute, and the block is gone by minute 70 unless an assembler was bought; a blob sees nothing yet. Between minutes 100 and 105 a scattered, quiet-block territory has the same moment. At two hours the patch is dry; a blob that has stayed in the civic and residential rows now has a few hundred steel and no income, but its magazines are paid for to the session's end. At minute 119 (seed 3) the blob's own pip turns, red two minutes later, first block lost around minute 125–132 if nothing is bought, and a single 40-steel assembler bought at the pip holds everything to 150. By minute 150 the tester holds about 22 blocks, has seen one pip turn or none depending on what they built and when, and has never been stopped by the budget.

## 7. Browser verification (step 5)

Four runs of the built prototype in headless Chromium 151 against the Vite dev server (1200 × 900 window), seed 3, 16× selected with the speed button, three hours of sim time each. Each run took 681–686 s of real time at 15.8–16.0 ticks/s, so a 3-hour session at 16× is about 11.5 real minutes. The tab has to be the foreground page: in the background Phaser throttled the run to under half of wall clock, and the sim clock is frame-driven. Config hash read from the page: `46619ab5` for the three bot runs, `91cbab82` for the control. The console held no errors, warnings or assertions in any run apart from the browser's own `favicon.ico` 404 on the first page load. `exportJson()` parsed in every run with 14 top-level keys; every assembler in the three bot runs was placed by the bot and logged with `source: 'bot'`.

| run | first amber | first red | first loss | first enclosure | Held 60 / 120 / 180 | steel / copper at 180 | assemblers (built at min) | lost at 180 | export |
|---|---|---|---|---|---|---|---|---|---|
| compact | never | never | never | 1:25:37 | 4 / 16 / 28 | 2 163 / 18 714 | 2 (54) | 0 | 317 752 bytes |
| spike | never | never | never | 1:40:59 | 4 / 16 / 28 | 37 064 / 11 118 | 7 (38, 69, 82, 97, 128, 160) | 0 | 323 247 bytes |
| cheapest | never | never | never | 1:00:29 | 4 / 16 / 28 | 733 / 11 141 | 3 (60, 117) | 0 | 319 902 bytes |
| control: compact, `economy=0` | 1:58:47 | 2:00:34 | 2:11:33 | 1:25:37 | 4 / 16 / 20 | n/a (no economy; stock stays 80 / 40) | 1 (none built) | 8 | 310 608 bytes |

Demand against production at 3:00: spike 77 mag/min against 130, cheapest 26 against 50, compact under 20 against 20, the control 12.7 against 10 with one hopper empty. The control run confirms the pre-calibration behaviour the brief describes: no pip leaves green before two hours (1:58:47), red follows in under two minutes, the first block falls eleven minutes later, and eight are gone by 3:00. Its three timestamps and its loss count match the node harness's never-builds run for seed 3 (119 / 121 / 132 min, 8 lost) to the minute; compact's steel at 3:00 is within 1 % of the harness (2 163 read at 3:00:05 against 2 178). Cheapest ends hour three on 733 steel with the patch gone and its income all copper and stone, which is the thinnest margin in the table and the reason yield could not go below 32 (part 4).

Not verified here: seeds 4 and 5 in the browser (harness only, part 2), the 4× pace the test plan prescribes for its 2.5-hour session (measured once in the earlier build verification at 4.0×), and any human input; the runs above are autoplay bots with no tester at the mouse.
