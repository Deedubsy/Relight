# Programme Relight — working rules for Claude

Read first, in this order: `docs/ROADMAP.md`, `docs/PROGRAMME_STATE.md`, `docs/SLICE_REPORT.md`, `docs/DECISIONS.md`, `docs/DEFERRED.md`. The design is `docs/RELIGHT-design.md`; the current slice prompt is `docs/relight-prompt-B-vertical-slice.md`.

## Layout

npm-workspaces monorepo. `packages/sim` (block sim, city, tiles, flow, engineer, walk), `packages/game` (Phaser + Vite world and map views; dev hooks on `window.__relight`), `packages/harness` (experiments E1–E9, E-rifle, E-walk, E-variance, calibration), `packages/tools` (section 18, docsync).

## Checks (run all from the repo root before a milestone commit)

```
npm test                 # node:test, sim/harness/tools
npm run typecheck        # includes the game build
npm run lint
npm run snapshot:check   # packages/game/public/snapshots/b-compact-seed3.json
npm run docsync:check    # doc tables match packages/sim
npm run experiments      # ~105 s
npm run calibrate -- --out docs/experiments/calibration.json --md docs/experiments/calibration.md
```

A red experiment is a red build. Fixtures `packages/sim/fixtures/city{3,4,5}.json` are the D6 regression set: a change that moves them is a rules change and is reported as one (regenerate with `npx tsx packages/sim/test/_exportCity.ts`).

## Constitution

- **Doc follows sim.** A doc number found wrong is edited, with a changelog line at the end of `RELIGHT-design.md`: `- §N — what changed — why — run name`. Numbers the sim confirms get a `[sim: run-name]` tag.
- **GAME-ASSUMPTION tags live in code** and every milestone report lists all of them.
- **Humans decide constants.** Give a recommendation and add a row to `docs/DECISIONS.md`; never settle it silently.
- **Rules surface in-game** (toasts, ghost reasons, panel text), never only in code.
- **Where the build disagrees with the doc, report it, do not resolve it.**
- A bare "go" from the user means the next milestone in the current prompt. Do not ask questions; make the routine calls and state the assumptions.

## Every milestone ends with

1. Its `docs/SLICE_REPORT.md` section: built / assumed (every GAME-ASSUMPTION) / deferred / measured / where it disagrees with the doc / three decisions for the human.
2. `docs/PROGRAMME_STATE.md` status line and section, plus the §26 untagged recount.
3. A `docs/DEFERRED.md` re-read section.
4. `docs/DECISIONS.md` rows with recommendations.
5. All checks green, then one commit with the trailers below, push, and the PR body updated with a section for the milestone and a "For the human" list.
6. **The report to the user ends with a file-by-file change list**: every file touched, marked created / edited / renamed / deleted, with a one-line summary of what changed in it, grouped sim / game / tests / docs. Build it from `git show --name-status HEAD`.
7. Then stop. The next milestone starts on the next go.

## Commits and PRs

Commit messages end with:

```
Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01GFnpcztBCdSbXdjYj46qPW
```

PR bodies end with `🤖 Generated with [Claude Code](https://claude.com/claude-code)`, a blank line, then the session URL. PRs are stacked: #1 → #2 → #3 → #4.

## Working habits

- Run Bash from the repo root. In bypass-permissions mode prefer Bash (cat, sed, python3 heredocs) over the Read/Edit/Write tools.
- Give a one-line progress update every few minutes of work.
- Temporary scripts go in the session scratchpad, never the repo.
- Soak hygiene: idle host, background Bash, kill by PID, never `pkill -f`. The reference-machine soak is by hand once a milestone; headless swiftshader fps is for regressions only.
- Headless browser checks run Playwright against the Vite preview on port 4173 (`?view=world&seed=3`).
