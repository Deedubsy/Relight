from pathlib import Path
from datetime import datetime,timezone
import hashlib,json,re,sys,zipfile
root=Path(__file__).resolve().parents[3];out=Path(__file__).resolve().parent
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
def read(p):
 b=p.read_bytes();return b.decode('utf-16' if b.startswith((b'\xff\xfe',b'\xfe\xff')) else 'utf-8-sig')
oldfolder=root/'docs/evidence/p6-04-2026-09-07';old=json.loads(read(oldfolder/'review-manifest.json'));protected=dict(old['protected_files'])
for n,h in protected.items():assert sha(root/n)==h,n
for n,h in old['verification_files'].items():assert sha(oldfolder/n)==h,n
for p in oldfolder.iterdir():
 if p.is_file():protected[p.relative_to(root).as_posix()]=sha(p)
protected['docs/P6_04_READINESS_REPORT.md']=sha(root/'docs/P6_04_READINESS_REPORT.md')
# Freeze old EX-08 documents as well; the B guide/record are distinct artifacts.
for n in ['EX08_PREPARATION_REPORT','EX08_SESSION_GUIDE','EX08_SESSION_RECORD','EXPLORATION_DEFENCE_PLAYTEST']:
 p=root/'docs'/(n+'.md');protected[p.relative_to(root).as_posix()]=sha(p)
build=json.loads(read(out/'build-manifest.json'))
assert build['source_files']==old['source_files']
for n,h in build['source_files'].items():assert sha(root/n)==h,n
for n,h in old['build_files'].items():assert sha(root/n)==h,n
for archive,key in [('source.zip','source_files'),('playtest-build.zip','served_files')]:
 assert sha(out/archive)==build['archives'][archive]
 with zipfile.ZipFile(out/archive) as z:
  assert set(z.namelist())==set(build[key])
  for n,h in build[key].items():assert hashlib.sha256(z.read(n)).hexdigest()==h,n
assert sha(out/'settings.json')==build['settings_sha256']
for name in ['fresh','network']:
 assert sha(out/'campaign'/(name+'.json'))==build['served_files'][name+'.json']
assert '# pass 51' in read(out/'focused.log') and '# fail 0' in read(out/'focused.log') and '# skipped 0' in read(out/'focused.log')
assert 'PASS: fresh start and ordinary-command network prefix' in read(out/'prepare.log')
b=json.loads(read(out/'browser-result.json'));assert not b['errors'] and len(b['starts'])==4 and len(b['rows'])==4
assert all(s['speed']==0 and s['complete'] and s['saveReload'] and not s['overflow'] for s in b['starts'])
assert all(r['edits']==r['successful'] and r['endSim']>r['startSim']+3 and r['lightPaints']>0 and not r['overflow'] for r in b['rows'])
w=json.loads(read(out/'warning-result.json'));assert not w['errors'] and w['cameraControlsPreserveState'] and w['before']['warning']['receivedAt']==2400
assert 'doc tables match' in read(out/'docsync.log');assert 'every generated file was made on an ancestor' in read(out/'freshness.log')
rows={}
for line in read(root/'docs/PROGRESS.md').splitlines():
 if not re.match(r'^\| (P[56]-|EX-|RI-|T\d)',line):continue
 c=[v.strip() for v in line.split('|')[1:-1]];assert len(c)==8 and c[0] not in rows;rows[c[0]]=c
assert rows['EX-08B'][3]=='done' and all(rows[n][3]=='todo' for n in ['EX-08H','P6-H','T18'])
assert rows['P6-AMMO'][3]=='blocked' and rows['EX-08'][3]=='blocked'
assert next(n for n,r in rows.items() if r[3] in ['todo','in_progress'])=='EX-08H'
record=read(root/'docs/EX08B_SESSION_RECORD.md');assert '**Status: NOT RUN.**' in record and 'Phase 6 verdict and reasons: **not recorded**' in record
names=['CLAUDE.md','README.md']+['docs/'+n+'.md' for n in ['PROGRESS','PROGRAMME_STATE','DECISIONS','PHASES','STANDARDS','EXPLORATION_DEFENCE_PLAN','EX08B_PREPARATION_REPORT','EX08B_SESSION_GUIDE','EX08B_SESSION_RECORD']]+['docs/evidence/ex08b-2026-09-07/README.md']
manifest=out/'verification-manifest.json';links=0
for n in names:
 p=root/n
 for target in re.findall(r'\]\(([^)]+)\)',read(p)):
  target=target.split('#')[0]
  if not target or '://' in target:continue
  dest=(p.parent/target).resolve()
  if not ('--capture' in sys.argv and dest==manifest):assert dest.exists(),(n,target)
  links+=1
current={n:sha(root/n) for n in names}
if '--capture' in sys.argv:
 assert not manifest.exists(),'preserve capture'
 data={'recorded_at':datetime.now(timezone.utc).isoformat(),'parent_commit':build['parent_commit'],'build_manifest_sha256':sha(out/'build-manifest.json'),'source_inputs':len(build['source_files']),'served_files':len(build['served_files']),'focused_tests':51,'status':'EX-08B preparation done; new human session NOT RUN; ammo/performance unresolved','protected_files':protected,'documents':current,'verification_files':{p.relative_to(out).as_posix():sha(p) for p in out.rglob('*') if p.is_file() and p.name not in ['verification-manifest.json','consistency.log']}}
 manifest.write_bytes((json.dumps(data,indent=2)+'\n').encode())
else:
 data=json.loads(read(manifest));assert current==data['documents']
 for n,h in data['protected_files'].items():assert sha(root/n)==h,n
 for n,h in data['verification_files'].items():assert sha(out/n)==h,n
print(f'PASS: 51 focused tests; 4 start/load cases, 4 live interaction samples and natural warning; {links} references and blank human record checked.')
print(f'PASS: 171 unchanged source inputs, 9 served archive files, {len(protected)} preserved historical files; EX-08H/P6-H/T18 ready, no human verdict.')
