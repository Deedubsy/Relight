# Relight — World Data and Assets (Unity migration reference)

Worker C draft, 2026-09-11. Branch `main` @ `586d4525` plus the uncommitted GP-* working tree described in `Unity/Docs/SOURCE_INVENTORY.md`.

## 1. Purpose, status legend and canonical-source statement

### 1.1 Purpose

This document is the single authoritative home for: **world data content and formats**, **coordinates and stable IDs**, the **asset inventory and its provenance**, and the **export → import data mapping** that a Unity port must satisfy. It describes *what data exists and in what shape*, not what the data means for play.

Deliberately **not** covered here (one authoritative home per fact):

- Gameplay rules, balance values, item/recipe/enemy content — `GAME_DESIGN.md` and `CONTENT_CATALOGUE.md` (Worker A).
- Unity code architecture, scene/assembly layout, sim-port strategy, save/serialisation design — `TECHNICAL_ARCHITECTURE.md` (Worker B).
- Screens, HUD, controls, onboarding, UI defects — `UI_AND_ONBOARDING.md` (Worker C, companion file).

Scope guard: this is **Relight only**. The owner's separate machine-survival game (cosmetic work crews, machine fortress, five-day raid schedule) is not part of this world and does not appear here.

### 1.2 Status legend

| Label | Meaning |
|---|---|
| **Implemented and retained** | Present in the current tree, exercised by code, intended to carry into Unity. |
| **Approved but not implemented** | Owner-approved or plan-approved, no working code yet. |
| **Implemented but needs correction** | Code exists and runs, but a recorded defect or stale artefact means Unity must not copy it as-is. |
| **Unresolved** | No decision on record; needs the coordinator or the owner. |
| **Retired** | Superseded; retained only as historical evidence. |

Unverified statements are prefixed **UNCERTAIN:**. Every fact carries a `path:line` or `path §` reference.

### 1.3 Canonical-source statement — is the Tiled export canonical?

**No. The Tiled TMJ export is not canonical, it is export-only, and the copy currently in the tree is stale.** Status: **Implemented but needs correction** (as a *reference artefact*; it is correct that it is non-canonical, incorrect that a stale copy is retained without a warning banner).

Evidence:

1. `maps/tiled/riverfront-v4/export-report.json` records `"buildings": 287`, `"props": 133`, `"sourceSha256": "86f5c477…"`, `"importSupported": false`. The live canonical map has **479** buildings and **370** props (`packages/sim/src/city/riverfront.ts:12516`, `:30896`). The export therefore predates CITY-F density work by a whole content generation.
2. `maps/tiled/riverfront-v4/verification.json` repeats `"importSupported": false`.
3. `packages/tools/src/exportTiled.ts:3` — "Editable, portable Tiled snapshot of the canonical authored map. **No game-state mutation/import.**"
4. `packages/tools/src/exportTiled.ts:15` refuses to overwrite an existing `riverfront.tmj`: "Output already contains a map … Choose a new output folder to preserve Tiled edits." — i.e. the file is a user artefact, never a regenerated build product.
5. `docs/EXPLORATION_DEFENCE_PLAN.md:68` (D-GP-14): "TMJ export-only … **Do not build a Tiled round-trip importer.**"
6. `docs/PHASER_EDITOR.md:31`: "**Tiled remains export-only.**"
7. `docs/PHASER_EDITOR.md:69`: gameplay objects are exported multiplied by 32 and labelled **"reference only"** — "Moving them in Tiled or Phaser does not move gameplay."
8. `docs/Implementation/RIVERFRONT_CITY.md:13`: the exported v4 map is "the **earlier pre-density** v4 map … This is **export-only**."
9. `packages/tools/src/exportTiled.ts:71` names one whole layer `Gameplay references — EDIT TYPESCRIPT, not imported`.

**The canonical world source is the TypeScript + Phaser-Editor chain:**

| Rank | Artefact | Role |
|---|---|---|
| 1 | `packages/sim/src/city/riverfront.ts` | The authored city: scalars, buildings, props, roads, paths, tram, resources, regions, sites. Sole runtime truth. |
| 1 | `packages/sim/src/city/gameplaySites.ts` | Canonical first-region gameplay placement (camps, gates, arena). |
| 2 | `maps/phaser/manifest.json` + `maps/phaser/RiverfrontCity.source.txt` | The authoring baseline binding editor object UUIDs to canonical building IDs, with a content hash of the source. |
| 2 | `packages/game/src/editor/RiverfrontCity.scene` | The Phaser Editor v5 native scene the owner edits (959 objects). |
| 3 | `maps/tiled/riverfront-v4/**` | **Reference/interchange only.** Stale. Never re-imported. |

Enforcement (all **Implemented and retained**): `validateRiverfront` (`packages/sim/src/city/validateRiverfront.ts`) for structural acceptance; `validateFirstRegion` (`packages/sim/src/firstRegion.ts`) for gameplay geometry; the source-hash and canonical-hash guards in `packages/tools/src/phaserCity.ts`; the runtime template drift assert in `docs/evidence/city-c/authorFullCity.py:231-233`; and `packages/tools/src/authorCityDensity.ts:13` ("City density source/scene drift: reconcile subsequent edits before rerunning.").

**Unity consequence:** do not write a TMJ importer. Port from `riverfront.ts` (and its sibling TS modules) through a new, purpose-built export, described in §7. That covers **existing** content only; content that does not exist in these sources is authored in the Unity Editor after handover and checked by a port of the `validateRiverfront` / `validateFirstRegion` rules (§4.7.1, DECISIONS.md U-M-15).

---

## 2. World dimensions and coordinate conventions

**Status: Implemented and retained** unless noted.

### 2.1 The three coordinate spaces

| Space | Unit | Definition | Source |
|---|---|---|---|
| **Sim tile** | 1 tile | The only space gameplay uses. Integer `(x, y)`; the flat index is `t = y * width + x`. | `packages/sim/src/city/riverfront.ts:33860` (`lineTiles` computes `t = y*RIVERFRONT.width + x`); the same convention appears throughout `ground.ts`, `riverfrontRail.ts`, `opening.ts:13-16`. |
| **Phaser pixel** | 32 px per tile | Renderer and Phaser Editor space. `TILE_PX = 32`. | `packages/sim/src/tiles.ts:17`; `docs/PHASER_EDITOR.md:19` ("pixels at **32 pixels per simulation tile**, with 32 px snapping"). |
| **Tiled pixel** | 32 px per tile | Identical scale to Phaser px; the exporter multiplies every tile coordinate by `P = TILE_PX`. | `packages/tools/src/exportTiled.ts:18,24` (`x: x*P, y: y*P`). |

Phaser px and Tiled px are the **same** scale. There is no second art scale in the world data (building *artwork* tiles are 320×256 px — see §8 — but they are placed by their top-left at `objectalignment: 'topleft'`, `exportTiled.ts:39`).

### 2.2 Origin and axes

- Origin `(0,0)` is the **top-left** tile. `+x` is east/right, `+y` is south/down. Screen-space and tile-space axes agree; there is no Y flip anywhere in the sim or the exporter.
- **UNCERTAIN:** no code comment states the handedness explicitly; it is inferred from `t = y*w + x` row-major indexing plus `objectalignment: 'topleft'` and the unmodified `y*P` in `exportTiled.ts:24`.
- **Unity note:** Unity's default 2D world has `+y` **up**. A Unity port must either flip Y on import (`unityY = (height - 1) - simY`) or run the whole city in a Y-down tilemap. Choose once and record it; mixed conventions will silently mirror the city. Recommend flipping at import so that Unity-native tooling (Tilemap, Cinemachine, physics) behaves normally, and keeping sim tile coordinates unflipped inside the ported simulation. This is a **proposal** — Worker B owns the final call in `TECHNICAL_ARCHITECTURE.md`.

### 2.3 Map scalars

From `packages/sim/src/city/riverfront.ts:10` (the `RIVERFRONT` definition literal; its type is at `:9`):

