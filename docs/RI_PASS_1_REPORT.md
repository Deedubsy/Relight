# RI pass 1 — report

The evidence file for `PROGRESS.md` RI-00 … RI-13, one section per task, written by the
task that closes it. History and evidence: never rewritten; a correction is a new line
saying so. The plan the tasks come from is `REVISED_DEVELOPMENT_PLAN.md` (adopted
2026-09-05, `DECISIONS.md` D-RI-1).

## RI-00 — Adopt the revised development plan and reconcile the repo (2026-09-05)

**Authorisation.** Daniel pasted "Revised development plan, Version 1.0 · 2026-09-05" as a
chat message and, asked for the execution scope, replied **"Whole plan"** (D-RI-1). No
earlier message approved any of its defaults; the plan's §1 forbids attributing them to a
previous gate, and none is.

**What the repository was (inspected before any edit).** HEAD `d778018` on `phase-4`
(the pre-Phase-5 cleanup, T11a); T12 `todo` and nothing of Phase 5 built; the file the
plan says it replaces (`RELIGHT_FUN_IMPROVEMENT_PLAN.md`) never existed in this
repository — no file, no reference, no history. Claims are made from the map and paid
from the Depot chest (`packages/sim/src/sim.ts` `claim`, `flow.ts` laying the poles on
the claim event); no save / load path exists in `packages/game`; the enemies are
crawler, shade and hulk. Commands verified at T11a the same day: `npm test` 121 pass,
typecheck, lint, docsync, freshness, snapshot all green.

**Built.** Nothing. Doc-only; no file under `packages/` changed; no gameplay number,
fixture, stamp or expected result moved.

**Reconciled (the one live task order).**

- `PROGRESS.md`: RI-00 … RI-13 inserted in dependency order; **T12 waived** into RI-01
  (economy, recipes, rubble, coal, the 75-minute hour, the docsync check) and RI-03
  (every front edge physical); **T12a waived** into RI-02 (the goal line); T12b and T12c
  re-blocked on RI-01 (T12b rerun after RI-03 and RI-07); **T19 revised** into RI-08's
  played session (its original 0–20-minute text kept inside the row) and re-blocked on
  RI-07; T13 re-blocked on RI-08 and its field-deployment piece moved to RI-03; T14's
  "every chain" narrowed to the chains whose machines exist; T16's minimal route moved to
  RI-05; RI-09 = T13 → T17, then T18. The mapping is recorded once, there.
