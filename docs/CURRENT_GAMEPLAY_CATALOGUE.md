# Relight — Current Gameplay Catalogue

**Implementation snapshot: 10 September 2026.** Covers the current `exploration-v2` campaign and authored Riverfront city, including the latest player-experience corrections. This is a readable reference to what is implemented, not a list of planned features or a balance approval. Older modes and saves can have different rules.

## At a glance

- **21 inventory item types**, plus packed machines.
- **10 production recipes**, with faster processing in Assembler Mk2.
- **24 player-buildable machine and structure types.** Fixed tram infrastructure, Home and restored facilities are additional world installations.
- **Progression through exploration and restoration:** recover power cores, commission regional plants, recruit specialists and complete encounters.
- **Real inventories and local power:** materials must be carried or transported; production needs a connected network with enough generating capacity.

## 1. Items and resources

### Basic materials

- **Steel plates** — The main construction material. Found in finite salvage and resource patches, or made by processing Iron ore in a Foundry. Used in machines, defences, components, ammunition and restoration projects.
- **Copper** — Used in electrical equipment, machines, ammunition and Wire. Obtain it from finite salvage/resource patches or refine Copper ore in a Foundry.
- **Stone** — Gathered from rubble/resource patches. Process it in a Mixer to make Concrete.
- **Coal** — Fuel for Generators and an ingredient in Shells. Gather it manually or with an Excavator.
- **Iron ore** — A mined resource that a Foundry turns into Steel plates. It is separate from the directly usable Steel plates found in salvage.
- **Copper ore** — A mined resource that a Foundry turns into usable Copper.

Hand gathering and Excavators serve the solid resource patches. Deposits are finite; placing an Excavator does not create an endless supply.

### Oil products

- **Crude oil** — Extracted by a Pumpjack; it cannot be hand-mined. Transported as an inventory item to a Refinery.
- **Refined fuel** — Made from Crude oil. Generators accept it as an alternative to Coal. Each unit supplies the same **4 MJ** as one Coal.
- **Polymer** — Made from Crude oil. Used to construct **Assembler Mk2** and **Fast belts**.

### Manufactured components

- **Wire** — Made from Copper. Currently used to manufacture Boards.
- **Frame** — Made from Steel plates. **No implemented consumer at present.** Retained for future turret work.
- **Board** — Made from Wire and Steel plates. **No implemented consumer at present.** Retained for future turret work.
- **Concrete** — Made from Stone after recruiting the Concrete crew. Used for Barricades and restoring the Turbine hall.

Wire, Frame and Board are real producible items. Their presence does not mean an advanced turret chain has already been implemented.

### Ammunition

- **Shot magazine** — Contains **10 rounds**. Supplies the player's rifle and Gun turrets. Made at the **Home workshop** or in an Assembler.
- **Shells** — Ammunition for the Cannon. The recipe and Cannon become available through the **Arsenal encounter**.

Rounds inside a weapon are an internal ammunition count, not another backpack item. Magazine transfers use whole magazines; partially spent rounds can remain in the weapon.

### Exploration rewards

- **Power core 1** — Recovered from the **Occupied freight depot**.
- **Power core 2** — Recovered from the **Occupied quarry works**.
- **Power core 3** — Recovered from the **Occupied waterfront warehouse**.

Clear the relay's defenders and recover its core locally. **Any recovered core can commission any uncommissioned prepared regional plant**; the numbers identify the cores, not a required plant pairing. Installed cores survive the plant being disabled.

- **Speed artifact 1** — Found in the **Workshop salvage room**.
- **Speed artifact 2** — Found in the **Quarry equipment cache**.
- **Speed artifact 3** — Found in the **Secured wharf store**.

All three artifacts provide the same **10% production-speed increase**. A supported production machine has one removable artifact slot. Power, material supply and output space can still limit actual output.

### Inventory sizes

- **Backpack:** 40 slots.
- **Most materials and components:** 50 per stack.
- **Shot magazines and Shells:** 20 per stack.
- **Power cores and Speed artifacts:** one per stack.
- **Packed machines:** one per slot; packing an existing machine is different from crafting a material item.
- **Supply chest:** 200 total items, not 200 stacks.
- **Truck:** 200 cargo stacks, separate from the backpack.
- **Tram:** 200 cargo items, increased to 250 by the Rail crew upgrade.
- **Tram platform stock and arrival cargo:** separate inventories, each with a 200-item capacity.

