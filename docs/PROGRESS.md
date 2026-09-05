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
(`docs/GATE_B.md` — no walkthrough, no line, no stranger, no played hour), and **T8's
waiver was lifted the same day when he played it and closed it**; T5, T6 and T7 stay
waived. T3
(the layout pass) was done out of order the same day on the human's instruction. **T1 is done
2026-09-05**: the three rows that walled it — D-P4-9, D-HOUR-2, D-P4-10 — were decided by
Daniel ("Do the blockers") and built, and `npm run experiments` is **13 experiments, 0
failing checks**, so "make main green" is met. Nothing on the critical path is blocked any
more. **T2 is done 2026-09-05** — E-hour is a
no-fall hour on seeds 3, 4 and 5, rifle off and on, and the numbers are written into
`SLICE_REPORT.md`. **T4 is done 2026-09-05** — at tile scale the rifle decides a rescue
(48 runs: 7 blocks fall without it, 4 with it, 3 saved and 3 delayed, every fall `unfed`
and never a shade), and the tile steady table now scores §19's two caps on §11's own hour,
taking E-rifle to 14 / 14. **T8 — Gate B — is done 2026-09-05, `verdict: proceed — with
conditions`, and Daniel closed it as fully tested the same day** (`docs/GATE_B.md`). It
**closed STANDARDS row A.7** (the dark reads as the claim's price; D-B5-1 stands) and
**opened D-GB-1 … D-GB-4**. **D-GB-1 is decided as the hybrid**: the claim still burns off
its block, and powering and lighting the next area becomes a physical expedition the
engineer walks, with lighting-up as a threat trigger. Of §19's first-hour table only the
**0–10 window carries the player's words** — the later windows were not recorded and stay
empty; the gate is closed on the owner's judgement, not on a full table, and the file says
so. **T9 is the next task** — it folds the hour and the decided loop change into code and
doc — and T10, which opens Phase 5, follows it.

## Tasks

| id | task | owner | status | blocked by | evidence | done on |
|---|---|---|---|---|---|---|
| T1 | Resolve the eight constant disagreements, rebuild the economy fix, make main green | claude | done | | `docs/ECONOMY_FIX_REPORT.md` §9 | 2026-09-05 |
| T2 | Re-run E-hour until no block falls on seeds 3, 4 and 5 | claude | done | | `docs/SLICE_REPORT.md` section "M6 re-run after the economy fix" | 2026-09-05 |
| T3 | Layout and readability pass | claude | done | | `docs/LAYOUT_PASS_REPORT.md` | 2026-09-05 |
| T4 | E-rifle at tile scale: the rescue run and the steady run | claude | done | | `docs/EXPERIMENTS.md` row `E-rifle-tile` | 2026-09-05 |
| T5 | Controls walkthrough on the reference machine | human | done (waived) | T3 | `docs/GATE_B.md` section "Controls walkthrough" |  2026-09-05 |
| T6 | Build a two-assembler ammo line unaided in ten minutes | human | done (waived) | T3 | `docs/GATE_B.md` section "Two-assembler line" |  2026-09-05 |
| T7 | Stranger test: eight questions, a person who has not seen the game | human | done (waived) | T3 | `docs/GATE_B.md` section "Stranger test" |  2026-09-05 |
| T8 | Gate B: play the §11 hour on seed 3 and fill the §19 table | human | **done** | T2, T5 | `docs/GATE_B.md` section "Gate B" |  2026-09-05 |
| T9 | Absorb Gate B: fold the played hour and D-GB-1's hybrid into code and doc, tag `[play: Gate B]`, and carry D-GB-2/3/4 into Phase 5 as conditions | claude | doing | | `docs/PROGRAMME_STATE.md` section "Absorb Gate B" | |
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
- **The first shade never happens in the bot's hour**, and Gate B closed without
  contradicting it. §11 and E3 put it at minute 32–47; six runs of E-hour print
  "never"; no shade kills anything in 48 tile rescue runs and none is born in six
  hours. The question is "does a human let a block sleep past d = 0.3 where the bot
  never does". **It is no longer a gate debt** — the gate is closed — but it is
  still the one E-hour finding a bot cannot settle. It rides into **Phase 5** with
  the walking share, the burn-off line and §19's unrecorded windows; any session
  that reaches minute 45 answers all four at once, whenever one happens.
- Row **D-R1** (the rifle and the shade) is still `provisional` and now has tile
  evidence attached: no shade kills anything in 48 rescues, no shade is born in six
  hours. Blocks nothing; option (a) is already what the code does. Deciding it is a
  signature, and the caveat is that both runs are bots.
- **§5's rescue sentence quotes the block sim** — "a whole belt 90 s away falls in
  ~1.5 min to a shade at an unlit edge, and the rifle cannot save it". The tile sim
  says a 90 s ring never falls at all and the killer is always crawlers. Both are
  tagged to their own run, so nothing is wrong as written; which sim §5 quotes is a
  human's word. Blocks nothing.
- **The 600 s belt is the rifle's only justification in evidence.** It is not §11's
  rescue (§11's is 90 s, at which the experiment cannot tell an armed engineer from
  an unarmed one). Keep it, drop it, or rename it — the human's call; §24 risk 10
  rests on it.
- ~~**T9 needs re-scoping.**~~ **Done 2026-09-05**: Gate B was played and closed,
  the verdict is `proceed — with conditions`, and T9's line now reads "fold the
  played hour and D-GB-1's hybrid into code and doc … and carry D-GB-2/3/4 into
  Phase 5 as conditions". It is unblocked, and it is the last Phase 4 task.
- **D-GB-2, D-GB-3 and D-GB-4 are gate conditions, not blockers.** They ride into
  Phase 5 by the verdict. D-GB-2 (the next-objective problem) is the one that
  touches a standing rule — constitution rule 8, "no tutorial screens" — and the
  rule-8-legal forms go first; a quest system is yours to take. D-GB-4 needs no
  permission at all: §19's shooting cap is 10 % of an hour and the measured hour
  is 0.50 / 0.69 / 0.06 %, so density has twenty-fold headroom inside the doc's
  own limits.
- **T5, T6 and T7 were waived, not passed** (T8 was played and is closed).
  `docs/GATE_B.md` records what that costs: D-P4-9 / D-P4-5 / D-B1-1 / D-B6-1..3
  lose the evidence source ROADMAP §2 names for them, and the layout pass's four
  STANDARDS checks (4.3, B.3, B.6, C.2) stay unclosed inside the waived
  walkthrough. **A.7 is no longer on that list** — the gate answered it and it is
  closed, the first of the 46.
- Decision rows **D-LP-1**, **D-LP-2** and **D-LP-3** — the opening zoom, the key
  strip's corner and the soft light falloff, all from T3. Block nothing: every one is
  drawing, `recommended`, and cheap to reverse. **D-LP-3** is the one that touches the
  look, and the one the layout pass most wants an eye on.
- **T3 built for four STANDARDS rows it could not close.** 4.3 (the key strip on the
  HUD), B.3 / B.6 (the kerb pip's segment state and empty turret) and C.2 (the Depot
  from the far edge of the viewport) are one-minute human checks inside the waived
  walkthrough. `ROADMAP.md` §6 now reads `closed: 1 of 46` — **A.7 closed at Gate
  B, 2026-09-05**; these four did not.
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
- 2026-09-05 — T4 **done**. E-rifle at tile scale, both runs, on a commit rather than a
  working tree: **13 experiments, 256 s, 0 failing checks**, E-rifle **14 / 14** in 96.6 s.
  **The rescue**: 24 scenarios (2 belts × 2 scopes × 3 seeds × 2 minutes) run rifle off and
  on — 48 runs — **7 blocks fall without the rifle and 4 with it; 3 saved outright, 3 of the
  other 4 delayed, and no block is ever lost sooner with it**. §11's 90 s belt is not the
  rifle's fight (12 of 12 hold in both arms); the fight is a belt that never comes (600 s),
  where one dry edge is saved 1 / 1 and a whole dry ring is beyond one engineer (2 / 6).
  **Every fall in all 48 runs is `unfed` — not one shade.** Cost: HP min 34, **0 knock-downs
  in 24 rifle runs**, mean 38.7 HP; ammo is never the limit (busiest rescue 96 of 200 rounds
  carried). **The steady hour**: 519 line magazines rifle off and on on every seed, 0 falls,
  0 HP lost, 5 of 990 kills the engineer's. **One thing was built** that the task line did not
  ask for: the tile steady table now carries §19's two guards — they always accumulated on the
  tile path (`markShot` from `walk.ts`, the block `step()` from `advanceFlow`) but were scored
  only on the 5 h compact bot — so **shooting 0.50 / 0.69 / 0.06 % against the 10 % cap and
  danger 0.00 % against D-B1-5's 5 %** are now checks, and E-rifle went 12 → 14. Determinism
  held a third time: the whole of `docs/` moved by fourteen `source_commit` stamps, one
  wall-clock figure and E-rifle's new columns. Evidence: `docs/EXPERIMENTS.md` rows
  `E-rifle-tile-steady` and `E-rifle-tile-rescue`; also `SLICE_REPORT.md` "E-rifle at tile
  scale", `PROGRAMME_STATE.md` B.27 / B.28, `DEFERRED.md` re-read (the 2026-09-04 tile item
  deleted), and the tile evidence appended to **D-R1**, which stays `provisional`.
