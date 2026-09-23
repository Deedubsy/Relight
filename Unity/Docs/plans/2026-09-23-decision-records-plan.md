# Decision records (ADR hybrid) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add an index to `Unity/Docs/DECISIONS.md`, a `Unity/Docs/decisions/` folder for one-file-per-decision records, a `/decision` command, and pointers in the workflow docs, without changing any existing decision row.

**Architecture:** Documentation-only. A throwaway Python script (kept in the session scratchpad, not committed) drafts the index from the existing table rows; titles over ~12 words are shortened by hand; a second throwaway script verifies coverage and that nothing below the index changed.

**Tech Stack:** Markdown, Python 3 (stdlib only, scratchpad only), a Claude Code slash command (`.claude/commands/*.md`).

**Spec:** `Unity/Docs/plans/2026-09-23-decision-records-design.md`

## Global Constraints

- `DECISIONS.md` stays the single owner of decisions; TASKS.md stays the single owner of status.
- Ids are unchanged; file names use the id as written, no zero padding: `U-D-74-<slug>.md`, `U-M-41-<slug>.md`; slug lowercase, hyphenated, ≤ 6 words.
- Existing rows are not edited, renumbered or moved. The only edit above the index is the opening paragraph.
- Index statuses: `Accepted (owner)`, `Accepted (engineering)`, `Superseded by U-D-nn`, `Amended by U-D-nn`, `Retired`, `Proposed`. For U-M rows the row's own Status column is copied verbatim instead.
- Date: taken from the row where it states one; otherwise blank, never guessed.
- Not touched: `docs/`, `packages/`, any `.cs`/`.uxml`/`.uss`, existing evidence folders, `.serena/project.yml`, `Unity/Relight/ProjectSettings/TimeManager.asset`.
- No commit or push without the owner's explicit word. When authorised, commit messages end with the `Co-Authored-By` trailer from the session reminder.
- Run every shell command with absolute paths (cwd resets); scratchpad: `/tmp/claude-1000/-mnt-e-Factorio2/02f83b76-f6a3-401e-a293-c9019f74b976/scratchpad`.

## Review Focus

1. **A row whose bold opening is missing or enormous** (U-D-01b has no bold; U-M-40's bold runs to 40+ words) — expect a short readable title, not a truncated sentence fragment or an empty cell. Pinned by Task 1 Step 4 (title length check).
2. **Ids with suffixes** (`U-D-01b`, `U-D-02b`) — expect them indexed exactly as written and counted once. Pinned by the verifier's id regex in Task 1 Step 1.
3. **A row that mentions another decision's supersession** (U-D-44's text says "This supersedes U-D-10") — expect U-D-44 to stay `Accepted (owner)` and U-D-10 to show `Superseded by U-D-44`, not the reverse. Pinned by Task 1 Step 4 spot-check.
4. **An accidental edit to the old rows** while inserting the index — expect the rest of the file byte-identical. Pinned by the verifier's tail comparison.
5. **`/decision` run when TASKS.md's "Next free ids" is stale** relative to the index or folder — expect the command to take the highest id found anywhere plus one and say so. Pinned by Task 3 Step 3 dry run.

---

### Task 1: The index in DECISIONS.md

**Files:**
- Modify: `Unity/Docs/DECISIONS.md` (opening paragraph lines 1–13; insert `## Index` before line 15)
- Scratch (not committed): `<scratchpad>/verify_index.py`, `<scratchpad>/draft_index.py`, `<scratchpad>/DECISIONS.before.md`

**Interfaces:**
- Produces: a `## Index` section with the table header `| ID | Title | Status | Date | Where |`, and anchor `#index`. Task 2's README and Task 3's command append rows to this table.

- [ ] **Step 1: Snapshot and write the verifier**

```bash
cp /mnt/e/Factorio2/Unity/Docs/DECISIONS.md /tmp/claude-1000/-mnt-e-Factorio2/02f83b76-f6a3-401e-a293-c9019f74b976/scratchpad/DECISIONS.before.md
```

