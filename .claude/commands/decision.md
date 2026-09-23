---
description: Record a new Unity-port decision (U-D owner policy or U-M engineering choice) as its own file and index it
argument-hint: "[U-D|U-M] [title]"
---

Record one decision under the rules in `Unity/Docs/decisions/README.md`. Read that file first. Arguments: $ARGUMENTS

1. **Type.** U-D (owner policy) or U-M (engineering choice). If not given, ask. If it is a number, stop: numbers are U-P rows in `Unity/Docs/DECISIONS.md` §3. If it is an open question, stop: it is a U-Q row or an F1 question in Linear REL-71. If the owner has not actually decided a U-D, stop and say so — never invent an approval.
2. **Id.** Take the highest of: the type's number in the last "Next free ids" in `Unity/Docs/TASKS.md` minus one, the highest `U-D-`/`U-M-` number in the `## Index` table of `Unity/Docs/DECISIONS.md`, the highest defined as a table row (`| U-D-nn |`) anywhere in that file, and the highest in `Unity/Docs/decisions/` file names. The new id is that plus one. If the sources disagree, say which was stale.
3. **Content.** Ask for anything missing: title, provenance (owner words verbatim, or engineering), what it supersedes or amends, context, the decision, options considered, consequences, related TASKS/Linear/U-P/evidence. Do not fill gaps with guesses — write what is known and ask about the rest.
4. **Write** `Unity/Docs/decisions/{ID}-{slug}.md` from the template between `<!-- template:start -->` and `<!-- template:end -->` in the README (without the fence), slug ≤ 6 lowercase hyphenated words.
5. **Index.** Add `| {ID} | {Title ≤ 12 words} | {Status} | {Date} | [file](decisions/{file}) |` to the `## Index` table in `Unity/Docs/DECISIONS.md`, after the last row of the same type (U-D rows before U-M rows, section rows last).
6. **Supersede / amend.** For each id named: if it is an old row, insert `**→ {amended|superseded} by [{ID}](decisions/{file})** ` at the start of its decision cell and change its index status; if it is a file, change its **Status** line. Nothing else in the old row changes.
7. **Report** the file path, the index line, any marker added, and remind: the TASKS.md log entry for this work must bump Next free ids (U-D, U-M, U-P, E, F1). Do not commit. The pre-commit check (`.githooks/check_decisions.py`) will refuse the commit if the file, its Index line or its heading disagree.
