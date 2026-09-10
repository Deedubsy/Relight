import {writeFileSync} from 'node:fs';
import {replayInterval} from '../../harness/src/blueprint';
import {test} from 'node:test';
import assert from 'node:assert/strict';
import {expedition,walk,command} from './transportFixture';
import {removalPreview,tramAt,loadState,makeSave,canPlace,inReach,machineAt,actionResult,advanceFlow,conservation,stateHash,parseBlueprint,truckWorkProblem,truckWorkRoute,truckFits,truckRect,stackSize,createCampaign,boardTruck,truckBoardCheck,findPath,passable,type SimState,type Command,type TruckWorkAction} from '../src/index';
let base:SimState;
function ready(){base??=expedition().st;const st=loadState(makeSave(base));st.speed=1;const r=st.campaign!.recruits!.sites.find(r=>r.kind==='foreman')!;r.seenAt=r.recruitedAt=st.t;return st;}
function send(st:SimState,c:Command,ok=true){command(st,[c]);assert.equal(actionResult(st).ok,ok,actionResult(st).reason);}
const work=(st:SimState,action:TruckWorkAction,ok=true)=>send(st,{type:'truckWork',action},ok);
function tick(st:SimState,n=1){advanceFlow(st,n);assert.equal(truckWorkProblem(st),'');}
function chest(st:SimState,n=20){const t=st.campaign!.truck!;for(let y=Math.floor(t.y)-4;y<t.y+4;y++)for(let x=Math.floor(t.x)-4;x<t.x+4;x++)if(canPlace(st,'chest',x,y).ok&&Math.hypot(x+.5-t.x,y+.5-t.y)<=4){walk(st,x,y);send(st,{type:'construct',edits:[{action:'place',item:'chest',x,y,dir:0}]});if(n)send(st,{type:'factory',action:{type:'chestPut',item:'steel',n,x,y}});return machineAt(st,x,y)!;}throw Error('No chest site');}
function plan(st:SimState,offset=11,width=2,real=false){const t=st.campaign!.truck!;for(let r=offset;r<offset+8;r++)for(let y=Math.floor(t.y)-r;y<=t.y+r;y++)for(let x=Math.floor(t.x)-r;x<=t.x+r;x++){
 if(Math.hypot(x+.5-t.x,y+.5-t.y)<offset||!Array.from({length:width},(_,i)=>canPlace(st,'belt',x+i,y).ok).every(Boolean))continue;
 const rects=[{x,y,w:1,h:1},{x:x+1,y,w:1,h:1}],route=truckWorkRoute(st,rects,8);if(!route?.length)continue;
 const clipboard=parseBlueprint({version:1,name:'Truck belts',entities:[{id:'a',kind:'belt',x:0,y:0,dir:1},{id:'b',kind:'belt',x:1,y:0,dir:1}]});if(real){walk(st,x,y);send(st,{type:'construct',edits:clipboard.entities.map(e=>({action:'place',item:e.kind,x:x+e.x,y:y+e.y,dir:e.dir}))});send(st,{type:'blueprintCopy',from:{x,y},to:{x:x+1,y}});send(st,{type:'construct',edits:[{action:'pickUp',x,y},{action:'pickUp',x:x+1,y}]});}else st.campaign!.clipboard=clipboard;send(st,{type:'blueprintOrder',action:{type:'queue',x,y}});return st.campaign!.plans!.orders.at(-1)!;
 }throw Error('No routed build site');}
function until(st:SimState,predicate:()=>boolean,seconds=60){for(let i=0;i<seconds&&!predicate();i++)tick(st);assert.ok(predicate(),st.campaign!.truck!.work?.reason);}

