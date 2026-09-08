import { test } from 'node:test';
import assert from 'node:assert/strict';
import { createCampaign, ground, applyCommands, advanceFlow, machineAt, passable, registerBase, nominateBase,
  tickCampaignSchedule, campaignStaging, campaignOrigin, campaignWarning, damageCore, repairCheck,
  loadState, makeSave, stateHash, defenceHp, canPlace, damageDefence, replay, conservation,
  type SimState, type Kind, type Command, type LoggedCommand } from '../src/index';
import { threatOf } from '../src/threat';
import { headingWord } from '../src/move';
import { loadSnapshot } from '../../game/src/session';

const run=(st:SimState,seconds:number)=>advanceFlow(st,seconds,[],Math.ceil(seconds*20)+1);
const schedule=(st:SimState,t:number)=>{st.t=t;tickCampaignSchedule(st,threatOf(st.flow!));};
/** Registry/clock fixtures isolate scheduling. Paid placement is real; relocation is not travel evidence. */
function fixture(seed=3,which=0):SimState {
  const st=createCampaign(seed),d=st.campaign!.defence!;
  d.sites=[];d.lastMinorSlot=100000;
  applyCommands(st,[{type:'chestTake',item:'steel',n:80},{type:'chestTake',item:'copper',n:20}]);
  for(const s of [st.campaign!.expansion!.station,st.campaign!.districts!.station]) {
    s.restoredAt=0;st.blocks[s.block].state=2;st.blocks[s.block].subOn=true;registerBase(st,s.block);
  }
  st.campaign!.expansion!.grantedAt=0;st.campaign!.expansion!.radio.restoredAt=0;
  d.nominations=[];nominateBase(st,d.bases[which].block);schedule(st,2400);
  assert.equal(d.major!.block,d.bases[which].block);return st;
}
function placeAt(st:SimState,t:number,kind:Kind):void {
  const G=ground(st),x=t%G.tw,y=Math.floor(t/G.tw),e=st.engineer,pos=[e.x,e.y];
  const adjacent=[[x-1,y],[x+1,y],[x,y-1],[x,y+1]].find(([xx,yy])=>passable(st,xx,yy));assert.ok(adjacent);
  e.x=adjacent[0]+.5;e.y=adjacent[1]+.5;
  const check=canPlace(st,kind,x,y);assert.ok(check.ok,`seed ${st.seed}: ${kind} at ${x},${y}: ${check.reason}`);
  applyCommands(st,[{type:'place',item:kind,x,y}]);assert.equal(machineAt(st,x,y)?.kind,kind);
  e.x=pos[0];e.y=pos[1];
}
function continuation(st:SimState,seconds:number):void {
  const saved=loadState(makeSave(st));saved.speed=st.speed;
  run(st,seconds);run(saved,seconds);assert.equal(stateHash(st),stateHash(saved));
}

test('post-lock paid structures or player presence reroute staging on the same approach across three bases and holdout seeds',()=>{
  for(const seed of [3,4,5,8,11,13])for(let which=0;which<3;which++) {
    const st=fixture(seed,which),d=st.campaign!.defence!,a=d.major!,core=d.bases[which],G=ground(st);
    const locked={id:a.id,block:a.block,origin:a.origin,startsAt:a.startsAt};
    const kind=which%2?'belt':'wall';
    // Some natural origins contain rubble: a paid pole can occupy it without mining/altering the fixture.
    const check=canPlace(st,kind,a.origin%G.tw,Math.floor(a.origin/G.tw));
    if(check.reason==='not buildable ground') {
      // This natural origin cannot receive construction. Player presence still requires a safe fallback.
      st.engineer.x=a.origin%G.tw+.5;st.engineer.y=Math.floor(a.origin/G.tw)+.5;
    }else placeAt(st,a.origin,check.reason==='rubble in the way'?'pole':kind);
    const staging=campaignStaging(st,core,a.origin);assert.ok(staging>=0);assert.notEqual(staging,a.origin);
    assert.equal(headingWord([staging%G.tw-core.x,Math.floor(staging/G.tw)-core.y]),headingWord([a.origin%G.tw-core.x,Math.floor(a.origin/G.tw)-core.y]));
    schedule(st,a.startsAt);assert.equal(d.majorSpawned,1);assert.equal(a.remaining,59);
    const body=threatOf(st.flow!).crawlers.find(c=>c.campaign?.layer==='major')!;
    assert.equal(body.campaign!.origin,staging);assert.equal(body.y,Math.floor(staging/G.tw)+.5);
    assert.equal(st.flow!.occ[staging],undefined,'birth is on empty ground');
    continuation(st,1);
    assert.deepEqual({id:a.id,block:a.block,origin:a.origin,startsAt:a.startsAt},locked);
  }
});

