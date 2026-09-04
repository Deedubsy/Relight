# PROGRESS — the only task list

This file is the single source for what to do next. Nothing else holds the task
order. `ROADMAP.md` explains the programme; this file says what happens next.

Rules for this file:
1. Tasks are done in the order listed. The top task that is not `done` is the
   next task.
2. A task is marked `done` only when the file named in its `evidence` column
   exists and contains the result. Saying it is finished does not mark it done.
3. Only a human may mark a `human` task done. Claude Code may mark a `claude`
   task done after writing its evidence.
4. A task marked `blocked` names what it is waiting for. It cannot start until
   that thing is done or decided.
5. When a task is marked done, add one line to the log at the bottom of this
   file, naming the evidence.
6. New tasks are added at the position they must run, not at the end.

**Now:** Phase 4 (vertical slice). T1 is `blocked` on three decision rows (see below); the
next task that can start is T2, which is blocked by T1. Nothing moves until the human
writes D-P4-9, D-HOUR-2 and D-P4-10.

## Tasks

| id | task | owner | status | blocked by | evidence | done on |
|---|---|---|---|---|---|---|
| T1 | Resolve the eight constant disagreements, rebuild the economy fix, make main green | claude | blocked | the eight rows of the original cell (D-P4-4, D-B1-1, D-P4-7, D-P4-8, D-B5-4, D-ENGINE-1, D-INSERTERS-1, D-HOUR-1) are `decided` and clauses 1 and 2 are done; "make main green" now waits on decision rows **D-P4-9**, **D-HOUR-2** and **D-P4-10** | `docs/ECONOMY_FIX_REPORT.md` | |
| T2 | Re-run E-hour until no block falls on seeds 3, 4 and 5 | claude | todo | T1 | `docs/SLICE_REPORT.md` section "M6 re-run after the economy fix" | |
| T3 | Layout and readability pass | claude | todo | | `docs/LAYOUT_PASS_REPORT.md` | |
| T4 | E-rifle at tile scale: the rescue run and the steady run | claude | todo | | `docs/EXPERIMENTS.md` row `E-rifle-tile` | |
| T5 | Controls walkthrough on the reference machine | human | todo | T3 | `docs/GATE_B.md` section "Controls walkthrough" | |
| T6 | Build a two-assembler ammo line unaided in ten minutes | human | todo | T3 | `docs/GATE_B.md` section "Two-assembler line" | |
| T7 | Stranger test: eight questions, a person who has not seen the game | human | todo | T3 | `docs/GATE_B.md` section "Stranger test" | |
| T8 | Gate B: play the §11 hour on seed 3 and fill the §19 table | human | todo | T2, T5 | `docs/GATE_B.md` section "Gate B" | |
| T9 | Absorb Gate B: fold the played hour into code and doc, tag `[play: Gate B]` | claude | todo | T8 | `docs/PROGRAMME_STATE.md` section "Absorb Gate B" | |
| T10 | Open Phase 5 (the factory, complete) | claude | blocked | T9 | `docs/PHASE_5_REPORT.md` | |

## Waiting on the human

- T5 — Controls walkthrough on the reference machine.
- T6 — Build a two-assembler ammo line unaided in ten minutes.
- T7 — Stranger test: eight questions, a person who has not seen the game.
- T8 — Gate B: play the §11 hour on seed 3 and fill the §19 table.
- Decision row **D-P4-9** — turrets on claimed blocks. Blocks T1. §11's two
  carried turrets are what loses north: removing them is measured green (no
  falls, four Held, every run); keeping them costs three runs of six.
- Decision row **D-HOUR-2** — the minute-15 steel cluster. Blocks T1. The
  decided 200 start steel is the decided minute list's bill to minute 15
  exactly, so the chest reads zero at 15:02–15:04.
- Decision row **D-P4-10** — north at 40:00 against Generator 4 at 45:00.
  Blocks T1. Exactly 300 s of brownout, by construction.
- The eight rows of T1's original `blocked by` cell (D-P4-4, D-B1-1, D-P4-7,
  D-P4-8, D-B5-4, D-ENGINE-1, D-INSERTERS-1, D-HOUR-1) are all `decided`.

## Log

- 2026-09-05 — T1 started and blocked. Clauses 1 ("the eight constant
  disagreements") and 2 ("rebuild the economy fix") are done and were already
  on the branch. Clause 3 ("make main green") is not: three hour-bot defects
  were found and fixed (the hands never stopped after §11's 20 steel; six kits
  could never fit in 40 stacks, so north's ring was born part-unkitted; the
  hand-feed beat watched the HQ alone), which took the falls from six runs of
  six to three and north's kit from `never` to 40:29–40:34 — but the six
  remaining red checks are decided-constant arithmetic, not defects. Evidence:
  `docs/ECONOMY_FIX_REPORT.md` §8. T1 is `blocked` on D-P4-9, D-HOUR-2 and
  D-P4-10.
