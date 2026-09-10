# Riverfront Arc city rebuild

## CITY-F — current city-wide density

The owner approved the starting neighbourhood and requested extension across the city. The current map has 479 buildings: 77 rebuilt smaller blocks and ten retained civic blocks with forecourts/details. Residential rows/courts, Old Town terraces/shops and industrial loading courts replace generic sparse placements. The approved CITY-E neighbourhood and all campaign/factory/transport geometry remain unchanged. [Full evidence, screenshots and limits](../evidence/city-density/README.md). New Game is required; the updated scene is saved in Phaser. Existing artwork remains in use.

## CITY-E — approved starting neighbourhood (retained)

The owner approved a focused density pass before adding new artwork. At this checkpoint the map had 301 buildings, including a short Court shopping street, Founders Mews and four Court infill homes. The two adjoining blocks increase building coverage from 13.7% to 32.0% and 26.3%. All retained buildings, factory yards, roads/tram and campaign bindings are unchanged. [Implementation and verification](../evidence/neighbourhood-density/README.md); [Phaser building/plot authoring](../PHASER_EDITOR.md). Reload 5178 and start New Game. The original full-city generator remains guarded against overwriting this editor baseline.

## Tiled editing export

The earlier pre-density v4 map is available as [riverfront.tmj](../../maps/tiled/riverfront-v4/riverfront.tmj), with portable tilesets/art and [editing instructions](../../maps/tiled/README.md). [exportTiled.ts](../../packages/tools/src/exportTiled.ts) reads the canonical TypeScript and shared geometry directly; `npm run map:export-tiled -- maps/tiled/new-folder` creates another copy without overwriting an existing map. The export has 23 layers, 2,031 objects, all 287 buildings and a paintable 32-pixel grid. Tiled's installed native rasterizer successfully loads the map, tilesets and SVGs; building/resource coordinates match the game data. [Verification](../../maps/tiled/riverfront-v4/verification.json) and [native preview](../../maps/tiled/riverfront-v4/preview.png) are included. Props and gameplay bindings use editor shapes; dynamic game rendering is not reproduced. This is **export-only**: Tiled edits do not update the game until an import/conversion step is implemented. Existing runtime data and saves are unchanged.

## CITY-D — Founders Court finishing pass (historical v4)

Implemented 2026-09-10 from the owner's focused finishing brief. Reload [mutable 5178](http://127.0.0.1:5178/?view=world) and choose **New Game** for `riverfront-arc-v4`. Added solid houses could overlap saved construction, so the existing original-build rejection preserves v1/v2/v3 saves instead of migrating their contents. No other city layout, scale, factory capacity, tram route or gameplay rule changed.

| Stable house ID | Top-left | Footprint | Facing |
| --- | --- | --- | --- |
| court-northwest-home | 31,351 | 10 × 9 | E |
| court-west-home | 31,374 | 10 × 9 | E |
| court-approach-home | 49,404 | 10 × 9 | E |
| court-east-cottage | 85,382 | 9 × 7 | W |

All four requested positions fit unchanged, with two-tile plot borders. The two existing Court houses remain, for six nearby houses. The northwest path turns around the maintenance garage to reach the court; the other three connect directly to public pavement. New houses are solid background buildings with boarded doors, no loot or interaction. Hip, ridge and cross-gable roof components, a smaller cottage, beds and a bench provide lightweight variation. All 283 existing building records (including paths), roads, yards, resources, drives, objectives and tram coordinates compare exactly against the pre-pass source. Total buildings: **287**; reachable land: **372,448** tiles. The four additional footprints account for the small land reduction.

### Source and regeneration conventions

Edit the explicit `homeHouses` list in [authorFullCity.py](../evidence/city-c/authorFullCity.py), then regenerate [riverfront.ts](../../packages/sim/src/city/riverfront.ts). Generic filler still excludes the combined Home region. Retained, explicit and filler buildings share full rectangle/corridor, turning-bulb, resource, yard, drive and solid-prop checks before output. Tram checks use the same quarter-circle samples and reserved cell rule as the runtime. Boundary tangency is allowed; penetrating reservations is rejected with IDs/coordinates. All footpaths have a checked 1.4-tile strip; new Home paths additionally avoid neighbouring garden plots. Existing contained interior furniture is intentional. Runtime validation independently checks canonical geometry and reachable entrances. Terrain ownership overrides remain classification only, not placement repairs.

The historical baseline still supplies retained content, but **runtime code no longer comes from `before-riverfront.ts` string substitutions**. [riverfront-runtime.inc](../evidence/city-c/riverfront-runtime.inc) preserves the inspected current definition/geometry implementation. The compiler refuses to emit if current TypeScript runtime code differs from that maintained template. Reconcile intentional runtime edits into the template first; never bypass the drift guard to overwrite newer navigation, rail or geometry work.

