import { FactoryDriver } from '../../../packages/harness/src/blueprint';
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { createCampaign, applyCommands, advanceFlow, inReach, findPath, ground, canPlace, machineAt,
  MACHINE_SIZE, placeable, baseCore, lockReason, recruitCheck, conservation, makeSave, loadState, stateHash, inspectMachine,
  machineStatus, damageDefence, defenceHp, passable, canPickUp, accepts, actionResult, tickCampaignSchedule,
  campaignOrigin, openLedger, blockOfTile, type SimState, type Command, type Kind, type Machine } from '../../../packages/sim/src/index';
import { createSession, parseUrl, replaySession } from '../../../packages/game/src/session';
import { threatOf } from '../../../packages/sim/src/threat';
import { shotAssemblers } from '../../../packages/sim/src/goal';

const run=(st:SimState,s:number)=>advanceFlow(st,s,[],Math.ceil(s*20)+1);
function walk(st:SimState,x:number,y:number,send=(c:Command)=>applyCommands(st,[c])):void {
 const driver=new FactoryDriver(st);driver.send=send;driver.approach(x,y,machineAt(st,x,y)?.size??1);
}
function unlock(st:SimState):void { // Labelled local-recruit fixture for isolated mechanics tests.
  const s=st.campaign!.recruits!.sites.find(s=>s.kind==='concrete')!;
  s.seenAt=0;s.recruitedAt=0;
}
function supplies(st:SimState):void {applyCommands(st,[{type:'chestTake',item:'steel',n:120},{type:'chestTake',item:'copper',n:50},{type:'chestTake',item:'stone',n:40}]);}
function line(st:SimState,send=(c:Command)=>applyCommands(st,[c])) {
  const b=ground(st).opening!.bounds;
  for(let y=b.y+2;y<b.y+b.size-4;y++)for(let x=b.x+5;x<b.x+b.size-6;x++) {
    const plan:[Kind,number,number][]=[['mixer',x,y],['chest',x-4,y+1],['inserter',x-2,y+1],['belt',x-1,y+1],['inserter',x+3,y+1],['chest',x+4,y+1]];
    if(plan.some(([k,xx,yy])=>!canPlace(st,k,xx,yy,1).ok||blockOfTile(st,xx,yy)!==st.campaign!.homeBlock))continue;
    for(const [item,xx,yy] of plan) {
      if(!inReach(st,xx,yy,MACHINE_SIZE[item]))walk(st,xx,yy,send);
      send({type:'construct',edits:[{action:'place',item,x:xx,y:yy,dir:1}]});assert.ok(actionResult(st).ok,actionResult(st).reason);
    }
    return {m:machineAt(st,x,y)!,input:machineAt(st,x-4,y+1)!,output:machineAt(st,x+4,y+1)!,arm:machineAt(st,x+3,y+1)!};
  }throw new Error('no home concrete line layout');
}
function put(st:SimState,m:Machine,item:string,n:number,send=(c:Command)=>applyCommands(st,[c])) {
  if(!inReach(st,m.x,m.y,m.size))walk(st,m.x,m.y,send);
  send({type:'factory',action:{type:'chestPut',x:m.x,y:m.y,item,n}});assert.ok(actionResult(st).ok,actionResult(st).reason);
}
const conserved=(st:SimState)=>assert.ok(conservation(st).ok,conservation(st).problems.join(', '));


