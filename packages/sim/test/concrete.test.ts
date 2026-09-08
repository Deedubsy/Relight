import { test } from 'node:test';
import assert from 'node:assert/strict';
import { createCampaign, applyCommands, advanceFlow, inReach, findPath, ground, canPlace, machineAt,
  MACHINE_SIZE, lockReason, recruitCheck, conservation, makeSave, loadState, stateHash, inspectMachine,
  machineStatus, damageDefence, defenceHp, passable, canPickUp, accepts, actionResult, tickCampaignSchedule,
  campaignOrigin, openLedger, blockOfTile, type SimState, type Command, type Kind, type Machine } from '../src/index';
import { createSession, parseUrl, replaySession } from '../../game/src/session';
import { threatOf } from '../src/threat';
import { shotAssemblers } from '../src/goal';

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

test('Concrete crew sites are separate and reachable across 32 seeds; version 6 migration preserves the original recruit',()=>{
  for(let seed=1;seed<=32;seed++) {
    const st=createCampaign(seed),[e,c]=st.campaign!.recruits!.sites;
    assert.equal(c.kind,'concrete');assert.ok(Math.hypot(e.x-c.x,e.y-c.y)>24);
    assert.ok(findPath(st,Math.floor(st.engineer.x),Math.floor(st.engineer.y),c.x,c.y),`seed ${seed}`);
    assert.match(lockReason(st,'mixer'),/Concrete crew/);assert.match(canPlace(st,'pole',c.x,c.y).reason,/shelter/);
    delete st.campaign!.turbine;delete st.campaign!.knowledge;st.campaign!.version=6;st.campaign!.recruits={version:1,sites:[e]};
    const copy=loadState(st);assert.deepEqual(copy.campaign!.recruits!.sites[0],e);assert.deepEqual(copy.campaign!.recruits!.sites[1],c);
    assert.deepEqual(copy.flow!.machines,st.flow!.machines);assert.equal(stateHash(copy),stateHash(loadState(makeSave(copy))));
  }
});

test('ordinary recruitment and paid stone → belt → Mixer → concrete chest chain save and replay',()=>{
  const previous=Object.getOwnPropertyDescriptor(globalThis,'location');Object.defineProperty(globalThis,'location',{configurable:true,value:{href:'http://localhost/'}});
  let session:ReturnType<typeof createSession>;
  try{session=createSession(parseUrl('?rules=exploration-v2&seed=3&view=world'));}finally{if(previous)Object.defineProperty(globalThis,'location',previous);else Reflect.deleteProperty(globalThis,'location');}
  const st=session.state,send=(c:Command)=>{session.log.push({tick:st.flow!.tick,c:structuredClone(c)});applyCommands(st,[c]);};
  for(const [item,n] of [['steel',120],['copper',50],['stone',40]] as const)send({type:'factory',action:{type:'chestTake',item,n}});
  const home={x:Math.floor(st.engineer.x),y:Math.floor(st.engineer.y)},crew=st.campaign!.recruits!.sites.find(s=>s.kind==='concrete')!,stock={...st.engineer.inv};
  assert.notEqual(recruitCheck(st,crew.id),'');walk(st,crew.x,crew.y,send);assert.equal(recruitCheck(st,crew.id),'');
  send({type:'recruitSurvivors',id:crew.id});const at=crew.recruitedAt;send({type:'recruitSurvivors',id:crew.id});
  assert.equal(crew.recruitedAt,at);assert.deepEqual(st.engineer.inv,stock);assert.equal(st.campaign!.defence!.bases.length,1);
  walk(st,home.x,home.y,send);const {m,input,output,arm}=line(st,send);put(st,input,'stone',40,send);
  if(!inReach(st,arm.x,arm.y))walk(st,arm.x,arm.y,send);
  send({type:'factory',action:{type:'routing',x:arm.x,y:arm.y,filter:'concrete'}});
  run(st,50);assert.equal(output.inv.concrete,20);assert.equal(st.flow!.stats.made.concrete,20);assert.equal(st.flow!.stats.consumed.stone,40);
  const info=inspectMachine(st,m.id)!;assert.equal(info.recipe,'Concrete');assert.equal(info.draw,60);assert.equal(info.nominal.find(r=>r.item==='concrete')!.output,30);
  assert.equal(shotAssemblers(st).length,0,'a Mixer never supplies ammunition capacity');
  assert.ok(info.measured!.rows.find(r=>r.item==='concrete')!.produced>0);assert.equal(accepts(st,m,'steel'),false);
  if(!inReach(st,output.x,output.y,2))walk(st,output.x,output.y,send);
  send({type:'factory',action:{type:'chestTake',x:output.x,y:output.y,item:'concrete',n:20}});assert.equal(st.engineer.inv.concrete,20);
  // Concrete is a normal carried/Depot item, with no source or sink for either transfer.
  walk(st,home.x,home.y,send);send({type:'factory',action:{type:'chestPut',item:'concrete',n:20}});
  send({type:'factory',action:{type:'chestTake',item:'concrete',n:20}});assert.equal(st.engineer.inv.concrete,20);
  conserved(st);const copy=loadState(makeSave(st));copy.speed=st.speed;run(st,1);run(copy,1);assert.equal(stateHash(st),stateHash(copy));
  const replay=replaySession(session);assert.ok('state' in replay,JSON.stringify(replay));if('state' in replay)assert.equal(stateHash(st),stateHash(replay.state));
});

