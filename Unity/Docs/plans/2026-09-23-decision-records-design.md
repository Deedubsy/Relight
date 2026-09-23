# Decision records (ADR hybrid) — design

**Date:** 2026-09-23 · **Status:** approved in conversation by the owner, awaiting their review of this written spec · **Scope:** the Unity port's documentation only.

## 1. Why

`Unity/Docs/DECISIONS.md` records good content (stable ids, provenance, port consequence) but the one-file, one-row-per-decision format has outgrown itself:

- 73 U-D rows (72 when first measured) average ~760 characters, the longest is 9,441 characters on a single line, so tables are unreadable and every edit diffs the whole row.
- 258 KB in one file: neither the owner nor an agent can read it whole; agents grep and miss context.
- Structure is drifting: recent additions were appended as loose sections with off-pattern ids (`U-D-ORE-01`, `U-D-SCENE-01`, `U-D-RAID-01`).
- Amendments are buried as in-row notes (U-D-38 carries a "Note 2026-09-23" that its cadence no longer holds).

**Success:** a reader can see every decision and its status on one screen of index, each new decision is a short file with its reasoning and rejected options, and no existing id, link or row breaks.

## 2. What the owner chose

The hybrid (owner, 2026-09-23: "I like the hybrid. Lets do that"; design sections: "That looks right"):

- `DECISIONS.md` stays the **single owner** of decisions and gains an index.
- New decisions go in one file each under `Unity/Docs/decisions/`.
- Old rows stay where they are, untouched, unrenumbered.

## 3. Layout

```
Unity/Docs/
├── DECISIONS.md              single owner of decisions
│    ├── Index (new, at the top)
│    ├── §1–§5 and later sections: existing rows, unchanged
│    ├── §3 U-P provisional values table (stays a table)
│    └── §4 U-Q owner questions (stays a table)
└── decisions/                new
     ├── README.md            when to write one, the template, the rules
     ├── U-D-<n>-<slug>.md    each new owner decision, from U-D-74
     └── U-M-<n>-<slug>.md    each new engineering/migration choice, from U-M-41
```

- **Ids are unchanged.** File names use the id as written (`U-D-74`, no zero padding) so `grep U-D-74` finds both the index line and the file. Slug: lowercase, hyphenated, ≤ 6 words.
- **Existing references keep working.** "DECISIONS.md U-D-47" still resolves because the row is still there.
- **Off-pattern sections** (`U-D-ORE-01`, `U-D-SCENE-01`, `U-D-RAID-01`, and the two dated "Owner UI direction" / "Current UI implementation authority" sections) get index lines under their current names and are not renamed.

## 4. The index

A section `## Index` inserted directly after the file's opening paragraphs, before §1. One table row per decision, U-D then U-M, in id order, then the off-pattern sections:

| Column | Content |
|---|---|
| ID | `U-D-47` |
| Title | ≤ ~12 words, taken from the row's bold opening sentence (or its first clause when there is none) |
| Status | `Accepted (owner)`, `Accepted (engineering)`, `Superseded by U-D-nn`, `Amended by U-D-nn`, `Retired`, `Proposed` |
| Date | the row's decision date where the row states one; otherwise blank rather than guessed |
| Where | `§1` / `§2` / section name for old rows; a relative link for file-based decisions |

Status for old rows is read from the row itself (e.g. U-D-10 → `Superseded by U-D-44`). Where a row does not say, the default is `Accepted (owner)` for U-D and `Accepted (engineering)` for U-M; nothing is inferred beyond what the row states.

## 5. Decision file template

Lives in `decisions/README.md` and is what `/decision` fills in.

