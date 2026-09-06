# Tram integration — first working increment

Date: 2026-09-06. Scope: EX-01 integration and EX-06A, authorised by the owner in D-EX-12. Phase 4 remains the last complete phase. This report separates implementation, automated evidence and human play.

## Integrated baseline

The local branch is `codex/tram-expansion`. Adopted documentation was checkpointed in `f20208a`; tested main `b32e4c3` was merged in `0fdbb67`. Main's urban geometry, collision/placement masks, inventory accounting and Heart/project fixes are retained. Conflicting workflow/handoff documents preserve the new design. No push was made. [BASELINE_INTEGRATION_REPORT.md](BASELINE_INTEGRATION_REPORT.md) contains the pre-merge comparison and reproduced checks.

## Implemented freight

- Multiple stops on the existing unbranched line. Per-item request targets, explicit export enablement and retained platform stock. Destination capacity and in-flight reservations across all trams prevent duplicate dispatch.
- Destination-reserved cargo: an intermediate stop cannot unload a later stop's shipment. Return exports travel upstream using the same mechanism. Cargo stays in its existing inventory for conservation; the manifest owns no duplicate items.
- Full or unpowered destinations retain undelivered cargo and send it back to its origin. Full/unpowered origins retain it for a later attempt. Existing four-second dwell stays bounded. A disconnected existing destination parks a loaded tram until reconnection. A removed endpoint permits return or ordinary pickup recovery without cargo deletion.
- An E-accessed station form submits ordinary logged, reach-checked commands; it shows request, export and reserve settings plus incoming reserved totals. Platform and arrivals amounts are separate, and Take availability uses arrivals. Numeric controls retain arrow keys; clicking the world returns keyboard focus to the canvas.
- Applying the first station configuration upgrades the state/save envelope to schema 2. New readers accept schema 1 and 2; old readers reject schema 2 rather than ignore cargo ownership. Invalid rules/manifests and mismatched envelope/state versions are rejected. Untouched legacy sessions stay schema 1. This schema version is not the full Version 2 gameplay profile.

## Technical defaults and limits

No campaign economy or attack constants changed. Existing 200-item platform/arrivals/tram capacities and four-second dwell are reused. Request targets count platform plus arrivals plus inbound reservations. Export reserves apply to platform stock; arrivals feed local consumers and must be physically moved back to a platform to export again. Stable route order and item-catalogue order allocate scarce supply; configurable priorities and balancing are not implemented.

Unconfigured routes retain legacy unrestricted transfers. Configuring a stop opts its route into selective loading: source and receiver need explicit rules. Existing unreserved cargo can still unload while transitioning. There is no automatic campaign migration or automatic initial station configuration.

Loaded trams park when a reservation's existing recipient is disconnected; an empty tram can still traverse the remaining connected segment. Junctions retain the existing park behaviour. Factories, specialised extraction and workshop repairs are outside this increment.

## Verification

Runtime: existing WSL Ubuntu-24.04, Node 22.18.0, Linux dependencies. Windows-native dependencies were not reinstalled. Check logs and machine-readable result files are under [evidence/tram-integration-2026-09-06](evidence/tram-integration-2026-09-06).

| Check | Actual result |
|---|---|
| Focused freight/project tests | 8 passed: selective three-stop deliveries, export reserves, return freight, duplicate-order prevention, shared capacity, interruption recovery, save validation and a placed physical route |
| Full simulation suite | 161 passed, 0 failed; includes old Heart, city, ledger and replay coverage |
| Typecheck and production build | Passed across sim, harness, tools and game; Vite retains its existing large-bundle advisory |
| Lint | Passed |
| Legacy snapshot | Matched the committed three-hour compact scenario; no expected data regenerated |
| Documentation generated blocks | Passed; original legacy tables retained; GDD pinned to LF on checkout |
| Freshness and active references | Passed after temporary review-fixture cleanup; active local references and task rows checked |
| Browser interaction | Local injected review scenario: opened station with E; changed coal request from 30 to 35, ArrowUp to 36; applied and reopened to verify 36 persisted; resumed simulation and observed 36 reserved incoming items, then all 36 in the arrivals buffer with Take enabled. Canvas keyboard focus correction verified after restarting the WSL dev server. |

The first full-suite run had 160 passing tests and one failure because the revised unsupported-version message changed a legacy-tested prefix. The prefix was restored; the subsequent complete run passed. The initial log remains `test-initial.log`; no expectations were weakened. The initial placed-route test exposed truncated-route shuttling after a track break; loaded trams now park until repair and that scenario passes.

The temporary scenario was removed after review; its unstamped file caused an initial freshness failure, retained in `freshness-review-fixture.log`. After cleanup, freshness passed without modifying any historical generated artifact.

The browser scenario injects stock and a prepared route from the focused test setup; it is a controls check, not a progression, balance or human play result. Full campaign experiments, reference-machine performance and the representative-loop human session were not run for this bounded freight increment.

## Handoff

EX-06A implements the reusable freight part of EX-06. EX-03 remains in progress for explicit campaign/evidence profiles and new-session semantics before replacing frontier pressure or introducing new campaign constants. EX-04 then supplies the cul-de-sac and second-area restoration. Base registration, the major/minor assault scheduler, radio, regional persistent resources, workshop repairs and discoveries remain unbuilt. Owner design approval is recorded; no human gameplay acceptance is inferred.
