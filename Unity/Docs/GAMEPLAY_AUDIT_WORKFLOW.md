# Gameplay audit workflow

Reusable procedure for a critical play-and-research review of the Unity port. First executed on 2026-09-15; output in [GAMEPLAY_FUN_AUDIT.md](GAMEPLAY_FUN_AUDIT.md). Rerun it whenever a phase changes what a normal player can reach. It is analysis only: no gameplay code, decisions, saves or task status change; `TASKS.md` owns status and gets one §8 line per run.

## 1. Triage the state before anything else (≈30 min)

Read `TASKS.md` (current phase rows and any proposal rows), `DECISIONS.md` (open U-Q items and the U-D rules the audit will touch), the confirmed-gameplay document, and the handoffs since the last audit. Sort everything the audit might report into five bins and keep the bins in the report:

1. implemented and reachable in normal play;
2. in code but disconnected or unreachable;
3. explicitly deferred (cite the task row; never call it a defect);
4. port gap that removed support for approved future gameplay (cite the dependency row);
5. unresolved decision (cite U-Q; proposals stay proposals).

Also list previously reported problems and check each in play; report only those the run actually confirms fixed.

## 2. Research with `/deep-research`, or label the fallback

Invoke the real workflow (`Workflow` name `deep-research`) with the concrete questions the audit needs answered, naming three to five reference games chosen for those questions. Record the run id, agent, source, claim and survivor counts. Keep only findings that survived verification; list the games that produced no survivors so nobody cites them. If the workflow is unavailable, say so and run labelled `WebSearch`/`WebFetch` instead. Never cite a source the run did not fetch. Prefer developer postmortems and talks; mark player threads as such; separate commercial success, satisfaction and interpretation in the comparison table.

## 3. Split the source work across at most three workers

One question per worker, source inspection only, no Unity control: typically (A) economy and automation, (B) combat and director, (C) progression, exploration and story. Ask each for figures with file:line references, a verdict, a problem table with severities, and a list of what only play can answer. The coordinator alone drives Unity. Cross-check every worker number that the play run can reach; the run wins on disagreement and the report says so.

## 4. Observe as a player on the normal stake

- Unity CLI from WSL against the open editor: `unity.exe --no-banner --non-interactive --format json command <cmd> --project-path 'E:\Factorio2\Unity\Relight'`; use `editor_play`, `eval_file`, `capture_game_view`, `console`, `editor_stop`. Never edit `.cs`/UXML/USS while in Play Mode.
- Start from the title with New Game, Standard ruleset, default seed. **No grants, no debug raid, no admin drawer** for any pacing, affordability or progression claim. If Input System events do not fire (unfocused editor), submit the same sim commands `WorldInput.cs` would send and say so in the log; read UI text from the live UI Toolkit tree.
- Log every step as: sim time, attempted, information the player had, outcome, feel, cause (presentation / implementation / design). Screenshot the first goal, first payoff, first fight, any failure and the end state.
- Prepared saves or debug triggers may be used to inspect later situations; label them in the log and keep their conclusions in a separate section. Use audit saves under a distinct name; restore any inspection setting; delete temporary assets; move screenshots out of `Assets` (e.g. `Relight/Temp/audit`, git-ignored) and copy the keepers to `Docs/evidence/gameplay-audit/`.
- Stop the run where the guide stops giving a next step; that stop point is a finding.

## 5. Keep the investigation table live

`Question → current evidence → hypothesis → research needed → local check → conclusion/confidence`. Add a row when a worker, the run or the research raises a question; close it when the evidence supports an action or clearly states why it stays open. Choose the next check from the table, not from a checklist: a confusing action leads to the interface and card text; a redundant machine leads to production/consumption figures; a dull fight leads to enemy roles and warning timing; weak exploration leads to discoveries, markers and story delivery.

## 6. Verification rules

- Reuse existing evidence for unchanged systems; no full-suite runs, soaks or timer-waiting when a prepared state answers the question.
- A debug-assisted encounter never proves natural pacing. Passing tests never prove a mechanic is enjoyable. Calculations are labelled as calculations; observed numbers as observed; a non-fighting run gives an upper bound on damage.
- Every proposed balance change states the player behaviour it improves; cost or time increases need a reason beyond slowing progress.

## 7. Deliverables

- `GAMEPLAY_FUN_AUDIT.md` (update in place): evidence and scope; plain-language assessment; playable vs planned bins; strongest parts; ten most important problems (class I/D/P/M/U); core loop; reference-game table with links beside claims; progression; combat; story (proposals labelled); archetype test; prioritised improvements in four buckets with dependencies and a check each; subjective scores with confidence; investigation table; uncertainties and a next-playtest checklist; the closing question "Why would someone keep playing Relight tomorrow, and what currently makes them stop?"; sources; delivery checks.
- `evidence/gameplay-audit/` with the play log and key screenshots (overwrite per run, or date-suffix if the owner wants history).
- One `TASKS.md` §8 line; no status change unless the owner assigns one.
- Final message: assessment, strongest parts, top problems, comparison, progression, combat/story, prioritised fixes, uncertainties, the closing answer, and the created/edited file list.
