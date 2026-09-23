# Decision records

One file per decision, from **U-D-74** (owner policy) and **U-M-41** (engineering choice) on. [DECISIONS.md](../DECISIONS.md) stays the single owner of decisions: its [Index](../DECISIONS.md#index) lists every decision, old and new, and older ones (U-D-01b–U-D-73, U-M-01–U-M-40) stay as rows there. Add one with `/decision`. Design: [2026-09-23 spec](../plans/2026-09-23-decision-records-design.md).

## When to write one

- **U-D** — new policy from the owner. Owner words are quoted verbatim; approvals are never invented. New policy an owner has not decided becomes an F1 question in Linear REL-71, not a decision.
- **U-M** — an engineering or architecture choice about how the port is built. The owner can overturn it.
- **Not here:** numbers stay U-P rows in DECISIONS.md §3 (U-D-28 delegates numbers, not policy); open questions stay U-Q rows in §4.

## Rules

1. File name: the id as written plus a slug of ≤ 6 lowercase hyphenated words — `U-D-74-gate-light-passes-when-open.md`.
2. The index line is added in the same change as the file.
3. No amendment notes inside old rows. An amendment is a new file with **Supersedes / amends** filled in; the old row gets one marker at the start of its decision cell — `**→ amended by [U-D-nn](decisions/U-D-nn-slug.md)**` or `superseded by` — and its index status changes. Existing in-row notes stay as they are.
4. Supersession is two-way: the new file names the old id and the old row or file names the new id.
5. Keep it to one or two screens. Longer detail goes to an evidence or design doc linked from **Related**.
6. The TASKS.md log entry for the change bumps **Next free ids**, which now includes `U-M-nn`.

## The commit check

`.githooks/pre-commit` runs `.githooks/check_decisions.py` on every commit and blocks it when:

- a file in this folder has a bad name, a heading that does not match its id, or not exactly one Index line linking it;
- an Index line names a new id or a `decisions/` file that does not exist;
- DECISIONS.md gains a new id (U-D-74+ / U-M-41+) as a table row instead of a file;
- any added line in a text or code file mentions a new id that has no file, except the current **Next free ids** and lines about this folder.

It cannot notice a decision nobody numbered; the Claude gate below covers that. Enable it once per clone with `git config core.hooksPath .githooks`; bypass only in an emergency with `git commit --no-verify`.

## The Claude gate

`.claude/settings.json` runs `.claude/hooks/decision_gate.py` before every Bash command a Claude Code session runs. It refuses a `git commit` once, listing what counts as an unrecorded decision (a choice between options with a reason, an engineering rule, a new owner rule). The session checks, runs `/decision` if needed, then re-runs the commit as `DECISION_CHECK=done git commit ...`. It applies to Claude Code sessions only, from their next start, and relies on the session's judgement; the git check above stays the deterministic one.

Statuses: `Proposed` · `Accepted (owner)` · `Accepted (engineering)` · `Superseded by U-D-nn` · `Amended by U-D-nn` · `Retired`.

## Template

<!-- template:start -->
```markdown
# {ID} — {Title}

**Status:** {Status} · **Date:** {YYYY-MM-DD}
**Provenance:** {owner, verbatim: “…” | engineering (overturnable by the owner)}
**Supersedes / amends:** {U-D-nn (what) | none}
**Related:** {TASKS rows · Linear REL-nn · U-P-nn · evidence/folder}

## Context
{Why this came up and what was true before. 3–8 lines.}

## Decision
{The rule, in lettered clauses (a), (b)… when there is more than one.}

## Options considered
- **Chosen:** {option} — {why}.
- **Rejected:** {option} — {why}.

## Consequences
{What changes in the port, which U-P values it sets, what it constrains later.}
```
<!-- template:end -->