Sources: [item names](../packages/sim/src/itemNames.ts), [item definitions and storage](../packages/sim/src/flow.ts), [backpack and stack sizes](../packages/sim/src/engineer.ts), [item guidance](../packages/sim/src/itemGuide.ts), [truck](../packages/sim/src/truck.ts), [freight](../packages/sim/src/freight.ts).

## 2. Recipes

Times below are **per completed craft at normal speed**, with full power, all inputs and room for the output. Quantities describe real inventory items.

| Product | Ingredients | Output | Base time | Where |
| --- | --- | --- | --- | --- |
| Steel plates | 2 Iron ore | 1 Steel plates | 3 seconds | Foundry |
| Copper | 2 Copper ore | 1 Copper | 3 seconds | Foundry |
| Refined fuel | 1 Crude oil | 4 Refined fuel | 3 seconds | Refinery |
| Polymer | 2 Crude oil | 1 Polymer | 3 seconds | Refinery |
| Wire | 1 Copper | 2 Wire | 1 second | Assembler / Assembler Mk2 |
| Frame | 2 Steel plates | 1 Frame | 2 seconds | Assembler / Assembler Mk2 |
| Board | 3 Wire + 1 Steel plates | 1 Board | 4 seconds | Assembler / Assembler Mk2 |
| Shot magazine | 2 Steel plates + 1 Copper | 1 magazine containing 10 rounds | 6 seconds | Home workshop / Assembler / Assembler Mk2 |
| Shells | 2 Steel plates + 1 Coal | 1 Shell | 3 seconds | Assembler / Assembler Mk2; Arsenal required |
| Concrete | 2 Stone | 1 Concrete | 2 seconds | Mixer; Concrete crew required |

**Processing details:**

- Assembler Mk2 runs the same assembler recipes at **twice the speed**. For example, one Shot magazine takes 3 seconds instead of 6.
- A Speed artifact multiplies the supported machine's processing speed by **1.1**. It does not add a new recipe.
- Home handcrafting uses carried Steel plates and Copper, takes 6 seconds and puts the magazine in the backpack. Stay near Home while crafting; no powered Assembler is required.
- Processing machines buffer ingredients locally, generally up to **four crafts' worth per ingredient**, and have a **five-item finished-output buffer**. A blocked output can stop production.
- The Foundry's internal recipe identifier `iron` produces **Steel plates**. There is no separate finished “Iron” item in the current inventory catalogue.

Sources: [recipe data](../packages/sim/src/recipes.ts), [active recipe mapping, outputs and processing](../packages/sim/src/flow.ts), [Home handcrafting](../packages/sim/src/flow.ts).

## 3. Buildable machines and structures

Costs below are **construction costs**, not the materials needed to run the machine. Dimensions are world tiles. Power values are rated demand; supply and network conditions affect operation. Unless an unlock is stated, the type is available without a recruit or encounter unlock, provided the player has the materials and a valid placement.

### Production

- **Excavator** — Collects solid resources under and immediately around its footprint. Nominal output: **one item every 2 seconds**. Cost: **10 Steel plates**. Size: **3×3**. Power: **60 kW**.
- **Pumpjack** — Extracts Crude oil from an oil deposit. Nominal output: **one item every 2 seconds**. Cost: **20 Steel plates + 10 Copper**. Size: **3×3**. Power: **60 kW**.
- **Foundry** — Processes Iron ore into Steel plates or Copper ore into Copper. Cost: **30 Steel plates + 10 Copper**. Size: **3×3**. Power: **80 kW**.
- **Refinery** — Processes Crude oil into Refined fuel or Polymer. Cost: **40 Steel plates + 20 Copper**. Size: **3×3**. Power: **100 kW**.
- **Assembler** — Makes ammunition and manufactured components using a selected recipe. Cost: **40 Steel plates + 20 Copper**. Size: **3×3**. Power: **100 kW**. Shells still require the Arsenal unlock.
- **Assembler Mk2** — Runs assembler recipes at **2× speed**. Cost: **60 Steel plates + 30 Copper + 4 Polymer**. Size: **3×3**. Power: **150 kW**. No separate recruit unlock; obtaining Polymer is the material dependency.
- **Mixer** — Converts Stone into Concrete. Cost: **20 Steel plates + 10 Copper**. Size: **3×3**. Power: **60 kW**. Unlock: **Concrete crew**.

