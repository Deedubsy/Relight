# PROGRESS setup report — the task list and the `/next` command

Task: the user's message of 2026-09-05, "create PROGRESS.md and the /next command". Five steps, one commit each on `phase-4`: 16c02c9 (Step 1), 4e8702f (Step 2), 5b5c367 (Step 3), 523764a (Step 4), and the commit that carries this report (Step 5). No code changed; no number in the design doc changed; nothing was decided.

## 1. What PROGRESS.md contains

`docs/PROGRESS.md` has five parts: the header block and its six rules, a **Now** line (`Phase 4 (vertical slice). Next task: T1.`), the task table, the "waiting on the human" list, and the log (`- (no tasks completed yet)`).

The ten task rows, as written:

| id | task | owner | status | blocked by | evidence | done on |
|---|---|---|---|---|---|---|
| T1 | Resolve the eight constant disagreements, rebuild the economy fix, make main green | claude | todo | decision rows D-P4-4, D-B1-1, D-P4-7, D-P4-8, D-B5-4, D-ENGINE-1, D-INSERTERS-1, D-HOUR-1 must be `decided` first | `docs/ECONOMY_FIX_REPORT.md` | |
| T2 | Re-run E-hour until no block falls on seeds 3, 4 and 5 | claude | todo | T1 | `docs/SLICE_REPORT.md` section "M6 re-run after the economy fix" | |
| T3 | Layout and readability pass | claude | todo | | `docs/LAYOUT_PASS_REPORT.md` | |
| T4 | E-rifle at tile scale: the rescue run and the steady run | claude | todo | | `docs/EXPERIMENTS.md` row `E-rifle-tile` | |
| T5 | Controls walkthrough on the reference machine | human | todo | T3 | `docs/GATE_B.md` section "Controls walkthrough" | |
| T6 | Build a two-assembler ammo line unaided in ten minutes | human | todo | T3 | `docs/GATE_B.md` section "Two-assembler line" | |
| T7 | Stranger test: eight questions, a person who has not seen the game | human | todo | T3 | `docs/GATE_B.md` section "Stranger test" | |
| T8 | Gate B: play the §11 hour on seed 3 and fill the §19 table | human | todo | T2, T5 | `docs/GATE_B.md` section "Gate B" | |
| T9 | Absorb Gate B: fold the played hour into code and doc, tag `[play: Gate B]` | claude | todo | T8 | `docs/PROGRAMME_STATE.md` section "Absorb Gate B" | |
| T10 | Open Phase 5 (the factory, complete) | claude | blocked | T9 | `docs/PHASE_5_REPORT.md` | |

The "waiting on the human" list holds T5, T6, T7 and T8, and no decision row: every row named in a `blocked by` cell (the eight of T1) has status `decided`, by Daniel, on 2026-09-04, via the message "fix the eight constant disagreements".

`docs/GATE_B.md` (Step 2) is the evidence file T5–T8 name. It holds six headings and nothing but the prompt text under each: Controls walkthrough, Two-assembler line, Stranger test, Gate B — the §19 first-hour table, Gate B — the extra rows, Verdict (`verdict:`).

## 2. Files edited to point at it

**`CLAUDE.md`** — read item 2 replaced:

- was: ``2. `docs/ROADMAP.md`, section 0 "Next actions" — the ordered list of what to do next.``
- now: ``2. `docs/PROGRESS.md` — the task list. The top task that is not done is the next task.``

The session's first action, replaced:

- was: `**Then do the top line of ROADMAP section 0 that is not ticked.** If that line's owner is "human", stop, tell the user what the line is, and do nothing else. If the line is owned by Claude Code, do it.`
- now: ``**Then do the top task in `docs/PROGRESS.md` whose status is not done.** If its owner is human, stop and tell the user which task it is and what it needs. If its owner is claude and it is blocked, stop and name what it is blocked by.``