| Field | Value | Meaning |
|---|---|---|
| `id` | `riverfront-arc-v4-editor-ac16d9188c05` | Content-derived map ID (`riverfront.ts:5`). |
| `version` | 4 | Authored-map generation. |
| `width` × `height` | **864 × 576 tiles** | = 27,648 × 18,432 px at 32 px/tile; 497,664 tiles total. |
| `cell` | 36 | Planning-cell side in tiles → a **24 × 16 lattice** (864/36 = 24, 576/36 = 16) of 384 cells. |
| `roadHalf` | 5 | Half-width of a public road carriageway, tiles. |
| `pavement` | 2 | Pavement width either side. |
| `setback` | 2 | Building setback from pavement. |
| `railHalf` | 3 | Half-width of the tram bed. |
| `tramRadius` | 12 | Fillet radius on tram corners. |
| `tramSpeed` | 40 | Tram speed (tiles/s). |
| `tramDwell` | 1.5 | Stop dwell, seconds. |
| `riverY` | 477 | The river's top edge row. |
| `homeOrigin` | `[58, 345]` | Home block origin tile. |
| `court` | `{x: 72, y: 374, radius: 5.5}` | Founders Court centre. |
| `homeRaidY` | 391 | Raid approach row for Home. |
| `lightKw` | 2 | kW per street light (the *value* belongs to `GAME_DESIGN.md`; it is listed here only because it is a field of the map record). |

Renderer chunking is separate: `packages/sim/src/ground.ts:29` — `CHUNK = 32` tiles, "a lattice cell exactly" (the comment is inaccurate against `cell = 36`; **Implemented but needs correction** — cosmetic comment drift only, the value 32 is what the code uses at `ground.ts:101,416,551-554`).

### 2.4 Content counts (current canonical map)

| Collection | Count | Source |
|---|---|---|
| Buildings (`Parcel[]`) | **479** | `riverfront.ts:12516` |
| Props (`MapProp[]`) | **370** | `riverfront.ts:30896` |
| Road polylines | 371 | `RIVERFRONT.roads` |
| Road graph nodes / edges | 210 / **371** | `RIVERFRONT.roadNodes`, `roadEdges` |
| Entrance/footpath polylines | 479 | `RIVERFRONT.paths` (one per building) |
| Named squares | 113 | `RIVERFRONT.squares` |
| Resource patches | 11 | `RIVERFRONT.resources` |
| Street lights | 18 | `riverfront.ts:341` |
| Substations | 9 | `riverfront.ts:416` |
| Service drives | 4 | `riverfront.ts:480` |
| Regions | 9 | `RIVERFRONT.regions` |
| Factory yards | 4 | `RIVERFRONT.yards` |
| Service areas | 1 | `riverfront.ts:81` |
| Tram stops | 4 | `RIVERFRONT.stops` |
| Recruits | 7 | `RIVERFRONT.recruits` |
| Projects | 9 | `RIVERFRONT.projects` |
| Plants / cores / artifacts | 3 / 3 / 3 | `RIVERFRONT.plants`, `.cores`, `.artifacts` |

Building kind histogram (`Parcel.kind`, enumerated from `riverfront.ts:12516`): house 293, shop 76, terrace 32, garage 26, workshop 16, warehouse 15, office 5, apartment 5, utility 4, department 2, arcade 2, parking 2, townhall 1. Facing histogram: S 305, N 115, E 31, W 28. **22** buildings carry `enterable: true`; these are exactly the 22 `FIXED` buildings in `maps/phaser/manifest.json` (interiors and campaign bindings).

### 2.5 Record formats

`packages/sim/src/city/riverfront.ts:7`:

```ts
export interface Parcel {
  id: string; name: string;
  x: number; y: number; w: number; h: number;           // collision footprint, tiles
  kind: BuildingKind; facing: 'N'|'S'|'E'|'W';
  parcel: {id: string; x: number; y: number; w: number; h: number};  // reserved plot
  visual: {x: number; y: number; w: number; h: number}; // artwork footprint (may overhang)
  path: [number, number][];                             // entrance path polyline, tiles
  enterable?: boolean; door?: [number, number]; doors?: [number, number, number, number][];
  variant?: number; compound?: string; colour?: number; content?: string; note?: string;
}
```

`riverfront.ts:8`:

```ts
export interface MapProp {
  id: string; x: number; y: number; w: number; h: number;
  kind: 'tree'|'debris'|'gate'|'furniture'|'fence'|'tank'|'crane'|'container'|'converter'|'excavator'|'rock'|'statue';
  clearable?: boolean;
}
```

`BuildingKind` (`riverfront.ts:6`): `house | terrace | garage | workshop | warehouse | shop | utility | civic | townhall | apartment | office | department | arcade | parking` (14 members; `civic` is declared but unused in the current data).

Three separate rectangles per building matter for the port: **`x/y/w/h`** is collision, **`parcel`** is the reserved plot the editor validates against, **`visual`** is where the artwork sits. They are not interchangeable.

### 2.6 Walkable, blocked and the tile enum

Terrain kinds (`packages/sim/src/tiles.ts:23`):

```ts
export const T_STREET = 0, T_GROUND = 1, T_RUBBLE = 2, T_INERT = 3,
             T_RIVER = 4, T_DEPOSIT = 5, T_PATCH = 6;
```

with display names at `tiles.ts:25`: `['street','ground','rubble','inert','river','deposit','patch']`.

The runtime world grid is `Ground` (`packages/sim/src/ground.ts:51`): parallel typed arrays of `kind`, `variant` and `patch` over `tw × th` tiles, chunked at `CHUNK = 32`, with `railYard` and block ownership fields. `walkable(G, tx, ty)` at `ground.ts:440` is the single passability predicate — the *rules* it encodes belong to `GAME_DESIGN.md`; the *format* is a per-tile byte array. Building collision comes from `Parcel.x/y/w/h`, not from the terrain array.

`GroundTiles` (`ground.ts:558`) — `{w, h, kind: Uint8Array, variant: Uint8Array, patch: Uint8Array}` — is the compact transferable form and is the natural shape for a Unity Tilemap import (§7).

### 2.7 The opening enclosure

`packages/sim/src/city/opening.ts:5`:

```ts
export const OPENING_LAYOUT = { frame: 24, gateWidth: 4 } as const;
```

A 26 × 26 tile ring (`frame + 2`, `opening.ts:8`) around `homeOrigin`, with a 4-tile gate on each of four sides generated in order E → S → W → N (`opening.ts:13-16`, indices 0..3). `opening.ts:28` renames the HQ place to **`'Home Court'`**. This is the `culdesac-v1` opening named in `packages/sim/src/rules.ts:7`.

### 2.8 Lighting and daylight

- **Authoritative:** the sim owns `lightMask` / `litAt` in `packages/sim/src/light.ts`. Light affects gameplay.
- **Presentation only:** `packages/game/src/riverfrontLighting.ts` computes a `daylight(t)` smoothstep for the renderer tint. It must never feed gameplay.
- Day length constants live in `packages/sim/src/rules.ts:7` (`CAMPAIGN_RULES = {daySeconds: 1200, daylightSeconds: 900, …}`). The **rule** is Worker A's (`GAME_DESIGN.md`); it is cited here only because §7's export must carry the light positions and `lightKw` field.
- Unity: street lights are 18 point positions plus one kW scalar. The sim light mask is a per-tile computation, not authored data — port the algorithm (Worker B), export only the emitter positions.

---

## 3. Regions and districts

**Status: Implemented and retained.** `RIVERFRONT.regions` is a `[name, x, y][]` triple list — a **label anchor**, not a bounds rectangle. There are no authored district bounds in the data; district membership is derived from the 36-tile planning lattice and block ownership at runtime.