import {rubbleAt,TURBINE,turbineCheck,campaignGrid,describeTurbine} from '../../../packages/sim/src/index';
import {writeFileSync} from 'node:fs';
function pod(d:FactoryDriver,core:NonNullable<ReturnType<typeof baseCore>>){
 const s=d.state,points:{x:number;y:number;distance:number}[]=[];
 for(let y=core.y-8;y<=core.y+core.size+6;y++)for(let x=core.x-8;x<=core.x+core.size+6;x++){
  if(blockOfTile(s,x,y)!==core.block||blockOfTile(s,x-3,y)!==core.block)continue;
  if(!placeable(s,'turret',x,y)&&!placeable(s,'inserter',x-1,y,1)&&!placeable(s,'chest',x-3,y))points.push({x,y,distance:Math.hypot(x+1-core.x-core.size/2,y+1-core.y-core.size/2)});
 }
 const p=points.sort((a,b)=>a.distance-b.distance||a.y-b.y||a.x-b.x)[0];if(!p)throw Error('No supply pod at base '+core.block);
 const chest=d.place('chest',p.x-3,p.y),arm=d.place('inserter',p.x-1,p.y,1),turret=d.place('turret',p.x,p.y);
 d.send({type:'factory',action:{type:'routing',x:arm.x,y:arm.y,filter:'magazine'}});return {chest,arm,turret,block:core.block};
}
function mine(d:FactoryDriver,item:'steel'|'copper'|'coal',target:number):void {
 const s=d.state,G=ground(s),home=s.campaign!.homeBlock;
 while((s.engineer.inv[item]??0)<target){
  let at:{x:number;y:number;units:number}|undefined;
  for(let t=0;t<G.patch.length;t++)if(G.patch[t]&&G.owner[t]===home){const r=rubbleAt(s,t%G.tw,Math.floor(t/G.tw));if(r?.type===item){at={x:t%G.tw,y:Math.floor(t/G.tw),units:r.units};break;}}
  if(!at)throw Error('No remaining home '+item);d.approach(at.x,at.y);d.send({type:'factory',action:{type:'mineAt',x:at.x,y:at.y}});
  const before=s.engineer.inv[item]??0;d.run(Math.min(30,target-before,at.units)+1);d.send({type:'factory',action:{type:'mineAt',x:-1,y:-1}});
  if((s.engineer.inv[item]??0)<=before)throw Error('No mining progress '+item);
 }
}

const seed=Number(process.argv[2]??3),reverse=process.argv[3]==='reverse';
Object.defineProperty(globalThis,'location',{configurable:true,value:{href:'http://localhost/'}});
const session=createSession(parseUrl('?rules=exploration-v2&seed='+seed+'&view=world')),st=session.state;
const send=(c:Command)=>{session.log.push({tick:st.flow!.tick,c:structuredClone(c)});applyCommands(st,[c]);};
for(const [item,n] of [['steel',200],['copper',100],['stone',50],['coal',20],['magazine',20]] as const)send({type:'chestTake',item,n});
const home=[Math.floor(st.engineer.x),Math.floor(st.engineer.y)],G=ground(st);
const driver=new FactoryDriver(st);driver.send=send;
const pods=[pod(driver,baseCore(st,st.campaign!.homeBlock)!),pod(driver,baseCore(st,st.campaign!.homeBlock)!)];
for(const p of pods)driver.put('magazine',10,p.chest);
mine(driver,'steel',240);

for(const kind of (reverse?['surveyors','lamplighters','electricians','concrete']:['electricians','concrete','lamplighters','surveyors'])){const r=st.campaign!.recruits!.sites.find(r=>r.kind===kind)!;walk(st,r.x,r.y,send);send({type:'recruitSurvivors',id:r.id});assert.ok(r.recruitedAt>=0);}
walk(st,home[0],home[1],send);
for(const t of G.blocks[st.campaign!.homeBlock].tiles){if((st.engineer.inv.stone??0)>=112)break;const x=t%G.tw,y=Math.floor(t/G.tw);if(rubbleAt(st,x,y)?.type!=='stone')continue;walk(st,x,y,send);send({type:'mineAt',x,y});for(let i=0;i<40&&(st.engineer.inv.stone??0)<112&&rubbleAt(st,x,y);i++)run(st,1);send({type:'mineAt',x:home[0],y:home[1]});}
assert.ok((st.engineer.inv.stone??0)>=112,'ordinary home rubble supplies 112 stone');
walk(st,home[0],home[1],send);const {input,output}=line(st,send);put(st,input,'stone',112,send);run(st,124);
assert.equal(st.flow!.stats.made.concrete,56);walk(st,output.x,output.y,send);send({type:'chestTake',x:output.x,y:output.y,item:'concrete',n:56});assert.equal(st.engineer.inv.concrete,56);
const hall=st.campaign!.turbine!;walk(st,hall.x-1,hall.y,send);
let gen:Machine|undefined;
for(let r=1;r<25&&!gen;r++)for(let y=hall.y-r;y<=hall.y+r&&!gen;y++)for(let x=hall.x-r;x<=hall.x+r;x++)if(blockOfTile(st,x,y)===hall.block&&canPlace(st,'generator',x,y).ok){walk(st,x,y,send);send({type:'place',item:'generator',x,y});gen=machineAt(st,x,y);break;}
assert.ok(gen);walk(st,gen.x,gen.y,send);send({type:'feed',x:gen.x,y:gen.y});walk(st,hall.x-1,hall.y,send);

