# Relight — Gameplay, Progression, Items, Weapons and Aliens

**Status: updated design record. The Alien Keys / Alien Schematics / Alien Artifacts system and alien combat direction are accepted. Enemy statistics, populations and raid timings are initial playtest targets, not verified balance or implemented behaviour. Remaining proposals are labelled.**

Prepared 10 September 2026 from the supplied **Relight — Current Gameplay Catalogue**, implementation snapshot dated 10 September 2026. “Current” below means reported by that document; the repository and runtime were not independently inspected. This updates the existing progression draft; it does not modify the repository or the separate confirmed gameplay document. Historical catalogue values and the new intended design are kept distinct. Where they differ, the historical values describe the supplied implementation snapshot, not the desired final behaviour.

**Latest revision:** replaces the earlier artifact-analysis design with three distinct alien item categories; adds regular enemies, light avoidance, camp and stronghold populations, guardians, raid schedules, and ammunition-economy checks. The filename is retained for continuity.

## 1. Direction and scope

The intended loop is: establish Home production and defence, craft equipment, raid small alien camps for Alien Keys, assault a core stronghold, restore a regional plant, establish its factory and freight supply, then prepare for the next expedition. Optional Alien Schematics unlock specific weapons and upgrades; consumable Alien Artifacts combine with manufactured components to build them. Raids periodically test the supply network.

Preserve Home plus three regional plant factories, four fixed tram stops, local physical inventories, finite resource deposits, limited plant power capacity, useful fuelled generators, and normal play at 1× with pause. Do not revive territorial frontage or placeable campaign tram tracks.

The design includes five carried weapon roles, five regular alien types and three stronghold guardians. This document now covers enemies as well as progression. Exact new crafting quantities, unspecified combat parameters, machine-gate refinements and the final restoration objective still need definition. Do not treat those gaps as permission to invent additional systems.

Use three labels consistently: **current** means the supplied implementation snapshot; **accepted direction / initial target** means the decisions recorded here; **proposal / undecided** means further design or measurement is needed. An accepted design is not evidence that it has been coded.

## 2. What the current catalogue reveals

- Most construction options are available immediately if materials are available. Oil products gate Mk2 assemblers and Fast belts materially, but restoration currently contributes little to machine availability.
- Frames and Boards have no current consumers. Wire currently feeds only Boards. These chains need a purpose before teaching or promoting them.
- The player rifle and an Arsenal-unlocked two-barrel rifle are documented. Weapon ownership, switching, weapon crafting recipes, damage and the two-barrel behaviour are not described sufficiently to verify a multiple-weapon system.
- The current three Speed artifacts are unique, removable machine upgrades. The accepted replacement separates consumable Alien Artifacts from crafted removable modules. Existing installed modules and collected rewards need an explicit migration policy; do not silently consume or erase them.
- Furnace Walker and Blackout Crown require the Arsenal but award only 50 Steel plates or 50 Copper respectively. Whether those rewards justify an expedition needs playtesting; they offer little unique progression identity.
- A truck carries 200 stacks, while the tram carries 200 items. For materials stacking to 50, the truck's theoretical capacity is 10,000 items versus the tram's 200. This is a reason to inspect transport balance, not proof that the tram is useless: automation and trip frequency also matter.
- A basic belt's nominal 7.5 items/second already exceeds one Excavator's 0.5 items/second by 15×. Fast belts need a demonstrated aggregate-throughput use case before being presented as a major reward.
- A turret fires 5 rounds/second and holds 50 rounds: about 10 seconds of continuous fire. One basic Assembler makes 10 rounds every 6 seconds, about 1.67 rounds/second. Roughly three dedicated Assemblers would match one continuously firing turret before transport constraints. This is a maximum-fire calculation, not an estimate of actual raid demand.

## 3. Proposed campaign milestones

| Milestone | Player objective | Proposed reward or availability | Dependency rule |
| --- | --- | --- | --- |
| M0: Home established | Generator → Excavator → chest → belt; make and load a turret; obtain a rifle | Complete basic production, power, lighting and defence toolkit | Home crafting and starter salvage must bootstrap this without research |
| M1: First expedition | Clear small camps and collect Alien Keys | Supplies, clues and access to the first core stronghold | Required keys and a viable weapon supply must be outside the sealed stronghold |
| M2: First plant commissioned | Recover a core, prepare a plant, deliberately activate it | Oil processing and an Alien workbench proposal; first regional factory | First stronghold must be beatable without these new machines |
| M3: Second plant commissioned | Supply another expedition and restore a second factory | Assembler Mk2; candidate advanced equipment recipes | Prior factories and tram freight support increased demand |
| M4: Third plant commissioned | Establish the final regional factory | Access to the final restoration project | Completion requirements are not defined in the supplied catalogue; do not invent an automatic victory |

Milestone gates use the number of distinct commissioned plants, not an assumed core-to-plant pairing. Current cores are interchangeable between eligible plants; preserve that unless a separate decision changes it. Commissioning unlocks remain earned when a plant is disabled, while machines still require actual operating power.

Restore tram stops independently using their existing power and material requirements. Do not add a plant requirement that prevents the first useful freight connection. Specialist discoveries remain useful parallel progression.

### Accepted Alien Key rules

