from pathlib import Path
import hashlib,json,re,zipfile,subprocess,urllib.request
root=Path(__file__).resolve().parents[3];out=Path(__file__).resolve().parent
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
read=lambda p:p.read_text(encoding='utf-8-sig')
build=json.loads(read(out/'build-manifest.json'));settings=json.loads(read(out/'settings.json'))
assert subprocess.check_output(['git','rev-parse','HEAD'],cwd=root,text=True).strip()==build['parent_commit']
for n,h in build['source_files'].items():assert sha(root/n)==h,n
for archive,key in [('source.zip','source_files'),('playtest-build.zip','served_files')]:
 assert sha(out/archive)==build['archives'][archive]
 with zipfile.ZipFile(out/archive) as z:
  assert set(z.namelist())==set(build[key])
  for n,h in build[key].items():assert hashlib.sha256(z.read(n)).hexdigest()==h,n
assert sha(out/'settings.json')==build['settings_sha256']
for n,h in build['served_files'].items():
 with urllib.request.urlopen('http://127.0.0.1:5180/'+n,timeout=10) as response:assert hashlib.sha256(response.read()).hexdigest()==h,('served',n)
for n in ['fresh','construction','network','turbine']:assert sha(out/'campaign'/(n+'.json'))==build['served_files'][n+'.json']
review5=root/'docs/evidence/p8-05-2026-09-08';v5=json.loads(read(review5/'verification.json'));m5=json.loads(read(review5/'manifest.json'))
for n,h in v5['evidence'].items():assert sha(review5/n)==h,('P8-05',n)
assert sha(root/'docs/P8_05_REVIEW_REPORT.md')==v5['documents']['docs/P8_05_REVIEW_REPORT.md']
assert build['source_files']==m5['source_files']
for n,h in m5['build_files'].items():
 assert sha(root/'packages/game/dist'/n)==h,('P8-05 dist',n)
 if n not in ['truck-ready.json','ammo-ready.json']:assert build['served_files'][n]==h
old=root/'docs/evidence/ex08c-2026-09-08';v=json.loads(read(old/'verification-manifest.json'));b=json.loads(read(old/'build-manifest.json'))
for n,h in v['verification_files'].items():assert sha(old/n)==h,('EX08C',n)
for n,h in b['archives'].items():assert sha(old/n)==h,n
for n in ['EX08C_PREPARATION_REPORT.md','EX08C_SESSION_GUIDE.md','EX08C_SESSION_RECORD.md']:assert sha(root/'docs'/n)==v['documents']['docs/'+n],n
assert '**Status: NOT RUN.**' in read(root/'docs/EX08C_SESSION_RECORD.md')
review=root/'docs/evidence/phase8-review-2026-09-08';rv=json.loads(read(review/'manifest.json'))
for n,h in rv['verification_files'].items():assert sha(review/n)==h,('scope review',n)
assert sha(root/'docs/PHASE_8_SCOPE_REPORT.md')==rv['documents']['docs/PHASE_8_SCOPE_REPORT.md']
prior=root/'docs/evidence/p7-05-2026-09-08';pv=json.loads(read(prior/'verification.json'))
for n,h in pv['evidence'].items():assert sha(prior/n)==h,('P7-05',n)
assert sha(root/'docs/P7_05_REVIEW_REPORT.md')==pv['documents']['docs/P7_05_REVIEW_REPORT.md']
retained=root/'docs/evidence/ex08b-2026-09-07';ov=json.loads(read(retained/'verification-manifest.json'));ob=json.loads(read(retained/'build-manifest.json'))
for n,h in ov['protected_files'].items():
 if not n.startswith('docs/experiments/campaign/'):assert sha(root/n)==h,n
for n,h in ov['verification_files'].items():assert sha(retained/n)==h,n
for n,h in ob['archives'].items():assert sha(retained/n)==h,n
for n in ['EX08B_SESSION_GUIDE.md','EX08B_SESSION_RECORD.md','EX08B_PREPARATION_REPORT.md']:assert sha(root/'docs'/n)==ov['documents']['docs/'+n]
previous=root/'docs/evidence/p8-01-2026-09-08';prev=json.loads(read(previous/'verification.json'))
for n,h in prev['evidence'].items():assert sha(previous/n)==h,('P8-01',n)
assert sha(root/'docs/P8_01_CLIPBOARD_REPORT.md')==prev['documents']['docs/P8_01_CLIPBOARD_REPORT.md']
previous2=root/'docs/evidence/p8-02-2026-09-08';prev2=json.loads(read(previous2/'verification.json'))
for n,h in prev2['evidence'].items():assert sha(previous2/n)==h,('P8-02',n)
assert sha(root/'docs/P8_02_LIBRARY_REPORT.md')==prev2['documents']['docs/P8_02_LIBRARY_REPORT.md']
previous3=root/'docs/evidence/p8-03-2026-09-08';prev3=json.loads(read(previous3/'verification.json'))
for n,h in prev3['evidence'].items():assert sha(previous3/n)==h,('P8-03',n)
assert sha(root/'docs/P8_03_TRUCK_REPORT.md')==prev3['documents']['docs/P8_03_TRUCK_REPORT.md']
for p in (previous3/'campaign').glob('*.json'):assert sha(root/'docs/experiments/campaign'/p.name)==sha(p)
previous4=root/'docs/evidence/p8-04-2026-09-08';prev4=json.loads(read(previous4/'verification.json'))
for n,h in prev4['evidence'].items():assert sha(previous4/n)==h,('P8-04',n)
assert sha(root/'docs/P8_04_REMOVAL_REPORT.md')==prev4['documents']['docs/P8_04_REMOVAL_REPORT.md']