| # | Region name | Anchor tile | Role |
|---|---|---|---|
| 1 | Founders Court | (72, 372) | Player start / Home. Campaign origin. |
| 2 | Riverside Works | (306, 414) | First expansion target (`riverfrontSpec.target = [306,414]`, `riverfront.ts:33879`). |
| 3 | Ironworks | (430, 78) | Mid-campaign industrial district. |
| 4 | Civic Utility | (728, 408) | Late-campaign civic district. |
| 5 | Westridge Homes | (126, 230) | Residential filler district. |
| 6 | Old Town | (294, 240) | Central district; workshop/records projects. |
| 7 | Northwood Freight | (75, 48) | First-region exploration target (freight depot). |
| 8 | Ravenholm Quarry | (810, 60) | Quarry district (see §4, GP-21). |
| 9 | East Wharf | (831, 432) | Waterfront district (see §4, GP-22). |

Progression **Home → Riverside → Ironworks → Civic** is stated in `CLAUDE.md` ("Prior CITY-D handoff") and realised by `riverfrontSpec` (`riverfront.ts:33879`): `start: [72, 372]`, `target: [306, 414]`. The ordering *rule* is Worker A's; the *anchors* are here.

Factory yards (`RIVERFRONT.yards`, 4 entries, tile rects): `{83,350,30,29}` (Founders Court), `{305,394,32,25}` (Riverside), `{424,57,36,24}` (Ironworks), `{731,390,32,26}` (Civic). These are the reserved industrial footprints `validateRiverfront` protects.

113 named squares (`RIVERFRONT.squares`, `{x,y,w,h,name}`) supply street/place names for wayfinding; they are presentation labels over the same tile grid.

---

## 4. Gameplay sites

**Status mixed — read the per-row label.** All coordinates are sim tiles. IDs are stable strings and must survive the port unchanged; saves and campaign logic key on them.

### 4.1 Plants, cores and artifacts — Implemented and retained

| ID | Name | Tile |
|---|---|---|
| `plant:riverside` | Riverside Works | (287, 417) |
| `plant:ironworks` | Ironworks | (402, 78) |
| `plant:civic` | Civic Utility | (707, 415) |
| `core:freight` | Occupied freight depot | (63, 48) |
| `core:quarry` | Occupied quarry works | (806, 49) |
| `core:wharf` | Occupied waterfront warehouse | (826, 431) |
| `artifact:workshop` | Workshop salvage room | (258, 166) |
| `artifact:quarry` | Quarry equipment cache | (773, 49) |
| `artifact:wharf` | Secured wharf store | (824, 398) |

### 4.2 Projects — Implemented and retained

`RIVERFRONT.projects` is a `Record<string, [x, y]>`: `station [272,414]`, `radio [699,399]`, `northStation [434,116]`, `workshop [250,403]`, `turbine [511,412]`, `records [254,165]`, `heart [622,242]`, `furnace [469,90]`, `crown [782,285]`.

### 4.3 Recruits — Implemented and retained

`RIVERFRONT.recruits`, `[id, x, y][]`: `foreman (102,397)`, `electricians (264,163)`, `concrete (251,403)`, `lamplighters (559,270)`, `surveyors (104,161)`, `gunsmith (401,99)`, `railcrew (452,97)`.

### 4.4 Resource patches — Implemented and retained

`RIVERFRONT.resources`, `[kind, x, y, w, h, amount][]`:

| Kind | Tile | Size | Amount |
|---|---|---|---|
| steel | (59, 352) | 5×5 | 7,680 |
| copper | (59, 359) | 4×3 | 1,200 |
| coal | (76, 359) | 3×3 | 700 |
| ironore | (475, 57) | 3×3 | 12,000 |
| copperore | (785, 91) | 3×3 | 12,000 |
| crude | (830, 413) | 3×3 | 12,000 |
| coal | (818, 91) | 3×3 | 12,000 |
| stone | (803, 91) | 3×3 | 12,000 |
| steel | (273, 402) | 3×3 | 3,000 |
| copper | (475, 63) | 3×3 | 3,000 |
| coal | (777, 411) | 3×3 | 3,000 |

(The first three are the Home starter patches; their **quantities** are a balance fact owned by `GAME_DESIGN.md` — reproduced here because the tuple *is* the map record.)

### 4.5 Power infrastructure — Implemented and retained

Substations (`riverfront.ts:416`, `[x,y][]`): **(78, 347) — the Founders Court substation named in the opening tutorial**, (304, 424), (416, 94), (720, 420), (115, 176), (278, 178), (98, 59), (833, 91), (834, 408). Street lights: 18 positions at `riverfront.ts:341`. Service drives: 4 polylines at `riverfront.ts:480`.

### 4.6 First-region combat sites — Implemented and retained

`packages/sim/src/city/gameplaySites.ts` (12 lines) is the whole file and the canonical placement, instantiated by `packages/sim/src/firstRegion.ts`:

```ts
export const FIRST_CAMPS = [
  {id:'freight:camp:1', name:'West passage camp',        x:95, y:286, groups:[[72,287],[97,283],[73,266],[100,267]], count:20},
  {id:'freight:camp:2', name:'Old utility camp',         x:96, y:188, groups:[[72,190],[97,190],[74,168],[100,169]], count:22},
  {id:'freight:camp:3', name:'Northwood approach camp',  x:95, y:103, groups:[[74,103],[98,103],[74,122],[101,124]], count:20},
] as const;
export const FREIGHT_GATES = [{x:65,y:61,w:3,h:1},{x:56,y:49,w:1,h:3}] as const;
export const FREIGHT_ARENA = {x:57,y:40,w:28,h:21, guardian:[77,50],
  groups:[[45,35],[45,58],[98,35],[98,61],[62,58],[79,46]], count:60} as const;
```

`validateFirstRegion` (`packages/sim/src/firstRegion.ts`) checks exterior Home paths and both actual warehouse doors (`docs/PHASER_EDITOR.md:69`).

### 4.7 Quarry and Wharf camps — **Approved but not implemented**

**Owner decisions 2026-09-11 (Q06, Q17):** both camp sets are approved Unity scope. The reference project's GP-21/GP-22 checkpoint hold is a fact about that project and does not gate the port, and no per-feature go-ahead is outstanding. Populations, layouts and rewards are provisional, with guardian HP at Freight 600, Quarry 1,000 and Wharf 1,500 (DECISIONS.md U-P-09). TASKS.md E-15 depends only on its engineering prerequisites.

`docs/PROGRESS.md:85-86` records **GP-21 (Quarry camps)** and **GP-22 (Wharf camps)** as **blocked / pending implementation**. The *destinations* exist in the map (`core:quarry`, `artifact:quarry`, `core:wharf`, `artifact:wharf`, and the Ravenholm Quarry / East Wharf region anchors, §3), but `gameplaySites.ts` defines camps, gates and an arena for the **Freight** region only. A Unity port must therefore ship the Quarry and Wharf districts as **explorable but uncontested**, and leave a clearly-named extension point for two further camp sets with the same record shape as `FIRST_CAMPS`.

**Where the missing content is authored (U-M-15).** The split is by *what already exists in the canonical sources*, not by district:

| Content | Where it comes from | Why |
|---|---|---|
| Quarry / Wharf **places, region anchors, core and artifact sites, roads, rail, parcels, props** | **Imported** from `city/riverfront.ts` + `city/gameplaySites.ts` through the §7 export (D-01b → D-02b; the Home-region halves D-01a/D-02a carry the opening only) | They already exist there; re-authoring them would fork the canonical map |
| Quarry / Wharf **stronghold gameplay** — camp placements, gates, arena, guardian roster, rewards (GP-21/22) | **Authored in the Unity Editor after handover**, outside `Generated/`, and validated by the Unity-side city validator | It does not exist in the TypeScript sources at all, so there is nothing to export; adding it there would keep the Phaser/Python authoring chain alive permanently (U-M-15) |

This corrects the earlier E-15 acceptance wording ("Sites authored in the export, not hand-placed in Unity"), which would have required editing `gameplaySites.ts` after the export is frozen. The corrected wording now stands in TASKS.md E-15 and D-02b; the record is DECISIONS.md U-M-15.