- Start with **three Alien Keys required per stronghold**. Where the authored map supports it, provide four eligible camps so the player can choose which three to raid; exact sites still need placement.
- Keys belong to a specific stronghold access system. Use player-facing names such as **Freight Alien Key**, with **Freight Stronghold access: 2/3**. “Access fragments” is superseded player-facing terminology.
- Claim each camp's key reward once. Re-entering, reoccupation, reloading or repeated interaction must not award another copy of that camp's key.
- Store collected keys in a persistent quest pouch/progress record, not ordinary backpack slots. They are never crafting ingredients and cannot be discarded to block progression.
- Opening the stronghold permanently completes the access requirement. It does not award the power core; the player must assault the stronghold and recover the core locally.
- Map information follows discovery: search areas, discovered camps, cleared camps, stronghold key progress and core recovery state. Do not reveal undiscovered loot precisely by default.
- All keys needed for a stronghold must be accessible outside its locked section. Later strongholds should demand different preparations, not simply more keys by default.
- No new power-dependent whole-district barriers are specified. Stronghold access locks are distinct from locking entire city districts.

## 4. Complete current item catalogue and proposed placement

All 21 reported inventory item types are represented below. Packed machines are handled in the machine catalogue. New quest records, Alien Schematics, consumable Alien Artifacts, modules and weapon items are additional or replacement definitions, not falsely counted as existing inventory types. See section 8 for the intended new catalogue.

| Current item | Current source or gate | Current use | Proposed progression treatment |
| --- | --- | --- | --- |
| Steel plates | Salvage/patches; Foundry converts Iron ore | Construction, ammunition, components, restoration | Available from M0; preserve initial salvage route |
| Copper | Salvage/patches; Foundry converts Copper ore | Construction, ammunition, Wire, restoration | Available from M0 |
| Stone | Rubble/solid deposits | Concrete | Gather from M0; processing remains Concrete crew reward |
| Coal | Manual gathering or Excavator | Generator fuel; Shell ingredient | Available from M0 |
| Iron ore | Manual gathering or Excavator | Steel plates | Foundry available from M0; ensure first accessible ore and power are sufficient |
| Copper ore | Manual gathering or Excavator | Copper | Foundry available from M0 |
| Crude oil | Pumpjack only | Fuel and Polymer | Oil machinery proposed at M2; deposits may be discovered earlier |
| Refined fuel | Refinery | Generator fuel | M2 through oil chain; currently 4 MJ/unit, same as Coal |
| Polymer | Refinery | Mk2 assembler, Fast belt | M2; candidate alien-cell and weapon construction material |
| Wire | Assembler/Mk2 | Boards | Available with Assembler; proposed electrical/weapon consumers need priced recipes |
| Frame | Assembler/Mk2 | No current consumer | Proposed advanced weapon and Mk2 construction ingredient; do not present as useful until a consumer exists |
| Board | Assembler/Mk2 | No current consumer | Proposed Alien workbench and advanced equipment ingredient; same requirement for real consumers |
| Concrete | Mixer; Concrete crew | Barricades, Turbine restoration | Retain specialist gate |
| Shot magazine | Home workshop or Assembler/Mk2 | Player rifle and Gun turret; 10 rounds | Retain M0; proposed two-barrel uses the same ammunition with explicit per-shot consumption |
| Shells | Assembler/Mk2; Arsenal | Cannon | Retain Arsenal gate; do not silently reuse Cannon Shells as shotgun cartridges |
| Power core 1 | Occupied freight depot | Commission any eligible plant | Add freight Alien Key prerequisite to its stronghold |
| Power core 2 | Occupied quarry works | Commission any eligible plant | Add quarry Alien Key prerequisite |
| Power core 3 | Occupied waterfront warehouse | Commission any eligible plant | Add wharf Alien Key prerequisite |
| Speed artifact 1 | Workshop salvage room | Removable +10% production-speed module | Historical module; replace the reward model with schematic + craftable Overclock Module; migration required |
| Speed artifact 2 | Quarry equipment cache | Same module effect | Same historical-to-crafted-module transition; no random stat rolls specified |
| Speed artifact 3 | Secured wharf store | Same module effect | Same transition; keep an artifact resupply route for replacement crafting |

### Current inventory rules to preserve until deliberately revised

Backpack: 40 slots. Most materials: 50/stack. Shot magazines and Shells: 20/stack. Cores, artifacts and packed machines: one/slot. Supply chest: 200 items. Truck: 200 stacks. Tram: 200 items, upgraded to 250; platform and arrival inventories: 200 items each.

Magazine transfer currently uses whole magazines, with partially spent rounds retained inside weapons. New equipment must respect loaded ammunition during switching, packing, saving and recovery. New ammunition stack limits and weapon slot rules need explicit definitions; do not infer them from old items.

## 5. Complete current recipe catalogue

These values are copied from the supplied snapshot, not proposed rebalance values. Base times assume sufficient power, inputs and output space. Mk2 processes assembler recipes at 2× speed; a compatible Speed artifact multiplies processing speed by 1.1.