test('Mixer pauses without power, retains a paid partial craft, refuses busy packing and stops at full output',()=>{
  const st=createCampaign();unlock(st);supplies(st);const {m,input,output,arm}=line(st);put(st,input,'stone',40);
  for(let i=0;i<100&&!m.busy;i++)run(st,.05);assert.ok(m.busy);assert.match(canPickUp(st,m.x,m.y).reason,/finish/);
  // Labelled outage fixture disables the core while preserving all fuel/materials.
  const core=st.campaign!.defence!.bases[0],hp=core.hp;core.hp=0;st.flow!.rev++;
  const timer=m.timer;run(st,3);assert.equal(m.timer,timer);assert.equal(machineStatus(st,m).state,'off');
  const copy=loadState(makeSave(st));copy.speed=st.speed;core.hp=hp;copy.campaign!.defence!.bases[0].hp=hp;st.flow!.rev++;copy.flow!.rev++;
  run(st,4);run(copy,4);assert.equal(stateHash(st),stateHash(copy));
  // Output disconnection is a real packing command; its held item returns to the pockets.
  if(!inReach(st,arm.x,arm.y))walk(st,arm.x,arm.y);applyCommands(st,[{type:'pickUp',x:arm.x,y:arm.y}]);assert.equal(machineAt(st,arm.x,arm.y),undefined);
  run(st,25);assert.equal(m.out,5);assert.equal(m.busy,false);assert.equal(machineStatus(st,m).state,'blocked');
  const made=st.flow!.stats.made.concrete;run(st,5);assert.equal(st.flow!.stats.made.concrete,made);
  if(!inReach(st,m.x,m.y,3))walk(st,m.x,m.y);applyCommands(st,[{type:'construct',edits:[{action:'pickUp',x:m.x,y:m.y}]}]);assert.ok(actionResult(st).ok,actionResult(st).reason);
  assert.equal(st.engineer.inv.mixer,1);assert.ok((st.engineer.inv.concrete??0)>=5);assert.ok((output.inv.concrete??0)>=0);conserved(st);
});

