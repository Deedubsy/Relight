import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { createCampaign, applyCommands, advanceFlow, loadState, makeSave, stateHash, findPath, ground,
  lockReason, recruitCheck, canPlace, campaignRecruited, conservation, HELD, idxOf, inReach, machineAt,
  type SimState, type Command } from '../src/index';
import { createSession, parseUrl, replaySession, loadSnapshot } from '../../game/src/session';

function run(st:SimState, seconds:number, commands:Command[]=[]):void { advanceFlow(st,seconds,commands,Math.ceil(seconds*20)+1); }

test('32 seeded shelters are accessible without a guardian fight and are permanent walkable reservations',()=>{
  for(let seed=1;seed<=32;seed++) {
    const st=createCampaign(seed),s=st.campaign!.recruits!.sites[0],G=ground(st);
    const path=findPath(st,Math.floor(st.engineer.x),Math.floor(st.engineer.y),s.x,s.y);
    assert.ok(path,`seed ${seed}`);
    assert.notEqual(s.block,st.campaign!.homeBlock);
    assert.equal(s.recruitedAt,-1);assert.equal(s.seenAt,-1);
    assert.match(canPlace(st,'pole',s.x,s.y).reason,/survivor shelter/);
    // The direct path chosen by the normal movement planner also avoids guarded discovery space.
    for(const t of path) {
      const cache=st.campaign!.discovery!;
      assert.ok(Math.hypot(t%G.tw-cache.x,Math.floor(t/G.tw)-cache.y)>10);
    }
    const loaded=loadState(makeSave(st));assert.equal(stateHash(loaded),stateHash(st));
  }
});

test('ordinary walk and recruitment grant real electrical plans once, preserve stock and replay',()=>{
  const prior=Object.getOwnPropertyDescriptor(globalThis,'location');
  Object.defineProperty(globalThis,'location',{configurable:true,value:{href:'http://localhost/'}});
  const session=(()=>{try{return createSession(parseUrl('?rules=exploration-v2&seed=3&view=world'));}
    finally{if(prior)Object.defineProperty(globalThis,'location',prior);else Reflect.deleteProperty(globalThis,'location');}})(),st=session.state;
  const s=st.campaign!.recruits!.sites[0],home={x:st.engineer.x,y:st.engineer.y};
  assert.match(lockReason(st,'floodlight'),/recruit the Electricians/);
  assert.equal(lockReason(st,'substation'),'');
  assert.notEqual(recruitCheck(st,s.id),'');applyCommands(st,[{type:'recruitSurvivors',id:s.id}]);assert.equal(s.recruitedAt,-1);
  const command=(c:Command)=>{session.log.push({tick:st.flow!.tick,c});run(st,.05,[c]);};
  command({type:'factory',action:{type:'chestTake',item:'steel',n:70}});
  command({type:'factory',action:{type:'chestTake',item:'copper',n:40}});
  const initial=structuredClone(st.engineer.inv);
  command({type:'move',x:s.x+.5,y:s.y+.5});
  for(let i=0;i<180&&st.engineer.target;i++)run(st,1);
  assert.equal(recruitCheck(st,s.id),'');assert.ok(s.seenAt>=0);
  const seen=loadState(makeSave(st));assert.equal(seen.campaign!.recruits!.sites[0].recruitedAt,-1);
  command({type:'recruitSurvivors',id:s.id});const recruited=s.recruitedAt;
  command({type:'recruitSurvivors',id:s.id});
  assert.equal(s.recruitedAt,recruited);assert.deepEqual(st.engineer.inv,initial);
  assert.equal(lockReason(st,'floodlight'),'');assert.equal(lockReason(st,'bigpole'),'');
  assert.equal(st.campaign!.defence!.bases.length,1,'a recruit is not another base');
  assert.equal(st.campaign!.discovery!.recoveredAt,-1,'separate reward ownership');
  command({type:'move',...home});for(let i=0;i<180&&st.engineer.target;i++)run(st,1);
  for(const kind of ['floodlight','bigpole'] as const) {
    let placed=false;
    for(let y=Math.floor(home.y)-6;y<=home.y+6&&!placed;y++)for(let x=Math.floor(home.x)-6;x<=home.x+6;x++) {
      if(!canPlace(st,kind,x,y).ok||!inReach(st,x,y,2))continue;
      command({type:'place',item:kind,x,y});assert.equal(machineAt(st,x,y)?.kind,kind);placed=true;break;
    }
    assert.ok(placed,kind);
  }
  assert.ok(conservation(st).ok);
  const replay=replaySession(session);assert.ok(!('error' in replay),JSON.stringify(replay));
  if(!('error' in replay))assert.equal(stateHash(replay.state),stateHash(st));
  const copy=loadState(makeSave(st));copy.speed=st.speed;run(copy,1);run(st,1);assert.equal(stateHash(copy),stateHash(st));
});

