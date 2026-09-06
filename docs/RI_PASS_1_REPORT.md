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

## RI-03 — Physical commissioning and field deployment (2026-09-05)

**Authorisation.** The plan's RI-03 row (§4) and `PROGRESS.md`'s, executed on "Whole plan"
(D-RI-1, recorded at RI-00). RI-01 was its only blocker; RI-02, before it in list order,
was done. The direction is D-RI-2 (decided, Daniel, 2026-09-05: one claim path, the map
charges nothing, the hour bot moves to it with the legacy run beside it); the defaults it
builds on are D-CU-1 (a) and D-CU-3 (b), provisional under D-RI-6. Every default this
task chose is named below as an implementation default: reversible, not a historical
approval, and no earlier gate is credited with any of them. The benchmark's configuration
did not change (D-RI-5): `E-hour` runs economy off, so the claim's delivery is zero there
and 10 steel + 5 Cu in the game; the unit test covers the paid path.

**What the repository was (inspected before any edit).** HEAD `38ec158` on `ri-pass-1`
(RI-02). The map's claim tool queued a `claim` command on a Dark block's click; `sim.ts`
`claim` took the price from the Depot chest's stock and started Contested at once, and
`flow.ts` `layPoles` then laid the pole run to the block's substation for free from the
claim event. Placing a pole whose reach touched a Dark block's substation claimed that
block (`poleClaims`): power connection alone activated it, which plan §4.1 forbids. Poles
and big poles were the only placement allowed off Held ground (on any Dark / Contested
block, adjacent to Held or not); every other kind, the craftable Substation included, was
refused with "the block is not Held". An outskirts block was claimed from the map and held
on the block sim's abstract supply until a Substation was built on it (`ground.ts`
GAME-ASSUMPTION; D-CU-3, D-B3-2's split). A turret fired only on a Held block and
`machineStatus` said "block not Held" for anything else. The hour bot claimed from the map
(`claimAt`). 128 tests, every check green (RI-02, the same day).

**Built (code, `packages/sim`, `packages/harness`, `packages/game`).**

- **The commissioning API** (`flow.ts`): `claimNeed(st)` (the claim's steel and copper;
  zero with economy off); `deliverTo(st, bx, by, item, n)` moves up to the need from the
  pockets into the block's installation — its substation, within interaction reach — as
  `f.delivered[bi]`, the ledger's new `committed` place (plan §4.3: in an inventory,
  committed, or consumed, never two); `activationCheck(st, bx, by)` names the one missing
  prerequisite in a fixed order (out of bounds, already commissioning, already Held, not a
  Dark block, no Held block adjacent, no substation — the outskirts need a Substation
  first, no pole run reaches its substation, the run hangs from a substation that is off,
  the grid has no supply, needs N more steel / M more Cu delivered, walk closer to the
  substation, the engineer is down); `activate(st, bx, by)` takes one commissioning id
  (`f.commissionSeq`), pushes `activate-rejected {id, reason}` on a refusal, and on
  success moves the delivered materials to the sinks once, deletes the delivery and calls
  `startContested(st, i, 'activate', id)`. Commands `deliver {bx, by, item, n}` and
  `activate {bx, by}` (hand commands, `engineer.ts`); a pre-RI-03 save upgrades with no
  deliveries and no attempts.
- **One Contested start** (`sim.ts` `startContested`): the legacy map `claim` and
  `activate` share it, so the wake bloom (§5 step 3) fires once per attempt and carries
  the activation's `id`, as does the `claim` event (`via: 'map' | 'activate'`); a
  second Activate is refused "already commissioning", so a replayed command is idempotent.
  `layPoles` from the claim event and `poleClaims` are gone: a pole reaching a Dark
  substation claims nothing.
- **The field kit** (`FIELD_KIT`: pole, big pole, lamp, floodlight, turret, belt,
  inserter, Substation): placeable on a Dark or Contested block that shares a street with
  a Held block (`fieldBlock`; "not next to Held ground" otherwise); reach, footprint,
  collision, inventory, unlock and price rules unchanged. A field device is powered by a
  connected, switched-on pole within its own reach on a grid with supply (`fieldPowered`,
  `poleGrid.on`), never by its block's substation and never by a flag; a belt or a turret
  with no draw runs where it stands (`running`, exported as `machineRunning`); a turret
  fires on the front (`threat.ts`); `machineStatus` says "no connected pole in reach"
  (`goal.ts`). Nothing here touches the block's state: a field device never marks a block
  Held.
- **The outskirts** (D-CU-3 (b)): the craftable Substation is field kit, placed on the
  Dark outskirts block from a neighbouring Held street and paid from the pockets (D-B3-1's
  stand-in); `substationAt` / `isSubstationTile` find it, and it is the installation the
  claim is delivered to and activated at. The `ground.ts` abstract-power assumption is
  gone; what remains there is narrower — a Held outskirts block's kerb has no
  streetlights to light.
- **The hour bot on the physical path** (`hour.ts`, `createHourBot(rifle, coalPlan,
  northAt, claimPath = 'physical')`): to the chest for the claim's materials plus the pole
  run's (`spare = poles + 2`), the run strung pole by pole to the block's substation
  (`stringTask`), the delivery, the walk to the substation, up to a minute's wait for a
  transient (a brownout second) to pass, the Activate, then the same walk-over and kit
  wait as the map path (`physicalClaimStep`); every command is one a player sends.
  `claimPath: 'map'` keeps the old bot for the legacy comparison.
- **The harness** (`ehour.ts`): the scored runs count Activates, deliveries and map claims
  from the log, with a new check that every claim was an Activate; a legacy run per seed
  (map claim, rifle off, 75 minutes) fills `E-hour-legacy`, `-legacy-end` and
  `-legacy-delta` (the claim and Held moments, physical − map in seconds).
- **The game**: the map's Dark-block click previews (rot, front, closes, the wake bloom,
  what the claim needs at its substation, the missing prerequisite) and sends nothing
  (`cityMapScene.ts`; `panel.ts`'s tooltip and hints say the same); in the world, E on a
  Dark block's substation within reach delivers the claim's steel then copper from the
  pockets and, when `activationCheck` passes, queues `activate` — otherwise a red toast
  names what is missing (`worldScene.ts`); the hover line on a Dark substation shows
  delivered / needed and the next prerequisite, and the walk-closer cursor covers it;
  toasts for the activation and a refusal (`main.ts`); `session.ts` counts an Activate as
  the player's claim; `telemetry.ts` records `via` and the rejections.
- **Tests** (`commission.test.ts`, five): one path (the map previews and charges nothing,
  a pole run claims nothing, the materials delivered once and consumed once at Activate,
  one claim event and one wake bloom carrying the attempt id, a second Activate refused,
  the ledger neither losing nor doubling them); every prerequisite reason in order,
  including missing power and a substation switched off; the outskirts Substation as
  field kit before activation and its claim; the field kit's power (a lamp off until a
  connected pole reaches it, then lit on real demand; a turret not off; an Assembler and
  a far belt refused; no block marked Held); the physical claim logged at 14:00 replaying
  from a save to the unbroken run's hash. `defence.test.ts`, `flow.test.ts` and
  `hour.test.ts` rewritten to the physical path (the pole test, the placement rule, the
  claim vias).

