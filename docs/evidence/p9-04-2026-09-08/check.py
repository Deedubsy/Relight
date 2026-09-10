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
population=out/'population';identity=json.loads(read(population/'manifest.json'));summary=json.loads(read(population/'summary.json'));analysis=json.loads(read(out/'analysis.json'))
assert identity['seeds']==list(range(1,10001)) and summary['complete'] and summary['attempted']==10000 and summary['sourceUnchanged']
assert analysis['complete'] and analysis['attempted']==10000 and analysis['failed']==summary['failed']
assert len(list(population.glob('seed-*.json')))==10000
assert '10000 attempted seeds' in read(out/'population-integrity.log')
for n,h in identity['sourceFiles'].items():assert sha(root/n)==h,('population source',n)
assert not (population/'run.lock').exists()
archive=json.loads(read(out/'population-archive.json'));assert sha(out/'population.zip')==archive['archive_sha256']
with zipfile.ZipFile(out/'population.zip') as z:
 assert set(z.namelist())==set(archive['files'])
 for n,h in archive['files'].items():assert sha(population/n)==h and hashlib.sha256(z.read(n)).hexdigest()==h,n
for cls in analysis['failureClasses'].values():
 rep=cls['representative']
 if rep['code']=='generation-or-run':
  r=json.loads(read(out/f"generation-failures/seed-{rep['seed']}.json"));assert r['ordinaryCommandsExecuted']==0
  assert len(r['attempts'])==2 and all(a['failure']==rep['reason'] for a in r['attempts'])
 else:assert rep['code']=='rail-placement' and rep['reason']=='rubble in the way','new failure class needs a probe'
for seed in analysis['retryIndexNeedsGenerationDiagnostic']:
 r=json.loads(read(out/f'retry-metadata/seed-{seed}.json'));assert r['seed']==seed and r['sourceDigest']==identity['sourceDigest']
 assert r['generatorAttempt'] is not None or r['baseFailure']
for seed in [3,4,5,8,11,13]:
 r=json.loads(read(out/f'paid/seed-{seed}.json'));saved=json.loads(read(out/f'paid/save-{seed}.json'))
 assert r['hash']==r['replay']==saved['hash'] and r['conserved'] and r['continuation']['same'] and saved['logComplete']
 assert r['stops']==3 and r['freightDelivered']==5 and r['truck']['order']=='completed' and r['truck']['pocketsUnchanged']
 for item in ['steel','copper','coal']:
  r=json.loads(read(out/f'extraction/seed-{seed}-{item}.json'));saved=json.loads(read(out/f'extraction/save-{seed}-{item}.json'))
  assert r['status']=='passed' and r['delivered']>=5 and r['conserved'] and r['continuation']['same'] and r['hash']==r['replay']==saved['hash']
r=json.loads(read(out/'paid-rubble/seed-102.json'));saved=json.loads(read(out/'paid-rubble/save-102.json'))
assert r['hash']==r['replay']==saved['hash'] and r['conserved'] and r['continuation']['same'] and r['truck']['order']=='completed'
assert any(p['mined']==300 and p['refusalConserved'] for p in r['clearedFootprints'])
assert not r['freightPass'] and r['freightDelivered']==0
assert {b['block'] for b in saved['state']['campaign']['defence']['bases'] if b['hp']==0}=={365,351}
stall=json.loads(read(out/'mining-stall.json'));assert stall['stacks']==40 and stall['hand']['full'] and stall['hash']==stall['replay'] and stall['conservation']['ok']
# The game and every simulation source are identical to P9-03; only runner modules/tests changed.
prior=json.loads(read(root/'docs/evidence/p9-03-2026-09-08/manifest.json'))
assert m['build_files']==prior['build_files']
for n,h in prior['source_files'].items():
 if n.startswith(('packages/sim/src/','packages/game/')):assert sha(root/n)==h,n
for p in (root/'docs/evidence/p9-03-2026-09-08/campaign').glob('*.json'):assert sha(p)==sha(root/'docs/experiments/campaign'/p.name)
interrupted=json.loads(read(out/'interruption.json'));assert interrupted['retainedBytesUnchanged'] and interrupted['sourceUnchanged'] and interrupted['passed']==6 and interrupted['interruptedExit']!=0
resume=json.loads(read(out/'resume-check/summary.json'));assert resume['complete'] and resume['passed']==2 and resume['sourceUnchanged']
assert 'PARTIAL: 1/2' in read(out/'resume-first.log') and '2/2: seed 4 passed' in read(out/'resume-second.log')
for n,count in [('test.log',338),('focused.log',9)]:
 log=read(out/n);assert f'# pass {count}' in log and '# fail 0' in log and '# skipped 0' in log,n
assert 'built in' in read(out/'typecheck.log') and not re.search(r'\berror\b',read(out/'lint.log'),re.I)
assert 'Legacy E-variance and preset evidence diff exit code: 0' in read(out/'legacy-evidence.log')
assert all(r['exitCode']!=0 and r['retainedRowsUnchanged'] for r in json.loads(read(out/'cli-refusals.json')))
assert 'git diff --check exit code: 0' in read(out/'git-diff-check.log')
assert 'doc tables match' in read(out/'docsync.log') and 'every generated file was made on an ancestor' in read(out/'freshness.log')
rows={r.split('|')[1].strip():r.split('|') for r in read(root/'docs/PROGRESS.md').splitlines() if r.startswith('| ')}
assert rows['P9-04'][4].strip()=='done' and rows['P9-04R'][4].strip()=='todo'
for n in ['P9-05','P9-H','RI-02B-UI-01']:assert rows[n][4].strip()=='blocked'
for n in ['EX-08H','P6-H','P7-H','P8-H','T18']:assert rows[n][4].strip()=='todo'
names=['CLAUDE.md','README.md']+['docs/'+n+'.md' for n in ['PROGRESS','PROGRAMME_STATE','DECISIONS','RELIGHT-design','EXPLORATION_DEFENCE_PLAN','PHASES','STANDARDS','P9_04_POPULATION_REPORT','CAMPAIGN_RULES']]
links=0
for n in names:
 p=root/n
 for ref in re.findall(r'\]\(([^)]+)\)',read(p)):
  if '://' in ref or ref.startswith('#'):continue
  target=(p.parent/ref.split('#')[0].strip('<>')).resolve()
  if target!=out/'verification.json':assert target.exists(),(n,ref)
  links+=1
assert 'PENDING' not in read(root/'docs/P9_04_POPULATION_REPORT.md')
v={'sourceInputs':len(m['source_files']),'productionFiles':len(m['build_files']),'localReferences':links,'documents':{n:sha(root/n) for n in names},'evidence':{p.relative_to(out).as_posix():sha(p) for p in out.rglob('*') if p.is_file() and p.name not in ['verification.json','consistency.log','server.log','server-error.log']},'priorEvidence':'P9-03, P9-02, P9-01, EX08D, P9-00, P8-05 and EX08C preserved','historicalDocumentByteDifferences':documentByteDifferences,'populationFailures':summary['failed'],'populationPasses':summary['passed'],'human_play':'not_run'}
(out/'verification.json').write_text(json.dumps(v,indent=2)+'\n',encoding='utf-8',newline='\n')
print(f'PASS: {len(m["source_files"])} source inputs / {len(m["build_files"])} production files; 338 full + 9 focused tests; 10000 retained seeds; 6 paid routes and 18 extraction probes; {links} references; historical evidence preserved; population validation retains failures; P9-04R remediation next; human gates open.')