test('physical truck loads a real chest, travels, builds from cargo, and leaves engineer pockets and position intact',()=>{
 const st=ready(),s=chest(st),o=plan(st),t=st.campaign!.truck!,initial={x:t.x,y:t.y},inv={...st.engineer.inv},position=[st.engineer.x,st.engineer.y],history=structuredClone(st.construction);
 work(st,{type:'start',sourceId:s.id,orderIds:[o.id]});const phases=new Set<string>();for(let i=0;i<120&&t.work!.phase!=='complete';i++){advanceFlow(st,.5);phases.add(t.work!.phase);assert.equal(truckWorkProblem(st),'');assert.ok(conservation(st).ok,conservation(st).problems.join(','));}
 assert.equal(o.status,'completed',t.work!.reason);assert.ok(phases.has('loading')&&phases.has('travelling')&&phases.has('building'));assert.notDeepEqual([t.x,t.y],[initial.x,initial.y]);assert.equal(s.inv.steel,18);assert.equal(t.cargo.steel??0,0);assert.deepEqual(st.engineer.inv,inv);assert.deepEqual([st.engineer.x,st.engineer.y],position);assert.deepEqual(st.construction,history);assert.equal(stateHash(loadState(st)),stateHash(st));
});

test('shortages resume after local chest replenishment; pause, cancel and repeat controls retain stock',()=>{
 const st=ready(),s=chest(st,0),o=plan(st);work(st,{type:'start',sourceId:s.id,orderIds:[o.id]});until(st,()=>st.campaign!.truck!.work!.reason.includes('shortage'));const t=st.campaign!.truck!,before={...t.cargo};work(st,{type:'pause'});const xy=[t.x,t.y];tick(st,4);assert.deepEqual([t.x,t.y],xy);assert.deepEqual(t.cargo,before);work(st,{type:'pause'});
 walk(st,s.x,s.y);send(st,{type:'factory',action:{type:'chestPut',item:'steel',n:10,x:s.x,y:s.y}});work(st,{type:'resume'});until(st,()=>t.work!.phase==='travelling'&&t.cargo.steel===2);send(st,{type:'blueprintOrder',action:{type:'cancel',id:o.id}});tick(st,.05);assert.equal(t.work!.phase,'complete');const stock={...t.cargo};tick(st,5);assert.deepEqual(t.cargo,stock);assert.ok(conservation(st).ok);
});

test('loading, travelling and partial-build checkpoints continue deterministically without double spending',()=>{
 const st=ready(),s=chest(st),o=plan(st);work(st,{type:'start',sourceId:s.id,orderIds:[o.id]});const seen=new Set<string>();
 for(let i=0;i<500&&st.campaign!.truck!.work!.phase!=='complete';i++){
  advanceFlow(st,.05);const phase=st.campaign!.truck!.work!.phase;if(!['loading','travelling','building'].includes(phase)||seen.has(phase)||phase==='loading'&&!(st.campaign!.truck!.cargo.steel>0)||phase==='building'&&!machineAt(st,o.x,o.y))continue;seen.add(phase);
  const copy=loadState(st);copy.speed=1;advanceFlow(copy,12);const run=structuredClone(st);run.speed=1;advanceFlow(run,12);assert.equal(stateHash(copy),stateHash(run));assert.ok(conservation(copy).ok);
 }
 assert.deepEqual([...seen].sort(),['building','loading','travelling']);assert.equal(o.status,'completed');
});

test('missing source and occupied destinations wait safely and retry after the obstruction is fixed',()=>{
 const st=ready(),s=chest(st),o=plan(st);work(st,{type:'start',sourceId:s.id,orderIds:[o.id]});until(st,()=>st.campaign!.truck!.work!.phase==='travelling'&&(st.campaign!.truck!.cargo.steel??0)>0);
 work(st,{type:'pause'});walk(st,o.x,o.y);send(st,{type:'construct',edits:[{action:'place',item:'belt',x:o.x,y:o.y,dir:0}]});work(st,{type:'resume'});until(st,()=>st.campaign!.truck!.work!.reason.includes('machine'));assert.equal(o.status,'waiting');walk(st,o.x,o.y);send(st,{type:'construct',edits:[{action:'pickUp',x:o.x,y:o.y}]});until(st,()=>o.status==='completed');assert.ok(conservation(st).ok);
 const other=plan(st,15);work(st,{type:'start',sourceId:s.id,orderIds:[other.id]});walk(st,s.x,s.y);send(st,{type:'construct',edits:[{action:'pickUp',x:s.x,y:s.y}]});until(st,()=>st.campaign!.truck!.work!.reason.includes('missing'));assert.equal(other.status,'waiting');assert.ok(conservation(st).ok);
});