| Output | Ingredients per craft | Base time | Current location/gate | Proposed change |
| --- | --- | --- | --- | --- |
| 1 Steel plates | 2 Iron ore | 3 s | Foundry | None |
| 1 Copper | 2 Copper ore | 3 s | Foundry | None |
| 4 Refined fuel | 1 Crude oil | 3 s | Refinery | Refinery availability moves to M2 |
| 1 Polymer | 2 Crude oil | 3 s | Refinery | Refinery availability moves to M2 |
| 2 Wire | 1 Copper | 1 s | Assembler/Mk2 | Add meaningful consumers, not recipe inflation |
| 1 Frame | 2 Steel plates | 2 s | Assembler/Mk2 | Add consumers |
| 1 Board | 3 Wire + 1 Steel plates | 4 s | Assembler/Mk2 | Add consumers |
| 1 Shot magazine / 10 rounds | 2 Steel plates + 1 Copper | 6 s | Home workshop; Assembler/Mk2 | None |
| 1 Shell | 2 Steel plates + 1 Coal | 3 s | Assembler/Mk2; Arsenal | None |
| 1 Concrete | 2 Stone | 2 s | Mixer; Concrete crew | None |

Home crafting consumes carried materials and requires proximity; an Assembler is not required. Machine ingredient buffers generally hold four crafts per ingredient and finished output five items. A recipe unlock does not bypass these local inventory constraints.

## 6. Complete buildable catalogue and proposed gates

All 24 current buildable types are included. “Initial” means no recruit/encounter gate in the supplied snapshot, subject to costs and placement. Costs are current construction costs; S = Steel plates, C = Copper, P = Polymer, Co = Concrete. Operating consumables are separate.

| Type | Current cost | Current gate | Proposed gate or treatment |
| --- | --- | --- | --- |
| Excavator | 10 S | Initial | M0 |
| Pumpjack | 20 S + 10 C | Initial | M2 |
| Foundry | 30 S + 10 C | Initial | M0; never lock basic metal sustainability behind a core |
| Refinery | 40 S + 20 C | Initial | M2 |
| Assembler | 40 S + 20 C | Initial | M0; automate defence before first stronghold |
| Assembler Mk2 | 60 S + 30 C + 4 P | Initial; Polymer dependency | M3; candidate Frame/Board construction cost requires separate balancing |
| Mixer | 20 S + 10 C | Concrete crew | Retain |
| Supply chest | 10 S | Initial | M0 |
| Belt | 1 S/tile | Initial | M0 |
| Fast belt | 2 S + 1 C + 1 P/tile | Initial; Polymer dependency | Available through M2 Polymer; no extra milestone unless throughput evidence supports it |
| Inserter | 1 S + 1 C | Initial | M0; optional because compatible belts transfer directly |
| Underground belt | 5 S + 2 C/endpoint | Initial | M0; layout freedom should not require progression |
| Priority splitter | 5 S + 2 C | Initial | M0; useful for supplying defence and production |
| Generator | 30 S + 10 C | Initial | M0; remains useful beside plants |
| Pole | 1 S + 1 C | Initial | M0 |
| Big pole | 4 S + 4 C | Electricians | Retain |
| Substation | 50 S + 25 C | Initial | Move to Electricians, provided basic Pole coverage remains practical |
| Lamp | 1 S + 1 C | Initial | M0 |
| Floodlight | 10 S + 5 C | Electricians | Retain |
| Arc lamp | 4 S + 4 C | Lamplighters | Retain |
| Gun turret | 15 S + 5 C | Initial | M0; no electrical demand currently |
| Cannon | 40 S + 20 C | Arsenal | Retain |
| Wall | 2 S | Initial | M0 |
| Barricade | 2 S + 4 Co | Concrete crew | Retain |

One proposed additional station: **Alien workbench**, available at M2. Its duties are decoding Alien Schematics, crafting alien weapons and modules, and energy-cell charging. Decoding a schematic does not consume an Alien Artifact. Use one understandable station rather than introducing a research lab, charger and weapon fabricator simultaneously. Candidate construction ingredients: Steel plates, Frames and Boards. Exact quantities, footprint, power draw and buffers are undecided.

## 7. Carried weapon direction

**Owner range override (2026-09-11):** Current provisional runtime profiles are Rifle effective/max 18/30 tiles; existing two-barrel equipment uses shotgun spread at 6/12; Arc 9/9; Plasma 14/14 travelling at 12 tiles/s. Conventional damage falls linearly beyond effective range. This supersedes unspecified ranges and the paired-shot role below for this bounded iteration. Arc/Plasma acquisition, cell economy, chaining and charged firing remain later proposals; runtime profiles alone do not implement those routes. See `Implementation/GP_CHECKPOINT.md` for final values and brief evidence.

Target five distinct weapons, including the existing two reported weapons. The reference Rifle damage is 10 per round for the initial enemy targets in section 11. Other weapon damage, range, fire rate, ammunition consumption, construction costs and crafting times still need definition. None are claimed here as verified balance.

