from pathlib import Path
import json,collections
out=Path(__file__).resolve().parent;old=out.parent/'p9-04-2026-09-08'
read=lambda p:json.loads(p.read_text(encoding='utf-8'))
a=read(old/'analysis.json');b=read(out/'analysis.json');assert a['complete'] and b['complete']
transitions=collections.Counter();regressions=[];repaired=[];measurementsChanged=[];routesChanged=[];retryChanges=[]
for seed in range(1,10001):
 before=read(old/f'population/seed-{seed}.json')['data'];after=read(out/f'population/seed-{seed}.json')['data']
 transitions[before['status']+' -> '+after['status']]+=1
 if before['status']=='passed' and after['status']!='passed':regressions.append(seed)
 if before['status']=='failed' and after['status']=='passed':repaired.append(seed)
 bm=before.get('report',{}).get('measurements');am=after.get('report',{}).get('measurements')
 if bm and am:
  if bm!=am:measurementsChanged.append(seed)
  if bm['routeTiles']!=am['routeTiles']:routesChanged.append(seed)
 previousAttempt=bm['generatorAttempt'] if bm else read(old/f'retry-metadata/seed-{seed}.json')['generatorAttempt']
 if am and previousAttempt!=am['generatorAttempt']:retryChanges.append(seed)
classes=[{'class':k,'baseline':len(v['seeds']),'nowPassed':sum(s in repaired for s in v['seeds']),'seeds':v['seeds']} for k,v in a['failureClasses'].items()]
r={'declared':10000,'transitions':dict(transitions),'repairedSeeds':repaired,'regressions':regressions,'classes':classes,'baseGeneratorAttemptChanges':retryChanges,'composedMeasurementChangesAmongPreviouslyInitialized':measurementsChanged,'routeLengthChangesAmongPreviouslyInitialized':routesChanged,'baselineSourceDigest':read(old/'population/manifest.json')['sourceDigest'],'sourceDigest':read(out/'population/manifest.json')['sourceDigest'],'baselineFingerprint':read(old/'population/manifest.json')['config_hash'],'fingerprint':read(out/'population/manifest.json')['config_hash'],'scope':'Paired original seeds and unchanged validator. No human fairness, safe-combat, reference performance or universal-seed guarantee.'}
(out/'comparison.json').write_text(json.dumps(r,indent=2)+'\n',encoding='utf-8',newline='\n');print(json.dumps({k:r[k] for k in ['transitions','regressions','baseGeneratorAttemptChanges']}))