### Storage and logistics

- **Supply chest** — Stores **200 items** for local production and transfers. Cost: **10 Steel plates**. Size: **2×2**. No power demand.
- **Belt** — Moves items at a nominal **7.5 items/second**. Cost: **1 Steel plates per tile**. Size: **1×1**. No power demand.
- **Fast belt** — Moves items at a nominal **15 items/second**. Cost: **2 Steel plates + 1 Copper + 1 Polymer per tile**. Size: **1×1**. No power demand.
- **Inserter** — An optional powered transfer device, nominally **one item/second**. Cost: **1 Steel plates + 1 Copper**. Size: **1×1**. Power: **10 kW**.
- **Underground belt** — Joins matching, correctly facing input/output endpoints across up to **four hidden tiles**. Cost: **5 Steel plates + 2 Copper per endpoint**; a pair costs **10 Steel plates + 4 Copper**. Each endpoint is **1×1**. No power demand.
- **Priority splitter** — Divides a belt stream, with balanced or left/right priority and item filtering. Cost: **5 Steel plates + 2 Copper**. Size: **2×1**, rotated with orientation. No power demand.

**Conveyors can deliver to and take from compatible machine/storage inventories directly. Inserters are optional.** Direction, adjacency, accepted item types and available space still matter; a connection does not bypass a recipe's input restrictions.

### Power and lighting

- **Generator** — Burns Coal or Refined fuel to supply up to **300 kW**. Has a **50-item combined fuel inventory**; consumption follows actual load. Cost: **30 Steel plates + 10 Copper**. Size: **2×2**.
- **Pole** — Connects the local power network, with an **8-tile reach**. Cost: **1 Steel plates + 1 Copper**. Size: **1×1**. No power demand.
- **Big pole** — Longer power connection, with a **12-tile reach**. Cost: **4 Steel plates + 4 Copper**. Size: **2×2**. No power demand. Unlock: **Electricians**.
- **Substation** — Provides local power distribution and network connections. Cost: **50 Steel plates + 25 Copper**. Size: **3×3**. No power demand. Available without the Electricians unlock.
- **Lamp** — General local lighting with a **4-tile radius**. Cost: **1 Steel plates + 1 Copper**. Size: **1×1**. Power: **5 kW**.
- **Floodlight** — Directional lighting reaching **12 tiles** in a **60-degree cone**. Cost: **10 Steel plates + 5 Copper**. Size: **2×2**. Power: **40 kW**. Unlock: **Electricians**.
- **Arc lamp** — Wider local lighting with a **6-tile radius**. Cost: **4 Steel plates + 4 Copper**. Size: **1×1**. Power: **12 kW**. Unlock: **Lamplighters**.

### Defence

- **Gun turret** — Uses Shot magazines. Range: **9 tiles**; firing rate: **5 rounds/second**; base hopper: **50 rounds**; health: **100 HP**. Cost: **15 Steel plates + 5 Copper**. Size: **2×2**. No electrical demand. The Gunsmith offers a paid hopper upgrade.
- **Cannon** — Uses Shells. Range: **12 tiles**; damage: **50 per shot**; reload: **2 seconds**; capacity: **20 Shells**. Cost: **40 Steel plates + 20 Copper**. Size: **2×2**. No electrical demand. Unlock: **Arsenal**.
- **Wall** — Basic obstruction and protection with **120 HP**. Cost: **2 Steel plates**. Size: **1×1**. No power demand.
- **Barricade** — Stronger obstruction with **240 HP**. Cost: **2 Steel plates + 4 Concrete**. Size: **1×1**. No power demand. Unlock: **Concrete crew**.

Sources: [machine types, costs, footprints, operation and availability](../packages/sim/src/flow.ts), [power and lighting constants](../packages/sim/src/recipes.ts), [combat constants](../packages/sim/src/constants.ts), [specialist unlocks](../packages/sim/src/campaignRecruits.ts).

