import assert from 'node:assert/strict';
import {writeFileSync,readFileSync} from 'node:fs';
import {ground,actionResult,stateHash,conservation} from '../../../packages/sim/src/index';
import {factoryScenario,power,restore} from '../../../packages/harness/src/factoryScenario';
import {replayInterval} from '../../../packages/harness/src/blueprint';
const out='docs/evidence/p9-01-2026-09-08';
for(const seed of [6,29]){
 const prior=JSON.parse(readFileSync(`${out}/sweep-initial/seed-${seed}.json`,'utf8'));
 const found=prior.report.failures.find((f:{code:string;subject:string})=>f.code==='rail-placement'&&f.subject.startsWith('tile:'));
 const t=Number(found.subject.slice(5)),{st,d,initial}=factoryScenario(seed),G=ground(st),site=st.campaign!.expansion!.station;
 power(d,site.block);restore(d,'station');d.approach(site.x,site.y,site.size);d.send({type:'collectTramKit'});
 d.approach(t%G.tw,Math.floor(t/G.tw));const before={pockets:{...st.engineer.inv},machines:st.flow!.machines.length};
 assert.ok(st.engineer.inv.track>0);let refusal='';
 try{d.send({type:'construct',edits:[{action:'place',item:'track',x:t%G.tw,y:Math.floor(t/G.tw),dir:0}]});}catch(e){refusal=(e as Error).message;}
 assert.equal(refusal,'not buildable ground');assert.equal(actionResult(st).ok,false);
 assert.deepEqual(st.engineer.inv,before.pockets);assert.equal(st.flow!.machines.length,before.machines);assert.ok(conservation(st).ok);
 const replay=replayInterval(initial,d.log,st.flow!.tick);assert.equal(stateHash(replay),stateHash(st));
 writeFileSync(`${out}/reproduction-${seed}.json`,JSON.stringify({seed,tile:t,x:t%G.tw,y:Math.floor(t/G.tw),owner:G.owner[t],near:G.near[t],blockState:st.blocks[G.near[t]].state,refusal,pockets:st.engineer.inv,seconds:st.t,log:d.log,hash:stateHash(st),replay:stateHash(replay),conserved:true,assistance:'Ordinary starting stock, paid station generator and restoration, earned tram kit, on-foot approach and real rejected track command. No stock, position or clock injection. Diagnostic automation, not human play.'},null,2)+'\n',{flag:'wx'});
 console.log(`PASS reproduction seed ${seed}: paid unlock/kit, ordinary approach, track refused at ${t}; conserved and full replay.`);
}