**The Unity-side validator is not optional.** Whatever authors the new camps must be checked by a port of the rules in `packages/sim/src/city/validateRiverfront.ts` (61 lines) and `packages/sim/src/firstRegion.ts` — see §4.7.1. Hand-placing camps in a scene without that check is exactly the failure mode the TS validator was written to stop.

#### 4.7.1 The validation rules to port (from `packages/sim/src/city/validateRiverfront.ts` and `packages/sim/src/firstRegion.ts`) — **Approved but not implemented** in Unity

These run today on the authored TS map and must run on Unity-authored content too. Grouped by what they protect:

| Group | Rules |
|---|---|
| **Identity** | No duplicate building/site ids; every `authoredProblem` binding resolves |
| **Roads and pavements** | Buildings and props clear of the **road/pavement setback**; the public-road graph stays **connected** with no unintended road ends; **factory service drives connect to a road**; service-drive setback; turning-bulb setback |
| **Rail clearance** | Nothing overlaps the **swept tram corridor**; props off the tram corridor and the public road; rail continuity and tangent are preserved; **tram stops sit on a straight** |
| **Entrances** | Door on the correct wall; the entrance path **faces the correct way**, is **unobstructed** and meets the **minimum width**; entrance approaches are **reachable**; site bindings are accessible |
| **Factory reservations** | Exactly **four factory yards**; no factory-yard overlap; no pavement or swept-rail overlap on a yard |
| **Occupancy** | Shape inside its parcel; no neighbouring-building overlap; no resource-patch overlap; no solid-prop overlap; **water exclusion** |
| **Required content** | Required parcels present (`home-workshop`, `riverside-pump`, `ironworks-hall`, `civic-utility`, `freight-stronghold`, `quarry-stronghold`, `wharf-stronghold`, `civic-dome`); downtown archetype minimums; required sites (3 plants / 3 cores / 3 artifacts / 4 tram stops) |

`validateFirstRegion` (`packages/sim/src/firstRegion.ts`) adds the gameplay-geometry checks — exterior Home paths and both actual warehouse doors — and is the model for the equivalent camp checks (a camp must have a reachable approach, a gate on a real wall, and an arena that does not overlap a road, the tram corridor or a resource patch). **UNVERIFIED:** no source states camp-specific validation rules, because GP-21/22 were never implemented; the three listed here are inferred from the existing Freight camp records and are labelled as a proposal, not a ported rule.

### 4.8 Tram stops — Implemented and retained

| ID | Name | Tile |
|---|---|---|
| `tram:home` | T1 · Founders Court | (72, 448) |
| `tram:riverside` | T2 · Riverside | (324, 448) |
| `tram:ironworks` | T3 · Ironworks | (432, 124) |
| `tram:civic` | T4 · Civic / East Wharf | (664, 432) |

---

## 5. Transport geometry

**Status: Implemented and retained.**

### 5.1 Tram

- Authored centreline: `RIVERFRONT.tram` = `[[72,450], [378,450], [378,126], [666,126], [666,450]]` — five points, one open polyline (a loop the tram reverses along, not a closed ring).
- The drawn/travelled line is that polyline with **radius-12 fillets** at each corner (`tramRadius: 12`), produced by `packages/sim/src/city/riverfrontRail.ts` (`riverfrontRail()`), which is the single shared source for both the renderer art (`packages/game/src/riverfrontRailDraw.ts`) and sim motion.
- Measured centreline length: **1226.5476612469843 tiles** (`maps/phaser/last-apply.json`, `validation.railLength`). Motion is interpolated at **20 Hz**.
- Bed half-width `railHalf: 3` (7-tile bed); `tramSpeed: 40` tiles/s; `tramDwell: 1.5` s per stop.
- `validateRiverfront` enforces **swept rail clearance** — nothing may be authored inside the tram's swept envelope.
- Unity: the fillet generator must be ported exactly, or the centreline must be baked and exported as a dense polyline (§7 proposes baking it; a re-derived spline will not reproduce `1226.5476…` and will desynchronise stop arrival timing).

### 5.2 Public roads

- **371 connected public-road edges** (`RIVERFRONT.roadEdges`, matching `validation.roads: 371` in `maps/phaser/last-apply.json`), over **210 road nodes** (`RIVERFRONT.roadNodes`, each `{id, x, y, end?}`).
- 371 road polylines in `RIVERFRONT.roads` supply the drawn carriageways; `roadHalf: 5` gives a 11-tile carriageway, plus `pavement: 2` each side.
- Four **legal dead-ends** exist and are expected — `roadNodes` entries carrying an `end` marker. A Unity connectivity check must allow exactly these.
- Reachability baseline: `land: 412634` tiles, `accessible: 345246` tiles (`maps/phaser/last-apply.json`). Any Unity import that changes these numbers has changed the world.

### 5.3 Footpaths and drives

- 479 entrance paths (`RIVERFRONT.paths`, one per building; also mirrored per-building as `Parcel.path`).
- 4 service drives (`riverfront.ts:480`) connecting yards to the road graph.

### 5.4 Freight

Freight movement is gameplay (trucks, station routes) and is owned by `GAME_DESIGN.md`. The **geometry** it consumes is: the four tram stops (§4.8), the four factory yards (§3), the `station` / `northStation` project anchors (§4.2), and the 371-edge road graph (§5.2).

---

## 6. Buildings, parcels and the authoring pipeline

**Status: Implemented and retained.**

### 6.1 Counts and forms

479 buildings over 14 `BuildingKind` values (histogram in §2.4). Six additional downtown forms were integrated in CITY-D (`docs/Implementation/RIVERFRONT_CITY.md`); CITY-F rebuilt 77 smaller blocks and retained ten civic blocks with new forecourts (`CLAUDE.md`, CITY-F handoff; `docs/evidence/city-density/README.md`).

### 6.2 Roof and lighting rules

Roof artwork selection is a pure function of the building record. The rule is implemented **twice** and the duplication is deliberate but fragile:

- Renderer: `packages/game/src/riverfrontDraw.ts` derives the `rf-…-roof-…` asset key from `kind`, `facing` and `variant`.
- Tooling: `packages/tools/src/phaserCityModel.ts` `roof(b)` **duplicates the renderer key rule** so the exporter and the importer agree.

**Implemented but needs correction (low severity):** two copies of one rule with no shared module and no test asserting they agree. In Unity, implement this once (a single `RoofKeyResolver`) and have both the import tool and the renderer call it.

House textures encode N/E/W facing; all other roof textures face south (`docs/PHASER_EDITOR.md:22`). `variant` selects roof variants `0`, `1`, `2` (`docs/PHASER_EDITOR.md:21`).

Street lighting: 18 emitters, `lightKw: 2` each; lit-state is a sim computation (§2.8).

### 6.3 Fixed campaign buildings

**22** buildings are marked `FIXED` in `maps/phaser/manifest.json` and carry `enterable: true` in `riverfront.ts`. They "contain interiors or campaign bindings and cannot move, change their plot or be deleted" (`docs/PHASER_EDITOR.md:23,29`). Roof variants remain editable on them. These 22 are the only buildings with interiors; the other 457 are "solid background scenery with boarded entrances" (`docs/PHASER_EDITOR.md:22`).

### 6.4 The Phaser Editor manifest ↔ sim binding

Commands (`docs/PHASER_EDITOR.md`, `package.json`): `npm run editor:dev | editor:assets | editor:scene | editor:check | editor:apply`.

- `editor:scene` is a **one-time exporter**; it refuses to overwrite an existing scene or manifest (`docs/PHASER_EDITOR.md:35`).
- `editor:check` validates a draft without changing the game; `editor:apply` validates and writes.
- `maps/phaser/manifest.json` binds editor object UUIDs → canonical building IDs, records `sourceHash c4a874ce…`, **480 objects** and the 22-entry `fixedBuildings` list.
- `maps/phaser/RiverfrontCity.source.txt` (347,941 B) is the source snapshot the hash is taken over; `packages/game/src/editor/RiverfrontCity.scene` (448,128 B, **959 objects** = 1 reference + 479 buildings + 479 plot rectangles) is the editable scene.
- `packages/tools/src/phaserCity.ts` performs export/check/apply. It asserts `hash(baseline) === manifest.sourceHash` before applying, writes atomically via an `.editor-tmp` file plus rename, and recomputes the map ID:
  `draft.city.id = changed ? \`riverfront-arc-v4-editor-${hash(...).slice(0,12)}\` : manifest.base.city.id`
  — i.e. **the map ID is a content hash**, which is why the current ID ends `ac16d9188c05`.
