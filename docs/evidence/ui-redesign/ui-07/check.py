from pathlib import Path
import hashlib,json,zipfile,re,urllib.request
root=Path(__file__).resolve().parents[4];out=Path(__file__).resolve().parent
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
read=lambda p:p.read_text(encoding='utf-8-sig')
m=json.loads(read(out/'manifest.json'));prior=out.parent/'ui-06';old=json.loads(read(prior/'manifest.json'))
assert len(m['source_files'])==219 and len(m['changes'])==5 and len(m['build_files'])==9
for n,h in m['source_files'].items():assert sha(root/n)==h,n
assert set(m['changes'])=={'packages/game/src/uiShell.ts','packages/game/src/hud.ts','packages/game/src/main.ts','packages/game/src/style.css','packages/game/src/worldScene.ts'}
for n,h in old['source_files'].items():
 if n.startswith(('packages/sim/','packages/harness/')) or n=='packages/game/src/session.ts':assert m['source_files'][n]==h,n
for archive,key,base in [('source.zip','source_files',root),('build.zip','build_files',root/'packages/game/dist')]:
 assert sha(out/archive)==m['archives'][archive]
 with zipfile.ZipFile(out/archive) as z:
  assert set(z.namelist())==set(m[key])
  for n,h in m[key].items():assert sha(base/n)==h and hashlib.sha256(z.read(n)).hexdigest()==h,n
for port in [5178,5182]:
 for n,h in m['build_files'].items():
  with urllib.request.urlopen(f'http://127.0.0.1:{port}/'+n,timeout=10) as r:assert hashlib.sha256(r.read()).hexdigest()==h,(port,n)
for n in ['fresh','construction','network']:assert m['build_files']['ui-'+n+'.json']==sha(root/'docs/evidence/p9-05-2026-09-09/campaign'/(n+'.json'))
v=json.loads(read(prior/'verification.json'))
for n,h in v['evidence'].items():assert sha(prior/n)==h,('UI-06',n)
for folder,docs in [('p9-05-2026-09-09',['P9_05_READINESS_REPORT','P9_SESSION_GUIDE','P9_SESSION_RECORD']),('p9-04r-2026-09-09',['P9_04R_REPAIR_REPORT'])]:
 p=root/'docs/evidence'/folder;v=json.loads(read(p/'verification.json'))
 for n,h in v['evidence'].items():assert sha(p/n)==h,(folder,n)
 for n in docs:assert sha(root/'docs'/(n+'.md'))==v['documents']['docs/'+n+'.md'],n
ex=root/'docs/evidence/ex08d-2026-09-08';v=json.loads(read(ex/'verification-manifest.json'))
for n,h in v['verification_files'].items():assert sha(ex/n)==h,('EX08D',n)
for n in ['EX08D_PREPARATION_REPORT','EX08D_SESSION_GUIDE','EX08D_SESSION_RECORD']:assert sha(root/'docs'/(n+'.md'))==v['documents']['docs/'+n+'.md']
for name,count in [('test.log',376),('focused.log',17)]:
 log=read(out/name);assert f'# pass {count}' in log and '# fail 0' in log and '# skipped 0' in log
for name in ['typecheck.log','game-build-final.log']:assert 'built in' in read(out/name) and 'error TS' not in read(out/name)
assert not re.search(r'\berror\b',read(out/'lint.log'),re.I)
assert 'doc tables match' in read(out/'docsync.log') and 'every generated file was made on an ancestor' in read(out/'freshness.log')
assert 'git diff --check exit code: 0' in read(out/'git-diff-check.log')
results={}
for name,count in [('matrix',8),('warnings',20),('inspection-browser',3),('regression-browser',2),('building',2),('projects',1),('frozen',3)]:
 b=json.loads(read(out/(name+'-result.json')));assert len(b['rows'])==count and not b['errors'],name;results[name]=b
 for row in b['rows']:
  if 'replay' in row:assert row['replay']['same'],(name,row)
