# CityValidator — the ported rule list (D-02b, EXP-01, REL-31)

`CityValidator.cs` is the reference's structural acceptance check for the authored city, ported so that a broken
export fails the import. This file is the rule-by-rule account: what was ported, what was ported with a
difference, and what was not ported and why.

**Sources in the read-only reference project**

| What | Where |
|---|---|
| `validateRiverfront` (rules 1–33) | `packages/sim/src/city/validateRiverfront.ts:9-61` |
| `validateFirstRegion` (rules 34–36) | `packages/sim/src/firstRegion.ts:118-123` |
| `overlaps`, `corridorRect`, `doorRect`, `doorOutside` | `packages/sim/src/city/parcelGeometry.ts` |
| swept corridor, baked centreline | `packages/sim/src/city/riverfrontRail.ts` |
| `lineTiles` | `packages/sim/src/city/riverfront.ts` |

**How it runs.** `RegionImporter.Import` calls `CityValidator.Validate` after its own file checks (hashes, the
solid mask, the count table) and before it writes anything. Any error fails the import, and the message names the
offending id. `RegionImporter.Check(regionId, folder)` runs the same checks on any folder, writes nothing and
shows no dialog; the tests use it.

**Input.** The validator is pure: a parsed `CityFile`, the exported `terrain.kind.bin` and `terrain.solid.bin`.
A tile is *passable* when it is inside the region, is not river and is not solid — the reference's `walkable`,
and the exporter's `accessibleFromSpawn`. Coordinates in messages are region-local, as the file has them.

## Whole city and crops

The exporter writes one whole-map region, `full`, and crops such as `home`. A crop carries only what touches its
rect (the `home` crop has 49 of the 479 buildings, 9 of the 210 road nodes and 194 of the 4909 rail samples), and
its polylines are not clipped. So:

- **Whole-city rules** (15–18, 21–24, 29–31) run only when `region.id == "full"`. On a crop they would all fail
  for a reason that is not a fault in the city.
- **Every other rule** runs on any region, and skips tiles that lie outside it.

A city that passes as `full` and breaks a whole-city rule is therefore caught on the `full` import, not on a crop's.

## Rules 1–14: one building at a time

Messages start `"{id} ({x},{y})"`, the building's id and its rect's corner.

| # | Reference rule | Message ends | Status |
|---|---|---|---|
| 1 | Two buildings share an id | `duplicate` | Ported |
| 2 | The rect, and the visual rect, lie inside the parcel | `outside parcel` | Ported. A building with no parcel reports `has no parcel` |
| 3 | The rect keeps `court.radius + setback` from the turning bulb | `turning bulb setback` | Ported |
| 4 | The rect keeps `roadHalf + setback` from every road | `road/pavement setback` | Ported. As in the reference, a road is its first two points; every authored road is one straight segment |
| 5 | The rect overlaps no factory yard | `factory yard` | Ported |
| 6 | The rect keeps clear of every service drive | `service drive` | Ported, with the reference's own lopsided clearance (2 tiles before, 3 after) |
| 7 | The rect overlaps no other building | `neighbouring building {id}` | Ported. Reported once per pair, against the later id (ordinal order) |
| 8 | The rect overlaps no resource | `resource {item} at x,y` | Ported |
| 9 | The rect overlaps no solid prop, unless the building is enterable and the prop is inside it | `solid prop {id}` | Ported |
| 10 | The door is inside the rect and on the facing wall | `door on wrong wall` | Ported |
| 11 | The entrance path starts one tile out through the door | `entrance path faces wrong way` | Ported. A building with no path reports `has no entrance path` |
| 12 | A 0.7-tile strip along the path touches no other building, prop, yard or resource | `path width obstructed by {id}` | Ported. A yard is named `yard:{i}`, a resource `resource:{item}` |
| 13 | No tile of the rect is river or swept tram corridor | `water` / `swept tram corridor` | Ported |
| 14 | Every tile the path crosses is passable | `obstructed entrance path at x,y` | Ported |

**Added, not in the reference — the door drift check.** The file carries each building's `doorRect` as the
reference worked it out; the validator works it out again from `door`, `facing` and the rect by the ported
`doorRect` rule, and the two must agree: `exported door x,y differs from the door rule's x,y`. This is the same
idea as the importer's solid-mask check: if this port of the door rule ever drifts from the reference, the import
says so instead of checking the wrong door.

## Rules 15–33: the city

