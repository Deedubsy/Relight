import { test } from 'node:test';
import assert from 'node:assert/strict';
import { createCampaign, applyCommands, advanceFlow, inReach, findPath, ground, canPlace, machineAt,
  MACHINE_SIZE, lockReason, recruitCheck, conservation, makeSave, loadState, stateHash, inspectMachine,
  machineStatus, damageDefence, defenceHp, passable, canPickUp, accepts, actionResult, tickCampaignSchedule,
  campaignOrigin, openLedger, blockOfTile, type SimState, type Command, type Kind, type Machine } from '../../../packages/sim/src/index';
import { createSession, parseUrl, replaySession } from '../../../packages/game/src/session';
import { threatOf } from '../../../packages/sim/src/threat';
import { shotAssemblers } from '../../../packages/sim/src/goal';

const run=(st:SimState,s:number)=>advanceFlow(st,s,[],Math.ceil(s*20)+1);
function walk(st:SimState,x:number,y:number,send=(c:Command)=>applyCommands(st,[c])):void {
  send({type:'move',x:x+.5,y:y+.5});for(let i=0;i<180&&st.engineer.target;i++)run(st,1);
  assert.ok(inReach(st,x,y),`reach ${x},${y}`);
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
Object.defineProperty(globalThis,'location',{configurable:true,value:{href:'http://localhost/'}});
const session=createSession(parseUrl('?rules=exploration-v2&seed=3&view=world')),st=session.state;
const send=(c:Command)=>{session.log.push({tick:st.flow!.tick,c:structuredClone(c)});applyCommands(st,[c]);};
for(const [item,n] of [['steel',180],['copper',80],['stone',50],['coal',20]] as const)send({type:'chestTake',item,n});
const home=[Math.floor(st.engineer.x),Math.floor(st.engineer.y)],G=ground(st);
for(const kind of ['concrete','lamplighters','surveyors']){const r=st.campaign!.recruits!.sites.find(r=>r.kind===kind)!;walk(st,r.x,r.y,send);send({type:'recruitSurvivors',id:r.id});assert.ok(r.recruitedAt>=0);}
walk(st,home[0],home[1],send);
for(const t of G.blocks[st.campaign!.homeBlock].tiles){if((st.engineer.inv.stone??0)>=80)break;const x=t%G.tw,y=Math.floor(t/G.tw);if(rubbleAt(st,x,y)?.type!=='stone')continue;walk(st,x,y,send);send({type:'mineAt',x,y});for(let i=0;i<40&&(st.engineer.inv.stone??0)<80&&rubbleAt(st,x,y);i++)run(st,1);send({type:'mineAt',x:home[0],y:home[1]});}
assert.ok((st.engineer.inv.stone??0)>=80,'ordinary home rubble supplies 80 stone');
walk(st,home[0],home[1],send);const {input,output}=line(st,send);put(st,input,'stone',80,send);run(st,100);
assert.equal(st.flow!.stats.made.concrete,40);walk(st,output.x,output.y,send);send({type:'chestTake',x:output.x,y:output.y,item:'concrete',n:40});assert.equal(st.engineer.inv.concrete,40);
const hall=st.campaign!.turbine!;walk(st,hall.x-1,hall.y,send);
let gen:Machine|undefined;
for(let r=1;r<25&&!gen;r++)for(let y=hall.y-r;y<=hall.y+r&&!gen;y++)for(let x=hall.x-r;x<=hall.x+r;x++)if(blockOfTile(st,x,y)===hall.block&&canPlace(st,'generator',x,y).ok){walk(st,x,y,send);send({type:'place',item:'generator',x,y});gen=machineAt(st,x,y);break;}
assert.ok(gen);walk(st,gen.x,gen.y,send);send({type:'feed',x:gen.x,y:gen.y});walk(st,hall.x-1,hall.y,send);
const pending=makeSave(st,{log:session.log,logComplete:true});writeFileSync('docs/evidence/p7-03-2026-09-08/turbine-pending.json',JSON.stringify(pending,null,2));
send({type:'deliverTurbine'});assert.equal(turbineCheck(st),'');send({type:'restoreTurbine'});
let lamp:Machine|undefined;for(let r=1;r<15&&!lamp;r++)for(let y=hall.y-r;y<=hall.y+r&&!lamp;y++)for(let x=hall.x-r;x<=hall.x+r;x++)if(blockOfTile(st,x,y)===hall.block&&canPlace(st,'arclamp',x,y).ok){walk(st,x,y,send);send({type:'construct',edits:[{action:'place',item:'arclamp',x,y,dir:0}]});lamp=machineAt(st,x,y);break;}
assert.ok(lamp);run(st,5);assert.equal(campaignGrid(st).turbineOutput,12);conserved(st);
const replay=replaySession(session);assert.ok('state' in replay,JSON.stringify(replay));if('state' in replay)assert.equal(stateHash(replay.state),stateHash(st));
writeFileSync('docs/evidence/p7-03-2026-09-08/turbine-complete.json',JSON.stringify(makeSave(st,{log:session.log,logComplete:true}),null,2));
console.log(JSON.stringify({check:'ordinary starting stock + mined stone + powered concrete + transported materials + paid local generator + restored Turbine + paid Arc lamp',description:describeTurbine(st),t:st.t,commands:session.log.length,hash:stateHash(st),conservation:conservation(st).ok,replay:true,remaining:st.engineer.inv},null,2));
