# Relight

A 2D city-reclamation factory game. The design doc is the spec, the headless sim is the judge.

- `docs/RELIGHT-design.md` — the spec. Every number carries `[sim: run]` or `[play: session]`.
- `docs/PROGRAMME_STATE.md` — current phase, inventory, untagged set, open constants. Read first.
- `docs/DECISIONS.md` — every human decision, and every open one.
- `docs/DEFERRED.md` — what is deferred and to which phase.
- `docs/PHASE_N_REPORT.md` — one per phase.
- `packages/sim` — the game, pure TypeScript, `step(state, commands) → state'`.
- `packages/harness` — node runners: experiments, calibration, bots.
- `packages/game` — Phaser renderer: the map view (Phase 2 prototype) and, from Phase 4, the world view.

```
npm install
npm test            # fixture regression + unit tests
npm run experiments # E1–E9 at three seeds → docs/EXPERIMENTS.md
npm run dev         # the prototype
```
