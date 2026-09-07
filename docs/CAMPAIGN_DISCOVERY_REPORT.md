# Campaign discovery — EX-07

Date: 2026-09-07. Authorisation: D-EX-18, owner request “OK lets implement EX-07”, within Q05/06. Branch: `codex/tram-expansion`, baseline `1849081`. Changes are uncommitted. Phase 4 remains complete; Phase 5 has not started.

## Implementation

One marked workshop records cache occupies a reachable side yard in the workshop neighbourhood, clear of essential source pads, restoration sites and the surveyed tram route. Its ID includes the seed and schematic identity. The world label, inspect text and optional panel section explain the reward, guardian, warning and alternatives without making the detour a required goal.

The existing Stalker state machine, pathfinding, renderer and rifle/turret targeting are reused. The campaign stores its own guardian layer, separate from the legacy well/respawn candidate. This guardian has one lifetime, 24 HP, 5-tile activity perception and an 8-tile home leash. Movement, paid rifle shots (including misses) and construction attract it. Each strike visibly winds up for 1.2 seconds and deals 5 HP; ordinary dodge and knockdown behaviour applies. It returns when the engineer leaves its territory and never retires merely because its district is commissioned. Obstructed return paths retain the guardian rather than fabricating a kill or respawn.

Recovery uses an ordinary command with the exact site ID and normal engineer reach. The guardian must be more than 3 tiles from the cache, or defeated. Drawing a living guardian away is sufficient. Death grants no automatic item or reward. Recovery permanently fits the field-repair tool, taking no pocket slot. New manual repairs take half the normal duration: up to 40 HP in 2 seconds for 2 steel + 1 copper, or disabled-core recovery in 6 seconds for 10 steel + 5 copper. Already-paid repairs keep their remaining duration; material charging, reach pauses and disabled-core restrictions are retained. Workshop service is unchanged.

These values, one guardian lifetime and automatic fitting are provisional implementation defaults within the approved discovery/equipment contract, not validated balance or new human approvals.

The wider seed check also found an existing EX-06 blocker on seed 11: the first station stop occupied the terminal’s sole valid street continuation. The initial station-pad search now keeps an unbranched street exit clear. Existing saved station layouts are not moved. The seed is included in the new reachability regression.

## Saves and evidence identity

The campaign save envelope remains schema 3; campaign metadata advances to 5. The persistent record contains location, clue/recovery times, guardian body/path/wind-up and lifecycle statistics. Partial current records, duplicate/inconsistent guardian histories, invalid reward IDs and malformed paths/tuning are rejected instead of recreated. Older previews add the record once; a guardian is not initially spawned within perception of the engineer. Prior-version logs are marked incomplete for fresh-factory replay. A recovered tool survives loading without another grant or enemy respawn. The campaign evidence hash now includes discovery settings; legacy hashes and candidate rules are preserved.

## Verification

Final verification passed: **195/195 simulation tests**, 9/9 focused discovery checks, all package typechecks and production build, lint, both documentation profiles, artifact freshness and the unchanged legacy three-hour snapshot. Creation and save validation passed for all 32 seeds in the wider sweep. Vite retains its existing large-bundle advisory. Raw initial and final logs remain distinct in [the evidence directory](evidence/campaign-discovery-2026-09-07/); final suite/build/lint logs use the `-final` suffix, focused coverage is in `focused-seed11.log`, and the corrected sweep is `seeds-final.log`.

The focused tests cover:

- Essential sites reachable while excluding the entire guardian territory plus contact radius, on seeds 3, 4, 5, 8, 11, 13 and 42; the cache itself is reachable and reserves its tile.
- Per-strike wind-up, a real dodge, knockdown, return/leash behaviour, no retirement on district ownership change, and deterministic continuation after a mid-wind-up save.
- A paid turret firing six actual rounds, killing the guardian without engineer damage; recovery remains separate and once-only. Material conservation holds in the labelled construction fixture.
- Recovery while the guardian remains alive after a walking distraction; rejection of wrong IDs and out-of-reach commands.
- Faster paid repairs, paused progress, unchanged existing jobs, disabled-core recovery and refusal without materials.
- Deterministic old-preview initialization and rejection of malformed current saves.
- A fresh campaign using ordinary starting ammunition, walking, aimed shots and recovery commands, with an identical complete command replay and saved continuation. Seed 3 records 75 commands, recovery at simulation second 17 and final tick 388/state hash `82b5c543` after the continuation.
- A stationary engineer’s paid missed shot attracting the guardian, matching the clue.

Combat/construction probes explicitly relocate the engineer or supply finite fixture materials; these isolate mechanics. The separate fresh replay uses the unchanged starting campaign and no injected progress. The additional [32-seed sweep](evidence/campaign-discovery-2026-09-07/seed-sweep.ts) checks creation and save validation; detailed encounter bypass coverage is the seven-seed focused test. Neither measures broad seed fairness or campaign pacing.

Initial failures exposed the missing command forwarding and tests that tried to advance a deliberately paused load; these are corrected. The wider sweep exposed the terminal-pad issue described above. Failed logs are preserved and no historical expectation or artifact was regenerated to pass.

## Browser observation

Automated local browser observation used seed-3 labelled snapshots. The workshop records label, home/perception rings and yellow attack wind-up were visible at the normal world zoom. The live guardian disabled recovery with a useful explanation. In the defeated-guardian snapshot, clicking the normal recovery button changed the panel to “Field-repair tool fitted”, displayed the exact new repair times and unchanged costs, and disabled the button with an empty-cache explanation. This exercised the actual queued command while paused. No browser save slots were overwritten. Temporary snapshots/tabs and the loopback server were removed/closed/stopped after observation. See [browser observation notes](evidence/campaign-discovery-2026-09-07/browser.md).

## Acceptance and remaining work

Implementation and automated evidence are separate from human play. Direction/implementation authorisation: D-EX-11 and D-EX-18. Human play: not run. EX-08A is next for representative-loop evidence and session preparation; EX-08H remains the human play gate. Difficulty, repair-tool value, clue discovery, maintenance burden and pacing need that observation. No Phase 5 or release gate is claimed, and no commit, merge or push was performed.
