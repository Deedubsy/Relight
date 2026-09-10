import assert from 'node:assert/strict';
import {writeFileSync,mkdirSync} from 'node:fs';
import {factoryScenario,extract} from '../../../packages/harness/src/factoryScenario';
import {replayInterval,savedContinuation} from '../../../packages/harness/src/blueprint';
import {conservation,stateHash,makeSave} from '../../../packages/sim/src/index';
const out=new URL('./extraction/',import.meta.url);mkdirSync(out,{recursive:true});
for(const seed of [3,4,5,8,11,13])for(const item of ['steel','copper','coal'] as const){
 const {st,d,initial,assistance}=factoryScenario(seed,false);let result:unknown;
 try{
  const {source,generator,excavator,chest}=extract(d,item),before=st.flow!.stats.minedOf[item]??0;
  for(let k=0;k<60&&(chest.inv[item]??0)<5;k++)d.run(1);
  assert.ok((chest.inv[item]??0)>=5,'five units actually delivered to the source chest');
  assert.ok((st.flow!.stats.minedOf[item]??0)>before);assert.ok(conservation(st).ok);
  const hash=stateHash(st),replay=stateHash(replayInterval(initial,d.log,st.flow!.tick)),continuation=savedContinuation(st);assert.equal(hash,replay);assert.ok(continuation.same);
  result={seed,item,status:'passed',assistance,source,placement:{generator:[generator.x,generator.y],excavator:[excavator.x,excavator.y,excavator.dir],chest:[chest.x,chest.y]},delivered:chest.inv[item],seconds:st.t,conserved:true,hash,replay,continuation};
 }catch(e){result={seed,item,status:'failed',assistance,reason:(e as Error).message,seconds:st.t,conservation:conservation(st),hash:stateHash(st)};}
 writeFileSync(new URL(`seed-${seed}-${item}.json`,out),JSON.stringify(result,null,2)+'\n',{flag:'wx'});
 writeFileSync(new URL(`save-${seed}-${item}.json`,out),JSON.stringify(makeSave(st,{log:d.log,logComplete:true})),{flag:'wx'});console.log(JSON.stringify(result));
}
