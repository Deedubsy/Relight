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

**Now:** Phase 4 (vertical slice). Next task: T1.

## Tasks

| id | task | owner | status | blocked by | evidence | done on |
|---|---|---|---|---|---|---|
| T1 | Resolve the eight constant disagreements, rebuild the economy fix, make main green | claude | doing | decision rows D-P4-4, D-B1-1, D-P4-7, D-P4-8, D-B5-4, D-ENGINE-1, D-INSERTERS-1, D-HOUR-1 must be `decided` first | `docs/ECONOMY_FIX_REPORT.md` | |
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
- Decision rows: none. Every row named in a `blocked by` cell (D-P4-4, D-B1-1,
  D-P4-7, D-P4-8, D-B5-4, D-ENGINE-1, D-INSERTERS-1, D-HOUR-1) has status
  `decided`.

## Log

- (no tasks completed yet)
