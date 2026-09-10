from pathlib import Path
import hashlib,json,zipfile
root=Path(__file__).resolve().parents[3];out=Path(__file__).resolve().parent
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
prior=json.loads((root/'docs/evidence/phase9-review-2026-09-08/manifest.json').read_text(encoding='utf-8'))
files=list(prior['source_files'])+['packages/sim/src/campaignCityValidation.ts','packages/sim/test/campaignCityValidation.test.ts','packages/harness/src/cityValidationRun.ts','packages/harness/src/cityValidationCli.ts']
assert not (out/'manifest.json').exists(),'preserve frozen evidence'
sources={n:sha(root/n) for n in sorted(files)};dist=root/'packages/game/dist';built={p.relative_to(dist).as_posix():sha(p) for p in dist.rglob('*') if p.is_file()}
for name,data,base in [('source.zip',sources,root),('build.zip',built,dist)]:
 with zipfile.ZipFile(out/name,'w',zipfile.ZIP_DEFLATED) as z:
  for n in data:z.write(base/n,n)
m={'source_files':sources,'build_files':built,'archives':{n:sha(out/n) for n in ['source.zip','build.zip']},'parent_commit':prior['parent_commit'],'human_play':'not_run'}
(out/'manifest.json').write_text(json.dumps(m,indent=2)+'\n',encoding='utf-8',newline='\n')
print(f'Captured {len(sources)} source inputs and {len(built)} production files.')