**Implementation defaults (reversible, D-RI-6).** Ordinary activation needs an adjacent
Held block (plan §4.1's default; the outskirts follow it too); the substation is the
installation (no separate cabinet); a delivery takes only up to the need and only from
the pockets; the prerequisite order above; the bot's minute-long wait for a transient
before the Activate and its `poles + 2` spare; the walk-over timed from the Activate to
the arrival (an engineer already in reach of the lot walks no logged walk, so the
arrival act writes it); the legacy run rifle off only; the field kit's power through a
pole's own reach (a turret, belt or pole needs no pole at all).

**Measured (`E-hour`, 75 minutes, seeds 3 / 4 / 5; `docs/EXPERIMENTS.md` E-hour, 19/19
checks, 69.1 s; the legacy sections read the rifle-off runs).** *Every claim physical*:
3 Activates, 0 deliveries (economy off), 0 map claims and 3 claims Held on all six scored
runs. *The claim moments* (rifle off; physical vs map): east Activate 15:18 / 15:11 /
15:09 against the map click's 15:03 / 15:03 / 15:01, Held 15:49 / 15:46 / 15:39 against
15:34 / 15:38 / 15:31; west 25:09 / 25:14 / 25:10 against 25:00, Held 25:44 / 25:43 /
25:40 against 25:35 / 25:30 / 25:30; north 65:10 / 65:14 / 65:18 against 65:00, Held
65:39 / 65:48 / 65:47 against 65:29 / 65:34 / 65:29 — the physical path adds 8–18 s a
claim (`E-hour-legacy-delta`), the pole run and the delivery walk before the Activate.
*End state*: 4 Held, the HQ standing, 6 turrets, 4 Generators, 7 Excavators, 3 Assemblers
and 669 line magazines on every run of both paths; walked 3.3–4.7 % of the hour physical
against 2.8–4.1 % map; the claim walk-overs 1–3 s physical (the Activate is next to the
lot) against 15–17 s map; no refusal, no brownout, no fall on either path. *Unchanged*:
the ledger conserves every item (0.000 unexplained, six runs), the rail yard's coal 47.9
min ahead, the chest's steel never under 19 (RI-01 and RI-02 read 23; the bot now carries
the pole run's steel out of the chest — a real change of when the chest is lowest, not a
rule change), the goal line 0 of 13,503 samples wrong, 42/42 beats, the save at 30:00
replaying to the unbroken hash on every seed. *`E-rifle`*: its tile tables replay the hour
bot's log, so the steady and rescue rows moved with the new log (shooting 0.47 / 0.92 /
0.31 % against 0.58 / 0.69 / 0.17 %; danger 0.00 %; the same rescue verdicts: every
single-edge fall saved, 2 of 6 ring falls saved and the rest delayed); all 14 checks
green. E1–E9, E-variance and E-walk are byte-identical but for the stamp.

**Browser check.** Not run this task: the game build is green inside `npm run typecheck`
(44 modules), and the map preview, the E delivery / Activate and the toasts were read,
not played. RI-02's Playwright check is the last browser evidence.

**Verified (commands actually run, 2026-09-05, after the code and before the records).**