`verify_index.py`:

```python
import re, sys
S = "/tmp/claude-1000/-mnt-e-Factorio2/02f83b76-f6a3-401e-a293-c9019f74b976/scratchpad"
before = open(f"{S}/DECISIONS.before.md", encoding="utf-8").read()
after = open("/mnt/e/Factorio2/Unity/Docs/DECISIONS.md", encoding="utf-8").read()
ID = r"U-[DM]-\d+[a-z]?"
defined = re.findall(rf"^\| ({ID}) \|", before, re.M)
if "## Index\n" not in after:
    sys.exit("FAIL: no ## Index section")
idx_start = after.index("## Index\n")
idx_end = after.index("\n## Current UI implementation authority", idx_start)
index = after[idx_start:idx_end]
indexed = re.findall(rf"^\| ({ID}) \|", index, re.M)
missing = [i for i in defined if i not in indexed]
dupes = sorted({i for i in indexed if indexed.count(i) > 1})
extra = [i for i in indexed if i not in defined]
# everything from "## Current UI implementation authority" down must be unchanged
anchor = "## Current UI implementation authority"
tail_ok = before[before.index(anchor):] == after[after.index(anchor):]
long_titles = [r for r in re.findall(rf"^\| ({ID}) \| ([^|]+) \|", index, re.M) if len(r[1].split()) > 14]
for sec in ["U-D-ORE-01", "U-D-SCENE-01", "U-D-RAID-01", "Owner UI direction", "Current UI implementation authority"]:
    if sec not in index: print("MISSING SECTION ROW:", sec)
print(f"defined={len(defined)} indexed={len(indexed)} missing={missing} dupes={dupes} extra={extra} tail_unchanged={tail_ok} long_titles={[t[0] for t in long_titles]}")
sys.exit(0 if not (missing or dupes or extra or long_titles) and tail_ok else 1)
```

- [ ] **Step 2: Run the verifier — expect FAIL**

Run: `python3 <scratchpad>/verify_index.py`
Expected: `FAIL: no ## Index section`

- [ ] **Step 3: Draft the index rows**

`draft_index.py` prints one Markdown row per U-D and U-M table row:

```python
import re
S = "/tmp/claude-1000/-mnt-e-Factorio2/02f83b76-f6a3-401e-a293-c9019f74b976/scratchpad"
text = open(f"{S}/DECISIONS.before.md", encoding="utf-8").read()
rows = re.findall(r"^\| (U-[DM]-\d+[a-z]?) \| (.*)$", text, re.M)
sup = {}  # old id -> new id, from phrases "supersedes U-D-nn" / "superseded by U-D-nn"
for rid, rest in rows:
    for old in re.findall(r"[Tt]his supersedes (U-D-\d+[a-z]?)", rest):
        sup[old] = rid
    m = re.search(r"superseded by (U-D-\d+[a-z]?)", rest)
    if m: sup.setdefault(rid, m.group(1))
def title(cell):
    m = re.match(r"\s*(?:~~.*?~~\s*—\s*)?\*\*(.+?)\*\*", cell)
    t = m.group(1) if m else re.split(r"(?<=\.)\s", cell.strip(), 1)[0]
    t = re.sub(r"[*`]", "", t).rstrip(".")
    return t
for rid, rest in rows:
    cells = [c.strip() for c in rest.rstrip("|").split(" | ")]
    t = title(cells[0])
    if rid.startswith("U-M"):
        status, where = cells[-1] if len(cells) >= 3 else "", "§2"
    else:
        status = f"Superseded by {sup[rid]}" if rid in sup else ("Retired" if "Retired" in cells[0][:200] else "Accepted (owner)")
        where = "§1"
    dates = re.findall(r"2026-\d\d-\d\d", " ".join(cells[1:2]) if rid.startswith("U-D") else cells[0])
    print(f"| {rid} | {t} | {status} | {dates[0] if dates else ''} | {where} |")
