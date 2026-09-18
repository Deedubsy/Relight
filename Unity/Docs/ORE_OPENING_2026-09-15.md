# Ore opening and deposit relocation — 2026-09-15

Implemented after the owner's approval of the [recipe review](RECIPE_REVIEW_2026-09-15.md) and request to move deposits away from cramped building positions. TASKS.md owns status; this document records behavior and evidence.

## What to play

**Choose New Game.** New campaigns use opening resource/balance version 1. Existing saves retain version 0, their original resource locations and original recipe/cost profile. This preserves already-placed extraction lines, depleted tiles, existing plates and ingredients already reserved by in-progress Foundry crafts. There is no automatic relocation of an established factory and no conversion of owned plates back into ore.

### Deposits and usable space

The former patches west of Home and Coal beside the entrance are clear ground in new campaigns. The new deposits occupy the eastern factory yard, leaving processing lanes to their south:

| Deposit | City tile origin | Size | Total units |
|---|---|---|---|
| Iron ore | 84,352 | 3 × 4 | 7,680 |
| Copper ore | 96,352 | 3 × 4 | 1,200 |
| Coal | 96,369 | 3 × 3 | 700 |

An example tested Steel line is Excavator (84,356) → Belt (85,359) → Foundry (84,360) → Belt (85,363) → chest (84,364), with outputs facing south. Generator (78,354) and Poles (81,357), (88,360) powered it. The parallel Copper footprints at x=96/97, additional Generator (90,366), and Assembler (84,368) have clear physical placement footprints. These are examples, not reserved construction ghosts or mandatory placements.

Resource deposits now draw above the paving, so the yard does not hide them. Ore uses the existing world presentation; this is not a new world-art pass. [Deposit screenshot](evidence/ore-opening/deposits.png).

Resource sites explicitly map to their real tile items, including distant ore, crude and stone. Metal-bearing rubble yields ore in new campaigns rather than finished plates. Crude remains Pumpjack-only for extraction. New resource patches distribute their authored totals across their tiles; mined amounts remain finite and are saved.

### Recipes and controls

| Recipe | Home workshop | Automated |
|---|---|---|
| 1 Iron ore → 1 Steel plate | 4 s | Foundry: 2 s |
| 1 Copper ore → 1 Copper plate | 4 s | Foundry: 2 s |
| 2 Steel + 1 Copper → 10 Bullets | 12 s | Assembler: 6 s; Mk2 ×2 speed |

Home smelting uses carried ore and requires no electricity. Craft makes one batch; **Queue 5 batches** adds up to five affordable batches. Only one manual recipe queue runs at a time. Cancellation refunds reserved inputs; refunds that do not fit remain saved and recoverable. Recipes and displayed durations share the same data. The workshop marks actions as unsaved changes even without a simulation tick.

Generator and Foundry now each cost 20 Steel + 5 Copper plates; a Supply chest costs 5 Steel. The starting stake remains 20 Steel + 5 Copper. Other machine costs remain unchanged.

The opening teaches five hand-smelted plates, extraction/storage, the Foundry line, power, Rifle/first defence, then automatic ammunition. The expansion objective distinguishes building, fueling and connecting an additional Generator. Belts remain independent of electricity. Minor raids wait until the introductory encounter has ended; the existing 25–30 minute first-major-raid window is unchanged. This is not a completed long-form pacing acceptance.

Later component, oil, specialist ammunition and artifact recipes were reviewed but their unported progression stages were not implemented by this correction.

## Implementation

- [OpeningBalance](../Relight/Assets/Relight/Sim/Data/OpeningBalance.cs) applies the Unity opening profile over the preserved exported baseline; both ReferenceData and the runtime registry use it for the new balance. Original data remains available for old saves. Exported assets and the Phaser reference were not rewritten.
- [OpeningResourceLayout](../Relight/Assets/Relight/World/OpeningResourceLayout.cs) derives a temporary runtime geometry copy and matching site list. The imported original assets remain untouched; WorldBootstrap selects the appropriate profile and geometry on new game or save adoption.
- [HandCraft](../Relight/Assets/Relight/Sim/Actor/HandCraft.cs) carries a selected recipe and conserved refund inventory; the workshop views submit recipe-aware commands.
- Save schema **5** adds the resource/balance version and generic Home crafting fields. Earlier schemas are verified before upgrade; existing bullet queues retain their meaning. The actual adoption path selects original balance before resuming an older factory, avoiding partial refunds at changed recipe costs.

## Actual verification

- Final C# compile completed successfully in Unity 6000.6.0f1.
- **596/596 simulation tests passed**, including new ore crafting/refund/save tests and the existing migration, production, flow, power, inventory, combat and replay checks. [Final results](evidence/ore-opening/sim-tests.json).
- The first suite found a shared schema constant still at 4; it now references SaveSchema.Version. Assertions for old 20-second hand crafting, 2:1 smelting and 10-plate chests were updated to the approved values. Four inherited test assumptions were reconciled: source hints need actual mineable geometry rather than site metadata alone, and swapping a hotbar entry preserves the displaced default tool. No production source-hint fallback or hotbar behavior was weakened to satisfy them.
- [Paid starter-line check](evidence/ore-opening/paid-line.txt): resources were hand-mined, smelted and paid through simulation commands, with no resource grants. The automatic Steel line delivered **28 plates to its chest in 60 simulated seconds**; conservation held. Engineer positions were set between operations to isolate reach/placement, so this is not a native walking or complete player-pacing test.
- [Live workshop pointer check](evidence/ore-opening/workshop-ui.txt): actual UI Toolkit pointer events selected Steel/Copper recipes, queued five Steel batches, produced the correct items and marked unsaved progress. It uses synthetic pointer events, not native OS mouse input. [Workshop screenshot](evidence/ore-opening/workshop.png).
- [Legacy save adoption](evidence/ore-opening/legacy-adoption.txt): the actual AutosaveController adoption path restored the old Steel patch, its 123 remaining test units, and the old 2-ore Foundry recipe.
- Screenshots were visually inspected. The visual pass caught and fixed deposits being hidden beneath yard paving. Runtime save writes were disabled; no player saves were read or written for these scenarios. Play Mode was stopped and runtime-only camera/host changes were discarded.

Native play, subjective pacing and standalone-build acceptance remain open. Report links and scoped git diff --check passed; Git emitted existing nested .gitattributes warnings. docsync:check and freshness:check were attempted but could not start because the local tsx executable is missing.
