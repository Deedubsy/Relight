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
(`docs/GATE_B.md` — no walkthrough, no line, no stranger, no played hour); T3 (the layout
pass) was done out of order the same day on the human's instruction. **T1 is done
2026-09-05**: the three rows that walled it — D-P4-9, D-HOUR-2, D-P4-10 — were decided by
Daniel ("Do the blockers") and built, and `npm run experiments` is **13 experiments, 0
failing checks**, so "make main green" is met. Nothing on the critical path is blocked any
more. **T2 is done 2026-09-05** — E-hour is a
no-fall hour on seeds 3, 4 and 5, rifle off and on, and the numbers are written into
`SLICE_REPORT.md`. **The next task is T4** (E-rifle at tile scale: the rescue run and the
steady run), which has no blocker and whose experiment is already 12 / 12. Then T9, which
still needs re-scoping — Gate B was waived, so there is no played hour to fold — and T10.

## Tasks

| id | task | owner | status | blocked by | evidence | done on |
|---|---|---|---|---|---|---|
| T1 | Resolve the eight constant disagreements, rebuild the economy fix, make main green | claude | done | | `docs/ECONOMY_FIX_REPORT.md` §9 | 2026-09-05 |
| T2 | Re-run E-hour until no block falls on seeds 3, 4 and 5 | claude | done | | `docs/SLICE_REPORT.md` section "M6 re-run after the economy fix" | 2026-09-05 |
| T3 | Layout and readability pass | claude | done | | `docs/LAYOUT_PASS_REPORT.md` | 2026-09-05 |
| T4 | E-rifle at tile scale: the rescue run and the steady run | claude | todo | | `docs/EXPERIMENTS.md` row `E-rifle-tile` | |
| T5 | Controls walkthrough on the reference machine | human | done (waived) | T3 | `docs/GATE_B.md` section "Controls walkthrough" |  2026-09-05 |
| T6 | Build a two-assembler ammo line unaided in ten minutes | human | done (waived) | T3 | `docs/GATE_B.md` section "Two-assembler line" |  2026-09-05 |
| T7 | Stranger test: eight questions, a person who has not seen the game | human | done (waived) | T3 | `docs/GATE_B.md` section "Stranger test" |  2026-09-05 |
| T8 | Gate B: play the §11 hour on seed 3 and fill the §19 table | human | done (waived) | T2, T5 | `docs/GATE_B.md` section "Gate B" |  2026-09-05 |
| T9 | Absorb Gate B: fold the played hour into code and doc, tag `[play: Gate B]` | claude | todo | T8 | `docs/PROGRAMME_STATE.md` section "Absorb Gate B" | |
| T10 | Open Phase 5 (the factory, complete) | claude | blocked | T9 | `docs/PHASE_5_REPORT.md` | |

## Waiting on the human

- **Nothing blocks a task.** D-P4-9, D-HOUR-2 and D-P4-10 were decided by Daniel
  on 2026-09-05 ("Do the blockers") and built; with the eight rows of T1's
  original `blocked by` cell they are all `decided`.
- Decision row **D-P4-12** — the rail yard's coal. Added 2026-09-05 by the half
  of D-P4-10 (a) that could not be built: the tile layer has no coal rubble kind
  and no quantity for west's lot, so a physical Excavator there digs steel.
  **Blocks nothing** — the hour is green without it. What it buys back is the
  Generators running dry at 67:51–68:01 in the 75-minute variant.
- **§11's enclosure and the Electricians are now outside the hour.**
  `HOUR_END.held` is 3, so the HQ's white border and the Electricians walking out
  of civic north are beats a 3,600 s run never reaches. Phase 5's opening should
  say whether that is the shape of hour one or a hole in it.
- **The first shade never happens in the bot's hour.** §11 and E3 put it at
  minute 32–47; six runs of E-hour print it as the experiment's only finding, and
  it has now survived the economy fix, the decided minutes and T2's no-fall hour.
  Either §11's minute is wrong for a two-claim hour, or the bot never lets a block
  sleep past d = 0.3 and a human would. **A played hour is what tells them apart**,
  and Gate B was waived.
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
- 2026-09-05 — T1 **done**. The three rows that blocked "make main green" were
  written as `decided` (Daniel, 2026-09-05, the message "Do the blockers", each
  taking its own recommendation) and built: **D-HOUR-2** (a) moved the second
  steel Excavator to minute 12; **D-P4-10** (a) moved north's claim to 65, past
  the hour, so hour one is two claims and `HOUR_END.held` is 3; **D-P4-9** kept
  the block-level hopper through Gate B, so §11's two carried turrets and the
  four functions behind them are gone. The light review also fixed E-hour's
  stock-table header, which was one column longer than its data and read every
  value a step late. **`npm run experiments`: 13 experiments, 231 s, 0 failing
  checks** — E-hour 10 / 10 (was 6 / 10), E-rifle 12 / 12 (was 10 / 12); steel
  bottoms at 30 @ 12:01–12:02, 0 brownout seconds, 0 falls in six runs; snapshot,
  tests (121 / 121), typecheck, lint and docsync green. `E-hour-north`: north
  claimed at 65:00 now **stands to 75:00 on all three seeds**, where it fell at
  72:45–74:38 with the carried turrets. **Half of D-P4-10 (a) could not be built**
  — "west's coal made real" needs a coal rubble kind and a rail-yard quantity that
  do not exist, deferred and asked as **D-P4-12**. Evidence:
  `docs/ECONOMY_FIX_REPORT.md` §9; also `DECISIONS.md` (three rows decided, one
  added), `DEFERRED.md` re-read, and §11's prose in `RELIGHT-design.md`.
- 2026-09-05 — T2 **done**. E-hour re-run on the committed tree: **10 / 10 checks in
  43.9 s**, and on seeds 3, 4 and 5 with the rifle off and on — six runs — **no block
  falls, the chest's steel is never zero (minimum 30 at 12:01–12:02) and there are 0
  brownout seconds and 0 refusals**. End state every run: 3 Held with the HQ standing, 6
  turrets, 4 Generators, 7 Excavators, 3 Assemblers, 519 line magazines; walking
  2.2–3.8 % against §19's 15 %; claim walk-overs 9–14 s; every Gate B replay verdict
  "held anyway". `E-hour-north` (measured, not scored): north claimed at 65:00 is Held at
  65:29–65:34 and **stands to 75:00 on all three seeds, 4 Held**, where the 2026-09-04
  attempt lost it at 72:45–74:38. Determinism confirmed across two invocations — the only
  byte that moved in `docs/experiments/E-hour.json` was the `source_commit` stamp. Nothing
  was changed to make it pass: it passes on the three rows decided on 2026-09-05. One
  finding stands, unchanged and already reported — the first shade never happens against
  §11 and E3's minute 32–47. Evidence: `docs/SLICE_REPORT.md` section "M6 re-run after the
  economy fix", which now carries the passing run (`B-M6-hour-3`) with the 2026-09-04
  attempt kept beneath it as the record.
