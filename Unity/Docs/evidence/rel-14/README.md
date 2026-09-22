# REL-14 — small gaps in the blind and defence signals

INT-10. Three separate holes in the two signals that tell a player their defence is losing: the eye-with-a-slash
badge on a turret, and the per-place defence row in the HUD.

Built 2026-09-23. Code and automated checks only. **Nobody has played this build.**

## The premise, checked first

All three parts held. Each was read in the source before anything was changed.

| The claim | What the code said |
| --- | --- |
| `"Blind"` needs `Target == 0` | True. `TurretQueries.Blind` returned early on `u.Target != 0` (`TurretQueries.cs:126`), and the doc comment listed "it has no target" as a deliberate condition. |
| A place whose turrets are all wrecked before any ran dry gets no defence row | True, **and documented as intentional**: `var raise = row.Dry > 0 \|\| (raid && row.Low > 0)`, with the class doc stating "Wrecks alone never raise one." |
| `MachineActivityVisual` runs the full blind query per turret per frame | True (`MachineActivityVisual.cs:105`) — and `TurretPhase.cs:72-75` was already computing the identical answer **once per tick** for every turret, and storing it in `TurretUnit.Blind`, to decide whether to raise `TurretBlindEvent`. The presenter was paying a second time for an answer the sim had in hand. |

One nuance worth stating, because it changes how big the third problem is: `Blind` returns almost immediately for a
turret that has not been hit in the last 3 seconds, so a quiet base never paid the full sweep. The sweep ran for a
turret **under fire** — which is to say during a raid, when the frame budget is already tightest.

## What changed

**1. Having a target is no longer a reason to stay quiet** (`TurretQueries.Blind`). The `u.Target != 0` early-out is
gone. The case the audit named — a gun firing at the skitter in front of it while a Spitter shells it from the unlit
street — now reads as blind, which is what the guide line it raises already says to do about it:
*"This turret can't see into the dark. Light the ground it guards."*

The sweep cannot contradict the gun's own target: `TurretRules.Reach` only reaches an unlit body inside dark sight,
and every body the sweep considers is unlit and **beyond** it. So the thing that makes the turret blind is never the
thing it is shooting at.

**2. A wrecked turret raises its place** (`DefenceAlertSource`, recorded as **U-D-70**). This narrows one clause of
U-D-61. The worst case was the quiet one: every gun at a site knocked down in a single raid, none of them having run
out of ammunition first, so nothing was ever dry and no row was ever raised. `DefenceAlertRow.TurretWrecks` counts
the turrets among the wrecks, and `raise` reads it.

A wrecked wall or belt alone still raises nothing — a broken conveyor is not a defence emergency and must not ring
like one — and the sentence still counts every structure at 0 hit points (REL-53), not only the turrets.

**3. The badge reads the tick's latch, not the query** (`TurretQueries.BlindNow`, read by `MachineActivityVisual`).
Same answer, computed once per tick by the sim that already needed it, so the badge and the guide line can also never
disagree about the same turret. `TurretUnit.Blind` is not visited, so a freshly loaded game shows no eye until the
first tick and a paused game holds the answer it was paused with.

### Cost, measured

Offline .NET 8 build, 12 turrets, 60 bodies on the map, 600 iterations, microseconds per frame for the whole set of
turrets:

| | Query (before) | Latch (after) |
| --- | --- | --- |
| Under fire | **24 µs** | 0 µs (below the measurement's 1 µs resolution) |
| Quiet | 1 µs | 0 µs |

This is the shape of the saving, not a frame-rate claim: it was measured in the offline .NET 8 test build rather
than in the Unity player, and the badge is only drawn for the machines the world presenter draws.

## Checks

| Suite | Result | Before |
| --- | --- | --- |
| Offline sim (`dotnet test`) | **929 passed, 1 skipped** | 925 passed, 1 skipped |
| Unity EditMode | **1025 / 1025** | 1021 / 1021 |
| Unity PlayMode | 97 total — **92 passed, 0 failed**, 5 skipped | unchanged |

Four new tests, two for each named acceptance criterion:

1. `ATurretFiringAtOneAlienIsStillBlindToTheOneShellingItFromTheDark` — the "blind while firing" case. A biter 4
   tiles out (inside dark sight, so a real target the gun keeps shooting) and a Spitter 7 tiles out (unlit, beyond
   dark sight). Asserts the turret **has** a target, **is** firing, and is blind.
2. `TheLatchedAnswerFollowsTheQueryTickByTick` — `BlindNow` is false before the hits, true during, false once they
   stop, so the badge follows the query it replaced.
3. `APlaceWhoseTurretsAreAllWreckedIsReported` — the "all turrets wrecked" case. Two loaded guns, nothing wrong, no
   row; both wrecked, one row saying "Home: 2 structures wrecked"; both repaired, the row clears itself as before.
4. `ANonTurretWreckIsCountedOnceTheTurretHasRaisedThePlace` — the wall alone says nothing, and is counted once the
   turret has spoken.

The old `WrecksAloneNeverRaiseARow` is kept, renamed `AWreckedWallOrBeltAloneNeverRaisesARow`: that half of the
rule still holds and is now the line that separates the two cases.

## Limits

* **U-D-70 is not an owner decision.** It is this port's reading of U-D-61's intent, taken under the audit finding
  and the owner's authorisation of the third fix batch. The turret-only restriction on raising in particular is a
  judgement, not the owner's word.
* No screenshots. The acceptance criterion this issue names is tests, and both the badge and the row would need a
  staged raid to photograph; the four tests assert the states directly instead.
* Nobody has played this build, so there is no human verdict on whether the badge is noticed mid-fight.
* The measurement above is an offline .NET 8 benchmark, written in the scratchpad and deleted afterwards so it
  cannot pollute the suite counts. It is not in the repository.