## 4. Fixed infrastructure and vehicles

- **Home / Founders Court workshop** — The starting base, storage and manual Shot magazine crafting location. It is a fixed installation, not a free placeable building. The regional **repair workshop** is a different facility.
- **Four fixed tram stops** — **Founders Court, Riverside, Ironworks, and Civic / East Wharf.** Players power the existing stops; they do not build the city's tram line or place extra campaign stops. A stop draws **20 kW**. With at least two stops powered, the tram automatically serves the powered stops.
- **Tram** — Carries physical freight between fixed stops. Base capacity: **200 items**, upgradeable to **250**. Platform stock, arrival cargo and onboard cargo are separate inventories.
- **Truck** — Unlocked by restoring the first away-from-Home station, the second-area tram station. Carries **200 stacks**, follows road access and can be driven. With the Foreman, it also delivers materials for queued construction from a chosen local Supply chest and supports explicit equipment-recovery jobs.
- **Regional power plants** — Three prepared factory sites accept recovered cores and supply **600 kW each** once commissioned: **Riverside Works, Ironworks, and Civic Utility**.
- **Turbine hall** — A separate restoration that adds **600 kW** of connected generation. It is not another build-menu Generator.

Installed plant cores and the Turbine do **not** consume a fuel inventory in the current implementation. Their generation has a finite **power capacity**, rather than a finite stock of energy that runs out. Generators do consume fuel.

Sources: [authored city](../packages/sim/src/city/riverfront.ts), [campaign bindings](../packages/sim/src/riverfrontCampaign.ts), [fixed tram](../packages/sim/src/fixedTram.ts), [truck](../packages/sim/src/truck.ts), [construction work](../packages/sim/src/truckWork.ts), [regional plants](../packages/sim/src/progression.ts), [Turbine](../packages/sim/src/campaignTurbine.ts).

## 5. Progression and unlocks

### The opening: establish a working supply line

The current guidance leads the player through this order. It is guidance, not a rule preventing other sensible construction.

1. **Generator:** 30 Steel plates + 10 Copper. Gather Coal to fuel it; at least one unit is needed to start supplying energy.
2. **Excavator:** 10 Steel plates. Place it at a resource patch and supply power.
3. **Supply chest:** 10 Steel plates.
4. **Belt to the chest:** 1 Steel plates for each belt tile; connect the Excavator's output to storage.
5. **Gun turret:** 15 Steel plates + 5 Copper.
6. **Shot magazine:** handcraft at Home using 2 Steel plates + 1 Copper, taking 6 seconds.
7. **Load the turret**, then establish automated magazine production and delivery as resources allow.

This sequence introduces gathering, fuel, power, output routing and ammunition before asking the player to depend on an automated defence line. Pole and belt counts depend on the chosen layout.

### Recover cores and commission regional plants

- Find the occupied freight depot, quarry works and waterfront warehouse; overcome the relay defenders and recover their portable power cores.
- Commission a prepared plant using **30 Steel plates + 15 Copper + one recovered Power core**.
- Each commissioned plant adds **600 kW** and becomes a defended regional site. Switching it off does not erase its attack eligibility.
- **Riverside → Ironworks → Civic** is the authored regional layout/progression direction, but core numbers do not force a matching plant order.
- Damage can disable a site without deleting its installed core, equipment or recruited specialists. Recovery and repair are part of the ongoing campaign.

### Restore services

- **First regional tram station:** **30 Steel plates + 15 Copper**, with local power. Unlocks the physical Truck and registers the restored regional base/service.
- **Later regional station:** **40 Steel plates + 20 Copper**, with local power. Restores the later station's regional service/base.
- **Radio:** **20 Steel plates + 10 Copper**, with local power. Its operating demand is **20 kW**; a powered radio provides major-assault warnings.
- **Radio precision upgrade:** **15 Steel plates + 10 Copper**, at a powered restored Radio. Adds warning information about approach direction and enemy composition.
- **Repair workshop:** **25 Steel plates + 15 Copper**, with local power. Its service draws **40 kW** and repairs nearby damaged equipment using local supplies. It is not the Home ammunition-crafting station.
- **Turbine hall:** **60 Steel plates + 30 Copper + 40 Concrete**, with power for commissioning. Adds **600 kW** generation; it does not register an additional assault base.
- **Workshop field-repair records:** an optional guarded discovery that permanently speeds up field repairs. No manufactured item chain is required to obtain it.

