from pathlib import Path
import hashlib,json,re,zipfile
root=Path(__file__).resolve().parents[3];out=Path(__file__).resolve().parent
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
manifest=json.loads((out/'manifest.json').read_text(encoding='utf-8'))
for n,h in manifest['source_files'].items():assert sha(root/n)==h,n
for n,h in manifest['build_files'].items():assert sha(root/'packages/game/dist'/n)==h,n
for archive,key in [('source.zip','source_files'),('build.zip','build_files')]:
 assert sha(out/archive)==manifest['archives'][archive],archive
 with zipfile.ZipFile(out/archive) as z:
  for n,h in manifest[key].items():assert hashlib.sha256(z.read(n)).hexdigest()==h,n
docs=['CLAUDE.md','README.md']+['docs/'+n+'.md' for n in ['PROGRESS','PROGRAMME_STATE','DECISIONS','PHASES','STANDARDS','EXPLORATION_DEFENCE_PLAN','RELIGHT-design','P7_01_DISCOVERIES_REPORT']]
count=0
for n in docs:
 p=root/n
 for link in re.findall(r'\]\(([^)]+)\)',p.read_text(encoding='utf-8-sig')):
  if re.match(r'\w+://',link) or link.startswith('#'):continue
  assert (p.parent/link.split('#')[0].strip('<>')).exists(),(n,link)
  count+=1
s=(root/'docs/PROGRESS.md').read_text(encoding='utf-8')
rows={r.split('|')[1].strip():r.split('|') for r in s.splitlines() if r.startswith('| ')}
assert rows['P7-01'][4].strip()=='done' and rows['P7-02'][4].strip()=='todo'
for n in ['EX-08H','P6-H','P7-H','T18']:assert rows[n][4].strip()=='blocked' and 'EX-08C' in rows[n][5],n
assert '# pass 255' in (out/'tests.log').read_text(encoding='utf-8') and '# fail 0' in (out/'tests.log').read_text(encoding='utf-8')
browser=json.loads((out/'browser.json').read_text(encoding='utf-8'))
assert not browser['errors'] and len(browser['rows'])==2
for r in browser['rows']:assert r['built']['replay']['same'] and r['saveReload']
(out/'verification.json').write_text(json.dumps({'documents':{n:sha(root/n) for n in docs},'evidence':{p.name:sha(p) for p in out.iterdir() if p.is_file() and p.name not in ['verification.json','consistency.log']},'localReferences':count},indent=2),encoding='utf-8')
print(f'PASS: {count} references; source/build/archive hashes; 255 tests; browser controls/save/replay; P7-02 next, human tasks after EX-08C.')
