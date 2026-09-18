# Superseded browser retests — U-2, U-5, U-6 (Wave 3 · Worker C, C-12)

UI_AND_ONBOARDING.md §9.1 Q20 rules that the three browser retests left open in §9 are replaced by Unity checks.
This file records that replacement in the only honest form available: the browser retest **was not performed**, and
the Unity check that supersedes it is named. Nothing below may be copied into the docs as "passed".

| Row | Browser retest status | Superseded by | Where | Run state |
|---|---|---|---|---|
| **U-2** — one tool occupies one action-bar slot | **browser retest not performed** — superseded by Unity check `OpeningDefectsTests.U2_AssigningOneToolByThreePathsLeavesItOnExactlyOneActionBarSlot` (with `U2_TheDisplacedToolTakesTheVacatedSlot` and `U2_LoadingABarHoldingOneToolSeveralTimesCollapsesToTheFirstOccurrence`), and on the UI side by `OpeningUiPlayTests.U2_TheRenderedActionBarShowsOneToolInExactlyOneSlot` | `Tests/Sim/Regression/OpeningDefectsTests.cs`; `Tests/Play/OpeningUiPlayTests.cs` | sim checks **green** in the scratch loop; Play check **not run** (needs Unity) |
| **U-5** — HUD status strip is content-width | **browser retest not performed** — superseded by Unity check `OpeningUiPlayTests.U5_TheHudStatusStripIsContentWidthAtEveryLayoutInTheMatrix` | `Tests/Play/OpeningUiPlayTests.cs` | **not run** (needs Unity); has no sim half — width is only a fact about the rendered panel |
| **U-6** — core repair reachable in one interaction | **browser retest not performed** — superseded by Unity check `OpeningDefectsTests.U6_CoreRepairIsOneQueryAndOneCommandFromTheDamagedCore` (with `U6_ADisabledCoreOffersItsRecommissionOnTheSameCardAndAcceptsItInOneCall` and `U6_AnUndamagedCoreStillProducesACardSoTheDrawerNeverOpensEmpty`), and on the UI side by `OpeningUiPlayTests.U6_InteractingWithTheCoreOpensOneDrawerWithTheCoreCardFirst` | `Tests/Sim/Regression/OpeningDefectsTests.cs`; `Tests/Play/OpeningUiPlayTests.cs` | sim checks **green** in the scratch loop; Play check **not run** (needs Unity) |

## Notes for the coordinator

1. **"Not run" is not "failed" and is certainly not "passed."** Until the Play Mode assembly has been compiled and
   the three `OpeningUiPlayTests` have been run in batchmode, U-5 has **no** evidence of any kind on the Unity side,
   and U-2 and U-6 have sim-side evidence only. The §9 table should say so in those words.
2. Two of the Play checks call `Assert.Ignore` with a "needs seam: …" message when the element they measure does
   not exist yet (`hud-status-strip`, `bar-slot-<n>`, `repair-card-core`, a workshop/drawer panel id). An **ignored**
   result is also not a pass — it means the row is still unevidenced and the named seam is owed. The seam requests
   are listed in §6 of `wave3-WC-report.md`.
3. The old browser retests are not worth reviving: the Phaser build is the paused reference and its DOM/CSS has no
   bearing on the UI Toolkit layout that now has to be right. Superseding them is the correct call; recording them
   as satisfied without running the replacement would not be.

## Addendum — run state at the end of Phase C (coordinator, 2026-09-14)

This file was copied into `Unity/Docs/evidence/phase-c/` at the Phase C handoff. The table above is the Wave 3
record and is left as written; what changed afterwards is only the **Run state** column:

- The Play Mode suite ran on 2026-09-14 (07:00 UTC): **44 run / 40 passed / 0 failed / 4 skipped** — the skips are
  the 50 s timing form and three R7-Tilemap tests, by design; the `DiskRoundTrip` Phase1/2 and `EvidenceCapture`
  forms are `[Explicit]` and were not run. None of the three `OpeningUiPlayTests` above was ignored or skipped, so
  U-5 now has Unity evidence and U-2 and U-6 have it on both sides.
- **The browser retest status is unchanged and stays "not performed".** The Unity checks supersede those retests;
  they do not retrospectively pass them.