test('recruitment requires an able engineer on foot and persists through disabled cores and block changes',()=>{
  const st=createCampaign(),s=st.campaign!.recruits!.sites[0];
  // Labelled location/status fixture isolates command rejection and unlock ownership.
  st.engineer.x=s.x+.5;st.engineer.y=s.y+.5;st.engineer.down=0;
  applyCommands(st,[{type:'recruitSurvivors',id:s.id}]);assert.equal(s.recruitedAt,-1);
  st.engineer.down=-1;st.engineer.truckSeat=true;
  applyCommands(st,[{type:'recruitSurvivors',id:s.id}]);assert.equal(s.recruitedAt,-1);
  delete st.engineer.truckSeat;
  applyCommands(st,[{type:'recruitSurvivors',id:'unknown'},{type:'recruitSurvivors',id:s.id}]);
  assert.equal(s.recruitedAt,0);st.campaign!.defence!.bases[0].hp=0;
  assert.equal(lockReason(st,'bigpole'),'');
  const legacy=st.survivors.find(s=>s.name==='Electricians')!;
  const bi=idxOf(st,legacy.x,legacy.y);st.blocks[bi].state=HELD;
  assert.ok(campaignRecruited(st,'electricians'));
});

test('version 5 migration keeps earned plans, existing workshop history and layouts; unearned plans stay locked',()=>{
  const original=JSON.parse(readFileSync(new URL('../../../docs/evidence/ex08b-2026-09-07/campaign/network.json',import.meta.url),'utf8'));
  const migrated=loadState(original);
  assert.equal(migrated.campaign!.version,9);
  assert.deepEqual(migrated.flow!.machines,original.state.flow.machines);
  assert.deepEqual(migrated.campaign!.discovery,original.state.campaign.discovery);
  assert.equal(stateHash(loadState(makeSave(migrated))),stateHash(migrated));
  for(const earned of [false,true]) {
    const st=createCampaign(),sv=st.survivors.find(s=>s.name==='Electricians')!,bi=idxOf(st,sv.x,sv.y);
    delete st.campaign!.recruits;delete st.campaign!.turbine;delete st.campaign!.knowledge;st.campaign!.version=5;
    if(earned)st.fallen[bi]=true;
    const copy=loadState(st),site=copy.campaign!.recruits!.sites[0];
    assert.equal(site.inherited,earned);assert.equal(campaignRecruited(copy,'electricians'),earned);
    assert.equal(lockReason(copy,'substation'),'');
    assert.equal(stateHash(loadState(makeSave(copy))),stateHash(copy));
  }
});

test('current saves reject missing, duplicated or malformed recruits and old logs are marked incomplete',async()=>{
  const st=createCampaign();
  for(const corrupt of [
    (s:SimState)=>{delete s.campaign!.recruits;},
    (s:SimState)=>{s.campaign!.recruits!.sites.push(structuredClone(s.campaign!.recruits!.sites[0]));},
    (s:SimState)=>{s.campaign!.recruits!.sites[0].id='wrong';},
    (s:SimState)=>{s.campaign!.recruits!.sites[0].recruitedAt=100;},
    (s:SimState)=>{s.campaign!.recruits!.sites[0].inherited=true;},
  ]){const copy=structuredClone(st);corrupt(copy);assert.throws(()=>loadState(copy),/recruit/);}
  const saved=makeSave(st,{log:[],logComplete:true});delete saved.state.campaign!.recruits;delete saved.state.campaign!.turbine;delete saved.state.campaign!.knowledge;saved.state.campaign!.version=5;
  const prior=Object.getOwnPropertyDescriptor(globalThis,'localStorage');
  Object.defineProperty(globalThis,'localStorage',{configurable:true,value:{getItem:()=>JSON.stringify(saved)}});
  try{const loaded=await loadSnapshot('local:p7-old');assert.equal(loaded.logComplete,false);assert.equal(loaded.state.campaign!.version,9);}
  finally{if(prior)Object.defineProperty(globalThis,'localStorage',prior);else Reflect.deleteProperty(globalThis,'localStorage');}
});
