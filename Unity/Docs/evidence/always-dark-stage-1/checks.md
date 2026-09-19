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

Manual save `slot-hhee.json` (`.../saves/exploration-v2/`) mtime was `Sep 19 19:17` before this session and **still `Sep 19 19:17` after** — untouched. The `auto/` folder's `auto-quit.json` (and its `.bak`) were rewritten by the Play Mode stop, as expected: that is the normal save-on-stop behaviour, not a new save file.

### What each screenshot actually shows

- **`spawn.png`** — a close top-down view (roughly 24×12 tiles) of a dark-green tiled area with a lit grey-walled structure at the top (a grey border around an orange/tan square) and a short mint-teal three-tile strip below its entrance with two dark squares on it. No HUD text (no clock, no "In light"/"In the dark" cue) is visible anywhere in the frame. This does **not** obviously match the brief's description ("the lit core lot, twilight around it, the flashlight cone, and the HUD showing `0:00:..` and `In light`") — see "Not done / concerns" below.
- **`silhouettes.png`** — taken 3 s after the spawn probe (`AdminCommand("spawn","skitter",3,1,12)`, which the eval reported as `"queued"` with no compile/runtime error). The same framing as `spawn.png`, now with two small solid-coloured squares (one dark, one maroon/dark-red) newly visible on the right side of the frame, near the ore resource tiles — consistent with spawned skitters rendering as flat silhouette squares. No HUD text visible.
- **`brightness.png`** — taken after `LightingPresenter.SetBrightness(0f)` (eval returned `"darkest"`, no error). Same framing; now three silhouette squares are visible (one dark-red/maroon, two near-black), consistent with all three spawned skitters now on screen. The scene still renders at the darkest brightness setting without going fully black or erroring. No HUD text visible.
- **`cable.png`** (extra, per the controller's note) — taken 2 s after placing a generator at (84,362) and two poles at (88,362) and (88,368) via a direct `st.Machines.Add` eval (compiled and ran without error, returned `"placed"`). Three orange/tan blocks appear on the right of frame (the generator, with a small red/orange icon on top — likely an unfuelled-generator indicator, since the eval added the machines directly without crediting coal) connected by two thin light-grey/white lines running between them. This confirms a power cable **is** drawn above the darkness and is visible against the dark ground, answering the earlier review's concern.

### Not done / concerns

- The owner has not yet tuned the overlay strength (provisional 0.55) by eye.
- The opening has not been played by a person from spawn to the first Excavator.
- **New concern found in this task:** none of the four Play Mode screenshots show any HUD text — no elapsed-time clock, no "In light"/"In the dark" cue, nothing from `Hud.uxml`/`HudController`. Checked the console for the whole Play Mode session (`console --level all --tail`, `console_status`): no compile or runtime errors, and the HUD wiring (`HudController.Bind`, `HudViewModel`) looks intact by inspection. The camera framing in all four captures also looks like a very tight zoom on a fixed structure near the Works Yard, not the wide (~40-tile) view of the core lot the briefs describe elsewhere — consistent with Play Mode resuming the existing `exploration-v2` save (whose auto-quit slot already existed from a prior session) rather than starting a fresh game at spawn. I did not change any code to investigate further, since that is outside Task 7's scope (evidence and status only); flagging it here as unresolved. **Status: DONE_WITH_CONCERNS** for this reason.

## 4. PlayMode test assembly (Task 5's WorldSceneTests, deferred)

```
unity command run_tests --mode playmode --filter WorldSceneTests --filter_type testName --async_tests true
```

Result: **3 total, 3 passed, 0 failed** —
- `Relight.Tests.Play.WorldSceneTests.PanelReadsTheSimulationAndItsButtonSubmitsACommand` — Passed
- `Relight.Tests.Play.WorldSceneTests.PlacedMachineAppearsAsAPrefabBoundToItsDataAsset` — Passed
- `Relight.Tests.Play.WorldSceneTests.SceneRunsAndViewsFollowTheSimulation` — Passed

This is the PlayMode assembly Task 5 edited but could not run; it now runs and passes in full. `World.unity` remained the open, non-dirty scene afterwards and the manual save was still untouched.

## Deviation from the task-6 brief (controller ruling)

`TheSubstationRowCompletesOnlyWhenTheSitesOwnCircuitHasSupply`'s new assertion was specified in the brief as `Assert.That(open.Detail, Does.EndWith("the court lights up."));`. Running it RED-then-GREEN showed this can never pass against the existing fixture: `OpeningQueries.Explain` (`OpeningQueries.cs` ~line 908) appends a "what to get next" sentence to `Detail` whenever an objective's material row is short, and in this fixture the engineer's inventory starts empty (`RaidFixture.State` never credits `Engineer.Inv`, and `OpeningFixture.ToRifle`/`RaidFixture.Add` place machines directly without crediting materials) — so a "Steel plates: Craft Smelt Steel plates at the Home workshop." sentence is always appended after "...the court lights up.", both before and after the "at night" wording was removed. I flagged this as a brief/code conflict and asked before proceeding; the controller ruled it a defect in the plan's test, not the code, and directed replacing the assertion with `Assert.That(open.Detail, Does.Contain("and the court lights up."), "the sentence ends at 'lights up.'; Explain may append a what-to-get-next sentence after it");` while keeping `Does.Not.Contain("at night")` as written and not touching the fixture. Implemented as ruled; the test passes and the full assembly stays at 664/0.
