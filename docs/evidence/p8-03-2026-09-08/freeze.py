from pathlib import Path
import hashlib,json,zipfile
root=Path(__file__).resolve().parents[3];out=Path(__file__).resolve().parent
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
prior=json.loads((root/'docs/evidence/p8-02-2026-09-08/manifest.json').read_text(encoding='utf-8-sig'))
files=sorted(set(prior['source_files'])|{'packages/game/src/truckWorkPanel.ts','packages/sim/src/truckWork.ts','packages/sim/test/truckWork.test.ts'})
build=root/'packages/game/dist';production=sorted(str(p.relative_to(build)).replace('\\','/') for p in build.rglob('*') if p.is_file())
manifest={'task':'P8-03','parent_commit':prior['parent_commit'],'campaign_metadata':10,'recruits_version':4,'plan_schema':1,'truck_work_schema':1,'source_files':{n:sha(root/n) for n in files},'build_files':{n:sha(build/n) for n in production},'archives':{}}
for filename,names,base in [('source.zip',files,root),('build.zip',production,build)]:
 with zipfile.ZipFile(out/filename,'w',zipfile.ZIP_DEFLATED) as z:
  for n in names:z.write(base/n,n)
 manifest['archives'][filename]=sha(out/filename)
(out/'manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
print(f"Archived {len(files)} source inputs and {len(production)} production files.")
