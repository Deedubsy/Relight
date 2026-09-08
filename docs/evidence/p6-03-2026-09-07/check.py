"""Freeze and verify P6-03 source/build, workload evidence and unchanged historical evidence."""
from pathlib import Path
from datetime import datetime,timezone
import hashlib,json,re,subprocess,sys,zipfile
root=Path(__file__).resolve().parents[3];out=Path(__file__).resolve().parent
capture='--capture' in sys.argv
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
def read(p):
 b=p.read_bytes();return b.decode('utf-16' if b.startswith((b'\xff\xfe',b'\xfe\xff')) else 'utf-8-sig')
previous=root/'docs/evidence/p6-02-2026-09-07'
old=json.loads(read(previous/'build-manifest.json'))
historical=dict(old['preserved_historical_files'])
for n,h in historical.items():assert sha(root/n)==h,n
for group in ['verification_files','archives']:
 for n,h in old[group].items():assert sha(previous/n)==h,n
for p in previous.iterdir():
 if p.is_file():historical[p.relative_to(root).as_posix()]=sha(p)
historical['docs/P6_02_INFORMATION_REPORT.md']=sha(root/'docs/P6_02_INFORMATION_REPORT.md')
changed={n for n,h in old['source_files'].items() if sha(root/n)!=h}
assert changed=={'package.json','packages/sim/src/campaignDistricts.ts'},changed
new={'packages/harness/src/defenceScenario.ts','packages/harness/src/defenceCli.ts','packages/sim/test/defenceHarness.test.ts'}
sources={n:sha(root/n) for n in sorted(set(old['source_files'])|new)}
builds={p.relative_to(root).as_posix():sha(p) for p in sorted((root/'packages/game/dist').rglob('*')) if p.is_file()}
suite=read(out/'suite-recorded.log');assert '# fail 0' in suite and '# skipped 0' in suite
passed=int(re.search(r'^# pass (\d+)$',suite,re.M)[1]);assert passed==250,passed
for name,needle in [('build-recorded','built in'),('lint-recorded','eslint '),('snapshot-recorded',' matches '),('docsync','doc tables match'),('freshness','every generated file was made on an ancestor'),('audit','Verified 117 checks'),('survey-check','')]:
 assert needle in read(out/(name+'.log')),name
measure=json.loads(read(out/'measurements.json'));assert measure['checksPassed']==117 and len(measure['rows'])==9
for p in (out/'recorded/campaign').glob('*.json'):
 a=json.loads(read(p));assert len(a['checks'])==13 and all(c['pass'] for c in a['checks']),p.name
 for n,h in a['source_files'].items():assert sha(root/n)==h,n
rows={}
for line in read(root/'docs/PROGRESS.md').splitlines():
 if not re.match(r'^\| (P[56]-|EX-|RI-|T\d)',line):continue
 cells=[c.strip() for c in line.split('|')[1:-1]];assert len(cells)==8 and cells[0] not in rows,line;rows[cells[0]]=cells
assert rows['P6-03'][3]=='done' and rows['P6-04'][3]=='todo'
assert next(n for n,r in rows.items() if r[3] in ['todo','in_progress'])=='P6-04'
assert all(rows[n][3]=='blocked' for n in ['P6-H','EX-08B','EX-08H','T18'])
docs=['CLAUDE.md','README.md']+['docs/'+n+'.md' for n in ['PROGRESS','PROGRAMME_STATE','DECISIONS','RELIGHT-design','PHASES','STANDARDS','EXPLORATION_DEFENCE_PLAN','P6_03_NETWORK_DEFENCE_REPORT']]
docs.append('docs/evidence/p6-03-2026-09-07/README.md')
links=0;pending=[]
for name in docs:
 p=root/name
 for target in re.findall(r'\]\(([^)]+)\)',read(p)):
  target=target.split('#')[0]
  if not target or '://' in target or target.startswith('mailto:'):continue
  dest=(p.parent/target).resolve()
  if capture and dest in {out/'build-manifest.json',out/'source.zip',out/'build.zip'}:pending.append(dest)
  else:assert dest.exists(),(name,target)
  links+=1
manifest=out/'build-manifest.json'
if capture:
 assert not manifest.exists(),'do not overwrite captured evidence'
 for filename,paths in [('source.zip',sources),('build.zip',builds)]:
  assert not (out/filename).exists(),filename
  with zipfile.ZipFile(out/filename,'w',zipfile.ZIP_DEFLATED) as z:
   for name in paths:z.write(root/name,name)
 logs={p.relative_to(out).as_posix():sha(p) for p in out.rglob('*') if p.is_file() and p.suffix in ['.log','.py','.sh','.ts','.json'] and p.name not in ['build-manifest.json','consistency.log']}
 data={'recorded_at':datetime.now(timezone.utc).isoformat(),'parent_commit':subprocess.check_output(['git','rev-parse','HEAD'],cwd=root).decode().strip(),'status':'P6-03 uncommitted ordinary-stock defence evidence; human Phase 6 verdict remains open','runtime':'WSL Ubuntu-24.04, Node 22.18.0','tests_passed':passed,'workload_checks_passed':117,'source_files':sources,'build_files':builds,'verification_files':logs,'archives':{n:sha(out/n) for n in ['source.zip','build.zip']},'preserved_historical_files':historical}
 manifest.write_bytes((json.dumps(data,indent=2)+'\n').encode('utf-8'))
else:
 data=json.loads(read(manifest));assert data['source_files']==sources and data['build_files']==builds
 for n,h in data['preserved_historical_files'].items():assert sha(root/n)==h,n
 for n,h in data['verification_files'].items():assert sha(out/n)==h,n
 for n,h in data['archives'].items():assert sha(out/n)==h,n
for filename,paths in [('source.zip',sources),('build.zip',builds)]:
 with zipfile.ZipFile(out/filename) as z:
  assert set(z.namelist())==set(paths)
  for n,h in paths.items():assert hashlib.sha256(z.read(n)).hexdigest()==h,n
assert all(p.exists() for p in pending)
print(f'{passed} tests, 117 workload checks, {links} local links and P6-04 next verified.')
print(f'Verified {len(sources)} source inputs, {len(builds)} build files and {len(historical)} preserved historical files; candidate failures also retained.')
