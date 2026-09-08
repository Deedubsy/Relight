from pathlib import Path
import hashlib,json,zipfile,difflib,subprocess
root=Path(__file__).resolve().parents[3];out=Path(__file__).resolve().parent
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
prior=root/'docs/evidence/p7-01-2026-09-07'
baseline=json.loads((prior/'manifest.json').read_text(encoding='utf-8'))
names=sorted(set(baseline['source_files'])|{'packages/sim/src/concreteValidation.ts','packages/sim/test/concrete.test.ts'})
source={n:sha(root/n) for n in names}
dist=root/'packages/game/dist';build={p.relative_to(dist).as_posix():sha(p) for p in sorted(dist.rglob('*')) if p.is_file()}
for filename,files,folder in [('source.zip',source,root),('build.zip',build,dist)]:
 with zipfile.ZipFile(out/filename,'w',zipfile.ZIP_DEFLATED) as z:
  for n in files:z.write(folder/n,n)
with zipfile.ZipFile(prior/'source.zip') as z:
 changes=[]
 for n in names:
  previous=z.read(n).decode('utf-8-sig').replace('\r\n','\n').splitlines(True) if n in z.namelist() else []
  current=(root/n).read_text(encoding='utf-8-sig').splitlines(True)
  changes.extend(difflib.unified_diff(previous,current,fromfile='P7-01/'+n,tofile='P7-02/'+n))
(out/'changes.diff').write_bytes(''.join(changes).encode('utf-8'))
manifest={'date':'2026-09-08','branch':'codex/tram-expansion','baseHead':subprocess.check_output(['git','rev-parse','HEAD'],cwd=root,text=True).strip(),
 'authority':'D-EX-36 / adopted Q09','profile':'exploration-v2','campaignMetadata':7,'source_files':source,'build_files':build,
 'archives':{n:sha(out/n) for n in ['source.zip','build.zip']},'checks':{'tests':262,'finalFocused':19,'seeds':list(range(1,33)),'browserWidths':[1366,900]},
 'humanPlay':'deferred until after P7 and EX-08C','nextTask':'P7-03'}
experiments=sorted((root/'docs/experiments/campaign').glob('*.json'))
assert len(experiments)==12
with zipfile.ZipFile(out/'campaign-experiments.zip','w',zipfile.ZIP_DEFLATED) as z:
 for p in experiments:
  d=json.loads(p.read_text(encoding='utf-8'));assert all(c['pass'] for c in d['checks']),p
  for n,h in d['source_files'].items():assert sha(root/n)==h,(p,n)
  z.write(p,p.relative_to(root).as_posix())
manifest['campaign_experiments']={p.relative_to(root).as_posix():sha(p) for p in experiments}
manifest['archives'].update({n:sha(out/n) for n in ['campaign-experiments.zip','prior-campaign-experiments.zip']})
manifest['checks']['factoryRuns']=12
(out/'manifest.json').write_bytes(json.dumps(manifest,indent=2).encode('utf-8'))
print(f'Archived {len(source)} source inputs and {len(build)} build files; P7-01-relative change record captured.')
