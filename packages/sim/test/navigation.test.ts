import {suppliedCampaign as createCampaign} from './suppliedCampaignFixture';
// Prepared historical starting stock; current ungranted progression is checked in gameplayCorrections.test.ts.
import {test} from 'node:test';
import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import {navigationTargets,navigationProblem,applyCommands,actionResult,stateHash,makeSave,loadState,ground,blockName,campaignDiscoveries,conservation,advanceFlow,type NavigationAction} from '../src/index';
import {FactoryDriver,replayInterval,savedContinuation} from '../../harness/src/blueprint';
import {loadSnapshot} from '../../game/src/session';
const send=(st:ReturnType<typeof createCampaign>,action:NavigationAction,ok=true)=>{applyCommands(st,[{type:'navigation',action}]);assert.equal(actionResult(st).ok,ok,actionResult(st).reason);};
test('navigation uses existing deterministic names and known sites without revealing hidden destinations',()=>{
 for(const seed of [3,6,29]){const st=createCampaign(seed),hash=stateHash(st),rows=navigationTargets(st);assert.deepEqual(rows,navigationTargets(loadState(st)));assert.equal(stateHash(st),hash);assert.equal(rows.filter(t=>t.kind==='place').length,1);assert.equal(rows.find(t=>t.id==='site:station')!.visited,false);assert.equal(rows.find(t=>t.id===`block:${st.campaign!.homeBlock}`)!.visited,true);assert.equal(new Set(rows.map(t=>t.id)).size,rows.length);}
});
test('names, defaults and pins use atomic bounded commands, stable identities and literal text',()=>{
 const st=createCampaign(3),id=`block:${st.campaign!.homeBlock}`,e=st.engineer,original=blockName(st,st.campaign!.homeBlock);
 send(st,{type:'rename',id,name:'<b>Harbour</b>'});assert.equal(blockName(st,st.campaign!.homeBlock),'<b>Harbour</b>');
 send(st,{type:'rename',id:'site:station',name:'Western Terminus'});assert.equal(campaignDiscoveries(st)[0].title,'Western Terminus');
 for(const action of [{type:'rename',id:'site:workshop',name:'Hidden'},{type:'rename',id,name:'x'.repeat(65)},{type:'rename',id,name:'bad\nname'},{type:'rename',id,name:'Western Terminus'},{type:'pin',x:-1,y:0,name:'Outside'}] as NavigationAction[]){const hash=stateHash(st);send(st,action,false);assert.equal(stateHash(st),hash);}
 send(st,{type:'rename',id,name:''});assert.equal(blockName(st,st.campaign!.homeBlock),original);
 for(let i=0;i<32;i++)send(st,{type:'pin',x:Math.floor(e.x),y:Math.floor(e.y),name:`Depot ${i}`});const hash=stateHash(st);send(st,{type:'pin',x:Math.floor(e.x),y:Math.floor(e.y),name:'overflow'},false);assert.equal(stateHash(st),hash);
 send(st,{type:'removePin',id:1});send(st,{type:'pin',x:Math.floor(e.x),y:Math.floor(e.y),name:''});assert.equal(st.campaign!.navigation!.pins.at(-1)!.id,33);assert.equal(navigationProblem(st),'');assert.ok(conservation(st).ok);
});
test('physical travel records visits; annotations and visits replay and continue through saves',()=>{
 const st=createCampaign(6),initial=structuredClone(st),d=new FactoryDriver(st),s=st.campaign!.expansion!.station;
 d.send({type:'navigation',action:{type:'rename',id:'site:station',name:'West line'}});d.approach(s.x,s.y,s.size);d.run(1);
 assert.ok(st.campaign!.navigation!.visitedSites.includes('station'));
 d.send({type:'navigation',action:{type:'pin',x:Math.floor(st.engineer.x),y:Math.floor(st.engineer.y),name:'Return here'}});
 const saved=makeSave(st,{log:d.log,logComplete:true});assert.equal(stateHash(loadState(saved)),stateHash(st));assert.equal(stateHash(replayInterval(initial,d.log,st.flow!.tick)),stateHash(st));assert.ok(savedContinuation(st).same);assert.ok(conservation(st).ok);
});
test('invalid saved navigation refuses versions, unknown targets, uncharted pins and malformed records',()=>{
 const st=createCampaign(3),G=ground(st),unknown=G.urban!.places.find(p=>!navigationTargets(st).some(t=>t.id===`block:${p.block}`))!;
 const mutate=[(n:any)=>n.version=2,(n:any)=>n.names=JSON.parse('{"__proto__":"bad"}'),(n:any)=>n.visitedBlocks=[-1],(n:any)=>n.visitedSites=['hidden'],(n:any)=>n.pins=[{id:1,x:unknown.pad.x,y:unknown.pad.y,name:'Hidden'}],(n:any)=>n.pins=null,(n:any)=>n.names=[]];
 for(const fn of mutate){const copy=structuredClone(st);fn(copy.campaign!.navigation);assert.throws(()=>loadState(copy),/navigation/i);}
});
test('old revision-2 saves retain geometry and stock but do not claim replay before visit tracking',async()=>{
 const old=JSON.parse(readFileSync(new URL('../../../docs/evidence/p9-02-2026-09-08/paid/save-6.json',import.meta.url),'utf8')),loaded=loadState(old);
 assert.equal(stateHash(loaded),old.hash);assert.equal(loaded.campaign!.navigation,undefined);assert.deepEqual(loaded.campaign!.expansion,old.state.campaign.expansion);
 const descriptor=Object.getOwnPropertyDescriptor(globalThis,'localStorage');Object.defineProperty(globalThis,'localStorage',{configurable:true,value:{getItem:()=>JSON.stringify(old)}});
 try{const snapshot=await loadSnapshot('local:old');assert.equal(snapshot.logComplete,false);assert.deepEqual(snapshot.log,old.log);}finally{if(descriptor)Object.defineProperty(globalThis,'localStorage',descriptor);else Reflect.deleteProperty(globalThis,'localStorage');}
 loaded.speed=1;advanceFlow(loaded,.05);assert.ok(loaded.campaign!.navigation);assert.equal(navigationProblem(loaded),'');
});
