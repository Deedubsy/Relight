import { createCampaign, validateCampaignCity, loadState, makeSave, ensureFlow, stateHash, type SimState } from '@relight/sim';

export function citySeeds(text:string):number[] {
 const seeds:number[]=[];
 for(const part of text.split(',')){
  const match=/^(\d+)(?:-(\d+))?$/.exec(part);if(!match)throw Error('seeds must be integers or ascending ranges');
  const lo=Number(match[1]),hi=Number(match[2]??match[1]);
  if(!Number.isSafeInteger(lo)||!Number.isSafeInteger(hi)||hi<lo||hi>2147483647||hi-lo>9999)throw Error('invalid seed range');
  for(let seed=lo;seed<=hi;seed++){if(seeds.length>=10000||seeds.includes(seed))throw Error('at most 10000 distinct seeds');seeds.push(seed);}
 }
 return seeds;
}

/** A thrown generator is retained as a failed attempted seed, never replaced by a different seed. */
export function cityValidationRun(seed:number,create:(seed:number)=>SimState=createCampaign) {
 const began=performance.now();
 const timings:{generationMs?:number;firstQueryMs?:number;repeatQueryMs?:number;saveLoadMs?:number;loadedQueryMs?:number}={};
 try {
  const state=create(seed);timings.generationMs=performance.now()-began;
  const initial=structuredClone(state);let phase=performance.now();const report=validateCampaignCity(state);timings.firstQueryMs=performance.now()-phase;
  phase=performance.now();const repeated=validateCampaignCity(state);timings.repeatQueryMs=performance.now()-phase;
  phase=performance.now();const loaded=loadState(makeSave(state));ensureFlow(loaded);timings.saveLoadMs=performance.now()-phase;
  phase=performance.now();const resumed=validateCampaignCity(loaded);timings.loadedQueryMs=performance.now()-phase;
  const unchanged=JSON.stringify(state)===JSON.stringify(initial),deterministic=JSON.stringify(report)===JSON.stringify(repeated),loadSame=JSON.stringify(report)===JSON.stringify(resumed);
  return {seed,status:report.ok&&unchanged&&deterministic&&loadSame?'passed':'failed',report,unchanged,deterministic,loadSame,hash:stateHash(state),timings,wallMs:performance.now()-began};
 } catch(error){return {seed,status:'failed',failure:{code:'generation-or-run',reason:(error as Error).message},timings,wallMs:performance.now()-began};}
}
