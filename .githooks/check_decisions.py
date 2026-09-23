"""Pre-commit check for the decision records (Unity/Docs/decisions/README.md).

From U-D-74 and U-M-41 every decision is one file in Unity/Docs/decisions/
with one line in the DECISIONS.md Index. Checks the staged tree and the
staged additions; blocks the commit on any mismatch. Bypass in an emergency
with `git commit --no-verify`.
"""
import re
import subprocess
import sys

FIRST = {"D": 74, "M": 41}  # first file-based id in each series
DECISIONS = "Unity/Docs/DECISIONS.md"
TASKS = "Unity/Docs/TASKS.md"
FOLDER = "Unity/Docs/decisions/"
# Files that describe the system itself and name future ids as examples.
EXEMPT = ("Unity/Docs/plans/", FOLDER + "README.md", ".claude/commands/decision.md", ".githooks/")
SCANNED = ["*.md", "*.cs", "*.txt", "*.uxml", "*.uss", "*.ts", "*.js", "*.mjs", "*.py", "*.json"]

ID = re.compile(r"\bU-([DM])-(\d+)\b")
FILE_NAME = re.compile(r"^U-([DM])-([1-9]\d*)-[a-z0-9]+(?:-[a-z0-9]+)*\.md$")
NEXT_FREE = re.compile(r"[Nn]ext free ids?\b[^.\n]*")
LINK = re.compile(r"decisions/(U-[^)\s\]]+?\.md)")


def git(*args):
    out = subprocess.run(["git", *args], capture_output=True, check=True).stdout
    return out.decode("utf-8", errors="replace")


def is_new(series, number):
    return int(number) >= FIRST[series]


def next_free():
    """The ids in the last "Next free ids" list in TASKS.md: not yet allocated, so free to mention."""
    try:
        text = git("show", f":{TASKS}")
    except subprocess.CalledProcessError:
        return set()
    lists = re.findall(r"[Nn]ext free ids?\b([^.\n]*)", text)
    return {f"U-{s}-{n}" for s, n in ID.findall(lists[-1])} if lists else set()


def main():
    if not git("ls-files", "--", DECISIONS).strip():
        return 0
    errors = []

    files = {}  # "U-D-74" -> file name
    for path in git("ls-files", "--", FOLDER).splitlines():
        name = path[len(FOLDER):]
        if "/" in name or name == "README.md":
            continue
        m = FILE_NAME.match(name)
        if not m:
            errors.append(f"{path}: name must be U-D-<n>-<slug>.md or U-M-<n>-<slug>.md (no zero padding, lowercase slug)")
            continue
        key = f"U-{m.group(1)}-{m.group(2)}"
        if key in files:
            errors.append(f"{path}: {key} already has a file ({files[key]})")
            continue
        files[key] = name
        heading = git("show", f":{path}").splitlines()[:1]
        hm = ID.search(heading[0]) if heading and heading[0].startswith("# ") else None
        if not hm or f"U-{hm.group(1)}-{hm.group(2)}" != key:
            errors.append(f"{path}: first line must be '# {key} — <title>'")

    text = git("show", f":{DECISIONS}")
    index, section = {}, None
    for line in text.splitlines():
        if line.startswith("## "):
            section = "index" if line.strip() == "## Index" else "other"
        m = re.match(r"^\| (U-([DM])-(\w+)) \|", line)
        if not m:
            continue
        if section == "index":
            index.setdefault(m.group(1), []).append(line)
        elif m.group(3).isdigit() and is_new(m.group(2), m.group(3)):
            errors.append(f"{DECISIONS}: {m.group(1)} is written as a table row; from U-D-74 / U-M-41 a decision is a file (use /decision)")

    for key, name in files.items():
        rows = index.get(key, [])
        if len(rows) != 1:
            errors.append(f"{FOLDER}{name}: needs exactly one Index line in {DECISIONS}, found {len(rows)}")
        elif f"(decisions/{name})" not in rows[0]:
            errors.append(f"{DECISIONS}: the Index line for {key} must link (decisions/{name})")
    for key, rows in index.items():
        m = ID.fullmatch(key)
        if m and is_new(*m.groups()) and key not in files:
            errors.append(f"{DECISIONS}: the Index lists {key}, which has no file in {FOLDER}")
        for row in rows:
            for target in LINK.findall(row):
                if target not in files.values():
                    errors.append(f"{DECISIONS}: the Index line for {key} links decisions/{target}, which is not staged")

    free = next_free()
    diff = git("diff", "--cached", "-U0", "--no-color", "--diff-filter=ACMR", "--", *SCANNED)
    path = None
    for line in diff.splitlines():
        if line.startswith("+++ "):
            path = line[6:] if line.startswith("+++ b/") else None
            continue
        if not line.startswith("+") or path is None or path.startswith(EXEMPT):
            continue
        added = NEXT_FREE.sub("", line[1:])
        for target in LINK.findall(added):
            if target not in files.values():
                errors.append(f"{path}: links decisions/{target}, which does not exist")
        if "decisions/" in added:
            continue  # a line about the decisions folder itself
        for series, number in ID.findall(added):
            key = f"U-{series}-{number}"
            if is_new(series, number) and key not in files and key not in free:
                errors.append(f"{path}: mentions {key}, which has no file in {FOLDER} (use /decision)")

    if errors:
        print("Decision records check failed:", file=sys.stderr)
        for e in dict.fromkeys(errors):
            print("  - " + e, file=sys.stderr)
        print("See Unity/Docs/decisions/README.md. Bypass only in an emergency: git commit --no-verify", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    for stream in (sys.stdout, sys.stderr):
        try:
            stream.reconfigure(encoding="utf-8", errors="replace")
        except AttributeError:
            pass
    sys.exit(main())
