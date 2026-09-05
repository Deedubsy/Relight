# Relight — working rules for Claude

Relight is a 2D city-reclamation factory game: a lit block on the river, a front of
street segments to feed with ammo, a factory built from rubble, and a city to relight.
The design is `docs/RELIGHT-design.md`. Priorities, in order: the game the design doc
describes; every rule headless first (`packages/sim`) and measured before it is drawn;
every number with provenance; humans deciding the rules, Claude the implementation.

## Read order (start of every session)

1. `docs/PROGRAMME_STATE.md` — where the programme is, what is built, what is a stand-in,
   and the next task. Two pages.
2. `docs/PROGRESS.md` — the only task list. The first task whose status is `todo` or
   `in_progress` and whose `blocked by` is clear is the current task.
3. `docs/CONSTITUTION.md` — the rules. Stable; skim once you know it.
4. `docs/DECISIONS.md` — the rows the current task's `blocked by` names, and the
   "Outstanding questions" section.
5. The design-doc sections the task names, and the section of
   `docs/REVISED_DEVELOPMENT_PLAN.md` (the adopted plan, D-RI-1) that defines the task;
   `docs/PHASES.md` for the phase's exit criteria. `docs/DEFERRED.md` only when a task
   points at it.

Do not read the archive (`docs/archive/`) or the phase reports to find current state;
they are history and evidence.

## Who owns what

| file | holds | does not hold |
|---|---|---|
| `docs/CONSTITUTION.md` | principles, decision authority, verification policy, status words | gameplay rules, tasks |
| `docs/RELIGHT-design.md` | the current intended rules; labelled implementation limitations and approved-not-built changes | task status |
| `docs/PROGRESS.md` | task status, order, dependencies, acceptance, evidence location | narratives |
| `docs/PROGRAMME_STATE.md` | the two-page handoff: phase, next task, built vs stand-in, evidence, open items, counters | milestone history (that is the phase reports and the archive) |
| `docs/PHASES.md` | each phase's scope and exit criteria | a second task tracker |
| `docs/REVISED_DEVELOPMENT_PLAN.md` | the adopted plan, verbatim, with a provenance preface: the RI task definitions, defaults and labels | task status; it is not edited below its preface |
| `docs/DECISIONS.md` | every decision and open question with provenance and supersession | task history |
| `docs/DEFERRED.md` | obligations with no task yet; a link once one exists | anything already scheduled |
| phase reports, `GATE_*.md`, `TEST_RESULTS.md`, `docs/EXPERIMENTS.md`, `docs/experiments/` | history and evidence | corrections (never rewritten) |

When two disagree: for a rule, a `decided` row beats the design doc, which beats every
other file; for task status, `PROGRESS.md` beats everything; for current state,
`PROGRAMME_STATE.md`. Fix the loser in the same change. A historical report is not
fixed; the current document is, with a line saying so.

## Task flow

Select (first runnable task; if it is `blocked`, name the blocker and go to the next
runnable one; if its owner is `human`, say what it needs and stop) → set `in_progress`
→ implement → verify (cheap checks after any meaningful code change; the focused
experiment when the change touches what it measures — constitution "Verification
policy") → review the diff (accidental gameplay change, a rule that does not surface
in-game, a missing tag, a fabricated approval) → update records (`PROGRESS.md` status
and one log line; `PROGRAMME_STATE.md` "Now"; `DECISIONS.md` rows touched; the design
doc's changelog for any doc edit) → commit → report → stop. `/next` runs this.

## Commands

All from the repo root, after `npm install`. Verified on 2026-09-05 by running them
during the cleanup (T11a) and again during RI-01 (every row, the same day).

| command | what | last run (2026-09-05) |
|---|---|---|
| `npm test` | node:test across sim / harness / tools (124 tests) | RI-01: 124 pass |
| `npm run typecheck` | `tsc --strict` on every package, then the game build | RI-01: green |
| `npm run lint` | eslint on sim, harness, tools | RI-01: green |
| `npm run docsync:check` | the design doc's generated tables and guarded prose sentences match `packages/sim` (`npm run docsync` regenerates); every `[play: <gate>]` tag names a recorded gate | RI-01: green (~3 s) |
| `npm run freshness:check` | every generated file's `source_commit` is an ancestor of HEAD and its `config_hash` current | RI-01: green (~4 s) |
| `npm run snapshot:check` | `packages/game/public/snapshots/b-compact-seed3.json` still reproduces | RI-01: green after `npm run snapshot` regenerated it (five new counters, state identical) |
| `npm run experiments` | E1–E9, E-hour, E-rifle, E-variance, E-walk → `docs/EXPERIMENTS.md` (a red experiment is a red build) | RI-01: 13 experiments, 0 failing, ~4 min |
| `npm run calibrate -- --out docs/experiments/calibration.json --md docs/experiments/calibration.md` | the bot calibration | not run |
| `npm run seeds`, `npm run section18`, `npm run replay`, `npm run nightly` | seed images, §18 images, hour replay, the nightly runs | not run |
| `npm run dev` / `npm run build` | the game (`packages/game`, Vite) | not run |

Do not invent commands. Do not claim a command ran if it was only inspected; report the
actual output, including a red result. The Playwright soak (verification pass only) runs
against the Vite preview on port 4173 (`?view=world&seed=3`), started from `packages/game`.

## The sim / renderer boundary

`packages/sim` is pure: `step(state, commands)` and nothing else changes state.
`packages/game` only draws state and sends commands. Bots and experiments use the same
commands, inventories and reach a player has. Never bypass a gameplay command or grant a
bot stock, power or placements a player cannot get; if a scenario needs that, the run is
labelled a scenario in its report and is not evidence for a player-facing claim.

## Four things "done" can mean

*Implementation complete* · *automated validation* (`not_run` / `passed` / `failed` /
`partial`, naming the run) · *human approval* (person, date, record) · *human play
evidence* (a played session's observation). Say which you have. A file existing is not a
pass. Statuses: tasks `todo` / `in_progress` / `blocked` / `done` / `waived`.

## Stop and ask

A core rule, progression, scope or go/no-go decision the task needs · a change that would
move a fixture, a `[play: …]` lock, a decided constant or an expected result · a check red
for a reason outside the task · a mechanic or benchmark change the adopted plan does not
name · anything destructive or outward-facing (push, merge, deleting evidence, external
trackers). Otherwise proceed and
record the choice. One unresolved question blocks the work that depends on it, not the
session.

## Handoff

Before ending a session: `PROGRAMME_STATE.md` "Now" is true, `PROGRESS.md` carries the
real statuses, and the final message names the next task and whether it is ready or
blocked on what.

## Commits

One commit per task, message named after the task. Trailers:

```
Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_018pVusisgbVYVEpZ9PFYF1w
```

No push, merge or PR change unless the human asks. Branch `ri-pass-1` carries the RI
tasks (RI-01 …) on top of `phase-4`, which carries the open PR #5 (#1–#4 merged
2026-09-03).

## Working habits

Run Bash from the repo root, prefixing commands with `cd /mnt/e/Factorio2 &&` or using
absolute paths (the shell's cwd can drift); in bypass-permissions mode prefer Bash (cat,
sed, python3 heredocs) over the Read/Edit/Write tools. Temporary scripts go in the session
scratchpad, never the repo. Soak hygiene: idle host, background Bash, kill by PID, never
`pkill -f`; the reference-machine soak is by hand. Give a one-line progress update every
few minutes of work.