- `DECISIONS.md`: D-RI-1 (adoption, decided), D-RI-2 (one claim path — physical
  commissioning, decided), D-RI-3 (the three-systems stop → rule 2's scope test,
  decided), D-RI-4 (the hybrid moved from Phase 6 into RI-03 / RI-05 / RI-06, decided;
  supersedes D-GB-1-rider), D-RI-5 (candidate configurations stay out of the benchmark,
  decided), D-RI-6 (the plan's implementation defaults, provisional). D-GB-2 → provisional
  (a), D-CU-1 → provisional (a), D-CU-3 → provisional (b); D-GB-3, D-GB-4, D-B4-3,
  D-P4-5, D-P4-9, D-P4-11, D-HOUR-3 annotated with the task that now owns them. Counts:
  37 decided, 37 provisional, 32 recommended, 2 open (gate condition), 2 superseded — 110.
- `CONSTITUTION.md`: Scope paragraph and rule 2 carry the scope test; rule 10 drops "a
  §26 recount reaches four" for "a mechanic or benchmark change the adopted plan does not
  name"; rule 8 names the goal line as its form; the verification policy gains the
  candidate-configuration sentence (D-RI-5). Rule numbers unchanged.
- `PHASES.md`: a top note on the interleaving; Phase 5 and 6–9 and 14 carry "moved
  forward → RI-xx" lines; Phase 6's entry criterion is met by the plan's §4–§9 and the
  hybrid is no longer built there.
- `RELIGHT-design.md`: new **§28** (the plan's rules as approved-not-built with the
  plan's labels); labelled notes in §5, §7, §10, §11, §23, §26; one changelog line. No
  guarded sentence and no generated block touched.
- `CLAUDE.md`: read order and ownership row for the plan; the §26 stop removed from
  "Stop and ask"; the cleanup-session paragraph deleted; branch `ri-pass-1` opened by
  RI-01; the commands table's last-run column.
- `PROGRAMME_STATE.md` rewritten ("Now", stand-ins, evidence, counters, open items);
  `DEFERRED.md` links repointed (RI-01, RI-03, RI-09, T19, RI-03 / RI-05 / RI-06);
  `README.md` lists the plan; `REVISED_DEVELOPMENT_PLAN.md` created with a provenance
  preface above the verbatim plan.

**Verified (commands actually run, 2026-09-05, after the edits).**

| command | result |
|---|---|
| `npm run docsync:check` | green — "doc tables match packages/sim" |
| `npm run freshness:check` | green — every generated file fresh at HEAD |
| `npm run snapshot:check` | green — compact seed 3 at 3:00:00 matches (config 0176f61d) |
| link check over the eleven touched docs (every back-ticked `.md` / `.ts` path) | green — every path resolves under `docs/`, `docs/archive/pre-phase-5-cleanup/` or `packages/sim/src/`; the only unresolved name is `RELIGHT_FUN_IMPROVEMENT_PLAN.md`, named on purpose as never having existed |

`npm test`, typecheck, lint and the experiments were not run: no code changed (last run
T11a, the same day, green).

**Not decided by this task (for the human, no blocker).** The plan's defaults are
provisional and reversible by name (D-RI-6). The Gate B conditions D-GB-3 and D-GB-4
stay open. T19 is the plan's human work; Claude prepares it in RI-08 and cannot mark it.

**Next.** RI-01, ready; branch `ri-pass-1` on top of `phase-4`.

## RI-01 — Establish the real opening economy and resource accounting (2026-09-05)

**Authorisation.** The plan's RI-01 row and `PROGRESS.md`'s, executed on "Whole plan"
(D-RI-1, recorded at RI-00). RI-00 was its only blocker. Every default this task chose
is named below as an implementation default (D-RI-6): reversible, not a historical
approval, and no earlier gate is credited with any of them.

**What the repository was (inspected before any edit).** HEAD `5f49691` on `phase-4`
(RI-00, doc only); branch `ri-pass-1` opened on it. The eight non-Shot recipes were data
(`recipes.ts`) and no machine made them; the block-level "Build assembler" and the HQ
lot's stand-in Excavators stood beside the physical line (D-P4-5); a claim's copper or
coal arrived in the chest as the block sim's flat yield; a district tile held its block's
pool over its tile count (257–360 units); no coal rubble existed and west's claim made
nothing (GA-B6-3); `constants.ts` `HOUR` ran 60 minutes with north's claim past it; no
conservation check existed; the hand-feed count was one number for magazines and coal.
121 tests, every check green (T11a and RI-00, the same day).

**Built (code, `packages/sim`, `packages/harness`, `packages/tools`, `packages/game`).**

- **Recipes by real machines** (D-B2-1 (b), the recipe half): one placed Assembler kind
  runs Shot, Wire, Frame or Board, chosen per machine (`flow.ts` `ASSEMBLER_RECIPES`,
  `setRecipe`; the `setRecipe` command; T on the machine in the world view), each priced
  by `recipes.ts`; the intermediates travel on belts, in pockets and in the Depot's store
  (`Item`, `StoreItem`, `STACK`). The recipes whose machine does not exist (Shell,
  Concrete, Fuel, Polymer, the Mk2's 3 s Shot) stay data, and the new generated §13
  machines table (`docsync:machines`) says which. The workbench craft time — (b)'s other
  half — is not built (RI-09, T13); machines are still paid in rubble at placement.
- **Rubble as finite typed ore** (D-P4-2): a district tile holds 300 units
  (`RUBBLE_UNITS_PER_TILE`); a tile's variant thins with what is left and the block's
  pool drops a tile's share when a tile is dug out, so the map's strip and the tiles
  agree. A fourth rubble kind, coal. Stone has no sink (below).
- **The rail yard's coal** (D-P4-12, C5): the rail yard is the HQ's most westward
  claimable neighbour (`ground.ts` `railYardOf`, GAME-ASSUMPTION: the generator has no
  rail-yard district) and its rubble is one 3×3 heap of 78 a tile = 702 units
  (`tiles.ts` `RAIL_YARD_HEAP`, `RAIL_YARD_COAL`), the HQ coal patch's shape and yield,
  nothing else standing on the block. This overrides §12's untagged "~30k a block" as
  D-P4-12 decided. The block-level flat yield is gone on a city with the tile layer:
  every unit is dug from a tile (`sim.ts`, D-P4-2 / D-P4-5).
- **Outskirts deposits** (D-P4-3): the placeholder half. The 160-tile placeholder stays
  inert; the tripwire is the hour bot's note and end-state adjustment when a claimed
  face has no rubble to dig (`hour.ts` `blockLine`, `HourReport.endWant`). No seed in
  three hits it in the 75-minute hour.
- **The 75-minute hour** (D-HOUR-3 (a)): `constants.ts` `HOUR_MINUTES` = 75, `HOUR_S`;
  the minute table's north row (65:00) is inside the run and §11's table regenerated;
  `E-hour` runs to 75:00 and scores north Held (`E-hour-north`, folded from the main
  runs). `firsthour.ts` keeps its 60-minute legacy configuration, labelled.
- **Every Assembler physical** (D-P4-5): `sim.ts` `addAssembler` is refused under the
  tile layer (`assembler-rejected` event) and the panel's button hidden; the HQ lot's
  stand-in Excavators removed; the claims' lines are real — east's Excavator on its own
  district rubble (stone on seeds 3 and 5, copper on seed 4) and the rail yard's on the
  coal heap, each belted into the Depot along routes the bot finds on placeable tiles
  (`hour.ts` `beltRoutes`, `HOUR_CORRIDOR`; on seed 5 a start turret standing in the
  corridor is picked up and put down beside the run, by player commands). The §11
  script's "wire recipe on a second Assembler" is a real machine on the Wire recipe,
  placed and never fed: a load stand-in, `made.wire` 0.
- **The conservation ledger** (`ledger.ts`, exported from the sim): opening stock +
  sources (units mined, recipe output) = held (chest, buffer, ring, pockets, machines,
  belts) + sinks (recipe inputs, machine and claim prices, repairs, coal burned, rounds
  fired or lost), per item, tolerance 0.01; transfers — belts, the chest, hand-feeds,
  the ring's draw — on neither side. The opening stock is recorded when the flow layer
  is created (`FlowState.ledger`), or when a pre-RI-01 save is upgraded. Three places
  rounds left the game uncounted are now `stats.roundsLost`: a removed turret's loose
  rounds over a full buffer, a stand-in edge's ring-drawn rounds over a full buffer when
  its first turret arrives (`hookSyncEdges`), and the block tick's buffer cap. Three
  tests (`ledger.test.ts`): a 20-minute hour balances and a planted 5 steel is caught;
  chest takes / puts, a craft, place / remove / re-place, a recipe change, hand-mining
  and a hand-feed all balance; a turret removed over a full buffer counts its 3 loose
  rounds lost.
- **The hand-feed count split** (D-P4-11): `handFedMags` (magazines into turret
  hoppers) and `handFedCoal` (coal into Generators) replace the one `handFed`; both are
  transfers the ledger balances. `hourReport` carries both.
- **The `[play: <gate>]` docsync check**: every play tag names a gate in `PLAY_GATES`
  (Gate A → `TEST_RESULTS.md`, Gate B → `GATE_B.md`) whose record exists; a placeholder
  or an unknown gate is red. The doc has no placeholder; both records exist.
- **The renderer**: T sets the recipe under the cursor; an Assembler draws its recipe's
  inputs, output and progress in the items' colours; coal rubble frames and the item
  colours for the intermediates; the hotbar line (`TOOL_KEY_LINE`); the block-level
  Assembler row hidden under the tile layer.

**Implementation defaults (reversible, D-RI-6).** The rail-yard heap 3×3 at 78 a tile
(the HQ coal patch's density, so one Excavator reaches every tile); the rail yard
designated by geometry; the intermediates stacking 50; the stand-in edge's ring rounds
refunded to the line buffer when its first turret arrives (before RI-01 they were
dropped); `roundsLost` as a counter, never a rule; the belt corridor and the seed-5
turret move as bot behaviour.

**Measured (`E-hour`, 75 minutes, seeds 3 / 4 / 5, rifle off and on; `docs/EXPERIMENTS.md`
E-hour, 13/13 checks, 40.4 s).** East's line reaches the Depot at 16:46–16:51 and the
rail yard's at 26:39–26:44; the first rail coal unit is at the Depot at 27:02–27:04
(the mark reads the delivered-coal count passing the hand-dug count, so it is a few
seconds late), 702 units dug per seed; the chest's coal never runs out (472–476 at
75:00), the Generators are never dry and no run browns out, so the D-P4-12 margin is
47.9–48.0 minutes to the run's end against the 10-minute bar. The first red pip is at
5:33–6:18 with 44–60 crawlers spawned and 0 arrivals at a Held edge by then. North,
claimed at 65:00, is Held at 65:29–65:34 and stands to 75:00 on all six runs; no block
falls; the end state is 4 Generators, 7 Excavators, 3 Assemblers, 6 turrets, 4 Held on
every run. Steel's minimum is 23 at 17:35 on seed 3 (30 at 12:01–12:02 on seeds 4 and 5;
east's line at ~16:50 is the extra draw); the chest at 75:00 holds 1,601–1,643 steel,
397 copper (2,098 on seed 4, whose east is copper), 472–476 coal and 121–283 magazines.
The line makes 669 magazines; the hands carry 69–150 magazines to hoppers and 928–932
coal to the Generators (the 60-minute hour's "790–860 magazines an hour" was both counts
together — a label corrected in §19 and §25 item 22, not a change of behaviour). Every
item is conserved on every run, `roundsLost` 0, worst unexplained 0.000. Walking is
2.7–4.1 % of the hour; the rifle run is "held anyway" on every seed. Findings the run
still reports: the Electricians never walk into the Depot, the first shade never comes
(§11 / E3: 32–47), and on seed 3 the HQ never goes interior (it has four land
neighbours).

**Verified (commands actually run, 2026-09-05, after the code and before the records).**

| command | result |
|---|---|
| `npm test` | 124 pass, 0 fail (121 + `ledger.test.ts`'s 3) |
| `npm run typecheck` | green, including the Vite game build |
| `npm run lint` | green |
| `npm run docsync:check` | green — "every [play: <gate>] tag names a recorded gate (Gate A, Gate B)"; "doc tables match packages/sim" (re-run after the record edits below, see the closing table) |
| `npm run experiments` | 13 experiments, 244 s, **0 failing checks**; `E-hour` 13/13 at 75:00; `E-rifle` 14/14, `E-variance` 4/4, `E-walk` 3/3, E1–E9 as before |
| `npm run freshness:check` | green — every generated file made on an ancestor of HEAD with the current config (the experiments and the snapshot are stamped `5f49691`, RI-01's parent, because they ran on the working tree before this commit) |
| `npm run snapshot:check` | red once the block stats gained five counters, then **regenerated with `npm run snapshot`** and green |

**Fixtures, expected results and generated files.** `packages/sim/fixtures/city{3,4,5}.json`
unchanged. `b-compact-seed3.json` regenerated: the only bytes that differ are the stamp
and five new stats keys (`ringDraw`, `ringFired`, `spentSteel`, `spentCopper`,
`roundsLost`); held / front / interior / lost / claims / assemblers are identical and
the config hash (0176f61d) unchanged — **not a rules change**, reported so the human
can check it. `tiles.test.ts` now expects 300 units a district tile and the rail yard's
coal heap; `hour.test.ts` expects north's claim inside the 75-minute hour — the
expectation changes of decided rows (D-P4-2, D-P4-12, D-HOUR-3), reported here as rules
changes, none made to turn a check green. `docs/EXPERIMENTS.md` and
`docs/experiments/*.json` regenerated by the run.

**Not built, and what the run says.** No sink for stone: east's line digs 1,750 stone on
seeds 3 and 5 and it sits in the chest (`E-hour-ledger` stone 50 + 1,750 → 1,800 + 0); a
stone-consuming use is a new mechanic and a separate decision. Wire, Frame and Board have
no hour-one consumer. The workbench machine craft (D-B2-1 (b)'s craft time) is RI-09's.
The rail-arrival mark's few seconds of lag is a measurement limit, not a rule.

**Not decided by this task (for the human, no blocker).** A stone sink (new mechanic).
D-P4-5 is built and stays provisional until signed. D-P4-11's (c) is still the standing
recommendation; T12b measures the hand-feed share per variant. The implementation
defaults above are reversible by name.

**Next.** RI-02, ready; T12b and T12c are runnable behind it in list order.

## RI-02 — Opening guidance and essential presentation (2026-09-05)

**Authorisation.** The plan's RI-02 row and `PROGRESS.md`'s, executed on "Whole plan"
(D-RI-1, recorded at RI-00). RI-01 was its only blocker. Constitution rule 8 is kept: the
goal is one HUD line read off the state — no list, no screens, no quest state. Every
default this task chose is named below as an implementation default (D-RI-6): reversible,
not a historical approval, and no earlier gate is credited with any of them.

**What the repository was (inspected before any edit).** HEAD `9a47430` on `ri-pass-1`
(RI-01). The plan's line "verify existing support before adding it" was applied first.
*Save / load*: a path existed — the panel's "Save snapshot" button downloaded the raw
state as JSON and `?state=<url>` loaded a JSON file (a raw state or a telemetry export's
`finalState`) into a scenario-B session with no command log, so a loaded session could
not replay; no hash, no validation, no slot; the turret cooldown lived in a `WeakMap`
outside the state, so a load reset every turret to ready. `PROGRAMME_STATE.md`'s "No
save / load exists (checked 2026-09-05)" and §28.10's "no save path exists on 2026-09-05"
were both wrong and are corrected in this commit with a line saying so. *Guidance*: no
goal line (T12a never started); the §11 beats' toasts existed (`main.ts` `describe`) and
named blocks by coordinates. *Presentation*: the engineer, the hotbar line, the ghost
with its reason ("walk closer", the price, the refusal), the "No <kind> here: <reason>"
and "No pick-up: <reason>" toasts, the hand-feed and pick-up toasts, the turret flash
and the E toast (`describeMachine`) existed; no running / starved / blocked word; blocks
were coordinates everywhere ("block (421,634)"), always shown; one fixed layout (panel
420 px, no breakpoints). 124 tests, every check green (RI-01, the same day).

**Built (code, `packages/sim`, `packages/harness`, `packages/game`).**

- **The current-goal line** (D-GB-2 (a), plan §11.2, rule 8's form): `goal.ts`
  `currentGoal(st)` → `{ goal, support }`, a pure function of the state — the first §11
  row the state has not met, in schedule order (`GOAL_ORDER`: hq-fell, front, mine,
  craft, coal-line, gen-2, steel-line, copper-line, shot-line, steel-2, gen-3, then
  claim / contest / retake for east, west and north with rail-coal, gen-4, copper-2 and
  assembler-3 between them, then hold), each with a `why` carrying the state's numbers
  (steel in the pockets, coal minutes left, the claim's price against the chest, the
  magazines in the chest against the front's draw). A second, amber **support** line
  names an immediate shortage — down, every Generator dry, a street's kit empty, a
  hopper to hand-feed, a brownout — under the goal without replacing it (§11.2). Drawn
  as `#goal` over the canvas (`index.html`, `style.css`), refreshed once a sim second
  (`panel.ts` `updateGoal`); the world HUD's top corners sit under it (`view.ts`
  `hudInset`). No completion state exists: a save reloads the state and the line follows.
- **Machine states**: `goal.ts` `machineStatus(st, m)` → running / starved / blocked /
  idle / off with a reason (no power, no rubble in reach, output full, waiting for
  inputs, out of coal, no target, …). The world view draws it as a shape on the machine
  (`worldScene.ts` `drawStatusMark`: ▶ running, ○ starved, ■ blocked, – idle, ✕ off — a
  shape and a word, never colour alone), in the hover tooltip and in the E toast.
- **Stable names** (`names.ts`): the HQ is "HQ"; the rail yard "<bearing> rail yard";
  every other block its compass bearing from the HQ plus its district word, with the
  hop count from two hops out ("east residential", "north-west civic 2"); unique per
  city (" A" / " B" on a collision) and state-free — a claim, a fall or digging never
  renames. `blockLabel(st, i, debug)` adds the coordinates only with the debug toggle
  (` \` `, `debugView.coords`); `edgeName` names the ring's rows; `streetName` gives
  "HQ's east street". Every toast, tooltip, ring row and facility row uses them.
- **The save / load baseline** (`save.ts`): `stateHash` = FNV-1a over the canonical JSON
  of the state minus the transients (`events`, `acc`, `speed`); `makeSave` → a versioned
  `SaveFile` (state, command log, `logComplete`, params, hash); `loadState` validates and
  deep-copies (a pre-RI-02 raw state and a telemetry export still load). In the game:
  **Ctrl+S / Save** writes slot 1 to `localStorage` (`relight.save.1`) and toasts the
  hash; **Ctrl+O / Load** confirms and reloads from it (`?state=local:1`); "Download
  save" (was "Save snapshot") downloads the SaveFile; a save carries the session's
  command log, so a loaded session replays (`replaySession` refuses only a log-less
  scenario-B load). The turret cooldown moved into the state (`Machine.cool`) so a save
  mid-cooldown reloads and replays identically — this task's one sim-state change, and
  every number RI-01 measured is unchanged (below).
- **Viewport sizes**: `style.css` breakpoints at 1200 px (narrower panel, smaller stats)
  and 900 px (map over panel); the goal box centred over the canvas.
- **The harness** (`goalcheck.ts`, `ehour.ts`): each seed's rifle-off log replayed from a
  fresh city with `currentGoal` sampled at 1 Hz against an oracle independent of the
  text building (one "still unmet" predicate per id), every id transition recorded, each
  §11 beat derived from the state (not the bot's marks), and the save round trip at
  30:00 (hash → `makeSave` → JSON → `loadState` → both copies replay to 75:00). Five new
  `E-hour` checks and three sections (`E-hour-goal`, `-beats`, `-changes`); four tests
  (`goal.test.ts`: names, the goal through 20 minutes, `machineStatus`, the save).
- **Dev hooks** (`window.__relight`): `stateHash`, `goal`, `save`, `loadUrl`,
  `debugCoords`, `replayHash`, `saveFile` — the browser check below drives them.

**Implementation defaults (reversible, D-RI-6).** The goal order (§11's schedule, each
row skipped once met; a claim's contest and retake forms; the five support ids); the
naming rule (bearing + district + hop count; "HQ"; "<bearing> rail yard"); the hash's
three transients; one local slot plus the download; the five status words and their
shapes; `patchExcavators`'s geometric rule (an Excavator counts for an HQ patch when its
reach covers a patch tile — counting by what it digs now reads 0 once the patch is out,
so the goal would come back); the turret cooldown as state; the 1200 / 900 px
breakpoints; the goal box sampled once a sim second.

**Measured (`E-hour`, 75 minutes, seeds 3 / 4 / 5; `docs/EXPERIMENTS.md` E-hour, 18/18
checks, 66.0 s; the RI-02 sections read the rifle-off runs).** *Never wrong*: 0 of
13,503 samples (4,501 a seed) disagree with the oracle. *Every beat*: 42/42 state-derived
§11 beats change the line — mine 0:21, craft 1:21, coal-excavator 6:01–6:02, generator-2
6:02–6:03, line-excavators 6:08–6:10, shot-line 8:01–8:03, steel-2 12:01–12:03,
generator-3 and claim-east in the same second at 15:02–15:04 (one change), claim-west
25:01, generator-4 45:01–45:02, copper-2 46:02–46:03, assembler-3 50:01–50:02,
claim-north 65:01. *Stable*: 17 / 18 / 17 changes a seed and no id comes back; the
sequence is mine → craft → coal-line → gen-2 → copper-line → shot-line → steel-2 → gen-3
→ contest-east → claim-west → contest-west → rail-coal → gen-4 → copper-2 → assembler-3
→ claim-north → contest-north → hold (seed 4 shows steel-line for 2 s between gen-2 and
copper-line). Two lines are up under 5 s on every seed: gen-2 for 1 s, because the bot
places the coal Excavator and Generator 2 a second apart (a real one-second state,
reported and not smoothed), and copper-line for 5–6 s. *Support*: "feed" (a hopper to
hand-feed) shows for 1 / 26 / 36 s of the hour; nothing else fires. *Save*: the state at
30:00 reloads to the same hash and replays to the unbroken run's hash at 75:00 on every
seed; the whole rifle-off log replays to the played run's hash on every seed. Every
RI-01 check and number is unchanged (rail coal 47.9 min ahead, steel's minimum 23,
ledger 0.000, 6/6 end states); the regenerated record differs from RI-01's only in the
stamp, the wall clock and the check count.

**Browser check (Playwright over `vite preview` on port 4173, `?view=world&seed=3`,
headless Chromium with swiftshader, 2026-09-05; the script and screenshots are in the
session scratchpad, not the repo).** At 1280×720 and 1920×1080: the goal box shows
"Mine steel from the HQ patch by hand: 0 / 20 (pockets 0 steel)" with its reason ("10
hand-crafted magazines take 20 steel + 10 Cu; 20 magazines in the chest; the front drew
0.0 a minute last minute"); the page never scrolls sideways (canvas 824×696 and
1464×1056, panel 420 wide). After 90 sim seconds Ctrl+S's hash equals the live hash; the
reload lands as scenario B with the log complete, paused, at the same hash, toasting
"Save loaded at 0:01:30 (state …) — paused"; the replay hook reports the replayed and the
played hash equal at both sizes. (The two sessions' hashes differ from each other because
each ran live at 1× for a different fraction of a second before the check; every
comparison is within one session.) The debug toggle shows "west rail yard (387,629) ·
Dark · rot 24 %"; off, the coordinates go. Two findings: the goal box first covered the
canvas's own key strip and territory text (both pinned at 8 px), fixed by the HUD inset
and re-shot; and the preview 404s `/favicon.ico` (no icon is shipped — harmless).

**Verified (commands actually run, 2026-09-05, after the code and before the records).**

| command | result |
|---|---|
| `npm test` | 128 pass, 0 fail (124 + `goal.test.ts`'s 4) |
| `npm run typecheck` | red once (`goal.test.ts` passed a `GoalId \| SupportId` to `GOAL_ORDER.indexOf`; tsx does not typecheck, so the tests had passed), fixed, then green including the Vite game build |
| `npm run lint` | red once (`threat.ts` kept an unused `Machine` import after the cooldown moved), fixed, then green |
| `npm run docsync:check` | green — both messages (re-run after the record edits, closing table) |
| `npm run experiments` | 13 experiments, 267 s, **0 failing checks**; `E-hour` 18/18 (13 + RI-02's 5), 66.0 s; the rest as at RI-01 |
| `npm run freshness:check` | green — every generated file made on an ancestor of HEAD with the current config (the experiments are stamped `9a47430`, RI-01's commit, because the run preceded this commit) |
| `npm run snapshot:check` | green, unchanged (the compact scenario has no flow layer, so the cooldown field never appears) |
| `npm run build --workspace=@relight/game` | green; `vite preview --port 4173` drove the browser check above |

**Fixtures, expected results and generated files.** `packages/sim/fixtures/city{3,4,5}.json`
and `b-compact-seed3.json` unchanged. No expected result altered. `docs/EXPERIMENTS.md`
and `docs/experiments/*.json` regenerated by the run (the RI-01 numbers byte-identical;
three RI-02 sections and five checks added).

**Not built, and where it goes.** Basic sound and volume / mute controls (plan §11.2) —
not in the RI-02 row, not built; `DEFERRED.md`. Enemy silhouettes and warnings before
consequences (RI-04); commissioning, restoration and enclosure effects tied to real
state (RI-03 / RI-05 / RI-06); the street / ruin treatment and facility silhouettes
(Phase 12's art pass). Whether a person follows the line is T19's observation, so D-GB-2
stays provisional.

**Not decided by this task (for the human, no blocker).** D-GB-2 (a) is built and stays
provisional until signed or reversed after T19; the naming rule, the goal order and the
one-slot save are reversible by name.

**Next.** RI-03, ready (blocked by RI-01, done; RI-02 before it in list order, done);
T12b and T12c runnable behind it.