[parcelGeometry.ts](../../packages/sim/src/city/parcelGeometry.ts) owns N/S/E/W doorway rectangles and outward approach points. Collision, tracking targets, doorway light pools and rendered steps consume these conventions. Roof entry hysteresis and dynamic ground invalidation retain the existing systems. New [build-facing-houses.py](../../packages/game/public/art/riverfront/build-facing-houses.py) generates nine original component-based N/E/W house roofs with fixed northwest highlights; finished sprites are not rotated. Existing S roofs/floors remain. The asset set now contains 65 SVG layers.

### Yard and lighting correction

The yard's misleading facade was the renderer's repeated grate bars along its lower edge, not a hidden building binding. The renderer now draws concrete slabs, a restrained perimeter and a clear east service opening connected to the existing Home drive. It stays ground-only below people and machines. Its `(83,350,30,29)` footprint, collision and production capacity are unchanged; an ordinary paid assembler placement and service route pass.

The authored view mask, not fog or electricity, caused midnight at the opening: `daylight(0)` was zero while the clock called the phase Daylight. Dawn now ramps during seconds 1110–1200 of the preceding night; the opening and day boundary are fully bright. Dusk remains 780–900, dark night 900–1110. Smooth transitions, dim interior ambient, occluded local lights, flashlight, power state, discovery and accessibility defaults are preserved. No HUD/global brightness or simulation lighting change was made.

### Focused evidence and limits

[Validation](../evidence/founders-court/validation.json) has no errors and confirms retained geometry equality. [Four targeted regressions](../evidence/founders-court/tests.txt) cover full corridor crossing missed by point sampling, all four door/wall/path/roof cases, final plots/yard/save isolation, and daylight continuity. Both retained interior/gate and dynamic-navigation checks pass separately (six focused checks total). The project type/build, lint and docsync checks pass. Freshness retains the same 12 historical stale/unstamped artifacts; they were not regenerated. Generator output is byte-for-byte repeatable. No campaign simulations or broad game audit were run for this bounded pass.

Actual Chrome/WebGL views: [before opening](../evidence/founders-court/before-day.png), [after opening](../evidence/founders-court/after-day.png), [matching night](../evidence/founders-court/after-night.png), [northwest](../evidence/founders-court/northwest-day.png), [approach house](../evidence/founders-court/approach-day.png), [open yard](../evidence/founders-court/yard-day.png), [walked interior](../evidence/founders-court/walked-interior.png), [walked exterior](../evidence/founders-court/walked-exterior.png). Inspected at 1600 × 1000 and zoom .7; day/night use the same `(72.5,377.5)` camera with phase 0/1050 and flashlight disabled. The before image retains the original default flashlight. Browser verification uses debug position/time only for setup, then ordinary held keys through the retained Rowan doorway; roof fade/return, collision against a new boarded east doorway and ordinary save/reload pass with no page errors. Home's existing supply structure stops walking farther through that entrance, so the walkthrough uses Rowan; Home supply approach remains reachable. New E/W houses are intentionally non-enterable; four-direction enterable behavior is a small simulation/roof-state fixture, not a claim of new interiors. [Browser record](../evidence/founders-court/visual-review.json) records setups and results. Debug states/settings are confined to isolated browser contexts, not default saves.

Human acceptance and wider performance/release gates remain open. Nothing was committed, pushed, deployed or published.

## CITY-C — full-city grid, fitted plots and smooth tram (historical v3)

Implemented 2026-09-10 in the existing Phaser **3.90.0** project. Configuration remains `Phaser.AUTO`; the actual browser check uses WebGL. Simulation uses the existing tile collision and bounded navigation, with no Phaser physics migration or engine upgrade. The attached **B — The High Street** close-up guided architecture; the written 24 × 16 blueprint supplied the macro layout. The earlier CITY-B section below records its superseded geometry and evidence.