```

Run: `python3 <scratchpad>/draft_index.py > <scratchpad>/index_rows.md; wc -l <scratchpad>/index_rows.md`
Expected: 114 lines (74 U-D + 40 U-M).

- [ ] **Step 4: Edit the draft by hand**

Open `index_rows.md` and fix, reading each source row where needed:
- Any title over 12 words: rewrite to ≤ 12 words keeping the row's own vocabulary (e.g. U-M-40 → `Correction pass: full riverfront map, saves and presentation`).
- Rows with no bold opening (U-D-01b etc.): short title from the first sentence.
- Spot-check statuses: U-D-10 = `Superseded by U-D-44`; U-D-44 = `Accepted (owner)`; U-D-23 gains nothing (its 2026-09-20 note is a note, not a supersession — leave `Accepted (owner)`). Any other row whose text contains "superseded" must be read and its status set only from what the row states.
- U-M Status column is copied verbatim (e.g. `Proposed`, `Confirmed …`); if a U-M status cell is longer than 6 words, keep its first clause.

Append these five section rows after the U-M rows (Where column links to the heading anchor):

```markdown
| Current UI implementation authority | Owner authorised the 2026-09-14 UI/UX pass | Accepted (owner) | 2026-09-14 | [section](#current-ui-implementation-authority--2026-09-14) |
| Owner UI direction | 2026-09-14 follow-up UI direction | Accepted (owner) | 2026-09-14 | [section](#owner-ui-direction--2026-09-14-follow-up) |
| U-D-ORE-01 | Owner-approved ore opening | Accepted (owner) | 2026-09-15 | [section](#u-d-ore-01--owner-approved-ore-opening-2026-09-15) |
| U-D-SCENE-01 | Scene-authored Unity world | Accepted (owner) | 2026-09-15 | [section](#u-d-scene-01--scene-authored-unity-world-owner-2026-09-15) |
| U-D-RAID-01 | Raid field reach and origin guard | Accepted (engineering) | 2026-09-19 | [section](#u-d-raid-01--raid-field-reach-and-origin-guard-engineering-2026-09-19-owner-authorised-the-fix-with-do-it-use-gizmos-where-you-can) |
```

Read lines 254–269 of DECISIONS.md first and correct the titles above if the sections say otherwise.

- [ ] **Step 5: Insert the index and edit the opening paragraph**

Insert before the line `## Current UI implementation authority — 2026-09-14`:

```markdown
## Index

Every decision, one line each. Rows U-D-01b–U-D-73 and U-M-01–U-M-40 live in the sections below and are not moved. **From U-D-74 and U-M-41 on, each decision is its own file in [decisions/](decisions/README.md)**, added with `/decision`, and gets a line here in the same change. Status is read from the row itself; a blank date means the row states none.

| ID | Title | Status | Date | Where |
|---|---|---|---|---|
<the 119 edited rows>

---

```

Then in the opening list (line 5, the "Confirmed owner decisions" bullet) append one sentence: ` New decisions are one file each in [decisions/](decisions/README.md); the [Index](#index) lists all of them.`

- [ ] **Step 6: Run the verifier — expect PASS**

Run: `python3 <scratchpad>/verify_index.py`
Expected: `defined=114 indexed=114 missing=[] dupes=[] extra=[] tail_unchanged=True long_titles=[]`, no `MISSING SECTION ROW`, exit 0.
Also: `git -C /mnt/e/Factorio2 diff --stat -- Unity/Docs/DECISIONS.md` shows only insertions plus the one changed opening line.

- [ ] **Step 7: Owner view** — nothing to commit yet (commit is Task 4, authorised only).

---

### Task 2: `decisions/README.md` — rules and template

**Files:**
- Create: `Unity/Docs/decisions/README.md`

**Interfaces:**
- Consumes: the Index table header from Task 1.
- Produces: the template block (between the markers `<!-- template:start -->` and `<!-- template:end -->`) that Task 3's command copies verbatim.

- [ ] **Step 1: Write the file**

````markdown
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
````

- [ ] **Step 2: Check links resolve**

```bash
cd /mnt/e/Factorio2/Unity/Docs/decisions && ls ../DECISIONS.md ../plans/2026-09-23-decision-records-design.md && grep -c '^## Index$' ../DECISIONS.md
```
Expected: both paths listed, count `1`.

---

### Task 3: The `/decision` command

**Files:**
- Create: `.claude/commands/decision.md`

**Interfaces:**
- Consumes: Task 1's index table, Task 2's template markers.
- Produces: `/decision [U-D|U-M] [title]`.

- [ ] **Step 1: Write the command**

```markdown
---
description: Record a new Unity-port decision (U-D owner policy or U-M engineering choice) as its own file and index it
argument-hint: "[U-D|U-M] [title]"
---

Record one decision under the rules in `Unity/Docs/decisions/README.md`. Read that file first. Arguments: $ARGUMENTS

1. **Type.** U-D (owner policy) or U-M (engineering choice). If not given, ask. If it is a number, stop: numbers are U-P rows in `Unity/Docs/DECISIONS.md` §3. If it is an open question, stop: it is a U-Q row or an F1 question in Linear REL-71. If the owner has not actually decided a U-D, stop and say so — never invent an approval.
2. **Id.** Take the highest of: the type's number in the last "Next free ids" in `Unity/Docs/TASKS.md` minus one, the highest `U-D-`/`U-M-` number in the `## Index` table of `Unity/Docs/DECISIONS.md`, and the highest in `Unity/Docs/decisions/` file names. The new id is that plus one. If the three sources disagree, say which was stale.
3. **Content.** Ask for anything missing: title, provenance (owner words verbatim, or engineering), what it supersedes or amends, context, the decision, options considered, consequences, related TASKS/Linear/U-P/evidence. Do not fill gaps with guesses — write what is known and ask about the rest.
4. **Write** `Unity/Docs/decisions/{ID}-{slug}.md` from the template between `<!-- template:start -->` and `<!-- template:end -->` in the README (without the fence), slug ≤ 6 lowercase hyphenated words.
5. **Index.** Add `| {ID} | {Title ≤ 12 words} | {Status} | {Date} | [file](decisions/{file}) |` to the `## Index` table in `Unity/Docs/DECISIONS.md`, after the last row of the same type (U-D rows before U-M rows, section rows last).
6. **Supersede / amend.** For each id named: if it is an old row, insert `**→ {amended|superseded} by [{ID}](decisions/{file})** ` at the start of its decision cell and change its index status; if it is a file, change its **Status** line. Nothing else in the old row changes.
7. **Report** the file path, the index line, any marker added, and remind: the TASKS.md log entry for this work must bump Next free ids (U-D, U-M, U-P, E, F1). Do not commit.
```

- [ ] **Step 2: Confirm the command is picked up**

Run: `ls /mnt/e/Factorio2/.claude/commands/` — expect `decision.md  next.md`.

- [ ] **Step 3: Dry run (throwaway), then remove it**

Follow the command's steps by hand for `U-M "Dry run placeholder"` with provenance "engineering", context "dry run". Expected: id `U-M-41` (TASKS list lacks U-M today, index max is U-M-40, folder empty → U-M-41; the report notes TASKS carried no U-M entry). Confirm the file renders from the template and the index line lands after U-M-40. Then remove both:

```bash
rm /mnt/e/Factorio2/Unity/Docs/decisions/U-M-41-dry-run-placeholder.md
```
and delete the `| U-M-41 |` index line; re-run `verify_index.py` — expect PASS as in Task 1 Step 6.

---

### Task 4: Workflow pointers, TASKS log, memory, commit

**Files:**
- Modify: `SESSION-START.md` (§4 "Documents", the `Unity/Docs/DECISIONS.md` bullet)
- Modify: `Unity/README.md` (line 27, read-order item 1)
- Modify: `CLAUDE.md` (line 3)
- Modify: `Unity/Docs/TASKS.md` (append one log entry at the end, no blank line before it)
- Modify: `/home/deedub/.claude/projects/-mnt-e-Factorio2/memory/relight-session-start.md` (one line)

- [ ] **Step 1: SESSION-START.md** — replace the bullet

`- \`Unity/Docs/DECISIONS.md\` — decisions (U-D-nn) and the parameter table (U-P-nn).`

with

`- \`Unity/Docs/DECISIONS.md\` — the single owner of decisions: an **Index** of every decision at the top, old rows (to U-D-73 / U-M-40) below, the parameter table (U-P-nn) and questions (U-Q-nn). From U-D-74 / U-M-41 each decision is its own file in \`Unity/Docs/decisions/\`; add one with \`/decision\`. No amendment notes inside old rows — see \`decisions/README.md\`.`

(Read the file first; match the bullet's exact current text.)

- [ ] **Step 2: Unity/README.md line 27** — after `[Docs/DECISIONS.md](Docs/DECISIONS.md) — ` insert `start at its Index; new decisions are files in [Docs/decisions/](Docs/decisions/README.md); ` leaving the rest of the line unchanged.

- [ ] **Step 3: CLAUDE.md line 3** — replace `\`Unity/Docs/DECISIONS.md\` holds migration decisions and open questions` with `\`Unity/Docs/DECISIONS.md\` holds migration decisions and open questions (Index at the top; from U-D-74/U-M-41 each decision is a file in \`Unity/Docs/decisions/\`, added with \`/decision\`)`.

- [ ] **Step 4: TASKS.md log entry** — append as the last line:

`- **2026-09-23 — Decision records (ADR hybrid).** Owner, verbatim: *“I want to setup an ADR for this project and have it worked into the workflow”*, then *“I like the hybrid. Lets do that”* and *“Approved. Proceed”*. \`DECISIONS.md\` gains an Index of all 114 table decisions plus five named sections; no existing row changed. New decisions are one file each in \`decisions/\` ([rules and template](decisions/README.md)), added with \`/decision\`; no more amendment notes inside old rows. Documentation only; nothing executable changed, no Unity run. [Spec](plans/2026-09-23-decision-records-design.md) · [plan](plans/2026-09-23-decision-records-plan.md). Next free ids: <copy the latest TASKS list, adding U-M-41>.`

Check: `tail -2 /mnt/e/Factorio2/Unity/Docs/TASKS.md | grep -c '^$'` → `0`.

- [ ] **Step 5: Memory** — in `relight-session-start.md` add: `Decisions: DECISIONS.md Index + one file per decision in Unity/Docs/decisions/ from U-D-74/U-M-41 (2026-09-23), via /decision; no in-row amendment notes.` Update the matching line in `MEMORY.md` only if its hook needs it.

- [ ] **Step 6: Final verification**

```bash
python3 <scratchpad>/verify_index.py
git -C /mnt/e/Factorio2 status --short
```
Expected: verifier PASS; status shows only `CLAUDE.md`, `SESSION-START.md` (untracked already), `Unity/README.md`, `Unity/Docs/DECISIONS.md`, `Unity/Docs/TASKS.md`, new `Unity/Docs/decisions/`, `Unity/Docs/plans/2026-09-23-decision-records-*.md`, `.claude/commands/decision.md`, plus the pre-existing `.serena/project.yml`, `TimeManager.asset` and four `.meta` files, which are not touched.

- [ ] **Step 7: Commit — only if the owner authorises it**

```bash
cd /mnt/e/Factorio2 && git add CLAUDE.md Unity/README.md Unity/Docs/DECISIONS.md Unity/Docs/TASKS.md Unity/Docs/decisions Unity/Docs/plans/2026-09-23-decision-records-design.md Unity/Docs/plans/2026-09-23-decision-records-plan.md .claude/commands/decision.md
git commit -m "Decisions get an index and a file each (ADR hybrid)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```
`SESSION-START.md` is untracked and outside this change's history; add it only if the owner says to.
