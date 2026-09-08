import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import {loadState,blockLights,campaignGrid,surveyedDistrict,campaignDiscoveries,itemGuide,stateHash} from '../../../packages/sim/src/index';
for(const seed of [3,4,5,8,11,13])for(const order of ['forward','reverse']){
 const name=`seed${seed}-${order}`,s=loadState(JSON.parse(readFileSync(`docs/evidence/p7-05-2026-09-08/${name}.json`,'utf8'))),before=stateHash(s),grid=campaignGrid(s);
 assert.equal(s.campaign!.recruits!.sites.filter(r=>r.recruitedAt>=0).length,4);
 const lights=s.flow!.machines.filter(m=>m.kind==='floodlight'||m.kind==='arclamp').map(m=>{const l=s.blocks.flatMap((_,i)=>blockLights(s,i)).find(l=>l.tx===m.x+(m.kind==='floodlight'?.5:0)&&l.ty===m.y+(m.kind==='floodlight'?.5:0))!;assert.ok(l?.lit);return {kind:m.kind,radius:l.r,lit:l.lit};});
 const pole=s.flow!.machines.find(m=>m.kind==='bigpole')!,circuit=grid.poles.get(pole.id)!;assert.ok(circuit.supply>0,'Big pole belongs to powered circuit');
 for(let i=0;i<s.blocks.length;i++)assert.equal(surveyedDistrict(s,i),s.blocks[i].name);
 assert.ok(itemGuide(s,'concrete')!.recipes[0].available);assert.match(campaignDiscoveries(s).find(r=>r.id===s.campaign!.turbine!.id)!.status,/generating/);assert.equal(stateHash(s),before);
 console.log(JSON.stringify({name,lights,poleCircuitSupply:circuit.supply,surveyDistricts:s.blocks.length,journal:true,recipe:true,readOnly:true}));
}
