# Programme Relight — working rules for Claude

**At the start of every session, read these files in this order:**
1. `docs/CONSTITUTION.md` — the rules.
2. `docs/PROGRESS.md` — the task list. The top task that is not done is the next task.
3. `docs/PROGRAMME_STATE.md` — the true current state. If it disagrees with the roadmap, PROGRAMME_STATE is right.
4. The phase prompt named in the current task of `docs/PROGRESS.md`, if it names one.
5. `docs/DECISIONS.md` — only the rows whose status is not `decided`.
6. `docs/DEFERRED.md`.

**Then do the top task in `docs/PROGRESS.md` whose status is not done.** If its owner is human, stop and tell the user which task it is and what it needs. If its owner is claude and it is blocked, stop and name what it is blocked by.

**Before starting any milestone:** compare the milestone's prompt with the design doc and with the decided rows of `DECISIONS.md`. If the prompt contains a number or a rule that is not in either, list every one, say what it conflicts with, and stop (Constitution rule 12).

The design is `docs/RELIGHT-design.md`; the current slice prompt is `docs/relight-prompt-B-vertical-slice.md`.

## Layout

npm-workspaces monorepo. `packages/sim` (block sim, city, tiles, flow, engineer, walk), `packages/game` (Phaser + Vite world and map views; dev hooks on `window.__relight`), `packages/harness` (experiments E1–E9, E-rifle, E-walk, E-variance, calibration), `packages/tools` (section 18, docsync).

## Verification pass (only when the user asks for it — never on a bare "go")

The user says 'verify' to run this. A bare 'go' runs only the cheap checks at the end of the milestone.

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

## The rules

**The rules are in `docs/CONSTITUTION.md`. Read it at the start of every session. Nothing in this file overrides it. If this file and CONSTITUTION.md ever disagree, CONSTITUTION.md is right and this file must be fixed.**

## Every milestone ends with

1. A light review: read the diff back once for type errors, dead paths, rules that do not surface in-game, and tags missing from the report. Fix what the read finds. Run the cheap checks (test, typecheck, lint, docsync). If any is red, fix it before writing the report. Do not run experiments, calibration or the soak unless the user said 'verify'.
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
