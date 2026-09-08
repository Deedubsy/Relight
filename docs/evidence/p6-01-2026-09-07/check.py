"""Capture/verify P6-01 source, build, checks and preserved historical artifacts."""
from pathlib import Path
from datetime import datetime, timezone
import hashlib
import json
import re
import subprocess
import sys
import zipfile

root=Path(__file__).resolve().parents[3]
out=Path(__file__).resolve().parent
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def read(p):
    b=p.read_bytes()
    return b.decode('utf-16' if b.startswith((b'\xff\xfe',b'\xfe\xff')) else 'utf-8-sig')
latest=json.loads(read(root/'docs/evidence/p5-05-2026-09-07/build-manifest.json'))
review=json.loads(read(root/'docs/evidence/phase5-readiness-2026-09-07/review-manifest.json'))
historical={n:h for n,h in review['protected_files'].items() if n.startswith('docs/')}
for n,h in historical.items(): assert sha(root/n)==h,n
for n,h in latest['preserved_ex08a_archives'].items():
    assert sha(root/'docs/evidence/representative-loop-2026-09-07'/n)==h,n
print(f'Preserved {len(historical)} historical report/evidence files and both EX-08A archives.')

changed={n for n,h in latest['source_files'].items() if sha(root/n)!=h}
expected={'packages/game/src/session.ts','packages/sim/src/campaignDefence.ts','packages/sim/src/campaignThreat.ts',
          'packages/sim/src/defenceValidation.ts','packages/sim/src/threat.ts','packages/sim/test/campaignDistricts.test.ts'}
assert changed==expected,changed
sources={n:sha(root/n) for n in sorted(set(latest['source_files'])|{'packages/sim/test/campaignReliability.test.ts'})}
builds={p.relative_to(root).as_posix():sha(p) for p in sorted((root/'packages/game/dist').rglob('*')) if p.is_file()}
suite=read(out/'tests-final.log')
assert '# fail 0' in suite and '# skipped 0' in suite
passed=int(re.search(r'^# pass (\d+)$',suite,re.M)[1])
assert passed>=243,passed
assert 'built in' in read(out/'typecheck-final.log')
assert ' matches ' in read(out/'snapshot.log')
assert 'eslint ' in read(out/'lint.log')
assert 'doc tables match' in read(out/'docsync.log')
assert 'every generated file was made on an ancestor' in read(out/'freshness.log')

docs=['CLAUDE.md','README.md']+['docs/'+n+'.md' for n in ['PROGRESS','PROGRAMME_STATE','DECISIONS',
 'RELIGHT-design','PHASES','STANDARDS','EXPLORATION_DEFENCE_PLAN','P6_01_RELIABILITY_REPORT']]
links=0
pending=[]
for name in docs:
    p=root/name
    for target in re.findall(r'\]\(([^)]+)\)',read(p)):
        target=target.split('#')[0]
        if not target or '://' in target or target.startswith('mailto:'): continue
        destination=(p.parent/target).resolve()
        if '--capture' in sys.argv and destination in {out/'build-manifest.json',out/'source.zip',out/'build.zip'}:
            pending.append(destination)
        else: assert destination.exists(),(name,target)
        links+=1
rows={}
for line in read(root/'docs/PROGRESS.md').splitlines():
    if not re.match(r'^\| (P[56]-|EX-|RI-|T\d)',line):continue
    cells=[c.strip() for c in line.split('|')[1:-1]]
    assert len(cells)==8 and cells[0] not in rows,line
    rows[cells[0]]=cells
assert rows['P6-01'][3]=='done' and rows['P6-02'][3]=='todo'
assert next(n for n,r in rows.items() if r[3] in ['todo','in_progress'])=='P6-02'
assert all(rows[n][3]=='blocked' for n in ['P6-03','P6-04','P6-H','EX-08B','EX-08H','T18'])
print(f'{passed} tests pass; final build, lint, legacy snapshot, docs/freshness and {links} local links checked; P6-02 next.')

manifest=out/'build-manifest.json'
if '--capture' in sys.argv:
    assert not manifest.exists(),'do not overwrite captured evidence'
    for filename,paths in [('source.zip',sources),('build.zip',builds)]:
        assert not (out/filename).exists(),filename
        with zipfile.ZipFile(out/filename,'w',zipfile.ZIP_DEFLATED) as z:
            for name in paths:z.write(root/name,name)
    logs={p.name:sha(p) for p in out.iterdir() if p.suffix in ['.log','.py','.sh'] and p.name!='consistency.log'}
    data={'recorded_at':datetime.now(timezone.utc).isoformat(),
          'parent_commit':subprocess.check_output(['git','rev-parse','HEAD'],cwd=root).decode().strip(),
          'status':'P6-01 uncommitted source/build; owner gameplay remains the earlier Phase 5 result',
          'runtime':'WSL Ubuntu-24.04, Node 22.18.0', 'tests_passed':passed,
          'source_files':sources,'build_files':builds,'verification_files':logs,
          'archives':{n:sha(out/n) for n in ['source.zip','build.zip']},'preserved_historical_files':historical}
    manifest.write_text(json.dumps(data,indent=2)+'\n',encoding='utf-8',newline='\n')
else:
    data=json.loads(read(manifest))
    assert data['source_files']==sources and data['build_files']==builds
    for n,h in data['verification_files'].items():assert sha(out/n)==h,n
    for n,h in data['archives'].items():assert sha(out/n)==h,n
for filename,paths in [('source.zip',sources),('build.zip',builds)]:
    with zipfile.ZipFile(out/filename) as z:
        assert set(z.namelist())==set(paths)
        for n,h in paths.items():assert hashlib.sha256(z.read(n)).hexdigest()==h,n
print(f'Verified {len(sources)} source/test/build inputs and {len(builds)} production files in independent P6-01 archives.')
assert all(p.exists() for p in pending)