- 2026-09-05 — **Gate B played, verdict `proceed — with conditions`.** Daniel
  played the opening hands-on and answered the gate's four questions; the results are
  in `docs/GATE_B.md`, and the same verdict is in `SLICE_REPORT.md`'s `## Gate B`
  section, which `ROADMAP.md` names as the gate's evidence. **What was recorded**:
  minutes 0–10 of §19's first-hour table carry the player's words and **10–30 and
  30–60 stay empty**; no telemetry was read, so the hour's shares stay the bot's; the controls walkthrough, the
  two-assembler line and the stranger test remain waived. **One STANDARDS row closed —
  A.7**, the hand-lamp sentence, played dark: "Yea the dark is pretty good so far", so
  the dark reads as the claim's price and **D-B5-1 does not reopen**; `ROADMAP.md` §6
  goes `closed: 0 of 46` → **`1 of 46`**. **§19's minutes 0–10 did not teach what the
  doc says they teach** — neither "blocks bloom" nor "ammo is made from rubble" came
  back; movement did, and the hour's one problem was "knowing what to do next". The
  rifle was fired without mattering ("I was just shooting at things"), which is the
  play half of **D-R1**. **Four rows opened, D-GB-1 … D-GB-4**, three asking for
  something the doc does not contain (constitution rule 12: listed, not built).
  **D-GB-1 was decided the same day as the hybrid (option c)**: the claim still burns
  off its block, so §5, §11 and every measured number stand, and **powering and
  lighting the next area becomes a physical expedition the engineer walks**, with an
  area lighting up as a threat trigger beside the wake bloom — Phase 5 / 6 work, not
  built by the gate. **D-GB-2 / 3 / 4 are `open (gate condition)`**, resolved inside
  Phase 5 by the verdict. T9 is unblocked and re-scoped accordingly. Evidence:
  `docs/GATE_B.md`; also `SLICE_REPORT.md` "## Gate B", `DECISIONS.md` (one decided,
  three conditions), `STANDARDS.md` A.7, `ROADMAP.md` §6 and the gates table.
- 2026-09-05 — **Gate B closed as fully tested.** On Daniel's word the same day, T8
  goes from `done (partly played)` to **`done`**: the gate's purpose is a human's
  go/no-go on the hour and it was given — `verdict: proceed — with conditions`,
  with A.7 closed and D-GB-1 decided. The record of *what was recorded* is
  unchanged and stays in `docs/GATE_B.md`: §19's table carries the player's words
  for the **0–10 window only**, the later windows and the burn-off line were not
  written down, and no telemetry was read, so the hour's shooting, danger and
  walking shares are still the bot's numbers. **Nothing is owed back to the gate.**
  The four questions the table would have answered — the first shade, the walking
  share, the burn-off line and how minutes 10–60 read to a player — travel into
  **Phase 5** as observations, not as a debt. T5, T6 and T7 stay waived. Evidence:
  `docs/GATE_B.md`; also `SLICE_REPORT.md` "## Gate B", `ROADMAP.md` §1 / §2 / §7.