| command | result |
|---|---|
| `npm test` | 133 pass, 0 fail (128 + `commission.test.ts`'s 5) |
| `npm run typecheck` | green, including the Vite game build |
| `npm run lint` | red once (`ehour.ts` kept a `\'` inside a template literal), fixed, then green |
| `npm run docsync:check` | green — both messages (re-run after the design-doc edits, closing table) |
| `npm run experiments` | 13 experiments, 277 s, **0 failing checks**; `E-hour` 19/19 (18 + RI-03's 1), 69.1 s; `E-rifle` 14/14; the rest as at RI-02 |
| `npm run freshness:check` | green — every generated file made on an ancestor of HEAD with the current config (the experiments are stamped `38ec158`, RI-02's commit, because the run preceded this commit) |
| `npm run snapshot:check` | green, unchanged (`held 28 front 11 interior 18 lost 0 claims 27`: the compact scenario claims by the legacy map path) |

**Fixtures, expected results and generated files.** `packages/sim/fixtures/city{3,4,5}.json`
and `b-compact-seed3.json` unchanged. No expected result altered; `hour.test.ts`'s
minute-45 expectations moved with the bot's path (the same 120 s tolerance, the vias
`activate`). `docs/EXPERIMENTS.md` and `docs/experiments/*.json` regenerated by the run
(`E-hour` and `E-rifle` carry the physical path's numbers; three legacy sections and one
check added; the rest byte-identical but for the stamp).

**Not built, and where it goes.** The hour bot laying a claim's turrets from stock
(D-P4-9's other half, the 6694b71 restoration): the field turret now exists and fires on
the front, but placing two on every claim changes the benchmark's balance — a separate,
visible decision, `DEFERRED.md` (RI-07's rerun is the natural place). Chests in the field
kit: the slice has no placeable chest kind (the Depot is its one chest); RI-05's local
supply depot is where one appears. A boss's configured response replacing the wake bloom
(RI-06) and the restoration stages (RI-05) sit on the commissioning id built here. Sound
stays deferred (RI-02).

**Not decided by this task (for the human, no blocker).** D-CU-1 (a) kept and D-CU-3
(b) built, both still provisional until signed or reversed; D-B3-1 and D-B3-2 keep their
recommendations (annotated). Whether the benchmark bot should lay a claim's turrets is the
one new question.

**Next.** RI-04, ready (blocked by RI-02 and RI-03, both done); T12b and T12c runnable
behind it in list order.

## RI-04 — Enemy origins, Crawler / Shade clarity and the Stalker prototype (2026-09-05)

**Authorisation.** The plan's RI-04 row (§6, §7, §7.1, §7.2) and `PROGRESS.md`'s, executed
on "Whole plan" (D-RI-1, recorded at RI-00). Blocked by RI-02 and RI-03, both done. The
direction is GDD §28.5 and §28.6 (the plan's §6 and §7 verbatim in intent); the Stalker's
state machine is a provisional default under D-RI-6 and its numbers are Tuning candidates
under D-RI-5 — this task built them as a candidate configuration behind a switch and
changed nothing in the benchmark's configuration (config hash `b95922d2` before and after).
D-GB-4 (enemy density, a Gate B condition) is owned here and stays open: no density
number was chosen, no roaming rule added, no combat made unavoidable (plan §8). Every
default this task chose is named below as an implementation default: reversible, not a
historical approval, and no earlier gate is credited with any of them.

**What the repository was (inspected before any edit).** HEAD `9d6a172` on `ri-pass-1`
(RI-03). `threat.ts` `spawn` bore every crawler and shade of an engagement's edge on the
street segment's *ridge* (the centre line) at a seeded hash (`hash01`, a GAME-ASSUMPTION
since M4), so a body could appear on either side of the street with no place a player
could point at, and nothing recorded where a body came from. `describeCrawler` gave HP
and the chain's three goals in the abstract ("nearest lamp, then turret, then the
substation"), not the tile it was walking to or its heading; a shade was unseen off lit
tiles until the substation went dark. No Stalker, no candidate configuration; the game's
threat drawing was a dot per body. `Block.well` marked the well blocks (`st.wells`).

**Built (code, `packages/sim`, `packages/harness`, `packages/game`).**

- **Emergence points** (`emergence.ts`): one per (block, shared street) — the block's
  frontage kerb on that street (`kerb`: the street tiles the block's watershed labels,
  4-adjacent to a lot tile that faces the street), anchored at the kerb tile nearest the
  street's midpoint; ids numbered in (block, neighbour) order from the geometry alone
  (2052 on seed 3, twice the segments), cached per `Ground`, nothing saved. `emergencePoint`,
  `emergencePointsOf`, `activeEmergencePoints` (a Dark block's points facing Held or
  Contested), `birthTile(st, p, u)` (the kerb tile `u` picks, or the next passable one).
  `threat.ts` `spawn` births on the *Dark* block's point with M4's seeded hash along the
  kerb and stamps `origin`; `T.born[id]` counts a point's births.
- **Crawler and Shade readability** (`move.ts`, `threat.ts`): every body keeps `dir`, the
  direction of its last move (`headingWord`); `crawlerTarget(st, c)` returns the tile and
  kind a crawler walks to (a lit lamp, a turret, the substation, or the engineer once
  turned); `describeCrawler` reads "Crawler · 12/12 HP · heading NE · toward the nearest
  lit lamp at (x,y) on (bx,by) · from emergence point N"; a shade keeps `trail`, the unlit
  tiles it crossed with their times (`shadeTraces`, `shadeNear`, `TRACE_S` / `TRACE_N`
  render constants). `litAt` and the shade's untargetable rule are untouched.
- **The Stalker candidate** (`candidates.ts`, `stalker.ts`): `CANDIDATES.stalker` holds
  §7.1's numbers in one object outside `SimConfig` — perception 8, leash 16, HP 2× a
  Crawler, speed 1.2×, wind-up 0.8 s, one 5 HP swing (the retaliation value) a second —
  plus the defaults below. `enableStalkers(st)` puts the layer on the flow (`stalk`);
  without it nothing exists (the benchmark's state has no layer, no events, no field in
  its config). One Stalker per Dark well block (`wellBlocks`, from `st.wells`) at its
  **home**, the nearest passable tile to the substation, on `guard → investigate →
  pursue → attack → return` with `dead`; cues are deterministic simulation events
  (the engineer moved or fired within perception, a machine placed within it); a leash
  from home ends a pursuit and it returns and guards again, cold; a wind-up precedes the
  first strike; a strike during a valid dodge is a miss (`dodged`); nothing lands on a
  downed engineer; it walks only passable ground (`moveTo` / `stepToward`); a Held site
  retires its survivor once home (`retired`) and never refills; a dead one's site refills
  after `respawnS` only while Dark and while the engineer is out of perception of the
  home. Turrets and the rifle hit it as one more body (`Target = Crawler | Stalker`);
  its kills count on the layer, not in `turretKills` / `rifleKills`. Events
  `{type: 'stalker', what: spawn | investigate | pursue | attack | hit | dodged | return |
  dead | retired}`. `describeStalker`, `stalkerAt`, `stalkersOf`. A save carries the
  layer and its candidate (`loadState(makeSave(st))` keeps five Stalkers on seed 3).
- **Tests**: `emergence.test.ts` (4) — the points (count, ids, placement, both ways of
  every ring edge, identical on a rebuilt seed, different on another), the active set
  (a Held neighbour silences its points), 60 s of births (every body on its point's
  frontage kerb, never a Held tile, spread along a wide frontage, counted per point, a
  heading and a target once moving), the shade trace (behind the shade, aged, `litAt`
  unchanged, fading); `stalker.test.ts` (8) — the candidate's values and its absence
  from the benchmark, five Stalkers on seed 3 at valid homes and the sweep, the approach
  (investigate → pursue → attack, the first hit 0.7–0.85 s after contact, one a second,
  5 HP each), the dodge (a strike during the dodge misses, HP unchanged, the next lands),
  death and the engineer down (no attack after either, no instant respawn), the leash
  (never beyond 16 tiles + one step from home, then home and guarding cold), the
  restored site (return, retire, no respawn) and the respawn rules, turret and rifle
  kills (24 HP = 6 rounds, counted on the layer).
- **The harness** (`erifle.ts`): `E-rifle-cand-stalker` — the rifle-on hour with the
  switch on, per seed: well sites, Stalkers fielded, pursuits, swings, hits, dodged,
  contact seconds, kills, retired, and HP lost / downs / falls / Held / magazines against
  the rifle-on hour above it; two bookkeeping checks (every swing lands or is dodged; one
  Stalker fielded per site). Labelled a candidate run (D-RI-5): reported, not gated.
- **The game**: `?stalker=1` switches the candidate on (`session.ts`, kept in a session's
  parameters so a replay switches it on too); the world view draws the active emergence
  points (a pulsing ring on the Dark kerb with a tick toward the block it feeds), a
  crawler's heading tick and, under the cursor, its target tile and line, a shade's
  fading trace on unlit tiles and a flicker on streetlight posts near a shade (drawing
  only — the light layer is untouched), and a Stalker with its perception ring, home
  mark, wind-up rim, pursuit / investigate rims and HP bar; the hover names an emergence
  point ("rot from A comes out along this street toward B"), a crawler's description and
  a Stalker's; toasts for a pursuit (with the dodge key), a kill, a retirement
  (`main.ts`).

**Implementation defaults (reversible, D-RI-6).** The emergence mouth is the Dark
block's frontage kerb with M4's seeded hash along it — a first cut birthed every body of
a point on the anchor tile alone and `E-rifle`'s tile rescue went red (the engineer, who
stands on the street's midpoint there, knocked down on seed 5 at 90 s; the steady hour
on seed 4 gained a down), a balance shift RI-04 does not own, so the spread stays. The
occupied site is a Dark well block; its home is the nearest passable tile to its
substation (a pole tile when the substation is the only one). Cues: the engineer moved or
shot within perception, a machine placed within it; investigate times out after 6 s,
pursuit keeps a 2-tile hysteresis beyond perception and is lost 2 s after sight; respawn
120 s after a death, only while the block is Dark and unrestored and the engineer is out
of perception of the home (a site never fielded because the engineer stood at the switch
fills once they leave). Stalker kills count on the layer only. Sim time for the layer's
timers is `st.t` (whole seconds), so timeouts and the respawn compare in seconds. The
trace's length and age and the flicker are render constants. The map scene does not draw
emergence points (the world view does).

**Measured.** From the tests on seed 3 (site 377, home (645.5, 674.5), the engineer six
tiles east walking in): investigate at 0.05 s, pursue at 0.10 s, contact at 0.60 s, the
first hit 0.75 s after contact and then exactly one a second at 5 HP; the leash run
peaks at 16.18 tiles from home (the leash plus one 0.05 s step) and ends guarding cold.
`E-rifle-cand-stalker`, seeds 3 / 4 / 5: 5 well sites, 5 Stalkers fielded, **0 pursuits,
0 swings, 0 contact seconds** — the hour bot never comes within perception of a well
block (the nearest is six hops from the HQ), so the candidate's contact numbers on the
benchmark hour are zero by geography and say nothing about the archetype; HP lost /
downs / falls / Held / magazines equal the rifle-on hour's on every seed. The emergence
geometry moved `E-rifle`'s numbers (rows in `EXPERIMENTS.md`, HEAD's JSON as the
reference): the tile steady hour's rifle rounds 17 / 34 / 11 → 9 / 10 / 6 and shooting
share 0.92 % → 0.25 % max; seed 4 gained one down (HP lost 102; a crawler that spotted
the bot walking past at 25:12 followed it into Dark block 364, where the bot waits on a
claim and the rifle sees nothing on unlit tiles — pre-existing behaviour on a new draw
of the seeded hash; danger 0.58 %, inside D-B1-5's 5 %); the tile rescue's falls stay
within 0.1–0.2 min of HEAD's and the rifle still saves what it saved (1/1 single-edge,
2/6 ring), but the damage moved with the lane: seed 4's rescues lost 2 HP where they
lost 50, seed 5's 600 s edge rescues ended at 8 and 2 HP where they ended at 53 and 55
(the stance stands on the midpoint the stream now crosses; the "never knocked down"
check holds, thinly). `E-hour` 19/19 on the same run: every check's detail identical but walking (4.67 → 4.57 % worst seed); the timeline and end state move by seconds (first shot 14:02 → 35:13 on seed 3, 5:37 → 8:24 on seed 4; rifle rounds 26 / 40 / 11 → 13 / 10 / 6; crawlers 519 → 517 on seed 3), 4 Held and no fall on every seed, the ledger conserved, the save hashes new as a changed state's must be.

**Browser check.** Not run this task: the game build is green inside `npm run typecheck`
and the world-view drawing, hover and toasts are typechecked, not viewed. The
`?stalker=1` switch and the flicker are for T19's session or the human's next look.

**Verified (commands actually run, 2026-09-05, after the code and before the records).**

| command | result |
|---|---|
| `npm test` | 145 pass, 0 fail (133 + `emergence.test.ts`'s 4 + `stalker.test.ts`'s 8) |
| `npm run typecheck` | green, including the Vite game build |
| `npm run lint` | green |
| `npm run docsync:check` | green — both messages (the enemies table is generated from `ENEMIES`; the Stalker is not in it) |
| `npm run experiments -- --only E-rifle --seeds 3,4,5` | red once (14/16: the single-tile mouth — the engineer knocked down in seed 5's rescue, the single-edge fall no longer saved), then 16/16 with the frontage spread, 109 s |
| `npm run experiments` | 13 experiments, 296 s, **0 failing checks**; `E-hour` 19/19, 72.3 s; `E-rifle` 16/16 (14 + RI-04's 2), 112 s; `E-variance` 4/4, `E-walk` 3/3, E1–E9 green as at RI-03 |
| `npm run freshness:check` | green — every generated file made on an ancestor of HEAD with the current config (the experiments are stamped `9d6a172`, RI-03's commit, because the run preceded this commit) |
| `npm run snapshot:check` | green, unchanged (`held 28 front 11 interior 18 lost 0 claims 27`) |

**Fixtures, expected results and generated files.** No fixture regenerated, no expected
result edited, no source stamp rewritten. `docs/EXPERIMENTS.md` and
`docs/experiments/E-*.json` are the run's output (the `--only` run rewrote
`E-rifle.json` alone; the full run rewrote them all). The compact snapshot is unchanged
(no threat state in it). The enemies table in the design doc is unchanged (generated;
the Stalker is a candidate, not an `ENEMIES` row).

**Not built, and where it goes.** The Conductor and the Breaker display name (RI-10; the
name stays "Hulk" in the generated table until then). Emergence points on the map scene
and any discovered-site marker for a project (RI-05 owns the project record; the world
view's active points are the local-activity cue plan §6 asks for). The hour bot never
dodges, so `E-rifle-cand-stalker`'s "dodged" is zero by construction and the dodge
counterplay is only unit-tested; a bot that walks to a well block is what would make the
candidate's contact numbers mean something — a harness scenario for the human to ask
for, not a benchmark change. Sound stays deferred (RI-02); the bot's turrets stay
`DEFERRED.md` (RI-03).

**Not decided by this task (for the human, no blocker).** D-GB-4 stays an open gate
condition: the candidate exists and is measurable, no density number was chosen. The
Stalker candidate's promotion into the benchmark (D-RI-5: a decided row). Whether the
tile rescue's legacy stance — the street midpoint — should move to the engineer's own
kerb now that the mouth is readable across the street (its configuration is preserved
as plan §14 asks; the thinner seed-5 margins are reported, not fixed).

**Next.** RI-05, ready (blocked by RI-03, done); T12b and T12c runnable behind it in list
order.

## RI-05 — Neighbourhood project framework and the rail-yard reward (2026-09-05)

**Authorisation.** The plan's RI-05 row (§4.2, §4.4, §5, §9.1, §13 — "bring a minimum
useful part of T16 / transport into RI-05") and `PROGRESS.md`'s, executed on "Whole plan"
(D-RI-1, recorded at RI-00). Blocked by RI-03, done. The hybrid's second task (D-RI-4);
the direction is GDD §28.4's project record, its first project and its reward, and §13 /
§14's transport rows; every size, cap and price this task chose is an implementation
default under D-RI-6 — reversible, not a historical approval, no earlier gate credited
with any of it. The benchmark's configuration is unchanged (config hash `b95922d2` before
and after; `E-hour`'s belt line untouched — the tram route runs beside it). Plan §2's
boundaries hold: the reward is the transport capability, no loot, no random drop, no stat
bonus; belts and machines stay indestructible. Plan §5.1's order holds and is measured:
the rail yard is deliverable before any tram exists, the kit unlocks at the restoration's
second and never before, and nothing the reward grants is needed to earn it.

**What the repository was (inspected before any edit).** HEAD `24bc007` on `ri-pass-1`
(RI-04's commit). No project record anywhere: territory was the block record alone
(§4.4), a Held block was the end of its story, and §28.4 was direction with a catalogue.
The Depot was the only chest — the HQ's inventory, reached by `chestTake` / `chestPut`
from the Depot alone; `FIELD_KIT` had no chest and "chests as placeable objects" stood in
the not-built list. No Track, Tram stop or Tram kind (§13's rows Track, Tram stop, Tram,
Tram depot, Freight tram and Rail crew all unbuilt); `unlockedKinds` was every kind, and
`defence.test.ts` asserted the literal 12. The yard's coal reached the Depot by belt in
`E-hour` (RI-01's west line). The hour bot's hand commands that fell out of reach were
dropped by the sim silently (a `chestTake` too far from the Depot does nothing and the
bot waited on it), which mattered once the bot had to work at a chest placed by itself.

**Built (code, `packages/sim`, `packages/harness`, `packages/game`).**

- `packages/sim/src/project.ts` (new). The record — `projectId, siteId,
  neighbourhoodBlockIds, requirements, deliveredItems, stage, activationAttemptId,
  rewardId` plus `restoredAt, rewardAt, installId` — and the stages `discovered,
  preparing, ready, commissioning, restored, interrupted`. The stage is derived by
  `syncProjects` from the site's real state after every block tick and every hand
  command, never stored as a second territory: `ready` when the claim's activation check
  passes without hands, `commissioning` while the block is Contested (its attempt id
  recorded), `restored` once when the block is first Held (a legacy map claim restores
  with attempt −1, so a pre-RI-05 save discovers and restores its projects on load),
  `interrupted` when a Dark site has an attempt behind it or a restored site is lost
  (the reward stays — owning an unlock is distinguished from an operational facility by
  `projectOperational`), `preparing` on a delivery, a pole run reaching the site or a
  crafted Substation, else `discovered`. Two catalogue entries: the rail yard (its
  requirements are the claim's, its reward `rail-route` — the kit) and the local supply
  depot (created on the yard's restoration; requirements 20 coal + 10 magazines in a
  Supply chest on the site; its reward `local-depot`, the chest that hands out kits).
  `commission` is the depot's hand command: within reach of a stocked chest on a
  restored site it changes the record alone — no claim, no charge, the stock stays,
  idempotent. `describeProject` for the panel and the toasts; `projectTitle` names them
  "<Block> restoration" / "<Block> supply depot".
- `packages/sim/src/flow.ts`. Four kinds — `chest` (2×2), `track` (1), `tramstop` (2×2),
  `tram` (1) — with `MACHINE_COST` (chest 10 steel, track 1, stop 10, tram 20 + 5 Cu) and
  kW (the stop 20, the rest 0). `RAIL_ROUTE_KINDS` and `PROJECT_UNLOCKS` behind
  `unlockedByProject`; `lockReason` says "the rail yard restoration unlocks it";
  `FIELD_KIT` gains the chest. The tram: `tramRoute` (the one line its track makes, a
  junction parks it; cached per placement revision), `stopAt`, `tramTransfer` (instant,
  only at a `running` stop: the platform aboard, the cargo to the arrivals), `tickTram`
  (8 t/s along the line, 4 s dwell, before the `!running` gate — it moves without
  power). Inserters take from a chest's inventory and a stop's arrivals; belts end in a
  chest or a stop's platform; `beltDeliver` lets a belt whose next tile is a Dark front
  block's Substation commit steel / copper to the claim (`stats.beltDelivered`, the
  "off a belt" delivery); `stats.tramMoved`. Placement: chest / track / stop on the
  margin, `'track runs on streets'` elsewhere, a tram on track only, one a tile, not in
  `occ`; pickup: a tram before its track, track under a tram refused. `chestTake` /
  `chestPut` take an optional `at` — a chest or a stop within reach — through one
  `handPool`; kits come only from the Depot or a restored depot's chest ("kits stay in
  the pockets"). `activationCheck(st, bx, by, hands = true)`; `activate` records the
  attempt on the project; `describeMachine` for the four kinds; the ASCII map's `c = H M`.
- `packages/sim/src/types.ts` (`ProjectStage`, `ProjectRecord`, `FlowState.projects`, the
  `commission` command, `chestTake` / `chestPut { x?, y? }`, the `project` event),
  `walk.ts` (track and a tram are passable — rails in the street), `goal.ts` (a status
  line for each kind), `ledger.ts` (a tram's load and a stop's pools are held items),
  `engineer.ts` (`commission` goes through the hand hook), `index.ts`.
- `packages/sim/src/hour.ts`. `createHourBot(…, route = 'belt' | 'tram')`; the belt
  route is byte-for-byte the benchmark's. `within(bot, label, x, y, size, fn, tries)`
  re-walks to the target up to three times before a hand command, because the sim drops
  an out-of-reach one silently; `putAt` uses it. `tramPlan` reads the route off the
  state — the street column between the yard and the HQ nearest the HQ lot that takes it
  all, stop B on the Depot's row, stop A nearest the heap, the belts by breadth-first
  search — and `tramLine` lays it from the Depot's stock at the west step + 60 s;
  `depotSupply` (six minutes later) loads the depot's stock onto stop B's platform, waits
  for the record to read ready, walks within reach of the chest and commissions. Marks:
  rail-yard-restored, rail-kit-unlocked, tram-route, tram-first-delivery, depot-supplied,
  depot-restored.
- Tests. `packages/sim/test/project.test.ts` (new, 3): the rail yard's stages through the
  pole run, hand and belt deliveries, the Activate, the restoration, the reward's lock
  before and unlock after, a later loss (stage and reward kept, non-operational) and the
  retake (re-grants nothing); the depot's commission (the record alone; the stock stays;
  kits from the restored chest; a pre-RI-05 save discovers its projects on load); a chest
  feeding and fed by inserters, the shuttle (track on the street only, a stop each end,
  platform → arrivals, the hand and an inserter taking there), the ledger balanced and a
  mid-run save to the same hash. `defence.test.ts` derives the unlocked count from
  `KINDS` minus `RAIL_ROUTE_KINDS`.
- `packages/harness/src/experiments/eproject.ts` (new) and `index.ts`: `E-project`, eight
  sections and 10 checks on seeds 3 / 4 / 5 — the timeline, the records at the end, the
  route as laid and its price, the replay with the kit's lock sampled every tick, the
  stages, the 40:00 save, the ledger, and the same bot on the belt route beside it.
- `packages/game/src/worldScene.ts` (the four kinds drawn — the chest's item squares, the
  stop's platform stripe lit when powered, the tram on its rail; `MACHINE_COL`,
  `BOX_MACHINE`, `UNLOCK_KEYS` C / L / H / V; E over a chest or a stop opens its
  pockets), `panel.ts` (the pockets target the chest or stop under the engineer, the BUILD
  rows, a Projects list from `describeProject`), `main.ts` (the `project` event's toasts:
  discovered, ready, commissioning, restored, interrupted — the toast is the
  presentation, never the completion).

**Implementation defaults (reversible, D-RI-6).** The reward grants §13's Tram depot /
Rail crew kit's three kinds — Track, Tram stop, Tram — and those rows stay the design;
the Tram depot and Rail crew as buildings are unbuilt (RI-09). The Tram stop is 2×2 with
two 200-item pools (platform, arrivals) and an instant transfer at a powered stop in
place of §13's 2×3 with six inserters; it draws §13's 20 kW always. The Tram is one tile
(§13: 1×3), 200 items at 8 t/s, a 4 s dwell, one line between two stops, a junction
parks it; it draws no kW. Track runs on streets only, 1 steel a tile, walkable. The
Supply chest is 2×2, 200 items in all, 10 steel, in the field kit. Prices: a stop 10
steel, the tram 20 steel + 5 Cu — the copper so the route costs the one thing the hour
is short of at 26:00 (GAME-ASSUMPTIONS; §13 prices none). The depot's stock is 20 coal +
10 magazines, not consumed; its function is the kit hand-out. `preparing` is a delivery,
a pole run reaching the site or a crafted Substation. A legacy map claim restores with
attempt −1. The bot's `within` re-walk is the bot's, not a rule. Titles are capitalised
block names. None of these is a historical approval; the human reverses any by name.

**Measured (`E-project`, seeds 3 / 4 / 5, rifle off, the hour bot on the tram route).**

| seed | claim-west | restored = kit unlocked | tram route | first delivery | yard's coal at the Depot | depot restored | first brownout | Generator 4 | claim-north | held-north |
|---|---|---|---|---|---|---|---|---|---|---|
| 3 | 25:11 | 25:46 | 26:45 | 26:59 | 27:15 | 32:18 | 26:43 | 45:00 | 65:10 | 65:39 |
| 4 | 25:14 | 25:43 | 26:47 | 27:00 | 27:15 | 32:15 | 26:44 | 45:01 | 65:14 | 65:48 |
| 5 | 25:10 | 25:40 | 26:42 | 26:56 | 27:05 | 32:15 | 26:39 | 45:00 | 65:17 | 65:46 |

At 75:00 on every seed: the rail-yard record `restored · attempt 2 · reward rail-route`,
delivered 0 steel / 0 copper (the benchmark's economy-off claim, as at RI-03); the
supply-depot record `restored · attempt -1 · reward local-depot · delivered 20 coal, 10
magazine`; 732 items moved by tram; 702 coal dug at the yard; 4 Held, the HQ held, no
fall, 0 refusals. The route as laid: seed 3 column 408 rows 622–628 (7 track), stops
(406,627) / (409,622), the chest (403,627), 19 + 1 belts, 89 steel + 7 Cu; seed 4 column
454 rows 662–665, 20 + 1 belts, 87 steel + 7 Cu; seed 5 column 283 rows 637–643, 16 + 3
belts, 88 steel + 7 Cu. The replay: hashes d26403f4 / ea05f464 / 1aa9b52d equal to the
played run's, the records equal, the kit locked at 0:00 and unlocked at the restoration's
second, the route laid after it; stages discovered 0:00 → ready 25:05 / 25:06 / 25:05 →
commissioning 25:11 / 25:14 / 25:10 → restored. The 40:00 save: 4bac3c2e / 5eb3e599 /
ae8fd197 equal after the load, both copies to the same end hash, the goal line wrong 0
samples. The ledger conserved on every seed (seed 3: steel 200 + 3297 → 1629 + 1868,
copper 100 + 1157 → 475 + 782, coal 80 + 1404 → 548 + 936, magazines 50 + 669 → 253.8 +
465.2; the tram's load and the stops' pools counted as held). Beside it, the same bot on
the belt route: the west line 26:42 / 26:44 / 26:39, the coal at the Depot 27:04 / 27:04 /
27:02, brownout 0 s, copper minimum 8 / 32 / 9, the chest at 75 1589/385/471/108,
1605/2084/469/279, 1631/385/472/215 (St/Cu/coal/mag) against the tram route's
1499/378/436/87, 1521/2048/436/259, 1546/378/440/196.

*A finding, reported and not tuned.* The two Tram stops' 40 kW (§13's own 20 kW figure)
put the tram-route hour into a proportional brownout from 26:43 / 26:44 / 26:39 until
Generator 4 at 45:00 (1688 / 1131 / 1130 s) where the belt route never browns out, and
the Tram's 5 Cu leaves copper at 1 on seed 3 (the belt route's minimum 8). The hour
still stands on every seed (4 Held, no fall, the coal by tram eleven seconds behind the
belt). Whether Generator 4 comes earlier when the route is chosen, the stop draws only
while transferring, or the price changes is the human's tuning decision (`DECISIONS.md`
"Outstanding questions", `DEFERRED.md`).

`E-hour` 19/19 with every timeline and end number unchanged; only its state hashes moved
(the state now carries `flow.projects` and two stats counters) and two marks were added
(rail-yard-restored / rail-kit-unlocked at 25:46 / 25:43 / 25:40 — the same seconds as
held-west). `E-rifle` 16/16, `E-variance`, `E-walk` and E1–E9 changed by their source
stamp only (`9d6a172` → `24bc007`).

**Browser check.** Not run this task: the game build is green inside `npm run typecheck`
and the four kinds, the pockets on a chest or stop, the Projects list and the toasts
were reviewed in the diff, not played. The record and its rewards are the sim's
(`project.ts`); the game layer only draws them.

**Verified (commands actually run, 2026-09-05, after the code and before the records).**

| command | result |
|---|---|
| `npm test` | 148 pass, 0 fail (145 + `project.test.ts`'s 3) |
| `npm run typecheck` | green, including the Vite game build |
| `npm run lint` | green |
| `npm run docsync:check` | green — both messages (the §13 footprint table is hand-written; the generated blocks are unchanged) |
| `npm run experiments` | 14 experiments, 369 s, **0 failing checks**; `E-project` 10/10, 53.9 s; `E-hour` 19/19, 79.3 s; `E-rifle` 16/16, 126.6 s; `E-variance` 4/4, `E-walk` 3/3, E1–E9 green as at RI-04 |
| `npm run freshness:check` | green — every generated file made on an ancestor of HEAD with the current config (the experiments are stamped `24bc007`, RI-04's commit, because the run preceded this commit) |
| `npm run snapshot:check` | green, unchanged (`held 28 front 11 interior 18 lost 0 claims 27`) |

The three checks were re-run after the document edits: docsync, freshness and snapshot
green with the same messages.

**Fixtures, expected results and generated files.** No fixture regenerated, no expected
result edited, no source stamp rewritten by hand. `docs/EXPERIMENTS.md` and
`docs/experiments/E-*.json` are the full run's output: `E-project.json` is new;
`E-hour.json` changed in its hash rows and the two added marks alone; the others in
their stamp alone. The compact snapshot is unchanged (no project in it — a snapshot is a
tile state, and a loaded pre-RI-05 save discovers its projects itself). The design doc's
generated tables are unchanged; the §13 rows edited are the hand-written footprint table.

**Not built, and where it goes.** The rest of T16 — the Freight tram, the Tram depot and
the Rail crew as buildings, routes with more than two stops, a stop's inserter sides —
is RI-09. The catalogue's foundry, riverside generation and transformer are RI-11 (§28.4
keeps them). A map-view marker for a discovered site (§5.3's presentation beyond the
Projects list and the toasts). An interruption narrated beyond the record: a lost
restored site reads `interrupted` and non-operational, nothing more. A readable refusal
for an out-of-reach hand command (`DEFERRED.md`; RI-09's T13 is the natural place). The
bot's turrets stay `DEFERRED.md` (RI-03); sound stays deferred (RI-02).

**Not decided by this task (for the human, no blocker).** The Tram stop's 20 kW on the
hour and the Tram's copper (the finding above). Whether the tram route ever joins the
benchmark (D-RI-5, a decided row: beside it until the human moves it). The depot's
reward — the kit hand-out is the minimum §28.4 names; anything richer is a design
choice, not this task's. RI-05's sizes, caps and prices, listed above as defaults.

**Next.** RI-06, ready (blocked by RI-04 and RI-05, both done); T12b and T12c runnable
behind it in list order.

## RI-06 — The Junction Heart (2026-09-05) — code only, unverified

**Authorisation.** The plan's RI-06 row (§9: "**Direction:** prototype this encounter
first"; §9.2's state machine and twelve implementation defaults; §9.3; §9.4's acceptance)
and `PROGRESS.md`'s, executed on "Whole plan" (D-RI-1, recorded at RI-00). Blocked by
RI-04 and RI-05, both done. The hybrid's third task (D-RI-4). The direction is GDD §28.8;
every number and shape this task chose is an implementation default under D-RI-6 —
reversible, not a historical approval, no earlier gate credited with any of it. The
encounter is a candidate configuration (D-RI-5): `CANDIDATES.heart` in `candidates.ts`,
outside `SimConfig` (hash unchanged), switched on per state by `enableHeart` and in the
game by `?heart=1`, reported as `E-heart` (run name RI-06-cand-heart) beside `E-hour`, which
keeps its configuration. Plan §2 holds: no loot, no random drop, no health bar to shoot
(the Heart is destroyed by commissioning, never by damage); belts and machines stay
indestructible (the cabinets are knocked out and repaired, never destroyed); §8 holds: no
new roaming rule, no density change — the packets are the only spawning the encounter adds
and they stay under the global population budget and their own cap.

**The user's instruction for this task.** After eight hours of verification passes on the
earlier tasks the user said: *"Ok don't worry about running tests or simulations, just
write the code."* So this task **ran no test, experiment, snapshot, docsync, freshness or
browser check**. `npx tsc --noEmit` on sim, harness and game and eslint on sim and harness
were run and are green; nothing else. `E-heart` and `heart.test.ts` are written and unrun;
`docs/EXPERIMENTS.md` still holds RI-05's runs. Every claim below about what the code does
is a reading of the code, not a measurement. The verification pass is scheduled
separately; a red result there is fixed under RI-06 before RI-07's evidence is claimed.

**What the repository was (inspected before any edit).** HEAD `60d9f3f` on `ri-pass-1`
(RI-05's commit). The rail yard's restoration was a claim like any other: the Activate at
its substation, a wake bloom, a burn-off of 20 + 60·d s, then Held and the project
record's reward. No boss site, no cabinet, no encounter state anywhere (§28.7, §28.8
direction only). `deliver` carried no target but the block; `startContested` always
bloomed and always set the burn-off timer; a crawler targeted the substation, streetlights
or the ring; the hour bot's west claim was RI-03's physical claim step.

**What was built.**

- *The layer (`packages/sim/src/heart.ts`, new; `candidates.ts`).* `HeartState` on
  `FlowState.heart`: the site (the rail yard's block), two `HeartCabinet`s, the attempt
  id, productive progress and stall seconds, the once-only charge, the requested packets
  (`attempt:threshold` → the second requested), the pending packets, `destroyed` and the
  stats. `enableHeart(st)` (idempotent, requires the flow layer and a rail yard) places
  one cabinet toward each of the yard's first two Dark neighbours in block-index order:
  the yard's emergence point toward that neighbour moved `MARGIN_TILES + 1` inward along
  the dominant axis, then the nearest passable lot tile the yard owns with no rubble,
  machine or substation (ring ≤ 4). The candidate: 90 s productive, 60 s stall, thresholds
  25 / 50 / 75 %, 5 s approach, packets of 2 / 3 / 4 Crawlers, 8 live at most, 4 steel +
  2 Cu a cabinet.
- *Deliveries and power.* `deliver` gained `cabinet`; `deliverToCabinet` moves what the
  cabinet still needs from the pockets within reach; a charged installation refuses a
  second delivery. `cabinetConnected` = not knocked out, supply above zero and a pole run
  reaching the tile (`polePlanTo` strings a run to any tile; `placeable` refuses the
  cabinet's tile). The ledger counts the cabinets' stock as committed.
- *The Start and the encounter.* `activationCheck` on the Heart's block adds
  `heartCheck` (each cabinet supplied, up and on the live grid — the reason names the
  cabinet and the corrective action) and reads the charge instead of the store when the
  installation is already charged. `activate` charges once (`H.charged`, the spent
  counters once), starts the block Contested with `bloom: false` and `until:
  Number.MAX_SAFE_INTEGER` (no burn-off — §28.8's sentence built; `contestProgress` and
  `blockLights` read the productive fraction), then `heartStarted`. `heartTick` each
  flow tick: productive while both cabinets are connected (progress and the threshold
  requests, keyed to the attempt and threshold, the packet born `approachS` later on the
  approach's emergence point via `heartBirth`, bounded by `maxAlive`); otherwise the stall
  counts; `productiveS` reached → `complete` (the cabinets' materials spent, the Heart
  destroyed, `contestUntil` = now so the block turns Held on its normal path and the
  project record grants the kit once); `stallS` reached, the explicit `abort`, or the
  block no longer Contested → `interruptHeart` (`interruptContested` turns the block Dark
  with no fall; the pending packets dropped; the deliveries and the charge kept;
  progress reset; the stats).
- *The bodies (`threat.ts`).* `heartBirth` makes a Crawler with `edge: -1`, `cls: 2`, its
  packet key and the point's id (the born-count and the spawned stat as any crawler);
  `targetsOf` on the Heart's block puts the live cabinets first; on arrival at a cabinet
  the body knocks it out (`knockOutCabinet`) and dies; a Heart body is Held-side for the
  field (`isCrawlerHeld`), the rifle hits it as any crawler, the turret rules are the
  ring's. `describeCrawler` names the packet and the cabinet.
- *The bot (`hour.ts`).* `createHourBot(…, heart = true)`; `physicalClaimStep` hands the
  Heart's block to `heartClaimTasks`: one chest trip for the installation's, the cabinets',
  the poles' and two turrets' materials plus the magazines; the run to the substation and
  the delivery as any claim's; per cabinet `stringToTask` (a pole run to the tile), the
  delivery, one turret within three tiles fed by hand; the kits; the Start; then
  `heartPatrol` every two seconds (a cabinet down → walk and `repairCabinet`; an
  interruption → the substation and the Start again; a turret at half → feed) until the
  yard is Held, with the walk-over and the kit wait after. Marks: `cabinet-N-supplied`,
  `turret-N`, `heart-start`, `packet-25/50/75`, `heart-first-body`, `cabinet-down`,
  `cabinet-repaired`, `heart-interrupted`, `heart-retry`, `heart-destroyed`.
- *`E-heart` (`packages/harness/src/experiments/eheart.ts`, the 15th experiment).* Per
  seed: the Heart's hour (the bot above, rifle off); the timeline; the end (attempts,
  packets, bodies, knock-outs, repairs, productive / stalled, the charge, the record);
  the cabinets; the replay on a fresh city with the layer, the heart and project events
  counted every tick (one destruction, one restoration, no duplicate packet key, the
  starts equal to the attempts); a 30:00 save reloaded and replayed (goalcheck.ts); the
  ledger; `E-hour`'s benchmark beside it. Ten checks on §9.4: destroyed with no shot on
  every seed, packets once, charged and rewarded once, an interruption preserving the
  deliveries, one readable line, the replay and save hashes, conservation, the HQ held, no
  refusal.
- *`heart.test.ts` (six tests).* The layer's geometry on seeds 3 / 4 / 5 and its
  idempotence; deliveries by hand and the Activate refused until both cabinets are
  supplied and powered (a labelled scenario: injected stock); the bot's encounter to the
  destruction with the once-only facts; a scripted stall (`knockOutCabinet` standing in
  for the arrival, 60 s to interrupted, the deliveries and the charge kept, the repair,
  the retry with no second charge and its packets keyed anew); a save mid-attempt and the
  abort; a pole removed from a cabinet's run pausing the commissioning without a
  knock-out.
- *The game.* `?heart=1` (session.ts, kept in the save's params); the cabinets drawn as
  squares (green on the grid, amber supplied, red knocked out, grey empty), a pulsing
  ring on the Heart's substation, a requested packet's approach on its emergence point
  with a line to the cabinet it comes for; E on a cabinet delivers from the pockets or
  repairs it; E on the substation reads `describeHeart` while it is commissioned; X
  aborts; the `heart` toasts (the Start with the rules, each packet, born, a knock-out,
  a repair, the interruption, the abort, the destruction); the claim toast names the
  productive commissioning instead of a burn-off; the goal line's `claimLine` and the
  Projects list's line read `describeHeart` (the objective, the active failure condition
  and the next action in one line — §9.4).

**Implementation defaults chosen here (D-RI-6, all reversible, none validated).** The
cabinet geometry and recipe; the Activate as the Start; no wake bloom; the far-future
`contestUntil`; Crawler-only packets and their counts, the alternating approaches, the
5 s approach and the cap of 8; the knock-out on arrival and the body's death there;
`REPAIR_COPPER` as the repair price; the charge kept through interruptions; the
cabinets' materials spent at the destruction; the bot's preparation (one turret a cabinet,
a 20-pole limit a run, the patrol's two-second beat and its deadline of 90 + 3·60 + 300 s).
`describeHeart`'s wording. Anything the plan lists as a Tuning candidate stays one.

**Not decided by this task.** Whether the Heart ever joins the benchmark (D-RI-5). §9.3's
"two materially different workable preparations" — the bot plays one; the second is a
human's or a later bot variant's. Stalkers in the packets (the plan allows a small number;
none are fielded — the Stalker is its own candidate layer). What §9.4's "representative
recoverable problem" is in play: the code offers the knocked-out feeder and the
interruption; a human names the representative one.

**Next.** RI-07, ready in list order (blocked by RI-06, done — unverified). The
verification pass first, ideally: `npm test`, `npm run experiments` (`E-heart` beside the
14), `snapshot:check`, `freshness:check`, `docsync:check`, `npm run typecheck` with the
Vite build, and a browser check with `?heart=1`.

**Files.** New: `packages/sim/src/heart.ts`, `packages/harness/src/experiments/eheart.ts`,
`packages/sim/test/heart.test.ts`. Edited: `packages/sim/src/{candidates,types,sim,flow,
threat,project,engineer,ledger,index,goal,hour}.ts`, `packages/harness/src/experiments/
index.ts`, `packages/game/src/{session,main,worldScene,panel}.ts`, `docs/RELIGHT-design.md`
(§28.8 and its changelog), `docs/PROGRESS.md`, `docs/PROGRAMME_STATE.md`,
`docs/DECISIONS.md` (D-RI-4 / D-RI-5 / D-RI-6 annotations), `CLAUDE.md` (the command
table's last-run cells), this report.

## RI-06 validation fixes — 2026-09-06

**Scope and baseline.** Daniel asked to fix the failures blocking RI-07. This pass
repairs RI-06; it does not implement RI-07 or change the candidate's costs, timers,
packet sizes, population cap or benchmark configuration. The six-test baseline was
1 passed / 5 failed (`evidence/heart-fix/baseline.log`). The city pass's E-heart
baseline had four failing checks. The earlier RI-06 report above is historical.

**Corrections.** Cabinet deliveries now move the rail-yard project to `preparing`.
The Heart bot budgets poles from both cabinet routes, mines copper shortfalls through
ordinary walking/mining/chest commands, and exchanges surplus kits for repair copper
and magazines before starting. The interruption test now uses that carried repair
copper rather than injecting it. These are preparation changes to the instrument,
not free resources or balance changes.

When the yard becomes Held, newly covered abstract hoppers return their rounds to
the buffer. Previously the production step copied and cleared that buffer before
synchronising the turrets, then overwrote the returned rounds: seed 3 lost 200 rounds
at the transition (`evidence/heart-fix/ledger-drop.json`). Synchronisation now happens
before the distribution snapshot. The existing encounter conservation assertion
covers this transition. Completion events retain the actual activation attempt id.
Replay observers see each tick's events before cleanup, so once-only checks can count
the packets and restoration; E-heart also checks completion against its start id.

Two test expectations were corrected: destruction precedes the next block tick's
Held/restored transition, so the completion test waits for both; loading deliberately
pauses, so the resumed comparison explicitly sets the same speed. The description
assertion checks the destroyed/operational facts rather than an obsolete exact string.

E-heart previously used the economy-disabled benchmark while asserting a nonzero
claim charge. Its candidate now enables the game's paid claims from state creation;
the comparison still uses the unchanged E-hour configuration. The report records
both hashes and the candidate configuration, making this distinction explicit.

**Measured.** The focused Heart tests passed 6/6. The first corrected E-heart run
passed 10/10 across seeds 3/4/5, with one attempt, three distinct packets, nine bodies,
90 productive seconds, no stalls, no rifle use, no refusals and no falls on each seed.
The yard restored at 27:10 / 26:50 / 27:05. Every ledger balanced; command replay and
30:00 save/load replay matched the played 75:00 state. Evidence:
`evidence/heart-fix/candidate/experiments/E-heart.json`.

The initial full `npm test` run passed 155/156: only the pre-existing cold-geometry
100 ms timing assertion failed under parallel load (172 ms). The threshold is
unchanged. Typecheck, including the Vite build, and lint passed. The final sequential
test run, complete experiment suite, snapshot and documentation checks are recorded
below when complete.

**Provenance and limits.** Windows, Node 24.20.0 (the installed runtime, not CI's Node
22), HEAD `b3a3f6c8f42a09841102287ac195a424c1f05e1f` plus the uncommitted city rebuild
and these repairs. Experiment files carry the source commit and configuration stamp;
the final E-heart additionally carries its actual candidate/benchmark hashes. No
human play approval, phase gate, reference-machine performance result, calibration
rerun or new browser soak is claimed. The prior city captures remain in
`CITY_REBUILD_REPORT.md`. Player comprehension and the integrated reward sequence
still need the scheduled RI-07/RI-08 work.

The archived-evidence warning was also traced to a Windows path bug in
`packages/tools/src/freshness.ts`: `path.join` produces backslashes, but archive
recognition required forward slashes. Normalising separators restores the checker's
existing archive policy (check ancestry, report the historical hash). Archived
results and their provenance are preserved; active evidence still requires the
current configuration hash.

The normal `npm test` script now uses `--test-concurrency=1`: this suite mixes
correctness tests with the strict 100 ms cold-geometry check, so test workers must
not compete for its CPU time. The unchanged 156 tests all passed in the sequential
run (94.3 s); the original parallel run took 21.9 s but failed the timing assertion.
The standard command is rerun after the experiments so that benchmark has an idle
host. This changes test scheduling, not its budget or expected geometry.

**Final verification.** Results from the repaired working tree:

| Check | Result | Evidence under `evidence/heart-fix/` |
|---|---|---|
| Standard `npm test` | 156/156 passed, 91.7 s; includes all six Heart tests | `tests-final.log`, `standard-test.json` |
| Full experiment suite, seeds 3/4/5 | 15 experiments, zero failing checks | `final/EXPERIMENTS.md`, `experiments-final.log` |
| E-heart | 10/10, including matching completion attempt ids | `final/experiments/E-heart.json` |
| E-hour / E-rifle / E-project | 19/19 / 16/16 / 10/10 | `final/experiments/` |
| Typecheck and Vite build / lint | passed / passed | `typecheck-final.log`, `lint-final.log` |
| Snapshot reproducibility | passed, unchanged | `snapshot.log` |
| Docsync / freshness | passed / passed | `docsync-final.log`, `freshness-final.log` |

The final E-heart candidate configuration hash is `d51dfee0`; its unchanged
E-hour comparison is `e29c6c3f`. The generic experiment stamp remains `b95922d2`.
The suite log includes a long host/tool wait; its wall-clock total is not a
performance measurement. `PROGRESS.md` closes the RI-06 repair and leaves RI-07
next. No commit, push or human gate was made in this pass.
