# Relight — programme constitution

The stable rules of the programme: what the game is, who decides what, what counts as
evidence, how work is verified, and what the status words mean. Operational steps are in
the root `CLAUDE.md`; the phases and their exit criteria are `PHASES.md`; the task list is
`PROGRESS.md`; the current state is `PROGRAMME_STATE.md`; decisions and open questions are
`DECISIONS.md`; the game itself is `RELIGHT-design.md`.

Rule numbers are stable because every phase report cites them ("rule 12", "rule 14"). The
wording was revised on 2026-09-05 by the pre-Phase-5 cleanup (`PROGRESS.md` T11a); the
previous text is `archive/pre-phase-5-cleanup/CONSTITUTION-2026-09-05.md`. It was revised
again the same day by `PROGRESS.md` RI-00, which adopted `REVISED_DEVELOPMENT_PLAN.md`
(D-RI-1) and replaced the three-systems stop with a scope test (D-RI-3); the T11a wording
is `git show d778018:docs/CONSTITUTION.md`. Where an old report quotes a rule, it quotes
the old wording, and the old wording was the rule then.

## Scope

Relight is the full game `RELIGHT-design.md` describes: a 2D city-reclamation factory game
whose loop is claim → power → produce → defend → relight, built through the fourteen
phases of `PHASES.md` to a Steam release, in the order `REVISED_DEVELOPMENT_PLAN.md`
gives (adopted 2026-09-05, D-RI-1; `PROGRESS.md` owns its task statuses). The programme's
ambition and identity are the design doc's; the phases build toward that game, never
toward a smaller one. Content is seeds, presets and facilities; a proposed feature passes
rule 2's scope test and §23's rejected-alternatives test before anything else.

## The rules

1. **Design, code and evidence.** The design doc states the intended rules. Code implements
   them. A simulation measures one implementation under stated conditions (scenario, seed,
   duration, config, commit). A human playtest assesses the experience. When any two of
   these disagree, the disagreement is diagnosed and recorded before anything moves: a
   sim can show the doc's number is unreachable, that the code is wrong, or that the
   scenario was wrong, and the report says which. The design doc labels three kinds of
   statement so a reader can tell them apart: the **current intended rule**; a **current
   implementation limitation** (`GAME-ASSUMPTION`, "the slice pays … as a stand-in"); and an
   **approved future change** ("decided … not yet built", with its decision id). Tables
   generated into the doc by `docsync` carry the code's constants; they never silently
   redefine a player-facing acceptance criterion, and a generated change that moves one is
   a rules change and is reported as one.

2. **The scope test.** §26 counts the systems (three on 2026-09-05: belts/inserters/
   machines; the front rule; found tech as a map) and every phase report recounts it as a
   reported number. A feature is added only if it strengthens the loop (claim, power,
   produce, defend, relight), has a clear implementation boundary, and justifies its
   interaction and testing cost, inside `REVISED_DEVELOPMENT_PLAN.md` §2's boundaries (no
   enemy loot, experience, random drops, survival needs, settlement happiness or dialogue
   trees; no automatic world-wide escalation; no compulsory reflex-heavy boss fights;
   belts and machinery not routinely destructible). This is not permission to add
   unrelated systems. D-RI-3 (2026-09-05) replaced the earlier "a recount of four or more
   is a stop condition" with this test. The engineer's body (rifle, sprint, dodge, reach,
   pockets) is presence, not a system, unless it grows rules of its own.

3. **Headless first.** `packages/sim` is pure TypeScript with no renderer dependency;
   `step(state, commands)` returns the next state. Every rule exists there before it is
   drawn. A red experiment is a red build.

4. **Gameplay rules live in the design doc, not here.** This file holds no game rule. (The
   old rule 4 stated one — "only interior blocks (plus the HQ) host production" — that the
   D6 rework's §5 and the built sim no longer say; it is now `DECISIONS.md` D-CU-1, a
   question for the human, and §5 is the current intended rule until it is answered.)

5. **Bots are the instrument; people judge the experience.** Harness bots and experiments
   issue the same commands a player can (`packages/sim` `Command`), from the same
   inventories, at the same reach. A run that bypasses gameplay commands, or gives the bot
   stock, power or placements a player cannot have, is labelled a scenario in its report
   and is never evidence for a player-facing claim. Whether something is fun, legible or
   tedious is a human observation and is recorded as one (rule 11).

6. **Assumptions are tagged.** A value or rule the code needs that the doc does not state
   carries a `GAME-ASSUMPTION` comment at its definition and a row in `DECISIONS.md` if a
   human should choose it. Untagged numbers in the design doc are counted
   (`PROGRAMME_STATE.md`) and the count is meant to fall.

7. **Decision authority.** Core rules, progression, scope, phase go/no-go and player-facing
   acceptance criteria are human decisions. Claude chooses routine implementation details
   without asking, and may tune a constant only within bounds a decided row or the design
   doc states. A number or rule a task needs that no decided row or design-doc sentence
   supplies becomes a `DECISIONS.md` row with a recommendation; the task that needs it is
   `blocked` on that row and every task that does not need it proceeds. **An unresolved
   question blocks dependent work, not every unrelated task.** Claude can record a human's
   direct authorisation (quoting the message, dated) and cannot originate an approval,
   sign on a human's behalf, or infer that a test was performed.

8. **No tutorial screens.** §11 is the tutorial; every rule surfaces as a toast, a tooltip, a
   pip, a HUD line driven by the sim's own state, or a thing happening on screen. The
   current-goal line (D-GB-2 (a), provisional; built by RI-02) is this rule's form, not an
   exception; a quest or dialogue system would be, and is the human's to move.

