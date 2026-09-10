from pathlib import Path
import hashlib, json, re, zipfile
root = Path(__file__).resolve().parents[3]
out = Path(__file__).resolve().parent
read = lambda p: p.read_text(encoding='utf-8-sig')
sha = lambda p: hashlib.sha256(p.read_bytes()).hexdigest()
previous = root / 'docs/evidence/ex08c-2026-09-08'
build = json.loads(read(previous / 'build-manifest.json'))
evidence = json.loads(read(previous / 'verification-manifest.json'))
for name, digest in build['source_files'].items():
    assert sha(root / name) == digest, name
for name, digest in evidence['verification_files'].items():
    assert sha(previous / name) == digest, name
for archive, key in [('source.zip', 'source_files'), ('playtest-build.zip', 'served_files')]:
    assert sha(previous / archive) == build['archives'][archive], archive
    with zipfile.ZipFile(previous / archive) as z:
        assert set(z.namelist()) == set(build[key])
        for name, digest in build[key].items():
            assert hashlib.sha256(z.read(name)).hexdigest() == digest, name
for name in ['EX08C_SESSION_GUIDE.md', 'EX08C_SESSION_RECORD.md', 'EX08C_PREPARATION_REPORT.md']:
    assert sha(root / 'docs' / name) == evidence['documents']['docs/' + name], name
assert '**Status: NOT RUN.**' in read(root / 'docs/EX08C_SESSION_RECORD.md')
prior = root / 'docs/evidence/p7-05-2026-09-08'
pv = json.loads(read(prior / 'verification.json'))
for name, digest in pv['evidence'].items():
    assert sha(prior / name) == digest, ('P7-05', name)
assert sha(root / 'docs/P7_05_REVIEW_REPORT.md') == pv['documents']['docs/P7_05_REVIEW_REPORT.md']
old = root / 'docs/evidence/ex08b-2026-09-07'
ov = json.loads(read(old / 'verification-manifest.json'))
ob = json.loads(read(old / 'build-manifest.json'))
for name, digest in ov['protected_files'].items():
    # Later completed campaign experiments supersede these generated working files.
    if not name.startswith('docs/experiments/campaign/'):
        assert sha(root / name) == digest, name
for name, digest in ov['verification_files'].items():
    assert sha(old / name) == digest, name
for name, digest in ob['archives'].items():
    assert sha(old / name) == digest, name
for name in ['EX08B_PREPARATION_REPORT', 'EX08B_SESSION_GUIDE', 'EX08B_SESSION_RECORD']:
    assert sha(root / 'docs' / (name + '.md')) == ov['documents']['docs/' + name + '.md']
docs = ['CLAUDE.md', 'README.md'] + ['docs/' + n + '.md' for n in [
    'DECISIONS', 'PROGRESS', 'PROGRAMME_STATE', 'PHASES', 'EXPLORATION_DEFENCE_PLAN',
    'STANDARDS', 'RELIGHT-design', 'PHASE_8_SCOPE_REPORT']]
links = 0
for name in docs:
    p = root / name
    for ref in re.findall(r'\]\(([^)]+)\)', read(p)):
        if '://' in ref or ref.startswith('#'):
            continue
        ref = ref.split('#')[0].strip('<>')
        target = (p.parent / ref).resolve()
        if target != out / 'manifest.json':
            assert target.exists(), (name, ref)
        links += 1
s = read(root / 'docs/PROGRESS.md')
rows = {x.split('|')[1].strip(): x.split('|') for x in s.splitlines() if x.startswith('| ')}
assert rows['P8-00'][4].strip() == 'done'
assert rows['P8-01'][4].strip() == 'todo'
for name in ['EX-08H', 'P6-H', 'T18', 'P7-H', 'P8-H']:
    assert rows[name][4].strip() == 'blocked' and 'EX-08D' in rows[name][5], name
assert rows['EX-08C'][4].strip() == 'done'
for name in ['P8-02', 'P8-03', 'P8-04', 'P8-05', 'EX-08D']:
    assert rows[name][4].strip() == 'blocked', name
for name in ['P8-00', 'P8-01', 'P8-02', 'P8-03', 'P8-04', 'P8-05', 'EX-08D', 'P8-H']:
    assert len([x for x in s.splitlines() if x.startswith('| ' + name + ' |')]) == 1, name
assert '| D-EX-Q10 |' in read(root / 'docs/DECISIONS.md')
log = read(out / 'focused.log')
assert '# pass 20' in log and '# fail 0' in log and '# skipped 0' in log
assert 'doc tables match' in read(out / 'docsync.log')
assert 'every generated file was made on an ancestor' in read(out / 'freshness.log')
print(f'PASS: {len(build["source_files"])} source inputs, EX-08C archives/10 served files/36 verification files and blank human records unchanged; retained P7-05/EX-08B evidence unchanged.')
print(f'PASS: {links} local references; P8 review and decided Q10; post-P8 human dependencies; 20/20 focused tests; docsync/freshness.')
(out / 'manifest.json').write_bytes(json.dumps({
    'parent_commit': build['parent_commit'], 'source_files': build['source_files'],
    'documents': {n: sha(root / n) for n in docs},
    'verification_files': {p.name: sha(p) for p in out.iterdir() if p.is_file() and p.name not in ['manifest.json', 'consistency.log']},
    'status': 'P8 scope review complete; Q10 adopted; P8-01 next; no gameplay change or new human result',
    'sourceInputs': len(build['source_files']), 'localReferences': links,
    'priorEX08C': 'unchanged', 'priorP705': 'unchanged', 'priorEX08B': 'unchanged'
}, indent=2).encode('utf-8'))