| # | Reference rule | Message | Status |
|---|---|---|---|
| 15 | Eight required parcels exist | `Missing required parcel {id}` | Ported, whole city only |
| 16 | Each downtown archetype appears | `Missing downtown archetype {kind}` | Ported, whole city only |
| 17 | Exactly the required plants (3), cores (3), artifacts (3) and tram stops (4), each once | `Missing/duplicate required sites … (no {id})` | Ported, whole city only. The first missing id is added to the reference's message so the fault is named |
| 18 | Four factory yards | `Four factory yards required` | Ported, whole city only |
| 19 | No yard overlaps a road's pavement | `Yard {i} overlaps public pavement` | Ported |
| 20 | No yard tile is swept tram corridor | `Yard {i} overlaps swept rail at x,y` | Ported |
| 21 | Every road edge joins two known nodes | `Missing road node {from}/{to}` | Ported, whole city only |
| 22 | The road graph is one piece | `Disconnected public road graph: {node} cannot be reached from {first}` | Ported, whole city only. The reference gives no id; the first unreached node is named |
| 23 | A node with one edge is a declared end | `Unintended road end {id}` | Ported, whole city only |
| 24 | Each service drive starts on a road | `Disconnected factory service drive {i}` | Ported, whole city only |
| 25 | The tile outside each door can be walked to from the spawn | `{id} unreachable entrance approach` | Ported |
| 26 | Each plant, core, artifact and stop has a reachable tile in the 5×5 around it | `{id} inaccessible binding` | Ported |
| 27 | No prop tile is swept tram corridor | `{id} prop on tram corridor` | Ported |
| 28 | No prop sits on a road or its pavement | `{id} prop on public road/pavement` | Ported, with the reference's own `+1` on the rect |
| 29 | Neighbouring rail samples are at most 0.251 apart along the line | `Rail discontinuity {i} at x,y` | **Ported by derivation** — see below. Whole city only |
| 30 | No rail cell is entered twice | `Duplicate rail cell x,y` | **Ported by derivation.** Whole city only |
| 31 | Heading turns at most 0.03 rad per sample, and position follows the heading | `Rail tangent/position discontinuity {i} at x,y` | **Ported by derivation.** Whole city only |
| 32 | Every sample within 4 tiles of a stop runs straight along an axis | `{id} not on a straight` | **Ported by derivation**, any region. Slightly stricter than the reference — see below |
| 33 | `authoredProblem`: the save agrees with the authored city | `city.json: authored city has no map ID` | **Partly ported** — see below |

### Rules 29–32: ported by derivation

The reference checks its baked centreline with each sample's distance `d` and analytic tangent angle `a`. The
export's `tram.baked` carries positions only. So:

- **`d`** is re-summed as the cumulative `hypot` between samples, which is exactly how `riverfrontRail.ts` defines it.
- **The heading** is the chord between neighbouring samples (the chord into a sample; the chord out of it for the
  first of a run), not the analytic tangent.

This keeps the reference's thresholds meaningful: an arc of radius 12 baked at 76 steps per quarter turn changes
heading by (π/2)/76 = 0.0207 rad per sample, under the 0.03 limit, and samples are at most 0.25 apart, under
0.251. The real city passes with no allowance added.

**The difference, rule 32.** At the sample where a straight meets an arc the reference's tangent is still exactly
on the axis, but the chord into the next sample is not. A stop within about a quarter of a tile of being 4 tiles
from an arc's end could pass the reference and fail here. No stop on the real city is near that edge; each of
the four passes.

### Rule 33: partly ported

`authoredProblem` compares a *save* with the authored city. An import has no save, so only one part applies.

| Part of the reference rule | Status | Why |
|---|---|---|
| The map id is set | **Ported** | An export with no id cannot be matched to a save later |
| The map id equals the hard-coded `RIVERFRONT_ID` | **Not ported** | The id carries the editor's content hash (`…-editor-ac16d9188c05`) and changes on every re-export. A hard-coded copy here would fail every legitimate new export. The importer checks `sourceSha256` and the side-car hashes instead |
| The dimensions equal the authored ones | **Not ported** | The importer takes the dimensions *from* the file and checks each side-car covers them. There is no second copy to disagree with |
| Authored progress, tram binding and site binding in the save | **Not ported** | Save state. There is none at import; the Unity save has its own version and region checks |

## Rules 34–36: `validateFirstRegion`

| # | Reference rule | Message | Status |
|---|---|---|---|
| 34 | Each first camp's key marker tile is passable | `{camp id}: key marker solid` | Ported |
| 35 | Each first camp can be walked to from Home | `{camp id}: no exterior Home path` | **Ported with a difference.** The reference runs `findPath` from Home; this uses the same flood fill from the spawn as rule 25. Both ask "is there a walkable way"; the flood is already worked out and needs no pathfinder in the Editor assembly |
| 36 | Each freight gate tile is a door, not a wall | `Gate x,y: not an existing door` | **Ported with a difference.** The exported solid mask is the static authored mask (buildings, props, substations). A gate tile that is solid there is not a door. Whether a gate is open or shut is runtime state and is not checked |

A camp or gate outside the region being imported is skipped.

## What is not here at all

- **The site's Unity side.** Whether a plant, core or artifact has a `WorldSiteKind` and becomes a Unity site is
  the importer's `SiteRecords` and the count table, not this validator.
- **Anything about play.** The validator proves the city's structure. It says nothing about balance, pacing or
  whether the city is good to play.
