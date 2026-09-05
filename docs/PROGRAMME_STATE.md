# Relight — programme state

The two-page handoff. **Replaced, never appended**: this file says where the programme is
now. The per-milestone history lives in the phase reports (`PHASE_0_REPORT.md` …
`PHASE_5_REPORT.md`, `SLICE_REPORT.md`, `GATE_B.md`) and in the pre-cleanup copy
`archive/pre-phase-5-cleanup/PROGRAMME_STATE-2026-09-05.md`. Rules: `CONSTITUTION.md`.
Tasks: `PROGRESS.md`. Decisions: `DECISIONS.md`.

## Now (2026-09-05, after RI-03 built physical commissioning and the field kit)

- **The revised development plan is in execution: RI-00, RI-01, RI-02 and RI-03 are done.**
  Daniel authorised the plan with "Whole plan" (D-RI-1); it is
  `REVISED_DEVELOPMENT_PLAN.md`, verbatim, and its tasks RI-00 … RI-13 are in
  `PROGRESS.md`. RI-01 built the real opening economy (`RI_PASS_1_REPORT.md` "RI-01"):
  the placed Assembler on four recipes (D-B2-1 (b)); rubble 300 a tile (D-P4-2); the rail
  yard's 3×3 heap of 702 coal belted into the Depot from 26:39–26:44 (D-P4-12);
  `HOUR_MINUTES` 75 with north scored (D-HOUR-3); every Assembler physical (D-P4-5);
  `ledger.ts` balancing every item; the hand-feed count split (D-P4-11). RI-02 built the
  opening guidance and the essential presentation ("RI-02"): the current-goal HUD line
  read off the state with its reason and an amber support line (D-GB-2 (a), rule 8 kept);
  machine running / starved / blocked / idle / off as a shape, a word and a reason; stable
  block names with debug coordinates behind the toggle; the save / load baseline (a state
  hash, a validated load, Ctrl+S / Ctrl+O to a local slot, a download carrying the command
  log); 1200 / 900 px breakpoints. `E-hour` 18/18: the goal line never wrong on 13,503
  samples, 42/42 beats, a save at 30:00 replaying to the unbroken hash on every seed.
  RI-03 built physical commissioning and field deployment ("RI-03"): one claim path — the
  map previews and charges nothing; a claim is a pole run to the block's substation, the
  claim's steel and copper delivered there from the pockets and an explicit Activate within
  reach, paid once, one commissioning id an attempt (D-RI-2); the field kit (poles, lights,
  turrets, belts, inserters, the outskirts Substation) on Dark / Contested blocks next to
  Held, on real pole power, never marking a block Held; the outskirts Substation before
  activation (D-CU-3 (b)); the hour bot on the physical path with the map claim beside it.
  `E-hour` 19/19: every claim an Activate, the claims 8–18 s after the map click's, 4 Held
  and no fall on every seed.
