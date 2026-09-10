from pathlib import Path
import hashlib,json,re,zipfile,subprocess
root=Path(__file__).resolve().parents[3];out=Path(__file__).resolve().parent
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
read=lambda p:p.read_text(encoding='utf-8-sig')
build=json.loads(read(out/'build-manifest.json'));settings=json.loads(read(out/'settings.json'))
assert subprocess.check_output(['git','rev-parse','HEAD'],cwd=root,text=True).strip()==build['parent_commit']
assert subprocess.check_output(['git','rev-parse','origin/codex/tram-expansion'],cwd=root,text=True).strip()==build['parent_commit']
for n,h in build['source_files'].items():assert sha(root/n)==h,n
for archive,key in [('source.zip','source_files'),('playtest-build.zip','served_files')]:
 assert sha(out/archive)==build['archives'][archive]
 with zipfile.ZipFile(out/archive) as z:
  assert set(z.namelist())==set(build[key])
  for n,h in build[key].items():assert hashlib.sha256(z.read(n)).hexdigest()==h,n
assert sha(out/'settings.json')==build['settings_sha256']
for n in ['fresh','network','turbine']:assert sha(out/'campaign'/(n+'.json'))==build['served_files'][n+'.json']
prior=root/'docs/evidence/p7-05-2026-09-08';v=json.loads(read(prior/'verification.json'))
for n,h in v['evidence'].items():assert sha(prior/n)==h,('P7-05',n)
assert sha(root/'docs/P7_05_REVIEW_REPORT.md')==v['documents']['docs/P7_05_REVIEW_REPORT.md']
old=root/'docs/evidence/ex08b-2026-09-07';ov=json.loads(read(old/'verification-manifest.json'));ob=json.loads(read(old/'build-manifest.json'))
for n,h in ov['protected_files'].items():
 if not n.startswith('docs/experiments/campaign/'):assert sha(root/n)==h,n
for n,h in ov['verification_files'].items():assert sha(old/n)==h,n
for n,h in ob['archives'].items():assert sha(old/n)==h,n
for n in ['EX08B_PREPARATION_REPORT','EX08B_SESSION_GUIDE','EX08B_SESSION_RECORD']:assert sha(root/'docs'/(n+'.md'))==ov['documents']['docs/'+n+'.md']
log=read(out/'focused.log');assert '# pass 37' in log and '# fail 0' in log and '# skipped 0' in log
assert 'PASS: three starts and ordinary-command prefixes' in read(out/'prepare.log')
b=json.loads(read(out/'browser-result.json'));assert not b['errors'] and len(b['starts'])==6 and len(b['rows'])==4
for s in b['starts']:assert s['speed']==0 and s['complete'] and s['saveReload'] and not s['overflow']
assert sum(r['successful'] for r in b['rows'])==72
for r in b['rows']:assert r['edits']==r['successful'] and r['lightPaints']>0 and r['endSim']>r['startSim']+3
w=json.loads(read(out/'warning-result.json'));assert not w['errors'] and w['cameraControlsPreserveState'] and w['before']['warning']['receivedAt']==2400
rest=json.loads(read(out/'restoration-result.json'));assert len(rest)==2
for r in rest:assert r['replay']['same'] and r['spent']==40 and r['bases']==1
assert 'doc tables match' in read(out/'docsync.log') and 'every generated file was made on an ancestor' in read(out/'freshness.log')
rows={r.split('|')[1].strip():r.split('|') for r in read(root/'docs/PROGRESS.md').splitlines() if r.startswith('| ')}
assert rows['EX-08C'][4].strip()=='done'
for n in ['EX-08H','P6-H','P7-H','T18']:assert rows[n][4].strip()=='todo',n
record=read(root/'docs/EX08C_SESSION_RECORD.md');assert '**Status: NOT RUN.**' in record and 'Phase 7 verdict and reasons: **not recorded**' in record and settings['config_hash'] in record
names=['CLAUDE.md','README.md']+['docs/'+n+'.md' for n in ['PROGRESS','PROGRAMME_STATE','DECISIONS','PHASES','STANDARDS','EXPLORATION_DEFENCE_PLAN','EX08C_PREPARATION_REPORT','EX08C_SESSION_GUIDE','EX08C_SESSION_RECORD']]
count=0
for n in names:
 p=root/n
 for ref in re.findall(r'\]\(([^)]+)\)',read(p)):
  if '://' in ref or ref.startswith('#'):continue
  target=(p.parent/ref.split('#')[0]).resolve()
  if target!=out/'verification-manifest.json':assert target.exists(),(n,ref)
  count+=1
result={'parent_commit':build['parent_commit'],'status':'EX-08C prepared locally; human play NOT RUN','documents':{n:sha(root/n) for n in names},'verification_files':{p.relative_to(out).as_posix():sha(p) for p in out.rglob('*') if p.is_file() and p.name not in ['verification-manifest.json','consistency.log']},'priorP705':'unchanged','priorEX08B':'unchanged','sourceInputs':181,'servedFiles':10,'localReferences':count}
(out/'verification-manifest.json').write_bytes(json.dumps(result,indent=2).encode('utf-8'))
print(f'PASS: committed/pushed source identity; 181 source inputs, 10 served files and archive hashes; 37 tests; six start/save cases; natural warning; two paid restorations; 72 live edits; {count} references; blank record and human-task handoff; prior P7-05/EX-08B preserved.')
