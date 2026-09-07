/** npm run factory -- --only E-chain,E-coal,E-tram,E-logistics --seeds 3,4,5 --hours 5 */
import { readFileSync, writeFileSync, mkdirSync, readdirSync } from 'node:fs';
import { resolve, relative, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { createHash } from 'node:crypto';
import { CAMPAIGN_RULESET, CAMPAIGN_RULES } from '@relight/sim';
import { parseBlueprint } from './blueprint';
import { CAMPAIGN_EXPERIMENTS } from './campaignExperiments';
import { stamp } from './provenance';

const root=fileURLToPath(new URL('../../../',import.meta.url)),args=process.argv.slice(2);
const allowed=['--only','--seeds','--hours','--blueprint','--out'];
for(let i=0;i<args.length;i+=2)if(!allowed.includes(args[i])||!args[i+1]||args[i+1].startsWith('--'))throw new Error(`Unknown or incomplete argument ${args[i]}`);
const opt=(key:string,fallback:string)=>{const i=args.indexOf(key);return i<0?fallback:args[i+1];};
const names=opt('--only',Object.keys(CAMPAIGN_EXPERIMENTS).join(',')).split(','),seeds=opt('--seeds','3,4,5').split(',').map(Number),hours=Number(opt('--hours','5'));
if(names.some(n=>!Object.hasOwn(CAMPAIGN_EXPERIMENTS,n))||!seeds.length||seeds.some(n=>!Number.isSafeInteger(n)||n<1||n>100000)||!Number.isFinite(hours)||hours<=0||hours>5)throw new Error('Invalid experiment, seeds or hours (0 < hours <= 5)');
const blueprintPath=resolve(root,opt('--blueprint','packages/harness/blueprints/factory-line.json')),blueprint=parseBlueprint(JSON.parse(readFileSync(blueprintPath,'utf8')));
const out=resolve(root,opt('--out','docs/experiments/campaign'));if(!out.replace(/\\/g,'/').endsWith('/campaign'))throw new Error('Campaign evidence needs a dedicated /campaign output directory');
mkdirSync(out,{recursive:true});
const files:Record<string,string>={},sha=(v:string|Buffer)=>createHash('sha256').update(v).digest('hex');
function scan(dir:string):void {for(const entry of readdirSync(dir,{withFileTypes:true})){const p=join(dir,entry.name);if(entry.isDirectory())scan(p);else if(entry.name.endsWith('.ts'))files[relative(root,p).replace(/\\/g,'/')]=sha(readFileSync(p));}}
for(const p of ['sim','harness','game','tools'])scan(resolve(root,'packages',p,'src'));
files[relative(root,blueprintPath).replace(/\\/g,'/')]=sha(readFileSync(blueprintPath));
const source_tree_hash=sha(JSON.stringify(Object.entries(files).sort(([a],[b])=>a.localeCompare(b)))),prov=stamp({kind:'campaign',ruleset:CAMPAIGN_RULESET,opening:CAMPAIGN_RULES.opening});
let failures=0;
for(const name of names)for(const seed of seeds){
 const t=performance.now();console.log(`${name}, seed ${seed}: starting`);
 const r=CAMPAIGN_EXPERIMENTS[name as keyof typeof CAMPAIGN_EXPERIMENTS]({seed,hours,blueprint,progress:s=>console.log(s)});
 const failed=r.checks.filter(c=>!c.pass);failures+=failed.length;
 const payload={...prov,source_tree_hash,source_files:files,blueprint:relative(root,blueprintPath),blueprint_hash:sha(readFileSync(blueprintPath)),generated_at:new Date().toISOString(),wall_seconds:(performance.now()-t)/1000,...r};
 writeFileSync(join(out,`${name}-seed${seed}.json`),JSON.stringify(payload,null,1)+'\n');
 console.log(`${name}, seed ${seed}: ${r.checks.length-failed.length}/${r.checks.length} checks in ${payload.wall_seconds.toFixed(1)} s`);
 for(const c of failed)console.error(`FAIL ${c.name}: ${JSON.stringify(c.detail)}`);
}
if(failures)process.exitCode=1;
