import {readFileSync,writeFileSync,existsSync} from 'node:fs';
import assert from 'node:assert/strict';
import {createCampaign,makeSave,stateHash,loadState,ensureFlow,conservation,CAMPAIGN_RULES,CAMPAIGN_THREAT,DEFENCE,DISTRICT_ECONOMY,DISCOVERY} from '../../../packages/sim/src/index';
import {replayInterval,savedContinuation} from '../../../packages/harness/src/blueprint';
import {stamp} from '../../../packages/harness/src/provenance';
const out='docs/evidence/ex08c-2026-09-08',ref={kind:'campaign',ruleset:'exploration-v2',opening:'culdesac-v1'} as const,provenance=stamp(ref);
const evidence=JSON.parse(readFileSync('docs/evidence/p7-05-2026-09-08/campaign/E-defence-turrets-seed3.json','utf8'));
const initial=createCampaign(3);assert.equal(stateHash(initial),stateHash(evidence.initialState));
const tick=2390*20,log=evidence.log.filter((l:any)=>l.tick<=tick),network=replayInterval(initial,log,tick);
assert.equal(network.t,2390);assert.equal(network.campaign!.defence!.major,null);assert.equal(network.campaign!.defence!.nextDawn,2400);
assert.ok(network.campaign!.expansion!.radio.restoredAt>=0);assert.ok(network.campaign!.defence!.bases.every(b=>b.hp>0));
const pending=JSON.parse(readFileSync('docs/evidence/p7-03-2026-09-08/turbine-pending.json','utf8')),turbine=replayInterval(initial,pending.log,pending.state.flow.tick);
assert.equal(turbine.campaign!.turbine!.restoredAt,-1);assert.equal(turbine.engineer.inv.concrete,40);
const starts=[];
for(const [name,st,commands,description] of [['fresh',initial,[],'Untouched ordinary tick-zero campaign; no assistance.'],['network',network,log,'Scripted opening replayed from ordinary starting stock and full paid command history; player did not perform the opening. No resource/position/clock/HP injection. Not an opening-time or unaided discovery observation.'],['turbine',turbine,pending.log,'Optional assisted restoration start: ordinary paid mining, concrete production, travel and local generator prepared; crew discoveries already known. Do not use for unaided discovery.']] as const){
 const save=makeSave(st,{log:[...commands],logComplete:true,params:{ruleset:'exploration-v2',seed:3,view:'world'}}),loaded=loadState(save);ensureFlow(loaded);
 assert.ok(conservation(structuredClone(st)).ok);assert.ok(savedContinuation(st).same);assert.equal(loadState(save).speed,0);
 const file=out+'/campaign/'+name+'.json';assert.ok(!existsSync(file));writeFileSync(file,JSON.stringify({...save,...provenance},null,1)+'\n');
 starts.push({name,file,description,hash:save.hash,browserNormalizedHash:stateHash(loaded),tick:st.flow!.tick,t:st.t,logEntries:commands.length,radioRestored:st.campaign!.expansion!.radio.restoredAt>=0,engineer:st.engineer,bases:st.campaign!.defence!.bases,stock:st.stock,store:st.flow!.store,buffer:st.buffer,machines:st.flow!.machines.map(m=>({id:m.id,kind:m.kind,x:m.x,y:m.y,dir:m.dir,inv:m.inv,cargo:m.cargo})),nextDawn:st.campaign!.defence!.nextDawn});
}
const settings={...provenance,session:'EX-08H-C / P6-H / P7-H',human_play:'not_run',starts,defaultStart:'fresh',speed:'1x; pauses/interventions recorded',difficulty:'unchanged default campaign rules',config:initial.config,clock:CAMPAIGN_RULES,threat:CAMPAIGN_THREAT,defence:DEFENCE,districts:DISTRICT_ECONOMY,discovery:DISCOVERY};
assert.ok(!existsSync(out+'/settings.json'));writeFileSync(out+'/settings.json',JSON.stringify(settings,null,2)+'\n');console.log(JSON.stringify(starts.map(s=>({name:s.name,t:s.t,hash:s.hash,browser:s.browserNormalizedHash,logEntries:s.logEntries,bases:s.bases.map(b=>({block:b.block,hp:b.hp}))})),null,2));
console.log('PASS: three starts and ordinary-command prefixes verified, conserved, load paused and continue deterministically.');
