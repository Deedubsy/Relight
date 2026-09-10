from pathlib import Path
import json
out=Path(__file__).resolve().parent
# Run projects.test.ts with UI05_FIXTURES first. The paid partial/powered/kit/radio starts are retained as captured.
p=out/'outage.json';f=json.loads(p.read_text());f['logComplete']=False;p.write_text(json.dumps(f),encoding='utf-8')
s=f['state'];d=s['campaign']['defence'];d['minor']={'id':2,'block':s['campaign']['homeBlock'],'origin':0,'retreat':False};d['nextId']=3;d['warning']={'assault':1,'block':s['campaign']['homeBlock'],'startsAt':900,'receivedAt':int(s['t']),'approach':'north','composition':'mostly crawlers'};f['log']=[]
(out/'known-threat.json').write_text(json.dumps(f),encoding='utf-8')
print('Prepared labelled outage and known-threat/received-warning fixtures; replay logs explicitly incomplete.')
