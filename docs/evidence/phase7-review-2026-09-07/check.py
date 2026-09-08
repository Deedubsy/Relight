from pathlib import Path
import hashlib, json, re

root = Path(__file__).resolve().parents[3]
out = Path(__file__).resolve().parent
sha = lambda p: hashlib.sha256(p.read_bytes()).hexdigest()
previous = root / 'docs/evidence/ex08b-2026-09-07'
build = json.loads((previous / 'build-manifest.json').read_text())
evidence = json.loads((previous / 'verification-manifest.json').read_text())
for name, digest in build['source_files'].items():
    assert sha(root / name) == digest, name
for name, digest in evidence['protected_files'].items():
    assert sha(root / name) == digest, name
for name, digest in evidence['verification_files'].items():
    assert sha(previous / name) == digest, name
for name, digest in build['archives'].items():
    assert sha(previous / name) == digest, name
for name in ['EX08B_SESSION_GUIDE.md', 'EX08B_SESSION_RECORD.md', 'EX08B_PREPARATION_REPORT.md']:
    assert sha(root / 'docs' / name) == evidence['documents']['docs/' + name], name
docs = ['CLAUDE.md', 'README.md'] + ['docs/' + n + '.md' for n in [
    'DECISIONS', 'PROGRESS', 'PROGRAMME_STATE', 'PHASES', 'EXPLORATION_DEFENCE_PLAN',
    'STANDARDS', 'RELIGHT-design', 'PHASE_7_SCOPE_REPORT']]
links = 0
for name in docs:
    p = root / name
    for ref in re.findall(r'\]\(([^)]+)\)', p.read_text(encoding='utf-8-sig')):
        if re.match(r'\w+://', ref) or ref.startswith('#'):
            continue
        ref = ref.split('#')[0].strip('<>')
        assert (p.parent / ref).exists(), (name, ref)
        links += 1
s = (root / 'docs/PROGRESS.md').read_text(encoding='utf-8')
rows = {x.split('|')[1].strip(): x.split('|') for x in s.splitlines() if x.startswith('| ')}
assert rows['P7-00'][4].strip() == 'done'
assert rows['P7-D'][4].strip() == 'todo'
for name in ['EX-08H', 'P6-H', 'T18', 'P7-H']:
    assert rows[name][4].strip() == 'blocked' and 'EX-08C' in rows[name][5], name
assert rows['EX-08B'][4].strip() == 'done'
assert len([x for x in s.splitlines() if x.startswith('| P7-01 |')]) == 1
assert '# pass 19' in (out / 'focused.log').read_text()
print(f'PASS: {len(build["source_files"])} source inputs, {len(evidence["protected_files"])} prior protected files, EX-08B archives/records and {len(evidence["verification_files"])} verification files unchanged.')
print(f'PASS: {links} local references; P7 review/catalogue and deferred human task consistency; 19 focused tests.')
(out / 'manifest.json').write_text(json.dumps({'source_files': build['source_files'],
    'documents': {n: sha(root / n) for n in docs},
    'status': 'P7 review complete; Q09 recommended pending owner choice; no new gameplay or human result'}, indent=2), encoding='utf-8')