for row in results['warnings']['rows']:
 g=row['geometry'];assert g['playerVisible'] and g['closeClear'] and not g['overflow'];assert g['warning']['right']<=g['drawer']['x'] and g['warning']['bottom']<g['engineer']['y']
for row in results['matrix']['rows']:assert row['inputIsolation'] and row['settingsHashUnchanged'] and row['cameraZoomUnchanged']
contrast=results['regression-browser']['contrast'];assert min(x['ratio'] for x in contrast['text'])>=4.5 and min(x['ratio'] for x in contrast['borders'])>=3
assert results['frozen']['identityFreshAndClearedAfterWalking'] and results['frozen']['paidReplay']
before=json.loads(read(out/'before-result.json'));after=json.loads(read(out/'after-result.json'))
assert before[0]['hash']==after[0]['hash']=='e13e92a4' and before[0]['zoom']==after[0]['zoom']==.65
for n in ['frozen-opening-1280.png','building-placement-1280.png','inspection-stalled-1280.png','inspection-working-1280.png','projects-service-1280.png','warning-raid-1366.png','warning-raid-3440.png']:assert (out/n).stat().st_size>1000
tasklines=[line.split('|') for line in read(root/'docs/PROGRESS.md').splitlines() if re.match(r'^\| (?:RI-|EX-|P\d|T\d)',line)]
ids=[line[1].strip() for line in tasklines];assert len(ids)==len(set(ids)),'Duplicate task IDs'
rows={line[1].strip():line for line in tasklines}
for i in range(1,8):assert rows[f'RI-02B-UI-0{i}'][4].strip()=='done'
for n in ['P9-H','P6-H','P7-H','P8-H','EX-08H','T18']:assert rows[n][4].strip()=='todo'
assert rows['EX-08'][4].strip()=='blocked' and 'EX-08H' in rows['EX-08'][5]
for i in range(2,8):
 for dep in rows[f'RI-02B-UI-0{i}'][5].split(';'):assert dep.strip() in rows and rows[dep.strip()][4].strip()=='done'
names=['CLAUDE.md','README.md']+['docs/'+n+'.md' for n in ['PROGRAMME_STATE','PROGRESS','DECISIONS','RELIGHT-design','EXPLORATION_DEFENCE_PLAN','PHASES','STANDARDS','RI-02B_UI_SPEC']]+['docs/evidence/ui-redesign/README.md']
links=0
for n in names:
 p=root/n
 for ref in re.findall(r'\]\(([^)]+)\)',read(p)):
  if '://' in ref or ref.startswith('#'):continue
  target=(p.parent/ref.split('#')[0].strip('<>')).resolve()
  if target!=out/'verification.json':assert target.exists(),(n,ref)
  links+=1
assert 'Pending — no independent participant observed' in read(root/'docs/evidence/ui-redesign/README.md')
result={'status':'UI-07 engineering and developer visual review complete; independent fresh-player feedback pending','sourceInputs':219,'changedFiles':m['changes'],'productionFiles':6,'preparedFixtures':3,'fullTests':376,'focusedTests':17,'scaleCases':8,'urgentCases':20,'frozenPort':5182,'localReferences':links,'uniqueTaskIds':len(ids),'priorEvidence':'UI-06, P9-05, P9-04R and EX-08D preserved','documents':{n:sha(root/n) for n in names},'evidence':{p.relative_to(out).as_posix():sha(p) for p in out.rglob('*') if p.is_file() and p.name not in ['verification.json','consistency.log']}}
(out/'verification.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8',newline='\n')
print(f'PASS: 219 sources / 5 game changes / 9 archived files on ports 5178 and 5182; 376 full + 17 focused; eight scales / 20 urgent cases / paid walkthroughs; {links} references and {len(ids)} unique task IDs; frozen evidence and human gates preserved.')
