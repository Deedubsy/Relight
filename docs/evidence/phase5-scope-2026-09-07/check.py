"""Validate the Phase 5 audit's local references, task mapping and unchanged source."""
from pathlib import Path
import hashlib
import json
import re

root = Path(__file__).resolve().parents[3]
names = ['CLAUDE.md', 'README.md'] + ['docs/' + n + '.md' for n in [
    'DECISIONS', 'PROGRESS', 'PROGRAMME_STATE', 'PHASES', 'RELIGHT-design',
    'EXPLORATION_DEFENCE_PLAN', 'PHASE_5_SCOPE_REPORT', 'STANDARDS']]
count = 0
for name in names:
    p = root / name
    for link in re.findall(r'\]\(([^)]+)\)', p.read_text(encoding='utf-8')):
        target = link.split('#')[0]
        if not target or '://' in target or target.startswith('mailto:'):
            continue
        assert (p.parent / target).exists(), (name, target)
        count += 1

text = (root / 'docs/PROGRESS.md').read_text(encoding='utf-8')
rows = [list(map(str.strip, line.split('|')[1:-1])) for line in text.splitlines()
        if re.match(r'^\| (EX-|P5-|RI-|T\d)', line)]
assert len({r[0] for r in rows}) == len(rows), 'duplicate task IDs'
tasks = {r[0]: r for r in rows}
assert all(len(r) == 8 for r in rows), 'malformed task columns'
assert tasks['EX-09A'][3] == 'done'
assert tasks['P5-01'][3] == 'todo' and tasks['P5-01'][4] == 'EX-09A'
assert next(r[0] for r in rows if r[3] in ('todo', 'in_progress')) == 'P5-01'
for i in range(2, 6):
    assert tasks[f'P5-0{i}'][3] == 'blocked'
    assert f'P5-0{i-1}' in tasks[f'P5-0{i}'][4]
for i in range(13, 18):
    assert tasks[f'T{i}'][4] == f'P5-0{i-12}'
assert tasks['T18'][3:5] == ['blocked', 'EX-08B']
assert tasks['EX-08H'][3:5] == ['blocked', 'EX-08B']
assert tasks['T18'][7] == tasks['EX-08H'][7] == ''
for key in ['P5-01', 'P5-02', 'P5-03', 'P5-04', 'P5-05']:
    assert 'EX-08' not in tasks[key][4] and 'Q07' not in tasks[key][4]

decisions = (root / 'docs/DECISIONS.md').read_text(encoding='utf-8')
ids = re.findall(r'^\| (D-EX-[^ |]+) \|', decisions, re.M)
assert len(ids) == len(set(ids)), 'duplicate current decision IDs'
assert re.search(r'^\| D-EX-Q08 \|.*\| decided \|', decisions, re.M)
assert re.search(r'^\| D-EX-Q07 \|.*\| open \|', decisions, re.M)

frozen = root / 'docs/evidence/representative-loop-2026-09-07'
manifest = json.loads((frozen / 'build-manifest.json').read_text(encoding='utf-8'))
print(f'{count} local references resolve; unique task/decision IDs; P5-01 is next; human gates remain blocked; Q08 decided and Q07 open.')
for name, digest in manifest['source_files'].items():
    assert hashlib.sha256((root / name).read_bytes()).hexdigest() == digest, name
for name, digest in manifest['archives'].items():
    assert hashlib.sha256((frozen / name).read_bytes()).hexdigest() == digest, name
print(f"{len(manifest['source_files'])} source/build inputs and both frozen archives are unchanged.")
