# P9-03 — saved names and map wayfinding

Implemented 2026-09-08 under the owner's “Continue with P9-03” ([D-EX-49](DECISIONS.md)). **Implementation and automated verification complete.** [Scope](PHASE_9_SCOPE_REPORT.md), [task status](PROGRESS.md).

## Player behavior

Open **Map and navigation** in the existing sidebar. Select a known place or district, change its name, or restore the generated default. **Show destination** moves the camera and brings the canvas into view; **Return to engineer** restores camera following. The selected destination has a persistent compass/distance cue, including an off-screen indication. Distances are straight-line tiles from the engineer, not a promised walking route or safe passage.

**Pin my position** adds a marker at the engineer; **Shift-click the map** adds one inside a known district. Clicking a place/pin marker selects it without issuing a walk command. Pins can be renamed or removed, and the selection can be cleared. Existing ordinary map walking and station inspection remain available away from destination markers. Pin markers appear in both views; the selected target is highlighted on the map.

Existing deterministic urban names supply defaults. Overrides share stable IDs across the district HUD, labels, journal, relevant inspection text and navigation controls; restoring a default does not change identity. Place and district records keep separate IDs. The selector/status includes the kind/identity, and renaming refuses a duplicate name among currently known destinations. All player text is rendered as text, including markup-shaped names.

Visited districts and known-but-unvisited destinations are distinguished explicitly. New campaigns start with home visited; simulation ticks record the engineer's physical district and nearby known sites. A site visit uses its existing footprint centre and a four-tile margin. Knowledge comes from existing discovery/restoration/recruit records; pins and camera movement reveal nothing. Surveyors retain their district-information unlock. No hidden installation, resource, reward or future assault target is added to the navigation list.

## State and compatibility

`packages/sim/src/navigation.ts` owns validated ordinary annotation commands, optional saved navigation version 1, visit tracking and read-only destination queries. Task-level limits are **64 characters per name, 80 place/district overrides and 32 pins**; IDs increase monotonically and cannot be reused by removal. Coordinates, IDs, versions, bounds, known targets, collections and text/control characters are checked during commands and save import. Rejected commands preserve state and inventory. Selection, camera position and UI focus remain presentation-only.

Existing layouts, surveys and stock are retained on load. Older saves have no inferred historic visit record; tracking begins when simulation resumes, with home and the current position. The loader preserves the supplied log as evidence while refusing to claim fresh replay across the introduction of automatic visit tracking. Current saves preserve complete replay. Save schema 3 and campaign metadata 10 remain unchanged; the optional navigation record is independently versioned. Campaign fingerprint changes from `9fc92bfc` to **`bd3b8939`**.

The existing map/world/panel architecture is reused. No UI redesign, new minimap, global inventory, pathfinding rule or progression change is included. D-UI-02 keeps RI-02B after P9-05.

## Verification

- **336/336 full simulation tests pass**, zero failed/skipped, in 598.94 seconds; [log](evidence/p9-03-2026-09-08/test.log).
- **9/9 focused tests pass**, including five new navigation cases and four retained survey compatibility cases; [log](evidence/p9-03-2026-09-08/focused.log). They cover deterministic knowledge filtering, names/defaults/literal text, pin limits and monotonic IDs, atomic refusals, corrupt saves, ordinary walking/visits, conservation, replay and old-save behavior. The full run also covers the later lightweight visit-query implementation.
- Types and production build pass; [full build](evidence/p9-03-2026-09-08/typecheck-verified.log), [final game build](evidence/p9-03-2026-09-08/game-build-final.log). [Lint passes](evidence/p9-03-2026-09-08/lint-final.log). The intentional control-character rejection has a narrow documented lint exception; Vite retains its existing chunk-size advisory.
- **8/8 composed city checks pass** on 3/4/5/6/8/11/13/29, with repeat/load equality, unchanged inputs and stable source identity; [summary](evidence/p9-03-2026-09-08/city/summary.json). This is a compatibility sample, not P9-04's 10,000-seed obligation.
- **12 regenerated campaign experiments / 78 checks pass**, including three five-hour logistics runs; [log](evidence/p9-03-2026-09-08/campaign.log). Previous exact results are preserved in [archive](evidence/p9-03-2026-09-08/previous-campaign.zip) and [hash record](evidence/p9-03-2026-09-08/previous-campaign.json). E-chain uses ordinary supplies; E-coal/E-tram/E-logistics retain their declared stock/threat assistance and are not unassisted balance evidence.
- Headless Chrome verifies actual controls at **1366×900 and 900×900**: rename/reset, literal text, pin creation/removal, map Shift-click, marker selection, camera/hash invariance, typed-key isolation, held movement cleared on focus capture, save/reload, exact replay and visited prepared state. [Browser record](evidence/p9-03-2026-09-08/browser-result.json), [driver](evidence/p9-03-2026-09-08/browser.cjs), [final log](evidence/p9-03-2026-09-08/browser-final.log).

The initial focused failure was a malformed-test fixture using JavaScript's special object-literal `__proto__` syntax; the corrected imported JSON case is refused. Initial long runs were interrupted before removing unnecessary service-description calculations from the visit tick. Initial browser-driver mistakes (an overly broad request interception and asserting before scheduled UI/pause updates) are retained. Screenshot review identified and fixed narrow-screen camera navigation leaving the canvas above the page; the final browser run includes that fix. Earlier screenshots/results are retained in [browser-first.zip](evidence/p9-03-2026-09-08/browser-first.zip).

Final [world at 900](evidence/p9-03-2026-09-08/world-900.png) and [map at 900](evidence/p9-03-2026-09-08/map-900.png) were opened and inspected after the scroll fix; both 1366 views were also inspected during the first run. The destination, cue and map markers remain visible. This is developer verification, not independent human usability feedback or frame-performance certification. [Prepared starts](evidence/p9-03-2026-09-08/prepared.json) disclose ordinary scripted walking and annotation commands without stock/position/unlock injection. EX-08D/5180 remains frozen.

## Provenance and handoff

[201-input source archive](evidence/p9-03-2026-09-08/source.zip), [six-file build archive](evidence/p9-03-2026-09-08/build.zip), [hash manifest](evidence/p9-03-2026-09-08/manifest.json) and [review delta against P9-02](evidence/p9-03-2026-09-08/source-delta.patch) identify the final implementation. Experiment stamps retain their actual startup source identity: after startup, navigation.ts gained only the intentional lint comment, and main.ts gained the browser-only scroll correction. Their exact earlier versions are retained in the evidence directory; simulation behavior used by the experiments is unchanged, and the final renderer is separately built and browser-verified. No results were restamped to hide these differences.

[Documentation sync](evidence/p9-03-2026-09-08/docsync.log), [freshness](evidence/p9-03-2026-09-08/freshness.log), [integrity checker](evidence/p9-03-2026-09-08/check.py) and [verification record](evidence/p9-03-2026-09-08/verification.json) cover current results and preserved P9-02/P9-01/EX-08D evidence. The existing scope-report CRLF-only difference and unrelated power4 fixture formatting remain preserved.

**P9-04 is next:** the resumable, declared 10,000-seed city-fairness run. P9-05 remains engineering review and refreshed human preparation. Human ten-seed/shared play and P6-AMMO remain open; no human result, phase verdict, commit or push is inferred.