log=read(out/'focused.log');assert '# pass 57' in log and '# fail 0' in log and '# skipped 0' in log
assert 'PASS: four starts and ordinary-command prefixes' in read(out/'prepare.log')
b=json.loads(read(out/'browser-result.json'));assert not b['errors'] and len(b['starts'])==8 and len(b['rows'])==4
for s in b['starts']:assert s['speed']==0 and s['complete'] and s['saveReload'] and not s['overflow']
assert sum(r['successful'] for r in b['rows'])==76
for r in b['rows']:assert r['edits']==r['successful'] and r['lightPaints']>0 and r['endSim']>r['startSim']+3
cb=json.loads(read(out/'construction-browser-result.json'));assert not cb['errors'] and len(cb['rows'])==2
for r in cb['rows']:
 assert r['paused']['work']['phase']=='paused' and r['built']['orders'][-1]['status']=='completed'
 assert r['built']['pockets']==r['paused']['pockets'] and not r['built']['source'] and r['replay']['same'] and r['final']['rounds']==40
probe=json.loads(read(out/'construction-probe.json'));assert probe['rounds']==20 and probe['conservation']['ok'] and probe['fullReplay']
w=json.loads(read(out/'warning-result.json'));assert not w['errors'] and w['cameraControlsPreserveState'] and w['before']['warning']['receivedAt']==2400
rest=json.loads(read(out/'restoration-result.json'));assert len(rest)==2
for r in rest:assert r['replay']['same'] and r['spent']==40 and r['bases']==1
assert 'doc tables match' in read(out/'docsync.log') and 'every generated file was made on an ancestor' in read(out/'freshness.log')
rows={r.split('|')[1].strip():r.split('|') for r in read(root/'docs/PROGRESS.md').splitlines() if r.startswith('| ')}
assert rows['EX-08D'][4].strip()=='done'
for n in ['EX-08H','P6-H','P7-H','P8-H','T18']:assert rows[n][4].strip()=='todo',n
record=read(root/'docs/EX08D_SESSION_RECORD.md');assert '**Status: NOT RUN.**' in record and 'Phase 8 verdict and reasons: **not recorded**' in record and settings['config_hash'] in record
names=['CLAUDE.md','README.md']+['docs/'+n+'.md' for n in ['PROGRESS','PROGRAMME_STATE','DECISIONS','RELIGHT-design','PHASES','STANDARDS','EXPLORATION_DEFENCE_PLAN','EX08D_PREPARATION_REPORT','EX08D_SESSION_GUIDE','EX08D_SESSION_RECORD']]
count=0
for n in names:
 p=root/n
 for ref in re.findall(r'\]\(([^)]+)\)',read(p)):
  if '://' in ref or ref.startswith('#'):continue
  target=(p.parent/ref.split('#')[0].strip('<>')).resolve()
  if target!=out/'verification-manifest.json':assert target.exists(),(n,ref)
  count+=1
# Server access logs remain live; exclude them from immutable evidence identity.
result={'parent_commit':build['parent_commit'],'status':'EX-08D prepared locally; human play NOT RUN','documents':{n:sha(root/n) for n in names},'verification_files':{p.relative_to(out).as_posix():sha(p) for p in out.rglob('*') if p.is_file() and p.name not in ['verification-manifest.json','consistency.log','server.log','server-errors.log']},'priorP801toP805':'unchanged','priorEX08ABC':'unchanged (A files retained through EX08B protected manifest)','sourceInputs':len(build['source_files']),'servedFiles':len(build['served_files']),'localReferences':count}
(out/'verification-manifest.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8',newline='\n')
print(f'PASS: source/P8-05 identity; {len(build["source_files"])} source inputs, {len(build["served_files"])} archived and HTTP-served files; 57 focused tests; eight start/save cases; both truck/direct-ammo cases; warning; two restorations; 76 live edits; {count} references; blank record and human handoff; prior evidence preserved.')
