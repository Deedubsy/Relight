/** Review-only scenarios: clock jumps and engineer relocation isolate locked-origin liveness.
 * No campaign balance, normal travel time or production-code change is claimed. */
import assert from 'node:assert/strict';
import { createCampaign, ground, applyCommands, machineAt, advanceFlow, campaignWarning,
  loadState, makeSave, stateHash, passable, campaignOrigin, registerBase } from '../../../packages/sim/src/index';
import { tickCampaignSchedule } from '../../../packages/sim/src/campaignThreat';
import { threatOf } from '../../../packages/sim/src/threat';

const origins: {seed:number; base:number; valid:boolean}[]=[];
for(let seed=1;seed<=32;seed++) {
  const st=createCampaign(seed);
  // Registry-only fixture: no claim of paid restoration or a built freight route.
  for(const site of [st.campaign!.expansion!.station,st.campaign!.districts!.station]) {
    site.restoredAt=0;registerBase(st,site.block);
  }
  const G=ground(st);
  for(const base of st.campaign!.defence!.bases) {
    const origin=campaignOrigin(st,base),valid=origin>=0&&passable(st,origin%G.tw,Math.floor(origin/G.tw));
    origins.push({seed,base:base.block,valid});assert.ok(valid,`seed ${seed}, base ${base.block}`);
  }
}
console.log(JSON.stringify({scenario:'fresh geometry with registry fixtures',originsChecked:origins.length,allValid:origins.every(o=>o.valid)}));

const st=createCampaign(),d=st.campaign!.defence!,G=ground(st);
applyCommands(st,[{type:'chestTake',item:'steel',n:2}]);
// Avoid unrelated site/minor damage while testing one locked assault origin.
d.sites=[];d.lastMinorSlot=100000;
st.t=2400;tickCampaignSchedule(st,threatOf(st.flow!));
const a=d.major!;assert.ok(a);
const x=a.origin%G.tw,y=Math.floor(a.origin/G.tw),before={...st.engineer};
const adjacent=[[x-1,y],[x+1,y],[x,y-1],[x,y+1]].find(([xx,yy])=>passable(st,xx,yy));
assert.ok(adjacent);
st.engineer.x=adjacent[0]+.5;st.engineer.y=adjacent[1]+.5;
applyCommands(st,[{type:'place',item:'wall',x,y}]);assert.equal(machineAt(st,x,y)?.kind,'wall');
st.engineer.x=before.x;st.engineer.y=before.y;
st.t=3300;advanceFlow(st,120,[],2500);
const resumed=loadState(makeSave(st));resumed.speed=st.speed;
advanceFlow(st,120,[],2500);advanceFlow(resumed,120,[],2500);
assert.equal(stateHash(st),stateHash(resumed));
assert.equal(d.major?.id,a.id);assert.equal(d.majorSpawned,0);assert.equal(d.major?.remaining,60);
console.log(JSON.stringify({scenario:'paid wall placed on origin after dawn lock',elapsedAfterDusk:st.t-3300,
  spawned:d.majorSpawned,remaining:d.major?.remaining,wallHp:machineAt(st,x,y)?.hp??120,
  savedContinuationMatches:true,warning:campaignWarning(st),finding:'locked origin waits; no attacker is spawned to breach the wall'}));
