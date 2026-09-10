import {suppliedCampaign as createCampaign} from './suppliedCampaignFixture';
// Prepared historical starting stock; current ungranted progression is checked in gameplayCorrections.test.ts.
import {test} from 'node:test';
import assert from 'node:assert/strict';
import {applyCommands,actionResult,canPlace,inReach,stateHash,loadState,conservation,blueprintOrderInfo,blueprintQueueCheck,exportBlueprintEntry,blueprintPlansProblem,parseBlueprint,ground, type SimState,type Command,type BlueprintLibraryAction,type BlueprintOrderAction} from '../src/index';
import {FactoryDriver,replayInterval} from '../../harness/src/blueprint';
function send(st:SimState,c:Command,ok=true){applyCommands(st,[c]);assert.equal(actionResult(st).ok,ok,actionResult(st).reason);}
const library=(st:SimState,action:BlueprintLibraryAction,ok=true)=>send(st,{type:'blueprintLibrary',action},ok);
const order=(st:SimState,action:BlueprintOrderAction,ok=true)=>send(st,{type:'blueprintOrder',action},ok);
const meta={name:'Ammo route',folder:'Defence',icon:'belt' as const};
const bp=()=>parseBlueprint({version:1,name:'Ammo route',entities:[{id:'a',kind:'belt',x:0,y:0,dir:1},{id:'b',kind:'belt',x:1,y:0,dir:1}]});
function ready(){const st=createCampaign();send(st,{type:'factory',action:{type:'chestTake',item:'steel',n:100}});send(st,{type:'factory',action:{type:'chestTake',item:'copper',n:60}});const r=st.campaign!.recruits!.sites.find(r=>r.kind==='foreman')!;r.seenAt=r.recruitedAt=0;st.campaign!.clipboard=bp();return st;}
function spot(st:SimState){for(let y=Math.floor(st.engineer.y)-5;y<st.engineer.y+5;y++)for(let x=Math.floor(st.engineer.x)-5;x<st.engineer.x+5;x++)if(canPlace(st,'belt',x,y).ok&&canPlace(st,'belt',x+1,y).ok&&inReach(st,x,y,2,1))return {x,y};throw Error('no space');}
const build=(x:number,y:number):Command=>({type:'construct',edits:[{action:'place',item:'belt',x,y,dir:1}]});

test('library import/export is bounded, atomic, Foreman-gated and independent of clipboard and orders',()=>{
 const locked=createCampaign(),hash=stateHash(locked);library(locked,{type:'import',text:JSON.stringify({format:'relight-blueprint',version:1,...meta,blueprint:bp()})},false);assert.equal(stateHash(locked),hash);
 const st=ready();library(st,{type:'save',...meta});const p=st.campaign!.plans!,id=p.library[0].id,text=exportBlueprintEntry(st,id);
 order(st,{type:'queue',...spot(st)});const o=st.campaign!.plans!.orders[0];
 library(st,{type:'update',id,name:'Renamed',folder:'Production',icon:'assembler'});assert.equal(o.blueprint.name,meta.name);
 library(st,{type:'remove',id});assert.equal(st.campaign!.plans!.orders[0].blueprint.name,meta.name);
 library(st,{type:'import',text});const next=st.campaign!.plans!.library[0];assert.ok(next.id>o.id);assert.equal(exportBlueprintEntry(st,next.id),text);
 send(st,{type:'blueprintTransform',operation:'rotate'});assert.deepEqual(next.blueprint,bp());library(st,{type:'load',id:next.id});assert.deepEqual(st.campaign!.clipboard,bp());
 assert.equal(stateHash(loadState(st)),stateHash(st));assert.ok(conservation(st).ok);
 for(const bad of ['{',text+'x','x'.repeat(262145),JSON.stringify({...JSON.parse(text),contents:100}),JSON.stringify({...JSON.parse(text),icon:'depot'}),JSON.stringify({...JSON.parse(text),blueprint:{...bp(),entities:[...bp().entities,{...bp().entities[0],id:'c'}]}})]){const before=stateHash(st);library(st,{type:'import',text:bad},false);assert.equal(stateHash(st),before);}
});

test('library limits and metadata rejection preserve allocator and current selection',()=>{
 const st=ready();for(let i=0;i<32;i++)library(st,{type:'save',...meta,name:`Plan ${i}`});const hash=stateHash(st);
 library(st,{type:'save',...meta},false);library(st,{type:'update',id:1,...meta,folder:'x'.repeat(65)},false);assert.equal(stateHash(st),hash);
 library(st,{type:'remove',id:1});library(st,{type:'save',...meta});assert.equal(st.campaign!.plans!.library.at(-1)!.id,33);
});