| Weapon | Current status | Proposed role and limitation | Proposed acquisition | Ammunition |
| --- | --- | --- | --- | --- |
| Rifle | Reported current weapon | Reliable general-purpose weapon; remains economical throughout | Home crafting available during M0; verify current ownership/crafting model | Existing Shot magazines |
| Shotgun | New proposal | Close-range spread, useful against clustered attackers; poor distant efficiency | Gunsmith enables Home crafting; place the specialist on an optional early expedition route | New Shotgun cartridges, crafted at Home and automated in Assembler |
| Two-barrel rifle | Current Arsenal unlock; behaviour unspecified | Proposed deliberate paired-shot weapon for exposed priority targets; slower recovery and higher ammunition use than Rifle | Retain Arsenal unlock; add an explicit crafting/equipment acquisition path if absent | Existing Shot magazines; consumption per shot must be explicit |
| Arc projector | New alien weapon proposal | Short-range chaining against several nearby enemies; limited reach and modest single-target damage | Recover and decode Arc Projector Schematic; Alien workbench at M2; craft with Alien Artifacts and components | Rechargeable Energy cells |
| Plasma lance | New alien weapon proposal | Charged, clearly telegraphed high-damage shot; expensive energy use and commitment while charging | Recover and decode a separate Plasma Lance Schematic; proposed M3 fabrication capability; craft with Alien Artifacts and components | Same Energy cells, greater consumption |

The two-barrel role is a proposed redesign, not a claim about its implementation. Inspect its current mechanics before changing it. If playtesting cannot distinguish it from Rifle and Plasma lance, consolidate roles before adding a sixth weapon.

Do not require a new armour system simply to justify Plasma lance. It can initially excel against high-health threats using existing damage rules. Arc chaining must respect range, solid geometry and line-of-sight policy; it must not quietly hit enemies through buildings.

### Carrying and switching

- Recommend two equipped weapon slots with a quick swap; additional owned weapons occupy backpack slots or storage. This is a proposed limit for discussion.
- Keep construction shortcuts configurable and separate from weapon equipment. Show active weapon, loaded ammunition or charge, and reserve supply clearly.
- Swapping must preserve each weapon's loaded state, reload progress policy and cooldown. No free reloads or cooldown bypass through switching.
- Choose and display how partial magazines, partial Energy cells, full backpacks and cancelled transfers behave. Never destroy remaining ammunition implicitly.
- Support the player's existing inventory expectations: item follows cursor while dragging, valid targets highlight, quantity transfer works, and cancellation returns ownership safely.
- Learned schematics persist. Replacing a lost alien weapon can consume more Alien Artifacts, but must not require rediscovering its schematic. Artifact resupply prevents permanently losing access; ordinary weapons remain the fallback.

## 8. Accepted alien item system

There are **three distinct alien discovery/crafting categories**, plus the existing stolen Power cores. Never conflate them in item names, UI, recipes or progression logic.

| Category | Purpose | Acquisition | Inventory and spending rule |
| --- | --- | --- | --- |
| **Alien Keys** | Unlock one stronghold | Small alien camps linked to that stronghold | Persistent quest pouch; three required; permanent access unlock; never used in crafting |
| **Alien Schematics** | Permanently unlock specific weapons or machine upgrades | Exploration rewards, guarded discoveries and authored caches | Recover and decode once; record learned blueprint permanently; no repeated schematic cost per craft |
| **Alien Artifacts** | Craft certain equipment and upgrades | Guarded caches and alien installations, with a repeatable recovery source | One shared physical crafting material in the backpack; consumed alongside ordinary components; final stack limit undecided |
| **Stolen Power cores** | Commission regional power plants | Successful core-stronghold assaults | Existing portable cores; any core fits any eligible uncommissioned plant; installed core survives site disablement |

**This replaces the former proposal to analyze a Speed artifact and unlock multiple weapon blueprints.** Knowledge now comes from a named Alien Schematic. Alien Artifacts are crafting materials, not the finished machine upgrade and not a synonym for keys or cores.

### Schematic discovery and learning

- Each schematic names its result: **Arc Projector Schematic**, **Plasma Lance Schematic**, **Overclock Module Schematic**. Finding one does not unlock the other two.
- Decode at the Alien workbench under the proposed M2 station gate. A schematic found earlier remains secured and explains its future decoding location. Record recovery and learning persistently; do not allow accidental loss of a unique required unlock.
- Learning is permanent and survives death, machine loss, plant outages and save/reload. Crafting requires the learned schematic, the appropriate station and actual ingredients.
- A known duplicate must yield a useful replacement reward instead of a second unusable unlock. Exact compensation and how it is presented remain undecided.
- Display discovery separately from readiness to craft. Show all outstanding requirements if a schematic is known but its station, milestone or ingredients are missing.
- Starter weapons and mandatory campaign completion must not depend on finding an optional alien schematic.

### Artifact supply and spending

- Begin with **one shared Alien Artifact material**, not separate currencies for every weapon or region.
- Artifacts are consumed to manufacture alien weapons and modules alongside ingredients such as Frames, Boards, Copper and Polymer. Exact quantities and craft times remain undecided.
- Supply must include a recoverable, repeatable route, such as announced reoccupation of selected optional camps. Finite one-time caches alone are insufficient for experimentation and replacement equipment.
- Cleared camps remain cleared long enough for the achievement to matter. Reoccupation is delayed and communicated; it never awards another copy of a previously claimed key. Exact timing and reward amounts need tuning.
- Artifact recovery must be possible using ordinary weapons, without already owning an artifact-crafted weapon. Keep a practical fallback after equipment loss.
- Spend artifacts principally on equipment and upgrades, **not every shot**. Ordinary ammunition remains manufacturable from ordinary resources.

### Crafted machine modules

**Overclock Module Schematic → Alien Artifacts + manufactured components → Overclock Module → install for +10% production speed.**

