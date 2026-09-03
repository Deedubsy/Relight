# RELIGHT — design concept

*2D top-down factory-automation game. Reclaim a dead city block by block; every block you take is front your factory has to feed.*

All numbers below are first-pass tuning targets. Where a number was checked against the throwaway front simulation (see section 25), it is marked **[sim]**. Everything else is a starting value for the prototype.

---

## 1. Name

**Relight.**

The verb is the whole game: you are not conquering the city, you are switching it back on. The title is also the name of the endgame event (section 16).

## 2. One-sentence hook

**"Factorio in a dead city: every block you take back adds front your factory has to feed, until you close the ring and the inside is yours for free."**

Refinement of the brief's line: it keeps "block by block" and "extends the front you have to feed", and adds the release valve (closing the ring) so the sentence contains both the cost and the reason to pay it.

## 3. Core fantasy

You are the engineer who came back. The city fell a long time ago; nobody is fighting over it, it is simply dark and something lives in the dark. You hold one lit block on the river with a generator, two turrets and a pile of rubble. You dig the rubble, turn it into wire and frames and bullets, and use those to power the next block's substation. When its streetlights come on, the block is yours, and the block after it wakes up and starts sending things at you.

The fantasy is civic, not military. Turrets are plumbing. The thing you are proud of is a district where every lamp works and no turret has fired in an hour because there is nothing dark left to fire at. The picture the game is built around: a black block on the map, a substation you just powered, and a line of streetlights coming on one after another down the street while the rot on the pavement burns away.

## 4. Top-down presentation

**Grid.** 32 px tiles, orthographic, no elevation. The city is a grid of **block cells**, each 32×32 tiles: a 24×24-tile buildable lot with a 4-tile street margin on every side, so two adjacent cells share an 8-tile-wide street. A standard city is 24×24 cells (768×768 tiles). Roughly 8–10 % of cells are **inert**: river, embankment, collapsed overpass. Inert cells cannot be built on and cannot hold rot.

**Two renderers, one data model.**
- *World view* (zoom 1.0× down to 0.2×): sprites on the tile grid. Machines are 1×1 to 8×8 tile sprites with a single idle/active animation each. Rubble is one tileset per rubble type with five density variants. Rot is a tinted overlay tileset with five density levels (0.05 steps of visible mottling) so "how bad is this block" is readable without a tooltip.
- *Map view* (below 0.2× or on hotkey): each block cell is a 24 px square. This is where the front is played from mid-game on. It is not a minimap; you can place claims, poles and blueprints from it.

**Lighting.** A low-resolution light map (one texel per tile) multiplied over the world layer. Lit tiles are full colour; unlit tiles are desaturated and darkened to ~25 %. No shadow casting. This one overlay carries the whole "lights coming on" payoff and the Shade rule (section 7) and is the only lighting tech in the game.

**Map-view encoding (fixed vocabulary, learned in the first 30 minutes).**
| Thing | How it is drawn |
|---|---|
| Held block | Warm amber square |
| Interior block | Brighter amber, thin white border |
| Contested block | Amber flicker, rot mottling burning off from the substation outward |
| Dark block | Navy square, mottled; brightness of the mottle = rot density |
| Frontage edge | Red-orange bar along the shared street, with **supply pips**: green ≥ 50 % of turret hoppers full, amber 10–50 %, red blinking = at least one empty hopper |
| Awake dark block | A ring around the square that fills as its bloom timer counts down |
| Belts | Thin lines with moving dashes; empty segments dashed grey; a jam draws a static red marker at the stop point |
| Trams | Moving rectangles on the street lines |
| Facilities | Silhouette icon (foundry chimney, arsenal star, turbine hall) |
| Survivors | A small lit-window icon on the block |

**HUD, always on:** `Front 14 edges · Interior 6 · Power 2.1 / 3.0 MW · Shot 412 · Shells 30 · Steel 1,840 · Cu 620`.

**Art scope.** One 32 px tile pass, ~40 machine sprites, 4 rubble tilesets × 5 variants, 1 rot overlay set, 3 enemy sprites with 4-direction walk, ~12 facility silhouettes, 1 light-map shader. Buildable by one artist and one technical artist over the project.

## 5. The front — exact rules

These are the rules the player learns by watching. Each is one sentence a player could say out loud after the first hour.

**Block states.** Every non-inert cell is **Dark**, **Contested** or **Held**. A Held cell with no Dark or Contested 4-neighbour is **Interior** (a display state, not a fourth state). Inert cells and the map boundary count as Held for adjacency. Adjacency is 4-neighbour only; diagonals never touch.

**Rot.** Every Dark cell has a rot density *d* in [0, 1]. Asleep cells (no Held/Contested neighbour) grow silently toward their district cap: *d* rises by `g · (dmax − d)` per second, so an untouched block reaches ~90 % of its cap in 34–67 minutes (outskirts fastest, civic slowest) **[sim: sanity]**. Rot cannot grow on a lit tile; a lit tile burns rot at 0.05/s. Only streets and lamp radii are lit: a Held lot is unlit buildable ground, burn-off (rule 4 below) clears the whole cell regardless, and rot never re-enters a Held block while its substation runs. "Lights out" means the substation has stopped; a lamp a crawler has eaten is an unlit gap (a shade corridor, section 7), not lights out.

**Waking.** A Dark cell with a Held or Contested 4-neighbour is **awake**. Awake cells bloom on a timer `T = 120 / (0.5 + d)` seconds (240 s at d = 0, 96 s at d = 0.75). A bloom spawns enemies at the cell's street edges facing the player and drops *d* to `max(0.05, 0.9·d)`. Between blooms *d* regrows, so each awake cell settles at a district-specific steady state (section 7).

**How a block becomes Held.**
1. Every city block has a pre-existing 3×3 **substation** somewhere on its lot. The claim tool highlights it. Tooltip: `Claim — 10 wire, 5 frames · rot 31 % · front +2 · closes 1`.
2. You connect it with power poles to a powered substation you already hold (poles have reach 8; substations never auto-link across streets). The instant it draws power the cell becomes **Contested**.
3. Contested does three things at once: the cell's pre-existing streetlights come on (some are broken — those gaps are yours to fill with lamps); the rot begins burning off from the lamps outward; and the cell fires a **wake bloom** of twice its normal size, immediately.
4. Burn-off takes `20 + 60·d` seconds (38 s at d = 0.3, 80 s at d = 1.0). When it finishes, the cell is **Held**: you can build on its lot, its rubble patches are minable, and its facility or survivors (if any) activate.

**Frontage.** The front is a count: `F = number of (Held, Dark-or-Contested) 4-adjacent pairs`. Each pair is one 32-tile street you must cover. The HUD shows F. A block's claim tooltip shows how F will change.

**What one frontage edge costs.**
| Item | Per edge (32 tiles) | Per tile |
|---|---|---|
| Gun turrets (range 9) | 2 | 1 per 16 tiles |
| Lamps to plug broken streetlights (avg) | 3 × 5 kW | — |
| Ammo, residential steady state | 1.3 magazines/min **[sim: E3-block]** | 0.04 mag/min |
| Ammo, industrial steady state | 1.8 magazines/min **[sim: E3-block]** | 0.06 mag/min |
| Ammo, outskirts steady state | 4.3 mag/min + 2 shells/min | 0.13 mag/min |
| Substation draw, Contested or Held block with any dark neighbour | 100 kW | — |
| Substation draw, interior block | 20 kW | — |

A held block touching three dark blocks costs three edges of turrets and ammo but only one substation. That asymmetry is the reason compact shapes are cheap. Measured over five hours of compact play, wake tails included, a residential edge averages 1.5 mag/min and a civic edge 0.9 **[sim: E3-compact-shade0.3-hulk0.5]**.

**Enclosure.** When the last Dark/Contested neighbour of a Held cell becomes Held, the cell is Interior: its substation drops to 20 kW, its turrets have nothing to shoot at and can be picked up, and its streets are free ground. Belts you laid along its front streets stay and become trunk lines.