### Recruit specialists

Recruits are found and recruited locally; they do not require a material purchase. The upgrades some recruits enable still have their own costs.

- **Foreman** — Unlocks blueprint copy/paste, saved layouts, queued construction and Truck construction work.
- **Electricians** — Unlock **Big poles** and **Floodlights**.
- **Concrete crew** — Unlock the **Mixer**, **Concrete production** and **Barricades**.
- **Lamplighters** — Unlock **Arc lamps**.
- **Surveyors** — Add district-type map information. They do not reveal every hidden reward.
- **Gunsmith** — Enables a **25% Gun turret hopper upgrade**, costing **20 Steel plates + 10 Copper per turret**. The current integer capacity calculation produces **62 rounds**, up from 50.
- **Rail crew** — Enables a **25% tram cargo upgrade**, costing **20 Steel plates + 10 Copper once**. Tram capacity rises from **200 to 250 items**; platform and arrival inventories remain 200 each.

### Complete the three engineering encounters

Each encounter costs **30 Steel plates + 15 Copper**, needs local power and adds **100 kW** of commissioning demand. Progress is measured in productive seconds: blockers or defenders can stop it, and the encounters trigger defence waves. Interrupted progress and paid materials are retained.

- **Junction Heart / Arsenal** — Requires powered ordinary **Poles on its west and east sides within 14 tiles**, plus cleared defenders. Takes **30 productive seconds**. Unlocks the **two-barrel rifle**, **Cannon** and **Shell recipe**. The current gate specifically checks ordinary Poles; Big poles are not substitutes.
- **Furnace Walker** — Requires the Arsenal, **two busy powered processing machines within 14 tiles**, and cleared defenders. Takes **40 productive seconds**. Reward: **50 Steel plates delivered to Home**.
- **Blackout Crown** — Requires the Arsenal, **two powered lights within 14 tiles**, and cleared defenders. Takes **50 productive seconds**. Reward: **50 Copper delivered to Home**.

These are the current encounter dependencies and rewards, not promises of future boss designs. Their wiring clarity, reward appeal and campaign pacing remain subject to review.

### Defend and recover

- Home and commissioned regional bases can be attacked. Production, ammunition delivery, lighting and repair supplies support defence.
- Base cores have **300 HP**. An ordinary core repair costs **2 Steel plates + 1 Copper** and restores **40 HP in 4 seconds** before applicable repair improvements.
- Recovering a disabled core costs **10 Steel plates + 5 Copper** and takes **12 seconds** before repair improvements. An installed regional Power core is retained.
- The Truck can perform explicit equipment recovery; cargo does not teleport back to Home.
- Normal play runs at **1× speed**, with pause available. Restored services and upgrades do not replace the need for fuel, materials and connected capacity.

Sources: [opening goals](../packages/sim/src/goal.ts), [plants and encounters](../packages/sim/src/progression.ts), [station and Radio restoration](../packages/sim/src/expansion.ts), [regional services](../packages/sim/src/campaignDistricts.ts), [recruits](../packages/sim/src/campaignRecruits.ts), [optional repair discovery](../packages/sim/src/campaignDiscovery.ts), [base defence and recovery](../packages/sim/src/campaignDefence.ts), [attack eligibility](../packages/sim/src/campaignThreat.ts).

## Reference notes

- This catalogue follows **active simulation behavior**, rather than copying older generated design tables. Some retained recipe comments/tables still describe the Refinery, Mixer or Mk2 as future/data-only content; those descriptions do not match the current campaign implementation.
- Legacy machine definitions include player-built tram infrastructure and older resources. Those are not additional buildable options in the current fixed-tram campaign.
- The future turrets intended to consume retained components are **not yet specified here**. No costs, unlocks or recipes have been invented for them.
- This document is a manually maintained snapshot. Recheck the linked source when changing definitions; it is not an automatically generated guarantee that later builds have identical values.
