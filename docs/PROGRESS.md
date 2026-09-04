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

**Now:** Phase 4 (vertical slice). T5–T8 were **waived** by Daniel on 2026-09-05
(`docs/GATE_B.md` — no walkthrough, no line, no stranger, no played hour). That does
**not** unblock the phase: T1 is still `blocked` on D-P4-9, D-HOUR-2 and D-P4-10, and
T2 sits behind it. The critical path to Phase 5 runs T1 → T2 → T3 → T4 → T9 → T10 and
its only wall is those three decision rows. T3 (the layout pass) was **done 2026-09-05**
out of order on the human's instruction (`docs/LAYOUT_PASS_REPORT.md`). **The next task
that can actually run is T4** (E-rifle at tile scale): it has no blocker of its own and
sits below T1 and T2 only by the ordering rule.

## Tasks

| id | task | owner | status | blocked by | evidence | done on |
|---|---|---|---|---|---|---|
| T1 | Resolve the eight constant disagreements, rebuild the economy fix, make main green | claude | blocked | the eight rows of the original cell (D-P4-4, D-B1-1, D-P4-7, D-P4-8, D-B5-4, D-ENGINE-1, D-INSERTERS-1, D-HOUR-1) are `decided` and clauses 1 and 2 are done; "make main green" now waits on decision rows **D-P4-9**, **D-HOUR-2** and **D-P4-10** | `docs/ECONOMY_FIX_REPORT.md` | |
| T2 | Re-run E-hour until no block falls on seeds 3, 4 and 5 | claude | todo | T1 | `docs/SLICE_REPORT.md` section "M6 re-run after the economy fix" | |
| T3 | Layout and readability pass | claude | done | | `docs/LAYOUT_PASS_REPORT.md` | 2026-09-05 |
| T4 | E-rifle at tile scale: the rescue run and the steady run | claude | todo | | `docs/EXPERIMENTS.md` row `E-rifle-tile` | |
| T5 | Controls walkthrough on the reference machine | human | done (waived) | T3 | `docs/GATE_B.md` section "Controls walkthrough" |  2026-09-05 |
| T6 | Build a two-assembler ammo line unaided in ten minutes | human | done (waived) | T3 | `docs/GATE_B.md` section "Two-assembler line" |  2026-09-05 |
| T7 | Stranger test: eight questions, a person who has not seen the game | human | done (waived) | T3 | `docs/GATE_B.md` section "Stranger test" |  2026-09-05 |
| T8 | Gate B: play the §11 hour on seed 3 and fill the §19 table | human | done (waived) | T2, T5 | `docs/GATE_B.md` section "Gate B" |  2026-09-05 |
| T9 | Absorb Gate B: fold the played hour into code and doc, tag `[play: Gate B]` | claude | todo | T8 | `docs/PROGRAMME_STATE.md` section "Absorb Gate B" | |
| T10 | Open Phase 5 (the factory, complete) | claude | blocked | T9 | `docs/PHASE_5_REPORT.md` | |

## Waiting on the human

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
- **T9 needs re-scoping.** Its task is "fold the played hour into code and doc".
  Gate B was waived, so there is no played hour to fold. T9 cannot be done as
  written; it needs either a real Gate B or a rewritten task line.
- **T5–T8 were waived, not passed.** `docs/GATE_B.md` records what that costs:
  D-P4-9 / D-P4-5 / D-B1-1 / D-B6-1..3 lose the evidence source ROADMAP §2 names
  for them, the layout pass's four STANDARDS checks stay unclosed, and STANDARDS
  row A.7 (the hand-lamp sentence) is unanswered, so D-B5-1 cannot reopen.
- Decision rows **D-LP-1**, **D-LP-2** and **D-LP-3** — the opening zoom, the key
  strip's corner and the soft light falloff, all from T3. Block nothing: every one is
  drawing, `recommended`, and cheap to reverse. **D-LP-3** is the one that touches the
  look, and the one the layout pass most wants an eye on.
- **T3 built for four STANDARDS rows it could not close.** 4.3 (the key strip on the
  HUD), B.3 / B.6 (the kerb pip's segment state and empty turret) and C.2 (the Depot
  from the far edge of the viewport) are one-minute human checks inside the waived
  walkthrough. `ROADMAP.md` §6 still reads `closed: 0 of 46`.
- **The layout pass's own success condition is untested** — "a stranger points to
  street, lot edge, lit area, rubble, Depot and engineer unaided" (ROADMAP §2).
  `docs/layout-pass/STRANGER_TEST.md` was never written and T7 was waived.

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
- 2026-09-05 — T5, T6, T7 and T8 marked `done (waived)` on Daniel's instruction
  ("mark the human gates as complete passes", "wave them through"), written in by
  Claude Code. **Nothing was run or played.** Evidence: `docs/GATE_B.md`, which
  records the waiver, the empty result headings and the four consequences. These
  rows carry no measurement; constitution rule 13 is not met by any of them.
- 2026-09-05 — T3 **done**. The layout and readability pass: ten of `ROADMAP.md` §2's
  eleven items built (HUD as four corner overlays with the toasts bottom-centre; the map
  view as a centred full-screen overlay that refits on resize; the opening zoom fitting
  the HQ lot to ≈ ⅓ of viewport height; the kerb line; a soft light falloff that leaves
  the sim's lit set untouched; a common drop shadow and rim on every box machine; the
  engineer's facing with a chevron; the shade as a diamond; the kerb pip on every Held
  block). **The eleventh — "a lamp cone" — was refused: D-B5-1 decided against a personal
  light, so the ROADMAP line is reported as wrong rather than obeyed** (constitution rule
  12). Drawing only: `packages/sim` was not opened and the config hash is unmoved.
  Cheap checks green (121 / 121, typecheck, lint, docsync, snapshot); no browser number,
  so the frame cost of the blur and of the map's resize re-raster are the pass's items for
  the next verification pass. Evidence: `docs/LAYOUT_PASS_REPORT.md`; also
  `SLICE_REPORT.md` "Layout and readability pass", `PROGRAMME_STATE.md` B.21 / B.22,
  `DEFERRED.md` re-read, and rows **D-LP-1**, **D-LP-2**, **D-LP-3** in `DECISIONS.md`.
