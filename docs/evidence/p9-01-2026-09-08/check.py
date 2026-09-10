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
for folder,manifest,key,docs in [
 ('ex08d-2026-09-08','verification-manifest.json','verification_files',['EX08D_PREPARATION_REPORT','EX08D_SESSION_GUIDE','EX08D_SESSION_RECORD']),
 ('phase9-review-2026-09-08','manifest.json','verification_files',['PHASE_9_SCOPE_REPORT']),
 ('p8-05-2026-09-08','verification.json','evidence',['P8_05_REVIEW_REPORT']),
 ('ex08c-2026-09-08','verification-manifest.json','verification_files',['EX08C_PREPARATION_REPORT','EX08C_SESSION_GUIDE','EX08C_SESSION_RECORD'])]:
 prior=root/'docs/evidence'/folder;v=json.loads(read(prior/manifest))
 for n,h in v[key].items():assert sha(prior/n)==h,(folder,n)
 for n in docs:assert sha(root/'docs'/(n+'.md'))==v['documents']['docs/'+n+'.md'],n
review=json.loads(read(root/'docs/evidence/phase9-review-2026-09-08/working-tree-difference.json'))
assert sha(root/review['path'])==review['currentSha256'],'unrelated fixture changed again'
summary=json.loads(read(out/'campaign/summary.json'));identity=json.loads(read(out/'campaign/manifest.json'))
assert summary['sourceUnchanged'] and summary['passed']==30 and summary['failed']==2 and summary['attempted']==32
for n,h in identity['sourceFiles'].items():assert sha(root/n)==h,('sweep source',n)
for seed in range(1,33):
 row=json.loads(read(out/f'campaign/seed-{seed}.json'))
 assert row['seed']==seed and row['unchanged'] and row['deterministic'] and row['loadSame']
 assert row['sourceDigest']==identity['sourceDigest']
 assert (row['status']=='failed')==(seed in [6,29])
 if seed in [6,29]:assert len(row['report']['failures'])==(1 if seed==6 else 35) and all(f['code']=='rail-placement' and f['reason']=='not buildable ground' for f in row['report']['failures'])
for seed in [6,29]:
 r=json.loads(read(out/f'reproduction-{seed}.json'));assert r['hash']==r['replay'] and r['conserved'] and r['refusal']=='not buildable ground'
for n,count in [('full-test.log',327),('focused-final.log',7)]:
 log=read(out/n);assert f'# pass {count}' in log and '# fail 0' in log and '# skipped 0' in log,n
assert 'output directory exists' in read(out/'existing-output-refusal.log')
assert 'built in' in read(out/'typecheck-final.log') and not re.search(r'\berror\b',read(out/'lint.log'),re.I)
assert 'doc tables match' in read(out/'docsync.log') and 'every generated file was made on an ancestor' in read(out/'freshness.log')
rows={r.split('|')[1].strip():r.split('|') for r in read(root/'docs/PROGRESS.md').splitlines() if r.startswith('| ')}
assert rows['P9-01'][4].strip()=='done' and rows['P9-02'][4].strip()=='todo'
for n in ['P9-03','P9-04','P9-05','P9-H']:assert rows[n][4].strip()=='blocked'
for n in ['EX-08H','P6-H','P7-H','P8-H','T18']:assert rows[n][4].strip()=='todo'
names=['CLAUDE.md','README.md']+['docs/'+n+'.md' for n in ['PROGRESS','PROGRAMME_STATE','DECISIONS','RELIGHT-design','EXPLORATION_DEFENCE_PLAN','PHASES','STANDARDS','P9_01_CITY_VALIDATION_REPORT']]
links=0
for n in names:
 p=root/n
 for ref in re.findall(r'\]\(([^)]+)\)',read(p)):
  if '://' in ref or ref.startswith('#'):continue
  target=(p.parent/ref.split('#')[0].strip('<>')).resolve()
  if target!=out/'verification.json':assert target.exists(),(n,ref)
  links+=1
v={'sourceInputs':len(m['source_files']),'productionFiles':len(m['build_files']),'localReferences':links,'documents':{n:sha(root/n) for n in names},'evidence':{p.relative_to(out).as_posix():sha(p) for p in out.rglob('*') if p.is_file() and p.name not in ['verification.json','consistency.log']},'priorEvidence':'EX08D, P9-00, P8-05 and EX08C preserved','human_play':'not_run'}
(out/'verification.json').write_text(json.dumps(v,indent=2)+'\n',encoding='utf-8',newline='\n')
print(f'PASS: {len(m["source_files"])} source inputs / {len(m["build_files"])} production files and archives; 327 full + 7 focused tests; 30/32 valid seeds and two retained paid-reproduced failures; {links} references; prior evidence preserved; P9-02 next and human gates open.')
