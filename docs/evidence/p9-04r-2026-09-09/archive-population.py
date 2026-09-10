from pathlib import Path
import json,hashlib,zipfile
out=Path(__file__).resolve().parent;pop=out/'population';s=json.loads((pop/'summary.json').read_text(encoding='utf-8'));assert s['complete'] and s['attempted']==10000 and s['sourceUnchanged'] and not (pop/'run.lock').exists()
files={p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in pop.iterdir() if p.is_file()}
with zipfile.ZipFile(out/'population.zip','x',zipfile.ZIP_DEFLATED) as z:
 for name in sorted(files):z.write(pop/name,name)
m={'archive_sha256':hashlib.sha256((out/'population.zip').read_bytes()).hexdigest(),'files':files,'raw_files_retained':True}
(out/'population-archive.json').write_text(json.dumps(m,indent=2)+'\n',encoding='utf-8',newline='\n');print('Archived',len(files),'population records without removing raw files.')
