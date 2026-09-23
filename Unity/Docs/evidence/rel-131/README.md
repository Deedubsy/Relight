# REL-131 — walls stop the torch

## The owner's note

> "Existing and placed walls should block light from the player torch, but not light from placed objects"

Asked which half to build, the owner chose **"Only stop the torch"** — not the recommendation. So **REL-116 stands**:
placed lamps keep the blocking they already have, and the torch gains it.

## What was already true (the premise, checked first)

| | Blocked by walls before this? |
|---|---|
| Lamp, Arc lamp, Floodlight (the sim's lit mask) | **Yes** — REL-116 / L-02, `LightRules.Stamp(..., ctx, st)` |
| The player's torch (the drawn cone) | **No** — it went through everything |

So the second half of the note ("but not light from placed objects") asked for nothing, and the first half asked
for one change. Nothing about placed lights was touched.

## What was built

| Piece | File | What it does |
|---|---|---|
| `LightRules.Shadow(...)` | `Sim/Campaign/Light/LightRules.cs` | `StampWindow`'s shadow, lifted out so a caller can supply its own shape. Same `Opaque` rule, same segment walk, same both-end-tiles exemption. |
| The beam's shadow | `Presentation/Light/LightingPresenter.cs` | Per frame: mark the tiles the cone reaches, ask `Shadow` which of them the torch can see, gate `Beam()` on the answer. |

### Why the shadow had to be lifted out rather than reused

The torch **cannot be expressed as a `Light`**. `Light.Dir` is one of eight fixed directions; the torch points
wherever the mouse is, a continuous angle. Writing a second shadow rule for it would have let the torch and the
lamp standing beside it disagree about the same wall — so the caller supplies the shape and `LightRules` supplies
only what stands in the way.

**The caller marks what it wants tested.** On entry a `1` means "does the source see this tile?", a `0` means "do
not ask". So a cone pays for the tiles it reaches and not for its whole bounding box, and tiles it never asked
about are left alone instead of coming back as shadow.

## Still picture only

- `LightQueries.LitAt` has never known the torch exists and does not learn it here.
- The lit mask does not move, `Builds` does not tick, `st.Rev` does not change — asserted in
  `AskingItChangesNoSimulationAnswer`.
- No raider hesitates and no turret sees further because of where the player is pointing.

## The two costs that are deliberately avoided

1. **Open ground costs one scan.** `Shadow` returns `false` without walking a single line when the box holds no
   occluder — which is most of the map, most of the time.
2. **Only the cone is line-tested.** A 30 × 30 box is 900 tiles; the 60° cone marks roughly 150 of them.

A tile is marked when the cone touches its **centre or any of its four corners**. The corners matter because the
overlay is supersampled: a texel near a tile's edge can sit inside the cone when the tile's own centre does not,
and an untested tile reads as shadow, which would bite a notch out of the cone's rim.

## The one place the two rules meet

The small glow at one's own feet (`GlowTiles = 1.75`) is **not** occluded — `Beam()` returns it before it consults
the shadow. Standing with your nose to a wall must not put you in the dark.

## Checks

| Suite | Result |
|---|---|
| Offline sim (`dotnet test`) | 957 passed, 1 skipped, 0 failed (was 949 + 8 new) |
| Unity EditMode | 1057 / 1057, 0 failed (was 1049 + 8 new) |
| Unity PlayMode | 107 total: 102 passed, 0 failed, 5 skipped (was 105 / 100 / 0 / 5) |

The five PlayMode skips are the declared environmental ones and are unchanged.

### Both halves were shown to bite

| Mutation | Result |
|---|---|
| `Shadow` stops clearing what the line test rejects | the 8 sim tests went to **4 passed / 4 failed** — and the four that still passed are the four that do not assert occlusion (open ground, no other machine, no sim change, cost). Restored: 8 / 8. |
| The gate removed from `Beam()` | the 2 Play tests went to **0 passed / 2 failed**, with their own messages: *"the cone is still showing on the far side of the wall"* and *"past the origin glow the wall should stop the beam, and it did not"*. Restored: 2 / 2. |

The second mutation is the one that matters: without it, a shadow rule that works perfectly and that nothing
consults would have passed every sim test in the suite.

New tests:

- `Tests/Sim/Campaign/Light/TorchShadowTests.cs` — 8 tests on the shared rule, including
  `ItAgreesWithTheStampALampGetsThroughTheSameWall`, which puts the torch's question and a lamp's question to the
  same wall and requires the same answer tile for tile, and `OneTorchShadowIsCheapEnoughForEveryFrame`.
- `Tests/Play/Scene/TorchShadowPlayTests.cs` — 2 tests in the authored scene, that the cone actually stops and that
  the feet glow survives. They pick a facade with nothing placed within 12 tiles, so the buildings' own glow
  (REL-127) reads zero and the whole of each reading is the beam.

`LightBlockingTests` (REL-116's own tests) pass **unchanged**, which is the acceptance item that placed lights
behave exactly as before.

## Not done here

- **The placed-wall half is proved in the sim suite, not the scene.** The PlayMode tests use an authored building
  so they need no inventory and cannot skip themselves; `st.Walls` is covered by the sim tests on both rules.
- **Not seen in play.** No owner playtest of this change, and no assistant screenshot is offered as one.
