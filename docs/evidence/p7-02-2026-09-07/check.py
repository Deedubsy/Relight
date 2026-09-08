from pathlib import Path
import hashlib,json,re,zipfile
root=Path(__file__).resolve().parents[3];out=Path(__file__).resolve().parent
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
manifest=json.loads((out/'manifest.json').read_text(encoding='utf-8'))
for archive,key,folder in [('source.zip','source_files',root),('build.zip','build_files',root/'packages/game/dist')]:
 assert sha(out/archive)==manifest['archives'][archive],archive
 with zipfile.ZipFile(out/archive) as z:
  for n,h in manifest[key].items():assert sha(folder/n)==h and hashlib.sha256(z.read(n)).hexdigest()==h,n
prior=root/'docs/evidence/p7-01-2026-09-07';verified=json.loads((prior/'verification.json').read_text(encoding='utf-8'))
for n,h in verified['evidence'].items():assert sha(prior/n)==h,('prior evidence changed',n)
assert sha(root/'docs/P7_01_DISCOVERIES_REPORT.md')==verified['documents']['docs/P7_01_DISCOVERIES_REPORT.md']
docs=['CLAUDE.md','README.md']+['docs/'+n+'.md' for n in ['PROGRESS','PROGRAMME_STATE','DECISIONS','PHASES','STANDARDS','EXPLORATION_DEFENCE_PLAN','RELIGHT-design','P7_02_CONCRETE_REPORT','CAMPAIGN_RULES']]
count=0
for n in docs:
 p=root/n
 for link in re.findall(r'\]\(([^)]+)\)',p.read_text(encoding='utf-8-sig')):
  if re.match(r'\w+://',link) or link.startswith('#'):continue
  assert (p.parent/link.split('#')[0].strip('<>')).exists(),(n,link)
  count+=1
s=(root/'docs/PROGRESS.md').read_text(encoding='utf-8');rows={r.split('|')[1].strip():r.split('|') for r in s.splitlines() if r.startswith('| ')}
assert rows['P7-02'][4].strip()=='done' and rows['P7-03'][4].strip()=='todo'
for n in ['EX-08H','P6-H','P7-H','T18']:assert rows[n][4].strip()=='blocked' and 'EX-08C' in rows[n][5],n
for filename,n in [('tests.log',262),('tests-final.log',262),('final-focused.log',19)]:
 log=(out/filename).read_text(encoding='utf-8');assert f'# pass {n}' in log and '# fail 0' in log,filename
browser=json.loads((out/'browser.json').read_text(encoding='utf-8'));assert not browser['errors'] and len(browser['rows'])==2
for r in browser['rows']:assert r['produced']['made']==20 and r['final']['concrete']==16 and r['final']['replay']['same'] and r['saveReload'] and not r['final']['overflow']
for archive,files in [('campaign-experiments.zip',manifest['campaign_experiments']),('prior-campaign-experiments.zip',json.loads((out/'prior-campaign-experiments.json').read_text(encoding='utf-8')))]:
 assert sha(out/archive)==manifest['archives'][archive]
 with zipfile.ZipFile(out/archive) as z:
  for n,h in files.items():assert hashlib.sha256(z.read(n)).hexdigest()==h,(archive,n)
for n,h in manifest['campaign_experiments'].items():
 assert sha(root/n)==h,n
 d=json.loads((root/n).read_text(encoding='utf-8'));assert all(c['pass'] for c in d['checks'])
assert 'freshness: every generated file' in (out/'freshness.log').read_text(encoding='utf-8')
(out/'verification.json').write_bytes(json.dumps({'documents':{n:sha(root/n) for n in docs},'evidence':{p.name:sha(p) for p in out.iterdir() if p.is_file() and p.name not in ['verification.json','consistency.log']},'localReferences':count,'priorP701Evidence':'unchanged'},indent=2).encode('utf-8'))
print(f'PASS: {count} references; source/build/archive hashes; 262 tests and 19 focused checks; browser controls/production/save/replay; prior P7-01 evidence unchanged; P7-03 next, human tasks after EX-08C.')