test('no staging ground explicitly defers without losing roster, resumes after paid obstruction removal, and reloads deterministically',()=>{
  const st=fixture(),d=st.campaign!.defence!,a=d.major!,core=d.bases[0],G=ground(st);
  // Finite injected test stock, solely to occupy every candidate; no opening-economy claim.
  st.engineer.inv.steel+=1000;
  let count=0;
  for(let t=campaignStaging(st,core,a.origin);t>=0;t=campaignStaging(st,core,a.origin)) {
    placeAt(st,t,'belt');assert.ok(++count<500);
  }
  schedule(st,a.startsAt);assert.equal(a.waiting,'approach');assert.equal(a.remaining,60);assert.equal(d.majorSpawned,0);
  assert.match(campaignWarning(st),/delayed.*staging/);continuation(st,2);
  const e=st.engineer,pos=[e.x,e.y],x=a.origin%G.tw,y=Math.floor(a.origin/G.tw);
  e.x=x+.5;e.y=y+.5;applyCommands(st,[{type:'pickUp',x,y}]);e.x=pos[0];e.y=pos[1];
  assert.equal(machineAt(st,x,y),undefined);continuation(st,2);
  assert.equal(a.waiting,undefined);assert.ok(d.majorSpawned>0);assert.equal(a.remaining+d.majorSpawned,60);
  placeAt(st,a.origin,'belt');continuation(st,4);assert.equal(a.waiting,'approach','restoring the obstruction defers the remainder again');
});

test('staging fallback preserves received radio precision and a locked remote target through an outage and load',()=>{
  const st=fixture(3,1),d=st.campaign!.defence!,a=d.major!,r=st.campaign!.expansion!.radio,G=ground(st);
  // Isolated power/upgrade fixture; their paid unlocks are covered by expansion/defence tests.
  const gen=st.flow!.machines.find(m=>m.kind==='generator')!;gen.x=r.x+2;gen.y=r.y;gen.inv.coal=10;st.flow!.rev++;
  d.radioUpgrade=true;schedule(st,2401);assert.ok(d.warning?.approach);
  const message=structuredClone(d.warning),locked=a.id;
  placeAt(st,a.origin,'belt');schedule(st,3300);assert.equal(d.majorSpawned,1);
  const body=threatOf(st.flow!).crawlers.find(c=>c.campaign?.layer==='major')!;
  assert.notEqual(body.campaign!.origin,a.origin);assert.deepEqual(d.warning,message);
  gen.inv.coal=0;schedule(st,3301);assert.match(campaignWarning(st),/last received/);
  continuation(st,1);assert.equal(d.major!.id,locked);assert.deepEqual(d.warning,message);
  assert.equal(headingWord([body.campaign!.origin%G.tw-d.bases[1].x,Math.floor(body.campaign!.origin/G.tw)-d.bases[1].y]),message!.approach);
});

test('withdrawing bodies breach a newly blocked birth tile and prevent recovery or rest completion until they physically exit',()=>{
  const st=fixture(),d=st.campaign!.defence!,a=d.major!,G=ground(st),core=d.bases[0];
  schedule(st,a.startsAt);run(st,2);
  const body=threatOf(st.flow!).crawlers.find(c=>c.campaign?.layer==='major')!,origin=body.campaign!.origin;
  placeAt(st,origin,'wall');const wall=machineAt(st,origin%G.tw,Math.floor(origin/G.tw))!;
  damageCore(st,core,300);assert.match(repairCheck(st,core.x,core.y),/leave/);
  continuation(st,1);assert.ok(d.major);assert.ok(threatOf(st.flow!).crawlers.length>0);
  continuation(st,30);assert.equal(defenceHp(wall),0);assert.equal(d.major,null);
  assert.equal(threatOf(st.flow!).crawlers.filter(c=>c.campaign?.layer==='major').length,0);
  assert.equal(d.history.at(-1)?.defeated,true);assert.ok(d.nextDawn+900>=d.lastMajorEnd+2400);
});

