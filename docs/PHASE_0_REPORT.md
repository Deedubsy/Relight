# Phase 0 report — inventory

Date 2026-09-03. Phase 0 built nothing and changed no code and no line of `RELIGHT-design.md`. It read, ran what could be run, and wrote four files: `PROGRAMME_STATE.md`, `DECISIONS.md`, phase annotations in `DEFERRED.md`, and this report.

## Built

Nothing. Four programme files created or annotated (above). The full inventory, untagged set, open-constants list and the §24/§25 → phase maps are in `PROGRAMME_STATE.md` §0.1–0.6; this report summarises and adds what a human has to act on.

## Found (the inventory in one paragraph)

The programme is further along than "Phase 0" implies. A Python reference sim (`frontsim.py`) models §5/§7 block rules, the ammo ring and a power model with shedding, and is the source of every `[sim]` tag in the doc. A pure-TS port (`packages/sim`) reproduces it bit-for-bit on three seed fixtures (`npm test` 28/28 today), adds six claim bots, the 80 % build rule, machine slots, finite rubble and the magazine recipe, but does **not** carry the power model, the well-death rule or the 10 s bloom interleave. A Phaser map-view prototype (`packages/proto`) builds clean today and implements almost all of the constitution's Phase 2 spec except snapshot loading and survivor markers; no human has played it, and Gate A (`TEST_RESULTS.md`) is `verdict: pending`. Seven reports document the fixes, calibrations and assumptions. Against the constitution, Phase 1 is roughly two-thirds done and unverified in CI, and Phase 2 is built and ungated.

Things the constitution assumes that are not there: a git repository, CI, `docs/`, `packages/harness`, `packages/tools`, `EXPERIMENTS.md`.

## Verified today

| Check | Result |
|---|---|
| `npm test` (fixture regression seeds 3/4/5 + unit tests) | 28 / 28 pass, ~4 s |
| `npm run build` (`tsc --strict` on sim and proto, Vite bundle) | clean; 1.53 MB bundle, chunk-size warning only |
| `python3 frontsim.py` one sim-hour, seed 3, compact, doc flags | runs, 0.23 s |
| Python `--experiments` suite | **not re-run** (results exist only in the reports) |
| Proto in a browser | **not run** |

## Assumed (`GAME-ASSUMPTION`)

Phase 0 made none of its own. It inherits fourteen `PROTO-ASSUMPTION`s from `PROTOTYPE_BUILD_REPORT.md` (rubble yield, stone sink, claim cost, assembler cost, start stock and patch, proto config, pip per edge, "closes N", telemetry after-values, shape metrics, facility placement, facility reached, distinct-shape thresholds) and one from `bots.ts` (build at 80 % of production). None is retagged here; Phase 2's report retags the ones the game keeps.

## Contradictions found in the doc (not fixed; Phase 1 doc pass)

1. §7 and §17 quote 3.2× / 1.31× (8-minute cadence) under the same run tag that §9, §19 and §27 quote as 2.2× / 0.97× (5-minute). The changelog line that updated §9 is labelled "§7".
2. §24 risk 6 and §25 item 7 carry the 8-minute figures (56 %, 131 %).
3. §25 item 13 is "closed" on a result the later calibration reversed (spike now loses 4–11 blocks). Reopened as D-25-13.
4. §15 says 4.5 MW at 3 h and 5.5/7.6 MW mid-game at the pre-D1 draw; §12 says 1.0–3.1 / 3.1–5.5 at the D1 draw.
5. §4 says a city is 24×24 cells; §18 and both sims are 24×22.
6. §13 big pole "supplies 3×3" is smaller than the pole's 7×7.
7. The constitution's Phase 4 text says belts run at 7.5/s; the doc says 8/s. The doc is the spec.

## Deferred

`DEFERRED.md`: all twelve prototype items now carry a phase (world view, belts, machines → Phase 4/5; light map → Phase 4 M5; power → Phase 1 sim + Phase 4 M3; trams → 7; blueprints and Line truck → 8; cannons → 6; the Relight → 10; art → 12; stone sinks → 5). Two items added: the legacy Python backup (delete in Phase 1) and the Python reference sim itself (retire once the TS suite reproduces every tagged number). Nothing deleted.

## Measured

Nothing new. The untagged set is **70 entries** across §5 (24), §7 (7), §12 (9), §13 (16 machine rows), §14 (5), §15 (9); four of them have evidence in a report but no tag, two are stale tagged numbers that need replacing. Ten open constants (C1–C10) are logged with their doc values.

## §26 recount

Three systems: the front, found tech, automated combat. Complexity 5/10, unchanged; Phase 0 added nothing.

## Three decisions for a human

1. **Ratify or reverse D1–D4** (`DECISIONS.md`): substation draw 100/20 kW with four start Generators, fall time 90 s, per-block arrival counter with a 60 s refeed reset, Relight survivable by banking. A prior session applied these to the doc from calibration evidence; rule 7 says a human decides constants. Phase 1's TS power model and experiment E4 encode D1 and D2, so this blocks Phase 1.
2. **D-25-13**: at the 5-minute cadence a spike on the scattered map loses 4–11 blocks and pays 2.2× compact's ammo. Either the doc says "a straight push bleeds blocks" and §7/§17/§25 are rewritten to the 2.2× figures, or that is the wrong tuning and dmax/g/cadence change. `CALIBRATION_REPORT.md` said this "needs a human, not a retag". Blocks the Phase 1 doc pass.
3. **D-CI**: the directory is not a git repository and has no CI. Rule 3 and Phase 1's definition of done need both. Decide `git init` here versus a hosted remote, and which CI runs the experiment suite. Blocks Phase 1's DoD.

## What Phase 1 starts with

- Port the power model to `packages/sim` (Python `--power`, `--draw`, `--shed`), add the well-death rule and bloom interleave or delete them from §5.
- `EXPERIMENTS.md` with both names per row (constitution E-number and doc run tag; proposed map in `PROGRAMME_STATE.md` §0.8).
- Reproduce E1–E9 in TS from the CLI, in CI, and retag the doc's stale numbers (contradictions 1–4 above).
- `packages/harness` from `packages/sim/test/calibrate.ts`.
- Move reports under `docs/` if the human agrees (root stubs stay).
