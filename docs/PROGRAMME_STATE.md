# Relight — programme state

The two-page handoff. **Replaced, never appended**: this file says where the programme is
now. The per-milestone history lives in the phase reports (`PHASE_0_REPORT.md` …
`PHASE_5_REPORT.md`, `SLICE_REPORT.md`, `GATE_B.md`) and in the pre-cleanup copy
`archive/pre-phase-5-cleanup/PROGRAMME_STATE-2026-09-05.md`. Rules: `CONSTITUTION.md`.
Tasks: `PROGRESS.md`. Decisions: `DECISIONS.md`.

## Now (2026-09-05, after RI-06 wrote the Junction Heart — code only, unverified)

- **RI-06's code is written and NOT verified.** On the user's instruction ("don't worry
  about running tests or simulations, just write the code") RI-06 ran no test, experiment,
  snapshot or browser check: `tsc` and eslint green is all. `E-heart` (15th experiment)
  and `heart.test.ts` (six tests) exist unrun; `docs/EXPERIMENTS.md` and the `CLAUDE.md`
  counters still describe RI-05's runs. The next verification pass runs `npm test`,
  `npm run experiments`, `snapshot:check`, `freshness:check` and a browser check with
  `?heart=1`, and fixes what they find before RI-06's evidence is claimed.
- **The revised development plan is in execution: RI-00 … RI-06 are done (RI-06 unverified).**
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
  and no fall on every seed. RI-04 built enemy origins, Crawler / Shade clarity and the
  Stalker prototype ("RI-04"): emergence points, one per block and shared street on the
  Dark block's frontage kerb, ids from the geometry, births along the kerb and never
  inside a Held block; a crawler's heading and target tile and a shade's trace readable
  in the sim and the world view; the Stalker candidate (`guard → investigate → pursue →
  attack → return`, one per Dark well block, §7.1's numbers in `candidates.ts`) behind
  `enableStalkers` / `?stalker=1`, outside the benchmark's configuration (D-RI-5).
  `E-rifle` 16/16 with `E-rifle-cand-stalker` beside the benchmark rows (0 contacts on
  the hour — the bot never nears a well block); `E-hour` 19/19; 145 tests. RI-05 built
  the neighbourhood project framework and the rail-yard reward ("RI-05"): the project
  record (`project.ts`) with its stage derived from the site's real state — the block
  record alone owns territory; the rail yard as the first project (its claim's materials
  by hand or off a belt into its substation, the Activate its commissioning, restored
  when Held with the attempt id) and its reward the transport kit — Track, Tram stop,
  Tram — unlocked at the restoration's second and never before; one minimal route (one
  line of track, two stops, one tram, paid from the Depot's stock) carrying the yard's
  coal to the Depot and the local supply depot's materials back; the depot a named Supply
  chest commissioned within reach, changing the record alone. `E-project` 10/10:
  restored 25:40–25:46, the coal at the Depot by tram 27:05–27:15, the depot restored
  32:15–32:18, 732 items by tram, the replay and a 40:00 save to the same hash on every
  seed, 4 Held and no fall. The route sits beside the benchmark, never in it: `E-hour`
  19/19 with the belt line unchanged (only the state hash moved — the state now carries
  the records). 148 tests.
- **RI-06 built the Junction Heart as a candidate layer** (`RI_PASS_1_REPORT.md` "RI-06"):
  `heart.ts` + `CANDIDATES.heart`, switched on by `enableHeart` / `?heart=1` (D-RI-5 —
  beside the benchmark, config hash unchanged): two feeder cabinets on the yard's lot,
  the Activate as the Start, 90 s productive / 60 s stall, Crawler packets keyed to
  attempt and threshold, knock-out and repair, interruption preserving the deliveries and
  the charge, the destruction and the once-only reward through the project record; the
  hour bot's Heart step and patrol, `E-heart`, `heart.test.ts`, the game's cabinets,
  ring, packet approach, E / X and toasts. Every number an implementation default.
- **Next task: RI-07 — integrate the opening candidate** (plan §11.1; T12c feeds it): one
  opening profile with real resources and commands in a candidate configuration separate
  from the 75-minute benchmark; automation → route choice → small enclosure → rail
  project → reward as one sequence. **Ready** in list order (blocked by RI-06, done —
  unverified; a red `E-heart` or `heart.test.ts` in the verification pass is a check red
  for a reason inside RI-06 and is fixed there first). Branch `ri-pass-1` on top of
  `phase-4`. The order after it: RI-08 (whose played session is **T19**, human) → RI-09
  (T13 → T17, T18) → RI-10 → RI-11 → RI-12 → RI-13; T12b and T12c are runnable behind
  RI-06 in list order.
- **Phase 5 — the factory, complete — is open; RI-01 built its first pieces** (the
  per-machine recipe, coal rubble, the ledger), RI-05 its transport's first piece (the
  minimal tram route and the Supply chest) and the rest is not built. The plan
  interleaves the phases (`PHASES.md` top note): the hybrid of D-GB-1 moved from Phase 6
  into RI-03 / RI-05 / RI-06 (D-RI-4); the three-systems stop became rule 2's scope test
  (D-RI-3); D-GB-2, D-CU-1 and D-CU-3 are provisional on the plan's defaults (D-RI-6).
- Phases 0–4 are complete. **Gate A** (2026-09-03) was an owner-only `go` on the bot
  calibration and the smoke test. **Gate B** (2026-09-05, `GATE_B.md`) is `proceed — with
  conditions`: one hour played by the owner, only minutes 0–10 recorded, no telemetry
  read; the conditions are D-GB-2 (provisional (a), built at RI-02, T19 tests it), D-GB-3 (rifle range,
  RI-08's candidate) and D-GB-4 (enemy density, still open after RI-04 built the Stalker candidate; RI-10) — none blocks RI-01.
- Branch `ri-pass-1` on `phase-4`; PR #5 (`phase-4`) open, #1–#4 merged (checked read-only on
  2026-09-05). Nothing
  is pushed by a task unless the human asks.

## What is built, what is a stand-in

**Implemented and measured** (`docs/EXPERIMENTS.md`, 14 experiments GREEN on 2026-09-05
at RI-05, stamped `24bc007` + RI-05's tree, config b95922d2; `SLICE_REPORT.md`):

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
  hulks, born on emergence points (one per block and shared street, on the Dark block's
  frontage kerb, never inside a Held block) with a readable heading, target tile and
  shade trace (RI-04); the conservation ledger (`ledger.ts`).
- Neighbourhood projects and the transport kit (RI-05): the project record with its
  stages derived from the site (`project.ts`); the rail yard as the first project, its
  claim's materials by hand or off a belt, restored by the Activate, its reward the kit —
  Track (streets only, walkable), the Tram stop (two 200-item pools, 20 kW), the Tram
  (200 items, 8 t/s, one line between two stops, 4 s dwell) — unlocked at the
  restoration and never before; the Supply chest (2×2, 200 items, field kit) and the
  local supply depot commissioned on it; belts ending in a chest or a stop, inserters
  taking from them (`E-project`: 732 items by tram, the replay and a 40:00 save to the
  same hash on seeds 3 / 4 / 5).
- The hour-one bot (`E-hour`): no block lost on seeds 3 / 4 / 5 at 75 minutes, rifle off
  and on; north claimed at 65:00 is Held from 65:29–65:34 to 75:00 on every run; the rail
  yard's coal at the Depot at 27:02–27:04, 47.9 min before the hour ends; every item
  conserved (`E-hour-ledger`); the same bot on the tram route beside it (`E-project`): the
  yard's coal at the Depot at 27:05–27:15, the depot restored 32:15–32:18, 4 Held, no
  fall. `E-rifle` at
  tile scale: the rifle decides a rescue (48 runs) and the bot spends 0.19 / 0.25 / 0.17 %
  of the hour shooting, 0.00 / 0.58 / 0.00 % in danger (RI-04's run).
- The game (`packages/game`): world view and map view, HUD corners, the telemetry panel,
  the current-goal line with its reason and support, machine status marks, stable block
  names with debug coordinates behind the toggle, save / load to a local slot and a
  download with a state hash (RI-02), the Supply chest / Track / Tram stop / Tram drawn
  and placed (keys C / L / H / V), E on a chest or a stop for its pockets, the Projects
  list and the project toasts (RI-05), the 3 h snapshot (`snapshot:check`), §18 and seed
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
  corrected 2026-09-05.) No Conductor or boss site exists (§28.7, §28.8) — RI-06, RI-10,
  RI-12; the project record exists (RI-05, §28.4) with the catalogue's first two entries,
  the rest RI-11.
- **The transport kit is the minimal route** (RI-05; §13, §14, §28.4; D-RI-6): a 2×2 Tram
  stop with two 200-item pools and an instant transfer at a powered stop in place of
  §13's 2×3 with six inserters; a one-tile Tram; the reward grants §13's Tram depot /
  Rail crew kit (those rows stay the design); the depot's stock 20 coal + 10 magazines
  and its 'hands out kits' function; the prices (a stop 10 steel, the tram 20 steel + 5
  Cu, track 1 steel a tile, the chest 10 steel) are GAME-ASSUMPTIONS (§13 prices none).
  The route sits beside the benchmark (`E-project`), and on it the two stops' 40 kW brown
  the hour out from 26:43 until Generator 4 at 45:00 where the belt route never does, and
  the tram's copper leaves 1 Cu on seed 3 — reported for the human, not tuned. The
  Freight tram, the truck and the Rail crew are unbuilt; the rest of T16 is RI-09.
- **The Stalker is a candidate, not a benchmark enemy** (RI-04; §28.6, D-RI-5): §7.1's
  numbers sit in `packages/sim/src/candidates.ts` outside `SimConfig`, switched on by
  `enableStalkers` (the game's `?stalker=1`) and reported as `E-rifle-cand-stalker`
  beside the benchmark rows; without the switch no Stalker exists. Its promotion is a
  decided row. The hour bot never nears a well block, so the candidate's contact numbers
  on the hour are zero by geography, not evidence. The emergence points (§28.5) are the
  benchmark's: every crawler and shade is born on one. The Conductor and the Breaker
  display name are RI-10.
- Turrets are unharmed waypoints in the crawler chain (D-B4-3, provisional) while §22
  says they can be chewed; the barricade chain is Phase 6.
- The hour bot hand-feeds 69–150 magazines and walks 928–932 coal from the chest to the
  Generators in the 75-minute hour against the line's 669 (`E-hour-end`; the 60-minute
  hour's "790–860 magazines" was both counts together) while §19 says hand-feeding ends
  by minute 10 (D-P4-11, open; measured next by T12b).
- The hybrid expedition loop (D-GB-1) is decided and not built; its definition is
  `REVISED_DEVELOPMENT_PLAN.md` §4–§9 (§28) and its tasks are RI-03 / RI-05 / RI-06
  (D-RI-4).
- Not built at all: the Freight tram, the truck, barricades, cannons, undergrounds,
  splitters, filters, the string table, undo / redo.

## Evidence

| kind | where | state |
|---|---|---|
| simulation | `docs/EXPERIMENTS.md`, `docs/experiments/*.json`, `calibration.md` | 14 experiments GREEN at RI-05 (2026-09-05, 369 s, 0 failing; `E-project` 10/10, `E-hour` 19/19, `E-rifle` 16/16 with the candidate rows; stamped `24bc007` + RI-05's tree); every generated file fresh (`freshness:check`, 2026-09-05) |
| snapshot | `packages/game/public/snapshots/b-compact-seed3.json` | matches (compact seed 3 at 3:00:00, config 0176f61d); regenerated by RI-01 for five new stats counters, the state otherwise identical |
| human approval | `TEST_RESULTS.md` (Gate A), `GATE_B.md` (Gate B) | both passed by the owner alone; no outside tester has played |
| human play | `GATE_B.md` §19 table | minutes 0–10 only; 10–30 and 30–60 unrecorded; walking, first shade, burn-off, first pip unrecorded |
| waivers | `PROGRESS.md` T5, T6, T7, T12, T12a | T5–T7 waived by Daniel 2026-09-05 (T6 returns as T18, the walkthrough's four STANDARDS rows ride on T19); T12 and T12a waived on "Whole plan" into RI-01 / RI-03 and RI-02 |

## Counters

- Untagged numbers in the design doc: **33** (21 tile-scale, 12 design inputs), unchanged
  since Phase 4 M3; the cleanup and RI-01 … RI-05 added none (RI-01's and RI-03's numbers
  carry `[sim: E-hour-…]` tags or a decision id; RI-04's are candidates in
  `candidates.ts` and named as such in §7, not written into the doc; RI-05's as-built
  sizes, caps and prices are labelled implementation defaults under D-RI-6 and its
  measured moments carry `[sim: E-project-…]`).
- §26: **three systems, complexity 5 / 10**, a reported count since D-RI-3 (the stop
  condition never fired and no longer exists; rule 2's scope test admits features).
  Recounted at RI-04: still three (commissioning is §5's claim made physical; the
  Stalker is §7's roster and the emergence points are the threat system's, not a fourth
  system). Recounted at RI-05: still three (a project is §5's claim given a purpose and a
  reward; the tram is §14's one bulk system, in the count since Phase 0; the Supply chest
  is the Depot's chest placed). Recounted at RI-06: still three (the Heart is §5's claim
  under §7's threat rules with two more delivery targets — no fourth system); next at RI-07.
- `STANDARDS.md`: **closed 1 of 46** (A.7, the hand-lamp sentence, at Gate B). Phase 5's
  fifteen build-with rows are listed on `PHASES.md` Phase 5.
- Engineer guards (shooting ≤ 10 %, danger ≤ 5 %, walking): every number is the bot's; no
  player number exists yet.

## Open items

- **Questions for the human** (`DECISIONS.md` "Outstanding questions"): D-GB-3 rifle
  range and D-GB-4 enemy density (gate conditions; RI-08 / RI-10 own them — RI-04 built
  the Stalker candidate and chose no density number, no blocker); D-CU-2 the Chemist and Polymer (RI-11, not a blocker); D-P4-11 the idle steel
  and the hand-feed (RI-01 explained the hand-fed total as transfers the ledger balances
  and split it; T12b measures the share); a sink for stone (RI-01 surfaced it: a new
  mechanic, nobody's task yet); the hour bot laying a claim's turrets from stock
  (D-P4-9's other half: RI-03 built the field turret and left the benchmark's balance to
  the human, `DEFERRED.md`); the Stalker candidate's promotion into the benchmark
  (D-RI-5, a decided row — `E-rifle-cand-stalker` measured no contact on the hour,
  geography, not evidence); whether `E-rifle`'s tile-rescue stance should leave the
  street midpoint now that the emergence mouth crosses it (RI-04 kept the legacy
  configuration, plan §14, and reported seed 5's thinner margins); the tram route's cost
  on the hour (RI-05: the two Tram stops' 20 kW brown the hour out from 26:43 to Generator
  4 where the belt route never does, and the Tram's copper leaves 1 Cu on seed 3 — an
  earlier Generator 4, a stop drawing only while transferring, or a price; no row yet);
  whether the tram route ever joins the benchmark (D-RI-5, a decided row).
- **Provisional rows on the plan's defaults** (D-RI-6; sign or reverse by name, any time):
  D-GB-2 (a) the goal line (RI-02); D-CU-1 (a) production on any Held lot (kept at
  RI-03); D-CU-3 (b) the outskirts Substation before activation (built at RI-03); the
  Stalker's §7.1 state machine (built at RI-04 as the candidate, `stalker.ts`); RI-05's
  transport-kit sizes, caps and prices and the depot's stock (`RI_PASS_1_REPORT.md`
  "RI-05"); RI-06's Heart geometry, cabinet recipe, packet roster / counts / cap /
  approach delay and the knock-out and repair rules (`RI_PASS_1_REPORT.md` "RI-06",
  unverified).
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
  stranger test, the calibration pip bands, the soak script's home, a readable refusal
  for an out-of-reach hand command, the Tram stop's 20 kW on the hour).

## Standing rules for every phase

The `STANDARDS.md` rows for the phase are checked at its exit, one minute each by a
human; no new gap without a row. The phase report and the full verification are the exit
(constitution rule 9 and "Verification policy").
