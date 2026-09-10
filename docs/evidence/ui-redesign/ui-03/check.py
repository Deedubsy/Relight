from pathlib import Path
import hashlib,json,zipfile,re,urllib.request
root=Path(__file__).resolve().parents[4];out=Path(__file__).resolve().parent
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
read=lambda p:p.read_text(encoding='utf-8-sig')
m=json.loads(read(out/'manifest.json'));prior=root/'docs/evidence/ui-redesign/ui-02';old=json.loads(read(prior/'manifest.json'))
assert len(m['source_files'])==212 and len(m['changes'])==12 and len(m['build_files'])==9
for n,h in m['source_files'].items():assert sha(root/n)==h,n
for n,h in old['source_files'].items():
 if n.startswith('packages/harness/') or n.startswith('packages/sim/src/city/') or n in ['packages/game/src/session.ts','packages/sim/src/save.ts','packages/sim/src/rules.ts']:assert m['source_files'][n]==h,n
for archive,key,base in [('source.zip','source_files',root),('build.zip','build_files',root/'packages/game/dist')]:
 assert sha(out/archive)==m['archives'][archive]
 with zipfile.ZipFile(out/archive) as z:
  assert set(z.namelist())==set(m[key])
  for n,h in m[key].items():assert sha(base/n)==h and hashlib.sha256(z.read(n)).hexdigest()==h,n
for n,h in m['build_files'].items():
 with urllib.request.urlopen('http://127.0.0.1:5178/'+n,timeout=10) as r:assert hashlib.sha256(r.read()).hexdigest()==h,('HTTP',n)
for n in ['fresh','construction','network']:assert m['build_files']['ui-'+n+'.json']==sha(root/'docs/evidence/p9-05-2026-09-09/campaign'/(n+'.json'))
v=json.loads(read(prior/'verification.json'))
for n,h in v['evidence'].items():assert sha(prior/n)==h,('UI-02',n)
for folder,docs in [('p9-05-2026-09-09',['P9_05_READINESS_REPORT','P9_SESSION_GUIDE','P9_SESSION_RECORD']),('p9-04r-2026-09-09',['P9_04R_REPAIR_REPORT'])]:
 p=root/'docs/evidence'/folder;v=json.loads(read(p/'verification.json'))
 for n,h in v['evidence'].items():assert sha(p/n)==h,(folder,n)
 for n in docs:assert sha(root/'docs'/(n+'.md'))==v['documents']['docs/'+n+'.md'],n
ex=root/'docs/evidence/ex08d-2026-09-08';v=json.loads(read(ex/'verification-manifest.json'))
for n,h in v['verification_files'].items():assert sha(ex/n)==h,('EX08D',n)
for n in ['EX08D_PREPARATION_REPORT','EX08D_SESSION_GUIDE','EX08D_SESSION_RECORD']:assert sha(root/'docs'/(n+'.md'))==v['documents']['docs/'+n+'.md']

for name,count in [('test.log',365),('focused.log',43)]:
 log=read(out/name);assert f'# pass {count}' in log and '# fail 0' in log and '# skipped 0' in log
assert 'built in' in read(out/'typecheck.log') and not re.search(r'error TS',read(out/'typecheck.log'))
assert not re.search(r'\berror\b',read(out/'lint.log'),re.I)
assert 'doc tables match' in read(out/'docsync.log') and 'every generated file was made on an ancestor' in read(out/'freshness.log')
assert 'git diff --check exit code: 0' in read(out/'git-diff-check.log')
b=json.loads(read(out/'browser-result.json'));assert len(b['rows'])==4 and not b['errors']
for r in b['rows']:
 if r.get('case')=='adapters':
  for k in ['copy','queue','removal','libraryExport']:assert r[k],k
 else:
  for k in ['emptyStock','unknownOmitted','materialPlacement','collisionRefused','repeatAndRotate','packedFirst','shortcutConservesStock','clearAndPersist','moreEscape','engineerVisible']:assert r[k],k
  assert r['replay']['same']
prefs=json.loads(read(out/'preferences-result.json'));assert not prefs['errors']
for k in ['corruptFallback','corruptRepair','unknownReserved','noUnlock','storageUnavailable','hashUnchanged']:assert prefs[k],k
r=json.loads(read(out/'regression-browser-result.json'));assert len(r['rows'])==2 and not r['errors']
for row in r['rows']:
 for k in ['typing','wheel','oneDrawer','escapeHierarchy','modalFocus','heldMovementReleased','dragCancelled','saveReload']:assert row[k],k
 assert row['replay']['same'] and row['resumeSpeed']==4
assert min(x['ratio'] for x in r['contrast']['text'])>=4.5 and min(x['ratio'] for x in r['contrast']['borders'])>=3
for n in ['catalogue-stocked-1366.png','catalogue-stocked-900.png','collision-900.png','quickbar-900.png','library-900.png']:assert (out/n).stat().st_size>1000
rows={line.split('|')[1].strip():line.split('|') for line in read(root/'docs/PROGRESS.md').splitlines() if line.startswith('| ')}
for n in ['RI-02B-UI-01','RI-02B-UI-02','RI-02B-UI-03']:assert rows[n][4].strip()=='done'
assert rows['RI-02B-UI-04'][4].strip()=='todo'
for n in ['RI-02B-UI-05','RI-02B-UI-06','RI-02B-UI-07']:assert rows[n][4].strip()=='blocked'
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

result={'status':'UI-03 implementation and automated checks complete; human play not_run','sourceInputs':212,'changedFiles':m['changes'],'productionFiles':6,'preparedFixtures':3,'fullTests':365,'focusedTests':43,'browserWidths':[1366,900],'browserCases':4,'shellRegressionWidths':[1366,900],'normalTextMinimumContrast':min(x['ratio'] for x in r['contrast']['text']),'controlBorderMinimumContrast':min(x['ratio'] for x in r['contrast']['borders']),'localReferences':links,'priorEvidence':'UI-02, P9-05, P9-04R and EX-08D preserved','documents':{n:sha(root/n) for n in names},'evidence':{p.relative_to(out).as_posix():sha(p) for p in out.rglob('*') if p.is_file() and p.name not in ['verification.json','consistency.log']}}
(out/'verification.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8',newline='\n')
print(f'PASS: 212 sources / 12 changes / 9 archived HTTP files; 365 full + 43 focused; types/build/lint/docs; 4 browser cases, preference failure cases and both-width shell/save/replay/contrast; {links} references; frozen evidence/human gates preserved; UI-04 next.')