- `packages/tools/src/phaserCityModel.ts` holds the `Manifest` type, a CRLF-normalising `hash`, four slice markers that locate the generated blocks inside `riverfront.ts`, and `roof(b)`.

### 6.5 Runtime template drift guard

`docs/evidence/city-c/authorFullCity.py:231-233` refuses to regenerate the city if the runtime template has diverged:

```python
runtime = (Path(__file__).with_name('riverfront-runtime.inc')).read_text(encoding='utf-8')
definition, tail = runtime.split('// RUNTIME_TAIL\n')
assert current[...] == definition and current[...] == tail, \
  'Runtime drift: reconcile riverfront-runtime.inc with current riverfront.ts before regenerating; do not overwrite newer logic.'
```

`packages/tools/src/authorCityDensity.ts:13` throws the equivalent for the density compiler (`'City density source/scene drift: reconcile subsequent edits before rerunning.'`), and `:104` throws on any `validation.errors`.

**Unity consequence:** the generators are *build-time authoring tools for the TS tree*. They should **not** be ported. Unity consumes the exported artefact (§7).

**After handover the authoring chain is frozen (U-M-15, Adopted).** The export (D-01a for the Home region, D-01b for the full city) stays repeatable only while the importers (D-02a, D-02b) are being accepted; once D-ACC passes, the Phaser Editor / `editor:apply` / Python generator chain is **provenance, not a live tool**, and these drift guards stop being an ongoing obligation. From that point all new world content is authored in the Unity Editor outside `Generated/` and checked by the ported validator (§4.7.1). §11 Q2 is therefore **resolved**, not open.

---

## 7. Export / import mapping for Unity

**Status of this whole section: PROPOSAL.** Nothing below exists yet. It is Worker C's recommended shape for the data hand-off; Worker B owns the runtime architecture that consumes it and may revise field names, containers and load order in `TECHNICAL_ARCHITECTURE.md`.

### 7.1 Field-by-field mapping

| Today (source) | Shape today | Proposed Unity representation | Generated or hand-edited in Unity |
|---|---|---|---|
| `RIVERFRONT` scalars (`riverfront.ts:10`) | JS object literal in TS | One `CityDefinition` ScriptableObject (or the header object of the export JSON) | **Generated, read-only** |
| `GroundTiles` (`ground.ts:558`) `kind`/`variant`/`patch` Uint8Arrays | 3 × 497,664 bytes | Three `Tilemap` layers on a `Grid` — `Terrain`, `TerrainVariant` (or a variant-per-tile rule tile), `ResourcePatch` — or one `Tilemap` + a `Texture2D` lookup for variant/patch | **Generated, read-only** |
| `RIVERFRONT_BUILDINGS` (`:12516`) 479 × `Parcel` | array of records with 3 rects | One prefab instance per building under a `Buildings` root, each carrying a `BuildingData` component (id, name, kind, facing, variant, collision rect, parcel rect, visual rect, doors, enterable, content) | **Generated**; the 22 `FIXED` interiors get **hand-editable** child prefabs |
| `RIVERFRONT_PROPS` (`:30896`) 370 × `MapProp` | records | Prefab instances under a `Props` root with a `PropData` component (kind, rect, clearable) | **Generated, read-only** |
| `roads`, `paths`, `drives`, `tram` polylines | `[x,y][][]` | A `RoadNetwork` ScriptableObject holding polylines + the 210-node / 371-edge graph; plus a baked `Tilemap` layer for the drawn surface | **Generated, read-only** |
| Baked tram centreline (from `riverfrontRail()`) | derived at runtime | A dense polyline asset (`TramSpline`) with the measured length `1226.5476612469843` stored as a checksum field | **Generated, read-only** |
| `stops`, `plants`, `cores`, `artifacts`, `projects`, `recruits`, `substations`, `lights` | typed arrays/records | One `GameplaySite` ScriptableObject **per site**, in an addressable group keyed by the stable string ID; empty `GameObject` markers in the scene reference them | **Generated once**, then **hand-editable** (designers may move a site; the ID must not change) |
| `FIRST_CAMPS`, `FREIGHT_GATES`, `FREIGHT_ARENA` (`gameplaySites.ts`) | `as const` tuples | A `CombatSiteSet` ScriptableObject per region, with room for the unimplemented Quarry/Wharf sets (§4.7) | **Hand-editable** |
| `squares` (113) | `{x,y,w,h,name}` | A `PlaceLabels` ScriptableObject (or a Tilemap of label anchors) | **Generated, read-only** |
| `yards` (4), `serviceAreas` (1) | tile rects | `ReservedArea` entries in the `CityDefinition` | **Generated, read-only** |
| `resources` (11) | `[kind,x,y,w,h,amount]` | `ResourcePatch` ScriptableObjects; amounts are balance data and must be overridable from `GAME_DESIGN.md` values | **Hand-editable** |
| Map ID `riverfront-arc-v4-editor-ac16d9188c05` | content hash string | A read-only `mapId` string on `CityDefinition`, compared on save load | **Generated, read-only** |
| `maps/tiled/**` | TMJ + tilesets | **Not imported.** Reference only. | n/a |

### 7.2 Proposed export JSON schema outline (proposal)

A new exporter in `packages/tools` (sibling of `exportTiled.ts`, reusing its `createCampaign()` + `validateRiverfront()` + `validateFirstRegion()` preamble at `exportTiled.ts:15-17`) emitting one file plus binary side-cars:

```
unity-export/
  city.json              # header + all vector/record data
  terrain.kind.bin       # 864*576 bytes, row-major, t = y*864 + x
  terrain.variant.bin    # 864*576 bytes
  terrain.patch.bin      # 864*576 bytes
  export-report.json     # counts, sourceSha256, validation results, tool version
```

```jsonc
{
  "schema": 1,
  "mapId": "riverfront-arc-v4-editor-ac16d9188c05",
  "sourceSha256": "<hash of riverfront.ts + gameplaySites.ts>",
  "generatedAt": "<ISO-8601>",
  "tilePixels": 32,
  "yAxis": "down",                       // Unity importer flips; see §2.2
  "size": { "width": 864, "height": 576 },
  "scalars": { "cell":36, "roadHalf":5, "pavement":2, "setback":2, "railHalf":3,
               "tramRadius":12, "tramSpeed":40, "tramDwell":1.5, "riverY":477,
               "homeOrigin":[58,345], "court":{"x":72,"y":374,"radius":5.5},
               "homeRaidY":391, "lightKw":2 },
  "terrain": { "kind":"terrain.kind.bin", "variant":"terrain.variant.bin",
               "patch":"terrain.patch.bin", "names":["street","ground","rubble","inert","river","deposit","patch"] },
  "buildings": [ { "id":"…", "name":"…", "kind":"house", "facing":"S",
                   "rect":[x,y,w,h], "parcel":[x,y,w,h], "visual":[x,y,w,h],
                   "path":[[x,y],…], "enterable":true, "door":[x,y],
                   "doors":[[x,y,w,h],…], "variant":0, "roofKey":"rf-…-roof-0",
                   "fixed":true } ],
  "props":     [ { "id":"…", "kind":"tree", "rect":[x,y,w,h], "clearable":true } ],
  "roads":     { "nodes":[{"id":"…","x":0,"y":0,"end":"…"}], "edges":[["a","b"]],
                 "polylines":[[[x,y],…]] },
  "paths":     [[[x,y],…]],
  "drives":    [[[x,y],…]],
  "tram":      { "control":[[72,450],…], "baked":[[x,y],…], "length":1226.5476612469843,
                 "stops":[{"id":"tram:home","name":"T1 · Founders Court","x":72,"y":448}] },
  "sites":     { "plants":[…], "cores":[…], "artifacts":[…],
                 "projects":{"station":[272,414],…}, "recruits":[["foreman",102,397],…],
                 "substations":[[78,347],…], "lights":[[x,y],…] },
  "combat":    { "firstCamps":[…], "freightGates":[…], "freightArena":{…} },
  "regions":   [["Founders Court",72,372],…],
  "squares":   [{"x":0,"y":0,"w":0,"h":0,"name":"…"}],
  "yards":     [{"x":83,"y":350,"w":30,"h":29},…],
  "serviceAreas":[{"x":0,"y":0,"w":0,"h":0,"block":0}],
  "resources": [["steel",59,352,5,5,7680],…],
  "validation":{ "buildings":479, "roads":371, "railLength":1226.5476612469843,
                 "land":412634, "accessible":345246 }
}
```