test('paid Barricades conserve concrete through history, damage, saved repair and packing',()=>{
  const st=createCampaign();unlock(st);supplies(st);const {input,output}=line(st);put(st,input,'stone',40);run(st,50);
  if(!inReach(st,output.x,output.y,2))walk(st,output.x,output.y);applyCommands(st,[{type:'chestTake',x:output.x,y:output.y,item:'concrete',n:20}]);
  const G=ground(st),t=G.opening!.gate[1],x=t%G.tw,y=Math.floor(t/G.tw);walk(st,x,y);
  const steel=st.engineer.inv.steel,concrete=st.engineer.inv.concrete;
  applyCommands(st,[{type:'construct',edits:[{action:'place',item:'barricade',x,y,dir:0}]}]);assert.ok(actionResult(st).ok,actionResult(st).reason);
  assert.equal(st.engineer.inv.steel,steel-2);assert.equal(st.engineer.inv.concrete,concrete-4);
  applyCommands(st,[{type:'undoBuild'},{type:'redoBuild'}]);assert.ok(actionResult(st).ok);assert.equal(st.flow!.stats.placed.concrete,4);
  const wall=machineAt(st,x,y)!;assert.equal(defenceHp(wall),240);assert.equal(passable(st,x,y),false);assert.equal(accepts(st,wall,'concrete'),false);
  damageDefence(st,wall,240);assert.equal(passable(st,x,y),true);assert.equal(canPickUp(st,x,y).ok,false);
  const paid=st.engineer.inv.steel;applyCommands(st,[{type:'repairDefence',x,y},{type:'repairDefence',x,y}]);assert.equal(st.engineer.inv.steel,paid-2);
  run(st,2);const copy=loadState(makeSave(st));copy.speed=st.speed;run(st,2.1);run(copy,2.1);assert.equal(stateHash(st),stateHash(copy));assert.equal(defenceHp(wall),40);
  for(let i=0;i<5;i++){applyCommands(st,[{type:'repairDefence',x,y}]);run(st,4.1);}
  assert.equal(defenceHp(wall),240);applyCommands(st,[{type:'pickUp',x,y}]);assert.equal(st.engineer.inv.barricade,1);assert.equal(st.flow!.stats.placed.concrete,4);conserved(st);
});

test('concrete saves reject corrupt production and counters; construction requires concrete',()=>{
  const st=createCampaign();unlock(st);supplies(st);const {m}=line(st),G=ground(st),t=G.opening!.gate[1];
  assert.match(canPlace(st,'barricade',t%G.tw,Math.floor(t/G.tw)).reason,/4 concrete/);
  for(const mutate of [(s:SimState)=>{s.flow!.machines.find(x=>x.id===m.id)!.out=6;},(s:SimState)=>{s.flow!.machines.find(x=>x.id===m.id)!.recipe='shot';},
    (s:SimState)=>{s.flow!.machines.find(x=>x.id===m.id)!.inv={concrete:1};},(s:SimState)=>{s.flow!.stats.placed.concrete=-1;}]) {
    const copy=structuredClone(st);mutate(copy);assert.throws(()=>loadState(copy),/concrete|Mixer/);
  }
  const old=structuredClone(st);delete old.campaign!.knowledge;old.campaign!.version=6;old.campaign!.recruits={version:1,sites:[old.campaign!.recruits!.sites[0]]};assert.throws(()=>loadState(old),/version 7/);
  const overlap=structuredClone(st),[a,b]=overlap.campaign!.recruits!.sites;b.x=a.x;b.y=a.y;b.block=a.block;assert.throws(()=>loadState(overlap),/overlapping/);
  const inherited=structuredClone(st);inherited.campaign!.recruits!.sites[1].inherited=true;assert.throws(()=>loadState(inherited),/recruit/);
  const sparse=structuredClone(st);Reflect.deleteProperty(sparse.flow!.ledger!.base,'concrete');sparse.engineer.inv.concrete=1;
  assert.equal(conservation(sparse).ok,false);assert.match(conservation(sparse).problems.join(','),/concrete: \+1/);
});

test('crawlers physically breach a sealed Barricade court',()=>{
  const st=createCampaign();unlock(st);supplies(st);
  // Labelled combat fixture supplies finished concrete; the production/payment chain is tested above.
  st.engineer.inv.concrete=40;st.flow!.ledger=openLedger(st);
  const G=ground(st),gate=G.opening!.gate,t=gate[1],dir=G.opening!.direction;
  walk(st,t%G.tw-[0,1,0,-1][dir]*4,Math.floor(t/G.tw)-[-1,0,1,0][dir]*4);
  for(const t of gate)applyCommands(st,[{type:'place',item:'barricade',x:t%G.tw,y:Math.floor(t/G.tw)}]);
  assert.ok(gate.every(t=>machineAt(st,t%G.tw,Math.floor(t/G.tw))?.kind==='barricade'));
  assert.ok(campaignOrigin(st,st.campaign!.defence!.bases[0])>=0);
  st.t=2400;tickCampaignSchedule(st,threatOf(st.flow!));st.t=3300;run(st,90);
  assert.ok(gate.some(t=>machineAt(st,t%G.tw,Math.floor(t/G.tw))?.hp===0));conserved(st);
});
