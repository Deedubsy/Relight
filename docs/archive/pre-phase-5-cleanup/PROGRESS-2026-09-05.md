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
so. **T9 is done 2026-09-05** — doc only. It made the two `[play: Gate B]` tags that had been
placed in `RELIGHT-design.md` *before* the gate was played true (§4's narrowed to the
movement clause the hour reached; §19's 10 % shooting cap stripped of a tag the gate never
earned, because no telemetry was read), wrote §19's first-hour finding against §19 itself,
recorded **D-GB-1's hybrid in §10 as decided and not built**, added the three gate
conditions to §25 as open questions 19–21, and attached the gate's evidence to D-R1,
D-P4-5, C1 and C2 without deciding any of them. **No code file changed**: the gate produced
no constant, and D-GB-2/3/4 are open, so building them would breach constitution rule 12.
**Phase 4's task list is finished. T10 is done 2026-09-05 and Phase 5 is open**
(`docs/PHASE_5_REPORT.md`). The opening is a constitution rule 12 read: Phase 5's line in
`PHASES.md` carries **seven rules that neither the design doc nor a decided row settles**,
so they are listed and **not built** — the brownout's map-view read (§14 says lights do not
dim), which §13 machines are Phase 5's, one belt lane or §14's two, what "blueprints from
minute one" means against Phases 7 and 8, where the filter lives, the tram route on the map
view, and the experiments' names. They are **D-P5-1 … D-P5-7**, and **T11 — a human task —
is the next task**: every Phase 5 build task (T12–T17) is blocked on it. Two other Phase 5
sentences were plainly settled by the doc and were corrected in `PHASES.md` rather than
asked ("up to six inserters a stop", and crude's source), and D-INSERTERS-1's sentence
landed in §13 where its own row said it would.

**T11 is done 2026-09-05**, written in on the human's instruction — the message *"Go with
your recommendation for all"*, taking each row's own recommendation. **Fourteen rows were
decided at once** and **no row in `DECISIONS.md` is `open` any more**; only D-GB-2, D-GB-3
and D-GB-4 remain `open (gate condition)`, which is what Gate B's verdict made them. The
seven rule-12 rows: no map-view dim (D-P5-1), the §13 rows whose unlock is `start` or a
Phase 5 system (D-P5-2), **one belt lane** (D-P5-3), the blueprint word splits into a
harness file and a found unlock (D-P5-4), a per-inserter filter (D-P5-5), the tram route
only while a stop is selected (D-P5-6), and `E-chain` / `E-coal` / `E-tram` (D-P5-7). The
five M1/M2 constants: D-P4-2, D-P4-3, **D-P4-12 at ~700**, C5, D-B2-1. And two programme
calls the same message settled: **Phase 6 builds D-GB-1's hybrid** (D-GB-1-rider) and
**hour one is re-scoped to 75 minutes** (D-HOUR-3, built at M1 because §11's minute table
is generated from `constants.ts` `HOUR`). Five design-doc sentences changed with it, each
with a changelog line, and **T12 — Phase 5 M1 — is the next task**.

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
| T9 | Absorb Gate B: fold the played hour and D-GB-1's hybrid into code and doc, tag `[play: Gate B]`, and carry D-GB-2/3/4 into Phase 5 as conditions | claude | **done** (doc only — no code; see the log) | | `docs/PROGRAMME_STATE.md` section "Absorb Gate B" | 2026-09-05 |
| T10 | Open Phase 5 (the factory, complete) | claude | **done** | | `docs/PHASE_5_REPORT.md` | 2026-09-05 |
| T11 | Decide the seven rows Phase 5 cannot start without: D-P5-1 … D-P5-7 (`PHASE_5_REPORT.md` §2c), and the rows M1 and M2 need — D-P4-2, D-P4-3, D-P4-12, C5, D-B2-1 | human | **done** | T10 | `docs/DECISIONS.md` rows D-P5-1…D-P5-7, D-P4-2, D-P4-3, D-P4-12, C5, D-B2-1, D-GB-1-rider, D-HOUR-3 — all `decided`, Daniel, 2026-09-05, via the human's message "Go with your recommendation for all" | 2026-09-05 |
| T12 | Phase 5 M1 — recipes, rubble and the 75-minute hour: the eight non-Shot recipes made by real machines and priced (D-B2-1 (b)); rubble as finite typed ore with visible depletion, units per tile set (D-P4-2); rail-yard coal rubble at ~700 a lot (D-P4-12); the outskirts deposits kept as the placeholder unless a run needs them, and which half was built reported (D-P4-3); the HQ patches sized (C5); **hour one re-scoped to 75 minutes in `constants.ts` `HOUR`, §11 regenerated, north a scored check (D-HOUR-3)**; the `[play: <gate>]` check in docsync | claude | todo | — | `docs/PHASE_5_REPORT.md` section "M1" | |
| T13 | Phase 5 M2 — placement and the command layer: every Phase 5 §13 machine with its footprint; undo / redo (1.10); drag placement taking a path (1.3); the hotbar as references (1.16); one binding table (1.17); one string table (8.5) | claude | blocked | T12 (D-SA-2 is `provisional` under rule 14, not signed) | `docs/PHASE_5_REPORT.md` section "M2" | |
| T14 | Phase 5 M3 — belts complete: undergrounds with the span shown (1.4), splitters with priority (2.3), the filter (2.5), side-loading, chests, the Depot's item list | claude | blocked | T13 | `docs/PHASE_5_REPORT.md` section "M3" | |
| T15 | Phase 5 M4 — power and the panels: power complete on the tile layer; the machine status enum (2.6), rates (2.7), alt-mode (2.9), per-item counters (3.1); the brownout's map-view read; **D-GB-2's next-objective line** | claude | blocked | T14 (D-GB-2's form is a Gate B condition M4 resolves, not a prerequisite: the row recommends (a), the rule-8-legal HUD next-objective line, then re-ask) | `docs/PHASE_5_REPORT.md` section "M4" | |
| T16 | Phase 5 M5 — trams and the driven truck: track, stops, trams, the truck driven (2.11 / 7.4 / A.9), the route on the map view | claude | blocked | T15 | `docs/PHASE_5_REPORT.md` section "M5" | |
| T17 | Phase 5 M6 — slots, the blueprint file and the DoD: slots as real lot geometry; the bot's blueprint file; copy / paste (1.11); the three experiments; then the bot's 5 h §18 territory | claude | blocked | T16 (C3 and D-B2-2 are `provisional` under rule 14, not signed) | `docs/PHASE_5_REPORT.md` section "M6" | |
| T18 | Build a two-assembler ammo line in the world view, unaided, in under ten minutes — Phase 5's DoD, and T6 returning from its waiver | human | blocked | T17 | `docs/PHASE_5_REPORT.md` section "The DoD, played" | |

## Waiting on the human

- ~~**T11 blocks the whole of Phase 5.**~~ **Done 2026-09-05** — the message *"Go with
  your recommendation for all"* took every row's own recommendation, and **`DECISIONS.md`
  now has no `open` row at all**. What that signed, in one place, so it can be disowned in
  one place: **D-P5-1** no map-view dim (the HUD bar and the machine panel carry the
  brownout; §14's "lights do not dim" stands); **D-P5-2** Phase 5 builds the §13 rows whose
  unlock is `start` or a Phase 5 system, and the Cannon, Barricade, Mixer, Pumpjack, the six
  facilities and the Line truck garage wait for their own phases; **D-P5-3 one lane**, and
  §14 was rewritten to match `flow.ts`; **D-P5-4** the word splits — the bot's blueprint
  file is a harness JSON, the in-game blueprint stays Phase 7's found unlock and Phase 8's
  build, and §14 / §19 / §24 lost "from minute one"; **D-P5-5** a per-inserter filter
  setting, one item kind, off by default; **D-P5-6** the tram route drawn only while a stop
  is selected, so §4's ten-entry vocabulary is untouched; **D-P5-7** `E-chain` / `E-coal` /
  `E-tram`, with `E10` left to the retired bloom run. Five of those changed a design-doc
  sentence, each with a changelog line.
- **The one row that overrode the design doc rather than filling a blank: D-P4-12.** It was
  taken as recommended — a `coal` rubble kind on rail-yard / industrial lots at **~700 a
  lot** — against §12's raws table, which read *"rail-yard rubble (direct, ~30k per
  block)"*, untagged, 43× larger. Both numbers were in front of the row when it was signed,
  and §12's line has been rewritten to ~700 with a changelog line. **If ~30k was the
  intent, this is the sentence to reopen**: at ~30k west's claim answers coal for the rest
  of the game instead of topping up hour two, which is a different game. `E-coal` measures
  it at M1.
- **Two programme calls were signed with them.** **Phase 6 builds D-GB-1's hybrid**
  (D-GB-1-rider) — powering and lighting the next area as a walked expedition, lighting up
  as a threat trigger — so Phase 5 stays the phase that finishes the factory, and the loop
  change gets its own milestone and doc-edit pass in Phase 6. And **hour one is re-scoped to
  75 minutes** (D-HOUR-3): north's claim at 65:00 becomes a scored check with its own DoD
  line, the HQ's white border and the Electricians stop being beats a 3,600 s run can never
  reach, and the build lands at **M1**, because §11's minute table is generated by docsync
  from `constants.ts` `HOUR`.
- **Three rows Claude took `provisional` under rule 14, not the human** — they were named
  by Phase 5 milestones but were not in T11's list, so they are Claude's call and stay
  provisional until signed: **D-B2-2** (slots stay `floor(area / 600)` now, dropped at M6
  when tile lines replace the block stand-in), **C3** (unchanged — one ammo line per
  interior block), and **D-SA-2** (a string table in Phase 5, translation bought in Phase
  13). T13 and T17 name them in their blocked-by lines so they surface before the work
  reaches them.
- **D-GB-2 is no longer written as a blocker on T15.** Gate B passed *with* it as a
  condition to be resolved inside Phase 5, so M4 resolves it rather than waiting on it: the
  row's recommendation is (a), the rule-8-legal forms first — a HUD next-objective line
  driven by the sim's own state — and then re-ask. **A quest system is still yours to
  take**, and it changes constitution rule 8 with a changelog line if you take it.
- **The first shade never happens in the bot's hour**, and Gate B closed without
  contradicting it. §11 and E3 put it at minute 32–47; six runs of E-hour print
  "never"; no shade kills anything in 48 tile rescue runs and none is born in six
  hours. The question is "does a human let a block sleep past d = 0.3 where the bot
  never does". **It is no longer a gate debt** — the gate is closed — but it is
  still the one E-hour finding a bot cannot settle. It rides into **Phase 5** with
  the walking share, the burn-off line and §19's unrecorded windows; any session
  that reaches minute 45 answers all four at once, whenever one happens.
- Row **D-R1** (the rifle and the shade) is still `provisional`, and after T9 it has
  both halves of its evidence. The bots: no shade kills anything in 48 rescues, none is
  born in six hours. The play: the rifle was fired and decided nothing. And **A.7's answer
  closes option (b)** — no hand lamp, D-B5-1 stands play-supported — so **(a) is the only
  live option left and is already what the code does**. Blocks nothing; deciding it is a
  signature.
- Row **D-P4-5** (the two assembler stand-ins) is `provisional` and its recommendation is
  now **unconditioned**: it said "make every assembler physical after Gate B, the
  block-level button stays through Gate B so the played hour is one model", and Gate B is
  past. Blocks nothing.
- Rows **C1** and **C2** both name "Phase 4's human hour" as the first place they can
  move. That hour was played and **could not move them** — no telemetry was read and no
  claim minutes were taken — so both stay `provisional` on the sim's numbers, and the next
  played session with the telemetry panel open is the occasion.
- **The calibration's pip bands** (16–31 min, the lattice's, against ≈ 3 / ≈ 6 min at tile
  level) are the last item T9's roadmap line listed that T9 could not settle: it is
  recalibrate-or-retire, and that is a human's word, not a run.
- **§5's rescue sentence quotes the block sim** — "a whole belt 90 s away falls in
  ~1.5 min to a shade at an unlit edge, and the rifle cannot save it". The tile sim
  says a 90 s ring never falls at all and the killer is always crawlers. Both are
  tagged to their own run, so nothing is wrong as written; which sim §5 quotes is a
  human's word. Blocks nothing.
- **The 600 s belt is the rifle's only justification in evidence.** It is not §11's
  rescue (§11's is 90 s, at which the experiment cannot tell an armed engineer from
  an unarmed one). Keep it, drop it, or rename it — the human's call; §24 risk 10
  rests on it.
- ~~**T9 needs re-scoping.**~~ **Re-scoped and then done, both 2026-09-05.** T9 ran
  against the re-scoped line and closed Phase 4. One thing in its own task line could not
  be honoured and is reported rather than fudged: it says "into **code** and doc", and
  **no code was written**, because the gate produced no constant, D-GB-1's build is Phase
  5 / 6 work by the terms of the decision itself, and D-GB-2/3/4 are open — building any
  of them now would be building from a prompt that carries a rule the design doc does not
  (constitution rule 12, the same rule the gate invoked when it refused them).
- **Two `[play: Gate B]` tags were in the design doc before Gate B was played**
  (commits `27cc9b0`, `906044c`), and one of them — §19's 10 % shooting cap — was never
  earned; T9 removed it and narrowed the other. **The Phase 5 opening answers the "what
  check" half**: `docsync:check` learns one rule in M1 — every `[play: <gate>]` tag must
  name a gate whose evidence file exists and carries a verdict — which is a dozen lines
  and the only class of provenance error the repo cannot currently see. It is the one
  piece of Phase 5 work with no rule-12 question in front of it, because it invents no
  game rule. Whether that is enough, or tags are re-read by hand at every gate as well,
  is still a human's word. Blocks nothing.
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
- 2026-09-05 — T9 **done**, doc only. **Absorb Gate B.** The two `[play: Gate B]`
  tags that had been placed in `RELIGHT-design.md` *before* the gate was played are
  now true: **§4's** is split, so the movement clause the hour actually reached keeps
  the tag ("kind of intuitive but needs more development time") while sprint, dodge
  and "completable without any of them" keep only `[sim: B-M1-body]`, and the 9-tile
  "no range advantage" is named as the clause play argued with (**D-GB-3**);
  **§19's 10 % shooting cap loses its tag entirely**, because the gate read no
  telemetry and no player shooting share exists — the bot's 0.50 / 0.69 / 0.06 % and
  0.00 % `[sim: E-rifle-tile]` stand in its place. **§19's first-hour test now carries
  the finding against itself** (neither of minute 0–10's two rules was named back;
  movement was; the hour's one problem was not knowing what to do next, **D-GB-2**,
  against rule 8). **§10 records D-GB-1's hybrid as decided and not built** — the three
  loops unchanged, the expedition and the lighting-up trigger written beneath them, and
  the statement that §5, §11 and §18 do not move until the milestone that builds it.
  **§25 gains open questions 19, 20 and 21**, the three gate conditions, and the
  Changelog preamble now says what a `[play: Gate B]` tag means. `SLICE_REPORT.md`'s
  §19 table and its two Gate B rows carry the played hour. Four `provisional` rows got
  the gate's evidence and **none was decided** (rule 7): **D-R1** (A.7 closes option
  (b), so (a) is the only live one), **D-P4-5** (its "through Gate B" condition met),
  **C1** and **C2** (unmoved — no telemetry, no claim minutes). **No code file
  changed**, and that is reported as a disagreement with T9's own task line rather
  than as a shortfall: the gate produced no constant, D-GB-1's build is Phase 5 / 6
  work by the terms of the decision, and D-GB-2/3/4 are open — rule 12. Cheap checks
  green: tests 121 / 121, typecheck, lint, docsync, snapshot (config `0176f61d`); the
  experiment suite is untouched at 13 experiments, 0 failing checks. **Phase 4 is
  complete.** Evidence: `docs/PROGRAMME_STATE.md` B.31 / B.32; also `SLICE_REPORT.md`
  "Absorb Gate B", `RELIGHT-design.md` (seven edits, seven changelog lines),
  `DECISIONS.md`, `DEFERRED.md` re-read, `ROADMAP.md` §1 / §2 / §6 / §7.
