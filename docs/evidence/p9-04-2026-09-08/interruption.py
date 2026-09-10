from pathlib import Path
import subprocess,time,json,hashlib
root=Path(__file__).resolve().parents[3];out=Path(__file__).resolve().parent;target=out/'interruption-check'
cmd=['node','--import','tsx','packages/harness/src/cityValidationCli.ts','--seeds','3-8','--out',str(target)]
with (out/'interrupted.log').open('w',encoding='utf-8') as log:
 p=subprocess.Popen(cmd,cwd=root,stdout=log,stderr=subprocess.STDOUT)
 deadline=time.monotonic()+120
 while not list(target.glob('seed-*.json')):
  assert p.poll() is None,'runner exited before first seed'
  assert time.monotonic()<deadline,'first-seed timeout'
  time.sleep(.1)
 p.terminate();p.wait(timeout=30)
records={q.name:hashlib.sha256(q.read_bytes()).hexdigest() for q in target.glob('seed-*.json')};assert 0<len(records)<6
with (out/'interruption-resume.log').open('w',encoding='utf-8') as log:r=subprocess.run(cmd+['--resume','--workers','2'],cwd=root,stdout=log,stderr=subprocess.STDOUT)
assert r.returncode==0
assert all(hashlib.sha256((target/n).read_bytes()).hexdigest()==h for n,h in records.items())
s=json.loads((target/'summary.json').read_text(encoding='utf-8'));assert s['complete'] and s['passed']==6 and s['sourceUnchanged']
(out/'interruption.json').write_text(json.dumps({'interruptedExit':p.returncode,'retained':records,'attempted':s['attempted'],'passed':s['passed'],'retainedBytesUnchanged':True,'sourceUnchanged':True},indent=2)+'\n',encoding='utf-8',newline='\n')
print('PASS: terminated a live run, recovered its stale lock and resumed all missing seeds without replacing completed records.')
