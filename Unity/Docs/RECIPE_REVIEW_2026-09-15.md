# Recipe and opening progression review — 2026-09-15

**Implementation follow-up:** the owner approved this opening direction and requested more usable deposit locations. [Implemented scope, New Game behavior and verification](ORE_OPENING_2026-09-15.md). Later progression recommendations remain a review, not a claim of implemented content.

Design review requested by the owner. No gameplay, assets or saves changed. The owner proposes Iron ore → Steel plates, with starter processing at Home and automation in a Foundry. The numerical changes below are recommendations for playtesting, not implemented or accepted balance.

## Recommendation

Make mineable metal deposits yield ore. Home workshop provides a small, power-independent smelting bench; Foundries automate the same conversion. Copper follows the same rule. Keep Coal as generator fuel and Stone as the concrete input. Keep the direct Iron ore → Steel plate abstraction requested by the owner; an additional iron-plate tier would add another mandatory opening step without a demonstrated gameplay need.

Finished plates can still be starting supplies or occasional clearly labelled salvage-crate rewards. They should not be the main mined resource. Existing owned plates retain their identity and value. Replace the large starting metal patches with actual ore sources, update their art and labels, and audit generic steel-yielding rubble so it does not become an unlimited practical bypass. Do not mechanically turn every rubble tile into an ore deposit.

## What the current Unity project does

The current generated recipe assets contain **15 recipe records**. Mining returns the authored resource item directly. The full map has starting steel salvage at (59,352), copper salvage at (59,359) and Coal at (76,359). The authored Iron ore site is at (475,57), and Copper ore at (785,91). Nearby site amounts are 7,680 Steel and 1,200 Copper, versus 12,000 units at each distant ore site. These are source-data amounts, not measurements of a particular player's depleted save.

Thus the existing opening rewards extracting finished materials, while the Foundry introduces extra cost and processing for resources far away. The ore chain exists but is poorly positioned to teach or reward automation.

Starting Backpack: 20 Steel plates + 5 Copper. Hand mining: 0.5 item/s. Excavator nominal rate: 0.5 item/s. Home has Rifle and hand-bullet recipes, but **no ore processing**. Its non-Rifle recipe buttons all invoke the bullet-specific HandCraftCommand; simply adding a smelting recipe asset would still craft bullets. The main and embedded workshop views need recipe-aware commands and queue state.

## Complete recipe audit

S = Steel plates; Cu = current refined Copper item. Times are seconds per batch at nominal full speed, excluding input/output stalls and power shortages. Values were read from the current Unity generated recipe assets, not assumed from historical design tables.

| Current recipe | Current inputs → outputs; time; station | Recommendation |
|---|---|---|
| Steel plates | 2 Iron ore → 1 S; 3 s; Foundry | Make the central opening chain. Trial 1 ore → 1 plate in 2 s automatically, plus Home version in 4 s. |
| Refined copper | 2 Copper ore → 1 Cu; 3 s; Foundry | Apply the same 1:1 yield and 2 s / 4 s automatic/Home times. Rename the output **Copper plate** in player-facing text, preserving its internal id. |
| Hand bullet batch | 2 S + 1 Cu → 10 Bullets; 20 s; Home | Keep ingredients/yield. Trial 12 s per batch so the first 50 rounds take 60 s of crafting rather than 100 s. |
| Bullet batch | 2 S + 1 Cu → 10 Bullets; 6 s; Assembler | Keep. Teach this immediately after the first small defence encounter. |
| Bullet batch Mk2 | Same ingredients/yield; 3 s; Assembler Mk2 record | The actual Mk2 uses the Assembler station and a ×2 speed multiplier on the ordinary recipe. Keep one displayed bullet recipe and derive its machine-specific duration; avoid applying both speed mechanisms. Preserve old keys if compatibility needs them. |
| Rifle | 10 S + 4 Cu → one owned Rifle; 6 s; Home | Keep. Introduce before the first encounter and budget additional carried ammo separately from the turret's 50 rounds. |
| Wire | 1 Cu → 2 Wire; 1 s; Assembler | Keep. Describe as Copper wire. Introduce when a real downstream consumer is available. |
| Frame | 2 S → 1 Frame; 2 s; Assembler | Keep its existing workbench/module uses. Do not add it to starter machine costs in this pass. |
| Board | 1 S + 3 Wire → 1 Board; 4 s; Assembler | Keep its existing workbench/module uses. Do not force electronics into the first defence setup. |
| Concrete | 2 Stone → 1 Concrete; 2 s; Mixer | Keep the simple recipe; present with the Mixer and its construction consumers. No extra water system needed for this review. |
| Refined fuel | 1 Crude → 4 Fuel; 3 s; Refinery | Keep provisionally. Coal and Fuel both carry 4 MJ per item, so this is an alternative supply chain, not more energy per fuel item. Explain that distinction. |
| Polymer | 2 Crude → 1 Polymer; 3 s; Refinery | Keep as the branch toward Mk2 machines and Fast belts. Show those consumers before asking the player to stockpile it. |
| Shell | 2 S + 1 Coal → 1 Shell; 3 s; Assembler | Keep provisionally for the cannon stage. The current SetRecipe handler lacks the intended Arsenal gate; enforce availability with progression and show the actual weapon consumer. |
| Overclock Module | 2 Artifacts + 2 Frames + 1 Board + 4 Wire → 1 Module; 20 s; Alien workbench | Keep. This already gives Frames and Boards real purpose; do not make Artifacts an ordinary ammunition cost. |
| Alien workbench decode | No recipe inputs → 2 Artifacts; 15 s; Alien workbench record | Treat as a special progression action, not ordinary free production. ProductionRules excludes input-free recipes. Verify its actual unlock/reward action when that stage is implemented; do not expose the record as an unlimited free-artifact recipe. |