test('ghost plans consume no stock or occupancy, reject overlap and bounds, and report exact missing stock',()=>{
 const st=ready(),p=spot(st),flow=structuredClone(st.flow),inv={...st.engineer.inv};order(st,{type:'queue',...p});assert.deepEqual(st.flow,flow);assert.deepEqual(st.engineer.inv,inv);
 const o=st.campaign!.plans!.orders[0],hash=stateHash(st);order(st,{type:'queue',...p},false);order(st,{type:'queue',x:-1,y:0},false);order(st,{type:'queue',x:0,y:ground(st).th},false);assert.equal(stateHash(st),hash);assert.ok(canPlace(st,'belt',p.x,p.y).ok);
 // Labelled stock-exhaustion fixture isolates planning before supplies; no conservation claim on this fixture.
 st.engineer.inv.steel=0;assert.deepEqual(blueprintOrderInfo(st,o).missing,{steel:2});const poor=stateHash(st);order(st,{type:'build',id:o.id},false);assert.equal(stateHash(st),poor);
 st.engineer.inv.belt=1;assert.deepEqual(blueprintOrderInfo(st,o).missing,{steel:1});
 order(st,{type:'cancel',id:o.id});assert.equal(o.status,'cancelled');const cancelled=stateHash(st);order(st,{type:'build',id:o.id},false);assert.equal(stateHash(st),cancelled);order(st,{type:'queue',...p});assert.equal(st.campaign!.plans!.orders.at(-1)!.status,'waiting');
});

test('partial manual construction survives save/load; remaining build pays once and completed orders never requeue on undo',()=>{
 let st=ready();const p=spot(st);order(st,{type:'queue',...p});const id=st.campaign!.plans!.orders[0].id;send(st,build(p.x,p.y));assert.equal(blueprintOrderInfo(st,st.campaign!.plans!.orders[0]).built,1);
 st=loadState(st);assert.equal(blueprintOrderInfo(st,st.campaign!.plans!.orders[0]).built,1);const steel=st.engineer.inv.steel;order(st,{type:'build',id});assert.equal(st.engineer.inv.steel,steel-1);assert.equal(st.campaign!.plans!.orders[0].status,'completed');const hash=stateHash(st);order(st,{type:'build',id},false);assert.equal(stateHash(st),hash);
 send(st,{type:'undoBuild'});assert.equal(st.campaign!.plans!.orders[0].status,'completed');send(st,{type:'redoBuild'});order(st,{type:'remove',id});assert.equal(st.campaign!.plans!.orders.length,0);assert.ok(conservation(st).ok);
});

test('ordinary matching construction completes waiting orders, occupied mismatches and remote builds remain blocked',()=>{
 const st=ready(),p=spot(st);order(st,{type:'queue',...p});const o=st.campaign!.plans!.orders[0];send(st,build(p.x,p.y));send(st,build(p.x+1,p.y));assert.equal(o.status,'completed');
 order(st,{type:'queue',...p});assert.equal(st.campaign!.plans!.orders.at(-1)!.status,'completed');
 send(st,{type:'construct',edits:[{action:'rotate',...p}]});order(st,{type:'queue',...p});const blocked=st.campaign!.plans!.orders.at(-1)!;assert.equal(blocked.status,'waiting');assert.match(blueprintOrderInfo(st,blocked).blockers.join(),/machine/);const hash=stateHash(st);order(st,{type:'build',id:blocked.id},false);assert.equal(stateHash(st),hash);
 order(st,{type:'cancel',id:blocked.id});order(st,{type:'queue',x:p.x+50,y:p.y});assert.match(blueprintOrderInfo(st,st.campaign!.plans!.orders.at(-1)!).blockers.join(),/closer/);
});

test('order limits include history, cancel is terminal and deleting records never recycles IDs',()=>{
 const st=ready(),p=spot(st);for(let i=0;i<32;i++){order(st,{type:'queue',...p});order(st,{type:'cancel',id:i+1});}const hash=stateHash(st);order(st,{type:'queue',...p},false);assert.equal(stateHash(st),hash);
 order(st,{type:'remove',id:1});order(st,{type:'queue',...p});assert.equal(st.campaign!.plans!.orders.at(-1)!.id,33);order(st,{type:'remove',id:33},false);assert.equal(blueprintPlansProblem(st),'');
});

test('hostile saved libraries and order lifecycle data are rejected; old optional state remains absent',()=>{
 const old=createCampaign();assert.equal(loadState(old).campaign!.plans,undefined);
 const st=ready();library(st,{type:'save',...meta});order(st,{type:'queue',...spot(st)});
 const mutations=[(s:SimState)=>s.campaign!.plans!.nextId=1,(s:SimState)=>s.campaign!.plans!.orders[0].x=-1,(s:SimState)=>s.campaign!.plans!.orders[0].finishedAt=0,(s:SimState)=>s.campaign!.plans!.orders[0].createdAt=1,(s:SimState)=>s.campaign!.plans!.orders.push(structuredClone(s.campaign!.plans!.orders[0])),(s:SimState)=>s.campaign!.plans!.library[0].blueprint.entities[0].x=NaN];
 for(const mutate of mutations){const bad=structuredClone(st);mutate(bad);assert.throws(()=>loadState(bad),/blueprint plans/);}
 assert.equal(stateHash(loadState(st)),stateHash(st));
});

