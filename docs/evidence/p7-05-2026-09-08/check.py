from pathlib import Path
import hashlib,json,re,zipfile
root=Path(__file__).resolve().parents[3];out=Path(__file__).resolve().parent
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
manifest=json.loads((out/'manifest.json').read_text(encoding='utf-8'))
for archive,key,folder in [('source.zip','source_files',root),('build.zip','build_files',root/'packages/game/dist')]:
 assert sha(out/archive)==manifest['archives'][archive],archive
 with zipfile.ZipFile(out/archive) as z:
  for n,h in manifest[key].items():assert sha(folder/n)==h and hashlib.sha256(z.read(n)).hexdigest()==h,n
prior=root/'docs/evidence/p7-04-2026-09-08';verified=json.loads((prior/'verification.json').read_text(encoding='utf-8'))
for n,h in verified['evidence'].items():assert sha(prior/n)==h,('prior evidence changed',n)
assert sha(root/'docs/P7_04_INFORMATION_REPORT.md')==verified['documents']['docs/P7_04_INFORMATION_REPORT.md']
retained=root/'docs/evidence/ex08b-2026-09-07';v=json.loads((retained/'verification-manifest.json').read_text(encoding='utf-8'));b=json.loads((retained/'build-manifest.json').read_text(encoding='utf-8'))
# Mutable campaign experiment paths were refreshed in P7-02; EX-08B archives retain their original evidence.
for n,h in v['protected_files'].items():
 if not n.startswith('docs/experiments/campaign/'):assert sha(root/n)==h,n
for n,h in v['verification_files'].items():assert sha(retained/n)==h,n
for n,h in b['archives'].items():assert sha(retained/n)==h,n
for n in ['EX08B_SESSION_GUIDE.md','EX08B_SESSION_RECORD.md','EX08B_PREPARATION_REPORT.md']:assert sha(root/'docs'/n)==v['documents']['docs/'+n]
docs=['CLAUDE.md','README.md']+['docs/'+n+'.md' for n in ['PROGRESS','PROGRAMME_STATE','DECISIONS','PHASES','STANDARDS','EXPLORATION_DEFENCE_PLAN','RELIGHT-design','P7_05_REVIEW_REPORT','CAMPAIGN_RULES']]
count=0
for n in docs:
 p=root/n
 for link in re.findall(r'\]\(([^)]+)\)',p.read_text(encoding='utf-8-sig')):
  if re.match(r'\w+://',link) or link.startswith('#'):continue
  assert (p.parent/link.split('#')[0].strip('<>')).exists(),(n,link)
  count+=1
s=(root/'docs/PROGRESS.md').read_text(encoding='utf-8');rows={r.split('|')[1].strip():r.split('|') for r in s.splitlines() if r.startswith('| ')}
assert rows['P7-05'][4].strip()=='done' and rows['EX-08C'][4].strip()=='todo'
for n in ['EX-08H','P6-H','P7-H','T18']:assert rows[n][4].strip()=='blocked' and 'EX-08C' in rows[n][5],n
log=(out/'tests.log').read_text(encoding='utf-8');assert '# pass 271' in log and '# fail 0' in log
browser=json.loads((out/'browser.json').read_text(encoding='utf-8'));assert not browser['errors'] and len(browser['rows'])==2
for r in browser['rows']:assert r['final']['replay']['same'] and r['reload'] and not r['final']['overflow'] and len(r['facts'])==9
assert len(browser['perf'])==4 and sum(r['successful'] for r in browser['perf'])==80
for r in browser['perf']:assert r['successful']==r['edits'] and r['guideOpen'] and r['lightPaints']>0 and r['endSim']>r['startSim']+3
assert 'freshness: every generated file' in (out/'freshness.log').read_text(encoding='utf-8')
assert 'docsync: doc tables match' in (out/'docsync-check.log').read_text(encoding='utf-8')
assert 'matches (compact seed 3' in (out/'snapshot.log').read_text(encoding='utf-8')
runs=[json.loads(x) for x in (out/'ordinary-final.log').read_text(encoding='utf-8').splitlines()]
assert len(runs)==12 and {(r['seed'],r['order']) for r in runs}=={(s,o) for s in [3,4,5,8,11,13] for o in ['electrical-first','survey-first']}
for r in runs:assert r['conservation'] and r['replay'] and r['save'] and r['coreHp']>0 and r['concreteMade']==56 and r['rounds']>0
sweep=[json.loads(x) for x in (out/'sweep.log').read_text(encoding='utf-8').splitlines()];assert len(sweep)==68 and [r['seed'] for r in sweep[:64]]==list(range(1,65))
services=[json.loads(x) for x in (out/'services.log').read_text(encoding='utf-8').splitlines()];assert len(services)==12
for r in services:assert all(l['lit'] for l in r['lights']) and r['poleCircuitSupply']>0 and r['readOnly']
network=json.loads((out/'campaign/E-defence-turrets-seed3.json').read_text(encoding='utf-8'));assert len(network['checks'])==13 and all(c['pass'] for c in network['checks'])
assert (out/'visual.log').read_text(encoding='utf-8').count('PASS')==2
checkpoint=json.loads((out/'guide-checkpoint.json').read_text(encoding='utf-8'));assert checkpoint['state']['campaign']['version']==9 and len(checkpoint['log'])==35
assert sha(root/'packages/game/dist/guide-checkpoint.json')==sha(out/'guide-checkpoint.json')
(out/'verification.json').write_bytes(json.dumps({'documents':{n:sha(root/n) for n in docs},'evidence':{p.relative_to(out).as_posix():sha(p) for p in out.rglob('*') if p.is_file() and p.name not in ['verification.json','consistency.log']},'localReferences':count,'priorP704Evidence':'unchanged','retainedEX08B':'unchanged'},indent=2).encode('utf-8'))
print(f'PASS: {count} references; 181 source inputs / 7 build files and archive hashes; 271 tests; 12 paid reward/raid runs; 64 seeds; 4 real-save migrations; 13 network checks; two browser widths with save/replay; 80 successful live edits; prior P7-04 and retained EX-08B evidence unchanged; EX-08C next, human tasks after EX-08C.')
