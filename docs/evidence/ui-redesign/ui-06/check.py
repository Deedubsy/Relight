from pathlib import Path
import hashlib,json,zipfile,re,urllib.request
root=Path(__file__).resolve().parents[4];out=Path(__file__).resolve().parent
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
read=lambda p:p.read_text(encoding='utf-8-sig')
m=json.loads(read(out/'manifest.json'));prior=out.parent/'ui-05';old=json.loads(read(prior/'manifest.json'))
assert len(m['source_files'])==219 and len(m['changes'])==12 and len(m['build_files'])==9
for n,h in m['source_files'].items():assert sha(root/n)==h,n
for n,h in old['source_files'].items():
 if n.startswith(('packages/sim/src/','packages/harness/')) or n=='packages/game/src/session.ts':assert m['source_files'][n]==h,n
for archive,key,base in [('source.zip','source_files',root),('build.zip','build_files',root/'packages/game/dist')]:
 assert sha(out/archive)==m['archives'][archive]
 with zipfile.ZipFile(out/archive) as z:
  assert set(z.namelist())==set(m[key])
  for n,h in m[key].items():assert sha(base/n)==h and hashlib.sha256(z.read(n)).hexdigest()==h,n
for n,h in m['build_files'].items():
 with urllib.request.urlopen('http://127.0.0.1:5178/'+n,timeout=10) as r:assert hashlib.sha256(r.read()).hexdigest()==h,('HTTP',n)
for n in ['fresh','construction','network']:assert m['build_files']['ui-'+n+'.json']==sha(root/'docs/evidence/p9-05-2026-09-09/campaign'/(n+'.json'))
v=json.loads(read(prior/'verification.json'))
for n,h in v['evidence'].items():assert sha(prior/n)==h,('UI-05',n)
for folder,docs in [('p9-05-2026-09-09',['P9_05_READINESS_REPORT','P9_SESSION_GUIDE','P9_SESSION_RECORD']),('p9-04r-2026-09-09',['P9_04R_REPAIR_REPORT'])]:
 p=root/'docs/evidence'/folder;v=json.loads(read(p/'verification.json'))
 for n,h in v['evidence'].items():assert sha(p/n)==h,(folder,n)
 for n in docs:assert sha(root/'docs'/(n+'.md'))==v['documents']['docs/'+n+'.md'],n
ex=root/'docs/evidence/ex08d-2026-09-08';v=json.loads(read(ex/'verification-manifest.json'))
for n,h in v['verification_files'].items():assert sha(ex/n)==h,('EX08D',n)
for n in ['EX08D_PREPARATION_REPORT','EX08D_SESSION_GUIDE','EX08D_SESSION_RECORD']:assert sha(root/'docs'/(n+'.md'))==v['documents']['docs/'+n+'.md']
for name,count in [('test.log',376),('focused-final.log',17)]:
 log=read(out/name);assert f'# pass {count}' in log and '# fail 0' in log and '# skipped 0' in log
assert 'built in' in read(out/'typecheck-final.log') and not re.search(r'error TS',read(out/'typecheck-final.log'))
assert not re.search(r'\berror\b',read(out/'lint.log'),re.I)
assert 'doc tables match' in read(out/'docsync.log') and 'every generated file was made on an ancestor' in read(out/'freshness.log')
assert 'git diff --check exit code: 0' in read(out/'git-diff-check.log')
results={}
for name,count in [('matrix',8),('preferences',3),('integration',4),('accessibility',4),('inspection-browser',6),('regression-browser',2),('building',4),('projects',2)]:
 b=json.loads(read(out/(name+'-result.json')));assert len(b['rows'])==count and not b['errors'],name;results[name]=b
 for row in b['rows']:
  if 'replay' in row and name!='accessibility':assert row['replay']['same'],(name,row)
for row in results['matrix']['rows']:
 assert row['settingsHashUnchanged'] and row['cameraZoomUnchanged'] and row['inputIsolation']
 assert row['build']['font']==17*row['scale']/100
contrast=results['regression-browser']['contrast'];assert min(x['ratio'] for x in contrast['text'])>=4.5 and min(x['ratio'] for x in contrast['borders'])>=3
assert results['integration']['rows'][0]['manualCompletion'] and results['integration']['rows'][0]['truckStartSourceRetryPauseResumeStop']
oldsave=results['accessibility']['rows'][2];assert oldsave['hash']=='d14fa9bc' and oldsave['replay']['error']=='a snapshot session without its command log does not replay (scenario B)'
for n in ['settings-1366-150.png','projects-1366-150.png','manual-order.png','projects-3440-150.png','legacy-management.png']:assert (out/n).stat().st_size>1000
rows={line.split('|')[1].strip():line.split('|') for line in read(root/'docs/PROGRESS.md').splitlines() if line.startswith('| ')}
for i in range(1,7):assert rows[f'RI-02B-UI-0{i}'][4].strip()=='done'
assert rows['RI-02B-UI-07'][4].strip()=='todo'
for n in ['P9-H','P6-H','P7-H','P8-H','EX-08H','T18']:assert rows[n][4].strip()=='todo'
names=['CLAUDE.md','README.md']+['docs/'+n+'.md' for n in ['PROGRAMME_STATE','PROGRESS','DECISIONS','RELIGHT-design','EXPLORATION_DEFENCE_PLAN','PHASES','STANDARDS','RI-02B_UI_SPEC']]+['docs/evidence/ui-redesign/README.md']
links=0
for n in names:
 p=root/n
 for ref in re.findall(r'\]\(([^)]+)\)',read(p)):
  if '://' in ref or ref.startswith('#'):continue
  target=(p.parent/ref.split('#')[0].strip('<>')).resolve()
  if target!=out/'verification.json':assert target.exists(),(n,ref)
  links+=1
result={'status':'UI-06 implementation and automated checks complete; human play not_run','sourceInputs':219,'changedFiles':m['changes'],'productionFiles':6,'preparedFixtures':3,'fullTests':376,'focusedTests':17,'scaleCases':8,'browserResults':list(results),'normalTextMinimumContrast':min(x['ratio'] for x in contrast['text']),'controlBorderMinimumContrast':min(x['ratio'] for x in contrast['borders']),'localReferences':links,'priorEvidence':'UI-05, P9-05, P9-04R and EX-08D preserved','documents':{n:sha(root/n) for n in names},'evidence':{p.relative_to(out).as_posix():sha(p) for p in out.rglob('*') if p.is_file() and p.name not in ['verification.json','consistency.log']}}
(out/'verification.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8',newline='\n')
print(f'PASS: 219 sources / 12 changes / 9 archived HTTP files; 376 full + 17 focused; types/build/lint/docs; eight scale cases and integration browser results; {links} references; frozen evidence/human gates preserved; UI-07 next.')
