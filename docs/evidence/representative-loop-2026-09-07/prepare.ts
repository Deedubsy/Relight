/** EX-08A preparation only: untouched tick-zero campaign, paused by the normal save API. */
import { writeFileSync } from 'node:fs';
import { createCampaign, makeSave, stateHash, CAMPAIGN_RULES, CAMPAIGN_THREAT, DISCOVERY, DEFENCE, DISTRICT_ECONOMY, conservation } from '../../../packages/sim/src/index';
import { stamp } from '../../../packages/harness/src/provenance';
const st=createCampaign(3),ref={kind:'campaign',ruleset:'exploration-v2',opening:'culdesac-v1'} as const;
const provenance=stamp(ref),save=makeSave(st,{log:[],logComplete:true,params:{ruleset:'exploration-v2',seed:3,view:'world'}});
if(st.t!==0||st.flow!.tick!==0||!conservation(structuredClone(st)).ok||stateHash(save.state)!==stateHash(st))throw new Error('not an unchanged fresh campaign');
writeFileSync('docs/evidence/representative-loop-2026-09-07/start.json',JSON.stringify({...save,...provenance},null,2)+'\n');
const settings={...provenance,generated_at:new Date().toISOString(),session:'EX-08H-A',human_play:'not_run',seed:3,
  ruleset:st.ruleset,save_schema:st.version,campaign_metadata:st.campaign!.version,initial_hash:stateHash(st),initial_tick:st.flow!.tick,
  start:'fresh, paused tick-zero save; no injected stock, progress, elapsed time or debug enemies',
  difficulty:'default campaign tuning; no separate difficulty selector',speed:'1x during measured play; pauses logged separately',
  clock:CAMPAIGN_RULES,first_lock_seconds:st.campaign!.defence!.nextDawn,first_attack_seconds:st.campaign!.defence!.nextDawn+900,
  threat:CAMPAIGN_THREAT,defence:DEFENCE,districts:DISTRICT_ECONOMY,discovery:DISCOVERY,
  initial_stock:st.stock,initial_buffer_rounds:st.buffer,initial_pockets:st.engineer.inv,initial_store:st.flow!.store,
  initial_machines:st.flow!.machines.map(m=>({id:m.id,kind:m.kind,x:m.x,y:m.y,inventory:m.inv})),
  radio:'unrestored; major target is home until the tower is restored; powered tower receives the actual lock',
  config:st.config};
writeFileSync('docs/evidence/representative-loop-2026-09-07/settings.json',JSON.stringify(settings,null,2)+'\n');
console.log(JSON.stringify({initial_hash:settings.initial_hash,tick:settings.initial_tick,config_hash:provenance.config_hash,source_commit:provenance.source_commit,stock:st.stock,buffer:st.buffer,pockets:st.engineer.inv,store:st.flow!.store}));
