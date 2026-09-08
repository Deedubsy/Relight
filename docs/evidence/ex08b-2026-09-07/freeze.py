from pathlib import Path
import hashlib,json,zipfile
root=Path(__file__).resolve().parents[3];out=Path(__file__).resolve().parent
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
old=json.loads((root/'docs/evidence/p6-04-2026-09-07/review-manifest.json').read_text())
for group in ['source_files','build_files']:
 for n,h in old[group].items():assert sha(root/n)==h,n
manifest=out/'build-manifest.json';assert not manifest.exists(),'preserve frozen build'
served={p.relative_to(root/'packages/game/dist').as_posix():p for p in (root/'packages/game/dist').rglob('*') if p.is_file()}
served.update({'fresh.json':out/'campaign/fresh.json','network.json':out/'campaign/network.json','settings.json':out/'settings.json'})
for filename,files in [('source.zip',{n:root/n for n in old['source_files']}),('playtest-build.zip',served)]:
 assert not (out/filename).exists(),filename
 with zipfile.ZipFile(out/filename,'w',zipfile.ZIP_DEFLATED) as z:
  for n,p in files.items():z.write(p,n)
a={'session':'EX-08H-B / P6-H','parent_commit':old['parent_commit'],'url':'http://127.0.0.1:5177/?rules=exploration-v2&seed=3&view=world&state=/network.json','fresh_url':'http://127.0.0.1:5177/?rules=exploration-v2&seed=3&view=world&state=/fresh.json','source_files':old['source_files'],'served_files':{n:sha(p) for n,p in served.items()},'archives':{n:sha(out/n) for n in ['source.zip','playtest-build.zip']},'settings_sha256':sha(out/'settings.json'),'human_play':'not_run'}
manifest.write_bytes((json.dumps(a,indent=2)+'\n').encode());print('Frozen',len(a['source_files']),'source inputs and',len(a['served_files']),'served files.');print(a['url']);print(a['archives'])
