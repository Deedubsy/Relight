"""Read-only Phase 6 review checks; historical readiness scripts/manifests stay untouched."""
from pathlib import Path
import hashlib
import json
import re
import subprocess

root = Path(__file__).resolve().parents[3]
folder = Path(__file__).resolve().parent
def read(path):
    data = path.read_bytes()
    return data.decode('utf-16' if data.startswith((b'\xff\xfe', b'\xfe\xff')) else 'utf-8-sig')

old = json.loads(read(root / 'docs/evidence/phase5-readiness-2026-09-07/review-manifest.json'))
for name, expected in old['protected_files'].items():
    assert hashlib.sha256((root / name).read_bytes()).hexdigest() == expected, name
print(f"Preserved {len(old['protected_files'])} source/build/evidence files from the earlier readiness manifest.")

focused = read(folder / 'focused.log')
assert '# pass 25' in focused and '# fail 0' in focused and '# skipped 0' in focused
probe = [json.loads(line) for line in read(folder / 'approach-probe.log').splitlines() if line.startswith('{')]
assert probe[0]['originsChecked'] == 96 and probe[0]['allValid']
assert probe[1]['elapsedAfterDusk'] == 240 and probe[1]['spawned'] == 0 and probe[1]['remaining'] == 60
assert probe[1]['savedContinuationMatches']
print('Fresh evidence: 25 tests passed; 96 valid fresh origins; locked-origin stall reproduced, not marked fixed.')

docs = ['CLAUDE.md', 'README.md'] + ['docs/' + name + '.md' for name in
    ['PROGRAMME_STATE', 'PROGRESS', 'DECISIONS', 'PHASES', 'STANDARDS', 'EXPLORATION_DEFENCE_PLAN',
     'PHASE_5_PLAYTEST_REPORT', 'PHASE_6_SCOPE_REPORT']]
links = 0
for name in docs:
    path = root / name
    for target in re.findall(r'\]\(([^)]+)\)', read(path)):
        target = target.split('#')[0]
        if not target or '://' in target or target.startswith('mailto:'):
            continue
        assert (path.parent / target).exists(), (name, target)
        links += 1

def rows(text):
    result = {}
    for line in text.splitlines():
        if not re.match(r'^\| (EX-|P[56]-|RI-|T\d)', line):
            continue
        cells = [c.strip() for c in line.split('|')[1:-1]]
        assert len(cells) == 8, line
        assert cells[0] not in result, cells[0]
        result[cells[0]] = cells
    return result

current = rows(read(root / 'docs/PROGRESS.md'))
previous = rows(subprocess.check_output(['git', 'show', 'HEAD:docs/PROGRESS.md'], cwd=root).decode('utf-8'))
assert [name for name in current if name in previous] == list(previous), 'historical task order changed'
for name, row in previous.items():
    if name not in {'EX-08B', 'EX-08H', 'T18'}:
        assert current[name] == row, name
    elif name != 'EX-08B':
        assert current[name][3:6] == row[3:6] and current[name][7] == row[7], name
assert current['P6-00'][3] == 'done'
assert next(name for name, row in current.items() if row[3] in {'todo', 'in_progress'}) == 'P6-01'
assert current['EX-08B'][3:5] == ['blocked', 'P6-04']
for name in ['P6-02', 'P6-03', 'P6-04', 'P6-H']:
    assert current[name][3] == 'blocked' and not current[name][7]
assert len(re.findall(r'^\| D-EX-28 \|', read(root / 'docs/DECISIONS.md'), re.M)) == 1
assert subprocess.check_output(['git', 'diff', '--name-only', '--', 'packages'], cwd=root).strip() == b''
print(f'Checked {links} local links, preserved historical task order/acceptance, P6-01 next, and unchanged production code.')