Design intent of that shape:

1. **Everything the sim reads, in one file**, so the Unity importer never parses TypeScript.
2. **`validation` is a fingerprint.** The Unity importer should re-derive those five numbers after import and fail loudly on mismatch — the same discipline `maps/phaser/last-apply.json` already provides.
3. **Terrain is binary side-cars**, not JSON arrays: 3 × 497,664 entries would be ~4 MB of JSON digits each.
4. **`roofKey` is precomputed by the exporter** so Unity does not reimplement the duplicated rule of §6.2.
5. `"yAxis": "down"` is declared explicitly so the flip decision (§2.2) is a data fact, not a convention.
6. **Never re-import.** Like TMJ, this export is one-directional: TS → Unity. If Unity becomes the authoring home, that is a separate, deliberate migration (§11).

---

## 8. Asset inventory

**Status: Implemented and retained** unless noted. Counts verified by directory listing on 2026-09-11.

| Path | Format | Count | Generated or authored | Provenance / licence | Unity import notes |
|---|---|---|---|---|---|
| `packages/game/public/art/riverfront/*.svg` | SVG | **65** | **Generated** in-repo | Own work. Produced by the three Python generators below. | Rasterise at import to 320×256 px sprites (the size the Tiled exporter declares, `exportTiled.ts:39`), or use the Unity **Vector Graphics** package to keep them vector. Sprite pivot must be **top-left** to match `objectalignment:'topleft'`. |
| `packages/game/public/art/riverfront/build-assets.py`, `build-downtown.py`, `build-facing-houses.py` | Python | 3 | Authored tooling | Own work | Do **not** port. Build-time authoring tools; run in this repo, commit the SVG output. |
| `packages/game/public/art/editor/riverfront-ground.svg` | SVG | 1 (274,587 B) | Generated | Own work | The Phaser Editor terrain **reference** image only. Not gameplay art. `docs/PHASER_EDITOR.md:31`: "The terrain/reference image is a snapshot". Exclude from the Unity build. |
| `packages/game/public/art/editor/terrain/*.svg` | SVG | 6 | Authored | Own work | Reusable 32×32 road/path/kerb textures (`docs/PHASER_EDITOR.md:41`). Authoring-only today — `editor:check`/`apply` **reject** them in the city scene (`docs/PHASER_EDITOR.md:45`). Candidates for Unity tile assets if road authoring moves to Unity. |
| `packages/game/public/relight-asset-pack.json` | JSON | 1 | Generated/maintained additively | Own work | Phaser asset manifest binding the 65 `rf-…` keys. Superseded in Unity by Addressables/labels; keep as the key list during migration. |
| `packages/game/public/relight-editor-pack.json` | JSON | 1 | Generated | Own work | Editor pack (terrain reference + road/path textures). Editor-only. |
| `packages/game/src/itemIcons.ts` | Inline SVG path strings in TypeScript | ~1 module, many icons | **Authored in code** | Own work | **Every UI item icon is an SVG path string embedded in source**, rendered by `iconMarkup` into a 64×64 viewBox, with a long alias table. There are no icon image files. Unity must extract these to `.svg`/sprite assets or re-author them; see `UI_AND_ONBOARDING.md` §10. |
| `packages/game/src/editor/RiverfrontCity.scene` | Phaser Editor v5 JSON | 1 (448,128 B, 959 objects) | Authored (owner-edited) | Own work | Authoring artefact. Not shipped, not imported by Unity. Preserve in the repo. |
| `maps/phaser/manifest.json`, `last-apply.json`, `RiverfrontCity.source.txt` | JSON / text | 3 | Generated | Own work | Authoring baseline + validation fingerprint. Keep; `last-apply.json`'s five validation numbers become the Unity import check (§7.2). |
| `maps/tiled/riverfront-v4/**` (incl. `assets/`, 65 art files, 23 layers, 2,031 objects) | TMJ + TSJ + SVG copies | 1 map dir | Generated (stale) | Own work | **Reference only, and stale** (§1.3). Do not import. |
| `packages/game/public/snapshots/*.json` | JSON saves | **3** (`b-compact-seed3.json` 246 KB, `city-factory-qa.json` 212 KB, `city-opening-qa.json` 180 KB) | Generated fixtures | Own work | **Reference data — never delete.** These are load-bearing QA fixtures reachable via `?state=/…json`. They pin save-format compatibility and give the Unity port concrete, known-good world states to diff against. |
| `docs/evidence/**/*.json` | JSON | 21,642 files | Generated evidence | Own work | Historical evidence, not runtime assets. Twelve stale freshness artifacts are knowingly retained (`docs/Implementation/RIVERFRONT_CITY.md`). Exclude from the Unity project; keep in the repo as provenance. |
| `Assets/urban-decay/48px+MVMZ/*.png` | PNG | **15** | **Third party** | ⚠️ **NO licence, readme or attribution file anywhere under `Assets/`.** RPG Maker MV/MZ 48 px tilesheet format: `Modern_Outside_A1`–`A5`, `Modern_Outside_B_Sheet`, `C_Sheet`, `D_Sheet`, `E_Sheet`, `!doors`, `!doors_dark`, plus four `non-rm-*` sheets. Git-tracked. **Referenced by zero code.** | **Quarantined: excluded from the Unity build** until the owner provides source and licence (Owner decisions 2026-09-11, Q04). See §9.1.2 and §11. |

### 8.1 Notes on the SVG pipeline

The SVG art is fully reproducible: `build-assets.py`, `build-downtown.py` and `build-facing-houses.py` emit the 65 files, `npm run editor:assets` adds any new file to `relight-asset-pack.json` additively (`docs/PHASER_EDITOR.md:37`), and `docs/evidence/phaser-editor/browser.json` records a real Chrome session fetching and loading **all 65 assets** through Phaser's asset-pack loader without errors (`docs/PHASER_EDITOR.md:59`).

**Correction to the brief:** the brief describes `art/riverfront/` as "68 SVG". It holds **65 SVG + 3 `.py` generators** = 68 files.

---

## 9. Missing or pending assets

