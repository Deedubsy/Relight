from pathlib import Path
import hashlib,json,re,zipfile
root=Path(__file__).resolve().parents[3];out=Path(__file__).resolve().parent
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
read=lambda p:p.read_text(encoding='utf-8-sig')
m=json.loads(read(out/'manifest.json'))
for archive,key,base in [('source.zip','source_files',root),('build.zip','build_files',root/'packages/game/dist')]:
 assert sha(out/archive)==m['archives'][archive]
 with zipfile.ZipFile(out/archive) as z:
  assert set(z.namelist())==set(m[key])
  for n,h in m[key].items():assert sha(base/n)==h and hashlib.sha256(z.read(n)).hexdigest()==h,n
documentByteDifferences=[]
for folder,manifest,key,docs in [
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
population=out/'population';identity=json.loads(read(population/'manifest.json'));summary=json.loads(read(population/'summary.json'));analysis=json.loads(read(out/'analysis.json'));comparison=json.loads(read(out/'comparison.json'))
assert identity['seeds']==list(range(1,10001)) and summary['complete'] and summary['attempted']==10000 and summary['sourceUnchanged'] and summary['failed']==0
assert analysis['complete'] and analysis['passed']==10000 and not analysis['failureClasses']
assert comparison['transitions']=={'passed -> passed':9742,'failed -> passed':258} and not comparison['regressions'] and not comparison['baseGeneratorAttemptChanges']
assert len(list(population.glob('seed-*.json')))==10000 and not (population/'run.lock').exists()
assert '10000 attempted seeds' in read(out/'population-integrity.log')
for n,h in identity['sourceFiles'].items():assert sha(root/n)==h,('population source',n)
archive=json.loads(read(out/'population-archive.json'));assert sha(out/'population.zip')==archive['archive_sha256']
with zipfile.ZipFile(out/'population.zip') as z:
 assert set(z.namelist())==set(archive['files'])
 for n,h in archive['files'].items():assert sha(population/n)==h and hashlib.sha256(z.read(n)).hexdigest()==h,n
failed=json.loads(read(out/'failed-seeds-final/summary.json'));assert failed['complete'] and failed['passed']==258 and failed['failed']==0 and failed['sourceUnchanged']
for seed in [3,4,5,8,11,13,80,88,102,842,2313,3610]:
 folder='paid-final' if seed==842 else 'paid'
 r=json.loads(read(out/f'{folder}/seed-{seed}.json'));saved=json.loads(read(out/f'{folder}/save-{seed}.json'))
 assert r['hash']==r['replay']==saved['hash'] and r['conserved'] and r['continuation']['same'] and saved['logComplete']
 assert r['stops']==3 and r['freightDelivered']==5 and r['truck']['order']=='completed' and r['truck']['pocketsUnchanged'] and 'travelling' in r['truck']['phases']
 if seed==842:assert r['extraSalvage']==9
for seed in [3,4,5,8,11,13,80,305,1340,7384,8102,9711]:
 for item in ['steel','copper','coal']:
  r=json.loads(read(out/f'extraction/seed-{seed}-{item}.json'));saved=json.loads(read(out/f'extraction/save-{seed}-{item}.json'))
  assert r['status']=='passed' and r['delivered']>=5 and r['conserved'] and r['continuation']['same'] and r['hash']==r['replay']==saved['hash']
for seed in [88,842]:
 r=json.loads(read(out/f'visit-{seed}.json'));saved=json.loads(read(out/f'visit-save-{seed}.json'))
 assert r['ordinaryWalk'] and r['conserved'] and r['hash']==r['replay']==saved['hash'] and r['continuation']['same'] and r['hp']==100
 if seed==842:assert r['recruited']
 else:assert 'Stalker guarding' in r['cacheGate']
previous=json.loads(read(out/'previous-campaign.json'));assert sha(out/'previous-campaign.zip')==previous['archive_sha256']
with zipfile.ZipFile(out/'previous-campaign.zip') as z:
 for n,h in previous['files'].items():assert hashlib.sha256(z.read(n)).hexdigest()==h,n
checks=0;experimentSourceDifferences={};experiments=list((out/'campaign').glob('*.json'));assert len(experiments)==12
for p in experiments:
 r=json.loads(read(p));assert r['config_hash']==identity['config_hash']=='ba11e896' and all(c['pass'] for c in r['checks']);checks+=len(r['checks'])
 assert sha(root/'docs/experiments/campaign'/p.name)==sha(p)
 for n,h in r['source_files'].items():
  if sha(root/n)!=h:
   assert n=='packages/game/src/session.ts' and sha(out/'experiment-session.ts.txt')==h
   current=read(root/n).replace('log: snapshot ? structuredClone(snapshot.log) : [],','log: snapshot && snapshot.logComplete ? snapshot.log : [],')
   assert current==read(out/'experiment-session.ts.txt').replace('\r\n','\n')
   experimentSourceDifferences[n]={'experimentSha256':h,'currentSha256':sha(root/n),'difference':'Only preservation of incomplete historical session logs changed after experiment startup. Simulation and harness sources are byte-identical; final full tests and browser separately exercise session resaves.'}
assert checks==78
browser=json.loads(read(out/'browser-result.json'));assert len(browser['rows'])==8 and not browser['errors']
for r in browser['rows']:
 assert r['width'] in [900,1366] and not r['initial']['overflow'] and r['initial']['hash']==r['loaded']['hash']
 assert r['initial']['logLength']==r['loaded']['logLength']
 if r['fixture']=='old':assert not r['loaded']['logComplete'] and r['loaded']['survey']==2 and r['loaded']['logLength']==96
 else:assert r['loaded']['logComplete'] and r['loaded']['survey']==3 and r['replay']['same']
for n,count in [('test-final.log',351),('focused-final.log',24)]:
 log=read(out/n);assert f'# pass {count}' in log and '# fail 0' in log and '# skipped 0' in log,n
assert 'built in' in read(out/'typecheck-final.log') and not re.search(r'\berror\b',read(out/'lint.log'),re.I)
assert 'Legacy E-variance and preset evidence diff exit code: 0' in read(out/'legacy-evidence.log')
assert 'git diff --check exit code: 0' in read(out/'git-diff-check.log')
assert 'doc tables match' in read(out/'docsync.log') and 'every generated file was made on an ancestor' in read(out/'freshness.log')
rows={r.split('|')[1].strip():r.split('|') for r in read(root/'docs/PROGRESS.md').splitlines() if r.startswith('| ')}
assert rows['P9-04'][4].strip()=='done' and rows['P9-04R'][4].strip()=='done' and rows['P9-05'][4].strip()=='todo'
for n in ['P9-H','RI-02B-UI-01']:assert rows[n][4].strip()=='blocked'
for n in ['EX-08H','P6-H','P7-H','P8-H','T18']:assert rows[n][4].strip()=='todo'
names=['CLAUDE.md','README.md']+['docs/'+n+'.md' for n in ['PROGRESS','PROGRAMME_STATE','DECISIONS','RELIGHT-design','EXPLORATION_DEFENCE_PLAN','PHASES','STANDARDS','P9_04R_REPAIR_REPORT','CAMPAIGN_RULES']]
links=0
for n in names:
 p=root/n
 for ref in re.findall(r'\]\(([^)]+)\)',read(p)):
  if '://' in ref or ref.startswith('#'):continue
  target=(p.parent/ref.split('#')[0].strip('<>')).resolve()
  if target!=out/'verification.json':assert target.exists(),(n,ref)
  links+=1
assert 'PENDING' not in read(root/'docs/P9_04R_REPAIR_REPORT.md')
v={'sourceInputs':len(m['source_files']),'productionFiles':len(m['build_files']),'localReferences':links,'documents':{n:sha(root/n) for n in names},'evidence':{p.relative_to(out).as_posix():sha(p) for p in out.rglob('*') if p.is_file() and p.name not in ['verification.json','consistency.log','server.log','server-error.log']},'priorEvidence':'P9-04, P9-03, P9-02, P9-01, EX08D, P9-00, P8-05 and EX08C preserved','historicalDocumentByteDifferences':documentByteDifferences,'experimentSourceDifferences':experimentSourceDifferences,'populationFailures':summary['failed'],'populationPasses':summary['passed'],'human_play':'not_run'}
(out/'verification.json').write_text(json.dumps(v,indent=2)+'\n',encoding='utf-8',newline='\n')
print(f'PASS: {len(m["source_files"])} source inputs / {len(m["build_files"])} production files; 351 full + 24 focused tests; 10000/10000 retained seeds; all 258 baseline failures repaired; 12 paid routes, 36 extraction probes, 78 experiment checks, 8 browser cases; {links} references; historical evidence preserved; P9-05 next; human gates open.')