**What a wave looks like.** Not a wave. Each awake cell blooms on its own timer; timers are interleaved by the spawner so no two adjacent cells bloom within 10 s of each other. A bloom is 4 + 36·d crawlers arriving in a 15-second stream at the street edge, plus shades and a hulk above the density thresholds in section 7; a wake bloom is double that, capped at 40 crawlers after doubling (a normal bloom never reaches the cap; it trims a well's wake bloom from 198 to 170 rounds **[sim: wake-cap]**). From the player's seat a front of 20 edges is a steady patter of small fights, one every few seconds somewhere on the line, not a siren and a horde.

**How a block falls.** A Held block falls only when its substation stops. Four ways: brownout (grid demand > supply for 20 s; machines shed first, then substations in order of most-dark-neighbours first **[sim: E2-h500-15pct-shed=machines-first]**), a **shade** reaching the substation (disables it for 30 s per shade, stacking), a **hulk** smashing it (600 HP, it walks through barricades and turrets to get there), or **crawlers** reaching it unshot: 40 arrivals stop it (the counter is per block across all its edges, so a corner block with two dark edges fails faster than a block with one), which on a residential edge whose hoppers have run dry is 5–8 minutes after the pip went red **[sim: E1-starve-substation-N40]**. Lights out → rot creeps in from every dark edge at 1 tile per 3 s → **90 s after the substation stops** the block is Dark again at d = 0.3, wherever the substation sits on the lot (the rule; the sim's fall timings use it **[sim: E8-fall16]**). With the 60 s refeed reset that leaves a 30 s window in which running ammo to the failing block saves it, a play we want. Machines on it are mothballed (greyed, contents kept). Belts and poles are never destroyed. Neighbouring Held blocks gain one frontage edge each, and while a power shortfall lasts that is a cascade: each shed front substation adds 80 kW of new front draw next door (160 kW when the cascade was measured at the old 200/40 kW draw), so a 15 % shortfall for 10 minutes cost 3 blocks under substation-first shedding and a 25 % shortfall cost 9, about one knock-on loss per two direct sheds; shedding machines first cost 0 and 2 **[sim: E2-h500-15pct-doc, E2-h500-25pct-doc, E2-h500-25pct-shed=machines-first]**. Nothing cascades once supply is restored: no run under either shedding rule lost a block outside the shortfall window. **Retake** = re-power the substation → wake bloom → burn-off → everything un-greys. Rot is data, not damage; the punishment is the front you just re-grew, not lost buildings.

**Encircled dark blocks.** A Dark cell whose 4 neighbours are all Held keeps blooming but has no growth input from other dark cells, so it decays to its own cap alone; it is never a threat to more than four edges and you close it whenever you have the wire.

**Rot wells.** 3–6 cells per map (always visible on the map as a darker throb) have `dmax + 0.3` and 4× growth, and lift the cap of cells within 3 blocks. A well dies when all four of its neighbours are Held and it has bloomed for five minutes with no dark neighbour to feed it. Killing a well is the mid-game milestone (section 15).

**Outskirt cells.** No substation and no streetlights. To hold one you place a craftable **Substation** (Electricians survivor, section 8) and your own lamps. Same rules otherwise; the district cap is 1.0, so these are the hardest edges in the game.

**Start.** The HQ always sits against an inert feature (river) so the first enclosure is a T-shape of three claims, not a ring of eight.

## 6. The payoff moment

You have just linked a pole to the substation of a residential block north of the HQ. The map square flickers amber. In world view the substation hums, and the block's streetlights come on in sequence down the street at three per second, each one pushing back a circle of desaturation. Rot mottling on the pavement fades outward from every lamp. The wake bloom arrives at the same moment — eighteen crawlers streaming across the north street into your two turrets — so the lamps come on *while* the guns are firing, and the crawlers that reach a lit tile are visible for the first time. Forty seconds later the mottling is gone, the "Contested" flicker stops, and the lot turns from navy to amber on the map.

Ten minutes later you power the block to the north-east, the HQ's last dark neighbour. The HQ square gets a thin white border. Its two north turrets show "no targets" and you pick them up. The 8-tile street between the HQ and the new block, which for the last forty minutes had a turret line and a lamp line along it, is now just a street with a belt on it. That is the moment the game is selling: not the fight, the *afterwards*.

## 7. Threat

**Model chosen: creeping spread, not hordes.** Threat is a field (rot density per block) that the player's lights suppress locally and the player's expansion wakes. It is never on a clock. The rejected horde/timed-wave model is in section 23.

**The Rot.** Per-block density *d*. District caps and growth rates:

| District | Rubble type | dmax | g (per s) | Steady-state awake *d* | Ammo per awake block |
|---|---|---|---|---|---|
| Civic (halls, hospital, library) | stone/concrete | 0.30 | 0.0004 | 0.13 | 0.9 mag/min |
| Residential | copper | 0.45 | 0.0005 | 0.21 | 1.3 mag/min |
| Rail yard | coal | 0.45 | 0.0005 | 0.21 | 1.3 mag/min |
| Industrial | steel | 0.60 | 0.0006 | 0.29 | 1.8 mag/min |
| Outskirts | none (deposits) | 1.00 | 0.0008 | 0.50 | 4.3 mag/min + 2 shells/min |
| Within 3 blocks of a well | as district | +0.30 | ×4 | 0.54–0.78 | 5.3–8.7 mag/min + 2.1–2.6 shells/min |

Depth multiplier: `dmax × (1 + 0.3 · distance_from_start / 20)`, capped at 1.0, so the same district is ~30 % worse on the far side of the city. Steady-state values and the ammo column are the equilibrium of 10 % drop per bloom against regrowth over one timer, stepped for an isolated awake block **[sim: E3-block]**, and the densities match the sim's observed mean awake densities of 0.24–0.34 across policies **[sim: E7]**.

**Three enemies, each a logistics failure made visible.**

| | Crawler | Shade | Hulk |
|---|---|---|---|
| Footprint / speed | 1 tile, 3 t/s | 1 tile, 2 t/s | 3×3, 0.8 t/s |
| HP | 12 (3 Shot rounds) | 40 (10 Shot rounds) | 600 (Shot does 1, Shell does 60 → 10 shells) |
| Appears | every bloom | 1 per 8 crawlers when d ≥ 0.3 | 40 % of blooms when d ≥ 0.5 |
| Targets | nearest lamp, then turret, then the substation (40 unshot arrivals stop it) | substation (disables 30 s each, stacking) | barricade → turret → substation |
| Special | none | **untargetable on unlit tiles** | walks through barricades at 1 per 4 s |
| Punishes | ammo throughput and hopper distribution: an edge left unfed loses its block in 5–8 min **[sim: E1-starve-substation-N40]** | light coverage: a single unlit gap in a street is a corridor to the substation | frontage length: one long thin front has nowhere with enough Cannons |
| The player sees | red supply pip, hoppers empty, crawlers eating lamps | a block goes dark with full hoppers and no visible fight | a 3×3 shape walking through a turret line that is plinking at it |

Wake blooms cap at 40 crawlers so the ammo maths stays bounded: the biggest single fight in the game is 40 crawlers and 5 shades, 170 rounds against a 100-round hopper **[sim: wake-cap]**. Shades only show the player a corridor, they never bypass a lit street; the fix is always "put a lamp there". Hulks force Cannons (Arsenal, section 8) and Shells (coal in the recipe), which is the only reason the coal chain touches the front.

**What threat scaling feels like.** Civic blocks are the soft way in; residential is the default; industrial is where the steel is and where shades become constant (deep industrial blocks sit at d ≥ 0.3 and shade on every bloom; the first shades come from residential blocks that have slept past d = 0.3; hulks belong to wells and outskirts, 25–47 % of their blooms against 4 % of deep industrial **[sim: E3-compact-shade0.3-hulk0.5, E3-spike-shade0.3-hulk0.5]**); outskirts are where the deposits are and they cost 3–4× a residential edge to hold; wells are the boss blocks. The sim's spike policy (straight line toward a target) paid 3.2× the ammo of the compact policy for the same 34 blocks at five hours on the scattered map (14,152 vs 4,414 magazines; 1.6× on open ground with no inert cells, 17,052 vs 10,679) **[sim: E6-scatter, E6-noscatter]**. That ratio is the number the whole game is tuned around: shape is worth three times your ammo budget where inert cells give compact play free walls (60 % of it on open ground).

## 8. Found tech

There is no research menu. Everything that is not in your starting toolbar is unlocked by reaching a **facility** or a **survivor group**, both procedurally placed under distance and direction constraints. Scouting: from any Held block you see the skyline silhouettes of facilities up to 6 blocks away; a block's contents (survivors, rubble type, well) are revealed when any 4-neighbour is Held.

**Facilities.** Restored by paying a fixed kit at the building (materials by belt or tram), then they run like a machine. Some cannot be crafted at all.

| Facility | Footprint | Placement rule | Gives | Restore kit |
|---|---|---|---|---|
| Depot / HQ | 6×6 | start block, on the river | global stock input, starting toolbar | — |
| Foundry | 8×8 | industrial, 5–9 blocks from start | iron/copper ore → steel/copper at 4/s; needed for deposits | 60 frames, 40 wire |
| Arsenal | 6×6 | 6–12 blocks, never same octant as Foundry | Cannon, Shell recipe | 100 frames, 50 boards |
| Tram depot | 6×8 | 5–10 blocks, on a street with rails | Tram, Tram stop, Track | 80 frames, 40 boards |
| Turbine hall | 8×6 | riverbank, 8–14 blocks | 5 MW, no fuel | 200 concrete, 100 frames |
| Refinery | 10×10 | adjacent to the oil field, outskirts | Crude → Fuel + Polymer | 300 concrete, 150 frames, 60 boards |
| Transformer yard ×5 | 6×6 | one per district, must be Interior to work | endgame node (section 16) | 100 concrete each |
| Power station | 16×16 | far edge, ≥ 15 blocks, opposite octant to start | the Relight | section 16 |

Foundry, Arsenal, Tram depot, Turbine hall, Refinery and Power station are **uncraftable**. Losing the block they sit on mothballs them; they cannot be destroyed.

**Survivors.** Found on a block; when it becomes Held they move into the HQ with a one-line "we're in" and their recipes appear on the toolbar. They are never fed, housed, counted or lost.

| Survivors | Distance | Unlocks |
|---|---|---|
| Electricians | ≤ 3 blocks | Floodlight, Big pole, craftable Substation |
| Concrete crew | ≤ 4 | Mixer, Concrete, Barricade |
| Gunsmith | 4–8 | Turret hopper accepts belt input directly (no inserter) |
| Rail crew | 5–10 (near Tram depot) | lay Track, Freight tram |
| Foreman | 6–10 | Line truck, Front kits, auto-restore on claim |
| Chemist (optional) | 10+, near oil | Fuel and Polymer recipes at the Refinery |
| Lamplighters (optional) | anywhere | Arc lamp (1×1, radius 6, 12 kW) |
| Surveyors (optional) | anywhere | reveal district type of every block on the map |

**Distribution rules.** Near (≤ 4 blocks): Electricians, Concrete crew, Foundry direction hinted by skyline. Mid (5–12): Foundry, Arsenal, Tram depot, Turbine hall, Gunsmith, Rail crew, Foreman. Far (≥ 15): Power station, oil field and Refinery, Chemist. Optional groups are placed in dead-end pockets so a player who never turns that way still finishes. The generator guarantees that Foundry and Arsenal are in different directions so the first big decision (steel first or Cannons first) is real.

## 9. Why the front creates interesting layouts

Every problem below is a decision with a visible cost on the HUD, not a puzzle with an answer.

1. **Shape vs reach.** The Foundry is 7 blocks north-east. A straight corridor of 7 blocks has 16 frontage edges; a 3×3 blob plus a 4-block spur has 14 and gets you a 3-block interior. The sim says the corridor costs 2.2× the ammo on the scattered map, where inert cells wall the blob (~1.3× on open ground) **[sim: E6-scatter, E6-noscatter, at the 5-minute cadence]**, but the blob is 40 minutes slower to the Foundry. *Cost paid:* time vs ammo.
2. **Which edge to starve.** Twenty edges, one assembler making 20 mag/min, industrial edges at 2.2 and civic at 0.9. A single belt loop that visits every turret feeds them in order; the last turret on the loop is the one that empties first. Where you start the loop decides which block you are willing to lose. *Cost paid:* ammo distribution vs a second assembler.
3. **Pole routing vs shade gaps.** Poles reach 8 tiles, lamps light radius 4, streetlights are broken 20 % of the time. A pole line that hugs the lot edge leaves an unlit strip on the far pavement; a shade walks it. Lighting the whole 8-wide street costs 8 lamps (40 kW) per edge; lighting only your side costs 4 and leaves a corridor. *Cost paid:* power vs a substation you will lose at 3 am.
4. **Interior trunk vs front belt.** The belt you lay along a front street to feed turrets is in exactly the place the trunk line will want to be when the block goes interior. Building it as a proper 2-lane trunk now costs double; building it cheap means ripping it later. *Cost paid:* steel now vs a rebuild later.
5. **Enclosing a well.** A well needs all four neighbours Held. Three of them are outskirts at d ≈ 0.75, each 7.7 mag/min plus shells. Taking the fourth side closes the well and removes ~30 mag/min of demand for good, but the 20 minutes before that is the worst load in the mid-game. *Cost paid:* a temporary ammo peak vs a permanent front.
6. **Where the Cannons go.** Hulks come from d ≥ 0.5 blocks only. A Cannon (3×3, range 12) covers one and a half edges. A front with 4 outskirt edges needs 3 Cannons and a shell line; a front that avoids outskirts entirely needs none but never reaches the coal seam. *Cost paid:* coal for shells vs coal for power.
7. **Retreat as a layout tool.** Un-claiming is not possible, but letting a spur block brown out on purpose (cutting its pole) converts a 3-edge salient into a 1-edge notch; you lose the mothballed machines until you retake it. *Cost paid:* a temporary loss of production vs two edges of turrets.

Each of these is the same rule (F counts pairs; each pair costs supply) seen from a different building.

## 10. Core gameplay loop

One loop at three time scales. All three are the same verbs: dig, make, feed, light.

**Minute loop (world view).** A supply pip on the map goes amber. You look: the belt to that turret pair is dashed grey past a splitter. You fix the splitter priority or add an inserter. Pip goes green. You go back to the excavator you were placing.

**Ten-minute loop (map view).** Front is 11 edges, ammo output is 24 mag/min against 16 mag/min demand, power is 1.9 of 2.4 MW. You pick the next block: the claim tool says `front +1 · closes 2`. You run a pole, watch the burn-off, pick up two turrets from the block that just went interior and drop them on the new edge. The Line truck (once you have it) does the drop for you.

**Hour loop.** A facility silhouette is 6 blocks away. You plan a shape toward it that keeps F under what your ammo line can feed, build the second assembler and the belt trunk that the shape will need, and take blocks in the order that closes interiors as you go. When the facility comes online its recipes change what the next hour's shape is for.

The loop never changes verbs. It changes what one edge costs (district, depth, wells) and what one held block gives (rubble type, facility, survivors).

## 11. First hour in three windows

**0–10 min.** Map opens on the HQ block: river to the south, a 6×6 Depot, one Generator (300 kW) with 40 coal, two Gun turrets on the north edge with 20 magazines in stock (enough: with the ammo line running from minute 10 the HQ never falls on 200 rounds **[sim: E1-start-min]**), a small steel-rubble patch, a copper patch and a coal patch of ~3,000 units on the lot, a starting stock of 200 steel, 100 copper, 50 stone. Three dark neighbours: rail yard west (coal + steel rubble), residential east (copper), civic north (Electricians, visible as a lit window). Front = 3. At about 3 minutes the north block blooms: 9 crawlers, the turrets fire, 3 magazines gone. You place 2 Excavators (3×3) on the steel patch, a belt, an Assembler (3×3) set to Shot magazine, a belt to the turret hoppers, and at minute 6 a second Generator with an Excavator on the coal patch: one Generator alone browns out at minute 8 and the 40 coal in hand is gone by minute 11 without the patch **[sim: E4h-literal-1gen, E4-literal]**. By minute 10 ammo is automated at 20 mag/min and you have hand-fed the west and east turrets twice each.

**10–30 min.** You claim east (residential, d ≈ 0.22): 10 wire, 5 frames, wake bloom of 22 crawlers, 40-second burn-off, then a lot with 90k copper rubble. Front goes 3 → 4. You put an Excavator on the copper, the wire recipe on a second Assembler, and a third Generator (hour-one draw peaks at 0.98 MW, two-thirds of it machines, and the HQ with its three dark neighbours is still the first substation a brownout sheds **[sim: E4h-literal-1gen, E4h-gen@0,6,15]**). You claim west (rail yard): front 4 → 5 and coal rubble feeds the Generators by belt. The HQ's own patch mines out at about minute 106 at one Excavator, so west's coal is wanted in hour two rather than by minute 30; a ~700-unit patch would mine out at minute 30, and the size is open (CALIBRATION_REPORT_2.md). Both times you watched the ring on the neighbouring squares fill and the crawlers come at the moment the lamps lit.

**30–60 min.** North is the cap. Claiming it puts the HQ at 4 Held/inert neighbours: the HQ square gets its white border at about minute 40, the two south-facing turrets on east and west get picked up, and the Electricians walk into the Depot: Floodlight, Big pole, Substation appear on the toolbar. Front is 7. The first shade comes with a residential bloom at about minute 47, from a block that slept past d = 0.3, and is the lamp rule's first lesson **[sim: E8-first-shade-compact]**. You have 3 Assemblers (one on Shot: hour-one demand is 8 mag/min against its 20 **[sim: E5-compact-1-shot-asm-h1]**), 4 Generators (three carry the front to minute 45; the fourth comes with the fifth Excavator and third Assembler, whose 980 kW peak three cannot hold; no brownout, coal never runs out **[sim: E4h-gen@0,6,15, E4h-gen@0,6,15,25]**), 5 Excavators, 8 turrets, and the first thing you say out loud is "if I take north-east and north-west next, east and west go interior too." You are now playing the game.

## 12. Resources

**Raws (5, all finite except river power).**
| Raw | Source | Notes |
|---|---|---|
| Steel | industrial rubble (direct) · iron ore deposit → Foundry | rubble tiles hold 300 units; a block holds 250–350 rubble tiles (75–105k) |
| Copper | residential rubble (direct) · copper ore deposit → Foundry | same |
| Stone | civic rubble (direct) · quarry | Mixer turns 2 stone → 1 concrete |
| Coal | rail-yard rubble (direct, ~30k per block) · coal seam (outskirts) | 4 MJ each; Shell recipe |
| Crude | oil field (outskirts, late) → Pumpjack → Crude drum item | no fluid system; Refinery → Fuel + Polymer |

Mined-out rubble tiles become plain ground you can build on. Deposits are large (iron mine ~2M, coal seam ~1.5M) and outskirts, so they are held at outskirt front cost.

**Intermediates (8).** Wire (1 Cu → 2, 1 s), Frame (2 steel → 1, 2 s), Concrete (2 stone → 1, Mixer, 2 s), Board (3 wire + 1 steel → 1, 4 s), Shot magazine (2 steel + 1 Cu → 1 magazine of 10 rounds, 3 s), Shell (2 steel + 1 coal → 1, 3 s, Arsenal recipe), Fuel (1 Crude → 4, Refinery), Polymer (2 Crude → 1, Refinery). Nothing is more than two steps from a raw.

**Ammo chain.** Steel rubble → Excavator → belt → Assembler (Shot) → belt → turret hopper (50 rounds = 5 magazines). One Assembler = 20 magazines/min = 40 steel + 20 Cu per minute = 1.3 Excavators on steel and 0.7 on copper. Shells: steel + coal → Arsenal-recipe Assembler → Cannon hopper (20 shells).

**Counts by phase.**
| | Early (0–3 h) | Mid (3–12 h) | Late (12–25 h) |
|---|---|---|---|
| Raws in use | steel, copper, coal, stone | + iron/copper ore via Foundry | + crude |
| Intermediates in use | wire, frame, shot | + concrete, board, shell | + fuel, polymer |
| Ammo demand (compact play) | 8–48 mag/min **[sim: E5-compact-base]** | 60–150 mag/min + 10–20 shells/min | 150–250 mag/min + 30 shells/min |
| Power | 1.0–3.1 MW **[sim: E4h-gen@0,6,15,25, E2-demand-half]** | 3.1–5.5 MW **[sim: E2-demand-half-25h]** | 5.5–8.6 MW **[sim: E2-demand-half-25h]**, then 40 MW for the Relight |

## 13. Machines with tile footprints

| Machine | Tiles | Power | Rate / notes | Unlock |
|---|---|---|---|---|
| Excavator | 3×3 | 60 kW | mines a 5×5 area under it at 0.5/s | start |
| Assembler | 3×3 | 100 kW | any intermediate recipe | start |
| Generator | 2×2 | — | 300 kW from coal or fuel | start |
| Mixer | 2×2 | 150 kW | stone → concrete | Concrete crew |
| Gun turret | 2×2 | none | range 9, 5 rounds/s, 50-round hopper | start |
| Cannon | 3×3 | none | range 12, 1 shell/2 s, 20-shell hopper | Arsenal |
| Lamp | 1×1 | 5 kW | lights radius 4 | start |
| Floodlight | 2×2 | 40 kW | 12-tile cone, rotatable | Electricians |
| Barricade | 1×1 | none | 200 HP, blocks crawlers, slows hulks | Concrete crew |
| Pole | 1×1 | — | reach 8, supplies 7×7 | start |
| Big pole | 2×2 | — | reach 12, supplies 3×3 | Electricians |
| Belt / Fast belt | 1×1 | — | 8/s · 16/s | start / Foundry |
| Inserter | 1×1 | 10 kW | 1 item/s | start |
| Splitter | 1×2 | — | with priority side | start |
| Underground pair | 1×1 ×2 | — | span 4 | start |
| Chest | 1×1 | — | 400 items | start |
| Depot input | 2×2 | — | belt → global stock | start |
| Track | 1×1 | — | streets only | Rail crew |
| Tram stop | 2×3 | 20 kW | loads/unloads via up to 6 inserters (one per tile of its long sides): a 600-item Freight tram fills in 100 s, a Tram in 34 s | Tram depot |
| Tram / Freight tram | 1×3 / 1×9 | — | 200 / 600 items, 8 t/s, two stops | Tram depot / Rail crew |
| Line truck garage | 4×4 | 50 kW | holds 4 front kits, re-fronts on claim | Foreman |
| Substation (craftable) | 3×3 | — | 20 frames + 20 wire + 10 boards | Electricians |
| Pumpjack | 3×3 | 200 kW | Crude drums at 0.5/s | Refinery found |
| Foundry / Arsenal / Turbine hall / Refinery / Transformer yard / Power station | 8×8 / 6×6 / 8×6 / 10×10 / 6×6 / 16×16 | see §8 | facilities | found |

Twenty-six placeable things. A Factorio player recognises twenty of them on sight.

## 14. Logistics

**Belts.** Standard two-lane belts, 8/s and 16/s, inserters at 1/s, splitters with priority, undergrounds of span 4. Belts run on lots and on streets; streets are 8 wide, which is room for two belts, a track and a lamp line.

**Power.** Poles link substations; each substation powers its whole block cell (lot and its four half-streets) so you never pole individual machines inside a held block. Grid is one pool; brownout sheds machines first (an assembler line is 220 kW and stopping it loses output, not blocks), then substations most-dark-neighbours first, so the front dims before the interior does; substation-first shedding cost 3 blocks in a 15 % shortfall where machines-first cost none **[sim: E2-h500-15pct-doc, E2-h500-15pct-shed=machines-first]**. Brownout shedding order: Shot and recipe Assemblers first, then other machines, Excavators feeding Generators last, substations only after all machines; substations then in most-dark-neighbours order.

**Global stock.** Anything belted into a Depot input joins one global pool. Claims, blueprints and hand-placement draw from it. Hand-collecting from a chest is slow (one stack per 2 s) so belting into the Depot is the first automation lesson.

**Trams (the one bulk system).** Track on streets only. A tram shuttles between exactly two stops, loads and unloads by the stop's inserters (up to 6, section 13), 200 items at 8 t/s. Freight tram (Rail crew) is 600, a 100-second load. There is no signalling, no junctions, no schedule: one tram per track. Long distances are solved by placing more tracks in parallel on 8-wide streets. This is bulk transport with the Factorio train fantasy but without the signalling chapter.

**Line truck and front kits (Foreman).** A front kit is a saved 32×4 strip: 2 turrets, lamps, a belt stub, a pole. The truck holds four kits and, when a block goes Held, lays the kit on every new frontage edge and picks the turrets up from every edge that just went interior, using global stock. This is the tedium fix for re-fronting and it is a found unlock so the player has done it by hand for ~4 hours first.

**Blueprints and copy-paste** from minute one.

**Not present:** fluids, robots, trains with signals, item quality, circuits. See section 22.

## 15. Progression

Progression is territory. There is no tech tree to read, so the pull is always a silhouette on the horizon and a number on the HUD.

**Early, 0–3 h (12–25 blocks).** Ammo automated, first interior at ~40 min, Electricians. Push toward the Foundry silhouette; take the industrial blocks around it for steel rubble. The first shade comes from a residential block at about 47 min in compact play (17 min on a straight push) and teaches the lamp rule; the industrial edge is where shades become constant **[sim: E8-first-shade]**. Around hour 2 the first iron deposit is in reach on the outskirts and its edge is brutal; most players wait. End state: two assembler lines, 4.5 MW **[sim: E2-demand]**, 15–20 edges, one blueprint for the front strip.

**Mid, 3–12 h (25–100 blocks).** Arsenal (Cannons, shells: coal now matters twice). Tram depot and Rail crew: the first tram line hauls steel from the Foundry across the interior. Turbine hall on the riverbank: 5 MW without coal, against a demand of 5.5 MW at 4 h and 7.6 MW at 10 h **[sim: E2-demand-25h]**; whether the rail-yard coal runs out here is not simulated (no rubble-depletion model, section 25). Foreman: the Line truck takes re-fronting away just as F passes 30. First well killed somewhere in hours 6–9: the map shows its throb stop and the three surrounding blocks drop to their district steady state. The mid-game is about half the playthrough and it is where shape play lives: closing districts, deciding whether to skirt or hold the outskirt deposits.

**Late, 12–25 h (100–200 blocks).** Oil field and Refinery: Fuel (for Generators far from coal) and Polymer (endgame only). Power station found at ≥ 15 blocks. The five Transformer yards are each in a different district and must be interior, so the late game is five enclosure puzzles across the map, with the outskirts and the remaining wells between them. Coal seam and iron mine held at outskirt cost. Front peaks at 50–70 edges even with good shape.

**Endgame, 20–40 h.** Section 16.

## 16. Endgame megaproject: the Relight

**Goal.** Restore the Power station and switch the whole city back on.

**Steps.**
1. Hold the Power station block (far edge, always an outskirts-adjacent industrial block, d ≈ 0.6–0.8 with a well within 3 blocks in 80 % of seeds).
2. Deliver the restore kit by tram or belt: 2,000 concrete, 1,500 frames, 800 boards, 500 polymer. At mid-game rates that is ~3 hours of a dedicated line.
3. Bring all five Transformer yards interior (they each show a red "exposed" flag until then).
4. Feed the station 20 coal/s or 5 fuel/s and start it. **Spin-up is a 10-minute hold.** For those 10 minutes every remaining well surges ×3 and every awake block blooms at its wake size. This is the only time the game is a siege, and it is the last thing that happens. It is survivable with preparation: banking magazines over the last hour is the intended play, and a player who has stocked about 2× an hour's production over the final hour should lose fewer than 5 blocks. Unprepared, with the wells' ×3 alone (the wake-size blooms are not in the sim, so this is the floor), the ten minutes demand 1.6× the ammo of the hour before them, peak minutes 2.3×, and cost 18–29 blocks in twenty minutes when production only matches demand and nothing is banked, against 2–4 without the surge **[sim: E11-relight-hold]**; §25 item 5 has the figures and the re-run that has to confirm the banked number before the world view builds the hold.
5. At 10:00 the station hits 40 MW and the Relight sweeps outward from it at one block per 0.4 s. Every dark block's streetlights come on whether it has a substation link or not, rot burns to zero everywhere, wells die. Two minutes of watching the map turn from navy to amber, one district at a time. Then the score screen.

**After.** Free play continues with no rot growth anywhere. The front is gone; you can build across the whole city. Optimisation targets (section 17) keep score.

## 17. Replayability

- **Procedural city.** District layout, river course, inert cells, well positions, facility and survivor positions all reroll. The default generator scatters 8–10 % inert cells across the city (the river aside), and every headline ratio in this document refers to that scattered map; open-ground figures appear only in parentheses. The generator uses rejection sampling against a validator: start on inert edge, Foundry 5–9, Arsenal in a different octant, Power station ≥ 15 opposite, five districts each with a yard, ≤ 6 wells, every facility reachable without crossing more than two outskirt edges.
- **Strategy space.** Compact vs spike vs skirting: the sim shows a 3.2× ammo spread between compact and spike at equal territory on the scattered map (1.6× on open ground); "always take the quietest block" costs 1.31× compact on the scattered map, because it never uses the free walls (0.56× on open ground) **[sim: E6-scatter, E6-noscatter]**. Steel-first (Foundry) vs Cannons-first (Arsenal). Coal-for-power vs coal-for-shells. Kill wells early vs wall them off.
- **Scenario presets (5).** *River city* (default); *Ring road* (an inert ring at radius 8 makes a natural first enclosure); *One giant industrial district* (steel everywhere, shades everywhere); *No outskirts* (deposits are inside the city, wells are more numerous); *Canals* (many inert strips, enclosure is cheap, fronts are short and dense).
- **Optimisation targets** on the score screen and a global stats panel: blocks per hour, magazines per frontage-edge-hour, time to first enclosure, coal per MW, Relight time. No loot, no unlock persistence between runs, no RPG layer.

## 18. Example territory at 10 minutes, 5 hours, 25 hours

Legend: `H` held, `I` interior, `C` contested, `.` dark, `~` river / inert, `Q` HQ, `F` Foundry, `A` Arsenal, `T` Tram depot, `U` Turbine hall, `R` Refinery, `P` Power station, `Y` transformer yard, `W` rot well (`w` = dead well), `E`/`G`/`K`/`M` survivors (Electricians, Gunsmith, Rail crew, Foreman). Map view: one character = one block cell.

**10 minutes (world view of the HQ lot, north half, 24 tiles wide).** Front = 3. Rot mottling on the far side of every street.

```
 north street (residential, d 0.22, awake, ring 40 % full)
 . . . . . . . . . . . . . . . . . . . . . . . .
 L L L L L L L L L L L L L L L L L L . . L L L L   <- streetlights (2 broken on the right)
 - - - T T - - - - - - - - - - - T T - - - - - -   <- 2 gun turrets, hoppers 5/5 and 2/5
 - - - ^ - - - - - - - - - - - - ^ - - - - - - -   <- belt stubs into hoppers
 =====================================< A A A     <- ammo belt from Assembler (Shot)
 x x x x x x . . . . . . . . . . . . . A A A
 x x x x x x . . . . . D D D D D D . . . A A A     <- x = steel rubble w/ Excavator, D = Depot 6x6
 x X X X x x . . . . . D D D D D D . . . . . .
 x X X X x x . . . . . D D D D D D . . G G . .     <- G = Generator 2x2 (coal by hand, 40 left)
 c c c . . . . . . . . D D D D D D . . . . . .     <- c = copper rubble (no excavator yet)
 (south half: more rubble, river embankment)
```

**5 hours (map view, south-centre 14×9 blocks; the drawing shows 52 blocks held, front 24, interior 36, with 5 cells drawn as front that are interior by the 4-neighbour rule; the sim at 5 h holds 34, front 24, interior 12; a claim every ~5 minutes after hour one would match the drawing's count [sim: E9-claim-cadence]).** Compact policy [sim: E7-compact, §18 recount]. Ammo demand ≈ 61 mag/min from 3–4 assemblers **[sim: E5-compact-base]**; 14 shells/min from the Arsenal for the two outskirt edges near the well.

```
 col: 6 7 8 9 0 1 2 3 4 5 6 7 8 9
 12   . . . . . . W . . . . . . .    <- well at (12,8) is 4 rows further north; this is its influence
 13   . . . . . H H H . . . . . .    <- northern spur toward the Foundry
 14   . . . . H H F H H . . . . .    <- Foundry held (industrial, shades on all three dark edges)
 15   . . . H H I I I H H . . . .
 16   . . . H I I I I I H . . . .
 17   . . H H I I I I I I H H . .    <- Arsenal is off-map to the east, not yet reached
 18   . . H I I E I I I I I H . .    <- Electricians block, long interior
 19   . . H I I I I Q I I I H . .    <- HQ, interior since minute 40
 20   ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~    <- river
```

**25 hours (map view, full 24×22 city; the drawing shows 313 blocks held, front 67, interior 270, with 7 cells drawn as front that are interior; the sim at 25 h holds 184, front 31, interior 155 [sim: E2-demand-25h, §18 recount]; a claim every ~5 minutes after hour one would match the drawing's count [sim: E9-claim-cadence]; four of five wells dead, Power station under restoration).**

```
 col: 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3
  0   . . . . . . . . . . . . P H H . . . . . . . . .   <- Power station block held, spin-up not started
  1   . . w . . . . . . . . H H H H H . . . . . . . .
  2   . . . . . . . . . . H H I I I H H . . . . W . .   <- live well (21,3) walled by 3 held sides
  3   . . . . . . . . . H I I I Y I I I H . . . . . .   <- outskirts yard (interior)
  4   . . . . . . . . H I I I I I I I I I H . . . . .
  5   . . . . . . . H I I I I I I I I I I I H . . . .
  6   . . . . . . H I I I I I I I I I I I I I H . . .
  7   . . . . . H I I I Y I I I I I I I I I I H . . .   <- industrial yard
  8   . . . . H I I I I I I I w I I I I I I I H . . .   <- dead well (12,8)
  9   . . . H I I I I I I I I I I I I I I I I I H . .
 10   . . H I I I I I I I I I I I I I I I I I I H . .
 11   . . H I I I R I I I I I I I I I I I A I I H . .   <- Refinery (oil outskirts pocket), Arsenal
 12   . . H I I I I I I I I I Y I I I I I I I I H . .   <- residential yard
 13   . . H I I I I I I I I I I I I I I I I I I H . .
 14   . . H I I I I I I F I I I I I I I I I I H . . .
 15   . . H I I I I I I I I I I I I I I I I H . . . .
 16   . . H I I I I I I I I I I I I I I I H . . . . .
 17   . . H I I I I I I I T I I I Y I I H . . . . . .   <- civic yard
 18   . w H I I I I I I I I I I I I I H . . . . W . .   <- live well (22,19) never taken; costs nothing
 19   . . H I I I I I I I I I I I I I H U H . . . . .   <- Turbine hall on the riverbank
 20   . . H I I I I E I I I Q I I I I I H . . . . . .   <- Y5 (rail yard) is at (4,20), off this row's edge
 21   ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~
```

The 25-hour shape is a fat blob with two spurs: one north to the Power station, one south-east to the Turbine hall. Note what is *not* held: the whole east strip and the far west. The live well at (22,19) has been ignored for 20 hours because nothing the player needs is behind it, which is the ignore test (section 19) working as intended.

## 19. Fun checks

**Hands test (what is the player physically doing most of the time?).** Placing belts and machines on a lot in world view, and clicking blocks in map view. Measured by the loop in section 10: roughly 70 % of input is belt/inserter/machine placement inside held blocks, 20 % is map-view claiming, pole-dragging and blueprint stamping, 10 % is looking at supply pips and following a dashed belt to a jam. Hand-feeding turrets exists only in the first 10 minutes and returns only when a line breaks. There is no unit control and no avatar to walk, so travel time is zero; that is a deliberate difference from Factorio where a Factorio player might miss the walk.

**Constraint or friction?** The front is a constraint: it takes something the player wanted to do anyway (expand) and attaches a price that can be paid cleverly (shape). On the scattered map the price of shape is 2.2× compact's ammo for a straight push and 0.97× for quiet-block play at equal territory (1.3× and 0.57× on open ground) **[sim: E6-scatter, E6-noscatter, at the 5-minute cadence]**. It is friction only when re-fronting is manual, and the Foreman removes that at hour 4–6. The honest weak point: before the Foreman, taking a block is claim → wait 40 s → pick up 2–4 turrets → place 2–4 turrets → extend a belt. Four hours of that is on the edge of tolerable. If playtests say it is not, the Line truck moves into the starting toolbar (section 24). The unfed rule gives the price teeth: an edge whose hoppers stay empty loses its block in 5–8 minutes, so the front constrains ammo routing as much as shape **[sim: E1-starve-substation-N40]**.

**Flow visibility (can the player see the bottleneck?).** Yes, in two places. In world view, empty belt segments are dashed grey and a jam has a static red marker at the stop tile. In map view, each frontage edge carries a supply pip, so "which edge is starving" is a colour on the map, not a tour of the line. Power shortfall is one HUD number; the assembler lines stop first, then the front dims **[sim: E2-h500-15pct-shed=machines-first]**. The thing the player *cannot* see directly is the rot growth rate of a block, only its density; that is intentional, but it is the first thing to expose if testers say fights feel random.

**Clear-the-jam (is fixing a problem a satisfying act, i.e. what does the player do and see?).** A red pip → click the edge → the camera lands on the turret pair → the belt behind it is dashed → the cause is a splitter with the wrong priority or an assembler out of copper. One or two clicks fix it, the dashes turn to moving items, the hopper counts climb, the pip turns green. The fix is always local and the confirmation is always visible on the same screen within 10 seconds. The bad case is a shade sabotage: the block goes dark with full hoppers and the player does not know why. The fix for the fix is a "sabotaged" icon on the substation for 60 s after a shade touches it, so the player learns the lamp rule from the first loss.

**Clever-feeling: three layouts a player can be proud of.**
1. *The ammo ring.* One belt loop around the interior boundary with a splitter to every frontage edge, fed at one point by three assemblers, so every edge shares one stock and the loop's priority order is the player's stated retreat order.
2. *The river T.* Claim east and west first so that the third claim (north) makes the HQ interior at minute 40 rather than minute 205 (the pure compact policy's first interior time) **[sim]**; the player who spots this feels like they beat the rule.
3. *The well noose.* Hold three sides of a well, put Cannons on the fourth-side neighbours before claiming the fourth, then claim it and let the truck lay kits while the noose closes. The player who does it with zero substations lost has understood every rule in section 5.

**Ignore test (can the player leave a problem alone?).** Yes. Nothing is on a clock. An awake block blooms forever but blooms are bounded (wake cap 40), and the turtle policy in the sim cost 528 magazines over three hours on the scattered map and the same on open ground, since the HQ's three neighbours are never inert **[sim: E7-turtle]**: flat, tiny, survivable. The one thing that cannot be ignored is an empty hopper: 40 unshot crawlers stop the substation, 5–8 minutes on a residential edge **[sim: E1-starve-substation-N40]**, and a line producing 0–15 % less than the front demands at the end of each hour lost 18–23 blocks in five hours on the spike policy, every one on an industrial edge (open ground; the same line on the scattered map lost 4–11 **[sim: E12-spike-scattered, at the 5-minute cadence]**) **[sim: E1-ring-spike, at the 5-minute cadence]**. A live well behind you that guards nothing (the (22,19) well in section 18) can be ignored for the whole run. The anti-turtle pressure is finite rubble (the start block's steel runs out in ~2 hours of two excavators) and the pull of facilities, not the enemy.

**Tedium audit.**
| Repeated action | Frequency | Removal point |
|---|---|---|
| Hand-feeding turrets | first 10 min, then only on line breaks | belt to hopper, minute ~8; Gunsmith removes the inserter |
| Placing a front strip on a new edge | every claim | blueprint from minute 1; Line truck kits from hour 4–6 |
| Picking up interior turrets | every enclosure | Line truck |
| Extending the ammo belt | every claim | interior trunk stays; the ring layout makes it one splitter |
| Linking a substation | every claim | one pole drag; never removed, it is the claim itself |
| Filling broken streetlights | every claim, 2–4 lamps | included in the kit |
| Carrying stuff to the Depot | early only | Depot input by belt |
| Re-taking a fallen block | rare | Foreman auto-restore |
| Watching burn-off (20–80 s) | every claim | never; it is the payoff, and you can do other things during it |

**Punish or teach?** Each enemy causes exactly one kind of loss and the loss is reversible. Crawlers eat lamps (cheap), then turrets, then walk to the substation; 40 of them stop it, 5–8 minutes after the pip you saw go red **[sim: E1-starve-substation-N40]**. Shades take a substation down for 30 s and if the block falls, everything on it is mothballed, not destroyed, and the retake costs one claim. Hulks are announced by a 3×3 sprite walking at 0.8 t/s, slow enough to watch. The only permanent cost in the game is ammo spent, and the only failure state is running out of a raw with nowhere to get more, which finite rubble makes possible but the outskirts deposits make avoidable.

**Pressure test (rhythm).** The rhythm is: calm inside, patter on the edge, one spike per claim. Claims are player-initiated, so spikes are chosen. The sim's claim cadence of one per 8 minutes gives a spike every 8 minutes with a 2× bloom and 40 s of burn-off between them; the steady patter in between is 1–4 small fights per minute across the front. There is no rising tide and no timer, and that is a risk: some players want to be pushed. The pressure that grows over a run is economic (the front gets longer and farther from the interior) and the wells and outskirts get worse with depth. The Relight's 10-minute hold exists to give the run one siege.

**First-hour test.** Section 11 in full. Minute 0–10: two rules learned (blocks bloom; ammo is made from rubble). Minute 10–30: two more (claiming wakes the neighbour; each claim changes the front count). Minute 30–60: the payoff rule (enclose, and the inside is free) and the first found unlock. Five rules in an hour and the first "if I take those two, these two go interior" sentence at about minute 50. Complexity of what has been introduced by minute 60: belts, inserters, one machine chain, poles, three block states, two enemy types: the first shade arrives with a residential bloom at ~47 min in compact play (17 min on a straight push); the first hulk waits for a well or outskirts edge, 99–154 min at the earliest **[sim: E8-first-shade, E8-first-hulk]**.

**"One more thing" test.** Every claim's tooltip shows `front +N · closes N`. The game keeps handing the player a claim that closes something: "one more block and east goes interior." Facilities are visible 6 blocks out, so there is always a silhouette you are two claims from. And each interior block you close hands back 2–4 turrets, which is exactly enough to take the next block. The chain is short, visible and free of timers. This is the strongest of the eleven checks and it comes straight from the one rule.

## 20. Prior art

- **Mindustry.** Closest cousin: tile grid, conveyor logistics, turrets fed by belts, enemy waves. Differences that matter: Mindustry's map is a fixed sector with a spawn point and a wave timer, so shape does not matter beyond choke points, and expansion is not a cost. Relight's front makes shape the primary decision, has no timer, and has no research (Mindustry's research tree is where its 200-hour depth lives; Relight trades that for territory).
- **They Are Billions.** The origin of "the map wakes as you expand" and of "a breach is a collapse". Relight takes the wake-on-approach and drops the collapse: a fallen block is mothballed, not eaten, because TAB's run-ending cascade is the reason its players save-scum. TAB also has population and housing, which Relight explicitly excludes.
- **Factorio biters.** Pollution-driven expansion of the enemy toward the player. Relight replaces pollution with light: threat is about where you *are*, not what you emit, so it is spatial and reversible. Factorio's turret creep is the closest existing feel to the claim → wake → hold loop.
- **Creeper World series.** Rot as a density field that the player's tools burn back is a direct descendant of the creeper fluid. Creeper World has no logistics; Relight's rot is per block, not per tile, to keep it readable at city scale.
- **Kingdom Two Crowns.** Expansion as "the next wall out", with the inside becoming safe. That is the interior rule, seen from the side.
- **Rimworld / Frostpunk** are the thing Relight is *not*: no colonists, no morale, no cold.

## 21. Why a Factorio fan or a Mindustry player would buy it

**Factorio fan.** The belts, inserters, splitters, poles and assembler recipes are the ones they know; there is nothing to relearn and the first 10 minutes feel like home. What is new is that the map has an opinion: every expansion is priced and shaped, so the layout question they love (where does the trunk go) is joined by a second one (what shape is the base). Biters were never that; you walled them off and forgot them. And there is no research tree, so a 25-hour run is a run, not a 60-hour one.

**Mindustry player.** They get a turret line fed by conveyors, which is the Mindustry core, in a city that is not a sector: no wave countdown, no fixed spawn, and the front moves because *they* moved it. Their skill at choke points becomes skill at enclosure. And there is a game-end that is a picture (the Relight), not a launch.

**Both.** Two to four people can ship it because it is one mechanic; the pitch survives a screenshot of map view with 40 red edges and 12 amber interiors, and anyone who has played either game reads that screenshot in five seconds.

## 22. What is deliberately not simulated

- **People.** Survivors are recipe unlocks with a sprite. No food, housing, morale, headcount, death.
- **Fluids.** Water and oil are handled by pre-placed riverside facilities and a Crude drum item. No pipes, no pumps, no tanks.
- **Damage to infrastructure.** Belts, poles, machines and facilities are never destroyed. Lamps and barricades and turrets can be chewed (they drop to 0 HP and are picked up as scrap-free "needs replacing" ghosts, refilled from stock by the truck).
- **Repair and maintenance.** Nothing wears out.
- **Day/night, weather, seasons.** The light map is the only lighting and it is static per tile.
- **Per-tile rot in asleep blocks.** Only awake and contested blocks have a per-tile rot layer; asleep blocks are one number.
- **Enemy AI beyond flow-field.** Each bloom follows a flow field to its target class. No formations, no retreat, no learning.
- **Research, technology tiers, item quality, modules, circuits, robots, trains with signals.**
- **Economy.** No money, no trade, no contracts.
- **Player avatar.** Nothing walks.

## 23. Rejected alternatives

- **Horde / timed waves (the enemy model not picked).** A global timer with escalating waves at the front is TAB's model and it is easier to tune. Rejected because it makes the front a *place* rather than a *cost*: with a timer, shape only matters as choke points and the "every block extends the front you have to feed" sentence stops being true. It also puts the player on a clock, which kills the ignore test. Kept: bloom size caps and interleaved timers, borrowed from wave design so that the creeping model still has legible fights.
- **Player avatar.** Familiar to Factorio players, but travel time in a 768×768-tile city is dead time and a hero with a gun turns fights into player combat. Rejected.
- **Salvage / dismantling / scrap.** Excluded by the brief and the right call: it makes every building a resource and every enemy a drop, which is a loot loop.
- **Research menu.** Found tech does the same job with a map instead of a tree.
- **Powered barricades / walls that need power.** Made power a third front cost. Rejected; barricades are dumb concrete.
- **Turrets that need power.** Would let a brownout collapse the front instantly. Rejected; turrets need only ammo, so ammo and power are two separate failures with two separate fixes. Machines-first shedding (section 14) recreates the ammo dependency one step removed: a brownout stops the Shot assemblers, the hoppers drain, and the unfed rule takes the block. The difference relied on is speed: 5–8 minutes with a red pip on the map, not instant.
- **Separate rail and tram.** Two bulk systems. Rejected; one tram system on streets.
- **8-neighbour adjacency.** Made enclosure need 8 claims instead of 4, and diagonal "touch" is unreadable at block scale. Rejected.
- **Rot spreading into lit blocks.** Would make holding a block a per-tile fight. Rejected; a held block with lights on is safe, full stop.
- **Survivors needing food or housing.** Fourth system. Rejected.
- **Day/night cycle.** Would make the light map dynamic and the Shade rule time-based. Rejected.
- **Fluid system.** A whole simulation layer for two recipes. Rejected; riverside facilities and drums.
- **Unit-based capture (send a squad to clear a block).** Player-controlled units. Rejected.
- **Pollution-driven threat.** Makes the factory, not the territory, the cause; it is the Factorio model and it does not produce a front.

## 24. Biggest development risks

**Design risks.**
1. *Re-fronting is tedious before the Foreman.* Mitigation as a design change: blueprints from minute one; the "kit" concept is available as a manual stamp from the start; if playtests still show fatigue at hour 2, the Line truck moves into the starting toolbar and the Foreman becomes an upgrade (4 kits → 8, auto-restore).
2. *Ammo tuning: the front either never bites or starves everyone.* Mitigation: the tuning surface is four numbers per district (dmax, g, bloom base, bloom slope) and the sim already exposes them; blooms are capped and bursty, but a line 7–30 % short lost 4 blocks in five hours on the spike policy, all on industrial and well edges, while compact play at 76 % load lost none **[sim: E1-ring-spike, E1-ring-substation-N40]**: the pip reddens slowly on a residential edge and a well edge simply falls.
3. *Rot is unreadable.* Mitigation: five discrete density tiers in the overlay tileset, the map-view mottle, and the bloom ring; if testers still cannot read it, show density as a number on hover only in map view.
4. *A block falling is not legible.* Mitigation: a 60-second "sabotaged" or "brownout" icon on the substation, the lights going out in sequence (the payoff played backwards), and the block's map square blinking navy for 10 s before the state change.
5. *The endgame is a haul, not a climax.* Mitigation: the 10-minute siege hold is the only timed event in the game and it is the last one; the restore kit is deliverable by a single freight tram line so the haul is a logistics build, not a wait.
6. *Greedy "quietest block" play dominates (the sim's cheapest policy used 56 % of compact's ammo on open ground and had 20 interiors at 5 h, but 131 % of compact's ammo on the scattered map, where compact play gets free walls)* **[sim: E6-noscatter, E6-scatter]**. Mitigation: the sim ignores resource pull. In the game the quiet blocks are civic (stone) and the player needs steel and copper, which live in dense blocks. If real play still shows it, civic caps rise from 0.30 to 0.40.
7. *An early brownout sheds the HQ first.* Under substation-first shedding the most-dark-neighbours rule picks the HQ (three dark neighbours) at minute 8 of the §11 start (minute 6 at the old 200 kW draw); exempting the HQ instead stalls the ammo line, and the 40 coal in hand still runs out at minute 11. Mitigation: the §11 start conditions (coal patch, a Generator at minute 6, a third at the east claim and a fourth at minute 45), under which hour one never browns out **[sim: E4h-literal-1gen, E4h-hq-exempt-1gen, E4h-gen@0,6,15,25]**.
8. *A straight push outruns its ammo line.* On the spike policy demand exceeds production by 7–30 % at 3 h, the buffer is empty after 2 h, and 4 blocks fall in 5 h even with the ring fed in creation order. Mitigation: none in the rules; it is the price of shape, and §12 now states the demand **[sim: E1-ring-spike]**.
9. *Cascade during a shortfall.* Under substation-first shedding a 25 % shortfall for 10 minutes at hour 4 cost 9 blocks, 4 of them knock-ons from the +160 kW each fallen block hands its neighbours. Mitigation: machines-first shedding (section 14), which cut it to 2, and 1 MW of headroom, which cut it to 0 **[sim: E2-h500-25pct-doc, E2-h500-25pct-shed=machines-first, E2-h1000-25pct-shed=machines-first]**.

**Technical risks.**
1. *Simulating rot per tile across a 768×768 map.* Only awake and contested blocks carry a per-tile layer (typically 20–70 blocks); asleep blocks are one float.
2. *Pathfinding for 40-crawler blooms across many edges.* One flow field per bloom per target class, cached per block edge; enemies never path across more than two blocks.
3. *Belt simulation at Factorio scale.* Standard lane-based belt sim; the city is smaller than a typical Factorio base by item count because chains are short.
4. *Procedural city validity.* Rejection sampling with the validator in section 17; target < 1 s per accepted seed.
5. *Light-map overlay on integrated GPUs.* One 768×768 single-channel texture updated per changed tile; trivially cheap.

## 25. Open questions

1. **Bloom cadence.** Is `T = 120/(0.5+d)` with a 10 % drop per bloom the right rhythm, or do fights need to be rarer and bigger? *Prototype first, headless.* The front simulation (`frontsim.py`, 24×22 blocks, five claim policies) now has an ammo ring with hoppers and a power model with shedding; belt latency is still not modelled. The cadence was varied on the scattered map: doubling the timer to `240/(0.5+d)` halves fights on a 20-edge front from 6.8 to 3.6 a minute and cuts ammo per edge 30 % (1.37 → 0.97 mag/edge/min); a 20 % drop per bloom instead of 10 % keeps 6.1 fights a minute and cuts ammo 24 % **[sim: E10-bloom-cadence]**. Which rhythm is right is a feel question for the prototype, not a sim question.
2. **Diagonal leaks.** With 4-adjacency, can a player build a checkerboard that is all "interior"? No: each held block needs all four neighbours held, so a checkerboard is all front. But a diagonal seam of inert cells may create degenerate free enclosures. A validator rejecting 2×2 inert squares or runs of three changed nothing on three scattered seeds (0–1 such squares occur and ammo totals were identical with and without it) **[sim: E6-scatter]**; keep it as a map-generator assertion, not a rule. Counting scattered inert as Dark for enclosure (the other reading of §5) changes nothing for compact or cheapest play — same claims, same ammo — and costs the river-hugging policy 21 % more ammo with its first enclosure 1.5–2.5 hours later; §5 stands **[sim: E13-inert-dark]**.
3. **Global stock too easy?** It removes a Factorio friction (carrying) that some players consider the game. Test with and without a Depot-radius rule.
4. **Well count and visibility.** Three to six always visible. Should some be hidden until adjacent? Hidden wells punish planning; visible wells are a promise. Lean visible.
5. **Endgame hold.** Run at 25 h on compact play at the doc cadence, three seeds, wells ×3 only (the sim has no wake-size bloom for every awake block, so this is the floor) **[sim: E11-relight-hold]**. With four assemblers (80 mag/min made) the window demands 133 mag/min against 83 the hour before; with six (120 made) 192 against 123. Peak minutes: 194 and 293 magazines against 120 and 162. Blocks lost in the twenty minutes from 25:00: 18–24 and 24–29, against 2–3 without the surge. At parity between production and demand the hold is a wall, not a nothing. Reframed (calibration 2): §16 now says the hold is survivable with preparation and that a stock of ~2× an hour's production, banked over the final hour, should hold the loss under 5 blocks. Open: confirm that number — re-run E11 with the wake-size bloom on every awake block and the bank in stock, before the world view builds the hold. Compact play at the doc cadence runs at parity at 25 h whatever the assembler count and never banks on its own, so no run has yet survived the hold.
6. **Density farming.** Can a player leave one dark block encircled as an ammo-free "rot farm" for nothing? There is nothing to farm (no drops), so the only exploit is a stable interior with a hole; the hole costs four edges forever, which is the intended price.
7. **The quiet-block problem** (risk 6 above). The first real-game playtest question is whether resource pull is enough to make players take dense blocks. On the scattered map the sim already prices quiet-block play above compact play (1.3×) **[sim: E6-scatter]**; on open ground it is still 0.56×.
8. **Outskirts and well edge cost in play.** The per-edge outskirts figure rests on 15 blooms in five hours of compact play (12 mag/edge/min against 4.3 at isolated steady state) and the well figures on one well; a 10-hour compact run that reaches the outskirts would settle them **[sim: E3-compact-shade0.3-hulk0.5]**.
9. **Coal depletion and the Turbine hall.** There is no rubble-depletion model, so "coal runs out for the second player in three" (section 15) is unsupported; it needs excavator throughput against per-block rubble stock.
10. **The §18 drawings are ahead of the sim's cadence.** The 5-hour drawing holds 52 blocks and the 25-hour drawing 313, against 34 and 184 in the sim at one claim per 8 minutes; either the drawings are redrawn or the claim cadence is the thing the map-view prototype tests. The drawings imply one claim every ~5 minutes after hour one: at 4 minutes compact holds 64 at 5 h and 364 at 25 h, at 6 minutes 44 and 244, at 8 minutes 34 and 184 **[sim: E9-claim-cadence]**. The drawings stay as they are; the prototype's claims-per-hour telemetry says which cadence players pick.
11. **Late-game demand.** Ammo demand past 5 h and power past 25 h were not measured with the new models; the mid and late ammo columns of §12 keep their original estimates.
12. **Fall distance.** Closed. 90 s from substation stop to Dark is the rule (§5), wherever the substation sits; the 48 s lot-centre figure is gone. Decisions matched at both **[sim: E8-fall16]**, and 90 s leaves a 30 s rescue window against the 60 s refeed reset, which is the play we want.
13. **Spike on the scattered map.** Closed. Spike on the scattered map costs 3.2× compact and loses nothing: with the doc's assembler schedule it lost 0 blocks in five hours on three seeds (open ground: 4, all to shades), so it is clearly worse but valid — a choice, not a trap **[sim: E12-spike-scattered]**.
14. **Power in hour one.** Closed on the draw. Front and Contested substations draw 100 kW and interior 20 kW (§5; 200/40 had no evidence behind it), and ammo, not power, is the hour-one lesson. At that draw three Generators carry hour one to minute 45 and a fourth is needed for the fifth Excavator and third Assembler (980 kW peak, two-thirds of it machines) **[sim: E4h-gen@0,6,15, E4h-gen@0,6,15,25]**; §11 says four, not five. Not settled: the ~3,000-unit HQ coal patch mines out at about minute 106 at one Excavator, so it does not make west wanted by minute 30; ~700 units would, and the choice is recorded in CALIBRATION_REPORT_2.md.

Prototype order: (1) headless front + ammo-line sim, two days; (2) a map-view-only prototype with claim, poles, pips and bloom rings and no world view, two weeks; (3) the world view with belts, one machine chain and turrets, eight weeks. The bet is settled by step 2: if choosing blocks on a map with a front count is not interesting on its own, no amount of belt fidelity saves it.

## 26. Complexity score

**5 / 10.**

Counting systems the player must hold in their head at once: (1) belts/inserters/machines, which they already know; (2) the front rule, one sentence with three consequences (wake, cost, enclosure); (3) found tech, which is a map, not a system. Power is a single number. Trams are one-track shuttles. Enemies are three, each bound to one supply. Nothing is on a timer except the last ten minutes.

Cuts made during design to keep it at 5: fluids (→ drums and pre-placed facilities), rail signalling (→ one tram per track), powered barricades, the avatar, a fourth enemy (a "burrower" that hit belts; cut because belts must never be damaged), survivor needs, and a per-tile rot layer in asleep blocks. The one thing I would still cut if a tester says "too much" is the Cannon/shell line: hulks could instead be killed by concentrated Shot at a much higher round count, and coal would be power-only.

Every feature, one sentence each: *Claim tool* is the mechanic. *Substations* make claiming one action. *Streetlights and lamps* are the shade rule and the payoff. *Turrets* are the ammo sink. *Cannons* make frontage length matter for one enemy. *Barricades* buy 4 s per tile against hulks and nothing else. *Excavators* are the miner. *Assembler/Mixer* are the recipes. *Generators/Turbine/Power station* are the power curve. *Belts/splitters/undergrounds/inserters/chests* are Factorio. *Depot* removes carrying. *Trams* are distance. *Line truck* removes re-fronting. *Blueprints* remove repetition. *Facilities/survivors* are the progression. *Wells* are bosses. *Inert cells* make enclosure geometry vary. *Map view* is where the front is played. *Presets and targets* are replay.

## 27. Fun confidence

**7 / 10.**

Evidence for: the one-sentence hook produces a decision at every claim (`front +N · closes N`); the sim shows a straight push costs 2.2× compact's ammo at equal territory on the scattered map and quiet-block play 0.97× (1.3× and 0.57× on open ground), so the decision has weight **[sim: E6-scatter, E6-noscatter, at the 5-minute cadence]**; the "one more thing" chain is built from the rule itself; the payoff is a visual event that happens 150+ times per run and gets bigger each time; the tedium audit has a removal point for every repeated action.

What would raise it to 8: the map-view-only prototype (section 25, step 2) showing that testers make different shapes on the same seed and argue about them.

What would drop it to 5: (a) testers treat the front as a tax, i.e. they always take the cheapest block and never talk about shape (the quiet-block risk); (b) the patter of small fights reads as noise rather than information, meaning pips are watched but blooms are not; (c) hour 2–4 re-fronting fatigue before the Foreman. Each has a named mitigation in section 24 and none requires a new system.

---

## Appendix: veteran self-review

*As a sceptical Factorio veteran.* "Where's my factory after containment?" The answer is that the front never ends until the Relight, and the Relight needs 40 MW and a four-item kit that is the largest build in the game; the factory grows for the whole run and interiors are the factory. "Is this Mindustry with a city skin?" The city skin is doing work: blocks are the unit of cost, and Mindustry has no unit of cost. "Hidden fourth system?" Power is the candidate; it is kept to one number with one failure mode (front dims first) and no distribution puzzle inside a block. "Degenerate strategy?" Quiet-block greed, named and mitigated. "Waiting?" Burn-off is 20–80 s and you are not made to watch it; the only wait is the ten-minute hold, once.

*As a sceptical Mindustry player.* "Where are the waves?" Replaced by blooms you cause. "Where is the tech?" On the map. "Do turrets need power?" No, and that is deliberate, because a Mindustry brownout is a wipe. "Is there a boss?" Wells, and the Relight hold. "Will I lose my base?" You will lose a block; you will never lose a building.

Neither review found a system to cut; both found the same risk (greedy quiet-block play), which is why it is open question 7 and the first playtest question.

## Changelog

Each edit as `§N — what changed — why — run name`. Every run name is a line in `python frontsim.py --experiments` (E8 also via `--experiments E8`); the sim's assumptions are printed at the top of that output. Entries from the map-view prototype brief onward are `§N — what changed — why`.

- §5 — map boundary counts as Held (the sim's frontage counts assume it) — E7
- §5 — rot timing was 2× too slow; defined lit lots vs streets and what 'lights out' means (wording gaps) — sanity
- §5 — residential steady-state ammo 1.2 → 1.3 — E3-block
- §5 — industrial steady-state ammo 2.2 → 1.8 — E3-block
- §5 — added the in-play per-edge average so the table's steady state and the sim's measured cost both appear — E3-compact-shade0.3-hulk0.5
- §5 — the cap was written as if it bounded normal blooms; it only ever trims wake blooms — wake-cap
- §5 — added the unfed consequence (crawlers stop the substation at 40 arrivals), shed order, the 48 s vs 90 s arithmetic, and replaced 'Nothing cascades' with what the sim shows — E1-starve-substation-N40, E2-h500-15pct-doc, E2-h500-25pct-doc, E2-h500-*-shed=machines-first, E8-fall16
- §7 — residential ammo 1.2 → 1.3 — E3-block
- §7 — rail yard ammo 1.2 → 1.3 — E3-block
- §7 — industrial steady d 0.28 → 0.29, ammo 2.2 → 1.8 — E3-block
- §7 — outskirts steady d 0.49 → 0.50 — E3-block
- §7 — well row: steady d ~0.75 → 0.54–0.78 by district, ammo 7.7+2.5 → 5.3–8.7 + 2.1–2.6 — E3-block
- §7 — tagged the table's source — E3-block, E7
- §7 — crawler target chain ends at the substation, which is the unfed consequence — E1-starve-substation-N40
- §7 — the crawler row had no consequence — E1-starve-substation-N40
- §7 — cap applies to wake blooms; stated the worst case it bounds — wake-cap
- §7 — shades start in residential, not industrial; hulks are a well/outskirts enemy; shape ratio restated for the scattered map — E3-*, E6-noscatter, E6-scatter
- §9 — corridor cost restated for the scattered map — E6-noscatter, E6-scatter
- §11 — start block gains a coal patch (40 coal runs out at minute 11); 20 magazines confirmed sufficient — E1-start-min, E4-literal, E4-start-coal-rubble
- §11 — second Generator at minute 6; the §11 start as written browns out at 6 min — E4-literal, E4-rubble+gen@6,15,25,40
- §11 — a Generator per claim; 2 Generators cannot carry four 200 kW substations — E4-literal, E4-rubble+gen@6,15,25,40
- §11 — 5 Generators by minute 40, one Shot assembler in hour 1, first shade at ~47 min — E4-rubble+gen@6,15,25,40, E5-compact-1-shot-asm-h1, E8-first-shade-compact
- §12 — early ammo demand 10–30 → 8–48 mag/min (1 h → 3 h) — E5-compact-base
- §12 — early power 0.3–1.5 → 1.2–4.5 MW, mid 3–8 → 4.5–8 MW at the doc's own 200/40 kW draws — E4-literal, E2-demand, E2-demand-25h
- §13 — inserter count per stop stated (600 items through one 1/s inserter would be 10 min) — wording gap, no sim
- §14 — shed order: machines before substations — E2-h500-15pct-doc, E2-h500-15pct-shed=machines-first
- §14 — freight load time follows from the stop's inserter count — wording gap, no sim
- §15 — first shades are residential, not industrial — E8-first-shade
- §15 — power at 3 h 2 → 4.5 MW — E2-demand
- §15 — removed a [sim] tag no run supports; stated the demand the 5 MW meets — E2-demand-25h
- §17 — 3.4× matched no run (open-ground compact/cheapest is 1.77×); quiet-block play is dearer than compact on the scattered map — E6-noscatter, E6-scatter
- §18 — caption counted the sim, not the drawing; both now stated — E7-compact, E5-compact-base, §18 recount
- §18 — caption (~180 / 38 / ~120) matched neither the drawing (313 / 67 / 270) nor the sim (184 / 31 / 155) — E2-demand-25h, §18 recount
- §19 — constraint now includes the unfed consequence — E1-starve-substation-N40
- §19 — follows the new shed order — E2-h500-15pct-shed=machines-first
- §19 — Ignore test now names the one thing on a clock — E7-turtle, E1-starve-substation-N40, E1-ring-spike
- §19 — crawler loss is now a block, on a visible timer — E1-starve-substation-N40
- §19 — hour one contains a shade; hulks are later than stated — E8-first-shade, E8-first-hulk
- §24 — the '20 % short is survivable' claim is false on industrial and well edges — E1-ring-spike, E1-ring-substation-N40
- §24 — 'a third' was 56 %; on the scattered map quiet-block play is dearer than compact — E6-noscatter, E6-scatter
- §24 — three risks the sim exposed — E4-literal, E4-hq-exempt, E1-ring-spike, E2-h500-25pct-*, E2-h1000-25pct-shed=machines-first
- §25 — the ammo-line extension the item asked for now exists — E1–E8
- §25 — validator tested and found irrelevant at 8–10 % scatter — E6-scatter
- §25 — status update — none
- §25 — unsettled questions the runs could not close — E6-scatter, E3-compact-shade0.3-hulk0.5, E8-fall16
- §17 — the default generator scatters 8–10 % inert cells and every headline ratio refers to that map — the scattered map is canonical; open ground is the special case
- §7 — shape ratio leads with 3.2× on the scattered map, open ground in a parenthetical — the scattered map is canonical
- §9 — corridor cost leads with the scattered map — the scattered map is canonical
- §19 — constraint states the scattered-map price of shape (3.2× and 1.31×); ignore test says the turtle figure holds on both maps and marks the E1-ring-spike loss count as open-ground — the scattered map is canonical
- §27 — evidence line leads with 3.2× and 1.31× instead of '~60 %' — the scattered map is canonical
- §25 — new item 13: spike on the scattered map may be a trap rather than a choice — untested until E12
- §25 — new item 14: power's hour-one role is undecided and outside the map-view prototype — §11's E4 fix makes power the dominant hour-one cost; decide before world view; §11 left as is
- §14 — full brownout shedding order stated (assemblers, other machines, excavators feeding Generators, then substations by dark neighbours) — 'machines first' named no order among machines
- §23 — turrets-need-power entry says machines-first shedding recreates the ammo dependency one step removed and that the difference relied on is speed — the entry claimed a separation the shed order does not give
- §25 — item 1 gains the varied-cadence numbers (a 240 s timer halves fights, a 20 % drop cuts ammo a quarter) — the cadence had never been varied — E10-bloom-cadence
- §25 — item 10 says the drawings imply a claim every ~5 minutes; the drawings stay — E9 ran 4/6/8-minute cadences to 25 h — E9-claim-cadence
- §18 — 5 h and 25 h captions note the ~5-minute cadence that matches the drawings' counts — same run; nothing redrawn — E9-claim-cadence
- §19, §25 — spike on the scattered map lost no blocks in five hours; item 13 closed as a choice, not a trap — the open-ground loss count was the only one — E12-spike-scattered
- §25 — item 2 says what inert-as-Dark would cost (river +21 %, compact and cheapest unchanged); §5 stands — the prototype needed the enclosure reading settled — E13-inert-dark
- §25 — item 5 has the hold's numbers (window demand 1.6× the hour before, peak minutes 2.3×, 18–29 blocks lost in twenty minutes at parity); banked stock left open — the hold had never been run — E11-relight-hold
- §16 — step 4 names the surge's ammo multiple and block cost and says the wake-size blooms are not in the sim — same run — E11-relight-hold
- §7 — item 1's corridor cost is 2.2× compact's ammo on the scattered map and ~1.3× on open ground (was 3.2× and ~1.6×) — the sim's claim gap after hour one is now the 5 minutes §18 is drawn at (was 8) and the fixtures were re-exported; the corridor's extra cost is smaller when claims are cheaper — E6 re-run, CALIBRATION_REPORT.md part 1
- §19 — the price of shape is 2.2× for a straight push and 0.97× for quiet-block play (1.3× and 0.57× on open ground); the spike line loses 18–23 blocks on open ground and 4–11 on the scattered map, within 0–15 % of demand — same re-export at the 5-minute cadence; the old 3.2× / 1.31× and "lost none on the scattered map" no longer hold — E1/E6/E12 re-run, CALIBRATION_REPORT.md part 1
- §27 — the evidence line quotes the same 2.2× / 0.97× figures as §7 and §19 (was 3.2× / 1.31×) — a stale copy of the §7 number would have contradicted it; beyond the §7/§19 brief, flagged in CALIBRATION_REPORT.md — same re-export
- §5 — cost table: substation draw 100 kW for a Contested or Held block with a dark neighbour, 20 kW interior (was 200/40); the cascade's knock-on draw is 80 kW — D1, calibration 2: ammo is the hour-one lesson, power is not — E4h-*, CALIBRATION_REPORT_2.md
- §5 — a block is Dark 90 s after its substation stops, wherever the substation sits; the 48 s lot-centre figure is deleted — D2: with the 60 s refeed reset that is a 30 s rescue window — E8-fall16
- §5 — the 40-unshot-arrivals rule says the counter is per block across all its edges, so corner blocks fail faster — D3 — E1-starve-substation-N40
- §11 — the HQ lot's coal patch is ~3,000 units; one Generator browns out at minute 8 at the new draw; third Generator with the east claim, fourth at minute 45, none with west or north (was five by minute 40); the patch mines out at ~106 min, so west's coal is wanted in hour two — D1; E4 re-run at 100/20 kW — E4h-literal-1gen, E4h-gen@0,6,15, E4h-gen@0,6,15,25
- §12 — power row 1.0–3.1 / 3.1–5.5 / 5.5–8.6 MW (was 1.2–4.5 / 4.5–8 / 10–15) — D1; E2-demand re-run at the halved draw, 5 h and 25 h, compact seed 3 — E2-demand-half, E2-demand-half-25h
- §23 — risk 7's HQ shed is at minute 8 at the new draw and its mitigation is three Generators plus a fourth at minute 45 — D1 — E4h-literal-1gen, E4h-hq-exempt-1gen, E4h-gen@0,6,15,25
- §16 — the hold is survivable with preparation: banking magazines over the last hour is the intended play, ~2× an hour's production banked should lose fewer than 5 blocks; the E11 figures are the unprepared floor — D4 — E11-relight-hold
- §25 — item 5 reframed to confirming the banked number with the wake-size bloom on every awake block before the world view builds the hold — D4
- §25 — item 12 closed: 90 s is the rule — D2
- §25 — item 14 closed on the draw (100/20 kW, four Generators in hour one); the patch size that makes west wanted by minute 30 is recorded as not settled (3,000 mines out at ~106 min, ~700 would at 30) — D1 — E4h-*
