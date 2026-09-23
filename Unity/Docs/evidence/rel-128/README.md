# REL-128 — a pole carries half again as far

PLAY-03, the third of the owner's ten notes from the 2026-09-22 play of the batch-3 build:

> Power pole connection range needs to be increased by about 50%

Built 2026-09-23. Code and automated checks only. **Nobody has played this build.**

## The numbers, and whose they are

**The owner named one figure — "about 50%" — and one machine: the power pole.** Everything else below is the
implementer's reading under U-D-28, recorded as **U-P-30**.

| Reach | Was | Now | |
| --- | --- | --- | --- |
| Pole | 8 tiles | **12** | the owner's +50% |
| Big pole | 12 | **18** | +50% |
| Substation (placed, and the authored lots) | 8 | **12** | **the implementer's extension — see below** |

### The substation is not tidiness

The note says "power pole". Raising the substation with it was stated before building, and the reason is concrete
rather than aesthetic:

* the authored city's substation **lots** are reach nodes in their own right — they have no machine row at all and
  read `Power.SubstationReachTiles` through `PowerGrid.SiteReach`;
* the opening's own advice describes **pole-to-pole** linking while quoting a reach in words.

That sentence quoted the *substation site's* reach, and read correctly only because the two numbers happened to
match at 8. Raise the pole alone and the tutorial would have said eight tiles while poles linked at twelve — the
game telling the player something untrue. So the sentence now quotes the Pole's own reach
(`PowerGrid.ReachOf(d, "pole")`, a new overload for callers with a kind but no placed machine), which is what it
was always describing. That also makes dropping the substation back to 8 a one-line change that breaks nothing.

## Where the change lives — and why not in the tuning asset

The issue's own text proposed "Data only — `Tuning - Power` and the generated asset". **That premise is wrong**, for
exactly the reason REL-126 hit two days earlier:

* `Sim/Data/Generated/CatalogueData.g.cs` is **generated** from the TypeScript reference in `packages/`, which this
  project may not edit. It is what `ReferenceData.Create()` — the data every offline sim test runs on — reads.
* The `.asset` is what `GameDataRegistry.Build()` — the data the **game** runs on — reads.

Editing only the asset would have left the tests measuring 8 and the game playing 12.

So the change is a new overlay layer, **`Sim/Data/GridBalance.cs`**, applied in *both* chains. It is its own layer
rather than a line in `DarkWorld` for two reasons: reach is not light, and `DarkWorld` is in the `BuildLegacy()`
chain, where reach must not be.

```
ReferenceData.Create()        = DarkWorld( CombatBalance( OpeningBalance( GridBalance( CatalogueData.Build() ))))
GameDataRegistry.Build()      = DarkWorld( CombatBalance( OpeningBalance( GridBalance( BuildOriginal()      ))))
GameDataRegistry.BuildLegacy()= DarkWorld(                                             BuildOriginal()       )
```

**`BuildLegacy()` deliberately does not apply it.** That path means "the original balance, but still no sun"; reach
*is* balance, and a save laid out under eight-tile poles keeps the grid it was built for. This is the first layer to
sit outside that path, and the test below pins the split in both directions.

### Two details that are not incidental

**It writes absolute reaches, not a multiplier.** A `× 1.5` applied in place would compound to 2.25 if the layer
ever ran twice. Setting the value means applying it twice is a no-op — asserted down to the layer returning the
*same instance*.

**It sets the machine rows and the `PowerTuning` record together**, for the same reason REL-126 did: the lots read
the record, the placed machines read their rows, and the advice text quotes the record in words.

A machine kind `GridBalance` has no number for keeps its own reach, so a distribution node added later is not
silently rescaled by a layer written before it existed. A row carrying no reach at all is not a distribution node
whatever its key says, and is left alone.

## Checks

| Suite | Result | Before |
| --- | --- | --- |
| Offline sim (`dotnet test`) | **940 passed, 1 skipped** | 938 passed, 1 skipped |
| Unity EditMode | **1038 / 1038** (172 s) | 1035 / 1035 |

Three new tests, and EditMode runs the sim tests too, so both totals move by the same three.

PlayMode was not run: nothing here touches a scene, a controller or `HudController.Paint`. Its three known failures
(`DroppedCargoPlayTests`, `OpeningUiPlayTests` U6, `EscapeCancelsRepairPlayTests`) are untouched and still open.

### Seven fixtures re-spaced, and why each was a real consequence

A longer reach changes which nodes link, so seven fixtures whose geometry was built for eight-tile poles stopped
saying what they claim. Each was **adapted to the intended change** — every one keeps its claim and its assertions
word for word, and moves only coordinates. None was rewritten to pass.

