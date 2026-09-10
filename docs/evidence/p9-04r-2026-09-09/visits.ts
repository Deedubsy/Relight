import assert from 'node:assert/strict';
import {writeFileSync} from 'node:fs';
import {factoryScenario} from '../../../packages/harness/src/factoryScenario';
import {replayInterval,savedContinuation} from '../../../packages/harness/src/blueprint';
import {actionResult,discoveryCheck,inReach,conservation,stateHash,makeSave} from '../../../packages/sim/src/index';
for(const seed of [88,842]){
 const {st,d,initial,assistance}=factoryScenario(seed,false);
 const site=seed===88?st.campaign!.discovery!:st.campaign!.recruits!.sites.find(s=>s.kind==='electricians')!;
 d.approach(site.x,site.y);assert.ok(inReach(st,site.x,site.y));
 if(seed===842){d.send({type:'recruitSurvivors',id:site.id});assert.ok(actionResult(st).ok,actionResult(st).reason);assert.ok(st.campaign!.recruits!.sites.find(s=>s.kind==='electricians')!.recruitedAt>=0);}
 const hash=stateHash(st),replay=stateHash(replayInterval(initial,d.log,st.flow!.tick)),continuation=savedContinuation(st);
 assert.equal(hash,replay);assert.ok(continuation.same);assert.ok(conservation(st).ok);
 const result={seed,assistance,site:{id:site.id,x:site.x,y:site.y},ordinaryWalk:true,seconds:st.t,hp:st.engineer.hp,recruited:seed===842,cacheGate:seed===88?discoveryCheck(st,site.id):null,conserved:true,hash,replay,continuation};
 writeFileSync(new URL(`visit-${seed}.json`,import.meta.url),JSON.stringify(result,null,2)+'\n',{flag:'wx'});
 writeFileSync(new URL(`visit-save-${seed}.json`,import.meta.url),JSON.stringify(makeSave(st,{log:d.log,logComplete:true})),{flag:'wx'});console.log(JSON.stringify(result));
}