- 2026-09-05 — T10 **done**, doc only. **Phase 5 is open** and nothing is built. The
  opening is the comparison constitution rule 12 requires, and it found **seven rules in
  Phase 5's line that neither `RELIGHT-design.md` nor a decided row carries**: the
  dimming on the map view (§14 says "lights do not dim" and §4's vocabulary has no
  throttle state), "the §13 machine list and nothing outside it" (six of its rows unlock
  in Phases 6–8), §14's two-lane belts against `flow.ts`'s one (STANDARDS 2.1 routed this
  here by name), the DoD's blueprint file against Phase 7's found unlock and Phase 8's
  build, the filter STANDARDS 2.5 asks for and §13 does not mention, the tram route on
  the map view, and the E10 name — already taken by the retired bloom-cadence run cited
  in §25, C8, D-P3-5 and D-P3-11. All seven are **listed and not built**, as rows
  **D-P5-1 … D-P5-7**, and **T11** is the human task that closes them. **Two sentences
  the doc plainly settles were corrected in `PHASES.md`** instead of asked, on the
  guardrails task's precedent: "six inserters per stop" → "up to six inserters a stop"
  (§13 and §14 both say *up to* 6, and a fixed six pre-answers E12), and "oil from the
  Refinery only" → crude from the Pumpjack, the Refinery its unlock and its only consumer
  (§12). **One design-doc edit**: §13's Inserter row now carries D-INSERTERS-1's answer,
  which that row said would land "when Phase 5 opens" — one inserter, no tier ladder, a
  faster one only as found tech if the chain experiment finds a chain that cannot reach
  its rate. **D-P4-12's premise was corrected**: §12's raws table does carry the rail-yard
  coal kind and a quantity (~30k a block, 43× the row's recommended ~700); the row stays
  the human's. **§11's enclosure and the Electricians** — the question `PROGRESS.md`
  addressed to this opening — are **the shape of hour one, not a hole in it**: both beats
  are gated on north, north is at 65 by D-P4-10, and §11 says so; what is left is whether
  the hour is re-scoped to 75 minutes, which `E-hour-north` already measures as green.
  Cheap checks green. Evidence: `docs/PHASE_5_REPORT.md`; also `DECISIONS.md` (seven rows
  added, D-P4-12 corrected), `PHASES.md` (two corrections, four `[open: …]` tags, a
  footnote), `RELIGHT-design.md` §13 + changelog, `PROGRAMME_STATE.md` 5.1 / 5.2,
  `DEFERRED.md` re-read, `ROADMAP.md`.
- 2026-09-05 — **T11 done, written in on the human's instruction.** The message
  *"Go with your recommendation for all"* answered the four groups the previous
  report named, so every row was marked `decided | Daniel | 2026-09-05 | the chat
  message of 2026-09-05 ("Go with your recommendation for all"), taking this row's
  recommendation`. **Fourteen rows**: D-P5-1 (a) no map-view dim, D-P5-2 (a) the §13
  rows whose unlock is `start` or a Phase 5 system, D-P5-3 (a) one belt lane, D-P5-4
  (a) the blueprint word splits, D-P5-5 (a) a per-inserter filter, D-P5-6 **(c)** the
  route only while a stop is selected, D-P5-7 (a) `E-chain` / `E-coal` / `E-tram`;
  D-P4-2, D-P4-3, D-P4-12 (a) at ~700, C5 and D-B2-1 on their own recommendations;
  and two new rows the same message settled — **D-GB-1-rider** (Phase 6 builds the
  hybrid) and **D-HOUR-3** (hour one re-scoped to 75 minutes, built at M1).
  `DECISIONS.md` now holds **no `open` row**; only D-GB-2/3/4 remain `open (gate
  condition)`. D-B2-2 and D-SA-2 were moved `recommended` → `provisional` under rule
  14 as Claude's call, clearly attributed, so M2 and M6 do not stall on an unsigned
  row. Five design-doc sentences changed with the decisions, each with a changelog
  line (rule 1): §14's "standard two-lane belts" → one lane; §14, §19's tedium audit
  and §24 risk 1's "blueprints from minute one" → a found unlock with the kit as the
  first-hour answer; §13's Inserter row → a filter setting; §12's raws coal → ~700 a
  lot. Files: `DECISIONS.md` (14 rows decided, 2 added, 2 taken provisional, header
  rewritten), `RELIGHT-design.md` (§12, §13, §14, §19, §24 + four changelog lines),
  `PHASES.md` (four `[open: …]` tags resolved, the experiments renamed, the DoD's
  blueprint clause and the 75-minute rider, Phase 6 given the hybrid, a T11
  footnote), `STANDARDS.md` (1.11, 2.1, 2.5, 2.11 and dealbreaker 1 closed),
  `PROGRESS.md`, `PROGRAMME_STATE.md` 5.3, `DEFERRED.md`, `PHASE_5_REPORT.md`.
  **T12 — Phase 5 M1 — is the next task and is not blocked.**
