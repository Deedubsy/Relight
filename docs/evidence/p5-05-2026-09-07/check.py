"""P5-05 evidence integrity, references and completion boundaries."""
from pathlib import Path
from datetime import datetime, timezone
import hashlib
import json
import re
import sys
import subprocess

root = Path(__file__).resolve().parents[3]
out = Path(__file__).resolve().parent
digest = lambda p: hashlib.sha256(p.read_bytes()).hexdigest()
frozen = root / 'docs/evidence/representative-loop-2026-09-07'
old = json.loads((frozen / 'build-manifest.json').read_text(encoding='utf-8'))
for name, expected in old['archives'].items():
    assert digest(frozen / name) == expected, name
print('Frozen EX-08A source/build archives retain their original hashes.')

count = 0
for name in ['E-chain', 'E-coal', 'E-tram', 'E-logistics']:
    for seed in [3, 4, 5]:
        p = root / f'docs/experiments/campaign/{name}-seed{seed}.json'
        r = json.loads(p.read_text(encoding='utf-8'))
        assert r['seed'] == seed and r['id'] == name
        assert r['config_ref']['kind'] == 'campaign' and r['finalState']['ruleset'] == 'exploration-v2'
        assert all(c['pass'] for c in r['checks']) and len(r['log']) > 0
        for path, expected in r['source_files'].items(): assert digest(root / path) == expected, (p.name, path)
        assert digest(root / r['blueprint']) == r['blueprint_hash']
        if name == 'E-logistics':
            m = r['measurements']
            assert m['hours'] == 5 and m['elapsedSeconds'] >= 18000 and m['maxError'] <= .01
            assert len(m['checkpoints']) == 5
            assert all(c['same'] and c['save']['same'] for c in m['checkpoints'])
            assert all(b['made']['magazine'] > a['made']['magazine'] for a, b in zip(m['checkpoints'], m['checkpoints'][1:]))
        count += 1
print(f'{count} campaign results pass; tested sources/blueprints match; three five-hour soaks have matching hourly saves/replays and live production.')

names = ['CLAUDE.md', 'README.md'] + ['docs/' + n + '.md' for n in ['DECISIONS', 'PROGRESS', 'PROGRAMME_STATE', 'PHASES', 'RELIGHT-design', 'EXPLORATION_DEFENCE_PLAN', 'PHASE_5_SCOPE_REPORT', 'P5_05_INTEGRATION_REPORT', 'STANDARDS']]
refs = 0
for name in names:
    p = root / name
    for link in re.findall(r'\]\(([^)]+)\)', p.read_text(encoding='utf-8')):
        target = link.split('#')[0]
        if not target or '://' in target or target.startswith('mailto:'): continue
        assert (p.parent / target).exists(), (name, target)
        refs += 1
rows = [list(map(str.strip, line.split('|')[1:-1])) for line in (root / 'docs/PROGRESS.md').read_text(encoding='utf-8').splitlines() if re.match(r'^\| (EX-|P5-|RI-|T\d)', line)]
tasks = {r[0]: r for r in rows}
assert len(tasks) == len(rows) and all(len(r) == 8 for r in rows)
assert tasks['P5-05'][3] == tasks['T17'][3] == 'done'
assert next(r[0] for r in rows if r[3] in ['todo', 'in_progress']) == 'EX-09B'
assert tasks['RI-09'][3:5] == ['blocked', 'EX-09B']
for key in ['EX-08H', 'T18']: assert tasks[key][3:5] == ['blocked', 'EX-08B'] and not tasks[key][7]
decisions = (root / 'docs/DECISIONS.md').read_text(encoding='utf-8')
ids = re.findall(r'^\| (D-EX-[^ |]+) \|', decisions, re.M)
assert len(ids) == len(set(ids)) and 'D-EX-26' in ids
assert re.search(r'^\| D-EX-Q08 \|.*\| decided \|', decisions, re.M)
assert re.search(r'^\| D-EX-Q07 \|.*\| open \|', decisions, re.M)
assert '# pass 232' in (out / 'suite-complete.log').read_text(encoding='utf-8')
assert '# pass 4' in (out / 'focused-complete.log').read_text(encoding='utf-8')
perf = json.loads((out / 'performance-result.json').read_text(encoding='utf-8'))
assert not perf['errors'] and perf['gap']
node = 'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/bin/node.exe'
js = "const fs=require('node:fs'),c=require('node:crypto');const r=JSON.parse(fs.readFileSync(process.argv[1],'utf8'));if(c.createHash('sha256').update(JSON.stringify(r.finalState)).digest('hex')!==process.argv[2])process.exit(1);"
subprocess.run([node, '-e', js, str(root / 'docs/experiments/campaign/E-logistics-seed3.json'), perf['fixtureStateSha256']], check=True)
assert {r['viewport']['width'] for r in perf['rows']} == {900, 1366, 1920}
assert all(r['lightChecks'] > 0 and not r['horizontalOverflow'] for r in perf['rows'])
assert all(r['lightPaints'] > 0 for r in perf['rows'] if r['preview'])
print(f'{refs} references resolve; P5-05/T17 done; EX-09B next; human gates pending, Q07 open, performance gap retained.')

if '--record' in sys.argv:
    files = set(old['source_files']) | {'package.json', 'packages/harness/blueprints/factory-line.json'}
    for package in ['sim', 'harness', 'tools', 'game']:
        for folder in ['src', 'test']:
            files.update(p.relative_to(root).as_posix() for p in (root / 'packages' / package / folder).rglob('*') if p.is_file())
    data = {
        'parent_commit': old['source_commit'], 'branch': 'codex/tram-expansion',
        'status': 'P5-05 uncommitted working-tree verification; parent commit is not the tested source identity',
        'recorded_at': datetime.now(timezone.utc).isoformat(),
        'runtime': 'WSL Ubuntu-24.04 Node 22.18.0; installed Windows Chrome through Playwright',
        'source_files': {n: digest(root / n) for n in sorted(files)},
        'build_files': {p.relative_to(root).as_posix(): digest(p) for p in sorted((root / 'packages/game/dist').rglob('*')) if p.is_file()},
        'experiment_files': {p.relative_to(root).as_posix(): digest(p) for p in sorted((root / 'docs/experiments/campaign').glob('*.json'))},
        'preserved_ex08a_archives': old['archives'],
        'verification_files': {p.name: digest(p) for p in sorted(out.iterdir()) if p.suffix in ['.log', '.json', '.png', '.cjs', '.sh', '.py'] and p.name not in ['build-manifest.json', 'consistency.log']},
    }
    (out / 'build-manifest.json').write_text(json.dumps(data, indent=2) + '\n', encoding='utf-8', newline='\n')
manifest = json.loads((out / 'build-manifest.json').read_text(encoding='utf-8'))
for group in ['source_files', 'build_files', 'experiment_files']:
    for name, expected in manifest[group].items(): assert digest(root / name) == expected, name
for name, expected in manifest['verification_files'].items(): assert digest(out / name) == expected, name
print(f"Verified {len(manifest['source_files'])} source/test/build inputs, {len(manifest['build_files'])} build files and all experiment/evidence hashes.")
