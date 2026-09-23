# REL-126 — a light reaches a quarter further

PLAY-01, the first of the owner's ten notes from the 2026-09-22 play of the batch-3 build:

> I think the lighting radius should be a little bigger.

Asked which light they meant, the owner answered **both** — the placed lights *and* the player's torch.

Built 2026-09-23. Code and automated checks only. **Nobody has played this build.**

## The numbers, and whose they are

**The owner named no figure.** "A little bigger" is what they said; everything below is the implementer's reading
under U-D-28, recorded as **U-P-29**.

| Light | Was | Now | |
| --- | --- | --- | --- |
| Lamp | 4 tiles | **5** | reference × 1.25 |
| Arc lamp | 6 | **7.5** | reference × 1.25 |
| Floodlight cone | 12 | **15** | reference × 1.25 — **longer, not wider**; the half-angle is untouched |
| Kerb streetlight (authored) | 7 | **8.75** | reference × 1.25 |
| Player's torch | 12 | **14** | **not** ×1.25 |

The torch is the one deliberate exception. At ×1.25 it would reach 15 tiles and out-reach an Arc lamp, and the
whole point of building a lamp is that it beats carrying your own light around. 14 keeps the torch the better
*immediate* answer and the lamp the better *permanent* one.

## Where the change lives — and why not in the tuning asset

The obvious place is `Data/Generated/Tuning/Tuning - Power.asset` and `PowerTuningAsset.cs`. That is the wrong
place, for a reason worth writing down:

* `Sim/Data/Generated/CatalogueData.g.cs` is **generated** from the TypeScript reference in `packages/`, which this
  project may not edit. It is what `ReferenceData.Create()` — the data every offline sim test runs on — reads.
* The `.asset` is what `GameDataRegistry.Build()` — the data the **game** runs on — reads.

Editing only the asset would have left the tests seeing 4 and the game seeing 5. The two data paths must stay
identical; that is the standing rule, and `Tests/Editor/GameDataRegistryTests` exists to enforce it.

So the change goes where the port's other light values already live: **`Sim/Data/DarkWorld.cs`**, the layer applied
last in *both* chains (`ReferenceData.Create()` and `GameDataRegistry.Build()`/`BuildLegacy()`). It already owns the
port's "there is no sun" (U-D-58) and each turret's dark sight (U-D-59); how far a lamp throws in a world that is
never daylit belongs beside them. The tuning asset keeps the reference's numbers, exactly as it keeps the
reference's sun.

### Two details that are not incidental

**It writes absolute reaches, not a multiplier.** `DarkWorld.Apply` is a once-per-build layer, but a `× 1.25`
applied in place would compound to 1.5625 if it ever ran twice, and nothing in the type system stops that. Setting
the value means applying the layer twice is a no-op — which `ThePortsLightsReachAQuarterFurtherThanTheReferences`
asserts, down to the layer returning the *same instance*.

**It sets the machine rows and the `PowerTuning` record together.** These are two copies of the same fact and they
had no guard keeping them in step:

* the light mask reads `MachineSpec.LightRadiusTiles` / `ConeRangeTiles` (`LightSources.Collect`);
* the opening advice quotes `Power.LampRadiusTiles` in words — *"A Lamp lights N tiles around it"*
  (`OpeningQueries.cs:348`);
* the authored kerb streetlights have **no machine row at all** and read `Power.StreetLightRadiusTiles`
  (`StreetLights.RadiusTiles`).

Had only the machine rows moved, the game would have lit five tiles while the tutorial said four. Both tests now
assert the two agree, so they cannot drift apart again silently.

A machine kind `DarkWorld` has no value for is left exactly as it arrived, so a light added later is not silently
rescaled by a layer that has never heard of it.

## The torch

`Presentation/Light/MouseFlashlightPresenter.ReachTiles` 12 → 14. That is the whole change: the torch is
presentation-only, computes a direction and hands it to `LightingPresenter.SetBeam`, submits no command and is
invisible to `litAt`. Nothing in the sim knows it moved. (The torch being blocked by walls is a different note —
PLAY-06 / REL-131 — and is not in this commit.)

## Checks

| Suite | Result | Before |
| --- | --- | --- |
| Offline sim (`dotnet test`) | **938 passed, 1 skipped** | 936 passed, 1 skipped |
| Unity EditMode | **1035 / 1035** | 1032 / 1032 |

