# Repository guidance

Read CLAUDE.md for the current coding-agent workflow, then docs/PROGRAMME_STATE.md and docs/PROGRESS.md. Phase 4 is complete; the active design and successor plan are docs/RELIGHT-design.md and docs/EXPLORATION_DEFENCE_PLAN.md. docs/REVISED_DEVELOPMENT_PLAN.md is historical Version 1.

Current user instructions override older repository directions. docs/DECISIONS.md records approved rules and unresolved contracts; docs/CONSTITUTION.md defines evidence and authority. PROGRESS is the only task list. Legacy references describe earlier rules, not current acceptance.

Keep gameplay in packages/sim; rendering submits ordinary commands. Reuse existing code and fetched-main city/Heart improvements after baseline reconciliation. Do not merge branches or claim tests passed without verifying. Follow the documentation and code verification policy in CLAUDE.md.

<!-- CODEGRAPH_START -->
## CodeGraph

In repositories indexed by CodeGraph (a .codegraph/ directory exists at the repo root), reach for it BEFORE grep/find or reading files when you need to understand or locate code. Prefer codegraph_explore or codegraph_node MCP tools when available; the shell equivalents are codegraph explore and codegraph node. If there is no .codegraph/ directory, skip CodeGraph entirely; indexing is the user's decision.
<!-- CODEGRAPH_END -->