| Gap | Status | Evidence / detail |
|---|---|---|
| **Owner's new art package** | **Approved but not implemented** — policy settled 2026-09-11 | `CLAUDE.md` (CITY-F handoff): "Existing artwork remains pending the owner's new package." `docs/PHASER_EDITOR.md:31`: "New artwork integration is a separate next step; adding an arbitrary texture to the pack alone does not define collision or a building archetype." All current building art is placeholder-grade generated SVG. **The package must not block foundation work or playable milestones** — see the policy in §9.1 (Owner decisions 2026-09-11, Q04). |
| **Audio — none in the reference; in scope for Unity** | **Approved but not implemented** | A repo-wide search finds **zero** `.mp3`, `.ogg`, `.wav` or `.m4a` files. `docs/RI-02B_UI_SPEC.md:17`: "no game audio integration was found in the inspected game source … do not build an audio engine for this phase." `RI-02B §UI-06`: "No audio/mute control pretending sound exists; future audio plugs into settings." **Correction, 2026-09-11:** the Unity port no longer starts from silence — audio is **in scope** by owner decision (Q09), reversing the recorded "silence with a hook point" default (DECISIONS.md U-M-24). Asset sourcing, licensing and layout are §9.2; the cue inventory is `UI_AND_ONBOARDING.md` §12. |
| **Fonts — none bundled** | **Implemented but needs correction** | Zero `.woff`, `.woff2`, `.ttf` or `.otf` files in the repo. `packages/game/src/style.css:6` requests `"IBM Plex Sans", "Segoe UI", system-ui, sans-serif` — **IBM Plex Sans is referenced but never shipped**, so it renders only on machines that happen to have it. `style.css:131` (the RI-02B token block) falls back to `"Segoe UI", system-ui, sans-serif`. Unity must pick and ship a licensed TMP font asset; the current look is "whatever Segoe UI does on Windows". |
| **UI icons are code, not assets** | **Implemented and retained** (but a migration cost) | `packages/game/src/itemIcons.ts` — inline SVG path strings, no files. Unity needs an extraction pass. |
| **Art for approved-not-implemented content** | **Approved but not implemented** | Quarry and Wharf camp content (GP-21/22, `docs/PROGRESS.md:85-86`) has map anchors but no camp props, no gate art and no arena dressing. |
| **Road/path terrain authoring** | **Approved but not implemented** | The six `art/editor/terrain/` SVGs exist and are explicitly rejected by the importer: "importing authored road/path geometry needs separate support" (`docs/PHASER_EDITOR.md:45`). |
| **Third-party tileset with no licence** | **Unresolved — blocking** | `Assets/urban-decay/48px+MVMZ/` (§8). 15 PNGs, RPG Maker MV/MZ format, committed, unreferenced, unlicensed, unattributed. |
| **UNCERTAIN: code-referenced-but-absent assets** | **Unresolved** | The Phaser asset-pack load of all 65 keys was verified clean (`docs/evidence/phaser-editor/browser.json`), so no *building* art is missing. I did not run an exhaustive cross-check of every string literal in `packages/game/src` against the public tree, so a stray reference could exist. |

### 9.1 Art asset policy — Owner decisions 2026-09-11 (Q04)

**Confirmed scope (owner):** proceed with **known reusable placeholder assets**. **Third-party assets with unverified permissions are excluded from the Unity build** until the owner provides source and licence. The owner's final art package **is still pending and must not block foundation work or playable milestones.**

Stated plainly, three lists. This is the whole policy; nothing else in §8 or §9 overrides it.

#### 9.1.1 May ship now

| Asset | Count | Why it may ship |
|---|---|---|
| `packages/game/public/art/riverfront/*.svg` | **65** | Generated in-repo by `build-assets.py`, `build-downtown.py` and `build-facing-houses.py` (§8, §8.1). Own work, fully reproducible from the committed generators. |
| `packages/game/public/art/editor/terrain/*.svg` | **6** | Authored in-repo, own work (`docs/PHASER_EDITOR.md:41`). Authoring-only today; usable as Unity tile assets if road authoring moves to Unity (§8). |
| `packages/game/src/itemIcons.ts` icon paths | one module | Authored in source, own work. Extraction pass required (§8; `UI_AND_ONBOARDING.md` §10.2). |
| **IBM Plex Sans** | 1 font family | Not in the repo — zero font files exist (§9) — but `style.css:6` already requests it and it is SIL Open Font Licence, so it is shippable. DECISIONS.md **U-M-16**. |

Everything on this list is **placeholder-grade** and is labelled as such wherever it appears in the build's own credits or asset manifest. Shipping it now is what keeps Phases B–E moving.

#### 9.1.2 Quarantined — excluded from the Unity build

| Asset | Why |
|---|---|
| `Assets/urban-decay/48px+MVMZ/*.png` (**15** files) | Third party with **no licence, readme or attribution file anywhere under `Assets/`** (§8). RPG Maker MV/MZ 48 px tilesheet format. Git-tracked, referenced by **zero** code. RPG Maker asset licences are commonly engine-restricted, which would make them unusable in a Unity build at all. |

Quarantine means, concretely: the files are **not copied into the Unity project**, not referenced by any prefab, material, Addressables group, asset label or import script, and not listed in any manifest the build reads. They stay in the reference repository as provenance — this is not a deletion instruction. DECISIONS.md **U-M-10** already forbids referencing them; **U-Q-04** remains genuinely open as a legal question and **F-03** stays blocked on it. Any future use requires the owner to state **source and licence** first; nothing else unblocks it.

Any *further* third-party asset proposed later enters the same quarantine by default. The test is "permissions verified in writing", not "probably fine" or "unreferenced anyway".

#### 9.1.3 What happens when the art package lands

The package is **not a prerequisite** for any foundation task or playable milestone. Phases B and C proceed on the placeholder set; C-ACC (the playable-opening check) is judged on behaviour and readability, never on final art. When the package arrives:

1. It is inventoried into §8 with format, count, provenance and licence per item — the same columns as every other row. An item without a licence entry is quarantined by §9.1.2, whoever supplied it.
2. **F-03** (TASKS.md) replaces placeholder rasters, integrates the package and settles the bundled UI font.
3. Sprite-pipeline consequence: if the package lands **before** the import tooling is written (D-09), the pipeline is built for the package rather than for the 65 placeholder SVGs; if it lands **after**, the placeholders are swapped at the same import boundary (top-left pivot, §8) and no world data changes — art is placement-independent, since parcels reference archetypes, not files (§6).
4. New artwork alone does not define collision or a building archetype (`docs/PHASER_EDITOR.md:31`); each new form still needs its archetype and footprint recorded (§6.1, §6.2).

### 9.2 Audio assets — Owner decisions 2026-09-11 (Q09)

**Confirmed scope (owner):** audio is **in scope**, covering player and turret weapons; impacts and appropriate enemy cues; machine operation; crafting and construction feedback; raid warnings; UI interactions; and environmental ambience. Implementation is **proportionate**, using **reusable, properly sourced assets**, with sensible volume controls. **Not required:** voice acting, a bespoke soundtrack, an elaborate audio framework.

> **Correction, 2026-09-11.** This reverses DECISIONS.md **U-M-24**, which recorded "Audio defaults to silence with a hook point … the build ships without audio" as the standing default pending U-Q-09. U-Q-09 is now answered. TASKS.md **F-04** moves off option (a) silence; its option (b)/(c) deliverables — a cue map, an asset list with licences, mixer and Settings entries — become real work. The `AudioCue` hook point U-M-24 required is still the right seam and is now used rather than left empty.

This section owns **assets and provenance only**, matching §8's job. The cue inventory and which sim system drives each cue are `UI_AND_ONBOARDING.md` §12; the volume controls are its §3.8.1; routing, pooling and the rule that `Relight.Sim` never references audio are `TECHNICAL_ARCHITECTURE.md` (Worker B).

#### 9.2.1 Sourcing and licence rules

The same discipline §9.1.2 applies to art, applied to sound from the first file:

1. **Every clip carries a licence record before it enters the project** — source URL or vendor, author, licence name and version, and any attribution string the licence requires. A clip without that record is quarantined exactly like the tileset.
2. **Prefer licences with no attribution burden and no engine restriction** (CC0 / public domain, or a bought royalty-free licence covering commercial game distribution). Where an attribution licence is used, the attribution text goes into an in-build credits list at the same time as the file, not later.
3. **Reusable** means one clip serves many cues through pitch/volume variation rather than a bespoke recording per event — the proportionality the owner asked for. A short, deliberately small set is the target, not a library.
4. **No AI-generated or scraped audio of uncertain provenance**, and no clip whose licence cannot be produced on request.
5. The manifest lives beside the assets and is mirrored as a row group in §8 once files exist, so that §8 stays the single asset inventory.

#### 9.2.2 Format and layout (proposal — Worker B owns the final call)