- The finished module is removable and reusable. Moving it between machines does not consume another Artifact.
- Retain the current one-module-slot model on compatible production machines as the baseline. A speed module multiplies processing speed by 1.1; power, input supply and output capacity can still limit real output.
- Artifacts spent on a module cannot simultaneously fund a weapon. This is the intended factory-versus-equipment choice, with repeatable artifact supply allowing later experimentation.
- Do not invent other module effects, stacking rules or research tiers yet. Additional schematics can be designed once this first loop works.
- The old three named Speed artifacts are historical definitions. Before changing saves, explicitly map installed/owned legacy upgrades to the new module model and decide how their old discovery locations change. Preserve earned value and prevent duplicate rewards; the exact conversion is not yet specified.

### Example expedition and fabrication flow

1. Raid camps for Freight Alien Keys.
2. Discover an optional Arc Projector Schematic and recover Alien Artifacts from guarded sites.
3. Use ordinary equipment to assault the first stronghold; recover its stolen core and activate a prepared plant.
4. Under the proposed milestone gates, build the Alien workbench and establish component/oil production.
5. Decode the Arc Projector Schematic and craft the weapon with Alien Artifacts and the specified manufactured components.
6. Alternatively, decode an Overclock Module Schematic and spend artifacts improving production. Both paths remain available as more artifacts are recovered.

### Energy-cell proposal retained for discussion

Arc projector and Plasma lance use the same rechargeable Energy-cell family, with different consumption. Cells would be manufactured from ordinary components, such as Copper, Wire and Polymer, and charged at a powered Alien workbench. Filled, partially used and depleted states must be explicit and preserve charge through inventory transfers and weapon switching.

Charging would consume power and time, with occupied input/output capacity. A fuel-free plant would eliminate recurring charging material costs under this proposal; limited charging throughput, carried charge and weapon tradeoffs must keep ballistic weapons useful. Exact recipes, charge units, recharge demand/time and whether charging needs a recurring ordinary ingredient remain open. Artifacts are not a per-shot cost.

## 9. Existing services, specialists and encounters

| Existing feature | Current requirement/reward | Proposed treatment |
| --- | --- | --- |
| Regional plants | 30 Steel plates + 15 Copper + any recovered core; 600 kW each | Retain costs as baseline; activation grants milestone and attack eligibility; installed core survives disablement |
| First away station | 30 Steel plates + 15 Copper and power; Truck unlock | Retain; check station restoration does not create unintended extra attack bases |
| Later station | 40 Steel plates + 20 Copper and power | Retain four fixed stops; two powered stops enable service |
| Radio | 20 Steel plates + 10 Copper; 20 kW operating demand | Retain assault warnings; ensure outage warnings work even when radio power fails |
| Radio upgrade | 15 Steel plates + 10 Copper; powered restored Radio | Retain direction/composition information |
| Repair workshop | 25 Steel plates + 15 Copper; 40 kW; local repair supplies | Retain; clearly distinguish from Home crafting and Alien workbench |
| Turbine hall | 60 Steel plates + 30 Copper + 40 Concrete; commissioning power; adds 600 kW | Retain separate restoration; does not add assault base |
| Field-repair records | Optional guarded discovery; faster field repair | Retain optional permanent improvement |
| Foreman | Recruitment; blueprint tools, queued construction, Truck jobs | Retain |
| Electricians | Recruitment; Big poles, Floodlights | Add Substation proposal |
| Concrete crew | Recruitment; Mixer, Concrete, Barricades | Retain |
| Lamplighters | Recruitment; Arc lamps | Retain |
| Surveyors | Recruitment; district-type map information | Candidate improvement: help locate camp search areas, not instantly reveal all keys |
| Gunsmith | Recruitment; paid turret hopper improvement | Add Shotgun and cartridge recipes proposal; retain existing upgrade pending balance review |
| Rail crew | Recruitment; paid tram capacity 200 → 250 | Retain pending freight-capacity review |
| Arsenal | Paid powered encounter; two-barrel, Cannon, Shells | Retain meaningful equipment reward; ordinary-Pole-only requirement should be explained or deliberately revised |
| Furnace Walker | Arsenal required; powered busy processing machines; 50 Steel plates reward | Candidate replacement/addition: a production efficiency upgrade; not an essential first-core dependency |
| Blackout Crown | Arsenal required; powered lights; 50 Copper reward | Candidate replacement/addition: a lighting or power-management upgrade; not an essential first-core dependency |

Both proposed encounter upgrades need actual definitions before adoption; do not ship unnamed upgrades or invent exact effects from this table. All three encounters currently cost 30 Steel plates + 15 Copper and add 100 kW commissioning demand. Productive durations are Arsenal 30 s, Furnace Walker 40 s and Blackout Crown 50 s. Preserve paid materials and interrupted progress unless deliberately changing that behaviour.

Current Gunsmith upgrade costs 20 Steel plates + 10 Copper per turret and produces a 62-round capacity. Rail upgrade costs the same once. Recruitment itself has no material purchase. These costs are baseline evidence, not balancing endorsements.

## 10. Bootstrap and progression checks

