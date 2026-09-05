# Relight

A 2D city-reclamation factory game. The design doc defines the intent, the code implements
it, the headless sim measures the implementation under stated conditions, and human play
assesses the experience (`docs/CONSTITUTION.md` rule 1).

Start with `CLAUDE.md` (the read order), then:

- `docs/PROGRAMME_STATE.md` — where the programme is, two pages. Read first.
- `docs/PROGRESS.md` — the only task list; the current task is the first runnable row.
- `docs/CONSTITUTION.md` — the rules, decision authority, the verification policy.
- `docs/DECISIONS.md` — every decision with provenance, and the outstanding questions.
- `docs/RELIGHT-design.md` — the spec. Every number carries `[sim: run]` or `[play: session]`.
- `docs/PHASES.md` — the fourteen phases, scope and exit criteria.
- `docs/DEFERRED.md` — unscheduled obligations only.
- `docs/PHASE_N_REPORT.md`, `docs/GATE_B.md`, `docs/EXPERIMENTS.md` — history and evidence.
- `docs/archive/` — superseded documents, with a README mapping old paths to new.
- `packages/sim` — the game, pure TypeScript, `step(state, commands) → state'`.
- `packages/harness` — node runners: experiments, calibration, bots.
- `packages/game` — Phaser renderer: the map view and the world view.
- `packages/tools` — docsync, freshness, §18 and seed images.

```
npm install
npm test               # fixture regression + unit tests
npm run docsync:check  # doc tables match packages/sim
npm run experiments    # E1–E9, E-hour, E-rifle … → docs/EXPERIMENTS.md
npm run dev            # the game
```
