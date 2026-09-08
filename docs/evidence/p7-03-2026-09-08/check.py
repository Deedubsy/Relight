from pathlib import Path
import hashlib,json,re,zipfile
root=Path(__file__).resolve().parents[3];out=Path(__file__).resolve().parent
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
manifest=json.loads((out/'manifest.json').read_text(encoding='utf-8'))
for archive,key,folder in [('source.zip','source_files',root),('build.zip','build_files',root/'packages/game/dist')]:
 assert sha(out/archive)==manifest['archives'][archive],archive
 with zipfile.ZipFile(out/archive) as z:
  for n,h in manifest[key].items():assert sha(folder/n)==h and hashlib.sha256(z.read(n)).hexdigest()==h,n
prior=root/'docs/evidence/p7-02-2026-09-07';verified=json.loads((prior/'verification.json').read_text(encoding='utf-8'))
for n,h in verified['evidence'].items():assert sha(prior/n)==h,('prior evidence changed',n)
assert sha(root/'docs/P7_02_CONCRETE_REPORT.md')==verified['documents']['docs/P7_02_CONCRETE_REPORT.md']
docs=['CLAUDE.md','README.md']+['docs/'+n+'.md' for n in ['PROGRESS','PROGRAMME_STATE','DECISIONS','PHASES','STANDARDS','EXPLORATION_DEFENCE_PLAN','RELIGHT-design','P7_03_TURBINE_REPORT','CAMPAIGN_RULES']]
count=0
for n in docs:
 p=root/n
 for link in re.findall(r'\]\(([^)]+)\)',p.read_text(encoding='utf-8-sig')):
  if re.match(r'\w+://',link) or link.startswith('#'):continue
  assert (p.parent/link.split('#')[0].strip('<>')).exists(),(n,link)
  count+=1
s=(root/'docs/PROGRESS.md').read_text(encoding='utf-8');rows={r.split('|')[1].strip():r.split('|') for r in s.splitlines() if r.startswith('| ')}
assert rows['P7-03'][4].strip()=='done' and rows['P7-04'][4].strip()=='todo'
for n in ['EX-08H','P6-H','P7-H','T18']:assert rows[n][4].strip()=='blocked' and 'EX-08C' in rows[n][5],n
log=(out/'tests-final.log').read_text(encoding='utf-8');assert '# pass 266' in log and '# fail 0' in log
browser=json.loads((out/'browser.json').read_text(encoding='utf-8'));assert not browser['errors'] and len(browser['rows'])==2
for r in browser['rows']:assert r['result']['concreteSpent']==40 and r['result']['bases']==1 and r['result']['replay']['same'] and r['reload'] and not r['result']['overflow']
probe=[json.loads(x) for x in (out/'placement-probe.log').read_text(encoding='utf-8').splitlines()];assert len(probe)==32 and all(p['path'] and p['stone']>0 for p in probe)
assert 'freshness: every generated file' in (out/'freshness.log').read_text(encoding='utf-8')
assert 'docsync: doc tables match' in (out/'docsync-check.log').read_text(encoding='utf-8')
assert (out/'compatibility.log').read_text(encoding='utf-8').count('PASS:')==3
assert '"conservation": true' in (out/'ordinary.log').read_text(encoding='utf-8') and '"replay": true' in (out/'ordinary.log').read_text(encoding='utf-8')
(out/'verification.json').write_bytes(json.dumps({'documents':{n:sha(root/n) for n in docs},'evidence':{p.name:sha(p) for p in out.iterdir() if p.is_file() and p.name not in ['verification.json','consistency.log']},'localReferences':count,'priorP702Evidence':'unchanged'},indent=2).encode('utf-8'))
print(f'PASS: {count} references; source/build/archive hashes; 266 tests; ordinary production/restoration replay; 32 seeds; actual browser controls/save/replay; prior P7-02 evidence unchanged; P7-04 next, human tasks after EX-08C.')
