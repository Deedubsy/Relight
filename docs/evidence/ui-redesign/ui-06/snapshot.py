from pathlib import Path
import hashlib,json,zipfile,difflib,subprocess
root=Path(__file__).resolve().parents[4];out=Path(__file__).resolve().parent;prior=out.parent/'ui-05'
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
old=json.loads((prior/'manifest.json').read_text());files=sorted(set(old['source_files'])|{'packages/game/src/settingsPanel.ts','packages/sim/test/uiSettings.test.ts'})
sources={n:sha(root/n) for n in files};changes=[n for n,h in sources.items() if old['source_files'].get(n)!=h]
assert all(n.startswith('packages/game/') or n=='packages/sim/test/uiSettings.test.ts' for n in changes)
dist=root/'packages/game/dist';build={p.relative_to(dist).as_posix():sha(p) for p in dist.rglob('*') if p.is_file()}
m={'parent_commit':subprocess.check_output(['git','rev-parse','HEAD'],cwd=root,text=True).strip(),'source_files':sources,'build_files':build,'changes':changes,'config_hash':'ba11e896','human_play':'not_run','archives':{}}
for name,key,base in [('source.zip','source_files',root),('build.zip','build_files',dist)]:
 with zipfile.ZipFile(out/name,'w',zipfile.ZIP_DEFLATED) as z:
  for n in m[key]:z.write(base/n,n)
 m['archives'][name]=sha(out/name)
patch=[]
with zipfile.ZipFile(prior/'source.zip') as z:
 for n in changes:
  before=z.read(n).decode().replace('\r\n','\n').splitlines(True) if n in old['source_files'] else []
  patch.extend(difflib.unified_diff(before,(root/n).read_text(encoding='utf-8').splitlines(True),fromfile='UI-05/'+n,tofile='UI-06/'+n))
(out/'source-delta.patch').write_text(''.join(patch),encoding='utf-8',newline='\n')
(out/'manifest.json').write_text(json.dumps(m,indent=2)+'\n',encoding='utf-8',newline='\n')
print('Archived',len(sources),'source inputs;',len(build),'served files;',len(changes),'changed files; simulation, save and harness sources unchanged.')
