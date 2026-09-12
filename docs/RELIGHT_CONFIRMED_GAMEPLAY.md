# Relight — Confirmed Gameplay Direction

**Narrow supersession, 2026-09-10:** The [updated design record](Design/RELIGHT_PROGRESSION_AND_WEAPONS_DRAFT.md) supplies newer accepted Keys/Schematics/Artifacts, crafted Overclock modules, five weapon roles, five regular enemies, three core guardians and global harassment/sustained-assault direction. Its accepted sections supersede conflicting parts of §§7,9,11–13,16,21 here; enemy/stat/population/time tables are initial targets, not measured balance. Directly installed Speed artifacts are historical; Schematics provide knowledge and Artifacts are consumed in crafting reusable equipment. No artifact-analysis blueprint path is active. Acquisition details, machine gates, recipes and final objective remain proposed/undecided. Four fixed stops and current CITY-F geography supersede earlier counts. Unaffected rules and completed evidence remain. [Implementation plan §6](EXPLORATION_DEFENCE_PLAN.md#6-gameplay-implementation-reconciliation-and-delivery-plan) awaits owner review; [PROGRESS](PROGRESS.md) alone owns status.

Implementation amendment, 9 September 2026: the owner authorises the complete audit correction set and explicitly rejects territorial frontage (see §10). The current build provides physical core/plant restoration, finite connected power, new production chains, optional removable speed artifacts, paid Gunsmith/Rail crew improvements, passenger tram travel, explicit equipment recovery and three engineering encounters. [Implementation defaults, evidence and open gates](Implementation/GAMEPLAY_CORRECTIONS.md) distinguish the new one-slot +10% artifact and +25% capacity starting values from proven balance or a Q07 ending decision.

**Gameplay reference · 9 September 2026**

This document consolidates the established gameplay design and the owner's accepted changes. It describes the intended game, not a claim that every mechanic has already been implemented. The latest direction takes precedence over older rules for starting equipment, regional power, tram activation and the interface.

Mechanics are grouped by their purpose. Experimental balance values, unapproved alternatives and implementation tasks are excluded.

## 1. Game identity and core loop

Relight is a top-down game about an engineer rebuilding and reclaiming a ruined city through automation, exploration, combat and restoration.

Aliens have stolen the city's power cores and taken them to their bases, where they use them to provide power. The player explores to locate those bases, recover the cores and restore regional power plants around the city.

The player builds machines and defences, establishes supply lines, explores different regions, finds survivors, restores infrastructure and expands the area they can support. Each reactivated regional power plant becomes an alien attack target and the centre of another factory and defensive position.

**Core loop:**

1. Gather and manufacture supplies.
2. Build and maintain production and defence.
3. Choose a region, restoration target or power-core search area.
4. Scout, gather resources and deal with local enemies.
5. Recover a core, find a survivor, obtain an artifact or restore infrastructure.
6. Use the resulting power, transport or upgrades to improve the factory and expand farther.

Automation supports expeditions and sustained defence. Exploration provides resources, infrastructure and upgrades that make further automation worthwhile.

## 2. Player and direct control

- The player directly controls an engineer moving through the city.
- The engineer can walk, sprint and dodge.
- Movement interacts with the physical world: solid structures and terrain block passage.
- Sprint and dodge use the movement/stamina system.
- The engineer gathers resources, carries items, places equipment, interacts with installations and uses weapons.
- The selected tool or item determines the primary action: gathering with an empty hand, placing a building, or firing an equipped weapon.
- Interaction with storage, machines, survivors and restoration sites is a distinct action.
- Personal construction and interaction respect physical reach and access.
- The engineer has health and can be knocked down by enemies.
- The established knockdown recovery returns the engineer to Home with inventory retained.
- Darkness creates visibility and enemy-related danger; simply standing in a dark area does not itself damage the engineer.

The player does not manage hunger, thirst or a separate survival-needs system.

## 3. Starting the game

- The player starts with nothing and builds up their own machines and defence.
- The opening takes place at the Home/base location.
- Hand gathering provides access to the first basic materials.
- Manual crafting and construction provide the route into the first automated production.
- The player establishes production, power and defensive equipment before the first major base attack.
- The opening guidance identifies the next useful action and where to perform it.

Starting with nothing replaces the earlier opening based on supplied stock and functioning starter equipment. The initial production chain must be achievable from hand-accessible resources.

### Authored Riverfront Arc world

The owner’s full-city brief replaces the earlier irregular authored layout. The canonical city is `riverfront-arc-v4`, an 864 × 576 tile north-bank city defined in `packages/sim/src/city/riverfront.ts`. Its 24 × 16 planning lattice uses 36-tile cells, with 287 fitted buildings, a large Civic Centre, connected gridded roads, northern cul-de-sacs and four unchanged factory-yard footprints. New games use the same geography, supplies, rewards, crews and infrastructure. Seed selection does not move this campaign. Existing procedural saves continue to use their original world. Earlier authored v1/v2/v3 saves require their original builds; v4 requires New Game and never relocates saved machinery. The expanded downtown has a town hall/square, apartments, offices, department stores, arcades and parking structures. Ground-floor interiors, local occluded day/night lighting and finite powered fixtures remain separate from discovery. Three compounds retain their existing enemies and permanent core recovery. Enemy variety, bosses, new access locks and campaign-duration tuning remain deferred.

Founders Court is the southwest maintenance cul-de-sac, with a workshop, salvage garages, a turning circle, one access neck and an adjoining factory yard. Riverside Works is the waterfront plant, Ironworks the northern industrial plant, and Civic Utility the eastern municipal plant. Those three plants plus Home are the four factory destinations. Their regions are finite power service areas, not claimed territory. The three stolen cores occupy the northwestern freight depot, northeastern quarry works and southeastern wharf warehouse; any recovered core can restore any eligible plant.

Exactly four permanent stops follow **T1 Home → T2 Riverside → T3 Ironworks → T4 Civic / East Wharf** on one continuous route with radius-12 circular bends. Rail drawing, tram distance/heading and passengers share that curve; the renderer interpolates the existing simulation ticks. Service skips unpowered stops once two stops have power. Track and boarding strips are protected from ordinary construction; side loading tiles permit direct belts and inserters. Last-mile freight uses actual local inventories. Placeable rail infrastructure stays out of the active catalogue.

Westridge and Old Town each contain three enterable homes with authored story, survivor or shortcut content. Additional workshops and occupied installations contain the existing projects and rewards. Boarded buildings are solid background objects. Enterable parcels have three-tile doors and same-map furnished interiors; crossing inside fades only that roof, leaving restores it with threshold hysteresis. Roofs never alter collision or combat visibility. Marked light debris can be cleared and the Westridge return gate opened from inside; both persist in saves.

Factory yards are explicitly buildable concrete. Permanent buildings, substantial furniture, service cabinets and scenery remain physical. Selected street fixtures and building lights follow actual regional supply; fixture demand is charged to that supply. Compact 1×/pause controls remain. Normal world presentation has no frontage boundaries, patrol/spawn circles or raw diagnostic labels. Maps show named infrastructure, plant state and broad core search areas; collected rewards lose their recovery prompt. Tracking leads to public entrances rather than solid building centres.

[World conventions, yard layouts, travel observations, navigation tuning and focused evidence](Implementation/RIVERFRONT_CITY.md) record the implementation. This replaces older procedural-map claims without retroactively changing their archived evidence.

## 4. Resources and manufactured items

Resources exist physically in the world and in inventories. The player gathers them manually or extracts them with machines.

### Resources

| Resource | Gameplay use |
| --- | --- |
| Steel | Construction, components and ammunition. |
| Copper | Electrical components, construction and ammunition. |
| Stone | Construction material and concrete production. |
| Coal | Generator fuel and shell production. |
| Iron ore | Processed through the Foundry into steel. |
| Copper ore | Processed through the Foundry into copper. |
| Crude | Extracted and transported as items for refining into fuel and polymer. |

Salvage and deposits are associated with different parts of the city. Resource locations give the player reasons to explore and establish additional supply routes. Mining out rubble frees the occupied ground for construction.

### Manufactured products

| Product | Production relationship / purpose |
| --- | --- |
| Wire | Made from copper; used in electrical equipment and other components. |
| Frames | Made from steel; used in construction and restoration. |
| Concrete | Made from stone in a Mixer. |
| Boards | Made from wire and steel; used in more advanced equipment and projects. |
| Shot magazines | Made from steel and copper; supply the rifle and gun turrets. |
| Shells | Made from steel and coal; supply Cannons. |
| Fuel | Refined from crude; supports fuel-powered equipment. |
| Polymer | Refined from crude; used in advanced construction/restoration. |

Recipes remain short enough to understand. Different manufacturing stages have distinct machines, inputs and outputs rather than an abstract resource conversion menu.

## 5. Automation and production machines

| Machine or facility | Function |
| --- | --- |
| Excavator | Automatically extracts resources from its working area. |
| Assembler | Manufactures components and ammunition from supplied inputs. |
| Assembler Mk2 | A more capable production machine unlocked through progression. |
| Mixer | Produces concrete from stone. |
| Foundry | Processes ore into usable metals. |
| Pumpjack | Extracts crude as transportable items. |
| Refinery | Processes crude into fuel and polymer. |
| Generator | Supplies local power using fuel. |
| Workbench | Supports manual crafting using the engineer's carried materials. |

- Machines occupy physical footprints and have valid placement requirements.
- Powered machines need access to their connected electrical supply.
- Manufacturing consumes real inputs and creates real outputs.
- Inputs must reach the machine, and output must have somewhere to go.
- Starved, unpowered or blocked machines communicate the cause of their stopped or reduced production.
- Production rates and storage buffers matter because the player must sustain defence and supply projects.
- Machinery can be picked up and repositioned through the building/inventory systems.
- Automated production replaces repeated manual gathering, crafting and delivery work.

## 6. Logistics and storage

| Equipment | Function |
| --- | --- |
| Belt | Moves items between production, storage and consumers. |
| Fast belt | Provides increased belt throughput. |
| Inserter | Transfers items between adjacent logistics elements; supports an item filter. |
| Splitter | Divides belt flow and supports output priority. |
| Underground belt pair | Carries a belt connection beneath an obstruction or crossing. |
| Chest | Stores physical items at a location. |
| Home/Depot storage | The base's physical supply store. |

- Belts use the established single-lane item flow.
- Separate ingredients can be delivered through appropriate belt and inserter arrangements.
- Storage at Home is not a global supply pool available everywhere.
- The engineer carries supplies in their backpack or uses physical transport.
- Construction, delivery and transfers use the relevant actual inventory.
- A displayed count distinguishes material being carried from material stored elsewhere.
- Belts, inserters and storage support production, defensive resupply and infrastructure delivery.

Crude and refined products use item logistics; the game does not require a separate fluid-pipe simulation.

## 7. Power cores and regional power plants

### Stolen power cores

- The aliens have taken power cores from the city.
- The cores are located at alien bases and power those bases.
- Recovering them is a major reason for the player to explore and undertake fights.
- A core is a major progression item, distinct from an optional machine artifact.

### Locating cores

- The map identifies a broad search radius for a core.
- The initial marker does not reveal the exact base or core position.
- The player scouts inside the search area to find the alien installation.
- The search area provides direction while leaving the actual discovery to exploration.

### Restoring a region

- The player takes a recovered core to a power plant around the city.
- The player can choose which plant to restore with a recovered core.
- Restoring that plant provides power for its region.
- Regional power enables the player to support more infrastructure and production in that part of the city.
- Power plants are identifiable restoration destinations, rather than hidden interaction points within the core search area.

### Power-plant factories and attack targets

- Reactivating a power plant makes it a target for alien attacks.
- Each activated plant requires a local factory and defensive position to sustain its protection.
- The local factory supports the ammunition, repair, fuel and other supplies needed by that position. Physical logistics connect its production, storage and defences.
- These sites remain part of the player's defended network while the engineer explores or works elsewhere.
- Expansion therefore increases both the player's available regional power and the number of factory sites they must support.
- The intended structure is **one starting factory plus one factory for each activated power plant**. With three activated plants, the player supports four factory sites: Home and three power-plant factories.

The factory count describes the required pattern of expansion, not an arbitrary building-count check that substitutes for a functioning defence and supply system.

### Connection to the tram line

- Power plants are placed at short to medium distances from the tram line.
- Their sites have usable access for connecting the local factory to tram freight through ordinary logistics.
- Tram-delivered resources and products support these factories and their defences.
- The separate factory sites form a connected production network; they can exchange supplies instead of each reproducing every production chain.
- The city layout provides usable factory space and defensive approaches around the plants as well as access to the tram corridor.

### Plant output and additional generation

- Each reactivated plant supplies a defined, finite amount of power to its region.
- Restoring a plant does not provide unlimited capacity for all future construction.
- Local generators remain necessary as additional automation and powered defensive/support equipment increase demand beyond the plant's capacity.
- Generators add power to the connected network and retain their fuel requirements.

Local generators therefore serve both the opening factory before regional restoration and the growing factories around reactivated plants.

## 8. Electrical distribution and lighting

| Equipment | Function |
| --- | --- |
| Generator | Fuel-powered generation for the opening and additional capacity at expanded factory sites. |
| Regional power plant | Restored using a recovered power core; provides a defined amount of power to its region and becomes an alien attack target. |
| Pole | Connects electrical infrastructure. |
| Big pole | Supports longer electrical connections. |
| Substation | Provides the local distribution/activation point for supported territory. |
| Lamp | Provides local illumination. |
| Floodlight | Provides directional illumination over a larger area. |
| Streetlights | Make restored streets visibly usable and distinguish them from dark streets. |

- Power must reach the installation through the electrical system.
- Production and lighting are connected to real power availability.
- The player can inspect supply, demand and the state of the relevant network.
- Network information distinguishes the plant's output, connected generator output and total demand so the need for additional generation is understandable.
- Limited power affects production and therefore the ability to maintain supplies.
- Lighting has a mechanical role in dealing with darkness-associated enemies, as well as making the city readable.
- Restoring infrastructure visibly changes the surrounding area.
- The interface distinguishes a completed restoration from whether its equipment is currently operating.

## 9. Alien artifacts and machine bonuses

- Alien artifacts are optional improvements found through exploration.
- Artifacts can be attached to machines.
- An attached artifact provides a modest improvement to that machine, including increased processing speed.
- These bonuses are secondary rewards alongside major discoveries such as stolen power cores.
- A machine's artifact bonus is visible in its inspection information.
- The machine still needs its normal inputs, power and output access.

Power cores restore major infrastructure. Machine artifacts improve individual equipment. The two have separate names, functions and presentation.

## 10. Site restoration and recoverable factories

Owner correction, 9 September 2026: the exploration campaign uses defended factory and power-plant sites, **not territorial frontage progression**. This supersedes the earlier Dark → Contested → Held requirement.

- Physical streets, regions, connected power, threats and valid building footprints remain.
- Existing installation/project records represent discovered, prepared, restored, operating, offline and damaged sites.
- Preparation, physical material delivery and deliberate activation remain; explicit engineering encounters may have progress and retries.
- Home and commissioned plants are defended bases. Plant attack eligibility survives an output toggle; damage preserves the installed core for repair and restart.
- Ordinary expansion does not require adjacent-block claims, interior status, boundary ammunition demand, enclosure rewards or automatic claim/burn-off timers.
- Collision, terrain, reach, inventory and explicit site restrictions govern placement. Internal geometry/compatibility flags must not impose an invisible ownership frontier.
- Factory equipment remains recoverable; recruits and earned capabilities persist through setbacks. Truck deployment and recovery are explicit player jobs.
- Power, light, restoration and damage are separate facts. Activation does not erase defenders or complete unrelated projects.

## 11. Weapons and personal combat

### Rifle

- The rifle is the engineer's personal ranged weapon.
- The player selects/equips it and aims toward the cursor.
- Firing consumes ammunition from the player's carried supply.
- The rifle uses the Shot ammunition chain also used by gun turrets.
- Selecting a weapon is distinct from selecting a building or mining with an empty hand.
- Personal fighting supports scouting, expeditions and responses to gaps in defence.
- The Arsenal provides weapon/defensive progression, including the established rifle upgrade.

### Movement in combat

- Sprinting and dodging provide ways to reposition and avoid attacks.
- Enemy attack cues and visible approaches allow the player to respond.
- Personal combat complements prepared defences and supply lines.

The game's sustained defensive capacity comes from production and deployed equipment, not the engineer replacing an entire defensive line indefinitely.

## 12. Defence and base attacks

| Defence element | Function |
| --- | --- |
| Gun turret | Automated fire using supplied Shot ammunition. |
| Cannon | Heavy automated defence using shells. |
| Walls/barricades | Shape approaches and obstruct or delay enemies. |
| Lighting | Supports visibility and counterplay against darkness-associated threats. |
| Repair supplies and interaction | Restore damaged defensive equipment. |
| Automated ammunition supply | Keeps defensive positions operating while the player is elsewhere. |

- The player builds up machines and defence before the first major base attack.
- Base attacks make preparation, ammunition production and defensive layout meaningful.
- Reactivated power plants join Home as attack targets. Their local factories must sustain the defences protecting them.
- An activated plant's attack-target status remains clear even when the surrounding area has been restored and lit.
- The interface communicates the next scheduled major assault.
- Local exploration can involve enemies independently of the major-assault schedule.
- During attacks the player can fight, repair and address supply problems.
- A turret without ammunition cannot provide its normal protection.
- Supply distribution matters: stock at Home does not protect a distant empty turret until it is delivered.
- The player can move and reuse defences when the active frontier changes.

## 13. Enemies, occupied sites and bosses

Aliens occupy bases containing stolen cores. Exploration and restoration take the player into occupied areas and encounters.

The established enemy roles provide different threats and responses:

| Enemy | Gameplay role | Main response |
| --- | --- | --- |
| Crawler | Basic pressure against exposed approaches and infrastructure. | Reliable ammunition supply and defensive coverage. |
| Shade | Exploits dark routes; is untargetable while unlit. | Illuminate the route and engage where it can be targeted. |
| Breaker / Hulk | A heavy threat that pushes through defensive positions. | Concentrate fire and use heavy defence. |
| Stalker | Guards occupied sites and makes scouting dangerous. | Read its territory, choose an approach, retreat or fight. |
| Conductor | Strengthens a bounded group of local enemy sources. | Remove or suppress the local source of pressure. |

- Enemy differences affect preparation and decisions, rather than only appearance or health.
- Threat origins, attack directions and consequential attacks need readable cues.
- Secured interiors do not receive unexplained enemy spawns.
- Major restoration projects include boss encounters.
- Bosses combine enemies and engineering requirements such as supplying, powering or protecting an installation.
- Prepared automated equipment remains useful during boss encounters.
- Completing a major encounter produces useful progress through the associated project or recovered access.

Power-core recovery and machine artifacts add exploration rewards. They do not establish a general experience-point system or a requirement to farm ordinary enemies for random equipment.

## 14. Exploration and the map

- The player scouts different city regions for resources, fights, survivors and infrastructure.
- Regions have recognisable geography and reasons to visit.
- Streets, landmarks, industrial yards, rail approaches and open factory space support navigation and construction.
- The player can choose destinations and prepare supplies for an expedition.
- Exploration occurs in the connected game world.
- The map shows places requiring power or lighting.
- Restoration targets are easy to identify and locate once known.
- Core search areas deliberately reveal an approximate area rather than an exact target.
- Known destinations can be tracked using map/world markers and location guidance.
- The minimap shows useful nearby geography, the player and a tracked destination.
- The full map provides the wider city view.
- Labels and marker states communicate meaningful information without requiring the player to interpret developer coordinates or diagnostic counters.

## 15. Trams and transport

### City tram service

- The city contains **three to four tram stops** to repower.
- Once **more than one stop is powered**, the tram moves between powered stops.
- Restoring additional stops extends the useful network.
- Tram travel connects parts of the city and reduces repeated journeys on foot.
- The tram moves through the physical world along its route.
- Powered stop availability and transport state are visible to the player.

The earlier exact-two-stop limit is replaced by the network above. The player is not required to discover another hidden activation condition after meeting the two-powered-stop requirement.

### Freight logistics

- Rail transport supports movement of physical resources.
- Stops connect to loading and unloading equipment.
- Freight transport makes longer material deliveries practical.
- The tram line supplies the factories around reactivated power plants through their local logistics connections.
- Rail-related progression improves transport and reduces repetitive hauling.

### Truck and later construction support

- A truck provides driven transport for the engineer and a larger carried inventory.
- The truck is found through world/facility progression.
- The Foreman unlocks the Line truck's automated support for deploying and recovering equipment through explicit player jobs.
- Reusable equipment kits capture a reusable arrangement of defensive and support equipment.
- Blueprints and copy/paste are progression tools that reduce repeated construction work.

## 16. Survivors and upgrades

- Survivors are found through exploration.
- Reaching survivor locations and completing their explicit local requirements brings their capabilities into the player's progression.
- Survivors help unlock or upgrade equipment, production, lighting, defence and transport.
- Their contribution is practical and visible through available equipment or functionality.
- Recruited knowledge/unlocks persist through subsequent site damage.
- Survivors do not introduce hunger, housing, happiness or population micromanagement.

Established specialist roles include:

| Group | Contribution |
| --- | --- |
| Electricians | Improved lighting and electrical infrastructure, including Floodlights, Big poles and substations. |
| Concrete crew | Concrete production and defensive construction. |
| Gunsmith | Improvements to defensive ammunition handling. |
| Rail crew | Rail and freight transport capabilities. |
| Foreman | Automated frontier deployment and construction support. |
| Lamplighters | Additional lighting equipment. |
| Surveyors | Improved information about the city. |

Progression comes from discoveries, recovered technology, restored facilities and survivors, rather than a detached research tree.

## 17. Facilities and restoration projects

- Important pre-existing facilities are destinations worth restoring.
- Projects use physical materials and their applicable power/activation requirements.
- Required materials are delivered through actual inventory and logistics interactions.
- The player can inspect a project's requirements, delivered supplies, blockers and benefit.
- Activation is a deliberate interaction when the requirements are satisfied.
- Project progress and completed unlocks persist across saves.
- A restored facility provides a functional reward, a visible environmental improvement and a useful contribution to further expansion.

| Facility type | Gameplay contribution |
| --- | --- |
| Home/Depot | Base storage and starting workshop functions. |
| Foundry | Processing ore into useful metals. |
| Arsenal | Heavy defence, shell production and weapon improvements. |
| Tram facilities | Travel and physical freight connections. |
| Power plants | Regional electrical restoration using recovered cores. |
| Refinery | Fuel and polymer production. |
| Supply and repair installations | Local stocking and support for maintained infrastructure. |
| Transformer installations | Larger-scale electrical restoration. |

The long-term objective is to restore the city through its infrastructure and power network. Automation, territorial expansion, recovered cores and major projects contribute to that objective.

## 18. Backpack and storage interface

- The personal inventory is presented as a Backpack window.
- It opens when requested; it is not a permanent sidebar.
- The Backpack uses a regular grid of equal-sized item-stack slots.
- Item illustrations and stack counts make contents easy to scan.
- Empty slots and used capacity are visible.
- Selected-item details are shown in one place.
- Items can be moved, merged, sorted and split according to actual inventory rules.
- Storage and Backpack appear side by side when transferring items.
- Dragging and quick-transfer controls move real items between the two inventories.
- Partial transfers and full destinations preserve the remaining items correctly.
- Actual item capacity is independent of the illustrative grid size in concept artwork.

The inventory remains a straightforward stack system, without a packing puzzle.

## 19. Build catalogue and action bar

- The Build menu contains recognisable item pictures, names, categories and costs.
- Selecting an item allows normal placement without requiring an action-bar assignment.
- The player can drag items from Build onto the action bar to create shortcuts.
- The player can rearrange and remove shortcuts.
- Shortcuts retain the player's arrangement instead of being automatically shuffled by context.
- Clicking a shortcut or pressing its assigned key selects the relevant building, item or tool.
- Assigning a shortcut does not spend resources, manufacture an item or place equipment.
- Removing a shortcut never destroys inventory.
- Slots show icons, key labels and relevant counts instead of repeated text descriptions.
- A click/keyboard alternative supports shortcut assignment and editing.
- Placement previews show footprints, orientation, relevant connections and clear reasons when placement is invalid.
- Controls and bindings remain discoverable through labels, prompts and tooltips.

## 20. HUD, time, pause and persistence

### Normal gameplay HUD

- A compact objective shows the current goal and next real action.
- Location guidance helps the player reach the relevant object or destination.
- Player health and relevant equipped-item information remain readable.
- Day/night information appears as a small indicator near the map.
- The next major assault is communicated without a large permanent time panel.
- Build, Backpack and Projects have clear access controls.
- Panels open when needed and do not overlap or hide each other's essential controls.
- Machine/project inspection explains actual state and blockers.
- UI size remains readable independently of camera zoom and desktop resolution.

### Time and pause

- Unpaused gameplay always runs at **normal 1× speed**.
- Players cannot accelerate time above normal speed through buttons, shortcuts or saved speed settings.
- Pause remains available.
- Resuming returns to normal speed.

### Persistence and interaction safety

- Saves retain gameplay progress, physical inventory and relevant infrastructure states.
- Custom action-bar arrangements persist.
- UI actions do not accidentally trigger movement, firing, gathering or construction in the world behind them.
- The player can close or cancel an interaction predictably.

## 21. How the mechanics connect

| Player activity | Immediate result | Supports |
| --- | --- | --- |
| Gather and extract resources | Physical materials. | Construction, ammunition and restoration. |
| Automate production | A sustained supply of products. | Defence and prepared expeditions. |
| Build and supply defences | Protected approaches and maintained territory. | Reliable production while exploring. |
| Scout regions | Useful locations, resources and encounters. | Expansion and selection of the next objective. |
| Search alien bases | Recovery of stolen power cores. | Regional power-plant restoration. |
| Restore a regional plant | Finite regional power and a new alien attack target. | A local factory, sustained defence and connected supply routes. |
| Add generators at a plant factory | Additional connected power capacity. | More automation and powered defensive/support equipment. |
| Repower tram stops | A useful transport network. | Travel and longer-distance logistics. |
| Find survivors | Practical equipment and capability upgrades. | More effective production, defence and construction. |
| Find machine artifacts | Modest bonuses to individual machines. | Improved factory performance. |
| Complete restoration projects | Working services and visible city recovery. | Further exploration and the wider restoration goal. |

The player grows from an empty-handed engineer into the operator of an expanding network of factories, defences, transport links and restored regions.
