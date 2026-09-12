# RI-02A — City Structure and Visual Identity

Implemented 2026-09-06 in the working tree based on `b3a3f6c8f42a09841102287ac195a424c1f05e1f`.
Task status is only in [PROGRESS](PROGRESS.md). No gameplay prices, recipes, claim rules,
enemy rules, unlocks or light radii were changed. No new encounter was implemented.

## Diagnosis and delivered layout

The normal build was using the real river street-first generator, seed 3, 800×800,
with curved arterials, a diagonal avenue and irregular faces. It was not a stale
lattice or fallback. The main problems were the map-first entry, distant camera,
resource-field rendering, weak street materials, absent building silhouettes,
coordinate-heavy labels and dark overlays. Baseline metadata is
[baseline-capture.json](evidence/city-rebuild/baseline-capture.json).

Normal new games now use the saved profile `riverside-v1`. It composes the existing
HQ neighbourhood with a real Depot facade, protected service yard, Founders Court,
Ironworks Yard, Exchange Square and Switchyard. Local anchors are chosen from
connected nearby lots; named industrial/civic sites must have a suitable building
when one exists nearby. Industrial selection can reuse a nearby 10×6 shell as a
smaller shed if the nominal 12×6 footprint will not fit. The residential court and rail yard provide
different immediate HQ exits; the civic and industrial sites extend exploration. Street
topology, ownership, facility bands, river, inert faces and start fixtures remain
the existing validated generator's. This is substantial lot infill and presentation,
not a replacement street algorithm. No decorative driveway adds a front edge.

Existing street widths remain 6–8 tiles for locals, 10–12 for arterials, 10 along
the embankment and 8 at the boundary (`city/generate.ts`). The shared urban
configuration (`city/urban.ts`) adds two-tile pavement and access clearances,
6×4 residential shells, 12×6 sheds, 10×6 civic buildings and 14×5 rail sheds,
rotated where needed. Structure coverage is capped at 12% per lot; the HQ adds
no new solid tiles. Protected pads are 24×24 at HQ, up to 12×12 residential and
16×16 elsewhere. Pads are reservations against decorative structures, not an
override of rubble, ownership or machine placement rules. Existing roads provide
the main approach, service routes and loops; no new alley graph was necessary.

The one derived structure mask drives rendering, walking/pathfinding and build
placement. Rubble beneath new shells moves within its original lot with its type,
quantity and depletion rank preserved; HQ patches and rail coal retain their exact
locations. This changes some later supply paths and is a real capacity change,
not just decoration. Rail metadata protects the real installation, two reachable
feeder approaches and a 16-tile loading area. The existing Heart remains optional;
these reservations add neither rules nor transport unlocks.

The code-native visual kit supplies kerbs, asphalt, quay surfaces, road markings,
roof silhouettes, south-facing door approaches, loading rails and warm restored
windows. No downloaded assets or external licences are involved. World and map
draw the same structure geometry. The default world frame enlarges the engineer;
wheel zoom supports construction and neighbourhood views, and M remains the map.
Local place names replace persistent coordinates; debug inspection retains them.
Placement/rifle selection can show the exact binary light boundary beneath the
softer visual falloff. Shade targetability and gameplay illumination are unchanged.

## Actual game captures and review

Playwright drove headless Chrome against the normal Vite application. In-app browser
attachment was unavailable. All captures use device scale 1. The reference is seed
3, config `d51dfee0`; the [manifest](evidence/city-rebuild/screenshot-manifest.json)
records camera, zoom, viewport, tick, profile, browser errors and performance samples.
Baseline images remain from before editing. Screenshot-camera relocation is labelled
QA; walking evidence comes from commands, not those relocations.

| Inspection | Before | After |
|---|---|---|
| Fair opening, same engineer/camera, 1920×1080, 0.5× | [Baseline](evidence/city-rebuild/baseline-opening.png) | [Rebuilt](evidence/city-rebuild/revised-matched-opening.png) |
| Normal opening frame | [Baseline](evidence/city-rebuild/baseline-opening.png) | [New default](evidence/city-rebuild/revised-opening.png) |
| Construction zoom | [Baseline](evidence/city-rebuild/baseline-construction.png) | [Rebuilt](evidence/city-rebuild/revised-construction.png) |
| Strategic map | [Baseline](evidence/city-rebuild/baseline-overview.png) | [Rebuilt](evidence/city-rebuild/revised-overview.png) |
| Matched Founders Court camera | [Dark](evidence/city-rebuild/street-dark.png) | [20-minute restoration](evidence/city-rebuild/street-restored.png) |

Additional inspected views: [factory](evidence/city-rebuild/factory.png),
[residential](evidence/city-rebuild/founders-court.png),
[industrial](evidence/city-rebuild/ironworks-yard.png),
[civic](evidence/city-rebuild/exchange-square.png),
[rail](evidence/city-rebuild/switchyard.png),
[seed 4](evidence/city-rebuild/seed-4-neighbourhood.png),
[seed 5](evidence/city-rebuild/seed-5-neighbourhood.png),
[seed 6](evidence/city-rebuild/seed-6-neighbourhood.png),
[2560×1440](evidence/city-rebuild/opening-2560.png) and
[1280×720](evidence/city-rebuild/opening-1280.png).

