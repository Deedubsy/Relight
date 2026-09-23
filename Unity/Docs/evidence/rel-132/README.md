# REL-132 — a gate in the wall, and the one wall a turret may shoot over

## The owner's note

> "With being able to build walls, we need the ability to build a gate so we can full wall off an area. Turrets
> should be able to shoot over a wall, only if up against the wall. Turrets placed away from wall can't shoot
> over it"

The seventh of the owner's ten notes from their 2026-09-22 play, and the last of the ten to be built. It was kept
apart from the other nine because it is new gameplay of about their combined size: no gate kind existed and
`TurretSight` had no adjacency rule of any sort.

The gate's behaviour is the owner's answer (d) to the four questions, option label verbatim: **"Auto, shuts
behind (Recommended)"**. U-D-72 and U-P-36 record the decision and the numbers.

## What was already true (the premise, checked first)

Two findings, and the second changed the shape of the work.

**There is no gate anywhere.** `grep -rn "\"gate\"\|Gate"` across `Sim/` returned `GateRules` only after it was
written — before that, nothing. No kind, no art, no build-menu entry, no rule.

**The turret half was a real defect, not a missing feature.** `Sightline.Clear` already exempted blocking tiles
under the two ENDS of the ray, and its own comment claimed that covered the owner's first sentence:

> *"a turret placed hard against its wall must still be able to fire over it"*

It did not. The exempt tile is the one the **turret is standing on**, not the wall beside it. So a player who
walled their perimeter properly disarmed every gun on it in the same action, and the code carried a comment
saying the opposite. The owner's first sentence was a bug report; only their second sentence was already true.

## What was built

| Piece | File | What it does |
|---|---|---|
| The gate's rules | `Sim/Combat/GateRules.cs` | `IsGate`, `Open`, `Horizontal`, `Walled`, `OpenRadiusTiles` = 2.0. No state of its own. |
| Its catalogue row | `Sim/Data/CombatBalance.cs` | `Gate()` beside `Breaker()`, appended by `Apply` if absent. |
| The engineer's way through | `Sim/World/Ground.cs` | `GroundState.YieldsToEngineer`; `PassableForEngineer`, `OccupiedForEngineer`. |
| Routing | `Sim/World/PathFinder.cs` | `throughGates` — a trailing parameter defaulting **false**, so every existing caller is unchanged. |
| The walk | `Sim/Actor/Movement/EngineerMovement.cs` | The one caller that passes `throughGates: true`. |
| The flush rule | `Sim/Combat/Sightline.cs` | `Opaque` now includes the gate; `FlushTiles` = 1.5 and `Flush(...)`, skipped inside `Clear`. |
| The build menu | `Sim/UI/Build/BuildCatalogue.cs` | `DefenceKinds` gains `gate`, plus its one-line description. |
| Its shortcut slot | `Sim/UI/Bindings.cs` | `Pair("gate")`, unbound — like the Wall and the Barricade. |
| Its icon colour | `UI/Icons/ItemIcons.cs` | One swatch. |
| The leaf | `Presentation/Building/GateVisual.cs` | Two halves that part and close, pooled strokes, no reference to the sim. |
| Its wiring | `Presentation/MachineView.cs` | One gate per view, between the health bar and the repair mark. |

## The three rules, and why each is drawn where it is

**A gate is solid to everyone and open to the engineer.** It is an ordinary machine: it has hit points, raiders
path into it and attack it, `TurretRules.BlocksRaiders` answers true with no change, and once it is wrecked it is
a hole for everyone exactly as a wrecked wall is. The only thing that knows otherwise is the engineer's own walk.

**The engineer passes a standing gate always, not only while it is drawn open.** `GroundState.SolidMap` is cached
on `SimState.Rev`, and an engineer's step does not bump `Rev` — nor should it, because rebuilding that map is
497,664 tiles on the real 864 × 576 city and the cache exists precisely so that a step is cheap. A gate that
became passable only inside its open radius would need the map rebuilt as the engineer moved. So the rule is the
simple one and **the drawn leaf is a picture of something already true**, which is also why `GateVisual` holds no
reference to the simulation at all: it is given a bool and a delta time.

**A turret may fire over the wall it is built against, and over no other wall.** `Sightline.Clear` now skips
blocking tiles within `FlushTiles` = 1.5 of the **shooter**, Chebyshev between tile centres.

### Why 1.5, exactly

Both guns the game has are 2×2 — `new MachineSpec("turret", "Gun turret", 2, …)` and
`new MachineSpec("cannon", "Cannon", 2, …)` — and `TurretSight.Coverage` uses `cx = x + size/2.0`, so a gun's
centre sits on the junction of its four tiles:

| Distance from a 2×2 gun's centre | What is there |
|---|---|
| 1.5 | the ring of tiles **touching** its footprint — the wall it is built against |
| 2.5 | the next course out — a wall it is standing away from |

1.5 is therefore the smallest number that grants the owner's first sentence, and it is still below the 2.0 that
would let a 1×1 shooter fire over a wall a tile back. Anything larger breaks their second sentence.

### The muzzle, which looked like a hole and is not

At the trigger the ray starts at the **muzzle**, one tile out from the centre along the barrel, so the ring it
measures is shifted toward the target and can excuse a wall the centre would not. That was checked rather than
assumed, and it cannot widen the rule: target selection runs first and runs **entirely from the centre** —
`TurretPhase.Eligible(ctx, st, def, cx, cy, u.Target)` and `EnemyQueries.NearestInSight(ctx, st, cx, cy, def)` —
so a turret the centre refuses never reaches the muzzle test. The muzzle test can only narrow what the centre
already allowed.

