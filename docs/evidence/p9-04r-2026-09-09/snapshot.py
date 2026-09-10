from pathlib import Path
import json,hashlib,zipfile,difflib,subprocess
root=Path(__file__).resolve().parents[3];out=Path(__file__).resolve().parent
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
prior=json.loads((root/'docs/evidence/p9-04-2026-09-08/manifest.json').read_text(encoding='utf-8'))
files=sorted(set(prior['source_files'])|{'packages/sim/src/campaignSurvey.ts'})
m={'source_files':{n:sha(root/n) for n in files},'build_files':{p.relative_to(root/'packages/game/dist').as_posix():sha(p) for p in sorted((root/'packages/game/dist').rglob('*')) if p.is_file()},'parent_commit':subprocess.check_output(['git','rev-parse','HEAD'],cwd=root,text=True).strip(),'human_play':'not_run','archives':{}}
for archive,key,base in [('source.zip','source_files',root),('build.zip','build_files',root/'packages/game/dist')]:
 with zipfile.ZipFile(out/archive,'w',zipfile.ZIP_DEFLATED) as z:
  for n in m[key]:z.write(base/n,n)
 m['archives'][archive]=sha(out/archive)
patch=[]
with zipfile.ZipFile(root/'docs/evidence/p9-04-2026-09-08/source.zip') as z:
 for n in files:
  if n in prior['source_files'] and prior['source_files'][n]==m['source_files'][n]:continue
  before=z.read(n).decode('utf-8').splitlines(keepends=True) if n in prior['source_files'] else []
  after=(root/n).read_text(encoding='utf-8').splitlines(keepends=True)
  patch.extend(difflib.unified_diff([s.rstrip('\r\n')+'\n' for s in before],after,fromfile='p9-04/'+n,tofile='p9-04r/'+n))
(out/'source-delta.patch').write_text(''.join(patch),encoding='utf-8',newline='\n')
(out/'manifest.json').write_text(json.dumps(m,indent=2)+'\n',encoding='utf-8',newline='\n')
print(len(files),'source files;',len(m['build_files']),'production files archived')
