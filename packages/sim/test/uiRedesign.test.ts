import {test} from 'node:test';
import assert from 'node:assert/strict';
import {createCampaign,pocketSlots,pocketUsed,invCap,inventoryCommand,take,drop,applyCommands,actionResult,stateHash,makeSave,loadState,chestCount,TILE_TPS} from '../src/index';
import {createSession,parseUrl,queue,dispatch,frame,loadSnapshot} from '../../game/src/session';
import {parseUiPreferences,DEFAULT_QUICKBAR} from '../../game/src/uiPreferences';
const move=(e:ReturnType<typeof createCampaign>['engineer'],from:number,to:number,type:'move'|'split'='move',n?:number)=>{const slots=pocketSlots(e),s=slots[from]!;return {type,from,to,item:s.item,count:s.count,layout:JSON.stringify(slots),n};};
test('UI-08 actual stacks conserve quantities through split, merge, swap, sort and save; stale repeated moves reject',()=>{
 const st=createCampaign(3),e=st.engineer;e.inv={steel:70,copper:20};const before={...e.inv};const cells=pocketSlots(e),from=cells.findIndex(c=>c?.item==='steel');
 const command={type:'inventory' as const,action:move(e,from,12,'split',10)};applyCommands(st,[command]);assert.ok(actionResult(st).ok);assert.equal(pocketSlots(e)[12]?.count,10);const hash=stateHash(st);applyCommands(st,[command]);assert.ok(!actionResult(st).ok);assert.equal(stateHash(st),hash);
 assert.ok(inventoryCommand(e,move(e,12,from)).ok);assert.ok(inventoryCommand(e,move(e,from,0)).ok);assert.deepEqual(e.inv,before);
 const loaded=loadState(makeSave(st));assert.deepEqual(pocketSlots(loaded.engineer),pocketSlots(e));assert.equal(stateHash(loaded),stateHash(st));
 assert.ok(inventoryCommand(e,{type:'sort'}).ok);assert.deepEqual(e.inv,before);drop(e,'steel',60);assert.equal(pocketSlots(e).filter(c=>c?.item==='steel').reduce((n,c)=>n+c!.count,0),10);
});
test('UI-08 fragmentation and full capacity are authoritative, including partial-stack slack and legacy kit reservations',()=>{
 const e=createCampaign(3).engineer;e.inv={steel:40};for(let i=1;i<40;i++){const from=pocketSlots(e).findIndex(c=>c?.item==='steel'&&c.count>1);assert.ok(inventoryCommand(e,move(e,from,i,'split',1)).ok);}
 assert.equal(pocketUsed(e),40);assert.equal(take(e,'copper',1),0);assert.equal(take(e,'steel',10),10);assert.equal(e.inv.steel,50);assert.equal(pocketUsed(e),40);
 assert.ok(!inventoryCommand(e,move(e,0,1,'split',1)).ok);
 const kit=createCampaign(3).engineer;kit.inv={kit:2,steel:10};assert.equal(pocketUsed(kit),21);assert.equal(pocketSlots(kit).length,40);assert.equal(pocketSlots(kit).filter(c=>c?.reserved).length,18);assert.ok(!inventoryCommand(kit,move(kit,0,30)).ok);kit.truck=true;assert.equal(invCap(kit),200);
});
test('UI-08 paired transfers conserve stock, consume the selected stack, reject stale and out-of-reach commands',()=>{
 const st=createCampaign(3),e=st.engineer;st.stock.steel=40;/* Prepared stored stack for the transfer regression. */applyCommands(st,[{type:'factory',action:{type:'chestTake',item:'steel',n:40}}]);assert.ok(actionResult(st).ok);
 const i=pocketSlots(e).findIndex(c=>c?.item==='steel');inventoryCommand(e,move(e,i,9,'split',10));const before=(e.inv.steel??0)+chestCount(st,'steel');
 const c={type:'factory' as const,action:{type:'chestPut' as const,item:'steel',n:4,expected:{total:e.inv.steel,slot:9,layout:JSON.stringify(pocketSlots(e))}}};applyCommands(st,[c]);assert.equal(actionResult(st).moved,4);assert.equal(pocketSlots(e)[9]?.count,6);assert.equal((e.inv.steel??0)+chestCount(st,'steel'),before);
 const hash=stateHash(st);applyCommands(st,[c]);assert.ok(!actionResult(st).ok);assert.equal(stateHash(st),hash);e.x=0;e.y=0;const inv={...e.inv};applyCommands(st,[{type:'factory',action:{type:'chestTake',item:'steel',n:10}}]);assert.ok(!actionResult(st).ok);assert.deepEqual(e.inv,inv);
});
test('UI-08 shortcut settings preserve existing arrangements and clear retired identifiers individually',()=>{
 assert.equal(DEFAULT_QUICKBAR.filter(Boolean).length,3);const old=['belt','inserter','excavator','assembler','turret','lamp','pole','generator','rifle','floodlight'];assert.deepEqual(parseUiPreferences(JSON.stringify({version:1,quickbar:old})).quickbar,old);old[4]='retired';const p=parseUiPreferences(JSON.stringify({version:1,quickbar:old}));assert.equal(p.quickbar[4],null);assert.equal(p.quickbar[8],'rifle');
});
test('UI-08 interactive speed clamps every command path, uses elapsed time and discards old accelerated accumulation',async()=>{
 Object.defineProperty(globalThis,'location',{configurable:true,value:{href:'http://localhost/'}});
 const s=createSession(parseUrl('?rules=exploration-v2'));queue(s,{type:'setSpeed',mult:16});assert.equal((s.pending[0] as {mult:number}).mult,1);dispatch(s,{type:'setSpeed',mult:4});assert.equal(s.state.speed,1);
 const tick=s.state.flow!.tick;for(let i=0;i<20;i++)frame(s,.05);assert.equal(s.state.flow!.tick-tick,TILE_TPS);
 s.state.acc=800;s.state.speed=16;frame(s,.05);assert.equal(s.state.speed,1);assert.equal(s.state.flow!.tick-tick,TILE_TPS+1);
 dispatch(s,{type:'setSpeed',mult:0});const paused=s.state.flow!.tick;frame(s,.1);assert.equal(s.state.flow!.tick,paused);dispatch(s,{type:'setSpeed',mult:16});frame(s,20);assert.equal(s.state.flow!.tick,paused);
 const fetchBefore=globalThis.fetch;try{const save=makeSave(s.state);save.state.speed=16;globalThis.fetch=async()=>new Response(JSON.stringify(save));const loaded=await loadSnapshot('/fast.json');assert.equal(loaded.state.speed,1);assert.equal(createSession(parseUrl('?rules=exploration-v2'),loaded).state.speed,1);save.state.speed=0;assert.equal((await loadSnapshot('/paused.json')).state.speed,0);}finally{globalThis.fetch=fetchBefore;}
});
