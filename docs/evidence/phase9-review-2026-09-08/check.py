from pathlib import Path
import hashlib,json,re,zipfile,subprocess
root=Path(__file__).resolve().parents[3];out=Path(__file__).resolve().parent
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
read=lambda p:p.read_text(encoding='utf-8-sig')
prior=root/'docs/evidence/ex08d-2026-09-08';v=json.loads(read(prior/'verification-manifest.json'));m=json.loads(read(prior/'build-manifest.json'))
assert subprocess.check_output(['git','rev-parse','HEAD'],cwd=root,text=True).strip()==m['parent_commit']
difference=json.loads(read(out/'working-tree-difference.json'))
for n,h in m['source_files'].items():
 if n==difference['path']:
  assert h==difference['frozenSha256'] and sha(root/n)==difference['currentSha256']
  with zipfile.ZipFile(prior/'source.zip') as z:assert json.loads(z.read(n))==json.loads((root/n).read_bytes())
 else:assert sha(root/n)==h,('source',n)
for n,h in v['verification_files'].items():assert sha(prior/n)==h,('EX08D',n)
for name,key in [('source.zip','source_files'),('playtest-build.zip','served_files')]:
 assert sha(prior/name)==m['archives'][name]
 with zipfile.ZipFile(prior/name) as z:
  assert set(z.namelist())==set(m[key])
  for n,h in m[key].items():assert hashlib.sha256(z.read(n)).hexdigest()==h,n
for n in ['EX08D_PREPARATION_REPORT.md','EX08D_SESSION_GUIDE.md','EX08D_SESSION_RECORD.md']:
 assert sha(root/'docs'/n)==v['documents']['docs/'+n],n
for n,h in m['served_files'].items():
 if n not in ['fresh.json','construction.json','network.json','turbine.json','settings.json']:assert sha(root/'packages/game/dist'/n)==h,n
for file,count in [('focused.log',6),('urban.log',2)]:
 t=read(out/file);assert f'# pass {count}' in t and '# fail 0' in t and '# skipped 0' in t,file
assert 'doc tables match' in read(out/'docsync.log')
assert 'every generated file was made on an ancestor' in read(out/'freshness.log')
rows={r.split('|')[1].strip():r.split('|') for r in read(root/'docs/PROGRESS.md').splitlines() if r.startswith('| ')}
assert rows['P9-00'][4].strip()=='done' and rows['P9-01'][4].strip()=='todo'
for n in ['P9-02','P9-03','P9-04','P9-05','P9-H']:assert rows[n][4].strip()=='blocked'
for n in ['EX-08H','P6-H','P7-H','P8-H','T18']:assert rows[n][4].strip()=='todo'
assert rows['EX-09'][4].strip()=='blocked' and 'Q07' in rows['EX-09'][5]
assert '**Status: NOT RUN.**' in read(root/'docs/EX08D_SESSION_RECORD.md')
names=['CLAUDE.md','README.md']+['docs/'+n+'.md' for n in ['PROGRAMME_STATE','PROGRESS','DECISIONS','PHASES','STANDARDS','EXPLORATION_DEFENCE_PLAN','RELIGHT-design','PHASE_9_SCOPE_REPORT']]
links=0
for n in names:
 p=root/n
 for ref in re.findall(r'\]\(([^)]+)\)',read(p)):
  if '://' in ref or ref.startswith('#'):continue
  target=(p.parent/ref.split('#')[0].strip('<>')).resolve()
  if target!=out/'manifest.json':assert target.exists(),(n,ref)
  links+=1
result={'parent_commit':m['parent_commit'],'sourceInputs':len(m['source_files']),'source_files':{n:sha(root/n) for n in m['source_files']},'frozen_source_files':m['source_files'],'workingTreeDifference':difference,'priorEX08D':'source/build/starts/evidence/report/guide/blank record unchanged','human_play':'not_run','documents':{n:sha(root/n) for n in names},'verification_files':{p.relative_to(out).as_posix():sha(p) for p in out.rglob('*') if p.is_file() and p.name not in ['manifest.json','consistency.log']},'localReferences':links}
(out/'manifest.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8',newline='\n')
print(f'PASS: 192/193 source inputs byte-identical, one unrelated fixture format-only change; compiled files unchanged; frozen EX-08D preserved; 6 campaign + 2 urban tests; {links} local references; P9 review complete/P9-01 next; human obligations and Q07 retained.')
