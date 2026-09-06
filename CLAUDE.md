# Relight — coding-agent working rules

Current direction: exploration, tram expansion and intermittent base defence, adopted 2026-09-06 (D-EX-01–09). Phase 4 is complete; Phase 5 is incomplete, with selected later features brought forward. Do not resume the old RI task order automatically.

## Read order

1. docs/PROGRAMME_STATE.md — checkout versus fetched-main state, evidence and next work.
2. docs/PROGRESS.md — the only task list and its dependencies.
3. docs/CONSTITUTION.md — scope, authority and verification.
4. Relevant D-EX decisions in docs/DECISIONS.md, including any open contract the task needs.
5. The active sections of docs/RELIGHT-design.md and the task definition in docs/EXPLORATION_DEFENCE_PLAN.md.

README.md and AGENTS.md point to the same documents. REVISED_DEVELOPMENT_PLAN.md is the preserved Version 1 plan, not a second executable plan. The GDD's legacy reference is retained for existing tooling and code; its old prohibitions and benchmark timings do not govern new implementation. Archives/reports are evidence, not instructions to restore superseded behaviour.

## Work and authority

Follow the user's requested execution scope. Current user instructions take precedence. For a next-task request, choose the first runnable PROGRESS row, inspect its actual dependencies, implement only authorised scope, verify, update its evidence and handoff, and report. Ask only for genuinely missing core decisions; proceed with independent work. A documentation revision is not permission to implement every discussed example.

Do not infer a completed later phase from a completed RI feature. This checkout differs from fetched main; EX-01 reconciles the baseline and proposes integration. Do not overwrite newer city/Heart work or claim it runs locally without checking. No merge, push or destructive evidence operation without user authorisation.

The sim alone mutates gameplay through commands. Bots use player inventories and reach. Renderer-only effects cannot imply mechanics the sim does not implement. Record code, automated validation, design approval and human play separately.

## Verification

Code: npm test, npm run typecheck, npm run lint, npm run docsync:check and focused checks required by the task. Phase gates require the constitution's broader policy. Docs: docsync:check, freshness:check and referenced-path/consistency checks. Report baseline failures and environment blockers, including unrun checks, without rewriting expectations to pass.

Current documentation compatibility: the GDD keeps original generated blocks and guarded legacy prose intact. EX-03 makes evidence/document tooling profile-aware before new constants are introduced. Do not regenerate old evidence into the new game.

## Handoff

PROGRESS owns task status and one log entry per meaningful status change. PROGRAMME_STATE states the actual branch/build and the next runnable task. DECISIONS holds new or superseded rules with accurate provenance. The final report names changes, actual checks and material limitations. Do not fabricate attribution or commit trailers; commit/push only within the user's authorised scope.
