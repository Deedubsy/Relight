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