Three new tests, and EditMode runs the sim tests too, so both totals move by the same three.

PlayMode was not run: nothing here touches a scene, a controller or `HudController.Paint`. Its three known failures
(`DroppedCargoPlayTests`, `OpeningUiPlayTests` U6, `EscapeCancelsRepairPlayTests`) are untouched and still open.

### Three tests changed, and why each was a real consequence

Two existing tests had hard-coded the reference's radii. Both were **adapted to the intended change**, not
rewritten to pass:

1. `LightTests.AStreetLightLightsItsRadius…` read `LightRules.StreetLightRadiusTiles` (the constant 7) and asserted
   the lit edge against it. It now reads `StreetLights.RadiusTiles(ctx.Data)` — the value the game actually uses.
   The constant is the fallback for data carrying no `Power` record at all, and asserting against it was testing
   the fallback while exercising the real path.
2. `LightPreviewTests.TheTurretTintIsTheLitMaskInsideTheRing` proves the tint is lit-*and*-in-range by naming a tile
   inside the ring that is dark. Its tile (45,41) is exactly 5.0 from the lamp — it became lit, so the assertion
   lost its subject. Moved to (43,41), 6.71 out, which is dark under the new radius. Same claim, valid tile.

### Four tests added

* `LightTests.ThePortsLightsReachAQuarterFurtherThanTheReferences` — the ratios against `CatalogueData.Build()`'s
  own rows (so the test states the *relationship*, not a second copy of the numbers), the machine/tuning agreement,
  the untouched cone angle, and idempotence.
* `LightTests.ALampNowLightsTheTileTheReferenceLeftDark` — the reach on the ground rather than in a record: the
  reference's edge tile is still lit, the tile past it is now lit, and the tile past *that* is dark. A radius that
  grew must still have an edge.
* `GameDataRegistryTests.EveryDataPathTheGameLoadsGivesAPlacedLightItsQuarterMoreReach` — the same facts through
  the **asset-backed** paths, `Build()` and `BuildLegacy()`, plus the assertion that `BuildOriginal()` still carries
  the reference's 4. That last one is the guard: it fails the day someone "fixes" this by editing the asset.

## Stated limits

* **Nobody has played it.** Whether a quarter is what the owner meant by "a little" is a question only they can
  answer.
* **The performance cost is unmeasured.** +25% radius is about **+56% lit area** per light. The R2 lighting
  prototype (`Prototypes/R2-Lighting/LightingRig.cs`) measured 200 lights at the reference's 4 / 6 / 12, and it is
  deliberately **not** retuned here — it exists to reproduce a recorded measurement, and changing it would
  invalidate the evidence it was recorded against. So the port now draws larger lights than any measurement on
  file covers. The mask is re-stamped only when `LightSources.Fold` moves, which bounds how often the cost is
  paid, but not how much it is.
* **Legacy saves get the bigger lights too**, because `BuildLegacy()` also applies `DarkWorld`. That is the same
  rule the always-dark change follows and is deliberate, but it is a change to how an old save looks.

No save-schema change. No new decision — U-D-71 already records the owner's four answers; U-P-29 records the
numbers as the implementer's.

## Files

* `Unity/Relight/Assets/Relight/Sim/Data/DarkWorld.cs` — the four reaches, `LightReach`, the machine and
  `PowerTuning` passes in `Apply`.
* `Unity/Relight/Assets/Relight/Presentation/Light/MouseFlashlightPresenter.cs` — `ReachTiles` 12 → 14.
* `Unity/Relight/Assets/Relight/Tests/Sim/Campaign/Light/LightTests.cs` — two tests added, one adapted.
* `Unity/Relight/Assets/Relight/Tests/Sim/Campaign/Light/LightPreviewTests.cs` — one tile moved.
* `Unity/Relight/Assets/Relight/Tests/Editor/GameDataRegistryTests.cs` — one test added.
* `Unity/Docs/DECISIONS.md` — U-P-29.
* **Unchanged, deliberately:** `Data/Tuning/PowerTuningAsset.cs`, `Data/Generated/Tuning/Tuning - Power.asset`,
  `Sim/Data/Generated/CatalogueData.g.cs`, `Prototypes/R2-Lighting/LightingRig.cs`.
