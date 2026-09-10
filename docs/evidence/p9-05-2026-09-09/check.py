from pathlib import Path
import hashlib,json,re,zipfile,urllib.request
root=Path(__file__).resolve().parents[3];out=Path(__file__).resolve().parent;repair=root/'docs/evidence/p9-04r-2026-09-09'
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
read=lambda p:p.read_text(encoding='utf-8-sig')
build=json.loads(read(out/'build-manifest.json'));priorBuild=json.loads(read(repair/'manifest.json'))
assert build['source_files']==priorBuild['source_files'] and len(build['source_files'])==205
for n,h in build['source_files'].items():assert sha(root/n)==h,n
for n,h in priorBuild['build_files'].items():assert sha(root/'packages/game/dist'/n)==h and build['served_files'][n]==h,n
for archive,key in [('source.zip','source_files'),('playtest-build.zip','served_files')]:
 assert sha(out/archive)==build['archives'][archive]
 with zipfile.ZipFile(out/archive) as z:
  assert set(z.namelist())==set(build[key])
  for n,h in build[key].items():assert hashlib.sha256(z.read(n)).hexdigest()==h,n
assert len(build['served_files'])==21
for n,h in build['served_files'].items():
 with urllib.request.urlopen('http://127.0.0.1:5181/'+n,timeout=10) as r:assert hashlib.sha256(r.read()).hexdigest()==h,('HTTP',n)
documentByteDifferences=[]
for folder,manifest,key,docs in [
 ('p9-04r-2026-09-09','verification.json','evidence',['P9_04R_REPAIR_REPORT']),
 ('p9-04-2026-09-08','verification.json','evidence',['P9_04_POPULATION_REPORT']),
 ('p9-03-2026-09-08','verification.json','evidence',['P9_03_WAYFINDING_REPORT']),
 ('p9-02-2026-09-08','verification.json','evidence',['P9_02_SURVEY_RELIABILITY_REPORT']),
 ('p9-01-2026-09-08','verification.json','evidence',['P9_01_CITY_VALIDATION_REPORT']),
 ('ex08d-2026-09-08','verification-manifest.json','verification_files',['EX08D_PREPARATION_REPORT','EX08D_SESSION_GUIDE','EX08D_SESSION_RECORD']),
 ('phase9-review-2026-09-08','manifest.json','verification_files',['PHASE_9_SCOPE_REPORT']),
 ('p8-05-2026-09-08','verification.json','evidence',['P8_05_REVIEW_REPORT']),
 ('ex08c-2026-09-08','verification-manifest.json','verification_files',['EX08C_PREPARATION_REPORT','EX08C_SESSION_GUIDE','EX08C_SESSION_RECORD'])]:
 prior=root/'docs/evidence'/folder;v=json.loads(read(prior/manifest))
 for n,h in v[key].items():assert sha(prior/n)==h,(folder,n)
 for n in docs:
  p=root/'docs'/(n+'.md');expected=v['documents']['docs/'+n+'.md']
  if sha(p)!=expected:
   normalized=hashlib.sha256(p.read_bytes().replace(b'\r\n',b'\n')).hexdigest()
   assert normalized==expected,(n,'historical content changed')
   documentByteDifferences.append({'document':p.relative_to(root).as_posix(),'frozenSha256':expected,'currentSha256':sha(p),'difference':'CRLF-only; normalized bytes equal frozen document; current formatting preserved'})
review=json.loads(read(root/'docs/evidence/phase9-review-2026-09-08/working-tree-difference.json'))
assert sha(root/review['path'])==review['currentSha256'],'unrelated fixture changed again'
population=repair/'population';identity=json.loads(read(population/'manifest.json'));summary=json.loads(read(population/'summary.json'));analysis=json.loads(read(repair/'analysis.json'));comparison=json.loads(read(repair/'comparison.json'))
assert identity['seeds']==list(range(1,10001)) and summary['complete'] and summary['attempted']==10000 and summary['sourceUnchanged'] and summary['failed']==0
assert analysis['complete'] and analysis['passed']==10000 and not analysis['failureClasses']
assert comparison['transitions']=={'passed -> passed':9742,'failed -> passed':258} and not comparison['regressions'] and not comparison['baseGeneratorAttemptChanges']
assert len(list(population.glob('seed-*.json')))==10000 and not (population/'run.lock').exists()
assert '10000 attempted seeds' in read(repair/'population-integrity.log')
for n,h in identity['sourceFiles'].items():assert sha(root/n)==h,('population source',n)
archive=json.loads(read(repair/'population-archive.json'));assert sha(repair/'population.zip')==archive['archive_sha256']
with zipfile.ZipFile(repair/'population.zip') as z:
 assert set(z.namelist())==set(archive['files'])
 for n,h in archive['files'].items():assert sha(population/n)==h and hashlib.sha256(z.read(n)).hexdigest()==h,n