test('minor withdrawal at dusk is physical, saved and exclusive with the major roster',()=>{
  const st=fixture(),d=st.campaign!.defence!,T=threatOf(st.flow!);
  d.lastMinorSlot=3;schedule(st,3000);assert.ok(d.minor);run(st,2);
  const minor=d.minor!,G=ground(st);placeAt(st,minor.origin,'wall');
  schedule(st,3300);assert.equal(d.major!.waiting,'minor');assert.equal(d.majorSpawned,0);
  continuation(st,1);assert.equal(d.majorSpawned,0);assert.ok(d.minor);
  const resumed=loadState(makeSave(st));resumed.speed=st.speed;
  for(let second=0;second<60&&d.majorSpawned===0;second++) {
    run(st,1);run(resumed,1);
    assert.ok(!(T.crawlers.some(c=>c.campaign?.layer==='minor')&&T.crawlers.some(c=>c.campaign?.layer==='major')));
  }
  assert.equal(stateHash(st),stateHash(resumed));assert.ok(d.majorSpawned>0);assert.equal(d.minor,null);
  assert.equal(defenceHp(machineAt(st,minor.origin%G.tw,Math.floor(minor.origin/G.tw))!),0);
});

test('restoration ties, disabled targets, recommissioning and post-lock nominations retain one promised schedule through saves',()=>{
  const st=fixture(),d=st.campaign!.defence!;
  // Resolve the isolated lock without bodies; the remaining schedule is tested separately from combat.
  d.major!.retreat=true;d.major!.remaining=0;schedule(st,3301);
  const promised=d.nextDawn,remote=d.bases.slice(1).sort((a,b)=>a.block-b.block);
  nominateBase(st,remote[1].block);nominateBase(st,remote[0].block);
  schedule(st,promised);assert.equal(d.major!.block,remote[0].block,'stable block tie-break');
  const id=d.major!.id;damageCore(st,remote[0],300);nominateBase(st,remote[1].block);
  continuation(st,1);assert.equal(d.major!.id,id);assert.equal(d.nextDawn,promised);
  schedule(st,d.major!.startsAt);continuation(st,1);
  assert.equal(d.major,null);const later=d.nextDawn;
  const e=st.engineer;e.x=remote[0].x-1+.5;e.y=remote[0].y+.5;
  applyCommands(st,[{type:'repairDefence',x:remote[0].x,y:remote[0].y}]);assert.ok(d.repair);
  continuation(st,13);assert.equal(remote[0].hp,300);assert.equal(d.nextDawn,later);
  schedule(st,later);assert.equal(d.major!.block,remote[0].block,'latest recommission wins the future window');
  assert.equal(d.nominations.length,0);continuation(st,1);
});

test('old defence saves retain attacks and paid jobs while incompatible old command histories stop claiming full replay',async()=>{
  const st=fixture(),d=st.campaign!.defence!;schedule(st,3300);
  const G=ground(st),origin=d.major!.origin,x=origin%G.tw,y=Math.floor(origin/G.tw);
  placeAt(st,origin,'wall');const wall=machineAt(st,x,y)!;damageDefence(st,wall,40);
  st.engineer.x=x-1+.5;st.engineer.y=y+.5;
  applyCommands(st,[{type:'repairDefence',x,y}]);assert.ok(d.repair);
  const old=makeSave(st,{log:[],logComplete:true});old.state.campaign!.defence!.version=1;
  old.state.flow!.threat!.crawlers[0].campaign!.waypoint=0; // legacy pursuit could retain a distant waypoint
  const prior=Object.getOwnPropertyDescriptor(globalThis,'localStorage');
  Object.defineProperty(globalThis,'localStorage',{configurable:true,value:{getItem:()=>JSON.stringify(old)}});
  try {
    const loaded=await loadSnapshot('local:old-defence');assert.equal(loaded.logComplete,false);
    assert.equal(loaded.state.campaign!.defence!.version,2);
    assert.deepEqual(loaded.state.campaign!.defence!.major,d.major);
    assert.equal(loaded.state.campaign!.defence!.nextDawn,d.nextDawn);
    assert.deepEqual(loaded.state.campaign!.defence!.repair,d.repair);
    assert.deepEqual(loaded.state.engineer.inv,st.engineer.inv);
    assert.equal(loaded.state.flow!.threat!.crawlers[0].campaign!.waypoint,undefined);
  }finally{if(prior)Object.defineProperty(globalThis,'localStorage',prior);else Reflect.deleteProperty(globalThis,'localStorage');}
  continuation(st,2);
});