- Generator + Excavator + chest + turret + one magazine costs 67 Steel plates and 16 Copper, before belts, Poles, Coal and the undocumented player weapon cost. Each belt adds one Steel plate; each Pole one Steel plate and one Copper. Tutorial amounts must use the actual chosen layout.
- Starter salvage must cover a viable route to ongoing production from the campaign's finite deposits, including an 80 kW Foundry and a 100 kW Assembler where needed. Do not require inaccessible ore or unavailable recipes to build their own prerequisite machinery.
- One 300 kW Generator can nominally run a 60 kW Excavator, 80 kW Foundry and 100 kW Assembler, leaving 60 kW before lights and other loads. More machines create an understandable reason to expand power.
- A plant supplies 600 kW continuously without fuel, subject to network/site state. Limited capacity is not a countdown until a core expires.
- Every proposed locked weapon must have an available ammunition recipe and an accessible crafting station when it becomes usable.
- Every mandatory stronghold must be viable with the preceding stage's weapons. No required key or crafting dependency can be behind its own access gate. Ordinary weapons must beat every mandatory stronghold without optional alien equipment.
- Losing power, equipment or a battle must not erase earned blueprints or duplicate key rewards. Recovery should restore playability without resetting all campaign work.
- The catalogue does not define the final victory project. Specify it separately before claiming an end-to-end campaign or estimating its duration.

## 11. Accepted alien behaviour and initial combat statistics

### Motivations and encounter behaviours

- **Roaming aliens** occupy the dark city. They hunt the player when disturbed and usually avoid illuminated routes. They do not automatically know where Home is or join every scheduled raid.
- **Camp and stronghold defenders** protect stolen technology, patrol their assigned approaches and respond to intrusion.
- **Raid groups** attack human power infrastructure. Activating plants makes the restoration network a growing threat to the occupation.

Use the same enemy roster with these different behaviours. Population, aggression and navigation should follow the encounter context, not a universal instruction to chase Home.

### Reference combat baseline

These are **initial playtest targets**, not claims about the current implementation:

- Player health: **100 HP** (also reported by the current UI in prior playtests).
- Basic Rifle damage: **10 per round**. Rifle cadence, range and reload timing still need verification/definition.
- Gun turret damage: **10 per round** for this test baseline; retain its catalogue rate of **5 rounds/second** and range of **9 tiles** initially.
- Movement percentages below are relative to normal player walking speed, not sprint speed. Attack animations must provide readable warning and recovery where specified.
- The HP targets must be retuned if the reference damage changes. Do not copy HP without checking the weapon side of the calculation.

| Alien | HP | Damage / attack | Attack interval | Movement | Role and readable behaviour |
| --- | ---: | --- | --- | ---: | --- |
| **Skitter** | 20 | 5 melee damage | 1 second | 90% | Common swarm enemy; weak alone, dangerous around the player's flanks |
| **Stalker** | 60 | 15 damage from a signalled lunge | 3-second lunge cooldown | 110% | Flanker; clearly winds up and pauses after a missed lunge |
| **Spitter** | 50 | Visible projectile dealing 10 damage | 2 seconds | 65% | Ranged support; attacks reachable exposed lights as well as suitable combat targets |
| **Breaker** | 220 | 25 damage to structures or 15 to player | 2 seconds | 45% | Slow obstacle breaker; invites concentrated fire and can push through light |
| **Howler** | 80 | Weak melee attack; exact damage/interval undecided | Alert radius and cooldown undecided | 70% | Alerts nearby existing enemies; priority target that can escalate a fight |

Give each type a recognisable silhouette, movement and attack cue. Introduce them gradually: the first camp mostly uses Skitters and one or two other types. Exact camp compositions remain authored tuning work, not a requirement to place all five types everywhere.

Howlers initially alert **existing nearby enemies**, using a bounded radius and cooldown. They do not summon unlimited reinforcements. Multiple Howlers must not recursively duplicate or generate enemies. Exact melee/projectile ranges, lunge distance, projectile speed, collision rules and animation timings are still required before calling the combat definitions complete.

### Light avoidance

- Ambient aliens prefer alleys, unlit gardens, interiors and other dark routes. They normally go around a well-lit street.
- At an illuminated perimeter they hesitate and look for a darker approach. The player should be able to observe this behaviour.
- Enemies directly engaged in combat and committed raid groups **can cross light**. Light changes approach preference; it is not an impassable barrier or permanent invulnerability field.
- Spitters can target reachable exposed lights. Breakers can force an illuminated approach. Do not require a new invisible light-resistance stat for every species.
- Light must reflect actual powered lamps and occlusion rules. A lamp behind a solid wall must not protect an unrelated street through the wall. Failed power removes the relevant deterrence.
- Use visible gathering at the edge of light, lamp damage/flicker where appropriate and the darkened route to explain the threat. Do not make decorative flicker secretly toggle pathfinding every frame.
- Navigation must retain a reachable attack/retreat solution when the player lights all available approaches. Preference for darkness cannot leave a raid stuck forever.

## 12. World, camp and stronghold populations

These are **initial authored population targets**, not measured engine budgets. They describe local groups and encounter totals rather than one huge global spawn count.

| Location | Initial population |
| --- | ---: |
| Protected starting court | No ambient spawns inside; scheduled raids may enter after preparation |
| Ordinary dark street or small courtyard | 3–6 |
| Dangerous alley, ruin or resource site | 8–14 |
| Roaming patrol | 4–8 |
| First-region key camp | 18–26 total defenders |
| Later key camp | 28–42 total defenders |
| First stronghold | 50–70 defenders plus one guardian |
| Second stronghold | 75–100 defenders plus one guardian |
| Final stronghold | 100–140 defenders plus one guardian |

