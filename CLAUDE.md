# Relight — coding-agent working rules

Current direction: exploration, tram expansion and intermittent base defence, adopted 2026-09-06 (D-EX-01–09). Phase 4 is complete; Phase 5 began with EX-09A reconciliation on 2026-09-07 (D-EX-21). The EX opening/defence revision is implemented through EX-07. D-EX-20 brings Phase 5 factory implementation before the human representative-loop test; EX-09A records the reconciled scope, P5-01 construction/controls, P5-02 routing and P5-03 factory inspection/power are complete; P5-04 transport and P5-05 integration are complete; EX-09B and RI-09 technical review are complete; EX-08B refreshed gameplay-test preparation is next, with rendering concerns retained. Phase completion still requires its human evidence. Do not resume the old RI task order automatically.

## Read order

1. docs/PROGRAMME_STATE.md — checkout versus fetched-main state, evidence and next work.
2. docs/PROGRESS.md — the only task list and its dependencies.
3. docs/CONSTITUTION.md — scope, authority and verification.
4. Relevant D-EX decisions in docs/DECISIONS.md, including any open contract the task needs.
5. The active sections of docs/RELIGHT-design.md and the task definition in docs/EXPLORATION_DEFENCE_PLAN.md.

README.md and AGENTS.md point to the same documents. REVISED_DEVELOPMENT_PLAN.md is the preserved Version 1 plan, not a second executable plan. The GDD's legacy reference is retained for existing tooling and code; its old prohibitions and benchmark timings do not govern new implementation. Archives/reports are evidence, not instructions to restore superseded behaviour.

## Work and authority

Follow the user's requested execution scope. Current user instructions take precedence. For a next-task request, choose the first runnable PROGRESS row, inspect its actual dependencies, implement only authorised scope, verify, update its evidence and handoff, and report. Ask only for genuinely missing core decisions; proceed with independent work. A documentation revision is not permission to implement every discussed example.

Do not infer a completed later phase from a completed RI feature. EX-01 reconciled the baseline, and authorised integration retained newer main's city/Heart work. EX-03 separates campaign/evidence profiles; preserve both the tested baseline and the adopted design. No merge, push or destructive evidence operation without user authorisation.

The sim alone mutates gameplay through commands. Bots use player inventories and reach. Renderer-only effects cannot imply mechanics the sim does not implement. Record code, automated validation, design approval and human play separately.

## Verification

Code: npm test, npm run typecheck, npm run lint, npm run docsync:check and focused checks required by the task. Phase gates require the constitution's broader policy. Docs: docsync:check, freshness:check and referenced-path/consistency checks. Report baseline failures and environment blockers, including unrun checks, without rewriting expectations to pass.

Current documentation compatibility: the GDD keeps original generated blocks and guarded legacy prose intact. EX-03 routes campaign constants to docs/CAMPAIGN_RULES.md and campaign evidence to explicit campaign subdirectories; docsync checks both profiles by default. Do not regenerate old evidence into the new game.

## Handoff

PROGRESS owns task status and one log entry per meaningful status change. PROGRAMME_STATE states the actual branch/build and the next runnable task. DECISIONS holds new or superseded rules with accurate provenance. The final report names changes, actual checks and material limitations. Do not fabricate attribution or commit trailers; commit/push only within the user's authorised scope.