failed=json.loads(read(repair/'failed-seeds-final/summary.json'));assert failed['complete'] and failed['passed']==258 and failed['failed']==0 and failed['sourceUnchanged']
for seed in [3,4,5,8,11,13,80,88,102,842,2313,3610]:
 folder='paid-final' if seed==842 else 'paid'
 r=json.loads(read(repair/f'{folder}/seed-{seed}.json'));saved=json.loads(read(repair/f'{folder}/save-{seed}.json'))
 assert r['hash']==r['replay']==saved['hash'] and r['conserved'] and r['continuation']['same'] and saved['logComplete']
 assert r['stops']==3 and r['freightDelivered']==5 and r['truck']['order']=='completed' and r['truck']['pocketsUnchanged'] and 'travelling' in r['truck']['phases']
 if seed==842:assert r['extraSalvage']==9
for seed in [3,4,5,8,11,13,80,305,1340,7384,8102,9711]:
 for item in ['steel','copper','coal']:
  r=json.loads(read(repair/f'extraction/seed-{seed}-{item}.json'));saved=json.loads(read(repair/f'extraction/save-{seed}-{item}.json'))
  assert r['status']=='passed' and r['delivered']>=5 and r['conserved'] and r['continuation']['same'] and r['hash']==r['replay']==saved['hash']
for seed in [88,842]:
 r=json.loads(read(repair/f'visit-{seed}.json'));saved=json.loads(read(repair/f'visit-save-{seed}.json'))
 assert r['ordinaryWalk'] and r['conserved'] and r['hash']==r['replay']==saved['hash'] and r['continuation']['same'] and r['hp']==100
 if seed==842:assert r['recruited']
 else:assert 'Stalker guarding' in r['cacheGate']
previous=json.loads(read(repair/'previous-campaign.json'));assert sha(repair/'previous-campaign.zip')==previous['archive_sha256']
with zipfile.ZipFile(repair/'previous-campaign.zip') as z:
 for n,h in previous['files'].items():assert hashlib.sha256(z.read(n)).hexdigest()==h,n
checks=0;experimentSourceDifferences={};experiments=list((repair/'campaign').glob('*.json'));assert len(experiments)==12
for p in experiments:
 r=json.loads(read(p));assert r['config_hash']==identity['config_hash']=='ba11e896' and all(c['pass'] for c in r['checks']);checks+=len(r['checks'])
 assert sha(root/'docs/experiments/campaign'/p.name)==sha(p)
 for n,h in r['source_files'].items():
  if sha(root/n)!=h:
   assert n=='packages/game/src/session.ts' and sha(repair/'experiment-session.ts.txt')==h
   current=read(root/n).replace('log: snapshot ? structuredClone(snapshot.log) : [],','log: snapshot && snapshot.logComplete ? snapshot.log : [],')
   assert current==read(repair/'experiment-session.ts.txt').replace('\r\n','\n')
   experimentSourceDifferences[n]={'experimentSha256':h,'currentSha256':sha(root/n),'difference':'Only preservation of incomplete historical session logs changed after experiment startup. Simulation and harness sources are byte-identical; final full tests and browser separately exercise session resaves.'}
assert checks==78

for n,count in [('test.log',351),('focused.log',29)]:
 log=read(out/n);assert f'# pass {count}' in log and '# fail 0' in log and '# skipped 0' in log,n