### The exemption is for player walls only

`ICityGeometry.Sight` is tested first in `Clear` and is never exempted, so building a turret against an authored
city block does not turn the block into a parapet. `AnAuthoredCityBuildingStopsTheShotEvenAtPointBlank` holds
that line.

## Tests

**`Tests/Sim/Combat/GateTests.cs` — 18 tests.** Four of them build a walled yard with one gate in it and ask the
same question four ways, because "you can enclose an area completely and still walk in and out" is one claim made
of two halves that must not be granted separately:

| Test | What it pins |
|---|---|
| `AnEnclosedYardCanStillBeWalkedIntoAndOutOf` | the engineer's path crosses the gate, both directions |
| `NothingButTheEngineerFindsAWayThroughThatYard` | the same yard is sealed to the default `FindPath` |
| `AYardWithNoGateIsSealedToTheEngineerAsWell` | a gate is the only reason they get through — not a leak in the wall |
| `ABrokenGateLetsTheYardBeWalkedByAnything` | and a wreck is a hole for everyone |

`TheGateStaysOpaqueWhileItIsDrawnOpen` is the one that keeps the picture honest: a player who fitted a gate must
not have cut themselves a firing slit. `TheGateAddsNothingToTheSaveFormat` asserts `SaveSchema.Version` is still
12 — a gate is an ordinary machine and open/shut is derived, so there is nothing to store.

**`Tests/Sim/Combat/Turrets/TurretFlushWallTests.cs` — 7 tests.** The owner's two sentences are tested against
the **same geometry, changed by one tile of wall thickness**, because they are one rule and a fix that granted
the first without keeping the second would be worse than the bug:

| Ring thickness | Coverage | Advice | Shots fired |
|---|---|---|---|
| 1 course (flush) | > 0.99 | none | > 0, ammunition spent |
| 2 courses | < `BlindFraction` | "Blind position…" | 0, `Rounds` still 50 |

Each half is checked three ways — the placement preview, the built turret's card and the round that does or does
not leave the barrel — because GP-W3's point was that those three must never disagree, and REL-15's criterion
says the card must keep telling the truth under the new rule. `TheExemptionReachesTheTouchingRingAndStopsThere`
asserts the boundary directly at 1.5 and 2.5.

### Four existing tests were moved, and one of them was already wrong

`TurretSightTests` had three tests using a **5×5 wall ring with a 1×1 hole**. A Gun turret is 2×2, so its centre
lands at (Tx+1, Ty+1) — inside that ring's only course on two faces — and under the new rule roughly half the
bearings opened up (`OpenFraction` 0.573 where the tests expect < 0.20). They now use `Courtyard`, the
**two-course ring around a 2×2 hole this same file already had** for the tests that place a turret properly. The
claims are unchanged; only the wall that has to be there to make them true moved out by one tile. The walk checks
in `AWreckedWallStopsBlockingSightMovementAndRoutingAllAtOnce` moved from (Tx+1, Ty) — which is the hole under
`Courtyard` — to (Tx+2, Ty), a real wall tile.

The fourth, `AWallBesideATurretDoesNotBlindItAlongTheOtherBearings`, put its wall column at x = Tx+1, which is
**inside the 2×2 turret's own footprint**: the fixture was building a wall through the gun. It now sits at
x = Tx+3, the first column that is a wall the gun is not standing against, which is the case the test is about.

## Checks run

| Check | Result |
|---|---|
| Offline sim suite (`simtests`, every `Sim/` + `Tests/Sim/` file) | **1004 passed, 1 skipped, 0 failed, 1005 total**, 7 s |
| Offline Unity-side compile (`gamecheck`, `Presentation/` + `UI/`) | **Build succeeded, 0 errors**, 7 pre-existing warnings |

The baseline before this change was 980 total, so all 25 new tests ran. The one skip is the documented baseline
skip, unchanged.

**Not run: the Unity EditMode and PlayMode runners.** The owner's standing instruction is *"Don't worry about
running too many tests. These are taking hours"*. Both new test classes are sim tests and are compiled and run in
full by the offline suite above; the Unity-only files this change touches (`GateVisual`, `MachineView`) are
compiled by `gamecheck`. There is no PlayMode test for the gate's leaf, so **nothing has drawn a gate on screen**
— see below.

## Not done here

- **The engineer's rifle is unchanged.** It has its own cast (`Ballistics.Blocked`), the note was about turrets,
  and letting the player shoot over the wall they are hugging would take away the cover a wall gives them.
  Recorded as an open question in U-D-72 (c).
- **Light does not pass an open gate.** A gate is opaque whatever it is drawn as, so a torch in a doorway stops
  at it. This may read wrong in play; it is a deliberate limit, not an oversight.
- **A gate has no art asset.** `PrefabRegistry` falls back to the chest prefab and `GateVisual` draws the leaf
  over it.
- **Not seen in play, and no screenshot is offered as one.** Nothing has drawn a gate on screen — not the owner
  and not the assistant. Whether a one-tile door reads as a gate, whether 2 tiles is the right distance for it to
  swing, and whether a turret on a parapet now feels right, are questions only the owner can answer.