test('manual boarding pauses the automated truck and preserves its cargo and independent orders',()=>{
 const st=ready(),s=chest(st),o=plan(st);work(st,{type:'start',sourceId:s.id,orderIds:[o.id]});work(st,{type:'pause'});const t=st.campaign!.truck!,r=truckRect(t);let found=false;
 for(let y=Math.floor(r.y)-2;y<=r.y+r.h+2&&!found;y++)for(let x=Math.floor(r.x)-2;x<=r.x+r.w+2;x++){if(!passable(st,x,y)||!findPath(st,Math.floor(st.engineer.x),Math.floor(st.engineer.y),x,y))continue;command(st,[{type:'move',x:x+.5,y:y+.5}]);for(let i=0;i<80&&st.engineer.target;i++)tick(st);if(!truckBoardCheck(st)){found=true;break;}}
 assert.ok(found);work(st,{type:'resume'});assert.equal(boardTruck(st),'');assert.equal(t.work!.phase,'paused');assert.equal(t.work!.enabled,false);const xy=[t.x,t.y],cargo={...t.cargo};tick(st,3);assert.deepEqual([t.x,t.y],xy);assert.deepEqual(t.cargo,cargo);assert.equal(o.status,'waiting');assert.equal(stateHash(loadState(st)),stateHash(st));
});

test('malformed jobs and saved teleport paths are rejected without granting stock or bypassing Foreman',()=>{
 const st=ready(),s=chest(st),o=plan(st);const hash=stateHash(st);for(const a of [{type:'start',sourceId:-1,orderIds:[o.id]},{type:'start',sourceId:s.id,orderIds:[o.id,o.id]},{type:'start',sourceId:s.id,orderIds:[]}] as TruckWorkAction[])work(st,a,false);assert.equal(stateHash(st),hash);
 work(st,{type:'start',sourceId:s.id,orderIds:[o.id]});until(st,()=>st.campaign!.truck!.work!.phase==='travelling');const bad=structuredClone(st);bad.campaign!.truck!.work!.route[0].x+=100;assert.throws(()=>loadState(bad),/truck route/);
 const old=ready();assert.equal(loadState(old).campaign!.truck!.work,undefined);old.campaign!.recruits!.sites.find(r=>r.kind==='foreman')!.recruitedAt=-1;work(old,{type:'start',sourceId:s.id,orderIds:[o.id]},false);
});


test('a new obstacle on a saved route stops the swept footprint and work resumes after removal',()=>{
 const st=ready(),s=chest(st),o=plan(st,20);work(st,{type:'start',sourceId:s.id,orderIds:[o.id]});for(let i=0;i<1000&&!(st.campaign!.truck!.work!.phase==='travelling'&&(st.campaign!.truck!.cargo.steel??0)>0);i++)tick(st,.05);
 const t=st.campaign!.truck!;let obstacle:{x:number;y:number}|undefined;
 for(const p of t.work!.route.slice(2)){const r=truckRect(p);for(let y=Math.floor(r.y);y<r.y+r.h;y++)for(let x=Math.floor(r.x);x<r.x+r.w;x++)if(canPlace(st,'chest',x,y).ok&&inReach(st,x,y)){obstacle={x,y};break;}if(obstacle)break;}
 assert.ok(obstacle,'reachable ordinary obstacle location');send(st,{type:'construct',edits:[{action:'place',item:'chest',...obstacle,dir:0}]});until(st,()=>t.work!.reason.includes('obstructed'));assert.ok(truckFits(st,t.x,t.y,t.dir));send(st,{type:'construct',edits:[{action:'pickUp',...obstacle}]});until(st,()=>o.status==='completed');assert.ok(conservation(st).ok);
});

test('unrelated full cargo blocks loading without spending source stock and recovers after unloading',()=>{
 const st=ready(),s=chest(st),o=plan(st),t=st.campaign!.truck!;
 // Labelled capacity fixture: isolate full cargo; never claim this synthetic stock was earned.
 t.cargo={stone:stackSize('stone')*200};const steel=s.inv.steel;work(st,{type:'start',sourceId:s.id,orderIds:[o.id]});until(st,()=>t.work!.reason.includes('full'));assert.equal(s.inv.steel,steel);assert.equal(o.status,'waiting');t.cargo={};until(st,()=>o.status==='completed');assert.equal(s.inv.steel,steel-2);
});

