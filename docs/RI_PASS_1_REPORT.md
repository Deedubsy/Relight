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