9. **Phase exit.** A phase ends with: its report (built / assumed / measured with
   provenance / where it disagrees with the doc / open questions — as many as there are,
   no fixed number); the full verification of the policy below, green or with every
   failure named; `PROGRAMME_STATE.md` rewritten to the new state; `PHASES.md`'s exit
   criteria checked one by one, including the `STANDARDS.md` rows for the phase; and the
   §26 recount. Human gates (`PHASES.md`) are passed by a named person on a dated record.

10. **Stop conditions.** Stop and ask when: a core rule, progression, scope or go/no-go
    decision is needed for the current task; a change would move a fixture, a `[play: …]`
    lock, a decided constant or an expected result; a check is red for a reason outside
    the task; a mechanic or benchmark change the adopted plan does not name
    (`REVISED_DEVELOPMENT_PLAN.md` §1: a separate, visible decision); or the action is
    destructive or outward-facing
    (push, merge, deploy, deleting evidence, an external tracker). Otherwise proceed and
    record what was chosen.

11. **Provenance.** A decision is `decided` only with `decided by` (a person), `on` (a date)
    and `via` (a message quote, commit or report section). Every claim of evidence names
    its kind — a **design approval** (a human said so), a **simulation measurement** (run
    name, seed, duration, config hash, commit) or a **human observation** (who, when, what
    was recorded and what was not) — and the kind travels with the claim. Gate A's
    `[play: Gate A]` tags are approval locks, not measurements; a `[play: Gate B]` tag must
    support the specific sentence it sits on. Generated files carry `source_commit` and
    `config_hash` stamps (`npm run freshness:check`); freshness depends on the inputs the
    file actually reads, and stamps, fixtures and expected results are never rewritten or
    regenerated to make a check green.

12. **Undecided numbers.** If a task's inputs contain a number, rate, size, cost or rule
    that is in neither the design doc nor a decided row, list each one as a `DECISIONS.md`
    row with a recommendation, mark the task `blocked` on those rows, and carry on with the
    work that does not depend on them. This is not a blanket stop: a doc-only task, a
    refactor, a harness change or a task whose own inputs are decided is never held by a
    number some other task needs.

13. **"Built" is four separate claims.** *Implementation complete* (the code exists and the
    cheap checks pass); *automated validation* (`not_run` / `passed` / `failed` /
    `partial`, naming the run); *human approval* (a named person, a date, a record); and
    *human play evidence* (an observation from a played session). A report names which of
    the four it has. A file existing is not a pass; a gate stays passed when an observation
    it hoped for went unmeasured, and the unmeasured observation is listed as owed.

14. **Provisional decisions.** When a task needs a row that has a recommendation and no
    human answer, Claude may act on the recommendation, set the row `provisional` with the
    date and the commit, and list it in the report and in `PROGRAMME_STATE.md`. A human
    confirms or reverses it; until then it is a choice the code embodies, not a rule the
    game has. Human waivers (a task marked `waived` by a person) are preserved as written.

## Verification policy (the one policy)

- **Cheap checks after every meaningful code change**: `npm test`, `npm run typecheck`,
  `npm run lint`, `npm run docsync:check` (about two minutes together).
- **Focused experiments when a change touches what they measure**: `E-hour` for `hour.ts`
  and `constants.ts` `HOUR`; `E-rifle` for the engineer and the enemies; `npm run
  snapshot:check` for any sim rule; `E-chain` / `E-coal` / `E-tram` once they exist.
- **Full verification** — `npm run experiments`, calibration, `freshness:check`,
  `snapshot:check` and the Playwright soak — at phase exit, before a human gate, or when the
  human asks for it. Until it has run on a milestone, that milestone's numbers are
  *unverified*.
- **Doc-only changes** run `docsync:check`, `freshness:check` and a link check on the paths
  they touch; they do not run unrelated simulations.
- **Baseline failures** (red before the task started) are recorded separately from the
  task's own results and never hidden by the task.

Candidate configurations (the plan's Tuning candidates: enemy numbers, the Junction Heart's
timers, the opening windows, rifle range) run beside the 75-minute benchmark and never in
place of it; the legacy reproduction runs keep their original configuration and are labelled
by scope (D-RI-5, `REVISED_DEVELOPMENT_PLAN.md` §14.4). Promotion into the benchmark is a
`decided` row.

## Status vocabulary

- Tasks (`PROGRESS.md`): `todo`, `in_progress`, `blocked` (names what it waits for), `done`
  (its evidence exists and holds the result), `waived` (a named person waived it, with the
  date). Only a human closes a `human` task.
- Validation: `not_run`, `passed`, `failed`, `partial` (some checks ran; the report says
  which).
- Decisions (`DECISIONS.md`): `open`, `recommended`, `provisional`, `decided`, `superseded`;
  `open (gate condition)` is kept for the rows a passed gate attached as conditions.

## Engine and layout

TypeScript throughout: `packages/sim` (headless), `packages/game` (Phaser 3 + Vite),
`packages/harness` (experiments, calibration, snapshot, replay), `packages/tools`
(docsync, freshness, section 18, seeds); `apps/steam` is planned for Phase 14. The engine
gate is Phase 11: if the performance targets fail in TypeScript, the port is a human
decision.

## Cross-cutting

Telemetry is a sim concern (walking, shooting and danger shares come from the same
counters for bots and people). `npm run docsync` keeps the design doc's generated tables
equal to the code and `docsync:check` fails when prose the constants table repeats has
drifted. `DECISIONS.md` holds one row per decision; `PROGRESS.md` is the only task list;
`PROGRAMME_STATE.md` is replaced, not appended. Two human gates have passed (A, B); the
engine gate in Phase 11 is the one left.