assert 'built in' in read(out/'typecheck.log') and not re.search(r'\berror\b',read(out/'lint.log'),re.I)
assert ' matches (compact seed 3' in read(out/'snapshot.log')
assert 'doc tables match' in read(out/'docsync.log') and 'every generated file was made on an ancestor' in read(out/'freshness.log')
assert 'git diff --check exit code: 0' in read(out/'git-diff-check.log')
assert 'Legacy E-variance and preset evidence diff exit code: 0' in read(out/'legacy-evidence.log')
assert 'PASS: four starts and ordinary-command prefixes verified' in read(out/'prepare.log')
assert 'PASS: ten declared fresh review seeds' in read(out/'prepare.log')
settings=json.loads(read(out/'settings.json'));assert settings['config_hash']=='ba11e896' and settings['human_play']=='not_run'
for n in ['fresh','construction','network','turbine']+['fresh-'+str(s) for s in [3,4,5,8,11,13,80,88,102,842]]:assert sha(out/'campaign'/(n+'.json'))==build['served_files'][n+'.json']
b=json.loads(read(out/'browser-result.json'));assert len(b['starts'])==8 and len(b['rows'])==4 and not b['errors']
for r in b['starts']:assert r['speed']==0 and r['complete'] and r['saveReload'] and not r['overflow']
assert sum(r['successful'] for r in b['rows'])==80
for r in b['rows']:assert r['successful']==r['edits'] and r['lightPaints']>0 and r['endSim']>r['startSim']+3
city=json.loads(read(out/'city-browser-result.json'));assert len(city['rows'])==22 and not city['errors']
assert sorted((r['width'],r['seed']) for r in city['rows'] if not r.get('old'))==sorted((w,s) for w in [1366,900] for s in [3,4,5,8,11,13,80,88,102,842])
for r in city['rows']:
 assert r['saveReload']
 if r.get('old'):assert r['before']['log']==96 and not r['before']['complete']
 else:assert r['replay']['same'] and r['cameraPreservesState'] and r['initial']['complete'] and not r['initial']['overflow']
cb=json.loads(read(out/'construction-browser-result.json'));assert len(cb['rows'])==2 and not cb['errors']
for r in cb['rows']:assert r['paused']['work']['phase']=='paused' and r['built']['orders'][-1]['status']=='completed' and r['built']['pockets']==r['paused']['pockets'] and not r['built']['source'] and r['final']['rounds']==40 and r['replay']['same']
probe=json.loads(read(out/'construction-probe.json'));assert probe['rounds']==20 and probe['conservation']['ok'] and probe['fullReplay']
w=json.loads(read(out/'warning-result.json'));assert w['before']['warning']['receivedAt']==2400 and w['cameraControlsPreserveState'] and not w['errors']
rest=json.loads(read(out/'restoration-result.json'));assert len(rest)==2
for r in rest:assert r['spent']==40 and r['replay']['same'] and r['bases']==1
rows={r.split('|')[1].strip():r.split('|') for r in read(root/'docs/PROGRESS.md').splitlines() if r.startswith('| ')}
assert rows['P9-05'][4].strip()=='done'
for n in ['P9-H','RI-02B-UI-01','EX-08H','P6-H','P7-H','P8-H','T18']:assert rows[n][4].strip()=='todo',n
record=read(root/'docs/P9_SESSION_RECORD.md');assert '**Status: NOT RUN.**' in record and 'Phase 9 verdict and reasons: **not recorded**' in record and 'ba11e896' in record
for s in [3,4,5,8,11,13,80,88,102,842]:assert f'| {s} | — | — | — | — | — | — | NOT RUN |' in record
names=['CLAUDE.md','README.md']+['docs/'+n+'.md' for n in ['PROGRAMME_STATE','PROGRESS','DECISIONS','PHASES','STANDARDS','EXPLORATION_DEFENCE_PLAN','P9_05_READINESS_REPORT','P9_SESSION_GUIDE','P9_SESSION_RECORD']]
links=0
for n in names:
 p=root/n
 for ref in re.findall(r'\]\(([^)]+)\)',read(p)):
  if '://' in ref or ref.startswith('#'):continue
  target=(p.parent/ref.split('#')[0].strip('<>')).resolve()
  if target!=out/'verification.json':assert target.exists(),(n,ref)
  links+=1
v={'sourceInputs':205,'productionFiles':6,'servedFiles':21,'fullTests':351,'focusedTests':29,'currentCityBrowserCases':20,'oldSaveBrowserCases':2,'preparedStartCases':8,'liveEdits':80,'populationReused':10000,'campaignChecksReused':checks,'historicalDocumentByteDifferences':documentByteDifferences,'experimentSourceDifferences':experimentSourceDifferences,'human_play':'not_run','localReferences':links,'documents':{n:sha(root/n) for n in names},'evidence':{p.relative_to(out).as_posix():sha(p) for p in out.rglob('*') if p.is_file() and p.name not in ['verification.json','consistency.log','server.log','server-errors.log']}}
(out/'verification.json').write_text(json.dumps(v,indent=2)+'\n',encoding='utf-8',newline='\n')
print(f'PASS: 205 source/6 production, 21 HTTP/archive files; 351 full+29 focused; 20 city+2 historical save+8 prepared cases; both truck/ammo+Turbine, warning, 80 edits; retained 10000 population/78 campaign checks and prior evidence; {links} references; blank record, P9-H available and UI-01 next.')
