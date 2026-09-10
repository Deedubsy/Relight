from pathlib import Path
import json,collections,re,math
out=Path(__file__).resolve().parent
rows=sorted((json.loads(p.read_text(encoding='utf-8'))['data'] for p in (out/'population').glob('seed-*.json')),key=lambda r:r['seed'])
def distribution(values):
 values=sorted(values)
 if not values:return {'n':0}
 return {'n':len(values),'min':values[0],'p05':values[math.ceil(.05*len(values))-1],'median':values[math.ceil(.5*len(values))-1],'p95':values[math.ceil(.95*len(values))-1],'p99':values[math.ceil(.99*len(values))-1],'max':values[-1],'mean':sum(values)/len(values)}
classes=collections.defaultdict(list)
for r in rows:
 if r['status']=='failed':
  findings=r.get('report',{}).get('failures',[]) or [r.get('failure',{'code':'invariance','reason':'repeat/load/non-mutation disagreement'})]
  for f in findings:classes[f['code']+': '+re.sub(r'\d+','#',f.get('reason',''))].append({'seed':r['seed'],**f})
reports=[r['report'] for r in rows if r.get('report')]
metrics={k:distribution([r['measurements'][k] for r in reports]) for k in ['reachableTiles','routeTiles','generatorAttempt']}
sites=sorted({s['id'] for r in reports for s in r['measurements']['sites']})
metrics['interactionSteps']={k:distribution([s['cardinalSteps'] for r in reports for s in r['measurements']['sites'] if s['id']==k and s['cardinalSteps'] is not None]) for k in sites}
metrics['homeSalvage']={k:{field:distribution([s[field] for r in reports for s in r['measurements']['resources'] if s['item']==k]) for field in ['reachableTiles','units']} for k in ['steel','copper','coal']}
metrics['extractionAvailable']={k:sum(s['footprint'] is not None for r in reports for s in r['measurements']['extraction'] if s['item']==k) for k in ['steel','copper','coal']}
timings={group:{phase:distribution([r['wallMs'] if phase=='wallMs' else r['timings'][phase] for r in subset if phase=='wallMs' or phase in r['timings']]) for phase in ['wallMs','generationMs','firstQueryMs','repeatQueryMs','saveLoadMs','loadedQueryMs']} for group,subset in [('coldProcess',[r for r in rows if r['processSample']==0]),('warmProcess',[r for r in rows if r['processSample']>0])]}
summary={'declared':10000,'attempted':len(rows),'complete':len(rows)==10000,'passed':sum(r['status']=='passed' for r in rows),'failed':sum(r['status']=='failed' for r in rows),'failureClasses':{k:{'count':len(v),'seeds':sorted(set(r['seed'] for r in v)),'representative':v[0],'findings':v} for k,v in sorted(classes.items())},'metrics':metrics,'timings':timings,'retryIndexKnown':len(reports),'retryIndexNeedsGenerationDiagnostic':sorted(r['seed'] for r in rows if not r.get('report')),'quantiles':'Nearest rank, no interpolation. Each metric reports its own sample size; failed initialization has no composed map. Cold is process-first, not OS-cache-cold. Warm timings include concurrent workers; no reference-machine certification.','human_play':'not_run'}
retry=[];unavailable=[]
for r in rows:
 if r.get('report'):retry.append(r['report']['measurements']['generatorAttempt'])
 else:
  p=out/f"retry-metadata/seed-{r['seed']}.json"
  if p.exists():
   metadata=json.loads(p.read_text(encoding='utf-8'))
   if metadata['generatorAttempt'] is not None:retry.append(metadata['generatorAttempt'])
   else:unavailable.append(r['seed'])
  else:unavailable.append(r['seed'])
summary['baseGeneratorRetries']={'distribution':distribution(retry),'histogram':dict(sorted(collections.Counter(retry).items())),'unavailableSeeds':unavailable,'method':'Zero-based successful base-city attempt index equals preceding rejected jitter attempts. Composed initialization failures use separately retained deterministic base-generation replays, never replacement campaign seeds.'}
(out/'analysis.json').write_text(json.dumps(summary,indent=2)+'\n',encoding='utf-8',newline='\n')
print(f"{len(rows)}/10000 attempted; {summary['failed']} failed; {len(classes)} distinct code/reason classes")
for k,v in summary['failureClasses'].items():print(k, v['count'], 'representative', v['representative']['seed'])
