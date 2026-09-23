"""Claude Code PreToolUse gate: a session must check for unrecorded decisions before `git commit`.

The git pre-commit check (.githooks/check_decisions.py) only sees numbered ids. This gate
covers the gap: a choice between options, or a new owner rule, that nobody numbered.
The first `git commit` is refused with the checklist below; re-running it as
`DECISION_CHECK=done git commit ...` passes once the session has checked.
"""
import json
import re
import shlex
import sys

MARKER = "DECISION_CHECK=done"
MESSAGE = """Before committing, check this change for unrecorded decisions (Unity/Docs/decisions/README.md):
- a choice between options with a reason ("X was not taken because ..."), or an engineering rule later work must follow -> U-M
- a new rule or direction from the owner -> U-D (only if the owner actually decided it; open policy is an F1 question in REL-71)
- numbers are U-P rows; open questions are U-Q rows; bug fixes that restore intended behaviour need nothing
If one is unrecorded, run /decision and stage the file, its Index line and the TASKS Next free ids bump.
Then re-run the same command with the git commit prefixed by DECISION_CHECK=done (e.g. `DECISION_CHECK=done git commit ...`)."""


def is_commit(segment):
    """True when this shell segment runs `git [global options] commit`."""
    try:
        words = shlex.split(segment, posix=True)
    except ValueError:
        words = segment.split()
    env = []
    while words and re.match(r"^[A-Za-z_][A-Za-z0-9_]*=", words[0]):
        env.append(words.pop(0))
    if not words or words[0] not in ("git", "rtk"):
        return False, env
    if words[0] == "rtk":
        words = words[1:]
        if not words or words[0] != "git":
            return False, env
    i = 1
    while i < len(words) and words[i].startswith("-"):
        i += 2 if words[i] in ("-C", "-c", "--git-dir", "--work-tree", "--namespace") else 1
    return i < len(words) and words[i] == "commit", env


def main():
    try:
        data = json.load(sys.stdin)
    except ValueError:
        return 0
    command = (data.get("tool_input") or {}).get("command") or ""
    for line in command.splitlines():
        for segment in re.split(r"&&|\|\||[;|]", line):
            commit, env = is_commit(segment.strip())
            if commit and MARKER not in env:
                print(MESSAGE, file=sys.stderr)
                return 2
    return 0


if __name__ == "__main__":
    sys.exit(main())