test('paired underground construction uses real cargo and preserves tunnel roles',()=>{
 const st=ready(),s=chest(st,30),o=plan(st,11,5);walk(st,s.x,s.y);send(st,{type:'factory',action:{type:'chestPut',item:'copper',n:10,x:s.x,y:s.y}});
 // Labelled plan-only fixture, equivalent to a validated import; material supply remains ordinary.
 o.blueprint=parseBlueprint({version:1,name:'Tunnel pair',entities:[{id:'a',kind:'underground',x:0,y:0,dir:1,underground:'input'},{id:'b',kind:'underground',x:4,y:0,dir:1,underground:'output'}]});
 work(st,{type:'start',sourceId:s.id,orderIds:[o.id]});until(st,()=>o.status==='completed');assert.equal(machineAt(st,o.x,o.y)!.underground,'input');assert.equal(machineAt(st,o.x+4,o.y)!.underground,'output');assert.ok(conservation(st).ok);
});

test('ordinary station restoration, Foreman recruitment, paid chest and copied plan fully replay through automated delivery',()=>{
 const {st,log}=expedition(),r=st.campaign!.recruits!.sites.find(r=>r.kind==='foreman')!;walk(st,r.x,r.y);send(st,{type:'recruitSurvivors',id:r.id});const s=chest(st),o=plan(st,11,2,true);
 assert.ok(conservation(st).ok);assert.equal(stateHash(replayInterval(createCampaign(3),log,st.flow!.tick)),stateHash(st));
 if(process.env.P8_TRUCK_FIXTURE)writeFileSync(process.env.P8_TRUCK_FIXTURE,JSON.stringify(makeSave(st,{log,logComplete:true,params:{seed:3,ruleset:'exploration-v2',map:'city',view:'world'}})));
 work(st,{type:'start',sourceId:s.id,orderIds:[o.id]});until(st,()=>st.campaign!.truck!.work!.phase==='complete');assert.equal(o.status,'completed');assert.ok(conservation(st).ok);assert.equal(stateHash(replayInterval(createCampaign(3),log,st.flow!.tick)),stateHash(st));
});


test('packed machines from a supply chest replace material costs without touching pockets',()=>{
 const st=ready(),s=chest(st,0),o=plan(st);walk(st,s.x,s.y);let p:{x:number;y:number}|undefined;
 for(let y=s.y-3;y<s.y+4&&!p;y++)for(let x=s.x-3;x<s.x+4;x++)if(canPlace(st,'belt',x,y).ok&&inReach(st,x,y)){p={x,y};break;}assert.ok(p);
 for(let i=0;i<2;i++){send(st,{type:'construct',edits:[{action:'place',item:'belt',...p,dir:1}]});send(st,{type:'construct',edits:[{action:'pickUp',...p}]});send(st,{type:'factory',action:{type:'chestPut',item:'belt',n:1,x:s.x,y:s.y}});}
 const paid=structuredClone(st.flow!.stats.placed),pockets={...st.engineer.inv};work(st,{type:'start',sourceId:s.id,orderIds:[o.id]});until(st,()=>o.status==='completed');assert.equal(s.inv.belt??0,0);assert.deepEqual(st.flow!.stats.placed,paid);assert.deepEqual(st.engineer.inv,pockets);assert.ok(conservation(st).ok);
});


