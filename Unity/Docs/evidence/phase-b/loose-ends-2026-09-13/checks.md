# Loose-ends pass — checks performed (2026-09-13, U-M-38)

Everything here ran on this machine (`C:\Users\dw\Desktop\Relight`, a clone of `origin/main` pulled the same day),
against the **real project sources in place** — no project copy was made and nothing under `Unity/Relight` was
written by a check. Unity Editor 6000.6.0f1 is **not installed** on this machine (6000.2.x / 6000.4.0f1 / 6000.5.8f1
are), no editor was running, and the Unity MCP server reported `instance_count: 0`, so **no editor observation, no
EditMode/PlayMode Test Runner run and no batchmode run** was possible. The two checks below are what was possible.

## 1. Focused sim tests — scratch `dotnet test` over the real sources

- Project: `scratch-sim-tests.csproj.txt` (net8.0, NUnit 3.14.0, NUnit3TestAdapter 4.6.0, Microsoft.NET.Test.Sdk
  17.11.1 from nuget.org via `scratch-nuget.config.txt`), placed in `%TEMP%\relight-loose-ends\simtests` and
  compiling `Unity/Relight/Assets/Relight/Sim/**/*.cs` and `Tests/Sim/**/*.cs` by absolute path.
- Command: `dotnet test --filter "FullyQualifiedName~SaveUpgradeTests|…SaveRefusalTests|…SaveRoundTripTests|…AutosaveTests|…RecoveryTests|…HandCraftTests|…SharedChecksTests" --logger trx`
  (`RecoveryTests` also matches `RealDiskRecoveryTests`, which writes and damages real files under `%TEMP%`).
- Result (`sim-focused.trx`): **82 passed, 0 failed, 0 skipped** —
  `SaveUpgradeTests` 11 (new), `SaveRefusalTests` 17, `SaveRoundTripTests` 7, `AutosaveTests` 20, `RecoveryTests` 9,
  `RealDiskRecoveryTests` 1, `SharedChecksTests` 7, `HandCraftTests` 10 (one new: refund delivered after the player
  moves stacks into chests, before and after a save/load).
- Not run: every other `Relight.Sim.Tests` class (unchanged code; the repair-pass evidence stands), the two
  `[Explicit]` long forms, and the Unity Test Runner itself.
- Runtime difference to note: .NET 8 rather than Unity's Mono/.NET Standard 2.1; the sources compile for both, and
  the persistence tests exercised here have no engine dependency by construction.

## 2. Compile check for the engine-facing change — scratch `dotnet build` against UnityEngine.CoreModule

- Project: `scratch-presentation.csproj.txt` (netstandard2.1) compiling `Sim/**`, `Presentation/Host/SimHost.cs`,
  `Presentation/Persistence/AutosaveController.cs`, `Tests/Play/Scene/SceneFixture.cs` and the new
  `Tests/Play/Persistence/AutosaveSessionTests.cs` against `UnityEngine.CoreModule.dll` from the installed
  **6000.5.8f1** (not the project's 6000.6.0f1) plus `nunit.framework` 3.14.0; `scratch-presentation-Stubs.cs.txt`
  supplies the one attribute (`UnityEngine.TestTools.UnityTest`) whose assembly lives only in an editor's package cache.
- Result (`presentation-compile.log`): **Build succeeded, 0 warnings, 0 errors**.
- What this does and does not show: the changed files compile against the engine API they use (MonoBehaviour,
  Application, Debug, Object.FindAnyObjectByType, SceneManager); it does not run the PlayMode tests, does not
  load `World.unity`, and does not prove the `AutosaveController` component on the `Sim` object behaves in Play
  Mode — those are B-ACC checklist items 12–13.

## 3. Repository inspection — read-only

- `repository-inspection.txt`: what the parent repository tracks under `Unity/`, the ignore rules the parent honours,
  the snapshot/bundle verification (all `sha256` OK; both bundles complete), the tarball-vs-tracked-tree diff (one
  stray Test Runner scene only), and the accidental `relight-nested-git-dir-2026-09-12/` snapshot. Nothing was
  staged, committed, moved or deleted.

## Scratch folder

`%TEMP%\relight-loose-ends` was removed after the files above were copied here.
