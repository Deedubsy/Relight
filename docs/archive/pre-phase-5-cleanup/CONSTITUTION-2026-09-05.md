# Relight — build the game from the design doc

Starting point: `RELIGHT-design.md` and whatever else is in the repo. Nothing is assumed built. This is a programme of fourteen phases with a gate between each (the phases and their DoDs are in `PHASES.md`); you will finish it over many sessions, each of which starts by reading `PROGRAMME_STATE.md` and the last phase report, and ends by writing the next. Treat this file as the constitution.

The session's read order is in `CLAUDE.md`; the phases are in `PHASES.md`.

## Constitution

1. **The doc is the spec, the sim is the judge, the doc follows the sim.** Every rule is a sentence in `RELIGHT-design.md`; every number that matters carries `[sim: run]` or `[play: session]`. Code and doc disagreeing means the code is wrong until a run says otherwise, and then the doc changes with a changelog line. The doc never drifts into "whatever the code does".
2. **Three systems: the front, found tech, automated combat.** Recount against §26 at every gate. A phase that needs a fourth system stops and reports. Complexity above 6 is a rework trigger.
3. **Headless first.** The whole game lives in `packages/sim`, pure TypeScript, no engine, no DOM, fixed tick, deterministic, JSON-serialisable state, `step(state, commands) → state'`. Every feature is testable from a node harness before it has a pixel. The experiment suite runs in CI; a red experiment is a red build.
4. **Production is territorial.** Machines occupy lots; only interior blocks (plus the HQ) host production; rubble is finite; magazines cost their recipe. These are §5 and §12 rules and they exist from the first prototype onward. A prototype in which an assembler is weightless or ore is infinite cannot test the front, because a number is always cheaper than a shape.
5. **Bots are the instrument.** Four claim policies (compact, spike, cheapest, balanced) that also build when demand exceeds 80 % of production and a slot is free. Calibration means stating the behaviour a session must have and finding the config that produces it with bots; it never means adjusting a number until it feels right.
6. **Report gaps, don't fill them.** Where the doc is silent, implement the simplest thing, tag it `GAME-ASSUMPTION` with the question it stands in for, list it in the phase report. Never a silent invention. Never "I made it more fun".
7. **Humans decide constants, direction and verdicts.** Design constants, art direction, difficulty philosophy, engine swaps, go/no-go at the two human gates. Write the question and the evidence into the report; do not pick. `DECISIONS.md` logs every one.
8. **Rules are watched, not read.** No tutorial screens. §11 is the tutorial; every rule surfaces as a toast, a tooltip, a pip, or a thing happening on screen. A rule that can't be taught that way is suspect.
9. **Every phase ends the same way.** The cheap checks green at every milestone; the full verification pass green at every phase end; a five-minute smoke test a human ran; `PHASE_N_REPORT.md` (built, assumed, deferred, measured, three decisions for a human); `PROGRAMME_STATE.md` updated; `DEFERRED.md` re-read and every item given a phase or deleted with a reason; §26 recounted; **the `STANDARDS.md` rows for this phase are checked (one minute each, by a human), and no new gap was introduced without a row** (added 2026-09-04 by the standards audit).
10. **Stop conditions are outcomes, not failures.** If the experiments say the front doesn't produce the layouts §9 promises, or a human gate returns `kill`, or the factory has no reason to exist without the threat, or complexity can't be held at 6 — write the finding and stop.
11. **Provenance.** A decision counts as decided only when its row in `DECISIONS.md` has three things filled in: `decided by` (a person's name), `on` (a date), and `via` (a link or a quote of the message, the commit, or the report section where they said it). If any of the three is missing, the row is *recommended*, and the assistant must not act as if it were decided. Every number in the design doc must say where it came from: a run name, a play session, or a commit. Every generated file must contain the git commit and the config hash it was made from, and CI must fail if either does not match the current code.
12. **Prompts do not carry constants or rules.** A task prompt may point at sections of the design doc and say what work to do. If a task prompt contains a number, a rate, a size, a cost, or a rule that is not already in the design doc or in a decided row of `DECISIONS.md`, the assistant's first and only action is to write a list of every such number or rule, say where it conflicts, and stop. The human resolves the list before any of the work starts.
13. **"Built" means measured.** A milestone report may use the word "built" in its heading only when its Measured table contains numbers that came from running the code. Until then the heading must say "coded (unverified)". The cheap checks (test, typecheck, lint, docsync) run at the end of every milestone, before the report is written; a milestone is never marked coded while any of them is red. The full verification pass runs when the user says "verify".
14. **Provisional decisions.** When the user says only "go", the assistant may act on a recommended decision only if all three are true: (a) the row is marked `provisional` in `DECISIONS.md`; (b) reversing it later would not change any game rule or any regression fixture; (c) the next report lists it first, under a heading "Taken provisionally". If a row is marked `blocking`, the assistant stops and asks the human to write the row before continuing.

## Engine and layout

Phaser 3 + TypeScript + Vite for rendering; Electron or Tauri for Steam (decided in Phase 13 on measured memory and startup). If Phase 11's performance gate fails after the allocation work, the sim ports to C# and the renderer to Godot (recommended — not yet decided, see DECISIONS.md row D-ENGINE-1); rule 3 is what makes that a month rather than a rewrite.

```
packages/sim        the game, engine-free
packages/game       Phaser renderer, UI, input, audio
packages/harness    node runners: experiments, bots, calibration, fixture export
packages/tools      seed browser, map viewer, telemetry analyser, save inspector
apps/steam          shell, Steamworks bridge, installers
docs/               RELIGHT-design.md, reports, PROGRAMME_STATE.md, DECISIONS.md, DEFERRED.md
```

---

## Cross-cutting

* **Telemetry** keeps one schema from the prototype to launch; the analyser reads all of it.
* **Docs sync**: CI fails if `recipes.ts`, `districts.ts` or `enemies.ts` differ from the doc's tables, which are generated from them.
* **DECISIONS.md**: one line per human decision — phase, evidence file, the doc sentence it produced.
* **PROGRESS.md** is the only task list. A task is done when its evidence file contains the result, not when someone says so. Only a human marks a human-owned task done.
* **The two human gates (A and B) and the engine gate are the only places the programme waits.** Everything else is a report and a next phase.