| Test | What broke | Fix |
| --- | --- | --- |
| `AGeneratorAtTheEdgeOfAPolesReachConnects…` | the boundary pair 18/19 is the *old* edge | 22 / 23 — the rule under test is the boundary, not the number |
| `AWreckedBigPoleSeversTheCircuitBehindIt…` | at 18 tiles the yard's own big pole covered the far yard, so cutting the relay proved nothing | far foundry 30 → **36** |
| `APoleWithinReachOfASubstationSiteLinks…` | at 12 the lot reached the generator directly, so "out of reach" had nothing left to say | west pair 26 → **25** |
| `ASubstationSiteNeverOwnsAConsumingMachine` | the lot now linked to the pole the test needs it *not* to reach | pole `SubX+12` → **`SubX+15`** |
| `ABrownoutThatShrinksTheLampLosesTheTarget…` | the lamp's pole at (32,40) is now in reach of the turret's at (32,30): one circuit, and the brownout would have taken the turret down with it | lamp fed from the south (30,44) + (24,48); 13.58 tiles clear of the turret's pole, and still the lamp's nearest node (3.5 against 9.62) |
| `LightReplayTests.Build()` | same merge — the fixture needs a circuit that can run out of coal while the turret's keeps running | line moved south to (30,45) / (36,45) / (38,45); every node ≥ 13.5 from the turret's |
| `LightReliefTests.Connect()` | at 12 the pole at (48,30) reached the lot's west edge itself, so cutting the last pole cut nothing | two longer hops, (44,31) then (56,30); the returned pole is again the only way in |

Every replacement was chosen with margin: the tightest new clearance is 12.5 against a reach of 12, and most are
≥ 13.5. Three neighbouring tests (`ASubstationSiteBridgesTwoPoleNetworks`, `AnUnreachedSubstationSiteHasNoCircuit`,
`AMachineWithNoPoleInReachJoinsNoCircuit`) were checked by hand and still pass **and** still mean what they say.

### Three tests added

* `PowerNetworkTests.ThePortsPolesCarryHalfAgainAsFarAsTheReferences` — the three ratios against
  `CatalogueData.Build()`'s own rows (so the test states the *relationship*, not a second copy of the numbers), the
  machine/tuning agreement for all three, `PowerGrid.SiteReach`, and idempotence.
* `PowerNetworkTests.TheLayerLeavesAReachItHasNoNumberForAlone` — every other machine's reach is byte-identical to
  the catalogue's.
* `GameDataRegistryTests.ANewGamesPolesCarryHalfAgainAsFarAndAnOldSavesDoNot` — the same facts through the
  **asset-backed** paths: `BuildOriginal()` still carries 8/12/8, `Build()` gives 12/18/12, `BuildLegacy()` stays at
  8, and — the real point — `ReferenceData.Create()` and `registry.Build()` answer the same number. That last one
  fails the day someone "fixes" this by editing the asset.

## Stated limits

* **Nobody has played it.** Whether "about 50%" means this is a question only the owner can answer.
* **The city was not re-surveyed.** The authored city's substation lots and its factory yards were laid out against
  eight-tile poles; a longer reach can only ever *add* links, never remove one, so nothing that was connected can
  have come apart — but whether any yard now joins a circuit its designer meant to keep separate is unmeasured.
  `RealCityRingTests` and the city validators pass unchanged.
* **Legacy saves keep the short reach**, deliberately. A player loading a pre-layout save sees the grid they built.
* **A longer reach is also a cheaper one.** A Pole still costs what it cost, so covering the same ground now takes
  roughly half as many, and the opening's pole budget was not re-costed.

No save-schema change. No new decision — U-D-71 already records the owner's note and flags the substation as the
implementer's; U-P-30 records the numbers.

## Files

* `Unity/Relight/Assets/Relight/Sim/Data/GridBalance.cs` — **new**, the layer.
* `Unity/Relight/Assets/Relight/Sim/Data/ReferenceData.cs` — the layer in the sim chain.
* `Unity/Relight/Assets/Relight/Data/GameDataRegistry.cs` — the layer in the game chain, and why not in the legacy one.
* `Unity/Relight/Assets/Relight/Sim/Power/PowerNetwork.cs` — `ReachOf(GameData, string kind)`.
* `Unity/Relight/Assets/Relight/Sim/Campaign/Opening/OpeningQueries.cs` — the advice quotes the Pole's reach.
* `Unity/Relight/Assets/Relight/Tests/Sim/Power/PowerNetworkTests.cs` — two tests added, four fixtures re-spaced.
* `Unity/Relight/Assets/Relight/Tests/Sim/Combat/Turrets/TurretDarkSightTests.cs` — one fixture re-spaced.
* `Unity/Relight/Assets/Relight/Tests/Sim/Campaign/Light/LightReplayTests.cs` — one fixture re-spaced.
* `Unity/Relight/Assets/Relight/Tests/Sim/Campaign/Light/LightReliefTests.cs` — one fixture re-spaced.
* `Unity/Relight/Assets/Relight/Tests/Editor/GameDataRegistryTests.cs` — one test added.
* `Unity/Docs/DECISIONS.md` — U-P-30.
* **Unchanged, deliberately:** `Data/Tuning/PowerTuningAsset.cs`, `Data/Generated/Tuning/Tuning - Power.asset`,
  `Sim/Data/Generated/CatalogueData.g.cs`.