Treat ambient groups, patrols and camp defenders as explicit encounter populations; do not automatically stack every row onto the same location. The citywide total depends on the authored number of sites and patrol routes. No total active-AI count or performance certification has been agreed.

**Encounter total is different from simultaneous engagement.** Start with about **12–20 engaged enemies** in early strongholds and **20–30** later. These are desired encounter flow targets, not hard rules that make the next enemy stop attacking when a counter is full. Use patrol routes, separated buildings, visibility and alert range to create manageable engagements. Large concentrations need open courtyards with readable attacks and movement.

A camp can contain an outer patrol, occupied buildings and a defended key location. Careful scouting allows smaller fights; triggering a Howler or rushing through the whole camp escalates danger. Do not wake every defender globally from one harmless distant interaction.

Cleared camps remain clear for a meaningful period. Selected optional camps can later be reoccupied to provide repeatable Artifact opportunities, with advance indication and preserved one-time key/schematic reward records. Specific reoccupation timing, amounts and duplicate-schematic compensation remain undecided. Avoid enemies popping into view or spawning on the player, their equipment or newly established factories.

### Stronghold guardians

| Stronghold | Guardian concept | Initial HP | Encounter identity |
| --- | --- | ---: | --- |
| Occupied freight depot | Freight guardian | 600 | Slow charging creature that breaks cover |
| Occupied quarry works | Quarry guardian | 1,000 | Armoured presentation with an exposed weak point after a heavy attack |
| Occupied waterfront warehouse | Wharf guardian | 1,500 | Ranged arena control with clearly telegraphed danger zones |

The guardian is **additional to** the listed defender total. Guardian support groups come **from that defender budget**, not an unbounded extra source. Avoid endless reinforcements during a long health-pool fight.

Attack damage, movement, cooldowns, weak-point multipliers, duration of danger zones and arena geometry remain undefined. Armour/weak points are a specific guardian design to implement deliberately; they do not imply a mandatory global armour-stat system for all aliens. Make guardian difficulty depend on readable attacks, positioning and preparation, not HP alone.

These guardians are new core-stronghold encounters. They do not silently replace the existing Arsenal, Furnace Walker or Blackout Crown engineering encounters in section 9.

## 13. Base harassment and sustained major assaults

**Accepted direction:** small raids keep defence supply relevant; large assaults maintain pressure over a declared attack window. Small raids should normally be handled by supplied defences while the player explores. Major assaults are announced events worth preparing for.

### Initial schedule at 1×

| Event | Initial timing target | Total attackers | Attack window |
| --- | --- | ---: | --- |
| Initial preparation | First 12–15 active-play minutes | No scheduled base raid | Tutorial/setup grace; does not make remote alien camps harmless |
| Early harassment | Every 5–7 minutes after the grace period | 6–10 | Short encounter |
| Established-campaign harassment | Every 4–6 minutes | 10–18 | Short encounter |
| Late-campaign harassment | Every 4–6 minutes | 16–26 | Short encounter |
| First major assault | Around 25–30 minutes into a fresh game | 50–70 | 2–3 minutes |
| Subsequent major assaults | Roughly every 18–22 minutes | 90–140 | 3–4 minutes |
| Late major assaults | Same broad 18–22 minute cadence | 160–220 | 4–5 minutes |

All timings are **active simulation time at 1×**; pause does not advance them. Reconcile these targets with the current day/night and Night 3 warning system in one scheduler. Do not leave the old schedule running alongside a new minute-based director. The exact mapping of early/established/late tiers to restoration progress, interval anchoring and recovery-grace duration still needs definition.

### Scheduling and delivery rules

- Use one **global harassment schedule across eligible factories**, not a separate raid timer at each factory. Four factories must not multiply interruption frequency by four.
- Home and commissioned regional plants are eligible targets. Later plants can receive a larger share of attacks. Existing rules retain attack eligibility after switching a commissioned site off.
- Major assaults focus on **one announced location**. Give useful advance warning and enough information to prepare or travel. Exact warning lead time still needs travel-time measurement; preserve the role of Radio and its precision upgrade.
- Suppress extra scheduled harassment during a major assault and allow a short recovery period afterward. Do not release accumulated missed raids as a burst immediately after suppression ends.
- During a major assault, use overlapping groups with only short lulls, so enemies keep arriving across the attack window. The totals above are **per assault**, not per wave, minute or factory.
- Enemies enter from actual approach locations and travel to the base. Do not spawn inside a defended factory or teleport replacements to maintain pressure.
- Distinguish the reinforcement window from cleanup. Surviving attackers remain real enemies after the last group has arrived; do not despawn them merely because the timer expires.
- Exact simultaneous base-assault population targets must be established with navigation/readability and frame/tick measurements. Stronghold engagement targets are not automatically the base-assault cap.
- Avoid an automatic failure spiral after a disabled base. Preserve equipment/core recovery and ensure the scheduling rules give the player a workable recovery opportunity; exact policy remains to be specified.

## 14. Factory demand and ammunition balance

A healthy ammunition reserve is a reward for successful automation. Do not secretly drain inventories or increase raids simply because the player has stock. Production should matter through expedition expenditure, multiple supplied defence positions, repairs and sustained peak consumption.

