import {createCampaign,makeSave} from '../../../packages/sim/src/index';
for(let seed=1;seed<=32;seed++) {
  try { const st=createCampaign(seed),d=st.campaign!.discovery!; console.log(JSON.stringify({seed,cache:[d.x,d.y],guardian:d.guardian.stalkers.length,save:makeSave(st).hash})); }
  catch(error) { console.error(seed,String(error));process.exitCode=1; }
}