send({type:'deliverTurbine'});assert.equal(turbineCheck(st),'');send({type:'restoreTurbine'});
let lamp:Machine|undefined;for(let r=1;r<15&&!lamp;r++)for(let y=hall.y-r;y<=hall.y+r&&!lamp;y++)for(let x=hall.x-r;x<=hall.x+r;x++)if(blockOfTile(st,x,y)===hall.block&&canPlace(st,'arclamp',x,y).ok){walk(st,x,y,send);send({type:'construct',edits:[{action:'place',item:'arclamp',x,y,dir:0}]});lamp=machineAt(st,x,y);break;}
assert.ok(lamp);run(st,5);assert.equal(campaignGrid(st).turbineOutput,12);conserved(st);

// Return through ordinary paths; pay for the optional electrical devices at Home Court.
walk(st,home[0],home[1],send);
const equipment:Machine[]=[];
for(const k of ['bigpole','floodlight'] as Kind[]){let built:Machine|undefined;for(let r=1;r<15&&!built;r++)for(let y=home[1]-r;y<=home[1]+r&&!built;y++)for(let x=home[0]-r;x<=home[0]+r;x++)if(blockOfTile(st,x,y)===st.campaign!.homeBlock&&canPlace(st,k,x,y).ok){walk(st,x,y,send);send({type:'construct',edits:[{action:'place',item:k,x,y,dir:0}]});built=machineAt(st,x,y);break;}assert.ok(built,k);equipment.push(built);}
const gate:number[]=[];
for(let i=0;i<4;i++){let built=false;for(let r=3;r<12&&!built;r++)for(let y=home[1]-r;y<=home[1]+r&&!built;y++)for(let x=home[0]-r;x<=home[0]+r;x++)if(blockOfTile(st,x,y)===st.campaign!.homeBlock&&canPlace(st,'barricade',x,y).ok){walk(st,x,y,send);send({type:'construct',edits:[{action:'place',item:'barricade',x,y,dir:0}]});assert.ok(actionResult(st).ok,actionResult(st).reason);gate.push(y*G.tw+x);built=true;break;}assert.ok(built);}
walk(st,home[0],home[1],send);
const before=st.t,raids=new Set<number>(),damaged=new Set<number>();
for(let i=0;i<700;i++){run(st,1);if(st.campaign!.defence!.minor)raids.add(st.campaign!.defence!.minor.id);for(const tile of gate){const m=machineAt(st,tile%G.tw,Math.floor(tile/G.tw))!;if(defenceHp(m)<240)damaged.add(m.id);}if(st.t>650&&!st.campaign!.defence!.minor)break;}
assert.ok(raids.size>0,'naturally scheduled live raid');assert.ok(st.campaign!.defence!.bases[0].hp>0,'home survives');assert.ok(st.flow!.stats.fired>0,'paid turret ammunition used');
const beforeRepair=st.flow!.stats.repaired??0;
for(const tile of gate){const m=machineAt(st,tile%G.tw,Math.floor(tile/G.tw))!;if(defenceHp(m)===240)continue;walk(st,m.x,m.y,send);for(let i=0;i<6&&defenceHp(m)<240;i++){send({type:'repairDefence',x:m.x,y:m.y});run(st,4.1);}assert.equal(defenceHp(m),240);}
assert.equal(st.campaign!.defence!.bases.length,1);conserved(st);
const saved=loadState(makeSave(st));assert.equal(stateHash(saved),stateHash(st));run(saved,2);const copy=loadState(makeSave(st));run(copy,2);assert.equal(stateHash(saved),stateHash(copy));
const replay=replaySession(session);assert.ok('state' in replay,JSON.stringify(replay));if('state' in replay)assert.equal(stateHash(replay.state),stateHash(st));
const name=`seed${seed}-${reverse?'reverse':'forward'}`;writeFileSync(`docs/evidence/p7-05-2026-09-08/${name}.json`,JSON.stringify(makeSave(st,{log:session.log,logComplete:true})));
console.log(JSON.stringify({name,seed,order:reverse?'survey-first':'electrical-first',t:st.t,setupAt:before,commands:session.log.length,hash:stateHash(st),conservation:true,replay:true,save:true,raids:[...raids],damagedBarricades:damaged.size,coreHp:st.campaign!.defence!.bases[0].hp,rounds:st.flow!.stats.fired,concreteMade:st.flow!.stats.made.concrete,equipment:equipment.map(m=>m.kind),turbine:describeTurbine(st),remaining:st.engineer.inv}));
