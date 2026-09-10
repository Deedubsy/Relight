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
old=root/'docs/evidence/ex08c-2026-09-08';v=json.loads(read(old/'verification-manifest.json'));b=json.loads(read(old/'build-manifest.json'))
for n,h in v['verification_files'].items():assert sha(old/n)==h,('EX08C',n)
for n,h in b['archives'].items():assert sha(old/n)==h,n
for n in ['EX08C_PREPARATION_REPORT.md','EX08C_SESSION_GUIDE.md','EX08C_SESSION_RECORD.md']:assert sha(root/'docs'/n)==v['documents']['docs/'+n],n
assert '**Status: NOT RUN.**' in read(root/'docs/EX08C_SESSION_RECORD.md')
review=root/'docs/evidence/phase8-review-2026-09-08';rv=json.loads(read(review/'manifest.json'))
for n,h in rv['verification_files'].items():assert sha(review/n)==h,('scope review',n)
assert sha(root/'docs/PHASE_8_SCOPE_REPORT.md')==rv['documents']['docs/PHASE_8_SCOPE_REPORT.md']
prior=root/'docs/evidence/p7-05-2026-09-08';pv=json.loads(read(prior/'verification.json'))
for n,h in pv['evidence'].items():assert sha(prior/n)==h,('P7-05',n)
assert sha(root/'docs/P7_05_REVIEW_REPORT.md')==pv['documents']['docs/P7_05_REVIEW_REPORT.md']
retained=root/'docs/evidence/ex08b-2026-09-07';ov=json.loads(read(retained/'verification-manifest.json'));ob=json.loads(read(retained/'build-manifest.json'))
for n,h in ov['protected_files'].items():
 if not n.startswith('docs/experiments/campaign/'):assert sha(root/n)==h,n
for n,h in ov['verification_files'].items():assert sha(retained/n)==h,n
for n,h in ob['archives'].items():assert sha(retained/n)==h,n
for n in ['EX08B_SESSION_GUIDE.md','EX08B_SESSION_RECORD.md','EX08B_PREPARATION_REPORT.md']:assert sha(root/'docs'/n)==ov['documents']['docs/'+n]
previous=root/'docs/evidence/p8-01-2026-09-08';prev=json.loads(read(previous/'verification.json'))
for n,h in prev['evidence'].items():assert sha(previous/n)==h,('P8-01',n)
assert sha(root/'docs/P8_01_CLIPBOARD_REPORT.md')==prev['documents']['docs/P8_01_CLIPBOARD_REPORT.md']
previous2=root/'docs/evidence/p8-02-2026-09-08';prev2=json.loads(read(previous2/'verification.json'))
for n,h in prev2['evidence'].items():assert sha(previous2/n)==h,('P8-02',n)
assert sha(root/'docs/P8_02_LIBRARY_REPORT.md')==prev2['documents']['docs/P8_02_LIBRARY_REPORT.md']
previous3=root/'docs/evidence/p8-03-2026-09-08';prev3=json.loads(read(previous3/'verification.json'))
for n,h in prev3['evidence'].items():assert sha(previous3/n)==h,('P8-03',n)
assert sha(root/'docs/P8_03_TRUCK_REPORT.md')==prev3['documents']['docs/P8_03_TRUCK_REPORT.md']
for p in (previous3/'campaign').glob('*.json'):assert sha(root/'docs/experiments/campaign'/p.name)==sha(p)
previous4=root/'docs/evidence/p8-04-2026-09-08';prev4=json.loads(read(previous4/'verification.json'))
for n,h in prev4['evidence'].items():assert sha(previous4/n)==h,('P8-04',n)
assert sha(root/'docs/P8_04_REMOVAL_REPORT.md')==prev4['documents']['docs/P8_04_REMOVAL_REPORT.md']
for file,total in [('full-test.log',320),('focused.log',76),('integration-final.log',7)]:
 log=read(out/file);assert f'# pass {total}' in log and '# fail 0' in log and '# skipped 0' in log