```markdown
# U-D-74 — <title>

**Status:** Accepted (owner) · **Date:** 2026-09-23
**Provenance:** owner, verbatim: “…” | engineering (overturnable by the owner)
**Supersedes / amends:** U-D-38 (amends: cadence figures) | none
**Related:** TASKS <row ids> · Linear <REL-nn> · U-P-<nn> · evidence/<folder>

## Context
Why this came up. What was true before. 3–8 lines.

## Decision
The rule itself, in lettered clauses (a), (b)… when there is more than one.

## Options considered
- **Chosen:** … — why.
- **Rejected:** … — why.

## Consequences
What changes in the port; which U-P values it sets; what it constrains later.
```

Target length: one to two screens. Longer detail goes to an evidence or design doc linked from Related.

Statuses: `Proposed` (written, owner has not decided — only used for policy an owner must approve), `Accepted (owner)`, `Accepted (engineering)`, `Superseded by …`, `Retired`. U-D is owner policy; U-M is an engineering choice the owner can overturn — the same split as today.

## 6. Rules

1. **When a file is written:** new owner policy (U-D) or an engineering/architecture choice (U-M). Numbers remain U-P rows and open questions remain U-Q rows, in their existing tables.
2. **Every decision file has an index line**, and the index line is added in the same change as the file.
3. **No in-row amendment notes from now on.** An amendment is a new decision file with `Supersedes / amends` filled in; the old row gains one marker at the start of its decision cell — `**→ amended by [U-D-nn](decisions/…)**` (or `superseded by`) — and its index status changes. Notes already inside old rows stay as they are.
4. **Supersession is two-way:** the new file names the old id; the old row/file names the new id.
5. **Unchanged rules:** U-D-28 still delegates numbers, not policy; new policy still becomes an F1 question in REL-71 rather than being decided in passing; approvals are never invented; owner words are quoted verbatim.
6. **Next free ids:** the TASKS.md log's "Next free ids" list gains `U-M-nn` alongside U-D, U-P, E and F1 (currently U-M-41).

## 7. Workflow integration

| Where | Change |
|---|---|
| `Unity/Docs/decisions/README.md` | New: purpose, the rules in §6, the template in §5. |
| `.claude/commands/decision.md` | New `/decision` command: reads the next free id from the last TASKS.md log entry (and checks it against the index and `decisions/`), asks for or takes type (U-D/U-M), title and provenance, writes the file from the template, adds the index line, adds the `→ amended/superseded by` marker to any old row named, and reminds the session to bump "Next free ids" in its TASKS log entry. It does not commit. |
| `Unity/Docs/DECISIONS.md` | Index section added; opening paragraph says new decisions live in `decisions/` and the index lists all of them. |
| `SESSION-START.md` §4 Documents | DECISIONS line updated: index plus `decisions/`, and `/decision` to add one. |
| `Unity/README.md` | Read-order item for DECISIONS mentions the index and `decisions/`. |
| Root `CLAUDE.md` line 3 | "`Unity/Docs/DECISIONS.md` holds migration decisions" gains "(index; new decisions are files in `Unity/Docs/decisions/`, added with `/decision`)". |
| Assistant memory | The relevant memory note records the new convention. |

**Not touched:** the paused reference project (`docs/DECISIONS.md` and everything under `docs/`, `packages/`), any `.cs`/`.uxml`/`.uss`, existing evidence folders, and the content of any existing decision row except the one-line markers under rule 3 (none are added by this change).

## 8. Out of scope (deliberately)

- An automated consistency check between the index and `decisions/` (can be added later if drift appears).
- Moving existing rows out into files (the very long rows can be split later on request).
- Renumbering or renaming anything.

## 9. Verification

- Every `U-D-nn` and `U-M-nn` defined in a table row of `DECISIONS.md` appears exactly once in the index (script count comparison, run once, not committed as a tool).
- The rest of `DECISIONS.md` below the index is byte-identical to before apart from the opening-paragraph edit (checked with `git diff`).
- Every link in the index and in `decisions/README.md` resolves.
- `/decision` is dry-run once on a throwaway id to confirm it produces a correct file and index line, and the throwaway output is removed.
- No Unity or npm test run is needed: nothing executable changes.