Read item 4 was also stale, because section 0 no longer names a prompt (not in the step's list; changed so the read list still works, and reported here):

- was: `4. The phase prompt named in ROADMAP section 0.`
- now: ``4. The phase prompt named in the current task of `docs/PROGRESS.md`, if it names one.``

**`docs/ROADMAP.md`** — the whole of section 0 (eight task lines, the tick rule and their changelog text) replaced by:

```
## 0. Next actions

The task list has moved to `docs/PROGRESS.md`. That file is the only place
task order lives. This file explains the programme; PROGRESS.md says what
happens next.
```

**`docs/CONSTITUTION.md`** — one line added to "Cross-cutting", after the `DECISIONS.md` bullet:

- `* **PROGRESS.md** is the only task list. A task is done when its evidence file contains the result, not when someone says so. Only a human marks a human-owned task done.`

**`docs/PROGRAMME_STATE.md`** — added at the top of the status line:

- ``**Task list: `docs/PROGRESS.md`.**``

The current-phase line (line 5) still pointed at the deleted section; its live pointer was moved (not in the step's list; reported here):

- was: `ROADMAP §0 lines 5–7 (the controls walkthrough, the two-assembler line, Gate B — play the hour, fill §19, write the verdict)`
- now: ``` `docs/PROGRESS.md` T5, T6 and T8 (the controls walkthrough, the two-assembler line, Gate B — play the hour, fill §19, write the verdict) ```

`grep -rn "ROADMAP.md section 0" .` returns nothing.

## 3. The /next command

`.claude/commands/next.md` — reads `docs/PROGRESS.md`, takes the first task whose status is not `done`, and then either prints what a `blocked` task waits for, or prints a `human` task with its evidence file and the heading the result goes under, or (for a `claude` task) marks it `doing`, checks any decision rows in its `blocked by` cell, does the task, writes the evidence, runs the cheap checks, marks it `done` with the date and a log line, commits, reports, and stops.

## 4. Anything that blocked a step

Nothing blocked a step. All five ran in order. Six things are worth recording:

1. **Three tasks' evidence files already exist, and the table still says `todo`.** The rows were written exactly as the task specified, and nothing was re-scored: `docs/ECONOMY_FIX_REPORT.md` (T1) exists and says main is **not** green; `SLICE_REPORT.md` "M6 re-run after the economy fix" (T2) exists and says **No-fall hour: no**; `EXPERIMENTS.md` has the `E-rifle-tile` rows (T4) at 10 / 12 checks. Under the file's own rule 2 a task is `done` only when its evidence contains **the result**, which in all three cases is a failure, so `todo` is consistent. T3's evidence file, `docs/LAYOUT_PASS_REPORT.md`, does not exist at all — the layout pass was reported inside `SLICE_REPORT.md`.
2. **`docs/layout-pass/STRANGER_TEST.md`**, named in `GATE_B.md`'s stranger-test prompt, does not exist yet. The eight questions have not been written; T3 is where they would land.
3. **Two edits beyond the step's list**, both stale pointers to the deleted ROADMAP section, both quoted in §2: `CLAUDE.md` read item 4, and the live pointer on `PROGRAMME_STATE.md`'s current-phase line.
4. **Two recommended decision rows are not in the "waiting on the human" list**: D-P4-10 (north's minute and the coal after the HQ patch) and D-HOUR-2 (the minute-15 steel cluster). The list is built from the `blocked by` column, and no task names them. The last E-hour run says T2 cannot reach "no block falls" until one of them settles.
5. **`ROADMAP.md` §7 "What is waiting on a human right now"** still holds a human-work table that overlaps T5–T8 and cites `SLICE_REPORT.md` as Gate B's evidence file, where `PROGRESS.md` now says `GATE_B.md`. Section 7 was left alone; the step named section 0 only.
6. **The `/next` command was not run.** Step 5 says the user runs it. It was verified by path and content only. The four cheap checks were run after the edits and are green: `npm test` 121 / 121, `npm run typecheck`, `npm run lint`, `npm run docsync:check`.
