"""Verify P5-03 references, handoff, tested source/build hashes and preserved EX-08A archives."""
from pathlib import Path
from datetime import datetime, timezone
import hashlib
import json
import re
import sys

root = Path(__file__).resolve().parents[3]
out = Path(__file__).resolve().parent
frozen = root / 'docs/evidence/representative-loop-2026-09-07'
old = json.loads((frozen / 'build-manifest.json').read_text(encoding='utf-8'))
digest = lambda p: hashlib.sha256(p.read_bytes()).hexdigest()
for name, expected in old['archives'].items():
    assert digest(frozen / name) == expected, name
print('EX-08A frozen source and build archives still match their original SHA-256 hashes.')

if '--record' in sys.argv:
    files = set(old['source_files'])
    for package in ['sim', 'harness', 'tools', 'game']:
        for folder in ['src', 'test']:
            files.update(p.relative_to(root).as_posix() for p in (root / 'packages' / package / folder).rglob('*') if p.is_file())
    data = {
        'parent_commit': old['source_commit'], 'branch': 'codex/tram-expansion',
        'status': 'P5-03 uncommitted working-tree verification; parent commit is not the tested source identity',
        'recorded_at': datetime.now(timezone.utc).isoformat(),
        'runtime': 'WSL Ubuntu-24.04, Node 22.18.0; browser automation uses installed Windows Chrome via Playwright',
        'source_files': {n: digest(root / n) for n in sorted(files)},
        'build_files': {p.relative_to(root).as_posix(): digest(p) for p in sorted((root / 'packages/game/dist').rglob('*')) if p.is_file()},
        'preserved_ex08a_archives': old['archives'],
        'verification_files': {p.name: digest(p) for p in sorted(out.iterdir()) if p.suffix in ['.log', '.json', '.png', '.cjs', '.sh', '.py'] and p.name not in ['build-manifest.json', 'consistency.log']},
    }
    (out / 'build-manifest.json').write_text(json.dumps(data, indent=2) + '\n', encoding='utf-8')

manifest = json.loads((out / 'build-manifest.json').read_text(encoding='utf-8'))
for group in ['source_files', 'build_files']:
    for name, expected in manifest[group].items():
        assert digest(root / name) == expected, name
for name, expected in manifest['verification_files'].items():
    assert digest(out / name) == expected, name
print(f"Verified {len(manifest['source_files'])} source/test/build inputs, {len(manifest['build_files'])} build files, and {len(manifest['verification_files'])} evidence files.")

names = ['CLAUDE.md', 'README.md'] + ['docs/' + n + '.md' for n in ['DECISIONS', 'PROGRESS', 'PROGRAMME_STATE', 'PHASES', 'RELIGHT-design', 'EXPLORATION_DEFENCE_PLAN', 'PHASE_5_SCOPE_REPORT', 'P5_03_INSPECTION_REPORT', 'STANDARDS']]
count = 0
for name in names:
    p = root / name
    for link in re.findall(r'\]\(([^)]+)\)', p.read_text(encoding='utf-8')):
        target = link.split('#')[0]
        if not target or '://' in target or target.startswith('mailto:'):
            continue
        assert (p.parent / target).exists(), (name, target)
        count += 1
print(f'{count} local documentation references resolve.')

rows = [list(map(str.strip, line.split('|')[1:-1])) for line in (root / 'docs/PROGRESS.md').read_text(encoding='utf-8').splitlines() if re.match(r'^\| (EX-|P5-|RI-|T\d)', line)]
tasks = {r[0]: r for r in rows}
assert len(tasks) == len(rows) and all(len(r) == 8 for r in rows)
assert tasks['P5-03'][3] == tasks['T15'][3] == 'done'
assert next(r[0] for r in rows if r[3] in ['todo', 'in_progress']) == 'P5-04'
for i in range(5, 6):
    assert tasks[f'P5-0{i}'][3] == 'blocked'
for key in ['EX-08H', 'T18']:
    assert tasks[key][3:5] == ['blocked', 'EX-08B'] and not tasks[key][7]
decisions = (root / 'docs/DECISIONS.md').read_text(encoding='utf-8')
ids = re.findall(r'^\| (D-EX-[^ |]+) \|', decisions, re.M)
assert len(ids) == len(set(ids)) and 'D-EX-24' in ids
assert re.search(r'^\| D-EX-Q08 \|.*\| decided \|', decisions, re.M)
assert re.search(r'^\| D-EX-Q07 \|.*\| open \|', decisions, re.M)
assert '# pass 220' in (out / 'suite.log').read_text(encoding='utf-8')
browser = json.loads((out / 'browser-result.json').read_text(encoding='utf-8'))
assert browser['replay']['same'] and not browser['errors']
assert 'PASS:' in (out / 'browser-final.log').read_text(encoding='utf-8-sig')
print('P5-03/T15 technical work complete; P5-04 next; human gates blocked; Q08 decided/Q07 open; 220 tests and browser replay confirmed.')