| Kind | Format | Import setting |
|---|---|---|
| One-shots (weapons, impacts, UI, stingers) | `.wav`, 44.1 kHz mono | Decompress on load; load in background off |
| Loops (machine beds, ambience) | `.ogg` | Streaming or compressed in memory, looping enabled |

Proposed project layout: `Assets/Audio/<category>/` with one folder per §12 category, plus `Assets/Audio/audio-credits.json` holding the licence records from §9.2.1. Nothing here is an owner decision; it is the cheapest arrangement that keeps the licence record next to the file.

#### 9.2.3 What is still absent

No audio file exists yet anywhere in the reference or in a Unity project — the repo-wide search result in §9 stands unchanged. Every row of `UI_AND_ONBOARDING.md` §12 is therefore **Approved but not implemented**, and none of it may be recorded as ported, verified or heard until real clips exist and a check has actually been run.

---

## 10. Player-facing world defects and open gates

| # | Item | Status | Evidence |
|---|---|---|---|
| W-1 | **60 fps performance gate is not certified.** The CITY-C downtown sample (180 frames) measured 16.7 ms median / 33.3 ms p95 / 33.5 ms max — a p95 at exactly 30 fps. `docs/Implementation/RIVERFRONT_CITY.md` states the sample "does not establish universal 60 fps"; `docs/Implementation/GP_CHECKPOINT.md:47` states "The 60-fps performance gate is not met/certified by this sample." | **Unresolved** (open gate) | `docs/Implementation/RIVERFRONT_CITY.md` (CITY-C), `GP_CHECKPOINT.md:47` |
| W-2 | **Stale Tiled export retained in-tree.** 287 buildings / 133 props against a live 479 / 370 (§1.3). It carries no in-file staleness warning; only `export-report.json`'s counts reveal it. | **Implemented but needs correction** | `maps/tiled/riverfront-v4/export-report.json` |
| W-3 | **Rendering/lighting**: sim `lightMask` and renderer `daylight(t)` are separate models. Renderer-only effects cannot imply mechanics (`CLAUDE.md`, "Work and authority"). Any visual/gameplay lighting mismatch is presentation drift by construction. | **Implemented and retained** (design), **UNCERTAIN** whether a visible mismatch has been reported | `packages/sim/src/light.ts`, `packages/game/src/riverfrontLighting.ts` |
| W-4 | **Tram-survey history.** P9-02 fixed seed-6/29 tram-survey failures for new games while preserving existing saves; all 32 diagnostic seeds now pass. P9-04 completed 10,000 declared seeds with **258 failures retained**, repaired by P9-04R which passes a separate 10,000-seed comparison. | **Implemented and retained** (fixed), history worth carrying | `CLAUDE.md` "Current direction"; `docs/P9_02_SURVEY_RELIABILITY_REPORT.md` |
| W-5 | **Broad test suite has never completed in one run** against the CITY-F map: a bounded partial run recorded **46 passes / 8 failures before a 180-second cutoff**. | **Unresolved** | `docs/evidence/city-density/README.md` |
| W-6 | **Twelve historical stale freshness artifacts** are knowingly retained in the evidence tree. | **Retired** (retained as history) | `docs/Implementation/RIVERFRONT_CITY.md` |
| W-7 | **Old authored saves (v1/v2/v3) need their original builds.** v4 requires New Game; procedural saves remain isolated. An applied editor change re-hashes the map ID and invalidates existing saves. | **Implemented and retained** (by design) | `CLAUDE.md` CITY-D handoff; `docs/PHASER_EDITOR.md:33` |
| W-8 | **Two different "cell" sizes exist.** `CHUNK = 32` in `ground.ts:29` is "a lattice cell exactly" and that comment is correct: it refers to the block cell `CELL_TILES = 32` (`tiles.ts:18`). The authored city's street-planning pitch `RIVERFRONT.cell = 36` (§2.3) is a different quantity. Not a defect; a naming hazard for the port. | **Implemented and retained** (name the two constants distinctly in Unity) | `packages/sim/src/ground.ts:29`, `tiles.ts:18`, `RIVERFRONT.cell = 36` |

---

## 11. Open questions for the coordinator

1. **Third-party tileset licence (blocking).** `Assets/urban-decay/48px+MVMZ/` holds 15 RPG Maker MV/MZ PNG sheets with **no licence, readme or attribution**, committed to git, referenced by **zero** code. RPG Maker asset licences are typically engine-restricted (usable only in RPG Maker products) — if that applies, these cannot ship in a Unity build at all. Needs the owner to state the source and licence, or the directory should be removed from the migration scope. Until answered, Unity must not reference them.
2. ~~**Does the city continue to be authored in this repo?**~~ **Resolved — U-M-15 (Adopted).** Existing content is **imported** from the canonical TypeScript sources (`city/riverfront.ts`, `city/gameplaySites.ts`, `riverfrontRail.ts`, `firstRegion.ts`, the Phaser density manifest) through the D-01a/D-01b exporters and the D-02a/D-02b importers. Content that does not exist there — GP-21/22 stronghold gameplay and any later sites — is **authored and validated in Unity after handover** (§4.7, §4.7.1). The export is repeatable while D-02b is being accepted and is frozen at D-ACC; after that the Phaser/Python chain is provenance only (§6.5). *Question retained here for provenance; no owner answer is outstanding.*
3. **Y-axis flip (§2.2).** Confirm the port flips Y at import and keeps sim tile space Y-down internally, or commits to a Y-down Unity world.
4. ~~**Quarry and Wharf camps (GP-21/22).**~~ **Answered — Owner decisions 2026-09-11 (Q06, Q17).** Both camp sets are in Unity scope: the full approved gameplay direction is approved, and the reference project's GP-21/GP-22 checkpoint hold does not gate the port. No per-feature go-ahead is outstanding. The districts do **not** ship uncontested. Guardian HP is provisional (Freight 600, Quarry 1,000, Wharf 1,500 — DECISIONS.md U-P-09) and the camps are **authored in Unity**, not added to the TypeScript export (§4.7). E-15 depends only on its engineering prerequisites.
5. ~~**Art package timing.**~~ **Answered — Owner decisions 2026-09-11 (Q04).** Proceed with the known reusable placeholder assets; the pending package must not block foundation work or playable milestones. The full policy — what may ship now, what is quarantined, and what happens when the package lands — is **§9.1**. *Question retained for provenance; the tileset's licence (Q1 above) remains genuinely open.*
6. ~~**Audio and fonts from zero.**~~ **Audio answered — Owner decisions 2026-09-11 (Q09): audio is in scope**, reversing the silence default (U-M-24). Coverage, sourcing and licence rules are **§9.2**; the cue inventory is `UI_AND_ONBOARDING.md` §12. The font is unchanged and still needs bundling (U-M-16). *Question retained for provenance.*
7. **The 60 fps gate (W-1).** Should the Unity port be held to the same uncertified gate, or is re-baselining performance in Unity the accepted answer? Worker B owns the rendering approach; someone must own the target.
8. **Is `docs/evidence/**` in migration scope at all?** 21,642 JSON files. Recommend: repo history only, excluded from the Unity project, with the 3 `public/snapshots/` fixtures explicitly carried across as test data.

*Owner decisions (2026-09-11):* Q5 → **answered by Q04** (§9.1: placeholders ship, unverified third-party assets are quarantined, the pending package blocks nothing); Q6 → **audio answered by Q09** (§9.2: in scope, proportionate, properly sourced), font unchanged. Q1 stays open as a legal question, and the tileset stays quarantined until it is answered.

*Coordinator disposition (2026-09-11):* Q1 → DECISIONS.md U-Q-04 (blocking for any use of the tileset; U-M-10 forbids referencing it); Q2 → U-M-15; Q3 → U-M-14; Q4 → **resolved by U-D-31/U-D-40** (E-15 is not blocked and the districts are contested; the earlier "E-15 blocked, districts ship uncontested" disposition is withdrawn); Q5 → U-Q-04; Q6 → U-Q-09 (audio) and U-M-16 (font); Q7 → U-M-17 (F-07 owns the measurement); Q8 → U-M-18.
