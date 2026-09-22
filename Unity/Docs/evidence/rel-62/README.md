# REL-62 — what the HUD forgets on load

PER-02, with ENM-07 appended to it. Two things that live in the SESSION rather than the save, and so come back wrong
after a load: a standing defence row, and the seam the player's own shots reach bodies through.

Built 2026-09-23. Code and automated checks only. **Nobody has played this build.**

## The premise, checked first

| The claim | What the code said |
| --- | --- |
| `DefenceAlertSource._raised` is per session | True: `private readonly HashSet<string> _raised`, never visited, never saved. |
| After a load a standing "3 wrecks" row is gone until state changes | **Partly.** REL-14 had already made a dry turret, a low turret during a raid and a WRECKED TURRET raise their place again on the first refresh. What was left was the one case none of those cover: a place whose turrets are all loaded and standing, whose only damage left is wrecked walls or belts. That row vanished. |
| The target seam is transient and only `EnemyPhase` re-points it | True (`EnemyPhase.cs:33-34`), and the weapon phase runs first, so a loaded game's first tick saw a null target list. |
| `EnemyTests` sets the seam by hand, which hides this | True: the round-trip test called `new EnemyInitializer().Init(ctx, loaded)` itself. |

## What changed

**1. One place knows how to point the seam** — `EnemyTargets.Point(SimState)`. The tick phase, the initialiser and
`Simulation.Wrap` all call it, so they cannot drift apart, and no test has to reach in and set the seam to make
shooting work. `Simulation.Wrap` is the load path (`AutosaveController.cs:228`), so a loaded game can shoot on its
first tick instead of firing one tick of ghost bullets. One tick is 50 ms; this is a correctness fix, not a felt one.

**2. A standing wreck row is re-derived on the first refresh after a load.** `SimState.Resumed` is raised by
`Simulation.Wrap` and left false by `Simulation.NewGame` — transient, not in `Visit`, so in neither the save nor the
hash, and no phase reads it. On its first refresh against a resumed state, `DefenceAlertSource` seeds `_raised` from
the places that carry a wreck. The row the player quit with is there again.

Why the load has to be told apart from a fresh start: seeding on *any* first refresh would have made a wrecked wall
raise a row in a game being played, which is the half of U-D-61 that REL-14 deliberately kept
(`AWreckedWallOrBeltAloneNeverRaisesARow`). The flag is the smallest honest way to separate the two.

**The save schema is untouched — it stays at 9, as the issue requires.**

## Checks

| Suite | Result | Before |
| --- | --- | --- |
| Offline sim (`dotnet test`) | **932 passed, 1 skipped** | 929 passed, 1 skipped |
| Unity EditMode | **1028 / 1028** | 1025 / 1025 |
| Unity PlayMode | **97 total — 89 passed, 3 failed, 5 skipped** | 97 total — 92 passed, 0 failed, 5 skipped |

The three PlayMode failures are `DroppedCargoPlayTests.DieWalkBackAndPressE_TheCargoReturnsAndThePileIsGone`,
`OpeningUiPlayTests.U6_InteractingWithTheCoreOpensOneDrawerWithTheCoreCardFirst` and
`RepairPlayTests.OneEscapeCancelsTheRepairAndLeavesTheDrawerOpen_TheNextOneClosesIt`. What was actually measured, one
at a time:

* **U6 passes on its own.** Order-dependent, as before.
* **The Escape/repair test was not re-run on its own.** That run was abandoned part-way and is not reported here.
* **The drop-cargo test fails on its own too** — "E did not collect the pile (or it was not emptied)" — so it was run
  again with every REL-62 change stashed out of the tree, on unmodified `f5f73e79`. **It fails there with the same
  message.** The failure reproduces without this task's code, and nothing in this diff touches the drop, the pointer
  or the E binding.

What that does *not* settle: the same test **passed in the full-suite run** at the REL-14 baseline and fails in the
full-suite run now, and no full PlayMode suite was run on unmodified code to compare. Run alone it fails either way,
so the isolated result cannot tell the two suite runs apart. **This is an open, undiagnosed PlayMode failure, recorded
as its own defect (REL-125), not fixed here and not claimed to be caused here.**

Three new tests:

1. `ALoadedGameHitsBodiesOnItsFirstTick` — ENM-07's stated acceptance. A plasma bolt is in the air half a tile from a
   Spitter when the game is saved; the state is written, read back, wrapped, and one tick is run in the real phase
   order (Weapons → Turrets → Enemies → Director). **The test never points the seam by hand.** The body loses hit
   points.
2. `AStandingWreckRowIsBackOnTheFirstRefreshAfterALoad` — a dry turret and a wrecked wall raise a row; the turret is
   reloaded and the row still stands on the wreck; save, load, fresh source — the row is there. Then repairing it
   clears it, and wrecking the wall again does **not** bring it back: the session rule is unchanged from there on.
3. `AWreckedWallInAGameThatWasNeverLoadedStillSaysNothing` — the other side of the same rule.

Both of the first two were run against the code with the fix reverted and **both failed** (the bolt passed through
the body; the row did not come back), so they bite on the defect they name.

## Limits

* **Two of the three things PER-02 lists are NOT fixed.** The guide lines' "already taught" flags
  (`HudViewModel._taughtHesitation`, `_taughtBlindTurret`) and the open raid tally are still per session, so a guide
  line can teach a second time after a load. They cannot be re-derived from the world — "has this player been told
  this before" is not written anywhere in it — so fixing them means adding to the save, which this issue holds at
  schema 9 by design. That is a separate decision and is not taken here. The issue's stated Outcome, "standing rows
  are re-derived on the first refresh after load. No schema change needed", is met for the rows.
* **The seeding has a stated price.** A place whose wall was wrecked while its row had never been raised now speaks
  after a load, where before the save it was silent. That is the honest half of "re-derived from the world": the
  world records the wreck, not whether the player was ever told about it.
* No screenshots, and nobody has played this build. Both effects need a save taken mid-raid to see, and the issue's
  acceptance criterion is a test.