test('order pause/resume, retry and source reassignment are idempotent and retain cargo through interruptions',()=>{
 const st=ready(),s=chest(st,0),o=plan(st),t=st.campaign!.truck!;const action={type:'start' as const,sourceId:s.id,orderIds:[o.id]};work(st,action);until(st,()=>t.work!.reason.includes('shortage'));
 work(st,{type:'retry'});let hash=stateHash(st);work(st,{type:'retry'});assert.equal(stateHash(st),hash);work(st,action);assert.equal(stateHash(st),hash);work(st,{type:'resume'});assert.equal(stateHash(st),hash);
 send(st,{type:'blueprintOrder',action:{type:'pause',id:o.id}});hash=stateHash(st);send(st,{type:'blueprintOrder',action:{type:'pause',id:o.id}});assert.equal(stateHash(st),hash);tick(st,3);assert.match(t.work!.reason,/paused/);assert.ok(t.work!.enabled);const xy=[t.x,t.y],cargo={...t.cargo};tick(st,3);assert.deepEqual([t.x,t.y],xy);assert.deepEqual(t.cargo,cargo);send(st,{type:'blueprintOrder',action:{type:'build',id:o.id}},false);
 let copy=loadState(st);assert.equal(stateHash(copy),stateHash(st));const bad=structuredClone(st);bad.campaign!.plans!.orders[0].paused='yes' as never;assert.throws(()=>loadState(bad),/pause/);send(st,{type:'blueprintOrder',action:{type:'resume',id:o.id}});hash=stateHash(st);send(st,{type:'blueprintOrder',action:{type:'resume',id:o.id}});assert.equal(stateHash(st),hash);
 walk(st,s.x,s.y);send(st,{type:'construct',edits:[{action:'pickUp',x:s.x,y:s.y}]});tick(st,3);assert.match(t.work!.reason,/missing/);const replacement=chest(st,10);work(st,{type:'source',sourceId:replacement.id});hash=stateHash(st);work(st,{type:'source',sourceId:replacement.id});assert.equal(stateHash(st),hash);assert.deepEqual(t.cargo,cargo);
 until(st,()=>t.work!.phase==='travelling'&&t.cargo.steel===2);hash=stateHash(st);work(st,{type:'resume'});assert.equal(stateHash(st),hash);work(st,{type:'start',sourceId:replacement.id,orderIds:[o.id]});assert.equal(stateHash(st),hash);
 send(st,{type:'blueprintOrder',action:{type:'pause',id:o.id}});tick(st,1);assert.equal(o.status,'waiting');assert.equal(t.cargo.steel,2);send(st,{type:'blueprintOrder',action:{type:'cancel',id:o.id}});hash=stateHash(st);send(st,{type:'blueprintOrder',action:{type:'cancel',id:o.id}});assert.equal(stateHash(st),hash);tick(st,.05);assert.equal(t.work!.phase,'complete');assert.equal(t.cargo.steel,2);assert.ok(conservation(st).ok);
 copy=loadState(st);assert.equal(stateHash(copy),stateHash(st));const next=plan(st,14);assert.notDeepEqual([next.x,next.y],[o.x,o.y]);const supply= replacement.inv.steel;work(st,{type:'start',sourceId:replacement.id,orderIds:[next.id]});until(st,()=>next.status==='completed');assert.equal(replacement.inv.steel,supply);assert.equal(t.cargo.steel??0,0);assert.equal(o.status,'cancelled');assert.ok(conservation(st).ok);
});


test('area removal packs an overlaid tram before its track and restores freight cargo without duplication',()=>{
 const st=ready(),t=st.campaign!.truck!;let p:{x:number;y:number}|undefined;for(let y=Math.floor(t.y)-8;y<t.y+8&&!p;y++)for(let x=Math.floor(t.x)-8;x<t.x+8;x++)if(canPlace(st,'track',x,y).ok){p={x,y};break;}assert.ok(p);walk(st,p.x,p.y);
 send(st,{type:'construct',edits:[{action:'place',item:'track',...p,dir:0},{action:'place',item:'tram',...p,dir:0}]});const tram=tramAt(st,p.x,p.y)!;tram.cargo={steel:3}; // labelled loaded cargo fixture
 const steel=st.engineer.inv.steel,preview=removalPreview(st,p,p);assert.ok(preview.ok,preview.reason);assert.deepEqual(preview.selection!.machines.map(m=>m.item),['tram','track']);send(st,{type:'removeArea',selection:preview.selection!});assert.equal(st.engineer.inv.steel,steel+3);assert.ok(!tramAt(st,p.x,p.y));assert.ok(!machineAt(st,p.x,p.y));send(st,{type:'undoBuild'});assert.equal(tramAt(st,p.x,p.y)!.id,tram.id);assert.equal(tramAt(st,p.x,p.y)!.cargo?.steel??0,0);assert.equal(machineAt(st,p.x,p.y)!.kind,'track');send(st,{type:'redoBuild'});assert.equal(st.engineer.inv.steel,steel+3);
});
