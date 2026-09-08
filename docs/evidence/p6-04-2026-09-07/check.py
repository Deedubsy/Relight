"""P6-04: verify unchanged reviewed source, fresh checks, retained evidence and handoff."""
from pathlib import Path
from datetime import datetime,timezone
import hashlib,json,re,sys
root=Path(__file__).resolve().parents[3];out=Path(__file__).resolve().parent
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
def read(p):
 b=p.read_bytes();return b.decode('utf-16' if b.startswith((b'\xff\xfe',b'\xfe\xff')) else 'utf-8-sig')
prev=root/'docs/evidence/p6-03-2026-09-07';old=json.loads(read(prev/'build-manifest.json'))
protected=dict(old['preserved_historical_files'])
for n,h in protected.items():assert sha(root/n)==h,n
for key in ['source_files','build_files']:
 for n,h in old[key].items():assert sha(root/n)==h,n
for key in ['verification_files','archives']:
 for n,h in old[key].items():assert sha(prev/n)==h,n
for folder in [prev,root/'docs/evidence/p6-ammo-2026-09-07']:
 for p in folder.rglob('*'):
  if p.is_file():protected[p.relative_to(root).as_posix()]=sha(p)
for n in ['P6_03_NETWORK_DEFENCE_REPORT.md','P6_AMMO_INVESTIGATION.md','PHASE_6_SCOPE_REPORT.md','PHASE_5_PLAYTEST_REPORT.md']:
 protected['docs/'+n]=sha(root/'docs'/n)
for filename,key in [('source.zip','source_files'),('build.zip','build_files')]:
 import zipfile
 with zipfile.ZipFile(prev/filename) as z:
  assert set(z.namelist())==set(old[key])
  for n,h in old[key].items():assert hashlib.sha256(z.read(n)).hexdigest()==h,n
results=list((prev/'recorded/campaign').glob('*.json'));assert len(results)==9
for p in results:
 a=json.loads(read(p));assert len(a['checks'])==13 and all(c['pass'] for c in a['checks'])
 for n,h in a['source_files'].items():assert sha(root/n)==h,n
ammo=root/'docs/evidence/p6-ammo-2026-09-07'
for name in ['direct-probe.log','inserter-probe.log']:assert 'PASS: 8 turret perimeter tiles' in read(ammo/name)
for name,needle in [('tests','# pass 250'),('typecheck','built in'),('lint','eslint '),('snapshot',' matches '),('docsync','doc tables match'),('freshness','every generated file was made on an ancestor')]:assert needle in read(out/(name+'.log')),name
assert '# fail 0' in read(out/'tests.log') and '# skipped 0' in read(out/'tests.log')
rows={}
for line in read(root/'docs/PROGRESS.md').splitlines():
 if not re.match(r'^\| (P[56]-|EX-|RI-|T\d)',line):continue
 cells=[c.strip() for c in line.split('|')[1:-1]];assert len(cells)==8 and cells[0] not in rows;rows[cells[0]]=cells
assert rows['P6-04'][3]=='done' and rows['EX-08B'][3]=='todo'
assert rows['P6-AMMO'][3]=='blocked' and 'owner retest' in rows['P6-AMMO'][4]
assert next(n for n,r in rows.items() if r[3] in ['todo','in_progress'])=='EX-08B'
assert all(rows[n][3]=='blocked' for n in ['P6-H','EX-08H','T18'])
docs=['CLAUDE.md','README.md']+['docs/'+n+'.md' for n in ['PROGRESS','PROGRAMME_STATE','DECISIONS','PHASES','STANDARDS','EXPLORATION_DEFENCE_PLAN','P6_04_READINESS_REPORT']]+['docs/evidence/p6-04-2026-09-07/README.md']
links=0;manifest=out/'review-manifest.json'
for n in docs:
 p=root/n
 for target in re.findall(r'\]\(([^)]+)\)',read(p)):
  target=target.split('#')[0]
  if not target or '://' in target:continue
  dest=(p.parent/target).resolve()
  if not ('--capture' in sys.argv and dest==manifest):assert dest.exists(),(n,target)
  links+=1
currentdocs={n:sha(root/n) for n in docs}
if '--capture' in sys.argv:
 assert not manifest.exists(),'preserve captured review'
 data={'recorded_at':datetime.now(timezone.utc).isoformat(),'parent_commit':old['parent_commit'],'status':'P6-04 engineering review complete; EX-08B next; ammo report awaits owner retest, human/performance gates remain open','source_manifest':'docs/evidence/p6-03-2026-09-07/build-manifest.json','source_manifest_sha256':sha(prev/'build-manifest.json'),'source_files':old['source_files'],'build_files':old['build_files'],'tests_passed':250,'retained_workload_checks':117,'retained_ammo_cases':16,'protected_files':protected,'review_docs':currentdocs,'verification_files':{p.name:sha(p) for p in out.iterdir() if p.is_file() and p.name not in ['review-manifest.json','consistency.log']}}
 manifest.write_bytes((json.dumps(data,indent=2)+'\n').encode('utf-8'))
else:
 data=json.loads(read(manifest));assert currentdocs==data['review_docs']
 for n,h in data['protected_files'].items():assert sha(root/n)==h,n
 for n,h in data['verification_files'].items():assert sha(out/n)==h,n
print(f'PASS: 250 current tests and build/lint/snapshot/docs checks; {links} local links; EX-08B next, ammo retest pending.')
print(f'PASS: {len(old["source_files"])} unchanged source inputs, {len(old["build_files"])} identical build files; {len(protected)} historical files preserved; retained 117 workload checks and 16 ammo cases.')
