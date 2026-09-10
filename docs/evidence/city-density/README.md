# CITY-F — city-wide density

Implemented 2026-09-10 after the owner approved CITY-E and requested the same approach across the rest of the city. Existing artwork and gameplay rules are retained. Human review of this expanded layout remains separate from the owner's approval of the starting neighbourhood.

## Result

The final layout is `riverfront-arc-v4-editor-ac16d9188c05`: **479 buildings**, up from 301. Seventy-seven smaller street blocks are rebuilt using district-specific templates; ten existing civic blocks retain their large buildings and gain forecourts/details. This replaces 237 generic background buildings with 415 buildings and adds 303 garden/street/loading details. No new loot, interiors, enemies, rewards or progression is implied by the scenery.

| District blocks in this pass | Before buildings | After buildings | Before footprint area | After footprint area |
| --- | ---: | ---: | ---: | ---: |
| Northwood Industry | 20 | 40 | 4,740 | 10,380 |
| North Gardens | 28 | 52 | 2,800 | 4,800 |
| East Gardens | 44 | 81 | 4,400 | 7,488 |
| Westridge | 62 | 116 | 6,200 | 10,752 |
| Old Town / southern commercial streets | 64 | 98 | 7,520 | 14,880 |
| Civic Approach | 12 | 12 | 1,200 | 2,688 |
| Civic Centre | 10 | 10 | 8,144 | 8,144 |
| East Wharf street blocks | 7 | 16 | 690 | 1,536 |

Areas are building footprint tiles, not district bounding-box percentages. The 54 named/non-generic buildings outside this table remain unchanged. Residential streets alternate garden rows and inward-facing courts; commercial streets use terrace rows and shops; industrial blocks use warehouses/workshops around loading courts. Existing civic landmarks receive paved forecourts. Campaign compounds, quarry working space and factory construction yards intentionally retain open space.

The approved Founders Court / CITY-E neighbourhood is preserved exactly. All campaign buildings and bindings, roads, 371 road edges, tram geometry, resources, factory yards, service drives, power positions and world dimensions are unchanged. Full validation reports **345,246 reachable tiles** and zero errors. Denser building footprints reduce the former 371,093 reachable tiles; the world itself has not shrunk. The older CITY-C test's 2.8–3.2× *empty walkable area* expectation is now an extent check against land area (412,634 versus the original 138,868). All entrance and campaign access requirements remain enforced. Older evidence is unchanged.

## Authoring and editor

`packages/tools/src/authorCityDensity.ts` compiles the archived CITY-E input through deterministic block templates, reservation checks, reachable entrance generation and `validateRiverfront`. It refuses to overwrite later source/scene edits. [Two consecutive runs](repeat-check.txt) produce identical canonical source. Reservations come from the archived input, so a rerun cannot mistake its own new paving for a pre-existing exclusion zone. The original full-city Python compiler's drift guard remains in place.

`connectEntrance` now offers an outward apron before turning into a shared court; this fixes the genuinely new side-facing middle-home layout while retaining clear full-width paths. Existing CITY-E entrance paths are unchanged.

The native Phaser scene contains **959 objects**: one reference image, 479 building Images and 479 editable plot Rectangles. [Native editor record](native-editor.json). Native object IDs were retained and the manifest rebound by verified unique labels/geometry after refreshing the scene. [The saved scene check](editor-check.txt) is a no-op against the final canonical map. [Editing instructions](../../PHASER_EDITOR.md) still apply. New artwork remains a separate next step.

## Visual review and checks

- [Whole-city overview](city-overview.png), [Westridge](scene-westridge.png), [Northwood](scene-northwood.png), [Old Town](scene-old-town.png), [Civic approach](scene-civic-approach.png), [East Gardens](scene-east-gardens.png), [southern streets](scene-riverside.png): actual Phaser rendering of saved scene images with plot outlines hidden for readability, not native editor screenshots.
- Actual game views: [industry](northwood.png), [Old Town](old-town.png), [courtyard housing](north-gardens.png), [civic district](civic.png), [East Gardens](east-gardens.png), [wharf streets](east-wharf.png), [unchanged starting area](founders.png). Long repeated district prefixes were removed after reviewing label overlap in denser streets.
- [15 focused checks](focused-tests.txt) plus [three retained city/factory/save checks](retained-city-tests.txt) pass. Tests cover district density, exact retained geometry, all entrances, truck streets, save identity, court aprons, editor add/move/plot validation and original Court behavior.
- [Typecheck/build](typecheck.txt), [lint](lint.txt), [editor validation](editor-check.txt), and documentation synchronization pass. Freshness retains historical findings rather than rewriting campaign evidence.
- [Browser record](visual-review.json): district views, held-key collision, retained interior entry/roof fade/exit and browser save/reload. Setup uses debug position/time in an isolated browser context. Short 120-frame view samples are rendering observations, not a campaign/raid performance gate.
- [Temporary edit/game/save/restore](roundtrip.json) validates a moved plot and added building in the real game, rejects an invalid campaign-anchor move, then restores the exact final city source.
- [Broad suite result](full-tests-summary.json) is a bounded partial run: 46 passes / 8 failures before the 180-second cutoff. Every reported failure name also appears in the preceding CITY-E partial run. This is not a full-suite pass; no broad regression repair or release verdict is claimed.

Reload `http://127.0.0.1:5178/?view=world` and choose **New Game**. Phaser Editor Play remains on 5190. Previous saves remain intact and require their matching map/build. Nothing was committed, pushed or published.
