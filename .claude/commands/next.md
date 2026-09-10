Read docs/PROGRAMME_STATE.md current handoff, then docs/PROGRESS.md.

Find the current task: the first row whose status is `todo` or `in_progress` and whose
"blocked by" column names only tasks that are `done` or `waived` and decision rows that
are `decided`. Rows that are `blocked`, or whose blockers are not clear, are reported
(id, task, what they wait for) and skipped; they do not stop the search.

Then:

1. If no task is runnable: print every blocked row with its blocker and stop.

2. If the current task's owner is `human`: print the task id, the task text, its
   acceptance column and the evidence file and section it needs. Then print: "This one is
   yours. Tell me the result when you have it and I will write it in." Stop.

3. If the current task's owner is `agent` or `claude`:
   a. Set its status to `in_progress` in docs/PROGRESS.md (one row; no commit without explicit authorisation).
   b. Read the decision rows its "blocked by" and acceptance columns name. If one the
      task cannot proceed without is not `decided`, set the status back to `blocked`,
      name the row, and stop. A row the task can embody provisionally (constitution rule
      14) is noted, not a stop.
   c. Do the task. Read the design-doc sections it names first. Follow the constitution's
      verification policy: cheap checks (npm test, npm run typecheck, npm run lint, npm
      run docsync:check) after meaningful code changes; the focused experiment when the
      change touches what it measures; full verification only at phase exit or on
      request. A red check is fixed or reported, never hidden; a failure that was red
      before the task is reported separately.
   d. Review the diff once: accidental gameplay change, a rule that does not surface
      in-game, a missing tag or GAME-ASSUMPTION, a fabricated approval, a fixture moved.
   e. Write the evidence named in the "evidence" column. Say which of the four claims it
      has: implementation complete / automated validation (not_run, passed, failed,
      partial) / human approval / human play evidence.
   f. Set the status to `done` with today's date, add one log line, move dependent
      blocked rows to `todo` only when all their named dependencies are satisfied, and update
      docs/PROGRAMME_STATE.md current handoff (and "What is built" if it changed), touch the
      DECISIONS.md rows the task embodied, and add the design-doc changelog line for any
      doc edit.
   g. Commit/push only with explicit user authorisation; follow current CLAUDE.md.
   h. Print: what was built; what was measured and how (run, seed, config, commit); what a
      human must decide, if anything; the file-by-file change list from
      the actual working-tree diff (or `git show --name-status HEAD` only when committed).
   i. Stop. The next task starts on the next `/next`.

Never mark a `human` task `done` or `waived`. Never change or reuse a task id.
