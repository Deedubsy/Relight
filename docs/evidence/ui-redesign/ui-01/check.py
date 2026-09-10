from pathlib import Path
import hashlib,json,zipfile,re,urllib.request
root=Path(__file__).resolve().parents[4];out=Path(__file__).resolve().parent
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
read=lambda p:p.read_text(encoding='utf-8-sig')
m=json.loads(read(out/'manifest.json'));prior=root/'docs/evidence/p9-05-2026-09-09';old=json.loads(read(prior/'build-manifest.json'))
assert len(m['source_files'])==206 and len(m['changes'])==10 and len(m['build_files'])==9
for n,h in m['source_files'].items():assert sha(root/n)==h,n
assert all(n.startswith('packages/game/') for n in m['changes'])
for n,h in old['source_files'].items():
 if not n.startswith('packages/game/'):assert m['source_files'][n]==h,n
assert m['source_files']['packages/game/src/session.ts']==old['source_files']['packages/game/src/session.ts']
for archive,key,base in [('source.zip','source_files',root),('build.zip','build_files',root/'packages/game/dist')]:
 assert sha(out/archive)==m['archives'][archive]
 with zipfile.ZipFile(out/archive) as z:
  assert set(z.namelist())==set(m[key])
  for n,h in m[key].items():assert sha(base/n)==h and hashlib.sha256(z.read(n)).hexdigest()==h,n
for n,h in m['build_files'].items():
 with urllib.request.urlopen('http://127.0.0.1:5178/'+n,timeout=10) as r:assert hashlib.sha256(r.read()).hexdigest()==h,('HTTP',n)
for n in ['fresh','construction','network']:assert m['build_files']['ui-'+n+'.json']==sha(prior/'campaign'/(n+'.json'))
for folder,docs in [('p9-05-2026-09-09',['P9_05_READINESS_REPORT','P9_SESSION_GUIDE','P9_SESSION_RECORD']),('p9-04r-2026-09-09',['P9_04R_REPAIR_REPORT'])]:
 p=root/'docs/evidence'/folder;v=json.loads(read(p/'verification.json'))
 for n,h in v['evidence'].items():assert sha(p/n)==h,(folder,n)
 for n in docs:assert sha(root/'docs'/(n+'.md'))==v['documents']['docs/'+n+'.md'],n
ex=root/'docs/evidence/ex08d-2026-09-08';v=json.loads(read(ex/'verification-manifest.json'))
for n,h in v['verification_files'].items():assert sha(ex/n)==h,('EX08D',n)
for n in ['EX08D_PREPARATION_REPORT','EX08D_SESSION_GUIDE','EX08D_SESSION_RECORD']:assert sha(root/'docs'/(n+'.md'))==v['documents']['docs/'+n+'.md']
log=read(out/'test.log');assert '# pass 351' in log and '# fail 0' in log and '# skipped 0' in log
assert 'built in' in read(out/'typecheck.log') and 'built in' in read(out/'game-build-final.log')
assert not re.search(r'\berror\b',read(out/'lint.log'),re.I)
assert 'doc tables match' in read(out/'docsync.log') and 'every generated file was made on an ancestor' in read(out/'freshness.log')
assert 'git diff --check exit code: 0' in read(out/'git-diff-check.log')
b=json.loads(read(out/'browser-result.json'));assert len(b['rows'])==2 and not b['errors'];assert {r['width'] for r in b['rows']}=={900,1366}
for r in b['rows']:
 for k in ['typing','wheel','oneDrawer','escapeHierarchy','modalFocus','heldMovementReleased','dragCancelled','saveReload']:assert r[k],k
 assert r['replay']['same'] and r['resumeSpeed']==4
assert min(r['ratio'] for r in b['contrast']['text'])>=4.5 and min(r['ratio'] for r in b['contrast']['borders'])>=3
ad=json.loads(read(out/'adapters-result.json'));assert not ad['errors'] and {r['case'] for r in ad['rows']}=={'existing-adapters','new-city','blur','legacy','missing-save'}
assert ad['rows'][0]['drawerSaveReload'] and ad['rows'][0]['inspection'] and ad['rows'][-1]['preservedStorage']
for n in ['opening-1280.png','build-900.png','pause-1366.png','missing-save.png']:assert (out/n).stat().st_size>1000
rows={r.split('|')[1].strip():r.split('|') for r in read(root/'docs/PROGRESS.md').splitlines() if r.startswith('| ')}
assert rows['RI-02B-UI-01'][4].strip()=='done' and rows['RI-02B-UI-02'][4].strip()=='todo'
for n in ['RI-02B-UI-03','RI-02B-UI-04','RI-02B-UI-05','RI-02B-UI-06','RI-02B-UI-07']:assert rows[n][4].strip()=='blocked',n
for n in ['P9-H','P6-H','P7-H','P8-H','EX-08H','T18']:assert rows[n][4].strip()=='todo',n
names=['CLAUDE.md','README.md']+['docs/'+n+'.md' for n in ['PROGRAMME_STATE','PROGRESS','DECISIONS','RELIGHT-design','EXPLORATION_DEFENCE_PLAN','PHASES','STANDARDS','RI-02B_UI_SPEC']]+['docs/evidence/ui-redesign/README.md']
links=0
for n in names:
 p=root/n
 for ref in re.findall(r'\]\(([^)]+)\)',read(p)):
  if '://' in ref or ref.startswith('#'):continue
  target=(p.parent/ref.split('#')[0].strip('<>')).resolve()
  if target!=out/'verification.json':assert target.exists(),(n,ref)
  links+=1
result={'status':'UI-01 implementation and automated checks complete; human play not_run','sourceInputs':206,'changedUiFiles':m['changes'],'productionFiles':6,'preparedFixtures':3,'fullTests':351,'browserWidths':[1366,900],'adapterCases':5,'normalTextMinimumContrast':min(r['ratio'] for r in b['contrast']['text']),'controlBorderMinimumContrast':min(r['ratio'] for r in b['contrast']['borders']),'localReferences':links,'priorEvidence':'P9-05, P9-04R and EX-08D preserved','documents':{n:sha(root/n) for n in names},'evidence':{p.relative_to(out).as_posix():sha(p) for p in out.rglob('*') if p.is_file() and p.name not in ['verification.json','consistency.log']}}
(out/'verification.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8',newline='\n')
print(f'PASS: 206 source inputs / 10 UI changes / 9 archived HTTP files; sim/harness/session unchanged; 351 tests, final build/lint/docs; both-width input/save/replay/contrast and 5 adapter cases; {links} references; protected checkpoints intact; UI-02 next, human gates open.')
