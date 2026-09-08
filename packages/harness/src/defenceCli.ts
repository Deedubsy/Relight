import {writeFileSync,mkdirSync,existsSync,readFileSync,readdirSync} from 'node:fs';
import {resolve,relative,join} from 'node:path';
import {createHash} from 'node:crypto';
import {CAMPAIGN_RULESET,CAMPAIGN_RULES} from '@relight/sim';
import {stamp} from './provenance';
import {runDefence} from './defenceScenario';
const args=process.argv.slice(2),allowed=['--seeds','--mode','--out'];
for(let i=0;i<args.length;i+=2)if(!allowed.includes(args[i])||!args[i+1])throw Error('Invalid argument '+args[i]);
const opt=(key:string,fallback:string)=>args.includes(key)?args[args.indexOf(key)+1]:fallback;
const seeds=opt('--seeds','3,4,5,8,11,13').split(',').map(Number),mode=opt('--mode','turrets'),out=resolve(opt('--out','docs/evidence/p6-03-2026-09-07/campaign'));
if(seeds.some(s=>!Number.isSafeInteger(s)||s<1)||!['turrets','support'].includes(mode)||!out.replace(/\\/g,'/').endsWith('/campaign'))throw Error('Invalid seeds, mode or campaign output directory');
for(const seed of seeds)if(existsSync(join(out,`E-defence-${mode}-seed${seed}.json`)))throw Error('Refusing to overwrite existing evidence');
mkdirSync(out,{recursive:true});
const sources:Record<string,string>={},sha=(b:Buffer|string)=>createHash('sha256').update(b).digest('hex');
function scan(dir:string):void{for(const e of readdirSync(dir,{withFileTypes:true})){const p=join(dir,e.name);if(e.isDirectory())scan(p);else if(e.name.endsWith('.ts'))sources[relative(process.cwd(),p).replace(/\\/g,'/')]=sha(readFileSync(p));}}
for(const name of ['sim','harness','game','tools'])scan(resolve('packages',name,'src'));
const provenance=stamp({kind:'campaign',ruleset:CAMPAIGN_RULESET,opening:CAMPAIGN_RULES.opening});let failures=0;
for(const seed of seeds){console.log('Starting live defence',seed,mode);const started=performance.now(),r=runDefence(seed,mode as 'turrets'|'support',console.log);const failed=r.checks.filter(c=>!c.pass);failures+=failed.length;
writeFileSync(join(out,`E-defence-${mode}-seed${seed}.json`),JSON.stringify({...provenance,source_files:sources,source_tree_hash:sha(JSON.stringify(sources)),generated_at:new Date().toISOString(),wall_seconds:(performance.now()-started)/1000,...r},null,1)+'\n');console.log('Finished',seed,mode,r.checks.length-failed.length+'/'+r.checks.length);}
if(failures)process.exitCode=1;