test('pursuit discards an attack waypoint before walking away and remains valid across saved continuation',()=>{
  const st=fixture(),core=st.campaign!.defence!.bases[0];schedule(st,3300);run(st,.05);
  const body=threatOf(st.flow!).crawlers.find(c=>c.campaign?.layer==='major')!;
  assert.ok(body.campaign!.waypoint!==undefined);
  const target=[[3,0],[-3,0],[0,3],[0,-3]].map(([dx,dy])=>[Math.floor(body.x)+dx,Math.floor(body.y)+dy])
    .find(([x,y])=>passable(st,x,y)&&Math.hypot(x+.5-core.x,y+.5-core.y)<25);
  assert.ok(target);st.engineer.x=target[0]+.5;st.engineer.y=target[1]+.5;body.onPlayer=true;
  run(st,.05);assert.equal(body.campaign!.waypoint,undefined);continuation(st,2);
});

test('paid obstruction, ordinary walking and the resulting assault replay from a declared pre-dusk checkpoint',()=>{
  const st=fixture(),d=st.campaign!.defence!,G=ground(st),origin=d.major!.origin;
  const x=origin%G.tw,y=Math.floor(origin/G.tw),home=[st.engineer.x,st.engineer.y];
  // Explicit initial clock/position fixture; every subsequent action is an ordinary logged command.
  st.t=3298;st.engineer.x=x-1+.5;st.engineer.y=y+.5;
  const checkpoint=makeSave(st),baseline=conservation(st).unexplained,log:LoggedCommand[]=[];
  const command=(c:Command)=>{log.push({tick:st.flow!.tick,c});applyCommands(st,[c]);};
  command({type:'place',item:'wall',x,y});assert.equal(machineAt(st,x,y)?.kind,'wall');
  command({type:'move',x:home[0],y:home[1]});run(st,20);continuation(st,120);
  assert.equal(d.history.length,1);assert.ok(d.history[0].spawned>0);assert.ok(d.history[0].defeated);
  const repeated=loadState(checkpoint);replay(repeated,log,st.flow!.tick,{dropAim:false});
  assert.equal(stateHash(st),stateHash(repeated));assert.deepEqual(conservation(st).unexplained,baseline);
});

test('save validation refuses mismatched body targets, duplicate identities, lost roster entries and invalid delay/withdrawal metadata',()=>{
  const st=fixture();schedule(st,3300);
  for(const corrupt of [
    (s:SimState)=>{s.flow!.threat!.crawlers[0].to=s.campaign!.defence!.bases[1].block;},
    (s:SimState)=>{s.flow!.threat!.crawlers.push(structuredClone(s.flow!.threat!.crawlers[0]));},
    (s:SimState)=>{s.campaign!.defence!.majorSpawned=0;},
    (s:SimState)=>{s.campaign!.defence!.major!.waiting='minor';},
    (s:SimState)=>{s.flow!.threat!.crawlers[0].campaign!.withdrawing=true;},
    (s:SimState)=>{s.flow!.threat!.crawlers[0].campaign!.waypoint=0;},
    (s:SimState)=>{const d=s.campaign!.defence!;d.minor={id:d.major!.id,block:d.major!.block,origin:d.major!.origin,retreat:false};},
  ]){const bad=structuredClone(st);corrupt(bad);assert.throws(()=>loadState(bad));}
});

test('fresh origins and same-approach fallback exist across all base positions on seeds 1–32',()=>{
  for(let seed=1;seed<=32;seed++) {
    const st=fixture(seed);
    for(const core of st.campaign!.defence!.bases) {
      const origin=campaignOrigin(st,core);assert.ok(origin>=0);assert.equal(campaignStaging(st,core,origin),origin);
    }
  }
});
