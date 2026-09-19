# Court D55 substation implementation — checks performed (2026-09-19)

Run on the Desktop clone (`C:\Users\dw\Desktop\Relight`, `main` at `27409163` plus the uncommitted change). Sections 1–2 ran before an
editor was open. Section 3 ran in the owner's 6000.6.0f1 editor through the Unity CLI. No Play Mode observation has
been made.

## 1. Relight.Sim tests — scratch `dotnet test` over the real sources

- Project: `scratch-sim-tests.csproj.txt` (net8.0, NUnit 3.14.0, NUnit3TestAdapter 4.6.0, Microsoft.NET.Test.Sdk
  17.11.1), in `%TEMP%\relight-substation\simtests`, compiling `Assets/Relight/Sim/**` and `Tests/Sim/**` in place.
- Full suite (`sim-full.trx`): **648 passed, 0 failed, 2 skipped** (the console total was 650).
- The seven D55 tests, all passing:
  - `PowerNetworkTests.APoleWithinReachOfASubstationSiteLinksItToTheCircuit`
  - `PowerNetworkTests.AnUnreachedSubstationSiteHasNoCircuit`
  - `PowerNetworkTests.ASubstationSiteNeverOwnsAConsumingMachine`
  - `PowerNetworkTests.ASubstationSiteBridgesTwoPoleNetworks`
  - `LightTests.AStreetLightRunsFromItsNearestSubstationNotTheNearestPole`
  - `LightTests.AnAuthoredStreetLightDrawsItsKwAndLightsItsRadiusWhenTheCircuitIsUp` (existing; still passes, which
    covers the fallback for regions without substation sites)
  - `OpeningObjectiveTests.TheSubstationRowCompletesOnlyWhenTheSitesOwnCircuitHasSupply`
- Runtime difference: .NET 8, not Unity's Mono.

## 2. Compile check — scratch `dotnet build` against the 6000.5.8f1 engine and editor DLLs

- Project: `scratch-unity-compile.csproj.txt` (netstandard2.1) compiling `Sim/**`, `World/**`, `Data/**`,
  `Presentation/**`, `Relight/Editor/**` and `Assets/Editor/**` against every `UnityEngine/*.dll` of the installed
  6000.5.8f1 (the project uses 6000.6.0f1) and its `Newtonsoft.Json.dll`.
- The first build had 8 errors, all in files this change does not touch and all missing-package references:
  `Presentation/WorldInput.cs` and `Presentation/Light/MouseFlashlightPresenter.cs` (Input System) and
  `Relight/Editor/Scene/SceneSetup.cs` (`Relight.UI`). With those three excluded (`unity-compile.log`):
  **Build succeeded, 0 warnings, 0 errors**. That covers the edited `PowerConnectionPresenter.cs` and
  `Assets/Editor/FoundersCourtCompound.cs` (Verify C21).
- This shows that the code compiles. It does not show that the cables draw, that the court lights come on at night
  or that Verify C21 passes on the real scene.

## 3. Editor runs through the Unity CLI (added the same day, once the owner opened the project)

The owner opened the project in Unity **6000.6.0f1**. It was driven with the Unity CLI (`unity` 1.0.0-beta.8, Pipeline
package, port 7800):

- `console_status`: `compilationFailed: false`, 0 errors. The 4 warnings are the existing
  `UI/Styles/industrial.uss` `-unity-slice-scale` unit warnings and are unrelated to this change.
- `run_tests --mode editor --filter Relight.Sim.Tests --filter_type assembly`: **650 passed, 0 failed, 0 skipped**
  in Unity's own Test Runner.
- `open_scene Assets/Relight/Scenes/World.unity`, then `menu "Relight/Founders Court/Verify"`
  (`fc_verify_log.txt`): **21 passed, 0 failed of twenty-one checks**. C21 reads: six streetlights in the block
  (`light:0`–`light:5`), all owned by `substation:0`; the network pole (75,347) plus Poles (80,340) and (88,343) put the
  site on its circuit. The only scene open before this was an empty, unmodified, untitled scene.

## 4. Not run

- Play Mode night check: chain Poles to the Works Yard substation, see step 8 complete, the cable to the lot and the
  court's six lights come on.
