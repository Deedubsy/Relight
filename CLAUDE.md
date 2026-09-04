# Programme Relight — working rules for Claude

Read first, in this order: `docs/ROADMAP.md`, `docs/PROGRAMME_STATE.md`, `docs/SLICE_REPORT.md`, `docs/DECISIONS.md`, `docs/DEFERRED.md`. The design is `docs/RELIGHT-design.md`; the current slice prompt is `docs/relight-prompt-B-vertical-slice.md`.

## Layout

npm-workspaces monorepo. `packages/sim` (block sim, city, tiles, flow, engineer, walk), `packages/game` (Phaser + Vite world and map views; dev hooks on `window.__relight`), `packages/harness` (experiments E1–E9, E-rifle, E-walk, E-variance, calibration), `packages/tools` (section 18, docsync).

## Verification pass (only when the user asks for it — never on a bare "go")

The scripted checks, experiments, calibration and the headless soak are **not** part of a milestone. They run as a separate verification pass when the user says so, and then all of them, from the repo root:

```
npm test                 # node:test, sim/harness/tools
npm run typecheck        # includes the game build
npm run lint
npm run snapshot:check   # packages/game/public/snapshots/b-compact-seed3.json
npm run docsync:check    # doc tables match packages/sim
npm run experiments      # ~105 s
npm run calibrate -- --out docs/experiments/calibration.json --md docs/experiments/calibration.md
```

plus the 900 s Playwright soak against the Vite preview. A red experiment is a red build. Fixtures `packages/sim/fixtures/city{3,4,5}.json` are the D6 regression set: a change that moves them is a rules change and is reported as one (regenerate with `npx tsx packages/sim/test/_exportCity.ts`). Until a verification pass has run, a milestone's measured numbers are marked *unverified* in the report; nothing else waits on it.

## Constitution

- **Doc follows sim.** A doc number found wrong is edited, with a changelog line at the end of `RELIGHT-design.md`: `- §N — what changed — why — run name`. Numbers the sim confirms get a `[sim: run-name]` tag.
- **GAME-ASSUMPTION tags live in code** and every milestone report lists all of them.
- **Humans decide constants.** Give a recommendation and add a row to `docs/DECISIONS.md`; never settle it silently.
- **Rules surface in-game** (toasts, ghost reasons, panel text), never only in code.
- **Where the build disagrees with the doc, report it, do not resolve it.**
- A bare "go" from the user means the next milestone in the current prompt: **code changes and a light review only**. Do not ask questions; make the routine calls and state the assumptions. No scripted checks, experiments, calibration or soaks — those are the verification pass above, on request.

## Every milestone ends with

1. A light review: read the diff back once for type errors, dead paths, rules that do not surface in-game, and tags missing from the report. Fix what the read finds; do not run the tool chain.
2. Its `docs/SLICE_REPORT.md` section: built / assumed (every GAME-ASSUMPTION) / deferred / measured (marked *unverified* until the verification pass; list what the pass should measure) / where it disagrees with the doc / three decisions for the human.
3. `docs/PROGRAMME_STATE.md` status line and section, plus the §26 untagged recount.
4. A `docs/DEFERRED.md` re-read section.
5. `docs/DECISIONS.md` rows with recommendations.
6. One commit with the trailers below, push, and the PR body updated with a section for the milestone and a "For the human" list. The commit message says "unverified" until the verification pass has run on it.
7. **The report to the user ends with a file-by-file change list**: every file touched, marked created / edited / renamed / deleted, with a one-line summary of what changed in it, grouped sim / game / tests / docs. Build it from `git show --name-status HEAD`.
8. Then stop. The next milestone starts on the next go; the verification pass starts only when the user asks for it.

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
- Soak hygiene (verification pass only): idle host, background Bash, kill by PID, never `pkill -f`. The reference-machine soak is by hand; headless swiftshader fps is for regressions only.
- Headless browser checks (verification pass only) run Playwright against the Vite preview on port 4173 (`?view=world&seed=3`), started from `packages/game`.
