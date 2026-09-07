"""Readiness review: retained evidence integrity and current documentation boundaries."""
from pathlib import Path
from datetime import datetime, timezone
import hashlib
import json
import re
import sys

root = Path(__file__).resolve().parents[3]
out = Path(__file__).resolve().parent
sha = lambda p: hashlib.sha256(p.read_bytes()).hexdigest()
read = lambda p: p.read_text(encoding='utf-8')
latest_dir = root / 'docs/evidence/p5-05-2026-09-07'
latest = json.loads(read(latest_dir / 'build-manifest.json'))
for group in ['source_files', 'build_files', 'experiment_files']:
    for name, expected in latest[group].items(): assert sha(root / name) == expected, name
for name, expected in latest['verification_files'].items(): assert sha(latest_dir / name) == expected, name
for name, expected in latest['preserved_ex08a_archives'].items():
    assert sha(root / 'docs/evidence/representative-loop-2026-09-07' / name) == expected, name
for n in range(1, 5):
    folder = root / f'docs/evidence/p5-0{n}-2026-09-07'
    manifest = json.loads(read(folder / 'build-manifest.json'))
    for name, expected in manifest['verification_files'].items(): assert sha(folder / name) == expected, (n, name)
    browser = json.loads(read(folder / 'browser-result.json'))
    assert not browser['errors'], n
assert '# pass 232' in read(latest_dir / 'suite-complete.log')
assert '# pass 4' in read(latest_dir / 'focused-complete.log')
experiments = []
for p in sorted((root / 'docs/experiments/campaign').glob('E-*.json')):
    r = json.loads(read(p)); assert all(c['pass'] for c in r['checks'])
    for name, expected in r['source_files'].items(): assert sha(root / name) == expected, (p.name, name)
    if r['id'] == 'E-logistics':
        m = r['measurements']; assert m['hours'] == 5 and m['elapsedSeconds'] >= 18000
        assert len(m['checkpoints']) == 5 and all(c['same'] and c['save']['same'] for c in m['checkpoints'])
    experiments.append({'file': p.relative_to(root).as_posix(), 'checks': len(r['checks']), 'seed': r['seed']})
assert len(experiments) == 12
print('Current 166 source/test/build inputs, 6 production files, twelve experiments and historical P5 verification artifacts match their manifests; EX-08A archives preserved.')

if '--capture' in sys.argv:
    paths = set()
    for group in ['source_files', 'build_files', 'experiment_files']: paths.update(latest[group])
    for folder in ['p5-01', 'p5-02', 'p5-03', 'p5-04', 'p5-05']:
        paths.update(p.relative_to(root).as_posix() for p in (root / f'docs/evidence/{folder}-2026-09-07').rglob('*') if p.is_file())
    paths.update(p.relative_to(root).as_posix() for p in (root / 'docs').glob('P5_0*_REPORT.md'))
    paths.update(['docs/EX08_SESSION_GUIDE.md', 'docs/EX08_SESSION_RECORD.md'])
    data = {'recorded_at': datetime.now(timezone.utc).isoformat(), 'reviewed_manifest_sha256': sha(latest_dir / 'build-manifest.json'),
            'protected_files': {n: sha(root / n) for n in sorted(paths)}, 'experiments': experiments,
            'assessment': 'Source/build/evidence review only. Existing 232-test full suite plus four final-source harness regressions are retained results, not a new simulation run.'}
    (out / 'review-manifest.json').write_text(json.dumps(data, indent=2) + '\n', encoding='utf-8', newline='\n')
    print(f'Captured {len(paths)} protected files before documentation changes.')
    sys.exit(0)

review = json.loads(read(out / 'review-manifest.json'))
for name, expected in review['protected_files'].items(): assert sha(root / name) == expected, name
docs = ['CLAUDE.md', 'README.md'] + ['docs/' + n + '.md' for n in ['PROGRESS','PROGRAMME_STATE','DECISIONS','RELIGHT-design','PHASES','STANDARDS','EXPLORATION_DEFENCE_PLAN','PHASE_5_READINESS_REPORT']]
references = 0
for name in docs:
    p = root / name
    for target in re.findall(r'\]\(([^)]+)\)', read(p)):
        target = target.split('#')[0]
        if not target or '://' in target or target.startswith('mailto:'): continue
        assert (p.parent / target).exists(), (name, target)
        references += 1
rows = [list(map(str.strip, line.split('|')[1:-1])) for line in read(root / 'docs/PROGRESS.md').splitlines() if re.match(r'^\| (EX-|P5-|RI-|T\d)', line)]
tasks = {r[0]: r for r in rows}; assert len(tasks) == len(rows)
assert tasks['EX-09B'][3] == tasks['RI-09'][3] == 'done'
assert next(r[0] for r in rows if r[3] in ['todo','in_progress']) == 'EX-08B'
for name in ['EX-08H','T18']: assert tasks[name][3:5] == ['blocked','EX-08B'] and not tasks[name][7]
assert all(tasks[f'T{n}'][3] == 'done' for n in range(13,18))
decisions = read(root / 'docs/DECISIONS.md'); ids = re.findall(r'^\| (D-EX-[^ |]+) \|', decisions, re.M)
assert len(ids) == len(set(ids)) and 'D-EX-27' in ids
assert re.search(r'^\| D-EX-Q07 \|.*\| open \|', decisions, re.M)
report = read(root / 'docs/PHASE_5_READINESS_REPORT.md')
for marker in ['24 ms','116.8','150.1','D-SA-2','C3','D-B2-2','unscored','not rerun','EX-08B']: assert marker in report, marker
print(f'{len(review["protected_files"])} protected files unchanged; {references} references resolve; EX-09B/RI-09 done, EX-08B next, human gates and performance gaps retained.')
