from pathlib import Path
import hashlib,json,zipfile,difflib,subprocess,re
root=Path(__file__).resolve().parents[3];out=Path(__file__).resolve().parent;prior=root/'docs/evidence/p7-02-2026-09-07'
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
baseline=json.loads((prior/'manifest.json').read_text(encoding='utf-8'))
names=sorted(set(baseline['source_files'])|{'packages/sim/src/campaignTurbine.ts','packages/sim/test/campaignTurbine.test.ts'})
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
  changes.extend(difflib.unified_diff(previous,current,fromfile='P7-02/'+n,tofile='P7-03/'+n))
(out/'changes.diff').write_bytes(''.join(changes).encode('utf-8'))
log=(out/'tests-final.log').read_text(encoding='utf-8');assert '# pass 266' in log and '# fail 0' in log
manifest={'date':'2026-09-08','branch':'codex/tram-expansion','baseHead':subprocess.check_output(['git','rev-parse','HEAD'],cwd=root,text=True).strip(),'authority':'D-EX-37 / adopted Q09','profile':'exploration-v2','campaignMetadata':8,'source_files':source,'build_files':build,'archives':{n:sha(out/n) for n in ['source.zip','build.zip']},'checks':{'tests':266,'placementSeeds':list(range(1,33)),'mechanicsSeeds':[3,4,5,8,11,13],'browserWidths':[1366,900],'ordinaryProductionAndRestorationReplay':True},'humanPlay':'deferred until after P7 and EX-08C','nextTask':'P7-04'}
(out/'manifest.json').write_bytes(json.dumps(manifest,indent=2).encode('utf-8'))
print(f'Archived {len(source)} source inputs and {len(build)} production files, including the ordinary pre-commissioning checkpoint; P7-02-relative diff captured.')