The Home smelting ratios/times, faster hand bullets and cheaper bootstrap costs below are a coherent proposed balance set. They should be evaluated together, not treated as independent approved changes.

## Proposed opening

1. Show Iron ore, Copper ore and Coal within a short, safe walk of Home. Mine a small amount and smelt the first plates at Home. The helper should say **Mine Iron ore → Smelt Steel plates at Home**, with a location and the full shortage calculation.
2. Build/fuel a Generator and an Excavator. Feed its ore to storage so mining becomes automatic before the player manually processes the entire bootstrap bill.
3. Build and connect a Foundry: **Excavator → Foundry → Supply chest**. A plate visibly arriving in storage is the first production milestone. One Foundry selects one recipe; manual Copper processing is a valid temporary bridge.
4. Craft/equip the Rifle, place/power a turret, and make its first 50 Bullets plus a small separate Rifle reserve. Keep the small tutorial encounter tied to a prepared defence. Review the independent ordinary-raid timer against the longer opening; tutorial readiness alone does not protect a slower player from that director.
5. Expand power and build the Assembler; feed plates into it and belt Bullets into the turret. Copper can be loaded manually initially, then gain its own Excavator and Foundry.
6. Expand to three defended approaches; introduce components, oil and specialist ammunition when their actual consumers become useful.

Home smelting must work without grid power and remain available for recovery when the Home core is disabled. Describe it as the workshop's manual starter furnace; no new fuel inventory or additional building is needed. Keep the existing near-Home/manual-work restriction for this first pass, with short batches, queue controls and cancellation that refunds all reserved ingredients. Long-term throughput comes from machines freeing the player to build and explore.

## Bootstrap costs and pacing

A literal switch to ore at today's 2:1 yield would significantly increase early gathering. Propose these starter costs while leaving other machine costs alone:

| Machine | Current | Proposed starting value |
|---|---|---|
| Generator | 30 S + 10 Cu | 20 S + 5 Cu |
| Foundry | 30 S + 10 Cu | 20 S + 5 Cu |
| Supply chest | 10 S | 5 S |
| Excavator | 10 S | Keep |
| Belt / Pole | 1 S / 1 S + 1 Cu | Keep |
| Assembler | 40 S + 20 Cu | Keep initially; evaluate after the new first-production milestone |

Retain the existing 20 S + 5 Cu starting stake as recovered supplies. For a budgeting example, one Generator + Excavator + Foundry + chest + six Belts + two Poles costs 63 S + 12 Cu under the proposal. After the starting stake, that is 43 S + 7 Cu, or 50 ore at 1:1. Fully manual gathering at 0.5/s takes 100 s and smelting at 4 s takes 200 s: **five minutes of serial work before walking, Coal, placement or mistakes**. Six belts/two poles are an illustrative compact layout, not a guarantee for every placement. Building the Excavator first overlaps gathering with workshop production and reduces manual work. This is arithmetic, not a measured tutorial completion time.

This is why adding ore conversion without changing the bootstrap bill would feel like extra grinding. Playtest the sequence with a fresh save and tune these values toward an early visible automation payoff.

## Throughput and power

