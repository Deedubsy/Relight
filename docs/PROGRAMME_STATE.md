# Relight — programme state

Current handoff: 2026-09-06, after the authorised documentation migration and owner adoption of the recommendation package. Status authority: PROGRESS.md. Rule authority: DECISIONS.md and the active RELIGHT-design.md. Delivery: EXPLORATION_DEFENCE_PLAN.md.

## Now

- **Phase 4 complete**, confirmed directly by the owner: "we have just finished phase four" (D-EX-10). Phase 5 is the next incomplete phase. RI features built ahead of the phase sequence do not mean their phases are complete. The approximate 40% estimate is the owner's estimate, not a measured percentage of the revised scope.
- **Version 2 direction is documented, not implemented by this pass:** free roaming and useful discoveries; a house in a one-approach cul-de-sac; second-area tram access and radio restoration opportunity; station factories serving specialised districts; site creatures, minor raids and single-target major assaults; two major-assault-free days initially, potentially one later. D-EX-01–09 record the owner's direction and documentation authorisation.
- **EX-00 editorial scope complete.** The active GDD, successor plan, constitution, decision registry, phase criteria, tracker, standards, deferred mapping and coding-agent entry points agree. Historical documents and evidence remain preserved. See EXPLORATION_DEFENCE_MIGRATION_REPORT.md for exact verification and limitations.
- **EX-01 in progress:** the [recommendation package](EXPLORATION_DEFENCE_RECOMMENDATIONS.md) is now adopted by the owner (D-EX-11); Q01–06 are decided and EX-02 is complete. The approved initial cycle is 20 minutes; all six contracts are incorporated in the GDD. These rules are not implemented or play-validated. The full baseline/feature/compatibility audit remains the next independent work; relevant main save/profile and Heart changes and current tram branching limitations were inspected read-only. No gameplay code, branch merge or benchmark promotion has occurred.

## Checkout versus fetched main

The checked-out branch is ri-pass-1, HEAD 678478a. Its previous handoff says RI-01–06 code exists, with RI-06 unverified locally. The previous RI-05 record reports 148 tests and fourteen experiments; these are historical reports, not newly rerun checks.

Fetched origin/main is b32e4c3. Its handoff reports integrated city geometry/presentation and repaired Heart validation: all six Heart tests, fifteen experiments and a 156-test suite passing sequentially. City recognisability remains AI-reviewed, with cold-player and reference-machine performance work outstanding. These reports do not mean the newer implementation runs in the current checkout. The earlier pull fetched main without switching the local branch.

Read-only main references are saved under archive/pre-exploration-defence-2026-09-06/fetched-main/. EX-01 verifies ancestry, diff, compatibility and evidence, proposes the intended baseline and integration, and avoids losing that newer work. The documentation revision itself does not integrate code.

## Reusable work and limitations

Reusable foundations include the pure sim/renderer boundary, real inventories/accounting, movement, production, recipes, belts, power, lights, physical commissioning, project rewards and deterministic save/replay. Existing tram code supports a minimal two-stop route. Existing Stalker/Heart primitives can inform encounters. Main's urban structures, landmarks, yards and rail reservation should carry forward after integration.

Unbuilt revised scope includes base registration/route semantics, the new opening, day/assault schedule, radio warning progression, multi-stop reserved freight, concentrated campaign economy and the approved discovery/repair contracts. Existing continuous frontier pressure does not meet the new specification.

Historical Gate A/B decisions and observations remain valid within their original scope. The new representative-loop gate EX-08H has not run. No new gameplay balance or fun claim is made.

## Decisions and remaining specification

Q01–06 are decided through D-EX-11: cores/territory, attack clock/schedule, radio, tram network, persistent resource sources, and combat/repair/discoveries. Q07 remains open for later endgame. Unspecified costs, HP, rates, radii and detailed tuning are scoped to their implementation tasks; no unlimited tuning range was inferred. EX-01's technical baseline audit remains next, with no need to re-ask the six core choices.

## Verification and workspace notes

Documentation validation is partial: static checks are recorded in the migration report. Under Windows the npm executable shim is missing, direct tsx first hits the sandbox user lookup limitation, and an approved outside-sandbox run reveals Linux-only esbuild dependencies. docsync/freshness could not execute; this is an environment blocker, not a content pass. No gameplay tests ran for the documentation pass. Existing code, constants, generated results and snapshots were not changed.

Untracked .serena/ and docs.zip predate this work and were left intact. No commit, push or branch switch was performed. The GDD's original generated blocks remain as explicitly labelled legacy references until EX-03 makes tooling profile-aware.