browser=json.loads(read(out/'browser-result.json'));assert not browser['errors'] and len(browser['rows'])==2
for r in browser['rows']:
 assert r['replay']['same'] and not r['overflow']
 assert r['built']['order']['status']=='completed' and r['packed']['pockets']['belt']==2
 assert len(r['packed']['machines'])==len(r['built']['machines'])-2
 assert len(r['saved']['machines'])==len(r['built']['machines'])
assert 'doc tables match' in read(out/'docsync.log')
assert 'every generated file was made on an ancestor' in read(out/'freshness.log')
assert 'built in' in read(out/'typecheck.log')
assert not re.search(r'\berror\b',read(out/'lint.log'),re.I)
assert sha(previous3/'truck-ready.json')==sha(root/'packages/game/dist/truck-ready.json')
assert 'matches (compact seed 3 at 3:00:00' in read(out/'snapshot.log')
assert sha(out/'ammo-ready.json')==sha(root/'packages/game/dist/ammo-ready.json')
a=json.loads(read(out/'ammo-ready.json'));b=json.loads(read(out/'ammo-final-ready.json'));a.pop('savedAt');b.pop('savedAt');assert a==b, 'final scenario state/log changed from browser fixture'
for seed in [3,4,5,8,11,13]:
 run=json.loads(read(out/f'seed-{seed}.json'));assert run['seed']==seed and run['hash']==run['replay'] and run['ledger']['ok']
 assert run['rounds']==[20,20] and run['cost']=={'steel':68,'copper':27}
 if seed==3:assert run['live']['coexistQuarterSeconds']>0 and run['live']['delivered']==3 and run['live']['walls']==[120,120]
sweep=json.loads(read(out/'route-sweep.json'));assert len(sweep['rows'])==16 and sweep['reachable']+sweep['unreachable']==128 and sweep['validTurns']>0 and sweep['blockedTurns']>0
for r in browser['rows']:
 assert r['focusAndCancellation'] and r['liveReplay']['same'] and r['perf']['edits']==r['perf']['success'] and r['perf']['edits']>=12 and r['perf']['simSeconds']>4
docs=['CLAUDE.md','README.md']+['docs/'+n+'.md' for n in ['PROGRESS','PROGRAMME_STATE','DECISIONS','RELIGHT-design','EXPLORATION_DEFENCE_PLAN','PHASES','STANDARDS','CAMPAIGN_RULES','P8_05_REVIEW_REPORT']]
links=0
for n in docs:
 p=root/n
 for ref in re.findall(r'\]\(([^)]+)\)',read(p)):
  if '://' in ref or ref.startswith('#'):continue
  target=(p.parent/ref.split('#')[0].strip('<>')).resolve()
  if target!=out/'verification.json':assert target.exists(),(n,ref)
  links+=1
rows={r.split('|')[1].strip():r.split('|') for r in read(root/'docs/PROGRESS.md').splitlines() if r.startswith('| ')}
assert rows['P8-05'][4].strip()=='done' and rows['EX-08D'][4].strip()=='todo'
for n in ['EX-08H','P6-H','P7-H','P8-H','T18']:assert rows[n][4].strip()=='blocked' and 'EX-08D' in rows[n][5],n
result={'sourceInputs':len(m['source_files']),'productionFiles':len(m['build_files']),'documents':{n:sha(root/n) for n in docs},'evidence':{p.relative_to(out).as_posix():sha(p) for p in out.rglob('*') if p.is_file() and p.name not in ['verification.json','consistency.log']},'localReferences':links,'priorCampaignMeasurements':'unchanged (P8-03)', 'retainedP804':'unchanged', 'retainedP803':'unchanged','retainedP802':'unchanged','retainedP801':'unchanged','retainedEX08C':'unchanged','retainedP800':'unchanged','retainedP705':'unchanged','retainedEX08B':'unchanged','human_play':'not_run'}
(out/'verification.json').write_bytes(json.dumps(result,indent=2).encode('utf-8'))
print(f'PASS: {len(m["source_files"])} source inputs / {len(m["build_files"])} production files and archives; 320 broad + 76 focused + 7 final integration checks; six seed results, 16-seed sweep, both browser widths and full replay; {links} local references; EX-08D next and human tasks preserved; prior evidence unchanged.')