The current basic Assembler recipe supplies **100 rounds/minute**: ten rounds every six seconds. A continuously firing turret at five rounds/second consumes **300 rounds/minute**. Its current 50-round hopper lasts roughly ten seconds under uninterrupted fire.

At the initial ten-damage-per-round reference baseline, ignoring misses, overkill, reloads and special damage rules:

| Example | Minimum rounds | Equivalent magazine amount | Basic Assembler production time |
| --- | ---: | --- | ---: |
| Eight 20-HP Skitters | 16 | 1.6 magazines' ammunition; supply two whole magazines | 12 seconds to craft two magazines |
| Sixty enemies at 50 HP each | 300 | 30 magazines | 3 minutes |

Actual mixes, armour/weak points where implemented, misses and concurrent player usage change demand. These examples are checks, not guaranteed combat costs.

- Frequent small raids alone will not exhaust a dedicated magazine line. Do not solve this solely by shortening raid intervals until exploration is impossible.
- Large assaults should test delivery rates and buffers as well as total inventory. A remote stockpile does not help if turrets cannot receive it fast enough.
- More factories create more supply destinations and repair needs. Use the fixed tram and local storage to make the network valuable rather than relying on manual emergency transport for every raid.
- Human ammunition remains an ordinary-resource production chain. Alien Artifacts are chiefly equipment/module construction inputs, not a mandatory per-shot expedition tax.
- Reusable Energy cells need a measured power/throughput tradeoff before assuming alien weapons can coexist economically with ballistics.
- Measure remaining finite resource reserves over repeated raids and expeditions. An accepted pressure schedule must not make the mandatory campaign mathematically impossible through unavoidable resource exhaustion.

## 15. Remaining design and implementation details

The three alien item categories, ordinary enemy roles, light-avoidance direction, guardian concepts and the population/schedule tables are recorded decisions or initial targets. Do not re-open the superseded artifact-analysis model as an equal alternative.

Still to resolve:

1. Finalise the proposed M2 oil/Alien workbench and M3 Mk2/advanced-fabrication gates, and the Substation specialist gate, against a playable bootstrap path.
2. Assign each new weapon/module a specific schematic source, crafting station, ingredient quantities, craft time, footprint where applicable and operating requirements. Preserve real Frame/Board consumers rather than teaching dead-end recipes.
3. Verify the existing two-barrel implementation and finalise two equipped slots versus another loadout limit. Define all weapon statistics, reload/swap rules and ammunition inventory semantics.
4. Specify Energy-cell manufacture/charging and Artifact stack size, repeatable reward quantity, reoccupation timing and duplicate-schematic compensation.
5. Define migration from the three old Speed artifact IDs to crafted modules and new discovery rewards without losing or duplicating earned upgrades.
6. Define remaining regular-enemy and guardian attack parameters, authored compositions, camp locations and full AI/navigation budgets. Do not claim that the target counts have passed Phaser performance checks.
7. Map raid tiers and timings to the campaign calendar; specify advance warnings, suppression/recovery rules and target selection while preserving exploration time.
8. Define distinct rewards for Furnace Walker and Blackout Crown, and the final restoration/victory objective. The third plant does not automatically complete an undefined endgame.

## 16. Focused validation

Use one complete playable cycle: **prepare Home → raid key camps → assault a stronghold → recover core → activate a plant → establish supply → defend it**. Then check the next region's prerequisites and a late-assault scenario rather than repeating broad tests without a concrete risk.

Record:

- Tutorial clarity, starter material sufficiency and time until the first functioning defence supply.
- Expedition duration split into travel, combat, gathering and waiting; how often raids force a return.
- Enemy totals and peak simultaneous engagement; whether silhouettes, attacks and light reactions remain readable.
- Rifle/turret ammunition production, rounds fired, loaded reserves, delivery bottlenecks and repair demand per event.
- Artifact income versus weapon/module costs and replacement availability using ordinary gear.
- Alert timing, target location clarity, ability of stocked defences to handle harassment without manual intervention, and recovery after a major assault.
- Real frame/tick behaviour under declared active-enemy, lighting, pathfinding, production and freight workloads. No unmeasured population target certifies performance.

Focused correctness checks cover: no duplicate keys on reload/reoccupation; persistent schematic learning; artifacts consumed exactly once per successful craft; reusable modules preserving ownership; no free ammo/charge from swapping; cancelled drags preserving items; light changes updating navigation without stranded raids; meaningful old-base and tram use; core/unlock persistence after defeat; and attack suppression preventing piled-up raids.

Do not substitute a simulated completion flag for a player run. Record observations and tune the initial targets together: damage, health, populations, supply rates, travel time and raid frequency all affect each other. Do not lengthen the campaign by adding compulsory empty travel or unavoidable ammunition grind.

## Source boundary

The source catalogue references `itemNames.ts`, `flow.ts`, `engineer.ts`, `recipes.ts`, `progression.ts`, `campaignRecruits.ts`, `fixedTram.ts`, `truck.ts` and other simulation files. Those links are repository-relative; the underlying files were not supplied here. Internal IDs, current weapon/enemy behaviour and exact unlock predicates need verification against the repository before implementation.

The complete current 21-item, 10-recipe and 24-buildable catalogues above remain historical reference. New item categories, enemy definitions and event targets describe intended changes, not verified implementation. New recipe quantities and unspecified combat values remain explicitly undecided.