Concrete refinements from image inspection: fractional camera rounding produced
black tile seams, so camera rounding was disabled; resource colours and overlays
were subdued; the engineer gained a minimum screen size; HQ label duplication was
removed; wayfinding uses the actual bearings; industrial/civic names were moved
from empty reservations to fitted buildings. Restoration visibly adds windows,
connected poles/belts, brighter street approaches and a new frontier. The small
viewport wraps the existing HUD without horizontal document overflow. The strategic
map deliberately retains the existing ownership palette. Recognisability is
AI-reviewed and still needs a cold-player check; no fun or human acceptance claim.
Some headless captures show partial HUD text texture artifacts, most visibly the
factory view's controls strip; those still need checking in the interactive GPU build.

## Verification and limits

[citycheck.ts](../packages/harness/src/citycheck.ts) runs the normal game's config
`d51dfee0`, checked against a reconstructible provenance recipe. Results are in
[compatibility.json](evidence/city-rebuild/compatibility.json).

| Seed | Blocks / inert | Structures / solid tiles | HQ lot tiles | Local-ring QA: held / interior / front |
|---|---|---|---|---|
| 3 | 382 / 35 | 343 / 18,598 | 780 | 5 / 1 / 10 |
| 4 | 373 / 36 | 360 / 19,390 | 1,197 | 4 / 1 / 6 |
| 5 | 349 / 34 | 362 / 19,656 | 891 | 4 / 1 / 7 |
| 6 | 370 / 36 | 352 / 19,450 | 949 | 4 / 1 / 6 |

- Every tested seed passes existing city validation. Ownership, starting frontage,
  inert faces, resource totals and enclosure geometry match legacy exactly. The
  local-ring check explicitly grants Held status to measure geometry; it makes no
  15–25-minute pacing claim.
- Commands walk from the workbench to every live HQ neighbour and named landmark
  and back. Doors, rail reservations and existing candidate cabinet sites are
  reachable. Every structure tile is
  solid, unbuildable and free of covered resources.
- At 20 minutes the real factory bot has three excavators and produces 129 magazines
  on every seed. Actual placement validation fits another two assemblers and a
  generator; only this capacity QA grants carried machines. Seeds 3–5 have no
  refusals. Seed 6 runs short of steel for nine later belt placements and the second
  assembler at 16:56–17:34; the first ammo line still operates. This is a disclosed
  route/economy sensitivity, not a tuned-away failure or evidence of a complete
  second operating line on every seed.
- Real tram runs on seeds 3/4/5 deliver 259/258/260 items by 35 minutes with no
  refusals. This checks the implemented grid tram, not a future long-vehicle model.
- New saves reproduce state hashes and identical derived geometry; profile-less
  saves retain legacy masks. Unknown profiles fail clearly. Generation attempts
  are bounded to 16 and included in cache keys; playable specs reject invalid cities.
- Typecheck including production build, lint, docsync and original snapshot
  reproducibility pass. Vite retains its large-chunk warning. Commands ran on Node
  24.20; the prescribed Node 22 environment was unavailable.
- Baseline tests: 149/154 pass, five Heart failures. Final full run: 150/156 pass,
  the same five Heart failures plus the existing 100 ms cold-ground timing assertion
  during concurrent browser/build work. New urban tests pass. See
  [baseline tests](evidence/city-rebuild/baseline-tests.log) and
  [final tests](evidence/city-rebuild/final-tests.log); the isolated timing rerun is
  [passes](evidence/city-rebuild/timing-isolated.log), recorded separately rather
  than erasing the overloaded run.
- The full legacy experiment suite ran to a separate evidence directory: 15
  experiments, four failing checks, all E-heart; the other 14 are green. Heart
  preparation, packets/retry and accounting remain existing RI-06 issues.
  [Full results](evidence/city-rebuild/legacy-suite/EXPERIMENTS.md).
  Legacy calibration misses C3/C7 and informational i1/i2; this is legacy-profile
  evidence and does not validate the new layout's pacing.
- Freshness reports exactly 12 pre-existing stale archived lattice files. New QA
  snapshots have matching config stamps. Historical files were not regenerated.
- Geometry is cached per state; the renderer retains visible chunks and avoids
  rebuilding city geometry in its frame loop. Loaded-neighbourhood measurements
  are recorded in the screenshot manifest. Headless software rendering is slow:
  the final sample delivered 25 frames in 3.025 seconds despite Phaser's capped
  delta reporting 60 FPS. That is about 8.3 actual FPS, not a performance pass.
  Visible-world draw time averaged approximately 3 ms by EMA, with a 52.6 ms
  maximum; opening the map took 653 ms including an intentional 500 ms wait. A GPU
  reference-machine soak and reliable comparative frame benchmark remain unrun.
  Cold generation was 0.9–4.1 seconds under concurrent checks, also not a clean
  hardware benchmark. No claim of meeting the reference performance target.

## Reproduce and handoff

Run `npm ci`, then `npm run dev` from the repository root; open
`http://localhost:5173/?seed=3`. Normal profile: `riverside-v1`; legacy geometry:
`?seed=3&city=legacy`. M toggles strategic map; wheel zooms. QA factory snapshot:
`?view=world&state=city-factory-qa` (paused at 20:00, press P).

Focused sim evidence: `node --import tsx packages/harness/src/citycheck.ts`.
Browser evidence: `node packages/game/capture-city.mjs` with Playwright available,
or `RELIGHT_PLAYWRIGHT` pointing to its installed package. Baseline captures require
the pre-edit revision and must not be overwritten as a purported new baseline.

Moved Phase 9/12 geometry and presentation work is recorded in [PHASES](PHASES.md).
Broader preset/fairness work, finished art/audio and human review remain there.
Stop here. The next integration task is RI-07, after repair/verification of the
existing RI-06 failures; this pass does not implement it.
