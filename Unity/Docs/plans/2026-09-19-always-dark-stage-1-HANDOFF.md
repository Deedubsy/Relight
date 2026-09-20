# Always dark stage 1 (L-01) — where we are up to

Written 2026-09-20, just before the owner's PC restarted. Read this first when resuming.

## State

- Branch: `unity-always-dark` (local only, nothing pushed). `main` is untouched at `27409163`.
- All seven tasks of `Unity/Docs/plans/2026-09-19-always-dark-stage-1.md` are implemented and committed:

| Commit | What |
|---|---|
| `6e24d667` | Founders Court substation powers the court lights (D55) |
| `9df3b157` | Always dark decision U-D-58, spec, document corrections |
| `b7f55aac` | Stage 1 plan |
| `f353a17c` | Task 1: the data has no sun; `Daylight` answers dark |
| `9448e359` | Task 2: brownouts shrink every powered light |
| `2781ba8f` | Task 3: twilight overlay strength (0.55) and the brightness setting |
| `6fafec3c` | Task 4: feedback draws above the darkness; alien silhouettes |
| `f66362eb` | Task 5: elapsed-time HUD clock and the in-light cue |
| `7fb54213` | Task 6: the substation step no longer says "at night" |
| `0ea23698` | Task 7: evidence and status |

- Last reported checks (from the Task 7 agent, not yet re-checked by the controller): EditMode `Relight.Sim.Tests` 664 passed, 0 failed; Founders Court Verify 21 of 21; PlayMode `WorldSceneTests` 3 passed.
- Evidence: `Unity/Docs/evidence/always-dark-stage-1/checks.md` and four screenshots.

## Reviews

- Tasks 1–5: each passed an independent task review.
- Tasks 6 and 7: reviewed 2026-09-20. Task 6 approved. Task 7 needed fixes to its record only; the corrections are in `checks.md` ("Review follow-up"), `TASKS.md` and the spec status line.
- The final whole-branch review ran 2026-09-20: ready to merge with fixes, no critical findings. Its before-merge fixes are made and checked (see `checks.md`, last section): commit `d9d4f2d4`, with the records in the commit after it.

## What is left to do

1. ~~Review Tasks 6+7~~ Done 2026-09-20.
2. ~~The Task 7 agent's concern~~ Done 2026-09-20: the screenshots were a fresh spawn, the capture command had left out the UI, and the HUD clock and "In light" are now seen on screen (`hud.png`). Original note: Look at the Task 7 agent's concern: the Play Mode screenshots show no HUD text and look like a resumed save near the Works Yard, not a fresh spawn. Likely causes to check: the `screenshot` command renders the camera and misses the Screen Space overlay UI (use `capture_game_view` with `source=screen`), and Play Mode continues the auto-quit save. The HUD clock and the "In light" cue still need to be seen on screen once.
3. ~~Run the final whole-branch review~~ Done 2026-09-20, fixes made. Owner question from it: save rows still say "Day N" — keep or change?
4. Decide with the owner how to finish the branch (merge to `main`, PR, or keep).
5. Owner's own checks (task L-ACC): tune the overlay strength by eye (0.55 now; pale Works Yard paving still reads fairly bright), and play the opening in the dark from spawn to the first Excavator.
6. Then stage 2 (task L-02): hesitation at lit edges, raids prefer dark approaches, what stops a bullet stops light, coverage preview, relief events, perception, teaching the rule. It needs its own plan.

## Deferred minor findings (for the final review to triage)

- `SettingsController.OnBrightness` inlines `Preferences.Save()` + `ShowStatus()` instead of calling the existing `Persist()` helper.
- `OnBrightness` calls `FindAnyObjectByType<LightingPresenter>()` on every slider change instead of caching it.
- `EnemyPresenter` silhouette block calls `Scale(body, bodyTiles)` twice.
- Test method `TheClockCountsDaysAndPrintsPausedOnlyWhenPaused` still says "Days".
- The HUD's `In light` label is clipped by the bottom edge of the top strip (seen in `hud.png`, 1280×720).
- `StatusPanel.uxml`'s `clock` label still has the default text "Day 1 · 00:00"; check whether the Status panel clock was meant to change with the HUD clock.
- Play Mode checks and PlayMode tests write to the owner's real save folder (auto-quit slot); consider pointing them at a scratch save root.

## Rulings the controller made on the owner's behalf

1. Worked in place on the branch, no git worktree, because the open Unity editor is bound to this checkout.
2. Added `.superpowers/` to `.git/info/exclude` (local only; no repo file changed).
3. Task 5's commit trailer says "Claude Sonnet 5" (the agent's own model) instead of the plan's fixed "Claude Fable 5.1". Kept: truthful attribution. Later agents were told to use their own session's trailer.
4. Task 6: the plan's assertion `Does.EndWith("the court lights up.")` could never pass, because the objective text gets a "what to get next" sentence appended when a material is short. Changed to `Does.Contain("and the court lights up.")`.

## Notes

- The owner's saves as they were on the morning of 2026-09-20 are copied to `%USERPROFILE%\AppData\LocalLow\DefaultCompany\Relight\saves-backup-2026-09-20`. The owner's auto-quit from before Task 7 was already lost by then.
- Every Play Mode stop rewrites the auto-quit save under `%USERPROFILE%\AppData\LocalLow\DefaultCompany\Relight\saves\exploration-v2\auto`. The manual save `slot-hhee.json` was not touched when last checked.
- The working ledger and the agents' reports are in `.superpowers/sdd/2026-09-19-always-dark-stage-1/` (git-ignored locally, survives a restart): `progress.md` is the ledger.
- Checks need the owner's editor open on `Unity/Relight`; the Unity CLI is `C:\Users\dw\AppData\Local\Unity\bin\unity.exe`.
- The owner's untracked files `Unity/Docs/CODE_AUDIT_2026-09-13.md` and `Unity/Docs/evidence/code-audit-2026-09-13/` are not part of this work and must not be committed with it.
