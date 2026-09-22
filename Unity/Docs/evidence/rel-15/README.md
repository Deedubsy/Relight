# REL-15 — a turret boxed in by buildings says so on its own card

INT-11, fun-audit problem 9: *"a turret inside the workshop walls never fires"*, recorded 2026-09-14 as legal,
silent and fatal, and never reproduced.

Built 2026-09-23. Code, automated checks and one staged editor session only. **Nobody has played this build.**

## The premise, checked first

Half of the audit's claim had already moved. GP-W3 gave placement `TurretSight`, so the build cursor draws the
real coverage polygon and prints a sentence before the gun goes down. Measured live against the loaded city,
at the Home workshop rect (65,347) 10 × 14:

| Question | Answer |
| --- | --- |
| Legal turret tiles in the rect | 32 |
| …that see a quarter of the compass or less | 20 |
| Blindest legal tile | (67,348), `OpenFraction` **0.294** |
| What the build cursor already says there | "Blind position: buildings block this turret's line of fire. It covers about 29% of its range. Its clearest field of fire is to the S." |
| `TurretQueries.Blind` once it is built | **false** — that query is the darkness rule (U-D-59) and deliberately excludes wall blindness |
| What the built turret's card said | "running · Power: powered (100%)" and its ammunition. **Nothing about the blindness.** |

So the defect that survives is not "no warning" but "the warning is only shown before the build". A player who
did not read the cursor had a turret that never fired and no way left to find out why.

## What changed

The issue offered two outcomes. This takes the second — *"the turret's hover says why it cannot see"* — and
leaves placement permissive, because GP-W3 already decided to warn rather than refuse and a gun covering one
doorway is a real choice.

* `TurretSight.BuiltAdvice` answers for a turret that is already standing there, and returns the placement
  sentence **verbatim**: one state must not get a second vocabulary. Empty at or above `ClearFraction`, so an
  ordinary turret says nothing. Empty for a wreck, whose repair line owns the louder problem.
* `ProductionQueries.Description` appends it, which is the one text both the world hover card
  (`UI/Hud/GameplayDock.cs`) and the machine panel (`UI/Inventory/InventoryPanelController.cs`) read.
* `TurretSightCache` keys the sweep on `SimState.Rev`. `TurretSight.Coverage` is about 1,700 sight tests — fine
  once per placement, far too much for a card that asks every frame — and `Rev` bumps on exactly the events that
  can change a gun's line of fire (a machine added, removed, rotated or wrecked). Never visited, so no save
  field and no schema change; a load rebuilds it on the first question anyone asks.

### Scope deviation, stated

The issue named `Sim/Building/Placement.cs` and `Sim/Combat/Turrets/TurretQueries.cs`. The change lands in
`Sim/Combat/Turrets/TurretSight.cs` and `Sim/Production/Machines/ProductionQueries.cs` instead, because the
sentence and the card already live there and placement is deliberately left permissive. `Placement` is
unchanged; a test asserts that it still does **not** refuse the position.

## The two frames

Both are the same staged session, the same gun turret kind, both powered at 100% and loaded 50 / 50, so the
only difference between the cards is the position.

| | |
| --- | --- |
| [`blind-position.png`](blind-position.png) | The turret at (67,348), inside the Home workshop yard. Card: "running · Power: powered (100%) / Loaded 50 / 50 rounds / **Blind position: buildings block this turret's line of fire. It covers about 29% of its range. Its clearest field of fire is to the S.**" |
| [`clear-position.png`](clear-position.png) | The turret at (70,342), the nearest legal tile with a clear field of fire (`OpenFraction` 0.717). Card: "running · Power: powered (100%) / Loaded 50 / 50 rounds" — and nothing else. No news is still good news. |

### How the frames were staged

Editor eval scripts against the running Play session, not a player path: `Placement.Add` puts each turret and
its pole and generator down directly, so no build cost is charged, and `WorldInput.OpenMachine` opens the
machine panel by id because the Input System overrides an injected pointer position (the world hover card reads
the same `ProductionQueries.Description` string, `GameplayDock.cs:143`). The HUD reads "Home core · DISABLED" in
both frames: a raid reached the core while the frames were being staged. It is unrelated to what the frames show.

## Checks

| Suite | Result | Before |
| --- | --- | --- |
| Offline sim (`dotnet test`) | **925 passed, 1 skipped** | 922 passed, 1 skipped |
| Unity EditMode | **1021 / 1021** | 1018 / 1018 |
| Unity PlayMode | 97 total — **92 passed, 0 failed**, 5 skipped | unchanged |

Three new tests in `Tests/Sim/Combat/Turrets/TurretSightTests.cs`:

1. `ATurretBoxedInSaysSoOnItsOwnCardAndIsStillAllowedToStandThere` — `Placement.GeometryProblem` is still empty
   (the acceptance criterion's "refusal or a stated reason", answered with the reason), and the card contains
   the build cursor's own sentence, compared string-for-string rather than by keyword.
2. `ATurretWithAClearFieldOfFireAddsNothingToItsCard`.
3. `KnockingTheWallsDownTakesTheWarningOffTheCardAgain` — wrecking the ring clears the line again, which is what
   proves the `Rev`-keyed cache cannot go stale.

The courtyard those tests build has a 2 × 2 hole, not the 1 × 1 the older `TurretSight` tests use: a Gun
turret's footprint is 2 × 2, and the older tests bypass `Placement` with a direct `Add`, so they never noticed.

## Limits

* The balance half of fun-audit problem 9 — 8 dps of unseen damage — is **not** addressed here and remains open.
* Nobody has played this build. There is no human verdict on whether the sentence is noticed in play.
* The thresholds are GP-W3's unchanged (`BlindFraction` 0.40, `ClearFraction` 0.70); this issue did not retune them.