- **Next task: RI-04 — enemy origins, Crawler / Shade clarity and the Stalker
  prototype** (plan §6, §7; D-GB-4 owned there): emergence points with stable ids and
  valid placement, no spawn inside a secured interior; a Crawler's target and direction
  and a Shade's trace inspectable; the Stalker (`guard → investigate → pursue → attack →
  return`) tied to an occupied site, its §7.1 candidates in one candidate configuration
  and never in the benchmark (D-RI-5); `E-rifle` rerun and reported. **Ready**: blocked
  by RI-02 and RI-03, both done. Branch `ri-pass-1` on top of `phase-4`. The order after
  it: RI-05 → RI-06 → RI-07 → RI-08 (whose played session is **T19**, human) → RI-09
  (T13 → T17, T18) → RI-10 → RI-11 → RI-12 → RI-13; T12b and T12c are runnable behind
  RI-04 in list order.
- **Phase 5 — the factory, complete — is open; RI-01 built its first pieces** (the
  per-machine recipe, coal rubble, the ledger) and the rest is not built. The plan
  interleaves the phases (`PHASES.md` top note): the hybrid of D-GB-1 moved from Phase 6
  into RI-03 / RI-05 / RI-06 (D-RI-4); the three-systems stop became rule 2's scope test
  (D-RI-3); D-GB-2, D-CU-1 and D-CU-3 are provisional on the plan's defaults (D-RI-6).
- Phases 0–4 are complete. **Gate A** (2026-09-03) was an owner-only `go` on the bot
  calibration and the smoke test. **Gate B** (2026-09-05, `GATE_B.md`) is `proceed — with
  conditions`: one hour played by the owner, only minutes 0–10 recorded, no telemetry
  read; the conditions are D-GB-2 (provisional (a), built at RI-02, T19 tests it), D-GB-3 (rifle range,
  RI-08's candidate) and D-GB-4 (enemy density, RI-04 / RI-10) — none blocks RI-01.
- Branch `ri-pass-1` on `phase-4`; PR #5 (`phase-4`) open, #1–#4 merged (checked read-only on
  2026-09-05). Nothing
  is pushed by a task unless the human asks.

## What is built, what is a stand-in

**Implemented and measured** (`docs/EXPERIMENTS.md`, 13 experiments GREEN on 2026-09-05
at RI-03, stamped `38ec158` + RI-03's tree, config b95922d2; `SLICE_REPORT.md`):

- The block sim: §5's front rule, wake bloom and burn-off, the 40-unshot-arrival fall,
  proportional brownout (D-B3-4), wells, the 25-hour compact / spike / quiet-block
  policies (E1–E9).
- The street-first procedural city (D6; fixtures `packages/sim/fixtures/city{3,4,5}.json`
  are the regression set) with substations and streetlights placed by face geometry
  (D-B1-4; 3 in 8 streetlights start broken).
- The tile layer with the engineer on foot (D5): WASD, sprint, dodge, reach, pockets,
  the rifle; hand-mining and the workbench; the physical line on the HQ lot and the
  claims (Excavators, the placed Assembler on Shot / Wire / Frame / Board, one-lane belts,
  one inserter, turrets with hoppers); rubble at 300 a tile, the rail yard's coal heap
  (RI-01); Generators on coal; claims by the physical path — poles, a delivery, an
  Activate (RI-03); the
  Electricians' unlocks (Floodlight, Big pole, craftable Substation); crawlers, shades,
  hulks; the conservation ledger (`ledger.ts`).
- The hour-one bot (`E-hour`): no block lost on seeds 3 / 4 / 5 at 75 minutes, rifle off
  and on; north claimed at 65:00 is Held from 65:29–65:34 to 75:00 on every run; the rail
  yard's coal at the Depot at 27:02–27:04, 47.9 min before the hour ends; every item
  conserved (`E-hour-ledger`). `E-rifle` at
  tile scale: the rifle decides a rescue (48 runs) and the bot spends 0.50 / 0.69 / 0.06 %
  of the hour shooting, 0.00 % in danger.
- The game (`packages/game`): world view and map view, HUD corners, the telemetry panel,
  the current-goal line with its reason and support, machine status marks, stable block
  names with debug coordinates behind the toggle, save / load to a local slot and a
  download with a state hash (RI-02), the 3 h snapshot (`snapshot:check`), §18 and seed
  images (`freshness:check`).

**Stand-ins and approved-not-built changes** (each labelled where it sits in the doc):

- The §12 recipes with no machine (Shell, Concrete, Fuel, Polymer, the Mk2's 3 s Shot)
  are data (`recipes.ts`; the generated §13 machines table says which) — Shell at RI-10,
  the rest later. Wire, Frame and Board are made but have no hour-one consumer; the hour
  bot's Wire Assembler is placed and never fed (`made.wire` 0), the load stand-in §11's
  "wire recipe on a second Assembler" was.
- Machines are paid in rubble at placement (D-B2-1 (a)); the workbench recipe with a craft
  time — (b)'s other half — is RI-09 (T13). Slots are `floor(area / 600)` (D-B2-2,
  provisional).
- Outskirts deposits are placeholders (D-P4-3): a claimed face with no rubble gets no
  line and the hour bot notes it (the tripwire); no seed in three hits it.
- Stone has no sink: east's line digs 1,750 stone in the hour on seeds 3 and 5 and it
  sits in the chest (`E-hour-ledger`); a stone-consuming use is a new mechanic and a
  separate decision (`RI_PASS_1_REPORT.md` "RI-01").
- **Claims are physical** (RI-03, D-RI-2; §5 steps 1–4): the map previews and charges
  nothing; a Dark block next to Held is claimed by a pole run to its substation, the
  claim's steel and copper delivered there from the pockets (E) and an explicit Activate
  (E) within reach, paid once (`flow.ts` `deliverTo` / `activate`, one commissioning id an
  attempt). The legacy map claim (`sim.ts` `claim`) survives for the block-level bots, the
  compact snapshot and the labelled `E-hour-legacy` comparison. The field kit stands on
  Dark / Contested blocks next to Held on real pole power (`fieldPowered`); the craftable
  Substation costs a 50 steel + 25 Cu stand-in (D-B3-1) and is field kit on the Dark
  outskirts block before activation (D-CU-3 (b), provisional). *Stand-ins:* the benchmark
  runs economy off, so its delivery is zero there and 10 steel + 5 Cu in the game; the
  hour bot does not lay a claim's turrets (D-P4-9's other half, `DEFERRED.md`).
- **Save / load is a baseline, not a feature** (RI-02): one local slot (Ctrl+S / Ctrl+O),
  a download, a state hash and the command log so a load replays; no slot list, no
  autosave, no naming. (Before RI-02 this line said none existed — wrong: a download and a
  `?state=<url>` load had existed since Phase 4 without a hash, a validation or a log;
  corrected 2026-09-05.) No project record, emergence points, Stalker, Conductor or boss
  site exist (§28.4–28.8) — RI-04 … RI-06, RI-10, RI-12.
- Turrets are unharmed waypoints in the crawler chain (D-B4-3, provisional) while §22
  says they can be chewed; the barricade chain is Phase 6.
- The hour bot hand-feeds 69–150 magazines and walks 928–932 coal from the chest to the
  Generators in the 75-minute hour against the line's 669 (`E-hour-end`; the 60-minute
  hour's "790–860 magazines" was both counts together) while §19 says hand-feeding ends
  by minute 10 (D-P4-11, open; measured next by T12b).
- The hybrid expedition loop (D-GB-1) is decided and not built; its definition is
  `REVISED_DEVELOPMENT_PLAN.md` §4–§9 (§28) and its tasks are RI-03 / RI-05 / RI-06
  (D-RI-4).
- Not built at all: trams, the truck, barricades, cannons, undergrounds, splitters,
  filters, chests as placeable objects, the string table, undo / redo.

## Evidence

| kind | where | state |
|---|---|---|
| simulation | `docs/EXPERIMENTS.md`, `docs/experiments/*.json`, `calibration.md` | 13 experiments GREEN at RI-02 (2026-09-05, 267 s, 0 failing; `E-hour` 18/18 with the goal-line and save checks; stamped `9a47430` + RI-02's tree); every generated file fresh (`freshness:check`, 2026-09-05) |
| snapshot | `packages/game/public/snapshots/b-compact-seed3.json` | matches (compact seed 3 at 3:00:00, config 0176f61d); regenerated by RI-01 for five new stats counters, the state otherwise identical |
| human approval | `TEST_RESULTS.md` (Gate A), `GATE_B.md` (Gate B) | both passed by the owner alone; no outside tester has played |
| human play | `GATE_B.md` §19 table | minutes 0–10 only; 10–30 and 30–60 unrecorded; walking, first shade, burn-off, first pip unrecorded |
| waivers | `PROGRESS.md` T5, T6, T7, T12, T12a | T5–T7 waived by Daniel 2026-09-05 (T6 returns as T18, the walkthrough's four STANDARDS rows ride on T19); T12 and T12a waived on "Whole plan" into RI-01 / RI-03 and RI-02 |

## Counters

- Untagged numbers in the design doc: **33** (21 tile-scale, 12 design inputs), unchanged
  since Phase 4 M3; the cleanup, RI-01, RI-02 and RI-03 added none (RI-01's and RI-03's numbers
  carry `[sim: E-hour-…]` tags or a decision id).
- §26: **three systems, complexity 5 / 10**, a reported count since D-RI-3 (the stop
  condition never fired and no longer exists; rule 2's scope test admits features).
  Recounted at RI-03: still three (commissioning is §5's claim made physical, not a
  fourth system); next at RI-04 … RI-06.
- `STANDARDS.md`: **closed 1 of 46** (A.7, the hand-lamp sentence, at Gate B). Phase 5's
  fifteen build-with rows are listed on `PHASES.md` Phase 5.
- Engineer guards (shooting ≤ 10 %, danger ≤ 5 %, walking): every number is the bot's; no
  player number exists yet.

## Open items

- **Questions for the human** (`DECISIONS.md` "Outstanding questions"): D-GB-3 rifle
  range and D-GB-4 enemy density (gate conditions; RI-04 / RI-08 / RI-10 own them, no
  blocker); D-CU-2 the Chemist and Polymer (RI-11, not a blocker); D-P4-11 the idle steel
  and the hand-feed (RI-01 explained the hand-fed total as transfers the ledger balances
  and split it; T12b measures the share); a sink for stone (RI-01 surfaced it: a new
  mechanic, nobody's task yet); the hour bot laying a claim's turrets from stock
  (D-P4-9's other half: RI-03 built the field turret and left the benchmark's balance to
  the human, `DEFERRED.md`).
- **Provisional rows on the plan's defaults** (D-RI-6; sign or reverse by name, any time):
  D-GB-2 (a) the goal line (RI-02); D-CU-1 (a) production on any Held lot (kept at
  RI-03); D-CU-3 (b) the outskirts Substation before activation (built at RI-03).
- **Provisional rows a task will embody**: D-P4-5 (every assembler physical — built by
  RI-01, unsigned);
  D-B4-3 (the turret in the chain, RI-10 reconciles); D-SA-2 (the string table, T13 in
  RI-09); C3 / D-B2-2 (slots, T13 / T17); D-R1 (rifle vs shade). The full list is
  `DECISIONS.md`.
- **Human work in the plan**: T19, RI-08's played session of the representative loop,
  after RI-07. Claude prepares it and cannot mark it.
- **Recommended rows whose rule is built and in the doc but carry no name** (D-B1-4,
  D-B3-4, D-B3-1, D5, D6, D-B1-5, D-LP-1…3 among 31): provenance gaps, not open questions.
- **Unscheduled obligations**: `DEFERRED.md` (the reference-machine soak by hand, the
  stranger test, the calibration pip bands, the soak script's home).

## Standing rules for every phase

The `STANDARDS.md` rows for the phase are checked at its exit, one minute each by a
human; no new gap without a row. The phase report and the full verification are the exit
(constitution rule 9 and "Verification policy").
