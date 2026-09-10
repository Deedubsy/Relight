/** P9-04: declared, resumable population with immutable seed records and separate process/query timings. */
import {readdirSync,readFileSync,writeFileSync,mkdirSync,existsSync,unlinkSync,renameSync} from 'node:fs';
import {resolve,relative,join} from 'node:path';
import {createHash,randomUUID} from 'node:crypto';
import {fork,type ChildProcess} from 'node:child_process';
import {hostname,cpus} from 'node:os';
import {citySeeds} from './cityValidationRun';
import {stamp} from './provenance';
import {checkPopulationIdentity,readPopulationRows,writePopulationRow,type PopulationRow} from './cityPopulation';

const args=process.argv.slice(2),options=new Map<string,string>();
for(let i=0;i<args.length;i++){
 const key=args[i];if(!['--seeds','--out','--resume','--workers','--limit'].includes(key)||options.has(key))throw Error('usage: city:validate --seeds 1-10000 --out DIRECTORY [--resume] [--workers 8] [--limit 100]');
 const value=key==='--resume'?'true':args[++i];if(!value)throw Error(`missing ${key}`);options.set(key,value);
}
if(!options.get('--out'))throw Error('--out required');
const seeds=citySeeds(options.get('--seeds')??'3,4,5,8,11,13'),out=resolve(options.get('--out')!),resume=options.has('--resume');
const integer=(key:string,fallback:number)=>{const text=options.get(key)??String(fallback);if(!/^\d+$/.test(text)||!Number.isSafeInteger(Number(text))||Number(text)<1)throw Error(`invalid ${key}`);return Number(text);};
const workers=integer('--workers',1),limit=integer('--limit',10000);if(workers>16)throw Error('at most 16 workers');
if(existsSync(out)!==resume)throw Error(resume?'resume directory missing':'output directory exists; use --resume with the same population and source');
const root=process.cwd();
function hashes(){
 const files:string[]=[];
 function collect(dir:string){for(const e of readdirSync(dir,{withFileTypes:true})){const p=join(dir,e.name);if(e.isDirectory())collect(p);else if(/\.(ts|json)$/.test(e.name))files.push(p);}}
 for(const dir of ['packages/sim/src','packages/harness/src'])collect(resolve(dir));
 for(const p of ['package.json','package-lock.json','packages/sim/package.json','packages/harness/package.json'])files.push(resolve(p));files.sort();
 return Object.fromEntries(files.map(p=>[relative(root,p).replaceAll('\\','/'),createHash('sha256').update(readFileSync(p)).digest('hex')]));
}
const sourceFiles=hashes(),sourceDigest=createHash('sha256').update(JSON.stringify(sourceFiles)).digest('hex'),provenance=stamp({kind:'campaign',ruleset:'exploration-v2',opening:'culdesac-v1'});
const identity={...provenance,sourceDigest,sourceFiles,seeds,runtime:process.version,version:2};
if(!resume)mkdirSync(out,{recursive:true});
const lock=join(out,'run.lock');
if(existsSync(lock)){
 const prior=JSON.parse(readFileSync(lock,'utf8'));if(prior.host!==hostname())throw Error('lock belongs to another host');
 let alive=true;try{process.kill(prior.pid,0);}catch(e){if((e as NodeJS.ErrnoException).code==='ESRCH')alive=false;else throw e;}
 if(alive)throw Error('population is already running');unlinkSync(lock);
}
writeFileSync(lock,JSON.stringify({pid:process.pid,host:hostname()}),{flag:'wx'});
const write=(name:string,data:unknown)=>writeFileSync(join(out,name),JSON.stringify(data,null,2)+'\n',{flag:'wx'});
const children:ChildProcess[]=[];
try{
 if(resume)checkPopulationIdentity(JSON.parse(readFileSync(join(out,'manifest.json'),'utf8')),identity);
 else write('manifest.json',{...identity,method:'Every declared seed: fresh campaign, private-copy physical query, repeat query and save/load query with equality and non-mutation checks. No reroll beyond existing generator attempts. Immutable checksummed seed records; failed seeds stay attempted on resume. Cold means first sample in a fresh worker process, not cold OS disk cache. Warm process samples and per-query phases are separate. Query geometry is not paid logistics or human fairness.',human_play:'not_run'});
 const rows=readPopulationRows(out,identity),pending=seeds.filter(s=>!rows.has(s)).slice(0,limit),session=randomUUID(),began=performance.now();
 const start={session,started:new Date().toISOString(),workers:Math.min(workers,pending.length),host:hostname(),cpu:cpus()[0]?.model,runtime:process.version,resume,retained:rows.size,pending:pending.length,...provenance,sourceDigest};
 write(`session-${session}-start.json`,start);
 let cursor=0,completed=0;
 await Promise.all(Array.from({length:Math.min(workers,pending.length)},()=>new Promise<void>((done,reject)=>{
  const child=fork(new URL('./cityValidationWorker.ts',import.meta.url),[],{execArgv:process.execArgv,stdio:['ignore','inherit','inherit','ipc']});children.push(child);let assigned:number|undefined;
  const next=()=>{if(cursor>=pending.length){assigned=undefined;child.disconnect();done();return;}assigned=pending[cursor++];child.send(assigned);};
  child.on('error',reject);child.on('exit',(code)=>{if(assigned!==undefined)reject(Error(`worker exited ${code} on seed ${assigned}; resume retains completed rows`));});
  child.on('message',(row:PopulationRow)=>{try{
   if(row.seed!==assigned)throw Error('worker returned an unassigned seed');
   writePopulationRow(out,identity,row);rows.set(row.seed,row);completed++;
   console.log(`${rows.size}/${seeds.length}: seed ${row.seed} ${row.status}; ${row.report?.failures.length??1} findings; ${Math.round(row.wallMs)} ms`);next();
  }catch(e){reject(e);}});next();
 })));
 const sourceUnchanged=JSON.stringify(hashes())===JSON.stringify(sourceFiles),ordered=seeds.flatMap(s=>rows.has(s)?[rows.get(s)!]:[]),failed=ordered.filter(r=>r.status==='failed').length;
 const summary={...provenance,sourceDigest,sourceUnchanged,declared:seeds.length,attempted:rows.size,passed:rows.size-failed,failed,complete:rows.size===seeds.length,rows:ordered.map(r=>({seed:r.seed,status:r.status,failures:r.report?.failures??[r.failure],wallMs:r.wallMs,processSample:r.processSample}))};
 write(`session-${session}-end.json`,{...start,completed,wallMs:performance.now()-began,sourceUnchanged,attempted:rows.size,failed});
 const temp=join(out,`summary-${session}.pending`);writeFileSync(temp,JSON.stringify(summary,null,2)+'\n',{flag:'wx'});renameSync(temp,join(out,'summary.json'));
 if(failed||!sourceUnchanged)process.exitCode=1;
 console.log(`population ${summary.complete?'complete':'PARTIAL'}: ${rows.size}/${seeds.length}, ${failed} failed; source unchanged: ${sourceUnchanged}`);
}finally{for(const child of children)if(child.connected)child.kill();unlinkSync(lock);}
