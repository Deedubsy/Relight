from pathlib import Path
import hashlib,json,zipfile,shutil,subprocess
root=Path(__file__).resolve().parents[3];out=Path(__file__).resolve().parent;prior=root/'docs/evidence/p8-05-2026-09-08'
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
old=json.loads((prior/'manifest.json').read_text(encoding='utf-8'));dist=root/'packages/game/dist'
for n,h in old['source_files'].items():assert sha(root/n)==h,n
for n,h in old['build_files'].items():assert sha(dist/n)==h,n
assert not (out/'build-manifest.json').exists(),'preserve frozen build'
served={n:dist/n for n in old['build_files'] if n not in ['truck-ready.json','ammo-ready.json']}
served.update({n+'.json':out/'campaign'/(n+'.json') for n in ['fresh','construction','network','turbine']});served['settings.json']=out/'settings.json'
shutil.copyfile(prior/'source.zip',out/'source.zip')
with zipfile.ZipFile(out/'playtest-build.zip','w',zipfile.ZIP_DEFLATED) as z:
 for n,p in served.items():z.write(p,n)
a={'session':'EX-08H-D / P6-H / P7-H / P8-H / T18','parent_commit':subprocess.check_output(['git','rev-parse','HEAD'],cwd=root,text=True).strip(),'url':'http://127.0.0.1:5180/?rules=exploration-v2&seed=3&view=world&state=/fresh.json','source_files':old['source_files'],'served_files':{n:sha(p) for n,p in served.items()},'archives':{n:sha(out/n) for n in ['source.zip','playtest-build.zip']},'settings_sha256':sha(out/'settings.json'),'human_play':'not_run'}
(out/'build-manifest.json').write_bytes(json.dumps(a,indent=2).encode('utf-8'));print(f'Frozen {len(a["source_files"])} source inputs and {len(served)} served files matching reviewed local P8-05.');print(a['url'])
