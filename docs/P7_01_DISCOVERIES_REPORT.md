# P7-01 — persistent discoveries and Electricians

Date: 2026-09-07. Branch `codex/tram-expansion`, base HEAD `17939145b5d40afe4dcfc1937a3ec6d34b763df6`, with prior uncommitted Phase 6 work. Authority: D-EX-35 / adopted Q09, owner “All looks good! Continue on with P7”. Human play follows P7.

## Change

Campaign metadata version 6 adds a typed recruit catalogue and saved records with stable ID, kind, location, seen/recruited timestamps and inherited-unlock provenance. Existing station/radio/workshop and field-repair cache records retain ownership of their restoration/reward state; no parallel copy can re-award the schematic or resurrect its guardian.

A shelter beyond the home court introduces the Electricians. Approach on foot and use E over the shelter or the panel button to recruit. No payment, block claim or new base registration is added. Floodlight and Big pole plans unlock permanently; construction still uses carried materials, actual footprints and physical power. Substation remains available at the start. Disabled cores do not remove learned plans.

The marker appears after local discovery; the panel explains the reward and prevents remote/repeated recruitment. Legacy survivor/project debug information is hidden in the campaign because its Held-block recruitment statements do not apply. P7-04 still owns comprehensive discovery/recipe presentation.

Provisional placement: 24-tile clue radius; nearest safe buildable lot reached by deterministic cardinal traversal from the home gate; one reserved walkable tile; more than 12 tiles from creature origins and workshop cache, with clearance from restoration/source pads, stops and surveyed track. Static geometry determines placement independently of saved machines/truck location. Migration never removes a machine: interact beside the marker or clear obstructing construction. Fresh-seed tests use actual paths with starting machines present.

## Compatibility and validation

Older saves receive the recruit record once. Old Electricians unlocks earned through a Held or fallen block remain earned; unearned plans stay locked. Machines, cargo, defence schedule and workshop cache/guardian are preserved. Current saves reject missing, duplicate or malformed recruit records. Outer save schema remains 3; upgraded older logs are retained as evidence but marked incomplete for current-code fresh replays.

Tests cover 32 seeded paths/reservations, ordinary paid acquisition/use of both machines, repeated/refused recruitment, impaired/embarked interaction, original EX-08B checkpoint migration, earned/unearned unlocks, malformed records and saved/full replay. Old preview fixtures explicitly remove the new metadata. Initial checks caught headless fixture issues (browser location, URL parsing and a boolean field); these were corrected before final verification.

Final results are in `docs/evidence/p7-01-2026-09-07/`. No human discovery verdict, ammo retest, balance approval or reference-machine performance pass is inferred. P7-02 is the next increment; P7 completion and refreshed shared preparation remain outstanding. No commit or push performed.

## Final results

- **255/255 tests passed**, including five P7 tests and the declared 32-seed path sweep: [full log](evidence/p7-01-2026-09-07/tests.log). Paid placement/replay also passed its targeted rerun. The final on-foot check uses the campaign `truckSeat` flag, not the legacy truck toggle.
- All-package [typechecks and production build](evidence/p7-01-2026-09-07/typecheck.log), [lint](evidence/p7-01-2026-09-07/lint.log), [legacy snapshot](evidence/p7-01-2026-09-07/snapshot.log), [docsync](evidence/p7-01-2026-09-07/docsync.log) and [freshness](evidence/p7-01-2026-09-07/freshness.log) passed. The existing Vite large-chunk advisory remains.
- [Browser results](evidence/p7-01-2026-09-07/browser.json): actual panel button at 1366×900 and keyboard E at 900×900, unchanged recruitment stock, paid Floodlight/Big pole construction, full replay and Ctrl+S/O save/reload; no page errors or horizontal overflow. The initial keyboard probe omitted the canvas page offset; correcting pointer coordinates passed without an interaction-code change. Both panel screenshots were inspected. Isolated headless Chrome 153, using command hooks for stock/walking/placement, is automated evidence rather than a human playtest.
- [Manifest](evidence/p7-01-2026-09-07/manifest.json) identifies current source/build and preserves source/build archives. Prior EX-08A/B, P6 and scope-review evidence remains unchanged. These small interaction checks do not close earlier light-map stalls or reference-machine validation.
