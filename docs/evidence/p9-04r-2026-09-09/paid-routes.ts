import assert from 'node:assert/strict';
import {writeFileSync,mkdirSync} from 'node:fs';
import {factoryScenario,power,restore,configure} from '../../../packages/harness/src/factoryScenario';
import {replayInterval,savedContinuation} from '../../../packages/harness/src/blueprint';
import {ground,canPlace,truckWorkRoute,actionResult,conservation,stateHash,makeSave,type Command} from '../../../packages/sim/src/index';

mkdirSync(new URL('paid/',import.meta.url));
for(const seed of (process.argv[2]?.split(',').map(Number)??[3,4,5,8,11,13,80,88,102,842,2313,3610])){
 const {st,d,initial,assistance}=factoryScenario(seed,false);
 try {
 const send=(c:Command)=>{d.send(c);assert.ok(actionResult(st).ok,actionResult(st).reason);};
 const e=st.campaign!.expansion!,district=st.campaign!.districts!,G=ground(st);
 power(d,e.station.block);restore(d,'station');
 // The first generator consumes the carried fuel. Fetch the remaining real home coal.
 const depot=st.flow!.machines.find(m=>m.kind==='depot')!;d.approach(depot.x,depot.y,depot.size);assert.equal(d.take('coal',20),20);
 power(d,district.station.block);restore(d,'northStation');
 const collect=()=>{d.approach(e.station.x,e.station.y,e.station.size);d.send({type:'collectTramKit'});};collect();
 const path=[...e.route,...district.route.slice(1)];for(const tile of path){if(!(st.engineer.inv.track>0)&&e.reward.track>0)collect();d.place('track',tile%G.tw,Math.floor(tile/G.tw));}
 const stops=[...e.stops,district.stop].map(([x,y])=>d.place('tramstop',x,y));d.place('tram',path[0]%G.tw,Math.floor(path[0]/G.tw));
 const network={path,stops},[home,,last]=stops;
 configure(d,home,{steel:{request:0,reserve:0,export:true}});
 configure(d,last,{steel:{request:5,reserve:0,export:false}});assert.equal(d.put('steel',5,home),5);
 const recruited=st.campaign!.recruits!.sites.find(r=>r.kind==='foreman')!;
 d.approach(recruited.x,recruited.y);send({type:'recruitSurvivors',id:recruited.id});assert.ok(recruited.recruitedAt>=0);
 const t=st.campaign!.truck!;
 const source=(()=>{for(let r=2;r<=8;r++)for(let y=Math.floor(t.y)-r;y<=t.y+r;y++)for(let x=Math.floor(t.x)-r;x<=t.x+r;x++){
  if(![y*G.tw+x,y*G.tw+x+1,(y+1)*G.tw+x,(y+1)*G.tw+x+1].every(tile=>G.owner[tile]>=0)||!canPlace(st,'chest',x,y).ok||Math.hypot(x+.5-t.x,y+.5-t.y)>4||truckWorkRoute(st,[{x,y,w:2,h:2}],3)===null)continue;
  return d.place('chest',x,y);
 }throw Error('No accessible supply chest');})();
 assert.equal(d.put('steel',2,source),2);
 const destination=(()=>{for(let r=11;r<=18;r++)for(let y=Math.floor(t.y)-r;y<=t.y+r;y++)for(let x=Math.floor(t.x)-r;x<=t.x+r;x++){
  if(Math.hypot(x+.5-t.x,y+.5-t.y)<11||!canPlace(st,'belt',x,y,1).ok||!canPlace(st,'belt',x+1,y,1).ok)continue;
  const route=truckWorkRoute(st,[{x,y,w:1,h:1},{x:x+1,y,w:1,h:1}],8);if(route?.length)return {x,y,route};
 }throw Error('No accessible truck construction destination');})();
 send({type:'blueprintLibrary',action:{type:'import',text:JSON.stringify({format:'relight-blueprint',version:1,name:'Survey belts',folder:'Survey',icon:'belt',blueprint:{version:1,name:'Survey belts',entities:[{id:'a',kind:'belt',x:0,y:0,dir:1},{id:'b',kind:'belt',x:1,y:0,dir:1}]}})}});
 send({type:'blueprintOrder',action:{type:'queue',x:destination.x,y:destination.y}});
 const order=st.campaign!.plans!.orders.at(-1)!,pockets={...st.engineer.inv},start={x:t.x,y:t.y};
 send({type:'truckWork',action:{type:'start',sourceId:source.id,orderIds:[order.id]}});
 const phases=new Set<string>();for(let i=0;i<2400&&t.work!.phase!=='complete';i++){d.run(.05);phases.add(t.work!.phase);}
 assert.equal(order.status,'completed',t.work!.reason);assert.equal(source.inv.steel??0,0);assert.equal(t.cargo.steel??0,0);
 assert.deepEqual(st.engineer.inv,pockets);assert.notDeepEqual({x:t.x,y:t.y},start);assert.ok(phases.has('travelling'));
 for(let i=0;i<180&&(last.cargo?.steel??0)<5;i++)d.run(1);
 assert.equal(last.cargo?.steel,5,'ordinary freight delivery');assert.ok(conservation(st).ok,conservation(st).problems.join(','));
 const hash=stateHash(st),replay=stateHash(replayInterval(initial,d.log,st.flow!.tick)),continuation=savedContinuation(st);
 assert.equal(replay,hash);assert.ok(continuation.same);
 const record={seed,assistance,surveyVersion:st.campaign!.expansion!.surveyVersion,seconds:st.t,tracks:network.path.length,stops:network.stops.length,freightDelivered:last.cargo?.steel,truck:{source:[source.x,source.y],destination:[destination.x,destination.y],routeSteps:destination.route.length,start,end:{x:t.x,y:t.y},phases:[...phases],order:order.status,pocketsUnchanged:true},conserved:true,hash,replay,continuation};
 writeFileSync(new URL(`paid/seed-${seed}.json`,import.meta.url),JSON.stringify(record,null,2)+'\n',{flag:'wx'});
 writeFileSync(new URL(`paid/save-${seed}.json`,import.meta.url),JSON.stringify(makeSave(st,{log:d.log,logComplete:true})),{flag:'wx'});
 console.log(JSON.stringify(record));
 }catch(error){
  const record={seed,status:'failed',reason:(error as Error).stack,seconds:st.t,hash:stateHash(st),conservation:conservation(st)};
  writeFileSync(new URL(`paid/failure-${seed}.json`,import.meta.url),JSON.stringify(record,null,2)+'\n',{flag:'wx'});
  writeFileSync(new URL(`paid/failure-save-${seed}.json`,import.meta.url),JSON.stringify(makeSave(st,{log:d.log,logComplete:true})),{flag:'wx'});
  console.log(JSON.stringify(record));process.exitCode=1;
 }
}