At current settings, an Excavator nominally supplies 30 ore/min. With 2 ore → 1 plate, the steel line is capped at 15 plates/min before transport/power stalls. That supports 75 Bullets/min, below the Assembler's 100 Bullets/min. The current Foundry itself can make 20 plates/min; its single Excavator is the bottleneck.

With the proposed 1:1, 2 s Foundry recipe, one Excavator can nominally feed one Foundry at 30 plates/min. One Mk1 bullet Assembler needs 20 Steel + 10 Copper per minute for 100 Bullets/min, leaving useful headroom for construction. Actual belt/miner transfer timing and circuit throttle still need a bounded live throughput check. Mk2 at 200 Bullets/min then needs more Steel production, giving expansion a practical purpose.

Keep finite electricity and the current 300 kW Generator initially:

- One Excavator (60) + Foundry (80) + Home core (100) + turret (20) = **260 kW**, before lights and other loads.
- Add one Assembler (100): **360 kW**, exceeding a single Generator.
- Two Excavators + two Foundries + one Assembler + Home + one turret = **500 kW**, before lights and extras; two Generators provide 600 kW.

Teach the second Generator before the full two-metal ammunition line, with an honest demand preview. Do not silently allow the new Foundry step to produce a brownout. Coal only fuels generation in this proposal; smelting does not acquire a second separate Coal requirement. Conveyors remain independent of electricity, as the owner requested.

## Implementation considerations for a follow-up

- Generalize Home crafting to recipe keys, quantities, per-recipe progress, real input reservations, completed outputs and overflow/refund accounting. Update both workshop views; their current bullet-specific paths must not interpret smelting as ammunition.
- Keep recipe displays, command validation and queue timings on the same data. Today hand bullets read Engineer tuning while the card reads a Recipe; eliminate that opportunity for disagreement.
- Provide one processing equation, quantity/batch controls, input availability, output preview, queue state and **Craft at Home / Automate in Foundry** guidance. Show locked later recipes with a reason or keep them out of the starter list.
- Update resource geometry as well as site metadata, sprites, hover text and objective source lookup. Merely renaming a site does not change the tile's actual yield.
- Update real Unity content through the existing data/Editor workflow. Preserve the Phaser reference and avoid changes that the next export silently overwrites.
- Preserve old Steel/Copper inventories, machines and progress. Version any resource-map changes and in-flight craft state; already-mined tiles must stay mined. Do not reinterpret existing plates as ore or replay tutorial rewards.
- Verify a fresh start with no grants; a powered automatic plate line; both-metal ammunition production under actual power load; cancellation/full Backpack/save-load conservation; recovery with a disabled Home; and a migrated existing save. Measure actual pacing rather than treating the arithmetic above as a playtest.

## Evidence and comparison

Local sources inspected:

- [All exported recipe definitions](../Relight/Assets/Relight/Sim/Data/Generated/CatalogueData.g.cs), cross-checked against all 15 [current recipe assets](../Relight/Assets/Relight/Data/Generated/Recipes).
- [Current engineer and power tuning](../Relight/Assets/Relight/Data/Generated/Tuning), including the starting stake.
- [Full-city resource sites](../Relight/Assets/Relight/World/Generated/Full/FullSites.asset), [mining yield selection](../Relight/Assets/Relight/Sim/Actor/Mining/Mining.cs) and [imported patch mapping](../Relight/Assets/Relight/Sim/World/ImportedGeometry.cs).
- [Home crafting](../Relight/Assets/Relight/Sim/Actor/HandCraft.cs), [workshop command wiring](../Relight/Assets/Relight/UI/Workshop/WorkshopPanelController.cs), [production recipe selection](../Relight/Assets/Relight/Sim/Production/Machines/ProductionRules.cs), [recipe gate handler](../Relight/Assets/Relight/Sim/Production/Machines/SetRecipeCommand.cs) and [opening objectives](../Relight/Assets/Relight/Sim/Campaign/Opening/OpeningQueries.cs).

For comparison, Factorio separates ore extraction from plate smelting, and distinguishes iron plates from its later steel tier ([iron plates](https://wiki.factorio.com/Iron_plate), [copper plates](https://wiki.factorio.com/Copper_plate)). The useful principle here is a readable processing chain with a viable bootstrap route. Relight can retain its simpler direct ore-to-steel conversion; the additional tier is not necessary to apply that principle.

Verification: read-only source/asset audit and explicit arithmetic. No gameplay implementation, fresh-start playtest, throughput run or balance acceptance is claimed. All local report links resolve and the audit covers all 15 recipe records. The repository docsync:check and freshness:check commands were attempted; both could not start because the local tsx executable is missing.
