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
summary=json.loads(read(out/'initial-survey/summary.json'));identity=json.loads(read(out/'initial-survey/manifest.json'))
assert summary['sourceUnchanged'] and summary['passed']==32 and summary['failed']==0 and summary['attempted']==32
assert summary['config_hash']=='9fc92bfc'
for n,h in identity['sourceFiles'].items():assert sha(root/n)==h,('sweep source',n)
for seed in range(1,33):
 row=json.loads(read(out/f'initial-survey/seed-{seed}.json'))
 assert row['seed']==seed and row['unchanged'] and row['deterministic'] and row['loadSame'] and row['status']=='passed'
 assert row['report']['surveyVersion']==2 and not row['report']['failures']
for seed in [3,4,5,6,8,11,13,29]:
 r=json.loads(read(out/f'paid/seed-{seed}.json'));saved=json.loads(read(out/f'paid/save-{seed}.json'))
 assert r['hash']==r['replay']==saved['hash'] and r['conserved'] and r['continuation']['same']
 assert r['stops']==3 and r['freightDelivered']==5 and r['truck']['order']=='completed' and r['truck']['pocketsUnchanged']
 assert all(p in r['truck']['phases'] for p in ['loading','travelling','building','complete'])
 assert saved['logComplete'] and saved['state']['campaign']['expansion']['surveyVersion']==2
for seed in [6,29]:
 oldsave=json.loads(read(out/f'old-seed-{seed}.json'));reproduction=json.loads(read(root/f'docs/evidence/p9-01-2026-09-08/reproduction-{seed}.json'))
 assert oldsave['hash']==reproduction['hash'] and oldsave['log']==reproduction['log'] and 'surveyVersion' not in oldsave['state']['campaign']['expansion']
previous=json.loads(read(out/'previous-campaign.json'));assert sha(out/'previous-campaign.zip')==previous['archive_sha256']
with zipfile.ZipFile(out/'previous-campaign.zip') as z:
 for n,h in previous['files'].items():assert hashlib.sha256(z.read(n)).hexdigest()==h,n
checks=0;experimentSourceDifferences={}
for p in (out/'campaign').glob('*.json'):
 r=json.loads(read(p));assert r['config_hash']=='9fc92bfc' and all(c['pass'] for c in r['checks']);checks+=len(r['checks'])
 assert sha(root/'docs/experiments/campaign'/p.name)==sha(p)
 for n,h in r['source_files'].items():
  if sha(root/n)!=h:
   assert n=='packages/tools/src/campaign-docsync.ts',('experiment runtime source',n)
   with zipfile.ZipFile(root/'docs/evidence/p9-01-2026-09-08/source.zip') as z:assert hashlib.sha256(z.read(n)).hexdigest()==h
   experimentSourceDifferences[n]={'experimentSha256':h,'currentSha256':sha(root/n),'difference':'Documentation generator only: survey reference row added after experiments; runtime sim/harness/game inputs unchanged; original generator preserved in P9-01 source archive'}
assert checks==78
browser=json.loads(read(out/'browser-result.json'));assert len(browser['rows'])==4 and not browser['errors']
for r in browser['rows']:
 assert r['saveReload'] and r['speed']==0 and r['complete']==(r['name']=='new')
 assert (r['replay'].get('same') if r['name']=='new' else bool(r['replay'].get('error')))
for n,count in [('test.log',331),('focused.log',16)]:
 log=read(out/n);assert f'# pass {count}' in log and '# fail 0' in log and '# skipped 0' in log,n
assert 'built in' in read(out/'typecheck-verified.log') and not re.search(r'\berror\b',read(out/'lint-verified.log'),re.I)
assert 'doc tables match' in read(out/'docsync.log') and 'every generated file was made on an ancestor' in read(out/'freshness.log')
rows={r.split('|')[1].strip():r.split('|') for r in read(root/'docs/PROGRESS.md').splitlines() if r.startswith('| ')}
assert rows['P9-02'][4].strip()=='done' and rows['P9-03'][4].strip()=='todo'
for n in ['P9-04','P9-05','P9-H','RI-02B-UI-01']:assert rows[n][4].strip()=='blocked'
for n in ['EX-08H','P6-H','P7-H','P8-H','T18']:assert rows[n][4].strip()=='todo'
names=['CLAUDE.md','README.md']+['docs/'+n+'.md' for n in ['PROGRESS','PROGRAMME_STATE','DECISIONS','RELIGHT-design','EXPLORATION_DEFENCE_PLAN','PHASES','STANDARDS','P9_02_SURVEY_RELIABILITY_REPORT','CAMPAIGN_RULES']]
links=0
for n in names:
 p=root/n
 for ref in re.findall(r'\]\(([^)]+)\)',read(p)):
  if '://' in ref or ref.startswith('#'):continue
  target=(p.parent/ref.split('#')[0].strip('<>')).resolve()
  if target!=out/'verification.json':assert target.exists(),(n,ref)
  links+=1
assert 'PENDING' not in read(root/'docs/P9_02_SURVEY_RELIABILITY_REPORT.md')
v={'sourceInputs':len(m['source_files']),'productionFiles':len(m['build_files']),'localReferences':links,'documents':{n:sha(root/n) for n in names},'evidence':{p.relative_to(out).as_posix():sha(p) for p in out.rglob('*') if p.is_file() and p.name not in ['verification.json','consistency.log','server.log','server-error.log']},'priorEvidence':'P9-01, EX08D, P9-00, P8-05 and EX08C preserved','historicalDocumentByteDifferences':documentByteDifferences,'experimentSourceDifferences':experimentSourceDifferences,'human_play':'not_run'}
(out/'verification.json').write_text(json.dumps(v,indent=2)+'\n',encoding='utf-8',newline='\n')
print(f'PASS: {len(m["source_files"])} source inputs / {len(m["build_files"])} production files; 331 full + 16 focused tests; 32/32 seeds; 8 ordinary paid routes; 78 refreshed experiment checks; 4 browser save/replay cases; {links} references; historical evidence preserved; P9-03 next and human gates open.')
