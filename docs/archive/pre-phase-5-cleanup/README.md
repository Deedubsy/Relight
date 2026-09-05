# Archive — pre-Phase-5 cleanup (2026-09-05)

Files moved or copied here by the pre-Phase-5 cleanup (`PROGRESS.md` T11a) so that the
read path holds one owner per kind of information. **Everything in this folder is
historical.** Instructions, task orders, statuses and rituals written in these files
were true when they were written and are not current: the current rules are
`docs/CONSTITUTION.md`, the current state is `docs/PROGRAMME_STATE.md`, the task list
is `docs/PROGRESS.md`, and the working procedure is the root `CLAUDE.md`.

Nothing here was rewritten. Failed or partial results stay failed or partial. Old
references inside these files (for example `ROADMAP.md` §0, "rule 12", "the current
slice prompt") point at the world as it was; the mapping below says where each thing
went.

## Mapping

| old path | new path | class | why |
|---|---|---|---|
| `docs/ROADMAP.md` | `docs/archive/pre-phase-5-cleanup/ROADMAP.md` | redundant | its §1 phase table duplicated `PHASES.md`; "Where we are", §6 counters and §7 waiting-on-human duplicated `PROGRAMME_STATE.md` / `PROGRESS.md`; §0 already said the task list had moved. The counters (§6) now live in `PROGRAMME_STATE.md` "Counters"; its §7 stale PR line ("PRs #1–#4 open and unmerged") was wrong when archived — #1–#4 were merged 2026-09-03, #5 is open |
| `docs/ROADMAP_OLD.md` | `docs/archive/pre-phase-5-cleanup/ROADMAP_OLD.md` | redundant | an untracked 2026-09-04 copy of `ROADMAP.md` that had never been committed; kept rather than deleted |
| `docs/PROGRESS_SETUP_REPORT.md` | `docs/archive/pre-phase-5-cleanup/PROGRESS_SETUP_REPORT.md` | historical | the report that created `PROGRESS.md`; nothing referenced it |
| `docs/relight-prompt-B-vertical-slice.md` | `docs/archive/pre-phase-5-cleanup/relight-prompt-B-vertical-slice.md` | historical | the Phase 4 slice prompt; Phase 4 is complete (`SLICE_REPORT.md`, `GATE_B.md`). The old `CLAUDE.md` called it "the current slice prompt" |
| `docs/relight-standards-audit.md` | `docs/archive/pre-phase-5-cleanup/relight-standards-audit.md` | historical | the 2026-09-04 audit brief; its outputs are `STANDARDS.md` and `STANDARDS_REPORT.md`, which now cite this path |
| `docs/PROGRAMME_STATE.md` (as of commit 1078b7d) | `docs/archive/pre-phase-5-cleanup/PROGRAMME_STATE-2026-09-05.md` | historical copy | the 808-line per-milestone history (sections 5.1–5.4, B.1–B.32, Rework, 4.1–4.11, Phases 0–3). The live file is now a two-page handoff; the per-milestone record is here and in the phase reports |
| `docs/PROGRESS.md` (as of 1078b7d) | `docs/archive/pre-phase-5-cleanup/PROGRESS-2026-09-05.md` | historical copy | the long "Now" narrative, the "Waiting on the human" list and the eleven log entries. The live file keeps the table with the same task ids and a one-line log per task |
| `docs/DEFERRED.md` (as of 1078b7d) | `docs/archive/pre-phase-5-cleanup/DEFERRED-2026-09-05.md` | historical copy | the twelve original items and the twenty-four "Re-read at …" sections. The live file lists only obligations that are still open |
| `docs/CONSTITUTION.md` (as of 1078b7d) | `docs/archive/pre-phase-5-cleanup/CONSTITUTION-2026-09-05.md` | historical copy | the fourteen-rule text the phase reports cite by number ("rule 12", "rule 14"). The live file keeps the rule numbers and revises the wording |
| `CLAUDE.md` (as of 1078b7d) | `docs/archive/pre-phase-5-cleanup/CLAUDE-2026-09-05.md` | historical copy | the old read order and the eight-step milestone ritual (SLICE_REPORT section, "three decisions for the human", push and PR update) |

Not moved: every phase and milestone report (`PHASE_*_REPORT.md`, `SLICE_REPORT.md`,
`GATE_B.md`, `TEST_RESULTS.md`, `ECONOMY_FIX_REPORT.md`, `GUARDRAILS_REPORT.md`,
`LAYOUT_PASS_REPORT.md`, `REWORK_REPORT.md`, `STANDARDS_REPORT.md`, `CALIBRATION_REPORT.md`,
`FRONT_FIX_REPORT.md`, `PROTOTYPE_TEST_PLAN.md`), because source comments, the freshness
tool and the design doc's changelog cite them by path; `docs/EXPERIMENTS.md`,
`docs/experiments/` (including its own `lattice/` archive), `docs/seeds/`, `docs/section18-*.png`
and `packages/game/public/snapshots/`, because `npm run freshness:check` and CI read them
at those paths; `docs/standards/`, because `STANDARDS.md` cites it row by row.

Git history of every file continues through the move (`git log --follow`).