test('imported locked machines remain plans and cannot bypass ordinary unlocks',()=>{
 const st=ready();const locked=parseBlueprint({version:1,name:'Locked',entities:[{id:'a',kind:'arclamp',x:0,y:0,dir:0}]});library(st,{type:'import',text:JSON.stringify({format:'relight-blueprint',version:1,name:'Locked',folder:'',icon:'arclamp',blueprint:locked})});const p=spot(st);order(st,{type:'queue',...p});const o=st.campaign!.plans!.orders[0],hash=stateHash(st);assert.ok(blueprintOrderInfo(st,o).blockers.length);order(st,{type:'build',id:o.id},false);assert.equal(stateHash(st),hash);
});

test('ordinary opening recruitment, library actions, ghost construction and cancellation replay deterministically',()=>{
 const st=createCampaign(3),initial=structuredClone(st),d=new FactoryDriver(st),home={x:st.engineer.x,y:st.engineer.y};d.take('steel',50);const r=st.campaign!.recruits!.sites.find(r=>r.kind==='foreman')!;d.approach(r.x,r.y);d.send({type:'recruitSurvivors',id:r.id});d.walk(home.x,home.y);
 const p=spot(st);d.send(build(p.x,p.y));d.send({type:'blueprintCopy',from:p,to:p});d.send({type:'blueprintLibrary',action:{type:'save',...meta}});d.send({type:'blueprintOrder',action:{type:'queue',x:p.x+1,y:p.y}});d.send({type:'blueprintLibrary',action:{type:'remove',id:1}});d.send({type:'blueprintOrder',action:{type:'build',id:2}});assert.equal(st.campaign!.plans!.orders[0].status,'completed');assert.ok(conservation(st).ok);assert.equal(stateHash(replayInterval(initial,d.log,st.flow!.tick)),stateHash(st));assert.equal(stateHash(loadState(st)),stateHash(st));assert.ok(!blueprintQueueCheck(st,-1,0).ok);
});


test('partly built underground plans reject interception atomically and preserve the intended pair',()=>{
 const st=ready();let p:{x:number;y:number}|undefined;
 for(let y=Math.floor(st.engineer.y)-4;y<st.engineer.y+4&&!p;y++)for(let x=Math.floor(st.engineer.x)-4;x<st.engineer.x+3;x++)if([0,1,2,3,4].every(dx=>canPlace(st,'underground',x+dx,y,1).ok&&inReach(st,x+dx,y))){p={x,y};break;}
 assert.ok(p);st.campaign!.clipboard=parseBlueprint({version:1,name:'Tunnel',entities:[{id:'a',kind:'underground',x:0,y:0,dir:1,underground:'input'},{id:'b',kind:'underground',x:4,y:0,dir:1,underground:'output'}]});
 order(st,{type:'queue',...p});const id=st.campaign!.plans!.orders[0].id;
 send(st,{type:'construct',edits:[{action:'place',item:'underground',...p,dir:1,underground:'input'},{action:'place',item:'underground',x:p.x+2,y:p.y,dir:1,underground:'output'}]});
 const hash=stateHash(st);order(st,{type:'build',id},false);assert.equal(stateHash(st),hash);
 send(st,{type:'construct',edits:[{action:'pickUp',x:p.x+2,y:p.y}]});order(st,{type:'build',id});assert.equal(st.campaign!.plans!.orders[0].status,'completed');assert.ok(conservation(st).ok);
});

test('largest freight-rich portable library entry round-trips within the bounded text size',()=>{
 const st=ready(),freight=Object.fromEntries(['steel','copper','coal','wire','frame','board','magazine','stone','concrete'].map(k=>[k,{request:20,reserve:10,export:true}]));
 st.campaign!.clipboard=parseBlueprint({version:1,name:meta.name,entities:Array.from({length:128},(_,i)=>({id:`station${i}`,kind:'tramstop',x:(i%16)*2,y:Math.floor(i/16)*2,dir:0,freight}))});
 library(st,{type:'save',...meta,icon:'tramstop'});const text=exportBlueprintEntry(st,1);assert.ok(text.length<=262144);library(st,{type:'import',text});assert.deepEqual(st.campaign!.plans!.library[0].blueprint,st.campaign!.plans!.library[1].blueprint);assert.equal(blueprintPlansProblem(st),'');
});
