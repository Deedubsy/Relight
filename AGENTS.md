# Repository Guidelines

## Project Structure & Module Organization

Relight is a 2D city-reclamation factory game organized as npm workspaces:

- `packages/sim/src/`: pure TypeScript simulation; implement gameplay here first. State advances through `step(state, commands)`.
- `packages/game/src/`: Phaser renderer and player input, built with Vite. Draw simulation state and send commands; keep gameplay rules in the sim.
- `packages/harness/src/`: bots, experiments, calibration, and replay runners.
- `packages/tools/src/`: documentation synchronization, provenance checks, and image generation.
- `packages/sim/test/`: unit and regression tests; `packages/game/public/snapshots/`: reproducible game snapshots.
- `docs/`: design, decisions, task tracking, and experimental evidence. `.github/workflows/` defines CI and nightly runs.

## Build, Test, and Development Commands

Run from the repository root with Node.js 22, matching CI:

- `npm ci`: install locked workspace dependencies.
- `npm run dev`: start the Vite game server.
- `npm run build`: check all packages and produce the browser build.
- `npm run typecheck`: check strict TypeScript across workspaces; also builds the game.
- `npm test`: run the simulation package's tests using `tsx --test`.
- `npm run lint`: lint simulation, harness, and tools sources with ESLint.
- `npm run docsync:check`: verify generated design tables and guarded prose.
- `npm run freshness:check`: verify generated evidence provenance.
- `npm run snapshot:check`: verify snapshot reproducibility.
- `npm run experiments`: run the experiment suite and generate reports.

## Coding Style & Naming Conventions

Follow existing TypeScript: two-space indentation, single quotes, semicolons, camelCase functions/variables, PascalCase types, and uppercase underscore-separated constants. Use descriptive filenames consistent with neighboring modules. ESLint permits unused names prefixed with `_`; no dedicated formatter is configured.

## Testing Guidelines

Use `node:test` and `node:assert/strict`; name tests `*.test.ts` under `packages/sim/test/`. No numeric coverage threshold is configured. After meaningful code changes, run tests, typecheck, lint, and docsync checks. Run relevant experiments for changed mechanics and snapshot checks for simulation rules. Documentation-only changes require docsync, freshness, and referenced-path checks. Report failures and unrun checks explicitly.

## Commit & Pull Request Guidelines

Keep commits focused on one task. History uses `RI-06: description` and `PROGRESS RI-05: description`. PR descriptions should identify the task or issue, behavior changes, validation results, and relevant screenshots for presentation changes. Do not present automated results as human play approval.

## Design & Task Records

Read `docs/PROGRAMME_STATE.md`, `docs/PROGRESS.md`, and `docs/CONSTITUTION.md` first. Track tasks only in `PROGRESS.md`; consult `DECISIONS.md` and `RELIGHT-design.md` for rule authority. Preserve historical evidence and obtain explicit authorization before changing locked gameplay expectations.
