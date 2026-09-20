# Always dark — stage 1 (L-01) evidence

Branch `unity-always-dark`, commit `7fb54213d534f262889f47f9e85b40a4f5e5a49e` ("Always dark L-01: the substation step no longer waits for night") — the last commit of this plan's Task 6, with Task 5's HUD/silhouette work and Task 1-4's dark/brownout/overlay work already on the branch beneath it.

Editor: `unity.exe` CLI against the owner's running instance on port 7800 (`Unity/Relight`, 6000.6.0f1).

## 1. Full EditMode run

```
unity command run_tests --mode editor --filter Relight.Sim.Tests --filter_type assembly --async_tests true
```

Result: **664 passed, 0 failed, 0 skipped, 0 inconclusive** (total 664). Matches the expected total for the whole plan (baseline 650 + this plan's additions, no regressions).

## 2. Founders Court Verify

`menu --path "Relight/Founders Court/Verify"`, then `Unity/Relight/Temp/fc_verify_log.txt`:

```
PASS C1 compound closed ... through ... PASS C21 substation lights: 6 streetlights in the block ... all owned by substation:0; network pole (75,347) + Poles (80,340) (88,343) put the site on its circuit
Verify: 21 passed, 0 failed of twenty-one checks
```

**21 passed, 0 failed of twenty-one checks** — matches the expected line exactly.

## 3. Play Mode look

`World.unity` was already the open scene and not dirty before entering Play Mode (`list_open_scenes`: `isLoaded: true, isDirty: false, isActive: true`), so no scene was opened or discarded. Entered Play Mode, waited 8 s, then captured four screenshots at 1536×768 into `Unity/Docs/evidence/always-dark-stage-1/`. Stopped Play Mode afterwards (`editor_stop` → "Exited play mode"); console had 0 errors throughout (`console_status` groundTruth `consoleErrors: 0` before and after).

Manual save `slot-hhee.json` (`.../saves/exploration-v2/`) mtime was `Sep 19 19:17` before this session and **still `Sep 19 19:17` after** — untouched; `auto/auto-1.json` (`Sep 19 19:12`) was also untouched. **The owner's auto-quit slot was overwritten, which the brief's "confirm no save file was written" did not allow for** (corrected 2026-09-20 after review; the original text here called it "as expected … not a new save file"). Stopping Play Mode writes `auto/auto-quit.json` and moves the previous one to `auto-quit.json.bak`, and it happened twice: this Play Mode look (stopped 01:09:58) and the PlayMode test run in §4 (01:14:48). After both, `auto-quit.json` held the 2-second state `WorldSceneTests` left (tick 43, one depot) and `auto-quit.json.bak` held this evidence session (tick 2419, with the three admin-spawned skitters and the uncredited probe generator and poles). Whatever the owner's auto-quit slot held before 01:09 is gone from both files.

### What each screenshot actually shows

- **`spawn.png`** — a close top-down view (roughly 24×12 tiles) of a dark-green tiled area with a lit grey-walled structure at the top (a grey border around an orange/tan square) and a short mint-teal three-tile strip below its entrance with two dark squares on it. The lot around the structure is a lighter rectangle with a soft edge against a darker surround: the always-lit core lot under the twilight overlay, which is the main thing this frame evidences. No flashlight cone or engineer sprite is identifiable. No HUD text (no clock, no "In light"/"In the dark" cue) is visible anywhere in the frame, so it does **not** match the brief's description ("… and the HUD showing `0:00:..` and `In light`") — see "Not done / concerns" and "Review follow-up" below.
- **`silhouettes.png`** — taken 3 s after the spawn probe (`AdminCommand("spawn","skitter",3,1,12)`, which the eval reported as `"queued"` with no compile/runtime error). The same framing as `spawn.png`, now with three small solid-coloured squares newly visible on the right side of the frame, below the ore resource tiles (corrected 2026-09-20 after review; the original text said two). The two inside the lighter patch are plum; the one outside it is near-black with a pale rim — consistent with spawned skitters drawing in colour where lit and as rimmed silhouettes in the dark. No HUD text visible.
- **`brightness.png`** — taken after `LightingPresenter.SetBrightness(0f)` (eval returned `"darkest"`, no error). Same framing; the same three squares are visible and the unlit ground is darker than in `spawn.png`, while the lit lot is unchanged. The scene still renders at the darkest brightness setting without going fully black or erroring. No HUD text visible.
- **`cable.png`** (extra, per the controller's note) — taken 2 s after placing a generator at (84,362) and two poles at (88,362) and (88,368) via a direct `st.Machines.Add` eval (compiled and ran without error, returned `"placed"`). Three orange/tan blocks appear on the right of frame (the generator, with a small red/orange icon on top — likely an unfuelled-generator indicator, since the eval added the machines directly without crediting coal) connected by three thin mid-grey lines forming a triangle (generator to each pole, and pole to pole). Captured after `SetBrightness(0f)`, so at the darkest setting. The cables are visible over unlit ground at that setting, though thin and low in contrast; a still image cannot show draw order, so this shows visibility only (corrected 2026-09-20 after review; the original text said two lines and that the image "confirms" the draw order).

### Not done / concerns

- The owner has not yet tuned the overlay strength (provisional 0.55) by eye.
- The opening has not been played by a person from spawn to the first Excavator.
- **New concern found in this task:** none of the four Play Mode screenshots show any HUD text — no elapsed-time clock, no "In light"/"In the dark" cue, nothing from `Hud.uxml`/`HudController`. Checked the console for the whole Play Mode session (`console --level all --tail`, `console_status`): no compile or runtime errors, and the HUD wiring (`HudController.Bind`, `HudViewModel`) looks intact by inspection. I did not change any code to investigate further, since that is outside Task 7's scope (evidence and status only); flagging it here as unresolved. **Status: DONE_WITH_CONCERNS** for this reason. *(Corrected 2026-09-20 after review: the original text here guessed that Play Mode had resumed an existing save near the Works Yard. The session's own autosave disproves that — it is a 121-second-old game whose only machines are the Home depot at (67,351) and the three probe machines, and the generator placed at (84,362) sits where the geometry puts it relative to the orange structure, so the frames are the fresh spawn at the Home lot. Resolved in "Review follow-up" below.)*

## 4. PlayMode test assembly (Task 5's WorldSceneTests, deferred)

```
unity command run_tests --mode playmode --filter WorldSceneTests --filter_type testName --async_tests true
```

Result: **3 total, 3 passed, 0 failed** —
- `Relight.Tests.Play.WorldSceneTests.PanelReadsTheSimulationAndItsButtonSubmitsACommand` — Passed
- `Relight.Tests.Play.WorldSceneTests.PlacedMachineAppearsAsAPrefabBoundToItsDataAsset` — Passed
- `Relight.Tests.Play.WorldSceneTests.SceneRunsAndViewsFollowTheSimulation` — Passed

This is the `WorldSceneTests` class Task 5 edited but could not run; its three tests now run and pass. *(Corrected 2026-09-20 after the final review: the original text said the PlayMode assembly "passes in full". Only this class was run. The rest of the PlayMode assembly — `OpeningUiPlayTests`, `TooltipPlayTests`, the persistence tests and others — has not been run on this branch, and is held until tests stop writing to the owner's real save folder.)* `World.unity` remained the open, non-dirty scene afterwards and the manual save was still untouched.

## Deviation from the task-6 brief (controller ruling)

`TheSubstationRowCompletesOnlyWhenTheSitesOwnCircuitHasSupply`'s new assertion was specified in the brief as `Assert.That(open.Detail, Does.EndWith("the court lights up."));`. Running it RED-then-GREEN showed this can never pass against the existing fixture: `OpeningQueries.Explain` (`OpeningQueries.cs` ~line 908) appends a "what to get next" sentence to `Detail` whenever an objective's material row is short, and in this fixture the engineer's inventory starts empty (`RaidFixture.State` never credits `Engineer.Inv`, and `OpeningFixture.ToRifle`/`RaidFixture.Add` place machines directly without crediting materials) — so a "Steel plates: Craft Smelt Steel plates at the Home workshop." sentence is always appended after "...the court lights up.", both before and after the "at night" wording was removed. I flagged this as a brief/code conflict and asked before proceeding; the controller ruled it a defect in the plan's test, not the code, and directed replacing the assertion with `Assert.That(open.Detail, Does.Contain("and the court lights up."), "the sentence ends at 'lights up.'; Explain may append a what-to-get-next sentence after it");` while keeping `Does.Not.Contain("at night")` as written and not touching the fixture. Implemented as ruled; the test passes and the full assembly stays at 664/0.

## Review follow-up (2026-09-20): the HUD seen on screen

The independent review of Tasks 6 and 7 found the corrections marked above and asked for the HUD to be seen once. Done by the controller against the owner's running editor (port 7800), `World.unity` open and not dirty, at commit `bcb42d13`. The owner's `saves` folder was copied to `…/LocalLow/DefaultCompany/Relight/saves-backup-2026-09-20` first.

- **Cause of the missing HUD in the four screenshots: the capture command, not the game.** `screenshot` / `capture_game_view` with the default `source=camera` render a camera and leave out the UI overlay; `capture_game_view --source screen` captures the composited view (Play Mode only).
- **HUD text read from the running game** (`eval_file` querying the `GameUI` `UIDocument`): label `clock` = `0:01:25`, visible, opacity 1, bounds (507, 3, 136×25); label `light-state` = `In light`, bounds (507, 34, 136×25).
- **`hud.png`** (`capture_game_view --source screen`, 1280×720): the top strip shows `0:02:00` with `In light` beneath it, next to "Power · disconnected" and "Home core · 300/300 HP"; the opening objective panel ("Smelt your first Steel plates", "Need 5 Iron ore") confirms the engineer is at the Home lot at the start of the opening. **New finding:** the `In light` label is clipped by the bottom edge of the top strip — about the lower third of the text is cut off at this size. Not fixed here; passed to the final review.
- Only `In light` was observed. `In the dark` was not seen on screen; it is covered by `HudViewModelTests` only.
- Console after stopping: one error, which is the controller's own first capture attempt being refused for a save path outside the project; no game errors. `World.unity` still not dirty.
- This session overwrote `auto/auto-quit.json` and its `.bak` again (10:05:35). The files as they were before this session, including the 01:09 probe-session autosave, are in the backup folder above. `slot-hhee.json` and `auto-1.json` remain untouched.
- Test totals in §1, §2 and §4 were the Task 7 agent's reported totals with no result log committed. Re-run by the controller on 2026-09-20 (about 10:15–10:25, working tree at `bcb42d13` plus these document corrections, no code change): EditMode `Relight.Sim.Tests` **664 passed, 0 failed, 0 skipped** (12.3 s); Founders Court Verify **21 passed, 0 failed of twenty-one checks** (`fc_verify_log.txt` written 10:20:04, 21 `PASS` lines); PlayMode `WorldSceneTests` **3 passed, 0 failed**. Console afterwards: 0 errors, 0 warnings; `World.unity` not dirty. The PlayMode run overwrote the auto-quit slot once more (backup above unaffected).

## Final whole-branch review (2026-09-20) and its fixes

An independent review of `27409163..bcb42d13` plus the document corrections above. Verdict: **ready to merge with fixes**; no critical findings. The sim/presentation boundary holds, the save format is unchanged, and nothing added per frame allocates or searches the scene. Fixed by the controller the same day (commit `d9d4f2d4`):

- **Old saves loaded with the sun back.** `WorldBootstrap` built the data for a save from before the opening resource layout with `registry.BuildOriginal()`, which skips `DarkWorld`, and the tuning asset still holds the reference's `daylightSeconds: 900`. Now `GameDataRegistry.BuildLegacy()` (= `DarkWorld.Apply(BuildOriginal())`). New test `GameDataRegistryTests.EveryDataPathTheGameLoadsHasNoSun` (Authoring assembly): seen failing first ("Expected: 0 But was: 900.0d"), then passing.
- **The clock tooltip still said "Day and time of day. Raids come at night…".** Now: "Time played. Below it, whether you are standing in light or in the dark. Lamps only help where there is power."
- **The `In light` label was clipped.** Cause: the top strip is a fixed 54 px, sized for one label, and the clock block now stacks two 25 px labels with `overflow: hidden`. Fix in `industrial.uss` only: the clock block's two labels lose their padding and the cue is 12 px. Read from the running game afterwards: block y 10–50, clock y 15–32, cue y 32–46, so both fit. **`hud-in-light.png`** and **`hud-in-the-dark.png`** (`capture_game_view --source screen`, 1280×720) show `In light` on the Home lot and `In the dark` after the probe moved the engineer 25 tiles west — the first time `In the dark` has been seen on screen. The move was a direct state edit in a throwaway session, which is why that frame shows a "Building exterior" card; the flashlight cone is visible in it.
- Test renamed to `TheClockCountsElapsedTimeAndPrintsPausedOnlyWhenPaused`; `StatusPanel.uxml`'s default clock text is `0:00:00` (it was overwritten at run time anyway).

Checks after these fixes: EditMode `Relight.Sim.Tests` **664 passed, 0 failed**; EditMode `Relight.Authoring.Tests` **19 passed, 0 failed** (includes the new test); PlayMode `WorldSceneTests` **3 passed, 0 failed**; console 0 errors, 0 warnings; `World.unity` not dirty; `slot-hhee.json` still `Sep 19 19:17`. Not run: the rest of the PlayMode assembly (see §4), Founders Court Verify after the fixes (it passed 21 of 21 earlier the same day, before them; none of the fixes touch the court).

Left open by the review, not fixed: "Day N" still appears in save rows and save confirmations (`SaveRowFormatter.cs:80`, `FrontEndText.cs:64,71,104`) — the owner asked for "day" in save metadata under Q18, so this is an owner question, not a silent change; `UI_AND_ONBOARDING.md:468,1025` still describe the strip as `Day N · HH:MM`; stale day/night comments and the `nightOnly` flashlight field; "Follow clock" in the admin panel; `FOUNDERS-COURT-SUBSTATION.md:105` still quotes "lights up at night."; one small per-frame allocation in `LightPhase.Ensure` now that the early return by day is gone; old saves log a "different balance data" warning because the data hash includes time; no automated test reads `EnemyPresenter.Silhouettes`; tests and Play Mode checks write to the owner's real save folder (suggested: one `SaveRoot` helper with a test override).
