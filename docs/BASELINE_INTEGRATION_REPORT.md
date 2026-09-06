# EX-01 — baseline comparison and integration plan

2026-09-06. Read-only comparison complete. The owner subsequently authorised bringing in the newer version and starting tram-based expansion. Phase 4 remains complete; the adopted opening contracts are D-EX-11. Integration is recorded separately from this comparison.

## Conclusion

Use fetched main `b32e4c3` as the code baseline, preserving the current Version 2 documentation. Local committed HEAD `678478a` is its merge base and ancestor; main already contains all local committed code. There are no local-only committed changes. Current uncommitted changes are the authorised documentation work, plus pre-existing `.serena/` and `docs.zip` which are not part of integration.

A branch from the current workspace can first commit the reviewed documentation, then merge main. Resolve overlapping current design/state/tracker/entry-point documents in favour of the new adopted design while carrying main's implementation and evidence into the updated handoff. Preserve the new local AGENTS guide and newer main's useful repository conventions. Keep main's reports, tests, fixes and evidence. Do not reset or replace the workspace blindly.

## Reuse inventory

| System | Verified source finding | Integration treatment |
|---|---|---|
| City | Main adds deterministic urban structures, shared solid masks, named landmarks, open factory yards and a rail reservation. It preserves existing ownership/resource identity rather than implementing the new cul-de-sac. | Retain together: urban generation, ground/walk/placement masks and rendering; adapt opening later. |
| Heart/project | Main fixes preparing-state detection from cabinet deliveries, original attempt identity on completion, bot supplies/patrol and observations. | Retain fixes; relocate/reframe the encounter only when implementing the adopted opening. |
| Inventory | Main moves stand-in hopper returns before buffer distribution, preventing ammunition loss. | Retain accounting fix and ledger coverage. |
| Transport | Current route traversal can visit encountered stops on an unbranched line; the validated scenario has two stops. Cargo is indiscriminately unloaded at each powered stop; branches invalidate the route. | Reuse track traversal/storage; add explicit station requests, exports and destination reservations. Do not claim route traversal itself must be rebuilt from zero. |
| Construction | Existing field kit requires adjacency to Held ground; track runs on streets; production depends on Held/power. | Version 2 remote bases/corridors need explicit replacement of these conditions, not only a new UI. |
| Threat/territory | Block-step code produces ordinary blooms, maintains per-edge demand, wells and fall/recovery. | Isolate legacy rules before adding new base assaults; adding a timer alone would retain the old continuous pressure. |
| Combat/repairs | Existing rifle, movement, knockdown, Stalker and lights/cabinets supply primitives. New wall/turret/core repair and workshop service are not complete. | Reuse primitives and implement the approved damage/service contract as a coherent later slice. |
| Saves/replay | Current envelope/state use version 1. Main preserves optional city profile across new sessions, loads and replay and rejects unknown profiles. Missing profile preserves legacy geometry. | Keep compatibility; schema/feature version new base/scheduler/cargo state before exposing new commands, with unsupported saves explicitly refused. A city geometry profile is not a gameplay-rules profile. |
| Tooling | Main serialises test workers and fixes Windows archive-path recognition in freshness checks. | Retain both. Ubuntu and an installed Node 22 runtime can run the existing Linux-built dependencies without reinstalling them. |

## Reproduced checks

Checks used installed WSL Ubuntu and Node 22.18.0. Fetched main was extracted into an isolated temporary directory. Workspace package links for that copy point to its own packages; third-party dependencies are reused from the existing install. No branch switch or source overlay was needed to test it.

| Check | Result | Scope |
|---|---|---|
| Local Heart, goal/save and project tests | 8 passed, 5 failed (13 total) | The five failures are existing Heart preparation/state and replay issues. |
| Main same files plus urban tests | 15 passed, 0 failed | Includes geometry/resource/placement/route preservation, profile rejection, project freight and save/replay checks. |
| Updated local documentation sync | Passed after correcting GDD line endings | All generated blocks and recorded play tags recognised. The first actual run found CRLF prevented the parser recognising markers; converted active GDD to LF without changing table content. |
| Local evidence freshness | Passed | Existing generated evidence has expected config hashes and ancestor stamps. |

Evidence: [local tests](evidence/baseline-2026-09-06/local-tests.log), [main tests](evidence/baseline-2026-09-06/main-tests.log), [docsync](evidence/baseline-2026-09-06/local-docsync.log), [freshness](evidence/baseline-2026-09-06/local-freshness.log). Adjacent JSON records identify runtime, paths, exit and duration. These are new runs, not a relabelling of old reports.

The local failures concern cabinet delivery showing discovered rather than preparing; insufficient preparation supplies causing refusal in the Heart completion, retry and power tests; and save continuation hash divergence. Main's matching tests pass. Main's committed evidence also reports broader checks and a 156-test suite passing, but this comparison did not rerun the full campaign/experiment suite or browser/performance tests. It does not assert live remote CI status.

## Concrete integration sequence

1. Preserve the documentation in a commit on a dedicated branch, leaving unrelated untracked files alone.
2. Merge fetched main; retain newer code/tests/evidence and reconcile overlapping docs with the adopted contracts.
3. Verify the integrated snapshot and documentation, then implement an independently reviewable first transport increment: selected station demand/export, reserved cargo and return freight, using commands and saves with visible controls.
4. Establish explicit new-game rules/state profiles before replacing ownership/frontage with bases, adding the assault schedule or promising the complete new opening. New save fields must not be silently interpreted by old readers.
5. Continue the approved phase/task sequence with the cul-de-sac, early kit, radio, specialised extraction and repairs. The first freight increment does not claim those systems are complete.

## Risks and limits

Overlapping files include CLAUDE, phases, programme state, progress and GDD; AGENTS exists as an untracked new local file and a tracked main addition. The main commit contains both geometry and Heart fixes, so retaining the whole tested code baseline is safer than selecting isolated lines. Update source/evidence references after the merge, rather than copying main's old development direction over Version 2.

Save migration needs separate schema and gameplay identity. Mid-encounter main saves are covered by the reproduced tests, but old saves cannot yet be converted into the new base/assault economy. Preserve legacy play/replay and begin the revised gameplay in a new explicitly identified profile. No automatic conversion is included in this audit.

The technical runtime blocker is resolved by using the existing Linux environment. Native Windows dependencies are still Linux-built; do not claim a Windows-native install was repaired. No outside service, plugin or dependency download was required.

## Subsequent authorised integration

The owner authorised the proposed integration and tram work (D-EX-12). Adopted docs were checkpointed in f20208a; tested origin/main b32e4c3 was merged in 0fdbb67 on codex/tram-expansion. Conflicting handoff/workflow docs retained the adopted design; main code, tests and historical evidence were kept. The comparison above records the pre-merge audit. TRAM_INTEGRATION_REPORT.md records the integrated result and validation.
