from pathlib import Path
import subprocess,json,hashlib
root=Path(__file__).resolve().parents[3];out=Path(__file__).resolve().parent
cases=[('active-lock',['--seeds','1-10000','--out',str(out/'population'),'--resume'],'already running'),('population-mismatch',['--seeds','3-5','--out',str(out/'resume-check'),'--resume'],'resume identity differs: seeds'),('existing-directory',['--seeds','3-4','--out',str(out/'resume-check')],'output directory exists')]
results=[]
for name,args,expected in cases:
 paths=list((out/'resume-check').glob('seed-*.json'));before={p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in paths}
 r=subprocess.run(['node','--import','tsx','packages/harness/src/cityValidationCli.ts',*args],cwd=root,text=True,capture_output=True)
 (out/(name+'.log')).write_text(r.stdout+r.stderr,encoding='utf-8',newline='\n')
 assert r.returncode!=0 and expected in r.stderr,(name,r.stderr)
 assert all(hashlib.sha256(p.read_bytes()).hexdigest()==before[p.name] for p in paths)
 results.append({'case':name,'exitCode':r.returncode,'expected':expected,'retainedRowsUnchanged':True})
(out/'cli-refusals.json').write_text(json.dumps(results,indent=2)+'\n',encoding='utf-8',newline='\n')
print('PASS: active lock, population mismatch and existing directory refuse; completed seed bytes preserved.')