Open [the revised local city](http://127.0.0.1:5178/?view=world), reload, and choose **New Game**. The preview serves `packages/game/dist`. Both earlier authored map revisions are rejected with an original-build/New Game message and their saves remain untouched. V3 roundtrips and ordinary browser save/reload are checked. Procedural saves retain their original constructor/geometry.

### Final layout and scale

The canonical [riverfront.ts](../../packages/sim/src/city/riverfront.ts) contains **864 × 576 tiles**, 36-tile planning cells, **283 buildings**, 371 public-road edges and four private factory service drives. Home remains southwest; Riverside is south, Ironworks north, the Civic Centre central-east and Civic Utility on its separate eastern parcel. Freight, quarry and occupied wharf retain their northwest/northeast/southeast positions. The Graywater River is the irregular southern boundary. There are no playable islands, boats or new bridge locks.

Baseline v2 was 530 × 300: 138,868 non-water tiles and 128,174 passable tiles. V3 has 412,634 non-water tiles and **372,789 tiles connected to Home on foot: 2.908× the baseline accessible area**. The validator flood-fills actual passability, excluding water, solid buildings, blocked equipment and unreachable pockets. The larger 497,664-tile rectangle is not the area claim. Ordinary player scale and 6 tiles/s walking are unchanged. Required yard sizes remain 30 × 29, 32 × 25, 36 × 24 and 32 × 26.

| Ordinary Home-origin walk | V2 seconds | V3 seconds |
| --- | ---: | ---: |
| Starter steel | 2.1 | 2.2 |
| Starter copper | 1.2 | 2.0 |
| Starter coal | 1.0 | 1.1 |
| Riverside entrance | 23.8 | 40.7 |
| Civic entrance | 69.2 | 110.4 |
| Quarry expedition | 84.5 | 143.6 |

[Route measurements](../evidence/city-c/pacing.json) use existing obstacle-aware A* length / walking speed, excluding sprint, search, combat and waiting. Plant-to-station walks are approximately 7.7–8.7 seconds, previously 3.2–4.5. Tram Home–Civic movement is 30.2 seconds, previously 16.7, at unchanged 40 tiles/s and 1.5-second powered-stop dwell; all-powered round trip is about 69.4 seconds. This is not a campaign-duration claim.

### Roads, parcels and factory reservations

Public streets use shared explicit junction IDs and connected edges. Carriageway is 6 tiles wide, with 2-tile pavements on each side; the central road reservation has half-width 5 and an additional 2-tile building setback. All paving is drawn before carriageways, so crossing strips do not overwrite finished junctions. Home has one access neck and a 5.5-tile turning bulb. North Gardens, East Gardens and the wharf loading turnaround are the only other named public dead ends. Unknown ends fail compilation rather than entering an ignore list.

Each building declares stable ID, archetype, north/south facing, solid rectangle, separate visual bounds, reserved parcel, entrance and path; selected existing interiors retain alternate doors, furniture and content. Full rectangles are validated against roads, yards, rail, water and neighbours. Footpaths stop at pavements; 4-tile factory drives are distinct private service surfaces. The Home workshop/yard and starter resources remain compact. Templates compile explicit frontage rows, never seed-based scattering; important placements retain identity and do not disappear to satisfy a collision check.

Truck testing required real approach drives, especially around Ironworks and Civic Utility. Small local adjustments move Ironworks surface resources east of its drive, the gunsmith/cabinet south of its entrance, and Civic coal clear of its driveway. The retained Heart interaction sits on the Civic forecourt below an apartment block; the Lamplighters shelter retains a separate entrance alongside the southern apartments. No objective is removed or gated. [Eleven real machine placements per yard](../evidence/city-c/yard-layouts.json) cover production, storage, direct belts, generators and defence, with walking and further expansion room. New games remain empty.

[Truck routes](../evidence/city-c/truck-routes.json) validate every swept segment to all four yards. The existing A* retains 12,000 nodes and two-second failed-route retry. Its authored heuristic weight changes from 1.4 to 2: the measured Ironworks route then completes within budget, without broadening collision or replacing the pathfinder. [The bounded diagnostic comparison](../evidence/city-c/truck-budget.json) records the reason. Home raid staging moves to the new neck; an actual raid reaches its target without crossing authored walls.

### Shared smooth tram geometry

[riverfrontRail.ts](../../packages/sim/src/city/riverfrontRail.ts) rounds the three corners of `(72,450) → (378,450) → (378,126) → (666,126) → (666,450)` with radius-12 circular arcs and tangent straights. Its approximately 1,226.55-tile continuous centreline drives rail spacing, sleepers, tram motion, heading and passengers. Existing four-connected logical cells remain only for machinery/freight bindings. [riverfrontRailDraw.ts](../../packages/game/src/riverfrontRailDraw.ts) interpolates 20 Hz positions along the same curve, without extrapolating beyond a stop or mutating gameplay.

T1 `(72,448)`, T2 `(324,448)`, T3 `(432,124)` and T4 `(664,432)` all sit on straights. A conservative swept reservation protects the route and platforms from construction. Loading tiles remain usable, including two left-side conveyor lanes at vertical T4; its right edge is the running line and stays protected. Intermediate unpowered stops do not break through service. Tests move real steel freight and a passenger between powered Home/Civic while Riverside/Ironworks remain unpowered. The browser followed the moving car through a bend and its Civic dwell.

### Architecture, light and rendering

The existing original SVG set is extended by [build-downtown.py](../../packages/game/public/art/riverfront/build-downtown.py): town hall, apartment, office, department store, shopping arcade and parking, each with three roof variants and a separate ground floor. The complete asset set has 56 SVG layers. Downtown contains the town hall/square, two apartment blocks, two offices, two department stores, two arcades and two parking structures. Roof form and facade tiers distinguish these from suburban houses and garages. Shops/terraces provide the transition. Garden trees and the civic memorial have real collision; windows are unlit art and runtime supply supplies the lit state.

[riverfrontDraw.ts](../../packages/game/src/riverfrontDraw.ts) keeps culled sprites, joined roads, civic paving and explicit physical props aligned with the canonical definition. Existing roof hysteresis, mouse flashlight, local wall-occluded light pools, dim interiors and independent HUD remain intact. Six Home fixtures light from a real fuelled generator and go dark without fuel. All three compounds retain exactly two defenders and the same roster/stats; their occupied interiors and extracted core state remain intact. No offscreen production or attack simulation is disabled.

A focused profile found `blockLights` repeatedly considering all track machinery during interaction scans. Its transient revision-keyed fixture list now includes only Lamps, Arc lamps and Floodlights; live power, damage and repair state is still read each time. Existing legacy light/repair tests pass; the historical always-lit Home check now explicitly selects the preserved procedural constructor. Authored finite Home lighting is independently covered by the city tests.

### Evidence and remaining limits

The [compact validator](../../packages/sim/src/city/validateRiverfront.ts) checks entire solid footprints, service drives, public-road graph/dead ends, required IDs/archetypes, accessible entrances, bindings, continuous rail/tangents and straight platforms. [Validation](../evidence/city-c/validation.json), [31 focused tests](../evidence/city-c/tests.txt), [build](../evidence/city-c/build.txt), [lint](../evidence/city-c/lint.txt), [defenders](../evidence/city-c/guards.json) and [browser record](../evidence/city-c/visual-review.json) provide the implementation evidence. The ordinary full campaign/10,000-seed suites were not rerun for this authored map pass.

[Actual game overview](../evidence/city-c/after-overview.png) · [Annotated canonical layout](../evidence/city-c/layout-overlay.svg) · [Home](../evidence/city-c/after-home.png) · [Downtown](../evidence/city-c/after-downtown.png) · [Apartments](../evidence/city-c/after-apartments.png) · [Moving bend](../evidence/city-c/after-moving-bend.png) · [Civic stop](../evidence/city-c/after-moving-station.png) · [Walked interior](../evidence/city-c/after-walked-interior.png) · [Powered night](../evidence/city-c/after-powered.png).

Final browser checks pass with no page errors: 2,160 sampled frames follow the tram, including 77 curved frames and Civic dwell. Regular adjacent frames move at most 1.22 tiles; capture gaps are excluded from that measurement. The final 180-frame populated downtown sample has 16.7 ms median / 33.3 ms p95 frame intervals, 33.5 ms maximum, and 3.05 ms drawing EMA / 8.8 ms maximum. It advances three simulation seconds at normal 1×. This improves the initial 66.7 ms p95 observation after the fixture lookup fix, but does not establish universal 60 fps. Types/build, lint, docsync, 111 referenced paths and whitespace checks pass; freshness retains the same 12 historical stale/unstamped files. Screenshots cover all four factory areas, the three compounds, a residential junction, a northern court, downtown, roof entry/exit, daylight/night and a recovered core. Map clicks preserve the player and actual held movement crosses the house doorway. These are debug-assisted engineering observations, not a human playtest or release/campaign-balance verdict. Existing Q07, human and broad performance gates remain open. Vite's existing large-bundle warning remains. No commit, push or deployment was performed.

## CITY-B — expanded town and atmosphere (historical v2 evidence)

The owner’s map-expansion follow-up is implemented locally in the existing engine. New games now use **`riverfront-arc-v2`, 530 × 300 tiles and 90 buildings**, compared with v1’s 360 × 230 and 46. The added space contains residential frontages between Home and Riverside, Westridge streets, Old Town terraces and shops, northern industrial sheds, civic shops/square, waterside workshops and dock warehouses. Essential content retains its stable identity. Home and three plants, four fixed tram stops, resource yields, inventories and finite supply remain authoritative; the factory yards remain 30 × 29, 32 × 25, 36 × 24 and 32 × 26.

Open [the local game](http://127.0.0.1:5178/?view=world), reload the page and choose **New Game**. This preview serves the rebuilt `packages/game/dist`. Earlier authored v1 saves are rejected with an explicit original-build/New Game message, preserving the save unchanged. Old procedural saves retain their old geometry. V2 saves validate and reload normally. No push, deployment or release approval is implied.

### Art, interiors and compounds

The canonical [definition](../../packages/sim/src/city/riverfront.ts) owns all physical positions. [riverfrontDraw.ts](../../packages/game/src/riverfrontDraw.ts) integrates 32 original SVG layers in `packages/game/public/art/riverfront`: house, terrace, garage, workshop, warehouse, shop, civic and utility, each with three roof variants and one floor. `build-assets.py` is their reproducible original drawing source. Sprites use a 320 × 256 canvas, top-left parcel anchor and explicit parcel display size, with consistent northwest highlights/southeast shadows. Ground walls, furniture, fences and door openings come from simulation data, never roof pixels. No reference image is used as a backdrop.

Housing has pitched/shingled roofs, chimneys and small planted frontages; workshops and warehouses have metal roofs/skylights; shops have striped awnings; civic buildings have domes and formal entrances; utilities have tanks, pipes and chimneys. Boarded background entrances remain non-interactive. Selected existing homes keep notes, shelters and the saved shortcut; existing workshop/artifact/recruit content remains accessible. Roof fade/exit hysteresis persists independently of collision and lighting. Side doors and connecting equipment-room partitions make compound access physically legible.

Founders Court’s turning circle is reduced from 8 to 5.5 tiles in radius around a small island, with an obvious southern neck, adjacent houses/garages, distinct workshop and supplies. Factory slabs have joints, drainage and restrained edges, with their useful interior space preserved. Salvage is rendered as metal stacks, copper coils and ore/coal piles without changing items or yields. Continuous paved edges replace the normal-world tile stair-step outline; underlying road and collision data remain intact.

Northwood has freight containers, loading equipment and a converted depot. Quarry has extraction machinery, stores, boulders and a conversion works. East Wharf has warehouses, dock crane/cargo and the waterfront installation. Each stronghold has a 30 × 23 main structure with two approaches and connected equipment spaces. Internal conduits and machinery follow existing core state; extraction stops the associated animated/emissive installation and survives reload. Existing enemy roster, counts, stats, abilities and waves are unchanged. No bosses, alarms, extraction battles or new reward economy were added.

### Lighting and visibility

The source bug was ordering: the old darkness layer sat below roofs, while outdoor ambient switched abruptly at night and rooms used outdoor daylight. The authored view now composes a culled local light mask above roofs, below the visible engineer and HUD. [riverfrontLighting.ts](../../packages/game/src/riverfrontLighting.ts) blends daylight, dusk and night; entered unpowered rooms have separate dim ambient. Closed roofs no longer receive an interior-shaped dark patch. Existing mouse flashlight settings remain active. Doors admit bounded daylight, powered lights create warm pools and abandoned areas remain cool and dark.

Light sources are existing `blockLights`, regional supply and unrecovered visible cores. Floodlights retain their existing cone angle/direction. Small ray fans stop at actual opaque walls/props and intact built walls/barricades. Offscreen effects are culled; the renderer no longer repaints the unused full-city authored light mask. Simulation darkness, `litAt`/light-mask rules, discovery and enemy modifiers are unchanged. The 18 selected fixtures keep the existing 2 kW budget; window indicators follow supply rather than a permanent restoration flag. The recorded night fixture has six Home lights lit with 50 coal in a real generator, zero with no fuel, at the same time and camera.

### Routes and focused verification

The fixed line remains **Home → Riverside → Ironworks → Civic**, at the unchanged 40 tiles/s and 1.5-second dwell. Intermediate unpowered stops are skipped. All four yards remain reachable by the existing road-sized truck and its bounded swept-footprint A*. No pathfinder replacement or offscreen simulation suspension was introduced.

| Home-origin trip | V1 walking seconds | V2 walking seconds |
| --- | ---: | ---: |
| Starter steel | 2.1 | 2.1 |
| Riverside entrance | 12.1 | 23.8 |
| Civic entrance | 40.8 | 69.2 |
| Freight approach | 16.3 | 23.8 |
| Quarry approach | 52.5 | 84.5 |
| Wharf approach | 53.3 | 82.4 |

These are measured obstacle-aware path lengths divided by the existing 6 tiles/s walk speed, with no sprint, fighting or searching. Home–Civic tram movement increases from 10.2 to 16.7 seconds; the measured all-powered door-to-door estimate is 29.3 seconds before waiting, with a 42.4-second full round trip. Plant-to-platform last miles remain about 3.2–4.5 seconds. These measurements do not claim to resolve the owner’s reported five-minute completion; campaign length and victory requirements remain a later design task.

Final verification: **29/29 focused tests pass**, full type/build checks and final game rebuild pass, and the repository lint command passes. Docsync and referenced paths pass; freshness retains the same 12 historical stale/unstamped files, which were not regenerated into v2. Vite retains its existing bundle-size warning. Three short defender checks retain exactly two existing enemies per compound on valid ground. Final 180-frame Home/compound Chrome samples have 16.7 ms median and 16.8 ms p95 frame intervals, with maximum drawing calls of 3.9/2.8 ms. The samples advanced two and three simulation seconds respectively under the unchanged normal player-time controls; no fast-forward was used. This is a short debug-assisted rendering sample, not a universal performance result.

Current evidence lives in [city-b](../evidence/city-b/). `layout.json` records no parcel overlaps, road-centre/building intersections, yard encroachment, blocked track tiles, core/prop overlaps or unreachable entered buildings. `truck-routes.json` records four successful swept routes. `tests.txt` covers the existing city/conveyor/UI cases plus v1 save rejection, side entrances and resource clearance. Factory checks temporarily place the same eleven real parts with expansion and walking room; no test layout is granted in New Game. Bootstrap uses ordinary mining/movement commands. Core/plant tests use disclosed debug relocation/materials and remove guards to isolate interaction; this is not a combat completion claim.

`review.cjs` captures the actual 1600 × 1000 Chrome game: overview, Home, each district, house exterior/interior/exit, day/dusk/night, powered/unpowered Home, all three compounds, and recovered core state. It also checks map clicks leave the player in place, browser save/reload and short 1× running samples. Debug actor/camera/time positioning is recorded in `visual-review.json`. Before images and pacing are preserved alongside the new evidence. Human play, Q07 and release/performance gates remain open. No seed sweep or full campaign audit was run.

Screenshots: [overview](../evidence/city-b/after-overview.png), [Home](../evidence/city-b/after-day.png), [street](../evidence/city-b/after-residential-street.png), [house inside](../evidence/city-b/after-house-interior.png), [dusk](../evidence/city-b/after-dusk.png), [powered night](../evidence/city-b/after-powered.png), [freight interior](../evidence/city-b/after-freight-interior.png), [quarry](../evidence/city-b/after-quarry.png), [wharf](../evidence/city-b/after-wharf.png).

## Historical CITY-A implementation and evidence (v1)

The following records the completed preceding revision. Its v1 dimensions, screenshots and timings are retained for comparison; CITY-B above owns the current map and launch/save instructions.


CITY-A is complete (engineering and inspected visual review, 2026-09-09). It implements the owner’s 9 September 2026 Riverfront Arc brief in the existing top-down game. This is a code-native playable map, not the reference image used as a backdrop. The southwest Home, waterfront Riverside, northern Ironworks, eastern Civic district and three outer strongholds follow the reference’s composition. The written Home → Riverside → Ironworks → Civic stop order overrides its inconsistent numbering.

## World and launch

The canonical definition is [riverfront.ts](../../packages/sim/src/city/riverfront.ts): `riverfront-arc-v1`, version 1, 360 × 230 tiles. It owns terrain routes, parcel footprints/doors, yards, resource locations, service cabinets, lights, props, stops, plants, strongholds, artifacts, crews and projects. `riverfrontCampaign.ts` binds existing campaign mechanics to that data. Default `createCampaign` and bare New Game load it; changing a seed does not relocate it. No generated essential placement or old rail-kit spawn runs after this constructor.

Open [the rebuilt local game](http://127.0.0.1:5178/?view=world) and choose New Game if an older save is selected. The current Python preview serves `packages/game/dist`; restart locally with `python -m http.server 5178 --bind 127.0.0.1 --directory packages/game/dist`. This task does not publish or push anything.

Old procedural saves retain their original geometry and coordinates. They do not acquire authored parcels; Continue is not a conversion. A known old 800 × 800 campaign save was loaded in the focused check. Old command histories are not advertised as replay-complete against the new constructor. Authored saves validate map dimensions/version, fixed site and tram bindings, and saved cleared/opened/visited IDs. Reward ownership and ordinary campaign validators still run. Unsupported authored IDs are rejected without erasing the original save.

## Authored places and factory capacity

Founders Court has a short dead-end street, a visible turning circle, workshop at the back, salvage garages and one southern neck. T1 stands just outside that neck. Steel salvage, copper and coal keep their existing item/yield semantics and hand-gathering capability. New games have empty carried/store stock and no generator, assembler, fuelled factory or free defensive line.

| Destination | Buildable yard | Representative temporary layout |
| --- | --- | --- |
| Home / Founders Court | 30 × 29 | 11 real parts, walking route and spare Assembler II footprint |
| Riverside Works | 32 × 25 | Same layout; pumping tower, tanks and separate service entrance |
| Ironworks | 36 × 24 | Same layout; turbine hall/chimney beside substantial industrial yard |
| Civic Utility | 32 × 26 | Same layout; cooling towers, municipal yard and nearby civic dome |

The 11-part check places a chest → belt → assembler → belt → chest → belt → turret line, two generators, another chest and another turret. A further Assembler II fits with walking/repair space. These fixtures are test setup, not starting gifts. [Exact layouts](../evidence/riverfront/yard-layouts.json). Building, yard and non-furniture prop overlaps are absent in the focused layout probe; the fixed running line has no blocked tiles. Road centrelines avoid buildings.

Buildings have solid ground footprints independent of roof overhangs. Westridge and Old Town each have three enterable homes. Story notes, the Surveyors’ shelter and a return shortcut make those visits purposeful; other rewards remain in the salvage workshop, quarry cache and wharf store. Boarded buildings have no enter/reward prompt. Beds/tables, fences, service cabinets and substantial props collide. Same-map interiors hide only the entered roof, fade over roughly 0.2 seconds and restore beyond a 0.6-tile exit margin. The foreground south wall is shortened visually while inside; its collision remains. Open three-tile doors fit small actors and normal large-body paths where surrounding space allows.

`court-brush` and `oldtown-alley` are marked clearable debris. `westridge-gate` opens from its eastern/inside approach, providing a shorter return route. Ordinary E commands update saved state, invalidate ground and increment flow revision; navigation sees the change. Permanent buildings are not a general demolition resource.

The three alien cores occupy powered equipment inside the freight depot, quarry works and wharf warehouse. Existing guardians/encounters and reward mechanics are retained, not replaced with new bosses. Every plant accepts any eligible recovered core, supplies finite 600 kW and becomes an attack target. The existing minor/major raid cadence is unchanged. Home’s first minor assault stages south of the access neck; Riverside’s scheduled assault reaches its actual plant room. Authored regions are power service areas, never frontage/capture progression. Eighteen selected street fixtures charge 2 kW each to their real regions; powered windows/equipment follow regional supply rather than a permanent unlock glow.

## Tram, access and pacing

Exactly four fixed platforms use one continuous, unique, unbranched route: **T1 Founders Court → T2 Riverside → T3 Ironworks → T4 Civic / East Wharf**. Two powered stops enable automatic passenger/freight service, skipping unpowered intermediates. Current authored settings are 40 tiles/s and 1.5 seconds dwell; legacy service tuning is unchanged. Track and the north boarding strips reject ordinary buildings. Designated side tiles permit belts/fast belts/inserters so platform protection does not disable real freight connections.

The focused freight/passenger case powers Home and Civic only, transfers 20 steel using existing station rules, boards, travels and exits with conserved ownership. A separate ordinary chest → belt → T4 → belt → chest case transfers 12 steel. There is no shared/global freight inventory. Existing road-sized truck navigation can reach service positions at all four factory yards from its actual restored-station spawn; each swept route segment is collision-checked.

Scale targets were seconds for starter gathering access, around 10–20 seconds for the first local destinations, and under a minute of uninterrupted walking for the far expeditions. The measured navigation paths at the unchanged 6 tiles/s walk speed are:

| Trip from the Home doorway | Uninterrupted walking |
| --- | --- |
| Starter steel / copper / coal access | 2.1 / 1.2 / 1.0 s |
| First optional electrical salvage workshop | 13.7 s |
| Riverside entrance / T2 platform | 12.1 / 15.8 s |
| Home to T1 | 5.0 s |
| Northwest freight depot | 16.3 s |
| Civic entrance | 40.8 s |
| Quarry / far wharf expedition | 52.5 / 53.3 s |

Plant doorway → local stop is 4.0 s at Riverside, 4.5 s at Ironworks and 3.2 s at Civic. Home → Civic by tram is about 22.8 s including walking access and all three departure dwells, before waiting. The all-powered round trip is about 29.4 s: arrival timing adds 0–29.4 s (about 14.7 s for uniformly timed arrivals). A poor connection can still make walking quicker; average nominal travel is about 37.5 s versus 40.8 s on foot, while automatic freight carries its own load. With only the endpoints powered, door-to-door travel before waiting is about 19.8 s. The first Riverside trip remains convenient on foot.

These are lengths of actual collision-valid A* paths divided by speed, and actual fixed-route/dwell calculations; they exclude combat, searching, sprinting and player hesitation. They are not a human expedition-time claim. The bounded bootstrap test also executes ordinary movement and hand-mining commands. [Pacing data](../evidence/riverfront/pacing.json), [entrance paths](../evidence/riverfront/travel.json), [truck routes](../evidence/riverfront/truck-routes.json).

## Navigation and presentation

The existing heap A* in `walk.ts` was suitable and was retained. Failures came from procedural geometry/content disagreement, a legacy failed-block-walk teleport, greedy local pursuit, stale paths and collision/LOS differences. Authored ground now feeds player collision, pedestrian paths, visibility and the vehicle’s existing footprint checks. Diagonal corner cuts are disallowed. Engineer WASD checks its body corners; authored failed scripted walks cancel rather than teleport. Dynamic machine revision changes replan valid goals. Sight remains blocked by permanent walls, substantial scenery and intact player-built walls/barricades, regardless of roof state.

`cityNavigation.ts` owns shared transient follower/local-hostile plans: FIFO admission, at most 3 searches/tick, 8,000 nodes/search, 0.75-second moving-goal refresh and 1-second failed-route retry. Breakers use one extra tile of clearance. Next-tile reservations and existing crowd separation keep moving actors out of walls and reduce doorway stacking; congestion is not written into permanent ground. Existing campaign raid reverse fields remain shared/bounded and use physical attack approaches, crowd spacing and their existing defensive-breach abilities. No universal scenery destruction was added. `flow.rev`, `invalidateGround` and equipment removal/recovery invalidate cached collision/paths. Caches are transient after reload.

The single logistics truck retains its separate 2 × 3 road-only swept-footprint A*, turns, storage and work state. Its 12,000-node search originally exhausted the Civic trip budget. An authored-only 1.4 heuristic weight now finds all four sampled factory routes; shortest-route optimality is not required. Existing 2-second retry and explicit blocked/budget feedback remain. Measured complete route checks, including swept segment validation, were approximately 6–36 ms on WSL Node 22. Dynamic work routes validate segments before moving and wait/retry if blocked. The tram continues to follow only its fixed line.

`riverfrontDraw.ts` draws culled authored ground, walls, furnishings, roofs, resources, utility silhouettes and vegetation. It is simplified code-native art, not a photorealistic asset overhaul. Map/minimap share this geometry, show H/P/T infrastructure glyphs and named plant state, and keep undiscovered cores as broad search areas. Tracking resolves to public doors. Recovered rewards and completed recruitment lose their action prompts; obsolete rail build entries remain unavailable. Block boundaries, interaction/perception radii, procedural spawn marks and raw identifiers are absent from normal world presentation. The action bar, Backpack, overlays, camera stability, visible player marker and 1×/pause controls remain.

## Focused evidence and limits

- 28/28 checks in `combined-final.txt`: 13 authored-city cases plus 15 retained direct-conveyor/UI guidance/settings checks. This covers empty bootstrap, placement capacity, every authored door/site/stop reachability, old/new saves, three interchangeable plant bindings, artifact/Foreman interaction, freight/passengers, real scheduled Home/Riverside raids, wall sight, dynamic obstacles, return gate, clearing and narrow/large/unreachable paths.
- Final build/type checks, lint, docsync and diff whitespace checks pass. `freshness.txt` retains the same 12 stale/unstamped historical evidence files as the preceding correction handoff; they are not regenerated into this map. Vite retains its existing large-bundle warning.
- `review.cjs` captures the actual game at 1600 × 1000: Home, overview, residential exterior/interior/exit, all plants, freight installation/interior and reward room. Camera/engineer relocation and pause are explicit debug assistance. Images are inspected, not inferred from compilation.
- `live-ui.cjs` verifies Tab open/close camera stability, map clicks without movement, broad core-search text and two loaded raid scenes at normal 1×. It records browser page errors and 180 rendered-frame samples per raid. Final Windows headless Chrome samples contain 11 Home attackers and 9 Riverside attackers, each advancing exactly 3 simulation seconds over 180 frames at 1×. Both have median 16.7 ms and p95 16.8 ms frame intervals, no page errors, and worst sampled drawing calls of 9.1/8.4 ms. This is browser rendering evidence, separately reported from headless simulation cost.
- The scheduled Home raid has 11 attackers. Its 281 measured headless simulation ticks had p95 below 1 ms and maximum about 8.1 ms against a 50 ms tick. The focused local navigation case reused one search over 600 movement calls. These are short, declared scenarios, not a universal performance guarantee.

Core/plant/raid fixtures use debug-supplied materials, removed guards or clock positioning where appropriate. Bootstrap movement and gathering are ordinary commands; build layout setup relocates the engineer, and the assembler proof injects a small input batch. This is engineering verification, not an unaided campaign completion or balance verdict. No exhaustive seed campaign or repo-wide regression run was requested for this rebuild. Prior full-suite/freshness findings and Q07/human/release gates remain open in their existing owners.

Screenshots: [overview](../evidence/riverfront/overview.png), [Home](../evidence/riverfront/home.png), [residential street](../evidence/riverfront/residential.png), [inside](../evidence/riverfront/interior.png), [after leaving](../evidence/riverfront/interior-exit.png), [Riverside](../evidence/riverfront/riverside.png), [Ironworks](../evidence/riverfront/ironworks.png), [Civic](../evidence/riverfront/civic.png), [freight depot](../evidence/riverfront/freight.png), [reward room](../evidence/riverfront/reward-room.png). Next human step: a fresh Riverfront playtest, especially the feel of travel, combat approaches, factory expansion and visual clarity.
